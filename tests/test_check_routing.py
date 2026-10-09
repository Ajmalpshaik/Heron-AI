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

  5. A ROUTING ROW THAT SENDS A SENTENCE TO A CAPABILITY NO FRAGMENT DECLARES
     FAILS THE RUN. Row 5b-388: `"rename the heading" -> SET_SCHEDULE_FIELD_FORMAT`
     (row 5b-232) and `"which materials are unused" -> PURGE_UNUSED_MATERIALS`
     both sat in the library from 2026-09-06 while this checker ran on every
     pull request, because it read only the rows that say `-> here`. The target
     is read wherever the row puts it - after a sentence that is still wrapping,
     on the comment line below a trailing arrow, after an arrow on a line of its
     own - and `here`, `NOT`, `ALL` and prose are not targets.

WHAT IT DOES NOT PROVE
  Anything about the routing result itself. That part of the checker is a
  REPORT - it exits 0 whatever collisions it finds, because a collision is a
  judgement and not a defect - so there is no verdict here to test. What is
  testable is the two cases where it declines to produce one at all, and
  section 5, the one part that is a verdict: a name either is a capability some
  fragment declares or it is not.
"""

from __future__ import print_function

import contextlib
import importlib.util
import io
import os
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
