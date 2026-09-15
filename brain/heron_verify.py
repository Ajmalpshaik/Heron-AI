# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-VAL-012
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Import verification - before it is saved, and by the agent that owns
each kind.

    python brain/heron_verify.py

WHAT IT IS FOR (docs/28, HERON-IMP-VAL-012)
--------------------------------------------
"Verifies the migrated result BEFORE IT IS SAVED." T1, risk READ.

IT WRITES NO THIRD VALIDATOR
------------------------------
A migrated fragment is validated by HERON-FRG-VAL-001 and a migrated
skill by HERON-SKL-VAL-004. Both already exist, both are the owner of
what a complete card looks like for their kind, and a third
implementation here would be a third thing that can be right on its own
while disagreeing with the other two - the objection that left
HERON-NAM-MET-006 unbuilt (PROPOSALS F17).

So each kind goes to its owner's `validate`, and their problems come
back in their own words.

BEFORE IT IS SAVED MEANS NOTHING IS ON DISK
---------------------------------------------
The cards are checked in memory. Nothing here opens a file, writes one
or creates a folder - which is what makes "before" true rather than
merely intended. A validator that runs after the write has already lost
the argument.

A KIND WITH NO VALIDATOR IS NOT VERIFIED
------------------------------------------
Not every migrated thing is a fragment or a skill. A document has no
validator here, and there is nothing wrong with that - but it must not
land in the same list as the things that were actually checked.

"Verified" meaning "nobody looked" is the silent pass this project
keeps catching, and HERON-IMP-DUP-007 refuses the same shape one step
earlier: nothing comparable is not nothing like it.

COMPLETE IS NOT CORRECT
-------------------------
A card that passes carries every field its kind requires, in the right
shape. Nothing here compiled anything, ran anything or read an
implementation. docs/09's ladder has six more rungs above where an
import enters, and every one of them is somebody else's to give.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402
import heron_skill as SKILL  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

FRAGMENT = "fragment"
SKILL_KIND = "skill"

# Each kind and the agent that owns what a complete one looks like.
# Nothing is validated here; the owner's own `validate` is called and
# its problems come back in its own words.
OWNERS = {
    FRAGMENT: ("HERON-FRG-VAL-001", FRAG.validate),
    SKILL_KIND: ("HERON-SKL-VAL-004", SKILL.validate),
}


def _wrap(kind, card, name):
    """The object each owner's validate expects, built in memory."""
    if kind == FRAGMENT:
        return FRAG.Fragment(card, os.path.join(ROOT, "brain", "fragments",
                                                name))
    return SKILL.Skill(card, os.path.join(ROOT, "brain", "skills",
                                          "%s.yaml" % name))


def verify(items):
    """
    {ready, problems, no_validator} - or a refusal. Nothing is saved.
    """
    items = list(items or [])
    if not items:
        return {"verified": False, "refused": "NOTHING_TO_VERIFY",
                "why": "no items were handed in. 'Nothing wrong' about an "
                       "empty import is true of every import."}

    seen = []
    ready, wrong, unowned = [], [], []
    for item in items:
        item = getattr(item, "data", item)
        if not isinstance(item, dict):
            return {"verified": False, "refused": "NOT_AN_ITEM",
                    "why": "%r is not an item. Each is {name, kind, card}."
                           % (item,)}
        name = str(item.get("name") or "").strip()
        kind = str(item.get("kind") or "").strip().lower()
        if not name or not kind:
            return {"verified": False, "refused": "NOT_AN_ITEM",
                    "why": "an item is missing its %s. Both are needed: the "
                           "name is what a problem is reported against and "
                           "the kind is what decides who validates it."
                           % ("name" if not name else "kind")}
        if name in seen:
            return {"verified": False, "refused": "NOT_AN_ITEM",
                    "why": "'%s' appears twice. Two items under one name "
                           "means a problem reported against it belongs to "
                           "neither." % name}
        seen.append(name)

        card = item.get("card")
        if not isinstance(card, dict) or not card:
            return {"verified": False, "refused": "NO_CARD",
                    "item": name,
                    "why": "'%s' has no card to check. Verification reads "
                           "the card the migration built; without one "
                           "there is nothing to verify and saying so is "
                           "the only honest answer." % name}

        owner = OWNERS.get(kind)
        if owner is None:
            # NOT VERIFIED, AND NOT IN `ready`.
            unowned.append({
                "item": name, "kind": kind,
                "why": "nothing here validates a %r. That is not a fault - "
                       "not every migrated thing is a fragment or a skill "
                       "- but it must not land beside the ones that were "
                       "actually checked, because 'verified' meaning "
                       "'nobody looked' is the silent pass." % kind})
            continue

        who, validate = owner
        found = validate(_wrap(kind, card, name))
        row = {"item": name, "kind": kind, "by": who}
        if found:
            row["problems"] = list(found)
            row["why"] = ("%d problem(s) found by %s, in its own words."
                          % (len(found), who))
            wrong.append(row)
        else:
            ready.append(row)

    return {
        "verified": True, "saved": False, "of": len(items),
        "ready": ready, "problems": wrong, "no_validator": unowned,
        "why": "%d item(s): %d complete, %d with problems, %d nothing here "
               "validates. Nothing was saved."
               % (len(items), len(ready), len(wrong), len(unowned)),
        "unjudged": [
            "NOTHING WAS SAVED, AND NOTHING WAS OPENED. The cards were "
            "checked in memory, which is what makes 'before it is saved' "
            "true rather than merely intended - a validator that runs "
            "after the write has already lost the argument.",
            "%s" % ("%d ITEM(S) HAVE NO VALIDATOR HERE AND ARE NOT IN "
                    "`ready`: %s. Not every migrated thing is a fragment "
                    "or a skill, and there is nothing wrong with that - "
                    "but 'verified' meaning 'nobody looked' is the silent "
                    "pass HERON-IMP-DUP-007 refuses one step earlier."
                    % (len(unowned),
                       ", ".join("%s (%s)" % (one["item"], one["kind"])
                                 for one in unowned))
                    if unowned else
                    "every item was a kind somebody owns, so every one was "
                    "really checked."),
            "COMPLETE IS NOT CORRECT. A card in `ready` carries every "
            "field its kind requires, in the right shape. Nothing here "
            "compiled anything, ran anything or read an implementation.",
            "NO THIRD VALIDATOR WAS WRITTEN. %s - each kind went to the "
            "agent that owns what a complete one looks like, and their "
            "problems came back in their own words. A third would be a "
            "third thing that can be right on its own while disagreeing "
            "with the other two (PROPOSALS F17)."
            % ", ".join("%s for a %s" % (who, kind)
                        for kind, (who, _) in sorted(OWNERS.items())),
        ],
    }


def main(argv):
    print("IMPORT VERIFICATION   before it is saved")
    print("=" * 72)
    print("\nwho validates what")
    for kind, (who, _) in sorted(OWNERS.items()):
        print("  %-10s %s" % (kind, who))

    found, _ = FRAG.load_all()
    real = list(found.values())[0]
    skills, _ = SKILL.load_all()
    real_skill = list(skills.values())[0]

    answer = verify([
        {"name": real.slug, "kind": "fragment", "card": dict(real.data)},
        {"name": "half-migrated", "kind": "fragment",
         "card": {"id": "FRG-T-001"}},
        {"name": real_skill.id, "kind": "skill",
         "card": dict(real_skill.data)},
        {"name": "mep-notes", "kind": "document",
         "card": {"title": "MEP notes"}}])

    print("\n%s" % answer["why"])
    for one in answer["ready"]:
        print("  READY         %-24s checked by %s"
              % (one["item"], one["by"]))
    for one in answer["problems"]:
        print("  PROBLEMS      %-24s %s" % (one["item"], one["why"]))
        for line in one["problems"][:2]:
            print("                  %s" % line[:58])
    for one in answer["no_validator"]:
        print("  NO VALIDATOR  %-24s %s" % (one["item"], one["kind"]))

    print("\nrefused")
    for these in ([], ["a string"], [{"name": "x"}],
                  [{"name": "x", "kind": "fragment"}],
                  [{"name": "x", "kind": "fragment", "card": {}}],
                  [{"name": "x", "kind": "fragment", "card": {"id": "a"}},
                   {"name": "x", "kind": "skill", "card": {"id": "b"}}]):
        bad = verify(these)
        print("  %-22s %s" % (bad["refused"], bad["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
