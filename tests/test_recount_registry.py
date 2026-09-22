#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The tool that owns the registry's counts, and the counts it leaves wrong.

    python tests/test_recount_registry.py

`tools/recount-agent-registry.py` says it recomputes EVERY stated count in
`docs/28-agent-registry.md` from its own rows. AGENTS.md's second Never is
the reason it exists: "never type a number a command can derive". This is
that command, and it is one of the SIX tools in `tools/` that no suite names.

TWO THINGS WERE MEASURED ON 2026-09-22, both by running it:

  * it left `across 249 agents` in a registry it had just counted to 250,
    because that one substitution of the seven is a LITERAL - `.replace(
    'across 244 agents', ...)` - and 244 stopped existing long ago;

  * given a registry whose table has gained one column, it wrote
    `Totals: 0 agents, 0 T1, 0 T2, 0 T3`, a summary table of zeros, said
    `verify: all departments reconcile`, and exited 0. The tier is read by
    `l.split('|')[4]`, and the rows were all still there. D-52, the
    plausible zero.

EVERY CASE BUILDS ITS OWN REGISTRY, in a temp folder, with `tool.P` pointed
at it. A suite that ran against docs/28-agent-registry.md would be rewriting
the document to test the writer.

WHAT IT CANNOT DO: it does not judge whether an agent BELONGS in the
department its row sits under, or whether a tier is the right tier. Those
are questions for a person, and this only asks whether the arithmetic the
tool publishes matches the rows underneath it.

    python tests/test_recount_registry.py
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
TOOL = os.path.join(ROOT, "tools", "recount-agent-registry.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load(home):
    """The tool as a module, or None.

    LOADED FROM INSIDE A TEMP FOLDER ON PURPOSE. Until 2026-09-22 this tool
    had no `main()` and did its whole job at import time, against a path
    relative to the working directory - so importing it from the repository
    rewrote the real registry. Loading from an empty folder means the worst
    an old copy can do is fail to find a file that is not there.
    """
    was = os.getcwd()
    os.chdir(home)
    try:
        spec = importlib.util.spec_from_file_location("recount_agent_registry",
                                                      TOOL)
        module = importlib.util.module_from_spec(spec)
        with contextlib.redirect_stdout(io.StringIO()):
            spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None
    finally:
        os.chdir(was)


# A registry small enough to check by eye and shaped exactly like the real
# one: a totals line, department headings carrying their own counts, a
# summary table, and the prose figure that went stale. EVERY NUMBER IN IT IS
# WRONG, so a tool that corrects them has to be seen doing it.
REGISTRY = """# 28. Agent registry

**Totals: 999 agents · 999 T1 · 999 T2 · 999 T3.**

Of the rest, 999 make one scoped call and 999 run a real agentic loop.

Detects breaking contract changes across 999 agents.

| Department | Agents | T1 | T2 | T3 |
|---|---|---|---|---|
| Orchestration | 9 | 9 | 9 | 9 |
| **Total** | **9** | **9** | **9** | **9** |

## 1. Orchestration - 99

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-ORC-MAIN-001` | Orchestrator | picks | T2 | - | 3 |
| `HERON-ORC-INT-002` | Intent | sorts | T1 | READ | 4 |

## 2. Revit Engineering - 99

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-RVT-SEL-003` | Selection | selects | T1 | READ | 5 |
| `HERON-RVT-PRM-004` | Parameters | reads | T3 | WRITE | 6 |
| `HERON-RVT-VIW-005` | Views | opens | T1 | READ | 7 |
"""
# The registry writes its department counts after an em dash and its
# totals between middle dots. Both are what the tool matches on, so the
# fixture has to carry the real characters rather than a stand-in.
DASH = "—"
DOT = "·"
REGISTRY = REGISTRY.replace(" - 99\n", " %s 99\n" % DASH)


def figures(text):
    """Every number this tool claims to own, read back out of the file."""
    headings = dict((m.group(1).strip(), int(m.group(2)))
                    for m in re.finditer(r"^## [0-9a-z]+\. (.+?) %s (\d+)"
                                         % DASH, text, re.M))
    totals = re.search(r"\*\*Totals: (\d+) agents %s (\d+) T1 %s (\d+) T2 "
                       r"%s (\d+) T3\.\*\*" % (DOT, DOT, DOT), text)
    summary = re.search(r"^\| \*\*Total\*\* \| \*\*(\d+)\*\* \| \*\*(\d+)\*\* "
                        r"\| \*\*(\d+)\*\* \| \*\*(\d+)\*\* \|", text, re.M)
    return {
        "headings": headings,
        "totals": tuple(int(g) for g in totals.groups()) if totals else None,
        "summary": tuple(int(g) for g in summary.groups()) if summary else None,
        "across": [int(n) for n in re.findall(r"across (\d+) agents", text)],
        "scoped": re.findall(r"Of the rest, (\d+) make one scoped call and "
                             r"(\d+) run a real agentic loop\.", text),
    }


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    home = tempfile.mkdtemp(prefix="heron-recount-test-")
    try:
        # ASK BEFORE CALLING - heron-ship section 2a. A tool that will not
        # load, or that has no main() to call, is ONE clean failure.
        tool = load(home)
        check(tool is not None, "tools/recount-agent-registry.py loads")
        runner = getattr(tool, "main", None) if tool else None
        check(callable(runner),
              "and it has a main() to call, so importing it does not rewrite "
              "the registry by itself")
        check(getattr(tool, "P", None) is not None
              and os.path.isabs(str(getattr(tool, "P", ""))),
              "and the file it writes is an absolute path, not one relative "
              "to whatever directory it was run from")
        if not callable(runner):
            print()
            print("FAILED")
            for line in FAILURES:
                print("  - %s" % line)
            return 1

        was = tool.P

        def run(text):
            """(exit code, what it printed, the registry afterwards)."""
            path = os.path.join(home, "registry.md")
            io.open(path, "w", encoding="utf-8", newline="").write(text)
            tool.P = path
            said = io.StringIO()
            try:
                with contextlib.redirect_stdout(said):
                    code = runner()
            except BaseException as raised:         # noqa: BLE001
                code = "raised %s" % type(raised).__name__
            return (code, said.getvalue(),
                    io.open(path, encoding="utf-8").read())

        try:
            print("1. Every stated count is recomputed, not just most of them")
            # Two departments, five rows: T1 three, T2 one, T3 one.
            code, spoke, after = run(REGISTRY)
            seen = figures(after)
            check(code == 0, "it exits 0, and it exits %r" % code)
            check(seen["headings"] == {"Orchestration": 2,
                                       "Revit Engineering": 3},
                  "each department heading is its own row count, and they "
                  "read %r" % (seen["headings"],))
            check(seen["totals"] == (5, 3, 1, 1),
                  "the totals line reads 5 agents, 3 T1, 1 T2, 1 T3, and it "
                  "reads %r" % (seen["totals"],))
            check(seen["summary"] == (5, 3, 1, 1),
                  "the summary table's Total row agrees with it, and it "
                  "reads %r" % (seen["summary"],))
            check(seen["scoped"] == [("1", "1")],
                  "the prose that names T2 and T3 agrees, and it reads %r"
                  % (seen["scoped"],))
            check(seen["across"] == [5],
                  "AND SO DOES `across N agents`, which is the one figure "
                  "this tool left stale - it reads %r" % (seen["across"],))

            print()
            print("2. Run twice, and the second run changes nothing")
            # A figure fixed by a LITERAL is right once and stale forever
            # after. Running the tool on its own output is how that shows.
            code, spoke, twice = run(after)
            check(code == 0 and twice == after,
                  "the registry is byte-identical after a second run")

            print()
            print("3. A tier column that is not a tier is refused, not counted")
            # The tier is read by column number. One column added to the
            # table and every row lands in a bucket that is not T1, T2 or T3
            # - which is a registry of NO agents, written over one that lists
            # five. D-52: a plausible zero is the one nobody questions.
            # THE COLUMN GOES IN AFTER THE ID, so every row still starts
            # `| \`HERON-` and is still counted. Put it in FRONT and the rows
            # stop being rows, which is a different refusal and would have
            # left this case green for the wrong reason.
            def shift(line):
                if not line.startswith("| `HERON-"):
                    return line
                part = line.split("|")
                part.insert(2, " new ")
                return "|".join(part)

            moved = "\n".join(shift(line) for line in REGISTRY.split("\n"))
            code, spoke, after = run(moved)
            check(code != 0,
                  "it exits non-zero, and it exits %r" % code)
            check(after == moved,
                  "and the registry is untouched - a count it could not take "
                  "is never written")
            check("0 agents" not in after,
                  "so nothing in the file says the registry holds no agents")

            print()
            print("4. A registry it cannot read at all is said, not guessed")
            code, spoke, after = run("# 28. Agent registry\n\nNo rows.\n")
            check(code != 0, "a registry with no agent rows exits non-zero, "
                             "and it exits %r" % code)
        finally:
            tool.P = was
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - every figure it publishes is one it counted, and a count")
    print("it could not take is refused rather than written as nought.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
