# Heron-Agent:  HERON-RAG-DIS-002
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Stage 1 - a document can go into a scope.

    python tests/test_ingest.py

THE FIRST TWO CHECKS ARE THE POINT OF THE FILE, and they are first because
02-implementation.md s4.3 says to write them before the chunker exists:

  1. A RULE AND ITS EXCEPTION STAY IN ONE CHUNK (R-68). Split "ducts shall be
     insulated ... except where installed within conditioned spaces" and Heron
     states the OPPOSITE of the requirement WITH A CITATION ATTACHED. That is
     the worst output this system is capable of - more convincing than any
     uncited guess - and QCS and Ashghal are written as rule-then-qualification
     throughout, so it is the normal case here and not an edge one.

  2. THE EXACT TOKENS ARE NEVER CUT (R-08). OST_DuctCurves, a
     BuiltInParameter, a shared-parameter GUID, "Revit 2024", a clause number.

Then: hierarchy at arbitrary depth (R-36, R-37), the heading path read from
structure and never generated (R-66, R-67), the refusal of a model file by
name (R-10), a free re-ingest (R-11), provenance (R-09), lifecycle (R-83),
the audit trail (R-84), untrusted marking (R-80), flagging without truncating
(R-82), and delete-and-rebuild losing nothing (R-12).

WHAT IT DOES NOT TEST, BECAUSE STAGE 1 DOES NOT DO IT
-----------------------------------------------------
Nothing is retrieved. A document goes in and can be counted; reading one back
out is Stage 2, and a citation is Stage 3.
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


# A numbered document, which is the ONE hard condition on a first document
# (Q-A): clause numbers are what make a citation testable on day one.
#
# The clause NUMBERS here are invented for this test and say so. W-7 records
# that this plan fabricated "QCS 2014 s21.3.2" as an illustration and it
# looked entirely real for ten repetitions - so nothing in this fixture
# pretends to be a real standard.
DOCUMENT = """Heron Test Standard 2026

This preamble sits before any numbered heading. It has no locator.

Section 4 Mechanical Works

4.1 Ductwork

General requirements for ductwork are given in this clause.

4.1.1 Insulation

Ducts shall be insulated to 25mm, except where installed within conditioned
spaces and the duct is within 3m of the terminal it serves.

4.1.2 Identification

Every duct shall carry the category OST_DuctCurves and the parameter
BuiltInParameter.RBS_DUCT_BOTTOM_ELEVATION shall be completed. The shared
parameter 3f2504e0-4f89-11d3-9a0c-0305e82c3301 shall be present in Revit 2024
and later, per Heron Test Standard 2026 4.1.2.

Section 5 Plumbing Works

5.1 Drainage

Drainage shall fall at 1:100 unless the authority requires otherwise.
"""


def write(folder, name, body):
    path = os.path.join(folder, name)
    with open(path, "w", encoding="utf-8") as handle:
        handle.write(body)
    return path


def main():
    import heron_ingest as I

    print("1. R-68 - a rule and its exception are NEVER in different chunks")
    rule = ("Ducts shall be insulated to 25mm, "
            + ("and the work shall be carried out by a competent person. " * 40)
            + "except where installed within conditioned spaces.")
    pieces = I._cut(rule, limit=200)
    holds = [p for p in pieces if "except where installed" in p]
    check(len(holds) == 1 and "shall be insulated" in holds[0],
          "the qualification stayed with the rule it qualifies, even though "
          "the text is %d characters against a limit of 200" % len(rule))
    check(not any(p.strip().lower().startswith("except") for p in pieces),
          "and no chunk BEGINS with 'except', which is what that failure "
          "would look like from the outside")

    for word in ("unless", "provided that", "save that", "however"):
        broken = ("The pipe shall be tested. " * 30) + word.capitalize() + \
                 " the authority directs otherwise."
        got = I._cut(broken, limit=200)
        check(not any(p.strip().lower().startswith(word) for p in got),
              "a split immediately before %r is refused too" % word)
    print()

    print("2. R-08 - the exact tokens BIM runs on survive whole")
    tokens = ["OST_DuctCurves", "BuiltInParameter.RBS_DUCT_BOTTOM_ELEVATION",
              "3f2504e0-4f89-11d3-9a0c-0305e82c3301", "Revit 2024", "4.1.2"]
    padded = ("Requirements are as follows. " * 12) + \
             " and ".join(tokens) + (" Further text follows here. " * 12)
    got = I._cut(padded, limit=120)
    joined = " || ".join(got)
    for token in tokens:
        check(any(token in piece for piece in got),
              "%r is whole in one chunk, not split across two" % token)
    check("||" in joined, "and the text really was cut somewhere - the check "
          "above is not passing because nothing was split at all")
    print()

    home = tempfile.mkdtemp(prefix="heron-ingest-")
    audit = tempfile.mkdtemp(prefix="heron-audit-")
    papers = tempfile.mkdtemp(prefix="heron-papers-")
    os.environ["HERON_KNOWLEDGE"] = home
    os.environ["HERON_AUDIT"] = audit

    import heron_scope as SCOPE

    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            path = write(papers, "heron-test-standard.md", DOCUMENT)
            got = I.ingest(store, path, added_by="tests",
                           source_trust="a fixture, not a standard")

            print("3. A numbered document goes in, and can be counted")
            check(got.chunks > 3,
                  "%d chunks from one document" % got.chunks)
            rows = I.boundaries(store, got.document_id)
            check(len(rows) == got.chunks,
                  "and every one of them is on the row, in reading order")
            print()

            print("4. R-36, R-37 - hierarchy at ARBITRARY depth, as a parent link")
            by_locator = dict((r["locator"], r) for r in rows if r["locator"])
            check("4.1.1" in by_locator, "the clause 4.1.1 is its own chunk")
            deep = by_locator["4.1.1"]
            parent = dict((r["id"], r) for r in rows)[deep["parent_id"]]
            check(parent["locator"] == "4.1",
                  "its parent is 4.1, taken from the document's OWN numbering "
                  "rather than from where it sat in a stack")
            grand = dict((r["id"], r) for r in rows)[parent["parent_id"]]
            check(grand["locator"] == "Section 4",
                  "and its grandparent is Section 4 - three levels, from one "
                  "parent_id column and no fixed Part/Section/Clause")
            check(deep["depth"] > parent["depth"] > grand["depth"],
                  "depth increases with the tree instead of being declared")
            print()

            print("5. R-66, R-67 - the heading path is READ, never generated")
            check("Section 4" in deep["heading_path"]
                  and "4.1" in deep["heading_path"]
                  and "Heron Test Standard" in deep["heading_path"],
                  "the clause carries its ancestors: %s"
                  % deep["heading_path"][:70])
            ancestors = [grand["locator"], parent["locator"]]
            check(all(a in deep["heading_path"] for a in ancestors),
                  "every locator in the path is one an ancestor row also "
                  "holds - so it cannot disagree with the hierarchy, which is "
                  "what 'derived, not generated' has to mean to be checkable")
            print()

            print("6. R-80 - every chunk of an ingested document is UNTRUSTED")
            marked = store.execute(
                "SELECT COUNT(*) AS n FROM chunks WHERE untrusted = 1 "
                "AND document_id = ?", (got.document_id,)).fetchone()["n"]
            check(marked == got.chunks,
                  "all %d carry it. Golden Rule 19: content from a document is "
                  "DATA, NEVER INSTRUCTION, and this is where that becomes a "
                  "column rather than a sentence" % marked)
            print()

            print("7. R-09 - provenance, and R-83 - a lifecycle")
            doc = store.execute("SELECT * FROM documents WHERE id = ?",
                                (got.document_id,)).fetchone()
            check(doc["path"] == path and os.path.isfile(doc["path"]),
                  "the store points AT the file and the file is still there")
            check(doc["added_by"] == "tests" and doc["added_utc"]
                  and doc["source_trust"],
                  "who added it, when, and who says it is authoritative")
            check(doc["status"] in I.STATUSES,
                  "and it has a status (%s), which GR 10 requires of every "
                  "important object and which a document did not have"
                  % doc["status"])
            check(doc["scope"] == SCOPE.GLOBAL,
                  "the row carries its own scope, so a store recovered by hand "
                  "still answers 'whose knowledge is this?'")
            print()

            print("8. R-84 - ingesting leaves an audit entry")
            trail = [f for f in os.listdir(audit) if f.endswith(".jsonl")]
            written = ""
            for name in trail:
                with open(os.path.join(audit, name), encoding="utf-8") as fh:
                    written += fh.read()
            check("knowledge.ingest" in written,
                  "the trail names the operation")
            check(got.document_id in written,
                  "and which document, so an ingest can be traced afterwards")
            print()

            print("9. R-11 - re-ingesting an UNCHANGED file costs nothing")
            again = I.ingest(store, path)
            check(again.reused and again.document_id == got.document_id,
                  "the same bytes are the same document, by CONTENT HASH and "
                  "never by mtime - mtime would re-index after every git "
                  "operation")
            check(again.chunks == got.chunks,
                  "and it reports the chunks already held rather than making "
                  "them a second time")
            print()

            print("10. R-12 - delete and rebuild loses nothing")
            before = [(r["ordinal"], r["locator"], r["text"]) for r in rows]
            I.forget(store, got.document_id)
            emptied = store.execute(
                "SELECT COUNT(*) AS n FROM chunks WHERE document_id = ?",
                (got.document_id,)).fetchone()["n"]
            check(emptied == 0, "the chunks are gone")
            check(os.path.isfile(path),
                  "AND THE ORIGINAL FILE IS UNTOUCHED - the index is derived "
                  "(GR 11), which is the strongest safety promise this system "
                  "makes and the one nothing ever says out loud")
            rebuilt = I.ingest(store, path)
            after = [(r["ordinal"], r["locator"], r["text"])
                     for r in I.boundaries(store, rebuilt.document_id)]
            check(after == before,
                  "and re-ingesting produces the identical chunks - same "
                  "count, same locators, same text")
            print()

            print("11. R-10 - a model file is refused BY NAME, before it is opened")
            model = write(papers, "Tower-MEP.rvt", "not really a model")
            try:
                I.ingest(store, model)
                check(False, "a .rvt was accepted, which breaks D-26")
            except I.RefusedByExtension as refused:
                check("Tower-MEP.rvt" in str(refused),
                      "it names the file rather than failing vaguely")
                check("D-26" in str(refused),
                      "and names the decision, so the refusal can be looked up")
            family = write(papers, "Damper.rfa", "not really a family")
            try:
                I.ingest(store, family)
                check(False, "a .rfa was accepted, which breaks D-26")
            except I.RefusedByExtension:
                check(True, "and a .rfa is refused the same way")

            # NOT refused, deliberately. 02-implementation.md s4.2 proposes it
            # and says it NEEDS THE OWNER'S WORD first, "because a refusal
            # nobody agreed to is as surprising as a leak". He has not given
            # it, so it is not coded - and this asserts the ABSENCE so that
            # adding it later is a deliberate act.
            check(".rte" not in I.REFUSED and ".rft" not in I.REFUSED,
                  "a Revit TEMPLATE is not refused - that refusal was proposed "
                  "and is waiting on the owner's word, not forgotten")
            print()

            print("12. R-82 - an oversized chunk is FLAGGED, never truncated")
            solid = "x" * (I.MAX_CHARS + 500)      # nowhere legal to cut
            wide = write(papers, "one-long-block.txt", solid)
            big = I.ingest(store, wide)
            held = store.execute(
                "SELECT text FROM chunks WHERE document_id = ?",
                (big.document_id,)).fetchall()
            check(any(len(r["text"]) > I.MAX_CHARS for r in held),
                  "the block came through whole at %d characters"
                  % max(len(r["text"]) for r in held))
            check(big.oversized,
                  "and it is REPORTED as oversized rather than trimmed - "
                  "trimming lets a payload be padded past a reader's window, "
                  "which turns the guard into a formality")
            print()

            print("13. A LARGE document does not end on a RecursionError")
            # The first version of _cut recursed once per cut, so a long
            # enough document died on the recursion limit instead of
            # producing a chunk - and a real QCS section is exactly the size
            # that finds that out. Measured, not assumed.
            huge = ("The Contractor shall submit shop drawings for approval "
                    "prior to fabrication of any part of the works. ") * 4000
            cuts = I._cut(huge, limit=400)
            check(len(cuts) > 500,
                  "a %d character block became %d pieces without recursing"
                  % (len(huge), len(cuts)))
            check(sum(len(c) for c in cuts) > len(huge) * 0.95,
                  "and nothing was lost on the way - the pieces still hold "
                  "the text, because a chunker that drops a clause is a "
                  "chunker that answers confidently without it")
            print()

            # THE WINDOW'S EDGE IS WHERE THE SPEED FIX COULD HAVE BROKEN THE
            # RULES. _cut scans only the next `limit` characters plus a
            # margin, so a token or a qualifier sitting exactly at the
            # boundary is the case that would fail silently.
            for offset in range(-30, 31, 6):
                at = 400 + offset
                filler = "The pipe shall be tested thoroughly. "
                lead = (filler * 40)[:at]
                probe = lead + "OST_DuctCurves and BuiltInParameter.RBS_X " + \
                    (filler * 40)
                got = I._cut(probe, limit=400)
                whole = any("OST_DuctCurves" in piece for piece in got)
                check(whole, "a token at the window edge (offset %+d) is still "
                             "whole" % offset)
                if not whole:
                    break

            for offset in range(-30, 31, 15):
                at = 400 + offset
                lead = ("Ducts shall be insulated to 25mm. " * 40)[:at]
                probe = lead + " except where within a conditioned space. " + \
                    ("Further text follows. " * 40)
                got = I._cut(probe, limit=400)
                check(not any(piece.strip().lower().startswith("except")
                              for piece in got),
                      "and a qualifier at the window edge (offset %+d) is "
                      "still never the start of a chunk" % offset)
            print()

            print("14. No chunk is stored empty, and the command itself runs")
            blank = [r for r in I.boundaries(store, rebuilt.document_id)
                     if not (r["text"] or "").strip()]
            check(not blank,
                  "no chunk has empty text. A heading with no prose of its "
                  "own - 'Section 4 Mechanical Works' - still has to exist for "
                  "its children to point at, and it holds its own label rather "
                  "than nothing: an empty chunk is one retrieval can return")

            # THE COMMAND, not just the function. The first version of main()
            # upper-cased the scope name against heron_scope's lowercase
            # constants, so every CLI call died while all of the checks above
            # passed - they call ingest() directly and never the command.
            code = I.main([path])
            check(code == 0, "python brain/heron_ingest.py <file> exits 0")
            check(I.main(["--boundaries", rebuilt.document_id]) == 0,
                  "and --boundaries prints them and exits 0")
            print()

            print("15. A sentence that opens with a number is not a heading")
            check(I.read_heading("4.1 Ductwork") is not None,
                  "'4.1 Ductwork' is a heading")
            check(I.read_heading("2024 requirements shall apply to all works.")
                  is None,
                  "'2024 requirements shall apply to all works.' is NOT - "
                  "reading it as one would invent a clause 2024 and hang "
                  "everything after it underneath")
            check(I.read_heading("21.3.2 Insulation") is not None
                  and I.read_heading("21.3.2 Insulation").depth == 3,
                  "and a three-part number is three levels deep, because the "
                  "document numbered it that way")
            print()

            print("16. Nothing here reads a document back out - that is Stage 2")
            check(not hasattr(I, "search") and not hasattr(I, "retrieve"),
                  "heron_ingest has no retrieval of any kind, and reviewing "
                  "an ingester alongside a retrieval change is reviewing "
                  "neither")
            print()

        finally:
            store.close()
    finally:
        shutil.rmtree(home, ignore_errors=True)
        shutil.rmtree(audit, ignore_errors=True)
        shutil.rmtree(papers, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)
        os.environ.pop("HERON_AUDIT", None)

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a numbered document goes into one scope, keeps its")
    print("hierarchy at whatever depth it actually has, carries its heading")
    print("path, is marked as untrusted content, and can be deleted and")
    print("rebuilt without losing a character. A rule is never parted from")
    print("its exception and no exact token is ever cut.")
    print()
    print("It proves nothing about a REAL standard. Every document here was")
    print("written to be easy, and S-4 is settled by running this on one real")
    print("QCS section and READING the output - not by this file passing.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
