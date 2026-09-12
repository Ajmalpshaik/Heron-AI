#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The runtime capability snapshot. Runs without Revit, and has to.

WHAT IS ACTUALLY BEING PINNED HERE
-----------------------------------
Not that the code runs - that six DIFFERENT answers stay six different
answers. brain/heron_capability.py returns None for five unrelated reasons,
and every one of them reaches a modeller as the same shrug. The negative cases
below are the point of the file: each asserts that one reason does NOT come
back wearing another reason's name.

    python tests/test_runtime_snapshot.py
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_runtime as rt                                    # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


READER = {"id": "FRG-SEL-001", "risk": "EXECUTE",
          "revit": ["2020", "2021", "2022", "2023", "2024"], "status": "PROVEN"}
WRITER = {"id": "FRG-ELE-009", "risk": "MODIFY", "revit": ["2024"], "status": "DRAFT"}
NEWEST = {"id": "FRG-QA-099", "risk": "READ", "revit": ["2027"], "status": "DRAFT"}

LIBRARY = {"SET_SELECTION": [READER], "MOVE_ELEMENTS": [WRITER], "NEW_ONLY": [NEWEST]}
ASKED = ["SET_SELECTION", "MOVE_ELEMENTS", "NEW_ONLY", "NOT_IN_THE_LIBRARY"]


def verdict(snap, name):
    return [c for c in snap.capabilities if c.name == name][0]


def main():
    print("Everything known: a permitted capability with a Revit to run it in")
    live = rt.snapshot(ASKED, LIBRARY, release="2024", bridge="connected")
    check(verdict(live, "SET_SELECTION").verdict == rt.AVAILABLE,
          "a proven EXECUTE provider on a supported release is AVAILABLE")
    check(verdict(live, "SET_SELECTION").provider == "FRG-SEL-001",
          "and it names which fragment would do it")

    print()
    print("The five ways it can be unavailable, and none of them is the others")
    check(verdict(live, "NOT_IN_THE_LIBRARY").verdict == rt.NO_PROVIDER,
          "nothing provides it at all -> NO_PROVIDER, a gap rather than a fault")
    check(verdict(live, "NEW_ONLY").verdict == rt.UNSUPPORTED_RELEASE,
          "a 2027-only provider asked about on 2024 -> UNSUPPORTED_RELEASE")
    check(verdict(live, "MOVE_ELEMENTS").verdict == rt.BLOCKED_BY_TRUST,
          "MODIFY with the write gate closed -> BLOCKED_BY_TRUST, not unavailable")
    check("write.enabled" in verdict(live, "MOVE_ELEMENTS").detail,
          "and it names the setting, so the answer is actionable")

    idle = rt.snapshot(ASKED, LIBRARY, release="2024", bridge="none")
    check(verdict(idle, "SET_SELECTION").verdict == rt.NEEDS_REVIT,
          "permitted, with no Revit answering -> NEEDS_REVIT")
    check("Heron AI > Heron" in verdict(idle, "SET_SELECTION").detail,
          "and it says which button to press")

    blind = rt.snapshot(ASKED, LIBRARY)
    check(verdict(blind, "SET_SELECTION").verdict == rt.NOT_DETECTED,
          "no release known -> NOT_DETECTED. Unknown is not the same as no")

    print()
    print("The distinctions that would be easy to lose")
    check(verdict(blind, "NOT_IN_THE_LIBRARY").verdict == rt.NO_PROVIDER,
          "NO_PROVIDER survives not knowing the release - it is certain either way")
    check(verdict(blind, "NEW_ONLY").verdict != rt.UNSUPPORTED_RELEASE,
          "a 2027-only provider is NOT reported unsupported when nothing said "
          "which Revit this is - that would be a guess presented as a finding")
    check(verdict(idle, "MOVE_ELEMENTS").verdict == rt.BLOCKED_BY_TRUST,
          "trust is answered before the bridge: a refused operation stays refused "
          "whether or not a Revit is connected")

    open_gate = rt.snapshot(ASKED, LIBRARY, release="2024", bridge="connected",
                            write_enabled=True)
    check(verdict(open_gate, "MOVE_ELEMENTS").verdict == rt.AVAILABLE,
          "opening the write gate makes MODIFY available and nothing else changes")
    check(verdict(open_gate, "NEW_ONLY").verdict == rt.UNSUPPORTED_RELEASE,
          "and it does not make an unsupported release supported")

    print()
    print("The risk of a capability is its worst provider, never its first")
    mixed = rt.snapshot(["MIXED"],
                        {"MIXED": [dict(READER, id="FRG-A"), dict(WRITER, id="FRG-B",
                                                                  revit=["2024"])]},
                        release="2024", bridge="connected")
    check(verdict(mixed, "MIXED").verdict == rt.BLOCKED_BY_TRUST,
          "a capability with one READ and one MODIFY provider is judged by the "
          "MODIFY one")

    print()
    print("The environment is stated, never implied")
    env = rt.snapshot([], {}).environment
    check(env["revit release"] == "not detected" and env["bridge"] == "not detected",
          "an unknown fact reads as 'not detected', never as a default value")
    check(env["risk ceiling"] == rt.CEILING_WITHOUT_WRITE,
          "the ceiling is derived from the write gate, so a caller cannot forget it")

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1
    print("PASSED - six verdicts, six meanings, and unknown is not no.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
