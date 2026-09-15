# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-SKL-PRF-006
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Skill performance - per Revit version, and a version nobody ran is not a
version that works.

    python brain/heron_performance.py

WHAT IT IS FOR (docs/28, HERON-SKL-PRF-006)
--------------------------------------------
"Success, user corrections, execution time, failure rate - PER REVIT
VERSION. Poor performers enter review." T1, risk READ.

"PER REVIT VERSION" IS THE WHOLE ROW
--------------------------------------
A skill that works in 2024 and fails in 2021 has a good overall figure
and a broken release. Aggregating is what hides it, and the row says so
by putting the qualifier in bold-case in the middle of a list of
aggregates. So nothing here is reported across versions - not one
number, not one rate, not an average.

THE ROW ASKS FOR A RATE, AND A RATE IS ONLY SAFE BESIDE ITS COUNTS
--------------------------------------------------------------------
D-39 refuses an agreement rate and HERON-LRN-OBS-001 refuses a success
rate, both because a rate is a number you can watch improve while
nothing gets better. The row asks for one, so it is given - PER VERSION,
never aggregated, and never without the two counts that made it. A
reader seeing "1.00 failure, 1 of 1 run" treats it differently from
"1.00 failure, 47 of 47", and a bare rate makes them identical.

A VERSION NOBODY RAN IS NOT A VERSION THAT WORKS
--------------------------------------------------
This is the finding a per-version agent exists to surface. A skill
declaring 2020 to 2027 and run only on 2024 has SEVEN releases nobody
has tested, and every aggregate it has looks excellent. Those versions
are reported as UNTESTED - separately from the ones that failed, because
"we tried and it broke" and "nobody tried" are different facts and only
one of them is evidence.

"POOR PERFORMERS" WITHOUT A THRESHOLD
---------------------------------------
Deciding what counts as poor needs a number and every number is
invented - the same wall HERON-NAM-TAX-004 refused to build. So one
thing is marked, and it is a universal rather than a cutoff: a version
where the skill has NEVER SUCCEEDED. Everything else is reported with
its counts and left to a person.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# D-05's list, read from HERON-FRG-VAL-001 rather than retyped. An unlisted
# release is an error, never a guess.
VERSIONS = FRAG.REVIT_VERSIONS

FAILED = "failed"
OK = "ok"


def measure(runs, declared=None):
    """
    {versions, untested, never_succeeded, why} - or a refusal.

    Nothing is aggregated across versions and nothing is stored.
    """
    if not runs:
        return {"measured": False, "refused": "NOTHING_TO_MEASURE",
                "why": "no runs were handed in. A skill with no recorded "
                       "runs has no performance, and reporting one would be "
                       "a statement about the records."}

    wanted = [str(each).strip() for each in (declared or [])]
    strangers = sorted(set(one for one in wanted if one not in VERSIONS))
    if strangers:
        return {"measured": False, "refused": "NOT_A_VERSION",
                "why": "%s is not a release this project supports: %s. D-05 "
                       "does not extrapolate, and a skill declared for a "
                       "release nobody has tested is a claim rather than a "
                       "capability."
                       % (", ".join("'%s'" % each for each in strangers),
                          ", ".join(VERSIONS))}

    book = {}
    for run in runs:
        if not isinstance(run, dict):
            return {"measured": False, "refused": "NOT_A_RUN",
                    "why": "%r is not a run. Each is {revit, outcome} and "
                           "may carry `corrections` and `seconds`."
                           % (run,)}
        release = str(run.get("revit") or "").strip()
        outcome = str(run.get("outcome") or "").strip().lower()
        if release not in VERSIONS:
            return {"measured": False, "refused": "NOT_A_VERSION",
                    "why": "a run names Revit %r. Known: %s - and a run "
                           "whose release is unknown cannot be filed under "
                           "any version, which is the only thing this agent "
                           "reports by."
                           % (run.get("revit"), ", ".join(VERSIONS))}
        if outcome not in (OK, FAILED):
            return {"measured": False, "refused": "NOT_AN_OUTCOME",
                    "why": "'%s' is neither '%s' nor '%s'." % (outcome, OK,
                                                               FAILED)}

        entry = book.setdefault(release, {"runs": 0, "failed": 0,
                                          "corrected": 0, "seconds": []})
        entry["runs"] += 1
        if outcome == FAILED:
            entry["failed"] += 1
        if run.get("corrections"):
            entry["corrected"] += 1
        if isinstance(run.get("seconds"), (int, float)):
            entry["seconds"].append(run["seconds"])

    versions = []
    for release in sorted(book):
        entry = book[release]
        times = sorted(entry["seconds"])
        versions.append({
            "revit": release, "runs": entry["runs"],
            "failed": entry["failed"],
            "succeeded": entry["runs"] - entry["failed"],
            "corrected": entry["corrected"],
            # A RATE, AND NEVER WITHOUT THE COUNTS THAT MADE IT.
            "failure_rate": round(entry["failed"] / float(entry["runs"]), 2),
            "middle_seconds": times[len(times) // 2] if times else None,
            "why": "%d run(s) on %s: %d failed, %d corrected. The rate is "
                   "%.2f and it means what %d run(s) make it mean."
                   % (entry["runs"], release, entry["failed"],
                      entry["corrected"],
                      entry["failed"] / float(entry["runs"]), entry["runs"])})

    never = [one["revit"] for one in versions if one["succeeded"] == 0]
    untested = [release for release in wanted if release not in book]

    return {
        "measured": True, "versions": versions,
        "never_succeeded": never, "untested": untested,
        "declared": wanted, "of": len(runs),
        "why": "%d run(s) across %d release(s)%s. Nothing was aggregated "
               "across versions."
               % (len(runs), len(versions),
                  ", %d declared release(s) never run" % len(untested)
                  if untested else ""),
        "unjudged": [
            "NOTHING WAS AGGREGATED ACROSS VERSIONS, AND THAT IS THE ROW'S "
            "OWN QUALIFIER. A skill that works in one release and fails in "
            "another has a good overall figure and a broken release, and "
            "aggregating is what hides it.",
            "%s" % ("%d DECLARED RELEASE(S) HAVE NO RUNS AT ALL: %s. Those "
                    "are UNTESTED, not passing - 'we tried and it broke' "
                    "and 'nobody tried' are different facts and only one of "
                    "them is evidence."
                    % (len(untested), ", ".join(untested)) if untested else
                    "every declared release has at least one run."),
            "%s" % ("THE SKILL HAS NEVER SUCCEEDED ON %s. That is a "
                    "universal rather than a cutoff, which is why it can be "
                    "marked at all - deciding what counts as POOR needs a "
                    "number and every number is invented."
                    % ", ".join(never) if never else
                    "every release with runs has at least one success, so "
                    "nothing is marked. No threshold was applied to the "
                    "rest."),
            "EVERY RATE HERE CAME BACK WITH ITS COUNTS. D-39 refuses an "
            "agreement rate and HERON-LRN-OBS-001 refuses a success rate, "
            "both because a rate is a number you can watch improve while "
            "nothing gets better. The row asks for one, so it is given "
            "beside the two numbers that made it.",
        ],
    }


def main(argv):
    print("SKILL PERFORMANCE   per Revit version, and untested is not passing")
    print("=" * 72)

    answer = measure(
        [{"revit": "2024", "outcome": "ok", "seconds": 2.1},
         {"revit": "2024", "outcome": "ok", "seconds": 2.4},
         {"revit": "2024", "outcome": "ok", "seconds": 9.0,
          "corrections": ["user re-ran it"]},
         {"revit": "2021", "outcome": "failed", "seconds": 0.3},
         {"revit": "2021", "outcome": "failed", "seconds": 0.3},
         {"revit": "2025", "outcome": "ok", "seconds": 2.2},
         {"revit": "2025", "outcome": "failed", "seconds": 0.4}],
        declared=["2020", "2021", "2022", "2023", "2024", "2025", "2026",
                  "2027"])

    print("\n%s" % answer["why"])
    for one in answer["versions"]:
        print("  %-6s %2d run(s)  %d failed  %d corrected  rate %.2f  "
              "middle %ss"
              % (one["revit"], one["runs"], one["failed"], one["corrected"],
                 one["failure_rate"], one["middle_seconds"]))
    print("\n  never succeeded: %s"
          % (", ".join(answer["never_succeeded"]) or "none"))
    print("  untested:        %s" % ", ".join(answer["untested"]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
