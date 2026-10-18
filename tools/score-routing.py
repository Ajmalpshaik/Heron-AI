#!/usr/bin/env python3
# Heron-Agent:  HERON-RAG-RNK-006
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
How does Heron's search do on the questions the owner ACTUALLY asked?

    python tools/score-routing.py               # score, and say what moved
    python tools/score-routing.py --record      # ...and append the row to
                                                #    brain/retrieval-history.md
    python tools/score-routing.py --revit 2024  # behind one release's wall
    python tools/score-routing.py --list        # the answer key; asks nothing
    python tools/score-routing.py --list-plain  # the plain questions; asks nothing

    # A SESSION'S OWN SENTENCES - main against a branch, on a named backend
    python tools/score-routing.py --sentences s.yaml --tree ../main-wt \\
        --backend lexical --backend model+rerank --shortlists --out main.json
    python tools/score-routing.py --sentences s.yaml \\
        --backend lexical --backend model+rerank --compare main.json
    python tools/score-routing.py --identities ids.txt --card RENAME_ELEMENTS \\
        --from main.json                        # re-score identities offline

WHAT IS DIFFERENT ABOUT THIS ONE
--------------------------------
Every routing number Heron had was measured on sentences written FOR the
library. `check-routing.py` asks each fragment its own declared words back,
and brain/retrieval-history.md calls that a near-circular lower bound, which it
is. `check-risk-crossings.py` asks ordinary sentences, but written by a session
and scored only on whether a write won. Neither was ever asked the way the
owner asks.

tests/data/owner-questions.yaml is his: 79 questions from real work, each with
the capability, skill or tool that should answer it - chosen from Heron's
capability list before the search had seen any of them, and confirmed by him.
This asks every one through `heron_brain.lookup`, THE SAME FUNCTION
`heron_lookup` CALLS, and says where the answer came back.

THE FOUR PLACES, KEPT APART
---------------------------
For a capability row, the place is where the answer sits in the shortlist
`heron_lookup` shows - the winner, then the other capabilities that came
close, each once:

    1st          the host is told this first
    2nd-3rd      in sight, and in the top three
    4th-5th      found, but low - shown, and easy to pass over
    not found    not in anything the host was shown

**A SKILL IS NEVER INDEXED** (`tools/check-skill-routing.py` says why), so the
best the search can do for a skill row is name one of the steps the skill
declares in `needs`. Those rows are scored the way that tool scores them -
and printed APART from the capability rows, because a skill's first step is
nearly always FILTER_ELEMENTS_BY_CATEGORY and counting that as a hit in the
headline would flatter it. A TOOL row and a GAP row have no capability to find
at all; for them only where the search sent them is read.

**AN EXACT DECLARED PHRASE IS COUNTED SEPARATELY.** `heron_retrieve.find`
answers a sentence some fragment declares word for word by `identity`, before
any ranking runs. That tests a declaration, not the search, so the count says
how many first places came that way.

AND THE ONE ANSWER THAT IS NOT A JUDGEMENT CALL (D-86)
------------------------------------------------------
A question the owner ASKED that the search answers with something that
CHANGES THE MODEL. The write line is read from the operation registry through
`generate-jobs.write_threshold()` - Golden Rule 19, never typed here - and a
row counts when the answer is at or above it while the answer key's own
answer is not. A change asked for and a DIFFERENT change given is listed on
its own, and so is a gap row that landed on a change.

WHAT IT WILL NOT DO
-------------------
It never changes the answer key, a question or an utterance to win a row -
FRAGMENT-ISSUES row 113's forbidden move, and D-34's. A row that looks wrong
in the key is the owner's to correct, in the key, with the day he said so.

It keeps 79 synthetic lookups OUT of the owner's audit trail. `lookup` writes
one line per call, and that trail is read as a record of real asking (D-62) -
so HERON_AUDIT, the documented override, points at a throwaway folder for the
length of the run. Nothing about the answer changes.

It reports and exits 0 whatever it finds, like every routing check here, and
that is the owner's decision rather than a default: on 2026-09-23 he decided
that a drop in this score is reported and never stops a pull request (D-100).
Exit 2 means it could not do its job: nothing was scored, which is not a pass.

AND A SECOND SET BESIDE HIS, NEVER INSIDE IT
--------------------------------------------
tests/data/plain-questions.yaml holds plain questions frozen out of
FRAGMENT-ISSUES rows, where they lived only as row text: the 120 a session
wrote on 2026-10-14 before reading any card (rows 5b-477 and 5b-478, frozen on
2026-10-17), and the four of row 5b-1005 the owner asked to join them.
They are asked after his, through the same lookup, and get their own report,
their own table in brain/retrieval-history.md and their own comparison.
Every one is a question, so the headline is how many reach a write; where the
row names the read that answers one, where that read came back is scored too.
A drop there is reported and never a gate either (D-100).

AND A SESSION'S OWN SENTENCES, WHICH NEVER REACH EITHER TABLE
-------------------------------------------------------------
Rows 5b-481, 5b-485 and 5b-510 each rebuilt the same scratch kit on the
owner's PC to measure a card's words: a probe asking a frozen sentence set on
a private store built from one checkout, a check that every answer ran on the
backend asked for, a comparer of main against the branch, and a fast loop
re-scoring candidate identities on the shortlists the re-ranker read. It is
`--sentences` now. Each backend is asked in a process of its own, with the
measured tree's search code and a store built from its cards, and the answers
go to a record (`--out`) - never to brain/retrieval-history.md, which holds
the two frozen sets above and nothing else. See "A session's own sentences"
below.

A NUMBER FROM HERE IS A SAMPLE OF ONE STORE
-------------------------------------------
The knowledge store is one file every worktree on a machine writes (row 116),
so the index fingerprint is printed beside the numbers, and a run is compared
only with an earlier run of THE SAME ANSWER KEY ON THE SAME BACKEND. The
library's size is allowed to differ between the two - a new fragment taking
one of his questions is exactly the drop this exists to catch.
"""

import argparse
import datetime
import hashlib
import importlib.util
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "brain"))

QUESTIONS = os.path.join(ROOT, "tests", "data", "owner-questions.yaml")
HISTORY = os.path.join(ROOT, "brain", "retrieval-history.md")
SKILLS_DIR = os.path.join(ROOT, "brain", "skills")

# His questions carry an em dash, and a redirected Windows console encodes in
# the ANSI code page and dies on one - heron-ship section 2 has the story.
for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

KINDS = ("capability", "skill", "tool", "gap")
# The search can PLACE these. It cannot place a tool or a gap - there is no
# capability to find - so for those only where it landed is read.
PLACED = ("capability", "skill")

# THE TABLE THIS APPENDS TO, found by its header line. brain/retrieval-history.md
# is the one place a retrieval number may be quoted from, and a row there with
# no library size and no backend is what that file exists to prevent.
COLUMNS = ["Date", "Fragments", "Backend", "Revit", "Answer key",
           "1st", "2nd-3rd", "4th-5th", "Not found", "Exact (right)",
           "Handed a change", "Skill rows, a step in the top 3",
           "Per question"]
TABLE_HEADER = "| " + " | ".join(COLUMNS) + " |"

# One character per question, in the answer key's order. A row of the table
# carries all 79, so the next run can say WHICH question moved, not only that
# a total did.
NOT_FOUND = "-"        # a placed row missing from the shortlist
ERROR = "?"            # the row could not be scored
LANDED_CHANGE = "w"    # a tool or gap row that landed on a change
LANDED_OTHER = "."     # a tool or gap row that landed on anything else
LANDED_NOTHING = "0"   # the search matched nothing at all

# THE SECOND SET, SCORED BESIDE HIS AND NEVER MIXED WITH IT. Plain
# questions frozen out of FRAGMENT-ISSUES rows - 5b-477 and 5b-478's 120,
# which a session wrote before reading any card, and 5b-1005's four - where
# they had lived only as row text no tool could read. Every one is a
# question, so its headline is not a judgement call: how many reach
# something at or above the write line. Where
# the row names the read that answers one, where that read came back is
# scored too. Its own table, its own key fingerprint, its own comparison.
PLAIN = os.path.join(ROOT, "tests", "data", "plain-questions.yaml")
PLAIN_NAMED = ("declared", "ranked", "owners-call", "no-read")
PLAIN_COLUMNS = ["Date", "Fragments", "Backend", "Revit", "Answer key",
                 "Asked", "Reached a write", "Exact", "Named read 1st",
                 "Named read in the top 3", "Per question"]
PLAIN_HEADER = "| " + " | ".join(PLAIN_COLUMNS) + " |"
# One character per plain question. A row with a named read keeps its place,
# `1`-`5`, or NOT_FOUND; a row with none keeps LANDED_OTHER. Either becomes
# LANDED_CHANGE when the answer writes, which outranks where the read sat:
# the question was answered by a change, whatever came second.
NAMED, UNNAMED = "named", "unnamed"


def _sibling(name):
    """A tool beside this one, imported by path - the filenames have hyphens.

    IMPORTED, NOT COPIED. The store's drift check, the index fingerprint and
    the write line each already have one home, and a second copy of any of
    them is a second opinion that starts disagreeing the day the first learns
    something.
    """
    path = os.path.join(ROOT, "tools", name)
    spec = importlib.util.spec_from_file_location(
        "heron_tool_" + name.replace("-", "_").replace(".py", ""), path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


# ---------------------------------------------------------------------------
# The answer key
# ---------------------------------------------------------------------------

def load_key(path=QUESTIONS):
    """(doc, rows, problems) - the answer key, and what is wrong with its SHAPE.

    Shape only: whether a named capability EXISTS is a question for the
    library, and `unknown_answers` asks it once the library is open.
    """
    import yaml

    with io.open(path, encoding="utf-8") as handle:
        doc = yaml.safe_load(handle) or {}
    rows = doc.get("questions") or []
    problems, seen = [], set()
    if not rows:
        problems.append("%s holds no questions" % os.path.relpath(path, ROOT))
    for i, row in enumerate(rows, 1):
        if not isinstance(row, dict):
            problems.append("row %d is not a mapping of n, question, kind "
                            "and answer" % i)
            continue
        n, kind = row.get("n"), row.get("kind")
        question = " ".join(str(row.get("question") or "").split())
        answer = row.get("answer")
        if n != i:
            problems.append("row %d is numbered %r - they run 1, 2, 3 in "
                            "order, because the per-question record is by "
                            "position" % (i, n))
        if not question:
            problems.append("#%s has no question" % n)
        elif question.lower() in seen:
            problems.append("#%s repeats an earlier question" % n)
        seen.add(question.lower())
        if kind not in KINDS:
            problems.append("#%s has kind %r - one of %s"
                            % (n, kind, ", ".join(KINDS)))
        elif kind == "gap" and answer:
            problems.append("#%s is a gap and names an answer (%s) - what "
                            "exists belongs in its note" % (n, answer))
        elif kind != "gap" and not answer:
            problems.append("#%s is a %s with no answer" % (n, kind))
    return doc, rows, problems


def key_fingerprint(rows):
    """Eight hex characters naming THIS answer key - questions and answers.

    Two runs are comparable only if they asked the same questions and scored
    them against the same answers. Any change - a question, an answer, the
    order - moves this, and the comparison then says so instead of reading a
    corrected answer as the search getting better or worse.
    """
    canon = [[row.get("n"), " ".join(str(row.get("question") or "").split()),
              row.get("kind"), row.get("answer") or ""] for row in rows]
    blob = json.dumps(canon, ensure_ascii=False, separators=(",", ":"))
    return hashlib.sha256(blob.encode("utf-8")).hexdigest()[:8]


def load_skills(folder=SKILLS_DIR):
    """{skill id: {"needs": [...], "risk": ...}} read from the skill files."""
    import yaml

    out = {}
    for name in sorted(os.listdir(folder)):
        if not name.endswith((".yaml", ".yml")):
            continue
        with io.open(os.path.join(folder, name), encoding="utf-8") as handle:
            doc = yaml.safe_load(handle) or {}
        if doc.get("id"):
            out[doc["id"]] = {"needs": list(doc.get("needs") or []),
                              "risk": doc.get("risk")}
    return out


def unknown_answers(rows, capabilities, skills, tools):
    """[(n, why)] - an answer naming something Heron does not have.

    A renamed or retired capability makes a row unanswerable, and scoring it
    "not found" would report a library change as a routing failure. So it is
    named, the row is scored `?`, and nothing is recorded until the owner has
    said what the new answer is.
    """
    out = []
    for row in rows:
        kind, answer = row.get("kind"), row.get("answer")
        if kind == "capability" and answer not in capabilities:
            out.append((row.get("n"), "no fragment in this library provides "
                                      "the capability %s" % answer))
        elif kind == "skill" and answer not in skills:
            out.append((row.get("n"), "there is no skill %s in brain/skills"
                                      % answer))
        elif kind == "tool" and answer not in tools:
            out.append((row.get("n"), "there is no tool %s in "
                                      "mcp/server/heron_tools.py" % answer))
    return out


# ---------------------------------------------------------------------------
# One question
# ---------------------------------------------------------------------------

def shortlist(found):
    """The capabilities `heron_lookup` shows, in its order, each once.

    The winner, then "Other capabilities that came close" - the server prints
    each capability once however many fragments provide it, so a place here
    is a place a person would count on the screen.
    """
    order = []
    if found.get("capability"):
        order.append(found["capability"])
    for one in found.get("candidates") or []:
        capability = one.get("capability")
        if capability and capability not in order:
            order.append(capability)
    return order


def place(order, wanted):
    """(1-based place, which) of the first of `wanted` in `order`, or (None, None)."""
    for i, capability in enumerate(order, 1):
        if capability in wanted:
            return i, capability
    return None, None


def score_one(row, found, wanted, expected_risk, rung, threshold):
    """What one answer means for one row of the key. Never raises.

    `wanted` is the set a hit may name - the capability, or a skill's declared
    steps. `rung` maps a risk to its place on the ladder and `threshold` is the
    rung at which a fragment changes the model.
    """
    order = shortlist(found)
    got = found.get("capability")
    got_risk = (found.get("risk") or "").upper() or None
    at, reached = place(order, wanted) if row["kind"] in PLACED else (None, None)
    writes = got_risk is not None and rung(got_risk) >= threshold
    if expected_risk is None:
        asked_change = None                   # a gap: nothing to compare with
    else:
        asked_change = rung(expected_risk) >= threshold
    return {"n": row["n"], "kind": row["kind"], "answer": row.get("answer"),
            "question": " ".join(str(row["question"]).split()),
            "route": found.get("route"), "got": got, "got_risk": got_risk,
            "order": order, "place": at, "reached": reached,
            "writes": writes, "expected_risk": expected_risk,
            "asked_change": asked_change, "error": None}


def code_of(result):
    """The one character the history table keeps for this row."""
    if result.get("error"):
        return ERROR
    if result["kind"] in PLACED:
        return str(result["place"]) if result["place"] else NOT_FOUND
    if not result.get("got"):
        return LANDED_NOTHING
    return LANDED_CHANGE if result["writes"] else LANDED_OTHER


def handed_a_change(result):
    """D-86's harm: the answer key's answer changes nothing in the model, and
    the search's answer does. A question is the plainest case; "isolate these"
    answered by a permanent hide is the same harm wearing an imperative."""
    return (not result.get("error") and result["writes"]
            and result["asked_change"] is False)


def wrong_change(result):
    """A change asked for, and a DIFFERENT change given."""
    return (not result.get("error") and result["writes"]
            and result["asked_change"] is True and result["place"] != 1)


def gap_onto_a_change(result):
    """A job nothing in Heron does, answered by something that writes anyway."""
    return (not result.get("error") and result["kind"] == "gap"
            and result["writes"])


# ---------------------------------------------------------------------------
# The whole run, summarised - and the row it becomes
# ---------------------------------------------------------------------------

def summarise(results):
    """The counts the table keeps. Capability rows only in the four places."""
    caps = [r for r in results if r["kind"] == "capability" and not r["error"]]
    skills = [r for r in results if r["kind"] == "skill" and not r["error"]]

    def at(rows, low, high):
        return sum(1 for r in rows if r["place"] and low <= r["place"] <= high)

    exact = [r for r in results if not r["error"] and r["route"] == "identity"]
    return {
        "capability_rows": len(caps),
        "first": at(caps, 1, 1),
        "second_third": at(caps, 2, 3),
        "low": sum(1 for r in caps if r["place"] and r["place"] >= 4),
        "not_found": sum(1 for r in caps if not r["place"]),
        "exact": len(exact),
        "exact_right": sum(1 for r in exact if r["place"] == 1),
        "handed": sum(1 for r in results if handed_a_change(r)),
        "skill_rows": len(skills),
        "skill_top3": at(skills, 1, 3),
        "errors": sum(1 for r in results if r["error"]),
    }


def history_row(stamp, summary, codes):
    """One line of the table in brain/retrieval-history.md."""
    cells = [stamp["date"], str(stamp["fragments"]), "`%s`" % stamp["backend"],
             stamp["revit"] or "any", "`%s`" % stamp["key"],
             str(summary["first"]), str(summary["second_third"]),
             str(summary["low"]), str(summary["not_found"]),
             "%d (%d)" % (summary["exact"], summary["exact_right"]),
             str(summary["handed"]),
             "%d of %d" % (summary["skill_top3"], summary["skill_rows"]),
             "`%s`" % codes]
    return "| " + " | ".join(cells) + " |"


def history_rows(text, header=TABLE_HEADER, columns=COLUMNS):
    """Every row of the table, oldest first, as dicts - or None if no table.

    None and [] are different answers and are kept apart (D-52): no table
    means the file lost its section, an empty table means nothing was ever
    recorded. `header` and `columns` name the plain questions' table instead.
    """
    lines = text.splitlines()
    if header not in lines:
        return None
    rows = []
    for line in lines[lines.index(header) + 2:]:
        if not line.startswith("|"):
            break
        cells = [c.strip().strip("`") for c in line.strip().strip("|").split("|")]
        if len(cells) == len(columns):
            rows.append(dict(zip(columns, cells)))
    return rows


def last_comparable(rows, stamp):
    """The latest row with the same answer key, backend and Revit filter."""
    for row in reversed(rows or []):
        if (row["Answer key"] == stamp["key"]
                and row["Backend"] == stamp["backend"]
                and row["Revit"] == (stamp["revit"] or "any")):
            return row
    return None


def _severity(kind, char):
    """How bad one character is for its kind of row; larger is worse."""
    if kind in PLACED:
        if char == NOT_FOUND:
            return 99
        return int(char) if char.isdigit() else None
    if kind == NAMED:
        # A plain question whose row names its read. Answered by a change is
        # the worst; the read missing, or nothing at all, next; then its place.
        if char == LANDED_CHANGE:
            return 100
        if char in (NOT_FOUND, LANDED_NOTHING):
            return 99
        return int(char) if char.isdigit() else None
    # A character this tool never writes - `?`, or a row somebody edited by
    # hand - is UNREADABLE. Reading it as "no change" would call a hole in the
    # record an improvement. An UNNAMED plain question reads like a tool row:
    # a change, or not.
    return {LANDED_CHANGE: 1, LANDED_OTHER: 0, LANDED_NOTHING: 0}.get(char)


def moved(before, after, kinds):
    """[(index, was, now, 'down'|'up'|'unreadable')] between two code strings.

    `kinds` is the answer key's kind per question, in the same order - the
    strings are only comparable because the key that produced both is the
    same, which `last_comparable` already made sure of.
    """
    out = []
    for i, (was, now) in enumerate(zip(before, after)):
        if was == now:
            continue
        a, b = _severity(kinds[i], was), _severity(kinds[i], now)
        if a is None or b is None:
            out.append((i, was, now, "unreadable"))
        elif b > a:
            out.append((i, was, now, "down"))
        elif b < a:
            out.append((i, was, now, "up"))
    return out


def dropped(movements, before_row, summary):
    """Did the score drop? Any question moving down, or more questions
    handed a change than last time."""
    if any(m[3] == "down" for m in movements):
        return True
    try:
        return summary["handed"] > int(before_row["Handed a change"])
    except (KeyError, TypeError, ValueError):
        return False


def exit_code(drop):
    """0 whether or not the score dropped - the owner's decision, D-100.

    On 2026-09-23 he decided that a drop in this score is REPORTED and never
    stops a pull request, the same as every routing check here: a gate on a
    routing number teaches people to buy the number back by rewording his
    questions. `drop` is taken, and ignored, so the decision is written where
    the exit code is made - changing this is changing his decision, and needs
    a new one that supersedes D-100.
    """
    return 0


def append_row(path, line, header=TABLE_HEADER):
    """Add one row at the end of the table. False if the table is not there.

    THE FILE KEEPS ITS OWN LINE ENDINGS. A Windows checkout usually holds CRLF.
    Read in universal-newline mode and written back with LF, one appended row
    rewrote every line ending in the file - a whole-file change on the owner's
    PC to record one run. Written back the way it came.
    """
    with io.open(path, encoding="utf-8", newline="") as handle:
        raw = handle.read()
    eol = "\r\n" if "\r\n" in raw else "\n"
    lines = raw.replace("\r\n", "\n").split("\n")
    if header not in lines:
        return False
    at = lines.index(header) + 2
    while at < len(lines) and lines[at].startswith("|"):
        at += 1
    lines.insert(at, line)
    with io.open(path, "w", encoding="utf-8", newline="") as handle:
        handle.write(eol.join(lines))
    return True


# ---------------------------------------------------------------------------
# Words
# ---------------------------------------------------------------------------

def _place_word(at):
    return {1: "1st", 2: "2nd", 3: "3rd"}.get(at, "%dth" % at if at else "-")


def _short(text, width=52):
    return text if len(text) <= width else text[:width - 3] + "..."


def _route_word(route):
    return {"identity": "exact phrase", "cache": "remembered",
            "hybrid": "ranked"}.get(route, route or "-")


class _AuditElsewhere(object):
    """HERON_AUDIT pointed at a throwaway folder for the length of the run.

    `heron_brain.lookup` writes one trail line per call, and heron_gaps and
    measure-routes read that trail as a record of what somebody really asked
    (D-62). Seventy-nine lines of a measurement in it would be read as use.
    HERON_AUDIT is the override heron_gaps.audit_dir() documents for exactly
    this; nothing about the answer depends on where its trail line goes.
    """

    def __enter__(self):
        self.before = os.environ.get("HERON_AUDIT")
        self.folder = tempfile.mkdtemp(prefix="heron-score-audit-")
        os.environ["HERON_AUDIT"] = self.folder
        return self.folder

    def __exit__(self, *_exc):
        if self.before is None:
            os.environ.pop("HERON_AUDIT", None)
        else:
            os.environ["HERON_AUDIT"] = self.before
        shutil.rmtree(self.folder, ignore_errors=True)
        return False


# ---------------------------------------------------------------------------
# The run
# ---------------------------------------------------------------------------

def measure(rows, lookup, capabilities, skills, tools, rung, threshold,
            revit=None, bad=(), progress=None):
    """Ask every row through `lookup` and score it. Returns the results.

    `lookup` is `heron_brain.lookup` in a real run; a test hands in the same
    function over a private store. `capabilities` maps a capability to its
    highest declared risk, `skills` a skill id to its needs and risk, `tools`
    a tool name to its risk name. Rows named in `bad` are not asked - their
    answer does not exist - and come back as errors.
    """
    results = []
    for i, row in enumerate(rows, 1):
        if progress and i % 10 == 0:
            progress(i, len(rows))
        kind, answer = row["kind"], row.get("answer")
        if row.get("n") in bad:
            results.append({"n": row.get("n"), "kind": kind, "answer": answer,
                            "question": " ".join(str(row["question"]).split()),
                            "error": "the answer key names something this "
                                     "library does not have"})
            continue
        if kind == "capability":
            wanted, expected_risk = {answer}, capabilities.get(answer)
        elif kind == "skill":
            wanted = set(skills[answer]["needs"])
            expected_risk = skills[answer]["risk"]
        elif kind == "tool":
            wanted, expected_risk = set(), tools.get(answer)
        else:
            wanted, expected_risk = set(), None
        try:
            found = lookup(row["question"], revit=revit)
        except Exception as why:                            # noqa: BLE001
            # ONE ROW, NOT THE RUN. The first lookup's BrainUnavailable is
            # caught in main() before this loop starts; anything here is one
            # sentence's problem, and it is named on its row.
            if type(why).__name__ == "BrainUnavailable":
                raise
            results.append({"n": row.get("n"), "kind": kind, "answer": answer,
                            "question": " ".join(str(row["question"]).split()),
                            "error": "%s: %s" % (type(why).__name__, why)})
            continue
        results.append(score_one(row, found, wanted, expected_risk, rung,
                                 threshold))
        results[-1]["backends"] = found.get("backends") or {}
        # AN UNREADABLE RISK COLUMN IS NOT A READ. With no risks every answer
        # looks safe, and a clean-looking row is the one place a crossing
        # would hide - FRAGMENT-ISSUES section 5b, row 35. Named, not judged.
        if found.get("risks_unreadable"):
            results[-1]["error"] = ("the fragments' risks could not be read "
                                    "(%s), so this row was not judged"
                                    % found["risks_unreadable"])
    return results


def backend_of(results):
    """The nearness backend the ranked answers ran on - read off the answers.

    An exact phrase runs no route, so only ranked answers vote. Two different
    names in one run is possible while a model is warming, and is said.

    "NOT USED" IS NO VOTE. heron_retrieve.ran() says it when no candidate of
    one answer carried a nearness rank - the meaning route found nothing for
    that sentence, on whichever backend was installed. It names no backend,
    so it cannot make a run "mixed"; measured 2026-10-17, *who drew this
    sheet* did that to the plain questions on lexical, and a label no later
    run would repeat made the run comparable with nothing. A run where NO
    answer carried a rank is still called "not used".
    """
    near, rerank, unused = {}, set(), 0
    for r in results:
        if r.get("error") or r.get("route") != "hybrid":
            continue
        again = (r.get("backends") or {}).get("rerank")
        if again and again != "not used":
            rerank.add(again)
        said = (r.get("backends") or {}).get("nearness") or "not said"
        if said == "not used":
            unused += 1
            continue
        near[said] = near.get(said, 0) + 1
    if not near:
        name = "not used" if unused else "none ranked"
    elif len(near) == 1:
        name = list(near)[0]
    else:
        name = "mixed " + ", ".join("%s %d" % kv for kv in sorted(near.items()))
    return name + ("+rerank" if rerank else "")


def report(results, summary, stamp, rows):
    """Print the run. Returns nothing; every figure is also in the row."""
    out = sys.stdout.write
    caps = summary["capability_rows"]

    def pct(k):
        return "%3d%%" % round(100.0 * k / caps) if caps else "  - "

    out("\nTHE OWNER'S QUESTIONS - %d capability rows, asked the way he asked "
        "them:\n\n" % caps)
    out("  1st            %3d  %s\n" % (summary["first"], pct(summary["first"])))
    out("  2nd-3rd        %3d  %s   top three together: %d\n"
        % (summary["second_third"], pct(summary["second_third"]),
           summary["first"] + summary["second_third"]))
    out("  4th-5th        %3d  %s   found, but low\n"
        % (summary["low"], pct(summary["low"])))
    out("  not found      %3d  %s\n"
        % (summary["not_found"], pct(summary["not_found"])))
    out("\n  First places that came by an EXACT DECLARED PHRASE: %d. That tests "
        "a declaration,\n  not the search. Rows of every kind answered that "
        "way: %d, and right: %d.\n"
        % (sum(1 for r in results if r["kind"] == "capability"
               and not r["error"] and r["place"] == 1
               and r["route"] == "identity"),
           summary["exact"], summary["exact_right"]))

    missed = [r for r in results if r["kind"] == "capability"
              and not r["error"] and r["place"] != 1]
    if missed:
        out("\nNOT FIRST - where each capability row went:\n")
        for r in sorted(missed, key=lambda r: (r["place"] or 99, r["n"])):
            out("  #%-3d %-4s wanted %-28s got %-28s %-12s %s\n"
                % (r["n"], _place_word(r["place"]), r["answer"],
                   r["got"] or "nothing", _route_word(r["route"]),
                   _short('"%s"' % r["question"])))

    skills = [r for r in results if r["kind"] == "skill" and not r["error"]]
    if skills:
        out("\nSKILL ROWS - %d of %d have a step in the top three. A skill is "
            "never indexed, so\nthe best the search can do is name one of "
            "the steps it declares:\n"
            % (summary["skill_top3"], summary["skill_rows"]))
        for r in skills:
            out("  #%-3d %-4s %-18s %s  %s\n"
                % (r["n"], _place_word(r["place"]), r["answer"],
                   ("reached %s" % r["reached"]) if r["reached"]
                   else "no step of it in the shortlist; got %s"
                   % (r["got"] or "nothing"),
                   _short('"%s"' % r["question"], 44)))

    landed = [r for r in results if r["kind"] in ("tool", "gap")
              and not r["error"]]
    if landed:
        out("\nTOOL AND GAP ROWS - no capability should answer these; where "
            "the search sent them:\n")
        for r in landed:
            out("  #%-3d %-4s %-14s got %-28s %-8s %s\n"
                % (r["n"], r["kind"], r["answer"] or "-", r["got"] or "nothing",
                   r["got_risk"] or "", _short('"%s"' % r["question"], 40)))

    handed = [r for r in results if handed_a_change(r)]
    out("\nASKED FOR NO CHANGE TO THE MODEL, AND HANDED ONE (D-86) - %d:\n"
        % len(handed))
    if not handed:
        out("  none\n")
    for r in handed:
        out("  #%-3d %-44s -> %s (%s); the answer key says %s (%s)\n"
            % (r["n"], _short('"%s"' % r["question"], 44), r["got"],
               r["got_risk"], r["answer"], r["expected_risk"]))

    wrong = [r for r in results if wrong_change(r)]
    if wrong:
        out("\nA CHANGE ASKED FOR, AND A DIFFERENT CHANGE GIVEN - %d:\n"
            % len(wrong))
        for r in wrong:
            out("  #%-3d %-44s -> %s (%s); wanted %s\n"
                % (r["n"], _short('"%s"' % r["question"], 44), r["got"],
                   r["got_risk"], r["answer"]))

    stand_in = [r for r in results if gap_onto_a_change(r)]
    if stand_in:
        out("\nA GAP ROW THAT LANDED ON A CHANGE - %d. Nothing in Heron does "
            "this job, so what\nanswered is a stand-in that writes:\n"
            % len(stand_in))
        for r in stand_in:
            out("  #%-3d %-44s -> %s (%s)\n"
                % (r["n"], _short('"%s"' % r["question"], 44), r["got"],
                   r["got_risk"]))

    broken = [r for r in results if r["error"]]
    if broken:
        out("\nNOT SCORED - %d. Each is named rather than counted as a miss:\n"
            % len(broken))
        for r in broken:
            out("  #%-3d %s\n" % (r["n"], r["error"]))


def compare(history_path, stamp, codes, summary, kinds, rows):
    """Print what moved since the last comparable run. Returns True on a drop."""
    out = sys.stdout.write
    try:
        with io.open(history_path, encoding="utf-8") as handle:
            table = history_rows(handle.read())
    except (IOError, OSError) as why:
        out("\nNOTHING TO COMPARE WITH - %s could not be read: %s\n"
            % (history_path, why))
        return False
    if table is None:
        out("\nNOTHING TO COMPARE WITH - %s has no table headed\n  %s\n"
            % (history_path, TABLE_HEADER))
        return False
    before = last_comparable(table, stamp)
    if before is None:
        out("\nNOTHING TO COMPARE WITH - no earlier run of answer key %s on "
            "the %s backend with\nRevit %s. This run is the first of its kind; "
            "--record makes it the baseline.\n"
            % (stamp["key"], stamp["backend"], stamp["revit"] or "any"))
        return False

    out("\nWHAT MOVED SINCE THE LAST COMPARABLE RUN - %s, %s fragments, %s, "
        "Revit %s:\n" % (before["Date"], before["Fragments"],
                         before["Backend"], before["Revit"]))
    if str(before["Fragments"]) != str(stamp["fragments"]):
        out("  The library changed size between the two (%s -> %s fragments). "
            "That is what this\n  comparison is for: a new fragment taking "
            "one of his questions is the drop.\n"
            % (before["Fragments"], stamp["fragments"]))
    for label, key, now in (("1st", "1st", summary["first"]),
                            ("2nd-3rd", "2nd-3rd", summary["second_third"]),
                            ("4th-5th", "4th-5th", summary["low"]),
                            ("not found", "Not found", summary["not_found"]),
                            ("handed a change", "Handed a change",
                             summary["handed"])):
        try:
            was = int(before[key])
        except (KeyError, ValueError):
            out("  %-16s could not read the earlier figure\n" % label)
            continue
        out("  %-16s %3d -> %3d%s\n"
            % (label, was, now, "" if now == was else "  (%+d)" % (now - was)))

    earlier = before.get("Per question", "")
    if len(earlier) != len(codes):
        out("  The earlier row records %d questions and this run asked %d - "
            "the rows cannot be\n  lined up, so nothing is said about any "
            "one question.\n" % (len(earlier), len(codes)))
        return False
    moves = moved(earlier, codes, kinds)
    if not moves:
        out("\n  Nothing moved: every question is where it was.\n")
    for direction, title in (("down", "MOVED DOWN"), ("up", "MOVED UP"),
                             ("unreadable", "COULD NOT BE COMPARED")):
        these = [m for m in moves if m[3] == direction]
        if not these:
            continue
        out("\n  %s - %d:\n" % (title, len(these)))
        for i, was, now, _ in these:
            out("    #%-3d %-4s -> %-4s %s\n"
                % (rows[i]["n"], was, now,
                   _short('"%s"' % " ".join(str(rows[i]["question"]).split()),
                          60)))
    drop = dropped(moves, before, summary)
    if drop:
        out("\n  THE SCORE DROPPED. Reported, never enforced - the owner "
            "decided a drop here never\n  stops a pull request (D-100). Read "
            "the questions above: each one is his.\n")
    return drop


# ---------------------------------------------------------------------------
# The plain questions - rows 5b-477 and 5b-478, scored beside his
# ---------------------------------------------------------------------------

def load_plain(path=PLAIN):
    """(doc, rows, problems) - the plain questions, and what is wrong with
    their SHAPE. Whether a named read exists is `unknown_plain`'s question."""
    import yaml

    with io.open(path, encoding="utf-8") as handle:
        doc = yaml.safe_load(handle) or {}
    rows = doc.get("questions") or []
    problems, seen = [], set()
    if not rows:
        problems.append("%s holds no questions" % os.path.relpath(path, ROOT))
    for i, row in enumerate(rows, 1):
        if not isinstance(row, dict):
            problems.append("row %d is not a mapping of n, row and question" % i)
            continue
        n, named = row.get("n"), row.get("named")
        question = " ".join(str(row.get("question") or "").split())
        answer = row.get("answer")
        if n != i:
            problems.append("row %d is numbered %r - they run 1, 2, 3 in "
                            "order, because the per-question record is by "
                            "position" % (i, n))
        if not question:
            problems.append("#%s has no question" % n)
        elif question.lower() in seen:
            problems.append("#%s repeats an earlier question" % n)
        seen.add(question.lower())
        if answer is not None and (not isinstance(answer, list) or not answer
                                   or not all(isinstance(a, str) and a
                                              for a in answer)):
            problems.append("#%s has answer %r - a list of capability names, "
                            "or no answer at all" % (n, answer))
        if named is not None and named not in PLAIN_NAMED:
            problems.append("#%s has named %r - one of %s"
                            % (n, named, ", ".join(PLAIN_NAMED)))
        elif named == "no-read" and answer:
            problems.append("#%s says no read answers it and names one (%s)"
                            % (n, ", ".join(answer)))
        elif named in ("declared", "ranked", "owners-call") and not answer:
            problems.append("#%s is %s and names no read" % (n, named))
        elif answer and named is None:
            problems.append("#%s names a read and does not say how the row "
                            "named it" % n)
    return doc, rows, problems


def plain_fingerprint(rows):
    """Eight hex characters naming THIS set - questions and named reads."""
    canon = [[row.get("n"), " ".join(str(row.get("question") or "").split()),
              sorted(row.get("answer") or [])] for row in rows]
    blob = json.dumps(canon, ensure_ascii=False, separators=(",", ":"))
    return hashlib.sha256(blob.encode("utf-8")).hexdigest()[:8]


def plain_kinds(rows):
    """NAMED or UNNAMED per row, in order - what `moved` reads each code as."""
    return [NAMED if row.get("answer") else UNNAMED for row in rows]


def unknown_plain(rows, capabilities):
    """[(n, why)] - a named read this library does not have. Scored `?`."""
    out = []
    for row in rows:
        missing = [a for a in row.get("answer") or [] if a not in capabilities]
        if missing:
            out.append((row.get("n"), "no fragment in this library provides "
                                      "%s" % ", ".join(missing)))
    return out


def measure_plain(rows, lookup, rung, threshold, revit=None, bad=(),
                  progress=None):
    """Ask every plain question through `lookup`. Returns the results.

    Never raises for one row: a failed lookup is named on its row, as
    `measure` does for his. Rows named in `bad` are not asked.
    """
    results = []
    for i, row in enumerate(rows, 1):
        if progress and i % 10 == 0:
            progress(i, len(rows))
        question = " ".join(str(row["question"]).split())
        base = {"n": row.get("n"), "row": row.get("row"), "question": question,
                "answer": list(row.get("answer") or []), "error": None}
        if row.get("n") in bad:
            base["error"] = ("a named read is one this library does not have")
            results.append(base)
            continue
        try:
            found = lookup(row["question"], revit=revit)
        except Exception as why:                            # noqa: BLE001
            if type(why).__name__ == "BrainUnavailable":
                raise
            base["error"] = "%s: %s" % (type(why).__name__, why)
            results.append(base)
            continue
        order = shortlist(found)
        got_risk = (found.get("risk") or "").upper() or None
        at, _ = place(order, set(base["answer"])) if base["answer"] else (None,
                                                                          None)
        base.update({"route": found.get("route"), "got": found.get("capability"),
                     "got_risk": got_risk, "order": order, "place": at,
                     "writes": got_risk is not None
                     and rung(got_risk) >= threshold,
                     "backends": found.get("backends") or {}})
        if found.get("risks_unreadable"):
            base["error"] = ("the fragments' risks could not be read (%s), so "
                             "this row was not judged"
                             % found["risks_unreadable"])
        results.append(base)
    return results


def plain_code(result):
    """The one character the plain table keeps for this row."""
    if result.get("error"):
        return ERROR
    if not result.get("got"):
        return LANDED_NOTHING
    if result["writes"]:
        return LANDED_CHANGE
    if result["answer"]:
        return str(result["place"]) if result["place"] else NOT_FOUND
    return LANDED_OTHER


def summarise_plain(results):
    """The counts the plain table keeps."""
    ok = [r for r in results if not r["error"]]
    named = [r for r in ok if r["answer"]]
    writes = [r for r in ok if r["writes"]]
    by_row = {}
    for r in writes:
        by_row[r.get("row")] = by_row.get(r.get("row"), 0) + 1
    return {"asked": len(ok), "writes": len(writes), "writes_by_row": by_row,
            "exact": sum(1 for r in ok if r["route"] == "identity"),
            "named": len(named),
            "named_first": sum(1 for r in named if r["place"] == 1),
            "named_top3": sum(1 for r in named if r["place"]
                              and r["place"] <= 3),
            "errors": len(results) - len(ok)}


def plain_history_row(stamp, summary, codes):
    """One line of the plain questions' table in brain/retrieval-history.md."""
    cells = [stamp["date"], str(stamp["fragments"]), "`%s`" % stamp["backend"],
             stamp["revit"] or "any", "`%s`" % stamp["key"],
             str(summary["asked"]), str(summary["writes"]),
             str(summary["exact"]),
             "%d of %d" % (summary["named_first"], summary["named"]),
             "%d of %d" % (summary["named_top3"], summary["named"]),
             "`%s`" % codes]
    return "| " + " | ".join(cells) + " |"


def plain_sources(rows):
    """'rows 5b-477, 5b-478 and 5b-1005' - read off the rows, never typed, so a
    row that joins the set is named the day it does."""
    found = sorted(set(r.get("row") for r in rows if r.get("row")), key=str)
    names = ["5b-%s" % n for n in found]
    if not names:
        return "no row"
    if len(names) == 1:
        return "row " + names[0]
    return "rows " + ", ".join(names[:-1]) + " and " + names[-1]


def report_plain(results, summary):
    """Print the plain questions' run."""
    out = sys.stdout.write
    by_row = ", ".join("row %s: %d" % kv for kv in sorted(
        summary["writes_by_row"].items(), key=lambda kv: str(kv[0])))
    out("\nTHE PLAIN QUESTIONS - %d asked, every one a question (%s):\n\n"
        % (summary["asked"], plain_sources(results)))
    out("  answered by something that CHANGES THE MODEL   %3d%s\n"
        % (summary["writes"], "   (%s)" % by_row if by_row else ""))
    out("  answered by an exact declared phrase           %3d   tests a "
        "declaration, not the search\n" % summary["exact"])
    out("  the read the row names, first                  %3d of %d\n"
        % (summary["named_first"], summary["named"]))
    out("  the read the row names, in the top three       %3d of %d\n"
        % (summary["named_top3"], summary["named"]))

    writes = [r for r in results if not r["error"] and r["writes"]]
    out("\nA QUESTION ANSWERED BY A CHANGE (D-86) - %d:\n" % len(writes))
    if not writes:
        out("  none\n")
    for r in writes:
        out("  #%-3d %-46s -> %s (%s)%s\n"
            % (r["n"], _short('"%s"' % r["question"], 46), r["got"],
               r["got_risk"], "; the row names %s" % " or ".join(r["answer"])
               if r["answer"] else ""))

    missed = [r for r in results if not r["error"] and r["answer"]
              and not r["writes"] and r["place"] != 1]
    if missed:
        out("\nA READ ANSWERED, BUT NOT THE ONE THE ROW NAMES - %d:\n"
            % len(missed))
        for r in missed:
            out("  #%-3d %-4s %-46s got %s; the row names %s\n"
                % (r["n"], _place_word(r["place"]),
                   _short('"%s"' % r["question"], 46), r["got"] or "nothing",
                   " or ".join(r["answer"])))

    broken = [r for r in results if r["error"]]
    if broken:
        out("\nNOT SCORED - %d:\n" % len(broken))
        for r in broken:
            out("  #%-3d %s\n" % (r["n"], r["error"]))


def compare_plain(history_path, stamp, codes, summary, kinds, rows):
    """Print what moved since the last comparable plain run. True on a drop:
    a question moving toward a change, or more of them answered by one."""
    out = sys.stdout.write
    try:
        with io.open(history_path, encoding="utf-8") as handle:
            table = history_rows(handle.read(), PLAIN_HEADER, PLAIN_COLUMNS)
    except (IOError, OSError) as why:
        out("\nNOTHING TO COMPARE THE PLAIN QUESTIONS WITH - %s could not be "
            "read: %s\n" % (history_path, why))
        return False
    if table is None:
        out("\nNOTHING TO COMPARE THE PLAIN QUESTIONS WITH - %s has no table "
            "headed\n  %s\n" % (history_path, PLAIN_HEADER))
        return False
    before = last_comparable(table, stamp)
    if before is None:
        out("\nNOTHING TO COMPARE THE PLAIN QUESTIONS WITH - no earlier run of "
            "set %s on the %s backend\nwith Revit %s. --record makes this one "
            "the baseline.\n" % (stamp["key"], stamp["backend"],
                                 stamp["revit"] or "any"))
        return False
    out("\nTHE PLAIN QUESTIONS SINCE THE LAST COMPARABLE RUN - %s, %s "
        "fragments:\n" % (before["Date"], before["Fragments"]))
    try:
        was = int(before["Reached a write"])
        out("  reached a write  %3d -> %3d%s\n"
            % (was, summary["writes"], "" if was == summary["writes"]
               else "  (%+d)" % (summary["writes"] - was)))
    except (KeyError, ValueError):
        was = None
        out("  reached a write  could not read the earlier figure\n")
    earlier = before.get("Per question", "")
    if len(earlier) != len(codes):
        out("  The earlier row records %d questions and this run asked %d, so "
            "nothing is said\n  about any one question.\n"
            % (len(earlier), len(codes)))
        return was is not None and summary["writes"] > was
    moves = moved(earlier, codes, kinds)
    if not moves:
        out("  Nothing moved: every question is where it was.\n")
    for direction, title in (("down", "MOVED DOWN"), ("up", "MOVED UP"),
                             ("unreadable", "COULD NOT BE COMPARED")):
        these = [m for m in moves if m[3] == direction]
        if not these:
            continue
        out("\n  %s - %d:\n" % (title, len(these)))
        for i, a, b, _ in these:
            out("    #%-3d %-4s -> %-4s %s\n"
                % (rows[i]["n"], a, b, _short('"%s"' % " ".join(
                    str(rows[i]["question"]).split()), 60)))
    drop = (any(m[3] == "down" for m in moves)
            or (was is not None and summary["writes"] > was))
    if drop:
        out("\n  THE PLAIN QUESTIONS DROPPED. Reported, never enforced (D-100) "
            "- read the questions\n  above.\n")
    return drop


# ---------------------------------------------------------------------------
# A session's own sentences - main against a branch, on the backend named
# ---------------------------------------------------------------------------
#
# Rows 5b-481, 5b-485 and 5b-510 each rebuilt this as a scratch kit on the
# owner's PC, and each time a silent fallback measured nothing: row 481's
# "both backends" were both re-ranked, found only later. So every backend is
# asked in a process of its own, every answer carries the backends that
# produced it, and a record whose answers ran on anything but the backend
# asked for is refused, whether it is being written or read back.

RECORD_KIND = "score-routing sentence record"
RECORD_VERSION = 1
WANT, NOT_A_WRITE, NEUTRAL = "want", "not-a-write", "neutral"
# The four backends a card's words were measured on in rows 485 and 510:
# lexical (CI's), the trained model, and each with the cross-encoder (the
# owner's PC is the model with it). The names are the ones heron_retrieve.ran()
# writes, joined the way backend_of() already labels a run.
LEXICAL, MODEL = "lexical", "model"
BACKENDS = (LEXICAL, MODEL, LEXICAL + "+rerank", MODEL + "+rerank")
# Whatever this machine has, named by what answered rather than by what was
# asked for - the run is still refused if its answers disagree with each other.
INSTALLED = "installed"
# What an answer's `backends` says when a route did not run. Anything else in
# `rerank` means the cross-encoder re-read that shortlist.
NOT_RAN = ("not used", "not said")

TAGS = (WANT, "bad", NEUTRAL)


class CouldNotAsk(Exception):
    """A worker could not ask the sentences at all - nothing was measured."""


def _norm(text):
    return " ".join(str(text or "").split())


def parse_expect(text):
    """(kind, capability) for one expectation, or None when it is not one.

    `want CAPABILITY` - this card should answer; `not-a-write` - a question,
    which nothing that changes the model should answer; `neutral` - watched,
    with no right answer either way.
    """
    words = _norm(text).split()
    if (len(words) == 2 and words[0].lower() == WANT
            and re.match(r"^[A-Z][A-Z0-9_]*$", words[1])):
        return WANT, words[1]
    if len(words) == 1 and words[0].lower() in (NOT_A_WRITE, NEUTRAL):
        return words[0].lower(), None
    return None


def expect_word(kind, want):
    return "%s %s" % (WANT, want) if kind == WANT else kind


def load_sentences(path):
    """(doc, items, problems) - a frozen sentence set, and what is wrong with
    its SHAPE. Whether a wanted capability exists is the library's question.

        groups:
          - group: the three
            expect: want RENAME_ELEMENTS     # for every sentence below
            sentences:
              - rename this view
              - say: which views have SUP in the name
                expect: not-a-write          # its own, over the group's

    A sentence may sit in two groups (row 485 had one) and is asked once, but
    only with ONE expectation: two would make every change to its answer both
    a win and a loss.
    """
    import yaml

    with io.open(path, encoding="utf-8") as handle:
        doc = yaml.safe_load(handle) or {}
    groups = doc.get("groups") if isinstance(doc, dict) else None
    if not isinstance(groups, list) or not groups:
        return doc, [], ["%s has no `groups:` list" % path]
    problems, items, seen = [], [], {}
    for g, group in enumerate(groups, 1):
        if not isinstance(group, dict) or not _norm(group.get("group")):
            problems.append("group %d has no `group:` name" % g)
            continue
        name = _norm(group["group"])
        default = group.get("expect")
        if default is not None and parse_expect(default) is None:
            problems.append("group %r expects %r - want <CAPABILITY>, "
                            "not-a-write or neutral" % (name, default))
            default = None
        said = group.get("sentences")
        if not isinstance(said, list) or not said:
            problems.append("group %r has no sentences" % name)
            continue
        for one in said:
            if isinstance(one, dict):
                say, expect = one.get("say"), one.get("expect", default)
                if say is None:
                    problems.append("group %r has a mapping with no `say:` - "
                                    "a sentence with a colon in it needs "
                                    "quotes: %r" % (name, one))
                    continue
            else:
                say, expect = one, default
            say = _norm(say)
            if not say:
                problems.append("group %r has an empty sentence" % name)
                continue
            parsed = parse_expect(expect)
            if parsed is None:
                problems.append("%r in group %r has %s - want <CAPABILITY>, "
                                "not-a-write or neutral"
                                % (say, name, "no expectation" if expect is None
                                   else "the expectation %r" % (expect,)))
                continue
            held = seen.get(say.lower())
            if held is not None:
                if (held["kind"], held["want"]) != parsed:
                    problems.append("%r is %s in group %r and %s in group %r "
                                    "- one sentence, one expectation"
                                    % (say, held["expect"], held["groups"][0],
                                       expect_word(*parsed), name))
                elif name not in held["groups"]:
                    held["groups"].append(name)
                continue
            item = {"n": len(items) + 1, "say": say, "kind": parsed[0],
                    "want": parsed[1], "expect": expect_word(*parsed),
                    "groups": [name]}
            seen[say.lower()] = item
            items.append(item)
    return doc, items, problems


def sentence_fingerprint(items):
    """Eight hex characters naming this set - its sentences and expectations.
    Groups are labels and stay out of it."""
    canon = [[item["say"], item["expect"]] for item in items]
    blob = json.dumps(canon, ensure_ascii=False, separators=(",", ":"))
    return hashlib.sha256(blob.encode("utf-8")).hexdigest()[:8]


def split_backend(name):
    """(nearness, re-ranked) asked for, or (None, None) for `installed`."""
    if name == INSTALLED:
        return None, None
    nearness, _sep, extra = name.partition("+")
    return nearness, extra == "rerank"


def _reranked(answer):
    return (answer.get("backends") or {}).get("rerank") not in (None,) + NOT_RAN


def backend_problems(asked, answers):
    """[] when these answers are a run on `asked`, else what says they are not.

    A RANK IS THE EVIDENCE, per answer (heron_retrieve.ran()). An exact phrase
    or a remembered answer runs no route and casts no vote. A ranked answer
    whose meaning route found nothing says "not used", which names no backend;
    a run where NO ranked answer named the one asked for proved nothing. And a
    shortlist of two or more that the re-ranker did not re-read, on a run that
    asked for it, is the silent fallback rows 481 and 485 measured.
    """
    ranked = [a for a in answers if not a.get("error")
              and a.get("route") == "hybrid"]
    named = {}
    for a in ranked:
        said = (a.get("backends") or {}).get("nearness") or "not said"
        if said != "not used":
            named.setdefault(said, []).append(a["n"])
    could_rerank = [a for a in ranked if (a.get("candidates") or 0) >= 2]
    reread = [a["n"] for a in could_rerank if _reranked(a)]
    unread = [a["n"] for a in could_rerank if not _reranked(a)]

    def which(ns):
        return "#" + ", #".join(str(n) for n in ns[:5]) + (
            " and %d more" % (len(ns) - 5) if len(ns) > 5 else "")

    problems = []
    nearness, rerank = split_backend(asked)
    if nearness is None:
        if len(named) > 1:
            problems.append("the answers ran on more than one nearness backend "
                            "- %s" % "; ".join("%s %s" % (k, which(v))
                                               for k, v in sorted(named.items())))
        if reread and unread:
            problems.append("the re-ranker re-read %d shortlists and not %d "
                            "others - %s" % (len(reread), len(unread),
                                             which(unread)))
        return problems
    for said, ns in sorted(named.items()):
        if said != nearness:
            problems.append("%d ranked answer(s) ran on %s nearness, not %s - %s"
                            % (len(ns), said, nearness, which(ns)))
    if ranked and nearness not in named:
        problems.append("no ranked answer carried a %s nearness rank, so "
                        "nothing here measured %s" % (nearness, nearness))
    if rerank and unread:
        problems.append("%d shortlist(s) of two or more were not re-read by the "
                        "cross-encoder - %s" % (len(unread), which(unread)))
    if not rerank and reread:
        problems.append("the cross-encoder re-read %d shortlist(s) on a run "
                        "asked for without it - %s" % (len(reread), which(reread)))
    return problems


def tree_problem(tree):
    """Why `tree` is not a Heron checkout this can measure, or None."""
    for part in (("brain", "heron_retrieve.py"), ("brain", "fragments"),
                 ("mcp", "server", "heron_brain.py")):
        if not os.path.exists(os.path.join(tree, *part)):
            return "%s has no %s" % (tree, "/".join(part))
    return None


def tree_head(tree):
    """(commit, dirty) of a checkout - (None, None) where git cannot say.

    DIRTY COUNTS AN UNTRACKED CARD. A new fragment folder not yet added is
    read into the store like any other, so a tree carrying one is not the
    commit its head names.
    """
    try:
        head = subprocess.run(["git", "-C", tree, "rev-parse",
                               "--show-toplevel", "HEAD"],
                              capture_output=True, encoding="utf-8",
                              errors="replace", timeout=60)
        said = head.stdout.strip().splitlines()
        # A FOLDER INSIDE SOMEBODY ELSE'S CHECKOUT IS NOT ONE, and git would
        # answer with the enclosing repository's commit.
        if head.returncode != 0 or len(said) != 2 or (
                os.path.normcase(os.path.realpath(said[0]))
                != os.path.normcase(os.path.realpath(tree))):
            return None, None
        status = subprocess.run(["git", "-C", tree, "status", "--porcelain"],
                                capture_output=True, encoding="utf-8",
                                errors="replace", timeout=60)
        dirty = (bool(status.stdout.strip()) if status.returncode == 0
                 else None)
        return said[1], dirty
    except (OSError, subprocess.SubprocessError):
        return None, None


def ask_tree(tree, backend, items, shortlists=False, revit=None):
    """Ask every sentence on one backend, with `tree`'s own search code over a
    store built from `tree`'s cards. Returns the worker's run.

    A PROCESS PER BACKEND, AND A STORE PER PROCESS. Which encoder and which
    re-ranker answer is settled once per process (heron_embed, heron_rerank),
    and a store's vectors are the encoder's that wrote them - so a second
    backend in the same process, or on the same store, is the silent mix this
    exists to stop. The store is a scratch folder deleted afterwards, never
    the one every chat on the machine reads (row 5b-233).

    Raises CouldNotAsk when nothing was measured.
    """
    work = tempfile.mkdtemp(prefix="heron-score-sentences-")
    try:
        job = os.path.join(work, "job.json")
        answers = os.path.join(work, "answers.json")
        with io.open(job, "w", encoding="utf-8") as handle:
            json.dump({"tree": tree, "backend": backend,
                       "sentences": [item["say"] for item in items],
                       "shortlists": bool(shortlists), "revit": revit},
                      handle, ensure_ascii=False)
        env = dict(os.environ)
        env["HERON_KNOWLEDGE"] = os.path.join(work, "knowledge")
        env["HERON_AUDIT"] = os.path.join(work, "audit")
        os.makedirs(env["HERON_KNOWLEDGE"])
        # Rows 485 and 510 measured with one BLAS thread, one run at a time;
        # this asks one backend at a time and keeps the thread.
        env.setdefault("OPENBLAS_NUM_THREADS", "1")
        env["PYTHONIOENCODING"] = "utf-8"
        started = time.time()
        done = subprocess.run([sys.executable, os.path.abspath(__file__),
                               "--worker", job, answers],
                              env=env, capture_output=True, encoding="utf-8",
                              errors="replace")
        took = time.time() - started
        if not os.path.exists(answers):
            tail = "\n".join((done.stderr or done.stdout or "").strip()
                             .splitlines()[-8:])
            raise CouldNotAsk("the worker on %s stopped with exit %s and wrote "
                              "nothing:\n%s" % (backend, done.returncode, tail))
        with io.open(answers, encoding="utf-8") as handle:
            run = json.load(handle)
        if run.get("refused"):
            raise CouldNotAsk(run["refused"])
        run["took_s"] = round(took, 1)
        return run
    finally:
        shutil.rmtree(work, ignore_errors=True)


def _cell(row, key):
    try:
        return row[key]
    except (IndexError, KeyError, TypeError):
        return None


def _spy_on_shortlists(retrieve, rerank, held):
    """Wrap `retrieve._rerank` so each call leaves the shortlist it read in
    `held`. False when this tree has no such seam.

    NO HOOK IN heron_retrieve, AND THAT IS WHY IT WORKS ON MAIN TOO. `retrieve`
    looks `_rerank` up as a module global on every call, so wrapping it here
    reads the head of the list - at most heron_rerank.SHORTLIST, the most the
    cross-encoder ever reads - with the passage each candidate is given, the
    fused score that breaks a tie and the cross-encoder's score once it has
    run. The order it returns is the real `_rerank`'s, untouched.
    """
    real = getattr(retrieve, "_rerank", None)
    if not callable(real):
        return False
    size = getattr(rerank, "SHORTLIST", 20)

    def spy(text, ranked, passage):
        head = list(ranked[:size])
        passages = [passage(c) for c in head]
        out = real(text, ranked, passage)
        held.append([{"id": c.id, "capability": _cell(c.row, "capability"),
                      "identity": _cell(c.row, "semantic_identity"),
                      "passage": said, "fused": c.score,
                      "rerank": c.rerank_score}
                     for c, said in zip(head, passages)])
        return out

    retrieve._rerank = spy
    return True


def _worker(job_path, out_path):
    """The other half of ask_tree(), in its own process. Writes the answers,
    or `refused` with why. Exit 0, or 2 when nothing was asked."""
    with io.open(job_path, encoding="utf-8") as handle:
        job = json.load(handle)

    def write(doc):
        with io.open(out_path, "w", encoding="utf-8") as handle:
            json.dump(doc, handle, ensure_ascii=False)

    def refuse(why):
        write({"refused": why})
        return 2

    # THE MEASURED TREE'S SEARCH, AND NONE OF THIS ONE'S. This file put its own
    # brain/ and mcp/server/ first on the path when it loaded; a module the
    # other tree lacked would otherwise be borrowed from here without a word.
    tree = os.path.realpath(job["tree"])
    ours = set(os.path.normcase(os.path.realpath(os.path.join(ROOT, *p)))
               for p in (("brain",), ("mcp", "server")))
    sys.path[:] = [p for p in sys.path
                   if os.path.normcase(os.path.realpath(p or os.curdir))
                   not in ours]
    sys.path[:0] = [os.path.join(tree, "mcp", "server"),
                    os.path.join(tree, "brain")]
    try:
        import heron_embed as EMBED
        import heron_rerank as RERANK
        import heron_retrieve as RETRIEVE
        import heron_scope as SCOPE
        import heron_brain as BRAIN
    except Exception as why:                                # noqa: BLE001
        return refuse("%s's search could not be imported: %s: %s"
                      % (tree, type(why).__name__, why))
    for module in (EMBED, RERANK, RETRIEVE, SCOPE, BRAIN):
        here = os.path.normcase(os.path.realpath(module.__file__))
        if not here.startswith(os.path.normcase(tree) + os.sep):
            return refuse("%s was loaded from %s, not from the tree measured"
                          % (module.__name__, here))

    # TURNED OFF BY SETTLING THE LOADER'S ANSWER BEFORE ANYTHING ASKS IT. Row
    # 481: system Python ran the cross-encoder whatever HERON_EMBED_MODEL
    # said. Each loader keeps its answer in one list and reads it first, so
    # [None] there is "nothing installed" for the life of this process.
    nearness, rerank = split_backend(job["backend"])
    for off, module, cache in ((nearness == LEXICAL, EMBED, "_MODEL_CACHE"),
                               (rerank is False, RERANK, "_CACHE")):
        if not off:
            continue
        held = getattr(module, cache, None)
        if not isinstance(held, list):
            return refuse("%s has no %s to turn it off with" % (module.__name__,
                                                                 cache))
        held[:] = [None]
    have = {"nearness": EMBED.backend()[0], "rerank": RERANK.backend()[0]}
    if nearness is not None and have["nearness"] != nearness:
        return refuse("%s was asked for, and this machine's encoder answers %s "
                      "- nothing was asked" % (job["backend"], have["nearness"]))
    if rerank and have["rerank"] != RERANK.CROSS_ENCODER:
        return refuse("%s was asked for, and this machine has no cross-encoder "
                      "(%s) - nothing was asked. %s"
                      % (job["backend"], have["rerank"],
                         "pip install --user sentence-transformers, which "
                         "needs huggingface.co once for the weights"))

    held = []
    if job.get("shortlists") and not _spy_on_shortlists(RETRIEVE, RERANK, held):
        return refuse("%s's heron_retrieve has no _rerank to read a shortlist "
                      "at" % tree)

    answers = []
    for i, say in enumerate(job["sentences"], 1):
        if i % 25 == 0:
            sys.stderr.write("  asked %d of %d\n" % (i, len(job["sentences"])))
        del held[:]
        try:
            found = BRAIN.lookup(say, revit=job.get("revit"))
        except Exception as why:                            # noqa: BLE001
            # ONE SENTENCE, NOT THE RUN - unless the store itself is gone.
            if type(why).__name__ == "BrainUnavailable":
                return refuse("COULD NOT ASK: %s" % why)
            answers.append({"n": i, "say": say,
                            "error": "%s: %s" % (type(why).__name__, why)})
            continue
        said = found.get("backends") or {}
        answers.append({
            "n": i, "say": say, "route": found.get("route"),
            "capability": found.get("capability"),
            "provider": found.get("provider"), "risk": found.get("risk"),
            "order": shortlist(found),
            "candidates": len(found.get("candidates") or []),
            "backends": {"nearness": said.get("nearness"),
                         "rerank": said.get("rerank")},
            # AN UNREADABLE RISK COLUMN IS NOT A READ - row 5b-35, as above.
            "error": ("the fragments' risks could not be read (%s)"
                      % found["risks_unreadable"]
                      if found.get("risks_unreadable") else None),
            "shortlist": (held[-1] if held else None)
            if job.get("shortlists") else None})

    store = SCOPE.open_scope(SCOPE.GLOBAL)
    try:
        cards = [[r["capability"], r.get("risk")] for r in store.fragments()
                 if r.get("capability")]
        fragments = store.count()
    finally:
        store.close()
    write({"tree": tree, "asked": job["backend"], "available": have,
           "fragments": fragments, "capabilities": cards,
           "shortlists": bool(job.get("shortlists")), "answers": answers})
    return 0


def highest_risks(cards, rung):
    """{capability: its highest declared risk} from a worker's [[cap, risk]]."""
    out = {}
    for capability, risk in cards:
        if capability not in out or rung(risk) > rung(out[capability]):
            out[capability] = risk
    return out


def outcome(item, answer, rung, threshold):
    """What one answer means for one sentence: (writes, place, ok).

    `place` is where a wanted capability sits in the shortlist heron_lookup
    shows; `ok` is None for a neutral sentence, or for an answer that could
    not be scored.
    """
    if answer is None or answer.get("error"):
        return None, None, None
    writes = (answer.get("risk") is not None
              and rung((answer.get("risk") or "").upper()) >= threshold)
    if item["kind"] == WANT:
        at, _ = place(answer.get("order") or [], {item["want"]})
        return writes, at, answer.get("capability") == item["want"]
    if item["kind"] == NOT_A_WRITE:
        return writes, None, not writes
    return writes, None, None


def tag(item, before, after, writes):
    """want, bad or neutral for one answer that changed. `writes(answer)`.

    want X: X came to answer it is a want; X left it is bad; one wrong answer
    for another is neutral. not-a-write: a question that reaches a write now
    is bad, one that stopped reaching one is a want. neutral is neutral.
    """
    if item["kind"] == WANT:
        if after.get("capability") == item["want"]:
            return WANT
        if before.get("capability") == item["want"]:
            return "bad"
        return NEUTRAL
    if item["kind"] == NOT_A_WRITE:
        if writes(after) and not writes(before):
            return "bad"
        if writes(before) and not writes(after):
            return WANT
    return NEUTRAL


def _risked(answer):
    return "%s (%s)" % (answer.get("capability") or "nothing",
                        answer.get("risk") or "-")


def report_run(label, run, items, rung, threshold):
    """Print one backend's run. Returns its counts."""
    out = sys.stdout.write
    by_n = dict((a["n"], a) for a in run["answers"])
    caps = run["capabilities"]
    totals = {"asked": 0, "wanted": 0, "reached": 0, "questions": 0,
              "to_a_write": 0, "exact": 0, "errors": 0}
    groups, order = {}, []
    missed, crossed, broken, unknown = [], [], [], []
    for item in items:
        answer = by_n.get(item["n"])
        writes, at, ok = outcome(item, answer, rung, threshold)
        if writes is None:
            broken.append((item, (answer or {}).get("error") or "not asked"))
            totals["errors"] += 1
            continue
        if item["kind"] == WANT and item["want"] not in caps:
            unknown.append(item)
            continue
        for name in item["groups"]:
            if name not in groups:
                order.append(name)
                groups[name] = dict((k, 0) for k in totals)
        for counts in [totals] + [groups[g] for g in item["groups"]]:
            counts["asked"] += 1
            counts["exact"] += answer.get("route") == "identity"
            if item["kind"] == WANT:
                counts["wanted"] += 1
                counts["reached"] += bool(ok)
            elif item["kind"] == NOT_A_WRITE:
                counts["questions"] += 1
                counts["to_a_write"] += bool(writes)
        if item["kind"] == WANT and not ok:
            missed.append((item, answer, at))
        if item["kind"] == NOT_A_WRITE and writes:
            crossed.append((item, answer))

    def frac(k, of):
        return "%d of %d" % (k, of) if of else "-"

    out("\n%s - %d fragments, %d sentences asked, %s s\n"
        % (label.upper(), run["fragments"], len(run["answers"]),
           run.get("took_s", "?")))
    out("  %-40s %5s  %-10s %-19s %s\n" % ("group", "asked", "wanted",
                                           "questions -> write", "exact"))
    for name in order + ["all"]:
        c = totals if name == "all" else groups[name]
        out("  %-40s %5d  %-10s %-19s %d\n"
            % (_short(name, 40), c["asked"], frac(c["reached"], c["wanted"]),
               frac(c["to_a_write"], c["questions"]), c["exact"]))
    if missed:
        out("\n  WANTED, AND SOMETHING ELSE ANSWERED - %d:\n" % len(missed))
        for item, answer, at in missed:
            out("    #%-4d %-44s -> %s; wants %s%s\n"
                % (item["n"], _short('"%s"' % item["say"], 44), _risked(answer),
                   item["want"], " (%s)" % _place_word(at) if at else ""))
    if crossed:
        out("\n  A QUESTION ANSWERED BY A WRITE (D-86) - %d:\n" % len(crossed))
        for item, answer in crossed:
            out("    #%-4d %-44s -> %s\n"
                % (item["n"], _short('"%s"' % item["say"], 44),
                   _risked(answer)))
    if unknown:
        out("\n  NOT SCORED - the set wants a capability this library does not "
            "have - %d:\n" % len(unknown))
        for item in unknown:
            out("    #%-4d %-44s wants %s\n"
                % (item["n"], _short('"%s"' % item["say"], 44), item["want"]))
    if broken:
        out("\n  NOT SCORED - %d:\n" % len(broken))
        for item, why in broken:
            out("    #%-4d %s\n" % (item["n"], why))
    return totals


def compare_runs(label, before, after, items, rung, threshold):
    """Print every sentence whose answer changed between two runs on one
    backend, tagged. Returns the counts. Sentences are matched by their words,
    so a set that grew compares what the two have in common."""
    out = sys.stdout.write

    def writes(answer):
        return outcome({"kind": NEUTRAL}, answer, rung, threshold)[0]

    was = dict((_norm(a["say"]).lower(), a) for a in before["answers"])
    now = dict((a["n"], a) for a in after["answers"])
    counts = {"both": 0, "changed": 0, "unreadable": 0, "only_here": 0,
              "questions": 0, "write_before": 0, "write_after": 0,
              "wanted": 0, "reached_before": 0, "reached_after": 0}
    for tagged in TAGS:
        counts[tagged] = 0
    lines = []
    for item in items:
        a, b = was.get(item["say"].lower()), now.get(item["n"])
        if a is None:
            counts["only_here"] += 1
            continue
        if b is None or a.get("error") or b.get("error"):
            counts["unreadable"] += 1
            continue
        counts["both"] += 1
        if item["kind"] == NOT_A_WRITE:
            counts["questions"] += 1
            counts["write_before"] += bool(writes(a))
            counts["write_after"] += bool(writes(b))
        if item["kind"] == WANT:
            counts["wanted"] += 1
            counts["reached_before"] += a.get("capability") == item["want"]
            counts["reached_after"] += b.get("capability") == item["want"]
        if a.get("capability") == b.get("capability"):
            continue
        tagged = tag(item, a, b, writes)
        counts["changed"] += 1
        counts[tagged] += 1
        lines.append((TAGS.index(tagged), item["n"], tagged, item, a, b))
    out("\n%s - %d sentences in both records, %d answers changed "
        "(want %d, bad %d, neutral %d):\n"
        % (label.upper(), counts["both"], counts["changed"], counts[WANT],
           counts["bad"], counts[NEUTRAL]))
    for _rank, _n, tagged, item, a, b in sorted(lines, key=lambda x: x[:2]):
        out("  %-7s #%-4d %-40s %s -> %s   [%s]\n"
            % (tagged, item["n"], _short('"%s"' % item["say"], 40), _risked(a),
               _risked(b), item["expect"]))
    out("  questions reaching a write  %d -> %d of %d\n"
        % (counts["write_before"], counts["write_after"], counts["questions"]))
    out("  wanted answers reached      %d -> %d of %d\n"
        % (counts["reached_before"], counts["reached_after"], counts["wanted"]))
    if counts["only_here"]:
        out("  %d sentence(s) are in this set and not in the earlier record\n"
            % counts["only_here"])
    if counts["unreadable"]:
        out("  %d sentence(s) could not be scored on one side or the other\n"
            % counts["unreadable"])
    return counts


def make_record(path, items, runs, tree, head, dirty, revit):
    """The record --out writes. Everything --compare and --from read back.
    `head` and `dirty` are the ones printed when the run began."""
    return {"record": RECORD_KIND, "version": RECORD_VERSION,
            "date": datetime.date.today().isoformat(),
            "tree": {"path": tree, "head": head, "dirty": dirty},
            "revit": revit,
            "sentences": {"file": path, "fingerprint": sentence_fingerprint(items),
                          "items": [{"say": i["say"], "expect": i["expect"],
                                     "groups": i["groups"]} for i in items]},
            "runs": runs}


def load_record(path):
    """(record, refused) - a record, and {backend: why} for every run in it
    that is NOT a run on the backend it is filed under. Raises CouldNotAsk
    when the file is not a record at all."""
    try:
        with io.open(path, encoding="utf-8") as handle:
            record = json.load(handle)
    except (IOError, OSError, ValueError) as why:
        raise CouldNotAsk("%s could not be read as a record: %s" % (path, why))
    if (not isinstance(record, dict) or record.get("record") != RECORD_KIND
            or not isinstance(record.get("runs"), dict)):
        raise CouldNotAsk("%s is not a record --out wrote" % path)
    if record.get("version") != RECORD_VERSION:
        raise CouldNotAsk("%s is a version %r record; this reads version %d"
                          % (path, record.get("version"), RECORD_VERSION))
    refused = {}
    for label, run in record["runs"].items():
        asked = run.get("asked")
        if asked not in BACKENDS + (INSTALLED,) or (asked != INSTALLED
                                                    and asked != label):
            refused[label] = "filed under %s and asked for %r" % (label, asked)
            continue
        problems = backend_problems(label if asked == INSTALLED else asked,
                                    run.get("answers") or [])
        if asked == INSTALLED and label != backend_of(run.get("answers") or []):
            problems.append("filed under %s, and its answers say %s"
                            % (label, backend_of(run.get("answers") or [])))
        if problems:
            refused[label] = "; ".join(problems)
    return record, refused


def items_from_record(record):
    """The sentence set a record was taken on, shaped as load_sentences()."""
    items = []
    for n, one in enumerate(record["sentences"]["items"], 1):
        kind, want = parse_expect(one["expect"]) or (NEUTRAL, None)
        items.append({"n": n, "say": one["say"], "kind": kind, "want": want,
                      "expect": one["expect"], "groups": list(one["groups"])})
    return items


# ---------------------------------------------------------------------------
# The fast loop - candidate identities for one card, on recorded shortlists
# ---------------------------------------------------------------------------

def load_identities(path):
    """The candidate identities, one per line; blank and # lines skipped."""
    with io.open(path, encoding="utf-8") as handle:
        lines = [_norm(line) for line in handle.read().splitlines()]
    out = []
    for line in lines:
        if line and not line.startswith("#") and line not in out:
            out.append(line)
    return out


def _winner(shortlist_, scores):
    """The capability first after a re-read - heron_retrieve._rerank's order:
    the cross-encoder's score, then the fused score, then the id; anything it
    did not score stays behind everything it did."""
    scored = [(s, e) for s, e in zip(scores, shortlist_) if s is not None]
    if not scored:
        return shortlist_[0].get("capability") if shortlist_ else None
    scored.sort(key=lambda se: (-se[0], -(se[1].get("fused") or 0.0),
                                str(se[1].get("id"))))
    return scored[0][1].get("capability")


def rescore(items, run, card, identities, scorer, rung, threshold):
    """What each candidate identity for `card` would win and take, on the
    shortlists `run` recorded. Never asks the search. Returns a dict.

    `scorer(question, passages)` is heron_rerank.scores in a real run - a
    score per passage, or None. The baseline is re-scored too, so the
    recorded identity and every candidate are judged by the same model.

    WHAT IT CANNOT SEE, SAID RATHER THAN GUESSED. A new identity also moves
    the words and nearness routes, so it can carry a card into a shortlist or
    out of one; this keeps every shortlist as recorded. A sentence whose
    shortlist did not hold the card is out of reach - only a full run says -
    and one answered by an exact phrase never reached the re-ranker at all.
    """
    card_writes = rung(run["capabilities"].get(card)) >= threshold
    # BY ITS WORDS, NOT ITS NUMBER: --sentences beside --from may be a set
    # that grew since the record was taken.
    by_say = dict((_norm(a["say"]).lower(), a) for a in run["answers"])
    skipped = {"identity": 0, "no shortlist": 0, "not scored": 0,
               "not in the record": 0}
    reach = {"wanted": 0, "bad": 0, "neutral": 0}
    blind = 0
    prepared = []
    for item in items:
        answer = by_say.get(item["say"].lower())
        if answer is None:
            skipped["not in the record"] += 1
            continue
        if answer.get("error"):
            skipped["not scored"] += 1
            continue
        if answer.get("route") != "hybrid":
            skipped["identity"] += 1
            continue
        held = answer.get("shortlist")
        if not held:
            skipped["no shortlist"] += 1
            continue
        if item["kind"] == WANT:
            role = "wanted" if item["want"] == card else "bad"
        elif item["kind"] == NOT_A_WRITE and card_writes:
            role = "bad"
        else:
            role = "neutral"
        mine = [k for k, e in enumerate(held) if e.get("capability") == card]
        if not mine:
            reach[role] += 1
            continue
        swap = [k for k in mine if held[k].get("identity")
                and held[k]["identity"] in (held[k].get("passage") or "")]
        blind += len(swap) < len(mine)
        base = scorer(item["say"], [e.get("passage") or "" for e in held])
        if base is None or len(base) != len(held):
            skipped["not scored"] += 1
            continue
        prepared.append((item, answer, held, swap, list(base), role))

    lines = []
    agree = 0
    for identity in [None] + list(identities):
        tally = {"wanted": 0, "won": 0, "bad": 0, "taken": 0, "questions": 0,
                 "neutral": 0, "neutral_taken": 0}
        for item, answer, held, swap, base, role in prepared:
            scores = list(base)
            if identity is not None and swap:
                got = scorer(item["say"], [
                    held[k]["passage"].replace(held[k]["identity"], identity, 1)
                    for k in swap])
                if got is not None and len(got) == len(swap):
                    for k, value in zip(swap, got):
                        scores[k] = value
            won = _winner(held, scores) == card
            if identity is None:
                agree += _winner(held, scores) == answer.get("capability")
            if role == "wanted":
                tally["wanted"] += 1
                tally["won"] += won
            elif role == "bad":
                tally["bad"] += 1
                tally["taken"] += won
                tally["questions"] += won and item["kind"] == NOT_A_WRITE
            else:
                tally["neutral"] += 1
                tally["neutral_taken"] += won
        lines.append((identity, tally))
    return {"lines": lines, "skipped": skipped, "out_of_reach": reach,
            "blind": blind, "read": len(prepared), "agree": agree,
            "card_writes": card_writes}


def report_rescore(label, card, found, took):
    """Print one line per candidate: wanted won, bad taken, neutral taken."""
    out = sys.stdout.write
    sk, reach = found["skipped"], found["out_of_reach"]
    out("\nRE-SCORED OFFLINE FOR %s (%s) on the %s shortlists - %d read, "
        "%.1f s:\n" % (card, "a write" if found["card_writes"] else "not a "
                       "write", label, found["read"], took))
    out("  skipped: %d answered by an exact phrase or remembered (they never "
        "reach the re-ranker),\n  %d with no shortlist, %d not scored, %d not "
        "in the record\n" % (sk["identity"], sk["no shortlist"],
                             sk["not scored"], sk["not in the record"]))
    if any(reach.values()):
        out("  OUT OF REACH - the recorded shortlist did not hold %s: %d wanted, "
            "%d bad, %d neutral.\n  A new identity can carry it in; only a "
            "full run says.\n" % (card, reach["wanted"], reach["bad"],
                                  reach["neutral"]))
    if found["blind"]:
        out("  %d shortlist(s) gave the re-ranker a passage without the card's "
            "identity in it, so no\n  candidate can move them.\n"
            % found["blind"])
    if label.endswith("+rerank") and found["agree"] != found["read"]:
        out("  THE RECORDED IDENTITY, RE-SCORED HERE, PICKS ANOTHER WINNER ON %d "
            "of %d - this cross-encoder\n  is not the one the record was taken "
            "with, so read the lines below against the first.\n"
            % (found["read"] - found["agree"], found["read"]))
    out("\n  %-14s %-16s %-8s %s\n" % ("wanted won", "bad taken", "neutral",
                                      "identity"))
    for identity, t in found["lines"]:
        out("  %-14s %-16s %-8s %s\n"
            % ("%d of %d" % (t["won"], t["wanted"]),
               "%d (%d question%s)" % (t["taken"], t["questions"],
                                       "" if t["questions"] == 1 else "s"),
               "%d" % t["neutral_taken"],
               "(as recorded)" if identity is None else '"%s"' % identity))


def sentences_main(args, rung, threshold):
    """--sentences, --compare, --identities: everything a session's own set
    does. Exit 0 whatever it finds (D-100); 2 when nothing was measured, or a
    record would be written or read on the wrong backend."""
    err = sys.stderr.write
    asking = args.sentences and not args.source
    if args.source and (args.tree or args.out or args.compare
                        or args.shortlists):
        err("COULD NOT RUN: --from re-scores a record and asks nothing, so "
            "--tree, --out, --compare and --shortlists do not go with it\n")
        return 2
    if args.identities and not args.card:
        err("COULD NOT RUN: --identities needs --card, the capability whose "
            "identity they would replace\n")
        return 2
    if args.identities and asking and not args.shortlists:
        err("COULD NOT RUN: --identities re-scores shortlists - take them with "
            "--shortlists, or read a record with --from\n")
        return 2
    if args.record:
        err("COULD NOT RUN: brain/retrieval-history.md holds the owner's and "
            "the plain questions only - a sentence set's record is --out\n")
        return 2
    backends = []
    for name in args.backend or [INSTALLED]:
        for one in (BACKENDS if name == "all" else [name]):
            if one not in BACKENDS + (INSTALLED,):
                err("COULD NOT RUN: --backend %s - one of %s, all or %s\n"
                    % (one, ", ".join(BACKENDS), INSTALLED))
                return 2
            if one not in backends:
                backends.append(one)

    items, unasked = [], {}
    if args.sentences:
        _doc, items, problems = load_sentences(args.sentences)
        if problems:
            err("COULD NOT RUN: the sentence set is not well formed:\n")
            for one in problems:
                err("  %s\n" % one)
            return 2
        if args.list:
            for item in items:
                print("%4d  %-32s %-40s %s" % (item["n"], item["expect"],
                                               _short(", ".join(item["groups"]),
                                                      40), item["say"]))
            return 0

    try:
        if args.source:
            record, refused = load_record(args.source)
            runs = dict((k, v) for k, v in record["runs"].items()
                        if k not in refused
                        and (not args.backend or k in backends))
            for label, why in sorted(refused.items()):
                print("NOT READ - the %s run in %s: %s" % (label, args.source,
                                                           why))
            items = items or items_from_record(record)
            tree = record["tree"]["path"]
        else:
            tree = os.path.realpath(args.tree or ROOT)
            problem = tree_problem(tree)
            if problem:
                err("COULD NOT RUN: %s\n" % problem)
                return 2
            print("Sentences: %d in %s, set %s" % (len(items), args.sentences,
                                                   sentence_fingerprint(items)))
            head, dirty = tree_head(tree)
            print("Tree: %s at %s%s" % (tree, (head or "no git")[:12],
                                        ", WITH CHANGES NOT COMMITTED"
                                        if dirty else ""))
            runs = {}
            for backend in backends:
                sys.stderr.write("  asking %d sentences on %s ...\n"
                                 % (len(items), backend))
                # ONE BACKEND THIS MACHINE HAS NOT GOT IS NOT THE RUN. On a
                # container with no trained model, `all` still measures the
                # two lexical backends, and says which two it could not.
                try:
                    run = ask_tree(tree, backend, items, args.shortlists,
                                   args.revit)
                except CouldNotAsk as why:
                    unasked[backend] = str(why)
                    continue
                label = backend if backend != INSTALLED else backend_of(
                    run["answers"])
                run["capabilities"] = highest_risks(run["capabilities"], rung)
                runs[label] = run
            for backend, why in unasked.items():
                print("\nNOT ASKED ON %s - %s" % (backend.upper(), why))
            if not runs:
                err("COULD NOT RUN: no backend asked for could be asked\n")
                return 2
    except CouldNotAsk as why:
        err("COULD NOT RUN: %s\n" % why)
        return 2

    wrong = {}
    if asking:
        for label, run in runs.items():
            problems = backend_problems(run["asked"], run["answers"])
            report_run(label, run, items, rung, threshold)
            if problems:
                wrong[label] = problems
                print("\n  NOT A RUN ON %s - %s" % (label.upper(),
                                                   "; ".join(problems)))
            else:
                print("\n  Every ranked answer ran on %s." % label)

    if args.compare:
        try:
            earlier, refused = load_record(args.compare)
        except CouldNotAsk as why:
            err("COULD NOT RUN: %s\n" % why)
            return 2
        print("\n" + "=" * 72)
        print("AGAINST %s - tree %s at %s, %s, set %s" % (
            args.compare, earlier["tree"].get("path"),
            (earlier["tree"].get("head") or "no git")[:12],
            earlier.get("date"), earlier["sentences"].get("fingerprint")))
        for label, run in runs.items():
            if label in wrong:
                print("\nNOT COMPARED ON %s - this run is not a run on it"
                      % label.upper())
            elif label in refused:
                print("\nNOT COMPARED ON %s - the earlier run is not a run on "
                      "it: %s" % (label.upper(), refused[label]))
            elif label not in earlier["runs"]:
                print("\nNOT COMPARED ON %s - the earlier record has no run on "
                      "it (it has %s)" % (label.upper(), ", ".join(
                          sorted(earlier["runs"])) or "none"))
            else:
                compare_runs(label, earlier["runs"][label], run, items, rung,
                             threshold)

    if args.identities:
        import heron_rerank as RERANK
        if RERANK.backend()[0] != RERANK.CROSS_ENCODER:
            err("COULD NOT RUN: re-scoring needs a cross-encoder on this "
                "machine, and there is none - %s\n" % RERANK.backend()[1])
            return 2
        identities = load_identities(args.identities)
        if not identities:
            err("COULD NOT RUN: %s holds no identity\n" % args.identities)
            return 2
        for label, run in sorted(runs.items()):
            if label in wrong:
                continue
            if args.card not in run["capabilities"]:
                err("COULD NOT RUN: no card in the %s run provides %s\n"
                    % (label, args.card))
                return 2
            if not run.get("shortlists"):
                print("\nNOT RE-SCORED ON %s - its run recorded no shortlists"
                      % label.upper())
                continue
            started = time.time()
            found = rescore(items, run, args.card, identities, RERANK.scores,
                            rung, threshold)
            report_rescore(label, args.card, found, time.time() - started)

    if args.out:
        if wrong:
            print("\nNOT RECORDED - %s did not run on the backend asked for, and "
                  "a record of it would be\nread as a measurement of that "
                  "backend." % ", ".join(sorted(wrong)))
            return 2
        with io.open(args.out, "w", encoding="utf-8") as handle:
            json.dump(make_record(args.sentences, items, runs, tree, head,
                                  dirty, args.revit),
                      handle, ensure_ascii=False, indent=1)
        print("\nRecorded in %s - %s.%s" % (
            args.out, ", ".join(sorted(runs)),
            " Not on %s: it could not be asked." % ", ".join(sorted(unasked))
            if unasked else ""))

    print("\nExit 0 whatever this finds (D-100).")
    return exit_code(False)


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="Score Heron's search on the owner's own questions.")
    parser.add_argument("--revit", default=None,
                        help="filter to one release, as a connected Revit "
                             "would; default: no filter")
    parser.add_argument("--record", action="store_true",
                        help="append this run's row to the history table")
    parser.add_argument("--list", action="store_true",
                        help="print the answer key and ask nothing")
    parser.add_argument("--list-plain", action="store_true",
                        help="print the plain questions and ask nothing")
    parser.add_argument("--questions", default=QUESTIONS,
                        help=argparse.SUPPRESS)
    parser.add_argument("--plain-questions", default=PLAIN,
                        help=argparse.SUPPRESS)
    parser.add_argument("--history", default=HISTORY, help=argparse.SUPPRESS)
    # A SESSION'S OWN SENTENCES - none of these reach the history tables.
    parser.add_argument("--sentences", default=None,
                        help="a frozen sentence set (yaml: groups, and per "
                             "sentence want <CAPABILITY>, not-a-write or "
                             "neutral); asks it instead of the two frozen sets")
    parser.add_argument("--tree", default=None,
                        help="measure this checkout - such as a detached "
                             "worktree of main - on a store built from its "
                             "cards; default: this one")
    parser.add_argument("--backend", action="append", default=None,
                        help="%s, all, or %s (the default: whatever this "
                             "machine has, named by what answered); repeat "
                             "for several" % (", ".join(BACKENDS), INSTALLED))
    parser.add_argument("--out", default=None,
                        help="write the record: every answer, with the "
                             "backends that produced it")
    parser.add_argument("--compare", default=None,
                        help="an earlier record: list every changed answer, "
                             "tagged want, bad or neutral, per backend")
    parser.add_argument("--shortlists", action="store_true",
                        help="record the shortlist the re-ranker read for "
                             "each sentence - at most its top 20")
    parser.add_argument("--identities", default=None,
                        help="candidate semantic identities, one per line, "
                             "re-scored offline for --card")
    parser.add_argument("--card", default=None,
                        help="the capability whose identity they replace")
    parser.add_argument("--from", dest="source", default=None,
                        help="the record whose shortlists --identities "
                             "re-scores; asks nothing")
    parser.add_argument("--worker", nargs=2, default=None,
                        help=argparse.SUPPRESS)
    args = parser.parse_args(argv)

    if args.worker:
        return _worker(*args.worker)

    try:
        import yaml                                      # noqa: F401
    except ImportError:
        import heron_install as INSTALL
        sys.stderr.write("COULD NOT RUN: this needs PyYAML - in the Heron folder: "
                         "%s\n" % INSTALL.install_line("pip install --user pyyaml"))
        return 2

    if args.sentences or args.source:
        _risk_name, threshold, ladder = _sibling(
            "generate-jobs.py").write_threshold()
        return sentences_main(args, lambda risk: ladder.get(
            (risk or "").upper(), -1), threshold)
    if (args.tree or args.backend or args.out or args.compare
            or args.shortlists or args.identities or args.card):
        sys.stderr.write("COULD NOT RUN: --tree, --backend, --out, --compare, "
                         "--shortlists, --identities and --card measure a "
                         "sentence set - name one with --sentences, or a "
                         "record with --from\n")
        return 2

    doc, rows, problems = load_key(args.questions)
    if problems:
        sys.stderr.write("COULD NOT RUN: the answer key is not well formed:\n")
        for one in problems:
            sys.stderr.write("  %s\n" % one)
        return 2
    kinds = [row["kind"] for row in rows]

    if args.list:
        for row in rows:
            print("%3d  %-10s %-28s %s" % (row["n"], row["kind"],
                                           row.get("answer") or "-",
                                           " ".join(row["question"].split())))
        return 0

    _plain_doc, plain_rows, plain_problems = load_plain(args.plain_questions)
    if plain_problems:
        sys.stderr.write("COULD NOT RUN: the plain questions are not well "
                         "formed:\n")
        for one in plain_problems:
            sys.stderr.write("  %s\n" % one)
        return 2
    plain_kind = plain_kinds(plain_rows)

    if args.list_plain:
        for row in plain_rows:
            print("%3d  %-4s %-12s %-30s %s"
                  % (row["n"], row.get("row") or "-", row.get("named") or "-",
                     " / ".join(row.get("answer") or []) or "-",
                     " ".join(row["question"].split())))
        return 0

    import heron_brain as brain
    import heron_scope as SCOPE
    import heron_tools

    # NO STORE, NO SCORE - AND ONE LINE RATHER THAN A TRACEBACK. The same
    # refusal check-routing.py makes, for the same reason: nothing would be
    # measured, and exit 2 is how this repository says so.
    if SCOPE.knowledge_dir() is None:
        sys.stderr.write(
            "COULD NOT RUN: no HERON_DATA, no Windows profile and no HERON_KNOWLEDGE, so there is\n"
            "nowhere to keep a knowledge store. Set HERON_KNOWLEDGE to a\n"
            "folder - an empty one is enough - and run this again.\n")
        return 2

    routing = _sibling("check-routing.py")
    skill_routing = _sibling("check-skill-routing.py")
    _risk_name, threshold, ladder = _sibling("generate-jobs.py").write_threshold()

    def rung(risk):
        return ladder.get((risk or "").upper(), -1)

    # THE STORE MUST HOLD THIS TREE'S LIBRARY, or every number below is about
    # somebody else's - check-routing.py's own guard, CALLED, NOT COPIED. Until
    # 2026-10-09 this file carried the ids half of it: no row's content was
    # compared with its card (row 5b-229), and the ONE store every chat on the
    # PC reads was rebuilt from whatever checkout this ran in (row 5b-233).
    # store_for_this_tree() asks all three and says why when it refuses; a
    # refusal is exit 2, which this tool already meant as "nothing was scored".
    store = routing.store_for_this_tree("score")
    if store is None:
        return 2

    capabilities = {}
    for r in store.fragments():
        if r["capability"]:
            now = capabilities.get(r["capability"])
            if now is None or rung(r.get("risk")) > rung(now):
                capabilities[r["capability"]] = r.get("risk")
    fragments = store.count()
    store.close()

    names = dict((v, k) for k, v in ladder.items())
    tools = dict((name, names.get(spec[0], "?"))
                 for name, spec in heron_tools.TOOLS.items())
    skills = load_skills()

    bad = unknown_answers(rows, capabilities, skills, tools)
    for n, why in bad:
        print("  ANSWER KEY #%s: %s. The row is not scored, and the owner "
              "decides the new answer." % (n, why))
    plain_bad = unknown_plain(plain_rows, capabilities)
    for n, why in plain_bad:
        print("  PLAIN QUESTION #%s: %s. The row is not scored." % (n, why))

    # BUILT BEFORE THE FINGERPRINT, as check-risk-crossings.py has been since
    # 2026-10-17 - row 5b-980. store_for_this_tree() rebuilds the fragments
    # table alone, and the search tables are made by the first lookup's open,
    # so on a fresh scratch store the block below said "COULD NOT BE READ"
    # and its md5 was of a store not yet indexed. The same preparation every
    # lookup makes, once, before any question is asked.
    try:
        with brain._Open():
            pass
    except brain.BrainUnavailable as why:
        sys.stderr.write("COULD NOT RUN: %s\n" % why)
        return 2

    # READ BEFORE ASKING, as check-risk-crossings.py does: asking can move the
    # store, and a fingerprint taken afterwards describes a different one.
    index = skill_routing._index_fingerprint()

    def progress(i, total):
        sys.stderr.write("  asked %d of %d\n" % (i, total))

    started = time.time()
    with _AuditElsewhere():
        try:
            results = measure(rows, brain.lookup, capabilities, skills, tools,
                              rung, threshold, revit=args.revit,
                              bad=set(n for n, _ in bad), progress=progress)
            plain = measure_plain(plain_rows, brain.lookup, rung, threshold,
                                  revit=args.revit,
                                  bad=set(n for n, _ in plain_bad),
                                  progress=progress)
        except brain.BrainUnavailable as why:
            sys.stderr.write("COULD NOT RUN: %s\n" % why)
            return 2
    took = time.time() - started

    summary = summarise(results)
    codes = "".join(code_of(r) for r in results)
    stamp = {"date": datetime.date.today().isoformat(), "fragments": fragments,
             "backend": backend_of(results), "revit": args.revit,
             "key": key_fingerprint(rows)}

    confirmed = doc.get("confirmed") or {}
    print("Answer key: %d questions, key %s, confirmed by %s on %s"
          % (len(rows), stamp["key"], confirmed.get("by", "nobody recorded"),
             confirmed.get("date", "no date")))
    print("Library: %d fragments   backend: %s   Revit: %s   %s   %.0f s"
          % (fragments, stamp["backend"], args.revit or "any (no version "
             "filter - no Revit connected)", stamp["date"], took))
    print("\nTHE INDEX THIS RAN AGAINST - compare it before comparing counts:")
    for line in skill_routing.fingerprint_lines(index):
        print(line)

    report(results, summary, stamp, rows)
    drop = compare(args.history, stamp, codes, summary, kinds, rows)

    plain_summary = summarise_plain(plain)
    plain_codes = "".join(plain_code(r) for r in plain)
    plain_stamp = dict(stamp, backend=backend_of(plain),
                       key=plain_fingerprint(plain_rows))
    print("\n" + "=" * 72)
    print("Plain questions: %d, set %s, frozen from FRAGMENT-ISSUES %s -\n"
          "never mixed with his. Backend: %s"
          % (len(plain_rows), plain_stamp["key"], plain_sources(plain_rows),
             plain_stamp["backend"]))
    report_plain(plain, plain_summary)
    plain_drop = compare_plain(args.history, plain_stamp, plain_codes,
                               plain_summary, plain_kind, plain_rows)

    line = history_row(stamp, summary, codes)
    plain_line = plain_history_row(plain_stamp, plain_summary, plain_codes)
    if args.record:
        holes = len(bad) + summary["errors"] + plain_summary["errors"]
        if holes:
            print("\nNOT RECORDED - %d row(s) could not be scored, and a row "
                  "with holes in it would be\ncompared with later runs as "
                  "though it were whole." % holes)
            return 2
        # BOTH TABLES OR NEITHER. Checked before either row goes in, so a
        # missing plain table cannot leave his row recorded and theirs not.
        with io.open(args.history, encoding="utf-8") as handle:
            held = handle.read()
        for header, columns in ((TABLE_HEADER, COLUMNS),
                                (PLAIN_HEADER, PLAIN_COLUMNS)):
            if history_rows(held, header, columns) is None:
                print("\nNOT RECORDED - %s has no table headed\n  %s"
                      % (args.history, header))
                return 2
        for header, one in ((TABLE_HEADER, line), (PLAIN_HEADER, plain_line)):
            append_row(args.history, one, header)
            print("\nRecorded in %s:\n%s"
                  % (os.path.relpath(args.history, ROOT), one))
    else:
        print("\nThe rows --record would append:\n%s\n%s" % (line, plain_line))

    print("\nExit 0 whatever this finds (D-100). A wrong answer here is a "
          "finding to read, and never\na reason to reword a question, change "
          "an answer or weaken an utterance to buy it back.")
    return exit_code(drop or plain_drop)


if __name__ == "__main__":
    sys.exit(main())
