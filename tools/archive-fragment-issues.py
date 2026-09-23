# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Move the FINISHED rows of FRAGMENT-ISSUES.md sections 5 and 5b into an archive
folder, leaving each one a single line in the register.

    python tools/archive-fragment-issues.py            # what would move - writes nothing
    python tools/archive-fragment-issues.py --write    # move it

Exits 0 when the plan is clean or was written, 1 when a safety check refused
and NOTHING was written, 2 when the register could not be read at all.

WHY THIS EXISTS
---------------
On 2026-09-22 docs/FRAGMENT-ISSUES.md was 1,123,145 bytes - roughly 280,000
tokens, more than a session can hold at once - and sections 5 and 5b were 83%
of it. open-defects.py counted 324 rows in those two sections, 47 of them open.
So a session sent to "the queue" could not read the queue. It grepped, and
every grep came back with the paragraphs of finished rows beside the few that
are still owed.

Nobody did anything wrong. Every session appended an honest row and closed
rows in place, which is what the register asks. The missing step was moving a
finished row out of the way. HANDOVER.md had the same problem and got
docs/handover-archive/ on 2026-09-12; this is the same step for the defect
register, written as a command, because the habit is what fell behind.

WHAT MOVES, AND WHAT NEVER DOES
-------------------------------
A row moves only when ALL of these hold:

  - open-defects.py would not count it open. Its state does not begin with
    the word "open" - the register's own convention, read the same way;
  - its state BEGINS with a closing word - FIXED, CLOSED, CLEARED, SOLVED,
    REPAIRED, ANSWERED, WITHDRAWN, SUPERSEDED, NOT A DEFECT, GUARD FIXED;
  - the opening of that state does not qualify the claim - PARTLY,
    PARTIALLY, MOSTLY, EXCEPT, IN PART;
  - and NOWHERE in the state does it say something is still owed - STILL
    OPEN, NOT FIXED, NOT YET, AWAITING, OWNER'S CALL, REOPENED and the rest
    of STILL_OWED below.

THE LAST RULE READS THE WHOLE STATE, AND THAT WAS LEARNED, NOT ASSUMED. A row
here is closed IN PLACE, so what is left over is usually written at the END
of a long state - "FIXED 2026-09-19 ... WHAT IS NOT FIXED: the store still
goes stale". The first version of this tool read only the first 200
characters, and measured against the register on 2026-09-22 it would have
moved 59 rows carrying such words further down - among them row 150, which
says "It stays OPEN because the defect is still in the file", and two rows
saying NOT YET RUN IN REVIT. A word used in passing - "while Revit is open" -
now holds a row back too. That costs a few lines in the register; the
opposite mistake costs a row nobody can find.

AND THE LIST WAS TOO SHORT THE FIRST TIME IT WAS WIDENED. A second read of
what had moved, the same night, found "the real thing is owed on Windows"
(5b-79), "NOT RUN IN REVIT" (5b-11), "NOT proven against a model" (13) and
"what is still NOT verified is the case the row is about" (130) - all in the
archive, all still owed. So STILL_OWED now carries every form of it the
register was found to use. Words the same read found used only to DESCRIBE -
"what to do", "deferred", "follow-up" - are left out, and each was checked
against the rows before it was.

Everything else stays in the register IN FULL. A state that says PROVED, or
METHOD ESTABLISHED, or anything this list does not name, is left alone:
guessing that a row is finished is how a register loses a row it still owes.

WHAT A MOVED ROW LEAVES BEHIND
------------------------------
Its line, with the same number, so every citation of "row 107" or "row 5b-62"
still lands in the register:

    | **107** | its own bold title - [full row](fragment-issues-archive/...#row-107) | the opening of its state |

open-defects.py reads the same ids and the same open or closed answer from
that line, and review-ledger.py still finds every 5b row it cites. The full
text goes to docs/fragment-issues-archive/ UNCHANGED except for its links,
which are re-pointed one folder deeper - and each is checked, by path
arithmetic, to reach the same file it reached before.

WIDTH IS PART OF EVERY LINK THIS TOOL WRITES. Each archive file holds one
fixed band of row numbers - 1 to 25, 26 to 50 - so the file that holds a row
is known without an index. Changing WIDTH would move rows between files and
break every link already written, which is why it is a constant.

IT REFUSES RATHER THAN GUESSES. Before a byte is written it checks that every
rewritten row still has exactly three cells, that open-defects.py - run on the
rewritten text - reads the same rows with the same open or closed answer, that
every link still reaches its old target, and that the archive does not already
hold a row the register still carries in full. Any failure names the row, and
nothing is written.

RUN IT AGAIN WHENEVER ROWS CLOSE. A moved row is recognised by its link and
skipped, so a second run moves only what has closed since the first.

SINCE 2026-09-23 THE REGISTER IS ONE FILE PER SECTION. The page keeps each
section's heading; sections 5 and 5b keep their own words there too, and
their rows are in files of 25 under docs/fragment-issues/. This reads the
register as one text through tools/register-text.py, plans on that text
exactly as before, and writes each changed file back through
tools/split-register.py's layout_split() - once the files it would write have
been read back as the new register.

NO BACKSLASH IS TYPED IN THIS FILE. The one regular expression it needs, and
the escaped pipe a table cell needs, are built from chr(92): a typed backslash
can be turned into a control character on its way into a file, and a regular
expression changed that way still compiles.
"""

import argparse
import contextlib
import datetime
import importlib.util
import io
import os
import re
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REGISTER = os.path.join(ROOT, "docs", "FRAGMENT-ISSUES.md")
ARCHIVE_NAME = "fragment-issues-archive"
ARCHIVE = os.path.join(ROOT, "docs", ARCHIVE_NAME)

# Part of every link this tool writes - read the docstring before changing it.
WIDTH = 25

# How much of a row the register keeps. Enough to recognise the row and to
# carry its closing word and date; the rest is one click away.
TITLE_LIMIT = 100
LEAD_LIMIT = 90

BS = chr(92)
NL = chr(10)
CR = chr(13)
DASH = chr(0x2014)
MORE = chr(0x2026)

# check-docs.py's own link pattern, [text](target), so the links re-pointed
# here are exactly the links that checker follows.
LINK = re.compile(BS + "[([^" + BS + "]]*)" + BS + "]" + BS + "(([^)]+)" + BS + ")")

CLOSING = ("FIXED", "CLOSED", "CLEARED", "SOLVED", "REPAIRED", "ANSWERED",
           "WITHDRAWN", "SUPERSEDED", "NOT A DEFECT", "GUARD FIXED")
# Words that qualify the closing claim itself - read in the OPENING of a state.
HEDGES = ("PARTLY", "PARTIALLY", "MOSTLY", "EXCEPT", "IN PART")
HEDGE_WINDOW = 200

# Words that say something is STILL OWED - read in the WHOLE state, for the
# reason the docstring gives. Whole words, so OPENS and OPENING are not OPEN.
STILL_OWED = ("STILL OPEN", "STAYS OPEN", "REMAINS OPEN", "LEFT OPEN",
              "IS OPEN", "OPEN QUESTION", "OWED", "OWNER'S CALL",
              "NOT APPLIED", "NOT MERGED", "NOT FIXED", "NOT REPAIRED",
              "NOT YET", "NOT DONE", "NOT RUN", "NOT PROVEN", "NOT VERIFIED",
              "UNVERIFIED", "UNTESTED", "STILL NEEDS", "STILL NEED",
              "OUTSTANDING", "WAITING ON", "WAITING FOR", "AWAITING",
              "PENDING", "TODO", "NEEDS REAL REVIT", "NEEDS A REAL REVIT")

# A prefix rather than a word, so REOPENED, REOPENS and RE-OPENED all count.
REOPENED = ("REOPEN", "RE-OPEN")

# The register writes the owner's both ways; one of them is this character.
CURLY_APOSTROPHE = chr(0x2019)

# One archive file name per section, and the words a reader sees for it.
STEMS = {"5": "proving-defects", "5b": "reading-defects-5b"}
WHAT = {"5": "Heron's own defects found by proving",
        "5b": "Heron's own defects found by reading the repository"}
SHORT = {"5": "found by proving", "5b": "found by reading"}

OK, REFUSED, COULD_NOT = 0, 1, 2


def _load_open_defects():
    """The register's own reader, used as it is: its row pattern, its cell
    splitter, its section headings and its idea of an open row. Two tools
    that each decided those for themselves would sooner or later disagree
    about a row, and the one that moves rows must not be the one that is
    wrong."""
    path = os.path.join(ROOT, "tools", "open-defects.py")
    spec = importlib.util.spec_from_file_location("open_defects", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


OD = _load_open_defects()

# SINCE 2026-09-23 THE REGISTER IS ONE FILE PER SECTION, and the rows of
# sections 5 and 5b are in files of 25 under docs/fragment-issues/. It is
# read as one text - the page with every file read back into its place, by
# the same reader open-defects.py uses - and written back through the same
# layout, so everything below still plans on one text.
RT = OD.RT


def _split_tool():
    """tools/split-register.py, loaded when a split register is written and
    not before: it loads this file, through archive-handover.py, so loading
    it here at import would load the two of them round and round."""
    path = os.path.join(ROOT, "tools", "split-register.py")
    spec = importlib.util.spec_from_file_location("split_register", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


# ------------------------------------------------------------------ the rows

def state_text(cell):
    """A state cell exactly as open-defects.py reads it."""
    return cell.replace("`", "").replace("*", "").strip()


def is_open(cell):
    return state_text(cell).lower().startswith("open")


def _has_word(text, word):
    """WORD standing on its own - EXCEPTION is not EXCEPT."""
    at = text.find(word)
    while at >= 0:
        end = at + len(word)
        before = text[at - 1] if at else " "
        after = text[end] if end < len(text) else " "
        if not before.isalpha() and not after.isalpha():
            return True
        at = text.find(word, at + 1)
    return False


def held_because(cell):
    """None when a row may move; otherwise why it stays, in words."""
    if is_open(cell):
        return "open"
    upper = state_text(cell).replace(CURLY_APOSTROPHE, "'").upper()
    if not upper.startswith(CLOSING):
        return "its state does not begin with a closing word"
    head = upper[:HEDGE_WINDOW]
    for word in HEDGES:
        if _has_word(head, word):
            return "hedged - " + word
    for word in STILL_OWED:
        if _has_word(upper, word):
            return "says something is still owed - " + word
    for word in REOPENED:
        if word in upper:
            return "says " + word
    return None


# ----------------------------------------------------------------- the links

def _same(one, other):
    return (os.path.normcase(os.path.normpath(one))
            == os.path.normcase(os.path.normpath(other)))


def repoint(text, register, archive, ident, trouble):
    """TEXT with every local link re-pointed from the register's folder to
    the archive's - and each one PROVED to reach the file it reached before.
    A link that would not is added to TROUBLE, and the row is held back."""
    home = os.path.dirname(register)
    name = os.path.basename(register)

    def swap(match):
        label, href = match.group(1), match.group(2)
        if href.startswith(("http://", "https://", "mailto:")):
            return match.group(0)
        target, mark, fragment = href.partition("#")
        if target.startswith("/"):
            return match.group(0)
        if target.startswith("./"):
            target = target[2:]
        moved = "../" + (target or name)
        if not _same(os.path.join(home, target or name),
                     os.path.join(archive, moved)):
            trouble.append("the link (%s) would reach a different file from "
                           "the archive" % href)
        return "[" + label + "](" + moved + mark + fragment + ")"

    return LINK.sub(swap, text)


# ------------------------------------------------------- what a stub says

def _plain(text):
    """Links reduced to their words, stars gone, spaces folded."""
    text = LINK.sub(lambda match: match.group(1), text).replace("*", "")
    return " ".join(text.split())


def _clip(text, limit):
    if len(text) <= limit:
        return text
    cut = text[:limit]
    space = cut.rfind(" ")
    if space > limit // 2:
        cut = cut[:space]
    return cut.rstrip(" ,;:-") + MORE


def _ticks(text):
    """An odd number of code ticks opens a span that never closes."""
    return text.replace("`", "") if text.count("`") % 2 else text


def _pipes(text):
    """Every pipe escaped exactly once. A bare one would split the cell."""
    return text.replace(BS + "|", "|").replace("|", BS + "|")


def _bold_head(text):
    """The words of TEXT's leading **bold** span, or None."""
    text = text.strip()
    if not text.startswith("**"):
        return None
    end = text.find("**", 2)
    head = text[2:end] if end > 2 else ""
    return head if head.strip() else None


def title_of(defect):
    """The row's own headline - its opening bold span, or its first sentence."""
    head = _bold_head(defect)
    if head is None:
        stop = defect.find(". ")
        head = defect[:stop + 1] if stop > 0 else defect
    return _pipes(_ticks(_clip(_plain(head), TITLE_LIMIT)))


def lead_of(state):
    """The opening of the state - enough to carry its closing word and date."""
    head = _bold_head(state)
    if head is not None:
        inner = _plain(head)
        if inner and len(inner) <= LEAD_LIMIT:
            return "**" + _pipes(_ticks(inner)) + "**"
    stop = state.find(". ")
    first = state[:stop + 1] if stop > 0 else state
    return _pipes(_ticks(_clip(_plain(first), LEAD_LIMIT)))


# -------------------------------------------------------------- the register

class Plan(object):
    """Everything a run would do, worked out in memory before anything is
    written - so refusing costs nothing."""

    def __init__(self, register, archive, today):
        self.register = register
        self.archive = archive
        self.today = today
        self.fatal = None      # the register could not be read at all
        self.problems = []     # a check failed: nothing may be written
        self.held = []         # (ident, why) - rows that stay in full
        self.already = 0       # rows an earlier run moved
        self.rows = 0          # rows read in both sections
        self.moves = {}        # archive file name -> [(number, ident, block)]
        self.bands = {}        # archive file name -> (label, low, high)
        self.sizes = {}        # archive file name -> bytes it will hold once written
        self.eol = NL
        self.before = ""       # the register as one text
        self.after = ""
        self.parity = None     # [(label, number, open)] as open-defects reads it
        self.split = False     # the page names files, so it is written through them
        self.index_after = ""  # the page as written, when it is split
        self.files_after = {}  # each of its files as written, when it is split


def _read(path):
    with io.open(path, "rb") as handle:
        return handle.read().decode("utf-8")


def _split(text):
    """Lines without their endings, and the ending. A mixture is refused:
    rewriting it would change lines nobody meant to touch."""
    pieces = text.split(NL)
    crlf = sum(1 for piece in pieces[:-1] if piece.endswith(CR))
    lf = len(pieces) - 1 - crlf
    if crlf and lf:
        return None, None
    if crlf:
        return [p[:-1] if p.endswith(CR) else p for p in pieces], CR + NL
    return pieces, NL


def _spans(lines, text):
    """(label, first line, end line) for each section open-defects.py reads."""
    spans = []
    for label, start, ends in OD.SECTIONS:
        if label not in STEMS:
            return None, ("open-defects.py reads a section %s that this tool "
                          "has no archive file for - teach it one first" % label)
        seen = text.count(start)
        if seen != 1:
            return None, ("the heading %r appears %d times, so which one is "
                          "section %s would be a guess" % (start, seen, label))
        first = next((i for i, line in enumerate(lines)
                      if line.startswith(start)), None)
        if first is None:
            return None, ("section %s's heading is not at the start of a line"
                          % label)
        after = [i for i, line in enumerate(lines)
                 if i > first and any(line.startswith(e) for e in ends)]
        if not after:
            return None, "found section %s but nothing that ends it" % label
        spans.append((label, first, min(after)))
    return spans, None


def _one_row(p, lines, i, label, existing):
    line = lines[i]
    match = OD.ROW.match(line)
    if not match:
        return
    number = int(match.group(1))
    ident = str(number) if label == "5" else "5b-%d" % number
    p.rows += 1

    cells = OD.CELL.split(line)
    if len(cells) != 5:
        p.held.append((ident, "malformed - %d cells, not 3" % (len(cells) - 2)))
        return
    if "](" + ARCHIVE_NAME + "/" in cells[2]:
        p.already += 1
        return
    why = held_because(cells[3])
    if why:
        p.held.append((ident, why))
        return

    trouble = []
    defect = repoint(cells[2].strip(), p.register, p.archive, ident, trouble)
    state = repoint(cells[3].strip(), p.register, p.archive, ident, trouble)
    if trouble:
        p.held.append((ident, "; ".join(trouble)))
        return

    low = (number - 1) // WIDTH * WIDTH + 1
    name = "%s-%03d-%03d.md" % (STEMS[label], low, low + WIDTH - 1)
    heading = "### Row " + ident

    if name not in existing:
        path = os.path.join(p.archive, name)
        existing[name] = _read(path) if os.path.exists(path) else None
    if (existing[name] is not None
            and heading in existing[name].replace(CR, "").split(NL)):
        p.problems.append(
            "row %s is already in %s, but the register still carries it in "
            "full. One of the two was edited by hand - look at both before "
            "running this again." % (ident, name))
        return

    stub = "|".join([
        cells[0], cells[1],
        " %s %s [full row](%s/%s#row-%s) " % (title_of(cells[2]), DASH,
                                              ARCHIVE_NAME, name, ident),
        " %s " % lead_of(cells[3]),
        cells[4]])
    again = OD.ROW.match(stub)
    split = OD.CELL.split(stub)
    if (not again or int(again.group(1)) != number or len(split) != 5
            or is_open(split[3])
            or not state_text(split[3]).upper().startswith(CLOSING)):
        p.problems.append(
            "row %s: its one-line form would not read back as the same closed "
            "row. That is a fault in this tool, not in the register." % ident)
        return

    block = NL.join([heading, "",
                     "*Moved from the register on %s.*" % p.today, "",
                     "**Defect.** " + defect, "",
                     "**State.** " + state, "",
                     "---", ""])
    p.moves.setdefault(name, []).append((number, ident, block))
    p.bands[name] = (label, low, low + WIDTH - 1)
    lines[i] = stub


def _reading(register):
    """Every (section, id, open?) exactly as open-defects.py reads REGISTER."""
    saved = OD.REGISTER
    OD.REGISTER = register
    try:
        seen = []
        with contextlib.redirect_stdout(io.StringIO()):
            for label, start, ends in OD.SECTIONS:
                found = OD.rows(start, ends, label)
                if found is None:
                    return None
                seen.extend((label, number, state.lower().startswith("open"))
                            for number, state in found)
        return seen
    finally:
        OD.REGISTER = saved


def _prove_parity(p):
    """The rewritten register, read by open-defects.py itself, must give the
    same rows with the same open answer as the register gives now."""
    before = _reading(p.register)
    handle, scratch = tempfile.mkstemp(prefix=".fragment-issues-",
                                       suffix=".check",
                                       dir=os.path.dirname(p.register))
    try:
        with os.fdopen(handle, "wb") as out:
            out.write(p.after.encode("utf-8"))
        after = _reading(scratch)
    finally:
        os.remove(scratch)
    if before is None or after is None:
        p.problems.append("open-defects.py could not read the register before "
                          "or after the change, so nothing can be compared")
    elif before != after:
        changed = sorted(set(before) ^ set(after))
        p.problems.append(
            "open-defects.py would read the register differently afterwards: "
            + ", ".join("%s/%d open=%s" % one for one in changed[:10]))
    else:
        p.parity = before


def plan(register=REGISTER, archive=ARCHIVE, today=None):
    """Work out the whole move in memory. Nothing is written here."""
    p = Plan(register, archive, today or datetime.date.today().isoformat())
    try:
        page = _read(register)
        p.split = bool(RT.named_files(page, register))
        p.before = RT.register_text(register)
    except (IOError, OSError) as error:
        p.fatal = "could not read %s: %s" % (register, error)
        return p
    except RT.RegisterBroken as broken:
        p.fatal = str(broken)
        return p
    lines, p.eol = _split(p.before)
    if lines is None:
        p.fatal = ("the register mixes CRLF and LF line endings; rewriting it "
                   "would change lines nobody meant to touch")
        return p
    if p.eol.join(lines) != p.before:
        p.fatal = "the register does not survive being split into lines and rejoined"
        return p
    spans, why = _spans(lines, p.before)
    if spans is None:
        p.fatal = why
        return p

    existing = {}
    for label, first, end in spans:
        for i in range(first, end):
            _one_row(p, lines, i, label, existing)

    p.after = p.eol.join(lines)
    # MEASURED HERE, BEFORE ANYTHING IS WRITTEN. The first version measured
    # in the report, after a --write, and so added every moved row to a file
    # that already held it: the files were right and the report said each
    # was twice its size.
    for name in p.moves:
        path = os.path.join(p.archive, name)
        was = _read(path) if os.path.exists(path) else None
        p.sizes[name] = len(_archive_file(p, name, was).encode("utf-8"))
    if p.moves and not p.problems:
        _prove_parity(p)
    if p.moves and not p.problems and p.split:
        split = _split_tool()
        p.index_after, p.files_after, trouble = split.layout_split(register, p.after, p.today)
        p.problems.extend(trouble)
        back = RT.register_text(register, read=split._served(register, p.index_after, p.files_after))
        if back != p.after:
            p.problems.append("the register's files, written the way it is laid out, would not "
                              "read back as the new register")
    return p


# --------------------------------------------------------------- the archive

def _header(label, low, high):
    return NL.join([
        "# FRAGMENT-ISSUES archive %s section %s, rows %d to %d" % (DASH, label, low, high),
        "",
        "> **Finished rows, moved out of the live register.** These are rows from section %s of" % label,
        "> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) %s %s %s whose state said they were" % (DASH, WHAT[label], DASH),
        "> finished when they were moved. Each still has its line in the register: the same number, a",
        "> one-line title, the opening of its state, and a link to its full text here.",
        ">",
        "> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and",
        "> each checked to reach the same file it reached before. Where a row disagrees with",
        "> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the",
        "> [Constitution](../../HERON_CONSTITUTION.md), **those win** %s a row records what was true on" % DASH,
        "> the day it was written.",
        ">",
        "> **Nothing new is written here.** A new defect goes in the register, and a row here that turns",
        "> out not to be finished is reopened **there**, on its own line.",
        ">",
        "> Rows %d to %d of section %s belong in this file. A number in that band with no entry below" % (low, high, label),
        "> was never moved %s it is still in the register, in full. Written by" % DASH,
        "> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).",
        "",
        "---",
        "",
    ])


def _archive_file(p, name, was):
    label, low, high = p.bands[name]
    blocks = [block for _, _, block in sorted(p.moves[name])]
    if was is None:
        text = _header(label, low, high) + NL + NL.join(blocks)
    else:
        text = was.replace(CR + NL, NL).rstrip(NL) + NL + NL + NL.join(blocks)
    return text.replace(NL, p.eol)


def _band_of(name):
    for label, stem in STEMS.items():
        if name.startswith(stem + "-") and name.endswith(".md"):
            low, _, high = name[len(stem) + 1:-3].partition("-")
            if low.isdigit() and high.isdigit():
                return label, int(low), int(high)
    return None


def _readme(p):
    names = set(p.moves)
    if os.path.isdir(p.archive):
        names |= set(os.listdir(p.archive))
    table = []
    for name in sorted(names):
        band = _band_of(name)
        if band:
            label, low, high = band
            table.append("| %s %s %s | %d to %d | [`%s`](%s) |"
                         % (label, DASH, SHORT[label], low, high, name, name))
    text = NL.join([
        "# FRAGMENT-ISSUES archive",
        "",
        "**Finished rows from sections 5 and 5b of [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md). Nothing",
        "here is a live obligation, and nothing here is specification.**",
        "",
        "> This page is written by [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py)",
        "> every time it moves rows. **Change the tool, not this page** %s an edit here is gone on the next" % DASH,
        "> run.",
        "",
        "## Why this folder exists",
        "",
        "On 2026-09-22 the register was 1,123,145 bytes " + DASH + " more than a session can hold at once " + DASH,
        "and sections 5 and 5b, one paragraph per defect, were 83% of it. Most of those rows were",
        "finished. A session sent to the queue could not read the queue, so it grepped, and every grep",
        "returned the finished rows beside the ones still owed. Finished rows now live here, and the",
        "register keeps one line for each.",
        "",
        "`HANDOVER.md` met the same problem first, and [`../handover-archive/`](../handover-archive/README.md)",
        "is the same answer for session notes.",
        "",
        "## Finding a row",
        "",
        "**Start at the register.** Every moved row kept its line there, with the same number and a link",
        "straight to its full text here.",
        "",
        "A row cited as **row 107** is in section 5, and one cited as **row 5b-62** is in section 5b. Each",
        "file holds one fixed band of %d row numbers and is named after it, so the file is known before it" % WIDTH,
        "is opened:",
        "",
        "| Section | Rows | File |",
        "|---|---|---|",
    ] + table + [
        "",
        "A number with no entry in its file was never moved: it is still in the register, in full.",
        "",
        "How many rows each file holds is derived, not typed:",
        "",
        "```bash",
        "grep -c '^### Row' docs/fragment-issues-archive/*.md",
        "```",
        "",
        "## What moves, and what stays",
        "",
        "A row moves only when its state begins with a closing word " + DASH + " " + ", ".join(CLOSING) + " " + DASH,
        "with nothing at its start that qualifies the claim, such as " + ", ".join(HEDGES[:3]) + ", and nothing",
        "anywhere in it that says something is still owed, such as STILL OPEN, NOT FIXED, NOT YET, AWAITING,",
        "OWNER'S CALL or REOPENED. The whole state is read for those, because a row closed in place usually",
        "writes what is left at the end. An open row never moves, and neither does a row whose state the rule",
        "does not recognise: guessing that a row is finished is how a register loses a row it still owes. The",
        "full rule, and the checks made before anything is written, are in the tool's own docstring.",
        "",
        "To move the rows that have closed since:",
        "",
        "```bash",
        "python tools/archive-fragment-issues.py            # what would move - writes nothing",
        "python tools/archive-fragment-issues.py --write",
        "```",
        "",
    ])
    return text.replace(NL, p.eol)


def _put(path, text):
    """Write TEXT whole or not at all."""
    scratch = path + ".writing"
    with io.open(scratch, "wb") as out:
        out.write(text.encode("utf-8"))
    os.replace(scratch, path)


def write(p):
    """Write the plan: archive files first, the register LAST - so a run cut
    short leaves the register whole, and the next run refuses and says why."""
    if p.fatal:
        return COULD_NOT
    if p.problems:
        return REFUSED
    if not p.moves:
        return OK
    if not os.path.isdir(p.archive):
        os.makedirs(p.archive)
    for name in sorted(p.moves):
        path = os.path.join(p.archive, name)
        was = _read(path) if os.path.exists(path) else None
        _put(path, _archive_file(p, name, was))
    _put(os.path.join(p.archive, "README.md"), _readme(p))
    if not p.split:
        _put(p.register, p.after)
        return OK
    folder = os.path.join(os.path.dirname(p.register), RT.folder_of(p.register))
    if not os.path.isdir(folder):
        os.makedirs(folder)
    for name in sorted(p.files_after):
        path = os.path.join(folder, name)
        if not os.path.exists(path) or _read(path) != p.files_after[name]:
            _put(path, p.files_after[name])
    _put(p.register, p.index_after)
    return OK


# -------------------------------------------------------------------- report

def report(p, writing):
    print("FRAGMENT-ISSUES.md - sections 5 and 5b")
    print("=" * 38)
    print("")
    if p.fatal:
        print("  COULD NOT RUN: %s" % p.fatal)
        return
    moving = sum(len(rows) for rows in p.moves.values())
    opened = sum(1 for _, why in p.held if why == "open")
    print("  rows read           : %d" % p.rows)
    print("  %-20s: %d, into %d file(s)"
          % ("moved" if writing and not p.problems else "to move",
             moving, len(p.moves)))
    print("  moved by a past run : %d" % p.already)
    print("  held back           : %d  (%d open, %d for another reason)"
          % (len(p.held), opened, len(p.held) - opened))
    before = len(p.before.encode("utf-8"))
    after = len(p.after.encode("utf-8"))
    if before:
        print("  register            : %s -> %s bytes (%d%% of what it was)"
              % (format(before, ","), format(after, ","), after * 100 // before))

    if p.moves:
        print("")
        print("  archive files")
        for name in sorted(p.moves):
            print("    %-36s %4d row(s)  %s bytes"
                  % (name, len(p.moves[name]), format(p.sizes[name], ",")))

    other = [(ident, why) for ident, why in p.held if why != "open"]
    if other:
        print("")
        print("  held back for a reason other than being open - these stay in full")
        for ident, why in other:
            print("    %-7s %s" % (ident, why[:100]))

    if p.problems:
        print("")
        print("  REFUSED - nothing was written:")
        for problem in p.problems:
            print("    - %s" % problem)
        return

    if p.parity is not None:
        print("")
        print("  open-defects.py reads the rewritten register the same way: %d row(s), %d open."
              % (len(p.parity), sum(1 for one in p.parity if one[2])))
    print("")
    if not p.moves:
        print("Nothing to move.")
    elif writing:
        print("Written. Check it: python tools/open-defects.py")
    else:
        print("Nothing was written. Run again with --write to move them.")


def main(argv=None):
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")
    parser = argparse.ArgumentParser(
        description="Move the finished rows of FRAGMENT-ISSUES.md sections 5 "
                    "and 5b into docs/fragment-issues-archive/.")
    parser.add_argument("--write", action="store_true",
                        help="move them - without this, nothing is written")
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
