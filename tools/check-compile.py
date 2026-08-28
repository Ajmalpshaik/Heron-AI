# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Compile every project against every Revit version this machine can reach.

    python tools/check-compile.py                 every version it can build
    python tools/check-compile.py 2020 2024       just those two

This is A2 and A3 of NEEDS-CHECKING.md in one command, across the whole
supported range instead of one version at a time.

WHY IT EXISTS
-------------
Until 2026-08-28 not one .cs file in this repository had been through a
compiler. Step 6 - the first code that can change a model - was written on a
phone, and the register's own Group A was blocked on finding a machine with
the .NET SDK on it.

It turned out not to need one. The Revit API assemblies come from NuGet, which
the add-in project already used, and the .NET SDK is packaged by most Linux
distributions - so the compile gate runs anywhere, and found a real 2020-only
error the first time it ran. Every session from here on can compile before it
claims anything. See docs/30-compiling-away-from-windows.md.

WHAT A PASS DOES AND DOES NOT MEAN
----------------------------------
A pass means the code is CONSISTENT WITH THAT VERSION'S API SURFACE: every type
and member it names exists there, with the signature it is used with. That is
the entire "worked in 2020, broke in 2024" class, caught without opening Revit.

It says NOTHING about behaviour. Code that compiles can still move a duct 200
feet instead of 200 millimetres. D3 in the register is what catches that, and
no compiler will ever stand in for it.

VERSIONS IT CANNOT REACH
------------------------
Revit 2025 and newer target net8.0-windows / net10.0-windows with WPF, which
needs the Windows Desktop SDK. That ships with Microsoft's own .NET SDK build
and is absent from the source-built packages Linux distributions carry, so
those versions are reported as SKIPPED rather than passed. A skip is not a
pass, and this script will not let one read as the other.
"""

import os
import subprocess
import sys

# Every project, in dependency order so the first failure is the deepest one.
PROJECTS = [
    "platform/Heron.Core/Heron.Core.csproj",
    "revit/Heron.Bridge/Heron.Bridge.csproj",
    "revit/Heron.Revit.Addin/Heron.Revit.Addin.csproj",
    "tests/Heron.Bridge.TestHost/Heron.Bridge.TestHost.csproj",
]

# The range Directory.Build.props knows how to target. Kept as an explicit list
# rather than a range() so that adding a Revit release is a deliberate edit
# here AND there - D-05 says an unlisted release is an error, never a guess.
ALL_VERSIONS = ["2020", "2021", "2022", "2023", "2024", "2025", "2026", "2027"]

# Runtimes that need the Windows Desktop SDK. Not "unsupported" - just not
# buildable everywhere, which is a different sentence and has to stay one.
NEEDS_WINDOWS_DESKTOP = {"2025", "2026", "2027"}

WINDOWS = os.name == "nt"


def have_dotnet():
    try:
        out = subprocess.run(["dotnet", "--version"],
                             stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        return out.returncode == 0, out.stdout.decode("utf-8", "replace").strip()
    except OSError as exc:
        return False, str(exc)


def build(project, version):
    """Build one project for one Revit version. Returns (ok, error lines)."""
    cmd = ["dotnet", "build", project, "-p:RevitVersion=" + version, "--nologo"]
    if version in NEEDS_WINDOWS_DESKTOP and WINDOWS:
        cmd.append("-p:EnableWindowsTargeting=true")

    proc = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    text = proc.stdout.decode("utf-8", "replace")
    if proc.returncode == 0 and "Build succeeded" in text:
        return True, []

    seen, errors = set(), []
    for line in text.splitlines():
        if " error " in line and line not in seen:
            seen.add(line)
            errors.append(line.strip())
    return False, errors or [text.strip().splitlines()[-1] if text.strip() else "no output"]


def main():
    wanted = [a for a in sys.argv[1:] if not a.startswith("-")] or ALL_VERSIONS
    unknown = [v for v in wanted if v not in ALL_VERSIONS]
    if unknown:
        print("Heron does not know these releases: %s" % ", ".join(unknown))
        print("Supported today: %s. Adding one means a row in" % ", ".join(ALL_VERSIONS))
        print("Directory.Build.props first - D-05, the runtime table is never guessed.")
        return 2

    ok, detail = have_dotnet()
    if not ok:
        print("FAIL  no .NET SDK on this machine (%s)" % detail)
        print("      Windows: install the .NET SDK, or run tools/setup.ps1 which checks it.")
        print("      Linux:   your distribution almost certainly packages it -")
        print("               see docs/30-compiling-away-from-windows.md.")
        return 1

    print("Compiling with .NET SDK %s on %s." % (detail, "Windows" if WINDOWS else os.name))
    print()

    passed, failed, skipped = [], [], []

    for version in wanted:
        if version in NEEDS_WINDOWS_DESKTOP and not WINDOWS:
            skipped.append(version)
            print("Revit %s  SKIPPED - needs the Windows Desktop SDK (WPF)." % version)
            continue

        print("Revit %s" % version)
        broke = False
        for project in PROJECTS:
            good, errors = build(project, version)
            name = os.path.basename(project)
            if good:
                print("   ok    %s" % name)
            else:
                broke = True
                print("   FAIL  %s" % name)
                for line in errors[:8]:
                    print("         %s" % line)
                break     # later projects depend on this one; the rest is noise
        (failed if broke else passed).append(version)
        print()

    print("-" * 70)
    if passed:
        print("COMPILED   %s" % ", ".join(passed))
    if skipped:
        print("SKIPPED    %s  (not tested here - a skip is not a pass)"
              % ", ".join(skipped))
    if failed:
        print("FAILED     %s" % ", ".join(failed))
        print()
        print("A failure is normal for code no compiler has read yet, and each one")
        print("names its file and line. Fix it and run this again.")
        return 1

    print()
    print("Every project compiles on every version tried. That is the API surface")
    print("agreeing - it is NOT evidence that anything behaves correctly. D3 in")
    print("NEEDS-CHECKING.md is still the line that catches a unit error.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
