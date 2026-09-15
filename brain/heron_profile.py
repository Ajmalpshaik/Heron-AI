# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-USR-PRO-001
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
User profile - declared, never inferred, and a mode is granted.

    python brain/heron_profile.py

WHAT IT IS FOR (docs/28, HERON-USR-PRO-001)
--------------------------------------------
"Holds the durable facts: role, discipline, experience level, language,
working style, technical depth. STATE, NOT BEHAVIOUR - Persona reads
this to choose how to speak." T1, risk READ.

The last clause is dead and the rest is not. D-27 is the reason, and it
is recorded as PROPOSALS F19 rather than worked around quietly.

D-27 ABOLISHED PERSONA, AND THREE PLACES STILL REFER TO IT
------------------------------------------------------------
D-27 (2026-08-28) supersedes "the two-persona table in 22 s2" and says
so in its own words:

    "There is no persona. The warning above was right and it argues
    further than it went: if silent switching reads as unreliability,
    the fix is not to display the guess - it is not to guess. Heron has
    ONE VOICE, and what varies is the SHAPE of the answer, read off the
    SHAPE of the request... the same request gets the same shape every
    time."

So nothing reads a profile in order to choose wording, because wording
is not chosen. This agent holds the state and offers nothing about how
to speak: there is no tone field, no level field and no phrasing field
in anything it returns, whatever it is asked.

WHAT SURVIVES D-27 IS THE HARDER HALF
---------------------------------------
    A FACT IS DECLARED      Somebody said it about themselves. Deciding
                            from a conversation that a user is a
                            beginner is a judgement they did not make
                            and cannot see, and D-33 says an unfamiliar
                            input is a question rather than a guess.

    A MODE IS GRANTED       docs/22 s3: modes are a PERMISSION BOUNDARY,
                            not a display preference. Its own note is
                            blunt - "mode cannot be inferred from
                            conversation... conflating the two would let
                            a user TALK THEIR WAY INTO ADMIN." So a mode
                            carries who granted it and when, and one
                            that does not is refused rather than read as
                            User Mode.

That second rule is the whole security value of this agent, and it is
the one the abolished persona used to blur: persona was inferable
BECAUSE it only changed wording. With wording gone, nothing here is
inferable at all.

NOTHING IS STORED HERE
------------------------
A profile is handed in and read. This agent writes nothing - what is
worth remembering, what expires and what must never be stored belongs to
HERON-USR-MEM-002, and putting half of that judgement here would give it
two homes.
"""

from __future__ import annotations

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/28's own list for this agent, in its order.
FACTS = ("role", "discipline", "experience", "language", "working style",
         "technical depth")

# docs/22 s3. A permission boundary, not a display preference.
MODES = {
    "user": "the work, progress, results, and nothing else",
    "developer": "agents, workflows, fragments, skills, RAG, logs, API",
    "admin": "users, permissions, company knowledge, policies, audit logs",
}

# What a granted mode must carry before it is a grant rather than a claim.
A_GRANT_CARRIES = (
    ("by", "who granted it - a person, so it can be withdrawn"),
    ("at", "when, so a grant can be aged and reviewed"),
)

# Words that say a value was worked out rather than said. Any of them and
# the fact is refused - the same rule HERON-NAM-KEY-005 applies to a term.
NOT_DECLARED = ("inferred", "derived", "guessed", "detected", "assumed",
                "estimated", "observed", "automatic", "auto", "model")


def _declared(who):
    """Whether a fact's `by` names somebody rather than a process."""
    name = str(who or "").strip().lower()
    if not name:
        return False
    return not any(word == name or word in name.split()
                   for word in NOT_DECLARED)


def read(profile):
    """
    {facts, mode, unstated, why} - or a refusal.

    Nothing is written, nothing is inferred, and nothing about HOW TO
    SPEAK comes back at all.
    """
    if not isinstance(profile, dict):
        return {"stored": False, "refused": "NOT_A_PROFILE",
                "why": "a profile is a map of facts, each {value, by}. A %s "
                       "is not one, and reading facts out of something else "
                       "is the guessing D-33 forbids."
                       % type(profile).__name__}

    facts, unstated = {}, []
    for name in FACTS:
        entry = profile.get(name)
        if entry is None:
            unstated.append(name)
            continue
        if not isinstance(entry, dict):
            return {"stored": False, "refused": "NOT_A_FACT",
                    "why": "'%s' is %r. Each fact is {value, by} - the "
                           "value and who said it about themselves."
                           % (name, entry)}
        value = str(entry.get("value") or "").strip()
        said_by = entry.get("by")
        if not value:
            return {"stored": False, "refused": "NOT_A_FACT",
                    "why": "'%s' has no value. An empty fact that still "
                           "counts as stated is worse than an absent one - "
                           "it stops anybody asking." % name}
        if not _declared(said_by):
            return {"stored": False, "refused": "AN_INFERENCE_IS_NOT_A_FACT",
                    "fact": name,
                    "why": "'%s' was set by '%s', which is not a person. "
                           "Deciding from a conversation that somebody is a "
                           "beginner is a judgement they did not make and "
                           "cannot see. D-33: an unfamiliar input is a "
                           "question, not a guess."
                           % (name, said_by)}
        facts[name] = {"value": value, "by": str(said_by).strip()}

    granted = profile.get("mode")
    if granted is None:
        mode = {"mode": None,
                "why": "no mode was granted, so none is held. It is NOT "
                       "read as User Mode: docs/22 s3 makes a mode a "
                       "permission boundary, and a boundary that defaults "
                       "to something is not one."}
    else:
        if not isinstance(granted, dict):
            return {"stored": False, "refused": "MODE_MUST_BE_GRANTED",
                    "why": "the mode is %r. A grant is {mode, by, at} - "
                           "docs/22 s3: mode cannot be inferred from "
                           "conversation, and conflating it with anything "
                           "inferable would let a user talk their way into "
                           "ADMIN." % granted}
        wanted = str(granted.get("mode") or "").strip().lower()
        if wanted not in MODES:
            return {"stored": False, "refused": "NOT_A_MODE",
                    "why": "'%s' is not one of %s. A fourth is not invented "
                           "here - each one grants a different boundary and "
                           "docs/22 s3 owns the list."
                           % (wanted, ", ".join(sorted(MODES)))}
        missing = [(field, what) for field, what in A_GRANT_CARRIES
                   if not str(granted.get(field) or "").strip()]
        if missing:
            return {"stored": False, "refused": "MODE_MUST_BE_GRANTED",
                    "missing": [field for field, _what in missing],
                    "why": "the '%s' grant carries no %s - %s. A mode is "
                           "GRANTED, not claimed, and a grant nobody signed "
                           "cannot be withdrawn either."
                           % (wanted,
                              " and no ".join("`%s`" % field
                                              for field, _what in missing),
                              "; ".join(what for _field, what in missing))}
        if not _declared(granted.get("by")):
            return {"stored": False, "refused": "MODE_MUST_BE_GRANTED",
                    "why": "the '%s' grant was made by '%s', which is not a "
                           "person. docs/22 s3: mode cannot be inferred from "
                           "conversation - that is exactly how somebody "
                           "talks their way into ADMIN."
                           % (wanted, granted.get("by"))}
        mode = {"mode": wanted, "sees": MODES[wanted],
                "by": str(granted["by"]).strip(),
                "at": str(granted["at"]).strip(),
                "why": "granted by %s on %s. docs/22 s3: a permission "
                       "boundary, not a display preference."
                       % (granted["by"], granted["at"])}

    return {
        "stored": False, "facts": facts, "mode": mode,
        "unstated": unstated, "of": len(FACTS),
        "why": "%d of %d durable fact(s) stated, %s. Nothing was written "
               "and nothing was inferred."
               % (len(facts), len(FACTS),
                  "mode '%s'" % mode["mode"] if mode["mode"] else "no mode"),
        "unjudged": [
            "NOTHING ABOUT HOW TO SPEAK IS HERE, AND THERE IS NO FIELD FOR "
            "IT. D-27 abolished persona: Heron has one voice and the shape "
            "of the answer follows the shape of the REQUEST, not the "
            "reader. docs/28's row for this agent still says 'Persona reads "
            "this to choose how to speak' - PROPOSALS F19.",
            "EVERY FACT WAS DECLARED BY SOMEBODY, AND %d OF %d WERE NOT "
            "STATED AT ALL. An unstated fact is absent, not a default: "
            "filling one in would be the inference this agent exists to "
            "refuse." % (len(unstated), len(FACTS)),
            "%s" % ("THE MODE WAS GRANTED BY %s, NOT INFERRED. docs/22 s3 "
                    "says conflating a granted mode with an inferable "
                    "anything would let a user talk their way into ADMIN."
                    % mode["by"] if mode["mode"] else
                    "NO MODE IS HELD, AND THAT IS NOT USER MODE. A "
                    "permission boundary that defaults to something is not "
                    "a boundary."),
            "WHETHER ANY OF IT IS TRUE IS NOT CHECKED. Somebody who says "
            "they are an expert is recorded as having said so, and that is "
            "all this agent claims.",
        ],
    }


def main(argv):
    print("USER PROFILE   declared, never inferred, and a mode is granted")
    print("=" * 72)

    good = read({
        "role": {"value": "BIM modeller", "by": "Ajmal"},
        "discipline": {"value": "MEP", "by": "Ajmal"},
        "language": {"value": "English", "by": "Ajmal"},
        "mode": {"mode": "developer", "by": "Ajmal", "at": "2026-09-15"},
    })
    print("\n%s" % good["why"])
    for name in sorted(good["facts"]):
        print("  %-16s %-16s said by %s"
              % (name, good["facts"][name]["value"],
                 good["facts"][name]["by"]))
    print("  %-16s %s" % ("mode", good["mode"]["why"]))
    print("  %-16s %s" % ("not stated", ", ".join(good["unstated"])))

    print("\nrefused")
    for profile in (
            {"role": {"value": "beginner", "by": "inferred"}},
            {"mode": {"mode": "admin", "by": "detected", "at": "now"}},
            {"mode": {"mode": "admin", "by": "Ajmal"}},
            {"mode": {"mode": "superuser", "by": "Ajmal", "at": "now"}},
            {"role": {"value": "", "by": "Ajmal"}}):
        answer = read(profile)
        print("  %-28s %s" % (answer["refused"], answer["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in good["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
