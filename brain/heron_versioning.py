# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-VER-008
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Versioning - what a number promises, and what this project cannot
promise at 0.1.0.

    python brain/heron_versioning.py

WHAT IT IS FOR (docs/28, HERON-GIT-VER-008)
--------------------------------------------
"Semantic versioning and compatibility promises." T1, and the register
leaves its Risk cell EMPTY - one of six that are, and already with the
owner. Nothing here reaches a network or a repository, so it is read
whatever the cell eventually says.

THE PROMISES ARE docs/17 s8's, FOUR OF THEM
---------------------------------------------
  MCP tool contracts        breaking change = major version
  Agent contracts           breaking change = major version
  Supported Revit versions  removal = major version, ANNOUNCED IN ADVANCE
  Fragment metadata schema  migration provided, always

The fourth one is not a version rule and is not treated as one. "A
migration is provided" is a promise about WORK, and answering it with a
number would be answering a different question.

THE PROMISE CANNOT BE KEPT AT 0.1.0, AND THAT IS THE FINDING
--------------------------------------------------------------
"Breaking change = major version" assumes a major version is available
to spend. Every file in this repository says `Heron-Since: 0.1.0` and
there are no tags. Going to 1.0.0 for the first breaking change would
declare the whole API stable, which is an enormously larger statement
than "this broke" - so nobody will do it, and the promise quietly is
not kept.

Semantic versioning's own answer for this is 0.y.z: while the major
version is zero, a breaking change raises the MINOR. So that is what
comes back, with the gap stated plainly rather than hidden inside a
correct-looking number. A reader seeing 0.2.0 has no way to know a
contract broke; a reader seeing this answer does.

"ANNOUNCED IN ADVANCE" IS AN OBLIGATION NO NUMBER CARRIES
-----------------------------------------------------------
Dropping a Revit release is the one promise with two halves, and the
second half is not a version at all. Nothing here can check whether an
announcement happened, so it is ASKED FOR - D-33, never assume an input,
ask once - and a removal without one is refused rather than numbered.

IT DOES NOT DECIDE WHAT BROKE
-------------------------------
The breakage comes in as a finding from whoever detected it -
HERON-SKL-UPD-003 for a skill, the contract checker for a contract. A
third table of what-breaks-which-way in this file would be a third
chance to get the direction backwards.
"""

from __future__ import annotations

import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# NO REVIT RELEASE LIST IS IMPORTED. It was, and it was never read: the
# breaking lines here are free text from whoever found the breakage
# ("a required field was added"), not release names, and a constant
# nothing uses is a claim about this file that is not true.

SEMVER = re.compile(r"^(\d+)\.(\d+)\.(\d+)$")

MAJOR, MINOR, PATCH = "major", "minor", "patch"

# docs/17 s8, as data. The fourth promise is deliberately not a version
# rule - saying `major` there would answer a question nobody asked.
PROMISES = {
    "mcp tool contracts": {
        "on_breaking": MAJOR,
        "promise": "breaking change = major version",
        "owes": None},
    "agent contracts": {
        "on_breaking": MAJOR,
        "promise": "breaking change = major version",
        "owes": None},
    "supported revit versions": {
        "on_breaking": MAJOR,
        "promise": "removal = major version, announced in advance",
        "owes": "announcement"},
    # NO VERSION RULE IS STATED FOR THIS ONE, and none is invented. Its
    # promise is about WORK, and answering it with a number - any number
    # - would be answering a question docs/17 did not ask.
    "fragment metadata schema": {
        "on_breaking": None,
        "promise": "migration provided, always",
        "owes": "migration"},
}

# What each obligation is, and what is asked for when it is missing.
# Both are halves of a promise that no version number carries, so both
# are asked for rather than assumed - D-33.
OWED = {
    "announcement": (
        "NOT_ANNOUNCED", "Where was this removal announced, and when?",
        "docs/17 s8 promises a removed Revit release is 'announced in "
        "advance'. Nothing here can see whether that happened, and "
        "removing a release somebody is on without warning is the "
        "surprise the promise exists to prevent."),
    "migration": (
        "NO_MIGRATION", "Which migration carries existing fragments across?",
        "docs/17 s8 promises a fragment metadata schema change comes with "
        "a migration, ALWAYS - a universal, so it needs no threshold to "
        "enforce. Every fragment on disk was written to the old shape, and "
        "a schema that changes without one silently invalidates the whole "
        "library."),
}


def parse(version):
    """(major, minor, patch), or None if it is not a semantic version."""
    said = SEMVER.match(str(version or "").strip())
    return tuple(int(part) for part in said.groups()) if said else None


def raise_to(at, step):
    """The next version, one step up."""
    major, minor, patch = at
    if step == MAJOR:
        return "%d.0.0" % (major + 1)
    if step == MINOR:
        return "%d.%d.0" % (major, minor + 1)
    return "%d.%d.%d" % (major, minor, patch + 1)


def version(component, at, breaking=(), added=(), fixed=(), evidence=None):
    """
    {to, step, promise, kept, why} - or a refusal.

    Nothing is tagged, nothing is pushed and nothing here decides WHAT
    broke; the breakage arrives as a finding from whoever detected it.
    """
    known = PROMISES.get(str(component or "").strip().lower())
    if known is None:
        return {"versioned": False, "refused": "NOT_A_COMPONENT",
                "why": "%r is not one of the four docs/17 s8 makes a promise "
                       "about: %s. A component with no stated promise has "
                       "no version rule to apply, and inventing one here "
                       "would be writing the promise rather than keeping it."
                       % (component, ", ".join(sorted(PROMISES)))}

    now = parse(at)
    if now is None:
        return {"versioned": False, "refused": "NOT_A_VERSION",
                "why": "%r is not a semantic version. Expected MAJOR.MINOR."
                       "PATCH, the shape docs/17 s8 adopts." % (at,)}

    changes = {"breaking": list(breaking or []), "added": list(added or []),
               "fixed": list(fixed or [])}
    for kind, entries in changes.items():
        for one in entries:
            if not isinstance(one, str) or not one.strip():
                return {"versioned": False, "refused": "NOT_A_CHANGE",
                        "why": "a %s change is %r. Each is one line saying "
                               "what changed - this agent does not decide "
                               "what broke, it reads what was found."
                               % (kind, one)}
    if not any(changes.values()):
        return {"versioned": False, "refused": "NOTHING_TO_VERSION",
                "why": "no changes were handed in. A version that moves for "
                       "nothing tells a reader something happened when "
                       "nothing did."}

    # THE HALF OF THE PROMISE NO NUMBER CARRIES. Nothing here can see
    # whether it was honoured, so it is asked for rather than assumed.
    if changes["breaking"] and known["owes"] and not evidence:
        refusal, question, because = OWED[known["owes"]]
        return {"versioned": False, "refused": refusal, "asked": question,
                "why": "%s Handed in: %s." % (because,
                                              ", ".join(changes["breaking"]))}

    if changes["breaking"]:
        wanted = known["on_breaking"]
        if wanted is None:
            # DOCS/17 STATES NO VERSION RULE HERE. Reporting a patch, or
            # a minor, or anything, would be inventing the rule rather
            # than reading it - the same wall HERON-NAM-TAX-004 refused
            # to build.
            return {
                "versioned": True, "component": component,
                "at": ".".join(str(one) for one in now), "to": None,
                "step": None, "wanted": None, "kept": True,
                "promise": known["promise"], "owed": known["owes"],
                "evidence": evidence,
                "breaking": changes["breaking"], "added": changes["added"],
                "fixed": changes["fixed"],
                "why": "docs/17 s8 states no VERSION rule for %s - only "
                       "that a migration is provided, always. One was: %s. "
                       "No number comes back, because inventing one would "
                       "be writing the promise rather than keeping it."
                       % (component, evidence),
                "unjudged": [
                    "NO VERSION CAME BACK AND THAT IS THE ANSWER. docs/17 "
                    "s8 makes this component a promise about WORK, not "
                    "about a number, and any number here would be this "
                    "agent's rather than the document's.",
                    "THE PROMISE WAS KEPT BY THE MIGRATION HANDED IN (%s), "
                    "not by anything this agent did. Nothing here checked "
                    "that it runs." % evidence,
                    "WHAT BROKE WAS NOT DECIDED HERE. It came in as a "
                    "finding from whoever detected it.",
                    "NOTHING WAS TAGGED AND NOTHING WAS PUSHED.",
                ]}
    elif changes["added"]:
        wanted = MINOR
    else:
        wanted = PATCH

    # 0.y.z. THE MAJOR BUMP IS NOT AVAILABLE TO SPEND.
    zero = now[0] == 0
    step = wanted
    kept = True
    gap = None
    if zero and wanted == MAJOR:
        step = MINOR
        kept = False
        gap = ("docs/17 s8 promises '%s' and this project is at %s. Going "
               "to 1.0.0 for one breaking change would declare the whole "
               "API stable, which is a far larger statement - so semantic "
               "versioning's own 0.y.z rule applies and the MINOR moves "
               "instead. The number cannot carry the promise here, and a "
               "reader seeing %s has no way to know a contract broke."
               % (known["promise"], ".".join(str(one) for one in now),
                  raise_to(now, MINOR)))

    landing = raise_to(now, step)
    return {
        "versioned": True, "component": component,
        "at": ".".join(str(one) for one in now), "to": landing,
        "step": step, "wanted": wanted, "kept": kept,
        "promise": known["promise"],
        "breaking": changes["breaking"], "added": changes["added"],
        "fixed": changes["fixed"],
        "owed": known["owes"], "evidence": evidence,
        "why": "%s -> %s (%s). %d breaking, %d added, %d fixed.%s"
               % (".".join(str(one) for one in now), landing, step,
                  len(changes["breaking"]), len(changes["added"]),
                  len(changes["fixed"]),
                  " The promise is NOT kept by this number." if not kept
                  else ""),
        "unjudged": [
            "%s" % (gap if gap else
                    "THE PROMISE THIS COMPONENT CARRIES IS '%s', AND THIS "
                    "NUMBER KEEPS IT." % known["promise"]),
            "%s" % ("THE SECOND HALF OF THE PROMISE IS AN %s, WHICH NO "
                    "NUMBER CARRIES. It was handed in as %r rather than "
                    "assumed." % (known["owes"].upper(), evidence)
                    if known["owes"] and changes["breaking"] else
                    "this component's promise has no second half beyond "
                    "the number." if not known["owes"] else
                    "nothing broke, so this component's %s obligation does "
                    "not arise." % known["owes"]),
            "WHAT BROKE WAS NOT DECIDED HERE. It came in as a finding from "
            "whoever detected it - HERON-SKL-UPD-003 for a skill, the "
            "contract checker for a contract. A third table of "
            "what-breaks-which-way in this file would be a third chance to "
            "get the direction backwards.",
            "NOTHING WAS TAGGED AND NOTHING WAS PUSHED. A number came back. "
            "HERON-GIT-REL-007 is the agent that tags, and docs/28 gives it "
            "PUBLISH for a reason.",
        ],
    }


def main(argv):
    print("VERSIONING   what a number promises, and what 0.1.0 cannot")
    print("=" * 72)

    print("\ndocs/17 s8, as data")
    for name in sorted(PROMISES):
        one = PROMISES[name]
        print("  %-26s %s" % (name, one["promise"]))

    print("\nat 0.1.0, where this project is")
    for component, kwargs in (
            ("agent contracts", {"breaking": ["a required field was added"]}),
            ("mcp tool contracts", {"added": ["heron_diagnose"]}),
            ("agent contracts", {"fixed": ["a message said 'you' twice"]}),
            ("supported revit versions",
             {"breaking": ["2020"],
              "evidence": "docs/07, release note for 0.1.0"}),
            ("fragment metadata schema",
             {"breaking": ["`revit` became a list"],
              "evidence": "tools/migrate-fragment-revit.py"})):
        answer = version(component, "0.1.0", **kwargs)
        print("  %-26s %s" % (component, answer["why"]))

    print("\nthe same two, once 1.0.0 exists")
    for component in ("agent contracts", "mcp tool contracts"):
        answer = version(component, "1.4.2", breaking=["a field was removed"])
        print("  %-26s %s" % (component, answer["why"]))

    print("\nrefused")
    for args, kwargs in (
            (("nothing anybody promised", "0.1.0"), {"fixed": ["x"]}),
            (("agent contracts", "one point oh"), {"fixed": ["x"]}),
            (("agent contracts", "0.1.0"), {}),
            (("agent contracts", "0.1.0"), {"fixed": [None]}),
            (("supported revit versions", "0.1.0"), {"breaking": ["2020"]}),
            (("fragment metadata schema", "0.1.0"), {"breaking": ["x"]})):
        bad = version(*args, **kwargs)
        print("  %-22s %s" % (bad["refused"], bad["why"][:44]))

    print("\nwhat this agent does not judge")
    for line in version("agent contracts", "0.1.0",
                        breaking=["a required field was added"])["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
