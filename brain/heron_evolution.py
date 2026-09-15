# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RAG-EVO-016
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Knowledge evolution - it plans a move, and moving is not removing.

    python brain/heron_evolution.py

WHAT IT IS FOR (docs/28, HERON-RAG-EVO-016)
--------------------------------------------
"Restructures knowledge organisation WHEN IT STOPS FITTING." T3, risk
MODIFY - the only agent in its department that may change where
knowledge lives, and the one with the most to destroy if it is wrong.

"WHEN IT STOPS FITTING" IS NOT A THING THIS AGENT MAY DECIDE
--------------------------------------------------------------
Deciding a scheme has stopped fitting needs a threshold, and every
threshold is invented - the same wall HERON-NAM-TAX-004 refused to build
for fragment areas. So this agent does not watch and pounce. It reports
the strain, with no line drawn through it, and it plans a move ONLY when
somebody asks for one.

That is also what MODIFY means here. An agent that decided on its own
that the organisation had stopped fitting, and then moved a practice's
knowledge, would be right about as often as its threshold was.

FOUR RULES, AND THE FIRST THREE ARE ALREADY SOMEBODY'S
--------------------------------------------------------
  A MOVE IS NOT A REMOVAL     HERON-WSP-CLN-009 archives and never
                              deletes. The same here: every claim that
                              goes in comes out somewhere, and a plan
                              that loses one is refused rather than
                              reported with a count.

  PROJECT KNOWLEDGE STAYS     docs/10 s2. Moving a project's claim to a
  IN ITS PROJECT              shared scope publishes one client's work to
                              every other one, and PROPOSALS F14 is
                              about how quietly that can happen. Refused
                              in both directions: nothing may be moved
                              out of a project, and nothing may be moved
                              into one it did not come from.

  NO BACKUP, NO MOVE          docs/07 s8's migration conditions -
                              idempotent, versioned, reversible or
                              backed up. HERON-WSP-MIG-008 enforces them
                              for a schema; this is the same rule for
                              knowledge, and it is refused rather than
                              warned about.

  NOTHING IS MOVED HERE       A plan comes back. The caller moves, and
                              HERON-WSP-MIG-008 is the agent that will
                              carry it out under conditions this one
                              does not restate.

WHAT THE STRAIN REPORT IS, AND IS NOT
---------------------------------------
Counts per scope, and how many claims in each are unreachable for the
project that holds them. No verdict, no threshold, and no key that
appears only when something is wrong - a scheme holding everything in
one scope returns the same shape as an evenly spread one, so a caller
cannot learn to read the presence of a key as an alarm.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_scope as SCOPE  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Scopes that belong to no project in particular. Read from
# HERON-RAG-LIB-001's own list so there is no second copy to drift.
SHARED = tuple(name for name in SCOPE.SCOPES if name != SCOPE.PROJECT)


def _scope_of(claim):
    """The scope a claim declares, lower cased, or ''."""
    return str(claim.get("scope") or "").strip().lower()


def _project_of(claim):
    """The project a claim belongs to, or '' when it belongs to none."""
    return str(claim.get("project") or "").strip().lower()


def strain(claims):
    """
    {scopes, projects, of, why} - what is where. No verdict.
    """
    scopes, projects = {}, {}
    for claim in claims:
        where = _scope_of(claim) or "unstated"
        scopes[where] = scopes.get(where, 0) + 1
        owner = _project_of(claim)
        if owner:
            projects[owner] = projects.get(owner, 0) + 1
    return {"scopes": scopes, "projects": projects, "of": len(claims),
            "why": "%d claim(s) across %d scope(s) and %d project(s). No "
                   "threshold is applied and nothing is called a problem: "
                   "deciding a scheme has stopped fitting needs a line "
                   "drawn somewhere, and every line is invented."
                   % (len(claims), len(scopes), len(projects))}


def plan(claims, moves=None, backup=None):
    """
    {move, strain, why, unjudged} - or a refusal.

    Nothing is moved. `moves` is what somebody ASKED for, as
    {id, to}; with none, only the strain comes back.
    """
    if not claims:
        return {"moved": False, "refused": "NOTHING_TO_RESTRUCTURE",
                "why": "no claims were handed in. An empty store reports a "
                       "perfectly fitting organisation, which is a statement "
                       "about the call rather than about the knowledge."}

    known = {}
    for claim in claims:
        if not isinstance(claim, dict):
            return {"moved": False, "refused": "NOT_A_CLAIM",
                    "why": "%r is not a claim. Each is {id, scope, project}."
                           % (claim,)}
        identifier = str(claim.get("id") or "").strip()
        if not identifier:
            return {"moved": False, "refused": "NOT_A_CLAIM",
                    "why": "a claim carries no id. A move names what it "
                           "moves, and a claim nobody can name cannot be "
                           "moved OR shown to have stayed."}
        if identifier in known:
            return {"moved": False, "refused": "NOT_A_CLAIM",
                    "why": "'%s' appears twice. Two claims under one id "
                           "means a move would be ambiguous and a count "
                           "would be wrong." % identifier}
        known[identifier] = claim

    where = strain(claims)
    if not moves:
        return {"moved": False, "move": [], "strain": where,
                "of": len(claims),
                "why": "%s Nothing was asked for, so nothing was planned."
                       % where["why"],
                "unjudged": _unjudged(len(claims), 0, where)}

    planned, landing = [], dict((identifier, _scope_of(claim))
                                for identifier, claim in known.items())
    for asked in moves:
        if not isinstance(asked, dict):
            return {"moved": False, "refused": "NOT_A_MOVE",
                    "why": "%r is not a move. Each is {id, to}." % (asked,)}
        identifier = str(asked.get("id") or "").strip()
        to = str(asked.get("to") or "").strip().lower()
        if identifier not in known:
            return {"moved": False, "refused": "NOT_A_MOVE",
                    "why": "'%s' is not one of the %d claims handed in. A "
                           "move of something nobody produced would be "
                           "planned against nothing."
                           % (identifier or asked.get("id"), len(known))}
        # A REQUEST TO REMOVE IS ITS OWN ANSWER, not "that is not a scope".
        # Somebody asking to drop a claim has asked a real question and
        # deserves the rule rather than a shrug about vocabulary.
        if to in ("", "none", "remove", "delete", "drop", "discard"):
            return {"moved": False, "refused": "WOULD_REMOVE_KNOWLEDGE",
                    "claim": identifier,
                    "why": "'%s' would go nowhere. HERON-WSP-CLN-009 "
                           "archives and never deletes, and this is the "
                           "same rule one level up: a restructure MOVES. "
                           "Every claim that goes in comes out somewhere, "
                           "and there is no scope called none."
                           % identifier}
        if to not in SCOPE.SCOPES:
            return {"moved": False, "refused": "NOT_A_MOVE",
                    "why": "'%s' is not a scope. Known: %s - read from "
                           "HERON-RAG-LIB-001 rather than listed here."
                           % (to, ", ".join(SCOPE.SCOPES))}

        claim = known[identifier]
        from_scope = _scope_of(claim)
        owner = _project_of(claim)

        # THE BOUNDARY, BOTH WAYS.
        if owner and to in SHARED:
            return {"moved": False, "refused": "WOULD_LEAVE_ITS_PROJECT",
                    "claim": identifier,
                    "why": "'%s' belongs to project '%s' and this would put "
                           "it in '%s', which belongs to no project. That "
                           "publishes one client's work to every other one "
                           "- docs/10 s2, and PROPOSALS F14 is about how "
                           "quietly it happens."
                           % (identifier, owner, to)}
        if not owner and to == SCOPE.PROJECT:
            return {"moved": False, "refused": "WOULD_LEAVE_ITS_PROJECT",
                    "claim": identifier,
                    "why": "'%s' belongs to no project and this would file "
                           "it under one. Knowledge acquires a client that "
                           "way, and nothing later can tell it was not "
                           "theirs all along." % identifier}

        planned.append({"claim": identifier, "from": from_scope or None,
                        "to": to, "project": owner or None,
                        "why": "stays inside %s."
                               % ("project '%s'" % owner if owner
                                  else "the shared scopes")})
        landing[identifier] = to

    # Every claim lands, and that is true BY CONSTRUCTION rather than by a
    # check: `landing` starts as every claim's current scope and a move
    # overwrites one entry. A check here could not fail, and a check that
    # cannot fail is decoration - the reachable version of this rule is the
    # refusal above, where somebody actually asks for a removal.
    assert len(landing) == len(known)

    # docs/07 s8. Refused rather than warned about.
    if not str(backup or "").strip():
        return {"moved": False, "refused": "NO_BACKUP",
                "would_move": len(planned),
                "why": "%d claim(s) would move and no backup was named. "
                       "docs/07 s8 asks a migration to be idempotent, "
                       "versioned, and reversible or backed up - "
                       "HERON-WSP-MIG-008 enforces that for a schema and "
                       "this is the same rule for knowledge. A restructure "
                       "that cannot be undone is not a restructure, it is "
                       "a rewrite." % len(planned)}

    return {
        "moved": False, "move": planned, "strain": where,
        "backup": str(backup).strip(), "of": len(claims),
        "why": "%d claim(s), %d move(s) planned, backup at %s. Nothing was "
               "moved." % (len(claims), len(planned), str(backup).strip()),
        "unjudged": _unjudged(len(claims), len(planned), where),
    }


def _unjudged(total, moving, where):
    """The same four, whatever the answer - so no key is an alarm."""
    return [
        "NOTHING WAS MOVED. A plan comes back and HERON-WSP-MIG-008 is the "
        "agent that carries one out, under conditions this one does not "
        "restate.",
        "NOTHING WAS CALLED A PROBLEM. %d claim(s) across %d scope(s), and "
        "no threshold was applied to that: deciding an organisation has "
        "stopped fitting needs a line drawn somewhere and every line is "
        "invented - the same wall HERON-NAM-TAX-004 refused to build."
        % (total, len(where["scopes"])),
        "A MOVE IS NOT A REMOVAL. %d claim(s) would move and all %d come "
        "out somewhere; a plan that lost one would be refused rather than "
        "reported with a count." % (moving, total),
        "WHETHER THE NEW ORGANISATION IS BETTER IS NOT JUDGED HERE. The "
        "moves were ASKED FOR, and this agent checked that they are "
        "possible and reversible - not that they are wise.",
    ]


def main(argv):
    print("KNOWLEDGE EVOLUTION   it plans a move, and moving is not removing")
    print("=" * 72)

    claims = [
        {"id": "K-1", "scope": "project", "project": "tower a"},
        {"id": "K-2", "scope": "project", "project": "tower a"},
        {"id": "K-3", "scope": "temporary"},
        {"id": "K-4", "scope": "temporary"},
        {"id": "K-5", "scope": "global"},
    ]

    looked = plan(claims)
    print("\n%s" % looked["why"])
    for name, count in sorted(looked["strain"]["scopes"].items()):
        print("  %-12s %d" % (name, count))

    print("\nrefused")
    for moves, backup in (
            ([{"id": "K-1", "to": "global"}], "Backup/2026-09-15"),
            ([{"id": "K-3", "to": "project"}], "Backup/2026-09-15"),
            ([{"id": "K-3", "to": "company"}], None),
            ([{"id": "K-9", "to": "company"}], "Backup/2026-09-15")):
        answer = plan(claims, moves=moves, backup=backup)
        print("  %-28s %s" % (answer["refused"], answer["why"][:44]))

    good = plan(claims, moves=[{"id": "K-3", "to": "company"},
                               {"id": "K-4", "to": "company"}],
                backup="Backup/2026-09-15")
    print("\n%s" % good["why"])
    for row in good["move"]:
        print("  %-6s %-10s -> %-10s %s" % (row["claim"], row["from"],
                                            row["to"], row["why"]))

    print("\nwhat this agent does not judge")
    for line in good["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
