# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RAG-EVO-016
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Knowledge evolution - it plans a move, and moving is not removing.

    python tests/test_evolution.py

WHAT IT PROVES
  1. IT DOES NOT DECIDE THAT A SCHEME HAS STOPPED FITTING. With no move
     asked for, nothing is planned however lopsided the store is, and
     the answer has no verdict key at any shape.

  2. THE PROJECT BOUNDARY HOLDS IN BOTH DIRECTIONS - nothing leaves a
     project for a shared scope, and nothing shared acquires a project.

  3. A MOVE IS NOT A REMOVAL, and the refusal is REACHABLE: asking to
     drop a claim gets the rule, not a shrug about vocabulary.

  4. NO BACKUP, NO MOVE - refused, not warned about, and the strain
     report alone needs none.

  5. THE SCOPES ARE HERON-RAG-LIB-001's, compared by identity.

  6. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_evolution as EVO                                  # noqa: E402
import heron_scope as SCOPE                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_evolution.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(claims, **kw):
        answer = EVO.plan(claims, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    shared = {"id": "K-1", "scope": "temporary"}
    owned = {"id": "K-2", "scope": "project", "project": "tower a"}

    print("1. It does not decide that a scheme has stopped fitting")
    lopsided = ask([{"id": "K-%d" % n, "scope": "temporary"}
                    for n in range(40)] + [{"id": "K-X", "scope": "global"}])
    even = ask([{"id": "A", "scope": "global"},
                {"id": "B", "scope": "company"}])
    check(not lopsided["move"] and not even["move"],
          "with no move asked for, nothing is planned either way")
    check(sorted(lopsided) == sorted(even),
          "and 40-to-1 gives the same keys as 1-to-1: %s"
          % ", ".join(sorted(even)))
    for verdict in ("problem", "warning", "verdict", "unbalanced",
                    "recommend", "severity", "score"):
        check(verdict not in lopsided,
              "no '%s' key however lopsided the store" % verdict)
    check(lopsided["strain"]["scopes"]["temporary"] == 40,
          "the counts are reported: %d in temporary"
          % lopsided["strain"]["scopes"]["temporary"])
    check("every line is invented" in lopsided["why"],
          "and the answer says why no line is drawn through them")
    check(len(lopsided["unjudged"]) == len(even["unjudged"]) == 4,
          "four unjudged lines either way")

    print("\n2. The project boundary holds in both directions")
    for to in EVO.SHARED:
        out = ask([owned], moves=[{"id": "K-2", "to": to}], backup="B")
        check(out.get("refused") == "WOULD_LEAVE_ITS_PROJECT",
              "a project's claim may not go to '%s'" % to)
    into = ask([shared], moves=[{"id": "K-1", "to": "project"}], backup="B")
    check(into.get("refused") == "WOULD_LEAVE_ITS_PROJECT",
          "and a shared claim may not acquire a project - nothing later "
          "can tell it was not theirs all along")
    check("F14" in whole and "docs/10 s2" in whole,
          "the refusals cite docs/10 s2 and PROPOSALS F14")
    within = ask([owned], moves=[{"id": "K-2", "to": "project"}], backup="B")
    check(not within.get("refused") and within["move"][0]["project"]
          == "tower a",
          "while a move INSIDE the project is fine, and says whose it is")
    across = ask([shared], moves=[{"id": "K-1", "to": "company"}],
                 backup="B")
    check(not across.get("refused"),
          "and one between shared scopes is fine")

    print("\n3. A move is not a removal, and the refusal is reachable")
    for nowhere in ("", None, "remove", "delete", "drop", "discard",
                    "  NONE  "):
        gone = ask([shared], moves=[{"id": "K-1", "to": nowhere}],
                   backup="B")
        check(gone.get("refused") == "WOULD_REMOVE_KNOWLEDGE",
              "'to: %r' is refused as a removal" % nowhere)
    check("archives and never deletes" in
          ask([shared], moves=[{"id": "K-1", "to": ""}],
              backup="B")["why"],
          "citing HERON-WSP-CLN-009's rule one level up")
    check(ask([shared], moves=[{"id": "K-1", "to": "nowhere-real"}],
              backup="B").get("refused") == "NOT_A_MOVE",
          "while a scope that simply does not exist is a DIFFERENT "
          "refusal - somebody asking to delete asked a real question")
    # And the invariant the refusal replaced is held by construction.
    check("BY CONSTRUCTION" in logic and "cannot fail is decoration" in logic,
          "the code says the every-claim-lands invariant is structural, "
          "not a check that could never fail")

    print("\n4. No backup, no move")
    check(ask([shared], moves=[{"id": "K-1", "to": "company"}]).get("refused")
          == "NO_BACKUP", "a move with no backup is refused")
    for empty in ("", "   ", None):
        check(ask([shared], moves=[{"id": "K-1", "to": "company"}],
                  backup=empty).get("refused") == "NO_BACKUP",
              "and %r is not a backup" % empty)
    check("reversible" in ask([shared],
                              moves=[{"id": "K-1", "to": "company"}]
                              )["why"],
          "the refusal names docs/07 s8's condition")
    check(ask([shared], moves=[{"id": "K-1", "to": "company"}]
              )["would_move"] == 1,
          "and says how many would have moved, so the ask is answerable")
    check(not ask([shared]).get("refused"),
          "while the strain report alone needs no backup - it changes "
          "nothing")

    print("\n5. The scopes are HERON-RAG-LIB-001's")
    check(set(EVO.SHARED) | {SCOPE.PROJECT} == set(SCOPE.SCOPES),
          "the shared scopes are every scope but PROJECT")
    check(EVO.SCOPE.SCOPES is SCOPE.SCOPES,
          "and the list IS heron_scope's object, not an equal copy")
    check("SCOPE.PROJECT" in logic and "SCOPE.SCOPES" in logic,
          "the agent reaches for PROJECT and the whole list through the "
          "module")
    # `"project"` also names a FIELD on a claim, which is not a copy of the
    # scope list, so it is excluded rather than pretended away.
    for name in (each for each in SCOPE.SCOPES if each != SCOPE.PROJECT):
        check('"%s"' % name not in logic,
              "and no literal '%s' anywhere in it" % name)
    check('claim.get("project")' in logic,
          "while the one 'project' in the agent is a claim FIELD, not a "
          "second copy of the scope name")

    print("\n6. Every failure is named and reached")
    check(ask(None).get("refused") == "NOTHING_TO_RESTRUCTURE",
          "nothing handed in is refused")
    check(ask([]).get("refused") == "NOTHING_TO_RESTRUCTURE",
          "and so is an empty store - it would report a perfect fit")
    for bad, why in (("not a map", "a claim that is not a map"),
                     ({"scope": "global"}, "and one with no id")):
        check(ask([bad]).get("refused") == "NOT_A_CLAIM", why)
    check(ask([shared, {"id": "K-1", "scope": "global"}]).get("refused")
          == "NOT_A_CLAIM",
          "two claims under one id is refused - a move would be ambiguous")
    for bad, why in (("not a map", "a move that is not a map"),
                     ({"to": "company"}, "one naming no claim"),
                     ({"id": "K-9", "to": "company"}, "and one naming a "
                                                      "claim nobody gave")):
        check(ask([shared], moves=[bad], backup="B").get("refused")
              == "NOT_A_MOVE", why)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-RAG-EVO-016.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 6, "the contract declares 6 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    spare = sorted(reached - set(named))
    check(not spare, "and nothing else was refused%s"
          % ("" if not spare else ": %s" % ", ".join(spare)))
    for acting in ("open(", "shutil", "os.rename", "os.remove", "write("):
        check(acting not in logic, "the code never uses %s" % acting)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    it plans a move, and moving is not removing")
    return 0


if __name__ == "__main__":
    sys.exit(main())
