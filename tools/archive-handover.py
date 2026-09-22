# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Move the session notes out of docs/HANDOVER.md into docs/handover-archive/, one
file per sitting, and leave HANDOVER.md the short entry point it says it is.

    python tools/archive-handover.py            # what would move - writes nothing
    python tools/archive-handover.py --write    # move it

Exits 0 when the plan is clean or was written, 1 when a check refused and
NOTHING was written, 2 when HANDOVER.md could not be read at all.

WHY THIS EXISTS
---------------
The rule was written down twice and kept by neither. docs/handover-archive/
README.md: "One file per sitting ... Do not write a new section into
../HANDOVER.md - that is the race this folder exists to remove." And HANDOVER.md
section 10a carries the owner's own rule for a note - keep only what is NEW, a
MISTAKE worth not repeating, and what is still TO DO - with his reason: "if we
keep everything by note that will be big." On 2026-09-22 HANDOVER.md was
485,741 bytes, and that one day had added 81 entries. A rule a session has to
remember is the rule that falls behind, so this is the rule as a command - the
same step tools/archive-fragment-issues.py takes for the defect register.

WHAT MOVES
----------
  - every entry under "## WHERE THIS STANDS RIGHT NOW". A SITTING is a dated
    "### YYYY-MM-DD ..." heading and the undated ### entries after it, up to the
    next dated one - so a note's parts travel together;
  - every note written below section 10a, which sessions appended as "## "
    sections under the rule instead of into the archive. A sitting there ends
    at a line that is only "---".

WHAT STAYS
----------
The owner's quick start, the preamble of WHERE THIS STANDS (everything before
its first entry), the session-archive pointer, and sections 1 to 10a - the
manual that other files link into. Where the entries were, a table links the
newest sittings; docs/handover-archive/README.md lists every one, newest first.

WORDS UNCHANGED, LINKS PROVED
-----------------------------
A sitting's text arrives exactly as it stood. Only links change: a relative link
is re-pointed one folder deeper and proved, by path arithmetic, to reach the same
file; a link to a heading that moved - from the sitting or from what stays - is
re-pointed to the file that heading moved to, and the heading is proved to be
there. A link from anywhere else in docs/ to a heading that would move stops the
run, naming it, because no rewrite here can reach it.

It borrows the link pattern, the line-ending handling and the path proof from
tools/archive-fragment-issues.py, so the two cannot drift apart. No backslash is
typed in this file, for the reason that one gives.
"""

import argparse
import datetime
import glob
import importlib.util
import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
HANDOVER = os.path.join(ROOT, "docs", "HANDOVER.md")
ARCHIVE = os.path.join(ROOT, "docs", "handover-archive")

# How many of the newest sittings HANDOVER.md links to directly. The archive
# README lists every one; this is only the short list a session starts from.
LATEST = 12


def _load_sibling(name, filename):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


AF = _load_sibling("archive_fragment_issues", "archive-fragment-issues.py")
NL, CR, BS, DASH = AF.NL, AF.CR, AF.BS, AF.DASH
LINK = AF.LINK
OK, REFUSED, COULD_NOT = AF.OK, AF.REFUSED, AF.COULD_NOT

WHERE = "## WHERE THIS STANDS RIGHT NOW"
MANUAL_LAST = "## 10a."
ARCHIVE_POINTER = "## The session archive"
LATEST_HEADING = "### Latest sittings"
ROW_DATE = re.compile("^[|] (20[0-9][0-9]-[0-9][0-9]-[0-9][0-9]|undated) [|]")
DATED = re.compile("^### (20[0-9][0-9]-[0-9][0-9]-[0-9][0-9])")
ANY_DATE = re.compile("20[0-9][0-9]-[0-9][0-9]-[0-9][0-9]")
HEADING = re.compile("^#{1,6} (.+)$")
NOT_WORD = re.compile("[^" + BS + "w" + BS + "s-]")


def anchor_of(text):
    """tools/check-docs.py's rule: GitHub's, runs of hyphens NOT collapsed."""
    return NOT_WORD.sub("", text.lower()).strip().replace(" ", "-")


def fenced_lines(lines):
    """Indexes of the lines inside a fenced code block, fences included. A
    '# ' or '### ' line there is an example or a shell comment, never a
    heading - HANDOVER.md carries five of them and DECISIONS.md its template."""
    inside, out = False, set()
    for i, line in enumerate(lines):
        if line.lstrip().startswith("```"):
            out.add(i)
            inside = not inside
        elif inside:
            out.add(i)
    return out


def slug_of(text, limit=60):
    words = "".join(c if c.isalnum() else " " for c in text.lower()).split()
    slug = ""
    for word in words:
        if len(slug) + len(word) + 1 > limit:
            break
        slug = word if not slug else slug + "-" + word
    return slug or "note"


class Sitting(object):
    def __init__(self, lines, where):
        while lines and not lines[-1].strip():
            lines.pop()
        if lines and lines[-1].strip() == "---":
            lines.pop()
        while lines and not lines[-1].strip():
            lines.pop()
        self.lines = lines
        self.where = where                  # "entries" or "notes"
        fenced = fenced_lines(lines)
        first = next((l for i, l in enumerate(lines) if HEADING.match(l) and i not in fenced), "")
        self.heading = HEADING.match(first).group(1).strip() if first else "untitled note"
        dated = DATED.match(first)
        found = ANY_DATE.findall(NL.join(lines))
        self.date = dated.group(1) if dated else (max(found) if found else "undated")
        title = self.heading
        if title.startswith(self.date):
            title = title[len(self.date):].lstrip(" " + DASH + "-:")
        self.title = title or self.heading
        self.name = None
        self.anchors = [anchor_of(HEADING.match(l).group(1)) for i, l in enumerate(lines)
                        if HEADING.match(l) and i not in fenced]


class Plan(object):
    def __init__(self, handover, archive, today):
        self.handover = handover
        self.archive = archive
        self.today = today
        self.fatal = None
        self.problems = []
        self.eol = NL
        self.before = ""
        self.after = ""
        self.sittings = []
        self.files = {}          # file name -> text to write
        self.readme = None       # new README text


def _group_entries(lines):
    """Sittings under WHERE THIS STANDS: a dated ### and the undated ones after it."""
    fenced = fenced_lines(lines)
    groups, current = [], None
    for i, line in enumerate(lines):
        if line.startswith("### ") and i not in fenced:
            if DATED.match(line) or current is None:
                current = []
                groups.append(current)
        if current is not None:
            current.append(line)
    return groups


def _group_notes(lines):
    """Sittings below section 10a: a run of ## sections ended by a '---' line."""
    fenced = fenced_lines(lines)
    groups, current, has_heading = [], [], False
    for i, line in enumerate(lines):
        current.append(line)
        if HEADING.match(line) and i not in fenced:
            has_heading = True
        if line.strip() == "---" and i not in fenced:
            if has_heading:
                groups.append(current)
            current, has_heading = [], False
    if has_heading:
        groups.append(current)
    return groups


def _repoint(text, sitting_file, moved, handover, archive, trouble, where):
    """Re-point every link in TEXT, which is going into SITTING_FILE (None for
    text that stays in HANDOVER.md). MOVED maps an anchor to the file it went to."""
    home = os.path.dirname(handover)
    name = os.path.basename(handover)
    staying = sitting_file is None

    def swap(match):
        label, href = match.group(1), match.group(2)
        if href.startswith(("http://", "https://", "mailto:")):
            return match.group(0)
        target, mark, fragment = href.partition("#")
        if target.startswith("/"):
            return match.group(0)
        if target.startswith("./"):
            target = target[2:]
        self_link = (not target) or AF._same(os.path.join(home, target), handover)
        if self_link and fragment and fragment in moved:
            dest = moved[fragment]
            if staying:
                new = os.path.basename(archive) + "/" + dest + "#" + fragment
            elif dest == sitting_file:
                new = "#" + fragment
            else:
                new = dest + "#" + fragment
            return "[" + label + "](" + new + ")"
        if staying:
            return match.group(0)
        moved_target = "../" + (target or name)
        if not AF._same(os.path.join(home, target or name), os.path.join(archive, moved_target)):
            trouble.append("%s: the link (%s) would reach a different file" % (where, href))
        return "[" + label + "](" + moved_target + mark + fragment + ")"

    return LINK.sub(swap, text)


def _banner(sitting, today):
    return NL.join([
        "# Session note " + DASH + " " + sitting.title,
        "",
        "> **Archived session note** from %s. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on"
        % sitting.date,
        "> %s by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to"
        % today,
        "> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were",
        "> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),",
        "> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those",
        "> win**. A note records what was true on its own day.",
        "",
        "---",
        "",
        "",
    ])


def _readme_rows(readme_lines, sittings):
    """The archive README's table with one row per new sitting, newest first.
    Existing rows keep their order; a new row goes in at its date."""
    head = next((i for i, l in enumerate(readme_lines) if l.strip() == "| Date | Sitting |"), None)
    if head is None:
        return None, "docs/handover-archive/README.md has no '| Date | Sitting |' table"
    first = head + 2
    last = first
    while last < len(readme_lines) and readme_lines[last].startswith("|"):
        last += 1
    rows = readme_lines[first:last]
    for s in sittings:
        title = AF._pipes(s.title.replace("[", "(").replace("]", ")"))
        rows.append("| %s | [%s](%s) |" % (s.date, title, s.name))
    keyed = [(r.split("|")[1].strip() if r.count("|") > 2 else "", i, r) for i, r in enumerate(rows)]
    keyed.sort(key=lambda k: (k[0] == "undated", [-ord(c) for c in k[0]], k[1]))
    return readme_lines[:first] + [r for _, _, r in keyed] + readme_lines[last:], None


def plan(handover=HANDOVER, archive=ARCHIVE, today=None):
    p = Plan(handover, archive, today or datetime.date.today().isoformat())
    try:
        p.before = AF._read(handover)
    except (IOError, OSError) as error:
        p.fatal = "could not read %s: %s" % (handover, error)
        return p
    lines, p.eol = AF._split(p.before)
    if lines is None:
        p.fatal = "HANDOVER.md mixes CRLF and LF line endings"
        return p

    fenced = fenced_lines(lines)
    where = [i for i, l in enumerate(lines) if l.startswith(WHERE) and i not in fenced]
    manual = [i for i, l in enumerate(lines) if l.startswith(MANUAL_LAST) and i not in fenced]
    if len(where) != 1 or len(manual) != 1:
        p.fatal = ("expected one %r heading and one %r heading, found %d and %d"
                   % (WHERE, MANUAL_LAST, len(where), len(manual)))
        return p
    w = where[0]
    w_end = next(i for i in range(w + 1, len(lines)) if lines[i].startswith("## ") and i not in fenced)
    first_entry = next((i for i in range(w + 1, w_end) if lines[i].startswith("### ") and i not in fenced), w_end)
    m = manual[0]
    notes_start = next((i for i in range(m + 1, len(lines)) if lines[i].startswith("## ") and i not in fenced),
                       len(lines))

    # THIS TOOL'S OWN TABLE IS NOT A SITTING. It is a ### block too, and the
    # first version archived it on its second run - which would have left
    # HANDOVER.md with no way to the notes at all. It is cut out here, its rows
    # kept to merge with whatever moves this time, and written back fresh.
    region, listed, k = [], [], first_entry
    while k < w_end:
        if lines[k].strip() == LATEST_HEADING:
            k += 1
            while k < w_end and not lines[k].startswith("### "):
                if lines[k].startswith("| ") and ROW_DATE.match(lines[k]):
                    listed.append(lines[k])
                k += 1
            continue
        region.append(lines[k])
        k += 1

    entries = [Sitting(g, "entries") for g in _group_entries(region)]
    notes = [Sitting(g, "notes") for g in _group_notes(lines[notes_start:])]
    p.sittings = entries + notes
    if not p.sittings:
        p.after = p.before
        return p

    # names, unique against each other and against the archive on disk
    taken = set(os.listdir(archive)) if os.path.isdir(archive) else set()
    for s in p.sittings:
        stem = "%s-%s" % (s.date, slug_of(s.title))
        name, n = stem + ".md", 2
        while name in taken:
            name, n = "%s-%d.md" % (stem, n), n + 1
        taken.add(name)
        s.name = name

    # WHICH MOVED HEADINGS MATTER: only the ones a link names. Sittings reuse
    # small headings - "The fix", "What is left" - dozens of times, and a
    # heading nobody links to can repeat harmlessly. A LINKED one that is not
    # unique is a real question - the link could land in either - so that
    # stops the run.
    home = os.path.dirname(handover)
    linked = set()
    for label, href in LINK.findall(p.before):
        target, _, fragment = href.partition("#")
        if fragment and (not target or AF._same(os.path.join(home, target), handover)):
            linked.add(fragment)
    staying = set(anchor_of(HEADING.match(lines[i]).group(1))
                  for i in list(range(first_entry)) + list(range(w_end, notes_start))
                  if HEADING.match(lines[i]) and i not in fenced)
    owners = {}
    for s in p.sittings:
        for a in s.anchors:
            owners.setdefault(a, set()).add(s.name)
    moved = {}
    for a, names in sorted(owners.items()):
        if a in staying:
            continue                        # the link keeps landing on the heading that stays
        if a in linked and len(names) > 1:
            p.problems.append("the linked heading #%s is in %d sittings, so the link could land in any of them"
                              % (a, len(names)))
        moved[a] = sorted(names)[0]

    # nothing outside HANDOVER.md may link to a heading that moves
    docs = os.path.dirname(handover)
    for path in glob.glob(os.path.join(docs, "**", "*.md"), recursive=True):
        if AF._same(path, handover):
            continue
        text = AF._read(path)
        for label, href in LINK.findall(text):
            target, _, fragment = href.partition("#")
            if fragment in moved and target and AF._same(os.path.join(os.path.dirname(path), target), handover):
                p.problems.append("%s links to #%s, which would move" % (os.path.basename(path), fragment))

    for s in p.sittings:
        trouble = []
        body = _repoint(NL.join(s.lines), s.name, moved, handover, archive, trouble, s.name)
        p.problems.extend(trouble)
        p.files[s.name] = _banner(s, p.today) + body + NL

    # what stays: everything but the moved lines, with a latest-sittings table in place of the entries.
    # New rows first, then the rows the table already held; newest date first; the first LATEST.
    fresh_rows = ["| %s | [%s](handover-archive/%s) |"
                  % (s.date, AF._pipes(s.title.replace("[", "(").replace("]", ")")), s.name) for s in p.sittings]
    rows = fresh_rows + listed
    order = sorted(range(len(rows)), key=lambda i: ([-ord(c) for c in ROW_DATE.match(rows[i]).group(1)], i))
    newest = [rows[i] for i in order][:LATEST]
    table = [
        LATEST_HEADING,
        "",
        "**Every session note lives in its own file in [`handover-archive/`](handover-archive/README.md)**, one",
        "file per sitting, newest first in that folder's README. These are the newest %d. Write a new note there,"
        % len(newest),
        "not here " + DASH + " [`tools/archive-handover.py`](../tools/archive-handover.py) moves any that land here anyway.",
        "",
        "| Date | Sitting |",
        "|---|---|",
    ] + newest + [""]
    kept = lines[:first_entry] + table + lines[w_end:notes_start]
    while kept and not kept[-1].strip():
        kept.pop()
    kept.append("")
    trouble = []
    p.after = _repoint(p.eol.join(kept), None, moved, handover, archive, trouble, "HANDOVER.md")
    p.problems.extend(trouble)

    # every anchor a re-pointed link now names must exist in its file
    for name, text in list(p.files.items()) + [("HANDOVER.md", p.after)]:
        for label, href in LINK.findall(text):
            target, _, fragment = href.partition("#")
            if not fragment:
                continue
            dest = os.path.basename(target) if target else name
            if dest in p.files and fragment not in [anchor_of(HEADING.match(l).group(1)) for l in p.files[dest].split(NL) if HEADING.match(l)]:
                p.problems.append("%s links to %s#%s and that heading is not there" % (name, dest, fragment))

    readme_path = os.path.join(archive, "README.md")
    readme_text = AF._read(readme_path)
    readme_lines, readme_eol = AF._split(readme_text)
    rows, why = _readme_rows(readme_lines, p.sittings)
    if rows is None:
        p.problems.append(why)
    else:
        p.readme = readme_eol.join(rows)
    return p


def write(p):
    if p.fatal:
        return COULD_NOT
    if p.problems:
        return REFUSED
    if not p.sittings:
        return OK
    for name in sorted(p.files):
        AF._put(os.path.join(p.archive, name), p.files[name].replace(NL, p.eol))
    AF._put(os.path.join(p.archive, "README.md"), p.readme)
    AF._put(p.handover, p.after)
    return OK


def report(p, writing):
    print("HANDOVER.md - session notes")
    print("=" * 27)
    print("")
    if p.fatal:
        print("  COULD NOT RUN: %s" % p.fatal)
        return
    entries = [s for s in p.sittings if s.where == "entries"]
    notes = [s for s in p.sittings if s.where == "notes"]
    print("  %-22s: %d from WHERE THIS STANDS, %d from below section 10a"
          % ("sittings moved" if writing and not p.problems else "sittings to move", len(entries), len(notes)))
    before = len(p.before.encode("utf-8"))
    after = len(p.after.encode("utf-8"))
    if before:
        print("  HANDOVER.md           : %s -> %s bytes (%d%% of what it was)"
              % (format(before, ","), format(after, ","), after * 100 // before))
    for s in p.sittings[:8]:
        print("    %-10s %s" % (s.date, s.name))
    if len(p.sittings) > 8:
        print("    ... and %d more" % (len(p.sittings) - 8))
    if p.problems:
        print("")
        print("  REFUSED - nothing was written:")
        for problem in p.problems[:20]:
            print("    - %s" % problem)
        return
    print("")
    print("Written." if writing else "Nothing was written. Run again with --write to move them.")


def main(argv=None):
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")
    parser = argparse.ArgumentParser(description="Move session notes out of docs/HANDOVER.md into docs/handover-archive/.")
    parser.add_argument("--write", action="store_true", help="move them - without this, nothing is written")
    args = parser.parse_args(argv)
    p = plan()
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
