# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-HR-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Agent HR - the door, and the three fields it will not be told.

    python tests/test_hr.py

WHAT IT PROVES
  1. THE DOOR HOLDS. No assessment and a negative assessment are refused
     identically. A caller with nothing to show has not been turned down, it
     has not asked, and a guard with a way around it guards nothing.

  2. THE DOOR CANNOT BE TALKED THROUGH. There is no argument that says
     "cleared" - the verdict is read out of Workforce Planning's own answer,
     the same way the deployment ladder reads a stage out of a record.

  3. TIER, RISK AND STATUS ARE REFUSED, NOT DROPPED. Dropping them silently
     would leave the caller believing the value had been taken.

  4. AN UNKNOWN FIELD IS REFUSED TOO, for the same reason.

  5. EVERY TOOL IS CHECKED AGAINST THE SERVER'S OWN TABLE, which the TEST
     supplies because `brain` may not read `mcp` (D-48). Naming tools with no
     table is refused: compared against nothing is not compared and found
     fine. The suite asserts HR does not import its way around the layer.

  6. THE RISK FLOOR IS DERIVED FROM THAT TABLE. A job calling a MODIFY tool
     cannot come back as READ whatever else it says.

  7. A DEPENDENCY ON AN AGENT NOBODY PLANNED IS REFUSED, against the real
     250-row register rather than a fixture.

  8. A JOB THAT PROVIDES NOTHING AND CALLS NOTHING IS REFUSED.

  9. TWO CLAUSES ARE A QUESTION, NEVER A REFUSAL - the same call Workforce
     Planning made about word overlap and Agent Validation made about
     near-duplicates.

 10. NO ID IS INVENTED. An id is a row in docs/28, and an agent writing one
     is an agent editing the register.

 11. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE. Nine of ten
     agents failed exactly this check on their first validation run, and the
     fix went into the code rather than into the contracts.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_hr as HR                                         # noqa: E402
import heron_tools as T                                       # noqa: E402

FAILURES = []
CLEARED = {"verdict": "PROPOSE_HIRING"}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    import heron_agents as REG
    agents, _claims, _host = REG._agent_count()
    # THE REAL TABLE, from the module the server enforces. The TEST may read
    # mcp (D-48 allows tests {platform, revit, mcp, brain}); HR may not, and
    # check 5 below asserts that it does not.
    table = dict((name, T.NAMES[risk])
                 for name, (risk, _op) in T.TOOLS.items())

    def job(**kw):
        kw.setdefault("name", "Duct Counter")
        kw.setdefault("purpose", "counts ducts in a view")
        kw.setdefault("capabilities", ["COUNT_DUCTS"])
        kw.setdefault("agents", agents)
        kw.setdefault("table", table)
        return HR.write(**kw)

    print("1. The door holds, and holds the same way both times")
    for label, assessment in (("no assessment", None),
                              ("ALREADY_AN_AGENT",
                               {"verdict": "ALREADY_AN_AGENT"}),
                              ("THIS_IS_A_FRAGMENT",
                               {"verdict": "THIS_IS_A_FRAGMENT"}),
                              ("EXTEND_EXISTING",
                               {"verdict": "EXTEND_EXISTING"}),
                              ("ALREADY_A_CAPABILITY",
                               {"verdict": "ALREADY_A_CAPABILITY"})):
        answer = job(assessment=assessment)
        check(answer.get("refused") == "NOT_CLEARED_TO_HIRE",
              "%s is refused with NOT_CLEARED_TO_HIRE" % label)

    print()
    print("2. There is no argument that says 'cleared'")
    import inspect
    names = set(inspect.signature(HR.write).parameters)
    for forbidden in ("cleared", "verdict", "approved", "ok"):
        check(forbidden not in names,
              "write() has no '%s' parameter a caller could assert"
              % forbidden)
    answer = job(assessment={"verdict": "PROPOSE_HIRING", "matches": []})
    check("job" in answer,
          "the verdict is read out of Workforce Planning's own answer")

    print()
    print("3. Tier, risk and status are refused, not dropped")
    for field, value in (("tier", "T2"), ("risk", "READ"),
                         ("status", "PROVEN")):
        answer = job(assessment=CLEARED, **{field: value})
        check(answer.get("refused") == "NOT_HRS_TO_DECIDE",
              "'%s' is refused - %s owns it" % (field, HR.NOT_HRS[field]))
    answer = job(assessment=CLEARED, tier="T2", risk="ADMIN")
    check(answer.get("refused") == "NOT_HRS_TO_DECIDE"
          and "risk" in answer["why"] and "tier" in answer["why"],
          "both are named when both arrive, not just the first")

    print()
    print("4. A field the job description does not have is refused too")
    answer = job(assessment=CLEARED, salary="a great deal")
    check(answer.get("refused") == "NOT_A_JOB_FIELD",
          "'salary' is refused rather than quietly kept")

    print()
    print("5. Tools are checked against the server's own table")
    check(table and "heron_lookup" in table,
          "the test supplies the real table from mcp/server/heron_tools.py")
    source_hr = open(os.path.join(ROOT, "brain", "heron_hr.py"),
                     encoding="utf-8").read()
    check("import heron_tools" not in source_hr,
          "and HR itself does not import it - brain may not read mcp (D-48)")
    answer = job(assessment=CLEARED, tools=["revit_delete_everything"])
    check(answer.get("refused") == "NO_SUCH_TOOL",
          "a tool nothing provides is refused")
    answer = job(assessment=CLEARED, tools=["heron_lookup"], table={})
    check(answer.get("refused") == "TOOLS_NOT_CHECKED",
          "naming tools with no table is refused, not written as verified")
    check(HR.risk_floor(["x"], {"x": "SUPERUSER"}) is None,
          "a risk word off docs/12's ladder counts as nothing, not as READ")

    print()
    print("6. The risk floor is derived, and cannot be argued down")
    answer = job(assessment=CLEARED, tools=["heron_lookup"])
    check(answer["job"]["risk-floor"] == "READ",
          "a job calling only a READ tool floors at READ")
    answer = job(assessment=CLEARED,
                 tools=["heron_lookup", "revit_apply_move"])
    check(answer["job"]["risk-floor"] == "MODIFY",
          "one MODIFY tool among reads floors the whole job at MODIFY")
    answer = job(assessment=CLEARED, tools=[])
    check(answer["job"]["risk-floor"] is None,
          "a job that calls nothing has no floor, which is not READ")
    check("tier" not in answer["job"] and "risk" not in answer["job"],
          "the job carries a floor and neither a tier nor a risk")

    print()
    print("7. A dependency nobody planned is refused")
    answer = job(assessment=CLEARED, dependencies=["HERON-AHR-NOPE-999"])
    check(answer.get("refused") == "NO_SUCH_DEPENDENCY",
          "an agent absent from docs/28 cannot be depended on")
    answer = job(assessment=CLEARED, dependencies=["HERON-AHR-WFP-015"])
    check("job" in answer,
          "an agent the register carries can be, against the real register")

    print()
    print("8. A job that provides nothing and calls nothing is refused")
    answer = job(assessment=CLEARED, capabilities=[], tools=[])
    check(answer.get("refused") == "NOTHING_TO_DO",
          "no capability and no tool is a description of nothing")
    answer = job(assessment=CLEARED, capabilities=[], tools=["heron_lookup"])
    check("job" in answer and answer["unjudged"],
          "a tool with no capability is written, and says what is missing")

    print()
    print("9. Two clauses ask a question rather than refuse")
    answer = job(assessment=CLEARED,
                 purpose="counts ducts in a view and renames the sheets")
    check("job" in answer, "it is written - a splitter does not get a veto")
    check(any("clauses" in note for note in answer["unjudged"]),
          "and the second clause is reported as unjudged")
    answer = job(assessment=CLEARED, purpose="counts ducts in a view")
    check(not any("clauses" in note for note in answer["unjudged"]),
          "one clause asks nothing")

    print()
    print("10. No id is invented")
    answer = job(assessment=CLEARED)
    check(answer["job"]["agent-id"] is None,
          "agent-id is None - an id is a row in docs/28")
    check("docs/28" in answer["why"],
          "and the answer says so rather than leaving it to be noticed")

    print()
    print("11. Every failure the contract declares is named by the code")
    import heron_contract as CON
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-AHR-HR-002.yaml"))
    source = open(os.path.join(ROOT, "brain", "heron_hr.py"),
                  encoding="utf-8").read()
    for failure in contract.get("failures") or []:
        check(failure in source,
              "the code names %s" % failure)
    check(len(contract.get("failures") or []) >= 1,
          "and the contract declares at least one")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the guard in front of the factory cannot be walked around")
    return 0


if __name__ == "__main__":
    sys.exit(main())
