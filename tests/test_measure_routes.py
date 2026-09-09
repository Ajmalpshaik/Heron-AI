# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The one thing measure-routes.py CONCLUDES, and the reason it is parsed not grepped.

    python tests/test_measure_routes.py

WHY THIS TEST EXISTS. Most of that tool counts routes, and a miscount would be
visible. One function draws a CONCLUSION - remember_callers() decides whether
the utterance cache can be written in production at all, and the whole finding
behind Q-43 rests on its answer.

The first version grepped for the text "remember(" and matched THE TOOL'S OWN
DOCSTRING, which describes the problem. It printed "the cache fills with use" -
the exact opposite of the truth - in the one place the tool exists to be right
about. A text search cannot tell a call from a sentence.

So it parses with `ast` now, and these cases are the difference:

  * prose naming remember() in a docstring or comment is NOT a caller
  * a real call is, plain or through a module
  * a file that will not parse is REPORTED, never silently skipped - "no
    production caller" must not be an artefact of a file nobody could read

WHAT IT DOES NOT PROVE. That the cache should or should not be written. That is
Q-43 and it is the owner's to answer.
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
    """Import the tool by path, optionally pointed at a temporary tree."""
    path = os.path.join(ROOT, "tools", "measure-routes.py")
    spec = importlib.util.spec_from_file_location("measure_routes", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    if root is not None:
        module.ROOT = root
    return module


def check(name, condition, detail=""):
    if condition:
        print("  ok    %s" % name)
    else:
        print("  FAIL  %s %s" % (name, detail))
        FAILURES.append("%s %s" % (name, detail))


def write(folder, name, body):
    path = os.path.join(folder, name)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    io.open(path, "w", encoding="utf-8").write(body)


def main():
    print("remember_callers() - a call, not a sentence")
    print("-" * 62)

    work = tempfile.mkdtemp(prefix="heron-routes-")
    try:
        tool = load(work)

        # The exact shape that fooled the first version: prose describing the
        # function, in a docstring, with the parenthesis attached.
        write(work, "prose.py", '"""remember() is never called here."""\nX = 1\n')
        write(work, "comment.py", "# nothing calls remember(store, text, fid)\nY = 2\n")
        check("a docstring naming remember() is not a caller",
              tool.remember_callers() == [],
              "(got %r)" % tool.remember_callers())

        write(work, "plain.py", "from x import remember\ndef go(s, t):\n    remember(s, t, 'FRG-1')\n")
        got = tool.remember_callers()
        check("a plain call IS a caller", got == ["plain.py"], "(got %r)" % got)

        write(work, "viamodule.py", "import heron_search as S\ndef go(s, t):\n    S.remember(s, t, 'FRG-1')\n")
        got = tool.remember_callers()
        check("a call through a module IS a caller",
              got == ["plain.py", "viamodule.py"], "(got %r)" % got)

        # A name that merely CONTAINS remember must not match. `ast` gives this
        # for free where a substring grep would not.
        write(work, "similar.py", "def go(s):\n    s.remembers()\n    s.remember_all()\n")
        got = tool.remember_callers()
        check("remembers() and remember_all() are not remember()",
              "similar.py" not in got, "(got %r)" % got)

        write(work, "broken.py", "def go(:\n")
        got = tool.remember_callers()
        check("a file that will not parse is reported, not skipped",
              "UNREADABLE:broken.py" in got, "(got %r)" % got)

        # The definition itself is not a call. Without this, heron_search.py
        # would always look like its own caller and the finding would invert.
        shutil.rmtree(work)
        os.makedirs(work)
        write(work, "defonly.py", "def remember(store, text, fid):\n    pass\n")
        check("the definition of remember() is not a call to it",
              tool.remember_callers() == [],
              "(got %r)" % tool.remember_callers())

        # __pycache__ holds stale copies of production code; counting one would
        # report a caller that no longer exists in the tree.
        write(work, os.path.join("__pycache__", "old.py"), "S.remember(a, b, c)\n")
        check("__pycache__ is not searched",
              tool.remember_callers() == [],
              "(got %r)" % tool.remember_callers())
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print()
    print("is_test() - a caller in a test is not a production caller")
    print("-" * 62)
    tool = load()
    check("tests/ is a test", tool.is_test(os.path.join("tests", "test_search.py")))
    check("a test_ file anywhere is a test",
          tool.is_test(os.path.join("brain", "test_thing.py")))
    check("brain/heron_search.py is not a test",
          not tool.is_test(os.path.join("brain", "heron_search.py")))

    print()
    print("the real tree - the finding behind Q-43")
    print("-" * 62)
    callers = tool.remember_callers()
    unreadable = [c for c in callers if c.startswith("UNREADABLE:")]
    production = [c for c in callers if not c.startswith("UNREADABLE:") and not tool.is_test(c)]
    check("every .py in the tree parses", not unreadable, "(%r)" % unreadable)
    print("        callers found: %s" % (", ".join(callers) or "none"))
    if production:
        print("        NOTE: remember() now has a production caller (%s)."
              % ", ".join(production))
        print("        Q-43 has been acted on. The tool's warning must be")
        print("        re-read, and this note is how you find out.")

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - prose is not a call, a module call is, a name that merely")
    print("contains 'remember' is not, and a file that will not parse is named")
    print("rather than quietly dropped out of the answer.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
