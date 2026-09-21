# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-SHD-012
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Shadow Execution - a disagreement log, never an agreement rate.

    python tests/test_shadow.py

WHAT IT PROVES
  1. THERE IS NO PERCENTAGE ANYWHERE, and no field that could become one.
     D-39: a rate invites a threshold, and a threshold is a number
     somebody invents and a later session tunes. The counts are arithmetic
     on recorded runs and nothing divides one by another.

  2. A HUNDRED AGREEMENTS ARE NOT A PASS. An unbroken run of agreement is
     a FINDING, and the refusal names the three things it could be.

  3. "IT ALWAYS MATCHED" IS REFUSED BY NAME - agreement is the observation
     being explained, so it cannot be the explanation.

  4. ONE DISAGREEMENT, EXAMINED, IS ENOUGH - and an unexamined one is not,
     naming which of D-39's three sentences is missing.

  5. THE MACHINE NEVER PICKS A WINNER. `compare` records what differed and
     refuses to say which was right, because that is the sentence a person
     writes.

  6. THE TABLE'S ROWS COMPOSE. A T3 MODIFY candidate gets both rules; a
     risk outside Q-29's table is refused rather than defaulted.

  7. A SIGNATURE IS REQUIRED EITHER WAY - Golden Rule 7, evidence plus a
     name.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_shadow as SHD                                     # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

SIGNED = {"by": "ajmal", "at": "2026-09-14T12:00Z"}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_shadow.py"),
                  encoding="utf-8").read()

    def ask(log, **kw):
        answer = SHD.verdict(log, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    def rules(**kw):
        answer = SHD.constraints(**kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    agreed = [SHD.compare({"count": 247}, {"count": 247}) for _ in range(199)]
    explained = SHD.compare({"count": 247}, {"count": 261},
                            on="align-mep-elevation")
    explained.update({"right": "production",
                      "explanation": "the candidate re-read after the "
                                     "user's edit and counted 14 ducts a "
                                     "link had reloaded"})

    print("1. There is no percentage anywhere")
    answer = ask(agreed + [explained], signature=SIGNED)
    check(answer["promote"] is True, "a signed, examined log promotes")
    for field in sorted(answer):
        value = answer[field]
        check(not isinstance(value, float),
              "'%s' is not a fraction" % field)
    check(answer["agreements"] == 199 and answer["analysed"] == 1,
          "the two counts are the recorded runs, not a ratio of them")
    for word in ("%.1f", "/ len(", "/ float", "* 100", "round("):
        check(word not in source,
              "the source computes no %s - there is no division in it" % word)
    # NOT a word search: "rate" and "percentage" appear in this module, and
    # the claim is that every mention is the decision refusing one.
    import re
    # Newlines flattened first: a sentence wrapped across two lines is
    # still one sentence, and a window that stops at the line end would
    # read half of it and call the other half missing.
    flat = " ".join(source.split())
    for word in ("rate", "percentage", "score"):
        mentions = re.findall(r".{0,70}\b" + word + r"\b.{0,70}", flat)
        check(mentions, "the source does mention a %s, to refuse it" % word)
        refused = [m for m in mentions
                   if any(w in m.lower() for w in
                          ("no ", "nothing", "not ", "never", "refus",
                           "invites", "threshold"))]
        check(len(refused) == len(mentions),
              "and all %d mentions of '%s' refuse one, rather than "
              "computing it (%s)"
              % (len(mentions), word,
                 "; ".join(sorted(set(m.strip() for m in mentions
                                      if m not in refused))) or "none left"))
    check("threshold" in source and "D-39" in source,
          "and names the decision that refused one")
    check(any("no rate here" in note.lower() for note in answer["unjudged"]),
          "the answer itself says there is no rate in it")
    check(any("NOT evidence" in note for note in answer["unjudged"]),
          "and that the agreements are not the evidence")

    print()
    print("2. A hundred agreements are not a pass")
    answer = ask(agreed)
    check(answer.get("refused") == "NEVER_DISAGREED",
          "199 agreements and nothing else is refused")
    check(answer["promote"] is False, "and does not promote")
    for possibility in ("not running", "not seeing the same inputs", "copy"):
        check(possibility in answer["why"],
              "the refusal names '%s' as what it might be" % possibility)
    check("FINDING" in answer["why"],
          "and calls it a finding rather than a pass")
    check(ask([SHD.compare({"a": 1}, {"a": 1})]).get("refused")
          == "NEVER_DISAGREED",
          "one agreement is refused the same way as 199 - the number was "
          "never what was being read")

    print()
    print("3. 'It always matched' is refused by name")
    for excuse in ("it always matched", "They always agreed",
                   "there were no differences", "identical every time",
                   "the two never differed"):
        answer = ask(agreed, reason=excuse, signature=SIGNED)
        check(answer.get("refused") == "REASON_IS_THAT_IT_MATCHED",
              "%r is refused" % excuse)
    check("cannot also be the explanation"
          in ask(agreed, reason="it always matched")["why"],
          "and the refusal says why that is circular")
    answer = ask(agreed, reason="only view-plan cases ran; nothing "
                                "exercised a section", signature=SIGNED)
    check(answer["promote"] is True,
          "while a real reason - a case never exercised - is accepted")
    check(answer["reason"] and "section" in answer["reason"],
          "and is kept in the answer, so what it rested on is on the record")

    print()
    print("4. One disagreement, examined, is enough")
    answer = ask([explained], signature=SIGNED)
    check(answer["promote"] is True and answer["analysed"] == 1,
          "one explained difference promotes, with no reason needed")
    bare = SHD.compare({"count": 247}, {"count": 261})
    answer = ask([bare], signature=SIGNED)
    check(answer.get("refused") == "NOT_ANALYSED",
          "an unexamined difference does not")
    wants = answer["missing"][0]["wants"]
    check("which one was right" in wants,
          "and it names which sentence is missing")
    check("same evidence as no difference at all" in answer["why"],
          "saying a difference nobody explained proves nothing")
    for field in ("right", "explanation"):
        half = dict(bare)
        half[field] = "something"
        check(ask([half], signature=SIGNED).get("refused") == "NOT_ANALYSED",
              "half the analysis - only %s - is still refused" % field)
    # THE KEY COLLISION THAT WOULD HAVE MADE THIS GATE SOFT.
    check("why" in bare and "explanation" not in bare,
          "compare() writes its own `why` about what it SAW...")
    check(ask([dict(bare, right="production")],
              signature=SIGNED).get("refused") == "NOT_ANALYSED",
          "...and that does not count as the person's explanation, so a "
          "raw comparison is never two-thirds examined the moment it is made")
    check(ask([], signature=SIGNED).get("refused") == "NO_RUNS",
          "and an empty log is refused, not read as a clean record")
    check("what that looks like from the outside" in ask([])["why"],
          "because nothing ran looks exactly like nothing disagreed")

    print()
    print("5. The machine never picks a winner")
    entry = SHD.compare({"count": 247, "kind": "duct"},
                        {"count": 261, "kind": "duct"})
    check(entry["agreed"] is False and entry["differed"] == ["count"],
          "it records exactly what differed")
    check("kind" not in entry["differed"],
          "and not what did not")
    check("right" not in entry,
          "it does not decide which was right")
    check("WHICH ONE WAS RIGHT is not recorded here" in entry["why"],
          "and says that is a person's sentence")
    same = SHD.compare({"a": 1}, {"a": 1})
    check(same["agreed"] is True and same["differed"] == [],
          "an agreement records nothing differing")
    for one, other in (({"a": 1}, None), (None, {"a": 1}), (None, None),
                       ("result", {"a": 1}), ({"a": 1}, 42)):
        answer = SHD.compare(one, other)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "NOT_A_RUN_PAIR",
              "one side missing is refused, not read as agreement")
    check("not a quiet agreement" in SHD.compare(None, None)["why"],
          "and says so in those words")
    check(ask(["not an entry"]).get("refused") == "NOT_A_RUN_PAIR",
          "a log entry this agent did not write is refused too")

    print()
    print("6. The table's rows compose")
    answer = rules(tier="T3", risk="MODIFY")
    check(len(answer["constraints"]) == 2,
          "a T3 MODIFY candidate gets both rules")
    came_from = sorted(rule["from"] for rule in answer["constraints"])
    check(came_from == ["risk MODIFY", "tier T3"],
          "one from each axis, each saying where it came from")
    check(any("never open a transaction" in rule["rule"]
              for rule in answer["constraints"]),
          "including preview only, never a transaction")
    check(any("NEVER promote" in rule["rule"]
              for rule in answer["constraints"]),
          "and never promoted from a shadow run")
    check(len(rules(tier="T1", risk="READ")["constraints"]) == 1,
          "while a T1 READ candidate gets the one that applies")
    for risk in SHD.NOT_IN_THE_TABLE:
        answer = rules(tier="T1", risk=risk)
        check(answer.get("refused") == "SHADOW_BEHAVIOUR_UNDEFINED",
              "%s is not in Q-29's table and is refused, not defaulted"
              % risk)
        check("is a decision, not a gap to fill in" in answer["why"],
              "and says extending the table is somebody's decision")
    for tier, risk in ((None, None), ("T4", "READ"), ("", ""),
                       ("tier one", "reading")):
        check(rules(tier=tier, risk=risk).get("refused")
              == "SHADOW_BEHAVIOUR_UNDEFINED",
              "%r / %r matches no row and is refused" % (tier, risk))
    answer = rules(tier="T1", risk="READ")
    check(any("D-84" in note for note in answer["unjudged"]),
          "and the answer says nothing here ENFORCES the constraints, "
          "citing the decision that settled it rather than a closed "
          "question called open (row 5b-56)")
    check(any("not this agent's to give" in note
              for note in answer["unjudged"]),
          "naming the register's 'guarantees' as a claim it cannot make")
    for word in ("subprocess", "os.system", "exec(", "eval(", "open(",
                 "threading", "multiprocessing"):
        check(word not in source,
              "the source has no %s - it executes neither side" % word)

    print()
    print("7. A signature is required either way")
    check(ask([explained]).get("refused") == "NOT_SIGNED",
          "an examined disagreement still needs a name")
    check(ask(agreed, reason="too few runs").get("refused") == "NOT_SIGNED",
          "and so does a stated reason there was none")
    for signature in ({"at": "T"}, {"by": "ajmal"}, {}, "ajmal", None):
        check(ask([explained], signature=signature).get("refused")
              == "NOT_SIGNED", "%r is not a signature" % (signature,))
    check("Golden Rule 7" in ask([explained])["why"],
          "citing the rule that no agent approves itself")

    print()
    print("8. Every failure the contract declares is named and reached")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-OPS-SHD-012.yaml"))
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
    print("PASS    a disagreement log, never an agreement rate")
    return 0


if __name__ == "__main__":
    sys.exit(main())
