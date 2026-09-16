# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-QA-016
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The final gate - what must pass is asked of CI, and a gate that ran
something else is not a gate.

    python brain/heron_qa.py

WHAT IT IS FOR (docs/28, HERON-DEV-QA-016)
-------------------------------------------
"QA Agent. Final gate before approval. NEVER THE IMPLEMENTER." T2.

IT RUNS NOTHING, AND THAT IS THE ROW
--------------------------------------
"Never the implementer" is not a note about tone. An agent that runs the
checks and then decides whether they passed is marking its own work, and
Golden Rule 7 exists for exactly that. So this reads results it is
handed and establishes four things:

    every required check is THERE
    every one of them PASSED
    each result is of the change AS IT STANDS
    whoever approves is not whoever wrote it

None of those is an opinion, and none of them needs the checks to be
re-run here.

WHAT MUST PASS IS ASKED, NOT LISTED
-------------------------------------
`.github/workflows/gates.yml` is what actually blocks a merge, so it is
the definition of "required" and it is PARSED. A list typed here would
be a second answer to the same question and would drift the first time
somebody adds a job - and the drift would be silent and in the
dangerous direction, because a check this file had never heard of would
simply not be required.

The four in docs and the ship skill are the same four; this reads the
file that enforces them.

IT IS PARSED WITHOUT PyYAML, ON PURPOSE
-----------------------------------------
`brain/heron_dotnet.py` hoisted a PyYAML import once and killed the CI
compile job, which installs a .NET SDK and nothing else, in one second
before a single project was built. The lines wanted here are
`run: python tools/x.py`, and finding them needs no parser.

A STALE RESULT IS REFUSED, NOT WARNED ABOUT
---------------------------------------------
A check that passed on a previous version of the change is a check of
something else - the same argument HERON-GIT-COM-010 makes about a
review of a summary, and HERON-DEV-SEC-009 about a review of a previous
diff. The fingerprint is HERON-DEV-SEC-009's, bound rather than
reimplemented, and recomputed here. D-35: unapproved is refused.

THE SECURITY REVIEW IS ASKED FOR, NOT REPEATED
------------------------------------------------
HERON-DEV-SEC-009's row is "required for anything at MODIFY or above",
and that agent decides whether one was needed and whether it counts.
This calls it and carries its answer through unaltered, refusals
included. Deciding a second time would be a second answer to one
question (Golden Rule 3).
"""

from __future__ import annotations

import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_security as SEC  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

WORKFLOW = os.path.join(ROOT, ".github", "workflows", "gates.yml")

# Every `python tools/<name>.py` a workflow step runs. Anchored on the
# invocation rather than on a step name, because a step's name is prose
# somebody rewrites and the command is what actually runs.
RUNS = re.compile(r"python\s+(tools/[A-Za-z0-9_\-]+\.py)")

# HERON-DEV-SEC-009's, bound by identity. Two ways of fingerprinting one
# change is how two agents come to disagree about whether a review is
# stale.
fingerprint = SEC.fingerprint
security_review = SEC.review


def required(path=None):
    """
    What CI requires, read from the workflow that enforces it.

    Returns the tool paths in the order they are run, de-duplicated. A
    workflow that cannot be read returns an empty tuple, and `gate`
    refuses rather than passing a change nothing was required of.
    """
    try:
        text = io.open(path or WORKFLOW, encoding="utf-8").read()
    except (IOError, UnicodeDecodeError):
        return ()
    found = []
    for name in RUNS.findall(text):
        if name not in found:
            found.append(name)
    return tuple(found)


def gate(change, results=None, approved_by=None, reviewed=None,
         checks=None, secrets=None):
    """
    {passed, required, missing, failed} - or a refusal. Nothing is run
    and nothing is fixed here.
    """
    if not change:
        return {"passed": False, "refused": "NOTHING_TO_GATE",
                "why": "no change was handed in."}

    change = getattr(change, "data", change)
    if not isinstance(change, dict):
        return {"passed": False, "refused": "NOT_A_SUBMISSION",
                "why": "%r is not a submission. One is a map declaring the "
                       "change, who wrote it, and the results of the checks "
                       "that were run against it." % (change,)}

    must = tuple(checks) if checks is not None else required()
    if not must:
        return {"passed": False, "refused": "NOTHING_IS_REQUIRED",
                "why": "%s names no check to run, so there is no gate here - "
                       "only the appearance of one. A file that cannot be "
                       "read and a file that requires nothing come back the "
                       "same way, and passing a change on either would be "
                       "approving it because nobody asked."
                       % os.path.relpath(WORKFLOW, ROOT)}

    now = fingerprint(change)
    author = str(change.get("by") or change.get("author") or "").strip()

    seen, missing, failed, stale, unsigned = {}, [], [], [], []
    for one in list(results or []):
        one = getattr(one, "data", one)
        if not isinstance(one, dict):
            return {"passed": False, "refused": "NOT_A_RESULT",
                    "why": "%r is not a check result. Each is a map naming "
                           "the check, whether it passed, and the "
                           "fingerprint of what it ran against." % (one,)}
        name = str(one.get("check") or "").strip()
        if not name:
            return {"passed": False, "refused": "NOT_A_RESULT",
                    "why": "a result names no check, so nothing here knows "
                           "what it is a result OF."}
        seen[name] = one

    for name in must:
        one = seen.get(name)
        if one is None:
            missing.append(name)
            continue
        if not one.get("passed"):
            failed.append({"check": name, "why": one.get("why")})
            continue
        # A CHECK THAT PASSED ON AN EARLIER VERSION IS A CHECK OF
        # SOMETHING ELSE. Recomputed, never taken on trust.
        was = str(one.get("of") or "").strip()
        if not was:
            unsigned.append(name)
        elif was != now:
            stale.append({"check": name, "ranAgainst": was[:16],
                          "isNow": now[:16]})

    # THE SECURITY REVIEW IS ITS OWN AGENT'S ANSWER, carried through.
    security = security_review(change, reviewed=reviewed, secrets=secrets)

    ready = {
        "required": list(must),
        "of": len(must),
        "ran": sorted(seen),
        "missing": missing,
        "failed": failed,
        "stale": stale,
        "unfingerprinted": unsigned,
        "security": security,
        "fingerprint": now,
        "author": author or None,
        "ranAnything": False,
        "fixedAnything": False,
    }

    if missing:
        return dict(ready, passed=False, refused="CHECK_MISSING",
                    why="%d of %d required check%s %s no result: %s. A check "
                        "nobody ran is not a check that passed."
                        % (len(missing), len(must),
                           "" if len(must) == 1 else "s",
                           "has" if len(missing) == 1 else "have",
                           ", ".join(missing)))
    if failed:
        return dict(ready, passed=False, refused="CHECK_FAILED",
                    why="%s did not pass. D-35 - this is refused, not "
                        "warned about."
                        % ", ".join(one["check"] for one in failed))
    if stale:
        return dict(ready, passed=False, refused="STALE_RESULT",
                    why="%s ran against %s... and the change is now %s... A "
                        "check that passed on a previous version of this "
                        "change is a check of something else."
                        % (", ".join(one["check"] for one in stale),
                           stale[0]["ranAgainst"], now[:16]))
    # A RESULT NOTHING CAN TIE TO THIS CHANGE, SITTING BESIDE ONES THAT
    # CAN. Most checks record nothing to compare against, and that
    # concession stands - see `unjudged`. What does not stand is a
    # MIXTURE: once one check in this run recorded a fingerprint, a result
    # without one is a check that did not record rather than a check that
    # cannot, and an old or fabricated {check, passed: true} is
    # indistinguishable from a fresh one in exactly that company.
    if unsigned and len(unsigned) != len(must):
        return dict(ready, passed=False, refused="RESULT_IS_UNFINGERPRINTED",
                    why="%s recorded no fingerprint while %s did. A result "
                        "nothing can tie to this change is not a result "
                        "ABOUT this change, and one sitting beside results "
                        "that name what they ran against is a check that "
                        "did not record rather than one that cannot."
                        % (", ".join(unsigned),
                           ", ".join(name for name in must
                                     if name not in unsigned)))

    if security.get("refused"):
        return dict(ready, passed=False, refused="NOT_SECURITY_REVIEWED",
                    why="HERON-DEV-SEC-009 refused: %s. %s"
                        % (security["refused"], security["why"]))

    who = str(approved_by or "").strip()
    if not who:
        return dict(ready, passed=False, refused="NOBODY_IS_APPROVING",
                    why="every required check passed and nobody is "
                        "approving. A gate reports; it does not approve, "
                        "and `passed` without a name on it is this agent "
                        "approving its own report (Golden Rule 7).")
    if author and who.lower() == author.lower():
        return dict(ready, passed=False, refused="APPROVED_BY_THE_IMPLEMENTER",
                    why="%r wrote the change and %r is approving it. The "
                        "register's row is `never the implementer`, and "
                        "Golden Rule 7 says no agent approves itself."
                        % (author, who))

    return dict(
        ready, passed=True, refused=None, approvedBy=who,
        why="all %d required check%s passed against the change as it "
            "stands, a security review was %s, and %s is approving work "
            "they did not write."
            % (len(must), "" if len(must) == 1 else "s",
               "given" if security.get("reviewed") else "not required here",
               who),
        unjudged=_unjudged(must, security, who, unsigned))


def _unjudged(must, security, who, unsigned=()):
    return [
        "NOTHING WAS RUN HERE. The results were read, not produced. An "
        "agent that runs a check and then decides whether it passed is "
        "marking its own work, which is the whole of `never the "
        "implementer` and of Golden Rule 7.",
        "WHAT MUST PASS IS %s's, PARSED RATHER THAN LISTED - %d check%s: "
        "%s. A list typed here would drift the first time somebody adds a "
        "job, and it would drift silently in the dangerous direction: a "
        "check this agent had never heard of would simply not be required."
        % (os.path.relpath(WORKFLOW, ROOT), len(must),
           "" if len(must) == 1 else "s", ", ".join(must)),
        "WHETHER THE CHANGE IS SAFE IS HERON-DEV-SEC-009's ANSWER, carried "
        "through unaltered - it said %s. Deciding it again here would be a "
        "second answer to one question (Golden Rule 3)."
        % ("a review was required and given" if security.get("reviewed")
           else "no review was required, which is NOT a pass"),
        "WHETHER THE CHANGE IS ANY GOOD. Every check passing means nothing "
        "a machine can see is wrong with it. %s is approving it, and that "
        "is a person's judgement about work they did not write." % who,
        "A PASSING CHECK IS AS OLD AS ITS FINGERPRINT. Each result was "
        "recomputed against the change as it stands. %s"
        % ("EVERY result here recorded one." if not unsigned else
           "%d recorded NONE and %s named in `unfingerprinted`: %s. They "
           "were accepted because refusing them would make this unusable "
           "against any check that does not record what it ran against, "
           "and that is most of them today - but nothing here can tell "
           "such a result from one run against an older change. A result "
           "with no fingerprint ALONGSIDE one that has is a different "
           "matter and is refused."
           % (len(unsigned), "is" if len(unsigned) == 1 else "are",
              ", ".join(unsigned))),
    ]


def main(argv):
    print("THE FINAL GATE   it runs nothing, and that is the row")
    print("=" * 72)

    must = required()
    print("\nwhat CI requires (%s):" % os.path.relpath(WORKFLOW, ROOT))
    for name in must:
        print("  %s" % name)

    change = {"name": "heron_qa", "risk": "MODIFY", "by": "Ajmal"}
    sha = fingerprint(change)
    review = {"by": "Reviewer", "of": sha}
    every = [{"check": name, "passed": True, "of": sha} for name in must]

    print("\npassed")
    said = gate(change, every, approved_by="Reviewer", reviewed=review)
    print("  %s" % said["why"])

    print("\nrefused")
    for label, kw in (
            ("nothing handed in", dict(change=None)),
            ("not a submission", dict(change="a string")),
            ("a result that is not one", dict(change=change,
                                              results=["a string"])),
            ("one check never run", dict(change=change, results=every[:-1],
                                         approved_by="R", reviewed=review)),
            ("one check failed", dict(
                change=change,
                results=[dict(one, passed=False) if one["check"] == must[0]
                         else one for one in every],
                approved_by="R", reviewed=review)),
            ("a result of an older version", dict(
                change=change,
                results=[dict(one, of="deadbeef" * 8) for one in every],
                approved_by="R", reviewed=review)),
            ("no security review", dict(change=change, results=every,
                                        approved_by="R")),
            ("nobody approving", dict(change=change, results=every,
                                      reviewed=review)),
            ("approved by the author", dict(change=change, results=every,
                                            approved_by="ajmal",
                                            reviewed=review)),
            ("nothing required", dict(change=change, results=every,
                                      checks=[], approved_by="R",
                                      reviewed=review))):
        bad = gate(**kw)
        print("  %-26s %-26s %s" % (label, bad["refused"], bad["why"][:30]))

    print("\nwhat this agent does not judge")
    for line in said["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
