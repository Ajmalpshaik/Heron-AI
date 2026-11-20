#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The knowledge library's rules for the person putting a document in.

    python tests/test_knowledge_rules.py

Issue #1006, WN-18, WN-20 and WN-21 - requirements R-85 to R-87 and R-89 to
R-93 of docs/work-notes/plans/rag/01-requirements.md, which were written and
held by nothing until 2026-11-20:

  1. R-89  where knowledge goes is said in a modeller's words, never a scope
  2. R-91  what Heron does to the file is said BEFORE it does it, and R-92's
           promise that deleting the store leaves every file untouched
  3. R-85  every document records its licence - UNKNOWN when nobody said
  4. R-93  a BIM standard is a kind of document of its own, and says so where
           it is cited
  5. R-86  a store holding a per-seat document warns before it is shared
  6. R-85  the licence and the kind survive the store being deleted and
           restored (Golden Rule 11) - or the warning would quietly go
  7. R-87  nothing shaped like a store is tracked by git

Every store here is a folder this suite makes and removes. No real document is
read and nothing is sent anywhere.
"""

import contextlib
import io
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write(folder, name, body):
    path = os.path.join(folder, name)
    with io.open(path, "w", encoding="utf-8") as handle:
        handle.write(body)
    return path


def one(store, sql, args=()):
    """One row, or None - never a traceback (heron-ship 2a). A store without
    the column being asked about is the failure itself, and the checks after
    it must still run and say how many."""
    import sqlite3
    try:
        return store.execute(sql, args).fetchone()
    except sqlite3.OperationalError:
        return None


def run(main, argv):
    """(exit code, what the command printed)."""
    said = io.StringIO()
    with contextlib.redirect_stdout(said):
        code = main(list(argv))
    return code, said.getvalue()


STANDARD = (u"Acme BIM Standard 2026\n\n3.1 Naming\n\nEvery view name shall "
            u"start with the discipline code.\n\n3.2 Worksets\n\nLinks shall "
            u"sit on their own workset.\n")
NOTE = u"# Site note\n\n1.1 Access\n\nThe plant room is reached from level 2.\n"


def main():
    home = tempfile.mkdtemp(prefix="heron-rules-kb-")
    audit = tempfile.mkdtemp(prefix="heron-rules-audit-")
    papers = tempfile.mkdtemp(prefix="heron-rules-papers-")
    os.environ["HERON_KNOWLEDGE"] = home
    os.environ["HERON_AUDIT"] = audit
    try:
        import heron_scope as SCOPE
        import heron_ingest as I

        print("1. R-89 - where knowledge goes, in a modeller's words")
        plain = getattr(SCOPE, "in_plain_words", None)
        check(callable(plain), "heron_scope.in_plain_words() exists")
        if callable(plain):
            check(plain(SCOPE.USER) == "just for me", "the user scope is 'just for me'")
            check(plain(SCOPE.COMPANY).startswith("for the whole company"),
                  "the company scope is 'for the whole company'")
            check(plain(SCOPE.PROJECT).startswith("for this project only"),
                  "the project scope is 'for this project only'")
            check("on this PC" in plain(SCOPE.COMPANY),
                  "and the company one says it is kept on this PC, so it cannot "
                  "read as sent to the company")
            check(plain(SCOPE.PROJECT, "Tower A").endswith("(Tower A)"),
                  "a project's own name is said with it")
            # NEVER `GLOBAL`, `COMPANY`, `PROJECT` - the scope as a word of
            # code. "company" and "project" as plain English are the owner's
            # own phrases, so it is the code spelling and the word "scope"
            # that may not appear.
            for scope in SCOPE.SCOPES:
                said = plain(scope)
                check(scope.upper() not in said and "scope" not in said.lower(),
                      "%r is said without the code's word for it: %r"
                      % (scope, said))
            refused = False
            try:
                plain("nowhere")
            except ValueError:
                refused = True
            check(refused, "a scope that does not exist is refused, never guessed")
        print()

        print("2. R-91 and R-92 - said BEFORE the file is read, and true")
        lines = I.before_adding(SCOPE.COMPANY) if hasattr(I, "before_adding") else []
        check(bool(lines), "heron_ingest.before_adding() says something")
        text = "\n".join(lines)
        for must in ("not moved", "not changed, and not deleted", "not uploaded",
                     "The file never does", "Nothing outside Heron's own folder",
                     "leaves every original file untouched"):
            check(must in text, "it says %r" % must)
        check("company" not in text.split()[:1] and "COMPANY" not in text,
              "and never names the scope as a word of code")
        # THE THIRD SENTENCE IS THE ONE TO KEEP HONEST - the note's own words.
        # "Nothing leaves your machine" would be easier and false: a quoted
        # clause travels with an answer (D-26).
        check("Nothing leaves" not in text and "travels with the answer" in text,
              "it does not claim nothing leaves - a quoted clause travels with "
              "the answer, and it says so")

        paper = write(papers, "acme-bim.md", STANDARD)
        code, spoke = run(I.main, [paper, "--scope", "company",
                                   "--licence", "per-seat",
                                   "--category", "bim-standard"])
        check(code == 0, "the command ingests the document, exit %r" % code)
        first = spoke.find("not moved")
        done = spoke.find("ingested")
        check(0 <= first < done,
              "and says what it does to the file BEFORE it says it ingested it")
        check("kept for the whole company" in spoke,
              "and where it went, in the same words")
        check("in the company scope" not in spoke,
              "never 'in the company scope'")
        print()

        print("3. R-85 - every document records its licence")
        store = SCOPE.open_scope(SCOPE.COMPANY)
        try:
            I.ensure_tables(store)
            row = one(store, "SELECT licence, category FROM documents "
                             "WHERE title = 'Acme BIM Standard 2026'")
            check(row is not None and row["licence"] == getattr(I, "PER_SEAT", "per-seat"),
                  "the per-seat licence given was recorded: %r"
                  % (row["licence"] if row else None))
            check(tuple(getattr(I, "LICENCES", ())) == ("owned", "per-seat", "public", "unknown"),
                  "the four the owner named: owned, per-seat, public, unknown")
            plain_note = write(papers, "site-note.md", NOTE)
            got = I.ingest(store, plain_note)
            row = one(store, "SELECT licence, category FROM documents "
                             "WHERE id = ?", (got.document_id,))
            check(row is not None and row["licence"] == getattr(I, "UNKNOWN", "unknown"),
                  "a document nobody described is UNKNOWN - never 'public' by "
                  "default: %r" % (row["licence"] if row else None))
            check(row is not None and row["category"] == getattr(I, "UNSTATED", "unstated"),
                  "and its kind is UNSTATED, never guessed from its title")
            try:
                I.ingest(store, plain_note, licence="owned")
                taken = True
            except TypeError:
                taken = False
            row = one(store, "SELECT licence FROM documents WHERE id = ?",
                      (got.document_id,))
            check(taken and row is not None and row["licence"] == "owned",
                  "a licence given on re-ingest is applied, not dropped")
            I.ingest(store, plain_note)
            row = one(store, "SELECT licence FROM documents WHERE id = ?",
                      (got.document_id,))
            check(row is not None and row["licence"] == "owned",
                  "and re-ingesting without one keeps it, rather than resetting "
                  "it to UNKNOWN")
            refused = False
            try:
                I.ingest(store, plain_note, licence="free-ish")
            except (ValueError, TypeError) as why:
                refused = isinstance(why, ValueError)
            check(refused, "a licence outside the four is refused")
            code, spoke = run(I.main, [plain_note, "--scope", "company",
                                       "--licence", "perseat"])
            check(code == 2 and "Nothing was ingested" in spoke,
                  "and the command refuses a mistyped licence before reading "
                  "anything, exit %r" % code)
            print()

            print("4. R-93 - a BIM standard is a kind of document of its own")
            check("bim-standard" in getattr(I, "CATEGORIES", ()),
                  "'bim-standard' is a kind of document")
            import heron_search as SEARCH
            import heron_embed as EMBED
            import heron_retrieve as RETRIEVE
            SEARCH.index_chunks(store)
            EMBED.index_chunks(store)
            answer = RETRIEVE.find_documents(store, "view name discipline code")
            mine = [c for c in answer.candidates
                    if c.get("document") == "Acme BIM Standard 2026"]
            check(bool(mine) and all(c.get("category") == "bim-standard"
                                     for c in mine),
                  "and a clause from it says so where it is retrieved")
            check(bool(mine) and all(c.get("licence") == "per-seat" for c in mine),
                  "and carries its licence too")
            import heron_context as CONTEXT
            packet = CONTEXT.assemble(store, "view name discipline code",
                                      path=CONTEXT.STANDARDS)
            cited = [p.citation for p in packet.parts
                     if p.kind == CONTEXT.STANDARD and p.citation
                     and p.citation.get("document") == "Acme BIM Standard 2026"]
            check(bool(cited) and all(c.get("category") == "bim-standard"
                                      for c in cited),
                  "and the context packet's citation says it is a BIM standard")
            print()

            print("5. R-86 - a store holding a per-seat document warns before sharing")
            warn = getattr(I, "sharing_warning", lambda store: None)
            warned = warn(store)
            check(warned is not None and "PER SEAT" in "\n".join(warned),
                  "the company knowledge warns: it holds a per-seat document")
            check(any("Acme BIM Standard 2026" in line for line in warned or []),
                  "and names it")
            code, spoke = run(I.main, ["--before-sharing", "--scope", "company"])
            check(code == 1 and "BEFORE THIS KNOWLEDGE IS COPIED" in spoke,
                  "--before-sharing says it and exits 1, so a copying script "
                  "can stop on it, exit %r" % code)
            mine = SCOPE.open_scope(SCOPE.USER)
            try:
                check(callable(getattr(I, "sharing_warning", None))
                      and warn(mine) is None,
                      "a store holding no per-seat document has no warning")
            finally:
                mine.close()
            code, spoke = run(I.main, ["--before-sharing", "--scope", "user"])
            check(code == 0 and "not as free" in spoke,
                  "and asked anyway, it says so - and that an unrecorded licence "
                  "is unknown, not free")
            print()

            print("6. Golden Rule 11 - the licence and the kind survive a restore")
            path = store.path
        finally:
            store.close()
        os.remove(path)
        store = SCOPE.open_scope(SCOPE.COMPANY)
        try:
            back = I.restore(store)
            check(len(back.reingested) >= 2,
                  "deleting the store and restoring brings the documents back")
            row = one(store, "SELECT licence, category FROM documents "
                             "WHERE title = 'Acme BIM Standard 2026'")
            check(row is not None and row["licence"] == "per-seat"
                  and row["category"] == "bim-standard",
                  "with its per-seat licence and its BIM-standard kind - a "
                  "restore that lost them would silently drop R-86's warning")
            check(warn(store) is not None,
                  "so the warning is still there after the restore")
        finally:
            store.close()
        print()

        print("7. R-87 - nothing shaped like a knowledge store is tracked")
        try:
            tracked = subprocess.check_output(
                ["git", "-C", ROOT, "ls-files"],
                stderr=subprocess.DEVNULL).decode("utf-8", "replace").splitlines()
        except (OSError, subprocess.CalledProcessError):
            tracked = None
        if tracked is None:
            print("  -     NOT RUN: no git here, so what is tracked cannot be read")
        else:
            shapes = (".db", ".sqlite", ".sqlite3", ".heron-db", "-documents.jsonl")
            stores = [name for name in tracked if name.lower().endswith(shapes)]
            check(not stores, "no tracked file is shaped like a store%s"
                  % ("" if not stores else ": %s" % ", ".join(stores[:5])))
        ignore = io.open(os.path.join(ROOT, ".gitignore"), encoding="utf-8").read()
        for pattern in ("*.db", "*.sqlite", "*.sqlite3"):
            check(pattern in ignore.splitlines(),
                  ".gitignore refuses %s, the second layer under the layout"
                  % pattern)
    finally:
        shutil.rmtree(home, ignore_errors=True)
        shutil.rmtree(audit, ignore_errors=True)
        shutil.rmtree(papers, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)
        os.environ.pop("HERON_AUDIT", None)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a modeller is told where knowledge goes in their own words")
    print("and what Heron does to the file before it does it; every document")
    print("records its licence and whether it is a BIM standard, through a")
    print("restore; and a store holding a per-seat standard says so before it")
    print("is shared.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
