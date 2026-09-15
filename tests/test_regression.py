# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-REG-006
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Regression - the order of the six steps is the argument.

    python tests/test_regression.py

WHAT IT PROVES
  1. THE SIX STEPS ARE docs/09 s8's, read out of the document, and the
     two this agent does NOT do are marked as somebody else's.

  2. THE ORDER HOLDS UNDER PRESSURE. A change that is untested AND
     regressed AND dropped AND unpreserved comes back as NOT_TESTED -
     because a release nobody ran cannot be compared, and a comparison
     nobody made cannot detect a break.

  3. THE CARD IS THE CLAIM. No "before" results are needed, and a
     release the card never claimed does not regress by failing.

  4. NOT TESTED IS NOT PASSING, and a third result word is refused
     rather than interpreted.

  5. DROPPING A RELEASE IS UNSAFE EVEN WHEN EVERYTHING PASSES.

  6. PRESERVATION IS CHECKED LAST AND ALWAYS - an otherwise perfect
     change with nowhere for the old implementation is still refused.

  7. NOTHING IS BUILT AND NOTHING IS RUN.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_regression as REG                                 # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

THREE = ["2023", "2024", "2025"]
GREEN = dict((one, "pass") for one in THREE)
KEPT = "git: 3331ee9 brain/fragments/x/impl"


def before(**changes):
    card = {"id": "FRG-T-001", "revit": list(THREE)}
    card.update(changes)
    return card


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_regression.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]
    doc = " ".join(io.open(
        os.path.join(ROOT, "docs", "09-skills-and-fragments.md"),
        encoding="utf-8").read().replace("*", "").split())

    print("\n1. the six steps are docs/09 s8's")
    check(len(REG.STEPS) == 6, "there are six, no more and no fewer")
    for number, what, _ in REG.STEPS:
        check("%d. %s" % (number, what) in doc,
              "docs/09 s8 carries '%d. %s'" % (number, what))
    check("This is Golden Rule 4 made executable" in doc,
          "and calls them Golden Rule 4 made executable")
    good = REG.check(before(), before(), GREEN, preserved=KEPT)
    steps = dict((one["step"], one["done_here"]) for one in good["steps"])
    check(steps[1] is False and steps[2] is False,
          "steps 1 and 2 are marked as somebody else's")
    check(all(steps[one] for one in (3, 4, 5, 6)),
          "and steps 3 to 6 as this agent's")
    check(REG.MINE == (3, 4, 5, 6),
          "which is what MINE says too, derived from the same table")

    print("\n2. the order holds under pressure")
    # UNTESTED AND REGRESSED AND DROPPED AND UNPRESERVED, all at once.
    worst = REG.check(before(), {"id": "x", "revit": ["2024"]},
                      {"2023": "fail"}, preserved="")
    reached.add(worst.get("refused"))
    check(worst["refused"] == "NOT_TESTED",
          "four things wrong at once comes back as NOT_TESTED - a "
          "release nobody ran cannot be compared")
    check(worst["releases"] == ["2024", "2025"],
          "naming the untested ones: %s" % ", ".join(worst["releases"]))
    # NOW TESTED, still regressed and dropped and unpreserved.
    then = REG.check(before(), {"id": "x", "revit": ["2024"]},
                     dict(GREEN, **{"2023": "fail"}), preserved="")
    reached.add(then.get("refused"))
    check(then["refused"] == "A_RELEASE_REGRESSED",
          "with results in, the regression is what comes back - not the "
          "drop and not the missing preservation")
    # NOW PASSING, still dropped and unpreserved.
    later = REG.check(before(), {"id": "x", "revit": ["2024", "2025"]},
                      GREEN, preserved="")
    reached.add(later.get("refused"))
    check(later["refused"] == "A_RELEASE_WAS_DROPPED",
          "with nothing failing, the drop is next")
    last = REG.check(before(), before(), GREEN, preserved="")
    reached.add(last.get("refused"))
    check(last["refused"] == "NOT_PRESERVED",
          "and preservation is the last thing left to be wrong")

    print("\n3. the card is the claim")
    check(good["claimed"] == THREE,
          "the claim is the BEFORE card's revit list, and nothing else "
          "was asked for - no second set of results")
    # A RELEASE THE CARD NEVER CLAIMED does not regress by failing.
    extra = REG.check(before(revit=["2024"]), {"id": "x",
                                               "revit": ["2024"]},
                      {"2024": "pass", "2021": "fail"}, preserved=KEPT)
    check(extra["safe"] is True,
          "a failure on a release the card never claimed is not a "
          "regression - there was nothing there to break")
    check("2021" in extra["tested"] and "2021" not in extra["claimed"],
          "it is reported as tested and not as claimed")

    print("\n4. not tested is not passing, and a third word is refused")
    short = REG.check(before(), before(), {"2023": "pass", "2024": "pass"},
                      preserved=KEPT)
    check(short["refused"] == "NOT_TESTED" and short["releases"] == ["2025"],
          "a claimed release with no result at all is refused")
    check("is not a release that passed" in short["why"],
          "said plainly")
    for word in ("maybe", "skipped", "inconclusive", "", "PASS?"):
        answer = REG.check(before(), before(),
                           dict(GREEN, **{"2024": word}), preserved=KEPT)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "NOT_A_RESULT",
              "'%s' is not a result" % word)
    check("'inconclusive' becomes 'fine'" in
          REG.check(before(), before(), dict(GREEN, **{"2024": "maybe"}),
                    preserved=KEPT)["why"],
          "with the reason: interpreting a test result is how "
          "inconclusive becomes fine")
    check(REG.check(before(), before(),
                    dict((one, "PASS") for one in THREE),
                    preserved=KEPT)["safe"] is True,
          "though case alone is not a third word - PASS is pass")

    print("\n5. dropping a release is unsafe even when everything passes")
    dropped = REG.check(before(), {"id": "x", "revit": ["2024", "2025"]},
                        GREEN, preserved=KEPT)
    check(dropped["refused"] == "A_RELEASE_WAS_DROPPED"
          and dropped["releases"] == ["2023"],
          "2023 passes and is dropped, and the drop is what is refused")
    check("whether or not it still builds" in dropped["why"],
          "explicitly: it is breaking whether or not it still builds")
    check("HERON-GIT-VER-008" in dropped["why"],
          "and points at the agent that owns the announcement docs/17 "
          "requires")
    # GAINING ONE IS FINE.
    gained = REG.check(before(), before(revit=THREE + ["2026"]),
                       dict(GREEN, **{"2026": "pass"}), preserved=KEPT)
    check(gained["safe"] and gained["gained"] == ["2026"],
          "gaining a release is safe and reported")
    check(any("not checked against anything here" in line
              for line in gained["unjudged"]),
          "and the answer says gaining is not checked - only that an old "
          "one was not lost")

    print("\n6. preservation is checked last and always")
    check(last.get("asked"), "the answer asks where it went: %r"
                            % last["asked"])
    check("Golden Rule 14" in last["why"],
          "citing never silently discard")
    check("a safe change is still a change that replaced something"
          in last["why"],
          "and why a safe change still owes an answer")
    check(REG.check(before(), before(), GREEN,
                    preserved="   ")["refused"] == "NOT_PRESERVED",
          "whitespace is not an answer")
    check(any("read back rather than opened" in line.lower()
              or "not opened" in line for line in good["unjudged"]),
          "and what WAS given is read back, never opened")

    print("\n7. nothing is built and nothing is run")
    imports = sorted(line.split()[1] for line in logic.split("\n")
                     if line.startswith("import "))
    check(imports == ["heron_fragment", "os", "sys"],
          "the whole import list is os, sys and the release list: %s"
          % ", ".join(imports))
    for reaching in ("subprocess", "socket", "urllib", "open(", "write("):
        check(reaching not in logic, "nothing here uses %s" % reaching)
    check(REG.VERSIONS is FRAG.REVIT_VERSIONS,
          "and the release list is HERON-FRG-VAL-001's object, not a copy")

    print("\n8. every declared failure is named and reached")
    for card, after, results, name in (
            (None, before(), GREEN, "NOTHING_TO_CHECK"),
            (before(), None, GREEN, "NOTHING_TO_CHECK"),
            (before(), "a string", GREEN, "NOT_A_FRAGMENT"),
            ("a string", before(), GREEN, "NOT_A_FRAGMENT"),
            (before(), before(revit=["2028"]), GREEN, "NOT_A_FRAGMENT"),
            (before(revit=[]), before(), GREEN, "NOT_A_FRAGMENT")):
        answer = REG.check(card, after, results, preserved=KEPT)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-FRG-REG-006.yaml"))
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
    print("PASS    Golden Rule 4 made executable, in that order")
    return 0


if __name__ == "__main__":
    sys.exit(main())
