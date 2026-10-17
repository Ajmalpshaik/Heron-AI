#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
The short change report `revit_change` prints, and the run records it points to.

WHY THIS EXISTS - FRAGMENT-ISSUES 5b-407. One afternoon's two family builds
carried 322,725 characters of `revit_change` replies back into the chat, about
70% of it saying nothing new: every variable the fragment's script left, and
the same "was KEPT" paragraph, word for word, in 170 of 172 replies. #472 cut
the variables that hold code. This cuts the rest of what repeats:

  * THE CHANGE REPORT. The add-in now counts what Revit says the job changed -
    added, modified and deleted, by category, the fragment's apart from its
    setup steps' (RevitFragment.ChangeWatch, HeronChangeReport) - and the chat
    is given those counts on two lines. Every element id is on disk instead.
  * THE RUN RECORD. One file per fragment write under HeronPaths.Runs, named
    by its workflow id - the same id as its audit line - holding every element
    and the whole reply. `heron_run_record` pages it back from DISK: no Revit,
    no pipe, no Revit thread.
  * ONCE PER BATCH. The add-in writes the KEPT verdict twice - the paragraph,
    and one sentence carrying the undo rule whole (`verdictShort`). The
    paragraph is printed on the first kept change of a batch of calls and the
    sentence on the rest; the undo rule (Article 8) is on every reply. And a
    value the last reply of the same capability showed word for word is
    counted, not printed again - only when a run record holds it.

NOTHING HERE DECIDES WHAT HAPPENED TO THE MODEL. Every sentence about the
outcome is the add-in's - the side that saw whether the group held - and this
only chooses between two of them, or prints Revit's own counts. A reply from
an add-in that predates the change report carries neither, and is printed
exactly as before.

Pure: no Revit, no SDK, no network - tests/test_run_records.py runs all of it
against records it writes for itself.
"""

import io
import json
import os
import re
import threading

import heron_bridge_client as bridge

# HeronRuns.IsRunId's shape, for the same reason: the id the chat types
# becomes a FILE NAME, so a separator, a dot or a drive letter in it would be a
# path out of the folder. Kept in step with platform/Heron.Core/HeronRuns.cs by
# tests/test_run_records.py.
RUN_ID = re.compile(r"^wf-[0-9A-Za-z-]{1,64}$")
EXTENSION = ".json"

# A page of a record, when the caller names no size - and the most one call may
# ask for. A page is what the chat carries for the rest of its turns, so the cap
# is the point: 200 lines is already a long reply.
DEFAULT_LIMIT = 40
MAX_LIMIT = 200

# How many categories a change line names before it counts the rest.
CATEGORIES_SHOWN = 4

# WHAT COUNTS AS ONE BATCH. A host sends the calls of one turn back to back -
# milliseconds apart, or at once - and every new turn costs the model at least
# a second or two to write. So a change that STARTS within this many seconds of
# the previous one ENDING is the same batch. Erring short costs only size: a
# batch split by a slow permission prompt prints the paragraph and the values
# twice, and nothing is lost.
BATCH_GAP_S = 3.0


def is_run_id(run):
    """Whether `run` is a workflow id a record may be named after."""
    return bool(run) and RUN_ID.match(str(run)) is not None


def record_path(run, folder=None):
    """The record's file, or None for anything that is not a run id."""
    if not is_run_id(run):
        return None
    return os.path.join(folder or bridge.RUNS_DIR, run + EXTENSION)


def read(run, folder=None):
    """
    (record, None) for a record on disk, or (None, why) - never raises.

    A missing record is said as missing, never as empty: the add-in prunes
    after runs.retainDays, and "it has gone" and "it held nothing" are
    different answers.
    """
    path = record_path(run, folder)
    if path is None:
        return None, ("'%s' is not a run id. A run id looks like "
                      "wf-20261009-133432-0042-a1b2 - the `record:` line of a "
                      "revit_change reply names it." % (run or ""))
    try:
        with io.open(path, "r", encoding="utf-8") as fh:
            record = json.load(fh)
    except (IOError, OSError):
        return None, ("There is no record for %s on this PC. Records are kept "
                      "for runs.retainDays (14 by default) and then pruned; a "
                      "change made before this Heron had run records has none."
                      % run)
    except ValueError as bad:
        return None, ("The record for %s could not be read: %s. Nothing was "
                      "changed by trying." % (run, bad))
    if not isinstance(record, dict):
        return None, "The record for %s is not a record Heron wrote." % run
    return record, None


def recent(folder=None, limit=10):
    """
    The newest records first, as small dicts - for a call that names no run.

    Newest by the id itself: a workflow id starts with its UTC time, so the
    order is the order the jobs ran, and no file's clock is trusted for it.
    """
    folder = folder or bridge.RUNS_DIR
    try:
        names = [n for n in os.listdir(folder)
                 if n.endswith(EXTENSION) and is_run_id(n[:-len(EXTENSION)])]
    except OSError:
        return []
    found = []
    for name in sorted(names, reverse=True)[:max(1, limit)]:
        run = name[:-len(EXTENSION)]
        record, _ = read(run, folder)
        record = record or {}
        # A file that parses is still read field by field: a record from
        # another version, or a hand-edited one, must list rather than raise.
        reply = record.get("reply") if isinstance(record.get("reply"), dict) else {}
        changes = reply.get("changes") if isinstance(reply.get("changes"), dict) else {}
        found.append({
            "run": run,
            "fragment": record.get("fragment"),
            "document": record.get("document"),
            "at": record.get("at"),
            "kind": reply.get("verdictKind"),
            "total": changes.get("total"),
        })
    return found


def entries(record):
    """
    Every line a record can be paged by, in a fixed order: what the fragment
    left, every element it changed, every warning Revit raised and Heron
    dismissed, and the verdict whole. One line each, so offset and limit mean
    the same thing whatever the record holds.
    """
    reply = record.get("reply") if isinstance(record.get("reply"), dict) else {}
    lines = []

    bound = reply.get("bound")
    if bound:
        lines.append("inputs    %s" % bound)

    provides = reply.get("provides")
    if isinstance(provides, dict):
        for name in sorted(provides):
            lines.append("value     %s  %s" % (name, provides[name]))

    for one in record.get("elements") or []:
        if not isinstance(one, dict):
            continue
        lines.append("%-9s %-8s %s%s" % (one.get("part", "?"), one.get("change", "?"),
                                         one.get("id", "?"),
                                         "  " + one["category"] if one.get("category") else ""))

    for one in reply.get("warningsDismissed") or []:
        if not isinstance(one, dict):
            continue
        lines.append("warning   %s (x%s, %s element(s)%s)"
                     % (one.get("text"), one.get("times"), one.get("elements"),
                        ", setup step " + one["step"] if one.get("step") else ""))

    if reply.get("verdict"):
        lines.append("verdict   %s" % reply["verdict"])
    return lines


def page(record, run, offset=0, limit=DEFAULT_LIMIT):
    """
    One page of a record, as the text the chat is given.

    The header is on every page - which job, which model, when, how it ended,
    what it changed - so a page read on its own still says what it is a page
    of. Offset and limit are clamped rather than refused: a page past the end
    says so and names the last offset that has anything.
    """
    try:
        offset = max(0, int(offset or 0))
    except (TypeError, ValueError):
        offset = 0
    try:
        limit = int(limit or DEFAULT_LIMIT)
    except (TypeError, ValueError):
        limit = DEFAULT_LIMIT
    limit = max(1, min(MAX_LIMIT, limit))

    reply = record.get("reply") if isinstance(record.get("reply"), dict) else {}
    every = entries(record)
    shown = every[offset:offset + limit]

    lines = ["Run %s: %s in %s, %s - %s." % (
        run, record.get("fragment") or reply.get("ran") or "?",
        record.get("document") or reply.get("document") or "?",
        record.get("at") or "?", _ended(reply))]
    lines.extend(change_lines(reply))
    lines.append("")
    if not shown:
        lines.append("Nothing at offset %d - the record has %d line(s)%s."
                     % (offset, len(every),
                        "" if not every else ", the last at offset %d" % (len(every) - 1)))
        return "\n".join(lines)

    lines.append("Lines %d-%d of %d:" % (offset, offset + len(shown) - 1, len(every)))
    lines.extend("  " + line for line in shown)
    if offset + len(shown) < len(every):
        lines.append("")
        lines.append("More: heron_run_record run=%s offset=%d"
                     % (run, offset + len(shown)))
    return "\n".join(lines)


def _ended(reply):
    kind = reply.get("verdictKind")
    if kind == "kept":
        return "KEPT"
    if kind == "rolledBack":
        return "rolled back, nothing kept"
    if kind == "notConfirmed":
        return "ROLLBACK NOT CONFIRMED - the model may still hold it"
    return "outcome not recorded" if reply.get("ok", True) else "refused"


def _clause(rows, field, count, word):
    """'3 added (Generic Models 2, Lines 1)' - or '' when the count is 0."""
    if not count:
        return ""
    named = [(row.get("category"), row.get(field)) for row in rows or []
             if isinstance(row, dict) and row.get(field)]
    shown = ", ".join("%s %s" % (name, n) for name, n in named[:CATEGORIES_SHOWN])
    if len(named) > CATEGORIES_SHOWN:
        shown += ", and %d more categor%s" % (
            len(named) - CATEGORIES_SHOWN, "y" if len(named) - CATEGORIES_SHOWN == 1 else "ies")
    return "%d %s%s" % (count, word, " (%s)" % shown if shown else "")


def _part_line(part):
    if not isinstance(part, dict):
        return ""
    rows = part.get("byCategory")
    said = [c for c in (_clause(rows, "added", part.get("added"), "added"),
                        _clause(rows, "modified", part.get("modified"), "modified"),
                        "%d deleted" % part["deleted"] if part.get("deleted") else "")
            if c]
    return ", ".join(said)


def change_lines(reply):
    """
    What Revit said the job changed, in at most three short lines - or nothing
    for a reply that carries no change report (a read, or an add-in from
    before it).

    COUNTS ARE PRINTED ONLY WHEN REVIT WAS SEEN TO REPORT. A job that saw no
    change event at all says so in those words, never "nothing changed":
    whether Revit raises the event for every commit inside a group is
    NEEDS-CHECKING's to settle, and until it is, silence is not evidence.
    """
    changes = reply.get("changes") if isinstance(reply, dict) else None
    if not isinstance(changes, dict):
        return []

    lines = []
    if not changes.get("witnessed"):
        if changes.get("unreadable"):
            lines.append("  changed: not counted - %s of Revit's change events could "
                         "not be read, so no count here is complete."
                         % changes.get("unreadable"))
        else:
            lines.append("  changed: not counted - Revit reported no change event for "
                         "this job, which is not proof that nothing changed.")
    else:
        fragment = _part_line(changes.get("fragment"))
        setup = _part_line(changes.get("setup"))
        lines.append("  changed: %s" % (fragment or "nothing, by Revit's account"))
        if setup:
            lines.append("  setup:   %s" % setup)

    run = reply.get("run")
    if is_run_id(run):
        lines.append("  record:  heron_run_record run=%s (every id and value)" % run)
    return lines


class Batch(object):
    """
    What this chat has already printed in the current batch of calls - one per
    server process, which is one chat.

    TWO THINGS ARE PRINTED ONCE PER BATCH, AND BOTH ONLY WHERE NOTHING IS LOST:

      THE KEPT PARAGRAPH, when the add-in wrote its one-sentence form itself
      (`verdictShort`). Only a KEPT change is ever shortened: a rollback, one
      Revit did not confirm, and any reply without the field print whole every
      time, and the sentence carries the undo rule unshortened.

      A VALUE THE LAST REPLY OF THE SAME CAPABILITY ALREADY SHOWED, word for
      word - and only when this reply names a run record, which holds every
      value. A fragment's helpers and constants come back identical on every
      call; a value that differs, a refusal above all, is printed as ever, and
      the reply says how many were left out.

    A BATCH ENDS AT A PAUSE. A call that starts more than the gap after the
    last one ended opens a new batch, and the next kept change prints the
    paragraph again and every value whole.
    """

    def __init__(self, gap=BATCH_GAP_S):
        self._gap = gap
        self._last = None
        self._kept = False
        self._values = {}
        self._lock = threading.Lock()

    def turn(self, started, finished):
        """Place one call in the batch: True when it opened a new one."""
        with self._lock:
            fresh = self._last is None or started - self._last > self._gap
            if fresh:
                self._kept = False
                self._values = {}
            self._last = finished
            return fresh

    def whole_kept(self):
        """True the first time a kept change asks in this batch, False after."""
        with self._lock:
            first = not self._kept
            self._kept = True
            return first

    def repeated(self, capability, provides):
        """
        The names whose value is exactly what the last reply of this capability
        in this batch showed - and remember this reply's values for the next.
        """
        if not isinstance(provides, dict):
            return []
        key = str(capability or "").upper()
        with self._lock:
            before = self._values.get(key) or {}
            self._values[key] = dict((str(k), str(v)) for k, v in provides.items())
        return sorted(str(name) for name in provides
                      if str(name) in before and before[str(name)] == str(provides[name]))


def verdict_line(reply, batch):
    """The verdict to print for one reply: the add-in's paragraph, or its sentence."""
    verdict = reply.get("verdict") if isinstance(reply, dict) else None
    if not verdict:
        return None
    brief = reply.get("verdictShort")
    if reply.get("verdictKind") != "kept" or not brief:
        return str(verdict)
    return str(verdict) if batch.whole_kept() else str(brief)


def shown_values(reply, capability, batch):
    """
    (values to print, names left out as unchanged) for one reply.

    Nothing is left out unless the reply names a run record - the record is
    where a value left out here can still be read - and the chat is told on
    one line how many were.
    """
    provides = reply.get("provides") if isinstance(reply, dict) else None
    if not isinstance(provides, dict):
        return provides, []
    same = batch.repeated(capability, provides)
    if not same or not is_run_id(reply.get("run")):
        return provides, []
    left = set(same)
    return dict((k, v) for k, v in provides.items() if str(k) not in left), same


def unchanged_line(same):
    """
    The one line saying how many values were left out as the same as the last
    reply's.

    COUNTED, NOT NAMED. The last reply, printed just above in the same batch,
    names every one of them with its value, and the record holds them all; a
    value that changed is never among them. Naming them again cost about 250
    characters a reply on a family build - most of them a fragment's helpers,
    already listed as not printed one reply up.
    """
    if not same:
        return []
    return ["  and %d value%s unchanged from the last reply - every one is in the record."
            % (len(same), "" if len(same) == 1 else "s")]
