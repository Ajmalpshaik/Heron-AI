# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-NAM-REF-007
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Reference update - five places, and "none found" is not an answer until
all five were looked in.

    python brain/heron_references.py

WHAT IT IS FOR (docs/28, HERON-NAM-REF-007)
--------------------------------------------
"After any rename or move: imports, references, metadata, registry,
docs. NO BROKEN REFERENCES." T1, risk MODIFY. It is the agent
HERON-NAM-REN-003 refuses to rename without, and it writes nothing: a
list of exact edits comes back and a caller makes them.

THE FIVE PLACES ARE THE REGISTER'S LIST, NOT A SHORTER ONE
------------------------------------------------------------
    imports     a module named in an import line
    references  a name used in code or in a call
    metadata    fragment.yaml, a contract, a header
    registry    docs/28, and anything else that indexes by name
    docs        prose that names the thing

**"No references found" is refused unless all five were searched.** A
search that covered three and found nothing is not the same statement
and must not be reported as one - it is the statement that breaks a
build, because it reads exactly like the safe one. So the places
actually searched are handed in and counted, and a short list is
NOT_SEARCHED_EVERYWHERE rather than a clean answer.

That is Golden Rule 14 applied to a search rather than to data: what was
not looked at is carried in the answer instead of being dropped.

A NAME INSIDE A LONGER NAME IS NOT A REFERENCE
------------------------------------------------
Renaming `heron_update` by replacing the text `heron_update` also
rewrites `heron_updates`, `heron_update_helper` and
`my_heron_update_thing`. That is the classic rename bug and it does not
announce itself: the build breaks somewhere else, later, in a file
nobody edited.

So every edit here is on a WORD BOUNDARY, and an occurrence where the
old name sits inside a longer identifier is refused as AMBIGUOUS_MATCH
rather than edited or skipped. Skipping it silently would leave the
question for whoever reads the diff, which is the same as not answering
it.

  No two modules in `brain/` collide this way today: no module's name is
  a prefix of another's. That is luck rather than design - nothing stops
  the next one being `heron_path.py` beside `heron_paths.py`, and nothing
  in HERON-NAM-VAL-002 would object. The suite checks the folder rather
  than trusting this paragraph, so it cannot quietly stop being true.

WHAT IT CANNOT DO, AND SAYS SO
--------------------------------
It cannot prove the search was complete. It is handed what was found
and which places were looked in, and it can check that all five were
claimed - not that any of them was done well. That limit is in the
answer every time rather than implied by its absence.
"""

from __future__ import annotations

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/28's own list for this agent, in its order.
PLACES = ("imports", "references", "metadata", "registry", "docs")

# What may sit either side of a name and still leave it a whole name.
_WORD = re.compile(r"[A-Za-z0-9_]")


def occurrences(text, name):
    """
    Every position `name` appears at, with whether it is a whole word.

    Deliberately not a regex on the NAME: a name is data and a name
    containing regex punctuation would otherwise change what is
    searched for - Golden Rule 19, at the level of a search.
    """
    found = []
    if not name:
        return found
    start = 0
    while True:
        at = text.find(name, start)
        if at < 0:
            return found
        before = text[at - 1] if at > 0 else ""
        after = (text[at + len(name)]
                 if at + len(name) < len(text) else "")
        whole = not (_WORD.match(before) or _WORD.match(after))
        found.append({"at": at, "whole": whole})
        start = at + 1


def plan(rename, found=None, searched=None):
    """
    {edit, refused_names, searched, why, unjudged} - or a refusal.

    Nothing is written and nothing is opened. `found` is a list of
    {where, text} handed in, and `searched` names which of the five
    places were actually looked in.
    """
    if not isinstance(rename, dict):
        return {"updated": False, "refused": "NOT_A_RENAME",
                "why": "a reference update follows a rename, and needs "
                       "{from, to}. A %s is not one."
                       % type(rename).__name__}

    was = str(rename.get("from") or "").strip()
    now = str(rename.get("to") or "").strip()
    if not was or not now:
        return {"updated": False, "refused": "NOT_A_RENAME",
                "why": "the rename names %s. Both halves are needed: the "
                       "old name to find and the new one to write."
                       % ("no old name" if not was else "no new name")}
    if was == now:
        return {"updated": False, "refused": "NOT_A_RENAME",
                "why": "'%s' is not being renamed, so nothing points "
                       "anywhere new." % was}

    looked = [str(each).strip().lower() for each in (searched or [])]
    strangers = sorted(set(looked) - set(PLACES))
    if strangers:
        return {"updated": False, "refused": "NOT_A_PLACE",
                "why": "%s is not one of the five docs/28 names for this "
                       "agent: %s. A sixth place is not invented here, and "
                       "a misspelt one would quietly reduce the count that "
                       "the whole answer rests on."
                       % (", ".join("'%s'" % each for each in strangers),
                          ", ".join(PLACES))}

    missed = [place for place in PLACES if place not in looked]
    if missed:
        return {"updated": False, "refused": "NOT_SEARCHED_EVERYWHERE",
                "searched": [place for place in PLACES if place in looked],
                "missed": missed,
                "why": "%d of the five places were searched; %s %s not. "
                       "docs/28 asks for imports, references, metadata, "
                       "registry and docs, with NO BROKEN REFERENCES - and "
                       "a search that covered some of them and found "
                       "nothing reads exactly like one that covered all of "
                       "them. Reporting the first as the second is what "
                       "breaks the build."
                       % (len(PLACES) - len(missed),
                          ", ".join(missed),
                          "was" if len(missed) == 1 else "were")}

    edits, refused = [], []
    for site in (found or []):
        if not isinstance(site, dict):
            refused.append({"site": repr(site)[:50],
                            "refused": "NOT_A_SITE",
                            "why": "each occurrence is {where, text}."})
            continue
        where = str(site.get("where") or "").strip()
        text = site.get("text")
        if not where or text is None:
            refused.append({"where": where or None,
                            "refused": "NOT_A_SITE",
                            "why": "an occurrence needs a place and the "
                                   "line it is on. Editing a line nobody "
                                   "can point at is how a fix lands in the "
                                   "wrong file."})
            continue

        text = str(text)
        here = occurrences(text, was)
        if not here:
            refused.append({"where": where,
                            "refused": "NOT_A_SITE",
                            "why": "'%s' does not appear in the line handed "
                                   "in for %s. A site that does not contain "
                                   "the name is a search result that has "
                                   "gone stale." % (was, where)})
            continue

        partial = [one for one in here if not one["whole"]]
        if partial:
            refused.append({"where": where, "refused": "AMBIGUOUS_MATCH",
                            "at": [one["at"] for one in partial],
                            "why": "'%s' sits inside a longer name in %s. "
                                   "Replacing the text would rewrite that "
                                   "name too, and the build would break "
                                   "somewhere else, later, in a file nobody "
                                   "edited. Refused rather than skipped: "
                                   "skipping leaves the question for "
                                   "whoever reads the diff."
                                   % (was, where)})
            continue

        edits.append({"where": where, "count": len(here),
                      "at": [one["at"] for one in here],
                      "was": was, "now": now,
                      "line": text.replace(was, now)})

    landed = len(edits) + len(refused)
    return {
        "updated": False, "from": was, "to": now,
        "edit": edits, "refused_names": refused,
        "searched": list(PLACES), "of": len(found or []),
        "why": "%d occurrence(s) of '%s': %d to rewrite as '%s', %d "
               "refused. All five places were searched. Nothing was written."
               % (len(found or []), was, len(edits), now, len(refused)),
        "unjudged": [
            "NOTHING WAS WRITTEN AND NOTHING WAS OPENED. The edits come "
            "back and a caller makes them; what was found and where it was "
            "looked for were HANDED IN.",
            "EVERY OCCURRENCE LANDS IN EXACTLY ONE LIST (%d of %d)."
            % (landed, len(found or [])),
            "ALL FIVE PLACES WERE CLAIMED SEARCHED, AND THAT IS ALL THAT "
            "CAN BE CHECKED HERE. Whether any of them was searched WELL is "
            "not knowable from inside this agent, and the difference "
            "between five careful searches and five claimed ones is the "
            "whole of the risk.",
            "EVERY EDIT IS ON A WORD BOUNDARY. '%s' inside a longer name "
            "is refused rather than rewritten - that rewrite does not "
            "announce itself, it breaks a build later in a file nobody "
            "edited." % was,
        ],
    }


def main(argv):
    print("REFERENCE UPDATE   five places, and none found is not an answer")
    print("=" * 72)

    print("\ndocs/28's five places for this agent")
    for place in PLACES:
        print("  - %s" % place)

    short = plan({"from": "heron_update", "to": "heron_updates"},
                 searched=["imports", "references", "docs"])
    print("\n%s\n  %s" % (short["refused"], short["why"][:120]))

    answer = plan(
        {"from": "heron_update", "to": "heron_revision"},
        found=[{"where": "brain/heron_migration.py",
                "text": "from heron_update import A_MIGRATION_DECLARES"},
               {"where": "docs/28-agent-registry.md",
                "text": "the heron_update module owns it"},
               {"where": "brain/heron_updates_helper.py",
                "text": "import heron_updates_helper"},
               {"where": "tests/test_update.py",
                "text": "nothing here names it"}],
        searched=list(PLACES))
    print("\n%s" % answer["why"])
    for row in answer["edit"]:
        print("  edit     %-32s %s" % (row["where"], row["line"][:34]))
    for row in answer["refused_names"]:
        print("  REFUSED  %-32s %s" % (row.get("where", "?"), row["refused"]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
