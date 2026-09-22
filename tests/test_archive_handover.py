# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/archive-handover.py moves session notes out of HANDOVER.md, one file per
sitting - and keeps the manual, the quick start and every link working.

    python tests/test_archive_handover.py

Every case runs against a SMALL HANDOVER BUILT HERE, in a temporary folder, never
against docs/HANDOVER.md, which changes every session.

WHAT IS PROVED
--------------
  1. a sitting is a dated ### entry and the undated ### entries after it, and a
     note below section 10a runs to its "---"; each lands in its own file, named
     by its date and title, with its words unchanged;
  2. the quick start, the preamble, the session-archive pointer and the manual
     (sections up to 10a) stay, and a table of the newest sittings replaces the
     entries;
  3. every link lands: a relative link one folder deeper, a link to a heading
     that moved - from a sitting or from what stays - to the file it went to, a
     link to a heading that stayed back into HANDOVER.md;
  4. the archive README gains one row per sitting, newest first, and its old
     rows keep their order;
  5. a second run moves nothing and changes no byte;
  6. a link from ANOTHER document to a heading that would move stops the run,
     and so does a LINKED heading that two sittings share; nothing is written;
  7. CRLF stays CRLF.

No backslash is typed here, for the reason tools/archive-fragment-issues.py gives.
"""

import importlib.util
import io
import os
import shutil
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

spec = importlib.util.spec_from_file_location("archive_handover", os.path.join(ROOT, "tools", "archive-handover.py"))
TOOL = importlib.util.module_from_spec(spec)
spec.loader.exec_module(TOOL)

NL = chr(10)
CR = chr(13)
DASH = chr(0x2014)
FAILURES = []


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILURES.append(said)


def handover_text(extra_staying=""):
    return NL.join([
        "# Heron AI " + DASH + " Session Handover",
        "",
        "Intro.",
        "",
        "## If you are the owner, starting your PC " + DASH + " say this and nothing else",
        "",
        "Quick start. See [the recipe](#9a-the-recipe).",
        "",
        "## WHERE THIS STANDS RIGHT NOW " + DASH + " read this, then 9a",
        "",
        "Preamble. The newest is [the second sitting](#2026-09-22--second-sitting)." + extra_staying,
        "",
        "### 2026-09-22 " + DASH + " SECOND SITTING",
        "",
        "Text with [a file](../tools/x.py), [the first](#2026-09-21--first-sitting) and [the recipe](#9a-the-recipe).",
        "",
        "### A PART OF THE SECOND SITTING",
        "",
        "```bash",
        "### 2026-09-01 not a heading, a comment in a shell example",
        "---",
        "```",
        "",
        "More of the same sitting.",
        "",
        "---",
        "",
        "### 2026-09-21 " + DASH + " FIRST SITTING",
        "",
        "Text. [Forward](#2026-09-22--second-sitting).",
        "",
        "## The session archive " + DASH + " what happened before today",
        "",
        "Pointer text.",
        "",
        "## 9a. The recipe",
        "",
        "Recipe text.",
        "",
        "## 10a. The rule section",
        "",
        "Keep only three things.",
        "",
        "## What is NEW since the last handover",
        "",
        "Written 2026-09-16, citing [a decision](DECISIONS.md).",
        "",
        "## Mistakes worth not repeating",
        "",
        "The same note.",
        "",
        "---",
        "",
        "## A later note",
        "",
        "Written 2026-09-19.",
        "",
    ])


README = NL.join([
    "# Session archive",
    "",
    "## The sittings",
    "",
    "| Date | Sitting |",
    "|---|---|",
    "| 2026-09-12 | [Older](2026-09-12-older.md) |",
    "| 2026-09-12 | [Older still, same day](2026-09-12-older-still.md) |",
    "",
])


def fresh(text, eol=NL, other=None):
    top = tempfile.mkdtemp(prefix="heron-handover-test-")
    docs = os.path.join(top, "docs")
    archive = os.path.join(docs, "handover-archive")
    os.makedirs(archive)
    for name, body in (("HANDOVER.md", text), (os.path.join("handover-archive", "README.md"), README),
                       (os.path.join("handover-archive", "2026-09-12-older.md"), "# Older" + NL),
                       (os.path.join("handover-archive", "2026-09-12-older-still.md"), "# Older still" + NL)):
        with io.open(os.path.join(docs, name), "wb") as out:
            out.write(body.replace(NL, eol).encode("utf-8"))
    if other:
        with io.open(os.path.join(docs, "OTHER.md"), "wb") as out:
            out.write(other.encode("utf-8"))
    return top, os.path.join(docs, "HANDOVER.md"), archive


def raw(path):
    if not os.path.isfile(path):
        return b""
    with io.open(path, "rb") as handle:
        return handle.read()


def text(path):
    return raw(path).decode("utf-8").replace(CR + NL, NL)


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")

    top, handover, archive = fresh(handover_text())
    try:
        print("1. sittings are found whole, and each lands in its own file")
        p = TOOL.plan(handover=handover, archive=archive, today="2026-09-23")
        check(p.fatal is None and not p.problems, "the plan is clean (%s)" % (p.fatal or "; ".join(p.problems) or "no problems"))
        names = [s.name for s in p.sittings]
        check(names == ["2026-09-22-second-sitting.md", "2026-09-21-first-sitting.md",
                        "2026-09-16-what-is-new-since-the-last-handover.md", "2026-09-19-a-later-note.md"],
              "four sittings, named by date and title (%s)" % ", ".join(names))
        check(TOOL.write(p) == TOOL.OK, "it writes")
        second = text(os.path.join(archive, "2026-09-22-second-sitting.md"))
        check("### A PART OF THE SECOND SITTING" in second and "More of the same sitting." in second,
              "an undated entry travels with the dated one above it")
        check("### 2026-09-01 not a heading" in second and not any("2026-09-01" in n for n in names),
              "a dated ### line inside a code block is part of the sitting, not a new one")
        note = text(os.path.join(archive, "2026-09-16-what-is-new-since-the-last-handover.md"))
        check("## Mistakes worth not repeating" in note and "The same note." in note,
              "a note below 10a runs to its --- line")
        check("A later note" not in note, "and stops there")

        print("")
        print("2. the manual and the quick start stay; a table replaces the entries")
        after = text(handover)
        for kept in ("## If you are the owner", "Preamble.", "## The session archive", "## 9a. The recipe",
                     "## 10a. The rule section", "Keep only three things.", "### Latest sittings"):
            check(kept in after, "HANDOVER.md keeps %r" % kept)
        for gone in ("### 2026-09-22 " + DASH + " SECOND SITTING", "More of the same sitting.",
                     "## Mistakes worth not repeating", "## A later note"):
            check(gone not in after, "and no longer holds %r" % gone[:40])
        check("| 2026-09-22 | [SECOND SITTING](handover-archive/2026-09-22-second-sitting.md) |" in after,
              "the table links the newest sitting")

        print("")
        print("3. every link lands")
        check("[the second sitting](handover-archive/2026-09-22-second-sitting.md#2026-09-22--second-sitting)" in after,
              "a staying link to a moved heading follows it to its file")
        check("[the recipe](#9a-the-recipe)" in after, "a staying link to a staying heading is left alone")
        check("[a file](../../tools/x.py)" in second, "a relative link is re-pointed one folder deeper")
        check("[the first](2026-09-21-first-sitting.md#2026-09-21--first-sitting)" in second,
              "a link between two moved sittings points at the other file")
        check("[the recipe](../HANDOVER.md#9a-the-recipe)" in second, "a link to the manual points back into HANDOVER.md")
        first = text(os.path.join(archive, "2026-09-21-first-sitting.md"))
        check("[Forward](2026-09-22-second-sitting.md#2026-09-22--second-sitting)" in first,
              "and the other way round")
        check("[a decision](../DECISIONS.md)" in note, "a note's link to a sibling document is re-pointed")
        check(os.path.normpath(os.path.join(os.path.dirname(handover), "../tools/x.py"))
              == os.path.normpath(os.path.join(archive, "../../tools/x.py")),
              "../tools/x.py from docs/ is ../../tools/x.py from the archive")

        print("")
        print("4. the archive README gains a row per sitting, newest first")
        rows = [l for l in text(os.path.join(archive, "README.md")).split(NL) if l.startswith("| 20")]
        dates = [r.split("|")[1].strip() for r in rows]
        check(dates == sorted(dates, reverse=True), "rows are newest first (%s)" % ", ".join(dates))
        check(rows[-2:] == ["| 2026-09-12 | [Older](2026-09-12-older.md) |",
                            "| 2026-09-12 | [Older still, same day](2026-09-12-older-still.md) |"],
              "old rows keep their order")
        check(len(rows) == 6, "one new row per sitting (%d rows)" % len(rows))

        print("")
        print("5. a second run moves nothing")
        snapshot = dict((n, raw(os.path.join(archive, n))) for n in os.listdir(archive))
        kept = raw(handover)
        again = TOOL.plan(handover=handover, archive=archive, today="2026-09-24")
        check(not again.sittings and not again.problems, "nothing left to move")
        check(TOOL.write(again) == TOOL.OK and raw(handover) == kept
              and all(raw(os.path.join(archive, n)) == b for n, b in snapshot.items())
              and sorted(os.listdir(archive)) == sorted(snapshot), "and not one byte changed")

        print("")
        print("5b. a note that lands here anyway moves on the next run, and joins the table")
        lines = text(handover).split(NL)
        # Found by position, not by index(): a tool that refused above left no
        # table, and that is a FAIL to report here, not a crash that hides it.
        at = next((i for i, l in enumerate(lines) if l == "### Latest sittings"), None)
        check(at is not None, "the table is there for a new note to land above")
        lines[(at or 0):(at or 0)] = ["### 2026-09-24 " + DASH + " THIRD SITTING", "", "Written the next day.", "", "---", ""]
        with io.open(handover, "wb") as out:
            out.write(NL.join(lines).encode("utf-8"))
        later = TOOL.plan(handover=handover, archive=archive, today="2026-09-24")
        check([s.name for s in later.sittings] == ["2026-09-24-third-sitting.md"] and not later.problems,
              "only the new sitting moves (%s)" % ", ".join(s.name for s in later.sittings))
        check(TOOL.write(later) == TOOL.OK, "it writes")
        table = [l for l in text(handover).split(NL) if l.startswith("| 20")]
        check(table[:2] == ["| 2026-09-24 | [THIRD SITTING](handover-archive/2026-09-24-third-sitting.md) |",
                            "| 2026-09-22 | [SECOND SITTING](handover-archive/2026-09-22-second-sitting.md) |"],
              "the table leads with it and keeps the rows it had")
        check(len(table) == 5 and text(handover).count("### Latest sittings") == 1,
              "one table, every sitting still listed (%d rows)" % len(table))
        check("Written the next day." in text(os.path.join(archive, "2026-09-24-third-sitting.md")),
              "and the note's words are in its file")
    finally:
        shutil.rmtree(top)

    print("")
    print("6. what it cannot rewrite stops it")
    top, handover, archive = fresh(handover_text(), other="See [the first](HANDOVER.md#2026-09-21--first-sitting)." + NL)
    try:
        kept = raw(handover)
        p = TOOL.plan(handover=handover, archive=archive, today="2026-09-23")
        check(any("OTHER.md links to #2026-09-21--first-sitting" in x for x in p.problems),
              "another document's link to a moving heading is named")
        check(TOOL.write(p) == TOOL.REFUSED and raw(handover) == kept
              and sorted(os.listdir(archive)) == ["2026-09-12-older-still.md", "2026-09-12-older.md", "README.md"],
              "and nothing is written")
    finally:
        shutil.rmtree(top)

    shared = handover_text(" Also [the fix](#the-fix).").replace(
        "More of the same sitting.", "More of the same sitting." + NL + NL + "#### The fix" + NL + NL + "One.").replace(
        "Text. [Forward]", "#### The fix" + NL + NL + "Two." + NL + NL + "Text. [Forward]")
    top, handover, archive = fresh(shared)
    try:
        p = TOOL.plan(handover=handover, archive=archive, today="2026-09-23")
        check(any("#the-fix is in 2 sittings" in x for x in p.problems),
              "a LINKED heading two sittings share is named")
        check(TOOL.write(p) == TOOL.REFUSED, "and refused")
    finally:
        shutil.rmtree(top)

    print("")
    print("7. CRLF stays CRLF")
    top, handover, archive = fresh(handover_text(), eol=CR + NL)
    try:
        p = TOOL.plan(handover=handover, archive=archive, today="2026-09-23")
        check(p.fatal is None and not p.problems and TOOL.write(p) == TOOL.OK, "a CRLF HANDOVER is split")
        for path in (handover, os.path.join(archive, "2026-09-22-second-sitting.md"), os.path.join(archive, "README.md")):
            data = raw(path)
            check(bool(data) and data.count(NL.encode("ascii")) == data.count((CR + NL).encode("ascii")),
                  "every line of %s ends CRLF" % os.path.basename(path))
    finally:
        shutil.rmtree(top)

    print("")
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        return 1
    print("PASSED")
    return 0


if __name__ == "__main__":
    sys.exit(main())
