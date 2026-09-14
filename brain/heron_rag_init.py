# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-RAG-008
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
RAG initialisation - derived, so rebuilding it freely is the right answer.

    python brain/heron_rag_init.py

WHAT IT IS FOR (docs/28, HERON-INS-RAG-008)
--------------------------------------------
"Creates vector store, indexes, embedding config." T1, risk ADMIN. Step 12
of docs/07 s1 - the one immediately after HERON-INS-BRN-007.

THE TWO ADJACENT STEPS HAVE OPPOSITE RULES, AND THAT IS THE POINT
--------------------------------------------------------------------
Step 11 initialises knowledge stores and must NEVER rebuild one that
exists: that is the DATA class, and nothing regenerates it. Step 12
initialises the vector index and may rebuild it whenever it likes.

docs/06 s2 is why, and docs/07 s8 says it outright: "The vector index is
the easy case - it is derived, so the migration is 'delete and rebuild'.
Fragments and memory are the real work."

Two steps, one after the other, with opposite answers to "may I rebuild
this?" - and the answer comes from which CLASS the artefact is in, not
from a policy either agent applies. Reading them as one rule in either
direction is the expensive mistake: rebuild the knowledge store and a
year of project memory is gone; refuse to rebuild the index and Heron
stays on a stale one forever.

THE EMBEDDING CONFIG IS NOT DERIVED, AND IT IS THE TRAP
---------------------------------------------------------
An index is only meaningful to the backend that built it. `heron_embed`
has two - a trained sentence model, and built-in character n-grams that
its own docstring calls NOT MEANING - and a vector from one is not
comparable to a vector from the other. Nothing about the numbers says
which produced them.

So the backend that built an index is RECORDED, and an index whose
recorded backend is not the one configured now must be REBUILT rather
than read. It is cheap - the index is derived - and the alternative is
retrieval that returns confident nonsense with nothing to indicate it.

An index that records NO backend is in the same position: nobody can say
what built it, so nobody can say it matches.

IT CREATES NOTHING
--------------------
It returns what would be built and what would be rebuilt, with the reason
for each. Building it is the ingest path's, in a process somebody started.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import heron_embed as EMBED                                    # noqa: E402
import heron_flags as FLG                                      # noqa: E402
import heron_scope as SCOPE                                    # noqa: E402

# heron_embed's own two. A third name here would be a backend nothing
# can produce.
BACKENDS = (EMBED.LEXICAL, EMBED.MODEL)


def plan(scopes, indexes=None, backend=None, origin=None, approval=None):
    """
    {build, rebuild, why} - or a refusal. Nothing is created.

    `indexes` is what exists, per scope, each saying which backend built
    it. `backend` is the one configured now - asked for, because which
    backend is active depends on what is installed and only the running
    interpreter knows.
    """
    if not scopes:
        return {"created": False, "refused": "NOTHING_TO_INDEX",
                "why": "no scope was named. An index over nothing is not an "
                       "empty index - it is a call that did not say what to "
                       "index."}

    allowed, why_origin = FLG.origin_allowed(origin)
    if not allowed:
        return {"created": False, "refused": "NOT_FROM_THE_USER",
                "why": why_origin,
                "proposal": "ask the user. Rebuilding an index is cheap; "
                            "deciding WHICH backend Heron retrieves with is "
                            "not, and this is where that is settled."}

    by = str((approval or {}).get("by") or "").strip() \
        if isinstance(approval, dict) else ""
    if not by:
        return {"created": False, "refused": "NOT_APPROVED",
                "why": "creating the vector store is ADMIN and nobody "
                       "signed."}

    now = str(backend or "").strip().lower()
    if now not in BACKENDS:
        return {"created": False, "refused": "BACKEND_NOT_DECLARED",
                "why": "%r is not one of heron_embed's backends (%s). Which "
                       "one built an index decides whether the index can be "
                       "read at all, so it is not something to leave "
                       "unstated - and a third name here would be a backend "
                       "nothing can produce."
                       % (backend, ", ".join(BACKENDS)),
                "proposal": "ask heron_embed.backend() for the one actually "
                            "active. It depends on what is installed, and "
                            "only the running interpreter knows."}

    indexes = indexes if isinstance(indexes, dict) else {}
    build, rebuild, keep = [], [], []

    for entry in scopes:
        scope = str(entry).strip().lower()
        if scope not in SCOPE.SCOPES:
            return {"created": False, "refused": "NOT_A_SCOPE",
                    "scope": scope or None,
                    "why": "%r is not a knowledge scope. Known: %s. The list "
                           "is heron_scope's own." % (entry,
                                                      ", ".join(SCOPE.SCOPES))}

        found = indexes.get(scope)
        if found is None:
            build.append({"scope": scope, "backend": now,
                          "why": "no index yet"})
            continue

        built_with = str((found or {}).get("backend") or "").strip().lower() \
            if isinstance(found, dict) else str(found).strip().lower()
        if not built_with:
            rebuild.append({"scope": scope, "was": None, "backend": now,
                            "why": "the index records no backend, so nobody "
                                   "can say what built it and nobody can "
                                   "say it matches. Rebuilding is cheap - "
                                   "the index is DERIVED (docs/06 s2) - and "
                                   "reading it is retrieval returning "
                                   "confident nonsense with nothing to "
                                   "indicate it."})
            continue
        if built_with != now:
            rebuild.append({"scope": scope, "was": built_with,
                            "backend": now,
                            "why": "built with %s and %s is configured now. "
                                   "A vector from one is not comparable to a "
                                   "vector from the other, and nothing about "
                                   "the numbers says which produced them."
                                   % (built_with, now)})
            continue
        keep.append({"scope": scope, "backend": built_with,
                     "why": "already indexed with %s" % built_with})

    return {
        "created": False, "build": build, "rebuild": rebuild, "keep": keep,
        "backend": now,
        "why": "%d to build, %d to rebuild, %d already current, on %s. "
               "Nothing was created - this is the plan."
               % (len(build), len(rebuild), len(keep), now),
        "unjudged": [
            "NOTHING WAS CREATED OR DELETED. The ingest path builds these, "
            "in a process somebody started on purpose.",
            "REBUILDING IS THE RIGHT ANSWER HERE AND THE WRONG ONE ONE STEP "
            "EARLIER. docs/07 s8: the vector index is derived, so the "
            "migration is delete and rebuild - while HERON-INS-BRN-007 must "
            "never rebuild a knowledge store, because that is the data "
            "class. Same question, opposite answers, and the answer comes "
            "from which class the artefact is in.",
            "%s"
            % ("the configured backend is %s - heron_embed's own docstring "
               "calls character n-grams NOT MEANING, so retrieval here "
               "matches spelling and word order rather than sense. "
               "HERON-INS-DEP-005 has the sentence to say about it."
               % now if now == EMBED.LEXICAL else
               "the configured backend is the trained model, which is the "
               "one that matches on meaning."),
            "which backend is active was ASKED FOR, not read here. It "
            "depends on what is installed, and a value stated by a caller "
            "is a caller that can make Heron record an index as built by "
            "something that never ran.",
        ],
    }


def main(argv):
    print("RAG INITIALISATION   derived, so rebuilding it freely is right")
    print("=" * 72)

    signed = {"by": "ajmal", "at": "2026-09-14T12:00Z"}
    active, why = EMBED.backend()
    print("  heron_embed.backend() on this machine: %s" % active)
    print("    %s" % why[:96])

    scopes = ["global", "company", "project", "user"]
    answer = plan(scopes, indexes={}, backend=active, origin="user",
                  approval=signed)
    print()
    print("  %s" % answer["why"])

    print()
    print("  And the trap - an index built by a backend that is not the one")
    print("  configured now:")
    other = EMBED.MODEL if active == EMBED.LEXICAL else EMBED.LEXICAL
    answer = plan(scopes, backend=active, origin="user", approval=signed,
                  indexes={"global": {"backend": active},
                           "company": {"backend": other},
                           "project": {}})
    print("  %s" % answer["why"])
    for entry in answer["rebuild"]:
        print("    %-10s %s" % (entry["scope"], entry["why"][:96]))
    for entry in answer["keep"]:
        print("    %-10s %s" % (entry["scope"], entry["why"]))

    print()
    print("  Two adjacent install steps, opposite answers:")
    print("    step 11  BRN-007  never rebuild - that is the DATA class")
    print("    step 12  RAG-008  rebuild freely - this is DERIVED")
    print("    and the answer comes from which class the artefact is in,")
    print("    not from a policy either agent applies.")

    print()
    print("  Every way it refuses:")
    for label, ask, override in (
            ("nothing to index", [], {}),
            ("a scope nobody has", ["knowledge"], {}),
            ("no backend declared", scopes, {"backend": None}),
            ("a backend nothing makes", scopes, {"backend": "openai"}),
            ("a document asked", scopes,
             {"origin": "a document Heron read"}),
            ("nothing signed", scopes, {"approval": None})):
        settings = {"indexes": {}, "backend": active, "origin": "user",
                    "approval": signed}
        settings.update(override)
        print("    %-26s %s" % (label, plan(ask, **settings)["refused"]))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
