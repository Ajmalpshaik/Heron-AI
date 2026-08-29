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
            n = SEARCH.index(store)
            check(n >= 2, "the scope indexes its fragments (%d)" % n)

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

            SEARCH.remember(store, odd, "FRG-ELE-001")
            a = SEARCH.ask(store, odd)
            check(a.route == "cache", "after remembering, it takes the cache route")
            check(a.fragment_id == "FRG-ELE-001", "and returns what was learned")
            check(not a.autorun,
                  "a cached answer is a CANDIDATE - it never runs unasked")

            SEARCH.remember(store, odd, "FRG-ELE-001")
            _fid, hits = SEARCH.recall(store, odd)
            check(hits == 2, "and the hit count rises (%d)" % hits)

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
        finally:
            store.close()
    finally:
        shutil.rmtree(home, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)

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
