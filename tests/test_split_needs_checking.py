# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/split-needs-checking.py gives every group of NEEDS-CHECKING.md its own
file, and every reader of the register still reads exactly what it read.

    python tests/test_split_needs_checking.py

Every case runs against a SMALL register BUILT HERE, never the real one, which
changes every day and would fail this suite for the tool being right.

WHAT IS PROVED
--------------
  1. a `## Group X` section moves to group-x.md, and so does a section whose
     rows are all one group; the page's rules, a section with rows of two
     groups, and one with none stay - each for its stated reason;
  2. read back through tools/needs-checking-register.py, the files ARE the
     register, byte for byte - LF and CRLF alike;
  3. every link is re-pointed to reach what it reached: one folder deeper, a
     link to a heading that moved follows it, a link from the page into a
     moved heading goes to its file;
  4. THE REAL READERS - owner-queue, check-gaps (its waiting rows too),
     balance-of-work, and check-docs' B8 - report exactly the same on the
     split register as on the one file, run against this suite's register;
  5. a second run moves nothing, and a group written into the page later is
     moved by the next run with every existing file's opening kept;
  6. a missing group file, or one that no longer opens with its heading, is
     RegisterBroken - never a shorter register - and the readers say so;
  7. archive-needs-checking.py still moves a done row on the split register:
     the full row to the archive, its line into its group's file;
  8. it refuses, writing nothing, when another document links to a heading
     that would move;
  9. CRLF in, CRLF out, and still byte for byte;
 10. check-docs.py, owner-queue.py, check-gaps.py, balance-of-work.py and
     archive-needs-checking.py each read the register through the one reader.
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


NCR = _load("needs_checking_register", "needs-checking-register.py")
SPLIT = _load("split_needs_checking", "split-needs-checking.py")

NL = chr(10)
CR = chr(13)
DASH = chr(0x2014)
FENCE = "`" * 3
FAILURES = []


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILURES.append(said)


def register_text():
    return NL.join([
        "# Needs checking " + DASH + " the register",
        "",
        "> | **Its numbers** | see [the first group](#group-a--it-builds) and [a decision](DECISIONS.md) |",
        "",
        "## How this file works",
        "",
        "The rules. The note that matters is [the deep one](#a-deep-note).",
        "",
        FENCE + "markdown",
        "## D-NN " + DASH + " a template, not a section",
        FENCE,
        "",
        "## Group A " + DASH + " it builds",
        "",
        "| # | Check | Result |",
        "|---|---|---|",
        "| **A1** | Build it - see [D-01](DECISIONS.md) | open |",
        "| ~~**A2**~~ | ~~Done~~ | **PASSED 2026-09-01** - three runs, all green |",
        "",
        "### A deep note",
        "",
        "It points at [Group B](#group-b--revit-loads), at [a tool](../tools/x.py) and at [a row](#).",
        "",
        "---",
        "",
        "## Group B " + DASH + " Revit loads",
        "",
        "| # | Check | Result |",
        "|---|---|---|",
        "| **B1** | Load it | open |",
        "| ~~**B8**~~ | ~~Move three ducts~~ | **PASSED 2026-09-07** - three ducts moved 200 mm |",
        "",
        "## 2026-09-21 " + DASH + " a dated stage whose rows are one group",
        "",
        "| **AC1** | Install it | open |",
        "| **AC2** | Run it | open |",
        "",
        "## 2026-09-22 " + DASH + " two groups at once",
        "",
        "| **AA11** | One | open |",
        "| **AB9** | Two | open |",
        "",
        "## Group AA - two letters",
        "",
        "| **AA1** | Three | open |",
        "",
    ])


def make_root(parent, text, name="one"):
    root = os.path.join(parent, name)
    os.makedirs(os.path.join(root, "docs"))
    os.makedirs(os.path.join(root, "tools"))
    for filename, body in (("docs/NEEDS-CHECKING.md", text), ("docs/DECISIONS.md", "# D" + NL),
                           ("tools/x.py", "# x" + NL)):
        with io.open(os.path.join(root, *filename.split("/")), "w", encoding="utf-8", newline="") as handle:
            handle.write(body)
    return root


def index_of(root):
    return os.path.join(root, "docs", "NEEDS-CHECKING.md")


def folder_of(root):
    return os.path.join(root, "docs", "needs-checking")


def read(path):
    with io.open(path, "rb") as handle:
        return handle.read().decode("utf-8")


def readers_at(root):
    """What each real reader reports, reading the register at ROOT."""
    out = {}
    for key, filename, call in (("owner-queue", "owner-queue.py", "needs_checking"),
                                ("check-gaps", "check-gaps.py", "check_register"),
                                ("balance-of-work", "balance-of-work.py", "register_rows")):
        module = _load(key.replace("-", "_") + "_at_root", filename)
        if module is None:
            out[key] = "could not load"
            continue
        module.ROOT = root
        buffer = io.StringIO()
        try:
            with contextlib.redirect_stdout(buffer):
                result = getattr(module, call)()
        # Each tool loads its OWN copy of the reader, so its RegisterBroken is
        # its own class - this suite's copy would never match it.
        except module.NCR.RegisterBroken as error:
            result = "raised RegisterBroken: %s" % error
        out[key] = (result, buffer.getvalue(), list(getattr(module, "WAITING", [])))
    try:
        text = NCR.register_text(root) or ""
    except NCR.RegisterBroken:
        text = ""
    b8 = SPLIT.B8.search(text)
    passed = SPLIT.PASSED.search(b8.group(0)) if b8 else None
    out["check-docs B8"] = passed.group(1) if passed else None
    return out


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
    print("PASSED  one file per group, and the register reads as one")
    return 0


def after_write(work, original, root, today):
    """Sections 2 to 7: they read what the split wrote, so they run only when it wrote."""
    names = sorted(os.listdir(folder_of(root)))
    check(names == ["group-a.md", "group-aa.md", "group-ac.md", "group-b.md"], "one file per group: %s" % ", ".join(names))
    index = read(index_of(root))
    check(NCR.stub_line("group-a.md") in index and "| **A1** |" not in index,
          "the page keeps the heading and a line naming the file, not the rows")
    check("## How this file works" in index and "| **AA11** |" in index, "and keeps in full what stays")
    check(NCR.register_text(root) == original, "read back, it is the register byte for byte")

    print()
    print("3. Every link reaches what it reached")
    a = read(os.path.join(folder_of(root), "group-a.md"))
    check("[D-01](../DECISIONS.md)" in a, "a link one folder deeper: DECISIONS.md -> ../DECISIONS.md")
    check("[a tool](../../tools/x.py)" in a, "and ../tools/x.py -> ../../tools/x.py")
    check("[Group B](group-b.md#group-b--revit-loads)" in a, "a link to a heading in another group follows it")
    check("[a row](../NEEDS-CHECKING.md#)" in a, "a placeholder link still lands on the register")
    check("[the deep one](needs-checking/group-a.md#a-deep-note)" in index,
          "a link from the page into a moved heading goes to its file")
    check("[the first group](needs-checking/group-a.md#group-a--it-builds)" in index,
          "and so does one to a moved group's heading")
    check("[a decision](DECISIONS.md)" in index, "a link that stays on the page is untouched")

    print()
    print("4. The real readers report exactly what they reported")
    now = readers_at(root)
    for key in sorted(today):
        check(now[key] == today[key], "%s is the same on the split register" % key)
    check(today["check-docs B8"] == "2026-09-07", "and B8's PASSED is found where it now lives (%s)" % now["check-docs B8"])
    check("A1" in [r[0] for r in now["owner-queue"][0]], "owner-queue still sees A1, now in group-a.md")

    print()
    print("5. A second run moves nothing; a new group is moved by the next")
    again = SPLIT.plan(index_of(root), folder_of(root), "2026-09-30", root)
    check(not again.moving and again.already == 4, "nothing to move, four moved before")
    header_a = read(os.path.join(folder_of(root), "group-a.md")).split("## Group A")[0]
    with io.open(index_of(root), "a", encoding="utf-8", newline="") as handle:
        handle.write(NL.join(["## Group C " + DASH + " written after the split", "",
                              "| **C1** | Gate it | open |", ""]))
    later = SPLIT.plan(index_of(root), folder_of(root), "2026-09-30", root)
    check([n for _, n in later.moving] == ["group-c.md"] and not later.problems, "the next run moves the new group")
    SPLIT.write(later)
    check(read(os.path.join(folder_of(root), "group-a.md")).split("## Group A")[0] == header_a,
          "and every existing file keeps its opening")
    check("| **C1** |" in NCR.register_text(root), "the new row is read back into the register")

    print()
    print("6. A missing group is broken, never shorter")
    gone = os.path.join(folder_of(root), "group-b.md")
    kept = read(gone)
    os.remove(gone)
    try:
        NCR.register_text(root)
        raised = False
    except NCR.RegisterBroken:
        raised = True
    check(raised, "a file the page names and nobody kept raises RegisterBroken")
    check("raised RegisterBroken" in str(readers_at(root)["owner-queue"]), "and owner-queue says so rather than list less")
    with io.open(gone, "w", encoding="utf-8", newline="") as handle:
        handle.write(kept.replace("## Group B " + DASH + " Revit loads", "## Group B " + DASH + " renamed"))
    try:
        NCR.register_text(root)
        raised = False
    except NCR.RegisterBroken:
        raised = True
    check(raised, "a file that no longer opens with its heading raises too")
    with io.open(gone, "w", encoding="utf-8", newline="") as handle:
        handle.write(kept)

    print()
    print("7. archive-needs-checking still moves a done row on the split register")
    ANC = _load("archive_needs_checking", "archive-needs-checking.py")
    check(ANC is not None, "the archive tool loads")
    if ANC is not None:
        archive = os.path.join(root, "docs", "needs-checking-archive")
        plan = ANC.plan(index_of(root), archive, "2026-09-23")
        check(plan.fatal is None and not plan.problems and sum(len(v) for v in plan.moves.values()) == 2,
              "it plans A2 and B8, both done (%s)" % (plan.fatal or "; ".join(plan.problems) or "clean"))
        before_index = read(index_of(root))
        check(ANC.write(plan) == ANC.OK, "it writes")
        a_after = read(os.path.join(folder_of(root), "group-a.md"))
        check("](../needs-checking-archive/group-a.md#row-a2)" in a_after,
              "A2's line in group-a.md links to its full text, one folder up")
        check(read(index_of(root)) == before_index, "the page itself is untouched - A2 was never on it")
        check("### Row A2" in read(os.path.join(archive, "group-a.md")), "and the full row is in the archive")
        check(NCR.register_text(root) == plan.after, "read back, the register is what the archive tool planned")


def run(work):
    check(NCR is not None, "tools/needs-checking-register.py exists and loads")
    check(SPLIT is not None, "tools/split-needs-checking.py exists and loads")
    for name in ("plan", "write", "layout_split", "B8", "PASSED"):
        check(SPLIT is not None and getattr(SPLIT, name, None) is not None, "the split tool has %s" % name)
    for name in ("register_text", "RegisterBroken", "STUB"):
        check(NCR is not None and getattr(NCR, name, None) is not None, "the reader has %s" % name)
    if FAILURES:
        return

    original = register_text()
    whole = make_root(work, original, "whole")
    root = make_root(work, original)
    today = readers_at(whole)

    print()
    print("1. What moves, and what stays")
    p = SPLIT.plan(index_of(root), folder_of(root), "2026-09-23", root)
    moving = dict((h, n) for h, n in p.moving)
    staying = dict(p.staying)
    check(p.fatal is None and not p.problems, "the plan is clean (%s)" % (p.fatal or "; ".join(p.problems) or "no problem"))
    check(moving.get("## Group A " + DASH + " it builds") == "group-a.md", "a Group section moves to its group's file")
    check(moving.get("## Group AA - two letters") == "group-aa.md", "and so does a two-letter one")
    check(moving.get("## 2026-09-21 " + DASH + " a dated stage whose rows are one group") == "group-ac.md",
          "a dated section whose rows are all one group moves to that group's file")
    check("rows of 2 groups" in staying.get("## 2026-09-22 " + DASH + " two groups at once", ""),
          "a section with rows of two groups stays, and says why")
    check("no rows" in staying.get("## How this file works", ""), "the page's rules stay")
    check(not any("D-NN" in h for h in list(moving) + list(staying)),
          "a '## ' line inside a fence is an example, not a section")

    print()
    print("2. Written, and read back, the files ARE the register")
    written = SPLIT.write(p) == SPLIT.OK and os.path.isdir(folder_of(root))
    check(written, "it writes")
    if written:
        after_write(work, original, root, today)
    else:
        print("  FAIL  sections 3 to 7 need the split written, and it was not - not run")
        FAILURES.append("sections 3 to 7 not run")

    print()
    print("8. A link from elsewhere into a heading that would move stops the run")
    guarded = make_root(work, original, "guarded")
    with io.open(os.path.join(guarded, "docs", "OTHER.md"), "w", encoding="utf-8", newline="") as handle:
        handle.write("See [the note](NEEDS-CHECKING.md#a-deep-note)." + NL)
    stopped = SPLIT.plan(index_of(guarded), folder_of(guarded), "2026-09-23", guarded)
    check(any("a heading that would move" in x for x in stopped.problems), "it is refused, and says which link")
    check(SPLIT.write(stopped) == SPLIT.REFUSED and not os.path.isdir(folder_of(guarded)), "and nothing is written")

    print()
    print("9. CRLF is kept, and still reads back byte for byte")
    crlf_text = original.replace(NL, CR + NL)
    crlf = make_root(work, crlf_text, "crlf")
    q = SPLIT.plan(index_of(crlf), folder_of(crlf), "2026-09-23", crlf)
    wrote = SPLIT.write(q) == SPLIT.OK and os.path.isdir(folder_of(crlf))
    check(wrote and (CR + NL) in read(os.path.join(folder_of(crlf), "group-a.md")), "the group files are written CRLF")
    check(wrote and NCR.register_text(crlf) == crlf_text, "and the register reads back as it was, CRLF included")

    print()
    print("10. Every reader reads the register through the one reader")
    for filename in ("check-docs.py", "owner-queue.py", "check-gaps.py", "balance-of-work.py",
                     "archive-needs-checking.py"):
        source = read(os.path.join(ROOT, "tools", filename))
        check("needs-checking-register.py" in source and "register_text(" in source,
              "%s reads it through tools/needs-checking-register.py" % filename)
    # check-docs silences its "never ran against Revit" rule when it cannot
    # find B8's dated PASSED - it does not fail. So finding it is checked here,
    # on the real register, read the way check-docs reads it.
    try:
        real = NCR.register_text(ROOT) or ""
    except NCR.RegisterBroken as broken:
        real = ""
        check(False, "the real register reads whole (%s)" % broken)
    b8 = SPLIT.B8.search(real)
    check(b8 is not None and SPLIT.PASSED.search(b8.group(0)) is not None,
          "and B8's dated PASSED is still found in the real register, read through it")


if __name__ == "__main__":
    sys.exit(main())
