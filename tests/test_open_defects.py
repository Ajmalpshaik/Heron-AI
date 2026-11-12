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

  5-7. THREE THINGS THAT ARE GATES: every row has three cells, no blank line
     splits a table, and no number is used twice.

  8. A NUMBER IS USED TWICE WHEREVER ITS TWO ROWS ARE KEPT. Since 2026-11-12
     a new row of section 5b is a file of its own, so the same number can sit
     in a rows file on one branch and in row-5b-N.md on another, and merge
     without a conflict. Check 7 has to see that pair, and is shown here to -
     on a register built in a scratch folder, never the real one. (Two
     branches that both write row-5b-N.md collide in git before any suite
     runs: one name, two files.)

  A REGISTER THAT CANNOT BE READ FAILS HERE, IN A SENTENCE. Checks 5 to 7
  read the register as one text; when the reader refuses it - a row's own
  file that is not what its name says - this suite says so and names
  check-docs.py, where it used to die with AttributeError.
"""

import collections
import importlib.util
import io
import os
import re
import shutil
import sys
import tempfile

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


def used_twice():
    """The ids used by more than one row, in either section, of the register
    OD.REGISTER names - read as the tool reads it."""
    twice = []
    for label, start, ends in OD.SECTIONS:
        found = OD.rows(start, ends, label) or []
        seen_ids = collections.Counter(number for number, _ in found)
        twice.extend(OD.ident(label, number)
                     for number, count in sorted(seen_ids.items())
                     if count > 1)
    return twice


NL = chr(10)
ROWS_FILE = "section-5b-rows-001-025.md"


def scratch_register(top, in_rows_file, own_files):
    """A small register in TOP/docs: section 5b's rows IN_ROWS_FILE in its one
    rows file, and one file each for the numbers in OWN_FILES, under a page
    that says rows from 3 on are one file each."""
    folder = os.path.join(top, "docs", "fragment-issues")
    if os.path.isdir(os.path.join(top, "docs")):
        shutil.rmtree(os.path.join(top, "docs"))
    os.makedirs(folder)
    head = ["| # | Defect | State |", "|---|---|---|"]
    page = NL.join([
        "# The register", "",
        OD.SECTION_START + " found by proving", "",
        "| # | Defect | State |", "|---|---|---|",
        "| **1** | **One.** | OPEN - recorded. |", "",
        OD.SECTION_5B + " the repository, file by file", "",
        "**Its rows, in order:** [001-025](fragment-issues/%s)" % ROWS_FILE, "",
        "**Its rows from 3 on, one file each:** [`fragment-issues/row-5b-NNNN.md`](fragment-issues/)", "",
        OD.SECTION_END, "", "Nothing.", ""])
    files = {ROWS_FILE: NL.join(["# Rows", ""] + head + [
        "| **%d** | **Row %d, in the rows file.** | OPEN - recorded. |" % (n, n) for n in in_rows_file] + [""])}
    for n in own_files:
        files["row-5b-%d.md" % n] = NL.join(["# Row 5b-%d" % n, ""] + head + [
            "| **%d** | **Row %d, in a file of its own.** | OPEN - recorded. |" % (n, n), ""])
    with io.open(os.path.join(top, "docs", "FRAGMENT-ISSUES.md"), "w", encoding="utf-8", newline="") as handle:
        handle.write(page)
    for name, text in files.items():
        with io.open(os.path.join(folder, name), "w", encoding="utf-8", newline="") as handle:
            handle.write(text)
    return os.path.join(top, "docs", "FRAGMENT-ISSUES.md")


def across_layouts():
    """Check 8. Each check FAILS, and does not raise, on a reader that does
    not read a row's own file: there the two rows of one number are never in
    one text, and nothing is used twice."""
    real, top = OD.REGISTER, tempfile.mkdtemp()
    try:
        OD.REGISTER = scratch_register(top, [1, 2], [3, 4])
        found = OD.rows(OD.SECTION_5B, [OD.SECTION_END], "5b") or []
        check([number for number, _ in found] == [1, 2, 3, 4],
              "rows kept one to a file are read with the rows file's, each once (%s)"
              % ", ".join("5b-%d" % number for number, _ in found))
        check(used_twice() == [], "and no number is used twice")

        OD.REGISTER = scratch_register(top, [1, 2, 3], [3, 4])
        found = OD.rows(OD.SECTION_5B, [OD.SECTION_END], "5b") or []
        check([number for number, _ in found].count(3) == 2,
              "a number in the rows file and in a file of its own is read as two rows")
        check(used_twice() == ["5b-3"],
              "and check 7's own count names it: %s" % (", ".join(used_twice()) or "nothing repeated"))

        OD.REGISTER = scratch_register(top, [1, 2, 4, 4], [3])
        check(used_twice() == ["5b-4"],
              "as it names a number used twice in one rows file, which it always did: %s"
              % (", ".join(used_twice()) or "nothing repeated"))
    finally:
        OD.REGISTER = real
        shutil.rmtree(top, ignore_errors=True)


def three_gates(src):
    """Checks 5 to 7, on SRC - the register as one text."""
    print("\n5. every row of both sections has exactly three cells")
    # THIS IS A GATE AND NOT A REPORT, which the four above deliberately are
    # not. A malformed row is not a judgement call: the state cell is read by
    # position, so a literal pipe anywhere in a row's text makes the tool read
    # a fragment of a sentence as the state. Rows 15, 156 and 163 each did
    # that, silently, and row 162 said OPEN for a day while the sweep counted
    # it neither open nor settled. Write a literal pipe as \\| - in a code
    # span too, which is where all four hid.
    # READ AS THE TOOL READS IT - the register as one text, every section's
    # file back in its place - since the rows of 5 and 5b have their own
    # files and the page alone holds none of them.
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

    print("\n7. and no number is used twice in either table (5b-163)")
    # A GATE, FOR THE REASON CHECK 5 IS ONE. Two sessions working at once each
    # appended rows 158 and 159 to section 5b on 2026-09-23, and every gate
    # and suite exited 0 on the tree that kept both - so "row 5b-158" named
    # two rows. test_owner_queue already refuses the same clash in
    # NEEDS-CHECKING; this is the register's half of it.
    twice = used_twice()
    check(not twice,
          "every row number is used once%s"
          % ("" if not twice else " - repeated: %s" % ", ".join(twice)))


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

    # A REGISTER THAT CANNOT BE READ IS A FAILED CHECK, NOT A TRACEBACK. One
    # row's own file that is not what its name says stops the reader
    # (RegisterBroken), `OD.source()` is then None, and until 2026-11-12 the
    # next line called .index on it: this suite died with AttributeError and
    # named nothing. open-defects.py itself says "Could not read the
    # register" and exits 0 (row 5b-1587), so this is where it turns red.
    src = OD.source()
    if src is None:
        check(False, "the register could not be read, so checks 5 to 7 read nothing - "
                     "run python tools/check-docs.py, which names the file that is wrong")
    else:
        three_gates(src)

    print("\n8. and a number is used twice wherever its two rows are kept (5b-1585)")
    across_layouts()

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
