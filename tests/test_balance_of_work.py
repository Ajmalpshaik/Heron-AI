#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The page that refuses a plausible zero, printing one of its own.

    python tests/test_balance_of_work.py

`tools/balance-of-work.py` is one of only TWO tools in `tools/` that no
suite names at all, measured 2026-09-22 against every file in `tests/`. It
is the page a person opens to decide what to do next, and its whole premise
is a single rule, stated in its own helper:

    A figure a tool did not give back is not a zero.

THAT RULE HAS ALREADY FAILED TWICE IN THIS FILE, both times on the day the
count it governed reached zero, and both times the fix is recorded in a
comment beside the row: row 4, where an empty list of unsigned agent proofs
was falsy and printed "not derived" on a board where every draft had in fact
been signed; and row 9, where a clean signature board printed "not derived"
because the heading it greps for is only printed when something IS stale.

So every case here asks the same question in a different place: WHEN THE
ANSWER IS ZERO, DOES THE PAGE SAY ZERO?

WHAT IT DOES NOT DO: it never asserts a figure about this repository. Those
move whenever anybody commits, and a suite that pinned them would be a third
place the numbers are typed - which is the failure this whole tool exists to
refuse. Every case builds its own tree in a temp folder with `tool.ROOT`
pointed at it, so the subprocesses `collect()` launches find no tools there
and their rows read "not derived" on purpose.
"""

import io
import importlib.util
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "balance-of-work.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has a hyphen in it."""
    try:
        spec = importlib.util.spec_from_file_location("balance_of_work", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


def write(home, rel, text):
    path = os.path.join(home, *rel.split("/"))
    folder = os.path.dirname(path)
    if folder and not os.path.isdir(folder):
        os.makedirs(folder)
    io.open(path, "w", encoding="utf-8", newline="").write(text)
    return path


def library(home, statuses):
    """A fragment library on disk, one folder per fragment."""
    for i, status in enumerate(statuses):
        write(home, "brain/fragments/frag-%d/fragment.yaml" % i,
              "id: frag-%d\nheron-status: %s\n" % (i, status))


def board(tool, home, **over):
    """collect() against the tree in `home`, with fields overridden.

    The dictionary is the tool's OWN, built by its own collect() from the
    files written above, so the chain from disk to printed row is never
    short-circuited by a hand-typed tally.
    """
    was = tool.ROOT
    try:
        tool.ROOT = home
        d = tool.collect()
    finally:
        tool.ROOT = was
    d.update(over)
    return d


def count(text, number):
    """The COUNT CELL of row `number`, and nothing else on the line.

    Matching anywhere in the row is not good enough and this suite learned it
    the hard way: every row prints `<count> of <total>`, so a check that only
    asks whether "not derived" appears somewhere on the line is satisfied by
    the TOTAL half while the count half is wrong. Breaking the tool to see
    this suite fail is what found that - the break that made an unread library
    report zero left every check green.
    """
    for line in text.splitlines():
        if line.startswith("| %s |" % number):
            cells = line.split("|")
            return cells[3].strip() if len(cells) > 3 else ""
    return ""


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    check(tool is not None, "tools/balance-of-work.py loads")
    if tool is None:
        print()
        print("FAILED - it did not import")
        return 1

    for name in ("collect", "render", "to_console", "statuses", "n",
                 "register_rows", "proposals_open", "live_work_notes", "grab"):
        check(callable(getattr(tool, name, None)), "and it has %s()" % name)
    check(getattr(tool, "ROOT", None) is not None,
          "and names the tree it reads, so a test can point it elsewhere")
    if getattr(tool, "ROOT", None) is None or not callable(getattr(tool, "collect", None)):
        print()
        print("FAILED - nothing to point at a fixture")
        for line in FAILURES:
            print("  - %s" % line)
        return 1

    home = tempfile.mkdtemp(prefix="heron-balance-")
    try:
        print()
        print("1. The rule the page states about its own blanks")
        check(tool.n(None) == "**not derived**", "n(None) is 'not derived'")
        check(tool.n("") == "**not derived**", "and so is an empty answer")
        check(tool.n("0") == "**0**", "but a derived zero prints as 0")
        check(tool.n(0) == "**0**", "including the integer zero, which is falsy")

        print()
        print("2. A LIBRARY WITH NOTHING LEFT TO PROVE SAYS ZERO, NOT 'NOT DERIVED'")
        print("   The day the largest body of work in the project is finished is")
        print("   the day this row matters most.")
        library(home, ["PROVEN", "PROVEN", "PROVEN"])
        write(home, "brain/skills/one.yaml", "heron-status: PROVEN\n")
        finished = board(tool, home)
        check(finished["frag"] == {"PROVEN": 3},
              "the tally reads what is on disk, and it is %r" % (finished["frag"],))
        page = tool.render(finished)
        spoke = tool.to_console(finished)
        one = count(page, 1)
        check(one == "**0** of **3**",
              "row 1 counts 0 of 3, and its count cell reads %r" % one)
        two = count(page, 2)
        check(two == "**0** of **1**",
              "row 2 counts 0 of 1, and its count cell reads %r" % two)
        check(re.search(r"fragments never in front of a model\s+0\s", spoke) is not None,
              "and the console prints 0 for the same row")
        check(re.search(r"skills never proved\s+0\s", spoke) is not None,
              "and 0 for skills")

        print()
        print("3. A library NOBODY READ is still 'not derived'")
        print("   The two answers must not collapse in either direction.")
        empty = tempfile.mkdtemp(prefix="heron-balance-none-")
        try:
            nothing = board(tool, empty)
            check(nothing["frag"] == {}, "no brain/fragments/ means no tally")
            blank = tool.render(nothing)
            one = count(blank, 1)
            check(one == "**not derived** of **not derived**",
                  "row 1's COUNT is not derived, not a zero - it reads %r" % one)
            two = count(blank, 2)
            check(two == "**not derived** of **not derived**",
                  "and so is row 2's - it reads %r" % two)
            hush = tool.to_console(nothing)
            check(re.search(r"fragments never in front of a model\s+not derived", hush)
                  is not None, "and the console says not derived rather than 0")
        finally:
            shutil.rmtree(empty, ignore_errors=True)

        print()
        print("4. A library with work left still counts it")
        work = tempfile.mkdtemp(prefix="heron-balance-work-")
        try:
            library(work, ["DRAFT", "PROVEN", "DRAFT"])
            left = board(tool, work)
            one = count(tool.render(left), 1)
            check(one == "**2** of **3**",
                  "two DRAFT of three reads as 2 of 3, and it reads %r" % one)
        finally:
            shutil.rmtree(work, ignore_errors=True)

        print()
        print("5. The register: a struck ID is done, a struck COMMENT is not")
        write(home, "docs/NEEDS-CHECKING.md",
              "| ~~**A1**~~ | done |\n"
              "| **A2** | open |\n"
              "| **A3** | ~~a struck note, which is not a struck id~~ |\n")
        rows, done = tool.register_rows()
        was = tool.ROOT
        try:
            tool.ROOT = home
            rows, done = tool.register_rows()
        finally:
            tool.ROOT = was
        check(rows == 3, "three rows are seen, and it saw %r" % rows)
        check(done == 1, "one is struck through, and it counted %r" % done)

        print()
        print("6. A work-notes index that could not be read is NOT 'no live note'")
        print("   Its two siblings in this file already refuse that collapse.")
        was = tool.ROOT
        try:
            tool.ROOT = home
            missing = tool.live_work_notes()
            check(missing is None,
                  "a missing index answers None, and it answered %r" % (missing,))
            write(home, "docs/work-notes/README.md",
                  "| [`LIVE.md`](LIVE.md) | **waiting on a Revit** | x |\n"
                  "| [`BALANCE-OF-WORK.md`](BALANCE-OF-WORK.md) | **generated** | x |\n"
                  "| ~~`GONE.md`~~ | retired | x |\n")
            live = tool.live_work_notes()
        finally:
            tool.ROOT = was
        names = [n for n, _s in (live or [])]
        check(names == ["LIVE.md"],
              "a linked row is live, a struck one is not, and this page is not "
              "its own outstanding work - it read %r" % (names,))

        print()
        print("7. grab() answers None rather than substituting a figure")
        check(tool.grab("nothing of the kind", r"(\d+) open") is None,
              "a pattern that does not match answers None")
        check(tool.grab("7 open", r"(\d+) open") == "7", "and a match answers it")
        print("   AND IT MUST NOT WALK ONTO THE NEXT LINE. When every question is")
        print("   answered check-docs prints 'still open:' with nothing after it.")
        clean = "still open:\nProgress: agrees with the stated line\n"
        check(tool.grab(clean, r"still open:[ \t]*(\S.*)") is None,
              "an empty tail does not capture the sentence below it")

        print()
        print("8. The defect rows read the AGGREGATE, not the first section")
        print("   open-defects.py grew a second section, and re.search takes the")
        print("   first match - so section 5's figures once stood for the whole.")
        spoke = ("  Section 5 rows      : 163\n"
                 "  Section 5 open      : 33\n"
                 "  TOTAL rows, all sections  : 306\n"
                 "  TOTAL ids, all sections   : 41, 5b-83, 5b-95\n"
                 "  OPEN across both sections : 44\n")
        check(tool.grab(spoke, r"OPEN across both sections\s*:\s*(\d+)") == "44",
              "the open count is the both-sections one")
        check(tool.grab(spoke, r"TOTAL rows, all sections\s*:\s*(\d+)") == "306",
              "and so is the row total")
        ids = tool.grab(spoke, r"TOTAL ids, all sections\s*:\s*([0-9a-zA-Z, \-]+)")
        check(ids is not None and "5b-83" in ids,
              "and the ids carry the second section's, which it read as %r" % ids)

        print()
        print("9. It reports and never gates")
        check("return 0" in io.open(TOOL, encoding="utf-8").read().split("def main")[-1],
              "main() returns 0 whatever it found")
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a zero it derived reads as zero, and a blank it could not")
    print("derive still reads as a blank.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
