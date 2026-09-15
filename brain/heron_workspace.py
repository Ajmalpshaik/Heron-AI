# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-ARC-001
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain

"""
The Workspace Architect - the only place where two plans are seen
together.

NOT `heron_architect.py`: that file is HERON-AHR-ARC-003, the AGENT
Architect, which turns a job description into a contract. Two agents in
this register are called "Architect" and they are not related.

    python brain/heron_workspace.py

WHAT IT IS FOR (docs/28, HERON-WSP-ARC-001)
--------------------------------------------
"Owns the physical structure." T2, risk ADMIN - the highest in the
register, and the head of the twelve-agent Folder Architecture
department. Every other agent in it answers one question and sees only
its own:

    REG-012  what exists          CRE-002  which folders to make
    VAL-003  what is misplaced    REP-004  how to correct it
    PLC-005  where a new thing    TPL-006  what a template makes
             belongs              MIG-008  which migrations run
    BAK-010  what to copy         CLN-009  what to archive

Each of those is right on its own. **Put two of them in one run and
they can want opposite things about one path**, and no agent above sees
it because there is no agent above. That is what this one is for.

WHAT ONLY THIS AGENT CAN SEE
------------------------------
    CLN-009  archive  Fragments/
    PLC-005  write    Fragments/select-by-name.json

Neither is wrong. The cleanup is archiving a folder and the placement
is putting something in it, and whichever runs second loses. A conflict
is found on a path OR ON ITS PARENT, because that second case is the
one nobody else is looking for.

ADMIN DOES NOT MEAN IT MAY ACT
--------------------------------
It runs nothing. It orders plans and refuses runs. Three things keep it
that way:

  Golden Rule 7, docs/14 s46   "No agent approves itself." An intent
                               authored by ARC-001 in the list it is
                               reviewing is WOULD_APPROVE_ITSELF - and
                               that is the rule an ADMIN department head
                               is most placed to break.

  docs/06 s142's own NOTE      "Cleanup Agent and Folder Repair Agent
                               both delete or move user files. Both must
                               be MODIFY-gated, dry-run by default, and
                               must never touch the data class without
                               explicit per-run confirmation."

  a restore is not a step      RST-011 puts the workspace BACK while
                               every other plan moves it forward. The
                               two in one run is a contradiction, not an
                               ordering problem.

And PTH-007 is left out of the order for a third reason: docs/28's own
Does column calls it "product / data / derived separation enforced in
code". It is the table everything else asks, not a thing that happens to
a disk, so it has no place in a sequence. Its Risk cell in docs/28 is
empty, which is corroboration rather than evidence - three empty Risk
cells in that register are still an open question for the owner, and
this agent does not read an empty cell as a stated zero.

THE ORDER IS DECLARED, NOT WORKED OUT
---------------------------------------
Read what exists, then copy it, then build, then change, then tidy. A
backup that runs after a migration has backed up the wrong thing; a
cleanup that runs before a migration takes what the chain needs. The
order is a table, so it can be read and argued with, rather than a
sort nobody can see the reasoning of.

INTENTS ARE HANDED IN, REDUCED BY THE CALLER
----------------------------------------------
Each plan arrives as {agent, does, path}. This agent does NOT parse
eight different answer shapes: a second copy of eight shapes is eight
things to drift. What it cannot check is whether the reduction was
faithful, and it says so rather than implying it checked.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_paths as PATHS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

SELF = "HERON-WSP-ARC-001"
RESTORE = "HERON-WSP-RST-011"

# Read what is there, copy it, build, change, tidy. A backup after a
# migration has backed up the wrong thing; a cleanup before one takes what
# the chain needs.
ORDER = (
    ("HERON-WSP-REG-012", "what exists, and under which identity"),
    ("HERON-WSP-VAL-003", "what is misplaced - read only, so it is free"),
    ("HERON-WSP-BAK-010", "copy the data class BEFORE anything changes it"),
    ("HERON-WSP-CRE-002", "make the folders the rest of the run needs"),
    ("HERON-WSP-MIG-008", "migrate, on a backup that already exists"),
    ("HERON-WSP-TPL-006", "apply templates into folders that now exist"),
    ("HERON-WSP-PLC-005", "place what is new"),
    ("HERON-WSP-REP-004", "correct what is misplaced, once placing is done"),
    ("HERON-WSP-CLN-009", "archive what is unused, last and never first"),
)
RANK = dict((agent, place) for place, (agent, _why) in enumerate(ORDER))
WHY = dict(ORDER)

# What a plan may say it does. Anything else is a vocabulary this agent
# does not have, and it refuses rather than assuming the safest reading.
DOES = ("read", "create", "write", "move", "remove", "replace")

# The four that make a path different afterwards.
CHANGES = ("create", "write", "move", "remove", "replace")

# docs/06 s142: both delete or move user files, so both are gated on the
# data class.
GATED = ("HERON-WSP-CLN-009", "HERON-WSP-REP-004")


def _under(one, other):
    """Is `one` the same path as `other`, or inside it?"""
    a = str(one or "").strip().strip("/").lower()
    b = str(other or "").strip().strip("/").lower()
    if not a or not b:
        return False
    return a == b or a.startswith(b + "/") or b.startswith(a + "/")


def review(intents, confirmed=None):
    """
    {run, order, gated, why, unjudged} - or a refusal.

    Nothing runs. An ordered list comes back, or a reason it may not.
    """
    if not isinstance(intents, (list, tuple)) or not intents:
        return {"approved": False, "refused": "NOT_A_PLAN",
                "why": "a review needs a list of intents, each {agent, does, "
                       "path}. An empty run reports success and changes "
                       "nothing, which reads exactly like a working one."}

    reduced = []
    for raw in intents:
        if not isinstance(raw, dict):
            return {"approved": False, "refused": "NOT_A_PLAN",
                    "why": "%r is not an intent. Each is {agent, does, "
                           "path}, reduced from a plan by the caller."
                           % (raw,)}
        agent = str(raw.get("agent") or "").strip().upper()
        does = str(raw.get("does") or "").strip().lower()
        path = str(raw.get("path") or "").strip()

        if agent == SELF:
            return {"approved": False, "refused": "WOULD_APPROVE_ITSELF",
                    "why": "%s is in the list it is reviewing. Golden Rule 7 "
                           "(docs/14 s46): no agent approves itself - and an "
                           "ADMIN department head is the one most placed to "
                           "break it." % SELF}
        if agent == RESTORE:
            return {"approved": False, "refused": "RESTORE_IS_NOT_A_STEP",
                    "why": "%s puts the workspace BACK while every other "
                           "plan moves it forward. The two in one run is a "
                           "contradiction rather than an ordering problem, "
                           "so a restore runs on its own or not at all."
                           % RESTORE}
        if agent not in RANK:
            return {"approved": False, "refused": "NOT_A_PLAN",
                    "why": "'%s' is not one of the nine this run can order. "
                           "A tenth is not given a place here: the order is "
                           "a table so it can be read and argued with."
                           % (agent or raw.get("agent"))}
        if does not in DOES:
            return {"approved": False, "refused": "NOT_A_PLAN",
                    "why": "'%s' is not something a plan does. Known: %s. "
                           "An unknown word is refused rather than read as "
                           "the safest one." % (does, ", ".join(DOES))}
        if not path:
            return {"approved": False, "refused": "NOT_A_PLAN",
                    "why": "an intent by %s says it will %s, and names no "
                           "path. There is nothing to order or to compare."
                           % (agent, does)}
        reduced.append({"agent": agent, "does": does, "path": path,
                        "class": PATHS.classify(path)["class"]})

    # THE THING NOBODY ELSE CAN SEE. Two agents, one path or one inside the
    # other, at least one of them changing it.
    for i, first in enumerate(reduced):
        for second in reduced[i + 1:]:
            if first["agent"] == second["agent"]:
                continue
            if not _under(first["path"], second["path"]):
                continue
            changing = [one for one in (first, second)
                        if one["does"] in CHANGES]
            if not changing:
                continue
            return {"approved": False, "refused": "PLANS_CONFLICT",
                    "why": "%s would %s '%s' and %s would %s '%s'. Neither "
                           "is wrong on its own and whichever runs second "
                           "loses - %s. This is the only place the two are "
                           "seen together."
                           % (first["agent"], first["does"], first["path"],
                              second["agent"], second["does"], second["path"],
                              "the same path" if first["path"].strip("/") ==
                              second["path"].strip("/") else
                              "one is inside the other, which is the case "
                              "nobody else is looking for"),
                    "conflict": [first, second]}

    # docs/06 s142: cleanup and repair never touch the data class without
    # explicit per-run confirmation.
    said_yes = set(str(each).strip().strip("/").lower()
                   for each in (confirmed or []))
    gated, missing = [], []
    for one in reduced:
        if one["agent"] not in GATED:
            continue
        if one["class"] != PATHS.DATA or one["does"] not in CHANGES:
            continue
        gated.append(one)
        if one["path"].strip("/").lower() not in said_yes:
            missing.append(one)

    if missing:
        return {"approved": False, "refused": "NO_CONFIRMATION",
                "why": "%d intent(s) by %s touch the DATA class and were not "
                       "confirmed: %s. docs/06 s142: both delete or move "
                       "user files, and must never touch the data class "
                       "without explicit per-run confirmation. An automatic "
                       "cleanup that removes a fragment the user spent a "
                       "month refining is unrecoverable trust damage."
                       % (len(missing), " or ".join(GATED),
                          ", ".join("%s %s" % (one["agent"][-7:], one["path"])
                                    for one in missing)),
                "needs": missing}

    order = sorted(reduced, key=lambda one: (RANK[one["agent"]],
                                             one["path"].lower()))
    return {
        "approved": False, "order": order, "of": len(order),
        "gated": gated,
        "why": "%d intent(s), ordered %s. Nothing ran: the order comes back "
               "and a caller runs it."
               % (len(order),
                  " then ".join(sorted(set(one["agent"][-7:] for one in order),
                                       key=lambda tail: RANK[
                                           "HERON-WSP-" + tail]))),
        "unjudged": [
            "NOTHING RAN, AND ADMIN DOES NOT MEAN IT MAY. This agent orders "
            "plans and refuses runs. It has the department's highest risk "
            "and the least reach, which is the arrangement Golden Rule 7 "
            "asks for.",
            "THE INTENTS WERE REDUCED BY THE CALLER, and whether the "
            "reduction was faithful to each plan cannot be checked here. "
            "Parsing eight answer shapes would be eight copies to drift; "
            "this is the cost of not doing that, and it is a real one.",
            "%s" % ("%d intent(s) touch the DATA class and were confirmed "
                    "by path. docs/06 s142 asks for an explicit per-run "
                    "confirmation; naming the paths is a stricter reading "
                    "than the words require, taken because 'yes' to a run "
                    "is not 'yes' to a file." % len(gated)
                    if gated else
                    "no intent touches the DATA class through cleanup or "
                    "repair, so docs/06 s142's gate did not apply. That is "
                    "not a statement that the run is safe."),
            "A CONFLICT IS FOUND, NOT RESOLVED. Which of two plans should "
            "give way is a decision, and D-35 says an unapproved thing is "
            "refused rather than warned about. The run stops and a person "
            "chooses.",
        ],
    }


def main(argv):
    print("WORKSPACE ARCHITECT   the only place two plans are seen together")
    print("=" * 72)

    print("\nthe order, and why it is that order")
    for place, (agent, why) in enumerate(ORDER, 1):
        print("  %d. %-20s %s" % (place, agent[-7:], why))

    good = review([
        {"agent": "HERON-WSP-CLN-009", "does": "move", "path": "Logs/old"},
        {"agent": "HERON-WSP-PLC-005", "does": "write",
         "path": "Fragments/select-by-name.json"},
        {"agent": "HERON-WSP-BAK-010", "does": "read", "path": "Brain"},
        {"agent": "HERON-WSP-CRE-002", "does": "create", "path": "Company"},
    ], confirmed=["Logs/old"])
    print("\n%s" % good["why"])
    for one in good["order"]:
        print("  %-8s %-8s %-32s %s" % (one["agent"][-7:], one["does"],
                                        one["path"], one["class"]))

    print("\nrefused")
    for intents, confirmed in (
            ([{"agent": "HERON-WSP-CLN-009", "does": "move",
               "path": "Fragments"},
              {"agent": "HERON-WSP-PLC-005", "does": "write",
               "path": "Fragments/select-by-name.json"}], None),
            ([{"agent": "HERON-WSP-CLN-009", "does": "move",
               "path": "Fragments/old.json"}], None),
            ([{"agent": SELF, "does": "write", "path": "Core"}], None),
            ([{"agent": RESTORE, "does": "replace", "path": "Brain"}], None),
            ([{"agent": "HERON-WSP-XXX-999", "does": "read", "path": "x"}],
             None)):
        answer = review(intents, confirmed=confirmed)
        print("  %-22s %s" % (answer["refused"], answer["why"][:62]))

    print("\nwhat this agent does not judge")
    for line in good["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
