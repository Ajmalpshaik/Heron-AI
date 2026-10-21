# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The three tools that ask the store through heron_brain, and the guard they lacked.

    python tests/test_skill_store_guard.py

FRAGMENT-ISSUES ROW 5b-1288. On 2026-10-21 `tools/check-skill-routing.py`
was run from a worktree with no HERON_KNOWLEDGE. Its first open of the store
re-indexed the ONE knowledge store every chat on the PC reads from that
worktree's unmerged card, so every live heron_lookup answered from it until
the store was rebuilt from the main checkout. check-routing, check-intrusion,
score-routing and the three measure-* tools ask check-routing's
store_for_this_tree() first (row 5b-233); this one did not, and neither did
`tools/prove-skill.py` when it asks, nor `tools/check-risk-crossings.py
--shared`. All three call it now.

FOR EACH OF THE THREE, FOUR STORES:

  1. THE SHARED STORE, ASKED FROM A LINKED WORKTREE. Not a planted answer: a
     real `.git` FILE naming a main checkout's `.git/worktrees/<name>`, read
     by heron_scope.main_checkout() as a worktree is read on the owner's PC.
     The shared folder is a scratch home (HERON_DATA), so the owner's own
     store is never asked. Twice - holding this tree's library with one row
     edited, as row 5b-1288's store held main's row for the worktree's edited
     card, and empty, which the first open would build from the worktree
     (row 5b-404). Each must be exit 2 - the edited store's file left byte
     for byte as it was, and the empty one holding no card row (the guard's
     own open makes an empty file where there was none, as every
     measurer's does).
  2. THE SHARED STORE, FROM THE MAIN FOLDER ON A FEATURE BRANCH -
     heron_scope.refreshes_from() and on_main_branch() planted, as
     tests/test_measure_routes.py plants them, because a test cannot check a
     branch out in the main folder. Exit 2, nothing written.
  3. THE SHARED STORE, FROM THE MAIN CHECKOUT ON MAIN, and
  4. A PRIVATE STORE (HERON_KNOWLEDGE): both let through to the measurement,
     so the guard is the rule's and not a refusal of every store.

A run the guard lets through is stopped at the re-index, the first step of
every open, which raises instead of indexing - so nothing is ranked and no
sentence is asked. A raise of any other kind is recorded as the exit code,
never let out (heron-ship s2a).

WHAT IT DOES NOT HOLD. The guard compares the store's ROWS with this tree's
cards. A worktree whose cards differ from main's only in words the row does
not keep - utterances, purpose, an impl/ file - passes it, and the open
re-indexes the shared store from them: row 5b-1289, measured, and row 136's
open half. This suite does not claim to close it.
"""

import contextlib
import hashlib
import importlib.util
import io
import os
import shutil
import sys
import tempfile

for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass  # already unicode-safe, or redirected to something that cannot

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real
# trail (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant()

FAILURES = []

# What a run the guard let through returns here: it reached the re-index.
MEASURED = "went on to measure"

TOOLS = (
    ("check-skill-routing.py", ["check-skill-routing.py"]),
    ("prove-skill.py", ["prove-skill.py"]),
    ("check-risk-crossings.py", ["check-risk-crossings.py", "--shared"]),
)


class _Measured(BaseException):
    """The guard let the run through. A BaseException, because heron_brain's
    open turns every Exception into BrainUnavailable, which the tools catch."""


def _stop(store, *_args, **_kwargs):
    """The re-index, stood in for. Closes the open's handle first, so the
    scratch folder can be removed on Windows."""
    store.close()
    raise _Measured()


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load(name):
    """The tool as a module - its filename has a hyphen in it."""
    spec = importlib.util.spec_from_file_location(
        "heron_test_" + name.replace("-", "_").replace(".py", ""),
        os.path.join(ROOT, "tools", name))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _set_env(values):
    """Set each variable in VALUES - None unsets it. Returns what was there."""
    kept = dict((k, os.environ.get(k)) for k in values)
    for key, value in values.items():
        if value is None:
            os.environ.pop(key, None)
        else:
            os.environ[key] = value
    return kept


def _md5(path):
    """The file's bytes, hashed - or None when there is no file."""
    if not os.path.isfile(path):
        return None
    with open(path, "rb") as handle:
        return hashlib.md5(handle.read()).hexdigest()


def _rows(folder):
    """How many card rows the GLOBAL store in FOLDER holds - 0 for none."""
    import heron_scope as SCOPE
    if not os.path.isfile(os.path.join(folder, "global.db")):
        return 0
    kept = _set_env({"HERON_KNOWLEDGE": folder})
    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            return store.count()
        finally:
            store.close()
    finally:
        _set_env(kept)


def _built(folder):
    """A store in FOLDER rebuilt from this tree, to copy for each case."""
    import heron_scope as SCOPE
    kept = _set_env({"HERON_KNOWLEDGE": folder})
    try:
        SCOPE.rebuild()
    finally:
        _set_env(kept)


def _edit(folder):
    """One row of the GLOBAL store in FOLDER made stale - main's row for a
    card this tree has changed."""
    import heron_scope as SCOPE
    kept = _set_env({"HERON_KNOWLEDGE": folder})
    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            some = sorted(row["id"] for row in store.fragments())[0]
            store.db.execute("UPDATE fragments SET semantic_identity = ? "
                             "WHERE id = ?", ("not what the card says", some))
            store.db.commit()
        finally:
            store.close()
    finally:
        _set_env(kept)


def _tail(said):
    """The last words a run printed, for a FAIL line."""
    return " ".join(said.split())[-300:]


def _linked_worktree(top):
    """(worktree, main): a folder whose .git is a FILE naming a main
    checkout's .git/worktrees/wt, as `git worktree add` writes it."""
    main = os.path.join(top, "main")
    gitdir = os.path.join(main, ".git", "worktrees", "wt")
    os.makedirs(gitdir)
    os.makedirs(os.path.join(main, "brain", "fragments"))
    with open(os.path.join(main, ".git", "HEAD"), "w") as handle:
        handle.write("ref: refs/heads/main\n")
    with open(os.path.join(gitdir, "commondir"), "w") as handle:
        handle.write("../..\n")
    tree = os.path.join(top, "wt")
    os.makedirs(tree)
    with open(os.path.join(tree, ".git"), "w") as handle:
        handle.write("gitdir: %s\n" % gitdir)
    return tree, main


def run(tool, argv, env, root=None, source=None, on_main=None):
    """(exit code, what it said) from the tool's main() in environment ENV.

    ROOT plants heron_scope.ROOT, the checkout the scope reads git from;
    SOURCE what refreshes_from() answers and ON_MAIN what on_main_branch()
    answers. All are put back, with the re-index and sys.argv.
    """
    import heron_scope as SCOPE
    import heron_search as SEARCH
    was = (SEARCH.index, SCOPE.ROOT, SCOPE.refreshes_from,
           SCOPE.on_main_branch, list(sys.argv))
    SEARCH.index = _stop
    if root is not None:
        SCOPE.ROOT = root
    if source is not None:
        SCOPE.refreshes_from = lambda root=None: source
    if on_main is not None:
        SCOPE.on_main_branch = lambda root=None: on_main
    sys.argv = list(argv)
    kept = _set_env(env)
    said = io.StringIO()
    try:
        with contextlib.redirect_stdout(said), contextlib.redirect_stderr(said):
            code = tool.main()
    except _Measured:
        code = MEASURED
    except SystemExit as stopped:
        code = stopped.code
    except BaseException as raised:                      # noqa: BLE001
        code = "raised %s: %s" % (type(raised).__name__, raised)
    finally:
        _set_env(kept)
        (SEARCH.index, SCOPE.ROOT, SCOPE.refreshes_from,
         SCOPE.on_main_branch, sys.argv) = was
    return code, said.getvalue()


def guarded(name, argv, built):
    """The four stores, for one tool. BUILT is a folder holding a store
    rebuilt from this tree, copied for each case that wants one."""
    import heron_fragment as FRAG

    print()
    print("%s - the store guard every measurer calls" % name)
    try:
        tool = load(name)
    except BaseException as raised:                      # noqa: BLE001
        check(False, "%s loads (%s: %s)" % (name, type(raised).__name__, raised))
        return

    # 1. The shared store, from a linked worktree - edited, then empty.
    for kind in ("one row edited", "empty"):
        top = tempfile.mkdtemp(prefix="heron-skillguard-wt-")
        try:
            tree, main = _linked_worktree(top)
            home = os.path.join(top, "home")
            shared = os.path.join(home, "knowledge")
            os.makedirs(shared)
            if kind != "empty":
                shutil.copy(os.path.join(built, "global.db"), shared)
                _edit(shared)
            before = _md5(os.path.join(shared, "global.db"))
            code, said = run(tool, argv, {"HERON_KNOWLEDGE": None,
                                          "HERON_DATA": home}, root=tree)
            after = _md5(os.path.join(shared, "global.db"))
            if kind == "empty":
                # No file at all: the guard's own open makes an empty one,
                # as every measurer's does - so no ROW is the test here.
                held = _rows(shared)
                check(code == 2 and held == 0,
                      "the SHARED store (empty), asked from a linked "
                      "worktree, is exit 2 with no card row written (got "
                      "%r, %d rows)" % (code, held))
            else:
                check(code == 2 and after == before,
                      "the SHARED store (%s), asked from a linked worktree, "
                      "is exit 2 and its file is left byte for byte as it "
                      "was (got %r, %s)"
                      % (kind, code, "unchanged" if after == before else
                         "WRITTEN - %d rows now" % _rows(shared)))
            flat = " ".join(said.split())
            check("SHARED" in said and "HERON_KNOWLEDGE" in said
                  and "not the main checkout" in flat,
                  "and it says the store is the shared one, that this is not "
                  "the main checkout, and to set HERON_KNOWLEDGE")
        finally:
            shutil.rmtree(top, ignore_errors=True)

    # 2. The shared store, from the main folder on a feature branch.
    top = tempfile.mkdtemp(prefix="heron-skillguard-branch-")
    try:
        home = os.path.join(top, "home")
        shared = os.path.join(home, "knowledge")
        os.makedirs(shared)
        code, said = run(tool, argv, {"HERON_KNOWLEDGE": None, "HERON_DATA": home},
                         source=(FRAG.FRAGMENTS_DIR, None), on_main=False)
        held = _rows(shared)
        check(code == 2 and held == 0,
              "from the MAIN folder with a feature branch checked out, the "
              "same: exit 2, nothing written (got %r, %d rows)" % (code, held))
        check("a branch that is not main" in " ".join(said.split()),
              "and it says the branch is why")
    finally:
        shutil.rmtree(top, ignore_errors=True)

    # 3. The shared store, from the main checkout on main - let through.
    top = tempfile.mkdtemp(prefix="heron-skillguard-main-")
    try:
        home = os.path.join(top, "home")
        shared = os.path.join(home, "knowledge")
        os.makedirs(shared)
        shutil.copy(os.path.join(built, "global.db"), shared)
        code, said = run(tool, argv, {"HERON_KNOWLEDGE": None, "HERON_DATA": home},
                         source=(FRAG.FRAGMENTS_DIR, None), on_main=True)
        check(code == MEASURED,
              "from the main checkout on main, the shared store holding this "
              "tree's library is measured (got %r)%s"
              % (code, "" if code == MEASURED else ": " + _tail(said)))
    finally:
        shutil.rmtree(top, ignore_errors=True)

    # 4. A private store - every suite, CI and a scratch measurement.
    top = tempfile.mkdtemp(prefix="heron-skillguard-private-")
    try:
        shutil.copy(os.path.join(built, "global.db"), top)
        code, said = run(tool, argv, {"HERON_KNOWLEDGE": top})
        check(code == MEASURED,
              "a PRIVATE store holding this tree's library is measured, on "
              "any checkout (got %r)%s"
              % (code, "" if code == MEASURED else ": " + _tail(said)))
    finally:
        shutil.rmtree(top, ignore_errors=True)


def main():
    print("Row 5b-1288 - the store guard, in the three tools that lacked it")
    print("-" * 66)

    built = tempfile.mkdtemp(prefix="heron-skillguard-built-")
    try:
        _built(built)
        check(_rows(built) > 0, "a store rebuilt from this tree holds its cards")
        for name, argv in TOOLS:
            guarded(name, argv, built)
    finally:
        shutil.rmtree(built, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - none of the three re-indexes the shared store from a")
    print("checkout that is not main on main; each says so and exits 2.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
