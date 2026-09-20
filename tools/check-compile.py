# Heron-Agent:  HERON-DEV-BLD-010
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

EVERY READ-ONLY FACT THIS SCRIPT USES IS HERON-DEV-NET-006's, in
brain/heron_dotnet.py, and imported rather than kept here a second time. That
agent answers what a build would NEED, in seconds and without a compiler; this
script is what BUILDS, and its answer is the only one that counts.

WHAT A PASS DOES AND DOES NOT MEAN
----------------------------------
A pass means the code is CONSISTENT WITH THAT VERSION'S API SURFACE: every type
and member it names exists there, with the signature it is used with. That is
the entire "worked in 2020, broke in 2024" class, caught without opening Revit.

It says NOTHING about behaviour. Code that compiles can still move a duct 200
feet instead of 200 millimetres. D3 in the register is what catches that, and
no compiler will ever stand in for it.

WHAT IT NEEDS INSTALLED
-----------------------
Revit 2025 and newer target net8.0-windows / net10.0-windows and the add-in
uses WPF for its ribbon icons, so those three releases need two things this
script checks for before it tries: an SDK carrying Microsoft.NET.Sdk.Windows-
Desktop, and -p:EnableWindowsTargeting=true, which is what makes the SDK
restore the Windows targeting packs from NuGet when it is not itself running
on Windows.

Both are available off Windows. Measured 2026-08-28 on Ubuntu 24.04: the
distribution's .NET 10 SDK package carries the WindowsDesktop targets and
builds all eight releases, 2020 through 2027, every project, 0 warnings. The
.NET 8 SDK package does NOT carry them - it builds 2020-2024 and the three
non-WPF projects on 2025+, and fails the add-in with MSB4019. That is a fact
about one distribution's packaging, not about the platform, which is why this
script probes for the targets rather than for an operating system.

Until 2026-08-28 this file skipped 2025-2027 whenever it was not on Windows,
and passed -p:EnableWindowsTargeting=true only WHEN it was - which is backwards,
that flag exists for the case where you are not. Three of eight releases had
nothing compiling them as a result. See docs/30-compiling-away-from-windows.md.

A skip is still not a pass, and this script will not let one read as the other -
but it now skips only what it has actually established it cannot build.
"""

import os
import subprocess
import sys

sys.path.insert(0, os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "brain"))

import heron_dotnet as NET  # noqa: E402

# EVERY READ-ONLY FACT BELOW USED TO LIVE IN THIS FILE, under
# `Heron-Agent: none`. It is HERON-DEV-NET-006 - "which target framework
# does this need, is it available, is the package set compatible, will it
# build on all supported versions", READ-ONLY - and it was the whole of
# this script except the part that actually builds.
#
# Golden Rule 4: keep, extend, adapt, version-branch, in that order. So it
# moved to brain/heron_dotnet.py rather than being copied, and this script
# imports it back. There is ONE SDK probe and one runtime table, and the
# agent and the gate can no longer disagree about what this machine can do.
#
# What stayed here is `build()` and the report. The agent compiles nothing;
# this script is what compiles, and its answer is the only one that counts.
PROJECTS = NET.PROJECTS
ALL_VERSIONS = NET.RELEASES
NEEDS_WINDOWS_DESKTOP = NET.NEEDS_WINDOWS_DESKTOP
MINIMUM_DOTNET_MAJOR = NET.MINIMUM_DOTNET_MAJOR
UNKNOWN_TOOLCHAIN = NET.UNKNOWN_TOOLCHAIN
WINDOWS = NET.WINDOWS

have_dotnet = NET.have_dotnet
installed_sdks = NET.installed_sdks
windows_desktop_toolchain = NET.windows_desktop_toolchain
why_unbuildable = NET.why_unbuildable


def build(project, version):
    """Build one project for one Revit version. Returns (ok, error lines)."""
    cmd = ["dotnet", "build", project, "-p:RevitVersion=" + version, "--nologo"]
    if version in NEEDS_WINDOWS_DESKTOP:
        # Restores the Windows targeting packs from NuGet. A no-op on Windows,
        # where the SDK already has them, and the whole point everywhere else.
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

    # BEFORE ANY BUILDING, AND DELIBERATELY BEFORE THE .NET CHECK. A project
    # nobody listed is a hole in this gate whatever is installed, and saying so
    # needs no compiler - so it is caught on a machine with no .NET too.
    forgotten = NET.unlisted()
    if forgotten:
        print("FAIL  %d .csproj on disk that nothing here builds:"
              % len(forgotten))
        for one in forgotten:
            print("        %s" % one)
        print()
        print("      This gate compiles the list in brain/heron_dotnet.py, and a")
        print("      project missing from it is never built on ANY release.")
        print("      Heron.Banner.TestHost was held back that way for three days")
        print("      for a good reason nobody could find, because the reason was")
        print("      in a commit message - FRAGMENT-ISSUES row 161. Add it to")
        print("      PROJECTS, or to NOT_SHIPPED where the reason is READABLE.")
        return 1

    ok, detail = have_dotnet()
    if not ok:
        print("FAIL  no .NET SDK on this machine (%s)" % detail)
        print("      Windows: install the .NET SDK, or run tools/setup.ps1 which checks it.")
        print("      Linux:   your distribution almost certainly packages it -")
        print("               see docs/30-compiling-away-from-windows.md.")
        return 1

    print("Compiling with .NET SDK %s on %s." % (detail, "Windows" if WINDOWS else os.name))

    desktop_major = windows_desktop_toolchain()
    if any(v in NEEDS_WINDOWS_DESKTOP for v in wanted):
        if desktop_major is UNKNOWN_TOOLCHAIN:
            print("Could not read the installed SDK list, so the WPF releases are")
            print("attempted rather than skipped - a build error is more honest")
            print("than a guess about what is installed.")
        elif desktop_major is None:
            print("No installed SDK carries the WindowsDesktop targets, so the")
            print("releases that need WPF cannot be built here. See below.")
        else:
            print("WindowsDesktop targets found in the .NET %d SDK." % desktop_major)
    print()

    passed, failed, skipped = [], [], []

    for version in wanted:
        missing = why_unbuildable(version, desktop_major)
        if missing:
            skipped.append(version)
            print("Revit %s  SKIPPED - %s." % (version, missing))
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
    print("All %d projects compile on every version tried, and no .csproj on"
          % len(PROJECTS))
    print("disk is missing from that list. That is the API surface agreeing -")
    print("it is NOT evidence that anything behaves correctly. D3 in")
    print("NEEDS-CHECKING.md is still the line that catches a unit error.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
