#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   13
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A module a suite tests by path is a module a suite tests.

    python tests/test_check_gaps.py

`tools/check-gaps.py` names every brain module no suite loads as UNFINISHED,
and its exit code follows that list. It used to count an `import` only. Then
`tests/test_install_lines.py` arrived, loading `brain/heron_install.py`
through importlib from a path it builds with `os.path.join` - on purpose, so
that a missing module is one FAIL line rather than a traceback - and
check-gaps reported `brain/heron_install.py` as imported by no suite, its one
UNFINISHED item on a plain container, while a suite tested it (row 5b-1067).

This asks `suite_loads`, the one reading, both ways: an import and a load by
path count; a comment, a quoted name with no loader, and a longer module name
do not. Then the real tree: `test_install_lines.py` loads `heron_install`.

WHAT IT CANNOT SAY: whether the suite that loads a module tests it well.
check-gaps counts that a suite exists, and so does this.
"""

import importlib.util
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-gaps.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load_tool():
    spec = importlib.util.spec_from_file_location("check_gaps_under_test", TOOL)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def import_only(text, module):
    """The reading check-gaps had before row 5b-1067 - so the checks below
    still RUN, and fail, on a tool that has no suite_loads."""
    return re.search(r"^\s*(?:import|from)\s+%s\b" % re.escape(module),
                     text, re.M) is not None


LOADER = "    spec = importlib.util.spec_from_file_location(name, path)\n"


def main():
    print("check-gaps - which suites load which brain module")
    print("=" * 62)
    tool = load_tool()
    loads = getattr(tool, "suite_loads", None)
    check(loads is not None, "check-gaps has one reading of 'a suite loads this module'")
    if loads is None:
        loads = import_only

    print()
    print("1. What counts")
    check(loads("import heron_example as EX\n", "heron_example"),
          "an import counts")
    check(loads("    from heron_example import thing\n", "heron_example"),
          "and an indented from-import")
    check(loads('HELPER = os.path.join(BRAIN, "heron_example.py")\n' + LOADER,
                "heron_example"),
          "and a load by path: the file's name in os.path.join, in a suite "
          "that calls spec_from_file_location")

    print()
    print("2. What does not")
    check(not loads("# heron_example.py is read elsewhere\n" + LOADER, "heron_example"),
          "a comment naming the file, beside a loader, is a mention")
    check(not loads('HELPER = os.path.join(BRAIN, "heron_example.py")\n', "heron_example"),
          "a path to the file with no loader in the suite loads nothing")
    check(not loads("import heron_example_two\n", "heron_example"),
          "an import of a longer name is not this module")
    check(not loads('X = os.path.join(BRAIN, "heron_example_two.py")\n' + LOADER,
                    "heron_example"),
          "nor a path to a longer name")

    print()
    print("3. The real tree")
    path = os.path.join(ROOT, "tests", "test_install_lines.py")
    if os.path.exists(path):
        text = io.open(path, encoding="utf-8").read()
        check(loads(text, "heron_install"),
              "tests/test_install_lines.py loads brain/heron_install.py - "
              "the module check-gaps said no suite imports")
    else:
        check(False, "tests/test_install_lines.py is there to read")

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - an import or a load by path counts, and a mention does not.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
