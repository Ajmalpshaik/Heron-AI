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

PROPOSALS.md - `python tools/split-register.py proposals` - and
OPEN-QUESTIONS.md - `... open-questions` - move every section whole:
`## F23 ...` to f23.md, `## Tier 3 ...` to tier-3.md. A section headed with
nothing but a date takes its first five words after the date as well, so
two written on one day get two files.

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
     balance-of-work.py counts; for OPEN-QUESTIONS, the questions owner-queue.py
     lists, and check-docs.py's count of them and of its Progress line - run
     whole, on each copy;
  3. every link re-pointed on the way out reaches the file it reached before
     (archive-handover.py's own function, which proves each one), and no other
     document links into a heading that would leave the page.

A LINK THAT NAMES A ROW THIS RUN MOVES FOLLOWS IT. A link whose words name
one row of section 5b - [row 5b-307](...) - and whose target is the rows
file the row is leaving is pointed at the file it goes to; only the file's
name in the link changes. Inside the register that is done to the register
as one text, before it is cut into files, so the read-back in 1 compares
the files with that text byte for byte - and the text with the register as
it stood, with every rows file's name left out, so nothing but those names
changed. The readers in 2 read the old layout with the same links
re-pointed. Every other document git tracks - .md, .yaml, .yml and .py,
or every one under the folder when there is no git to ask - is re-pointed
as a separate pass, written after the register. A link in fenced code or
an inline code span is an example and is left as it is, and so is one whose
words name two rows. Until 2026-10-09 neither half was done: when the rows
after 200 were re-banded on 2026-10-08, every link naming one of them still
pointed at the file it had left, and a one-off script put them right
(FRAGMENT-ISSUES row 5b-381). check-docs.py fails on a link left that way.

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
import subprocess
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


def _between(text, start, end):
    at = text.find(start)
    return text[at:text.find(end, at)] if at >= 0 else None


def _open_questions_readers(docs):
    """What OPEN-QUESTIONS' readers make of the register laid out in DOCS:
    the questions owner-queue.py puts in front of the owner, and what
    check-docs.py counts - run whole, in a folder holding nothing but this
    register, and read at its sections 4 and 6, where the questions are."""
    top = os.path.dirname(docs)
    answers = {}
    oq = _load("owner_queue_as_served", "owner-queue.py")
    oq.ROOT = top
    answers["owner-queue"] = oq.open_questions()
    run = subprocess.run([sys.executable, os.path.join(HERE, "check-docs.py")], cwd=top,
                         stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    text = run.stdout.decode("utf-8", "replace")
    answers["check-docs, its questions"] = _between(text, "=== 4. QUESTIONS ===", "=== 5.")
    answers["check-docs, its Progress line"] = _between(text, "=== 6. THE PROGRESS LINE ===", "=== 7.")
    return answers


# Each register this splits: its page, the words its files are titled with,
# the sections that are the page's own rules, the sections whose rows are
# banded and the name each band's file starts with, the sections whose rows
# a link names by label - '5b-307' - with their bands' names, the folders its
# readers also read, and the readers themselves. Section 5's rows are called
# by their bare number, which no link's words can be read for.
REGISTERS = {
    "fragment-issues": {
        "index": os.path.join("docs", "FRAGMENT-ISSUES.md"),
        "title": "Fragment issues",
        "stays": ("## Add to this file, do not start another",),
        "rows": (("## 5. ", "section-5-rows"), ("## 5b. ", "section-5b-rows")),
        "named": (("5b", "section-5b-rows"),),
        "beside": ("fragment-issues-archive",),
        "readers": _fragment_issues_readers,
    },
    "proposals": {
        "index": os.path.join("docs", "PROPOSALS.md"),
        "title": "Proposals",
        "stays": (),
        "rows": (),
        "named": (),
        "beside": (),
        "readers": _proposals_readers,
    },
    "open-questions": {
        "index": os.path.join("docs", "OPEN-QUESTIONS.md"),
        "title": "Open questions",
        "stays": (),
        "rows": (),
        "named": (),
        "beside": (),
        "readers": _open_questions_readers,
    },
}

# The documents whose links follow a row that moved, and the folders a walk
# of a tree with no git leaves out because git would never track them.
RELINKED = (".md", ".yaml", ".yml", ".py")
UNWALKED = (".git", "worktrees", "bin", "obj", "node_modules", "__pycache__")


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

def _beside(text, folder_name):
    """TEXT, going into one of the register's files, with every link into the
    folder written as a link to the file beside it. The way out puts ../ in
    front of a page link, so fragment-issues/x.md arrives as
    ../fragment-issues/x.md: it reaches the right file, but nobody writes it
    that way, and a file that read section-6.md before a run should read the
    same after it. register-text.py reads both back to the same page link."""
    return text.replace("](../" + folder_name + "/", "](")


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
            body = _beside(AH._repoint(NL.join(lines[start:k]), name, moved, index, folder, trouble,
                                       heading), folder_name)
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
            body = _beside(AH._repoint(NL.join(lines[top:top + 2] + chunk), name, moved, index, folder,
                                       trouble, "%s, rows %d to %d" % (heading, low, high)), folder_name)
            opening = headers.get(name) or _rows_header(config["title"], page, heading, low, high, today)
            files[name] = opening + body + NL
    return NL.join(out), files, trouble


# --------------------------------------------------- a link that names a row

def _holders(files, stem):
    """{row: file} for every rows file in FILES whose name starts with STEM."""
    held = {}
    for name in sorted(files):
        if name.startswith(stem + "-"):
            for number in RT.rows_held(files[name]):
                held[number] = name
    return held


def moved_rows(config, files_before, files_after):
    """{label: {row: (the file it is in, the file it goes to)}} - each row
    this run moves to another rows file, for each section whose rows a link
    names by label."""
    out = {}
    for label, stem in config.get("named", ()):
        was, now = _holders(files_before, stem), _holders(files_after, stem)
        out[label] = dict((n, (was[n], now[n])) for n in sorted(was) if n in now and was[n] != now[n])
    return out


def relink(text, where, folder, moved, said, done):
    """TEXT, sitting in the folder WHERE, with every link that names one row
    MOVED moves and points at the file that row is leaving pointed at the
    file it goes to. Only the file's name in the link changes - the folders
    in front of it, its anchor and its words stay as written - so one rule
    serves a rows file, the page, the register as one text and any other
    document. DONE gets (SAID, label, row, from, to) for each link changed."""
    for label in sorted(moved):
        rows = moved[label]
        if not rows:
            continue
        edits = []
        for number, start, end in RT.row_links(text, label):
            if number not in rows:
                continue
            left, went = rows[number]
            target, mark, fragment = text[start:end].partition("#")
            name = target.rsplit("/", 1)[-1]
            if name != left or not AF._same(os.path.join(where, target), os.path.join(folder, left)):
                continue
            edits.append((start, end, target[:len(target) - len(name)] + went + mark + fragment))
            done.append((said, label, number, left, went))
        for start, end, new in reversed(edits):
            text = text[:start] + new + text[end:]
    return text


def _band_blind(text, config):
    """TEXT with the name of every rows file a link may name written alike."""
    for _, stem in config.get("named", ()):
        text = re.sub(re.escape(stem) + "-[0-9]{3}-[0-9]{3}[.]md", stem + "-NNN-NNN.md", text)
    return text


def _documents(root):
    """([path], how they were listed) - every file under ROOT whose links
    follow a moved row: what git tracks when git answers, and otherwise a
    walk of the folder that leaves out what git would never track."""
    try:
        run = subprocess.run(["git", "ls-files", "-z"], cwd=root,
                             stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    except OSError:
        run = None
    if run is not None and run.returncode == 0:
        names = [n for n in run.stdout.decode("utf-8", "surrogateescape").split(chr(0)) if n]
        return ([os.path.join(root, *n.split("/")) for n in names if n.endswith(RELINKED)],
                "what git tracks")
    found = []
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = sorted(d for d in dirnames if d not in UNWALKED)
        found.extend(os.path.join(dirpath, f) for f in sorted(filenames) if f.endswith(RELINKED))
    return found, "every file in the folder - there is no git here to ask what it tracks"


def elsewhere(root, index, folder, moved, done):
    """({path: (its text, its text re-pointed)}, how they were listed) - each
    document outside the register with a link that names a row MOVED moves,
    pointing at the file the row is leaving."""
    paths, how = _documents(root)
    labels = [label + "-" for label in moved if moved[label]]
    out = {}
    for path in paths:
        if AF._same(path, index) or AF._same(os.path.dirname(path), folder):
            continue
        try:
            text = AF._read(path)
        except (IOError, OSError, UnicodeDecodeError):
            continue
        if not any(label in text for label in labels):
            continue
        new = relink(text, os.path.dirname(path), folder, moved,
                     os.path.relpath(path, root).replace(os.sep, "/"), done)
        if new != text:
            out[path] = (text, new)
    return out, how


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
        self.joined_after = ""      # the same, with each link that names a moved row following it
        self.moved = {}             # {label: {row: (from, to)}} rows moved to another rows file
        self.relinked = []          # (where, label, row, from, to) re-pointed inside the register
        self.others = {}            # {path: (text, text re-pointed)} documents outside it
        self.others_relinked = []   # (path, label, row, from, to) re-pointed in them
        self.others_listed = ""     # how those documents were listed
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
    # A link that names a row this split moves follows it: re-pointed in the
    # register as one text, which is then cut again - re-pointing changes no
    # row's place, so the files differ only in those links.
    p.moved = moved_rows(config, p.files_before, files)
    p.joined_after = relink(p.joined, os.path.dirname(index), folder, p.moved, "the register", p.relinked)
    if p.relinked:
        after, files, trouble = split_by(p.joined_after, layout, banded | wanted, config,
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

    back = (RT.register_text(index, read=_served(index, p.after, p.files_after)) or "").replace(CR + NL, NL)
    if back != p.joined_after:
        p.problems.append("the new files, read back, are not the register byte for byte")
    if _band_blind(back, config) != _band_blind(p.joined, config):
        p.problems.append("the new files, read back, differ from the register in more than the rows "
                          "file a link names")
    # The readers are asked whether the move changed anything else, so the
    # old layout they read has the same links re-pointed in place.
    was = (p.before, p.files_before)
    if p.relinked:
        was = (relink(p.before, os.path.dirname(index), folder, p.moved, None, []),
               dict((n, relink(t, folder, folder, p.moved, None, [])) for n, t in p.files_before.items()))
    before, now = readers(config, index, [was, (p.after, p.files_after)])
    for name in before:
        if before[name] != now.get(name):
            p.problems.append("%s would read the register differently" % name)
    if any(p.moved.values()):
        p.others, p.others_listed = elsewhere(root, index, folder, p.moved, p.others_relinked)
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
    # The other documents last, as their own pass: each changes only in the
    # rows file its links name, and a run cut short leaves the register whole.
    for path in sorted(p.others):
        AF._put(path, p.others[path][1])
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
    if p.relinked or p.others:
        print("  %-26s: %d in the register, %d in %d other document(s), read from %s"
              % ("links that follow a row" if done else "links to follow a row", len(p.relinked),
                 len(p.others_relinked), len(p.others), p.others_listed or "nowhere"))
        for path in sorted(set(one[0] for one in p.others_relinked))[:20]:
            print("    %s" % path)
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
