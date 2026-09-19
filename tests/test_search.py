# Heron-Agent:  HERON-RAG-RET-003, HERON-RAG-IDX-010
# Heron-Step:   9
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Step 9 - finding a fragment by the exact words somebody typed.

    python tests/test_search.py

Runs anywhere. FTS5 ships with SQLite in the standard library.

WHAT IT PROVES
  1. The common sentence costs ONE lookup and says so - route "identity".
  2. Only a PROVEN fragment may run off that match without asking.
  3. The cache remembers a wording, counts it, and never resurrects a fragment
     that has gone.
  4. Revit's own tokens are findable - the class a meaning search handles worst.
  5. Two fragments claiming one sentence is a MISS, not a coin toss.

WHAT IT DOES NOT PROVE. Nothing about meaning. Ask in words no fragment
contains and this layer correctly finds nothing - that is Step 10's job, and
this file asserts the limit rather than papering over it.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    home = tempfile.mkdtemp(prefix="heron-search-")
    os.environ["HERON_KNOWLEDGE"] = home

    import heron_scope as SCOPE
    import heron_search as SEARCH

    try:
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            check(SEARCH.indexed_from(store) is None,
                  "a store nobody has indexed names no tree - an absent "
                  "answer, not agreement (D-52)")
            n, _ = SEARCH.index(store)
            check(n >= 2, "the scope indexes its fragments (%d)" % n)

            # WHOSE FRAGMENTS ARE IN THE STORE (FRAGMENT-ISSUES row 131). It
            # is ONE file for every checkout on the machine, so a rebuild
            # replaces what Heron knows with the opinion of whichever tree
            # asked last. index() knew both paths and recorded neither.
            import heron_fragment as FRAG
            here = os.path.abspath(FRAG.FRAGMENTS_DIR).replace(os.sep, "/")
            check(SEARCH.indexed_from(store) == here,
                  "and after indexing it names the tree it was built from")
            # IT RECORDS AND DOES NOT REFUSE. Whether a scope should be
            # per-worktree is the policy question row 131 leaves open, and a
            # library function that started refusing would settle it by
            # accident - so a second index from the same tree still works.
            again, _ = SEARCH.index(store, force=True)
            check(again == n and SEARCH.indexed_from(store) == here,
                  "a second rebuild is not refused - this names, it does not "
                  "gate")

            print()
            print("1. The common sentence costs one lookup")
            for said in ["select all ducts", "how many ducts are there",
                         "highlight them on screen"]:
                a = SEARCH.ask(store, said)
                check(a.route == "identity",
                      "%r -> identity, no search run" % said)

            a = SEARCH.ask(store, "  SELECT ALL DUCTS!!  ")
            check(a.route == "identity",
                  "case, spacing and punctuation do not make it a new question")

            print()
            print("2. Revit's own tokens are findable")
            a = SEARCH.ask(store, "OST_DuctCurves")
            check(a.fragment_id == "FRG-ELE-001",
                  "OST_DuctCurves finds the category filter - the token class "
                  "a meaning search handles worst")

            print()
            print("3. Only a PROVEN fragment runs without asking")
            # THE FIXTURE NOW SAYS WHAT IT MEANS. The claim below is that a
            # DRAFT fragment is OFFERED rather than run, and it needs a DRAFT
            # fragment to say it about. It used to get one BY ACCIDENT, from
            # whatever status FRG-ELE-001 happened to ship with - and on
            # 2026-09-13 that fragment was proved against a real model and
            # promoted, so the exact match autoran and this check failed
            # against a library that was correct. The store is a throwaway in
            # a temp directory, so this touches no fragment on disk, and both
            # claims below are unchanged.
            store.execute("UPDATE fragments SET status = 'DRAFT' "
                          "WHERE id = 'FRG-ELE-001'")
            store.db.commit()
            SEARCH.index(store)

            a = SEARCH.ask(store, "select all ducts")
            check(not a.autorun,
                  "a DRAFT fragment matching exactly is OFFERED, not run")
            check("nobody has watched it work" in a.note,
                  "and it says why in words")

            store.execute(
                "INSERT INTO fragments (id, capability, semantic_identity, kind,"
                " status, domain, risk, folder, revit) VALUES "
                "('FRG-QA-900','COUNT_THINGS','count the things','filter',"
                "'PROVEN','test','READ','x','2024')")
            store.db.commit()
            SEARCH.index(store)
            a = SEARCH.ask(store, "count the things")
            check(a.autorun, "a PROVEN fragment MAY run off an exact match")
            check(a.route == "identity", "and it took the one-lookup route")

            print()
            print("4. The cache remembers a wording, and counts it")
            odd = "gimme the ductwork innit"
            a = SEARCH.ask(store, odd)
            check(a.route in ("keywords", "nothing"),
                  "an unusual wording does not match an identity")

            SEARCH.remember(store, odd, "FRG-ELE-001", SEARCH.RAN)
            a = SEARCH.ask(store, odd)
            check(a.route == "cache", "after remembering, it takes the cache route")
            check(a.fragment_id == "FRG-ELE-001", "and returns what was learned")
            check(not a.autorun,
                  "a cached answer is a CANDIDATE - it never runs unasked")

            SEARCH.remember(store, odd, "FRG-ELE-001", SEARCH.RAN)
            _fid, hits = SEARCH.recall(store, odd)
            check(hits == 2, "and the hit count rises (%d)" % hits)

            print()
            print("  ..and a GUESS cannot be remembered at all (D-61)")
            for guess in ("keywords", "hybrid", "identity", "cache", None, ""):
                try:
                    SEARCH.remember(store, "another wording", "FRG-ELE-001",
                                    guess)
                    check(False, "%r was accepted as evidence" % (guess,))
                except SEARCH.NotEvidence:
                    pass
            check(SEARCH.recall(store, "another wording")[0] is None,
                  "none of the six non-evidence values wrote a row")

            print()
            print("  ..an EDITED fragment forgets its cached wordings")
            store.execute(
                "UPDATE utterances SET fingerprint = 'stale' "
                "WHERE utterance = ?", (SEARCH.normalise(odd),))
            store.db.commit()
            dropped = SEARCH.forget_stale(store)
            check(dropped >= 1, "forget_stale() dropped %d row(s)" % dropped)
            check(SEARCH.recall(store, odd)[0] is None,
                  "the wording confirmed against the old bytes is gone")

            SEARCH.remember(store, odd, "FRG-ELE-001", SEARCH.RAN)
            check(SEARCH.forget_stale(store) == 0,
                  "and a row written against the CURRENT bytes survives")

            print()
            print("  ..a cache never resurrects a fragment that has gone")
            store.execute("DELETE FROM fragments WHERE id = 'FRG-ELE-001'")
            store.db.commit()
            fid, _ = SEARCH.recall(store, odd)
            check(fid is None, "the cached id is dropped, not returned dangling")
            SCOPE.rebuild()
            SEARCH.index(store)

            print()
            print("5. Two fragments claiming one sentence is a MISS")
            store.execute(
                "INSERT OR REPLACE INTO fragments (id, capability, "
                "semantic_identity, kind, status, domain, risk, folder, revit) "
                "VALUES ('FRG-QA-901','ALSO_SELECT_DUCTS','select all ducts',"
                "'filter','PROVEN','test','READ','x','2024')")
            store.db.commit()
            SEARCH.index(store)
            a = SEARCH.ask(store, "select all ducts")
            check(a.route != "identity",
                  "an ambiguous sentence does not take the short circuit")
            check(not a.autorun,
                  "and above all it does NOT run one of them on a guess")

            print()
            print("6. Keyword search does the ordinary work")
            SEARCH.index(store)
            hits = SEARCH.keywords(store, "level")
            check(len(hits) >= 1, "a plain word finds candidates (%d)" % len(hits))

            a = SEARCH.ask(store, "which pipes are on level 2?")
            check(a.route == "keywords",
                  "a sentence nobody declared falls through to ranked search")
            check(a.candidates, "and it returns candidates rather than one guess")

            print()
            print("  ..punctuation is a question, not a syntax error")
            for typed in ["300x300 duct?", "what about *this*", "level-2 pipes",
                          '"quoted" thing', "a - b"]:
                broke = False
                try:
                    SEARCH.ask(store, typed)
                except Exception as exc:
                    broke = True
                    print("        %s -> %s" % (typed, exc))
                check(not broke, "%r does not blow up the query" % typed)

            print()
            print("7. The limit of this layer, asserted rather than hidden")
            a = SEARCH.ask(store, "stop the air blowing in the wrong direction")
            check(a.route in ("keywords", "nothing"),
                  "words no fragment uses do not reach an identity match")
            check(not a.autorun,
                  "and nothing runs on a weak match - that is Step 10's job")
            print()
            print("8. Asking a question does not rewrite what Heron knows")
            # ROW 136. `heron_brain._Open` calls SEARCH.index() on EVERY
            # lookup, and this function opened with DELETE FROM identities -
            # against a store that is ONE file for every checkout on the
            # machine. So a question asked from one tree silently replaced what
            # Heron knew with that tree's opinion. Measured 2026-09-19: six
            # declared phrases confirmed present BY NAME, and gone after a
            # single lookup from a tree that did not declare them.
            #
            # THE SECOND TREE IS STOOD IN FOR BY A ROW NO FRAGMENT DECLARES,
            # because that is precisely what one looks like from in here: a
            # phrase in the table that this tree's own files cannot account
            # for. A second worktree cannot be built inside a test; this is the
            # exact thing that was destroyed, and it is destroyed the same way.
            store.execute(
                "INSERT OR REPLACE INTO identities (phrase, fragment_id) "
                "VALUES ('a phrase another tree declared', 'FRG-ELE-001')")
            store.db.commit()

            SEARCH.index(store)
            check(store.execute(
                      "SELECT fragment_id FROM identities WHERE phrase = "
                      "'a phrase another tree declared'").fetchone() is not None,
                  "indexing again with nothing changed leaves another tree's "
                  "declaration STANDING - on the old code that row was gone, "
                  "and that is row 136 in one line")

            os.utime(os.path.join(ROOT, "brain", "fragments",
                                  "set-selection", "fragment.yaml"), None)
            SEARCH.index(store)
            check(store.execute(
                      "SELECT fragment_id FROM identities WHERE phrase = "
                      "'a phrase another tree declared'").fetchone() is not None,
                  "and touching a file without changing a character still "
                  "changes nothing - hashed like heron_embed, not timed, "
                  "because a checkout moves every mtime it touches")

            # AND THE OTHER HALF, or this is a skip that never stops skipping.
            # A REAL change must still rebuild, and rebuilding correctly DROPS
            # the row above - a tree's own files replacing the table is what
            # indexing IS. Merging is what makes a declaration durable; this
            # only stops a reader destroying one on its way past.
            store.execute("UPDATE fragments SET domain = domain || ' changed' "
                          "WHERE id = 'FRG-ELE-001'")
            store.db.commit()
            SEARCH.index(store)
            check(store.execute(
                      "SELECT fragment_id FROM identities WHERE phrase = "
                      "'a phrase another tree declared'").fetchone() is None,
                  "a REAL change rebuilds, so the skip is a skip and not a "
                  "stop - D-30: it has to find nothing when there is nothing")

            written, skipped = SEARCH.index(store, force=True)
            check(written >= 2 and skipped == 0,
                  "and force=True rebuilds whatever the digest says, matching "
                  "heron_embed.index(store, force=True) - %d written" % written)

            # AND A SKIP MUST REPORT ITSELF AS A SKIP. `heron_index._counts`
            # reads a bare number as "(written, 0)", so while this returned a
            # scalar every no-op call claimed it had rewritten the whole
            # library - next to a vector index correctly reporting nothing.
            # Found by review on PR #198.
            written, skipped = SEARCH.index(store)
            check(written == 0 and skipped >= 2,
                  "a skip reports (0, rows) and never counts itself as work")

        finally:
            store.close()
    finally:
        shutil.rmtree(home, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)

    # ---- ROW 127: a PERMISSION may not be read from the cache alone --------
    #
    # `short_circuit` reads a fragment's status out of the INDEX, and
    # heron_lookup turns it into "so it may run without asking". On 2026-09-19
    # it said exactly that about a fragment demoted to DRAFT the same morning,
    # and closed the same reply with a total derived live from disk that
    # disagreed. The file is the authority; the index is a cache.
    print()
    print("ROW 127 - the index is a cache, the file is the authority")
    room = tempfile.mkdtemp(prefix="heron-disk-status-")
    try:
        def fragment(folder, fid, status):
            here = os.path.join(room, folder)
            os.makedirs(here)
            io.open(os.path.join(here, "fragment.yaml"), "w",
                    encoding="utf-8").write(
                        u"heron-status: %s\nid: %s\ncapability: X\n"
                        u"contract:\n  needs:\n    - name: id\n" % (status, fid))

        fragment("proved", "FRG-TEST-001", "PROVEN")
        fragment("drafted", "FRG-TEST-002", "DRAFT")

        check(SEARCH.disk_status("FRG-TEST-001", room) == "PROVEN",
              "the status is read from the fragment's own file")
        check(SEARCH.disk_status("FRG-TEST-002", room) == "DRAFT",
              "for each one separately")
        check(SEARCH.disk_status("FRG-TEST-999", room) is None,
              "and an id no file carries reads as None, never as a status")

        # `id:` ALSO APPEARS INDENTED INSIDE A CONTRACT on real fragments, and
        # taking the first match anywhere would read a need's name as the
        # fragment's own id.
        check(SEARCH.disk_status("id", room) is None,
              "a need called `id` inside the contract is not read as the "
              "fragment's id - the parse is TOP LEVEL only")

        allowed, told = SEARCH.may_run_unasked("FRG-TEST-001", "PROVEN", room)
        check(allowed and "may run without asking" in told,
              "index and file agreeing on PROVEN still grants, unchanged")

        # ROW 127's OWN CASE.
        allowed, told = SEARCH.may_run_unasked("FRG-TEST-002", "PROVEN", room)
        check(not allowed, "a stale PROVEN in the index does NOT grant when "
                           "the file says DRAFT")
        check("the index says PROVEN while the fragment's own file says DRAFT"
              in told,
              "and the refusal names BOTH, so a reader can see which is stale")
        check("re-index" in told, "and says how to clear it")

        # ABSENCE IS A LEGITIMATE STATE, NOT EVIDENCE OF STALENESS. A store
        # can be indexed from another root, or hold rows that never had a file
        # - this suite's own FRG-QA-900 has `folder='x'` and no file anywhere.
        # Row 127's defect is the two sources answering DIFFERENTLY, and a
        # rule that punished absence to catch it would change behaviour far
        # beyond the defect. That rule was written first and removed.
        allowed, told = SEARCH.may_run_unasked("FRG-TEST-999", "PROVEN", room)
        check(allowed and "may run without asking" in told,
              "an id with no file behaves exactly as before - absence is not "
              "disagreement")

        allowed, told = SEARCH.may_run_unasked("FRG-TEST-002", "DRAFT", room)
        check(not allowed and "nobody has watched it work" in told,
              "and when both agree it is DRAFT, the old sentence is unchanged")

        # THE WHOLE SAFETY ARGUMENT: no arrangement makes this grant something
        # the old code refused.
        every = [("FRG-TEST-001", "PROVEN"), ("FRG-TEST-001", "DRAFT"),
                 ("FRG-TEST-002", "PROVEN"), ("FRG-TEST-002", "DRAFT"),
                 ("FRG-TEST-999", "PROVEN"), ("FRG-TEST-999", "DRAFT")]
        widened = [pair for pair in every
                   if SEARCH.may_run_unasked(pair[0], pair[1], room)[0]
                   and pair[1] not in SEARCH.RUNNABLE_UNASKED]
        check(not widened,
              "across every combination it never grants where the index alone "
              "would not have - it can only ever WITHHOLD")

        # And it is cheap enough to sit in a lookup - see the docstring.
        import time
        start = time.time()
        for _ in range(50):
            SEARCH.disk_status("FRG-TEST-001", room)
        warm = (time.time() - start) / 50.0
        check(warm < 0.05,
              "a warm read costs %.4fs, so a lookup can afford to ask" % warm)
    finally:
        shutil.rmtree(room, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the common sentence costs one lookup, only a PROVEN")
    print("fragment runs off it, and an ambiguous sentence is a miss.")
    print("It says NOTHING about meaning: ask in words no fragment contains")
    print("and this layer correctly finds nothing. Step 10 is that.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
