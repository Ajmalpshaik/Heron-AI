# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-SKL-UPD-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Skill revision - back to DRAFT, and a production skill does not get its
callers broken quietly.

    python brain/heron_revision.py

WHAT IT IS FOR (docs/28, HERON-SKL-UPD-003)
--------------------------------------------
"Revises an existing skill without breaking callers. Enters the
lifecycle at DRAFT, never straight to production." T3, risk MODIFY.

BREAKING IS A DIRECTION, NOT AN OPINION
-----------------------------------------
"Without breaking callers" needs a definition or it is a wish. The one
used here is mechanical, and it is a table rather than a run of ifs
(D-29 - a contract is data, not prose):

  utterances    REMOVING one breaks whoever says it
  revit         REMOVING one breaks whoever is on that release
  risk          RAISING it breaks whoever could run it and now cannot
  needs         ADDING one breaks wherever that capability is unserved
  preconditions ADDING one breaks whoever met the old set

Note that the direction is not the same for every field. Removing an
utterance breaks; adding one cannot. Adding a precondition breaks;
removing one cannot. Getting this backwards is how an agent reports a
harmless change as dangerous and the dangerous one as fine.

Risk is on the list because docs/22 s64 makes modes "a permission
boundary, not a display preference". Raising a skill's risk is not
paperwork - it is taking the skill away from everybody below the new
line.

A BREAKING CHANGE TO A PRODUCTION SKILL IS PROPOSED, NOT APPLIED
------------------------------------------------------------------
docs/09 s118 says of split, merge and evolution: "All three PROPOSE;
none may apply autonomously to anything at PRODUCTION... These agents
should open a proposal - in practice, a pull request."

That note is written about fragments. Applying it to skills is an
EXTENSION of it rather than a quotation, and it is made deliberately:
the reason given there - that it "silently changes the behaviour of
every skill that depends on it" - is exactly as true of a skill and the
people who depend on it.

Below PRODUCTION the break is reported and applied. A DRAFT skill has no
callers to break; that is what DRAFT means.

EVERY REVISION GOES BACK TO DRAFT
-----------------------------------
Not only the breaking ones. A revised skill is a thing nobody has
watched work, whatever its predecessor earned, and D-30 wants one
recorded proof rather than an inherited reputation.

Which is why a revision that changes NOTHING is refused. Knocking a
PRODUCTION skill back to DRAFT for a no-op would be the most destructive
thing in this file, and it would look like housekeeping.
"""

from __future__ import annotations

import io
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import yaml  # noqa: E402

import heron_fragment as FRAG  # noqa: E402
import heron_skill as SKILL  # noqa: E402
import heron_capability as CAP  # noqa: E402
import heron_authoring as CRE  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

VERSIONS = FRAG.REVIT_VERSIONS
RISK = CAP.RISK_ORDER

# Where a revision lands, read off docs/09's ladder rather than typed.
ENTERS_AT = CRE.ENTERS_AT

# The state docs/09 protects by name, and the end state nothing comes
# back from.
GUARDED = "PRODUCTION"
ENDED = "ARCHIVED"

# WHICH DIRECTION BREAKS, per field. Data rather than a run of ifs, and
# the directions are NOT all the same - which is the whole point of
# writing it down.
BREAKS = (
    ("utterances", "removed", "whoever says it has nothing to say now"),
    ("revit", "removed", "whoever is on that release loses the skill"),
    ("preconditions", "added", "whoever met the old set now fails a new "
                               "one, and finds out at run time"),
    ("needs", "added", "the new capability may have no provider, and the "
                       "skill that ran yesterday does not run today"),
)

# risk is its own row: it is one value, not a set, and it breaks upward.
# docs/22 s64 - modes are a permission boundary, not a display preference.
RAISING_RISK_BREAKS = ("risk", "raised",
                       "everybody below the new line could run it and now "
                       "cannot - docs/22 s64, a permission boundary is not "
                       "a display preference")


def _least(risk):
    risk = str(risk or "").strip().upper()
    return RISK.index(risk) if risk in RISK else None


def _set(card, field):
    return [str(one).strip() for one in (card.get(field) or [])
            if str(one).strip()]


def breaks(before, after):
    """Every breaking change between two cards, each naming its direction."""
    found = []
    for field, direction, harm in BREAKS:
        was, now = set(_set(before, field)), set(_set(after, field))
        gone = sorted(was - now) if direction == "removed" else sorted(now - was)
        if gone:
            found.append({"field": field, "direction": direction,
                          "what": gone, "why": harm})

    field, direction, harm = RAISING_RISK_BREAKS
    was, now = _least(before.get(field)), _least(after.get(field))
    if was is not None and now is not None and now > was:
        found.append({"field": field, "direction": direction,
                      "what": [RISK[was], RISK[now]], "why": harm})
    return found


def revise(existing, changes, into=None):
    """
    Writes the revised card at DRAFT and returns {path, card, broke} - or
    refuses. Nothing at PRODUCTION is broken without a proposal.
    """
    if not existing:
        return {"revised": False, "refused": "NO_SUCH_SKILL",
                "why": "no existing skill was handed in. Revising nothing "
                       "is authoring, and HERON-SKL-CRE-002 is the agent "
                       "for that."}
    if not changes:
        return {"revised": False, "refused": "NOTHING_TO_APPLY",
                "why": "no changes were handed in. An empty revision would "
                       "still put the skill back to %s, which is a "
                       "demotion dressed as an edit." % ENTERS_AT}

    before = getattr(existing, "data", existing)
    changes = getattr(changes, "data", changes)
    if not isinstance(before, dict) or not isinstance(changes, dict):
        return {"revised": False, "refused": "NOT_A_REVISION",
                "why": "both the existing skill and the changes must be "
                       "skill cards."}

    who = str(before.get("id") or "").strip()
    if not who:
        return {"revised": False, "refused": "NO_SUCH_SKILL",
                "why": "the existing skill has no id, so there is nothing "
                       "to revise and nothing to write over."}
    if changes.get("id") and str(changes["id"]).strip() != who:
        return {"revised": False, "refused": "NOT_A_REVISION",
                "why": "the changes rename '%s' to '%s'. A skill's id is "
                       "what every caller holds; changing it is authoring "
                       "a second skill, which HERON-SKL-CRE-002 does and "
                       "this agent does not."
                       % (who, str(changes["id"]).strip())}

    # A CARD THAT IS NOT A VALID CARD CANNOT BE REVISED INTO ONE. The
    # step and version below are carried forward from it, and carrying
    # forward a field that is not there writes `heron-step: null`.
    missing = [field for field in SKILL.REQUIRED
               if before.get(field) in (None, "", [], {})]
    if missing:
        return {"revised": False, "refused": "INCOMPLETE",
                "why": "'%s' is missing %s, so it is not a card this agent "
                       "can carry forward. HERON-SKL-VAL-004 says what a "
                       "complete one is." % (who, ", ".join(missing))}

    status = str(before.get("heron-status") or "").strip().upper()
    if status == ENDED:
        return {"revised": False, "refused": "NOT_REVISABLE",
                "why": "'%s' is %s. docs/09's ladder ends there and never "
                       "deletes - reviving it as a %s would put a retired "
                       "skill back in front of people as a new one."
                       % (who, ENDED, ENTERS_AT)}

    after = dict(before)
    for field, value in changes.items():
        if not field.startswith("heron-"):
            after[field] = value

    strangers = sorted(set(one for one in _set(after, "revit")
                           if one not in VERSIONS))
    if strangers:
        return {"revised": False, "refused": "NOT_A_VERSION",
                "why": "the revision declares %s. Known: %s - D-05 does "
                       "not extrapolate."
                       % (", ".join("'%s'" % one for one in strangers),
                          ", ".join(VERSIONS))}
    if _least(after.get("risk")) is None:
        return {"revised": False, "refused": "NOT_A_RISK",
                "why": "risk %r is not one of %s."
                       % (after.get("risk"), ", ".join(RISK))}

    moved = sorted(field for field in after
                   if not field.startswith("heron-")
                   and after[field] != before.get(field))
    if not moved:
        return {"revised": False, "refused": "NOTHING_CHANGED",
                "why": "the revision changes nothing. Every revision goes "
                       "back to %s, so applying a no-op to a %s skill "
                       "would knock it off the ladder for free - the most "
                       "destructive thing this agent could do, wearing the "
                       "clothes of housekeeping." % (ENTERS_AT, GUARDED)}

    broke = breaks(before, after)
    if broke and status == GUARDED:
        return {"revised": False, "refused": "BREAKS_CALLERS",
                "broke": broke, "status": status,
                "propose": "docs/09 s118 - a proposal, in practice a pull "
                           "request, which is what the GitHub department "
                           "is for",
                "why": "'%s' is at %s and %d change(s) break callers: %s. "
                       "Proposed, not applied."
                       % (who, GUARDED, len(broke),
                          "; ".join("%s %s %s"
                                    % (one["field"], one["direction"],
                                       ", ".join(one["what"]))
                                    for one in broke))}

    card = dict((field, after[field]) for field in SKILL.REQUIRED
                if not field.startswith("heron-") and field in after)
    for field in after:
        if field not in card and not field.startswith("heron-"):
            card[field] = after[field]
    card = dict([("heron-status", ENTERS_AT),
                 ("heron-step", before.get("heron-step")),
                 ("heron-since", before.get("heron-since")),
                 ("heron-layer", before.get("heron-layer") or "brain")]
                + list(card.items()))

    where = into or SKILL.SKILLS_DIR
    path = os.path.join(where, "%s.yaml" % who)
    if not os.path.isdir(where):
        os.makedirs(where)
    io.open(path, "w", encoding="utf-8").write(
        "%s\n%s" % (
            yaml.safe_dump(dict((f, card[f]) for f in CRE.WRITES),
                           default_flow_style=False, sort_keys=False,
                           allow_unicode=True),
            yaml.safe_dump(dict((f, card[f]) for f in card
                                if not f.startswith("heron-")),
                           default_flow_style=False, sort_keys=False,
                           allow_unicode=True)))

    return {
        "revised": True, "path": path, "card": card, "changed": moved,
        "was": status or None, "status": ENTERS_AT, "broke": broke,
        "why": "'%s' revised in %d field(s) - %s - and put back to %s "
               "from %s.%s"
               % (who, len(moved), ", ".join(moved), ENTERS_AT,
                  status or "no status",
                  " %d change(s) break callers, applied because it is not "
                  "at %s." % (len(broke), GUARDED) if broke else ""),
        "unjudged": [
            "IT WENT BACK TO %s, AND NOT ONLY BECAUSE SOMETHING BROKE. A "
            "revised skill is a thing nobody has watched work, whatever "
            "its predecessor earned - D-30 wants one recorded proof, not "
            "an inherited reputation." % ENTERS_AT,
            "%s" % ("%d CHANGE(S) BREAK CALLERS AND WERE APPLIED ANYWAY: "
                    "%s. That is allowed here only because '%s' is not at "
                    "%s - below it there are no callers to break, which is "
                    "what %s means."
                    % (len(broke),
                       "; ".join("%s %s" % (one["field"], one["direction"])
                                 for one in broke),
                       who, GUARDED, ENTERS_AT) if broke else
                    "no change here breaks a caller: nothing was removed "
                    "from utterances or revit, nothing was added to needs "
                    "or preconditions, and the risk was not raised."),
            "WHETHER THE REVISION IS AN IMPROVEMENT. Nothing here ran it. "
            "HERON-SKL-VAL-004 validates the card and "
            "HERON-SKL-PRF-006 will have no runs at all for it yet.",
            "THE OLD CARD IS GONE FROM DISK. This agent writes over the "
            "file it revised, and git is the history - there is no second "
            "copy kept here, and nothing in this answer pretends there is.",
        ],
    }


def main(argv):
    import shutil
    import tempfile

    print("SKILL REVISION   back to DRAFT, and PRODUCTION is not broken "
          "quietly")
    print("=" * 72)

    print("\nwhich direction breaks, per field")
    for field, direction, harm in BREAKS + (RAISING_RISK_BREAKS,):
        print("  %-14s %-8s %s" % (field, direction, harm[:44]))

    live = {"id": "tally-terminals", "name": "How many terminals",
            "domain": "revit.reporting", "purpose": "Counts them.",
            "utterances": ["how many air terminals", "count the terminals"],
            "needs": ["COUNT_ELEMENTS"],
            "preconditions": ["a document is open"],
            "risk": "READ", "revit": ["2024", "2025"],
            "heron-status": "PRODUCTION", "heron-step": 15,
            "heron-since": "0.1.0", "heron-layer": "brain"}

    where = tempfile.mkdtemp()
    try:
        good = revise(live, {"utterances": live["utterances"]
                             + ["terminal count on level 2"]}, into=where)
        print("\n%s" % good["why"])

        print("\nrefused")
        for existing, changes in (
                (None, {"risk": "MODIFY"}),
                (live, None),
                (live, {"id": "something-else"}),
                ({"id": "thin", "name": "x"}, {"risk": "MODIFY"}),
                (dict(live, **{"heron-status": "ARCHIVED"}),
                 {"risk": "MODIFY"}),
                (live, {"revit": ["2028"]}),
                (live, {"risk": "DELETE"}),
                (live, {"name": live["name"]}),
                (live, {"utterances": ["how many air terminals"]})):
            bad = revise(existing, changes, into=where)
            print("  %-20s %s" % (bad["refused"], bad["why"][:48]))

        print("\nat DRAFT the same break is applied and reported")
        drafted = revise(dict(live, **{"heron-status": "DRAFT"}),
                         {"risk": "MODIFY",
                          "preconditions": ["a document is open",
                                            "the view is not a sheet"]},
                         into=where)
        print("  %s" % drafted["why"])
        for one in drafted["broke"]:
            print("    %-14s %-8s %s" % (one["field"], one["direction"],
                                         ", ".join(one["what"])))

        print("\nwhat this agent does not judge")
        for line in good["unjudged"]:
            print("  - %s" % line)
    finally:
        shutil.rmtree(where)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
