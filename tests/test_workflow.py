#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Workflow Engine and checkpoints. Runs without Revit.

The property that matters most is not "stages run in order" - it is:

    A stage whose INPUT changed must NOT return its cached output.

Without that, "continue" silently builds on stale work, which is worse than
restarting from scratch, because it looks like it succeeded. docs/23 lists it as
requirement 2 for a reason.

Second most important: the engine never decides a retry for itself. It asks the
Failure Analysis Agent, so an outcome that is unknown stops the workflow instead
of repeating a move that may already have happened.

    python tests/test_workflow.py
"""

import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

from heron_workflow import CheckpointStore, Workflow, WorkflowStopped   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    root = tempfile.mkdtemp(prefix="heron-wf-")
    store = CheckpointStore(root)
    try:
        print("Stages run, and their output comes back")
        calls = []
        wf = Workflow("wf1", store)
        out = wf.run("preview", lambda: (calls.append("preview"), {"ok": True, "n": 4})[1],
                     inputs={"mm": 200})
        check(out["n"] == 4, "a stage returns what it produced")
        check(calls == ["preview"], "and it actually ran")

        print()
        print("THE PROPERTY: a completed stage is not re-run for the SAME input")
        wf2 = Workflow("wf1", store)
        out2 = wf2.run("preview", lambda: (calls.append("preview"), {"ok": True, "n": 999})[1],
                       inputs={"mm": 200})
        check(out2["n"] == 4, "it returned the CHECKPOINTED output, not a fresh one")
        check(calls == ["preview"], "and the action was never called a second time")

        print()
        print("THE OTHER HALF: a changed input invalidates the checkpoint")
        wf3 = Workflow("wf1", store)
        out3 = wf3.run("preview", lambda: (calls.append("preview-again"), {"ok": True, "n": 7})[1],
                       inputs={"mm": 500})
        check(out3["n"] == 7, "a different input re-runs the stage")
        check("preview-again" in calls,
              "content-addressed: 'continue' must never build on stale work")

        print()
        print("A failure keeps the stages that already finished")
        wf4 = Workflow("wf2", store)
        wf4.run("one", lambda: {"ok": True, "v": 1}, inputs={})
        wf4.run("two", lambda: {"ok": True, "v": 2}, inputs={})
        try:
            wf4.run("three", lambda: {"ok": False, "error": "no_document"}, inputs={})
            check(False, "a failing stage stops the workflow")
        except WorkflowStopped as stop:
            check(stop.stage == "three", "a failing stage stops the workflow, and names itself")

        resumed = Workflow("wf2", store)
        check(resumed.done_stages() == ["one", "two"],
              "the two finished stages survived - that is the whole point of resume")

        print()
        print("A paused workflow is VISIBLE - docs/23 requirement 4")
        check(resumed.waiting_on() is not None,
              "it can say what it is stopped on, so a job cannot silently die")
        check("three" in resumed.waiting_on(), "and it names the stage")
        check("Stopped at" in resumed.describe(), "the description says so in words")

        print()
        print("It NEVER blind-retries - it asks the Failure Analysis Agent")
        tries = []

        def lost_answer():
            tries.append(1)
            return {"ok": False, "error": "unknown_outcome"}

        try:
            Workflow("wf3", store).run("move", lost_answer, inputs={}, writes=True)
            check(False, "a lost answer on a write stops the workflow")
        except WorkflowStopped:
            check(len(tries) == 1,
                  "a lost answer on a WRITE was tried exactly once - never repeated")

        retried = []

        def not_ready():
            retried.append(1)
            return {"ok": False, "error": "not_ready"}

        try:
            Workflow("wf4", store).run("ping", not_ready, inputs={}, attempts=3)
            check(False, "an exhausted retry still stops")
        except WorkflowStopped:
            check(len(retried) == 3,
                  "a failure that provably never ran IS retried - three attempts")

        print()
        print("Rollback undoes finished stages, newest first")
        undone = []
        wf5 = Workflow("wf5", store)
        wf5.run("a", lambda: {"ok": True}, inputs={}, undo=lambda: undone.append("a"))
        wf5.run("b", lambda: {"ok": True}, inputs={}, undo=lambda: undone.append("b"))
        wf5.rollback()
        check(undone == ["b", "a"],
              "newest first - undoing the foundation before what stands on it makes it worse")

        print()
        print("A throwing undo does not stop the rest, or replace the real error")
        undone2 = []

        def boom():
            raise RuntimeError("undo failed")

        wf6 = Workflow("wf6", store)
        wf6.run("a", lambda: {"ok": True}, inputs={}, undo=lambda: undone2.append("a"))
        wf6.run("b", lambda: {"ok": True}, inputs={}, undo=boom)
        wf6.rollback()
        check(undone2 == ["a"], "the surviving undo still ran after one threw")

        print()
        print("An exception inside a stage is a failure, not a crash")
        try:
            Workflow("wf7", store).run("bad", lambda: 1 / 0, inputs={}, attempts=1)
            check(False, "a throwing stage stops the workflow")
        except WorkflowStopped as stop:
            check("ZeroDivisionError" in (stop.reason or ""),
                  "and the reason names what actually went wrong")

        print()
        print("Checkpoints are on DISK, not in memory")
        check(os.path.exists(os.path.join(root, "wf2.json")),
              "a crash mid-workflow must not lose finished stages")

        print()
        print("Finishing clears the checkpoint")
        wf8 = Workflow("wf8", store)
        wf8.run("only", lambda: {"ok": True}, inputs={})
        wf8.finish()
        check(not os.path.exists(os.path.join(root, "wf8.json")),
              "a completed workflow has nothing left to protect")

    finally:
        shutil.rmtree(root, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("PASSED - stages resume, stale inputs re-run, and nothing retries blindly.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
