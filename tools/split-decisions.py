# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Give every decision in docs/DECISIONS.md its own file, docs/decisions/D-NN.md,
and keep DECISIONS.md as the index: each decision's heading, its Status line and
a link to its full record.

    python tools/split-decisions.py            # what would change - writes nothing
    python tools/split-decisions.py --write    # change it

Exits 0 when the plan is clean or was written, 1 when a check refused and
NOTHING was written, 2 when DECISIONS.md could not be read at all.

WHY THIS EXISTS
---------------
On 2026-09-22 DECISIONS.md was 342,653 bytes holding 97 decisions, and a
session looking up one read past ninety-six others to reach it. The standard
shape for a decision log is one record per decision with an index - an
architecture decision record, or ADR - and this log already kept every other
rule of that shape: numbered, append-only, a status on each, a new record to
supersede an old one. Only the one-file part was missing.

WHAT STAYS IN DECISIONS.md, AND WHY IT IS EXACTLY THAT
------------------------------------------------------
Everything that is not a decision, and for each decision:

    ## D-NN — Title
    <its metadata lines - Status, Date, Question, Affects - unchanged>
    **Full record:** [decisions/D-NN.md](decisions/D-NN.md)

The HEADING stays because every `[D-NN](DECISIONS.md#d-nn-...)` link in the
repository points at it and tools/check-docs.py reads the set of defined
decisions from it. The METADATA stays because tools/generate-decision-summary.py
reads a decision's Status and Date from the six lines under its heading - so
the summary table is rebuilt from the index unchanged, and says the same thing.

THE FULL RECORD
---------------
docs/decisions/D-NN.md holds the decision exactly as it stood, every word, with
only its links changed: a relative link is re-pointed one folder deeper and
proved to reach the same file, and a link to a heading that moved follows it -
to the same file, or to the decision file it went to.

It borrows the link handling from tools/archive-handover.py and the file
handling from tools/archive-fragment-issues.py. No backslash is typed here.
"""

import argparse
import datetime
import importlib.util
import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
DECISIONS = os.path.join(ROOT, "docs", "DECISIONS.md")
FOLDER = os.path.join(ROOT, "docs", "decisions")


def _load_sibling(name, filename):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


AH = _load_sibling("archive_handover", "archive-handover.py")
AF = AH.AF
NL, DASH, LINK = AF.NL, AF.DASH, AF.LINK
OK, REFUSED, COULD_NOT = AF.OK, AF.REFUSED, AF.COULD_NOT

HEADING = re.compile("^## (D-[0-9]+) +" + DASH + " +(.+)$")
META = re.compile("^[*][*][A-Za-z][^*]*:[*][*]")
FULL = "**Full record:** "


class Decision(object):
    def __init__(self, ident, title, start, end):
        self.ident, self.title, self.start, self.end = ident, title, start, end
        self.name = ident + ".md"
        self.meta_end = start + 1


class Plan(object):
    def __init__(self, path, folder, today):
        self.path, self.folder, self.today = path, folder, today
        self.fatal = None
        self.problems = []
        self.eol = NL
        self.before = ""
        self.after = ""
        self.decisions = []
        self.files = {}


def plan(path=DECISIONS, folder=FOLDER, today=None):
    p = Plan(path, folder, today or datetime.date.today().isoformat())
    try:
        p.before = AF._read(path)
    except (IOError, OSError) as error:
        p.fatal = "could not read %s: %s" % (path, error)
        return p
    lines, p.eol = AF._split(p.before)
    if lines is None:
        p.fatal = "DECISIONS.md mixes CRLF and LF line endings"
        return p

    # A "## " line inside a fenced code block is an example, not a section -
    # the Format section's own template is one - so it never ends a decision.
    fenced = AH.fenced_lines(lines)
    heads = [i for i, l in enumerate(lines) if l.startswith("## ") and i not in fenced]
    for n, i in enumerate(heads):
        m = HEADING.match(lines[i])
        if not m:
            continue
        end = heads[n + 1] if n + 1 < len(heads) else len(lines)
        d = Decision(m.group(1), m.group(2).strip(), i, end)
        k = i + 1
        while k < end and (not lines[k].strip() or META.match(lines[k])):
            k += 1
        d.meta_end = k
        if any(FULL in lines[x] for x in range(i, end)):
            continue                            # already split: nothing of it is here to move
        p.decisions.append(d)
    if not p.decisions:
        p.after = p.before
        return p

    seen = {}
    for d in p.decisions:
        if d.ident in seen:
            p.problems.append("%s is defined twice - the split would file one under the other's name" % d.ident)
        seen[d.ident] = d
        if os.path.exists(os.path.join(folder, d.name)):
            p.problems.append("docs/decisions/%s already exists and DECISIONS.md still holds its text" % d.name)

    # headings that move: every heading INSIDE a decision (its ### sections).
    # The decision's own heading stays in the index, and its file gets an H1
    # with the same words, so a link to it lands in either place.
    moved = {}
    staying = set()
    inside = set()
    for d in p.decisions:
        for x in range(d.start + 1, d.end):
            h = AH.HEADING.match(lines[x])
            if h and x not in fenced:
                inside.add(x)
                moved.setdefault(AH.anchor_of(h.group(1)), d.name)
    for x, l in enumerate(lines):
        h = AH.HEADING.match(l)
        if h and x not in inside and x not in fenced:
            staying.add(AH.anchor_of(h.group(1)))
    moved = dict((a, f) for a, f in moved.items() if a not in staying)

    docs = os.path.dirname(path)
    import glob
    for other in glob.glob(os.path.join(docs, "**", "*.md"), recursive=True):
        if AF._same(other, path):
            continue
        for label, href in LINK.findall(AF._read(other)):
            target, _, fragment = href.partition("#")
            if fragment in moved and target and AF._same(os.path.join(os.path.dirname(other), target), path):
                p.problems.append("%s links to #%s inside a decision, which would move"
                                  % (os.path.basename(other), fragment))

    kept, cursor = [], 0
    for d in p.decisions:
        kept.extend(lines[cursor:d.start])
        meta = lines[d.start + 1:d.meta_end]
        while meta and not meta[-1].strip():
            meta.pop()
        kept.append(lines[d.start])
        kept.extend(meta)
        kept.extend(["", FULL + "[`decisions/%s`](decisions/%s)" % (d.name, d.name), ""])
        cursor = d.end
        # The "---" between two decisions separates them in ONE file; it is not
        # part of either record, so a record ends at its own last line.
        own = lines[d.start + 1:d.end]
        while own and (not own[-1].strip() or own[-1].strip() == "---"):
            own.pop()
        trouble = []
        body = AH._repoint(NL.join(own), d.name, moved, path, folder, trouble, d.name)
        p.problems.extend(trouble)
        p.files[d.name] = NL.join([
            "# " + d.ident + " " + DASH + " " + d.title,
            "",
            "> One record of [the decision log](../DECISIONS.md). It was given its own file on %s so that it"
            % p.today,
            "> can be read alone; **its words are unchanged, only its links were re-pointed.** Decisions are",
            "> append-only: to change one, write a new decision that supersedes it.",
            "",
        ]) + body + NL
    kept.extend(lines[cursor:])
    trouble = []
    p.after = AH._repoint(p.eol.join(kept), None, moved, path, folder, trouble, "DECISIONS.md")
    p.problems.extend(trouble)
    return p


def write(p):
    if p.fatal:
        return COULD_NOT
    if p.problems:
        return REFUSED
    if not p.decisions:
        return OK
    if not os.path.isdir(p.folder):
        os.makedirs(p.folder)
    for name in sorted(p.files):
        AF._put(os.path.join(p.folder, name), p.files[name].replace(NL, p.eol))
    AF._put(p.path, p.after)
    return OK


def main(argv=None):
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")
    parser = argparse.ArgumentParser(description="Give every decision its own file under docs/decisions/.")
    parser.add_argument("--write", action="store_true", help="change it - without this, nothing is written")
    args = parser.parse_args(argv)
    p = plan()
    if args.write and not p.fatal and not p.problems:
        write(p)
    print("DECISIONS.md - one file per decision")
    print("=" * 36)
    if p.fatal:
        print("  COULD NOT RUN: %s" % p.fatal)
        return COULD_NOT
    before, after = len(p.before.encode("utf-8")), len(p.after.encode("utf-8"))
    print("  decisions to give a file: %d" % len(p.decisions))
    if before:
        print("  DECISIONS.md            : %s -> %s bytes (%d%%)" % (format(before, ","), format(after, ","), after * 100 // before))
    if p.problems:
        print("  REFUSED - nothing was written:")
        for problem in p.problems[:20]:
            print("    - %s" % problem)
        return REFUSED
    print("Written." if args.write else "Nothing was written. Run again with --write.")
    return OK


if __name__ == "__main__":
    sys.exit(main())
