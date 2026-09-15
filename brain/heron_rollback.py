# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-REVIT-RBK-036
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Session rollback - the panic button, and the one thing Revit will not let
it do.

    python brain/heron_rollback.py

WHAT IT IS FOR (docs/28, HERON-REVIT-RBK-036)
-----------------------------------------------
"The panic button. Reverses EVERYTHING Heron did this session, newest
first, using the audit log's transaction groups. Distinct from
Transaction Safety, which owns one operation." T1, risk MODIFY.

REVIT HAS NO API TO UNDO A NAMED TRANSACTION GROUP
----------------------------------------------------
This was measured, not assumed. `PostableCommand.Undo` exists and
compiles on Revit 2020, 2024 and 2027, so Heron CAN post an undo. What
it cannot do is choose which one: an undo takes the TOP of the stack,
and Revit exposes no way to name a group and reverse that one.

Everything below follows from that single fact.

WHAT A ROLLBACK ACTUALLY IS, THEN
-----------------------------------
N undos, newest first, where N is how many undo entries Heron made. And
the undo stack is not Heron's - it is the document's, shared with the
person modelling in it. If they moved a wall after Heron's last change,
that wall sits ON TOP of Heron's entry, and undoing back to Heron means
undoing their wall first.

THE TRAIL CANNOT SEE THE USER'S OWN WORK AT ALL. Nothing Heron writes
records what a person did between two Heron operations, so this agent
CANNOT say how many of those N undos are Heron's. It says how many
entries Heron made, in order, and it says plainly that anything done in
between goes first and is not counted.

That is why the answer is a PLAN and not a button. A count that quietly
ignored the user's own edits would be the most dangerous number in this
project.

AN UNDO ENTRY IS THE EVIDENCE, NOT A LIST OF OPERATIONS
---------------------------------------------------------
A row counts as reversible because it CARRIES an `undoEntry` - the
TransactionGroup name RevitWrite gave it. That is a fact recorded at
the time by the code that made the change, not a list of write
operations kept here and kept in step by hand.

A write that failed rolled itself back when it happened (Golden Rule
16, one TransactionGroup) and left no entry. Those rows are reported
separately rather than dropped, because "nothing to reverse" and
"nothing happened" read the same in a count and mean different things.

ONE DOCUMENT AT A TIME
------------------------
An undo stack belongs to a document. Heron can have written to two, and
the entries interleave in the trail - so the plan is grouped by
document and each group is its own sequence of undos. A single ordered
list across documents would be a plan nobody can carry out.

IT REVERSES NOTHING
---------------------
The register gives this row MODIFY and this file is the planner. Posting
the undos has to happen inside Revit, in an API context, and that half
does not exist yet - PROPOSALS F29.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_gaps as GAPS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# HERON-AHR-GAP-001's reader, bound rather than copied. One place resolves
# the trail and one place reads it.
read = GAPS.read
audit_dir = GAPS.audit_dir

# The field RevitWrite records the TransactionGroup name in. A row carrying
# one made an undo entry; a row without one did not. This is the whole
# test, and it is a fact from the trail rather than a list of write
# operations that would have to be kept in step by hand.
UNDO_ENTRY = "undoEntry"

# What identifies the document an undo stack belongs to, and what to call
# it for a person. Both are recorded by the same write.
DOCUMENT_ID = "documentId"
DOCUMENT = "document"

# Measured on 2026-09-15 by compiling it: this exists on 2020, 2024 and
# 2027. It is what the missing half would post, and it takes the top of
# the stack - there is no named variant.
THE_ONLY_UNDO = "PostableCommand.Undo"


def plan(entries=None, directory=None, session=None):
    """
    {planned, documents, unreversible, failed} - or a refusal. Nothing is
    reversed and nothing is posted.
    """
    if entries is None:
        entries, _ = read(directory)
    entries = list(entries or [])
    if not entries:
        return {"planned": False, "refused": "NOTHING_RECORDED",
                "why": "the audit trail has no entries%s. Nothing to "
                       "reverse is not the same as nothing having "
                       "happened - a trail that was never written says "
                       "nothing either way."
                       % ("" if directory is None
                          else " under %s" % directory)}

    if session:
        entries = [row for row in entries
                   if str(row.get("workflow") or "") == str(session)]
        if not entries:
            return {"planned": False, "refused": "NO_SUCH_SESSION",
                    "why": "no entry carries workflow %r. An empty answer "
                           "for a session id nobody used would read as "
                           "`nothing to undo`." % (session,)}

    documents, unreversible, failed = {}, [], []
    for row in entries:
        entry = str(row.get(UNDO_ENTRY) or "").strip()
        if not entry:
            if row.get("ok") is False:
                failed.append({
                    "at": row.get("at"), "op": row.get("op"),
                    "why": "it failed, and Golden Rule 16 means it rolled "
                           "itself back when it happened. There is nothing "
                           "to reverse, which is not the same as nothing "
                           "having been attempted"})
            continue

        if row.get("ok") is False:
            # An entry name AND a failure. RevitWrite rolls back on
            # failure, so this should not happen - and if it does, guessing
            # whether the group survived is the one guess that could leave
            # a model half-changed.
            unreversible.append({
                "at": row.get("at"), "op": row.get("op"), "entry": entry,
                "document": row.get(DOCUMENT),
                "why": "it recorded an undo entry AND a failure. Whether "
                       "the group survived is not something this trail "
                       "says, and assuming either way could leave a model "
                       "half-changed"})
            continue

        key = str(row.get(DOCUMENT_ID) or row.get(DOCUMENT) or "")
        if not key:
            unreversible.append({
                "at": row.get("at"), "op": row.get("op"), "entry": entry,
                "document": None,
                "why": "no document is recorded, and an undo stack belongs "
                       "to a document. There is nowhere to post this one"})
            continue

        group = documents.setdefault(key, {
            "document": row.get(DOCUMENT) or key,
            "documentId": row.get(DOCUMENT_ID), "undos": []})
        group["undos"].append({
            "at": row.get("at"), "op": row.get("op"), "entry": entry,
            "workflow": row.get("workflow") or None,
            "elements": row.get("elements") or None})

    out = []
    for key in sorted(documents):
        group = documents[key]
        # NEWEST FIRST. The trail is oldest first and an undo stack is the
        # other way round, so this is reversed rather than sorted again -
        # two rows written in the same millisecond keep the order they
        # were recorded in.
        group["undos"] = list(reversed(group["undos"]))
        group["of"] = len(group["undos"])
        group["posts"] = "%d x %s" % (len(group["undos"]), THE_ONLY_UNDO)
        out.append(group)

    total = sum(group["of"] for group in out)
    return {
        "planned": True,
        "of": len(entries),
        "documents": out,
        "undos": total,
        "unreversible": unreversible,
        "failed": failed,
        "reversed": False,
        "why": "%d entr%s: %d undo entr%s Heron made across %d document%s, "
               "%d that cannot be reversed, %d that failed and rolled %s "
               "back. Nothing was reversed."
               % (len(entries), "y" if len(entries) == 1 else "ies",
                  total, "y" if total == 1 else "ies", len(out),
                  "" if len(out) == 1 else "s",
                  len(unreversible), len(failed),
                  "itself" if len(failed) == 1 else "themselves"),
        "unjudged": [
            "HOW MANY UNDOS IT REALLY TAKES. %s takes the TOP of the stack "
            "and Revit exposes no way to name a group and reverse that one "
            "- measured by compiling it on 2020, 2024 and 2027. The stack "
            "is the document's, shared with the person modelling in it, "
            "and NOTHING HERON RECORDS SEES THEIR OWN EDITS. If they moved "
            "a wall after Heron's last change, that wall is undone first "
            "and is not in any count here." % THE_ONLY_UNDO,
            "THE PLAN IS PER DOCUMENT, because an undo stack is. Heron can "
            "have written to two and the entries interleave in the trail; "
            "one ordered list across documents would be a plan nobody can "
            "carry out.",
            ("%d ENTR%s CANNOT BE REVERSED and %s named rather than "
             "dropped: a recorded undo entry beside a failure, or a write "
             "with no document. Guessing either way could leave a model "
             "half-changed."
             % (len(unreversible), "Y" if len(unreversible) == 1 else "IES",
                "is" if len(unreversible) == 1 else "are")
             if unreversible else
             "every undo entry named a document and a success, so none was "
             "left unreversible."),
            ("%d WRITE%s FAILED AND ROLLED %s BACK AT THE TIME (Golden Rule "
             "16, one TransactionGroup). There is nothing to reverse, which "
             "is not the same as nothing having been attempted."
             % (len(failed), "" if len(failed) == 1 else "S",
                "itself" if len(failed) == 1 else "themselves")
             if failed else
             "no recorded write failed, so nothing rolled itself back."),
            "WHETHER THE DOCUMENT IS STILL OPEN. A model saved and closed "
            "since has no undo stack left, and nothing in the trail says "
            "whether it is open. That is checked in Revit or not at all.",
            "NOTHING WAS REVERSED AND NOTHING WAS POSTED. This is the "
            "planner; posting the undos has to happen inside Revit in an "
            "API context, and that half does not exist yet (PROPOSALS "
            "F29).",
        ],
    }


def main(argv):
    print("SESSION ROLLBACK   the panic button, and what Revit will not let "
          "it do")
    print("=" * 72)
    print("\nthe only undo Revit offers: %s - it takes the TOP of the stack"
          % THE_ONLY_UNDO)
    print("bound, not reimplemented: %s.%s" % (read.__module__, read.__name__))

    entries, _ = read(argv[0] if argv else None)
    if not entries:
        print("\nno trail on this machine - a made-up one, clearly labelled")
        entries = [
            {"at": "2026-09-15T09:00:00Z", "op": "count_elements",
             "ok": True, "workflow": "wf-1"},
            {"at": "2026-09-15T09:01:00Z", "op": "move_elements", "ok": True,
             "workflow": "wf-2", "undoEntry": "Heron: move 12 ducts up 200mm",
             "document": "Tower B MEP", "documentId": "doc-b",
             "elements": "a,b,c"},
            {"at": "2026-09-15T09:02:00Z", "op": "move_elements", "ok": False,
             "workflow": "wf-3", "document": "Tower B MEP",
             "documentId": "doc-b"},
            {"at": "2026-09-15T09:03:00Z", "op": "move_elements", "ok": True,
             "workflow": "wf-4", "undoEntry": "Heron: move 3 pipes up 50mm",
             "document": "Tower A", "documentId": "doc-a"},
            {"at": "2026-09-15T09:04:00Z", "op": "move_elements", "ok": True,
             "workflow": "wf-5", "undoEntry": "Heron: move 8 ducts down 100mm",
             "document": "Tower B MEP", "documentId": "doc-b"},
            {"at": "2026-09-15T09:05:00Z", "op": "run_fragment_write",
             "ok": False, "workflow": "wf-6",
             "undoEntry": "Heron: tag 40 sheets", "document": "Tower A",
             "documentId": "doc-a"},
        ]

    answer = plan(entries)
    print("\n%s" % answer["why"])
    for group in answer["documents"]:
        print("\n  %s - %s, newest first"
              % (group["document"], group["posts"]))
        for n, undo in enumerate(group["undos"], 1):
            print("    %d. %-34s %s" % (n, undo["entry"], undo["at"]))
    for card in answer["unreversible"]:
        print("\n  CANNOT REVERSE  %s" % (card["entry"] or card["op"]))
        print("                  %s" % card["why"][:58])
    for card in answer["failed"]:
        print("\n  NOTHING TO DO   %-16s %s" % (card["op"], card["why"][:44]))

    print("\nrefused")
    for these, session in (([], None), (None, None), (entries, "wf-999")):
        bad = plan(these, session=session)
        print("  %-20s %s" % (bad["refused"], bad["why"][:44]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
