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


def _majors(said, prefix=None):
    """The MAJOR version of every line dotnet listed, highest last.

    `--list-runtimes` prints "Microsoft.NETCore.App 10.0.12 [path]" and
    `--list-sdks` prints "10.0.112 [path]", so the version is the first field
    for one and the second for the other. One reader with a prefix rather
    than two, because two would drift.
    """
    out = []
    for line in (said or "").splitlines():
        parts = line.split()
        if prefix:
            if not line.startswith(prefix):
                continue
            parts = parts[1:]
        if not parts:
            continue
        try:
            out.append(int(parts[0].split(".")[0]))
        except ValueError:
            continue
    return sorted(set(out))


def _tfm():
    """The newest framework BOTH an installed SDK can build and a runtime can run.

    A RUNTIME AND AN SDK ARE DIFFERENT THINGS AND THIS SUITE NEEDS BOTH. Only
    an SDK BUILDS an assembly; only a runtime RUNS one. This suite builds its
    host and then runs it, so it needs a target that is under the newest SDK
    and present among the runtimes.

    ASKING EITHER ONE ALONE PICKS A TARGET THAT CANNOT BE BUILT, and both
    mistakes were review findings on PR #212 rather than guesses:

      - runtimes alone: a machine with a runtime and NO SDK returns a target,
        `dotnet build` fails, and a missing dependency is reported as a
        FAILING REPOSITORY;
      - and adding a bare "is there any SDK" is not enough either - a .NET 10
        runtime beside only the .NET 8 SDK still selects `net10.0`, which
        that SDK cannot target, and the build fails the same way.

    Both end as exit 1 where the honest answer is exit 3. That is exactly the
    confusion FRAGMENT-ISSUES row 162 is about, in the suite that row's own
    repair is named after, which is why it is worth this much care.

    Returns None when no runtime and SDK meet.
    """
    runtimes = _majors(_asked("--list-runtimes"), "Microsoft.NETCore.App")
    sdks = _majors(_asked("--list-sdks"))
    if not runtimes or not sdks:
        return None
    # An SDK builds for its own major and older, so the target cannot be
    # newer than the newest SDK - and it must be one a runtime can run.
    usable = [m for m in runtimes if m <= max(sdks)]
    return "net%d.0" % max(usable) if usable else None


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
        print("COULD NOT RUN - no framework here that an installed SDK can")
        print("  build AND an installed runtime can run. This suite BUILDS")
        print("  its host and then runs it, so it needs both.")
        print("  Linux:   apt-get install -y dotnet-sdk-10.0")
        print("  This is exit 3, which is NOT a pass: nothing was checked.")
        return COULD_NOT_RUN

    out_dir = "bin/x64/Debug-%s/" % tfm
    # READ THE OUTPUT RATHER THAN ONLY PIPING IT. `subprocess.call` with
    # stdout=PIPE and nobody reading deadlocks as soon as the child fills the
    # OS pipe buffer - 64 KiB on Linux, which a verbose restore failure
    # passes easily. Demonstrated rather than assumed: a child writing 1 MB
    # to a piped stdout under `call` never returns. The suite would then hang
    # until the outer CI timeout instead of saying PASS, FAIL or NOT RUN.
    # `run` reads the pipe, so it cannot fill, and the captured text is then
    # there to PRINT - which the old version threw away, leaving a build
    # failure with no diagnostic. A Codex review on PR #212 raised both.
    built = subprocess.run(
        ["dotnet", "build", HOST, "-p:RevitVersion=2024",
         "-p:HeronTfm=%s" % tfm, "-p:OutputPath=%s" % out_dir],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built.returncode != 0:
        print("FAILED - the test host did not build for %s." % tfm)
        print("  dotnet build %s -p:RevitVersion=2024 -p:HeronTfm=%s "
              "-p:OutputPath=%s" % (HOST, tfm, out_dir))
        said = (built.stdout or b"").decode("utf-8", "replace").strip()
        for line in said.splitlines()[-25:]:
            print("    %s" % line)
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
