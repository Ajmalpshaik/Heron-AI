# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-NAM-KEY-005
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Keywords - recorded by a person, looked up, never inferred.

    python tests/test_keywords.py

WHAT IT PROVES
  1. THE CONTRADICTION IS REAL, read out of both documents: docs/28 asks
     this agent for synonyms and D-34 says Heron builds no synonym table.

  2. IT SHIPS NO TABLE, proved by BEHAVIOUR rather than by a word
     search: with nothing handed in, every word comes back unknown.

  3. AN INFERENCE IS NOT A RECORD - every word that says a machine made
     it is refused, and a refused entry does not become a hit.

  4. AN UNKNOWN WORD IS A QUESTION, not a guess and not an expansion -
     D-33, with the question in the answer.

  5. TWO PEOPLE WHO DISAGREE BOTH COME BACK. The disagreement is the
     useful part and is not merged away.

  6. WHAT WAS REFUSED IS CARRIED, not dropped - Golden Rule 14.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND
     REACHED, and the code refuses nothing the contract omits.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_keywords as KEY                                   # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def decision_log():
    """DECISIONS.md and every record under docs/decisions/. Since 2026-09-23
    each decision's full text is its own file and DECISIONS.md is the index
    (tools/split-decisions.py), so what a decision says is in one of them."""
    paths = [os.path.join(ROOT, "docs", "DECISIONS.md")]
    folder = os.path.join(ROOT, "docs", "decisions")
    if os.path.isdir(folder):
        paths += [os.path.join(folder, n) for n in sorted(os.listdir(folder)) if n.endswith(".md")]
    return " ".join(io.open(p, encoding="utf-8").read() for p in paths)


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_keywords.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]

    def ask(term, **kw):
        answer = KEY.look_up(term, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        for entry in (answer.get("rejected") or []):
            reached.add(entry["refused"])
        return answer

    print("1. The contradiction is real, read out of both documents")
    register = io.open(os.path.join(ROOT, "docs", "28-agent-registry.md"),
                       encoding="utf-8").read()
    row = [line for line in register.splitlines() if "NAM-KEY-005" in line]
    check(len(row) == 1 and "synonyms" in row[0].lower(),
          "docs/28 gives this agent search terms and synonyms")
    decisions = decision_log()
    flat = " ".join(decisions.split())
    check("No phrase list, no synonym table" in flat,
          "and D-34 says: no phrase list, no synonym table")
    check("Heron builds nothing to understand language" in flat,
          "because Heron builds nothing to understand language")
    # AND THE READING THAT SETTLES IT, also D-34's own.
    check("that is knowledge" in flat and "looked up and corrected" in flat,
          "while D-34 itself says a word the model calls something else is "
          "KNOWLEDGE, to be looked up and corrected")
    check("D-34" in whole and "F16" in whole,
          "the agent cites both the decision and the finding")

    print("\n2. It ships no table - proved by behaviour")
    for word in ("duct", "ductwork", "riser", "shaft", "supply air",
                 "a", "THE"):
        answer = ask(word)
        check(answer.get("refused") == "NOTHING_RECORDED",
              "with nothing handed in, '%s' is unknown" % word)
    check(ask("duct")["held"] == 0,
          "and the answer says zero terms are held, so an empty result can "
          "be read")
    check(ask("duct", recorded=[])["refused"] == "NOTHING_RECORDED",
          "an explicitly empty store answers the same way")
    # No module-level vocabulary at all.
    check(code.count("= (") + code.count("= {") == 2,
          "the module defines exactly two collections of its own")
    check("A_RECORD_CARRIES" in code and "NOT_RECORDED" in code,
          "and both are about PROVENANCE - what a record carries, and "
          "words that say a machine made it")

    print("\n3. An inference is not a record")
    for maker in KEY.NOT_RECORDED:
        entry = {"term": "x", "means": "y", "by": maker, "at": "2026-09-15"}
        check(KEY.check(entry).get("refused")
              == "AN_INFERENCE_IS_NOT_A_RECORD",
              "'%s' is not a person" % maker)
    check(KEY.check({"term": "x", "means": "y", "by": "the model",
                     "at": "2026-09-15"}).get("refused")
          == "AN_INFERENCE_IS_NOT_A_RECORD",
          "and neither is 'the model' - the word need not stand alone")
    # THE ONE THAT MATTERS: a refused entry must not become a hit.
    poisoned = ask("riser", recorded=[
        {"term": "riser", "means": "Vertical duct run", "by": "auto",
         "at": "2026-09-15"}])
    check(poisoned.get("refused") == "NOTHING_RECORDED",
          "a word recorded ONLY by a machine is still unknown - the "
          "refusal is not a warning beside a usable answer")
    check(poisoned["held"] == 0, "and it is not counted as held")
    check(len(poisoned["rejected"]) == 1,
          "while the entry itself is in the answer")
    person = ask("riser", recorded=[
        {"term": "riser", "means": "Vertical duct run", "by": "Ajmal",
         "at": "2026-09-15"}])
    check(person["found"] and person["means"] == ["Vertical duct run"],
          "and the same entry from a person IS found, so the difference is "
          "who recorded it and nothing else")

    print("\n4. An unknown word is a question")
    unknown = ask("shaft", recorded=[
        {"term": "riser", "means": "Vertical duct run", "by": "Ajmal",
         "at": "2026-09-15"}])
    check(unknown.get("ask") == "What does 'shaft' mean here?",
          "the answer carries the question to put")
    check("D-33" in unknown["why"],
          "and cites D-33 - Heron never assumes an input, it asks once")
    check("means" not in unknown,
          "nothing was expanded quietly - there is no partial answer "
          "beside the question")
    check(unknown["held"] == 1,
          "and the count says the store was not empty, so 'unknown' is "
          "about the word rather than about the store")
    check(ask("").get("refused") == "NO_TERM",
          "while asking about nothing is a DIFFERENT refusal - an empty "
          "term matching nothing is not a word nobody recorded")

    print("\n5. Two people who disagree both come back")
    both = ask("ductwork", recorded=[
        {"term": "ductwork", "means": "Duct", "by": "Ajmal",
         "at": "2026-09-15"},
        {"term": "ductwork", "means": "Flex Duct", "by": "Site engineer",
         "at": "2026-09-15"}])
    check(both["means"] == ["Duct", "Flex Duct"],
          "both recordings come back, in the order they were given")
    check(len(both["means"]) == 2,
          "not merged, not ranked, not picked between - the disagreement "
          "is the useful part")
    check("Ajmal" in both["why"] and "Site engineer" in both["why"],
          "and both names are in the answer, so either can be argued with")
    check("ductwork".upper() in str(ask("DUCTWORK", recorded=[
        {"term": "ductwork", "means": "Duct", "by": "Ajmal",
         "at": "2026-09-15"}])["term"]),
        "a lookup is case-insensitive on the word asked")

    print("\n6. What was refused is carried, not dropped")
    messy = ask("ductwork", recorded=[
        {"term": "ductwork", "means": "Duct", "by": "Ajmal",
         "at": "2026-09-15"},
        {"term": "sleeve", "means": "Penetration", "by": "Ajmal"},
        {"term": "riser", "means": "x", "by": "inferred", "at": "2026-09-15"},
        "not a map at all"])
    check(messy["found"] and len(messy["rejected"]) == 3,
          "the good one is found and all three bad ones are reported")
    check(sorted(set(row["refused"] for row in messy["rejected"]))
          == ["AN_INFERENCE_IS_NOT_A_RECORD", "NOT_A_RECORD"],
          "with the right refusal on each")
    check(any("Golden Rule 14" in line for line in messy["unjudged"]),
          "and the answer says why they are carried - Golden Rule 14")
    check(messy["held"] == 1, "one record was held out of four entries")

    print("\n7. Every failure is named and reached")
    for bad, why in ((None, "None is not a record"),
                     ({"means": "y", "by": "A", "at": "d"}, "no term"),
                     ({"term": "x", "by": "A", "at": "d"}, "no meaning"),
                     ({"term": "x", "means": "y", "at": "d"}, "nobody"),
                     ({"term": "x", "means": "y", "by": "A"}, "and no date")):
        got = KEY.check(bad)
        reached.add(got["refused"])
        check(got.get("refused") == "NOT_A_RECORD", why)
    check(all(field in code for field, _why in KEY.A_RECORD_CARRIES),
          "the four fields a record carries are named in the code")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-NAM-KEY-005.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 4, "the contract declares 4 failures")
    for failure in named:
        check(failure in code, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    spare = sorted(reached - set(named))
    check(not spare,
          "and the code refuses nothing the contract omits%s"
          % ("" if not spare else ": %s" % ", ".join(spare)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    knowledge, not language - and it ships no table")
    return 0


if __name__ == "__main__":
    sys.exit(main())
