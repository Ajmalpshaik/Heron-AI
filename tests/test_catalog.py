# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-FRG-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Fragment Documentation Agent - the catalogue it generates.

    python tests/test_catalog.py

WHY THIS EXISTS WHEN generate-agent-map.py HAS NO TEST
------------------------------------------------------
Generators are not unit-tested in this repository, and mostly they should not
be: a map that reads a table and draws it either draws it or does not, and a
person sees which. This one carries a JUDGEMENT - it decides whether a
fragment's declared negative case can actually be run - and that judgement was
wrong on its first run, reporting 0 stranded cases across a library holding
18. A generator that only draws needs no test. One that concludes does.

WHAT IT PROVES
  1. Every fragment in the library appears exactly once.
  2. A declared negative case of the disproved shape is flagged, even when the
     fragment also has workable ones - the bug that shipped for one run.
  3. A fragment whose ONLY negative case is stranded says BLOCKING; one with a
     workable case left says it is not. Those are different situations and a
     page that merged them would be the same failure in a new place.
  4. A fragment with no stranded case is not flagged.
  5. The page is real output: the placeholder is gone, the embedded data is
     valid JSON, and its counts match the library it was built from.

WHAT IT DOES NOT PROVE. That the page LOOKS right. Nothing here renders it.
"""

import io
import os
import sys
import json
import re
import shutil
import tempfile
import importlib.util

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as HF                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load_tool():
    """The generator, loaded by path - its filename has hyphens in it."""
    path = os.path.join(ROOT, "tools", "generate-fragment-catalog.py")
    spec = importlib.util.spec_from_file_location("heron_catalog_tool", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def fake(negative):
    """A Fragment whose cases() returns what this test wants to try."""
    frag = HF.Fragment({"id": "FRG-T-001", "capability": "TEST"}, "nowhere")
    frag.cases = lambda: ({"positive": [{"given": "something", "expect": "x"}],
                           "negative": [{"given": g, "expect": "empty"}
                                        for g in negative]}, None)
    return frag


def main():
    tool = load_tool()

    print("2, 3 and 4. Which declared negative cases can actually be run")

    _p, _n, note = tool.cases_of(fake(["an empty element list",
                                       "a set containing only walls"]))
    check(note is not None,
          "a stranded case is flagged even when a workable one exists")
    check(note is not None and "Not blocking" in note,
          "and it says it is NOT blocking, because one workable case remains")

    _p, _n, note = tool.cases_of(fake(["an empty element list"]))
    check(note is not None and "BLOCKING" in note,
          "a fragment whose only negative case is stranded says BLOCKING")

    _p, _n, note = tool.cases_of(fake(["a set containing only walls",
                                       "a curtain wall"]))
    check(note is None, "a fragment with no stranded case is not flagged")

    _p, _n, note = tool.cases_of(fake([]))
    check(note is None, "no negative cases at all is not reported as stranded")

    print()
    print("1 and 5. The page is real output over the real library")
    work = tempfile.mkdtemp(prefix="heron-catalog-")
    try:
        out = os.path.join(work, "catalog.html")
        keep = os.environ.get("HERON_CATALOG_OUT")
        os.environ["HERON_CATALOG_OUT"] = out
        try:
            tool = load_tool()          # re-read OUT from the environment
            quiet = io.StringIO()
            stdout, sys.stdout = sys.stdout, quiet
            try:
                code = tool.main()
            finally:
                sys.stdout = stdout
        finally:
            if keep is None:
                os.environ.pop("HERON_CATALOG_OUT", None)
            else:
                os.environ["HERON_CATALOG_OUT"] = keep

        check(code == 0, "the generator exits 0")
        check(os.path.exists(out), "and writes the file it was pointed at")

        text = io.open(out, encoding="utf-8").read()
        check("__DATA__" not in text, "the data placeholder is gone")

        found = re.search(r"^var D = (\{.*\});$", text, re.M)
        check(found is not None, "the embedded data line is there")
        data = json.loads(found.group(1)) if found else {"rows": []}

        library, _problems = HF.load_all()
        slugs = [r["slug"] for r in data["rows"]]
        check(len(slugs) == len(library),
              "every fragment appears (%d of %d)" % (len(slugs), len(library)))
        check(len(set(slugs)) == len(slugs), "and none appears twice")
        check(data.get("proven") == sum(
            1 for f in library.values() if f.data.get("proof")),
            "the proven count matches the library rather than being carried")
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a stranded negative case is flagged whether or not it")
    print("blocks the fragment, and the page's counts come from the library.")
    print()
    print("It says nothing about how the page looks, and nothing about whether")
    print("any fragment in it works.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
