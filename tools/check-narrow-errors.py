# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
A database fault must never be reported as an empty database.

    python tools/check-narrow-errors.py

Exits 1 on a finding, 0 when there is nothing to say.

WHY THIS EXISTS
---------------
D-52 calls it THE PLAUSIBLE ZERO: an answer that is wrong in a way nobody
doubts. `except sqlite3.OperationalError: return []` is its commonest shape in
this repository, because the honest case - the table has not been created yet -
really is normal, and the handler written for it quietly swallows everything
else as well:

    a LOCKED database          -> "nothing has been ingested"
    a MALFORMED file           -> "nothing has been ingested"
    a MISSING COLUMN           -> "nothing has been ingested"
    a schema older than the code -> "nothing has been ingested"

A reader told "no document is indexed in this scope - put one in" goes and
ingests a document into a broken store. Nothing in that sentence invites doubt,
which is exactly what makes it expensive.

REVIEWS FOUND THIS FOUR TIMES, IN FOUR FILES, AND THAT IS WHY IT IS A TOOL.
2026-09-11: heron_retrieve.documents() one round, heron_graph the next,
heron_search.index_chunks the next, heron_retrieve.find_documents the round
after - each one a copy of a line already corrected somewhere else. A defect
that keeps arriving one file at a time is not a defect any more, it is a shape,
and a shape is something a command can look for.

WHAT IT ASKS FOR
----------------
That every `except sqlite3.OperationalError` narrows on the message before it
swallows it - a `"no such table" not in str(exc)` (or "duplicate column", for
the ALTER TABLE migrations) followed by a `raise`. It does not care which
wording, only that the handler distinguishes the normal case from a fault.

It reads text rather than an AST on purpose: the rule is about what a person
maintaining this file will see beside the handler.
"""

from __future__ import print_function

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOOKED_AT = ("brain", "mcp", "tools")

# A handler has this many lines to narrow itself before this complains. Five is
# room for a comment and the two lines that do the work, and not room to hide a
# second behaviour.
WINDOW = 12

CATCHES = re.compile(r"^\s*except\s+(?:sqlite3\.)?OperationalError\b")
NARROWS = re.compile(r"not\s+in\s+str\(\s*\w+\s*\)|\.args\[0\]|raise\b")


def findings():
    """(path, line number, the handler) for every handler that swallows."""
    out = []
    for folder in LOOKED_AT:
        for here, _dirs, files in os.walk(os.path.join(ROOT, folder)):
            if "__pycache__" in here:
                continue
            for name in sorted(files):
                if not name.endswith(".py"):
                    continue
                path = os.path.join(here, name)
                lines = io.open(path, encoding="utf-8").read().splitlines()
                for i, line in enumerate(lines):
                    if not CATCHES.match(line):
                        continue
                    window = "\n".join(lines[i:i + WINDOW])
                    # Stop at the end of the handler: a `raise` belonging to
                    # the NEXT block is not this one narrowing itself.
                    indent = len(line) - len(line.lstrip())
                    kept = []
                    for later in lines[i + 1:i + WINDOW]:
                        if later.strip() and (len(later) - len(later.lstrip())) <= indent:
                            break
                        kept.append(later)
                    window = "\n".join(kept)
                    if not NARROWS.search(window):
                        out.append((os.path.relpath(path, ROOT), i + 1,
                                    line.strip()))
    return out


def main():
    found = findings()
    if not found:
        print("Every sqlite3.OperationalError handler narrows before it "
              "swallows.")
        print("A locked, malformed or out-of-date store cannot be reported as "
              "an empty one (D-52).")
        return 0

    print("%d handler(s) turn every database fault into an empty result:"
          % len(found))
    print()
    for path, line, text in found:
        print("  %s:%d" % (path, line))
        print("      %s" % text)
    print()
    print("Narrow it. The normal case is the table not existing yet:")
    print()
    print("      except sqlite3.OperationalError as exc:")
    print('          if "no such table" not in str(exc):')
    print("              raise")
    print("          return []")
    print()
    print("Anything else is a broken store, and D-52 is about exactly this: a")
    print("plausible zero is worse than a crash, because nobody goes looking.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
