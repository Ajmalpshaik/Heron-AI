# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
How many of Heron's own defects are still open, and WHICH ones.

    python tools/open-defects.py            # the open ones, by id
    python tools/open-defects.py --all      # every row, with its state

Always exits 0. This reports; it does not gate.

WHY THIS EXISTS
---------------
Section 5 of FRAGMENT-ISSUES.md was headed "six still open" from the day it
was written until 2026-09-16. By then ninety-four rows had been appended and
twenty-nine of them were open. Every single append was careful - each session
wrote an honest row and left the heading to somebody else - which is what makes
the drift recurrent rather than sloppy.

NEEDS-CHECKING.md has recorded this same failure against itself seven times,
and named the cure each time: A PROSE TOTAL IS A CACHE WITH NO INVALIDATION.
It also named the discipline that survives a bad pattern - PRINT THE IDS AND
READ THEM, because every drift that file caught was visible in the list and
invisible in the number.

So this prints the ids. The count at the end is a convenience; the list above
it is the answer.

WHAT COUNTS AS OPEN
-------------------
The Status cell - the last column - beginning with the word "open", in any
case. That is the register's own convention and it is honoured literally
rather than interpreted: a row whose state has been corrected to FIXED,
WITHDRAWN or NOT A DEFECT drops out, and a row hedged as "OPEN as a question"
or "OPEN, and it is the owner's call" stays in, because it is.

A ROW THAT CONTRADICTS ITSELF IS REPORTED SEPARATELY
-----------------------------------------------------
A cell can be appended to rather than reordered - the fix written under the
sentence that says OPEN - and then the row is fixed while the sweep, which
reads the first word, goes on counting it. Row 127 did exactly that between
2026-09-19 and the same evening, and no count or list showed it.

So a state that BEGINS with "open" and also carries a DATED `FIXED` or
`CLOSED` claim is printed as a question. The test is deliberately narrow: a
dated claim in capitals, not the word "fixed" in passing, because eleven open
rows mention something else being fixed or closed and every one of them is
genuinely open. **It asks; it never re-counts**, because which sentence is the
state is a reader's judgement and a tool that guessed would start closing rows.

THE ONE THING THIS CANNOT SEE is a row whose state cell still says OPEN when a
LATER row has closed it. Four of those were found by hand on 2026-09-16 (rows
37, 44, 96 and 97) and there is no pattern that finds them, because the closure
is written in a different row in prose. Reading beats grepping here. This tool
narrows the pile you have to read; it does not replace reading it.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REGISTER = os.path.join(ROOT, "docs", "FRAGMENT-ISSUES.md")

SECTION_START = "## 5. HERON'S OWN DEFECTS"
SECTION_5B = "## 5b. HERON'S OWN DEFECTS found by reading"
SECTION_END = "## 6. WHAT CANNOT BE RUN AT ALL"

# TWO REGISTERS, COUNTED APART. Section 5 is what PROVING against a model
# found; 5b is what READING the repository file by file found. A reader asking
# "what did the sweep turn up" must not have to subtract one from the other,
# and the two go stale differently - a proving defect is re-tested by running
# the fragment, a reading defect by reading the file again.
#
# Each section names EVERY heading that could follow it, not just the next one.
# Section 5 ends at 5b when 5b is there and at section 6 when it is not, so
# inserting 5b did not silently make section 5 swallow it - which would have
# merged two id spaces that both start at 1.
SECTIONS = [
    ("5", SECTION_START, [SECTION_5B, SECTION_END]),
    ("5b", SECTION_5B, [SECTION_END]),
]

ROW = re.compile(r"^\|\s*\*{0,2}(\d+)\*{0,2}\s*\|")

# A DATED CLAIM IN CAPITALS, not the word in passing. `[^.]` keeps it inside
# one sentence, so "7 of 14 fixed" followed three sentences later by a date
# is not a hit - measured against every row in the register: no false ones.
SETTLED = re.compile(r"\b(FIXED|CLOSED)\b[^.]{0,40}?\d{4}-\d{2}-\d{2}")

# A CELL BOUNDARY IS AN UNESCAPED PIPE. Markdown lets a table cell hold a
# literal pipe as `\|`, and a plain split("|") chops the cell there - so the
# LAST cell of such a row is a fragment of a sentence rather than the state,
# and the row can never start with the word OPEN. That is not theory: row 162
# said OPEN in writing for a day, wrote `ls tests/test_*.py \| wc -l` in the
# same cell, and this tool reported it as neither open nor settled. A register
# that cannot see one of its own open rows is the failure it exists to prevent.
CELL = re.compile(r"(?<!\\)\|")


def rows(start=None, ends=None, label="5"):
    """Every (id, state) pair in one section, in the order they are written.

    Called with no arguments it reads section 5, which is what it has always
    done and what tests/test_open_defects.py calls.
    """
    start = SECTION_START if start is None else start
    ends = [SECTION_5B, SECTION_END] if ends is None else ends

    with open(REGISTER, encoding="utf-8") as handle:
        src = handle.read()

    # The headings moved. Say so rather than silently reporting zero - a
    # register that reports "no open defects" because it could not find the
    # register is the worst answer available.
    if start not in src:
        sys.stdout.write(
            "Could not find section %s in docs/FRAGMENT-ISSUES.md.\n"
            "  Looked for: %r\n"
            "  The heading has moved. Fix this tool, do not trust it.\n"
            % (label, start))
        return None

    opened = src.index(start)
    after = [src.index(e) for e in ends if e in src and src.index(e) > opened]
    if not after:
        sys.stdout.write(
            "Found section %s but nothing that ends it.\n"
            "  Looked for any of: %s\n"
            "  Reading to the end of the file would sweep in later sections,\n"
            "  so this reports nothing rather than a wrong number.\n"
            % (label, ", ".join(repr(e) for e in ends)))
        return None

    body = src[opened:min(after)]

    found = []
    for line in body.split("\n"):
        match = ROW.match(line)
        if not match:
            continue
        cells = CELL.split(line)
        if len(cells) < 3:
            continue
        # THREE CELLS, OR SAY SO. A row with a fourth cell has a literal pipe
        # somewhere in its text, and the state this tool then reads is a
        # fragment of a sentence rather than the state. That is not a
        # hypothetical: rows 15, 156 and 163 all read from the wrong cell
        # until 2026-09-21, and one of them reported FIXED off the back of it.
        # Complaining is the point - a silent misread is how a register loses
        # sight of its own rows.
        if len(cells) != 5:
            sys.stdout.write(
                "  MALFORMED  row %s of section %s has %d cells, not 3.\n"
                "             A literal | in the text splits it - write it as"
                " \\| - and\n"
                "             until it is fixed the state below is read from"
                " the wrong cell.\n"
                % (ident(label, int(match.group(1))), label, len(cells) - 2))
        state = re.sub(r"[`*]", "", cells[-2]).strip()
        found.append((int(match.group(1)), state))
    return found


def ident(label, number):
    """Section 5's rows have always been called by their bare number, and are
    referred to that way all over this repository. 5b's carry their section,
    because `3` alone would now be ambiguous and a defect nobody can find is
    the one failure this register exists to prevent."""
    return str(number) if label == "5" else "%s-%d" % (label, number)


def report(label, found, show_all):
    """Print one section and return how many of its rows are open."""
    out = sys.stdout.write
    out("Heron's own defects - docs/FRAGMENT-ISSUES.md section %s\n" % label)
    out("=" * 62 + "\n\n")

    if not found:
        out("  No rows in this section yet.\n\n")
        if label == "5b":
            out("  THAT IS NOT THE SAME AS NOTHING BEING WRONG. This section\n"
                "  fills as the repository is read, and an empty table means\n"
                "  nothing until you know how much reading has been done:\n\n"
                "      python tools/review-ledger.py\n\n")
        else:
            out("  No rows matched the pattern. Check it before believing "
                "this.\n\n")
        return 0, 0, []

    is_open = [(n, s) for n, s in found if s.lower().startswith("open")]
    arguing = [(n, s, SETTLED.search(s).group(0))
               for n, s in is_open if SETTLED.search(s)]

    if show_all:
        for number, state in found:
            mark = "OPEN" if state.lower().startswith("open") else "    "
            out("  %s %-5s  %s\n" % (mark, ident(label, number), state[:96]))
        out("\n")
    else:
        for number, state in is_open:
            out("  %-5s  %s\n" % (ident(label, number), state[:100]))
        out("\n")

    out("  rows in the section : %d\n" % len(found))
    out("  still OPEN          : %d\n" % len(is_open))
    out("  ids                 : %s\n"
        % ", ".join(ident(label, n) for n, _ in is_open))
    out("\n")
    if arguing:
        out("  ROWS THAT ARGUE WITH THEMSELVES - the state begins with OPEN\n"
            "  and the same cell carries a dated fix. One of the two is\n"
            "  wrong, and which one is a reader's call:\n")
        for number, _state, claim in arguing:
            out("    %-5s  says OPEN, and also %r\n"
                % (ident(label, number), claim))
        out("\n")
    return len(is_open), len(found), [ident(label, n) for n, _ in is_open]


def main():
    show_all = "--all" in sys.argv
    out = sys.stdout.write

    total, seen, all_rows, all_ids = 0, 0, 0, []
    for label, start, ends in SECTIONS:
        found = rows(start, ends, label)
        if found is None:
            continue          # rows() has already said which heading moved
        seen += 1
        section_open, section_rows, section_ids = report(label, found, show_all)
        total += section_open
        all_rows += section_rows
        all_ids.extend(section_ids)
        out("\n")

    if not seen:
        return 0

    # THE AGGREGATE, ON ITS OWN LINES AND NAMED SO IT CAN BE PARSED.
    # tools/balance-of-work.py reads this output with re.search, which takes
    # the FIRST match - so the moment section 5b arrived beside section 5, the
    # dashboard silently began reporting section 5's numbers as the whole
    # truth: 33 of 163 where the real figures were larger, with every 5b id
    # missing. A consumer reading a per-section line was never going to
    # survive a second section. These three lines are the contract; the
    # per-section blocks above stay for a human to read.
    # Reported by a Codex review on PR #219.
    out("  TOTAL rows, all sections  : %d\n" % all_rows)
    out("  TOTAL ids, all sections   : %s\n" % (", ".join(all_ids) or "(none)"))
    out("  OPEN across both sections : %d\n\n" % total)
    out("Read the ids, not the count. A row can say OPEN after a later row\n"
        "has closed it - four did on 2026-09-16 - and no pattern sees that.\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
