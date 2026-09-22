# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
How big the fragment library is, typed as a fact in files nobody re-reads.

    python tests/test_library_total.py

`AGENTS.md`'s second Never is *"never type a number a command can derive"*,
and the number it was written about is this one. The library has been 328,
329, 343, 349, 350, 360, 372 and is 395 today. Every one of those was true
on the day it was typed.

WHY THIS SUITE EXISTS
----------------------
On 2026-09-21 fourteen live sentences still named an old total. The worst
was three lines of one report:

    2. The derivation, against all 360 fragments on disk
      ok    395 fragments on disk carry a capability

A heading that names its own sample size, and the derived line under it
disagreeing by 35. Neither `check-docs.py` nor anything else could see it:
check-docs reads markdown, and its fragment pattern is the much narrower
*"N of the M fragments are PROVEN"*, deliberately, so that the dated records
are not flagged.

THE RULE THIS HOLDS
--------------------
A sentence naming the size of the whole library must either **be right
today** or **say when it was measured**. Both are fine and the second is
often better - a measurement is worth keeping with the number it was taken
at. What is never fine is a bare total in the present tense, because there
is no way for a reader to tell which one they are looking at.

So a claim is excused when its SENTENCE carries a date, a history word, or
opens inside a quotation. Three lessons paid for already:

  A DATE, because `heron_merge.py` says "Measured on 2026-09-15 across 360
  fragments" and that is a record, not a mistake.

  THE SENTENCE AND NOT THE LINE OR THE PARAGRAPH. Row 5b-55 tried the
  paragraph: one historical row in a markdown table disabled the check for
  every row beside it. The line is too narrow the other way - a comment
  wraps, and `create-from-room-boundaries` carries its date on the line
  above the count.

  A QUOTATION, because `tests/test_stack_guard.py` quotes HANDOVER saying
  "runs the rewriter over all 372 fragments" in the very paragraph that
  corrects it. Citing a stale figure is how you correct one.

WHAT IT DOES NOT COVER
-----------------------
The dated registers in `RECORDS` below, `docs/work-notes/` and
`docs/handover-archive/` are skipped whole. They are logs: they are SUPPOSED
to hold the number that was true that morning, and flagging them trains the
reader to ignore the checker. `check-docs.py` draws the same line for the
same reason and says so in its own comment.

`docs/fragment-issues-archive/` is skipped whole too. It holds rows moved
VERBATIM out of `docs/FRAGMENT-ISSUES.md`, which is in `RECORDS`, so a dated
total that was a record in the register must not turn into a claim because
its row moved folder. Four lines did exactly that on 2026-09-22, the day the
folder was made - "134 fragments", "the 372 fragments", "all 360 fragments" -
each true on the day its row was written.

A count of PART of the library - "68 DRAFT", "91 carrying a guard", "279
need a typed value" - is a different claim and is not matched here.
`check-docs.py` owns the status and risk ones.

AND A THIRD STATE, WHICH IS THE POINT OF HAVING FOUR
------------------------------------------------------
A stale total inside a `PROVEN` fragment's `impl/` is reported as **wait**,
not as a failure and not as a pass. D-30's fingerprint covers the whole
implementation file, so correcting one word of a comment in there makes
`brain/heron_fragment.py` refuse the fragment - the correction and the
re-proof are one act, and the re-proof needs a real Revit and a named model.
This was measured by doing it: the fix went in, CI rejected it, and the
comment was put back (row 5b-69). A stale count is cheaper than a broken
proof.
"""

from __future__ import annotations

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# WHAT A WHOLE-LIBRARY TOTAL LOOKS LIKE. The determiner is what makes it the
# whole library rather than a slice: "all 360 fragments", "the 372
# fragments", "over 360 fragments". Without one, "68 fragments" is a count of
# something and this suite has no business with it.
WHOLE = re.compile(
    r'\b(?:all|the|every|those|only|its|real)\s+(\d{3})\s+(?:real\s+)?fragments?\b'
    r'|\b(?:over|across)\s+(\d{3})\s+fragments?\b'
    r'|\b(\d{3})\s+fragments?\s+(?:on disk|in the library)\b', re.I)

# A date, or one of the words this repository uses when it is quoting a
# figure it has already corrected. check-docs.py's HISTORY, plus the date.
EXCUSED = re.compile(r'(20\d\d-\d\d-\d\d|used to|it said|until 20\d\d|no longer'
                     r'|superseded|was wrong|had stood|had been|stopped saying'
                     r'|this (?:line|cell|paragraph) said|measured off disk)', re.I)

# A sentence ends at . ! ? or a table-cell wall - the same splitter
# check-docs.py uses, and for the same reason. THE TRAILING CLASS MATTERS:
# this prose ends sentences with `.**`, `."` and `.)`, and a one-character
# lookbehind does not break there, so a sentence runs on into the next and a
# date in the FIRST excuses a stale total in the SECOND. Measured here and
# fixed in check-docs.py too, which had the same splitter. Row 5b-67.
BREAK = re.compile(r'[.!?][*_"\'\u2019)\]]*\s+|\s*\|\s*')

SEARCH = (".py", ".md", ".cs", ".yaml", ".yml", ".toml")

# `worktrees` BY NAME, which is what check-docs.py skips and what
# test_docs_guard.py and test_metadata_guard.py already carry for the same
# reason. `.claude/worktrees/<name>/` is a full second checkout another
# session is working in, at whatever commit it started from, so every
# whole-library total typed in one of those copies is stale BY DESIGN - and
# this suite was reading them as claims about today's library.
#
# Measured 2026-09-22: 140 failures, and ALL 140 were inside a worktree. Not
# one was in this tree. The suite was not finding anything wrong with the
# repository at all - it was reading another session's past.
#
# CI clones once and has no worktrees, so it is green there forever. This can
# only ever go red on a machine running several sessions at once, which is
# the normal way of working here rather than the exception.
SKIP_FOLDERS = ("__pycache__", ".git", "worktrees", "bin", "obj", ".vs",
                "node_modules", "handover-archive", "work-notes",
                "fragment-issues-archive")

# DATED REGISTERS. A row in one of these describes the day it was written -
# docs/FRAGMENT-ISSUES.md says so in its own header - so a total inside one
# is a record and not a claim about today.
#
# AND THIS FILE, WHICH IS AN EXEMPTION AND IS NAMED AS ONE. Its docstring
# quotes the stale report it was written for, in an indented block where the
# quotation rule cannot see it. heron_guard.py answers the same problem
# better - it builds its pattern from PARTS so it cannot fail its own gate -
# and that is not available here, because what has to be quoted is prose.
# An exemption written down is the second-best answer; an undeclared one is
# the failure tools/check-reachable.py warns about.
RECORDS = ("docs/HANDOVER.md", "docs/DECISIONS.md", "docs/FRAGMENT-ISSUES.md",
           "docs/OPEN-QUESTIONS.md", "docs/NEEDS-CHECKING.md",
           "tests/test_library_total.py")

# (path, number) -> why this one is not the library's size. Empty today. Put
# a reason here rather than widening the pattern until it catches nothing -
# the rule tools/check-reachable.py follows.
NOT_THE_LIBRARY = {}

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def real_total():
    """Counted off disk, which is the only place that knows."""
    base = os.path.join(ROOT, "brain", "fragments")
    return len([d for d in os.listdir(base)
                if os.path.isfile(os.path.join(base, d, "fragment.yaml"))])


def sealed_by_a_proof(rel):
    """The fragment this file implements, if its proof seals the bytes.

    A `PROVEN` fragment's proof carries a fingerprint of its implementation
    ([D-30](docs/DECISIONS.md)), and the fingerprint is over the WHOLE file -
    there is no honest way to tell a comment from code by bytes. So changing
    one word of a comment inside `impl/` makes `brain/heron_fragment.py`
    refuse the fragment: "status is PROVEN but the proof does not match this
    implementation."

    That is the fingerprint working. It also means a stale sentence in there
    CANNOT be corrected on a machine with no Revit: the correction and the
    re-proof are one act, and the re-proof needs a model. Measured on
    2026-09-21 by making the correction and watching CI reject it (row
    5b-69). Named here rather than skipped silently, and reported as its own
    state rather than as a pass.
    """
    parts = rel.split("/")
    if len(parts) < 4 or parts[0] != "brain" or parts[1] != "fragments":
        return None
    if "impl" not in parts[2:]:
        return None
    card = os.path.join(ROOT, "brain", "fragments", parts[2], "fragment.yaml")
    try:
        with io.open(card, encoding="utf-8", errors="replace") as fh:
            head = fh.read(2000)
    except IOError:
        return None
    m = re.search(r'^heron-status:\s*(\S+)', head, re.M)
    if m and m.group(1) == "PROVEN":
        return parts[2]
    return None


def sentence_at(text, at):
    """The sentence the match at `at` sits in, and whether it is a citation.

    PARITY IS COUNTED WITHIN THE SENTENCE, and nowhere wider. Counting from
    the start of the file called three dated measurements in `brain/*.py`
    citations, because a module's docstrings and regexes leave the running
    total of `"` odd at an arbitrary point. Counting from the enclosing
    table cell is right for markdown and meaningless for source. The
    sentence is the unit a quotation lives in, and BREAK already splits on
    the cell wall, so a sentence never spans two cells either.
    """
    start = 0
    for m in BREAK.finditer(text, 0, at):
        start = m.end()
    end = len(text)
    m = BREAK.search(text, at)
    if m:
        end = m.start()
    quoted = text.count('"', start, at) % 2 == 1
    return text[start:end], quoted


def claims():
    """Every whole-library total typed anywhere, found rather than listed."""
    found = []
    for where, dirs, files in os.walk(ROOT):
        dirs[:] = [d for d in dirs if d not in SKIP_FOLDERS]
        for name in sorted(files):
            if not name.endswith(SEARCH):
                continue
            path = os.path.join(where, name)
            rel = os.path.relpath(path, ROOT).replace(os.sep, "/")
            if rel in RECORDS:
                continue
            try:
                with io.open(path, encoding="utf-8", errors="replace") as fh:
                    text = fh.read()
            except IOError:
                continue
            for m in WHOLE.finditer(text):
                n = int(m.group(1) or m.group(2) or m.group(3))
                line = text[:m.start()].count("\n") + 1
                sentence, quoted = sentence_at(text, m.start())
                found.append((rel, line, n, sentence.strip(), quoted))
    return sorted(found)


def main():
    total = real_total()
    waiting = []

    print("\nThe library, counted off disk")
    check(total > 100,
          "brain/fragments holds %d fragment.yaml files" % total)

    found = claims()
    print("\n%d whole-library total(s) typed across the repository" % len(found))

    # AN EMPTY SWEEP AGREES WITH EVERYTHING. If the pattern stops matching -
    # somebody rewords every one of them - this suite would pass having
    # compared nothing, which is the failure it exists to prevent elsewhere.
    check(len(found) >= 8,
          "enough to be comparing something - a sweep that finds nothing "
          "passes perfectly and means the opposite")

    print("\nEach one is either right today, or says when it was measured")
    for rel, line, n, sentence, quoted in found:
        why = NOT_THE_LIBRARY.get((rel, n))
        if why:
            print("  --    %s:%d excused: %s" % (rel, line, why))
            continue
        if n == total:
            print("  ok    %s:%d says %d, which is today's count" % (rel, line, n))
            continue
        if quoted:
            print("  --    %s:%d quotes %d - a citation, not a claim"
                  % (rel, line, n))
            continue
        if EXCUSED.search(sentence):
            print("  ok    %s:%d says %d and says when it was measured"
                  % (rel, line, n))
            continue
        sealed = sealed_by_a_proof(rel)
        if sealed:
            # NOT A PASS AND NOT A FAILURE - the third state, named. Editing
            # this file breaks the proof that seals it, and re-taking that
            # proof needs a real Revit and a named model.
            waiting.append((rel, line, n, sealed))
            print("  wait  %s:%d says %d - sealed by %s's proof, and "
                  "correcting it needs a Revit" % (rel, line, n, sealed))
            continue
        check(False,
              "%s:%d says %d and the library is %d  <- %s"
              % (rel, line, n, total, sentence[:90]))

    if waiting:
        print("\n%d WAITING ON A REVIT - each is inside a PROVEN fragment's\n"
              "implementation, where the correction and the re-proof are one\n"
              "act and the re-proof needs a named real model (D-30):" % len(waiting))
        for rel, line, n, who in waiting:
            print("  %s:%d  says %d  (%s)" % (rel, line, n, who))
        print("  Take them with the next proving pass, or leave them - a\n"
              "  stale count is cheaper than a broken proof.")

    print()
    if FAILURES:
        print("FAILED")
        for one in FAILURES:
            print("  - %s" % one)
        print("\n  Either derive the number, or - if the sentence is a record")
        print("  of a measurement - say the date in the SAME sentence. Do not")
        print("  widen the pattern, and do not edit a dated measurement to a")
        print("  number it was never taken at.")
        return 1
    print("PASSED - %d total(s) checked against %d on disk; every one is "
          "current or dated,\n         and %d wait on a Revit."
          % (len(found), total, len(waiting)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
