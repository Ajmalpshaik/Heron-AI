# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-QA-BIM-011
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A QA check never passes on missing data (D-165) - the fourth answer, NO DATA,
and the coverage line beside every pass count.

    python tests/test_checklist_no_data.py

WHAT IT PROVES
  1. ALL DATA THERE: every check is decided - PASS or FAIL - and the coverage
     line says so: every check decided, none with no data, and a check that
     names its coverage outputs says how many elements it checked of how many.
  2. SOME DATA MISSING: an output that came back absent, empty or not a number
     is NO DATA, never NOT RUN; a rule that holds while the fragment's own
     count of elements it could not read is above nought is NO DATA, not a
     pass - 6 checked of 43 is never "every duct has a System Name"; a rule
     that FAILS stays a FAIL, because a failure found is real; and the counts
     and the coverage line keep NO DATA out of both the passes and the fails.
  3. ALL DATA MISSING: nothing decided, nothing passed, nothing failed, and
     the sentence says a check with no data is not a pass - in words that
     never call the model compliant.
  4. A CHECK'S COVERAGE OUTPUTS are checked when saved against the card - one
     the fragment does not leave, or one that is not a number, is refused by
     name - kept when good, said in the check's line, and a hand-edited file
     whose coverage output is not text is listed as one that could not be read.
  5. NO DATA IS NEVER A CHANGE: against the last run it is NOT COMPARED, never
     FIXED or NEW.

Each section runs inside its own guard and reads the answer through .get, so
on the code from before D-165 every check fails by name rather than one
traceback ending the suite - seen 2026-11-16: 32 failures, none raised.
"""

from __future__ import print_function

import json
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_checklists as Q                                  # noqa: E402

FAILURES = []
BANNED = re.compile(r"\b(compliant|compliance|clean|healthy)\b", re.I)


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


# What the server would read off a card that counts what it looked at and
# what it could not read - the shape CHECK_MODEL_STANDARDS-like cards declare.
CONTRACTS = {
    "CHECK_DUCT_NAMES": {
        "risk": "READ",
        "needs": [{"name": "doc", "type": "Document"}],
        "provides": [{"name": "failures", "type": "int"},
                     {"name": "offenders", "type": "IList<ElementId>"},
                     {"name": "scanned", "type": "int", "role": "accounting"},
                     {"name": "notChecked", "type": "int", "role": "accounting"},
                     {"name": "report", "type": "string"}],
        "required": [], "selection": []},
}


def checklist():
    """Four checks, each on what the stand-in Revit below sends back."""
    return {"name": "Before issue", "checks": [
        {"sentence": "every duct has a System Name", "capability": "CHECK_DUCT_NAMES",
         "values": [], "output": "failures", "op": "==", "target": 0, "ids": "offenders",
         "idsKind": "list", "missing": "notChecked", "applicable": "scanned"},
        {"sentence": "every linked model is loaded", "capability": "LIST_LINKED_MODELS",
         "values": [], "output": "notLoaded", "op": "==", "target": 0},
        {"sentence": "no room is left unenclosed", "capability": "SELECT_UNENCLOSED_ROOMS",
         "values": [], "output": "unenclosed", "op": "==", "target": 0},
        {"sentence": "every pipe has a System Name", "capability": "CHECK_PIPE_NAMES",
         "values": [], "output": "failures", "op": "==", "target": 0,
         "missing": "notChecked", "applicable": "scanned"},
    ]}


def ask_with(replies):
    """A stand-in for the server's ask: one reply a capability, all read."""
    def ask(check_):
        return {"ran": True, "document": "Tower MEP",
                "provides": replies[check_["capability"]]}
    return ask


def counted(answer, key):
    return (answer.get("counts") or {}).get(key)


def coverage(answer):
    return answer.get("coverage") or {}


def all_data():
    print("1. every check has its data: each one decided")
    answer = Q.run(checklist(), ask_with({
        "CHECK_DUCT_NAMES": {"failures": "0", "scanned": "43", "notChecked": "0"},
        "LIST_LINKED_MODELS": {"notLoaded": "0 item(s)"},
        "SELECT_UNENCLOSED_ROOMS": {"unenclosed": "2 item(s) [7, 8]"},
        "CHECK_PIPE_NAMES": {"failures": "0", "scanned": "12", "notChecked": "0"}}))
    states = [r["state"] for r in answer["rows"]]
    check(states == ["pass", "pass", "fail", "pass"], "PASS, PASS, FAIL, PASS as read - found %s" % states)
    check(counted(answer, "missing") == 0, "no check is NO DATA")
    cov = coverage(answer)
    check(cov.get("checked") == 4 and cov.get("applicable") == 4 and cov.get("missing") == 0
          and cov.get("notRun") == 0, "coverage: 4 checked of 4 applicable, 0 missing - found %s" % cov)
    check("Coverage: 4 of 4 check(s) decided, 0 with no data, 0 not run." in answer["said"],
          "the coverage line sits beside the counts in the sentence")
    first = answer["rows"][0]
    check(first.get("coverage") == {"checked": 43, "applicable": 43, "missing": 0}
          and first.get("coverageShown") == "checked 43 of 43 element(s), 0 with no data",
          "a check naming its coverage outputs says how many elements it checked of how many")
    check("(checked 43 of 43 element(s), 0 with no data)" in Q.text(answer),
          "and the chat's line for it says the same")
    check(answer["rows"][1].get("coverageShown") == "",
          "a check naming no coverage output shows none - nothing is guessed")


def some_missing():
    print("2. some data missing: NO DATA, never a pass, never a fail")
    answer = Q.run(checklist(), ask_with({
        # The research note's case: the rule holds on 6 of 43, 37 not read.
        "CHECK_DUCT_NAMES": {"failures": "0", "scanned": "43", "notChecked": "37"},
        # The output the rule is on never came back.
        "LIST_LINKED_MODELS": {"report": "nothing"},
        # Decided on what it has: a FAIL.
        "SELECT_UNENCLOSED_ROOMS": {"unenclosed": "2 item(s) [7, 8]"},
        # A failure found stands, whatever else could not be read.
        "CHECK_PIPE_NAMES": {"failures": "3", "scanned": "12", "notChecked": "5"}}))
    rows = answer["rows"]
    states = [r["state"] for r in rows]
    check(states == ["missing", "missing", "fail", "fail"],
          "NO DATA, NO DATA, FAIL, FAIL - found %s" % states)
    check(rows[0].get("label") == "NO DATA" and "37 element(s) of 43 looked at had no data to check"
          in rows[0]["why"] and "No data is not a pass" in rows[0]["why"],
          "a rule met on 6 of 43 is NO DATA, saying how many had no data - %r" % rows[0]["why"][:120])
    check(rows[0].get("coverageShown") == "checked 6 of 43 element(s), 37 with no data",
          "and its coverage is 6 checked of 43, 37 with no data")
    check(rows[1].get("label") == "NO DATA" and "did not send back 'notLoaded'" in rows[1]["why"],
          "an output that did not come back is NO DATA, not NOT RUN")
    check(rows[3]["state"] == "fail" and rows[3].get("coverageShown") == "checked 7 of 12 element(s), 5 with no data",
          "a FAIL stays a FAIL with its coverage beside it")
    check(counted(answer, "pass") == 0 and counted(answer, "fail") == 2 and counted(answer, "missing") == 2
          and counted(answer, "not run") == 0,
          "the counts keep NO DATA out of both the passes and the fails - %s" % answer.get("counts"))
    check("0 meet the value checked, 2 do not, 2 could not decide - data missing, 0 not run" in answer["said"]
          and "Coverage: 2 of 4 check(s) decided, 2 with no data, 0 not run." in answer["said"]
          and "A check with no data is not a pass." in answer["said"],
          "the sentence counts NO DATA on its own, with the coverage, and says it is not a pass")
    check("NO DATA" in Q.text(answer), "the chat sees NO DATA")
    empty = Q.judge(1, checklist()["checks"][1], {"ran": True, "provides": {"notLoaded": ""}})
    text = Q.judge(1, checklist()["checks"][1], {"ran": True, "provides": {"notLoaded": "Func`2"}})
    check(empty["state"] == "missing" and "came back empty" in empty["why"]
          and text["state"] == "missing" and "not a number" in text["why"],
          "an output that came back empty, or as text, is NO DATA")
    lost = Q.judge(1, checklist()["checks"][0], {"ran": True, "provides": {"failures": "0", "scanned": "43"}})
    check(lost["state"] == "missing" and "'notChecked'" in lost["why"] and "did not come back" in lost["why"],
          "a pass whose count of what it could not read never came back is NO DATA")
    nothing = Q.judge(1, checklist()["checks"][0],
                      {"ran": True, "provides": {"failures": "0", "scanned": "0", "notChecked": "0"}})
    check(nothing["state"] == "missing" and "nothing was there to check" in nothing["why"].lower(),
          "a rule met with nothing looked at is NO DATA, never a pass")
    odd = Q.judge(1, checklist()["checks"][0],
                  {"ran": True, "provides": {"failures": "0", "scanned": "4", "notChecked": "9"}})
    check(odd["state"] == "missing" and odd["coverage"]["checked"] is None,
          "more without data than looked at: no coverage figure is shown, and it is not a pass")
    not_read = Q.judge(1, checklist()["checks"][0], {"ran": False, "why": "Revit is not connected."})
    check(not_read["state"] == "not run", "nothing read is still NOT RUN - NO DATA is for a read that ran")


def all_missing():
    print("3. all data missing: nothing decided, nothing passed")
    answer = Q.run(checklist(), ask_with({
        "CHECK_DUCT_NAMES": {"failures": "0", "scanned": "0", "notChecked": "0"},
        "LIST_LINKED_MODELS": {"notLoaded": "(null)"},
        "SELECT_UNENCLOSED_ROOMS": {},
        "CHECK_PIPE_NAMES": {"failures": "0", "scanned": "12", "notChecked": "12"}}))
    states = [r["state"] for r in answer["rows"]]
    check(states == ["missing"] * 4, "every check is NO DATA - found %s" % states)
    check(counted(answer, "pass") == 0 and counted(answer, "fail") == 0 and counted(answer, "missing") == 4,
          "nothing passed and nothing failed")
    cov = coverage(answer)
    check(cov.get("checked") == 0 and cov.get("applicable") == 4 and cov.get("missing") == 4,
          "coverage: 0 checked of 4 applicable, 4 missing - found %s" % cov)
    check("Coverage: 0 of 4 check(s) decided, 4 with no data, 0 not run." in answer["said"]
          and "A check with no data is not a pass." in answer["said"],
          "the sentence says none was decided, and that no data is not a pass")
    words = [answer["said"], Q.text(answer), json.dumps(answer), json.dumps(Q.STATES)]
    hits = [m.group(0) for text in words for m in BANNED.finditer(text)]
    check(not hits, "never compliant, clean or healthy%s" % (" - found %s" % hits if hits else ""))
    mixed = Q.result({"name": "x", "checks": [{}, {}]},
                     [dict(answer["rows"][0]), dict(Q.judge(2, checklist()["checks"][1],
                                                            {"ran": False, "why": "gone"}))])
    check("A check with no data, or not run, is not a pass." in mixed["said"],
          "with both kinds, the sentence names both")


def saving(home):
    print("4. a check's coverage outputs, when saved and read")
    good = {"name": "Duct names", "checks": [
        {"sentence": "every duct has a System Name", "capability": "CHECK_DUCT_NAMES",
         "values": [], "rule": "failures == 0", "ids": "offenders",
         "missing": "notChecked", "applicable": "scanned"}]}
    check(Q.problems(good, CONTRACTS) == [], "a check naming two number outputs it leaves is good")
    for change, words, what in (
            ({"missing": "unread"}, "'unread' is not something CHECK_DUCT_NAMES leaves behind",
             "a coverage output the fragment does not leave"),
            ({"applicable": "report"}, "'report' is string, not a number",
             "a coverage output that is text, not a number"),
            ({"missing": 7}, "must name the one output that counts the elements the check could not read",
             "a coverage output not named as text")):
        bad = json.loads(json.dumps(good))
        bad["checks"][0].update(change)
        said = " ".join(Q.problems(bad, CONTRACTS))
        check(words in said, "refused when saved: %s%s" % (what, "" if words in said else " - %r" % said[:160]))
    Q.save(good, CONTRACTS, home=home)
    kept = Q.find("Duct names", home)["checks"][0]
    check(kept.get("missing") == "notChecked" and kept.get("applicable") == "scanned",
          "kept with the check")
    line = Q.check_line(kept)
    check("elements with no data counted in notChecked" in line
          and "elements looked at counted in scanned" in line, "and said in the check's line")
    path = os.path.join(Q.folder(home), "duct-names.json")
    with open(path, encoding="utf-8") as fh:
        data = json.load(fh)
    data["checks"][0]["missing"] = ["notChecked"]
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(data, fh)
    found, broken = Q.all_checklists(home)
    check(broken == ["duct-names.json"] and not found,
          "a hand-edited file whose coverage output is not text is listed as one that could not be read")


def history(home):
    print("5. NO DATA is never a change since the last run")
    before = {"heron-checklist-run": Q.RUNS_FORMAT, "at": "2026-11-15 09:00", "rows": []}
    rows = []
    for number, (then, now) in enumerate((("fail", "missing"), ("pass", "missing"),
                                         ("missing", "pass"), ("missing", "fail")), 1):
        row = {"sentence": "check %d" % number, "capability": "X", "rule": "a == 0", "values": [],
               "state": now, "selection": []}
        rows.append(row)
        before["rows"].append({"key": Q._key(row), "state": then, "selection": False})
    answer = Q.compare({"rows": rows, "document": "Tower MEP"}, before)
    changes = [r["change"] for r in answer["rows"]]
    check(changes == ["unknown"] * 4, "each is NOT COMPARED, never FIXED or NEW - found %s" % changes)
    why = getattr(Q, "NO_DATA", None)
    check(why is not None and all(r["changeWhy"] == why for r in answer["rows"]),
          "and says the data to decide it was missing, now or last time")
    kept_by_newer = dict(before, rows=[dict(before["rows"][0], state="some later state")])
    later = Q.compare({"rows": [dict(rows[0], state="fail")], "document": "Tower MEP"}, kept_by_newer)
    check(later["rows"][0]["change"] == "unknown",
          "a state this Heron does not know is not compared either - only PASS and FAIL are")


def main():
    home = tempfile.mkdtemp(prefix="heron-nodata-")
    try:
        for part in (all_data, some_missing, all_missing):
            try:
                part()
            except Exception as why:                 # noqa: BLE001 - one failure, not a traceback
                check(False, "%s raised %s: %s" % (part.__name__, type(why).__name__, why))
        for part in (saving, history):
            try:
                part(home)
            except Exception as why:                 # noqa: BLE001 - one failure, not a traceback
                check(False, "%s raised %s: %s" % (part.__name__, type(why).__name__, why))
    finally:
        shutil.rmtree(home, ignore_errors=True)
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for what in FAILURES:
            print("  - %s" % what)
        return 1
    print("PASS    a QA check never passes on missing data, and every run says its coverage (D-165)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
