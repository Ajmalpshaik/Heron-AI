#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
What the MCP tools SAY about Heron matches what Heron IS. Runs without Revit.

THE FAILURE THIS GUARDS, and it happened.

Three tools - heron_capabilities, heron_lookup, heron_resolve - ended every
answer with two hand-typed sentences:

    "a fragment's code has no way to reach Revit"
    "Every skill and every fragment is DRAFT"

Both were true when written. Both went FALSE on 2026-09-06, in the same
afternoon: D-28's executor was built and twenty fragments ran against a real
model, thirteen of them promoted on a recorded proof. The sentences did not
change, because a sentence in a source file does not know what the rest of the
repository did.

That is the worst shape a wrong answer can take here - not a tool that fails,
but a tool that answers confidently about the state of the system and is wrong.
It is also what the AI host reads to decide what it can attempt.

THEN THE FIRST FIX WAS WRONG TOO, and this test exists because of that as much
as the first fault. Counting from `brain.catalogue()` read the RETRIEVAL STORE,
which is an index rebuilt on demand: thirteen fragments were PROVEN on disk and
the store still said zero. A count from a cache of the truth is not the truth.

    A status is a fact about a file. Read it from the file.

    python tests/test_served_claims.py
"""

import io
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))


def disk_count():
    """PROVEN fragments, counted here independently of the server's own code."""
    folder = os.path.join(ROOT, "brain", "fragments")
    proven = total = 0
    for name in sorted(os.listdir(folder)):
        path = os.path.join(folder, name, "fragment.yaml")
        if not os.path.isfile(path):
            continue
        total += 1
        with io.open(path, encoding="utf-8", errors="replace") as fh:
            for line in fh:
                if line.startswith("heron-status:"):
                    if line.split(":", 1)[1].strip().upper() in ("PROVEN", "PRODUCTION"):
                        proven += 1
                    break
    return proven, total


def run():
    failures = []

    # WITHOUT THE SDK THIS EXITS 3, NOT 1, and the distinction is the whole
    # reason for the guard. heron_mcp_server needs the MCP SDK, which is an
    # optional dependency; importing it without one raised, this file died
    # with a traceback, and check-gaps.py counted it UNFINISHED - the bucket
    # that means SOMEBODY COULD FIX THIS HERE. Nobody could: it is an
    # install, not a defect. test_mcp_serves.py has exited 3 for the same
    # absence since 2026-08-31; this file was the odd one out.
    #
    # BaseException, not ImportError, on purpose: `pip install --user mcp`
    # leaves the SDK importable but PANICKING on some images -
    # pyo3_runtime.PanicException out of the distro's cryptography - and a
    # panic is not an ImportError. Either way nothing here can run.
    try:
        import heron_mcp_server as server       # noqa: E402
    except BaseException as absent:             # noqa: BLE001
        print("What the tools say about Heron")
        print("  SKIPPED - the MCP server will not import here:")
        print("    %s: %s" % (type(absent).__name__, absent))
        print("\n  This proves NOTHING either way. Install the SDK and run it "
              "again:")
        print("    pip install --user mcp")
        print("    pip install --user --upgrade cryptography   # if it panics")
        return 3

    proven, total = disk_count()

    # PROVE THE COUNT CAN SEE WHAT IS THERE. A comparison of two zeros passes
    # perfectly, and this repository has been caught by that five times.
    if total < 100:
        failures.append(
            "only %d fragment(s) found on disk - the count is not reading the "
            "library, and every check below would pass on nothing" % total)

    said = server._proven_split()

    if said != (proven, total):
        failures.append(
            "the server says %r and the files on disk say %r. If the server is "
            "reading the retrieval store, that is an INDEX and it goes stale - "
            "a promotion on disk stays invisible until somebody reindexes"
            % (said, (proven, total)))

    message = server._not_proven()

    if proven and str(proven) not in message:
        failures.append(
            "%d fragment(s) are PROVEN and the served sentence does not say so: "
            "%r" % (proven, message[:90]))

    if proven and "Every skill and every fragment is DRAFT" in message:
        failures.append(
            "the served sentence still claims everything is DRAFT while %d "
            "are PROVEN" % proven)

    runnable = server._cannot_run()

    # The executor exists. Saying otherwise sends the host away from something
    # it can actually do - and the opposite error is worse still, so the
    # sentence has to keep the WRITE caution while dropping the read one.
    if "no way to reach Revit" in runnable and "WRITES" not in runnable:
        failures.append(
            "the served sentence still says a fragment cannot reach Revit at "
            "all. The read executor exists; only the write path does not")

    if "WRITES" not in runnable:
        failures.append(
            "the served sentence no longer warns that a fragment which WRITES "
            "still cannot reach Revit - that caution is still true and a plan "
            "built without it fails at its last step")

    print("What the tools say about Heron")
    print("  on disk   %d PROVEN of %d" % (proven, total))
    print("  served    %r" % (said,))

    if failures:
        for line in failures:
            print("  FAIL  " + line)
        print("\nFAILED - a tool is answering confidently about a state that is "
              "not the state.")
        return 1

    print("  PASS  the proven count is read from the files, not from an index")
    print("  PASS  the served sentences match what the repository actually is")
    print("\nA status is a fact about a file. It is read from the file.")
    return 0


if __name__ == "__main__":
    sys.exit(run())
