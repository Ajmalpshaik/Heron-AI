#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The safety rails around the first write. Runs without Revit.

WHAT THIS PROVES AND WHAT IT DOES NOT.

Step 6 is split across two machines by force: the add-in half is C# that only
Revit can run. This paragraph said it "has never been compiled, let alone
executed", which was true when it was written and is the FOURTH place that
sentence outlived its facts - the .NET SDK turned out to be one
`apt-get install dotnet-sdk-10.0` away, `tools/check-compile.py` builds every
project on Revit 2020 through 2027, and NEEDS-CHECKING records the write path
moving three ducts under a single undo entry. FRAGMENT-ISSUES section 5b rows
9 and 26.

What has NOT happened is the measurement: D3, "move them, then MEASURE one",
is not struck through. This file tests the half that can be tested without
Revit at all, and it is deliberate about which half that is:

    TESTED HERE   reading a distance a person typed, and the document pin
                  and single-use approval that decide whether a write is
                  offered at all.

    NOT TESTED    the transaction group, the rollback, the re-count against
                  a live model, and the move itself. Nothing in this file
                  says anything about those. See HANDOVER section 6.

The distance parser is the one that earns its tests hardest. It is where a
sentence a person typed becomes a number that moves a building, and its bad
failure is not an exception - it is a model that moved by a thousand times
what was meant and looks perfectly fine until somebody measures it.

    python tests/test_write_safety.py
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

from heron_write import (                                  # noqa: E402
    BadDistance, DocumentPin, PendingApproval, describe, describe_vertical,
    parse_millimetres)

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def refuses(text, because):
    try:
        parse_millimetres(text)
        check(False, "refuses %r - %s" % (text, because))
    except BadDistance:
        check(True, "refuses %r - %s" % (text, because))


def reads(text, expected):
    try:
        actual = parse_millimetres(text)
        check(abs(actual - expected) < 1e-9,
              "%r is %s" % (text, describe(expected)))
    except BadDistance as bad:
        check(False, "%r should be %s, but was refused: %s" % (text, expected, bad))


def main():
    print("Distances a person actually types")
    reads("200", 200.0)                 # bare number is mm, the house convention
    reads("200 mm", 200.0)
    reads("200mm", 200.0)
    reads("  200  ", 200.0)
    reads("-50", -50.0)                 # down is a direction, not an error
    reads("200.5", 200.5)
    reads("0.5 m", 500.0)
    reads("20 cm", 200.0)
    reads("1,200", 1200.0)              # someone typed a thousands separator
    reads("1,234,567 mm", 1234567.0)    # several groups, still unambiguous
    reads("1,000.5 mm", 1000.5)         # grouping AND a decimal point
    reads("+200", 200.0)

    print()
    print("Distances that must be refused rather than guessed at")
    refuses("", "nothing was said")
    refuses(None, "nothing was said")
    refuses("up a bit", "not a number at all")
    refuses("200 furlongs", "not a unit Heron knows")
    refuses("0", "moving by zero changes nothing")
    refuses("0.0000001", "rounds to no movement")
    refuses("200000001", "past the 100 km ceiling")
    refuses("nan", "NaN never reaches a transaction")
    refuses("inf", "infinity never reaches a transaction")
    refuses("2 ft", "feet are refused, not converted")
    refuses('6"', "inches are refused, not converted")
    refuses("200 200", "two numbers is not a distance")

    # A DECIMAL COMMA IS NOT A THOUSANDS SEPARATOR, and the parser used to
    # delete every comma without asking which it was. Measured 2026-09-21,
    # before the fix: "1,5 m" gave 15000.0 mm where "1.5 m" gives 1500.0 -
    # TEN TIMES, and it passed every check after it because 15 metres is in
    # range, is not zero and is not NaN. Half of Europe writes a decimal that
    # way. FRAGMENT-ISSUES section 5b, row 24.
    refuses("1,5 m", "a decimal comma, and dropping it is ten times the move")
    refuses("0,5 m", "the same, and it cleared every other check")
    refuses("2,54 cm", "the same, in the unit an inch is usually converted to")
    refuses("1,5", "the same with no unit, where mm is assumed")

    print()
    print("A refusal explains the unit rather than just saying no")
    try:
        parse_millimetres("2 ft")
        check(False, "feet: refused with an explanation")
    except BadDistance as bad:
        check("millimetres" in str(bad).lower(),
              "feet: the refusal says which unit to use instead")

    print()
    print("Document pinning - Golden Rule 20")
    tower = {"document": "Tower A", "documentPath": r"C:\jobs\Tower A.rvt"}
    annexe = {"document": "Annexe", "documentPath": r"C:\jobs\Annexe.rvt"}

    pin = DocumentPin()
    check(pin.check(tower) is None, "first sight of a model pins it, silently")
    check(pin.check(tower) is None, "the same model again is fine")

    refusal = pin.check(annexe)
    check(refusal is not None, "a DIFFERENT model in front is refused")
    check("Tower A" in (refusal or "") and "Annexe" in (refusal or ""),
          "the refusal names BOTH models, so the user knows which is which")
    check("Nothing has been sent to Revit" in (refusal or ""),
          "the refusal answers 'did it half-do something?' unasked")
    check("click back" not in (refusal or "").lower(),
          "it does NOT say 'click back' - from here it cannot know the old model is still open")

    print()
    print("Two models called Project1 - the case that actually happened")
    one = {"document": "Project1", "documentPath": r"C:\a\Project1.rvt"}
    two = {"document": "Project1", "documentPath": r"C:\b\Project1.rvt"}
    pin = DocumentPin()
    pin.check(one)
    check(pin.check(two) is not None,
          "same title, different file -> refused, because title is not identity")

    print()
    print("An unsaved model still pins, as loosely as it deserves")
    draft = {"document": "Project1", "documentPath": None}
    pin = DocumentPin()
    check(pin.check(draft) is None, "an unsaved model pins on its title")
    check(pin.check({"document": "Something else", "documentPath": None}) is not None,
          "and a different unsaved title is still refused")

    print()
    print("Saving a model does not un-pin it")
    pin = DocumentPin()
    pin.check(tower)
    renamed = {"document": "Tower A - Rev B", "documentPath": r"C:\jobs\Tower A.rvt"}
    check(pin.check(renamed) is None, "same file, new title -> still the same document")
    check(pin.title == "Tower A - Rev B", "and the pin learns the new title")

    print()
    print("Repinning is possible, but only deliberately")
    pin = DocumentPin()
    pin.check(tower)
    pin.repin(annexe)
    check(pin.check(annexe) is None, "after repinning, the new model is the pinned one")
    check(pin.check(tower) is not None, "and the old one is now the refused one")

    print()
    print("Approval is single use")
    pending = PendingApproval()
    pending.offer("abc123", "move 4 ducts up 200 mm")
    token, summary = pending.take()
    check(token == "abc123", "the token offered is the token taken")
    check(summary == "move 4 ducts up 200 mm", "and what the user was shown comes with it")

    again, _ = pending.take()
    check(again is None,
          "a second take gets nothing - one 'yes' must not move the ducts twice")

    print()
    print("Nothing to approve is not the same as approving nothing")
    fresh = PendingApproval()
    check(fresh.take() == (None, None), "an unoffered approval yields no token")

    print()
    print("Down is a direction, not a negative up")
    # THE RULE, NOT THE SENTENCE. These check that a lowering move never
    # describes itself with a minus sign and never calls itself "up" - not
    # that any particular wording survives. A test quoting the whole phrase
    # would fail the next time somebody improves it, which is how a test
    # comes to be edited rather than read.
    #
    # Found in front of a model on 2026-09-12 proving D6, the check that
    # exists precisely because a negative distance must not be an error. It
    # was not an error: it answered "move 9 ducts up -50 mm" and named Revit's
    # own undo entry the same way.
    down = describe_vertical(-50)
    check("down" in down, "a downward move says down (%r)" % down)
    check("-" not in down, "and carries no minus sign - the sign IS the word")
    check("up" not in down, "and never says up")

    up = describe_vertical(200)
    check(up.startswith("up ") and "200" in up,
          "an upward move still says up (%r)" % up)

    # Zero has no direction to claim. "up 0 mm" would name one it does not have.
    still = describe_vertical(0)
    check("up" not in still and "down" not in still,
          "a zero move claims no direction (%r)" % still)

    # The magnitude is the same either way - only the word differs, so a
    # reader comparing two replies is comparing distances, not signs.
    check(describe(50) in describe_vertical(-50)
          and describe(50) in describe_vertical(50),
          "and the distance itself reads identically in both directions")

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("PASSED - the chat half of Step 6's rails.")
    print("The add-in half is untested by construction: see HANDOVER section 6.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
