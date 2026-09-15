# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-BRN-003
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Branching - the prohibition is real, and the cost is counted.

    python tests/test_branch.py

WHAT IT PROVES
  1. GOLDEN RULE 4's ORDER IS docs/14's, read out of the document -
     "keep, extend, adapt, or version-branch - in that order of
     preference", with version-branch LAST.

  2. docs/16 s64 SAYS WHAT THE AGENT SAYS IT SAYS, including "it is a
     trap" and "cherry-picked into seven others, forever".

  3. THE COST IS COUNTED, NOT QUOTED. It equals every supported release
     but this one - which is seven today, agreeing with docs/16's prose,
     and which stays right when the list changes.

  4. AN ORDINARY BRANCH GETS NO NAMING RULE, and the answer says none is
     written down rather than inventing one.

  5. EACH OF keep, extend AND adapt IS ASKED ABOUT SEPARATELY, and the
     answer names which are missing.

  6. NOTHING IS CREATED - the import list holds nothing that could.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_branch as BRN                                     # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def tried(**changes):
    card = {"keep": "the existing code throws on 2020",
            "extend": "the new case needs a different return type",
            "adapt": "the adapter would have to expose two types"}
    card.update(changes)
    return card


def version(**changes):
    card = {"name": "revit-2020-support", "for_release": "2020",
            "because": "a 32-bit ElementId the adapter cannot hide",
            "tried": tried()}
    card.update(changes)
    return card


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_branch.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]
    rules = io.open(os.path.join(ROOT, "docs", "14-golden-rules.md"),
                    encoding="utf-8").read()
    strategy = io.open(os.path.join(ROOT, "docs",
                                    "16-version-support-strategy.md"),
                       encoding="utf-8").read()

    print("\n1. Golden Rule 4's order is docs/14's")
    check("Keep, extend, adapt, or version-branch — in that order of "
          "preference" in rules,
          "docs/14 carries the sentence verbatim")
    check(BRN.LADDER == ("keep", "extend", "adapt", "version-branch"),
          "and the module's ladder is that order")
    check(BRN.LAST == "version-branch" and BRN.LADDER[-1] == BRN.LAST,
          "with version-branch LAST - which is what makes the other "
          "three worth asking about")
    check("Never break a working Revit version unnecessarily" in rules,
          "under the rule it belongs to")

    print("\n2. docs/16 s64 says what the agent says it says")
    for said in ("Branch-per-version is the intuitive answer and it is a "
                 "trap",
                 "cherry-picked into seven others, forever",
                 "the branches diverge"):
        check(said in strategy, "docs/16 carries %r" % said)
    check("Why not separate branches per version" in strategy,
          "in a section with that title")

    print("\n3. the cost is counted, not quoted")
    good = BRN.propose(version())
    check(good["allowed"] and good["kind"] == "version",
          "a version branch that shows its work is allowed")
    check(good["cost"]["cherry_picks_per_fix"] == len(FRAG.REVIT_VERSIONS) - 1,
          "the cost is every supported release but this one")
    check(good["cost"]["cherry_picks_per_fix"] == 7,
          "which is SEVEN today - the same number docs/16's prose uses, "
          "arrived at by counting")
    check("2020" not in good["cost"]["into"]
          and len(good["cost"]["into"]) == 7,
          "and the branch's own release is not one of them")
    later = BRN.propose(version(name="revit-2027", for_release="2027"))
    check("2027" not in later["cost"]["into"],
          "the same holds for a different release - it is computed, not "
          "a constant")
    check(BRN.VERSIONS is FRAG.REVIT_VERSIONS,
          "and the release list is HERON-FRG-VAL-001's object, so the "
          "count stays right when the list changes")

    print("\n4. an ordinary branch gets no naming rule")
    plain = BRN.propose({"name": "claude/two-agents"})
    check(plain["allowed"] and plain["kind"] == "ordinary",
          "it is allowed")
    check(plain["cost"] is None, "with no cost, because there is none")
    check("no document states a shape for a branch name" in plain["why"],
          "and the answer says no rule applies rather than applying one")
    check(any("HERON-NAM-GEN-001" in line for line in plain["unjudged"]),
          "naming the other agent left unbuilt for the same reason")
    # A NAME NOBODY WOULD CHOOSE IS STILL ALLOWED, because there is no
    # rule to break.
    check(BRN.propose({"name": "!!!"})["allowed"] is True,
          "even '!!!' is allowed - inventing a shape here would make it "
          "the convention the moment it were enforced")

    print("\n5. each earlier option is asked about separately")
    for step in ("keep", "extend", "adapt"):
        answer = BRN.propose(version(tried=tried(**{step: ""})))
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "EARLIER_OPTION_UNTRIED"
              and answer["untried"] == [step],
              "'%s' alone missing is refused, and named" % step)
    two = BRN.propose(version(tried={"keep": "throws"}))
    check(two["untried"] == ["extend", "adapt"],
          "two missing come back as two, in Golden Rule 4's order")
    check(two.get("asked"), "with a question: %r" % two["asked"])
    check(BRN.propose(version(tried=tried(keep="it would work fine")))
          ["allowed"] is True,
          "and an ANSWER is enough - nothing here judges whether it is a "
          "good one")

    print("\n6. nothing is created")
    imports = sorted(line.split()[1] for line in logic.split("\n")
                     if line.startswith("import "))
    check(imports == ["heron_fragment", "os", "sys"],
          "the whole import list is os, sys and the release list: %s"
          % ", ".join(imports))
    for reaching in ("subprocess", "socket", "urllib", "open(", "write("):
        check(reaching not in logic, "nothing here uses %s" % reaching)
    check(any("NOTHING WAS CREATED" in line for line in good["unjudged"]),
          "and the answer says so")

    print("\n7. every declared failure is named and reached")
    for branch, name in (
            (None, "NOTHING_TO_PROPOSE"),
            ("a string", "NOT_A_BRANCH"),
            ({"name": "  "}, "NOT_A_BRANCH"),
            (version(tried="yes"), "NOT_A_BRANCH"),
            (version(for_release="2028"), "NOT_A_VERSION"),
            (version(because=""), "NOT_SHOWN_NECESSARY")):
        answer = BRN.propose(branch)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-GIT-BRN-003.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 5, "the contract declares 5 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(good["unjudged"]) == 4 and len(plain["unjudged"]) == 4,
          "four things are left unjudged, on both paths")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the only strategy written down is a prohibition")
    return 0


if __name__ == "__main__":
    sys.exit(main())
