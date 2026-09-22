#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Reached by a neighbour, and the neighbour reached by nobody.

    python tests/test_module_reach.py

`tools/module-reach.py` answers `mcp/README.md`'s sentence - eight brain
modules "imported by nothing but their own tests, and therefore UNREACHABLE
FROM ANY CONVERSATION" - by counting it again rather than assuming it. It is
one of the SIX tools in `tools/` that no suite names, and row 5b-83 carries
the number it prints.

IT COUNTS ONE HOP. A module imported by another brain module lands in
"reached by mcp/ or another brain module", whether or not that neighbour is
itself reached by anything. Measured 2026-09-22 on this repository: bucket
one holds 53, and only 17 of the 145 are reachable from `mcp/` by following
imports. THIRTY-SIX are reached only by neighbours that are themselves
unreachable from any conversation - closed loops inside `brain/`.

EVERY CASE BUILDS ITS OWN TREE, in a temp folder, with `tool.ROOT` and
`tool.BRAIN_DIR` pointed at it. A suite that measured the real repository
would be asserting today's import graph, which changes whenever anybody
writes a module.

WHAT IT CANNOT DO: it does not say whether an unreached module is WRONG.
`brain/` may be a library the fragment and capability layers draw on, and
row 5b-83 is OPEN on exactly that question. This only asks whether the
report's arithmetic describes the tree underneath it.

    python tests/test_module_reach.py
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
TOOL = os.path.join(ROOT, "tools", "module-reach.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has a hyphen in it."""
    try:
        spec = importlib.util.spec_from_file_location("module_reach", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


# One tree, holding every shape that decides a bucket. Read it as a graph:
#
#   mcp/server.py      -> direct          -> chained
#   tools/a-tool.py    -> tooled
#   tests/test_lonely  -> lonely
#   brain/orphan.py    -> looped          (and orphan is reached by nobody)
#   nobody             -> nothing at all
TREE = {
    "mcp/server.py": "import direct\n",
    "brain/direct.py": "import chained\n",
    "brain/chained.py": "x = 1\n",
    "tools/a-tool.py": "import tooled\n",
    "brain/tooled.py": "x = 1\n",
    "tests/test_lonely.py": "import lonely\n",
    "brain/lonely.py": "x = 1\n",
    "brain/orphan.py": "import looped\n",
    "brain/looped.py": "x = 1\n",
    "tests/test_orphan.py": "import orphan\n",
    "brain/nobody.py": "x = 1\n",
}


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    check(tool is not None, "tools/module-reach.py loads")
    runner = getattr(tool, "main", None) if tool else None
    check(callable(runner), "and it has a main() to call")
    check(getattr(tool, "BRAIN_DIR", None) is not None
          and getattr(tool, "ROOT", None) is not None,
          "and names the tree it walks, so a test can point it elsewhere")
    if not callable(runner) or getattr(tool, "BRAIN_DIR", None) is None:
        print()
        print("FAILED")
        for line in FAILURES:
            print("  - %s" % line)
        return 1

    was = (tool.ROOT, tool.BRAIN_DIR, sys.argv)
    home = tempfile.mkdtemp(prefix="heron-reach-test-")
    try:
        def build(files):
            place = tempfile.mkdtemp(dir=home)
            for rel, body in files.items():
                path = os.path.join(place, *rel.split("/"))
                if not os.path.isdir(os.path.dirname(path)):
                    os.makedirs(os.path.dirname(path))
                io.open(path, "w", encoding="utf-8", newline="").write(body)
            return place

        def run(files, argv=()):
            """(exit code, what it printed) against a tree built here."""
            place = build(files)
            tool.ROOT = place
            tool.BRAIN_DIR = os.path.join(place, "brain")
            sys.argv = ["module-reach.py"] + list(argv)
            said = io.StringIO()
            try:
                with contextlib.redirect_stdout(said):
                    with contextlib.redirect_stderr(said):
                        code = runner()
            except SystemExit as stop:              # argparse
                code = stop.code
            except BaseException as raised:         # noqa: BLE001
                code = "raised %s" % type(raised).__name__
            return code, said.getvalue()

        def bucket(spoke, label):
            """The count the report prints beside one bucket, or None."""
            m = re.search(r"^\s*%s\s+(\d+)\s*$" % re.escape(label), spoke, re.M)
            return int(m.group(1)) if m else None

        def listed(spoke, label):
            """The module names printed under one bucket in --list."""
            lines = spoke.split("\n")
            for i, line in enumerate(lines):
                if label in line:
                    out = []
                    for rest in lines[i + 1:]:
                        if not rest.startswith("        "):
                            break
                        out.append(rest.strip())
                    return out
            return []

        B0 = "reached by mcp/ or another brain module"
        B1 = "reached only by a tool"
        B2 = "imported ONLY by its own test"
        B3 = "imported by nothing at all"

        print("1. The four buckets each hold what belongs in them")
        code, spoke = run(TREE, ["--list"])
        check(code == 0, "a report exits 0 whatever it finds, and it exits %r"
                         % code)
        check("Modules in brain/: 7" in spoke,
              "it counted the seven modules in brain/")
        check(listed(spoke, B3) == ["nobody"],
              "imported by nothing at all: %r" % listed(spoke, B3))
        check(listed(spoke, B2) == ["lonely", "orphan"],
              "imported ONLY by its own test: %r" % listed(spoke, B2))
        check(listed(spoke, B1) == ["tooled"],
              "reached only by a tool: %r" % listed(spoke, B1))
        check(listed(spoke, B0) == ["chained", "direct", "looped"],
              "reached by mcp/ or another brain module: %r"
              % listed(spoke, B0))

        print()
        print("2. A module is not its own importer")
        # brain/self.py importing itself must not count as being reached.
        code, spoke = run({"brain/self.py": "import self\n"}, ["--list"])
        check(listed(spoke, B3) == ["self"],
              "a module that imports itself is reached by nothing: %r"
              % listed(spoke, B3))

        print()
        print("3. ONE HOP IS NOT REACH")
        # `looped` is imported by `orphan`, and `orphan` is imported only by
        # its own test. So `looped` sits in the bucket a reader takes as
        # reachable, while nothing a conversation can call ever gets to it.
        code, spoke = run(TREE, ["--list"])
        check("looped" in listed(spoke, B0),
              "`looped` is in the one-hop bucket, which is honest as far as "
              "it goes")
        found = re.search(r"reachable from mcp/[^\n]*?(\d+)", spoke)
        check(found is not None,
              "AND THE REPORT SAYS HOW MANY ARE REACHABLE FROM mcp/ BY "
              "FOLLOWING IMPORTS - which is the sentence mcp/README.md "
              "actually makes, and the number row 5b-83 carries")
        if found:
            check(int(found.group(1)) == 2,
                  "and it is 2 - direct and chained - rather than the 3 the "
                  "one-hop bucket holds; it says %s" % found.group(1))
            check(bucket(spoke, B0) == 3,
                  "and the one-hop bucket still says 3, so the two numbers "
                  "are printed side by side and neither stands in for the "
                  "other - it says %r" % bucket(spoke, B0))

        print()
        print("4. A file it cannot parse counts as nothing, and says so")
        broken = dict(TREE)
        broken["mcp/broken.py"] = "def (:\n"
        code, spoke = run(broken)
        check("could not be parsed" in spoke,
              "an unparsable file is named rather than skipped in silence")
        check("mcp/broken.py" in spoke.replace(os.sep, "/"),
              "and it is named")

        print()
        print("5. Nothing to compare is COULD NOT RUN, not a clean report")
        code, spoke = run({"mcp/server.py": "x = 1\n"})
        check(code == 2, "an empty brain/ exits 2, and it exits %r" % code)
        check("COULD NOT RUN" in spoke, "and says so in those words")
    finally:
        tool.ROOT, tool.BRAIN_DIR, sys.argv = was
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the buckets hold what belongs in them, and one hop is")
    print("not mistaken for reach.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
