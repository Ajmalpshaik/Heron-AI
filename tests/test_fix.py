# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-ORC-FIX-005
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Repair - one of six routes is a repair, and every code in the table
proves it.

    python tests/test_fix.py

WHAT IT PROVES
  1. THE SIX ROUTES ARE HERON-ORC-FAIL-004's SIX. D-48 forbids `brain`
     importing `mcp`, so they are restated there - and a second copy is
     only safe if something compares them. A test may import from every
     layer, so this is where that happens.

  2. EVERY CODE IN THE REAL TABLE LANDS WHERE IT SHOULD. All of them are
     run through, and the answer for each is exactly what its route
     says - no code is unreachable and none is mis-sorted.

  3. STOP IS REFUSED UNDER ITS OWN NAME, because working around it is
     precisely what an agent called the Fix Agent is for.

  4. THE QUESTION IS THE ANALYSIS'S OWN WORDING, not a new one.

  5. TARGETED MEANS TARGETED. A field the request does not take is
     refused, and a supply that changes nothing is not a repair.

  6. NOTHING IS SENT.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_fix as FIX                                        # noqa: E402
import heron_failure as FAIL                                   # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

REQUEST = {"operation": "select_by_category", "category": ""}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_fix.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. the six routes are HERON-ORC-FAIL-004's six")
    # D-48 FORBIDS THE IMPORT IN brain. A test may cross every layer, so
    # this is the one place the copy can be checked against its source.
    for mine, theirs in ((FIX.RETRY, FAIL.RETRY), (FIX.WAIT, FAIL.WAIT),
                         (FIX.FIX_FIRST, FAIL.FIX_FIRST),
                         (FIX.LOOK_AT_THE_MODEL, FAIL.LOOK_AT_THE_MODEL),
                         (FIX.START_OVER, FAIL.START_OVER),
                         (FIX.STOP, FAIL.STOP)):
        check(mine == theirs, "'%s' is that module's word for it" % mine)
    check(len(FIX.ROUTES) == 6 and len(set(FIX.ROUTES)) == 6,
          "there are six, all different")
    # AND NO SEVENTH ON EITHER SIDE.
    theirs = set(step for _, step, _ in FAIL._KNOWN.values())
    check(not theirs - set(FIX.ROUTES),
          "and the real table uses no route this file has not got%s"
          % ("" if not theirs - set(FIX.ROUTES)
             else ": %s" % ", ".join(sorted(theirs - set(FIX.ROUTES)))))
    check(sorted(FIX.NOT_A_REPAIR) + [FIX.FIX_FIRST, FIX.STOP]
          and set(FIX.NOT_A_REPAIR) | {FIX.FIX_FIRST, FIX.STOP}
          == set(FIX.ROUTES),
          "every route is either the repair, the stop, or explained as "
          "not a repair - none is silently unhandled")

    print("\n2. every code in the real table lands where it should")
    counted = {"repair": 0, "stop": 0, "other": 0}
    for code, (outcome, route, reason) in sorted(FAIL._KNOWN.items()):
        answer = FIX.repair({"code": code, "next_step": route,
                             "reason": reason}, None, REQUEST)
        if route == FAIL.FIX_FIRST:
            want, bucket = "NEEDS_AN_ANSWER", "repair"
        elif route == FAIL.STOP:
            want, bucket = "MUST_NOT_WORK_AROUND", "stop"
        else:
            want, bucket = "NOT_A_REPAIR", "other"
        if answer.get("refused") != want:
            check(False, "%s (%s) gave %s, wanted %s"
                         % (code, route, answer.get("refused"), want))
            break
        counted[bucket] += 1
    else:
        check(True, "all %d codes land right: %d ask, %d stop, %d are not "
                    "a repair" % (len(FAIL._KNOWN), counted["repair"],
                                  counted["stop"], counted["other"]))
    check(counted["repair"] and counted["stop"] and counted["other"],
          "and all three outcomes are actually reached by the real table")

    print("\n3. stop is refused under its own name")
    for code, (outcome, route, reason) in FAIL._KNOWN.items():
        if route != FAIL.STOP:
            continue
        answer = FIX.repair({"code": code, "next_step": route,
                             "reason": reason},
                            {"category": "ducts"}, REQUEST)
        reached.add(answer.get("refused"))
        check(answer["refused"] == "MUST_NOT_WORK_AROUND",
              "'%s' is refused even WITH an answer supplied" % code)
    stopped = FIX.repair({"code": "read_only", "next_step": FIX.STOP,
                          "reason": "the model is open read-only"},
                         {"category": "ducts"}, REQUEST)
    check("do not retry, and do not work around it" in stopped["why"],
          "quoting HERON-ORC-FAIL-004's own words")
    check("what an agent called the Fix Agent is for" in stopped["why"],
          "and saying why this refusal names itself")

    print("\n4. the question is the analysis's own wording")
    asked = FIX.repair({"code": "no_category", "next_step": FIX.FIX_FIRST,
                        "reason": "no category was given"}, None, REQUEST)
    reached.add(asked.get("refused"))
    check(asked["refused"] == "NEEDS_AN_ANSWER", "with nothing supplied "
                                                 "it asks")
    check(asked["asked"] == "no category was given",
          "using the reason verbatim: %r" % asked["asked"])
    check(asked["asked"] == FAIL._KNOWN["no_category"][2],
          "which is the table's own text, not a new sentence")
    check("Heron cannot open a model" in asked["why"],
          "and says most fix_first codes are not a machine's to fix")

    print("\n5. targeted means targeted")
    odd = FIX.repair({"code": "no_category", "next_step": FIX.FIX_FIRST,
                      "reason": "no category was given"},
                     {"colour": "blue"}, REQUEST)
    reached.add(odd.get("refused"))
    check(odd["refused"] == "NOT_IN_THE_REQUEST",
          "a field the request does not take is refused")
    check(odd["fields"] == ["colour"] and "category" in odd["takes"],
          "naming what was supplied and what the request takes")
    check("improvisation" in odd["why"], "as an improvisation")
    same = FIX.repair({"code": "no_category", "next_step": FIX.FIX_FIRST,
                       "reason": "no category was given"},
                      {"category": ""}, REQUEST)
    reached.add(same.get("refused"))
    check(same["refused"] == "NOTHING_TO_REPAIR",
          "and a supply that changes nothing is not a repair")
    check("is a retry, which HERON-ORC-FAIL-004 did not choose"
          in same["why"], "it is a retry, which was not what was chosen")
    good = FIX.repair({"code": "no_category", "next_step": FIX.FIX_FIRST,
                       "reason": "no category was given"},
                      {"category": "ducts"}, REQUEST)
    check(good["repaired"] and good["changed"] == ["category"],
          "a real answer repairs exactly one field")
    check(good["was"]["category"] == "" and
          good["request"]["category"] == "ducts",
          "and both the before and the after come back")
    check(good["request"]["operation"] == REQUEST["operation"],
          "with everything else untouched")

    print("\n6. nothing is sent")
    imports = sorted(line.split()[1] for line in logic.split("\n")
                     if line.startswith("import "))
    check(imports == ["os", "sys"],
          "the whole import list is os and sys - it cannot even reach "
          "HERON-ORC-FAIL-004, which is D-48 and why the copy exists")
    for reaching in ("requests", "urllib", "socket", "subprocess",
                     "write(", "open("):
        check(reaching not in logic, "nothing here uses %s" % reaching)
    check("Nothing was sent" in good["why"], "and the answer says so")

    print("\n7. every declared failure is named and reached")
    for failure, supply, request, name in (
            (None, {"category": "x"}, REQUEST, "NOTHING_TO_REPAIR"),
            ({"code": "x"}, {"category": "x"}, REQUEST, "NOT_AN_ANALYSIS"),
            ({"code": "x", "next_step": "sideways", "reason": "y"},
             {"category": "x"}, REQUEST, "NOT_AN_ANALYSIS"),
            ({"code": "still_running", "next_step": FIX.WAIT,
              "reason": "y"}, {"category": "x"}, REQUEST, "NOT_A_REPAIR"),
            ({"code": "no_category", "next_step": FIX.FIX_FIRST,
              "reason": "y"}, "not a map", REQUEST, "NEEDS_AN_ANSWER"),
            ({"code": "no_category", "next_step": FIX.FIX_FIRST,
              "reason": "y"}, {"category": "x"}, None,
             "NOTHING_TO_REPAIR")):
        answer = FIX.repair(failure, supply, request)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-ORC-FIX-005.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 6, "the contract declares 6 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(good["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    one of six is a repair, and the table proves it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
