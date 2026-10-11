#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Every tracked Python file compiles without a warning. Needs no Revit.

    python tests/test_compiles_quietly.py

WHY. A backslash in an ordinary string that does not start an escape - a
Windows path in a docstring, `\\Z` quoted from a regex - is a SyntaxWarning
from Python 3.12, printed to stderr every time the file is compiled, and an
error in a later release. It has been found and fixed one file at a time:
heron_placement.py (row 5b-379, with a check for that one file only), then
hook_log.py, whose warning reached every Write and Edit hook's stderr on the
owner's laptop (row 5b-408), and on the same day test_bridge_roundtrip.py and
tools/check-products.py, found by compiling the whole tree once. A check that
holds one file lets the next one in; this holds all of them.

WHAT IT PROVES
  1. Every Python file git tracks compiles, and compiling it records no
     warning of any kind. Compiled from source here, never imported: an
     import can use cached bytecode and warn nothing.

It exits 3, COULD NOT RUN, when git cannot list the files - that is not a
pass (tests/README.md).
"""

import io
import os
import subprocess
import sys
import warnings

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def tracked_python():
    """The .py files git tracks here, or None when git cannot say."""
    try:
        listed = subprocess.check_output(["git", "ls-files", "--", "*.py"],
                                         cwd=ROOT, stderr=subprocess.DEVNULL)
    except (OSError, subprocess.CalledProcessError):
        return None
    return [line for line in listed.decode("utf-8").splitlines() if line.strip()]


def main():
    files = tracked_python()
    if not files:
        print("COULD NOT RUN - git did not list the tracked Python files here.")
        return 3

    noisy = []
    for name in files:
        try:
            with io.open(os.path.join(ROOT, name), encoding="utf-8") as handle:
                source = handle.read()
        except (OSError, UnicodeDecodeError) as exc:
            noisy.append("%s: could not be read (%s)" % (name, exc))
            continue
        with warnings.catch_warnings(record=True) as heard:
            warnings.simplefilter("always")
            try:
                compile(source, name, "exec")
            except SyntaxError as exc:
                noisy.append("%s: does not compile - %s" % (name, exc))
                continue
        noisy.extend("%s line %s: %s: %s" % (name, w.lineno, w.category.__name__,
                                             w.message) for w in heard)

    print("1. Every tracked Python file compiles without a warning")
    print("  %s    %d file(s) compiled, %d warning(s)"
          % ("ok  " if not noisy else "FAIL", len(files), len(noisy)))
    for line in noisy:
        print("        %s" % line)
    print()
    if noisy:
        print("FAILED - double the backslash, or make the string raw.")
        return 1
    print("PASSED - nothing tracked prints a warning when Python compiles it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
