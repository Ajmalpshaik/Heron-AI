# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-BRN-003
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Branching - the only strategy this project has written down is a
prohibition.

    python brain/heron_branch.py

WHAT IT IS FOR (docs/28, HERON-GIT-BRN-003)
--------------------------------------------
"Branching strategy." T1, risk MODIFY.

NOBODY HAS WRITTEN DOWN A BRANCH NAMING CONVENTION, SO NONE IS INVENTED
-------------------------------------------------------------------------
An ordinary branch gets no opinion here. There is no stated shape for a
branch name anywhere in the documents, and a shape guessed by an agent
becomes the convention the moment it is enforced - the same reason
HERON-NAM-GEN-001 was left unbuilt. So the answer for an ordinary branch
says there is no rule, rather than inventing one and calling it a check.

WHAT IS WRITTEN DOWN IS A TRAP, NAMED
---------------------------------------
docs/16 s64 has a whole section called "Why not separate branches per
version":

    "Branch-per-version is the intuitive answer and it is a trap. A fix
    made in one branch has to be cherry-picked into seven others,
    forever."

docs/00 s209 says the same in one line - "ONLY when necessary should
Heron AI create a version-specific branch" - and Golden Rule 4 puts it
last of four: "Keep, extend, adapt, or version-branch - IN THAT ORDER OF
PREFERENCE."

So a version branch is the one branch this agent has something to say
about, and what it says is: show the work.

TWO THINGS ARE ASKED FOR, BOTH BECAUSE THE DOCUMENTS ASK FOR THEM
--------------------------------------------------------------------
  WHY IT IS NECESSARY. "Only when necessary" means the necessity is
  stated, not asserted by the act of branching. docs/16's entire
  argument is that it FEELS necessary and is not.

  WHAT WAS TRIED FIRST. Golden Rule 4 gives an order, and an order
  nobody is asked about is a preference nobody has. Each of keep,
  extend and adapt is asked about once - D-33 - and an answer of "it
  would not work, because..." is an answer. Silence is not.

THE COST IS COMPUTED, NOT DESCRIBED
-------------------------------------
docs/16 says "seven others" because there were eight releases when it
was written. Rather than repeating the seven, this counts: every fix
after this branch exists has to be cherry-picked into one branch per
other declared release, forever. The number comes from
HERON-FRG-VAL-001's list, so it stays true when the list changes.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

VERSIONS = FRAG.REVIT_VERSIONS

# Golden Rule 4, in its order. A version branch is the LAST of four, and
# the three before it are what this agent asks about.
LADDER = ("keep", "extend", "adapt", "version-branch")
LAST = LADDER[-1]

WHY_EACH = {
    "keep": "the working code is left alone",
    "extend": "the working code gains the new case",
    "adapt": "one place absorbs the difference - docs/16 s4's adapter "
             "layer is where a version difference is supposed to live",
}


def propose(branch):
    """
    {allowed, kind, cost, why} - or a refusal. Nothing is created.
    """
    if not branch:
        return {"proposed": False, "refused": "NOTHING_TO_PROPOSE",
                "why": "no branch was handed in."}

    branch = getattr(branch, "data", branch)
    if not isinstance(branch, dict):
        return {"proposed": False, "refused": "NOT_A_BRANCH",
                "why": "%r is not a branch. One is {name} and, when it is "
                       "version-specific, {for_release, because, tried}."
                       % (branch,)}

    name = str(branch.get("name") or "").strip()
    if not name:
        return {"proposed": False, "refused": "NOT_A_BRANCH",
                "why": "the branch has no name."}

    release = str(branch.get("for_release") or "").strip()
    if not release:
        # NO STATED RULE APPLIES, and that is the answer rather than a
        # shape invented here and then enforced as if somebody had
        # chosen it.
        return {
            "proposed": True, "allowed": True, "kind": "ordinary",
            "name": name, "cost": None,
            "why": "'%s' is not version-specific, and no document states a "
                   "shape for a branch name. Nothing here objects, and "
                   "nothing here approves - there is no rule to apply."
                   % name,
            "unjudged": [
                "NO NAMING CONVENTION WAS APPLIED BECAUSE NONE IS WRITTEN "
                "DOWN. A shape guessed by an agent becomes the convention "
                "the moment it is enforced, which is why "
                "HERON-NAM-GEN-001 was left unbuilt too.",
                "THE ONE BRANCHING RULE THIS PROJECT HAS WRITTEN DOWN IS "
                "ABOUT VERSION BRANCHES (docs/16 s64, docs/00 s209, "
                "Golden Rule 4), and this is not one. Hand in "
                "`for_release` and it is checked.",
                "WHETHER THIS BRANCH SHOULD EXIST AT ALL. Nothing here "
                "read the work it is for.",
                "NOTHING WAS CREATED. HERON-GIT-MAIN-001 owns repository "
                "interaction.",
            ]}

    if release not in VERSIONS:
        return {"proposed": False, "refused": "NOT_A_VERSION",
                "why": "'%s' is not a release this project supports: %s. "
                       "D-05 does not extrapolate, and a branch for a "
                       "release nobody supports is a maintenance cost with "
                       "nothing on the other side."
                       % (release, ", ".join(VERSIONS))}

    because = str(branch.get("because") or "").strip()
    if not because:
        return {"proposed": False, "refused": "NOT_SHOWN_NECESSARY",
                "asked": "What makes a branch for %s necessary, when the "
                         "adapter layer is where a version difference is "
                         "supposed to live?" % release,
                "why": "docs/00 s209 allows a version-specific branch ONLY "
                       "when necessary, which means the necessity is "
                       "stated rather than asserted by the act of "
                       "branching. docs/16 s64's whole argument is that "
                       "branch-per-version is 'the intuitive answer and it "
                       "is a trap' - it feels necessary, and that feeling "
                       "is the trap."}

    tried = branch.get("tried") or {}
    if not isinstance(tried, dict):
        return {"proposed": False, "refused": "NOT_A_BRANCH",
                "why": "`tried` is %r. It maps each of %s to what happened "
                       "when it was tried." % (tried,
                                               ", ".join(LADDER[:-1]))}

    silent = [step for step in LADDER[:-1]
              if not str(tried.get(step) or "").strip()]
    if silent:
        return {"proposed": False, "refused": "EARLIER_OPTION_UNTRIED",
                "untried": silent,
                "asked": "What happened when you tried to %s?"
                         % ", then ".join(silent),
                "why": "Golden Rule 4 puts %s LAST of four - 'keep, "
                       "extend, adapt, or version-branch, in that order of "
                       "preference' - and %s %s not been answered for. An "
                       "order nobody is asked about is a preference nobody "
                       "has. 'It would not work, because...' is an answer; "
                       "silence is not."
                       % (LAST, ", ".join(silent),
                          "has" if len(silent) == 1 else "have")}

    # THE COST, COUNTED. docs/16 says "seven others" because there were
    # eight releases when it was written; this counts instead.
    others = [one for one in VERSIONS if one != release]
    return {
        "proposed": True, "allowed": True, "kind": "version",
        "name": name, "for_release": release, "because": because,
        "tried": dict((step, str(tried[step]).strip())
                      for step in LADDER[:-1]),
        "cost": {"cherry_picks_per_fix": len(others), "into": others},
        "why": "'%s' for Revit %s. Every fix after this exists has to be "
               "cherry-picked into %d other branch(es) - %s - forever. "
               "That is docs/16 s64's argument, counted rather than "
               "quoted."
               % (name, release, len(others), ", ".join(others)),
        "unjudged": [
            "THE COST IS %d CHERRY-PICKS PER FIX, FOREVER, and it does not "
            "go away. docs/16 s64 adds the second half: within a year the "
            "branches diverge, and Golden Rule 4 becomes impossible to "
            "verify because there is no single thing to test."
            % len(others),
            "WHETHER THE REASON GIVEN IS A GOOD ONE. It was required and "
            "it was read back, not judged: '%s'. docs/16's whole point is "
            "that this always feels necessary." % because,
            "WHETHER keep, extend AND adapt WERE REALLY TRIED. Each was "
            "answered for, and an answer is what a later reader can "
            "ARGUE with. Nothing here checked the answers.",
            "NOTHING WAS CREATED. HERON-GIT-MAIN-001 owns repository "
            "interaction, and docs/28 gives this agent MODIFY for the day "
            "it does.",
        ],
    }


def main(argv):
    print("BRANCHING   the only strategy written down is a prohibition")
    print("=" * 72)

    print("\nGolden Rule 4, in its order")
    for step in LADDER:
        print("  %-15s %s" % (step, WHY_EACH.get(step, "the last resort")))

    plain = propose({"name": "claude/two-agents"})
    print("\n%s" % plain["why"])

    good = propose({
        "name": "revit-2020-support", "for_release": "2020",
        "because": "2020 needs a 32-bit ElementId the adapter cannot hide "
                   "without changing every caller's signature",
        "tried": {"keep": "the existing code throws on 2020",
                  "extend": "the new case needs a different return type",
                  "adapt": "the adapter would have to expose two types, "
                           "which is the difference, not an absorption"}})
    print("\n%s" % good["why"])

    print("\nrefused")
    for branch in (None, "a string", {"name": ""},
                   {"name": "x", "for_release": "2028"},
                   {"name": "x", "for_release": "2020"},
                   {"name": "x", "for_release": "2020", "because": "it is",
                    "tried": "yes"},
                   {"name": "x", "for_release": "2020", "because": "it is",
                    "tried": {"keep": "throws"}}):
        bad = propose(branch)
        print("  %-26s %s" % (bad["refused"], bad["why"][:40]))

    print("\nwhat this agent does not judge")
    for line in good["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
