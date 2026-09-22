# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-BRN-007
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Brain initialisation - the one install step that can destroy what it finds.

    python brain/heron_brain_init.py

WHAT IT IS FOR (docs/28, HERON-INS-BRN-007)
--------------------------------------------
"Initialises knowledge stores." T1, risk ADMIN. Step 11 of docs/07 s1.

WHY THIS STEP IS DIFFERENT FROM THE OTHER FIFTEEN
---------------------------------------------------
Every other install step writes product. If it goes wrong it is rerun and
the second attempt costs minutes. This one touches the DATA class (docs/06
s2): Brain, Skills, Fragments, Memory - the half that "must survive every
update, uninstall and reinstall", because nothing regenerates it.

So the dangerous case is not a failed initialisation. It is a SUCCESSFUL
one, on a machine that already had stores. An empty knowledge base looks
exactly like a fresh install, and the modeller whose year of project
memory it replaced finds out weeks later.

**An existing store is never re-initialised here.** It is named and left,
and the run continues for the scopes that really are new. "Initialise
everything" is the instruction a person gives when they believe the
machine is empty, and this agent is the thing that checks whether it is.

ONE STORE PER SCOPE, PHYSICALLY
---------------------------------
Golden Rule 5 and D-23: separation is the filesystem, not a WHERE clause
somebody can forget. heron_scope.py already makes that real - one file per
scope, and a statement that tries to reach a second file raises. This
agent plans against the SAME scope list and the same paths rather than a
second copy of them, and refuses a plan that would put two scopes in one
file before any of it reaches disk.

THE PROJECT SCOPE NEEDS A PROJECT, AND NOTHING DEFAULTS IT
------------------------------------------------------------
heron_scope.scope_path() already refuses this, with the reason: guessing
which project a store belongs to writes one client's knowledge into
another's file, which is a contractual breach rather than a bug (docs/10
s2). Refusing it HERE too is not duplication - it is refusing at plan
time, before an install script is halfway through, and the error a person
sees is about their install rather than about a path.

IT CREATES NOTHING
--------------------
It returns what would be created, what already exists, and what it
refuses. Creating them is heron_scope's, in a process somebody started.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import heron_flags as FLG                                      # noqa: E402
import heron_scope as SCOPE                                    # noqa: E402


def _wanted(entry):
    """(scope, project_key) for one requested store."""
    if isinstance(entry, dict):
        return (str(entry.get("scope") or "").strip().lower(),
                str(entry.get("project") or entry.get("project_key")
                    or "").strip())
    return str(entry).strip().lower(), ""


def _same_file(path):
    """One spelling of a path, so two spellings of one file compare equal.

    THE COMPARISON THAT DECIDES WHETHER A STORE IS LEFT ALONE WAS RAW
    STRING EQUALITY. A caller who wrote the path with a trailing separator,
    or with a redundant "." in it, got a plan to CREATE a store that is
    already there - which is this agent's own dangerous case reached
    through the one line that guards it. `normcase` is here because
    Heron's platform is Windows, where two spellings differing only in
    case name one file; on POSIX it does nothing. Row 5b-119.
    """
    return os.path.normcase(os.path.normpath(str(path).strip()))


def plan(wanted, existing=None, origin=None, approval=None):
    """
    {create, keep, why} - or a refusal. Nothing is created.

    `existing` is what is already on disk, handed in: this agent does not
    look, because looking and then acting on what it saw is the shape of
    the mistake it exists to prevent.
    """
    if not wanted:
        return {"created": False, "refused": "NOTHING_TO_INITIALISE",
                "why": "no scope was asked for. An initialisation that "
                       "creates nothing is not a safe one - it is a call "
                       "that did not say what to build."}

    allowed, why_origin = FLG.origin_allowed(origin, "setting up the brain's store")
    if not allowed:
        return {"created": False, "refused": "NOT_FROM_THE_USER",
                "why": why_origin,
                "proposal": "ask the user. This step touches the data class "
                            "(docs/06 s2), which is the half nothing "
                            "regenerates."}

    by = str((approval or {}).get("by") or "").strip() \
        if isinstance(approval, dict) else ""
    if not by:
        return {"created": False, "refused": "NOT_APPROVED",
                "why": "initialising knowledge stores is ADMIN and nobody "
                       "signed. The failure worth signing against is not a "
                       "failed initialisation - it is a SUCCESSFUL one on a "
                       "machine that already had stores."}

    here = set(_same_file(path) for path in (existing or [])
               if str(path).strip())
    create, keep, seen = [], [], {}

    for entry in wanted:
        scope, project = _wanted(entry)
        if scope not in SCOPE.SCOPES:
            return {"created": False, "refused": "NOT_A_SCOPE",
                    "scope": scope or None,
                    "why": "%r is not a knowledge scope. Known: %s. The "
                           "scope list is heron_scope's own - a second copy "
                           "is a second copy to disagree with."
                           % (scope or None, ", ".join(SCOPE.SCOPES))}
        if scope == SCOPE.PROJECT and not project:
            return {"created": False, "refused": "PROJECT_KEY_MISSING",
                    "scope": scope,
                    "why": "the project scope needs a project key and was "
                           "given none. Guessing which project a store "
                           "belongs to writes one client's knowledge into "
                           "another's file, which is a contractual breach "
                           "rather than a bug (docs/10 s2).",
                    "proposal": "ask which project, then pass it. D-33: "
                                "Heron does not assume an input - it asks, "
                                "once."}
        if scope != SCOPE.PROJECT and project:
            return {"created": False, "refused": "PROJECT_KEY_NOT_WANTED",
                    "scope": scope,
                    "why": "the %s scope is one apiece and was given the "
                           "project key %r. A project key where none "
                           "belongs is somebody expecting per-project "
                           "separation from a store that has none."
                           % (scope, project)}

        try:
            path = SCOPE.scope_path(scope, project or None)
        except ValueError as refused:
            return {"created": False, "refused": "NOWHERE_TO_PUT_IT",
                    "scope": scope, "why": str(refused)}

        # ONE STORE PER SCOPE, PHYSICALLY - checked before anything reaches
        # disk rather than after two scopes share a file.
        # THE SAME STORE ASKED FOR TWICE IS ONE STORE, requested twice -
        # not a conflict and not two creates. A plan that listed it twice
        # would have an installer create it, then create it again over
        # what it just made.
        if seen.get(path) == (scope, project):
            continue
        if path in seen:
            return {"created": False, "refused": "TWO_SCOPES_ONE_STORE",
                    "path": path,
                    "why": "%s and %s would both be %s. Golden Rule 5 and "
                           "D-23 make separation the FILESYSTEM, not a "
                           "WHERE clause somebody can forget, and two "
                           "scopes in one file is the clause coming back."
                           % (seen[path][0], scope, os.path.basename(path))}
        seen[path] = (scope, project)

        # THE DANGEROUS CASE IS A SUCCESSFUL RE-INITIALISATION.
        if _same_file(path) in here:
            keep.append({"scope": scope, "project": project or None,
                         "path": path,
                         "why": "already there, and left alone. An empty "
                                "knowledge base looks exactly like a fresh "
                                "install, and whoever lost a year of "
                                "project memory finds out weeks later "
                                "(docs/06 s2: data survives every update, "
                                "uninstall and reinstall)."})
            continue

        create.append({"scope": scope, "project": project or None,
                       "path": path,
                       "meaning": SCOPE.SCOPE_MEANING[scope],
                       "schema": SCOPE.SCHEMA_VERSION})

    return {
        "created": False, "create": create, "keep": keep,
        "why": "%d store(s) to create, %d already there and left alone. "
               "Nothing was created - this is the plan."
               % (len(create), len(keep)),
        "unjudged": [
            "NOTHING WAS CREATED OR TOUCHED. heron_scope.open_scope() does "
            "that, in a process somebody started on purpose.",
            "WHAT ALREADY EXISTS WAS HANDED IN, not looked up. This agent "
            "does not scan the disk and then act on what it saw - that is "
            "the shape of the mistake it exists to prevent, and a caller "
            "that passes a stale list gets a plan to overwrite stores this "
            "agent was never told about.",
            "the scopes, their meanings, their paths and the schema version "
            "are heron_scope's own. A second copy of any of them is a "
            "second copy to disagree with.",
            "A RELATIVE PATH IN `existing` IS NOT MATCHED. Two spellings "
            "of one file compare equal - a trailing separator, a redundant "
            "\".\", and on Windows a difference of case - but resolving a "
            "relative path needs a working directory, and choosing one "
            "would be this agent guessing where a caller meant. Hand in "
            "the paths as they are on disk.",
        ],
    }


def main(argv):
    print("BRAIN INITIALISATION   the one step that can destroy what it finds")
    print("=" * 72)

    signed = {"by": "ajmal", "at": "2026-09-14T12:00Z"}
    wanted = ["global", "company", "user",
              {"scope": "project", "project": "Tower-B"}]

    answer = plan(wanted, existing=[], origin="user", approval=signed)
    if answer.get("refused"):
        # NOWHERE TO PUT IT is a real answer, not a broken demo: this
        # container has no %APPDATA% and no HERON_KNOWLEDGE, which is
        # exactly the machine heron_scope refuses to guess for.
        print("  %s" % answer["why"])
        print()
        print("  That is the honest answer on this machine. Set "
              "HERON_KNOWLEDGE to see")
        print("  the rest: HERON_KNOWLEDGE=/tmp/kb python %s"
              % os.path.relpath(__file__, ROOT))
        return 0
    print("  %s" % answer["why"])
    for entry in answer["create"]:
        print("    %-12s %-34s %s" % (entry["scope"],
                                      os.path.basename(entry["path"]),
                                      entry["meaning"][:34]))

    print()
    print("  And on a machine that already has them:")
    already = [entry["path"] for entry in answer["create"][:2]]
    answer = plan(wanted, existing=already, origin="user", approval=signed)
    print("  %s" % answer["why"])
    print("    %s" % answer["keep"][0]["why"][:112])

    print()
    print("  Every way it refuses:")
    cases = [
        ("a scope nobody has", ["knowledge"], {}),
        ("project with no project", ["project"], {}),
        ("a project key on global",
         [{"scope": "global", "project": "Tower-B"}], {}),
        ("two scopes, one file",
         [{"scope": "project", "project": "Tower B"},
          {"scope": "project", "project": "Tower-B"}], {}),
        ("nothing asked for", [], {}),
        ("a document asked", wanted, {"origin": "a document Heron read"}),
        ("nothing signed", wanted, {"approval": None}),
    ]
    for label, ask, override in cases:
        settings = {"existing": [], "origin": "user", "approval": signed}
        settings.update(override)
        print("    %-26s %s" % (label, plan(ask, **settings)["refused"]))

    print()
    print("  The dangerous case is not a FAILED initialisation. It is a")
    print("  successful one, on a machine that already had stores: an empty")
    print("  knowledge base looks exactly like a fresh install.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
