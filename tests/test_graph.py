# Heron-Agent:  HERON-KRN-DEP-013
# Heron-Step:   13
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Step 13 - what breaks if this changes.

    python tests/test_graph.py

WHAT IT PROVES
  1. THE DERIVER CATCHES A BREAK IT IS KNOWN TO CONTAIN. A contract is broken
     on purpose - a provided name renamed - and the graph reports the
     composition gone BEFORE any clean output is believed. The build order asks
     for this in as many words, and it is the standard check-api-surface.py was
     held to.
  2. "What breaks if this changes" returns the right set.
  3. Sole provision is called out - the dangerous case, because a caller that
     asked for the capability never named the fragment.
  4. Nothing is stored that could be computed, so nothing can go stale.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []
FRAGMENTS_DIR = os.path.join(ROOT, "brain", "fragments")


def providers_of_elements():
    """Every fragment.yaml that PROVIDES `elements`, found on disk.

    This list is discovered rather than written down, and the history is the
    argument. The test originally named one file. The library grew a second
    provider on 2026-08-29 and the test broke - breaking one of two leaves the
    consumer feedable, which is the capability registry working exactly as
    designed. The fix then was to name the second file too, and the comment
    above it said "EVERY provider" while the code named exactly two.

    It broke again on 2026-08-30 at the third, which is the same bug one growth
    later, and under D-45 the library is about to grow by hundreds. A test that
    encodes how many ways there are to do a job stops testing anything the
    moment somebody adds another way.

    Only the PROVIDES side is renamed. Several fragments NEED `elements` at the
    same type, and renaming those would break the composition from the other
    end - the test would still fail, for a reason it was not asking about.
    """
    found = []
    for name in sorted(os.listdir(FRAGMENTS_DIR)):
        path = os.path.join(FRAGMENTS_DIR, name, "fragment.yaml")
        if not os.path.exists(path):
            continue
        text = io.open(path, encoding="utf-8").read()
        head, sep, tail = text.partition("  provides:")
        if sep and PROVIDED in tail:
            found.append(path)
    return found


PROVIDED = "    - name: elements\n      type: IList<Element>"
RENAMED = "    - name: somethingElse\n      type: IList<Element>"


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    home = tempfile.mkdtemp(prefix="heron-graph-")
    os.environ["HERON_KNOWLEDGE"] = home
    provider_paths = providers_of_elements()
    originals = dict((path, io.open(path, encoding="utf-8").read())
                     for path in provider_paths)
    if not originals:
        print("  FAIL  nothing on disk provides `elements` - this test has "
              "nothing to break, which is itself the finding")
        FAILURES.append("no providers of elements found on disk")

    def break_providers():
        """Rename what EVERY provider of `elements` leaves behind.

        Every one, discovered from disk - see providers_of_elements(). Leaving
        a single provider intact leaves the consumer feedable and the orphan
        check silently stops asserting anything.
        """
        for path, text in originals.items():
            head, sep, tail = text.partition("  provides:")
            io.open(path, "w", encoding="utf-8").write(
                head + sep + tail.replace(PROVIDED, RENAMED))

    def restore_providers():
        for path, text in originals.items():
            io.open(path, "w", encoding="utf-8").write(text)

    import heron_scope as SCOPE
    import heron_graph as G
    import heron_capability as CAP

    try:
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            CAP.rebuild(store)

            print("1. THE DERIVER CATCHES A BREAK IT WAS GIVEN")
            before = G.composes_into("FRG-ELE-001")
            check("FRG-SEL-001" in before,
                  "with the real contracts, the filter feeds the action: %s"
                  % ", ".join(before))

            # Break it on purpose: rename what the filter provides, so the
            # action's `elements` need is no longer met by anything.
            break_providers()
            after = G.composes_into("FRG-ELE-001")
            check("FRG-SEL-001" not in after,
                  "rename what it provides and the composition is GONE - the "
                  "deriver saw it, so its clean answers mean something")

            broken = G.orphans(store)
            check(any(i == "FRG-ELE-001" for i, _w in broken),
                  "and the filter is reported as an orphan nothing can consume")
            check(any(i == "FRG-SEL-001" for i, _w in broken),
                  "and the action as one nothing can feed")

            restore_providers()
            check(G.composes_into("FRG-ELE-001") == before,
                  "put it back and the graph returns to what it was - it is "
                  "reading the files, not remembering")
            check(not G.orphans(store), "and the orphans go with it")

            print()
            print("2. What breaks if this changes")
            got = G.impact(store, "FRG-ELE-001")
            check(got["exists"], "the fragment is found")
            check(got["provides"] == "FILTER_ELEMENTS_BY_CATEGORY",
                  "its capability is reported")
            check("FRG-SEL-001" in got["downstream"],
                  "what could run after it: %s" % ", ".join(got["downstream"]))
            check(got["upstream"] == [],
                  "and NOTHING feeds it - every need it has comes from the "
                  "wrapper or the request, so no fragment has to run first")

            missing = G.impact(store, "FRG-NOPE-999")
            check(not missing["exists"],
                  "an unknown fragment says so rather than returning an empty "
                  "graph that reads like 'nothing depends on it'")

            print()
            print("3. Sole provision is the dangerous case, and it is named")
            check(got["sole_provider_of"] == ["FILTER_ELEMENTS_BY_CATEGORY"],
                  "it is the ONLY provider, so changing it changes the capability")

            store.execute(
                "INSERT OR REPLACE INTO fragments (id, capability, "
                "semantic_identity, kind, status, domain, risk, folder, revit) "
                "VALUES ('FRG-ELE-778','FILTER_ELEMENTS_BY_CATEGORY','x',"
                "'filter','PROVEN','revit.elements','READ','x','2024')")
            store.db.commit()
            CAP.rebuild(store)
            shared = G.impact(store, "FRG-ELE-001")
            check(shared["sole_provider_of"] == [],
                  "add a second provider and it stops being sole")
            check(shared["shares_capability"] == ["FILTER_ELEMENTS_BY_CATEGORY"],
                  "it shares the capability instead - a change is now survivable")

            print()
            print("4. A skill's requirement is the ONE stored edge")
            G.skill_needs(store, "select-ducts", "FILTER_ELEMENTS_BY_CATEGORY")
            got = G.impact(store, "FRG-ELE-001")
            check(("select-ducts", "FILTER_ELEMENTS_BY_CATEGORY")
                  in got["skills_affected"],
                  "the skill is reported as affected, through the capability - "
                  "and it never named the fragment")

            print()
            print("5. Nothing derived is stored, so nothing derived is stale")
            tables = [r["name"] for r in store.execute(
                "SELECT name FROM sqlite_master WHERE type='table'").fetchall()]
            check("skill_needs" in tables, "the stored edge has a table")
            for banned in ("fragment_edges", "composition", "provides_edges"):
                check(banned not in tables,
                      "there is no %s table - D-40, an edge is derived before "
                      "it is stored" % banned)
        finally:
            store.close()
    finally:
        restore_providers()
        shutil.rmtree(home, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the deriver was shown catching a break before its clean")
    print("answers were believed, and every edge but one is computed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
