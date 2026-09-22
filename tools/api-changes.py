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
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL_DIR = os.path.join(ROOT, "tools", "api-surface")
CACHE = os.path.join(TOOL_DIR, ".assemblies")
SURFACE = os.path.join(TOOL_DIR, ".surface")
READER = os.path.join(TOOL_DIR, "bin", "Debug", "ApiSurface.dll")
OUT = os.path.join(TOOL_DIR, "changes.json")

sys.path.insert(0, os.path.join(ROOT, "brain"))

# For short() and nothing else. heron_relpath, NOT heron_fragment: this tool
# answers NOT RUN without importing heron_fragment and the PyYAML it needs.
import heron_relpath as RELPATH                                  # noqa: E402


def short(path):
    """`path` as the repository spells it, for a message - never a raised error.

    EVERY USE OF THIS IS DISPLAY: a refusal naming the file it refused, or
    the closing line saying what was written. `os.path.relpath` RAISES
    `ValueError` on Windows when the two paths are on different drives - the
    repository on D: and a redirected OUT under the temp directory on C: is
    the shape that found it - so formatting the path could take down the very
    refusal it was being formatted for, turning a clean exit 1 into a
    traceback. AGENTS.md has four states and a traceback is none of them.

    CI can never see this. Linux has one mount, so relpath always answers
    there, and the two calls guarded here sit in the REFUSING TO WRITE path -
    the one branch whose whole job is to fail cleanly.

    An absolute path is a worse message. It is never a worse outcome.

    THE RULE IS brain/heron_relpath's, and this only binds ROOT, read at the
    call. Until row 5b-152 (2026-09-22) this carried its own try/except - a
    copy, made because heron_fragment, where the rule then lived, costs
    PyYAML and this tool answers NOT RUN without it. heron_relpath imports
    nothing but `os`, so the copy went.
    """
    return RELPATH.relpath(path, ROOT)


def releases():
    """The supported releases, read from `brain/heron_fragment` and not typed.

    NAMED PRECISELY, because the comment that used to sit here named the
    wrong owner: it said `tools/check-api-surface.py` owns the list and that
    this file is "bound rather than retyped so one list stays one list". It
    is bound to `heron_fragment.REVIT_VERSIONS`, and measured 2026-09-22
    check-api-surface.py types its OWN `ALL_VERSIONS`, so there are three
    lists - that one, this module's source, and `heron_dotnet.RELEASES`.

    All three agree today; what was wrong was a sentence telling the next
    reader that a binding exists where it does not. AJ Tools' L3 is why the
    list is read at all: a hardcoded version list silently installed nothing
    on three releases while the document advertised them.
    """
    import heron_fragment as FRAG
    return list(FRAG.REVIT_VERSIONS)


def cannot_answer(years):
    """Why this machine cannot produce the digest, or None.

    ASKED AFTER THE ARGUMENT QUESTIONS AND BEFORE ANY DUMP, which is the
    order `tools/check-fragments-compile.py` settled on for the same reason:
    "two releases are needed" is answerable with no SDK at all and must keep
    its own exit code.

    MEASURED 2026-09-22 on a container with no cached assemblies and no .NET
    SDK: `api-changes.py 2025 2026` raised `OSError` out of `surface_of` and
    exited **1** - the code a real failure uses - with the explanation buried
    in a traceback. A traceback is none of AGENTS.md's four states, and this
    is the third state: NOT RUN. Row 5b-133 found the same shape in
    check-fragments-compile and its remedy is exit 3.

    A release whose surface is already dumped needs neither the assemblies
    nor the SDK, so the work needed is worked out first and the tools are
    asked about only if some remains.
    """
    todo = [year for year in years
            if not (os.path.isfile(os.path.join(SURFACE, "%s.txt" % year))
                    and os.path.getsize(
                        os.path.join(SURFACE, "%s.txt" % year)) > 0)]
    if not todo:
        return None

    bare = [year for year in todo
            if not (os.path.isdir(os.path.join(CACHE, year))
                    and os.listdir(os.path.join(CACHE, year)))]
    if bare:
        return ("the reference assemblies for %s are not cached, so no "
                "surface can be read for them. Run `python "
                "tools/check-api-surface.py %s` first - it fetches them."
                % (", ".join(bare), " ".join(bare)))
    if shutil.which("dotnet") is None:
        return ("there is no `dotnet` on PATH, so the surface reader cannot "
                "be run and nothing is claimed about what %s changed"
                % ", ".join(todo))
    if not os.path.isfile(READER):
        return ("%s is not built, so no surface can be dumped. Run `dotnet "
                "build tools/api-surface`." % short(READER))
    return None


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
            % short(READER))

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

    blocked = cannot_answer(years)
    if blocked:
        sys.stdout.write("NOT RUN - %s\n" % blocked)
        sys.stdout.write(
            "Nothing was written, and nothing is claimed about what these "
            "releases changed.\n")
        return 3

    found = changes(years)

    # A TARGETED RUN REFRESHES, IT DOES NOT REPLACE. `api-changes.py 2025
    # 2026` is documented at the top of this file, and until 2026-09-15 it
    # wrote a digest holding that one transition over the committed one
    # holding all seven. HERON-REVIT-ACI-034 then had no transition into
    # 2027 - and read the absence as "2027 removed nothing", reporting
    # every fragment clear.
    #
    # Both halves are fixed: that agent now refuses a release it has no
    # evidence for, and this keeps the transitions it did not recompute.
    # Found by a review, not by either file's own suite.
    keep = []
    refreshed = set((one["from"], one["to"]) for one in found)
    if os.path.isfile(OUT):
        # A DIGEST THAT CANNOT BE READ IS NOT AN EMPTY ONE (D-52), and
        # treating it as empty put this defect back through a different door.
        # MEASURED 2026-09-22 on the seven-transition digest truncated
        # mid-file: `api-changes.py 2025 2026` wrote a digest holding ONE
        # transition, exit 0, and said nothing - which is exactly what the
        # block above says was fixed on 2026-09-15.
        try:
            existing = json.loads(io.open(OUT, encoding="utf-8").read())
        except ValueError as why:
            sys.stdout.write(
                "REFUSING TO WRITE: %s exists and does not parse (%s).\n"
                % (short(OUT), why))
            sys.stdout.write(
                "Overwriting it would silently drop every transition this "
                "run did not recompute. Fix or delete the file, then run "
                "again.\n")
            return 1
        if not isinstance(existing, dict):
            sys.stdout.write(
                "REFUSING TO WRITE: %s parses but is a %s, not a digest.\n"
                % (short(OUT), type(existing).__name__))
            sys.stdout.write(
                "Overwriting it would silently drop every transition this "
                "run did not recompute. Fix or delete the file, then run "
                "again.\n")
            return 1
        for one in existing.get("transitions") or []:
            if (one.get("from"), one.get("to")) not in refreshed:
                keep.append(one)

    found = sorted(found + keep, key=lambda one: str(one.get("to")))
    covered = sorted(set([one["from"] for one in found]
                         + [one["to"] for one in found]))

    payload = {
        "releases": covered,
        "refreshed": sorted(years),
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
            "a member that moved up or down a hierarchy - the surfaces "
            "are declared-only, so Element.Name is listed on Element and "
            "not on the types inheriting it, and a member pushed to a "
            "base class reads as removed from the derived type",
        ],
    }
    io.open(OUT, "w", encoding="utf-8").write(
        json.dumps(payload, indent=1, sort_keys=True) + "\n")

    for one in found:
        sys.stdout.write("%s -> %s   %5d removed   %5d added   of %d%s\n"
                         % (one["from"], one["to"], one["removedCount"],
                            one["addedCount"], one["of"],
                            "" if (one["from"], one["to"]) in refreshed
                            else "   (kept, not recomputed)"))
    sys.stdout.write("wrote %s (%d KB)\n"
                     % (short(OUT),
                        os.path.getsize(OUT) // 1024))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
