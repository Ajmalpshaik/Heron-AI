# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
A register that was split into files, read back as one text.

    python tools/register-text.py docs/FRAGMENT-ISSUES.md    # print the whole register

A register too big to read whole keeps an INDEX - its own page, such as
docs/FRAGMENT-ISSUES.md - and a folder beside it with the same name in lower
case, docs/fragment-issues/. tools/split-register.py writes both. Where a
section was, the index keeps its heading and one line naming its file:

    **Its own file:** [`fragment-issues/section-1c.md`](fragment-issues/section-1c.md)

A section whose table is too big for one file keeps its own words in the
index, and where the table was, one line naming the files its rows are in,
in order:

    **Its rows, in order:** [001-025](fragment-issues/section-5b-rows-001-025.md) · ...

A ROW WRITTEN FROM THE CUT-OVER ON HAS A FILE OF ITS OWN. Until 2026-11-12
every new row of section 5b was appended to the end of one rows file, so two
branches that each added a row changed the same lines of the same file, and
git stopped at almost every merge of parallel work. Under the line above, the
index now keeps one more line, written once and never added to:

    **Its rows from 1576 on, one file each:** [`fragment-issues/row-5b-NNNN.md`](fragment-issues/)

and row 5b-1585 is fragment-issues/row-5b-1585.md: the table's two header
lines and that row's own line, in the cells every rows file uses, so a row
moves between the two layouts by copying its line. THE FOLDER IS THE LIST.
No line anywhere names a row's file - a line every branch added to would be
the same conflict in a new place - so the reader lists the folder, and two
branches that take the same number collide in git as two files of one name.
The rows come back after the rows files' rows, in number order; a row from
the cut-over on that an older branch still appended to a rows file is read
in that order too, so the text is the same wherever the row is kept.

register_text() puts every file back where its line stands, with its links
written as they were in the one file - so a tool that read the register as
one text reads exactly the text it read before, and prints exactly what it
printed. tools/needs-checking-register.py is the same reader for
NEEDS-CHECKING.md, written first, and its line for a group has this shape.

This is only the READER. It imports nothing but the standard library, so a
tool that reads a register never breaks because the tool that WRITES it
changed.

IT ALSO SAYS WHICH LINKS NAME A ROW, AND WHICH ROWS A FILE HOLDS -
row_links() and rows_held(). split-register.py re-points a link that names
a row it moved, and check-docs.py fails on one that reaches a rows file not
holding its row, so the writer and the gate read a link by one rule here
rather than two that drift apart (FRAGMENT-ISSUES row 5b-381).

A FILE THAT IS MISSING IS NOT SKIPPED. A register that quietly drops a
section is the failure a register exists to prevent, so a line naming a file
that is not there, a section's file that does not open with the heading that
names it, or a rows file with no table, raises RegisterBroken rather than
returning a shorter register. So does a row's own file that holds any row
but the one its name says, a heading that names another row or none, or
any text under its table, a file named like one that is not one
(row-5b-1585-copy.md would otherwise be a row nobody reads), and a caller
that serves the register from memory and gave no way to list the folder. An index that names no file is returned as it
is: that is how a register that was never split, and every register a suite
builds for itself, is read.

`python tools/register-text.py docs/FRAGMENT-ISSUES.md | grep ...` searches
the whole register at once, the way `grep docs/FRAGMENT-ISSUES.md` did
before it was split.

NO BACKSLASH IS TYPED IN THIS FILE. The patterns are built from chr(92) and
character classes, as archive-fragment-issues.py's are.
"""

import bisect
import io
import os
import re
import sys

NL = chr(10)
CR = chr(13)
BS = chr(92)
FENCE = "`" * 3
DOT = chr(0xB7)

# The two lines split-register.py writes, and nothing else writes.
FILE_KEY = "**Its own file:** "
ROWS_KEY = "**Its rows, in order:** "
ROWS_GAP = " " + DOT + " "
# The line that says a section's newer rows are one file each. It is written
# once - by hand on 2026-11-12, and by split-register.py when it writes the
# page again - and it names the first such row and the files' stem, never a
# file: "**Its rows from 1576 on, one file each:** [`f/row-5b-NNNN.md`](f/)".
EACH_KEY = "**Its rows from "
EACH_MID = " on, one file each:** "
EACH_ANY = "NNNN"
NAME = "[a-z0-9][a-z0-9-]*[.]md"

# check-docs.py's link pattern, [text](target), built as archive-fragment-issues.py builds it.
LINK = re.compile(BS + "[([^" + BS + "]]*)" + BS + "]" + BS + "(([^)]+)" + BS + ")")
# open-defects.py's row pattern: a table line whose first cell is a number, bold or not.
ROW = re.compile("^[|]" + BS + "s*[*]{0,2}([0-9]+)[*]{0,2}" + BS + "s*[|]")
# The line under a table's header.
RULE = re.compile("^[|](" + BS + "s*:?-+:?" + BS + "s*[|])+" + BS + "s*$")
# A run of backticks: the edge of an inline code span, or a backtick.
TICKS = re.compile("`+")


class RegisterBroken(Exception):
    """The index names a file that is missing, or a file that does not hold
    what the index says it holds."""


def folder_of(index):
    """The folder a register's files live in: the index's own name in lower
    case, beside it. docs/FRAGMENT-ISSUES.md -> fragment-issues."""
    name = os.path.basename(index.replace(os.sep, "/"))
    return name[:-len(".md")].lower() if name.endswith(".md") else name.lower()


def _base(index):
    index = index.replace(os.sep, "/")
    return index.rsplit("/", 1)[0] + "/" if "/" in index else ""


def path_in(index, name):
    """The path of NAME in INDEX's folder, written the way INDEX was."""
    return _base(index) + folder_of(index) + "/" + name


def file_line(folder, name):
    return "%s[`%s/%s`](%s/%s)" % (FILE_KEY, folder, name, folder, name)


def rows_line(folder, names, labels):
    return ROWS_KEY + ROWS_GAP.join("[%s](%s/%s)" % (label, folder, name)
                                    for name, label in zip(names, labels))


def _file_pattern(folder):
    return re.compile("^" + re.escape(FILE_KEY + "[`" + folder + "/") + "(" + NAME + ")"
                      + re.escape("`](" + folder + "/") + "(" + NAME + ")" + re.escape(")") + "$")


def _rows_piece(folder):
    return re.compile("^" + BS + "[[^" + BS + "]]+" + BS + "]" + BS + "("
                      + re.escape(folder + "/") + "(" + NAME + ")" + BS + ")$")


def rows_names(line, folder):
    """The files a rows line names, in order - or None when LINE is not one."""
    if not line.startswith(ROWS_KEY):
        return None
    piece = _rows_piece(folder)
    names = []
    for one in line[len(ROWS_KEY):].split(ROWS_GAP):
        m = piece.match(one)
        if not m:
            return None
        names.append(m.group(1))
    return names or None


def each_line(folder, stem, cut):
    """The line that says rows from CUT on are one file each, named STEM-<n>.md."""
    return "%s%d%s[`%s/%s-%s.md`](%s/)" % (EACH_KEY, cut, EACH_MID, folder, stem, EACH_ANY, folder)


def _each_pattern(folder):
    return re.compile("^" + re.escape(EACH_KEY) + "([1-9][0-9]*)" + re.escape(EACH_MID + "[`" + folder + "/")
                      + "([a-z0-9][a-z0-9-]*)" + re.escape("-" + EACH_ANY + ".md`](" + folder + "/)") + "$")


def each_of(line, folder):
    """(the first row that has a file of its own, the stem of those files'
    names) when LINE is the line that says so - or None."""
    if not line.startswith(EACH_KEY):
        return None
    m = _each_pattern(folder).match(line)
    return (int(m.group(1)), m.group(2)) if m else None


def row_name(stem, number):
    """The file row NUMBER has to itself: row-5b-1585.md."""
    return "%s-%d.md" % (stem, number)


def row_number(name, stem):
    """The row the file NAME is for, or None when NAME is not a row's own
    file of STEM. The number is written as a person writes it - no leading
    zero - so one row has one name, and two branches that take it collide."""
    m = re.match("^" + re.escape(stem) + "-([1-9][0-9]*)[.]md$", name)
    return int(m.group(1)) if m else None


def row_files(names, stem, folder="", page=""):
    """{row: file} for the rows' own files among NAMES, the names in the
    register's folder. A name that starts like one and is not one raises
    RegisterBroken: skipped, it would be a row no tool reads."""
    found = {}
    for name in sorted(names):
        if not name.startswith(stem + "-"):
            continue
        number = row_number(name, stem)
        if number is None:
            raise RegisterBroken("%s%s is named like a row's own file and is not one - a row's file "
                                 "is %s-<its number>.md, and %s reads no other"
                                 % (folder + "/" if folder else "", name, stem, page or "the register"))
        found[number] = name
    return found


def row_groups(lines):
    """[(row number or None, [lines])] - a table's rows, each with the lines
    that continue it. Lines before the first row have no number."""
    groups = []
    for line in lines:
        m = ROW.match(line)
        if m:
            groups.append((int(m.group(1)), [line]))
        elif groups:
            groups[-1][1].append(line)
        else:
            groups.append((None, [line]))
    return groups


def settled(body, cut, more=()):
    """BODY, a table's rows as its rows files hold them, with every row from
    CUT on taken out of its place and put after the rest, in number order,
    together with MORE - [(number, [lines])], the rows that have files of
    their own. Rows before CUT stay exactly as written. A number held twice
    stays twice, the rows file's first: which one is the row is not for a
    reader to choose, and tests/test_open_defects.py refuses the pair."""
    low, high = [], []
    for number, got in row_groups(body):
        (high if number is not None and number >= cut else low).append((number, got))
    high = sorted(high + list(more), key=lambda one: one[0])
    return [line for _, got in low + high for line in got]


def _lines(text):
    return text.replace(CR + NL, NL).split(NL)


def fenced_lines(lines):
    """Indexes of the lines inside a fenced code block, fences included - the
    same rule as archive-handover.py's. A '## ' line there is an example."""
    inside, out = False, set()
    for i, line in enumerate(lines):
        if line.lstrip().startswith(FENCE):
            out.add(i)
            inside = not inside
        elif inside:
            out.add(i)
    return out


def code_spans(line):
    """[(start, end)] of the inline code spans in LINE: a run of backticks to
    the next run of the same length. A run with no partner on the line is a
    backtick, not code. A span carried on to the next line is not seen, and
    a link inside one is read as prose."""
    runs = [m.span() for m in TICKS.finditer(line)]
    spans, i = [], 0
    while i < len(runs):
        width = runs[i][1] - runs[i][0]
        partner = next((k for k in range(i + 1, len(runs))
                        if runs[k][1] - runs[k][0] == width), None)
        if partner is None:
            i += 1
            continue
        spans.append((runs[i][0], runs[partner][1]))
        i = partner + 1
    return spans


def row_links(text, label):
    """[(row, start, end)] for every link in TEXT whose words name exactly one
    row of section LABEL - '[row 5b-307](...)' - with TEXT[start:end] its
    target as written. A link in fenced code or in an inline code span is an
    example, not a link, and is left out; so is one whose words name two
    rows, or none - which row it means is not for a tool to choose.

    THE LINKS ARE FOUND IN THE WHOLE TEXT, NOT LINE BY LINE, so a link whose
    words a line break wraps - '[row' at the end of one line and '5b-322](...)'
    at the start of the next - is read as check-docs.py's section 1 reads it.
    Where it starts decides whether it is in fenced code or a code span."""
    named = re.compile("(?<![0-9A-Za-z-])" + re.escape(label) + "-([0-9]+)(?![0-9])")
    lines = text.split(NL)
    fenced = fenced_lines(lines)
    starts, at = [], 0
    for line in lines:
        starts.append(at)
        at += len(line) + 1
    spans, found = {}, []
    for m in LINK.finditer(text):
        rows = set(int(n) for n in named.findall(m.group(1)))
        if len(rows) != 1:
            continue
        i = bisect.bisect_right(starts, m.start()) - 1
        if i in fenced:
            continue
        if i not in spans:
            spans[i] = code_spans(lines[i])
        column = m.start() - starts[i]
        if any(start <= column < end for start, end in spans[i]):
            continue
        found.append((rows.pop(), m.start(2), m.end(2)))
    return found


def rows_held(text):
    """The numbers of the rows TEXT's tables hold - what a rows file holds."""
    lines = _lines(text)
    fenced = fenced_lines(lines)
    held = set()
    for i, line in enumerate(lines):
        m = None if i in fenced else ROW.match(line)
        if m:
            held.add(int(m.group(1)))
    return held


def sections(lines):
    """([(start, end)] of every '## ' section, index of the first one)."""
    fenced = fenced_lines(lines)
    heads = [i for i, line in enumerate(lines) if line.startswith("## ") and i not in fenced]
    spans = [(i, heads[n + 1] if n + 1 < len(heads) else len(lines)) for n, i in enumerate(heads)]
    return spans, (heads[0] if heads else len(lines))


def stub_of(lines, start, end, folder):
    """The file a section's one line names, when the section is a stub."""
    body = [line for line in lines[start + 1:end] if line.strip()]
    if len(body) != 1:
        return None
    m = _file_pattern(folder).match(body[0])
    return m.group(1) if m and m.group(1) == m.group(2) else None


def last_filled(lines, start, end):
    """The end of a span once its trailing blank lines are left off."""
    k = end
    while k > start + 1 and not lines[k - 1].strip():
        k -= 1
    return k


def table_of(lines, start=0, end=None):
    """(header, end) of the first table in LINES[start:end] - its header line
    and the line after its last row - or None. A table is a line starting
    with '|', the rule under it, and every '|' line that follows."""
    end = len(lines) if end is None else end
    fenced = fenced_lines(lines)
    for i in range(start, end - 1):
        if i in fenced or not lines[i].startswith("|") or not RULE.match(lines[i + 1]):
            continue
        k = i + 2
        while k < end and lines[k].startswith("|") and k not in fenced:
            k += 1
        return i, k
    return None


def unpoint(text, name, folder, index_name):
    """TEXT with every link written as the one file would write it.

    NAME is the file the text came from, or None for the index's own text.
    The inverse of what split-register.py does on the way out: a link one
    folder deeper comes back up, a link to a file beside it goes down into
    the folder, a link to a heading in another of the register's files comes
    back to an anchor on the page, and a link from the index into one of its
    files does the same."""
    def swap(match):
        label, href = match.group(1), match.group(2)
        if href.startswith(("http://", "https://", "mailto:")):
            return match.group(0)
        target, mark, fragment = href.partition("#")
        if target.startswith("/"):
            return match.group(0)
        if name is None:
            inner = target[len(folder) + 1:]
            if (target.startswith(folder + "/") and mark and "/" not in inner
                    and inner.endswith(".md")):
                return "[%s](#%s)" % (label, fragment)
            return match.group(0)
        if not target:
            return match.group(0)
        if mark and "/" not in target and target.endswith(".md"):
            return "[%s](#%s)" % (label, fragment)
        if target == "../" + index_name:
            # A link to the page with no anchor was written with the page
            # name: an empty target is not a link, so nothing else leaves this.
            if not mark:
                return "[%s](%s)" % (label, index_name)
            return "[%s](%s%s)" % (label, mark, fragment)
        if target.startswith("../"):
            return "[%s](%s%s%s)" % (label, target[3:], mark, fragment)
        # A FILE BESIDE IT. Written in one of the register's files, a link
        # with no ../ reaches into the folder, so on the page it is one folder
        # deeper. Left as it was, the way out read it as the page's own
        # neighbour and wrote ../ in front - on 2026-10-08 every link from a
        # re-banded row to another rows file came out one folder too high.
        if target.startswith("./"):
            target = target[2:]
        return "[%s](%s/%s%s%s)" % (label, folder, target, mark, fragment)
    return LINK.sub(swap, text)


def _read(path):
    if not os.path.exists(path):
        return None
    with io.open(path, "rb") as handle:
        return handle.read().decode("utf-8", "replace")


def _list(path):
    return sorted(os.listdir(path)) if os.path.isdir(path) else []


def rows_files(text, index):
    """{heading: [the rows files INDEX's text names under it, in order]}."""
    folder = folder_of(index)
    lines = _lines(text)
    spans, _ = sections(lines)
    fenced = fenced_lines(lines)
    found = {}
    for start, end in spans:
        for k in range(start, end):
            names = None if k in fenced else rows_names(lines[k], folder)
            if names:
                found.setdefault(lines[start], []).extend(names)
    return found


def each_lines(text, index):
    """[(heading, first row with a file of its own, the files' stem)] - one
    for every section of INDEX's text that says its newer rows are one file
    each, in the order the page has them."""
    folder = folder_of(index)
    lines = _lines(text)
    spans, _ = sections(lines)
    fenced = fenced_lines(lines)
    found = []
    for start, end in spans:
        for k in range(start, end):
            each = None if k in fenced else each_of(lines[k], folder)
            if each:
                found.append((lines[start], each[0], each[1]))
    return found


def named_files(text, index):
    """Every file INDEX's text names, in the order it names them."""
    folder = folder_of(index)
    lines = _lines(text)
    spans, _ = sections(lines)
    fenced = fenced_lines(lines)
    names = []
    for start, end in spans:
        name = stub_of(lines, start, end, folder)
        if name:
            names.append(name)
            continue
        for k in range(start, end):
            found = None if k in fenced else rows_names(lines[k], folder)
            names.extend(found or [])
    return names


def register_text(index, read=None, listing=None):
    """The whole register, one text, as it was before it was split.

    INDEX is the register's own page. READ is the caller's reader, called
    with INDEX and then with each file's path built from INDEX - so a tool
    whose files a suite has served from memory reads the register the suite
    gave it. LISTING is the caller's way to name the files in the register's
    folder, called with that folder's path as path_in(INDEX, "") builds it;
    it is asked only when the page says a section's newer rows are one file
    each, and a caller that passes READ passes it too. Returns what READ
    returned for the index when it names no file."""
    if listing is None and read is None:
        listing = _list
    read = read or _read
    text = read(index)
    if not text:
        return text
    folder = folder_of(index)
    page = os.path.basename(index.replace(os.sep, "/"))
    if not named_files(text, index) and not each_lines(text, index):
        return text
    eol = CR + NL if CR + NL in text else NL
    lines = _lines(text)
    spans, first = sections(lines)
    fenced = fenced_lines(lines)

    def held(name, why):
        found = read(path_in(index, name))
        if not found:
            raise RegisterBroken("%s names %s/%s %s, and %s"
                                 % (page, folder, name, why,
                                    "there is no such file" if found is None else "the file is empty"))
        return _lines(found)

    def rows(names, heading):
        out = []
        for n, name in enumerate(names):
            got = held(name, "for the rows of %r" % heading)
            table = table_of(got)
            if table is None:
                raise RegisterBroken("%s/%s holds no table, and %s names it for the rows of %r"
                                     % (folder, name, page, heading))
            top, _ = table
            body = got[top if n == 0 else top + 2:last_filled(got, top, len(got))]
            if body:
                out.extend(unpoint(NL.join(body), name, folder, page).split(NL))
        return out

    def own(number, name, heading):
        """(the table's two header lines, the row's lines) of a row's own file."""
        got = held(name, "for row %d of %r" % (number, heading))
        table = table_of(got)
        if table is None:
            raise RegisterBroken("%s/%s holds no table, and its name says it holds row %d of %r"
                                 % (folder, name, number, heading))
        top, bottom = table
        # THE HEADING NAMES THE ROW, as the file's name and the row's first
        # cell do - `row 5b-1585`. tools/renumber.py renames the three
        # together, and a file whose heading says another row is one a person
        # opens for the wrong defect. Read where the section has a label to
        # name a row by, which is how a link names one (row_links).
        label = re.match("^## ([0-9]+[a-z]*)[.] ", heading)
        if label:
            fenced = fenced_lines(got[:top])
            titles = [line for i, line in enumerate(got[:top]) if line.startswith("# ") and i not in fenced]
            named = re.compile("(?<![0-9A-Za-z-])" + re.escape(label.group(1)) + "-([0-9]+)(?![0-9])")
            says = sorted(set(int(n) for n in named.findall(titles[0]))) if titles else []
            if says != [number]:
                raise RegisterBroken("%s/%s %s, and its name says it holds row %s-%d - the heading above "
                                     "the table names the row, as the name and the row's first cell do"
                                     % (folder, name,
                                        ("is headed %r" % titles[0][2:]) if titles else "has no heading",
                                        label.group(1), number))
        # THE FILE ENDS WITH ITS ROW. A rows file is read to its last line,
        # and words under a row's own table would be read into the register's
        # table as part of the row - a blank line and a paragraph in the
        # middle of the rows that follow.
        if any(line.strip() for line in got[bottom:]):
            raise RegisterBroken("%s/%s holds text under its table, and a row's own file ends with "
                                 "its row - what is to be said of row %d goes in the row's cells"
                                 % (folder, name, number))
        body = got[top + 2:bottom]
        has = [n for n, _ in row_groups(body) if n is not None]
        if has != [number] or not ROW.match(body[0]):
            raise RegisterBroken("%s/%s holds %s, and its name says it holds row %d alone"
                                 % (folder, name, "row%s %s" % ("" if len(has) == 1 else "s",
                                                                ", ".join(str(n) for n in has))
                                    if has else "no row", number))
        return (unpoint(NL.join(got[top:top + 2]), name, folder, page).split(NL),
                unpoint(NL.join(body), name, folder, page).split(NL))

    def each_rows(table, cut, stem, heading):
        """TABLE - the rows a rows line has just put back, header first, or
        None - with the rows that have files of their own after them."""
        if listing is None:
            raise RegisterBroken("%s says the rows of %r from %d on are one file each, and this "
                                 "reader was given no way to list %s/" % (page, heading, cut, folder))
        files = row_files(listing(path_in(index, "")), stem, folder, page)
        head, more = None, []
        for number in sorted(files):
            top, body = own(number, files[number], heading)
            head = head or top
            more.append((number, body))
        if table is not None:
            head, table = table[:2], table[2:]
        return (head or []) + settled(table or [], cut, more)

    out = [unpoint(NL.join(lines[:first]), None, folder, page)] if first else []
    for start, end in spans:
        name = stub_of(lines, start, end, folder)
        if name:
            got = held(name, "under %r" % lines[start])
            inner, _ = sections(got)
            if not inner or got[inner[0][0]] != lines[start]:
                raise RegisterBroken("%s/%s does not open its first section with %r, the heading "
                                     "that names it" % (folder, name, lines[start]))
            begin = inner[0][0]
            body = unpoint(NL.join(got[begin:last_filled(got, begin, len(got))]), name, folder, page)
            out.append(NL.join([body] + lines[last_filled(lines, start, end):end]))
            continue
        # TABLE is where in KEPT the rows of the last rows line begin, while
        # nothing but blank lines has followed it: the line for the rows'
        # own files stands under the rows line, and its rows go on the table.
        kept, waiting, table = [], [], None
        for k in range(start, end):
            names = None if k in fenced else rows_names(lines[k], folder)
            each = None if k in fenced or names is not None else each_of(lines[k], folder)
            if names is None and each is None:
                waiting.append(lines[k])
                continue
            if each is not None and table is not None and not any(w.strip() for w in waiting):
                waiting = []
                before, kept = kept[table:], kept[:table]
                kept.extend(each_rows(before, each[0], each[1], lines[start]))
                table = None
                continue
            if waiting:
                kept.extend(unpoint(NL.join(waiting), None, folder, page).split(NL))
                waiting = []
            if each is not None:
                kept.extend(each_rows(None, each[0], each[1], lines[start]))
                table = None
                continue
            table = len(kept)
            kept.extend(rows(names, lines[start]))
        if waiting:
            kept.extend(unpoint(NL.join(waiting), None, folder, page).split(NL))
        out.append(NL.join(kept))
    return NL.join(out).replace(NL, eol)


def main(argv=None):
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass
    argv = sys.argv[1:] if argv is None else argv
    if len(argv) != 1:
        print("usage: python tools/register-text.py docs/FRAGMENT-ISSUES.md")
        return 2
    try:
        text = register_text(argv[0])
    except RegisterBroken as broken:
        print("NOT READ - %s" % broken)
        return 2
    if text is None:
        print("NOT READ - %s is not there" % argv[0])
        return 2
    sys.stdout.write(text.replace(CR + NL, NL))
    return 0


if __name__ == "__main__":
    sys.exit(main())
