# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A tripwire for Q-51. It fails the day Heron first carries text it did not write.

    python tests/test_carried_sources.py

WHAT THIS IS FOR, AND WHY IT IS NOT A GUARD
--------------------------------------------
Q-51 asks what guards the path from retrieval into context on the day Heron
indexes something it did not author - a project standard, a specification, a
community package. That day has not come. Every part heron_context assembles
today comes from a source Heron wrote: the caller's own request, the fragment
library, its test cases, the executor's import list.

So the guard is not built, deliberately. docs/33 s5.2 records the reason in one
line: a scanner written against no corpus is a scanner written against a guess.
And 2026-09-09 supplied the evidence for that rule rather than the opinion -
gbrain's +31.4 said "add the graph", Heron's own corpus said no at all six
settings tried (Q-52). Guessing loses here.

WHAT IT DOES INSTEAD
---------------------
It notices the day the question becomes real, so nobody has to remember it.

Two assertions, and both are about the BOUNDARY rather than the content:

  1. Every part carried, on every path, at every depth, has a source that is
     either one of Heron's own internal labels or a file inside this
     repository. Nothing else. The first part sourced from an indexed document
     fails this, by construction.

  2. The STANDARDS path still refuses, naming its missing source. That path
     exists precisely to carry clauses Heron did not write, and heron_context
     says so in its own part list:

         STANDARD = "standard"   # the clauses cited - source does not exist yet

     The day somebody implements it, this fails and points here.

WHAT IT DELIBERATELY DOES NOT DO
---------------------------------
It reads no part's BODY and looks for no pattern in it. It cannot tell a safe
clause from a hostile one and does not pretend to. It answers exactly one
question - "is Heron still only carrying its own words?" - which is the
question whose answer changing is what makes Q-51 urgent.

IF THIS TEST FAILS, IT IS PROBABLY NOT A BUG. It is Q-51 becoming live, and the
failure message says so.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


# Heron's own labels, each attached to a part built here from Heron's own
# material. A source that is not one of these and is not a file in this
# repository is text from somewhere else, which is the whole point.
#
# ADDING TO THIS LIST IS A DECISION, NOT A FIX. If a new part is genuinely
# Heron's own, add its label and say in one line why it is. If it is not, the
# test has done its job and Q-51 is now a live question rather than a parked
# one.
OWN_LABELS = (
    "the caller",                      # the request, verbatim
    "the caller and heron_scope",      # the situation lines
    "derived from the fragment store", # the matched capability
    "heron_retrieve.eligible()",       # what the walls removed
)


def owns(source):
    """Is this a source Heron wrote, or a file inside this checkout?"""
    if source in OWN_LABELS:
        return True
    # heron_fragment.repo_relative() gives a repo-relative path for anything
    # inside the checkout, and an ABSOLUTE one for a library kept elsewhere.
    # An absolute path is not automatically foreign - a fragment library on
    # another drive is still Heron's - so it is accepted only when it really
    # is a fragment or the executor's import list.
    if os.path.isabs(source):
        return ("fragments" in source.replace("\\", "/")
                or source.replace("\\", "/").endswith("HeronFragmentImports.cs"))
    return os.path.exists(os.path.join(ROOT, source))


def main():
    print("Q-51 TRIPWIRE - is Heron still carrying only its own words?")
    print("=" * 70)

    home = tempfile.mkdtemp(prefix="heron-sources-")
    was = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        import heron_scope as SCOPE
        import heron_search as SEARCH
        import heron_context as CONTEXT

        SCOPE.rebuild(SCOPE.GLOBAL)
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            SEARCH.index(store)

            rows = [r for r in store.fragments() if r["semantic_identity"]]
            check(len(rows) > 100,
                  "the library is loaded (%d fragments with a phrasing)"
                  % len(rows))

            print()
            print("1. Every part, every path, every depth - whose words are they?")
            print("-" * 70)

            foreign = {}
            carried = 0
            # A spread rather than all 360: the question is which SOURCES exist,
            # and every fragment produces the same four kinds. Enough to cover
            # each part kind on each path, cheap enough to run every time.
            for row in rows[:60]:
                for path in (CONTEXT.CACHED, CONTEXT.SIMPLE,
                             CONTEXT.GENERATION):
                    for depth in (CONTEXT.FULL, CONTEXT.OVERVIEW,
                                  CONTEXT.ABSTRACT):
                        try:
                            ctx = CONTEXT.assemble(
                                store, row["semantic_identity"], path=path,
                                revit="2024", depth=depth)
                        except (CONTEXT.SourceMissing, CONTEXT.OverBudget,
                                CONTEXT.TooDeep, ValueError):
                            continue
                        for part in ctx.parts:
                            carried += 1
                            if not owns(part.source):
                                foreign.setdefault(part.source, set()).add(
                                    part.kind)

            check(carried > 100,
                  "%d parts were assembled and inspected" % carried)
            check(not foreign,
                  "EVERY ONE came from a source Heron wrote"
                  + ("" if not foreign else
                     " - but %d did not: %s"
                     % (len(foreign), sorted(foreign)[:3])))

            if foreign:
                print()
                print("  " + "!" * 62)
                print("  Q-51 IS NOW LIVE. Heron is carrying text it did not")
                print("  write, from:")
                for source in sorted(foreign):
                    print("      %-46s %s" % (source, sorted(foreign[source])))
                print()
                print("  This is probably not a bug. Read Q-51 in")
                print("  docs/OPEN-QUESTIONS.md and decide the guard NOW,")
                print("  with the index rather than after it. If the source")
                print("  above is genuinely Heron's own, add its label to")
                print("  OWN_LABELS in this file and say in one line why.")
                print("  " + "!" * 62)

            print()
            print("2. The STANDARDS path still has no source to carry")
            print("-" * 70)
            refused = None
            try:
                CONTEXT.assemble(store, rows[0]["semantic_identity"],
                                 path=CONTEXT.STANDARDS, revit="2024")
            except CONTEXT.SourceMissing as why:
                refused = str(why)
            check(refused is not None,
                  "the STANDARDS path still refuses - there is no clause store")
            check(refused and "clause" in refused,
                  "and it names the missing source rather than degrading")
            if refused is None:
                print()
                print("  The STANDARDS path now WORKS, which means somebody")
                print("  built the clause store. That is the day Q-51 was")
                print("  written for. The guard belongs in the same change.")

            print()
            print("3. The part list still says the source does not exist")
            print("-" * 70)
            body = io.open(os.path.join(ROOT, "brain", "heron_context.py"),
                           encoding="utf-8").read()
            check("source does not exist yet" in body,
                  "heron_context.py still carries the countdown comment on "
                  "STANDARD - remove it only when the guard exists")
        finally:
            store.close()
    finally:
        if was is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = was
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        print()
        print("If the failure is about a FOREIGN SOURCE, this test has done")
        print("its job: Q-51 stopped being a question about the future.")
        return 1

    print("PASSED - Heron carries only its own words, so Q-51 is still a")
    print("question about the future and the guard is still correctly")
    print("unbuilt. This test is what will say when that changes.")
    print()
    print("It reads no part's body and looks for no pattern in one. It")
    print("cannot tell a safe clause from a hostile one and does not")
    print("pretend to.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
