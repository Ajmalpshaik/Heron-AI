# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-LRN-EXT-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Knowledge extraction - a constant and an unvaried variable look the same.

    python tests/test_extract.py

WHAT IT PROVES
  1. A VALUE THAT VARIED IS A PARAMETER, and that part is certain.

  2. A VALUE THAT NEVER VARIED IS A QUESTION, never baked in silently -
     the candidate's `fixed` map is empty until somebody asks.

  3. FIXING A VALUE THAT VARIED IS REFUSED - the failure the row names.

  4. A ONE-OFF IS REFUSED, citing the agent whose verdict it is.

  5. TWO OBSERVATIONS IS THE MINIMUM, because one cannot show anything
     varying.

  6. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_extract as EXT                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_extract.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(pattern, observations, **kw):
        answer = EXT.extract(pattern, observations, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    PATTERN = {"pattern": "p", "verdict": "corroborated"}
    SEEN = [{"size": "300x300", "system": "Supply"},
            {"size": "250x250", "system": "Supply"},
            {"size": "400x200", "system": "Supply"}]

    print("1. A value that varied is a parameter")
    row = [line for line in io.open(
        os.path.join(ROOT, "docs", "28-agent-registry.md"),
        encoding="utf-8").read().splitlines() if "LRN-EXT-003" in line][0]
    check("parameterised, not hard-coded to the numbers it happened to see"
          in row,
          "docs/28: parameterised, not hard-coded to the numbers it "
          "happened to see")
    answer = ask(PATTERN, SEEN)
    check([one["name"] for one in answer["parameters"]] == ["size"],
          "'size' took three values and is a parameter")
    check(answer["parameters"][0]["seen"]
          == ["250x250", "300x300", "400x200"],
          "with every value it was seen as: %s"
          % answer["parameters"][0]["seen"])
    check(answer["candidate"]["parameters"] == ["size"],
          "and the candidate carries it as a parameter")
    check(any("that part is certain" in line.lower()
              for line in answer["unjudged"]),
          "the answer says that part is certain")

    print("\n2. A value that never varied is a question")
    check([one["name"] for one in answer["questions"]] == ["system"],
          "'system' was always Supply and is a question, not a constant")
    check(answer["candidate"]["fixed"] == {},
          "the candidate fixes NOTHING until somebody asks: %s"
          % answer["candidate"]["fixed"])
    check("Supply" not in str(answer["candidate"]),
          "and the value it happened to see is not in the candidate at all")
    check(any("EITHER a constant OR a parameter nobody has varied yet"
              in line for line in answer["unjudged"]),
          "the answer says it is either a constant or an unvaried "
          "parameter")
    asked = ask(PATTERN, SEEN, fix=["system"])
    check(asked["candidate"]["fixed"] == {"system": "Supply"},
          "and fixing it on request works: %s" % asked["candidate"]["fixed"])
    check(not asked["questions"], "leaving no questions open")

    print("\n3. Fixing a value that varied is refused")
    wrong = ask(PATTERN, SEEN, fix=["size"])
    check(wrong.get("refused") == "WOULD_HARD_CODE",
          "'size' varied, so fixing it is refused")
    check(wrong["fields"] == ["size"], "and the field is named")
    check("wrong in the way that looks like success" in wrong["why"],
          "with the reason: wrong in the way that looks like success")
    both = ask(PATTERN, SEEN, fix=["size", "system"])
    check(both.get("refused") == "WOULD_HARD_CODE",
          "one bad field spoils the request - nothing is half-applied")
    check(ask(PATTERN, SEEN, fix=["nowhere"]).get("refused")
          == "NOTHING_TO_EXTRACT",
          "and fixing a field nobody observed is refused too")

    print("\n4. A one-off is refused")
    once = ask({"pattern": "p", "verdict": "a_one_off"}, SEEN)
    check(once.get("refused") == "A_ONE_OFF_IS_NOT_A_PATTERN",
          "ANA-002's verdict is honoured")
    check("HERON-LRN-ANA-002" in once["why"],
          "citing the agent whose verdict it is")
    check("two places" in once["why"],
          "and why re-deciding it here would be wrong")
    check(EXT.A_ONE_OFF == "a_one_off",
          "the verdict name matches what ANA-002 returns")
    import heron_analysis as ANA
    check(EXT.A_ONE_OFF in ANA.VERDICTS,
          "and really is one of ANA-002's three")

    print("\n5. Two observations is the minimum")
    check(ask(PATTERN, SEEN[:1]).get("refused") == "NOTHING_TO_EXTRACT",
          "one observation is refused")
    check("two is the least that can show a value varying"
          in ask(PATTERN, SEEN[:1])["why"].lower(),
          "because two is the least that can show a value varying")
    check(ask(PATTERN, SEEN[:2])["extracted"] is False
          and not ask(PATTERN, SEEN[:2]).get("refused"),
          "and two is enough")
    check(ask(PATTERN, [{"a": 1}, {"a": 1}])["questions"][0]["name"] == "a",
          "two identical observations give a question, not a parameter")

    print("\n6. Every failure is named and reached")
    for bad, why in ((None, "no pattern"), ("", "an empty name"),
                     ({}, "and a map with no pattern")):
        check(ask(bad, SEEN).get("refused") == "NOT_A_PATTERN", why)
    for kind in ("policy", "", "standard"):
        check(ask(PATTERN, SEEN, as_kind=kind).get("refused") == "NOT_A_KIND",
              "'%s' is not a kind" % kind)
    check(ask(PATTERN, ["not a map", {"a": 1}]).get("refused")
          == "NOT_AN_OBSERVATION", "an observation that is not a map")
    check(ask(PATTERN, [{}, {}]).get("refused") == "NOTHING_TO_EXTRACT",
          "and observations carrying no values at all")
    for writing in ("open(", "write(", "os.remove", "subprocess"):
        check(writing not in logic, "the agent never uses %s" % writing)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-LRN-EXT-003.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 6, "the contract declares 6 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(answer["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    what varied is a parameter, what did not is a question")
    return 0


if __name__ == "__main__":
    sys.exit(main())
