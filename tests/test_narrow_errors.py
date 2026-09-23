#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The word `raise` in a comment, standing in for the statement.

    python tests/test_narrow_errors.py

`tools/check-narrow-errors.py` is one of the gates a pull request has to
pass, and it looks for D-52's commonest shape: an
`except sqlite3.OperationalError` that swallows a locked, malformed or
out-of-date store and reports it as an empty one. Reviews found that same
line in four files on four consecutive rounds, which is why it is a command
rather than a habit.

IT READS TEXT RATHER THAN AN AST, ON PURPOSE - its docstring says so, and
the reason is good: the rule is about what a person maintaining the file
sees beside the handler. **But the narrowing test searched the handler's raw
text for the word `raise`**, so measured 2026-09-22:

    # deliberately do not raise here    satisfied the gate
    log("nothing to raise")             satisfied the gate

A gate a comment can satisfy is not a gate. A handler saying in words that
it will not re-raise, and then not re-raising, was the one shape it could
not see.

EVERY CASE BUILDS ITS OWN TREE, in a temp folder, with `tool.ROOT` and
`tool.LOOKED_AT` pointed at it. A suite that scanned `brain/` would be
asserting today's handlers, which is what CI already does and a different
question from whether the gate works.

WHAT IT CANNOT DO: it does not say whether a handler SHOULD narrow. That is
D-52's question and the gate's docstring answers it; this only asks whether
the gate sees what it claims to see.

    python tests/test_narrow_errors.py
"""

import importlib.util
import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-narrow-errors.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has hyphens in it."""
    try:
        spec = importlib.util.spec_from_file_location("check_narrow_errors",
                                                      TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


HEAD = """import sqlite3


def go(db):
    try:
        return db.execute("select 1").fetchall()
"""


def handler(body):
    """One module holding one `except`, with `body` inside it."""
    return HEAD + "    except sqlite3.OperationalError as exc:\n" + body


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    check(tool is not None, "tools/check-narrow-errors.py loads")
    finder = getattr(tool, "findings", None) if tool else None
    check(callable(finder), "and it still has findings()")
    check(getattr(tool, "LOOKED_AT", None) is not None
          and getattr(tool, "ROOT", None) is not None,
          "and names the tree it walks, so a test can point it elsewhere")
    if not callable(finder) or getattr(tool, "LOOKED_AT", None) is None:
        print()
        print("FAILED")
        for line in FAILURES:
            print("  - %s" % line)
        return 1

    home = tempfile.mkdtemp(prefix="heron-narrow-test-")
    was = (tool.ROOT, tool.LOOKED_AT)
    try:
        def flagged(body, name="x.py"):
            """True when the gate reports the module this writes."""
            place = tempfile.mkdtemp(dir=home)
            os.makedirs(os.path.join(place, "brain"))
            io.open(os.path.join(place, "brain", name), "w",
                    encoding="utf-8", newline="").write(body)
            tool.ROOT, tool.LOOKED_AT = place, ("brain",)
            try:
                return bool(finder())
            finally:
                tool.ROOT, tool.LOOKED_AT = was

        print("1. The shape it was written for")
        check(flagged(handler("        return []\n")),
              "a bare swallow is reported")

        print()
        print("2. The shapes that are already narrow")
        check(not flagged(handler('        if "no such table" not in str(exc):\n'
                                  "            raise\n"
                                  "        return []\n")),
              "the narrowing its own message recommends is accepted")
        check(not flagged(handler('        if "no such table" not in exc.args[0]:\n'
                                  "            raise\n"
                                  "        return []\n")),
              "and so is the args[0] form")
        check(not flagged(handler("        raise\n")),
              "and a handler that simply re-raises")

        print()
        print("3. A COMMENT IS NOT A NARROWING")
        # The word `raise` anywhere in the handler's text satisfied this,
        # and a handler that says in words it will not re-raise is the
        # clearest possible case of one that does not.
        check(flagged(handler("        # deliberately do not raise here\n"
                              "        return []\n")),
              "a swallow whose comment contains the word raise is still "
              "reported")
        check(flagged(handler('        log("nothing to raise")\n'
                              "        return []\n")),
              "and so is one whose message contains it")
        check(flagged(handler('        note = "we could raise"\n'
                              "        return []\n")),
              "and one that only assigns it to a name")

        print()
        print("4. The raise has to be THIS handler's")
        # The window stops at the end of the handler on purpose: a `raise`
        # belonging to the next block is not this one narrowing itself.
        check(flagged(HEAD
                      + "    except sqlite3.OperationalError as exc:\n"
                      + "        return []\n"
                      + "    except ValueError:\n"
                      + "        raise\n"),
              "a raise in the NEXT handler does not rescue this one")

        print()
        print("5. A handler with nothing to do with sqlite is not its business")
        check(not flagged(HEAD + "    except ValueError:\n"
                          + "        return []\n"),
              "a ValueError handler is left alone - D-52's shape here is the "
              "database one, and the gate says so")
    finally:
        tool.ROOT, tool.LOOKED_AT = was
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the gate reads code for its narrowing, and a comment")
    print("saying the word no longer stands in for the statement.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
