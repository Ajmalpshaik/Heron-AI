# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-CMP-008
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Import compatibility - unknown is not "all of them".

    python brain/heron_compatibility.py

WHAT IT IS FOR (docs/28, HERON-IMP-CMP-008)
--------------------------------------------
"Determines which Revit and .NET versions the import supports." T1,
risk READ.

WHAT CAN AND CANNOT BE DETERMINED WITHOUT COMPILING
-----------------------------------------------------
Nothing here compiles anything, so "determines" has to mean something
narrower than it sounds. Three things are actually available:

  the import DECLARES releases   check them against D-05's list
  the import DECLARES a runtime  the releases that use it are a lookup
  the import declares NEITHER    nothing is known

The lookup is HERON-FRG-MTX-009's, and that agent parses it out of
Directory.Build.props rather than carrying a table. So the answer to
"which releases use net48" is the build's own answer, not this file's.

UNKNOWN IS THE ONE THAT MATTERS
---------------------------------
An import declaring no compatibility supports NOTHING KNOWN. Reporting
2020 to 2027 there would be the largest silent pass in this project -
eight releases claimed on the strength of nobody having said otherwise.

So an unknown import comes back with an empty `revit` and says why,
in its own field. "Nothing was determined" and "it works everywhere"
must never be the same answer, which is the same rule
HERON-IMP-DUP-007 applies to a comparison nobody could make and
HERON-IMP-VAL-012 to a kind nobody validates.

A RUNTIME NOBODY BUILDS FOR IS A FINDING, NOT A GAP
-----------------------------------------------------
An import targeting net6.0 names a real .NET that no Revit release in
this project uses. That is different from naming nonsense, and
different again from naming nothing - so it comes back as its own
answer with the runtimes that ARE built for, which is the question its
author actually needs answered.

DECLARED AND DERIVED CAN DISAGREE
-----------------------------------
An import can say "2020 to 2027" and target net8.0-windows, which the
build uses for 2025 and 2026 only. Both are reported and neither is
resolved: the declaration is a claim its author made and the runtime is
a fact about the code, and deciding which is wrong needs the code.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402
import heron_matrix as MTX  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# D-05's list, read from HERON-FRG-VAL-001.
VERSIONS = FRAG.REVIT_VERSIONS

# HERON-FRG-MTX-009's, which parses Directory.Build.props rather than
# carrying a table - so "which releases use net48" is the build's own
# answer and not this file's.
runtimes = MTX.runtimes


def look(declared=None, runtime=None):
    """
    {revit, from_runtime, unknown, why} - or a refusal. Nothing is
    compiled and nothing is stored.
    """
    table = runtimes()
    if not table:
        return {"looked": False, "refused": "NO_RUNTIME_TABLE",
                "why": "Directory.Build.props could not be read, so "
                       "nothing here can say which release uses which "
                       ".NET. A table guessed here would be this file's "
                       "opinion rather than what the build actually does."}

    said = [str(one).strip() for one in (declared or [])
            if str(one).strip()]
    strangers = sorted(set(one for one in said if one not in VERSIONS))
    if strangers:
        return {"looked": False, "refused": "NOT_A_VERSION",
                "why": "the import declares %s. Known: %s - D-05 does not "
                       "extrapolate, and an import claiming a release this "
                       "project does not support is a claim about "
                       "something nobody here can check."
                       % (", ".join("'%s'" % one for one in strangers),
                          ", ".join(VERSIONS))}

    target = str(runtime or "").strip().lower()
    built_for = sorted(set(table.values()))
    from_runtime, unbuilt = [], None
    if target:
        from_runtime = sorted(one for one in table if table[one] == target)
        if not from_runtime:
            # A REAL .NET NOBODY HERE BUILDS FOR is its own answer.
            unbuilt = {
                "runtime": target, "built_for": built_for,
                "why": "no Revit release in this project uses %r. The "
                       "build targets %s, and which releases those cover "
                       "is Directory.Build.props's answer rather than this "
                       "file's." % (target, ", ".join(built_for))}

    # UNKNOWN IS NOT ALL OF THEM.
    if not said and not from_runtime:
        return {
            "looked": True, "revit": [], "runtime": target or None,
            "declared": [], "from_runtime": [], "unbuilt": unbuilt,
            "unknown": True,
            "why": "nothing is known about what this import supports. It "
                   "declares no release and %s, so the answer is an empty "
                   "list and not eight releases."
                   % ("names a runtime nobody here builds for" if unbuilt
                      else "no runtime"),
            "unjudged": _unjudged(True, [], [], unbuilt, built_for),
        }

    # A RUNTIME NOBODY HERE BUILDS FOR CONTRADICTS THE DECLARATION, and
    # it used to be reported beside it without either noticing the other:
    # declared 2025 plus net6.0 came back `revit: ['2025']`, supported and
    # undisputed, while `unbuilt` in the same answer said no release in
    # this project uses that runtime. Both cannot be true. The runtime is
    # a fact about the code and the declaration is a claim its author
    # made, so neither is resolved here - but the conflict is named, and
    # the intersection of a claim with an empty fact is empty.
    if unbuilt and said:
        return {
            "looked": True, "revit": [], "runtime": target,
            "declared": said, "from_runtime": [], "unbuilt": unbuilt,
            "disagree": {
                "declared": said, "from_runtime": [],
                "only_declared": said, "only_from_runtime": [],
                "why": "the import declares %s and targets %r, which no "
                       "Revit release in this project uses - the build "
                       "targets %s. A declaration cannot be checked "
                       "against a runtime nobody here builds for, so the "
                       "answer is no release KNOWN to be supported rather "
                       "than the %d the import claims. Both are reported "
                       "and neither is resolved: deciding which is wrong "
                       "needs the code."
                       % (", ".join(said), target, ", ".join(built_for),
                          len(said))},
            "unknown": False,
            "why": "0 release(s) supported: none. The declared %s and the "
                   "runtime %r disagree, and the runtime is one nothing "
                   "here builds for."
                   % (", ".join(said), target),
            "unjudged": _unjudged(False, said, [], unbuilt, built_for),
        }

    both = sorted(set(said) & set(from_runtime)) if said and from_runtime \
        else sorted(set(said) or set(from_runtime))
    disagree = None
    if said and from_runtime and set(said) != set(from_runtime):
        disagree = {
            "declared": said, "from_runtime": from_runtime,
            "only_declared": sorted(set(said) - set(from_runtime)),
            "only_from_runtime": sorted(set(from_runtime) - set(said)),
            "why": "the import declares %s and targets %r, which the build "
                   "uses for %s. Both are reported and neither is resolved "
                   "- the declaration is a claim its author made and the "
                   "runtime is a fact about the code, and deciding which "
                   "is wrong needs the code."
                   % (", ".join(said), target, ", ".join(from_runtime))}

    return {
        "looked": True, "revit": both, "runtime": target or None,
        "declared": said, "from_runtime": from_runtime,
        "unbuilt": unbuilt, "disagree": disagree, "unknown": False,
        "why": "%d release(s) supported: %s.%s"
               % (len(both), ", ".join(both) or "none",
                  " Declared and derived disagree." if disagree else ""),
        "unjudged": _unjudged(False, said, from_runtime, unbuilt,
                              built_for),
    }


def _unjudged(unknown, said, from_runtime, unbuilt, built_for):
    return [
        "%s" % ("NOTHING IS KNOWN, AND THAT IS AN EMPTY LIST RATHER THAN "
                "EIGHT RELEASES. An import declaring no compatibility "
                "supports nothing KNOWN - claiming 2020 to 2027 on the "
                "strength of nobody having said otherwise would be the "
                "largest silent pass in this project." if unknown else
                "%s came from the import's own declaration and %s from its "
                "runtime."
                % (", ".join(said) or "nothing",
                   ", ".join(from_runtime) or "nothing")),
        "%s" % (unbuilt["why"] if unbuilt else
                "the runtime named is one this project builds for."
                if from_runtime else
                "no runtime was named, so none was looked up. The build "
                "targets %s." % ", ".join(built_for)),
        "WHETHER ANY OF IT ACTUALLY COMPILES. Nothing here compiled "
        "anything - this reads a declaration and a runtime name. "
        "HERON-FRG-MTX-009 owns what COMPILES, from tests rather than "
        "assumption, and its four states are never merged.",
        "THE RUNTIME LOOKUP IS HERON-FRG-MTX-009's, WHICH PARSES "
        "Directory.Build.props. So which releases use which .NET is the "
        "build's own answer, and it stays right when the build changes.",
    ]


def main(argv):
    print("IMPORT COMPATIBILITY   unknown is not all of them")
    print("=" * 72)

    table = runtimes()
    print("\nthe build's own table, parsed by HERON-FRG-MTX-009")
    for release in sorted(table):
        print("  %-6s %s" % (release, table[release]))

    for label, kwargs in (
            ("declares nothing", {}),
            ("declares releases", {"declared": ["2024", "2025"]}),
            ("names a runtime", {"runtime": "net8.0-windows"}),
            ("both, agreeing", {"declared": ["2025", "2026"],
                                "runtime": "net8.0-windows"}),
            ("both, disagreeing", {"declared": list(VERSIONS),
                                   "runtime": "net8.0-windows"}),
            ("a runtime nobody builds for", {"runtime": "net6.0"})):
        answer = look(**kwargs)
        print("\n%-28s %s" % (label, answer["why"]))

    print("\nrefused")
    bad = look(declared=["2028"])
    print("  %-20s %s" % (bad["refused"], bad["why"][:46]))

    print("\nwhat this agent does not judge")
    for line in look(declared=["2024"])["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
