# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-ARC-011
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Architecture matching - a classification is not a kind, and the two
vocabularies share no word.

    python brain/heron_belongs.py

WHAT IT IS FOR (docs/28, HERON-IMP-ARC-011)
--------------------------------------------
"Places content where the architecture says it belongs." T2, risk
MODIFY. Step 12 of docs/00 s28's sixteen.

IT DECIDES NO FOLDER. HERON-WSP-PLC-005 DOES
----------------------------------------------
That agent's row is "decides where a new artefact belongs", it holds
docs/06 s2's eight data folders, and its rule is that the DECLARED kind
decides and a name is never a place. Deciding a second time here would
be a second answer to the same question, which Golden Rule 3 exists to
stop. So `place` is bound by identity and its answers come back
unaltered, refusals included.

WHAT THIS AGENT ADDS IS THE THING THAT WENT WRONG IN THE MIDDLE
----------------------------------------------------------------
The import pipeline classifies before it files. HERON-IMP-CLS-003 sorts
content into docs/28's five:

    code   documentation   config   metadata   asset

HERON-WSP-PLC-005 files by docs/06 s2's eight:

    knowledge  skill  fragment  memory  project  company  log  backup

THOSE TWO LISTS SHARE NO WORD. Not one. An item arriving with
`kind: asset` is not a kind the workspace has ever heard of, and PLC-005
refuses it correctly - "a ninth is not invented here" - but refuses it
as though somebody had mistyped, when what actually happened is that an
earlier step in the same pipeline produced a vocabulary nothing
downstream reads.

So the five are intercepted here, by name, and answered with WHICH AGENT
HAS TO RUN FIRST:

    code            HERON-IMP-FEX-004 or HERON-IMP-SEX-005 - a fragment
                    or a skill is a kind; "code" is not
    documentation   HERON-RAG-DIS-002 - a document enters a SCOPE
                    (global, company, project), not a data folder

    config          nothing places these, and that is the finding
    metadata        below rather than a gap to paper over
    asset

WHY THOSE THREE HAVE NOWHERE TO GO, PRECISELY
-----------------------------------------------
docs/06 s2 draws nineteen folders and classifies fourteen.
HERON-WSP-CRE-002 found the five it leaves out - RAG, Community,
CONFIGURATION, Tests, Documentation. So `config` has a folder with no
class, and `asset` has neither a folder nor a class.

A class is what decides whether a product update may replace a folder
wholesale, whether a cleanup may delete it and whether a backup covers
it. Filing an imported `.ini` into an unclassified folder means nobody
can say whether the next update deletes it. That is why this refuses
rather than picks, and it is PROPOSALS F28.

NOTHING IS MOVED
------------------
The register gives this row MODIFY and the answer says where each item
belongs. HERON-IMP-MIG-009 moves, and docs/28 gives HERON-IMP-MAIN-001
"never modifies the source folder" in its own row.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_classify as CLS  # noqa: E402
import heron_placement as PLC  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The two vocabularies, both bound rather than retyped, so that the claim
# they share no word is checked against the real lists every run.
KINDS = PLC.KINDS
CATEGORIES = CLS.CATEGORIES

# docs/06 s2's own decision, called rather than copied.
place = PLC.place

# What has to happen before each classification becomes a kind. The three
# with no answer carry None, and None is reported as a gap rather than
# filled in.
BECOMES_A_KIND = {
    "code": "HERON-IMP-FEX-004 pulls a fragment out of it, or "
            "HERON-IMP-SEX-005 finds a skill in it. A fragment and a "
            "skill are kinds; `code` is not",
    "documentation": "HERON-RAG-DIS-002 ingests it into a SCOPE - global, "
                     "company or project. A document does not live in a "
                     "data folder at all",
    "config": None,
    "metadata": None,
    "asset": None,
}

NO_HOME = ("docs/06 s2 draws nineteen folders and classifies fourteen. "
           "HERON-WSP-CRE-002 found the five it leaves out - RAG, "
           "Community, Configuration, Tests, Documentation. So there is "
           "no CLASS for this, and the class is what decides whether an "
           "update may replace the folder, whether a cleanup may delete "
           "it and whether a backup covers it. Filing it somewhere would "
           "mean nobody can answer those (PROPOSALS F28)")


def match(items, project=None, asked=None):
    """
    {matched, placed, not_yet, no_home} - or a refusal. No folder is
    decided here and nothing is moved.
    """
    items = list(items or [])
    if not items:
        return {"matched": False, "refused": "NOTHING_TO_MATCH",
                "why": "no items were handed in."}

    placed, not_yet, homeless, refused = [], [], [], []
    for item in items:
        item = getattr(item, "data", item)
        if not isinstance(item, dict):
            return {"matched": False, "refused": "NOT_AN_ITEM",
                    "why": "%r is not an item. Each is a map declaring a "
                           "kind and a name, which is what "
                           "HERON-WSP-PLC-005 reads." % (item,)}

        kind = str(item.get("kind") or "").strip().lower()
        name = item.get("name")

        # THE INTERCEPT, and the only thing this agent decides. A word from
        # HERON-IMP-CLS-003's list is not a mistyped kind - it is an earlier
        # step's vocabulary, and handing it to PLC-005 would get it refused
        # as though somebody had guessed at a ninth folder.
        if kind in CATEGORIES:
            card = {"name": name, "category": kind,
                    "needs": BECOMES_A_KIND[kind]}
            if BECOMES_A_KIND[kind]:
                not_yet.append(dict(card, why="`%s` is a classification, not "
                                              "a kind. %s"
                                              % (kind, BECOMES_A_KIND[kind])))
            else:
                homeless.append(dict(card,
                                     why="nothing in Heron places %s `%s`. %s"
                                         % ("an" if kind[0] in "aeiou"
                                            else "a", kind, NO_HOME)))
            continue

        answer = place(item, project=project, asked=asked)
        # SUCCESS IS THE ABSENCE OF `refused`, NOT `placed` BEING TRUE.
        # HERON-WSP-PLC-005 declares `placed` as ALWAYS FALSE - "the caller
        # writes the file" - so it means nothing was written to disk, not
        # that the decision failed. Reading it as the verdict rejects every
        # correct answer, and the first version of this file did exactly
        # that. HERON-FRG-CRE-007 hit the same trap on the Matcher's
        # `ran: False`.
        if answer.get("refused"):
            refused.append(dict(answer, name=name))
        else:
            placed.append(dict(answer, name=name))

    return {
        "matched": True,
        "of": len(items),
        "placed": placed,
        "not_yet": not_yet,
        "no_home": homeless,
        "refused_by_placement": refused,
        "moved": False,
        "why": "%d item%s: %d placed, %d waiting on an earlier agent, %d "
               "with nowhere in the architecture to go, %d refused by "
               "HERON-WSP-PLC-005. Nothing was moved."
               % (len(items), "" if len(items) == 1 else "s", len(placed),
                  len(not_yet), len(homeless), len(refused)),
        "unjudged": [
            "NO FOLDER WAS DECIDED HERE. HERON-WSP-PLC-005's `place` was "
            "called and its answers come back unaltered, refusals "
            "included. Deciding a second time would be a second answer to "
            "one question (Golden Rule 3).",
            "THE TWO VOCABULARIES SHARE NO WORD. HERON-IMP-CLS-003 sorts "
            "into %s; HERON-WSP-PLC-005 files by %s. Nothing in this "
            "project maps one onto the other, and this agent does not "
            "invent that mapping - it says which agent turns a "
            "classification into a kind."
            % (", ".join(sorted(CATEGORIES)), ", ".join(sorted(KINDS))),
            ("%d ITEM%s NOWHERE TO GO: %s. %s"
             % (len(homeless),
                " HAS" if len(homeless) == 1 else "S HAVE",
                ", ".join(sorted(set(card["category"]
                                     for card in homeless))), NO_HOME)
             if homeless else
             "every item either placed or named the agent that has to run "
             "first; none was left without a home."),
            "`placed` IS FALSE ON EVERY ANSWER AND THAT IS NOT A "
            "REFUSAL. HERON-WSP-PLC-005 declares it always false - the "
            "folder comes back and the caller writes the file. A refusal "
            "carries `refused`, and that is what was read here.",
            "NOTHING WAS MOVED. The answer says where each item belongs. "
            "HERON-IMP-MIG-009 moves, and HERON-IMP-MAIN-001's own row "
            "says the source folder is never modified.",
        ],
    }


def main(argv):
    print("ARCHITECTURE MATCHING   a classification is not a kind")
    print("=" * 72)
    print("\nHERON-IMP-CLS-003 sorts into : %s" % ", ".join(sorted(CATEGORIES)))
    print("HERON-WSP-PLC-005 files by   : %s" % ", ".join(sorted(KINDS)))
    print("they share                   : %s"
          % (", ".join(sorted(set(CATEGORIES) & set(KINDS))) or "NOT ONE WORD"))

    answer = match([
        {"kind": "fragment", "name": "COUNT_DUCTS"},
        {"kind": "skill", "name": "count-things"},
        {"kind": "code", "name": "CountDucts.py"},
        {"kind": "documentation", "name": "README.md"},
        {"kind": "config", "name": "settings.json"},
        {"kind": "asset", "name": "logo.png"},
        {"kind": "metadata", "name": "manifest.xml"},
        {"kind": "fragment", "name": "../escape"},
    ])

    print("\n%s" % answer["why"])
    for card in answer["placed"]:
        print("  PLACED    %-16s -> %s" % (card["name"], card["folder"]))
    for card in answer["not_yet"]:
        print("  NOT YET   %-16s %s" % (card["name"], card["needs"][:44]))
    for card in answer["no_home"]:
        print("  NO HOME   %-16s %s" % (card["name"],
                                       card["why"].split(".")[0]))
    for card in answer["refused_by_placement"]:
        print("  REFUSED   %-16s %s" % (card["name"], card["refused"]))

    print("\nrefused")
    for these in ([], None, ["a string"]):
        bad = match(these)
        print("  %-20s %s" % (bad["refused"], bad["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
