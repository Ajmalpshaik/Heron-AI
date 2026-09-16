# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Performance Agent - what it must do, asserted.

    python tests/test_devperf.py

WHAT IT PROVES
  1. THE BOUND IS READ FROM check-gaps.py, NEVER TYPED. Two copies of one
     number is how they drift apart, and this repository has written that
     sentence about prose totals seven times.

  2. THE MARGIN IS THE ANSWER, NOT THE DURATION. 279 seconds means nothing
     on its own; "7% of its bound left" is the finding. Asserted at the
     real numbers that cost this project two five-minute hangs.

  3. A THIN MARGIN IS NAMED, NOT COUNTED. A total tells you something is
     wrong and not what, which is the failure NEEDS-CHECKING.md records
     against its own prose.

  4. A SUITE THAT GOT FASTER IS ALSO REPORTED. A halving may be a fix or
     may be work silently skipped, and this agent does not claim to know
     which - reporting only slowdowns would have called the 279s to 5.5s
     fix and a suite that stopped asserting by the same name: nothing.

  5. A DIFFERENCE INSIDE THE NOISE FLOOR IS NOT A MOVE. Measured the same
     day this was written: a 9,506-pair run finished FASTER than a 756-pair
     one, because both were dominated by start-up. A tool that reported
     that as an improvement would be worse than no tool.

  6. IT NEVER FAILS THE BUILD ON TIME. Timing varies with the machine, and
     A18's whole lesson is what a report that cries wolf does to the person
     reading it. Exit 0 even when everything is at risk.

  7. EVERY FAILURE STATE THE MODULE DECLARES IS EXERCISED - a failure
     nothing has ever triggered is a failure nobody has seen happen. The
     declaration used to be read from the contract; there is no contract now,
     so it is read from the module and checked against the module's own source.

  8. `--suites` STOPS AT THE NEXT FLAG. Added after running the agent for
     real: `--suites a.py --out x.json` swallowed `--out` and its path as
     two more suite names, and because a missing file is recorded as
     FAILED rather than refused, they were REPORTED - two rows sitting in
     a table of real measurements looking exactly like real measurements.
     Every claim above called summarise() directly, so none of them went
     through argv and none of them could have caught it.
"""

import io
import json
import os
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_devperf as PERF

CHECKS = [0]
FAILURES = []


def check(condition, what):
    CHECKS[0] += 1
    if not condition:
        FAILURES.append(what)


def test_bound_is_read_not_typed():
    """1. The bound comes out of check-gaps.py."""
    bound = PERF.bound_seconds()
    check(bound == 300,
          "the bound should be check-gaps.py's 300, got %r" % (bound,))

    # And it is genuinely READ: the number is in that file and this one
    # asserts they agree, so moving it there and not here fails this test
    # rather than silently reporting against a stale limit.
    source = io.open(os.path.join(ROOT, "tools", "check-gaps.py"),
                     encoding="utf-8").read()
    check("SUITE_TIMEOUT = %d" % bound in source,
          "check-gaps.py no longer declares SUITE_TIMEOUT = %d" % bound)


def test_margin_is_the_answer():
    """2. Margin, not duration - at the numbers that actually cost time."""
    row = PERF.measurement("test_agents.py", 279.3, True, 300)
    check(abs(row["marginPercent"] - 6.9) < 0.2,
          "279.3s of a 300s bound is ~6.9%% margin, got %r"
          % (row["marginPercent"],))
    check(row["atRisk"] is True,
          "a suite with 7% of its bound left must be at risk")

    fixed = PERF.measurement("test_agents.py", 5.5, True, 300)
    check(fixed["atRisk"] is False,
          "5.5s of a 300s bound is not at risk")

    # A suite that FAILED is not at risk of timing out - it already has an
    # answer, and calling it at-risk would bury a real failure in a
    # performance report.
    failed = PERF.measurement("test_x.py", 290.0, False, 300)
    check(failed["atRisk"] is False,
          "a suite that failed is reported as failed, not as at-risk")


def test_at_risk_is_named_never_counted():
    """3. Named individually."""
    rows = [PERF.measurement("test_slow.py", 280.0, True, 300),
            PERF.measurement("test_fast.py", 1.0, True, 300)]
    report = PERF.summarise(rows, baseline=None, bound=300)
    check(report["atRisk"] == ["test_slow.py"],
          "at-risk must be the NAMES, got %r" % (report["atRisk"],))


def test_faster_is_reported_too():
    """4. A halving is a finding."""
    before = {"measurements": [{"suite": "test_agents.py", "seconds": 279.3}]}
    now = [PERF.measurement("test_agents.py", 5.5, True, 300)]
    report = PERF.summarise(now, baseline=before, bound=300)
    moved = {m["suite"]: m for m in report["moved"]}
    check("test_agents.py" in moved,
          "a suite that went 279.3s -> 5.5s must be reported as moved")
    check(moved["test_agents.py"]["direction"] == "faster",
          "and named as faster, got %r"
          % (moved.get("test_agents.py", {}).get("direction"),))


def test_noise_is_not_a_move():
    """5. Inside the noise floor is not a finding."""
    before = {"measurements": [{"suite": "test_x.py", "seconds": 2.21}]}
    now = [PERF.measurement("test_x.py", 1.52, True, 300)]
    report = PERF.summarise(now, baseline=before, bound=300)
    check(report["moved"] == [],
          "a sub-noise-floor difference is not a move, got %r"
          % (report["moved"],))

    # The floor is absolute AND relative, because neither alone works: a
    # fixed 1s floor calls every fast suite unchanged forever, and a pure
    # percentage calls 0.01s -> 0.02s a doubling.
    check(PERF.NOISE_SECONDS > 0 and PERF.NOISE_FRACTION > 0,
          "both a seconds floor and a fraction are needed")


def test_never_fails_the_build():
    """6. It reports; it does not gate."""
    rows = [PERF.measurement("test_slow.py", 299.0, True, 300)]
    report = PERF.summarise(rows, baseline=None, bound=300)
    check(report["atRisk"] == ["test_slow.py"], "the risk is still reported")
    check(PERF.exit_code(report) == 0,
          "a performance report must never fail the build")


def test_every_declared_failure_is_exercised():
    """7. NO_SUITES_FOUND, BASELINE_UNREADABLE, BOUND_NOT_FOUND."""
    declared = list(PERF.FAILURES)
    check(sorted(declared) == ["BASELINE_UNREADABLE", "BOUND_NOT_FOUND",
                               "NO_SUITES_FOUND"],
          "the declared failures moved: %r" % (declared,))

    # And every one is genuinely PRODUCED, not merely listed. This is what
    # reading the contract used to buy: rename a produce site and the list
    # goes on naming the old one, with nothing anywhere to notice.
    module = io.open(os.path.join(ROOT, "brain", "heron_devperf.py"),
                     encoding="utf-8").read()
    for name in declared:
        check(module.count(name) >= 2,
              "%s is declared and never produced" % name)

    # NO_SUITES_FOUND - an empty sweep must never read as success. A tool
    # that finds nothing and says "all clear" is the same defect as a grep
    # whose pattern cannot see what is there.
    empty = tempfile.mkdtemp()
    failure = PERF.find_suites(empty)
    check(failure == [], "an empty folder yields no suites")
    report = PERF.summarise([], baseline=None, bound=300)
    check(report["failure"] == "NO_SUITES_FOUND",
          "no suites must be a declared failure, got %r"
          % (report.get("failure"),))

    # BASELINE_UNREADABLE - a baseline that cannot be parsed must be said
    # out loud, never treated as "nothing to compare against", which would
    # silently turn a comparison run into a description run.
    broken = os.path.join(empty, "broken.json")
    io.open(broken, "w", encoding="utf-8").write(u"{not json")
    loaded, problem = PERF.load_baseline(broken)
    check(loaded is None and problem == "BASELINE_UNREADABLE",
          "a broken baseline is BASELINE_UNREADABLE, got %r" % (problem,))

    missing, problem = PERF.load_baseline(os.path.join(empty, "nope.json"))
    check(missing is None and problem == "BASELINE_UNREADABLE",
          "a missing baseline is BASELINE_UNREADABLE, got %r" % (problem,))

    # BOUND_NOT_FOUND - if check-gaps.py stops declaring the bound, this
    # agent must refuse rather than fall back to a number of its own. A
    # invented default is exactly the second copy the whole design avoids.
    check(PERF.bound_seconds(os.path.join(empty, "nope.py")) is None,
          "a missing check-gaps.py yields None, not a guess")


def test_suites_stops_at_the_next_flag():
    """8. The bug running it for real found, that no unit test could."""
    import subprocess
    out = os.path.join(tempfile.mkdtemp(), "x.json")
    proc = subprocess.run(
        [sys.executable, os.path.join(ROOT, "brain", "heron_devperf.py"),
         # NOT test_devperf.py. Measuring the suite that is doing the
         # measuring is an infinite regress - this test runs the agent,
         # which runs this test, which runs the agent. It recursed until
         # the 300s bound killed it, which reads exactly like a hang.
         "--suites", "test_contract.py", "--out", out],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT, cwd=ROOT,
        timeout=300)
    text = proc.stdout.decode("utf-8", "replace")

    check("--out" not in text.split("suites:")[0],
          "`--out` was measured as a suite: %s" % text[:400])
    check("suites: 1" in text,
          "one suite was asked for, so one must be measured: %s" % text[:400])
    check(os.path.isfile(out),
          "--out still has to be honoured after the flag stops the list")

    # And a name that is not a suite is REFUSED rather than recorded as a
    # failure, because a typo that becomes a row of data is a typo nobody
    # notices.
    bad = subprocess.run(
        [sys.executable, os.path.join(ROOT, "brain", "heron_devperf.py"),
         "--suites", "test_this_does_not_exist.py"],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT, cwd=ROOT,
        timeout=300)
    check(bad.returncode == 2,
          "an unknown suite name is refused, got exit %d" % bad.returncode)
    check(b"Not a suite" in bad.stdout,
          "and it says which one")


def main():
    for test in (test_bound_is_read_not_typed,
                 test_margin_is_the_answer,
                 test_at_risk_is_named_never_counted,
                 test_faster_is_reported_too,
                 test_noise_is_not_a_move,
                 test_never_fails_the_build,
                 test_every_declared_failure_is_exercised,
                 test_suites_stops_at_the_next_flag):
        test()

    if FAILURES:
        print("FAIL  tests/test_devperf.py  %d of %d"
              % (len(FAILURES), CHECKS[0]))
        for failure in FAILURES:
            print("        %s" % failure)
        return 1

    print("PASS  tests/test_devperf.py  %d checks" % CHECKS[0])
    return 0


if __name__ == "__main__":
    sys.exit(main())
