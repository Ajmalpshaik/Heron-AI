# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-MRG-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Merging - the same shape is not the same thing, and the library says so
twice.

    python brain/heron_merge.py

WHAT IT IS FOR (docs/28, HERON-FRG-MRG-004)
--------------------------------------------
"Detects near-identical fragments; proposes merge, replace, keep
separate or deprecate." T2, risk SUGGEST. It merges nothing.

"NEAR-IDENTICAL" WITHOUT UNDERSTANDING THE LANGUAGE
-----------------------------------------------------
D-34 says Heron builds nothing to understand language, so "near" cannot
mean "these two descriptions read alike". What a fragment DOES have is a
contract - the names and types it needs and provides - and that is a
signature two fragments either share or do not.

Same signature is the detection. It is exact, it is mechanical, and it
is the only thing here that is decided rather than proposed.

SAME SHAPE IS NOT SAME THING, AND THE LIBRARY PROVES IT
---------------------------------------------------------
Measured on 2026-09-15 across 360 fragments: exactly two pairs share a
contract signature, and BOTH are pairs that must stay separate.

  dimension-family-instances / dimension-mep-runs
  transfer-materials-between-documents /
      transfer-view-filters-between-documents

Each pair takes the same arguments and hands back the same shape, and
each does a different job - DIMENSION_FAMILY_INSTANCES is not
DIMENSION_MEP_RUNS. The signature matched because the plumbing matched.

So a matching signature is a QUESTION, never an answer, and the one
answer this agent gives on its own is KEEP SEPARATE - when the two
declare different capabilities, the match is a coincidence of plumbing
and there is nothing to merge.

THE OTHER THREE VERDICTS ARE NOT THIS AGENT'S TO PICK
-------------------------------------------------------
Merge, replace and deprecate all turn on which implementation is better,
or which is the one people should use. That is not in either card. It is
the one scoped call that makes this row T2, and under D-01 it belongs to
the host - which is handed the pair and the evidence rather than a
recommendation dressed as a finding.

docs/09 s118 binds this row by name: "All three PROPOSE; none may apply
autonomously to anything at PRODUCTION."
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/09 s115's four. Only the third is decided here.
VERDICTS = ("merge", "replace", "keep separate", "deprecate")
KEEP_SEPARATE = VERDICTS[2]

# The state docs/09 s118 protects by name.
GUARDED = "PRODUCTION"

# Statuses that mean a fragment is already on its way out, so a pair
# involving one is not a question about which to keep.
LEAVING = ("DEPRECATED", "ARCHIVED")


def signature(fragment):
    """
    The contract, as a comparable shape - or (None, why).

    Names AND types, because a `doc:Document` and a `doc:View` are not
    the same argument however alike they read.
    """
    card = getattr(fragment, "data", fragment)
    contract = card.get("contract") if isinstance(card, dict) else None
    if not isinstance(contract, dict):
        return None, "it has no contract, so there is no shape to compare"
    out = []
    for side in ("needs", "provides"):
        entries = contract.get(side) or []
        if not isinstance(entries, list):
            return None, "its `%s` is not a list" % side
        shape = []
        for entry in entries:
            if not isinstance(entry, dict):
                return None, "an entry in `%s` is not a mapping" % side
            shape.append("%s:%s" % (str(entry.get("name") or "").strip(),
                                    str(entry.get("type") or "").strip()))
        out.append(tuple(sorted(shape)))
    return tuple(out), None


def _of(fragment, field):
    card = getattr(fragment, "data", fragment)
    return str((card or {}).get(field) or "").strip()


def look(fragments):
    """
    {pairs, decided, for_the_host, unreadable} - or a refusal. Nothing
    is merged, replaced or deprecated.
    """
    fragments = list(fragments or [])
    if len(fragments) < 2:
        return {"looked": False, "refused": "NOTHING_TO_COMPARE",
                "why": "%d fragment(s) were handed in. Near-identical is a "
                       "relation, and a relation needs two."
                       % len(fragments)}

    known, unreadable = {}, []
    for entry in fragments:
        card = getattr(entry, "data", entry)
        if not isinstance(card, dict):
            return {"looked": False, "refused": "NOT_A_FRAGMENT",
                    "why": "%r is not a fragment card." % (card,)}
        who = str(getattr(entry, "slug", None)
                  or card.get("id") or "").strip()
        if not who:
            return {"looked": False, "refused": "NOT_A_FRAGMENT",
                    "why": "a fragment has no id, so nothing here could "
                           "report a pair it is in."}
        if who in known:
            return {"looked": False, "refused": "DUPLICATE_ID",
                    "why": "'%s' appears twice. Two fragments under one id "
                           "would pair with themselves, which is not a "
                           "finding." % who}
        shape, why_not = signature(entry)
        if shape is None:
            # REPORTED, NEVER DROPPED. A fragment nothing can compare is
            # not a fragment that matched nothing.
            unreadable.append({"fragment": who, "why": why_not})
            continue
        known[who] = {"shape": shape, "entry": entry,
                      "capability": _of(entry, "capability"),
                      "status": _of(entry, "heron-status").upper()}

    by_shape = {}
    for who in sorted(known):
        by_shape.setdefault(known[who]["shape"], []).append(who)

    decided, asking = [], []
    for shape in sorted(by_shape, key=lambda one: by_shape[one][0]):
        group = by_shape[shape]
        if len(group) < 2:
            continue
        capabilities = sorted(set(known[who]["capability"] for who in group))
        rows = [{"fragment": who,
                 "capability": known[who]["capability"],
                 "status": known[who]["status"] or None} for who in group]
        if len(capabilities) > 1:
            # THE ONE ANSWER THIS AGENT GIVES ON ITS OWN.
            decided.append({
                "fragments": group, "verdict": KEEP_SEPARATE,
                "capabilities": capabilities, "rows": rows,
                "why": "%s take the same arguments and hand back the same "
                       "shape, and declare %d different capabilities: %s. "
                       "The signature matched because the plumbing "
                       "matched, and there is nothing to merge."
                       % (" and ".join(group), len(capabilities),
                          ", ".join(capabilities))})
            continue
        leaving = [who for who in group
                   if known[who]["status"] in LEAVING]
        asking.append({
            "fragments": group, "capability": capabilities[0] or None,
            "rows": rows, "leaving": leaving,
            "at_production": [who for who in group
                              if known[who]["status"] == GUARDED],
            "open": [one for one in VERDICTS if one != KEEP_SEPARATE],
            "why": "%s share a contract signature AND declare the same "
                   "capability %s. Which of %s applies turns on which "
                   "implementation is better, and that is in neither "
                   "card.%s"
                   % (" and ".join(group),
                      "%r" % capabilities[0] if capabilities[0]
                      else "(neither declares one)",
                      ", ".join(one for one in VERDICTS
                                if one != KEEP_SEPARATE),
                      " %s %s already on the way out."
                      % (" and ".join(leaving),
                         "is" if len(leaving) == 1 else "are")
                      if leaving else "")})

    paired = sum(len(one["fragments"]) for one in decided + asking)
    return {
        "looked": True, "of": len(known),
        "decided": decided, "for_the_host": asking,
        "unreadable": unreadable,
        "why": "%d fragment(s) compared: %d group(s) share a signature "
               "covering %d fragment(s) - %d decided here as '%s', %d left "
               "to the host%s."
               % (len(known), len(decided) + len(asking), paired,
                  len(decided), KEEP_SEPARATE, len(asking),
                  ", %d could not be compared" % len(unreadable)
                  if unreadable else ""),
        "unjudged": [
            "NOTHING WAS MERGED, REPLACED OR DEPRECATED. docs/09 s118: all "
            "three of split, merge and evolution PROPOSE, and none may "
            "apply autonomously to anything at %s. This agent's risk is "
            "SUGGEST and it writes nothing at all." % GUARDED,
            "%s" % ("%d GROUP(S) ARE THE HOST'S TO DECIDE: %s. Merge, "
                    "replace and deprecate all turn on which "
                    "implementation is better, which is in neither card - "
                    "that is the one scoped call making this row T2, and "
                    "under D-01 it is the host's. What went across is the "
                    "pair and the evidence, not a recommendation dressed "
                    "as a finding."
                    % (len(asking),
                       "; ".join(" and ".join(one["fragments"])
                                 for one in asking))
                    if asking else
                    "no group shares both a signature and a capability, so "
                    "nothing needed the host."),
            "%s" % ("%d GROUP(S) WERE DECIDED HERE AS '%s' - same shape, "
                    "different capability. Measured across the whole "
                    "library on 2026-09-15, BOTH of the only two matching "
                    "pairs were this: the signature matched because the "
                    "plumbing matched."
                    % (len(decided), KEEP_SEPARATE) if decided else
                    "nothing was decided here; no pair shares a signature "
                    "while declaring different capabilities."),
            "%s" % ("%d FRAGMENT(S) COULD NOT BE COMPARED AND ARE LISTED "
                    "RATHER THAN DROPPED: %s. A fragment nothing can "
                    "compare is not a fragment that matched nothing."
                    % (len(unreadable),
                       ", ".join(one["fragment"] for one in unreadable))
                    if unreadable else
                    "every fragment handed in had a contract to compare."),
            "WHAT ANY OF THEM DOES. Only the contract was read - the names "
            "and types in and out. Two fragments doing utterly different "
            "work through the same plumbing look identical here, which is "
            "why a matching signature is a question rather than an answer.",
        ],
    }


def main(argv):
    print("MERGING   the same shape is not the same thing")
    print("=" * 72)
    print("\ndocs/09 s115's four verdicts: %s" % ", ".join(VERDICTS))
    print("only '%s' is decided here" % KEEP_SEPARATE)

    found, problems = FRAG.load_all()
    answer = look(list(found.values()))
    print("\n%s" % answer["why"])
    for one in answer["decided"]:
        print("\n  %s" % one["verdict"].upper())
        print("  %s" % one["why"])
    for one in answer["for_the_host"]:
        print("\n  FOR THE HOST  %s" % one["why"])

    print("\nrefused")
    two = list(found.values())[:2]
    for these in ([], two[:1], ["a string", two[0]],
                  [{"id": ""}, two[0]], [two[0], two[0]]):
        bad = look(these)
        print("  %-22s %s" % (bad["refused"], bad["why"][:44]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
