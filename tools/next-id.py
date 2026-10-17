# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Which id is free, in each append-only register - on main, on every branch,
in every open pull request and in every worktree's working files here.

    python tools/next-id.py               # fetch, list the open pull requests, then answer
    python tools/next-id.py --no-fetch    # no network: the branches as last fetched, and it says so
    python tools/next-id.py --json        # the same answer, for a machine
    python tools/next-id.py --github OWNER/NAME   # when the remote's URL is not github.com's

Exits 0 with an answer, and 2 when it could not look at main: no git, a fetch
that failed, no main, or a register in which main holds no id at all - which
means the register moved and this tool is reading the wrong place. A source it
could not read - no gh, a gh that is not logged in, a worktree whose folder is
gone - is SAID in the report and never changes the exit code: this is a
report, and nothing it finds is a failure.

WHY THIS EXISTS
---------------
Four registers take an id a person picks by hand: a NEEDS-CHECKING group
(`docs/needs-checking/group-xx.md` and its heading in the index), a 5b row of
FRAGMENT-ISSUES, an open question `Q-nn` and a decision `D-nnn`. Many sessions
work at once, and each one picked "the next free id" from its own checkout -
which cannot see a branch that has not merged yet.

On 2026-10-17 PR #561 renamed its group, its row and its question THREE TIMES -
EK -> EL -> EQ, 489 -> 491 -> 511, Q-64 -> Q-66 - because other pull requests
took the same ids first, and D-126 was drafted as D-116 and renamed five times
the same day. Every rename cost a merge, a fresh gate run and a CI cycle of
about fourteen minutes. Nobody chose carelessly; every one of them read a
checkout that was right about main and blind to everything else.

And the same day showed what reading branches alone missed. PR #549 held
5b-489, pushed; PR #553, opened later, took 489 too and merged first, so #549
renumbered. Once sessions began picking round numbers to stay clear of each
other - 540, 550, 610 - main's highest passed them, and three pull requests
held 5b-540 below it with nothing to say so: only ids ABOVE main's highest were
compared. And a dozen sessions on one PC each type a row into a worktree long
before they push it.

So this reads every branch on the remote, main and this checkout; the open
pull requests, through gh, to know which branches are live and to read one
from a fork; and the working files of every worktree of this repository. It
answers with the id above the highest one used ANYWHERE, lists what each piece
of work ADDS that main has not got, and names two kinds of collision at any
number: an id two pieces of work both add (HELD TWICE), and an id an open pull
request or this checkout adds that main has since taken (TAKEN ON MAIN).

WHAT IT READS, AND HOW
----------------------
Committed files at each branch's head, through `git ls-tree` and one
`git cat-file --batch` - never a checkout:

  group  every `Group XX` heading in docs/NEEDS-CHECKING.md, in each
         docs/needs-checking/group-xx.md and docs/needs-checking-archive/
         group-xx.md, and the xx in each of those file names
  5b     every row of the 5b section: the rows files
         docs/fragment-issues/section-5b-rows-*.md, the section on the
         register's page itself (where a branch cut before the split keeps
         it), and the `Row 5b-N` headings of the archive
  Q      the first Q-number in each question heading (`### ... Q-nn`) of
         docs/OPEN-QUESTIONS.md and docs/open-questions/ - the reading
         check-docs.py's question count uses
  D      every `D-nnn - title` heading of docs/DECISIONS.md and
         docs/decisions/, and each decision file's number

A heading inside a fenced block is an example, not an id: DECISIONS.md's own
Format section carries `## D-NN` as a template.

What a piece of work ADDS is the ids at its head less the ids at the commit
where it left main (`git merge-base`); with no such commit here - a clone cut
too short - it is compared with main itself, and the report says so. A
worktree in the middle of a merge is compared with where the commit it is
merging (MERGE_HEAD) left main as well: its working files already hold what
that brought, and those ids are main's, not its own.

Open pull requests: `gh api repos/OWNER/NAME/pulls?state=open`, the REST call,
because `gh pr list` asks GraphQL and a Claude Code cloud session refuses
GraphQL (HTTP 403, measured 2026-10-17). The repository is named to gh with
--github or read from the remote's URL, never left for gh to infer from the
folder. A pull request whose head is a branch this run already read is that
branch, labelled with its number; any other - from a fork, or pushed since the
fetch - is fetched as `refs/pull/N/head` and read the same way.

Worktrees: `git worktree list --porcelain`, read line by line - a path holds
spaces ("Heron Ai") and is everything after `worktree `. Each worktree's
register files are read FROM DISK, saved or not, committed or not.

WHAT IT DOES NOT DO
-------------------
It writes no register file and it reserves nothing. An id is free AS OF THIS
RUN; the only thing that holds one is a pushed branch carrying it, so push
the id early, and run this again before the pull request merges.

It does not write the NEEDS-CHECKING index. heron-ship's section 6a says why
that cannot be generated - its early groups are in dependency order and only a
person knows where a group belongs - and this does not try. It only answers
"which id is free?".

It fills no gap. Rows and groups are appended in order, so the next id is one
above the highest; a gap below it may be an id a branch is holding, or one a
rename left behind.

It calls TAKEN ON MAIN only for an open pull request and for this checkout. A
branch with no open pull request that adds an id main has may be one that
merged and was never deleted - its rows are main's now - and the files cannot
tell that from a clash, so it is not called one. Without gh, only this
checkout is judged, and the report says so.

Nor does it call TAKEN ON MAIN an id whose ENTRY at the live work is the very
entry main has: a 5b row's whole table line, a group's own file
docs/needs-checking/group-xx.md, a question's or a decision's heading line.
That is the work's own id, merged. A squash-merge puts it on main as a new
commit, so the session still on that branch goes on adding it against the
commit where it left main - seen 2026-10-17, when 5b-941 was called TAKEN ON
MAIN on its own branch the moment #599 merged. It is listed as MERGED instead.
Different words under the same number stay TAKEN ON MAIN, and so does an
entry main has edited since it merged, which the files cannot tell from a
clash. Line ends are not words: a worktree's CRLF is read as git's LF.

A branch that has merged main in AFTER main took its id carries the number
twice on one tree, and adds nothing main lacks; tests/test_open_defects.py
section 7 refuses a 5b number used twice on one tree, which is where that one
is caught.

It sees only what was pushed, or what is on this machine. An id typed in a
session on another machine and not pushed is invisible here, as it is to
everyone else - which is the case for pushing early, not a defect this can fix.
"""

import argparse
import json
import os
import re
import subprocess
import sys

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

NL = chr(10)
CR = chr(13)
TAB = chr(9)
FENCE = "`" * 3
# The same three dashes tools/check-decision-titles.py accepts in a heading.
DASHES = chr(0x2014) + chr(0x2013) + "-"

GIT_TIMEOUT = 300


class CouldNot(Exception):
    """This tool could not look. Exit 2, never an answer - unless the thing it
    could not look at is one source among several, which the report names."""


def git(root, args, data=None):
    """git's stdout as bytes, or CouldNot naming the command and its last line.
    GIT_OPTIONAL_LOCKS=0 stops git refreshing an index on the way past, so
    reading a worktree writes nothing in it."""
    env = dict(os.environ, GIT_OPTIONAL_LOCKS="0")
    try:
        done = subprocess.run(["git", "-C", root] + list(args), input=data,
                              capture_output=True, timeout=GIT_TIMEOUT, env=env)
    except OSError as exc:
        raise CouldNot("git could not be run: %s" % exc)
    except subprocess.TimeoutExpired:
        raise CouldNot("git %s took longer than %d seconds"
                       % (" ".join(args[:2]), GIT_TIMEOUT))
    if done.returncode != 0:
        said = done.stderr.decode("utf-8", "replace").strip().splitlines()
        raise CouldNot("git %s failed: %s"
                       % (" ".join(args[:2]), said[-1] if said else "no message"))
    return done.stdout


def run_gh(args, root):
    """(exit code, stdout, stderr) of gh. OSError when there is no gh at all."""
    try:
        done = subprocess.run(["gh"] + list(args), cwd=root, capture_output=True,
                              timeout=GIT_TIMEOUT)
    except subprocess.TimeoutExpired:
        return 1, "", "gh took longer than %d seconds" % GIT_TIMEOUT
    return (done.returncode, done.stdout.decode("utf-8", "replace"),
            done.stderr.decode("utf-8", "replace"))


# --------------------------------------------------------------------------
# How each register is read from one file's text
# --------------------------------------------------------------------------

def outside_fences(text):
    """Each line of TEXT that is not inside a fenced block."""
    fenced = False
    for line in text.replace(CR, "").split(NL):
        if line.lstrip().startswith(FENCE):
            fenced = not fenced
            continue
        if not fenced:
            yield line


# `## Group ET - ...` in the index, `# Needs checking — Group ET` at the top of
# a group's file and `# NEEDS-CHECKING archive — Group A` in the archive.
GROUP_HEADING = re.compile("^#{1,6} +(?:[^#" + NL + "]*?[" + DASHES + "] +)?"
                           "Group +([A-Z]{1,3})(?![A-Za-z0-9])")
# The row pattern tools/open-defects.py reads a register's table with.
ROW = re.compile(r"^\|\s*\*{0,2}(\d+)\*{0,2}\s*\|")
ARCHIVED_ROW = re.compile(r"^#{1,6} +Row +5b-(\d+)(?![0-9])")
SECTION_5B = "## 5b."
QUESTION = re.compile(r"^#{3,6} +[^\n]*?\bQ-(\d+)")
DECISION = re.compile("^#{1,6} +D-([0-9]+) +[" + DASHES + "]+ ")


def group_ids(text):
    return {m.group(1) for m in map(GROUP_HEADING.match, outside_fences(text)) if m}


def table_rows(text):
    return {int(m.group(1)) for m in map(ROW.match, outside_fences(text)) if m}


def page_row_lines_5b(text):
    """(number, line) of each row written on the register's page inside
    section 5b - where a branch cut before 2026-09-23's split still keeps every
    one of them. The section ends at the next `## ` heading, so section 5's
    rows, which have their own numbers from 1, are never read as 5b's."""
    inside = False
    for line in outside_fences(text):
        if line.startswith("## "):
            inside = line.startswith(SECTION_5B)
            continue
        m = ROW.match(line) if inside else None
        if m:
            yield int(m.group(1)), line


def page_rows_5b(text):
    return {n for n, _ in page_row_lines_5b(text)}


def archived_rows(text):
    return {int(m.group(1)) for m in map(ARCHIVED_ROW.match, outside_fences(text)) if m}


def question_ids(text):
    return {int(m.group(1)) for m in map(QUESTION.match, outside_fences(text)) if m}


def decision_ids(text):
    return {int(m.group(1)) for m in map(DECISION.match, outside_fences(text)) if m}


# What an id IS at one tree - its ENTRY - so an id a piece of work adds can be
# told from the same id merged onto main from that work. Each reader takes a
# file's text and the match of its path, and gives (id, entry).

def lines_of(pattern):
    """A reader whose entry for each id is the whole line PATTERN matched."""
    def read(text, path_match):
        for line in outside_fences(text):
            m = pattern.match(line)
            if m:
                yield int(m.group(1)), line
    return read


def page_lines_5b(text, path_match):
    return page_row_lines_5b(text)


def group_file(text, path_match):
    """A group's entry is its own file, whole, with its line ends as git keeps
    them - a worktree on Windows has the same file on disk with CRLF."""
    yield path_match.group(1).upper(), text.replace(CR, "")


# --------------------------------------------------------------------------
# The four registers
# --------------------------------------------------------------------------

def group_key(gid):
    """A, B ... Z, AA ... AZ, BA ...: shorter first, then by letter."""
    return (len(gid), gid)


def group_next(gid):
    """The group after GID: ET -> EU, EZ -> FA, Z -> AA, ZZ -> AAA."""
    letters = list(gid)
    at = len(letters) - 1
    while at >= 0:
        if letters[at] != "Z":
            letters[at] = chr(ord(letters[at]) + 1)
            return "".join(letters)
        letters[at] = "A"
        at -= 1
    return "A" + "".join(letters)


class Register(object):
    def __init__(self, key, name, sources, show, order, after, entries):
        self.key, self.name, self.sources = key, name, sources
        self.show, self.order, self.after = show, order, after
        self.entries = entries


FI_PAGE = re.compile(r"^docs/FRAGMENT-ISSUES\.md$")
FI_ROWS = re.compile(r"^docs/fragment-issues/section-5b-rows-[0-9-]+\.md$")
FI_ARCHIVE = re.compile(r"^docs/fragment-issues-archive/reading-defects-5b-[0-9-]+\.md$")
OQ_PAGE = re.compile(r"^docs/OPEN-QUESTIONS\.md$")
OQ_FILES = re.compile(r"^docs/open-questions/[^/]+\.md$")
D_PAGE = re.compile(r"^docs/DECISIONS\.md$")
D_FILES = re.compile(r"^docs/decisions/D-([0-9]+)\.md$")

# Each source is (a path pattern, how its text is read, how a name caught by
# the pattern becomes an id - or None when the name carries no id). Each entry
# source is (a path pattern, how its text gives each id's entry).
REGISTERS = [
    Register("group", "NEEDS-CHECKING group", [
        (re.compile(r"^docs/NEEDS-CHECKING\.md$"), group_ids, None),
        (re.compile(r"^docs/needs-checking(?:-archive)?/group-([a-z]{1,3})\.md$"),
         group_ids, lambda name: name.upper()),
    ], show=lambda gid: gid, order=group_key, after=group_next, entries=[
        (re.compile(r"^docs/needs-checking/group-([a-z]{1,3})\.md$"), group_file),
    ]),
    Register("5b", "FRAGMENT-ISSUES 5b row", [
        (FI_PAGE, page_rows_5b, None),
        (FI_ROWS, table_rows, None),
        (FI_ARCHIVE, archived_rows, None),
    ], show=lambda n: "5b-%d" % n, order=lambda n: n, after=lambda n: n + 1, entries=[
        (FI_PAGE, page_lines_5b),
        (FI_ROWS, lines_of(ROW)),
        (FI_ARCHIVE, lines_of(ARCHIVED_ROW)),
    ]),
    Register("Q", "open question", [
        (OQ_PAGE, question_ids, None),
        (OQ_FILES, question_ids, None),
    ], show=lambda n: "Q-%d" % n, order=lambda n: n, after=lambda n: n + 1, entries=[
        (OQ_PAGE, lines_of(QUESTION)),
        (OQ_FILES, lines_of(QUESTION)),
    ]),
    Register("D", "decision", [
        (D_PAGE, decision_ids, None),
        (D_FILES, decision_ids, int),
    ], show=lambda n: "D-%d" % n, order=lambda n: n, after=lambda n: n + 1, entries=[
        (D_PAGE, lines_of(DECISION)),
        (D_FILES, lines_of(DECISION)),
    ]),
]

# Everything the sources above can match, so one ls-tree per branch is enough.
PATHS = ["docs/NEEDS-CHECKING.md", "docs/needs-checking", "docs/needs-checking-archive",
         "docs/FRAGMENT-ISSUES.md", "docs/fragment-issues", "docs/fragment-issues-archive",
         "docs/OPEN-QUESTIONS.md", "docs/open-questions",
         "docs/DECISIONS.md", "docs/decisions"]


def ids_in(files):
    """{register key: set of ids} in FILES, an iterable of (path, text)."""
    found = {reg.key: set() for reg in REGISTERS}
    for path, text in files:
        for reg in REGISTERS:
            for pattern, read, named in reg.sources:
                m = pattern.match(path)
                if not m:
                    continue
                found[reg.key] |= read(text)
                if named is not None:
                    found[reg.key].add(named(m.group(1)))
    return found


def entries_in(files):
    """{register key: {id: set of entries}} in FILES, an iterable of (path,
    text). A set, because one id can be written in two places on one tree - a
    decision's heading on the log's page and in its own file."""
    found = {reg.key: {} for reg in REGISTERS}
    for path, text in files:
        for reg in REGISTERS:
            for pattern, read in reg.entries:
                m = pattern.match(path)
                if not m:
                    continue
                for i, entry in read(text, m):
                    found[reg.key].setdefault(i, set()).add(entry)
    return found


# --------------------------------------------------------------------------
# Reading the branches
# --------------------------------------------------------------------------

def fetch(root, remote):
    """Every branch on REMOTE, and drop the ones it no longer has - a deleted
    branch left behind would go on holding its ids here."""
    git(root, ["fetch", "--prune", "--no-tags", remote,
               "+refs/heads/*:refs/remotes/%s/*" % remote])


def branches(root, remote):
    """[(name, commit)] for every remote-tracking branch of REMOTE."""
    out = git(root, ["for-each-ref", "--format=%(refname)%00%(objectname)",
                     "refs/remotes/%s/" % remote]).decode("utf-8", "replace")
    found = []
    for line in out.splitlines():
        ref, _, sha = line.partition(chr(0))
        if ref.endswith("/HEAD"):
            continue
        found.append((ref[len("refs/remotes/"):], sha))
    return found


def head_commit(root):
    try:
        return git(root, ["rev-parse", "--verify", "-q", "HEAD^{commit}"]).decode().strip()
    except CouldNot:
        return None


def current_branch(root):
    """The branch this checkout is on, or None when its HEAD is detached."""
    try:
        return git(root, ["symbolic-ref", "--short", "-q", "HEAD"]).decode().strip() or None
    except CouldNot:
        return None


def merge_base(root, one, other):
    """The commit where ONE left OTHER, or None when this clone has none."""
    try:
        return git(root, ["merge-base", one, other]).decode().strip() or None
    except CouldNot:
        return None


def files_at(root, commit):
    """[(path, blob)] of every register file at COMMIT."""
    out = git(root, ["ls-tree", "-r", "-z", "--full-tree", commit, "--"] + PATHS)
    found = []
    for entry in out.decode("utf-8", "replace").split(chr(0)):
        meta, _, path = entry.partition("\t")
        parts = meta.split()
        if len(parts) == 3 and parts[1] == "blob":
            found.append((path, parts[2]))
    return found


def read_blobs(root, shas):
    """{sha: text} for every blob in SHAS, through one cat-file process."""
    shas = sorted(set(shas))
    if not shas:
        return {}
    out = git(root, ["cat-file", "--batch"], data=("\n".join(shas) + "\n").encode())
    texts, at = {}, 0
    for _ in shas:
        end = out.index(b"\n", at)
        header = out[at:end].decode().split()
        if len(header) != 3:
            raise CouldNot("git cat-file did not find %s" % header[0])
        size = int(header[2])
        texts[header[0]] = out[end + 1:end + 1 + size].decode("utf-8", "replace")
        at = end + 1 + size + 1
    return texts


def texts_at(root, commit, cache):
    """[(path, text)] of every register file at COMMIT. CACHE holds blobs
    already read, since most branches share most files with main."""
    listed = files_at(root, commit)
    missing = [sha for _, sha in listed if sha not in cache]
    cache.update(read_blobs(root, missing))
    return [(path, cache[sha]) for path, sha in listed]


def ids_at(root, commit, cache):
    """{register key: set of ids} at COMMIT."""
    return ids_in(texts_at(root, commit, cache))


# --------------------------------------------------------------------------
# Open pull requests, through gh
# --------------------------------------------------------------------------

# https://github.com/O/N(.git), git@github.com:O/N.git, ssh://git@github.com/O/N
GITHUB_URL = re.compile(r"^(?:https?://(?:[^@/]+@)?github\.com/|git@github\.com:"
                        r"|ssh://git@github\.com/)([^/]+)/([^/]+?)(?:\.git)?/?$")
# One open pull request per line: number, head branch, head commit, head repo.
PULLS_JQ = ('.[] | "\\(.number)\\t\\(.head.ref)\\t\\(.head.sha)'
            '\\t\\(.head.repo.full_name // "")"')


def github_repo(url):
    """OWNER/NAME of a github.com remote URL, or None for any other URL."""
    m = GITHUB_URL.match(url.strip())
    return "%s/%s" % (m.group(1), m.group(2)) if m else None


def parse_pulls(text):
    """[{number, branch, sha, repo}] from gh's PULLS_JQ lines. A line in any
    other shape means gh answered something this tool does not read, and it
    says so rather than skipping it."""
    found = []
    for line in text.replace(CR, "").split(NL):
        if not line.strip():
            continue
        parts = line.split(TAB)
        if len(parts) != 4 or not parts[0].isdigit() or not parts[2]:
            raise CouldNot("gh answered a line this tool does not read: %r" % line[:120])
        found.append({"number": int(parts[0]), "branch": parts[1],
                      "sha": parts[2], "repo": parts[3]})
    return found


def first_line(text):
    for line in text.replace(CR, "").split(NL):
        if line.strip():
            return line.strip()
    return "no message"


def open_pulls(gh, root, repo):
    """[{number, branch, sha, repo}] of REPO's open pull requests, or CouldNot
    saying in plain words why gh did not answer."""
    try:
        code, out, err = gh(["api", "--paginate", "repos/%s/pulls?state=open&per_page=100"
                             % repo, "--jq", PULLS_JQ], root)
    except OSError as exc:
        raise CouldNot("gh could not be run - it is not installed, or not on PATH (%s)" % exc)
    if code != 0:
        raise CouldNot("gh did not list them (exit %d): %s" % (code, first_line(err or out)))
    return parse_pulls(out)


def fetch_pull(root, remote, number):
    """The head commit of pull request NUMBER, fetched from REMOTE."""
    git(root, ["fetch", "--no-tags", remote, "refs/pull/%d/head" % number])
    return git(root, ["rev-parse", "--verify", "-q", "FETCH_HEAD^{commit}"]).decode().strip()


# --------------------------------------------------------------------------
# Worktrees, and their working files
# --------------------------------------------------------------------------

def parse_worktrees(text):
    """[{path, head, branch, bare, prunable}] from `git worktree list
    --porcelain`. A path is the rest of its line, spaces and all."""
    found, one = [], None
    for line in text.replace(CR, "").split(NL):
        if line.startswith("worktree "):
            one = {"path": line[len("worktree "):], "head": None, "branch": None,
                   "bare": False, "prunable": False}
            found.append(one)
        elif one is None:
            continue
        elif line.startswith("HEAD "):
            one["head"] = line[len("HEAD "):]
        elif line.startswith("branch "):
            ref = line[len("branch "):]
            one["branch"] = ref[len("refs/heads/"):] if ref.startswith("refs/heads/") else ref
        elif line == "bare":
            one["bare"] = True
        elif line == "prunable" or line.startswith("prunable "):
            one["prunable"] = True
    return found


def merge_head(folder):
    """The commit the worktree at FOLDER is merging, or None."""
    try:
        return git(folder, ["rev-parse", "--verify", "-q", "MERGE_HEAD^{commit}"]
                   ).decode().strip() or None
    except CouldNot:
        return None


def files_on_disk(folder):
    """(path, text) of every register file under FOLDER, as it is on disk."""
    for rel in PATHS:
        full = os.path.join(folder, *rel.split("/"))
        if os.path.isfile(full):
            found = [full]
        elif os.path.isdir(full):
            found = []
            for here, dirs, names in os.walk(full):
                dirs.sort()
                found.extend(os.path.join(here, name) for name in sorted(names))
        else:
            continue
        for path in found:
            with open(path, "rb") as handle:
                text = handle.read().decode("utf-8", "replace")
            yield os.path.relpath(path, folder).replace(os.sep, "/"), text


def same_folder(one, other):
    return (os.path.normcase(os.path.realpath(one))
            == os.path.normcase(os.path.realpath(other)))


# --------------------------------------------------------------------------
# The answer
# --------------------------------------------------------------------------

class Holder(object):
    """One piece of work that may hold ids: a branch, this checkout's HEAD, a
    pull request read on its own, or a worktree's working files. OWNER is the
    work it belongs to - a branch name - so a branch, its pull request and the
    worktree it is checked out in are one piece of work, never three."""

    def __init__(self, name, owner, commit, ids=None, merging=None, folder=None):
        self.name, self.owner, self.commit, self.ids = name, owner, commit, ids
        self.merging, self.folder = merging, folder
        self.adds, self.compared_with = None, None


def answer(root, remote="origin", base="main", fetched=True, gh=run_gh, github=None,
           worktrees=True):
    """Everything this tool says, as one dict. Raises CouldNot when it could
    not look at main; a source it could not read is named in the dict."""
    if fetched:
        fetch(root, remote)
    refs = branches(root, remote)
    base_name = "%s/%s" % (remote, base)
    base_commit = dict(refs).get(base_name)
    if base_commit is None:
        raise CouldNot("there is no %s here - %s"
                       % (base_name, "the fetch did not bring one" if fetched
                          else "run without --no-fetch"))

    cache, at_commit, at_entries = {}, {}, {}

    def ids_of(commit):
        if commit not in at_commit:
            at_commit[commit] = ids_at(root, commit, cache)
        return at_commit[commit]

    def entries_of(one):
        """ONE's entries - a worktree's from its files on disk, anything else's
        from its commit - read only when one of its ids needs comparing."""
        key = ("folder", one.folder) if one.folder else one.commit
        if key not in at_entries:
            try:
                at_entries[key] = entries_in(files_on_disk(one.folder) if one.folder
                                             else texts_at(root, one.commit, cache))
            except OSError:
                # A worktree's file gone since its ids were read: nothing to
                # compare, so nothing is let off TAKEN ON MAIN.
                at_entries[key] = {reg.key: {} for reg in REGISTERS}
        return at_entries[key]

    prefix = remote + "/"
    mine = current_branch(root) or "HEAD"
    holders = [Holder(name, name[len(prefix):], sha) for name, sha in refs]

    # This checkout counts too, when it holds commits no branch on the remote
    # has: an id committed here and not yet pushed is still taken.
    head = head_commit(root)
    if head is not None and head not in {sha for _, sha in refs}:
        holders.append(Holder("HEAD", mine, head))
    for one in holders:
        one.ids = ids_of(one.commit)

    pulls = {"read": False, "repo": None, "why_not": None, "open": []}
    if not fetched:
        pulls["why_not"] = "--no-fetch reads nothing from the network"
    else:
        pulls["repo"] = github
        if pulls["repo"] is None:
            try:
                url = git(root, ["remote", "get-url", remote]).decode("utf-8", "replace")
            except CouldNot:
                url = ""
            pulls["repo"] = github_repo(url)
        if pulls["repo"] is None:
            pulls["why_not"] = ("%s is not a github.com remote - name the repository with "
                                "--github OWNER/NAME" % remote)
        else:
            try:
                listed = open_pulls(gh, root, pulls["repo"])
            except CouldNot as exc:
                pulls["why_not"] = str(exc)
            else:
                pulls["read"] = True
                for pr in listed:
                    pulls["open"].append(read_pull(root, remote, pr, pulls["repo"],
                                                   holders, ids_of))

    trees = read_worktrees(root, mine, holders) if worktrees else None

    # What each piece of work adds: the ids at its head less those where it
    # left main. Main itself adds nothing.
    for one in holders:
        if one.name == base_name:
            one.adds = {reg.key: set() for reg in REGISTERS}
            continue
        start = merge_base(root, one.commit, base_commit) if one.commit else None
        one.compared_with = "merge-base" if start else "main"
        before = ids_of(start or base_commit)
        if one.merging:
            also = ids_of(merge_base(root, one.merging, base_commit) or base_commit)
            before = {reg.key: before[reg.key] | also[reg.key] for reg in REGISTERS}
        one.adds = {reg.key: one.ids[reg.key] - before[reg.key] for reg in REGISTERS}

    pr_of = {}
    for pr in pulls["open"]:
        pr_of.setdefault(pr["owner"], pr["number"])
    # Live: work that will merge as it stands - an open pull request - and this
    # checkout. Only live work is called TAKEN ON MAIN.
    live = set(pr_of) | {mine}

    result = {
        "remote": remote,
        "base": base_name,
        "base_commit": base_commit,
        "fetched": fetched,
        "branches": [name for name, _ in refs if name != base_name],
        "this_checkout": any(one.name == "HEAD" for one in holders),
        "pull_requests": pulls,
        "worktrees": trees,
        "holders": {one.name: {"owner": one.owner, "pr": pr_of.get(one.owner),
                               "this_checkout": one.owner == mine,
                               "compared_with": one.compared_with}
                    for one in holders if one.name != base_name},
        "registers": {},
    }
    main_holder = next(one for one in holders if one.name == base_name)
    main_ids = main_holder.ids
    for reg in REGISTERS:
        on_main = main_ids[reg.key]
        if not on_main:
            raise CouldNot("%s holds no %s at all - the register moved, so this "
                           "tool is reading the wrong place. Fix this tool, do "
                           "not trust it." % (base_name, reg.name))
        main_high = max(on_main, key=reg.order)
        everywhere = set()
        for one in holders:
            everywhere |= one.ids[reg.key]
        high = max(everywhere, key=reg.order)

        above, holds, adders, taken, merged = {}, {}, {}, {}, {}
        for one in holders:
            ahead = sorted((i for i in one.ids[reg.key]
                            if reg.order(i) > reg.order(main_high)), key=reg.order)
            if ahead:
                above[one.name] = [reg.show(i) for i in ahead]
            new = sorted(one.adds[reg.key] - on_main, key=reg.order)
            if new:
                holds[one.name] = [reg.show(i) for i in new]
            for i in new:
                adders.setdefault(i, []).append(one)
            if one.owner in live:
                for i in sorted(one.adds[reg.key] & on_main, key=reg.order):
                    # The very entry main has is this work's own, merged - a
                    # squash leaves its branch adding it against the old
                    # merge-base. Other words under the number are a clash.
                    its = entries_of(one)[reg.key].get(i)
                    if its and its == entries_of(main_holder)[reg.key].get(i):
                        merged.setdefault(i, []).append(one.name)
                    else:
                        taken.setdefault(i, []).append(one.name)
        result["registers"][reg.key] = {
            "name": reg.name,
            "next": reg.show(reg.after(high)),
            "main_highest": reg.show(main_high),
            "highest_anywhere": reg.show(high),
            "above_main": above,
            # What each piece of work adds that main has not got, at any
            # number - below main's highest as well as above it.
            "holds": holds,
            # Two pieces of work adding the same id: one of them renames
            # before it merges, and the sooner it knows the cheaper that is.
            "held_twice": {reg.show(i): [one.name for one in ones] for i, ones in
                           sorted(adders.items(), key=lambda kv: reg.order(kv[0]))
                           if len({one.owner for one in ones}) > 1},
            # Live work adding an id main has taken since it left main.
            "taken_on_main": {reg.show(i): names for i, names in
                              sorted(taken.items(), key=lambda kv: reg.order(kv[0]))},
            # Live work adding an id main has with the same entry: its own.
            "already_merged": {reg.show(i): names for i, names in
                               sorted(merged.items(), key=lambda kv: reg.order(kv[0]))},
        }
    return result


def read_pull(root, remote, pr, repo, holders, ids_of):
    """One open pull request: the branch already read at its head, or its head
    fetched as refs/pull/N/head and read as a holder of its own."""
    fork = pr["repo"] != repo
    owner = "PR #%d" % pr["number"] if fork else pr["branch"]
    entry = {"number": pr["number"], "branch": pr["branch"], "from_fork": fork,
             "owner": owner, "read_as": None, "why_not": None}
    for one in holders:
        if one.commit == pr["sha"] and one.owner == owner:
            entry["read_as"] = one.name
            return entry
    try:
        commit = fetch_pull(root, remote, pr["number"])
    except CouldNot as exc:
        entry["why_not"] = "its head is not a branch this run read, and %s" % exc
        return entry
    one = Holder("PR #%d" % pr["number"], owner, commit, ids_of(commit))
    holders.append(one)
    entry["read_as"] = one.name
    return entry


def read_worktrees(root, mine, holders):
    """Every worktree of this repository, read from disk; each one read is a
    holder of its own."""
    trees = []
    try:
        listed = parse_worktrees(git(root, ["worktree", "list", "--porcelain"])
                                 .decode("utf-8", "replace"))
        top = git(root, ["rev-parse", "--show-toplevel"]).decode("utf-8", "replace").strip()
    except CouldNot as exc:
        return [{"path": None, "branch": None, "this_checkout": False, "read": False,
                 "why_not": "git worktree list did not answer: %s" % exc}]
    for wt in listed:
        this = same_folder(wt["path"], top)
        entry = {"path": wt["path"], "branch": wt["branch"], "this_checkout": this,
                 "read": False, "why_not": None}
        trees.append(entry)
        if wt["bare"]:
            entry["why_not"] = "a bare repository has no working files"
            continue
        if wt["prunable"] or not os.path.isdir(wt["path"]):
            entry["why_not"] = "its folder is gone"
            continue
        try:
            ids = ids_in(files_on_disk(wt["path"]))
        except OSError as exc:
            entry["why_not"] = "its files could not be read: %s" % exc
            continue
        owner = wt["branch"] or (mine if this else "worktree " + wt["path"])
        holders.append(Holder("worktree " + wt["path"], owner, wt["head"], ids,
                              merging=merge_head(wt["path"]), folder=wt["path"]))
        entry["read"] = True
    return trees


def label(result, name):
    """NAME with what it is: its pull request, or none, and whether it is
    this checkout's."""
    one = result["holders"].get(name, {})
    bits = []
    if one.get("pr"):
        bits.append("#%d" % one["pr"])
    elif result["pull_requests"]["read"] and name.startswith(result["remote"] + "/"):
        bits.append("no open PR")
    if one.get("this_checkout"):
        bits.append("this checkout")
    return name + (" (%s)" % ", ".join(bits) if bits else "")


def report(result):
    out = sys.stdout.write
    pulls, trees = result["pull_requests"], result["worktrees"]
    many = lambda n, one, more: "%d %s" % (n, one if n == 1 else more)
    out("Next free id in each register - %s (%s), %s, %s and %s, %s\n\n"
        % (result["base"], result["base_commit"][:8],
           many(len(result["branches"]), "other branch", "other branches"),
           many(len(pulls["open"]), "open pull request", "open pull requests")
           if pulls["read"] else "NO pull request",
           many(sum(1 for t in trees or [] if t["read"]), "worktree", "worktrees"),
           "fetched just now" if result["fetched"]
           else "AS LAST FETCHED - this run did not fetch"))
    for reg in REGISTERS:
        one = result["registers"][reg.key]
        out("  %-24s next %-8s main's highest %s\n"
            % (one["name"], one["next"], one["main_highest"]))
        if not one["holds"]:
            out("      nothing read adds one main has not got\n")
        for name in sorted(one["holds"]):
            out("      %-56s %s\n" % (label(result, name), ", ".join(one["holds"][name])))
        for gid, names in one["held_twice"].items():
            out("      HELD TWICE  %s is added by %s - one of them renames before it merges\n"
                % (gid, " and ".join(label(result, n) for n in names)))
        for gid, names in one["taken_on_main"].items():
            out("      TAKEN ON MAIN  %s is on main already, and %s adds it too - renames "
                "before it merges\n" % (gid, " and ".join(label(result, n) for n in names)))
        for gid, names in one["already_merged"].items():
            out("      MERGED  %s is on main exactly as %s adds it - its own, not a clash\n"
                % (gid, " and ".join(label(result, n) for n in names)))
        out("\n")

    out("What was read\n")
    out("  main            %s at %s\n" % (result["base"], result["base_commit"][:8]))
    out("  branches        %d, %s\n" % (len(result["branches"]),
                                       "fetched just now" if result["fetched"]
                                       else "as last fetched"))
    if pulls["read"]:
        fetched = [p for p in pulls["open"] if p["read_as"] and p["read_as"].startswith("PR #")]
        missed = [p for p in pulls["open"] if not p["read_as"]]
        out("  pull requests   %d open on %s - %d read at their branch, %d fetched as "
            "refs/pull/N/head, %d NOT READ\n"
            % (len(pulls["open"]), pulls["repo"], len(pulls["open"]) - len(fetched) - len(missed),
               len(fetched), len(missed)))
        for p in missed:
            out("                  #%d NOT READ - %s\n" % (p["number"], p["why_not"]))
    else:
        out("  pull requests   NOT READ - %s\n" % pulls["why_not"])
        out("                  so TAKEN ON MAIN judges this checkout alone: a branch whose id\n"
            "                  main took since cannot be told from one that merged\n")
    if trees is None:
        out("  worktrees       not read - --no-worktrees\n")
    for t in trees or []:
        out("  worktree        %s%s%s\n"
            % (t["path"], " (%s)" % t["branch"] if t["branch"] else " (detached)",
               ", this checkout" if t["this_checkout"] else ""))
        if not t["read"]:
            out("                  NOT READ - %s\n" % t["why_not"])
    out("\nFree as of this run, and reserved by nothing: push the branch that takes\n"
        "one early, and run this again before the pull request merges.\n")


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="The next id free on main, on every branch, in every open pull "
                    "request and every worktree here, in each append-only register. "
                    "Reads; writes no register file.")
    parser.add_argument("--no-fetch", action="store_true",
                        help="no network: the branches as last fetched, no pull request")
    parser.add_argument("--json", action="store_true", help="the answer as JSON")
    parser.add_argument("--remote", default="origin")
    parser.add_argument("--base", default="main", help="the branch ids merge into")
    parser.add_argument("--github", default=None, metavar="OWNER/NAME",
                        help="the repository whose open pull requests are read "
                             "(default: from the remote's URL)")
    parser.add_argument("--no-worktrees", action="store_true",
                        help="leave the worktrees' working files unread")
    parser.add_argument("--repo", default=ROOT, help="the checkout to ask (default: this one)")
    args = parser.parse_args(argv)
    try:
        result = answer(args.repo, args.remote, args.base, fetched=not args.no_fetch,
                        github=args.github, worktrees=not args.no_worktrees)
    except CouldNot as exc:
        if args.json:
            sys.stdout.write(json.dumps({"error": str(exc)}, indent=2) + "\n")
        else:
            sys.stdout.write("next-id: could not look - %s\n" % exc)
        return 2
    if args.json:
        sys.stdout.write(json.dumps(result, indent=2) + "\n")
    else:
        report(result)
    return 0


if __name__ == "__main__":
    sys.exit(main())
