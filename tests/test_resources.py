# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-RES-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Resource Manager - a reading that failed is not a reading of zero.

    python tests/test_resources.py

WHAT IT PROVES
  1. FIVE THINGS ARE ASKED FOR AND THREE CAN BE SEEN. Revit responsiveness
     and AI spend are reported UNMEASURED every time, with the reason - an
     absent key reads as an oversight, a present one saying no reads as a
     decision.

  2. A READING THAT FAILED IS NEVER COMPARED AGAINST A THRESHOLD. Returning
     0% when the file was missing would report a machine under no pressure
     at all, on the platform Heron ships to.

  3. WITH NOTHING MEASURABLE IT PAUSES, and says NOTHING_MEASURABLE rather
     than a boolean that means both "busy" and "blind". "I could not tell"
     is not permission to use somebody's machine - the same reading
     HERON-OPS-SCH-001 gives silence.

  4. EACH THRESHOLD CROSSED IS NAMED WITH ITS NUMBER, so a person can
     disagree with the threshold rather than with the verdict.

  5. IT NEVER ANSWERS ABOUT A PERSON'S WORK. docs/21 s12 has background
     yielding to interactive and never the other way round.

  6. IT COUNTS NO SPEND OF ITS OWN. HERON-KRN-TOK-015 is the meter, and two
     meters for one number is one meter and one argument.

  7. IT FEEDS THE SCHEDULER, and the seam really fits: a pause here is a
     scheduler that dispatches nothing background.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_resources as RES                                 # noqa: E402
import heron_contract as CON                                  # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def fixed(**values):
    """Readings with the named ones measured and the rest unmeasured."""
    found = {}
    for name in ("cpu_per_core", "memory_free", "disk_free",
                 "revit_responsiveness", "ai_spend"):
        if name in values:
            found[name] = {"value": values[name], "unmeasured": False,
                           "why": "a fixture"}
        else:
            found[name] = {"value": None, "unmeasured": True,
                           "why": "a fixture that could not read it"}
    return found


def main():
    source = open(os.path.join(ROOT, "brain", "heron_resources.py"),
                  encoding="utf-8").read()

    print("1. Five asked for, three visible")
    found = RES.reading()
    check(sorted(found) == ["ai_spend", "cpu_per_core", "disk_free",
                            "memory_free", "revit_responsiveness"],
          "all five are keys, including the two it cannot see")
    for name in RES.CANNOT_SEE:
        check(found[name]["unmeasured"] is True,
              "%s is unmeasured, always" % name)
        check(len(found[name]["why"]) > 40,
              "and says why in a sentence, not a shrug")
    check("D-48" in found["revit_responsiveness"]["why"],
          "Revit responsiveness cites the rule that makes it invisible")
    check("HERON-KRN-TOK-015" in found["ai_spend"]["why"],
          "and AI spend names the agent that does own the number")

    print()
    print("2. A failed reading is never compared against a threshold")
    answer = RES.should_pause(fixed())
    check(answer["because"] == [],
          "nothing unmeasured produces a 'because'")
    check(len(answer["unmeasured"]) == 5,
          "and all five are listed as unread")
    # A ZERO IS A READING; None IS NOT.
    answer = RES.should_pause(fixed(memory_free=0.0))
    check(answer["pause"] and any("memory_free" in line
                                  for line in answer["because"]),
          "a genuine 0% free memory DOES cross the threshold")
    answer = RES.should_pause(fixed())
    check(not any("memory_free" in line for line in answer["because"]),
          "while an unread one does not, which is the whole difference")

    print()
    print("3. Nothing measurable pauses, and is named")
    blind = RES.should_pause(fixed())
    check(blind["pause"] is True, "with nothing readable, background pauses")
    check(blind["state"] == "NOTHING_MEASURABLE",
          "and the state says blind, not busy")
    check("not permission" in blind["why"],
          "with the reason: 'I could not tell' is not permission")
    seeing = RES.should_pause(fixed(cpu_per_core=0.1))
    check(seeing["state"] is None,
          "one real reading is enough for the state to clear")

    print()
    print("4. Each threshold crossed is named with its number")
    answer = RES.should_pause(fixed(cpu_per_core=RES.BUSY_LOAD_PER_CPU))
    check(answer["pause"] and "cpu_per_core" in answer["because"][0],
          "a load at the threshold pauses - at or over, not over")
    check(str(RES.BUSY_LOAD_PER_CPU) in answer["because"][0],
          "and the threshold itself is quoted")
    answer = RES.should_pause(
        fixed(cpu_per_core=RES.BUSY_LOAD_PER_CPU - 0.01))
    check(not answer["pause"], "just under it does not")
    answer = RES.should_pause(fixed(cpu_per_core=2.0, memory_free=0.01,
                                    disk_free=0.01))
    check(len(answer["because"]) == 3,
          "three readings under pressure give three reasons, not one verdict")

    print()
    print("5. It never answers about a person's work")
    import inspect
    names = set(inspect.signature(RES.should_pause).parameters)
    for forbidden in ("user", "interactive", "foreground", "stop_user"):
        check(forbidden not in names,
              "should_pause() has no '%s' parameter" % forbidden)
    answer = RES.should_pause(fixed(cpu_per_core=99.0))
    check(set(answer) == {"pause", "state", "because", "unmeasured", "why"},
          "and the answer is about pausing background work and nothing else")

    print()
    print("6. It counts no spend of its own")
    for word in ("heron_budget", "cost", "tokens", "dollars"):
        check(word not in source.replace("HERON-KRN-TOK-015", ""),
              "the source does not count '%s' itself" % word)

    print()
    print("7. It feeds the scheduler, and the seam fits")
    import heron_scheduler as SCH
    import heron_queue as QUE
    queue = QUE.Queue()
    queue.add("re-index", 4, "the learning loop", "343 moved")
    # NOBODY IS WORKING, BUT THE MACHINE IS BLIND. The scheduler's `busy`
    # reader is exactly the shape this agent's answer fits into.
    blind_says_pause = RES.should_pause(fixed())["pause"]
    answer = SCH.decide(queue, busy=lambda: blind_says_pause)
    check(blind_says_pause and not answer["run"]
          and answer["refused"] == "A_PERSON_IS_WORKING",
          "a blind resource reading stops the scheduler dispatching")
    quiet = RES.should_pause(fixed(cpu_per_core=0.1, memory_free=0.9,
                                   disk_free=0.9))["pause"]
    answer = SCH.decide(queue, busy=lambda: quiet)
    check(not quiet and answer["run"],
          "and a quiet machine lets the same item through")

    print()
    print("8. Every failure the contract declares is named and reached")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-OPS-RES-003.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    check(blind["state"] in named,
          "and NOTHING_MEASURABLE was reached above, as a state a caller "
          "can branch on")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    unmeasured is not zero, and blind is not quiet")
    return 0


if __name__ == "__main__":
    sys.exit(main())
