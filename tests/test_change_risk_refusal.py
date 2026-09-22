# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A fragment above Modify is refused on the MCP path, not just the CLI.

    python tests/test_change_risk_refusal.py

WHAT WENT WRONG
---------------
FRAGMENT-ISSUES row 9 closed this hole on the command line: `risk_refusal`
refuses to SEND a fragment declared `risk: PUBLISH` or `risk: ADMIN`, because
HeronPermissions puts both out of reach in Phase 0 and Phase 1 - and the
add-in cannot tell, since `run_fragment_write` is declared Modify and row 9
records why the fragment's own risk is not sent over for the add-in to judge.

`revit_change` was written a week later and never called it. So with Changes
ON, a chat could run SYNC_WITH_CENTRAL, CREATE_WORKSET, SAVE_DOCUMENT or
UPGRADE_FAMILY_FILES, while the same request typed at the command line was
refused on the spot. FRAGMENT-ISSUES row 5b-155.

SO THIS SUITE CHECKS THREE THINGS, AND THE SECOND IS THE ONE THAT WOULD ROT.
The helper refuses every fragment above Modify in the real library and none
at or below it; `revit_change` CALLS it, before it binds a session or reads
any code; and the server carries no second copy of the rule. A helper that
nothing on the path people actually use calls is exactly the state this fixes.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_bridge_client as CLIENT                            # noqa: E402

FAILURES = []


def check(condition, what):
    if condition:
        print("  ok    %s" % what)
    else:
        print("  FAIL  %s" % what)
        FAILURES.append(what)


def capability_of(card):
    """The `capability:` line of one fragment.yaml, or None."""
    with io.open(card, "r", encoding="utf-8", errors="replace") as fh:
        for line in fh:
            if line.startswith("capability:"):
                return line.split(":", 1)[1].strip().upper()
    return None


def finish():
    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for f in FAILURES:
            print("  - %s" % f)
        return 1
    print("PASSED - the MCP path refuses what the command line refuses, "
          "using the one copy of the rule.")
    return 0


def main():
    print("1. The helper, over every fragment in the library")
    above, below, unreadable = [], [], []
    sync = None
    for name in sorted(os.listdir(FRAGMENTS)):
        card = os.path.join(FRAGMENTS, name, "fragment.yaml")
        if not os.path.isfile(card):
            continue
        if capability_of(card) == "SYNC_WITH_CENTRAL":
            sync = name
        risk = CLIENT.fragment_risk(card)
        refusal = CLIENT.risk_refusal(ROOT, name)
        if risk is None:
            unreadable.append(name)
        elif risk in CLIENT.RUNNABLE_RISKS:
            below.append((name, refusal))
        else:
            above.append((name, refusal))

    # Neither half may be empty, or the two checks after it pass on nothing.
    check(len(above) > 0,
          "the library has fragments above Modify to refuse - %d of them"
          % len(above))
    check(len(below) > 0,
          "and fragments at or below it to let through - %d of them" % len(below))
    check(all(r for _, r in above),
          "every fragment above Modify is refused - not refused: %r"
          % [n for n, r in above if not r])
    check(not any(r for _, r in below),
          "no fragment at or below Modify is refused - refused: %r"
          % [n for n, r in below if r][:5])
    check(all("Nothing was sent to Revit" in r for _, r in above if r),
          "and every refusal says that nothing was sent")
    check(sync is not None and sync in [n for n, _ in above],
          "SYNC_WITH_CENTRAL is among the refused - the one that reaches "
          "everybody on the project")
    check(unreadable == [],
          "every fragment declares a risk the helper can read - unreadable: %r"
          % unreadable[:5])

    print()
    print("2. revit_change calls it, before anything is bound or read")
    try:
        text = io.open(SERVER, encoding="utf-8").read()
    except OSError as why:
        print("  FAIL  could not read %s: %s" % (SERVER, why))
        return 1

    start = text.find(chr(10) + "def revit_change(")
    check(start > 0, "revit_change is still in the server")
    if start < 0:
        return finish()
    body = text[start:]
    end = body.find(chr(10) + "@server.tool()")
    if end > 0:
        body = body[:end]

    # EVERY POSITION IS CHECKED FOR PRESENCE BEFORE IT IS COMPARED. `find`
    # returns -1 for a string that is not there, so `a < b` passes loudest
    # when the guard it is meant to order has been deleted - heron-ship 2a.
    call = body.find("risk_refusal(")
    bind = body.find("binding.resolve(")
    read = body.find("fh.read()")
    check(call > 0, "revit_change CALLS risk_refusal rather than carrying a copy")
    check(bind > 0 and read > 0,
          "the session binding and the source read are still where this "
          "suite looks for them")
    check(call > 0 and bind > 0 and call < bind,
          "it refuses BEFORE binding a session - a refusal must touch nothing")
    check(call > 0 and read > 0 and call < read,
          "and before reading the fragment's code")
    check("return refusal" in body,
          "and returns what the helper said, so the reason reaches the chat")

    print()
    print("3. One copy of the rule")
    check("def risk_refusal" not in text,
          "the server did not reimplement the refusal")
    check("RUNNABLE_RISKS" not in text,
          "nor carry its own list of runnable risks, which would drift")

    return finish()


if __name__ == "__main__":
    sys.exit(main())
