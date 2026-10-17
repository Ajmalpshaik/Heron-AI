# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Give an id this branch took - a decision, a question, a NEEDS-CHECKING group
or a 5b row - a new number, in this branch's own text and nowhere else.

    python tools/renumber.py --next                          # the next free id in each register
    python tools/renumber.py --from D-116 --to D-131 --dry-run   # what it would change
    python tools/renumber.py --from D-116 --to D-131         # change it
    python tools/renumber.py --from "Group EJ" --to "Group FZ"
    python tools/renumber.py --from Q-64 --to Q-69
    python tools/renumber.py --from 5b-489 --to 5b-652

Exits 0 when every use was renamed (or, with --dry-run, would be); 1 when
some were left for a person to rename and the rest were done; 2 when it
refused or could not look, and then it wrote nothing.

WHY THIS EXISTS
---------------
On 2026-10-17 about twelve sessions worked on Heron at once, and their open
pull requests claimed the same new ids: D-116 in four of them, Q-64 in three,
NEEDS-CHECKING Group EJ in four and Group EK in three, row 5b-489 in five.
tools/next-id.py now says which id is free before one is picked, and the gates
catch a clash that happens anyway - check-docs.py a number defined twice,
check-decision-titles.py a number reused with another title,
tests/test_open_defects.py a 5b row used twice. The repair was still by hand:
heron-ship section 6a told the side merging second to take the next free id
and `git grep` the old one. A grep cannot tell this branch's D-116 from
main's, and the hand that does the renaming has to.

WHAT IS "THIS BRANCH'S OWN TEXT"
--------------------------------
Every line this checkout has added since its merge-base with --base
(origin/main), committed or not, and every untracked file. A line main has
too is main's, and is never touched - the same id on main is main's own id.

A changed line whose OLD side carries the id too is left alone and named: the
id in it may be main's, kept through an edit, and only a person can tell.
That only happens once main has been merged, which is why the advice is to
renumber BEFORE merging main: then the merge has nothing to collide on. While
a merge is half done this refuses - abort it, renumber, and merge again.

A file this branch added whose name carries the id is renamed with it -
docs/decisions/D-116.md, docs/needs-checking/group-ej.md. The rename is on
disk only; this never stages anything. Stage the paths it prints, by name.

WHAT IT RENAMES, PER REGISTER
-----------------------------
  D      D-116, and d-116 in an anchor (#d-116--its-title) or a file name
  Q      Q-64, and q-64 likewise
  5b     5b-489, and the number in the first cell of the row's line in a
         docs/fragment-issues/section-5b-rows-*.md file
  group  Group EJ, an item EJ1, EJ2..., and group-ej in a file name or anchor.
         A bare EJ on its own is NOT renamed - a two-letter group can be a
         word (AT, IT, OR) in the capitals 5b rows are written in - and each
         one left is named, for a person.

A one-letter group is refused: every one was taken long ago, and its letter
is a word. A number inside a longer one (D-1160, 5b-4890) is never touched.

WHAT IT REFUSES
---------------
  - an --from this branch does not DEFINE (a heading, a row, the file): its
    uses are then references to main's, and renaming them would point them
    away from it;
  - a --to that --base or this checkout already defines - take the id
    `--next` prints;
  - a rename onto a file that is already there;
  - a merge half done, or a folder with no merge-base with --base.

It does not write a **Numbering:** line or reorder rows. A renamed 5b row
stays where it was in its file; heron-ship section 6a has the order rows are
kept in when main is merged.

--next is tools/next-id.py's answer, unchanged - it reads main AND every
branch on the remote, so its "next" is free everywhere, not only on main.
"""

import argparse
import importlib.util
import io
import os
import re
import sys

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

NL = chr(10)


def _load(name, filename):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


# The registers, the readers and the git helper are next-id's: one rule for
# what an id is, read in one place.
NI = _load("next_id", "next-id.py")
CouldNot = NI.CouldNot
REGISTERS = {reg.key: reg for reg in NI.REGISTERS}
ROWS_5B = re.compile(r"^docs/fragment-issues/section-5b-rows-[0-9-]+\.md$")
SNIPPET = 36


# --------------------------------------------------------------------------
# An id, and every form it is written in
# --------------------------------------------------------------------------

class Ident(object):
    """KIND is a next-id register key; SHOW how a person writes it."""

    def __init__(self, kind, value, show):
        self.kind, self.value, self.show = kind, value, show

    def __eq__(self, other):
        return (self.kind, self.value) == (other.kind, other.value)

    def __ne__(self, other):
        return not self == other


ID_FORMS = [
    ("D", re.compile(r"^[Dd]-([0-9]+)$")),
    ("Q", re.compile(r"^[Qq]-([0-9]+)$")),
    ("5b", re.compile(r"^(?:[Rr]ow +)?5[Bb]-([0-9]+)$")),
    ("group", re.compile(r"^(?:[Gg]roup[ -]+)?([A-Za-z]{1,3})$")),
]


def parse(text):
    """An Ident from `D-116`, `Q-64`, `5b-489`, `Group EJ` or `EJ` - or CouldNot."""
    said = " ".join(text.split())
    for kind, pattern in ID_FORMS:
        m = pattern.match(said)
        if not m:
            continue
        if kind == "group":
            letters = m.group(1).upper()
            if len(letters) == 1:
                raise CouldNot("a one-letter group (%s) is renamed by hand - its letter "
                               "is also a word, and every one was taken long ago" % letters)
            return Ident(kind, letters, "Group " + letters)
        digits = m.group(1)
        return Ident(kind, int(digits), {"D": "D-", "Q": "Q-", "5b": "5b-"}[kind] + digits)
    raise CouldNot("%r is not an id this renames - write D-116, Q-64, 5b-489 or Group EJ"
                   % text)


def _token(text):
    return re.escape(text)


def forms(ident, number_text):
    """[(pattern, replacement)] for every form IDENT is written in, with the
    number spelled as NUMBER_TEXT is - D-05 stays two digits."""
    if ident.kind == "group":
        up, low = ident.value, ident.value.lower()
        return [
            (re.compile(r"(?<![A-Za-z0-9_])([Gg]roups? +)" + up + r"(?![A-Za-z0-9_])"), up),
            (re.compile(r"(?<![A-Za-z0-9_])" + up + r"(?=[0-9]+(?![A-Za-z0-9_]))"), up),
            (re.compile(r"(?<![A-Za-z0-9_])(group-)" + low + r"(?![a-z0-9])"), low),
        ]
    prefix = {"D": "D-", "Q": "Q-", "5b": "5b-"}[ident.kind]
    found = [(re.compile(r"(?<![A-Za-z0-9_])" + _token(prefix + number_text) + r"(?![0-9])"),
              prefix + number_text)]
    if prefix.lower() != prefix:
        low = prefix.lower() + number_text
        found.append((re.compile(r"(?<![A-Za-z0-9_])" + _token(low) + r"(?![0-9])"), low))
    return found


class Renamer(object):
    """Renames OLD to NEW in one line of text, and says where."""

    def __init__(self, old, new, old_text, new_text):
        self.old, self.new = old, new
        self.pairs = []
        for (pattern, said), (_, written) in zip(forms(old, old_text), forms(new, new_text)):
            self.pairs.append((pattern, said, written))
        self.bare = None
        if old.kind == "group":
            self.bare = re.compile(r"(?<![A-Za-z0-9_])" + old.value + r"(?![A-Za-z0-9_])")
        self.row = None
        if old.kind == "5b":
            self.row = re.compile(r"^(\|\s*\*{0,2})" + old_text + r"(\*{0,2}\s*\|)")
            self.row_new = new_text

    def carries(self, text, path=None):
        """True when TEXT holds any form of OLD."""
        if any(p.search(text) for p, _, _ in self.pairs):
            return True
        return bool(self.row and path and ROWS_5B.match(path)
                    and any(self.row.match(line) for line in text.split(NL)))

    def line(self, text, path):
        """(the line renamed, [(column in the renamed line, said, written)])."""
        spans = []
        for pattern, said, written in self.pairs:
            for m in pattern.finditer(text):
                start = m.start() + (len(m.group(1)) if pattern.groups else 0)
                spans.append((start, m.end(), said, written))
        if self.row is not None and ROWS_5B.match(path):
            m = self.row.match(text)
            if m:
                spans.append((m.end(1), m.start(2), text[m.end(1):m.start(2)], self.row_new))
        out, hits, at = [], [], 0
        for start, end, said, written in sorted(spans):
            if start < at:
                continue
            out.append(text[at:start])
            hits.append((sum(len(part) for part in out), said, written))
            out.append(written)
            at = end
        out.append(text[at:])
        return "".join(out), hits

    def bare_left(self, text):
        return [m.start() for m in self.bare.finditer(text)] if self.bare else []


def number_text(ident, typed):
    """The number as the person typed it, so D-05 is matched as D-05."""
    if ident.kind == "group":
        return ident.value
    return re.search(r"([0-9]+)$", typed.strip()).group(1)


# --------------------------------------------------------------------------
# What this branch added
# --------------------------------------------------------------------------

def git_text(root, args):
    return NI.git(root, args).decode("utf-8", "replace")


def merge_base(root, base):
    try:
        base_commit = git_text(root, ["rev-parse", "--verify", "-q",
                                      base + "^{commit}"]).strip()
    except CouldNot:
        raise CouldNot("there is no %s here - run `git fetch origin main` first" % base)
    try:
        mb = git_text(root, ["merge-base", base_commit, "HEAD"]).strip()
    except CouldNot:
        raise CouldNot("this branch has no merge-base with %s" % base)
    return base_commit, mb


def refuse_half_merge(root):
    unmerged = git_text(root, ["ls-files", "-u"]).strip()
    git_dir = git_text(root, ["rev-parse", "--git-dir"]).strip()
    if not os.path.isabs(git_dir):
        git_dir = os.path.join(root, git_dir)
    if unmerged or os.path.exists(os.path.join(git_dir, "MERGE_HEAD")):
        raise CouldNot("a merge is half done here. `git merge --abort`, renumber, "
                       "commit, and merge again - then the merge has nothing to "
                       "collide on")


HUNK = re.compile(r"^@@ -[0-9]+(?:,([0-9]+))? \+([0-9]+)(?:,([0-9]+))? @@")


def added_lines(root, mb):
    """{path: {"added_file": bool, "lines": {line number: the hunk's old side}}}
    - every line this checkout added since MB, and every untracked file."""
    out = git_text(root, ["-c", "core.quotepath=off", "diff", "-U0", "--no-color",
                          "--no-ext-diff", "--no-textconv", "--find-renames", mb, "--"])
    found, path, is_new = {}, None, False
    lines = out.split(NL)
    at = 0
    while at < len(lines):
        line = lines[at]
        at += 1
        if line.startswith("diff --git "):
            path, is_new = None, False
        elif line.startswith("--- "):
            is_new = line == "--- /dev/null"
        elif line.startswith("+++ "):
            path = line[len("+++ b/"):] if line.startswith("+++ b/") else None
            if path is not None and path.startswith('"'):
                path = None      # a name git had to quote: never one of Heron's
            if path is not None:
                found.setdefault(path, {"added_file": is_new, "lines": {}})
        elif line.startswith("@@") and path is not None:
            m = HUNK.match(line)
            old_n = int(m.group(1)) if m.group(1) is not None else 1
            start = int(m.group(2))
            new_n = int(m.group(3)) if m.group(3) is not None else 1
            # -U0: a hunk is its old lines then its new ones, and nothing
            # else but git's "no newline" note - read by count, since a line
            # of text may itself begin "+++".
            old_side, new_seen = [], 0
            while (old_n or new_seen < new_n) and at < len(lines):
                body = lines[at]
                at += 1
                if body.startswith("\\"):
                    continue
                if body.startswith("-") and old_n:
                    old_side.append(body[1:])
                    old_n -= 1
                elif body.startswith("+") and new_seen < new_n:
                    new_seen += 1
            for number in range(start, start + new_n):
                found[path]["lines"][number] = NL.join(old_side)
    untracked = git_text(root, ["ls-files", "--others", "--exclude-standard", "-z"])
    for rel in untracked.split(chr(0)):
        if rel:
            found[rel] = {"added_file": True, "lines": None}
    return found


def read_text(root, rel):
    """The file's text, or None when it is not UTF-8 text."""
    try:
        with open(os.path.join(root, *rel.split("/")), "rb") as handle:
            data = handle.read()
    except (IOError, OSError):
        return None
    if b"\0" in data:
        return None
    try:
        return data.decode("utf-8")
    except UnicodeDecodeError:
        return None


def defined(kind, texts):
    """The ids of register KIND defined in TEXTS, [(path, text, whole file)],
    read by next-id's own rules."""
    reg = REGISTERS[kind]
    ids = set()
    for path, text, whole in texts:
        for pattern, read, named in reg.sources:
            m = pattern.match(path)
            if not m:
                continue
            ids |= read(text)
            if named is not None and whole:
                ids.add(named(m.group(1)))
    return ids


def checkout_texts(root):
    """[(path, text, True)] for every register file on disk in this checkout."""
    found = []
    for top in NI.PATHS:
        full = os.path.join(root, *top.split("/"))
        if os.path.isfile(full):
            found.append(top)
        for folder, _, names in os.walk(full):
            for name in names:
                found.append(os.path.relpath(os.path.join(folder, name), root)
                             .replace(os.sep, "/"))
    return [(rel, read_text(root, rel) or "", True) for rel in found]


# --------------------------------------------------------------------------
# The plan, and doing it
# --------------------------------------------------------------------------

class Plan(object):
    def __init__(self):
        self.edits = {}        # path -> (new text, [(line, column, said, written, new line)])
        self.renames = []      # (old path, new path)
        self.left = []         # (path, line, why, the line)


def plan(root, old_typed, new_typed, base="origin/main"):
    old, new = parse(old_typed), parse(new_typed)
    if old.kind != new.kind:
        raise CouldNot("%s and %s are in different registers" % (old.show, new.show))
    if old == new:
        raise CouldNot("%s is already %s" % (old.show, new.show))
    refuse_half_merge(root)
    base_commit, mb = merge_base(root, base)
    branch = added_lines(root, mb)
    renamer = Renamer(old, new, number_text(old, old_typed), number_text(new, new_typed))

    texts = {}
    for rel in branch:
        texts[rel] = read_text(root, rel)

    own = []
    for rel, info in branch.items():
        if texts[rel] is None:
            continue
        if info["lines"] is None:
            own.append((rel, texts[rel], True))
        else:
            body = texts[rel].split(NL)
            own.append((rel, NL.join(body[n - 1] for n in sorted(info["lines"])
                                     if n <= len(body)), info["added_file"]))
    if old.value not in defined(old.kind, own):
        raise CouldNot("this branch defines no %s of its own - no heading, row or file "
                       "of it is in what the branch added since %s. Its uses are "
                       "references to %s's, and renaming them would point them away "
                       "from it." % (old.show, mb[:8], base))
    if new.value in NI.ids_at(root, base_commit, {})[new.kind]:
        raise CouldNot("%s already has %s - take the id `--next` prints" % (base, new.show))
    if new.value in defined(new.kind, checkout_texts(root)):
        raise CouldNot("this checkout already has %s - take the id `--next` prints"
                       % new.show)

    result = Plan()
    for rel in sorted(branch):
        info, text = branch[rel], texts[rel]
        if info["added_file"]:
            moved, hits = renamer.line(rel, "")
            if hits:
                result.renames.append((rel, moved))
        if text is None:
            continue
        body = text.split(NL)
        numbers = range(1, len(body) + 1) if info["lines"] is None else sorted(info["lines"])
        changed = []
        for number in numbers:
            if number > len(body):
                continue
            line = body[number - 1]
            renamed, hits = renamer.line(line, rel)
            main_side = None if info["lines"] is None else info["lines"][number]
            if hits and main_side and renamer.carries(main_side, rel):
                result.left.append((rel, number, "the line it replaced carries %s too - "
                                    "it may be %s's" % (old.show, base), line))
                continue
            for column in renamer.bare_left(renamed):
                result.left.append((rel, number, "a bare %s at column %d - a group's letters "
                                    "can be a word, so a person renames this one"
                                    % (old.value, column + 1), line))
            if hits:
                body[number - 1] = renamed
                changed.extend((number, col, said, written, renamed)
                               for col, said, written in hits)
        if changed:
            result.edits[rel] = (NL.join(body), changed)

    targets = set()
    for was, now in result.renames:
        if os.path.exists(os.path.join(root, *now.split("/"))) or now in targets:
            raise CouldNot("%s would be renamed onto %s, which is already there" % (was, now))
        targets.add(now)
    result.old, result.new, result.mb, result.base = old, new, mb, base
    return result


def apply(root, result):
    for rel, (text, _) in sorted(result.edits.items()):
        with io.open(os.path.join(root, *rel.split("/")), "w", encoding="utf-8",
                     newline="") as handle:
            handle.write(text)
    for was, now in result.renames:
        target = os.path.join(root, *now.split("/"))
        if not os.path.isdir(os.path.dirname(target)):
            os.makedirs(os.path.dirname(target))
        os.rename(os.path.join(root, *was.split("/")), target)


def snippet(line, column, width):
    start = max(0, column - SNIPPET)
    end = min(len(line), column + width + SNIPPET)
    return (("..." if start else "") + line[start:end] + ("..." if end < len(line) else ""))


def report(result, dry_run):
    out = sys.stdout.write
    out("Renumber %s -> %s in this branch's own text - what it added since %s, its "
        "merge-base with %s%s\n\n" % (result.old.show, result.new.show, result.mb[:8],
                                       result.base,
                                       " - DRY RUN, nothing written" if dry_run else ""))
    count = 0
    for rel in sorted(result.edits):
        for number, column, said, written, line in result.edits[rel][1]:
            count += 1
            out("  %s:%d  %s -> %s   %s\n" % (rel, number, said, written,
                                              snippet(line, column, len(written))))
    for was, now in result.renames:
        out("  renamed  %s -> %s\n" % (was, now))
    if not count and not result.renames:
        out("  nothing of this branch's own says %s\n" % result.old.show)
    if result.left:
        out("\nLEFT ALONE - a person decides each of these:\n")
        for rel, number, why, line in result.left:
            out("  %s:%d  %s\n      %s\n" % (rel, number, why, snippet(line, 0, 0)))
    touched = sorted(set(result.edits) | {p for pair in result.renames for p in pair})
    out("\n%d use%s in %d file%s, %d file%s renamed%s.\n"
        % (count, "" if count == 1 else "s", len(result.edits),
           "" if len(result.edits) == 1 else "s", len(result.renames),
           "" if len(result.renames) == 1 else "s",
           " - nothing was written" if dry_run else ""))
    if touched and not dry_run:
        out("Nothing was staged. Stage these by name:\n  git add -- %s\n" % " ".join(touched))


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="Rename an id this branch took - D-nnn, Q-nn, a NEEDS-CHECKING "
                    "group or a 5b row - in this branch's own text only.")
    parser.add_argument("--next", action="store_true",
                        help="print the next free id in each register (tools/next-id.py)")
    parser.add_argument("--from", dest="old", metavar="ID", help="the id to rename")
    parser.add_argument("--to", dest="new", metavar="ID", help="its new id")
    parser.add_argument("--dry-run", action="store_true", help="say what would change; write nothing")
    parser.add_argument("--no-fetch", action="store_true", help="with --next: do not fetch first")
    parser.add_argument("--json", action="store_true", help="with --next: the answer as JSON")
    parser.add_argument("--base", default="origin/main",
                        help="the branch this one merges into (default origin/main)")
    parser.add_argument("--repo", default=ROOT, help="the checkout to change (default: this one)")
    args = parser.parse_args(argv)

    if args.next:
        if args.old or args.new:
            parser.error("--next takes no --from or --to")
        remote, _, base = args.base.partition("/")
        passed = ["--repo", args.repo, "--remote", remote, "--base", base or "main"]
        return NI.main(passed + (["--no-fetch"] if args.no_fetch else [])
                       + (["--json"] if args.json else []))
    if not (args.old and args.new):
        parser.error("give --next, or both --from and --to")
    try:
        result = plan(args.repo, args.old, args.new, args.base)
    except CouldNot as exc:
        sys.stdout.write("renumber: nothing written - %s\n" % exc)
        return 2
    if not args.dry_run:
        apply(args.repo, result)
    report(result, args.dry_run)
    return 1 if result.left else 0


if __name__ == "__main__":
    sys.exit(main())
