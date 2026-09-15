# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-USR-PRO-001
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
User profile - declared, never inferred, and a mode is granted.

    python tests/test_profile.py

WHAT IT PROVES
  1. D-27 REALLY DID ABOLISH PERSONA, and the three places that still
     hand work to it really do say so - read out of the documents.

  2. NOTHING ABOUT HOW TO SPEAK COMES BACK, whatever is asked. There is
     no tone, level or phrasing field at any input.

  3. A FACT IS DECLARED. Every word that says a value was worked out is
     refused, and an unstated fact is ABSENT rather than defaulted.

  4. A MODE IS GRANTED - and no mode held is NOT User Mode, which is the
     whole security point.

  5. THE SIX FACTS AND THE THREE MODES ARE docs/28's AND docs/22's.

  6. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_profile as PRO                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_profile.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(profile):
        answer = PRO.read(profile)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    said = {"value": "x", "by": "Ajmal"}
    grant = {"mode": "developer", "by": "Ajmal", "at": "2026-09-15"}

    print("1. D-27 really did abolish persona")
    decisions = " ".join(io.open(os.path.join(ROOT, "docs", "DECISIONS.md"),
                                 encoding="utf-8").read().split())
    check("Heron has one voice: plain, non-developer language, always"
          in decisions,
          "D-27, in DECISIONS.md: Heron has one voice")
    check("it does not exist as a setting" in decisions,
          "and persona does not exist as a setting")
    check("the two-persona table in" in decisions,
          "and it says it supersedes the two-persona table in docs/22")
    users = " ".join(io.open(os.path.join(ROOT, "docs",
                                          "22-users-modes-and-extensibility.md"),
                             encoding="utf-8").read().split())
    # The blunt sentence lives in docs/22's own [DECIDED - D-27] block, not
    # in DECISIONS.md. Two files, one decision, and the quotes are not
    # interchangeable.
    check("There is no persona" in users,
          "and docs/22 s2's own DECIDED block says: there is no persona")
    check("**Persona** may be inferred" in users,
          "while docs/22 s3, further down, still says persona may be "
          "inferred")
    check("talk their way into `ADMIN`" in users,
          "in the sentence about talking your way into ADMIN")
    register = io.open(os.path.join(ROOT, "docs", "28-agent-registry.md"),
                       encoding="utf-8").read()
    check("Persona reads this to choose how to speak" in register,
          "and docs/28's own row for this agent still says persona reads it")
    check("F19" in whole, "the agent records that as PROPOSALS F19")

    print("\n2. Nothing about how to speak comes back")
    for profile in ({}, {"role": said}, {"role": said, "mode": grant},
                    {"technical depth": {"value": "expert", "by": "A"}}):
        answer = ask(profile)
        for speaking in ("tone", "level", "phrasing", "voice", "persona",
                         "style", "wording", "verbosity"):
            check(speaking not in answer,
                  "  no '%s' key" % speaking if profile else
                  "no '%s' key, at any input" % speaking)
    deep = ask({"technical depth": {"value": "expert", "by": "A"}})
    check(deep["facts"]["technical depth"]["value"] == "expert",
          "'technical depth' is held as a FACT")
    check("how to speak" in " ".join(deep["unjudged"]).lower(),
          "and the answer says nothing about how to speak is here")

    print("\n3. A fact is declared, and an unstated one is absent")
    for machine in PRO.NOT_DECLARED:
        check(ask({"role": {"value": "beginner", "by": machine}}
                  ).get("refused") == "AN_INFERENCE_IS_NOT_A_FACT",
              "'%s' is not a person" % machine)
    check(ask({"role": {"value": "beginner", "by": "the model"}}
              ).get("refused") == "AN_INFERENCE_IS_NOT_A_FACT",
          "and neither is 'the model' - the word need not stand alone")
    empty = ask({})
    check(empty["facts"] == {} and sorted(empty["unstated"])
          == sorted(PRO.FACTS),
          "an empty profile states nothing and defaults nothing")
    check(not any(empty.get(name) for name in PRO.FACTS),
          "no fact appears as a key with a filled-in value")
    partial = ask({"role": said})
    check(len(partial["unstated"]) == len(PRO.FACTS) - 1,
          "one stated leaves %d unstated" % (len(PRO.FACTS) - 1))
    check(any("absent, not a default" in line for line in
              partial["unjudged"]),
          "and the answer says an unstated fact is absent, not a default")

    print("\n4. A mode is granted, and none is not User Mode")
    none = ask({"role": said})
    check(none["mode"]["mode"] is None, "with no grant, no mode is held")
    check("NOT read as User Mode" in none["mode"]["why"],
          "and the answer says it is NOT read as User Mode")
    check("user" not in str(none["mode"]["mode"] or ""),
          "the value is None rather than the least-privileged name, so "
          "nothing downstream can mistake a default for a grant")
    for mode in PRO.MODES:
        held = ask({"mode": {"mode": mode, "by": "Ajmal", "at": "d"}})
        check(held["mode"]["mode"] == mode, "'%s' is granted" % mode)
        check(held["mode"]["sees"] == PRO.MODES[mode],
              "  with what docs/22 s3 says it sees")
    for machine in ("inferred", "detected", "assumed", "auto"):
        check(ask({"mode": {"mode": "admin", "by": machine, "at": "d"}}
                  ).get("refused") == "MODE_MUST_BE_GRANTED",
              "a grant '%s' made is refused - that is how somebody talks "
              "their way into ADMIN" % machine)
    for short in ({"mode": "admin", "by": "Ajmal"},
                  {"mode": "admin", "at": "d"},
                  {"mode": "admin"}):
        answer = ask({"mode": short})
        check(answer.get("refused") == "MODE_MUST_BE_GRANTED",
              "a grant missing %s is refused"
              % ", ".join(answer.get("missing") or ["something"]))
    check(ask({"mode": "developer"}).get("refused")
          == "MODE_MUST_BE_GRANTED",
          "and a bare string is not a grant - it carries no signature")
    for wrong in ("superuser", "root", "", "USER MODE"):
        check(ask({"mode": {"mode": wrong, "by": "A", "at": "d"}}
                  ).get("refused") == "NOT_A_MODE",
              "'%s' is not one of the three" % wrong)

    print("\n5. The facts and modes are the documents'")
    row = [line for line in register.splitlines()
           if "USR-PRO-001" in line][0].lower()
    for fact in PRO.FACTS:
        check(fact in row, "docs/28's row names '%s'" % fact)
    check(len(PRO.FACTS) == 6, "six facts, as the row lists")
    check(sorted(PRO.MODES) == ["admin", "developer", "user"],
          "and three modes")
    for mode in PRO.MODES:
        check(mode.title() + " Mode" in
              io.open(os.path.join(ROOT, "docs",
                                   "22-users-modes-and-extensibility.md"),
                      encoding="utf-8").read(),
              "  docs/22 s3 names %s Mode" % mode.title())
    check("permission boundary" in users,
          "and calls them a permission boundary, not a display preference")

    print("\n6. Every failure is named and reached")
    for bad, why in ((None, "None is not a profile"),
                     ("Ajmal", "a string is not a profile"),
                     ([], "and a list is not one")):
        check(ask(bad).get("refused") == "NOT_A_PROFILE", why)
    for bad, why in (({"role": "BIM modeller"}, "a fact that is not a map"),
                     ({"role": {"by": "Ajmal"}}, "one with no value"),
                     ({"role": {"value": "  ", "by": "Ajmal"}},
                      "and one whose value is blank")):
        check(ask(bad).get("refused") == "NOT_A_FACT", why)
    for storing in ("open(", "write(", "os.remove", "sqlite", "json.dump"):
        check(storing not in logic, "the agent never uses %s" % storing)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-USR-PRO-001.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 5, "the contract declares 5 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    spare = sorted(reached - set(named))
    check(not spare, "and nothing else was refused%s"
          % ("" if not spare else ": %s" % ", ".join(spare)))
    check(len(none["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    declared, never inferred, and no mode is not User Mode")
    return 0


if __name__ == "__main__":
    sys.exit(main())
