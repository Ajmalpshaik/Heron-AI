# Heron-Agent:  HERON-RAG-LIB-001
# Heron-Step:   8
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The SHARED knowledge index is built from the MAIN checkout's cards, asked from
anywhere - FRAGMENT-ISSUES row 136 (the half left open) and row 5b-404.

    python tests/test_shared_index_from_main.py

Runs anywhere. No Revit, no git, no encoder: the checkouts are folders laid
out the way git lays them out, and the vectors come from a stand-in encoder
that records what it was asked to embed.

THE OWNER'S DECISION, 2026-10-21: "Always build from main". Every Code-tab
chat runs Heron's server from its own folder, and the knowledge store is ONE
file every chat on the PC reads. Until this, a chat in a worktree whose card
differed from main's re-indexed that store from ITS card on every lookup (row
136), and the first open of an EMPTY store - and `heron_scope.py --rebuild` -
built every row from the asking checkout (row 5b-404). So one session's
unmerged words became every other session's answers.

WHAT IT PROVES - every check from inside a planted WORKTREE unless it says so,
on a scratch shared store under a planted HERON_DATA:
  1. An empty shared store is rebuilt from main's cards - main's words, a
     PROVEN card only main holds (validated as main reads it), and no card
     only the worktree holds.
  2. The search index and the vectors are built from main's words, and the
     store records main's card folder as where they came from.
  3. Once main's chat has indexed the store, a worktree chat whose card
     differs leaves it as it is - row 136's open half.
  4. The server's own path (heron_brain._Open) and the --rebuild command
     line do the same.
  5. Nothing is written when main's cards are not on disk anywhere: a
     worktree whose main checkout cannot be found, or a main checkout on a
     branch that is not main - asked from it or from a worktree. The empty
     store is refused in plain words, the --rebuild exits non-zero, and a
     changed card's row is not rewritten.
  6. A PRIVATE store (HERON_KNOWLEDGE of its own) keeps this checkout's cards.

Every name the fix adds is asked for with getattr, and a keyword the old code
does not take is caught, so the code before the fix FAILS here rather than
raising (heron-ship 2a).

WHAT IT DOES NOT PROVE. That the owner's own shared store is now level - that
is a fact about his PC, read after the merge and a restart of the Claude app.
Nothing here opens it: HERON_DATA points at a scratch folder before
heron_scope is imported.
"""

import contextlib
import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines, and its home, go to scratch folders - never the
# owner's real trail or his shared store (rows 5b-1070, 5b-1288).
scratch_audit.plant(data=True)

WORK = tempfile.mkdtemp(prefix="heron-from-main-")
HOME = os.path.join(WORK, "home")
os.environ["HERON_DATA"] = HOME
os.environ.pop("HERON_KNOWLEDGE", None)

import yaml                                                     # noqa: E402

import heron_fragment as F                                      # noqa: E402
import heron_scope as S                                         # noqa: E402
import heron_search as SEARCH                                   # noqa: E402
import heron_embed as EMBED                                     # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


CASES = (
    u"positive:\n"
    u"  - given: two ducts in the set\n"
    u"    expect: both reported, named\n"
    u"negative:\n"
    u"  - given: a set containing none of what it reports\n"
    u"    expect: zero reported, in words, and no error\n"
)


def card(cards, name, fid, identity, said, capability="DO_A_TEST_THING"):
    """A valid DRAFT card at cards/name. Returns its folder."""
    folder = os.path.join(cards, name)
    os.makedirs(os.path.join(folder, "impl", "any"))
    os.makedirs(os.path.join(folder, "tests"))
    data = {
        "heron-agent": "HERON-RAG-LIB-001", "heron-step": 8,
        "heron-status": "DRAFT", "heron-since": "0.1.0",
        "heron-layer": "brain",
        "id": fid, "semantic-identity": identity, "kind": "filter",
        "domain": "test", "capability": capability, "version": 1,
        "source": "OFFICIAL", "risk": "READ",
        "purpose": "Exists to be indexed - %s." % identity,
        "contract": {
            "needs": [{"name": "doc", "type": "Document"}],
            "provides": [{"name": "elements", "type": "IList<Element>",
                          "role": "result"}],
        },
        "revit": ["2020", "2024"], "runtime": ["net472", "net48"],
        "utterances": [said],
    }
    _write(folder, data)
    io.open(os.path.join(folder, "impl", "any", "fragment.cs"), "w",
            encoding="utf-8").write(u"// %s\n" % identity)
    io.open(os.path.join(folder, "tests", "cases.yaml"), "w",
            encoding="utf-8").write(CASES)
    return folder


def _write(folder, data):
    io.open(os.path.join(folder, "fragment.yaml"), "w", encoding="utf-8").write(
        yaml.safe_dump(data, default_flow_style=False, sort_keys=False))


def prove(folder, checkout):
    """Make the card at `folder` PROVEN, its proof fingerprinted as the
    checkout it lives in reads it - so read from anywhere else, with that
    checkout's paths, it is stale and does not validate."""
    fingerprint = F.load(folder, root=checkout).fingerprint()
    data = yaml.safe_load(io.open(os.path.join(folder, "fragment.yaml"),
                                  encoding="utf-8").read())
    data["heron-status"] = "PROVEN"
    data["proof"] = {
        "date": "2026-10-21", "by": "a test", "model": "no model - a fixture",
        "positive_case": "two ducts in the set, both reported",
        "negative_case": "a set holding none of them reported zero, in words",
        "second_route": "none exists for a fixture",
        "fingerprint": fingerprint,
    }
    _write(folder, data)


def same_path(a, b):
    """The same folder, however its separators are written."""
    if not a or not b:
        return False
    return (os.path.normcase(os.path.abspath(a.replace("/", os.sep)))
            == os.path.normcase(os.path.abspath(b)))


def rewrite(folder, old, new):
    path = os.path.join(folder, "fragment.yaml")
    text = io.open(path, encoding="utf-8").read()
    assert old in text, "%r is not in %s" % (old, path)
    io.open(path, "w", encoding="utf-8").write(text.replace(old, new))


# ---------------------------------------------------------------------------
# The trees: a main checkout and one linked worktree, as git lays them out
# ---------------------------------------------------------------------------

MAIN = os.path.join(WORK, "main")
WT = os.path.join(WORK, "wt")
LOST = os.path.join(WORK, "lost")
MAIN_CARDS = os.path.join(MAIN, "brain", "fragments")
WT_CARDS = os.path.join(WT, "brain", "fragments")
POINTER = os.path.join(MAIN, ".git", "worktrees", "wt")


def on_branch(name):
    io.open(os.path.join(MAIN, ".git", "HEAD"), "w", encoding="utf-8").write(
        u"ref: refs/heads/%s\n" % name)


def lay_out():
    os.makedirs(POINTER)
    io.open(os.path.join(POINTER, "commondir"), "w",
            encoding="utf-8").write(u"../..\n")
    on_branch("main")
    os.makedirs(WT)
    io.open(os.path.join(WT, ".git"), "w", encoding="utf-8").write(
        u"gitdir: %s\n" % POINTER.replace(os.sep, "/"))
    os.makedirs(LOST)
    io.open(os.path.join(LOST, ".git"), "w", encoding="utf-8").write(
        u"gitdir: nowhere/.git/worktrees/lost\n")
    card(os.path.join(LOST, "brain", "fragments"), "do-a-test-thing",
         "FRG-ELE-001", "a lost tree's thing", "do the lost tree's thing")

    # ONE CARD BOTH TREES HOLD, its words different: main's are merged, the
    # worktree's are a session's unmerged edit.
    card(MAIN_CARDS, "do-a-test-thing", "FRG-ELE-001",
         "the merged thing", "do the merged thing")
    card(WT_CARDS, "do-a-test-thing", "FRG-ELE-001",
         "the unmerged thing", "do the unmerged thing")
    # A PROVEN CARD ONLY MAIN HOLDS - merged after the worktree was cut.
    proven = card(MAIN_CARDS, "do-a-proven-thing", "FRG-ELE-020",
                  "a proven merged thing", "do the proven merged thing",
                  capability="DO_A_PROVEN_THING")
    prove(proven, MAIN)
    # A CARD ONLY THE WORKTREE HOLDS - that session's new, unmerged card.
    card(WT_CARDS, "do-an-unmerged-thing", "FRG-ELE-030",
         "an unmerged new thing", "do the unmerged new thing",
         capability="DO_AN_UNMERGED_THING")


REAL = {"S.ROOT": S.ROOT, "F.ROOT": F.ROOT,
        "F.FRAGMENTS_DIR": F.FRAGMENTS_DIR}


def run_from(tree):
    """Plant this process as if Heron were running from `tree` - the way a
    chat's own MCP server runs from its own folder."""
    S.ROOT = tree
    F.ROOT = tree
    F.FRAGMENTS_DIR = os.path.join(tree, "brain", "fragments")


def restore():
    S.ROOT, F.ROOT, F.FRAGMENTS_DIR = (REAL["S.ROOT"], REAL["F.ROOT"],
                                       REAL["F.FRAGMENTS_DIR"])


# ---------------------------------------------------------------------------
# What the store holds
# ---------------------------------------------------------------------------

def shared_file():
    return os.path.join(HOME, "knowledge", "global.db")


def empty_the_store():
    folder = os.path.join(HOME, "knowledge")
    if os.path.isdir(folder):
        shutil.rmtree(folder)


def rows():
    store = S.open_scope(S.GLOBAL)
    try:
        return dict((r["id"], r) for r in store.fragments())
    finally:
        store.close()


def identity(fid):
    row = rows().get(fid)
    return row["semantic_identity"] if row else None


def hit(text):
    store = S.open_scope(S.GLOBAL)
    try:
        SEARCH.ensure_tables(store)
        return SEARCH.short_circuit(store, text)[0]
    finally:
        store.close()


EMBEDDED = []


def stand_in_encoder():
    """(stamp, embed) - what heron_embed.encoder() returns, recording each
    text it is asked to embed."""
    def embed(text):
        EMBEDDED.append(text)
        return [float(len(text)), 1.0]
    return "test-stand-in", embed


def index_both(store, **source):
    """SEARCH.index and EMBED.index on `store`, passing `source` through when
    given. (written, skipped) of each, or the exception either raised."""
    try:
        return SEARCH.index(store, **source), EMBED.index(store, **source)
    except Exception as exc:                     # noqa: BLE001 - named by the check
        return exc


def rebuild(**kwargs):
    """S.rebuild(**kwargs) - its (count, problems), or the exception it raised,
    so a refusal and a crash are both a value a check can read."""
    try:
        return S.rebuild(**kwargs)
    except Exception as exc:                     # noqa: BLE001 - named by the check
        return exc


# ---------------------------------------------------------------------------
# The sections
# ---------------------------------------------------------------------------

def built_from_main():
    print("1. An EMPTY shared store, asked from a worktree, is built from "
          "main's cards (row 5b-404)")
    empty_the_store()
    run_from(WT)
    got = rebuild()
    check(not isinstance(got, Exception),
          "the rebuild runs from a worktree whose main checkout is on main "
          "(%r)" % (got,))
    check(identity("FRG-ELE-001") == "the merged thing",
          "a card both trees hold carries MAIN's words, not the worktree's "
          "(%r)" % (identity("FRG-ELE-001"),))
    held = rows()
    check("FRG-ELE-020" in held,
          "a PROVEN card only main holds is there - validated as main reads "
          "it, so its proof is not stale (%s)" % ", ".join(sorted(held)))
    folder = (held.get("FRG-ELE-020") or {}).get("folder") or ""
    check(folder.replace("\\", "/") == "brain/fragments/do-a-proven-thing",
          "with its folder written as the main checkout writes it (%r)"
          % (folder,))
    check("FRG-ELE-030" not in held,
          "a card only the worktree holds is not (%s)" % ", ".join(sorted(held)))

    # The loader takes the checkout, so a caller can read another checkout's
    # cards as that checkout reads them.
    try:
        found, _ = F.load_all(MAIN_CARDS, checkout=MAIN)
    except TypeError as exc:
        found = {}
        check(False, "heron_fragment.load_all() takes the checkout its cards "
                     "belong to (%s)" % exc)
    else:
        proven = found.get("FRG-ELE-020")
        check(proven is not None and F.validate(proven) == [],
              "load_all(cards, checkout=main) reads main's PROVEN card as "
              "valid (%r)" % (F.validate(proven) if proven else "missing",))
    stale, _ = F.load_all(MAIN_CARDS)
    proven = stale.get("FRG-ELE-020")
    check(proven is not None and F.validate(proven) != [],
          "and without the checkout the same card reads as stale here - so "
          "the check above is the threading, not the fixture")


def indexed_from_main():
    print()
    print("2. The search index and the vectors are built from main's words "
          "(row 136)")
    run_from(WT)
    del EMBEDDED[:]
    store = S.open_scope(S.GLOBAL)
    try:
        SEARCH.index(store, force=True)
        EMBED.index(store, force=True)
        came = SEARCH.indexed_from(store)
    finally:
        store.close()
    check(hit("do the merged thing") == "FRG-ELE-001",
          "main's sentence finds the card by identity (%r)"
          % (hit("do the merged thing"),))
    check(hit("do the unmerged thing") is None,
          "the worktree's unmerged sentence does not (%r)"
          % (hit("do the unmerged thing"),))
    check(hit("do the proven merged thing") == "FRG-ELE-020",
          "and main's PROVEN card is found by its own sentence (%r)"
          % (hit("do the proven merged thing"),))
    said = " | ".join(EMBEDDED)
    check("do the merged thing" in said and "unmerged" not in said,
          "the vectors were made from main's words (%s)" % said[:160])
    check(same_path(came, MAIN_CARDS),
          "and the store records main's card folder as where they came from "
          "(%r)" % (came,))


def worktree_leaves_it():
    print()
    print("3. Main's chat indexed the store; a worktree chat whose card "
          "differs leaves it as it is (row 136's open half)")
    empty_the_store()
    run_from(MAIN)
    rebuild()
    store = S.open_scope(S.GLOBAL)
    try:
        S.refresh(store)
        SEARCH.index(store)
        EMBED.index(store)
    finally:
        store.close()
    check(hit("do the merged thing") == "FRG-ELE-001",
          "main's chat built it from main's words")

    run_from(WT)
    del EMBEDDED[:]
    store = S.open_scope(S.GLOBAL)
    try:
        S.refresh(store)
        searched = SEARCH.index(store)
        embedded = EMBED.index(store)
    finally:
        store.close()
    check(searched[0] == 0,
          "the worktree chat's index writes nothing - its card differs, "
          "main's has not moved (%r)" % (searched,))
    check(embedded[0] == 0 and not EMBEDDED,
          "and embeds nothing (%r, %d text(s))" % (embedded, len(EMBEDDED)))
    check(hit("do the merged thing") == "FRG-ELE-001"
          and hit("do the unmerged thing") is None,
          "so every other chat still finds main's sentence, not the "
          "worktree's (%r, %r)"
          % (hit("do the merged thing"), hit("do the unmerged thing")))
    check(identity("FRG-ELE-001") == "the merged thing",
          "and the row keeps main's identity (%r)"
          % (identity("FRG-ELE-001"),))

    # MAIN MOVES: a merge changes main's card. The worktree's chat, still
    # running its own different card, brings the index level with main.
    rewrite(os.path.join(MAIN_CARDS, "do-a-test-thing"),
            "do the merged thing", "do the newly merged thing")
    store = S.open_scope(S.GLOBAL)
    try:
        S.refresh(store)
        SEARCH.index(store)
        EMBED.index(store)
    finally:
        store.close()
    check(hit("do the newly merged thing") == "FRG-ELE-001"
          and hit("do the unmerged thing") is None,
          "after a merge the worktree's chat indexes MAIN's new sentence "
          "(%r)" % (hit("do the newly merged thing"),))
    rewrite(os.path.join(MAIN_CARDS, "do-a-test-thing"),
            "do the newly merged thing", "do the merged thing")


def server_and_command_line():
    print()
    print("4. The server's own open and the --rebuild command line do the same")
    try:
        import heron_brain as BRAIN
    except Exception as exc:                     # noqa: BLE001
        check(False, "heron_brain imports (%s: %s)" % (type(exc).__name__, exc))
        return

    empty_the_store()
    run_from(WT)
    try:
        with BRAIN._Open():
            pass
        opened = None
    except Exception as exc:                     # noqa: BLE001
        opened = exc
    check(opened is None, "the server opens an empty shared store from a "
                          "worktree (%r)" % (opened,))
    check(identity("FRG-ELE-001") == "the merged thing"
          and "FRG-ELE-020" in rows() and "FRG-ELE-030" not in rows(),
          "and fills it from main's cards (%r; %s)"
          % (identity("FRG-ELE-001"), ", ".join(sorted(rows()))))
    check(hit("do the merged thing") == "FRG-ELE-001"
          and hit("do the unmerged thing") is None,
          "and indexes main's sentence, not the worktree's")

    empty_the_store()
    said = io.StringIO()
    with contextlib.redirect_stdout(said):
        code = S.main(["--rebuild"])
    check(code == 0, "--rebuild from a worktree runs (exit %r)" % (code,))
    check(identity("FRG-ELE-001") == "the merged thing"
          and "FRG-ELE-030" not in rows(),
          "and rebuilds the shared store from main's cards (%r)"
          % (identity("FRG-ELE-001"),))
    check(hit("do the merged thing") == "FRG-ELE-001"
          and hit("do the unmerged thing") is None,
          "and its search index from main's words")


def nothing_written():
    print()
    print("5. Main's cards are not on disk anywhere - nothing is written")
    refused = getattr(S, "SharedStoreRefused", None)
    check(refused is not None,
          "heron_scope names the refusal (SharedStoreRefused)")

    for label, tree, branch in (
            ("a worktree whose main checkout cannot be found", LOST, "main"),
            ("the main checkout on a branch that is not main", MAIN,
             "claude/a-feature"),
            ("a worktree, while main's folder is on a feature branch", WT,
             "claude/a-feature")):
        print("  ..%s" % label)
        on_branch(branch)
        run_from(tree)
        try:
            empty_the_store()
            got = rebuild()
            check(refused is not None and isinstance(got, refused),
                  "an empty shared store is not rebuilt (%r)" % (got,))
            check(not rows(), "and holds no row (%s)" % ", ".join(sorted(rows())))
            plain = str(got) if isinstance(got, Exception) else ""
            check("main checkout" in plain and "HERON_KNOWLEDGE" in plain,
                  "and the refusal says why, and what to do, in plain words "
                  "(%s)" % plain[:120])

            said = io.StringIO()
            with contextlib.redirect_stdout(said):
                try:
                    code = S.main(["--rebuild"])
                except Exception as exc:         # noqa: BLE001
                    code = exc
            check(code not in (0, None) and not isinstance(code, Exception)
                  and not rows(),
                  "--rebuild exits non-zero and writes nothing (%r)" % (code,))

            try:
                import heron_brain as BRAIN
                with BRAIN._Open():
                    pass
                opened = None
            except Exception as exc:             # noqa: BLE001
                opened = exc
            check(type(opened).__name__ == "BrainUnavailable"
                  and "main checkout" in str(opened) and not rows(),
                  "the server's open says so in plain words, and writes "
                  "nothing (%s: %s)"
                  % (type(opened).__name__, str(opened)[:120]))
        finally:
            on_branch("main")

    # A STORE ALREADY BUILT, and a changed card in the main folder on a
    # feature branch: refresh() and both indexes leave it alone.
    print("  ..a built store, the main folder on a feature branch with a "
          "changed card")
    empty_the_store()
    run_from(MAIN)
    rebuild()
    store = S.open_scope(S.GLOBAL)
    try:
        S.refresh(store)
        SEARCH.index(store)
        EMBED.index(store)
    finally:
        store.close()
    on_branch("claude/a-feature")
    edited = os.path.join(MAIN_CARDS, "do-a-test-thing")
    # The sentence first: "the merged thing" is inside it.
    rewrite(edited, "do the merged thing", "do the branch edit")
    rewrite(edited, "the merged thing", "a branch edit")
    del EMBEDDED[:]
    try:
        for tree in (MAIN, WT):
            run_from(tree)
            store = S.open_scope(S.GLOBAL)
            try:
                got = S.refresh(store)
                searched = SEARCH.index(store)
                embedded = EMBED.index(store)
            finally:
                store.close()
            where = "the main folder" if tree == MAIN else "a worktree"
            check(got == [] and identity("FRG-ELE-001") == "the merged thing",
                  "asked from %s, refresh() does not rewrite the row from the "
                  "branch's card (%r, %r)"
                  % (where, got, identity("FRG-ELE-001")))
            check(searched[0] == 0 and embedded[0] == 0 and not EMBEDDED,
                  "and neither index writes (%r, %r)" % (searched, embedded))
            check(hit("do the merged thing") == "FRG-ELE-001"
                  and hit("do the branch edit") is None,
                  "so every chat still finds main's sentence")
    finally:
        on_branch("main")
        rewrite(edited, "a branch edit", "the merged thing")
        rewrite(edited, "do the branch edit", "do the merged thing")

    # BACK ON MAIN the main folder's chat brings it level as before.
    run_from(MAIN)
    rewrite(edited, "the merged thing", "a merged edit")
    store = S.open_scope(S.GLOBAL)
    try:
        got = S.refresh(store)
    finally:
        store.close()
    check(got == ["FRG-ELE-001"] and identity("FRG-ELE-001") == "a merged edit",
          "back on main, the same refresh() rewrites the row (%r)" % (got,))
    rewrite(edited, "a merged edit", "the merged thing")


def private_store():
    print()
    print("6. A PRIVATE store keeps this checkout's own cards")
    private = os.path.join(WORK, "private")
    os.environ["HERON_KNOWLEDGE"] = private
    try:
        run_from(WT)
        got = rebuild()
        held = rows()
        check(not isinstance(got, Exception)
              and (held.get("FRG-ELE-001") or {}).get("semantic_identity")
              == "the unmerged thing" and "FRG-ELE-030" in held,
              "a store HERON_KNOWLEDGE points at is built from the worktree's "
              "own cards (%r; %s)" % (got, ", ".join(sorted(held))))
        store = S.open_scope(S.GLOBAL)
        try:
            SEARCH.index(store, force=True)
        finally:
            store.close()
        check(hit("do the unmerged thing") == "FRG-ELE-001",
              "and indexes the worktree's own words")
        check(os.path.normcase(os.path.dirname(S.scope_path(S.GLOBAL))) ==
              os.path.normcase(private),
              "(the private store is where HERON_KNOWLEDGE says)")
    finally:
        os.environ.pop("HERON_KNOWLEDGE", None)

    # A FOLDER OF ITS OWN INSIDE THE SHARED ONE is still private: no other
    # chat opens a store there. Found by review on PR #717 - "inside the
    # shared folder" called it shared, and from a worktree its rebuild was
    # refused while check-routing's guard let it through.
    inside = os.path.join(HOME, "knowledge", "scratch")
    os.environ["HERON_KNOWLEDGE"] = inside
    try:
        run_from(WT)
        got = rebuild()
        check(not isinstance(got, Exception)
              and identity("FRG-ELE-001") == "the unmerged thing",
              "a HERON_KNOWLEDGE folder INSIDE the shared knowledge folder is "
              "private too - this checkout's cards (%r, %r)"
              % (got, identity("FRG-ELE-001")))
    finally:
        os.environ.pop("HERON_KNOWLEDGE", None)

    # A PROJECT'S STORE under the shared folder's projects/ IS read by every
    # chat that opens that project, so it follows main like the global one.
    empty_the_store()
    run_from(WT)
    got = rebuild(scope=S.PROJECT, project_key="a-test-project")
    store = S.open_scope(S.PROJECT, "a-test-project")
    try:
        held = dict((r["id"], r) for r in store.fragments())
    finally:
        store.close()
    check(not isinstance(got, Exception)
          and (held.get("FRG-ELE-001") or {}).get("semantic_identity")
          == "the merged thing" and "FRG-ELE-030" not in held,
          "a project store in the shared folder's projects/ is built from "
          "main's cards (%r; %s)" % (got, ", ".join(sorted(held))))

    # THE SAME PRIVATE STORE, its HERON_KNOWLEDGE gone again: the shared
    # store is untouched by any of it.
    check(not os.path.exists(shared_file()) or "FRG-ELE-030" not in rows(),
          "and nothing from it reached the shared store")


def main():
    original = EMBED.encoder
    EMBED.encoder = stand_in_encoder
    try:
        lay_out()
        built_from_main()
        indexed_from_main()
        worktree_leaves_it()
        server_and_command_line()
        nothing_written()
        private_store()
    finally:
        EMBED.encoder = original
        restore()
        shutil.rmtree(WORK, ignore_errors=True)

    print()
    if FAILURES:
        print("%d check(s) FAILED" % len(FAILURES))
        return 1
    print("all checks passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
