# Heron-Agent:  HERON-RAG-CNF-015
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Stage 8 - two scopes disagreeing is said out loud, and never settled.

    python tests/test_conflict.py

WHAT IT PROVES
  1. THE CASE STAGE 4 LEFT SITTING IS CAUGHT. Company 30mm against project
     40mm, both at clause 3.1, both correctly returned - and until now nothing
     said they disagreed. A modeller had to notice the numbers themselves.

  2. NOTHING IS RESOLVED, AND THE TEST ASSERTS THE ABSENCE. R-24: surfaced and
     asked about, never settled by rank. The Librarian still returns both
     clauses, under their own scopes, unchanged. This check is meant to fail
     the day somebody makes it pick a winner.

  3. THE HIERARCHY IS REPORTED AND NOT APPLIED. docs/20 s2 orders project above
     company and then says the half that matters - "overrides must not mean
     silently replaces". The report names which one that ordering would weigh
     highest and says in the same breath that Heron has not acted on it.

  4. ONLY QUANTITIES ARE COMPARED. A clause number, a year and a bare count are
     not disagreements, and treating them as such would fill the report with
     noise - which is how a flag stops being read.

  5. ONE SCOPE CANNOT DISAGREE WITH ITSELF. Two requirements in one document are
     a document with two requirements in it. R-24 is about two SOURCES.

  6. IT INHERITS THE MISSING FLOOR AND SAYS SO. Retrieval cannot refuse a
     question nothing covers (R-56, R-58, blocked on W-8), so a question
     NEITHER scope covers still reports the same disagreement. The test
     asserts that, because asserting silence would have been asserting a floor
     that is not there - and the report carries the caveat instead of the
     module inventing the number R-60 forbids.

WHAT IT DOES NOT PROVE, AND THE MODULE SAYS SO ITSELF
------------------------------------------------------
That the two clauses are ABOUT THE SAME REQUIREMENT. Heron compares units, not
subjects, because deciding sameness is meaning and this layer has no model and
no network. "Insulation 25mm" against "clearance 40mm" would be reported too.
The report says that in words rather than leaving it to be found out.
"""

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


COMPANY_DOC = """Acme Engineering BIM Standard 2026

Section 3 Ductwork

3.1 Insulation

Company practice is to insulate all ductwork to 30mm regardless of location.
"""

PROJECT_DOC = """Tower B Project Specification

Section 3 Ductwork

3.1 Insulation

For this project ductwork shall be insulated to 40mm in plant rooms only.
"""

# One document, two requirements. NOT a conflict - and the easiest thing for a
# unit-comparing check to get wrong.
ONE_VOICE_DOC = """Acme Engineering BIM Standard 2026

Section 3 Ductwork

3.1 Insulation

Ductwork shall be insulated to 30mm.

3.2 Clearance

A clearance of 75mm shall be maintained above every duct.
"""


def main():
    import heron_scope as SCOPE
    import heron_search as SEARCH
    import heron_embed as EMBED
    import heron_retrieve as R
    import heron_ingest as I
    import heron_conflict as C

    print("1. Only a QUANTITY is something two sources can disagree about")
    check(C.quantity("30mm") == ("mm", "30"),
          "30mm is 30 of a millimetre")
    check(C.quantity("2.5m") == ("m", "2.5"),
          "and 2.5m is a metre value, kept apart from millimetres")
    check(C.quantity("1in100") == ("gradient", "1:100"),
          "1:100 is a GRADIENT, not one inch followed by a hundred - the unit "
          "pattern would have read it that way")
    check(C.quantity("50pct") == ("pct", "50"),
          "and a percentage is its own unit")
    for noise in ("9.1.1", "2024", "3", "ost_ductcurves"):
        check(C.quantity(noise) is None,
              "%r is not a quantity - a clause number, a year, a bare count "
              "and a category are not things two sources disagree ABOUT, and "
              "reporting them would fill the report with noise" % noise)
    print()

    home = tempfile.mkdtemp(prefix="heron-conflict-")
    papers = tempfile.mkdtemp(prefix="heron-conflict-papers-")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        def load(scope, name, body, project=None):
            path = os.path.join(papers, name)
            with open(path, "w", encoding="utf-8") as handle:
                handle.write(body)
            store = SCOPE.open_scope(scope, project)
            try:
                I.ingest(store, path, added_by="tests")
                SEARCH.index_chunks(store)
                EMBED.index_chunks(store)
            finally:
                store.close()

        load(SCOPE.COMPANY, "acme-standard.md", COMPANY_DOC)
        load(SCOPE.PROJECT, "tower-b-spec.md", PROJECT_DOC, project="Tower B")

        question = "how thick should duct insulation be"
        found = C.disagreements(question,
                                scopes=[SCOPE.COMPANY, SCOPE.PROJECT],
                                project="Tower B")

        print("2. The case Stage 4 left sitting is CAUGHT")
        check(len(found) == 1,
              "one disagreement is reported, not none and not five")
        one = found[0] if found else None
        check(one is not None and one.unit == "mm",
              "it is about millimetres")
        check(one is not None and set(v.value for v in one.values) ==
              set(["30", "40"]),
              "30 against 40 - the two numbers a modeller had to spot "
              "themselves until now")
        check(one is not None and len(one.scopes) == 2,
              "from two different scopes, which is what R-24 means by two "
              "SOURCES")
        check(one is not None and any("Tower B" in s for s in one.scopes),
              "and the project answer is labelled with its project")
        print()

        print("3. It is SURFACED and nothing is resolved (R-24)")
        said = one.sentence() if one else ""
        check("DISAGREE" in said.upper(),
              "the sentence says they disagree")
        check("not decided" in said or "NOT BEEN APPLIED" in said,
              "and says Heron has not decided between them")
        check(not hasattr(one, "winner") and not hasattr(one, "resolve"),
              "there is no `winner` and no `resolve` on a Disagreement - this "
              "check is meant to FAIL the day somebody makes it pick one")
        check(not any(name.startswith("resolve") or name == "decide"
                      for name in dir(C)),
              "and heron_conflict exposes nothing that resolves a conflict")

        # THE LIBRARIAN IS UNCHANGED. Surfacing must not quietly remove one of
        # the two answers it was surfacing.
        asked = R.librarian(question, scopes=[SCOPE.COMPANY, SCOPE.PROJECT],
                            project="Tower B")
        answered = [a for a in asked if a.answer and a.answer.candidates]
        check(len(answered) == 2,
              "both scopes still answer, each under its own label - surfacing "
              "a disagreement removes nothing")
        print()

        print("4. The hierarchy is REPORTED, and the report says it was not used")
        check(one is not None and one.would_be_preferred and
              "Tower B" in one.would_be_preferred,
              "docs/20 s2 would weigh the PROJECT above the company, and the "
              "report names it")
        check("HAS NOT BEEN APPLIED" in said,
              "and says so in the same breath - docs/20 s2: 'overrides must "
              "not mean silently replaces'")
        check(one is not None and one.same_locator,
              "both sit at clause 3.1, which is reported as making them "
              "likelier to be the same requirement")
        check("cannot tell" in said,
              "and the report still says Heron cannot tell that they are - it "
              "compared units, not subjects")
        print()

        print("5. ONE scope cannot disagree with itself")
        alone = C.disagreements(question, scopes=[SCOPE.COMPANY],
                                project="Tower B")
        check(alone == [],
              "one scope reports nothing - a source does not conflict with "
              "itself, and R-24 is about two of them")

        second = tempfile.mkdtemp(prefix="heron-conflict-2-")
        os.environ["HERON_KNOWLEDGE"] = second
        try:
            load(SCOPE.COMPANY, "acme-two-clauses.md", ONE_VOICE_DOC)
            load(SCOPE.PROJECT, "tower-b-spec.md", PROJECT_DOC,
                 project="Tower B")
            two_in_one = C.disagreements("how thick should duct insulation be",
                                         scopes=[SCOPE.COMPANY],
                                         project="Tower B")
            check(two_in_one == [],
                  "and a single document carrying 30mm in one clause and 75mm "
                  "in another is a document with TWO REQUIREMENTS in it, not a "
                  "conflict")
        finally:
            os.environ["HERON_KNOWLEDGE"] = home
            shutil.rmtree(second, ignore_errors=True)
        print()

        print("6. IT INHERITS THE MISSING FLOOR, and the report carries it")
        # THIS CHECK WAS WRITTEN THE OTHER WAY ROUND AND THE CODE DISAGREED.
        # It expected a question neither scope covers to report nothing.
        # Retrieval has no floor - R-56 and R-58 are blocked on W-8 - so five
        # clauses come back for any question at all, and their numbers still
        # differ. The test now asserts what actually happens and holds the
        # limit open rather than claiming a floor that is not there.
        off_topic = C.disagreements("what colour should a sheet border be",
                                    scopes=[SCOPE.COMPANY, SCOPE.PROJECT],
                                    project="Tower B")
        check(len(off_topic) == 1,
              "a question NEITHER scope covers still reports the same "
              "disagreement - retrieval has no floor (R-56, R-58, blocked on "
              "W-8), so it returns clauses for anything asked")
        check("not established" in off_topic[0].sentence(),
              "and the report says so: whether these clauses ANSWER the "
              "question is not established. It reports the caveat rather than "
              "inventing the floor R-60 forbids")

        # AND THE CAVEAT IS THE RIGHT ONE FOR THIS CORPUS SIZE. The first
        # version carried Contest.words_selected_nothing, which on two chunks
        # per scope fired on the REAL question and stayed quiet on this one -
        # backwards, for a reason Contest already documents: below a pool of
        # twenty none of those counts means anything.
        check("FEWER CLAUSES THAN THE POOL" in off_topic[0].sentence(),
              "the caveat names the small pool, which is what is actually "
              "true here - not a route measurement that says more about the "
              "corpus size than the question")

        source = open(os.path.join(ROOT, "brain", "heron_conflict.py"),
                      encoding="utf-8").read()
        check("NOT THE SAME AS 'THEY AGREE'" in source,
              "and where nothing IS found, the command says out loud that an "
              "empty result is not agreement - it is also what an empty scope "
              "and an uncovered question produce (D-52)")
        print()

        print("7. One store at a time - the crossing stays small, MEASURED")
        # COUNTED, NOT GREPPED. The first version of this check looked for a
        # function name in the source and broke the moment the function was
        # renamed - which proves nothing about how many stores are open. This
        # wraps open_scope and watches.
        live = {"now": 0, "worst": 0}
        real_open = SCOPE.open_scope

        def counting_open(scope, project=None):
            store = real_open(scope, project)
            live["now"] += 1
            live["worst"] = max(live["worst"], live["now"])
            real_close = store.close

            def close():
                live["now"] -= 1
                return real_close()

            store.close = close
            return store

        SCOPE.open_scope = counting_open
        try:
            C.disagreements(question, scopes=[SCOPE.COMPANY, SCOPE.PROJECT],
                            project="Tower B")
        finally:
            SCOPE.open_scope = real_open
        check(live["worst"] == 1,
              "at most ONE store was open at any moment across two scopes "
              "(worst seen: %d) - each is opened, reduced to values and closed "
              "before the next, so no scope's TEXT is ever in memory beside "
              "another's" % live["worst"])
        check(live["now"] == 0,
              "and every one of them was closed")
        check("never a clause" in source,
              "what crosses is a number and a clause number, and the module "
              "says so where a reader will find it (Golden Rule 5, D-33)")
        print()

        print("8. What is NOT built is recorded rather than half-built")
        check("write time" in source and "contractual" in source,
              "write-time detection is named as a CONTRACTUAL question - at "
              "ingest nothing has been asked of anybody, so reading another "
              "scope would be a crossing nothing authorises")
        check("Q-C" in source,
              "and the trust half (R-16, R-25) is named as blocked on Q-C, "
              "because two of its four signals do not exist")
    finally:
        os.environ.pop("HERON_KNOWLEDGE", None)
        shutil.rmtree(home, ignore_errors=True)
        shutil.rmtree(papers, ignore_errors=True)
    print()

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - two scopes answering one question with different numbers")
    print("now say so, and Heron still decides nothing between them.")
    print()
    print("It proves nothing about whether the two clauses are the SAME")
    print("REQUIREMENT. Heron compared millimetres, not meaning, and the")
    print("report says that rather than leaving it to be discovered.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
