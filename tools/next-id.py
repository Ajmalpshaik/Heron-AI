# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Which id is free, in each append-only register - on main AND on every branch.

    python tools/next-id.py               # fetch every branch, then answer
    python tools/next-id.py --no-fetch    # answer from what was last fetched, and say so
    python tools/next-id.py --json        # the same answer, for a machine

Exits 0 with an answer, and 2 when it could not look: no git, a fetch that
failed, no main, or a register in which main holds no id at all - which means
the register moved and this tool is reading the wrong place.

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

So this reads every branch on the remote, as well as main and this checkout,
and answers with the id above the highest one used ANYWHERE - and names the
branches holding ids above main's highest, because those are the pull requests
a new id is racing.

WHAT IT READS, AND HOW
----------------------
Committed files at each branch's head, through `git ls-tree` and one
`git cat-file --batch` - never the working tree, and never a checkout:

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

It sees only what was pushed. An id typed in another session and not pushed
is invisible here, as it is to everyone else - which is the case for pushing
early, not a defect this can fix.
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
FENCE = "`" * 3
# The same three dashes tools/check-decision-titles.py accepts in a heading.
DASHES = chr(0x2014) + chr(0x2013) + "-"

GIT_TIMEOUT = 300


class CouldNot(Exception):
    """This tool could not look. Exit 2, never an answer."""


def git(root, args, data=None):
    """git's stdout as bytes, or CouldNot naming the command and its last line."""
    try:
        done = subprocess.run(["git", "-C", root] + list(args), input=data,
                              capture_output=True, timeout=GIT_TIMEOUT)
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


def page_rows_5b(text):
    """The rows written on the register's page inside section 5b - where a
    branch cut before 2026-09-23's split still keeps every one of them. The
    section ends at the next `## ` heading, so section 5's rows, which have
    their own numbers from 1, are never read as 5b's."""
    found, inside = set(), False
    for line in outside_fences(text):
        if line.startswith("## "):
            inside = line.startswith(SECTION_5B)
            continue
        m = ROW.match(line) if inside else None
        if m:
            found.add(int(m.group(1)))
    return found


def archived_rows(text):
    return {int(m.group(1)) for m in map(ARCHIVED_ROW.match, outside_fences(text)) if m}


def question_ids(text):
    return {int(m.group(1)) for m in map(QUESTION.match, outside_fences(text)) if m}


def decision_ids(text):
    return {int(m.group(1)) for m in map(DECISION.match, outside_fences(text)) if m}


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
    def __init__(self, key, name, sources, show, order, after):
        self.key, self.name, self.sources = key, name, sources
        self.show, self.order, self.after = show, order, after


# Each source is (a path pattern, how its text is read, how a name caught by
# the pattern becomes an id - or None when the name carries no id).
REGISTERS = [
    Register("group", "NEEDS-CHECKING group", [
        (re.compile(r"^docs/NEEDS-CHECKING\.md$"), group_ids, None),
        (re.compile(r"^docs/needs-checking(?:-archive)?/group-([a-z]{1,3})\.md$"),
         group_ids, lambda name: name.upper()),
    ], show=lambda gid: gid, order=group_key, after=group_next),
    Register("5b", "FRAGMENT-ISSUES 5b row", [
        (re.compile(r"^docs/FRAGMENT-ISSUES\.md$"), page_rows_5b, None),
        (re.compile(r"^docs/fragment-issues/section-5b-rows-[0-9-]+\.md$"), table_rows, None),
        (re.compile(r"^docs/fragment-issues-archive/reading-defects-5b-[0-9-]+\.md$"),
         archived_rows, None),
    ], show=lambda n: "5b-%d" % n, order=lambda n: n, after=lambda n: n + 1),
    Register("Q", "open question", [
        (re.compile(r"^docs/OPEN-QUESTIONS\.md$"), question_ids, None),
        (re.compile(r"^docs/open-questions/[^/]+\.md$"), question_ids, None),
    ], show=lambda n: "Q-%d" % n, order=lambda n: n, after=lambda n: n + 1),
    Register("D", "decision", [
        (re.compile(r"^docs/DECISIONS\.md$"), decision_ids, None),
        (re.compile(r"^docs/decisions/D-([0-9]+)\.md$"), decision_ids, int),
    ], show=lambda n: "D-%d" % n, order=lambda n: n, after=lambda n: n + 1),
]

# Everything the sources above can match, so one ls-tree per branch is enough.
PATHS = ["docs/NEEDS-CHECKING.md", "docs/needs-checking", "docs/needs-checking-archive",
         "docs/FRAGMENT-ISSUES.md", "docs/fragment-issues", "docs/fragment-issues-archive",
         "docs/OPEN-QUESTIONS.md", "docs/open-questions",
         "docs/DECISIONS.md", "docs/decisions"]


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


def ids_at(root, commit, cache):
    """{register key: set of ids} at COMMIT. CACHE holds blobs already read,
    since most branches share most files with main."""
    listed = files_at(root, commit)
    missing = [sha for _, sha in listed if sha not in cache]
    cache.update(read_blobs(root, missing))
    found = {reg.key: set() for reg in REGISTERS}
    for path, sha in listed:
        for reg in REGISTERS:
            for pattern, read, named in reg.sources:
                m = pattern.match(path)
                if not m:
                    continue
                found[reg.key] |= read(cache[sha])
                if named is not None:
                    found[reg.key].add(named(m.group(1)))
    return found


# --------------------------------------------------------------------------
# The answer
# --------------------------------------------------------------------------

def answer(root, remote="origin", base="main", fetched=True):
    """Everything this tool says, as one dict. Raises CouldNot."""
    if fetched:
        fetch(root, remote)
    refs = branches(root, remote)
    base_name = "%s/%s" % (remote, base)
    base_commit = dict(refs).get(base_name)
    if base_commit is None:
        raise CouldNot("there is no %s here - %s"
                       % (base_name, "the fetch did not bring one" if fetched
                          else "run without --no-fetch"))

    # This checkout counts too, when it holds commits no branch on the remote
    # has: an id committed here and not yet pushed is still taken.
    sources = list(refs)
    head = head_commit(root)
    if head is not None and head not in {sha for _, sha in refs}:
        sources.append(("HEAD", head))

    cache = {}
    held = {name: ids_at(root, sha, cache) for name, sha in sources}
    others = [name for name, _ in refs if name != base_name]

    result = {
        "remote": remote,
        "base": base_name,
        "base_commit": base_commit,
        "fetched": fetched,
        "branches": others,
        "this_checkout": "HEAD" in held,
        "registers": {},
    }
    for reg in REGISTERS:
        on_main = held[base_name][reg.key]
        if not on_main:
            raise CouldNot("%s holds no %s at all - the register moved, so this "
                           "tool is reading the wrong place. Fix this tool, do "
                           "not trust it." % (base_name, reg.name))
        main_high = max(on_main, key=reg.order)
        everywhere = set()
        for ids in held.values():
            everywhere |= ids[reg.key]
        high = max(everywhere, key=reg.order)

        above, holders = {}, {}
        for name, _ in sources:
            ahead = sorted((i for i in held[name][reg.key]
                            if reg.order(i) > reg.order(main_high)), key=reg.order)
            if ahead:
                above[name] = [reg.show(i) for i in ahead]
                if name != "HEAD":
                    for i in ahead:
                        holders.setdefault(i, []).append(name)
        result["registers"][reg.key] = {
            "name": reg.name,
            "next": reg.show(reg.after(high)),
            "main_highest": reg.show(main_high),
            "highest_anywhere": reg.show(high),
            "above_main": above,
            # Two branches above main with the same id: one of them renames
            # before it merges, and the sooner it knows the cheaper that is.
            "held_twice": {reg.show(i): names for i, names in
                           sorted(holders.items(), key=lambda kv: reg.order(kv[0]))
                           if len(names) > 1},
        }
    return result


def report(result):
    out = sys.stdout.write
    out("Next free id in each register - %s (%s) and %d other branch%s%s, %s\n\n"
        % (result["base"], result["base_commit"][:8], len(result["branches"]),
           "" if len(result["branches"]) == 1 else "es",
           " and this checkout's HEAD" if result["this_checkout"] else "",
           "fetched just now" if result["fetched"]
           else "AS LAST FETCHED - this run did not fetch"))
    for reg in REGISTERS:
        one = result["registers"][reg.key]
        out("  %-24s next %-8s main's highest %s\n"
            % (one["name"], one["next"], one["main_highest"]))
        if not one["above_main"]:
            out("      no branch holds one above main's highest\n")
        for name in sorted(one["above_main"]):
            out("      %-44s %s\n" % (name, ", ".join(one["above_main"][name])))
        for gid, names in one["held_twice"].items():
            out("      HELD TWICE  %s is on %s - one of them renames before it merges\n"
                % (gid, " and ".join(names)))
        out("\n")
    out("Free as of this run, and reserved by nothing: push the branch that takes\n"
        "one early, and run this again before the pull request merges.\n")


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="The next id free on main and on every branch, in each "
                    "append-only register. Reads; writes no register file.")
    parser.add_argument("--no-fetch", action="store_true",
                        help="answer from the branches as last fetched")
    parser.add_argument("--json", action="store_true", help="the answer as JSON")
    parser.add_argument("--remote", default="origin")
    parser.add_argument("--base", default="main", help="the branch ids merge into")
    parser.add_argument("--repo", default=ROOT, help="the checkout to ask (default: this one)")
    args = parser.parse_args(argv)
    try:
        result = answer(args.repo, args.remote, args.base, fetched=not args.no_fetch)
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
