# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-REVIT-PRF-017
# Heron-Step:   4
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Revit performance - a missing measurement is never a fast one.

    python tests/test_timing.py

WHAT IT PROVES
  1. NOTHING IS TIMED HERE. The reader and the duration rule are the SAME
     OBJECTS as HERON-AHR-GAP-001's, and this module starts no clock.

  2. A ROW WITH NO `ms` IS EXCLUDED, NOT COUNTED AS ZERO - shown by
     adding one to a trail and watching every number stay put.

  3. THE NUMBERS ARE THE NUMBERS - fastest, median and slowest against a
     trail whose answers can be worked out by hand.

  4. COST PER ELEMENT ONLY WHERE A SIZE WAS RECORDED.

  5. NO LIMIT IS INVENTED. `against` needs the caller's number, and with
     no number it refuses rather than assuming one.

  6. A LIMIT NOTHING HAS REACHED IS NOT A LIMIT PROVED RIGHT, and the
     answer says so in those terms.

  7. NO SLOWER-OR-FASTER VERDICT IS DRAWN, even when the trail doubles.

  8. IT REALLY READS A TRAIL FROM DISK, written the way HeronAudit writes
     one.

  9. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_timing as PRF                                     # noqa: E402
import heron_gaps as GAPS                                      # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def row(op, ms=None, at="2026-09-14T09:00:00Z", **extra):
    card = {"at": at, "op": op, "ok": True}
    if ms is not None:
        card["ms"] = ms
    card.update(extra)
    return card


# Chosen so every answer can be worked out by hand: 100, 300, 500 gives
# fastest 100, median 300, slowest 500.
TRAIL = [row("count_elements", 100, candidates=1000),
         row("count_elements", 300, candidates=1000),
         row("count_elements", 500, candidates=2000),
         row("select_by_category", 40),
         row("release")]


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_timing.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. nothing is timed here")
    check(PRF.read is GAPS.read, "PRF.read IS GAPS.read - the same object")
    check(PRF.duration is GAPS.duration,
          "PRF.duration IS GAPS.duration - one rule for a missing `ms`")
    check(PRF.audit_dir is GAPS.audit_dir,
          "PRF.audit_dir IS GAPS.audit_dir - one place resolves the trail")
    # NOT A WORD SEARCH FOR "Stopwatch" - this is Python. The claim is that
    # the module measures no elapsed time, so the proof is that it imports
    # nothing that could.
    check("import time" not in logic and "time.time" not in logic
          and "datetime" not in logic,
          "it imports no clock, so it cannot be timing anything itself")

    print("\n2. a row with no duration is excluded, not counted as zero")
    answer = PRF.cost(TRAIL)
    counts = dict((card["op"], card) for card in answer["operations"])
    more = PRF.cost(TRAIL + [row("count_elements", candidates=9999)])
    after = dict((card["op"], card) for card in more["operations"])
    for field in ("fastest", "median", "slowest", "timed"):
        check(after["count_elements"][field] == counts["count_elements"][field],
              "adding an untimed run leaves %s unchanged (%d)"
              % (field, counts["count_elements"][field]))
    check(after["count_elements"]["runs"]
          == counts["count_elements"]["runs"] + 1,
          "but it IS counted as a run - never silently discarded")
    check(after["count_elements"]["untimed"] == 1,
          "and it is named as untimed")

    print("\n3. the numbers are the numbers")
    check(counts["count_elements"]["fastest"] == 100, "fastest is 100ms")
    check(counts["count_elements"]["median"] == 300, "median is 300ms")
    check(counts["count_elements"]["slowest"] == 500, "slowest is 500ms")
    check(counts["count_elements"]["timed"] == 3, "over 3 timed runs")
    # An even count takes the two middle values, so 40 and 60 give 50.
    even = PRF.cost([row("a", 40), row("a", 60)])
    check(even["operations"][0]["median"] == 50,
          "an even number of runs medians the middle pair: 40, 60 -> 50")
    only = [card["op"] for card in answer["untimed"]]
    check(only == ["release"],
          "an operation timed NOWHERE is listed apart: %s" % ", ".join(only))

    print("\n4. cost per element only where a size was recorded")
    sized = dict((card["op"], card) for card in answer["per_element"])
    check(sorted(sized) == ["count_elements"],
          "only the operation carrying a count: %s" % ", ".join(sorted(sized)))
    # 900ms over 4000 elements is 225ms per thousand, by hand.
    check(sized["count_elements"]["msPerThousand"] == 225,
          "900ms over 4,000 elements is 225ms per thousand")
    check("select_by_category" not in sized,
          "an operation with a duration and no count is not divided by a "
          "number nobody wrote")

    print("\n5. no limit is invented")
    check("timeout-seconds" not in logic.replace("`timeout-seconds`", ""),
          "the module reads no contract of its own to find a limit")
    for bad in (0, -1, "sixty", None, True, 1.5):
        said = PRF.against(bad, "count_elements", answer)
        reached.add(said.get("refused"))
        check(said.get("refused") == "NOT_A_TIMEOUT", "%r is refused" % bad)
    said = PRF.against(60, "move_elements", answer)
    check(said["known"] is False and "move_elements" in said["why"],
          "an operation nothing timed comes back not known, not 'fine'")

    print("\n6. a limit nothing has reached is not a limit proved right")
    said = PRF.against(60, "count_elements", answer)
    check(said["exceeded"] is False, "500ms is inside a 60s limit")
    check("not the limit proved right" in said["why"],
          "and the answer says so rather than reporting a pass")
    over = PRF.against(1, "count_elements", PRF.cost(
        TRAIL + [row("count_elements", 4000, candidates=1000)]))
    check(over["exceeded"] is True, "4,000ms is past a 1s limit")
    check(over["declaredMs"] == 1000, "the limit is reported in the same unit")

    print("\n7. no slower-or-faster verdict is drawn")
    doubled = PRF.cost([row("a", 100), row("a", 100),
                        row("a", 1000), row("a", 1000)])
    early = dict((c["op"], c) for c in doubled["earlier"])
    late = dict((c["op"], c) for c in doubled["recent"])
    check(early["a"]["median"] == 100 and late["a"]["median"] == 1000,
          "both halves are reported: 100ms then 1000ms")
    words = " ".join(doubled["unjudged"]).lower()
    for verdict in ("regression", "slower than", "degraded", "improved"):
        check(verdict not in words.replace("a regression needs", ""),
              "the answer never calls it %r" % verdict)
    check(not [k for k in doubled if k in ("slower", "faster", "verdict")],
          "and there is no field for a verdict to hide in")

    print("\n8. it really reads a trail from disk")
    yard = tempfile.mkdtemp(prefix="heron-timing-")
    try:
        with io.open(os.path.join(yard, "audit-2026-09-14.jsonl"), "w",
                     encoding="utf-8") as handle:
            for card in TRAIL:
                handle.write(json.dumps(card) + "\n")
            handle.write("{ this line is truncated\n")
        from_disk = PRF.cost(directory=yard)
        check(from_disk["of"] == len(TRAIL),
              "%d rows read back off disk" % from_disk["of"])
        check([c["op"] for c in from_disk["operations"]]
              == [c["op"] for c in answer["operations"]],
              "and the same operations, so one bad line cost one entry")

        print("\n9. every failure is named and reached")
        empty = os.path.join(yard, "empty")
        os.makedirs(empty)
        for these, name in ((([], None), "NOTHING_RECORDED"),
                            ((None, empty), "NOTHING_RECORDED"),
                            (([row("x")], None), "NOTHING_TIMED")):
            entries, where = these
            said = PRF.cost(entries, where)
            reached.add(said.get("refused"))
            check(said.get("refused") == name, "%s is reached" % name)
        said = PRF.against(60, "count_elements", {"measured": False})
        reached.add(said.get("refused"))
        check(said.get("refused") == "NOTHING_RECORDED",
              "and `against` refuses with no measurement too")
    finally:
        shutil.rmtree(yard, ignore_errors=True)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-REVIT-PRF-017.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 3, "the contract declares 3 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(answer["unjudged"]) == 5, "five things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a missing measurement is never a fast one")
    return 0


if __name__ == "__main__":
    sys.exit(main())
