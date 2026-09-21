#!/usr/bin/env python3
# Heron-Agent:  HERON-KRN-WFL-007, HERON-KRN-STA-005
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
Workflow Engine and checkpoints. What makes "Continue." a first-class command.

docs/23 sections 3 and 4. The split it exists to enforce:

    Orchestrator     decides WHAT should happen - which capability, which agents
    Workflow Engine  makes sure it happens CORRECTLY - ordering, retries,
                     timeouts, rollback, resumption

Keeping them apart means the retry logic is written once, here, instead of being
reinvented inside every multi-step job - and it keeps the Orchestrator thin,
which Golden Rule 2 requires.

WHY CHECKPOINTS ARE THE VALUABLE HALF. docs/23 calls resume "the most
practically valuable item in Part 4", and the reason is money as much as time:
the new-tool pipeline is 18 stages, several of them agentic loops. Failing at
stage 13 and discarding twelve completed stages is expensive and demoralising.

    Step 1 done   Step 2 done   Step 3 done   Step 4 FAILED
    After fixing Step 4: continue FROM Step 4.

The four requirements docs/23 sets, and how each is met here:

1. **Every stage output is persisted, not held in memory** - written to disk as
   it completes, so a crash mid-workflow loses nothing.
2. **Checkpoints are content-addressed** - a stage's cached output is keyed by
   the hash of its INPUT. Change the input and the cache misses, so the stage
   re-runs. Without this, "continue" silently builds on stale work, which is
   worse than restarting.
3. **Scoped to a Workflow ID** - the same key the audit log uses (docs/21 §13),
   so one identifier ties a sentence to every stage, retry and element touched.
4. **A paused workflow is visible** - `waiting_on()` answers "what is Heron
   waiting for?", so a job cannot silently die and be discovered three days
   later.

NOTHING CALLS THIS YET, AND THAT IS DELIBERATE - say so rather than letting a
reader assume otherwise.

Phase 1's only multi-stage flow is preview -> approve -> apply, and that is
already sequenced by the preview token inside the ADD-IN. The add-in is the
better place for it: it is the side that can re-count against the live model
immediately before writing, which is what Golden Rule 21 requires and what this
engine, sitting outside Revit, cannot do. Wiring the engine into that flow would
duplicate a mechanism that already exists and is better placed.

Its real customer is the 18-stage new-tool pipeline in Phase 2, where a failure
at stage 13 currently means discarding twelve finished stages. It is built now
because the roadmap puts it in Phase 1 and because building it after the
pipeline means retrofitting resume through code that assumes it restarts.

So: this is proven by its tests and unproven in use. When the first real
multi-stage workflow arrives, it is here - and if it turns out to be the wrong
shape, changing it costs nothing today and would cost a rewrite once eighteen
stages depend on it.

IT NEVER RETRIES BLINDLY. The engine does not decide that for itself; it asks
the Failure Analysis Agent (heron_failure), which is the one place that knows
the difference between "never reached the model" and "was sent, and the answer
was lost". A workflow engine that retried on its own judgement would undo the
whole point of that agent.
"""

import hashlib
import io
import json
import os
import time

import heron_failure


# How long a stage may take before the engine stops waiting on it.
DEFAULT_TIMEOUT_S = 300.0

# Between retries. Short, and only ever used for failures the Failure Analysis
# Agent has said are safe to repeat.
RETRY_BACKOFF_S = 0.5
MAX_ATTEMPTS = 3


def checkpoint_root():
    """
    Where stage outputs live. Overridable with HERON_CHECKPOINTS, which is what
    the tests use and the only way to run this where %LOCALAPPDATA% does not
    exist - which is most of the machines this gets written on.

    DERIVED state, not user data: safe to delete, and deleting it only costs
    re-running work (docs/17 / D-17).
    """
    override = os.environ.get("HERON_CHECKPOINTS")
    if override:
        return override
    base = os.environ.get("LOCALAPPDATA") or os.environ.get("TMPDIR") or "/tmp"
    return os.path.join(base, "Heron", "checkpoints")


def _hash(value):
    """A stable hash of a stage's input. Sorted keys, so dict order cannot change it."""
    text = json.dumps(value, sort_keys=True, default=str)
    return hashlib.sha256(text.encode("utf-8")).hexdigest()[:16]


class Stage(object):
    """One step, its result, and how it got there."""

    def __init__(self, name, status, output=None, reason=None, attempts=0, seconds=0.0):
        self.name = name
        self.status = status          # "done" | "failed" | "waiting"
        self.output = output
        self.reason = reason
        self.attempts = attempts
        self.seconds = seconds

    def __repr__(self):
        return "<%s %s>" % (self.name, self.status)


class CheckpointStore(object):
    """
    Stage outputs on disk, keyed by workflow id, stage name and input hash.

    One file per workflow rather than one per stage: a workflow is read and
    written as a whole, and a single file cannot be half-updated into a state
    where stage 3 exists and stage 2 does not.
    """

    def __init__(self, root=None):
        self.root = root or checkpoint_root()

    def _path(self, workflow_id):
        return os.path.join(self.root, "%s.json" % workflow_id)

    def load(self, workflow_id):
        try:
            with io.open(self._path(workflow_id), encoding="utf-8") as fh:
                return json.load(fh)
        except (IOError, OSError, ValueError):
            # A missing or unreadable checkpoint file means "nothing done yet",
            # never an error. The worst case is repeating work, which is always
            # safer than refusing to run.
            return {"workflow": workflow_id, "stages": {}}

    def save(self, state):
        try:
            if not os.path.isdir(self.root):
                os.makedirs(self.root)
            path = self._path(state["workflow"])
            # Write beside, then replace: a crash mid-write leaves the previous
            # checkpoint intact rather than a truncated file that parses to
            # "nothing was ever done".
            temp = path + ".tmp"
            with io.open(temp, "w", encoding="utf-8") as fh:
                fh.write(json.dumps(state, indent=2, sort_keys=True, default=str))
            # os.replace, NOT remove-then-rename. The comment above has always
            # described an atomic swap and the code did not do one: it deleted
            # the old file and then renamed the new one into place, leaving a
            # window with NO checkpoint at all - and load() answers a missing
            # file with "nothing done yet". A crash in that window silently
            # discarded every finished stage, which is the exact loss
            # checkpoints exist to prevent.
            #
            # THE C# SIDE HAD THIS AND FIXED IT A WEEK EARLIER. HeronConfig's
            # own note reads "This was write-tmp, DELETE, move until
            # 2026-09-16, which left a window with no config file at all",
            # and HeronAtomicWrite exists because of it. The Python half was
            # never looked at. FRAGMENT-ISSUES section 5b, row 31.
            #
            # os.replace is atomic on POSIX and on Windows, and unlike
            # os.rename it overwrites an existing file on both.
            os.replace(temp, path)
        except (IOError, OSError):
            # Losing a checkpoint costs repeated work. Failing the operation
            # because it could not be recorded costs the work itself.
            pass

    def forget(self, workflow_id):
        try:
            os.remove(self._path(workflow_id))
        except OSError:
            pass


class Workflow(object):
    """
    One user sentence, run as ordered stages that can be resumed.

    Usage:

        wf = Workflow("a1b2c3", store)
        found  = wf.run("preview", lambda: preview(...), inputs={"mm": 200})
        moved  = wf.run("apply",   lambda: apply(...),   inputs={"token": found["token"]},
                        writes=True, undo=lambda: ...)

    A stage that already completed with the SAME inputs returns its recorded
    output without running. Change the inputs and it runs again.
    """

    def __init__(self, workflow_id, store=None, timeout=DEFAULT_TIMEOUT_S):
        self.id = workflow_id
        self.store = store or CheckpointStore()
        self.timeout = timeout
        self.state = self.store.load(workflow_id)
        self.stages = []

    # ------------------------------------------------------------------ run

    def run(self, name, action, inputs=None, writes=False, undo=None, attempts=MAX_ATTEMPTS):
        """
        Run one stage, or return what it produced last time.

        `writes` is passed straight to the Failure Analysis Agent and decides
        how a failure is treated. Pass it truthfully: saying False about a stage
        that changes the model is what makes an unknown outcome look retryable.

        `undo` is remembered so a later failure can roll this stage back.
        """
        key = _hash(inputs)
        recorded = self.state["stages"].get(name)

        # CONTENT-ADDRESSED. A recorded output is only reusable if the input
        # that produced it is the input being asked for now.
        if recorded and recorded.get("status") == "done" and recorded.get("input") == key:
            stage = Stage(name, "done", recorded.get("output"), "resumed from checkpoint")
            self.stages.append((stage, undo))
            return stage.output

        started = time.time()
        last = None

        for attempt in range(1, attempts + 1):
            if time.time() - started > self.timeout:
                last = "gave up after %.0fs" % (time.time() - started)
                break

            if attempt > 1:
                time.sleep(RETRY_BACKOFF_S * (2 ** (attempt - 2)))

            try:
                output = action()
            except Exception as exc:                     # a stage must not escape
                last = "%s: %s" % (type(exc).__name__, exc)

                # AND THIS IS ASKED, NOT DECIDED HERE. A stage that THREW was
                # retried outright until 2026-09-21, up to three times, even
                # on the writing path - which is the blind retry this file's
                # own header says it never does. An exception carries no
                # reply, so whether the request reached Revit is exactly the
                # unknown outcome heron_failure was written for, and it
                # already answers it: analyse(None, writes=...) is RETRY for
                # a read and LOOK_AT_THE_MODEL for a write.
                # FRAGMENT-ISSUES section 5b, row 32.
                thrown = heron_failure.analyse(None, writes=writes)
                if not thrown.may_retry:
                    self._record(name, "failed", key, None, last, attempt)
                    stage = Stage(name, "failed", None, last, attempt)
                    self.stages.append((stage, undo))
                    raise WorkflowStopped(name, last, thrown)
                continue

            failure = heron_failure.analyse(output, writes=writes) \
                if isinstance(output, dict) else None

            if failure is None:
                stage = Stage(name, "done", output, None, attempt, time.time() - started)
                self._record(name, "done", key, output, None, attempt)
                self.stages.append((stage, undo))
                return output

            last = failure.reason

            # NEVER BLIND-RETRIES. The engine does not judge this itself - the
            # Failure Analysis Agent is the one place that knows whether the
            # request reached the model, and an unknown outcome must stop here
            # rather than be repeated.
            if not failure.may_retry:
                self._record(name, "failed", key, None, last, attempt)
                stage = Stage(name, "failed", None, last, attempt)
                self.stages.append((stage, undo))
                raise WorkflowStopped(name, last, failure)

        self._record(name, "failed", key, None, last, attempts)
        stage = Stage(name, "failed", None, last, attempts)
        self.stages.append((stage, undo))
        raise WorkflowStopped(name, last, None)

    def _record(self, name, status, key, output, reason, attempts):
        self.state["stages"][name] = {
            "status": status, "input": key, "output": output,
            "reason": reason, "attempts": attempts, "at": time.time(),
        }
        self.store.save(self.state)

    # ------------------------------------------------------------- rollback

    def rollback(self):
        """
        Undo completed stages, newest first.

        Newest first because a later stage may depend on an earlier one, and
        undoing the foundation before the thing standing on it is how a rollback
        makes the mess worse. Each undo is guarded: one that throws must not
        stop the rest, and must not replace the failure that caused the rollback
        - the same rule the add-in's SafeRollBack follows.
        """
        undone = []
        for stage, undo in reversed(self.stages):
            if stage.status != "done" or undo is None:
                continue
            try:
                undo()
                undone.append(stage.name)
            except Exception:
                pass
        return undone

    # -------------------------------------------------------------- visible

    def waiting_on(self):
        """
        What is this workflow waiting for, or None if it is finished or idle.

        docs/23 requirement 4: a paused workflow must be visible, so a job
        cannot silently die and be discovered days later.
        """
        for name, s in sorted(self.state["stages"].items(), key=lambda kv: kv[1].get("at", 0)):
            if s.get("status") == "failed":
                return "%s - %s" % (name, s.get("reason") or "failed")
        return None

    def done_stages(self):
        return sorted(n for n, s in self.state["stages"].items() if s.get("status") == "done")

    def describe(self):
        """The workflow as a person reads it."""
        lines = ["Workflow %s" % self.id]
        if not self.state["stages"]:
            lines.append("  nothing recorded yet")
            return "\n".join(lines)

        for name, s in sorted(self.state["stages"].items(), key=lambda kv: kv[1].get("at", 0)):
            mark = {"done": "done  ", "failed": "FAILED", "waiting": "wait  "}.get(
                s.get("status"), "?     ")
            extra = ""
            if s.get("attempts", 1) > 1:
                extra += "  (%d attempts)" % s["attempts"]
            if s.get("reason"):
                extra += "  %s" % s["reason"]
            lines.append("  %s %s%s" % (mark, name, extra))

        waiting = self.waiting_on()
        if waiting:
            lines.append("")
            lines.append("Stopped at: %s" % waiting)
            lines.append("Fix it and ask again - the finished stages are kept.")
        return "\n".join(lines)

    def finish(self):
        """Completed successfully: the checkpoint has nothing left to protect."""
        self.store.forget(self.id)


class WorkflowStopped(Exception):
    """A stage failed and the workflow stopped. Earlier stages are kept."""

    def __init__(self, stage, reason, failure=None):
        Exception.__init__(self, "%s: %s" % (stage, reason))
        self.stage = stage
        self.reason = reason
        self.failure = failure
