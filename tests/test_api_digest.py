#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A digest that cannot be read is not an empty one.

    python tests/test_api_digest.py

`tools/api-changes.py` writes `tools/api-surface/changes.json`, the
committed evidence `HERON-REVIT-ACI-034` reads to say which members a Revit
release stopped shipping. Measured 2026-09-22, it was the LAST tool in
`tools/` that nothing runs - no suite directly, none through another tool,
and not CI.

`tests/test_api_changes.py` is the suite for the AGENT that reads the
digest. This is the suite for the tool that writes it, which is a different
subject: that one asks whether the evidence catches a real breakage, this
one asks whether the evidence is whole.

THE DEFECT IT WAS WRITTEN FOR. The file carries a comment about a real
failure already fixed once:

    A TARGETED RUN REFRESHES, IT DOES NOT REPLACE. `api-changes.py 2025
    2026` ... until 2026-09-15 it wrote a digest holding that one
    transition over the committed one holding all seven.
    HERON-REVIT-ACI-034 then had no transition into 2027 - and read the
    absence as "2027 removed nothing", reporting every fragment clear.

The fix reads the committed digest and keeps what it did not recompute -
and handles a digest that DOES NOT PARSE by treating it as empty. Measured
on the seven-transition shape truncated mid-file: the targeted run wrote a
digest holding ONE transition and said nothing. That is the same failure
through a different door, and D-52 is the rule it breaks - a file that
cannot be read is not an empty one.

IT NEEDS THE NETWORK, 264 MB OF REFERENCE ASSEMBLIES AND A .NET SDK, and
this container has none of the three - so nothing here dumps a surface or
compares two releases. Every case works on the merge, the refusals and the
arithmetic, which need none of them.
"""

import importlib.util
import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "api-changes.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has a hyphen in it."""
    try:
        spec = importlib.util.spec_from_file_location("api_changes", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


def transition(older, newer):
    return {"from": older, "to": newer, "removed": [], "removedCount": 0,
            "addedCount": 0, "of": 1}


def seven():
    """The committed digest's shape: 2020 to 2027, seven transitions."""
    years = [str(y) for y in range(2020, 2028)]
    return {"releases": years,
            "transitions": [transition(years[i], years[i + 1])
                            for i in range(len(years) - 1)]}


def spoken(fn, *a, **k):
    """Run it, and hand back (result, what it printed).

    A RAISE IS CAUGHT AND NAMED RATHER THAN LET OUT. The whole subject here
    is a tool that answers a bad input with a traceback, so a suite that
    died on the same traceback would report nothing at all - and an error is
    not a failure (`heron-ship` section 2a). The exception comes back as the
    result, so the check that wanted an exit code fails and says what it got.
    """
    was = sys.stdout
    sys.stdout = io.StringIO()
    try:
        try:
            out = fn(*a, **k)
        except BaseException as why:               # noqa: BLE001
            out = "RAISED %s: %s" % (type(why).__name__, why)
        return out, sys.stdout.getvalue()
    finally:
        sys.stdout = was


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    check(tool is not None, "tools/api-changes.py loads")
    if tool is None:
        print()
        print("FAILED - it did not import")
        return 1

    for name in ("main", "changes", "members", "releases", "surface_of"):
        check(callable(getattr(tool, name, None)), "and it has %s()" % name)
    for name in ("OUT", "CACHE", "SURFACE", "READER"):
        check(getattr(tool, name, None), "and names %s" % name)
    if not callable(getattr(tool, "main", None)):
        print()
        print("FAILED - nothing to run")
        return 1

    home = tempfile.mkdtemp(prefix="heron-api-digest-")
    was_out, was_changes, was_surface = tool.OUT, tool.changes, tool.SURFACE
    try:
        tool.OUT = os.path.join(home, "changes.json")
        # A RELEASE WHOSE SURFACE IS ALREADY DUMPED NEEDS NEITHER THE
        # ASSEMBLIES NOR THE SDK, so pointing SURFACE at a folder holding two
        # of them is what makes the merge reachable on this container - and
        # it exercises cannot_answer()'s first branch honestly rather than
        # stubbing the guard out.
        tool.SURFACE = os.path.join(home, "surface")
        os.makedirs(tool.SURFACE)
        for year in ("2025", "2026"):
            io.open(os.path.join(tool.SURFACE, "%s.txt" % year), "w",
                    encoding="utf-8").write("Some.Vendor.Namespace.Thing.Name\n")
        # The comparison itself needs 264 MB of assemblies and a .NET SDK.
        # Only the MERGE is under test, so one transition is enough and it
        # is handed over rather than computed.
        tool.changes = lambda years: [transition(years[0], years[1])]
        check(tool.cannot_answer(["2025", "2026"]) is None
              if callable(getattr(tool, "cannot_answer", None)) else True,
              "a release already dumped needs no assemblies and no SDK")

        print()
        print("1. A TARGETED RUN REFRESHES, IT DOES NOT REPLACE")
        io.open(tool.OUT, "w", encoding="utf-8").write(json.dumps(seven()))
        code, said = spoken(tool.main, ["2025", "2026"])
        kept = json.loads(io.open(tool.OUT, encoding="utf-8").read())
        pairs = [(t["from"], t["to"]) for t in kept["transitions"]]
        check(code == 0, "a good digest is refreshed, exit %r" % code)
        check(len(pairs) == 7,
              "all seven transitions survive, and %d did" % len(pairs))
        check(("2026", "2027") in pairs,
              "including the one into the newest release - the transition "
              "whose absence read as '2027 removed nothing'")
        check("kept, not recomputed" in said,
              "and the ones it did not recompute say so")

        print()
        print("2. A DIGEST THAT DOES NOT PARSE IS NOT AN EMPTY ONE")
        print("   D-52. The same failure as the comment above the merge,")
        print("   through a different door: a truncated file read as 'there")
        print("   was nothing there' and six transitions went out silently.")
        broken = json.dumps(seven())[:120]
        io.open(tool.OUT, "w", encoding="utf-8").write(broken)
        code, said = spoken(tool.main, ["2025", "2026"])
        # READ AS TEXT. A refused run leaves the corrupt file exactly as it
        # was, so parsing it here would crash this suite on the very input
        # the case is about.
        on_disk = io.open(tool.OUT, encoding="utf-8").read()
        check(code != 0,
              "a digest that will not parse is refused, and it answered %r"
              % (code,))
        check(on_disk == broken,
              "the file is left exactly as it was found - the six it did not "
              "recompute are not silently dropped")
        check("refusing" in said.lower() and "parse" in said.lower(),
              "and it says which file and why, not just a number - it said %r"
              % said[:90])

        print()
        print("3. A DIGEST THAT IS JSON BUT NOT A DIGEST is refused too")
        io.open(tool.OUT, "w", encoding="utf-8").write("[1, 2, 3]")
        code, said = spoken(tool.main, ["2025", "2026"])
        check(code not in (0,) and not str(code).startswith("RAISED"),
              "a JSON list where a mapping belongs is refused rather than "
              "raising, and it answered %r" % (code,))
        check(io.open(tool.OUT, encoding="utf-8").read() == "[1, 2, 3]",
              "and that file is left alone too")

        print()
        print("4. NO DIGEST AT ALL is a first run, not a fault")
        os.remove(tool.OUT)
        code, said = spoken(tool.main, ["2025", "2026"])
        check(code == 0, "a first run writes one, exit %r" % code)
        first = json.loads(io.open(tool.OUT, encoding="utf-8").read())
        check(len(first["transitions"]) == 1,
              "holding what it computed, and it held %d"
              % len(first["transitions"]))
    finally:
        tool.OUT, tool.changes, tool.SURFACE = was_out, was_changes, was_surface
        shutil.rmtree(home, ignore_errors=True)

    print()
    print("5. TWO RELEASES ARE AN ARGUMENT QUESTION, answerable with no SDK")
    code, said = spoken(tool.main, ["2025"])
    check(code == 2, "one release exits 2, and it exited %r" % code)

    print()
    print("6. A MACHINE THAT CANNOT ANSWER SAYS SO, AND DOES NOT TRACEBACK")
    print("   `api-changes.py 2025 2026` with nothing cached raised OSError")
    print("   and exited 1 - the same code as a real failure, and a traceback")
    print("   is none of AGENTS.md's four states. Row 5b-133 is the precedent")
    print("   and its remedy is exit 3, NOT RUN.")
    print()
    print("   THE CONDITION IS FORCED, NOT ASSUMED - fixed 2026-09-22.")
    print("   This read 'Measured on this container: no cached assemblies, no")
    print("   .NET SDK' and asserted the refusal against whatever the machine")
    print("   happened to be. On a machine that CAN answer - the owner's PC,")
    print("   a runner with the assemblies cached - cannot_answer() correctly")
    print("   returned None and main() correctly returned 0, and four checks")
    print("   failed the tool for being right. Worse, main() then ran the real")
    print("   comparison and wrote the COMMITTED tools/api-surface/changes.json")
    print("   as a side effect of a test. Empty CACHE and SURFACE folders make")
    print("   the refusal reachable on EVERY machine, and OUT goes to a temp")
    print("   file so committed evidence is never under the pen.")
    blocked = getattr(tool, "cannot_answer", None)
    check(callable(blocked),
          "there is a cannot_answer() to ask before any dump is attempted")
    if callable(blocked):
        denied = tempfile.mkdtemp(prefix="heron-api-blocked-")
        was = tool.CACHE, tool.SURFACE, tool.OUT
        try:
            tool.CACHE = os.path.join(denied, "cache")
            tool.SURFACE = os.path.join(denied, "surface")
            os.makedirs(tool.CACHE)
            os.makedirs(tool.SURFACE)
            tool.OUT = os.path.join(denied, "changes.json")

            why = blocked(["2025", "2026"])
            check(why, "with nothing cached it answers with a reason, and it "
                       "said %r" % (why if why is None else str(why)[:70],))
            code, said = spoken(tool.main, ["2025", "2026"])
            check(code == 3,
                  "and main() returns 3 - could not run - rather than 1 or a "
                  "traceback; it returned %r" % code)
            check("NOT RUN" in said,
                  "saying NOT RUN in those words, and it said %r" % said[:90])
            check("check-api-surface" in said,
                  "and naming what to run to fix it")
            check(not os.path.isfile(tool.OUT),
                  "and it wrote no digest at all, having run nothing")
        finally:
            tool.CACHE, tool.SURFACE, tool.OUT = was
            shutil.rmtree(denied, ignore_errors=True)

    print()
    print("7. The arithmetic, which needs nothing external")
    path = os.path.join(tempfile.mkdtemp(prefix="heron-api-members-"), "s.txt")
    # THE NAME IS DELIBERATELY NOT THE REAL VENDOR NAMESPACE. check-structure
    # greps for it and refuses it outside revit/, on purpose: "a vendor
    # namespace named in a COMMENT is still a boundary being discussed in the
    # wrong file". What is under test is trimming and de-duplication, and the
    # text of the member has nothing to do with either.
    io.open(path, "w", encoding="utf-8").write(
        "  Some.Vendor.Namespace.Thing.Name  \n\n  \nB.C\nB.C\n")
    got = tool.members(path)
    check(got == {"Some.Vendor.Namespace.Thing.Name", "B.C"},
          "blank lines go, whitespace is trimmed, and a repeat is one "
          "member - it read %r" % (sorted(got),))
    shutil.rmtree(os.path.dirname(path), ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a digest it cannot read is refused, and a machine that")
    print("cannot answer says NOT RUN.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
