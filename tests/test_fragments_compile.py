#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
No compiler on this machine is NOT RUN, and NOT RUN is not a failure.

    python tests/test_fragments_compile.py

`tools/check-fragments-compile.py` compiles every fragment's C# against every
Revit release it claims, with no Revit and no Windows. It is the sixth of the
seven CI gates that had never been opened, and this is the first suite it has
ever had.

IT SHELLS OUT TO `dotnet build` WITH NO GUARD. Measured 2026-09-22 on this
container, which has no .NET SDK:

    FileNotFoundError: [Errno 2] No such file or directory: 'dotnet'
    exit 1

A traceback is not one of AGENTS.md's four states, and exit 1 is the code for
"a fragment does not compile" - so a reader, or a script, cannot tell a broken
library from a machine with no compiler on it. THE WORKFLOW ALREADY KNOWS: the
step lives in a different CI job, and the comment beside it says why - "it
shells out to `dotnet build` with no guard: the gates runner has no SDK and it
would fail there for the wrong reason". That is this defect, written into the
workflow instead of fixed in the tool.

THIS SUITE NEVER COMPILES ANYTHING. Everything here is the part that runs
without an SDK: the harness text, the contract assignments it generates, what
it reads out of a fragment.yaml, the record it leaves for the matrix agent,
and how it answers a release nobody claims. A compile needs the SDK, it needs
minutes, and CI already does it.

WHAT IT CANNOT DO: it cannot say whether the fragments compile. Only the
compiler answers that, on a runner that has one, and a green run here is not
that answer.

    python tests/test_fragments_compile.py
"""

import contextlib
import importlib.util
import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-fragments-compile.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has hyphens in it."""
    try:
        spec = importlib.util.spec_from_file_location("check_fragments_compile",
                                                      TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


FRAGMENT = """id: FRG-TEST-001
heron-status: DRAFT
revit: ["2020", "2024"]
contract:
  needs:
    - name: doc
      type: Document
    - name: level
      type: Level
  provides:
    - name: elements
      type: IList<Element>
"""

SNIPPET = "var elements = new List<Element>();\n"


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    if tool is None:
        print("  tools/check-fragments-compile.py would not load. It needs")
        print("  PyYAML at call time, so this is usually a missing dependency")
        print("  rather than a defect. NOT RUN - and that is not a pass.")
        return 3

    check(True, "tools/check-fragments-compile.py loads")
    runner = getattr(tool, "main", None)
    check(callable(runner), "and it still has main()")
    check(getattr(tool, "FRAGMENTS", None) is not None,
          "and names the folder it reads, so a test can point it elsewhere")
    if not callable(runner) or getattr(tool, "FRAGMENTS", None) is None:
        print()
        print("FAILED")
        for line in FAILURES:
            print("  - %s" % line)
        return 1

    home = tempfile.mkdtemp(prefix="heron-compile-test-")
    was = (tool.FRAGMENTS, getattr(tool, "RESULTS", None), sys.argv)
    try:
        def library(manifest=FRAGMENT, snippet=SNIPPET, impl="any"):
            """One little fragment library, pointed at by tool.FRAGMENTS."""
            place = tempfile.mkdtemp(dir=home)
            folder = os.path.join(place, "a-fragment")
            os.makedirs(os.path.join(folder, "impl", impl))
            io.open(os.path.join(folder, "fragment.yaml"), "w",
                    encoding="utf-8", newline="").write(manifest)
            io.open(os.path.join(folder, "impl", impl, "fragment.cs"), "w",
                    encoding="utf-8", newline="").write(snippet)
            return place

        def run(place, argv=()):
            """(exit code, what it printed) against a library built here."""
            tool.FRAGMENTS = place
            tool.RESULTS = os.path.join(home, "compile-results.json")
            sys.argv = ["check-fragments-compile.py"] + list(argv)
            said = io.StringIO()
            try:
                with contextlib.redirect_stdout(said):
                    with contextlib.redirect_stderr(said):
                        code = runner()
            except SystemExit as stop:              # argparse
                code = stop.code
            except BaseException as raised:         # noqa: BLE001
                code = "raised %s" % type(raised).__name__
            finally:
                tool.FRAGMENTS, tool.RESULTS, sys.argv = (was[0], was[1],
                                                          was[2])
            return code, said.getvalue()

        print()
        print("1. What it reads out of a fragment.yaml")
        tool.FRAGMENTS = library()
        found = tool.load_fragments()
        tool.FRAGMENTS = was[0]
        check(len(found) == 1, "one implementation found, and it found %d"
                               % len(found))
        if found:
            one = found[0]
            check(one["id"] == "FRG-TEST-001", "the id comes from the file")
            check(one["revit"] == ["2020", "2024"],
                  "the releases it claims, as strings - %r" % one["revit"])
            check([n["name"] for n in one["needs"]] == ["doc", "level"],
                  "its declared needs, in order")
            check([p["name"] for p in one["provides"]] == ["elements"],
                  "and what it promises")

        print()
        print("2. THE HARNESS - a snippet is not standalone")
        if found:
            text, safe = tool.harness(found[0])
            check("Document doc, Level level" in text,
                  "the parameters ARE the declared needs, at the declared "
                  "types - generated from the contract and not guessed")
            # THE VENDOR NAMESPACE IS NEVER TYPED HERE. check-structure.py
            # greps for it on purpose - "a vendor namespace named in a
            # COMMENT is still a boundary being discussed in the wrong
            # file" - and it caught this suite's first draft. So the list is
            # read from the tool that owns it, and the check is that the
            # harness declares every name on it and no others, which is
            # tighter than naming one.
            declared = [l for l in text.split("\n") if l.startswith("using ")]
            check(bool(tool.USINGS) and len(declared) == len(tool.USINGS),
                  "the harness declares every namespace USINGS names and no "
                  "others - %d using lines for %d names"
                  % (len(declared), len(tool.USINGS)))
            check(all(("using %s;" % u) in text for u in tool.USINGS),
                  "and each one of them by name")
            check(SNIPPET.strip() in text, "the snippet is in there whole")
            check("FRG_TEST_001" in safe,
                  "the wrapper carries the id, so an error in it can be "
                  "blamed on the right fragment - %r" % safe)

            print()
            print("   AND THE CONTRACT IS CHECKED, NOT JUST THE SYNTAX")
            # A snippet that compiles while breaking its promise is the one
            # that composes into something broken later, far from here.
            check("IList<Element> __provides_elements = elements;" in text,
                  "each promised name is assigned to a local of the declared "
                  "type, so leaving the wrong type is a compiler error")
            check("GC.KeepAlive(__provides_elements);" in text,
                  "and kept alive, so it cannot be optimised into nothing")

            print()
            print("   AND #line POINTS AT THE FRAGMENT'S OWN FILE")
            line = [l for l in text.split("\n") if l.startswith("#line 1 ")]
            check(len(line) == 1, "there is one #line directive")
            if line:
                path = line[0].split('"')[1]
                check(os.path.isabs(path),
                      "and the path is ABSOLUTE - a repo-relative one built "
                      "on net472 and failed on every net8 release with "
                      "CS1504; it reads %r" % path[:40])

        print()
        print("3. A release nobody claims is said, not compiled")
        code, spoke = run(library(), ["2019"])
        check(code == 2, "an undeclared release exits 2, and it exits %r"
                         % code)
        check("No fragment claims Revit 2019" in spoke, "and names it")

        print()
        print("4. Nothing to compile is said too")
        code, spoke = run(tempfile.mkdtemp(dir=home))
        check(code == 1 and "Nothing to compile" in spoke,
              "an empty library exits 1 and says so")

        print()
        print("5. NO COMPILER ON THIS MACHINE IS NOT RUN, NOT A FAILURE")
        # AGENTS.md keeps four states apart: PASS, FAIL, NOT RUN (say why),
        # NEEDS REAL REVIT. A traceback is none of them, and exit 1 is the
        # code for "a fragment does not compile".
        if shutil.which("dotnet") is None:
            code, spoke = run(library(), ["2020"])
            check(code == 3,
                  "with no dotnet on PATH it exits 3 - the house's code for "
                  "could not run - and it exits %r" % code)
            check("NOT RUN" in spoke,
                  "and says NOT RUN in those words, so it is not read as a "
                  "library that does not compile")
            check("dotnet" in spoke and "Traceback" not in spoke,
                  "and names what is missing rather than printing a "
                  "traceback")
        else:
            print("        this machine HAS a .NET SDK, so the no-compiler")
            print("        path cannot be exercised here. Not asserted.")

        print()
        print("6. The record it leaves for the matrix agent")
        # HERON-FRG-MTX-009 must take its status from TESTS, never
        # assumption, so there has to be something on disk to read.
        tool.RESULTS = os.path.join(home, "r.json")
        written = tool.record(["2020"], {"2020": (True, [], 3)},
                              [1, 2, 3])
        tool.RESULTS = was[1]
        check(written is not None, "it writes the file")
        if written:
            payload = json.loads(io.open(written, encoding="utf-8").read())
            check(payload["versions"]["2020"]["ok"] is True,
                  "and records whether the release passed")
            check(payload["versions"]["2020"]["claimed_by"] == 3,
                  "how many fragments claimed it")
            check(payload["fragments"] == 3, "and how many there were")
    finally:
        tool.FRAGMENTS, tool.RESULTS, sys.argv = was
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the harness is built from the contract, the contract is")
    print("checked, and no compiler is NOT RUN rather than a failure.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
