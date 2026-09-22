#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The file that forbids two numbers for one measurement, carrying two.

    python tests/test_check_intrusion.py

`tools/check-intrusion.py` asks the question `check-routing.py` cannot: for
every utterance in the library, who ELSE came back? It is the last of the
seven CI gates that had never been opened, and this is the first suite it has
ever had.

ITS DOCSTRING STATES THE RULE AND ITS OWN OUTPUT BREAKS IT. The docstring
records what the tool measured the first time it ran, and then says in terms:

    Quote what THIS TOOL prints, never this line. An earlier hand-written
    probe on the same day gave 0.234 over 312 utterances - a different corpus
    one commit back, not a different finding. TWO NUMBERS FOR ONE MEASUREMENT
    IS HOW A FIGURE BECOMES A REMEMBERED COMPOSITE that matches no run that
    ever happened, which is a failure this repository has already had once and
    now has a checker for.

And then the branch that fires when the correlation has gone strong prints
the HAND PROBE's number as what the tool measured - at exactly the moment a
reader needs the right baseline to compare against.

THIS SUITE NEVER RUNS THE MEASUREMENT. That opens the knowledge store,
re-indexes it and retrieves for every utterance in the library - measured
2026-09-22 at 396 fragments and 2,227 utterances, and it takes minutes. So
what is asked here is the arithmetic, the two claims agreeing with each
other, and the settings being refused before any of that starts.

WHAT IT CANNOT DO: it does not say whether any intrusion is a defect. The
tool says plainly that none of them is by itself, and exits 0 for that
reason - "a gate here would be a gate on how ordinary somebody's phrasing
is".

    python tests/test_check_intrusion.py
"""

import contextlib
import importlib.util
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-intrusion.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has a hyphen in it."""
    try:
        spec = importlib.util.spec_from_file_location("check_intrusion", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    check(tool is not None, "tools/check-intrusion.py loads")
    if tool is None:
        print()
        print("FAILED")
        for line in FAILURES:
            print("  - %s" % line)
        return 1

    source = io.open(TOOL, encoding="utf-8").read()

    print()
    print("1. Pearson, written out, with no dependency for it")
    r = getattr(tool, "correlation", None)
    check(callable(r), "it still has correlation()")
    if callable(r):
        check(abs(r([1, 2, 3], [2, 4, 6]) - 1.0) < 1e-9,
              "a perfect straight line is 1.0")
        check(abs(r([1, 2, 3], [6, 4, 2]) + 1.0) < 1e-9,
              "and the same line downhill is -1.0")
        check(abs(r([1, 2, 3], [5, 5, 5])) < 1e-9,
              "a flat answer correlates with nothing - and dividing by that "
              "zero would be a crash, so it returns 0.0")
        check(abs(r([1], [1])) < 1e-9,
              "one point is not a correlation either")

    print()
    print("2. TWO NUMBERS FOR ONE MEASUREMENT")
    # The docstring records what THIS TOOL measured the first time it ran,
    # and separately names a hand probe's number as NOT that. The branch
    # that quotes a past figure back at a reader must quote the tool's own.
    recorded = re.findall(r"correlation\(purpose words, intrusions\) = "
                          r"(0\.\d+), over (\d+) utterances", source)
    check(len(recorded) == 1,
          "the docstring records what the tool itself measured first, once")
    recorded = recorded[0] if recorded else None
    # WHICHEVER WORDS THE BRANCH USES, the figure it calls "measured" is the
    # one being compared against. A second figure the file merely mentions is
    # introduced as something else - the docstring says "gave" for the hand
    # probe - so the word is the distinction, not the number.
    quoted = re.findall(r"measured\s+(0\.\d+)", source)
    check(len(quoted) == 1,
          "the strong-correlation branch quotes exactly one figure as "
          "measured, and it quotes %d" % len(quoted))
    if recorded and quoted:
        check(recorded[0] == quoted[0],
              "and it is THE SAME NUMBER the docstring records - the "
              "docstring says %s and the branch says %s"
              % (recorded[0], quoted[0]))

    print()
    print("3. A SETTING IT DOES NOT HAVE IS REFUSED BEFORE THE STORE OPENS")
    # Opening the store and retrieving for 2,227 utterances takes minutes.
    # A mistyped flag must not cost that, and must not be silently ignored.
    settings = getattr(tool, "settings_from", None)
    check(callable(settings),
          "the settings are read by something a test can call, so a bad one "
          "is refused BEFORE the store is opened")
    if callable(settings):
        for label, argv in (("a misspelt flag", ["--tpo", "5"]),
                            ("a flag with no value", ["--top"]),
                            ("a count that is not a number", ["--top", "six"]),
                            ("a bare word", ["12"])):
            said = io.StringIO()
            try:
                with contextlib.redirect_stdout(said):
                    with contextlib.redirect_stderr(said):
                        code = tool.main(argv)
            except BaseException as raised:         # noqa: BLE001
                code = "raised %s" % type(raised).__name__
            check(code == 2,
                  "%s is refused with 2, and it gives %r" % (label, code))
            check("Fragments:" not in said.getvalue(),
                  "%s: and no measurement was run" % label)
        top, why = settings(["--top", "20"])
        check(why is None and top == 20,
              "and a setting it does have is accepted: %r" % (top,))

    print()
    print("4. It is a report, and it says so")
    check("return 0" in source and "not a defect" in source,
          "it exits 0 whatever it finds - a gate here would be a gate on how "
          "ordinary somebody's phrasing is")
    check("COULD NOT RUN" in source and "return 2" in source,
          "and no knowledge store is COULD NOT RUN at exit 2, not a pass and "
          "not a traceback")

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - one number for one measurement, and a setting it does not")
    print("have costs nothing.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
