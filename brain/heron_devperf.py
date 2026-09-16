# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Performance Agent - how close a suite is to its bound, not how long it took.

    python brain/heron_devperf.py                       time every suite
    python brain/heron_devperf.py --out before.json     and record it
    python brain/heron_devperf.py --baseline before.json
    python brain/heron_devperf.py --suites test_agents.py test_docs.py

Always exits 0. It reports; it does not gate - see WHY IT NEVER FAILS below.

It has NO contract file, and that absence is the withdrawal. A contract in
brain/agents/ is itself the claim on an id, so blanking these headers while
leaving the contract in place withdrew nothing - which is exactly what the
first attempt did, and what tests/test_contract_reference.py caught: 126
contracts standing for 215 agents, and one of them with no suite to its name.

WHY THE HEADER SAYS `none`, AND IT IS A DECISION RATHER THAN AN OMISSION
------------------------------------------------------------------------
This file was written on 2026-09-16 claiming HERON-DEV-PRF-015, and the claim
was withdrawn the same day on reading PROPOSALS F27, which names that exact row
and says what to do about it: **do not claim it on a guess.**

F27's question is not about this file. It is that nobody has said whether a
Development agent acts on THE ARTEFACT HERON IS BUILDING or on HERON ITSELF -
and the two Development rows already claimed point opposite ways. `DEV-RVT-013`
is `tools/batch-prove.py`, which proves FRAGMENTS; `DEV-REL-018` is
`tools/check-package.py`, which packages HERON. Under the first reading
`DEV-PRF-015` measures a fragment's cost; under the second it measures this
repository's own suites, which is what this file does.

So there are now THREE candidates for one row - `tools/measure-brain.py`
(the brain's stages), this file (the test suites), and whatever would measure a
fragment - and the register has one id.

**The tool is not in doubt; the label is.** Everything below works and is
tested. What is withheld is the claim, exactly as `tools/measure-brain.py`
withholds it for the same row and the same reason.

HOW THE CLAIM CAME TO BE MADE, because the mistake is more useful than the fix.
Before building, every candidate agent was checked against PROPOSALS and
OPEN-QUESTIONS for a recorded blocker, and PRF-015 came back clean. The grep
searched for `HERON-DEV-PRF-015`. **F27 writes it `DEV-PRF-015`, with no
prefix** - so the pattern could not see the one row that was about it. The same
grep wrongly cleared nine other agents. That is this repository's own rule,
broken by the check written to enforce it: **prove the pattern can see what you
know is there.**

WHY IT EXISTS
-------------
`tests/test_agents.py` ran for weeks at **279.3 seconds against check-gaps'
300-second bound** - seven per cent of margin. It HUNG on two of three runs,
each hang costing five minutes and reporting a failure that did not exist, and
the first of those was written into HANDOVER.md as an undiagnosed defect in a
suite that had nothing wrong with it. The cause turned out to be one omitted
argument, and the fix took it to 5.5 seconds.

**Nothing in this repository could have found it.** `tools/change-evidence.py`
captures before and after honestly and captures exit codes and the SET of
failing suites - so a suite creeping from 4 seconds to 279 passes every check
it has, right up to the run where it does not. `check-gaps.py` enforces the
bound and reports only the verdict, so a suite at 99% of its limit and one at
1% are the same word.

A suite that passes with seven per cent of margin will fail at random forever,
and every time it does somebody will chase a defect that is not there.

THE MARGIN IS THE ANSWER, NOT THE DURATION
-------------------------------------------
"279 seconds" is unreadable without the bound beside it. "7% of its bound left"
needs nothing else, and it is the same sentence whether the bound moves or the
machine does.

WHY IT NEVER FAILS THE BUILD
-----------------------------
Timing varies with the machine, with what else is running, and with a cold
cache. A gate that fails on it fails at random, and `A18`'s whole lesson is
what a report that cries wolf does to the person reading it. So this exits 0
even when every suite is at risk. The number is for a person to act on.

WHAT IT DELIBERATELY DOES NOT CLAIM
------------------------------------
That a suite which got FASTER got better. A halving is reported exactly like a
doubling, because the two are indistinguishable from out here: the 279 to 5.5
fix and a suite that quietly stopped asserting anything both look like this.
Naming it and refusing to judge it is the honest half.
"""

import io
import json
import os
import re
import subprocess
import sys
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TESTS = os.path.join(ROOT, "tests")
CHECK_GAPS = os.path.join(ROOT, "tools", "check-gaps.py")

# AT RISK below this much of the bound left. Chosen at 25% rather than at the
# 7% that actually bit, because 7% is where it is already failing - a warning
# that only fires once the failures have started is a description, not a
# warning.
AT_RISK_FRACTION = 0.25

# THE NOISE FLOOR, BOTH HALVES, AND NEITHER ALONE WORKS. A fixed floor calls
# every fast suite unchanged forever; a pure percentage calls 0.01s -> 0.02s a
# doubling. Measured the day this was written: a 9,506-pair Revit run finished
# FASTER than a 756-pair one, because both were start-up and one round trip.
# A tool that reported that as an improvement would be worse than no tool.
NOISE_SECONDS = 0.75
NOISE_FRACTION = 0.20

# THE FAILURES THIS AGENT CAN REPORT, DECLARED ONCE. A built agent declares
# these in its contract and the test reads them from there; this one has no
# contract until F27 is settled, and a test that types them itself stops
# noticing when a produce site is renamed. So they are declared here, beside
# the code that produces them, and the test asserts each is genuinely produced
# rather than only listed.
FAILURES = ("BASELINE_UNREADABLE", "BOUND_NOT_FOUND", "NO_SUITES_FOUND")


def bound_seconds(path=None):
    """
    The bound a suite is measured against, READ from check-gaps.py.

    NOT TYPED HERE, and not defaulted either. check-gaps.py is the file that
    actually kills a suite at this number; a second copy of it in this file
    would be two numbers that agree until the day somebody moves one. When it
    cannot be read this returns None and the caller reports BOUND_NOT_FOUND -
    inventing a fallback is exactly the second copy the design avoids.
    """
    source_path = CHECK_GAPS if path is None else path
    try:
        with io.open(source_path, encoding="utf-8") as handle:
            source = handle.read()
    except (IOError, OSError):
        return None

    found = re.search(r"^SUITE_TIMEOUT\s*=\s*(\d+)", source, re.M)
    return int(found.group(1)) if found else None


def find_suites(folder=None):
    """Every tests/test_*.py, sorted so two runs list them the same way."""
    where = TESTS if folder is None else folder
    try:
        names = os.listdir(where)
    except (IOError, OSError):
        return []
    return sorted(n for n in names
                  if n.startswith("test_") and n.endswith(".py"))


def measurement(suite, seconds, passed, bound):
    """
    One suite, described the way a reader can act on.

    `marginPercent` is how much of the bound went UNUSED. `atRisk` is the
    judgement, and a suite that FAILED is never at risk: it already has an
    answer, and burying a real failure inside a performance warning is how a
    report stops being read.
    """
    left = bound - seconds
    margin = (left / float(bound)) * 100.0 if bound else 0.0
    return {
        "suite": suite,
        "seconds": round(seconds, 2),
        "passed": bool(passed),
        "marginPercent": round(margin, 1),
        "atRisk": bool(passed) and margin < (AT_RISK_FRACTION * 100.0),
    }


def load_baseline(path):
    """
    A recording made earlier, or (None, "BASELINE_UNREADABLE").

    A baseline that cannot be read is SAID, never shrugged off. Treating it as
    "nothing to compare against" turns a comparison run into a description run
    with no sign that it happened, which is the quietest way to lose a check.
    """
    try:
        with io.open(path, encoding="utf-8") as handle:
            data = json.loads(handle.read())
    except (IOError, OSError, ValueError):
        return None, "BASELINE_UNREADABLE"
    if not isinstance(data, dict) or "measurements" not in data:
        return None, "BASELINE_UNREADABLE"
    return data, None


def _moved(before_seconds, now_seconds):
    """Whether a difference is real, and which way. None when it is noise."""
    difference = now_seconds - before_seconds
    if abs(difference) < NOISE_SECONDS:
        return None
    floor = max(before_seconds, now_seconds, 0.0001)
    if abs(difference) / floor < NOISE_FRACTION:
        return None
    return "slower" if difference > 0 else "faster"


def summarise(rows, baseline, bound):
    """The whole answer: the rows, who is at risk, and what moved."""
    report = {
        "bound": bound,
        "measurements": rows,
        "atRisk": [r["suite"] for r in rows if r["atRisk"]],
        "moved": [],
        "failure": None,
    }

    if not rows:
        # AN EMPTY SWEEP MUST NEVER READ AS SUCCESS. A tool that finds
        # nothing and says "all clear" is the same defect as a grep whose
        # pattern cannot see what is there.
        report["failure"] = "NO_SUITES_FOUND"
        return report

    if not baseline:
        return report

    was = {}
    for entry in baseline.get("measurements", []):
        if isinstance(entry, dict) and "suite" in entry:
            was[entry["suite"]] = entry.get("seconds")

    for row in rows:
        before = was.get(row["suite"])
        if before is None:
            continue
        direction = _moved(float(before), float(row["seconds"]))
        if direction is None:
            continue
        report["moved"].append({
            "suite": row["suite"],
            "was": round(float(before), 2),
            "now": row["seconds"],
            "direction": direction,
        })
    return report


def exit_code(report):
    """Always 0. See WHY IT NEVER FAILS THE BUILD in the module docstring."""
    return 0


def run_suite(name, bound):
    """Time one suite. A timeout is recorded at the bound, never as zero."""
    start = time.time()
    try:
        proc = subprocess.run(
            [sys.executable, os.path.join(TESTS, name)],
            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
            cwd=ROOT, timeout=bound)
        passed = proc.returncode == 0
        seconds = time.time() - start
    except subprocess.TimeoutExpired:
        # It reached the bound. Recording the real elapsed time would say
        # "300.4s" and read like a measurement; it is a censored one, and the
        # `passed: False` beside it is what makes that legible.
        passed = False
        seconds = float(bound)
    except (IOError, OSError):
        passed = False
        seconds = time.time() - start
    return measurement(name, seconds, passed, bound)


def render(report, out=None):
    """The report, for a person."""
    write = (out or sys.stdout).write
    write("PERFORMANCE   how close each suite is to its bound\n")
    write("=" * 62 + "\n\n")

    if report["failure"]:
        write("  %s\n" % report["failure"])
        write("\n  Nothing was measured, and that is reported rather than\n")
        write("  returned as an empty pass.\n")
        return

    write("  bound: %ds, read from tools/check-gaps.py\n\n" % report["bound"])

    for row in sorted(report["measurements"], key=lambda r: r["marginPercent"]):
        mark = "AT RISK" if row["atRisk"] else ("FAILED" if not row["passed"]
                                                else "       ")
        write("  %-8s %7.2fs  %5.1f%% margin  %s\n"
              % (mark, row["seconds"], row["marginPercent"], row["suite"]))

    write("\n  suites: %d\n" % len(report["measurements"]))

    if report["atRisk"]:
        write("\n  AT RISK - under %d%% of the bound left. These will fail at\n"
              % int(AT_RISK_FRACTION * 100))
        write("  random on a loaded machine, and each time somebody will\n")
        write("  chase a defect that is not there:\n")
        for name in report["atRisk"]:
            write("    %s\n" % name)

    if report["moved"]:
        write("\n  MOVED since the baseline. Faster is reported too: a\n")
        write("  halving may be a fix or may be work silently skipped, and\n")
        write("  this cannot tell the difference:\n")
        for move in report["moved"]:
            write("    %-8s %7.2fs -> %7.2fs   %s\n"
                  % (move["direction"], move["was"], move["now"],
                     move["suite"]))

    write("\n  This never fails the build. Timing varies with the machine,\n")
    write("  and a gate that fails at random teaches people to ignore it.\n")


def main(argv):
    suites, baseline_path, out_path = None, None, None
    rest = argv[1:]
    index = 0
    while index < len(rest):
        flag = rest[index]
        if flag == "--baseline" and index + 1 < len(rest):
            baseline_path = rest[index + 1]
            index += 2
        elif flag == "--out" and index + 1 < len(rest):
            out_path = rest[index + 1]
            index += 2
        elif flag == "--suites":
            # UP TO THE NEXT FLAG, NOT TO THE END. Taking the rest of the
            # line swallowed `--out` and its path as two more suite names,
            # and because a missing file is recorded as FAILED rather than
            # refused, the run REPORTED them - two rows named `--out` and a
            # temp path, sitting in a table of real measurements looking
            # exactly like real measurements. Found by running it, which is
            # the only thing that could have found it: every unit test
            # called summarise() directly and never went through argv.
            names = []
            index += 1
            while index < len(rest) and not rest[index].startswith("--"):
                names.append(rest[index])
                index += 1
            suites = names
        else:
            print("unknown option '%s'" % flag)
            return 2

    bound = bound_seconds()
    if bound is None:
        print("BOUND_NOT_FOUND")
        print("tools/check-gaps.py no longer declares SUITE_TIMEOUT, and this")
        print("agent will not invent a limit of its own - a second copy of")
        print("that number is how two files come to disagree silently.")
        return 0

    baseline = None
    if baseline_path is not None:
        baseline, problem = load_baseline(baseline_path)
        if problem:
            print("BASELINE_UNREADABLE  %s" % baseline_path)
            print("A baseline that cannot be read is said out loud rather")
            print("than treated as nothing to compare against.")
            return 0

    names = suites if suites else find_suites()

    # A NAME THAT IS NOT A SUITE IS REFUSED, NOT MEASURED. run_suite records
    # a missing file as FAILED in 0.1s, which is indistinguishable in the
    # rendered table from a suite that really did fail - so a typo became a
    # row of data rather than a question.
    unknown = [n for n in names
               if not os.path.isfile(os.path.join(TESTS, n))]
    if unknown:
        print("Not a suite in tests/: %s" % ", ".join(unknown))
        print("Nothing was measured. A name that is not a suite would be")
        print("recorded as FAILED and read like one.")
        return 2

    rows = [run_suite(name, bound) for name in names]
    report = summarise(rows, baseline, bound)
    render(report)

    if out_path:
        with io.open(out_path, "w", encoding="utf-8") as handle:
            handle.write(json.dumps(report, indent=2, sort_keys=True))
        print("\n  written  %s" % out_path)

    return exit_code(report)


if __name__ == "__main__":
    sys.exit(main(sys.argv))
