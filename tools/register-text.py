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

register_text() puts every file back where its line stands, with its links
written as they were in the one file - so a tool that read the register as
one text reads exactly the text it read before, and prints exactly what it
printed. tools/needs-checking-register.py is the same reader for
NEEDS-CHECKING.md, written first, and its line for a group has this shape.

This is only the READER. It imports nothing but the standard library, so a
tool that reads a register never breaks because the tool that WRITES it
changed.

A FILE THAT IS MISSING IS NOT SKIPPED. A register that quietly drops a
section is the failure a register exists to prevent, so a line naming a file
that is not there, a section's file that does not open with the heading that
names it, or a rows file with no table, raises RegisterBroken rather than
returning a shorter register. An index that names no file is returned as it
is: that is how a register that was never split, and every register a suite
builds for itself, is read.

`python tools/register-text.py docs/FRAGMENT-ISSUES.md | grep ...` searches
the whole register at once, the way `grep docs/FRAGMENT-ISSUES.md` did
before it was split.

NO BACKSLASH IS TYPED IN THIS FILE. The patterns are built from chr(92) and
character classes, as archive-fragment-issues.py's are.
"""

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
NAME = "[a-z0-9][a-z0-9-]*[.]md"

# check-docs.py's link pattern, [text](target), built as archive-fragment-issues.py builds it.
LINK = re.compile(BS + "[([^" + BS + "]]*)" + BS + "]" + BS + "(([^)]+)" + BS + ")")
# open-defects.py's row pattern: a table line whose first cell is a number, bold or not.
ROW = re.compile("^[|]" + BS + "s*[*]{0,2}([0-9]+)[*]{0,2}" + BS + "s*[|]")
# The line under a table's header.
RULE = re.compile("^[|](" + BS + "s*:?-+:?" + BS + "s*[|])+" + BS + "s*$")


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
    folder deeper comes back up, a link to a heading in another of the
    register's files comes back to an anchor on the page, and a link from the
    index into one of its files does the same."""
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
        return match.group(0)
    return LINK.sub(swap, text)


def _read(path):
    if not os.path.exists(path):
        return None
    with io.open(path, "rb") as handle:
        return handle.read().decode("utf-8", "replace")


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


def register_text(index, read=None):
    """The whole register, one text, as it was before it was split.

    INDEX is the register's own page. READ is the caller's reader, called
    with INDEX and then with each file's path built from INDEX - so a tool
    whose files a suite has served from memory reads the register the suite
    gave it. Returns what READ returned for the index when it names no file."""
    read = read or _read
    text = read(index)
    if not text:
        return text
    folder = folder_of(index)
    page = os.path.basename(index.replace(os.sep, "/"))
    if not named_files(text, index):
        return text
    eol = CR + NL if CR + NL in text else NL
    lines = _lines(text)
    spans, first = sections(lines)
    fenced = fenced_lines(lines)

    def held(name, why):
        found = read(path_in(index, name))
        if not found:
            raise RegisterBroken("%s names %s/%s %s, and there is no such file"
                                 % (page, folder, name, why))
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
        kept, waiting = [], []
        for k in range(start, end):
            names = None if k in fenced else rows_names(lines[k], folder)
            if names is None:
                waiting.append(lines[k])
                continue
            if waiting:
                kept.extend(unpoint(NL.join(waiting), None, folder, page).split(NL))
                waiting = []
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
