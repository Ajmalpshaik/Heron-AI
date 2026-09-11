# Heron-Agent:  HERON-RAG-RNK-006, HERON-RAG-CTX-007, HERON-RAG-LIB-001
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
  5. SAY HOW CONTESTED THE RESULT WAS. Stages 1 to 4 already compute every
     number this needs and used to throw all of them away, so a coin toss and
     a clear winner came back wearing the same face. See Contest.

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
import sqlite3
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

# ONE RANK OF FUSION - the unit every spread on this page is measured in.
#
# Derived, never typed. It is what one place in a route's ranking is worth to
# the fused score, and it is the yardstick the quality nudge below is already
# held against: the nudge must be SMALLER than this, or status would outrank
# matching. Measuring a shortlist's spread in the same unit means "the top two
# are half a rank apart" reads against a number the file already lives by,
# instead of against 0.0002 with nothing to compare it to.
ONE_RANK = 1.0 / (RRF_K + 1) - 1.0 / (RRF_K + 2)

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
        self.keyword_score = None      # bm25, negative, lower is better
        self.vector_rank = None
        self.vector_score = None       # cosine, higher is better
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


class Contest(object):
    """How contested a shortlist was. A MEASUREMENT, never a verdict.

    docs/work-notes/plans/rag/00-structure.md s3.1: retrieval answers with the
    same face whether it is sure or guessing, and the evidence was already on
    record. At 14 fragments brain/retrieval-history.md called a top five
    spanning 0.0021 "noise rather than ranking - a reader who takes the top hit
    as 'the answer' is reading a coin toss", and nobody was told that at query
    time. Every number below was computed by retrieve() before this class
    existed and discarded on the way out.

    WHAT IT REPORTS, AND WHY EACH ONE IS HERE

      top_gap    the winner's lead over the runner-up, in ONE_RANK. The one
                 number that answers "was this a coin toss?".
      spread     first to last across the shortlist, same unit. What
                 retrieval-history was describing in raw score.
      agreed     how many of the shortlist both routes found. Agreement is the
                 only signal in this file that is not an opinion - but see
                 pool_is_evidence.
      breadth    how many fragments the WORDS route matched at all.
      best_words
      best_near  the two MAGNITUDES fusion throws away. Reported, never ranked
                 on - see the warning below.

    THE THRESHOLD THAT IS NOT INVENTED. There is exactly one comparison here -
    top_gap against ONE rank - and it is not a dial. A gap smaller than one
    rank is a gap narrower than the quality nudge, which means the order could
    have been produced by the fragments' STATUS rather than by either route
    liking one more than the other. That is a fact about the arithmetic, and
    R-55 - a threshold is never moved to make a report look better - has
    nothing to bite on because there is no number here to move.

    WHAT THIS DELIBERATELY DOES NOT DO - and it is the whole reason it stops
    here. It does not DROP a candidate (R-56) and it does not REFUSE a question
    nothing covers (R-58). Both need a floor, and R-60 says a floor is derived
    from a measurement or it is not set at all.

    THE FUSED SCORE CANNOT BE THAT MEASUREMENT, and this is arithmetic rather
    than an opinion. Reciprocal rank fusion keeps ORDER and discards STRENGTH,
    so every shortlist looks similar from the outside no matter what went into
    it. Measured 2026-09-11 at 360 fragments on the lexical backend:

        "what is the best food for a cat"    top 0.0254   gap 2.4 ranks
        "show me every duct in the model"    top 0.0246   gap 2.1 ranks

    The cat question scores HIGHER than the duct one. A floor on the fused
    score would have to cut the real question to reach the unreal one.

    NEITHER SURVIVING MAGNITUDE IS THAT MEASUREMENT EITHER, ON THIS BACKEND.
    Three questions suggested they might be - a real question appeared to make
    the words route select rather than match everything - so the run was
    widened to twelve, six BIM and six with no BIM content at all, and the
    shape did not survive it. Every column overlaps:

                        gap        breadth     bm25          nearness
        six BIM        2.1 - 8.1   265 - 360   -4.27 -10.37  0.19 - 0.59
        six not BIM    0.9 - 29.9  245 - 360   -0.00 - -6.59 0.15 - 0.47

    "how do I bake sourdough bread" has the widest winning gap of all twelve
    and the second most selective words route. "tag every mechanical
    equipment" is less near than the cat question. A floor anywhere on any of
    these four columns cuts real questions to reach unreal ones.

    SO NOTHING ACTS ON THEM, AND THE REASON IS NOT CAUTION - it is that the
    numbers say the lexical backend has no opinion about meaning, which is
    precisely what heron_embed's own docstring says about it: "IT IS NOT
    MEANING". Asking it to tell a duct from a cat is asking it for the one
    thing it says it cannot do. The measurement belongs on the MODEL backend,
    where nearness is meaning, and it has not been run because the container
    this was written in refuses huggingface.co - the same block heron_embed
    recorded on 2026-08-28, still in force on 2026-09-11.

    Reported, so the run on a machine that can reach a model has a before to
    read against. brain/retrieval-history.md carries this run in full;
    docs/work-notes/plans/rag/03-working-note.md carries it as W-8.
    """

    def __init__(self, ranked, eligible_count, pool, breadth, noun="fragment",
                 nudged=True):
        self.count = len(ranked)
        self.eligible = eligible_count
        self.pool = pool
        self.breadth = breadth
        # WHAT IT IS COUNTING. The first version said "fragment" whatever it
        # had been given, so the document side reported "5 fragment(s) were
        # eligible" about five clauses - a sentence that is wrong in the one
        # word a reader uses to tell the two corpora apart.
        self.noun = noun
        # WHETHER A QUALITY NUDGE COULD HAVE SET THIS ORDER. It can on the
        # fragment side, where status says how far a piece of code has been
        # proved. It cannot on the document side, because documents get no
        # nudge - a DRAFT clause is not a worse answer than a REVIEWED one,
        # it is an unread one. Saying otherwise would explain a tie with a
        # mechanism that is not running.
        self.nudged = nudged

        scores = [c.score for c in ranked]
        self.top_gap = ((scores[0] - scores[1]) / ONE_RANK
                        if len(scores) > 1 else None)
        self.spread = ((scores[0] - scores[-1]) / ONE_RANK
                       if len(scores) > 1 else 0.0)
        self.agreed = len([c for c in ranked
                           if c.keyword_rank is not None
                           and c.vector_rank is not None])

        words = [c.keyword_score for c in ranked if c.keyword_score is not None]
        near = [c.vector_score for c in ranked if c.vector_score is not None]
        self.best_words = min(words) if words else None   # bm25: lower is better
        self.best_near = max(near) if near else None

    @property
    def pool_is_evidence(self):
        """Whether "both agree" means anything yet.

        MEASURED LIMITATION, and it predates this class - it was a comment in
        find(). The nearness route returns a similarity for EVERY eligible
        fragment rather than a shortlist, so while the library is smaller than
        the pool, every candidate is found by both routes and "both agree" is
        true of everything, including a question about cats.
        """
        return self.eligible > self.pool

    @property
    def words_selected_nothing(self):
        """The words route matched at least as much as the filter left.

        Not a threshold - a comparison of two counts. When it is true the
        route ranked the library rather than choosing from it, and a position
        in that ranking says nothing about having a claim on the sentence.
        """
        return self.breadth >= self.eligible

    def sentence(self):
        """The same measurement in words, because words are the contract.

        S-3 in 00-structure.md: a number and a sentence, and the sentence is
        what survives. This file already spoke this way - "both agree" - and
        this extends that vocabulary rather than starting a second one.
        """
        if self.count < 2:
            return ("one candidate, so nothing was contested - a shortlist of "
                    "one is not a ranking")

        if self.top_gap < 1.0 and self.nudged:
            said = ("A COIN TOSS: the top two are %.1f of one fusion rank "
                    "apart, which is narrower than the quality nudge - their "
                    "order could have come from status alone, not from either "
                    "route preferring one" % self.top_gap)
        elif self.top_gap < 1.0:
            said = ("A COIN TOSS: the top two are %.1f of one fusion rank "
                    "apart, and one rank is the smallest difference either "
                    "route can express - so their order carries no preference "
                    "at all" % self.top_gap)
        else:
            said = ("the winner is %.1f rank(s) clear of the runner-up"
                    % self.top_gap)

        said += ("; the shortlist spans %.1f rank(s); %d of %d found by both "
                 "routes" % (self.spread, self.agreed, self.count))

        if not self.pool_is_evidence:
            said += (". Only %d %s(s) were eligible, no more than the pool "
                     "of %d - every one of them is found by both routes, so "
                     "'both agree' means nothing here yet"
                     % (self.eligible, self.noun, self.pool))
        if self.words_selected_nothing:
            said += (". The words route matched %d %s(s) - at least as "
                     "many as the %d the filter left - so it ranked the "
                     "library rather than selecting from it"
                     % (self.breadth, self.noun, self.eligible))
        return said

    def __repr__(self):
        return ("<contest gap=%s spread=%.1f agreed=%d/%d>"
                % ("-" if self.top_gap is None else "%.1f" % self.top_gap,
                   self.spread, self.agreed, self.count))


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
        got = candidate(hit["id"])
        got.keyword_rank = rank
        got.keyword_score = hit.get("score")
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


# ---------------------------------------------------------------------------
# The same stack, over documents
# ---------------------------------------------------------------------------

# Statuses a document may be OFFERED at. RETIRED is excluded the way
# DEPRECATED is for fragments: the row survives so a record is never
# destroyed, not so it can be handed back as an answer.
OFFERABLE_DOCUMENTS = ("DRAFT", "REVIEWED")


def documents(store, text, limit=5, pool=20):
    """The retrieval stack over ingested chunks. Returns (candidates, why).

    THE REVIT VERSION FILTER IS NOT APPLIED HERE, AND THAT IS THE ONE PLACE
    THIS COULD HAVE GONE WRONG QUIETLY. A fragment declares which releases it
    supports; a clause in QCS does not, and never will. Run documents through
    the fragment filter and EVERY DOCUMENT IS EXCLUDED for every question that
    names a release - which looks exactly like "we have nothing on that".
    docs/.../02-implementation.md s5 names this as the way this stage fails.

    So documents get their own structured filter, and today it is one rule:
    a RETIRED document is not an answer.
    """
    SEARCH.ensure_chunk_table(store)

    try:
        allowed = store.execute(
            "SELECT c.id, c.document_id, c.locator, c.heading_path, c.text, "
            "c.untrusted, d.title, d.status, d.path "
            "FROM chunks c JOIN documents d ON d.id = c.document_id "
            "WHERE d.status IN (%s)"
            % ",".join("?" * len(OFFERABLE_DOCUMENTS)),
            OFFERABLE_DOCUMENTS).fetchall()
    except sqlite3.OperationalError:
        return [], 0                     # nothing has ever been ingested here

    if not allowed:
        return [], 0

    keep = set(row["id"] for row in allowed)
    by_id = dict((row["id"], row) for row in allowed)
    candidates = {}

    def candidate(chunk_id):
        if chunk_id not in candidates:
            candidates[chunk_id] = Candidate(chunk_id, by_id[chunk_id])
        return candidates[chunk_id]

    rank = 0
    for hit in SEARCH.chunk_keywords(store, text, limit=pool * 3):
        if hit["id"] not in keep:
            continue
        rank += 1
        got = candidate(hit["id"])
        got.keyword_rank = rank
        got.keyword_score = hit.get("score")
        if rank >= pool:
            break

    rank = 0
    for chunk_id, score in EMBED.nearest(store, text, limit=pool * 3,
                                         kind=EMBED.CHUNK):
        if chunk_id not in keep:
            continue
        rank += 1
        got = candidate(chunk_id)
        got.vector_rank = rank
        got.vector_score = score
        if rank >= pool:
            break

    backend_name, _why = EMBED.backend()
    vector_weight = VECTOR_WEIGHT_BY_BACKEND.get(backend_name, 0.6)
    for got in candidates.values():
        if got.keyword_rank is not None:
            got.fused += KEYWORD_WEIGHT / (RRF_K + got.keyword_rank)
        if got.vector_rank is not None:
            got.fused += vector_weight / (RRF_K + got.vector_rank)
        # NO QUALITY NUDGE. The fragment nudge reads a fragment's status,
        # which says how far a piece of Heron's own code has been proved. A
        # document's status is a lifecycle, not a grade - a DRAFT clause is
        # not a worse answer than a REVIEWED one, it is an unread one. Reusing
        # the nudge here would be borrowing a number that means something
        # else, which is how a ranking stops being explainable.

    ranked = sorted(candidates.values(),
                    key=lambda c: (-c.score,
                                   c.keyword_rank if c.keyword_rank else 999,
                                   c.id))
    return ranked[:limit], len(allowed)


def find_documents(store, text, limit=5):
    """What a caller asks when it wants a clause rather than a fragment.

    SEPARATE FROM find(), AND NOT MERGED INTO IT. R-20 asks that documents be
    retrievable ALONGSIDE fragments and that a result say which kind each hit
    is. Two labelled answers do that; one fused list does not, and it would
    also invent a comparison the numbers cannot carry - reciprocal rank
    fusion produces the same score for "first of seven chunks" and "first of
    four hundred fragments". Contest already records that fusion keeps order
    and discards strength; fusing across two corpora is that defect on
    purpose.

    It is the same argument R-38 makes about scopes one level up: when two
    things must not be pooled, the answer is two queries and two labelled
    answers, never one merged one.
    """
    SEARCH.ensure_chunk_table(store)

    # R-19, AND THE THREE NOTHINGS ARE THREE DIFFERENT SENTENCES.
    #
    # This exact defect was found on the fragment side on 2026-08-30 BY
    # MEASURING: a query against an empty store printed the same words as a
    # genuine miss, so a number recorded from that run would have been a
    # measurement of an empty database. It is already known. It is not being
    # rediscovered here.
    try:
        total = store.execute("SELECT COUNT(*) AS n FROM documents").fetchone()["n"]
    except sqlite3.OperationalError:
        total = 0
    if not total:
        return SEARCH.Answer(
            "empty",
            note="NO DOCUMENT IS INDEXED in this scope - this is not a search "
                 "result and it is not 'nothing matched'. Nothing has been "
                 "put in yet:  python brain/heron_ingest.py <file>")

    # DOCUMENTS INGESTED BUT NOT INDEXED IS A THIRD STATE, and reporting it
    # as "nothing matched" is the R-19 defect one level down: the chunks are
    # there, the searchable text is not, and a miss and an unbuilt index read
    # identically from the outside. Found by a test that ingested and forgot
    # to index - which is exactly how a caller will meet it.
    indexed = store.execute("SELECT COUNT(*) AS n FROM chunk_text").fetchone()["n"]
    if not indexed:
        return SEARCH.Answer(
            "unindexed",
            note="%d document(s) are ingested and NOT INDEXED - this is not a "
                 "search result either. The searchable text is derived and "
                 "has not been built:  heron_search.index_chunks(store) and "
                 "heron_embed.index_chunks(store)" % total)

    ranked, eligible_chunks = documents(store, text, limit=limit)
    if not ranked:
        retired = store.execute(
            "SELECT COUNT(*) AS n FROM documents WHERE status = 'RETIRED'"
        ).fetchone()["n"]
        note = "nothing in the indexed documents matched"
        if retired and retired == total:
            note = ("%d document(s) are indexed and EVERY ONE IS RETIRED - "
                    "they exist, they are just not offered as answers"
                    % retired)
        elif retired:
            note += ". %d retired document(s) were not searched" % retired
        return SEARCH.Answer("nothing", note=note, candidates=[])

    best = ranked[0]
    note = "%d chunk(s); best found by %s" % (len(ranked), best.why())
    contest = Contest(ranked, eligible_chunks, 20,
                      SEARCH.chunk_breadth(store, text), noun="chunk",
                      nudged=False)
    note += ". " + contest.sentence()

    return SEARCH.Answer(
        "documents", best.id,
        candidates=[{"id": c.id, "kind": "chunk",
                     "document": c.row["title"],
                     "locator": c.row["locator"],
                     "heading_path": c.row["heading_path"],
                     "status": c.row["status"],
                     "untrusted": c.row["untrusted"],
                     "score": c.score, "why": c.why(),
                     "words_score": c.keyword_score,
                     "nearness_score": c.vector_score} for c in ranked],
        note=note, contest=contest)


# ---------------------------------------------------------------------------
# The Librarian - which scope, and the trap in the question
# ---------------------------------------------------------------------------

class Asked(object):
    """One scope, asked on its own, with its own answer. Never merged."""

    def __init__(self, scope, project=None, answer=None, skipped=None):
        self.scope = scope
        self.project = project
        self.answer = answer
        self.skipped = skipped        # why it was not asked, or None

    @property
    def label(self):
        if self.project:
            return "%s (%s)" % (self.scope, self.project)
        return self.scope

    def __repr__(self):
        return "<asked %s %s>" % (self.label,
                                  self.skipped or (self.answer.route
                                                   if self.answer else "-"))


def librarian(text, scopes=None, project=None, limit=5):
    """Ask each scope SEPARATELY. Returns one Asked per scope, labelled.

    THE TRAP IN THIS WHOLE TRACK IS IN THE NAME OF THIS FUNCTION.
    "An agent that decides which scopes to search" reads as "an agent that
    searches several", and implemented that way it is a UNION - one client's
    knowledge in the same result set as another's. D-33 calls that a
    CONTRACTUAL problem rather than a technical one, and Golden Rule 5 exists
    to make it impossible to write.

    So the rule, written down before any of this was coded:

        The Librarian decides WHICH ONE. If it needs two, it makes TWO
        SEPARATE QUERIES and says which answer came from where. It never
        merges them, and CrossScopeRefused stays exactly as it is.

    THERE IS NO ARGUMENT HERE THAT COULD TAKE A MERGED QUERY, and that is the
    design rather than an omission. Each scope is opened as its own Store,
    asked its own question, and its answer is returned under its own label.
    Nothing compares two scopes' scores, because a score from one store and a
    score from another are not the same measurement - the same reason
    find_documents() is separate from find().

    AND IT DOES NOT CHOOSE (R-62, D-01). It reports what each scope holds.
    Deciding what the user meant, and which answer to use, is the host's act -
    which is S-1's "hand it back", taken as the plan's stated default.

    In Revit terms: you can open two models side by side. You do not copy one
    into the other to compare them.
    """
    wanted = list(scopes or [SCOPE.GLOBAL])
    out = []
    for scope in wanted:
        if scope not in SCOPE.SCOPES:
            out.append(Asked(scope, skipped="not a knowledge scope"))
            continue

        if scope == SCOPE.PROJECT and not project:
            # D-33, and heron_scope already refuses this - the refusal is
            # called rather than re-implemented. A guess here writes one
            # client's knowledge into another's file.
            out.append(Asked(scope, skipped=(
                "no project is identified, so the project store is not opened "
                "- guessing which project a question belongs to is how one "
                "client's knowledge reaches another")))
            continue

        # THE PROJECT KEY LABELS THE PROJECT SCOPE AND NOTHING ELSE. It was
        # attached to every answer at first, so a company answer came back
        # labelled "company (Tower B)" - which reads as this being Tower B's
        # copy of the company standard. It is not: it is the company's one
        # store, and mislabelling whose knowledge something is, is the exact
        # confusion Golden Rule 5 exists to prevent.
        label = project if scope == SCOPE.PROJECT else None

        try:
            store = SCOPE.open_scope(scope, project)
        except Exception as why:
            out.append(Asked(scope, project=label,
                             skipped="could not be opened: %s" % why))
            continue

        try:
            SEARCH.ensure_chunk_table(store)
            out.append(Asked(scope, project=label,
                             answer=find_documents(store, text, limit=limit)))
        finally:
            store.close()
    return out


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

    # ONE MEASUREMENT, NOT TWO. The small-pool caveat used to be built here,
    # in an `elif`, so it was only ever said when the best candidate happened
    # to be found by both routes - the limitation is true either way. It now
    # lives in Contest with the rest of the numbers, because 00-structure.md
    # s3.7's rule is that this measurement is built ONCE: three copies of it
    # become three numbers that disagree.
    contest = Contest(ranked, len(allowed), pool,
                      SEARCH.match_breadth(store, text))
    note += ". " + contest.sentence()

    return SEARCH.Answer(
        "hybrid", best.id,
        candidates=[{"id": c.id, "capability": c.row["capability"],
                     "status": c.row["status"], "score": c.score,
                     "why": c.why(), "words_score": c.keyword_score,
                     "nearness_score": c.vector_score} for c in ranked],
        note=note, contest=contest)


def main(argv):
    revit = None
    if "--revit" in argv:
        i = argv.index("--revit")
        revit = argv[i + 1]
        argv = argv[:i] + argv[i + 2:]

    # An unknown flag used to become part of the QUESTION. "--rebuild" - a flag
    # this tool does not have - was silently searched for, matched nothing, and
    # printed "nothing matched", which reads as a measured result rather than a
    # typo. Anything starting with "-" is now refused by name. (2026-08-30)
    unknown = [a for a in argv if a.startswith("-")]
    if unknown:
        print("  not a flag this tool has: %s" % " ".join(unknown))
        print('  python brain/heron_retrieve.py "show me every duct" --revit 2024')
        print("  the store is filled by brain/heron_scope.py, not from here")
        return 2

    if not argv:
        print('  python brain/heron_retrieve.py "show me every duct" --revit 2024')
        return 2

    store = SCOPE.open_scope(SCOPE.GLOBAL)
    try:
        SEARCH.index(store)
        EMBED.index(store)
        # The document side of the same two indexes. Derived, rebuilt on
        # demand, and free when nothing changed - the same contract, so a
        # scope with no documents pays nothing and says nothing.
        SEARCH.index_chunks(store)
        EMBED.index_chunks(store)

        # An EMPTY store is not a retrieval result, and must never be printed as
        # one. On a fresh machine the store has no fragments in it yet, and this
        # tool used to answer "nothing matched" - indistinguishable from a real
        # measurement of a real miss. A retrieval history built out of that is
        # exactly the drift retrieval-history.md exists to prevent. (2026-08-30)
        if store.count() == 0:
            print("  the knowledge store is EMPTY - this is not a retrieval result.")
            print("  nothing has been indexed, so no question can be answered yet.")
            print("  fill it first:  python brain/heron_scope.py --rebuild")
            return 2
        text = " ".join(argv)
        answer = find(store, text, revit=revit)

        print("Asked:  %s%s" % (text, "   (Revit %s)" % revit if revit else ""))
        print("Route:  %s" % answer.route)
        print("Best:   %s" % (answer.fragment_id or "nothing"))
        print("        %s" % answer.note)
        for c in answer.candidates:
            print("          %-14s %-30s %-11s %.4f  %s"
                  % (c["id"], c["capability"], c["status"], c["score"], c["why"]))

        # THE MAGNITUDES, printed because fusion discards them.
        #
        # The line above is fused score, which is built from POSITIONS. These
        # two are the routes' own opinions of how well they matched, and they
        # are the only numbers on this page a floor could ever be derived from
        # (R-60). Printed so a person can look at them across many questions
        # before anybody sets one. Nothing ranks on them today.
        k = answer.contest
        if k is not None:
            print("Numbers: %s"
                  % ("words matched %d of %d eligible; best bm25 %s; "
                     "best nearness %s"
                     % (k.breadth, k.eligible,
                        "-" if k.best_words is None else "%.4f" % k.best_words,
                        "-" if k.best_near is None else "%.4f" % k.best_near)))

        _rows, excluded = eligible(store, revit)
        for e in excluded:
            print("  excluded  %-14s %s" % (e.id, e.reason))

        # DOCUMENTS, ALONGSIDE FRAGMENTS AND LABELLED AS A DIFFERENT KIND.
        #
        # R-20. They are printed as their own section rather than mixed into
        # the list above, because a fragment and a clause are not
        # interchangeable: one is a piece of code Heron can run, the other is
        # a sentence somebody has to read. A shortlist that mixes them
        # without saying so is worse than either alone.
        papers = find_documents(store, text)
        print()
        print("Documents:")
        if papers.route in ("empty", "nothing"):
            print("        %s" % papers.note)
        else:
            print("        %s" % papers.note)
            for c in papers.candidates:
                print("          %-9s %-22s %.4f  %s"
                      % (c["locator"] or "-", (c["document"] or "")[:22],
                         c["score"], c["why"]))
                print("            %s" % (c["heading_path"] or "")[:78])
            # GOLDEN RULE 19, said at the point a person reads the text.
            # Every one of these came out of a file this project did not
            # write. It is quoted content, never direction.
            print("        quoted from an ingested document - content, never "
                  "instruction (GR 19)")
        return 0
    finally:
        store.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
