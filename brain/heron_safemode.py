# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-SAF-008
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Safe Mode - a sweep back to a moment the user says was good.

    python brain/heron_safemode.py

WHAT IT IS FOR (docs/28, HERON-OPS-SAF-008)
--------------------------------------------
"Disables recent components, returns to last-known-good." T1, risk ADMIN.

docs/00d s35 calls it Emergency Recovery Mode: disable recently installed
agents, plugins, skills and fragments, and return to the last known-good
configuration. docs/23 s11 says what that second half actually is - **Safe
Mode is a flag sweep back to last-known-good** - which is why this agent
is built on the flag table (HERON-OPS-FLG-009) rather than beside it.

WHO DECIDES WHAT "GOOD" MEANS, AND IT IS NOT THIS AGENT
--------------------------------------------------------
"Last-known-good" is the phrase that would quietly turn this into a guess.
Nothing in this repository scores a flag as good. The flag table records
what a flag HAS been and who moved it; it does not record whether any of
those states worked - HERON-OPS-FLG-009 says so in its own answer rather
than letting the next agent assume otherwise.

So the goodness comes from the person: they name a MOMENT - "it was fine
yesterday afternoon" - and everything changed since then is undone. The
machine does the undoing and the arithmetic; the human supplies the only
judgement in the whole operation. There is no default window, because a
default window is this agent guessing when the trouble started.

A SWEEP BACK IS NOT "TURN EVERYTHING OFF"
-------------------------------------------
The obvious reading of a safety mode is that it disables things, and for
newly installed components that is exactly right. For flags it is wrong and
dangerously so. A flag can gate a CHECK - a validator, a guard, a budget
ceiling - and the change that broke the system may have been somebody
turning one of those OFF. Sweeping to OFF would repeat the damage and call
it recovery.

So the sweep RESTORES: each flag goes back to the state it held at the
named moment, whatever that state was. A guard switched off an hour ago
comes back ON. That is what docs/00d s35's "last known-good configuration"
means, and the direction is a consequence of the history rather than a
policy this agent applies.

IT REFUSES WHAT IT CANNOT DATE, RATHER THAN SWEEPING IT ANYWAY
----------------------------------------------------------------
A flag with no recorded history has nothing to go back to. A change with a
timestamp this agent cannot compare cannot be placed before or after the
moment. In both cases it is named and left alone, because the failure of
the alternative is silent: a flag quietly left in its broken state while
the report says the system was restored is worse than a flag the report
says it could not judge.

IT CHANGES NOTHING ITSELF
--------------------------
It returns the swept flag table and the list of components to disable. A
caller writes them. This is the same line HERON-OPS-HEA-006 draws for the
same reason - the agent that decides what should be undone is not also the
process that undoes it - and it keeps the whole operation a value somebody
can read before it takes effect.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import heron_flags as FLG                                      # noqa: E402

# What docs/00d s35 names. Anything else in the list is carried, reported
# and not dropped - this is for the report, not a filter.
RECENTLY_INSTALLED = ("agent", "plugin", "skill", "fragment")

# The one action this agent performs, and what an approval must name.
THE_ACTION = "safe-mode"


def _moment(when):
    """
    (ok, why) - can this timestamp be placed before or after another?

    Only a date this agent can COMPARE is accepted. A timestamp in some
    other shape would sort as a string and quietly put a change on the
    wrong side of the moment, leaving a broken flag in place under a
    report saying the system was restored.
    """
    text = str(when or "").strip()
    if not text:
        return False, "nothing was given"
    if len(text) < 10 or text[4] != "-" or text[7] != "-":
        return False, ("'%s' is not YYYY-MM-DD..., and this agent compares "
                       "timestamps as text - a different shape sorts wrongly "
                       "and puts a change on the wrong side of the moment"
                       % text)
    for part, size in ((text[0:4], 4), (text[5:7], 2), (text[8:10], 2)):
        if not part.isdigit() or len(part) != size:
            return False, "'%s' is not a date" % text
    return True, text


def _approval(approval, since):
    """
    (refusal, why) - is this signed, for THIS moment?

    Entering Safe Mode is ONE action - the flags it moves are consequences
    of that decision, not separate decisions - so it is signed once. What
    keeps it "per action" (Golden Rule 19) is that the signature names the
    moment: an approval to undo everything since Tuesday cannot be reused
    to undo everything since last year.
    """
    if not isinstance(approval, dict):
        return "NOT_APPROVED", ("entering Safe Mode is ADMIN (docs/21 s9) "
                                "and nothing was signed. It undoes work - "
                                "that is a decision somebody makes, not a "
                                "recovery that happens.")
    if not str(approval.get("by") or "").strip():
        return "NOT_APPROVED", "the approval names nobody."
    if not str(approval.get("at") or "").strip():
        return "NOT_APPROVED", ("the approval carries no time, so the audit "
                                "trail would say the system was rolled back "
                                "and not when it was decided.")
    said = str(approval.get("since") or "").strip()
    if not said:
        return "NOT_APPROVED", ("the approval does not say which moment it "
                                "is for. An approval that names no moment is "
                                "an approval to undo any amount of work.")
    if said != since:
        return "NOT_APPROVED", ("the approval is to undo everything since "
                                "%s, and this would undo everything since "
                                "%s." % (said, since))
    return None, "%s signed to undo everything since %s" % (
        str(approval.get("by")).strip(), since)


def _target(entry, since):
    """
    (state, refusal, why) - what this flag held at the named moment.

    Walks the recorded changes backwards, discarding every one made at or
    after the moment. The answer is what the earliest discarded change
    moved AWAY from, which is the state in effect when the moment passed.
    """
    changes = entry.get("changes") if isinstance(entry, dict) else None
    if not isinstance(changes, list) or not changes:
        return None, "NO_HISTORY", ("nothing records what this flag has "
                                    "been, so there is no state to go back "
                                    "to. It is left exactly as it is: a flag "
                                    "quietly moved to a state nobody can "
                                    "vouch for is what this mode exists to "
                                    "undo.")
    target, undone = None, 0
    for change in reversed(changes):
        if not isinstance(change, dict):
            return None, "UNDATED_CHANGE", ("a recorded change is not a "
                                            "record, so the history cannot "
                                            "be walked.")
        ok, why = _moment(change.get("at"))
        if not ok:
            return None, "UNDATED_CHANGE", ("a change to this flag is dated "
                                            "%s and cannot be placed before "
                                            "or after %s. Leaving it alone: "
                                            "a wrong comparison here is "
                                            "silent." % (why, since))
        if why < since:
            break
        target = str(change.get("was") or "").strip().upper()
        undone += 1
    if not undone:
        return None, None, "nothing moved this flag since %s" % since
    return target, None, ("%d change%s since %s, back to %s"
                          % (undone, "" if undone == 1 else "s", since,
                             target or "(unset)"))


def enter(flags, since=None, components=None, origin=None, approval=None):
    """
    {flags, disable, swept, could_not, why, unjudged} - or a refusal.

    Nothing is written. The swept table and the list to disable come back
    as values, so somebody can read what Safe Mode would undo before it
    takes effect.
    """
    if not isinstance(flags, dict):
        return {"entered": False, "refused": "NO_FLAGS",
                "why": "no flag table was given, and a flag sweep with no "
                       "flags is not a recovery."}

    ok, why_moment = _moment(since)
    if not str(since or "").strip():
        return {"entered": False, "refused": "NO_MOMENT",
                "why": "no moment was named. 'Last-known-good' is not "
                       "something this agent knows - nothing here scores a "
                       "flag as good - it is a moment the person says was "
                       "good, and there is no default because a default is "
                       "this agent guessing when the trouble started.",
                "proposal": "ask when it was last working, and pass that."}
    if not ok:
        return {"entered": False, "refused": "BAD_MOMENT",
                "why": "the moment %s. Everything in this operation turns on "
                       "which side of it a change falls." % why_moment}
    since = why_moment

    allowed, why_origin = FLG.origin_allowed(origin, "Safe Mode")
    if not allowed:
        return {"entered": False, "refused": "NOT_FROM_THE_USER",
                "why": why_origin,
                "proposal": "ask the user, in Heron's own UI. Safe Mode "
                            "undoes work and switches components off - text "
                            "Heron read is the last thing that should be "
                            "able to ask for it (Golden Rule 19)."}

    unsigned, why_approval = _approval(approval, since)
    if unsigned:
        return {"entered": False, "refused": unsigned, "why": why_approval}

    # THE FLAGS. Each goes back to what it held at the moment - which may
    # mean switching a guard back ON.
    swept, could_not, fresh = [], [], dict(flags)
    for name in sorted(flags):
        entry = FLG.flag_entry(flags, name)
        now = str(entry.get("state") or "").strip().upper()
        target, refusal, why = _target(entry, since)
        if refusal:
            could_not.append({"flag": name, "state": now, "refused": refusal,
                              "why": why})
            continue
        if target is None or target == now:
            continue
        fresh[name] = {"state": target,
                       "changes": list(entry.get("changes") or [])
                       + [{"to": target, "was": now,
                           "by": str(approval.get("by")).strip(),
                           "at": str(approval.get("at")).strip(),
                           "safe_mode_since": since}]}
        swept.append({"flag": name, "was": now, "to": target, "why": why})

    # THE COMPONENTS. Recent means installed at or after the same moment.
    disable = []
    for component in (components or []):
        if not isinstance(component, dict):
            could_not.append({"component": component,
                              "refused": "UNDATED_COMPONENT",
                              "why": "not a record, so it has no date"})
            continue
        found, why = _moment(component.get("installed"))
        if not found:
            could_not.append({"component": component.get("id"),
                              "refused": "UNDATED_COMPONENT",
                              "why": "nothing says when this was installed "
                                     "(%s), so it cannot be called recent or "
                                     "settled. Left alone and named." % why})
            continue
        if why >= since:
            disable.append({"component": component.get("id"),
                            "kind": component.get("kind"),
                            "installed": why,
                            "why": "installed %s, at or after %s"
                                   % (why, since)})

    if not swept and not disable:
        return {"entered": False, "refused": "NOTHING_TO_UNDO",
                "could_not": could_not,
                "why": "nothing changed since %s that this agent can undo. "
                       "That is not the same as a healthy system, and %d "
                       "thing(s) could not be judged at all."
                       % (since, len(could_not))}

    return {
        "entered": True, "flags": fresh, "swept": swept, "disable": disable,
        "could_not": could_not,
        "why": "%d flag(s) swept back to %s, %d component(s) to disable, %d "
               "left alone because nothing could date them. %s."
               % (len(swept), since, len(disable), len(could_not),
                  why_approval),
        "unjudged": [
            "'good' came from the person who named %s, not from this agent. "
            "Nothing here scores a state as working - it restores what was "
            "in effect, which is a different claim and the honest one."
            % since,
            "the sweep RESTORES rather than disables: a flag gating a check "
            "that somebody switched off comes back ON. Reading Safe Mode as "
            "'turn everything off' would repeat the damage and call it "
            "recovery.",
            "nothing was written. The swept table and the disable list are "
            "values - a caller applies them, and until it does the system "
            "is exactly as it was.",
            "disabling a component is named here and not done. What it takes "
            "to unload a running agent, plugin, skill or fragment belongs to "
            "whatever loaded it.",
        ],
    }


def main(argv):
    print("SAFE MODE   a sweep back to a moment the user says was good")
    print("=" * 72)

    since = "2026-09-14T09:00Z"
    flags = {
        # Somebody turned a guard OFF an hour ago. This is the case that
        # makes "turn everything off" the wrong reading of a safety mode.
        "ClashGuard": {"state": "OFF", "changes": [
            {"to": "ON", "was": "OFF", "by": "ajmal", "at": "2026-08-01T09:00Z"},
            {"to": "OFF", "was": "ON", "by": "sam", "at": "2026-09-14T10:00Z"}]},
        "ExperimentalRAG": {"state": "ON", "changes": [
            {"to": "ON", "was": "OFF", "by": "sam", "at": "2026-09-14T11:00Z"}]},
        "SmartDimensioning": {"state": "ON", "changes": [
            {"to": "ON", "was": "OFF", "by": "ajmal", "at": "2026-07-02T09:00Z"}]},
        "NewAgentSystem": {"state": "TEST"},
    }
    components = [
        {"id": "community-duct-sizer", "kind": "fragment",
         "installed": "2026-09-14T10:30Z"},
        {"id": "HERON-MEP-DCT-021", "kind": "agent",
         "installed": "2026-03-11T09:00Z"},
        {"id": "sheet-namer", "kind": "skill"},
    ]
    answer = enter(flags, since=since, components=components, origin="user",
                   approval={"by": "ajmal", "at": "2026-09-14T12:00Z",
                             "since": since})
    print("  %s" % answer["why"])

    print()
    print("  SWEPT BACK:")
    for entry in answer["swept"]:
        print("    %-20s %-5s -> %-5s  %s"
              % (entry["flag"], entry["was"], entry["to"], entry["why"]))
    print("    ClashGuard came back ON. A sweep RESTORES; it does not")
    print("    disable. The change that broke things may have been somebody")
    print("    switching a check off, and sweeping to OFF would repeat it.")

    print()
    print("  DISABLED (named, not done):")
    for entry in answer["disable"]:
        print("    %-24s %-9s %s" % (entry["component"], entry["kind"],
                                     entry["why"]))

    print()
    print("  LEFT ALONE, AND SAID SO:")
    for entry in answer["could_not"]:
        print("    %-24s %s" % (entry.get("flag") or entry.get("component"),
                                entry["refused"]))
        print("        %s" % entry["why"][:84])

    print()
    print("  Who decides what 'good' means:")
    for approval, label in (
            (None, "nothing signed"),
            ({"by": "ajmal", "at": "T"}, "signed, names no moment"),
            ({"by": "ajmal", "at": "T", "since": "2020-01-01"},
             "signed for a different moment")):
        refused = enter(flags, since=since, origin="user", approval=approval)
        print("    %-30s %s" % (label, refused["refused"]))
    print("    %s" % enter(flags, origin="user")["why"][:88])
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
