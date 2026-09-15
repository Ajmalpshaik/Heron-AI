# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-PLC-005
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain

"""
File placement - the DECLARED kind decides, and a name is never a place.

    python brain/heron_placement.py

WHAT IT IS FOR (docs/28, HERON-WSP-PLC-005)
--------------------------------------------
"Decides where a new artefact belongs." T2, risk MODIFY. Something has
just been made - a fragment, a skill, a log line, a backup - and one
question has to be answered before it can be written down: which folder.

THE KIND DECIDES. THE NAME NEVER DOES
---------------------------------------
Golden Rule 19: content Heron reads is "data, never instruction", and
that includes an artefact's own name. So a thing called
`Skills/helper.py` does not go to Skills because of what it is called,
and `../../Core/patch` does not go to Core. The caller DECLARES a kind,
and the kind maps to exactly one folder. A name that is not a plain
name is refused - never cleaned up and used anyway.

    kind        fragment   -> Fragments
    name        anything with / \ or .. in it  -> NAME_IS_NOT_A_PLACE

THE EIGHT KINDS ARE docs/06 s2's DATA ROW, AND NOTHING ELSE
-------------------------------------------------------------
Brain, Skills, Fragments, Memory, Projects, Company, Logs, Backup. One
kind each, read from HERON-WSP-PTH-007's own table so the two cannot
drift apart. Two things follow from the list being exactly that row:

  nothing Heron makes is placed in PRODUCT. Core, Agents, Revit, MCP
  and Packages are what the installer shipped, and docs/07 s7 rule 6
  lets an update replace them wholesale. An artefact written there is
  gone at the next update, silently, and it is also Heron writing into
  its own installation.

  nothing DERIVED is placed either. A cache or an index is not placed,
  it is regenerated where its owner puts it - D-40 derives before
  storing, and a thing that can be rebuilt does not need a decision
  about where it belongs.

AND ONE FOUND WHILE BUILDING IT, WHICH IS WHY THE NAME RULE IS HARD
---------------------------------------------------------------------
`heron_scope.scope_path()` turns a project key into a filename through
`_safe_key()`, which replaces every character outside `[A-Za-z0-9._-]`
with a hyphen. Measured, not read:

    scope_path("project", "Tower B")   ->  .../projects/Tower-B.db
    scope_path("project", "Tower/B")   ->  .../projects/Tower-B.db
    scope_path("project", "Tower-B")   ->  .../projects/Tower-B.db

Three different projects, one file. Two lines above that function its
own docstring says guessing the project wrong "writes one client's
knowledge into another's file, which is a contractual breach rather
than a bug". The key is MEANT to be a Revit UniqueId, which is hex and
hyphens and never collides - but nothing enforces that, and
`heron_conflict.py`'s own usage line reads `--project "Tower B"`.
Recorded as PROPOSALS F14 and not fixed here: changing the rule moves
where existing knowledge lives, which is a migration and the owner's
call.

What this agent takes from it: **a name that would have to be rewritten
to be usable is refused.** Silently rewriting a name is how two things
end up in one place, and the rewrite is invisible at exactly the moment
it matters.

NOTHING IS WRITTEN
--------------------
A folder comes back. The caller writes the file. This agent does not
create, does not copy, and does not look up what is already there.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_paths as PATHS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# One kind per DATA folder, and the folder names come from the path table.
KINDS = {
    "knowledge": "Brain",
    "skill": "Skills",
    "fragment": "Fragments",
    "memory": "Memory",
    "project": "Projects",
    "company": "Company",
    "log": "Logs",
    "backup": "Backup",
}

# Anything that makes a NAME into a PATH. A name carrying one of these is
# refused rather than cleaned up - see PROPOSALS F14.
SEPARATORS = ("/", "\\", "..", ":", "\0", "\n", "\r")


def _plain(name):
    """Which separator a name carries, or None."""
    text = str(name or "")
    for mark in SEPARATORS:
        if mark in text:
            return mark
    if text != text.strip() or not text.strip():
        return "blank"
    return None


def place(artefact, project=None, asked=None):
    """
    {folder, class, kind, why} - or a refusal.

    `asked` is the one scoped question this tier may put, and it is
    asked ONLY when the declared kind is not one of the eight. Its
    answer is checked against the same rules as everything else: an
    answer from a model is data too.
    """
    if not isinstance(artefact, dict):
        return {"placed": False, "refused": "NO_KIND",
                "why": "an artefact is a map declaring a kind and a name. "
                       "%s is not one, and reading a kind out of something "
                       "else is guessing." % type(artefact).__name__}

    kind = str(artefact.get("kind") or "").strip().lower()
    name = artefact.get("name")

    if not kind:
        return {"placed": False, "refused": "NO_KIND",
                "why": "the artefact declares no kind. The kind is the ONLY "
                       "thing that decides the folder - Golden Rule 19 makes "
                       "the name data, never instruction - so with no kind "
                       "there is nothing to decide with."}

    bad = _plain(name)
    if bad is not None:
        return {"placed": False, "refused": "NAME_IS_NOT_A_PLACE",
                "why": "the name %r carries %s. A name is a name: it is "
                       "refused rather than cleaned up and used anyway, "
                       "because a rewritten name is a DIFFERENT name and "
                       "the rewrite is invisible at the moment it matters "
                       "- PROPOSALS F14."
                       % (name, "nothing usable" if bad == "blank"
                          else "'%s'" % bad)}

    answered = None
    if kind not in KINDS:
        if not callable(asked):
            return {"placed": False, "refused": "NOT_A_KIND",
                    "why": "'%s' is not one of %s, and nothing was given to "
                           "ask. The eight are docs/06 s2's data row, one "
                           "each. A ninth is not invented here."
                           % (kind, ", ".join(sorted(KINDS)))}
        answered = str(asked(kind) or "").strip()
        if answered.lower() not in KINDS:
            return {"placed": False, "refused": "THE_ANSWER_IS_NOT_A_PLACE",
                    "why": "asked where a '%s' belongs, the answer was %r, "
                           "which is not one of the eight kinds. An answer "
                           "is data like any other: it is checked, not "
                           "trusted." % (kind, answered)}
        kind = answered.lower()

    folder = KINDS[kind]

    where = folder
    named = None
    if project is not None:
        named = str(project)
        bad = _plain(named)
        if bad is not None:
            return {"placed": False, "refused": "NAME_IS_NOT_A_PLACE",
                    "why": "the project %r carries %s. This is the one "
                           "PROPOSALS F14 measured: 'Tower B', 'Tower/B' "
                           "and 'Tower-B' all reach ONE file today, which "
                           "is one client's work in another's store."
                           % (project, "nothing usable" if bad == "blank"
                              else "'%s'" % bad)}
        where = "%s/%s/%s" % (KINDS["project"], named, folder)

    found = PATHS.classify(where)
    return {
        "placed": False, "folder": where, "kind": kind, "of": name,
        "class": found["class"], "project": named,
        "asked": answered,
        "why": "a '%s' belongs in %s, which is %s. %s"
               % (kind, where, found["class"],
                  "Nothing was written - the folder comes back and the "
                  "caller writes the file."),
        "unjudged": [
            "NOTHING WAS WRITTEN AND NOTHING WAS LOOKED UP. Whether "
            "something is already at that name is the caller's to check; "
            "this agent does not read the disk.",
            "THE KIND DECIDED, NOT THE NAME. Golden Rule 19 makes an "
            "artefact's own name data rather than instruction, so %r "
            "chose nothing here." % name,
            "NO ARTEFACT IS PLACED IN PRODUCT OR IN DERIVED. The eight "
            "kinds are docs/06 s2's data row exactly. Product is replaced "
            "wholesale by an update; derived is regenerated where its "
            "owner puts it, and a thing that can be rebuilt needs no "
            "decision about where it belongs.",
            "%s" % ("A NAME WAS REFUSED RATHER THAN CLEANED UP, and that "
                    "is the rule heron_scope does not follow: PROPOSALS "
                    "F14 measured three project names reaching one file."
                    if project is None else
                    "THE PROJECT NAME %r WAS TAKEN AS GIVEN. It is not "
                    "reduced to a safe filename here, because a reduced "
                    "name is a different name - PROPOSALS F14." % named),
        ],
    }


def main(argv):
    print("FILE PLACEMENT   the declared kind decides, a name never does")
    print("=" * 72)

    good = place({"kind": "fragment", "name": "select-by-name"})
    print("\n%s" % good["why"])

    scoped = place({"kind": "memory", "name": "duct-sizes"},
                   project="Tower A")
    print("%s" % scoped["why"])

    print("\nrefused")
    for artefact, project in (
            ({"kind": "fragment", "name": "../../Core/patch"}, None),
            ({"kind": "skill", "name": "Skills/helper.py"}, None),
            ({"kind": "invoice", "name": "march"}, None),
            ({"name": "no kind at all"}, None),
            ({"kind": "memory", "name": "duct-sizes"}, "Tower/A")):
        answer = place(artefact, project=project)
        print("  %-22s %s" % (answer["refused"], answer["why"][:58]))

    print("\nwhat this agent does not judge")
    for line in good["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
