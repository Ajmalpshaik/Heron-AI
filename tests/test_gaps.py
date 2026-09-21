# Heron-Agent:  HERON-AHR-GAP-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Capability Gap Agent - the report over the audit trail.

    python tests/test_gaps.py

WHAT IT PROVES
  1. Both shapes of `ms` are read. Durations were written as strings until
     2026-09-07 and as numbers after it, the trail is never pruned, so a
     reader that handles one shape silently loses half the timings.
  2. A CORRECT REFUSAL is never counted as a capability gap. `needs_unbound`
     is the executor behaving correctly, and building a fragment to "fix" it
     would be work caused by a misread report.
  3. A DEFECT is counted as one.
  4. An error code in NEITHER list is surfaced rather than assumed harmless.
     A checker that quietly files the unknown under "fine" is worse than none.
  5. A fragment is attributed to its runs, and entries that name no fragment
     are counted apart rather than pooled into one nameless bucket that would
     out-rank every real one.
  6. A corrupt line costs one entry, not the file.
  7. An empty trail reports nothing rather than claiming there are no gaps.
  9. AND A WINDOW THAT CANNOT HOLD A DAY IS REFUSED rather than emptying a
     full trail - because the message check 7 proves is right for an empty
     trail is a LIE about a full one, and `--days -7` used to reach it.
 10. The median of two runs is between them, not the slower one. The same
     rule as a missing duration never being averaged in as zero.

WHAT IT DOES NOT PROVE. That the report's ADVICE is right. It says what the
trail contains; whether the thing most worth building is the thing that failed
most often is a judgement, and no test here makes it.
"""

import io
import os
import sys
import shutil
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_gaps as GAPS                                     # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def trail(directory, lines):
    """Write a month file exactly as HeronAudit does - one object per line."""
    path = os.path.join(directory, "audit-202609.jsonl")
    with io.open(path, "w", encoding="utf-8") as handle:
        for line in lines:
            handle.write(line + u"\n")


def main():
    workspace = tempfile.mkdtemp(prefix="heron-gaps-")
    try:
        print("1. Both shapes of ms, because the trail is never pruned")
        trail(workspace, [
            u'{"at":"2026-09-06T10:00:00Z","op":"run_fragment_read","ok":true,"ms":"6620"}',
            u'{"at":"2026-09-07T10:00:00Z","op":"run_fragment_read","ok":true,"ms":41}',
        ])
        entries, skipped = GAPS.read(workspace)
        found = GAPS.analyse(entries)
        timings = found["per_op"]["run_fragment_read"]["ms"]
        check(sorted(timings) == [41, 6620],
              "a string ms and a numeric ms both read as numbers")
        check(GAPS.duration({"ms": "27"}) == 27 and GAPS.duration({"ms": 27}) == 27,
              "duration() accepts either shape")
        check(GAPS.duration({}) is None and GAPS.duration({"ms": "soon"}) is None,
              "a missing or unreadable duration is None, never 0")

        print()
        print("2 and 3. A refusal is not a gap; a defect is")
        trail(workspace, [
            u'{"at":"2026-09-07T10:00:00Z","op":"run_fragment_read","ok":false,'
            u'"error":"needs_unbound","ms":3}',
            u'{"at":"2026-09-07T10:00:01Z","op":"run_fragment_read","ok":false,'
            u'"error":"needs_unbound","ms":3}',
            u'{"at":"2026-09-07T10:00:02Z","op":"run_fragment_read","ok":false,'
            u'"error":"compile_failed","ms":9}',
        ])
        entries, _ = GAPS.read(workspace)
        found = GAPS.analyse(entries)
        check(found["refusals"]["needs_unbound"] == 2,
              "two needs_unbound counted as correct refusals")
        check("needs_unbound" not in found["defects"],
              "needs_unbound is NOT counted as a capability gap")
        check(found["defects"]["compile_failed"] == 1,
              "compile_failed is counted as a defect")
        check(found["failed"] == 3,
              "all three still count as failures - the split is in how they are judged")

        print()
        print("4. An unknown error code is surfaced, not assumed harmless")
        trail(workspace, [
            u'{"at":"2026-09-07T10:00:00Z","op":"run_fragment_read","ok":false,'
            u'"error":"a_code_nobody_has_classified","ms":1}',
        ])
        entries, _ = GAPS.read(workspace)
        found = GAPS.analyse(entries)
        check(found["unclassified"]["a_code_nobody_has_classified"] == 1,
              "an unclassified failure is reported as unclassified")
        check(not found["defects"] and not found["refusals"],
              "and is not quietly filed under either")

        print()
        print("5. A fragment is attributed; nameless runs are counted apart")
        trail(workspace, [
            u'{"at":"2026-09-07T10:00:00Z","op":"run_fragment_read","ok":true,'
            u'"fragment":"list-levels","ms":7}',
            u'{"at":"2026-09-07T10:00:01Z","op":"run_fragment_read","ok":false,'
            u'"fragment":"list-levels","error":"compile_failed","ms":9}',
            u'{"at":"2026-09-07T10:00:02Z","op":"run_fragment_read","ok":true,"ms":37017}',
        ])
        entries, _ = GAPS.read(workspace)
        found = GAPS.analyse(entries)
        levels = found["per_fragment"].get("list-levels", {})
        check(levels.get("runs") == 2 and levels.get("failed") == 1,
              "list-levels shows 2 runs, 1 failed")
        check(found["unnamed_fragment_runs"] == 1,
              "the run naming no fragment is counted apart")
        check("" not in found["per_fragment"] and None not in found["per_fragment"],
              "and never becomes a nameless bucket in the ranking")

        print()
        print("6. A corrupt line costs one entry, not the file")
        trail(workspace, [
            u'{"at":"2026-09-07T10:00:00Z","op":"count_elements","ok":true,"ms":3}',
            u'{"at":"2026-09-07T10:00:01Z","op":"count_ele',
            u'{"at":"2026-09-07T10:00:02Z","op":"count_elements","ok":true,"ms":4}',
        ])
        entries, skipped = GAPS.read(workspace)
        check(len(entries) == 2 and skipped == 1,
              "two entries read, one truncated line skipped and counted")

        print()
        print("8. Yesterday's fixed bug is not today's alarm")
        trail(workspace, [
            u'{"at":"2026-09-06T10:00:00Z","op":"run_fragment_read","ok":false,'
            u'"error":"compile_failed","ms":9}',
            u'{"at":"2026-09-06T10:00:01Z","op":"run_fragment_read","ok":false,'
            u'"error":"compile_failed","ms":9}',
            u'{"at":"2026-09-07T10:00:00Z","op":"run_fragment_read","ok":true,"ms":7}',
            u'{"at":"2026-09-07T10:00:01Z","op":"run_fragment_read","ok":true,"ms":7}',
        ])
        entries, _ = GAPS.read(workspace)
        found = GAPS.analyse(entries)
        check(found["recent"]["day"] == "2026-09-07",
              "recent is the newest day in the trail, not today's date")
        check(found["recent"]["defects"] == 0 and found["recent"]["requests"] == 2,
              "the newest day shows 0 defects in 2 runs")
        check(found["defects"]["compile_failed"] == 2,
              "and the 2 historical defects are still counted, as history")
        check(found["by_day"]["2026-09-06"]["defects"] == 2,
              "the day they happened on is still named")

        print()
        print("7. An empty trail says nothing rather than 'no gaps'")
        for name in os.listdir(workspace):
            os.remove(os.path.join(workspace, name))
        entries, _ = GAPS.read(workspace)
        found = GAPS.analyse(entries)
        out = io.StringIO()
        GAPS.report(found, [], 0, out)
        text = out.getvalue()
        check(found["requests"] == 0, "an empty directory reads as zero requests")
        check("not the same as no gaps" in text,
              "and the report says so rather than reporting a clean bill")

        print()
        print("9. a window that cannot hold a day is refused, not emptied")
        # ROW 5b-93, AND THE SECTION ABOVE IS WHY IT MATTERS. That message is
        # right for an empty trail and a LIE for a full one, and `since()`
        # could empty a full one: --days -7 came back with nothing, and the
        # report then told somebody "Heron has no record of doing anything
        # yet" about their own history. The window is where it belongs.
        with io.open(os.path.join(workspace, "audit-202609.jsonl"), "w",
                     encoding="utf-8") as handle:
            for day in ("01", "10", "20"):
                handle.write(u'{"at": "2026-09-%sT10:00:00.000Z", '
                             u'"op": "revit_select", "ok": true}\n' % day)
        entries, _ = GAPS.read(workspace)
        check(len(entries) == 3, "three entries to window (%d)" % len(entries))

        refused = None
        try:
            GAPS.since(entries, -7)
        except ValueError as why:
            refused = str(why)
        check(refused is not None,
              "a window of -7 days is REFUSED rather than returning nothing")
        check(refused and "not a window" in refused,
              "and the refusal says what was wrong with it")
        check(GAPS.since(entries, 30) == entries,
              "while a real window still works")
        check(len(GAPS.since(entries, 7)) == 1,
              "and still narrows - anchored on the newest entry, so a real "
              "window can never come back empty")
        for whole in (None, 0):
            check(GAPS.since(entries, whole) == entries,
                  "%r still means the whole trail" % (whole,))

        print()
        print("10. the median of two runs is between them, not the slower one")
        # The same rule as duration() returning None rather than 0: this file
        # refuses to average a missing duration with a real one because "that
        # is how a timing report starts lying". Two runs is the ordinary case
        # for most of the library.
        middle, worst = GAPS._stat([10, 100])
        check(middle == 55,
              "median of 10 ms and 100 ms is 55, and it came back %s" % middle)
        check(worst == 100, "and the worst is still 100")
        check(GAPS._stat([10, 20, 100])[0] == 20, "an odd count is unchanged")
        check(GAPS._stat([10, 20, 30, 100])[0] == 25,
              "and four values take the middle pair")
        check(GAPS._stat([7]) == (7, 7), "one run is its own median and worst")
        check(GAPS._stat([]) == (None, None),
              "and nothing timed stays None rather than becoming 0")

        print()
        print("   (bonus) a directory that does not exist is not an error")
        entries, skipped = GAPS.read(os.path.join(workspace, "nope"))
        check(entries == [] and skipped == 0,
              "a missing trail reads as empty, never as a crash")

    finally:
        shutil.rmtree(workspace, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - both shapes of ms are read, a correct refusal is never")
    print("reported as a missing capability, and an error nobody has classified")
    print("is surfaced rather than assumed harmless.")
    print()
    print("It proves nothing about the report's judgement. What the trail")
    print("contains is measured here; what is most worth building next is not.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
