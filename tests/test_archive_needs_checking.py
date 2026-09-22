# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/archive-needs-checking.py moves the done checks out of NEEDS-CHECKING.md -
and every reader of that file sees the same register afterwards.

    python tests/test_archive_needs_checking.py

Every case runs against a SMALL REGISTER BUILT HERE, never docs/NEEDS-CHECKING.md.

WHAT IS PROVED
--------------
  1. a row struck at both ends moves to its group's file, each cell labelled by
     its table's header, and its line stays with the SAME number of cells - for a
     two-, three- and five-column table alike;
  2. an open row, a row struck at one end only, and a struck row that says it is
     STILL WORTH running or is still OWED all stay byte for byte;
  3. row B8's dated PASSED - what tools/check-docs.py reads - survives in the stub;
  4. owner-queue, check-gaps and balance-of-work give the same answers after;
  5. a second run moves nothing, and CRLF stays CRLF.
"""

import importlib.util
import io
import os
import shutil
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

spec = importlib.util.spec_from_file_location("archive_needs_checking",
                                              os.path.join(ROOT, "tools", "archive-needs-checking.py"))
TOOL = importlib.util.module_from_spec(spec)
spec.loader.exec_module(TOOL)

NL = chr(10)
CR = chr(13)
FAILURES = []


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILURES.append(said)


def register():
    return NL.join([
        "# What still needs checking",
        "",
        "## Group A " + chr(0x2014) + " does it build",
        "",
        "| # | Check | Result |",
        "|---|---|---|",
        "| ~~**A1**~~ | ~~`dotnet --version`~~ | **DONE 2026-08-28.** SDK 8.0.130. |",
        "| **A2** | `pip install mcp` | not yet |",
        "| ~~**A3**~~ | ~~build it~~ | **DONE.** Still worth one run on the owner's PC |",
        "| ~~**A4** | half struck | DONE but the strike was left open |",
        "| ~~**A5**~~ | ~~see [the notes](README.md)~~ | **DONE 2026-09-01.** Read [the log](../tools/x.py). |",
        "",
        "## Group B " + chr(0x2014) + " does Revit still load",
        "",
        "| # | Check | Result |",
        "|---|---|---|",
        "| ~~**B8**~~ | ~~With `write.enabled = true`, move ducts~~ | **PASSED 2026-09-07, after the token fix.** Three ducts moved. |",
        "| ~~**B9**~~ | ~~measure one~~ | **DONE.** The real thing is owed on Windows |",
        "",
        "## Group C " + chr(0x2014) + " two columns",
        "",
        "| # | Check |",
        "|---|---|",
        "| ~~**C1**~~ | ~~the gate refuses, done 2026-09-02~~ |",
        "",
        "## Group AA " + chr(0x2014) + " five columns",
        "",
        "| # | Do | See | Where | Who |",
        "|---|---|---|---|---|",
        "| ~~**AA1**~~ | ~~deploy both~~ | **DONE 2026-09-21.** Two tabs. | the PC | owner |",
        "",
    ])


def fresh(eol=NL):
    top = tempfile.mkdtemp(prefix="heron-nc-test-")
    docs = os.path.join(top, "docs")
    os.makedirs(docs)
    path = os.path.join(docs, "NEEDS-CHECKING.md")
    with io.open(path, "wb") as out:
        out.write(register().replace(NL, eol).encode("utf-8"))
    return top, path, os.path.join(docs, TOOL.ARCHIVE_NAME)


def raw(path):
    if not os.path.isfile(path):
        return b""
    with io.open(path, "rb") as handle:
        return handle.read()


def line_of(text, ident):
    for line in text.replace(CR + NL, NL).split(NL):
        m = TOOL.ROW.match(line)
        if m and m.group(2) + m.group(3) == ident:
            return line
    return None


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")
    original = register()
    top, path, archive = fresh()
    try:
        print("1. a done check moves, and its line keeps its shape")
        p = TOOL.plan(register=path, archive=archive, today="2026-09-23")
        check(p.fatal is None and not p.problems, "the plan is clean (%s)" % (p.fatal or "; ".join(p.problems) or "no problems"))
        moved = sorted(i for blocks in p.moves.values() for i in [b.split(NL)[0] for _, b in blocks])
        check(moved == ["### Row A1", "### Row A5", "### Row AA1", "### Row B8", "### Row C1"],
              "A1, A5, B8, C1 and AA1 move (%s)" % ", ".join(moved))
        check(TOOL.write(p) == TOOL.OK, "it writes")
        after = raw(path).decode("utf-8")
        for ident in ("A1", "A5", "B8", "C1", "AA1"):
            was, now = line_of(original, ident), line_of(after, ident) or ""
            check(len(TOOL.CELL.split(now)) == len(TOOL.CELL.split(was)) and "needs-checking-archive/" in now,
                  "%s keeps %d cells and links to its file" % (ident, len(TOOL.CELL.split(was)) - 2))
        group_a = raw(os.path.join(archive, "group-a.md")).decode("utf-8")
        check("**Check.** ~~`dotnet --version`~~" in group_a and "**Result.** **DONE 2026-08-28.** SDK 8.0.130." in group_a,
              "each cell is labelled by its table's header, words unchanged")
        check("[the notes](../README.md)" in group_a and "[the log](../../tools/x.py)" in group_a,
              "its links are re-pointed one folder deeper")
        check("**Who.** owner" in raw(os.path.join(archive, "group-aa.md")).decode("utf-8"),
              "a five-column row keeps every cell, under its own label")
        readme = raw(os.path.join(archive, "README.md")).decode("utf-8")
        check(all(("(%s)" % n) in readme for n in ("group-a.md", "group-b.md", "group-c.md", "group-aa.md")),
              "the folder has a README linking every group file")

        print("")
        print("2. what must stay does, byte for byte")
        for ident, why in (("A2", "open"), ("A3", "STILL WORTH a run"), ("A4", "struck at one end only"),
                           ("B9", "still OWED")):
            check(line_of(after, ident) == line_of(original, ident), "%s stays - %s" % (ident, why))

        print("")
        print("3. and B8's dated PASSED, which check-docs reads, survives")
        check(TOOL.readers(after)["check-docs B8"] == "2026-09-07", "check-docs would still find PASSED 2026-09-07")

        print("")
        print("4. the three readers give the same answers")
        before, now = TOOL.readers(original), TOOL.readers(after)
        for key in ("owner-queue", "check-gaps", "balance-of-work"):
            check(before[key] == now[key], "%s reads the same register" % key)

        print("")
        print("5. a second run moves nothing")
        kept = raw(path)
        again = TOOL.plan(register=path, archive=archive, today="2026-09-24")
        check(not again.moves and not again.problems and again.already == 5, "nothing left to move, 5 recognised")
        check(TOOL.write(again) == TOOL.OK and raw(path) == kept, "and not one byte changed")
    finally:
        shutil.rmtree(top)

    top, path, archive = fresh(eol=CR + NL)
    try:
        p = TOOL.plan(register=path, archive=archive, today="2026-09-23")
        check(p.fatal is None and not p.problems and TOOL.write(p) == TOOL.OK, "a CRLF register is moved")
        for f in (path, os.path.join(archive, "group-a.md")):
            data = raw(f)
            check(bool(data) and data.count(NL.encode("ascii")) == data.count((CR + NL).encode("ascii")),
                  "every line of %s ends CRLF" % os.path.basename(f))
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
