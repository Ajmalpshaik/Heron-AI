#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A picture's longest side is the width asked, and a family's frame is its parts.

    python tests/test_picture_fit.py

WHY THIS EXISTS
---------------
FRAGMENT-ISSUES row 5b-1890, issue #1112 (from field report #1051, item A5,
Revit 2020, 2026-11-19). revit_view_image of a rod hanger about 760 x 140 mm,
pictured as its View 1, came back 1000 x 5159 px: the export fitted the width
asked and let the height follow the view. And with `elements=` the frame
zoomed out to a context slab crossing the section box, so the small parts
could not be shown close up.

TWO HALVES, AND ONE OF THEM CANNOT BE RUN HERE
-----------------------------------------------
  THE RULES     when a picture is taken again fitted on its height, and which
                element reaches out of the section box - HeronPicture.cs,
                which names no Autodesk type and is linked by source into
                tests/Heron.Picture.TestHost. Built and RUN here.
  THE WIRING    that RevitCapture exports once fitted on its width, asks the
                rule, and only then exports again fitted on its height,
                inside the run guard's door and before the group's rollback;
                that a family's elements picture hides what reaches out, and
                a project's does not; and that the reply carries what the
                server reads. That half calls the Revit API, so it is read
                as TEXT.

Neither half proves Revit then does what it is told. That is the PC's,
NEEDS-CHECKING Group PM.

MEASURED AGAINST THE CODE AS IT STOOD: with RevitCapture.cs from before the
change and no HeronPicture.cs, every wiring check fails and the host does not
build - and this suite says so and exits 1 rather than raising.

IT EXITS 3 WHEN .NET IS ABSENT AND THE WIRING PASSED, and that is NOT a pass:
the rules were not run.
"""

import io
import os
import re
import subprocess
import sys

for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass  # already unicode-safe, or redirected to something that cannot

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

# The comment-and-string stripper, the brace walker and the framework picker
# test_failure_note.py already proved - imported, not copied.
import test_failure_note as NOTE                                # noqa: E402

ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
CAPTURE = os.path.join(ADDIN, "RevitCapture.cs")
PICTURE = os.path.join(ADDIN, "HeronPicture.cs")
HOST = os.path.join(ROOT, "tests", "Heron.Picture.TestHost")
COULD_NOT_RUN = 3

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def wiring():
    raw = NOTE.read(CAPTURE) or ""
    check(bool(raw), "RevitCapture.cs is there to read")
    code = NOTE.code_only(raw)

    print("\n1. a tall picture is taken again fitted on its height")
    exported = NOTE.body_of(code, "static string Exported(")
    check(bool(exported), "the export is there")
    first = exported.find("ExportOnce(")
    sized = exported.find("HeronPicture.Size(")
    tall = exported.find("HeronPicture.TooTall(")
    again = exported.find("ExportOnce(", first + 1) if first >= 0 else -1
    check(0 <= first < sized < tall < again,
          "it exports once, reads the picture's size, asks the rule, and only then "
          "exports again")
    check(re.search(r"ExportOnce\([^;]*FitDirectionType\.Horizontal\s*\)", exported) is not None
          and re.search(r"ExportOnce\([^;]*FitDirectionType\.Vertical\s*\)", exported) is not None,
          "the first fitted on its width, the second on its height")
    check(re.search(r"var\s+again\s*=\s*ExportOnce\(", exported) is not None
          and re.search(r"if\s*\(\s*again\s*!=\s*null\s*\)\s*\{[^}]*picture\s*=\s*again\s*;"
                        r"[^}]*fittedOn\s*=", exported) is not None,
          "the picture is replaced, and said fitted on its height, only when the second "
          "export wrote one")
    rolled = exported.find("RollBackAndCheck(")
    check(again >= 0 and 0 <= again < rolled,
          "and both exports are made before the group is rolled back")
    once = NOTE.body_of(code, "static byte[] ExportOnce(")
    door = once.find("HeronRunGuard.HeronExports()")
    check(0 <= door < once.find("ExportImage("),
          "every export goes through the run guard's door for Heron's own exports")
    check(re.search(r"options\.FitDirection\s*=\s*fit\s*;", once) is not None
          and re.search(r"options\.PixelSize\s*=\s*size\s*;", once) is not None,
          "and is fitted the way it is asked, to the size asked")
    check("FitDirectionType.Horizontal;" not in exported.replace(" ", ""),
          "no export in Exported sets the width fit on its own any more")

    print("\n2. in a family, what reaches out of the box is hidden; in a project, not")
    elements = NOTE.body_of(code, "static string OfElements(")
    family = re.search(r"if\s*\(\s*!doc\.IsFamilyDocument\s*\)\s*\{[^}]*HideCoversAbove\([^}]*\}"
                       r"\s*else\s*\{[^}]*crossing\s*=\s*HideReachingOut\(", elements)
    check(family is not None,
          "HideReachingOut runs in a family's branch only; a project keeps HideCoversAbove")
    check(re.search(r"Exported\([^;]*crossing\s*:\s*crossing\s*\)", elements) is not None,
          "and what it hid is handed to the reply")
    reach = NOTE.body_of(code, "static string HideReachingOut(")
    check(bool(reach), "HideReachingOut is there")
    check(re.search(r"framed\.Exists\(", reach) is not None
          and re.search(r"is\s+ConnectorElement", reach) is not None,
          "it never hides an element asked for, and leaves connectors to Prepare")
    check(re.search(r"CategoryType\s*!=\s*CategoryType\.Model", reach) is not None,
          "it passes over every category that is not a model one")
    check("HeronPicture.ReachesOut(" in reach and "CanBeHidden(view)" in reach,
          "HeronPicture decides what reaches out, and only what Revit can hide is hidden")
    check(re.search(r"try\s*\{\s*view\.HideElements\(ids\);\s*\}\s*catch", reach) is not None,
          "and a hide Revit refuses costs the hide, never the picture")

    print("\n3. the reply carries what the server reads")
    for key in ("pictureWidth", "pictureHeight", "fittedOn", "refitFailed", "hiddenCrossing"):
        check('"%s"' % key in raw, "the reply writes `%s`" % key)

    note = NOTE.read(PICTURE)
    check(note is not None, "HeronPicture.cs is there")
    check(note is not None and "Autodesk" not in NOTE.code_only(note),
          "and names no Autodesk type, so a host can link it without Revit")


def rule():
    print("\n4. the rules themselves, built and run without Revit")
    tfm = NOTE._tfm()
    if tfm is None:
        print("  NOT RUN - no framework here that an installed SDK can build AND")
        print("  a runtime can run. Linux: apt-get install -y dotnet-sdk-10.0")
        return COULD_NOT_RUN

    out_dir = "bin/x64/Debug-%s/" % tfm
    built = subprocess.run(
        ["dotnet", "build", HOST, "-p:RevitVersion=2024", "-p:HeronTfm=%s" % tfm,
         "-p:OutputPath=%s" % out_dir, "--nologo", "-v", "q"],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built.returncode != 0:
        check(False, "the host builds for %s" % tfm)
        for line in (built.stdout or b"").decode("utf-8", "replace").splitlines()[-20:]:
            print("    %s" % line)
        return 1

    dll = os.path.join(HOST, out_dir, "Heron.Picture.TestHost.dll")
    ran = subprocess.run(["dotnet", dll], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    for line in (ran.stdout or b"").decode("utf-8", "replace").splitlines():
        print("  " + line)
    check(ran.returncode == 0, "the host's own checks pass (exit %d)" % ran.returncode)
    return 0


def main():
    wiring()
    host = rule()

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    if host == COULD_NOT_RUN:
        print("COULD NOT RUN - the wiring passed and the rules were not run. Exit 3,")
        print("which is NOT a pass.")
        return COULD_NOT_RUN
    print("PASS    a picture's longest side is the width asked, and a family's frame is its parts")
    return 0


if __name__ == "__main__":
    sys.exit(main())
