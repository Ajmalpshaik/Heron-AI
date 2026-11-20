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

4. ITS OWN OUTPUT, AND ITS SUITES', SURVIVE A CODE PAGE WITH NO ARROW (row
   5b-1642). Python is made to print in cp1252, as Windows does to a redirected
   stdout: the tool loaded, an arrow printed after it does not crash; and a
   suite it runs, in a scratch tests folder, prints an arrow and passes.

5. THE MCP SERVING SUITE HAS A CEILING OF ITS OWN (row 5b-402): the sweep
   gives tests/test_mcp_serves.py more than the 300 s every other suite gets,
   and every other suite still 300 s.
"""

import shutil
import subprocess
import tempfile

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


def suites(tool):
    """Check 4's second half, and check 5: check_tests() run over a scratch
    tests folder of two suites, in a process whose own output is cp1252, with
    each run's ceiling written down as it is asked."""
    top = tempfile.mkdtemp(prefix="check-gaps-suites-")
    real_root, real_run = tool.ROOT, subprocess.run
    asked = {}
    was = os.environ.get("PYTHONIOENCODING")
    try:
        os.makedirs(os.path.join(top, "tests"))
        arrow = "import sys\nprint('a row with ' + chr(0x2192))\nsys.exit(0)\n"
        for name in ("test_mcp_serves.py", "test_other.py"):
            with io.open(os.path.join(top, "tests", name), "w", encoding="utf-8") as handle:
                handle.write(arrow)

        def watched(cmd, *args, **kwargs):
            asked[os.path.basename(cmd[-1])] = kwargs.get("timeout")
            return real_run(cmd, *args, **kwargs)

        tool.ROOT = top
        tool.subprocess.run = watched
        os.environ["PYTHONIOENCODING"] = "cp1252"
        before = list(tool.UNFINISHED)
        said = io.StringIO()
        real_out, sys.stdout = sys.stdout, said
        try:
            tool.check_tests()
        finally:
            sys.stdout = real_out
        new = [line for line in tool.UNFINISHED if line not in before]
        check(not new, "a suite printing an arrow passes under it, not FAILS (%s)"
              % ("; ".join(new) or "nothing unfinished"))

        print()
        print("5. The MCP serving suite has a ceiling of its own (5b-402)")
        mine, other = asked.get("test_mcp_serves.py"), asked.get("test_other.py")
        check(other == tool.SUITE_TIMEOUT,
              "an ordinary suite still gets %ss (%s)" % (tool.SUITE_TIMEOUT, other))
        check(isinstance(mine, (int, float)) and mine > tool.SUITE_TIMEOUT and mine >= 562,
              "test_mcp_serves.py gets more than that, and more than the 562s measured "
              "for it under load (%s)" % mine)
    finally:
        tool.ROOT = real_root
        tool.subprocess.run = real_run
        if was is None:
            os.environ.pop("PYTHONIOENCODING", None)
        else:
            os.environ["PYTHONIOENCODING"] = was
        shutil.rmtree(top, ignore_errors=True)


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
    print("4. A code page with no arrow does not stop it (5b-1642)")
    printed = subprocess.run(
        [sys.executable, "-c",
         "import importlib.util,sys;"
         "spec=importlib.util.spec_from_file_location('g', sys.argv[1]);"
         "m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);"
         "print('WAITING - a row with ' + chr(0x2192) + ' in it')", TOOL],
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, cwd=ROOT,
        env=dict(os.environ, PYTHONIOENCODING="cp1252"))
    check(printed.returncode == 0 and "\u2192".encode("utf-8") in printed.stdout,
          "printed through cp1252, as a redirected stdout is on Windows, its arrow "
          "comes out as UTF-8 (exit %d)" % printed.returncode)
    suites(tool)

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
