#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Golden Test Library - a permanent record of what Heron has been PROVEN to do.

docs/13 section 2b and Part 4 section 28: *"a permanent collection of known-good
cases. Every major update runs against these tests. Extremely important for
preventing regressions."* And the roadmap is blunt about the timing - it only
has value if it grows from day one, which is why it starts here rather than
after there is something to regress.

WHAT MAKES THIS DIFFERENT FROM tests/. Those suites run here and prove Heron's
own logic. These cases can only be proven by a person, in Revit, watching what
happens - "4 ducts highlighted on screen", "one Ctrl+Z put it back". No CI can
run them. So the library's job is not to execute them; it is to REMEMBER them,
and to notice when a proof has quietly expired.

THE PART THAT EARNS ITS KEEP: `depends`.

Every case records the files its proof rested on, and a `fingerprint` of those
files taken at the moment it was proven. The runner recomputes that fingerprint.
If it differs, the proof was against code that no longer exists and the case
reverts to needing re-proof.

That is not hypothetical tidiness. On 2026-08-28 an audit found that Steps 1, 4
and 5 - all proven in real Revit - had stopped being proven, because Step 6 had
modified six of the files those proofs rested on. It was found by hand, by
reading a diff. This is that check, automated, so the next one is found by
running something.

A case proven against an UNKNOWN build carries `fingerprint: None`. It is
treated as stale, because that is what it is: somebody saw it work, and nobody
can now say against what.

    python tests/test_golden.py
"""

# Status values. PROVEN means a person watched it happen in Revit.
PROVEN = "PROVEN"
PENDING = "PENDING"      # never yet run against Revit
FAILED = "FAILED"        # run, and it did not do what it should


CASES = [
    # ---------------------------------------------------------------- Step 1
    {
        "id": "G-01",
        "capability": "Bridge connect / disconnect",
        "request": "Press Heron AI > Heron on the ribbon",
        "expected": "Connects. The button's icon lights. Pressing again disconnects.",
        "status": PROVEN,
        "revit": ["2020", "2024"],
        "proven_on": "2026-08-26",
        "fingerprint": None,
        "depends": [
            "revit/Heron.Revit.Addin/HeronApplication.cs",
            "revit/Heron.Revit.Addin/Commands.cs",
            "revit/Heron.Bridge/BridgeServer.cs",
        ],
    },
    {
        "id": "G-02",
        "capability": "Per-session token",
        "request": "Send Revit 2024's token to the Revit 2020 pipe",
        "expected": "Refused as unauthorized. A wrong token learns nothing about what exists.",
        "status": PROVEN,
        "revit": ["2020", "2024"],
        "proven_on": "2026-08-26",
        "fingerprint": None,
        "depends": ["revit/Heron.Bridge/BridgeIdentity.cs", "revit/Heron.Bridge/BridgeServer.cs"],
    },

    # ---------------------------------------------------------------- Step 2
    {
        "id": "G-03",
        "capability": "The thread hop",
        "request": "count_elements against a real model",
        "expected": "A real number comes back. 5,844 from 2024 and 3,167 from 2020 when first proven.",
        "status": PROVEN,
        "revit": ["2020", "2024"],
        "proven_on": "2026-08-26",
        "fingerprint": None,
        "depends": [
            "revit/Heron.Revit.Addin/RevitDispatcher.cs",
            "revit/Heron.Revit.Addin/RevitOperations.cs",
        ],
    },
    {
        "id": "G-04",
        "capability": "Revit is busy, not a hang",
        "request": "Open a modal dialog in Revit, then ask for a count",
        "expected": "A clean 'Revit is busy' refusal within about 10s. Recovers by itself.",
        "status": PROVEN,
        "revit": ["2020"],
        "proven_on": "2026-08-26",
        "fingerprint": None,
        "depends": ["revit/Heron.Revit.Addin/RevitDispatcher.cs"],
    },

    # ---------------------------------------------------------------- Step 4
    {
        "id": "G-05",
        "capability": "Select all ducts",
        "request": "select all ducts",
        "expected": "They highlight ON SCREEN. The answer names the count, the document AND the session.",
        "status": PROVEN,
        "revit": ["2020", "2024"],
        "proven_on": "2026-08-27",
        "fingerprint": None,
        "depends": [
            "revit/Heron.Revit.Addin/RevitOperations.cs",
            "mcp/server/heron_mcp_server.py",
        ],
    },

    # ---------------------------------------------------------------- Step 5
    {
        "id": "G-06",
        "capability": "Two Revits, the picker",
        "request": "With 2020 and 2024 both connected, ask for ducts",
        "expected": "Refuses to guess. Offers a numbered list. Nothing is sent to Revit.",
        "status": PROVEN,
        "revit": ["2020", "2024"],
        "proven_on": "2026-08-27",
        "fingerprint": None,
        "depends": ["mcp/server/heron_session.py", "mcp/client/heron_bridge_client.py"],
    },
    {
        "id": "G-07",
        "capability": "Fails closed when the chosen Revit closes",
        "request": "Choose 2024, then close it while 2020 stays open",
        "expected": "Heron STOPS and says so. It must never slide onto the other model.",
        "status": PROVEN,
        "revit": ["2020", "2024"],
        "proven_on": "2026-08-27",
        "fingerprint": None,
        "depends": ["mcp/server/heron_session.py"],
    },

    # ---------------------------------------------------------------- Step 6
    {
        "id": "G-08",
        "capability": "The write gate",
        "request": "With write.enabled false, ask to move ducts",
        "expected": "Refused, naming write.enabled and the config file. Nothing sent to Revit.",
        "status": PENDING,
        "revit": [],
        "proven_on": None,
        "fingerprint": None,
        "depends": [
            "platform/Heron.Core/HeronPermissions.cs",
            "platform/Heron.Core/HeronOperationRegistry.cs",
            "revit/Heron.Revit.Addin/RevitOperations.cs",
        ],
    },
    {
        "id": "G-09",
        "capability": "Preview changes nothing",
        "request": "move the ducts up 200 mm",
        "expected": "A preview with a count and what would be skipped. NOTHING moves.",
        "status": PENDING,
        "revit": [],
        "proven_on": None,
        "fingerprint": None,
        "depends": ["revit/Heron.Revit.Addin/RevitWrite.cs"],
    },
    {
        "id": "G-10",
        "capability": "The move, and the distance is right",
        "request": "Approve the 200 mm preview, then MEASURE one duct",
        "expected": "Exactly 200 mm. Not 200 feet, not 0.656 of anything.",
        "status": PENDING,
        "revit": [],
        "proven_on": None,
        "fingerprint": None,
        "depends": [
            "revit/Heron.Revit.Addin/RevitWrite.cs",
            "platform/Heron.Core/HeronUnits.cs",
        ],
    },
    {
        "id": "G-11",
        "capability": "One user action, one undo",
        "request": "Press Ctrl+Z once after a move",
        "expected": "Everything back, in ONE undo step, named 'Heron: move ducts up 200 mm'.",
        "status": PENDING,
        "revit": [],
        "proven_on": None,
        "fingerprint": None,
        "depends": ["revit/Heron.Revit.Addin/RevitWrite.cs"],
    },
    {
        "id": "G-12",
        "capability": "A failed operation leaves the model untouched",
        "request": "Force a failure mid-move",
        "expected": "Complete rollback. The message says so. Never a partial move.",
        "status": PENDING,
        "revit": [],
        "proven_on": None,
        "fingerprint": None,
        "depends": ["revit/Heron.Revit.Addin/RevitWrite.cs"],
    },
    {
        "id": "G-13",
        "capability": "The preview expires",
        "request": "Preview, wait over two minutes, then approve",
        "expected": "Refused as expired. Nothing moves.",
        "status": PENDING,
        "revit": [],
        "proven_on": None,
        "fingerprint": None,
        "depends": ["revit/Heron.Revit.Addin/RevitWrite.cs"],
    },
    {
        "id": "G-14",
        "capability": "The model moved on",
        "request": "Preview, draw one more duct, then approve",
        "expected": "Refused, naming both counts. Golden Rule 21.",
        "status": PENDING,
        "revit": [],
        "proven_on": None,
        "fingerprint": None,
        "depends": ["revit/Heron.Revit.Addin/RevitWrite.cs"],
    },
    {
        "id": "G-15",
        "capability": "The wrong model is refused",
        "request": "Preview in one project, click into a second, approve",
        "expected": "Refused, naming BOTH models. Golden Rule 20.",
        "status": PENDING,
        "revit": [],
        "proven_on": None,
        "fingerprint": None,
        "depends": ["revit/Heron.Revit.Addin/RevitWrite.cs", "mcp/server/heron_write.py"],
    },
    {
        "id": "G-16",
        "capability": "A second chat is refused, not allowed to take over",
        "request": "Two chats, one Revit, both ask for work",
        "expected": "The second is refused with a message. The first is never cut off.",
        "status": PENDING,
        "revit": [],
        "proven_on": None,
        "fingerprint": None,
        "depends": ["platform/Heron.Core/HeronLease.cs", "revit/Heron.Bridge/BridgeServer.cs"],
    },
    {
        "id": "G-17",
        "capability": "The audit says WHICH elements",
        "request": "Move ducts, then read the audit log",
        "expected": "The entry carries every moved element's UniqueId, keyed by Workflow ID.",
        "status": PENDING,
        "revit": [],
        "proven_on": None,
        "fingerprint": None,
        "depends": [
            "revit/Heron.Revit.Addin/RevitWrite.cs",
            "platform/Heron.Core/HeronAudit.cs",
        ],
    },
]
