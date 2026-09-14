# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-SAF-008
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Safe Mode - a sweep back to a moment the user says was good.

    python tests/test_safemode.py

WHAT IT PROVES
  1. THE SWEEP RESTORES, IT DOES NOT DISABLE. A flag gating a check that
     somebody switched off comes back ON. Reading a safety mode as "turn
     everything off" would repeat the damage and call it recovery.

  2. GOODNESS COMES FROM THE PERSON. No moment, no sweep - there is no
     default window, because a default is this agent guessing when the
     trouble started.

  3. THE MOMENT IS THE HINGE, AND BOTH SIDES OF IT ARE RIGHT. Changes
     before it stand, changes at or after it are undone, and several
     changes to one flag unwind to the state held at the moment - not to
     the one before the last change.

  4. WHAT IT CANNOT DATE IT LEAVES ALONE AND NAMES. A flag with no
     history, a change with a timestamp it cannot compare, a component
     with no install date. Silence here would be a broken flag left in
     place under a report saying the system was restored.

  5. IT IS ADMIN, SIGNED FOR THIS MOMENT. An approval naming a different
     moment is an approval to undo a different amount of work.

  6. GOLDEN RULE 19 APPLIES, THROUGH THE SAME CHECK THE FLAGS USE - one
     rule in one place, not two that drift.

  7. NOTHING IS WRITTEN AND NOTHING IS UNLOADED.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import copy
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_safemode as SAF                                   # noqa: E402
import heron_flags as FLG                                      # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

SINCE = "2026-09-14T09:00Z"
SIGNED = {"by": "ajmal", "at": "2026-09-14T12:00Z", "since": SINCE}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def change(to, was, at, by="sam"):
    return {"to": to, "was": was, "by": by, "at": at}


def table():
    return {
        # A guard somebody switched OFF after the moment.
        "ClashGuard": {"state": "OFF", "changes": [
            change("ON", "OFF", "2026-08-01T09:00Z"),
            change("OFF", "ON", "2026-09-14T10:00Z")]},
        # Switched on after the moment - goes back off.
        "ExperimentalRAG": {"state": "ON", "changes": [
            change("ON", "OFF", "2026-09-14T11:00Z")]},
        # Settled long before the moment - untouched.
        "SmartDimensioning": {"state": "ON", "changes": [
            change("ON", "OFF", "2026-07-02T09:00Z")]},
    }


def main():
    reached = set()

    source = open(os.path.join(ROOT, "brain", "heron_safemode.py"),
                  encoding="utf-8").read()

    def ask(flags, **kw):
        kw.setdefault("since", SINCE)
        kw.setdefault("origin", "user")
        kw.setdefault("approval", dict(SIGNED))
        answer = SAF.enter(flags, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        for entry in (answer.get("could_not") or []):
            reached.add(entry["refused"])
        return answer

    print("1. The sweep restores, it does not disable")
    answer = ask(table())
    moved = dict((e["flag"], (e["was"], e["to"])) for e in answer["swept"])
    check(moved.get("ClashGuard") == ("OFF", "ON"),
          "a guard switched OFF after the moment comes back ON")
    check(moved.get("ExperimentalRAG") == ("ON", "OFF"),
          "and one switched ON after the moment goes back OFF")
    check(sorted(moved) == ["ClashGuard", "ExperimentalRAG"],
          "so the direction comes from the history, not from a policy")
    check(all(e["to"] != "OFF" or e["was"] != "OFF" for e in answer["swept"]),
          "nothing is swept to a state it was already in")
    check(any("repeat the damage" in note for note in answer["unjudged"]),
          "and the answer says why 'turn everything off' is the wrong "
          "reading of a safety mode")
    check(answer["flags"]["SmartDimensioning"]["state"] == "ON",
          "a flag settled before the moment is untouched")
    for entry in answer["swept"]:
        state = answer["flags"][entry["flag"]]["state"]
        check(state == entry["to"],
              "%s really reads %s in the returned table"
              % (entry["flag"], entry["to"]))
        check(FLG.meaning(state) is not None,
              "and %s is a state the flag agent recognises" % state)

    print()
    print("2. Goodness comes from the person")
    answer = ask(table(), since=None)
    check(answer.get("refused") == "NO_MOMENT", "no moment, no sweep")
    check("no default" in answer["why"],
          "and it says there is no default window")
    check("guessing when the trouble started" in answer["why"],
          "with what a default would actually be")
    check("scores a flag as good" in answer["why"],
          "and that nothing here scores a state as good")
    import inspect
    given = inspect.signature(SAF.enter).parameters
    check(given["since"].default is None,
          "the signature carries no fallback moment - the only default is "
          "nothing, which refuses")
    check(all(given[name].default is None
              for name in ("components", "origin", "approval")),
          "and nothing else is pre-filled either, so no run is half-signed "
          "by the defaults")
    answer = ask(table())
    check(any(SINCE in note and "not from this agent" in note
              for note in answer["unjudged"]),
          "and a successful run repeats where 'good' came from")

    print()
    print("3. The moment is the hinge, and both sides of it are right")
    # THREE changes after the moment. The target is the state held AT the
    # moment - not the one before the last change.
    flags = {"Rolling": {"state": "TEST", "changes": [
        change("OFF", "ON", "2026-01-01T09:00Z"),
        change("ON", "OFF", "2026-09-14T09:30Z"),
        change("TEST", "ON", "2026-09-14T10:30Z")]}}
    answer = ask(flags)
    check(answer["swept"][0]["to"] == "OFF",
          "three changes unwind to the state held at the moment, not the "
          "one before the last change")
    check("2 changes" in answer["swept"][0]["why"],
          "and it says how many were undone")
    # A change EXACTLY at the moment is undone - "since" includes it.
    flags = {"Exactly": {"state": "ON", "changes": [
        change("ON", "OFF", SINCE)]}}
    check(ask(flags)["swept"][0]["to"] == "OFF",
          "a change made exactly at the moment counts as after it")
    # A flag whose only changes are older is not swept at all.
    flags = {"Old": {"state": "ON", "changes": [
        change("ON", "OFF", "2020-01-01T00:00Z")]}}
    answer = SAF.enter(flags, since=SINCE, origin="user",
                       approval=dict(SIGNED))
    check(answer.get("refused") == "NOTHING_TO_UNDO",
          "and a table with nothing changed since the moment is refused")
    reached.add("NOTHING_TO_UNDO")
    check("not the same as a healthy system" in answer["why"],
          "saying that is not the same as a healthy system")

    print()
    print("4. What it cannot date it leaves alone and names")
    flags = dict(table(), NewAgentSystem={"state": "TEST"})
    answer = ask(flags)
    left = dict((e.get("flag") or e.get("component"), e["refused"])
                for e in answer["could_not"])
    check(left.get("NewAgentSystem") == "NO_HISTORY",
          "a flag with no recorded history is left alone")
    check(answer["flags"]["NewAgentSystem"]["state"] == "TEST",
          "and really is left alone, not quietly moved")
    flags = dict(table(), Odd={"state": "ON", "changes": [
        {"to": "ON", "was": "OFF", "by": "sam", "at": "last Tuesday"}]})
    answer = ask(flags)
    left = dict((e.get("flag") or e.get("component"), e["refused"])
                for e in answer["could_not"])
    check(left.get("Odd") == "UNDATED_CHANGE",
          "a change this agent cannot compare is left alone")
    check(any("silent" in e["why"] for e in answer["could_not"]
              if e.get("flag") == "Odd"),
          "because a wrong comparison there would be silent")
    answer = ask(table(), components=[
        {"id": "new-thing", "kind": "fragment", "installed": "2026-09-14T10:00Z"},
        {"id": "old-thing", "kind": "agent", "installed": "2026-01-02T09:00Z"},
        {"id": "no-date", "kind": "skill"},
        {"id": "bad-date", "kind": "plugin", "installed": "yesterday"},
        "not even a record"])
    check([e["component"] for e in answer["disable"]] == ["new-thing"],
          "only the component installed at or after the moment is disabled")
    named = [e.get("component") for e in answer["could_not"]]
    check("no-date" in named and "bad-date" in named,
          "and the undatable ones are named rather than assumed either way")
    check("not even a record" in str(answer["could_not"]),
          "including one that is not a record at all")
    check(all(e["refused"] == "UNDATED_COMPONENT"
              for e in answer["could_not"] if "component" in e),
          "each as UNDATED_COMPONENT")
    check(str(len(answer["could_not"])) in answer["why"],
          "and the sentence says how many were left unjudged")

    print()
    print("5. It is ADMIN, signed for this moment")
    for approval, label in (
            (None, "nothing signed"),
            (True, "a bare True"),
            ({"at": "T", "since": SINCE}, "signed by nobody"),
            ({"by": "a", "since": SINCE}, "signed at no time"),
            ({"by": "a", "at": "T"}, "naming no moment"),
            ({"by": "a", "at": "T", "since": "2020-01-01"},
             "naming a different moment")):
        answer = ask(table(), approval=approval)
        check(answer.get("refused") == "NOT_APPROVED", "%s is refused" % label)
    check("undo a different amount of work" in
          ask(table(), approval={"by": "a", "at": "T",
                                 "since": "2020-01-01"})["why"]
          or "would undo everything since" in
          ask(table(), approval={"by": "a", "at": "T",
                                 "since": "2020-01-01"})["why"],
          "and a mismatched moment says both amounts")
    answer = ask(table())
    check(all(c["changes"][-1].get("safe_mode_since") == SINCE
              for name, c in answer["flags"].items()
              if name in [e["flag"] for e in answer["swept"]]),
          "every swept flag records that Safe Mode moved it, and since when")
    check(all(c["changes"][-1]["by"] == "ajmal"
              for name, c in answer["flags"].items()
              if name in [e["flag"] for e in answer["swept"]]),
          "under the name of whoever signed, so the audit trail has a person")

    print()
    print("6. Golden Rule 19 applies, through the same check")
    for origin in ("a document Heron read", "a community package",
                   "a family name", "a script", None, ""):
        answer = ask(table(), origin=origin)
        check(answer.get("refused") == "NOT_FROM_THE_USER",
              "%r cannot put Heron into Safe Mode" % origin)
    check("FLG.origin_allowed" in source,
          "and it is the flag agent's own check, not a second copy that "
          "would drift from it")
    check(ask(table(), origin="user").get("entered") is True,
          "while the user in Heron's own UI goes through")

    print()
    print("7. Nothing is written and nothing is unloaded")
    before = table()
    kept = copy.deepcopy(before)
    answer = SAF.enter(before, since=SINCE, origin="user",
                       approval=dict(SIGNED),
                       components=[{"id": "x", "kind": "skill",
                                    "installed": "2026-09-14T10:00Z"}])
    check(before == kept, "the table handed in is not modified")
    check(answer["flags"] is not before, "a new one comes back")
    for word in ("subprocess", "os.system", "exec(", "eval(", "open(",
                 "os.remove", "shutil", "import requests", "urllib"):
        check(word not in source, "the source has no %s" % word)
    check(any("nothing was written" in note for note in answer["unjudged"]),
          "and the answer says so rather than leaving it to be assumed")
    check(any("belongs to whatever loaded it" in note
              for note in answer["unjudged"]),
          "including that unloading a component is not this agent's")

    print()
    print("8. Every failure the contract declares is named and reached")
    check(SAF.enter(None, since=SINCE).get("refused") == "NO_FLAGS",
          "no flag table is refused")
    reached.add("NO_FLAGS")
    for bad in ("yesterday", "14-09-2026", "2026/09/14", "2026-9-1",
                "202X-09-14"):
        answer = ask(table(), since=bad)
        check(answer.get("refused") == "BAD_MOMENT",
              "%r is refused as a moment" % bad)
    check("which side of it a change falls" in ask(table(),
                                                   since="yesterday")["why"],
          "and says why the shape of it matters")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-OPS-SAF-008.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    it restores what was in effect, and names what it cannot")
    return 0


if __name__ == "__main__":
    sys.exit(main())
