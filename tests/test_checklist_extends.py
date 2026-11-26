#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
An office checklist, and a project checklist that extends it (D-179).

    python tests/test_checklist_extends.py

WHAT IT HOLDS
-------------
  1. SAVING        a project list names the office list it extends and its
                   overrides - only severity, target and on/off, each naming
                   an office check by its exact words - plus checks of its
                   own. What it runs is saved whole beside them.
  2. REFUSALS      anything else is refused in words and nothing is kept: an
                   override of any other field, one naming no office check,
                   two for one check, an office list that is not saved, one
                   that itself extends another, a list extending itself, and
                   a list with nothing left to run.
  3. AT EACH RUN   the office list is read again, so its changes reach every
                   project - and the run says so. An office list deleted, or
                   changed so an override no longer matches, runs the copy
                   saved with the project, and says that instead.
  4. THE FINGERPRINT of the checks a run used, on every run, so two runs that
                   used different checks can never be taken for one another;
                   and the report carries it, with the office list's name.
  5. A HAND-EDITED FILE whose extends or overrides are the wrong shape is
                   listed as one that could not be read.
  6. THE WIRING    heron_checklist takes `extends` and `overrides`, and both
                   runs - the chat's and the Companion's - read the office
                   list again first. Read as text.
"""

from __future__ import print_function

import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
import heron_tools as SERVER_TOOLS                          # noqa: E402
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_checklists as Q                                  # noqa: E402
import heron_checklist_report as R                            # noqa: E402

FAILURES = []

_PROVIDES = [{"name": "failures", "type": "int"}, {"name": "offenders", "type": "IList<ElementId>"},
             {"name": "report", "type": "string"}]
CONTRACTS = dict((cap, {"risk": "READ", "needs": [{"name": "doc", "type": "Document"}],
                        "provides": _PROVIDES, "required": [], "selection": []})
                 for cap in ("CHECK_DUCT_NAMES", "CHECK_PIPE_NAMES", "CHECK_WALL_TYPES",
                             "CHECK_ROOM_NAMES", "CHECK_LEVEL_NAMES"))


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def one(sentence, capability, severity=""):
    return {"sentence": sentence, "capability": capability, "values": [],
            "rule": "failures == 0", "ids": "offenders", "severity": severity}


OFFICE = {"name": "Office standard", "description": "Every project, every issue", "checks": [
    one("every duct has a System Name", "CHECK_DUCT_NAMES", "high"),
    one("every pipe has a System Name", "CHECK_PIPE_NAMES"),
    one("every wall type is from the office library", "CHECK_WALL_TYPES"),
]}


def project(**changes):
    base = {"name": "Tower - before issue", "description": "", "extends": "Office standard",
            "overrides": [{"sentence": "every duct has a System Name", "severity": "low"},
                          {"sentence": "every pipe has a System Name", "target": 5},
                          {"sentence": "every wall type is from the office library", "enabled": False}],
            "checks": [one("every room has a name", "CHECK_ROOM_NAMES")]}
    base.update(changes)
    return base


def refused(home, checklist, what, words):
    try:
        Q.save(checklist, CONTRACTS, home=home, replace=True)
    except Q.ChecklistError as why:
        check(words in str(why), "%s - refused, saying \"%s\"" % (what, words))
        return
    check(False, "%s - was saved, and should be refused" % what)


def saving(home):
    print("1. saving a project list that extends the office list")
    Q.save(OFFICE, CONTRACTS, home=home)
    Q.save(project(), CONTRACTS, home=home)
    kept = Q.find("Tower - before issue", home)
    sentences = [c["sentence"] for c in kept["checks"]]
    check(sentences == ["every duct has a System Name", "every pipe has a System Name",
                        "every room has a name"],
          "what it runs: the office checks, the one switched off left out, then its own - found %s"
          % sentences)
    check(kept["checks"][0]["severity"] == "low", "an override changes severity")
    check(Q.rule_text(kept["checks"][1]) == "failures == 5", "an override changes the target")
    check(kept.get("extends") == "Office standard" and len(kept.get("overrides") or []) == 3,
          "the office list's name and the overrides are kept")
    check([c["sentence"] for c in kept.get("own") or []] == ["every room has a name"],
          "and its own checks apart")
    check("extends 'Office standard'" in Q.describe(kept), "its one line says what it extends")


def refusals(home):
    print("\n2. what is refused")
    refused(home, project(overrides=[{"sentence": "every duct has a System Name",
                                      "capability": "CHECK_PIPE_NAMES"}]),
            "an override of another field", "only severity, target or enabled")
    refused(home, project(overrides=[{"sentence": "every door has a mark", "severity": "low"}]),
            "an override naming no office check", "no check in 'Office standard' says")
    refused(home, project(overrides=[{"sentence": "every duct has a System Name", "severity": "low"},
                                     {"sentence": "every duct has a System Name", "target": 2}]),
            "two overrides for one check", "more than one override")
    refused(home, project(overrides=[{"sentence": "every duct has a System Name", "severity": "urgent"}]),
            "a severity that is not high, medium or low", "severity")
    refused(home, project(overrides=[{"sentence": "every duct has a System Name", "enabled": "no"}]),
            "on/off that is not true or false", "enabled")
    refused(home, project(extends="Head office"), "an office list not saved", "No checklist called 'Head office'")
    refused(home, project(name="Office standard"), "a list extending itself", "cannot extend itself")
    refused(home, project(overrides=[{"sentence": c["sentence"], "enabled": False} for c in OFFICE["checks"]],
                          checks=[]),
            "a list with nothing left to run", "nothing left to run")
    Q.save(dict(project(), name="Tower L2"), CONTRACTS, home=home, replace=True)
    refused(home, dict(project(), name="Tower L3", extends="Tower L2", overrides=[]),
            "extending a list that itself extends another", "itself extends")
    check(Q.find("Tower - before issue", home)["checks"][0]["severity"] == "low",
          "and nothing refused changed what was saved")


def at_each_run(home):
    print("\n3. at each run, the office list is read again")
    kept = Q.find("Tower - before issue", home)
    used, notes = Q.prepare_run(kept, home)
    check(notes == [] and [c["sentence"] for c in used["checks"]] == [c["sentence"] for c in kept["checks"]],
          "office list unchanged: the saved checks, nothing said")
    office = json.loads(json.dumps(OFFICE))
    office["checks"].append(one("every level has a name", "CHECK_LEVEL_NAMES"))
    Q.save(office, CONTRACTS, home=home, replace=True)
    used, notes = Q.prepare_run(Q.find("Tower - before issue", home), home)
    check("every level has a name" in [c["sentence"] for c in used["checks"]],
          "a check added to the office list reaches the project at its next run")
    check(any("changed since" in n for n in notes), "and the run says the office list changed")
    check(used["checks"][0]["severity"] == "low", "the project's overrides still apply")
    office["checks"] = [c for c in office["checks"] if c["sentence"] != "every pipe has a System Name"]
    Q.save(office, CONTRACTS, home=home, replace=True)
    kept = Q.find("Tower - before issue", home)
    used, notes = Q.prepare_run(kept, home)
    check(used["checks"] == kept["checks"], "an override that no longer matches: the saved copy runs")
    check(any("no longer matches" in n for n in notes), "and the run says why")
    Q.delete("Office standard", home)
    used, notes = Q.prepare_run(kept, home)
    check(used["checks"] == kept["checks"] and any("no longer saved" in n for n in notes),
          "the office list deleted: the saved copy runs, and the run says so")
    plain = Q.find("Tower L2", home)
    plain.pop("extends", None)
    check(Q.prepare_run(plain, home) == (plain, []), "a list that extends nothing runs as it is")


def fingerprint():
    print("\n4. the fingerprint")
    a = [one("x", "CHECK_DUCT_NAMES")]
    check(Q.fingerprint(a) == Q.fingerprint(json.loads(json.dumps(a))), "the same checks, the same fingerprint")
    check(Q.fingerprint(a) != Q.fingerprint([one("y", "CHECK_DUCT_NAMES")]), "other checks, another")
    answer = {"name": "Tower", "rows": [], "counts": {}, "findings": [], "said": "Tower: 0 check(s)."}
    Q.stamp(answer, dict(project(), checks=a), ["The office checklist changed since."])
    check(answer.get("fingerprint") == Q.fingerprint(a) and answer.get("extends") == "Office standard",
          "a run carries the fingerprint of what it used, and what it extends")
    check("The office checklist changed since." in answer["findings"], "and the notes, as findings")
    page = R.html(answer)
    check(Q.fingerprint(a) in page and "Office standard" in page, "the report carries both")


def hand_edited(home):
    print("\n5. a hand-edited file of the wrong shape")
    where = Q.folder(home, make=True)
    for bad, what in ((dict(project(), overrides="severity low"), "overrides that are not a list"),
                      (dict(project(), extends=5), "extends that is not text")):
        bad = dict(bad, **{"heron-checklist": Q.FORMAT})
        bad["checks"] = [dict(c, output="failures", op="==", target=0) for c in bad["checks"]]
        with io.open(os.path.join(where, "hand-made.json"), "w", encoding="utf-8") as fh:
            fh.write(json.dumps(bad))
        found, broken = Q.all_checklists(home)
        check("hand-made.json" in broken, "%s: listed as could not be read" % what)


def wiring():
    print("\n6. the wiring")
    server = SERVER_TOOLS.server_text()
    head = server[server.find("def heron_checklist("):]
    head = head[:head.find(")")]
    check('extends: str = ""' in head and 'overrides: str = ""' in head,
          "heron_checklist takes `extends` and `overrides`")
    check(server.count("store.prepare_run(") >= 2,
          "both runs - the chat's and the Companion's - read the office list again first")
    check(server.count("store.stamp(") >= 2, "and both stamp what the run used")


def main():
    home = tempfile.mkdtemp(prefix="heron-extends-")
    try:
        for part in (saving, refusals, at_each_run, hand_edited):
            try:
                part(home)
            except Exception as why:                 # noqa: BLE001 - one failure, not a traceback
                check(False, "%s raised %s: %s" % (part.__name__, type(why).__name__, why))
        for part in (fingerprint, wiring):
            try:
                part()
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
    print("PASS    a project checklist extends the office one, and every run says what it used (D-179)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
