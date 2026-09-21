# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-NET-006
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
.NET compatibility - what a build would need, before anything is built.

    python brain/heron_dotnet.py

WHAT IT IS FOR (docs/28, HERON-DEV-NET-006)
--------------------------------------------
"READ-ONLY. Which target framework does this need, is it available, is
the package set compatible, will it build on all supported versions."
T1, risk READ. Four questions, and the fourth one is not answered by
building.

IT COMPILES NOTHING, AND THAT IS THE WHOLE LINE BETWEEN THIS AND THE GATE
--------------------------------------------------------------------------
tools/check-compile.py builds. It is the gate, it takes minutes, and its
answer is the only one that counts. This agent answers before you start
it: what each release needs, what this machine has, and which releases
would be skipped and why - in seconds, having run no compiler.

MOST OF THIS WAS ALREADY WRITTEN AND CLAIMED BY NOBODY
--------------------------------------------------------
check-compile.py carried `Heron-Agent: none` and held the whole read-only
half: the runtime facts, the SDK probe, and the sentence explaining a
skip. Golden Rule 4 is keep, extend, adapt, version-branch - in that
order - so this file is that half moved rather than a second copy of it,
and the tool imports it back. There is one probe, and both callers get
the same answer.

WHAT IS NEW HERE: THE MIRRORED TABLES ARE CHECKED AGAINST THE PROPS
--------------------------------------------------------------------
Two of those tables mirror Directory.Build.props - which releases need
the WindowsDesktop targets, and which .NET major each needs. Their
comments say so and say why: D-05, the runtime table is never guessed.
NOTHING CHECKED THAT THEY STILL AGREED.

They are derivable. A TFM ending in `-windows` needs the desktop
targets; `net8.0-windows` needs .NET 8. So `disagreements()` reads
HERON-FRG-MTX-009's parse of the props and reports every row where the
mirror has drifted - which is how adding Revit 2028 to the props and
forgetting the tables here gets caught by a report rather than by a
build failing three releases later.

THE RELEASE LIST STAYS AN EXPLICIT EDIT
-----------------------------------------
`RELEASES` is a written-out list and not derived, and that is
deliberate: check-compile's own comment says adding a release must be a
deliberate edit HERE AND THERE, because an unlisted release is an error
and never a guess. Deriving it would quietly remove that second pair of
eyes. It is CHECKED against the props instead of replaced by them.

THE PACKAGE SET IS PARAMETERISED, AND WHAT THAT DOES AND DOES NOT SETTLE
-------------------------------------------------------------------------
The Revit API arrives as `Nice3point.Revit.Api.RevitAPI` at version
`$(RevitVersion).*`, so the package set follows the release rather than
being pinned per release. "Is the package set compatible" is therefore
answered by construction for any release in the table - PROVIDED a
package exists on NuGet for that version.

This agent does not go and look. It reports the reference and its
version expression as written, and says that whether the feed carries
2028 is a fact about the internet, not about this repository.
"""

from __future__ import annotations

import io
import os
import re
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Every project, in dependency order so the first failure is the deepest one.
#
# IT SAID "EVERY PROJECT" WHILE NAMING FIVE OF SIX. `Heron.Banner.TestHost`
# was left out ON PURPOSE when it arrived on 2026-09-17 (`5669e7e`), and the
# reason was sound: it is a WinExe WPF project, CI builds on Linux with
# EnableWindowsTargeting, and that combination had never been watched go
# green. THE REASON LIVED ONLY IN THE COMMIT MESSAGE. Nothing in this file,
# in the project file, or in the gate said a project was being held back, so
# the next reader could not tell a decision from an oversight - and the
# comment above them read "every project". That is FRAGMENT-ISSUES row 161.
#
# The condition that commit set was "trivial once someone has watched one
# green run". It has been watched: 2026-09-20, `tools/check-compile.py` on
# Linux with the 10.0.x SDK and -p:EnableWindowsTargeting=true - the same
# command, OS, SDK line and flag gates.yml uses - built all seven projects on
# all eight releases, Banner included. So it is listed now, on that evidence.
#
# `tools/api-surface` stays out: it is a tool that reads the Revit assemblies
# rather than something Heron ships, and it targets no Revit release. It is
# named in NOT_SHIPPED instead of being silently absent, because silently
# absent is the thing that went wrong here.
PROJECTS = [
    "platform/Heron.Core/Heron.Core.csproj",
    "revit/Heron.Bridge/Heron.Bridge.csproj",
    "revit/Heron.Revit.Addin/Heron.Revit.Addin.csproj",
    # STAGE 2 SHAPE PROOFS, deleted when that stage closes. Listed here so
    # they are held to the same eight releases as everything else: a proof
    # that only builds on 2024 proves the shape on 2024 (docs/work-notes/
    # plans/plugin-extension/02-implementation.md, Stage 2).
    "revit/Heron.Doc/Heron.Doc.csproj",
    "revit/Heron.Tools/Heron.Tools.csproj",
    "tests/Heron.Banner.TestHost/Heron.Banner.TestHost.csproj",
    "tests/Heron.BindingNote.TestHost/Heron.BindingNote.TestHost.csproj",
    "tests/Heron.Bridge.TestHost/Heron.Bridge.TestHost.csproj",
    "tests/Heron.StackGuard.TestHost/Heron.StackGuard.TestHost.csproj",
]

# The one project that is a tool rather than something Heron ships. Named here
# so `unlisted()` can tell "deliberately out" from "somebody forgot", which is
# the distinction the missing Banner host had no way to make.
NOT_SHIPPED = ["tools/api-surface/ApiSurface.csproj"]

# The range Directory.Build.props knows how to target. Kept as an explicit list
# rather than a range() so that adding a Revit release is a deliberate edit
# here AND there - D-05 says an unlisted release is an error, never a guess.
# `disagreements()` checks it against the props; it does not replace it.
RELEASES = ["2020", "2021", "2022", "2023", "2024", "2025", "2026", "2027"]

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

# Where the gate lives, named rather than described, because every answer
# here ends by pointing at it.
THE_GATE = "tools/check-compile.py"

_TFM_MAJOR = re.compile(r"^net(\d+)\.")


def _table():
    """
    Release -> .NET target, from Directory.Build.props.

    IMPORTED HERE AND NOT AT THE TOP, and that is not tidiness.
    HERON-FRG-MTX-009 reaches heron_fragment, which imports PyYAML, and
    tools/check-compile.py imports this module. The compile job on CI
    installs a .NET SDK and NOTHING ELSE - no PyYAML - because compiling
    C# has never needed one.

    Hoisting this import gave that gate a third-party dependency it had
    never had, and it failed in under a second with ModuleNotFoundError
    before a single project was built. Found by CI on 2026-09-15, in the
    change that moved these facts out of that file.

    So the probe half - which is all the gate uses - stays importable
    with the standard library alone, and the props table is read only by
    the two functions that actually need it.
    """
    import heron_matrix as MTX
    return MTX.runtimes()


def have_dotnet():
    """(present, version-or-reason). No build is started."""
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
        if os.path.isdir(os.path.join(path, "Sdks",
                                      "Microsoft.NET.Sdk.WindowsDesktop")):
            if best is None or major > best:
                best = major
    return best


def why_unbuildable(release, desktop_major):
    """The reason this release cannot be built here, or None if it can.

    Returns a sentence naming what is missing, because "SKIPPED" on its own
    tells whoever reads it nothing about whether that is fixable in a minute
    or needs a different machine. It is nearly always the former.
    """
    if release not in NEEDS_WINDOWS_DESKTOP:
        return None
    if desktop_major is UNKNOWN_TOOLCHAIN:
        # Attempt it. A build that then fails on the missing targets says so in
        # its own error, which is better than this agent inventing a reason.
        return None

    needed = MINIMUM_DOTNET_MAJOR[release]

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


def unlisted(root=None):
    """
    Every .csproj on disk that neither PROJECTS nor NOT_SHIPPED names.

    THE SAME ARGUMENT `disagreements()` MAKES, ONE LEVEL UP: a hand-written
    list is only worth keeping if something checks it, and nothing did.
    `Heron.Banner.TestHost` sat beside a comment reading *"every project"*
    for long enough to answer two NEEDS-CHECKING rows, and the compile gate
    never built it once - on any release.

    Returns paths in the repository's own spelling, sorted, so a caller can
    print them. It reads the SOURCE TREE and never the build, so it holds on a
    machine with no .NET at all - which is the point: the failure it catches
    is somebody adding a project, not somebody's toolchain.

    IT ASKS GIT WHICH PROJECTS ARE TRACKED, and walks the disk only when git
    cannot answer. A walk alone reports a GENERATED project as forgotten:
    `check-fragments-compile.py --keep` deliberately leaves
    `build/<version>/FragmentCheck.csproj` behind for inspection, `build/` is
    in `.gitignore`, and the gate would then exit 1 on the next run because an
    earlier documented command was given a documented flag. A tracked file is
    exactly the right test - a project nobody committed is not one this gate
    was written to compile.
    """
    where = ROOT if root is None else root
    known = set(PROJECTS) | set(NOT_SHIPPED)
    found = _tracked_projects(where)
    if found is None:
        found = _walked_projects(where)
    return sorted(rel for rel in found if rel not in known)


def _tracked_projects(where):
    """Every .csproj git tracks, or None when git cannot say.

    None rather than an empty list, and the distinction is the whole reason
    this is a separate function: "git told me there are none" and "there is no
    git here" are different answers, and D-52 says an absent measurement is
    not a clean one. An empty result from a real repository is believed; a
    missing git falls back to the walk.
    """
    try:
        out = subprocess.check_output(
            ["git", "-C", where, "ls-files", "--", "*.csproj"],
            stderr=subprocess.DEVNULL)
    except (OSError, subprocess.CalledProcessError):
        return None
    return [line.strip().replace("\\", "/")
            for line in out.decode("utf-8", "replace").splitlines()
            if line.strip()]


def _generated_dirs(where):
    """Directory names .gitignore says hold generated output.

    READ RATHER THAN TYPED, and that is not only the house rule about derived
    values. The tree that made this necessary is where the generated
    FragmentCheck project lands, and naming it here would be a second list to
    keep in step with .gitignore - which is the failure this repository keeps
    having. Only the plain `name/` lines are taken; a pattern with a wildcard
    or a slash inside is not a directory name and is left alone.
    """
    names = {".git"}
    try:
        text = io.open(os.path.join(where, ".gitignore"), encoding="utf-8").read()
    except (IOError, OSError):
        return names
    for line in text.splitlines():
        line = line.strip()
        if (not line or line.startswith("#") or not line.endswith("/")
                or any(ch in line for ch in "*?![")):
            continue
        name = line[:-1].lstrip("/")
        if name and "/" not in name:
            names.add(name)
    return names


def _walked_projects(where):
    """The fallback for a tree that is not a git checkout."""
    skip = _generated_dirs(where)
    out = []
    for folder, dirs, files in os.walk(where):
        dirs[:] = [d for d in dirs if d not in skip]
        for name in files:
            if name.endswith(".csproj"):
                rel = os.path.relpath(os.path.join(folder, name), where)
                out.append(rel.replace(os.sep, "/"))
    return out


def disagreements(table=None):
    """
    Where the mirrored tables have drifted from Directory.Build.props.

    Both are DERIVABLE from the props - a `-windows` suffix means the
    desktop targets, and `net8.0-windows` means .NET 8 - so a mirror is
    only worth keeping if something checks it. Nothing did.
    """
    table = _table() if table is None else table
    out = []

    listed, parsed = set(RELEASES), set(table)
    for release in sorted(parsed - listed):
        out.append({"release": release, "field": "RELEASES",
                    "props": table[release], "here": None,
                    "why": "Directory.Build.props targets this release and "
                           "RELEASES does not list it, so nothing here would "
                           "ever try to build it"})
    for release in sorted(listed - parsed):
        out.append({"release": release, "field": "RELEASES",
                    "props": None, "here": "listed",
                    "why": "RELEASES lists this release and the props file "
                           "gives it no runtime, so a build would fail with "
                           "an empty TargetFramework"})

    for release in sorted(listed & parsed):
        tfm = table[release]
        wants_desktop = tfm.endswith("-windows")
        if wants_desktop != (release in NEEDS_WINDOWS_DESKTOP):
            out.append({"release": release, "field": "NEEDS_WINDOWS_DESKTOP",
                        "props": tfm,
                        "here": release in NEEDS_WINDOWS_DESKTOP,
                        "why": "the props target %s, so the desktop targets "
                               "are %sneeded" % (tfm, "" if wants_desktop
                                                 else "not ")})
        major = _TFM_MAJOR.match(tfm)
        wanted = int(major.group(1)) if major else None
        mine = MINIMUM_DOTNET_MAJOR.get(release)
        if wants_desktop and wanted is not None and mine != wanted:
            out.append({"release": release, "field": "MINIMUM_DOTNET_MAJOR",
                        "props": tfm, "here": mine,
                        "why": "the props target %s, which is .NET %d"
                               % (tfm, wanted)})
    return out


def packages(project=None):
    """
    Every PackageReference, as written. Nothing is resolved or fetched.

    The Revit API ones carry `$(RevitVersion).*`, so the package set
    follows the release rather than being pinned per release.
    """
    where = project or os.path.join(ROOT, *PROJECTS[2].split("/"))
    try:
        text = io.open(where, encoding="utf-8").read()
    except (IOError, OSError):
        return []

    out = []
    for name, version in re.findall(
            r'PackageReference\s+Include="([^"]+)"\s+Version="([^"]+)"', text):
        out.append({"package": name, "version": version,
                    "follows_release": "$(RevitVersion)" in version})
    return out


def check(releases=None, table=None):
    """
    {checked, buildable, blocked, mismatches} - or a refusal. Nothing is
    compiled; %s is what compiles.
    """
    wanted = list(RELEASES if releases is None else releases)
    if not wanted:
        return {"checked": False, "refused": "NOTHING_TO_CHECK",
                "why": "an empty list of releases was handed in. That is not "
                       "the same as asking about all of them - omit the "
                       "argument for that."}

    unknown = [r for r in wanted if r not in RELEASES]
    if unknown:
        return {"checked": False, "refused": "UNKNOWN_RELEASE",
                "why": "Heron does not know which .NET runtime Revit %s "
                       "uses. Supported today: %s. D-05 - Autodesk has moved "
                       "the runtime twice, so Heron will not guess."
                       % (", ".join(unknown), ", ".join(RELEASES))}

    present, said = have_dotnet()
    if not present:
        return {"checked": False, "refused": "NO_DOTNET",
                "why": "there is no .NET SDK on this machine: %s. See "
                       "docs/30-compiling-away-from-windows.md - it takes "
                       "about five minutes on any Linux container." % said}

    table = _table() if table is None else table
    desktop = windows_desktop_toolchain()
    sdks = sorted(set(major for major, _ in installed_sdks()))

    buildable, blocked = [], []
    for release in wanted:
        card = {"release": release, "tfm": table.get(release),
                "needsWindowsDesktop": release in NEEDS_WINDOWS_DESKTOP,
                "needsDotnet": MINIMUM_DOTNET_MAJOR.get(release)}
        stopped = why_unbuildable(release, desktop)
        if stopped:
            blocked.append(dict(card, why=stopped))
        else:
            buildable.append(card)

    drift = disagreements(table)
    refs = packages()
    return {
        "checked": True,
        "dotnet": said,
        "sdks": sdks,
        "desktop": desktop,
        "of": len(wanted),
        "buildable": buildable,
        "blocked": blocked,
        "mismatches": drift,
        "packages": refs,
        "compiled": False,
        "why": "%d release%s: %d this machine could build, %d it could not, "
               "%d table row%s drifted from Directory.Build.props. Nothing "
               "was compiled."
               % (len(wanted), "" if len(wanted) == 1 else "s",
                  len(buildable), len(blocked), len(drift),
                  "" if len(drift) == 1 else "s"),
        "unjudged": [
            "NOTHING WAS COMPILED. This says what a build would need; %s is "
            "what builds, and its answer is the only one that counts. A "
            "release listed as buildable here can still fail there, and that "
            "is the point of running it." % THE_GATE,
            ("%d RELEASE%s COULD NOT BE BUILT HERE, WHICH IS NOT THE SAME AS "
             "UNSUPPORTED. Each one names what is missing, and it is nearly "
             "always one package away."
             % (len(blocked), "" if len(blocked) == 1 else "S")
             if blocked else
             "every release asked about could be built on this machine, so "
             "nothing would be skipped."),
            ("%d MIRRORED TABLE ROW%s HAS DRIFTED from Directory.Build.props. "
             "Nothing checked these before this agent existed."
             % (len(drift), "" if len(drift) == 1 else "S")
             if drift else
             "the mirrored tables still agree with Directory.Build.props on "
             "every release - checked, not assumed."),
            "WHETHER THE PACKAGE FEED CARRIES A RELEASE. The Revit API "
            "packages are written as `$(RevitVersion).*`, so the set follows "
            "the release by construction - but whether NuGet actually "
            "publishes one for a given year is a fact about the internet, "
            "and nothing here went and looked.",
            "THE RELEASE LIST IS AN EXPLICIT EDIT, NOT A DERIVATION. Adding "
            "a Revit release has to be a deliberate change in the props AND "
            "here, because D-05 says an unlisted release is an error rather "
            "than a guess. It is checked against the props, never replaced "
            "by them.",
        ],
    }


check.__doc__ = check.__doc__ % THE_GATE


def main(argv):
    print("DOTNET COMPATIBILITY   what a build would need, before it starts")
    print("=" * 72)

    answer = check(argv or None)
    if not answer.get("checked"):
        print("\n%s\n  %s" % (answer["refused"], answer["why"]))
        return 0

    print("\ndotnet %s, SDK majors %s, WindowsDesktop targets: %s"
          % (answer["dotnet"], ", ".join(str(n) for n in answer["sdks"]),
             answer["desktop"]))
    print("\n%s" % answer["why"])

    print("\n%-8s %-18s %-8s %s" % ("release", "target", "desktop", "verdict"))
    for card in answer["buildable"]:
        print("  %-6s %-18s %-8s could build here"
              % (card["release"], card["tfm"],
                 "yes" if card["needsWindowsDesktop"] else "no"))
    for card in answer["blocked"]:
        print("  %-6s %-18s %-8s SKIPPED - %s"
              % (card["release"], card["tfm"],
                 "yes" if card["needsWindowsDesktop"] else "no",
                 card["why"][:34]))

    print("\npackages, as written - nothing was resolved")
    for ref in answer["packages"]:
        print("  %-46s %-14s %s"
              % (ref["package"], ref["version"],
                 "follows the release" if ref["follows_release"] else ""))

    print("\nmirrored tables against Directory.Build.props")
    if not answer["mismatches"]:
        print("  every row agrees - checked, not assumed")
    for one in answer["mismatches"]:
        print("  DRIFTED  %-6s %-22s %s"
              % (one["release"], one["field"], one["why"][:36]))

    print("\nrefused")
    for these in ([], ["2028"], ["2019", "2020"]):
        bad = check(these)
        print("  %-18s %s" % (bad["refused"], bad["why"][:46]))
    # NO_DOTNET, reached honestly: the SDK is here, so the PATH it is found
    # on is taken away for one call rather than the answer being imagined.
    was = os.environ.get("PATH", "")
    os.environ["PATH"] = os.path.join(ROOT, "no-such-bin")
    try:
        bad = check(["2024"])
    finally:
        os.environ["PATH"] = was
    print("  %-18s %s" % (bad["refused"], bad["why"][:46]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
