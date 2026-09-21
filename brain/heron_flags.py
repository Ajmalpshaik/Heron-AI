# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-FLG-009
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Feature Flags - three states, and only a person may move one.

    python brain/heron_flags.py

WHAT IT IS FOR (docs/28, HERON-OPS-FLG-009)
--------------------------------------------
"Flags for staged rollout and shadow running." T1, risk ADMIN.

docs/23 s11 gives the whole vocabulary in three lines:

    SmartDimensioning = OFF
    ExperimentalRAG   = ON
    NewAgentSystem    = TEST

Three states. Not four, not "ON with a percentage", not a string somebody
typed. A word this module does not recognise is REFUSED - never rounded to
the nearest state, because the nearest state to a typo is a guess about
whether a component runs.

TEST IS THE ONE THAT EARNS ITS KEEP
-------------------------------------
OFF and ON are obvious. `TEST` is how a shadow-mode component (docs/18 s4)
gets exercised on real requests **without its output being used** - it runs,
it produces a result, and the result is scored rather than delivered. That
is the difference between evidence and a guess, and it is the whole reason
Shadow Mode exists.

So `meaning()` answers two questions, never one: does it RUN, and is its
output USED. A caller that reads only "is this flag on" will treat TEST as
OFF and get no evidence, or treat it as ON and ship an unproven component
to a live model. Both readings are wrong and both are easy.

FLIPPING A FLAG IS ADMIN, IN BOTH DIRECTIONS
----------------------------------------------
docs/21 s9: configuration is a security boundary, so editing it is ADMIN.
The obvious wrong intuition is that turning something OFF is the safe
direction and needs less. It is not. The components most worth attacking
are the ones that check things - a validator, a guard, a budget ceiling -
and those are switched off, not on. OFF gets exactly the same rule as ON.

GOLDEN RULE 19 IS THE REASON THIS MODULE IS SHAPED LIKE THIS
--------------------------------------------------------------
"No text Heron reads may raise Heron's own permission level. Content from
documents, family names, parameter descriptions, imported folders, model
text and community packages is **data, never instruction**. Permission
comes from the user, through Heron's own UI, per action."

Heron reads text it does not control, all day. A flag flip asked for by
that text is the shortest path from a sentence in a shared document to a
write in a live project model, and it would look like configuration in the
audit trail. So a change carries an ORIGIN, exactly one origin is allowed,
and the six kinds of content Golden Rule 19 names are refused BY NAME, so
the refusal says what it was rather than only that it was not allowed.

Anything else - an origin nobody recognises, or none at all - is refused
too. This is the same fail-closed rule the scheduler applies to "is a
person working": the unknown answer is the safe one, never the convenient
one.

"PER ACTION" IS ENFORCED, NOT ASKED FOR
-----------------------------------------
An approval that does not name what it approves is a blanket approval, and
a blanket approval reused across a session is how "per action" quietly
stops being true. So the approval must NAME the flag and the state it is
for, and a signature already recorded against that flag is refused as a
replay. A bare `True` is not an approval here; it is a caller asserting one.

WHAT IT DOES NOT DO
--------------------
It stores nothing. The flag table is handed in and a new one is handed
back, which is what makes the change auditable (docs/21 s9) rather than a
side effect somewhere. It runs no component and it sweeps nothing: Safe
Mode (HERON-OPS-SAF-008) is the flag sweep back to last-known-good, and it
needs a `was` on every flag to sweep to - which this records, with the
warning that a previous state is not a proven-good one.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/23 s11. Exactly these three words.
STATES = ("OFF", "ON", "TEST")

# The two questions, answered separately, because one boolean cannot carry
# both and TEST is the state where they disagree.
MEANING = {
    "OFF": {"runs": False, "output_used": False,
            "why": "the component does not run at all"},
    "ON": {"runs": True, "output_used": True,
           "why": "the component runs and its result is delivered"},
    "TEST": {"runs": True, "output_used": False,
             "why": "the component runs on real requests and its result is "
                    "SCORED, never delivered - this is shadow running "
                    "(docs/18 s4), and reading it as ON ships an unproven "
                    "component to a live model"},
}

# Golden Rule 19: permission comes from the user, through Heron's own UI,
# per action. One origin. Everything else, recognised or not, is refused.
THE_ONE_ORIGIN = "user"

# The six kinds of content Golden Rule 19 names, so a refusal can say what
# the request actually was rather than only that it was not the user.
DATA_NEVER_INSTRUCTION = (
    ("document", "text in a document Heron read"),
    ("family", "a family name, which anybody who can save a family can write"),
    ("parameter", "a parameter description, which travels inside the model"),
    ("import", "an imported folder, whose contents Heron did not write"),
    ("folder", "an imported folder, whose contents Heron did not write"),
    ("model", "text inside a Revit model - a comment, a note, a type name"),
    ("package", "a community package, which is untrusted by default "
                "(docs/00d s37)"),
    ("community", "a community package, which is untrusted by default "
                  "(docs/00d s37)"),
)


def meaning(state):
    """
    {runs, output_used, why} for one state - or None for a word that is not
    one of the three. Never a best guess: see the module docstring.
    """
    state = str(state or "").strip().upper()
    if state not in MEANING:
        return None
    return dict(MEANING[state], state=state)


# What a caller is asking for, when the caller did not say. Deliberately
# vague rather than wrong: a refusal that names the wrong act is worse than
# one that names none, because naming the wrong act sends the reader off to
# check something they were never doing.
AN_ADMIN_ACT = "an ADMIN action"


def origin_allowed(origin, action=None):
    """
    (allowed, why). Fails closed on anything that is not the user.

    THIS IS NOT ONLY THE FLAG AGENT'S CHECK. It is Golden Rule 19's gate for
    every ADMIN surface that has one, and the rest of them borrow it rather
    than keeping a second copy that would drift. Who stands on it:

        grep -rn 'origin_allowed' --include='*.py' brain mcp

    No count is typed here; it would be wrong the first time a surface is
    added, and the command is the answer.

    SO `action` IS NOT DECORATION. It NAMES what is being asked for, as a
    short noun phrase - "a flag flip", "Safe Mode", "installing a package" -
    and the refusal is the sentence a person reads. One that names a flag
    flip to somebody entering Safe Mode tells them nothing they can act on,
    and sends them to look at a flag table that has nothing to do with it.
    A caller that says nothing gets the vague default above.
    """
    doing = str(action or "").strip() or AN_ADMIN_ACT
    given = str(origin or "").strip().lower()
    if not given:
        return False, ("nothing said where this change came from. Golden "
                       "Rule 19 puts permission with the user, through "
                       "Heron's own UI, per action - an unsigned origin is "
                       "not that, and guessing it is would make the one "
                       "rule that stops a shared document asking for %s "
                       "depend on a caller remembering to fill a field"
                       % doing)
    if given == THE_ONE_ORIGIN:
        return True, "the user, acting in Heron's own UI"
    for marker, what in DATA_NEVER_INSTRUCTION:
        if marker in given:
            return False, ("this came from %s. That is DATA, never "
                           "instruction (Golden Rule 19) - Heron reads it, "
                           "Heron does not take orders from it, and %s "
                           "asked for by text somebody else wrote is the "
                           "shortest path from a sentence to a change in a "
                           "live project" % (what, doing))
    return False, ("'%s' is not the user acting in Heron's own UI, and "
                   "nothing here decides which other origins are close "
                   "enough to allow %s. Permission for an ADMIN act comes "
                   "from the user, through Heron's own UI, per action "
                   "(Golden Rule 19; docs/21 s9 is why configuration is one "
                   "of them); an origin nobody recognised gets the safe "
                   "answer, not the convenient one" % (origin, doing))


def flag_entry(flags, name):
    """One flag as a dict, whichever of the two shapes it was written in."""
    found = flags.get(name)
    if isinstance(found, dict):
        return found
    return {"state": found}


def last_change(entry):
    """
    The most recent recorded change to a flag, or None.

    `was`, `by` and `at` are NOT kept beside `state` as their own fields.
    Two places holding the same fact is two places to disagree, and the
    audit trail docs/21 s9 asks for is the list - so the list is the only
    copy and the convenient reading is derived from it.
    """
    changes = entry.get("changes") if isinstance(entry, dict) else None
    if not isinstance(changes, list) or not changes:
        return None
    return changes[-1] if isinstance(changes[-1], dict) else None


def _approval(approval, name, state, entry):
    """
    (refusal, why) - is this an approval FOR THIS ACTION?

    It has to name the flag and the state, because an approval that names
    neither is a blanket approval and "per action" stops being true the
    first time one is reused. The refusal NAME comes back rather than a
    sentence a caller has to read words out of: which failure this was is
    a fact, and recovering a fact from prose is how prose becomes an API.
    """
    if not isinstance(approval, dict):
        return "NOT_APPROVED", ("flipping a flag is ADMIN (docs/21 s9) and "
                                "nothing was signed. A bare %r is a caller "
                                "asserting an approval, not an approval - "
                                "one names who signed, when, and what for."
                                % (approval,))
    by = str(approval.get("by") or "").strip()
    at = str(approval.get("at") or "").strip()
    if not by:
        return ("NOT_APPROVED",
                "the approval names nobody. Somebody signs, or nobody did.")
    if not at:
        return "NOT_APPROVED", ("the approval carries no time, so the audit "
                                "trail docs/21 s9 asks for would say a flag "
                                "changed and not when.")
    for field, expected in (("flag", name), ("state", state)):
        said = str(approval.get(field) or "").strip()
        if not said:
            return "NOT_APPROVED", ("the approval does not say which %s it "
                                    "is for. An approval that names neither "
                                    "the flag nor the state is a blanket "
                                    "one, and a blanket approval reused "
                                    "across a session is how 'per action' "
                                    "quietly stops being true." % field)
        if said.upper() != str(expected).upper():
            return "NOT_APPROVED", ("the approval is for %s %s, and this is "
                                    "%s %s." % (field, said, field, expected))

    # REPLAY. Checked against EVERY change this flag has recorded, not only
    # the last one: a signature reused after somebody else moved the flag
    # back is the replay worth catching, and comparing against the current
    # state alone would wave it straight through.
    for earlier in (entry.get("changes") or []):
        if not isinstance(earlier, dict):
            continue
        if (str(earlier.get("by") or "").strip(),
                str(earlier.get("at") or "").strip()) == (by, at):
            return "APPROVAL_ALREADY_USED", (
                "this exact signature - %s at %s - is already recorded "
                "against %s, when it moved to %s. One person's single "
                "decision applied twice is not two decisions."
                % (by, at, name, earlier.get("to")))
    return None, "%s signed for %s = %s at %s" % (by, name, state, at)


def read(flags, name):
    """
    {state, runs, output_used} for one flag - or a refusal that still fails
    closed.

    A flag nobody declared comes back REFUSED **and** OFF. The refusal is
    there because answering a typo with a silent OFF leaves a component
    switched off forever with nothing to say why; the OFF is there because
    a caller that ignores the refusal should still get the safe reading.
    """
    table = flags if isinstance(flags, dict) else None
    if table is None:
        return {"refused": "NO_FLAGS", "state": "OFF", "runs": False,
                "output_used": False,
                "why": "no flag table was given. Reading OFF, because a "
                       "component whose flag cannot be found does not run."}

    name = str(name or "").strip()
    if name not in table:
        return {"refused": "FLAG_NOT_DECLARED", "state": "OFF", "runs": False,
                "output_used": False,
                "why": "'%s' is not a declared flag. Reading OFF, which is "
                       "the safe answer, but saying so rather than only "
                       "answering OFF - a typo answered silently leaves a "
                       "component switched off forever and nothing ever "
                       "says why." % name}

    state = flag_entry(table, name).get("state")
    found = meaning(state)
    if found is None:
        return {"refused": "NOT_A_FLAG_STATE", "state": "OFF", "runs": False,
                "output_used": False,
                "why": "'%s' holds %r, which is not one of %s. Reading OFF: "
                       "the nearest state to a word nobody recognises is a "
                       "guess about whether a component runs."
                       % (name, state, ", ".join(STATES))}

    return {"state": found["state"], "runs": found["runs"],
            "output_used": found["output_used"],
            "why": "%s = %s: %s" % (name, found["state"], found["why"])}


def set_flag(flags, name, state, origin=None, approval=None):
    """
    {flags, changed, was, why, unjudged} - or a refusal. Nothing is stored.

    The table goes in and a new one comes back, so the change is a value a
    caller can record, diff and undo, rather than a side effect somewhere
    that an audit has to reconstruct (docs/21 s9).
    """
    if not isinstance(flags, dict):
        return {"changed": False, "refused": "NO_FLAGS",
                "why": "no flag table was given, so there is nothing to "
                       "change and nothing to change it in."}

    name = str(name or "").strip()
    if name not in flags:
        return {"changed": False, "refused": "FLAG_NOT_DECLARED",
                "why": "'%s' is not a declared flag. Setting it would "
                       "CREATE one, and a flag created by a typo is a "
                       "component that stays off forever while its real "
                       "flag reads exactly as expected." % name,
                "proposal": "declare %s in the configuration first, with "
                            "the component it gates." % name}

    wanted = meaning(state)
    if wanted is None:
        return {"changed": False, "refused": "NOT_A_FLAG_STATE",
                "why": "%r is not one of %s. docs/23 s11 gives three states "
                       "and this agent does not round to the nearest one - "
                       "the nearest state to a typo is a guess about "
                       "whether a component runs."
                       % (state, ", ".join(STATES))}

    allowed, why_origin = origin_allowed(origin, "a flag flip")
    if not allowed:
        return {"changed": False, "refused": "NOT_FROM_THE_USER",
                "why": why_origin,
                "proposal": "ask the user, in Heron's own UI, for this flag "
                            "and this state. Golden Rule 19 puts the "
                            "permission there and nowhere else."}

    entry = flag_entry(flags, name)
    was = str(entry.get("state") or "").strip().upper()

    # A change that changes nothing does not spend an approval and does not
    # write a line in the audit trail saying a flag moved when it did not.
    if was == wanted["state"]:
        return {"changed": False, "flags": flags, "was": was,
                "why": "%s is already %s. Nothing moved, so nothing was "
                       "signed for and nothing is recorded." % (name, was),
                "unjudged": ["whether it SHOULD be %s is not this agent's "
                             "question" % was]}

    unsigned, why_approval = _approval(approval, name, wanted["state"], entry)
    if unsigned:
        return {"changed": False, "refused": unsigned,
                "why": why_approval,
                "proposal": "sign for this flag and this state, once. "
                            "Flipping a flag is ADMIN in BOTH directions - "
                            "the components most worth switching off are "
                            "the ones that check things."}

    change = {"to": wanted["state"], "was": was,
              "by": str(approval.get("by")).strip(),
              "at": str(approval.get("at")).strip()}
    fresh = dict(flags)
    fresh[name] = {"state": wanted["state"],
                   "changes": list(entry.get("changes") or []) + [change]}
    return {
        "changed": True, "flags": fresh, "was": was, "now": wanted["state"],
        "change": change,
        "runs": wanted["runs"], "output_used": wanted["output_used"],
        "why": "%s: %s -> %s. %s. %s."
               % (name, was or "(unset)", wanted["state"], why_origin,
                  why_approval),
        "unjudged": [
            "`was` is the PREVIOUS state, which is not the same as a "
            "last-known-good one - nothing here scores a flag as good. Safe "
            "Mode (HERON-OPS-SAF-008) sweeps back to last-known-good, and "
            "what this leaves it is a dated list of what the flag HAS been.",
            "a flag carried in as a bare word has no recorded changes, so "
            "its first change here is also the first thing anything knows "
            "about it. That is a gap in the audit trail, not a clean start.",
            "whether the component behind %s is ready for %s is not asked "
            "here. A flag says what runs; the deployment ladder "
            "(HERON-AHR-DEP-013) says what is allowed to."
            % (name, wanted["state"]),
        ],
    }


def survey(flags):
    """
    {on, test, off, unreadable, why} - the table at a glance.

    `test` is listed apart from `on` because they are not the same thing
    and a report that adds them together is a report saying an unproven
    component is live.
    """
    if not isinstance(flags, dict) or not flags:
        return {"refused": "NO_FLAGS",
                "why": "no flags were given. An empty survey reads as a "
                       "system with nothing behind a flag, which is a "
                       "different thing from a system nobody asked about."}

    found = {"on": [], "test": [], "off": [], "unreadable": []}
    for name in sorted(flags):
        answer = read(flags, name)
        if answer.get("refused"):
            found["unreadable"].append({"flag": name,
                                        "refused": answer["refused"],
                                        "why": answer["why"]})
        else:
            found[answer["state"].lower()].append(name)
    found["why"] = ("%d live, %d shadow running (result scored, not used), "
                    "%d off, %d unreadable."
                    % (len(found["on"]), len(found["test"]),
                       len(found["off"]), len(found["unreadable"])))
    return found


def main(argv):
    print("FEATURE FLAGS   three states, and only a person may move one")
    print("=" * 72)

    flags = {"SmartDimensioning": {"state": "OFF"},
             "ExperimentalRAG": {"state": "ON"},
             "NewAgentSystem": {"state": "TEST"}}

    for name in sorted(flags):
        answer = read(flags, name)
        print("  %-20s %-5s runs=%-5s output used=%-5s"
              % (name, answer["state"], answer["runs"], answer["output_used"]))
    print("  %s" % survey(flags)["why"])

    print()
    print("  TEST is the one that earns its keep:")
    print("    %s" % MEANING["TEST"]["why"][:96])

    print()
    print("  Golden Rule 19 - the same flip, asked for by different things:")
    for origin in ("a document Heron read", "a family name",
                   "an imported folder", "a community package",
                   "a script", None, "user"):
        answer = set_flag(flags, "SmartDimensioning", "ON", origin=origin,
                          approval={"by": "ajmal", "at": "2026-09-14T10:00Z",
                                    "flag": "SmartDimensioning",
                                    "state": "ON"})
        print("    %-24s %s" % (origin or "(nothing said)",
                                answer.get("refused") or "CHANGED"))

    print()
    print("  And 'per action' is enforced rather than asked for:")
    for approval, label in (
            (True, "a bare True"),
            ({"by": "ajmal", "at": "x"}, "signed, names no flag"),
            ({"by": "ajmal", "at": "x", "flag": "ExperimentalRAG",
              "state": "ON"}, "signed for a different flag"),
            ({"by": "ajmal", "at": "x", "flag": "SmartDimensioning",
              "state": "TEST"}, "signed for a different state")):
        answer = set_flag(flags, "SmartDimensioning", "ON", origin="user",
                          approval=approval)
        print("    %-30s %s" % (label, answer.get("refused")))
        print("        %s" % answer["why"][:86])

    print()
    print("  A signature is spent once, even after the flag moved back:")
    signed = {"by": "ajmal", "at": "2026-09-14T10:00Z",
              "flag": "SmartDimensioning", "state": "ON"}
    on = set_flag(flags, "SmartDimensioning", "ON", origin="user",
                  approval=signed)
    off = set_flag(on["flags"], "SmartDimensioning", "OFF", origin="user",
                   approval={"by": "sam", "at": "2026-09-14T11:00Z",
                             "flag": "SmartDimensioning", "state": "OFF"})
    again = set_flag(off["flags"], "SmartDimensioning", "ON", origin="user",
                     approval=signed)
    print("    OFF -> ON (ajmal), ON -> OFF (sam), then ajmal's slip resent:")
    print("    %s - %s" % (again["refused"], again["why"][:76]))
    print("    Checked against every change the flag records, not the last")
    print("    one: comparing with the current state alone would wave this")
    print("    straight through, and that is the replay worth catching.")

    print()
    print("  Turning one OFF is the same rule, on purpose:")
    answer = set_flag(flags, "ExperimentalRAG", "OFF", origin="user")
    print("    %s - %s" % (answer["refused"], answer["why"][:70]))
    print("  The components most worth switching off are the ones that check")
    print("  things: a validator, a guard, a budget ceiling. OFF is not the")
    print("  safe direction and it does not get the easier rule.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
