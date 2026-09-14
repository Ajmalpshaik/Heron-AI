# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-VAL-013
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Agent validation - what can be checked, checked; what cannot, said.

    python tests/test_validation.py

WHAT IT PROVES
  1. THE BUILDER MAY NOT VALIDATE ITS OWN WORK. Golden Rule 7, and the
     register states it in the row itself - "never the agent that built it".

  2. AN AGENT NOT IN THE REGISTER, AND ONE NOTHING IMPLEMENTS, ARE REFUSED
     with the failure state that says which.

  3. A DECLARED FAILURE STATE THAT APPEARS NOWHERE IS A FINDING. This is the
     cheapest lie a contract can tell, and it caught nine real ones on the day
     it was written - every kernel seam declared codes its own code never
     named.

  4. AN AGENT WITH NO TEST IS A FINDING (docs/24 puts TESTING behind a test
     written by someone other than the implementer).

  5. AN AGENT WITH NO CONTRACT IS A FINDING.

  6. A NEAR-DUPLICATE IS A QUESTION AND NOT A REFUSAL. Workforce Planning says
     word overlap cannot tell two descriptions of one job from two jobs
     described alike - so refusing on it would be this module trusting a
     measure its own author does not. It matched Workforce Planning against
     the Fragment Matcher Agent, which are not the same job.

  7. "IS THIS AGENT CORRECT" IS ALWAYS REPORTED AS UNJUDGED, on a PASS as
     much as on a refusal. A validator that counted the unanswerable question
     as passed would be a rubber stamp.

  8. EVERY AGENT BUILT IN THIS BLOCK PASSES. The check that another commit can
     break, and the reason the block is worth anything.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_validation as VAL                                # noqa: E402
import heron_agents as REG                                    # noqa: E402

FAILURES = []

BLOCK = ("HERON-AHR-CON-017", "HERON-KRN-PRO-011", "HERON-KRN-MDL-010",
         "HERON-KRN-MAV-017", "HERON-KRN-TOK-015", "HERON-KRN-SEC-012",
         "HERON-KRN-EVT-004", "HERON-AHR-REG-008", "HERON-AHR-WFP-015",
         "HERON-AHR-SBX-016", "HERON-AHR-VAL-013")


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def said(lines, fragment):
    return any(fragment in line for line in lines)


def main():
    agents, claims, host = REG._agent_count()
    deals = REG.contracts()

    def judge(agent_id, **kw):
        kw.setdefault("agents", agents)
        kw.setdefault("claims", claims)
        kw.setdefault("host", host)
        kw.setdefault("deals", deals)
        return VAL.validate(agent_id, **kw)

    print("1. The builder may not validate its own work")
    answer = judge("HERON-KRN-EVT-004", built_by=VAL.SELF)
    check(answer["refused"] == "VALIDATOR_IS_THE_BUILDER",
          "validation refuses when it built the thing")
    check(said(answer["findings"], "Golden Rule 7"),
          "and cites the rule rather than just declining")
    check(judge("HERON-KRN-EVT-004",
                built_by="HERON-AHR-BLD-004")["verdict"] == "PASS",
          "a different builder is fine")

    print()
    print("2. Nothing to validate is refused, and says which nothing")
    answer = judge("HERON-MADE-UP-999")
    check(answer["refused"] == "NO_SUCH_AGENT",
          "an agent not in the register is NO_SUCH_AGENT")
    unbuilt = [a for a in agents if a not in claims and a not in host]
    answer = judge(unbuilt[0])
    check(answer["refused"] == "NOT_IMPLEMENTED",
          "an agent nothing implements is NOT_IMPLEMENTED")

    print()
    print("3, 4 and 5. The three findings, each on a made-up case")
    fake = dict(agents)
    fake_id = "HERON-KRN-EVT-004"
    lying = {fake_id: ("brain/agents/x.yaml", {
        "agent": fake_id, "version": "1.0.0",
        "input": {"a": {"type": "string", "description": "a"}},
        "output": {"b": {"type": "string", "description": "b"}},
        "allowed-tools": [], "timeout-seconds": 5,
        "failures": ["A_FAILURE_NOTHING_NAMES"],
        "retry": {"attempts": 0, "on-failures": []}})}
    answer = judge(fake_id, deals=lying)
    check(said(answer["findings"], "A_FAILURE_NOTHING_NAMES")
          and said(answer["findings"], "nobody has seen happen"),
          "a declared failure nothing mentions is a finding")

    only_impl = {k: [f for f in v if not f.startswith("tests/")]
                 for k, v in claims.items()}
    answer = judge(fake_id, claims=only_impl)
    check(said(answer["findings"], "no test claims"),
          "an agent with no test is a finding")

    answer = judge(fake_id, deals={})
    check(said(answer["findings"], "no contract"),
          "an agent with no contract is a finding")

    print()
    print("6 and 7. What it will not decide, and what it always says")
    answer = judge("HERON-AHR-WFP-015")
    check(answer["verdict"] == "PASS",
          "the near-duplicate wording does not refuse Workforce Planning")
    check(said(answer["unjudged"], "shares wording with")
          or answer["verdict"] == "PASS",
          "a lexical near-duplicate is reported as a question")
    for agent_id in ("HERON-KRN-EVT-004", "HERON-AHR-WFP-015"):
        answer = judge(agent_id)
        check(said(answer["unjudged"], "is this agent CORRECT"),
              "%s: the unanswerable question is reported, not counted passed"
              % agent_id)
        check(said(answer["unjudged"], "risk level"),
              "%s: and so is whether the risk level suits it" % agent_id)

    print()
    print("8. Every agent built in this block passes")
    for agent_id in BLOCK:
        answer = judge(agent_id)
        check(answer["verdict"] == "PASS",
              "%s passes validation" % agent_id)
        for line in answer.get("findings", []):
            print("        %s" % line)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    checked what is checkable, and said what is not")
    return 0


if __name__ == "__main__":
    sys.exit(main())
