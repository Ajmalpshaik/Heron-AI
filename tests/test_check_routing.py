# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RAG-RNK-006
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The routing checker's own command line - the two ways it could not answer.

    python tests/test_check_routing.py

WHY THIS EXISTS
---------------
`tools/check-routing.py` is one of the twelve runs `.github/workflows/gates.yml`
makes, and it had no suite at all. Both things checked here are cases where the
tool is supposed to REFUSE, and a refusal is the one behaviour nobody notices
is broken: the happy path is exercised on every pull request, and neither of
these is.

WHAT IT PROVES
  1. `--revit` WITH NO VALUE IS A REFUSAL, NOT A TRACEBACK. Row 5b-104. The
     flag's value was read as `argv[i + 1]` with no guard, so `--revit` last on
     the line came back as `IndexError: list index out of range` and exit 1 -
     four lines above a comment about not answering with a traceback.

  2. A FLAG STANDING WHERE A RELEASE SHOULD BE IS REFUSED TOO. Otherwise the
     library is filtered to the Revit release called `--rebuild`, and an empty
     result is printed as a routing measurement.

  3. NO KNOWLEDGE STORE IS EXIT 2 AND A SENTENCE. Row 5b-71: until 2026-09-21
     this raised `ValueError` from four frames down while `gates.yml` claimed
     the tool would "say so rather than failing when there is none". That was
     fixed and nothing held it.

  7. A ROUTING ROW THAT SENDS A SENTENCE TO A CAPABILITY NO FRAGMENT DECLARES
     FAILS THE RUN. Row 5b-388: `"rename the heading" -> SET_SCHEDULE_FIELD_FORMAT`
     (row 5b-232) and `"which materials are unused" -> PURGE_UNUSED_MATERIALS`
     both sat in the library from 2026-09-06 while this checker ran on every
     pull request, because it read only the rows that say `-> here`. The target
     is read wherever the row puts it - after a sentence that is still wrapping,
     on the comment line below a trailing arrow, after an arrow on a line of its
     own - and `here`, `NOT`, `ALL` and prose are not targets.

  Sections 5, 6 and 8 to 10 hold the store guard, store_for_this_tree(): a stale row is
  named, and the SHARED store is never rebuilt except from the main checkout
  on branch main - section 8, added 2026-10-09, is the main folder with a
  feature branch checked out, section 9 that the guard and the lookup ask
  one rule, heron_scope.rebuild_refusal() (row 5b-233), and section 10 that
  rule's two other refusals: no knowledge folder at all, and a worktree
  whose main checkout cannot be found.

  11. A ROUTING-TABLE CLAIM THAT WRAPS ONTO A SECOND COMMENT LINE IS READ AS ONE
      SENTENCE. Row 5b-382: the claim was captured WITH the newline and the next
      line's `#    `, so "which walls are fair faced block" - a declared
      utterance - was searched with a `#` in it and listed as claimed and not
      reached, and could never be seen to collide with the same sentence on
      another table.

  12. A CLAIM WHOSE `-> here` SITS ON THE NEXT COMMENT LINE IS STILL READ. Row
      5b-383: after the closing quote the arrow could not step over
      the next line's `#`, so nine claims in the library were never checked for
      reach or for a second table claiming them, and nothing said so.

  13. THREE MORE LAYOUTS ARE READ - AND TWO LOOK-ALIKES ARE NOT. Row 5b-384:
      an arrow on the line BEFORE the one that closes the quote, several quoted
      sentences sharing one arrow (`"a" / "b"  -> here`), and `-> HERE` in
      capitals. Text between the closing quote and the arrow is still not a
      claim, because that is also how a measurement log reads (`"x"  REPORT ->
      here`).

  14. THE FIVE ROWS NO RULE COULD READ ARE NOW WRITTEN SO THE READER READS
      THEM. Row 5b-392: three sentences in a dated paragraph, a bracket between
      a quote and its arrow, two sentences stacked under another row's arrow, a
      row naming its own capability, and `-> declared HERE`. Each was rewritten
      in its fragment.yaml's comments - never an impl/ - rather than taught to
      the reader, because each layout is also how prose or a log reads.

WHAT IT DOES NOT PROVE
  Anything about the routing result itself. That part of the checker is a
  REPORT - it exits 0 whatever collisions it finds, because a collision is a
  judgement and not a defect - so there is no verdict here to test. What is
  testable is the two cases where it declines to produce one at all, and
  section 7, the one part that is a verdict: a name either is a capability some
  fragment declares or it is not.
  Sections 11 to 14 test what it READS as a routing-table claim before it
  asks anything (rows 5b-382, 5b-383, 5b-384 and 5b-392).
"""

from __future__ import print_function

import contextlib
import importlib.util
import io
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

spec = importlib.util.spec_from_file_location(
    "heron_check_routing", os.path.join(ROOT, "tools", "check-routing.py"))
CR = importlib.util.module_from_spec(spec)
spec.loader.exec_module(CR)

FAILURES = []


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILURES.append(said)


class _Captured(object):
    """stderr, held so a refusal's WORDS can be checked and not only its code."""

    def __init__(self):
        self.said = io.StringIO()
        self.was = None

    def __enter__(self):
        self.was = sys.stderr
        sys.stderr = self.said
        return self

    def __exit__(self, *_):
        sys.stderr = self.was
        return False

    def text(self):
        return self.said.getvalue()


# What the routing run's first step raises in sections 7-8, so a run the
# store guard lets through stops there instead of asking every utterance.
MEASURED = "went on to route"


class _Measured(Exception):
    """The store guard let the run through to the routing."""


def _rows(home):
    """How many rows the GLOBAL store in HOME holds, read through a private
    HERON_KNOWLEDGE so nothing else is opened."""
    import heron_scope as SCOPE
    was = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            return store.count()
        finally:
            store.close()
    finally:
        if was is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = was


def _home_beside(env):
    """D-135: a planted %APPDATA% stood for the folder the shared store is in.
    That folder is HERON_DATA's since, or the Windows profile's - so HERON_DATA
    is planted beside it, and an %APPDATA% taken away takes the profile too,
    or on Windows the owner's real profile would answer for it."""
    if "APPDATA" not in env or "HERON_DATA" in env:
        return env
    if env["APPDATA"]:
        return dict(env, HERON_DATA=os.path.join(env["APPDATA"], "Heron"))
    return dict(env, HERON_DATA=None, USERPROFILE=None)


class _planted_env(object):
    """ENV in os.environ for the length of a `with` - a variable mapped to
    None is unset - and everything put back as it was afterwards."""

    def __init__(self, env):
        self.env = _home_beside(env)
        self.kept = {}

    def __enter__(self):
        self.kept = dict((k, os.environ.get(k)) for k in self.env)
        for key, value in self.env.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value
        return self

    def __exit__(self, *_):
        for key, value in self.kept.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value
        return False


def _asked(fn):
    """What FN() answers - or "raised ...", never the raise itself, so a
    check that errors is a FAIL line and not the end of the suite."""
    try:
        return fn()
    except BaseException as raised:              # noqa: BLE001 - that IS the check
        return "raised %s: %s" % (type(raised).__name__, raised)


# "Not planted", for an argument whose planted answer can itself be None.
_UNSET = object()


def _guarded_run(env, source=_UNSET, on_main=None, refusal=None):
    """(exit code, what it said) from CR.main([]) in the environment ENV.

    The re-index, the routing run's first step, raises - so the code is
    MEASURED when the store guard let the run through. Planted where given:
    SOURCE is what heron_scope.refreshes_from() answers - None included, a
    worktree whose main checkout cannot be found - ON_MAIN what
    heron_scope.on_main_branch() answers, REFUSAL what
    heron_scope.rebuild_refusal() answers. ENV maps a variable to its value,
    or to None to unset it. A raise of any other kind is recorded as the
    code, never let out (heron-ship s2a).
    """
    import contextlib
    import heron_scope as SCOPE
    import heron_search as SEARCH

    def stop(*_args, **_kwargs):
        raise _Measured()

    planted = {"refreshes_from": ((lambda root=None: source)
                                  if source is not _UNSET else None),
               "on_main_branch": ((lambda root=None: on_main)
                                  if on_main is not None else None),
               "rebuild_refusal": ((lambda root=None: refusal[0])
                                   if refusal is not None else None)}
    kept = dict((name, getattr(SCOPE, name, None)) for name in planted)
    env = _home_beside(env)
    kept_env = dict((k, os.environ.get(k)) for k in env)
    was_index = SEARCH.index
    SEARCH.index = stop
    for name, fn in planted.items():
        if fn is not None:
            setattr(SCOPE, name, fn)
    for key, value in env.items():
        if value is None:
            os.environ.pop(key, None)
        else:
            os.environ[key] = value
    said = io.StringIO()
    try:
        with contextlib.redirect_stdout(said), contextlib.redirect_stderr(said):
            code = CR.main([])
    except _Measured:
        code = MEASURED
    except BaseException as raised:              # noqa: BLE001 - that IS the check
        code = "raised %s: %s" % (type(raised).__name__, raised)
    finally:
        SEARCH.index = was_index
        for name, fn in planted.items():
            if fn is None:
                continue
            if kept[name] is None:
                delattr(SCOPE, name)
            else:
                setattr(SCOPE, name, kept[name])
        for key, value in kept_env.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value
    return code, said.getvalue()


def main():
    print("1. --revit WITH NO VALUE IS REFUSED, NOT A TRACEBACK")
    # RAISING IS THE FAILURE, so it is caught and recorded as one rather than
    # ending the suite. A check that errors has proved nothing
    # (.claude/skills/heron-ship/SKILL.md s2a).
    for argv, what in (
            (["--revit"], "--revit last on the line"),
            (["--revit", "--rebuild"], "a flag standing where a release should be")):
        code = None
        try:
            with _Captured() as out:
                code = CR.main(list(argv))
        except BaseException as raised:          # noqa: BLE001 - that IS the check
            check(False, "%s is refused rather than raising %s"
                         % (what, type(raised).__name__))
            continue
        check(code == 2,
              "%s exits 2 - the code this repository uses for 'the tool could "
              "not do its job', so nothing reads as a routing result" % what)
        check("--revit" in out.text(),
              "and the refusal NAMES the flag and the shape of a value, "
              "rather than printing a stack")
    print()

    print("2. IT REFUSES BEFORE IT OPENS ANYTHING")
    # The guard has to sit above the imports, or a typo still pays for
    # heron_scope, heron_search, heron_embed and heron_retrieve first.
    source = io.open(os.path.join(ROOT, "tools", "check-routing.py"),
                     encoding="utf-8").read()
    body = source.split("\ndef main(", 1)[1]
    guard = body.find("--revit needs a release")
    imports = body.find("import heron_scope")
    check(guard != -1 and imports != -1 and guard < imports,
          "the flag refusal comes before the brain imports, so a typo costs "
          "nothing")
    print()

    print("3. NO KNOWLEDGE STORE IS EXIT 2 AND A SENTENCE, NOT A TRACEBACK")
    import heron_scope as SCOPE
    was = os.environ.pop("HERON_KNOWLEDGE", None)
    try:
        if SCOPE.knowledge_dir() is not None:
            # A MACHINE WITH %APPDATA% HAS SOMEWHERE TO KEEP KNOWLEDGE, so
            # this case cannot be arranged here and is NOT reported as a pass.
            # Row 5b-71 is about the Linux runner, which is where it bit.
            print("  NOT RUN  this machine has a knowledge folder without "
                  "HERON_KNOWLEDGE (%APPDATA%), so the refusal cannot be "
                  "arranged - it is not a pass")
        else:
            code = None
            try:
                with _Captured() as out:
                    code = CR.main([])
            except BaseException as raised:      # noqa: BLE001 - that IS the check
                check(False, "no knowledge store is refused rather than "
                             "raising %s from four frames down"
                             % type(raised).__name__)
            if code is not None:
                check(code == 2,
                      "no knowledge store exits 2 - NOT 0, because nothing "
                      "was checked and a green run would be a lie, and NOT 1, "
                      "because nothing failed either")
                check("COULD NOT RUN" in out.text()
                      and "HERON_KNOWLEDGE" in out.text(),
                      "and it says so in words, naming the variable that "
                      "fixes it - gates.yml has claimed this since the day "
                      "the tool was wired in")
    finally:
        if was is not None:
            os.environ["HERON_KNOWLEDGE"] = was
    print()

    print("4. THE RISK LADDER IS THE REGISTER'S, AND AN UNKNOWN LEVEL IS -1")
    check(CR.rung("READ") < CR.rung("MODIFY") < CR.rung("ADMIN"),
          "READ sits below MODIFY sits below ADMIN - the crossing this tool "
          "separates from an ordinary collision is a question answered by a "
          "write, and that only means anything if the order is right")
    check(CR.rung("nonsense") == -1,
          "and a level nobody declared is -1 rather than an exception or a "
          "quiet zero, so it can never out-rank READ")
    print()

    print("5. A CARD EDITED SINCE THE STORE WAS BUILT IS A STALE STORE, NOT ONLY A NEW ID")
    # Row 5b-229: the store was rebuilt only when the SET OF IDS differed, so a
    # card whose identity, capability or domain was edited was searched under
    # its old text - the search reads those from the store's row.
    home = tempfile.mkdtemp(prefix="heron-cr-")
    was = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        import heron_scope as SCOPE
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        stale = getattr(CR, "stale_rows", None)
        check(stale is not None, "check-routing can say which rows differ from their cards")
        if stale is not None:
            check(stale(store) == [], "a store just rebuilt from this tree has no stale row")
            some = store.fragments()[0]["id"]
            store.db.execute("UPDATE fragments SET semantic_identity = ? WHERE id = ?",
                             ("an identity the card no longer says", some))
            store.db.commit()
            check(stale(store) == [some],
                  "a row whose identity is not its card's is named (%s)" % stale(store))
        store.close()
    finally:
        if was is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = was
        shutil.rmtree(home, ignore_errors=True)
    print()

    print("6. THE SHARED STORE IS NOT REBUILT FROM A CHECKOUT THAT IS NOT MAIN")
    # Row 5b-233: one store serves every checkout and chat on the PC, and a run
    # in a worktree found it stale and rebuilt it from that branch - from then
    # on every session's heron_lookup named a capability main does not hold.
    # Shown here by telling heron_scope the store is the shared one, asked from
    # a worktree, and handing the tool an empty store that wants a rebuild.
    home = tempfile.mkdtemp(prefix="heron-cr-shared-")
    was = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = home
    import heron_scope as SCOPE
    real = SCOPE.refreshes_from
    SCOPE.refreshes_from = lambda root=None: (os.path.join("main", "brain", "fragments"), "main")
    try:
        with _Captured() as out:
            code = CR.main([])
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        held = store.count()
        store.close()
        check(code == 2, "a run that would rebuild the shared store from here exits 2 (got %s)" % code)
        check(held == 0, "and the store is left as it was - nothing rebuilt into it (%d rows)" % held)
        check("HERON_KNOWLEDGE" in out.text(), "and it says to point HERON_KNOWLEDGE at a scratch folder")
    finally:
        SCOPE.refreshes_from = real
        if was is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = was
        shutil.rmtree(home, ignore_errors=True)
    print()

    print("7. A ROUTING ROW'S `-> NAME` IS A CAPABILITY SOME FRAGMENT DECLARES")
    # Row 5b-388. ASKED BEFORE IT IS CALLED, so the checker as it stood -
    # which read no target at all - fails these checks rather than raising
    # (.claude/skills/heron-ship/SKILL.md s2a). The stand-ins find nothing and
    # rule nothing, which is exactly what the checker did.
    targets_of = getattr(CR, "routing_targets", None)
    dangling_in = getattr(CR, "dangling_targets", None)
    verdict_of = getattr(CR, "targets_verdict", None)
    known = getattr(CR, "KNOWN_DANGLING", None)
    check(None not in (targets_of, dangling_in, verdict_of, known),
          "the checker reads a row's target, lists the ones no fragment "
          "declares, keeps a list of the known ones, and rules on them")
    if targets_of is None:
        targets_of = lambda text: []                           # noqa: E731
    if dangling_in is None:
        dangling_in = lambda folder=None: (0, [])              # noqa: E731
    if verdict_of is None:
        verdict_of = lambda found, known: 0                    # noqa: E731
    if known is None:
        known = {}

    def ruled(found, listed):
        """The verdict, and what it printed - a ruling nobody can read is
        half a ruling."""
        said = io.StringIO()
        try:
            with contextlib.redirect_stdout(said):
                code = verdict_of(found, listed)
        except BaseException as raised:          # noqa: BLE001 - that IS the check
            return "raised %s" % type(raised).__name__, said.getvalue()
        return code, said.getvalue()

    # ROW 5b-232'S OWN TABLE, as add-schedule-combined-field carried it on
    # 2026-10-09. Copied rather than read from the fragment, so the day that
    # row is repaired this still proves the checker sees its shape: a target
    # after a wrapped sentence that has not closed yet, and a target whose
    # explanation wraps onto the lines below it.
    row_232 = (
        '# ROUTING - the same rows are in the fragments named here.\n'
        '#\n'
        '#   "one column with the type and the    -> here\n'
        '#    mark together"\n'
        '#   "join these two columns"             -> here\n'
        '#   "a column that calculates something" -> NOT here. A combined field joins\n'
        '#                                           text; arithmetic is a calculated\n'
        '#                                           value, a different mechanism with\n'
        '#                                           no public API across this whole\n'
        '#                                           release range\n'
        '#   "add a column"                       -> ADD_SCHEDULE_FIELDS, for a plain\n'
        '#                                           parameter\n'
        '#   "rename the heading"                 -> SET_SCHEDULE_FIELD_FORMAT\n'
        '#   "what fields does this schedule      -> REPORT_SCHEDULE_DEFINITION, which\n'
        '#    have"                                  is where the exact names come from\n')
    check([n for _line, n in targets_of(row_232)]
          == ["ADD_SCHEDULE_FIELDS", "SET_SCHEDULE_FIELD_FORMAT",
              "REPORT_SCHEDULE_DEFINITION"],
          "row 5b-232's table gives three targets - the one after a sentence "
          "still wrapping included, and `here` and `NOT here` not - got %r"
          % (targets_of(row_232),))
    check((13, "SET_SCHEDULE_FIELD_FORMAT") in targets_of(row_232),
          "and the dangling one is named at the line it sits on, so the "
          "failure points at it")

    shapes = (
        '#   "on one line"                -> SET_ONE_LINE (MODIFY)\n'
        '#   "a target on the line below" ->\n'
        '#                                   SET_NEXT_LINE, which\n'
        '#   "an arrow on a line of its own"\n'
        '#   -> SET_ARROW_BELOW\n'
        '#   "in backticks"               -> `SET_IN_TICKS`\n'
        '  #   "an indented comment"      -> SET_INDENTED\n'
        '#   "mine"                       -> here\n'
        '#   "not mine"                   -> NOT here, see SET_IN_PROSE\n'
        '#   "every one"                  -> ALL of them, HERE or NOTHING\n'
        '#   Rename Scheme=Rentable       -> Net Lettable\n'
        '#   "a Revit name"               -> OST_DuctCurves\n'
        '#   "an arrow, then YAML"        ->\n'
        'purpose: a value, not a comment -> SET_IN_YAML\n'
        '#    continuation prose naming SET_IN_CONTINUATION\n')
    check(targets_of(shapes)
          == [(1, "SET_ONE_LINE"), (3, "SET_NEXT_LINE"), (5, "SET_ARROW_BELOW"),
              (6, "SET_IN_TICKS"), (7, "SET_INDENTED")],
          "every place a row puts its target is read, and nothing else is: "
          "not `here`, `NOT`, `ALL`, `HERE` or `NOTHING`, not a mixed-case "
          "name, not a name in the prose after a target, not an arrow in a "
          "YAML value - got %r" % (targets_of(shapes),))

    # A LIBRARY OF TWO, so the comparison is seen against a declared set.
    lib = tempfile.mkdtemp(prefix="heron-routing-targets-")
    try:
        for folder, text in (
                ("set-real", 'capability: SET_REAL\n'
                             '# ROUTING\n'
                             '#   "this one"     -> here\n'
                             '#   "that one"     -> READ_OTHER\n'
                             '#   "a ghost"      -> SET_GHOST\n'),
                ("read-other", 'capability: READ_OTHER\n'
                               '#   "the writer" -> SET_REAL\n')):
            os.makedirs(os.path.join(lib, folder))
            with io.open(os.path.join(lib, folder, "fragment.yaml"), "w",
                         encoding="utf-8") as fh:
                fh.write(text)
        read, found = dangling_in(lib)
        check(read == 3 and found == [("set-real", 5, "SET_GHOST")],
              "of three targets read, the one no fragment declares is the one "
              "listed, by fragment and line - got %r of %r" % (found, read))

        code, said = ruled(found, {})
        check(code == 1 and "set-real/fragment.yaml:5" in said
              and "SET_GHOST" in said,
              "a target no fragment declares FAILS the run, naming the file, "
              "the line and the name - got %r" % (code,))
        code, said = ruled(found, {("set-real", "SET_GHOST"): "row 5b-0"})
        check(code == 0 and "row 5b-0" in said,
              "the same target on the known list is printed with its reason "
              "and does not fail - got %r" % (code,))
        code, said = ruled([], {("set-real", "SET_GHOST"): "row 5b-0"})
        check(code == 1 and "SET_GHOST" in said,
              "a known entry whose row was repaired FAILS until it comes off "
              "the list, or the list goes on excusing that row if it comes "
              "back - got %r" % (code,))
        code, _said = ruled([], {})
        check(code == 0, "nothing dangling and nothing listed passes - got %r"
              % (code,))

        # THROUGH main(), the way CI runs it, and with no store: --targets is
        # the verdict alone. NOT CALLED on a checker that has no such verdict:
        # that one ignores the flag and runs the whole report, which rebuilds
        # whichever knowledge store it finds from this branch (row 5b-233).
        if getattr(CR, "dangling_targets", None) is None:
            code = "not called - the checker has no --targets verdict"
        else:
            was = CR.FRAGMENTS, CR.KNOWN_DANGLING
            CR.FRAGMENTS, CR.KNOWN_DANGLING = lib, {}
            try:
                with contextlib.redirect_stdout(io.StringIO()):
                    code = CR.main(["--targets"])
            except BaseException as raised:      # noqa: BLE001 - that IS the check
                code = "raised %s" % type(raised).__name__
            finally:
                CR.FRAGMENTS, CR.KNOWN_DANGLING = was
        check(code == 1,
              "and main() fails on it, with no knowledge store asked for - "
              "got %r" % (code,))
    finally:
        shutil.rmtree(lib, ignore_errors=True)

    # THE LIBRARY AS IT STANDS.
    read, found = dangling_in()
    code, said = ruled(found, known)
    check(read > 0 and code == 0,
          "every target in this library is a declared capability or on the "
          "known list with its reason, and every known entry still dangles - "
          "got %r over %r targets%s"
          % (code, read, "" if code == 0 else ":\n" + said))
    check(all(reason.strip() for reason in known.values()),
          "every known entry says why it is waiting")
    check("dangling_targets(" in body,
          "and main() reads targets through the function the checks above "
          "call, rather than a pattern of its own they never see")
    print()

    print("8. NOR FROM THE MAIN FOLDER WITH A FEATURE BRANCH CHECKED OUT")
    # Row 5b-233's last hole in this guard. refreshes_from() names the main
    # checkout there whatever its branch, and section 6's test asked only
    # that - so a branch checked out in the main folder rebuilt the shared
    # store from its own cards. The lookup never allowed it (heron_brain only
    # warms or advises a rebuild of the shared store from the main checkout
    # ON main). Planted: the store is the shared one by its own spelling -
    # %APPDATA%\Heron\knowledge, HERON_KNOWLEDGE unset - refreshes_from()
    # answers as it does in the main checkout, and the branch is planted,
    # since a test cannot check a branch out in the main folder.
    import heron_fragment as FRAG
    here = (FRAG.FRAGMENTS_DIR, None)
    for on_main in (False, True):
        top = tempfile.mkdtemp(prefix="heron-cr-mainfolder-")
        shared = os.path.join(top, "appdata", "Heron", "knowledge")
        os.makedirs(shared)
        try:
            code, said = _guarded_run(
                {"HERON_KNOWLEDGE": None,
                 "APPDATA": os.path.join(top, "appdata")},
                source=here, on_main=on_main)
            held = _rows(shared)
            if not on_main:
                check(code == 2, "the shared store, asked from the main folder "
                                 "on a feature branch, exits 2 (got %r)" % (code,))
                check(held == 0, "and nothing is rebuilt into it (%d rows)" % held)
                check("HERON_KNOWLEDGE" in said
                      and "a branch that is not main" in " ".join(said.split()),
                      "and it says the branch is why, and to point "
                      "HERON_KNOWLEDGE at a scratch folder")
            else:
                check(code == MEASURED and held > 0,
                      "the main folder ON main still rebuilds the shared store - "
                      "it is where that store comes from (got %r, %d rows)"
                      % (code, held))
        finally:
            shutil.rmtree(top, ignore_errors=True)
    # THE SHARED FOLDER UNDER ANOTHER SPELLING is still the shared folder. A
    # HERON_KNOWLEDGE that names it by another path - a link, a junction, or
    # here a `.` in the middle, which needs no rights to make - must not pass
    # for a private store. Found by review of this change, 2026-10-09, when
    # comparing the folders by spelling left every suite green.
    top = tempfile.mkdtemp(prefix="heron-cr-spelling-")
    appdata = os.path.join(top, "appdata")
    shared = os.path.join(appdata, "Heron", "knowledge")
    os.makedirs(shared)
    try:
        code, said = _guarded_run(
            {"HERON_KNOWLEDGE": os.path.join(appdata, ".", "Heron", "knowledge"),
             "APPDATA": appdata},
            source=here, on_main=False)
        held = _rows(shared)
        check(code == 2 and held == 0,
              "the shared folder named by another path is still the shared one: "
              "refused from the main folder on a feature branch (got %r, %d rows)"
              % (code, held))
    finally:
        shutil.rmtree(top, ignore_errors=True)
    # AND A STORE WHOSE IDS MATCH BUT WHOSE ROWS ARE STALE is refused the same
    # way - the rule is asked before ANY rebuild, not only when an id is new.
    # Every other case here plants an empty store, so the ids always differ;
    # found by review of this change, 2026-10-09.
    import heron_scope as SCOPE
    top = tempfile.mkdtemp(prefix="heron-cr-stale-shared-")
    appdata = os.path.join(top, "appdata")
    shared = os.path.join(appdata, "Heron", "knowledge")
    os.makedirs(shared)
    try:
        with _planted_env({"HERON_KNOWLEDGE": shared}):
            SCOPE.rebuild()
            store = SCOPE.open_scope(SCOPE.GLOBAL)
            edited = store.fragments()[0]["id"]
            store.db.execute("UPDATE fragments SET semantic_identity = ? WHERE id = ?",
                             ("an identity the card no longer says", edited))
            store.db.commit()
            store.close()
        code, said = _guarded_run({"HERON_KNOWLEDGE": None, "APPDATA": appdata},
                                  source=here, on_main=False)
        with _planted_env({"HERON_KNOWLEDGE": shared}):
            store = SCOPE.open_scope(SCOPE.GLOBAL)
            still = [row["semantic_identity"] for row in store.fragments() if row["id"] == edited]
            store.close()
        check(code == 2 and still == ["an identity the card no longer says"],
              "a shared store holding this tree's ids with one row stale is refused "
              "from the main folder on a feature branch, and the row left as it was "
              "(got %r, %r)" % (code, still))
    finally:
        shutil.rmtree(top, ignore_errors=True)
    # CI: gates.yml runs this on a fresh HERON_KNOWLEDGE, on a checkout whose
    # HEAD is not main. A private store is rebuilt on any branch.
    home = tempfile.mkdtemp(prefix="heron-cr-private-")
    try:
        code, said = _guarded_run({"HERON_KNOWLEDGE": home}, on_main=False)
        held = _rows(home)
        check(code == MEASURED and held > 0,
              "CI's private store, on a branch that is not main, is still "
              "rebuilt and routed over (got %r, %d rows)" % (code, held))
    finally:
        shutil.rmtree(home, ignore_errors=True)
    print()

    print("9. ONE RULE, ASKED FROM BOTH SIDES - heron_scope.rebuild_refusal()")
    # The lookup's warm-up and its rebuild advice, and this guard, decided the
    # same question with two different rules - which is how the branch case
    # above was let through here and refused there. Planted both ways, and
    # each side must follow the plant.
    import heron_scope as SCOPE
    check(callable(getattr(SCOPE, "rebuild_refusal", None)),
          "heron_scope has the one rule, rebuild_refusal()")
    home = tempfile.mkdtemp(prefix="heron-cr-rule-")
    try:
        code, said = _guarded_run({"HERON_KNOWLEDGE": home},
                                  refusal=("a reason planted by the suite",))
        held = _rows(home)
        check(code == 2 and held == 0,
              "the guard refuses whenever the rule does, even on a private "
              "store (got %r, %d rows)" % (code, held))
        check("a reason planted by the suite" in said,
              "and it prints the rule's own reason")
    finally:
        shutil.rmtree(home, ignore_errors=True)
    sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
    try:
        import heron_brain as BRAIN
    except BaseException as raised:              # noqa: BLE001 - that IS the check
        BRAIN = None
        check(False, "heron_brain imports (%s)" % type(raised).__name__)
    if BRAIN is not None:
        real = getattr(SCOPE, "rebuild_refusal", None)
        answers = []
        try:
            for planted in ("a reason planted by the suite", None):
                SCOPE.rebuild_refusal = lambda root=None, said=planted: said
                answers.append(BRAIN._store_warm_allowed())
        finally:
            if real is None:
                del SCOPE.rebuild_refusal
            else:
                SCOPE.rebuild_refusal = real
        check(answers == [False, True],
              "and the lookup's _store_warm_allowed() follows the same rule: "
              "refused, then allowed (got %r)" % (answers,))
    print()

    print("10. THE TWO REFUSALS NO OTHER SECTION ASKS - NO KNOWLEDGE FOLDER, "
          "AND NO MAIN CHECKOUT FOUND")
    # Both moved into rebuild_refusal() from rules that held them with no
    # check behind them - found by review of row 5b-233's change, 2026-10-09,
    # when either answer turned to "allowed" left every suite green. The
    # lookup refused to warm a store with no knowledge folder at all; this
    # guard refused the SHARED store from a worktree whose main checkout
    # cannot be found, which is refreshes_from() answering None - row
    # 5b-233's own rebuild, from a moved or broken worktree.
    import heron_scope as SCOPE
    rule = getattr(SCOPE, "rebuild_refusal", None)
    with _planted_env({"HERON_KNOWLEDGE": None, "APPDATA": None}):
        nowhere = _asked(rule) if rule is not None else None
        warm = _asked(BRAIN._store_warm_allowed) if BRAIN is not None else None
    check(nowhere is not None and not str(nowhere).startswith("raised "),
          "with no knowledge folder at all - no %%APPDATA%%, no "
          "HERON_KNOWLEDGE - the rule refuses (got %r)" % (nowhere,))
    check(warm is False,
          "and the lookup's _store_warm_allowed() does not warm a store it "
          "has nowhere to keep (got %r)" % (warm,))
    top = tempfile.mkdtemp(prefix="heron-cr-nomain-")
    appdata = os.path.join(top, "appdata")
    shared = os.path.join(appdata, "Heron", "knowledge")
    os.makedirs(shared)
    try:
        code, said = _guarded_run({"HERON_KNOWLEDGE": None, "APPDATA": appdata},
                                  source=None)
        held = _rows(shared)
        check(code == 2,
              "the shared store, asked from a worktree whose main checkout "
              "cannot be found, exits 2 (got %r)" % (code,))
        check(held == 0, "and nothing is rebuilt into it (%d rows)" % held)
        check("HERON_KNOWLEDGE" in said
              and "cannot be found" in " ".join(said.split()),
              "and it says why, and to point HERON_KNOWLEDGE at a scratch "
              "folder")
        if BRAIN is not None:
            real = SCOPE.refreshes_from
            SCOPE.refreshes_from = lambda root=None: None
            try:
                with _planted_env({"HERON_KNOWLEDGE": None, "APPDATA": appdata}):
                    warm = _asked(BRAIN._store_warm_allowed)
            finally:
                SCOPE.refreshes_from = real
            check(warm is False,
                  "and the lookup's _store_warm_allowed() refuses it too "
                  "(got %r)" % (warm,))
    finally:
        shutil.rmtree(top, ignore_errors=True)
    print("11. A CLAIM THAT WRAPS ONTO A SECOND COMMENT LINE IS ONE SENTENCE")
    # Row 5b-382. ASKED BEFORE IT IS CALLED, so the checker as it stood -
    # with the claim regex inline in main() - fails these checks rather than
    # raising (.claude/skills/heron-ship/SKILL.md s2a). The fallback is what
    # main() did inline until that row, so the red run shows the real defect.
    claims_of = getattr(CR, "routing_claims", None)
    check(claims_of is not None,
          "the checker reads a table's claims in one place a suite can call")
    if claims_of is None:
        claims_of = lambda text: [                               # noqa: E731
            m.group(1).strip() for m in re.finditer(
                r'^#\s+"([^"]+)"\s*->\s*here\b', text, re.M)]

    table = (
        '# ROUTING\n'
        '#   "select all ducts"                    -> here\n'
        '#   "which walls are fair faced\n'
        '#    block"                               -> here\n'
        '#   "make the vertical grid fixed distance\n'
        '#      1500 on every\n'
        '#    wall"                                -> here\n'
        '#   "delete these"                        -> DELETE_ELEMENTS\n'
        '#   "half a sentence\n'
        'not a comment"                            -> here\n'
        'id: FRG-TEST-001\n')
    said = claims_of(table)
    check(said == ["select all ducts",
                   "which walls are fair faced block",
                   "make the vertical grid fixed distance 1500 on every wall"],
          "a claim on one line, on two and on three come back as three "
          "sentences, the `#` and the indentation of each continuation gone - "
          "got %r" % (said,))
    check(not any("\n" in s or "#" in s for s in said),
          "no claim carries a newline or a `#` into the identity check or the "
          "search")
    check(claims_of('#   "which walls are fair faced block"  -> here\n')
          == claims_of('#   "which walls are fair faced\n'
                       '#    block"                         -> here\n'),
          "the same sentence wrapped and unwrapped is ONE key, so two tables "
          "claiming it can be seen to collide")
    check(not any(s.startswith("half a sentence") for s in said),
          "a quote that runs off the comment into YAML is not a claim - a "
          "sentence wraps onto COMMENT lines only")

    # THE TWO ROWS THE DEFECT WAS FOUND ON, read from the library itself.
    import yaml
    for folder, sentence, declared in (
            ("select-by-material", "which walls are fair faced block", True),
            ("set-curtain-wall-grid",
             "make the vertical grid fixed distance 1500", False)):
        path = os.path.join(ROOT, "brain", "fragments", folder, "fragment.yaml")
        text = io.open(path, encoding="utf-8").read()
        check(sentence in claims_of(text),
              "%s's table claims %r as one sentence" % (folder, sentence))
        if declared:
            spoken = [(u or "").strip().lower()
                      for u in (yaml.safe_load(text).get("utterances") or [])]
            check(sentence in spoken,
                  "and it is a declared utterance, so the checker answers it by "
                  "identity and never searches it")

    wrapped = []
    for folder in sorted(os.listdir(os.path.join(ROOT, "brain", "fragments"))):
        path = os.path.join(ROOT, "brain", "fragments", folder, "fragment.yaml")
        if os.path.exists(path):
            wrapped += [(folder, s) for s in
                        claims_of(io.open(path, encoding="utf-8").read())
                        if "\n" in s]
    check(not wrapped,
          "no claim anywhere in the library reaches the checker with a line "
          "break in it - %d did: %s"
          % (len(wrapped), ", ".join(sorted(set(f for f, _ in wrapped)))))

    check("routing_claims(" in body,
          "and main() reads claims through it, rather than a regex of its own "
          "that the checks above never see")
    print()

    print("12. A CLAIM WHOSE `-> here` SITS ON THE NEXT COMMENT LINE IS STILL READ")
    # Row 5b-383. Same reader as section 11, so a checker without it has
    # already failed there and the fallback above keeps this section running.
    table = (
        '#   "will this family connect when it is placed on a pipe"\n'
        '#                                    -> here, in the Family Editor (v2)\n'
        '#   "the duct is missing\n'
        '#    from my schedule"\n'
        '#                                    -> here FIRST to see what is there\n'
        '#   "fix the names this found"\n'
        '#                                    -> RENAME_ELEMENTS\n'
        '#   "select all ducts"                -> here\n')
    said = claims_of(table)
    check(said == ["will this family connect when it is placed on a pipe",
                   "the duct is missing from my schedule",
                   "select all ducts"],
          "a claim with its arrow on the next line - wrapped or not - is read, "
          "in order, beside one with its arrow on the same line - got %r"
          % (said,))
    check("fix the names this found" not in said,
          "and a sentence whose next-line arrow points ELSEWHERE is still not "
          "a claim for this fragment")

    # THE NINE ROWS THE DEFECT WAS FOUND ON, measured 2026-10-09.
    unread = []
    for folder, sentence in (
            ("check-family-standards",
             "will this family connect when it is placed on a pipe"),
            ("check-model-standards",
             "do the type names follow our naming standard"),
            ("group-by-assembly", "which families are nested inside this one"),
            ("group-elements", "group these forms in this family"),
            ("list-revisions", "show me the sheet issues and revisions table"),
            ("read-schedule-contents", "the duct is missing from my schedule"),
            ("report-mep-pressure-drop", "how much resistance is in this pipe"),
            ("select-types", "what are the mullion types in this model"),
            ("set-schedule-sort-group",
             "one line per size instead of every duct")):
        path = os.path.join(ROOT, "brain", "fragments", folder, "fragment.yaml")
        if sentence not in claims_of(io.open(path, encoding="utf-8").read()):
            unread.append(folder)
    check(not unread,
          "every next-line-arrow claim in the library is read - %d were not: %s"
          % (len(unread), ", ".join(unread)))
    print()

    print("13. THREE MORE LAYOUTS ARE READ - AND TWO LOOK-ALIKES ARE NOT")
    # Row 5b-384. Same reader again, so the fallback in section 11 keeps this
    # running on a checker that has none.
    table = (
        '#   "one column with the type and the    -> here\n'
        '#    mark together"\n'
        '#   "the colours are not showing on      -> here when a filter is on\n'
        '#    this drawing"                          and nothing draws;\n'
        '#   "delete these" / "remove these"  -> here\n'
        '#   "flip the door" / "the handing is\n'
        '#    wrong, fix it"                       -> here. It CHANGES the model\n'
        '#   "set the view template"            -> HERE, and it is declared here\n'
        '#   "wash the walls light grey so it\n'
        '#    prints"                          -> HERE. A colour with no pattern\n'
        '#   "which doors are flipped" / "is this\n'
        '#    door mirrored"                       -> REPORT_MIRRORED_INSTANCES\n'
        '#   "a wrapped sentence going   -> SOMEWHERE_ELSE\n'
        '#    elsewhere"\n'
        '#   "change the global param"    REPORT -> here, by identity\n'
        '#   "delete these forms" (ids given)            -> here, then DELETE\n'
        '#   "-> here" is what a row in one of these tables says\n')
    said = claims_of(table)
    check(said == ["one column with the type and the mark together",
                   "the colours are not showing on this drawing",
                   "delete these", "remove these",
                   "flip the door", "the handing is wrong, fix it",
                   "set the view template",
                   "wash the walls light grey so it prints"],
          "an arrow before the closing quote, sentences sharing one arrow and "
          "`-> HERE` are each read, in order, as whole sentences - got %r"
          % (said,))
    check(not any(s.startswith(("which doors", "is this door", "a wrapped",
                                "change the global", "delete these forms"))
                  for s in said),
          "and rows routed ELSEWHERE, a measurement log with a word between "
          "the quote and the arrow, and a parenthesis there are not claims")
    check(not any(s.startswith("-> here") or "is what a row" in s
                  for s in said),
          "and prose that quotes the notation itself is not a claim")

    # LOOK-ALIKES, each found by a reviewer set to break the reader on
    # 2026-10-09. None is in the library; each was read by the first version
    # of the 5b-384 reader, or by the 5b-382 pattern it grew from.
    wrong = []
    for text, want, what in (
            ('#   "update the global parameter value  SET    -> here, kept\n'
             '#    for the whole project"\n', [],
             "a log column between the sentence and an arrow that comes "
             "before the closing quote"),
            ('#   "a wrapped sentence      NOT -> here\n'
             '#    going on"\n', [], "a NOT before that arrow"),
            ('#   "make the leaders     -> here\n'
             '#    -> OTHER for this"\n', [],
             "an arrow inside the continuation"),
            ('#   "one column with the type and   -> here\n'
             '#    mark together, see "x"\n', [],
             "a continuation whose quote opens something else"),
            ('#   "copy the legend          -> here\n'
             '#                                REPORT_LEGENDS reads "the list"\n',
             [], "right-hand-column prose under an unclosed quote"),
            ('#   "make a legend"                     -> CREATE_LEGEND, which\n'
             '#                                          "the legend list   -> here\n'
             '#                                          stands"\n', [],
             "a quote opening another row's right-hand column"),
            ('#   "x"                                 -> OTHER; the pair\n'
             '#                                          "a" / "b" -> here\n', [],
             "a quoted pair in another row's right-hand column"),
            ('utterances:\n#\n  "select all ducts"\n#   -> here\n', [],
             "a quote on a YAML line under a bare `#`"),
            ('#   "x" ->\nhere: 1\n', [], "`here` as the YAML key below"),
            ('#   "x"                -> here-and-there\n', [],
             "`here-and-there`"),
            ('#   "a" / "   "       -> here\n', ["a"], "an all-space quote"),
            ('#   "delete these" /\n'
             '#   "remove these"                  -> here\n',
             ["delete these", "remove these"],
             "a group broken after its `/`"),
            ('#   "delete these"\n'
             '#   / "remove these"                -> here\n',
             ["delete these", "remove these"],
             "a group broken before its `/`"),
            ('#   "x"\r\n#   -> here\r\n', ["x"],
             "CRLF text with the arrow on the next line")):
        got = claims_of(text)
        if got != want:
            wrong.append("%s: got %r" % (what, got))
    check(not wrong,
          "look-alikes read as what they are - a log column, a NOT, a stray "
          "arrow or quote, another row's right-hand column and plain YAML are "
          "not claims; a broken group and CRLF text are read whole - "
          "%d wrong: %s" % (len(wrong), "; ".join(wrong)))

    # ONE OF EACH LAYOUT FROM THE LIBRARY, measured 2026-10-09.
    unread = []
    for folder, sentence in (
            ("add-schedule-combined-field",
             "one column with the type and the mark together"),
            ("audit-view-filters",
             "the colours are not showing on this drawing"),
            ("delete-elements", "remove these"),
            ("flip-elements", "the handing is wrong, fix it"),
            ("show-elements", "reset the view"),
            ("apply-view-template", "set the view template"),
            ("set-category-solid-fill",
             "wash the walls light grey so it prints")):
        path = os.path.join(ROOT, "brain", "fragments", folder, "fragment.yaml")
        if sentence not in claims_of(io.open(path, encoding="utf-8").read()):
            unread.append("%s %r" % (folder, sentence))
    check(not unread,
          "one claim of each layout in the library is read - %d were not: %s"
          % (len(unread), "; ".join(unread)))

    leaked = []
    for folder in sorted(os.listdir(os.path.join(ROOT, "brain", "fragments"))):
        path = os.path.join(ROOT, "brain", "fragments", folder, "fragment.yaml")
        if os.path.exists(path):
            leaked += ["%s %r" % (folder, s) for s in
                       claims_of(io.open(path, encoding="utf-8").read())
                       if "->" in s or '"' in s or "#" in s]
    check(not leaked,
          "no claim anywhere in the library carries an arrow, a quote or a `#` "
          "into the sentence - %d did: %s" % (len(leaked), "; ".join(leaked[:4])))
    print()

    print("14. THE FIVE ROWS NO RULE COULD READ ARE NOW WRITTEN SO IT READS THEM")
    # Row 5b-392. The rows were fixed in the fragments, not in the reader, so
    # this asks the library - and fails, by name, on a table rewritten back.
    unread = []
    for folder, sentence in (
            ("place-hosted-family",
             "place doors and windows hosted in walls at points"),
            ("place-hosted-family", "add windows to the outside walls"),
            ("place-hosted-family", "put doors in the walls at these points"),
            ("select-family-forms", "delete these forms"),
            ("apply-color-fill-scheme", "colour the spaces by zone"),
            ("apply-color-fill-scheme", "colour fill the zones"),
            ("set-sheet-title-block",
             "change the title block on these sheets"),
            ("set-sheet-title-block", "swap the title block"),
            ("list-worksets", "is this model workshared")):
        path = os.path.join(ROOT, "brain", "fragments", folder, "fragment.yaml")
        if sentence not in claims_of(io.open(path, encoding="utf-8").read()):
            unread.append("%s %r" % (folder, sentence))
    check(not unread,
          "every sentence of the five rows is read as a claim - %d were not: %s"
          % (len(unread), "; ".join(unread)))
    print()

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the routing checker refuses a typo and a missing store by")
    print("name, and it refuses before it loads anything. A routing row that")
    print("names a capability no fragment declares fails it, wherever the row")
    print("puts the name.")
    print()
    print("It proves NOTHING about the routing result. That is a report and a")
    print("finding in it is a question for a person, so there is no verdict")
    print("here to test - only the two cases where it declines to give one,")
    print("and the one part that is not a ranking.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
