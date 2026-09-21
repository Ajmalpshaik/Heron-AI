# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   16
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Which Revit releases Heron supports, declared once and agreed everywhere.

    python tests/test_supported_releases.py

`brain/heron_dotnet.py`'s `RELEASES` is the one home. Golden Rule 4 is
"never break a working Revit version", and `docs/16` is the strategy that
rests on it - so the day a release is added or dropped, every list of them
has to move together or Heron supports different sets depending on which
file you ask.

WHY THIS SUITE EXISTS
----------------------
Two pairs were already held together and four declarations were not:

    Directory.Build.props        gated by tests/test_dotnet.py
    tools/check-compile.py       gated - its ALL_VERSIONS IS NET.RELEASES
    tools/check-api-surface.py   typed separately, nothing compared it
    brain/heron_fragment.py      typed separately, nothing compared it
    brain/heron_packages.py      typed separately, nothing compared it
    mcp/server/heron_register.py typed separately, and tests/test_register.py
                                 pinned it against THE SAME LITERAL TYPED
                                 AGAIN - a test agreeing only with itself

All of them agree today. This is what keeps them agreeing, and it is the
third time this repository has answered a two-copies problem this way:
`tests/test_heron_guard.py` for the layering rule the hook restates, and
`tests/test_host_agents.py` for the two hosts' agent definitions.

`tools/check-package.py` records the duplication in a comment and says
merging the declarations "belongs in its own review". That is still true -
five working modules is not a change to make in passing - so this holds
them together rather than merging them, and the failure message says which
file to edit.

THE DECLARATIONS ARE FOUND, NOT LISTED, which is the half that matters: a
sixth copy added next month is caught the day it arrives rather than the day
somebody notices.

WHAT IT DOES NOT COVER
-----------------------
`tests/` is excluded. A suite may legitimately use three releases as a
fixture - `tests/test_regression.py` does - and a fixture is not a claim
about what Heron supports.

A subset declared in a MODULE is a different thing and would fail here. If
one is ever legitimate, name it in `NOT_THE_SUPPORTED_LIST` below with the
reason, the way `tools/check-reachable.py` records an excuse rather than
widening a pattern until it catches nothing.
"""

from __future__ import annotations

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_dotnet as NET                                    # noqa: E402

# Where a declaration is a CLAIM about what Heron supports. tests/ is not
# here on purpose - see the docstring.
ROOTS = ("brain", "mcp", "platform", "revit", "tools")

SKIP_FOLDERS = ("__pycache__", "bin", "obj", ".vs", "fragments")

# A module-level constant whose value is a list or tuple of Revit years.
# A SET is not matched, deliberately: heron_dotnet.NEEDS_WINDOWS_DESKTOP is
# {"2025", "2026", "2027"} and is a genuine subset with its own meaning.
DECLARATION = re.compile(
    r'^([A-Z][A-Z0-9_]*)\s*=\s*[\[\(]\s*'
    r'((?:"20\d\d"\s*,?\s*|\'20\d\d\'\s*,?\s*)+)[\]\)]', re.M)

# (path, name) -> why it is NOT the supported list. Empty today, and that is
# the honest state: every declaration found is the supported list.
NOT_THE_SUPPORTED_LIST = {}

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def declarations():
    """Every module-level list of Revit years, found rather than listed."""
    found = []
    for top in ROOTS:
        base = os.path.join(ROOT, top)
        if not os.path.isdir(base):
            continue
        for where, dirs, files in os.walk(base):
            dirs[:] = [d for d in dirs if d not in SKIP_FOLDERS]
            for name in sorted(files):
                if not name.endswith(".py"):
                    continue
                path = os.path.join(where, name)
                rel = os.path.relpath(path, ROOT).replace(os.sep, "/")
                with io.open(path, encoding="utf-8", errors="replace") as fh:
                    text = fh.read()
                for m in DECLARATION.finditer(text):
                    line = text[:m.start()].count("\n") + 1
                    years = tuple(re.findall(r"20\d\d", m.group(2)))
                    found.append((rel, line, m.group(1), years))
    return sorted(found)


def main():
    supported = tuple(NET.RELEASES)

    print("\nThe one home")
    check(len(supported) >= 2,
          "brain/heron_dotnet.RELEASES names %d release(s): %s"
          % (len(supported), ", ".join(supported)))

    found = declarations()
    print("\n%d declaration(s) found across %s"
          % (len(found), ", ".join(ROOTS)))

    # AN EMPTY SWEEP AGREES WITH EVERYTHING. If the pattern stops matching -
    # somebody reformats a list onto one line per year, say - this suite
    # would pass having compared nothing, which is the failure it exists to
    # prevent in other files.
    check(len(found) >= 4,
          "enough to be comparing something - a sweep that finds nothing "
          "passes perfectly and means the opposite")

    print("\nEvery one of them is the supported list")
    for rel, line, name, years in found:
        why = NOT_THE_SUPPORTED_LIST.get((rel, name))
        if why:
            print("  --    %s:%d %s is excused: %s" % (rel, line, name, why))
            continue
        check(years == supported,
              "%s:%d %s%s" % (rel, line, name,
                              "" if years == supported else
                              "  <- says %s ; RELEASES says %s"
                              % (list(years), list(supported))))

    print()
    if FAILURES:
        print("FAILED")
        for one in FAILURES:
            print("  - %s" % one)
        print("\n  Edit the file named above, or - if it is genuinely NOT the")
        print("  supported list - add it to NOT_THE_SUPPORTED_LIST here with")
        print("  the reason. Do not widen the pattern.")
        return 1
    print("PASSED - one home, and every declaration agrees with it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
