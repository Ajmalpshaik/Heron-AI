#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The kernel classes that nothing had ever RUN.

    python tests/test_kernel.py

WHY THIS EXISTS
---------------
FRAGMENT-ISSUES section 5b, row 7 counted, across every suite in `tests/`,
which of the ten kernel classes are named by a test. Four were at zero:
`HeronStop`, `HeronUnits`, `HeronIdentity` and `HeronAtomicWrite`. Row 7
calls `HeronUnits` the one that matters, because row 3 is a units-guard
defect - two callers wrote the range check out by hand instead of asking
`IsUsableMillimetres`, and every comparison against `NaN` is false, so the
hand-written pair waved `NaN` through into a parameter write and an `XYZ`.
The proof for that row had to be written from scratch in a throwaway console
project. That is the gap this closes.

IT RUNS THE CODE, WHICH IS THE POINT
------------------------------------
Every other suite here that tests C# reads the source as TEXT and checks a
claim about it. That is the right tool for "does this file still say what it
must" and the wrong one for "what does this function return for NaN". Both
kinds are wanted; only the first was here. `tests/Heron.Kernel.TestHost`
takes a PROJECT REFERENCE on `Heron.Core` rather than linking one file,
because the kernel has no Revit reference at all - that is its defining
property - so the honest thing is to test the assembly everything else
loads.

MEASURED AGAINST THE PREVIOUS IMPLEMENTATION, which is what makes it a test
rather than a description. Built against `HeronPaths` and `HeronConfig` as
they stood before the same sitting's repairs, **11 of the 63 checks fail and
the host exits 1**: the four sibling-folder cases from row 1
(`HeronBackup`, `Heron-old`, `Heron.bak`, `Heron backup` all read as SAFE TO
DELETE), and seven from row 8 - `on` and a padded ` true ` read as false,
and five unrecognised values ignored a `true` fallback.

IT EXITS 3 WHEN .NET IS ABSENT, and that is NOT a pass. `check-gaps.py` and
`change-evidence.py` both read 3 as "could not run", the fourth state beside
PASS, FAIL and NEEDS REAL REVIT. A machine with no SDK learns that it did
not check, rather than that everything is fine.

WHAT IT DOES NOT COVER, said plainly rather than left to be discovered:
no Revit is involved, so nothing here says what Revit does with a value that
gets past a guard; `HeronIdentity.InstallationId` is deliberately untouched
because reading it writes a file into the user's own DATA folder; and
`HeronConfig.Load`/`Save` are untouched for the same reason - `GetBool` is
exercised through a config built in memory.
"""

import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HOST = os.path.join(ROOT, "tests", "Heron.Kernel.TestHost")
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
    """The MAJOR version of every line dotnet listed, lowest first.

    `--list-runtimes` prints "Microsoft.NETCore.App 10.0.12 [path]" and
    `--list-sdks` prints "10.0.112 [path]", so the version is the first field
    for one and the second for the other.
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

    A RUNTIME AND AN SDK ARE DIFFERENT THINGS AND THIS SUITE NEEDS BOTH -
    the same reasoning `tests/test_binding_note.py` records, and for the same
    reason: only an SDK builds an assembly and only a runtime runs one, so
    asking either alone picks a target that cannot be built and reports a
    missing dependency as a failing repository.

    Returns None when no runtime and SDK meet.
    """
    runtimes = _majors(_asked("--list-runtimes"), "Microsoft.NETCore.App")
    sdks = _majors(_asked("--list-sdks"))
    if not runtimes or not sdks:
        return None
    usable = [m for m in runtimes if m <= max(sdks)]
    return "net%d.0" % max(usable) if usable else None


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
    # OS pipe buffer, which a verbose restore failure passes easily. `run`
    # reads the pipe, and the captured text is then there to PRINT - which
    # matters, because a build failure with no diagnostic is unfixable.
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

    dll = os.path.join(HOST, out_dir, "Heron.Kernel.TestHost.dll")
    if not os.path.exists(dll):
        print("FAILED - built, and %s is not there." % dll)
        return 1

    # The host prints its own checks; they are the output of this suite.
    return subprocess.call(["dotnet", dll])


if __name__ == "__main__":
    sys.exit(main())
