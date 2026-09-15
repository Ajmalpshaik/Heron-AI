# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-SPL-003
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Splitting - two consumers is a count, and ten short fragments is not.

    python tests/test_split.py

WHAT IT PROVES
  1. THE BAR IS docs/09 s120's, read out of the document - "at least two
     distinct consumers, actual or clearly imminent", and "reuse
     justifies a split; tidiness does not".

  2. DISTINCT MEANS DISTINCT. The same consumer named twice for one part
     counts once, which is the whole difference between a rule and a
     formality.

  3. ACTUAL AND IMMINENT COUNT TOGETHER AND REPORT APART - together
     because docs/09 counts them together, apart because they are not
     the same evidence.

  4. AN IMMINENT CONSUMER WITH NO REASON IS REFUSED, and the answer asks
     what makes it imminent.

  5. OVER-DECOMPOSITION IS NOT TURNED INTO A NUMBER. Twenty parts each
     with two consumers pass, and the answer says whose judgement the
     rest is.

  6. A ONE-PART SPLIT IS A RENAME, and is refused as one.

  7. NOTHING IS SPLIT, and a fragment at PRODUCTION is marked as one
     nobody may apply this to.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_split as SPL                                      # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

PARTS = [{"name": "filter-by-category", "does": "narrows to a category"},
         {"name": "count-them", "does": "counts what it is given"}]


def consumers(*rows):
    return [dict(row) for row in rows]


TWO_EACH = consumers(
    {"part": "filter-by-category", "name": "count-elements"},
    {"part": "filter-by-category", "name": "select-elements"},
    {"part": "count-them", "name": "count-elements"},
    {"part": "count-them", "name": "size-breakdown"})


def frag(**changes):
    card = {"id": "FRG-T-001", "heron-status": "PROVEN"}
    card.update(changes)
    return card


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_split.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]
    doc = " ".join(io.open(
        os.path.join(ROOT, "docs", "09-skills-and-fragments.md"),
        encoding="utf-8").read().replace("*", "").split())

    print("\n1. the bar is docs/09 s120's")
    check("at least two distinct consumers, actual or clearly imminent"
          in doc, "docs/09 carries the guard verbatim")
    check("Reuse justifies a split; tidiness does not." in doc,
          "and the sentence that explains it")
    check("over-decomposition" in doc and "nine layers of indirection"
          in doc, "and the failure mode it is guarding against")
    check(SPL.ENOUGH == 2, "the module's bar is two")
    check(SPL.KINDS == ("actual", "imminent"),
          "and the two kinds it counts are docs/09's two")

    print("\n2. distinct means distinct")
    good = SPL.propose(frag(), PARTS, TWO_EACH)
    check(good["proposed"] is True, "two consumers each is enough")
    # THE SAME CONSUMER TWICE IS ONE CONSUMER.
    twice = SPL.propose(frag(), PARTS, consumers(
        {"part": "filter-by-category", "name": "count-elements"},
        {"part": "filter-by-category", "name": "count-elements"},
        {"part": "count-them", "name": "count-elements"},
        {"part": "count-them", "name": "size-breakdown"}))
    reached.add(twice.get("refused"))
    check(twice["refused"] == "TOO_FEW_CONSUMERS",
          "the same consumer named twice for one part counts ONCE")
    check(twice["parts"][0]["part"] == "filter-by-category"
          and twice["parts"][0]["have"] == 1,
          "and the answer says it has 1, not 2")
    check(twice["need"] == 2, "against a bar of 2")
    check(good["parts"][0]["consumers"]
          == sorted(set(good["parts"][0]["consumers"])),
          "the reported consumers are a distinct, sorted set")

    print("\n3. actual and imminent count together, report apart")
    mixed = SPL.propose(frag(), PARTS, consumers(
        {"part": "filter-by-category", "name": "count-elements"},
        {"part": "filter-by-category", "name": "select-elements"},
        {"part": "count-them", "name": "count-elements"},
        {"part": "count-them", "name": "size-breakdown",
         "kind": "imminent",
         "because": "its card already names COUNT_ELEMENTS in `needs`"}))
    check(mixed["proposed"] is True,
          "one actual and one imminent clears the bar - they count "
          "together, as docs/09 counts them")
    part = [one for one in mixed["parts"] if one["name"] == "count-them"][0]
    check(part["actual"] == ["count-elements"]
          and [one["name"] for one in part["imminent"]] == ["size-breakdown"],
          "and they are reported apart, because they are not the same "
          "evidence")
    check(part["imminent"][0]["because"],
          "the reason is carried back: %r" % part["imminent"][0]["because"])
    check(any("disbelieve" in line for line in mixed["unjudged"]),
          "for somebody to disbelieve - nothing here checked it")
    check(any("every consumer counted here is an ACTUAL one"
              in line for line in good["unjudged"]),
          "and with no imminent ones, the answer says nothing rests on a "
          "claim about the future")

    print("\n4. an imminent consumer with no reason is refused")
    hoped = SPL.propose(frag(), PARTS, consumers(
        {"part": "filter-by-category", "name": "count-elements"},
        {"part": "filter-by-category", "name": "select-elements"},
        {"part": "count-them", "name": "count-elements"},
        {"part": "count-them", "name": "maybe-later", "kind": "imminent"}))
    reached.add(hoped.get("refused"))
    check(hoped["refused"] == "NOT_IMMINENT",
          "a claim with nothing behind it is refused")
    check(hoped.get("asked") and "maybe-later" in hoped["asked"],
          "and the answer asks what makes it imminent: %r" % hoped["asked"])
    check("one consumer and an intention" in hoped["why"],
          "with the reason: it turns two consumers into one consumer and "
          "an intention")

    print("\n5. over-decomposition is not turned into a number")
    # TWENTY PARTS, each with two consumers. docs/09 warns about this
    # shape and gives no number, so none is invented.
    many = [{"name": "part-%02d" % i, "does": "does a little"}
            for i in range(20)]
    crowd = []
    for one in many:
        crowd.append({"part": one["name"], "name": "user-a"})
        crowd.append({"part": one["name"], "name": "user-b"})
    lots = SPL.propose(frag(), many, crowd)
    check(lots["proposed"] is True and lots["of"] == 20,
          "twenty parts each clearing the bar are proposed")
    check(any("WARNING rather than a rule" in line
              for line in lots["unjudged"]),
          "and the answer says over-decomposition is a warning, not a rule")
    check(any("theirs" in line for line in lots["unjudged"]),
          "leaving whether it is readable to a person")
    # AND THE COUNT CHANGES NOTHING. A word search would only find the
    # module quoting docs/09's own warning, so the proof is behavioural.
    for how_many in (2, 3, 7, 20, 50):
        these = [{"name": "part-%02d" % i, "does": "does a little"}
                 for i in range(how_many)]
        pair = []
        for one in these:
            pair.append({"part": one["name"], "name": "user-a"})
            pair.append({"part": one["name"], "name": "user-b"})
        answer = SPL.propose(frag(), these, pair)
        check(answer["proposed"] is True and answer["of"] == how_many,
              "%d parts, each with two consumers, are proposed" % how_many)

    print("\n6. a one-part split is a rename")
    one = SPL.propose(frag(), PARTS[:1], TWO_EACH)
    reached.add(one.get("refused"))
    check(one["refused"] == "ONE_PART", "one part is refused")
    check("HERON-NAM-REN-003" in one["why"],
          "naming the agent that does renames")
    check("with nothing checking the move" in one["why"],
          "and why accepting it here would be a hole")

    print("\n7. nothing is split, and PRODUCTION is marked")
    check(good["split"] is False,
          "`split` is false even on the good path - it is always false")
    check(good["may_be_applied"] is True,
          "a PROVEN fragment may be applied to by somebody else")
    live = SPL.propose(frag(**{"heron-status": "PRODUCTION"}), PARTS,
                       TWO_EACH)
    check(live["proposed"] is True and live["may_be_applied"] is False,
          "a PRODUCTION one is still PROPOSED - and marked as one nobody "
          "may apply this to")
    check("docs/09 s118" in live["unjudged"][0]
          and "PRODUCTION" in live["why"],
          "citing docs/09 s118 in both the answer and the unjudged line")
    for writing in ("open(", "write(", "makedirs", "subprocess"):
        check(writing not in logic, "the agent never uses %s" % writing)

    print("\n8. every declared failure is named and reached")
    for fragment, parts, these, name in (
            (None, PARTS, TWO_EACH, "NOTHING_TO_SPLIT"),
            (frag(), [], TWO_EACH, "NOTHING_TO_SPLIT"),
            ("a string", PARTS, TWO_EACH, "NOT_A_FRAGMENT"),
            ({"id": " "}, PARTS, TWO_EACH, "NOT_A_FRAGMENT"),
            (frag(), ["a string", "another"], TWO_EACH, "NOT_A_PART"),
            (frag(), [{"name": "x"}, {"name": "y", "does": "z"}],
             TWO_EACH, "NOT_A_PART"),
            (frag(), PARTS, consumers({"part": "count-them", "name": "x",
                                       "kind": "hoped-for"}),
             "NOT_A_CONSUMER"),
            (frag(), PARTS, consumers({"part": "nowhere", "name": "x"}),
             "NOT_A_CONSUMER"),
            (frag(), PARTS, ["a string"], "NOT_A_CONSUMER")):
        answer = SPL.propose(fragment, parts, these)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-FRG-SPL-003.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 7, "the contract declares 7 failures")
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
    print("PASS    reuse justifies a split, tidiness does not")
    return 0


if __name__ == "__main__":
    sys.exit(main())
