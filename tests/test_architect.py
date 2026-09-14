# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-ARC-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Architect - it may narrow a job and it may never widen one.

    python tests/test_architect.py

WHAT IT PROVES
  1. IT DESIGNS ONLY FROM HR'S OUTPUT. A dict that is not a job description
     is refused, and the fields it checks are the ones the later checks
     compare against - not decoration.

  2. A TOOL THE JOB NEVER ASKED FOR IS REFUSED. This is the rule the file is
     built around: scope creep is invisible the moment the job description
     and the contract stop being read side by side.

  3. NARROWING IS ALLOWED AND REPORTED. A design needing less than the job
     asked for is worth looking at, not an error.

  4. IT CANNOT DESIGN FOR AN ID THE REGISTER DOES NOT CARRY, and the id is
     checked against the real 250-row register rather than trusted. HR
     leaves it None on purpose, which is what puts a person between the job
     description and the contract.

  5. IT VALIDATES ITS OWN OUTPUT, and CONTRACT_INVALID is reachable - a
     declared failure state no input can produce is a dead state, and this
     suite reaches every one the contract declares.

  6. THE CONTRACT IT PRODUCES CARRIES NO TIER, NO RISK AND NO STATUS,
     because a field settable in two places disagrees with itself.

  7. IT ALWAYS REPORTS WHAT IT COULD NOT DECIDE. Three questions come back
     every time, including on the confident path - a silent answer from a
     judgement nobody made is the failure this repository keeps recording.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_architect as ARC                                 # noqa: E402
import heron_contract as CON                                  # noqa: E402

FAILURES = []

JOB = {"agent-id": None, "name": "Duct Counter",
       "responsibility": "counts ducts in a view",
       "capabilities": ["COUNT_DUCTS"], "dependencies": [],
       "tools": ["heron_lookup", "heron_resolve"], "department": None,
       "risk-floor": "READ"}

INPUTS = {"view": {"type": "string", "required": True,
                   "description": "The view to count in."}}
OUTPUTS = {"count": {"type": "number", "description": "How many."}}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    known = CON.registry_ids()
    real = "HERON-AHR-ARC-003"
    reached = set()

    def ask(**kw):
        kw.setdefault("job", JOB)
        kw.setdefault("agent_id", real)
        kw.setdefault("failures", ["NOTHING_FOUND"])
        kw.setdefault("inputs", INPUTS)
        kw.setdefault("outputs", OUTPUTS)
        kw.setdefault("known_ids", known)
        answer = ARC.design(**kw)
        if "refused" in answer:
            reached.add(answer["refused"])
        return answer

    print("1. It designs only from HR's output")
    check(ask(job=None).get("refused") == "NO_JOB_DESCRIPTION",
          "no job description is refused")
    check(ask(job="counts ducts").get("refused") == "NO_JOB_DESCRIPTION",
          "a string is not a job description either")
    check(ask(job={"name": "Duct Counter"}).get("refused")
          == "NOT_A_JOB_DESCRIPTION",
          "a dict missing HR's fields is refused, and they are named")
    check(ask(job=dict(JOB, responsibility="  ")).get("refused")
          == "NOT_A_JOB_DESCRIPTION",
          "a job with an empty responsibility is refused - every contract "
          "field answers it")

    print()
    print("2. A tool the job never asked for is refused")
    answer = ask(tools=["heron_lookup", "revit_apply_move"])
    check(answer.get("refused") == "CONTRACT_EXCEEDS_THE_JOB",
          "the added tool is refused")
    check("revit_apply_move" in answer["why"]
          and "heron_lookup" not in answer["why"],
          "and the refusal names what was ADDED, not the whole list")

    print()
    print("3. Narrowing is allowed, and reported")
    answer = ask(tools=["heron_lookup"])
    check("contract" in answer, "a design using fewer tools is written")
    check(answer["dropped"] == ["heron_resolve"],
          "and what it dropped is named")
    check(any("fewer tool" in note for note in answer["unjudged"]),
          "and reported as something to look at, not buried")
    answer = ask(tools=[])
    check("contract" in answer and answer["contract"]["allowed-tools"] == [],
          "a design using no tool at all is still a design")

    print()
    print("4. It cannot design for an id the register does not carry")
    check(ask(agent_id=None).get("refused") == "NO_AGENT_ID",
          "HR's None is refused - the id is a row somebody adds to docs/28")
    check(ask(agent_id="  ").get("refused") == "NO_AGENT_ID",
          "and so is whitespace")
    check(ask(agent_id="HERON-AHR-NOPE-999").get("refused")
          == "NOT_IN_THE_REGISTER",
          "an id nobody planned is refused against the real register")
    check(real in known and len(known) > 200,
          "and the register really is the 250-row one, not a fixture")

    print()
    print("5. It validates its own output, and the state is reachable")
    answer = ask(version="not-a-version")
    check(answer.get("refused") == "CONTRACT_INVALID",
          "a design that does not validate is refused, not returned")
    check("the designed contract" in answer["why"],
          "and the problem is quoted from the validator itself")
    answer = ask(failures=[])
    check(answer.get("refused") == "NO_FAILURE_DECLARED",
          "a contract with no failure state leaves its caller nothing")
    answer = ask(failures=["NOTHING_FOUND", "NOTHING_FOUND"])
    check("contract" in answer
          and answer["contract"]["failures"] == ["NOTHING_FOUND"],
          "a repeated failure state is one state, not two")

    print()
    print("6. The contract carries no tier, no risk and no status")
    answer = ask()
    for field in ("tier", "risk", "status"):
        check(field not in answer["contract"],
              "the designed contract has no '%s'" % field)
    problems = CON.validate(answer["contract"], known, where="designed")
    check(problems == [],
          "and it passes heron_contract.validate() from outside too")

    print()
    print("7. It always reports what it could not decide")
    answer = ask()
    check(len(answer["unjudged"]) >= 3,
          "three questions come back even on the confident path")
    check(any("SHAPE" in n for n in answer["unjudged"]),
          "whether the shape is right - T3, and no adapter to call")
    check(any("timed" in n for n in answer["unjudged"]),
          "whether the timeout is right - nothing has been timed")
    check(any("promise" in n for n in answer["unjudged"]),
          "whether the failure states are the ones the code will name")
    check(answer["contract"]["timeout-seconds"] == ARC.DEFAULT_TIMEOUT,
          "the default timeout is used, and named as a default")

    print()
    print("8. Every failure the contract declares is named by the code")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-AHR-ARC-003.yaml"))
    source = open(os.path.join(ROOT, "brain", "heron_architect.py"),
                  encoding="utf-8").read()
    declared = contract.get("failures") or []
    for failure in declared:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(declared) - reached - {"REGISTER_UNREADABLE"})
    check(not unreached,
          "and every state but REGISTER_UNREADABLE was reached above - a "
          "declared state no input can produce is a dead one%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a contract may narrow a job and may never widen one")
    return 0


if __name__ == "__main__":
    sys.exit(main())
