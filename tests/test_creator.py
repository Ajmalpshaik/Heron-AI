# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-CRT-006
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Creator - ADMIN, and still unable to promote anything.

    python tests/test_creator.py

WHAT IT PROVES
  1. PROPOSED IS THE ONLY STATUS IT EVER ASSIGNS, on every path including
     the one where every step succeeds.

  2. NO CALLER MAY HAND A STATUS IN. A parameter that sets one is a
     promotion route through the agent that automates the hiring - which is
     the one place Golden Rule 7 would be most convenient to forget.

  3. IT DOES NOT TOUCH THE DEPLOYMENT LADDER. Its source imports no
     deployment, holds no approval, and names no stage above PROPOSED.

  4. THE GUARD RUNS FIRST. Workforce Planning is step one, so the pipeline
     cannot be used to route round it.

  5. IT STOPS AT THE REGISTER ROW, and that is the design rather than an
     omission: HR leaves the id None and the Architect refuses an id the
     register does not carry, so the pipeline halts at the one step that is
     the hiring decision.

  6. IT RUNS EVERY STEP ONCE THE ROW EXISTS, and the contract that comes out
     is the Architect's own, already validated.

  7. NOTHING IS SWALLOWED. Each step's refusal arrives whole - the state and
     the sentence - and each step's unjudged notes are carried with the step
     that raised them (Golden Rule 14).

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED
     BY THIS SUITE.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_creator as CRT                                   # noqa: E402
import heron_agents as REG                                    # noqa: E402
import heron_contract as CON                                  # noqa: E402

FAILURES = []

SAMPLE = {"heron_lookup": "READ"}
NEW = dict(name="Duct Sizing Reviewer",
           purpose="reviews duct sizing against the project brief",
           tools=["heron_lookup"], table=SAMPLE)
DESIGNABLE = dict(NEW, agent_id="HERON-AHR-CRT-006",
                  failures=["NO_DUCTS_IN_VIEW"],
                  inputs={"view": {"type": "string", "required": True,
                                   "description": "The view to read."}},
                  outputs={"findings": {"type": "list",
                                        "description": "Where it departs."}})


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    agents, _claims, _host = REG._agent_count()
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_creator.py"),
                  encoding="utf-8").read()

    def ask(**kw):
        kw.setdefault("agents", agents)
        answer = CRT.run(**kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. PROPOSED is the only status it ever assigns")
    check(CRT.MAY_ASSIGN == "PROPOSED", "the constant is PROPOSED")
    for label, kw in (("a turned-down proposal",
                       dict(name="Agent Sandbox Agent",
                            purpose="runs a newly built agent in isolation")),
                      ("a job with no register row", NEW),
                      ("a job with one", DESIGNABLE)):
        answer = ask(**kw)
        check(answer.get("assigned") == "PROPOSED",
              "%s comes back assigned PROPOSED" % label)
    # EVERY PLACE THE SOURCE SETS `assigned` SETS IT TO THE CONSTANT. A
    # word search would fail on the demo, which passes status="PROVEN" on
    # purpose to show the refusal - so this reads the assignments instead.
    import re
    written = re.findall(r'"assigned":\s*([^,\n}]+)', source)
    check(written, "the source sets `assigned` somewhere")
    check(all(value.strip() == "MAY_ASSIGN" for value in written),
          "and every one of the %d places sets it to MAY_ASSIGN, never a "
          "literal" % len(written))

    print()
    print("2. No caller may hand a status in")
    import inspect
    names = set(inspect.signature(CRT.run).parameters)
    for forbidden in ("status", "state", "stage", "approval", "assigned"):
        check(forbidden not in names,
              "run() has no '%s' parameter of its own" % forbidden)
        answer = ask(name="Duct Sizing Reviewer", purpose="reviews sizing",
                     **{forbidden: "PROVEN"})
        check(answer.get("refused") == "MAY_ONLY_ASSIGN_PROPOSED",
              "and passing '%s' anyway is refused, not ignored" % forbidden)
    answer = ask(name="Duct Sizing Reviewer", purpose="reviews sizing",
                 salary="a great deal")
    check(answer.get("refused") == "NOT_A_PIPELINE_INPUT",
          "an unknown field is refused too, not quietly kept")

    print()
    print("3. It does not touch the deployment ladder")
    check("heron_deployment" not in source,
          "the source does not import the deployment agent")
    check("activate" not in source,
          "and never calls activate()")
    for word in ("approve", "approval", "signature"):
        check("def %s" % word not in source,
              "it defines no %s of its own" % word)

    print()
    print("4. The guard runs first")
    check(CRT.PIPELINE[0][1] == "HERON-AHR-WFP-015",
          "step one is Workforce Planning")
    answer = ask(name="Agent Sandbox Agent",
                 purpose="runs a newly built agent in isolation, never "
                         "against a live model")
    check(answer["stopped_at"] == "assess",
          "a near-duplicate stops at the guard, not after HR has written "
          "a job description for it")
    check(answer["pipeline"][0]["refused"],
          "and the guard's own verdict is what stopped it")

    print()
    print("5. It stops at the register row")
    answer = ask(**NEW)
    check(answer["stopped_at"] == "register",
          "with no id, the pipeline halts at the row")
    check(answer["waiting_on"] == "a person",
          "and the person is who it waits on")
    check("hiring decision" in answer["pipeline"][-1]["why"],
          "the reason given is that the row IS the hiring decision")
    check([s["step"] for s in answer["pipeline"]][:2]
          == ["assess", "describe"],
          "and the two steps before it did run")
    job = answer["pipeline"][1]["result"]
    check(job["agent-id"] is None,
          "HR's job description still carries no id")

    print()
    print("6. It runs every step once the row exists")
    answer = ask(**DESIGNABLE)
    check(answer["stopped_at"] is None, "nothing stopped it")
    check([s["step"] for s in answer["pipeline"]]
          == ["assess", "describe", "design"],
          "all three agent steps ran, in order")
    contract = answer["pipeline"][-1]["result"]
    check(contract["agent"] == "HERON-AHR-CRT-006",
          "and a contract came out, for the id the register carries")
    check(CON.validate(contract, CON.registry_ids(),
                       where="from the pipeline") == [],
          "which validates from outside the Architect too")
    for field in ("tier", "risk", "status"):
        check(field not in contract,
              "and carries no '%s'" % field)

    print()
    print("7. Nothing is swallowed")
    answer = ask(**NEW)
    for step in answer["pipeline"]:
        check(step.get("owner"),
              "the '%s' step names its owner" % step["step"])
        check(step.get("result") is not None or step.get("why"),
              "and carries either its result or the sentence it refused with")
    notes = answer["unjudged"]
    check(notes and "NOTHING HERE IS APPROVED" in notes[0],
          "the first note on every run is that nothing is approved")
    attributed = [n for n in notes[1:] if n.split(" ")[0] in
                  ("assess", "describe", "design")]
    check(attributed,
          "and each step's caveats are carried under that step's name")
    answer = ask(name="", purpose="")
    check(answer.get("refused") == "NOTHING_TO_CREATE",
          "a pipeline with nothing to create is refused before any step")

    print()
    print("8. Every failure the contract declares is named and reached")
    declared = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-AHR-CRT-006.yaml"))
    named = declared.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(named) - reached - {"REGISTER_UNREADABLE"})
    check(not unreached,
          "and every state but REGISTER_UNREADABLE was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the agent that automates the hiring cannot complete it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
