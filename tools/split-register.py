# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Give every section of a register too big to read whole its own file, and
keep the register's page as its index.

    python tools/split-register.py fragment-issues            # what would move - writes nothing
    python tools/split-register.py fragment-issues --write    # move it

Exits 0 when the plan is clean or was written, 1 when a check refused and
NOTHING was written, 2 when the register could not be read at all.

WHY THIS EXISTS
---------------
The owner asked for it on 2026-09-23, the day NEEDS-CHECKING.md was split the
same way: a register a session cannot read whole is a register that gets read
in part. docs/FRAGMENT-ISSUES.md was 777,112 bytes that morning, and sections
5 and 5b - one table row per defect - were three quarters of it. `wc -c` on
the page says how big it is now; `python tools/register-text.py
docs/FRAGMENT-ISSUES.md | wc -c` says the same of the register.

WHAT MOVES, AND WHAT STAYS
--------------------------
Every `## ` section moves whole to its own file in the folder named after the
page - docs/fragment-issues/ - and leaves its heading in the page, in its
place, with one line naming its file. A numbered section `## 1c.` becomes
section-1c.md; any other is named from its heading's opening words and its
date. What stays in full is what REGISTERS names as the page's own rules, and
a section whose name another section has already taken.

A section whose table would still be too big for one file - REGISTERS names
them, sections 5 and 5b here - keeps its own words in the page and moves only
its rows, 25 to a file by row number, the way fragment-issues-archive/ bands
them: rows 26 to 50 of section 5b are in section-5b-rows-026-050.md. A row
with the lines that continue it moves as one, and the page keeps one line
where the table was, naming the files in order. A row written after the last
file's band - the register's rule is that a new row goes at the end of its
section's last file - is moved to its own band by the next run.

PROPOSALS.md - `python tools/split-register.py proposals` - moves every
section whole: `## F23 ...` to f23.md, `## Part A ...` to part-a.md. A
section headed with nothing but a date takes its first five words after
the date as well, so two written on one day get two files.

THE REGISTER IS STILL ONE TEXT, AND THAT IS WHAT IS PROVED
----------------------------------------------------------
Every tool that reads a register reads it through tools/register-text.py,
which puts every file back where its line stands with its links as they were
written. Nothing here writes a byte unless:

  1. the new files, read back, ARE the register - byte for byte;
  2. the real readers, run on a copy of the register laid out the old way and
     on one laid out the new way, answer exactly the same - borrowed, not
     re-implemented: for FRAGMENT-ISSUES, open-defects.py's whole report,
     review-ledger.py's rows of section 5b, and archive-fragment-issues.py's
     plan; for PROPOSALS, the proposals owner-queue.py lists and the open ones
     balance-of-work.py counts;
  3. every link re-pointed on the way out reaches the file it reached before
     (archive-handover.py's own function, which proves each one), and no other
     document links into a heading that would leave the page.

A second run moves nothing. A file already written is left as it is, and a
section written into the page later is moved by the next run.
archive-fragment-issues.py writes through layout_split() below, so the rows
it rewrites go back to the files they came from.

NO BACKSLASH IS TYPED IN THIS FILE, as in archive-fragment-issues.py.
"""

import argparse
import contextlib
import datetime
import importlib.util
import io
import os
import re
import shutil
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)


def _load(name, filename):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


RT = _load("register_text", "register-text.py")
AH = _load("archive_handover", "archive-handover.py")
AF = AH.AF
NL, CR, DASH = AF.NL, AF.CR, AF.DASH
OK, REFUSED, COULD_NOT = AF.OK, AF.REFUSED, AF.COULD_NOT

# Part of every rows file's name, as archive-fragment-issues.py's WIDTH is
# part of every archive file's. Changing it would move rows between files.
WIDTH = 25

NUMBERED = re.compile("^## ([0-9]+[a-z]*(?:-[a-z]+)*)[.] ")
DATE = re.compile("[0-9]{4}-[0-9]{2}-[0-9]{2}")


def _fragment_issues_readers(docs):
    """What FRAGMENT-ISSUES' readers make of the register laid out in DOCS."""
    index = os.path.join(docs, "FRAGMENT-ISSUES.md")
    answers = {}
    od = _load("open_defects_as_served", "open-defects.py")
    od.REGISTER = index
    buffer = io.StringIO()
    with contextlib.redirect_stdout(buffer):
        od.main()
    answers["open-defects"] = buffer.getvalue()
    rl = _load("review_ledger_as_served", "review-ledger.py")
    rl.REGISTER = index
    answers["review-ledger"] = sorted(rl.register_rows() or [])
    af = _load("archive_fragment_issues_as_served", "archive-fragment-issues.py")
    p = af.plan(register=index, archive=os.path.join(docs, af.ARCHIVE_NAME), today="2000-01-01")
    buffer = io.StringIO()
    with contextlib.redirect_stdout(buffer):
        af.report(p, False)
    answers["archive-fragment-issues"] = buffer.getvalue()
    return answers


def _proposals_readers(docs):
    """What PROPOSALS' readers make of the register laid out in DOCS: the
    proposals owner-queue.py puts in front of the owner, and the open ones
    balance-of-work.py counts."""
    top = os.path.dirname(docs)
    answers = {}
    oq = _load("owner_queue_as_served", "owner-queue.py")
    oq.ROOT = top
    answers["owner-queue"] = oq.proposals()
    bw = _load("balance_of_work_as_served", "balance-of-work.py")
    bw.ROOT = top
    answers["balance-of-work"] = bw.proposals_open()
    return answers


# Each register this splits: its page, the words its files are titled with,
# the sections that are the page's own rules, the sections whose rows are
# banded and the name each band's file starts with, the folders its readers
# also read, and the readers themselves.
REGISTERS = {
    "fragment-issues": {
        "index": os.path.join("docs", "FRAGMENT-ISSUES.md"),
        "title": "Fragment issues",
        "stays": ("## Add to this file, do not start another",),
        "rows": (("## 5. ", "section-5-rows"), ("## 5b. ", "section-5b-rows")),
        "beside": ("fragment-issues-archive",),
        "readers": _fragment_issues_readers,
    },
    "proposals": {
        "index": os.path.join("docs", "PROPOSALS.md"),
        "title": "Proposals",
        "stays": (),
        "rows": (),
        "beside": (),
        "readers": _proposals_readers,
    },
}


# ------------------------------------------------------------------ names

def _slug(text):
    return re.sub("[^a-z0-9]+", "-", text.lower()).strip("-")


def label_of(heading):
    """'5b' for '## 5b. HERON'S ...', or the heading's opening words."""
    m = NUMBERED.match(heading)
    if m:
        return m.group(1)
    return re.split(" " + DASH + " ", heading[3:], 1)[0].strip()


def name_of(heading):
    """The file a section moves to, named from its heading: `## 1c.` is
    section-1c.md, and any other is named from its opening words - with the
    date, when one is written before the dash. A heading that opens with
    nothing but a date takes its first five words after the dash as well,
    so two sections written on one day do not ask for the same file."""
    m = NUMBERED.match(heading)
    if m:
        return "section-%s.md" % m.group(1).lower()
    head, _, rest = heading[3:].partition(" " + DASH + " ")
    words = head.split(", ", 1)[0]
    date = DATE.search(head)
    slug = _slug(words)
    if DATE.fullmatch(words.strip()) and rest:
        slug += "-" + _slug(" ".join(rest.split()[:5]))
    elif date and date.group(0) not in words:
        slug += "-" + date.group(0)
    return (slug or "section") + ".md"


def rows_stem(config, heading):
    for prefix, stem in config["rows"]:
        if heading.startswith(prefix):
            return stem
    return None


def bands(lines):
    """[(low, high, lines)] - LINES, a table's rows, cut into files of WIDTH
    by row number. A row keeps the lines that continue it, and the order is
    never changed: a row whose number belongs to an earlier band than the
    one it follows stays where it is."""
    chunks = []
    for line in lines:
        m = RT.ROW.match(line)
        if m:
            band = max(int(m.group(1)) - 1, 0) // WIDTH
            if not chunks or band > chunks[-1][0]:
                chunks.append([band, []])
        elif not chunks:
            chunks.append([0, []])
        chunks[-1][1].append(line)
    return [(b * WIDTH + 1, b * WIDTH + WIDTH, got) for b, got in chunks]


def band_name(stem, low, high):
    return "%s-%03d-%03d.md" % (stem, low, high)


# ----------------------------------------------------------------- headers

def _section_header(title, page, heading, today):
    label = label_of(heading)
    return NL.join([
        "# %s %s %s" % (title, DASH, "section " + label if NUMBERED.match(heading) else label),
        "",
        "> One section of [the register](../%s), in its own file since %s so that it can be read" % (page, today),
        "> alone. **The register's rules, and every section's place in it, are on that page.** What is",
        "> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)",
        "> reads it back into the register for every tool that reads the register, so it is seen exactly",
        "> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).",
        "",
        "",
    ])


def _rows_header(title, page, heading, low, high, today):
    label = label_of(heading)
    return NL.join([
        "# %s %s section %s, rows %d to %d" % (title, DASH, label, low, high),
        "",
        "> Rows %d to %d of section %s of [the register](../%s), in their own file since %s so that"
        % (low, high, label, page, today),
        "> they can be read alone. **The section's own rules are on that page, under its heading.** A new",
        "> row goes at the end of the section's last file, whatever its number, and the next run of",
        "> [`tools/split-register.py`](../../tools/split-register.py) moves it to the file its number",
        "> belongs to. [`tools/register-text.py`](../../tools/register-text.py) reads every row back into",
        "> the register for every tool that reads it, so a row here is seen exactly as it was seen there.",
        "",
        "",
    ])


def header_of(text):
    """What an existing file says above its section or its table, kept as it
    is so that a second run does not rewrite a file's date."""
    held = text.replace(CR + NL, NL).split(NL)
    spans, first = RT.sections(held)
    if spans:
        return NL.join(held[:first]) + NL
    table = RT.table_of(held)
    if table:
        return NL.join(held[:table[0]]) + NL
    return None


# ------------------------------------------------------------------- split

def split_by(text, layout, banded, config, headers, today, index):
    """(page text, {file: text}, trouble) - TEXT, the register as one text,
    with each section LAYOUT maps by its heading line moved to its file, and
    the rows of each section in BANDED moved to their bands' files."""
    folder_name = RT.folder_of(index)
    folder = os.path.join(os.path.dirname(index), folder_name)
    page = os.path.basename(index)
    lines = text.replace(CR + NL, NL).split(NL)
    spans, first = RT.sections(lines)
    fenced = RT.fenced_lines(lines)
    moved = {}
    for start, end in spans:
        name = layout.get(lines[start])
        if not name:
            continue
        for k in range(start, end):
            h = AH.HEADING.match(lines[k])
            if h and k not in fenced:
                moved.setdefault(AH.anchor_of(h.group(1)), name)
    trouble, out, files = [], [], {}
    if first:
        out.append(AH._repoint(NL.join(lines[:first]), None, moved, index, folder, trouble,
                               "the register's opening"))
    for start, end in spans:
        heading = lines[start]
        name = layout.get(heading)
        table = RT.table_of(lines, start, end) if heading in banded else None
        if name:
            k = RT.last_filled(lines, start, end)
            out.append(NL.join([heading, "", RT.file_line(folder_name, name)] + lines[k:end]))
            body = AH._repoint(NL.join(lines[start:k]), name, moved, index, folder, trouble, heading)
            files[name] = (headers.get(name) or _section_header(config["title"], page, heading, today)) + body + NL
            continue
        if not table:
            out.append(AH._repoint(NL.join(lines[start:end]), None, moved, index, folder, trouble, heading))
            continue
        top, bottom = table
        chunks = bands(lines[top + 2:bottom])
        stem = rows_stem(config, heading)
        names = [band_name(stem, low, high) for low, high, _ in chunks]
        labels = ["%03d-%03d" % (low, high) for low, high, _ in chunks]
        kept = lines[start:top] + [RT.rows_line(folder_name, names, labels)] + lines[bottom:end]
        out.append(AH._repoint(NL.join(kept), None, moved, index, folder, trouble, heading))
        for (low, high, chunk), name in zip(chunks, names):
            body = AH._repoint(NL.join(lines[top:top + 2] + chunk), name, moved, index, folder, trouble,
                               "%s, rows %d to %d" % (heading, low, high))
            opening = headers.get(name) or _rows_header(config["title"], page, heading, low, high, today)
            files[name] = opening + body + NL
    return NL.join(out), files, trouble


def _served(index, index_text, files):
    """A reader that serves the register from memory: the page, and FILES."""
    folder = RT.path_in(index, "")

    def read(path):
        path = path.replace(os.sep, "/")
        if path == index.replace(os.sep, "/"):
            return index_text
        if path.startswith(folder):
            return files.get(path[len(folder):])
        return None
    return read


def _lay_out(top, page, folder_name, index_text, files, beside, source):
    """The register written into TOP/docs/ the way INDEX_TEXT and FILES lay it
    out, with the folders its readers also read copied beside it."""
    docs = os.path.join(top, "docs")
    os.makedirs(os.path.join(docs, folder_name))
    with io.open(os.path.join(docs, page), "wb") as out:
        out.write(index_text.encode("utf-8"))
    for name, text in files.items():
        with io.open(os.path.join(docs, folder_name, name), "wb") as out:
            out.write(text.encode("utf-8"))
    for name in beside:
        if os.path.isdir(os.path.join(source, name)):
            shutil.copytree(os.path.join(source, name), os.path.join(docs, name))
    return docs


def readers(config, index, layouts):
    """What the register's readers make of each of LAYOUTS - [(page text,
    {file: text})] - each laid out in a folder of its own."""
    answers = []
    for index_text, files in layouts:
        top = tempfile.mkdtemp(prefix="heron-split-")
        try:
            docs = _lay_out(top, os.path.basename(index), RT.folder_of(index), index_text, files,
                            config.get("beside", ()), os.path.dirname(index))
            try:
                answers.append(config["readers"](docs))
            except RT.RegisterBroken as broken:
                answers.append({"register": "RegisterBroken: %s" % broken})
        finally:
            shutil.rmtree(top, ignore_errors=True)
    return answers


def inbound(root, index, folder, anchors):
    """Links from any other document into one of ANCHORS on the page."""
    found = []
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if not d.startswith(".") and d not in ("bin", "obj", "node_modules")]
        if AF._same(dirpath, folder):
            continue
        for filename in filenames:
            path = os.path.join(dirpath, filename)
            if not filename.endswith(".md") or AF._same(path, index):
                continue
            try:
                text = AF._read(path)
            except (IOError, OSError, UnicodeDecodeError):
                continue
            for m in AF.LINK.finditer(text):
                target, mark, fragment = m.group(2).partition("#")
                if (mark and fragment in anchors and target and not target.startswith(("http", "/"))
                        and AF._same(os.path.join(dirpath, target), index)):
                    found.append("%s links to %s#%s, a heading that would leave the page"
                                 % (os.path.relpath(path, root), os.path.basename(index), fragment))
    return found


class Plan(object):
    def __init__(self, key, index, folder, today):
        self.key, self.index, self.folder, self.today = key, index, folder, today
        self.fatal = None
        self.problems = []
        self.eol = NL
        self.before = ""            # the page as it stands
        self.files_before = {}      # its files as they stand
        self.joined = ""            # the register as one text
        self.after = ""
        self.files_after = {}
        self.moving = []            # (heading, file) moved by this run
        self.banding = []           # (heading, [file]) rows files written by this run
        self.staying = []           # (heading, why)
        self.already = 0


def _current(index, before, files_before):
    """The layout on disk: {heading: file} for each section the page names a
    file for, and the headings under which it names rows files."""
    folder_name = RT.folder_of(index)
    lines = before.replace(CR + NL, NL).split(NL)
    spans, _ = RT.sections(lines)
    fenced = RT.fenced_lines(lines)
    layout, banded = {}, set()
    for start, end in spans:
        name = RT.stub_of(lines, start, end, folder_name)
        if name:
            layout[lines[start]] = name
            continue
        if any(RT.rows_names(lines[k], folder_name) for k in range(start, end) if k not in fenced):
            banded.add(lines[start])
    return layout, banded


def _headers(files):
    out = {}
    for name, text in files.items():
        found = header_of(text)
        if found:
            out[name] = found
    return out


def _files_in(folder):
    found = {}
    if os.path.isdir(folder):
        for name in sorted(os.listdir(folder)):
            if name.endswith(".md"):
                found[name] = AF._read(os.path.join(folder, name))
    return found


def plan(key="fragment-issues", index=None, today=None, root=ROOT, check_others=True):
    config = REGISTERS[key]
    index = index or os.path.join(root, config["index"])
    folder = os.path.join(os.path.dirname(index), RT.folder_of(index))
    p = Plan(key, index, folder, today or datetime.date.today().isoformat())
    try:
        p.before = AF._read(index)
    except (IOError, OSError) as error:
        p.fatal = "could not read %s: %s" % (index, error)
        return p
    lines, p.eol = AF._split(p.before)
    if lines is None:
        p.fatal = "%s mixes CRLF and LF line endings" % os.path.basename(index)
        return p
    p.files_before = _files_in(folder)
    try:
        p.joined = RT.register_text(index, read=_served(index, p.before, p.files_before)).replace(CR + NL, NL)
    except RT.RegisterBroken as broken:
        p.fatal = str(broken)
        return p

    layout, banded = _current(index, p.before, p.files_before)
    p.already = len(layout)
    taken = set(p.files_before)
    joined = p.joined.split(NL)
    fenced = RT.fenced_lines(joined)
    spans, _ = RT.sections(joined)
    wanted = set()
    for start, end in spans:
        heading = joined[start]
        if heading in layout:
            continue
        if heading in config["stays"]:
            p.staying.append((heading, "the page's own rules"))
            continue
        if rows_stem(config, heading):
            if RT.table_of(joined, start, end) is None:
                p.staying.append((heading, "its rows are banded, and it has no table"))
            else:
                wanted.add(heading)
            continue
        name = name_of(heading)
        if name in taken:
            p.staying.append((heading, "%s already holds another section" % name))
            continue
        layout[heading] = name
        taken.add(name)
        p.moving.append((heading, name))

    after, files, trouble = split_by(p.joined, layout, banded | wanted, config,
                                     _headers(p.files_before), p.today, index)
    p.problems.extend(trouble)
    p.after = after.replace(NL, p.eol)
    p.files_after = dict((n, t.replace(NL, p.eol)) for n, t in files.items())
    for heading in sorted(banded | wanted):
        written = [n for n in sorted(files) if n.startswith(rows_stem(config, heading) + "-")
                   and p.files_after[n] != p.files_before.get(n)]
        if written:
            p.banding.append((heading, written))
    if p.after == p.before and all(p.files_after[n] == p.files_before.get(n) for n in p.files_after):
        return p
    stale = sorted(set(p.files_before) - set(p.files_after))
    if stale:
        p.problems.append("the folder holds %s, which the new layout does not name - look at %s by "
                          "hand before running this again" % (", ".join(stale), "it" if len(stale) == 1 else "them"))

    back = RT.register_text(index, read=_served(index, p.after, p.files_after)) or ""
    if back.replace(CR + NL, NL) != p.joined:
        p.problems.append("the new files, read back, are not the register byte for byte")
    before, now = readers(config, index, [(p.before, p.files_before), (p.after, p.files_after)])
    for name in before:
        if before[name] != now.get(name):
            p.problems.append("%s would read the register differently" % name)
    if check_others:
        # A section's own '## ' heading stays on the page, so a link to it
        # still lands. Only the headings under it leave.
        leaving, subs = set(h for h, _ in p.moving), set()
        for start, end in spans:
            if joined[start] in leaving:
                for k in range(start + 1, end):
                    h = AH.HEADING.match(joined[k])
                    if h and k not in fenced:
                        subs.add(AH.anchor_of(h.group(1)))
        p.problems.extend(inbound(root, index, folder, subs))
    return p


def write(p):
    if p.fatal:
        return COULD_NOT
    if p.problems:
        return REFUSED
    if not os.path.isdir(p.folder):
        os.makedirs(p.folder)
    for name in sorted(p.files_after):
        if p.files_after[name] != p.files_before.get(name):
            AF._put(os.path.join(p.folder, name), p.files_after[name])
    if p.after != p.before:
        AF._put(p.index, p.after)
    return OK


def layout_split(index, joined, today=None, key=None):
    """For a tool that rewrote the register as one text -
    archive-fragment-issues.py - the files that text becomes, keeping the
    layout on disk as it is: a section the page already names a file for goes
    to that file, a section whose rows are already banded is banded again,
    and everything else stays where it is. (page text, {file: text}, trouble)."""
    page = os.path.basename(index)
    key = key or next(k for k, c in REGISTERS.items() if os.path.basename(c["index"]) == page)
    config = REGISTERS[key]
    folder = os.path.join(os.path.dirname(index), RT.folder_of(index))
    before = AF._read(index)
    files_before = _files_in(folder)
    layout, banded = _current(index, before, files_before)
    eol = CR + NL if CR + NL in before else NL
    after, files, trouble = split_by(joined, layout, banded, config, _headers(files_before),
                                     today or datetime.date.today().isoformat(), index)
    return after.replace(NL, eol), dict((n, t.replace(NL, eol)) for n, t in files.items()), trouble


def report(p, writing):
    page = os.path.basename(p.index)
    print("%s - one file per section" % page)
    print("=" * (len(page) + 25))
    if p.fatal:
        print("  COULD NOT RUN: %s" % p.fatal)
        return
    folder = RT.folder_of(p.index)
    done = writing and not p.problems
    print("  %-26s: %d, into %s/" % ("sections moved" if done else "sections to move", len(p.moving), folder))
    print("  %-26s: %d" % ("moved by a past run", p.already))
    for heading, names in p.banding:
        print("  %-26s: %d file(s) - %s" % (("rows written" if done else "rows to write"), len(names),
                                             heading[3:60]))
    print("  %-26s: %d" % ("staying on the page", len(p.staying)))
    for heading, why in p.staying:
        print("    %-58s %s" % (heading[3:61], why))
    b, a = len(p.before.encode("utf-8")), len(p.after.encode("utf-8"))
    if b:
        print("  %-26s: %s -> %s bytes (%d%%)" % (page, format(b, ","), format(a, ","), a * 100 // b))
    if p.problems:
        print("  REFUSED - nothing was written:")
        for problem in p.problems[:20]:
            print("    - %s" % problem)
        return
    if p.moving or p.banding:
        print("  read back, the files are the register byte for byte, and its readers read it exactly")
        print("  as they do now")
        print("Written." if writing else "Nothing was written. Run again with --write.")
    else:
        print("Nothing to move.")


def main(argv=None):
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass
    parser = argparse.ArgumentParser(description="Give every section of a register its own file.")
    parser.add_argument("register", choices=sorted(REGISTERS), help="which register")
    parser.add_argument("--write", action="store_true", help="move them - without this, nothing is written")
    args = parser.parse_args(argv)
    p = plan(args.register)
    if args.write and not p.fatal and not p.problems:
        write(p)
    report(p, args.write)
    if p.fatal:
        return COULD_NOT
    if p.problems:
        return REFUSED
    return OK


if __name__ == "__main__":
    sys.exit(main())
