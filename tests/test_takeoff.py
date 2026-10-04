# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The take-off a Revit model gives for a building's loads (docs/44 section 4):
read whole or refused whole, checked before anything is calculated (gate 1),
and each Space's faces turned into the surfaces the room engine reads.

    python tests/test_takeoff.py

WHAT IT PROVES
  A wall nets out its window; every azimuth turns with True North; a window
  type with no SHGC refuses its Space and is never defaulted; a Space not
  placed is listed and left out; an opening larger than its face is a FAIL;
  a wall to another conditioned Space carries no load; a model with no site
  location is a FAIL; the module imports the standard library only.

WHAT IT DOES NOT PROVE
  That REPORT_SPACE_ENVELOPE reads a real model the way format 1 says - that
  is a run on a named model (docs/needs-checking).
"""

from __future__ import print_function

import ast
import copy as _copy
import os
import sys
import traceback

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_takeoff as T                                     # noqa: E402

ROOM = {"format": 1, "document": "t", "units": {"power": "W", "airflow": "L/s"},
        "site": {"latitude_deg": 25.28, "longitude_deg": 51.53, "utc_offset_h": 3.0,
                 "elevation_m": 10.0, "project_to_true_north_deg": 0.0, "named": "Doha"},
        "types": {"w": {"name": "Ext wall", "category": "Walls", "u_w_m2k": 0.5,
                        "shgc": None, "absorptance": 0.7},
                  "g": {"name": "Window 1200", "category": "Windows", "u_w_m2k": 2.8,
                        "shgc": 0.4, "absorptance": None},
                  "r": {"name": "Roof", "category": "Roofs", "u_w_m2k": 0.3,
                        "shgc": None, "absorptance": 0.6}},
        "spaces": [{"id": 1, "unique_id": "u1", "number": "1", "name": "Office 01",
                    "level": "L1", "area_m2": 20.0, "volume_m3": 54.0, "height_m": 2.7,
                    "zone": "Z1", "space_type": "Office", "placed": True,
                    "current": {"Design Cooling Load": "0.00 W",
                                "Design Heating Load": "0.00 W",
                                "Specified Supply Airflow": "0.0 L/s"},
                    "terminals": [],
                    "faces": [{"element": 10, "type": "w", "side": "wall",
                               "normal": [-1.0, 0.0, 0.0], "area_m2": 13.5,
                               "beyond": "outside", "beyond_space": None,
                               "openings": [{"element": 11, "kind": "window", "type": "g",
                                             "area_m2": 2.0}]},
                              {"element": 12, "type": "r", "side": "top",
                               "normal": [0.0, 0.0, 1.0], "area_m2": 20.0,
                               "beyond": "outside", "beyond_space": None, "openings": []}]}],
        "findings": []}


def copy(**change):
    d = _copy.deepcopy(ROOM)
    d.update(change)
    return d


def raises(kind, fn, *args):
    try:
        fn(*args)
    except kind:
        return True
    return False


def test_west_wall_nets_out_its_window():
    t = T.read(ROOM)
    s = T.surfaces(t, t.spaces[0])
    assert s["walls"] == [{"name": "Ext wall (10)", "area_m2": 11.5, "u_w_m2k": 0.5,
                           "facing": 270.0, "absorptance": 0.7}], s["walls"]
    assert s["windows"] == [{"name": "Window 1200 (11)", "area_m2": 2.0, "u_w_m2k": 2.8,
                             "shgc": 0.4, "facing": 270.0}], s["windows"]
    assert s["roofs"] == [{"name": "Roof (12)", "area_m2": 20.0, "u_w_m2k": 0.3,
                           "absorptance": 0.6}], s["roofs"]
    assert s["refused"] == []


def test_true_north_turns_every_azimuth():
    assert T.azimuth_deg([0.0, 1.0, 0.0], 0.0) == 0.0
    assert T.azimuth_deg([1.0, 0.0, 0.0], 0.0) == 90.0
    assert T.azimuth_deg([-1.0, 0.0, 0.0], 0.0) == 270.0
    # project north turned 30 degrees east of true north: a face to project north faces 30
    assert abs(T.azimuth_deg([0.0, 1.0, 0.0], 30.0) - 30.0) < 1e-9


def test_missing_shgc_refuses_that_space_only():
    d = copy()
    d["types"]["g"]["shgc"] = None
    t = T.read(d)
    s = T.surfaces(t, t.spaces[0])
    assert any("Window 1200" in r and "SHGC" in r for r in s["refused"]), s["refused"]
    assert s["windows"] == []                     # never defaulted to anything


def test_unplaced_space_is_listed_not_run():
    d = copy()
    d["spaces"][0]["placed"] = False
    d["spaces"][0]["area_m2"] = 0.0
    findings = T.qa(T.read(d))
    assert any(f["level"] == "FAIL" and f["space"] == 1 and "not placed" in f["text"]
               for f in findings), findings


def test_wrong_format_is_refused_whole():
    assert raises(T.TakeoffError, T.read, {"format": 2})
    assert raises(T.TakeoffError, T.read, "not json")
    assert raises(T.TakeoffError, T.read, {"format": 1, "spaces": []})


def test_opening_larger_than_its_face_is_a_fail():
    d = copy()
    d["spaces"][0]["faces"][0]["openings"][0]["area_m2"] = 14.0
    assert any(f["level"] == "FAIL" and "larger than" in f["text"] for f in T.qa(T.read(d)))


def test_partition_to_another_space_carries_no_load():
    d = copy()
    d["spaces"][0]["faces"][0]["beyond"] = "space"
    d["spaces"][0]["faces"][0]["beyond_space"] = 2
    t = T.read(d)
    s = T.surfaces(t, t.spaces[0])
    assert s["walls"] == [] and s["windows"] == [] and s["partitions"] == []


def test_no_site_location_is_a_fail():
    d = copy()
    d["site"]["latitude_deg"] = None
    assert any(f["level"] == "FAIL" and "site location" in f["text"] for f in T.qa(T.read(d)))


def test_imports_only_the_standard_library():
    allowed = set(getattr(sys, "stdlib_module_names", ())) or {"json", "math"}
    tree = ast.parse(open(os.path.join(ROOT, "brain", "heron_takeoff.py")).read())
    names = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            names.update(alias.name.split(".")[0] for alias in node.names)
        elif isinstance(node, ast.ImportFrom):
            names.add((node.module or "").split(".")[0])
    assert names <= allowed | {"__future__"}, names


def run_all(module):
    """Every test_ function in the module, in order; exit 1 if any failed."""
    own = [n for n, f in vars(module).items() if n.startswith("test_") and callable(f)
           and f.__module__ == module.__name__]
    failed = []
    for name in sorted(own, key=lambda n: getattr(module, n).__code__.co_firstlineno):
        try:
            getattr(module, name)()
            print("  ok    %s" % name)
        except Exception:                               # noqa: BLE001 - reported, not hidden
            print("  FAIL  %s" % name)
            traceback.print_exc(file=sys.stdout)
            failed.append(name)
    print("FAILED - %d of %d" % (len(failed), len(own)) if failed
          else "PASSED - %d of %d" % (len(own), len(own)))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(run_all(sys.modules[__name__]))
