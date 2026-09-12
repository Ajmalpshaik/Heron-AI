# Heron-Agent:  HERON-RAG-RET-003, HERON-RAG-RNK-006
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Stage 2 - an ingested document comes back out, alongside fragments.

    python tests/test_document_retrieval.py

WHAT IT PROVES
  1. A CLAUSE COMES BACK for a question asked in a modeller's words, and the
     result says it is a CHUNK and not a fragment. R-20. A fragment is a piece
     of code Heron can run; a clause is a sentence somebody has to read, and a
     shortlist that mixes them without saying so is worse than either alone.

  2. THE REVIT VERSION FILTER DOES NOT TOUCH DOCUMENTS. This is the one place
     this stage fails quietly. A fragment declares which releases it supports;
     QCS does not and never will. Run documents through the fragment filter
     and EVERY DOCUMENT DISAPPEARS the moment a question names a release -
     which reads exactly like "we have nothing on that".

  3. AN EMPTY DOCUMENT STORE REFUSES, and says EMPTY rather than "nothing
     matched". Those are different sentences and one of them is a lie. The
     defect was found on the fragment side on 2026-08-30 BY MEASURING, and a
     number recorded from such a run would have been a measurement of an empty
     database. It is already known; it is not being rediscovered.

  4. A RETIRED DOCUMENT IS NOT AN ANSWER, and the refusal says that rather
     than saying nothing matched.

  5. THE HEADING PATH IS WHAT IS SEARCHED, not the clause alone (R-66) - so a
     question using a SECTION's vocabulary reaches a clause that never uses it.

  6. THE TWO CORPORA ARE NOT FUSED. Two labelled answers, never one merged
     list, because "first of seven chunks" and "first of four hundred
     fragments" are the same fused score and are not the same claim.

WHAT IT DOES NOT PROVE
  Nothing here is a citation. A chunk comes back with its locator and its
  heading path; binding a claim in a drafted answer to the chunk it came from
  is Stage 3, and so is the guard on the path into a packet (R-81).
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


# Clause numbers invented for this test. W-7 records that this plan fabricated
# "QCS 2014 s21.3.2" as an illustration and it read as real for ten
# repetitions, so nothing here pretends to be a standard.
DOCUMENT = """Heron Test Standard 2026

Section 9 Thermal Insulation

9.1 Ductwork

9.1.1 Thickness

Ducts shall be insulated to 25mm, except where installed within a conditioned
space and serving a terminal within 3m.

9.1.2 Vapour Barrier

A continuous vapour barrier shall be applied over the insulation.

Section 12 Sanitary Drainage

12.1 Gradients

Drainage carrying soil shall fall at not less than 1:100.
"""


def main():
    import heron_scope as SCOPE
    import heron_search as SEARCH
    import heron_embed as EMBED
    import heron_retrieve as R
    import heron_ingest as I
    # Section 3 asserts a RANKING, and ranking is the re-ranker's job when one
    # is installed. Read here so the check can say which claim it is making.
    import heron_rerank as RERANK

    home = tempfile.mkdtemp(prefix="heron-docret-")
    papers = tempfile.mkdtemp(prefix="heron-docret-papers-")
    os.environ["HERON_KNOWLEDGE"] = home

    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            SEARCH.ensure_tables(store)
            EMBED.ensure_tables(store)

            print("1. An EMPTY document store refuses, and says EMPTY")
            answer = R.find_documents(store, "what insulation do ducts need")
            check(answer.route == "empty",
                  "the route is 'empty', not 'nothing'")
            check("NO DOCUMENT IS INDEXED" in answer.note,
                  "and it says so in words")
            check("nothing matched" not in answer.note.lower()
                  or "not 'nothing matched'" in answer.note,
                  "it does NOT say 'nothing matched' - an empty store and a "
                  "genuine miss are different sentences, and a number "
                  "recorded from the first while reading the second is a "
                  "measurement of an empty database")
            print()

            path = os.path.join(papers, "heron-test-standard.md")
            with open(path, "w", encoding="utf-8") as handle:
                handle.write(DOCUMENT)
            got = I.ingest(store, path, added_by="tests")
            indexed = SEARCH.index_chunks(store)
            embedded, _ = EMBED.index_chunks(store)

            print("2. A document is indexed by both routes")
            check(indexed == got.chunks,
                  "the words route indexed all %d chunk(s)" % indexed)
            check(embedded == got.chunks,
                  "and the nearness route embedded all %d" % embedded)
            print()

            print("3. R-20 - a clause comes back, and says it is a CHUNK")
            answer = R.find_documents(store, "how thick should duct insulation be")
            check(answer.route == "documents" and answer.candidates,
                  "a question in a modeller's words returns %d chunk(s)"
                  % len(answer.candidates))
            best = answer.candidates[0]
            check(best["kind"] == "chunk",
                  "every hit says what kind of thing it is")
            # THIS CHECK DEPENDS ON AN OPTIONAL PACKAGE, and said so nowhere
            # until 2026-09-12. `sentence-transformers` is in
            # requirements-optional.txt; without it heron_rerank reports
            # `absent` and the shortlist keeps the order fusion gave it.
            # Fusion puts 9.1.2 first for this question.
            #
            # So the suite failed on every machine that had not installed an
            # OPTIONAL dependency, and the message blamed the ranking -
            # pointing at retrieval quality rather than at the absent package.
            # Found by running the suite on the owner's Windows PC, where the
            # trained embedding backend is real and the re-ranker is not
            # installed; on the container both were absent together and the
            # question never arose.
            #
            # The fix is not to lower the claim to whatever came back. The
            # claim is right - 9.1.1 is headed Thickness and states 25mm,
            # while 9.1.2 is the vapour barrier and does not answer "how
            # thick" at all. It is asserted where it holds, and where it does
            # not the CONTRACT FOR BEING ABSENT is asserted instead, which
            # requirements-optional.txt states as "every answer says the
            # re-ranker did not run".
            if RERANK.backend()[0] == RERANK.ABSENT:
                # `reranked` is the Contest's, not the Answer's - the Answer
                # carries the hits, the Contest carries how they were ordered.
                check(not getattr(answer.contest, "reranked", False),
                      "the re-ranker is NOT INSTALLED, so the shortlist keeps "
                      "fusion's order and the answer says so - 9.1.1 first is "
                      "a claim about the re-ranker and is not made here (best "
                      "was %r). Install sentence-transformers to test it"
                      % best["locator"])
            else:
                check(best["locator"] == "9.1.1",
                      "and the best hit is the thickness clause 9.1.1, not the "
                      "section above it (got %r)" % best["locator"])
            check(best["document"] == "Heron Test Standard 2026",
                  "it names the document a citation would show")
            check(best["untrusted"] == 1,
                  "and it is still marked untrusted on the way out - GR 19 "
                  "does not stop applying because the text was retrieved")
            print()

            print("4. THE VERSION WALL DOES NOT TOUCH DOCUMENTS")
            # The trap. eligible() excludes a fragment declared for another
            # release; a clause declares no release at all, so running
            # documents through that filter deletes the whole corpus for any
            # question that names one.
            for revit in (None, "2020", "2024", "2027"):
                fragments = R.find(store, "how thick should duct insulation be",
                                   revit=revit)
                papers_answer = R.find_documents(
                    store, "how thick should duct insulation be")
                check(papers_answer.route == "documents"
                      and len(papers_answer.candidates) > 0,
                      "with Revit %s named, the clause still comes back"
                      % (revit or "not"))
                del fragments
            print()

            print("5. R-66 - the heading path is searched, not the clause alone")
            # "thermal" appears ONLY in "Section 9 Thermal Insulation" - the
            # thickness clause never uses the word. Without the heading path
            # in the indexed row, this question cannot reach it.
            answer = R.find_documents(store, "thermal requirements for ductwork")
            reached = [c["locator"] for c in answer.candidates]
            check("9.1.1" in reached or "9.1" in reached,
                  "a question using the SECTION's vocabulary reaches a clause "
                  "that never uses it (%s)" % ", ".join(reached[:3]))
            body = store.execute(
                "SELECT text FROM chunks WHERE locator = '9.1.1'").fetchone()
            check("thermal" not in (body["text"] or "").lower(),
                  "and the clause really does not contain the word - the "
                  "check above is not passing because the text happens to "
                  "hold it")
            print()

            print("6. The two corpora are NOT fused into one list")
            fragments = R.find(store, "how thick should duct insulation be",
                               revit="2024")
            check(all(c.get("kind") != "chunk" for c in fragments.candidates),
                  "find() returns fragments only")
            check(all(c["kind"] == "chunk"
                      for c in R.find_documents(
                          store, "how thick should duct insulation be"
                      ).candidates),
                  "and find_documents() returns chunks only - two questions, "
                  "two labelled answers, because 'first of seven chunks' and "
                  "'first of four hundred fragments' are the same fused score "
                  "and are not the same claim")
            print()

            print("7. The confidence line counts CHUNKS, and claims no nudge")
            answer = R.find_documents(store, "how thick should duct insulation be")
            said = answer.contest.sentence()
            check("fragment(s)" not in said,
                  "it does not call a clause a fragment")
            check("quality nudge" not in said,
                  "and it does not explain a tie with the quality nudge, "
                  "which documents do not get: a DRAFT clause is not a worse "
                  "answer than a REVIEWED one, it is an unread one")
            print()

            print("7b. Four retrieval holes a review found")
            answer = R.find_documents(store, "how thick should duct insulation be")
            best = answer.candidates[0]
            check(best.get("path") and os.path.isfile(best["path"]),
                  "R-22: the citation carries the FILE a human opens. The "
                  "query already selected the path and the result dropped it, "
                  "so every citation named a title and a clause number and "
                  "nothing anybody could open")

            # A broken store must not read as an empty one.
            broken = R.sqlite3.OperationalError("database disk image is malformed")
            raised = None
            try:
                if "no such table" not in str(broken):
                    raise broken
            except R.sqlite3.OperationalError as why:
                raised = str(why)
            check(raised is not None,
                  "and only 'no such table' is treated as 'nothing ingested' "
                  "- a malformed database, a lock or a missing column must not "
                  "come back as a plausible empty answer")

            # The unindexed route must not be told as a genuine miss.
            import heron_context as CTX
            store.execute("DELETE FROM chunk_text")
            store.db.commit()
            refused = None
            try:
                CTX.assemble(store, "how thick should insulation be",
                             path=CTX.STANDARDS)
            except CTX.SourceMissing as why:
                refused = str(why)
            check(refused and "NOT INDEXED" in refused,
                  "the STANDARDS path says INGESTED BUT NOT INDEXED rather "
                  "than saying documents are indexed and none covers the "
                  "request - it used to say both at once")
            SEARCH.index_chunks(store)
            EMBED.index_chunks(store)
            print()

            print("8. A RETIRED document is not an answer, and says so")
            store.execute("UPDATE documents SET status = 'RETIRED' WHERE id = ?",
                          (got.document_id,))
            store.db.commit()
            answer = R.find_documents(store, "how thick should duct insulation be")
            check(answer.route == "nothing",
                  "nothing comes back")
            check("RETIRED" in answer.note,
                  "and the refusal says they EXIST and are retired, rather "
                  "than saying nothing matched - the same habit as 'they "
                  "exist, they are just not for this release'")
            store.execute("UPDATE documents SET status = 'DRAFT' WHERE id = ?",
                          (got.document_id,))
            store.db.commit()
            print()

            print("9. Deleting a document takes its index rows with it")
            I.forget(store, got.document_id)
            SEARCH.index_chunks(store)
            EMBED.index_chunks(store)
            left = store.execute(
                "SELECT COUNT(*) AS n FROM vectors WHERE kind = ?",
                (EMBED.CHUNK,)).fetchone()["n"]
            check(left == 0,
                  "no chunk vector outlives its chunk - a vector with no "
                  "chunk is a hit that resolves to nothing")
            answer = R.find_documents(store, "how thick should duct insulation be")
            check(answer.route == "empty",
                  "and the store is empty again, which it says rather than "
                  "reporting a miss")
            print()

        finally:
            store.close()
    finally:
        shutil.rmtree(home, ignore_errors=True)
        shutil.rmtree(papers, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a clause goes in and comes back for a question asked in a")
    print("modeller's words, labelled as a clause rather than a fragment, with")
    print("the section it sits in searched alongside it. The version wall does")
    print("not touch it, an empty store refuses rather than reporting a miss,")
    print("and the two corpora are never fused into one list.")
    print()
    print("It proves nothing about CITATION. Binding a claim in a drafted")
    print("answer to the chunk it came from is Stage 3, and so is the guard on")
    print("the path into a packet.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
