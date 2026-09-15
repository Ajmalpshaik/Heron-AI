# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-SKL-RES-001
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Skill research - three questions, three different kinds of answer.

    python tests/test_prebuild.py

WHAT IT PROVES
  1. A SKILL IS WHAT IT NEEDS. The same capabilities in the same domain
     is reported as the same job; the same capabilities in a DIFFERENT
     domain is not.

  2. AN EMPTY NEEDS LIST IS NOT A DUPLICATE OF EVERY OTHER EMPTY ONE,
     and the answer says the comparison was skipped rather than passed.

  3. UTTERANCES ARE MATCHED AS EXACT TEXT, NEVER AS MEANING. D-34's rule,
     proved by handing it two sentences a synonym table would join.

  4. THE HOST GETS A SHORTLIST, NOT THE LIBRARY - and a skill sharing no
     capability is not on it.

  5. SERVED IN 2024 IS NOT SERVED. A capability provided on only some of
     the declared releases is counted apart from served, never inside it.

  6. THE STANDARDS ANSWER IS ALWAYS `answered: False`, names its owners,
     and states the ONE precedence docs/28 states.

  7. THE RELEASE LIST IS HERON-FRG-VAL-001'S BY IDENTITY, and 2028 is
     refused rather than assumed.

  8. NOTHING IS WRITTEN, AND EVERY DECLARED FAILURE IS REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_prebuild as RES                              # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_prebuild.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    counting = {"id": "count-elements", "domain": "revit.reporting",
                "needs": ["FILTER_ELEMENTS_BY_CATEGORY", "COUNT_ELEMENTS"],
                "utterances": ["how many ducts are there"]}
    selecting = {"id": "select-elements", "domain": "revit.selection",
                 "needs": ["SELECT_ELEMENTS"],
                 "utterances": ["select all the ducts"]}

    print("\n1. a skill is what it needs")
    same = RES.research(
        {"name": "tally them", "domain": "revit.reporting",
         "needs": ["COUNT_ELEMENTS", "FILTER_ELEMENTS_BY_CATEGORY"]},
        skills=[counting, selecting])
    check(same["exists"]["same_job"] == ["count-elements"],
          "the same capabilities in the same domain is the same job "
          "under another name")
    # ORDER IS NOT IDENTITY. The proposal lists them the other way round.
    check(same["exists"]["same_job"] and "another name"
          in same["exists"]["why"],
          "and the order they were listed in made no difference")
    elsewhere = RES.research(
        {"name": "tally them", "domain": "revit.mep",
         "needs": ["COUNT_ELEMENTS", "FILTER_ELEMENTS_BY_CATEGORY"]},
        skills=[counting])
    check(elsewhere["exists"]["same_job"] == [],
          "the same capabilities in a DIFFERENT domain is not the same job")
    taken = RES.research({"id": "count-elements", "name": "x"},
                         skills=[counting])
    check(taken["exists"]["id_taken"] == ["count-elements"],
          "and an id already in use is reported plainly")

    print("\n2. an empty needs list is not a duplicate of every other one")
    bare = RES.research({"name": "something new"},
                        skills=[{"id": "also-bare", "domain": "", "needs": []}])
    check(bare["exists"]["same_job"] == [],
          "two skills declaring nothing are not each other")
    check(any("empty set equals every other empty set" in line
              for line in bare["unjudged"]),
          "and the answer says the comparison was SKIPPED, not passed")

    print("\n3. utterances are exact text, never meaning")
    echo = RES.research(
        {"name": "x", "utterances": ["  HOW many   ducts ARE there "]},
        skills=[counting])
    check(echo["exists"]["same_utterances"]
          and echo["exists"]["same_utterances"][0]["utterances"]
          == ["how many ducts are there"],
          "the same sentence is caught through case and spacing")
    # D-34: NO SYNONYM TABLE. These two mean the same to a person and
    # nothing here is allowed to know that.
    meaning = RES.research({"name": "x", "utterances": ["count the ducts"]},
                           skills=[counting])
    check(meaning["exists"]["same_utterances"] == [],
          "'count the ducts' and 'how many ducts are there' are NOT joined "
          "- D-34, there is no synonym table and there will not be one")
    check(any("D-34" in line and "belongs to the host" in line
              for line in meaning["unjudged"]),
          "and the answer hands that question to the host under D-01")

    print("\n4. the host gets a shortlist, not the library")
    listed = RES.research({"name": "x", "needs": ["COUNT_ELEMENTS"]},
                          skills=[counting, selecting])
    check([one["skill"] for one in listed["shortlist"]] == ["count-elements"],
          "only the skill sharing a capability is shortlisted")
    check(listed["of"] == 2 and len(listed["shortlist"]) == 1,
          "2 skills were read and 1 was handed on - a skill sharing no "
          "capability is not a near-duplicate in any sense that matters")
    nothing = RES.research({"name": "x", "needs": ["BRAND_NEW"]},
                           skills=[counting, selecting])
    check(nothing["shortlist"] == [],
          "and an unrelated proposal shortlists nobody, not everybody")

    print("\n5. served in 2024 is not served")
    fragments = [{"id": "everywhere", "capability": "FILTER_ELEMENTS",
                  "revit": list(FRAG.REVIT_VERSIONS)},
                 {"id": "recent-only", "capability": "COUNT_ELEMENTS",
                  "revit": ["2024", "2025"]}]
    thin = RES.research(
        {"name": "x", "needs": ["FILTER_ELEMENTS", "COUNT_ELEMENTS",
                                "NOBODY_PROVIDES_THIS"],
         "revit": ["2023", "2024", "2025"]},
        fragments=fragments)
    got = thin["needs"]
    check([one["capability"] for one in got["served"]] == ["FILTER_ELEMENTS"],
          "the capability covering every declared release is served")
    check([one["capability"] for one in got["partly_served"]]
          == ["COUNT_ELEMENTS"],
          "the one missing 2023 is partly_served - counted APART from "
          "served, never inside it")
    check(got["partly_served"][0]["not_on"] == ["2023"],
          "and the answer names which release is missing")
    check(got["gaps"] == ["NOBODY_PROVIDES_THIS"],
          "and a capability nobody provides is a gap")
    served_names = [one["capability"] for one in got["served"]]
    check("COUNT_ELEMENTS" not in served_names,
          "a partly-served capability appears in NEITHER served nor gaps - "
          "that is how a skill ships broken on half the releases it claims")

    print("\n6. the standards question is named, never guessed")
    check(thin["standard"]["answered"] is False,
          "`answered` is false, always")
    check(thin["standard"]["by"] is None, "and nothing cited it")
    owners = [one["agent"] for one in thin["standard"]["owners"]]
    check(owners[0] == "HERON-STD-PRJ-009",
          "the project standard is first - the ONE precedence docs/28 "
          "states: it outranks the company default")
    check(owners[1] == "HERON-STD-CMP-002" and len(owners) == 4,
          "with the company default beneath it, four owners in all")
    check("cites, never invents" in thin["standard"]["why"],
          "and HERON-STD-ISO-003's rule is the reason given")

    print("\n7. the release list is HERON-FRG-VAL-001's, by identity")
    check(RES.VERSIONS is FRAG.REVIT_VERSIONS,
          "VERSIONS is FRAG.REVIT_VERSIONS - the same object, not a copy")
    written = [one for one in RES.VERSIONS if '"%s"' % one in logic]
    check(not written,
          "and the module's code writes none of them%s"
          % ("" if not written else ": %s" % ", ".join(written)))

    print("\n8. nothing is written, and every failure is reached")
    for writing in ("open(", "write(", "makedirs", "rmtree"):
        check(writing not in logic, "the agent never uses %s" % writing)
    check(len(thin["unjudged"]) == 5, "five things are left unjudged")
    for proposal, skills, name in (
            (None, [], "NOTHING_TO_RESEARCH"),
            ("a string", [], "NOT_A_PROPOSAL"),
            ({"utterances": ["x"]}, [], "NOT_A_PROPOSAL"),
            ({"name": "x", "revit": ["2028"]}, [], "NOT_A_VERSION"),
            ({"name": "x"}, ["not a card"], "NOT_A_SKILL"),
            ({"name": "x"}, [{"needs": []}], "NOT_A_SKILL")):
        answer = RES.research(proposal, skills=skills)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-SKL-RES-001.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 4, "the contract declares 4 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    what exists, what is missing, what nobody holds")
    return 0


if __name__ == "__main__":
    sys.exit(main())
