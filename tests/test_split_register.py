# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/split-register.py gives every section of FRAGMENT-ISSUES.md its own file
and the rows of sections 5 and 5b files of 25, and every section of
PROPOSALS.md and OPEN-QUESTIONS.md its own file - and every reader of each
register still reads exactly what it read.

    python tests/test_split_register.py

Every case runs against a SMALL register BUILT HERE, never the real one, which
changes every day and would fail this suite for the tool being right.

WHAT IS PROVED
--------------
  1. every section moves to its own file, named from its number or from its
     heading's words and date; the page's own rules stay, and so does a
     section whose name another has taken - each for its stated reason;
  2. sections 5 and 5b keep their own words on the page and their rows move
     25 to a file by number, a row with the lines that continue it;
  3. read back through tools/register-text.py, the files ARE the register,
     byte for byte;
  4. every link is re-pointed to reach what it reached: one folder deeper, a
     link to a heading that moved follows it, a link from the page into a
     moved heading goes to its file;
  5. THE REAL READERS - open-defects.py's report, review-ledger.py's rows of
     5b, archive-fragment-issues.py's plan - answer the same on the split
     register as on the one file;
  6. a second run moves nothing; a row written after the last file's band is
     moved to its own band by the next run, every other file kept as it was;
     and a link whose words name a row it moves, at the file the row left,
     follows the row - on the page, in a section's file, in another row, and
     in each .md, .yaml and .py elsewhere, kept CRLF - while an example in
     code, a link naming two rows, a row that stays, a file of that name in
     another folder and a document git does not track are left as they were;
  7. a missing file, a section's file that no longer opens with its heading,
     or a rows file with no table is RegisterBroken - never a shorter
     register - and open-defects.py says so;
  8. archive-fragment-issues.py still moves a finished row on the split
     register: the full row to the archive, its line into its rows file;
  9. it refuses, writing nothing, when another document links to a heading
     that would leave the page;
 10. CRLF in, CRLF out, and still byte for byte; a register mixing the two is
     not read at all;
 11. open-defects.py, review-ledger.py and archive-fragment-issues.py each
     read the register through the one reader;
 12. PROPOSALS.md splits the same way - every section to its own file, two
     sections written on one day to two files - and owner-queue.py and
     balance-of-work.py read the same proposals from it, through the reader;
 13. OPEN-QUESTIONS.md splits the same way, one file per tier, and
     owner-queue.py and check-docs.py - its questions and its Progress line
     - read the same questions from it, through the reader;
 14. a row that links to a 5b row past 999 does not stop the split: row 1001
     moves to a rows file named with four digits, the link follows it, and
     the register still reads back as it was (row 5b-1169);
 15. a row from the cut-over on has a file of its own (row 5b-1564): the
     page's one line says where the change is and names no file; the rows are
     read after the rows files' rows, in number order, with their links as
     the page writes them, and open-defects.py lists them unchanged; a new
     row an older branch appended to the rows file is read in the same
     order, and `--new-rows` moves it alone, with every link that names it,
     and re-bands nothing; a number held in both layouts is read twice and
     the move is refused; a file that is not what its name says, and a
     reader that cannot list the folder, are RegisterBroken; and the archive
     tool leaves a finished row's line in the row's own file.
"""

import contextlib
import importlib.util
import io
import os
import re
import shutil
import stat
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except AttributeError:
        pass


def _load(name, filename):
    """A tool, or None - so its absence is one clean failure, not a traceback."""
    try:
        spec = importlib.util.spec_from_file_location(name, os.path.join(ROOT, "tools", filename))
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except (IOError, OSError, SyntaxError):
        return None


RT = _load("register_text", "register-text.py")
SPLIT = _load("split_register", "split-register.py")

NL = chr(10)
CR = chr(13)
BS = chr(92)
DASH = chr(0x2014)
FENCE = "`" * 3
FAILURES = []

S5 = "## 5. HERON'S OWN DEFECTS found by proving"
S5B = "## 5b. HERON'S OWN DEFECTS found by reading the repository, file by file"


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILURES.append(said)


def register_text():
    five = ["| %d | One. %s [full row](fragment-issues-archive/proving-defects-001-025.md#row-1) | Fixed %s D-54 |"
            % (1, DASH, DASH),
            "| **2** | **Two.** Found 2026-09-01. | OPEN - recorded, not repaired. |",
            "| | **A CONTINUATION.** More words for row 2, and [row 3](#). |",
            "| **3** | **Three.** Found 2026-09-01. | FIXED 2026-09-02 - done, and [the tool](../tools/x.py) shows it. |"]
    five += ["| **%d** | **Row %d.** Found 2026-09-01. | OPEN - recorded. |" % (n, n) for n in range(4, 28)]
    return NL.join([
        "<!-- Heron-Agent:  none -->",
        "",
        "# Fragments with something wrong " + DASH + " the sit-down list",
        "",
        "> | | |",
        "> |---|---|",
        "> | **Finished rows** | the [archive](fragment-issues-archive/README.md), and [the deep note](#a-deep-note) |",
        "",
        "**What this is.** A register built by the suite.",
        "",
        FENCE + "bash",
        "## a shell comment, not a section",
        FENCE,
        "",
        "---",
        "",
        "## 1. SUSPECT " + DASH + " these upset Revit",
        "",
        "| Fragment | Why |",
        "|---|---|",
        "| `a` | see [a decision](DECISIONS.md) |",
        "",
        "### A deep note",
        "",
        "It points at [section 3](#3-needs-the-owner--the-model), at [a tool](../tools/x.py) and at [the register](#).",
        "",
        "## 3. NEEDS THE OWNER " + DASH + " the model",
        "",
        "Arrange it by hand, as [the page](FRAGMENT-ISSUES.md) says.",
        "",
        S5,
        "",
        "> Intro for section 5.",
        "",
        "| # | Defect | State |",
        "|---|---|---|",
    ] + five + [
        "",
        "---",
        "",
        S5B,
        "",
        "> Intro for section 5b.",
        "",
        "**A ROW HERE IS WRITTEN FOR A SESSION THAT WAS NOT PRESENT.**",
        "",
        "| # | Defect | State |",
        "|---|---|---|",
        "| **1** | **One.** `ls x " + BS + "| wc -l` | OPEN - recorded, not repaired. |",
        "| **2** | **Two.** | FIXED 2026-09-02 - it was. |",
        "",
        "*No count is typed here.*",
        "",
        "---",
        "",
        "## 6. WHAT CANNOT BE RUN AT ALL " + DASH + " by what they need",
        "",
        "Nothing, in this register.",
        "",
        "## Tag leaders, 2026-09-21 " + DASH + " five things measured",
        "",
        "Measured once.",
        "",
        "## Tag leaders, 2026-09-21 " + DASH + " measured again",
        "",
        "Measured twice.",
        "",
        "## Add to this file, do not start another",
        "",
        "The page's own rules.",
        "",
    ])


def make_root(parent, text, name="one"):
    root = os.path.join(parent, name)
    os.makedirs(os.path.join(root, "docs"))
    os.makedirs(os.path.join(root, "tools"))
    for filename, body in (("docs/FRAGMENT-ISSUES.md", text), ("docs/DECISIONS.md", "# D" + NL),
                           ("tools/x.py", "# x" + NL)):
        with io.open(os.path.join(root, *filename.split("/")), "w", encoding="utf-8", newline="") as handle:
            handle.write(body)
    return root


def index_of(root):
    return os.path.join(root, "docs", "FRAGMENT-ISSUES.md")


def folder_of(root):
    return os.path.join(root, "docs", "fragment-issues")


def read(path):
    with io.open(path, "rb") as handle:
        return handle.read().decode("utf-8")


def put(path, text):
    with io.open(path, "w", encoding="utf-8", newline="") as handle:
        handle.write(text)


def whole(root):
    return RT.register_text(index_of(root))


def readers_at(root):
    """What each real reader answers, reading the register at ROOT."""
    out = {}
    od = _load("open_defects_at_root", "open-defects.py")
    od.REGISTER = index_of(root)
    buffer = io.StringIO()
    with contextlib.redirect_stdout(buffer):
        od.main()
    out["open-defects"] = buffer.getvalue()
    rl = _load("review_ledger_at_root", "review-ledger.py")
    rl.REGISTER = index_of(root)
    out["review-ledger"] = rl.register_rows()
    af = _load("archive_fragment_issues_at_root", "archive-fragment-issues.py")
    p = af.plan(register=index_of(root), archive=os.path.join(root, "docs", af.ARCHIVE_NAME), today="2026-09-23")
    buffer = io.StringIO()
    with contextlib.redirect_stdout(buffer):
        af.report(p, False)
    out["archive-fragment-issues"] = buffer.getvalue()
    return out


def _writable(func, path, _info):
    os.chmod(path, stat.S_IWRITE)       # git marks its objects read-only on Windows
    func(path)


def raises(call):
    try:
        call()
    except RT.RegisterBroken:
        return True
    return False


def main():
    work = tempfile.mkdtemp()
    try:
        run(work)
    finally:
        shutil.rmtree(work, onerror=_writable)
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        return 1
    print("PASSED  one file per section, rows in files of 25, and the register reads as one")
    return 0


def after_write(original, root, today):
    """Sections 3 to 8: they read what the split wrote, so they run only when it wrote."""
    names = sorted(os.listdir(folder_of(root)))
    check(names == ["section-1.md", "section-3.md", "section-5-rows-001-025.md", "section-5-rows-026-050.md",
                    "section-5b-rows-001-025.md", "section-6.md", "tag-leaders-2026-09-21.md"],
          "one file per section, and one per band of rows: %s" % ", ".join(names))
    index = read(index_of(root))
    check(RT.file_line("fragment-issues", "section-1.md") in index and "| `a` |" not in index,
          "the page keeps a moved section's heading and a line naming its file, not its words")
    check("> Intro for section 5." in index and "| **2** |" not in index and "| # | Defect | State |" not in index,
          "section 5 keeps its own words on the page, and none of its table")
    check(RT.rows_line("fragment-issues", ["section-5-rows-001-025.md", "section-5-rows-026-050.md"],
                       ["001-025", "026-050"]) in index, "and one line where the table was, naming its files in order")
    check("*No count is typed here.*" in index, "what follows 5b's table stays on the page too")
    five = read(os.path.join(folder_of(root), "section-5-rows-001-025.md"))
    check("| **25** |" in five and "| **26** |" not in five and "| | **A CONTINUATION.**" in five,
          "rows 1 to 25 are in their file, row 2 with the line that continues it")
    check(five.count("| # | Defect | State |") == 1, "and each rows file has the table's header, so it reads as a table")
    check(whole(root) == original, "read back, it is the register byte for byte")

    print()
    print("4. Every link reaches what it reached")
    one = read(os.path.join(folder_of(root), "section-1.md"))
    check("[a decision](../DECISIONS.md)" in one, "a link one folder deeper: DECISIONS.md -> ../DECISIONS.md")
    check("[a tool](../../tools/x.py)" in one, "and ../tools/x.py -> ../../tools/x.py")
    check("[section 3](section-3.md#3-needs-the-owner--the-model)" in one,
          "a link to a heading in another section follows it")
    check("[the register](../FRAGMENT-ISSUES.md#)" in one, "a placeholder link still lands on the register")
    check("[the page](../FRAGMENT-ISSUES.md)" in read(os.path.join(folder_of(root), "section-3.md")),
          "and so does a link written with the page's own name, read back as it was written")
    check("[the deep note](fragment-issues/section-1.md#a-deep-note)" in index,
          "a link from the page into a moved heading goes to its file")
    check("[archive](fragment-issues-archive/README.md)" in index, "a link that stays on the page is untouched")
    check("(../fragment-issues-archive/proving-defects-001-025.md#row-1)" in five
          and "[the tool](../../tools/x.py)" in five and "[row 3](../FRAGMENT-ISSUES.md#)" in five,
          "and a row's links are re-pointed the same way")

    print()
    print("5. The real readers answer exactly what they answered")
    now = readers_at(root)
    for key in sorted(today):
        check(now[key] == today[key], "%s is the same on the split register" % key)
    check("2, 4, 5" in now["open-defects"] and "5b-1" in now["open-defects"],
          "open-defects still lists the open rows of both sections, now read from their files")
    check(now["review-ledger"] == set([1, 2]), "review-ledger still finds 5b's rows")

    print()
    print("6. A second run moves nothing; a row past the last band is moved by the next")
    again = SPLIT.plan("fragment-issues", index_of(root), "2026-09-30", root)
    check(not again.moving and not again.banding and again.already == 4 and not again.problems,
          "nothing to move, four sections moved before")
    last = os.path.join(folder_of(root), "section-5b-rows-001-025.md")
    kept_first = read(os.path.join(folder_of(root), "section-5-rows-001-025.md"))
    put(last, read(last) + "| **26** | **Twenty-six.** Found 2026-09-24. | OPEN - new. |" + NL)
    check("| **26** | **Twenty-six.**" in whole(root), "a row written at the end of the last file is read at once")
    later = SPLIT.plan("fragment-issues", index_of(root), "2026-09-30", root)
    check(not later.problems and later.banding == [(S5B, ["section-5b-rows-001-025.md", "section-5b-rows-026-050.md"])],
          "the next run moves it to the file its number belongs to (%s)" % ("; ".join(later.problems) or later.banding))
    SPLIT.write(later)
    check("| **26** |" in read(os.path.join(folder_of(root), "section-5b-rows-026-050.md"))
          and "| **26** |" not in read(last), "it is there, and gone from the file it was written in")
    check("[026-050](fragment-issues/section-5b-rows-026-050.md)" in read(index_of(root)),
          "and the page names the new file")
    check(read(os.path.join(folder_of(root), "section-5-rows-001-025.md")) == kept_first,
          "every other file is kept as it was, its date included")
    check(whole(root).count("| **26** | **Twenty-six.**") == 1, "and the register reads the row once")

    # A ROW WRITTEN IN A ROWS FILE LINKS THE WAY THAT FILE SEES THINGS: another
    # band is a file beside it, the page is one folder up. Moved to its band, it
    # must reach the same files. On 2026-10-08 the real register's rows 201 to
    # 370 were re-banded and every link to a file beside them came out as
    # ../section-5b-rows-051-075.md - one folder too high, read back byte for
    # byte, and broken.
    put(last, read(last) + "| **27** | **Twenty-seven.** See [row 5b-1](section-5b-rows-001-025.md), "
        "[section 6](section-6.md) and [the page](../FRAGMENT-ISSUES.md). | OPEN - new. |" + NL)
    linked = SPLIT.plan("fragment-issues", index_of(root), "2026-09-30", root)
    check(not linked.problems, "a row with links beside it plans clean (%s)" % "; ".join(linked.problems))
    SPLIT.write(linked)
    band = read(os.path.join(folder_of(root), "section-5b-rows-026-050.md"))
    check("| **27** |" in band, "row 27 is moved to its band")
    for label, target in (("row 5b-1", "section-5b-rows-001-025.md"), ("section 6", "section-6.md"),
                          ("the page", "../FRAGMENT-ISSUES.md")):
        written = re.search(r"\[" + re.escape(label) + r"\]\(([^)#]*)", band)
        reached = written and os.path.isfile(os.path.join(folder_of(root), written.group(1)))
        check(written is not None and written.group(1) == target and reached,
              "its link to %s still reaches %s (written %s)"
              % (label, target, written.group(1) if written else "nowhere"))
    check(whole(root).count("| **27** | **Twenty-seven.**") == 1, "and the register reads it once")
    relinked_case(root)

    print()
    print("7. A missing file is broken, never shorter")
    gone = os.path.join(folder_of(root), "section-3.md")
    kept = read(gone)
    os.remove(gone)
    check(raises(lambda: whole(root)), "a file the page names and nobody kept raises RegisterBroken")
    check("Could not read the register" in readers_at_safe(root), "and open-defects says so rather than count less")
    put(gone, kept.replace("## 3. NEEDS THE OWNER", "## 3. RENAMED"))
    check(raises(lambda: whole(root)), "a file that no longer opens with its heading raises too")
    put(gone, kept)
    rows_file = os.path.join(folder_of(root), "section-5-rows-026-050.md")
    kept_rows = read(rows_file)
    put(rows_file, "# no table left" + NL)
    check(raises(lambda: whole(root)), "and a rows file with no table")
    put(rows_file, kept_rows)
    check(not raises(lambda: whole(root)), "put back, it reads whole again")

    print()
    print("8. archive-fragment-issues still moves a finished row on the split register")
    AF = _load("archive_fragment_issues", "archive-fragment-issues.py")
    check(AF is not None, "the archive tool loads")
    if AF is not None:
        archive_relinked_case(AF, root)
        archive = os.path.join(root, "docs", AF.ARCHIVE_NAME)
        plan = AF.plan(register=index_of(root), archive=archive, today="2026-09-23")
        check(plan.fatal is None and not plan.problems and sum(len(v) for v in plan.moves.values()) == 2,
              "it plans 3 and 5b-2, both finished (%s)" % (plan.fatal or "; ".join(plan.problems) or "clean"))
        before_index = read(index_of(root))
        check(AF.write(plan) == AF.OK, "it writes")
        five = read(os.path.join(folder_of(root), "section-5-rows-001-025.md"))
        check("](../fragment-issues-archive/proving-defects-001-025.md#row-3)" in five,
              "row 3's line in its rows file links to its full text, one folder up")
        check(read(index_of(root)) == before_index, "the page itself is untouched - row 3 was never on it")
        check("### Row 3" in read(os.path.join(archive, "proving-defects-001-025.md")),
              "and the full row is in the archive")
        check(whole(root) == plan.after, "read back, the register is what the archive tool planned")


def archive_relinked_case(AF, root):
    """Section 8, on a copy. The archive tool writes through the split's
    layout, which re-bands a row appended past the last band - and until
    2026-10-09 left every link naming that row at the file it had left
    (FRAGMENT-ISSUES row 5b-381). It follows the row now: in another row, in
    the archive file this run writes, and in a document outside the register."""
    copy = os.path.join(os.path.dirname(root), "archive-relinked")
    shutil.copytree(root, copy)
    folder = folder_of(copy)
    bands = sorted(n for n in os.listdir(folder) if re.match(r"^section-5b-rows-[0-9]{3}-[0-9]{3}[.]md$", n))
    last = bands[-1]
    high = int(last[len("section-5b-rows-"):-3].split("-")[1])
    row = high + 2
    beyond = "section-5b-rows-%03d-%03d.md" % (high + 1, high + 25)
    held = RT.rows_held(read(os.path.join(folder, last)))
    stays = next(n for n in range(high - 24, high + 1) if n not in held)
    put(os.path.join(folder, last), read(os.path.join(folder, last))
        + "| **%d** | **It names [row 5b-%d](%s).** Found 2026-10-09. | OPEN - new. |" % (stays, row, last) + NL
        + "| **%d** | **Appended past the last band.** Found 2026-10-09. | OPEN - new. |" % row + NL)
    first = os.path.join(folder, "section-5b-rows-001-025.md")
    put(first, read(first).replace("| **2** | **Two.** |",
                                   "| **2** | **Two.** It names [row 5b-%d](%s). |" % (row, last)))
    outside = os.path.join(copy, "docs", "ARCHIVE-LINKS.md")
    put(outside, "See [row 5b-%d](fragment-issues/%s)." % (row, last) + NL)

    archive = os.path.join(copy, "docs", AF.ARCHIVE_NAME)
    p = AF.plan(register=index_of(copy), archive=archive, today="2026-10-09")
    check(p.fatal is None and not p.problems, "the archive run plans clean with a row past the last band (%s)"
          % (p.fatal or "; ".join(p.problems) or "clean"))
    check(AF.write(p) == AF.OK, "and writes")
    check(os.path.exists(os.path.join(folder, beyond))
          and ("| **%d** |" % row) in read(os.path.join(folder, beyond))
          and ("| **%d** |" % row) not in read(os.path.join(folder, last)),
          "row 5b-%d is moved to %s" % (row, beyond))
    check("[row 5b-%d](%s)" % (row, beyond) in read(os.path.join(folder, last)),
          "a link to it in another row follows it to its band")
    check(read(outside) == "See [row 5b-%d](fragment-issues/%s)." % (row, beyond) + NL,
          "and so does a document outside the register")
    moved_to = [n for n in os.listdir(archive) if n.endswith(".md") and n != "README.md"
                and "It names [row 5b-%d]" % row in read(os.path.join(archive, n))]
    check(len(moved_to) == 1 and ("fragment-issues/%s)" % beyond) in read(os.path.join(archive, moved_to[0]))
          and ("fragment-issues/%s)" % last) not in read(os.path.join(archive, moved_to[0])),
          "and so does the link in the finished row the run moved to the archive (%s)"
          % (", ".join(moved_to) or "no archive file holds it"))
    check(whole(copy) == p.after, "read back, the register is what the archive tool planned, links and all")


def relinked_case(root):
    """Section 6, continued. A link whose words name one row the split moves,
    pointing at the file the row is leaving, follows the row: inside the
    register - on the page, in a section's file, in another row - and in every
    other document, as its own pass. On 2026-10-08 rows after 200 were
    re-banded and every such link was left at the file its row had left; a
    one-off script put them right (FRAGMENT-ISSUES row 5b-381)."""
    print()
    print("6b. A link naming a row the split moves follows the row, wherever it is written")
    folder = folder_of(root)
    old, new = "section-5b-rows-026-050.md", "section-5b-rows-051-075.md"
    last = os.path.join(folder, old)
    example = "`[row 5b-51](%s)`" % old
    put(last, read(last)
        + "| **28** | **Twenty-eight.** It names [row 5b-51](%s), and %s is an example. | OPEN - new. |" % (old, example)
        + NL + "| **51** | **Fifty-one.** Found 2026-10-09. | OPEN - new. |" + NL)
    six = os.path.join(folder, "section-6.md")
    put(six, read(six) + "See [row 5b-51](%s)." % old + NL)
    index = index_of(root)
    put(index, read(index).replace("> Intro for section 5b.",
                                   "> Intro for section 5b. See [row 5b-51](fragment-issues/%s)." % old))
    row_links = os.path.join(root, "docs", "ROW-LINKS.md")
    kept_lines = ["Not [rows 5b-51 and 5b-1](fragment-issues/%s), which names two rows." % old,
                  "Not `[row 5b-51](fragment-issues/%s)`, an example." % old,
                  FENCE + "text", "[row 5b-51](fragment-issues/%s)" % old, FENCE,
                  "And [row 5b-26](fragment-issues/%s), whose row stays." % old]
    wrapped = "A wrapped one, ([row" + NL + "5b-51](fragment-issues/%s)), follows as well."
    put(row_links, NL.join(["See [row 5b-51](fragment-issues/%s) and [5b-51 again](./fragment-issues/%s#top)."
                            % (old, old), wrapped % old] + kept_lines) + NL)
    a_py = os.path.join(root, "tools", "y.py")
    put(a_py, "# See [row 5b-51](../docs/fragment-issues/%s)." % old + NL)
    a_yaml = os.path.join(root, "docs", "notes.yaml")
    put(a_yaml, 'note: "[row 5b-51](fragment-issues/%s)"' % old + NL)
    elsewhere = os.path.join(root, "tools", "z.py")
    put(elsewhere, "# [row 5b-51](%s) is a file beside this one, not the register's." % old + NL)
    crlf = os.path.join(root, "docs", "CRLF.md")
    put(crlf, "See [row 5b-51](fragment-issues/%s).%sEnd.%s" % (old, CR + NL, CR + NL))

    p = SPLIT.plan("fragment-issues", index, "2026-10-09", root)
    check(not p.problems, "it plans clean (%s)" % ("; ".join(p.problems) or "no problem"))
    check(new in dict(p.banding).get(S5B, []), "row 51 is to move to %s" % new)
    inside = getattr(p, "relinked", None) or []
    outside = getattr(p, "others_relinked", None) or []
    check(len(inside) == 3, "three links inside the register follow it - on the page, in a section's file "
                            "and in another row - and the example does not (%d)" % len(inside))
    check(sorted(set(one[0] for one in outside)) == ["docs/CRLF.md", "docs/ROW-LINKS.md", "docs/notes.yaml",
                                                     "tools/y.py"],
          "and four documents outside it are to follow it (%s)"
          % (", ".join(sorted(set(one[0] for one in outside))) or "none"))
    check(SPLIT.write(p) == SPLIT.OK, "it writes")

    band = read(os.path.join(folder, new))
    check("| **51** |" in band and "| **51** |" not in read(last), "row 51 is in its band, and gone from %s" % old)
    rows = read(last)
    check("[row 5b-51](%s)" % new in rows, "a link to it in another row follows it to the file beside")
    check(example in rows, "an example in an inline code span is left as it was")
    check("See [row 5b-51](%s)." % new in read(six), "and in a section's file")
    check("See [row 5b-51](fragment-issues/%s)." % new in read(index), "and on the page")
    check(whole(root).count("| **51** | **Fifty-one.**") == 1, "the register reads row 51 once")
    linked = read(row_links)
    check("See [row 5b-51](fragment-issues/%s) and [5b-51 again](./fragment-issues/%s#top)." % (new, new) in linked,
          "a document elsewhere follows it, its ./ and its anchor kept")
    check(wrapped % new in linked, "and so does a link whose words a line break wraps")
    check(linked.endswith(NL.join(kept_lines) + NL),
          "and in it a link naming two rows, an example in code, fenced code and a row that stays are left")
    check(read(a_py) == "# See [row 5b-51](../docs/fragment-issues/%s)." % new + NL, "so does a .py")
    check(read(a_yaml) == 'note: "[row 5b-51](fragment-issues/%s)"' % new + NL, "and a .yaml")
    check(old in read(elsewhere), "a link that reaches a file of that name in another folder is left")
    check(read(crlf) == "See [row 5b-51](fragment-issues/%s).%sEnd.%s" % (new, CR + NL, CR + NL),
          "and a CRLF document follows it with its line endings kept")
    again = SPLIT.plan("fragment-issues", index, "2026-10-09", root)
    check(not again.banding and not (getattr(again, "relinked", None) or getattr(again, "others", None))
          and not again.problems, "a second run moves nothing and re-points nothing")

    # WHAT GIT TRACKS: a document git does not track is not the repository's,
    # and is left as it is. A copy, so the rest of this suite reads no git.
    gitted = os.path.join(os.path.dirname(root), "relinked-git")
    shutil.copytree(root, gitted)
    made = subprocess.run(["git", "init", "-q"], cwd=gitted, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    added = subprocess.run(["git", "add", "-A"], cwd=gitted, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    check(made.returncode == 0 and added.returncode == 0, "a copy is made a git tree")
    glast = os.path.join(folder_of(gitted), new)
    put(glast, read(glast) + "| **52** | **Fifty-two.** It names [row 5b-76](%s). | OPEN - new. |" % new + NL
        + "| **76** | **Seventy-six.** Found 2026-10-09. | OPEN - new. |" + NL)
    link = "[row 5b-76](fragment-issues/%s)" % new
    tracked = os.path.join(gitted, "docs", "ROW-LINKS.md")
    put(tracked, read(tracked) + link + NL)
    untracked = os.path.join(gitted, "docs", "UNTRACKED.md")
    put(untracked, link + NL)

    # THE READ-BACK STILL GUARDS. The register is compared with the text the
    # links were re-pointed in, so a re-pointing that changed anything else
    # would read back "byte for byte" - and is caught by the second half: the
    # text with every rows file's name left out must be the register as it was.
    real = getattr(SPLIT, "relink", None)
    check(real is not None, "the split tool has relink(), the one rule every link follows by")
    if real is not None:
        def careless(text, where, folder, moved, said, done):
            out = real(text, where, folder, moved, said, done)
            return out.replace("Fifty-two", "Fifty-TWO") if said == "the register" else out
        SPLIT.relink = careless
        try:
            bad = SPLIT.plan("fragment-issues", index_of(gitted), "2026-10-09", gitted)
        finally:
            SPLIT.relink = real
        check(any("more than the rows file a link names" in x for x in bad.problems)
              and SPLIT.write(bad) == SPLIT.REFUSED and "| **76** |" in read(glast),
              "a re-pointing that changes anything else is refused, and nothing is written (%s)"
              % ("; ".join(bad.problems) or "no problem"))
    q = SPLIT.plan("fragment-issues", index_of(gitted), "2026-10-09", gitted)
    check(not q.problems and SPLIT.write(q) == SPLIT.OK, "row 76 plans clean and is written (%s)"
          % ("; ".join(q.problems) or "no problem"))
    check("[row 5b-76](section-5b-rows-076-100.md)" in read(glast), "row 52's link follows row 76 to its band")
    check(read(tracked).endswith("[row 5b-76](fragment-issues/section-5b-rows-076-100.md)" + NL),
          "a document git tracks follows row 76 to its band")
    check(read(untracked) == link + NL, "one git does not track is left as it was")


def readers_at_safe(root):
    od = _load("open_defects_broken", "open-defects.py")
    od.REGISTER = index_of(root)
    buffer = io.StringIO()
    with contextlib.redirect_stdout(buffer):
        od.main()
    return buffer.getvalue()


def run(work):
    check(RT is not None, "tools/register-text.py exists and loads")
    check(SPLIT is not None, "tools/split-register.py exists and loads")
    for name in ("plan", "write", "layout_split", "REGISTERS"):
        check(SPLIT is not None and getattr(SPLIT, name, None) is not None, "the split tool has %s" % name)
    for name in ("register_text", "RegisterBroken", "file_line", "rows_line"):
        check(RT is not None and getattr(RT, name, None) is not None, "the reader has %s" % name)
    if FAILURES:
        return

    original = register_text()
    one_file = make_root(work, original, "whole")
    root = make_root(work, original)
    today = readers_at(one_file)
    check(whole(one_file) == original, "a register that names no file is read as it is")

    print()
    print("1. What moves, and what stays")
    p = SPLIT.plan("fragment-issues", index_of(root), "2026-09-23", root)
    moving = dict((h, n) for h, n in p.moving)
    staying = dict(p.staying)
    check(p.fatal is None and not p.problems, "the plan is clean (%s)" % (p.fatal or "; ".join(p.problems) or "no problem"))
    check(moving.get("## 1. SUSPECT " + DASH + " these upset Revit") == "section-1.md",
          "a numbered section moves to a file named from its number")
    check(moving.get("## Tag leaders, 2026-09-21 " + DASH + " five things measured") == "tag-leaders-2026-09-21.md",
          "any other is named from its heading's opening words and date")
    check("already holds another section" in staying.get("## Tag leaders, 2026-09-21 " + DASH + " measured again", ""),
          "a section whose name another has taken stays, and says why")
    check("rules" in staying.get("## Add to this file, do not start another", ""), "the page's own rules stay")
    check(not any("shell comment" in h for h in list(moving) + list(staying)),
          "a '## ' line inside a fence is an example, not a section")

    print()
    print("2. Sections 5 and 5b move their rows, 25 to a file")
    check(dict(p.banding).get(S5) == ["section-5-rows-001-025.md", "section-5-rows-026-050.md"],
          "section 5's 27 rows go to two files")
    check(dict(p.banding).get(S5B) == ["section-5b-rows-001-025.md"], "section 5b's two rows to one")
    check(S5 not in moving and S5B not in moving, "and neither section leaves the page")

    print()
    print("3. Written, and read back, the files ARE the register")
    written = SPLIT.write(p) == SPLIT.OK and os.path.isdir(folder_of(root))
    check(written, "it writes")
    if written:
        after_write(original, root, today)
    else:
        print("  FAIL  sections 4 to 8 need the split written, and it was not - not run")
        FAILURES.append("sections 4 to 8 not run")

    print()
    print("9. A link from elsewhere into a heading that would leave the page stops the run")
    guarded = make_root(work, original, "guarded")
    put(os.path.join(guarded, "docs", "OTHER.md"), "See [the note](FRAGMENT-ISSUES.md#a-deep-note)." + NL)
    stopped = SPLIT.plan("fragment-issues", index_of(guarded), "2026-09-23", guarded)
    check(any("a heading that would leave the page" in x for x in stopped.problems), "it is refused, and says which link")
    check(SPLIT.write(stopped) == SPLIT.REFUSED and not os.path.isdir(folder_of(guarded)), "and nothing is written")

    print()
    print("10. CRLF is kept, and still reads back byte for byte")
    crlf_text = original.replace(NL, CR + NL)
    crlf = make_root(work, crlf_text, "crlf")
    q = SPLIT.plan("fragment-issues", index_of(crlf), "2026-09-23", crlf)
    wrote = SPLIT.write(q) == SPLIT.OK and os.path.isdir(folder_of(crlf))
    check(wrote and (CR + NL) in read(os.path.join(folder_of(crlf), "section-5-rows-026-050.md")),
          "the files are written CRLF")
    check(wrote and whole(crlf) == crlf_text, "and the register reads back as it was, CRLF included")
    mixed = make_root(work, original.replace(NL, CR + NL, 3), "mixed")
    r = SPLIT.plan("fragment-issues", index_of(mixed), "2026-09-23", mixed)
    check(r.fatal is not None and SPLIT.write(r) == SPLIT.COULD_NOT and not os.path.isdir(folder_of(mixed)),
          "a register mixing CRLF and LF is not split")

    print()
    print("11. Every reader reads the register through the one reader")
    for filename in ("open-defects.py", "review-ledger.py", "archive-fragment-issues.py"):
        source = read(os.path.join(ROOT, "tools", filename))
        check("register-text.py" in source or "OD.RT" in source,
              "%s reads it through tools/register-text.py" % filename)
        check("register_text(" in source, "and calls register_text()")

    print()
    print("12. PROPOSALS.md splits the same way, and its readers read the same proposals")
    proposals_case(work)

    print()
    print("13. OPEN-QUESTIONS.md splits the same way, and its readers count the same questions")
    questions_case(work)

    print()
    print("14. A link to a 5b row past 999 does not stop the split")
    four_digit_case(work)

    print()
    print("15. A row from the cut-over on has a file of its own")
    own_file_case(work)


def four_digit_case(work):
    """Row 5b-1169. Rows 1001 to 1025 go to a file named with FOUR digits, and
    the read-back comparison knew a rows file only by three - so once any row
    of the register linked to a 5b row numbered 1000 or more, the split
    refused: *"the new files, read back, differ from the register in more
    than the rows file a link names"*. Measured on the real register,
    2026-10-18. Every check below is written to FAIL on that code, not to
    raise: a file the refused split never wrote is asked for before it is
    read."""
    root = make_root(work, register_text(), "thousand")
    first = SPLIT.plan("fragment-issues", index_of(root), "2026-09-23", root)
    if SPLIT.write(first) != SPLIT.OK:
        check(False, "the small register splits before the four-digit row is written (%s)"
              % (first.fatal or "; ".join(first.problems)))
        return
    folder = folder_of(root)
    old, new = "section-5b-rows-001-025.md", "section-5b-rows-1001-1025.md"
    last = os.path.join(folder, old)
    put(last, read(last)
        + "| **3** | **Three.** It names [row 5b-1001](%s). | OPEN - new. |" % old + NL
        + "| **1001** | **One thousand and one.** Found 2026-10-18. | OPEN - new. |" + NL)
    before = whole(root)

    p = SPLIT.plan("fragment-issues", index_of(root), "2026-10-18", root)
    check(p.fatal is None and not p.problems,
          "a register whose row links to a four-digit row plans clean (%s)"
          % (p.fatal or "; ".join(p.problems) or "no problem"))
    check(p.banding == [(S5B, [old, new])],
          "row 1001 is to move to the file its number belongs to (%s)" % p.banding)
    check(SPLIT.write(p) == SPLIT.OK, "and the split is written, not refused")

    moved_to = os.path.join(folder, new)
    held = read(moved_to) if os.path.isfile(moved_to) else ""
    check("| **1001** | **One thousand and one.**" in held,
          "row 1001 is in %s" % new)
    check("| **1001** |" not in read(last), "and gone from the file it was written in")
    check("[row 5b-1001](%s)" % new in read(last),
          "the link that names it follows it to the four-digit file")
    check("[1001-1025](fragment-issues/%s)" % new in read(index_of(root)),
          "and the page names the new file")
    try:
        after = whole(root)
    except RT.RegisterBroken as why:
        after = "broken: %s" % why
    # Read as one text the link is written from the page, one folder up.
    check(after == before.replace("[row 5b-1001](fragment-issues/%s)" % old,
                                  "[row 5b-1001](fragment-issues/%s)" % new),
          "the register reads back as it was, the one re-pointed link aside")
    check(after.count("| **1001** | **One thousand and one.**") == 1, "and reads row 1001 once")
    again = SPLIT.plan("fragment-issues", index_of(root), "2026-10-19", root)
    check(again.fatal is None and not again.problems and not again.banding and not again.moving,
          "a second run moves nothing (%s)" % (again.fatal or "; ".join(again.problems) or again.banding))

    blind = getattr(SPLIT, "_band_blind", None)
    check(blind is not None, "the split tool still has the comparison archive-fragment-issues.py asks")
    if blind is not None:
        config = SPLIT.REGISTERS["fragment-issues"]
        check(blind("see (%s)" % new, config) == blind("see (%s)" % old, config),
              "which reads a four-digit rows file as it reads a three-digit one")
        check(blind("see (section-5b-rows-01-25.md)", config) != blind("see (%s)" % old, config),
              "and still takes nothing shorter than three digits for a rows file")


RED, YELLOW, DONE = chr(0x1F534), chr(0x1F7E1), chr(0x2705)


EACH_LINE = "**Its rows from 3 on, one file each:** [`fragment-issues/row-5b-NNNN.md`](fragment-issues/)"


def own_file(number, title, state):
    """A row's own file, written the way a person writes one: a heading that
    names the row, the table's two header lines and the row's line."""
    return NL.join([
        "# Fragment issues %s row 5b-%d" % (DASH, number),
        "",
        "> One row of section 5b of [the register](../FRAGMENT-ISSUES.md), in a file of its own.",
        "",
        "| # | Defect | State |",
        "|---|---|---|",
        "| **%d** | **%s** | %s |" % (number, title, state),
        "",
    ])


def new_rows_plan(root, today):
    """The split's plan for only the new rows left in a rows file - or None
    where the tool has no such plan, so its absence is a failed check and
    not a traceback."""
    try:
        return SPLIT.plan("fragment-issues", index_of(root), today, root, new_rows=True)
    except TypeError:
        return None


def own_file_case(work):
    """Since 2026-11-12 a row from the cut-over on has a file of its own,
    because every new row used to be appended to the end of ONE rows file and
    two branches that each added a row stopped at each other in every merge.
    The page says where the change is with one line under its rows line, and
    names no row's file: the folder is the list. Every check below is written
    to FAIL on the code as it stood, not to raise - there the line is prose,
    a row's own file is read by nothing, and the split has no plan for one."""
    root = make_root(work, register_text(), "own")
    first = SPLIT.plan("fragment-issues", index_of(root), "2026-09-23", root)
    if SPLIT.write(first) != SPLIT.OK:
        check(False, "the small register splits before a row gets its own file (%s)"
              % (first.fatal or "; ".join(first.problems)))
        return
    folder = folder_of(root)
    band = "section-5b-rows-001-025.md"
    page = read(index_of(root))
    rows_line = next(line for line in page.split(NL) if line.startswith(RT.ROWS_KEY) and band in line)
    before = whole(root)
    put(index_of(root), page.replace(rows_line, rows_line + NL + NL + EACH_LINE))
    check(whole(root) == before,
          "the page's line for rows from 3 on is the reader's, not the register's: read back, nothing changed")

    # Written 4 first and 3 second, the way two sessions would.
    put(os.path.join(folder, "row-5b-4.md"), own_file(4, "Four.** A long account of it, which the archive keeps.**",
                                                     "FIXED 2026-11-12 - it was."))
    put(os.path.join(folder, "row-5b-3.md"),
        own_file(3, "Three.** It names [row 5b-1](%s) and [the tool](../../tools/x.py).**" % band,
                 "OPEN - recorded, not repaired."))
    text = whole(root)
    table = text[text.index(S5B):text.index("*No count is typed here.*")]
    rows = [int(m.group(1)) for m in map(RT.ROW.match, table.split(NL)) if m]
    check(rows == [1, 2, 3, 4], "rows 3 and 4 are read from their own files, after the rows files' rows and "
                                "in number order whichever was written first (%s)" % rows)
    check((NL + NL + "| **3**") not in table and (NL + NL + "| **4**") not in table
          and table.count("| # | Defect | State |") == 1,
          "on the one table: no blank line before them, and its header once")
    check("[row 5b-1](fragment-issues/%s)" % band in table and "[the tool](../tools/x.py)" in table,
          "a row's own file is one folder down, and its links are read as the page would write them")
    check("5b-3 " in readers_at_safe(root), "open-defects.py lists 5b-3, open, with no change to open-defects.py")
    full = SPLIT.plan("fragment-issues", index_of(root), "2026-11-12", root)
    check(full.fatal is None and not full.problems and not full.banding and not full.moving
          and full.after == full.before,
          "the whole split leaves the rows' own files, and the page's line, as they are (%s)"
          % (full.fatal or "; ".join(full.problems) or full.banding or
             ("nothing to move" if full.after == full.before else "the page would change")))

    print()
    print("   a new row an older branch still appended to the rows file")
    outside = os.path.join(root, "docs", "OWN-LINKS.md")
    put(outside, "See [row 5b-5](fragment-issues/%s), and [row 5b-1](fragment-issues/%s)." % (band, band) + NL)
    three = os.path.join(folder, "row-5b-3.md")
    put(three, read(three).replace("Three.**", "Three.** After [row 5b-5](%s)." % band))
    put(os.path.join(folder, band), read(os.path.join(folder, band))
        + "| **5** | **Five, written the old way.** | OPEN - new. |" + NL)
    left = whole(root)
    table = left[left.index(S5B):left.index("*No count is typed here.*")]
    check([int(m.group(1)) for m in map(RT.ROW.match, table.split(NL)) if m] == [1, 2, 3, 4, 5],
          "it is read in number order with the rows that have files, wherever it is kept")
    kept = dict((n, read(os.path.join(folder, n))) for n in sorted(os.listdir(folder)))
    kept_page = read(index_of(root))
    p = new_rows_plan(root, "2026-11-12")
    check(p is not None and p.fatal is None and not p.problems,
          "the split plans the move of that row alone (%s)"
          % ("it has no such plan" if p is None else p.fatal or "; ".join(p.problems) or "clean"))
    check(p is not None and p.moved.get("5b") == {5: (band, "row-5b-5.md")},
          "row 5b-5 goes from the rows file to row-5b-5.md, and no other row moves")
    check(p is not None and SPLIT.write(p) == SPLIT.OK, "and it is written")
    five = os.path.join(folder, "row-5b-5.md")
    check(os.path.isfile(five) and "| **5** | **Five, written the old way.** | OPEN - new. |" in read(five)
          and read(five).startswith("# Fragment issues %s row 5b-5" % DASH),
          "row-5b-5.md holds the row's line as it was written, under a heading that names it")
    check("| **5** |" not in read(os.path.join(folder, band)), "and the rows file no longer holds it")
    now = whole(root) if os.path.isfile(five) else ""
    check(now == left.replace("[row 5b-5](fragment-issues/%s)" % band, "[row 5b-5](fragment-issues/row-5b-5.md)"),
          "the register reads back as it did, the links that follow the row aside")
    check("[row 5b-5](row-5b-5.md)" in read(three) and "[row 5b-1](%s)" % band in read(three),
          "a link to it in another row's own file follows it, and a link to a row that stayed does not")
    check(read(outside) == "See [row 5b-5](fragment-issues/row-5b-5.md), and [row 5b-1](fragment-issues/%s)."
          % band + NL, "and so does a document outside the register")
    unchanged = [n for n in kept if n not in (band, "row-5b-3.md")]
    check(os.path.isfile(five) and read(index_of(root)) == kept_page
          and all(read(os.path.join(folder, n)) == kept[n] for n in unchanged),
          "the page and every other file are byte for byte what they were: nothing was re-banded")
    again = new_rows_plan(root, "2026-11-13")
    check(again is not None and again.fatal is None and not again.problems and not any(again.moved.values())
          and again.after == again.before, "a second run moves nothing")

    print()
    print("   one number, in a rows file and in a file of its own")
    clash = os.path.join(work, "own-clash")
    shutil.copytree(root, clash)
    cfolder = folder_of(clash)
    put(os.path.join(cfolder, band), read(os.path.join(cfolder, band))
        + "| **4** | **Another four, from another branch.** | OPEN - new. |" + NL)
    twice = whole(clash)
    twice = twice[twice.index(S5B):twice.index("*No count is typed here.*")]
    check(twice.count(NL + "| **4** |") == 2,
          "both rows are read - which one is row 4 is not for a reader to choose, and "
          "tests/test_open_defects.py refuses the pair")
    held = dict((n, read(os.path.join(cfolder, n))) for n in sorted(os.listdir(cfolder)))
    c = new_rows_plan(clash, "2026-11-12")
    check(c is not None and any("5b-4" in problem and "row-5b-4.md" in problem for problem in c.problems),
          "the split refuses to put the second one in row-5b-4.md, and names the row (%s)"
          % ("it has no such plan" if c is None else "; ".join(c.problems) or "no problem"))
    check(c is not None and SPLIT.write(c) == SPLIT.REFUSED
          and all(read(os.path.join(cfolder, n)) == held[n] for n in held), "and nothing is written")

    print()
    print("   a file that is not what its name says is broken, never skipped")
    stray = os.path.join(folder, "row-5b-4-copy.md")
    put(stray, own_file(6, "Six.", "OPEN - new."))
    check(raises(lambda: whole(root)), "a file named like a row's own that is not one raises RegisterBroken")
    check("Could not read the register" in readers_at_safe(root), "and open-defects says so rather than count less")
    os.remove(stray)
    wrong = os.path.join(folder, "row-5b-6.md")
    put(wrong, own_file(7, "Seven.", "OPEN - new."))
    check(raises(lambda: whole(root)), "a row's file that holds another row than its name says raises too")
    put(wrong, own_file(6, "Six.", "OPEN - new.").rstrip(NL) + NL
        + "| **7** | **Seven.** | OPEN - new. |" + NL)
    check(raises(lambda: whole(root)), "and one that holds a second row beside its own")
    os.remove(wrong)
    check(not raises(lambda: whole(root)), "taken away, it reads whole again")
    served = dict((os.path.join(folder, n).replace(os.sep, "/"), read(os.path.join(folder, n)))
                  for n in os.listdir(folder))
    served[index_of(root).replace(os.sep, "/")] = read(index_of(root))
    check(raises(lambda: RT.register_text(index_of(root), read=lambda path: served.get(path.replace(os.sep, "/")))),
          "a caller that serves the register from memory and cannot list the folder is told, not given less")

    print()
    print("   archive-fragment-issues moves a finished row that has a file of its own")
    AF = _load("archive_fragment_issues_own", "archive-fragment-issues.py")
    if AF is None:
        check(False, "the archive tool loads")
        return
    archive = os.path.join(root, "docs", AF.ARCHIVE_NAME)
    plan = AF.plan(register=index_of(root), archive=archive, today="2026-11-12")
    idents = [ident for moved in plan.moves.values() for _, ident, _ in moved]
    check(plan.fatal is None and not plan.problems and "5b-4" in idents,
          "it plans 5b-4, finished, from its own file (%s)" % (plan.fatal or "; ".join(plan.problems) or "clean"))
    check(AF.write(plan) == AF.OK, "it writes")
    four = read(os.path.join(folder, "row-5b-4.md"))
    check("| **4** |" in four and "](../fragment-issues-archive/" in four and "#row-5b-4)" in four
          and "A long account" not in four,
          "row 5b-4's line stays in row-5b-4.md, shortened, with the link to its full text")
    check(four.startswith("# Fragment issues %s row 5b-4" % DASH), "under the heading the file had")
    check(not any(n.startswith("section-5b-rows-") and "| **4** |" in read(os.path.join(folder, n))
                  for n in os.listdir(folder)), "and no rows file has taken it")
    check(whole(root) == plan.after, "read back, the register is what the archive tool planned")



def proposals_register():
    return NL.join([
        "# Proposals " + DASH + " Gaps, Additions & Ideas",
        "",
        "> | **Its numbers** | Derive the open ones: `python tools/owner-queue.py` |",
        "",
        "---",
        "",
        "## Part A " + DASH + " Gaps in Master Specification Part 1",
        "",
        "### " + RED + " A1. The threading constraint is not mentioned",
        "",
        "See [the first of Part F](#f1-should-it-be-a-tool).",
        "",
        "## Part F " + DASH + " Opened by building the Improvement Gate, 2026-09-12",
        "",
        "### " + YELLOW + " F1. Should it be a tool?",
        "",
        "Words, and [a decision](DECISIONS.md).",
        "",
        "### " + DONE + " F3. Closed already",
        "",
        "## F23 " + DASH + " four agents have no distinct source",
        "",
        "### " + YELLOW + " F23. The same, as a heading",
        "",
        "See [F27](#f27--five-rows-are-built).",
        "",
        "## F27 " + DASH + " five rows are built",
        "",
        "Built.",
        "",
        "## 2026-09-19 " + DASH + " what is left after twenty-three gaps were closed",
        "",
        "Left.",
        "",
        "## 2026-09-19 " + DASH + " rule (d) refuses four fragments, and only two deserve it",
        "",
        "Refused.",
        "",
    ])


def proposals_readers_at(root):
    oq = _load("owner_queue_at_root", "owner-queue.py")
    bw = _load("balance_of_work_at_root", "balance-of-work.py")
    oq.ROOT = root
    bw.ROOT = root
    return {"owner-queue": oq.proposals(), "balance-of-work": bw.proposals_open()}


def proposals_case(work):
    original = proposals_register()
    one_file = make_root(work, original, "proposals-whole")
    root = make_root(work, original, "proposals")
    for base in (one_file, root):
        put(os.path.join(base, "docs", "PROPOSALS.md"), original)
    index = os.path.join(root, "docs", "PROPOSALS.md")
    folder = os.path.join(root, "docs", "proposals")
    today = proposals_readers_at(one_file)
    p = SPLIT.plan("proposals", index, "2026-09-23", root)
    check(p.fatal is None and not p.problems, "the plan is clean (%s)" % (p.fatal or "; ".join(p.problems) or "no problem"))
    check(sorted(n for _, n in p.moving) == ["2026-09-19-rule-d-refuses-four-fragments.md",
                                            "2026-09-19-what-is-left-after-twenty-three.md",
                                            "f23.md", "f27.md", "part-a.md", "part-f.md"],
          "every section moves; one named from its opening words, and two written on one day to two files")
    check(SPLIT.write(p) == SPLIT.OK, "it writes")
    check(RT.register_text(index) == original, "read back, it is the register byte for byte")
    part_a = read(os.path.join(folder, "part-a.md"))
    check("[the first of Part F](part-f.md#f1-should-it-be-a-tool)" in part_a,
          "a link to a proposal in another section follows it")
    check("[F27](f27.md#f27--five-rows-are-built)" in read(os.path.join(folder, "f23.md")),
          "and so does one to another section's own heading")
    check("[a decision](../DECISIONS.md)" in read(os.path.join(folder, "part-f.md")),
          "a link one folder deeper: DECISIONS.md -> ../DECISIONS.md")
    now = proposals_readers_at(root)
    for key in sorted(today):
        check(now[key] == today[key], "%s reads the same proposals from the split register" % key)
    check([r[0] for r in now["owner-queue"]] == ["F1", "F23"],
          "owner-queue still puts F1 and F23 in front of the owner, and not the closed F3")
    for filename in ("owner-queue.py", "balance-of-work.py"):
        source = read(os.path.join(ROOT, "tools", filename))
        check("register-text.py" in source and "proposals_text()" in source,
              "%s reads PROPOSALS.md through tools/register-text.py" % filename)


def questions_register():
    orange, red = chr(0x1F7E0), chr(0x1F534)
    return NL.join([
        "# Open Questions",
        "",
        "> | **Its numbers** | `python tools/check-docs.py` derives answered-vs-open |",
        "",
        "**Progress: 3 answered " + chr(0xB7) + " 1 open " + chr(0xB7) + " nothing blocking any phase**",
        "",
        "Start at [the second](#q-2--is-it-answered), and see [the page](OPEN-QUESTIONS.md).",
        "",
        "---",
        "",
        "## Tier 1 " + DASH + " Blocking",
        "",
        "### " + red + " Q-1 " + DASH + " Where does it run?",
        "",
        "**Answer:** On the PC, as [D-01](DECISIONS.md) says.",
        "",
        "---",
        "",
        "## Tier 2 " + DASH + " Blocks a major area",
        "",
        "### " + orange + " Q-2 " + DASH + " Is it answered?",
        "",
        "**Answer:** Yes, in full.",
        "",
        "---",
        "",
        "### " + orange + " Q-3 " + DASH + " Is this one open?",
        "",
        "**Answer:**",
        "",
        "---",
        "",
        "## Answered",
        "",
        "### " + DONE + " Q-4 " + DASH + " Closed long ago",
        "",
        "Closed, and [Q-1](#q-1--where-does-it-run) with it.",
        "",
    ])


def questions_readers_at(root):
    oq = _load("owner_queue_questions_at_root", "owner-queue.py")
    oq.ROOT = root
    run = subprocess.run([sys.executable, os.path.join(ROOT, "tools", "check-docs.py")], cwd=root,
                         stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    text = run.stdout.decode("utf-8", "replace")
    four = text[text.find("=== 4. QUESTIONS ==="):text.find("=== 5.")]
    six = text[text.find("=== 6. THE PROGRESS LINE ==="):text.find("=== 7.")]
    return {"owner-queue": oq.open_questions(), "check-docs, its questions": four,
            "check-docs, its Progress line": six}


def questions_case(work):
    original = questions_register()
    roots = []
    for name in ("questions-whole", "questions"):
        root = make_root(work, "", name)
        os.remove(index_of(root))
        put(os.path.join(root, "docs", "OPEN-QUESTIONS.md"), original)
        roots.append(root)
    one_file, root = roots
    index = os.path.join(root, "docs", "OPEN-QUESTIONS.md")
    folder = os.path.join(root, "docs", "open-questions")
    today = questions_readers_at(one_file)
    check("ACTUAL: 3 answered, 1 open" in today["check-docs, its Progress line"]
          and "Q-4" in today["check-docs, its questions"],
          "check-docs counts this register's questions as one file (%s)"
          % today["check-docs, its Progress line"].strip().replace(NL, " / "))
    p = SPLIT.plan("open-questions", index, "2026-09-23", root)
    check(p.fatal is None and not p.problems, "the plan is clean (%s)" % (p.fatal or "; ".join(p.problems) or "no problem"))
    check(sorted(n for _, n in p.moving) == ["answered.md", "tier-1.md", "tier-2.md"], "one file per tier")
    check(SPLIT.write(p) == SPLIT.OK, "it writes")
    page = read(index)
    check(RT.register_text(index) == original, "read back, it is the register byte for byte")
    check("**Progress: 3 answered" in page and "### " not in page,
          "the Progress line stays on the page, and no question does")
    check("[the second](open-questions/tier-2.md#q-2--is-it-answered)" in page,
          "a link from the page to a question goes to its tier's file")
    check("[the page](OPEN-QUESTIONS.md)" in page, "and a link to the page itself is untouched")
    check("[D-01](../DECISIONS.md)" in read(os.path.join(folder, "tier-1.md")),
          "a link one folder deeper: DECISIONS.md -> ../DECISIONS.md")
    check("[Q-1](tier-1.md#q-1--where-does-it-run)" in read(os.path.join(folder, "answered.md")),
          "a link to a question in another tier follows it")
    now = questions_readers_at(root)
    for key in sorted(today):
        check(now[key] == today[key], "%s is the same on the split register" % key)
    check([r[0] for r in now["owner-queue"]] == ["Q-3"], "owner-queue still lists Q-3, the one open question")
    for filename in ("check-docs.py", "owner-queue.py"):
        source = read(os.path.join(ROOT, "tools", filename))
        check("register-text.py" in source and "OPEN-QUESTIONS.md" in source,
              "%s reads OPEN-QUESTIONS.md through tools/register-text.py" % filename)


if __name__ == "__main__":
    sys.exit(main())
