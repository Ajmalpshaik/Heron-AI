# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-ISS-006
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Issues - the table is derived, and a code is not an address.

    python tests/test_issue.py

WHAT IT PROVES
  1. THE TABLE IS DERIVED FROM THE CONTRACTS. Every code an agent
     declares routes to that agent, checked against the contract file
     itself rather than against a list.

  2. A CODE CLAIMED BY SEVERAL COMES BACK WITH ALL OF THEM, and no
     tie-break is applied - not the first, not the shortest, not the
     one whose name sorts first.

  3. A SHOUTED WORD IS NOT A CODE. `REVIT`, `MCP` and `BIM` are ignored;
     a token with an underscore that nothing declares is REPORTED.

  4. THE EGRESS CHECKS COME FIRST. An issue carrying a credential is
     refused for the credential even when its codes route perfectly, and
     the value is nowhere in the answer.

  5. NO CODE MEANS NOTHING WAS TRIAGED, said plainly rather than guessed
     at from the wording.

  6. NOTHING IS POSTED, and no confirmation is demanded - with the
     reason, because the register asks for one on two other rows and not
     this one.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_issue as ISS                                      # noqa: E402
import heron_release as RELEASE                                # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

TOKEN = "ghp_" + "A" * 36


def issue(**changes):
    card = {"title": "something went wrong", "body": "it did"}
    card.update(changes)
    return card


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_issue.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]
    book = ISS.declared()

    print("\n1. the table is derived from the contracts")
    check(len(book) > 300,
          "%d distinct code(s) were read off the contracts" % len(book))
    # CHECKED AGAINST A CONTRACT FILE, not against a list here.
    mine = CON.load(os.path.join(ROOT, "brain", "agents",
                                 "HERON-GIT-ISS-006.yaml"))
    for code in (mine.get("failures") or []):
        check("HERON-GIT-ISS-006" in book.get(code, []),
              "%s routes to the agent whose contract declares it" % code)
    # DERIVED, NOT NAMED. Naming a code here is this test asserting
    # something about the library rather than about the agent - and the
    # first attempt named one that turned out to have two owners.
    only_one = sorted(code for code in book if len(book[code]) == 1)
    check(len(only_one) == len(book) - 47,
          "%d code(s) have exactly one owner and %d have several"
          % (len(only_one), len(book) - len(only_one)))
    lone = only_one[0]
    solo = ISS.triage(issue(body="it said %s" % lone))
    check([one["agent"] for one in solo["routed"]] == book[lone],
          "and '%s', which one agent declares, routes to exactly %s"
          % (lone, book[lone][0]))
    check(solo["ambiguous"] == [], "with nothing ambiguous about it")

    print("\n2. a shared code comes back with all of them")
    shared = sorted(code for code in book if len(book[code]) > 1)
    check(len(shared) == 47,
          "%d code(s) are declared by more than one agent" % len(shared))
    worst = max(book, key=lambda code: len(book[code]))
    answer = ISS.triage(issue(body="it said %s" % worst))
    check(answer["routed"] == [] and len(answer["ambiguous"]) == 1,
          "'%s' is not routed - it is ambiguous" % worst)
    got = answer["ambiguous"][0]
    check(got["agents"] == book[worst] and len(got["agents"]) == 10,
          "and ALL %d agents come back, not one of them"
          % len(got["agents"]))
    check("No tie-break is applied" in got["why"],
          "with the reason: a tie-break here would be a guess wearing a "
          "routing table's clothes")
    check(got["agents"] == sorted(got["agents"]),
          "they are sorted, which is an ordering rather than a choice")

    print("\n3. a shouted word is not a code")
    loud = ISS.triage(issue(body="REVIT 2024, MCP server, BIM model, "
                                 "ALL CAPS SHOUTING"))
    check(loud["codes_seen"] == [],
          "no underscore, no code - REVIT, MCP and BIM are ignored")
    check(loud["unknown_codes"] == [],
          "and none of them is reported as an unknown code")
    odd = ISS.triage(issue(body="it said WIDGET_EXPLODED"))
    check(odd["unknown_codes"] == ["WIDGET_EXPLODED"],
          "a code-shaped token nothing declares IS reported")
    check(odd["routed"] == [] and odd["ambiguous"] == [],
          "and routed nowhere")
    check(any("either a typo or a failure somebody removed" in line
              for line in odd["unjudged"]),
          "with why it is worth a maintainer's eye")

    print("\n4. the egress checks come first")
    leaky = ISS.triage(issue(body="NOT_A_SCOPE and the token is %s" % TOKEN))
    reached.add(leaky.get("refused"))
    check(leaky["refused"] == "CARRIES_A_SECRET",
          "a credential beats a perfectly routable code")
    check(TOKEN not in repr(leaky),
          "and the value is nowhere in the answer")
    check(leaky["where"] == "body" and leaky["found"],
          "only the field and the kind: %s in the %s"
          % (", ".join(leaky["found"]), leaky["where"]))
    heavy = ISS.triage(issue(body="NOT_A_SCOPE",
                             attachments=["Tower A.rvt"]))
    reached.add(heavy.get("refused"))
    check(heavy["refused"] == "CARRIES_A_MODEL",
          "and so does a Revit file")
    check(ISS.NEVER_LEAVES is RELEASE.NEVER_LEAVES,
          "whose extension list is heron_release's object, not a copy")

    print("\n5. no code means nothing was triaged")
    quiet = ISS.triage(issue(body="Selecting ducts takes ages on a big "
                                  "model and I am not sure why."))
    check(quiet["triaged"] is True, "the issue is still read")
    check(quiet["routed"] == [] and quiet["ambiguous"] == []
          and quiet["unknown_codes"] == [],
          "and nothing is routed anywhere")
    check("no failure code in the text" in quiet["why"],
          "the answer says so plainly")
    check("the host's under D-01" in quiet["unjudged"][0],
          "and hands the prose to the host rather than guessing from the "
          "wording")

    print("\n6. nothing is posted, and no confirmation is demanded")
    good = ISS.triage(issue(body="it said %s" % lone))
    check("NOTHING WAS POSTED" in good["unjudged"][3],
          "the answer says nothing was posted")
    check("HERON-GIT-PR-005" in good["unjudged"][3]
          and "HERON-GIT-COM-010" in good["unjudged"][3],
          "and names the two rows that DO ask for one, so the absence "
          "here reads as deliberate rather than forgotten")
    check("confirmation" not in logic.split("unjudged")[0],
          "no confirmation gate exists in the code before the answer")
    for reaching in ("requests", "urllib", "socket", "subprocess", "http"):
        check(reaching not in logic, "nothing here reaches %s" % reaching)

    print("\n7. every declared failure is named and reached")
    for card, kwargs, name in (
            (None, {}, "NOTHING_TO_FILE"),
            ("a string", {}, "NOT_AN_ISSUE"),
            ({"title": "x"}, {}, "NOT_AN_ISSUE"),
            (issue(body="  "), {}, "NOT_AN_ISSUE"),
            (issue(), {"contracts": {}}, "NO_CONTRACTS")):
        answer = ISS.triage(card, **kwargs)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    named = mine.get("failures") or []
    check(len(named) == 5, "the contract declares 5 failures")
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
    print("PASS    triage by the code, and a code is not an address")
    return 0


if __name__ == "__main__":
    sys.exit(main())
