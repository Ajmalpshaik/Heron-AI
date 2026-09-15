# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-NAM-KEY-005
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Keywords - recorded by a person, looked up, never inferred.

    python brain/heron_keywords.py

WHAT IT IS FOR (docs/28, HERON-NAM-KEY-005)
--------------------------------------------
The register calls this the Keyword Agent and gives it "search terms and
synonyms". T1. Read the row on its own and it describes a synonym table.

AND D-34 FORBIDS EXACTLY THAT
-------------------------------
    "Heron builds nothing to understand language. No phrase list, NO
    SYNONYM TABLE, no parser for dictated near-misses. That belongs to
    the host and duplicating it there would be worse than the host's
    version and would need maintaining forever."

The two lines are about the same words and they point opposite ways.
Recorded as PROPOSALS F16.

THE READING THAT SATISFIES BOTH, AND IT IS D-34'S OWN
-------------------------------------------------------
D-34's consequences answer it, three lines further down:

    "A site word that maps to a Revit word is a different problem and is
    not solved by translation. When somebody says something the model
    calls by another name, that is KNOWLEDGE - it belongs in Heron's own
    knowledge store where it can be looked up and corrected, not in a
    language setting."

So this agent holds **knowledge, not language**. The difference is not a
nicety - it is four rules, and each one is a refusal here:

  it ships NO LIST          The table starts empty and stays empty until
                            somebody fills it. A built-in vocabulary is
                            the synonym table D-34 refuses, whatever it
                            is called.

  every entry NAMES A       "Looked up and corrected" needs somebody to
  PERSON AND A DATE         correct, and a date to correct from. An entry
                            with neither cannot be argued with.

  an INFERENCE IS NOT A     An entry marked derived, guessed, inferred or
  RECORD                    automatic is refused. That is what keeps D-34
                            true by construction rather than by
                            remembering it - there is no route by which a
                            model's guess becomes a stored term.

  an UNKNOWN TERM IS A      D-33: Heron never assumes an input, it asks,
  QUESTION                  and it asks once. Nothing is expanded
                            quietly, and "I do not know this word" comes
                            back with the question to put.

NOTHING HERE KNOWS ENGLISH
----------------------------
There is not one English vocabulary word in the code below - no term, no
stem, no plural rule, no stop list. The words in this docstring are
quotations from the register and from D-34. The suite checks the code
for them separately from the prose, because a word search that reads a
docstring has proved nothing.
"""

from __future__ import annotations

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# What a record must carry to be one. "Looked up and corrected" (D-34)
# needs somebody to correct and a date to correct from.
A_RECORD_CARRIES = (
    ("term", "the word somebody actually said"),
    ("means", "what it maps to, in the model's own words"),
    ("by", "who recorded it - a person, so it can be argued with"),
    ("at", "when, so a correction can say which version it replaces"),
)

# Words an entry may use to say it was NOT recorded by a person. Any of
# them and it is refused: there is no route by which a guess becomes a
# stored term.
NOT_RECORDED = ("derived", "guessed", "inferred", "automatic", "auto",
                "model", "suggested", "expanded")


def _person(who):
    """Whether `by` names somebody rather than a process."""
    name = str(who or "").strip().lower()
    if not name:
        return False
    return not any(word in name.split() or word == name
                   for word in NOT_RECORDED)


def check(entry):
    """{ok, refused, why} - is this a record, or an inference wearing one?"""
    if not isinstance(entry, dict):
        return {"ok": False, "refused": "NOT_A_RECORD",
                "why": "a record is a map carrying %s. A %s is not one."
                       % (", ".join(field for field, _why in A_RECORD_CARRIES),
                          type(entry).__name__)}

    missing = [field for field, _why in A_RECORD_CARRIES
               if not str(entry.get(field) or "").strip()]
    if missing:
        return {"ok": False, "refused": "NOT_A_RECORD",
                "why": "the entry carries no %s. %s"
                       % (", ".join(missing),
                          " ".join(why for field, why in A_RECORD_CARRIES
                                   if field in missing))}

    if not _person(entry.get("by")):
        return {"ok": False, "refused": "AN_INFERENCE_IS_NOT_A_RECORD",
                "why": "'%s' recorded it, which is not a person. D-34: "
                       "Heron builds nothing to understand language, and a "
                       "guess that is allowed to be stored becomes the "
                       "synonym table that decision refuses - under a "
                       "different name and with nobody to correct it."
                       % entry.get("by")}

    return {"ok": True, "term": str(entry["term"]).strip(),
            "means": str(entry["means"]).strip(),
            "why": "recorded by %s on %s, and correctable by whoever "
                   "disagrees." % (entry["by"], entry["at"])}


def look_up(term, recorded=None):
    """
    {found, means, why} - what a PERSON recorded, or the question to ask.

    `recorded` is handed in. There is no built-in table and no default
    one: an empty answer here means nobody has recorded this word, which
    is a true statement about the knowledge store rather than about the
    word.
    """
    word = str(term or "").strip()
    if not word:
        return {"found": False, "refused": "NO_TERM",
                "why": "nothing was asked about. An empty term matching "
                       "nothing is not the same as a word nobody has "
                       "recorded, and they must not share an answer."}

    kept, rejected = [], []
    for entry in (recorded or []):
        judged = check(entry)
        if judged.get("ok"):
            kept.append(judged)
        else:
            rejected.append(dict(judged, entry=repr(entry)[:60]))

    hits = [one for one in kept if one["term"].lower() == word.lower()]
    if not hits:
        return {"found": False, "refused": "NOTHING_RECORDED",
                "term": word, "held": len(kept), "rejected": rejected,
                "ask": "What does '%s' mean here?" % word,
                "why": "nobody has recorded '%s'. %d term(s) are held and "
                       "none of them is this one. D-33: Heron never assumes "
                       "an input - it asks, and it asks once - so this comes "
                       "back as a question rather than as a guess, and "
                       "nothing was expanded quietly."
                       % (word, len(kept))}

    return {
        "found": True, "term": word,
        "means": [one["means"] for one in hits],
        "held": len(kept), "rejected": rejected,
        "why": "'%s' was recorded %d time(s): %s. %s"
               % (word, len(hits), "; ".join(one["why"] for one in hits),
                  "Every one of them is somebody's statement, not this "
                  "agent's."),
        "unjudged": [
            "NOTHING WAS INFERRED. This agent holds no list of its own and "
            "ships none: the table starts empty and stays empty until "
            "somebody fills it, because a built-in vocabulary is the "
            "synonym table D-34 refuses whatever it is called.",
            "WHETHER THE RECORD IS RIGHT IS NOT JUDGED HERE. It names who "
            "made it and when, which is what D-34 asks for - knowledge "
            "that can be looked up AND CORRECTED - and correcting it is a "
            "person's, not this agent's.",
            "%s" % ("%d entry(s) were handed in and refused, and they are "
                    "in the answer rather than dropped - Golden Rule 14."
                    % len(rejected) if rejected else
                    "every entry handed in was a record."),
        ],
    }


def main(argv):
    print("KEYWORDS   recorded by a person, looked up, never inferred")
    print("=" * 72)

    print("\nwhat a record must carry")
    for field, why in A_RECORD_CARRIES:
        print("  %-6s %s" % (field, why))

    held = [
        {"term": "ductwork", "means": "Duct",
         "by": "Ajmal", "at": "2026-09-15"},
        {"term": "ductwork", "means": "Flex Duct",
         "by": "Site engineer", "at": "2026-09-15"},
        {"term": "riser", "means": "Vertical duct run",
         "by": "the model", "at": "2026-09-15"},
        {"term": "sleeve", "means": "Penetration",
         "by": "Ajmal"},
    ]

    for word in ("ductwork", "riser", "shaft", ""):
        answer = look_up(word, recorded=held)
        if answer.get("found"):
            print("\n'%s' -> %s" % (word, ", ".join(answer["means"])))
        else:
            print("\n'%s' -> %s" % (word, answer["refused"]))
            if answer.get("ask"):
                print("      ask: %s" % answer["ask"])

    answer = look_up("ductwork", recorded=held)
    print("\nrefused, and carried in the answer rather than dropped")
    for row in answer["rejected"]:
        print("  %-28s %s" % (row["refused"], row["why"][:44]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
