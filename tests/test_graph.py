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

PROVIDES_ELEMENTS = ("    - name: elements\n"
                     "      type: IList<Element>")


def break_elements(text):
    """Rename `elements` where a fragment PROVIDES it, and nowhere else.

    The same two lines appear under `needs:` on most of the library, and
    renaming those breaks the composition from the CONSUMER's side - which
    orphans everything, passes for the wrong reason, and hides whatever this
    test was actually asking. Only the tail after `provides:` is touched.

    Returns the new text, or None if this fragment does not provide it.
    """
    at = text.find("  provides:")
    if at < 0:
        return None
    head, tail = text[:at], text[at:]
    if PROVIDES_ELEMENTS not in tail:
        return None
    return head + tail.replace(PROVIDES_ELEMENTS,
                               "    - name: somethingElse\n"
                               "      type: IList<Element>", 1)


def providers_of_elements():
    """EVERY fragment on disk that provides `elements`, found rather than named.

    This list was hardcoded twice and was wrong both times. It named one
    fragment until 2026-08-29, when the library grew a second and breaking one
    of two stopped orphaning the consumer; it named two until 2026-08-31, when
    a third arrived and did it again. Each time the test failed for a library
    that was perfectly correct, and each time the fix was to type one more
    path.

    So it is derived. A test whose fixture is a list of filenames is a test
    that expires quietly the next time somebody does the thing this repository
    is for - adding a fragment.
    """
    root = os.path.join(ROOT, "brain", "fragments")
    found = []
    for name in sorted(os.listdir(root)):
        path = os.path.join(root, name, "fragment.yaml")
        if not os.path.isfile(path):
            continue
        if break_elements(io.open(path, encoding="utf-8").read()) is not None:
            found.append(path)
    return found


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    home = tempfile.mkdtemp(prefix="heron-graph-")
    os.environ["HERON_KNOWLEDGE"] = home
    providers = providers_of_elements()
    originals = dict((path, io.open(path, encoding="utf-8").read())
                     for path in providers)

    def break_providers():
        """Rename what EVERY provider of `elements` leaves behind.

        All of them, or the consumer keeps a feeder and never orphans - which
        looks like this test failing and is actually the library being fine.
        """
        for path, text in originals.items():
            io.open(path, "w", encoding="utf-8").write(break_elements(text))

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

            # Asserts THE BREAK'S orphans are gone, not that the library has
            # none. It used to say `not G.orphans(store)` - a claim about the
            # whole library, which held only while every fragment was a filter
            # feeding an action. GET_ACTIVE_VIEW broke it by being legitimately
            # standalone: it is consumed by the HOST, which the graph does not
            # model (D-46). Testing the thing under test survives the library
            # growing; testing a global property does not, and this is the
            # second assertion in this file to learn that.
            healed = [i for i, _w in G.orphans(store)]
            check("FRG-ELE-001" not in healed and "FRG-SEL-001" not in healed,
                  "and the orphans the break created go with it")

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
