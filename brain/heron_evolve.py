# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-EVO-005
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Evolution - eight verdicts, and this agent owns none of them.

    python brain/heron_evolve.py

WHAT IT IS FOR (docs/28, HERON-FRG-EVO-005)
--------------------------------------------
"Decides KEEP / UPDATE / EXTEND / SPLIT / MERGE / BRANCH / DEPRECATE /
ARCHIVE. ALWAYS PROPOSES." T3, risk SUGGEST.

IT ROUTES; IT DOES NOT RE-DECIDE
----------------------------------
Every one of those eight is already somebody's. SPLIT is
HERON-FRG-SPL-003's, on a count of consumers. MERGE is
HERON-FRG-MRG-004's, on a contract signature. BRANCH is
HERON-GIT-BRN-003's, on Golden Rule 4's last resort. DEPRECATED and
ARCHIVED are docs/09 s98's lifecycle transitions.

So this file decides nothing and holds no rule of its own. What it does
is the thing none of them can: say which of the eight are even
AVAILABLE for this fragment, who owns each, and in what order Golden
Rule 4 wants the ones that remain.

FOUR OF THE EIGHT ARE ON GOLDEN RULE 4's LADDER, AND FOUR ARE NOT
-------------------------------------------------------------------
Golden Rule 4: "Keep, extend, adapt, or version-branch - IN THAT ORDER
OF PREFERENCE." Four verdicts, ordered. This row names eight, and four
of them are that ladder under different words:

    keep      -> KEEP
    extend    -> EXTEND
    adapt     -> UPDATE
    version-branch -> BRANCH

The mapping of "adapt" onto UPDATE is the one place this file reads
between two documents, and it is said out loud rather than assumed
silently - the alternative is eight verdicts with no order at all, which
is how the last resort gets picked first.

SPLIT, MERGE, DEPRECATE and ARCHIVE are not on that ladder. They are not
degrees of changing a fragment; they are decisions about whether it
should stay one fragment, stay at all, or stay reachable. So they are
reported beside the ladder rather than inside it.

A VERDICT NOTHING SUPPORTS IS NOT OFFERED
-------------------------------------------
SPLIT is available only if HERON-FRG-SPL-003 proposed one. MERGE only if
HERON-FRG-MRG-004 found a group. ARCHIVE only from DEPRECATED, because
docs/09 s98 says so. Offering a verdict nobody can act on is how a
proposal becomes a menu.

WHY IT IS BEING CONSIDERED IS ASKED FOR
-----------------------------------------
Evolution runs because something happened. Without that, every answer
here would be "KEEP, probably" - D-33, asked once rather than assumed.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/28's eight, in the register's own order.
VERDICTS = ("KEEP", "UPDATE", "EXTEND", "SPLIT", "MERGE", "BRANCH",
            "DEPRECATE", "ARCHIVE")

# Golden Rule 4's four, in ITS order of preference, mapped onto the
# register's words. The mapping of "adapt" onto UPDATE is read between
# two documents and said out loud.
LADDER = (("KEEP", "keep"), ("EXTEND", "extend"), ("UPDATE", "adapt"),
          ("BRANCH", "version-branch"))
ON_THE_LADDER = tuple(one for one, _ in LADDER)
BESIDE_IT = tuple(one for one in VERDICTS if one not in ON_THE_LADDER)

# Who decides each, and on what. Nothing here is decided.
OWNS = {
    "KEEP": ("this fragment's owner", "nothing needs doing"),
    "UPDATE": ("HERON-FRG-UPD-008", "applies what this agent proposes"),
    "EXTEND": ("HERON-FRG-UPD-008", "applies what this agent proposes"),
    "SPLIT": ("HERON-FRG-SPL-003",
              "at least two distinct consumers per part, docs/09 s120"),
    "MERGE": ("HERON-FRG-MRG-004",
              "a shared contract signature AND a shared capability"),
    "BRANCH": ("HERON-GIT-BRN-003",
               "Golden Rule 4's last resort, and the cherry-pick cost"),
    "DEPRECATE": ("docs/09 s98", "replaced, unreliable, or incompatible"),
    "ARCHIVE": ("docs/09 s98", "after a retention period; never deleted"),
}

# The lifecycle STATUSES docs/09 s98 names, and the two VERDICTS that
# share their words. A status and a verdict are not the same thing -
# ARCHIVED is where a fragment is, ARCHIVE is what somebody would do.
DEPRECATED = "DEPRECATED"
ARCHIVED = "ARCHIVED"
GUARDED = "PRODUCTION"

DO_ARCHIVE = "ARCHIVE"
DO_DEPRECATE = "DEPRECATE"


def consider(fragment, because=None, findings=None):
    """
    {available, ruled_out, order, why} - or a refusal. Nothing is
    decided, changed or written.
    """
    if not fragment:
        return {"considered": False, "refused": "NOTHING_TO_EVOLVE",
                "why": "no fragment was handed in."}

    card = getattr(fragment, "data", fragment)
    if not isinstance(card, dict):
        return {"considered": False, "refused": "NOT_A_FRAGMENT",
                "why": "%r is not a fragment card." % (card,)}
    who = str(getattr(fragment, "slug", None) or card.get("id") or "").strip()
    if not who:
        return {"considered": False, "refused": "NOT_A_FRAGMENT",
                "why": "the fragment has no id."}

    reason = str(because or "").strip()
    if not reason:
        return {"considered": False, "refused": "NO_REASON",
                "asked": "What happened that makes '%s' worth evolving?"
                         % who,
                "why": "evolution runs because something happened - a "
                       "failure, a duplicate, a release that dropped. "
                       "Without it every answer here is 'KEEP, probably', "
                       "which is a menu rather than a proposal. D-33: "
                       "asked once rather than assumed."}

    findings = dict(findings or {})
    strangers = sorted(set(str(one).strip().upper() for one in findings)
                       - set(VERDICTS))
    if strangers:
        return {"considered": False, "refused": "NOT_A_VERDICT",
                "why": "%s %s not one of the eight docs/28 names: %s."
                       % (", ".join("'%s'" % one for one in strangers),
                          "is" if len(strangers) == 1 else "are",
                          ", ".join(VERDICTS))}
    said = dict((str(key).strip().upper(), value)
                for key, value in findings.items())

    status = str(card.get("heron-status") or "").strip().upper()

    available, ruled_out = [], []
    for verdict in VERDICTS:
        owner, on_what = OWNS[verdict]
        supported = said.get(verdict)
        # A VERDICT NOBODY CAN ACT ON IS NOT OFFERED.
        if verdict in ("SPLIT", "MERGE") and not supported:
            ruled_out.append({
                "verdict": verdict, "owner": owner,
                "why": "%s has proposed none for '%s'. Offering it anyway "
                       "would be a menu rather than a proposal."
                       % (owner, who)})
            continue
        if verdict == DO_ARCHIVE and status != DEPRECATED:
            ruled_out.append({
                "verdict": verdict, "owner": owner,
                "why": "docs/09 s98 reaches ARCHIVED only from DEPRECATED, "
                       "and '%s' is %s." % (who, status or "unstated")})
            continue
        if verdict == DO_DEPRECATE and status in (DEPRECATED,
                                                  ARCHIVED):
            ruled_out.append({
                "verdict": verdict, "owner": owner,
                "why": "'%s' is already %s." % (who, status)})
            continue
        available.append({
            "verdict": verdict, "owner": owner, "on": on_what,
            "supported_by": supported if supported else None,
            "on_the_ladder": verdict in ON_THE_LADDER})

    order = [one["verdict"] for one in available
             if one["verdict"] in ON_THE_LADDER]
    order.sort(key=lambda one: ON_THE_LADDER.index(one))
    beside = [one["verdict"] for one in available
              if one["verdict"] in BESIDE_IT]

    return {
        "considered": True, "decided": False, "fragment": who,
        "because": reason, "status": status or None,
        "available": available, "ruled_out": ruled_out,
        "order": order, "beside_the_ladder": beside,
        "may_be_applied": status != GUARDED,
        "why": "'%s' (%s), because: %s. %d of the %d verdict(s) are "
               "available; Golden Rule 4's order among them is %s%s."
               % (who, status or "no status", reason, len(available),
                  len(VERDICTS), " then ".join(order) or "(none of the "
                  "four)",
                  ", and %s sit beside that ladder" % ", ".join(beside)
                  if beside else ""),
        "unjudged": [
            "WHICH ONE. This agent decides none of the eight and holds no "
            "rule of its own - every verdict names the agent or the "
            "document that owns it. docs/28 says this row ALWAYS "
            "PROPOSES, and proposing means naming what is available and "
            "who would say so.",
            "THE ORDER IS GOLDEN RULE 4's, AND ONLY FOUR OF THE EIGHT ARE "
            "ON IT: keep, extend, adapt, version-branch. 'Adapt' maps onto "
            "UPDATE, which is the one place this file reads between two "
            "documents - said out loud, because the alternative is eight "
            "verdicts with no order at all, which is how the last resort "
            "gets picked first.",
            "%s" % ("%d VERDICT(S) WERE RULED OUT, NOT HIDDEN: %s. A "
                    "verdict nobody can act on is a menu item."
                    % (len(ruled_out),
                       "; ".join("%s (%s)" % (one["verdict"],
                                              one["owner"])
                                 for one in ruled_out))
                    if ruled_out else
                    "nothing was ruled out; every verdict has somebody "
                    "who could act on it."),
            "%s" % ("'%s' IS AT %s, WHICH docs/09 s118 SAYS NOTHING MAY "
                    "CHANGE AUTONOMOUSLY. Whatever is chosen needs a "
                    "person." % (who, GUARDED) if status == GUARDED else
                    "nothing here was applied. This agent's risk is "
                    "SUGGEST and it writes nothing at all."),
        ],
    }


def main(argv):
    print("EVOLUTION   eight verdicts, and this agent owns none of them")
    print("=" * 72)

    print("\nGolden Rule 4's ladder, mapped onto docs/28's words")
    for mine, theirs in LADDER:
        print("  %-10s <- %s" % (mine, theirs))
    print("  beside it: %s" % ", ".join(BESIDE_IT))

    answer = consider(
        {"id": "count-elements", "heron-status": "PROVEN"},
        because="it failed twice on Revit 2021 and nobody has run it "
                "there since",
        findings={"SPLIT": "HERON-FRG-SPL-003 proposed two parts"})
    print("\n%s" % answer["why"])
    for one in answer["available"]:
        print("  %-10s %-22s %s" % (one["verdict"], one["owner"],
                                    one["on"][:34]))
    print("\n  ruled out")
    for one in answer["ruled_out"]:
        print("    %-10s %s" % (one["verdict"], one["why"][:56]))

    print("\nrefused")
    for fragment, reason, these in (
            (None, "x", {}),
            ("a string", "x", {}),
            ({"id": "x"}, "", {}),
            ({"id": "x"}, "x", {"RETIRE": "somebody said so"})):
        bad = consider(fragment, reason, these)
        print("  %-22s %s" % (bad["refused"], bad["why"][:44]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
