# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-CHG-009
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Change detection - the second step is the one nobody does by hand.

    python tests/test_upstream.py

WHAT IT PROVES
  1. A SKILL NOBODY EDITED IS REACHED through the capability a touched
     fragment provides. That is the step the diff does not show.

  2. A SKILL WHOSE OWN CARD CHANGED IS NOT ALSO "REACHED". The two lists
     answer different questions and a skill in both would make the
     second count meaningless.

  3. `only_provider` IS MEASURED, NOT ASSUMED. A capability with two
     providers is not flagged; with one, it is.

  4. A PATH IT CANNOT ATTRIBUTE IS REPORTED, NEVER DROPPED - both
     shapes: an unknown fragment folder, and a path outside both trees.

  5. A PATH LEAVING THE REPOSITORY IS REFUSED, and a Windows-shaped one
     is normalised rather than rejected.

  6. IT READS THE LIBRARY AND RUNS NOTHING - the whole import list is
     three standard modules and the two libraries it reads.

  7. AGAINST THE REAL LIBRARY, two fragments reach most of it.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_upstream as UPS                                   # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

FRAGMENTS = [
    {"slug": "count-them", "id": "FRG-T-001", "capability": "COUNT"},
    {"slug": "filter-them", "id": "FRG-T-002", "capability": "FILTER"},
    # TWO PROVIDERS FOR ONE CAPABILITY. The library has none of these
    # today, which is exactly why the check has to be measured.
    {"slug": "filter-them-fast", "id": "FRG-T-003", "capability": "FILTER"},
]
SKILLS = [
    {"id": "tally", "needs": ["COUNT", "FILTER"]},
    {"id": "picker", "needs": ["FILTER"]},
    {"id": "elsewhere", "needs": ["SOMETHING_ELSE"]},
]


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def look(paths):
    return UPS.look(paths, fragments=FRAGMENTS, skills=SKILLS)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_upstream.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. a skill nobody edited is reached")
    one = look(["brain/fragments/count-them/impl.cs"])
    check([each["slug"] for each in one["fragments"]] == ["count-them"],
          "the touched fragment is found by its FOLDER, which is what a "
          "path carries")
    check(one["capabilities"] == ["COUNT"], "carrying COUNT")
    check([each["skill"] for each in one["skills_reached"]] == ["tally"],
          "and `tally` is reached - nobody edited it, it routes through "
          "COUNT")
    check(one["skills_reached"][0]["through"] == ["COUNT"],
          "the answer names WHICH capability carried the change")
    check("elsewhere" not in
          [each["skill"] for each in one["skills_reached"]],
          "a skill needing nothing that moved is not reached")

    print("\n2. an edited card is not also 'reached'")
    both = look(["brain/fragments/count-them/impl.cs",
                 "brain/skills/tally.yaml"])
    check(both["skills_changed"] == ["tally"], "`tally` is reported edited")
    check([each["skill"] for each in both["skills_reached"]] == [],
          "and NOT also reached - one skill in both lists would make the "
          "second count meaningless")

    print("\n3. only_provider is measured, not assumed")
    filtered = look(["brain/fragments/filter-them/impl.cs"])
    picked = [each for each in filtered["skills_reached"]
              if each["skill"] == "picker"][0]
    check(picked["through"] == ["FILTER"], "`picker` is reached through "
                                           "FILTER")
    check(picked["only_provider"] == [],
          "and FILTER is NOT flagged as a single point - a second "
          "fragment provides it")
    check("something else provides each of them" in picked["why"],
          "with the answer saying so in words")
    counted = look(["brain/fragments/count-them/impl.cs"])
    check(counted["skills_reached"][0]["only_provider"] == ["COUNT"],
          "COUNT, with one provider, IS flagged")

    print("\n4. an unattributable path is reported, never dropped")
    odd = look(["brain/fragments/no-such-folder/impl.cs",
                "docs/09-skills-and-fragments.md",
                "brain/skills/no-such-skill.yaml"])
    check(len(odd["unattributed"]) == 3, "all three are listed")
    where = dict((each["path"], each["why"]) for each in odd["unattributed"])
    check("not a fragment in the library"
          in where["brain/fragments/no-such-folder/impl.cs"],
          "an unknown folder under brain/fragments says so")
    check("not a skill in the library"
          in where["brain/skills/no-such-skill.yaml"],
          "an unknown card under brain/skills says so")
    check("nothing here knows what depends on it"
          in where["docs/09-skills-and-fragments.md"],
          "and a path outside both trees says THAT, rather than nothing")
    check(odd["skills_reached"] == [] and any(
        "GR 14" in line for line in odd["unjudged"]),
          "nothing is downstream of any of them - and the answer says "
          "why that sentence is not the same as 'no impact'")

    print("\n5. a path leaving the repository is refused")
    for path in ("/etc/passwd", "../elsewhere/x", "brain/../../x"):
        answer = look([path])
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "OUTSIDE_THE_REPOSITORY",
              "'%s' is refused" % path)
    # NORMALISED, NOT REJECTED. git prints forward slashes; a person may
    # not.
    windows = look(["brain\\fragments\\count-them\\impl.cs"])
    check(windows["looked"] and
          [each["slug"] for each in windows["fragments"]] == ["count-them"],
          "a Windows-shaped path is normalised and still attributed")
    check(look(["brain/fragments/./count-them/impl.cs"])["fragments"],
          "and so is one carrying a './'")

    print("\n6. it reads the library and runs nothing")
    imports = sorted(line.split()[1] for line in logic.split("\n")
                     if line.startswith("import "))
    check(imports == ["heron_fragment", "heron_skill", "os", "posixpath",
                      "sys"],
          "the whole import list is os, posixpath, sys and the two "
          "libraries: %s" % ", ".join(imports))
    for writing in ("open(", "write(", "makedirs"):
        check(writing not in logic, "the agent never uses %s" % writing)

    print("\n7. against the real library")
    real = UPS.look(["brain/fragments/count-elements/fragment.yaml",
                     "brain/fragments/filter-elements-by-category/impl.cs"])
    check(real["looked"] and len(real["fragments"]) == 2,
          "two real fragments are found")
    check(len(real["skills_reached"]) >= 2,
          "and %d skill(s) are reached that nobody edited"
          % len(real["skills_reached"]))
    single = [each for each in real["skills_reached"]
              if each["only_provider"]]
    check(len(single) == len(real["skills_reached"]),
          "every one of them through a capability with a SINGLE provider "
          "- 360 fragments, 360 capabilities, not one with a second")
    check(real["unattributed"] == [],
          "and both paths were attributed")

    print("\n8. every declared failure is named and reached")
    for paths, name in (([], "NOTHING_TO_CHECK"),
                        ([None], "NOT_A_PATH"),
                        (["   "], "NOT_A_PATH"),
                        ([42], "NOT_A_PATH")):
        answer = look(paths)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-GIT-CHG-009.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 3, "the contract declares 3 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(one["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    what a change touched, and what it reaches")
    return 0


if __name__ == "__main__":
    sys.exit(main())
