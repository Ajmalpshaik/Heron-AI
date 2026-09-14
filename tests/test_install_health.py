# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-HLT-009
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Install health - "done" is not evidence, and end to end means every step.

    python tests/test_install_health.py

WHAT IT PROVES
  1. EVERY STEP OF docs/07 s1 IS IN THE LIST, and a name outside it is
     refused - the install is a defined sequence.

  2. A STEP WITH NO CHECK IS UNCHECKED, NOT FINE, and keeps the overall
     state below HEALTHY. "Nobody looked" and "fine" are the two readings
     an install report must never merge.

  3. A STATE WITH NO OBSERVER IS THE INSTALLER'S OWN WORD, and is refused.

  4. A STEP VERIFIED BY THE THING THAT PERFORMED IT IS NOT VERIFIED -
     Golden Rule 7, one layer down.

  5. THE FOUR STATES AND THE SEVERITY ORDER ARE HERON-OPS-HLT-004's, not a
     second set that would drift from them.

  6. `complete()` IS THE ONLY PART THAT SAYS NO, and it names everything
     outstanding.

  7. IT FIXES NOTHING AND RE-RUNS NOTHING.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_install_health as INS                             # noqa: E402
import heron_health as HEALTH                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def ok(installed="installer", checked="heron-verify",
       state=HEALTH.HEALTHY):
    return {"state": state, "installed_by": installed,
            "checked_by": checked, "why": "checked"}


def checks(**more):
    found = dict((step, ok()) for step in INS.VERIFIED_BY_CHECKS)
    found.update(more)
    return found


def main():
    reached = set()
    source = open(os.path.join(ROOT, "mcp", "server",
                               "heron_install_health.py"),
                  encoding="utf-8").read()

    # A SENTINEL, not None: `None` is one of the inputs under test.
    DEFAULT = object()

    def look(what=DEFAULT):
        answer = INS.report(checks() if what is DEFAULT else what)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    def finish(what=DEFAULT):
        answer = INS.complete(checks() if what is DEFAULT else what)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. Every step of docs/07 s1 is in the list")
    check(len(INS.STEPS) == 16, "there are sixteen steps")
    check(INS.STEPS[-1] == "user-told",
          "and the last is step 16 - the sentence the user reads")
    check(len(INS.VERIFIED_BY_CHECKS) == 15,
          "so fifteen are verified by checks and the sixteenth is produced")
    check(INS.STEPS[-1] not in INS.VERIFIED_BY_CHECKS,
          "step 16 is not something a check reports on")
    for name in ("telemetry", "revit", "", "addin_deployed", "step-8"):
        answer = look({name: ok()})
        check(answer.get("refused") == "NOT_A_STEP",
              "'%s' is not one of the sixteen" % name)
    check("a defined sequence" in look({"telemetry": ok()})["why"],
          "and the refusal says the install is a defined sequence")

    print()
    print("2. A step with no check is UNCHECKED, not fine")
    partial = dict((step, ok()) for step in INS.VERIFIED_BY_CHECKS[:12])
    answer = look(partial)
    check(answer["state"] == HEALTH.WARNING,
          "twelve of fifteen is not HEALTHY")
    check(len(answer["unchecked"]) == 3, "three are named as unchecked")
    check(answer["checked"] == 12 and answer["of"] == 15,
          "and the counts say twelve of fifteen")
    check("which is why this is not HEALTHY" in answer["why"],
          "with the reason stated rather than left to be worked out")
    unchecked = [entry for entry in answer["steps"]
                 if entry["state"] == "UNCHECKED"]
    check(len(unchecked) == 3, "each unchecked step is still in `steps`...")
    check("'Done' is not evidence" in unchecked[0]["why"],
          "...saying why the installer's word is not a check")
    check(look()["state"] == HEALTH.HEALTHY,
          "while all fifteen checked and healthy IS HEALTHY")
    check(look()["unchecked"] == [], "with nothing unchecked")
    # AND AN UNCHECKED STEP NEVER LOWERS A WORSE STATE.
    answer = look(dict(partial, **{"rag-initialised":
                                   ok(state=HEALTH.FAILED)}))
    check(answer["state"] == HEALTH.FAILED,
          "an unchecked step does not soften a FAILED one - worst still wins")

    print()
    print("3. A state with no observer is the installer's own word")
    for missing in ({"state": "HEALTHY", "installed_by": "setup"},
                    {"state": "HEALTHY", "checked_by": ""},
                    {"state": "HEALTHY", "checked_by": "   "}):
        answer = look(checks(**{"mcp-configured": missing}))
        check(answer.get("refused") == "STEP_NOT_CHECKED",
              "%r has no observer" % missing)
    answer = look(checks(**{"mcp-configured": {"state": "HEALTHY",
                                               "installed_by": "setup"}}))
    check("wearing a check's clothes" in answer["why"],
          "and the refusal says what that state actually is")
    check(answer["step"] == "mcp-configured", "naming which step")
    # A STATE NOBODY RECOGNISES IS NOT A STATE - it reads as unchecked.
    answer = look(checks(**{"mcp-configured": {"state": "PROBABLY FINE",
                                               "checked_by": "x"}}))
    check("mcp-configured" in answer["unchecked"],
          "and a state outside the four reads as UNCHECKED, not as a pass")

    print()
    print("4. A step verified by the thing that performed it")
    answer = look(checks(**{"addin-deployed": ok("deploy-addin",
                                                 "deploy-addin")}))
    check(answer.get("refused") == "INSTALLER_VERIFIED_ITSELF",
          "same installer and checker is refused")
    check("Golden Rule 7" in answer["why"], "citing the rule")
    check("success path with no observer on it" in answer["why"],
          "and what that leaves behind")
    check(look(checks(**{"addin-deployed": ok("deploy-addin", "DEPLOY-ADDIN")}))
          .get("refused") == "INSTALLER_VERIFIED_ITSELF",
          "and case does not get round it")
    check(look(checks(**{"addin-deployed": ok("deploy-addin", "heron-verify")}))
          ["state"] == HEALTH.HEALTHY,
          "while a different checker is fine")
    check(look(checks(**{"addin-deployed": {"state": "HEALTHY",
                                            "checked_by": "heron-verify"}}))
          ["state"] == HEALTH.HEALTHY,
          "and a check that does not say who installed it is not refused - "
          "this agent cannot claim a conflict it was not told about")

    print()
    print("5. The four states are HERON-OPS-HLT-004's")
    check(sorted(HEALTH.SEVERITY) == sorted(["HEALTHY", "WARNING",
                                             "DEGRADED", "FAILED"]),
          "the four come from heron_health")
    check("HEALTH.SEVERITY" in source and "SEVERITY = {" not in source,
          "and the severity order is imported, not redeclared here")
    worse = {HEALTH.WARNING: HEALTH.WARNING,
             HEALTH.DEGRADED: HEALTH.DEGRADED,
             HEALTH.FAILED: HEALTH.FAILED}
    for state, expected in worse.items():
        answer = look(checks(**{"rag-initialised": ok(state=state)}))
        check(answer["state"] == expected,
              "one step %s makes the whole install %s" % (state, expected))
    answer = look(checks(**{"rag-initialised": ok(state=HEALTH.WARNING),
                            "brain-initialised": ok(state=HEALTH.FAILED)}))
    check(answer["state"] == HEALTH.FAILED, "and the WORST wins, not the last")
    check(any("two vocabularies that disagree" in note
              for note in look()["unjudged"]),
          "the answer says why it did not invent a second set")

    print()
    print("6. complete() is the only part that says no")
    answer = finish()
    check(answer["complete"] is True, "a clean install may be reported")
    check("each by something other than what installed it" in answer["why"],
          "and the sentence says what that rests on")
    answer = finish(partial)
    check(answer.get("refused") == "NOT_COMPLETE",
          "an install with unchecked steps may not")
    check(len(answer["outstanding"]) == 3, "naming all three")
    check(all(entry["state"] == "UNCHECKED"
              for entry in answer["outstanding"]),
          "with what is wrong with each")
    check("the sentence a user remembers" in answer["why"],
          "and why that sentence is worth being careful about")
    check("it is READ" in answer["proposal"],
          "while making clear this agent will not fix them")
    answer = finish(checks(**{"rag-initialised": ok(state=HEALTH.WARNING)}))
    check(answer.get("refused") == "NOT_COMPLETE",
          "even one WARNING is not complete")
    check("rag-initialised (WARNING)" in answer["why"],
          "named with its state")
    check(finish({}).get("refused") == "NO_STEPS",
          "and complete() carries report()'s refusals through")

    print()
    print("7. It fixes nothing and re-runs nothing")
    for word in ("subprocess", "os.system", "exec(", "eval(", "open(",
                 "shutil", "os.remove", "import requests", "install("):
        check(word not in source, "the source has no %s" % word)
    check(any("NOTHING WAS RE-RUN OR FIXED" in note
              for note in look()["unjudged"]),
          "and the answer says so")
    check(any("thorough check from a shallow one" in note
              for note in look()["unjudged"]),
          "with the limit of what it can tell")

    print()
    print("8. Every failure the contract declares is named and reached")
    for empty in ({}, None, [], "nothing"):
        check(look(empty).get("refused") == "NO_STEPS",
              "%r is nothing checked" % (empty,))
    check("a report nobody ran" in look({})["why"],
          "and an empty report is not a clean install")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-INS-HLT-009.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    'done' is not evidence, and end to end means every step")
    return 0


if __name__ == "__main__":
    sys.exit(main())
