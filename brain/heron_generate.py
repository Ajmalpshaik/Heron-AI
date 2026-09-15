# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-CRE-007
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Authoring a fragment - only after the Matcher has reported, and only
when it found nothing to start from.

    python brain/heron_generate.py

WHAT IT IS FOR (docs/28, HERON-FRG-CRE-007)
--------------------------------------------
"Authors a new fragment - identity, metadata, implementation, tests.
ONLY AFTER FRAGMENT MATCHER REPORTS NOTHING REUSABLE." T3, risk MODIFY.

docs/09 s151 MAKES THAT MECHANICAL AND ASKS FOR A PERCENTAGE
--------------------------------------------------------------
    "Enforce this mechanically rather than by instruction. Before the
    Code Generation Agent runs, the Fragment Matcher must have searched
    and reported. If a proven fragment covers >=80% OF THE REQUEST,
    generation is not permitted to start from scratch - it must start
    from that fragment. Otherwise the knowledge base fills with
    near-duplicates, and the Merge Agent spends its life cleaning up
    after the Generation Agent."

The gate is right and the percentage has no producer. HERON-RAG-FMT-004
is the Fragment Matcher, and it returns `matched`, `partial` and
`excluded` - no score, deliberately. Its whole rule is "a near match is
not a match": a fragment short of one need will run, half-work and look
exactly like a success, so it is put in `partial` where no caller can
reach it by reading `matched`. Scoring it 0.9 would be the thing that
agent exists to refuse.

So the gate is enforced in the Matcher's OWN vocabulary, which is
stricter than the percentage rather than looser:

    `matched` non-empty   something already does this. Golden Rule 3 -
                          reuse proven knowledge before creating new.
                          Refused.
    `partial` non-empty   something covers part of it. That IS docs/09's
                          ">=80%" case, decided structurally rather than
                          by a number nobody computes. Refused, and the
                          answer names what to start from.

The discrepancy is recorded as PROPOSALS F24, because restating docs/09
s151 or making the Matcher score are both the owner's call.

THE REST IS HERON-SKL-CRE-002's SHAPE, ONE LAYER DOWN
-------------------------------------------------------
Required fields derived from heron_fragment.REQUIRED rather than listed.
Enters at DRAFT and the author gets no say. Never overwrites. And the
capability must be free - 360 fragments hold 360 distinct capabilities
today, so a second claim on one is a collision rather than a variant.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/09's ladder, read from HERON-FRG-VAL-001 rather than typed.
ENTERS_AT = FRAG.STATUSES[FRAG.STATUSES.index("DRAFT")]

# What the agent writes and what the author supplies, split on the
# metadata prefix. Derived, so a field added to heron_fragment.REQUIRED
# becomes required here with no edit.
WRITES = tuple(field for field in FRAG.REQUIRED if field.startswith("heron-"))
AUTHORS = tuple(field for field in FRAG.REQUIRED
                if not field.startswith("heron-"))

# HERON-RAG-FMT-004's two lists, and what each means for this gate.
STOPS = (
    ("matched", "something already does this - Golden Rule 3, reuse "
                "proven knowledge before creating new"),
    ("partial", "something covers part of it, which is docs/09 s151's "
                "'>=80% of the request' decided structurally. Start from "
                "it rather than from scratch"),
)


def author(draft, report, cases=None, into=None):
    """
    {authored, card, why} - or a refusal. Nothing is written to disk.
    """
    if not draft:
        return {"authored": False, "refused": "NOTHING_TO_AUTHOR",
                "why": "no draft was handed in."}

    draft = getattr(draft, "data", draft)
    if not isinstance(draft, dict):
        return {"authored": False, "refused": "NOTHING_TO_AUTHOR",
                "why": "%r is not a draft. One is a fragment card without "
                       "its metadata: %s." % (draft, ", ".join(AUTHORS))}

    # THE GATE COMES FIRST. docs/09 s151: before the Code Generation
    # Agent RUNS, the Matcher must have searched and reported.
    if not report:
        return {"authored": False, "refused": "NO_MATCHER_REPORT",
                "asked": "What did HERON-RAG-FMT-004 find for this "
                         "capability?",
                "why": "docs/09 s151 requires the Fragment Matcher to have "
                       "searched and reported BEFORE generation runs, and "
                       "asks for it enforced mechanically rather than by "
                       "instruction. No report came with this draft."}

    report = getattr(report, "data", report)
    # A REFUSAL IS TOLD BY `refused`, NOT BY `ran`. The Matcher sets
    # `ran: False` on its SUCCESS path too - it means nothing was
    # executed, which is that agent's whole point. Reading it as failure
    # would reject every real report, which is what the suite caught.
    if not isinstance(report, dict) or report.get("refused") \
            or not any(field in report for field, _ in STOPS):
        return {"authored": False, "refused": "NO_MATCHER_REPORT",
                "why": "%r is not a Fragment Matcher answer, or is one "
                       "that refused. A report that could not run is not "
                       "a report that found nothing." % (report,)}

    for field, because in STOPS:
        found = report.get(field) or []
        if found:
            names = [str(getattr(one, "id", None)
                         or (one.get("fragment") if isinstance(one, dict)
                             else one) or one) for one in found]
            return {"authored": False,
                    "refused": "REUSE_EXISTS" if field == "matched"
                               else "START_FROM_IT",
                    "found": names, "where": field,
                    "why": "the Matcher reports %d in `%s`: %s. %s."
                           % (len(names), field, ", ".join(names), because)}

    said = draft.get("heron-status")
    if said and str(said).strip().upper() != ENTERS_AT:
        return {"authored": False, "refused": "NOT_A_DRAFT",
                "why": "the draft arrives at %s. A new fragment enters "
                       "docs/09's ladder at %s and nowhere else, and one "
                       "arriving higher is refused rather than quietly "
                       "lowered - D-35." % (said, ENTERS_AT)}

    missing = [field for field in AUTHORS
               if draft.get(field) in (None, "", [], {})]
    if missing:
        return {"authored": False, "refused": "INCOMPLETE",
                "missing": missing,
                "why": "the draft is missing %s. Read from "
                       "heron_fragment.REQUIRED rather than listed here, "
                       "so a field added there becomes required with no "
                       "edit." % ", ".join(missing)}

    # THE ROW ASKS FOR TESTS, AND A FRAGMENT WITH NONE IS NOT AUTHORED.
    negative = []
    if isinstance(cases, dict):
        negative = list(cases.get("negative") or [])
    if not cases or not isinstance(cases, dict) or not negative:
        return {"authored": False, "refused": "NO_CASES",
                "asked": "What must this fragment come back EMPTY for?",
                "why": "docs/28 asks this agent for identity, metadata, "
                       "implementation AND tests, and a fragment with no "
                       "NEGATIVE case cannot be disproved. A positive case "
                       "alone shows it does something; only a negative one "
                       "shows it is not doing it to everything."}

    who = str(draft["id"]).strip()
    capability = str(draft["capability"]).strip()
    library = draft.get("library") or []

    for entry in library:
        card = getattr(entry, "data", entry)
        if not isinstance(card, dict):
            continue
        if str(card.get("id") or "").strip() == who:
            return {"authored": False, "refused": "ALREADY_EXISTS",
                    "why": "'%s' is already in the library. Nothing is "
                           "overwritten here - GR 14 - and "
                           "HERON-FRG-UPD-008 is the agent allowed to "
                           "change an existing fragment." % who}
        if str(card.get("capability") or "").strip() == capability:
            return {"authored": False, "refused": "CAPABILITY_TAKEN",
                    "by": str(card.get("id") or "").strip(),
                    "why": "'%s' already provides %s. 360 fragments hold "
                           "360 distinct capabilities today, so a second "
                           "claim on one is a collision rather than a "
                           "variant - and the Matcher finds a provider by "
                           "capability, so two would make its answer "
                           "arbitrary."
                           % (str(card.get("id") or "").strip(), capability)}

    card = dict((field, draft[field]) for field in AUTHORS)
    card["heron-agent"] = "HERON-FRG-CRE-007"
    card["heron-status"] = ENTERS_AT
    card["heron-step"] = draft.get("heron-step") or 0
    card["heron-since"] = str(draft.get("heron-since") or "0.1.0")
    card["heron-layer"] = "brain"

    where = into or os.path.join(ROOT, "brain", "fragments")
    folder = os.path.join(where, FRAG.folder_for(capability))

    return {
        "authored": True, "fragment": who, "capability": capability,
        "status": ENTERS_AT, "card": card, "folder": folder,
        "cases": {"positive": len(cases.get("positive") or []),
                  "negative": len(negative)},
        "why": "'%s' authored at %s providing %s, with %d negative "
               "case(s). Nothing was written to disk."
               % (who, ENTERS_AT, capability, len(negative)),
        "unjudged": [
            "THE MATCHER FOUND NOTHING TO START FROM, WHICH IS WHY THIS "
            "RAN AT ALL. docs/09 s151 asks for that gate enforced "
            "mechanically, and it is - in the Matcher's own vocabulary, "
            "because the '>=80%' it asks for has no producer: "
            "HERON-RAG-FMT-004 refuses to score a near match on purpose. "
            "PROPOSALS F24.",
            "WHETHER THE IMPLEMENTATION WORKS. Nothing here compiled or "
            "ran anything. It enters at %s and D-30 wants one recorded "
            "proof against a real model before it moves." % ENTERS_AT,
            "WHETHER THE CASES ARE GOOD ONES. %d negative case(s) were "
            "required and counted, not read - a case that cannot be "
            "arranged is HERON-FRG-VAL-001's finding, not this agent's."
            % len(negative),
            "NOTHING WAS WRITTEN TO DISK. The card and the folder it "
            "belongs in come back; the folder name is "
            "heron_fragment.folder_for's, so the capability decides where "
            "it lives and nobody types a path.",
        ],
    }


def main(argv):
    print("FRAGMENT AUTHORING   only after the Matcher has reported")
    print("=" * 72)
    print("\nthe author supplies %d field(s), this agent writes %d"
          % (len(AUTHORS), len(WRITES)))

    empty = {"ran": True, "matched": [], "partial": [], "excluded": []}
    draft = {"id": "FRG-T-001", "semantic-identity": "count the ducts",
             "kind": "query", "domain": "revit.reporting",
             "capability": "COUNT_DUCTS", "version": "1.0.0",
             "source": "OFFICIAL", "risk": "READ",
             "purpose": "Counts ducts and says which filter it used.",
             "contract": {"needs": [], "provides": []},
             "revit": ["2024"], "runtime": "net8.0-windows",
             "utterances": ["how many ducts"]}
    cases = {"positive": [{"given": "a model with ducts"}],
             "negative": [{"given": "a model with none", "expect": "empty"}]}

    answer = author(draft, empty, cases)
    print("\n%s" % answer["why"])
    print("  folder: %s" % os.path.relpath(answer["folder"], ROOT))

    print("\nrefused")
    taken = [{"id": "FRG-OLD-001", "capability": "COUNT_DUCTS"}]
    for card, report, these in (
            (None, empty, cases),
            (draft, None, cases),
            (draft, {"ran": False}, cases),
            (draft, dict(empty, matched=[{"fragment": "FRG-X-001"}]), cases),
            (draft, dict(empty, partial=[{"fragment": "FRG-Y-002"}]), cases),
            (dict(draft, **{"heron-status": "PROVEN"}), empty, cases),
            ({"id": "x"}, empty, cases),
            (draft, empty, None),
            (draft, empty, {"positive": [{"given": "x"}]}),
            (dict(draft, library=taken), empty, cases),
            (dict(draft, id="FRG-OLD-001", library=taken), empty, cases)):
        bad = author(card, report, these)
        print("  %-22s %s" % (bad["refused"], bad["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
