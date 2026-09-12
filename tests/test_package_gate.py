#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The packaging gate. Runs without Windows, without Revit and without a compiler.

WHAT IT PROVES, AND WHAT IT CANNOT
-----------------------------------
It proves the gate SAYS NO to the four delivery faults that are invisible to
every other check in this repository and fatal on a modeller's machine:

    a FullClassName that names no class      Revit refuses, and says nothing useful
    an Assembly the project does not build   the same
    a <ManifestSettings> element             CRASHES Revit 2025 and older
    a manifest the deploy rewrite misses     installs pointing at the wrong path

It proves nothing about whether Heron installs. Nothing here can; that needs
Windows and a Revit, and the gate's own output says so every run.

    python tests/test_package_gate.py
"""

import importlib.util
import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load(name, filename):
    path = os.path.join(ROOT, "tools", filename)
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


PKG = load("heron_check_package", "check-package.py")

GOOD = """<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>Heron AI</Name>
    <Assembly>Heron.Revit.Addin.dll</Assembly>
    <AddInId>7A1F4C62-9D3E-4B18-8E52-1C0A6F2D5B41</AddInId>
    <FullClassName>Heron.Revit.Addin.HeronApplication</FullClassName>
    <VendorId>AJPS</VendorId>
  </AddIn>
</RevitAddIns>
"""

CLASSES = {"Heron.Revit.Addin.HeronApplication":
           {"file": "revit/Heron.Revit.Addin/HeronApplication.cs",
            "bases": ["IExternalApplication"]},
           "Heron.Revit.Addin.IconLoader":
           {"file": "revit/Heron.Revit.Addin/IconLoader.cs", "bases": []}}


def said_no(raw, about, classes=None, assembly="Heron.Revit.Addin"):
    problems, _ = PKG.manifest_problems(raw, assembly, classes or CLASSES)
    return any(about in p for p in problems)


def main():
    print("The manifest as it stands")
    problems, asked = PKG.manifest_problems(GOOD, "Heron.Revit.Addin", CLASSES)
    check(not problems, "a correct manifest raises nothing")
    check(asked >= 8, "and it was actually asked several questions (%d)" % asked)

    print()
    print("The four faults that are fatal and otherwise invisible")
    check(said_no(GOOD.replace("HeronApplication</FullClassName>",
                               "HeronApplicaton</FullClassName>"),
                  "names no class"),
          "a mistyped FullClassName is refused - Revit would only say it cannot "
          "run the external application")
    check(said_no(GOOD, "does not implement IExternalApplication",
                  classes={"Heron.Revit.Addin.HeronApplication":
                           {"file": "x.cs", "bases": ["IExternalCommand"]}}),
          "a class that exists but is the wrong kind is refused too")
    check(said_no(GOOD, "builds 'Heron.Other.dll'", assembly="Heron.Other"),
          "an Assembly the project does not build is refused")
    check(said_no(GOOD.replace("</AddIn>",
                               "<ManifestSettings><Isolation>Full</Isolation>"
                               "</ManifestSettings></AddIn>"),
                  "CRASH"),
          "<ManifestSettings> is refused, because one manifest is deployed to "
          "every release and Revit 2025 and older crash reading it")

    print()
    print("The shape Revit requires")
    check(said_no(GOOD.replace("<VendorId>AJPS</VendorId>", ""), "has no <VendorId>"),
          "a missing required element is named, not skipped")
    check(said_no(GOOD.replace("7A1F4C62-9D3E-4B18-8E52-1C0A6F2D5B41", "not-a-guid"),
                  "is not a GUID"),
          "an AddInId that is not a GUID is refused")
    problems, _ = PKG.manifest_problems("<RevitAddIns><AddIn>", None, CLASSES)
    check(any("not well-formed" in p for p in problems),
          "a manifest that does not parse stops there rather than producing a "
          "confident report from half a file")

    print()
    print("Release-to-runtime, evaluated rather than searched")
    versions = PKG.releases()
    check(versions and versions[0] == "2020" and versions[-1] == "2027",
          "the supported list comes from check-compile.py, not a second copy here")
    mapped, unreadable = PKG.runtime_map(PKG.read(PKG.PROPS), versions)
    check(not unreadable, "every condition in Directory.Build.props could be read")
    check(mapped.get("2020") == "net472", "2020 resolves to net472")
    check(mapped.get("2022") == "net48" and mapped.get("2023") == "net48",
          "2022 and 2023 resolve through the RANGE row - the first version of "
          "this check searched for the literal year and raised both of them")
    check(mapped.get("2025") == "net8.0-windows" and mapped.get("2027") == "net10.0-windows",
          "2025 and 2027 resolve to the runtimes Autodesk moved to")
    check(all(mapped.get(v) for v in versions),
          "and every supported release resolves to something")

    missing, _ = PKG.runtime_map(
        "<HeronTfm Condition=\"'$(RevitVersion)' == '2020'\">net472</HeronTfm>",
        versions)
    check(not missing.get("2024"),
          "a release with no matching row is reported missing, so dropping one "
          "cannot pass quietly")
    _, unreadable = PKG.runtime_map(
        "<HeronTfm Condition=\"somebody wrote prose here\">net48</HeronTfm>",
        versions)
    check(unreadable,
          "a condition this cannot parse is reported, never assumed true")

    print()
    print("End to end, against the repository as it is")
    out = subprocess.run([sys.executable, os.path.join(ROOT, "tools", "check-package.py")],
                         capture_output=True, text=True, cwd=ROOT)
    check(out.returncode == 0, "the gate passes on the current tree")
    check("STILL OWED" in out.stdout,
          "and still prints what it cannot answer - a green run here is not an "
          "install and never reports itself as one")

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1
    print("PASSED - the gate says no to the four faults nothing else here sees.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
