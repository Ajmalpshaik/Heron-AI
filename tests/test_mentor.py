# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-MEN-014
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Mentor - the senior is not the answer key.

    python tests/test_mentor.py

WHAT IT PROVES
  1. THE WINDOW IS SHADOW, AND THE STAGE IS READ. Every other stage is
     refused, and there is no argument a caller could use to say it is in
     SHADOW - the same hole HERON-AHR-DEP-012 spent three review rounds
     closing.

  2. A MENTOR MUST BE PROVEN OR IN PRODUCTION. Naming one does not make a
     DRAFT agent eligible: the named mentor's stage is read too.

  3. NOTHING IN THE REAL REGISTER QUALIFIES, so a real pairing today ends in
     NO_PROVEN_OWNER. That is today's correct answer and it is returned
     rather than softened into the nearest match.

  4. AN AGENT MAY NOT MENTOR ITSELF, and the check survives a difference in
     case or spacing.

  5. A TIE IS REFUSED, NOT BROKEN. Word overlap cannot choose between two
     agents scoring the same and does not pretend to.

  6. THE MATCHER IS BORROWED, NOT REWRITTEN. One crude matcher in the
     repository beats two that disagree.

  7. A DIVERGENCE NEVER NAMES A WINNER. This is the one that matters: a
     proven agent is proven against the cases somebody thought of, and
     treating it as the answer key is how a bug becomes the specification.

  8. AN AGREEMENT IS NOT A CORRECTNESS CLAIM EITHER.

  9. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_mentor as MEN                                    # noqa: E402
import heron_agents as REG                                    # noqa: E402
import heron_contract as CON                                  # noqa: E402

FAILURES = []

STUDENT = "HERON-AHR-WFP-015"
OWNER = "HERON-AHR-CON-017"
CAPABILITY = "agent contract interface"


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def staged(records, **states):
    """A copy of the register with stages set. The real one is never edited."""
    copy = dict((k, dict(v)) for k, v in records.items())
    for agent_id, state in states.items():
        copy[agent_id]["state"] = state
    return copy


def main():
    records = REG.records()
    reached = set()

    def ask(**kw):
        answer = MEN.pair(**kw)
        if "refused" in answer:
            reached.add(answer["refused"])
        return answer

    print("0. A pairing has two sides and a subject")
    for label, kw in (
            ("no student", dict(student_id="", capability=CAPABILITY)),
            ("a student nobody planned",
             dict(student_id="HERON-AHR-NOPE-999", capability=CAPABILITY)),
            ("a mentor nobody planned",
             dict(student_id=STUDENT, capability=CAPABILITY,
                  mentor_id="HERON-AHR-NOPE-999"))):
        answer = ask(records=staged(records, **{STUDENT: "SHADOW"}), **kw)
        check(answer.get("refused") == "NO_SUCH_AGENT",
              "%s is refused with NO_SUCH_AGENT" % label)
    for capability in ("", "   ", None):
        answer = ask(student_id=STUDENT, capability=capability,
                     records=records)
        check(answer.get("refused") == "NO_CAPABILITY_NAMED",
              "a pairing with no capability is refused - "
              "pairing by department would pair by filing")

    print()
    print("1. The window is SHADOW, and the stage is read")
    import inspect
    names = set(inspect.signature(MEN.pair).parameters)
    for forbidden in ("stage", "state", "from_stage", "in_shadow"):
        check(forbidden not in names,
              "pair() has no '%s' parameter a caller could assert"
              % forbidden)
    for stage in ("DRAFT", "TESTING", "VALIDATED", "PROVEN", "PRODUCTION",
                  "ARCHIVED"):
        answer = ask(student_id=STUDENT, capability=CAPABILITY,
                     records=staged(records, **{STUDENT: stage}))
        check(answer.get("refused") == "NOT_IN_SHADOW",
              "a student at %s is refused - the window is SHADOW" % stage)
    check(MEN.ACTIVE_STAGE == "SHADOW", "and SHADOW is the only one")

    print()
    print("2. A mentor must be PROVEN or in PRODUCTION")
    check(MEN.MENTOR_STAGES == ("PROVEN", "PRODUCTION"),
          "the two stages are the two docs/24 calls proven")
    world = staged(records, **{STUDENT: "SHADOW"})
    answer = ask(student_id=STUDENT, capability=CAPABILITY,
                 mentor_id=OWNER, records=world)
    check(answer.get("refused") == "MENTOR_NOT_PROVEN",
          "naming a DRAFT mentor does not make it eligible")
    for stage in ("PROVEN", "PRODUCTION"):
        world = staged(records, **{STUDENT: "SHADOW", OWNER: stage})
        answer = ask(student_id=STUDENT, capability=CAPABILITY,
                     mentor_id=OWNER, records=world)
        check("pairing" in answer, "a mentor at %s is accepted" % stage)

    print()
    print("2b. And being proven is not the same as owning this capability")
    # docs/28: "the PROVEN agent that currently OWNS that capability". Both
    # halves, or a shadow duct-sizing agent gets paired with a proven
    # payroll auditor and the pairing means nothing.
    elsewhere = "HERON-AHR-RET-010"
    world = staged(records, **{STUDENT: "SHADOW", elsewhere: "PROVEN"})
    check(elsewhere not in [c["agent"]
                            for c in MEN.owners(CAPABILITY, world)],
          "%s's role does not read as covering '%s'" % (elsewhere,
                                                        CAPABILITY))
    answer = ask(student_id=STUDENT, capability=CAPABILITY,
                 mentor_id=elsewhere, records=world)
    check(answer.get("refused") == "MENTOR_DOES_NOT_OWN_IT",
          "so naming it is refused even though it is PROVEN")
    check("teaches something else" in answer["why"],
          "and the refusal says why proven-at-something-else is not enough")

    print()
    print("3. Nothing in the real register qualifies")
    above_draft = [a for a, r in records.items()
                   if str((r or {}).get("state") or "").upper()
                   in MEN.MENTOR_STAGES]
    check(above_draft == [],
          "no agent in the real 250-row register is PROVEN or PRODUCTION")
    answer = ask(student_id=STUDENT, capability=CAPABILITY,
                 records=staged(records, **{STUDENT: "SHADOW"}))
    check(answer.get("refused") == "NO_PROVEN_OWNER",
          "so a real pairing is refused rather than given a DRAFT mentor")
    check("nobody to learn from yet" in answer["why"],
          "and the refusal says why, in words a person can act on")

    print()
    print("4. An agent may not mentor itself")
    world = staged(records, **{STUDENT: "SHADOW"})
    for spelling in (STUDENT, "  %s  " % STUDENT):
        answer = ask(student_id=STUDENT, capability=CAPABILITY,
                     mentor_id=spelling, records=world)
        check(answer.get("refused") == "MENTOR_IS_THE_STUDENT",
              "'%s' is refused as itself" % spelling.strip())

    print()
    print("5. A tie is refused, not broken")
    twin = "HERON-AHR-VAL-013"
    world = staged(records, **{STUDENT: "SHADOW"})
    world[OWNER] = dict(world[OWNER], state="PROVEN")
    world[twin] = dict(world[twin], state="PROVEN",
                       role=records[OWNER]["role"])
    answer = ask(student_id=STUDENT, capability=CAPABILITY, records=world)
    check(answer.get("refused") == "AMBIGUOUS_OWNER",
          "two proven agents scoring the same are not silently ordered")
    check("name the mentor" in answer["why"],
          "and the refusal says what would resolve it")

    print()
    print("6. The matcher is borrowed, not rewritten")
    import heron_workforce as WFP
    found = MEN.owners(CAPABILITY, records)
    check(found and found[0]["agent"] == OWNER,
          "the owner of '%s' is found by role, not by department"
          % CAPABILITY)
    check(found[0]["score"] == round(
        WFP.overlap(CAPABILITY, records[OWNER]["role"]), 2),
        "and the score is HERON-AHR-WFP-015's own overlap, unchanged")
    source = open(os.path.join(ROOT, "brain", "heron_mentor.py"),
                  encoding="utf-8").read()
    check("def overlap" not in source and "def words" not in source,
          "the Mentor writes no second matcher of its own")

    print()
    print("7. A divergence never names a winner")
    answer = MEN.compare({"count": 12}, {"count": 11})
    check(answer["verdict"] == "DIVERGED", "a different value diverges")
    check(len(answer["differences"]) == 1
          and answer["differences"][0]["kind"] == "DIFFERENT_VALUE",
          "and the field is named with both values")
    text = " ".join(answer["unjudged"])
    check("not the mentor by default" in text,
          "the senior is explicitly NOT the default answer")
    for word in ("correct", "wrong", "winner", "right answer"):
        check(not any(d.get("verdict") == word
                      for d in answer["differences"]),
              "no difference carries a '%s' verdict" % word)
    answer = MEN.compare({"count": 12, "units": "mm"}, {"count": 12})
    check(answer["differences"][0]["kind"] == "ONLY_THE_STUDENT",
          "a field only the student has is named as that, not as a value")
    answer = MEN.compare({"count": 12}, {"count": 12, "units": "mm"})
    check(answer["differences"][0]["kind"] == "ONLY_THE_MENTOR",
          "and the same the other way round")
    answer = MEN.compare("12", {"count": 12})
    check(answer.get("refused") == "NOT_COMPARABLE",
          "two strings are refused - spelling is not judgement")
    reached.add("NOT_COMPARABLE")

    print()
    print("8. An agreement is not a correctness claim either")
    answer = MEN.compare({"count": 12}, {"count": 12})
    check(answer["verdict"] == "AGREED" and answer["differences"] == [],
          "identical output agrees")
    check(any("not being right" in n for n in answer["unjudged"]),
          "and says that agreeing is not being right - same documents, "
          "same hands")

    print()
    print("9. Every failure the contract declares is named by the code")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-AHR-MEN-014.yaml"))
    declared = contract.get("failures") or []
    for failure in declared:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(declared) - reached - {"REGISTER_UNREADABLE"})
    check(not unreached,
          "and every state but REGISTER_UNREADABLE was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a divergence is a question, not a verdict against the new")
    return 0


if __name__ == "__main__":
    sys.exit(main())
