# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-QA-BIM-011
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Companion's Model Dashboard - what brain/heron_model_dashboard.py makes
of REPORT_MODEL_DASHBOARD's dashboardJson (docs/40 section 21.10).

    python tests/test_model_dashboard.py

WHAT IT PROVES
  A dashboard from another release, or one that is not a dashboard, is
  refused with a sentence and never drawn; every number is a count of what
  was sent, never invented; a part Revit could not read shows as NOT READ,
  never as zero; a non-workshared model has no closed-worksets row; the
  worth-a-look rows come first; an error warning's bar is the error colour;
  no row or line calls the model compliant, clean or healthy; ids are digit
  strings, capped, and model text is carried as plain data.

WHAT IT DOES NOT PROVE
  That the fragment reads a real model as described - that is its proof,
  owed in Revit. How the page draws it - the Companion's test and an eye.
"""

from __future__ import print_function

import ast
import copy
import json
import os
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests"))

import heron_model_dashboard as D                             # noqa: E402
from test_takeoff import run_all                              # noqa: E402

SAMPLE = {
    "format": 1,
    "model": {"title": "Tower MEP", "isFamily": False, "isWorkshared": True},
    "elementTotal": 1250,
    "categories": [{"name": "Ducts", "instances": 600, "types": 4},
                   {"name": "Pipes", "instances": 400, "types": 3},
                   {"name": "Air Terminals", "instances": 250, "types": 2}],
    "levels": [{"name": "Level 01", "elevationMm": 0, "instances": 700},
               {"name": "Level 02", "elevationMm": 4000, "instances": 540}],
    "noLevel": 10,
    "worksets": [{"name": "MEP", "open": True, "instances": 1200},
                 {"name": "Shared Levels and Grids", "open": False, "instances": 50}],
    "warnings": {"total": 31, "errors": 1, "groups": [
        {"text": "Elements have duplicate 'Mark' values.", "severity": "error", "count": 1,
         "ids": ["101", "102"]},
        {"text": "There are identical instances in the same place.", "severity": "warning",
         "count": 30, "ids": ["201", "202", "203"]}]},
    "views": {"total": 80, "templates": 6, "onSheets": 50, "notOnSheets": 30,
              "withoutTemplate": 12, "byType": {"FloorPlan": 40, "Section": 20, "ThreeD": 20},
              "notOnSheetsIds": ["301"]},
    "sheets": {"total": 20, "empty": 2, "placeholders": 1, "emptyIds": ["401", "402"]},
    "families": {"loaded": 90, "inPlace": 1, "unused": 14, "types": 300,
                 "unusedNames": ["Old Diffuser"]},
    "groups": {"modelTypes": 3, "detailTypes": 1, "unusedTypes": 2},
    "links": [{"name": "ARCH.rvt", "loaded": True, "pinned": False, "instances": 1}],
    "cad": {"total": 3, "linked": 2, "imported": 1, "names": ["site.dwg"]},
    "rooms": {"total": 40, "placed": 37, "unplaced": 2, "notEnclosed": 1,
              "unplacedIds": ["501", "502"], "notEnclosedIds": ["503"]},
    "spaces": {"total": 40, "placed": 40, "unplaced": 0, "notEnclosed": 0,
               "unplacedIds": [], "notEnclosedIds": []},
    "notRead": [],
}


def sample(**changes):
    data = copy.deepcopy(SAMPLE)
    data.update(changes)
    return json.dumps(data)


def built(**changes):
    got = D.dashboard(sample(**changes))
    assert got["dashboard"] is not None, got["said"]
    return got["dashboard"]


def row(board, key):
    return [r for r in board["look"] if r["key"] == key][0]


def tile(board, key):
    return [t for t in board["tiles"] if t["key"] == key][0]


def test_refuses_what_is_not_a_dashboard():
    for text in ("", "not json", "[]", json.dumps({"format": 2})):
        got = D.dashboard(text)
        assert got["dashboard"] is None and got["said"].startswith("No dashboard was made")
    assert "different releases" in D.dashboard(json.dumps({"format": 2}))["said"]


def test_numbers_are_the_ones_sent():
    board = built()
    assert tile(board, "elements")["value"] == 1250
    assert tile(board, "warnings")["value"] == 31
    assert tile(board, "worksets")["note"] == "1 closed"
    assert row(board, "rooms-unplaced")["count"] == 2
    assert row(board, "rooms-unplaced")["ids"] == ["501", "502"]
    assert row(board, "links-unpinned")["names"] == ["ARCH.rvt"]
    cats = [c for c in board["charts"] if c["key"] == "categories"][0]
    assert [b["value"] for b in cats["bars"]] == [600, 400, 250]


def test_a_part_not_read_is_never_zero():
    board = built(sheets=None, notRead=["sheets: the title blocks could not be read"])
    assert tile(board, "sheets")["state"] == "not read"
    assert tile(board, "sheets")["value"] is None
    assert tile(board, "sheets")["note"] == "sheets: the title blocks could not be read"
    assert row(board, "sheets-empty")["count"] is None
    data = json.loads(sample())
    del data["rooms"]
    board = D.dashboard(json.dumps(data))["dashboard"]
    assert tile(board, "rooms")["state"] == "not read"


def test_a_note_beside_a_number_never_hides_it():
    note = "elements: 3 element(s) could not be read and are in no count"
    board = built(notRead=[note])
    assert tile(board, "elements")["value"] == 1250
    assert tile(board, "elements")["state"] == "info"
    assert note in board["notRead"]


def test_a_list_cut_short_is_said():
    board = built(capped=["warnings: 812 kinds, the first 500 sent"])
    assert board["capped"] == ["warnings: 812 kinds, the first 500 sent"]


def test_a_family_document_reads_nothing_as_zero():
    parts = dict((key, None) for _part, key in D.PARTS)
    board = built(model={"title": "Diffuser.rfa", "isFamily": True, "isWorkshared": False},
                  notRead=["%s: not a project" % p for p, _k in D.PARTS], **parts)
    assert all(t["state"] == "not read" for t in board["tiles"] if t["key"] != "worksets")
    assert all(r["count"] is None for r in board["look"])


def test_not_workshared_has_no_closed_worksets_row():
    # As the fragment sends it: an empty list, and no note.
    board = built(model={"title": "Small", "isFamily": False, "isWorkshared": False},
                  worksets=[])
    assert not [r for r in board["look"] if r["key"] == "worksets-closed"]
    assert tile(board, "worksets")["note"] == "not workshared"
    assert tile(board, "worksets")["state"] == "info"


def test_worth_a_look_first_and_none_found_after():
    states = [r["state"] for r in built()["look"]]
    order = {"look": 0, "info": 1, "ok": 2, "not read": 3}
    assert states == sorted(states, key=order.get)
    assert row(built(), "spaces-unplaced")["state"] == "ok"


def test_error_bars_are_the_error_colour():
    bars = [c for c in built()["charts"] if c["key"] == "warnings"][0]["bars"]
    assert bars[0]["colour"] == D.BAR_ERROR and bars[1]["colour"] == D.BAR


def test_many_categories_fold_into_others():
    many = [{"name": "C%02d" % i, "instances": 100 - i, "types": 1} for i in range(20)]
    bars = [c for c in built(categories=many)["charts"] if c["key"] == "categories"][0]["bars"]
    assert len(bars) == D.CHART_BARS + 1 and bars[-1]["label"].startswith("Others (8 more)")
    assert bars[-1]["value"] == sum(100 - i for i in range(D.CHART_BARS, 20))


def test_never_calls_the_model_compliant():
    words = json.dumps(built()).lower()
    for word in ("compliant", "complies", "healthy", "clean model", "score", "passes"):
        assert word not in words, word


def test_ids_are_digits_and_capped():
    many = [str(i) for i in range(1000)] + ["12; DROP", "<b>9</b>"]
    board = built(rooms=dict(SAMPLE["rooms"], unplacedIds=many))
    ids = row(board, "rooms-unplaced")["ids"]
    assert len(ids) == D.IDS_MAX and all(i.isdigit() for i in ids)


def test_model_text_is_carried_as_data():
    board = built(categories=[{"name": "<img src=x onerror=alert(1)>", "instances": 5}])
    bars = [c for c in board["charts"] if c["key"] == "categories"][0]["bars"]
    assert bars[0]["label"] == "<img src=x onerror=alert(1)>"


def test_a_family_is_said_to_be_a_family():
    said = built(model={"title": "Diffuser.rfa", "isFamily": True, "isWorkshared": False},
                 notRead=["categories: not a project"])["said"]
    assert "is a family" in said and "Nothing was changed" in said


def test_said_is_one_line_and_says_nothing_changed():
    said = built()["said"]
    assert "\n" not in said and said.endswith("Nothing was changed.")
    assert "31 warnings (1 error)" in said


FILL = {"parameters": ["Mark", "Fire Rating"], "rows": [
    {"category": "Doors", "parameter": "Mark", "populated": 8, "empty": 2, "absent": 0,
     "emptyIds": ["701", "702"]},
    {"category": "Walls", "parameter": "Mark", "populated": 10, "empty": 0, "absent": 0,
     "emptyIds": []},
    {"category": "Doors", "parameter": "Fire Rating", "populated": 0, "empty": 0, "absent": 10,
     "emptyIds": []}]}


def test_no_parameters_asked_means_no_fill():
    board = built()
    assert board["fill"] is None
    assert not [r for r in board["look"] if r["key"].startswith("fill:")]


def test_fill_keeps_empty_and_absent_apart():
    board = built(fill=FILL)
    mark, fire = board["fill"]["parameters"]
    assert (mark["populated"], mark["empty"], mark["absent"]) == (18, 2, 0)
    assert mark["rows"][0]["category"] == "Doors" and mark["rows"][0]["share"] == 80
    assert mark["rows"][0]["state"] == "look"
    assert (fire["empty"], fire["absent"]) == (0, 10) and fire["rows"][0]["state"] == "info"
    row_ = row(board, "fill:Mark")
    assert row_["count"] == 2 and row_["ids"] == ["701", "702"] and row_["state"] == "look"
    assert row(board, "fill:Fire Rating")["state"] == "ok"


def test_fill_not_read_is_null_not_zero():
    board = built(fill=None, notRead=["fill: the parameters could not be read"])
    assert board["fill"] is None
    assert "fill: the parameters could not be read" in board["notRead"]


def test_since_the_last_read_says_what_moved():
    old = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = tempfile.mkdtemp(prefix="heron-dash-")
    try:
        first = D.dashboard(sample(), project_key="proj-1")["dashboard"]
        assert first["since"] is None
        more = copy.deepcopy(SAMPLE)
        more["elementTotal"] = 1300
        more["categories"][0]["instances"] = 650
        more["warnings"]["total"] = 25
        second = D.dashboard(json.dumps(more), project_key="proj-1")["dashboard"]
        moved = dict((r["label"], r["change"]) for r in second["since"]["rows"])
        assert moved["Model elements"] == 50 and moved["Warnings"] == -6 and moved["Ducts"] == 50
        assert "Sheets" not in moved
        assert len(D.kept("proj-1")) == 2
        assert D.dashboard(sample(), project_key="proj-2")["dashboard"]["since"] is None
    finally:
        if old is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = old


def test_a_part_unread_is_never_compared():
    was = D.snapshot(D.read(sample()))
    data = D.read(sample(sheets=None, notRead=["sheets: could not be read"]))
    now = D.snapshot(data)
    assert "sheets.total" not in now["numbers"]
    assert not [r for r in D.compare(was, now) if r["label"] in ("Sheets", "Empty sheets")]


def test_a_snapshot_keeps_no_ids_or_names():
    snap = json.dumps(D.snapshot(D.read(sample())))
    for text in ("501", "ARCH.rvt", "Old Diffuser", "site.dwg"):
        assert text not in snap, text


def test_nothing_kept_without_a_project():
    assert D.dashboard(sample())["dashboard"]["since"] is None


def test_the_demo_is_invented_and_names_no_element():
    got = D.demo()
    board = got["dashboard"]
    assert board["demo"] is True and got["said"].startswith("DEMO")
    assert all(not r["ids"] for r in board["look"])
    assert board["since"] is None if "since" in board else True


def test_imports_only_the_standard_library():
    allowed = set(getattr(sys, "stdlib_module_names", ())) or {"json"}
    tree = ast.parse(open(os.path.join(ROOT, "brain", "heron_model_dashboard.py")).read())
    names = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            names.update(alias.name.split(".")[0] for alias in node.names)
        elif isinstance(node, ast.ImportFrom):
            names.add((node.module or "").split(".")[0])
    # heron_designbasis: where a project's kept answers live - the Loads
    # panel's runs use the same folder.
    assert names <= allowed | {"__future__", "heron_designbasis"}, names


if __name__ == "__main__":
    sys.exit(run_all(sys.modules[__name__]))
