# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
docs/NEEDS-CHECKING.md as one text again - the index, with every group's file
read back into its place.

    python tools/needs-checking-register.py      # print the whole register

Since 2026-09-23 each group of the register has its own file,
docs/needs-checking/group-x.md, and NEEDS-CHECKING.md keeps the page's rules
and, where each group was, its heading and one line naming its file
(tools/split-needs-checking.py writes that). register_text() puts every group
back under its heading with its links written as they were in the one file, so
a tool that read the register as one text reads exactly the text it read
before, and prints exactly what it printed. owner-queue.py, check-gaps.py,
balance-of-work.py and check-docs.py (row B8) all read it here, and so does
archive-needs-checking.py.

This is only the READER. It imports nothing but the standard library, so a
tool that reads the register never breaks because a tool that WRITES it
changed.

A GROUP FILE THAT IS MISSING IS NOT SKIPPED. A register that quietly drops a
group is the failure NEEDS-CHECKING.md records against itself three times over
- "a count that quietly omits a whole group is worse than no count" - so a
heading that names a file that is not there, or a file that does not hold that
heading, raises RegisterBroken rather than returning a shorter register.

`python tools/needs-checking-register.py | grep ...` is how to search the whole
register at once, the way `grep docs/NEEDS-CHECKING.md` did before the split.
"""

import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
INDEX_NAME = "NEEDS-CHECKING.md"
INDEX_REL = "docs/" + INDEX_NAME
FOLDER_NAME = "needs-checking"
FOLDER_REL = "docs/" + FOLDER_NAME

NL = chr(10)
CR = chr(13)
FENCE = "`" * 3

# The one line that stands in the index where a group used to be, under the
# group's own heading. Written by split-needs-checking.py and nowhere else.
STUB_KEY = "**Its own file:** "
STUB = re.compile("^" + re.escape(STUB_KEY + "[`" + FOLDER_NAME + "/") + "(group-[a-z]+[.]md)"
                  + re.escape("`](" + FOLDER_NAME + "/") + "(group-[a-z]+[.]md)" + re.escape(")") + "$")

# The same pattern as archive-fragment-issues.py's LINK, which the writer
# re-points with, built the same way: from chr(92), not a typed backslash.
BS = chr(92)
LINK = re.compile(BS + "[([^" + BS + "]]*)" + BS + "]" + BS + "(([^)]+)" + BS + ")")


class RegisterBroken(Exception):
    """The index names a group file that is missing, or that does not hold
    the group whose heading names it."""


def stub_line(name):
    return "%s[`%s/%s`](%s/%s)" % (STUB_KEY, FOLDER_NAME, name, FOLDER_NAME, name)


def _eol(text):
    return CR + NL if CR + NL in text else NL


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


def stub_of(lines, start, end):
    """The file a section's one line names, when the section is a stub."""
    body = [line for line in lines[start + 1:end] if line.strip()]
    if len(body) != 1:
        return None
    m = STUB.match(body[0])
    return m.group(1) if m and m.group(1) == m.group(2) else None


def last_filled(lines, start, end):
    """The end of a section once its trailing blank lines are left off."""
    k = end
    while k > start + 1 and not lines[k - 1].strip():
        k -= 1
    return k


def unpoint(text, name):
    """TEXT with every link written as NEEDS-CHECKING.md would write it.

    NAME is the group file the text came from, or None for the index's own
    text. The inverse of what split-needs-checking.py does on the way out: a
    link one folder deeper comes back up, a link to a heading in another group
    file comes back to an anchor on the page, and a link from the index into a
    group file does the same."""
    def swap(match):
        label, href = match.group(1), match.group(2)
        if href.startswith(("http://", "https://", "mailto:")):
            return match.group(0)
        target, mark, fragment = href.partition("#")
        if target.startswith("/"):
            return match.group(0)
        if name is None:
            inner = target[len(FOLDER_NAME) + 1:]
            if (target.startswith(FOLDER_NAME + "/") and mark and "/" not in inner
                    and inner.startswith("group-")):
                return "[%s](#%s)" % (label, fragment)
            return match.group(0)
        if not target:
            return match.group(0)
        if mark and "/" not in target and target.startswith("group-") and target.endswith(".md"):
            return "[%s](#%s)" % (label, fragment)
        if target == "../" + INDEX_NAME:
            return "[%s](%s%s)" % (label, mark, fragment)
        if target.startswith("../"):
            return "[%s](%s%s%s)" % (label, target[3:], mark, fragment)
        return match.group(0)
    return LINK.sub(swap, text)


def _read(root, rel):
    path = os.path.join(root, *rel.split("/"))
    if not os.path.exists(path):
        return None
    with io.open(path, "rb") as handle:
        return handle.read().decode("utf-8", "replace")


def register_text(root=ROOT, read=None):
    """The whole register, one text, as it was before it was split.

    READ is the caller's own reader, called with a path relative to ROOT -
    "docs/NEEDS-CHECKING.md", then "docs/needs-checking/group-x.md" - so a tool
    whose reader a suite has patched reads the register the suite gave it.
    Returns what READ returned for the index when the index names no file."""
    if read is None:
        read = lambda rel: _read(root, rel)
    index = read(INDEX_REL)
    if not index:
        return index
    eol = _eol(index)
    lines = _lines(index)
    spans, first = sections(lines)
    if not any(stub_of(lines, s, e) for s, e in spans):
        return index
    out = [unpoint(NL.join(lines[:first]), None)] if first else []
    for start, end in spans:
        name = stub_of(lines, start, end)
        if not name:
            out.append(unpoint(NL.join(lines[start:end]), None))
            continue
        text = read(FOLDER_REL + "/" + name)
        if not text:
            raise RegisterBroken("%s names %s/%s under %r, and there is no such file"
                                 % (INDEX_NAME, FOLDER_NAME, name, lines[start]))
        held = _lines(text)
        inner, _ = sections(held)
        if not inner or held[inner[0][0]] != lines[start]:
            raise RegisterBroken("%s/%s does not open its first section with %r, the heading "
                                 "that names it" % (FOLDER_NAME, name, lines[start]))
        begin = inner[0][0]
        body = unpoint(NL.join(held[begin:last_filled(held, begin, len(held))]), name)
        tail = lines[last_filled(lines, start, end):end]
        out.append(NL.join([body] + tail))
    return NL.join(out).replace(NL, eol)


def main():
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass
    try:
        text = register_text()
    except RegisterBroken as broken:
        print("NOT READ - %s" % broken)
        return 2
    if text is None:
        print("NOT READ - %s is not there" % INDEX_REL)
        return 2
    sys.stdout.write(text.replace(CR + NL, NL))
    return 0


if __name__ == "__main__":
    sys.exit(main())
