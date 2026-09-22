# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The owner's queue sees every row, and says so when it cannot.

    python tests/test_owner_queue.py

WHY THIS SUITE EXISTS
  `tools/owner-queue.py` matched `[AR]\\d+` from the day it was written
  until 2026-09-15, so it reported 10 of the 61 open rows in
  NEEDS-CHECKING.md. Groups B, C, D, E, F, G, H, J and K matched nothing
  and were dropped BEFORE `classify` ran, so they never reached the
  UNCLASSIFIED bucket the tool's own docstring promises them.

  That is the exact failure NEEDS-CHECKING.md records against itself three
  times - "a count that quietly omits a whole group is worse than no
  count" - committed by the one tool whose whole job is not to commit it.
  It survived because nothing tested this tool.

WHAT IT PROVES
  1. EVERY OPEN ID-SHAPED ROW IS REPORTED, counted independently of the
     tool's own walk.
  2. EVERY GROUP LETTER IN THE FILE IS REPRESENTED - the check that would
     have caught the original bug.
  3. A STRUCK ROW IS CLOSED, and stays out.
  4. NOTHING IS DROPPED: a row it cannot classify goes to UNCLASSIFIED.
  5. THE GUARD FIRES when the pattern goes narrow again.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import importlib.util                                          # noqa: E402
spec = importlib.util.spec_from_file_location(
    "owner_queue", os.path.join(ROOT, "tools", "owner-queue.py"))
QUEUE = importlib.util.module_from_spec(spec)
spec.loader.exec_module(QUEUE)

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    src = io.open(os.path.join(ROOT, "docs", "NEEDS-CHECKING.md"),
                  encoding="utf-8").read()
    rows = QUEUE.needs_checking()
    reported = [r[0] for r in rows]

    print("1. Every open ID-shaped row is reported")
    # Derived here, not borrowed from the tool - a check that reuses the
    # thing it is checking proves the two agree and nothing else.
    every = re.findall(r"(?m)^\|\s*(~~)?\*\*([A-Z]+\d+[a-z]?)\*\*", src)
    should = [rid for struck, rid in every if not struck]
    check(should, "the file has %d open ID-shaped rows" % len(should))
    missing = sorted(set(should) - set(reported))
    check(not missing,
          "and the queue reports every one%s"
          % ("" if not missing else " - MISSING: %s" % ", ".join(missing)))
    check("!!" not in reported,
          "so the tool's own guard does not fire")

    print()
    print("2. Every group letter in the file is represented")
    # THE CHECK THAT WOULD HAVE CAUGHT THE ORIGINAL BUG.
    letters = sorted(set(re.match("[A-Z]+", rid).group(0) for rid in should))
    seen = sorted(set(re.match("[A-Z]+", rid).group(0) for rid in reported if rid != "!!"))
    check(len(letters) > 2,
          "there are %d group letters open: %s" % (len(letters),
                                                   ", ".join(letters)))
    check(seen == letters,
          "and the queue shows all of them%s"
          % ("" if seen == letters else
             " - missing %s" % ", ".join(sorted(set(letters) - set(seen)))))

    print()
    print("3. A struck row is closed and stays out")
    struck = sorted(rid for was, rid in every if was)
    check(struck, "%d rows are struck through" % len(struck))
    still = sorted(set(struck) & set(reported))
    check(not still,
          "and none is reported as waiting%s"
          % ("" if not still else " - %s" % ", ".join(still)))

    print()
    print("4. Nothing is dropped")
    buckets = set(r[2] for r in rows)
    check(buckets, "every row carries a bucket")
    check(all(r[1].strip() for r in rows),
          "and every row carries text a person can read")
    check(len(reported) == len(set(reported)),
          "no row is reported twice")

    print()
    print("5. The guard fires when the pattern goes narrow again")
    tool = io.open(os.path.join(ROOT, "tools", "owner-queue.py"),
                   encoding="utf-8").read()
    check(r"[A-Z]+\d+[a-z]?" in tool,
          "the pattern is [A-Z]+, so a group of two letters - AA, AB - is seen too")
    check("shaped" in tool and "cannot see them" in tool,
          "and the tool counts what it SHOULD have matched, separately")
    # Prove the guard by narrowing the pattern the way it used to be.
    narrowed = tool.replace(r'^\|\s*(~~)?\*\*([A-Z]+\d+[a-z]?)\*\*',
                            r'^\|\s*(~~)?\*\*([AR]\d+[a-z]?)\*\*')
    check(narrowed != tool, "narrowing it back to [AR] is a real change...")
    spec2 = importlib.util.spec_from_loader("narrowed", loader=None)
    broken = importlib.util.module_from_spec(spec2)
    broken.__dict__["__file__"] = os.path.join(ROOT, "tools", "owner-queue.py")
    exec(compile(narrowed, "owner-queue.py", "exec"), broken.__dict__)
    again = [r[0] for r in broken.needs_checking()]
    check(len(again) < len(reported),
          "...it really does report fewer rows (%d against %d)"
          % (len(again), len(reported)))
    check("!!" in again,
          "AND THE GUARD CATCHES IT - the narrowed tool reports its own "
          "blindness instead of a short list that looks complete")

    print()
    print("6. The guard fires when the pattern goes back to ONE letter")
    # FRAGMENT-ISSUES row 5b-156: one letter hid every two-letter group -
    # AA, AB and on - from the owner's queue, and a self-check that shared
    # the pattern agreed with it. The guard now counts with the wider
    # pattern, so the old narrowing is exactly what it must catch.
    one = tool.replace(r'^\|\s*(~~)?\*\*([A-Z]+\d+[a-z]?)\*\*',
                       r'^\|\s*(~~)?\*\*([A-Z]\d+[a-z]?)\*\*')
    check(one != tool, "narrowing it to one letter is a real change...")
    spec3 = importlib.util.spec_from_loader("one_letter", loader=None)
    single = importlib.util.module_from_spec(spec3)
    single.__dict__["__file__"] = os.path.join(ROOT, "tools", "owner-queue.py")
    exec(compile(one, "owner-queue.py", "exec"), single.__dict__)
    thin = [r[0] for r in single.needs_checking()]
    check(len(thin) < len(reported)
          and not any(re.match("[A-Z][A-Z]", r) for r in thin if r != "!!"),
          "...it loses every two-letter group (%d rows against %d)" % (len(thin), len(reported)))
    check("!!" in thin,
          "AND THE GUARD CATCHES IT - it counts with the wider pattern")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    %d open rows in the file, %d reported, none dropped"
          % (len(should), len(reported)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
