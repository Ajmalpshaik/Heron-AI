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
    assert view["legend"][0] == {"name": "Ducts", "colour": V.KNOWN["Ducts"], "count": 2}


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
