# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-NUP-019
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
.NET update - every place a Revit release is declared, found rather than
remembered.

    python brain/heron_retarget.py 2028 net10.0-windows

WHAT IT IS FOR (docs/28, HERON-DEV-NUP-019)
--------------------------------------------
"CHANGES PROJECTS. Retargets a framework, bumps packages, migrates
project files. REGRESSION MATRIX MUST PASS BEFORE IT IS ACCEPTED." T1,
risk MODIFY.

ADDING A RELEASE IS NOT ONE EDIT, AND NOBODY HAS EVER WRITTEN DOWN HOW
MANY IT IS
-------------------------------------------------------------------------
Directory.Build.props is the runtime table's home, and it is not the
only place a release list exists. Several agents hold their own, each
for a stated reason: HERON-FRG-VAL-001 validates a fragment's `revit`
against one, HERON-DEV-NET-006 keeps two tables that MIRROR the props
and says so in their comments.

Miss one and nothing fails immediately. The build still works, and the
release is simply invisible to whichever agent was not told - which is
the shape of every version bug this project has had.

So this agent FINDS them. It parses every module in brain/ and tools/
with `ast` and reports each module-level assignment whose value holds
release strings. A year in a docstring is not an assignment and does not
appear; a list, a set or a dict does.

IT WILL NOT GUESS A RUNTIME, AND THE PROPS FILE SAYS WHY
----------------------------------------------------------
Directory.Build.props already refuses this, in its own error text:

    "Heron does not know which .NET runtime Revit $(RevitVersion) uses.
     Autodesk has moved the runtime twice (at 2025 and at 2027), so
     HERON WILL NOT GUESS - confirm the target for that release against
     the Autodesk SDK."

So the runtime is a required input. Derived from the last release it
would say net10.0-windows forever, and it has been wrong twice in eight
releases - D-05 exactly.

IT WRITES NOTHING
-------------------
The register gives this row MODIFY and what comes back is the edit list:
every file, every symbol, and what the new value has to be. Applying it
is the caller's act, the same line HERON-IMP-REN-010 and
HERON-IMP-ARC-011 draw - and here the reason is sharper, because an
agent that edits its own release list and then reports success has
checked nothing.

THE REGRESSION MATRIX IS THE ACCEPTANCE, AND IT IS NAMED
----------------------------------------------------------
The register puts it in the row: the matrix must pass BEFORE the change
is accepted. Both gates are named rather than run - brain may not import
tools/, and neither is cheap - and `accepted` is false until somebody
has run them and said so.
"""

from __future__ import annotations

import ast
import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_dotnet as NET  # noqa: E402
import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

PROPS = os.path.join(ROOT, "Directory.Build.props")

# Where a release list can live. brain/ holds the agents' own tables and
# tools/ holds the gates'; tests/ is excluded because a fixture naming a
# release is a fixture, not a declaration.
SEARCHED = ("brain", "tools")

# A Revit release, as this project spells one.
_RELEASE = re.compile(r"^20\d\d$")

# A target framework moniker, loosely - enough to refuse `2028` or
# `windows` typed into the runtime by mistake, without inventing a table
# of every legal TFM.
_TFM = re.compile(r"^net(\d+\.\d+|\d{2,3})(-[a-z]+)?$")

# What the matrix is, named rather than run. brain may not import tools/,
# and neither of these is cheap.
GATES = (
    ("tools/check-compile.py",
     "every project against every release, which is what proves the new "
     "runtime is reachable at all"),
    ("tools/check-fragments-compile.py",
     "every fragment's C# against every release it claims, and its "
     "contract as well as its syntax"),
)


def declared_in(path):
    """
    Every module-level assignment in one file whose value holds releases.

    PARSED, NOT GREPPED. A year in a docstring is prose - it needs no
    edit and reporting it would bury the four that do. An assignment is
    a declaration.
    """
    try:
        tree = ast.parse(io.open(path, encoding="utf-8",
                                 errors="replace").read())
    except (IOError, OSError, SyntaxError):
        return []

    out = []
    for node in tree.body:
        if not isinstance(node, (ast.Assign, ast.AnnAssign)):
            continue
        targets = (node.targets if isinstance(node, ast.Assign)
                   else [node.target])
        names = [getattr(one, "id", None) for one in targets]
        names = [one for one in names if one]
        if not names or node.value is None:
            continue

        found = [text for text in _constants(node.value)
                 if _RELEASE.match(text)]
        if not found:
            continue
        out.append({"symbol": names[0], "line": node.lineno,
                    "releases": sorted(set(found)),
                    "shape": type(node.value).__name__})
    return out


def _constants(node):
    """Every string constant anywhere inside one expression."""
    out = []
    for child in ast.walk(node):
        if isinstance(child, ast.Constant) and isinstance(child.value, str):
            out.append(child.value)
    return out


def places(root=None):
    """
    Every file that declares a release list, and the props file.

    The props file is first and separate: it is where the runtime table
    actually lives, and the rest MIRROR it.
    """
    where = root or ROOT
    out = [{"file": os.path.relpath(PROPS, where) if root is None
            else "Directory.Build.props",
            "symbol": "HeronTfm", "line": None,
            "releases": sorted(NET._table()) if root is None else [],
            "shape": "MSBuild conditions",
            "isSource": True}]

    for folder in SEARCHED:
        here = os.path.join(where, folder)
        if not os.path.isdir(here):
            continue
        for name in sorted(os.listdir(here)):
            if not name.endswith(".py"):
                continue
            path = os.path.join(here, name)
            for card in declared_in(path):
                card["file"] = "%s/%s" % (folder, name)
                card["isSource"] = False
                out.append(card)
    return out


def plan(release, runtime, root=None):
    """
    {planned, edits, gates, accepted} - or a refusal. Nothing is written.
    """
    release = str(release or "").strip()
    if not release:
        return {"planned": False, "refused": "NO_RELEASE",
                "why": "no release was named."}
    if not _RELEASE.match(release):
        return {"planned": False, "refused": "NOT_A_RELEASE",
                "why": "%r is not a Revit release. This project spells one "
                       "as four digits - 2020 through %s today."
                       % (release, FRAG.REVIT_VERSIONS[-1])}
    if release in FRAG.REVIT_VERSIONS:
        return {"planned": False, "refused": "ALREADY_KNOWN",
                "why": "Revit %s is already supported. Retargeting a "
                       "release Heron already knows would rewrite a table "
                       "that is correct." % release}

    runtime = str(runtime or "").strip()
    if not runtime:
        return {"planned": False, "refused": "NO_RUNTIME",
                "why": "which .NET runtime does Revit %s use? "
                       "Directory.Build.props refuses to guess this in its "
                       "own error text - Autodesk has moved the runtime "
                       "twice in eight releases - so it is asked for, once "
                       "(D-33), and confirmed against the Autodesk SDK."
                       % release}
    if not _TFM.match(runtime):
        return {"planned": False, "refused": "NOT_A_RUNTIME",
                "why": "%r is not a target framework moniker. Heron's are "
                       "%s." % (runtime,
                                ", ".join(sorted(set(NET._table().values()))))}

    known = set(FRAG.REVIT_VERSIONS)
    desktop = runtime.endswith("-windows")
    major = _TFM.match(runtime).group(1).split(".")[0]

    found = places(root)
    edits = []
    for card in found:
        if card["isSource"]:
            edits.append(dict(card, certain=True,
                              add="a HeronTfm condition mapping %s to %s"
                                  % (release, runtime),
                              why="this is where the runtime table lives. "
                                  "Every other place mirrors it"))
            continue

        held = set(card["releases"])
        if len(held) == 1:
            # ONE RELEASE IS A SETTING, NOT A LIST. check-api-surface's
            # BUILD_VERSION is which release the add-in was BUILT for, and
            # telling somebody to add 2028 to it would be a false
            # instruction. Told apart by the data, not by the symbol name.
            edits.append(dict(card, certain=False, add=None,
                              why="this names ONE release (%s), so it is a "
                                  "setting rather than a list. Whether %s "
                                  "changes it is a decision, not an "
                                  "addition - check it by hand"
                                  % (card["releases"][0], release)))
        elif held >= known:
            edits.append(dict(card, certain=True, add=release,
                              why="%s holds every release Heron knows, so "
                                  "%s belongs in it" % (card["symbol"],
                                                        release)))
        else:
            # A SUBSET WAS CHOSEN FOR A REASON, AND THE REASON IS NOT IN
            # THE DATA. What the runtime IMPLIES is stated instead, so
            # whoever applies it has the facts rather than a guess.
            edits.append(dict(
                card, certain=False, add=None,
                implies="%s is %s-suffixed and .NET %s"
                        % (runtime, "windows" if desktop else "not windows",
                           major),
                why="%s holds %d of the %d releases, so it was chosen for a "
                    "reason this agent cannot read. %s needs the "
                    "WindowsDesktop targets and is .NET %s - if that is "
                    "what this subset is about, it belongs"
                    % (card["symbol"], len(held), len(known),
                       release if desktop else "%s does not, and" % release,
                       major)))

    mirrors = [card for card in edits if not card["isSource"]]
    unsure = [card for card in edits if not card.get("certain")]
    return {
        "planned": True,
        "release": release,
        "runtime": runtime,
        "of": len(edits),
        "edits": edits,
        "files": sorted(set(card["file"] for card in edits)),
        "gates": [{"gate": name, "why": why} for name, why in GATES],
        "applied": False,
        "accepted": False,
        "uncertain": [card for card in edits if not card.get("certain")],
        "why": "adding Revit %s on %s touches %d declaration%s across %d "
               "file%s - %d certain, %d that need a human eye. Nothing was "
               "written, and nothing is accepted until the matrix has run."
               % (release, runtime, len(edits),
                  "" if len(edits) == 1 else "s",
                  len(set(card["file"] for card in edits)),
                  "" if len(set(card["file"] for card in edits)) == 1
                  else "s", len(edits) - len(unsure), len(unsure)),
        "unjudged": [
            "THE RUNTIME WAS NOT DERIVED. Directory.Build.props refuses to "
            "guess it in its own error text: Autodesk has moved it twice "
            "in eight releases, at 2025 and at 2027, so deriving it from "
            "the last one would say net10.0-windows forever (D-05).",
            "NOTHING WAS WRITTEN. What comes back is the edit list - every "
            "file, every symbol, every new value. An agent that edits its "
            "own release list and then reports success has checked "
            "nothing, so applying it is the caller's act.",
            ("%d PLACE%s MIRROR THE PROPS FILE: %s. Miss one and nothing "
             "fails immediately - the build still works and the release is "
             "simply invisible to whichever agent was not told, which is "
             "the shape of every version bug this project has had."
             % (len(mirrors), "" if len(mirrors) == 1 else "S",
                ", ".join(sorted(set("%s.%s" % (card["file"], card["symbol"])
                                     for card in mirrors))))),
            ("%d DECLARATION%s %s A HUMAN EYE: %s. One release is a "
             "SETTING, not a list - `add 2028` to it would be a false "
             "instruction - and a SUBSET was chosen for a reason that is "
             "not in the data. What the runtime implies is stated instead "
             "of a guess."
             % (len(unsure), "" if len(unsure) == 1 else "S",
                "NEEDS" if len(unsure) == 1 else "NEED",
                ", ".join("%s.%s" % (card["file"], card["symbol"])
                          for card in unsure))
             if unsure else
             "every declaration holds the full release list, so every edit "
             "is certain."),
            "DECLARATIONS WERE PARSED, NOT GREPPED. A year in a docstring "
            "is prose and needs no edit; reporting it would bury the ones "
            "that do. Only module-level assignments appear here.",
            "NOTHING IS ACCEPTED UNTIL THE MATRIX HAS RUN. The register "
            "puts that in this row, and both gates are named rather than "
            "run - brain may not import tools/, and neither is cheap. "
            "`accepted` stays false until somebody has run them and said "
            "so.",
            "PACKAGES WERE NOT BUMPED. The Revit API packages are written "
            "as `$(RevitVersion).*` so they follow the release by "
            "construction - HERON-DEV-NET-006 reports that - and whether "
            "NuGet actually publishes one for %s is a fact about the "
            "internet that nothing here went and looked up." % release,
        ],
    }


def main(argv):
    print("DOTNET UPDATE   every place a release is declared")
    print("=" * 72)

    found = places()
    print("\n%d declaration(s), parsed rather than grepped" % len(found))
    for card in found:
        print("  %-28s %-22s %s"
              % (card["file"], card["symbol"],
                 "SOURCE - the runtime table" if card["isSource"]
                 else ", ".join(card["releases"][:4])
                      + ("" if len(card["releases"]) <= 4 else ", ...")))

    release = argv[0] if argv else "2028"
    runtime = argv[1] if len(argv) > 1 else "net10.0-windows"
    answer = plan(release, runtime)
    print("\n%s" % answer["why"])
    for card in answer["edits"]:
        print("  %-28s %-22s %s"
              % (card["file"], card["symbol"],
                 ("add %s" % card["add"])[:34] if card.get("certain")
                 else "BY HAND - %s" % card["why"][:24]))

    print("\nand then, before it is accepted")
    for gate in answer["gates"]:
        print("  %-38s %s" % (gate["gate"], gate["why"][:30]))
    print("  accepted: %s" % answer["accepted"])

    print("\nrefused")
    for these, runtime in ((None, "net48"), ("banana", "net48"),
                           ("2024", "net48"), ("2028", None),
                           ("2028", "windows")):
        bad = plan(these, runtime)
        print("  %-18s %s" % (bad["refused"], bad["why"][:46]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
