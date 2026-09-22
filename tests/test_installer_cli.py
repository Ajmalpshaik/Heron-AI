#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The command line door - the thing routes 1 and 2 knock on. Q-PE-16.

WHAT THIS PROVES
----------------
The door's own DECIDING is checked by tests/test_installer_engine.py, which
runs Arguments for real against a fake Revit. What is checked here is the
shape of the file around it, which no run can show:

    the source is judged BEFORE anything on this PC is touched - so a
      hostile address is refused on a machine with no Revit, and no
      PowerShell has run by the time it is
    it NEVER removes - R-21 makes unticking mean uninstall, and there is
      no unticking on a command line, so Install is called and never Apply
    it holds no rules - the greying, the plan and the engine are all
      somewhere a test can reach
    it names no product and no Revit release, so adding either stays a
      line in the manifest - R-3
    it never writes "Release" or "Debug" - the configuration comes from
      the deployer, because two copies of it have already disagreed once
    it is release-independent and does not reference Heron.Core
    no Autodesk assembly is anywhere near it

WHAT IT CANNOT PROVE
--------------------
That it installs anything. It needs Windows, PowerShell and a Revit. Those
rows are in docs/NEEDS-CHECKING.md.

    python tests/test_installer_cli.py
"""

import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CLI = os.path.join(ROOT, "platform", "Heron.Installer.Cli")
ENTRY = os.path.join(CLI, "Program.cs")
ARGS = os.path.join(CLI, "Arguments.cs")
PROJ = os.path.join(CLI, "Heron.Installer.Cli.csproj")
APP_PROJ = os.path.join(ROOT, "platform", "Heron.Installer.App",
                        "Heron.Installer.App.csproj")
MANIFEST = os.path.join(ROOT, "platform", "heron-products.json")

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    return io.open(path, encoding="utf-8").read()


def code(path):
    """The file with its commentary taken out.

    EVERY CHECK BELOW IS ABOUT CODE, SO IT HAS TO READ CODE. The first draft
    of this suite read whole files, and three checks went red against a door
    that was correct: Arguments.cs explains in a doc comment that InstallSource
    does the judging, the .csproj explains that it does NOT reference
    Heron.Core, and both say the word "Error" while saying never to print it.

    A check that a word is absent is not a check that a rule is kept - it is
    the same mistake this branch already made once, testing a variable name
    instead of a rule. Prose that describes a rule must not be able to break
    the check for it, or the only safe comment is no comment.

    // and /// to end of line, and <!-- --> for a .csproj. Not a parser: a
    // inside a string literal would be cut too. Nothing here has one, and a
    // check reading less than it should fails loudly rather than passing
    // quietly, which is the right direction for this to be wrong in.
    """
    text = read(path)
    text = re.sub(r"<!--.*?-->", "", text, flags=re.S)
    return re.sub(r"//.*", "", text)


def main():
    entry = read(ENTRY)
    args = read(ARGS)
    proj = read(PROJ)

    print("THE COMMAND LINE DOOR - routes 1 and 2 (Q-PE-16)")
    print()
    print("  the source is judged before anything on this PC is touched")
    # A refusal that arrives after PowerShell has been run is a refusal that
    # ran something first. The first draft had these the other way round,
    # because that is the order the window does things in - and the window
    # has no untrusted input at that point, which is this door's whole
    # reason for existing.
    judged = entry.index("InstallSource.Judge")
    touched = entry.index("new PowerShellRevitEnvironment")
    check(judged < touched,
          "InstallSource.Judge comes before PowerShellRevitEnvironment is built")
    check(entry.index("Arguments.Read") < judged,
          "and the line is read before either, so a typo never reaches the gate")

    print()
    print("  it installs, and it never removes - R-21")
    # Unticking uninstalls, and unticking is a gesture a person makes in a
    # window having been asked to confirm. A product left out of --products
    # was not unticked; it was not mentioned.
    check(".Remove(" not in code(ENTRY),
          "the door never calls Remove")
    check("engine.Install(" in entry,
          "it calls Install, which is the overload that hands Apply no removals")
    check("engine.Apply(" not in code(ENTRY),
          "and never Apply, which is the one that takes a removal list")
    check("ToRemove" not in code(ENTRY) and "WhatWillBeRemoved" not in code(ENTRY),
          "and it never asks the screen what would be removed")

    print()
    print("  it holds no rules")
    check("InstallerScreen.Build" in entry,
          "what may be offered is the screen's answer")
    check("InstallerScreen.AnythingBuilt" in entry,
          "and which route the disk puts it on is the screen's answer too, "
          "not a copy of the window's")
    check("InstallSource.Judge" in entry,
          "and whether an address may be used at all is InstallSource's")
    check("HasBuild(" not in code(ENTRY),
          "it never decides for itself whether something is built")
    check("SupportsRevit(" not in code(ENTRY),
          "nor whether a product supports a release")

    print()
    print("  it names no product and no Revit release - R-3")
    # Adding a product is a line in heron-products.json. A door that names
    # one is a door that needs recompiling to ship it.
    # CODE, NOT COMMENTARY - the same lesson three checks in this suite
    # already learned. "2026" went red the moment a comment cited D-85's date
    # of 2026-09-20, which is a date rather than a Revit release. A comment
    # explaining a rule must not be able to break the check for it.
    door = code(ENTRY)
    for named in ("heron-ai-bridge", "heron-doc", "heron-tools", "heron-mep",
                  "2020", "2024", "2025", "2026", "2027"):
        check(named not in door,
              "'%s' appears nowhere in the door" % named)

    print()
    print("  the ribbon tab is read from the manifest, never typed")
    # D-85 renamed the tab on 2026-09-20 and counted EIGHT printed strings
    # that had to move with it. One did not - tools/setup.ps1 line 183 sent
    # every new user to a tab that does not exist for two days, until row
    # 5b-138 caught it (#273). A typed tab name here would be a ninth string
    # waiting to do the same.
    # AND THESE THREE WERE WORTHLESS FIRST, WHICH IS WHY THEY READ AS THEY DO.
    # The first pair were: "product.Tab is in the file" and '"Heron tab" is
    # not'. Typing the tab name straight back into the printed line broke
    # NEITHER - the TabsFor method stayed in the file unused, so the first
    # still matched, and the second looked for a quoted string that could
    # never occur. Both breaks stayed GREEN.
    #
    # A guard that cannot be shown to fire is a comment. This branch had
    # already learned that once, on the user-info check in InstallSource.
    door = code(ENTRY)
    tabs = sorted(set(p.get("tab") for p in
                      json.loads(read(MANIFEST).lstrip("\ufeff"))["products"]
                      if p.get("tab")))
    check(len(tabs) > 0,
          "the manifest carries tab names for the door to read: %s"
          % ", ".join(tabs))
    check(door.count("TabsFor(") >= 2,
          "TabsFor is DEFINED AND CALLED - a method left in the file while "
          "the printed line goes back to a typed name is the break that "
          "stayed green")
    typed = [t for t in tabs if ("%s tab" % t) in door]
    check(not typed,
          "and no tab name is typed beside the word tab%s"
          % ("" if not typed else " - found: " + ", ".join(typed)))
    check("Heron AI" not in door,
          "and the name D-85 retired in 2026 appears nowhere - that exact "
          "string sat in setup.ps1 for two days sending users to a tab that "
          "does not exist")

    print()
    print("  the configuration is asked for, never typed")
    # "Built" means nothing without a configuration: Debug and Release are
    # two folders and the deploy script reads exactly one. Writing it out a
    # second time is how the two come to disagree, and that has already cost
    # a round trip once.
    check('"Release"' not in code(ENTRY) and '"Debug"' not in code(ENTRY),
          "neither configuration is written out in the door")
    check("localDeployer.Configuration" in entry,
          "it takes the configuration from the deployer instead")

    print()
    print("  the reader decides nothing about installing")
    # Arguments is the untrusted input on this door. It is string handling
    # and nothing else: a second gate in there would be one more thing to
    # keep in step with InstallSource.
    check("InstallSource.Judge" not in code(ARGS),
          "Arguments never calls the gate - it hands the text on for the gate "
          "to judge, so there is only ever one gate to keep in step")
    check("github" not in code(ARGS).lower(),
          "and never mentions GitHub, so it cannot grow an opinion about hosts")
    check("File." not in code(ARGS) and "Directory." not in code(ARGS),
          "and it opens no file")
    check("Process" not in code(ARGS) and "Http" not in code(ARGS),
          "and starts nothing and fetches nothing")

    print()
    print("  the refusals say what to do next - docs/14")
    check("error" not in code(ARGS).lower(),
          "no refusal the reader prints says 'error'")
    check("Q-PE-12" in entry or "not been decided" in entry,
          "and --from says plainly that route 2 is not decided yet, rather "
          "than failing in a way that looks like a bug")

    print()
    print("  release-independent, and nowhere near Revit")
    check("Heron.Core" not in code(PROJ),
          "it does not reference Heron.Core, which follows the Revit release "
          "and would pin a release-independent thing to one")
    check("Heron.Installer.csproj" in proj,
          "it references the engine")
    check("Autodesk" not in entry and "Autodesk" not in args and "Autodesk" not in proj,
          "no Autodesk assembly anywhere near it")
    check("<TargetFramework>net8.0</TargetFramework>" in proj,
          "net8.0, not net8.0-windows - it draws nothing, so it runs and is "
          "testable on the machine this is developed on")
    check("UseWPF" not in proj, "and it is not a window")
    check("RollForward" in proj,
          "and it rolls forward, so a machine with only a later .NET runs it "
          "rather than naming a framework the user has never heard of")

    print()
    print("  its name cannot be mistaken for the window's")
    # HeronInstaller.exe is the file in a Downloads folder. Two names a
    # couple of letters apart is a thing somebody double-clicks the wrong
    # one of - and one of these draws a window to read while the other does
    # the whole install with nothing to confirm.
    check("<AssemblyName>heron-install</AssemblyName>" in proj,
          "the command is called heron-install")
    check("<AssemblyName>HeronInstaller</AssemblyName>" in read(APP_PROJ),
          "and the window is still HeronInstaller")
    check("heron-install" != "HeronInstaller".lower(),
          "and the two are not the same word in any casing")

    print()
    print("  it is registered in both places a project has to be")
    props = read(os.path.join(ROOT, "Directory.Build.props"))
    check("'Heron.Installer.Cli'" in props,
          "Directory.Build.props knows it is release-independent, so it is "
          "built once rather than eight times")
    dotnet = read(os.path.join(ROOT, "brain", "heron_dotnet.py"))
    check("platform/Heron.Installer.Cli/Heron.Installer.Cli.csproj" in dotnet,
          "and heron_dotnet.py builds it, so it cannot break unnoticed")

    print()
    if FAILURES:
        print("FAILED (%d)" % len(FAILURES))
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("The door judges the address before it touches the machine, holds no")
    print("rule of its own, names no product, and never removes anything.")
    print()
    print("IT HAS INSTALLED NOTHING. This is a text check on source, plus the")
    print("argument reader run for real in tests/test_installer_engine.py.")
    print("Whether it installs into a Revit is owed on Windows -")
    print("docs/NEEDS-CHECKING.md.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
