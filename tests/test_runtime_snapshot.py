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


ADMINISTER = {"id": "FRG-PAR-010", "risk": "ADMIN", "revit": ["2024"], "status": "PROVEN"}
PUBLISHER = {"id": "FRG-EXP-001", "risk": "PUBLISH", "revit": ["2024"], "status": "PROVEN"}
ABOVE = {"MOVE_ELEMENTS": [WRITER], "ADD_PROJECT_PARAMETER": [ADMINISTER],
         "EXPORT_TO_IFC": [PUBLISHER]}


def switched(**switches):
    """A snapshot of ABOVE on a connected 2024 with these switches - or None,
    said as one failure, from a snapshot() that does not take them."""
    try:
        return rt.snapshot(sorted(ABOVE), ABOVE, release="2024",
                           bridge="connected", **switches)
    except TypeError as why:
        check(False, "snapshot() takes the Admin and Publish switches - %s" % why)
        return None


def switches_above_changes():
    """
    D-106 HAS THREE SWITCHES, AND THE SNAPSHOT KNEW ONE (FRAGMENT-ISSUES row
    254, item 5). Changes alone gave a ceiling of ADMIN, so an Admin or
    Publish capability read as permitted with only Changes on - which the
    add-in refuses. Harmless while nothing called it; heron_why serves it
    since PROPOSALS F1, 2026-11-15.
    """
    print()
    print("The switches above Changes each let their own level through (D-106)")
    alone = switched(write_enabled=True)
    if alone is not None:
        check(verdict(alone, "MOVE_ELEMENTS").verdict == rt.AVAILABLE,
              "Changes on lets a MODIFY capability through")
        check(verdict(alone, "ADD_PROJECT_PARAMETER").verdict == rt.BLOCKED_BY_TRUST
              and "admin.enabled" in verdict(alone, "ADD_PROJECT_PARAMETER").detail,
              "and NOT an ADMIN one - it names admin.enabled - found %s: %s"
              % (verdict(alone, "ADD_PROJECT_PARAMETER").verdict,
                 verdict(alone, "ADD_PROJECT_PARAMETER").detail))
        check(verdict(alone, "EXPORT_TO_IFC").verdict == rt.BLOCKED_BY_TRUST
              and "publish.enabled" in verdict(alone, "EXPORT_TO_IFC").detail,
              "nor a PUBLISH one - it names publish.enabled")
    admin = switched(write_enabled=True, admin_enabled=True)
    if admin is not None:
        check(verdict(admin, "ADD_PROJECT_PARAMETER").verdict == rt.AVAILABLE,
              "Changes and Admin on let the ADMIN capability through")
        check(verdict(admin, "EXPORT_TO_IFC").verdict == rt.BLOCKED_BY_TRUST,
              "and still not the PUBLISH one - Admin is not a higher ceiling")
    publish = switched(write_enabled=True, publish_enabled=True)
    if publish is not None:
        check(verdict(publish, "EXPORT_TO_IFC").verdict == rt.AVAILABLE
              and verdict(publish, "ADD_PROJECT_PARAMETER").verdict == rt.BLOCKED_BY_TRUST,
              "Changes and Publish on let PUBLISH through and not ADMIN")
    without = switched(admin_enabled=True, publish_enabled=True)
    if without is not None:
        check(all(verdict(without, n).verdict == rt.BLOCKED_BY_TRUST
                  and "write.enabled" in verdict(without, n).detail for n in ABOVE),
              "and neither does anything with Changes off - each names write.enabled")


def in_revit_words():
    """
    PROPOSALS F1: what heron_why hands a modeller - for each of the six
    verdicts, the reason in Revit words and what can be done about it. Each
    sentence is held to the reason it belongs to, and no two are the same.
    """
    print()
    print("Each verdict in Revit words, and what the modeller can do (PROPOSALS F1)")
    words = getattr(rt, "in_revit_words", None)
    check(words is not None, "heron_runtime has in_revit_words")
    if words is None:
        return
    how = "2 Revits are connected and none has been chosen"
    button = "Heron > AI Bridge > Heron"
    live = rt.snapshot(ASKED, LIBRARY, release="2024", bridge="connected")
    idle = rt.snapshot(ASKED, LIBRARY, release="2024", bridge="none")
    blind = rt.snapshot(ASKED, LIBRARY)
    admin = rt.snapshot(["ADD_PROJECT_PARAMETER"], ABOVE, release="2024",
                        bridge="connected", write_enabled=True)

    def say(snap, name, **kw):
        rows = (LIBRARY.get(name) or ABOVE.get(name))
        return words(verdict(snap, name), rows, **kw)

    cases = [
        ("NO_PROVIDER", say(live, "NOT_IN_THE_LIBRARY", release="2024"),
         ["no way of doing NOT_IN_THE_LIBRARY", "nothing in its library",
          "heron_lookup"]),
        ("UNSUPPORTED_RELEASE", say(live, "NEW_ONLY", release="2024"),
         ["not on Revit 2024", "written for Revit 2027 only",
          "written and checked for Revit 2024"]),
        ("NOT_DETECTED", say(blind, "SET_SELECTION", how=how),
         ["cannot tell which Revit release", how, "not a no", "revit_use_session"]),
        ("BLOCKED_BY_TRUST", say(live, "MOVE_ELEMENTS", release="2024"),
         ["changes the model", "Changes is off", "Heron's Settings in Revit",
          "Revit is right"]),
        ("NEEDS_REVIT", say(idle, "SET_SELECTION", release="2024"),
         ["no Revit 2024 has Heron connected", button, "revit_health"]),
        ("AVAILABLE", say(live, "SET_SELECTION", release="2024"),
         ["Nothing stands in the way of SET_SELECTION on Revit 2024"]),
    ]
    for name, (what, can_do), phrases in cases:
        text = what + " " + can_do
        missing = [p for p in phrases if p not in text]
        check(not missing, "%s reads as its own reason - missing %s"
              % (name, missing or "nothing"))
        check(not any(v in text for v in (rt.AVAILABLE, rt.NO_PROVIDER,
                                          rt.UNSUPPORTED_RELEASE, rt.NOT_DETECTED,
                                          rt.BLOCKED_BY_TRUST, rt.NEEDS_REVIT)),
              "%s is said in Revit words, never as a verdict's name" % name)
    check(len(set(what for _n, (what, _d), _p in cases)) == len(cases),
          "no two of the six reasons are the same sentence")

    upper = words(verdict(admin, "ADD_PROJECT_PARAMETER"),
                  ABOVE["ADD_PROJECT_PARAMETER"], release="2024", write_enabled=True)
    check("Admin is off" in upper[0] and "Changes is off" not in upper[0]
          and "turns Admin on" in upper[1],
          "with Changes on, an Admin capability names the Admin switch, not "
          "Changes - found %r" % (upper[0],))

    away = words(verdict(idle, "SET_SELECTION"), [READER], release="2024",
                 no_revit_here=True)
    check("no Revit on this machine (it is not Windows)" in away[0]
          and "Windows PC" in away[1] and button in away[1],
          "with no Revit on this machine it says so, and that it is done on "
          "the modeller's Windows PC")
    far = rt.snapshot(["MOVE_ELEMENTS"], LIBRARY, release="2024", bridge="none",
                      ceiling=rt.RISK_ORDER[-1])
    there = words(verdict(far, "MOVE_ELEMENTS"), [WRITER], release="2024",
                  no_revit_here=True)
    check("Changes switch must be on there too" in there[1],
          "and for a change, that the owner's Changes switch is needed there too")

    # WHOSE WORD THE SWITCH WAS (Q-70, decided 2026-11-15). When revit_health
    # found a Revit's own settings file is not this PC's, heron_why judges by
    # what that Revit said, and the sentence says so - never "this PC's file".
    print()
    print("Whose word the switch was: this PC's file, or what a Revit said (Q-70)")
    blocked = verdict(live, "MOVE_ELEMENTS")
    try:
        heard = words(blocked, [WRITER], release="2024",
                      said_by="Revit 2024 (session 4242)")
    except TypeError as why:
        heard = ("", "(in_revit_words cannot be told whose word it was: %s)" % why)
    check("Revit 2024 (session 4242) said it has" in heard[1]
          and "revit_health" in heard[1] and "Revit is right" in heard[1],
          "judged by what a Revit said, the sentence names that Revit and when it "
          "was heard, and that Revit is right - found %r" % (heard[1][:120],))
    check("settings file on this PC" not in heard[1],
          "and it does not call that this PC's settings file")
    plain = words(blocked, [WRITER], release="2024")
    check("Heron's settings file on this PC says" in plain[1]
          and "Revit is right" in plain[1],
          "judged by this PC's file, it says so, as it did")


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
    check("Heron > AI Bridge > Heron" in verdict(idle, "SET_SELECTION").detail,
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
    # AND NAMED BY THE SAME ONE. This reported risk MODIFY beside
    # provider FRG-A - the READ fragment, which happened to sort first - so
    # the two facts printed side by side were about two different fragments,
    # and a planner would have blamed the wrong one for being blocked.
    # FRAGMENT-ISSUES section 5b, row 27.
    check(verdict(mixed, "MIXED").risk == "MODIFY",
          "the risk reported is the MODIFY one")
    check(verdict(mixed, "MIXED").provider == "FRG-B",
          "and the provider NAMED is the same fragment that risk came from, "
          "not whichever happened to sort first")

    print()
    print("The environment is stated, never implied")
    env = rt.snapshot([], {}).environment
    check(env["revit release"] == "not detected" and env["bridge"] == "not detected",
          "an unknown fact reads as 'not detected', never as a default value")
    check(env["risk ceiling"] == rt.CEILING_WITHOUT_WRITE,
          "the ceiling is derived from the write gate, so a caller cannot forget it")

    switches_above_changes()
    in_revit_words()

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
