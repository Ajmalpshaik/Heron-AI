# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-MAIN-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Repository interaction - the claim that nothing posts is read, not made.

    python tests/test_github.py

WHAT IT PROVES
  1. THE TOOL SURFACE IS READ. Given a department where one contract
     DOES declare a tool, the answer names it instead of saying none -
     which is the difference between a check and a reassurance.

  2. AN UNREADABLE CONTRACT IS REPORTED, never counted as empty.
     "Nothing here posts" cannot be claimed about a contract nobody read.

  3. THE ORDER IS THE DEPENDENCY ORDER: a commit before a pull request,
     a version before a release - whatever order the caller's intent
     listed them in.

  4. EXACTLY THE TWO ROWS docs/28 GATES CARRY A GATE, and the words are
     the register's. HERON-GIT-REL-007 does not, although it asks for a
     confirmation of its own.

  5. EVERY AGENT IN THE ORDER HAS A CONTRACT, and every agent an intent
     names is in the order.

  6. AN INTENT NOBODY LISTED IS REFUSED, not guessed at.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_github as GIT                                     # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_github.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]
    register = io.open(os.path.join(ROOT, "docs",
                                    "28-agent-registry.md"),
                       encoding="utf-8").read()

    print("\n1. the tool surface is read, not asserted")
    real = GIT.plan("open a pull request")
    check(real["reaches_outside"] == [],
          "today nothing in the department declares a tool")
    check("None of the %d agents" % real["of"] in real["why"],
          "and the answer says none of the %d posts anything" % real["of"])
    where = tempfile.mkdtemp()
    try:
        # A DEPARTMENT WHERE ONE AGENT DOES DECLARE A TOOL. If the claim
        # were asserted rather than read, this would still say "none".
        for name in ("HERON-GIT-BRN-003", "HERON-GIT-CMT-004",
                     "HERON-GIT-PR-005"):
            tools = "[github_post]" if name == "HERON-GIT-PR-005" else "[]"
            io.open(os.path.join(where, "%s.yaml" % name), "w",
                    encoding="utf-8").write(
                "agent: %s\nversion: 1.0.0\nallowed-tools: %s\n"
                "failures: []\n" % (name, tools))
        loud = GIT.plan("open a pull request", where=where)
        check(loud["reaches_outside"] == ["HERON-GIT-PR-005"],
              "with a contract declaring a tool, the answer NAMES it")
        check("1 of 3 declare a tool" in loud["why"],
              "and the summary changes rather than staying reassuring")
        check("no longer holds as written" in loud["unjudged"][0],
              "the unjudged line says the claim no longer holds")
        step = [one for one in loud["steps"]
                if one["agent"] == "HERON-GIT-PR-005"][0]
        check(step["tools"] == ["github_post"],
              "and the step itself carries what it is allowed to do")

        print("\n2. an unreadable contract is reported")
        io.open(os.path.join(where, "HERON-GIT-BRN-003.yaml"), "w",
                encoding="utf-8").write("agent: [unclosed\n")
        broken = GIT.plan("open a pull request", where=where)
        check(broken["unreadable"] == ["HERON-GIT-BRN-003"],
              "the contract that will not parse is named")
        check("could not be read" in broken["unjudged"][0].lower()
              or "COULD NOT BE READ" in broken["unjudged"][0],
              "and counted against the claim, never as an empty tool list")
    finally:
        shutil.rmtree(where)

    print("\n3. the order is the dependency order")
    steps = [one["agent"] for one in real["steps"]]
    check(steps == ["HERON-GIT-BRN-003", "HERON-GIT-CMT-004",
                    "HERON-GIT-PR-005"],
          "a branch, then a commit, then the pull request")
    check(GIT.RANK["HERON-GIT-CMT-004"] < GIT.RANK["HERON-GIT-PR-005"],
          "a commit outranks a pull request - one before the other is a "
          "pull request with nothing in it")
    check(GIT.RANK["HERON-GIT-VER-008"] < GIT.RANK["HERON-GIT-REL-007"],
          "and a version outranks a release - a tag nobody can order")
    # THE CALLER'S ORDER DOES NOT DECIDE.
    check([one["agent"] for one in
           GIT.plan("cut a release")["steps"]]
          == ["HERON-GIT-VER-008", "HERON-GIT-REL-007"],
          "the steps come back in the register's order whatever order "
          "the intent listed them in")
    check(len(GIT.ORDER) == len(GIT.RANK) == 8,
          "the order holds all 8 steps, each exactly once")

    print("\n4. exactly the two gated rows carry a gate")
    check(sorted(GIT.GATED) == ["HERON-GIT-COM-010", "HERON-GIT-PR-005"],
          "two rows are gated, and only two")
    for agent, words in GIT.GATED.items():
        check(words in register.lower().replace("**", ""),
              "docs/28 carries %r for %s" % (words, agent))
    check(real["gated"] == ["HERON-GIT-PR-005"],
          "the pull request plan reports its gate")
    check(GIT.plan("cut a release")["gated"] == [],
          "and the release plan reports NONE - REL-007 asks for a "
          "confirmation of its own, which is not a register requirement")
    check("not a requirement docs/28 stated"
          in GIT.plan("cut a release")["unjudged"][1],
          "the answer says so rather than blurring the two")

    print("\n5. the order and the contracts agree")
    for agent, _ in GIT.ORDER:
        check(os.path.exists(os.path.join(ROOT, "brain", "agents",
                                          "%s.yaml" % agent)),
              "%s has a contract on disk" % agent)
    named = set()
    for one in GIT.INTENTS.values():
        named |= set(one)
    check(not named - set(GIT.RANK),
          "and every agent an intent names is in the order%s"
          % ("" if not named - set(GIT.RANK)
             else ": %s" % ", ".join(sorted(named - set(GIT.RANK)))))

    print("\n6. an intent nobody listed is refused")
    odd = GIT.plan("do something clever with the repository")
    reached.add(odd.get("refused"))
    check(odd["refused"] == "NOT_AN_INTENT",
          "it is refused rather than matched to whatever sounds close")
    check("reading the words rather than the register" in odd["why"],
          "with the reason: guessing which agents sound relevant is "
          "reading the words")
    check(all(one in odd["why"] for one in sorted(GIT.INTENTS)),
          "and every intent it DOES know is listed back")

    print("\n7. every declared failure is named and reached")
    for intent, kwargs, name in (
            (None, {}, "NOTHING_TO_PLAN"),
            ("", {}, "NOTHING_TO_PLAN"),
            ("file an issue", {"where": "/nowhere"}, "NO_CONTRACTS")):
        answer = GIT.plan(intent, **kwargs)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-GIT-MAIN-001.yaml"))
    declared = contract.get("failures") or []
    check(len(declared) == 3, "the contract declares 3 failures")
    for failure in declared:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(declared) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(real["unjudged"]) == 4, "four things are left unjudged")
    check(contract.get("allowed-tools") == [],
          "and this agent's own contract declares no tool either")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the claim that nothing posts is read, not made")
    return 0


if __name__ == "__main__":
    sys.exit(main())
