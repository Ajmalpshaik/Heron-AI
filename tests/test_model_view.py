# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-QA-BIM-011
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Model Dashboard's 3D view - what brain/heron_model_view.py makes of
REPORT_MODEL_GEOMETRY's geometryJson (docs/40 section 21.13).

    python tests/test_model_view.py

WHAT IT PROVES
  Geometry from another release, or that is not geometry, is refused with a
  sentence; a triangle naming a point its element does not have, or a point
  that is not a number, refuses the WHOLE read - a damaged model is never
  drawn half right; the same category gets the same colour every time and
  two categories never share one; what the triangle limit left out is said.

WHAT IT DOES NOT PROVE
  That the fragment reads a real model's shapes - its proof, owed in Revit.
  How the page draws them - test_companion and an eye.
"""

from __future__ import print_function

import ast
import copy
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests"))

import heron_model_view as V                                  # noqa: E402
from test_takeoff import run_all                              # noqa: E402


def box(ident, c, x, y, z, w=1.0, d=1.0, h=1.0):
    """An axis-aligned box: 8 points, 12 triangles."""
    v = []
    for dz in (0, h):
        for dy in (0, d):
            for dx in (0, w):
                v += [x + dx, y + dy, z + dz]
    t = [0, 1, 3, 0, 3, 2, 4, 6, 7, 4, 7, 5, 0, 4, 5, 0, 5, 1,
         2, 3, 7, 2, 7, 6, 0, 2, 6, 0, 6, 4, 1, 5, 7, 1, 7, 3]
    return {"id": str(ident), "c": c, "v": v, "t": t}


SAMPLE = {
    "format": 1, "scope": "active 3D view '{3D}'", "units": "m", "offset": [10.0, 20.0, 0.0],
    "categories": ["Walls", "Ducts", "Zebra Things"],
    "elements": [box(101, 0, 0, 0, 0, 10, 0.2, 3), box(102, 1, 1, 1, 2.5, 4, 0.4, 0.3),
                 box(103, 1, 1, 2, 2.5, 4, 0.4, 0.3), box(104, 2, 5, 5, 0)],
    "triangles": 48, "elementsRead": 6, "noGeometry": 1, "leftOut": 1,
    "notRead": [], "capped": ["triangles: the budget of 150000 was reached; 1 element(s) left out"],
}


def sample(**changes):
    data = copy.deepcopy(SAMPLE)
    data.update(changes)
    return json.dumps(data)


def test_refuses_what_is_not_geometry():
    for text in ("", "nope", "[]", json.dumps({"format": 2})):
        got = V.view(text)
        assert got["view"] is None and got["said"].startswith("No 3D view was made")


def test_a_damaged_triangle_refuses_the_whole_read():
    bad = copy.deepcopy(SAMPLE)
    bad["elements"][1]["t"][0] = 99
    got = V.view(json.dumps(bad))
    assert got["view"] is None and "naming a point it does not have" in got["said"]
    bad = copy.deepcopy(SAMPLE)
    bad["elements"][0]["v"][0] = "NaN"
    assert V.view(json.dumps(bad))["view"] is None
    bad = copy.deepcopy(SAMPLE)
    bad["elements"][0]["c"] = 7
    assert V.view(json.dumps(bad))["view"] is None


def test_counts_and_what_was_left_out():
    got = V.view(sample())
    view = got["view"]
    assert view["drawn"] == 4 and view["triangles"] == 48
    assert "1 element(s) were left out" in got["said"] and "Nothing was changed." in got["said"]
    assert view["legend"][0] == {"name": "Ducts", "colour": V.KNOWN["Ducts"], "count": 2,
                                 "volume": False}
    # A version-2 read has no materials: it still draws, by category only.
    assert view["hasMaterials"] is False and view["volumes"] == [] and view["rooms"] == 0
    assert all(e["m"] == -1 for e in view["elements"])


def _with_materials():
    data = copy.deepcopy(SAMPLE)
    data["categories"] = ["Rooms", "Walls", "Doors"]
    data["volumes"] = [0]
    data["materials"] = [{"name": "Concrete", "colour": "#808080", "transparency": 0},
                         {"name": "Glass", "colour": "#AACCEE", "transparency": 70},
                         {"name": "Broken", "colour": "red", "transparency": 400}]
    data["elements"] = [dict(box(201, 0, 0, 0, 0, 5, 4, 3), m=-1),
                        dict(box(202, 1, 0, 0, 0, 5, 0.2, 3), m=0),
                        dict(box(203, 2, 1, 0, 0, 1, 0.1, 2), m=[0] * 6 + [1] * 6)]
    data["triangles"] = 36
    return data


def test_materials_and_room_volumes_v3():
    # docs/40 s21.18: each face's material, and rooms drawn see-through.
    view = V.view(json.dumps(_with_materials()))["view"]
    assert view["volumes"] == [0] and view["rooms"] == 1
    assert [r["name"] for r in view["legend"] if r["volume"]] == ["Rooms"]
    assert view["materials"][1] == {"name": "Glass", "colour": "#aaccee", "alpha": 0.3}
    # A damaged colour keeps its place - indexes must not move - with the stand-in colour.
    assert view["materials"][2]["colour"] == V.NO_MATERIAL and view["materials"][2]["alpha"] == 0.0
    legend = {r["name"]: r["triangles"] for r in view["materialLegend"]}
    assert legend == {"Concrete": 18, "Glass": 6}, legend   # the room's volume is not counted
    assert view["hasMaterials"] is True
    assert "1 room(s) or space(s) drawn as see-through volumes" in V.view(
        json.dumps(_with_materials()))["said"]


def test_rooms_and_spaces_are_drawn_in_a_brand_colour():
    # D-149: the owner's "use brand colours for rooms and spaces". The palette
    # is read from docs/brand/README.md, as tools/check-brand.py reads it.
    import re
    brand = open(os.path.join(ROOT, "docs", "brand", "README.md"), encoding="utf-8").read()
    palette = {h.lower() for h in re.findall(r"`(#[0-9A-Fa-f]{6})`", brand)}
    assert V.KNOWN["Rooms"] in palette and V.KNOWN["Spaces"] in palette


def test_a_material_the_read_did_not_list_refuses_the_read():
    bad = _with_materials()
    bad["elements"][1]["m"] = 7
    assert "did not list" in V.view(json.dumps(bad))["said"]
    bad = _with_materials()
    bad["elements"][2]["m"] = [0, 1]          # not one a triangle
    assert "do not match its triangles" in V.view(json.dumps(bad))["said"]
    bad = _with_materials()
    bad["elements"][1]["m"] = True            # a boolean is not an index
    assert V.view(json.dumps(bad))["view"] is None


def test_an_empty_read_draws_nothing_and_is_not_refused():
    # The fragment's own negative: a section box clear of the building.
    got = V.view(json.dumps({"format": 1, "scope": "active 3D view 'Heron empty box'",
                             "units": "m", "offset": [0, 0, 0], "categories": [],
                             "elements": [], "triangles": 0, "elementsRead": 0,
                             "noGeometry": 0, "leftOut": 0, "notRead": [], "capped": []}))
    assert got["view"] is not None and got["view"]["drawn"] == 0 and got["view"]["legend"] == []
    assert "0 element(s), 0 triangle(s)" in got["said"]


def test_which_limit_stopped_the_read_is_said():
    timed = V.view(sample(stopped="time"))
    assert timed["view"]["stopped"] == "time" and "stopped itself before Revit's time limit" in timed["said"]
    budget = V.view(sample(stopped="triangles"))
    assert "triangle limit was reached" in budget["said"]
    odd = V.view(sample(stopped="whenever"))
    assert odd["view"]["stopped"] is None and "a limit was reached" in odd["said"]


def test_colours_are_stable_and_distinct():
    a = V.colours(["Walls", "Zebra Things", "Odd Category", "Ducts"])
    b = V.colours(["Walls", "Zebra Things", "Odd Category", "Ducts"])
    assert a == b and len(set(a)) == 4
    assert a[0] == V.KNOWN["Walls"] and a[3] == V.KNOWN["Ducts"]


def test_model_text_is_carried_as_data():
    view = V.view(sample(categories=["<b>x</b>", "Ducts", "Zebra Things"]))["view"]
    assert view["categories"][0]["name"] == "<b>x</b>"


def test_imports_only_the_standard_library():
    allowed = set(getattr(sys, "stdlib_module_names", ())) or {"json", "colorsys"}
    tree = ast.parse(open(os.path.join(ROOT, "brain", "heron_model_view.py")).read())
    names = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            names.update(alias.name.split(".")[0] for alias in node.names)
        elif isinstance(node, ast.ImportFrom):
            names.add((node.module or "").split(".")[0])
    assert names <= allowed | {"__future__"}, names


if __name__ == "__main__":
    sys.exit(run_all(sys.modules[__name__]))
