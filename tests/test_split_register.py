# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/split-register.py gives every section of FRAGMENT-ISSUES.md its own file
and the rows of sections 5 and 5b files of 25, and every reader of the
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
     read the register through the one reader.
"""

import contextlib
import importlib.util
import io
import os
import shutil
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
        shutil.rmtree(work)
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


if __name__ == "__main__":
    sys.exit(main())
