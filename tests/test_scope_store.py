# Heron-Agent:  HERON-RAG-LIB-001
# Heron-Step:   8
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Step 8 - one knowledge store per scope, as one file each.

    python tests/test_scope_store.py

Runs anywhere. No Revit, no Windows - SQLite is in the standard library.

WHAT IT PROVES
  1. A cross-scope query is impossible to WRITE, not merely absent.
  2. Deleting one project's store leaves every other scope untouched.
  3. The stores are DERIVED - delete them all and a rebuild puts them back.
  4. The scope is resolved from facts, and an unidentified project is REFUSED
     rather than guessed at.
  5. A card changed on disk reaches its row without a rebuild - only that
     card's row, never a row somebody else wrote, and never from a linked
     worktree into the one shared store (FRAGMENT-ISSUES 5b-249, row 131).

WHAT IT DOES NOT PROVE. That any of this is wired to a real Revit document. The
resolver is handed the same facts the bridge would report; nothing here has
asked a live model what is open.
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


# Declared cases are required for a fragment to validate, since 2026-09-07. The
# fixture carries them rather than being exempted: a fixture that skips a rule is
# a fixture that has stopped testing it.
CASES = (
    u"positive:\n"
    u"  - given: two ducts in the set\n"
    u"    expect: both reported, named\n"
    u"negative:\n"
    u"  - given: a set containing none of what it reports\n"
    u"    expect: zero reported, in words, and no error\n"
)


def write_valid_fragment(folder):
    """A fragment that passes validation, written at `folder`.

    Built, not copied - see the note at the call site. Kept minimal on purpose:
    every key here is one heron_fragment.validate() actually requires, so if that
    gate gains a rule this fixture fails and says so, rather than drifting.
    """
    import yaml

    data = {
        "heron-agent": "HERON-RAG-LIB-001",
        "heron-step": 8,
        "heron-status": "DRAFT",
        "heron-since": "0.1.0",
        "heron-layer": "brain",
        "id": "FRG-ELE-001",
        "semantic-identity": "a test fragment",
        "kind": "filter",
        "domain": "test",
        "capability": "DO_A_TEST_THING",
        "version": 1,
        "source": "OFFICIAL",
        "risk": "READ",
        "purpose": "Exists to be indexed.",
        "contract": {
            "needs": [{"name": "doc", "type": "Document"}],
            "provides": [{"name": "elements", "type": "IList<Element>",
                          "role": "result"}],
        },
        "revit": ["2020", "2024"],
        "runtime": ["net472", "net48"],
        "utterances": ["do the test thing"],
    }

    os.makedirs(os.path.join(folder, "impl", "any"))
    os.makedirs(os.path.join(folder, "tests"))
    io.open(os.path.join(folder, "fragment.yaml"), "w", encoding="utf-8").write(
        yaml.safe_dump(data, default_flow_style=False, sort_keys=False))
    io.open(os.path.join(folder, "impl", "any", "fragment.cs"), "w",
            encoding="utf-8").write(u"// code\n")
    io.open(os.path.join(folder, "tests", "cases.yaml"), "w",
            encoding="utf-8").write(CASES)
    return folder


def _rewrite_card(path, old, new):
    """Replace `old` with `new` in a card, and MOVE ITS MARK for certain.

    A rewrite inside the same clock tick can leave mtime where it was, and the
    same length leaves the size - the one-second .pyc trap heron-ship records,
    in another place. The mark is pushed on by a second so this suite tests
    the refresh, not the filesystem's clock.
    """
    text = io.open(path, encoding="utf-8").read()
    io.open(path, "w", encoding="utf-8").write(text.replace(old, new))
    st = os.stat(path)
    os.utime(path, ns=(st.st_atime_ns, st.st_mtime_ns + 10 ** 9))


def _row(S, fid, field):
    store = S.open_scope(S.GLOBAL)
    try:
        for r in store.fragments():
            if r["id"] == fid:
                return r[field]
        return None
    finally:
        store.close()


def refreshing(S, F):
    """FRAGMENT-ISSUES row 5b-249: a card changed on disk reaches its row.

    The search reads a card's purpose from the FILE and its identity, status,
    capability, domain and risk from the store's ROW, and only rebuild() wrote
    rows - so a merged identity change was searched under the old identity
    until somebody rebuilt by hand. Every name below is asked for with
    getattr first, so the code before the fix FAILS here rather than raising
    (heron-ship 2a).
    """
    print()
    print("5. A card changed on disk reaches its row, without a rebuild")
    refresh = getattr(S, "refresh", None)
    refreshes_from = getattr(S, "refreshes_from", None)
    check(callable(refresh), "heron_scope has a refresh() at all")
    check(callable(refreshes_from),
          "and a refreshes_from() that says whose cards it reads")
    if not callable(refresh):
        refresh = lambda store, root=None: []          # noqa: E731
    if not callable(refreshes_from):
        refreshes_from = lambda root=None: "missing"   # noqa: E731
    seen_key = getattr(S, "CARDS_SEEN", "cards_seen:")

    work = tempfile.mkdtemp(prefix="heron-refresh-")
    was_fragments_dir = F.FRAGMENTS_DIR
    try:
        card = os.path.join(write_valid_fragment(
            os.path.join(work, "do-a-test-thing")), "fragment.yaml")
        F.FRAGMENTS_DIR = work
        S.rebuild()
        check(_row(S, "FRG-ELE-001", "semantic_identity") == "a test fragment",
              "the rebuilt row carries the card's identity")

        # THE DEFECT, as a merge delivers it: the card changes, the store does
        # not get rebuilt. DEPRECATED rather than PROVEN because a PROVEN card
        # needs a proof to validate, and one that does not validate keeps its
        # old row on purpose - which is not what this check is about.
        _rewrite_card(card, "a test fragment", "a renamed test fragment")
        _rewrite_card(card, "heron-status: DRAFT", "heron-status: DEPRECATED")
        check(F.validate(F.load(os.path.dirname(card))) == [],
              "the edited card still validates, so a red below is the refresh")
        store = S.open_scope(S.GLOBAL)
        try:
            got = refresh(store)
        finally:
            store.close()
        check(got == ["FRG-ELE-001"],
              "refresh() names the one row it rewrote (%r)" % (got,))
        check(_row(S, "FRG-ELE-001", "semantic_identity")
              == "a renamed test fragment",
              "the row now carries the NEW identity (%r)"
              % _row(S, "FRG-ELE-001", "semantic_identity"))
        check(_row(S, "FRG-ELE-001", "status") == "DEPRECATED",
              "and the new status")

        # NOTHING MOVED ON DISK, SO NOTHING IS READ OR WRITTEN - and a row
        # somebody else wrote is theirs. A suite plants a DRAFT row in a
        # throwaway store; a session proving an unmerged card puts one row in
        # the shared store. Rewriting either would undo a deliberate write.
        store = S.open_scope(S.GLOBAL)
        try:
            store.execute("UPDATE fragments SET status = 'PLANTED' "
                          "WHERE id = 'FRG-ELE-001'")
            store.db.commit()
            got = refresh(store)
        finally:
            store.close()
        check(got == [], "a card that has not changed is not read (%r)"
              % (got,))
        check(_row(S, "FRG-ELE-001", "status") == "PLANTED",
              "and a row somebody else wrote is left as they wrote it")

        # A CHECKOUT SEEING THE LIBRARY FOR THE FIRST TIME - a new chat in a
        # new worktree - has no marks of its own. It reads each card's bytes
        # and finds the row was made from exactly those, so it neither parses
        # the library nor undoes the plant above.
        store = S.open_scope(S.GLOBAL)
        try:
            store.execute("DELETE FROM meta WHERE key LIKE ?",
                          (seen_key + "%",))
            store.db.commit()
            got = refresh(store)
        finally:
            store.close()
        check(got == [] and _row(S, "FRG-ELE-001", "status") == "PLANTED",
              "a checkout with no marks of its own trusts a row made from the "
              "same bytes (%r)" % (got,))

        # A NEW CARD IS NOT ADDED. That is rebuild()'s job: adding would fill
        # a store built with a subset on purpose.
        other = write_valid_fragment(os.path.join(work, "do-another-thing"))
        _rewrite_card(os.path.join(other, "fragment.yaml"),
                      "FRG-ELE-001", "FRG-ELE-002")
        _rewrite_card(os.path.join(other, "fragment.yaml"),
                      "DO_A_TEST_THING", "DO_ANOTHER_THING")
        check(F.validate(F.load(other)) == [],
              "the new card validates, so it is left out for being NEW")
        store = S.open_scope(S.GLOBAL)
        try:
            refresh(store)
            ids = sorted(r["id"] for r in store.fragments())
        finally:
            store.close()
        check(ids == ["FRG-ELE-001"],
              "a card the store does not hold is not added (%s)"
              % ", ".join(ids))

        # A CARD CAUGHT HALF-SAVED KEEPS THE ROW IT HAD (D-48).
        io.open(card, "w", encoding="utf-8").write(u"id: [unclosed\n")
        st = os.stat(card)
        os.utime(card, ns=(st.st_atime_ns, st.st_mtime_ns + 10 ** 9))
        store = S.open_scope(S.GLOBAL)
        try:
            got = refresh(store)
        finally:
            store.close()
        check(got == [] and _row(S, "FRG-ELE-001", "semantic_identity")
              == "a renamed test fragment",
              "a card that will not load keeps its row, not loses it")

        # A CARD THAT COULD NOT BE READ IS TRIED AGAIN - a Windows lock, an
        # updater holding the file. Its mark must not be recorded as looked at,
        # or the lookup after the lock clears sees nothing to do (review on
        # PR #353). The read is made to fail once, from inside.
        shutil.rmtree(os.path.dirname(card))
        write_valid_fragment(os.path.dirname(card))
        _rewrite_card(card, "a test fragment", "a locked-out rename")
        real_card = getattr(S, "_card", None)
        if callable(real_card):
            S._card = lambda folder, name: None
        store = S.open_scope(S.GLOBAL)
        try:
            first = refresh(store)
        finally:
            store.close()
            if callable(real_card):
                S._card = real_card
        store = S.open_scope(S.GLOBAL)
        try:
            second = refresh(store)
        finally:
            store.close()
        check(first == [] and second == ["FRG-ELE-001"]
              and _row(S, "FRG-ELE-001", "semantic_identity")
              == "a locked-out rename",
              "a card that could not be read is read again next time "
              "(%r then %r)" % (first, second))

        # A NEW WAY OF MAKING A ROW makes every record of the old way void:
        # ROW_FORMAT, as heron_search's INDEX_FORMAT (review on PR #353).
        store = S.open_scope(S.GLOBAL)
        was_format = getattr(S, "ROW_FORMAT", None)
        try:
            store.execute("UPDATE fragments SET domain = 'OLD-DERIVATION' "
                          "WHERE id = 'FRG-ELE-001'")
            store.db.commit()
            untouched = refresh(store)
            if was_format is not None:
                S.ROW_FORMAT = was_format + 1
            got = refresh(store)
        finally:
            if was_format is not None:
                S.ROW_FORMAT = was_format
            store.close()
        check(untouched == [] and got == ["FRG-ELE-001"]
              and _row(S, "FRG-ELE-001", "domain") == "test",
              "a row made by an older ROW_FORMAT is compared again (%r then "
              "%r)" % (untouched, got))

        # A STORE THAT CANNOT SAY WHAT ITS ROWS WERE MADE FROM - one written
        # before this existed - compares every card once: an absent record is
        # not a clean one (D-52).
        shutil.rmtree(os.path.dirname(card))
        write_valid_fragment(os.path.dirname(card))
        store = S.open_scope(S.GLOBAL)
        try:
            store.execute("DELETE FROM meta WHERE key LIKE ? OR key = ?",
                          (seen_key + "%", getattr(S, "ROWS_FROM", "rows_from")))
            store.execute("UPDATE fragments SET capability = 'STALE' "
                          "WHERE id = 'FRG-ELE-001'")
            store.db.commit()
            got = refresh(store)
        finally:
            store.close()
        check(got == ["FRG-ELE-001"]
              and _row(S, "FRG-ELE-001", "capability") == "DO_A_TEST_THING",
              "a store with no record of what it saw is compared in full")
    finally:
        F.FRAGMENTS_DIR = was_fragments_dir
        shutil.rmtree(work, ignore_errors=True)

    print()
    print("  ..and the SHARED store follows the main checkout's cards, never "
          "a worktree's (row 131)")
    saved = {k: os.environ.get(k) for k in ("APPDATA", "HERON_KNOWLEDGE")}
    fake = tempfile.mkdtemp(prefix="heron-trees-")
    try:
        # A MAIN CHECKOUT AND ONE WORKTREE, laid out as git lays them out: the
        # main checkout's .git is a folder, the worktree's is a one-line file
        # naming its folder under it, and that folder's `commondir` leads back.
        # Both hold a full card, as a real checkout does.
        main_tree = os.path.join(fake, "main")
        worktree = os.path.join(fake, "wt")
        installed = os.path.join(fake, "installed")
        pointer = os.path.join(main_tree, ".git", "worktrees", "wt")
        os.makedirs(pointer)
        io.open(os.path.join(pointer, "commondir"), "w",
                encoding="utf-8").write(u"../..\n")
        os.makedirs(os.path.join(installed, "brain", "fragments"))
        main_cards = os.path.join(main_tree, "brain", "fragments")
        wt_cards = os.path.join(worktree, "brain", "fragments")
        wt_card = os.path.join(write_valid_fragment(
            os.path.join(wt_cards, "do-a-test-thing")), "fragment.yaml")
        main_card = os.path.join(write_valid_fragment(
            os.path.join(main_cards, "do-a-test-thing")), "fragment.yaml")
        shutil.copyfile(wt_card, main_card)
        io.open(os.path.join(worktree, ".git"), "w", encoding="utf-8").write(
            u"gitdir: %s\n" % pointer.replace(os.sep, "/"))
        lost = os.path.join(fake, "lost")
        os.makedirs(lost)
        io.open(os.path.join(lost, ".git"), "w", encoding="utf-8").write(
            u"gitdir: nowhere/.git/worktrees/lost\n")

        def same(a, b):
            return bool(a) and bool(b) and \
                os.path.normcase(os.path.abspath(a)) == \
                os.path.normcase(os.path.abspath(b))

        os.environ["HERON_KNOWLEDGE"] = os.path.join(fake, "private")
        got = refreshes_from(worktree)
        check(got is not None and same(got[0], F.FRAGMENTS_DIR)
              and got[1] is None,
              "a store HERON_KNOWLEDGE points at is private: this checkout's "
              "own cards (%r)" % (got,))

        # THE SHARED STORE: knowledge_dir() is the Heron/knowledge folder
        # under %APPDATA%.
        os.environ.pop("HERON_KNOWLEDGE", None)
        os.environ["APPDATA"] = os.path.join(fake, "appdata")
        got = refreshes_from(worktree)
        check(got is not None and same(got[0], main_cards)
              and same(got[1], main_tree),
              "the SHARED store, asked from a worktree, is refreshed from the "
              "MAIN checkout's cards (%r)" % (got,))
        got = refreshes_from(main_tree)
        check(got is not None and same(got[0], main_cards),
              "and asked from the main checkout, from its own")
        got = refreshes_from(installed)
        check(got is not None and same(got[1], installed),
              "an installed Heron, which has no .git at all, uses its own")
        check(refreshes_from(lost) is None,
              "a worktree whose main checkout cannot be found refreshes "
              "nothing (%r)" % (refreshes_from(lost),))
        os.environ["HERON_KNOWLEDGE"] = os.path.join(
            fake, "appdata", "Heron", "knowledge")
        check(refreshes_from(lost) is None,
              "HERON_KNOWLEDGE spelling out the shared folder is still shared")

        # THE SAME FOLDER BY ANOTHER NAME - a junction or a symbolic link -
        # is still the shared one (review on PR #353). Skipped, and said so,
        # where this machine will not make a link.
        shared = os.path.join(fake, "appdata", "Heron", "knowledge")
        os.makedirs(shared)
        alias = os.path.join(fake, "alias")
        linked = False
        try:
            os.symlink(shared, alias, target_is_directory=True)
            linked = True
        except (OSError, NotImplementedError, AttributeError):
            try:
                import _winapi
                _winapi.CreateJunction(shared, alias)
                linked = True
            except (ImportError, OSError, AttributeError):
                linked = False
        if linked:
            os.environ["HERON_KNOWLEDGE"] = alias
            check(refreshes_from(lost) is None,
                  "HERON_KNOWLEDGE naming the shared folder through a link "
                  "is still shared")
            try:
                os.unlink(alias)
            except OSError:
                os.rmdir(alias)
        else:
            print("  (not run: this machine would not make a folder link)")
        os.environ.pop("HERON_KNOWLEDGE", None)

        # AND IN ACTION: a chat in the worktree asks, on the shared store.
        F.FRAGMENTS_DIR = wt_cards
        S.rebuild()
        _rewrite_card(wt_card, "a test fragment", "an unmerged edit")
        store = S.open_scope(S.GLOBAL)
        try:
            got = refresh(store, worktree)
        finally:
            store.close()
        check(got == [] and _row(S, "FRG-ELE-001", "semantic_identity")
              == "a test fragment",
              "a worktree's UNMERGED edit is not pushed into the shared "
              "store (%r)" % (got,))

        # ANOTHER PR MERGES: the main checkout moves to a card the worktree
        # has never held. The worktree's chat - opened before that merge, its
        # own card different again - still brings the row level, because it
        # reads the main checkout's card rather than comparing its own (the
        # review's P1 on PR #353).
        _rewrite_card(main_card, "a test fragment", "merged from elsewhere")
        check(F.validate(F.load(os.path.dirname(main_card))) == [],
              "the main checkout's new card validates")
        store = S.open_scope(S.GLOBAL)
        try:
            got = refresh(store, worktree)
        finally:
            store.close()
        check(got == ["FRG-ELE-001"]
              and _row(S, "FRG-ELE-001", "semantic_identity")
              == "merged from elsewhere",
              "a chat opened BEFORE the merge brings the row to the main "
              "checkout's card (%r)" % (got,))
        folder = _row(S, "FRG-ELE-001", "folder")
        check(folder is not None
              and folder.replace(chr(92), "/") == "brain/fragments/do-a-test-thing",
              "and writes its folder as the main checkout would (%r)" % folder)

        _rewrite_card(main_card, "merged from elsewhere", "never seen")
        store = S.open_scope(S.GLOBAL)
        try:
            got = refresh(store, lost)
        finally:
            store.close()
        check(got == [] and _row(S, "FRG-ELE-001", "semantic_identity")
              == "merged from elsewhere",
              "and a checkout that cannot say where its main checkout is "
              "leaves the shared store alone (%r)" % (got,))
    finally:
        F.FRAGMENTS_DIR = was_fragments_dir
        for key, value in saved.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value
        shutil.rmtree(fake, ignore_errors=True)

    # THE WIRING. The rows are only as fresh as the path that serves a lookup,
    # and that path is heron_brain._Open - read as TEXT, as test_review_findings
    # reads it, because importing the server here would pull the search models
    # into a store suite. The two command-line lookups open the same store and
    # owe the same order (review on PR #353).
    server = io.open(os.path.join(ROOT, "mcp", "server", "heron_brain.py"),
                     encoding="utf-8").read()
    start = server.find("class _Open")
    enter = server[start:server.find("def _shut", start)] if start >= 0 else ""
    at = enter.find("SCOPE.refresh(")
    check(at >= 0, "_Open refreshes the rows")
    check(at >= 0 and all(0 <= at < enter.find(later) for later in
                          ("CAP.rebuild(", "SEARCH.index(", "EMBED.index(")),
          "and does it BEFORE the capability map and both indexes read them")
    for name in ("heron_retrieve.py", "heron_context.py"):
        text = io.open(os.path.join(ROOT, "brain", name),
                       encoding="utf-8").read()
        body = text[text.find("def main("):]
        at = body.find("SCOPE.refresh(")
        check(0 <= at < body.find("SEARCH.index("),
              "brain/%s refreshes the rows before it indexes" % name)

def main():
    home = tempfile.mkdtemp(prefix="heron-kn-")
    os.environ["HERON_KNOWLEDGE"] = home

    import heron_scope as S
    import heron_fragment as F

    try:
        print("1. A cross-scope query cannot be written")
        store = S.open_scope(S.GLOBAL)
        try:
            refused = False
            try:
                store.execute("ATTACH DATABASE 'other.db' AS other")
            except S.CrossScopeRefused as exc:
                refused = True
                reason = str(exc)
            check(refused, "ATTACH is refused, by name")
            check("Golden Rule 5" in reason,
                  "and the refusal says which rule it is protecting")

            refused_lower = False
            try:
                store.execute("attach database 'x.db' as x")
            except S.CrossScopeRefused:
                refused_lower = True
            check(refused_lower, "lower case does not slip past it")

            detached = False
            try:
                store.execute("DETACH DATABASE other")
            except S.CrossScopeRefused:
                detached = True
            check(detached, "so does DETACH - the other half of the same door")

            ok_query = store.execute(
                "SELECT COUNT(*) AS n FROM fragments").fetchone()["n"]
            check(ok_query == 0, "an ordinary query still works")
        finally:
            store.close()

        check(not hasattr(S, "open_scopes"),
              "there is no open_scopes() - the API cannot express two at once")

        print()
        print("2. The scope is resolved from facts, never guessed")
        scope, key = S.resolve(wanted=S.GLOBAL)
        check((scope, key) == (S.GLOBAL, None), "a non-project scope needs no key")

        refused = False
        try:
            S.resolve(document={}, wanted=S.PROJECT)
        except ValueError as exc:
            refused = True
            why = str(exc)
        check(refused, "an unidentified project is REFUSED, not defaulted")
        check("does not guess" in why,
              "and the message says why: a wrong guess is a contractual breach")

        scope, key = S.resolve(
            document={"project_key": "abcd-1234", "project_name": "Tower A"},
            wanted=S.PROJECT)
        check((scope, key) == (S.PROJECT, "abcd-1234"),
              "given a key, it resolves to that project")
        check(S.read_labels().get("abcd-1234") == "Tower A",
              "the human label is remembered separately from the key")

        print()
        print("  ..the document NAME is never the key")
        _s, k1 = S.resolve(document={"project_key": "same-key",
                                     "project_name": "Project1"}, wanted=S.PROJECT)
        _s, k2 = S.resolve(document={"project_key": "same-key",
                                     "project_name": "Renamed Later"},
                           wanted=S.PROJECT)
        check(k1 == k2, "renaming the model does not move its knowledge")

        print()
        print("3. One project's store is its own file")
        a = S.open_scope(S.PROJECT, "client-a")
        b = S.open_scope(S.PROJECT, "client-b")
        g = S.open_scope(S.GLOBAL)
        try:
            check(a.path != b.path, "two projects, two files")
            check(os.path.dirname(a.path) == os.path.dirname(b.path),
                  "both under projects/, so a human can find them")
            a.execute("INSERT INTO meta (key, value) VALUES ('secret', 'A only')")
            a.db.commit()
            found = b.execute(
                "SELECT value FROM meta WHERE key = 'secret'").fetchone()
            check(found is None,
                  "what is written to client A is not visible from client B")
        finally:
            a.close(); b.close(); g.close()

        print()
        print("  ..deleting one project takes nothing else with it")
        os.remove(S.scope_path(S.PROJECT, "client-a"))
        check(not os.path.exists(S.scope_path(S.PROJECT, "client-a")),
              "client A's store is gone")
        check(os.path.exists(S.scope_path(S.PROJECT, "client-b")),
              "client B's is untouched")
        check(os.path.exists(S.scope_path(S.GLOBAL)),
              "and so is the global scope")

        print()
        print("4. The stores are DERIVED - deleting them all loses nothing")
        count, problems = S.rebuild()
        check(count >= 2, "a rebuild indexes the fragments on disk (%d)" % count)

        shutil.rmtree(home)
        check(not os.path.exists(home), "every knowledge file deleted")

        again, _ = S.rebuild()
        check(again == count,
              "and a rebuild puts back exactly what was there (%d)" % again)

        store = S.open_scope(S.GLOBAL)
        try:
            rows = store.fragments()
            ids = sorted(r["id"] for r in rows)
            check("FRG-ELE-001" in ids,
                  "the indexed rows are the real fragments: %s" % ", ".join(ids))
            check(all(r["status"] for r in rows),
                  "each row carries its lifecycle status for later filtering")
        finally:
            store.close()

        print()
        print("An invalid fragment is not indexed")
        # THE LIBRARY IS NOT SCRATCH, AND THIS USED TO TREAT IT AS SCRATCH. The
        # broken fragment was written straight into brain/fragments/ as
        # zz-broken-temp/ and deleted afterwards - so a test wrote into library
        # source, and an interrupted run left it sitting there. The earlier fix
        # moved the makedirs inside the try with exist_ok so the NEXT run could
        # clear it, which made the mess survivable rather than stopping it.
        #
        # A temp library holds exactly the same proof and cannot outlive the run.
        # rebuild() calls heron_fragment.load_all() with no root, and load_all
        # resolves `root or FRAGMENTS_DIR` at CALL time, so pointing that one
        # module global at the copy redirects it - restored in the finally.
        #
        # The good fragment is BUILT rather than copied from the library. A copy
        # was tried first and does not work: every PROVEN fragment's proof is
        # fingerprinted against its own bytes and path, so copying one out of
        # brain/fragments/ makes it fail validation and the temp library indexes
        # NOTHING. Copying a DRAFT one instead would work today and rot quietly
        # the day that fragment is promoted - a built one cannot be reached by
        # the library changing under it.
        work = tempfile.mkdtemp(prefix="heron-broken-")
        was_fragments_dir = F.FRAGMENTS_DIR
        try:
            write_valid_fragment(os.path.join(work, "do-a-test-thing"))
            F.FRAGMENTS_DIR = work
            before, _ = S.rebuild()

            broken_dir = os.path.join(work, "zz-broken")
            os.makedirs(os.path.join(broken_dir, "impl", "any"))
            io.open(os.path.join(broken_dir, "fragment.yaml"), "w",
                    encoding="utf-8").write(u"id: FRG-ELE-999\nkind: nonsense\n")
            after, _ = S.rebuild()

            check(after == before,
                  "a malformed fragment is skipped, not indexed as if it were fine")
            # WITHOUT THIS THE CHECK ABOVE PASSES ON 0 == 0. It compared two
            # counts and never asked whether either was a real one, so a library
            # that indexed nothing at all satisfied it. That is not theoretical:
            # the first version of this rewrite copied a PROVEN fragment, indexed
            # 0, and the comparison above still said ok.
            check(before == 1,
                  "and it is compared against a real count rather than 0 (%d)"
                  % before)
        finally:
            F.FRAGMENTS_DIR = was_fragments_dir
            shutil.rmtree(work, ignore_errors=True)

        refreshing(S, F)

        print()
        print("Unknown scopes are an error, never a guess")
        bad = False
        try:
            S.scope_path("clients")
        except ValueError:
            bad = True
        check(bad, "'clients' is not a scope and is refused")

    finally:
        shutil.rmtree(home, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - one file per scope, a cross-scope query that cannot be")
    print("written, and stores that are derived rather than precious.")
    print("Nothing here has spoken to a real Revit document: the resolver was")
    print("handed the facts the bridge would report, not asked for them.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
