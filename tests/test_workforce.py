# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-WFP-015
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Workforce planning - three of the five answers are no, and that is the ratio.

    python tests/test_workforce.py

WHAT IT PROVES
  1. A NEAR-DUPLICATE OF AN EXISTING AGENT IS CAUGHT, against the real 250-row
     register rather than a fixture.

  2. ONE SHARED WORD IS NEVER A MATCH. The first draft scored "Duct Counter -
     counts ducts in a view" against "Revit View Agent" at 1.00: one shared
     word, divided by a name with one word left in it after the stop list. A
     rule that scores a coincidence at certainty is the rule that gets
     believed.

  3. PLURALS MEET THEIR SINGULARS - "ducts" and "duct" are the same word for
     this purpose - and short words keep their s, so "gas" does not become
     "ga".

  4. A CAPABILITY THAT ALREADY EXISTS IS FOUND BY NAME AND BY WORDS. A check
     that only fired on an exact id would answer a narrower question than the
     register asks.

  5. ONE COMPOSABLE OPERATION IS A FRAGMENT, NOT AN AGENT (D-29).

  6. A NEIGHBOUR IN THE SAME DEPARTMENT IS OFFERED BEFORE HIRING.

  7. PROPOSE_HIRING IS ONLY REACHED WHEN EVERY CHEAPER ANSWER FAILED, and it
     says in the answer itself that it is a proposal and not an approval
     (Golden Rule 7).

  8. THE JUDGEMENT WORD OVERLAP CANNOT MAKE IS REPORTED AS UNJUDGED, EVERY
     TIME - including when the verdict is a confident yes. A silent "no
     overlap found" from a check that never ran is how the guard fails.

  9. A PROPOSAL WITH NO NAME OR NO PURPOSE IS REFUSED, as data.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_workforce as WFP                                 # noqa: E402
import heron_router as ROUTER                                 # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    import heron_agents as REG
    agents, _claims, _host = REG._agent_count()
    capabilities = WFP.fragment_capabilities()

    def ask(name, purpose, **kw):
        kw.setdefault("agents", agents)
        kw.setdefault("capabilities", capabilities)
        return WFP.assess(name, purpose, **kw)

    print("1. A near-duplicate of a real agent is caught")
    answer = ask("Session Rollback Helper",
                 "Reverses everything Heron did this session, newest first, "
                 "using the audit log")
    check(answer["verdict"] == WFP.ALREADY_AN_AGENT,
          "a proposal that restates an existing agent is refused")
    check(any(m["id"] == "HERON-REVIT-RBK-036" for m in answer["matches"]),
          "and the agent it duplicates is named")

    print()
    print("2 and 3. One shared word is not a match; plurals are")
    check(WFP.overlap("counts ducts in a view", "Revit View Agent") == 0.0,
          "one shared word scores zero, not one")
    check(WFP.overlap("counts ducts in a view",
                      "duct view counting") > 0.5,
          "two shared words do score")
    check("duct" in WFP.words("ducts") and "view" in WFP.words("views"),
          "plurals meet their singulars")
    check("ga" not in WFP.words("gas") and "gas" in WFP.words("gas"),
          "a short word keeps its s")
    check(WFP.words("the and of a duct") == {"duct"},
          "stop words and two-letter words are dropped")

    print()
    print("4. A capability that already exists is found")
    answer = ask("Element Counter",
                 "count elements that a filter found in the model")
    check(answer["verdict"] == WFP.ALREADY_A_CAPABILITY,
          "a proposal matching an existing capability by words is refused")
    check("COUNT_ELEMENTS" in answer["why"], "and the capability is named")

    known = sorted(capabilities)[0]
    answer = ask("Something New", "an unrelated responsibility entirely",
                 capability=known)
    check(answer["verdict"] == WFP.ALREADY_A_CAPABILITY,
          "and an exact capability id is refused too")

    print()
    print("5. One composable operation is a fragment")
    answer = ask("Sleeve Sizer", "work out the sleeve size for a pipe",
                 capability="SIZE_SLEEVE_FOR_PIPE")
    check(answer["verdict"] == WFP.THIS_IS_A_FRAGMENT,
          "a named capability in an operation's shape is a fragment")
    check("D-29" in answer["why"], "and the decision that settles it is cited")

    print()
    print("6. A neighbour is offered before hiring")
    answer = ask("Workset Checker",
                 "Worksets, element ownership and checkout state, reported",
                 department="Revit Engineering")
    check(answer["verdict"] in (WFP.ALREADY_AN_AGENT, WFP.EXTEND_EXISTING),
          "a proposal close to a neighbour does not reach hiring")

    print()
    print("7. Hiring is the last answer, and it is a proposal")
    answer = ask("Weather Reporter",
                 "tells somebody whether it is raining outside the office")
    check(answer["verdict"] == WFP.PROPOSE_HIRING,
          "something genuinely unlike anything here reaches PROPOSE_HIRING")
    check("not an approval" in answer["why"]
          and "Golden Rule 7" in answer["why"],
          "and the answer says it is a proposal, in the answer itself")

    print()
    print("8. What could not be judged is always reported")
    seen = []
    for name, purpose, kw in (
            ("Session Rollback Helper", "Reverses everything Heron did this "
             "session, newest first, using the audit log", {}),
            ("Weather Reporter", "tells somebody whether it is raining", {}),
            ("Element Counter", "count elements that a filter found", {})):
        answer = ask(name, purpose, **kw)
        seen.append(bool(answer["unjudged"]))
    check(all(seen),
          "every verdict carries the question nothing asked, confident ones "
          "included")

    router = ROUTER.Router()
    answer = ask("Weather Reporter", "tells somebody whether it is raining",
                 router=router)
    check(any("NO_ADAPTER" in line or "nobody to ask" in line
              for line in answer["unjudged"]),
          "with a router and no adapter, the router's own reason is carried")
    router.register("on-machine", ROUTER.LOCAL)
    answer = ask("Weather Reporter", "tells somebody whether it is raining",
                 router=router)
    check(any("on-machine" in line for line in answer["unjudged"]),
          "with an adapter, it names who would be asked - and still asks "
          "nobody, because this module does not call models yet")

    print()
    print("9. A proposal nobody could assess is refused")
    for name, purpose in (("", "a purpose"), ("A name", ""), ("  ", "  ")):
        answer = WFP.assess(name, purpose, agents=agents,
                            capabilities=capabilities)
        check(answer.get("refused") == "NOTHING_TO_ASSESS",
              "'%s' / '%s' is refused as data, not raised" % (name, purpose))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the expensive answer stays the last one tried")
    return 0


if __name__ == "__main__":
    sys.exit(main())
