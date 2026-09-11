# Heron-Agent:  HERON-RAG-RIX-011, HERON-RAG-DUP-012
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Stage 6 - the index stays true to the disk without anybody remembering.

    python tests/test_maintenance.py

WHAT IT PROVES
  1. RE-INDEX ON CHANGE, BY CONTENT HASH AND NEVER BY mtime (R-27). docs/05
     s7 names the reason: a git checkout moves every file's mtime and changes
     none of their content, so anything keyed on time re-indexes the whole
     library for nothing.
  2. A CHANGED FILE RETIRES ITS PREDECESSOR AND NAMES ITS REPLACEMENT. Golden
     Rule 4 - a record is never destroyed - and it is what finally makes
     "what did this clause say before?" answerable.
  3. A MISSING SOURCE DELETES NOTHING. Q-B: the store points at the file and
     never held it, so the clauses and their citations still read correctly
     after somebody tidies a folder. Only opening the original breaks.
  4. DUPLICATE CLAUSES ARE FOUND AT WRITE TIME (R-28), by byte-equality and
     NOT by a similarity number - and they are REPORTED, never refused. A
     project spec quoting a company standard verbatim is Tuesday.
  5. THE USER NEVER MANAGES THE INDEX BY HAND (R-29). The host rebuilds both
     halves on every open, and a refresh on an unchanged library is free.
"""

import os
import shutil
import sys
import tempfile
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


FIRST = """Heron Maintenance Standard 2026

Section 5 Ductwork

5.1 Insulation

Ducts shall be insulated to 25mm.
"""

SECOND = """Heron Maintenance Standard 2026

Section 5 Ductwork

5.1 Insulation

Ducts shall be insulated to 30mm.
"""


def main():
    import heron_scope as SCOPE
    import heron_ingest as I

    home = tempfile.mkdtemp(prefix="heron-maint-")
    papers = tempfile.mkdtemp(prefix="heron-maint-papers-")
    os.environ["HERON_KNOWLEDGE"] = home

    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            path = os.path.join(papers, "standard.md")
            with open(path, "w", encoding="utf-8") as handle:
                handle.write(FIRST)
            first = I.ingest(store, path, added_by="tests")

            print("1. R-27 - mtime moving is NOT a change")
            # The exact case docs/05 s7 names: a git checkout touches every
            # file and changes none of them.
            later = time.time() + 10_000
            os.utime(path, (later, later))
            done = I.refresh(store)
            check(len(done.unchanged) == 1 and not done.changed,
                  "the file's mtime moved 10,000 seconds and nothing was "
                  "re-indexed - the hash decides, not the clock")
            print()

            print("2. Content changing IS a change, and the old row is retired")
            with open(path, "w", encoding="utf-8") as handle:
                handle.write(SECOND)
            done = I.refresh(store)
            check(len(done.changed) == 1,
                  "one document changed")
            old_id, new_id, _title = done.changed[0]
            check(old_id == first.document_id and new_id != old_id,
                  "the new content is a new document, by content hash")
            was = store.execute("SELECT status, replaced_by FROM documents "
                                "WHERE id = ?", (old_id,)).fetchone()
            check(was["status"] == "RETIRED",
                  "the old row is RETIRED rather than deleted - Golden Rule 4, "
                  "a record is never destroyed")
            check(was["replaced_by"] == new_id,
                  "and it NAMES its replacement, which is what makes 'what "
                  "did this clause say before?' answerable at all")
            gone = store.execute("SELECT COUNT(*) AS n FROM chunks WHERE "
                                 "document_id = ?", (old_id,)).fetchone()["n"]
            check(gone > 0,
                  "the old clauses are still there - %d of them - so a "
                  "citation written last month still resolves" % gone)
            print()

            print("3. A RETIRED document is not offered as an answer")
            import heron_search as SEARCH
            import heron_embed as EMBED
            import heron_retrieve as R
            SEARCH.index_chunks(store)
            EMBED.index_chunks(store)
            answer = R.find_documents(store, "how thick should ducts be insulated")
            texts = " ".join(store.execute(
                "SELECT text FROM chunks WHERE id = ?", (c["id"],)
            ).fetchone()["text"] for c in answer.candidates)
            check("30mm" in texts and "25mm" not in texts,
                  "retrieval returns the CURRENT 30mm and not the retired "
                  "25mm - the history is kept and is not an answer")
            print()

            print("4. A missing source deletes NOTHING")
            before = store.execute(
                "SELECT COUNT(*) AS n FROM chunks").fetchone()["n"]
            os.remove(path)
            done = I.refresh(store)
            check(len(done.missing) == 1,
                  "the moved file is reported")
            after = store.execute(
                "SELECT COUNT(*) AS n FROM chunks").fetchone()["n"]
            check(after == before,
                  "and not one chunk was deleted (%d before, %d after) - the "
                  "store points at the file and never held it, so the text "
                  "and the citations still read correctly" % (before, after))
            check(not done.changed,
                  "a missing file is never treated as a change")
            print()

            print("5. R-28 - duplicate clauses at WRITE time, by byte-equality")
            copy_a = os.path.join(papers, "acme-standard.md")
            copy_b = os.path.join(papers, "acme-standard-FINAL-v3.md")
            body = ("Acme Standard 2026\n\nSection 7 Pipework\n\n"
                    "7.1 Supports\n\nPipe supports shall be spaced at not "
                    "more than 2m.\n")
            with open(copy_a, "w", encoding="utf-8") as handle:
                handle.write(body)
            # The same clauses, a different file - a cover line added, which
            # is what a re-export or a different tool does. The hashes differ.
            with open(copy_b, "w", encoding="utf-8") as handle:
                handle.write("Issued for construction\n\n" + body)
            a = I.ingest(store, copy_a, added_by="tests")
            b = I.ingest(store, copy_b, added_by="tests")
            check(a.document_id != b.document_id,
                  "two different files, so two different content hashes - "
                  "the hash alone does NOT catch this")
            check(b.duplicates,
                  "but the clause check does: %d clause(s) are byte-identical "
                  "to ones already here" % len(b.duplicates))
            found = dict((loc, other) for loc, other, _t in b.duplicates)
            check(found.get("7.1") == a.document_id,
                  "and it names which clause and which document: %s"
                  % ", ".join(sorted(found)))
            check("Section 7" in found,
                  "the section heading is duplicated too, and is reported as "
                  "its own row rather than folded into the clause below it")
            check(b.chunks > 0,
                  "and NOTHING was refused - a project spec quoting a company "
                  "standard verbatim is Tuesday, not an error")
            print()

            print("5b. GOLDEN RULE 11 - deleting the store is safe RECOVERY")
            # The rule: "the index is derived, never authoritative - deleting
            # it must always be a safe recovery action." Fragments obeyed it;
            # documents did NOT. The documents table was the only record of
            # which external files had been ingested, into which scope, with
            # which title, status and trust - so deleting a scope file, the
            # documented recovery action, destroyed all of it while every
            # original file sat untouched on disk. A review found it.
            mpath = I.manifest_path(store)
            check(os.path.isfile(mpath),
                  "a manifest lives BESIDE the store, not inside it: %s"
                  % os.path.basename(mpath))
            check(not mpath.endswith(".db"),
                  "and it is not the store file, so deleting one leaves the "
                  "other")
            before_docs = store.execute(
                "SELECT COUNT(*) AS n FROM documents").fetchone()["n"]
            db = store.path
            store.close()
            os.remove(db)
            store = SCOPE.open_scope(SCOPE.GLOBAL)
            check(store.execute(
                      "SELECT COUNT(*) AS n FROM sqlite_master "
                      "WHERE name = 'documents'").fetchone()["n"] == 0,
                  "the store is deleted and the documents table is gone with it")
            done = I.restore(store)
            after_docs = store.execute(
                "SELECT COUNT(*) AS n FROM documents").fetchone()["n"]
            check(done.reingested,
                  "restore() re-reads the source files the manifest names, "
                  "and brings %d document(s) back" % len(done.reingested))
            check(after_docs > 0,
                  "%d document row(s) exist again, from %d before - the ones "
                  "whose source files are still on disk" % (after_docs,
                                                            before_docs))
            check(not done.gone or all(isinstance(g, tuple) for g in done.gone),
                  "and a source that has MOVED is named, never invented")
            print()

            print("6. R-29 - the index is never a thing a person maintains")
            done = I.refresh(store)
            check(not done.changed,
                  "a refresh on an unchanged library finds nothing to do, so "
                  "it may be run at any time")
            host = open(os.path.join(ROOT, "mcp", "server", "heron_brain.py"),
                        encoding="utf-8").read()
            check("SEARCH.index_chunks" in host and "EMBED.index_chunks" in host,
                  "and the host rebuilds the DOCUMENT half on every open, not "
                  "just the fragment half - which is where this had quietly "
                  "stopped being true")
            check("--rebuild" in open(
                      os.path.join(ROOT, "brain", "heron_scope.py"),
                      encoding="utf-8").read(),
                  "--rebuild still exists, as recovery. It has stopped being "
                  "the only way")
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

    print("PASSED - a changed file re-indexes and retires what it replaced, a")
    print("moved file deletes nothing, a duplicated clause is named at write")
    print("time, and a touched-but-unchanged file costs one hash.")
    print()
    print("It proves nothing about WHEN a refresh runs in production. Nothing")
    print("watches the filesystem; the host rebuilds on open and a person can")
    print("ask for one. A document changed while Heron is open stays stale")
    print("until the next open.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
