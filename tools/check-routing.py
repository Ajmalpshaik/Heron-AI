# Heron-Agent:  HERON-RAG-RNK-006
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Can each fragment still be found by its OWN declared words?

    python tools/check-routing.py
    python tools/check-routing.py --revit 2024

WHAT THIS ASKS, AND WHY IT IS THE ONE RETRIEVAL QUESTION WORTH AUTOMATING
------------------------------------------------------------------------
Every fragment declares `utterances` - the sentences somebody would say to want
it. This asks each of those sentences back to the search and checks the fragment
that declared it comes back first.

It is a LOWER BOUND and it must be read as one. A fragment's own phrasing shares
vocabulary with its own indexed text, so passing proves very little. FAILING
proves something real: another fragment now outranks it for the words it claimed,
which means a request phrased that way lands somewhere else.

WHY IT EXISTS
-------------
Adding a fragment can make an EXISTING one unreachable, silently, and nothing
else in this repository would notice. It happened twice while the library was
being written and neither was visible at the time:

  * `isolate-elements` declared "show me just these", and the tracked retrieval
    query begins "show me". The duct filter dropped from 3rd to 5th.
  * `create-duct` and `set-mep-size` declared "duct" phrasings of their own, and
    it dropped again, out of the top five entirely.

Neither fragment was wrong to claim its words. That is the point: this is not a
list of defects, it is a list of PLACES TWO FRAGMENTS WANT THE SAME SENTENCE,
and a human has to decide which should win - or whether the sentence names a
composition, in which case it belongs to a SKILL and to neither fragment.

ONE CLASS OF COLLISION IS NOT A JUDGEMENT CALL
----------------------------------------------
Everything above treats every contest alike, and for a long time this tool did
too. It is wrong about one of them. When the sentence a READ fragment claims is
answered by a fragment that WRITES, the failure mode is not "the user gets the
wrong table" - it is "the user asked a question and the model changed". That is
the risk ladder crossing in the one direction that cannot be undone by reading
the answer again, and it deserves to be separated from two report fragments
squabbling over "show me the sizes".

So contests are now split. The plain list stays a judgement call and stays
exit 0. The crossing list is printed on its own, above it, with the two risk
levels named - because a person scanning thirty rows for a problem will not
spot that one of them routes a question into a MODIFY.

WHY IT IS NOT A GATE
--------------------
It exits 0 whatever it finds. A collision is a judgement, not a defect, and a
tool that failed a build over "two fragments both answer to 'grey the
background'" would be teaching people to weaken their own utterances to buy a
number. That is the one response ruled out in brain/retrieval-history.md: taking
"show me just these" away from the isolate fragment would make the isolate
unfindable in order to protect a measurement.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FRAGMENTS = os.path.join(ROOT, "brain", "fragments")


def risk_of(store, fragment_id):
    """The declared risk of one fragment, or None if it is not in the store.

    Read from the scope rather than from disk: the store is what retrieval
    ranked, so a fragment edited but not re-indexed must be compared as the
    search actually saw it, not as the file now reads.
    """
    for row in store.fragments():
        if row["id"] == fragment_id:
            return row.get("risk")
    return None


def ids_on_disk():
    """The id of every fragment in this working tree.

    The IDS and not the COUNT - see the staleness check in main() for the run
    that made the difference matter.
    """
    try:
        import yaml
    except ImportError:
        sys.stderr.write("This needs PyYAML: pip install --user pyyaml\n")
        raise SystemExit(2)

    found = set()
    for name in sorted(os.listdir(FRAGMENTS)):
        path = os.path.join(FRAGMENTS, name, "fragment.yaml")
        if not os.path.exists(path):
            continue
        with open(path, encoding="utf-8") as fh:
            doc = yaml.safe_load(fh)
        if doc and doc.get("id"):
            found.add(doc["id"])
    return found


def describe_drift(store_ids, disk_ids, limit=4):
    """How the store and the working tree disagree, in words.

    "held 226 of 226" was the old message for a store holding a completely
    different library, which is why this names the fragments instead.
    """
    missing = sorted(disk_ids - store_ids)
    unexpected = sorted(store_ids - disk_ids)

    def few(ids):
        shown = ", ".join(ids[:limit])
        return shown + (", and %d more" % (len(ids) - limit) if len(ids) > limit else "")

    parts = []
    if missing:
        parts.append("%d on disk the store does not hold (%s)"
                     % (len(missing), few(missing)))
    if unexpected:
        parts.append("%d in the store that are not on disk (%s)"
                     % (len(unexpected), few(unexpected)))
    return "; ".join(parts) if parts else "the same ids in a different order"


def utterances():
    """(fragment id, sentence) for every declared utterance."""
    try:
        import yaml
    except ImportError:
        sys.stderr.write("This needs PyYAML: pip install --user pyyaml\n")
        raise SystemExit(2)

    out = []
    risk = {}
    for name in sorted(os.listdir(FRAGMENTS)):
        path = os.path.join(FRAGMENTS, name, "fragment.yaml")
        if not os.path.exists(path):
            continue
        with open(path, encoding="utf-8") as fh:
            doc = yaml.safe_load(fh)
        risk[doc["id"]] = doc.get("risk") or "?"
        for said in doc.get("utterances") or []:
            out.append((doc["id"], said))
    return out, risk


# The ladder from the Constitution, lowest first. Position is what matters:
# a contest MATTERS when the winner sits higher than the loser, and matters
# most when the loser is READ - somebody asked a question.
LADDER = ["READ", "ANALYZE", "SUGGEST", "EXECUTE", "MODIFY", "PUBLISH", "ADMIN"]


def rung(level):
    """Where a risk level sits on the ladder; -1 for anything unrecognised."""
    return LADDER.index(level) if level in LADDER else -1


def main(argv):
    revit = None
    if "--revit" in argv:
        i = argv.index("--revit")
        revit = argv[i + 1]

    import heron_scope as SCOPE
    import heron_search as SEARCH
    import heron_embed as EMBED
    import heron_retrieve as RETRIEVE

    # The stores are DERIVED (Golden Rule 11), so an empty one is a fresh machine
    # rather than damage, and a checker should run on a fresh machine without a
    # setup step. Rebuild rather than refuse - but rebuild EXPLICITLY, because
    # the one thing this must never do is print a routing result computed over
    # an empty library.
    # STALE counts as empty here, and that distinction cost a real run. A store
    # that simply has not seen the fragments added since it was last built
    # reports every one of them as rank #None - not "ranked badly", ABSENT - and
    # that reads as a routing catastrophe when nothing is wrong at all. It is
    # the same failure the empty case guards against, one step milder, and the
    # comment above already states the principle: never print a routing result
    # computed over a library this store does not actually hold.
    #
    # THE TEST IS THE IDS AND NOT THE COUNT, and that cost a run too. This
    # compared store.count() to the number of folders on disk until 2026-09-06.
    # One store at %APPDATA%\Heron\knowledge serves every checkout on the
    # machine, and three sessions building fragments in parallel (HANDOVER 9b)
    # each had a working tree of exactly 226 - so the count MATCHED while the
    # store held another session's library, no rebuild was triggered, and the
    # tool reported confidently on fragments that were not the ones on disk. It
    # printed "claimed and not reached: 14" on one run and "7 questions answered
    # by a writer" on another. Both were artefacts of the wrong library, and
    # both look exactly like a real result - which is the whole danger. A COUNT
    # IS NOT AN IDENTITY.
    disk_ids = ids_on_disk()
    store = SCOPE.open_scope(SCOPE.GLOBAL)
    store_ids = set(row["id"] for row in store.fragments())
    if store_ids != disk_ids:
        drift = describe_drift(store_ids, disk_ids)
        store.close()
        built, problems = SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        store_ids = set(row["id"] for row in store.fragments())
        print("  (store did not match this working tree - %s; rebuilt %d%s)"
              % (drift, built,
                 "; %d problem(s)" % len(problems) if problems else ""))
        if store.count() == 0:
            store.close()
            print("  the store is STILL empty after a rebuild - nothing to route")
            print("  against, and this is not a routing result. Check brain/fragments/.")
            return 2
        if store_ids != disk_ids:
            # A rebuild that does not reconcile them means something is wrong
            # with the library itself - a fragment that will not load, most
            # likely. Refusing is the same principle as the empty case: the
            # numbers below would be computed over a library this store does
            # not hold, and they would look perfectly normal.
            print("  the store STILL does not match after a rebuild - %s."
                  % describe_drift(store_ids, disk_ids))
            print("  This is not a routing result. Run `python brain/heron_fragment.py`.")
            store.close()
            return 2

    try:
        SEARCH.index(store)
        EMBED.index(store)

        rows, risk = utterances()
        if not rows:
            print("  no fragment declares an utterance - nothing to check")
            return 0

        size = store.count()
        words_first = words_top3 = near_first = near_top3 = 0
        taken = []

        for fid, said in rows:
            by_word = [h["id"] for h in SEARCH.keywords(store, said, limit=size)]
            by_near = [i for i, _s in EMBED.nearest(store, said, limit=size)]

            wr = by_word.index(fid) + 1 if fid in by_word else None
            nr = by_near.index(fid) + 1 if fid in by_near else None

            if wr == 1:
                words_first += 1
            if wr is not None and wr <= 3:
                words_top3 += 1
            if nr == 1:
                near_first += 1
            if nr is not None and nr <= 3:
                near_top3 += 1

            if wr != 1:
                taken.append((said, fid, wr, by_word[0] if by_word else "-"))

        total = len(rows)
        backend, _why = EMBED.backend()

        print("Fragments: %d   utterances: %d   backend: %s%s"
              % (size, total, backend, "   Revit %s" % revit if revit else ""))
        print()
        print("  by words     #1  %3d of %d  (%.0f%%)      top 3  %3d  (%.0f%%)"
              % (words_first, total, 100.0 * words_first / total,
                 words_top3, 100.0 * words_top3 / total))
        print("  by nearness  #1  %3d of %d  (%.0f%%)      top 3  %3d  (%.0f%%)"
              % (near_first, total, 100.0 * near_first / total,
                 near_top3, 100.0 * near_top3 / total))

        # A contest where the winner sits HIGHER on the risk ladder than the
        # fragment that claimed the sentence. The loser being READ is the case
        # that matters: a question routed into something that writes.
        crossing = [row for row in taken
                    if rung(risk.get(row[3], "?")) > rung(risk.get(row[1], "?"))
                    and rung(risk.get(row[1], "?")) >= 0]

        if not taken:
            print()
            print("Every fragment is ranked first for every sentence it claims.")
            print("That is a LOWER BOUND passing - a fragment's own words share")
            print("vocabulary with its own indexed text, so this proves the")
            print("library has no COLLISIONS, never that retrieval is good.")
            return 0

        # THE DANGEROUS SUBSET - a question the HOST would answer with a
        # fragment that CHANGES THE MODEL.
        #
        # Every collision above is a judgement, and most are harmless: two read
        # fragments arguing produce a slightly worse answer. This subset is
        # different in kind - a caller acting on the answer does not get a poor
        # reply to its question, it modifies the model in reply to one.
        #
        # IT CALLS `find`, WHICH IS WHAT THE HOST CALLS - AND IT TOOK TWO GOES
        # TO GET THAT RIGHT, WHICH IS THE POINT WORTH KEEPING.
        #
        # Written 2026-08-31 against `taken`, the KEYWORD ranking. It reported
        # sentences as answered by a writer when the host answered them
        # correctly - a claim about a route no caller uses.
        #
        # Corrected to `retrieve`, the fused stage. Still wrong, and less
        # obviously so: `retrieve` is fusion ALONE, while the host goes through
        # `find`, which tries the identity and cache short circuits FIRST. A
        # sentence that is a fragment's own declared utterance is answered by
        # identity and never reaches fusion at all - so the fused ranking said
        # "a writer wins" for a sentence the host resolves exactly right.
        #
        # THE RULE THIS LEAVES: a check that makes a claim about CONSEQUENCE
        # must call the same entry point the system calls, not the stage that
        # looks like it. The section above is a per-route diagnostic and says
        # so; this one says what would actually happen, so it has to ask the
        # thing that actually happens.
        risky = []
        for said, fid, _rank, _winner in taken:
            if risk_of(store, fid) != "READ":
                continue

            answer = RETRIEVE.find(store, said, limit=1)
            served = getattr(answer, "fragment_id", None)
            if not served or served == fid:
                continue                    # the host answers correctly
            if risk_of(store, served) in ("READ", None):
                continue

            risky.append((said, fid, "served", served))

        print()
        if not risky:
            print("NO QUESTION IS ANSWERED BY SOMETHING THAT WRITES - and read")
            print("what that green is worth before trusting it.")
            print()
            print("  This checks each fragment's OWN DECLARED utterances, and")
            print("  those are exactly the sentences `find` answers by IDENTITY -")
            print("  an exact declared phrasing is resolved before any ranking")
            print("  runs. So while the identity route holds, this section is")
            print("  empty BY CONSTRUCTION, and its emptiness says the identity")
            print("  route works - not that no question can reach a writer.")
            print()
            print("  THE REAL RISK SURFACE IS PARAPHRASE, which no fragment")
            print("  declares and this corpus therefore does not contain. What")
            print("  this does still catch: a declared question that stops being")
            print("  matched by identity, or two fragments declaring one sentence")
            print("  where the survivor writes.")
        else:
            print("A QUESTION ANSWERED BY SOMETHING THAT WRITES (%d):" % len(risky))
            print()
            for said, fid, _how, winner in risky:
                print("  %-44s %s (READ) loses; %s is SERVED"
                      % ('"' + said + '"', fid, winner))
            print()
            print("  Measured through `find` - the same entry point the host")
            print("  calls, identity and cache first. A caller acting on this")
            print("  answer does not get a poor reply to its question; it")
            print("  CHANGES THE MODEL in reply to one. Fix the read fragment's")
            print("  reach or the writer's wording; never leave it because the")
            print("  rank looks close.")

        # ------------------------------------------------------------------
        # A ROUTING TABLE IS A COMMENT, AND COMMENTS ARE NOT INDEXED.
        #
        # Added 2026-09-01, after OVERRIDE_GRAPHICS_IN_VIEW was found claiming
        # "make these red" in its routing table and never declaring it - so the
        # sentence resolved to GROUP_ELEMENTS. heron_search indexes
        # semantic-identity, the utterances, the capability, the domain and the
        # purpose. A table saying "-> here" records a DECISION that retrieval
        # cannot act on.
        #
        # The audit found 69 such claims across half the library, 23 of them
        # reaching the wrong fragment - including "zoom to these" building an
        # MEP fitting and "write that up" reaching a bulk parameter WRITE.
        #
        # Measured through `find`, the entry point the host calls, for the same
        # reason the risk section is: a claim about consequence must ask the
        # thing that actually happens.
        #
        # A claim on TWO tables is a different fault and is reported as one: two
        # comments disagreeing is invisible to every other check here.
        claimed = re.compile(r'^#\s+"([^"]+)"\s*->\s*here\b', re.M)
        unheard = []
        claims = {}

        for name in sorted(os.listdir(FRAGMENTS)):
            path = os.path.join(FRAGMENTS, name, "fragment.yaml")
            if not os.path.exists(path):
                continue
            with open(path, encoding="utf-8") as fh:
                text = fh.read()

            import yaml
            doc = yaml.safe_load(text)
            fid = doc.get("id")
            spoken = set((u or "").strip().lower() for u in (doc.get("utterances") or []))

            for match in claimed.finditer(text):
                sentence = match.group(1).strip()
                claims.setdefault(sentence.lower(), set()).add(fid)

                if sentence.lower() in spoken:
                    continue

                answer = RETRIEVE.find(store, sentence, limit=1)
                served = getattr(answer, "fragment_id", None)
                if served == fid:
                    continue

                unheard.append((sentence, fid, served or "-"))

        contested = sorted((s, ids) for s, ids in claims.items() if len(ids) > 1)

        print()
        if not unheard and not contested:
            print("EVERY SENTENCE A ROUTING TABLE CLAIMS ACTUALLY REACHES IT.")
            print()
            print("  A routing table is a COMMENT and the index does not read")
            print("  one - it reads semantic-identity, the utterances, the")
            print("  capability, the domain and the purpose. So a table saying")
            print("  \"-> here\" is a decision retrieval cannot act on unless the")
            print("  sentence is ALSO an utterance, or something else carries it")
            print("  there. This says one of those is true for all of them.")
        else:
            if unheard:
                print("CLAIMED IN A ROUTING TABLE AND NOT REACHED (%d):" % len(unheard))
                print()
                for sentence, fid, served in unheard:
                    print("  %-44s %s claims it; %s is SERVED"
                          % ('"' + sentence + '"', fid, served))
                print()
                print("  The table records a decision; the index never saw it.")
                print("  Either declare the sentence as an utterance, or change")
                print("  the table - but do not leave the two disagreeing.")
            if contested:
                print()
                print("ONE SENTENCE CLAIMED BY TWO TABLES (%d):" % len(contested))
                print()
                for sentence, ids in contested:
                    print("  %-44s %s" % ('"' + sentence + '"', ", ".join(sorted(set(ids)))))
                print()
                print("  Two comments disagreeing, which no other check here can")
                print("  see. Decide which fragment owns it and drop the claim")
                print("  from the other.")

        print()
        print("SENTENCES TWO FRAGMENTS BOTH WANT (%d of %d):" % (len(taken), total))
        print()
        for said, fid, rank, winner in taken:
            print("  %-44s %s is #%-4s  %s answers instead"
                  % ('"' + said + '"', fid, rank, winner))
        print()
        print("MEASURED ON THE KEYWORD ROUTE ALONE - read that before acting.")
        print("`taken` is built from SEARCH.keywords, which is ONE HALF of the")
        print("fusion and is not what the host calls. `find` resolves an exact")
        print("declared phrasing by IDENTITY before any ranking runs, so a line")
        print("here can be a sentence the host already answers correctly.")
        print()
        print("  Check one before fixing it:")
        print("    heron_brain.lookup(\"the sentence\")  -> what the host does")
        print()
        print("  Written 2026-09-02, after this section misled two consecutive")
        print("  sessions into editing fragments to chase ranks that `find` was")
        print("  already getting right. The section above about writers says it")
        print("  measures through `find` and says so loudly; this one did not")
        print("  say anything, and looked like a defect list.")
        print()
        print("None of these is automatically a defect, and the exit code says so.")
        print("Three things one can be, and they need different answers:")
        print()
        print("  * the OTHER fragment is genuinely the better answer to that")
        print("    sentence - in which case the loser's utterance is wrong")
        print("  * the sentence names a COMPOSITION (filter then act), which no")
        print("    fragment can win and a SKILL should claim")
        print("  * a genuine collision, where one of the two must be reworded")
        print()
        print("What is NEVER the answer: weakening an utterance that is exactly")
        print("what somebody says, to buy back a rank. See brain/retrieval-history.md.")
        return 0
    finally:
        store.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
