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

WHAT IT MUST NOT DO
  Break the real library to prove any of it. The break happens in a copy under
  the system temp folder, for the reason stand_in_library() gives.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []

PROVIDES_LINE = "    - name: elements"


def break_elements(text):
    """Rename `elements` where a fragment PROVIDES it, and nowhere else.

    The same name appears under `needs:` on most of the library, and renaming
    those breaks the composition from the CONSUMER's side - which orphans
    everything, passes for the wrong reason, and hides whatever this test was
    actually asking. So only the `provides:` block is touched, and only up to
    the next key at its own indent or shallower.

    Returns the new text, or None if this fragment does not provide it.

    IT MATCHED TWO EXACT ADJACENT LINES UNTIL 2026-09-12, and by then it
    matched NOTHING. `- name: elements` was followed by `type:` when this was
    written; D-51 and D-52's work put `role:` between them, so the fixture
    stopped finding any of the 50 fragments that provide `elements`,
    break_providers() became a no-op, and the three checks below failed while
    the library and the deriver were both perfectly correct.

    That is the failure providers_of_elements() already carries a docstring
    about - it derives the FILE LIST for exactly this reason - and the pattern
    inside it was left hardcoded, so the test expired the next time somebody
    did the thing this repository is for. Matching the entry by its `name:`
    line and nothing else is what makes it survive another key arriving.
    """
    at = text.find("  provides:")
    if at < 0:
        return None
    head, tail = text[:at], text[at:]

    lines = tail.splitlines(True)
    end = len(lines)
    for index, line in enumerate(lines[1:], start=1):
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        indent = len(line) - len(line.lstrip())
        if indent <= 2:
            end = index
            break

    for index in range(1, end):
        if lines[index].rstrip("\n").rstrip() == PROVIDES_LINE:
            lines[index] = lines[index].replace("name: elements",
                                                "name: somethingElse")
            return head + "".join(lines)
    return None


def providers_of_elements(root):
    """EVERY fragment under `root` that provides `elements`, found rather
    than named.

    This list was hardcoded twice and was wrong both times. It named one
    fragment until 2026-08-29, when the library grew a second and breaking one
    of two stopped orphaning the consumer; it named two until 2026-08-31, when
    a third arrived and did it again. Each time the test failed for a library
    that was perfectly correct, and each time the fix was to type one more
    path.

    So it is derived. A test whose fixture is a list of filenames is a test
    that expires quietly the next time somebody does the thing this repository
    is for - adding a fragment.

    `root` is passed in rather than computed, because the library this is asked
    about is the throwaway copy and never brain/fragments - see
    stand_in_library().
    """
    found = []
    for name in sorted(os.listdir(root)):
        path = os.path.join(root, name, "fragment.yaml")
        if not os.path.isfile(path):
            continue
        if break_elements(io.open(path, encoding="utf-8").read()) is not None:
            found.append(path)
    return found


def stand_in_library(destination):
    """A copy of the library to break, so that the real one never is.

    THIS TEST USED TO BREAK brain/fragments ITSELF. It rewrote the `provides:`
    block of all 55 fragments that provide `elements`, ran the deriver over
    them, and wrote the originals back - correctly, on every path that returns.
    Measured 2026-09-19: the real library sat broken for 11.9s of a 67s run,
    18% of it, with `git status` reporting 55 modified files for that whole
    window.

    Two things were wrong with that, and only one of them was about exceptions.

      A KILLED RUN LEAVES THEM BROKEN. tools/check-gaps.py runs every suite
      through subprocess.run with a 300s timeout, and a timeout KILLS the
      child; so does Ctrl+Break, and so does the machine going down. `finally`
      runs through none of those. What survives is 55 fragments whose contracts
      say `somethingElse`, and nothing on disk saying why.

      AND NOTHING HAS TO CRASH AT ALL. Several sessions share this checkout and
      its worktrees. One that runs `git add -A` inside those 11.9 seconds
      commits 55 fragment.yaml changes nobody made and is told nothing - the
      same shape as row 131 of docs/FRAGMENT-ISSUES.md, one layer out: a shared
      thing left as whichever process got there last wrote it.

    So the break happens somewhere the repository does not live. Only
    fragment.yaml is copied - load() reads only fragment.yaml, and the impl/
    trees are two thirds of the files for seconds that would buy nothing.
    That shortcut is a bet, so main() checks it: the copy must hold the same
    fragments, and read with the same problems, as brain/fragments. A load()
    that starts needing a sibling file fails there rather than quietly testing
    a smaller library.
    """
    source = os.path.join(ROOT, "brain", "fragments")
    for name in sorted(os.listdir(source)):
        card = os.path.join(source, name, "fragment.yaml")
        if not os.path.isfile(card):
            continue
        folder = os.path.join(destination, name)
        os.makedirs(folder)
        shutil.copyfile(card, os.path.join(folder, "fragment.yaml"))
    return destination


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    home = tempfile.mkdtemp(prefix="heron-graph-")
    os.environ["HERON_KNOWLEDGE"] = home
    workspace = tempfile.mkdtemp(prefix="heron-graph-library-")

    import heron_scope as SCOPE
    import heron_graph as G
    import heron_capability as CAP
    import heron_fragment as FRAG

    try:
        library = stand_in_library(os.path.join(workspace, "fragments"))
        providers = providers_of_elements(library)
        originals = dict((path, io.open(path, encoding="utf-8").read())
                         for path in providers)

        def loaded():
            """The stand-in, RE-READ from disk every time it is asked for.

            Not read once and reused. The last check of section 1 is that
            putting the files back puts the graph back, and a dict loaded once
            would make that sentence true whatever the deriver did.
            """
            found, _problems = FRAG.load_all(library)
            return found

        def break_providers():
            """Rename what EVERY provider of `elements` leaves behind.

            All of them, or the consumer keeps a feeder and never orphans -
            which looks like this test failing and is actually the library
            being fine.
            """
            for path, body in originals.items():
                with io.open(path, "w", encoding="utf-8") as handle:
                    handle.write(break_elements(body))

        def restore_providers():
            for path, body in originals.items():
                with io.open(path, "w", encoding="utf-8") as handle:
                    handle.write(body)

        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            CAP.rebuild(store)

            print("0. The stand-in IS the library, not something like it")
            real, real_problems = FRAG.load_all()
            mirror, mirror_problems = FRAG.load_all(library)
            check(sorted(real) == sorted(mirror),
                  "the copy holds the same %d fragment(s) as brain/fragments - "
                  "one that had lost any would test a smaller library and say "
                  "nothing about this one" % len(real))
            check(len(real_problems) == len(mirror_problems),
                  "and reads with the same %d problem(s), so copying only "
                  "fragment.yaml lost nothing load() wanted"
                  % len(real_problems))
            check(len(providers) > 1,
                  "%d of them provide `elements`, and every one gets broken"
                  % len(providers))

            print()
            print("1. THE DERIVER CATCHES A BREAK IT WAS GIVEN")
            before = G.composes_into("FRG-ELE-001", loaded())
            check("FRG-SEL-001" in before,
                  "with the real contracts, the filter feeds the action: %s"
                  % ", ".join(before))
            check(before == G.composes_into("FRG-ELE-001", real),
                  "and brain/fragments answers identically - what is about to "
                  "be broken is THIS library, not a resemblance of it")

            # Break it on purpose: rename what the filter provides, so the
            # action's `elements` need is no longer met by anything. IN THE
            # COPY. Nothing in this file opens a file under brain/ for writing.
            break_providers()
            after = G.composes_into("FRG-ELE-001", loaded())
            check("FRG-SEL-001" not in after,
                  "rename what it provides and the composition is GONE - the "
                  "deriver saw it, so its clean answers mean something")

            broken = G.orphans(store, loaded())
            check(any(i == "FRG-ELE-001" for i, _w in broken),
                  "and the filter is reported as an orphan nothing can consume")
            check(any(i == "FRG-SEL-001" for i, _w in broken),
                  "and the action as one nothing can feed")

            restore_providers()
            check(G.composes_into("FRG-ELE-001", loaded()) == before,
                  "put it back and the graph returns to what it was - it is "
                  "reading the files, not remembering")

            # Asserts THE BREAK'S orphans are gone, not that the library has
            # none. It used to say `not G.orphans(store)` - a claim about the
            # whole library, which held only while every fragment was a filter
            # feeding an action. GET_ACTIVE_VIEW broke it by being legitimately
            # standalone: it is consumed by the HOST, which the graph does not
            # model (D-98). Testing the thing under test survives the library
            # growing; testing a global property does not, and this is the
            # second assertion in this file to learn that.
            healed = [i for i, _w in G.orphans(store, loaded())]
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
        # NOTHING HERE PUTS brain/fragments BACK, because nothing here took it
        # apart. Both of these can fail, or never run at all, and the
        # repository is still exactly as this suite found it. That is the whole
        # of what the copy buys - a `finally` is a promise about the paths that
        # return, and a killed process does not take any of them.
        shutil.rmtree(workspace, ignore_errors=True)
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
