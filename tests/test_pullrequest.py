# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-PR-005
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Pull requests - every time means this one, and a leak beats a signature.

    python tests/test_pullrequest.py

WHAT IT PROVES
  1. THE EGRESS CHECKS COME FIRST. A pull request carrying a credential
     is refused for the credential even when it is perfectly confirmed -
     asking somebody to confirm a leak would be asking them to
     authorise one.

  2. THE VALUE NEVER APPEARS. The whole answer is searched for the
     secret that was found, and it is not in it.

  3. "EVERY TIME" MEANS THIS HEAD. A confirmation naming another branch
     is refused, and the answer names both - what was confirmed and what
     was being opened.

  4. A MACHINE MAY NOT CONFIRM, and the word list is
     HERON-LRN-PRO-004's object by identity, not a copy of it.

  5. A REVIT FILE IS REFUSED BY ITS NAME ALONE, for every extension on
     heron_release's list - which is that module's object, not a copy.

  6. NOTHING IS OPENED. The import list carries nothing that could reach
     a network, and the answer says so.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_pullrequest as PR                                 # noqa: E402
import heron_promotion as PRO                                  # noqa: E402
import heron_release as RELEASE                                # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

TOKEN = "ghp_" + "A" * 36


def request(**changes):
    card = {"title": "Two agents", "body": "What they do and why.",
            "head": "claude/two-agents", "base": "main"}
    card.update(changes)
    return card


def yes(**changes):
    # `of` binds the confirmation to WHAT was confirmed. A branch name is
    # mutable - the same head carries new commits, a replaced title and a
    # different base - so the head alone made every confirmation a
    # standing one wearing a specific one's coat.
    card = {"by": "Ajmal", "at": "2026-09-15 09:12",
            "head": "claude/two-agents",
            "of": PR.fingerprint_of(request())}
    card.update(changes)
    return card


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_pullrequest.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. the egress checks come first")
    # PERFECTLY CONFIRMED, AND STILL REFUSED. Asking somebody to confirm
    # a leak would be asking them to authorise one.
    leaky = PR.open_request(request(body="the token is %s" % TOKEN), yes())
    reached.add(leaky.get("refused"))
    check(leaky["refused"] == "CARRIES_A_SECRET",
          "a credential beats a valid confirmation")
    heavy = PR.open_request(request(attachments=["Tower A.rvt"]), yes())
    reached.add(heavy.get("refused"))
    check(heavy["refused"] == "CARRIES_A_MODEL",
          "and so does a Revit file")
    check(PR.open_request(request(attachments=["Tower A.rvt"]),
                          None)["refused"] == "CARRIES_A_MODEL",
          "the model is refused even with NO confirmation at all - the "
          "egress answer does not depend on who asked")
    check(PR.open_request(request(body="the token is %s" % TOKEN),
                          yes(by="ci"))["refused"] == "CARRIES_A_SECRET",
          "and a leak confirmed by a machine is reported as the leak, "
          "which is the worse of the two")

    print("\n2. the value never appears")
    check(TOKEN not in repr(leaky),
          "the token is nowhere in the answer - not in `why`, not in a "
          "field, not anywhere")
    check(leaky["found"] and leaky["where"] == "body",
          "what comes back is the KIND and the field: %s in the %s"
          % (", ".join(leaky["found"]), leaky["where"]))
    check("refused WHOLE rather than stripped" in leaky["why"],
          "and it is refused whole - a stripped body is one somebody "
          "publishes believing it is clean")
    titled = PR.open_request(request(title="key %s" % TOKEN), yes())
    check(titled["refused"] == "CARRIES_A_SECRET"
          and titled["where"] == "title" and TOKEN not in repr(titled),
          "the title is read too, and its value is just as absent")

    print("\n3. every time means this head")
    standing = PR.open_request(request(), yes(head="claude/something-else"))
    reached.add(standing.get("refused"))
    check(standing["refused"] == "CONFIRMATION_IS_STANDING",
          "a confirmation naming another branch is refused")
    check(standing["confirmed"] == "claude/something-else"
          and standing["opening"] == "claude/two-agents",
          "and the answer names BOTH - %s was confirmed, %s was being "
          "opened" % (standing["confirmed"], standing["opening"]))
    headless = PR.open_request(request(), yes(head=""))
    reached.add(headless.get("refused"))
    check(headless["refused"] == "NOT_CONFIRMED"
          and headless["missing"] == ["head"],
          "a confirmation with NO head is not a confirmation of anything "
          "in particular")
    good = PR.open_request(request(), yes())
    check(good["prepared"] and good["may_open"],
          "the same head goes through")
    check("WOULD NOT COVER ANOTHER" in good["unjudged"][1],
          "and the answer says the confirmation would not cover another")
    # NOTHING REMEMBERS IT. Proved by asking again with none - a word
    # search would only find the module saying it keeps no store.
    after = PR.open_request(request(), None)
    reached.add(after.get("refused"))
    check(after["refused"] == "NOT_CONFIRMED",
          "and the very next call with no confirmation is refused - the "
          "one that just succeeded was not remembered")
    check(after.get("asked"),
          "with the question asked again rather than assumed: %r"
          % after["asked"])

    print("\n4. a machine may not confirm")
    for who in ("ci", "bot", "the automatic pipeline", "script"):
        answer = PR.open_request(request(), yes(by=who))
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "CONFIRMED_BY_A_MACHINE",
              "'%s' cannot confirm a pull request" % who)
    check(PR.NOT_A_PERSON is PRO.NOT_A_PERSON,
          "and the word list is HERON-LRN-PRO-004's OBJECT, not a copy - "
          "so one of them cannot learn a new word while the other does not")
    check("Golden Rule 7" in PR.open_request(
        request(), yes(by="bot"))["why"],
          "with the rule named: no agent approves itself")

    print("\n5. a Revit file is refused by its name alone")
    check(PR.NEVER_LEAVES is RELEASE.NEVER_LEAVES,
          "the extension list is heron_release's object, not a copy")
    for extension in PR.NEVER_LEAVES:
        answer = PR.open_request(
            request(attachments=["Tower A%s" % extension]), yes())
        check(answer.get("refused") == "CARRIES_A_MODEL",
              "%s is refused" % extension)
    check("the FILE, not at the information" in heavy["why"],
          "citing D-26 - the line is the file, and nothing asked what "
          "was inside it")
    ordinary = request(attachments=["notes.txt", "shot.png"])
    check(PR.open_request(ordinary,
                          yes(of=PR.fingerprint_of(ordinary)))["prepared"],
          "while an ordinary attachment travels fine")
    check(PR.open_request(ordinary, yes()).get("refused")
          == "CONFIRMATION_IS_STALE",
          "and a confirmation given BEFORE the attachment was added does "
          "not cover it - the attachments are part of what was agreed to")

    print("\n6. nothing is opened")
    imports = sorted(line.split()[1] for line in logic.split("\n")
                     if line.startswith("import "))
    check(imports == ["heron_promotion", "heron_release", "heron_secrets",
                      "heron_security", "os", "sys"],
          "the whole import list is os, sys and the four agents it "
          "borrows rules from: %s" % ", ".join(imports))
    for reaching in ("requests", "urllib", "socket", "subprocess", "http"):
        check(reaching not in logic, "nothing here reaches %s" % reaching)
    check(good["draft"] is True,
          "and what it prepares is a DRAFT unless told otherwise")

    print("\n7. every declared failure is named and reached")
    for card, confirmation, name in (
            (None, yes(), "NOTHING_TO_OPEN"),
            ("a string", yes(), "NOT_A_PULL_REQUEST"),
            ({"title": "x"}, yes(), "NOT_A_PULL_REQUEST"),
            (request(body="  "), yes(), "NOT_A_PULL_REQUEST"),
            (request(base="claude/two-agents"), yes(), "SAME_BRANCH"),
            (request(), None, "NOT_CONFIRMED"),
            (request(), "a nod", "NOT_CONFIRMED")):
        answer = PR.open_request(card, confirmation)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    print()
    print("R. THE SECOND CODEX REVIEW - a branch name is not what was "
          "confirmed")
    same = {"title": "t", "body": "b", "head": "feature", "base": "main"}
    agreed = {"by": "Ajmal", "at": "2026-09-16", "head": "feature",
              "of": PR.fingerprint_of(same)}
    answer = PR.open_request(dict(same), dict(agreed))
    check(answer.get("may_open") is True,
          "the request that was actually confirmed opens")
    for field, value in (("title", "a different title"),
                         ("base", "release"),
                         ("body", "a rewritten body")):
        answer = PR.open_request(dict(same, **{field: value}), dict(agreed))
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "CONFIRMATION_IS_STALE",
              "the same confirmation does NOT cover a changed %s - a "
              "branch name is mutable, so a confirmation naming only the "
              "head is a standing one wearing a specific one's coat"
              % field)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-GIT-PR-005.yaml"))
    named = contract.get("failures") or []
    check(len(named) == len(set(named)),
          "the contract declares each failure once")
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
    print("PASS    every time means this one, and a leak beats a signature")
    return 0


if __name__ == "__main__":
    sys.exit(main())
