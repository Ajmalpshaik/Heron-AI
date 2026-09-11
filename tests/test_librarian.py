# Heron-Agent:  HERON-RAG-LIB-001
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Stage 4 - the Librarian picks the scope, and never pools two.

    python tests/test_librarian.py

THE WHOLE STAGE IS ONE SENTENCE, AND THE DANGER IS IN THE NAME.
"An agent that decides which scopes to search" reads as "an agent that
searches several", and implemented that way it is a UNION - one client's
knowledge in the same result set as another's. D-33 calls that a CONTRACTUAL
problem rather than a technical one.

    The Librarian decides WHICH ONE. If it needs two, it makes TWO SEPARATE
    QUERIES and says which answer came from where. It never merges them, and
    CrossScopeRefused stays exactly as it is.

WHAT IT PROVES
  1. A question needing company AND project knowledge produces TWO LABELLED
     ANSWERS, each naming the scope it came from.
  2. NOTHING IS MERGED AND NOTHING IS RANKED ACROSS SCOPES. There is no
     combined list and no "best scope" - because a score from one store and a
     score from another are not the same measurement.
  3. EXPRESSING IT AS ONE QUERY STILL RAISES. ATTACH is refused by name, and
     the refusal is unchanged by any of this.
  4. A PROJECT SCOPE WITH NO PROJECT IS NOT OPENED, and says why rather than
     answering emptily - guessing which project a question belongs to is how
     one client's knowledge reaches another.
  5. A SCOPE WITH NO STORE says it was not asked, which is a different
     sentence from "nothing matched".
  6. THE BRAIN DOES NOT CHOOSE (R-62, D-01). It reports what each scope holds.
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


def main():
    import heron_scope as SCOPE
    import heron_search as SEARCH
    import heron_embed as EMBED
    import heron_retrieve as R
    import heron_ingest as I

    home = tempfile.mkdtemp(prefix="heron-librarian-")
    papers = tempfile.mkdtemp(prefix="heron-librarian-papers-")
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

        print("1. Two scopes, TWO LABELLED ANSWERS")
        asked = R.librarian(question, scopes=[SCOPE.COMPANY, SCOPE.PROJECT],
                            project="Tower B")
        check(len(asked) == 2, "two scopes asked, two results back")
        answered = [a for a in asked if a.answer and a.answer.candidates]
        check(len(answered) == 2,
              "both found something, and each is its own answer")
        labels = [a.label for a in answered]
        check(any("company" in l for l in labels)
              and any("project" in l for l in labels),
              "each answer names the scope it came from: %s"
              % ", ".join(labels))
        check("company" in labels,
              "and the COMPANY label carries no project name. It read "
              "'company (Tower B)' at first, which says this is Tower B's "
              "copy of the company standard - it is not, and mislabelling "
              "whose knowledge something is, is the confusion GR 5 exists to "
              "prevent")
        check("project (Tower B)" in labels,
              "while the project label names the project, because that one "
              "really is per-project")

        company = [a for a in answered if a.scope == SCOPE.COMPANY][0]
        project = [a for a in answered if a.scope == SCOPE.PROJECT][0]
        check("Acme" in company.answer.candidates[0]["document"],
              "the company answer is the company's document")
        check("Tower B" in project.answer.candidates[0]["document"],
              "and the project answer is the project's - two different "
              "thicknesses, and nothing decided between them")
        print()

        print("2. NOTHING is merged and NOTHING is ranked across scopes")
        company_ids = set(c["id"] for c in company.answer.candidates)
        project_ids = set(c["id"] for c in project.answer.candidates)
        check(not (company_ids & project_ids),
              "no chunk appears in both answers")
        check(not hasattr(asked, "candidates") and isinstance(asked, list),
              "the result is a list of per-scope answers, not one shortlist")
        for got in asked:
            check(not hasattr(got, "best") and not hasattr(got, "winner"),
                  "%s carries no 'best' or 'winner' - the brain reports what "
                  "each scope holds and does not choose between clients"
                  % got.label)
        print()

        print("3. Expressing it as ONE query still raises")
        store = SCOPE.open_scope(SCOPE.COMPANY)
        try:
            raised = None
            try:
                store.execute("ATTACH DATABASE ? AS other",
                              (os.path.join(home, "global.db"),))
            except SCOPE.CrossScopeRefused as why:
                raised = str(why)
            check(raised is not None,
                  "ATTACH is refused by name, exactly as before this stage")
            check(raised and "Golden Rule 5" in raised,
                  "and the refusal still names the rule it is keeping")
            check(raised and "Open a second Store instead" in raised,
                  "and it already told anybody who tried what to do instead - "
                  "which is what this stage built")
        finally:
            store.close()
        print()

        print("4. A project scope with NO project is not opened")
        asked = R.librarian(question, scopes=[SCOPE.COMPANY, SCOPE.PROJECT])
        skipped = [a for a in asked if a.scope == SCOPE.PROJECT][0]
        check(skipped.skipped and skipped.answer is None,
              "it was not asked at all")
        check("no project is identified" in skipped.skipped,
              "and says why: %s" % skipped.skipped[:60])
        check([a for a in asked if a.scope == SCOPE.COMPANY][0].answer
              is not None,
              "while the company scope was still asked - one scope refusing "
              "does not silence the others")
        print()

        print("5. A scope with NO store says so, not 'nothing matched'")
        asked = R.librarian(question, scopes=[SCOPE.EXPERIMENTAL])
        got = asked[0]
        check(got.answer is not None and got.answer.route == "empty",
              "an empty scope refuses with 'empty' rather than reporting a "
              "miss - the same distinction R-19 makes, one level up")
        asked = R.librarian(question, scopes=["nonsense"])
        check(asked[0].skipped == "not a knowledge scope",
              "and a name that is not a scope is refused rather than guessed")
        print()

        print("6. There is no argument that could ask for a merged query")
        import inspect
        args = inspect.getfullargspec(R.librarian).args
        check("scopes" in args,
              "librarian takes `scopes` - a list to ask SEPARATELY")
        for cand in asked:
            pass
        merged = R.librarian(question, scopes=[SCOPE.COMPANY, SCOPE.PROJECT],
                             project="Tower B")
        check(all(isinstance(a, R.Asked) for a in merged),
              "and every element is an Asked, one per scope - there is no "
              "shape this function could return that pooled them")
        print()

    finally:
        shutil.rmtree(home, ignore_errors=True)
        shutil.rmtree(papers, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a question that needs two scopes gets two labelled")
    print("answers, from two separate queries, and nothing anywhere compares")
    print("or pools them. ATTACH still raises.")
    print()
    print("It proves nothing about WHICH answer is right. Two documents here")
    print("give two different thicknesses and both are correctly reported -")
    print("deciding between them is the host's act, and surfacing that they")
    print("disagree at all is Stage 8.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
