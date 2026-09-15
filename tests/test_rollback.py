# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-REVIT-RBK-036
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Session rollback - newest first, per document, and it reverses nothing.

    python tests/test_rollback.py

WHAT IT PROVES
  1. IT REVERSES NOTHING. `reversed` is false and the module cannot post
     anything - it imports nothing that could reach Revit.

  2. NEWEST FIRST, AND PER DOCUMENT. Two documents interleaved in one
     trail come back as two independent sequences, each in reverse order.

  3. THE EVIDENCE IS THE RECORDED UNDO ENTRY, not a list of write
     operations kept here - a row with an unheard-of op and an undo entry
     is planned; a `move_elements` without one is not.

  4. `undos` IS HERON'S ENTRIES, NOT THE NUMBER OF CTRL+Z. The answer
     says so, because the user's own edits are invisible to the trail.

  5. A FAILURE IS NOT A ROLLBACK, AND NEITHER IS AN UNRECORDED ONE. The
     three cases are told apart and none is dropped.

  6. A SESSION ID NOBODY USED IS REFUSED, never answered with an empty
     plan.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_rollback as RBK                                   # noqa: E402
import heron_gaps as GAPS                                      # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def row(at, op="move_elements", ok=True, entry=None, doc="Tower B",
        doc_id="doc-b", workflow="wf"):
    card = {"at": at, "op": op, "ok": ok, "workflow": workflow}
    if entry:
        card["undoEntry"] = entry
    if doc:
        card["document"] = doc
        card["documentId"] = doc_id
    return card


# Two documents, interleaved, oldest first - the shape a real trail has.
TRAIL = [
    row("2026-09-15T09:00:00Z", op="count_elements"),
    row("2026-09-15T09:01:00Z", entry="A1", doc="Tower A", doc_id="doc-a",
        workflow="wf-1"),
    row("2026-09-15T09:02:00Z", entry="B1", workflow="wf-2"),
    row("2026-09-15T09:03:00Z", entry="A2", doc="Tower A", doc_id="doc-a",
        workflow="wf-3"),
    row("2026-09-15T09:04:00Z", entry="B2", workflow="wf-4"),
]


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_rollback.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. it reverses nothing")
    answer = RBK.plan(TRAIL)
    check(answer["reversed"] is False, "`reversed` is false")
    imports = sorted(set(
        line.split()[1].split(".")[0]
        for line in logic.split("\n")
        if line.startswith("import ") or line.startswith("from ")))
    check(imports == ["heron_gaps", "os", "sys"],
          "it imports only %s - nothing that could reach Revit"
          % ", ".join(imports))
    check(RBK.read is GAPS.read, "RBK.read IS GAPS.read - one reader")

    print("\n2. newest first, and per document")
    check(len(answer["documents"]) == 2,
          "two documents come back as two plans")
    by_doc = dict((group["documentId"], group)
                  for group in answer["documents"])
    check([u["entry"] for u in by_doc["doc-a"]["undos"]] == ["A2", "A1"],
          "Tower A is newest first: %s"
          % ", ".join(u["entry"] for u in by_doc["doc-a"]["undos"]))
    check([u["entry"] for u in by_doc["doc-b"]["undos"]] == ["B2", "B1"],
          "Tower B is newest first: %s"
          % ", ".join(u["entry"] for u in by_doc["doc-b"]["undos"]))
    check(by_doc["doc-a"]["posts"].startswith("2 x"),
          "and each says how many posts it is: %r" % by_doc["doc-a"]["posts"])
    # SAME MILLISECOND. Sorting again would shuffle these; reversing keeps
    # the order they were recorded in.
    tie = RBK.plan([row("2026-09-15T09:00:00.000Z", entry="first"),
                    row("2026-09-15T09:00:00.000Z", entry="second")])
    check([u["entry"] for u in tie["documents"][0]["undos"]]
          == ["second", "first"],
          "two entries at the same instant keep their recorded order, "
          "reversed")

    print("\n3. the evidence is the recorded undo entry")
    odd = RBK.plan([row("2026-09-15T09:00:00Z", op="some_future_write",
                        entry="X1"),
                    row("2026-09-15T09:01:00Z", op="move_elements")])
    check(odd["undos"] == 1,
          "an unheard-of op WITH an undo entry is planned")
    check(odd["documents"][0]["undos"][0]["op"] == "some_future_write",
          "and it is the future one: %s"
          % odd["documents"][0]["undos"][0]["op"])
    check("move_elements" not in logic,
          "the module names no write operation at all - it reads the trail")
    check(RBK.UNDO_ENTRY == "undoEntry",
          "the field it reads is the one RevitWrite records")

    print("\n4. `undos` is Heron's entries, not the number of Ctrl+Z")
    check(answer["undos"] == 4, "four undo entries were made (%d)"
                                % answer["undos"])
    stack = " ".join(answer["unjudged"])
    check("TOP of the stack" in stack,
          "the answer says an undo takes the top of the stack")
    check("their own edits" in stack.lower() or
          "OWN EDITS" in stack,
          "and that the user's own edits are invisible to the trail")
    check("2020, 2024 and 2027" in stack,
          "with the API claim marked as measured, not assumed")

    print("\n5. a failure is not a rollback, and neither is an unrecorded one")
    mixed = RBK.plan([
        row("2026-09-15T09:00:00Z", ok=False),
        row("2026-09-15T09:01:00Z", ok=False, entry="half"),
        row("2026-09-15T09:02:00Z", entry="fine"),
        row("2026-09-15T09:03:00Z", entry="nowhere", doc=None),
    ])
    check(len(mixed["failed"]) == 1,
          "a failure with no entry rolled itself back")
    check(len(mixed["unreversible"]) == 2,
          "an entry beside a failure, and an entry with no document, are "
          "both unreversible (%d)" % len(mixed["unreversible"]))
    check(mixed["undos"] == 1, "leaving exactly one real undo")
    everything = (mixed["undos"] + len(mixed["failed"])
                  + len(mixed["unreversible"]))
    check(everything == 4, "and all four rows are accounted for (%d)"
                           % everything)
    reasons = set(card["why"][:20] for card in mixed["unreversible"])
    check(len(reasons) == 2, "the two unreversible cases give two reasons")

    print("\n6. a session id nobody used is refused")
    one = RBK.plan(TRAIL, session="wf-3")
    check(one["undos"] == 1 and one["documents"][0]["undos"][0]["entry"]
          == "A2", "a real session plans only its own entry")
    said = RBK.plan(TRAIL, session="wf-999")
    reached.add(said.get("refused"))
    check(said.get("refused") == "NO_SUCH_SESSION",
          "and an unused one is REFUSED, not answered with an empty plan")

    print("\n7. it really reads a trail from disk, and every failure is "
          "reached")
    yard = tempfile.mkdtemp(prefix="heron-rollback-")
    try:
        with io.open(os.path.join(yard, "audit-2026-09-15.jsonl"), "w",
                     encoding="utf-8") as handle:
            for card in TRAIL:
                handle.write(json.dumps(card) + "\n")
        from_disk = RBK.plan(directory=yard)
        check(from_disk["undos"] == answer["undos"],
              "%d undos read back off disk" % from_disk["undos"])

        empty = os.path.join(yard, "empty")
        os.makedirs(empty)
        for these, where, name in (([], None, "NOTHING_RECORDED"),
                                   (None, empty, "NOTHING_RECORDED")):
            bad = RBK.plan(these, directory=where)
            reached.add(bad.get("refused"))
            check(bad.get("refused") == name, "%s is reached" % name)
    finally:
        shutil.rmtree(yard, ignore_errors=True)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-REVIT-RBK-036.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 2, "the contract declares 2 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(answer["unjudged"]) == 6, "six things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    newest first, per document, and it reverses nothing")
    return 0


if __name__ == "__main__":
    sys.exit(main())
