# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-NET-006
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
.NET compatibility - it compiles nothing, and the gate uses its answers.

    python tests/test_dotnet.py

WHAT IT PROVES
  1. IT STARTS NO BUILD. Not by word search - by taking `dotnet` off the
     PATH and showing the agent still answers, refusing rather than
     hanging, and by there being no `dotnet build` anywhere in it.

  2. THE GATE USES THIS AGENT'S FACTS, not a second copy. Every constant
     and probe tools/check-compile.py names is the SAME OBJECT.

  3. THE MIRRORED TABLES ARE CHECKED AGAINST THE PROPS, and drift in
     either direction is found - shown with a props table that has
     moved.

  4. THE RUNTIME TABLE IS READ, NOT RETYPED - the same eight releases
     Directory.Build.props gives, through HERON-FRG-MTX-009.

  5. AN UNLISTED RELEASE IS AN ERROR, NEVER A GUESS (D-05), and an empty
     list is not the same request as no list.

  6. "COULD NOT BUILD HERE" IS NOT "UNSUPPORTED", and every blocked
     release says what is missing.

  7. NOTHING IS RESOLVED OR FETCHED - the packages come back as written.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tools"))

import heron_dotnet as NET                                     # noqa: E402
import heron_matrix as MTX                                     # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def without_dotnet(run):
    """Call `run` with the SDK off the PATH. Really off it, not mocked."""
    was = os.environ.get("PATH", "")
    os.environ["PATH"] = os.path.join(ROOT, "no-such-bin")
    try:
        return run()
    finally:
        os.environ["PATH"] = was


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_dotnet.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. it starts no build")
    check("\"build\"" not in logic and "'build'" not in logic,
          "no subprocess argument in this module is `build`")
    blind = without_dotnet(lambda: NET.check(["2024"]))
    reached.add(blind.get("refused"))
    check(blind.get("refused") == "NO_DOTNET",
          "with dotnet off the PATH it refuses, having run nothing")
    check(without_dotnet(lambda: NET.installed_sdks()) == [],
          "and the SDK probe comes back empty rather than raising")
    answer = NET.check()
    check(answer.get("compiled") is False,
          "and a successful answer says compiled: false")

    print("\n2. the gate uses this agent's facts")
    import importlib.util
    spec = importlib.util.spec_from_file_location(
        "check_compile", os.path.join(ROOT, "tools", "check-compile.py"))
    gate = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(gate)
    for name, mine in (("PROJECTS", NET.PROJECTS),
                       ("ALL_VERSIONS", NET.RELEASES),
                       ("NEEDS_WINDOWS_DESKTOP", NET.NEEDS_WINDOWS_DESKTOP),
                       ("MINIMUM_DOTNET_MAJOR", NET.MINIMUM_DOTNET_MAJOR),
                       ("UNKNOWN_TOOLCHAIN", NET.UNKNOWN_TOOLCHAIN),
                       ("have_dotnet", NET.have_dotnet),
                       ("installed_sdks", NET.installed_sdks),
                       ("windows_desktop_toolchain",
                        NET.windows_desktop_toolchain),
                       ("why_unbuildable", NET.why_unbuildable)):
        check(getattr(gate, name) is mine,
              "check-compile's %s IS the agent's - one copy" % name)
    check(hasattr(gate, "build") and gate.build.__module__ != "heron_dotnet",
          "and `build` stayed in the gate, where compiling belongs")

    print("\n2a. the gate still needs nothing but the standard library")
    # THE REGRESSION CI FOUND ON 2026-09-15. Moving these facts out of
    # check-compile.py hoisted `import heron_matrix` to the top of this
    # module; that reaches heron_fragment, which imports PyYAML. The CI
    # compile job installs a .NET SDK and nothing else, so the gate died
    # with ModuleNotFoundError in under a second, before one project was
    # built. The probe half must import with the standard library alone.
    import importlib.abc
    import subprocess as _sub

    blocked = _sub.run(
        [sys.executable, "-c",
         "import sys, importlib.abc\n"
         "class Block(importlib.abc.MetaPathFinder):\n"
         "    def find_spec(self, name, path=None, target=None):\n"
         "        if name == 'yaml' or name.startswith('yaml.'):\n"
         "            raise ModuleNotFoundError(\"No module named 'yaml'\")\n"
         "        return None\n"
         "sys.meta_path.insert(0, Block())\n"
         "sys.path.insert(0, %r)\n"
         "import heron_dotnet as NET\n"
         "assert NET.why_unbuildable('2020', None) is None\n"
         "assert NET.RELEASES and NET.PROJECTS\n"
         "print('ok')\n" % os.path.join(ROOT, "brain")],
        stdout=_sub.PIPE, stderr=_sub.STDOUT)
    check(blocked.returncode == 0,
          "the module imports and probes with PyYAML BLOCKED: %s"
          % blocked.stdout.decode("utf-8", "replace").strip().splitlines()[-1])
    check("import heron_matrix" not in logic.split("def _table(")[0],
          "and heron_matrix is not imported at module level")
    check("import heron_matrix" in logic,
          "it is imported inside the one function that reads the props")

    print("\n3. the mirrored tables are checked against the props")
    check(NET.disagreements() == [],
          "as they stand today, every row agrees")
    moved = dict(MTX.runtimes())
    moved["2024"] = "net8.0-windows"
    found = NET.disagreements(moved)
    fields = sorted(set(one["field"] for one in found))
    check("NEEDS_WINDOWS_DESKTOP" in fields,
          "a release becoming a -windows target is found: %s"
          % ", ".join(fields))
    check(any(one["release"] == "2024" for one in found),
          "and it names 2024, the row that moved")
    added = dict(MTX.runtimes())
    added["2028"] = "net10.0-windows"
    check(any(one["field"] == "RELEASES" and one["release"] == "2028"
              for one in NET.disagreements(added)),
          "a release in the props that RELEASES does not list is found")
    dropped = dict(MTX.runtimes())
    del dropped["2027"]
    check(any(one["field"] == "RELEASES" and one["release"] == "2027"
              for one in NET.disagreements(dropped)),
          "and so is one listed here that the props no longer target")
    wrong = dict(MTX.runtimes())
    wrong["2026"] = "net10.0-windows"
    check(any(one["field"] == "MINIMUM_DOTNET_MAJOR"
              for one in NET.disagreements(wrong)),
          "a changed .NET major is found too")

    print("\n4. the runtime table is read, not retyped")
    table = MTX.runtimes()
    check(sorted(table) == sorted(NET.RELEASES),
          "Directory.Build.props and RELEASES name the same %d releases"
          % len(NET.RELEASES))
    by_release = dict((card["release"], card)
                      for card in answer["buildable"] + answer["blocked"])
    for release in NET.RELEASES:
        check(by_release[release]["tfm"] == table[release],
              "%s -> %s, from the props" % (release, table[release]))

    print("\n5. an unlisted release is an error, never a guess")
    for these in (["2028"], ["2019"], ["2024", "2031"]):
        said = NET.check(these)
        reached.add(said.get("refused"))
        check(said.get("refused") == "UNKNOWN_RELEASE", "%r is refused"
              % (these,))
    check("D-05" in said["why"], "and the reason names D-05")
    empty = NET.check([])
    reached.add(empty.get("refused"))
    check(empty.get("refused") == "NOTHING_TO_CHECK",
          "an EMPTY list is refused, not read as all of them")
    check(NET.check(None)["of"] == len(NET.RELEASES),
          "while no list at all means all %d" % len(NET.RELEASES))

    print("\n6. could not build here is not unsupported")
    blocked = NET.check(table=table)["blocked"]
    for card in blocked:
        check(card["why"] and "unsupported" not in card["why"].lower(),
              "%s says what is MISSING, not that it is unsupported"
              % card["release"])
    check(len(answer["buildable"]) + len(answer["blocked"]) == answer["of"],
          "every release asked about is in exactly one of the two (%d)"
          % answer["of"])
    # The one case that must not read as absence.
    check(NET.why_unbuildable("2027", NET.UNKNOWN_TOOLCHAIN) is None,
          "an unreadable SDK list does not block a release - ignorance is "
          "not absence, and the build error is more honest than a guess")
    check(NET.why_unbuildable("2027", None) is not None,
          "but no desktop toolchain at all does block it")
    check(NET.why_unbuildable("2020", None) is None,
          "and a release that needs no desktop targets is never blocked")

    print("\n7. nothing is resolved or fetched")
    names = dict((ref["package"], ref) for ref in answer["packages"])
    check("Nice3point.Revit.Api.RevitAPI" in names,
          "the Revit API package is reported: %s"
          % ", ".join(sorted(names)))
    check(names["Nice3point.Revit.Api.RevitAPI"]["version"]
          == "$(RevitVersion).*",
          "with its version expression AS WRITTEN, not resolved")
    check(names["Nice3point.Revit.Api.RevitAPI"]["follows_release"] is True,
          "and marked as following the release")
    check(not [r for r in answer["packages"]
               if r["package"] == "Microsoft.CodeAnalysis.CSharp.Scripting"
               and r["follows_release"]],
          "a fixed-version package is not marked as following it")

    print("\n8. every failure is named and reached")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-DEV-NET-006.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 3, "the contract declares 3 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(answer["unjudged"]) == 5, "five things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    it compiles nothing, and the gate uses its answers")
    return 0


if __name__ == "__main__":
    sys.exit(main())
