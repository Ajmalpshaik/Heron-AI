#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The layering rules, in the language two thirds of this repository is written in.

WHY THIS FILE EXISTS
--------------------
`tools/check-structure.py` has enforced the dependency table since Step 1 and,
until 2026-09-12, only against C# project references. Its own source said so:
the Python side *"has never been checked here at all"*. An unchecked rule reads
as an enforced one, which is worse than an absent one.

The checks below are all negative, because a layering gate that has never been
seen to say no is a layering gate nobody knows the meaning of. Each writes one
throwaway file into `brain/`, runs the real checker, and removes it again -
including when the check fails.

    python tests/test_layering.py
"""

import importlib.util
import io
import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROBE = os.path.join(ROOT, "brain", "_layering_probe.py")

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    path = os.path.join(ROOT, "tools", "check-structure.py")
    spec = importlib.util.spec_from_file_location("heron_check_structure", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def run_checker():
    out = subprocess.run([sys.executable, os.path.join(ROOT, "tools", "check-structure.py")],
                         capture_output=True, text=True, cwd=ROOT)
    return out.returncode, out.stdout


def with_probe(text):
    io.open(PROBE, "w", encoding="utf-8").write(text)
    try:
        return run_checker()
    finally:
        if os.path.exists(PROBE):
            os.unlink(PROBE)


def main():
    struct = load()

    print("The table itself")
    check(struct.ALLOWED["platform"] == set(),
          "platform may depend on nothing")
    check("brain" not in struct.ALLOWED["revit"] and "revit" not in struct.ALLOWED["brain"],
          "revit and brain never touch - the brain stays runnable with no Revit "
          "on the machine")
    check("brain" in struct.ALLOWED["mcp"],
          "mcp may depend on brain, which is the seam docs/02 draws")

    print()
    print("Reading imports rather than grepping for them")
    check(struct.python_imports("import os\nfrom heron_brain import x\n")
          == {"os", "heron_brain"},
          "both import forms are found")
    check(struct.python_imports("'''a docstring mentioning import heron_brain'''\n")
          == set(),
          "and prose that merely contains the words is NOT an import - failing a "
          "build over a docstring is how a gate gets skimmed past")
    check(struct.python_imports("def broken(\n") is None,
          "a file that does not parse says so rather than reporting no imports")

    print()
    print("The checker says no, and the repository is clean without the probe")
    code, out = run_checker()
    check(code == 0, "the tree as it stands passes")

    code, out = with_probe("from heron_brain import anything\n")
    check(code == 1 and "heron_brain" in out,
          "a brain module importing an mcp one fails the build and names it")
    check("brain may depend on: platform" in out,
          "and says what it may depend on instead")

    code, out = with_probe("'''mentions import heron_brain in prose only'''\nimport os\n")
    check(code == 0, "the same words inside a docstring do not")

    code, out = with_probe("def broken(\n")
    check(code == 1 and "does not parse" in out,
          "and a file whose imports cannot be read is reported, never skipped")

    print()
    check(not os.path.exists(PROBE), "the probe file is gone afterwards")
    code, _ = run_checker()
    check(code == 0, "and the tree is clean again")

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1
    print("PASSED - the Python half of the layering table is enforced, and was")
    print("         watched refusing three different things.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
