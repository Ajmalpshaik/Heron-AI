# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-REG-006
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Regression - Golden Rule 4 made executable, which is what docs/09 s8
calls it.

    python brain/heron_regression.py

WHAT IT IS FOR (docs/28, HERON-FRG-REG-006)
--------------------------------------------
"Builds and tests every supported version; REJECTS UNSAFE CHANGES;
preserves the previous implementation." T1, risk READ.

docs/09 s8 GIVES SIX NUMBERED STEPS, AND THEY ARE THE AGENT
-------------------------------------------------------------
    1. Identify supported versions
    2. Build/test each supported implementation
    3. Compare previous behaviour
    4. Detect breaking changes
    5. REJECT UNSAFE CHANGES
    6. PRESERVE THE PREVIOUS WORKING IMPLEMENTATION

    "This is Golden Rule 4 made executable."

Steps 1 and 2 are not this agent's: HERON-FRG-MTX-009 owns which
releases a fragment builds on, and the results come in from whatever ran
them. Steps 3 to 6 are, and they are done in that order because the
order is the argument - a release nobody tested cannot be compared, and
a comparison nobody made cannot detect a break.

THE CARD IS THE CLAIM, SO NO "BEFORE" RESULTS ARE NEEDED
----------------------------------------------------------
Step 3 says "compare previous behaviour", which sounds like it needs a
second set of results. It does not, and asking for one would make this
agent unusable on the first change after a release.

A fragment's `revit` list is what it CLAIMS to work on. That claim is
the previous behaviour, stated by the fragment itself and validated by
HERON-FRG-VAL-001. So a release the card claimed before the change and
which fails after it is a regression, whoever last ran it - and that is
Golden Rule 4 exactly: never break a working Revit version.

NOT TESTED IS NOT PASSING
---------------------------
A supported release with no result is refused, not assumed. The same
rule HERON-GIT-REL-007 applies to a gate and HERON-SKL-PRF-006 applies
to a release: a thing nobody ran is not a thing that passed.

REJECTED, NOT REPORTED
------------------------
Step 5 says reject. D-35 says unapproved is refused rather than warned
about, and this row says it in bold. A regression comes back as a
refusal with the releases named, not as a finding somebody has to
notice.

PRESERVED IS CHECKED LAST AND ALWAYS
--------------------------------------
Golden Rule 14 - never silently discard. A change that passes every
other step and cannot say where the previous implementation went is
still refused. It is checked last because a rejected change is never
applied, so nothing was at risk until the rest passed.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

VERSIONS = FRAG.REVIT_VERSIONS

PASSED = "pass"
FAILED = "fail"

# docs/09 s8, verbatim, with who owns each. The order is the argument.
STEPS = (
    (1, "Identify supported versions",
     "HERON-FRG-VAL-001 - the card's own `revit` list"),
    (2, "Build/test each supported implementation",
     "HERON-FRG-MTX-009 and whatever ran them; the results come in"),
    (3, "Compare previous behaviour", "this agent"),
    (4, "Detect breaking changes", "this agent"),
    (5, "Reject unsafe changes", "this agent"),
    (6, "Preserve the previous working implementation", "this agent"),
)

MINE = tuple(number for number, _, who in STEPS if who == "this agent")


def _releases(card):
    return [str(one) for one in (card.get("revit") or [])]


def check(before, after, results, preserved=None):
    """
    {safe, steps, why} - or a refusal naming the releases. Nothing is
    applied and nothing is built.
    """
    if not before or not after:
        return {"checked": False, "refused": "NOTHING_TO_CHECK",
                "why": "a regression check needs the fragment as it was "
                       "AND as it would be. One of them is missing, and "
                       "one card on its own is a description rather than "
                       "a change."}

    before = getattr(before, "data", before)
    after = getattr(after, "data", after)
    for card, which in ((before, "before"), (after, "after")):
        if not isinstance(card, dict):
            return {"checked": False, "refused": "NOT_A_FRAGMENT",
                    "why": "the %s side is %r, which is not a fragment "
                           "card." % (which, card)}
        strangers = sorted(set(one for one in _releases(card)
                               if one not in VERSIONS))
        if strangers:
            return {"checked": False, "refused": "NOT_A_FRAGMENT",
                    "why": "the %s side declares %s. Known: %s - D-05 does "
                           "not extrapolate."
                           % (which,
                              ", ".join("'%s'" % one for one in strangers),
                              ", ".join(VERSIONS))}

    claimed = _releases(before)
    if not claimed:
        return {"checked": False, "refused": "NOT_A_FRAGMENT",
                "why": "the before side declares no `revit`, so there is "
                       "no previous behaviour to compare against. A "
                       "fragment that never claimed a release cannot "
                       "regress on one."}

    said = {}
    for release, outcome in dict(results or {}).items():
        word = str(outcome).strip().lower()
        if word not in (PASSED, FAILED):
            return {"checked": False, "refused": "NOT_A_RESULT",
                    "why": "the result for %r is %r, which is neither '%s' "
                           "nor '%s'. A third word would have to be "
                           "interpreted, and interpreting a test result is "
                           "how 'inconclusive' becomes 'fine'."
                           % (release, outcome, PASSED, FAILED)}
        said[str(release)] = word

    # STEP 2's OUTPUT, CHECKED BEFORE STEP 3 CAN USE IT. A release
    # nobody ran cannot be compared, and "not reported" is not "passed".
    #
    # EVERY RELEASE THE CHANGE TOUCHES, not only the ones claimed BEFORE
    # it. Ranging over `claimed` alone let a fragment ADD a release,
    # supply results for the old ones only, and come back safe - so the
    # release with no evidence at all was the newly claimed one, which is
    # the one nothing has ever run. A release is either claimed before,
    # claimed after, or both, and all three need a result.
    wanted = sorted(set(claimed) | set(_releases(after)))
    silent = sorted(one for one in wanted if one not in said)
    if silent:
        added = sorted(set(silent) - set(claimed))
        return {"checked": False, "refused": "NOT_TESTED",
                "releases": silent, "claimed": claimed,
                "newly_claimed": added,
                "why": "%s %s no result. A release nobody ran is not a "
                       "release that passed - and step 3 cannot compare a "
                       "behaviour nobody observed.%s"
                       % (", ".join("'%s'" % one for one in silent),
                          "has" if len(silent) == 1 else "have",
                          "" if not added else
                          " %s newly claimed by this change, so there is "
                          "no previous behaviour to fall back on either: "
                          "nothing has ever run there."
                          % (", ".join("'%s'" % one for one in added)
                             + (" is" if len(added) == 1 else " are")))}

    # STEPS 3 AND 4. The card is the claim, so the previous behaviour is
    # what it said it did.
    failing = sorted(one for one in wanted if said.get(one) == FAILED)

    # A RELEASE THIS CHANGE ADDS AND THAT FAILS DID NOT REGRESS - there
    # is no previous behaviour for it to have fallen away from. It is
    # still refused: a fragment may not start claiming a release it does
    # not work on. Named apart so the answer does not tell a reader the
    # release used to work.
    arriving = sorted(one for one in failing if one not in claimed)
    if arriving:
        return {"checked": False, "refused": "A_NEW_RELEASE_FAILED",
                "releases": arriving, "claimed": claimed,
                "why": "%s %s newly claimed by this change and %s there. "
                       "Nothing regressed - there is no previous "
                       "behaviour on %s - but a fragment may not begin "
                       "claiming a release it does not work on, which is "
                       "the same promise Golden Rule 4 protects from the "
                       "other side."
                       % (", ".join("'%s'" % one for one in arriving),
                          "is" if len(arriving) == 1 else "are",
                          "fails" if len(arriving) == 1 else "fail",
                          "it" if len(arriving) == 1 else "them")}

    regressed = sorted(one for one in failing if one in claimed)
    if regressed:
        return {"checked": False, "refused": "A_RELEASE_REGRESSED",
                "releases": regressed, "claimed": claimed,
                "why": "%s worked before this change and %s after it. "
                       "Golden Rule 4: never break a working Revit version "
                       "unnecessarily - and docs/09 s8 calls this agent "
                       "that rule made executable, so it is REJECTED "
                       "rather than reported."
                       % (", ".join("'%s'" % one for one in regressed),
                          "does not" if len(regressed) == 1
                          else "do not")}

    dropped = sorted(set(claimed) - set(_releases(after)))
    if dropped:
        return {"checked": False, "refused": "A_RELEASE_WAS_DROPPED",
                "releases": dropped, "claimed": claimed,
                "why": "%s %s claimed before this change and %s after it. "
                       "Dropping a supported release is a breaking change "
                       "whether or not it still builds - docs/17 s8 makes "
                       "it a major version AND requires it announced in "
                       "advance, which is HERON-GIT-VER-008's to enforce "
                       "and not something this agent may wave through."
                       % (", ".join("'%s'" % one for one in dropped),
                          "was" if len(dropped) == 1 else "were",
                          "is not" if len(dropped) == 1 else "are not")}

    # STEP 6, LAST AND ALWAYS. Nothing was at risk until the rest passed.
    kept = str(preserved or "").strip()
    if not kept:
        return {"checked": False, "refused": "NOT_PRESERVED",
                "asked": "Where is the previous implementation kept?",
                "why": "the change is safe on every release and nothing "
                       "says where the previous implementation went. "
                       "docs/09 s8 step 6 asks for it kept, and Golden "
                       "Rule 14 says never silently discard - a safe "
                       "change is still a change that replaced something."}

    gained = sorted(set(_releases(after)) - set(claimed))
    return {
        "checked": True, "safe": True,
        "claimed": claimed, "tested": sorted(said),
        "passing": sorted(one for one in said if said[one] == PASSED),
        "gained": gained, "preserved": kept,
        "steps": [{"step": number, "what": what, "who": who,
                   "done_here": who == "this agent"}
                  for number, what, who in STEPS],
        "why": "%d claimed release(s), all tested and all passing%s. The "
               "previous implementation is at %s."
               % (len(claimed),
                  ", and %d gained: %s" % (len(gained), ", ".join(gained))
                  if gained else "", kept),
        "unjudged": [
            "STEPS 1 AND 2 WERE NOT DONE HERE. The supported list came off "
            "the card (HERON-FRG-VAL-001's) and the results came in from "
            "whatever ran them. This agent did steps %s - it built "
            "nothing and ran nothing."
            % ", ".join(str(one) for one in MINE),
            "%s" % ("%d RELEASE(S) WERE GAINED: %s. Gaining one is not "
                    "checked against anything here - nothing in docs/09 s8 "
                    "asks whether a new release was earned, only that an "
                    "old one was not lost." % (len(gained),
                                               ", ".join(gained))
                    if gained else
                    "no release was gained or lost; the claim is the same "
                    "on both sides."),
            "WHETHER THE CHANGE IS AN IMPROVEMENT. Every claimed release "
            "passes, which is the whole of Golden Rule 4 and none of "
            "whether the fragment got better.",
            "WHAT IS AT '%s'. It was required and read back, not opened. "
            "Golden Rule 14 asks that the previous implementation be kept; "
            "nothing here confirmed that anything is there." % kept,
        ],
    }


def main(argv):
    print("REGRESSION   Golden Rule 4 made executable, in six steps")
    print("=" * 72)

    print("\ndocs/09 s8, and who owns each step")
    for number, what, who in STEPS:
        print("  %d. %-46s %s" % (number, what, who))

    three = ["2023", "2024", "2025"]
    before = {"id": "FRG-T-001", "revit": three}
    green = dict((one, "pass") for one in three)

    good = check(before, {"id": "FRG-T-001", "revit": three + ["2026"]},
                 dict(green, **{"2026": "pass"}),
                 preserved="git: 3331ee9 brain/fragments/x/impl")
    print("\n%s" % good["why"])

    print("\nrefused")
    for after, results, preserved in (
            (None, green, "somewhere"),
            ("a string", green, "somewhere"),
            ({"id": "x", "revit": ["2028"]}, green, "somewhere"),
            ({"id": "x", "revit": three}, dict(green, **{"2024": "maybe"}),
             "somewhere"),
            ({"id": "x", "revit": three},
             {"2023": "pass", "2024": "pass"}, "somewhere"),
            ({"id": "x", "revit": three}, dict(green, **{"2023": "fail"}),
             "somewhere"),
            ({"id": "x", "revit": ["2024", "2025"]}, green, "somewhere"),
            ({"id": "x", "revit": three}, green, "")):
        bad = check(before, after, results, preserved)
        print("  %-24s %-14s %s"
              % (bad["refused"], ", ".join(bad.get("releases", [])) or "-",
                 bad["why"][:34]))

    print("\nwhat this agent does not judge")
    for line in good["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
