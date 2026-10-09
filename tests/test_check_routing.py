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

  5. A ROUTING-TABLE CLAIM THAT WRAPS ONTO A SECOND COMMENT LINE IS READ AS ONE
     SENTENCE. Row 5b-382: the claim was captured WITH the newline and the next
     line's `#    `, so "which walls are fair faced block" - a declared
     utterance - was searched with a `#` in it and listed as claimed and not
     reached, and could never be seen to collide with the same sentence on
     another table.

  6. A CLAIM WHOSE `-> here` SITS ON THE NEXT COMMENT LINE IS STILL READ. Row
     5b-383: after the closing quote the pattern's `\s*->` could not step over
     the next line's `#`, so nine claims in the library were never checked for
     reach or for a second table claiming them, and nothing said so.

  7. THREE MORE LAYOUTS ARE READ - AND TWO LOOK-ALIKES ARE NOT. Row 5b-384:
     an arrow on the line BEFORE the one that closes the quote, several quoted
     sentences sharing one arrow (`"a" / "b"  -> here`), and `-> HERE` in
     capitals. Text between the closing quote and the arrow is still not a
     claim, because that is also how a measurement log reads (`"x"  REPORT ->
     here`).

WHAT IT DOES NOT PROVE
  Anything about the routing result itself. This checker is a REPORT - it exits
  0 whatever collisions it finds, because a collision is a judgement and not a
  defect - so there is no verdict here to test. What is testable is the two
  cases where it declines to produce one at all, and what it READS as a claim
  before it asks anything.
"""

from __future__ import print_function

import importlib.util
import io
import os
import re
import sys

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

    print("5. A CLAIM THAT WRAPS ONTO A SECOND COMMENT LINE IS ONE SENTENCE")
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

    print("6. A CLAIM WHOSE `-> here` SITS ON THE NEXT COMMENT LINE IS STILL READ")
    # Row 5b-383. Same reader as section 5, so a checker without it has
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

    print("7. THREE MORE LAYOUTS ARE READ - AND TWO LOOK-ALIKES ARE NOT")
    # Row 5b-384. Same reader again, so the fallback in section 5 keeps this
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

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the routing checker refuses a typo and a missing store by")
    print("name, and it refuses before it loads anything.")
    print()
    print("It proves NOTHING about the routing result. That is a report and a")
    print("finding in it is a question for a person, so there is no verdict")
    print("here to test - only the two cases where it declines to give one.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
