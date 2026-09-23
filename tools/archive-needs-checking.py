# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Move the FINISHED rows of docs/NEEDS-CHECKING.md into docs/needs-checking-archive/,
one file per group, and leave each row a single line in the register.

    python tools/archive-needs-checking.py            # what would move - writes nothing
    python tools/archive-needs-checking.py --write    # move it

Exits 0 when the plan is clean or was written, 1 when a check refused and
NOTHING was written, 2 when NEEDS-CHECKING.md could not be read at all.

WHY THIS EXISTS
---------------
The same reason as tools/archive-fragment-issues.py, whose helpers this borrows:
a register closed row by row in place grows until a session cannot read it. On
2026-09-22 NEEDS-CHECKING.md was 295,763 bytes and 58 of its rows were done.

WHAT MOVES
----------
A row whose ID is struck through at both ends - `| ~~**A1**~~ |`. That is the
one mark all three of this register's readers take to mean done:
tools/owner-queue.py, tools/check-gaps.py and tools/balance-of-work.py. AND whose
words say nowhere that something is still owed - the list in
archive-fragment-issues.py, plus STILL WORTH, which this register uses for a check
that passed and is still worth running on the owner's PC. A struck row that says
so stays in full.

WHAT A MOVED ROW LEAVES BEHIND
------------------------------
A line of the same shape: its struck ID exactly as it was, a one-line title with a
link to its full text, the opening of its result, and as many cells as its table
has, so the table still renders. The full row goes to the file for its group,
under a heading per row, each cell labelled by its table's own header.

IT PROVES THE READERS SEE THE SAME REGISTER
-------------------------------------------
Before a byte is written it runs all three readers on the rewritten text - the
owner queue's open rows, the gaps report's register section, the balance's row
and done counts - and refuses unless each answers exactly as it does now. It also
checks the one thing tools/check-docs.py reads from this file: row B8's dated
PASSED, which decides whether "the write path has never run" may be said.
"""

import argparse
import contextlib
import datetime
import importlib.util
import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
REGISTER = os.path.join(ROOT, "docs", "NEEDS-CHECKING.md")
ARCHIVE_NAME = "needs-checking-archive"
ARCHIVE = os.path.join(ROOT, "docs", ARCHIVE_NAME)


def _load(name, filename):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


AF = _load("archive_fragment_issues", "archive-fragment-issues.py")
# SINCE 2026-09-23 THE REGISTER IS ONE FILE PER GROUP. It is read as one text
# - NEEDS-CHECKING.md with every group read back into its place - and written
# back through the same layout, so everything below still plans on one text.
NCR = _load("needs_checking_register", "needs-checking-register.py")
SPLIT = _load("split_needs_checking", "split-needs-checking.py")
NL, CR, DASH = AF.NL, AF.CR, AF.DASH
OK, REFUSED, COULD_NOT = AF.OK, AF.REFUSED, AF.COULD_NOT
CELL = AF.OD.CELL

ROW = re.compile("^[|] *(~~)?[*][*]([A-Z]+)([0-9]+[a-z]?)[*][*](~~)? *[|]")
GROUP = re.compile("^## (Group [A-Z]+) +(.*)$")
B8 = re.compile("^[|] *~~[*][*]B8[*][*]~~.*$", re.M)
PASSED = re.compile("[*][*]PASSED +([0-9]{4}-[0-9]{2}-[0-9]{2})")
EXTRA_HOLDS = ("STILL WORTH",)


def held_because(cells):
    text = " ".join(cells).replace("~", "").replace("*", "").replace("`", "")
    upper = text.replace(AF.CURLY_APOSTROPHE, "'").upper()
    for word in AF.STILL_OWED + EXTRA_HOLDS:
        if AF._has_word(upper, word):
            return "says something is still owed - " + word
    for word in AF.REOPENED:
        if word in upper:
            return "says " + word
    return None


def header_of(lines, i):
    """The header cells of the table that line I sits in."""
    k = i
    while k > 0 and lines[k - 1].startswith("|"):
        k -= 1
    if k + 1 < len(lines) and lines[k + 1].replace("|", "").replace("-", "").replace(":", "").strip() == "":
        return [c.strip() for c in CELL.split(lines[k])[1:-1]]
    return []


def readers(text):
    """What the three readers - and check-docs' B8 test - make of TEXT."""
    rel = "docs/NEEDS-CHECKING.md"
    answers = {}
    for key, filename, call in (("owner-queue", "owner-queue.py", "needs_checking"),
                                ("check-gaps", "check-gaps.py", "check_register"),
                                ("balance-of-work", "balance-of-work.py", "register_rows")):
        module = _load(key.replace("-", "_"), filename)
        original = module.read
        module.read = lambda path, _o=original: text if path.replace(os.sep, "/").endswith(rel) else _o(path)
        buffer = io.StringIO()
        try:
            with contextlib.redirect_stdout(buffer):
                result = getattr(module, call)()
        finally:
            module.read = original
        answers[key] = (result, buffer.getvalue())
    b8 = B8.search(text)
    passed = PASSED.search(b8.group(0)) if b8 else None
    answers["check-docs B8"] = passed.group(1) if passed else None
    return answers


class Plan(object):
    def __init__(self, register, archive, today):
        self.register, self.archive, self.today = register, archive, today
        self.fatal = None
        self.problems = []
        self.held = []
        self.moves = {}          # file name -> [(sort key, block)]
        self.titles = {}         # file name -> group heading
        self.eol = NL
        self.before = ""
        self.after = ""
        self.already = 0
        self.index_after = ""     # NEEDS-CHECKING.md as written
        self.files_after = {}     # each group file as written


def plan(register=REGISTER, archive=ARCHIVE, today=None):
    p = Plan(register, archive, today or datetime.date.today().isoformat())
    try:
        p.before = NCR.register_text(os.path.dirname(os.path.dirname(register)))
        if p.before is None:
            raise IOError("there is no such file")
    except (IOError, OSError) as error:
        p.fatal = "could not read %s: %s" % (register, error)
        return p
    except NCR.RegisterBroken as broken:
        p.fatal = str(broken)
        return p
    lines, p.eol = AF._split(p.before)
    if lines is None:
        p.fatal = "NEEDS-CHECKING.md mixes CRLF and LF line endings"
        return p

    group, existing = None, {}
    for i, line in enumerate(lines):
        g = GROUP.match(line)
        if g:
            group = (g.group(1), g.group(2))
            continue
        m = ROW.match(line)
        if not m or not (m.group(1) and m.group(4)):
            continue
        ident = m.group(2) + m.group(3)
        cells = CELL.split(line)
        if "](" + ARCHIVE_NAME + "/" in line:
            p.already += 1
            continue
        why = held_because(cells[2:-1])
        if why:
            p.held.append((ident, why))
            continue
        name = "group-%s.md" % m.group(2).lower()
        heading = "### Row " + ident
        if name not in existing:
            path = os.path.join(archive, name)
            existing[name] = AF._read(path) if os.path.exists(path) else None
        if existing[name] is not None and heading in existing[name].replace(CR, "").split(NL):
            p.problems.append("row %s is already in %s, but the register still carries it in full" % (ident, name))
            continue

        trouble = []
        labels = header_of(lines, i)
        parts = []
        for n, cell in enumerate(cells[2:-1]):
            label = labels[n + 1] if n + 1 < len(labels) and labels[n + 1] else "Cell %d" % (n + 2)
            parts.append("**%s.** %s" % (label.rstrip("."), AF.repoint(cell.strip(), register, archive, ident, trouble)))
        if trouble:
            p.held.append((ident, "; ".join(trouble)))
            continue

        what = cells[2].replace("~~", "")
        title = AF.title_of(what)
        link = "[full row](%s/%s#row-%s)" % (ARCHIVE_NAME, name, ident.lower())
        stub_cells = [cells[0], cells[1], " %s %s %s " % (title, DASH, link)]
        if len(cells) > 4:
            stub_cells.append(" %s " % AF.lead_of(cells[3].replace("~~", "")))
            stub_cells.extend([" "] * (len(cells) - 5))
        stub_cells.append(cells[-1])
        stub = "|".join(stub_cells)
        again = ROW.match(stub)
        if (len(CELL.split(stub)) != len(cells) or not again
                or again.group(2) + again.group(3) != ident or not (again.group(1) and again.group(4))):
            p.problems.append("row %s: its one-line form would not read back as the same struck row" % ident)
            continue

        block = NL.join([heading, "", "*Moved from the register on %s.*" % p.today, ""]
                        + [x + NL for x in parts] + ["---", ""])
        key = (int(re.match("[0-9]+", m.group(3)).group(0)), m.group(3))
        p.moves.setdefault(name, []).append((key, block))
        p.titles[name] = group
        lines[i] = stub

    p.after = p.eol.join(lines)
    if p.moves and not p.problems:
        before, after = readers(p.before), readers(p.after)
        for key in before:
            if before[key] != after[key]:
                p.problems.append("%s would read the register differently afterwards" % key)
    if p.moves and not p.problems:
        p.index_after, p.files_after, trouble = SPLIT.layout_split(register, p.after, p.today)
        p.problems.extend(trouble)
        back = NCR.register_text(read=SPLIT._served(p.index_after, p.files_after))
        if back != p.after:
            p.problems.append("the register's files, written this way, would not read back as the new register")
    return p


def _header(name, group):
    label, title = group if group else ("Group", "")
    return NL.join([
        "# NEEDS-CHECKING archive %s %s" % (DASH, label),
        "",
        "> **Checks that were done, moved out of the live register.** These are rows of %s of" % label,
        "> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) %s *%s* %s whose ID was struck through, the register's own"
        % (DASH, title.lstrip(DASH + " -"), DASH),
        "> sign that the check was done. Each still has its line in the register, with a link here. **Its words",
        "> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the",
        "> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).",
        "",
        "---",
        "",
    ])


def _readme(p):
    """The folder's index, rebuilt from the files in it every time rows move."""
    names = set(p.moves)
    if os.path.isdir(p.archive):
        names |= set(n for n in os.listdir(p.archive) if n.startswith("group-") and n.endswith(".md"))
    rows = ["| Group %s | [`%s`](%s) |" % (n[len("group-"):-3].upper(), n, n)
            for n in sorted(names, key=lambda n: (len(n), n))]
    return NL.join([
        "# NEEDS-CHECKING archive",
        "",
        "**Checks that were done, from [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md). Nothing here is a live",
        "obligation.** One file per group; every row here still has its struck line in the register, with a link",
        "to its full text in one of these files.",
        "",
        "> Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py) every time it moves",
        "> rows. **Change the tool, not this page.**",
        "",
        "| Group | File |",
        "|---|---|",
    ] + rows + [
        "",
        "How many rows each file holds is derived, not typed: `grep -c '^### Row' docs/needs-checking-archive/*.md`",
        "",
    ])


def write(p):
    if p.fatal:
        return COULD_NOT
    if p.problems:
        return REFUSED
    if not p.moves:
        return OK
    if not os.path.isdir(p.archive):
        os.makedirs(p.archive)
    for name in sorted(p.moves):
        path = os.path.join(p.archive, name)
        was = AF._read(path) if os.path.exists(path) else None
        blocks = [b for _, b in sorted(p.moves[name])]
        if was is None:
            opening = _header(name, p.titles.get(name))
        else:
            opening = was.replace(CR + NL, NL).rstrip(NL) + NL + NL
        text = opening + NL.join(blocks)
        AF._put(path, text.replace(NL, p.eol))
    AF._put(os.path.join(p.archive, "README.md"), _readme(p).replace(NL, p.eol))
    folder = os.path.join(os.path.dirname(p.register), NCR.FOLDER_NAME)
    for name in sorted(p.files_after):
        path = os.path.join(folder, name)
        if not os.path.exists(path) or AF._read(path) != p.files_after[name]:
            AF._put(path, p.files_after[name])
    AF._put(p.register, p.index_after)
    return OK


def main(argv=None):
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")
    parser = argparse.ArgumentParser(description="Move done checks out of docs/NEEDS-CHECKING.md.")
    parser.add_argument("--write", action="store_true", help="move them - without this, nothing is written")
    args = parser.parse_args(argv)
    p = plan()
    if args.write and not p.fatal and not p.problems:
        write(p)
    print("NEEDS-CHECKING.md - rows whose check was done")
    print("=" * 45)
    if p.fatal:
        print("  COULD NOT RUN: %s" % p.fatal)
        return COULD_NOT
    moving = sum(len(v) for v in p.moves.values())
    print("  %-22s: %d, into %d file(s)" % ("moved" if args.write and not p.problems else "to move", moving, len(p.moves)))
    print("  moved by a past run    : %d" % p.already)
    print("  held back, still owed  : %d  %s" % (len(p.held), ", ".join(i for i, _ in p.held)))
    b, a = len(p.before.encode("utf-8")), len(p.after.encode("utf-8"))
    if b:
        print("  NEEDS-CHECKING.md      : %s -> %s bytes (%d%%)" % (format(b, ","), format(a, ","), a * 100 // b))
    if p.problems:
        print("  REFUSED - nothing was written:")
        for problem in p.problems[:20]:
            print("    - %s" % problem)
        return REFUSED
    if p.moves:
        print("  the three readers and check-docs' B8 test read the rewritten register the same way")
    print("Written." if args.write else "Nothing was written. Run again with --write.")
    return OK


if __name__ == "__main__":
    sys.exit(main())
