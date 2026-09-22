# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/archive-fragment-issues.py moves finished defect rows out of the live
register - and moves nothing it should not.

    python tests/test_archive_fragment_issues.py

Every case runs against a SMALL REGISTER BUILT HERE, in a temporary folder,
never against docs/FRAGMENT-ISSUES.md. The real register changes every day,
and a suite that asserted against it would fail the tool for being right the
first time somebody closed a row.

WHAT IS PROVED
--------------
  1. a finished row moves: its line stays with the same number, three cells,
     the opening of its state and a link; its full text lands in the file for
     its band of row numbers, word for word, with its links re-pointed;
  2. what must not move does not, byte for byte - an open row, a PARTLY FIXED
     row, a hedged row, a row that says REOPENED, a PROVED row, a malformed
     row, a row closed in place with NOT FIXED written at the END of its
     state, and one leaving the owner's call to the owner - and no line
     outside the two tables changes, while a row that merely OPENS still
     moves;
  3. every re-pointed link reaches the file it reached before, and the anchor
     it points at is the one check-docs.py derives;
  4. open-defects.py reads the same rows with the same open answer afterwards;
  5. a second run moves nothing and changes no byte;
  6. an archive that already holds a row the register still carries in full
     is REFUSED, and nothing at all is written;
  7. CRLF stays CRLF, and a register mixing CRLF and LF is not touched.

No backslash is typed here either: the escaped pipe and the anchor pattern
are built from chr(92), for the reason the tool's own docstring gives.
"""

import importlib.util
import io
import os
import re
import shutil
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)


def _load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


TOOL = _load("archive_fragment_issues",
             os.path.join(ROOT, "tools", "archive-fragment-issues.py"))
OD = TOOL.OD

BS = chr(92)
NL = chr(10)
CR = chr(13)
PIPE = BS + "|"

FAILURES = []


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILURES.append(said)


ROWS_5 = [
    # finished, carrying every kind of link and an escaped pipe in code
    "| **1** | **A FINISHED DEFECT WITH LINKS.** See [the file](../platform/x.cs), "
    "[D-1](DECISIONS.md#d-1), [the section](#1b-a-human) and `a " + PIPE + " b`. "
    "| **FIXED 2026-09-21, done properly.** The proof is [here](../tests/t.py). |",
    "| **2** | **STILL OWED.** Nothing done yet. | OPEN - needs a sitting |",
    "| **3** | **HALF DONE.** One leg left. | PARTLY FIXED 2026-09-20 - one leg remains |",
    "| **4** | **HEDGED.** Nearly. | FIXED 2026-09-20, except the Windows leg |",
    "| **5** | **CAME BACK.** Twice. | FIXED 2026-09-19. REOPENED 2026-09-21 when it came back |",
    "| **6** | **A PROVING ROW.** Round one. | Proved, awaiting signature |",
    # a bare pipe in the text - four parts where three belong
    "| **7** | **BROKEN ROW.** a | b | CLOSED - never read right |",
    # closed in place, with what is left written at the END, past the opening
    # the hedges are read in - the shape the first version of the tool missed
    "| **8** | **FIXED BUT OWED.** One route of two. | FIXED 2026-09-19 by the first route. "
    + "The detail runs on. " * 12 + "WHAT IS NOT FIXED: the store still goes stale |",
    # and the owner's call spelled with the curly apostrophe the register also uses
    "| **9** | **THE OWNER DECIDES.** A choice. | CLOSED 2026-09-18 for now. "
    + "More words follow here. " * 10 + "Whether to widen it is the owner" + chr(0x2019) + "s call |",
    # the two shapes a second read found in the archive on the first night
    "| **11** | **OWED ON WINDOWS.** A proof. | FIXED 2026-09-21 in the add-in. "
    + "The build is clean on every release. " * 6 + "The real thing is owed on Windows |",
    "| **12** | **NEEDS A MODEL.** Words. | FIXED 2026-09-20, singular and plural both written out. "
    + "**NOT RUN IN REVIT** - it needs a model changing |",
    # OPENS and OPENING are not OPEN - this one is finished and must move
    "| **10** | **OPENS NOW.** Fine. | FIXED 2026-09-20 - the dialog opens cleanly and the OPENING count is right |",
    "| **30** | **SECOND BAND.** Its own file. | CLOSED 2026-09-18 - superseded by row 1 |",
]

ROWS_5B = [
    "| **1** | **A READING DEFECT.** In [x.py](../brain/x.py). | FIXED 2026-09-22. Repaired and tested. |",
    "| **2** | **OPEN READING.** Recorded. | OPEN - recorded, not repaired |",
]

MOVED = 4        # rows 1, 10, 30 and 5b-1


def register_text():
    return NL.join([
        "# Fragments with something wrong",
        "",
        "## 1b. A HUMAN",
        "",
        "Some text.",
        "",
        OD.SECTION_START + " found by proving",
        "",
        "| # | Defect | State |",
        "|---|---|---|",
    ] + ROWS_5 + [
        "",
        OD.SECTION_5B + " the repository, file by file",
        "",
        "| # | Defect | State |",
        "|---|---|---|",
    ] + ROWS_5B + [
        "",
        OD.SECTION_END + " - by what they need",
        "",
        "Nothing here.",
        "",
    ])


def fresh(text, eol=NL):
    top = tempfile.mkdtemp(prefix="heron-archive-test-")
    docs = os.path.join(top, "docs")
    os.makedirs(docs)
    register = os.path.join(docs, "FRAGMENT-ISSUES.md")
    with io.open(register, "wb") as out:
        out.write(text.replace(NL, eol).encode("utf-8"))
    return top, register, os.path.join(docs, TOOL.ARCHIVE_NAME)


def raw(path):
    """The file's bytes - or none at all when it is missing, so a tool that
    failed to write it shows up as a FAIL below instead of a crash that hides
    every check after it."""
    if not os.path.isfile(path):
        return b""
    with io.open(path, "rb") as handle:
        return handle.read()


def lines_of(path):
    return raw(path).decode("utf-8").replace(CR + NL, NL).split(NL)


def row_line(lines, section, number):
    """The line of row NUMBER inside the section whose heading starts SECTION."""
    inside = False
    for line in lines:
        if line.startswith(section):
            inside = True
            continue
        if inside and line.startswith("## "):
            break
        if inside:
            match = OD.ROW.match(line)
            if match and int(match.group(1)) == number:
                return line
    return None


def anchor_of(text):
    """tools/check-docs.py's rule: GitHub's, runs of hyphens NOT collapsed."""
    slug = re.sub("[^" + BS + "w" + BS + "s-]", "", text.lower())
    return slug.strip().replace(" ", "-")


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")
    before = register_text()
    original = before.split(NL)

    top, register, archive = fresh(before)
    try:
        print("1. a finished row moves, and leaves its line behind")
        p = TOOL.plan(register=register, archive=archive, today="2026-09-22")
        check(p.fatal is None and not p.problems,
              "the plan is clean (%s)" % (p.fatal or "; ".join(p.problems) or "no problems"))
        check(TOOL.write(p) == TOOL.OK, "it writes")
        check(sorted(p.sizes) == sorted(p.moves)
              and all(p.sizes[n] == len(raw(os.path.join(archive, n))) for n in p.moves),
              "the size it reports for each file is the size it wrote")
        after = lines_of(register)
        check(len(after) == len(original), "the register has the same number of lines")

        one = row_line(after, OD.SECTION_START, 1) or ""
        check(len(OD.CELL.split(one)) == 5, "row 1 still has three cells")
        check("[full row](fragment-issues-archive/proving-defects-001-025.md#row-1)" in one,
              "row 1 links to its band's file and its own anchor")
        check(OD.CELL.split(one)[3].strip().startswith("**FIXED 2026-09-21, done properly.**"),
              "row 1 keeps the opening of its state")
        check("x.cs" not in one, "row 1's long text is no longer in the register")
        ten = row_line(after, OD.SECTION_START, 10) or ""
        check("proving-defects-001-025.md#row-10" in ten,
              "row 10 moves - OPENS and OPENING are not the word OPEN")
        thirty = row_line(after, OD.SECTION_START, 30) or ""
        check("proving-defects-026-050.md#row-30" in thirty, "row 30 goes to the next band's file")
        five_b = row_line(after, OD.SECTION_5B, 1) or ""
        check("reading-defects-5b-001-025.md#row-5b-1" in five_b,
              "row 5b-1 goes to section 5b's own file")

        band = os.path.join(archive, "proving-defects-001-025.md")
        body = raw(band).decode("utf-8") if os.path.exists(band) else ""
        check("### Row 1" in body.split(NL), "the archive holds row 1 under its own heading")
        check("(../../platform/x.cs)" in body, "a link up out of docs/ is re-pointed one folder deeper")
        check("(../DECISIONS.md#d-1)" in body, "a sibling link is re-pointed, its anchor kept")
        check("(../FRAGMENT-ISSUES.md#1b-a-human)" in body,
              "an in-page anchor now points back into the register")
        check("`a " + PIPE + " b`" in body, "an escaped pipe in code arrives exactly as written")
        check("(../../tests/t.py)" in body, "links in the state are re-pointed too")
        check("**Defect.** **A FINISHED DEFECT WITH LINKS.** See [the file]" in body,
              "the defect's own words arrive unchanged")
        readme_path = os.path.join(archive, "README.md")
        readme = raw(readme_path).decode("utf-8") if os.path.exists(readme_path) else ""
        check(all(name in readme for name in ("proving-defects-001-025.md",
                                               "proving-defects-026-050.md",
                                               "reading-defects-5b-001-025.md")),
              "the folder has a README listing every file")

        print("")
        print("2. what must not move does not, byte for byte")
        for number, why in ((2, "open"), (3, "PARTLY FIXED"), (4, "hedged with EXCEPT"),
                            (5, "says REOPENED"), (6, "PROVED is not a closing word"),
                            (7, "malformed"),
                            (8, "NOT FIXED written past the opening"),
                            (9, "the owner's call, with a curly apostrophe"),
                            (11, "the real thing is owed on Windows"),
                            (12, "NOT RUN IN REVIT")):
            check(row_line(after, OD.SECTION_START, number)
                  == row_line(original, OD.SECTION_START, number),
                  "row %d stays in full - %s" % (number, why))
        check(row_line(after, OD.SECTION_5B, 2) == row_line(original, OD.SECTION_5B, 2),
              "row 5b-2 stays in full - open")
        check([l for l in after if not OD.ROW.match(l)]
              == [l for l in original if not OD.ROW.match(l)],
              "no line outside the two tables changed")

        print("")
        print("3. every re-pointed link reaches the file it reached before")
        docs = os.path.dirname(register)
        for was, now in (("../platform/x.cs", "../../platform/x.cs"),
                         ("DECISIONS.md", "../DECISIONS.md"),
                         ("FRAGMENT-ISSUES.md", "../FRAGMENT-ISSUES.md"),
                         ("../tests/t.py", "../../tests/t.py")):
            check(os.path.normpath(os.path.join(docs, was))
                  == os.path.normpath(os.path.join(archive, now)),
                  "%s from docs/ is %s from the archive" % (was, now))
        headings = [line[4:] for line in body.split(NL) if line.startswith("### ")]
        check("row-1" in [anchor_of(h) for h in headings],
              "check-docs.py would find the #row-1 anchor the register links to")
        check(anchor_of("Row 5b-1") == "row-5b-1", "and #row-5b-1 for a row of section 5b")

        print("")
        print("4. open-defects.py reads the same rows, the same way")
        check(p.parity is not None and len(p.parity) == len(ROWS_5) + len(ROWS_5B),
              "the tool proved it before writing (%s rows)"
              % (len(p.parity) if p.parity else "no"))
        copy = os.path.join(docs, "original-copy.md")
        with io.open(copy, "wb") as out:
            out.write(before.encode("utf-8"))
        check(TOOL._reading(register) == TOOL._reading(copy),
              "and reading both again, independently, agrees")
        os.remove(copy)

        print("")
        print("5. a second run moves nothing and changes no byte")
        # A broken tool may have written no folder at all. Report that as the
        # failure it is, rather than crash and hide the checks after it.
        names = os.listdir(archive) if os.path.isdir(archive) else []
        snapshot = dict((name, raw(os.path.join(archive, name))) for name in names)
        kept = raw(register)
        second = TOOL.plan(register=register, archive=archive, today="2026-09-23")
        check(not second.moves and not second.problems and second.already == MOVED,
              "nothing left to move; %d rows recognised as moved (%d)" % (MOVED, second.already))
        check(TOOL.write(second) == TOOL.OK, "writing an empty plan is fine")
        check(raw(register) == kept
              and all(raw(os.path.join(archive, n)) == b for n, b in snapshot.items())
              and sorted(os.listdir(archive) if os.path.isdir(archive) else []) == sorted(snapshot),
              "and not one byte changed")
    finally:
        shutil.rmtree(top)

    print("")
    print("6. an archive that already holds a row the register carries in full is refused")
    top, register, archive = fresh(register_text())
    try:
        os.makedirs(archive)
        planted = os.path.join(archive, "proving-defects-001-025.md")
        with io.open(planted, "wb") as out:
            out.write(NL.join(["# planted", "", "### Row 1", "", "someone was here", ""]).encode("utf-8"))
        kept, kept_plant = raw(register), raw(planted)
        p = TOOL.plan(register=register, archive=archive, today="2026-09-22")
        check(any("row 1 is already in" in problem for problem in p.problems),
              "it names the row it will not move")
        check(TOOL.write(p) == TOOL.REFUSED, "it refuses")
        check(raw(register) == kept and raw(planted) == kept_plant, "the register and that file are untouched")
        check(sorted(os.listdir(archive)) == ["proving-defects-001-025.md"],
              "and no other file was written either")
    finally:
        shutil.rmtree(top)

    print("")
    print("7. line endings are kept, and a mixture is left alone")
    top, register, archive = fresh(register_text(), eol=CR + NL)
    try:
        p = TOOL.plan(register=register, archive=archive, today="2026-09-22")
        check(p.fatal is None and not p.problems and TOOL.write(p) == TOOL.OK,
              "a CRLF register is moved")
        data = raw(register)
        check(data.count(NL.encode("ascii")) == data.count((CR + NL).encode("ascii")),
              "every line of the register still ends CRLF")
        band = raw(os.path.join(archive, "proving-defects-001-025.md"))
        # an empty file has no lines to end wrongly, so it must not pass here
        check(bool(band)
              and band.count(NL.encode("ascii")) == band.count((CR + NL).encode("ascii")),
              "and every line of the archive file it wrote")
    finally:
        shutil.rmtree(top)

    top, register, archive = fresh(register_text().replace(NL, CR + NL, 3))
    try:
        kept = raw(register)
        p = TOOL.plan(register=register, archive=archive, today="2026-09-22")
        check(p.fatal is not None and "mixes" in p.fatal,
              "a register mixing CRLF and LF is refused")
        check(TOOL.write(p) == TOOL.COULD_NOT and raw(register) == kept
              and not os.path.exists(archive),
              "and not touched")
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
