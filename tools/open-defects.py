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
SECTION_END = "## 6. WHAT CANNOT BE RUN AT ALL"

ROW = re.compile(r"^\|\s*\*{0,2}(\d+)\*{0,2}\s*\|")

# A DATED CLAIM IN CAPITALS, not the word in passing. `[^.]` keeps it inside
# one sentence, so "7 of 14 fixed" followed three sentences later by a date
# is not a hit - measured against every row in the register: no false ones.
SETTLED = re.compile(r"\b(FIXED|CLOSED)\b[^.]{0,40}?\d{4}-\d{2}-\d{2}")


def rows():
    """Every (id, state) pair in section 5, in the order they are written."""
    with open(REGISTER, encoding="utf-8") as handle:
        src = handle.read()

    try:
        body = src[src.index(SECTION_START):src.index(SECTION_END)]
    except ValueError:
        # The headings moved. Say so rather than silently reporting zero -
        # a register that reports "no open defects" because it could not find
        # the register is the worst answer available.
        sys.stdout.write(
            "Could not find section 5 in docs/FRAGMENT-ISSUES.md.\n"
            "  Looked for: %r ... %r\n"
            "  The headings have moved. Fix this tool, do not trust it.\n"
            % (SECTION_START, SECTION_END))
        return None

    found = []
    for line in body.split("\n"):
        match = ROW.match(line)
        if not match:
            continue
        cells = line.split("|")
        if len(cells) < 3:
            continue
        state = re.sub(r"[`*]", "", cells[-2]).strip()
        found.append((int(match.group(1)), state))
    return found


def main():
    show_all = "--all" in sys.argv
    found = rows()
    if found is None:
        return 0

    if not found:
        sys.stdout.write("No rows matched in section 5. Check the pattern "
                         "before believing this.\n")
        return 0

    is_open = [(n, s) for n, s in found if s.lower().startswith("open")]
    arguing = [(n, s, SETTLED.search(s).group(0))
               for n, s in is_open if SETTLED.search(s)]

    out = sys.stdout.write
    out("Heron's own defects - docs/FRAGMENT-ISSUES.md section 5\n")
    out("=" * 62 + "\n\n")

    if show_all:
        for number, state in found:
            mark = "OPEN" if state.lower().startswith("open") else "    "
            out("  %s %3d  %s\n" % (mark, number, state[:96]))
        out("\n")
    else:
        for number, state in is_open:
            out("  %3d  %s\n" % (number, state[:100]))
        out("\n")

    out("  rows in the section : %d\n" % len(found))
    out("  still OPEN          : %d\n" % len(is_open))
    out("  ids                 : %s\n"
        % ", ".join(str(n) for n, _ in is_open))
    out("\n")
    if arguing:
        out("  ROWS THAT ARGUE WITH THEMSELVES - the state begins with OPEN\n"
            "  and the same cell carries a dated fix. One of the two is\n"
            "  wrong, and which one is a reader's call:\n")
        for number, _state, claim in arguing:
            out("    %3d  says OPEN, and also %r\n" % (number, claim))
        out("\n")
    out("Read the ids, not the count. A row can say OPEN after a later row\n"
        "has closed it - four did on 2026-09-16 - and no pattern sees that.\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
