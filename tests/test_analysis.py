# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-LRN-ANA-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Learning analysis - frequency and corroboration are two things.

    python tests/test_analysis.py

WHAT IT PROVES
  1. THE ROW NAMES BOTH WORDS, read out of docs/28.

  2. FREQUENT IS NOT CORROBORATED. Ten times in one project by one
     person is its own verdict, and it is the dangerous one.

  3. ONE SIGHTING IS A ONE-OFF, however new it is called.

  4. NOVELTY IS ECHOED AND IGNORED, not silently dropped.

  5. THERE IS NO "ENOUGH" NUMBER - three sightings and thirty give the
     same verdict when the sources are the same.

  6. NOTHING IS EXTRACTED OR PROMOTED.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_analysis as ANA                                   # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_analysis.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(observations):
        answer = ANA.analyse(observations)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    def seen(pattern="p", project="Tower A", by="Ajmal", workflow="w", **kw):
        base = {"pattern": pattern, "project": project, "by": by,
                "workflow": workflow}
        base.update(kw)
        return base

    def verdict(answer, pattern="p"):
        return [one for one in answer["patterns"]
                if one["pattern"] == pattern][0]["verdict"]

    print("1. The row names both words")
    row = [line for line in io.open(
        os.path.join(ROOT, "docs", "28-agent-registry.md"),
        encoding="utf-8").read().splitlines() if "LRN-ANA-002" in line][0]
    check("Frequency and corroboration, not novelty" in row,
          "docs/28: frequency and corroboration, not novelty")
    check("one-off" in row, "and asks whether it is a one-off")
    check(ANA.VERDICTS == ("corroborated", "frequent_but_uncorroborated",
                           "a_one_off"),
          "the agent's three verdicts follow from that")

    print("\n2. Frequent is not corroborated")
    same = ask([seen() for _ in range(10)])
    check(verdict(same) == "frequent_but_uncorroborated",
          "ten sightings, one project, one person, one workflow")
    check(same["patterns"][0]["seen"] == 10, "the count is reported: 10")
    check(same["patterns"][0]["corroborated_by"] == [],
          "and nothing corroborates it")
    check("one situation repeated" in same["patterns"][0]["why"],
          "the reason says the count looks like proof and the proof is one "
          "situation repeated")
    across = ask([seen(project="Tower A"), seen(project="Tower B")])
    check(verdict(across) == "corroborated",
          "two sightings in two projects ARE corroborated")
    check(across["patterns"][0]["corroborated_by"] == ["project"],
          "and the answer says which kind: %s"
          % across["patterns"][0]["corroborated_by"])
    for source in ANA.SOURCES:
        two = ask([seen(**{source: "one"}), seen(**{source: "two"})])
        check(verdict(two) == "corroborated",
              "a different %s corroborates" % source)
    check(ask([seen(), seen()])["patterns"][0]["verdict"]
          == "frequent_but_uncorroborated",
          "while two identical sightings do not")

    print("\n3. One sighting is a one-off")
    once = ask([seen()])
    check(verdict(once) == "a_one_off", "seen once")
    check("however new it looks" in once["patterns"][0]["why"],
          "and it stays one however new it looks")
    novel = ask([seen(note="never seen this before, very unusual")])
    check(verdict(novel) == "a_one_off",
          "calling it new does not change the verdict")
    wide = ask([seen(project="A"), seen(project="B"), seen(project="C")])
    check(verdict(wide) == "corroborated",
          "while three sightings in three projects are corroborated")

    print("\n4. Novelty is echoed and ignored")
    for word in ANA.NOVELTY:
        answer = ask([seen(note="this is %s" % word), seen(project="B")])
        check(word in answer["patterns"][0]["claimed"],
              "'%s' is echoed back under `claimed`" % word)
    check(ask([seen(note="new"), seen(project="B")]
              )["patterns"][0]["verdict"] == "corroborated",
          "and the verdict is what the counts say, not what the note says")
    check(ask([seen(note="new")])["patterns"][0]["verdict"] == "a_one_off",
          "in both directions")
    check(any("changed nothing" in line.lower()
              for line in ask([seen(note="new")])["unjudged"]),
          "the answer says the claim changed nothing")
    # Asserted on the rendered line, not the source: adjacent string
    # literals are joined by quotes in the file, so a search there finds a
    # sentence the reader never sees.
    check(any("keeps sending it" in line
              for line in ask([seen(note="new")])["unjudged"]),
          "and the answer says why it is echoed rather than dropped")
    # Read from all three fields, and asserted rather than waved at.
    for field in ("note", "claim", "why"):
        claimed = ask([seen(**{field: "unusual"})])["patterns"][0]["claimed"]
        check(claimed == ["unusual"],
              "a claim in `%s` is read: %s" % (field, claimed))
    check(ask([seen(note="ordinary enough")])["patterns"][0]["claimed"]
          == [],
          "while an ordinary note claims nothing")

    print("\n5. There is no 'enough' number")
    three = ask([seen() for _ in range(3)])
    thirty = ask([seen() for _ in range(30)])
    check(verdict(three) == verdict(thirty) == "frequent_but_uncorroborated",
          "three and thirty give the same verdict when the sources are the "
          "same - more is not better evidence, it is more of the same")
    check(three["patterns"][0]["seen"] == 3
          and thirty["patterns"][0]["seen"] == 30,
          "and the counts differ, so a reader can see the difference")
    for number in ("== 3", ">= 3", "> 2", "* 0.", "threshold", "enough ="):
        check(number not in logic, "no tuned number in the agent (%s)"
              % number)
    check("< 2" in logic,
          "the only boundary is at more than one, written once")
    check(any("no 'enough' number anywhere" in line.lower()
              for line in three["unjudged"]),
          "and the answer says so")

    print("\n6. Nothing is extracted or promoted")
    # STRUCTURAL, not a word search: the agent's own sentences say it does
    # not extract or promote, so those words are in the file on purpose.
    public = sorted(line.split("(")[0][4:] for line in logic.split("\n")
                    if line.startswith("def ") and not line.startswith(
                        "def _"))
    check(public == ["analyse"],
          "the agent defines exactly one public function: %s"
          % ", ".join(public))
    check(sorted(three) == ["analysed", "counts", "of", "patterns",
                            "unjudged", "why"],
          "and its answer has six keys, none of which is a candidate or a "
          "promotion: %s" % ", ".join(sorted(three)))
    for acting in ("write(", "open(", "subprocess", "os.remove"):
        check(acting not in logic, "it never uses %s" % acting)
    check("HERON-LRN-EXT-003" in whole and "HERON-LRN-PRO-004" in whole,
          "and names the agents whose jobs those are")
    check(any("one success is still not proof" in line.lower()
              for line in three["unjudged"]),
          "including that one success is still not proof")

    print("\n7. Every failure is named and reached")
    check(ask(None).get("refused") == "NOTHING_TO_ANALYSE",
          "nothing handed in is refused")
    check(ask([]).get("refused") == "NOTHING_TO_ANALYSE",
          "and so is empty - deciding nothing is a pattern after seeing "
          "nothing is not the same answer")
    for bad, why in (("not a map", "an observation that is not a map"),
                     ({"project": "A"}, "one with no pattern"),
                     ({"pattern": "  "}, "and one whose pattern is blank")):
        check(ask([bad]).get("refused") == "NOT_AN_OBSERVATION", why)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-LRN-ANA-002.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 2, "the contract declares 2 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(three["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    ten times in one place is not evidence")
    return 0


if __name__ == "__main__":
    sys.exit(main())
