# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-FLG-009
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Feature Flags - three states, and only a person may move one.

    python tests/test_flags.py

WHAT IT PROVES
  1. THREE STATES, AND THE FOURTH WORD IS REFUSED. Not rounded to the
     nearest one - the nearest state to a typo is a guess about whether a
     component runs.

  2. TEST IS NOT ON AND IS NOT OFF. It runs and its output is not used.
     Both easy misreadings are checked for directly, because both are how
     shadow mode stops producing evidence or ships an unproven component.

  3. GOLDEN RULE 19 IS ENFORCED FOR EVERY KIND OF CONTENT IT NAMES, and for
     origins nobody recognised, and for no origin at all. The same flip,
     asked for by the user, goes through - so the refusals are the rule and
     not a broken function.

  4. OFF GETS THE SAME RULE AS ON. The components most worth switching off
     are the ones that check things.

  5. "PER ACTION" IS STRUCTURAL. An approval that does not name this flag
     and this state is refused, and a signature already recorded against the
     flag is refused as a replay - including after the flag moved back,
     which is the case a check against the current state alone waves through.

  6. NOTHING IS STORED AND NOTHING IS RUN. The table handed in is not
     modified, and the module starts no process.

  7. A FLAG NOBODY DECLARED FAILS CLOSED EVEN FOR A CARELESS CALLER -
     refused AND reading OFF.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import copy
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_flags as FLG                                      # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

SIGNED = {"by": "ajmal", "at": "2026-09-14T10:00Z"}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def sign(flag, state, **more):
    record = dict(SIGNED, flag=flag, state=state)
    record.update(more)
    return record


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_flags.py"),
                  encoding="utf-8").read()

    def ask(flags, name, state, **kw):
        answer = FLG.set_flag(flags, name, state, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    def reading(flags, name):
        answer = FLG.read(flags, name)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    # The three docs/23 s11 gives, in the three states it gives them in.
    def table():
        return {"SmartDimensioning": {"state": "OFF"},
                "ExperimentalRAG": {"state": "ON"},
                "NewAgentSystem": {"state": "TEST"}}

    print("1. Three states, and the fourth word is refused")
    check(FLG.STATES == ("OFF", "ON", "TEST"),
          "the states are exactly the three docs/23 s11 names")
    for word in ("ENABLED", "on-ish", "50%", "True", "", "SHADOW", "TESTING"):
        answer = ask(table(), "SmartDimensioning", word, origin="user",
                     approval=sign("SmartDimensioning", word))
        check(answer.get("refused") == "NOT_A_FLAG_STATE",
              "%r is refused, not rounded" % word)
    check("guess about whether a component runs"
          in ask(table(), "SmartDimensioning", "TESTING")["why"],
          "and the refusal says why rounding it would be a guess")
    check(FLG.meaning("SHADOW") is None,
          "meaning() answers None for a word it does not know, rather than "
          "a default a caller would act on")
    check(FLG.meaning(" test ")["state"] == "TEST",
          "while spacing and case are read, not refused")

    print()
    print("2. TEST is not ON and is not OFF")
    test = FLG.meaning("TEST")
    check(test["runs"] is True, "TEST runs - reading it as OFF gets no evidence")
    check(test["output_used"] is False,
          "and its output is NOT used - reading it as ON ships an unproven "
          "component to a live model")
    check(FLG.meaning("ON")["output_used"] is True
          and FLG.meaning("OFF")["runs"] is False,
          "while ON delivers and OFF does not run, so TEST is the one state "
          "where the two answers disagree")
    check("docs/18" in test["why"],
          "and TEST cites shadow mode, which is what it is for")
    answer = reading(table(), "NewAgentSystem")
    check(answer["runs"] is True and answer["output_used"] is False,
          "read() carries both answers, so a caller cannot get only one")
    survey = FLG.survey(table())
    check(survey["on"] == ["ExperimentalRAG"]
          and survey["test"] == ["NewAgentSystem"],
          "and a survey lists TEST apart from ON, never added to it")
    check("scored, not used" in survey["why"],
          "saying what the TEST count means in the same sentence")

    print()
    print("3. Golden Rule 19 - every kind of content it names")
    for origin in ("a document Heron read", "a family name",
                   "a parameter description", "an imported folder",
                   "text in the model", "a community package"):
        answer = ask(table(), "SmartDimensioning", "ON", origin=origin,
                     approval=sign("SmartDimensioning", "ON"))
        check(answer.get("refused") == "NOT_FROM_THE_USER"
              and "data, never instruction" in answer["why"].lower(),
              "'%s' is refused as data, never instruction" % origin)
    for origin in (None, "", "a script", "the MCP bridge", "an agent",
                   "USER_APPROVED", "system"):
        answer = ask(table(), "SmartDimensioning", "ON", origin=origin,
                     approval=sign("SmartDimensioning", "ON"))
        check(answer.get("refused") == "NOT_FROM_THE_USER",
              "%r is refused too - it fails closed, not open" % origin)
    answer = ask(table(), "SmartDimensioning", "ON", origin="user",
                 approval=sign("SmartDimensioning", "ON"))
    check(answer.get("changed") is True,
          "and the SAME flip from the user goes through, so the refusals "
          "above are the rule and not a broken function")
    check("Golden Rule 19" in FLG.__doc__ and "Golden Rule 19" in source,
          "the code names the rule it is enforcing")

    print()
    print("4. OFF gets the same rule as ON")
    for state in ("OFF", "ON", "TEST"):
        flags = table()
        start = FLG.read(flags, "ExperimentalRAG")["state"]
        if start == state:
            continue
        answer = ask(flags, "ExperimentalRAG", state,
                     origin="a document Heron read",
                     approval=sign("ExperimentalRAG", state))
        check(answer.get("refused") == "NOT_FROM_THE_USER",
              "a document cannot move ON -> %s either" % state)
        answer = ask(flags, "ExperimentalRAG", state, origin="user")
        check(answer.get("refused") == "NOT_APPROVED",
              "and ON -> %s unsigned is refused the same way" % state)
    check("BOTH directions" in source,
          "and the source says the direction does not change the rule")

    print()
    print("5. 'Per action' is structural")
    for approval, label in (
            (True, "a bare True"),
            ("yes", "the word yes"),
            ({}, "an empty record"),
            ({"at": "T"}, "signed by nobody"),
            ({"by": "ajmal"}, "signed at no time"),
            (dict(SIGNED), "naming neither flag nor state"),
            (sign("SmartDimensioning", "ON"), "for another flag"),
            (sign("ExperimentalRAG", "TEST"), "for another state")):
        answer = ask(table(), "ExperimentalRAG", "OFF", origin="user",
                     approval=approval)
        check(answer.get("refused") == "NOT_APPROVED",
              "%s is refused" % label)
    # THE REPLAY THAT A CHECK AGAINST THE CURRENT STATE WOULD WAVE THROUGH.
    slip = sign("SmartDimensioning", "ON")
    on = ask(table(), "SmartDimensioning", "ON", origin="user", approval=slip)
    check(on["changed"] is True, "ajmal moves OFF -> ON once")
    off = ask(on["flags"], "SmartDimensioning", "OFF", origin="user",
              approval=sign("SmartDimensioning", "OFF", by="sam", at="T2"))
    check(off["changed"] is True, "sam moves it back ON -> OFF")
    again = ask(off["flags"], "SmartDimensioning", "ON", origin="user",
                approval=slip)
    check(again.get("refused") == "APPROVAL_ALREADY_USED",
          "and ajmal's first signature, resent, is refused as a replay")
    check("already recorded" in again["why"] and "ON" in again["why"],
          "naming the change it was already spent on")
    check(len(off["flags"]["SmartDimensioning"]["changes"]) == 2,
          "because every change is recorded, not only the last")
    check(FLG.last_change(off["flags"]["SmartDimensioning"])["by"] == "sam",
          "and last_change() derives the convenient reading from that list")
    check(sorted(off["flags"]["SmartDimensioning"]) == ["changes", "state"],
          "and the recorded flag holds ONLY the state and that list - who "
          "moved it and when is not copied beside it, so there is nothing "
          "for the list to disagree with")
    # AND NOTHING IS SPENT ON A CHANGE THAT CHANGES NOTHING.
    same = FLG.set_flag(table(), "ExperimentalRAG", "ON", origin="user")
    check(same.get("changed") is False and not same.get("refused"),
          "setting a flag to the state it is already in changes nothing...")
    check("nothing is recorded" in same["why"],
          "...and spends no approval and writes no audit line")

    print()
    print("6. Nothing is stored and nothing is run")
    before = table()
    kept = copy.deepcopy(before)
    answer = FLG.set_flag(before, "SmartDimensioning", "TEST", origin="user",
                          approval=sign("SmartDimensioning", "TEST"))
    check(before == kept, "the table handed in is not modified")
    check(answer["flags"] is not before
          and answer["flags"]["SmartDimensioning"]["state"] == "TEST",
          "and the change is in the new one")
    for word in ("subprocess", "os.system", "exec(", "eval(", "open(",
                 "os.environ", "import requests", "urllib", "socket"):
        check(word not in source, "the source has no %s" % word)

    print()
    print("7. A flag nobody declared fails closed even for a careless caller")
    answer = reading(table(), "SmartDimensionning")
    check(answer.get("refused") == "FLAG_NOT_DECLARED",
          "a typo is refused rather than answered silently")
    check(answer["state"] == "OFF" and answer["runs"] is False,
          "AND reads OFF, so a caller that ignores the refusal still fails "
          "closed")
    check("nothing ever says why" in answer["why"],
          "with the reason a silent OFF would be worse")
    answer = ask(table(), "SmartDimensionning", "ON", origin="user",
                 approval=sign("SmartDimensionning", "ON"))
    check(answer.get("refused") == "FLAG_NOT_DECLARED",
          "and setting it is refused rather than creating it")
    check("stays off forever" in answer["why"],
          "naming what a flag created by a typo actually costs")
    answer = reading({"X": {"state": "MAYBE"}}, "X")
    check(answer.get("refused") == "NOT_A_FLAG_STATE"
          and answer["runs"] is False,
          "a declared flag holding a word nobody knows also reads OFF")
    check(reading(None, "X").get("refused") == "NO_FLAGS",
          "and no table at all is refused, still reading OFF")
    check(FLG.survey({}).get("refused") == "NO_FLAGS",
          "an empty survey is refused - a system with nothing behind a flag "
          "is not the same as a system nobody asked about")
    check(FLG.survey(dict(table(), Broken={"state": "?"}))["unreadable"],
          "and a survey lists the unreadable ones rather than dropping them")

    print()
    print("8. Every failure the contract declares is named and reached")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-OPS-FLG-009.yaml"))
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
    print("PASS    three states, and only a person may move one")
    return 0


if __name__ == "__main__":
    sys.exit(main())
