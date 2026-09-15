# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-NUP-019
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
.NET update - parsed, not grepped, and a setting is not a list.

    python tests/test_retarget.py

WHAT IT PROVES
  1. DECLARATIONS ARE PARSED. A year in a docstring does not appear; an
     assignment does. Shown on a folder built for it.

  2. IT FINDS THE REAL ONES. Every place in this repository that mirrors
     Directory.Build.props is listed, and the props file is first and
     marked as the source.

  3. A SETTING IS NOT A LIST. A declaration holding ONE release comes
     back uncertain, because "add 2028" to it would be a false
     instruction.

  4. A SUBSET IS NOT A FULL LIST EITHER, and what the runtime IMPLIES is
     stated instead of a guess.

  5. THE RUNTIME IS REQUIRED, NEVER DERIVED, and the reason is the props
     file's own error text.

  6. NOTHING IS WRITTEN AND NOTHING IS ACCEPTED - proved by comparing
     every file before and after.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_retarget as NUP                                   # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_dotnet as NET                                     # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def snapshot(where):
    out = {}
    for here, folders, names in os.walk(where):
        for name in names:
            if not name.endswith((".py", ".props")):
                continue
            full = os.path.join(here, name)
            stat = os.stat(full)
            out[os.path.relpath(full, where)] = (stat.st_size, stat.st_mtime)
    return out


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_retarget.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    yard = tempfile.mkdtemp(prefix="heron-retarget-test-")
    try:
        os.makedirs(os.path.join(yard, "brain"))
        with io.open(os.path.join(yard, "brain", "prose.py"), "w",
                     encoding="utf-8") as handle:
            handle.write('"""Heron supports 2020 through 2027, and 2028 is '
                         'not known."""\n\n\n'
                         "def about():\n"
                         '    """Revit 2025 moved the runtime."""\n'
                         '    return "2026"\n')
        with io.open(os.path.join(yard, "brain", "real.py"), "w",
                     encoding="utf-8") as handle:
            handle.write('ALL = ("2020", "2021", "2022", "2023", "2024",\n'
                         '       "2025", "2026", "2027")\n'
                         'ONE = "2024"\n'
                         'SOME = {"2025", "2026", "2027"}\n')

        print("\n1. declarations are parsed")
        prose = NUP.declared_in(os.path.join(yard, "brain", "prose.py"))
        check(prose == [],
              "a module that only TALKS about releases declares none: %s"
              % prose)
        real = NUP.declared_in(os.path.join(yard, "brain", "real.py"))
        check(sorted(one["symbol"] for one in real) == ["ALL", "ONE", "SOME"],
              "while three assignments are found: %s"
              % ", ".join(sorted(one["symbol"] for one in real)))
        check(dict((one["symbol"], one["line"])
                   for one in real)["ONE"] == 3,
              "each with its line number")

        print("\n2. it finds the real ones")
        found = NUP.places()
        check(found[0]["isSource"] is True,
              "the props file is first and marked as the source")
        check(found[0]["file"].endswith("Directory.Build.props"),
              "and it is %s" % found[0]["file"])
        symbols = set("%s.%s" % (one["file"], one["symbol"])
                      for one in found)
        for wanted in ("brain/heron_fragment.py.REVIT_VERSIONS",
                       "brain/heron_dotnet.py.RELEASES",
                       "brain/heron_dotnet.py.NEEDS_WINDOWS_DESKTOP",
                       "tools/check-api-surface.py.ALL_VERSIONS"):
            check(wanted in symbols, "%s is found" % wanted)
        check(len([one for one in found if one["isSource"]]) == 1,
              "exactly one source, and the rest mirror it")

        print("\n3. a setting is not a list")
        answer = NUP.plan("2028", "net10.0-windows", root=yard)
        by_symbol = dict((one["symbol"], one) for one in answer["edits"])
        check(by_symbol["ONE"]["certain"] is False,
              "a declaration holding ONE release is uncertain")
        check(by_symbol["ONE"]["add"] is None,
              "and it is given nothing to add")
        check("setting rather than a list" in by_symbol["ONE"]["why"],
              "with the reason: %r" % by_symbol["ONE"]["why"][:40])
        check(by_symbol["ALL"]["certain"] is True
              and by_symbol["ALL"]["add"] == "2028",
              "while a full list is certain, and gets the release")

        print("\n4. a subset is not a full list either")
        check(by_symbol["SOME"]["certain"] is False,
              "a subset is uncertain")
        check("chosen for a reason" in by_symbol["SOME"]["why"],
              "because the reason it is a subset is not in the data")
        check("net10.0-windows" in by_symbol["SOME"]["implies"],
              "and what the runtime implies is stated: %r"
              % by_symbol["SOME"]["implies"])
        check("windows-suffixed" in by_symbol["SOME"]["implies"]
              and ".NET 10" in by_symbol["SOME"]["implies"],
              "both halves of it")
        plain = NUP.plan("2028", "net48", root=yard)
        said = dict((one["symbol"], one) for one in plain["edits"])["SOME"]
        check("not windows-suffixed" in said["implies"],
              "and a non-windows runtime implies the opposite: %r"
              % said["implies"])
        check(len(answer["uncertain"]) == 2,
              "two of the three need a human eye (%d)"
              % len(answer["uncertain"]))

        print("\n5. the runtime is required, never derived")
        for runtime in (None, "", "   "):
            said = NUP.plan("2028", runtime, root=yard)
            reached.add(said.get("refused"))
            check(said.get("refused") == "NO_RUNTIME",
                  "runtime=%r is refused" % runtime)
        check("will not guess" in said["why"].lower()
              or "refuses to guess" in said["why"].lower(),
              "and the reason is the props file's own: %r"
              % said["why"][:46])
        check("FRAG.REVIT_VERSIONS[-1]" in logic
              and "runtime =" not in logic.split("def plan(")[1]
              .split("if not runtime")[0].replace(
                  'runtime = str(runtime or "").strip()', ""),
              "the module derives no runtime from the last release")

        print("\n6. nothing is written and nothing is accepted")
        before = snapshot(yard)
        NUP.plan("2028", "net10.0-windows", root=yard)
        check(snapshot(yard) == before,
              "every file is byte for byte and minute for minute as it was")
        check(answer["applied"] is False and answer["accepted"] is False,
              "`applied` and `accepted` are both false")
        check(len(answer["gates"]) == 2,
              "and two gates are named: %s"
              % ", ".join(g["gate"] for g in answer["gates"]))
        # IT DOES OPEN FILES - to READ them. The claim is that none of
        # those opens is a write, and the way to check that is every
        # call, not a slice of the source.
        import ast as _ast
        opens = [node for node in _ast.walk(_ast.parse(whole))
                 if isinstance(node, _ast.Call)
                 and getattr(getattr(node.func, "value", None), "id", "")
                 == "io" and getattr(node.func, "attr", "") == "open"]
        check(len(opens) >= 1, "the module does open files - %d call(s)"
                               % len(opens))
        modes = [one.value for node in opens for one in node.args[1:]
                 if isinstance(one, _ast.Constant)]
        check(all("w" not in str(mode) and "a" not in str(mode)
                  for mode in modes),
              "and not one of them is a write: modes %s"
              % (modes or "none given, which is read"))
        check('"w"' not in logic, "no write mode appears anywhere in it")

        print("\n7. every failure is named and reached")
        for release, runtime, name in (
                (None, "net48", "NO_RELEASE"),
                ("", "net48", "NO_RELEASE"),
                ("banana", "net48", "NOT_A_RELEASE"),
                ("28", "net48", "NOT_A_RELEASE"),
                (FRAG.REVIT_VERSIONS[0], "net48", "ALREADY_KNOWN"),
                (FRAG.REVIT_VERSIONS[-1], "net48", "ALREADY_KNOWN"),
                ("2028", "windows", "NOT_A_RUNTIME"),
                ("2028", "2028", "NOT_A_RUNTIME")):
            said = NUP.plan(release, runtime, root=yard)
            reached.add(said.get("refused"))
            check(said.get("refused") == name, "%s is reached" % name)
        check(NET.RELEASES and FRAG.REVIT_VERSIONS,
              "and both release tables the agent reads are real")

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-DEV-NUP-019.yaml"))
        named = contract.get("failures") or []
        check(len(named) == 5, "the contract declares 5 failures")
        for failure in named:
            check(failure in logic, "the code names %s" % failure)
        unreached = sorted(set(named) - reached)
        check(not unreached,
              "and every one was reached above%s"
              % ("" if not unreached else ": %s" % ", ".join(unreached)))
        check(len(answer["unjudged"]) == 7, "seven things are left unjudged")
    finally:
        shutil.rmtree(yard, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    parsed, not grepped, and a setting is not a list")
    return 0


if __name__ == "__main__":
    sys.exit(main())
