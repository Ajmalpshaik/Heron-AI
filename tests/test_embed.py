# Heron-Agent:  HERON-RAG-EMB-008, HERON-RAG-VEC-009
# Heron-Step:   10
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Step 10 - finding a fragment by something other than its exact words.

    python tests/test_embed.py

Runs anywhere, offline, with nothing installed. That is the point of it.

WHAT IT PROVES
  1. The same text embeds the same way IN A DIFFERENT PROCESS. Python's own
     hash() is salted per process and would have broken this silently.
  2. Plurals, word endings and word order are handled.
  3. Re-indexing unchanged text costs NOTHING - content-hashed, not mtime.
  4. Vectors stay inside their own scope file.

WHAT IT ASSERTS AS A LIMIT, RATHER THAN HIDING
  5. Synonyms score ZERO. This backend does not know meaning and the test says
     so in numbers.
  6. On "show me every duct", the vector route ranks the WRONG fragment first
     and the keyword route ranks the right one first. That disagreement is
     recorded here as Step 11's acceptance criterion, not smoothed over.
"""

import os
import shutil
import subprocess
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
    home = tempfile.mkdtemp(prefix="heron-embed-")
    os.environ["HERON_KNOWLEDGE"] = home

    import heron_scope as SCOPE
    import heron_embed as E
    import heron_search as SEARCH

    def cos(a, b):
        return sum(x * y for x, y in zip(E.lexical_vector(a), E.lexical_vector(b)))

    try:
        name, why = E.backend()
        print("Backend in use: %s - %s" % (name, why))

        print()
        print("1. The same text embeds the same way in a DIFFERENT process")
        code = ("import sys; sys.path.insert(0, %r);"
                "import heron_embed as E;"
                "print(sum(E.lexical_vector('select all ducts')[:8]))"
                % os.path.join(ROOT, "brain"))
        first = subprocess.check_output([sys.executable, "-c", code]).strip()
        second = subprocess.check_output([sys.executable, "-c", code]).strip()
        here = sum(E.lexical_vector("select all ducts")[:8])
        check(first == second,
              "two fresh processes agree - Python's salted hash() would not")
        check(abs(float(first) - here) < 1e-6,
              "and they agree with this one")

        print()
        print("2. Plurals, endings and word order")
        check(cos("duct", "ducts") > 0.5, "duct/ducts %.3f" % cos("duct", "ducts"))
        check(cos("duct", "ductwork") > 0.5,
              "duct/ductwork %.3f" % cos("duct", "ductwork"))
        check(cos("select all ducts", "all ducts selected") > 0.8,
              "word order barely matters: %.3f"
              % cos("select all ducts", "all ducts selected"))
        check(cos("duct", "ducte") > 0.4,
              "an inserted letter still matches: %.3f" % cos("duct", "ducte"))

        print()
        print("3. THE LIMITS, asserted in numbers rather than described")
        check(cos("diffuser", "grille") <= 0.0,
              "synonyms score zero or less (%.3f) - this is NOT meaning"
              % cos("diffuser", "grille"))
        check(cos("level", "floor") <= 0.0,
              "level/floor likewise (%.3f)" % cos("level", "floor"))
        check(cos("duct", "dcut") == 0.0,
              "a TRANSPOSED pair destroys every n-gram and scores exactly 0.000")
        check(cos("duct", "wall") == 0.0, "unrelated words score 0.000")

        print()
        print("4. Indexing is content-hashed, so a checkout costs nothing")
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            did, skipped = E.index(store)
            check(did >= 2 and skipped == 0, "first pass embeds (%d)" % did)

            did, skipped = E.index(store)
            check(did == 0 and skipped >= 2,
                  "second pass embeds NOTHING - %d unchanged" % skipped)

            os.utime(os.path.join(ROOT, "brain", "fragments",
                                  "set-selection", "fragment.yaml"), None)
            did, _ = E.index(store)
            check(did == 0,
                  "touching a file without changing it still embeds nothing - "
                  "which is the whole reason this is hashed and not timed")

            did, _ = E.index(store, force=True)
            check(did >= 2, "--force re-embeds everything when asked (%d)" % did)

            print()
            print("5. Nearest returns similarity, best first")
            hits = E.nearest(store, "put them on the screen")
            check(hits and hits[0][0] == "FRG-SEL-001",
                  "'put them on the screen' -> the selection fragment (%.3f)"
                  % hits[0][1])
            check(all(-1.01 <= s <= 1.01 for _i, s in hits),
                  "scores are similarities in [-1, 1], not distances")
            check(hits == sorted(hits, key=lambda p: -p[1]), "best first")

            print()
            print("6. THE DISAGREEMENT Step 11 has to settle")
            query = "show me every duct in the model"
            SEARCH.index(store)
            by_word = [h["id"] for h in SEARCH.keywords(store, query)]
            by_vector = [i for i, _s in E.nearest(store, query)]
            check(by_word[0] == "FRG-ELE-001",
                  "keywords rank the duct filter FIRST - %s" % ", ".join(by_word))
            check(by_vector[0] == "FRG-SEL-001",
                  "vectors rank it SECOND - %s" % ", ".join(by_vector))
            check(by_word[0] != by_vector[0],
                  "so the two routes disagree, and neither is reliable alone. "
                  "Step 11 fuses them and MUST return FRG-ELE-001 here")

            print()
            print("7. Vectors stay inside their own scope")
            other = SCOPE.open_scope(SCOPE.USER)
            try:
                E.ensure_tables(other)
                mine = store.execute("SELECT COUNT(*) AS n FROM vectors").fetchone()["n"]
                theirs = other.execute("SELECT COUNT(*) AS n FROM vectors").fetchone()["n"]
                check(mine >= 2 and theirs == 0,
                      "the global scope's %d vectors are not in the user scope" % mine)
            finally:
                other.close()
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

    print("PASSED - deterministic across processes, tolerant of endings and")
    print("word order, and free to re-index when nothing changed.")
    print()
    print("It is NOT meaning, and check 3 says so in numbers. A trained model")
    print("is the backend that would be - and it could not be tested here,")
    print("because this container's network refuses huggingface.co. A7 in")
    print("NEEDS-CHECKING.md is that run, on a machine that can reach one.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
