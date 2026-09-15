# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-REVIT-PRF-017
# Heron-Step:   4
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Revit performance - what the work actually costs, from runs that happened.

    python brain/heron_timing.py

WHAT IT IS FOR (docs/28, HERON-REVIT-PRF-017)
-----------------------------------------------
"Timing, element counts, operation cost limits." T1, risk READ.

IT ANSWERS A QUESTION ANOTHER AGENT ALREADY ADMITS IT IS GUESSING AT
----------------------------------------------------------------------
heron_architect writes every new contract's timeout and then says so in
its own answer:

    "is %d seconds the right timeout? Nothing has been timed, so it is
     a default and not a measurement (docs/19 s3)."

That sentence is this agent's whole reason to exist. Something HAS been
timed - HERON-REVIT-APP-003's dispatcher has put an `ms` on every job in
the audit trail since Step 6 - and nobody was reading it back as a cost.

NOTHING IS MEASURED HERE; IT IS READ BACK
-------------------------------------------
No stopwatch in this file. The dispatcher already times every job with
one, and a second clock on this side would be measuring the trail rather
than the work. The trail is read through HERON-AHR-GAP-001's own reader
and its own `duration`, bound by identity, so a change to how a duration
is written is followed in one place.

A MISSING DURATION IS NOT A FAST ONE
--------------------------------------
`duration` returns None for a row with no `ms`, and that rule is worth
keeping rather than re-deciding: averaging a missing measurement in as
zero is precisely how a timing report starts lying. Rows with no
duration are counted in `untimed` and left out of every number.

THE LIMIT IS NOT INVENTED HERE
--------------------------------
"Operation cost limits" could be read as a table of how long each thing
may take. There is no such table in this project, and writing one would
be eight numbers nobody measured - the exact mistake this agent exists
to correct.

So the limit is always somebody else's declaration, handed in. `against`
takes a contract's declared `timeout-seconds` and says whether any
recorded run has ever gone past it. If nothing was recorded it says
that, rather than a reassuring silence.

Two things about the limit that are NOT settled here and are said out
loud instead: the bridge's own operation timeout lives in
`mcp/client/heron_config.py`, which D-48 forbids brain from importing,
so this agent cannot state that number; and a timeout nobody has
exceeded is not a timeout proved right - it is one nothing has tested.

IS IT GETTING SLOWER? THE NUMBERS, NOT THE VERDICT
----------------------------------------------------
The trail is split in half and both halves are reported per operation.
Saying "this got slower" needs a threshold - 10%? 2x? - and every
candidate is a number nobody chose. So both halves come back beside each
other and the comparison is left to the reader, named in `unjudged`.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_gaps as GAPS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# HERON-AHR-GAP-001's reader and its duration rule, bound rather than
# copied. `duration` is the one that matters: it returns None for a
# missing `ms`, and keeping that identical is what stops two reports
# disagreeing about the same trail.
read = GAPS.read
duration = GAPS.duration
audit_dir = GAPS.audit_dir

# The count fields HeronAudit actually writes beside a duration. Cost per
# element is only computable where one of these is present, so the list is
# what exists rather than what would be convenient.
SIZE_FIELDS = ("candidates", "elements", "parts")


def _middle(numbers):
    """The median, without pulling in a library for one line."""
    ordered = sorted(numbers)
    middle = len(ordered) // 2
    if len(ordered) % 2:
        return ordered[middle]
    return (ordered[middle - 1] + ordered[middle]) // 2


def _size(row):
    """How much work the row says it was, or None."""
    for field in SIZE_FIELDS:
        value = row.get(field)
        if isinstance(value, bool):
            continue
        if isinstance(value, int) and value > 0:
            return value
    return None


def _per_op(entries):
    """{op: {runs, ms, sizes}} - every row, timed or not."""
    out = {}
    for row in entries:
        bucket = out.setdefault(str(row.get("op") or "(none)"),
                                {"runs": 0, "ms": [], "pairs": []})
        bucket["runs"] += 1
        ms = duration(row)
        if ms is None:
            continue
        bucket["ms"].append(ms)
        size = _size(row)
        if size:
            bucket["pairs"].append((ms, size))
    return out


def _shape(bucket):
    """The three numbers, or None when nothing was timed."""
    if not bucket["ms"]:
        return None
    return {"timed": len(bucket["ms"]),
            "fastest": min(bucket["ms"]),
            "median": _middle(bucket["ms"]),
            "slowest": max(bucket["ms"])}


def cost(entries=None, directory=None):
    """
    {measured, operations, untimed, per_element, earlier, recent} - or a
    refusal. Nothing is timed here; the trail is read back.
    """
    if entries is None:
        entries, _ = read(directory)
    entries = list(entries or [])
    if not entries:
        return {"measured": False, "refused": "NOTHING_RECORDED",
                "why": "the audit trail has no entries%s. A cost report over "
                       "no runs is not a fast system."
                       % ("" if directory is None
                          else " under %s" % directory)}

    buckets = _per_op(entries)
    if not any(bucket["ms"] for bucket in buckets.values()):
        return {"measured": False, "refused": "NOTHING_TIMED",
                "why": "%d %s, and not one carries a duration. Every "
                       "number in this report would be made up."
                       % (len(entries),
                          "entry" if len(entries) == 1 else "entries")}

    half = len(entries) // 2
    early, late = _per_op(entries[:half]), _per_op(entries[half:])

    operations, untimed, per_element = [], [], []
    for op in sorted(buckets):
        bucket = buckets[op]
        shape = _shape(bucket)
        if shape is None:
            untimed.append({"op": op, "runs": bucket["runs"],
                            "why": "every run of this operation was recorded "
                                   "without a duration. That is not a run "
                                   "that took no time"})
            continue
        card = {"op": op, "runs": bucket["runs"]}
        card.update(shape)
        card["untimed"] = bucket["runs"] - shape["timed"]
        operations.append(card)

        if bucket["pairs"]:
            work = sum(size for _, size in bucket["pairs"])
            spent = sum(ms for ms, _ in bucket["pairs"])
            per_element.append({
                "op": op, "runs": len(bucket["pairs"]),
                "elements": work, "ms": spent,
                "msPerThousand": int(round(spent * 1000.0 / work))
                if work else None})

    missing = (sum(card["runs"] for card in untimed)
               + sum(card["untimed"] for card in operations))

    def window(where):
        out = []
        for op in sorted(where):
            shape = _shape(where[op])
            if shape is None:
                continue
            card = {"op": op}
            card.update(shape)
            out.append(card)
        return out

    return {
        "measured": True,
        "of": len(entries),
        "operations": operations,
        "untimed": untimed,
        "per_element": per_element,
        "earlier": window(early),
        "recent": window(late),
        "why": "%d entries over %d operation%s: %d timed, %d with no "
               "duration recorded, %d with a size to divide by."
               % (len(entries), len(buckets),
                  "" if len(buckets) == 1 else "s",
                  sum(card["timed"] for card in operations),
                  missing, len(per_element)),
        "unjudged": [
            "NOTHING WAS TIMED HERE. The dispatcher put the `ms` on every "
            "job when it ran it; this is that trail read back through "
            "HERON-AHR-GAP-001's own reader. A second clock on this side "
            "would be timing the report.",
            ("%d %s no duration and %s left out of every number "
             "rather than counted as zero - a measurement nobody took is "
             "not a fast one."
             % (missing, "run carries" if missing == 1 else "runs carry",
                "was" if missing == 1 else "were")
             if missing else
             "every run carried a duration, so nothing had to be left out."),
            "WHETHER IT IS GETTING SLOWER. `earlier` and `recent` are the "
            "two halves of the trail, side by side, and no verdict is "
            "drawn - calling a change a regression needs a threshold, and "
            "every candidate for one is a number nobody chose.",
            ("COST PER ELEMENT IS ONLY WHERE A SIZE WAS RECORDED - %d of %d "
             "operations. HeronAudit writes a count for some jobs and not "
             "others, so the rest have a duration and nothing to divide it "
             "by." % (len(per_element), len(operations))
             if per_element else
             "NO OPERATION RECORDED BOTH A DURATION AND A SIZE, so cost per "
             "element could not be computed for any of them."),
            "THE BRIDGE'S OWN TIMEOUT IS NOT STATED HERE. It lives in "
            "mcp/client/heron_config.py and D-48 forbids brain from "
            "importing it, so this agent reports what runs cost and lets a "
            "caller hand in the limit it wants compared (`against`).",
        ],
    }


def against(declared_seconds, op, measured):
    """
    {known, exceeded} for one declared timeout - or a refusal.

    The limit is the caller's, never this agent's. A contract's
    `timeout-seconds` is the usual one, and heron_architect writes it
    admitting it is a default rather than a measurement.
    """
    if not isinstance(declared_seconds, int) or isinstance(declared_seconds, bool) \
            or declared_seconds <= 0:
        return {"known": False, "refused": "NOT_A_TIMEOUT",
                "why": "%r is not a declared timeout. heron_contract requires "
                       "a positive whole number of seconds."
                       % (declared_seconds,)}

    if not measured or not measured.get("measured"):
        return {"known": False, "refused": "NOTHING_RECORDED",
                "why": "there is no measurement to compare against. Hand in "
                       "a `cost` answer that measured something."}

    limit = declared_seconds * 1000
    for card in measured["operations"]:
        if card["op"] != op:
            continue
        return {
            "known": True, "op": op,
            "declaredMs": limit, "slowest": card["slowest"],
            "runs": card["timed"],
            "exceeded": card["slowest"] > limit,
            "why": ("%s has run past its declared %ds limit - slowest %dms "
                    "of %d timed runs."
                    % (op, declared_seconds, card["slowest"], card["timed"])
                    if card["slowest"] > limit else
                    "%s has never reached its declared %ds limit - slowest "
                    "%dms of %d timed runs. That is not the limit proved "
                    "right; it is a limit nothing has tested."
                    % (op, declared_seconds, card["slowest"], card["timed"])),
        }

    return {"known": False, "op": op, "declaredMs": limit,
            "why": "nothing named %r was timed in this trail, so the %ds "
                   "limit is still what heron_architect calls it: a default "
                   "and not a measurement." % (op, declared_seconds)}


def main(argv):
    print("REVIT PERFORMANCE   what the work costs, from runs that happened")
    print("=" * 72)
    print("\nbound, not reimplemented")
    for name, thing in (("read", read), ("duration", duration),
                        ("audit_dir", audit_dir)):
        print("  %-10s %s.%s" % (name, thing.__module__, thing.__name__))

    where = argv[0] if argv else None
    entries, skipped = read(where)
    made = None
    if not entries:
        # No trail on this machine. A demo over nothing would print a
        # refusal and teach nothing, so a small one is written and the
        # refusals are reached separately below.
        made = [
            {"at": "2026-09-14T09:00:00Z", "op": "count_elements",
             "ok": True, "ms": 120, "candidates": 4000},
            {"at": "2026-09-14T09:01:00Z", "op": "count_elements",
             "ok": True, "ms": 180, "candidates": 6000},
            {"at": "2026-09-14T09:02:00Z", "op": "select_by_category",
             "ok": True, "ms": 40},
            {"at": "2026-09-14T09:03:00Z", "op": "release", "ok": True},
            {"at": "2026-09-15T09:04:00Z", "op": "count_elements",
             "ok": True, "ms": 900, "candidates": 4100},
            {"at": "2026-09-15T09:05:00Z", "op": "select_by_category",
             "ok": True, "ms": 55},
        ]
        entries = made
        print("\nno trail on this machine - a made-up one, clearly labelled")

    answer = cost(entries)
    print("\n%s" % answer["why"])
    print("\n%-22s %6s %8s %8s %8s" % ("operation", "runs", "fastest",
                                       "median", "slowest"))
    for card in answer["operations"]:
        print("  %-20s %6d %7dms %7dms %7dms"
              % (card["op"], card["runs"], card["fastest"], card["median"],
                 card["slowest"]))
    for card in answer["untimed"]:
        print("  %-20s %6d   no duration recorded" % (card["op"],
                                                      card["runs"]))
    for card in answer["per_element"]:
        print("  PER SIZE %-12s %8d elements   %dms per 1000"
              % (card["op"], card["elements"], card["msPerThousand"]))

    print("\nearlier half vs recent half - the numbers, not a verdict")
    recent = dict((card["op"], card) for card in answer["recent"])
    for card in answer["earlier"]:
        now = recent.get(card["op"])
        print("  %-20s median %dms -> %s"
              % (card["op"], card["median"],
                 "%dms" % now["median"] if now else "not run since"))

    print("\nagainst a declared timeout (the caller's number, not this "
          "agent's)")
    for seconds, op in ((60, "count_elements"), (1, "count_elements"),
                        (60, "move_elements")):
        said = against(seconds, op, answer)
        print("  %-18s %s" % ("%ds %s" % (seconds, op), said["why"][:52]))

    print("\nrefused")
    for these, limit in (([], None), ([{"op": "x", "ok": True}], None)):
        bad = cost(these)
        print("  %-18s %s" % (bad["refused"], bad["why"][:46]))
    for bad_limit in (0, "sixty", None):
        said = against(bad_limit, "count_elements", answer)
        print("  %-18s %s" % (said["refused"], said["why"][:46]))
    said = against(60, "count_elements", {"measured": False})
    print("  %-18s %s" % (said["refused"], said["why"][:46]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
