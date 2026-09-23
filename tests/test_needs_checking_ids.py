# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Every reader of NEEDS-CHECKING.md sees a row whose ID has TWO letters.

    python tests/test_needs_checking_ids.py

FRAGMENT-ISSUES row 5b-156: tools/owner-queue.py, tools/check-gaps.py and
tools/balance-of-work.py each matched a row ID of ONE capital letter and digits,
so every row of a two-letter group - AA, AB and on, each needing the owner's PC -
reached none of them, and owner-queue's own count of what it should have matched
used the same pattern and agreed. This runs all three on a SMALL REGISTER BUILT
HERE - never the real one - that holds one- and two-letter rows, open and
struck, and a results table.

WHAT IS PROVED
--------------
  1. owner-queue reports every open row, two-letter ones included, and none of
     the struck ones, without its guard firing - and a RESULT that names its
     row in plain text, the shape of every results table in the register, is
     not a second copy of that row;
  2. check-gaps counts every row, done and left, and files a two-letter row
     under its own group rather than under the first letter's - RA1 is not
     treated like Group R's rows;
  3. balance-of-work counts every row and every done one.
"""

import contextlib
import importlib.util
import io
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
NL = chr(10)
DASH = chr(0x2014)
FAILURES = []


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILURES.append(said)


REGISTER = NL.join([
    "# What still needs checking",
    "",
    "## Group A " + DASH + " one letter",
    "",
    "| # | Check | Result |",
    "|---|---|---|",
    "| **A1** | an open check on the owner's PC | not yet |",
    "| ~~**A2**~~ | ~~a done check~~ | DONE |",
    "",
    "## Group AA " + DASH + " two letters",
    "",
    "| # | Check | Result |",
    "|---|---|---|",
    "| **AA1** | Deploy both proofs. Start Revit. Screenshot. | not yet |",
    "| ~~**AA2**~~ | ~~a done two-letter check~~ | DONE |",
    "",
    "### Group AA was RUN",
    "",
    "| ID | Verdict | What was actually seen |",
    "|---|---|---|",
    "| AA1 | **NOT RUN** | a result names its row in plain text |",
    "| AA2 | **PASS** | and so does this one |",
    "",
    "## Group AB " + DASH + " more",
    "",
    "| # | Check | Result |",
    "|---|---|---|",
    "| **AB3** | the installer window, on the PC | not yet |",
    "",
    "## Group R " + DASH + " a conversation",
    "",
    "| # | Check | Result |",
    "|---|---|---|",
    "| **R1** | read the decisions back | not yet |",
    "",
    "## Group RA " + DASH + " two letters that start with R",
    "",
    "| # | Check | Result |",
    "|---|---|---|",
    "| **RA1** | a check in Group RA, not Group R | not yet |",
    "",
])


def reader(filename):
    """The tool, loaded from tools/, reading REGISTER instead of the real file."""
    spec = importlib.util.spec_from_file_location(filename.replace("-", "_")[:-3],
                                                  os.path.join(ROOT, "tools", filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    original = module.read
    module.read = lambda path, _o=original: (REGISTER if path.replace(os.sep, "/").endswith("docs/NEEDS-CHECKING.md")
                                             else _o(path))
    return module


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")

    print("1. owner-queue")
    reported = [r[0] for r in reader("owner-queue.py").needs_checking()]
    check(sorted(reported) == ["A1", "AA1", "AB3", "R1", "RA1"],
          "reports the five open rows, two-letter ones included (%s)" % ", ".join(reported))
    check("!!" not in reported, "and its guard agrees it saw every one")
    check(reported.count("AA1") == 1,
          "a result naming AA1 in plain text is not a second AA1")

    print("")
    print("2. check-gaps")
    gaps = reader("check-gaps.py")
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        gaps.check_register()
    check("7 row(s), 2 done, 5 left" in out.getvalue(), "counts 7 rows, 2 done, 5 left")
    filed = dict((what.split(" ")[0], needs) for what, needs in gaps.WAITING)
    check("AA1" in filed and "AB3" in filed, "and files both AA and AB rows as waiting")
    check(filed.get("R1") == "the owner" and filed.get("RA1") != "the owner",
          "a row's group is ALL its letters: R1 is Group R, RA1 is not (%s / %s)"
          % (filed.get("R1"), filed.get("RA1")))

    print("")
    print("3. balance-of-work")
    check(tuple(reader("balance-of-work.py").register_rows()) == (7, 2),
          "counts 7 rows and 2 done")

    print("")
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        return 1
    print("PASSED")
    return 0


if __name__ == "__main__":
    sys.exit(main())
