# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-COM-010
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Contributions - one byte changes the sha, and the review goes stale.

    python tests/test_contribution.py

WHAT IT PROVES
  1. THE SHA IS OF THE PAYLOAD. One byte changed and the review is
     STALE - not close enough, not near enough, stale.

  2. IT IS RECOMPUTED HERE, NOT TRUSTED. A review carrying a
     well-formed sha of something else is caught, which is the failure
     HERON-RPT-VAL-004 was built to find, used on purpose.

  3. PER ITEM MEANS PER ITEM. A review of one item does not cover
     another, however many items there are and however good that review
     is.

  4. THE EGRESS CHECKS COME FIRST, per item - a leak in the second item
     is caught although the first is perfectly reviewed, and the value
     is nowhere in the answer.

  5. A MACHINE MAY NOT REVIEW, and the word list is
     HERON-LRN-PRO-004's object.

  6. NOTHING IS SUBMITTED.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_contribution as COM                               # noqa: E402
import heron_promotion as PRO                                  # noqa: E402
import heron_release as RELEASE                                # noqa: E402
import heron_render as RND                                     # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

TOKEN = "ghp_" + "A" * 36
WHERE = "heron-community/fragments"

DUCTS = {"name": "count-ducts", "payload": "capability: COUNT_DUCTS\n"}
PIPES = {"name": "count-pipes", "payload": "capability: COUNT_PIPES\n"}


def read(thing, **changes):
    # NOT `item` - that is also a field name in a review, and the two
    # collide as keyword arguments.
    card = {"by": "Ajmal", "at": "2026-09-15 11:20", "item": thing["name"],
            "saw": COM.sha_of(thing["payload"])}
    card.update(changes)
    return card


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_contribution.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. the sha is of the payload")
    good = COM.submit([DUCTS], [read(DUCTS)], to=WHERE)
    check(good["prepared"] and good["items"][0]["sha"]
          == COM.sha_of(DUCTS["payload"]),
          "a matching review goes through, carrying the sha")
    # ONE BYTE. Not close enough, not near enough - stale.
    edited = dict(DUCTS, payload=DUCTS["payload"] + " ")
    stale = COM.submit([edited], [read(DUCTS)], to=WHERE)
    reached.add(stale.get("refused"))
    check(stale["refused"] == "REVIEW_IS_STALE",
          "one trailing space added after the review makes it stale")
    check(stale["reviewed"] != stale["is_now"]
          and stale["item"] == "count-ducts",
          "and the answer names the item and BOTH shas")
    check("it was not this" in stale["why"],
          "with the plain statement: somebody reviewed something, and it "
          "was not this")
    # THE NAME IS NOT THE PAYLOAD.
    renamed = dict(DUCTS, name="counts-the-ducts")
    check(COM.sha_of(renamed["payload"]) == COM.sha_of(DUCTS["payload"]),
          "renaming an item does not change its payload's sha - the sha "
          "is of what a reviewer READ, not of what it is called")

    print("\n2. it is recomputed here, not trusted")
    # A WELL-FORMED SHA OF SOMETHING ELSE. This is exactly the shape of
    # the bug HERON-RPT-VAL-004 was built to catch.
    lying = COM.submit([DUCTS],
                       [read(DUCTS, saw=COM.sha_of(PIPES["payload"]))],
                       to=WHERE)
    check(lying["refused"] == "REVIEW_IS_STALE",
          "a review carrying a perfectly valid sha of a DIFFERENT payload "
          "is caught")
    check(COM.sha_of("x") == RND.hashlib.sha256(b"x").hexdigest(),
          "and the sha is computed the same way HERON-RPT-RND-002 does it")
    check(any("recomputed here" in line.lower()
              for line in good["unjudged"]),
          "the answer says the sha was recomputed rather than read off "
          "the review")

    print("\n3. per item means per item")
    short = COM.submit([DUCTS, PIPES], [read(DUCTS)], to=WHERE)
    reached.add(short.get("refused"))
    check(short["refused"] == "NOT_REVIEWED"
          and short["item"] == "count-pipes",
          "one review of two items leaves the second unreviewed, named")
    check(short.get("asked") and "in full" in short["asked"],
          "and the question asks them to read it IN FULL: %r"
          % short["asked"])
    # A REVIEW THAT NAMES NOTHING COVERS NOTHING.
    floating = COM.submit([DUCTS], [read(DUCTS, item="")], to=WHERE)
    check(floating["refused"] == "NOT_REVIEWED",
          "a review naming no item covers no item")
    both = COM.submit([DUCTS, PIPES], [read(DUCTS), read(PIPES)], to=WHERE)
    check(both["prepared"] and both["of"] == 2,
          "two items with two reviews go through")
    check(sorted(one["item"] for one in both["items"])
          == ["count-ducts", "count-pipes"],
          "and each is reported on its own")

    print("\n4. the egress checks come first, per item")
    leaky = COM.submit([DUCTS, dict(PIPES, payload="token %s" % TOKEN)],
                       [read(DUCTS), read(PIPES)], to=WHERE)
    reached.add(leaky.get("refused"))
    check(leaky["refused"] == "CARRIES_A_SECRET",
          "a credential in the SECOND item is caught although the first "
          "is perfectly reviewed")
    check(leaky["item"] == "count-pipes" and TOKEN not in repr(leaky),
          "the answer names the item and never the value")
    heavy = COM.submit([dict(DUCTS, attachments=["Tower A.rvt"])],
                       [read(DUCTS)], to=WHERE)
    reached.add(heavy.get("refused"))
    check(heavy["refused"] == "CARRIES_A_MODEL",
          "and a Revit file beats a valid review too")
    check(COM.NEVER_LEAVES is RELEASE.NEVER_LEAVES,
          "whose extension list is heron_release's object, not a copy")
    # AN ITEM NAMED LIKE A MODEL is refused on its name alone.
    check(COM.submit([{"name": "Tower A.rvt", "payload": "x"}], [],
                     to=WHERE)["refused"] == "CARRIES_A_MODEL",
          "an item NAMED like a model is refused before anything is read")

    print("\n5. a machine may not review")
    for who in ("ci", "bot", "the automatic pipeline"):
        answer = COM.submit([DUCTS], [read(DUCTS, by=who)], to=WHERE)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "REVIEWED_BY_A_MACHINE",
              "'%s' may not review a contribution" % who)
    check(COM.NOT_A_PERSON is PRO.NOT_A_PERSON,
          "and the word list is HERON-LRN-PRO-004's object, not a copy")
    check("HUMAN review in as many words" in
          COM.submit([DUCTS], [read(DUCTS, by="bot")], to=WHERE)["why"],
          "citing docs/28, which asks for human review in as many words")

    print("\n6. nothing is submitted")
    imports = sorted(line.split()[1] for line in logic.split("\n")
                     if line.startswith("import "))
    check(imports == ["hashlib", "heron_promotion", "heron_release",
                      "heron_secrets", "os", "sys"],
          "the import list is hashlib, os, sys and three agents: %s"
          % ", ".join(imports))
    for reaching in ("requests", "urllib", "socket", "subprocess", "http",
                     "write("):
        check(reaching not in logic, "nothing here uses %s" % reaching)
    check("Prepared, not submitted" in good["why"], "and the answer says so")

    print("\n7. every declared failure is named and reached")
    for items, reviews, to, name in (
            ([], [], WHERE, "NOTHING_TO_SUBMIT"),
            ([DUCTS], [read(DUCTS)], "", "NOT_A_SUBMISSION"),
            (["a string"], [], WHERE, "NOT_AN_ITEM"),
            ([{"name": "x"}], [], WHERE, "NOT_AN_ITEM"),
            ([DUCTS, DUCTS], [], WHERE, "NOT_AN_ITEM"),
            ([DUCTS], [read(DUCTS, at="")], WHERE, "NOT_REVIEWED")):
        answer = COM.submit(items, reviews, to=to)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-GIT-COM-010.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 8, "the contract declares 8 failures")
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
    print("PASS    per item, and the review has to have seen it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
