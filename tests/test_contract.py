# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-CON-017
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The agent contract - what it refuses, and what it calls breaking.

    python tests/test_contract.py

WHAT IT PROVES
  1. A WELL-FORMED CONTRACT PASSES. Asserted first, because a validator that
     refuses everything also refuses everything wrong, and would look like it
     worked.

  2. TIER AND RISK ARE REFUSED. docs/28-agent-registry.md owns them. This is
     the rule the whole block-0 design rests on, and it is worth a test of its
     own rather than a sentence in a docstring nobody runs.

  3. AN AGENT NOT IN THE REGISTER IS REFUSED. An agent is in the register
     before it is in the code.

  4. AN AGENT THAT CANNOT FAIL IS REFUSED. No declared failure state means a
     caller has nothing to plan around (Golden Rule 14).

  5. BLIND RETRY IS REFUSED, in both shapes - retrying with nothing named, and
     naming a failure the contract never declared (D-21).

  6. THE YAML `on` TRAP IS CAUGHT BY NAME. A bare `on:` is the boolean true in
     YAML 1.1, so the list under it is silently lost. The first contract this
     module ever read hit it. A rule learned by being bitten is a rule that
     gets a test.

  7. THE SIX BREAKING CHANGES ARE CALLED BREAKING, and each is checked on its
     own so a verdict cannot be right for the wrong reason.

  8. AN ADDITION THAT COSTS A CALLER NOTHING IS COMPATIBLE, and an unchanged
     contract is IDENTICAL. Without these two, "BREAKING" means nothing.

  9. A BREAKING CHANGE THAT DID NOT RAISE THE MAJOR VERSION IS REPORTED.

 10. THE VALIDATOR DOES NOT RAISE ON RUBBISH. It is handed prose instead of a
     mapping and must report, not crash - a validator that dies on the input
     it exists to judge reports nothing at all.

 11. EVERY CONTRACT IN THE REPOSITORY VALIDATES. The one test here that can be
     broken by somebody else's commit, which is what makes it worth running.
"""

import copy
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_contract as CON                                  # noqa: E402

FAILURES = []
KNOWN = {"HERON-AHR-CON-017", "HERON-KRN-EVT-004"}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def said(problems, fragment):
    """Is `fragment` in any of the problems reported."""
    return any(fragment in line for line in problems)


def good():
    """A contract with nothing wrong with it."""
    return {
        "agent": "HERON-KRN-EVT-004",
        "version": "1.0.0",
        "input": {"topic": {"type": "string", "required": True,
                            "description": "which channel to publish on"}},
        "output": {"delivered": {"type": "integer",
                                 "description": "how many subscribers ran"}},
        "allowed-tools": [],
        "timeout-seconds": 10,
        "failures": ["NO_SUBSCRIBER", "HANDLER_FAILED"],
        "retry": {"attempts": 1, "on-failures": ["HANDLER_FAILED"]},
    }


def main():
    print("1. A well-formed contract passes")
    check(CON.validate(good(), KNOWN) == [],
          "nothing is reported against a contract with nothing wrong")

    print()
    print("2. Tier and risk belong to the register, not to a contract")
    for field in ("tier", "risk"):
        c = good()
        c[field] = "T1" if field == "tier" else "READ"
        check(said(CON.validate(c, KNOWN), "docs/28-agent-registry.md owns"),
              "a contract declaring '%s' is refused" % field)

    print()
    print("3. An agent that is not in the register does not exist")
    c = good()
    c["agent"] = "HERON-MADE-UP-999"
    check(said(CON.validate(c, KNOWN), "not in docs/28-agent-registry.md"),
          "an unregistered agent id is refused")

    print()
    print("4. An agent that cannot fail is a claim nobody can plan around")
    c = good()
    c["failures"] = []
    check(said(CON.validate(c, KNOWN), "declares no failure state"),
          "an empty failure list is refused")

    print()
    print("5. Blind retry, in both of its shapes")
    c = good()
    c["retry"] = {"attempts": 3, "on-failures": []}
    check(said(CON.validate(c, KNOWN), "blind retry is refused"),
          "retrying while naming no failure state is refused")
    c = good()
    c["retry"] = {"attempts": 1, "on-failures": ["SOMETHING_UNDECLARED"]}
    check(said(CON.validate(c, KNOWN), "not in 'failures'"),
          "retrying a failure the contract never declared is refused")

    print()
    print("6. The YAML `on` trap, caught by name")
    c = good()
    c["retry"] = {"attempts": 1, True: ["HANDLER_FAILED"]}
    problems = CON.validate(c, KNOWN)
    check(said(problems, "'on-failures'"),
          "a retry block whose key YAML turned into the boolean true is named")

    print()
    print("7. The six changes a caller cannot survive")
    base = good()

    gone = copy.deepcopy(base)
    del gone["input"]["topic"]
    gone["version"] = "2.0.0"
    check(CON.compare(base, gone)[0] == "BREAKING", "a removed input is breaking")

    demanded = copy.deepcopy(base)
    demanded["input"]["scope"] = {"type": "string", "required": True,
                                  "description": "which scope"}
    demanded["version"] = "2.0.0"
    check(CON.compare(base, demanded)[0] == "BREAKING",
          "a new REQUIRED input is breaking")

    hardened = copy.deepcopy(base)
    hardened["input"]["topic"]["required"] = True
    hardened["input"]["topic"]["type"] = "list"
    hardened["version"] = "2.0.0"
    check(CON.compare(base, hardened)[0] == "BREAKING",
          "a changed field type is breaking")

    dropped = copy.deepcopy(base)
    del dropped["output"]["delivered"]
    dropped["version"] = "2.0.0"
    check(CON.compare(base, dropped)[0] == "BREAKING",
          "a removed output is breaking")

    forgot = copy.deepcopy(base)
    forgot["failures"] = ["NO_SUBSCRIBER"]
    forgot["retry"] = {"attempts": 0, "on-failures": []}
    forgot["version"] = "2.0.0"
    check(CON.compare(base, forgot)[0] == "BREAKING",
          "a removed failure state is breaking")

    invented = copy.deepcopy(base)
    invented["failures"] = base["failures"] + ["BUS_CLOSED"]
    invented["version"] = "2.0.0"
    verdict, reasons = CON.compare(base, invented)
    check(verdict == "BREAKING" and said(reasons, "no branch for it"),
          "a NEW failure state is breaking, and the reason says why")

    print()
    print("8. What a caller can survive, so that BREAKING means something")
    kind = copy.deepcopy(base)
    kind["input"]["scope"] = {"type": "string", "required": False,
                              "description": "optional scope"}
    kind["version"] = "1.1.0"
    check(CON.compare(base, kind)[0] == "COMPATIBLE",
          "a new OPTIONAL input is compatible")
    check(CON.compare(base, copy.deepcopy(base))[0] == "IDENTICAL",
          "an unchanged contract is identical")

    print()
    print("9. A breaking change must raise the major version")
    sneaky = copy.deepcopy(base)
    del sneaky["output"]["delivered"]
    sneaky["version"] = "1.0.1"
    verdict, reasons = CON.compare(base, sneaky)
    check(verdict == "BREAKING" and said(reasons, "raises the MAJOR number"),
          "a breaking change on a patch bump is reported as well as refused")

    print()
    print("10. Rubbish is reported, never raised")
    problems = CON.validate("a contract, but written as prose", KNOWN)
    check(said(problems, "is not a mapping"),
          "prose instead of a mapping is reported without an exception")

    print()
    print("11. Every contract in the repository validates")
    known = CON.registry_ids()
    found = CON.contracts()
    check(len(found) > 0, "there is at least one contract to check")
    for path, data in found:
        name = os.path.basename(path)
        check(CON.validate(data, known, name) == [], "%s validates" % name)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the contract refuses what it must and calls a break a break")
    return 0


if __name__ == "__main__":
    sys.exit(main())
