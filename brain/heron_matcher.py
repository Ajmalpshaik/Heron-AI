# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RAG-FMT-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Fragment matching - a near match is not a match.

    python brain/heron_matcher.py

WHAT IT IS FOR (docs/28, HERON-RAG-FMT-004)
--------------------------------------------
"Matches the request against existing fragments." T2, risk READ. It sits
after HERON-RAG-RET-003, which retrieves CANDIDATES, and answers a
different question: does any of them actually DO the thing that was
asked.

Retrieval scores. Matching decides. That is why the register gives
retrieval T1 and this T2 - and it is also why this agent's most useful
answer is often "nothing here does this".

THE MATCH IS ON THE DECLARED CONTRACT, NOT ON THE NAME
--------------------------------------------------------
A fragment called `filter-by-size` that declares no size input does not
filter by size. D-29 made the contract DATA rather than prose exactly so
this could be checked: `needs` and `provides` are named, typed entries,
and a match is a question about them.

So three things must line up, and all three are read from the card:

    the capability     equal, not similar. `APPLY_VIEW_FILTER` is not
                       `CREATE_VIEW_FILTER`, and a near name is the
                       most expensive kind of near miss.
    every `source:     the caller must be able to supply each one. A
    request` need      fragment needing a resolved FilterElement cannot
                       run for somebody holding only a name.
    what it provides   the request asked for an outcome; a fragment that
                       does not declare it does not deliver it.

A NEAR MATCH IS REPORTED AS PARTIAL AND NEVER AS THE ANSWER
-------------------------------------------------------------
This is the whole point of the agent. A fragment that satisfies four of
five needs will run, half-work, and produce a confident wrong result -
and it will look exactly like a success. So a partial match goes in its
own list, with what was missing named, and no caller can reach it by
reading `matched` alone.

NOTHING MATCHING IS A FIRST-CLASS ANSWER, NOT AN EMPTY LIST
-------------------------------------------------------------
When nothing does the job, the useful output is the BRIEF: which
capability was wanted, what was missing from the nearest thing, and how
many were excluded for the wrong Revit. That is what
HERON-RAG-RSH-017 needs to research it, and what somebody writing a new
fragment needs to write it. An empty list carries none of that.

THE REVIT VERSION IS A WALL, NOT A WEIGHTING (docs/05 s8)
-----------------------------------------------------------
HERON-RAG-RNK-006's own module says why, and this agent obeys the same
rule rather than a softer version of it:

    "A fragment written for Revit 2021, applied in 2025, does not
    announce itself. It runs, it half-works, and the damage is found
    later by somebody measuring something. That is the
    confident-wrong-retrieval failure, and a ranking cannot protect
    against it."

So a fragment that does not declare the running version is EXCLUDED, and
with no version named at all this agent REFUSES rather than matching
everything. Guessing a release is what D-05 forbids, and here the guess
would be invisible in the answer.

STATUS IS REPORTED, NEVER PREFERRED QUIETLY
---------------------------------------------
A DRAFT fragment that matches is a match. It is not silently ranked
below a PROVEN one, because that is a judgement about risk and the
caller is the one carrying the risk. The status is on every match and
the ordering is not this agent's opinion.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# A need the CALLER must supply, as opposed to one the fragment finds for
# itself. Read from the card's own `source:` field.
FROM_REQUEST = "request"


def _named(entries):
    """The names in a contract side, in order."""
    out = []
    for entry in (entries or []):
        if isinstance(entry, dict):
            name = str(entry.get("name") or "").strip()
            if name:
                out.append(name)
    return out


def _wanted(entries):
    """The names a fragment needs the CALLER to hand it."""
    out = []
    for entry in (entries or []):
        if not isinstance(entry, dict):
            continue
        if str(entry.get("source") or "").strip().lower() != FROM_REQUEST:
            continue
        name = str(entry.get("name") or "").strip()
        if name:
            out.append(name)
    return out


def match(request, fragments, revit=None):
    """
    {matched, partial, excluded, brief, why} - or a refusal.

    Nothing is run. `fragments` are the cards handed in, the same shape
    heron_fragment.load_all returns.
    """
    if not isinstance(request, dict):
        return {"ran": False, "refused": "NOT_A_REQUEST",
                "why": "a request is {capability, has, wants}. A %s is not "
                       "one, and reading a capability out of something else "
                       "is guessing at what was asked."
                       % type(request).__name__}

    capability = str(request.get("capability") or "").strip().upper()
    if not capability:
        return {"ran": False, "refused": "NOT_A_REQUEST",
                "why": "the request names no capability. D-01 gives the host "
                       "the job of turning a sentence into one, and this "
                       "agent does not do it instead - it would be language "
                       "understanding, which D-34 refuses."}

    version = str(revit or "").strip()
    if not version:
        return {"ran": False, "refused": "NO_REVIT_VERSION",
                "why": "no Revit version was named, so the version wall "
                       "cannot be applied. docs/05 s8 makes it a wall and "
                       "not a weighting: a fragment written for 2021 and "
                       "applied in 2025 does not announce itself, it "
                       "half-works, and the damage is found later by "
                       "somebody measuring something. Matching everything "
                       "instead would hide that, and guessing a release is "
                       "what D-05 forbids."}
    if version not in FRAG.REVIT_VERSIONS:
        return {"ran": False, "refused": "NO_REVIT_VERSION",
                "why": "'%s' is not one of the releases this project "
                       "supports: %s. D-05 does not extrapolate to a "
                       "release nobody has tested."
                       % (version, ", ".join(FRAG.REVIT_VERSIONS))}

    if not fragments:
        return {"ran": False, "refused": "NOTHING_TO_MATCH",
                "why": "no fragments were handed in. That is a statement "
                       "about the call rather than about the library - "
                       "HERON-RAG-RET-003 retrieves the candidates and this "
                       "agent does not go looking for them."}

    has = set(str(each).strip() for each in (request.get("has") or []))
    wants = [str(each).strip() for each in (request.get("wants") or [])]

    # Materialised once: a view or a generator read twice is a bug waiting
    # for the day somebody passes the second kind.
    cards = list(fragments.values() if isinstance(fragments, dict)
                 else fragments)

    matched, partial, excluded, wrong_version = [], [], [], 0
    for card in cards:
        # heron_fragment.load_all returns Fragment objects, which are the
        # card PLUS where it was found. Only the card is matched on: where a
        # fragment sits is not what it does, and heron_fragment's own
        # comment says the folder is where it was FOUND, never what it IS.
        card = getattr(card, "data", card)
        if not isinstance(card, dict):
            return {"ran": False, "refused": "NOT_A_FRAGMENT",
                    "why": "%r is not a fragment card. Each is the map "
                           "heron_fragment.load returns." % (card,)}

        identifier = str(card.get("id") or "").strip()
        theirs = str(card.get("capability") or "").strip().upper()
        if not identifier or not theirs:
            return {"ran": False, "refused": "NOT_A_FRAGMENT",
                    "why": "a card carries no %s. Both are required before "
                           "it can be matched or honestly excluded."
                           % ("id" if not identifier else "capability")}

        releases = [str(each).strip() for each in (card.get("revit") or [])]
        # THE WALL, and it is applied BEFORE the capability is even looked
        # at: a wrong-version fragment is not a weaker candidate, it is not
        # a candidate.
        if version not in releases:
            wrong_version += 1
            excluded.append({"id": identifier, "capability": theirs,
                             "refused": "WRONG_REVIT_VERSION",
                             "declares": releases,
                             "why": "declares %s and this is %s. A wall, "
                                    "not a weighting - ranking it fifth "
                                    "instead of first still returns it on a "
                                    "quiet day when nothing else matches."
                                    % (", ".join(releases) or "nothing",
                                       version)})
            continue

        if theirs != capability:
            continue

        contract = card.get("contract") or {}
        needs = _wanted(contract.get("needs"))
        provides = _named(contract.get("provides"))
        missing = [name for name in needs if name not in has]
        undelivered = [name for name in wants if name not in provides]

        entry = {"id": identifier, "capability": theirs,
                 "status": str(card.get("heron-status") or "").strip()
                 or "UNSTATED",
                 "source": str(card.get("source") or "").strip() or "UNSTATED",
                 "risk": str(card.get("risk") or "").strip() or "UNSTATED",
                 "needs": needs, "provides": provides}

        if missing or undelivered:
            partial.append(dict(
                entry, missing=missing, undelivered=undelivered,
                why="%s. A near match is not a match: this would run, "
                    "half-work, and look exactly like a success."
                    % "; ".join(filter(None, [
                        "needs %s and the caller has none of it"
                        % ", ".join(missing) if missing else "",
                        "does not provide %s" % ", ".join(undelivered)
                        if undelivered else ""]))))
            continue

        matched.append(dict(entry, why="capability, every requested need "
                                       "and everything asked for line up, "
                                       "and it declares %s." % version))

    brief = None
    if not matched:
        nearest = sorted(partial,
                         key=lambda one: len(one["missing"])
                         + len(one["undelivered"]))[:1]
        brief = {
            "capability": capability,
            "revit": version,
            "nearest": nearest[0]["id"] if nearest else None,
            "missing": (nearest[0]["missing"] + nearest[0]["undelivered"])
            if nearest else [],
            "excluded_for_version": wrong_version,
            "why": "nothing does '%s' on Revit %s. %s %s"
                   % (capability, version,
                      "The nearest is %s, short of %s."
                      % (nearest[0]["id"],
                         ", ".join(nearest[0]["missing"]
                                   + nearest[0]["undelivered"]))
                      if nearest else "Nothing came close.",
                      "%d candidate(s) were excluded for the Revit version."
                      % wrong_version if wrong_version else
                      "No candidate was excluded for its version.")}

    return {
        "ran": False, "capability": capability, "revit": version,
        "matched": matched, "partial": partial, "excluded": excluded,
        "brief": brief, "of": len(cards),
        "why": "'%s' on Revit %s: %d match(es), %d partial, %d excluded. %s"
               % (capability, version, len(matched), len(partial),
                  len(excluded),
                  "Nothing was run." if matched else
                  "Nothing matched, and the brief says what was missing."),
        "unjudged": [
            "NOTHING WAS RUN AND NOTHING WAS LOOKED UP. The candidates were "
            "handed in - HERON-RAG-RET-003 retrieves them - and what comes "
            "back is a decision about them, not an execution of one.",
            "A NEAR MATCH IS IN `partial` AND NOWHERE ELSE. A fragment "
            "short of one need will run, half-work and look exactly like a "
            "success, so no caller can reach it by reading `matched`.",
            "THE VERSION WAS A WALL, APPLIED BEFORE THE CAPABILITY. %d "
            "candidate(s) were excluded for declaring something other than "
            "Revit %s, and none of them was ranked lower instead - docs/05 "
            "s8." % (wrong_version, version),
            "STATUS AND SOURCE ARE REPORTED, NOT PREFERRED. A DRAFT match "
            "is a match and is not quietly put below a PROVEN one: that is "
            "a judgement about risk, and the caller is the one carrying it.",
        ],
    }


def main(argv):
    print("FRAGMENT MATCHING   a near match is not a match")
    print("=" * 72)

    library = [
        {"id": "FRG-VIEW-039", "capability": "APPLY_VIEW_FILTER",
         "heron-status": "DRAFT", "source": "OFFICIAL", "risk": "MODIFY",
         "revit": ["2020", "2024", "2025"],
         "contract": {"needs": [{"name": "doc", "type": "Document"},
                                {"name": "view", "source": "request"},
                                {"name": "filter", "source": "request"},
                                {"name": "visible", "source": "request"}],
                      "provides": [{"name": "applied", "role": "result"}]}},
        {"id": "FRG-VIEW-012", "capability": "APPLY_VIEW_FILTER",
         "heron-status": "PROVEN", "source": "OFFICIAL", "risk": "MODIFY",
         "revit": ["2021"],
         "contract": {"needs": [{"name": "view", "source": "request"}],
                      "provides": [{"name": "applied"}]}},
        {"id": "FRG-VIEW-044", "capability": "APPLY_VIEW_FILTER",
         "heron-status": "PROVEN", "source": "OFFICIAL", "risk": "MODIFY",
         "revit": ["2024"],
         "contract": {"needs": [{"name": "view", "source": "request"},
                                {"name": "filter", "source": "request"},
                                {"name": "overrides", "source": "request"}],
                      "provides": [{"name": "applied"}]}},
        {"id": "FRG-SEL-001", "capability": "SELECT_BY_CATEGORY",
         "heron-status": "PROVEN", "source": "OFFICIAL", "risk": "READ",
         "revit": ["2024"], "contract": {"needs": [], "provides": []}},
    ]

    answer = match({"capability": "APPLY_VIEW_FILTER",
                    "has": ["view", "filter", "visible"],
                    "wants": ["applied"]},
                   library, revit="2024")

    print("\n%s" % answer["why"])
    for row in answer["matched"]:
        print("  MATCH    %-14s %-8s %s"
              % (row["id"], row["status"], row["why"][:44]))
    for row in answer["partial"]:
        print("  partial  %-14s %-8s %s"
              % (row["id"], row["status"], row["why"][:44]))
    for row in answer["excluded"]:
        print("  excluded %-14s %-8s %s"
              % (row["id"], row["refused"][:8], row["why"][:44]))

    nothing = match({"capability": "SPLIT_DUCT_BY_LENGTH",
                     "has": ["view"], "wants": ["split"]},
                    library, revit="2024")
    print("\nand when nothing does it, the brief rather than an empty list")
    for key in ("capability", "revit", "nearest", "missing",
                "excluded_for_version"):
        print("  %-22s %s" % (key, nothing["brief"][key]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
