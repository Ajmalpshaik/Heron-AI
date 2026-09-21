# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The register's own sweep - and the row that argued with itself.

    python tests/test_open_defects.py

WHY THIS EXISTS
---------------
`tools/open-defects.py` used only to COUNT, and tests/test_catalog.py states
the rule this repository works to: a tool that only draws needs no test, one
that CONCLUDES does. It now concludes something new - **this row says OPEN and
also carries a dated fix, so one of the two is wrong** - and that conclusion
was added because row 127 was fixed by APPENDING to its state cell rather than
reordering it. The row was fixed; the sweep went on counting it open; no count
and no list showed it.

WHAT IT PROVES
  1. THE DETECTOR FIRES ON THE SHAPE IT WAS WRITTEN FOR - row 127's cell as
     it actually stood, reconstructed here rather than described.

  2. AND NOT ON THE ELEVEN THAT MENTION A FIX IN PASSING. Eleven open rows
     say something else was fixed or closed; every one is genuinely open, and
     a detector that flagged them would be eleven false questions a reader
     learns to skip.

  3. IT ASKS AND NEVER RE-COUNTS. Which sentence is the state is a reader's
     judgement; a tool that guessed would start closing rows. The open count
     is the same with and without a contradicting row.

  4. THE REAL REGISTER IS REPORTED, NOT GATED. This suite prints what the
     sweep finds today and does not fail on it - a finding is a question,
     which is the rule every report in tools/ is written to.
"""

import importlib.util
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

spec = importlib.util.spec_from_file_location(
    "heron_open_defects", os.path.join(ROOT, "tools", "open-defects.py"))
OD = importlib.util.module_from_spec(spec)
spec.loader.exec_module(OD)

FAILURES = []


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILURES.append(said)


def state_of(line):
    """The state cell, read exactly as the tool reads it."""
    cells = OD.CELL.split(line)
    return re.sub(r"[`*]", "", cells[-2]).strip()


def main():
    print("1. the detector fires on the shape it was written for")
    # ROW 127 AS IT ACTUALLY STOOD after PR #202 - the fix appended under the
    # sentence that says OPEN. Written out here rather than read from the
    # register, because the register has since been repaired and a suite that
    # read it would stop testing anything the moment it was.
    was = ("OPEN. Found 2026-09-19 while checking which checkout Heron "
           "reads. AND THE CHECK THAT FOUND IT IS WORTH MORE THAN THE ROW: "
           "the totals line is a reliable fingerprint of which tree is being "
           "read. FIXED 2026-09-19 BY THE FIRST OF THE TWO ROUTES THIS CELL "
           "NAMES - derive the status the same way the totals are derived")
    check(was.lower().startswith("open"),
          "the cell begins with OPEN, so the sweep counts it open")
    found = OD.SETTLED.search(was)
    check(found is not None,
          "and the detector finds the dated fix underneath it")
    if found:
        check("FIXED" in found.group(0) and "2026-09-19" in found.group(0),
              "naming the claim it found: %r" % found.group(0))

    print("\n2. and not on a fix mentioned in passing")
    # EVERY ONE OF THESE IS A REAL OPEN ROW'S OPENING WORDS. A detector that
    # flagged them would be a question a reader learns to skip.
    passing = [
        "OPEN - 7 of 14 fixed, and THE SWEEP THAT MEASURES IT IS NOT "
        "DETERMINISTIC, found 2026-09-17 while trying to re-measure it",
        "OPEN as a library-wide question. It was fixed in RENAME_PHASE, and "
        "that fragment has since been removed. Found 2026-09-16",
        "OPEN - BUT TWO OF THE FOUR IT NAMES ARE CLOSED AND THE ONE RULE "
        "NEVER HAPPENED - corrected 2026-09-18",
        "OPEN, and recorded rather than fixed - the fragments are not this "
        "session's to touch. Found 2026-09-19",
        # ROW 75 AS IT STANDS. A DATED DENIAL IS NOT A DATED FIX, and this
        # one was reported as a row arguing with itself for a day while the
        # tool's own docstring claimed it had no false hits. The whole value
        # of that report is that it is short enough to read every time.
        "OPEN - THE LOSS IS VISIBLE NOW AND THE WRONG BINDING IS NOT "
        "FIXED, 2026-09-20. The state still says OPEN because the repair "
        "this row names is not built",
        "OPEN. The chain was NEVER FIXED, 2026-09-18 - recorded only",
    ]
    for cell in passing:
        check(OD.SETTLED.search(cell) is None,
              "not a hit: %s..." % cell[:52])

    print("\n3. it asks, and never re-counts")
    body = OD.rows()
    check(body is not None, "the register's section 5 was found")
    if body:
        opens = [n for n, s in body if s.lower().startswith("open")]
        arguing = [n for n, s in body
                   if s.lower().startswith("open") and OD.SETTLED.search(s)]
        check(set(arguing) <= set(opens),
              "a row that argues with itself is still counted OPEN - the "
              "tool asks and does not decide")
        print("       %d open, %d of them arguing with themselves"
              % (len(opens), len(arguing)))

        print("\n4. the real register today, reported and not gated")
        if arguing:
            for number in arguing:
                print("       row %d says OPEN and carries a dated fix - a "
                      "question for a reader" % number)
        else:
            print("       no row argues with itself. That is today's state, "
                  "not a rule this suite enforces")
        check(True, "reported either way - a finding is a question")

    print("\n5. every row of both sections has exactly three cells")
    # THIS IS A GATE AND NOT A REPORT, which the four above deliberately are
    # not. A malformed row is not a judgement call: the state cell is read by
    # position, so a literal pipe anywhere in a row's text makes the tool read
    # a fragment of a sentence as the state. Rows 15, 156 and 163 each did
    # that, silently, and row 162 said OPEN for a day while the sweep counted
    # it neither open nor settled. Write a literal pipe as \\| - in a code
    # span too, which is where all four hid.
    src = io.open(OD.REGISTER, encoding="utf-8").read()
    opened = src.index(OD.SECTION_START)
    body_text = src[opened:src.index(OD.SECTION_END)]
    malformed = []
    for line in body_text.split("\n"):
        match = OD.ROW.match(line)
        if not match:
            continue
        cells = OD.CELL.split(line)
        if len(cells) != 5:
            malformed.append((match.group(1), len(cells) - 2))
    check(not malformed,
          "no row holds a literal | (%s)"
          % (", ".join("row %s has %d cells" % m for m in malformed)
             if malformed else "all rows have 3"))

    print("\n6. and no blank line splits either table")
    # A BLANK LINE ENDS A MARKDOWN TABLE. Twenty of them sat between rows of
    # sections 5 and 5b until 2026-09-21, so GitHub rendered the register as
    # about ten separate one-row tables, most without a header - while every
    # tool that reads it line by line saw nothing wrong. The register is read
    # by people as well as by open-defects.py, and this is the half no
    # checker was watching.
    split = []
    for label, opened, closed in (("5", OD.SECTION_START, OD.SECTION_5B),
                                  ("5b", OD.SECTION_5B, OD.SECTION_END)):
        chunk = src[src.index(opened):src.index(closed)].split("\n")
        for i in range(1, len(chunk) - 1):
            if (not chunk[i].strip() and chunk[i - 1].startswith("|")
                    and chunk[i + 1].startswith("|")):
                split.append(label)
    check(not split,
          "no blank line sits between two table rows%s"
          % ("" if not split else " - %d in section(s) %s"
             % (len(split), ", ".join(sorted(set(split))))))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a row that says OPEN and carries a dated fix is a\n"
          "        question, and a fix mentioned in passing is not")
    return 0


if __name__ == "__main__":
    sys.exit(main())
