# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-SKL-CMP-005
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Skill composition - the graph is acyclic, and a declared risk may not
understate what it composes.

    python brain/heron_composition.py

WHAT IT IS FOR (docs/28, HERON-SKL-CMP-005)
--------------------------------------------
"Builds skills from skills. Validates the graph is ACYCLIC, and
propagates the highest risk level upward." T2, risk READ. It builds
nothing: a verdict and an effective risk come back.

THE CYCLE IS NAMED, NOT ONLY REPORTED
---------------------------------------
"There is a cycle" is true and useless. The path is what somebody
fixes: a -> b -> c -> a tells them which edge to cut. So the answer
carries the loop in order, starting and ending at the same skill, and a
skill that uses itself is a loop of one rather than a special case.

A DECLARED RISK LOWER THAN WHAT IT COMPOSES IS REFUSED
--------------------------------------------------------
Propagating upward means the effective risk of a skill is the highest of
its own and everything it uses, all the way down. That much is
arithmetic.

What is NOT arithmetic is what to do when the author declared something
lower. Raising it quietly would mean a skill's declared risk stops
meaning anything - the card would say READ, the thing would do MODIFY,
and the card is the one place a person looks before running it. So it is
REFUSED, and the author fixes the declaration.

Declaring HIGHER than the composition is allowed and reported. An author
being cautious about their own skill is not an error, and refusing it
would teach people to declare the minimum.

THE RISK LADDER IS IMPORTED, AND THERE ARE ALREADY FOUR COPIES
----------------------------------------------------------------
This file adds none. It imports HERON-KRN-CAP-008's `RISK_ORDER`,
because the capability agent is the closest thing brain/ has to an owner
of what an agent may do.

But `brain/` carries FOUR full copies of that ladder under three names -
heron_capability.RISK_ORDER, heron_events.RISK_ORDER,
heron_hr.RISK_LADDER, and an inline tuple in heron_skill.validate() -
plus three more modules deriving from it. They are equal today and they
are four places to drift tomorrow. Recorded as PROPOSALS F22.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_capability as CAP  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# HERON-KRN-CAP-008's, imported rather than copied. See F22.
RISK = CAP.RISK_ORDER


def _rank(risk):
    """Where a risk sits, or None if it is not one."""
    risk = str(risk or "").strip().upper()
    return RISK.index(risk) if risk in RISK else None


def _cycle(name, uses, seen, stack):
    """The loop reachable from `name`, in order, or None."""
    seen.add(name)
    stack.append(name)
    for used in uses.get(name, []):
        if used not in seen:
            found = _cycle(used, uses, seen, stack)
            if found:
                return found
        elif used in stack:
            # THE PATH, not the fact. a -> b -> c -> a tells somebody
            # which edge to cut; "there is a cycle" tells them nothing.
            return stack[stack.index(used):] + [used]
    stack.pop()
    return None


def compose(skills):
    """
    {graph, effective, why, unjudged} - or a refusal.

    Nothing is built. The effective risk of each skill is the highest of
    its own and everything it uses.
    """
    if not skills:
        return {"composed": False, "refused": "NOTHING_TO_COMPOSE",
                "why": "no skills were handed in. An empty graph is acyclic "
                       "and says nothing, which is not the same as a graph "
                       "that was checked."}

    declared, uses = {}, {}
    for skill in skills:
        if not isinstance(skill, dict):
            return {"composed": False, "refused": "NOT_A_SKILL",
                    "why": "%r is not a skill. Each is {name, risk, uses}."
                           % (skill,)}
        name = str(skill.get("name") or "").strip()
        if not name:
            return {"composed": False, "refused": "NOT_A_SKILL",
                    "why": "a skill has no name. Nothing can use it and "
                           "nothing can report on it."}
        if name in declared:
            return {"composed": False, "refused": "NOT_A_SKILL",
                    "why": "'%s' appears twice. Two skills under one name "
                           "means `uses` is ambiguous and the graph is not "
                           "a graph." % name}
        rank = _rank(skill.get("risk"))
        if rank is None:
            return {"composed": False, "refused": "NOT_A_RISK",
                    "why": "'%s' declares risk %r, which is not one of %s - "
                           "imported from HERON-KRN-CAP-008 rather than "
                           "listed here."
                           % (name, skill.get("risk"), ", ".join(RISK))}
        declared[name] = rank
        uses[name] = [str(each).strip() for each in (skill.get("uses") or [])
                      if str(each).strip()]

    for name in sorted(uses):
        unknown = sorted(set(used for used in uses[name]
                             if used not in declared))
        if unknown:
            return {"composed": False, "refused": "UNKNOWN_SKILL",
                    "why": "'%s' uses %s, which %s not among the %d handed "
                           "in. A graph with a missing node cannot be "
                           "checked for cycles OR for risk - the missing "
                           "one could carry either."
                           % (name, ", ".join("'%s'" % each
                                              for each in unknown),
                              "is" if len(unknown) == 1 else "are",
                              len(declared))}

    for name in sorted(uses):
        loop = _cycle(name, uses, set(), [])
        if loop:
            return {"composed": False, "refused": "A_CYCLE",
                    "loop": loop,
                    "why": "%s. %s"
                           % (" -> ".join(loop),
                              "A skill that uses itself is a loop of one "
                              "rather than a special case."
                              if len(loop) == 2 else
                              "The path is what somebody fixes: it says "
                              "which edge to cut.")}

    # PROPAGATE UPWARD. Acyclic, so a plain walk terminates.
    effective, walked = {}, {}

    def highest(name):
        if name in effective:
            return effective[name]
        best, through = declared[name], None
        for used in uses[name]:
            inner = highest(used)
            if inner > best:
                best, through = inner, used
        effective[name] = best
        walked[name] = through
        return best

    understated = []
    for name in sorted(declared):
        best = highest(name)
        if best > declared[name]:
            understated.append({
                "skill": name, "declared": RISK[declared[name]],
                "effective": RISK[best], "through": walked[name],
                "why": "'%s' declares %s and composes %s through '%s'. "
                       "Raising it quietly would mean a declared risk stops "
                       "meaning anything - the card would say %s, the thing "
                       "would do %s, and the card is the one place a person "
                       "looks before running it."
                       % (name, RISK[declared[name]], RISK[best],
                          walked[name], RISK[declared[name]], RISK[best])})

    if understated:
        return {"composed": False, "refused": "RISK_IS_UNDERSTATED",
                "understated": understated,
                "why": "%d skill(s) declare less than they compose: %s."
                       % (len(understated),
                          ", ".join("%s says %s and does %s"
                                    % (one["skill"], one["declared"],
                                       one["effective"])
                                    for one in understated))}

    cautious = [{"skill": name, "declared": RISK[declared[name]],
                 # WHAT IT COMPOSES, not what it declares. Reporting the
                 # declaration back would make every cautious row read
                 # "says MODIFY and does MODIFY", which is the one thing
                 # a cautious row is not.
                 "composes": RISK[max(effective[used]
                                      for used in uses[name])]}
                for name in sorted(declared)
                if uses[name] and declared[name] > max(
                    effective[used] for used in uses[name])]

    return {
        "composed": False, "effective": dict(
            (name, RISK[effective[name]]) for name in sorted(effective)),
        "declared": dict((name, RISK[declared[name]])
                         for name in sorted(declared)),
        "cautious": cautious, "of": len(skills),
        "why": "%d skill(s), acyclic, %d declaring more than they compose. "
               "Nothing was built."
               % (len(skills), len(cautious)),
        "unjudged": [
            "NOTHING WAS BUILT. A verdict and an effective risk come back; "
            "HERON-SKL-CRE-002 authors and HERON-SKL-UPD-003 revises.",
            "THE GRAPH IS ACYCLIC AND THAT WAS CHECKED FROM EVERY SKILL, "
            "not only from the ones nothing uses. A cycle reachable from "
            "one entry point is a cycle.",
            "%s" % ("%d SKILL(S) DECLARE MORE THAN THEY COMPOSE: %s. That "
                    "is allowed and reported - an author being cautious "
                    "about their own skill is not an error, and refusing it "
                    "would teach people to declare the minimum."
                    % (len(cautious),
                       ", ".join(one["skill"] for one in cautious))
                    if cautious else
                    "every declared risk equals what it composes."),
            "THE RISK LADDER WAS IMPORTED FROM HERON-KRN-CAP-008 AND THIS "
            "FILE ADDS NO COPY. brain/ already carries four full copies "
            "under three names - PROPOSALS F22.",
        ],
    }


def main(argv):
    print("SKILL COMPOSITION   acyclic, and a card may not understate")
    print("=" * 72)

    print("\nthe ladder, imported from HERON-KRN-CAP-008")
    print("  %s" % " < ".join(RISK))

    good = compose([
        {"name": "tag-sheet", "risk": "MODIFY", "uses": ["find-ducts",
                                                         "place-tag"]},
        {"name": "find-ducts", "risk": "READ", "uses": []},
        {"name": "place-tag", "risk": "MODIFY", "uses": []},
        {"name": "report", "risk": "PUBLISH", "uses": ["find-ducts"]},
    ])
    print("\n%s" % good["why"])
    for name in sorted(good["effective"]):
        print("  %-12s declared %-8s effective %s"
              % (name, good["declared"][name], good["effective"][name]))

    print("\nrefused")
    for skills in (
            [{"name": "a", "risk": "READ", "uses": ["b"]},
             {"name": "b", "risk": "READ", "uses": ["c"]},
             {"name": "c", "risk": "READ", "uses": ["a"]}],
            [{"name": "a", "risk": "READ", "uses": ["a"]}],
            [{"name": "a", "risk": "READ", "uses": ["b"]},
             {"name": "b", "risk": "ADMIN", "uses": []}],
            [{"name": "a", "risk": "READ", "uses": ["missing"]}],
            [{"name": "a", "risk": "DELETE", "uses": []}]):
        bad = compose(skills)
        print("  %-22s %s" % (bad["refused"], bad["why"][:46]))

    print("\nwhat this agent does not judge")
    for line in good["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
