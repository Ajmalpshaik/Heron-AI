# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/renumber.py - an id this branch took is renamed in this branch's own
text, and main's own use of the same id is never touched.

    python tests/test_renumber.py

Every repository here is a throwaway with its own local `origin`, in which
main and a branch BOTH take D-116, Q-64, Group EJ and row 5b-489 - the clash
of 2026-10-17. Nothing here needs the network or touches the real registers.

WHAT THIS PROVES
  1. WHAT IT READS AS AN ID: D-116, Q-64, 5b-489, Row 5b-489, Group EJ and EJ;
     a one-letter group, an id from another register and no id at all are
     refused.
  2. A DRY RUN WRITES NOTHING and says every change it would make.
  3. BEFORE MAIN IS MERGED, each of the four renames the branch's headings,
     index rows, anchors, items, row cell, references and the files named
     for the id - committed or untracked - and leaves D-115, D-1160 and
     5b-4890 alone. A bare EJ is left for a person, named, and exits 1.
     The merge that follows has no add/add conflict on a renamed file.
  4. THE NEGATIVE CASE - AFTER MAIN IS MERGED, main's D-116, Q-64, Group EJ
     and row 489 sit in the same files as the branch's, and every one of
     main's lines is byte for byte as main wrote it while the branch's are
     renamed. A line of main's the branch edited is left alone and named.
  5. IT REFUSES, AND WRITES NOTHING: a --to main already has, a --from the
     branch only refers to, a rename onto a file that is there, a merge half
     done, and a folder that is not a repository - each exits 2.
  6. --next IS next-id's answer.
  7. A DECISION'S NUMBERING LINE SAYS WHAT HAPPENED: its words stay as
     history and a sentence names the old number, the date and why - main
     took it (3, 4) or which other branch holds it, never this branch's own
     pushed copy. One with no Numbering line gets one under its Status line
     (4, 7); one with no Status line either, and another decision's Numbering
     line naming the old id, are left alone, named, and exit 1.

WHAT IT DOES NOT PROVE. That every form a real document writes an id in is
one of the forms it renames. A form it misses stays as it was, so the gate
that caught the clash - check-docs.py, check-decision-titles.py or
test_open_defects.py - goes on catching it after the run.
"""

import datetime
import importlib.util
import io
import json
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "renumber.py")

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

_spec = importlib.util.spec_from_file_location("renumber", TOOL)
RN = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(RN)

FAILURES = []
ROWS = "docs/fragment-issues/section-5b-rows-476-500.md"
NOTE = "docs/work-notes/2026-10-17-d-116-note.md"


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


# core.autocrlf PINNED ON EVERY CALL, because the scratch repositories must not
# inherit the machine's line endings. With `core.autocrlf=true` - a Windows
# PC's usual git - the clones checked main's files out with CRLF, and sections
# 3 and 4 failed four "byte for byte as main wrote it" checks that were about
# the machine, not renumber.py (row 5b-1337). `-c`, not `git config`: a clone's
# checkout runs before any repository config exists, and a setting from the
# environment (GIT_CONFIG_COUNT) outranks the repository's own.
def git(where, *args, **kw):
    done = subprocess.run(["git", "-c", "core.autocrlf=false"] + list(args),
                          cwd=where, capture_output=True,
                          text=True, encoding="utf-8", timeout=60)
    if done.returncode != 0 and not kw.get("may_fail"):
        raise RuntimeError("git %s failed: %s" % (" ".join(args), done.stderr))
    return done.stdout.strip()


def configure(where):
    git(where, "config", "user.email", "test@example.invalid")
    git(where, "config", "user.name", "Heron test")
    git(where, "config", "commit.gpgsign", "false")


def write(where, files):
    for rel, text in files.items():
        path = os.path.join(where, *rel.split("/"))
        if not os.path.isdir(os.path.dirname(path)):
            os.makedirs(os.path.dirname(path))
        with io.open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)


def read(where, rel):
    with io.open(os.path.join(where, *rel.split("/")), encoding="utf-8", newline="") as handle:
        return handle.read()


def exists(where, rel):
    return os.path.exists(os.path.join(where, *rel.split("/")))


def commit(where, files, message):
    write(where, files)
    git(where, "add", "--", *sorted(files))
    git(where, "commit", "-q", "-m", message)


def run(where, *args):
    done = subprocess.run([sys.executable, TOOL, "--repo", where] + list(args),
                          capture_output=True, text=True, encoding="utf-8", timeout=300)
    return done.returncode, done.stdout + done.stderr


def snapshot(where):
    found = {}
    for folder, dirs, names in os.walk(where):
        dirs[:] = [d for d in dirs if d != ".git"]
        for name in names:
            path = os.path.join(folder, name)
            with open(path, "rb") as handle:
                found[os.path.relpath(path, where)] = handle.read()
    return found


# --------------------------------------------------------------------------
# The registers before either side took an id, in the shapes the real ones have
# --------------------------------------------------------------------------

START = {
    "docs/DECISIONS.md": (
        "# Decisions\n\n"
        "| [D-115](#d-115--the-last-before-the-fork) | The last before the fork | Accepted |\n\n"
        "## D-115 — The last before the fork\n\n"
        "**Full record:** [`decisions/D-115.md`](decisions/D-115.md)\n"),
    "docs/decisions/D-115.md": "# D-115 — The last before the fork\n",
    "docs/NEEDS-CHECKING.md": (
        "# Needs checking\n\n"
        "## Group EI - the last group before the fork\n\n"
        "**Its own file:** [`needs-checking/group-ei.md`](needs-checking/group-ei.md)\n"),
    "docs/needs-checking/group-ei.md": (
        "# Needs checking — Group EI\n\n| **EI1** | run it | look |\n"),
    "docs/FRAGMENT-ISSUES.md": (
        "# Fragment issues\n\n## 5b. HERON'S OWN DEFECTS\n\n"
        "**Its rows, in order:** [476-500](fragment-issues/section-5b-rows-476-500.md)\n"),
    ROWS: "| # | Defect | State |\n|---|---|---|\n| **488** | the last row before the fork | open |\n",
    "docs/OPEN-QUESTIONS.md": "# Open questions\n",
    "docs/open-questions/tier-2.md": "## Tier 2\n\n### \U0001F535 Q-63 — the last question before the fork\n",
    "docs/HANDOVER.md": "# Handover\n\nNothing yet.\n",
}

TODAY = datetime.date.today().isoformat()
NEXT_ID_SAID = "**Numbering:** `tools/next-id.py` found D-116 free."
MAIN_NUMBERING = "**Numbering:** main's own D-116, found free on main."
BECAUSE_MAIN = ", because `main` had given D-116 to another decision first."
# What the branch's Numbering line says once D-116 is D-131: its words kept, as history.
RENUMBERED = (NEXT_ID_SAID + " Renumbered from D-116 to D-131 on " + TODAY
              + " with `tools/renumber.py`" + BECAUSE_MAIN)

# Main takes all four first, and pushes.
MAIN_ADDS = {
    "docs/DECISIONS.md": START["docs/DECISIONS.md"] + (
        "\n## D-116 — Main's sixteen\n\n**Status:** Accepted\n" + MAIN_NUMBERING + "\n"
        "**Full record:** [`decisions/D-116.md`](decisions/D-116.md)\n"),
    "docs/decisions/D-116.md": "# D-116 — Main's sixteen\n",
    "docs/NEEDS-CHECKING.md": START["docs/NEEDS-CHECKING.md"] + (
        "\n## Group EJ - main's group\n\n"
        "**Its own file:** [`needs-checking/group-ej.md`](needs-checking/group-ej.md)\n"),
    "docs/needs-checking/group-ej.md": "# Needs checking — Group EJ\n\n| **EJ1** | main's | look |\n",
    ROWS: START[ROWS] + "| **489** | main's row 489, see D-116 | open |\n",
    "docs/open-questions/tier-1.md": "## Tier 1\n\n### \U0001F7E0 Q-64 — main's question\n",
    "docs/HANDOVER.md": "# Handover\n\nMain took D-116, Q-64, Group EJ and row 5b-489.\n\nNothing yet.\n",
}

# The branch, cut before main took them, takes the same four.
BRANCH_ADDS = {
    "docs/DECISIONS.md": START["docs/DECISIONS.md"].replace(
        "| Accepted |\n", "| Accepted |\n"
        "| [D-116](#d-116--the-branchs-sixteen) | The branch's sixteen | Accepted |\n") + (
        "\n## D-116 — The branch's sixteen\n\n**Status:** Accepted · **Date:** 2026-10-17\n"
        + NEXT_ID_SAID + "\n**Full record:** [`decisions/D-116.md`](decisions/D-116.md)\n"),
    "docs/decisions/D-116.md": ("# D-116 — The branch's sixteen\n\n**Status:** Accepted · **Date:** 2026-10-17\n"
                                + NEXT_ID_SAID + "\n\nIt keeps [D-115](D-115.md).\n"),
    "docs/NEEDS-CHECKING.md": START["docs/NEEDS-CHECKING.md"] + (
        "\n## Group EJ - the branch's group\n\n"
        "**Its own file:** [`needs-checking/group-ej.md`](needs-checking/group-ej.md)\n"),
    "docs/needs-checking/group-ej.md": (
        "# Needs checking — Group EJ\n\n"
        "| **EJ1** | run it | look |\n| **EJ2** | the negative | nothing |\n\n"
        "EJ1 to EJ2 need a Revit; [row 5b-489](../fragment-issues/section-5b-rows-476-500.md) has why.\n"),
    ROWS: START[ROWS] + "| **489** | the branch's row, see [D-116](../DECISIONS.md#d-116--the-branchs-sixteen) | open |\n",
    "docs/open-questions/tier-2.md": START["docs/open-questions/tier-2.md"] + (
        "\n### \U0001F535 Q-64 — the branch's question\n\nAnswered by [D-116](../decisions/D-116.md).\n"),
    "docs/HANDOVER.md": START["docs/HANDOVER.md"] + (
        "\nThis branch took [D-116](decisions/D-116.md), "
        "[Q-64](open-questions/tier-2.md#-q-64--the-branchs-question), group EJ (EJ1, EJ2) and "
        "row 5b-489. It keeps D-115. Not D-1160, not 5b-4890, not Q-640.\n"),
    "brain/fragments/demo/fragment.yaml": "# row 5b-489 and NEEDS-CHECKING Group EJ\nname: demo\n",
}


def world(home, name):
    """(origin, work): origin's main has taken all four ids; WORK is a clone
    on a branch cut before that, which has taken them too."""
    origin = os.path.join(home, name + "-origin.git")
    seed = os.path.join(home, name + "-seed")
    work = os.path.join(home, name + "-work")
    git(home, "init", "-q", "--bare", origin)
    git(origin, "symbolic-ref", "HEAD", "refs/heads/main")
    git(home, "clone", "-q", origin, seed)
    configure(seed)
    git(seed, "checkout", "-q", "-b", "main")
    commit(seed, START, "the registers before the fork")
    git(seed, "push", "-q", "origin", "main")
    git(home, "clone", "-q", origin, work)
    configure(work)
    git(work, "checkout", "-q", "-b", "feat/clash")
    commit(seed, MAIN_ADDS, "main takes D-116, Q-64, Group EJ and 5b-489")
    git(seed, "push", "-q", "origin", "main")
    commit(work, BRANCH_ADDS, "the branch takes them too")
    git(work, "fetch", "-q", "origin")
    return origin, work


# --------------------------------------------------------------------------

def section_1():
    print("\n1. what it reads as an id")
    for typed, kind, value in (("D-116", "D", 116), ("d-116", "D", 116), ("Q-64", "Q", 64),
                               ("5b-489", "5b", 489), ("Row 5b-489", "5b", 489),
                               ("Group EJ", "group", "EJ"), ("EJ", "group", "EJ"),
                               ("group-ej", "group", "EJ")):
        got = RN.parse(typed)
        check((got.kind, got.value) == (kind, value), "%r is %s %r" % (typed, kind, value))
    for typed in ("Group A", "D116", "Q 64", "row 489", ""):
        try:
            RN.parse(typed)
            refused = False
        except RN.CouldNot:
            refused = True
        check(refused, "%r is refused" % typed)
    renamer = RN.Renamer(RN.parse("D-116"), RN.parse("D-131"), "116", "131")
    line, hits = renamer.line("[D-116](#d-116--x) not D-1160 nor AD-116", "x.md")
    check(line == "[D-131](#d-131--x) not D-1160 nor AD-116",
          "a longer number and a longer word are not the id: %r" % line)
    check([h[0] for h in hits] == [1, 9], "the columns are in the renamed line: %r" % hits)


def section_2(work):
    print("\n2. a dry run writes nothing")
    before = snapshot(work)
    status = git(work, "status", "--porcelain")
    code, text = run(work, "--from", "D-116", "--to", "D-131", "--dry-run")
    check(code == 0, "it exits 0 (%d)" % code)
    check(snapshot(work) == before and git(work, "status", "--porcelain") == status,
          "and the checkout is byte for byte as it was")
    check("DRY RUN" in text and "nothing was written" in text, "and it says so")
    check("renamed  docs/decisions/D-116.md -> docs/decisions/D-131.md" in text,
          "it names the file it would rename")
    check("docs/DECISIONS.md:4  D-116 -> D-131" in text
          and "docs/DECISIONS.md:4  d-116 -> d-131" in text,
          "and each use it would rename, by line")


def section_3(work):
    print("\n3. before main is merged, each register is renamed in the branch's text")
    write(work, {NOTE: "# Notes on D-116\n"})
    code, text = run(work, "--from", "D-116", "--to", "D-131")
    check(code == 0, "D-116 -> D-131 exits 0 (%d)" % code)
    decisions = read(work, "docs/DECISIONS.md")
    check("| [D-131](#d-131--the-branchs-sixteen) | The branch's sixteen |" in decisions,
          "the index row and its anchor")
    check("## D-131 — The branch's sixteen" in decisions
          and "[`decisions/D-131.md`](decisions/D-131.md)" in decisions,
          "the heading and the line naming its file")
    check(all(line in decisions.split("\n") for line in START["docs/DECISIONS.md"].split("\n")),
          "and every line it had before the fork - D-115's - is untouched")
    check(not exists(work, "docs/decisions/D-116.md")
          and read(work, "docs/decisions/D-131.md").startswith("# D-131 — The branch's sixteen\n")
          and "It keeps [D-115](D-115.md)." in read(work, "docs/decisions/D-131.md"),
          "the committed decisions/D-116.md is now D-131.md, and its title says so")
    for rel in ("docs/DECISIONS.md", "docs/decisions/D-131.md"):
        lines = read(work, rel).split("\n")
        check(RENUMBERED in lines and NEXT_ID_SAID not in lines,
              "%s: its Numbering line keeps D-116 as history and says it was renumbered, and why" % rel)
    check("numbering  docs/DECISIONS.md:" in text and "added to: Renumbered from D-116 to D-131" in text,
          "and it prints the Numbering line it wrote")
    check(not exists(work, NOTE) and exists(work, NOTE.replace("d-116", "d-131"))
          and read(work, NOTE.replace("d-116", "d-131")) == "# Notes on D-131\n",
          "an untracked note named for it is renamed too")
    check("[D-131](../DECISIONS.md#d-131--the-branchs-sixteen)" in read(work, ROWS),
          "a reference in a 5b row")
    check("git add -- " in text and "Nothing was staged" in text,
          "it stages nothing, and prints the paths to stage")

    code, text = run(work, "--from", "Q-64", "--to", "Q-69")
    check(code == 0, "Q-64 -> Q-69 exits 0 (%d)" % code)
    check("### \U0001F535 Q-69 — the branch's question" in read(work, "docs/open-questions/tier-2.md"),
          "the question's heading")

    code, text = run(work, "--from", "5b-489", "--to", "5b-652")
    check(code == 0, "5b-489 -> 5b-652 exits 0 (%d)" % code)
    rows = read(work, ROWS)
    check("| **652** | the branch's row" in rows and "| **488** |" in rows,
          "the row's own number cell, and row 488 untouched")
    check("[row 5b-652](" in read(work, "docs/needs-checking/group-ej.md")
          and "row 5b-652 and" in read(work, "brain/fragments/demo/fragment.yaml"),
          "and its references, in a group file and in a fragment")

    code, text = run(work, "--from", "Group EJ", "--to", "Group FZ")
    check(code == 0, "Group EJ -> Group FZ exits 0 (%d)" % code)
    check(not exists(work, "docs/needs-checking/group-ej.md") and exists(work, "docs/needs-checking/group-fz.md"),
          "group-ej.md is now group-fz.md")
    group = read(work, "docs/needs-checking/group-fz.md")
    check(group.startswith("# Needs checking — Group FZ") and "| **FZ1** |" in group
          and "| **FZ2** |" in group and "FZ1 to FZ2" in group, "its title and its items")
    index = read(work, "docs/NEEDS-CHECKING.md")
    check("## Group FZ - the branch's group" in index
          and "[`needs-checking/group-fz.md`](needs-checking/group-fz.md)" in index
          and "## Group EI" in index, "the index heading and its file line, and Group EI untouched")
    handover = read(work, "docs/HANDOVER.md")
    check("group FZ (FZ1, FZ2)" in handover, "a lower-case 'group EJ' in prose")
    check("Not D-1160, not 5b-4890, not Q-640." in handover and "It keeps D-115." in handover,
          "D-1160, 5b-4890, Q-640 and D-115 are as they were")
    check("[D-131](decisions/D-131.md)" in handover and "#-q-69--the-branchs-question" in handover
          and "row 5b-652" in handover, "and every rename landed in the same line of prose")

    write(work, {"docs/work-notes/bare.md": "Checked FZ by hand.\n"})
    code, text = run(work, "--from", "Group FZ", "--to", "Group GA", "--dry-run")
    check(code == 1 and "LEFT ALONE" in text and "docs/work-notes/bare.md:1" in text
          and "a bare FZ at column 9" in text, "a bare id is named, left alone, and exits 1")
    os.remove(os.path.join(work, "docs", "work-notes", "bare.md"))

    git(work, "add", "-A")
    git(work, "commit", "-q", "-m", "renumbered")
    git(work, "merge", "--no-edit", "origin/main", may_fail=True)
    conflicted = set(git(work, "diff", "--name-only", "--diff-filter=U").split())
    check(not conflicted & {"docs/decisions/D-116.md", "docs/decisions/D-131.md",
                            "docs/needs-checking/group-ej.md", "docs/needs-checking/group-fz.md"},
          "merging main now collides on no file named for an id: %s" % sorted(conflicted))
    check(read(work, "docs/decisions/D-116.md") == MAIN_ADDS["docs/decisions/D-116.md"],
          "and main's decisions/D-116.md arrives as main wrote it")

    before = snapshot(work)
    code, text = run(work, "--from", "D-131", "--to", "D-140")
    check(code == 2 and "half done" in text and snapshot(work) == before,
          "with the merge half done it refuses, exits 2 and writes nothing")
    git(work, "merge", "--abort")


def merged_world(home):
    """WORK has merged main and kept both sides of every collision, as
    heron-ship 6a says to - except the two files named for an id, which a
    branch cannot keep both of and so does not have here."""
    origin, work = world(home, "merged")
    git(work, "rm", "-q", "docs/decisions/D-116.md", "docs/needs-checking/group-ej.md")
    git(work, "commit", "-q", "-m", "this branch keeps its decision and its group inline")
    git(work, "merge", "--no-edit", "origin/main", may_fail=True)
    both = {
        "docs/DECISIONS.md": MAIN_ADDS["docs/DECISIONS.md"].replace(
            "| Accepted |\n", "| Accepted |\n"
            "| [D-116](#d-116--the-branchs-sixteen) | The branch's sixteen | Accepted |\n") + (
            "\n## D-116 — The branch's sixteen\n\n**Status:** Accepted\n\nIts words, inline.\n"),
        "docs/NEEDS-CHECKING.md": MAIN_ADDS["docs/NEEDS-CHECKING.md"] + (
            "\n## Group EJ - the branch's group\n\n| **EJ1** | run it | look |\n"),
        ROWS: MAIN_ADDS[ROWS] + BRANCH_ADDS[ROWS].splitlines(True)[-1],
        "docs/HANDOVER.md": MAIN_ADDS["docs/HANDOVER.md"] + BRANCH_ADDS["docs/HANDOVER.md"][len(START["docs/HANDOVER.md"]):],
    }
    write(work, both)
    git(work, "add", "-A")
    git(work, "commit", "-q", "-m", "merge main, both sides kept")
    return origin, work


def section_4(home):
    print("\n4. THE NEGATIVE CASE - after main is merged, main's own uses are untouched")
    _, work = merged_world(home)
    mains = {
        "docs/decisions/D-116.md": "# D-116 — Main's sixteen\n",
        "docs/needs-checking/group-ej.md": MAIN_ADDS["docs/needs-checking/group-ej.md"],
        "docs/open-questions/tier-1.md": MAIN_ADDS["docs/open-questions/tier-1.md"],
    }
    main_lines = ["## D-116 — Main's sixteen", "## Group EJ - main's group",
                  "| **489** | main's row 489, see D-116 | open |", MAIN_NUMBERING,
                  "Main took D-116, Q-64, Group EJ and row 5b-489."]
    for old, new in (("Q-64", "Q-69"), ("5b-489", "5b-652"), ("Group EJ", "Group FZ")):
        code, text = run(work, "--from", old, "--to", new)
        check(code == 0, "%s -> %s exits 0 after the merge (%d)" % (old, new, code))
    # A line main wrote, which the branch then edits: whose D-116 is it?
    handover = read(work, "docs/HANDOVER.md")
    write(work, {"docs/HANDOVER.md": handover.replace("row 5b-489.\n", "row 5b-489. Seen.\n", 1)})
    code, text = run(work, "--from", "D-116", "--to", "D-131")
    check(code == 1, "D-116 -> D-131 exits 1: one line is left for a person (%d)" % code)
    check("docs/HANDOVER.md:3" in text and "it may be origin/main's" in text,
          "the line of main's the branch edited is named, and why")

    for rel, text in mains.items():
        check(read(work, rel) == text, "%s is byte for byte as main wrote it" % rel)
    whole = "".join(read(work, rel) for rel in ("docs/DECISIONS.md", "docs/NEEDS-CHECKING.md",
                                                ROWS, "docs/HANDOVER.md"))
    for line in main_lines:
        check(line in whole, "main's line is untouched: %r" % line)
    check("Main took D-116, Q-64, Group EJ and row 5b-489. Seen." in whole,
          "and the edited line is left as it was, for a person")
    check("## D-131 — The branch's sixteen" in whole
          and "| [D-131](#d-131--the-branchs-sixteen) |" in whole
          and "## Group FZ - the branch's group" in whole and "| **FZ1** | run it" in whole
          and "| **652** | the branch's row, see [D-131]" in whole,
          "while every line of the branch's own is renamed")
    written = ("**Numbering:** written as D-116; renumbered D-131 on " + TODAY
               + " with `tools/renumber.py`" + BECAUSE_MAIN)
    check("## D-131 — The branch's sixteen\n\n**Status:** Accepted\n" + written + "\n\nIts words, inline." in whole,
          "a decision with no Numbering line gets one, under its Status line")
    check(whole.count("**Numbering:**") == 2, "and main's decision gets none from it")
    check("### \U0001F535 Q-69 — the branch's question" in read(work, "docs/open-questions/tier-2.md"),
          "and its question, in the file main never touched")
    check(git(work, "diff", "--stat", "origin/main", "--", *sorted(mains)) == "",
          "git agrees: main's own files do not differ from origin/main")


def section_5(home):
    print("\n5. it refuses, and writes nothing")
    _, work = world(home, "refuse")
    write(work, {NOTE: "# Notes on D-116\n", "docs/work-notes/2026-10-17-d-141-note.md": "taken\n"})
    before = snapshot(work)
    for args, said in ((["--from", "D-116", "--to", "D-115"], "origin/main already has D-115"),
                       (["--from", "D-115", "--to", "D-140"], "defines no D-115 of its own"),
                       (["--from", "D-116", "--to", "D-141"], "which is already there"),
                       (["--from", "Q-64", "--to", "Q-64"], "is already"),
                       (["--from", "D-116", "--to", "Q-70"], "different registers"),
                       (["--from", "D-116", "--to", "D-140", "--base", "origin/nothing"],
                        "there is no origin/nothing")):
        code, text = run(work, *args)
        check(code == 2 and said in text and snapshot(work) == before,
              "%s: exits 2, says %r, writes nothing (%d)" % (" ".join(args), said, code))
    write(work, {"docs/decisions/D-142.md": "# D-142 — written here, not yet committed\n"})
    code, text = run(work, "--from", "D-116", "--to", "D-142")
    check(code == 2 and "this checkout already has D-142" in text,
          "a --to this checkout already defines is refused (%d)" % code)
    bare = os.path.join(home, "not-a-repo")
    os.makedirs(bare)
    code, text = run(bare, "--from", "D-116", "--to", "D-140")
    check(code == 2 and "nothing written" in text, "a folder that is not a repository exits 2")


def section_6(home):
    print("\n6. --next is next-id's answer")
    _, work = world(home, "next")
    code, text = run(work, "--next", "--no-fetch")
    check(code == 0 and "Next free id in each register" in text and "next D-117" in text
          and "next 5b-490" in text and "next EK" in text and "next Q-65" in text,
          "it prints the id above main's and this checkout's (%d)" % code)
    code, text = run(work, "--next", "--no-fetch", "--json")
    try:
        answer = json.loads(text)
    except ValueError:
        answer = {}
    check(code == 0 and answer.get("registers", {}).get("D", {}).get("next") == "D-117",
          "and --json gives it for a machine")


def section_7(home):
    print("\n7. the Numbering line, when another branch holds the id and main does not")
    origin = os.path.join(home, "held-origin.git")
    seed = os.path.join(home, "held-seed")
    work = os.path.join(home, "held-work")
    git(home, "init", "-q", "--bare", origin)
    git(origin, "symbolic-ref", "HEAD", "refs/heads/main")
    git(home, "clone", "-q", origin, seed)
    configure(seed)
    git(seed, "checkout", "-q", "-b", "main")
    commit(seed, START, "the registers")
    git(seed, "push", "-q", "origin", "main")
    git(seed, "checkout", "-q", "-b", "feat/other")
    commit(seed, {"docs/decisions/D-116.md": "# D-116 — Another branch's sixteen\n"}, "other takes D-116")
    git(seed, "push", "-q", "origin", "feat/other")
    git(home, "clone", "-q", origin, work)
    configure(work)
    git(work, "checkout", "-q", "-b", "feat/clash")
    sibling = "**Numbering:** took D-117, because D-116 is this branch's other decision."
    commit(work, {
        "docs/DECISIONS.md": START["docs/DECISIONS.md"] + (
            "\n## D-116 — The branch's sixteen\n\n**Full record:** [`decisions/D-116.md`](decisions/D-116.md)\n"
            "\n## D-117 — Its sibling\n\n**Status:** Accepted\n" + sibling + "\n"),
        "docs/decisions/D-116.md": "# D-116 — The branch's sixteen\n\n**Status:** Accepted\n\nIts words.\n",
    }, "the branch takes D-116 and D-117")
    # Pushed, so the remote holds this branch's own copy of D-116 too - and it is no reason.
    git(work, "push", "-q", "-u", "origin", "feat/clash")
    git(work, "fetch", "-q", "origin")

    code, text = run(work, "--from", "D-116", "--to", "D-131")
    check(code == 1, "it exits 1: two lines are left for a person (%d)" % code)
    record = read(work, "docs/decisions/D-131.md")
    check(record == ("# D-131 — The branch's sixteen\n\n**Status:** Accepted\n"
                     "**Numbering:** written as D-116; renumbered D-131 on " + TODAY
                     + " with `tools/renumber.py`, because feat/other holds D-116 too.\n\nIts words.\n"),
          "a record with no Numbering line gets one under its Status line, naming the branch "
          "that holds D-116 and not this one: %r" % record)
    log = read(work, "docs/DECISIONS.md")
    check("## D-131 — The branch's sixteen\n\n**Full record:**" in log,
          "the index's section with no Status line is given no Numbering line")
    check("docs/DECISIONS.md:" in text and "has no **Status:** line" in text,
          "and that is named for a person")
    check(sibling in log and "another decision's Numbering line" in text,
          "another decision's Numbering line naming D-116 is left as written, and named")


def main():
    home = tempfile.mkdtemp(prefix="heron-renumber-")
    try:
        section_1()
        _, work = world(home, "before")
        section_2(work)
        section_3(work)
        section_4(home)
        section_5(home)
        section_6(home)
        section_7(home)
    finally:
        shutil.rmtree(home, ignore_errors=True)
    print()
    if FAILURES:
        print("%d check(s) FAILED" % len(FAILURES))
        return 1
    print("all checks passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
