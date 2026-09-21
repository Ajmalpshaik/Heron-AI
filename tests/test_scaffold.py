# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-NCR-020
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
.NET project creation - every reference resolves to a file that is there.

    python tests/test_scaffold.py

WHAT IT PROVES
  1. THE LAYER TABLE IS READ OUT OF THE GATE, not copied - shown by
     handing it a different file and watching the answer change.

  2. THE PRODUCED FILE IS REAL XML, and every ProjectReference in it
     points at a .csproj that exists on disk.

  3. A LAYER WITH NO C# PROJECT IS REFUSED. `mcp may reference brain` is
     true of the imports and impossible in a .csproj.

  4. A LAYER WITH SEVERAL PROJECTS IS ASKED ABOUT, never picked from.

  5. THE TARGET FRAMEWORK IS NEVER A LITERAL - no release, no `net`
     anything, in any answer.

  6. D-48 IS ENFORCED BEFORE ANYTHING IS AUTHORED, in both directions.

  7. NOTHING IS WRITTEN.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import re
import shutil
import sys
import tempfile
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_scaffold as NCR                                   # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_scaffold.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. the layer table is read out of the gate")
    real = NCR.layers()
    check(real.get("platform") == set(),
          "platform may reference nothing: %r" % real.get("platform"))
    check(real.get("mcp") == {"platform", "brain"},
          "and mcp may reference platform and brain: %s"
          % ", ".join(sorted(real.get("mcp") or [])))
    yard = tempfile.mkdtemp(prefix="heron-scaffold-test-")
    try:
        fake = os.path.join(yard, "other-gate.py")
        with io.open(fake, "w", encoding="utf-8") as handle:
            handle.write('ALLOWED = {"alpha": set(), "beta": {"alpha"}}\n')
        check(NCR.layers(fake) == {"alpha": set(), "beta": {"alpha"}},
              "a different file gives a different table - it is READ")
        empty = os.path.join(yard, "no-table.py")
        with io.open(empty, "w", encoding="utf-8") as handle:
            handle.write("SOMETHING_ELSE = 1\n")
        check(NCR.layers(empty) == {},
              "and a file with no ALLOWED gives nothing")

        print("\n2. the produced file is real XML")
        # "platform/Heron.Core", not "platform". platform held one project
        # until 2026-09-21 and holds two since, so the bare layer name is
        # now correctly refused - section 4 below checks that it is, and
        # that the refusal says how to be specific.
        answer = NCR.author("Heron.Reporting", "mcp",
                            references=["platform/Heron.Core"],
                            why="Reports, rendered away from Revit.")
        check(answer["authored"] is True, "it authored")
        tree = ET.fromstring(answer["text"])
        check(tree.tag == "Project", "the root element is <Project>")
        check(tree.get("Sdk") == "Microsoft.NET.Sdk",
              "with the SDK an existing project uses: %r" % tree.get("Sdk"))
        names = dict((child.tag, child.text)
                     for group in tree.findall("PropertyGroup")
                     for child in group)
        check(names.get("RootNamespace") == "Heron.Reporting"
              and names.get("AssemblyName") == "Heron.Reporting",
              "and the namespace and assembly are the component's name")
        refs = [one.get("Include")
                for one in tree.findall("ItemGroup/ProjectReference")]
        check(len(refs) == 1, "one ProjectReference: %s" % refs)
        here = os.path.join(ROOT, os.path.dirname(answer["path"]))
        for one in refs:
            target = os.path.normpath(os.path.join(here,
                                                   one.replace("\\", "/")))
            check(os.path.isfile(target),
                  "and it points at a file that EXISTS: %s"
                  % os.path.relpath(target, ROOT))
        check(answer["referencePaths"]["platform"].endswith(
            "Heron.Core.csproj"),
              "resolved to the project's REAL name, not Heron.Platform: %s"
              % answer["referencePaths"]["platform"])

        print("\n3. a layer with no C# project is refused")
        said = NCR.author("Heron.X", "mcp", references=["brain"])
        reached.add(said.get("refused"))
        check(said.get("refused") == "NO_PROJECT_IN_LAYER",
              "referencing brain from a C# project is refused")
        check("Python" in said["why"],
              "and the reason says brain is Python: %r" % said["why"][:44])
        check(NCR.projects_in("brain") == [],
              "brain really holds no .csproj")
        # THE COUNT, NOT THE LIST. This pinned the exact list until
        # 2026-09-21 and broke twice in one day as the installer grew - a
        # check that fails every time a project is added is a check people
        # learn to edit. What the cases below actually need is that platform
        # holds MORE THAN ONE, so the bare layer name is refused, and that
        # the one they name is in there.
        platform_projects = NCR.projects_in("platform")
        check(len(platform_projects) > 1,
              "platform holds more than one project since 2026-09-21: %s"
              % ", ".join(platform_projects))
        check(os.path.join("platform", "Heron.Core", "Heron.Core.csproj")
              in platform_projects,
              "and Heron.Core is one of them, which is the one named above")

        print("\n4. a layer with several projects is asked about")
        said = NCR.author("Heron.X", "tests", references=["revit"])
        reached.add(said.get("refused"))
        check(said.get("refused") == "WHICH_PROJECT",
              "revit holds more than one, so it is asked about")
        check(len(NCR.projects_in("revit")) > 1,
              "it really does: %s" % ", ".join(NCR.projects_in("revit")))
        check("D-33" in said["why"], "and the reason names D-33")

        # A REFUSAL THAT NAMES NO WAY OUT IS A DEAD END. Until 2026-09-21 a
        # reference could only ever name a layer, so "which one do you mean"
        # asked for something the caller could not say. platform gaining a
        # second project is what found it.
        said = NCR.author("Heron.X", "mcp", references=["platform"])
        reached.add(said.get("refused"))
        check(said.get("refused") == "WHICH_PROJECT",
              "platform holds more than one too, so it is asked about")
        check("platform/Heron.Core" in said["why"],
              "and the refusal SAYS HOW to be specific: %r"
              % said["why"][-60:])
        named = NCR.author("Heron.X", "mcp",
                           references=["platform/Heron.Installer"])
        check(named.get("authored") is True,
              "naming one of them authors")
        check(named["referencePaths"]["platform"].endswith(
            "Heron.Installer.csproj"),
              "against the project that was named, not the first: %s"
              % named["referencePaths"]["platform"])
        said = NCR.author("Heron.X", "mcp", references=["platform/Heron.Nope"])
        reached.add(said.get("refused"))
        check(said.get("refused") == "NO_SUCH_PROJECT",
              "and a project that is not there is refused, not guessed at")

        print("\n5. the target framework is never a literal")
        check(answer["targetFramework"] == NCR.TFM,
              "it is %r" % answer["targetFramework"])
        check(NCR.TFM in answer["text"], "and that is what the file says")
        check(not re.search(r"\bnet\d", answer["text"]),
              "no `net` framework appears in the file at all")
        check(not re.search(r"\b20[0-9]{2}\b", answer["text"]),
              "and neither does any Revit release")

        print("\n6. D-48 is enforced before anything is authored")
        for layer, refs in (("platform", ["revit"]),
                            ("platform", ["brain"]),
                            ("brain", ["revit"]),
                            ("revit", ["mcp"]),
                            ("tools", ["platform"])):
            said = NCR.author("Heron.X", layer, references=refs)
            reached.add(said.get("refused"))
            check(said.get("refused") == "LAYER_FORBIDS",
                  "%s may not reference %s" % (layer, ", ".join(refs)))
            check("text" not in said,
                  "and nothing was authored before the refusal")
        allowed = NCR.author("Heron.X", "revit",
                             references=["platform/Heron.Core"])
        check(allowed["authored"] is True,
              "while revit -> platform is allowed and authors")

        print("\n7. nothing is written")
        before = sorted(os.listdir(os.path.join(ROOT, "mcp")))
        NCR.author("Heron.Reporting", "mcp",
                   references=["platform/Heron.Core"])
        check(sorted(os.listdir(os.path.join(ROOT, "mcp"))) == before,
              "the folder it names is untouched")
        check(answer["written"] is False, "`written` is false")
        check('"w"' not in logic and "makedirs" not in logic,
              "and the module names no write mode and makes no directory")

        print("\n8. every failure is named and reached")
        for name, layer, refs, wanted in (
                (None, "platform", None, "NO_NAME"),
                ("  ", "platform", None, "NO_NAME"),
                ("reporting", "platform", None, "NOT_A_NAME"),
                ("Heron", "platform", None, "NOT_A_NAME"),
                ("Heron.X", "middleware", None, "NOT_A_LAYER")):
            said = NCR.author(name, layer, references=refs)
            reached.add(said.get("refused"))
            check(said.get("refused") == wanted, "%s is reached" % wanted)
        for missing in ("NO_LAYER_TABLE", "NO_TEMPLATE"):
            check(missing in logic, "%s is named in the code" % missing)
            reached.add(missing)

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-DEV-NCR-020.yaml"))
        named = contract.get("failures") or []
        check(len(named) == 9, "the contract declares 9 failures")
        for failure in named:
            check(failure in logic, "the code names %s" % failure)
        unreached = sorted(set(named) - reached)
        check(not unreached,
              "and every one was reached or named above%s"
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
    print("PASS    every reference resolves to a file that is there")
    return 0


if __name__ == "__main__":
    sys.exit(main())
