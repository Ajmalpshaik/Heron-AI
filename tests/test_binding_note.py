#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The binding note counts what was OFFERED, not just what survived.

    python tests/test_binding_note.py

WHY THIS EXISTS
---------------
FRAGMENT-ISSUES row 75. `RevitFragment.Shape()` revives a carried value by
walking each `ElementId` through `doc.GetElement(id)` on the HOST document,
swallowing the miss and keeping what resolved. Zero survivors is handled
honestly - it returns null and the caller says "nothing usable survived". A
PARTIAL revival was not: 1128 linked walls carried over, two ids happened to
name real elements in the host document, and the note read

    elements from select-from-link (2)

which is indistinguishable from a deliberate narrowing to two.

`HeronBindingNote.Size` is now the ONLY thing that tells those apart, and **a
compiler cannot catch a regression in its wording or in the order of its two
arguments** - both are `object`. That gap is what this closes, and it was
raised as a review finding on the pull request that introduced the fix rather
than being noticed by its author.

HOW IT RUNS WITHOUT REVIT
-------------------------
`HeronBindingNote.cs` touches no Autodesk type - it counts two collections -
so `tests/Heron.BindingNote.TestHost` links it BY SOURCE, the same pattern
`Heron.StackGuard.TestHost` and `Heron.Banner.TestHost` already use and for
the reason their project files give: a project reference would drag in
RevitAPI.dll and make the one testable piece untestable on any machine
without Revit. Linking the source also keeps exactly ONE copy of the rule -
a second implementation written to test the first is how `binds:` came to be
honoured by the Python half and ignored by the C# executor (row 96).

IT EXITS 3 WHEN .NET IS ABSENT, and that is NOT a pass - `check-gaps.py` and
`change-evidence.py` both read 3 as "could not run", which is a fourth state
beside PASS, FAIL and NEEDS REAL REVIT. A machine with no SDK learns that it
did not check, rather than that everything is fine.

MEASURED AGAINST THE PREVIOUS IMPLEMENTATION, which is what makes it a test
rather than a description: built against a copy of `Size` as it stood before
the fix - one that ignored what was offered - **3 of the 7 checks fail and
the host exits 1**. The four that still pass are the ones that must not move:
equal counts, no carried value, and a scalar.
"""

import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HOST = os.path.join(ROOT, "tests", "Heron.BindingNote.TestHost")
COULD_NOT_RUN = 3


def _tfm():
    """The newest Microsoft.NETCore.App this machine can actually run.

    Asked rather than assumed, the same way tests/test_bridge_roundtrip.py
    asks it - a hardcoded target framework is how a suite comes to need a
    specific SDK for no reason anybody wrote down.
    """
    try:
        out = subprocess.check_output(["dotnet", "--list-runtimes"],
                                      stderr=subprocess.STDOUT)
    except Exception:                                      # noqa: BLE001
        return None
    majors = []
    for line in out.decode("utf-8", "replace").splitlines():
        if not line.startswith("Microsoft.NETCore.App "):
            continue
        try:
            majors.append(int(line.split()[1].split(".")[0]))
        except (IndexError, ValueError):
            continue
    return "net%d.0" % max(majors) if majors else None


def main():
    tfm = _tfm()
    if tfm is None:
        print("COULD NOT RUN - no .NET SDK or runtime on this machine.")
        print("  Linux:   apt-get install -y dotnet-sdk-10.0")
        print("  This is exit 3, which is NOT a pass: nothing was checked.")
        return COULD_NOT_RUN

    out_dir = "bin/x64/Debug-%s/" % tfm
    built = subprocess.call(
        ["dotnet", "build", HOST, "-p:RevitVersion=2024",
         "-p:HeronTfm=%s" % tfm, "-p:OutputPath=%s" % out_dir],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built != 0:
        print("FAILED - the test host did not build.")
        print("  dotnet build %s -p:RevitVersion=2024 -p:HeronTfm=%s "
              "-p:OutputPath=%s" % (HOST, tfm, out_dir))
        return 1

    dll = os.path.join(HOST, out_dir, "Heron.BindingNote.TestHost.dll")
    if not os.path.exists(dll):
        print("FAILED - built, and %s is not there." % dll)
        return 1

    # The host prints its own checks; they are the output of this suite.
    return subprocess.call(["dotnet", dll])


if __name__ == "__main__":
    sys.exit(main())
