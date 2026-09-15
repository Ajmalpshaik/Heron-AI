# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-WOP-016
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Workflow optimiser - three facts about recorded runs, and no fourth.

    python tests/test_stages.py

WHAT IT PROVES
  1. EXACTLY THREE OBSERVATIONS, and they are the three docs/28 names -
     read out of the register row rather than typed here.

  2. "ALWAYS" MEANS ALWAYS. One run that does not fit ends the
     observation, which is what makes a universal usable where a
     percentage would have needed an invented threshold.

  3. CONSECUTIVE IS CHECKED BOTH WAYS. "A is always followed by B" still
     allows B to turn up without A, and two stages that come apart in
     either direction are not inseparable.

  4. UNOBSERVED IS NOT NEVER - the window is in the answer, and the
     answer says what "never" is worth without it.

  5. IT SUGGESTS AND NEVER CHANGES.

  6. THE AGENTS ARE NOT THIS AGENT'S - an agent id is refused, and the
     refusal names who owns it.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_stages as WOP                                     # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def run(when, *stages):
    return {"workflow": "w", "when": when,
            "stages": [{"stage": name, "outcome": outcome,
                        "changed": changed}
                       for name, outcome, changed in stages]}


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_stages.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(runs, **kw):
        answer = WOP.suggest(runs, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    def kinds(answer, observation):
        return [row for row in answer["suggestions"]
                if row["observation"] == observation]

    print("1. Exactly three observations, and they are docs/28's three")
    row = [line for line in io.open(
        os.path.join(ROOT, "docs", "28-agent-registry.md"),
        encoding="utf-8").read().splitlines() if "KRN-WOP-016" in line][0]
    said = row.lower()
    check("always fails at the same stage" in said,
          "the register says: always fails at the same stage")
    check("two stages could merge" in said, "that two stages could merge")
    check("never changes the outcome" in said,
          "and that a step never changes the outcome")
    check(len(WOP.OBSERVATIONS) == 3,
          "the agent makes three (%d), one for each" % len(WOP.OBSERVATIONS))
    check("not the agents doing it" in said,
          "and the register draws the line: not the agents doing it")

    print("\n2. ALWAYS means always")
    always = ask([run("1", ("a", "ok", ["x"]), ("b", "failed", [])),
                  run("2", ("a", "ok", ["y"]), ("b", "failed", [])),
                  run("3", ("a", "ok", ["z"]), ("b", "failed", []))])
    fails = kinds(always, "ALWAYS_FAILS_AT")
    check(len(fails) == 1 and fails[0]["stage"] == "b",
          "'b' failed in all three runs, so it is reported")
    check(fails[0]["of"] == 3 and fails[0]["runs"] == 3,
          "with the counts that make it readable: %d of %d"
          % (fails[0]["of"], fails[0]["runs"]))
    # ONE RUN THAT DOES NOT FIT ENDS IT.
    nearly = ask([run("1", ("a", "ok", ["x"]), ("b", "failed", [])),
                  run("2", ("a", "ok", ["y"]), ("b", "failed", [])),
                  run("3", ("a", "ok", ["z"]), ("b", "ok", ["done"]))])
    check(not kinds(nearly, "ALWAYS_FAILS_AT"),
          "two failures out of three is NOT reported - a universal is not "
          "a percentage in disguise")
    check(not kinds(nearly, "NEVER_CHANGED"),
          "and 'b' changed something once, so it is not NEVER_CHANGED "
          "either")
    never = kinds(always, "NEVER_CHANGED")
    check([one["stage"] for one in never] == ["b"],
          "while 'b' changed nothing in every run and 'a' changed "
          "something, so only 'b' is reported")
    once = ask([run("1", ("a", "failed", []))])
    check(kinds(once, "ALWAYS_FAILS_AT")[0]["runs"] == 1,
          "one run says 'always, in 1 of 1' - true, and weak, and the "
          "count is what says so")

    print("\n3. Consecutive is checked both ways")
    chained = ask([run("1", ("a", "ok", ["x"]), ("b", "ok", ["y"])),
                   run("2", ("a", "ok", ["x"]), ("b", "ok", ["y"]))])
    pairs = kinds(chained, "ALWAYS_CONSECUTIVE")
    check(len(pairs) == 1 and pairs[0]["stages"] == ["a", "b"],
          "a always followed by b, and b never without a")
    # B WITHOUT A - one direction holds and the other does not.
    loose = ask([run("1", ("a", "ok", ["x"]), ("b", "ok", ["y"])),
                 run("2", ("a", "ok", ["x"]), ("b", "ok", ["y"])),
                 run("3", ("c", "ok", ["z"]), ("b", "ok", ["y"]))])
    check(not [one for one in kinds(loose, "ALWAYS_CONSECUTIVE")
               if one["stages"] == ["a", "b"]],
          "but once 'b' turns up after 'c', the pair is no longer "
          "inseparable and is not reported")
    check(pairs[0]["one_order"] is True,
          "and a workflow that never varied says so on the pair")
    varied = ask([run("1", ("a", "ok", ["x"]), ("b", "ok", ["y"])),
                  run("2", ("a", "ok", ["x"]), ("b", "ok", ["y"]),
                      ("d", "ok", ["w"]))])
    same = [one for one in kinds(varied, "ALWAYS_CONSECUTIVE")
            if one["stages"] == ["a", "b"]]
    check(same and same[0]["one_order"] is False,
          "while a workflow that ran two different ways does not")

    print("\n4. Unobserved is not never")
    check(always["window"] == {"first": "1", "last": "3"},
          "the window is in the answer: %s" % always["window"])
    check(any("UNOBSERVED IS NOT NEVER" in line
              for line in always["unjudged"]),
          "and the answer says unobserved is not never")
    check(any("CLN-009" in line for line in always["unjudged"]),
          "citing the agent that learned the same about unused artefacts")
    check("may be the one that matters on the day it matters"
          in never[0]["why"],
          "and every NEVER_CHANGED carries it too")
    undated = ask([{"workflow": "w", "stages": [
        {"stage": "a", "outcome": "failed", "changed": []}]}])
    check(undated["window"] == {"first": None, "last": None},
          "with no dates the window is empty, not invented")
    check(any("unknown span" in line for line in undated["unjudged"]),
          "and the answer says 'never' then covers an unknown span")

    print("\n5. It suggests and never changes")
    check(always["changed"] is False, "`changed` is false")
    for acting in ("merge(", "remove(", "rewrite(", "apply(", "open(",
                   "os.remove", "write("):
        check(acting not in logic, "the code never uses %s" % acting)
    for verdict in ("fixed", "applied", "improved", "score", "severity"):
        check(verdict not in always,
              "no '%s' key in the answer" % verdict)
    check(any("not a suggestion" in line for line in always["unjudged"]),
          "and the answer says a suggestion acted on automatically is not "
          "one")

    print("\n6. The agents are not this agent's")
    for named in ("HERON-AHR-OPT-009", "heron-krn-wop-016", "HERON-ANY-X-1"):
        refused = ask([run("1", ("a", "ok", []))], about=named)
        check(refused.get("refused") == "NOT_ABOUT_A_WORKFLOW",
              "'%s' is refused" % named)
    check("HERON-AHR-OPT-009" in
          ask([run("1", ("a", "ok", []))],
              about="HERON-X")["why"],
          "and the refusal names who owns the agents")
    fine = ask([run("1", ("a", "ok", []))], about="tag-and-sheet")
    check(not fine.get("refused"),
          "while a workflow name is fine")
    check("heron_optimizer" not in logic and "OPT-009" in logic,
          "the agent cites the boundary without importing across it")

    print("\n7. Every failure is named and reached")
    check(ask(None).get("refused") == "NOTHING_TO_LEARN_FROM",
          "nothing handed in is refused")
    check(ask([]).get("refused") == "NOTHING_TO_LEARN_FROM",
          "and so is an empty list - 'no problems found' would be about "
          "the records, not the work")
    for bad, why in (("not a map", "a run that is not a map"),
                     ({"stages": [{"stage": "a"}]}, "one with no workflow"),
                     ({"workflow": "w"}, "one with no stages"),
                     ({"workflow": "w", "stages": [{"outcome": "ok"}]},
                      "and a stage with no name")):
        check(ask([bad]).get("refused") == "NOT_A_RUN", why)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-KRN-WOP-016.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 3, "the contract declares 3 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    spare = sorted(reached - set(named))
    check(not spare, "and nothing else was refused%s"
          % ("" if not spare else ": %s" % ", ".join(spare)))
    for observation in WOP.OBSERVATIONS:
        check(observation in logic, "the code names %s" % observation)
    check(len(always["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    three facts, a universal rather than a threshold")
    return 0


if __name__ == "__main__":
    sys.exit(main())
