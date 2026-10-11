# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The fragments that read dates typed on sheets and views share ONE date parser.

    python tests/test_sheet_date_parser.py

WHY. LIST_DATES_ON_SHEETS, FIND_DATES_IN_VIEWS and
CREATE_REVISIONS_FROM_SHEET_DATES each carry the same parser - a day, a written
month, a 2 or 4 digit year, a calendar check, a numeric month refused - copied
character for character, because a fragment is compiled on its own and cannot
call another. Three copies of one rule drift: the day one of them learns
"Sept" or stops refusing 03-04-2026, LIST_DATES_ON_SHEETS shows the modeller
one set of dates and CREATE_REVISIONS_FROM_SHEET_DATES makes revisions from
another. So the block from `var months` to the end of `datesIn` must be the
same text in all three.

It also holds the rule the copies exist to keep: a numeric month refused, and
the calendar check present.

EXTRACT_DATES_FROM_TEXT has the same rules in a different layout (its copy is
indented inside a block) and is not held here: the copies that must agree to
the character are the ones that read the same notes for the same job.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
COPIES = ("list-dates-on-sheets", "find-dates-in-views", "create-revisions-from-sheet-dates")
START = "var months = new Dictionary"
LAMBDA = "datesIn = text =>"


def parser_of(slug):
    path = os.path.join(FRAGMENTS, slug, "impl", "any", "fragment.cs")
    # Normalised at the read: a CRLF checkout must not fail a compare.
    with io.open(path, encoding="utf-8") as handle:
        text = handle.read().replace("\r\n", "\n")
    if text.count(START) != 1 or text.count(LAMBDA) != 1:
        return None
    start = text.index(START)
    end = text.index("\n};", text.index(LAMBDA)) + 3
    return text[start:end]


def main():
    failures = []
    blocks = {}
    for slug in COPIES:
        block = parser_of(slug)
        if block is None:
            failures.append("%s: needs exactly one '%s' and one '%s'" % (slug, START, LAMBDA))
        else:
            blocks[slug] = block

    reference = COPIES[0]
    if reference in blocks:
        for slug in COPIES[1:]:
            if slug not in blocks or blocks[slug] == blocks[reference]:
                continue
            a, b = blocks[reference].split("\n"), blocks[slug].split("\n")
            for number, (left, right) in enumerate(zip(a, b), 1):
                if left != right:
                    failures.append("%s's parser differs from %s's at line %d:\n  %s\n  %s"
                                    % (slug, reference, number, left.strip(), right.strip()))
                    break
            else:
                failures.append("%s's parser differs from %s's in length: %d against %d lines"
                                % (slug, reference, len(b), len(a)))

        rules = {
            "if (!months.ContainsKey(key)) continue;": "a month must be a written one - a numeric month is refused",
            "if (day < 1 || day > lastDay) continue;": "the calendar check - 31 SEP is not a date",
        }
        for needle, why in rules.items():
            if needle not in blocks[reference]:
                failures.append("the parser lost '%s' - %s" % (needle, why))

    if failures:
        for failure in failures:
            print("FAIL: " + failure)
        return 1
    print("PASS: %d fragments carry one date parser, character for character" % len(COPIES))
    return 0


if __name__ == "__main__":
    sys.exit(main())
