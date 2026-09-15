# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-SKL-CMP-005
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Skill composition - the cycle comes back as a path, and a card may not
understate what it composes.

    python tests/test_composition.py

WHAT IT PROVES
  1. THE RISK LADDER IS HERON-KRN-CAP-008'S BY IDENTITY, not by value.
     `RISK is CAP.RISK_ORDER` - and the module holds no ladder of its
     own, checked by counting the risk names written in its code.

  2. A CYCLE COMES BACK AS A PATH IN ORDER, starting and ending at the
     same skill, and a skill that uses itself is a loop of one.

  3. A CYCLE IS FOUND FROM ANYWHERE, not only from a root. A graph whose
     every node is used by something has no root at all.

  4. RISK_IS_UNDERSTATED FIRES, and the answer names what it composes
     and through which skill.

  5. DECLARING HIGHER IS ALLOWED AND REPORTED - and `composes` says what
     it composes, not the declaration back again.

  6. THE PROPAGATION IS THE HIGHEST OF THE WHOLE CHAIN, not one level.

  7. NOTHING IS BUILT - `composed` is false on the good path too.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_composition as CMP                                # noqa: E402
import heron_capability as CAP                                 # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_composition.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    print("\n1. the ladder is HERON-KRN-CAP-008's, by identity")
    check(CMP.RISK is CAP.RISK_ORDER,
          "RISK is CAP.RISK_ORDER - the same object, not an equal copy")
    # A COPY WOULD BE EQUAL TOO. Identity is the only check that fails
    # the day somebody retypes the ladder here.
    check(CMP.RISK == ("READ", "ANALYZE", "SUGGEST", "EXECUTE", "MODIFY",
                       "PUBLISH", "ADMIN"),
          "and it is the seven-rung ladder, in order")
    written = [rung for rung in CMP.RISK if '"%s"' % rung in logic]
    check(not written,
          "the module's code writes none of the seven names%s"
          % ("" if not written else ": %s" % ", ".join(written)))

    print("\n2. a cycle comes back as a path, in order")
    three = CMP.compose([{"name": "a", "risk": "READ", "uses": ["b"]},
                         {"name": "b", "risk": "READ", "uses": ["c"]},
                         {"name": "c", "risk": "READ", "uses": ["a"]}])
    reached.add(three.get("refused"))
    check(three["refused"] == "A_CYCLE", "a -> b -> c -> a is refused")
    check(three["loop"] == ["a", "b", "c", "a"],
          "and the loop comes back IN ORDER, not as a set: %s"
          % " -> ".join(three["loop"]))
    check(three["loop"][0] == three["loop"][-1],
          "it starts and ends at the same skill, so the edge to cut is "
          "readable off it")
    check(" -> ".join(three["loop"]) in three["why"],
          "and the path is in the message a person reads")

    one = CMP.compose([{"name": "a", "risk": "READ", "uses": ["a"]}])
    check(one["refused"] == "A_CYCLE" and one["loop"] == ["a", "a"],
          "a skill that uses itself is a loop of one, not a special case")

    print("\n3. found from anywhere, not only from a root")
    # EVERY NODE IS USED BY SOMETHING, so there is no root to start at.
    # An agent walking only the roots would report this graph clean.
    rootless = CMP.compose([{"name": "a", "risk": "READ", "uses": ["b"]},
                            {"name": "b", "risk": "READ", "uses": ["a"]}])
    check(rootless["refused"] == "A_CYCLE",
          "a graph with no root at all is still checked")
    # AND A CYCLE HANGING OFF A CLEAN ROOT, which a walk that stops at
    # the first tidy branch would miss.
    hanging = CMP.compose([{"name": "top", "risk": "READ", "uses": ["ok"]},
                           {"name": "ok", "risk": "READ", "uses": []},
                           {"name": "x", "risk": "READ", "uses": ["y"]},
                           {"name": "y", "risk": "READ", "uses": ["x"]}])
    check(hanging["refused"] == "A_CYCLE" and hanging["loop"][0] == "x",
          "and a cycle in a second component is found: %s"
          % " -> ".join(hanging["loop"]))

    print("\n4. a card may not declare less than it composes")
    under = CMP.compose([{"name": "tidy", "risk": "READ",
                          "uses": ["wipe"]},
                         {"name": "wipe", "risk": "MODIFY", "uses": []}])
    reached.add(under.get("refused"))
    check(under["refused"] == "RISK_IS_UNDERSTATED",
          "READ composing MODIFY is refused, not raised quietly")
    row = under["understated"][0]
    check(row["declared"] == "READ" and row["effective"] == "MODIFY",
          "the answer names both: declared %s, effective %s"
          % (row["declared"], row["effective"]))
    check(row["through"] == "wipe",
          "and WHICH skill carries it - 'through %s'" % row["through"])
    check("the card is the one place a person looks" in row["why"],
          "with the reason a person can act on")

    print("\n5. declaring higher is allowed, and reported honestly")
    over = CMP.compose([{"name": "report", "risk": "PUBLISH",
                         "uses": ["find"]},
                        {"name": "find", "risk": "READ", "uses": []}])
    check(not over.get("refused"), "PUBLISH composing READ is not refused")
    care = over["cautious"][0]
    check(care["skill"] == "report" and care["declared"] == "PUBLISH",
          "it is reported as cautious")
    # THE ONE THING A CAUTIOUS ROW IS NOT is "says X and does X".
    check(care["composes"] == "READ",
          "and `composes` says what it COMPOSES (%s), not the "
          "declaration back again" % care["composes"])
    check(care["composes"] != care["declared"],
          "so the two columns cannot read the same")

    print("\n6. the highest of the whole chain, not one level")
    chain = CMP.compose([{"name": "a", "risk": "ADMIN", "uses": ["b"]},
                         {"name": "b", "risk": "READ", "uses": ["c"]},
                         {"name": "c", "risk": "READ", "uses": ["d"]},
                         {"name": "d", "risk": "MODIFY", "uses": []}])
    check(chain["refused"] == "RISK_IS_UNDERSTATED",
          "READ three levels above MODIFY is still understated")
    deep = dict((one["skill"], one["effective"])
                for one in chain["understated"])
    check(deep.get("b") == "MODIFY" and deep.get("c") == "MODIFY",
          "both b and c carry MODIFY up from d, not only c")
    check("a" not in deep,
          "and ADMIN over MODIFY is not understated - it is already higher")

    print("\n7. nothing is built")
    good = CMP.compose([{"name": "one", "risk": "READ", "uses": []}])
    check(good["composed"] is False,
          "`composed` is false on the good path too - a verdict comes "
          "back, never a skill")
    check("Nothing was built" in good["why"], "and the answer says so")
    check(len(good["unjudged"]) == 4, "four things are left unjudged")
    for writing in ("open(", "write(", "makedirs", "rmtree"):
        check(writing not in logic, "the agent never uses %s" % writing)

    print("\n8. every declared failure is named and reached")
    for skills, name in (
            ([], "NOTHING_TO_COMPOSE"),
            (["not a dict"], "NOT_A_SKILL"),
            ([{"name": "", "risk": "READ"}], "NOT_A_SKILL"),
            ([{"name": "a", "risk": "READ"},
              {"name": "a", "risk": "READ"}], "NOT_A_SKILL"),
            ([{"name": "a", "risk": "DELETE"}], "NOT_A_RISK"),
            ([{"name": "a", "risk": "READ", "uses": ["gone"]}],
             "UNKNOWN_SKILL")):
        answer = CMP.compose(skills)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-SKL-CMP-005.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 6, "the contract declares 6 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
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
    print("PASS    acyclic, and a card may not understate")
    return 0


if __name__ == "__main__":
    sys.exit(main())
