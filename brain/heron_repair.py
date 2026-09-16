# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-REP-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Folder repair - dry-run by default, and never across a class boundary.

    python brain/heron_repair.py

WHAT IT IS FOR (docs/28, HERON-WSP-REP-004)
--------------------------------------------
"Corrects placement. **Dry-run by default**." T1, risk MODIFY. It takes
what HERON-WSP-VAL-003 found misplaced and says where each file should
go instead.

DRY-RUN BY DEFAULT IS IN THE REGISTER ROW, SO IT IS IN THE SIGNATURE
----------------------------------------------------------------------
`apply` defaults to False and there is no setting, no environment
variable and no config field that can change what the default is. A
default that something else can flip is not a default - it is a value
with an extra step, and the extra step is always taken on the machine
where it matters.

MOVING A FILE ACROSS A CLASS BOUNDARY IS NOT A REPAIR
-------------------------------------------------------
This is the rule the agent is built around, and it is the one a
placement fixer gets wrong.

docs/06 s2 splits the workspace into product, data and derived. A file in
the wrong FOLDER is a tidiness problem. A file moved into the wrong
CLASS is a different thing entirely:

  data -> product    the next update replaces it wholesale and a
                     modeller's work is gone
  product -> data    it survives the update that was meant to replace
                     it, and Heron runs old code that looks current
  anything -> derived  the next cleanup deletes it, correctly, because
                     derived state is what cleanup may remove outright

So every move is checked against HERON-WSP-PTH-007, and one that changes
class is REFUSED rather than proposed - even when the layering gate is
the thing that asked for it. A repair that needs a class change is a
design decision somebody makes on purpose, not a file this agent slides
sideways.

IT NEVER OVERWRITES AND NEVER DELETES
---------------------------------------
A destination that already holds something is refused. Two files that
both believe they belong at one path is a question about which is
current, and answering it by overwriting destroys the evidence needed to
answer it properly.

WHAT IT DOES NOT DECIDE
-------------------------
Where a file BELONGS is HERON-WSP-VAL-003's answer and this agent takes
it as given - it does not re-derive the layout rules, because two
opinions about where `brain/` starts is worse than one.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import heron_paths as PATHS                                    # noqa: E402


def _move(finding):
    """(from, to) for one validation finding, or (None, None)."""
    if not isinstance(finding, dict):
        return None, None
    return (str(finding.get("path") or "").strip(),
            str(finding.get("belongs") or finding.get("should_be")
                or "").strip())


def plan(findings, apply=False, exists=None):
    """
    {moves, refused, applied, why} - or a refusal. Dry-run unless asked.

    `findings` is what HERON-WSP-VAL-003 reported. `exists` is a reader
    asked whether a destination is already taken - handed in, because
    this agent does not touch the disk.
    """
    if not findings:
        return {"applied": False, "refused": "NOTHING_MISPLACED",
                "why": "nothing was reported misplaced. A repair run with "
                       "no findings is not a tidy workspace - it is a run "
                       "nobody asked for."}

    taken = exists if callable(exists) else (
        lambda path: path in set(exists or []))

    moves, refused, claimed = [], [], {}
    for finding in findings:
        source, destination = _move(finding)
        if not source or not destination:
            return {"applied": False, "refused": "NOT_A_FINDING",
                    "why": "a finding must say both where the file IS and "
                           "where it BELONGS. %r says %s."
                           % (finding,
                              "neither" if not source and not destination
                              else "only where it is" if source
                              else "only where it belongs")}

        # THE RULE THIS AGENT EXISTS FOR.
        was = PATHS.classify(source)
        goes = PATHS.classify(destination)
        if was["class"] != goes["class"]:
            refused.append({
                "path": source, "to": destination,
                "refused": "WOULD_CROSS_A_CLASS_BOUNDARY",
                "from_class": was["class"], "to_class": goes["class"],
                "why": "%s is %s and %s is %s. docs/06 s2 keeps those "
                       "apart for a reason - %s - so this is a design "
                       "decision somebody makes on purpose, not a file "
                       "this agent slides sideways."
                       % (source, was["class"], destination, goes["class"],
                          _cost(was["class"], goes["class"]))})
            continue

        if taken(destination):
            refused.append({
                "path": source, "to": destination,
                "refused": "DESTINATION_TAKEN",
                "why": "%s already holds something. Two files that both "
                       "believe they belong there is a question about "
                       "which is current, and overwriting destroys the "
                       "evidence needed to answer it." % destination})
            continue

        # AND TAKEN BY THIS PLAN COUNTS. `exists` answers about the disk
        # as it stands, and it cannot know about a move this same run has
        # already proposed - so two findings pointing at one ABSENT
        # destination both passed, and running the plan overwrote the
        # first move with the second. The no-overwrite guarantee held
        # against the disk and not against the plan's own second half.
        if destination in claimed:
            refused.append({
                "path": source, "to": destination,
                "refused": "DESTINATION_TAKEN",
                "claimed_by": claimed[destination],
                "why": "%s is already where this same plan moves %s. "
                       "Nothing holds it on disk yet, which is why "
                       "`exists` cannot see it - the second move would "
                       "land on the first. Two files that both believe "
                       "they belong there is a question about which is "
                       "current, and it is the same question whether the "
                       "collision is with the disk or with this run."
                       % (destination, claimed[destination])})
            continue
        claimed[destination] = source

        moves.append({"path": source, "to": destination,
                      "class": was["class"],
                      "why": "stays %s, so this is placement and nothing "
                             "else" % was["class"]})

    return {
        "applied": False, "moves": moves, "refused_moves": refused,
        "why": "%d move(s) proposed, %d refused. %s"
               % (len(moves), len(refused),
                  "DRY RUN - nothing was moved." if not apply else
                  "`apply` was asked for, and this agent still moved "
                  "nothing: it returns the moves and a caller makes them."),
        "unjudged": [
            "NOTHING WAS MOVED, and `apply` does not change that. The "
            "register row says dry-run by DEFAULT; this agent goes one "
            "step further and does not touch the disk at all, so what "
            "`apply` really records is that somebody meant it.",
            "WHERE A FILE BELONGS IS HERON-WSP-VAL-003's ANSWER, taken as "
            "given. This agent does not re-derive the layout rules - two "
            "opinions about where brain/ starts is worse than one.",
            "whether the destination is taken ON DISK was ASKED, not "
            "looked up. A caller that answers from a stale listing gets a "
            "plan to overwrite a file this agent was never told about. "
            "Collisions WITHIN this plan are caught here, because `exists` "
            "answers about the disk as it stands and cannot know about a "
            "move this same run has already proposed.",
            "THIS IS FOR THE INSTALLED WORKSPACE, NOT THE SOURCE REPOSITORY, "
            "and the two share folder names. HERON-WSP-PTH-007 classifies "
            "by name against docs/06 s2, so the repository's own `brain/` - "
            "which holds source code - reads as the DATA class, because in "
            "an installed workspace `Brain` is the knowledge store. Pointed "
            "at a checkout, the class check above is answering a different "
            "question from the one it looks like it is answering.",
        ],
    }


def _cost(was, goes):
    """What crossing that particular boundary actually costs."""
    if was == PATHS.DATA and goes == PATHS.PRODUCT:
        return ("the next update replaces product wholesale, and a "
                "modeller's work would go with it")
    if was == PATHS.PRODUCT and goes == PATHS.DATA:
        return ("it would survive the update meant to replace it, and "
                "Heron would run old code that looks current")
    if goes == PATHS.DERIVED:
        return "the next cleanup deletes derived state, correctly"
    if was == PATHS.DERIVED:
        return ("something rebuilds it there, and it would come back "
                "beside the copy that was moved")
    return ("one of the two is UNKNOWN, and a move nobody can classify "
            "is not one to make quietly")


def main(argv):
    print("FOLDER REPAIR   dry-run by default, never across a class")
    print("=" * 72)

    findings = [
        {"path": "brain/heron_tools.py", "belongs": "brain/tools/heron_tools.py"},
        {"path": "Fragments/duct-sizing.yaml", "belongs": "Core/duct-sizing.yaml"},
        {"path": "Core/Heron.Old.dll", "belongs": "Cache/Heron.Old.dll"},
        {"path": "mcp/server/x.py", "belongs": "mcp/client/x.py"},
    ]
    answer = plan(findings, exists=["mcp/client/x.py"])
    print("  %s" % answer["why"])

    print()
    print("  WOULD MOVE (placement only):")
    for move in answer["moves"]:
        print("    %-34s -> %s" % (move["path"], move["to"]))

    print()
    print("  REFUSED:")
    for move in answer["refused_moves"]:
        print("    %-34s %s" % (move["path"], move["refused"]))
        print("        %s" % move["why"][:92])

    print()
    print("  `apply=True` still moves nothing:")
    answer = plan(findings[:1], apply=True)
    print("    applied=%s  %s" % (answer["applied"], answer["why"][:82]))

    print()
    print("  A file in the wrong FOLDER is tidiness. A file moved into the")
    print("  wrong CLASS is a modeller's work deleted by the next update,")
    print("  or old code running because it survived one.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
