# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-UNT-011
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The suite sweep - every tests/test_*.py, and WHY each one that failed did.

    python brain/heron_unit_test.py
    python brain/heron_unit_test.py --suites test_docs_guard.py test_qa.py
    python brain/heron_unit_test.py --timeout 120
    python brain/heron_unit_test.py --json        the whole result, for a caller

WHAT IT IS FOR (docs/28, HERON-DEV-UNT-011)
--------------------------------------------
"Runs unit tests." T1. D-75 settles whose: Heron's own code, not the
fragment library.

IT IS A FILE BECAUSE THERE WAS NOWHERE TO PUT THE CLAIM
--------------------------------------------------------
The sweep already existed - as inline bash in .github/workflows/gates.yml,
which cannot carry a metadata header. So D-75 could not close this row on
an existing file the way it closed HERON-DEV-BLD-010 on check-compile.py:
"a file has to be written before there is anything to claim". This is that
file.

AND THE BASH IT WAS WRITTEN FROM IS GONE, BECAUSE THE COPIES HAD DRIFTED.
This paragraph used to say the two "run the same suites the same way", and
they did not: the workflow's loop took any non-zero exit as a failure, so a
suite saying COULD NOT RUN landed on the failure list beside one that ran
and broke - the single distinction this file exists for. Three suites were
permanently excused for it, and once excused a real failure in one of them
was invisible. gates.yml now CALLS this agent, with --json, and there is
one sweep in one place. FRAGMENT-ISSUES row 5b-49.

THREE EXIT CODES, NOT TWO, AND THE THIRD IS THE POINT
-------------------------------------------------------
    0   passed
    3   could not run HERE - WAITING, not failing
    *   failed

tests/test_mcp_serves.py exits 3 when the MCP SDK is absent, so
check-gaps.py reports it as waiting rather than as a break. A sweep that
collapsed 3 into "failed" would report a machine without an optional
package as a repository with a regression in it - and the person reading
it would go looking for a bug that is not there.

**A waiting suite is UNPROVEN. It is never counted as a pass**, which is
why `passed` and `ok` are separate numbers and `waiting` has its own list.

WHY EVERY FAILURE CARRIES ITS LAST LINE
-----------------------------------------
"3 suites failed" is the shape a real regression hides in. Three failing
for one missing package is ONE finding about this machine; three failing
for three reasons is three findings about the code, and the difference
between them is the whole value of the sweep. So each failure keeps the
last thing it printed, and `sharedReason` says when they are all the same
sentence.

The ship skill states this as a rule and had no tool for it: "what matters
is that the failures do not share a reason, because a lump total is how a
real regression hides."

IT RUNS. HERON-DEV-QA-016 JUDGES
----------------------------------
That gate's row is "final gate before approval, NEVER THE IMPLEMENTER",
and it runs nothing on purpose. This is the other half of that seam: it
produces results and forms no opinion about whether they are good enough
to merge. `passed` here means no suite failed - not that the change is
acceptable, which is the gate's answer and not this one's.

WHAT IT DOES NOT DO
---------------------
**It does not decide what CI requires.** gates.yml is the definition of
required and HERON-DEV-QA-016 parses it. This runs what is on disk, which
is a larger set on purpose: a suite that exists and that CI does not run
is still a suite whose failure is worth seeing.

**It never edits, skips or quarantines a suite.** A test made to pass by
being switched off is the failure mode D-30 exists to prevent.

**A green sweep is not a proof.** Every suite here runs with no Revit, so
passing says the code is self-consistent and nothing about what happens in
front of a model. That is D-30, and it travels in `unjudged`.
"""

from __future__ import annotations

import glob
import json
import os
import subprocess
import sys
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The repository's own word for "could not run here". Anything else
# non-zero is a failure. See the docstring.
WAITING = 3

# The four refusals that happen BEFORE any suite runs. Their answer carries
# no `ok`, `waiting` or `failed` to read, so a caller has to be able to tell
# them apart before it indexes anything - and they are exit 2, which is
# neither a pass nor a test failure.
BEFORE_ANYTHING_RAN = ("NO_SUITES", "NOT_A_SUITE", "NOT_A_ROOT", "BAD_TIMEOUT")

# One suite's bound. test_brain_reachable.py is the longest on record at
# about 45 seconds, so this is generous rather than tight: the bound is
# here to catch a HANG, and a bound that fires on a slow machine reports
# the machine as a regression.
TIMEOUT = 600

SLOWEST = 5


def suites(root=None, named=None):
    """
    Every tests/test_*.py, sorted - or the named ones, resolved.

    Discovered rather than listed. A typed list goes stale the first time
    somebody adds a suite, and it goes stale SILENTLY, which is the worst
    direction: a suite nobody named is a suite nobody ran.
    """
    where = os.path.join(root or ROOT, "tests")
    if named is None:
        return sorted(glob.glob(os.path.join(where, "test_*.py")))

    found = []
    for one in named:
        one = str(one).strip()
        path = one if os.path.isabs(one) else os.path.join(where,
                                                           os.path.basename(one))
        found.append(path)
    return found


def _why(text):
    """
    The last thing a suite printed, which is what it died saying.

    Tracebacks end on the exception line, and this repository's own tests
    end on a FAIL line, so the last non-empty line is the sentence in both
    shapes. Truncated, because a result is read by a person.
    """
    for line in reversed((text or "").splitlines()):
        line = line.strip()
        if line:
            return line[:200]
    return ""


def _run(path, timeout, python=None):
    """{suite, code, seconds, why} for one suite. It is never re-run here."""
    started = time.time()
    name = os.path.basename(path)
    try:
        done = subprocess.run(
            [python or sys.executable, path],
            cwd=os.path.dirname(os.path.dirname(os.path.abspath(path))),
            stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
            timeout=timeout)
    except subprocess.TimeoutExpired:
        return {"suite": name, "code": None, "seconds": round(time.time() - started, 1),
                "why": "did not finish inside %d seconds and was stopped."
                       % timeout, "timedOut": True}
    except OSError as problem:
        return {"suite": name, "code": None,
                "seconds": round(time.time() - started, 1),
                "why": "could not be started: %s" % problem, "timedOut": False}

    text = done.stdout.decode("utf-8", "replace")
    return {"suite": name, "code": done.returncode,
            "seconds": round(time.time() - started, 1),
            "why": _why(text), "timedOut": False}


def sweep(named=None, timeout=None, root=None, python=None):
    """
    {passed, ok, waiting, failed} - or a refusal. Nothing is fixed here.
    """
    where = root or ROOT
    if not os.path.isdir(os.path.join(where, "tests")):
        return {"passed": False, "refused": "NOT_A_ROOT",
                "why": "%s has no tests/ directory, so there is nothing to "
                       "sweep. A root that holds no suites and a root that "
                       "does not exist must not come back the same way as a "
                       "repository whose suites all passed." % where}

    if timeout is None:
        timeout = TIMEOUT
    elif not isinstance(timeout, int) or isinstance(timeout, bool) or timeout <= 0:
        return {"passed": False, "refused": "BAD_TIMEOUT",
                "why": "a timeout of %r is not a positive whole number of "
                       "seconds. A suite with no bound can hang the sweep, "
                       "and a bound of zero fails every suite before it "
                       "starts." % (timeout,)}

    found = suites(where, named)
    if not found:
        return {"passed": False, "refused": "NO_SUITES",
                "why": "no tests/test_*.py under %s. An empty sweep reports "
                       "nothing failing, which is indistinguishable from "
                       "everything passing and means the opposite." % where}

    missing = [os.path.basename(p) for p in found if not os.path.isfile(p)]
    if missing:
        return {"passed": False, "refused": "NOT_A_SUITE",
                "why": "%s does not exist. A named suite that is not there "
                       "was not run, and a sweep that quietly dropped it "
                       "would report a pass for work nobody did."
                       % ", ".join(missing)}

    started = time.time()
    ok, waiting, failed, timed_out, timings = [], [], [], [], []
    for path in found:
        one = _run(path, timeout, python)
        timings.append({"suite": one["suite"], "seconds": one["seconds"]})
        if one.pop("timedOut"):
            timed_out.append({"suite": one["suite"], "bound": timeout,
                              "why": one["why"]})
        elif one["code"] == 0:
            ok.append(one["suite"])
        elif one["code"] == WAITING:
            waiting.append({"suite": one["suite"], "why": one["why"]})
        else:
            failed.append(one)

    # Counted BEFORE anything is taken out of it. `shared` used to pop from
    # this set, so the count read later was the count after the pop - the
    # kind of arithmetic that is right until somebody reorders two lines.
    reasons = set(one["why"] for one in failed if one["why"])
    distinct = len(reasons)
    shared = sorted(reasons)[0] if distinct == 1 and len(failed) > 1 else None
    slowest = sorted(timings, key=lambda t: -t["seconds"])[:SLOWEST]

    result = {
        "passed": not failed and not timed_out,
        "of": len(found),
        "ok": ok,
        "waiting": waiting,
        "failed": failed,
        "timedOut": timed_out,
        "sharedReason": shared,
        "slowest": slowest,
        "seconds": round(time.time() - started, 1),
        "ranAnything": True,
        "fixedAnything": False,
        "unjudged": [
            "a green sweep is not a proof. Every suite here runs with no "
            "Revit, so passing says the code is self-consistent and nothing "
            "about what happens in front of a model (D-30).",
            "a WAITING suite is unproven, not passed. %d of %d could not run "
            "on this machine and prove nothing either way."
            % (len(waiting), len(found)),
            "whether the change is acceptable is HERON-DEV-QA-016's answer. "
            "This one only says which suites ran and what they said.",
        ],
    }

    if timed_out:
        result["refused"] = "TIMED_OUT"
        result["why"] = ("%d suite(s) did not finish inside %d seconds: %s. A "
                         "suite that hangs is not a suite that failed, and "
                         "neither is it one that passed."
                         % (len(timed_out), timeout,
                            ", ".join(t["suite"] for t in timed_out)))
        return result

    if failed:
        result["refused"] = "SUITE_FAILED"
        if len(failed) == 1:
            said = "%s said: %s" % (failed[0]["suite"],
                                    failed[0]["why"] or "nothing at all")
        elif result["sharedReason"]:
            said = ("all %d said the same thing, which usually means this "
                    "machine rather than the code: %s"
                    % (len(failed), result["sharedReason"]))
        elif distinct:
            said = ("they gave %d different reasons, so they are that many "
                    "separate findings rather than one." % distinct)
        else:
            said = ("not one of them printed a reason, so the sweep cannot "
                    "say whether they share one. Run them singly.")
        result["why"] = ("%d of %d suite(s) failed - %s"
                         % (len(failed), len(found), said))
        return result

    result["why"] = (
        "%d suite(s) ran: %d passed, %d waiting on something this machine has "
        "not got. Nothing failed." % (len(found), len(ok), len(waiting)))
    return result


def main(argv):
    named, timeout, rest = None, None, list(argv[1:])
    if "--suites" in rest:
        at = rest.index("--suites")
        named = [a for a in rest[at + 1:] if not a.startswith("--")]
        rest = rest[:at] + rest[at + 1 + len(named):]
    if "--timeout" in rest:
        at = rest.index("--timeout")
        try:
            timeout = int(rest[at + 1])
        except (IndexError, ValueError):
            sys.stdout.write("--timeout wants a whole number of seconds.\n")
            return 2

    out = sweep(named=named, timeout=timeout)
    refused_early = out.get("refused") in BEFORE_ANYTHING_RAN

    if "--json" in rest:
        # THE WHOLE RESULT, NOT A SUMMARY OF IT. The printed report below is
        # written for a person and drops the two things a caller needs most:
        # which suites are WAITING as against FAILED, and the last line each
        # failure printed. .github/workflows/gates.yml kept a second copy of
        # this sweep in bash for want of a way to ask (row 5b-49), and a
        # second copy is a copy that drifts.
        sys.stdout.write(json.dumps(out, indent=2, sort_keys=True) + "\n")
        return 2 if refused_early else (0 if out["passed"] else 1)

    def w(text):
        sys.stdout.write(text.encode("ascii", "replace").decode("ascii"))

    if refused_early:
        w("REFUSED  %s\n  %s\n" % (out["refused"], out["why"]))
        return 2

    w("\nTHE SUITE SWEEP   %d suite(s) in %.1fs\n" % (out["of"], out["seconds"]))
    w("%s\n" % ("=" * 62))
    w("  passed   %d\n" % len(out["ok"]))
    w("  waiting  %d\n" % len(out["waiting"]))
    w("  failed   %d\n" % len(out["failed"]))

    for one in out["waiting"]:
        w("\n  WAITING  %s\n           %s\n" % (one["suite"], one["why"]))
    for one in out["failed"]:
        w("\n  FAILED   %s (exit %s)\n           %s\n"
          % (one["suite"], one["code"], one["why"]))
    if out.get("sharedReason"):
        w("\n  Every failure says the same thing, which usually means this\n"
          "  machine rather than the code:\n    %s\n" % out["sharedReason"])

    w("\n  slowest: %s\n"
      % ", ".join("%s %.1fs" % (t["suite"], t["seconds"])
                  for t in out["slowest"]))
    w("\n%s\n" % out["why"])
    for line in out["unjudged"]:
        w("  - %s\n" % line)
    return 0 if out["passed"] else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
