#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A status cell kept verbatim, and the placeholder that was kept with it.

    python tests/test_decision_summary.py

`tools/generate-decision-summary.py` rebuilds the Status summary table in
`docs/DECISIONS.md` so that it cannot fall behind the decisions it indexes.
It was written because the table had stopped at D-50 while the file reached
D-70 - twenty decisions missing from the index of decisions, with nothing
able to notice. `check-docs.py` section 8 runs its `--check`, so CI does
fail when the table is stale.

IT KEEPS AN EXISTING STATUS CELL VERBATIM, AND THAT IS RIGHT. "read back
2026-09-06" records a conversation, not a fact on disk, and a generator that
discarded those would lose the record of every read-back the owner has done.

BUT ITS OWN PLACEHOLDER IS NOT CURATION. A decision with no `**Status:**`
line gets `• status not stated` derived for it, and from then on that cell
is preserved like any other. Measured 2026-09-22: give D-72 a `**Status:**`
line and `--check` still answers "Status summary is current: 97
decision(s), none missing" and exits 0, while the table reads `• status not
stated`. Seven of the 97 decisions carry no `**Status:**` line, and D-72's
cell is that placeholder, frozen.

EVERY CASE BUILDS ITS OWN DECISIONS FILE, in a temp folder, with `tool.PATH`
pointed at it. A suite that ran against docs/DECISIONS.md would be rewriting
the top of the truth hierarchy to test the writer.

WHAT IT CANNOT DO: it does not judge whether a status is the RIGHT status -
that is the owner's, always - and it does not check the table's anchors
against GitHub's rule, because check-docs already resolves every link in the
real file, which is the stronger check.

    python tests/test_decision_summary.py
"""

import contextlib
import importlib.util
import io
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "generate-decision-summary.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has hyphens in it."""
    try:
        spec = importlib.util.spec_from_file_location("generate_decision_summary",
                                                      TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


DASH = "—"
DOT = "·"

# Four decisions, one of each shape that matters: one stating its status,
# one whose cell is curated and must survive, one stating nothing at all,
# and one that has since been given a status the table has not caught up
# with. The table below them is deliberately correct for the first three.
HEAD = """# Decisions

## Status summary

| # | Decision | Status |
|---|---|---|
| [D-01](#d-01{dash}the-first-one) | The first one | OK Accepted {dot} 2026-01-01 |
| [D-02](#d-02{dash}the-curated-one) | The curated one | OK Accepted {dot} read back 2026-09-06 |
| [D-03](#d-03{dash}the-silent-one) | The silent one | BULLET status not stated |

## The decisions

## D-01 {dash} The first one

**Status:** Accepted {dot} **Date:** 2026-01-01

Something.

## D-02 {dash} The curated one

**Status:** Accepted {dot} **Date:** 2026-02-02

Something else.

## D-03 {dash} The silent one

**2026-03-03.** A decision that states no status of its own.
"""


def decisions(bullet="•", tick="✅"):
    return (HEAD.replace("{dash}", DASH).replace("{dot}", DOT)
                .replace("BULLET", bullet).replace("OK", tick))


def cell(text, ident):
    """The status cell for one decision, or None."""
    m = re.search(r"^\| \[%s\]\([^)]*\) \|[^|]*\|([^|]*)\|" % ident, text, re.M)
    return m.group(1).strip() if m else None


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    # ASK BEFORE CALLING - heron-ship section 2a.
    tool = load()
    check(tool is not None, "tools/generate-decision-summary.py loads")
    runner = getattr(tool, "main", None) if tool else None
    check(callable(runner), "and it has a main() to call")
    check(getattr(tool, "PATH", None) is not None,
          "and names the file it writes, so a test can point it elsewhere")
    if not callable(runner) or getattr(tool, "PATH", None) is None:
        print()
        print("FAILED")
        for line in FAILURES:
            print("  - %s" % line)
        return 1

    home = tempfile.mkdtemp(prefix="heron-decisions-test-")
    was_path, was_argv = tool.PATH, sys.argv
    try:
        def run(text, argv=()):
            """(exit code, what it printed, the file afterwards)."""
            path = os.path.join(home, "DECISIONS.md")
            io.open(path, "w", encoding="utf-8", newline="").write(text)
            tool.PATH = path
            sys.argv = ["generate-decision-summary.py"] + list(argv)
            said = io.StringIO()
            try:
                with contextlib.redirect_stdout(said):
                    code = runner()
            except BaseException as raised:         # noqa: BLE001
                code = "raised %s" % type(raised).__name__
            return (code, said.getvalue(),
                    io.open(path, encoding="utf-8").read())

        print("1. A table that already matches is left exactly alone")
        # THE BASELINE IS THE TOOL'S OWN OUTPUT, not a table typed here. The
        # block it writes carries a "Generated - do not edit" note, so a
        # hand-typed table is stale by definition and every case below it
        # would then be green for the wrong reason.
        _code, _spoke, start = run(decisions())
        check("Generated" in start, "the baseline is the tool's own output")
        code, spoke, after = run(start, ["--check"])
        check(code == 0, "--check exits 0 on a current table, and it exits %r"
                         % code)
        check(after == start, "and writes nothing")
        code, spoke, after = run(start)
        check(after == start,
              "and a rewrite of a current table is byte-identical")

        print()
        print("2. A curated status cell survives a rewrite, verbatim")
        # This is the whole reason the tool preserves cells: "read back
        # 2026-09-06" records a conversation, not a fact on disk.
        code, spoke, after = run(start)
        check("read back 2026-09-06" in (cell(after, "D-02") or ""),
              "D-02 still reads %r" % cell(after, "D-02"))

        print()
        print("3. A NEW decision is added with the status it states")
        grown = start.rstrip("\n") + (
            "\n\n## D-04 %s The new one\n\n**Status:** Proposed %s "
            "**Date:** 2026-04-04\n\nSomething new.\n" % (DASH, DOT))
        code, spoke, after = run(grown, ["--check"])
        check(code == 1, "--check calls it STALE, and it exits %r" % code)
        check("D-04" in spoke, "and names the decision that is missing")
        code, spoke, after = run(grown)
        got = cell(after, "D-04") or ""
        check("Proposed" in got and "2026-04-04" in got,
              "after a rewrite D-04 reads %r" % got)

        print()
        print("4. ITS OWN PLACEHOLDER IS NOT CURATION")
        # D-03 states no status, so the tool derived `status not stated` for
        # it. Give D-03 a status and the table must catch up - a cell the
        # generator wrote itself is not a record of anything a person said.
        spoke_up = start.replace(
            "## D-03 %s The silent one\n\n**2026-03-03.**" % DASH,
            "## D-03 %s The silent one\n\n**Status:** Accepted %s "
            "**Date:** 2026-03-03\n\n**2026-03-03.**" % (DASH, DOT), 1)
        # A replace that matched nothing would make every check below it
        # green against an unchanged file.
        check(spoke_up != start, "the fixture actually gained a status line")
        # The baseline is CURRENT before this line - section 1 proved it -
        # so a STALE answer here is about the status line and nothing else.
        code, spoke, after = run(spoke_up, ["--check"])
        check(code == 1,
              "--check calls the table STALE once the decision states a "
              "status, and it exits %r" % code)
        code, spoke, after = run(spoke_up)
        got = cell(after, "D-03") or ""
        check("Accepted" in got and "2026-03-03" in got,
              "and a rewrite catches the cell up - it reads %r" % got)
        check("status not stated" not in got,
              "so the placeholder is gone rather than frozen")

        print()
        print("5. A decision that still states nothing is still said so")
        code, spoke, after = run(start)
        check("status not stated" in (cell(after, "D-03") or ""),
              "D-03 reads %r while it states nothing, which is honest"
              % cell(after, "D-03"))

        print()
        print("6. It stops rather than guessing")
        code, spoke, after = run("# Decisions\n\nNothing here.\n")
        check(code == 1 and "No decisions found" in spoke,
              "a file with no decisions exits 1 and says so")
        headless = decisions().replace("## Status summary", "## Something else")
        code, spoke, after = run(headless)
        check(code == 1 and after == headless,
              "and a file with no Status summary heading is left untouched")
    finally:
        tool.PATH, sys.argv = was_path, was_argv
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a person's cell is kept and the generator's own")
    print("placeholder is not mistaken for one.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
