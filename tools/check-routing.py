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
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FRAGMENTS = os.path.join(ROOT, "brain", "fragments")


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
    on_disk = len([
        name for name in os.listdir(FRAGMENTS)
        if os.path.exists(os.path.join(FRAGMENTS, name, "fragment.yaml"))
    ])
    store = SCOPE.open_scope(SCOPE.GLOBAL)
    if store.count() != on_disk:
        was = store.count()
        store.close()
        built, problems = SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        print("  (store held %d of %d fragment(s) on disk - rebuilt %d%s)"
              % (was, on_disk, built,
                 "; %d problem(s)" % len(problems) if problems else ""))
        if store.count() == 0:
            store.close()
            print("  the store is STILL empty after a rebuild - nothing to route")
            print("  against, and this is not a routing result. Check brain/fragments/.")
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

        if crossing:
            print()
            print("*** %d SENTENCE(S) ROUTE UP THE RISK LADDER ***" % len(crossing))
            print()
            print("The fragment that claimed each of these is LOWER risk than the")
            print("one answering it. Where the claimant is READ, somebody asks a")
            print("question and reaches something that changes the model - which")
            print("is not the same kind of problem as two reports colliding, and")
            print("is why these are listed apart rather than buried below.")
            print()
            for said, fid, rank, winner in crossing:
                print("  %-40s %s %-7s is #%-4s  ->  %s %s answers"
                      % ('"' + said + '"', fid, risk.get(fid, "?"), rank,
                         winner, risk.get(winner, "?")))
            print()
            print("Neither fragment is necessarily wrong. What is NOT available")
            print("here is leaving it alone unexamined: say which should win, or")
            print("say that the sentence names a composition and belongs to a")
            print("skill. Silence on one of these is the model changing on a")
            print("question, which is this project's defining failure shape.")

        print()
        print("SENTENCES TWO FRAGMENTS BOTH WANT (%d of %d):" % (len(taken), total))
        print()
        for said, fid, rank, winner in taken:
            print("  %-44s %s is #%-4s  %s answers instead"
                  % ('"' + said + '"', fid, rank, winner))
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
