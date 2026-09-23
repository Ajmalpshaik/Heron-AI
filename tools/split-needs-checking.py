# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Give every group of docs/NEEDS-CHECKING.md its own file,
docs/needs-checking/group-x.md, and keep NEEDS-CHECKING.md as the register's
index - its rules, and each group's heading where the group was.

    python tools/split-needs-checking.py            # what would move - writes nothing
    python tools/split-needs-checking.py --write    # move it

Exits 0 when the plan is clean or was written, 1 when a check refused and
NOTHING was written, 2 when the register could not be read at all.

WHY THIS EXISTS
---------------
The owner asked for it on 2026-09-23, after the handover, the decisions and the
defect register had each been split for the same reason: a register a session
cannot read whole is a register that gets read in part. `wc -c
docs/NEEDS-CHECKING.md` said how big it had grown; `python
tools/needs-checking-register.py | wc -c` says the same of the register now.

WHAT MOVES, AND WHAT STAYS
--------------------------
A `## Group X` section moves whole to group-x.md: heading, table, notes. So
does any other `##` section whose rows all belong to one group - the dated
installer sections hold AC to AF, and no `## Group` heading names those. Each
leaves its heading in NEEDS-CHECKING.md, in its place, and one line naming its
file. What stays in full is everything else: the page's own rules, a section
with rows of two groups or none, and a section whose file another section has
already taken.

THE REGISTER IS STILL ONE TEXT, AND THAT IS WHAT IS PROVED
----------------------------------------------------------
Four tools read this register - owner-queue.py, check-gaps.py,
balance-of-work.py and check-docs.py (row B8) - and archive-needs-checking.py
rewrites it. All of them read it through tools/needs-checking-register.py, which
puts every group back under its heading with its links as they were written.
Nothing here writes a byte unless:

  1. the new files, read back, ARE the register - byte for byte;
  2. the real readers, served the new files, print exactly what they print
     today, and check-gaps files exactly the same rows as waiting - borrowed,
     not re-implemented, as archive-needs-checking.py proves itself;
  3. every link re-pointed on the way out reaches the file it reached before
     (archive-handover.py's own function, which proves each one), and no other
     document links into a heading that would move.

A second run moves nothing. Sections already moved are left as they are, and
a section written into NEEDS-CHECKING.md later is moved by the next run.
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


def _load(name, filename):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


NCR = _load("needs_checking_register", "needs-checking-register.py")
AH = _load("archive_handover", "archive-handover.py")
AF = AH.AF
NL, CR, DASH = AF.NL, AF.CR, AF.DASH
OK, REFUSED, COULD_NOT = AF.OK, AF.REFUSED, AF.COULD_NOT

INDEX = os.path.join(ROOT, "docs", NCR.INDEX_NAME)
FOLDER = os.path.join(ROOT, "docs", NCR.FOLDER_NAME)

GROUP = re.compile("^## Group ([A-Z]+)(?![A-Za-z])")
ROW = re.compile("^[|] *(~~)?[*][*]([A-Z]+)([0-9]+[a-z]?)[*][*]")
B8 = re.compile("^[|] *~~[*][*]B8[*][*]~~.*$", re.M)
PASSED = re.compile("[*][*]PASSED +([0-9]{4}-[0-9]{2}-[0-9]{2})")

# The register's readers, each by the one function that reads it.
READERS = (("owner-queue", "owner-queue.py", "needs_checking"),
           ("check-gaps", "check-gaps.py", "check_register"),
           ("balance-of-work", "balance-of-work.py", "register_rows"))


def target_of(lines, start, end, fenced):
    """(file, None) for a section that belongs to one group, or (None, why)."""
    g = GROUP.match(lines[start])
    if g:
        return "group-%s.md" % g.group(1).lower(), None
    groups = set()
    for k in range(start + 1, end):
        m = ROW.match(lines[k])
        if m and k not in fenced:
            groups.add(m.group(2))
    if len(groups) == 1:
        return "group-%s.md" % groups.pop().lower(), None
    if groups:
        return None, "rows of %d groups - %s" % (len(groups), ", ".join(sorted(groups)))
    return None, "no rows of any group"


def _header(name, today):
    letters = name[len("group-"):-len(".md")].upper()
    return NL.join([
        "# Needs checking %s Group %s" % (DASH, letters),
        "",
        "> One group of [the register](../%s), in its own file since %s so that it can be read"
        % (NCR.INDEX_NAME, today),
        "> alone. **The register's rules, and every group's place in it, are on that page.** A new row",
        "> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)",
        "> reads it back into the register for every tool that reads the register, so a row here is seen",
        "> exactly as it was seen there. Written by",
        "> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).",
        "",
        "",
    ])


def split_by(text, layout, headers, today, index, folder):
    """(index text, {file: text}, trouble) - TEXT, the register as one text,
    with each section LAYOUT maps by its heading line moved to its file."""
    lines = text.replace(CR + NL, NL).split(NL)
    spans, first = NCR.sections(lines)
    fenced = NCR.fenced_lines(lines)
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
        name = layout.get(lines[start])
        if not name:
            out.append(AH._repoint(NL.join(lines[start:end]), None, moved, index, folder, trouble,
                                   lines[start]))
            continue
        k = NCR.last_filled(lines, start, end)
        out.append(NL.join([lines[start], "", NCR.stub_line(name)] + lines[k:end]))
        body = AH._repoint(NL.join(lines[start:k]), name, moved, index, folder, trouble, lines[start])
        files[name] = (headers.get(name) or _header(name, today)) + body + NL
    return NL.join(out), files, trouble


def _served(index_text, files):
    """A reader that serves the register from memory: the index, and FILES."""
    def read(rel):
        rel = rel.replace(os.sep, "/")
        if rel == NCR.INDEX_REL:
            return index_text
        if rel.startswith(NCR.FOLDER_REL + "/"):
            return files.get(rel[len(NCR.FOLDER_REL) + 1:])
        return None
    return read


def readers(index_text, files):
    """What the three readers - and check-docs' B8 test - make of the register
    served from INDEX_TEXT and FILES. Each reader is loaded fresh, its own
    `read` served from memory for the register's paths only."""
    serve = _served(index_text, files)
    answers = {}
    for key, filename, call in READERS:
        module = _load(key.replace("-", "_") + "_as_served", filename)
        original = module.read

        def patched(*parts, _o=original):
            rel = "/".join(parts).replace(os.sep, "/")
            if rel == NCR.INDEX_REL or rel.startswith(NCR.FOLDER_REL + "/"):
                return serve(rel)
            return _o(*parts)
        module.read = patched
        buffer = io.StringIO()
        try:
            with contextlib.redirect_stdout(buffer):
                result = getattr(module, call)()
        # The reader loads its OWN copy of needs-checking-register.py, so the
        # class to catch is that copy's, not this module's.
        except module.NCR.RegisterBroken as broken:
            result = "RegisterBroken: %s" % broken
        finally:
            module.read = original
        answers[key] = (result, buffer.getvalue(), list(getattr(module, "WAITING", [])))
    try:
        joined = NCR.register_text(read=serve) or ""
    except NCR.RegisterBroken as broken:
        joined = "RegisterBroken: %s" % broken
    b8 = B8.search(joined)
    passed = PASSED.search(b8.group(0)) if b8 else None
    answers["check-docs B8"] = passed.group(1) if passed else None
    return answers


def inbound(root, index, folder, anchors):
    """Links from any other document into one of ANCHORS on the register."""
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
                    found.append("%s links to %s#%s, a heading that would move"
                                 % (os.path.relpath(path, root), NCR.INDEX_NAME, fragment))
    return found


class Plan(object):
    def __init__(self, index, folder, today):
        self.index, self.folder, self.today = index, folder, today
        self.fatal = None
        self.problems = []
        self.eol = NL
        self.before = ""            # NEEDS-CHECKING.md as it stands
        self.files_before = {}      # group files as they stand
        self.joined = ""            # the register as one text
        self.after = ""
        self.files_after = {}
        self.moving = []            # (heading, file) moved by this run
        self.staying = []           # (heading, why)
        self.already = 0


def plan(index=INDEX, folder=FOLDER, today=None, root=ROOT, check_others=True):
    p = Plan(index, folder, today or datetime.date.today().isoformat())
    try:
        p.before = AF._read(index)
    except (IOError, OSError) as error:
        p.fatal = "could not read %s: %s" % (index, error)
        return p
    lines, p.eol = AF._split(p.before)
    if lines is None:
        p.fatal = "%s mixes CRLF and LF line endings" % NCR.INDEX_NAME
        return p
    if os.path.isdir(folder):
        for name in sorted(os.listdir(folder)):
            if name.startswith("group-") and name.endswith(".md"):
                p.files_before[name] = AF._read(os.path.join(folder, name))
    try:
        p.joined = NCR.register_text(read=_served(p.before, p.files_before)).replace(CR + NL, NL)
    except NCR.RegisterBroken as broken:
        p.fatal = str(broken)
        return p

    spans_before, _ = NCR.sections(lines)
    layout, taken = {}, set(p.files_before)
    for start, end in spans_before:
        name = NCR.stub_of(lines, start, end)
        if name:
            layout[lines[start]] = name
            p.already += 1
    joined = p.joined.split(NL)
    fenced = NCR.fenced_lines(joined)
    spans, _ = NCR.sections(joined)
    for start, end in spans:
        heading = joined[start]
        if heading in layout:
            continue
        name, why = target_of(joined, start, end, fenced)
        if name and name in taken:
            name, why = None, "%s already holds another section" % name
        if not name:
            p.staying.append((heading, why))
            continue
        layout[heading] = name
        taken.add(name)
        p.moving.append((heading, name))
    if not p.moving:
        p.after, p.files_after = p.before, dict(p.files_before)
        return p

    headers = {}
    for name, text in p.files_before.items():
        held = text.replace(CR + NL, NL).split(NL)
        _, first = NCR.sections(held)
        headers[name] = NL.join(held[:first]) + NL
    after, files, trouble = split_by(p.joined, layout, headers, p.today, index, folder)
    p.problems.extend(trouble)
    p.after = after.replace(NL, p.eol)
    p.files_after = dict((n, t.replace(NL, p.eol)) for n, t in files.items())

    back = NCR.register_text(read=_served(p.after, p.files_after)) or ""
    if back.replace(CR + NL, NL) != p.joined:
        p.problems.append("the new files, read back, are not the register byte for byte")
    before, now = readers(p.before, p.files_before), readers(p.after, p.files_after)
    for key in before:
        if before[key] != now[key]:
            p.problems.append("%s would read the register differently" % key)
    if check_others:
        # A section's own '## ' heading stays in NEEDS-CHECKING.md, so a link to
        # it still lands. Only the headings under it leave the page.
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
    if not p.moving:
        return OK
    if not os.path.isdir(p.folder):
        os.makedirs(p.folder)
    for name in sorted(p.files_after):
        if p.files_after[name] != p.files_before.get(name):
            AF._put(os.path.join(p.folder, name), p.files_after[name])
    AF._put(p.index, p.after)
    return OK


def layout_split(index, joined, today=None, folder=None):
    """For a tool that rewrote the register as one text - archive-needs-checking.py
    - the files that text becomes, keeping the layout on disk as it is: a
    section NEEDS-CHECKING.md already names a file for goes to that file, and
    everything else stays where it is. (index text, {file: text}, trouble)."""
    folder = folder or os.path.join(os.path.dirname(index), NCR.FOLDER_NAME)
    before = AF._read(index)
    lines = before.replace(CR + NL, NL).split(NL)
    spans, _ = NCR.sections(lines)
    layout = {}
    for start, end in spans:
        name = NCR.stub_of(lines, start, end)
        if name:
            layout[lines[start]] = name
    headers = {}
    for name in set(layout.values()):
        path = os.path.join(folder, name)
        if os.path.exists(path):
            held = AF._read(path).replace(CR + NL, NL).split(NL)
            _, first = NCR.sections(held)
            headers[name] = NL.join(held[:first]) + NL
    eol = CR + NL if CR + NL in before else NL
    after, files, trouble = split_by(joined, layout, headers, today or datetime.date.today().isoformat(),
                                     index, folder)
    return after.replace(NL, eol), dict((n, t.replace(NL, eol)) for n, t in files.items()), trouble


def main(argv=None):
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass
    parser = argparse.ArgumentParser(description="Give every group of NEEDS-CHECKING.md its own file.")
    parser.add_argument("--write", action="store_true", help="move them - without this, nothing is written")
    args = parser.parse_args(argv)
    p = plan()
    if args.write and not p.fatal and not p.problems:
        write(p)
    print("NEEDS-CHECKING.md - one file per group")
    print("=" * 39)
    if p.fatal:
        print("  COULD NOT RUN: %s" % p.fatal)
        return COULD_NOT
    print("  %-24s: %d, into %s/" % ("moved" if args.write and not p.problems else "to move",
                                     len(p.moving), NCR.FOLDER_NAME))
    print("  moved by a past run      : %d" % p.already)
    print("  staying in the index     : %d" % len(p.staying))
    for heading, why in p.staying:
        print("    %-60s %s" % (heading[3:63], why))
    b, a = len(p.before.encode("utf-8")), len(p.after.encode("utf-8"))
    if b:
        print("  %-24s: %s -> %s bytes (%d%%)" % (NCR.INDEX_NAME, format(b, ","), format(a, ","), a * 100 // b))
    if p.problems:
        print("  REFUSED - nothing was written:")
        for problem in p.problems[:20]:
            print("    - %s" % problem)
        return REFUSED
    if p.moving:
        print("  read back, the files are the register byte for byte, and owner-queue, check-gaps,")
        print("  balance-of-work and check-docs' B8 test read it exactly as they do now")
    print("Written." if args.write else "Nothing was written. Run again with --write.")
    return OK


if __name__ == "__main__":
    sys.exit(main())
