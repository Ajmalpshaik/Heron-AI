#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
What `revit_change` tells the user a write actually did.

WHY THIS FILE EXISTS
--------------------
Until 2026-09-16 it told them nothing. `heron_mcp_server.py` printed
`"<CAPABILITY> ran in <model>"` and then looked for the detail under
`reply.get("answer")` - A KEY NOTHING EMITS, on either side of the bridge.

  * `RevitFragment.Report` leaves `provides`, `providesCount`, `bound`, `ran`,
    `document`.
  * `WithVerdict` adds `applied`, `rolledBack` and `verdict`.
  * Nothing, anywhere, writes `answer`.

So the value was always `None`, and EVERY write through that tool reported
only that it had run. Measured: `RENAME_PHASE` answered `"RENAME_PHASE ran in
PIPE."` while the fragment had produced `renamed: false` and a refusal carrying
Revit's own words - *"This element does not support assignment of a
user-specified name."* The model was untouched and the tool said nothing about
it either way. FRAGMENT-ISSUES row 111.

THE POINT OF TESTING IT AS TEXT
--------------------------------
`revit_change` needs a live bridge and the MCP SDK to call, so every other test
here reads the server as TEXT and asserts on what it contains. That is a weak
test of behaviour and a STRONG test of this particular defect, which was a
field name and nothing else.

AND IT MUST FAIL ON THE OLD KEY, or it is decoration. Row 111's own state line
asks for that: without it the next rename of a reply field puts the bug
straight back, silently, exactly as it arrived.

    python tests/test_change_reporting.py
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

FAILURES = []


def check(condition, what):
    if not condition:
        FAILURES.append(what)


def main():
    server = io.open(os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py"),
                     encoding="utf-8").read()
    fragment = io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin",
                                    "RevitFragment.cs"), encoding="utf-8").read()

    # ------------------------------------------------------------------
    # THE DEFECT ITSELF. A read of a key no producer writes.
    # ------------------------------------------------------------------
    live = [line for line in server.splitlines()
            if 'reply.get("answer")' in line and not line.lstrip().startswith("#")]
    check(not live,
          "revit_change does NOT read reply.get('answer') - a key neither side "
          "emits, so the verdict was always None and every write reported only "
          "that it ran (FRAGMENT-ISSUES row 111). A mention in a comment is "
          "fine; a live read is the bug")

    check('reply.get("verdict")' in server,
          "it reads `verdict` instead - the field WithVerdict actually adds, "
          "and the add-in's own words rather than a sentence composed here "
          "from `applied`, which is the request and not the result")

    check('reply.get("provides")' in server,
          "and it prints `provides`, because A REFUSAL LIVES THERE AND NOWHERE "
          "ELSE. A fragment that declined says so in its results, and 'it ran' "
          "is the one sentence that makes a refusal look like a success")

    check('reply.get("bound")' in server,
          "and the binding note, which is the line a proof is judged on "
          "(fragment-proving rule 5) - the difference between a fragment that "
          "ran on the selection and one that ran on a stale carried value")

    # ------------------------------------------------------------------
    # THE OTHER SIDE. These are what makes the keys above the right ones,
    # so a rename there fails HERE rather than in front of a modeller.
    # ------------------------------------------------------------------
    check('Json.Str("verdict", verdict)' in fragment,
          "the add-in emits `verdict` - if this line is renamed, the server's "
          "read above is wrong again and this test is where that surfaces")

    check('parts.Add("\\"provides\\":{" + string.Join(",", left) + "}")' in fragment,
          "and emits `provides` as the fragment's own read-back")

    check('"answer"' not in fragment,
          "and never emits `answer` at all - which is what made the server's "
          "read dead rather than merely wrong")

    # ------------------------------------------------------------------
    # The verdict's THREE cases must all survive, because the third is the
    # one that matters and the easiest to lose.
    # ------------------------------------------------------------------
    check("THE ROLLBACK DID NOT REPORT SUCCESS" in fragment,
          "the add-in still distinguishes a rollback that reported success "
          "from one that did not - on 2026-09-09 a rollback did not hold while "
          "the client claimed it had, and a write that may still be standing "
          "is the one thing a reader must not have to guess at")

    if FAILURES:
        print("FAILED")
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1

    print("PASSED - a write now reports what it did, and a refusal is visible "
          "as a refusal.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
