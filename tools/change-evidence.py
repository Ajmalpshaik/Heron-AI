# Heron-Agent:  HERON-DEV-EXP-021
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Before and after, measured the same way twice.

    python tools/change-evidence.py capture --out before.json
    ... make the change ...
    python tools/change-evidence.py capture --out after.json --against before.json
    python tools/change-evidence.py compare before.json after.json

WHAT IT IS FOR
--------------
"Better" is a comparison, and this repository already refuses claims that have
no evidence behind them (AGENTS.md, D-30). What was missing was anything a
person could run mid-task to hold the two states side by side. CI has half of
it - .github/workflows/gates.yml compares a SET against a named list rather
than counting anything - and that half is the good half, borrowed here
deliberately: a total hides a regression that arrives on the same day something
else is fixed. (That list is a known-NOT-RUNNABLE one since row 5b-49; this
paragraph called it a known-failure list until 2026-09-21, which is row 5b-60.)

WHAT IT RECORDS, AND WHAT IT REFUSES TO RECORD
-----------------------------------------------
Only things a command can derive: gate exit codes, the set of suites that did
not pass, and the counts this repository has already been wrong about in prose
- fragments by lifecycle, suites, tools. Nothing is typed by hand into a record
that exists to be believed.

It does NOT record a generic dump of the tree. A record big enough to hide a
change in is a record nobody reads.

IT NEVER CHANGES ANYTHING, AND THAT IS THE SAFETY PROPERTY
-----------------------------------------------------------
The improvement loop this serves is: measure, change one thing, measure again,
keep or revert. The tool owns the two measurements and the ruling. It does not
own the change, and it cannot apply or undo one - so no prompt, no fragment
description, no routing hint and no line of code can be rewritten by anything
in this file. That is what keeps a measuring tool from quietly becoming an
editor, and it is why the loop is safe to point at low-risk assets.

THREE RULINGS, AND THE THIRD IS THE ONE USUALLY MISSING
--------------------------------------------------------
    REVERT              something that passed before does not pass now
    KEEP                something that failed before passes now, and nothing broke
    NO CHANGE MEASURED  neither. NOT "fine", and NOT "better" - this is the
                        honest answer for a change whose effect nothing here
                        can see, and it is the answer most such tools quietly
                        round up into a pass.

Exit 0 = the record was written, or the comparison found no regression.
Exit 1 = a regression. Exit 2 = the tool could not do its job.
"""

import argparse
import datetime
import glob
import io
import json
import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The three that need no compiler, no knowledge store and no Revit. The
# heron-ship skill calls them "the three that must pass"; a non-zero exit from
# any of them is the change rather than the machine, which is exactly the
# property a baseline needs.
FAST = ["check-docs", "check-metadata", "check-structure"]

# Everything this tool knows how to run. Anything not here can still be
# recorded with --stated, and is then marked as a claim rather than a
# measurement - see stated_gates().
#
# THE TWO AT THE BOTTOM ARE HERE BECAUSE check-change.py ASKS FOR THEM.
# Its BY_SIGNAL table owes ["check-compile", "check-api-surface"] for a
# revit-version signal - any change touching a .cs file or
# Directory.Build.props - and neither was in this table, so every such change
# came back REVISE, "gate(s) this change owes were not run", unless the author
# knew to reach for --stated. That files a real measurement as a claim, which
# is the one thing --stated exists NOT to be used for. Measured on PR #228,
# 2026-09-21; FRAGMENT-ISSUES row 5b-73.
#
# Both run on a machine with a .NET SDK. check-api-surface answers in seconds
# with its assemblies cached; check-compile takes minutes, which is presumably
# why it was left out - but check-gaps is slower still and has been here all
# along, and --gate is opt-in, so nothing gets slower by default. What changes
# is that it becomes POSSIBLE to record what the other tool requires.
RUNNABLE = {
    "check-docs":      ["python3", "tools/check-docs.py"],
    "check-metadata":  ["python3", "tools/check-metadata.py"],
    "check-structure": ["python3", "tools/check-structure.py"],
    "check-gaps":      ["python3", "tools/check-gaps.py"],
    "check-licence":   ["python3", "tools/check-licence.py"],
    "check-package":   ["python3", "tools/check-package.py"],
    "check-routing":   ["python3", "tools/check-routing.py"],
    "check-intrusion": ["python3", "tools/check-intrusion.py"],
    "check-compile":      ["python3", "tools/check-compile.py"],
    "check-api-surface":  ["python3", "tools/check-api-surface.py"],
}

PASS, FAIL, NOT_RUN = "PASS", "FAIL", "NOT RUN"


def w(s):
    sys.stdout.write(s.encode("ascii", "replace").decode("ascii"))


def run(cmd, timeout=1800):
    try:
        out = subprocess.run(cmd, cwd=ROOT, capture_output=True,
                             text=True, timeout=timeout)
    except (OSError, subprocess.TimeoutExpired) as exc:
        return None, str(exc)
    lines = (out.stdout or "").strip().splitlines()
    return out.returncode, (lines[-1] if lines else "")


def git(args):
    out = subprocess.run(["git", "-C", ROOT] + args, capture_output=True, text=True)
    return out.stdout.strip() if out.returncode == 0 else None


# ------------------------------------------------------------------ the counts
def counts():
    """
    Every number here comes from disk. None is typed.

    This repository has had three READMEs disagree with the tools by more than
    a hundred fragments at once, each with a sentence beside the number
    admitting it would go stale. The sentence did not help. A count in a record
    is only worth having if the record derived it.
    """
    status = {}
    for path in glob.glob(os.path.join(ROOT, "brain", "fragments", "*", "fragment.yaml")):
        try:
            with io.open(path, encoding="utf-8", errors="replace") as handle:
                for line in handle:
                    if line.startswith("heron-status:"):
                        value = line.split(":", 1)[1].strip()
                        status[value] = status.get(value, 0) + 1
                        break
        except OSError:
            continue
    return {
        "fragments": sum(status.values()),
        "fragments_by_status": status,
        "suites": len(glob.glob(os.path.join(ROOT, "tests", "test_*.py"))),
        "tools": len(glob.glob(os.path.join(ROOT, "tools", "*.py"))),
    }


# ------------------------------------------------------------------- the gates
def gate_results(wanted):
    results = {}
    for name in wanted:
        cmd = RUNNABLE.get(name)
        if cmd is None:
            results[name] = {"result": NOT_RUN, "exit": None, "source": "derived",
                             "detail": "this tool cannot run it - record it "
                                       "with --stated if it ran elsewhere"}
            continue
        code, tail = run(cmd)
        results[name] = {
            "result": NOT_RUN if code is None else (PASS if code == 0 else FAIL),
            "exit": code,
            "source": "derived",
            "detail": (tail or "").strip()[:120],
        }
    return results


def stated_gates(pairs):
    """
    A gate that ran somewhere this container is not - the owner's PC, with a
    .NET SDK or a Revit on it.

    Recorded, and marked `stated` rather than `derived`, because the two are
    not the same kind of fact. A record that let an assertion sit beside a
    measurement in identical type would be a record in which the assertion
    eventually gets believed as one.
    """
    results = {}
    for pair in pairs or []:
        name, _, rest = pair.partition("=")
        value, _, note = rest.partition(":")
        name, value = name.strip(), value.strip().upper()
        if value not in (PASS, FAIL, NOT_RUN, "NOT RUN"):
            w("Ignored --stated %s: the result must be PASS, FAIL or NOT_RUN\n" % pair)
            continue
        results[name] = {"result": NOT_RUN if value in ("NOT_RUN", "NOT RUN") else value,
                         "exit": None, "source": "stated",
                         "detail": note.strip() or "stated, not measured here"}
    return results


def tests_gate(results, note):
    """
    One verdict over the suite map, decided in one place.

    Exit 3 is a suite that COULD NOT RUN (tests/README.md). It is neither a
    pass nor a failure, so it cannot fail this gate - but it is named, because
    a gate reported PASS while four suites never ran is a gate that lies by
    omission.
    """
    if not results:
        return {"result": NOT_RUN, "exit": None, "source": "derived", "detail": note}
    failed = sorted(n for n, c in results.items() if c not in (0, 3))
    could_not = sorted(n for n, c in results.items() if c == 3)
    detail = "%d ran" % len(results)
    if could_not:
        detail += ", %d could not run (%s)" % (len(could_not), ", ".join(could_not)[:60])
    if failed:
        detail += ", failed: %s" % ", ".join(failed)
    return {"result": FAIL if failed else PASS, "exit": None,
            "source": "derived", "detail": detail[:200]}


# ------------------------------------------------------------------- the suites
def suite_results(which):
    """
    Exit code per suite, kept as a MAP rather than a total.

    tests/README.md: 0 is a pass, 1 is a failure, and 3 means the suite could
    not run for want of an optional dependency and proves nothing either way.
    A record that collapsed those three into a number would be the exact
    mistake that file exists to prevent.
    """
    if which == "none":
        return {}, "not run - pass --tests all, or a glob"
    pattern = "test_*.py" if which == "all" else which
    paths = sorted(glob.glob(os.path.join(ROOT, "tests", pattern)))
    results = {}
    for path in paths:
        name = os.path.basename(path)
        try:
            out = subprocess.run(["python3", path], cwd=ROOT,
                                 capture_output=True, text=True, timeout=600)
            results[name] = out.returncode
        except (OSError, subprocess.TimeoutExpired):
            results[name] = None
    return results, "%d suite(s) matching %s" % (len(results), pattern)


# ------------------------------------------------------------------ comparison
def compare(before, after):
    """
    What moved. Sets, not totals - see the module docstring.

    A suite or gate that did not run on EITHER side is not comparable, and is
    reported as such rather than counted as agreement. Two unknowns are not a
    match.
    """
    regressions, improvements, incomparable = [], [], []
    # The same facts as `regressions`, but as names rather than sentences, so a
    # reader downstream does not have to parse English to find out WHICH gate
    # moved. tools/check-change.py needs exactly that: a gate failing on both
    # sides is pre-existing, and one that started failing is this change.
    regressed = {"gates": [], "suites": []}

    b_gates = before.get("gates", {})
    a_gates = after.get("gates", {})
    for name in sorted(set(b_gates) | set(a_gates)):
        was = b_gates.get(name, {}).get("result", NOT_RUN)
        now = a_gates.get(name, {}).get("result", NOT_RUN)
        if NOT_RUN in (was, now):
            if was != now:
                incomparable.append("gate %s: %s -> %s" % (name, was, now))
            continue
        if was == PASS and now == FAIL:
            regressions.append("gate %s passed before and fails now" % name)
            regressed["gates"].append(name)
        elif was == FAIL and now == PASS:
            improvements.append("gate %s failed before and passes now" % name)

    b_tests = before.get("tests", {})
    a_tests = after.get("tests", {})
    for name in sorted(set(b_tests) | set(a_tests)):
        was = b_tests.get(name)
        now = a_tests.get(name)
        if was is None or now is None:
            incomparable.append("suite %s: %s -> %s"
                                % (name, _code(was), _code(now)))
            continue
        # Exit 3 is NOT RUN. Moving between 3 and anything else is a change in
        # what the machine has, not in what the code does.
        if 3 in (was, now) and was != now:
            incomparable.append("suite %s: %s -> %s (an optional dependency "
                                "arrived or left)" % (name, _code(was), _code(now)))
            continue
        if was == 0 and now not in (0, 3):
            regressions.append("suite %s passed before and does not now" % name)
            regressed["suites"].append(name)
        elif was not in (0, 3) and now == 0:
            improvements.append("suite %s failed before and passes now" % name)

    moved = []
    b_counts = before.get("counts", {})
    a_counts = after.get("counts", {})
    for key in sorted(set(b_counts) | set(a_counts)):
        if b_counts.get(key) != a_counts.get(key):
            moved.append("%s: %s -> %s" % (key, b_counts.get(key), a_counts.get(key)))

    if regressions:
        ruling = "REVERT"
    elif improvements:
        ruling = "KEEP"
    else:
        ruling = "NO CHANGE MEASURED"

    return {"ruling": ruling, "regressions": regressions,
            "regressed": regressed,
            "improvements": improvements, "incomparable": incomparable,
            "counts_moved": moved}


def _code(value):
    if value is None:
        return "did not run"
    return {0: "pass", 3: "could not run"}.get(value, "exit %d" % value)


# --------------------------------------------------------------------- capture
def capture(args):
    record = {
        "taken": datetime.datetime.now(datetime.timezone.utc)
                 .strftime("%Y-%m-%dT%H:%M:%SZ"),
        "label": args.label,
        "commit": git(["rev-parse", "HEAD"]),
        "branch": git(["rev-parse", "--abbrev-ref", "HEAD"]),
        "dirty": bool(git(["status", "--porcelain"])),
        "counts": counts(),
    }
    wanted = list(FAST)
    if args.gaps and "check-gaps" not in wanted:
        wanted.append("check-gaps")
    for name in args.gate or []:
        for piece in name.replace(";", ",").split(","):
            piece = piece.strip()
            if piece and piece not in wanted:
                wanted.append(piece)

    record["gates"] = gate_results(wanted)
    record["tests"], record["tests_note"] = suite_results(args.tests)
    record["gates"]["tests"] = tests_gate(record["tests"], record["tests_note"])
    record["gates"].update(stated_gates(args.stated))

    if args.against:
        try:
            with io.open(args.against, encoding="utf-8") as handle:
                before = json.load(handle)
        except (OSError, ValueError) as exc:
            w("Could not read %s - %s\n" % (args.against, exc))
            return 2
        result = compare(before, record)
        record["compared_to"] = {"label": before.get("label"),
                                 "commit": before.get("commit"),
                                 "taken": before.get("taken")}
        record.update(result)

    with io.open(args.out, "w", encoding="utf-8") as handle:
        json.dump(record, handle, indent=2, sort_keys=True)
        handle.write("\n")

    show(record)
    w("\nWritten to %s\n" % args.out)
    return 1 if record.get("regressions") else 0


def show(record):
    w("Commit:   %s%s\n" % ((record.get("commit") or "unknown")[:12],
                            "  (working tree dirty)" if record.get("dirty") else ""))
    w("Taken:    %s\n" % record.get("taken"))
    if record.get("label"):
        w("Label:    %s\n" % record["label"])
    w("\nGates:\n")
    for name in sorted(record.get("gates", {})):
        body = record["gates"][name]
        mark = "" if body.get("source", "derived") == "derived" else " (stated)"
        w("  %-16s %-8s%s %s\n"
          % (name, body["result"], mark, body.get("detail") or ""))

    tests = record.get("tests", {})
    w("\nSuites:   %s\n" % record.get("tests_note", ""))
    if tests:
        # NOT RUN IS NOT A FAILURE, AND THIS LINE MERGED THEM. The gate
        # three functions up gets it right - its `failed` excludes 3 and the
        # result reads PASS - and `_code` below already knows the words. Only
        # this headline counted them together, so one record printed
        #
        #     tests   PASS   2 ran, 1 could not run (test_mcp_serves.py)
        #     2 ran, 1 did not pass
        #
        # three lines apart. Measured 2026-09-21 with the MCP SDK made
        # unimportable. AGENTS.md states the rule this broke: separate the
        # four states and never merge them. FRAGMENT-ISSUES row 5b-60.
        failed = sorted(n for n, c in tests.items() if c not in (0, 3))
        could_not = sorted(n for n, c in tests.items() if c == 3)
        w("  %d ran, %d did not pass" % (len(tests), len(failed)))
        if could_not:
            w(", %d could not run" % len(could_not))
        w("\n")
        for name in failed + could_not:
            w("    %-28s %s\n" % (name, _code(tests[name])))

    counts_ = record.get("counts", {})
    if counts_:
        w("\nDerived counts:\n")
        w("  fragments %s   suites %s   tools %s\n"
          % (counts_.get("fragments"), counts_.get("suites"), counts_.get("tools")))
        by_status = counts_.get("fragments_by_status") or {}
        if by_status:
            w("  by status: %s\n"
              % ", ".join("%s %d" % (k, by_status[k]) for k in sorted(by_status)))

    if "ruling" in record:
        w("\nAgainst %s (%s):\n"
          % (record.get("compared_to", {}).get("label") or "the earlier record",
             (record.get("compared_to", {}).get("commit") or "?")[:12]))
        for line in record.get("regressions", []):
            w("  REGRESSION   %s\n" % line)
        for line in record.get("improvements", []):
            w("  IMPROVEMENT  %s\n" % line)
        for line in record.get("incomparable", []):
            w("  NOT COMPARABLE  %s\n" % line)
        for line in record.get("counts_moved", []):
            w("  count moved  %s\n" % line)
        w("\n  %s\n" % record["ruling"])
        if record["ruling"] == "NO CHANGE MEASURED":
            w("  Nothing measured here moved. That is not the same as 'no harm done'\n"
              "  and it is certainly not 'better' - it means this record cannot see\n"
              "  the effect of the change, and something else has to.\n")


def main(argv=None):
    parser = argparse.ArgumentParser(add_help=True)
    sub = parser.add_subparsers(dest="command")

    cap = sub.add_parser("capture")
    cap.add_argument("--out", required=True)
    cap.add_argument("--label", default="")
    cap.add_argument("--tests", default="none",
                     help="none (default) | all | a glob such as test_frag*.py")
    cap.add_argument("--gaps", action="store_true", help="also run check-gaps (slow)")
    cap.add_argument("--gate", action="append",
                     help="also run this gate. Known: %s" % ", ".join(sorted(RUNNABLE)))
    cap.add_argument("--stated", action="append", metavar="NAME=RESULT[:note]",
                     help="a gate that ran somewhere else. Marked as stated, "
                          "never as measured")
    cap.add_argument("--against", help="an earlier record, to rule on")

    cmp_ = sub.add_parser("compare")
    cmp_.add_argument("before")
    cmp_.add_argument("after")
    cmp_.add_argument("--json", action="store_true")

    args = parser.parse_args(argv)

    if args.command == "capture":
        return capture(args)

    if args.command == "compare":
        try:
            with io.open(args.before, encoding="utf-8") as handle:
                before = json.load(handle)
            with io.open(args.after, encoding="utf-8") as handle:
                after = json.load(handle)
        except (OSError, ValueError) as exc:
            w("Could not read a record - %s\n" % exc)
            return 2
        result = compare(before, after)
        if args.json:
            w(json.dumps(result, indent=2, sort_keys=True) + "\n")
        else:
            merged = dict(after)
            merged.update(result)
            merged["compared_to"] = {"label": before.get("label"),
                                     "commit": before.get("commit"),
                                     "taken": before.get("taken")}
            show(merged)
        return 1 if result["regressions"] else 0

    parser.print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main())
