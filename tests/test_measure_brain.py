# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The two pieces of measure-brain.py that COMPUTE rather than time.

    python tests/test_measure_brain.py

WHY ONLY TWO PIECES. Most of that tool is a clock around somebody else's code,
and a clock cannot be unit tested into being right. Two parts do arithmetic,
and a wrong answer in either produces a table that looks entirely normal:

  sample()  decides WHICH requests are asked. If it is not deterministic, two
            runs ask different questions and the whole point - comparing a run
            against a baseline - quietly stops working. Nothing about the
            output would look wrong.

  stat()    computes the median and the worst. A median is the one statistic
            people read without checking, and an even-length list is where a
            hand-written one usually goes wrong.

WHAT IT DOES NOT PROVE. That any timing is accurate. That is the machine's
business, and the tool prints the machine for exactly that reason.

AND THE STORE IT TIMES, since 2026-10-09: the tool calls check-routing's
store_for_this_tree(), and the last section plants the four stores that
guard must catch - a renamed id, an edited row, and the SHARED store asked
from a worktree or from the main folder on a feature branch (rows 5b-229 and
5b-233). The re-index is replaced by a raise, so nothing is timed.
"""

import contextlib
import io
import os
import shutil
import sys
import tempfile
import importlib.util

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FAILURES = []


def load():
    """Import the tool by path - it lives in tools/ and is not a package."""
    path = os.path.join(ROOT, "tools", "measure-brain.py")
    spec = importlib.util.spec_from_file_location("measure_brain", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def check(name, condition, detail=""):
    if condition:
        print("  ok    %s" % name)
    else:
        print("  FAIL  %s %s" % (name, detail))
        FAILURES.append("%s %s" % (name, detail))


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
    home = tempfile.mkdtemp(prefix="heron-brain-store-")
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

    home = tempfile.mkdtemp(prefix="heron-brain-shared-")
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

    top = tempfile.mkdtemp(prefix="heron-brain-mainfolder-")
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
    tool = load()

    print("sample() - the same questions every run")
    print("-" * 62)

    phrases = ["select all ducts", "grey the background", "count the sheets",
               "find unused materials", "move them up", "tag every duct"]

    first = tool.sample(phrases, 3)
    second = tool.sample(phrases, 3)
    check("two calls return the same list", first == second,
          "(%r vs %r)" % (first, second))

    shuffled = list(reversed(phrases))
    check("directory order does not change the sample",
          tool.sample(shuffled, 3) == first,
          "(%r vs %r)" % (tool.sample(shuffled, 3), first))

    check("asking for 3 gives 3", len(first) == 3, "(got %d)" % len(first))

    # A stride, not the first N. Taking phrases[:3] would sample one corner of
    # an alphabetically sorted library - every fragment whose words start with
    # 'a' - and call it a spread.
    check("it strides rather than taking the first three",
          first != sorted(set(phrases))[:3],
          "(got the first three: %r)" % first)

    everything = tool.sample(phrases, 99)
    check("asking for more than there are returns all of them",
          len(everything) == len(set(phrases)),
          "(got %d of %d)" % (len(everything), len(set(phrases))))

    check("an empty library samples to nothing", tool.sample([], 5) == [])

    # Duplicated utterances are real: two fragments may declare the same words,
    # and asking the same question twice would weight the median toward it.
    check("a duplicate is asked once",
          len(tool.sample(["a", "a", "b"], 9)) == 2,
          "(got %r)" % tool.sample(["a", "a", "b"], 9))

    print()
    print("stat() - median and worst")
    print("-" * 62)

    clock = tool.Timer()
    for value in (10.0, 2.0, 6.0):
        clock.readings.setdefault("s", []).append(value)
    clock.order.append("s")
    runs, median, worst = clock.stat("s")
    check("odd count takes the middle value", median == 6.0, "(got %r)" % median)
    check("worst is the largest, not the last", worst == 10.0, "(got %r)" % worst)
    check("runs counts every reading", runs == 3, "(got %d)" % runs)

    even = tool.Timer()
    for value in (1.0, 2.0, 3.0, 4.0):
        even.readings.setdefault("s", []).append(value)
    even.order.append("s")
    _runs, median, _worst = even.stat("s")
    check("even count averages the middle pair", median == 2.5, "(got %r)" % median)

    empty = tool.Timer()
    check("a stage never run reports nothing rather than zero",
          empty.stat("never") == (0, None, None),
          "(got %r)" % (empty.stat("never"),))

    print()
    print("time() - the reading is kept and the value passes through")
    print("-" * 62)

    passed = tool.Timer()
    got = passed.time("work", lambda: "the answer")
    check("the wrapped call's return value comes back", got == "the answer",
          "(got %r)" % got)
    check("one call leaves one reading", len(passed.readings["work"]) == 1)
    check("the stage is remembered in call order", passed.order == ["work"],
          "(got %r)" % passed.order)

    print()
    print("fmt() - a number a person reads")
    print("-" * 62)
    check("nothing measured prints a dash", tool.fmt(None) == "-")
    check("sub-10 ms keeps two decimals", tool.fmt(1.234) == "1.23",
          "(got %r)" % tool.fmt(1.234))
    check("over 100 ms drops them", tool.fmt(1246.4) == "1246",
          "(got %r)" % tool.fmt(1246.4))

    print()
    print("the store it times - check-routing's guard, rows 5b-229 and 5b-233")
    print("-" * 62)
    store_guard(tool, lambda passed, what: check(what, passed))

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the sample is deterministic and independent of directory")
    print("order, the median survives an even count, and a stage that never ran")
    print("reports nothing rather than zero.")
    print()
    print("It proves nothing about whether a timing is accurate. That is the")
    print("machine's business, which is why the tool prints the machine.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
