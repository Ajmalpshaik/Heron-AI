# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Turn one register finding into a GitHub issue, and write the issue's number
back where the finding is written.

    python tools/open-issue.py 5b-1884                  # print the issue it would open - sends and writes nothing
    python tools/open-issue.py 5b-1884 --kind revit-run --title "..." --happened "..." --expected "..."
    python tools/open-issue.py 5b-1884 --create         # open it with gh, then write #N into the row
    python tools/open-issue.py 5b-1884 --into 1078      # print the line it would add to open issue #1078
    python tools/open-issue.py 5b-1884 --into 1078 --create
    python tools/open-issue.py 5b-1884 --record 1092    # opened another way: write #1092 back, send nothing

THE IDS IT READS
  5b-N    a FRAGMENT-ISSUES row of section 5b - its own file, or a rows file
  5-N     a FRAGMENT-ISSUES row of section 5 - "row N" reads the same
  OQ      a NEEDS-CHECKING group, by its letters - every check in it not yet struck
  OQ15    one NEEDS-CHECKING check
  Q-94    an open question

EXIT CODES
  0  printed, or done
  1  refused - the id is not there, not open, already tracked, or the text
     carries a credential or a personal path; it says which
  2  could not read: no such register file, or a row its readers cannot split
  3  NOT RUN - GitHub could not be reached (no gh, not logged in, a container
     that refuses it). Nothing was sent and nothing was written. The issue is
     listed under "Issues to open" in the pull request, and the session that
     opens it writes the number back with --record N

WHY THIS EXISTS (D-187)
-----------------------
The owner, Ajmal PS, 2026-11-20: every mistake or owed check an AI finds in
Heron becomes a GitHub issue at once, not only a row in a register that
somebody turns into an issue later. The register stays the permanent record -
next-id.py, open-defects.py, the archive and the gates all read it - so a new
row, check or question is written there AND opened as an issue in the same
change, and its state names the issue from the day it is written. This does
the second half the same way every time, so the issue reads like the ones the
owner already works through, and the link goes both ways: the issue names the
row's file, and the row's state names the issue.

WHAT IT WRITES BACK, in the forms the registers already use
  a row       its state cell begins MOVED TO ISSUE [#N](url) <date>, and
              everything it said before follows "Was:" - open-defects.py
              counts it as moved, archive-fragment-issues.py moves it as one
  a group     "Tracked as GitHub issue [#N](url) since <date>" under its
              heading, as every group moved on 2026-11-16 carries
  one check   a plain line under the group's heading naming the check and
              the issue - plain, because the register's readers count a check
              by its BOLD id, and a second bold copy would count it twice
  a question  "Tracked as GitHub issue [#N](url) since <date>" under its
              heading, as Q-93 carried

WHAT IT SENDS, AND WHAT IT NEVER SENDS
  Only text already in this repository's registers - the row, check or
  question - and the words given with --title, --happened and --expected.
  Nothing from a model, nothing from the PC. Before anything is sent the whole
  issue is checked against the credential shapes brain/heron_secrets.py
  removes from a log (the same list check-secrets.py reads), an e-mail
  address and a user's own folder (C:\\Users\\<name>, /Users/<name>); any one
  of them refuses the run, naming the line.

  It talks to GitHub through `gh api` - the REST calls, because `gh issue
  create` asks GraphQL and a Claude Code cloud session refuses that (measured
  2026-11-20, the same 403 next-id.py records for `gh pr list`).

WHAT IT DOES NOT DO
  It does not decide what the issue is about, which kind it is, or which open
  issue shares its subject - that is a reading job, and it is the
  heron-issue-recorder agent's. Run with nothing but an id it fills the words
  from the row itself: the row's bold lead as what happened, its "Owed" part
  as what is expected. Those read like a register, not like the owner's
  issues, so the agent passes plain words with --title, --happened and
  --expected.

  It adds no line to docs/fragment-issues/ISSUES.md. That page's tables map
  the rows moved before 2026-11-20; since then a row's own state is its link
  (D-187), for the reason a new 5b row is a file of its own - one table every
  branch appends to is one conflict in every merge (row 5b-1585).
"""

from __future__ import print_function

import argparse
import datetime
import importlib.util
import io
import json
import os
import re
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_secrets as SECRETS  # noqa: E402


def _load(name, filename):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


# One rule for which repository a remote names, and one way of running gh:
# next-id's.
NI = _load("next_id", "next-id.py")

FOOTER = "---\n_Generated by [Claude Code](https://claude.ai/code)_"
TITLE_MAX = 100

# --------------------------------------------------------------------------
# The kinds, as the owner's issues name them
# --------------------------------------------------------------------------

# key: (title word, label, the part of ISSUES.md "Working an issue" to follow,
#       the kind line, what the owner does, when it closes)
KINDS = {
    "fix": (
        "Fix", "bug", "FIX",
        "**Kind: code fix** - an AI session does it; you merge the result. No Revit is needed unless "
        "the row says so.",
        "Nothing until its pull request is ready. Any Claude Code session in the Heron folder - a cloud "
        "session is fine - takes the prompt below, and you merge what it opens.",
        "Every box is ticked. The pull request that ticks the last box says `Closes` with this "
        "issue's number."),
    "build": (
        "Build", "bug", "FIX",
        "**Kind: build** - an AI session builds it at DRAFT; you merge the result, and proving it is "
        "a Revit run on your PC afterwards.",
        "Nothing until its pull request is ready. Any Claude Code session in the Heron folder - a cloud "
        "session is fine - takes the prompt below, and you merge what it opens.",
        "Every box is ticked. The pull request that ticks the last box says `Closes` with this "
        "issue's number."),
    "tidy-up": (
        "Tidy-up", "documentation", "FIX",
        "**Kind: tidy-up** - an AI session does it; anything it would change in a tool comes to you "
        "first.",
        "Open Claude Code in the Heron folder (any PC; a cloud session is fine), paste the prompt "
        "below, and answer what it asks.",
        "Every box is ticked. The pull request that ticks the last box says `Closes` with this "
        "issue's number."),
    "revit-run": (
        "Revit run", "needs-revit-run", "REVIT RUN",
        "**Kind: Revit run on your PC** - Claude runs it through Heron on a scratch copy of a sample "
        "model, never a job model; you press Keep or Discard, and you sign.",
        "On your Windows PC, open Claude Code in the Heron folder, paste the prompt below, and answer "
        "what it asks.",
        "Every box is ticked: each run on a scratch copy, its result written where the row says - "
        "the Revit release, the model and the date."),
    "pc-run": (
        "PC run", "needs-revit-run", "REVIT RUN",
        "**Kind: PC run** - on your Windows PC; it needs the PC, not Revit.",
        "On your Windows PC, open Claude Code in the Heron folder, paste the prompt below, and answer "
        "what it asks.",
        "Every box is ticked: each run on your PC, its result written where the row says, with "
        "the date."),
    "decision": (
        "Your decision", "question", "DECISION",
        "**Kind: your decision** - nothing is changed until you answer.",
        "Answer in a comment below, one line a row, in your own words. An AI session then writes your "
        "answer in, dated, and makes any change it needs.",
        "Every box is ticked: your answer is written into the row in your words, and any change it "
        "needed is merged."),
}

# What each part of "Working an issue" adds to the ready prompt.
PROMPT_TAIL = {
    "FIX": "Ask me in plain words before any choice.",
    "REVIT RUN": "Scratch copies of sample models only - never my job model. Ask me before signing "
                 "anything in my name.",
    "DECISION": "Ask me each question in plain words and wait for my answer.",
    "NEEDS-CHECKING": "Scratch copies of sample models only - never my job model. Ask me before "
                      "signing anything in my name.",
    "QUESTION OR PROPOSAL": "Ask me the question in plain words and wait for my answer.",
}


class Refused(Exception):
    """The run is refused: exit 1, with the reason."""


class CouldNotRead(Exception):
    """A register file could not be read or split: exit 2."""


class CouldNotReach(Exception):
    """GitHub could not be reached: exit 3, NOT RUN."""


# --------------------------------------------------------------------------
# Text helpers
# --------------------------------------------------------------------------

CELL = re.compile(r"(?<!\\)\|")            # an unescaped pipe - open-defects.py's rule
LINK = re.compile(r"\[([^\]]*)\]\([^)]*\)")
CODE = re.compile(r"(`[^`]*`)")
DATE = re.compile(r"^\d{4}-\d{2}-\d{2}$")


def plain(text):
    """Markdown links read as their words, bold marks dropped, one line."""
    text = LINK.sub(r"\1", text)
    text = text.replace("**", "")
    return re.sub(r"\s+", " ", text).strip()


# Kept as written when a capitals lead is put in sentence case: code, a link's
# words (a name - "Group JB", a card), a word with a digit or an underscore
# (5B-1585, JB1, SET_FAMILY_SETTINGS), and the names below.
KEPT = re.compile(r"(`[^`]*`|\[[^\]]*\]\([^)]*\)|\b[\w-]*[\d_][\w-]*\b)")
NAMES = {"revit": "Revit", "heron": "Heron", "mep": "MEP", "hvac": "HVAC", "ifc": "IFC",
         "bim": "BIM", "mcp": "MCP", "github": "GitHub", "windows": "Windows", "claude": "Claude",
         "draft": "DRAFT", "proven": "PROVEN", "deprecated": "DEPRECATED", "readme": "README",
         "needs-checking": "NEEDS-CHECKING", "fragment-issues": "FRAGMENT-ISSUES", "ok": "OK",
         "id": "id", "ids": "ids", "csv": "CSV", "pdf": "PDF", "dwg": "DWG", "api": "API"}


def calm(text):
    """A register lead written in capitals, in sentence case - names kept as written."""
    pieces = KEPT.split(text)
    loose = "".join(p for i, p in enumerate(pieces) if i % 2 == 0)
    letters = [c for c in loose if c.isalpha()]
    if not letters or sum(1 for c in letters if c.isupper()) < 0.7 * len(letters):
        return text
    out = []
    for i, piece in enumerate(pieces):
        if i % 2:
            out.append(piece)
            continue
        out.append(re.sub(r"[A-Za-z][A-Za-z-]*",
                          lambda m: NAMES.get(m.group(0).lower(), m.group(0).lower()), piece))
    out = "".join(out)
    m = re.search(r"[A-Za-z`\[]", out)
    if m and out[m.start()].isalpha():
        out = out[:m.start()] + out[m.start()].upper() + out[m.start() + 1:]
    return out


def short(text, limit):
    """TEXT cut at a word boundary before LIMIT characters."""
    text = text.strip()
    if len(text) <= limit:
        return text
    cut = text[:limit - 3].rsplit(" ", 1)[0].rstrip(" ,;:-")
    return cut + "..."


def lines_of(text):
    """The lines of TEXT, each with its own line ending."""
    return text.splitlines(True)


def eol_of(text):
    return "\r\n" if "\r\n" in text else "\n"


def read(path):
    try:
        with io.open(path, encoding="utf-8", newline="") as handle:
            return handle.read()
    except (OSError, UnicodeDecodeError) as exc:
        raise CouldNotRead("%s could not be read: %s" % (path, exc))


def write(path, text):
    """Write TEXT to PATH whole, or leave PATH as it was."""
    folder = os.path.dirname(path)
    handle, scratch = tempfile.mkstemp(prefix=".open-issue-", dir=folder)
    try:
        with io.open(handle, "w", encoding="utf-8", newline="") as out:
            out.write(text)
        os.replace(scratch, path)
    except OSError:
        if os.path.exists(scratch):
            os.remove(scratch)
        raise


# --------------------------------------------------------------------------
# A finding, read from its register
# --------------------------------------------------------------------------

class Finding(object):
    """One row, group, check or question, and how to write its issue back."""

    def __init__(self, what, show, path, line, title, happened, expected):
        self.what = what            # row | group | check | question
        self.show = show            # 5b-1884, row 18, Group OQ, OQ15, Q-94
        self.path = path            # relative to the root, / separators
        self.line = line            # 1-based line the finding starts on
        self.title = title
        self.happened = happened
        self.expected = expected
        self.items = []             # [(id, words)] for a group or a check
        self.tracked = None         # the issue a group already names
        self.cells = None           # a row's cells, split on unescaped pipes

    def link(self, repo, anchor=True):
        url = "https://github.com/%s/blob/main/%s" % (repo, self.path)
        if anchor and not self.path.startswith("docs/fragment-issues/row-5b-"):
            url += "?plain=1#L%d" % self.line
        return url


ROW_ID = re.compile(r"^(?:row\s+)?(5b?)-([1-9][0-9]*)$", re.I)
ROW_5 = re.compile(r"^row\s+([1-9][0-9]*)$", re.I)          # section 5's rows go by their number
QUESTION_ID = re.compile(r"^Q-([1-9][0-9]*[a-z]?)$", re.I)
GROUP_ID = re.compile(r"^(?:group[\s-]+)?([A-Za-z]{1,2})$", re.I)
CHECK_ID = re.compile(r"^([A-Za-z]{1,2})([0-9]+[a-z]?)$")


def find(root, ident):
    """The Finding IDENT names, or Refused / CouldNotRead."""
    ident = ident.strip()
    m = ROW_ID.match(ident)
    if m:
        return find_row(root, m.group(1).lower(), int(m.group(2)))
    m = ROW_5.match(ident)
    if m:
        return find_row(root, "5", int(m.group(1)))
    m = QUESTION_ID.match(ident)
    if m:
        return find_question(root, "Q-" + m.group(1))
    m = CHECK_ID.match(ident)
    if m:
        return find_group(root, m.group(1).upper(), m.group(1).upper() + m.group(2))
    m = GROUP_ID.match(ident)
    if m:
        return find_group(root, m.group(1).upper(), None)
    raise Refused("%r is not an id this reads - 5b-N, 5-N or row N, a group's letters (OQ), one "
                  "check (OQ15) or Q-N" % ident)


def find_row(root, section, number):
    folder = os.path.join(root, "docs", "fragment-issues")
    if not os.path.isdir(folder):
        raise CouldNotRead("docs/fragment-issues/ is not there")
    files = []
    if section == "5b":
        own = "row-5b-%d.md" % number
        if os.path.exists(os.path.join(folder, own)):
            files.append(own)
    stem = "section-%s-rows-" % section
    files += sorted(n for n in os.listdir(folder) if n.startswith(stem) and n.endswith(".md"))
    pattern = re.compile(r"^\|\s*\*{0,2}%d\*{0,2}\s*\|" % number)
    show = "5b-%d" % number if section == "5b" else "row %d" % number
    said_as = "row " + show if section == "5b" else show
    hits = []
    for name in files:
        for index, line in enumerate(lines_of(read(os.path.join(folder, name)))):
            if pattern.match(line):
                hits.append((name, index + 1, line))
    if not hits:
        raise Refused("%s is in no file of docs/fragment-issues/ - write it first (a 5b row is "
                      "docs/fragment-issues/row-5b-N.md, N from tools/next-id.py)" % said_as)
    if len(hits) > 1:
        raise Refused("%s is written %d times (%s) - renumber one with tools/renumber.py first"
                      % (said_as, len(hits), ", ".join("%s:%d" % (n, i) for n, i, _ in hits)))
    name, at, line = hits[0]
    cells = CELL.split(line.rstrip("\r\n"))
    if len(cells) != 5:
        raise CouldNotRead("%s in %s has %d cells, not 3 - a literal | in its text splits it; "
                           "write it as \\|" % (said_as, name, len(cells) - 2))
    defect, state = cells[2], cells[3]
    said = re.sub(r"[`*]", "", state).strip()
    if re.match(r"^moved to issue\b", said, re.I):
        raise Refused("%s is already tracked as a GitHub issue - its state reads: %s"
                      % (said_as, short(said, 120)))
    if not said.lower().startswith("open"):
        raise Refused("%s's state does not begin OPEN, so open-defects.py does not count it open "
                      "- it reads: %s" % (said_as, short(said, 120)))

    lead = re.match(r"\s*\*\*(.+?)\*\*", defect)
    lead = plain(calm(lead.group(1))) if lead else plain(calm(defect)).split(". ")[0]
    lead = lead.rstrip(".")
    owed = re.search(r"\bOwed\b[^:]{0,60}:\**\s*(.+)", defect)
    if owed:
        expected = plain(owed.group(1))
    else:
        expected = re.sub(r"^open\b[^-]*-\s*", "", plain(state), flags=re.I)
    finding = Finding("row", show, "docs/fragment-issues/" + name, at, lead,
                      lead + ".", short(expected, 700))
    finding.cells = cells
    return finding


def tracked_in(text):
    m = re.search(r"\*\*Tracked as GitHub issue \[#(\d+)\]", text)
    return int(m.group(1)) if m else None


def find_group(root, letters, check):
    rel = "docs/needs-checking/group-%s.md" % letters.lower()
    path = os.path.join(root, *rel.split("/"))
    if not os.path.exists(path):
        raise Refused("%s is not there. A group written in full on docs/NEEDS-CHECKING.md is moved "
                      "into its own file first: python tools/split-needs-checking.py --write" % rel)
    text = read(path)
    lines = lines_of(text)
    heading = None
    for index, line in enumerate(lines):
        if re.match(r"^## Group %s\b" % re.escape(letters), line):
            heading = index
            break
    if heading is None:
        raise CouldNotRead("%s holds no '## Group %s' heading" % (rel, letters))
    title = re.sub(r"^## Group %s\s*[-:]?\s*" % re.escape(letters), "", lines[heading]).strip()
    title = plain(title) or "Group %s" % letters

    items, seen = [], set()
    bold = re.compile(r"(~~)?\*\*(%s[0-9]+[a-z]?)\*\*(~~)?" % re.escape(letters))
    for line in lines[heading:]:
        for m in bold.finditer(line):
            ident = m.group(2)
            if ident in seen:
                continue
            seen.add(ident)
            if m.group(1) and m.group(3):
                continue                      # struck at both ends: it held
            words = plain(bold.sub(r"\2", line).replace("|", " - "))
            words = re.sub(r"^[-\s]*%s\s*[-:]?\s*" % re.escape(ident), "", words)
            items.append((ident, short(words, 160)))

    if check:
        mine = [item for item in items if item[0] == check]
        if not mine:
            raise Refused("%s has no unstruck check %s - one that held is struck at both ends, and "
                          "a new one is written there first, its id in bold" % (rel, check))
        items = mine
        title = "%s - %s" % (check, short(mine[0][1], 90))
    elif not items:
        raise Refused("%s has no check left unstruck - nothing is owed" % rel)

    finding = Finding("check" if check else "group", check or "Group %s" % letters, rel,
                      heading + 1, title,
                      "Group %s: %s." % (letters, title.rstrip(".")) if not check
                      else "Check %s of Group %s is owed: %s" % (check, letters, mine[0][1]),
                      "Each check below passes as its line in the group's file says; a check that "
                      "fails stays, saying what failed.")
    finding.items = items
    finding.tracked = tracked_in(text)
    if not check and finding.tracked:
        raise Refused("Group %s is already tracked as GitHub issue #%d - its line is under its heading"
                      % (letters, finding.tracked))
    return finding


def find_question(root, qid):
    folder = os.path.join(root, "docs", "open-questions")
    if not os.path.isdir(folder):
        raise CouldNotRead("docs/open-questions/ is not there")
    # A TICK, A PRIORITY COLOUR OR NOTHING before the id: OPEN-QUESTIONS.md marks
    # an open question red, orange, yellow or blue, and an answered one may keep
    # its colour - its Answer paragraph says it is answered. Until 2026-11-21 only
    # the tick was read, so no colour-marked question could be found at all.
    heading = re.compile(r"^###\s+(?:(✅)\s*|(?:🔴|🟠|🟡|🔵)\s*)?%s\b(.*)$" % re.escape(qid))
    for name in sorted(os.listdir(folder)):
        if not name.endswith(".md"):
            continue
        lines = lines_of(read(os.path.join(folder, name)))
        for index, line in enumerate(lines):
            m = heading.match(line.rstrip("\r\n"))
            if not m:
                continue
            rel = "docs/open-questions/" + name
            end = index + 1
            while end < len(lines) and not re.match(r"^#{2,3} ", lines[end]):
                end += 1
            section = "".join(lines[index:end])
            if m.group(1) or "**Answer:**" in section:
                raise Refused("%s in %s is answered - nothing is owed" % (qid, rel))
            if tracked_in(section):
                raise Refused("%s is already tracked as GitHub issue #%d" % (qid, tracked_in(section)))
            title = re.sub(r"^\s*[—-]\s*", "", m.group(2))
            title = re.sub(r"\s*\*\([^)]*\)\*\s*$", "", title)
            title = plain(title) or qid
            return Finding("question", qid, rel, index + 1, title,
                           "An open question waits for you: %s" % title,
                           "Your answer, in your words, written under the question and recorded as a "
                           "decision where it is a rule.")
    raise Refused("%s is in no file of docs/open-questions/ - a new question goes at the end of its "
                  "tier's file, its id from tools/next-id.py" % qid)


# --------------------------------------------------------------------------
# The issue
# --------------------------------------------------------------------------

def part_of(finding, kind):
    if finding.what in ("group", "check"):
        return "NEEDS-CHECKING"
    if finding.what == "question":
        return "QUESTION OR PROPOSAL"
    return KINDS[kind][2]


def default_kind(finding):
    if finding.what in ("group", "check"):
        return "revit-run"
    if finding.what == "question":
        return "decision"
    return "fix"


def checklist(finding, repo):
    """The issue's checkbox lines for FINDING."""
    if finding.what == "row":
        return ["- [ ] **%s** - %s ([row](%s))" % (finding.show, short(finding.title, 140),
                                                    finding.link(repo))]
    if finding.what == "question":
        return ["- [ ] **%s** - %s ([question](%s))" % (finding.show, short(finding.title, 140),
                                                         finding.link(repo))]
    url = finding.link(repo, anchor=False)
    return ["- [ ] **%s** - %s ([group](%s))" % (ident, words, url) for ident, words in finding.items]


def reference(finding):
    """How the issue's title names FINDING: row 5b-1884, row 18, Group OQ, check OQ15, Q-94."""
    if finding.what == "row" and not finding.show.startswith("row"):
        return "row " + finding.show
    if finding.what == "check":
        return "check " + finding.show
    return finding.show


def issue_title(finding, kind, title):
    words = (title or finding.title).strip().rstrip(".")
    tail = "" if finding.show.lower() in words.lower() else " - " + reference(finding)
    room = TITLE_MAX - len(KINDS[kind][0]) - 2 - len(tail)
    return "%s: %s%s" % (KINDS[kind][0], short(words, max(room, 30)), tail)


def issue_body(finding, kind, repo, title, happened, expected, footer=True):
    word, _label, _part, kind_line, you_do, close = KINDS[kind]
    part = part_of(finding, kind)
    if finding.what in ("group", "check"):
        close = ("Every box is ticked: each result written into the group's file - release, model, "
                 "date - and a check that held struck at both ends.")
    elif finding.what == "question":
        close = ("The box is ticked: your answer is written under the question as its **Answer:** "
                 "paragraph, and recorded in DECISIONS.md where it is a rule.")
    heading = {"row": "Rows", "question": "Questions", "group": "Checks owed",
               "check": "Checks owed"}[finding.what]
    prompt = ('Read AGENTS.md and follow it. Then read docs/fragment-issues/ISSUES.md, the part '
              '"Working an issue", and do this GitHub issue in %s ("%s") the way it says for a %s '
              'issue. %s' % (repo, title.replace('"', "'"), part, PROMPT_TAIL[part]))
    parts = [
        "**What happened:** %s" % (happened or finding.happened).strip(),
        "**What is expected:** %s" % (expected or finding.expected).strip(),
        kind_line,
    ]
    if finding.what in ("group", "check"):
        parts.append("Each check's pass condition is in [`%s`](%s)."
                     % (finding.path, finding.link(repo, anchor=False)))
    parts += [
        "### %s - tick each one when it is done\n\n%s" % (heading, "\n".join(checklist(finding, repo))),
        "### What you do\n\n%s" % you_do,
        "### Ready prompt\n\n```text\n%s\n```" % prompt,
        "### Close this issue when\n\n%s" % close,
    ]
    if footer:
        parts.append(FOOTER)
    return "\n\n".join(parts) + "\n"


# --------------------------------------------------------------------------
# What is never sent
# --------------------------------------------------------------------------

PERSONAL = (
    ("an e-mail address", re.compile(r"[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+\.[A-Za-z0-9.-]*[A-Za-z]")),
    ("a Windows user's own folder",
     re.compile(r"\b[A-Za-z]:\\+Users\\+(?!<you>|<user>|%USERNAME%|Public\b|Default\b)[^\\\s<>%]+",
                re.I)),
    ("a Mac user's own folder", re.compile(r"(?<![\w.])/Users/(?!<you>|<user>|Shared\b)[^/\s<>]+")),
)


def private_shapes(text):
    """[(line number, shape)] for every credential or personal shape in TEXT."""
    found = []
    for number, line in enumerate(text.split("\n"), 1):
        for shape, pattern in tuple(SECRETS.PATTERNS) + PERSONAL:
            if pattern.search(line):
                found.append((number, shape))
    return found


def guard(text):
    hits = private_shapes(text)
    if hits:
        raise Refused("the issue would carry %s - nothing was sent. Take it out of the row or the "
                      "words given, then run this again"
                      % "; ".join("%s on line %d" % (shape, n) for n, shape in hits))


# --------------------------------------------------------------------------
# Writing the number back
# --------------------------------------------------------------------------

def issue_url(repo, number):
    return "https://github.com/%s/issues/%d" % (repo, number)


def written_back(root, finding, repo, number, date):
    """(path, new text) with issue NUMBER written into FINDING's register."""
    path = os.path.join(root, *finding.path.split("/"))
    text = read(path)
    eol = eol_of(text)
    lines = lines_of(text)
    at = finding.line - 1
    link = "[#%d](%s)" % (number, issue_url(repo, number))

    if finding.what == "row":
        line = lines[at]
        ending = line[len(line.rstrip("\r\n")):]
        cells = CELL.split(line.rstrip("\r\n"))
        if cells != finding.cells:
            raise Refused("row %s changed in %s since it was read - run this again"
                          % (finding.show, finding.path))
        was = cells[3].strip()
        cells[3] = " **MOVED TO ISSUE %s %s** - still open, tracked there. Was: %s " % (link, date, was)
        lines[at] = "|".join(cells) + ending
        return path, "".join(lines)

    if finding.what == "group" or (finding.what == "check" and finding.tracked is None):
        if finding.what == "group":
            words = ("**Tracked as GitHub issue %s since %s** - the checks still owed are listed "
                     "there; tick the group in that issue when they are done. Every result stays in "
                     "this file." % (link, date))
        else:
            words = ("Tracked as GitHub issue %s since %s: check %s - tick it there."
                     % (link, date, finding.show))
        insert = [eol, words + eol]
        if at + 1 < len(lines) and lines[at + 1].strip():
            insert.append(eol)
        lines[at + 1:at + 1] = insert
        return path, "".join(lines)

    if finding.what == "check":
        if finding.tracked == number:
            return path, None                   # the group's own line already names it
        after = at
        for index in range(at, len(lines)):
            if tracked_in(lines[index]) is not None:
                after = index
                break
        words = "Tracked as GitHub issue %s since %s: check %s - tick it there." % (
            link, date, finding.show)
        lines[after + 1:after + 1] = [eol, words + eol]
        return path, "".join(lines)

    # a question
    words = "**Tracked as GitHub issue %s since %s** - the owner answers there." % (link, date)
    insert = [eol, words + eol]
    if at + 1 < len(lines) and lines[at + 1].strip():
        insert.append(eol)
    lines[at + 1:at + 1] = insert
    return path, "".join(lines)


def record(root, finding, repo, number, date, out):
    path, text = written_back(root, finding, repo, number, date)
    if text is None:
        out("Nothing to write: %s's heading already names issue #%d.\n" % (finding.path, number))
        return
    write(path, text)
    out("Wrote issue #%d into %s (line %d). Stage that file with the change.\n"
        % (number, finding.path, finding.line))


# --------------------------------------------------------------------------
# GitHub, through gh's REST calls
# --------------------------------------------------------------------------

def gh_json(gh, root, args, payload=None):
    """gh api ARGS, its answer read as JSON; CouldNotReach when it is not."""
    scratch = None
    try:
        if payload is not None:
            handle, scratch = tempfile.mkstemp(prefix="heron-open-issue-", suffix=".json")
            with io.open(handle, "w", encoding="utf-8") as out:
                json.dump(payload, out)
            args = list(args) + ["--input", scratch]
        try:
            code, said, err = gh(["api"] + list(args), root)
        except OSError as exc:
            raise CouldNotReach("gh could not be run: %s" % exc)
    finally:
        if scratch and os.path.exists(scratch):
            os.remove(scratch)
    if code != 0:
        lines = [l for l in (err or said or "").splitlines() if l.strip()]
        raise CouldNotReach("gh api %s failed: %s" % (args[0], lines[0] if lines else "no message"))
    try:
        return json.loads(said)
    except ValueError:
        raise CouldNotReach("gh api %s answered something that is not JSON" % args[0])


def tracks(body, finding):
    """True when BODY tracks FINDING - its own checkbox line, or for a group its own heading.

    A passing mention in another entry's words does not count (row 5b-1962, 5b-1965): issue
    #1202 tracks Group PK and names Group PG inside check PK4, and that is not PG's issue.
    """
    ids = [finding.show] + [ident for ident, _words in finding.items]
    for ident in ids:
        box = r"^\s*- \[[ xX]\] \*{0,2}%s\*{0,2}(?![\w-])" % re.escape(ident)
        if re.search(box, body, re.I | re.M):
            return True
    if finding.what == "group":
        heading = r"^\s*#+ .*(?<![\w-])%s(?![\w-])" % re.escape(finding.show)
        return bool(re.search(heading, body, re.I | re.M))
    return False


def already_named(gh, root, repo, finding, out):
    """Open issues that already name FINDING's id - [] when none, or when the search could not run."""
    query = 'repo:%s is:issue is:open "%s"' % (repo, finding.show)
    try:
        found = gh_json(gh, root, ["-X", "GET", "search/issues", "-f", "q=" + query])
    except CouldNotReach as why:
        out("Could not search for an open issue already naming %s (%s) - look by hand before "
            "trusting this one is the only one.\n" % (finding.show, why))
        return []
    word = re.compile(r"(?<![\w-])%s(?![\w-])" % re.escape(finding.show), re.I)
    return [item["number"] for item in found.get("items", [])
            if word.search(item.get("title") or "") or tracks(item.get("body") or "", finding)]


def added_to(body, lines):
    """BODY with LINES after its last checkbox, or before its prompt when it has none."""
    rows = body.split("\n")
    last = None
    for index, line in enumerate(rows):
        if re.match(r"^\s*- \[[ xX]\] ", line):
            last = index
    if last is not None:
        at = last + 1
        while at < len(rows) and rows[at].startswith("  ") and rows[at].strip():
            at += 1
        return "\n".join(rows[:at] + lines + rows[at:])
    for index, line in enumerate(rows):
        if line.startswith("### Ready prompt") or line.startswith("### What you do") or line == "---":
            return "\n".join(rows[:index] + lines + [""] + rows[index:])
    return body.rstrip("\n") + "\n\n" + "\n".join(lines) + "\n"


# --------------------------------------------------------------------------
# The command
# --------------------------------------------------------------------------

def default_repo(root):
    try:
        done = subprocess.run(["git", "remote", "get-url", "origin"], cwd=root,
                              capture_output=True, timeout=30)
    except (OSError, subprocess.TimeoutExpired):
        return None
    if done.returncode != 0:
        return None
    return NI.github_repo(done.stdout.decode("utf-8", "replace"))


def parse(argv):
    p = argparse.ArgumentParser(
        prog="open-issue.py",
        description="Turn one register finding into a GitHub issue and write its number back.")
    p.add_argument("id", help="5b-N, 5-N, a NEEDS-CHECKING group (OQ) or check (OQ15), or Q-N")
    p.add_argument("--kind", choices=sorted(KINDS),
                   help="the issue's kind (default: fix for a row, revit-run for a check, "
                        "decision for a question)")
    p.add_argument("--title", help="the issue's title in plain words, without the kind word")
    p.add_argument("--happened", help="what happened, in plain words")
    p.add_argument("--expected", help="what is expected, in plain words")
    p.add_argument("--label", help="the label, when the kind's own is not the one")
    p.add_argument("--into", type=int, metavar="N", help="add it to open issue N instead of opening one")
    p.add_argument("--create", action="store_true",
                   help="send it - open the issue, or add it to --into - and write the number back")
    p.add_argument("--record", type=int, metavar="N",
                   help="the issue was opened another way: write #N back and send nothing")
    p.add_argument("--repo", help="OWNER/NAME (default: the origin remote's)")
    p.add_argument("--date", help="the date written back, YYYY-MM-DD (default: today)")
    p.add_argument("--no-footer", action="store_true", help="leave out the Generated-by footer")
    args = p.parse_args(argv)
    if args.record is not None and (args.create or args.into is not None):
        p.error("--record sends nothing, so it goes alone - not with --create or --into")
    if args.date and not DATE.match(args.date):
        p.error("--date is YYYY-MM-DD")
    for name in ("into", "record"):
        value = getattr(args, name)
        if value is not None and value < 1:
            p.error("--%s is an issue number" % name)
    return args


def main(argv=None, root=ROOT, gh=None, out=None):
    out = out or sys.stdout.write
    gh = gh or NI.run_gh
    args = parse(sys.argv[1:] if argv is None else argv)
    date = args.date or datetime.date.today().isoformat()
    repo = args.repo or default_repo(root)
    if not repo:
        out("Could not tell which GitHub repository this is - name it with --repo OWNER/NAME.\n")
        return 2

    try:
        finding = find(root, args.id)
        kind = args.kind or default_kind(finding)
        label = args.label or KINDS[kind][1]
        title = issue_title(finding, kind, args.title)

        if args.record is not None:
            record(root, finding, repo, args.record, date, out)
            return 0

        if args.into is not None:
            lines = checklist(finding, repo)
            if args.expected:
                lines.append("  - Owed: %s" % args.expected.strip())
            guard("\n".join(lines))
            out("%s, added to issue #%d:\n\n%s\n\n" % (finding.show, args.into, "\n".join(lines)))
            if not args.create:
                out("Nothing was sent and nothing was written. Add --create to send it.\n")
                return 0
            issue = gh_json(gh, root, ["repos/%s/issues/%d" % (repo, args.into)])
            if issue.get("pull_request"):
                raise Refused("#%d is a pull request, not an issue" % args.into)
            if issue.get("state") != "open":
                raise Refused("issue #%d is %s - a finding goes into an open issue, or a new one"
                              % (args.into, issue.get("state")))
            body = issue.get("body") or ""
            if tracks(body, finding):
                raise Refused("issue #%d already names %s - write the number back with --record %d"
                              % (args.into, finding.show, args.into))
            gh_json(gh, root, ["repos/%s/issues/%d" % (repo, args.into), "--method", "PATCH"],
                    {"body": added_to(body, lines)})
            out("Added %s to issue #%d: %s\n" % (finding.show, args.into, issue_url(repo, args.into)))
            number = args.into
        else:
            body = issue_body(finding, kind, repo, title, args.happened, args.expected,
                              footer=not args.no_footer)
            guard(title + "\n" + body)
            out("Title: %s\nLabel: %s\n\n%s\n" % (title, label, body))
            if not args.create:
                out("Nothing was sent and nothing was written. Add --create to open it.\n")
                return 0
            named = already_named(gh, root, repo, finding, out)
            if named:
                raise Refused("open issue %s already names %s - add to it with --into, or write its "
                              "number back with --record" % (", ".join("#%d" % n for n in named),
                                                              finding.show))
            made = gh_json(gh, root, ["repos/%s/issues" % repo, "--method", "POST"],
                           {"title": title, "body": body, "labels": [label]})
            number = made.get("number")
            if not isinstance(number, int):
                raise CouldNotReach("GitHub answered without an issue number - look on GitHub "
                                    "before running this again")
            out("Opened issue #%d: %s\n" % (number, made.get("html_url") or issue_url(repo, number)))

        try:
            record(root, finding, repo, number, date, out)
        except (Refused, CouldNotRead, OSError) as why:
            out("ISSUE #%d IS ON GITHUB, BUT ITS NUMBER WAS NOT WRITTEN BACK: %s\n"
                "  Write it back with: python tools/open-issue.py %s --record %d\n"
                % (number, why, args.id, number))
            return 1
        return 0
    except Refused as why:
        out("REFUSED: %s\n" % why)
        return 1
    except CouldNotRead as why:
        out("COULD NOT READ: %s\n" % why)
        return 2
    except CouldNotReach as why:
        out("NOT RUN - GitHub could not be reached: %s\n"
            "  Nothing was sent and nothing was written. List this issue under \"Issues to open\" in "
            "the pull request - its title, label and body are above, or print them with the same "
            "command and no --create - and the session that opens it writes the number back with:\n"
            "  python tools/open-issue.py %s --record N\n" % (why, args.id))
        return 3


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(errors="replace")
    except (AttributeError, ValueError):
        pass
    sys.exit(main())
