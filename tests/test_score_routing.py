#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The score of the owner's own questions, and a drop planted to prove it is seen.

    python tests/test_score_routing.py

`tools/score-routing.py` asks the 79 questions in tests/data/owner-questions.yaml
through `heron_brain.lookup` - the function `heron_lookup` calls - and scores
where the owner's confirmed answer came back. A score is only worth having if
a FALL in it is noticed, so the claim this suite exists for is that one:

    PLANT A DROP AND THE SCORER CATCHES IT.

It is planted twice. Once in the arithmetic, where a question that was first
is made to vanish and a tool or gap row is made to land on a change. And once
for real, through the real search: a private knowledge store is built, two of
his questions are asked, and then the capability that answered each is
RETIRED in that private store - DEPRECATED, which the search never offers -
and asked again. The scorer has to see both fall, and say so.

THE FIXTURE KEY IN SECTION 5 IS NOT HIS KEY, AND THAT IS DELIBERATE. Its
answers are whatever the search put first on the first pass, read at test
time - so the drop is certain whatever the library looks like the day this
runs, and nothing here asserts today's ranking, which is a sample (row 116).
HIS answer key is never derived from the search: section 1 checks it only
against the library, and pins the wording of his questions.

WHAT IT DOES NOT DO: it never runs the full 79 - that takes minutes and is the
tool's job. Whether a drop stops a pull request is not this suite's call
either: the owner decided it never does (D-100), and section 4 holds the
tool to that.

Exit 0 passed, 1 failed, 3 could not run for want of PyYAML.
"""

import contextlib
import hashlib
import importlib.util
import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "score-routing.py")
KEY = os.path.join(ROOT, "tests", "data", "owner-questions.yaml")
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

# THE OWNER'S WORDS, PINNED. sha256 of the 79 questions joined by newlines,
# whitespace collapsed. A question is never reworded to suit the search
# (FRAGMENT-ISSUES row 113, D-34); if one must change, it is on his word,
# recorded in the key's `corrections`, and this pin moves in the same change.
QUESTIONS_PIN = "ba1e6f01ddecbc9d78d767cd116f1ae1a39d9826cd1818b6b168fd9493733bf6"

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has a hyphen in it."""
    try:
        spec = importlib.util.spec_from_file_location("score_routing", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                                 # noqa: BLE001
        return None


def finish():
    print()
    if FAILURES:
        print("FAILED - %d:" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASSED")
    return 0


def result(tool, n, kind, place=None, got="X", risk="READ", asked=False,
           route="hybrid", error=None):
    """One scored row, shaped the way `score_one` shapes it."""
    return {"n": n, "kind": kind, "answer": "A%d" % n, "question": "q%d" % n,
            "route": route, "got": got, "got_risk": risk, "order": [],
            "place": place, "reached": None,
            "writes": risk in ("MODIFY", "PUBLISH", "ADMIN"),
            "expected_risk": None if kind == "gap" else "READ",
            "asked_change": None if kind == "gap" else asked, "error": error}


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    try:
        import yaml
    except ImportError:
        print("COULD NOT RUN: this needs PyYAML - pip install --user pyyaml")
        return 3

    tool = load()
    check(tool is not None, "tools/score-routing.py loads")
    if tool is None:
        return finish()

    # ------------------------------------------------------------------
    print()
    print("1. HIS answer key - checked against the library, never against the search")
    doc, rows, problems = tool.load_key(KEY)
    check(not problems, "the key is well formed%s"
          % ("" if not problems else ": " + "; ".join(problems[:3])))
    check(len(rows) == 79 and [r["n"] for r in rows] == list(range(1, 80)),
          "79 questions, numbered 1 to 79 in order")
    blob = "\n".join(" ".join(r["question"].split()) for r in rows)
    check(hashlib.sha256(blob.encode("utf-8")).hexdigest() == QUESTIONS_PIN,
          "the questions are his words, unchanged - a reworded question fails "
          "here, and moves only on his word")
    confirmed = doc.get("confirmed") or {}
    check(confirmed.get("by") and confirmed.get("date") and confirmed.get("said"),
          "it records who confirmed it, when, and what he said")

    capabilities = set()
    for name in os.listdir(os.path.join(ROOT, "brain", "fragments")):
        path = os.path.join(ROOT, "brain", "fragments", name, "fragment.yaml")
        if os.path.exists(path):
            with io.open(path, encoding="utf-8") as handle:
                capabilities.add((yaml.safe_load(handle) or {}).get("capability"))
    skills = tool.load_skills()
    import heron_tools
    unknown = tool.unknown_answers(rows, capabilities, skills, heron_tools.TOOLS)
    check(not unknown, "every answer names a capability, skill or tool Heron "
          "has%s" % ("" if not unknown else ": " + "; ".join(
              "#%s %s" % u for u in unknown[:3])))
    kinds = [r["kind"] for r in rows]
    check(all(r.get("answer") is None for r in rows if r["kind"] == "gap"),
          "a gap names no answer - what exists is only a note")
    check(set(kinds) <= set(tool.KINDS), "every kind is one of %s"
          % ", ".join(tool.KINDS))

    # ------------------------------------------------------------------
    print()
    print("2. The key's fingerprint moves with any answer, so a correction is "
          "never read as a movement")
    same = tool.key_fingerprint(rows)
    check(same == tool.key_fingerprint([dict(r) for r in rows]),
          "the same key gives the same fingerprint")
    changed = [dict(r) for r in rows]
    changed[40]["answer"] = "SOMETHING_ELSE"
    check(tool.key_fingerprint(changed) != same,
          "one answer changed moves it")
    reworded = [dict(r) for r in rows]
    reworded[0]["question"] = reworded[0]["question"] + " please"
    check(tool.key_fingerprint(reworded) != same, "one question changed moves it")

    # ------------------------------------------------------------------
    print()
    print("3. Where a row lands, and the one class that is not a judgement call")
    check(tool.shortlist({"capability": "A", "candidates": [
        {"capability": "A"}, {"capability": "B"}, {"capability": "A"},
        {"capability": "C"}]}) == ["A", "B", "C"],
        "the shortlist is the winner, then each other capability once - the "
        "way heron_lookup prints it")
    check(tool.shortlist({"route": "identity", "capability": "A",
                          "candidates": []}) == ["A"],
          "an exact phrase shows one capability and no others")
    check(tool.place(["A", "B", "C"], {"C", "Z"}) == (3, "C"),
          "a place is the first wanted name in that order")
    check(tool.place(["A", "B"], {"Z"}) == (None, None), "absent is None")

    handed = result(tool, 1, "capability", place=None, risk="MODIFY",
                    asked=False)
    check(tool.handed_a_change(handed),
          "a row whose answer changes nothing, answered by a MODIFY, is D-86's "
          "harm")
    check(not tool.handed_a_change(result(tool, 2, "capability", place=1,
                                          risk="MODIFY", asked=True)),
          "a change asked for and given is not")
    check(tool.wrong_change(result(tool, 3, "capability", place=None,
                                   risk="MODIFY", asked=True)),
          "a change asked for and a DIFFERENT one given is listed on its own")
    gap_write = result(tool, 4, "gap", risk="MODIFY")
    check(tool.gap_onto_a_change(gap_write) and not tool.handed_a_change(gap_write),
          "a gap row on a change is its own list - a gap has no answer to "
          "say whether a change was asked for")

    results = [result(tool, 1, "capability", place=1, route="identity"),
               result(tool, 2, "capability", place=2),
               result(tool, 3, "capability", place=3),
               result(tool, 4, "capability", place=4),
               result(tool, 5, "capability", place=5),
               result(tool, 6, "capability", place=None, risk="MODIFY"),
               result(tool, 7, "skill", place=1),
               result(tool, 8, "tool", risk="MODIFY"),
               result(tool, 9, "gap", risk="READ")]
    summary = tool.summarise(results)
    check((summary["first"], summary["second_third"], summary["low"],
           summary["not_found"]) == (1, 2, 2, 1),
          "1st, 2nd-3rd, 4th-5th and not found are counted apart")
    check(summary["capability_rows"] == 6 and summary["skill_rows"] == 1,
          "the four count capability rows only - a skill row is kept out")
    check(summary["exact"] == 1 and summary["exact_right"] == 1,
          "an exact declared phrase is counted separately")
    check(summary["handed"] == 2,
          "a READ row answered by a MODIFY and a READ tool answered by a "
          "MODIFY are both handed a change")
    codes = "".join(tool.code_of(r) for r in results)
    check(codes == "12345-1w.", "one character per row: %r" % codes)

    # ------------------------------------------------------------------
    print()
    print("4. A DROP PLANTED IN THE ARITHMETIC IS CAUGHT")
    stamp = {"date": "2026-01-01", "fragments": 400, "backend": "lexical",
             "revit": None, "key": "abcd1234"}
    kinds9 = [r["kind"] for r in results]
    line = tool.history_row(stamp, summary, codes)
    table = "\n".join(["# a history file", "", tool.TABLE_HEADER,
                       "|" + "---|" * len(tool.COLUMNS), line, "",
                       "text after the table"])
    parsed = tool.history_rows(table)
    check(parsed is not None and len(parsed) == 1
          and parsed[0]["Per question"] == codes
          and parsed[0]["Answer key"] == "abcd1234",
          "a written row reads back as it was written")
    check(tool.history_rows("# no table here") is None,
          "no table is None, which is not the same as an empty one")

    planted = "-2345-1ww"            # #1 fell from first; #9 now a change
    moves = tool.moved(codes, planted, kinds9)
    check([(m[0], m[3]) for m in moves] == [(0, "down"), (8, "down")],
          "both planted falls are named, and nothing else: %s" % moves)
    check(tool.dropped(moves, parsed[0], summary),
          "and it is called a drop")
    better = "1234211w."
    check([m[3] for m in tool.moved(codes, better, kinds9)] == ["up", "up"],
          "a question climbing is an UP, never a drop")
    check(not tool.dropped(tool.moved(codes, codes, kinds9), parsed[0], summary),
          "the same codes are no drop")
    more_handed = dict(summary, handed=summary["handed"] + 1)
    check(tool.dropped([], parsed[0], more_handed),
          "one more question handed a change is a drop even if no place moved")
    check([m[3] for m in tool.moved("1", "?", ["capability"])] == ["unreadable"],
          "a row that could not be scored is never read as a movement")
    # A HAND-EDITED ROW MUST NOT TAKE THE COMPARISON DOWN. The per-question
    # string comes back out of a markdown file a person can edit, so a
    # character this tool never writes is unreadable - never a crash, and
    # never read as "no change" on a tool or gap row.
    try:
        odd = [m[3] for m in tool.moved("1.w", "x?z", ["capability", "gap",
                                                       "tool"])]
    except (ValueError, KeyError, TypeError) as why:
        odd = "raised %s" % type(why).__name__
    check(odd == ["unreadable"] * 3,
          "a character this tool never writes is unreadable, not a crash: %s"
          % (odd,))

    # D-100. The owner decided on 2026-09-23 that a drop is REPORTED and
    # never stops a pull request. Asked by name first, so the check fails
    # cleanly on a tool that has no word for it (heron-ship section 2a).
    decided = getattr(tool, "exit_code", None)
    check(callable(decided), "the exit code is made in one place, exit_code()")
    if callable(decided):
        check(decided(True) == 0 and decided(False) == 0,
              "a drop exits 0 - reported, never a failure (D-100)")

    other = dict(stamp, backend="model")
    check(tool.last_comparable(parsed, other) is None,
          "a run on another backend is not comparable")
    check(tool.last_comparable(parsed, dict(stamp, key="ffff0000")) is None,
          "nor a run of another answer key")
    check(tool.last_comparable(parsed, dict(stamp, fragments=401)) is not None,
          "but a bigger library IS comparable - that is the drop it is for")

    work = tempfile.mkdtemp(prefix="heron-score-test-")
    try:
        history = os.path.join(work, "history.md")
        with io.open(history, "w", encoding="utf-8") as handle:
            handle.write(table)
        spoken = io.StringIO()
        with contextlib.redirect_stdout(spoken):
            said_drop = tool.compare(history, dict(stamp, fragments=401),
                                     planted, tool.summarise(results),
                                     kinds9, [{"n": r["n"], "question":
                                               "question %d" % r["n"]}
                                              for r in results])
        text = spoken.getvalue()
        check(said_drop and "THE SCORE DROPPED" in text,
              "compare() says THE SCORE DROPPED")
        check("#1 " in text and "#9 " in text and "question 1" in text,
              "and names the questions that fell")
        check("400 -> 401" in text,
              "and says the library changed size between the two runs")

        check(tool.append_row(history, "| new row |"),
              "a row is appended to the table")
        with io.open(history, encoding="utf-8") as handle:
            after = handle.read().split("\n")
        check(after.index("| new row |") == after.index(line) + 1
              and after[-1] == "text after the table",
              "right after the last row, and nothing after the table moves")
        # A WINDOWS CHECKOUT HOLDS CRLF. The table must still be found, and
        # the file must keep its own line endings when a row goes in.
        windows = os.path.join(work, "windows.md")
        with io.open(windows, "w", encoding="utf-8", newline="") as handle:
            handle.write(table.replace("\n", "\r\n"))
        check(tool.append_row(windows, "| crlf row |"),
              "a CRLF file's table is found, and the row goes in")
        with io.open(windows, "rb") as handle:
            raw = handle.read()
        check(b"| crlf row |\r\n" in raw and b"\n" not in raw.replace(
            b"\r\n", b""), "and every line still ends CRLF")
        bare = os.path.join(work, "bare.md")
        with io.open(bare, "w", encoding="utf-8") as handle:
            handle.write("# nothing\n")
        check(not tool.append_row(bare, "| x |"),
              "and a file with no table is refused rather than written to")
    finally:
        shutil.rmtree(work, ignore_errors=True)

    # ------------------------------------------------------------------
    print()
    print("5. A DROP PLANTED IN A REAL STORE, THROUGH THE REAL SEARCH, IS CAUGHT")
    saved = dict((k, os.environ.get(k)) for k in ("HERON_KNOWLEDGE",
                                                   "HERON_AUDIT"))
    private = tempfile.mkdtemp(prefix="heron-score-kb-")
    trail = tempfile.mkdtemp(prefix="heron-score-trail-")
    try:
        os.environ["HERON_KNOWLEDGE"] = private
        os.environ["HERON_AUDIT"] = trail
        import heron_scope as SCOPE
        import heron_brain as brain

        built, _problems = SCOPE.rebuild()
        check(built > 0, "a private store is built (%d fragments)" % built)

        audit_folder = []
        with tool._AuditElsewhere() as folder:
            audit_folder.append(folder)
            check(os.environ.get("HERON_AUDIT") == folder != trail,
                  "during a run the trail goes to a throwaway folder")
        check(os.environ.get("HERON_AUDIT") == trail
              and not os.path.exists(audit_folder[0]),
              "and afterwards the owner's own setting is back and the "
              "throwaway folder is gone")

        # Two of HIS questions, asked for real. The answers in THIS key are
        # read off the first pass, so each is first by construction.
        asked = [dict(r) for r in rows if r["n"] in (10, 59)]
        _name, threshold, ladder = tool._sibling(
            "generate-jobs.py").write_threshold()

        def rung(risk):
            return ladder.get((risk or "").upper(), -1)

        store = SCOPE.open_scope(SCOPE.GLOBAL)
        risk_of = {}
        for r in store.fragments():
            risk_of[r["capability"]] = r["risk"]
        store.close()

        with tool._AuditElsewhere():
            first_pass = [brain.lookup(r["question"]) for r in asked]
        for row, found in zip(asked, first_pass):
            row["kind"], row["answer"] = "capability", found.get("capability")
        check(all(r["answer"] for r in asked),
              "the search named a capability for both questions")

        with tool._AuditElsewhere():
            before = tool.measure(asked, brain.lookup, risk_of, {}, {}, rung,
                                  threshold)
        before_codes = "".join(tool.code_of(r) for r in before)
        check(before_codes == "11", "both are first before the drop: %r"
              % before_codes)

        # THE PLANT: every fragment providing either answer is RETIRED in the
        # private store. The ids stay, so nothing detects drift and rebuilds;
        # the search simply may not offer a DEPRECATED fragment.
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        for r in asked:
            store.execute("UPDATE fragments SET status = 'DEPRECATED' "
                          "WHERE capability = ?", (r["answer"],))
        store.db.commit()
        store.close()

        with tool._AuditElsewhere():
            after = tool.measure(asked, brain.lookup, risk_of, {}, {}, rung,
                                 threshold)
        after_codes = "".join(tool.code_of(r) for r in after)
        check("1" not in after_codes,
              "after the plant neither is first: %r" % after_codes)
        falls = tool.moved(before_codes, after_codes, ["capability"] * 2)
        check(len(falls) == 2 and all(m[3] == "down" for m in falls),
              "the scorer names both falls as DOWN")
        row = {"Handed a change": "0"}
        check(tool.dropped(falls, row, tool.summarise(after)),
              "and calls it a drop")
        check(not os.listdir(trail),
              "and not one of those lookups reached the audit trail")
    finally:
        for key, value in saved.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value
        shutil.rmtree(private, ignore_errors=True)
        shutil.rmtree(trail, ignore_errors=True)

    # ------------------------------------------------------------------
    print()
    print("6. It refuses rather than scoring nothing")
    quiet = io.StringIO()
    held = dict((k, os.environ.pop(k, None)) for k in ("HERON_KNOWLEDGE",
                                                        "APPDATA"))
    try:
        with contextlib.redirect_stderr(quiet), contextlib.redirect_stdout(quiet):
            code = tool.main([])
        check(code == 2, "no knowledge store anywhere is exit 2, not a score")
    finally:
        for key, value in held.items():
            if value is not None:
                os.environ[key] = value
    broken = tempfile.mkdtemp(prefix="heron-score-key-")
    try:
        bad = os.path.join(broken, "key.yaml")
        with io.open(bad, "w", encoding="utf-8") as handle:
            handle.write("questions:\n  - n: 1\n    question: ask\n"
                         "    kind: capability\n  - n: 3\n    question: ask\n"
                         "    kind: gap\n    answer: SOMETHING\n")
        with contextlib.redirect_stderr(quiet), contextlib.redirect_stdout(quiet):
            code = tool.main(["--questions", bad, "--list"])
        check(code == 2, "a malformed key is exit 2 - no answer, a number out "
              "of order, a repeated question, a gap with an answer")
        _d, _r, shape = tool.load_key(bad)
        check(len(shape) == 4, "and all four faults are named: %s" % shape)
    finally:
        shutil.rmtree(broken, ignore_errors=True)
    listed = io.StringIO()
    with contextlib.redirect_stdout(listed):
        code = tool.main(["--list"])
    check(code == 0 and len(listed.getvalue().strip().splitlines()) == 79,
          "--list prints his 79 questions and asks nothing")

    return finish()


if __name__ == "__main__":
    sys.exit(main())
