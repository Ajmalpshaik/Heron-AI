# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A sprinkler layout for the rooms the modeller chose (docs/47).

    python tests/test_sprinkler_layout.py

WHAT IT PROVES
  The read is taken whole and refused when it is not format 1; which heads are
  in a room is decided in plan, so a head at the ceiling above a room's upper
  limit is still in it; a room with heads, with separation lines or with no
  outline is shown and not laid out; every answer is asked before anything is
  laid out - the ceiling height and the angle the model offers are never used
  until given; the heads' height is the level's project elevation + the
  ceiling - the deflector, said; a face-based type is refused for placing;
  Apply's plan is one level, `Family: Type` exactly, points in mm; a room
  whose outline or heads moved since the preview is named; the read-back finds
  the heads placed, says how far each is from where it was sent, and runs the
  spacing check on every head really in the room; answers carry between calls
  and a blank clears one.

WHAT IT DOES NOT PROVE
  That a real model is read as format 1 says, or where Revit puts a head sent
  at a z - that is NEEDS-CHECKING group CF.
"""

from __future__ import print_function

import copy
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests"))

import heron_sprinkler_layout as L                            # noqa: E402
import test_sprinkler_takeoff as NET                          # noqa: E402

STD = "NFPA 13-2022"
LIGHT = {"max_spacing_m": 4.6, "max_area_m2": 20.9, "max_wall_distance_m": 2.3,
         "min_wall_distance_m": 0.1}
TYPE = "Sprinkler - Pendent: K80"


def _m(points):
    return [[x / 1000.0, y / 1000.0] for x, y in points]


def read(heads_in_store=True, separation=0):
    """An L-shaped office on a level at 4 m, a store with one head, a lobby with a separation
    line - and a level-based and a face-based sprinkler type."""
    office = [(0, 0), (10000, 0), (10000, 4000), (6000, 4000), (6000, 10000), (0, 10000)]
    store = [(20000, 0), (24000, 0), (24000, 3000), (20000, 3000)]
    lobby = [(30000, 0), (36000, 0), (36000, 6000), (30000, 6000)]
    return {
        "format": 1, "document": "Test", "asked": "*",
        "spaces": [
            {"id": "u-office", "element_id": "11", "kind": "space", "number": "201",
             "name": "Office", "level": "L2", "level_elevation_m": 4.0,
             "level_project_elevation_m": 4.0, "area_m2": 76.0, "location": [3.0, 3.0],
             "outline": _m(office), "holes": [], "separation_edges": separation,
             "ceilings": [{"id": "91", "type": "Grid 600", "height_m": 2.7}],
             # A head at the ceiling, above a Space topped at 2.4 m, but outside
             # the office in plan - in the store's extent only.
             "sprinklers": []},
            {"id": "u-store", "element_id": "12", "kind": "room", "number": "202",
             "name": "Store", "level": "L2", "level_elevation_m": 4.0,
             "level_project_elevation_m": 4.0, "area_m2": 12.0, "location": [22.0, 1.5],
             "outline": _m(store), "holes": [], "separation_edges": 0, "ceilings": [],
             "sprinklers": ([{"id": "501", "type": TYPE, "x": 22.0, "y": 1.5, "z": 6.6,
                              "offset_m": 2.6}] if heads_in_store else [])},
            {"id": "u-lobby", "element_id": "13", "kind": "space", "number": "203",
             "name": "Lobby", "level": "L2", "level_elevation_m": 4.0,
             "level_project_elevation_m": 4.0, "area_m2": 36.0, "location": [33.0, 3.0],
             "outline": _m(lobby), "holes": [], "separation_edges": 1, "ceilings": [],
             "sprinklers": []}],
        "sprinkler_types": [
            {"name": TYPE, "family": "Sprinkler - Pendent", "type": "K80",
             "placement": "OneLevelBased", "k": "K-Factor = 80"},
            {"name": "Sprinkler - Hosted: K80", "family": "Sprinkler - Hosted",
             "type": "K80", "placement": "WorkPlaneBased", "k": None}],
        "findings": []}


def answers(**room):
    office = dict({"hazard": "light", "ceiling_mm": 2700, "angle_deg": 0}, **room)
    return {"standards": {"sprinkler_standard": STD},
            "job": {"type": TYPE, "deflector_mm": 50},
            "limits": {"light": dict(LIGHT)},
            "rooms": {"u-office": office}}


def test_the_read_is_format_one():
    try:
        L.read({"format": 2, "spaces": []})
    except L.LayoutError:
        pass
    else:
        raise AssertionError("a read of another format was taken")
    assert L.read(read())["format"] == 1


def test_heads_are_found_in_plan():
    rooms = dict((r["key"], r) for r in L.rooms(read()))
    store = rooms["u-store"]
    assert [h["id"] for h in store["heads"]] == ["501"]
    assert store["problem"] and "already has 1 sprinkler" in store["problem"]
    assert rooms["u-lobby"]["problem"] and "separation lines" in rooms["u-lobby"]["problem"]
    assert rooms["u-office"]["problem"] is None and rooms["u-office"]["heads"] == []


def test_everything_is_asked_and_nothing_offered_is_used():
    got = L.preview(read(), {})
    office = [r for r in got["rooms"] if r["key"] == "u-office"][0]
    asked = set(a["input"] for a in office["asked"])
    assert {"rooms.u-office.hazard", "rooms.u-office.ceiling_mm",
            "rooms.u-office.angle_deg"} <= asked
    assert office["status"] == "asked" and office["count"] == 0
    assert office["angle_offer_deg"] == 0.0 and office["ceilings"][0]["height_mm"] == 2700.0
    jobs = set(a["input"] for a in got["asked"])
    assert {"job.type", "job.deflector_mm"} <= jobs and got["status"] == "asked"
    no_limits = L.preview(read(), dict(answers(), limits={}))
    office = [r for r in no_limits["rooms"] if r["key"] == "u-office"][0]
    lim = [a for a in office["asked"] if a["input"].startswith("limits.light hazard.")]
    assert len(lim) == 4 and all(a["offer"] for a in lim
                                 if a["input"].endswith("max_spacing_m"))


def test_the_office_is_laid_out_and_the_others_are_not():
    got = L.preview(read(), answers())
    by = dict((r["key"], r) for r in got["rooms"])
    office = by["u-office"]
    assert office["status"] == "ok" and office["count"] > 0, (office["status"], office["why"])
    assert all(p["z_mm"] == 4000 + 2700 - 50 for p in office["points"])
    assert "4000 mm + ceiling 2700 mm - deflector 50 mm" in office["z_from"]
    assert by["u-store"]["status"] == "skipped" and by["u-lobby"]["status"] == "skipped"
    assert got["status"] == "ok" and got["levels"]["L2"]["heads"] == office["count"]
    assert "ready to place" in L.summary_text(got)


def test_a_face_based_type_is_not_placed():
    given = answers()
    given["job"]["type"] = "Sprinkler - Hosted: K80"
    got = L.preview(read(), given)
    assert got["status"] == "asked"
    assert any("needs a face" in a["why"] for a in got["asked"] if a["input"] == "job.type")


def test_apply_sends_one_level_by_name_and_value():
    got = L.preview(read(), answers())
    sent = L.plan(got, "L2")
    assert sent["symbol"] == "Sprinkler - Pendent: K80" and ": " in sent["symbol"]
    assert sent["level"] == "L2" and sent["rooms"] == ["u-office"]
    first = sent["points"].split("; ")[0].split(",")
    assert len(first) == 3 and float(first[2]) == 6650.0
    assert sent["count"] == len(sent["sent"]) == got["levels"]["L2"]["heads"]
    try:
        L.plan(got, "L9")
    except L.LayoutError:
        pass
    else:
        raise AssertionError("a level with nothing passed was planned")


def test_a_moved_room_is_named():
    before = read()
    after = copy.deepcopy(before)
    assert L.changed(before, after, ["u-office"]) == []
    after["spaces"][0]["ceilings"][0]["height_m"] = 2.8
    assert "Office" in L.changed(before, after, ["u-office"])[0]
    after = copy.deepcopy(before)
    after["spaces"][0]["sprinklers"].append({"id": "777", "type": TYPE, "x": 2.0, "y": 2.0,
                                             "z": 6.65, "offset_m": 2.65})
    assert L.changed(before, after, ["u-office"]), "a head added since the preview is a change"
    after["spaces"] = after["spaces"][1:]
    assert "no longer in the model" in L.changed(before, after, ["u-office"])[0]


def test_the_read_back_finds_and_checks_the_heads():
    before = read()
    got = L.preview(before, answers())
    sent = L.plan(got, "L2")
    after = copy.deepcopy(before)
    after["spaces"][0]["sprinklers"] = [
        {"id": str(900 + k), "type": TYPE, "x": s["x_mm"] / 1000.0, "y": s["y_mm"] / 1000.0,
         "z": s["z_mm"] / 1000.0, "offset_m": 2.65} for k, s in enumerate(sent["sent"])]
    back = L.read_back(before, after, sent, got, answers())
    office = back["rooms"][0]
    assert back["placed"] == back["sent"] == sent["count"]
    assert office["worst_plan_mm"] < 1.0 and office["worst_z_mm"] < 1.0
    assert office["spacing"]["verdict"] == "OK", office["spacing"]["verdict"]
    # The level counted twice: every head 4 m too high - said, with Revit's undo named.
    high = copy.deepcopy(after)
    for h in high["spaces"][0]["sprinklers"]:
        h["z"] += 4.0
    back = L.read_back(before, high, sent, got, answers())
    assert "CHECK: the height is not the one asked" in back["text"] and "undo" in back["text"]


def test_answers_carry_and_a_blank_clears():
    kept = answers()
    now = {"rooms": {"u-office": {"ceiling_mm": ""}}, "job": {"deflector_mm": 75}}
    got = L.carried(kept, now)
    assert "ceiling_mm" not in got["rooms"]["u-office"]
    assert got["rooms"]["u-office"]["hazard"] == "light"
    assert got["job"]["deflector_mm"] == 75 and got["job"]["type"] == TYPE
    assert L.normalise({"rooms": "not a map"})["rooms"] == {}


if __name__ == "__main__":
    sys.exit(NET.run_all(sys.modules[__name__]))
