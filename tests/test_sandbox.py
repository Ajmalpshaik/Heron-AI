# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-SBX-016
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The sandbox - refused and recorded, and never evidence.

    python tests/test_sandbox.py

WHAT IT PROVES
  1. A SANDBOXED AGENT CANNOT REACH REVIT, and the attempt is recorded rather
     than silently swallowed. What a new agent tried on its first run is the
     most useful thing about that run.

  2. IT CANNOT WRITE PRODUCTION KNOWLEDGE - global, company, project or user -
     and it CAN write the two scopes docs/10 calls discarded and quarantined.

  3. THE REFUSAL REACHES THE AGENT AS AN EXCEPTION IT CAN CATCH, not as a
     None it will mistake for success.

  4. EVERY RUN IS MARKED SANDBOXED AND IS NEVER EVIDENCE - the same rule and
     the same question as the availability agent's degraded results.

  5. AN AGENT THAT RAISES DOES NOT TAKE THE SANDBOX WITH IT. The supervising
     process is the one thing that has to survive a bad agent.

  6. AN OVERRUN AGAINST THE CONTRACT'S TIMEOUT IS REPORTED, and the record
     says plainly that the sandbox measured rather than interrupted.

  7. THE PAYLOAD THE AGENT IS GIVEN IS A COPY. An agent that edits what it was
     handed must not change what the caller still holds.

  8. WHAT WAS WRITTEN IS IN THE RECORD, so a first run can be read afterwards
     without the world object being kept.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_sandbox as BOX                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    print("1. Revit is not reachable, and the attempt is kept")
    def reaches_for_revit(world, payload):
        world.revit("move_elements", mm=200)
        return "should not get here"

    record = BOX.run("A", reaches_for_revit)
    check(record["result"] is None and "REFUSED" in record["failed"],
          "the run is refused rather than completed")
    check(("LIVE_MODEL_REFUSED", "move_elements") in record["attempts"],
          "and the operation it tried is recorded by name")
    check("nothing was sent to any model" in record["failed"],
          "the refusal answers the question every refusal is asked")

    print()
    print("2 and 3. Production knowledge is shut; the two open scopes are open")
    def writes_everywhere(world, payload):
        refused = []
        for scope in ("global", "company", "project", "user"):
            try:
                world.write(scope, "k", 1)
            except BOX.Refused:
                refused.append(scope)
        for scope in BOX.WRITABLE_SCOPES:
            world.write(scope, "k", 1)
        return refused

    record = BOX.run("B", writes_everywhere)
    check(record["result"] == ["global", "company", "project", "user"],
          "every production scope refused the write")
    check(record["failed"] is None,
          "and the agent that caught the refusals still finished")
    check(all(("PRODUCTION_SCOPE_REFUSED", s) in record["attempts"]
              for s in ("global", "company", "project", "user")),
          "each refused scope is in the record")
    check(sorted(k[0] for k in record["wrote"]) == sorted(BOX.WRITABLE_SCOPES),
          "temporary and experimental were written, and nothing else")

    print()
    print("4. Marked, and never evidence")
    check(record["sandboxed"] is True, "sandboxed rides on the record")
    check(BOX.counts_as_evidence(record) is False,
          "and a sandboxed run never counts toward promotion")
    import heron_availability as AVAIL
    check(not AVAIL.counts_as_evidence({"adapter": "x", "degraded": True}),
          "the availability agent answers the same question the same way")

    print()
    print("5. A bad agent does not take the sandbox with it")
    def explodes(world, payload):
        raise ZeroDivisionError("it divided by a duct")

    record = BOX.run("C", explodes)
    check(record["failed"].startswith("AGENT_RAISED: ZeroDivisionError"),
          "the exception is recorded with its type and message")
    check(record["sandboxed"] and record["seconds"] >= 0,
          "and the record is still complete")

    print()
    print("6. An overrun is reported, and the limitation is stated")
    def dawdles(world, payload):
        end = __import__("time").time() + 0.05
        while __import__("time").time() < end:
            pass
        return "done"

    record = BOX.run("D", dawdles, timeout_seconds=0.01)
    check(record["overran"] and "OVERRAN_ITS_TIMEOUT" in record["failed"],
          "running past the contract's ceiling is reported")
    check("cannot interrupt" in record["failed"],
          "and the record says the sandbox measured rather than stopped it")
    check(BOX.run("D", dawdles, timeout_seconds=30)["overran"] is False,
          "a run inside its ceiling is not marked")

    print()
    print("7. The payload is a copy")
    def eats_its_payload(world, payload):
        payload.clear()
        return "ate it"

    mine = {"category": "ducts"}
    BOX.run("E", eats_its_payload, mine)
    check(mine == {"category": "ducts"},
          "what the caller still holds is untouched")

    print()
    print("7b. The copy is deep, because a payload may be a map")
    def rummages(world, payload):
        payload["nested"]["category"] = "pipes"
        payload["list"].append("added")
        return "changed what it was given"

    mine = {"nested": {"category": "ducts"}, "list": ["one"]}
    record = BOX.run("G", rummages, mine)
    check(mine["nested"]["category"] == "ducts" and mine["list"] == ["one"],
          "a nested value the caller still holds is untouched")
    check(record["payload"]["nested"]["category"] == "ducts",
          "and the agent could not rewrite the record of what it was given")

    print()
    print("8. The record stands alone afterwards")
    record = BOX.run("F", lambda w, p: w.write("temporary", "seen", 7))
    check(record["wrote"] == {("temporary", "seen"): 7},
          "what was written is in the record, not only in the world")
    check(set(record) >= {"agent", "sandboxed", "payload", "result",
                          "failed", "attempts", "seconds", "overran", "wrote"},
          "and the record carries every field a reader needs")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    every door shut, every knock written down, nothing counted")
    return 0


if __name__ == "__main__":
    sys.exit(main())
