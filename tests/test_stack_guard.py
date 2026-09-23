#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The 890 checks that compiled every day and ran on no day.

    python tests/test_stack_guard.py

WHY THIS EXISTS
---------------
`tests/Heron.StackGuard.TestHost` runs `HeronStackGuard`'s rewriter over the
WHOLE fragment library and checks the three things that decide whether it is
safe to deploy: that no fragment's line COUNT moves (so a compile error still
points at the right line of `fragment.cs`), that no new diagnostic appears,
and that every block-bodied lambda ends up guarded. Its own project file says
what it is for - *"Proves the stack guard WITHOUT Revit"* - and how to run it:
*"Run it with `dotnet run`, pointed at this folder"*.

NOTHING RAN IT. It was added to `heron_dotnet.PROJECTS` so that it COMPILES on
all eight Revit releases - HANDOVER calls that "the 'a gate nobody runs' shape
caught before it could set" - and the half it caught was the compiling half.
No suite built it, no suite executed it, and `check-gaps.py` sweeps
`tests/test_*.py`, so 890 checks over 398 fragments sat outside every count of
"every test passes". Its three siblings that link C# by source - Kernel,
BindingNote and Bridge - each have a runner here. This is the fourth.

IT RUNS ON LINUX, MEASURED RATHER THAN ASSUMED. `HeronStackGuard.cs` touches
no Autodesk type, which is why the project links it by source instead of
referencing the add-in; `dotnet run` as the project file suggests fails,
because `RevitVersion=2024` maps to `net48` and there is no Mono here. Built
with `HeronTfm` overridden to a plain `net10.0` - the same override
`tests/test_kernel.py` works out for itself - it builds in under two seconds
and prints **PASSED - 890 checks, 398 fragments, 93 carrying a guard**.

THE COUNT IT WAS LAST DESCRIBED WITH WAS 372. HANDOVER says the host "runs the
rewriter over all 372 fragments: 836 checks". The library is 398 now and the
host reports 883 checks run - so the host itself was right all along and the
only thing that had gone stale is everything ABOUT it, which is what happens
to a thing nobody runs.

IT EXITS 3 WHEN .NET IS ABSENT, and that is NOT a pass. `check-gaps.py` and
`change-evidence.py` both read 3 as "could not run", the fourth state beside
PASS, FAIL and NEEDS REAL REVIT.

WHAT IT DOES NOT SAY, in the host's own closing words: that a guarded fragment
survives runaway recursion in front of Revit. That needs a model, and it is
NEEDS-CHECKING J9's to hold.
"""

import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HOST = os.path.join(ROOT, "tests", "Heron.StackGuard.TestHost")
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

    dll = os.path.join(HOST, out_dir, "Heron.StackGuard.TestHost.dll")
    if not os.path.exists(dll):
        print("FAILED - built, and %s is not there." % dll)
        return 1

    # The host prints its own checks; they are the output of this suite.
    return subprocess.call(["dotnet", dll])


if __name__ == "__main__":
    sys.exit(main())
