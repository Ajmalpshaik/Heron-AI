# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-REP-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Repositories - a remote is a destination, and an undeclared one is a
push to somewhere nobody chose.

    python brain/heron_repository.py

WHAT IT IS FOR (docs/28, HERON-GIT-REP-002)
--------------------------------------------
"Structure, remotes, settings." T1, risk PUBLISH. Three words, and only
one of them turns out to be this agent's.

STRUCTURE IS ALREADY OWNED, AND IS NAMED RATHER THAN RE-DERIVED
-----------------------------------------------------------------
`tools/check-structure.py` carries HERON-WSP-VAL-003 and
HERON-AHR-MON-011, and it enforces D-48's layering in both languages on
every push. A second implementation here would be a second thing that
can be right on its own while disagreeing with the first - the objection
that left HERON-NAM-MET-006 unbuilt (PROPOSALS F17). So the answer names
the owner and stops.

SETTINGS NEED THE API, WHICH THIS PROJECT DOES NOT HAVE
---------------------------------------------------------
Branch protection, merge rules, who may push - all of it lives behind
the GitHub API, and D-01 leaves the network with the host.
`allowed-tools` is empty. Reporting settings from here would mean
reporting what this machine GUESSES they are.

REMOTES ARE THE JOB
---------------------
A remote is where a push goes. Nothing else in this project looks at
them, and two things can be wrong with one:

  IT IS NOT DECLARED. A push goes to whatever the remote points at, and
  a remote nobody declared is a destination nobody chose. That is the
  same class of failure HERON-RPT-RED-003 refuses for a report, arriving
  by a different door - and it is refused, not listed as a curiosity.

  IT CARRIES A CREDENTIAL. `https://user:token@host/...` is how a token
  ends up in `.git/config`, in every clone of a mirrored repository and
  in the output of any command that prints a remote.

THE CREDENTIAL CHECK IS STRUCTURAL AS WELL AS PATTERN-BASED
-------------------------------------------------------------
heron_secrets knows the shapes of the credentials it knows -
`https://x:ghp_...@github.com` is caught. `https://x:hunter2@github.com`
is not, because `hunter2` is not a shape anybody can recognise. So the
URL is ALSO checked for userinfo at all: anything between `://` and `@`
with a colon in it is a credential by construction, whoever issued it.
Measured on 2026-09-15: the pattern list alone misses that case.
"""

from __future__ import annotations

import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_secrets as SECRETS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Anything between :// and @ carrying a colon. A password nobody can
# pattern-match is still a password.
USERINFO = re.compile(r"^[a-z][a-z0-9+.-]*://([^/@]*:[^/@]*)@", re.I)

# Named, never re-derived. PROPOSALS F17's objection.
STRUCTURE_IS_OWNED_BY = (
    ("tools/check-structure.py",
     "HERON-WSP-VAL-003 and HERON-AHR-MON-011 - D-48's layering, in both "
     "languages, on every push"),
)

SETTINGS_NEED = ("the GitHub API, which D-01 leaves with the host. "
                 "allowed-tools is empty here, so anything this agent "
                 "said about branch protection or merge rules would be "
                 "what this machine guesses they are")


def inspect(remotes, declared=(), secrets=None):
    """
    {remotes, structure, settings, why} - or a refusal. Nothing is
    changed and nothing is pushed.
    """
    if not remotes:
        return {"inspected": False, "refused": "NOTHING_TO_CHECK",
                "why": "no remotes were handed in. A repository with no "
                       "remote pushes nowhere, which is a fact about the "
                       "repository rather than an answer about it."}

    seen = {}
    for remote in remotes:
        remote = getattr(remote, "data", remote)
        if not isinstance(remote, dict):
            return {"inspected": False, "refused": "NOT_A_REMOTE",
                    "why": "%r is not a remote. Each is {name, url}."
                           % (remote,)}
        name = str(remote.get("name") or "").strip()
        url = str(remote.get("url") or "").strip()
        if not name or not url:
            return {"inspected": False, "refused": "NOT_A_REMOTE",
                    "why": "a remote is missing its %s. Both are needed: "
                           "the name is what a person types and the url is "
                           "where it actually goes, and they are not the "
                           "same fact."
                           % ("name" if not name else "url")}
        seen[name] = url

    keeper = secrets if secrets is not None else SECRETS.Secrets()
    for name in sorted(seen):
        url = seen[name]
        _, found = keeper.redact(url)
        userinfo = USERINFO.match(url)
        if found or userinfo:
            # THE VALUE IS NOT IN THIS ANSWER, and neither is the URL -
            # the URL IS the credential here.
            return {"inspected": False, "refused": "CARRIES_A_SECRET",
                    "remote": name,
                    "found": sorted(set(found)) or ["a username and "
                                                    "password in the url"],
                    "by": "the pattern list" if found else "its shape",
                    "why": "remote '%s' carries a credential in its url, "
                           "caught by %s. Neither the value nor the url is "
                           "in this answer. A credential there sits in "
                           ".git/config, in every clone of a mirror, and "
                           "in the output of any command that prints a "
                           "remote."
                           % (name, "the pattern list" if found
                              else "its SHAPE - anything between :// and @ "
                                   "with a colon in it is a credential by "
                                   "construction, whoever issued it")}

    wanted = {}
    for one in (declared or []):
        one = getattr(one, "data", one)
        if not isinstance(one, dict):
            continue
        wanted[str(one.get("name") or "").strip()] = \
            str(one.get("url") or "").strip()

    if wanted:
        strangers = sorted(name for name in seen
                           if seen[name] not in wanted.values())
        if strangers:
            return {"inspected": False, "refused": "UNDECLARED_REMOTE",
                    "remotes": strangers,
                    "why": "%s %s not point at any declared destination. A "
                           "push goes to whatever the remote points at, so "
                           "a remote nobody declared is a destination "
                           "nobody chose - refused rather than listed as a "
                           "curiosity."
                           % (", ".join("'%s'" % one for one in strangers),
                              "does" if len(strangers) == 1 else "do")}

    absent = sorted(name for name in wanted
                    if wanted[name] not in seen.values())

    return {
        "inspected": True,
        "remotes": [{"name": name, "url": seen[name],
                     "declared": bool(wanted) and seen[name]
                     in wanted.values()}
                    for name in sorted(seen)],
        "missing": absent, "of": len(seen),
        "structure": {"checked": False,
                      "owned_by": [{"where": where, "what": what}
                                   for where, what in STRUCTURE_IS_OWNED_BY],
                      "why": "not checked here. %s"
                             % STRUCTURE_IS_OWNED_BY[0][1]},
        "settings": {"read": False, "why": "not read. %s" % SETTINGS_NEED},
        "why": "%d remote(s), none carrying a credential%s. Nothing was "
               "changed."
               % (len(seen),
                  " and all pointing at a declared destination" if wanted
                  else " - and nothing was declared to check them against"),
        "unjudged": [
            "%s" % ("NOTHING WAS DECLARED, SO NO REMOTE WAS COMPARED "
                    "AGAINST ANYTHING. Every url here was checked for a "
                    "credential and nothing else - 'not carrying a token' "
                    "is not the same as 'somewhere you meant'."
                    if not wanted else
                    "%d REMOTE(S) WERE CHECKED AGAINST %d DECLARED "
                    "DESTINATION(S)%s." % (
                        len(seen), len(wanted),
                        ", and %d declared one(s) are not configured: %s"
                        % (len(absent), ", ".join(absent)) if absent
                        else ", and every declared one is configured")),
            "THE STRUCTURE. %s" % STRUCTURE_IS_OWNED_BY[0][1] +
            " - named rather than re-derived, because a second "
            "implementation is a second thing that can be right on its own "
            "while disagreeing with the first (PROPOSALS F17).",
            "THE SETTINGS. %s" % SETTINGS_NEED,
            "WHERE ANY OF THESE ACTUALLY GOES. A url was read as text. "
            "Nothing here resolved a host, opened a socket or asked a "
            "server who it was.",
        ],
    }


def main(argv):
    print("REPOSITORIES   a remote is a destination")
    print("=" * 72)

    mine = [{"name": "origin",
             "url": "https://github.com/Ajmalpshaik/Heron-AI.git"}]
    declared = [{"name": "origin",
                 "url": "https://github.com/Ajmalpshaik/Heron-AI.git"}]

    answer = inspect(mine, declared)
    print("\n%s" % answer["why"])
    for one in answer["remotes"]:
        print("  %-8s %-46s declared %s"
              % (one["name"], one["url"], one["declared"]))
    print("\n  structure  %s" % answer["structure"]["why"][:60])
    print("  settings   %s" % answer["settings"]["why"][:60])

    print("\nrefused")
    for remotes, wanted in (
            ([], []),
            (["a string"], []),
            ([{"name": "origin"}], []),
            ([{"name": "origin",
               "url": "https://a:ghp_" + "A" * 36 + "@github.com/x.git"}],
             []),
            ([{"name": "origin",
               "url": "https://ajmal:hunter2@github.com/x.git"}], []),
            (mine + [{"name": "mirror",
                      "url": "https://example.invalid/copy.git"}],
             declared)):
        bad = inspect(remotes, wanted)
        print("  %-22s %s" % (bad["refused"], bad["why"][:44]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
