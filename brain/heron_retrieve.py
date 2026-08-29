# Heron-Agent:  HERON-RAG-RNK-006, HERON-RAG-CTX-007
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The whole lookup, in the order docs/05 s4 sets out.

    python brain/heron_retrieve.py "show me every duct" --revit 2024

Step 11 of docs/27-build-order.md. Steps 9 and 10 each built half a search and
each is unreliable alone; this is where they stop being two answers.

THE ORDER MATTERS AND IT IS NOT NEGOTIABLE
------------------------------------------
  1. STRUCTURED FILTER, as a plain SQL WHERE. Scope, Revit version, status,
     domain, kind. It eliminates most of the corpus for free and it enforces
     the rules that must never be a matter of degree.
  2. Two searches over the SURVIVORS - keywords (Step 9) and vectors (Step 10).
  3. Reciprocal rank fusion, to make one list out of two.
  4. A small quality nudge, which may settle a near-tie and may not overturn a
     clearly better match.

WHY THE VERSION FILTER IS A WALL AND NOT A WEIGHTING (docs/05 s8)
----------------------------------------------------------------
A fragment written for Revit 2021, applied in 2025, does not announce itself.
It runs, it half-works, and the damage is found later by somebody measuring
something. That is the confident-wrong-retrieval failure, and a ranking cannot
protect against it: rank a wrong-version fragment 5th instead of 1st and it
still comes back on a quiet day when nothing else matches.

So an incompatible fragment is NOT RETURNED. Not demoted, not flagged - absent.
The test proves it by making such a fragment the best possible textual match
and confirming it still does not appear.

WHY FUSION RATHER THAN PICKING A WINNER
---------------------------------------
Step 10 recorded the case that forced this, in numbers: on "show me every duct
in the model" the keyword route ranks the duct filter FIRST and the vector
route ranks it SECOND, because "show me" happens to be a declared phrasing of
a different fragment. Neither route is trustworthy alone and no rule about
which to prefer survives contact with the next example. Reciprocal rank fusion
asks both and rewards agreement, which is the only signal here that is not an
opinion.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_scope as SCOPE                                   # noqa: E402
import heron_search as SEARCH                                 # noqa: E402
import heron_embed as EMBED                                   # noqa: E402
import heron_fragment as FRAG                                 # noqa: E402

# The constant in reciprocal rank fusion. 60 is the value the technique is
# normally used with; what it does is stop rank 1 from dwarfing everything
# below it, so a fragment both routes place 2nd or 3rd can still beat one that
# only ONE route loved. That is the whole point of fusing rather than picking.
RRF_K = 60

# THE TWO ROUTES DO NOT GET AN EQUAL VOTE, AND THE REASON IS MEASURED.
#
# With equal weights, a fragment ranked (words 1st, nearness 2nd) and one
# ranked (words 2nd, nearness 1st) score IDENTICALLY - fusion is symmetric -
# and the winner is then decided by whatever the tiebreak happens to be. On the
# very first run of this file that was ALPHABETICAL ORDER, which got the right
# answer for no reason at all and would have got the wrong one had the ids been
# named the other way round.
#
# So the weight follows the backend, because Step 10 measured what each is
# worth rather than assuming:
#
#   lexical  the built-in one. Measured as NOT meaning - synonyms score zero -
#            so it is a real signal about spelling and a weak one about intent.
#            docs/05 s4 already argues the same thing from the other end: BIM
#            requests are full of exact tokens that embeddings handle worst.
#   model    a trained encoder. It earns an equal vote, and gets one.
#
# Revisit this when A7 puts a trained model in front of it - not by feeling,
# by running the same queries and looking.
KEYWORD_WEIGHT = 1.0
VECTOR_WEIGHT_BY_BACKEND = {
    EMBED.LEXICAL: 0.6,
    EMBED.MODEL:   1.0,
}

# The quality nudge. Deliberately smaller than the gap between adjacent fusion
# ranks, so it settles near-ties and cannot overturn a clearly better match:
# a PROVEN fragment should win a coin-toss against a DRAFT one, and should NOT
# win an argument against a fragment that actually matches the request.
# One rank of fusion is 1/(K+1) - 1/(K+2) = 0.000264 at K=60. Every value here
# is well inside that, so the nudge can settle a dead heat and cannot move a
# fragment past one the routes actually ranked higher.
#
# THE FIRST VERSION OF THIS TABLE WAS EIGHT TIMES TOO BIG - 0.0025 against a
# rank gap of 0.00026 - which would have let a PROVEN fragment jump roughly
# eight places over better matches. The docstring claimed it could not. The
# test asserted the arithmetic rather than the intention and caught it, which
# is the entire reason that check is written as a comparison and not as a
# sentence.
QUALITY = {
    "PRODUCTION":  0.00012,
    "PROVEN":      0.00010,
    "VALIDATED":   0.00006,
    "TESTING":     0.00003,
    "DRAFT":       0.0,
    "DISCOVERED": -0.00004,
    "DEPRECATED": -0.00015,
    "ARCHIVED":   -0.00020,
}


class Candidate(object):
    def __init__(self, fragment_id, row):
        self.id = fragment_id
        self.row = row
        self.keyword_rank = None
        self.vector_rank = None
        self.vector_score = None
        self.fused = 0.0
        self.quality = 0.0

    @property
    def score(self):
        return self.fused + self.quality

    def why(self):
        parts = []
        if self.keyword_rank is not None:
            parts.append("words #%d" % self.keyword_rank)
        if self.vector_rank is not None:
            parts.append("nearness #%d" % self.vector_rank)
        if not parts:
            parts.append("no route")
        agreed = self.keyword_rank is not None and self.vector_rank is not None
        return "%s%s" % (" + ".join(parts), " - both agree" if agreed else "")

    def __repr__(self):
        return "<%s %.4f %s>" % (self.id, self.score, self.why())


class Excluded(object):
    """A fragment the structured filter removed, and why.

    Kept and reported rather than silently dropped. "Heron found nothing" and
    "Heron found something it is not allowed to offer you on Revit 2025" are
    completely different situations, and a user told only the first will go
    looking for a fragment that is sitting right there.
    """

    def __init__(self, fragment_id, reason):
        self.id = fragment_id
        self.reason = reason

    def __repr__(self):
        return "<excluded %s: %s>" % (self.id, self.reason)


# ---------------------------------------------------------------------------
# Stage 1 - the structured filter
# ---------------------------------------------------------------------------

# Statuses a fragment may be OFFERED at. DEPRECATED and ARCHIVED are excluded
# by default: they exist so a record is never destroyed (Golden Rule 4), not so
# they can be handed back as answers.
OFFERABLE = ("DISCOVERED", "DRAFT", "TESTING", "VALIDATED", "PROVEN", "PRODUCTION")


def eligible(store, revit=None, domain=None, kind=None, statuses=OFFERABLE):
    """(rows, excluded). Everything allowed to be an answer, and what was not.

    This is a SQL WHERE, not a search, and it runs FIRST. Most of the corpus
    disappears here for nothing, and - far more importantly - the rules that
    must never be a matter of degree are applied where degree does not exist.
    """
    rows, excluded = [], []
    for row in store.fragments():
        if row["status"] not in statuses:
            excluded.append(Excluded(row["id"], "status is %s" % row["status"]))
            continue

        if revit is not None:
            supported = [v for v in (row["revit"] or "").split(",") if v]
            if str(revit) not in supported:
                excluded.append(Excluded(
                    row["id"],
                    "declared for Revit %s, and this is %s. NOT ranked lower - "
                    "not offered at all" % (", ".join(supported) or "nothing",
                                            revit)))
                continue

        if domain and row["domain"] != domain:
            excluded.append(Excluded(row["id"], "domain is %s" % row["domain"]))
            continue

        if kind and row["kind"] != kind:
            excluded.append(Excluded(row["id"], "kind is %s" % row["kind"]))
            continue

        rows.append(row)
    return rows, excluded


# ---------------------------------------------------------------------------
# Stages 2 to 4 - two searches, fused, then nudged
# ---------------------------------------------------------------------------

def retrieve(store, text, revit=None, domain=None, kind=None, limit=5,
             pool=20):
    """The full stack. Returns (candidates, excluded).

    `pool` is how deep each route is asked before fusing - docs/05 s4's "top
    ~20". Fusing only the top few would throw away exactly the agreement this
    is built to find: a fragment ranked 1st by words and 8th by nearness is a
    better answer than one ranked 2nd by words and nowhere by nearness, and
    that is invisible if nearness is only asked for three.
    """
    allowed, excluded = eligible(store, revit, domain, kind)
    if not allowed:
        return [], excluded

    keep = set(row["id"] for row in allowed)
    by_id = dict((row["id"], row) for row in allowed)
    candidates = {}

    def candidate(fragment_id):
        if fragment_id not in candidates:
            candidates[fragment_id] = Candidate(fragment_id, by_id[fragment_id])
        return candidates[fragment_id]

    # -- stage 2a: keywords, filtered to the survivors --------------------
    rank = 0
    for hit in SEARCH.keywords(store, text, limit=pool * 3):
        if hit["id"] not in keep:
            continue
        rank += 1
        candidate(hit["id"]).keyword_rank = rank
        if rank >= pool:
            break

    # -- stage 2b: nearness, same treatment -------------------------------
    rank = 0
    for fragment_id, score in EMBED.nearest(store, text, limit=pool * 3):
        if fragment_id not in keep:
            continue
        rank += 1
        got = candidate(fragment_id)
        got.vector_rank = rank
        got.vector_score = score
        if rank >= pool:
            break

    # -- stage 3: reciprocal rank fusion -----------------------------------
    backend_name, _why = EMBED.backend()
    vector_weight = VECTOR_WEIGHT_BY_BACKEND.get(backend_name, 0.6)

    for got in candidates.values():
        if got.keyword_rank is not None:
            got.fused += KEYWORD_WEIGHT / (RRF_K + got.keyword_rank)
        if got.vector_rank is not None:
            got.fused += vector_weight / (RRF_K + got.vector_rank)

    # -- stage 4: the quality nudge ----------------------------------------
    for got in candidates.values():
        got.quality = QUALITY.get(got.row["status"], 0.0)

    # The tiebreak is the stronger route, then the id. Alphabetical order is
    # not a reason and must never be the last word on which fragment wins.
    ranked = sorted(candidates.values(),
                    key=lambda c: (-c.score,
                                   c.keyword_rank if c.keyword_rank else 999,
                                   c.id))
    return ranked[:limit], excluded


def find(store, text, revit=None, limit=5):
    """What the rest of Heron calls. Identity and cache first, then the stack.

    Steps 9's two one-lookup routes still come first and still mean what they
    meant: an exact declared phrasing is answered without any of this running.
    The difference is what happens when they miss - Step 9 fell through to
    keywords alone, and this falls through to both routes fused.

    THE STRUCTURED FILTER APPLIES TO THE SHORT CIRCUIT TOO. An exact phrase
    match on a fragment that does not support this Revit is still a fragment
    that does not support this Revit, and the wall does not have a door in it
    for convenience.
    """
    SEARCH.ensure_tables(store)
    allowed, _ = eligible(store, revit)
    keep = set(row["id"] for row in allowed)

    fid, status = SEARCH.short_circuit(store, text)
    if fid and fid in keep:
        return SEARCH.Answer(
            "identity", fid,
            autorun=status in SEARCH.RUNNABLE_UNASKED,
            note=("%s is %s, so it may run without asking" % (fid, status))
            if status in SEARCH.RUNNABLE_UNASKED else
            ("%s matched exactly but is %s - nobody has watched it work, so it "
             "is offered, not run" % (fid, status)))

    fid, hits = SEARCH.recall(store, text)
    if fid and fid in keep:
        return SEARCH.Answer("cache", fid,
                             note="seen %d time(s) before; a candidate, never "
                                  "a decision" % hits)

    pool = 20
    ranked, excluded = retrieve(store, text, revit=revit, limit=limit, pool=pool)
    if not ranked:
        note = "nothing matched"
        blocked = [e for e in excluded if "Revit" in e.reason]
        if blocked:
            note += (". %d fragment(s) were excluded by the Revit version "
                     "filter - they exist, they are just not for this release"
                     % len(blocked))
        return SEARCH.Answer("nothing", note=note, candidates=[])

    best = ranked[0]
    note = "%d candidate(s); best found by %s" % (len(ranked), best.why())

    # A candidate no route agreed on is a WEAK match, and saying so costs
    # nothing while staying silent costs the user a wrong turn.
    #
    # Deliberately NOT a score threshold. A cut-off would be a number invented
    # today and tuned tomorrow (D-33), and the first tune to reduce noise is
    # the one that starts hiding real answers. Agreement between two
    # independent routes is a fact about the result, not a dial.
    if best.keyword_rank is None or best.vector_rank is None:
        note += (". NO candidate was found by both routes, so treat this as a "
                 "weak match - one route liking something is not two agreeing")
    elif len(allowed) <= pool:
        # MEASURED LIMITATION, worth more than the label above.
        #
        # The nearness route ranks EVERY eligible fragment - it returns a
        # similarity for all of them, not a shortlist. So while the library is
        # smaller than `pool`, every candidate is found by both routes and
        # "both agree" is true of everything, including an unrelated question.
        # It becomes real evidence only once there are more fragments than the
        # pool and the routes have something to disagree about.
        #
        # Said out loud because a future reader will otherwise see "both agree"
        # on a 7-fragment library and believe it.
        note += (". Only %d fragment(s) were eligible, fewer than the pool of "
                 "%d - every one of them is found by both routes, so 'both "
                 "agree' means nothing here yet" % (len(allowed), pool))
    return SEARCH.Answer(
        "hybrid", best.id,
        candidates=[{"id": c.id, "capability": c.row["capability"],
                     "status": c.row["status"], "score": c.score,
                     "why": c.why()} for c in ranked],
        note=note)


def main(argv):
    revit = None
    if "--revit" in argv:
        i = argv.index("--revit")
        revit = argv[i + 1]
        argv = argv[:i] + argv[i + 2:]

    if not argv:
        print('  python brain/heron_retrieve.py "show me every duct" --revit 2024')
        return 2

    store = SCOPE.open_scope(SCOPE.GLOBAL)
    try:
        SEARCH.index(store)
        EMBED.index(store)
        text = " ".join(argv)
        answer = find(store, text, revit=revit)

        print("Asked:  %s%s" % (text, "   (Revit %s)" % revit if revit else ""))
        print("Route:  %s" % answer.route)
        print("Best:   %s" % (answer.fragment_id or "nothing"))
        print("        %s" % answer.note)
        for c in answer.candidates:
            print("          %-14s %-30s %-11s %.4f  %s"
                  % (c["id"], c["capability"], c["status"], c["score"], c["why"]))

        _rows, excluded = eligible(store, revit)
        for e in excluded:
            print("  excluded  %-14s %s" % (e.id, e.reason))
        return 0
    finally:
        store.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
