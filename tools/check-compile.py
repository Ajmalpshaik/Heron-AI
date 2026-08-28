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

# Releases whose runtime is a windows-suffixed TFM. The add-in uses WPF there,
# so these need the WindowsDesktop MSBuild targets and the targeting packs.
# Not "unsupported" - just needing more installed, which is a different
# sentence and has to stay one.
NEEDS_WINDOWS_DESKTOP = {"2025", "2026", "2027"}

# The .NET major each release's TFM requires, for the ones that need one newer
# than the SDK a 2020-2024 build gets by with. Mirrors Directory.Build.props -
# D-05 again: the runtime table is never guessed, and never extrapolated.
MINIMUM_DOTNET_MAJOR = {"2025": 8, "2026": 8, "2027": 10}

# Returned by the probe when the SDK list could not be read at all - distinct
# from "read it, the targets are not there". Not knowing is not the same as
# knowing it is absent, and only one of the two justifies a skip.
UNKNOWN_TOOLCHAIN = "unknown"

WINDOWS = os.name == "nt"


def have_dotnet():
    try:
        out = subprocess.run(["dotnet", "--version"],
                             stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        return out.returncode == 0, out.stdout.decode("utf-8", "replace").strip()
    except OSError as exc:
        return False, str(exc)


def installed_sdks():
    """Every .NET SDK on this machine, as (major, path-to-sdk-folder)."""
    try:
        out = subprocess.run(["dotnet", "--list-sdks"],
                             stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    except OSError:
        return []
    if out.returncode != 0:
        return []

    found = []
    for line in out.stdout.decode("utf-8", "replace").splitlines():
        # "10.0.111 [/usr/lib/dotnet/sdk]"
        if " [" not in line or not line.endswith("]"):
            continue
        version, root = line.split(" [", 1)
        version, root = version.strip(), root[:-1].strip()
        try:
            major = int(version.split(".")[0])
        except ValueError:
            continue
        found.append((major, os.path.join(root, version)))
    return found


def windows_desktop_toolchain():
    """The newest .NET major whose SDK carries the WindowsDesktop targets.

    Probed rather than inferred from the operating system. Ubuntu's .NET 8 SDK
    package omits those targets and its .NET 10 one ships them, so "am I on
    Windows" answers a different question than the one being asked, and
    answering it wrongly is what left three Revit releases unchecked.
    """
    sdks = installed_sdks()
    if not sdks:
        # Could not enumerate at all - "dotnet --list-sdks" failed or printed
        # something unexpected. That is ignorance, not absence, and the two must
        # not be reported as the same thing. Say so and let the build decide.
        return UNKNOWN_TOOLCHAIN

    best = None
    for major, path in sdks:
        if os.path.isdir(os.path.join(path, "Sdks", "Microsoft.NET.Sdk.WindowsDesktop")):
            if best is None or major > best:
                best = major
    return best


def why_unbuildable(version, desktop_major):
    """The reason this release cannot be built here, or None if it can.

    Returns a sentence naming what is missing, because "SKIPPED" on its own
    tells whoever reads it nothing about whether that is fixable in a minute
    or needs a different machine. It is nearly always the former.
    """
    if version not in NEEDS_WINDOWS_DESKTOP:
        return None
    if desktop_major is UNKNOWN_TOOLCHAIN:
        # Attempt it. A build that then fails on the missing targets says so in
        # its own error, which is better than this script inventing a reason.
        return None

    needed = MINIMUM_DOTNET_MAJOR[version]

    # The advice names .NET 10 whatever the release needs, because an SDK builds
    # target frameworks older than itself and the .NET 10 SDK is the one MEASURED
    # to carry these targets on Ubuntu - its dotnet-sdk-8.0 package does not.
    # Naming dotnet-sdk-8.0 here would send a reader to the package that fails.
    fix = ("install an SDK that has them - on Ubuntu that is dotnet-sdk-10.0, "
           "whose SDK builds every target framework Heron uses")

    if desktop_major is None:
        return ("no installed .NET SDK carries the WindowsDesktop targets, and "
                "the add-in uses WPF for its ribbon icons; %s" % fix)
    if desktop_major < needed:
        return ("this release needs .NET %d and the newest SDK here carrying "
                "the WindowsDesktop targets is .NET %d; %s"
                % (needed, desktop_major, fix))
    return None


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
    print("Every project compiles on every version tried. That is the API surface")
    print("agreeing - it is NOT evidence that anything behaves correctly. D3 in")
    print("NEEDS-CHECKING.md is still the line that catches a unit error.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
