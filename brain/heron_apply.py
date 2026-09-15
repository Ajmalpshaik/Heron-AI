# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-UPD-008
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Applying - Evolution decides, this one does it, and a changed fragment
is not still proven.

    python brain/heron_apply.py

WHAT IT IS FOR (docs/28, HERON-FRG-UPD-008)
--------------------------------------------
"APPLIES what Fragment Evolution decided. Evolution decides; this one
does it, and never to PRODUCTION without approval." T3, risk MODIFY.

IT APPLIES WHAT WAS DECIDED, WHICH MEANS IT CHOOSES NOTHING
-------------------------------------------------------------
The verdict must be one HERON-FRG-EVO-005 listed as AVAILABLE for this
fragment. A verdict that agent ruled out is refused here, because
"applies what Evolution decided" is not the same as "applies whatever it
is handed" - and the ruled-out list is exactly where the reasons live.

THREE OF THE EIGHT ARE NOT THIS AGENT'S TO APPLY
--------------------------------------------------
SPLIT and MERGE do not change a fragment; they create or destroy one.
Their own agents are SUGGEST and docs/09 s118 says they only PROPOSE, so
nothing here applies them. BRANCH is HERON-GIT-BRN-003's, and a branch
is a repository act rather than a fragment one.

They are handed on by name rather than refused as nonsense - somebody
chose them, and the answer says who does them.

A CHANGED IMPLEMENTATION IS NOT STILL PROVEN
----------------------------------------------
This is the rule that matters most and the one easiest to skip. D-30's
proof carries a FINGERPRINT of the implementation it was taken against,
and heron_fragment already knows how to tell when the code moved under
it. Applying UPDATE or EXTEND moves the code - so the status comes back
down to DRAFT and the proof goes with it.

Leaving a fragment at PROVEN across an implementation change would mean
the library's own word for "somebody watched this work" attaches to
bytes nobody watched. The status is not lowered as a punishment; it is
lowered because the evidence no longer describes the thing.

NEVER TO PRODUCTION WITHOUT APPROVAL
--------------------------------------
docs/28 puts it in the row and docs/09 s118 puts it in the document. The
approval must name a PERSON, this FRAGMENT and this VERDICT - the same
shape HERON-GIT-PR-005 requires of a confirmation, and for the same
reason: one that names none of them covers everything.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_evolve as EVO  # noqa: E402
import heron_fragment as FRAG  # noqa: E402
import heron_promotion as PRO  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

NOT_A_PERSON = PRO.NOT_A_PERSON
GUARDED = EVO.GUARDED

# Where a fragment lands when its implementation moves. docs/09's ladder,
# read from HERON-FRG-VAL-001 rather than typed.
LANDS_AT = FRAG.STATUSES[FRAG.STATUSES.index("DRAFT")]
NEEDS_PROOF = FRAG.NEEDS_PROOF

# What this agent does, and what it hands on. The three it hands on are
# the ones whose owners are SUGGEST or belong to another department.
APPLIES = ("KEEP", "UPDATE", "EXTEND", "DEPRECATE", "ARCHIVE")
HANDS_ON = ("SPLIT", "MERGE", "BRANCH")

# The two that move the code, and therefore the proof.
MOVES_THE_CODE = ("UPDATE", "EXTEND")

# What each applied verdict does to the status. KEEP is deliberately
# absent: it changes nothing, and a no-op that writes is the most
# destructive thing an applying agent can do quietly.
LANDING = {
    "DEPRECATE": "DEPRECATED",
    "ARCHIVE": "ARCHIVED",
    "UPDATE": LANDS_AT,
    "EXTEND": LANDS_AT,
}

AN_APPROVAL_CARRIES = (
    ("by", "a person's name - Golden Rule 7"),
    ("at", "when"),
    ("fragment", "WHICH fragment"),
    ("verdict", "WHICH verdict. One naming neither covers everything"),
)


def apply_verdict(proposal, verdict, approval=None, implementation=None):
    """
    {applied, card, why} - or a refusal. The card comes back changed;
    nothing is written to disk here.
    """
    if not proposal:
        return {"applied": False, "refused": "NOTHING_TO_APPLY",
                "why": "no evolution proposal was handed in. This agent "
                       "applies what HERON-FRG-EVO-005 decided, and "
                       "without its answer there is nothing to apply."}

    proposal = getattr(proposal, "data", proposal)
    if not isinstance(proposal, dict) or not proposal.get("considered"):
        return {"applied": False, "refused": "NOT_A_PROPOSAL",
                "why": "%r is not an answer from HERON-FRG-EVO-005. One "
                       "carries `considered`, `available` and `ruled_out`."
                       % (proposal,)}

    chosen = str(verdict or "").strip().upper()
    if chosen not in EVO.VERDICTS:
        return {"applied": False, "refused": "NOT_A_VERDICT",
                "why": "'%s' is not one of the eight docs/28 names: %s."
                       % (verdict, ", ".join(EVO.VERDICTS))}

    who = str(proposal.get("fragment") or "").strip()
    available = [str(one.get("verdict")).strip().upper()
                 for one in (proposal.get("available") or [])]
    if chosen not in available:
        blocked = [one for one in (proposal.get("ruled_out") or [])
                   if str(one.get("verdict")).strip().upper() == chosen]
        return {"applied": False, "refused": "NOT_AVAILABLE",
                "verdict": chosen, "available": available,
                "why": "HERON-FRG-EVO-005 did not list %s as available for "
                       "'%s'. %s Applying what Evolution decided is not "
                       "the same as applying whatever is handed over."
                       % (chosen, who,
                          blocked[0].get("why", "") if blocked else
                          "It listed: %s." % ", ".join(available))}

    if chosen in HANDS_ON:
        owner = EVO.OWNS[chosen][0]
        return {"applied": False, "refused": "NOT_MINE_TO_APPLY",
                "verdict": chosen, "owner": owner,
                "why": "%s is %s's. %s SPLIT and MERGE do not change a "
                       "fragment, they create or destroy one, and docs/09 "
                       "s118 says their agents only PROPOSE; a branch is a "
                       "repository act rather than a fragment one."
                       % (chosen, owner,
                          "Handed on by name rather than refused as "
                          "nonsense - somebody chose it.")}

    status = str(proposal.get("status") or "").strip().upper()

    # NEVER TO PRODUCTION WITHOUT APPROVAL. docs/28's row and docs/09 s118.
    if status == GUARDED:
        if not approval:
            return {"applied": False, "refused": "NOT_APPROVED",
                    "asked": "Approve %s on '%s', which is at %s?"
                             % (chosen, who, GUARDED),
                    "why": "'%s' is at %s and docs/28 says never to %s "
                           "without approval. D-35: unapproved is refused, "
                           "not applied with a note."
                           % (who, GUARDED, GUARDED)}
        approval = getattr(approval, "data", approval)
        if not isinstance(approval, dict):
            return {"applied": False, "refused": "NOT_APPROVED",
                    "why": "%r is not an approval. One carries %s."
                           % (approval, ", ".join(
                               field for field, _ in AN_APPROVAL_CARRIES))}
        absent = [field for field, _ in AN_APPROVAL_CARRIES
                  if not str(approval.get(field) or "").strip()]
        if absent:
            return {"applied": False, "refused": "NOT_APPROVED",
                    "missing": absent,
                    "why": "the approval is missing %s. %s"
                           % (", ".join(absent),
                              " ".join(why for field, why
                                       in AN_APPROVAL_CARRIES
                                       if field in absent))}
        if not PRO._person(approval.get("by")):
            return {"applied": False, "refused": "APPROVED_BY_A_MACHINE",
                    "why": "'%s' approved it, which is not a person. "
                           "Golden Rule 7." % approval.get("by")}
        named = str(approval["fragment"]).strip()
        said = str(approval["verdict"]).strip().upper()
        if named != who or said != chosen:
            return {"applied": False, "refused": "NOT_APPROVED",
                    "approved": "%s on '%s'" % (said, named),
                    "applying": "%s on '%s'" % (chosen, who),
                    "why": "the approval is for %s on '%s' and this "
                           "applies %s on '%s'. An approval that does not "
                           "name both covers everything."
                           % (said, named, chosen, who)}

    if chosen in MOVES_THE_CODE and not str(implementation or "").strip():
        return {"applied": False, "refused": "NOTHING_TO_CHANGE",
                "asked": "What does the new implementation of '%s' say?"
                         % who,
                "why": "%s moves the code and no new implementation came "
                       "with it. Applying it would change the status and "
                       "nothing else, which says a fragment was revised "
                       "when it was not." % chosen}

    # KEEP CHANGES NOTHING, AND SAYS SO.
    if chosen == "KEEP":
        return {
            "applied": True, "fragment": who, "verdict": chosen,
            "was": status or None, "status": status or None,
            "changed": [], "proof_kept": True,
            "why": "KEEP on '%s'. Nothing was changed and nothing was "
                   "written - that is what KEEP means, and an applying "
                   "agent that wrote something here would be the "
                   "quietest bug in the library." % who,
            "unjudged": _unjudged(who, chosen, status, [], True),
        }

    landing = LANDING[chosen]
    changed = ["heron-status"]
    if chosen in MOVES_THE_CODE:
        changed.append("implementation")

    card = dict(proposal.get("card") or {})
    card["heron-status"] = landing
    kept = chosen not in MOVES_THE_CODE
    if not kept:
        # THE PROOF DESCRIBED DIFFERENT BYTES. D-30.
        card.pop("proof", None)
        changed.append("proof")

    return {
        "applied": True, "fragment": who, "verdict": chosen,
        "was": status or None, "status": landing, "card": card,
        "changed": changed, "proof_kept": kept,
        "implementation": str(implementation or "") or None,
        "why": "%s on '%s': %s -> %s%s. Nothing was written to disk."
               % (chosen, who, status or "no status", landing,
                  ", and the proof was dropped with it" if not kept
                  else ""),
        "unjudged": _unjudged(who, chosen, status, changed, kept),
    }


def _unjudged(who, chosen, status, changed, kept):
    return [
        "NOTHING WAS WRITTEN TO DISK. The card comes back changed and "
        "somebody else saves it - this agent's risk is MODIFY for the day "
        "it does, and today it hands back a value.",
        "%s" % ("THE PROOF WAS DROPPED BECAUSE %s MOVES THE CODE. D-30's "
                "proof carries a fingerprint of the implementation it was "
                "taken against; leaving '%s' at a status that needs one "
                "(%s) would attach the library's word for 'somebody "
                "watched this work' to bytes nobody watched."
                % (chosen, who, ", ".join(NEEDS_PROOF)) if not kept else
                "%s does not move the code, so the proof stands as it "
                "was." % chosen),
        "WHETHER THIS WAS THE RIGHT VERDICT. HERON-FRG-EVO-005 listed it "
        "as available and somebody chose it; nothing here weighed it "
        "against the others it listed.",
        "%s" % ("'%s' WAS AT %s, AND THE APPROVAL THAT ALLOWED THIS NAMED "
                "BOTH THE FRAGMENT AND THE VERDICT." % (who, GUARDED)
                if status == GUARDED else
                "'%s' is not at %s, so no approval was required - docs/28 "
                "asks for one there and nowhere else, and demanding one "
                "here would be inventing a gate." % (who, GUARDED)),
    ]


def main(argv):
    print("APPLYING   Evolution decides, this one does it")
    print("=" * 72)
    print("\napplies: %s" % ", ".join(APPLIES))
    print("hands on: %s" % ", ".join(
        "%s -> %s" % (one, EVO.OWNS[one][0]) for one in HANDS_ON))

    frag = {"id": "count-elements", "heron-status": "PROVEN"}
    plan = EVO.consider(frag, because="it failed twice on Revit 2021",
                        findings={"SPLIT": "two parts proposed"})
    plan["card"] = dict(frag, proof={"model": "Tower A, Revit 2024"})

    for verdict, extra in (("KEEP", {}),
                           ("UPDATE", {"implementation": "new bytes"}),
                           ("DEPRECATE", {})):
        answer = apply_verdict(plan, verdict, **extra)
        print("\n%s" % answer["why"])
        if answer.get("changed"):
            print("  changed: %s   proof kept: %s"
                  % (", ".join(answer["changed"]), answer["proof_kept"]))

    print("\nrefused")
    live = dict(plan, status="PRODUCTION")
    for proposal, verdict, kwargs in (
            (None, "KEEP", {}),
            ({"considered": False}, "KEEP", {}),
            (plan, "RETIRE", {}),
            (plan, "MERGE", {}),
            (plan, "SPLIT", {}),
            (plan, "UPDATE", {}),
            (live, "UPDATE", {"implementation": "x"}),
            (live, "UPDATE", {"implementation": "x",
                              "approval": {"by": "ci", "at": "now",
                                           "fragment": "count-elements",
                                           "verdict": "UPDATE"}}),
            (live, "UPDATE", {"implementation": "x",
                              "approval": {"by": "Ajmal", "at": "now",
                                           "fragment": "count-elements",
                                           "verdict": "EXTEND"}})):
        bad = apply_verdict(proposal, verdict, **kwargs)
        print("  %-24s %s" % (bad["refused"], bad["why"][:40]))

    print("\nwhat this agent does not judge")
    for line in apply_verdict(plan, "UPDATE",
                              implementation="new bytes")["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
