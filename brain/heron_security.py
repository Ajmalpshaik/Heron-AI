# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-SEC-009
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Security review - required at MODIFY and above, and a review of a
sentence is not a review.

    python brain/heron_security.py

WHAT IT IS FOR (docs/28, HERON-DEV-SEC-009)
--------------------------------------------
"Security Review Agent. REQUIRED FOR ANYTHING AT MODIFY OR ABOVE." T2.

IT DOES NOT DECIDE WHETHER A CHANGE IS SAFE
---------------------------------------------
That is the review, and a review is a person reading a diff. What this
agent does is make the requirement real: it establishes whether a
review was needed, whether one happened, whether the person who did it
could have done it, and whether what they saw is still what is there.

Four questions, all mechanical, and none of them is "is this safe".

WHERE THE LINE IS, AND WHY IT IS NOT A NUMBER HERE
----------------------------------------------------
The ladder is HERON-KRN-CAP-005's `RISK_ORDER`, bound by identity, and
MODIFY's place on it is looked up rather than written down. A change at
or above that rung needs a review; below it does not, and that comes
back as `required: False` - which is not the same as a pass and is
never reported as one.

Writing `4` here, or copying the seven rungs, would be the fifth copy of
that ladder in brain/ (PROPOSALS F22 counts four) and the one most
likely to be read as permission.

A REVIEW OF A SENTENCE IS NOT A REVIEW
----------------------------------------
docs/10's rule about publishing says it plainly: somebody reviewing
"3 fragments, MEP, low risk" has reviewed a sentence.
HERON-GIT-COM-010 answers that by recording the sha of what was read
and RECOMPUTING it. The same trick, turned on a change: a review
carries the fingerprint of the thing reviewed, and one trailing space
added afterwards makes it stale.

Stale is refused, not warned about (D-35). A review of a previous
version of a MODIFY change is exactly the review nobody meant to give.

NO AGENT APPROVES ITSELF, AND NEITHER DOES AN AUTHOR
------------------------------------------------------
Golden Rule 7, structurally: the reviewer may not be the author. A
review signed by the person who wrote the change is refused, and that
refusal says which name appeared twice rather than implying anything
about them.

WHAT THE REVIEWER IS GIVEN
----------------------------
The evidence, gathered: the declared risk and where it sits on the
ladder, the tools the change may use, and anything in it that looks
like a credential.

That last one is HERON-KRN-SEC-012's `refuse_secret_input`, called
rather than re-implemented, and calling it rather than writing a test
here settles two things the first version of this file got wrong:

  A VALUE, NOT A KEY NAME.  That method's own comment says it - "it
  looks at values rather than at key names: calling the field `token`
  is not what makes it dangerous". A field called `name` holding
  "Ajmal" is not a credential, and a field called `blurb` holding a
  forge token is. The first version tested key names as well and
  reported five credentials in a change carrying one.

  NESTED, NOT TOP LEVEL.  `{"auth": {"token": ...}}` is how a
  credential is actually handed over; nobody puts one in a bare field
  called `secret`. That method walks in, and a loop written here did
  not.

WHAT IT CANNOT SEE, SAID PLAINLY
----------------------------------
The shapes are HERON-KRN-SEC-012's six, and HERON-GIT-REP-002 already
established the edge: `https://a:ghp_...@host` is caught and
`https://a:hunter2@host` is not. A password that looks like a word
looks like a word. So `credentialShaped` being empty is NOT "there are
no credentials in this change" - it is "none of the six known shapes
appeared", and the answer says that in those words rather than in
reassuring ones.
"""

from __future__ import annotations

import hashlib
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_capability as CAP  # noqa: E402
import heron_secrets as SECRETS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# HERON-KRN-CAP-005's ladder, bound rather than copied. PROPOSALS F22
# counts four copies of it in brain/ already, and a fifth inside the
# agent that decides when a review is required would be the one most
# likely to be read as permission.
RISK = CAP.RISK_ORDER

# The rung the register names. Its POSITION is looked up, never written
# as a number - the ladder is allowed to grow a rung without this file
# becoming wrong.
FROM_RISK = "MODIFY"

# What a review has to carry to be one.
A_REVIEW_CARRIES = (
    ("by", "who read it. A review nobody signed is not a review"),
    ("of", "the fingerprint of what they read, so a later change makes "
           "it stale"),
)


def required_at():
    """Every rung at or above the one the register names."""
    try:
        at = list(RISK).index(FROM_RISK)
    except ValueError:
        return tuple(RISK)
    return tuple(RISK[at:])


def fingerprint(change):
    """
    A stable sha of what a reviewer would have read.

    Over the whole change, sorted, so a field reordered is the same
    review and a character added is not. The same argument
    HERON-GIT-COM-010 makes about a contribution, one step earlier.
    """
    change = getattr(change, "data", change)
    return hashlib.sha256(_stable(change).encode("utf-8")).hexdigest()


def _stable(thing):
    """A change written out the same way every time."""
    if isinstance(thing, dict):
        return "{%s}" % ",".join("%s:%s" % (key, _stable(thing[key]))
                                 for key in sorted(thing))
    if isinstance(thing, (list, tuple)):
        return "[%s]" % ",".join(_stable(one) for one in thing)
    return str(thing)


def review(change, reviewed=None, secrets=None):
    """
    {required, reviewed, evidence} - or a refusal. Nothing here decides
    whether the change is safe.
    """
    if not change:
        return {"required": None, "refused": "NOTHING_TO_REVIEW",
                "why": "no change was handed in."}

    change = getattr(change, "data", change)
    if not isinstance(change, dict):
        return {"required": None, "refused": "NOT_A_CHANGE",
                "why": "%r is not a change. One declares a `risk` and an "
                       "author." % (change,)}

    risk = str(change.get("risk") or "").strip().upper()
    if not risk:
        return {"required": None, "refused": "NOT_A_CHANGE",
                "why": "the change declares no risk. The risk is what "
                       "decides whether a review is required at all, so "
                       "there is nothing to decide with."}
    if risk not in RISK:
        return {"required": None, "refused": "UNKNOWN_RISK",
                "why": "%r is not one of %s. HERON-KRN-CAP-005 owns the "
                       "ladder and an eighth rung is not invented here."
                       % (risk, ", ".join(RISK))}

    keeper = secrets if secrets is not None else SECRETS.Secrets()
    needed = risk in required_at()

    # WHAT THE REVIEWER IS GIVEN, gathered whether or not one is
    # required - a change below the line that turns out to carry a
    # credential is still worth seeing.
    looks_like = _credentials(change, keeper)
    evidence = {
        "risk": risk,
        "rung": list(RISK).index(risk) + 1,
        "of": len(RISK),
        "requiredFrom": FROM_RISK,
        "tools": sorted(str(one) for one in (change.get("allowed-tools")
                                             or change.get("tools") or [])),
        "author": change.get("by") or change.get("author"),
        "credentialShaped": looks_like,
        "fingerprint": fingerprint(change),
    }

    if not needed:
        return {
            "required": False, "reviewed": None, "evidence": evidence,
            "why": "%s is below %s, so no security review is required. That "
                   "is NOT a pass - nothing was reviewed." % (risk,
                                                              FROM_RISK),
            "unjudged": _unjudged(evidence, needed, None),
        }

    if not reviewed:
        return {"required": True, "refused": "NOT_REVIEWED",
                "evidence": evidence,
                "why": "%s is at or above %s, and the register says a "
                       "security review is REQUIRED for anything there. "
                       "None was handed in. D-35 - unapproved is refused, "
                       "not warned about." % (risk, FROM_RISK)}

    reviewed = getattr(reviewed, "data", reviewed)
    if not isinstance(reviewed, dict):
        return {"required": True, "refused": "NOT_A_REVIEW",
                "evidence": evidence,
                "why": "%r is not a review. One carries %s."
                       % (reviewed, ", ".join(field for field, _
                                              in A_REVIEW_CARRIES))}
    absent = [field for field, _ in A_REVIEW_CARRIES
              if not str(reviewed.get(field) or "").strip()]
    if absent:
        return {"required": True, "refused": "NOT_A_REVIEW",
                "evidence": evidence, "missing": absent,
                "why": "the review is missing %s. %s"
                       % (", ".join(absent),
                          " ".join(why for field, why in A_REVIEW_CARRIES
                                   if field in absent))}

    by = str(reviewed["by"]).strip()
    author = str(evidence["author"] or "").strip()
    if author and by.lower() == author.lower():
        return {"required": True, "refused": "REVIEWED_BY_THE_AUTHOR",
                "evidence": evidence,
                "why": "%r wrote the change and %r reviewed it. Golden Rule "
                       "7 - no agent approves itself, and neither does an "
                       "author." % (author, by)}

    now = evidence["fingerprint"]
    if str(reviewed["of"]).strip() != now:
        return {"required": True, "refused": "STALE_REVIEW",
                "evidence": evidence,
                "reviewedOf": str(reviewed["of"]).strip()[:16],
                "isNow": now[:16],
                "why": "the review was of %s... and the change is now "
                       "%s... One character added after a review makes it "
                       "a review of something else, and that is exactly "
                       "the review nobody meant to give."
                       % (str(reviewed["of"]).strip()[:16], now[:16])}

    return {
        "required": True,
        "reviewed": True,
        "by": by,
        "evidence": evidence,
        "safe": None,
        "why": "%s is at or above %s, and %s reviewed it against the change "
               "as it stands. Whether it is SAFE is their answer, not this "
               "agent's." % (risk, FROM_RISK, by),
        "unjudged": _unjudged(evidence, needed, by),
    }


# HERON-KRN-SEC-012's own answer to "is there a credential in this
# payload", bound rather than re-implemented. It walks nested maps and
# lists, it skips a Handle and a `heron:secret/` string - which are
# exactly what SHOULD travel - and it judges the VALUE.
CREDENTIALS_IN = SECRETS.Secrets.refuse_secret_input


def _credentials(change, keeper):
    """
    Anything in the change shaped like a credential, never its value.

    THE VALUE IS NEVER READ OUT. `refuse_secret_input` returns where it
    was and which shape matched, and that is all that comes back - the
    point of naming a field is that somebody goes and looks at it, not
    that they read the credential here.

    THE FIRST VERSION OF THIS CALLED redact() AND COMPARED THE RESULT TO
    THE TEXT. `redact` returns a TUPLE - (clean, found) - so the compare
    was tuple-against-string, which is never equal, so every non-empty
    field in the change came back as a credential. The demo said five,
    in a change carrying one. A two-value return read as one value fails
    in the direction that looks like it is working, which is why the
    method that answers the question is called instead of the primitive
    underneath it.
    """
    out = []
    for where, _why, shape in CREDENTIALS_IN(keeper, change):
        out.append({"field": str(where), "shape": shape,
                    "why": "the VALUE of this field is shaped like a %s. It "
                           "is not in this answer - go and look" % shape})
    return out


def _unjudged(evidence, needed, by):
    return [
        "WHETHER THE CHANGE IS SAFE. That is the review, and a review is "
        "a person reading a diff. What was established here is that one "
        "was %s, by %s, against the change as it stands."
        % ("required and given" if needed and by
           else "not required" if not needed else "required",
           by or "nobody yet"),
        "THE LADDER IS HERON-KRN-CAP-005's, BOUND RATHER THAN COPIED, and "
        "%s's place on it is looked up rather than written as a number. "
        "PROPOSALS F22 counts four copies of that ladder in brain/ "
        "already, and a fifth inside the agent that decides when a review "
        "is REQUIRED would be the one most likely to be read as "
        "permission." % FROM_RISK,
("THE VALUE OF %d FIELD%s SHAPED LIKE A CREDENTIAL: %s. The value "
         "itself is not in this answer - HERON-KRN-SEC-012's "
         "`refuse_secret_input` said which field and which shape, and the "
         "point of naming one is that somebody goes and looks."
         % (len(evidence["credentialShaped"]),
            " IS" if len(evidence["credentialShaped"]) == 1 else "S ARE",
            ", ".join("%s (%s)" % (one["field"], one["shape"])
                      for one in evidence["credentialShaped"]))
         if evidence["credentialShaped"] else
         "NONE OF THE SIX KNOWN SHAPES APPEARED IN THIS CHANGE, which is "
         "not the same as `there is no credential in it`. "
         "HERON-GIT-REP-002 established the edge: "
         "https://a:ghp_...@host is caught and https://a:hunter2@host is "
         "not, because a password that looks like a word looks like a "
         "word."),
        ("BELOW %s IS NOT A PASS. Nothing was reviewed, and reporting "
         "`no review required` as a clean result is how a change at "
         "EXECUTE gets treated like one at READ." % FROM_RISK
         if not needed else
         "THE FINGERPRINT IS RECOMPUTED, NOT TRUSTED. A review carries the "
         "sha of what was read, and one character added afterwards makes "
         "it stale - which is refused rather than warned about (D-35)."),
        "WHAT THE TOOLS ACTUALLY DO. The change's `allowed-tools` are "
        "reported as declared. Whether a tool named there can reach the "
        "network or the disk is the tool registry's answer, and that "
        "lives in mcp/ - which D-48 puts out of this agent's reach.",
    ]


def main(argv):
    print("SECURITY REVIEW   required at %s and above" % FROM_RISK)
    print("=" * 72)
    print("\nthe ladder: %s" % ", ".join(RISK))
    print("required at: %s" % ", ".join(required_at()))

    # THE TOKEN IS HERON-KRN-SEC-012's OWN FAKE, assembled there from
    # pieces. A token-shaped literal typed into a tracked file is what
    # push protection exists to stop, and a demonstration that cannot be
    # committed demonstrates nothing. Nesting it under `auth` is how one
    # actually arrives - nobody puts a credential in a bare top-level
    # field - and a top-level loop would have walked straight past it.
    change = {"name": "move_elements", "risk": "MODIFY", "by": "Ajmal",
              "allowed-tools": ["revit_apply_move"],
              "auth": {"header": "Bearer " + SECRETS.FAKE_FORGE_TOKEN}}

    print("\nbelow the line")
    # RISK[0], not "READ". The only rung this module spells out as a value
    # is the one the register names, so that the bottom of the ladder can
    # be renamed without a demonstration here quietly becoming wrong - and
    # the demo says "the bottom rung is below the line" whatever it is
    # called.
    low = review({"name": "count", "risk": RISK[0], "by": "Ajmal"})
    print("  required: %s" % low["required"])
    print("  %s" % low["why"])

    print("\nat the line, with no review")
    said = review(change)
    print("  %-24s %s" % (said["refused"], said["why"][:44]))
    print("  evidence gathered anyway: risk %s, rung %d of %d, %d "
          "credential-shaped field(s)"
          % (said["evidence"]["risk"], said["evidence"]["rung"],
             said["evidence"]["of"],
             len(said["evidence"]["credentialShaped"])))

    print("\nwith a review")
    sha = fingerprint(change)
    for who, of, label in (("Ajmal", sha, "signed by the author"),
                           ("Reviewer", "deadbeef" * 8, "of an older version"),
                           ("Reviewer", sha, "by somebody else, current")):
        said = review(change, {"by": who, "of": of})
        print("  %-22s %-24s %s"
              % (label, said.get("refused") or "accepted",
                 said["why"][:34]))

    print("\nrefused")
    for these in (None, "a string", {"name": "x"}, {"risk": "DANGEROUS"}):
        bad = review(these)
        print("  %-24s %s" % (bad["refused"], bad["why"][:42]))
    bad = review(change, "a string")
    print("  %-24s %s" % (bad["refused"], bad["why"][:42]))
    bad = review(change, {"by": "Reviewer"})
    print("  %-24s %s" % (bad["refused"], bad["why"][:42]))

    good = review(change, {"by": "Reviewer", "of": sha})
    print("\nwhat this agent does not judge")
    for line in good["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
