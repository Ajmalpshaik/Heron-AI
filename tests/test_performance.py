# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-SKL-PRF-006
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Skill performance - nothing is aggregated across versions, and a release
nobody ran is not a release that works.

    python tests/test_performance.py

WHAT IT PROVES
  1. THE RELEASE LIST IS HERON-FRG-VAL-001'S BY IDENTITY, and the
     module's code writes no release of its own. D-05 does not
     extrapolate and this file may not quietly add 2028.

  2. NOTHING IS AGGREGATED ACROSS VERSIONS. The case the row exists for:
     a skill that works in one release and is broken in another. The
     answer carries no single number across releases, and measuring each
     release ALONE gives back the identical row.

  3. A DECLARED RELEASE WITH NO RUNS IS `untested`, AND IS NOT IN
     `versions`. Untested is not passing, and it is kept apart from the
     releases that failed.

  4. `never_succeeded` IS A UNIVERSAL, NOT A CUTOFF - one success in a
     hundred runs is not marked, because marking it needs a number and
     every number is invented.

  5. EVERY RATE CAME BACK WITH THE TWO COUNTS THAT MADE IT, and 1-of-1
     does not read the same as 47-of-47.

  6. NOTHING IS STORED.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_performance as PRF                                # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_performance.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    print("\n1. the release list is HERON-FRG-VAL-001's, by identity")
    check(PRF.VERSIONS is FRAG.REVIT_VERSIONS,
          "VERSIONS is FRAG.REVIT_VERSIONS - the same object, not an "
          "equal copy")
    check(PRF.VERSIONS == ("2020", "2021", "2022", "2023", "2024", "2025",
                           "2026", "2027"),
          "and it is D-05's eight releases")
    written = [one for one in PRF.VERSIONS if '"%s"' % one in logic]
    check(not written,
          "the module's code writes none of them%s"
          % ("" if not written else ": %s" % ", ".join(written)))
    # D-05 NEVER EXTRAPOLATES. 2028 is refused because it is not on the
    # list, not because anybody knows anything about 2028.
    future = PRF.measure([{"revit": "2024", "outcome": "ok"}],
                         declared=["2028"])
    reached.add(future.get("refused"))
    check(future["refused"] == "NOT_A_VERSION",
          "so a release off the end of the list is refused, not guessed")

    print("\n2. nothing is aggregated across versions")
    # THE CASE THE ROW EXISTS FOR: good in one release, broken in another.
    # Every aggregate this skill has looks fine.
    split = PRF.measure(
        [{"revit": "2024", "outcome": "ok"}] * 9
        + [{"revit": "2021", "outcome": "failed"}])
    by = dict((one["revit"], one) for one in split["versions"])
    check(by["2024"]["failure_rate"] == 0.0
          and by["2021"]["failure_rate"] == 1.0,
          "2024 reads 0.00 and 2021 reads 1.00 - the broken release is "
          "visible")
    # 1 of 10 failed. An overall rate would read 0.10 and hide it.
    flat = [key for key, value in split.items()
            if isinstance(value, float)]
    check(not flat,
          "and the answer carries no single number across releases%s"
          % ("" if not flat else ": %s" % ", ".join(flat)))
    # AND NO RELEASE INFLUENCED ANOTHER. Measuring each one ALONE must
    # give back the identical row - a word search would only find the
    # word "average", which the prose above uses to refuse it.
    for one in split["versions"]:
        alone = PRF.measure([run for run in
                             [{"revit": "2024", "outcome": "ok"}] * 9
                             + [{"revit": "2021", "outcome": "failed"}]
                             if run["revit"] == one["revit"]])
        check(alone["versions"] == [one],
              "%s measured alone gives the identical row - no release "
              "moved another" % one["revit"])

    print("\n3. a declared release with no runs is untested, not passing")
    thin = PRF.measure([{"revit": "2024", "outcome": "ok"}],
                       declared=list(FRAG.REVIT_VERSIONS))
    check(thin["untested"] == ["2020", "2021", "2022", "2023", "2025",
                               "2026", "2027"],
          "seven declared releases have no runs at all")
    ran = [one["revit"] for one in thin["versions"]]
    check(ran == ["2024"],
          "and they are NOT in `versions` - nothing was reported about a "
          "release nobody ran")
    check(not set(thin["untested"]) & set(thin["never_succeeded"]),
          "untested is kept apart from never_succeeded: 'we tried and it "
          "broke' and 'nobody tried' are different facts")
    check(any("UNTESTED, not passing" in line for line in thin["unjudged"]),
          "and the answer says so in its own words")

    print("\n4. never_succeeded is a universal, not a cutoff")
    hundred = PRF.measure([{"revit": "2024", "outcome": "failed"}] * 99
                          + [{"revit": "2024", "outcome": "ok"}])
    check(hundred["versions"][0]["failure_rate"] == 0.99,
          "99 failures in 100 runs reads 0.99")
    check(hundred["never_succeeded"] == [],
          "and it is STILL not marked - one success is a success, and "
          "marking 0.99 would need a cutoff nobody has written down")
    none_at_all = PRF.measure([{"revit": "2021", "outcome": "failed"}])
    check(none_at_all["never_succeeded"] == ["2021"],
          "zero successes IS marked - that is a universal, so it needs "
          "no number")
    check(none_at_all["versions"][0]["succeeded"] == 0,
          "and the count that makes it is right there")

    print("\n5. every rate came back with its counts")
    for one in split["versions"] + hundred["versions"]:
        check("runs" in one and "failed" in one and "failure_rate" in one,
              "%s carries the rate AND the counts (%d failed of %d)"
              % (one["revit"], one["failed"], one["runs"]))
    thin_one = PRF.measure([{"revit": "2024", "outcome": "failed"}])
    big_one = PRF.measure([{"revit": "2024", "outcome": "failed"}] * 47)
    check(thin_one["versions"][0]["failure_rate"]
          == big_one["versions"][0]["failure_rate"] == 1.0,
          "1-of-1 and 47-of-47 give the same rate")
    check(thin_one["versions"][0]["why"] != big_one["versions"][0]["why"],
          "but NOT the same sentence - a bare rate would make them "
          "identical")

    print("\n6. nothing is stored")
    check(thin["measured"] is True, "runs are read")
    for writing in ("open(", "write(", "makedirs", "rmtree"):
        check(writing not in logic, "the agent never uses %s" % writing)
    check(len(thin["unjudged"]) == 4, "four things are left unjudged")

    print("\n7. every declared failure is named and reached")
    for runs, declared, name in (
            ([], None, "NOTHING_TO_MEASURE"),
            (["not a dict"], None, "NOT_A_RUN"),
            ([{"revit": "1999", "outcome": "ok"}], None, "NOT_A_VERSION"),
            ([{"revit": "2024", "outcome": "maybe"}], None,
             "NOT_AN_OUTCOME")):
        answer = PRF.measure(runs, declared=declared)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-SKL-PRF-006.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 4, "the contract declares 4 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    per Revit version, and untested is not passing")
    return 0


if __name__ == "__main__":
    sys.exit(main())
