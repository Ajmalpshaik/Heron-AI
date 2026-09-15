# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-DUP-007
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Duplicate detection - and "nothing comparable" is not "nothing like it".

    python brain/heron_duplicates.py

WHAT IT IS FOR (docs/28, HERON-IMP-DUP-007)
--------------------------------------------
"Compares against existing knowledge BEFORE ANYTHING IS CREATED." T1,
risk READ.

THE COMPARISONS ARE ALREADY OWNED, SO THEY ARE BORROWED
---------------------------------------------------------
HERON-FRG-MRG-004 decides whether two fragments are near-identical, and
it does it on the contract signature - names and types in and out.
That function is imported, not rewritten, so the two agents cannot
disagree about what "the same shape" means.

What this agent adds is the import's own question: not "are these two
library fragments alike" but "does the library ALREADY have this thing
somebody is about to bring in".

THE FINDING THIS AGENT EXISTS FOR
-----------------------------------
An imported item usually arrives as raw code. It has a file name and
maybe a purpose; it does not yet have a Heron capability or a declared
contract, because those are things the migration has not done yet.

Which means the comparisons mostly CANNOT RUN. And an agent that
answers "no duplicate found" when it had nothing to compare is the
silent pass this project keeps catching itself in - the same shape as a
gate nobody ran reading as a gate that passed.

So an item with nothing comparable comes back UNCHECKED, in its own
list, never in `new`. "Nothing comparable" and "nothing like it" are
different answers and only one of them is evidence.

WHAT IS COMPARED, AND WHAT IS NOT
-----------------------------------
  capability   exact. 360 fragments hold 360 distinct capabilities, so
               a second claim is decisive rather than suggestive.
  signature    HERON-FRG-MRG-004's, imported. Same shape in and out.
  name         exact, after flattening case and spacing.

  what it READS LIKE   not compared, and never will be. D-34: Heron
               builds nothing to understand language, so "CountDucts.py
               looks like count-elements" is not a finding this agent
               can make. It is the host's under D-01.

COMPARING AGAINST NOTHING IS REFUSED
--------------------------------------
Handed an empty library, every item is new and the answer is worthless.
Refused rather than returned, for the same reason as above.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_merge as MRG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# HERON-FRG-MRG-004's, imported so the two agents cannot disagree about
# what "the same shape" means.
signature = MRG.signature

# What an item can be compared BY, strongest first. The order is the
# argument: a capability collision is decisive, a matching signature is
# a question, and a matching name is neither on its own.
BY = ("capability", "signature", "name")


def _flat(value):
    return " ".join(str(value or "").strip().lower().split())


def _card(thing):
    return getattr(thing, "data", thing)


def look(items, fragments=(), skills=()):
    """
    {already, unchecked, new, why} - or a refusal. Nothing is created.
    """
    items = list(items or [])
    if not items:
        return {"looked": False, "refused": "NOTHING_TO_CHECK",
                "why": "no items were handed in. 'No duplicates' about "
                       "nothing is true of every import and useful about "
                       "none."}

    known = list(fragments or []) + list(skills or [])
    if not known:
        return {"looked": False, "refused": "NO_LIBRARY",
                "why": "nothing was handed in to compare against, so every "
                       "item would come back new. An answer that cannot be "
                       "wrong is not evidence - the same reason an "
                       "unchecked item is not reported as new."}

    by_capability, by_signature, by_name = {}, {}, {}
    for entry in known:
        card = _card(entry)
        if not isinstance(card, dict):
            continue
        who = str(getattr(entry, "slug", None)
                  or card.get("id") or "").strip()
        if not who:
            continue
        capability = str(card.get("capability") or "").strip()
        if capability:
            by_capability.setdefault(capability, []).append(who)
        shape, _ = signature(entry)
        if shape:
            by_signature.setdefault(shape, []).append(who)
        by_name.setdefault(_flat(card.get("name") or who), []).append(who)

    seen = []
    already, unchecked, fresh = [], [], []
    for item in items:
        item = _card(item)
        if not isinstance(item, dict):
            return {"looked": False, "refused": "NOT_AN_ITEM",
                    "why": "%r is not an item. Each carries a name and, "
                           "once migration has given it one, a capability "
                           "or a contract." % (item,)}
        name = str(item.get("name") or "").strip()
        if not name:
            return {"looked": False, "refused": "NOT_AN_ITEM",
                    "why": "an item has no name, so nothing here could "
                           "report a finding against it."}
        if _flat(name) in seen:
            return {"looked": False, "refused": "DUPLICATE_NAME",
                    "why": "'%s' appears twice in the import itself. Two "
                           "items under one name would each be compared "
                           "and neither reported clearly." % name}
        seen.append(_flat(name))

        hits, could = [], []
        capability = str(item.get("capability") or "").strip()
        if capability:
            could.append("capability")
            for who in by_capability.get(capability, []):
                hits.append({"by": "capability", "is": who,
                             "on": capability,
                             "why": "'%s' already provides %s, and a "
                                    "capability has exactly one provider "
                                    "across the whole library - so this is "
                                    "decisive rather than suggestive."
                                    % (who, capability)})
        shape, _ = signature(item)
        if shape:
            could.append("signature")
            for who in by_signature.get(shape, []):
                hits.append({"by": "signature", "is": who,
                             "why": "'%s' takes the same arguments and "
                                    "hands back the same shape. "
                                    "HERON-FRG-MRG-004's test, imported - "
                                    "and its own finding is that a "
                                    "matching signature is a QUESTION, "
                                    "because the plumbing can match while "
                                    "the job differs." % who})
        could.append("name")
        for who in by_name.get(_flat(name), []):
            hits.append({"by": "name", "is": who,
                         "why": "'%s' is already called that. On its own a "
                                "matching name is the weakest of the "
                                "three - two people naming two different "
                                "things the same is ordinary." % who})

        row = {"item": name, "compared_by": could, "matches": hits}
        if hits:
            already.append(row)
        elif could == ["name"]:
            # NOTHING COMPARABLE IS NOT NOTHING LIKE IT.
            row["why"] = ("'%s' has no capability and no contract yet, so "
                          "only its NAME could be compared. Migration has "
                          "not given it the two things that would make a "
                          "real comparison possible, and reporting it as "
                          "new would be reporting a check that did not "
                          "happen." % name)
            unchecked.append(row)
        else:
            fresh.append(row)

    return {
        "looked": True, "of": len(items), "against": len(known),
        "already": already, "unchecked": unchecked, "new": fresh,
        "why": "%d item(s) against %d known: %d already here, %d could not "
               "be compared, %d new."
               % (len(items), len(known), len(already), len(unchecked),
                  len(fresh)),
        "unjudged": [
            "%s" % ("%d ITEM(S) COULD NOT BE COMPARED AND ARE NOT IN "
                    "`new`: %s. They have no capability and no contract "
                    "yet, so only a name was available - and an agent that "
                    "answers 'no duplicate' when it had nothing to compare "
                    "is the silent pass this project keeps catching itself "
                    "in." % (len(unchecked),
                             ", ".join(one["item"] for one in unchecked))
                    if unchecked else
                    "every item carried a capability or a contract, so "
                    "every one was really compared."),
            "WHAT ANY OF IT READS LIKE. D-34: Heron builds nothing to "
            "understand language, so 'CountDucts.py looks like "
            "count-elements' is not a finding this agent can make. It is "
            "the host's under D-01.",
            "WHETHER A MATCH IS A PROBLEM. A matching capability is "
            "decisive, a matching signature is a question - "
            "HERON-FRG-MRG-004's own finding is that the plumbing can "
            "match while the job differs - and a matching name on its own "
            "is neither.",
            "NOTHING WAS CREATED, WHICH IS THE POINT OF RUNNING BEFORE "
            "ANYTHING IS. HERON-IMP-APR-014 presents what survives this, "
            "and everything enters at DISCOVERED.",
        ],
    }


def main(argv):
    print("DUPLICATE DETECTION   before anything is created")
    print("=" * 72)
    print("\ncompared by, strongest first: %s" % ", ".join(BY))

    library = [
        {"id": "count-them", "name": "count elements",
         "capability": "COUNT_ELEMENTS",
         "contract": {"needs": [{"name": "doc", "type": "Document"}],
                      "provides": [{"name": "count", "type": "int"}]}},
        {"id": "filter-them", "name": "filter by category",
         "capability": "FILTER_ELEMENTS",
         "contract": {"needs": [{"name": "doc", "type": "Document"}],
                      "provides": [{"name": "elements",
                                    "type": "IList<Element>"}]}}]

    answer = look([
        {"name": "CountDucts.py"},
        {"name": "count elements", "capability": "COUNT_ELEMENTS"},
        {"name": "TagSheet.py", "capability": "TAG_SHEET",
         "contract": {"needs": [{"name": "doc", "type": "Document"}],
                      "provides": [{"name": "count", "type": "int"}]}},
        {"name": "BrandNew.py", "capability": "SOMETHING_ELSE",
         "contract": {"needs": [], "provides": []}}],
        fragments=library)

    print("\n%s" % answer["why"])
    for one in answer["already"]:
        print("\n  ALREADY HERE  %s" % one["item"])
        for hit in one["matches"]:
            print("    by %-10s %s" % (hit["by"], hit["why"][:52]))
    for one in answer["unchecked"]:
        print("\n  UNCHECKED     %s" % one["why"][:66])
    for one in answer["new"]:
        print("\n  NEW           %s (compared by %s)"
              % (one["item"], ", ".join(one["compared_by"])))

    print("\nrefused")
    for these, known in (([], library), ([{"name": "x"}], []),
                         (["a string"], library),
                         ([{"name": ""}], library),
                         ([{"name": "x"}, {"name": "X"}], library)):
        bad = look(these, fragments=known)
        print("  %-20s %s" % (bad["refused"], bad["why"][:44]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
