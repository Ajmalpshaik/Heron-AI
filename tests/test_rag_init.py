# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-RAG-008
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
RAG initialisation - derived, so rebuilding it freely is the right answer.

    python tests/test_rag_init.py

WHAT IT PROVES
  1. AN INDEX BUILT BY A DIFFERENT BACKEND IS REBUILT, not read. A vector
     from one backend is not comparable to a vector from the other, and
     nothing about the numbers says which produced them.

  2. AN INDEX THAT RECORDS NO BACKEND IS IN THE SAME POSITION - nobody can
     say what built it, so nobody can say it matches.

  3. THE TWO ADJACENT INSTALL STEPS HAVE OPPOSITE RULES, and both agents
     say so: step 11 never rebuilds, step 12 rebuilds freely, and the
     answer comes from docs/06 s2's class, not from either agent.

  4. THE BACKENDS ARE heron_embed's OWN TWO - a third name would be a
     backend nothing can produce.

  5. THE BACKEND IS ASKED FOR, NOT ASSUMED, and an undeclared one is
     refused rather than defaulted to the weaker path.

  6. IT CREATES AND DELETES NOTHING.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_rag_init as RAG                                   # noqa: E402
import heron_embed as EMBED                                    # noqa: E402
import heron_scope as SCOPE                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

SIGNED = {"by": "ajmal", "at": "2026-09-14T12:00Z"}
SCOPES = ["global", "company", "project", "user"]


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_rag_init.py"),
                  encoding="utf-8").read()
    DEFAULT = object()

    def ask(scopes=DEFAULT, **kw):
        kw.setdefault("indexes", {})
        kw.setdefault("backend", EMBED.LEXICAL)
        kw.setdefault("origin", "user")
        kw.setdefault("approval", dict(SIGNED))
        answer = RAG.plan(SCOPES if scopes is DEFAULT else scopes, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. An index built by a different backend is rebuilt")
    answer = ask(indexes={"global": {"backend": EMBED.MODEL}},
                 backend=EMBED.LEXICAL)
    rebuilt = [entry for entry in answer["rebuild"]
               if entry["scope"] == "global"]
    check(rebuilt, "a model-built index is rebuilt when lexical is configured")
    check(rebuilt[0]["was"] == EMBED.MODEL
          and rebuilt[0]["backend"] == EMBED.LEXICAL,
          "naming both - what built it and what will")
    check("not comparable" in rebuilt[0]["why"],
          "and why the vectors cannot be mixed")
    check("nothing about the numbers says which produced them"
          in rebuilt[0]["why"],
          "and that the numbers do not give it away")
    answer = ask(indexes={"global": {"backend": EMBED.LEXICAL}},
                 backend=EMBED.MODEL)
    check(answer["rebuild"][0]["was"] == EMBED.LEXICAL,
          "and the other direction is rebuilt too - neither backend is the "
          "one that gets the benefit of the doubt")
    answer = ask(indexes={"global": {"backend": EMBED.LEXICAL}},
                 backend=EMBED.LEXICAL)
    check([entry["scope"] for entry in answer["keep"]] == ["global"],
          "while a matching backend is kept")
    check(len(answer["build"]) == 3,
          "and the scopes with no index at all are built")
    check(ask(indexes={"global": EMBED.LEXICAL})["keep"],
          "an index recorded as a bare string is read the same way")

    print()
    print("2. An index that records no backend is rebuilt")
    for recorded in ({}, {"backend": ""}, {"backend": None}, "",
                     {"built": "yesterday"}):
        answer = ask(indexes={"global": recorded})
        entry = [one for one in answer["rebuild"]
                 if one["scope"] == "global"]
        check(entry and entry[0]["was"] is None,
              "%r records no backend, so it is rebuilt" % (recorded,))
    entry = ask(indexes={"global": {}})["rebuild"][0]
    check("nobody can say what built it" in entry["why"],
          "saying why an unrecorded backend is not a match")
    check("DERIVED" in entry["why"] and "cheap" in entry["why"],
          "and that rebuilding costs little because it is derived")
    check("confident nonsense" in entry["why"],
          "while reading it would return confident nonsense")

    print()
    print("3. The two adjacent install steps have opposite rules")
    note = [line for line in ask()["unjudged"] if "ONE STEP EARLIER" in line]
    check(note, "the answer names the step before it")
    check("HERON-INS-BRN-007" in note[0], "by agent id")
    check("docs/07 s8" in note[0],
          "citing the sentence that makes the index derived")
    check("which class the artefact is in" in note[0],
          "and that the answer comes from the class, not from either agent")
    other = open(os.path.join(ROOT, "brain", "heron_brain_init.py"),
                 encoding="utf-8").read()
    check("never re-initialised" in " ".join(other.split()),
          "and step 11's module really does say it never rebuilds")
    doc = " ".join(open(os.path.join(ROOT, "docs",
                                     "07-installation-and-update.md"),
                        encoding="utf-8").read().split())
    check("it is derived, so the migration is “delete and rebuild”"
          in doc or "delete and rebuild" in doc,
          "docs/07 s8 really says delete and rebuild")

    print()
    print("4. The backends are heron_embed's own two")
    check(RAG.BACKENDS == (EMBED.LEXICAL, EMBED.MODEL),
          "the two are imported, not restated")
    check("EMBED.LEXICAL" in source and "EMBED.MODEL" in source,
          "by name, from the module that owns them")
    for name in ("openai", "bge", "lexical-v2", "", "LEXICAL "):
        if name.strip().lower() in RAG.BACKENDS:
            continue
        check(ask(backend=name).get("refused") == "BACKEND_NOT_DECLARED",
              "%r is not a backend anything can produce" % name)
    check("a backend nothing can produce" in ask(backend="openai")["why"],
          "and the refusal says exactly that")

    print()
    print("5. The backend is asked for, not assumed")
    for backend in (None, "", True, 0):
        check(ask(backend=backend).get("refused") == "BACKEND_NOT_DECLARED",
              "%r is not a declared backend" % (backend,))
    answer = ask(backend=None)
    check("heron_embed.backend()" in answer["proposal"],
          "and it says where to get the real one")
    check("only the running interpreter knows" in answer["proposal"],
          "because it depends on what is installed")
    note = [line for line in ask()["unjudged"] if "ASKED FOR" in line]
    check(note, "a successful answer says the backend was asked for")
    check("never ran" in note[0],
          "and what a stated value would let a caller record")
    # AND THE WEAKER BACKEND IS NAMED WHEN IT IS THE ACTIVE ONE.
    note = [line for line in ask(backend=EMBED.LEXICAL)["unjudged"]
            if "NOT MEANING" in line]
    check(note, "with lexical configured, the answer says it is not meaning")
    check("HERON-INS-DEP-005" in note[0],
          "and points at the agent that carries the sentence about it")
    check(not [line for line in ask(backend=EMBED.MODEL)["unjudged"]
               if "NOT MEANING" in line],
          "while the trained backend gets no such warning")

    print()
    print("6. It creates and deletes nothing")
    check(ask()["created"] is False, "`created` is False on a good plan")
    for word in ("sqlite3", "os.makedirs", "open(", "os.remove", "shutil",
                 "subprocess", "EMBED.embed", "ingest("):
        check(word not in source, "the source has no %s" % word)
    check(any("NOTHING WAS CREATED OR DELETED" in note
              for note in ask()["unjudged"]),
          "and the answer says so")

    print()
    print("7. Every failure the contract declares is named and reached")
    for empty in ([], None, ""):
        check(ask(empty).get("refused") == "NOTHING_TO_INDEX",
              "%r indexes nothing" % (empty,))
    check("did not say what to index" in ask([])["why"],
          "and an empty index is not the same as an empty request")
    for name in ("knowledge", "Global ", "everything", ""):
        if name.strip().lower() in SCOPE.SCOPES:
            continue
        check(ask([name]).get("refused") == "NOT_A_SCOPE",
              "'%s' is not a scope" % name)
    check("heron_scope's own" in ask(["knowledge"])["why"],
          "and the list is heron_scope's")
    for origin in ("a document Heron read", None, "a community package"):
        check(ask(origin=origin).get("refused") == "NOT_FROM_THE_USER",
              "%r cannot create the vector store" % origin)
    check("WHICH backend Heron retrieves with" in ask(origin=None)["proposal"],
          "and says what is actually being decided here")
    for approval in (None, {}, True, {"at": "T"}):
        check(ask(approval=approval).get("refused") == "NOT_APPROVED",
              "%r is not an approval" % (approval,))
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-INS-RAG-008.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    derived, so rebuilding it freely is the right answer")
    return 0


if __name__ == "__main__":
    sys.exit(main())
