# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Check every Revit API member Heron uses against every supported release.

    python tools/check-api-surface.py                 2020 through 2027
    python tools/check-api-surface.py 2025 2026       just those

WHY IT EXISTS — it covers what the compiler cannot reach
--------------------------------------------------------
`tools/check-compile.py` is the stronger check and should be run first, but it
stops at Revit 2024 off Windows: 2025+ target `net8.0-windows` with WPF, which
needs the Windows Desktop SDK. Those releases were reported SKIPPED, and a
skip is not a pass — so the newest three releases had nothing checking them at
all on the machines this project is actually worked on.

This closes that hole for the failure that has actually happened here:
**a member that simply does not exist in a release.** `Document.CreationGUID`
compiled clean on 2024 and does not exist on 2020; it had been read several
times and survived every reading.

It works by reading the COMPILED add-in's reference tables — exactly the types
and members the code calls, not what a regex over the source can find — and
looking each one up in that release's shipped reference assemblies from NuGet.

WHAT IT DOES NOT DO
-------------------
**Matching is by name.** A member that still exists but changed SIGNATURE
passes here and would fail a real compile. So this SUPPLEMENTS check-compile.py
and never replaces it: where a release can be compiled, compiling is the
better answer.

And as ever, neither one says anything about behaviour. `D3` in
NEEDS-CHECKING.md — move the ducts, then measure one — is still what catches a
unit error.

VALIDATE IT BEFORE TRUSTING A CLEAN RESULT
------------------------------------------
A checker that finds nothing is evidence about the checker until it has been
shown to catch something. Put `doc.CreationGUID` back into
`RevitWrite.DocumentKey()`, build for 2024, run this against 2020, and it must
report the missing member. That is how this tool was accepted in the first
place, and it is worth repeating after any change to it.
"""

import io
import os
import subprocess
import sys
import urllib.request
import zipfile

ALL_VERSIONS = ["2020", "2021", "2022", "2023", "2024", "2025", "2026", "2027"]

# The add-in is the only assembly that references Revit at all - Heron.Bridge
# and Heron.Core are Revit-free by construction, which check-structure.py
# enforces. So there is exactly one thing to read.
ADDIN = os.path.join("revit", "Heron.Revit.Addin", "bin", "x64", "Debug",
                     "Heron.Revit.Addin.dll")
BUILD_VERSION = "2024"

TOOL_DIR = os.path.join("tools", "api-surface")
CACHE = os.path.join(TOOL_DIR, ".assemblies")     # gitignored: bin/obj rules do not cover it

FEED = "https://api.nuget.org/v3-flatcontainer"
PACKAGES = ["nice3point.revit.api.revitapi", "nice3point.revit.api.revitapiui"]


def newest_release(package, year):
    """The newest non-preview package for that Revit year."""
    url = "%s/%s/index.json" % (FEED, package)
    with urllib.request.urlopen(url, timeout=60) as fh:
        body = fh.read().decode("utf-8")
    found = []
    for chunk in body.replace('"', " ").replace(",", " ").split():
        if chunk.startswith(year + ".") and "preview" not in chunk:
            found.append(chunk)
    if not found:
        return None
    # Sort numerically, not lexically: 2024.10 is newer than 2024.9.
    return sorted(found, key=lambda v: [int(p) if p.isdigit() else 0
                                        for p in v.split(".")])[-1]


def fetch(year):
    """Put that release's reference assemblies in the cache. Returns the dir."""
    target = os.path.join(CACHE, year)
    if os.path.isdir(target) and os.listdir(target):
        return target
    os.makedirs(target, exist_ok=True)

    for package in PACKAGES:
        version = newest_release(package, year)
        if version is None:
            print("   no published %s for Revit %s" % (package, year))
            continue
        url = "%s/%s/%s/%s.%s.nupkg" % (FEED, package, version, package, version)
        local = os.path.join(target, "package.zip")
        urllib.request.urlretrieve(url, local)
        with zipfile.ZipFile(local) as zf:
            for name in zf.namelist():
                # ref/ holds the reference assemblies; lib/ would be the real
                # ones, which are Autodesk's and not ours to carry around.
                if name.endswith(".dll") and "/ref/" in "/" + name:
                    data = zf.read(name)
                    out = os.path.join(target, os.path.basename(name))
                    with io.open(out, "wb") as fh:
                        fh.write(data)
        os.remove(local)
    return target


def main():
    wanted = [a for a in sys.argv[1:] if not a.startswith("-")] or ALL_VERSIONS
    unknown = [v for v in wanted if v not in ALL_VERSIONS]
    if unknown:
        print("Heron does not know these releases: %s" % ", ".join(unknown))
        return 2

    if not os.path.exists(ADDIN):
        print("FAIL  %s not found - it is what gets read." % ADDIN)
        print("      dotnet build revit/Heron.Revit.Addin -p:RevitVersion=%s"
              % BUILD_VERSION)
        return 1

    print("Building the reader...")
    built = subprocess.run(
        ["dotnet", "build", TOOL_DIR, "--nologo", "-v", "q"],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built.returncode != 0:
        print(built.stdout.decode("utf-8", "replace"))
        return 1

    print("Fetching reference assemblies (cached in %s)..." % CACHE)
    dirs = []
    for year in wanted:
        try:
            d = fetch(year)
        except Exception as exc:                     # noqa: BLE001 - report, do not mask
            print("   Revit %s: could not fetch (%s)" % (year, exc))
            continue
        if os.listdir(d):
            dirs.append(d)
        else:
            print("   Revit %s: no assemblies found" % year)

    if not dirs:
        print("FAIL  no reference assemblies - is the network reachable?")
        return 1

    print()
    run = subprocess.run(["dotnet", "run", "--project", TOOL_DIR, "--no-build",
                          "--", ADDIN] + dirs,
                         stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    print(run.stdout.decode("utf-8", "replace").rstrip())

    if run.returncode != 0:
        print("A missing member is a real cross-version defect. Prefer a member")
        print("that exists on every release over a #if that hides one that does")
        print("not - see .claude/skills/revit-version-support/SKILL.md.")
        return 1

    print("Every Revit member Heron calls exists in every release checked.")
    print("Matching is by NAME - a changed signature would pass here and fail a")
    print("real compile, so run tools/check-compile.py too wherever it can run.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
