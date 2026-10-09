#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The file that forbids two numbers for one measurement, carrying two.

    python tests/test_check_intrusion.py

`tools/check-intrusion.py` asks the question `check-routing.py` cannot: for
every utterance in the library, who ELSE came back? It is the last of the
seven CI gates that had never been opened, and this is the first suite it has
ever had.

ITS DOCSTRING STATES THE RULE AND ITS OWN OUTPUT BREAKS IT. The docstring
records what the tool measured the first time it ran, and then says in terms:

    Quote what THIS TOOL prints, never this line. An earlier hand-written
    probe on the same day gave 0.234 over 312 utterances - a different corpus
    one commit back, not a different finding. TWO NUMBERS FOR ONE MEASUREMENT
    IS HOW A FIGURE BECOMES A REMEMBERED COMPOSITE that matches no run that
    ever happened, which is a failure this repository has already had once and
    now has a checker for.

And then the branch that fires when the correlation has gone strong prints
the HAND PROBE's number as what the tool measured - at exactly the moment a
reader needs the right baseline to compare against.

THIS SUITE NEVER RUNS THE MEASUREMENT. That opens the knowledge store,
re-indexes it and retrieves for every utterance in the library - measured
2026-09-22 at 396 fragments and 2,227 utterances, and it takes minutes. So
what is asked here is the arithmetic, the two claims agreeing with each
other, and the settings being refused before any of that starts.

AND THE STORE IT MEASURES OVER (sections 5-7). Until 2026-10-09 the tool
rebuilt its store when store.count() differed from the number of card
folders - a COUNT, which check-routing.py stopped trusting on 2026-09-06 -
and it rebuilt the ONE store every chat on the PC reads from any checkout,
which check-routing stopped doing on 2026-10-08 (row 5b-233). It uses
check-routing's guard now. Those sections run the tool's main() on a private
scratch store and STOP IT at the first step of the measurement - the
re-index is replaced by a raise - so they ask only what the guard did to the
store, in seconds. Sections 8-9, 2026-10-09: the MAIN folder with a feature
branch checked out is refused the shared store too, as the lookup always
refused it, while the main folder on main and CI's private store still
rebuild.

WHAT IT CANNOT DO: it does not say whether any intrusion is a defect. The
tool says plainly that none of them is by itself, and exits 0 for that
reason - "a gate here would be a gate on how ordinary somebody's phrasing
is".

    python tests/test_check_intrusion.py
"""

import contextlib
import importlib.util
import io
import os
import re
import sys

import shutil
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-intrusion.py")
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []

# What the measurement's first step raises in sections 5-7, so a run the
# guard lets through stops there instead of retrieving for every utterance.
MEASURED = "went on to measure"


class _Measured(Exception):
    """The guard let the run through to the measurement."""


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


@contextlib.contextmanager
def knowledge_at(home):
    """HERON_KNOWLEDGE pointed at HOME for the block, and put back after."""
    was = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        yield
    finally:
        if was is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = was


def run_guard(tool, home, shared=False):
    """(exit code, what it said) from the tool's main() on the store in HOME.

    The code is MEASURED when the guard let the run through - the re-index,
    the measurement's first step, raises instead of running. SHARED tells
    heron_scope the store is the one every chat on the PC reads, asked from
    a checkout that is not main: check-routing's own section 6 arrangement.
    A raise of any other kind is recorded as the code, never let out, so one
    broken case cannot hide the sections after it (heron-ship s2a).
    """
    import heron_scope as SCOPE
    import heron_search as SEARCH

    def stop(*_args, **_kwargs):
        raise _Measured()

    was_index = SEARCH.index
    was_from = SCOPE.refreshes_from
    SEARCH.index = stop
    if shared:
        SCOPE.refreshes_from = lambda root=None: (
            os.path.join("main", "brain", "fragments"), "main")
    said = io.StringIO()
    try:
        with knowledge_at(home):
            with contextlib.redirect_stdout(said):
                with contextlib.redirect_stderr(said):
                    code = tool.main([])
    except _Measured:
        code = MEASURED
    except BaseException as raised:                 # noqa: BLE001
        code = "raised %s: %s" % (type(raised).__name__, raised)
    finally:
        SEARCH.index = was_index
        SCOPE.refreshes_from = was_from
    return code, said.getvalue()


def run_in_main_folder(tool, top, on_main):
    """(exit code, what it said, rows left) from the tool's main() on the
    SHARED store, asked from the MAIN checkout - on branch main when ON_MAIN,
    on a feature branch when not.

    The store is the shared one by its own spelling: %APPDATA%\\Heron\\knowledge
    under TOP, with HERON_KNOWLEDGE unset, as on the owner's PC. Planted:
    refreshes_from() answers what it answers in the main checkout, this
    checkout's own cards, and on_main_branch() answers ON_MAIN - the main
    folder's HEAD, which a test cannot check a branch out in. The re-index
    raises, as in run_guard().
    """
    import heron_fragment as FRAG
    import heron_scope as SCOPE
    import heron_search as SEARCH

    appdata = os.path.join(top, "appdata")
    shared = os.path.join(appdata, "Heron", "knowledge")
    if not os.path.isdir(shared):
        os.makedirs(shared)

    def stop(*_args, **_kwargs):
        raise _Measured()

    env = dict((k, os.environ.get(k)) for k in ("APPDATA", "HERON_KNOWLEDGE"))
    was_index, was_from = SEARCH.index, SCOPE.refreshes_from
    was_branch = getattr(SCOPE, "on_main_branch", None)
    SEARCH.index = stop
    SCOPE.refreshes_from = lambda root=None: (FRAG.FRAGMENTS_DIR, None)
    SCOPE.on_main_branch = lambda root=None: on_main
    os.environ.pop("HERON_KNOWLEDGE", None)
    os.environ["APPDATA"] = appdata
    said = io.StringIO()
    try:
        with contextlib.redirect_stdout(said):
            with contextlib.redirect_stderr(said):
                code = tool.main([])
    except _Measured:
        code = MEASURED
    except BaseException as raised:                 # noqa: BLE001
        code = "raised %s: %s" % (type(raised).__name__, raised)
    finally:
        SEARCH.index, SCOPE.refreshes_from = was_index, was_from
        if was_branch is None:
            del SCOPE.on_main_branch
        else:
            SCOPE.on_main_branch = was_branch
        for key, value in env.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value
    return code, said.getvalue(), len(held_in(shared))


def held_in(home):
    """{id: row} the GLOBAL store in HOME holds, read without changing it."""
    import heron_scope as SCOPE
    with knowledge_at(home):
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            return dict((row["id"], row) for row in store.fragments())
        finally:
            store.close()


def edit_store(home, sql, args):
    """One statement against the GLOBAL store in HOME - a stale store, made."""
    import heron_scope as SCOPE
    with knowledge_at(home):
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            store.db.execute(sql, args)
            store.db.commit()
        finally:
            store.close()


def built_store():
    """A private scratch folder holding a store rebuilt from this tree."""
    import heron_scope as SCOPE
    home = tempfile.mkdtemp(prefix="heron-ci-")
    with knowledge_at(home):
        SCOPE.rebuild()
    return home


def load():
    """The tool as a module, or None - its name has a hyphen in it."""
    try:
        spec = importlib.util.spec_from_file_location("check_intrusion", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    check(tool is not None, "tools/check-intrusion.py loads")
    if tool is None:
        print()
        print("FAILED")
        for line in FAILURES:
            print("  - %s" % line)
        return 1

    source = io.open(TOOL, encoding="utf-8").read()

    print()
    print("1. Pearson, written out, with no dependency for it")
    r = getattr(tool, "correlation", None)
    check(callable(r), "it still has correlation()")
    if callable(r):
        check(abs(r([1, 2, 3], [2, 4, 6]) - 1.0) < 1e-9,
              "a perfect straight line is 1.0")
        check(abs(r([1, 2, 3], [6, 4, 2]) + 1.0) < 1e-9,
              "and the same line downhill is -1.0")
        check(abs(r([1, 2, 3], [5, 5, 5])) < 1e-9,
              "a flat answer correlates with nothing - and dividing by that "
              "zero would be a crash, so it returns 0.0")
        check(abs(r([1], [1])) < 1e-9,
              "one point is not a correlation either")

    print()
    print("2. TWO NUMBERS FOR ONE MEASUREMENT")
    # The docstring records what THIS TOOL measured the first time it ran,
    # and separately names a hand probe's number as NOT that. The branch
    # that quotes a past figure back at a reader must quote the tool's own.
    recorded = re.findall(r"correlation\(purpose words, intrusions\) = "
                          r"(0\.\d+), over (\d+) utterances", source)
    check(len(recorded) == 1,
          "the docstring records what the tool itself measured first, once")
    recorded = recorded[0] if recorded else None
    # WHICHEVER WORDS THE BRANCH USES, the figure it calls "measured" is the
    # one being compared against. A second figure the file merely mentions is
    # introduced as something else - the docstring says "gave" for the hand
    # probe - so the word is the distinction, not the number.
    quoted = re.findall(r"measured\s+(0\.\d+)", source)
    check(len(quoted) == 1,
          "the strong-correlation branch quotes exactly one figure as "
          "measured, and it quotes %d" % len(quoted))
    if recorded and quoted:
        check(recorded[0] == quoted[0],
              "and it is THE SAME NUMBER the docstring records - the "
              "docstring says %s and the branch says %s"
              % (recorded[0], quoted[0]))

    print()
    print("3. A SETTING IT DOES NOT HAVE IS REFUSED BEFORE THE STORE OPENS")
    # Opening the store and retrieving for 2,227 utterances takes minutes.
    # A mistyped flag must not cost that, and must not be silently ignored.
    settings = getattr(tool, "settings_from", None)
    check(callable(settings),
          "the settings are read by something a test can call, so a bad one "
          "is refused BEFORE the store is opened")
    if callable(settings):
        for label, argv in (("a misspelt flag", ["--tpo", "5"]),
                            ("a flag with no value", ["--top"]),
                            ("a count that is not a number", ["--top", "six"]),
                            ("a bare word", ["12"])):
            said = io.StringIO()
            try:
                with contextlib.redirect_stdout(said):
                    with contextlib.redirect_stderr(said):
                        code = tool.main(argv)
            except BaseException as raised:         # noqa: BLE001
                code = "raised %s" % type(raised).__name__
            check(code == 2,
                  "%s is refused with 2, and it gives %r" % (label, code))
            check("Fragments:" not in said.getvalue(),
                  "%s: and no measurement was run" % label)
        top, why = settings(["--top", "20"])
        check(why is None and top == 20,
              "and a setting it does have is accepted: %r" % (top,))

    print()
    print("4. It is a report, and it says so")
    check("return 0" in source and "not a defect" in source,
          "it exits 0 whatever it finds - a gate here would be a gate on how "
          "ordinary somebody's phrasing is")
    check("COULD NOT RUN" in source and "return 2" in source,
          "and no knowledge store is COULD NOT RUN at exit 2, not a pass and "
          "not a traceback")

    print()
    print("5. THE STORE IS CHECKED BY ITS IDS, NOT ITS COUNT")
    # check-routing.py, 2026-09-06: three parallel sessions each had 226
    # cards, the count matched, and the store held another session's library.
    # Made here by renaming one row: as many rows as cards, not THESE cards.
    home = built_store()
    try:
        held = held_in(home)
        some = sorted(held)[0]
        stranger = "FRG-NOT-ON-DISK-000"
        edit_store(home, "UPDATE fragments SET id = ? WHERE id = ?",
                   (stranger, some))
        code, said = run_guard(tool, home)
        now = held_in(home)
        check(code == MEASURED,
              "a stale private store is put right and the run goes on to "
              "measure (got %r)" % (code,))
        check(some in now and stranger not in now,
              "a store holding as many rows as there are cards, but not THESE "
              "cards, is rebuilt (%s back: %s; %s gone: %s)"
              % (some, some in now, stranger, stranger not in now))
        check(stranger in said,
              "and the run names the id that did not match: %s"
              % " ".join(said.split())[:160])
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    print("6. AND BY EVERY ROW'S CONTENT, NOT ONLY ITS IDS - row 5b-229")
    # The search reads a card's identity, capability and domain from the
    # store's ROW, so a row the card no longer matches is searched under
    # words the card does not say. A store of its own, so a section 5 that
    # failed to put its store right cannot take this one down with it.
    home = built_store()
    try:
        some = sorted(held_in(home))[0]
        code, said = run_guard(tool, home)
        check(code == MEASURED and "did not match" not in said,
              "a store that already matches this tree is left alone (got %r)"
              % (code,))
        wanted = held_in(home)[some]["semantic_identity"]
        edit_store(home,
                   "UPDATE fragments SET semantic_identity = ? WHERE id = ?",
                   ("an identity the card no longer says", some))
        code, said = run_guard(tool, home)
        now = held_in(home).get(some) or {}
        check(code == MEASURED,
              "a store with one edited row goes on to measure once put right "
              "(got %r)" % (code,))
        check(now.get("semantic_identity") == wanted,
              "the row whose identity is not its card's is put back from the "
              "card - it reads %r" % (now.get("semantic_identity"),))
        check("edited since" in said and some in said,
              "and the run says which card's row was stale")
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    print("7. THE SHARED STORE IS NOT REBUILT FROM A CHECKOUT THAT IS NOT MAIN - row 5b-233")
    # One store serves every checkout and chat on the PC. Rebuilt from a
    # worktree, every session's heron_lookup named that branch's cards.
    # check-routing has refused since 2026-10-08; this tool did not.
    home = tempfile.mkdtemp(prefix="heron-ci-shared-")
    try:
        code, said = run_guard(tool, home, shared=True)
        held = held_in(home)
        check(code == 2,
              "a run that would rebuild the shared store from here exits 2 "
              "(got %r)" % (code,))
        check(len(held) == 0,
              "and the store is left as it was - nothing rebuilt into it "
              "(%d rows)" % len(held))
        check("HERON_KNOWLEDGE" in said and "SHARED" in said,
              "and it says the store is the shared one and to point "
              "HERON_KNOWLEDGE at a scratch folder")
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    print("8. NOR FROM THE MAIN FOLDER WITH A FEATURE BRANCH CHECKED OUT - row 5b-233")
    # refreshes_from() names the main checkout there whatever its branch, so
    # the guard let that branch's cards into the store every chat reads. The
    # lookup never did: heron_brain only warms or advises a rebuild of the
    # shared store from the main checkout ON main. One rule now, in
    # heron_scope.rebuild_refusal(), and this guard asks it.
    top = tempfile.mkdtemp(prefix="heron-ci-mainfolder-")
    try:
        code, said, held = run_in_main_folder(tool, top, on_main=False)
        check(code == 2,
              "the shared store, asked from the main folder on a feature "
              "branch, exits 2 (got %r)" % (code,))
        check(held == 0,
              "and nothing is rebuilt into it (%d rows)" % held)
        check("HERON_KNOWLEDGE" in said
              and "a branch that is not main" in " ".join(said.split()),
              "and it says the branch is why, and to point HERON_KNOWLEDGE "
              "at a scratch folder")
    finally:
        shutil.rmtree(top, ignore_errors=True)
    top = tempfile.mkdtemp(prefix="heron-ci-mainfolder-")
    try:
        code, said, held = run_in_main_folder(tool, top, on_main=True)
        check(code == MEASURED and held > 0,
              "the main folder ON main still rebuilds the shared store - it "
              "is where that store comes from (got %r, %d rows)" % (code, held))
    finally:
        shutil.rmtree(top, ignore_errors=True)

    print()
    print("9. CI'S PRIVATE STORE IS NOT TOUCHED BY ANY OF THIS")
    # gates.yml runs this on a fresh HERON_KNOWLEDGE, on a checkout whose HEAD
    # is not main. A private store is rebuilt on any branch: nobody else
    # reads it.
    home = tempfile.mkdtemp(prefix="heron-ci-private-")
    import heron_scope as SCOPE
    was_branch = getattr(SCOPE, "on_main_branch", None)
    SCOPE.on_main_branch = lambda root=None: False
    try:
        code, said = run_guard(tool, home)
        held = held_in(home)
        check(code == MEASURED and len(held) > 0,
              "an empty private store, on a branch that is not main, is "
              "rebuilt and measured (got %r, %d rows)" % (code, len(held)))
    finally:
        if was_branch is None:
            del SCOPE.on_main_branch
        else:
            SCOPE.on_main_branch = was_branch
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - one number for one measurement, a setting it does not")
    print("have costs nothing, and the store it measures over holds this")
    print("tree's cards - never rebuilt from here when it is the shared one.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
