# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The one thing measure-routes.py CONCLUDES, and the reason it is parsed not grepped.

    python tests/test_measure_routes.py

WHY THIS TEST EXISTS. Most of that tool counts routes, and a miscount would be
visible. One function draws a CONCLUSION - remember_callers() decides whether
the utterance cache can be written in production at all, and the whole finding
behind Q-43 rests on its answer.

The first version grepped for the text "remember(" and matched THE TOOL'S OWN
DOCSTRING, which describes the problem. It printed "the cache fills with use" -
the exact opposite of the truth - in the one place the tool exists to be right
about. A text search cannot tell a call from a sentence.

So it parses with `ast` now, and these cases are the difference:

  * prose naming remember() in a docstring or comment is NOT a caller
  * a real call is, plain or through a module
  * a file that will not parse is REPORTED, never silently skipped - "no
    production caller" must not be an artefact of a file nobody could read

WHAT IT DOES NOT PROVE. That the cache should or should not be written. That is
Q-43 and it is the owner's to answer.

AND THE STORE IT COUNTS OVER, since 2026-10-09: the tool calls check-routing's
store_for_this_tree(), and the last section plants the four stores that guard
must catch - a renamed id, an edited row, and the SHARED store asked from a
worktree or from the main folder on a feature branch (rows 5b-229 and 5b-233).
The re-index is replaced by a raise, so no route is counted.
"""

import contextlib
import io
import os
import sys
import shutil
import tempfile
import importlib.util

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FAILURES = []


def load(root=None):
    """Import the tool by path, optionally pointed at a temporary tree."""
    path = os.path.join(ROOT, "tools", "measure-routes.py")
    spec = importlib.util.spec_from_file_location("measure_routes", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    if root is not None:
        module.ROOT = root
    return module


def check(name, condition, detail=""):
    if condition:
        print("  ok    %s" % name)
    else:
        print("  FAIL  %s %s" % (name, detail))
        FAILURES.append("%s %s" % (name, detail))


def write(folder, name, body):
    path = os.path.join(folder, name)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    io.open(path, "w", encoding="utf-8").write(body)


# ---------------------------------------------------------------------------
# The store it measures over - check-routing's guard (rows 5b-229, 5b-233)
# ---------------------------------------------------------------------------

# What the re-index, the measurement's first step, raises in the store
# section, so a run the guard lets through stops there in seconds.
MEASURED = "went on to measure"


class _Measured(Exception):
    """The store guard let the run through."""


def _stop(*_args, **_kwargs):
    raise _Measured()


def _set_env(values):
    """Set each variable in VALUES - None unsets it. Returns what was there."""
    kept = dict((k, os.environ.get(k)) for k in values)
    for key, value in values.items():
        if value is None:
            os.environ.pop(key, None)
        else:
            os.environ[key] = value
    return kept


def _store_rows(home):
    """{id: row} the GLOBAL store in HOME holds, read without changing it."""
    import heron_scope as SCOPE
    kept = _set_env({"HERON_KNOWLEDGE": home})
    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            return dict((row["id"], row) for row in store.fragments())
        finally:
            store.close()
    finally:
        _set_env(kept)


def _edit_store(home, sql, args):
    """One statement against the GLOBAL store in HOME - a stale store, made."""
    import heron_scope as SCOPE
    kept = _set_env({"HERON_KNOWLEDGE": home})
    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            store.db.execute(sql, args)
            store.db.commit()
        finally:
            store.close()
    finally:
        _set_env(kept)


def _built_store():
    """A private scratch folder holding a store rebuilt from this tree."""
    import heron_scope as SCOPE
    home = tempfile.mkdtemp(prefix="heron-routes-store-")
    kept = _set_env({"HERON_KNOWLEDGE": home})
    try:
        SCOPE.rebuild()
    finally:
        _set_env(kept)
    return home


def _guarded(tool, env, source=None, on_main=None):
    """(exit code, what it said) from the tool's main([]) in environment ENV.

    The re-index raises instead of running, so the code is MEASURED when the
    store guard let the run through. SOURCE plants what
    heron_scope.refreshes_from() answers, ON_MAIN what
    heron_scope.on_main_branch() answers: the main folder's HEAD, which a
    test cannot check a branch out in. A raise of any other kind is
    recorded as the code, never let out (heron-ship s2a).
    """
    import heron_scope as SCOPE
    import heron_search as SEARCH
    was = (SEARCH.index, SCOPE.refreshes_from,
           getattr(SCOPE, "on_main_branch", None))
    SEARCH.index = _stop
    if source is not None:
        SCOPE.refreshes_from = lambda root=None: source
    if on_main is not None:
        SCOPE.on_main_branch = lambda root=None: on_main
    kept = _set_env(env)
    said = io.StringIO()
    try:
        with contextlib.redirect_stdout(said), contextlib.redirect_stderr(said):
            code = tool.main([])
    except _Measured:
        code = MEASURED
    except BaseException as raised:                      # noqa: BLE001
        code = "raised %s: %s" % (type(raised).__name__, raised)
    finally:
        _set_env(kept)
        SEARCH.index, SCOPE.refreshes_from = was[0], was[1]
        if on_main is not None:
            if was[2] is None:
                del SCOPE.on_main_branch
            else:
                SCOPE.on_main_branch = was[2]
    return code, said.getvalue()


def store_guard(tool, check):
    """Until 2026-10-09 this tool kept its own copy of the store check - the
    ids at most, no row's content, and a rebuild of the ONE store every chat
    on the PC reads from whatever checkout it ran in. It calls
    check-routing's store_for_this_tree() now, and these are the four
    stores that guard must catch. CHECK is called as check(passed, what)."""
    import heron_fragment as FRAG

    home = _built_store()
    try:
        some = sorted(_store_rows(home))[0]
        stranger = "FRG-NOT-ON-DISK-000"
        _edit_store(home, "UPDATE fragments SET id = ? WHERE id = ?",
                    (stranger, some))
        code, said = _guarded(tool, {"HERON_KNOWLEDGE": home})
        now = _store_rows(home)
        check(code == MEASURED and some in now and stranger not in now,
              "a store with one id renamed - as many rows as cards, not "
              "THESE cards - is rebuilt before anything is measured (got %r)"
              % (code,))
        check(stranger in said, "and the run names the id that did not match")
        wrong = "an identity the card no longer says"
        _edit_store(home, "UPDATE fragments SET semantic_identity = ? "
                          "WHERE id = ?", (wrong, some))
        code, said = _guarded(tool, {"HERON_KNOWLEDGE": home})
        now = _store_rows(home).get(some) or {}
        check(code == MEASURED
              and now.get("semantic_identity") not in (None, wrong),
              "a store with one row's identity edited is rebuilt - every "
              "row's content is compared, not only the ids (got %r)" % (code,))
        check("edited since" in said and some in said,
              "and the run names the card")
    finally:
        shutil.rmtree(home, ignore_errors=True)

    home = tempfile.mkdtemp(prefix="heron-routes-shared-")
    try:
        code, said = _guarded(
            tool, {"HERON_KNOWLEDGE": home},
            source=(os.path.join("main", "brain", "fragments"), "main"))
        held = len(_store_rows(home))
        check(code == 2 and held == 0,
              "the SHARED store, asked from a checkout that is not main, is "
              "exit 2 with nothing written (got %r, %d rows)" % (code, held))
        check("SHARED" in said and "HERON_KNOWLEDGE" in said,
              "and it says to point HERON_KNOWLEDGE at a scratch folder")
    finally:
        shutil.rmtree(home, ignore_errors=True)

    top = tempfile.mkdtemp(prefix="heron-routes-mainfolder-")
    try:
        appdata = os.path.join(top, "appdata")
        shared = os.path.join(appdata, "Heron", "knowledge")
        os.makedirs(shared)
        code, said = _guarded(tool, {"HERON_KNOWLEDGE": None,
                                     "APPDATA": appdata},
                              source=(FRAG.FRAGMENTS_DIR, None), on_main=False)
        held = len(_store_rows(shared))
        check(code == 2 and held == 0,
              "and from the MAIN folder with a feature branch checked out, "
              "the same: exit 2, nothing written (got %r, %d rows)"
              % (code, held))
        check("a branch that is not main" in " ".join(said.split()),
              "and it says the branch is why")
    finally:
        shutil.rmtree(top, ignore_errors=True)


def main():
    print("remember_callers() - a call, not a sentence")
    print("-" * 62)

    work = tempfile.mkdtemp(prefix="heron-routes-")
    try:
        tool = load(work)

        # The exact shape that fooled the first version: prose describing the
        # function, in a docstring, with the parenthesis attached.
        write(work, "prose.py", '"""remember() is never called here."""\nX = 1\n')
        write(work, "comment.py", "# nothing calls remember(store, text, fid)\nY = 2\n")
        check("a docstring naming remember() is not a caller",
              tool.remember_callers() == [],
              "(got %r)" % tool.remember_callers())

        write(work, "plain.py", "from x import remember\ndef go(s, t):\n    remember(s, t, 'FRG-1')\n")
        got = tool.remember_callers()
        check("a plain call IS a caller", got == ["plain.py"], "(got %r)" % got)

        write(work, "viamodule.py", "import heron_search as S\ndef go(s, t):\n    S.remember(s, t, 'FRG-1')\n")
        got = tool.remember_callers()
        check("a call through a module IS a caller",
              got == ["plain.py", "viamodule.py"], "(got %r)" % got)

        # A name that merely CONTAINS remember must not match. `ast` gives this
        # for free where a substring grep would not.
        write(work, "similar.py", "def go(s):\n    s.remembers()\n    s.remember_all()\n")
        got = tool.remember_callers()
        check("remembers() and remember_all() are not remember()",
              "similar.py" not in got, "(got %r)" % got)

        write(work, "broken.py", "def go(:\n")
        got = tool.remember_callers()
        check("a file that will not parse is reported, not skipped",
              "UNREADABLE:broken.py" in got, "(got %r)" % got)

        # The definition itself is not a call. Without this, heron_search.py
        # would always look like its own caller and the finding would invert.
        shutil.rmtree(work)
        os.makedirs(work)
        write(work, "defonly.py", "def remember(store, text, fid):\n    pass\n")
        check("the definition of remember() is not a call to it",
              tool.remember_callers() == [],
              "(got %r)" % tool.remember_callers())

        # __pycache__ holds stale copies of production code; counting one would
        # report a caller that no longer exists in the tree.
        write(work, os.path.join("__pycache__", "old.py"), "S.remember(a, b, c)\n")
        check("__pycache__ is not searched",
              tool.remember_callers() == [],
              "(got %r)" % tool.remember_callers())
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print()
    print("is_test() - a caller in a test is not a production caller")
    print("-" * 62)
    tool = load()
    check("tests/ is a test", tool.is_test(os.path.join("tests", "test_search.py")))
    check("a test_ file anywhere is a test",
          tool.is_test(os.path.join("brain", "test_thing.py")))
    check("brain/heron_search.py is not a test",
          not tool.is_test(os.path.join("brain", "heron_search.py")))

    print()
    print("the real tree - the finding behind Q-43")
    print("-" * 62)
    callers = tool.remember_callers()
    unreadable = [c for c in callers if c.startswith("UNREADABLE:")]
    production = [c for c in callers if not c.startswith("UNREADABLE:") and not tool.is_test(c)]
    check("every .py in the tree parses", not unreadable, "(%r)" % unreadable)
    print("        callers found: %s" % (", ".join(callers) or "none"))
    if production:
        print("        NOTE: remember() now has a production caller (%s)."
              % ", ".join(production))
        print("        Q-43 has been acted on. The tool's warning must be")
        print("        re-read, and this note is how you find out.")

    print()
    print("the store it counts routes over - check-routing's guard, rows 5b-229 and 5b-233")
    print("-" * 62)
    store_guard(load(), lambda passed, what: check(what, passed))

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - prose is not a call, a module call is, a name that merely")
    print("contains 'remember' is not, and a file that will not parse is named")
    print("rather than quietly dropped out of the answer.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
