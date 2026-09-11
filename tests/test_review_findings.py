# Heron-Agent:  HERON-RAG-CIT-014, HERON-RAG-DIS-002, HERON-RAG-RNK-006
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
One check per defect an automated review found on 2026-09-11, so none returns.

Six rounds, each after the one before it was called done: 16, 15, 8, 7, 8 and 9.

    python tests/test_review_findings.py

WHY THESE ARE TOGETHER RATHER THAN SPREAD ACROSS SEVEN SUITES
--------------------------------------------------------------
Each suite here already tests what its module was BUILT to do. These test what
its module was found to do INSTEAD - and the two read differently. A reader
asking "what went wrong once, and what stops it now" gets one file, and the
sentence beside each check is the failure in the words it was reported in.

They are ordinary checks, not a log: every one fails if the defect comes back.

THE ONE THAT MATTERS MOST IS FIRST. The fabrication check was endorsing a
quotation that REVERSED its source - "No ducts shall..." quoted as "All ducts
shall..." scored 0.982 against a 0.90 gate and came back grounded. A checker
that passes the opposite of a clause is worse than no checker, because the
answer now carries a citation AND a clean report.
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


LONG_CLAUSE = ("No ducts shall be installed within the ceiling void unless a "
               "maintenance access panel is provided adjacent to each damper "
               "and the panel is not less than 450mm square")


def main():
    import heron_conflict as C
    import heron_ground as G
    import heron_graph as GRAPH
    import heron_search as SEARCH
    import heron_ingest as I
    import heron_retrieve as R
    import heron_rerank as RERANK
    import heron_graph as GRAPH
    import heron_scope as SCOPE
    import heron_search as SEARCH
    import heron_embed as EMBED
    import heron_context as CTX

    print("1. A QUOTATION THAT REVERSES ITS SOURCE IS NOT GROUNDED")
    reversed_quote = ('The clause says "All ducts shall be installed within '
                      'the ceiling void unless a maintenance access panel is '
                      'provided adjacent to each damper and the panel is not '
                      'less than 450mm square" [c]')
    got = G.coverage(reversed_quote, LONG_CLAUSE)
    check(got < G.THRESHOLDS[G.QUOTE],
          "the reversed quotation (%.3f) is below the gate (%.2f) - it used "
          "to score above a gate of 0.90 because the unchanged TAIL of a long "
          "quote dominated a longest-common-run ratio"
          % (got, G.THRESHOLDS[G.QUOTE]))
    check(G.coverage('It says "a continuous vapour barrier shall be applied" [c]',
                     "A continuous vapour barrier shall be applied over the "
                     "insulation.") == 1.0,
          "and a quotation that IS in its clause still scores exactly 1.0, "
          "which is what the original measurement recorded")
    check(G.THRESHOLDS[G.QUOTE] == 1.0,
          "the gate is containment, not 0.90 - the measurement that set 0.90 "
          "recorded 1.000 and 0.265 with nothing between, so the slack was "
          "never evidence")
    print()

    print("2. A CLAIM WITH THE NEGATION TAKEN OUT IS REVERSED, NOT GROUNDED")
    check(G.reverses("Duct insulation shall exceed 25 mm",
                     "Duct insulation shall not exceed 25mm"),
          "'shall exceed' against 'shall not exceed' is caught - both yield "
          "the single fact 25mm, so the added-fact rule passed it and the "
          "ratio was HIGH rather than low")
    check(not G.reverses("Insulation is limited to 25mm",
                         "Duct insulation shall not exceed 25mm"),
          "a genuine paraphrase that reads the same way is NOT caught - the "
          "rule is structural and fires only when the negation is the ONLY "
          "difference, so it cannot flood the report")
    check(G.REVERSED not in G.PASSES,
          "and a reversal does not pass")

    # THROUGH check(), NOT JUST THROUGH THE HELPER. The first version of
    # reverses() passed every unit check above and still returned GROUNDED end
    # to end, because normalise() keeps the full stop a source sentence ends
    # with and the claim had none - so the two word lists never matched. A
    # helper that works and a check that works are different claims.
    part = CTX.Part(CTX.STANDARD, "QCS 9.1.1",
                    CTX.as_quoted_source("Duct insulation shall not exceed "
                                         "25mm.", "QCS 2014", "9.1.1"),
                    "s", "w",
                    citation={"chunk": "c1", "document": "QCS 2014",
                              "locator": "9.1.1", "path": "/tmp/x.md"},
                    evidence="Duct insulation shall not exceed 25mm.")

    class _Packet(object):
        parts = [part]

    report = G.check("Duct insulation shall exceed 25mm [c1]", _Packet())
    check(report.claims[0].verdict == G.REVERSED and not report.ok,
          "a served draft with the negation removed comes back REVERSED and "
          "the report is not ok")
    report = G.check("Duct insulation shall not exceed 25mm [c1]", _Packet())
    check(report.ok,
          "and the same claim WITH the negation still passes, so the rule has "
          "not simply started failing everything")
    print()

    print("3. A FACT IS A FACT WITH A SIGN, AND A BARE COUNT IS ONE TOO")
    check(G.facts("a fall of -200mm") != G.facts("a fall of 200mm"),
          "-200mm and 200mm are different facts - the pattern started at the "
          "digit, so a reversed gradient passed")
    check(G.facts("Install 99 supports"),
          "'install 99 supports' is checkable - with no unit, no clause "
          "number and no year it produced no fact at all and was SKIPPED, so "
          "a wrong count against a clause requiring 2 was reported ok")
    print()

    print("4. A LABEL ON A CLAUSE IS NOT EVIDENCE FOR IT")
    wrapped = CTX.as_quoted_source("Ducts shall be insulated.", "QCS 2014",
                                   "9.1.1")
    check("2014" in G.normalise(wrapped),
          "the rendered part carries the title, so its year is in the text a "
          "reader sees")
    part = CTX.Part(CTX.STANDARD, "n", wrapped, "s", "w",
                    citation={"chunk": "c1", "document": "QCS 2014",
                              "locator": "9.1.1", "path": "/tmp/x.md"},
                    evidence="Ducts shall be insulated.")
    check("2014" not in G.facts(part.evidence),
          "but the EVIDENCE is the clause alone, so the title's year cannot "
          "ground a claim about Revit 2014 that the clause never made")
    check(part.citation.get("path"),
          "and the citation carries the file path (R-22) - it named a title "
          "and a clause number and nothing a person could open")
    print()

    print("5. NOTHING DOWNLOADS A MODEL WITHOUT SAYING SO FIRST")
    said = RERANK.fetch(confirmed=False)
    check("Nothing was downloaded" in said,
          "fetch() without a yes downloads nothing")
    check(RERANK.SIZE in said,
          "and shows the size first - warm() used to go straight to "
          "CrossEncoder(), which FETCHES the weights, so production start "
          "could pull gigabytes with nobody told")
    import inspect
    source = inspect.getsource(RERANK._load)
    check("offline=True" in source,
          "and the automatic path loads offline-only, so it can never start "
          "a download by itself")
    print()

    print("6. THE CONTEST DOES NOT BLAME STATUS FOR GAPS STATUS CANNOT MAKE")
    span = R.QUALITY_SPAN / R.ONE_RANK
    check(span < 1.0,
          "the quality nudge spans %.2f of one rank across OFFERABLE "
          "statuses, so it cannot produce every gap below 1.0" % span)

    def pair(top, second):
        made = []
        for i, value in enumerate((top, second)):
            got = R.Candidate("f%d" % i, {"capability": "c", "status": "DRAFT",
                                          "semantic_identity": "s",
                                          "domain": "d"})
            got.fused = value
            got.keyword_rank = i + 1
            made.append(got)
        return made

    wide = R.Contest(pair(0.30, 0.30 - 0.8 * R.ONE_RANK), 200, 20, 40)
    check("status alone" not in wide.sentence(),
          "a 0.8-rank gap is NOT explained by status, because status cannot "
          "move that far - it used to say it could")
    narrow = R.Contest(pair(0.30, 0.30 - 0.2 * R.ONE_RANK), 200, 20, 40)
    check("status alone" in narrow.sentence(),
          "and a 0.2-rank gap still is, because status can")
    print()

    print("7. A RE-RANKED REPORT SAYS HOW MANY IT ACTUALLY READ")
    RERANK._CACHE[:] = [lambda pairs: [float(i) for i in range(len(pairs))]]
    many = [R.Candidate("f%02d" % i, {"capability": "c", "status": "DRAFT",
                                      "semantic_identity": "s", "domain": "d"})
            for i in range(20)]
    for i, got in enumerate(many):
        got.fused = 1.0 - i * 0.01
        got.keyword_rank = i + 1
    ranked = R._rerank("a question", many, R._fragment_passage)
    contest = R.Contest(ranked[:5], 200, 20, 40)
    check("it read 20" in contest.sentence(),
          "with limit=5 the report says the re-ranker read 20 - it used to "
          "call five candidates 'the shortlist it was given'")
    check("over THESE 5" in contest.sentence(),
          "and says the fusion numbers are over the five in hand")
    RERANK._CACHE[:] = [None]
    print()

    print("8. A SENTENCE END INSIDE THE LIMIT BEATS A BLANK LINE PAST IT")
    body = ("Alpha beta gamma delta epsilon zeta. Eta theta iota kappa lambda "
            "mu. Nu xi omicron pi rho sigma.\n\nTau upsilon phi chi psi omega.")
    pieces = I._cut(body, limit=80)
    check(all(len(piece) <= 80 for piece in pieces),
          "every piece is inside the limit - the search used to stop at the "
          "first blank line past the limit and skip every sentence end before "
          "it, returning one oversized chunk")
    print()

    print("9. MORE FORMS OF QUALIFICATION ARE RECOGNISED BEFORE A SPLIT")
    for opener in ("subject to", "notwithstanding", "with the exception of",
                   "excluding"):
        check(I.starts_a_qualification("%s the above, ducts shall be lagged"
                                       % opener),
              "'%s' begins a qualification - a rule cut away from it states "
              "the opposite of its source, with a citation attached" % opener)
    print()

    print("10. A CORRUPT .docx IS A NAMED REFUSAL, NOT A TRACEBACK")
    import zipfile
    hold = tempfile.mkdtemp(prefix="heron-review-")
    try:
        bad = os.path.join(hold, "broken.docx")
        with zipfile.ZipFile(bad, "w") as archive:
            archive.writestr("word/document.xml", "<w:document><w:body>")
        refused = None
        try:
            I.READERS[".docx"](bad)
        except I.UnreadableDocument as why:
            refused = str(why)
        check(refused and "broken.docx" in refused,
              "a valid zip with truncated XML is refused BY NAME - the parse "
              "sat outside the guard and came out as a traceback")
    finally:
        shutil.rmtree(hold, ignore_errors=True)
    print()

    print("11. A DOTTED MEASUREMENT IS NOT A CLAUSE REFERENCE")
    # "1.5m" never matched at all - there is no word boundary between the
    # digit and the unit - so the real shapes are the SPACED one and the
    # percentage, and those did match.
    for text in ("The duct shall fall 1.5 m to the riser.",
                 "A gradient of 2.5% is required."):
        match = GRAPH._CLAUSE_REFERENCE.search(text)
        check(match and not GRAPH._is_clause_reference(text, match),
              "%r is a measurement - in a document that also numbers a clause "
              "%s this made an edge between two unrelated clauses, and "
              "inflated the density count that decides whether the route is "
              "viable" % (text, match.group(0) if match else "?"))
    cited = "Labelling shall be in accordance with clause 1.5 throughout."
    match = GRAPH._CLAUSE_REFERENCE.search(cited)
    check(match and GRAPH._is_clause_reference(cited, match),
          "and 'in accordance with clause 1.5' still is")
    deep = "See 21.3.2 for the rest."
    match = GRAPH._CLAUSE_REFERENCE.search(deep)
    check(match and GRAPH._is_clause_reference(deep, match),
          "as is a three-segment number, which no measurement is written as")
    print()

    print("12. THE STORE, THE MANIFEST AND THE INDEX ALL STAY TRUE")
    home = tempfile.mkdtemp(prefix="heron-review-home-")
    papers = tempfile.mkdtemp(prefix="heron-review-papers-")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            SEARCH.ensure_tables(store)
            EMBED.ensure_tables(store)

            plain = os.path.join(papers, "notes.txt")
            with open(plain, "w", encoding="utf-8") as handle:
                handle.write("Ducts shall be insulated to 25mm.\n\n"
                             "A vapour barrier shall be continuous.\n")
            got = I.ingest(store, plain, added_by="tests")
            locators = [r["locator"] for r in store.execute(
                "SELECT locator FROM chunks WHERE document_id = ?",
                (got.document_id,)).fetchall()]
            check(all(locator for locator in locators),
                  "a document with no headings still gives every chunk a "
                  "locator - they were stored EMPTY, and a chunk with no "
                  "locator is offered as a citable source that cites nothing")
            check(any(locator.startswith("para-") for locator in locators),
                  "and it is a position a person can count to, not a clause "
                  "number it has invented")

            print()
            print("13. RE-INGESTING APPLIES WHAT THE CALLER ASKED FOR")
            again = I.ingest(store, plain, status="REVIEWED", title="Site Notes")
            row = store.execute("SELECT status, title FROM documents WHERE id = ?",
                                (got.document_id,)).fetchone()
            check(again.reused and row["status"] == "REVIEWED",
                  "a re-ingest as REVIEWED moves the row - it returned early "
                  "and dropped the status, and nothing else anywhere moved a "
                  "document's lifecycle")
            check(row["title"] == "Site Notes",
                  "and an explicit title is applied too")
            plain_status = I.ingest(store, plain)
            row = store.execute("SELECT status FROM documents WHERE id = ?",
                                (got.document_id,)).fetchone()
            check(plain_status.reused and row["status"] == "REVIEWED",
                  "while a re-ingest that says NOTHING about status leaves it "
                  "alone, rather than demoting it back to DRAFT")

            print()
            print("14. A MOVED FILE IS RECORDED WHERE RECOVERY READS")
            moved = os.path.join(papers, "renamed.txt")
            shutil.move(plain, moved)
            I.ingest(store, moved)
            paths = [line.get("path") for line in I.manifest(store)]
            check(moved in paths,
                  "the manifest carries the NEW path - it recorded only the "
                  "first one, so restore() looked for a file that had moved "
                  "and restored nothing")

            print()
            print("15. THE DERIVED INDEX COMES BACK WHEN IT IS DELETED")
            SEARCH.index_chunks(store)
            first = store.execute(
                "SELECT COUNT(*) AS n FROM chunk_text").fetchone()["n"]
            store.execute("DELETE FROM chunk_text")
            store.db.commit()
            SEARCH.index_chunks(store)
            back = store.execute(
                "SELECT COUNT(*) AS n FROM chunk_text").fetchone()["n"]
            check(first and back == first,
                  "index_chunks() rebuilds an emptied table - skipping on a "
                  "source fingerprint alone left it empty and silently "
                  "unsearchable, which is Golden Rule 11 broken by an "
                  "optimisation")

            print()
            print("16. RESTORE PUTS THE DOCUMENT BACK UNDER ITS OWN NAME")
            store.execute("DELETE FROM documents")
            store.execute("DELETE FROM chunks")
            store.db.commit()
            out = I.restore(store)
            row = store.execute("SELECT title FROM documents").fetchone()
            check(out.reingested and row and row["title"] == "Site Notes",
                  "the title the manifest recorded is the title it comes back "
                  "with - restore() omitted it and let ingest() re-derive one "
                  "from the filename, changing every citation written against "
                  "it")
        finally:
            store.close()
    finally:
        os.environ.pop("HERON_KNOWLEDGE", None)
        shutil.rmtree(home, ignore_errors=True)
        shutil.rmtree(papers, ignore_errors=True)
    print()

    print("17. THE CHECK AND THE CITATION REACH A CONVERSATION")
    sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
    import heron_tools as TOOLS
    check("heron_check" in TOOLS.TOOLS,
          "heron_check is a registered tool - heron_ground.check() was "
          "complete, tested, and callable only from a command line with a "
          "file on disk, so no served answer ever went through it")
    import heron_brain as BRAIN
    check(hasattr(BRAIN, "check_answer"),
          "and the brain has the seam it is served through")
    server_source = open(os.path.join(ROOT, "mcp", "server",
                                      "heron_mcp_server.py"),
                         encoding="utf-8").read()
    check('part.get("citation")' in server_source,
          "the MCP renderer reads the citation - it was put in the dictionary "
          "and dropped by the formatter, so a host got a quoted clause with "
          "no marker to cite it by")
    print()

    print("18. A VALUE MOVED BETWEEN SUBJECTS DOES NOT PASS")
    two = "Duct insulation shall be 25mm. Pipe insulation shall be 50mm."

    def packet_of(text):
        made = CTX.Part(CTX.STANDARD, "n",
                        CTX.as_quoted_source(text, "Spec", "9.1"), "s", "w",
                        citation={"chunk": "c1", "document": "Spec",
                                  "locator": "9.1", "path": "/tmp/x.md"},
                        evidence=text)

        class _P(object):
            parts = [made]

        return _P()

    report = G.check("Duct insulation shall be 50mm [c1]", packet_of(two))
    check(report.claims[0].verdict == G.MISPLACED and not report.ok,
          "50mm claimed for ducts is MISPLACED - it IS in the chunk, under "
          "pipes, and facts() over the whole chunk let a requirement move "
          "between subjects and still report ok")
    for right in ("Duct insulation shall be 25mm [c1]",
                  "Pipe insulation shall be 50mm [c1]"):
        check(G.check(right, packet_of(two)).ok,
              "and %r still passes, so the rule has not started failing "
              "everything" % right[:34])
    print()

    print("19. NO PROCESS-WIDE OFFLINE FLAGS, EVER")
    source = inspect.getsource(RERANK)
    check("os.environ[key]" not in source and "HF_HUB_OFFLINE\"" not in source,
          "the re-ranker sets no huggingface environment variable - it did, "
          "around its own load, and heron_brain.warm() starts the ENCODER on "
          "another thread at the same moment, so the encoder could see the "
          "flag, fail its download and cache None for the process")
    check("local_files_only" in source,
          "it uses a per-load keyword instead, which no other loader can see")
    print()

    print("20. ONE BAD MOMENT DOES NOT DISABLE MAINTENANCE FOR THE PROCESS")
    brain_source = open(os.path.join(ROOT, "mcp", "server", "heron_brain.py"),
                        encoding="utf-8").read()
    reconcile = brain_source[brain_source.index("def _reconcile"):]
    reconcile = reconcile[:reconcile.index("class _Open")]
    check(reconcile.rstrip().endswith("_SYNCED.add(key)"),
          "the store is marked reconciled only AFTER the work succeeds - it "
          "was marked before the attempt, so one locked database made every "
          "later request in the process skip refresh until a restart")
    print()

    print("21. A PROJECT KEY CANNOT REACH A SHARED SCOPE")
    hold = tempfile.mkdtemp(prefix="heron-review-scope-")
    papers2 = tempfile.mkdtemp(prefix="heron-review-scope-p-")
    os.environ["HERON_KNOWLEDGE"] = hold
    try:
        doc = os.path.join(papers2, "spec.md")
        with open(doc, "w", encoding="utf-8") as handle:
            handle.write("Ducts shall be insulated.\n")
        code = I.main([doc, "--project", "Tower"])
        check(code == 2,
              "--project with no --scope project is REFUSED - scope defaulted "
              "to global and the key was still handed to open_scope(), so one "
              "forgotten flag put project knowledge in the shared store and "
              "overwrote its project metadata (Golden Rule 5)")
        check(I.main([doc, "--scope", "project", "--project", "Tower"]) == 0,
              "and naming both still works")
    finally:
        os.environ.pop("HERON_KNOWLEDGE", None)
        shutil.rmtree(hold, ignore_errors=True)
        shutil.rmtree(papers2, ignore_errors=True)
    print()

    print("22. A TITLE SURVIVES A REFRESH, AND A TOMBSTONE IS REPORTED")
    hold = tempfile.mkdtemp(prefix="heron-review-refresh-")
    papers3 = tempfile.mkdtemp(prefix="heron-review-refresh-p-")
    os.environ["HERON_KNOWLEDGE"] = hold
    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            SEARCH.ensure_tables(store)
            EMBED.ensure_tables(store)
            doc = os.path.join(papers3, "spec.md")
            with open(doc, "w", encoding="utf-8") as handle:
                handle.write("Ducts shall be insulated to 25mm.\n")
            first = I.ingest(store, doc, title="Acme Standard 2026",
                             added_by="tests")
            with open(doc, "a", encoding="utf-8") as handle:
                handle.write("\nA vapour barrier shall be applied.\n")
            I.refresh(store)
            fresh = store.execute(
                "SELECT title FROM documents WHERE status != 'RETIRED'"
            ).fetchone()
            check(fresh and fresh["title"] == "Acme Standard 2026",
                  "a refreshed document keeps the title it was ingested "
                  "under - refresh() dropped it, so the correctly named row "
                  "was retired and its replacement took a filename")

            gone, remembered = I.forget(store, first.document_id)
            check(remembered is True,
                  "forget() reports whether its TOMBSTONE reached the "
                  "manifest - it ignored that, so an unwritable manifest let "
                  "a later restore bring back the document somebody "
                  "deliberately removed, while forget reported success")
        finally:
            store.close()
    finally:
        os.environ.pop("HERON_KNOWLEDGE", None)
        shutil.rmtree(hold, ignore_errors=True)
        shutil.rmtree(papers3, ignore_errors=True)
    print()

    print("23. THE SERVED LOOKUP SAYS WHICH BACKENDS ANSWERED")
    check("_backends(answer)" in brain_source,
          "brain.lookup() carries the backend state - the retrieval CLI "
          "printed it and the seam a host actually uses printed neither, so "
          "two machines could give two orders with nothing saying why")

    # AND IT REPORTS WHAT RAN, not what is installed. The first version asked
    # backend() after the search, so an identity or cache short circuit - which
    # runs neither route - still claimed both had answered.
    sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
    import heron_brain as BRAIN2
    for route in ("identity", "cache", "nothing"):
        said = BRAIN2._backends(SEARCH.Answer(route))
        check(said["nearness"] == "not used" and said["rerank"] == "not used",
              "the %r route reports both backends as 'not used' - it answers "
              "without searching, so there was no shortlist for either to "
              "touch" % route)

    # AND IT READS WHAT THE RUN RECORDED, never what the machine has now.
    # Gating on the route fixed only the short circuits: a warm-up finishing
    # between find() and the question still labelled a lexical answer "model",
    # and a loaded re-ranker that returned no scores was reported as having
    # re-read the shortlist. Round seven, 2026-09-11.
    check("def ran(" in inspect.getsource(R),
          "heron_retrieve records which backends produced a rank, in the "
          "function that produced it")
    made_up = SEARCH.Answer("hybrid", backends={
        "nearness": "lexical", "nearness_why": "measured on this run",
        "rerank": "not used", "rerank_why": "nothing re-read this shortlist"})
    check(BRAIN2._backends(made_up)["nearness"] == "lexical",
          "and the seam repeats the run's own record rather than asking a "
          "backend a second question with a different answer")
    check(BRAIN2._backends(SEARCH.Answer("hybrid"))["rerank"] == "not said",
          "a searching route that recorded nothing says so - naming a backend "
          "there would be a sentence about the machine wearing the words of a "
          "sentence about the answer")
    check('found.get("backends")' in server_source,
          "and the tool renders it")
    print()

    print("24. A VECTOR REMEMBERS WHICH MODEL MADE IT")
    stamp = EMBED.stamp()
    check(stamp,
          "vectors are stamped with %r rather than just the backend name - "
          "'model' covered every trained encoder, so changing "
          "HERON_EMBED_MODEL left unchanged chunks holding vectors from the "
          "OLD one: at a different dimension nearest() discards them all, at "
          "the same one it computes meaningless cross-model dot products, and "
          "nothing says so either way" % stamp)
    check(EMBED.MODEL not in stamp or ":" in stamp,
          "and where the backend IS a trained model the stamp names WHICH")
    embed_source = inspect.getsource(EMBED)
    check(embed_source.count("name, embed = encoder()") == 2,
          "both index paths take the stamp and the encoder TOGETHER, so a "
          "changed encoder invalidates the "
          "cache instead of satisfying it")
    print()

    print("25. A MOVED-THEN-FORGOTTEN DOCUMENT STAYS FORGOTTEN")
    hold = tempfile.mkdtemp(prefix="heron-review-move-")
    papers4 = tempfile.mkdtemp(prefix="heron-review-move-p-")
    os.environ["HERON_KNOWLEDGE"] = hold
    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            SEARCH.ensure_tables(store)
            EMBED.ensure_tables(store)
            first = os.path.join(papers4, "spec.md")
            with open(first, "w", encoding="utf-8") as handle:
                handle.write("Spec\n\n1.1 Rule\n\nDucts to 25mm.\n")
            got = I.ingest(store, first, added_by="tests")
            moved = os.path.join(papers4, "renamed.md")
            shutil.move(first, moved)
            I.ingest(store, moved)
            I.forget(store, got.document_id)

            store.execute("DELETE FROM documents")
            store.execute("DELETE FROM chunks")
            store.db.commit()
            out = I.restore(store)
            check(not out.reingested,
                  "a document moved from A to B and then forgotten is NOT "
                  "resurrected - the manifest was reconciled BY PATH, so A "
                  "and B kept separate histories and A's newest event was "
                  "still 'ingested'. Rebuilding the store brought back "
                  "exactly what somebody had removed")
            check(any("forgotten" in reason for _p, reason in out.skipped),
                  "and it is skipped by name, because it was forgotten on "
                  "purpose")
        finally:
            store.close()
    finally:
        os.environ.pop("HERON_KNOWLEDGE", None)
        shutil.rmtree(hold, ignore_errors=True)
        shutil.rmtree(papers4, ignore_errors=True)
    print()

    print("26. A REFUSAL AND A DEFECT DO NOT COME BACK LOOKING THE SAME")
    check("except brain.ContextRefused as why" in server_source,
          "heron_check catches the NAMED refusal - it caught every Exception, "
          "so a TypeError or a malformed store came back wearing the words of "
          "a normal answer and the transport was never told the call failed")
    check("except Exception as why" not in
          server_source[server_source.index("def heron_check"):
                        server_source.index("def heron_standards")],
          "and nothing blanket is left in that handler")
    check("raise ContextRefused(str(why))" in brain_source,
          "and check_answer() translates the packet's own refusals, which is "
          "what makes the narrow catch possible at all")
    print()

    print("27. A BROKEN STORE IS NOT A ZERO-DENSITY GRAPH")
    graph_source = inspect.getsource(GRAPH)
    check('if "no such table" not in str(exc)' in graph_source,
          "document_neighbours() re-raises anything but a missing table - a "
          "locked database returned {} and document_density() then reported a "
          "zero-chunk, zero-density corpus, so the Stage 5 measurement that "
          "decides whether the graph route is ever worth a vote could record "
          "a clean empty reading where no measurement ran")
    print()

    print("28. AN UNSPLITTABLE OPENING DOES NOT SWALLOW THE REST")
    body = ("A" * 200) + ". Beta gamma delta. Epsilon zeta eta.\n\nTheta iota."
    pieces = I._cut(body, limit=80)
    check(len(pieces) > 1,
          "a 200-character prefix with no legal cut in it no longer collapses "
          "the whole body into one chunk - the loop ENDED there, so however "
          "many clean paragraph breaks came later, the rest of the section "
          "became one enormous poorly-retrievable piece")
    check(any(len(piece) <= 80 for piece in pieces),
          "and everything after the prefix is split normally")
    print()

    print("29. A BYTE-ORDER MARK DOES NOT HIDE THE FIRST HEADING")
    hold = tempfile.mkdtemp(prefix="heron-review-bom-")
    try:
        bom = os.path.join(hold, "bom.md")
        with open(bom, "wb") as handle:
            handle.write(u"\ufeff# Section 3 Ductwork\n\nDucts to 25mm.\n"
                         .encode("utf-8"))
        text = I.READERS[".md"](bom)
        check(not text.startswith(u"\ufeff"),
              "utf-8-sig is tried FIRST - plain utf-8 ACCEPTS a BOM and keeps "
              "it as a character, so the utf-8-sig branch was unreachable and "
              "an invisible mark sat in front of the first heading, stopping "
              "its regex matching. Windows tools write that BOM by default")
        check(text.startswith("# Section"),
              "so the document's top-level structure survives")
    finally:
        shutil.rmtree(hold, ignore_errors=True)
    print()

    print("30. A LENGTH-SPLIT CONTINUATION IS A SIBLING, NOT ITS OWN CHILD")
    long_body = "Ducts shall be lagged. " * 200
    made = I.chunk_document("Spec\n\n3.1 Insulation\n\n" + long_body, "Spec")
    parts = [c for c in made if c.locator == "3.1"]
    check(len(parts) > 1, "the clause was split for length")
    check(len(set(c.parent_key for c in parts)) == 1,
          "every piece keeps the SAME parent - continuations used to point at "
          "the first piece while keeping its depth, so a row was a child of "
          "something at its own level and anything reading the hierarchy got "
          "two different trees")
    check(len(set(c.depth for c in parts)) == 1,
          "and they are all at one depth, which is what being siblings means")
    print()

    print("31. 30mm AND 30.0mm ARE ONE MEASUREMENT")
    check(C.same_number("30") == C.same_number("30.0"),
          "they compare equal - the raw strings differed, so two sources that "
          "AGREE were reported as a disagreement. A flag on nothing is what "
          "teaches people to stop reading flags")
    check(C.same_number("2.5") == "2.5",
          "and a real fraction is not flattened")
    print()

    print("32. ONE SEARCH PER SCOPE ON THE SERVED PATH")
    check("asked=asked" in brain_source,
          "standards() hands the Librarian's answer to the conflict check - "
          "without it every served request searched, and where a "
          "cross-encoder is installed re-ranked, every scope TWICE, and the "
          "two shortlists could differ so the disagreement shown was about "
          "other clauses than the ones listed above it")
    check("asked=None" in inspect.getsource(C.disagreements),
          "and disagreements() still asks for itself when nobody hands it one")
    print()

    print("33. THE STANDARDS ANSWER CARRIES THE CLAUSE AND AN OPENABLE CITE")
    check("_with_text" in brain_source,
          "the clause body reaches the caller - the tool's closing line said "
          "'both clauses are above, each with its own citation' while the "
          "payload held a title, a locator and a ranking reason: nothing to "
          "read and nothing to open")
    check('c.get("path")' in server_source and '"        | %s"' in server_source,
          "and the renderer prints the file path and the quoted clause")
    print()

    print("34. THE PROJECT KEY IS THE KEY, NOT THE DISPLAY TITLE")
    # PAIRED, NOT COUNTED. The first version asserted "== 2" and went stale the
    # moment Stage 9 added a third tool that gets it right - a typed count of
    # something a command can derive, which is the rule this repository states
    # about its own prose and had not applied to its own test.
    keys = server_source.count("project=pinned.project_key")
    names = server_source.count("project_name=pinned.title")
    check(keys >= 2,
          "the calls that OPEN a project store pass the stable key, which is "
          "what DocumentPin's own docstring says identity is: 'Title alone is "
          "NOT identity', written after two Revit sessions here both had a "
          "document called Project1")
    check(keys == names,
          "and every one of them carries the display name BESIDE it rather "
          "than instead of it (%d keys, %d names) - a store's name and a "
          "modeller's word for the building are two facts, and the first fix "
          "for this swapped one for the other" % (keys, names))
    check("depth=depth or None, project=pinned.title)" in server_source,
          "while heron_context gets the NAME: it opens no project store and "
          "renders `project` straight into the situation line, so passing the "
          "Project Information UniqueId put an opaque identifier on the one "
          "line that exists to say which building this is")

    # AND THE KEY IS THE ONE heron_scope DEFINES, OBTAINED IN A READ-ONLY
    # CONVERSATION. Round seven found that the previous fix moved a name and
    # established nothing: pinned.check() ran in ONE tool, the write preview,
    # so a chat that only ever read had no pin at all - and key_of() built a
    # PATH-based key while heron_scope names the project store after the
    # Project Information UniqueId.
    from heron_write import DocumentPin
    unpinned = DocumentPin()
    check(unpinned.project_key is None,
          "a pin nobody has set names no project store, and says None rather "
          "than a default - D-33: guessing writes one client's knowledge into "
          "another's file")
    by_path = DocumentPin()
    by_path.check({"document": "Tower B", "documentPath": "C:/x/TowerB.rvt"})
    check(by_path.is_pinned and by_path.project_key is None,
          "a path pins the CHAT to one model (Golden Rule 20) and still names "
          "no store - rename the file and a path-named store is orphaned")
    real = DocumentPin()
    real.check({"document": "Tower B", "documentPath": "C:/x/TowerB.rvt",
                "projectKey": "1a2b3c-0000-4d5e"})
    check(real.project_key == "1a2b3c-0000-4d5e",
          "and the Project Information UniqueId the add-in now sends is what "
          "a project store is named after - it survives save, rename and move")
    check("Json.Str(\"projectKey\"" in open(
              os.path.join(ROOT, "revit", "Heron.Revit.Addin",
                           "RevitOperations.cs"), encoding="utf-8").read(),
          "the add-in SENDS it - it computed this identity for its own "
          "preview pairing all along and never put it on the wire, so the "
          "server had no way to obtain one")
    server_pin = server_source[server_source.index("def revit_select_by_category"):]
    server_pin = server_pin[:server_pin.index("def revit_use_session")]
    check("pinned.check(reply)" in server_pin,
          "and a READ tool establishes the pin - it was set in the write "
          "preview alone, so a conversation that never previewed a move "
          "skipped the project store every time and said nothing")
    print()

    print("35. A BROKEN STORE IS NOT AN EMPTY INDEX EITHER")
    search_source = inspect.getsource(SEARCH)
    check(search_source.count('if "no such table" not in str(exc)') >= 2,
          "index_chunks() and chunk_keywords() re-raise anything but a missing "
          "table - index_chunks() read a locked database as 'nothing ingested', "
          "returned 0 and LEFT the old chunk_text in place, so retrieval went "
          "on answering from stale clauses. THIRD copy of this pattern: "
          "documents() was fixed in one round, the graph in another, and this "
          "one got neither")
    print()

    print("36. A RETIRED REVISION CANNOT HIDE THE CURRENT STANDARD")
    check("d.status IN" in search_source,
          "the lifecycle filter is inside the words query, before its LIMIT - "
          "it ran afterwards, so a document with a long retained history could "
          "fill the whole fetched window with its own old revisions and the "
          "current standard, ranking just below them, disappeared")
    print()

    # -----------------------------------------------------------------
    # Round seven, 2026-09-11. Thirteen findings, every one real.
    # -----------------------------------------------------------------

    print("37. TWO QUOTED SPANS ARE TWO CLAIMS, NOT ONE LONG ONE")
    both_exact = 'The clause says "ducts" shall be "insulated" [c]'
    check(G.coverage(both_exact, "Ductwork: ducts shall be insulated.") == 1.0,
          "two separately quoted excerpts of one clause both pass - they were "
          "joined with a space and that invented phrase was looked for whole, "
          "so a sentence whose every quotation was exact came back flagged")
    one_wrong = 'The clause says "ducts" shall be "painted red" [c]'
    check(G.coverage(one_wrong, "Ductwork: ducts shall be insulated.") < 1.0,
          "and a claim where ONE span is fabricated is still caught - the "
          "combination is the worst span, not the average, so a true "
          "quotation beside a false one cannot carry it")
    print()

    print("38. BREADTH AND ELIGIBLE ARE COUNTED OVER THE SAME CORPUS")
    check("statuses=OFFERABLE_DOCUMENTS" in inspect.getsource(R),
          "the document breadth query applies the same lifecycle filter the "
          "eligible count does - it counted every row in the index, retired "
          "revisions included, so a long history pushed breadth above "
          "eligible and the report said the words route had ranked the whole "
          "library when it had picked one clause out of it")
    check("keep=keep" in inspect.getsource(R.find),
          "and the fragment breadth is counted over what the structured "
          "filter left, not over the whole FTS table")
    check(R.OFFERABLE_DOCUMENTS is SEARCH.OFFERABLE_DOCUMENTS,
          "one lifecycle rule, in one place - it was two literal tuples in "
          "two modules, and two copies of a rule is a rule that can disagree "
          "with itself")
    print()

    print("39. A BROKEN STORE IS NEVER A PLAUSIBLE ZERO")
    retrieve_source = inspect.getsource(R)
    check(retrieve_source.count('if "no such table" not in str(exc)') >= 2,
          "find_documents()' document COUNT re-raises anything but a missing "
          "table - a locked or malformed store counted as zero documents and "
          "returned the `empty` route, which tells the reader 'nothing has "
          "been put in yet' about a store that is full and broken (D-52)")
    check('if "no such table" not in str(exc)' in inspect.getsource(EMBED),
          "and so does the meaning index - the fourth copy of this shape, "
          "found by grepping for it rather than by waiting for the next "
          "review to name it")
    print()

    print("40. TWO DOCUMENTS ARE TWO SOURCES EVEN IN ONE SCOPE")
    # BEHAVIOURAL, because the old guard's text survives in the comment that
    # explains it - and a check that greps for a line it expects to be gone
    # passes the day somebody quotes it.
    home7 = tempfile.mkdtemp(prefix="heron-r7-one-scope-")
    papers7 = tempfile.mkdtemp(prefix="heron-r7-one-scope-p-")
    os.environ["HERON_KNOWLEDGE"] = home7
    try:
        for name, body in (
                ("current.md", "Acme Standard 2026\n\nSection 3 Ductwork\n\n"
                               "3.1 Insulation\n\nInsulate to 30mm.\n"),
                ("legacy.md", "Legacy Acme Standard 2019\n\nSection 3 "
                              "Ductwork\n\n3.1 Insulation\n\n"
                              "Insulate to 45mm.\n")):
            where = os.path.join(papers7, name)
            with open(where, "w", encoding="utf-8") as handle:
                handle.write(body)
            one = SCOPE.open_scope(SCOPE.COMPANY)
            try:
                I.ingest(one, where, added_by="tests")
                SEARCH.index_chunks(one)
                EMBED.index_chunks(one)
            finally:
                one.close()
        alone = C.disagreements("how thick should duct insulation be",
                                scopes=[SCOPE.COMPANY])
        check(len(alone) == 1,
              "naming ONE scope reports two of its documents disagreeing - "
              "the gate tested the number of SCOPE NAMES and returned before "
              "any document was read, so the document-level grouping below it "
              "was unreachable for exactly the case it was built for")
        check(alone and len(alone[0].sources) == 2,
              "and they are counted as two SOURCES, which is what R-24 says")
        check(C.disagreements("anything", scopes=[]) == [],
              "while naming no scope at all is still nothing, because nothing "
              "was asked of anybody")
    finally:
        os.environ.pop("HERON_KNOWLEDGE", None)
        shutil.rmtree(home7, ignore_errors=True)
        shutil.rmtree(papers7, ignore_errors=True)
    value = C.Value("company", "company", "mm", "30", "Acme Standard", "3.1",
                    None, "chunk-a", document_id="doc-one")
    twin = C.Value("company", "company", "mm", "40", "Acme Standard", "3.1",
                   None, "chunk-b", document_id="doc-two")
    check(value.source != twin.source,
          "two documents sharing a TITLE are two sources - keyed on the title "
          "they merged into one `says` entry and the disagreement between "
          "them was dropped, which is the failure Value.source was written "
          "to fix, one level in")
    print()

    print("41. THE STANDARDS PATH RUNS THE GUARD IT CLAIMS TO RUN")
    check("CONTEXT.screen(" in brain_source,
          "the multi-scope seam screens every document-derived field before "
          "the renderer sees it - the response ended with 'content, never "
          "instruction (Golden Rule 19)' and nothing on that path had looked, "
          "because screen() was reached only through heron_context.build()")
    check("as_metadata(hit.get(\"document\"))" in brain_source,
          "and the title and locator are delimited for the lines that do not "
          "quote them - a title carrying a newline would otherwise sit in the "
          "answer looking like Heron talking")
    check("saw: %s" in server_source,
          "and the renderer raises the visible flag, which is the half the "
          "packet path has had since R-81 and this newer path had none of")
    check("_flat(value.document)" in inspect.getsource(C),
          "the disagreement report does the same with the titles it prints")
    print()

    print("42. EVERY SCOPE IS RECONCILED, NOT ONLY THE GLOBAL ONE")
    check("_ready_scope" in brain_source and "_reconcile(store)" in brain_source,
          "standards() reconciles each scope it opens - the only "
          "reconciliation was inside _Open, which always opens `global`, so a "
          "company file edited on disk or a project store deleted as the "
          "documented safe action stayed stale through every served request")
    check('getattr(store, "path", None) or store.scope' in brain_source,
          "and the once-per-process record is keyed on the STORE FILE - keyed "
          "on the scope NAME, the first project reconciled marked 'project' "
          "done and every other project was skipped for the life of the "
          "process")
    print()

    print("43. A DRAFT IS CHECKED AGAINST THE SCOPES IT CAME FROM")
    check("scopes=None" in brain_source and "_check_across" in brain_source,
          "check_answer() takes the scopes that supplied the evidence - it "
          "opened the global store and only that, so a draft written from "
          "heron_standards cited chunks global has never heard of and every "
          "marker came back unresolved, or the reassembly refused outright")
    across = brain_source[brain_source.index("def _check_across"):]
    across = across[:across.index("def _ready_scope")]
    check("store.close()" in across and across.count("CONTEXT.assemble") == 1,
          "and no packet ever holds two scopes' clauses - one store is opened "
          "at a time and closed before the next, which is the discipline "
          "heron_conflict keeps (D-33, Golden Rule 5)")
    check("scopes=named" in server_source,
          "and the tool passes them through, so the workflow the check exists "
          "for can actually use it")
    print()

    print("44. A MISTYPED FLAG NEVER PUBLISHES TO THE GLOBAL STORE")
    ingest_source = inspect.getsource(I)
    main_body = ingest_source[ingest_source.index("def _main"):]
    main_body = main_body[:main_body.index("def _flag")] \
        if "def _flag" in main_body else main_body
    check('stray = [a for a in argv if a.startswith("-")]' in ingest_source,
          "every argument still wearing a dash after the known flags are "
          "taken out is refused - `--scop company spec.pdf`, one letter "
          "short, left scope at its default and ingested the file into the "
          "SHARED store, exiting 0 because something had been ingested")
    check(ingest_source.index('show = _flag(argv, "--boundaries")')
          < ingest_source.index('stray = [a for a in argv'),
          "and the check sits BELOW the parsing - written above it, it read "
          "argv before _flag() had taken anything out and refused "
          "`--boundaries`, a flag this tool has. Caught by the suite within "
          "a minute")
    print()

    print("45. A RETIRED REVISION SURVIVES THE STORE BEING DELETED")
    check('_remember(store, "retired"' in ingest_source,
          "refresh() records the retirement beside the store - the retired "
          "row was the only record a previous revision had ever existed, and "
          "the manifest held one line per path which now points at the NEW "
          "bytes, so the database was authoritative for history while Golden "
          "Rule 11 declares it disposable")
    check("_restore_retired" in ingest_source,
          "and restore() puts it back as a ROW: its id, its title, when it "
          "was retired and what replaced it")
    check("text_recoverable" in ingest_source,
          "and says plainly that its TEXT is not recoverable - the source was "
          "overwritten in place and Q-B says Heron points at a file and never "
          "copies it, so inventing chunks for it would be the worse answer")
    print()

    print("46. THE TOOL INVENTORY IS DERIVED, NEVER TYPED")
    tools_source = open(os.path.join(ROOT, "mcp", "server", "heron_tools.py"),
                        encoding="utf-8").read()
    check("def main(" in tools_source,
          "there is a command that prints every declared MCP tool from the "
          "registry the server enforces")
    readme = open(os.path.join(ROOT, "brain", "README.md"),
                  encoding="utf-8").read()
    check("mcp/server/heron_tools.py" in readme,
          "brain's README names that command - it said 'Four MCP tools' and "
          "listed them, and heron_check and heron_standards made it a stale "
          "description of which surfaces a caller can reach")
    opened = server_source.index('"""')
    header = server_source[opened:server_source.index('"""', opened + 3)]
    check("revit_use_this_model      reads" not in header
          and "mcp/server/heron_tools.py" in header,
          "and the server's own module header does too - it listed seven "
          "while the registry held sixteen. A hand-typed inventory of "
          "callable surfaces is a security claim with a half-life")
    print()

    # -----------------------------------------------------------------
    # Round eight, 2026-09-11. Nine findings, every one real.
    # -----------------------------------------------------------------

    print("47. BRACKETS ARE NOT A WAY OUT OF THE FABRICATION CHECK")
    check(G.facts("Use [50mm] insulation [abc123def:0001]") == ["50mm"],
          "a value in brackets is still a fact - every bracketed span counted "
          "as a citation marker and was stripped before facts() looked, so "
          "`Use [50mm] insulation [chunk]` had no facts, was SKIPPED, and the "
          "report said ok against a clause requiring 25mm")
    check(G.facts("Insulate to 25mm [9.1.1].") == ["25mm"]
          and G._MARKER.findall("Insulate to 25mm [9.1.1].") == ["9.1.1"],
          "and a clause number in brackets is still a citation - it IS a fact "
          "pattern, which is the whole reason markers are stripped, so the "
          "disqualifier is a UNIT and nothing wider")
    check(G._MARKER.findall("cited [99]", known=set(["99"])) == ["99"]
          and G._MARKER.findall("claims [50mm]", known=set(["50mm"])) == ["50mm"],
          "and a span the packet actually carries is a citation whatever it "
          "looks like, which is what keeps a caller's own short chunk ids "
          "working")
    print()

    print("48. THE SELECTION CANNOT MOVE BEFORE THE PIN IS CHECKED")
    ops = open(os.path.join(ROOT, "revit", "Heron.Revit.Addin",
                            "RevitOperations.cs"), encoding="utf-8").read()
    guard = ops.index("expectProject")
    check(guard < ops.index("uiDoc.Selection.SetElementIds(found)"),
          "the add-in compares the expected project key BEFORE it touches the "
          "selection - the server compared its pin against the REPLY, which "
          "arrives after SetElementIds has run, so switching model mid-chat "
          "highlighted the wrong building's ducts and then said nothing had "
          "been sent to Revit")
    check('"expectProject": pinned.project_key or ""' in server_source,
          "and the server sends the key with the request rather than checking "
          "it afterwards. Empty means 'do not check': a first request has "
          "nothing to compare against, and refusing it would make the pin "
          "unobtainable")
    print()

    print("49. AN EXPLICIT SESSION SWITCH MOVES THE PIN")
    switch = server_source[server_source.index("def revit_use_session"):]
    switch = switch[:switch.index("def revit_preview_move")]
    check("pinned.repin(reply)" in switch and "pinned.check(reply)" not in switch,
          "choosing a session repins - check() pins on FIRST sight and "
          "otherwise returns a refusal without moving anything, and this line "
          "called it and threw the refusal away: the binding moved, the pin "
          "did not, the answer said 'Now working with', and every tool that "
          "touches a model then refused for a reason nothing had told anybody")
    print()

    print("50. THE KEY NAMES THE STORE, THE TITLE NAMES THE BUILDING")
    lib = inspect.getsource(R.librarian)
    check("project_name" in lib and "(project_name or project)" in lib,
          "the librarian labels the project scope with the NAME - since the "
          "key became the Project Information UniqueId, every answer came "
          "back labelled `project (a7f3c2e1-0000-...)` instead of "
          "`project (Tower B)`")
    check("remember_label" in brain_source,
          "and the folder index learns that name, so a project store on disk "
          "is not an unreadable filename")
    print()

    print("51. A SCOPE THE GROUNDING CHECK COULD NOT OPEN IS NAMED")
    check("not a knowledge scope, or it could not be " in brain_source,
          "a scope that could not be opened is reported rather than dropped - "
          "`scopes=\"company,proejct\"`, one letter short, produced an `ok` "
          "COMPANY-ONLY report with nothing saying the other named source was "
          "never looked at. The same shape as the mistyped ingest flag, one "
          "tool along")
    print()

    print("52. A DOCUMENT IS NOT LOST BECAUSE ITS NEWEST PATH IS")
    check("earlier.setdefault" in ingest_source and "fallback" in ingest_source,
          "restore() tries the earlier paths a document was ingested at - a "
          "document id is a CONTENT hash, so the same standard at A and later "
          "at B is one id with two paths, and keeping only B lost the "
          "document when B was deleted while its bytes still sat at A")
    print()

    print("53. THE SAME MEASUREMENT IN TWO UNITS IS ONE MEASUREMENT")
    check(C.comparable("mm", "30") == ("length", "30")
          and C.comparable("cm", "4") == ("length", "40"),
          "30mm and 4cm are compared - grouped on the literal unit string "
          "they landed in separate groups, were never compared, and two "
          "clauses prescribing different thicknesses came back agreeing by "
          "silence")
    check(C.comparable("gradient", "2:200") == C.comparable("gradient", "1:100"),
          "and 1:100 against 2:200 is NOT a disagreement - comparing the "
          "strings reported two sources contradicting each other about a fall "
          "they agree on, which is a flag on nothing")
    check(C.comparable("dn", "50") == ("dn", "50"),
          "while a unit nothing knows how to convert keeps its own group, "
          "which is exactly what grouping on the literal string always did")
    print()

    print("54. A NUMERIC RANGE IS NOT A CLAUSE REFERENCE")
    ranged = "spacing varies from 1.5 to 2.5 times the diameter"
    check(not any(GRAPH._is_clause_reference(ranged, m)
                  for m in GRAPH._CLAUSE_REFERENCE.finditer(ranged)),
          "a bare 'to' no longer cites - every numeric RANGE in a standard is "
          "written this way, and standards are mostly ranges, so the false "
          "edges landed exactly where they are densest: in the number that "
          "decides whether the graph route is viable at all")
    for cites in ("refer to 2.5 for the detail", "according to 4.1",
                  "shall conform to 5.2", "see 4.1", "as per 3.2"):
        check(any(GRAPH._is_clause_reference(cites, m)
                  for m in GRAPH._CLAUSE_REFERENCE.finditer(cites)),
              "while %r still does" % cites)
    print()

    print("55. ONE INDEXING PASS, ONE ENCODER, ONE NAME")
    check("def encoder(" in embed_source,
          "the stamp and the encoder are one decision - stamp() was read once "
          "at the top of a pass while vector() consulted the loaded model per "
          "row, so a warm-up finishing mid-pass stored MODEL vectors under "
          "the name `lexical` in the same table")
    check("_pack(vector(" not in embed_source,
          "and neither index path reaches past it to the live model")
    print()

    # -----------------------------------------------------------------
    # Round nine, 2026-09-11. Nine findings, every one real.
    # -----------------------------------------------------------------

    print("56. A NAMED FACT CANNOT LEAVE THROUGH THE BRACKETS EITHER")
    for bracketed, fact in (("[DN100]", "dn100"),
                            ("[OST_DuctCurves]", "ost_ductcurves")):
        sentence = "Use %s pipe [abc12345:0001]" % bracketed
        check(fact in G.facts(sentence),
              "%s is a fact, not a citation - the round before refused "
              "anything carrying a UNIT, which only caught numeric-leading "
              "measurements while a nominal bore, a Revit category and a "
              "parameter name all walked past it" % bracketed)
    check(G._MARKER.findall("Use [DN100] pipe [abc12345:0001]")
          == ["abc12345:0001"],
          "and the real chunk id is still the citation")
    check(G._MARKER.findall("cited [c1]", known=set(["c1"])) == ["c1"],
          "while a short id the shape does not know is still a citation when "
          "the PACKET carries it - a fact about the packet, not a guess")
    print()

    print("57. THE PACKET REACHES EVERY MARKER-STRIPPING SITE")
    ground_source = inspect.getsource(G)
    check(ground_source.count('_MARKER.sub(" ", text or "")') == 0
          and ground_source.count('_MARKER.sub(" ", sentence or "")') == 0,
          "no marker is stripped without the packet - kind_of, _bare and "
          "_negations each stripped blind, so narrowing the marker shape left "
          "'[c1]' in the word list and reverses() STOPPED FIRING. That put "
          "back the round-three defect where a reversed clause passed. Caught "
          "by the check standing on that finding, inside a minute")
    print()

    print("58. A SENTENCE IS CHECKED AGAINST EVERY SOURCE IT CITES")
    def _two(cid, text, loc):
        return CTX.Part(CTX.STANDARD, "x",
                        CTX.as_quoted_source(text, "D", loc), "s", "w",
                        citation={"chunk": cid, "document": "D",
                                  "locator": loc, "path": "/x"},
                        evidence=text)

    class _Both(object):
        parts = [_two("a", "Duct insulation shall be 25mm.", "4.1"),
                 _two("b", "Duct insulation shall be 50mm.", "4.2")]

    both = G.check("Company requires 25mm [a], but project requires 50mm [b].",
                   _Both())
    check(both.claims[0].verdict == G.GROUNDED,
          "the sentence a standards answer most needs to write - two clauses, "
          "two citations - passes. _cited() returned on the FIRST marker, so "
          "50mm was reported as invented and the gate rejected the natural "
          "way to report exactly the disagreement Stage 8 surfaces")
    invented = G.check("Company requires 25mm [a], but project requires "
                       "80mm [b].", _Both())
    check(invented.claims[0].verdict == G.FLAGGED
          and "80mm" in invented.claims[0].added,
          "and a value in NEITHER cited clause is still flagged, which is the "
          "rule that matters")
    print()

    print("59. RANKING DOES NOT DECIDE WHETHER EVIDENCE IS CHECKABLE")
    check("_carry_cited" in brain_source and "cited_ids" in brain_source,
          "the chunks a draft CITES are fetched by id - check_answer "
          "reassembles the packet by re-running retrieval, so a clause that "
          "heron_standards had shown could fall out of the new top-five when "
          "a re-ranker warmed or a document was ingested between the two "
          "calls, and the report said UNRESOLVED about a real clause still "
          "sitting in the store")
    print()

    print("60. SCOPE ORDER DOES NOT DECIDE A GROUNDING VERDICT")
    check("more than one scope carries this citation" in brain_source,
          "two scopes that both resolve a bare locator and DISAGREE is "
          "ambiguous, not first-wins - company 4.1 at 25mm against project "
          "4.1 at 50mm passed one way round and flagged the identical draft "
          "the other. The docstring had recorded that as a limit instead of "
          "fixing it")
    print()

    print("61. THE ENCODER IS READ BEFORE IT IS USED, NOT AFTER")
    retrieve_src = inspect.getsource(R)
    before = retrieve_src.index("backend_name, backend_why = EMBED.backend()")
    check(before < retrieve_src.index("for fragment_id, score in EMBED.nearest"),
          "the nearness backend is taken BEFORE the route runs - read "
          "afterwards, a warm-up finishing mid-request meant the scores came "
          "from the lexical encoder while both the fusion weight and the "
          "recorded name said `model`. The round before moved the REPORT onto "
          "the candidate and left the poll where it was: it fixed who was "
          "asked, not when")
    print()

    print("62. A RESTORED FALLBACK HAS TO BE THE RIGHT BYTES")
    check("file_hash(was) != wanted_id" in ingest_source,
          "an earlier path is accepted only when its content hash matches the "
          "manifest - taking any path that still EXISTED restored SITE SAFETY "
          "NOTES under the title 'Acme Standard 2026' and reported success, "
          "because a path gets reused. Measured on that exact code. The "
          "document id IS the content hash, so the check is one comparison "
          "and the comment had stood in for it")
    print()

    print("63. AN UNOPENED SCOPE IS A PREREQUISITE, NOT A MISS")
    research_source = open(os.path.join(ROOT, "brain", "heron_research.py"),
                           encoding="utf-8").read()
    check("return not self.skipped and self.route in FOUND_NOTHING"
          in research_source,
          "a skipped scope is not a miss - an empty company store plus an "
          "unopened project store made the gap CERTAIN and the brief said "
          "Heron knows nothing, while the project specification sat unopened")
    # ON THE DEFECT, NOT ON THE PROSE. The first version of this checked for a
    # sentence in the brief and failed because the source wraps it across two
    # lines - the same substring trap that broke the UNVERIFIED check a round
    # earlier. What matters is that the line which CHOSE a scope is gone.
    check("scopes[0] if scopes else" not in research_source,
          "the brief names no ingest destination - taking the FIRST SCOPE "
          "SEARCHED printed `--scope global` under a company standard, Golden "
          "Rule 5 broken by list order")
    check("at + len(document)" in research_source,
          "and an edition is searched for at the DOCUMENT, so a delivery date "
          "elsewhere in the sentence cannot satisfy the citation contract")
    print()

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - every defect an automated review found on 2026-09-11 has")
    print("a check standing on it, and each one fails if it comes back.")
    print()
    print("It proves nothing about the defects NOBODY has found yet. NINE")
    print("rounds of this review, each after the one before it was called")
    print("done, and each found real things - a value moved between two")
    print("requirements of one clause, a whole multi-scope path that two")
    print("stages built and no host could reach, and a display title passed")
    print("where a stable key was required, in the class written to stop")
    print("exactly that. That is the honest measure of a green suite.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
