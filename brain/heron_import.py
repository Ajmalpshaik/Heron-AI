# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-APR-014
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Import approval - everything enters at DISCOVERED, and a proof taken
elsewhere is not a proof.

    python brain/heron_import.py

WHAT IT IS FOR (docs/28, HERON-IMP-APR-014)
--------------------------------------------
"Presents the manifest for human review. EVERYTHING ENTERS AT
`DISCOVERED`." T1, risk SUGGEST. It accepts nothing; it prepares a
manifest somebody can say yes to.

DISCOVERED IS ONE RUNG BELOW WHAT HERON AUTHORS ITSELF
--------------------------------------------------------
HERON-FRG-CRE-007 and HERON-SKL-CRE-002 both enter at DRAFT, because
what they write has identity, metadata and a stated purpose - which is
exactly docs/09 s98's gate for DISCOVERED -> DRAFT.

An import has not earned that. It arrives as somebody else's file, and
whether it even has a stated purpose is one of the things review is for.
So it enters lower, and that difference is the whole point of the row.

A PROOF FROM ANOTHER LIBRARY IS REFUSED, AND THIS ONE IS AJMAL'S RULE
-----------------------------------------------------------------------
    "even in the AJ AI proven fragment don't mark in Heron this is
    proven, because we will check each and every one again in Heron AI."
    - 2026-08-29, on re-authoring from his earlier library

heron_fragment enforces the same thing at the other end, by fingerprint:
a proof taken against different bytes reads as stale. This agent
enforces it at the door, by refusing the proof outright - because a
manifest arriving with one is somebody asking to skip the checking, and
the answer is no before any fingerprint is computed.

TWO THINGS NEVER ENTER THE LIBRARY AT ALL
-------------------------------------------
A Revit model. It is not a fragment, not a skill and not knowledge - it
is somebody's building, and the library is not where it lives. The
extension list is HERON-RPT-RED-003's, which draws the same line in the
other direction.

A credential. Imported code is the likeliest place in this whole project
for one to be sitting in a file, and the manifest is read for it before
anybody is asked to approve anything. Refused whole, never stripped, and
the value is not in this answer.

IT PRESENTS; IT DOES NOT ACCEPT
---------------------------------
Risk SUGGEST. What comes back is a manifest ready for review and a
verdict on whether it may be shown - HERON-IMP-MAIN-001 owns the
pipeline, and docs/28 gives that row MODIFY and "never modifies the
source folder".
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402
import heron_release as RELEASE  # noqa: E402
import heron_secrets as SECRETS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/09's ladder, read from HERON-FRG-VAL-001. An import enters at the
# bottom rung - one below what Heron authors itself.
ENTERS_AT = FRAG.STATUSES[0]
AUTHORED_AT = FRAG.STATUSES[1]

# HERON-RPT-RED-003's list, which draws the same line outward.
NEVER_LEAVES = RELEASE.NEVER_LEAVES

AN_ITEM_CARRIES = (
    ("name", "what a reviewer will call it"),
    ("kind", "what it is proposed as - a fragment, a skill, a document"),
    ("from", "where it came from, so a reviewer can go and look"),
)


def present(manifest, secrets=None):
    """
    {presented, items, why} - or a refusal. Nothing is accepted and
    nothing is written.
    """
    if not manifest:
        return {"presented": False, "refused": "NOTHING_TO_PRESENT",
                "why": "no manifest was handed in. An empty import is not "
                       "an import with nothing controversial in it."}

    manifest = getattr(manifest, "data", manifest)
    items = manifest.get("items") if isinstance(manifest, dict) else None
    if not isinstance(manifest, dict) or not isinstance(items, list) \
            or not items:
        return {"presented": False, "refused": "NOT_A_MANIFEST",
                "why": "%r is not a manifest. One carries `items`, each "
                       "%s." % (manifest,
                                ", ".join(field for field, _
                                          in AN_ITEM_CARRIES))}

    keeper = secrets if secrets is not None else SECRETS.Secrets()
    seen, ready = [], []
    for item in items:
        item = getattr(item, "data", item)
        if not isinstance(item, dict):
            return {"presented": False, "refused": "NOT_AN_ITEM",
                    "why": "%r is not an item. Each carries %s."
                           % (item, ", ".join(field for field, _
                                              in AN_ITEM_CARRIES))}
        absent = [field for field, _ in AN_ITEM_CARRIES
                  if not str(item.get(field) or "").strip()]
        if absent:
            return {"presented": False, "refused": "NOT_AN_ITEM",
                    "missing": absent,
                    "why": "an item is missing %s. %s"
                           % (", ".join(absent),
                              " ".join(why for field, why
                                       in AN_ITEM_CARRIES
                                       if field in absent))}
        name = str(item["name"]).strip()
        if name in seen:
            return {"presented": False, "refused": "NOT_AN_ITEM",
                    "why": "'%s' appears twice. A reviewer approving it "
                           "would be approving one of two things and not "
                           "know which." % name}
        seen.append(name)

        # A MODEL IS NOT KNOWLEDGE.
        binaries = RELEASE._binaries([name] + list(item.get("files") or []))
        if binaries:
            return {"presented": False, "refused": "CARRIES_A_MODEL",
                    "item": name,
                    "files": [one for one, _ in binaries],
                    "why": "'%s' carries %s. A Revit model is not a "
                           "fragment, not a skill and not knowledge - it "
                           "is somebody's building, and the library is not "
                           "where it lives."
                           % (name, ", ".join("'%s'" % one
                                              for one, _ in binaries))}

        _, found = keeper.redact(str(item.get("content") or ""))
        if found:
            return {"presented": False, "refused": "CARRIES_A_SECRET",
                    "item": name, "found": sorted(set(found)),
                    "why": "'%s' carries %s. Imported code is the likeliest "
                           "place in this project for a credential to be "
                           "sitting in a file, so the manifest is read "
                           "before anybody is asked to approve it. Refused "
                           "whole, never stripped, and the value is not in "
                           "this answer."
                           % (name, ", ".join(sorted(set(found))))}

        # A PROOF FROM ANOTHER LIBRARY IS SOMEBODY ASKING TO SKIP THE
        # CHECKING, AND THE ANSWER IS NO.
        if item.get("proof"):
            return {"presented": False, "refused": "CARRIES_A_PROOF",
                    "item": name,
                    "why": "'%s' arrives carrying a proof. Ajmal's "
                           "instruction of 2026-08-29: 'even in the AJ AI "
                           "proven fragment don't mark in Heron this is "
                           "proven, because we will check each and every "
                           "one again in Heron AI.' A proof taken "
                           "elsewhere is evidence about THAT code, and a "
                           "manifest arriving with one is a request to "
                           "skip the checking." % name}

        said = str(item.get("status") or "").strip().upper()
        if said and said != ENTERS_AT:
            return {"presented": False, "refused": "ENTERS_TOO_HIGH",
                    "item": name, "said": said,
                    "why": "'%s' arrives at %s. docs/28 says EVERYTHING "
                           "enters at %s - one rung below %s, where Heron's "
                           "own authors enter, because an import has not "
                           "shown it even has a stated purpose. Refused "
                           "rather than quietly lowered: a manifest that "
                           "asked for more than it may have is a thing a "
                           "reviewer should see."
                           % (name, said, ENTERS_AT, AUTHORED_AT)}

        ready.append({"name": name, "kind": str(item["kind"]).strip(),
                      "from": str(item["from"]).strip(),
                      "status": ENTERS_AT,
                      "files": list(item.get("files") or [])})

    kinds = {}
    for one in ready:
        kinds[one["kind"]] = kinds.get(one["kind"], 0) + 1

    return {
        "presented": True, "accepted": False,
        "items": ready, "of": len(ready), "kinds": kinds,
        "status": ENTERS_AT,
        "source": str(manifest.get("from") or "").strip() or None,
        "why": "%d item(s) ready for review, every one at %s: %s. Nothing "
               "was accepted."
               % (len(ready), ENTERS_AT,
                  ", ".join("%d %s" % (count, kind)
                            for kind, count in sorted(kinds.items()))),
        "unjudged": [
            "NOTHING WAS ACCEPTED. This agent's risk is SUGGEST: it "
            "prepares a manifest somebody can say yes to, and "
            "HERON-IMP-MAIN-001 owns the pipeline - with 'never modifies "
            "the source folder' in its own row.",
            "EVERY ITEM ENTERS AT %s, WHICH IS ONE RUNG BELOW %s. "
            "HERON-FRG-CRE-007 and HERON-SKL-CRE-002 enter what THEY "
            "write at %s, because it has identity, metadata and a stated "
            "purpose. An import has not shown that, and whether it has is "
            "one of the things review is for."
            % (ENTERS_AT, AUTHORED_AT, AUTHORED_AT),
            "WHETHER ANY OF IT IS ANY GOOD. Nothing here read an "
            "implementation, ran anything or judged a single item - it "
            "checked what may not enter at all and set the rung "
            "everything enters on.",
            "WHAT IS INSIDE THE FILES. Their NAMES were checked against "
            "the Revit extensions and any `content` handed in was read "
            "for a credential. Nothing here opened a file on disk.",
        ],
    }


def main(argv):
    print("IMPORT APPROVAL   everything enters at %s" % ENTERS_AT)
    print("=" * 72)
    print("\ndocs/09's ladder starts at %s; Heron's own authors enter at %s"
          % (ENTERS_AT, AUTHORED_AT))

    manifest = {"from": "C:/AJ-Tools", "items": [
        {"name": "count-ducts", "kind": "fragment",
         "from": "AJ-Tools/Scripts/CountDucts.py"},
        {"name": "tag-sheet", "kind": "skill",
         "from": "AJ-Tools/Scripts/TagSheet.py"},
        {"name": "mep-notes", "kind": "document",
         "from": "AJ-Tools/README.md"}]}

    answer = present(manifest)
    print("\n%s" % answer["why"])
    for one in answer["items"]:
        print("  %-14s %-10s %-8s %s"
              % (one["name"], one["kind"], one["status"], one["from"]))

    print("\nrefused")
    one = manifest["items"][0]
    for these in (
            None, {"from": "x"}, {"items": ["a string"]},
            {"items": [{"name": "x"}]},
            {"items": [one, dict(one)]},
            {"items": [dict(one, files=["Tower A.rvt"])]},
            {"items": [dict(one, content="token ghp_" + "A" * 36)]},
            {"items": [dict(one, proof={"model": "Tower A, Revit 2024"})]},
            {"items": [dict(one, status="PROVEN")]}):
        bad = present(these)
        print("  %-22s %s" % (bad["refused"], bad["why"][:44]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
