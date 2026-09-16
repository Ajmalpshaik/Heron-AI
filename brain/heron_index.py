# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-IDX-013
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Import indexing - it builds no index, and presented is not accepted.

    python brain/heron_index.py

WHAT IT IS FOR (docs/28, HERON-IMP-IDX-013)
--------------------------------------------
"Indexes the accepted result." T1, risk MODIFY. Step 15 of docs/00
s28's sixteen.

THERE ARE ALREADY TWO INDEXES AND THIS AGENT WRITES NEITHER
-------------------------------------------------------------
HERON-RAG-IDX-010 is the keyword index (`heron_search.index`) and
HERON-RAG-EMB-008 is the vector index (`heron_embed.index`), with a
chunk half of each for documents. Golden Rule 3 is reuse proven
knowledge before creating new, and a third index built here would be a
third thing to keep in step with the other two.

So this agent calls them. The functions are bound by identity, not
copied, and the suite checks that they are the same objects.

PRESENTED IS NOT ACCEPTED
---------------------------
HERON-IMP-APR-014 returns `{presented, items, why}` and accepts
nothing - its own docstring says so: "It presents; it does not accept."
Handing its answer straight to this agent is the easiest mistake in
the whole pipeline, and it would put an unreviewed import into the
library.

So an accepted result carries `accepted: True` and a `by` - and an
acceptance nobody signed is not an acceptance. Golden Rule 7: no agent
approves itself.

THE MOMENT THIS RUNS, THE IMPORT IS FINDABLE
----------------------------------------------
`heron_retrieve.OFFERABLE` reads:

    DISCOVERED  DRAFT  TESTING  VALIDATED  PROVEN  PRODUCTION

DISCOVERED is in it. Everything imported enters at DISCOVERED
(HERON-IMP-APR-014), so indexing is the step where somebody else's
code starts coming back as an answer to a modeller's question. It is
ranked below proven work and its status travels with it, but it is
offered. That is not a bug and it is not hidden either - the answer
says it out loud, because it is the thing a human is actually agreeing
to when they press accept.

YOU CANNOT INDEX WHAT THE LIBRARY DOES NOT HOLD
-------------------------------------------------
docs/00 s28 and docs/10 s5 both order the pipeline "... validate ->
index -> save". Both indexers read the library - `store.fragments()`
and `heron_fragment.load_all()` - so an item that is still only a
manifest row is invisible to them whatever step it is at.

This agent does not resolve that. It reports every accepted fragment
the library does not hold, by id, in `not_in_the_library`, so the
pipeline owner sees exactly which items the index could not reach.
The ordering question is PROPOSALS F26.

NOTHING IN THIS PROJECT INDEXES A SKILL
-----------------------------------------
`heron_search` and `heron_embed` know fragments and document chunks.
Neither mentions skills, so an accepted skill comes back in `no_index`
saying so, rather than being counted as indexed. Golden Rule 14: never
silently discard.

WHAT IT WRITES
----------------
Index rows, which is why the register gives this row MODIFY. No
fragment, no file, no status. Whether it should run now is
HERON-OPS-SCH-001's - docs/11 s157 says re-indexing must not compete
with a user who is modelling.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_embed as EMBED  # noqa: E402
import heron_import as IMPORT  # noqa: E402
import heron_promotion as PRO  # noqa: E402
import heron_retrieve as RETRIEVE  # noqa: E402
import heron_search as SEARCH  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# HERON-IMP-APR-014's manifest shape, borrowed rather than restated. An
# item being indexed needs its library `id` on top of these three.
AN_ITEM_CARRIES = IMPORT.AN_ITEM_CARRIES
NEEDS_AN_ID = "id"

# The rung everything imported enters at, and the statuses a fragment may
# be offered at. Both by identity - DISCOVERED being in the second list is
# the fact this agent exists to say out loud.
ENTERS_AT = IMPORT.ENTERS_AT
OFFERABLE = RETRIEVE.OFFERABLE

# The two indexes that already exist. Bound, not reimplemented.
INDEXES = {
    "fragment": (("keyword", SEARCH.index), ("vector", EMBED.index)),
    "document": (("keyword", SEARCH.index_chunks),
                 ("vector", EMBED.index_chunks)),
}

# What nothing here indexes, and who to ask instead.
NO_INDEX = {
    "skill": "neither heron_search nor heron_embed knows what a skill "
             "is - the word appears in neither. Skills live in "
             "brain/skills and are loaded by name (heron_skill.load_all), "
             "not searched for",
}


def _counts(answer):
    """Both indexers return a number or a (done, skipped) pair."""
    if isinstance(answer, tuple):
        return int(answer[0]), int(answer[1])
    return int(answer), 0


def index_accepted(store, accepted):
    """
    {indexed, ran, found, not_in_the_library, no_index} - or a refusal.
    No index is built here; the two that exist are called.
    """
    if not accepted:
        return {"indexed": False, "refused": "NOTHING_TO_INDEX",
                "why": "no accepted result was handed in."}

    accepted = getattr(accepted, "data", accepted)
    if not isinstance(accepted, dict):
        return {"indexed": False, "refused": "NOT_ACCEPTED",
                "why": "%r is not an accepted result. One carries "
                       "`accepted`, `by` and `items`." % (accepted,)}

    if accepted.get("accepted") is not True:
        return {"indexed": False, "refused": "NOT_ACCEPTED",
                "why": ("this result was PRESENTED, not accepted. "
                        "HERON-IMP-APR-014 prepares a manifest somebody "
                        "can say yes to and accepts nothing itself - "
                        "somebody still has to say yes."
                        if accepted.get("presented") is not None else
                        "the result does not say it was accepted. "
                        "Nothing enters the library on a maybe.")}

    by = str(accepted.get("by") or "").strip()
    if not by:
        return {"indexed": False, "refused": "NOT_ACCEPTED",
                "why": "the acceptance is not signed. An acceptance nobody "
                       "made is not an acceptance - Golden Rule 7, no agent "
                       "approves itself, and `by` is where a human's name "
                       "goes."}
    # AND IT IS A PERSON'S NAME, not any non-empty string. `ci`, `bot` and
    # an agent id all read as signed while nobody has looked at the import
    # at all. HERON-FRG-PRO-00X's own test, bound rather than a second
    # list of the words a machine signs with.
    if not PRO._person(by):
        return {"indexed": False, "refused": "ACCEPTED_BY_A_MACHINE",
                "by": by,
                "why": "'%s' accepted it, which is not a person. Golden "
                       "Rule 7: no agent approves itself, and an import "
                       "signed by the pipeline that fetched it is that "
                       "rule with the sign painted over. The promotion and "
                       "contribution gates apply the same test." % by}

    items = accepted.get("items")
    if not isinstance(items, list) or not items:
        return {"indexed": False, "refused": "NOTHING_TO_INDEX",
                "why": "the accepted result carries no items. An empty "
                       "import is not an import with nothing in it."}

    if store is None:
        return {"indexed": False, "refused": "NO_LIBRARY",
                "why": "there is no library to index into. The scope is "
                       "opened by whoever runs the pipeline, not here."}

    wanted = dict((field, why) for field, why in AN_ITEM_CARRIES)
    kinds, cards = {}, []
    for item in items:
        item = getattr(item, "data", item)
        if not isinstance(item, dict):
            return {"indexed": False, "refused": "NOT_AN_ITEM",
                    "why": "%r is not an item. Each carries %s, and an `id` "
                           "besides." % (item, ", ".join(sorted(wanted)))}
        absent = [field for field in sorted(wanted) + [NEEDS_AN_ID]
                  if not str(item.get(field) or "").strip()]
        if absent:
            return {"indexed": False, "refused": "NOT_AN_ITEM",
                    "missing": absent,
                    "why": "an item is missing %s. By this step the id has "
                           "been asked for (HERON-IMP-REN-010) and answered "
                           "- an item without one cannot be found again."
                           % ", ".join(absent)}
        card = {"id": str(item[NEEDS_AN_ID]).strip(),
                "name": str(item["name"]).strip(),
                "kind": str(item["kind"]).strip().lower()}
        cards.append(card)
        kinds.setdefault(card["kind"], []).append(card)

    ran, refused = {}, []
    for kind in sorted(kinds):
        if kind in NO_INDEX:
            for card in kinds[kind]:
                refused.append(dict(card, why=NO_INDEX[kind]))
            continue
        if kind not in INDEXES:
            for card in kinds[kind]:
                refused.append(dict(card,
                                    why="nothing in this project indexes a "
                                        "%r. It is not a fragment and not a "
                                        "document, and guessing which index "
                                        "it belongs in is not indexing it"
                                        % kind))
            continue
        for named, run in INDEXES[kind]:
            done, skipped = _counts(run(store))
            ran["%s %s" % (kind, named)] = {"written": done,
                                            "unchanged": skipped}

    held = set(row["id"] for row in store.fragments())
    found, missing = [], []
    for card in cards:
        if card["kind"] != "fragment":
            continue
        (found if card["id"] in held else missing).append(card)

    return {
        "indexed": True,
        "by": by,
        "of": len(cards),
        "ran": ran,
        "found": found,
        "not_in_the_library": [
            dict(card,
                 why="accepted, but the library holds no fragment with this "
                     "id. Both indexers read the library, so an item that "
                     "is not saved cannot be indexed whatever step the "
                     "pipeline calls it (PROPOSALS F26)")
            for card in missing],
        "no_index": refused,
        "why": "%d accepted by %s: %d fragment%s now in the index, %d the "
               "library does not hold, %d nothing here indexes. %s"
               % (len(cards), by, len(found), "" if len(found) == 1 else "s",
                  len(missing), len(refused),
                  ", ".join("%s wrote %d" % (named, counts["written"])
                            for named, counts in sorted(ran.items()))
                  or "no index was run"),
        "unjudged": [
            "NO INDEX WAS BUILT HERE. HERON-RAG-IDX-010's and "
            "HERON-RAG-EMB-008's were called, by identity. A third index "
            "would be a third thing to keep in step with the other two "
            "(Golden Rule 3).",
            "WHAT IS NOW FINDABLE IS FINDABLE BY THE USER. Everything "
            "imported enters at %s, and %s is in heron_retrieve's "
            "OFFERABLE list. Ranked below proven work, with its status "
            "travelling with it - but offered. That is what accepting an "
            "import means, and it is said here rather than discovered "
            "later." % (ENTERS_AT, ENTERS_AT),
            ("%d accepted fragment%s the library does not hold, so the "
             "index could not reach %s. docs/10 s5 orders index before "
             "save and both indexers read the library - PROPOSALS F26."
             % (len(missing), "" if len(missing) == 1 else "s",
                "it" if len(missing) == 1 else "them")
             if missing else
             "every accepted fragment was already in the library, so the "
             "index reached all of them."),
            ("%d item%s nothing here indexes, named rather than counted as "
             "done: %s." % (len(refused), "" if len(refused) == 1 else "s",
                            ", ".join(sorted(set(card["kind"]
                                                 for card in refused))))
             if refused else
             "every accepted item was of a kind one of the two indexes "
             "covers."),
            "NO FRAGMENT, FILE OR STATUS WAS WRITTEN. Index rows only - "
            "nothing was promoted, and an import is still %s afterwards."
            % ENTERS_AT,
            "NEITHER INDEX IS INCREMENTAL IN THE SAME WAY, AND THAT IS "
            "THEIRS TO DECIDE. The keyword index deletes and rebuilds the "
            "whole library every run; the vector index is content-hashed "
            "and re-embeds only what changed. So importing one fragment "
            "still rewrites every keyword row - %d of them here."
            % sum(counts["written"] for named, counts in ran.items()
                  if named.endswith("keyword")),
            "WHETHER THIS SHOULD RUN NOW IS NOT THIS AGENT'S. Re-indexing "
            "is heavy for exactly that reason, and docs/11 s157 says it "
            "must not compete with a user who is modelling. "
            "HERON-OPS-SCH-001 decides.",
        ],
    }


def main(argv):
    import shutil
    import tempfile

    print("IMPORT INDEXING   it builds no index, and presented is not "
          "accepted")
    print("=" * 72)
    print("\ncalled, not reimplemented")
    for kind in sorted(INDEXES):
        for named, run in INDEXES[kind]:
            print("  %-10s %-8s %s.%s" % (kind, named, run.__module__,
                                          run.__name__))
    print("\nenters at %s, and OFFERABLE is %s"
          % (ENTERS_AT, ", ".join(OFFERABLE)))

    home = tempfile.mkdtemp(prefix="heron-index-")
    was = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        import heron_scope as SCOPE
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        real = sorted(row["id"] for row in store.fragments())[:1]

        answer = index_accepted(store, {
            "accepted": True, "by": "Ajmal",
            "items": [
                {"id": real[0] if real else "FRG-ELE-001",
                 "name": "CountDucts.py", "kind": "fragment",
                 "from": "AJ-Tools/tools/CountDucts.py"},
                {"id": "FRG-ZZZ-999", "name": "NeverSaved.py",
                 "kind": "fragment", "from": "AJ-Tools/tools/NeverSaved.py"},
                {"id": "count-things", "name": "count things",
                 "kind": "skill", "from": "AJ-Tools/skills"},
                {"id": "DOC-1", "name": "AEB standard", "kind": "poster",
                 "from": "AJ-Tools/docs"}]})

        print("\n%s" % answer["why"])
        for named, counts in sorted(answer["ran"].items()):
            print("  RAN      %-18s wrote %d, unchanged %d"
                  % (named, counts["written"], counts["unchanged"]))
        for card in answer["found"]:
            print("  INDEXED  %-18s %s" % (card["id"], card["name"]))
        for card in answer["not_in_the_library"]:
            print("  MISSING  %-18s %s" % (card["id"], card["why"][:44]))
        for card in answer["no_index"]:
            print("  NO INDEX %-18s %s" % (card["kind"], card["why"][:44]))

        print("\nrefused")
        for bad in (None,
                    {"presented": True, "items": [{"name": "x"}]},
                    {"accepted": True, "by": "Ajmal", "items": []},
                    {"accepted": True, "by": "", "items": [{"name": "x"}]},
                    {"accepted": True, "by": "Ajmal",
                     "items": [{"name": "x", "kind": "fragment"}]}):
            said = index_accepted(store, bad)
            print("  %-18s %s" % (said["refused"], said["why"][:44]))
        said = index_accepted(None, {"accepted": True, "by": "Ajmal",
                                     "items": [{"id": "a", "name": "a",
                                                "kind": "fragment",
                                                "from": "a"}]})
        print("  %-18s %s" % (said["refused"], said["why"][:44]))

        print("\nwhat this agent does not judge")
        for line in answer["unjudged"]:
            print("  - %s" % line)
    finally:
        if was is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = was
        shutil.rmtree(home, ignore_errors=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
