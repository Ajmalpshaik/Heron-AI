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

It reports and exits 0 whatever it finds, like every routing check here.
Exit 2 means it could not do its job: nothing was scored, which is not a pass.
Whether a drop should ever stop a pull request is the owner's decision (D3 of
the earlier-brain plan), and it has not been taken.

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
import shutil
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


def history_rows(text):
    """Every row of the table, oldest first, as dicts - or None if no table.

    None and [] are different answers and are kept apart (D-52): no table
    means the file lost its section, an empty table means nothing was ever
    recorded.
    """
    lines = text.splitlines()
    if TABLE_HEADER not in lines:
        return None
    rows = []
    for line in lines[lines.index(TABLE_HEADER) + 2:]:
        if not line.startswith("|"):
            break
        cells = [c.strip().strip("`") for c in line.strip().strip("|").split("|")]
        if len(cells) == len(COLUMNS):
            rows.append(dict(zip(COLUMNS, cells)))
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
    if char == ERROR:
        return None
    if kind in PLACED:
        return 99 if char == NOT_FOUND else int(char)
    return 1 if char == LANDED_CHANGE else 0


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


def append_row(path, line):
    """Add one row at the end of the table. False if the table is not there."""
    with io.open(path, encoding="utf-8") as handle:
        lines = handle.read().split("\n")
    if TABLE_HEADER not in lines:
        return False
    at = lines.index(TABLE_HEADER) + 2
    while at < len(lines) and lines[at].startswith("|"):
        at += 1
    lines.insert(at, line)
    with io.open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write("\n".join(lines))
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
    """
    near, rerank = {}, set()
    for r in results:
        if r.get("error") or r.get("route") != "hybrid":
            continue
        said = (r.get("backends") or {}).get("nearness") or "not said"
        near[said] = near.get(said, 0) + 1
        again = (r.get("backends") or {}).get("rerank")
        if again and again != "not used":
            rerank.add(again)
    if not near:
        name = "none ranked"
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
        out("\n  THE SCORE DROPPED. Reported, not enforced: whether a drop "
            "stops a pull request is\n  the owner's decision, and today it "
            "does not.\n")
    return drop


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
    parser.add_argument("--questions", default=QUESTIONS,
                        help=argparse.SUPPRESS)
    parser.add_argument("--history", default=HISTORY, help=argparse.SUPPRESS)
    args = parser.parse_args(argv)

    try:
        import yaml                                      # noqa: F401
    except ImportError:
        sys.stderr.write("COULD NOT RUN: this needs PyYAML - "
                         "pip install --user pyyaml\n")
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

    import heron_brain as brain
    import heron_scope as SCOPE
    import heron_tools

    # NO STORE, NO SCORE - AND ONE LINE RATHER THAN A TRACEBACK. The same
    # refusal check-routing.py makes, for the same reason: nothing would be
    # measured, and exit 2 is how this repository says so.
    if SCOPE.knowledge_dir() is None:
        sys.stderr.write(
            "COULD NOT RUN: no %APPDATA% and no HERON_KNOWLEDGE, so there is\n"
            "nowhere to keep a knowledge store. Set HERON_KNOWLEDGE to a\n"
            "folder - an empty one is enough - and run this again.\n")
        return 2

    routing = _sibling("check-routing.py")
    skill_routing = _sibling("check-skill-routing.py")
    _risk_name, threshold, ladder = _sibling("generate-jobs.py").write_threshold()

    def rung(risk):
        return ladder.get((risk or "").upper(), -1)

    # THE STORE MUST HOLD THIS TREE'S LIBRARY, or every number below is about
    # somebody else's - check-routing.py's own rule, and its own functions.
    disk = routing.ids_on_disk()
    store = SCOPE.open_scope(SCOPE.GLOBAL)
    held = set(r["id"] for r in store.fragments())
    if held != disk:
        drift = routing.describe_drift(held, disk)
        store.close()
        built, _problems = SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        held = set(r["id"] for r in store.fragments())
        print("  (store did not match this working tree - %s; rebuilt %d)"
              % (drift, built))
        if held != disk:
            print("  the store STILL does not match after a rebuild - %s. "
                  "Nothing was scored." % routing.describe_drift(held, disk))
            store.close()
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
    compare(args.history, stamp, codes, summary, kinds, rows)

    line = history_row(stamp, summary, codes)
    if args.record:
        if bad or summary["errors"]:
            print("\nNOT RECORDED - %d row(s) could not be scored, and a row "
                  "with holes in it would be\ncompared with later runs as "
                  "though it were whole." % (len(bad) + summary["errors"]))
            return 2
        if not append_row(args.history, line):
            print("\nNOT RECORDED - %s has no table headed\n  %s"
                  % (args.history, TABLE_HEADER))
            return 2
        print("\nRecorded in %s:\n%s" % (os.path.relpath(args.history, ROOT),
                                         line))
    else:
        print("\nThe row --record would append:\n%s" % line)

    print("\nExit 0 whatever this finds. A wrong answer here is a finding to "
          "read, and never a reason\nto reword a question, change an answer "
          "or weaken an utterance to buy it back.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
