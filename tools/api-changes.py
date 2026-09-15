# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
What each Revit release stopped shipping. The evidence, not the answer.

    python tools/api-changes.py                 all eight, in order
    python tools/api-changes.py 2025 2026        just that transition

This produces `tools/api-surface/changes.json`, which
`HERON-REVIT-ACI-034` reads. It is a tool and not that agent: it needs
the network, 264 MB of reference assemblies and a .NET SDK, and an agent
that can only answer on a machine with all three answers nowhere useful.
The split is the one `HERON-DEV-NET-006` and `tools/check-compile.py`
already use.

WHY THE DIGEST IS COMMITTED AND THE SURFACES ARE NOT
------------------------------------------------------
A release's full public surface is about 3 MB and there are eight of
them. What changed between two of them is a few hundred lines. The
surfaces are the working, the digest is the result, and the result is
what survives a fresh checkout with no network.

REMOVALS ARE KEPT IN FULL. ADDITIONS ARE COUNTED
--------------------------------------------------
A member that disappeared breaks code that calls it. A member that
appeared breaks nothing - it is worth knowing how much a release grew,
which is a number, and not worth 18,000 lines of names nothing can act
on.

WHAT THIS CANNOT SEE, AND THE REGISTER ASKS FOR IT
----------------------------------------------------
docs/28 gives `HERON-REVIT-ACI-034` "deprecated and renamed APIs,
changed methods, parameters, units, namespaces, and SILENT BEHAVIOURAL
CHANGES". Reading two assemblies finds a member that is gone. It cannot
find:

  a member still there that returns a different unit
  a member still there that now throws where it used to return null
  a member still there whose meaning changed

Nothing static finds those, and a tool that implied otherwise would be
worse than one that says so. `D3` in NEEDS-CHECKING.md - move the ducts,
then measure one - is still what catches a unit change.

A DEPRECATION IS NOT A REMOVAL, AND THIS SEES ONLY REMOVALS
-------------------------------------------------------------
`[Obsolete]` is an attribute, and this reads names. A member marked
deprecated in 2024 and deleted in 2026 appears here in the 2025 -> 2026
transition and nowhere earlier - which is two years later than a warning
would have been. Reading the attribute is the obvious next step and it
is not done here, so the answer does not claim it.
"""

import io
import json
import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL_DIR = os.path.join(ROOT, "tools", "api-surface")
CACHE = os.path.join(TOOL_DIR, ".assemblies")
SURFACE = os.path.join(TOOL_DIR, ".surface")
READER = os.path.join(TOOL_DIR, "bin", "Debug", "ApiSurface.dll")
OUT = os.path.join(TOOL_DIR, "changes.json")

# tools/check-api-surface.py owns which releases are supported and how
# they are fetched. Bound rather than retyped so one list stays one list.
sys.path.insert(0, os.path.join(ROOT, "brain"))


def releases():
    """The supported releases, from the agent that owns the list."""
    import heron_fragment as FRAG
    return list(FRAG.REVIT_VERSIONS)


def surface_of(year):
    """That release's public surface, dumped if it is not already."""
    path = os.path.join(SURFACE, "%s.txt" % year)
    if os.path.isfile(path) and os.path.getsize(path) > 0:
        return path

    assemblies = os.path.join(CACHE, year)
    if not os.path.isdir(assemblies) or not os.listdir(assemblies):
        raise IOError(
            "NO_ASSEMBLIES: %s is not cached. Run "
            "`python tools/check-api-surface.py %s` first - it fetches the "
            "reference assemblies this reads." % (year, year))
    if not os.path.isfile(READER):
        raise IOError(
            "NO_READER: %s is not built. Run `dotnet build tools/api-surface`."
            % os.path.relpath(READER, ROOT))

    os.makedirs(SURFACE, exist_ok=True)
    result = subprocess.run(
        ["dotnet", READER, "--dump", assemblies, path],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if result.returncode != 0:
        raise IOError("DUMP_FAILED: %s" % result.stdout.decode("utf-8",
                                                               "replace"))
    return path


def members(path):
    return set(line.strip() for line
               in io.open(path, encoding="utf-8").read().splitlines()
               if line.strip())


def changes(years):
    """Each adjacent pair, what left and how much arrived."""
    out = []
    for older, newer in zip(years, years[1:]):
        was = members(surface_of(older))
        now = members(surface_of(newer))
        gone = sorted(was - now)
        out.append({
            "from": older,
            "to": newer,
            "removed": gone,
            "removedCount": len(gone),
            "addedCount": len(now - was),
            "of": len(was),
        })
    return out


def main(argv):
    years = [one for one in argv if one.isdigit()] or releases()
    if len(years) < 2:
        sys.stdout.write("two releases are needed to see a change; got %s\n"
                         % ", ".join(years))
        return 2

    found = changes(years)
    payload = {
        "releases": years,
        "transitions": found,
        "reads": "the public types and members each release's reference "
                 "assemblies ship, by NAME. A removal breaks code that "
                 "calls it. Additions are counted rather than listed - they "
                 "break nothing.",
        "cannotSee": [
            "a member still there that returns a different unit",
            "a member still there that now throws where it returned null",
            "a member still there whose meaning changed",
            "a deprecation - [Obsolete] is an attribute and this reads "
            "names, so a member marked in 2024 and deleted in 2026 appears "
            "here only at 2025 -> 2026",
            "a changed SIGNATURE - matching is by name, so an overload "
            "removed and a parameter added both read as no change",
        ],
    }
    io.open(OUT, "w", encoding="utf-8").write(
        json.dumps(payload, indent=1, sort_keys=True) + "\n")

    for one in found:
        sys.stdout.write("%s -> %s   %5d removed   %5d added   of %d\n"
                         % (one["from"], one["to"], one["removedCount"],
                            one["addedCount"], one["of"]))
    sys.stdout.write("wrote %s (%d KB)\n"
                     % (os.path.relpath(OUT, ROOT),
                        os.path.getsize(OUT) // 1024))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
