# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-PRO-011
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The instruction registry - assembled from one source, and nothing copied.

    python tests/test_instructions.py

WHAT IT PROVES
  1. THE CONSTITUTION PARSES INTO ITS 30 NUMBERED RULES, including the three
     with a letter (12a, 12b, 12c). A parser that silently found 27 would
     drop three rules about which model is being written to, which are the
     three it can least afford to drop.

  2. AN ARTICLE IS ASSEMBLED, NOT COPIED. The composed instruction carries the
     Constitution's own words because it read them, and the registry refuses
     an instruction that pastes them instead. This is the whole reason the
     module exists.

  3. INCLUDES COMPOSE, AND A CYCLE IS REFUSED RATHER THAN HUNG.

  4. A MISSING INSTRUCTION, INCLUDE OR ARTICLE RAISES - each one a failure
     state the contract declares. A composer that returned a partial
     instruction would be an agent running without a rule it was never given.

  5. AN INSTRUCTION WITH NO EVALUATION CASE IS REFUSED (docs/23 s9).

  6. THE REAL REGISTRY VALIDATES AND ITS CASES PASS - the check that another
     commit can break, which is what makes it worth running.

  7. A MODIFY AGENT IS NOT TOLD ITS PERMISSION STOPS AT READING. agent.modify
     included agent.read for one commit and inherited exactly that sentence.
     The case that catches it lives in the registry; this asserts the registry
     still carries that case, because the case is the guard.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_instructions as INS                              # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def raises(call, kind):
    try:
        call()
    except kind:
        return True
    except Exception:                                        # noqa: BLE001
        return False
    return False


def main():
    rules = INS.articles()

    print("1. The Constitution parses into its numbered rules")
    check(len(rules) == 30, "30 numbered rules found, not 27")
    check(all(n in rules for n in ("12a", "12b", "12c")),
          "the three lettered rules about which model survive the parse")
    check("one press of undo" in rules["8"].lower()
          or "one step" in rules["8"].lower(),
          "article 8 carries its own words, read from the file")

    print()
    print("2. Assembled, never copied")
    text, used = INS.compose("agent.modify")
    check("12a" in used and "9" in used,
          "agent.modify assembles the articles it declares")
    check(rules["9"].splitlines()[0] in text,
          "the article's own wording reaches the agent because it was read")

    pasted = {
        "copycat": {
            "id": "copycat", "version": "1.0.0", "purpose": "x",
            "text": rules["9"], "cases": [{"name": "c",
                                           "must-contain": ["x"]}],
            "_path": "copycat.yaml",
        }
    }
    problems = INS.validate(pasted, rules)
    check(any("repeats the wording of article" in p for p in problems),
          "an instruction that pastes an article is refused, and told to "
          "declare it instead")

    print()
    print("3. Includes compose, and a cycle is refused")
    check("State your evidence" in text,
          "agent.modify carries what agent.base gives it")
    loop = {
        "a": {"id": "a", "version": "1.0.0", "purpose": "p", "text": "A",
              "includes": ["b"], "cases": [{"name": "c"}], "_path": "a.yaml"},
        "b": {"id": "b", "version": "1.0.0", "purpose": "p", "text": "B",
              "includes": ["a"], "cases": [{"name": "c"}], "_path": "b.yaml"},
    }
    check(raises(lambda: INS.compose("a", loop, rules), ValueError),
          "a -> b -> a is reported as a cycle, not followed")
    check(any("INCLUDE_CYCLE" in p for p in INS.validate(loop, rules)),
          "validate() names it INCLUDE_CYCLE rather than hanging on it")

    print()
    print("3b. An included instruction's articles arrive once, not twice")
    modify, used = INS.compose("agent.modify")
    check(len(used) == len(set(used)), "no article number is collected twice")
    first_line = rules["22"].splitlines()[0]
    check(modify.count(first_line) == 1,
          "agent.base's article 22 appears once in agent.modify")
    check(modify.count("Rules you may not violate") == 1,
          "and the line that introduces them appears once")

    print()
    print("4. Missing things raise, one per declared failure state")
    check(raises(lambda: INS.compose("no.such.instruction"), KeyError),
          "an unknown instruction raises")
    missing_include = {
        "x": {"id": "x", "version": "1.0.0", "purpose": "p", "text": "X",
              "includes": ["gone"], "cases": [{"name": "c"}], "_path": "x.yaml"}
    }
    check(raises(lambda: INS.compose("x", missing_include, rules), KeyError),
          "an unknown include raises")
    missing_article = {
        "y": {"id": "y", "version": "1.0.0", "purpose": "p", "text": "Y",
              "articles": [99], "cases": [{"name": "c"}], "_path": "y.yaml"}
    }
    check(raises(lambda: INS.compose("y", missing_article, rules), KeyError),
          "an article that is not in the Constitution raises")
    check(any("not in HERON_CONSTITUTION.md" in p
              for p in INS.validate(missing_article, rules)),
          "and validate() says which article it was")

    print()
    print("5. An instruction nothing tests is refused")
    untested = {
        "z": {"id": "z", "version": "1.0.0", "purpose": "p", "text": "Z",
              "cases": [], "_path": "z.yaml"}
    }
    check(any("no evaluation case" in p for p in INS.validate(untested, rules)),
          "no evaluation case is a refusal, not a warning")

    print()
    print("6. The registry in this repository")
    known = INS.instructions()
    check(len(known) >= 3, "there are instructions to check")
    problems = INS.validate(known, rules)
    check(problems == [], "every instruction validates")
    for line in problems:
        print("        %s" % line)
    passed, case_failures = INS.run_cases(known, rules)
    check(passed > 0 and case_failures == [],
          "%d evaluation assertion(s) pass, none fail" % passed)
    for line in case_failures:
        print("        %s" % line)

    print()
    print("7. The guard that caught the composition bug is still there")
    modify = known.get("agent.modify", {})
    guard = [c for c in modify.get("cases") or []
             if "Your permission stops at reading"
             in (c.get("must-not-contain") or [])]
    check(bool(guard),
          "agent.modify still asserts it is NOT told it may only read")
    check("Your permission stops at reading" not in text,
          "and the assembled text does not contain it")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    instructions assemble from one source, and nothing copies")
    return 0


if __name__ == "__main__":
    sys.exit(main())
