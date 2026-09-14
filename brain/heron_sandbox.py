# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-SBX-016
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Agent Sandbox - where a new agent runs before it is allowed near real work.

    python brain/heron_sandbox.py     an agent that tries three forbidden things

WHAT THE REGISTER ASKS FOR
---------------------------
"Runs a newly built agent in isolation - never against a live model, never able
to write production knowledge - before it is allowed anywhere near real work."

Two prohibitions, and they are not the same kind of thing. A live model can be
damaged in a way that costs somebody a day; production knowledge can be
poisoned in a way nobody notices for a month. The first is loud, the second is
the one this repository has more to lose from.

REFUSED AND RECORDED, NEVER SILENTLY BLOCKED
---------------------------------------------
The sandbox hands the agent a world whose Revit door and production scopes are
shut, and every attempt to open one is written down. What a new agent TRIED to
do on its first run is the most useful thing about that run - a sandbox that
quietly returned None to a forbidden call would throw away the evidence and
leave the agent looking well behaved.

EVERY RESULT IS MARKED, AND A MARKED RESULT IS NOT EVIDENCE
------------------------------------------------------------
`sandboxed` rides on every run, for the same reason `degraded` rides on a
fallback (HERON-KRN-MAV-017): docs/24 promotes on real executions, and a run
against a stub world is not one. The two agents use the same word for it -
counts_as_evidence - so a caller learns one question rather than two.

WHAT IT CANNOT DO, AND THE HONEST VERSION OF SAYING SO
--------------------------------------------------------
**IT RESTRAINS A COOPERATING AGENT. IT DOES NOT CONTAIN A HOSTILE ONE.**

The handler runs as ordinary Python in this process. An agent that goes
through `world.write()` and `world.revit()` is held to the rules above; one
that calls `open()`, imports the bridge, or reaches into globals is not
stopped by anything here. Every rule in this module is a door in a wall that
has no other side yet.

That is a real gap in something with "sandbox" in its name, so it is written
at the top of the file rather than left to be discovered, and it is
`Q-56` in OPEN-QUESTIONS. Closing it means running the agent in a separate
process with the ambient capabilities removed - which is a different piece of
work from this one, and not something to half-do inside a module that would
then look finished.

Until that exists, the honest reading is: this is the stage where a NEW agent
is watched and its attempts recorded, not the wall that would hold a bad one.
What it does contain is the accident - the agent that reaches for Revit
because nobody told it not to - and that is most of them.

It also cannot INTERRUPT an agent that will not stop. Interrupting arbitrary
Python needs a thread or a signal, both of which bring their own failure modes
into the one place that must not have any. So it measures against the
contract's timeout and reports an overrun after the fact, and a runaway agent
still hangs the process it was run in. The same subprocess fixes both, which
is why they are one question and not two.
"""

import copy
import sys
import time

# Where a sandboxed agent may write. Everything else in docs/10 is production
# in the sense that matters: somebody's real work depends on it being true.
WRITABLE_SCOPES = ("temporary", "experimental")


class Refused(Exception):
    """Raised into the agent when it reaches for something it may not have."""


class SandboxWorld(object):
    """
    What a sandboxed agent is given instead of the real thing.

    Every door is shut and every knock is recorded. The agent is free to
    catch Refused and carry on - plenty of good agents will - and the attempt
    is in the record either way.
    """

    def __init__(self, scope="temporary"):
        self.attempts = []
        self.writes = {}
        self.scope = scope

    def revit(self, operation, **kw):
        self.attempts.append(("LIVE_MODEL_REFUSED", operation))
        raise Refused(
            "a sandboxed agent cannot reach Revit. '%s' was refused and "
            "recorded; nothing was sent to any model." % operation)

    def write(self, scope, key, value):
        if scope not in WRITABLE_SCOPES:
            self.attempts.append(("PRODUCTION_SCOPE_REFUSED", scope))
            raise Refused(
                "a sandboxed agent may write to %s only. '%s' is production "
                "knowledge, and poisoned knowledge is the damage nobody "
                "notices for a month." % (" or ".join(WRITABLE_SCOPES), scope))
        self.writes[(scope, key)] = value
        return True

    def read(self, scope, key, default=None):
        return self.writes.get((scope, key), default)


def run(agent_id, handler, payload=None, timeout_seconds=None, world=None):
    """
    Run one agent in the sandbox. Returns a record; never raises on its behalf.

    `handler` is the agent's callable - (world, payload) in, anything out. A
    sandbox that let the agent's exception escape would take down whatever was
    supervising it, which is the one process that has to survive a bad agent.
    """
    world = world or SandboxWorld()
    started = time.time()
    # THREE INDEPENDENT COPIES, not one shallow one. A contract may declare a
    # `map` payload, so a nested dictionary or list inside it is normal - and
    # dict(payload) copies only the outer mapping, leaving every nested object
    # shared with the caller AND with the record. A new agent could then edit
    # the caller's data and rewrite the audit record of what it was given,
    # during the very run the record exists to describe.
    record = {"agent": agent_id, "sandboxed": True,
              "payload": copy.deepcopy(payload) if payload else {},
              "result": None, "failed": None, "attempts": world.attempts}

    try:
        record["result"] = handler(
            world, copy.deepcopy(payload) if payload else {})
    except Refused as exc:
        record["failed"] = "REFUSED: %s" % exc
    except Exception as exc:                                 # noqa: BLE001
        # Deliberately broad: the agent is new, and "it threw something
        # nobody expected" is the most likely thing a first run produces.
        record["failed"] = "AGENT_RAISED: %s: %s" % (type(exc).__name__, exc)

    record["seconds"] = round(time.time() - started, 3)
    if timeout_seconds and record["seconds"] > timeout_seconds:
        record["overran"] = True
        record["failed"] = (record["failed"] or "") + \
            ("OVERRAN_ITS_TIMEOUT: %ss against a contract of %ss. The sandbox "
             "measures; it cannot interrupt." % (record["seconds"],
                                                 timeout_seconds))
    else:
        record["overran"] = False
    record["wrote"] = dict(world.writes)
    return record


def counts_as_evidence(record):
    """
    Never. Kept as a function, and named the same as the availability agent's,
    so a caller asks one question of both rather than remembering which
    results are real.
    """
    return False


def main(argv):
    def badly_behaved(world, payload):
        try:
            world.revit("delete_elements", ids=[1, 2, 3])
        except Refused:
            pass
        try:
            world.write("company", "standard.duct.velocity", 9)
        except Refused:
            pass
        world.write("experimental", "guess.duct.velocity", 9)
        return {"did": "what it could"}

    record = run("HERON-MADE-UP-001", badly_behaved, {"category": "ducts"},
                 timeout_seconds=5)

    print("AGENT SANDBOX   every door shut, every knock written down")
    print("=" * 68)
    print("  agent      %s" % record["agent"])
    print("  sandboxed  %s   evidence: %s"
          % (record["sandboxed"], counts_as_evidence(record)))
    print("  result     %s" % record["result"])
    print("  seconds    %s (overran: %s)" % (record["seconds"],
                                             record["overran"]))
    print()
    print("  what it tried that was refused:")
    for code, what in record["attempts"]:
        print("    %-26s %s" % (code, what))
    print()
    print("  what it was allowed to write:")
    for (scope, key), value in sorted(record["wrote"].items()):
        print("    %-14s %-30s %s" % (scope, key, value))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
