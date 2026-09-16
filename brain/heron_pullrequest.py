# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-PR-005
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Pull requests - explicit confirmation, every time, and every time means
this one.

    python brain/heron_pullrequest.py

WHAT IT IS FOR (docs/28, HERON-GIT-PR-005)
-------------------------------------------
"Opens PRs - EXPLICIT CONFIRMATION, EVERY TIME." T1, risk PUBLISH. The
bold half of that row is the entire agent; the rest is a title and a
body.

"EVERY TIME" MEANS A STANDING APPROVAL IS NOT ONE
---------------------------------------------------
A confirmation that does not name what is being opened would cover the
next one too, and the one after that. So a confirmation must carry the
HEAD it is confirming, and it is checked against the head actually being
opened. "Yes, go ahead" is not a confirmation of anything in particular,
which is the whole reason the register put "every time" in bold.

Nothing here remembers a confirmation, either. There is no store, so
there is nothing that could be reused - the confirmation arrives with
the request or the request is refused.

A MACHINE MAY NOT CONFIRM
---------------------------
Golden Rule 7: no agent approves itself. The machine-word list is
HERON-LRN-PRO-004's, imported rather than copied, because a second copy
is a second chance for one of them to learn a new word and the other not
to.

A PULL REQUEST IS AN EGRESS
-----------------------------
Whatever a repository is, it is somewhere else. So the same two rules
HERON-RPT-RED-003 applies to a report apply here, and for the same
reasons:

  A Revit file by NAME ALONE is refused. D-26 - the line is the FILE,
  not the information. The extension list is heron_release's.

  A body carrying a credential is refused WHOLE, never stripped. The
  detection is heron_secrets' redact(), and the value it found never
  appears in this answer - only that something did.

Stripping would be worse than refusing. A stripped body is a body
somebody is about to publish believing it is clean, and the one thing
they cannot see is what was taken out of it.

IT PREPARES, IT DOES NOT OPEN
-------------------------------
`allowed-tools` is empty and nothing here has a network. What comes back
is a pull request ready to open and a verdict on whether it may be -
D-01 leaves the network with the host, and HERON-GIT-MAIN-001 owns
repository interaction.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_promotion as PRO  # noqa: E402
import heron_release as RELEASE  # noqa: E402
import heron_security as SEC  # noqa: E402
import heron_secrets as SECRETS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# heron_release's, which is D-26's left-hand column. Imported so there is
# one list rather than two that agree today.
NEVER_LEAVES = RELEASE.NEVER_LEAVES

# HERON-LRN-PRO-004's. The underscore is crossed deliberately: one rule,
# one owner, and a second copy is a second chance for one of them to
# learn a new word and the other not to.
NOT_A_PERSON = PRO.NOT_A_PERSON

A_PULL_REQUEST_CARRIES = (
    ("title", "what a reviewer sees in a list of twenty"),
    ("body", "what the change is for - an empty one asks a reviewer to "
             "read the diff and guess"),
    ("head", "the branch being merged"),
    ("base", "the branch it is merged into"),
)

A_CONFIRMATION_CARRIES = (
    ("by", "a person's name. A machine confirming its own work is Golden "
           "Rule 7 with the sign painted over"),
    ("at", "when. A confirmation with no time cannot be shown to have "
           "come before the thing it confirms"),
    ("head", "THE HEAD BEING CONFIRMED. Without it the confirmation "
             "covers this pull request and the next one and the one "
             "after that, which is what 'every time' rules out"),
    ("of", "THE FINGERPRINT OF WHAT WAS CONFIRMED. A branch name is "
           "mutable: the same name carries new commits, a replaced title "
           "and a different base, so a confirmation naming only the head "
           "authorises whatever that name comes to mean afterwards"),
)

# What a confirmation is a confirmation OF. The branch NAME is in here
# because it is part of what somebody agreed to, and it is not enough on
# its own - a name is a label on a moving thing.
A_CONFIRMATION_IS_OF = ("title", "body", "head", "base", "draft",
                        "attachments")


def fingerprint_of(request):
    """
    What a person is confirming when they confirm this pull request.

    HERON-DEV-SEC-009's canonical fingerprint, bound rather than a second
    way of hashing one thing (D-30's own argument). Only the fields a
    reviewer actually agreed to go in, so re-running with the same
    request gives the same answer and changing any of them does not.
    """
    request = getattr(request, "data", request)
    if not isinstance(request, dict):
        return None
    of = {}
    for field in A_CONFIRMATION_IS_OF:
        if field == "draft":
            of[field] = bool(request.get("draft", True))
        elif field == "attachments":
            of[field] = sorted(str(one) for one
                               in (request.get("attachments") or []))
        else:
            of[field] = str(request.get(field) or "").strip()
    return SEC.fingerprint(of)


def open_request(request, confirmation=None, secrets=None):
    """
    {prepared, may_open, why} - a pull request and a verdict. Nothing is
    opened and no network is touched.
    """
    if not request:
        return {"prepared": False, "refused": "NOTHING_TO_OPEN",
                "why": "no pull request was handed in."}

    request = getattr(request, "data", request)
    if not isinstance(request, dict):
        return {"prepared": False, "refused": "NOT_A_PULL_REQUEST",
                "why": "%r is not a pull request. One carries %s."
                       % (request, ", ".join(field for field, _ in
                                             A_PULL_REQUEST_CARRIES))}

    missing = [field for field, _ in A_PULL_REQUEST_CARRIES
               if not str(request.get(field) or "").strip()]
    if missing:
        return {"prepared": False, "refused": "NOT_A_PULL_REQUEST",
                "missing": missing,
                "why": "the pull request is missing %s. %s"
                       % (", ".join(missing),
                          " ".join(why for field, why
                                   in A_PULL_REQUEST_CARRIES
                                   if field in missing))}

    head = str(request["head"]).strip()
    base = str(request["base"]).strip()
    if head == base:
        return {"prepared": False, "refused": "SAME_BRANCH",
                "why": "'%s' is both the head and the base. A pull request "
                       "from a branch into itself has no diff, and opening "
                       "one says a change was proposed when none was."
                       % head}

    # THE EGRESS CHECKS COME FIRST, before any question of who confirmed.
    # A confirmed pull request carrying a credential is still a leak, and
    # asking somebody to confirm it would be asking them to authorise one.
    binaries = RELEASE._binaries(request.get("attachments") or [])
    if binaries:
        return {"prepared": False, "refused": "CARRIES_A_MODEL",
                "attachments": [name for name, _ in binaries],
                "why": "%s. D-26 draws the line at the FILE, not at the "
                       "information in it - a model is refused by its name "
                       "alone, before anybody asks what is inside. Project "
                       "names, file names and element data all travel "
                       "fine; the model does not."
                       % "; ".join("'%s' is a %s" % (name, extension)
                                   for name, extension in binaries)}

    keeper = secrets if secrets is not None else SECRETS.Secrets()
    for field in ("title", "body"):
        _, found = keeper.redact(str(request.get(field) or ""))
        if found:
            # THE VALUE IS NOT IN THIS ANSWER. Only that something was.
            return {"prepared": False, "refused": "CARRIES_A_SECRET",
                    "where": field, "found": sorted(set(found)),
                    "why": "the %s carries %s. It is refused WHOLE rather "
                           "than stripped: a stripped body is one somebody "
                           "publishes believing it is clean, and the one "
                           "thing they cannot see is what was taken out. "
                           "The value is not in this answer either - only "
                           "that it was there."
                           % (field, ", ".join(sorted(set(found))))}

    if not confirmation:
        return {"prepared": False, "refused": "NOT_CONFIRMED",
                "asked": "Open a pull request from %s into %s?" % (head, base),
                "why": "docs/28 requires explicit confirmation EVERY TIME, "
                       "and none came with this one. D-35: unapproved is "
                       "refused, not opened with a warning attached."}

    confirmation = getattr(confirmation, "data", confirmation)
    if not isinstance(confirmation, dict):
        return {"prepared": False, "refused": "NOT_CONFIRMED",
                "why": "%r is not a confirmation. One carries %s."
                       % (confirmation, ", ".join(
                           field for field, _ in A_CONFIRMATION_CARRIES))}

    absent = [field for field, _ in A_CONFIRMATION_CARRIES
              if not str(confirmation.get(field) or "").strip()]
    if absent:
        return {"prepared": False, "refused": "NOT_CONFIRMED",
                "missing": absent,
                "why": "the confirmation is missing %s. %s"
                       % (", ".join(absent),
                          " ".join(why for field, why
                                   in A_CONFIRMATION_CARRIES
                                   if field in absent))}

    if not PRO._person(confirmation.get("by")):
        return {"prepared": False, "refused": "CONFIRMED_BY_A_MACHINE",
                "why": "'%s' confirmed it, which is not a person. Golden "
                       "Rule 7: no agent approves itself, and a machine "
                       "confirming a machine's pull request is that rule "
                       "with the sign painted over."
                       % confirmation.get("by")}

    said = str(confirmation["head"]).strip()
    if said != head:
        return {"prepared": False, "refused": "CONFIRMATION_IS_STANDING",
                "confirmed": said, "opening": head,
                "why": "the confirmation names '%s' and this pull request "
                       "opens '%s'. A confirmation that does not name what "
                       "it is confirming covers this one and the next one "
                       "too, which is exactly what 'every time' rules out."
                       % (said, head)}

    # AND THE NAME IS NOT THE THING. A branch is mutable: the same head
    # carries new commits tomorrow, and the title and base can be
    # replaced under a confirmation that named neither. This is the
    # heron_apply fix - an approval that does not name what it approved
    # covers everything - applied to the second place a name stood in for
    # the content.
    now = fingerprint_of(request)
    was = str(confirmation["of"]).strip()
    if was != now:
        return {"prepared": False, "refused": "CONFIRMATION_IS_STALE",
                "confirmed": was[:16], "is_now": now[:16],
                "head": head,
                "why": "the confirmation covers %s... and this pull "
                       "request is %s... Somebody confirmed something on "
                       "'%s', and it was not this - %s can all change "
                       "while the branch name stays the same, so a "
                       "confirmation naming only the head is a standing "
                       "one wearing a specific one's coat."
                       % (was[:12], now[:12], head,
                          ", ".join(A_CONFIRMATION_IS_OF))}

    return {
        "prepared": True, "may_open": True,
        "head": head, "base": base,
        "title": str(request["title"]).strip(),
        "body": str(request["body"]),
        "draft": bool(request.get("draft", True)),
        "confirmed_by": str(confirmation["by"]).strip(),
        "confirmed_at": str(confirmation["at"]).strip(),
        "why": "'%s' -> '%s', confirmed by %s at %s for this head and no "
               "other. Prepared, not opened."
               % (head, base, confirmation["by"], confirmation["at"]),
        "unjudged": [
            "NOTHING WAS OPENED AND NO NETWORK WAS TOUCHED. A pull request "
            "and a verdict come back. D-01 leaves the network with the "
            "host, and HERON-GIT-MAIN-001 owns repository interaction.",
            "THE CONFIRMATION NAMED THIS HEAD ('%s') AND THIS CONTENT "
            "(%s...), AND WOULD NOT COVER ANOTHER. Nothing here stores "
            "it, so there is nothing that could be reused - it arrives "
            "with the request or the request is refused. The fingerprint "
            "covers %s, so a new commit under the same branch name does "
            "not ride in on it."
            % (head, fingerprint_of(request)[:12],
               ", ".join(A_CONFIRMATION_IS_OF)),
            "WHETHER THE CHANGE IS ANY GOOD, or whether %s understood what "
            "they were confirming. A person's name is what makes a later "
            "reader able to ASK them, which is the whole reason it is a "
            "name rather than a tick." % confirmation["by"],
            "WHAT IS IN THE DIFF. The title and body were read for "
            "credentials and the attachments for Revit files; nothing here "
            "looked at the change itself. HERON-GIT-COM-010 is the row "
            "that asks for per-item review of the actual payload.",
        ],
    }


def main(argv):
    print("PULL REQUESTS   explicit confirmation, and every time means this "
          "one")
    print("=" * 72)

    good = {"title": "Two agents", "body": "What they do and why.",
            "head": "claude/two-agents", "base": "main"}
    yes = {"by": "Ajmal", "at": "2026-09-15 09:12",
           "head": "claude/two-agents"}

    answer = open_request(good, yes)
    print("\n%s" % answer["why"])

    print("\nrefused")
    for request, confirmation in (
            (None, yes),
            ("a string", yes),
            ({"title": "x"}, yes),
            (dict(good, base="claude/two-agents"), yes),
            (dict(good, attachments=["Tower A.rvt"]), yes),
            (dict(good, body="the token is ghp_" + "A" * 36), yes),
            (good, None),
            (good, {"by": "Ajmal", "at": "now"}),
            (good, dict(yes, by="ci")),
            (good, dict(yes, head="claude/something-else"))):
        bad = open_request(request, confirmation)
        print("  %-26s %s" % (bad["refused"], bad["why"][:40]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
