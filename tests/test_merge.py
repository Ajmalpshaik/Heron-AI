# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-MRG-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Merging - a matching signature is a question, not an answer.

    python tests/test_merge.py

WHAT IT PROVES
  1. THE SIGNATURE IS NAMES AND TYPES. `doc:Document` and `doc:View` do
     not match, and neither do two fragments that merely take the same
     number of arguments.

  2. ORDER DOES NOT MATTER AND DUPLICATION DOES. The same contract
     written in a different order matches; one with an extra argument
     does not.

  3. SAME SHAPE, DIFFERENT CAPABILITY IS DECIDED HERE as "keep
     separate", and never handed to the host.

  4. SAME SHAPE, SAME CAPABILITY IS THE HOST'S, with the other three
     verdicts left open and no recommendation.

  5. A FRAGMENT WITH NO CONTRACT IS LISTED, never dropped.

  6. AGAINST THE REAL LIBRARY: exactly two groups share a signature, and
     BOTH are keep-separate. That is the finding this agent exists for -
     the answer people expect from a merge agent is the one it gives
     least.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_merge as MRG                                      # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def frag(who, capability, needs, provides, status="PROVEN"):
    return {"id": who, "capability": capability, "heron-status": status,
            "contract": {"needs": needs, "provides": provides}}


DOC = [{"name": "doc", "type": "Document"}]
VIEW = [{"name": "doc", "type": "View"}]
OUT = [{"name": "count", "type": "int"}]


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_merge.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. the signature is names AND types")
    same = MRG.signature(frag("a", "A", DOC, OUT))[0]
    other = MRG.signature(frag("b", "B", VIEW, OUT))[0]
    check(same != other,
          "`doc:Document` and `doc:View` are different signatures - the "
          "type is part of it")
    renamed = MRG.signature(frag("c", "C",
                                 [{"name": "document", "type": "Document"}],
                                 OUT))[0]
    check(same != renamed, "and so is the name")
    apart = MRG.look([frag("a", "A", DOC, OUT), frag("b", "B", VIEW, OUT)])
    check(apart["decided"] == [] and apart["for_the_host"] == [],
          "so two fragments differing only by a type are not a pair at all")

    print("\n2. order does not matter, an extra argument does")
    two = [{"name": "doc", "type": "Document"},
           {"name": "view", "type": "View"}]
    backwards = list(reversed(two))
    check(MRG.signature(frag("a", "A", two, OUT))[0]
          == MRG.signature(frag("b", "A", backwards, OUT))[0],
          "the same contract in a different order is the same signature")
    check(MRG.signature(frag("a", "A", two, OUT))[0]
          != MRG.signature(frag("b", "A", two + VIEW, OUT))[0],
          "and one extra argument makes it a different one")
    check(MRG.signature(frag("a", "A", DOC, OUT))[0]
          != MRG.signature(frag("b", "A", OUT, DOC))[0],
          "needs and provides are not interchangeable either")

    print("\n3. same shape, different capability is decided here")
    pair = MRG.look([frag("a", "DIMENSION_FAMILY", DOC, OUT),
                     frag("b", "DIMENSION_MEP", DOC, OUT)])
    check(len(pair["decided"]) == 1 and pair["for_the_host"] == [],
          "it is decided, and nothing goes to the host")
    got = pair["decided"][0]
    check(got["verdict"] == MRG.KEEP_SEPARATE,
          "the verdict is '%s'" % MRG.KEEP_SEPARATE)
    check(got["fragments"] == ["a", "b"]
          and len(got["capabilities"]) == 2,
          "naming both fragments and both capabilities")
    check("the plumbing matched" in got["why"],
          "with the reason: the signature matched because the plumbing "
          "matched")
    check(MRG.KEEP_SEPARATE in MRG.VERDICTS and len(MRG.VERDICTS) == 4,
          "and it is one of docs/09's four verdicts, not a fifth")

    print("\n4. same shape, same capability is the host's")
    twins = MRG.look([frag("a", "COUNT_THEM", DOC, OUT),
                      frag("b", "COUNT_THEM", DOC, OUT)])
    check(twins["decided"] == [] and len(twins["for_the_host"]) == 1,
          "nothing is decided; it goes to the host")
    asked = twins["for_the_host"][0]
    check(asked["capability"] == "COUNT_THEM",
          "carrying the capability they share")
    check(sorted(asked["open"]) == sorted(
        one for one in MRG.VERDICTS if one != MRG.KEEP_SEPARATE),
          "with the other three verdicts left open: %s"
          % ", ".join(asked["open"]))
    check(MRG.KEEP_SEPARATE not in asked["open"],
          "and the one this agent CAN decide is not among them")
    check("in neither card" in asked["why"],
          "because which implementation is better is in neither card")
    check(asked["at_production"] == [],
          "nothing here is at PRODUCTION")
    live = MRG.look([frag("a", "X", DOC, OUT, status="PRODUCTION"),
                     frag("b", "X", DOC, OUT, status="DEPRECATED")])[
        "for_the_host"][0]
    check(live["at_production"] == ["a"] and live["leaving"] == ["b"],
          "and when they are, the evidence says which is live and which "
          "is already on the way out")
    check("already on the way out" in live["why"], "in words too")

    print("\n5. a fragment with no contract is listed")
    odd = MRG.look([{"id": "a"}, frag("b", "X", DOC, OUT),
                    frag("c", "X", DOC, OUT)])
    check([one["fragment"] for one in odd["unreadable"]] == ["a"],
          "the one with no contract is listed")
    check("no contract" in odd["unreadable"][0]["why"],
          "with why: %s" % odd["unreadable"][0]["why"])
    check(odd["of"] == 2 and len(odd["for_the_host"]) == 1,
          "and the other two were still compared")
    check(any("not a fragment that matched nothing" in line
              for line in odd["unjudged"]),
          "the answer says a fragment nothing can compare is not one "
          "that matched nothing")
    bent = MRG.look([{"id": "a", "contract": {"needs": "doc"}},
                     frag("b", "X", DOC, OUT), frag("c", "X", DOC, OUT)])
    check(bent["unreadable"][0]["fragment"] == "a",
          "a contract whose `needs` is not a list is listed too")

    print("\n6. against the real library")
    found, problems = FRAG.load_all()
    real = MRG.look(list(found.values()))
    check(real["looked"] and real["of"] == len(found),
          "all %d fragments were compared" % len(found))
    check(len(real["decided"]) == 2,
          "exactly two groups share a contract signature")
    check(real["for_the_host"] == [],
          "and NOT ONE of them needs the host - both are keep-separate")
    names = sorted(one for group in real["decided"]
                   for one in group["fragments"])
    check(names == ["dimension-family-instances", "dimension-mep-runs",
                    "transfer-materials-between-documents",
                    "transfer-view-filters-between-documents"],
          "the four are: %s" % ", ".join(names))
    check(all(len(one["capabilities"]) == 2 for one in real["decided"]),
          "each pair declares two different capabilities - the answer "
          "people expect from a merge agent is the one it gives least")
    check(real["unreadable"] == [],
          "and every fragment in the library had a contract to compare")

    print("\n7. every declared failure is named and reached")
    one = frag("a", "X", DOC, OUT)
    for these, name in (
            ([], "NOTHING_TO_COMPARE"),
            ([one], "NOTHING_TO_COMPARE"),
            (["a string", one], "NOT_A_FRAGMENT"),
            ([{"id": "  "}, one], "NOT_A_FRAGMENT"),
            ([one, dict(one)], "DUPLICATE_ID")):
        answer = MRG.look(these)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)
    for writing in ("open(", "write(", "makedirs", "subprocess"):
        check(writing not in logic, "the agent never uses %s" % writing)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-FRG-MRG-004.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 3, "the contract declares 3 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(real["unjudged"]) == 5, "five things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a matching signature is a question, not an answer")
    return 0


if __name__ == "__main__":
    sys.exit(main())
