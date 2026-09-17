# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-REL-007
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Releases - the gate list is read, and a gate nobody ran is not green.

    python tests/test_tag.py

WHAT IT PROVES
  1. THE GATE LIST IS READ OUT OF .github/workflows/gates.yml, and it is
     the four that file actually runs - matched against the file, not
     against a list that looks like it. A workflow that cannot be read
     REFUSES rather than falling back to a guess.

  2. "NOT REPORTED" COUNTS AS RED. A gate missing from the map, one set
     to false, and one set to a string are all red - a gate nobody ran
     is not a gate that passed.

  3. docs/07 s64's THREE PROPERTIES ARE ALL CHECKED, and the document
     says what the agent says it says.

  4. A BRANCH IS NOT A COMMIT. `main` is refused; a short sha and a full
     one are both accepted.

  5. THE VERSION MUST BE AHEAD, AND MUST MATCH THE BUMP WHEN ONE IS
     GIVEN. Without one the answer says the bump was NOT verified rather
     than implying it was.

  6. A CONFIRMATION NAMES THE VERSION, and a machine may not give one.

  7. NOTHING IS PUBLISHED.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_tag as REL                                        # noqa: E402
import heron_promotion as PRO                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

GATES = REL.required_gates()


def release(**changes):
    card = {"version": "0.2.0", "artefact": "heron-0.2.0.zip",
            "signature": "minisign:RWQ...", "built_from": "0913c3ab4f21",
            "gates": dict((name, True) for name in GATES)}
    card.update(changes)
    return card


def yes(**changes):
    card = {"by": "Ajmal", "at": "2026-09-15 10:04", "version": "0.2.0"}
    card.update(changes)
    return card


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_tag.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]
    workflow = io.open(REL.WORKFLOW, encoding="utf-8").read()
    # FLATTENED AND UNEMPHASISED. docs/07 wraps "versioned release
    # artefact" across a line and puts ** round it, so an exact search
    # of the raw file misses a phrase that is plainly there.
    install = " ".join(io.open(
        os.path.join(ROOT, "docs", "07-installation-and-update.md"),
        encoding="utf-8").read().replace("*", "").split())

    print("\n1. the gate list is read out of the workflow")
    # THIS LIST GREW FROM FOUR TO EIGHT on 2026-09-17, and the growth is the
    # design working rather than an edit chasing it. `required_gates` READS the
    # workflow precisely so "a fifth gate added to CI becomes required here with
    # no edit" - its own docstring. Four checkers had sat in tools/ that CI ran
    # nowhere; wiring them made them release-blocking in the same stroke, which
    # is the promise that docstring makes. The list is still pinned here so that
    # a gate LEAVING CI is a test failure rather than a quietly easier release.
    check(GATES == ["check-docs", "check-metadata", "check-structure",
                    "check-signatures", "check-licence", "check-narrow-errors",
                    "check-package", "check-fragments-compile"],
          "it is the eight CI runs, in the order it runs them: %s"
          % ", ".join(GATES))
    for name in GATES:
        check("python tools/%s.py" % name in workflow,
              "%s is in the workflow, as a line it actually runs" % name)
    # A LIST GUESSED HERE would be a release cut against this file's
    # opinion rather than against what CI runs.
    check(REL.required_gates("/nowhere/gates.yml") == [],
          "an unreadable workflow yields no list")
    blind = REL.cut(release(), yes(), workflow="/nowhere/gates.yml")
    reached.add(blind.get("refused"))
    check(blind["refused"] == "NO_GATE_LIST",
          "and the release is REFUSED rather than cut against a guess")

    print("\n2. not reported counts as red")
    def all_but_first(value):
        book = dict((name, True) for name in GATES)
        book[GATES[0]] = value
        return book

    for gates, what in (({}, "an empty map"),
                        (dict((n, True) for n in GATES[:-1]),
                         "one gate simply missing"),
                        (all_but_first(False), "one set to false"),
                        (all_but_first("passed"),
                         "one set to the STRING 'passed'"),
                        ("all green", "gates that are not a map at all")):
        answer = REL.cut(release(gates=gates), yes())
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "A_GATE_IS_RED",
              "%s is red" % what)
    one_short = REL.cut(release(gates=dict((n, True) for n in GATES[:-1])),
                        yes())
    check(one_short["red"] == [GATES[-1]]
          and one_short["required"] == GATES,
          "and the answer names which - %s of %d required"
          % (", ".join(one_short["red"]), len(GATES)))
    check("a gate nobody ran is not a gate that passed" in one_short["why"],
          "with the reason stated")

    print("\n3. docs/07 s64's three properties")
    for said in ("fetches a signed release",
                 "versioned release artefact",
                 "not whatever the default branch happens to say today"):
        check(said in install, "docs/07 carries %r" % said)
    unsigned = REL.cut(release(signature=""), yes())
    reached.add(unsigned.get("refused"))
    check(unsigned["refused"] == "NOT_SIGNED",
          "an unsigned artefact is refused")
    check("D-35" in unsigned["why"],
          "refused rather than shipped with a note - D-35")

    print("\n4. a branch is not a commit")
    for built in ("main", "HEAD", "claude/agent-count", "v0.2.0"):
        answer = REL.cut(release(built_from=built), yes())
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "NOT_A_COMMIT",
              "'%s' is not a commit" % built)
    for built in ("0913c3a", "0913c3ab4f21", "0" * 40):
        check(REL.cut(release(built_from=built), yes())["cut"] is True,
              "'%s' is" % built[:14])

    print("\n5. ahead, and matching the bump when one is given")
    check(REL.cut(release(), yes(), last="0.1.0")["cut"] is True,
          "0.2.0 after 0.1.0 goes through")
    for before in ("0.2.0", "0.3.0", "1.0.0"):
        answer = REL.cut(release(), yes(), last=before)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "NOT_AHEAD",
              "0.2.0 after %s is not ahead" % before)
    wrong = REL.cut(release(), yes(), bump={"to": "1.0.0"})
    reached.add(wrong.get("refused"))
    check(wrong["refused"] == "WRONG_VERSION"
          and wrong["wanted"] == "1.0.0" and wrong["tagging"] == "0.2.0",
          "a tag disagreeing with HERON-GIT-VER-008 is refused, naming "
          "both")
    check("not knowable from either side alone" in wrong["why"],
          "and says which of the two is wrong is not knowable from one")
    with_bump = REL.cut(release(), yes(), bump={"to": "0.2.0"})
    check(with_bump["checked_bump"] is True
          and "MATCHES HERON-GIT-VER-008" in with_bump["unjudged"][2],
          "handed a matching one, the answer says it matched")
    without = REL.cut(release(), yes())
    check(without["checked_bump"] is False
          and "NOT VERIFIED" in without["unjudged"][2],
          "without one it says the bump was NOT verified, rather than "
          "implying it was")

    print("\n6. a confirmation names the version")
    other = REL.cut(release(), yes(version="0.9.9"))
    reached.add(other.get("refused"))
    check(other["refused"] == "NOT_CONFIRMED"
          and other["confirmed"] == "0.9.9" and other["cutting"] == "0.2.0",
          "a confirmation for another version is refused, naming both")
    check(REL.cut(release(), yes(version="v0.2.0"))["cut"] is True,
          "and a leading v on either side is the same version")
    machine = REL.cut(release(), yes(by="the release pipeline"))
    reached.add(machine.get("refused"))
    check(machine["refused"] == "CONFIRMED_BY_A_MACHINE",
          "a machine may not confirm a release")
    check(REL.NOT_A_PERSON is PRO.NOT_A_PERSON,
          "and the word list is HERON-LRN-PRO-004's object, not a copy")
    check("they run it" in machine["why"],
          "with the reason: nobody downstream can check the decision")

    print("\n7. nothing is published")
    good = REL.cut(release(), yes(), last="0.1.0",
                   bump={"to": "0.2.0"})
    check(good["tag"] == "v0.2.0" and good["may_publish"] is True,
          "the tag comes back as v0.2.0 with a verdict")
    for reaching in ("requests", "urllib", "socket", "subprocess", "http"):
        check(reaching not in logic, "nothing here reaches %s" % reaching)
    check("write(" not in logic,
          "and it writes nothing - it only reads the workflow")
    check("Green gates are not an install" in good["unjudged"][3],
          "the answer says green gates are not an install, rather than "
          "letting four ticks read as one")

    print("\n8. every declared failure is named and reached")
    for card, confirmation, name in (
            (None, yes(), "NOTHING_TO_CUT"),
            ("a string", yes(), "NOT_A_RELEASE"),
            ({"version": "0.2.0"}, yes(), "NOT_A_RELEASE"),
            (release(version="two"), yes(), "NOT_A_VERSION"),
            (release(), None, "NOT_CONFIRMED"),
            (release(), "a nod", "NOT_CONFIRMED"),
            (release(), yes(at=""), "NOT_CONFIRMED")):
        answer = REL.cut(card, confirmation)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-GIT-REL-007.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 11, "the contract declares 11 failures")
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
    print("PASS    the gate list is read, and not reported is red")
    return 0


if __name__ == "__main__":
    sys.exit(main())
