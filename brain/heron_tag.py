# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-REL-007
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Releases - signed, versioned, and built from a commit rather than from
whatever the branch says today.

    python brain/heron_tag.py

WHAT IT IS FOR (docs/28, HERON-GIT-REL-007)
--------------------------------------------
"Tags and publishes releases." T1, risk PUBLISH. It publishes nothing -
`allowed-tools` is empty and there is no network. What comes back is a
verdict on whether the tag may be cut.

docs/07 s64 GIVES THREE PROPERTIES, AND ALL THREE ARE CHECKED
---------------------------------------------------------------
    "one documented command that fetches a SIGNED release... what it
    downloads is a VERSIONED RELEASE ARTEFACT - not whatever the default
    branch happens to say today."

  SIGNED             an unsigned artefact is refused, not warned about
  A VERSIONED ARTEFACT  the version is a real one and ahead of the last
  NOT A BRANCH       `built_from` must be a commit. A release pointing
                     at a branch says something different next week
                     while claiming to be the same release, which is the
                     precise failure docs/07 names

THE GATE LIST IS READ, NOT TYPED
----------------------------------
`.github/workflows/gates.yml` is what actually runs on every push, and
it names four. They are read from there, so a fifth gate added to CI
becomes required here with no edit - and if that file cannot be read,
the release is REFUSED rather than cut against a list this file guessed.
A release nobody can say the gates for is the thing gates exist to stop.

THE VERSION IS HERON-GIT-VER-008'S IF IT IS ANYBODY'S
-------------------------------------------------------
Hand in that agent's answer and the tag must equal it. Leave it out and
only the shape and the ordering are checked - and the answer says the
bump was not verified, rather than implying it was.

PUBLISH MEANS A PERSON SAYS SO, FOR THIS VERSION
--------------------------------------------------
The same rule as HERON-GIT-PR-005 and for a stronger reason: a release
is the thing a stranger runs on their own machine. The confirmation
names the version, so it cannot cover the next one, and the machine-word
list is HERON-LRN-PRO-004's.
"""

from __future__ import annotations

import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_promotion as PRO  # noqa: E402
import heron_versioning as VER  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

NOT_A_PERSON = PRO.NOT_A_PERSON

# A commit, not a branch. docs/07 s64's whole point.
A_COMMIT = re.compile(r"^[0-9a-f]{7,40}$")

WORKFLOW = os.path.join(ROOT, ".github", "workflows", "gates.yml")
RUNS = re.compile(r"run:\s*python\s+tools/(check-[a-z-]+)\.py")

A_RELEASE_CARRIES = (
    ("version", "the number a person will quote when something breaks"),
    ("artefact", "the file that is downloaded - docs/07 s64: a versioned "
                 "release artefact, not a branch"),
    ("signature", "docs/07 s64 asks for a SIGNED release, and an unsigned "
                  "one is refused rather than shipped with a note"),
    ("built_from", "the commit it was built from"),
)

A_CONFIRMATION_CARRIES = (
    ("by", "a person's name - Golden Rule 7"),
    ("at", "when"),
    ("version", "THE VERSION BEING CUT. Without it the confirmation "
                "covers this release and the next one"),
)


def _where(path):
    """A path a person can read - relative inside the repo, whole outside."""
    near = os.path.relpath(path, ROOT)
    return path if near.startswith("..") else near


def required_gates(workflow=None):
    """
    The gates CI actually runs, read out of the workflow.

    Read rather than typed so a fifth gate added to CI becomes required
    here with no edit. Returns [] when the file cannot be read, and the
    caller REFUSES on that rather than falling back to a guess.
    """
    path = workflow or WORKFLOW
    try:
        text = io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return []
    seen = []
    for name in RUNS.findall(text):
        if name not in seen:
            seen.append(name)
    return seen


def cut(release, confirmation=None, last=None, bump=None, workflow=None):
    """
    {tagged, may_publish, why} - a verdict. Nothing is tagged or pushed.
    """
    if not release:
        return {"cut": False, "refused": "NOTHING_TO_CUT",
                "why": "no release was handed in."}

    release = getattr(release, "data", release)
    if not isinstance(release, dict):
        return {"cut": False, "refused": "NOT_A_RELEASE",
                "why": "%r is not a release. One carries %s."
                       % (release, ", ".join(field for field, _
                                             in A_RELEASE_CARRIES))}

    missing = [field for field, _ in A_RELEASE_CARRIES
               if not str(release.get(field) or "").strip()]
    if missing and missing != ["signature"]:
        return {"cut": False, "refused": "NOT_A_RELEASE",
                "missing": missing,
                "why": "the release is missing %s. %s"
                       % (", ".join(missing),
                          " ".join(why for field, why in A_RELEASE_CARRIES
                                   if field in missing))}

    version = str(release["version"]).strip().lstrip("v")
    if VER.parse(version) is None:
        return {"cut": False, "refused": "NOT_A_VERSION",
                "why": "'%s' is not a semantic version. docs/17 s8 adopts "
                       "MAJOR.MINOR.PATCH, and a tag nobody can order is a "
                       "tag nobody can tell is newer."
                       % release["version"]}

    if last:
        before = VER.parse(str(last).strip().lstrip("v"))
        if before is None:
            return {"cut": False, "refused": "NOT_A_VERSION",
                    "why": "the last release '%s' is not a semantic "
                           "version, so nothing here can say whether '%s' "
                           "is ahead of it." % (last, version)}
        if VER.parse(version) <= before:
            return {"cut": False, "refused": "NOT_AHEAD",
                    "why": "'%s' is not ahead of '%s'. A release that goes "
                           "backwards leaves two artefacts a person cannot "
                           "order, and the one they install is whichever "
                           "their tooling happens to prefer."
                           % (version, last)}

    if bump:
        bump = getattr(bump, "data", bump)
        wanted = str((bump or {}).get("to") or "").strip()
        if wanted and wanted != version:
            return {"cut": False, "refused": "WRONG_VERSION",
                    "wanted": wanted, "tagging": version,
                    "why": "HERON-GIT-VER-008 answered '%s' for this change "
                           "and the tag says '%s'. One of the two is wrong "
                           "about what changed, and which is not knowable "
                           "from either side alone."
                           % (wanted, version)}

    if not str(release.get("signature") or "").strip():
        return {"cut": False, "refused": "NOT_SIGNED",
                "why": "the artefact is unsigned. docs/07 s64 asks for a "
                       "SIGNED release because the install is one "
                       "documented command a stranger runs on their own "
                       "machine, and a signature is the only thing between "
                       "that command and whatever else answers the URL. "
                       "Refused rather than shipped with a note - D-35."}

    built = str(release["built_from"]).strip()
    if not A_COMMIT.match(built.lower()):
        return {"cut": False, "refused": "NOT_A_COMMIT",
                "why": "'%s' is not a commit. docs/07 s64: what a person "
                       "downloads is a versioned release artefact, 'NOT "
                       "whatever the default branch happens to say today'. "
                       "A release built from a branch says something "
                       "different next week while claiming to be the same "
                       "release." % built}

    wanted_gates = required_gates(workflow)
    if not wanted_gates:
        return {"cut": False, "refused": "NO_GATE_LIST",
                "why": "%s could not be read, so nothing here can say "
                       "which gates a release must pass. A list guessed "
                       "here would be a release cut against this file's "
                       "opinion rather than against what CI runs."
                       % _where(workflow or WORKFLOW)}

    gates = release.get("gates") or {}
    if not isinstance(gates, dict):
        gates = {}
    red = [name for name in wanted_gates if gates.get(name) is not True]
    if red:
        return {"cut": False, "refused": "A_GATE_IS_RED",
                "red": red, "required": wanted_gates,
                "why": "%d of the %d gate(s) CI runs %s not green: %s. A "
                       "release cut on a red gate is the thing gates exist "
                       "to stop, and 'not reported' counts as red - a gate "
                       "nobody ran is not a gate that passed."
                       % (len(red), len(wanted_gates),
                          "is" if len(red) == 1 else "are", ", ".join(red))}

    if not confirmation:
        return {"cut": False, "refused": "NOT_CONFIRMED",
                "asked": "Cut and publish release %s?" % version,
                "why": "a release is the thing a stranger runs on their "
                       "own machine, and docs/28 gives this agent PUBLISH. "
                       "D-35: unapproved is refused, not published with a "
                       "warning attached."}

    confirmation = getattr(confirmation, "data", confirmation)
    if not isinstance(confirmation, dict):
        return {"cut": False, "refused": "NOT_CONFIRMED",
                "why": "%r is not a confirmation. One carries %s."
                       % (confirmation, ", ".join(
                           field for field, _ in A_CONFIRMATION_CARRIES))}

    absent = [field for field, _ in A_CONFIRMATION_CARRIES
              if not str(confirmation.get(field) or "").strip()]
    if absent:
        return {"cut": False, "refused": "NOT_CONFIRMED",
                "missing": absent,
                "why": "the confirmation is missing %s. %s"
                       % (", ".join(absent),
                          " ".join(why for field, why
                                   in A_CONFIRMATION_CARRIES
                                   if field in absent))}

    if not PRO._person(confirmation.get("by")):
        return {"cut": False, "refused": "CONFIRMED_BY_A_MACHINE",
                "why": "'%s' confirmed it, which is not a person. Golden "
                       "Rule 7, and a release is the one artefact where "
                       "nobody downstream can check the decision - they "
                       "run it." % confirmation.get("by")}

    said = str(confirmation["version"]).strip().lstrip("v")
    if said != version:
        return {"cut": False, "refused": "NOT_CONFIRMED",
                "confirmed": said, "cutting": version,
                "why": "the confirmation names '%s' and this cuts '%s'. A "
                       "confirmation that does not name the version covers "
                       "this release and the next one." % (said, version)}

    return {
        "cut": True, "may_publish": True, "version": version,
        "tag": "v%s" % version, "artefact": str(release["artefact"]).strip(),
        "built_from": built, "signed": True,
        "gates": wanted_gates,
        "checked_bump": bool(bump),
        "confirmed_by": str(confirmation["by"]).strip(),
        "confirmed_at": str(confirmation["at"]).strip(),
        "why": "v%s from %s, signed, %d gate(s) green, confirmed by %s for "
               "this version. Prepared, not published."
               % (version, built[:12], len(wanted_gates),
                  confirmation["by"]),
        "unjudged": [
            "NOTHING WAS TAGGED AND NOTHING WAS PUBLISHED. A verdict comes "
            "back. D-01 leaves the network with the host, and "
            "HERON-GIT-MAIN-001 owns repository interaction.",
            "THE %d GATE(S) WERE READ OUT OF %s RATHER THAN LISTED HERE "
            "(%s), so a fifth one added to CI becomes required with no "
            "edit to this file."
            % (len(wanted_gates), _where(workflow or WORKFLOW),
               ", ".join(wanted_gates)),
            "%s" % ("THE VERSION MATCHES HERON-GIT-VER-008'S ANSWER."
                    if bump else
                    "THE BUMP WAS NOT VERIFIED. No answer from "
                    "HERON-GIT-VER-008 was handed in, so only the shape "
                    "and the ordering were checked - that '%s' is a "
                    "version and that it is ahead. Whether it is the RIGHT "
                    "one for what changed was not asked." % version),
            "WHETHER THE ARTEFACT WORKS. Green gates are not an install - "
            "tools/check-package.py says so about itself, and this agent "
            "says it again rather than letting four ticks read as one.",
        ],
    }


def main(argv):
    print("RELEASES   signed, versioned, and built from a commit")
    print("=" * 72)

    gates = required_gates()
    print("\nthe gates CI runs, read out of the workflow")
    for name in gates:
        print("  %s" % name)

    green = dict((name, True) for name in gates)
    good = {"version": "0.2.0", "artefact": "heron-0.2.0.zip",
            "signature": "minisign:RWQ...", "built_from": "0913c3ab4f21",
            "gates": green}
    yes = {"by": "Ajmal", "at": "2026-09-15 10:04", "version": "0.2.0"}

    answer = cut(good, yes, last="0.1.0",
                 bump={"to": "0.2.0", "step": "minor"})
    print("\n%s" % answer["why"])

    print("\nrefused")
    for release, confirmation, kwargs in (
            (None, yes, {}),
            ("a string", yes, {}),
            ({"version": "0.2.0"}, yes, {}),
            (dict(good, version="two"), yes, {}),
            (good, yes, {"last": "0.3.0"}),
            (good, yes, {"bump": {"to": "1.0.0"}}),
            (dict(good, signature=""), yes, {}),
            (dict(good, built_from="main"), yes, {}),
            (good, yes, {"workflow": "/nowhere/gates.yml"}),
            (dict(good, gates={}), yes, {}),
            (good, None, {}),
            (good, dict(yes, by="the release pipeline"), {}),
            (good, dict(yes, version="0.9.9"), {})):
        bad = cut(release, confirmation, **kwargs)
        print("  %-26s %s" % (bad["refused"], bad["why"][:40]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
