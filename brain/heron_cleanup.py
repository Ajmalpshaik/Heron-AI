# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-CLN-009
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Cleanup - unused is a claim, and archiving is the only removal there is.

    python brain/heron_cleanup.py

WHAT IT IS FOR (docs/28, HERON-WSP-CLN-009)
--------------------------------------------
"Identifies unused artefacts. **Archives, never deletes**." T1, risk
MODIFY.

"UNUSED" IS A CLAIM, AND NOTHING HERE MAKES IT WITHOUT EVIDENCE
-----------------------------------------------------------------
The whole agent turns on one distinction that is easy to lose:

  NOT USED       something watched it over a stated window and it was
                 not used. That is an observation.
  NOT OBSERVED   nothing watched it. That is not the same thing, and it
                 is what most artefacts look like.

An artefact with no usage record is UNOBSERVED, never unused, and it is
left alone. Absence of evidence is not evidence, and the fragment nobody
recorded a run for is exactly the one somebody uses once a quarter on the
job that matters.

So a usage record has to name a WINDOW as well as a count. "Used 0 times"
with no window is a sentence about the record-keeping, not about the
artefact - and a cleanup that read it as the second is a cleanup that
archives a modeller's library the first week after somebody turned
logging on.

ARCHIVING IS THE ONLY REMOVAL THERE IS
----------------------------------------
The register row says archives, never deletes, and there is no delete in
this module - no unlink, no rmtree, no os.remove, and the suite asserts
their absence rather than trusting the sentence. Archiving is reversible
and visible; deleting is neither, and the difference matters most for the
class nobody can regenerate.

HERON-WSP-PTH-007 DECIDES WHAT MAY BE REMOVED OUTRIGHT
--------------------------------------------------------
Derived state may go - something rebuilds it, and deleting it is a valid
recovery action. Everything else is archived instead. That rule is not
restated here; it is asked, which is the second consumer of that table
and the point of having built it.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import heron_paths as PATHS                                    # noqa: E402

# What a usage record has to carry before "unused" is an observation
# rather than a sentence about the record-keeping.
A_USAGE_RECORD_CARRIES = (
    ("since", "the date watching started - without it, 'used 0 times' is "
              "about the logging, not the artefact"),
    ("until", "the date it stopped, so the window can be read"),
    ("uses", "how many times it was used in that window"),
)


def sweep(artefacts, usage=None, now=None):
    """
    {remove, archive, unobserved, keep, why} - or a refusal.

    Nothing is removed, archived or written. Four lists come back and a
    caller acts on them, which is what keeps "archives, never deletes"
    checkable from outside rather than promised from inside.
    """
    if not artefacts:
        return {"refused": "NOTHING_TO_SWEEP",
                "why": "nothing was offered. An empty sweep is not a tidy "
                       "workspace - it is a run nobody asked for."}

    usage = usage if isinstance(usage, dict) else {}
    remove, archive, unobserved, keep = [], [], [], []

    for artefact in artefacts:
        # A non-string entry is a hole, not an artefact named "None".
        name = artefact.strip() if isinstance(artefact, str) else ""
        if not name:
            return {"refused": "NOT_AN_ARTEFACT",
                    "why": "something in the list names nothing. A list with "
                           "a hole in it is not a list."}

        record = usage.get(name)
        missing = [what for field, what in A_USAGE_RECORD_CARRIES
                   if not isinstance(record, dict)
                   or record.get(field) in (None, "")]
        if missing:
            unobserved.append({
                "artefact": name,
                "wants": missing,
                "why": "nothing watched this, so it is UNOBSERVED and not "
                       "unused. Absence of evidence is not evidence, and "
                       "the fragment nobody recorded a run for is exactly "
                       "the one somebody uses once a quarter on the job "
                       "that matters."})
            continue

        try:
            used = int(record["uses"])
        except (TypeError, ValueError):
            return {"refused": "NOT_A_USAGE_RECORD", "artefact": name,
                    "why": "%s records %r uses, which is not a count. A "
                           "record nobody can read is not a record."
                           % (name, record.get("uses"))}

        if used > 0:
            keep.append({"artefact": name, "uses": used,
                         "why": "used %d time(s) between %s and %s"
                                % (used, record["since"], record["until"])})
            continue

        # UNUSED, OBSERVED - and now the only question is what may happen
        # to it, which is HERON-WSP-PTH-007's and not this agent's.
        allowed = PATHS.may("cleanup", name)
        entry = {"artefact": name, "class": allowed.get("class"),
                 "read_as": allowed.get("read_as"),
                 "window": "%s to %s" % (record["since"], record["until"]),
                 "why": allowed.get("why")}
        (remove if allowed.get("allowed") else archive).append(entry)

    return {
        "remove": remove, "archive": archive, "unobserved": unobserved,
        "keep": keep,
        "why": "%d unused and rebuildable, %d unused and ARCHIVED rather "
               "than deleted, %d unobserved and left alone, %d in use. "
               "Nothing was touched."
               % (len(remove), len(archive), len(unobserved), len(keep)),
        "unjudged": [
            "NOTHING WAS REMOVED, ARCHIVED OR WRITTEN. Four lists come "
            "back and a caller acts on them - which is what keeps "
            "'archives, never deletes' checkable from outside rather than "
            "promised from inside.",
            "UNOBSERVED IS NOT UNUSED. %d artefact(s) had no readable usage "
            "record and are in their own list for that reason, not folded "
            "in with the ones nobody used." % len(unobserved),
            "the window came from the record. This agent does not know how "
            "long is long enough for an artefact to be worth archiving - "
            "that is a judgement about the practice, and a default here "
            "would be a number somebody invented.",
            "what may be removed outright is HERON-WSP-PTH-007's answer, "
            "asked rather than restated. Derived state may go because "
            "something rebuilds it; everything else is archived.",
        ],
    }


def main(argv):
    print("CLEANUP   unused is a claim, and archiving is the only removal")
    print("=" * 72)

    artefacts = ["Cache/vectors.db", "Fragments/old-duct-sizer",
                 "Fragments/align-mep-elevation", "Core/Heron.Old.dll",
                 "Memory/2024-notes", "who/knows.bin"]
    usage = {
        "Cache/vectors.db": {"since": "2026-06-01", "until": "2026-09-14",
                             "uses": 0},
        "Fragments/old-duct-sizer": {"since": "2026-06-01",
                                     "until": "2026-09-14", "uses": 0},
        "Fragments/align-mep-elevation": {"since": "2026-06-01",
                                          "until": "2026-09-14", "uses": 41},
        "Core/Heron.Old.dll": {"since": "2026-06-01", "until": "2026-09-14",
                               "uses": 0},
        "who/knows.bin": {"since": "2026-06-01", "until": "2026-09-14",
                          "uses": 0},
    }
    answer = sweep(artefacts, usage=usage)
    print("  %s" % answer["why"])

    for label, entries in (("MAY BE REMOVED (something rebuilds it)",
                            answer["remove"]),
                           ("ARCHIVED, NEVER DELETED", answer["archive"]),
                           ("UNOBSERVED - left alone", answer["unobserved"]),
                           ("IN USE", answer["keep"])):
        print()
        print("  %s:" % label)
        for entry in entries:
            print("    %-32s %s"
                  % (entry["artefact"],
                     (entry.get("class") or entry["why"])[:34]))

    print()
    print("  'Memory/2024-notes' is the one worth looking at. Nothing")
    print("  recorded a use of it, and it is NOT in the unused list:")
    entry = [one for one in answer["unobserved"]
             if one["artefact"] == "Memory/2024-notes"][0]
    print("    %s" % entry["why"][:100])

    print()
    print("  And a record with no window is about the logging, not the")
    print("  artefact:")
    answer = sweep(["Fragments/x"], usage={"Fragments/x": {"uses": 0}})
    print("    %s" % answer["unobserved"][0]["wants"][0][:92])

    print()
    print("  Every way it refuses:")
    for label, ask, use in (("nothing offered", [], {}),
                            ("an artefact naming nothing", ["", "x"], {}),
                            ("uses that is not a count", ["x"],
                             {"x": {"since": "a", "until": "b",
                                    "uses": "lots"}})):
        print("    %-28s %s" % (label, sweep(ask, usage=use)["refused"]))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
