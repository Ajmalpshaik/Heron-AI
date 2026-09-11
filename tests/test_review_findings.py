# Heron-Agent:  HERON-RAG-CIT-014, HERON-RAG-DIS-002, HERON-RAG-RNK-006
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
One check per defect an automated review found on 2026-09-11, so none returns.

Five rounds, each after the one before it was called done: 16, 15, 8, 7 and 8.

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
    import heron_ground as G
    import heron_graph as GRAPH
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
    check(reconcile.rstrip().endswith("_SYNCED.add(store.scope)"),
          "the scope is marked reconciled only AFTER the work succeeds - it "
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
    check("_backends(answer.route)" in brain_source,
          "brain.lookup() carries the backend state - the retrieval CLI "
          "printed it and the seam a host actually uses printed neither, so "
          "two machines could give two orders with nothing saying why")

    # AND IT REPORTS WHAT RAN, not what is installed. The first version asked
    # backend() after the search, so an identity or cache short circuit - which
    # runs neither route - still claimed both had answered.
    sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
    import heron_brain as BRAIN2
    for route in ("identity", "cache", "nothing"):
        said = BRAIN2._backends(route)
        check(said["nearness"] == "not used" and said["rerank"] == "not used",
              "the %r route reports both backends as 'not used' - it answers "
              "without searching, so there was no shortlist for either to "
              "touch" % route)
    check(BRAIN2._backends("hybrid")["rerank"] != "not used",
          "and a route that DID search reports the real backend")
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
    check("name = stamp()" in embed_source,
          "both index paths use it, so a changed encoder invalidates the "
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

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - every defect an automated review found on 2026-09-11 has")
    print("a check standing on it, and each one fails if it comes back.")
    print()
    print("It proves nothing about the defects NOBODY has found yet. FIVE")
    print("rounds of this review, each after the one before it was called")
    print("done, and each found real things - a value moved between two")
    print("requirements of one clause, and a whole multi-scope path that two")
    print("stages built and no host could reach.")
    print("That is the honest measure of what a green suite is worth.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
