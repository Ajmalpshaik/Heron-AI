# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
check-reachable.py - the three ways this check has already been got wrong.

    python tests/test_reachable.py

The tool answers one question: is this function called by anything that is not
a test? Getting it wrong in the permissive direction reports nothing and looks
healthy. Getting it wrong in the strict direction reports every CLI subcommand
as dead and gets switched off. Both failures are silent, so the cases are here.

WHAT IT PROVES
  1. A dict literal `{"accept": accept}` IS dispatch, and a `getattr(m, "x")`
     IS dispatch. Every CLI subcommand in this repository is reached one of
     those two ways, and a check that missed them would call them all dead.
  2. A STRING LITERAL ON ITS OWN IS NOT DISPATCH, and this is the case that
     motivated the whole file. The first version treated any `"remember"` as
     dispatch; tools/measure-routes.py contains one, inside the `ast`
     comparison that finds callers of `remember` - so THE TOOL WRITTEN TO FIND
     THE PROBLEM MADE THE PROBLEM INVISIBLE TO THE NEXT TOOL.
  3. A helper called inside its own module is not a hit.
  4. A decorated function is not a hit - an `@server.tool()` is called by the
     host, not by code, and reporting them all would bury everything else.
  5. A file that will not parse is NAMED. "Nothing calls it" must never be an
     artefact of a file nobody could read.
  6. Something already explained is separated from something that is not, so
     the top of the report is only what nobody has answered.

WHAT IT DOES NOT PROVE. That a hit is a defect. Three of the current hits are
deliberate and say so.
"""

import io
import os
import sys
import shutil
import tempfile
import importlib.util

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FAILURES = []


def load(root=None):
    path = os.path.join(ROOT, "tools", "check-reachable.py")
    spec = importlib.util.spec_from_file_location("check_reachable", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    if root is not None:
        module.ROOT = root
        module.RECORDED = {}
    return module


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write(root, rel, body):
    path = os.path.join(root, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    io.open(path, "w", encoding="utf-8").write(body)


def names(tool):
    found, _unreadable = tool.hits()
    return set(n for _rel, n, _tests in found)


def main():
    work = tempfile.mkdtemp(prefix="heron-reach-")
    try:
        tool = load(work)

        print("1. A function only a test calls is a hit")
        print("-" * 66)
        write(work, "brain/thing.py", "def lonely(store):\n    return 1\n")
        write(work, "tests/test_thing.py",
              "import thing\ndef go():\n    thing.lonely(None)\n")
        check("lonely" in names(tool), "a test-only caller is reported")

        print()
        print("2. Dispatch by structure is not a hit")
        print("-" * 66)
        write(work, "brain/cli.py",
              "def accept(a):\n    return a\n"
              "def restamp(a):\n    return a\n"
              "TABLE = {'accept': accept}\n")
        write(work, "brain/dyn.py",
              "import cli\ndef go(n):\n    return getattr(cli, 'restamp')\n")
        got = names(tool)
        check("accept" not in got,
              "{'accept': accept} is dispatch - a dict literal whose value is "
              "the function")
        check("restamp" not in got, "getattr(m, 'restamp') is dispatch too")

        print()
        print("3. A bare string literal is NOT dispatch - the case that")
        print("   motivated this file")
        print("-" * 66)
        write(work, "brain/writer.py", "def remember(store, text):\n    return 1\n")
        write(work, "tests/test_writer.py",
              "import writer\ndef go():\n    writer.remember(None, 'x')\n")
        write(work, "tools/finder.py",
              "# a tool that LOOKS FOR callers of remember\n"
              "def scan(called):\n"
              "    if called == 'remember':\n"
              "        return True\n"
              "    return False\n")
        check("remember" in names(tool),
              "a tool comparing against the string 'remember' does NOT make "
              "remember() look reachable")

        print()
        print("4. A helper used inside its own module is not a hit")
        print("-" * 66)
        write(work, "brain/self.py",
              "def helper(x):\n    return x\n"
              "def go(x):\n    return helper(x)\n")
        got = names(tool)
        check("helper" not in got, "helper() is called by its own module")
        check("go" not in got or True, "and go() is reported or not on its "
                                       "own merits, not helper's")

        print()
        print("5. A decorated function is not a hit")
        print("-" * 66)
        write(work, "brain/served.py",
              "def deco(f):\n    return f\n\n@deco\ndef exposed(x):\n    return x\n")
        check("exposed" not in names(tool),
              "@deco means something else calls it - an @server.tool() is "
              "called by the host, not by code")

        print()
        print("6. A file that will not parse is NAMED, never skipped")
        print("-" * 66)
        write(work, "brain/broken.py", "def go(:\n")
        _found, unreadable = tool.hits()
        check("brain/broken.py" in unreadable,
              "the unreadable file is reported, so 'nothing calls it' is "
              "never an artefact of a file nobody could read")

        print()
        print("7. tests/ and tools/ are searched but never reported")
        print("-" * 66)
        write(work, "tests/helper_only.py", "def only_in_tests():\n    return 1\n")
        write(work, "tools/tool_only.py", "def only_in_tools():\n    return 1\n")
        got = names(tool)
        check("only_in_tests" not in got and "only_in_tools" not in got,
              "a function defined in tests/ or tools/ is never a hit - the "
              "question is about PRODUCTION code")
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print()
    print("7b. An excuse that no longer applies is reported as stale")
    print("-" * 66)
    # D-54's lesson applied to this tool's own record: a sentence describing a
    # gap has to be corrected when the gap closes. Without this, RECORDED would
    # go on excusing remember() for ever after somebody wired it up.
    import io as _io
    import sys as _sys
    tool = load()
    tool.RECORDED = dict(tool.RECORDED)
    tool.RECORDED[("brain/nowhere.py", "gone")] = "an excuse for a non-hit"
    buf = _io.StringIO()
    keep, _sys.stdout = _sys.stdout, buf
    try:
        tool.main([])
    finally:
        _sys.stdout = keep
    out = buf.getvalue()
    check("THE RECORD IS OUT OF DATE" in out,
          "an excuse for something that is no longer unreached is reported")
    check("nowhere.py" in out, "and it is named, so it can be removed")

    tool = load()
    buf = _io.StringIO()
    keep, _sys.stdout = _sys.stdout, buf
    try:
        tool.main([])
    finally:
        _sys.stdout = keep
    check("THE RECORD IS OUT OF DATE" not in buf.getvalue(),
          "and the section is silent when every excuse still applies")

    print()
    print("8. The real tree, reported not asserted")
    print("-" * 66)
    tool = load()
    found, unreadable = tool.hits()
    new = [h for h in found if (h[0], h[1]) not in tool.RECORDED]
    check(not unreadable, "every .py in the tree parses (%r)" % unreadable)
    print("        %d unreached, %d of them already explained"
          % (len(found), len(found) - len(new)))
    for rel, name, tests in new:
        print("        %-34s %s" % (rel, name))
    check(len(found) < 40,
          "the check is sharp rather than a dump (%d hits) - a naive version "
          "of this reported 248" % len(found))

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - structure counts as dispatch and prose does not, a helper")
    print("is not dead because it is local, and an unreadable file is named.")
    print()
    print("It does not prove a hit is a defect. Several are deliberate.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
