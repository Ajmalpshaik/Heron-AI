# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RAG-CTX-007
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Context Manager - what it REFUSES, which is the half that matters.

    python tests/test_context.py

Gathering context is easy and every piece already existed. docs/19 s1 says why
the refusing half is the point:

    AI systems do not fail loudly when over-fed context - they fail QUIETLY, by
    attending to the wrong thing and producing a confident, plausible, wrong
    answer.

So these cases are almost all negative ones.

WHAT IT PROVES
  1. A part outside the path's budget RAISES. docs/19 s2 says exceeding a
     budget is a bug in retrieval, not a reason to raise the budget - so the
     packet must not quietly carry it.
  2. THE REQUEST CROSSES BYTE FOR BYTE. A Revit token - OST_DuctCurves, a
     shared-parameter GUID - is the load-bearing part of a BIM sentence and is
     exactly what a compressor damages first (docs/05 s4). Asserted here rather
     than promised in a docstring, so a future compressor cannot quietly be
     pointed at it.
  3. A path whose SOURCE does not exist is refused BY NAME, not degraded.
  4. The CACHED path refuses a wording nothing can answer in one lookup, rather
     than falling through to a search the caller did not budget for.
  5. An assumed path is MARKED assumed. D-01 puts classification in the host,
     and a default read as a decision is how that boundary erodes.
  6. Every part carries a source. docs/19 s1 asks for traceability, and a part
     that cannot say where it came from cannot be checked when it is wrong.
  7. Size never refuses. Heron has no tokeniser (D-58) and a size limit here
     would be an invented number.

WHAT IT DOES NOT PROVE. That the context assembled is the RIGHT context for
any real request. That needs a model on the other end and a person judging the
answer, which is docs/18's evaluation work and not this.
"""

import os
import sys
import shutil
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    home = tempfile.mkdtemp(prefix="heron-context-")
    os.environ["HERON_KNOWLEDGE"] = home

    import heron_scope as SCOPE
    import heron_search as SEARCH
    import heron_context as CONTEXT

    try:
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            SEARCH.index(store)

            print("1. The budget is enforced, not advisory")
            ctx = CONTEXT.assemble(store, "select all ducts", revit="2024")
            over = CONTEXT.Part(CONTEXT.NEIGHBOUR, "x", "y", "z", "w")
            raised = False
            try:
                ctx.add(over)
            except CONTEXT.OverBudget:
                raised = True
            check(raised,
                  "a neighbour offered to the CACHED path raises OverBudget")
            check(CONTEXT.NEIGHBOUR not in ctx.kinds(),
                  "and it is NOT in the packet afterwards - refused means gone")

            allowed = set(CONTEXT.BUDGET[CONTEXT.CACHED])
            check(set(ctx.kinds()) <= allowed,
                  "every part carried is inside the path's declared budget")

            print()
            print("2. The request crosses byte for byte")
            for said in ["OST_DuctCurves",
                         "set RBS_DUCT_BOTTOM_ELEVATION to 2400",
                         "  SELECT   all ducts!!  "]:
                got = CONTEXT.assemble(store, said, path=CONTEXT.SIMPLE)
                body = [p.body for p in got.parts if p.kind == CONTEXT.REQUEST]
                check(body == [said],
                      "%r survives unaltered - no trim, no case fold, no cut"
                      % said)

            long_one = "select all ducts " * 500
            got = CONTEXT.assemble(store, long_one, path=CONTEXT.SIMPLE)
            body = [p.body for p in got.parts if p.kind == CONTEXT.REQUEST][0]
            check(body == long_one,
                  "an 8,500-character request is not truncated either - size "
                  "is reported, never enforced")

            print()
            print("3. A missing source is named, not quietly dropped")
            raised = None
            try:
                CONTEXT.assemble(store, "check this against our standard",
                                 path=CONTEXT.STANDARDS)
            except CONTEXT.SourceMissing as why:
                raised = str(why)
            check(raised is not None, "the STANDARDS path raises SourceMissing")
            check(raised and "clause" in raised,
                  "and the message names WHICH source is missing")

            print()
            print("4. CACHED refuses what it cannot answer in one lookup")
            raised = False
            try:
                CONTEXT.assemble(store, "some wording nobody ever declared",
                                 path=CONTEXT.CACHED)
            except CONTEXT.SourceMissing:
                raised = True
            check(raised,
                  "it refuses rather than falling through to a search the "
                  "caller did not budget for")

            print()
            print("5. An assumed path says so - D-01")
            got = CONTEXT.assemble(store, "some wording nobody ever declared")
            check(got.path == CONTEXT.SIMPLE and got.assumed_path,
                  "an unclassified request defaults to SIMPLE and is MARKED "
                  "assumed")
            got = CONTEXT.assemble(store, "select all ducts")
            check(got.path == CONTEXT.CACHED and not got.assumed_path,
                  "a short circuit hit is DERIVED, not assumed - it is a fact "
                  "about the store, not a judgement about the user")

            print()
            print("6. Every part can say where it came from")
            got = CONTEXT.assemble(store, "select all ducts",
                                   path=CONTEXT.GENERATION, revit="2024")
            check(all(p.source for p in got.parts),
                  "no part has an empty source (%d part(s))" % len(got.parts))
            check(all(p.why for p in got.parts),
                  "and no part has an empty reason for being there")
            sources = [p.source for p in got.parts
                       if p.kind in (CONTEXT.NEIGHBOUR, CONTEXT.TESTS)]
            check(all(s.startswith("brain/fragments/") for s in sources),
                  "a part read from disk cites the file (%s)" % (sources or "-"))

            print()
            print("7. An unknown path is refused rather than guessed at")
            raised = False
            try:
                CONTEXT.assemble(store, "anything", path="whatever")
            except ValueError:
                raised = True
            check(raised, "'whatever' is not a path and raises ValueError")

            print()
            print("8. The budgets are docs/19 s2's, and nest as it describes")
            check(set(CONTEXT.BUDGET[CONTEXT.CACHED])
                  < set(CONTEXT.BUDGET[CONTEXT.SIMPLE]),
                  "CACHED carries strictly less than SIMPLE")
            for wider in (CONTEXT.STANDARDS, CONTEXT.GENERATION):
                check(set(CONTEXT.BUDGET[CONTEXT.SIMPLE])
                      <= set(CONTEXT.BUDGET[wider]),
                      "%s is SIMPLE plus its own extras, never a different set"
                      % wider)
            check(CONTEXT.REQUEST in CONTEXT.BUDGET[CONTEXT.CACHED],
                  "every path carries the request - there is no path that "
                  "forwards a question without the question")
        finally:
            store.close()
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the budget refuses rather than stretches, the request")
    print("crosses byte for byte including Revit's own tokens, a missing source")
    print("is named instead of degraded, and an assumed path admits it.")
    print()
    print("It does not prove the context assembled is the RIGHT context. That")
    print("needs a model on the other end and a person judging the answer.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
