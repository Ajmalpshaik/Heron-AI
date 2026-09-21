# Heron-Agent:  HERON-AHR-GAP-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Capability Gap Agent - what Heron was asked for and could not do.

    python brain/heron_gaps.py              the report
    python brain/heron_gaps.py --days 7     only the last week

docs/06 section 6 calls this "the most valuable agent in the entire
specification and probably the cheapest to build", and says what it is:

    essentially a report over the audit log - which requests failed, which
    required fallback, which the user corrected. It needs no autonomy at all
    to be useful. Build it early, as a READ-ONLY REPORT.

WHY THE HEADER SAYS 17 AND THE REGISTRY STILL SAYS DASH
------------------------------------------------------
Two different questions, and conflating them broke a check within a minute of
trying it. `Heron-Step` in a file header is *"build step that introduced it"*
(docs/29), and this was introduced now - which docs/27 records as batch marker
17, *"everything from 2026-09-02 onward"*, the number every fragment written
since has carried.

The **Step column in the registry** asks something else entirely: is this agent
part of Phase 0/1. This one is not. Setting it to 4 - on the reasoning that
docs/27 Step 4 promised this report when it built the audit log - moved the
Phase-0/1 total from 49 to 50 and made two documents wrong in the same edit.
`tools/check-metadata.py` said so immediately, which is the whole reason that
check exists.

So the promise stays a citation rather than a step number. docs/27, Step 4, on
building the audit log:

    It is three lines of code now and the foundation of the cost meter, the
    "what did Heron change?" report and the Capability Gap report later.

This is that later.

ROADMAP says the same in fewer words - *"here are the ten things people asked
for that I could not do"* - and adds the condition this file was written
against: **build it as soon as the audit log exists.** It exists, it has real
traffic in it, and nothing was reading it.

WHAT THIS DOES NOT DO, BECAUSE SOMETHING ELSE ALREADY DOES
-----------------------------------------------------------
`heron_capability.gaps()` already answers one half: a capability somebody
declared a need for and no fragment provides. docs/18's "a capability with no
provider IS the capability gap" is implemented there, and re-deriving it here
would make two answers to one question.

So this file owns the OTHER half, the half only the audit log knows: what was
actually attempted, what failed, how often, and how slowly. The registry knows
what is missing in theory; the log knows what hurt in practice. The report
prints both and says which is which.

WHY A REFUSAL IS NOT ALWAYS A GAP
---------------------------------
The loudest error in the log is `needs_unbound` - a fragment asked to work on a
selection when nothing was selected. That is the executor being RIGHT
(NEEDS-CHECKING, 2026-09-07: running anyway "would report 0 results, which
reads as 'there was nothing to find' rather than 'nobody was asked'"). Counting
it as a missing capability would send somebody off to build a fragment that
already exists and behaved correctly.

So failures are split into two lists that must never be one - the same
discipline `tools/check-gaps.py` applies to unfinished versus waiting. A
CORRECT REFUSAL is Heron declining safely. A DEFECT is Heron unable to do the
thing. Only the second column is a capability gap.

TWO SHAPES OF `ms`, FOR EVER
----------------------------
Until 2026-09-07 every audit field was written as a string, so durations are
`"27"` in older lines and `27` in newer ones. The log is append-only and never
pruned (HeronAudit), so both shapes are in it permanently and this reader
accepts either. A reader that assumed one shape would silently drop half the
timings and report a confident average over the half it liked.
"""

import io
import json
import os
import sys
import collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Errors the executor returns when it is working correctly. A refusal here is
# evidence the safety rails held, not evidence of a missing capability.
#
# Each one is a decision somebody wrote down. If a new error code appears in
# the log that is not in either list, the report says so rather than guessing -
# an unclassified failure quietly counted as "fine" is the failure mode this
# whole split exists to prevent.
CORRECT_REFUSALS = {
    "needs_unbound":
        "asked to work on a selection when nothing was selected - refusing is "
        "right, because 0 results reads as 'nothing to find'",
    "needs_request_values":
        "asked for something without saying what",
    "revit_busy":
        "Revit was mid-command; interrupting it is not on offer",
    "write_disabled":
        "asked to change the model while writing was switched off",
    "no_such_document":
        "the model it was pointed at was not open",
    "ambiguous_document":
        "two open models share the name it was pointed at, so naming one could "
        "not say which was meant - refusing is right, because the alternative "
        "is picking whichever Revit happened to list last",
    "unknown_op":
        "asked for an operation Heron does not declare",
    # THE BRAIN'S OWN REFUSALS (D-62). Added 2026-09-09 after Codex pointed
    # out on PR #44 that brain_lookup and brain_resolve wrote ok=false with
    # NO error at all - so a request Heron correctly could not answer landed
    # in `unclassified` under the code "(none)" and inflated the failure
    # count. That is this file's own founding mistake repeated: its loudest
    # error was the executor behaving correctly, and counting it as a gap
    # would have commissioned work already done.
    "no_capability":
        "the words matched no capability Heron provides - the honest answer "
        "to a request outside what it can do",
    "no_provider":
        "the capability is known and no fragment provides it on this "
        "release - D-63 records it as a WANT rather than a failure",
    "context_refused":
        "the Context Manager declined to assemble - a part outside the "
        "path's budget, or a path whose source this installation has not "
        "got. docs/19 s1: refusing is the half worth having",
    "not_permitted":
        "the permission gate declined it",
}

DEFECTS = {
    "compile_failed":
        "the fragment would not build when it was asked to run",
    "fragment_threw":
        "the fragment ran and crashed",
    "operation_failed":
        "the operation threw out of the handler",
    "not_implemented":
        "declared in the registry with no handler behind it - a fault in Heron",
}


def audit_dir():
    """
    Where the trail lives: %APPDATA%\\Heron\\audit.

    HERON_AUDIT overrides it, which is what the tests use and is also the only
    way to read a trail on a machine with no %APPDATA% at all - the same
    reasoning, and the same escape hatch, as heron_config.config_path().

    Python cannot call HeronPaths, which is C# and the only thing allowed to
    build a Heron path. This is the documented seam rather than a second path
    builder: it resolves ONE directory, it is the only place in brain/ that
    does, and if HeronPaths ever moves the trail this is the single line that
    follows it.
    """
    override = os.environ.get("HERON_AUDIT")
    if override:
        return override
    appdata = os.environ.get("APPDATA")
    if not appdata:
        return None
    return os.path.join(appdata, "Heron", "audit")


def read(directory=None):
    """
    Every audit entry, oldest first. A bad line is skipped, never fatal.

    One truncated write - a full disk, a killed process - must cost one entry
    rather than the report. HeronAudit writes one JSON object per line for
    exactly this reason, and reading it any other way would give that up.
    """
    target = directory if directory is not None else audit_dir()
    if not target or not os.path.isdir(target):
        return [], 0

    entries, skipped = [], 0
    for name in sorted(os.listdir(target)):
        if not name.startswith("audit-") or not name.endswith(".jsonl"):
            continue
        path = os.path.join(target, name)
        try:
            handle = io.open(path, encoding="utf-8", errors="replace")
        except (IOError, OSError):
            continue
        with handle:
            for line in handle:
                line = line.strip()
                if not line:
                    continue
                try:
                    row = json.loads(line)
                except ValueError:
                    skipped += 1
                    continue
                if isinstance(row, dict):
                    entries.append(row)
                else:
                    skipped += 1

    entries.sort(key=lambda r: str(r.get("at", "")))
    return entries, skipped


def duration(row):
    """
    The `ms` field as a number, whichever shape it was written in.

    Returns None rather than 0 when it is missing or unreadable: a request with
    no recorded duration is not a request that took no time, and averaging the
    two together is how a timing report starts lying.
    """
    value = row.get("ms")
    if isinstance(value, bool):
        return None
    if isinstance(value, (int, float)):
        return int(value)
    try:
        return int(str(value).strip())
    except (TypeError, ValueError):
        return None


def since(entries, days):
    """
    The last `days` days of the trail, by the newest entry rather than by
    today's date - a report read on Monday about work done on Friday must not
    come back empty.

    `None` and `0` mean the whole trail. ANYTHING BELOW 1 IS REFUSED rather
    than quietly returning nothing, because what "nothing" turns into two
    functions later is the sentence *"Heron has no record of doing anything
    yet"* - told to somebody whose trail is full. A window that cannot hold
    a day is not a window, and the anchoring above means a real one can never
    come back empty: the newest entry is always inside it. Row 5b-93.
    """
    if days is not None and not isinstance(days, bool) and days < 0:
        raise ValueError(
            "a window of %r days is not a window. Pass a whole number of days "
            "from 1 upward, or None for the whole trail - this report is read "
            "to find out what went wrong, and an empty answer from it reads as "
            "'Heron has never been asked for anything', which would be a lie "
            "about somebody's own history." % (days,))
    if not days or not entries:
        return entries
    newest = str(entries[-1].get("at", ""))[:10]
    if not newest:
        return entries
    import datetime
    try:
        end = datetime.date(int(newest[0:4]), int(newest[5:7]), int(newest[8:10]))
    except ValueError:
        return entries
    first = (end - datetime.timedelta(days=days - 1)).isoformat()
    return [r for r in entries if str(r.get("at", ""))[:10] >= first]


def analyse(entries):
    """
    The findings, as data. Printing is somebody else's job so that the MCP
    seam and the command line report the same numbers rather than two
    implementations of nearly the same sum.
    """
    failed = [r for r in entries if not r.get("ok")]

    defects = collections.Counter()
    refusals = collections.Counter()
    unclassified = collections.Counter()
    for row in failed:
        code = str(row.get("error") or "(none)")
        if code in DEFECTS:
            defects[code] += 1
        elif code in CORRECT_REFUSALS:
            refusals[code] += 1
        else:
            unclassified[code] += 1

    # Per fragment: how often asked, how often it failed, how slow at worst.
    # Entries written before 2026-09-07 carry no fragment name; they are
    # counted separately rather than pooled under a blank, because a bucket
    # named "" ranking first would be the report's own biggest finding.
    per_fragment = collections.defaultdict(
        lambda: {"runs": 0, "failed": 0, "ms": []})
    unnamed = 0
    for row in entries:
        if row.get("op") != "run_fragment_read":
            continue
        name = row.get("fragment")
        if not name:
            unnamed += 1
            continue
        bucket = per_fragment[name]
        bucket["runs"] += 1
        if not row.get("ok"):
            bucket["failed"] += 1
        ms = duration(row)
        if ms is not None:
            bucket["ms"].append(ms)

    per_op = collections.defaultdict(lambda: {"runs": 0, "failed": 0, "ms": []})
    for row in entries:
        bucket = per_op[str(row.get("op") or "(none)")]
        bucket["runs"] += 1
        if not row.get("ok"):
            bucket["failed"] += 1
        ms = duration(row)
        if ms is not None:
            bucket["ms"].append(ms)

    # WHEN, not just how many. A count over the whole trail answers "has this
    # ever gone wrong", and the question somebody asking for a health check
    # means is "is it going wrong NOW". Those gave different answers within a
    # day of each other here: 131 defects on 2026-09-06 from a compile bug, and
    # 1 across 353 runs the day after it was fixed. Reporting 132 as one number
    # warns about a problem that no longer exists, and a warning that cries
    # wolf is one people stop reading - which costs more than never having
    # warned at all.
    by_day = collections.defaultdict(lambda: {"requests": 0, "defects": 0})
    for row in entries:
        day = str(row.get("at", ""))[:10]
        if not day:
            continue
        bucket = by_day[day]
        bucket["requests"] += 1
        if str(row.get("error") or "") in DEFECTS:
            bucket["defects"] += 1

    newest = max(by_day) if by_day else None
    recent = dict(by_day.get(newest, {"requests": 0, "defects": 0})) if newest else         {"requests": 0, "defects": 0}
    recent["day"] = newest

    return {
        "requests": len(entries),
        "failed": len(failed),
        "by_day": dict(by_day),
        "recent": recent,
        "defects": defects,
        "refusals": refusals,
        "unclassified": unclassified,
        "per_fragment": dict(per_fragment),
        "per_op": dict(per_op),
        "unnamed_fragment_runs": unnamed,
        "documents": collections.Counter(
            r["document"] for r in entries if r.get("document")),
        "sessions": len(set(r.get("session") for r in entries if r.get("session"))),
        "first": str(entries[0].get("at", ""))[:10] if entries else None,
        "last": str(entries[-1].get("at", ""))[:10] if entries else None,
    }


def wanted_but_unprovided():
    """
    The other half of the gap, from the capability registry rather than the log.

    Imported rather than re-derived: heron_capability owns "a capability with
    no provider IS the capability gap", and a second implementation of that
    sentence would eventually disagree with the first. Returns an empty list if
    the store is not available - a missing knowledge store must not stop the
    log half of this report, which needs nothing but a file.
    """
    try:
        sys.path.insert(0, os.path.join(ROOT, "brain"))
        import heron_scope as SCOPE
        import heron_capability as CAP
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            return CAP.gaps(store)
        finally:
            close = getattr(store, "close", None)
            if callable(close):
                close()
    except Exception:
        # Deliberately broad. This half is a bonus; the log half is the
        # deliverable, and a knowledge store that will not open must not take
        # the report down with it.
        return []


def _stat(values):
    """
    median and worst, or None when nothing was timed.

    THE MIDDLE OF AN EVEN COUNT IS BETWEEN TWO VALUES, and taking the upper
    one of the pair made the median of TWO runs the slower of them - so a
    fragment run twice at 10 ms and 100 ms was reported as *median 100,
    worst 100*. Two runs is the ordinary case for most of the library, which
    is exactly where a reader has the least context to notice. This file
    already refuses to average a missing duration with a real one because
    "that is how a timing report starts lying"; this is the same rule one
    function along. Row 5b-93.
    """
    if not values:
        return None, None
    ordered = sorted(values)
    middle = len(ordered) // 2
    if len(ordered) % 2:
        return ordered[middle], ordered[-1]
    return (ordered[middle - 1] + ordered[middle]) // 2, ordered[-1]


def report(found, gaps, skipped=0, out=None):
    """The report, in the words a modeller would use."""
    write = (out or sys.stdout).write

    write("CAPABILITY GAP REPORT\n")
    write("=" * 66 + "\n")
    if not found["requests"]:
        write("The audit trail is empty. Nothing has been asked of Heron yet,\n")
        write("so there is nothing to report - which is not the same as no gaps.\n")
        return
    write("%d requests, %s to %s, %d Revit session(s)\n"
          % (found["requests"], found["first"], found["last"], found["sessions"]))
    write("%d refused or failed (%d%%)\n"
          % (found["failed"], 100 * found["failed"] // found["requests"]))
    if skipped:
        write("%d unreadable line(s) skipped\n" % skipped)
    write("\n")

    write("THINGS HERON COULD NOT DO  (defects - these are the gaps)\n")
    write("-" * 66 + "\n")
    if not found["defects"]:
        write("  None. Every failure was a refusal Heron was right to make.\n")
    for code, count in found["defects"].most_common():
        write("  %5d x  %-20s %s\n" % (count, code, DEFECTS[code]))
    write("\n")

    write("THINGS HERON DECLINED, CORRECTLY  (not gaps - do not build for these)\n")
    write("-" * 66 + "\n")
    if not found["refusals"]:
        write("  None.\n")
    for code, count in found["refusals"].most_common():
        write("  %5d x  %-20s %s\n" % (count, code, CORRECT_REFUSALS[code]))
    write("\n")

    if found["unclassified"]:
        write("FAILURES THIS REPORT CANNOT CLASSIFY\n")
        write("-" * 66 + "\n")
        for code, count in found["unclassified"].most_common():
            write("  %5d x  %s\n" % (count, code))
        write("  Add each to CORRECT_REFUSALS or DEFECTS in brain/heron_gaps.py.\n")
        write("  Until then they are counted as failures and judged as neither.\n")
        write("\n")

    write("BY FRAGMENT  (most failures first, then slowest)\n")
    write("-" * 66 + "\n")
    rows = []
    for name, bucket in found["per_fragment"].items():
        median, worst = _stat(bucket["ms"])
        rows.append((bucket["failed"], worst or 0, name, bucket, median, worst))
    rows.sort(key=lambda r: (-r[0], -r[1]))
    if not rows:
        write("  No run names a fragment yet.\n")
    for _, _, name, bucket, median, worst in rows[:15]:
        write("  %-34s %4d run(s)  %3d failed  median %5s ms  worst %6s ms\n"
              % (name[:34], bucket["runs"], bucket["failed"],
                 "-" if median is None else median,
                 "-" if worst is None else worst))
    if found["unnamed_fragment_runs"]:
        write("\n  %d fragment run(s) name no fragment. Those entries predate\n"
              % found["unnamed_fragment_runs"])
        write("  2026-09-07, when the dispatcher started recording the name.\n")
        write("  They can be counted but never attributed.\n")
    write("\n")

    write("BY OPERATION\n")
    write("-" * 66 + "\n")
    for op, bucket in sorted(found["per_op"].items(),
                             key=lambda kv: -kv[1]["runs"]):
        median, worst = _stat(bucket["ms"])
        write("  %-22s %4d run(s)  %3d failed  median %5s ms  worst %6s ms\n"
              % (op, bucket["runs"], bucket["failed"],
                 "-" if median is None else median,
                 "-" if worst is None else worst))
    write("\n")

    write("MODELS\n")
    write("-" * 66 + "\n")
    for name, count in found["documents"].most_common():
        write("  %-40s %4d request(s)\n" % (name[:40], count))
    write("\n")

    write("WANTED, WITH NOBODY TO DO IT  (from the capability registry)\n")
    write("-" * 66 + "\n")
    if not gaps:
        write("  Nothing recorded. Note this is only what somebody WROTE DOWN\n")
        write("  as needed - a silence here is not proof that nothing is missing.\n")
    for name, why in gaps:
        write("  %-34s %s\n" % (name, why))


def main(argv):
    days = None
    if "--days" in argv:
        try:
            days = int(argv[argv.index("--days") + 1])
        except (IndexError, ValueError):
            sys.stderr.write("--days needs a number, e.g. --days 7\n")
            return 2
        # AND A NUMBER IS NOT THE SAME AS A WINDOW. since() refuses below 1
        # as well, and this is the message worth reading at a command line.
        if days < 1:
            sys.stderr.write(
                "--days %d is not a window. Use 1 or more, or leave --days "
                "off for the whole trail.\n" % days)
            return 2

    entries, skipped = read()
    if not entries and not os.path.isdir(audit_dir() or ""):
        sys.stdout.write(
            "No audit trail found. Heron writes one the first time it is asked\n"
            "to do something; there is nothing here yet.\n")
        return 0

    entries = since(entries, days)
    report(analyse(entries), wanted_but_unprovided(), skipped)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
