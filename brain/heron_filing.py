# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-REN-010
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Import filing - the folder is derived, the id is asked for.

    python brain/heron_filing.py

WHAT IT IS FOR (docs/28, HERON-IMP-REN-010)
--------------------------------------------
"Applies naming conventions to imported artefacts." T1, risk MODIFY.

ONE OF THESE NAMES IS A CONSEQUENCE AND THE OTHERS ARE CHOICES
----------------------------------------------------------------
heron_fragment states the conventions and they are not all the same
kind of thing:

  the FOLDER      is the capability, lower case, underscores to hyphens.
                  `folder_for` computes it. Nobody chooses it - given
                  the capability there is exactly one right answer, and
                  this agent applies it.

  the ID          is FRG-<AREA>-<NNN>. The area is one of nine and the
                  number is the next free one, and BOTH are choices.
                  Deriving `FRG-ELE-042` from `CountDucts.py` means
                  picking an area and a number, which is the wall
                  HERON-NAM-GEN-001 was left unbuilt at (PROPOSALS F15).

  the CAPABILITY  is SCREAMING_SNAKE, verb first. Turning "CountDucts"
                  into COUNT_DUCTS looks mechanical and is not: it is
                  naming what the code DOES, which is language, and D-34
                  says Heron builds nothing for that.

So this agent derives the one that is derivable and ASKS for the two
that are not - D-33, once, with the stated shape in the question so the
answer can be right first time.

IT CHECKS BY CALLING, NOT BY COPYING
--------------------------------------
Whether a supplied id or capability satisfies the convention is
HERON-FRG-VAL-001's `naming_problems`, called on the card. Its words
come back unaltered. This file compiles no pattern of its own, and the
suite checks that.

A NAME IS NOT AN IDENTITY
---------------------------
heron_fragment says it plainly: "A fragment in a renamed folder is STILL
THE SAME FRAGMENT - that is what an id is for - and it is ALSO
misfiled." So this agent reports the move it would make and changes no
id. Renaming a folder is filing; changing an id is something else, and
HERON-NAM-REN-003 is where that argument lives.

NOTHING IS MOVED
------------------
The answer says where each artefact belongs. Moving it is
HERON-IMP-MIG-009's, and docs/28 gives HERON-IMP-MAIN-001 "never
modifies the source folder" in its own row.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

FRAGMENT = "fragment"
SKILL = "skill"

# The one name that is a consequence rather than a choice.
folder_for = FRAG.folder_for

# What must be supplied, because deriving it would mean choosing. The
# shape goes in the question so the answer can be right first time.
ASKED_FOR = {
    "capability": "SCREAMING_SNAKE_CASE, verb first - COUNT_DUCTS, not "
                  "CountDucts. It names what the code DOES, which is "
                  "language, and D-34 says Heron builds nothing for that",
    "id": "FRG-<AREA>-<NNN>, e.g. FRG-ELE-001. The area is one of %s and "
          "the number is the next free one - both are choices, not "
          "derivations" % ", ".join(sorted(FRAG.AREAS)),
}


def file_it(items):
    """
    {filed, asks, problems} - or a refusal. Nothing is moved or renamed
    on disk.
    """
    items = list(items or [])
    if not items:
        return {"filed": False, "refused": "NOTHING_TO_FILE",
                "why": "no items were handed in."}

    seen = []
    placed, asking, wrong = [], [], []
    for item in items:
        item = getattr(item, "data", item)
        if not isinstance(item, dict):
            return {"filed": False, "refused": "NOT_AN_ITEM",
                    "why": "%r is not an item. Each is {name, kind} and "
                           "carries whatever the migration has given it "
                           "so far." % (item,)}
        name = str(item.get("name") or "").strip()
        kind = str(item.get("kind") or "").strip().lower()
        if not name or not kind:
            return {"filed": False, "refused": "NOT_AN_ITEM",
                    "why": "an item is missing its %s."
                           % ("name" if not name else "kind")}
        if name in seen:
            return {"filed": False, "refused": "NOT_AN_ITEM",
                    "why": "'%s' appears twice." % name}
        seen.append(name)

        if kind == SKILL:
            who = str(item.get("id") or "").strip()
            if not who:
                asking.append({"item": name, "needs": ["id"],
                               "why": "a skill is filed as <id>.yaml, and "
                                      "'%s' has no id yet." % name})
                continue
            placed.append({"item": name, "kind": kind, "id": who,
                           "goes": "brain/skills/%s.yaml" % who,
                           "derived_from": "id"})
            continue

        if kind != FRAGMENT:
            asking.append({"item": name, "needs": [],
                           "why": "nothing here files a %r. The two "
                                  "conventions heron_fragment states are "
                                  "for a fragment's folder and a skill's "
                                  "file, and inventing a third for a %r "
                                  "would be this agent choosing where "
                                  "somebody else's work lives."
                                  % (kind, kind)})
            continue

        missing = [field for field in ("capability", "id")
                   if not str(item.get(field) or "").strip()]
        if missing:
            asking.append({
                "item": name, "needs": missing,
                "asked": dict((field, ASKED_FOR[field])
                              for field in missing),
                "why": "'%s' needs %s before it can be filed, and neither "
                       "can be derived from a file name - deriving one "
                       "would mean choosing, which is the wall "
                       "HERON-NAM-GEN-001 was left unbuilt at."
                       % (name, " and ".join(missing))})
            continue

        capability = str(item["capability"]).strip()
        who = str(item["id"]).strip()
        card = dict(item.get("card") or {})
        card.update({"id": who, "capability": capability})
        where = folder_for(capability)
        # THE CONVENTION IS HERON-FRG-VAL-001's, CALLED NOT COPIED.
        found = FRAG.naming_problems(FRAG.Fragment(
            card, os.path.join(ROOT, "brain", "fragments", where)))
        if found:
            wrong.append({"item": name, "id": who,
                          "capability": capability,
                          "problems": list(found),
                          "by": "HERON-FRG-VAL-001",
                          "why": "%d problem(s) with the names supplied, "
                                 "in that agent's own words."
                                 % len(found)})
            continue
        placed.append({"item": name, "kind": kind, "id": who,
                       "capability": capability,
                       "goes": "brain/fragments/%s" % where,
                       "derived_from": "capability",
                       "why": "the folder IS the capability in lower case "
                              "- given %s there is exactly one right "
                              "answer, and nobody chooses it."
                              % capability})

    return {
        "filed": True, "moved": False, "of": len(items),
        "placed": placed, "asks": asking, "problems": wrong,
        "why": "%d item(s): %d have a place, %d need an answer first, %d "
               "carry a name the convention refuses. Nothing was moved."
               % (len(items), len(placed), len(asking), len(wrong)),
        "unjudged": [
            "NOTHING WAS MOVED OR RENAMED ON DISK. The answer says where "
            "each artefact belongs; moving it is HERON-IMP-MIG-009's, and "
            "docs/28 gives HERON-IMP-MAIN-001 'never modifies the source "
            "folder' in its own row.",
            "%s" % ("%d ITEM(S) NEED AN ANSWER BEFORE THEY CAN BE FILED: "
                    "%s. A capability names what the code DOES and an id "
                    "picks an area and a number - both are choices, and "
                    "deriving either from a file name is the wall "
                    "HERON-NAM-GEN-001 was left unbuilt at (F15)."
                    % (len(asking),
                       ", ".join(one["item"] for one in asking))
                    if asking else
                    "every item carried what it needed; nothing had to be "
                    "asked for."),
            "THE CONVENTION IS HERON-FRG-VAL-001's, CALLED RATHER THAN "
            "COPIED. This file compiles no pattern of its own - the id "
            "shape, the area list and the capability shape are all that "
            "agent's, and its problems come back unaltered.",
            "A NAME IS NOT AN IDENTITY. heron_fragment says it plainly: a "
            "fragment in a renamed folder is still the same fragment, and "
            "it is ALSO misfiled. Nothing here changed an id - that is "
            "HERON-NAM-REN-003's argument, not this one's.",
        ],
    }


def main(argv):
    print("IMPORT FILING   the folder is derived, the id is asked for")
    print("=" * 72)
    print("\nderived: the folder, by heron_fragment.folder_for")
    for field, shape in sorted(ASKED_FOR.items()):
        print("  asked:   %-12s %s" % (field, shape[:52]))

    answer = file_it([
        {"name": "CountDucts.py", "kind": "fragment"},
        {"name": "TagSheet.py", "kind": "fragment",
         "capability": "TAG_SHEET", "id": "FRG-SHT-042"},
        {"name": "BadName.py", "kind": "fragment",
         "capability": "CountDucts", "id": "FRG-SHT-043"},
        {"name": "WrongArea.py", "kind": "fragment",
         "capability": "COUNT_DUCTS", "id": "FRG-ZZZ-001"},
        {"name": "count-things", "kind": "skill", "id": "count-things"},
        {"name": "README.md", "kind": "document"}])

    print("\n%s" % answer["why"])
    for one in answer["placed"]:
        print("  PLACED   %-16s -> %s" % (one["item"], one["goes"]))
    for one in answer["asks"]:
        print("  ASKS     %-16s %s" % (one["item"], one["why"][:48]))
    for one in answer["problems"]:
        print("  REFUSED  %-16s %s" % (one["item"], one["problems"][0][:48]))

    print("\nrefused")
    for these in ([], ["a string"], [{"name": "x"}],
                  [{"name": "x", "kind": "fragment"},
                   {"name": "x", "kind": "skill"}]):
        bad = file_it(these)
        print("  %-20s %s" % (bad["refused"], bad["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
