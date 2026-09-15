# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-MAIN-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Repository interaction - the department in order, and the claim that
nothing here posts, proved.

    python brain/heron_github.py

WHAT IT IS FOR (docs/28, HERON-GIT-MAIN-001)
---------------------------------------------
"Owns repository interaction." T1, risk PUBLISH.

SIX AGENTS SAY THIS ONE OWNS IT. THIS ONE SAYS NOBODY DOES
------------------------------------------------------------
Every other agent in this department ends its answer with some version
of "nothing was posted; HERON-GIT-MAIN-001 owns repository interaction".
That sentence is true and it is easy to read as "and THAT one does the
posting".

It does not. Under D-01 the host has the network, and every contract in
this department declares `allowed-tools: []`. So this agent's first job
is to stop being a place where that claim is taken on trust: it READS
the contracts and reports what each one is allowed to do, rather than
asserting that they are allowed nothing.

If a contract ever declares a tool, this page says so instead of saying
"none". That is the difference between a claim and a check.

THE SECOND JOB IS THE ORDER
-----------------------------
Like HERON-WSP-ARC-001 one department over, this is the only place the
whole sequence is seen at once - and the order carries reasons rather
than being a list somebody arranged. A pull request before a commit is
a pull request with nothing in it; a release before a version number is
a tag nobody can order.

TWO ROWS ARE GATED, AND docs/28 SAYS WHICH
--------------------------------------------
HERON-GIT-PR-005 - "explicit confirmation, every time".
HERON-GIT-COM-010 - "per-item human review of the actual payload".

Nothing else in the department carries one in the register, and no gate
is invented for the rows that do not. HERON-GIT-REL-007 asks for a
confirmation anyway, and that is its own reading of PUBLISH rather than
a requirement docs/28 stated - which is said here rather than blurred.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_contract as CON  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

AGENTS = os.path.join(ROOT, "brain", "agents")

# The department, in the order a thing actually happens, with the reason
# each step comes where it does.
ORDER = (
    ("HERON-GIT-CHG-009", "what a change reaches, before anybody decides "
                          "what to do about it"),
    ("HERON-GIT-BRN-003", "a branch, because the work has to live "
                          "somewhere before it is committed"),
    ("HERON-GIT-CMT-004", "a commit, because a pull request before a "
                          "commit is a pull request with nothing in it"),
    ("HERON-GIT-PR-005", "a pull request, once there is something to "
                         "review"),
    ("HERON-GIT-ISS-006", "an issue, which needs none of the above and "
                          "sits here because it is the other thing a "
                          "repository is for"),
    ("HERON-GIT-VER-008", "the version, because a release before a number "
                          "is a tag nobody can order"),
    ("HERON-GIT-REL-007", "the release, last of the internal ones - "
                          "everything above it has to be true first"),
    ("HERON-GIT-COM-010", "a contribution outward, which leaves this "
                          "project entirely and so comes after "
                          "everything that keeps it honest"),
)

RANK = dict((agent, index) for index, (agent, _) in enumerate(ORDER))

# docs/28's own words, on the only two rows that carry a gate.
GATED = {
    "HERON-GIT-PR-005": "explicit confirmation, every time",
    "HERON-GIT-COM-010": "per-item human review of the actual payload",
}

# What somebody actually asks for, and which steps that is.
INTENTS = {
    "open a pull request": ("HERON-GIT-BRN-003", "HERON-GIT-CMT-004",
                            "HERON-GIT-PR-005"),
    "cut a release": ("HERON-GIT-VER-008", "HERON-GIT-REL-007"),
    "contribute upstream": ("HERON-GIT-COM-010",),
    "file an issue": ("HERON-GIT-ISS-006",),
    "see what a change reaches": ("HERON-GIT-CHG-009",),
}


def surface(where=None):
    """
    Every agent in this department and what its contract lets it do.

    READ, not asserted. Six agents say this one owns repository
    interaction; this is where that stops being taken on trust.
    """
    folder = where or AGENTS
    found = []
    if not os.path.isdir(folder):
        return found
    for name in sorted(os.listdir(folder)):
        if not name.startswith("HERON-GIT-") or not name.endswith(".yaml"):
            continue
        try:
            contract = CON.load(os.path.join(folder, name))
        except Exception:                            # noqa: BLE001
            found.append({"agent": name[:-5], "tools": None,
                          "why": "its contract could not be read, so "
                                 "nothing here can say what it may do"})
            continue
        tools = contract.get("allowed-tools")
        found.append({
            "agent": str(contract.get("agent") or name[:-5]).strip(),
            "tools": list(tools) if tools else [],
            "failures": len(contract.get("failures") or [])})
    return found


def plan(intent, where=None):
    """
    {steps, reaches_outside, gated, why} - or a refusal. Nothing runs.
    """
    if not intent:
        return {"planned": False, "refused": "NOTHING_TO_PLAN",
                "why": "no intent was handed in."}

    wanted = str(intent).strip().lower()
    steps = INTENTS.get(wanted)
    if steps is None:
        return {"planned": False, "refused": "NOT_AN_INTENT",
                "why": "'%s' is not something this department does. It "
                       "does: %s. An intent nobody listed would be planned "
                       "by guessing which agents sound relevant, which is "
                       "reading the words rather than the register."
                       % (intent, "; ".join(sorted(INTENTS)))}

    known = surface(where)
    if not known:
        return {"planned": False, "refused": "NO_CONTRACTS",
                "why": "no contracts could be read from %s, so nothing "
                       "here can say what any of these agents is allowed "
                       "to do - and saying 'they post nothing' without "
                       "reading them is the assertion this agent exists "
                       "to replace." % os.path.relpath(AGENTS, ROOT)}

    book = dict((one["agent"], one) for one in known)
    outward = [one for one in known if one["tools"]]
    unreadable = [one for one in known if one["tools"] is None]

    return {
        "planned": True, "intent": wanted,
        "steps": [{"agent": agent,
                   "why": dict(ORDER)[agent],
                   "tools": (book.get(agent) or {}).get("tools"),
                   "gate": GATED.get(agent)}
                  for agent in sorted(steps, key=lambda one: RANK[one])],
        "gated": [agent for agent in steps if agent in GATED],
        "reaches_outside": [one["agent"] for one in outward],
        "unreadable": [one["agent"] for one in unreadable],
        "of": len(known),
        "why": "%d step(s) for '%s', in the order the register's own "
               "dependencies put them. %s"
               % (len(steps), wanted,
                  "None of the %d agents in this department declares a "
                  "tool, so none of them posts anything." % len(known)
                  if not outward and not unreadable else
                  "%d of %d declare a tool: %s."
                  % (len(outward), len(known),
                     ", ".join(one["agent"] for one in outward))),
        "unjudged": [
            "%s" % ("WHETHER ANY OF THIS POSTS - THAT WAS READ, NOT "
                    "ASSERTED. All %d contracts in this department declare "
                    "`allowed-tools: []`, so the host does every "
                    "outward step under D-01. If one ever declares a "
                    "tool this line changes rather than staying "
                    "reassuring." % len(known)
                    if not outward and not unreadable else
                    "%d AGENT(S) DECLARE A TOOL AND %d COULD NOT BE READ. "
                    "The claim that nothing here posts no longer holds as "
                    "written." % (len(outward), len(unreadable))),
            "%s" % ("%d STEP(S) NEED A PERSON, AND docs/28 SAYS SO: %s."
                    % (len([one for one in steps if one in GATED]),
                       "; ".join("%s - %s" % (one, GATED[one])
                                 for one in steps if one in GATED))
                    if any(one in GATED for one in steps) else
                    "no step here carries a gate in the register, and none "
                    "is invented. HERON-GIT-REL-007 asks for a "
                    "confirmation anyway - that is its own reading of "
                    "PUBLISH, not a requirement docs/28 stated."),
            "WHETHER THE STEPS SHOULD RUN AT ALL. This is an order, not a "
            "decision. Each agent refuses on its own terms, and this one "
            "has read none of the work.",
            "EVERYTHING OUTSIDE THIS DEPARTMENT. A release needs four "
            "green gates that other agents own, and a contribution needs "
            "a payload somebody else wrote.",
        ],
    }


def main(argv):
    print("REPOSITORY INTERACTION   the department in order, and nothing "
          "here posts")
    print("=" * 72)

    known = surface()
    print("\nwhat each contract in this department allows, read off disk")
    for one in known:
        print("  %-22s tools %-6s %d declared failure(s)"
              % (one["agent"],
                 "none" if one["tools"] == [] else str(one["tools"]),
                 one.get("failures", 0)))

    print("\nthe order, and why each step is where it is")
    for agent, why in ORDER:
        print("  %-22s %s" % (agent, why))

    for intent in ("open a pull request", "cut a release",
                   "contribute upstream"):
        answer = plan(intent)
        print("\n%s" % answer["why"])
        for step in answer["steps"]:
            print("  %-22s %s" % (step["agent"],
                                  "GATE: %s" % step["gate"] if step["gate"]
                                  else step["why"][:44]))

    print("\nrefused")
    for intent, kwargs in ((None, {}), ("do something clever", {}),
                           ("file an issue", {"where": "/nowhere"})):
        bad = plan(intent, **kwargs)
        print("  %-18s %s" % (bad["refused"], bad["why"][:46]))

    print("\nwhat this agent does not judge")
    for line in plan("open a pull request")["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
