#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Twenty-six of the questions cannot produce the finding the sweep exists for.

    python tests/test_risk_crossings.py

`tools/check-risk-crossings.py` is 440 lines and, measured 2026-09-22
counting the second hop as well as the first, one of exactly TWO never-read
tools that NOTHING runs - no suite directly, no suite through another tool,
and not CI.

WHAT THE SWEEP IS FOR. Its list of ordinary sentences exists because
`check-routing.py` can only ask what a fragment DECLARES, and the two real
crossings found on 2026-09-16 were sentences nobody declares. Its own
docstring is explicit about what makes the list worth anything:

    A question a fragment already declares is worth less here, not more:
    the point is the sentences nobody thought to claim.

and the block that added ten skill sentences on 2026-09-19 says each one
"is declared in a skill's `utterances:` and declared by NO fragment -
measured, not judged - which is exactly the hole rows 113 and 116 name:
identity cannot fire, so ranking decides, and ranking is the thing that
answers a question with a write."

THAT CRITERION HAS GONE STALE, and the report cannot show it. Measured
2026-09-22 against the live store: of 78 questions, **26 are answered by the
IDENTITY route** - a fragment declares them now - and only 52 reach ranking
at all. A question answered by identity gets a fixed answer at its own
declared risk and CANNOT be a crossing here. The headline said
`questions asked: 78`, which is the size of the list rather than the size of
the question, and a zero against 78 reads stronger than a zero against 52.

WHAT THIS SUITE DOES NOT DO. It never calls `heron_brain.lookup`, opens no
store and asserts no count about this repository - a count from that sweep
is a SAMPLE and not a measurement (FRAGMENT-ISSUES row 116: three runs
minutes apart gave 7, 9 and 8, two of them on byte-identical inputs). It
tests the judgement and the arithmetic, which do not move.
"""

import importlib.util
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-risk-crossings.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has a hyphen in it."""
    try:
        spec = importlib.util.spec_from_file_location("check_risk_crossings", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


def answer(capability, risk, others=()):
    """A reply shaped the way `heron_brain.lookup` shapes one."""
    return {
        "capability": capability,
        "risk": risk,
        "candidates": ([{"capability": capability, "risk": risk}]
                       + [{"capability": c, "risk": r} for c, r in others]),
    }


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    check(tool is not None, "tools/check-risk-crossings.py loads")
    if tool is None:
        print()
        print("FAILED - it did not import")
        return 1

    for name in ("QUESTIONS", "CHANGES_THE_MODEL", "CHANGES_A_VIEW",
                 "_index_fingerprint", "_ROUTING", "main"):
        check(getattr(tool, name, None) is not None, "and it has %s" % name)

    print()
    print("1. ONE COPY OF THE RULE, NOT TWO")
    print("   This file already imports the store fingerprint from")
    print("   check-skill-routing.py, saying in its own comment that 'two")
    print("   copies of one rule is how one of them goes stale'. The two")
    print("   tuples that decide its whole verdict were the second copy.")
    check(tool._index_fingerprint is tool._ROUTING._index_fingerprint,
          "the fingerprint is the sibling's own function")
    check(tool.CHANGES_THE_MODEL is tool._ROUTING.CHANGES_THE_MODEL,
          "and so is CHANGES_THE_MODEL")
    check(tool.CHANGES_A_VIEW is tool._ROUTING.CHANGES_A_VIEW,
          "and so is CHANGES_A_VIEW")

    print()
    print("2. THE DENOMINATOR IS THE QUESTIONS RANKING DECIDED")
    print("   A question a fragment declares is answered by IDENTITY at its")
    print("   own declared risk, so it cannot be a crossing here - and the")
    print("   list's own criterion was 'declared by NO fragment'.")
    # GUARDED, because a name that is not there yet must FAIL rather than
    # raise. An AttributeError proves nothing - heron-ship section 2a.
    denominator = getattr(tool, "denominator_lines", None)
    check(callable(denominator),
          "the report can say how many questions ranking actually decided")
    if callable(denominator):
        joined = "\n".join(denominator(78, 26))
        check("78" in joined and "26" in joined and "52" in joined,
              "all three numbers are shown - asked, by identity, and left - "
              "and it said %r" % (joined[:120],))
        check("identity" in joined.lower(),
              "and it names the route, so the reader knows why they do not "
              "count")
        none_left = "\n".join(denominator(78, 78))
        check("0" in none_left,
              "every question answered by identity leaves 0, and it said %r"
              % (none_left[:110],))
        check(denominator(78, 0),
              "and none answered by identity still prints something")

    print()
    print("3. THE DISCRIMINATOR: was a READ right there and beaten?")
    print("   'An imperative is not a question' is NOT the test, and the line")
    print("   is at EXECUTE, not at 'it is an imperative' (row 145).")
    beaten = getattr(tool, "read_that_lost", None)
    check(callable(beaten),
          "the judgement is reachable without opening a store")
    if callable(beaten):
        lost = beaten(
            answer("SET_MEP_SLOPE", "MODIFY", [("COUNT_ELEMENTS", "READ")]),
            "SET_MEP_SLOPE")
        check(lost == "COUNT_ELEMENTS",
              "a READ in the shortlist behind a MODIFY is named, and it named "
              "%r" % (lost,))
        check(beaten(
            answer("SET_MEP_SLOPE", "MODIFY", [("HIDE_ELEMENTS", "MODIFY")]),
            "SET_MEP_SLOPE") is None,
            "another write behind it is not a read that lost")
        check(beaten(
            answer("SET_MEP_SLOPE", "MODIFY", [("ISOLATE_ELEMENTS", "EXECUTE")]),
            "SET_MEP_SLOPE") is None,
            "and neither is a view change - EXECUTE is the honest middle")
        check(beaten(
            answer("DESCRIBE_BLANK_PARAMETERS", "ANALYZE",
                   [("COUNT_ELEMENTS", "READ")]),
            "DESCRIBE_BLANK_PARAMETERS") == "COUNT_ELEMENTS",
            "ANALYZE counts as safe - docs/12 gives it side effects 'none'")
        check(beaten(answer("X", "MODIFY"), "X") is None,
              "and a shortlist of one has nothing behind it")

    print()
    print("4. The list is sentences, and each one is asked once")
    said = tool.QUESTIONS
    check(isinstance(said, list) and said, "QUESTIONS is a non-empty list")
    check(all(isinstance(one, str) and one.strip() for one in said),
          "every entry is a sentence")
    dupes = sorted({one for one in said if said.count(one) > 1})
    check(not dupes, "none is asked twice, and these are: %r" % (dupes,))
    check(all(one == one.strip().lower() or one == one.strip()
              for one in said),
          "none carries stray whitespace")

    print()
    print("5. It reports and never gates")
    body = open(TOOL, encoding="utf-8").read()
    tail = body.split("def main")[-1]
    check("return 0" in tail and "return 1" not in tail,
          "main() returns 0 whatever it finds - a crossing is a judgement a "
          "person makes")

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the denominator is the questions ranking decided, and the")
    print("rule has one copy.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
