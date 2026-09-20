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

import io
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HOST = os.path.join(ROOT, "tests", "Heron.BindingNote.TestHost")
COULD_NOT_RUN = 3


def _asked(what):
    """`dotnet <what>`, or None when dotnet cannot answer at all."""
    try:
        out = subprocess.check_output(["dotnet", what],
                                      stderr=subprocess.STDOUT)
    except Exception:                                      # noqa: BLE001
        return None
    return out.decode("utf-8", "replace")


def _has_sdk():
    """Is there an SDK here, not merely a runtime?

    THE TWO ARE DIFFERENT AND THIS SUITE NEEDS THE SDK. A runtime RUNS a
    built assembly; only an SDK BUILDS one, and this suite builds its host
    before running it. Asking `--list-runtimes` alone answers the wrong
    question: on a machine with a runtime and no SDK it returns a target,
    `dotnet build` then fails, and a missing dependency is reported as a
    FAILING REPOSITORY - exactly the confusion FRAGMENT-ISSUES row 162 is
    about, in the suite that row's own repair is named after.
    """
    said = _asked("--list-sdks")
    return bool(said and said.strip())


def _tfm():
    """The newest Microsoft.NETCore.App this machine can actually run.

    Asked rather than assumed, the same way tests/test_bridge_roundtrip.py
    asks it - a hardcoded target framework is how a suite comes to need a
    specific SDK for no reason anybody wrote down.
    """
    said = _asked("--list-runtimes")
    if said is None:
        return None
    majors = []
    for line in said.splitlines():
        if not line.startswith("Microsoft.NETCore.App "):
            continue
        try:
            majors.append(int(line.split()[1].split(".")[0]))
        except (IndexError, ValueError):
            continue
    return "net%d.0" % max(majors) if majors else None


CALL_SITE = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")

# THE REVIVED VALUE FIRST, THE CARRIED ONE SECOND, AT THE PRODUCTION CALL.
# Written as a pattern rather than a fixed string so that reformatting the
# line does not fail the suite, while swapping the two DOES.
BINDS = re.compile(r"Size\(\s*shaped\s*:\s*shaped\s*,\s*before\s*:\s*value\s*\)")


def crosses_the_seam():
    """Does RevitFragment still pass SURVIVED first and OFFERED second?

    WHY THIS IS HERE AND NOT IN THE TEST HOST. The host links
    HeronBindingNote.cs by source and proves the FORMATTER: given (2, 1128)
    it writes "2 of 1128". It cannot prove the call, because RevitFragment.cs
    touches Autodesk types and linking it would drag in RevitAPI.dll - the
    whole reason the formatter was split out.

    So the host's argument-order check only verifies the order the host
    itself supplied. Swap the production call to Size(before: ..., shaped:
    ...) and every check in the host still passes while a modeller reads
    "1128 of 2" - the loss reported backwards, which is worse than the "(2)"
    row 75 started with. Both arguments are `object`, so no compiler catches
    it. A Codex review on PR #212 raised exactly this, and it was right.

    Reading the one line is crude. It is also the only thing here that can
    fail when that line changes, and a crude check that fails beats a
    thorough one that cannot.
    """
    try:
        text = io.open(CALL_SITE, encoding="utf-8").read()
    except (IOError, OSError) as exc:
        return False, "could not read %s (%s)" % (CALL_SITE, exc)
    if BINDS.search(text):
        return True, "binds shaped: shaped, before: value"
    loose = re.findall(r"\+ Size\([^)]*\)", text)
    return False, ("the binding note's call does not pass the revived value "
                   "as `shaped` and the carried one as `before`. Found: %s"
                   % (loose or "no call at all"))


def main():
    tfm = _tfm()
    if tfm is None:
        print("COULD NOT RUN - no .NET runtime on this machine.")
        print("  Linux:   apt-get install -y dotnet-sdk-10.0")
        print("  This is exit 3, which is NOT a pass: nothing was checked.")
        return COULD_NOT_RUN

    if not _has_sdk():
        print("COULD NOT RUN - a .NET runtime is here but no SDK, and this")
        print("  suite BUILDS its host before running it.")
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
    ran = subprocess.call(["dotnet", dll])
    if ran != 0:
        return ran

    # AND THE ONE CHECK THE HOST CANNOT MAKE, about the call rather than the
    # formatter. Last, so that a failure here is never confused with the
    # formatter's own.
    ok, said = crosses_the_seam()
    print()
    print("  %s  the production call passes the survivor first - %s"
          % ("ok  " if ok else "FAIL", said))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
