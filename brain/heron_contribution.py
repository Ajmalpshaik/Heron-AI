# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-COM-010
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Contributions - per item, and the review has to have seen the payload.

    python brain/heron_contribution.py

WHAT IT IS FOR (docs/28, HERON-GIT-COM-010)
--------------------------------------------
"Prepares a submission. PER-ITEM HUMAN REVIEW OF THE ACTUAL PAYLOAD."
T2, risk PUBLISH. The bold half is the agent.

IT IS A STRONGER RULE THAN HERON-GIT-PR-005's, IN TWO WAYS
------------------------------------------------------------
A pull request takes one confirmation naming the head. A submission
takes one review PER ITEM, and each review has to have seen the thing
itself.

"THE ACTUAL PAYLOAD" is the half that is easy to lose. A person
reviewing "3 fragments, MEP, low risk" has reviewed a sentence. So each
review carries the SHA of the payload it saw, and the sha is recomputed
here from the item as it stands now. If they do not match, the review is
STALE - somebody reviewed something, and it was not this.

That is the stale-hash failure HERON-RPT-VAL-004 was built to catch,
turned round and used on purpose: there it was a bug that a document
could carry its old sha and pass; here, the whole point is that it
cannot.

A REVIEW MAY NOT COVER TWO ITEMS
----------------------------------
Each review names the item it reviewed. One that does not, or one whose
sha matches a different item, is not a per-item review - it is a
signature that travels, which is exactly what "per-item" rules out.

A MACHINE MAY NOT REVIEW
--------------------------
Golden Rule 7, and the machine-word list is HERON-LRN-PRO-004's.

IT IS AN EGRESS, AND THE WORST ONE
------------------------------------
A submission goes to a repository this project does not control. So the
two rules from HERON-RPT-RED-003 apply per item, and they run before any
question of who reviewed: a Revit file by name alone, and a credential
refused whole with the value never in this answer.
"""

from __future__ import annotations

import hashlib
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_promotion as PRO  # noqa: E402
import heron_release as RELEASE  # noqa: E402
import heron_secrets as SECRETS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

NEVER_LEAVES = RELEASE.NEVER_LEAVES
NOT_A_PERSON = PRO.NOT_A_PERSON

AN_ITEM_CARRIES = (
    ("name", "what the reviewer will call it"),
    ("payload", "the thing itself, in full. A summary is what a review "
                "of a summary reviews"),
)

A_REVIEW_CARRIES = (
    ("by", "a person's name - Golden Rule 7"),
    ("at", "when"),
    ("item", "WHICH item. A review that does not name one is a "
             "signature that travels"),
    ("saw", "the sha of the payload they read. Recomputed here from the "
            "item as it stands, so a review of an earlier draft is "
            "STALE rather than accepted"),
)


def sha_of(payload):
    """The sha of a payload, the same way HERON-RPT-RND-002 does it."""
    return hashlib.sha256(str(payload).encode("utf-8")).hexdigest()


def submit(items, reviews=(), to=None, secrets=None):
    """
    {prepared, items, why} - a verdict. Nothing is submitted anywhere.
    """
    if not items:
        return {"prepared": False, "refused": "NOTHING_TO_SUBMIT",
                "why": "no items were handed in. An empty submission is "
                       "not a contribution with nothing controversial in "
                       "it, it is nothing."}
    if not str(to or "").strip():
        return {"prepared": False, "refused": "NOT_A_SUBMISSION",
                "why": "no destination was named. A submission goes to a "
                       "repository this project does not control, and "
                       "which one is the first thing a reviewer needs to "
                       "know."}

    seen = {}
    for item in items:
        item = getattr(item, "data", item)
        if not isinstance(item, dict):
            return {"prepared": False, "refused": "NOT_AN_ITEM",
                    "why": "%r is not an item. Each carries %s."
                           % (item, ", ".join(field for field, _
                                              in AN_ITEM_CARRIES))}
        absent = [field for field, _ in AN_ITEM_CARRIES
                  if not str(item.get(field) or "").strip()]
        if absent:
            return {"prepared": False, "refused": "NOT_AN_ITEM",
                    "missing": absent,
                    "why": "an item is missing %s. %s"
                           % (", ".join(absent),
                              " ".join(why for field, why in AN_ITEM_CARRIES
                                       if field in absent))}
        name = str(item["name"]).strip()
        if name in seen:
            return {"prepared": False, "refused": "NOT_AN_ITEM",
                    "why": "'%s' appears twice. Two items under one name "
                           "means a review naming it reviews neither."
                           % name}
        seen[name] = item

    # THE EGRESS CHECKS FIRST, per item. A reviewed leak is still a leak.
    keeper = secrets if secrets is not None else SECRETS.Secrets()
    for name in sorted(seen):
        item = seen[name]
        binaries = RELEASE._binaries(
            [name] + list(item.get("attachments") or []))
        if binaries:
            return {"prepared": False, "refused": "CARRIES_A_MODEL",
                    "item": name,
                    "attachments": [one for one, _ in binaries],
                    "why": "'%s' carries %s. D-26 draws the line at the "
                           "FILE, and this one goes to a repository this "
                           "project does not control."
                           % (name, ", ".join("'%s'" % one
                                              for one, _ in binaries))}
        _, found = keeper.redact(str(item["payload"]))
        if found:
            return {"prepared": False, "refused": "CARRIES_A_SECRET",
                    "item": name, "found": sorted(set(found)),
                    "why": "'%s' carries %s in its payload. Refused WHOLE "
                           "rather than stripped, and the value is not in "
                           "this answer - only that it was there."
                           % (name, ", ".join(sorted(set(found))))}

    by_item = {}
    for review in (reviews or []):
        review = getattr(review, "data", review)
        if not isinstance(review, dict):
            continue
        which = str(review.get("item") or "").strip()
        if which:
            by_item.setdefault(which, []).append(review)

    prepared = []
    for name in sorted(seen):
        item = seen[name]
        mine = by_item.get(name) or []
        if not mine:
            return {"prepared": False, "refused": "NOT_REVIEWED",
                    "item": name,
                    "asked": "Read '%s' in full and say whether it may go "
                             "to %s." % (name, to),
                    "why": "'%s' has no review. docs/28 asks for PER-ITEM "
                           "human review of the actual payload, so one "
                           "review of the submission is not a review of "
                           "this." % name}
        review = mine[0]
        absent = [field for field, _ in A_REVIEW_CARRIES
                  if not str(review.get(field) or "").strip()]
        if absent:
            return {"prepared": False, "refused": "NOT_REVIEWED",
                    "item": name, "missing": absent,
                    "why": "the review of '%s' is missing %s. %s"
                           % (name, ", ".join(absent),
                              " ".join(why for field, why in A_REVIEW_CARRIES
                                       if field in absent))}
        if not PRO._person(review.get("by")):
            return {"prepared": False, "refused": "REVIEWED_BY_A_MACHINE",
                    "item": name,
                    "why": "'%s' reviewed '%s', which is not a person. "
                           "Golden Rule 7, and docs/28 asks for HUMAN "
                           "review in as many words."
                           % (review.get("by"), name)}

        # THE PAYLOAD, NOT A SUMMARY OF IT.
        now = sha_of(item["payload"])
        said = str(review["saw"]).strip().lower()
        if said != now:
            return {"prepared": False, "refused": "REVIEW_IS_STALE",
                    "item": name, "reviewed": said, "is_now": now,
                    "why": "the review of '%s' saw %s and the payload is "
                           "now %s. Somebody reviewed something, and it "
                           "was not this - a review of an earlier draft is "
                           "stale rather than close enough."
                           % (name, said[:12], now[:12])}
        prepared.append({"item": name, "sha": now,
                         "by": str(review["by"]).strip(),
                         "at": str(review["at"]).strip(),
                         "bytes": len(str(item["payload"]))})

    return {
        "prepared": True, "may_submit": True, "to": str(to).strip(),
        "items": prepared, "of": len(prepared),
        "why": "%d item(s) to %s, each reviewed by a person who read the "
               "payload this agent just re-hashed. Prepared, not "
               "submitted." % (len(prepared), to),
        "unjudged": [
            "NOTHING WAS SUBMITTED. A verdict comes back. D-01 leaves the "
            "network with the host, and HERON-GIT-MAIN-001 owns "
            "repository interaction.",
            "EACH REVIEW WAS CHECKED AGAINST THE PAYLOAD'S SHA, RECOMPUTED "
            "HERE - not against the sha the review carried, which would "
            "let a review of an earlier draft pass. That is the failure "
            "HERON-RPT-VAL-004 was built to catch, used on purpose.",
            "WHETHER THE REVIEWERS AGREED WITH WHAT THEY READ. A name and "
            "a sha say somebody saw this exact thing, which is what makes "
            "them answerable for it - not that they thought it was good.",
            "WHAT %s DOES WITH IT. This project does not control that "
            "repository, which is why the payload is checked here rather "
            "than trusted to be checked there." % to,
        ],
    }


def main(argv):
    print("CONTRIBUTIONS   per item, and the review has to have seen it")
    print("=" * 72)

    ducts = {"name": "count-ducts", "payload": "capability: COUNT_DUCTS\n"}
    pipes = {"name": "count-pipes", "payload": "capability: COUNT_PIPES\n"}

    def read(item, by="Ajmal"):
        return {"by": by, "at": "2026-09-15 11:20", "item": item["name"],
                "saw": sha_of(item["payload"])}

    answer = submit([ducts, pipes], [read(ducts), read(pipes)],
                    to="heron-community/fragments")
    print("\n%s" % answer["why"])
    for one in answer["items"]:
        print("  %-14s %s  %d bytes  %s"
              % (one["item"], one["sha"][:12], one["bytes"], one["by"]))

    print("\nrefused")
    stale = dict(ducts, payload="capability: COUNT_DUCTS\n# and one more\n")
    for items, reviews, to in (
            ([], [read(ducts)], "somewhere"),
            ([ducts], [read(ducts)], ""),
            (["a string"], [], "somewhere"),
            ([ducts, ducts], [], "somewhere"),
            ([dict(ducts, attachments=["Tower A.rvt"])], [], "somewhere"),
            ([dict(ducts, payload="token ghp_" + "A" * 36)], [],
             "somewhere"),
            ([ducts, pipes], [read(ducts)], "somewhere"),
            ([ducts], [read(ducts, by="ci")], "somewhere"),
            ([stale], [read(ducts)], "somewhere")):
        bad = submit(items, reviews, to=to)
        print("  %-24s %s" % (bad["refused"], bad["why"][:38]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
