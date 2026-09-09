# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The two pieces of measure-brain.py that COMPUTE rather than time.

    python tests/test_measure_brain.py

WHY ONLY TWO PIECES. Most of that tool is a clock around somebody else's code,
and a clock cannot be unit tested into being right. Two parts do arithmetic,
and a wrong answer in either produces a table that looks entirely normal:

  sample()  decides WHICH requests are asked. If it is not deterministic, two
            runs ask different questions and the whole point - comparing a run
            against a baseline - quietly stops working. Nothing about the
            output would look wrong.

  stat()    computes the median and the worst. A median is the one statistic
            people read without checking, and an even-length list is where a
            hand-written one usually goes wrong.

WHAT IT DOES NOT PROVE. That any timing is accurate. That is the machine's
business, and the tool prints the machine for exactly that reason.
"""

import os
import sys
import importlib.util

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FAILURES = []


def load():
    """Import the tool by path - it lives in tools/ and is not a package."""
    path = os.path.join(ROOT, "tools", "measure-brain.py")
    spec = importlib.util.spec_from_file_location("measure_brain", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def check(name, condition, detail=""):
    if condition:
        print("  ok    %s" % name)
    else:
        print("  FAIL  %s %s" % (name, detail))
        FAILURES.append("%s %s" % (name, detail))


def main():
    tool = load()

    print("sample() - the same questions every run")
    print("-" * 62)

    phrases = ["select all ducts", "grey the background", "count the sheets",
               "find unused materials", "move them up", "tag every duct"]

    first = tool.sample(phrases, 3)
    second = tool.sample(phrases, 3)
    check("two calls return the same list", first == second,
          "(%r vs %r)" % (first, second))

    shuffled = list(reversed(phrases))
    check("directory order does not change the sample",
          tool.sample(shuffled, 3) == first,
          "(%r vs %r)" % (tool.sample(shuffled, 3), first))

    check("asking for 3 gives 3", len(first) == 3, "(got %d)" % len(first))

    # A stride, not the first N. Taking phrases[:3] would sample one corner of
    # an alphabetically sorted library - every fragment whose words start with
    # 'a' - and call it a spread.
    check("it strides rather than taking the first three",
          first != sorted(set(phrases))[:3],
          "(got the first three: %r)" % first)

    everything = tool.sample(phrases, 99)
    check("asking for more than there are returns all of them",
          len(everything) == len(set(phrases)),
          "(got %d of %d)" % (len(everything), len(set(phrases))))

    check("an empty library samples to nothing", tool.sample([], 5) == [])

    # Duplicated utterances are real: two fragments may declare the same words,
    # and asking the same question twice would weight the median toward it.
    check("a duplicate is asked once",
          len(tool.sample(["a", "a", "b"], 9)) == 2,
          "(got %r)" % tool.sample(["a", "a", "b"], 9))

    print()
    print("stat() - median and worst")
    print("-" * 62)

    clock = tool.Timer()
    for value in (10.0, 2.0, 6.0):
        clock.readings.setdefault("s", []).append(value)
    clock.order.append("s")
    runs, median, worst = clock.stat("s")
    check("odd count takes the middle value", median == 6.0, "(got %r)" % median)
    check("worst is the largest, not the last", worst == 10.0, "(got %r)" % worst)
    check("runs counts every reading", runs == 3, "(got %d)" % runs)

    even = tool.Timer()
    for value in (1.0, 2.0, 3.0, 4.0):
        even.readings.setdefault("s", []).append(value)
    even.order.append("s")
    _runs, median, _worst = even.stat("s")
    check("even count averages the middle pair", median == 2.5, "(got %r)" % median)

    empty = tool.Timer()
    check("a stage never run reports nothing rather than zero",
          empty.stat("never") == (0, None, None),
          "(got %r)" % (empty.stat("never"),))

    print()
    print("time() - the reading is kept and the value passes through")
    print("-" * 62)

    passed = tool.Timer()
    got = passed.time("work", lambda: "the answer")
    check("the wrapped call's return value comes back", got == "the answer",
          "(got %r)" % got)
    check("one call leaves one reading", len(passed.readings["work"]) == 1)
    check("the stage is remembered in call order", passed.order == ["work"],
          "(got %r)" % passed.order)

    print()
    print("fmt() - a number a person reads")
    print("-" * 62)
    check("nothing measured prints a dash", tool.fmt(None) == "-")
    check("sub-10 ms keeps two decimals", tool.fmt(1.234) == "1.23",
          "(got %r)" % tool.fmt(1.234))
    check("over 100 ms drops them", tool.fmt(1246.4) == "1246",
          "(got %r)" % tool.fmt(1246.4))

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the sample is deterministic and independent of directory")
    print("order, the median survives an even count, and a stage that never ran")
    print("reports nothing rather than zero.")
    print()
    print("It proves nothing about whether a timing is accurate. That is the")
    print("machine's business, which is why the tool prints the machine.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
