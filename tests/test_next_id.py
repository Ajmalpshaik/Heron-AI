# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/next-id.py - the next free id is free on EVERY branch, not just on main.

    python tests/test_next_id.py

Every repository here is a throwaway with its own local `origin`, and the
registers in it are written by this suite, so nothing here needs the network
or reads the real registers.

WHAT THIS PROVES
  1. THE ORDER OF GROUPS: ET -> EU, AZ -> BA, Z -> AA, ZZ -> AAA, and a
     longer id sorts after a shorter one.
  2. WHAT COUNTS AS AN ID: each register is read where it lives, in both
     layouts a branch may have - a heading in a fenced block, section 5's
     own rows, a second Q-number in a question's heading and a Q-number in
     prose are NOT ids.
  3. WITHOUT A FETCH ONLY MAIN IS SEEN: a clone that has not fetched gives
     the answer every session gave before this tool, and says AS LAST
     FETCHED.
  4. EVERY BRANCH IS READ: with three branches pushed above main, the default
     run fetches, the next id is above all of them, each branch is named with
     the ids it holds, and two branches holding the same id are HELD TWICE.
  5. A BRANCH DELETED ON THE REMOTE stops holding its ids.
  6. THIS CHECKOUT COUNTS: an id committed here and not yet pushed is taken,
     and once pushed it is not counted twice.
  7. IT WRITES NOTHING: the checkout's files, its branch and `git status`
     are the same after a run as before it.
  8. IT SAYS WHEN IT COULD NOT LOOK: no such main, a main with no register,
     and a folder that is not a repository each exit 2 - in --json too.
  9. WHAT A SOURCE LOOKS LIKE: a github.com URL in each spelling gives its
     OWNER/NAME and a local path gives none; gh's lines are read, and a line
     in another shape is refused; a worktree's path is the rest of its line,
     spaces and all.
 10. OPEN PULL REQUESTS, through a fake gh: a pull request whose head is a
     branch read is that branch, labelled; one from a fork is fetched as
     refs/pull/N/head and read; the repository is NAMED to gh, never
     inferred. An id two pull requests add BELOW main's highest is HELD
     TWICE, and an id an open pull request adds that main has taken since is
     TAKEN ON MAIN - a branch with no open pull request never is.
 11. WITHOUT gh: not installed, not logged in, a remote that is not
     github.com, and --no-fetch each answer, exit 0, and say plainly that
     the pull requests were NOT READ - and gh is never called for the last
     two.
 12. WORKTREES: every worktree's working files are read from disk, saved
     and not committed, in a folder whose name holds spaces; a worktree on a
     pushed branch is that branch's work, never a second holder; two
     worktrees typing the same row are HELD TWICE; this checkout's own
     uncommitted row that main has taken is TAKEN ON MAIN; a worktree in the
     middle of merging main is not, for the rows that merge brought; a
     worktree whose folder is gone is NOT READ; and the run leaves every
     worktree as it was.

WHAT IT DOES NOT PROVE. That the real registers keep the shapes it reads.
Running the tool on this repository and reading the ids it names is that
check, and a register that moves is caught there: a main with no id in a
register exits 2 rather than answering. Nor that the real gh answers in the
shape the fake one does - the lines its --jq asks for: a run on this
repository with gh logged in, naming each open pull request's number, is
that check. And Windows paths are parsed here, not walked on Windows.
"""

import importlib.util
import io
import json
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "next-id.py")

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

_spec = importlib.util.spec_from_file_location("next_id", TOOL)
NI = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(NI)

FAILURES = []
FENCE = "`" * 3


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def git(where, *args):
    done = subprocess.run(["git"] + list(args), cwd=where, capture_output=True,
                          text=True, encoding="utf-8", timeout=60)
    if done.returncode != 0:
        raise RuntimeError("git %s failed: %s" % (" ".join(args), done.stderr))
    return done.stdout.strip()


def configure(where):
    git(where, "config", "user.email", "test@example.invalid")
    git(where, "config", "user.name", "Heron test")
    git(where, "config", "commit.gpgsign", "false")


def write(where, files):
    for rel, text in files.items():
        path = os.path.join(where, *rel.split("/"))
        if not os.path.isdir(os.path.dirname(path)):
            os.makedirs(os.path.dirname(path))
        with io.open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)


def commit(where, files, message):
    write(where, files)
    git(where, "add", "--", *sorted(files))
    git(where, "commit", "-q", "-m", message)


def run(*args):
    done = subprocess.run([sys.executable, TOOL] + list(args), capture_output=True,
                          text=True, encoding="utf-8", timeout=300)
    return done.returncode, done.stdout + done.stderr


def as_json(*args):
    code, text = run(*(list(args) + ["--json"]))
    try:
        return code, json.loads(text)
    except ValueError:
        return code, {"unparsed": text}


def snapshot(where):
    """Every file under WHERE, outside .git, with its bytes."""
    found = {}
    for folder, dirs, names in os.walk(where):
        dirs[:] = [d for d in dirs if d != ".git"]
        for name in names:
            path = os.path.join(folder, name)
            with open(path, "rb") as handle:
                found[os.path.relpath(path, where)] = handle.read()
    return found


# --------------------------------------------------------------------------
# The registers on main, written in the shapes the real ones have
# --------------------------------------------------------------------------

MAIN = {
    "docs/NEEDS-CHECKING.md": (
        "# Needs checking\n\n"
        "## Group A — does it build\n\n"
        "**Its own file:** [`needs-checking/group-a.md`](needs-checking/group-a.md)\n\n"
        "## Group AY - the last group on main\n\n"
        "**Its own file:** [`needs-checking/group-ay.md`](needs-checking/group-ay.md)\n\n"
        "## 2026-09-21 — a dated note, not a group\n\n"
        "Group ZQ is named in prose and is not a heading.\n\n"
        + FENCE + "\n## Group ZZ - an example inside a fence\n" + FENCE + "\n"),
    "docs/needs-checking/group-a.md": "# Needs checking — Group A\n",
    "docs/needs-checking/group-ay.md": "# Needs checking — Group AY\n",
    "docs/needs-checking-archive/group-b.md": "# NEEDS-CHECKING archive — Group B\n",
    "docs/FRAGMENT-ISSUES.md": (
        "# Fragment issues\n\n"
        "## 5. HERON'S OWN DEFECTS found by proving\n\n"
        "| # | Defect | State |\n|---|---|---|\n"
        "| **900** | a proving row, numbered on its own | open |\n\n"
        "## 5b. HERON'S OWN DEFECTS found by reading the repository, file by file\n\n"
        "**Its rows, in order:** [001-025](fragment-issues/section-5b-rows-001-025.md)\n\n"
        "## 6. WHAT CANNOT BE RUN AT ALL\n\n"
        "| **901** | not a 5b row either | open |\n"),
    "docs/fragment-issues/section-5-rows-001-025.md": (
        "| # | Defect | State |\n|---|---|---|\n| **950** | a proving row | open |\n"),
    "docs/fragment-issues/section-5b-rows-001-025.md": (
        "| # | Defect | State |\n|---|---|---|\n"
        "| **1** | one | FIXED |\n| **2** | two | open |\n"),
    "docs/fragment-issues/section-5b-rows-026-050.md": (
        "| # | Defect | State |\n|---|---|---|\n| **30** | thirty | open |\n"),
    "docs/fragment-issues-archive/reading-defects-5b-001-025.md": (
        "# archive\n\n### Row 5b-3\n\n*Moved.*\n"),
    "docs/OPEN-QUESTIONS.md": (
        "# Open questions\n\nQ-200 is named in prose, and is not a question.\n"),
    "docs/open-questions/tier-2.md": (
        "## Tier 2\n\n"
        "### \U0001F7E0 Q-7 — an open question\n\n"
        "### ✅ Q-12 — answered; it was Q-99 before a rename\n"),
    "docs/DECISIONS.md": (
        "# Decisions\n\n## Format\n\n" + FENCE + "\n## D-NN — Short title\n"
        "## D-999 — an example inside a fence\n" + FENCE + "\n\n"
        "## D-05 — a decision\n\n## D-41 — the last decision on main\n"),
    "docs/decisions/D-41.md": "# D-41 — the last decision on main\n",
}


def branch(seed, name, files, message):
    git(seed, "checkout", "-q", "-b", name, "main")
    commit(seed, files, message)
    git(seed, "push", "-q", "origin", name)
    git(seed, "checkout", "-q", "main")


def world(home):
    """An origin with main, a clone that saw only main, and three branches
    pushed after it was cloned."""
    origin = os.path.join(home, "origin.git")
    seed = os.path.join(home, "seed")
    work = os.path.join(home, "work")
    git(home, "init", "-q", "--bare", origin)
    git(origin, "symbolic-ref", "HEAD", "refs/heads/main")
    git(home, "clone", "-q", origin, seed)
    configure(seed)
    git(seed, "checkout", "-q", "-b", "main")
    commit(seed, MAIN, "the registers on main")
    git(seed, "push", "-q", "origin", "main")
    git(home, "clone", "-q", origin, work)
    configure(work)

    index = MAIN["docs/NEEDS-CHECKING.md"]
    branch(seed, "feat/one", {
        "docs/NEEDS-CHECKING.md": index + "\n## Group AZ - one's group\n",
        "docs/needs-checking/group-az.md": "# Needs checking — Group AZ\n",
        "docs/fragment-issues/section-5b-rows-026-050.md":
            MAIN["docs/fragment-issues/section-5b-rows-026-050.md"] + "| **31** | one's row | open |\n",
        "docs/open-questions/tier-2.md":
            MAIN["docs/open-questions/tier-2.md"] + "\n### \U0001F535 Q-13 — one's question\n",
        "docs/decisions/D-42.md": "# D-42 — one's decision\n",
    }, "one takes AZ, 31, Q-13 and D-42")
    # TWO takes the same group as ONE - the race this tool exists to show.
    branch(seed, "feat/two", {
        "docs/needs-checking/group-az.md": "# Needs checking — Group AZ\n",
        "docs/fragment-issues/section-5b-rows-026-050.md":
            MAIN["docs/fragment-issues/section-5b-rows-026-050.md"] + "| **33** | two's row | open |\n",
    }, "two takes AZ as well, and 33")
    # OLD was cut before the registers were split: its group and its row are
    # written on the register's page, where every branch kept them then.
    branch(seed, "feat/old", {
        "docs/NEEDS-CHECKING.md": index + "\n## Group BA — written inline\n",
        "docs/FRAGMENT-ISSUES.md": MAIN["docs/FRAGMENT-ISSUES.md"].replace(
            "## 6.", "| # | Defect | State |\n|---|---|---|\n| **40** | inline | open |\n\n## 6."),
    }, "old writes BA and row 40 on the register's page")
    return origin, seed, work


def section_1():
    print("\n1. the order of groups")
    for before, after in (("ET", "EU"), ("EZ", "FA"), ("AZ", "BA"), ("Z", "AA"),
                          ("ZZ", "AAA"), ("A", "B")):
        check(NI.group_next(before) == after, "the group after %s is %s" % (before, after))
    check(sorted(["AA", "Z", "B", "BA", "AZ"], key=NI.group_key) == ["B", "Z", "AA", "AZ", "BA"],
          "a one-letter group sorts before every two-letter one")


def section_2():
    print("\n2. what counts as an id")
    groups = NI.group_ids(MAIN["docs/NEEDS-CHECKING.md"])
    check(groups == {"A", "AY"}, "the index's groups are its Group headings: %s" % sorted(groups))
    check(NI.group_ids(MAIN["docs/needs-checking/group-a.md"]) == {"A"},
          "a group's own file names it in its title")
    check(NI.group_ids(MAIN["docs/needs-checking-archive/group-b.md"]) == {"B"},
          "and so does an archived group's")
    page = NI.page_rows_5b(MAIN["docs/FRAGMENT-ISSUES.md"])
    check(page == set(), "the page's section 5 and section 6 rows are not 5b's: %s" % sorted(page))
    inline = NI.page_rows_5b(MAIN["docs/FRAGMENT-ISSUES.md"].replace(
        "## 6.", "| **40** | inline | open |\n\n## 6."))
    check(inline == {40}, "a row written inside section 5b on the page is: %s" % sorted(inline))
    check(NI.table_rows(MAIN["docs/fragment-issues/section-5b-rows-001-025.md"]) == {1, 2},
          "a rows file's rows are read, and its header row is not")
    check(NI.archived_rows(MAIN["docs/fragment-issues-archive/reading-defects-5b-001-025.md"]) == {3},
          "an archived row is read from its heading")
    questions = NI.question_ids(MAIN["docs/open-questions/tier-2.md"])
    check(questions == {7, 12}, "a question is the first Q-number of its heading: %s" % sorted(questions))
    check(NI.question_ids(MAIN["docs/OPEN-QUESTIONS.md"]) == set(), "a Q-number in prose is not")
    decisions = NI.decision_ids(MAIN["docs/DECISIONS.md"])
    check(decisions == {5, 41}, "decisions are headings outside a fence: %s" % sorted(decisions))


def section_3_to_7(home):
    origin, seed, work = world(home)

    print("\n3. without a fetch only main is seen")
    code, stale = as_json("--repo", work, "--no-fetch")
    regs = stale.get("registers", {})
    check(code == 0, "a run that does not fetch still answers, exit %s" % code)
    check(stale.get("fetched") is False and stale.get("branches") == [],
          "and it says it did not fetch, having seen no branch but main")
    check(regs.get("group", {}).get("next") == "AZ"
          and regs.get("5b", {}).get("next") == "5b-31",
          "from main alone the next group is AZ and the next row 5b-31 - the "
          "answer every session gave before this tool")
    code, text = run("--repo", work, "--no-fetch")
    check("AS LAST FETCHED" in text, "and the plain report says AS LAST FETCHED")

    print("\n4. every branch is read")
    code, got = as_json("--repo", work)
    regs = got.get("registers", {})
    check(code == 0 and got.get("fetched") is True, "the default run fetches and answers, exit %s" % code)
    check(sorted(got.get("branches", [])) == ["origin/feat/old", "origin/feat/one", "origin/feat/two"],
          "it read all three branches: %s" % got.get("branches"))
    expect = {"group": ("BB", "AY"), "5b": ("5b-41", "5b-30"),
              "Q": ("Q-14", "Q-12"), "D": ("D-43", "D-41")}
    for key, (nxt, high) in sorted(expect.items()):
        one = regs.get(key, {})
        check(one.get("next") == nxt and one.get("main_highest") == high,
              "%s: next %s above every branch, main's highest %s (got %s, %s)"
              % (key, nxt, high, one.get("next"), one.get("main_highest")))
    check(regs.get("group", {}).get("above_main") == {
        "origin/feat/one": ["AZ"], "origin/feat/two": ["AZ"], "origin/feat/old": ["BA"]},
        "each branch is named with the groups it holds above main's highest")
    check(regs.get("5b", {}).get("above_main") == {
        "origin/feat/one": ["5b-31"], "origin/feat/two": ["5b-33"], "origin/feat/old": ["5b-40"]},
        "and with its rows - section 5's 900 and 950 are not among them")
    check(regs.get("group", {}).get("held_twice") == {"AZ": ["origin/feat/one", "origin/feat/two"]},
          "AZ on two branches is HELD TWICE, naming both")
    check(regs.get("5b", {}).get("held_twice") == {}, "and a register with no shared id says none")
    code, text = run("--repo", work, "--no-fetch")
    check("HELD TWICE" in text and "origin/feat/old" in text and "next BB" in text,
          "the plain report names the next group, the branches and the clash")

    print("\n5. a branch deleted on the remote")
    git(seed, "push", "-q", "origin", "--delete", "feat/old")
    code, got = as_json("--repo", work)
    regs = got.get("registers", {})
    check(regs.get("group", {}).get("next") == "BA" and regs.get("5b", {}).get("next") == "5b-34",
          "once feat/old is gone its BA and 40 are free again (got %s, %s)"
          % (regs.get("group", {}).get("next"), regs.get("5b", {}).get("next")))

    print("\n6. this checkout counts")
    git(work, "checkout", "-q", "-b", "claude/mine", "origin/main")
    commit(work, {"docs/decisions/D-50.md": "# D-50 — committed here, not pushed\n"},
           "a decision taken here")
    code, got = as_json("--repo", work, "--no-fetch")
    one = got.get("registers", {}).get("D", {})
    check(got.get("this_checkout") is True and one.get("next") == "D-51",
          "an unpushed D-50 here makes the next decision D-51 (got %s)" % one.get("next"))
    check(one.get("above_main", {}).get("HEAD") == ["D-50"], "and HEAD is named as holding it")
    git(work, "push", "-q", "origin", "claude/mine")
    code, got = as_json("--repo", work)
    one = got.get("registers", {}).get("D", {})
    check(got.get("this_checkout") is False and "HEAD" not in one.get("above_main", {})
          and one.get("above_main", {}).get("origin/claude/mine") == ["D-50"],
          "once pushed it is that branch's, and is not counted twice")

    print("\n7. it writes nothing")
    before, where = snapshot(work), git(work, "rev-parse", "--abbrev-ref", "HEAD")
    code, _ = run("--repo", work)
    check(code == 0, "a run that fetches, exit %s" % code)
    check(snapshot(work) == before, "every file in the checkout is byte for byte what it was")
    check(git(work, "rev-parse", "--abbrev-ref", "HEAD") == where,
          "the same branch is checked out")
    check(git(work, "status", "--porcelain") == "", "and git status is clean")
    return origin, seed, work


def section_8(home, seed, work):
    print("\n8. it says when it could not look")
    code, text = run("--repo", work, "--no-fetch", "--base", "nope")
    check(code == 2 and "origin/nope" in text, "no such main: exit 2, naming it (%s)" % code)
    git(seed, "checkout", "-q", "--orphan", "bare")
    git(seed, "rm", "-rq", "--cached", ".")
    for rel in MAIN:
        os.remove(os.path.join(seed, *rel.split("/")))
    commit(seed, {"README.md": "nothing here\n"}, "a branch with no register")
    git(seed, "push", "-q", "origin", "bare")
    code, got = as_json("--repo", work, "--base", "bare")
    check(code == 2 and "register moved" in got.get("error", ""),
          "a main with no register: exit 2, and --json carries the reason")
    nowhere = os.path.join(home, "not-a-repo")
    os.makedirs(nowhere)
    code, text = run("--repo", nowhere, "--no-fetch")
    check(code == 2 and "could not look" in text, "not a repository: exit 2 (%s)" % code)


# --------------------------------------------------------------------------
# Pull requests and worktrees
# --------------------------------------------------------------------------

def named(name, default):
    """NAME from the tool, or DEFAULT when the tool has no such name - so a
    check against a tool without it still RUNS and FAILS."""
    found = getattr(NI, name, None)
    check(found is not None, "the tool has %s at all" % name)
    return found if found is not None else default


def ask(root, **kw):
    """NI.answer with the keywords a tool without them would refuse, as a
    failure rather than a traceback."""
    try:
        return NI.answer(root, **kw)
    except TypeError as exc:
        check(False, "answer() takes %s (%s)" % (sorted(kw), exc))
    except NI.CouldNot as exc:
        check(False, "answer() could look (%s)" % exc)
    return {"registers": {}, "pull_requests": {}, "worktrees": [], "holders": {}}


class FakeGh(object):
    """gh, answering from a script: what it was asked, and what it says."""

    def __init__(self, code=0, out="", err="", missing=False):
        self.code, self.out, self.err, self.missing = code, out, err, missing
        self.calls = []

    def __call__(self, args, root):
        self.calls.append(list(args))
        if self.missing:
            raise OSError(2, "No such file or directory: 'gh'")
        return self.code, self.out, self.err


def pull_line(number, branch, sha, repo):
    return "%d\t%s\t%s\t%s\n" % (number, branch, sha, repo)


def section_9():
    print("\n9. what a source looks like")
    github_repo = named("github_repo", lambda url: None)
    for url in ("https://github.com/Ajmalpshaik/Heron-AI",
                "https://github.com/Ajmalpshaik/Heron-AI.git\n",
                "https://someone@github.com/Ajmalpshaik/Heron-AI/",
                "git@github.com:Ajmalpshaik/Heron-AI.git",
                "ssh://git@github.com/Ajmalpshaik/Heron-AI.git"):
        check(github_repo(url) == "Ajmalpshaik/Heron-AI",
              "%r names Ajmalpshaik/Heron-AI (got %r)" % (url.strip(), github_repo(url)))
    for url in ("/home/someone/origin.git", "C:/Users/Ajmal PS/origin.git",
                "https://gitlab.com/a/b.git"):
        check(github_repo(url) is None, "%r is not a github.com remote" % url)

    parse_pulls = named("parse_pulls", lambda text: [])
    got = parse_pulls(pull_line(590, "claude/x", "abc123", "Ajmalpshaik/Heron-AI")
                      + "\n" + pull_line(7, "main", "def456", ""))
    check(got == [{"number": 590, "branch": "claude/x", "sha": "abc123",
                   "repo": "Ajmalpshaik/Heron-AI"},
                  {"number": 7, "branch": "main", "sha": "def456", "repo": ""}],
          "gh's lines are read, a deleted fork's empty repository too: %s" % got)
    try:
        parse_pulls('[{"number": 590}]\n')
        refused = False
    except Exception as exc:
        refused = "does not read" in str(exc)
    check(refused, "an answer in another shape is refused, not skipped")

    parse_worktrees = named("parse_worktrees", lambda text: [])
    porcelain = ("worktree C:/Users/Ajmal PS/Heron Ai\r\nHEAD " + "a" * 40 + "\r\n"
                 "branch refs/heads/main\r\n\r\n"
                 "worktree C:/Users/Ajmal PS/Heron Ai/.claude/worktrees/two words\n"
                 "HEAD " + "b" * 40 + "\ndetached\n\n"
                 "worktree /srv/gone\nHEAD " + "c" * 40 + "\nbranch refs/heads/claude/x\n"
                 "prunable gitdir file points to non-existent location\n\n"
                 "worktree /srv/bare.git\nbare\n")
    got = parse_worktrees(porcelain)
    check([w["path"] for w in got] == ["C:/Users/Ajmal PS/Heron Ai",
                                       "C:/Users/Ajmal PS/Heron Ai/.claude/worktrees/two words",
                                       "/srv/gone", "/srv/bare.git"],
          "every path is the rest of its line, spaces kept: %s" % [w["path"] for w in got])
    check([w["branch"] for w in got] == ["main", None, "claude/x", None]
          and [w["prunable"] for w in got] == [False, False, True, False]
          and [w["bare"] for w in got] == [False, False, False, True],
          "and its branch, detached, prunable and bare are read")


def section_10_and_11(home):
    os.makedirs(home)
    origin, seed, work = world(home)
    sha = lambda ref: git(seed, "rev-parse", ref)

    # Main moves on after the branches were cut: another pull request merged
    # 5b-31 - the row feat/one holds - and main's highest passes 50.
    rows = MAIN["docs/fragment-issues/section-5b-rows-026-050.md"]
    commit(seed, {"docs/fragment-issues/section-5b-rows-026-050.md":
                  rows + "| **31** | another pull request's row, merged first | open |\n"
                         "| **60** | a round number, merged | open |\n"},
           "another pull request merges 31 and 60")
    git(seed, "push", "-q", "origin", "main")
    # THREE and FOUR both take 50 - below main's highest now, the race that
    # comparing above main's highest alone never saw.
    for name in ("feat/three", "feat/four"):
        branch(seed, name, {"docs/fragment-issues/section-5b-rows-026-050.md":
                            rows + "| **50** | %s's row | open |\n" % name}, "%s takes 50" % name)
    # A fork's pull request: its head is on the remote only as refs/pull/7/head.
    git(seed, "checkout", "-q", "-b", "fork-work", "main")
    commit(seed, {"docs/needs-checking/group-bc.md": "# Needs checking — Group BC\n",
                  "docs/fragment-issues/section-5b-rows-026-050.md":
                  rows + "| **45** | the fork's row | open |\n"}, "a fork takes BC and 45")
    git(seed, "push", "-q", "origin", "fork-work:refs/pull/7/head")
    fork_sha = sha("fork-work")
    git(seed, "checkout", "-q", "main")
    git(work, "fetch", "-q", "origin")

    print("\n10. open pull requests, through a fake gh")
    code, got = as_json("--repo", work)
    check(code == 0 and got.get("registers", {}).get("5b", {}).get("held_twice", {}).get("5b-50")
          == ["origin/feat/four", "origin/feat/three"],
          "from the branches alone, with no gh: 50, added by two branches BELOW main's "
          "highest 60, is HELD TWICE (got %s)"
          % got.get("registers", {}).get("5b", {}).get("held_twice"))
    gh = FakeGh(out=pull_line(11, "feat/one", sha("origin/feat/one"), "acme/heron")
                + pull_line(12, "feat/two", sha("origin/feat/two"), "acme/heron")
                + pull_line(13, "feat/three", sha("origin/feat/three"), "acme/heron")
                + pull_line(14, "feat/four", sha("origin/feat/four"), "acme/heron")
                + pull_line(7, "main", fork_sha, "someone/heron"))
    got = ask(work, gh=gh, github="acme/heron")
    pulls, regs = got.get("pull_requests", {}), got.get("registers", {})
    check(pulls.get("read") is True and len(pulls.get("open", [])) == 5,
          "five open pull requests are read: %s" % pulls.get("why_not"))
    check(len(gh.calls) == 1 and "repos/acme/heron/pulls?state=open&per_page=100" in gh.calls[0]
          and gh.calls[0][0] == "api",
          "gh is asked once, through the REST api, with the repository NAMED: %s" % gh.calls)
    by_number = {p["number"]: p for p in pulls.get("open", [])}
    check(by_number.get(11, {}).get("read_as") == "origin/feat/one",
          "a pull request whose head is a branch read is that branch")
    check(got.get("holders", {}).get("origin/feat/one", {}).get("pr") == 11,
          "and the branch is labelled with its number")
    check(by_number.get(7, {}).get("read_as") == "PR #7" and by_number.get(7, {}).get("from_fork"),
          "the fork's is fetched as refs/pull/7/head and read as PR #7")
    holds = regs.get("5b", {}).get("holds", {})
    check(holds.get("PR #7") == ["5b-45"] and regs.get("group", {}).get("holds", {})
          .get("PR #7") == ["BC"], "and what it adds is read: %s" % holds.get("PR #7"))
    check(regs.get("group", {}).get("next") == "BD",
          "the next group is above the fork's BC too (got %s)" % regs.get("group", {}).get("next"))
    check(regs.get("5b", {}).get("held_twice", {}).get("5b-50") ==
          ["origin/feat/four", "origin/feat/three"],
          "50, added by two pull requests BELOW main's highest 60, is HELD TWICE: %s"
          % regs.get("5b", {}).get("held_twice"))
    check(regs.get("5b", {}).get("taken_on_main") == {"5b-31": ["origin/feat/one"]},
          "31, which feat/one (#11) adds and main has since merged, is TAKEN ON MAIN: %s"
          % regs.get("5b", {}).get("taken_on_main"))
    check("5b-31" not in holds.get("origin/feat/one", []),
          "and it is not listed among what feat/one holds, since main has it")
    gh_no_two = FakeGh(out=pull_line(11, "feat/one", sha("origin/feat/one"), "acme/heron"))
    commit(seed, {"docs/fragment-issues/section-5b-rows-026-050.md":
                  rows + "| **31** | another pull request's row, merged first | open |\n"
                         "| **33** | two's row, as it merged | open |\n"
                         "| **60** | a round number, merged | open |\n"}, "two merges 33")
    git(seed, "push", "-q", "origin", "main")
    got = ask(work, gh=gh_no_two, github="acme/heron")
    taken = got.get("registers", {}).get("5b", {}).get("taken_on_main", {})
    check("5b-33" not in taken and taken.get("5b-31") == ["origin/feat/one"],
          "feat/two has no open pull request - it may be one that merged - so its 33 "
          "on main is not TAKEN ON MAIN: %s" % taken)
    code, text = run_with(work, gh, github="acme/heron")
    check("HELD TWICE  5b-50" in text and "TAKEN ON MAIN  5b-31" in text
          and "origin/feat/one (#11)" in text and "PR #7" in text,
          "the plain report names the clash, the taken row, the labels and the fork")

    print("\n11. without gh")
    for fake, said in ((FakeGh(missing=True), "not installed"),
                       (FakeGh(code=4, err="To get started with GitHub CLI, please run:  "
                                           "gh auth login\n"), "gh auth login"),
                       (FakeGh(code=1, err="HTTP 401: Bad credentials\n"), "Bad credentials")):
        got = ask(work, gh=fake, github="acme/heron")
        pulls = got.get("pull_requests", {})
        check(pulls.get("read") is False and said in (pulls.get("why_not") or "")
              and got.get("registers", {}).get("5b", {}).get("next") == "5b-61",
              "%s: still answers, and says why the pull requests were not read (%s)"
              % (said, pulls.get("why_not")))
    fake = FakeGh(out=pull_line(11, "feat/one", "x", "acme/heron"))
    got = ask(work, gh=fake)
    check(fake.calls == [] and "github.com" in (got.get("pull_requests", {}).get("why_not") or ""),
          "a remote that is not github.com: gh is never called, and it says to name one")
    got = ask(work, gh=fake, github="acme/heron", fetched=False)
    check(fake.calls == [] and "--no-fetch" in (got.get("pull_requests", {}).get("why_not") or ""),
          "--no-fetch: gh is never called either")
    code, text = run("--repo", work)
    check(code == 0 and "pull requests   NOT READ" in text and "this checkout alone" in text,
          "the plain report says NOT READ, and what that leaves unjudged, exit %s" % code)


def run_with(root, gh, github=None):
    """The plain report, from the module with a fake gh, and the exit code."""
    out, saved = io.StringIO(), sys.stdout
    sys.stdout = out
    try:
        result = ask(root, gh=gh, github=github)
        if result.get("registers"):
            NI.report(result)
    finally:
        sys.stdout = saved
    return 0, out.getvalue()


def section_12(home):
    os.makedirs(home)
    origin, seed, work = world(home)
    rows_file = "docs/fragment-issues/section-5b-rows-026-050.md"
    rows = MAIN[rows_file]
    index = MAIN["docs/NEEDS-CHECKING.md"]

    print("\n12. worktrees")
    git(work, "fetch", "-q", "origin")
    spaced = os.path.join(home, "Heron Ai", "two words")
    os.makedirs(os.path.dirname(spaced))
    git(work, "worktree", "add", "-q", "-b", "claude/local", spaced, "origin/main")
    # Saved, not committed: a new group's file and its heading, and a row.
    write(spaced, {"docs/needs-checking/group-bd.md": "# Needs checking — Group BD\n",
                   "docs/NEEDS-CHECKING.md": index + "\n## Group BD - typed, not committed\n",
                   rows_file: rows + "| **70** | typed in the spaced worktree | open |\n"})
    # A worktree on a branch already pushed: its 33 is feat/two's, once.
    pushed = os.path.join(home, "on two")
    git(work, "worktree", "add", "-q", "-b", "feat/two", pushed, "origin/feat/two")
    # Another worktree typing the same row 70.
    other = os.path.join(home, "other")
    git(work, "worktree", "add", "-q", "-b", "claude/other", other, "origin/main")
    write(other, {rows_file: rows + "| **70** | typed in another worktree | open |\n"})
    # A worktree of a pull request that will merge main in, once main has moved.
    merging = os.path.join(home, "merging")
    git(work, "worktree", "add", "-q", "-b", "claude/merging", merging, "origin/main")
    git(merging, "push", "-q", "origin", "claude/merging")
    # A worktree whose folder has gone.
    gone = os.path.join(home, "gone")
    git(work, "worktree", "add", "-q", "-b", "claude/gone", gone, "origin/main")
    shutil.rmtree(gone)
    # This checkout's own row, typed while main merged the same number.
    git(work, "checkout", "-q", "-b", "claude/here", "origin/main")
    write(work, {rows_file: rows + "| **80** | typed here | open |\n"})
    commit(seed, {rows_file: rows + "| **80** | merged first by somebody else | open |\n"},
           "main takes 80")
    git(seed, "push", "-q", "origin", "main")
    git(work, "fetch", "-q", "origin")
    git(merging, "merge", "-q", "--no-commit", "--no-ff", "origin/main")

    states = {where: (snapshot(where), git(where, "status", "--porcelain"))
              for where in (work, spaced, pushed, other, merging)}
    got = ask(work, gh=FakeGh(missing=True))
    regs, trees = got.get("registers", {}), got.get("worktrees") or []
    by_path = {os.path.normcase(os.path.realpath(t["path"])): t for t in trees if t.get("path")}
    key = lambda path: os.path.normcase(os.path.realpath(path))
    check(by_path.get(key(spaced), {}).get("read") is True,
          "the worktree in a folder whose name holds spaces is read: %s"
          % [t.get("path") for t in trees])
    name = "worktree " + by_path.get(key(spaced), {}).get("path", spaced)
    check(regs.get("group", {}).get("holds", {}).get(name) == ["BD"]
          and "5b-70" in regs.get("5b", {}).get("holds", {}).get(name, []),
          "its saved, uncommitted group BD and row 70 are what it holds")
    check(regs.get("group", {}).get("next") == "BE" and regs.get("5b", {}).get("next") == "5b-81",
          "and the next group and row are above them (got %s, %s)"
          % (regs.get("group", {}).get("next"), regs.get("5b", {}).get("next")))
    held = regs.get("5b", {}).get("held_twice", {})
    check("5b-33" not in held,
          "the worktree on pushed feat/two is that branch's work, not a second holder: %s" % held)
    check(len(held.get("5b-70", [])) == 2,
          "two worktrees typing row 70 are HELD TWICE: %s" % held.get("5b-70"))
    check(regs.get("5b", {}).get("taken_on_main", {}).get("5b-80") ==
          ["worktree " + by_path.get(key(work), {}).get("path", work)],
          "this checkout's uncommitted 80, which main has since taken, is TAKEN ON MAIN: %s"
          % regs.get("5b", {}).get("taken_on_main"))
    check(by_path.get(key(gone), {}).get("read") is False
          and "gone" in (by_path.get(key(gone), {}).get("why_not") or ""),
          "a worktree whose folder is gone is NOT READ, and says why")
    check(all((snapshot(where), git(where, "status", "--porcelain")) == states[where]
              for where in states),
          "every worktree's files and git status are what they were before the run")
    gh = FakeGh(out=pull_line(21, "claude/merging", git(work, "rev-parse", "origin/claude/merging"),
                              "acme/heron"))
    got = ask(work, gh=gh, github="acme/heron")
    taken = got.get("registers", {}).get("5b", {}).get("taken_on_main", {})
    check(taken.get("5b-80") == ["worktree " + by_path.get(key(work), {}).get("path", work)],
          "a pull request's worktree in the middle of merging main holds main's 80 on disk, "
          "and is not called TAKEN ON MAIN for it: %s" % taken)
    code, text = run("--repo", work, "--no-fetch")
    check(code == 0 and "Heron Ai" in text and "NOT READ - its folder is gone" in text,
          "the plain report names each worktree read, spaces kept, and the one not read")
    got = ask(work, gh=FakeGh(missing=True), worktrees=False)
    check(got.get("worktrees") is None
          and "5b-70" not in str(got.get("registers", {}).get("5b", {}).get("holds")),
          "--no-worktrees leaves the working files unread")


def main():
    print("tools/next-id.py")
    section_1()
    section_2()
    home = tempfile.mkdtemp(prefix="heron-next-id-")
    try:
        _, seed, work = section_3_to_7(home)
        section_8(home, seed, work)
        section_9()
        section_10_and_11(os.path.join(home, "pulls"))
        section_12(os.path.join(home, "trees"))
    finally:
        shutil.rmtree(home, ignore_errors=True)
    print("\n%s" % ("%d FAILED" % len(FAILURES) if FAILURES else "all passed"))
    for what in FAILURES:
        print("  - %s" % what)
    return 1 if FAILURES else 0


if __name__ == "__main__":
    sys.exit(main())
