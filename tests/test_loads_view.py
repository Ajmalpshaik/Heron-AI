# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Companion's 3D view of a building's loads (docs/44 section 12): the same
faces the loads were worked out from, each with what it counted as.

    python tests/test_loads_view.py

WHAT IT PROVES
  On an L-shaped building of three offices with real outlines: every face is
  drawn and every opening sits on its wall at its own size; a wall between two
  Spaces is drawn as no load; True North turns the facing colours, and the
  compass leans the way Revit's Site tab says (FRAGMENT-ISSUES 5b-345); a Space
  refused for a missing U-value shows refused, on the face that caused it;
  the load colours come from the run; a face Revit gave no outline is counted
  and said, not drawn; glass by the way it faces is added up from the
  take-off; Spaces that share no wall are a check; the module imports the
  standard library only.

WHAT IT DOES NOT PROVE
  That REPORT_SPACE_ENVELOPE outlines a real model's faces the way this
  building is outlined - that is Group CC, on a named model.
"""

from __future__ import print_function

import copy as _copy
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests"))

import heron_building_loads as B                              # noqa: E402
import heron_loads_view as V                                  # noqa: E402
import heron_takeoff as T                                     # noqa: E402
from test_takeoff import run_all                              # noqa: E402
from test_building_loads import PROJECT, OFFICE               # noqa: E402

HEIGHT = 3.0
ROOMS = {"A": ("101", (0.0, 0.0, 6.0, 5.0)), "B": ("102", (6.0, 0.0, 12.0, 5.0)),
         "C": ("103", (0.0, 5.0, 6.0, 10.0))}
WINDOWS = {("A", "S"): (2.0, 1.5), ("B", "S"): (3.0, 1.5), ("B", "E"): (2.0, 1.5),
           ("C", "W"): (2.0, 1.5)}


def _beside(rooms, name, side):
    x0, y0, x1, y1 = rooms[name][1]
    for other, (_id, (a0, b0, a1, b1)) in rooms.items():
        if other == name:
            continue
        if ((side == "E" and a0 == x1 and (b0, b1) == (y0, y1))
                or (side == "W" and a1 == x0 and (b0, b1) == (y0, y1))
                or (side == "N" and b0 == y1 and (a0, a1) == (x0, x1))
                or (side == "S" and b1 == y0 and (a0, a1) == (x0, x1))):
            return other
    return None


def l_building(true_north=0.0, rooms=None):
    """Three offices in an L on one level, 3 m high, as REPORT_SPACE_ENVELOPE would read them.

    A (0-6 x 0-5 m) with B east of it and C north of it: A's east and north
    walls are shared; every other wall is to outside. Windows on A south, B
    south, B east and C west. A roof over each; each floor finds nothing below.
    """
    rooms = rooms or ROOMS
    H = HEIGHT
    spaces = []
    for name, (sid, (x0, y0, x1, y1)) in sorted(rooms.items()):
        walls = {"S": ([[x0, y0, 0], [x1, y0, 0], [x1, y0, H], [x0, y0, H]], [0.0, -1.0, 0.0],
                       x1 - x0),
                 "E": ([[x1, y0, 0], [x1, y1, 0], [x1, y1, H], [x1, y0, H]], [1.0, 0.0, 0.0],
                       y1 - y0),
                 "N": ([[x1, y1, 0], [x0, y1, 0], [x0, y1, H], [x1, y1, H]], [0.0, 1.0, 0.0],
                       x1 - x0),
                 "W": ([[x0, y1, 0], [x0, y0, 0], [x0, y0, H], [x0, y1, H]], [-1.0, 0.0, 0.0],
                       y1 - y0)}
        faces = []
        for side, (loop, normal, run) in sorted(walls.items()):
            other = _beside(rooms, name, side)
            openings = []
            if (name, side) in WINDOWS and other is None:
                w, h = WINDOWS[(name, side)]
                mid = [(loop[0][k] + loop[1][k]) / 2.0 for k in range(3)]
                openings.append({"element": "%s%s-win" % (sid, side), "kind": "window",
                                 "type": "g", "area_m2": w * h,
                                 "centre": [mid[0], mid[1], 1.5], "width_m": w, "height_m": h})
            faces.append({"element": "%s%s" % (sid, side), "type": "w", "link": None,
                          "side": "wall", "normal": normal, "area_m2": run * H,
                          "beyond": "space" if other else "outside",
                          "beyond_space": rooms[other][0] if other else None,
                          "loops": [loop], "openings": openings})
        area = (x1 - x0) * (y1 - y0)
        faces.append({"element": sid + "R", "type": "r", "link": None, "side": "top",
                      "normal": [0.0, 0.0, 1.0], "area_m2": area, "beyond": "outside",
                      "beyond_space": None, "openings": [],
                      "loops": [[[x0, y0, H], [x1, y0, H], [x1, y1, H], [x0, y1, H]]]})
        faces.append({"element": sid + "F", "type": "f", "link": None, "side": "bottom",
                      "normal": [0.0, 0.0, -1.0], "area_m2": area, "beyond": "unknown",
                      "beyond_space": None, "openings": [],
                      "loops": [[[x0, y1, 0], [x1, y1, 0], [x1, y0, 0], [x0, y0, 0]]]})
        spaces.append({"id": sid, "unique_id": "u" + sid, "number": sid,
                       "name": "Office %s" % name, "level": "Level 1", "area_m2": area,
                       "volume_m3": area * H, "height_m": H, "zone": "Zone 1",
                       "space_type": "Office", "placed": True,
                       "current": {"Design Cooling Load": "0.00 W",
                                   "Design Heating Load": "0.00 W",
                                   "Specified Supply Airflow": "0.0 L/s"},
                       "terminals": [], "faces": faces})
    return {"format": 1, "document": "L-shaped office (test)",
            "units": {"power": "W", "airflow": "L/s"},
            "site": {"latitude_deg": 25.28, "longitude_deg": 51.53, "utc_offset_h": 3.0,
                     "elevation_m": 10.0, "project_to_true_north_deg": true_north,
                     "named": "Doha"},
            "types": {"w": {"name": "Basic Wall: Ext 300", "category": "Walls",
                            "u_w_m2k": 0.45, "shgc": None, "absorptance": 0.7},
                      "g": {"name": "Window: 2000 x 1500", "category": "Windows",
                            "u_w_m2k": 2.8, "shgc": 0.35, "absorptance": None},
                      "r": {"name": "Basic Roof: Flat 250", "category": "Roofs",
                            "u_w_m2k": 0.3, "shgc": None, "absorptance": 0.6},
                      "f": {"name": "Floor: Slab 200", "category": "Floors",
                            "u_w_m2k": 0.5, "shgc": None, "absorptance": None}},
            "spaces": spaces, "findings": []}


# Each office's floor finds nothing below it: the modeller answers "ground".
ANSWERED = dict(("beyond:%sF" % sid, "ground") for sid, _r in ROOMS.values())
ANSWERED["beyond:104F"] = "ground"
GROUNDED = dict(PROJECT, **ANSWERED)


def view(d=None, run=True):
    t = T.read(d or l_building())
    said = B.answers(GROUNDED)
    return V.build(t, B.run(t, GROUNDED, {"Office": OFFICE}) if run else None, said)


def faces_of(v, element):
    return [f for f in v["faces"] if f["element"] == element]


def test_every_face_is_drawn_with_its_outline():
    v = view()
    walls = [f for f in v["faces"] if not f["opening"]]
    openings = [f for f in v["faces"] if f["opening"]]
    assert len(walls) == 3 * 6 and len(openings) == 4, (len(walls), len(openings))
    assert v["levels"] == ["Level 1"] and not v["notes"]
    assert all(re.match(r"^#[0-9a-f]{6}$", c) for f in v["faces"] for c in f["colours"].values())
    keys = set(m["key"] for m in v["modes"])
    assert all(set(f["colours"]) == keys for f in v["faces"])


def test_a_wall_between_two_spaces_is_drawn_as_no_load():
    v = view()
    shared = faces_of(v, "101E")[0]
    assert shared["category"] == "wall_between_spaces" and shared["used_as"].startswith("no load")
    assert shared["beyond_space"] == "102 Office B"
    assert faces_of(v, "101S")[0]["category"] == "outside_wall"
    assert faces_of(v, "101R")[0]["category"] == "roof"
    floor = faces_of(v, "101F")[0]
    assert floor["category"] == "on_ground" and "heating loss only" in floor["used_as"]
    assert "as the modeller answered" in floor["used_as"]
    unanswered = faces_of(V.build(T.read(l_building())), "101F")[0]
    assert unanswered["used_as"].startswith("NOT COUNTED YET")
    assert unanswered["category"] == "floor_unknown"


def test_an_opening_sits_on_its_wall_at_its_own_size():
    v = view()
    win = faces_of(v, "101S-win")[0]
    pts = win["loops"][0]
    ys = set(round(p[1], 6) for p in pts)
    assert len(ys) == 1, ys                                   # flat on the wall's plane
    wall_y = faces_of(v, "101S")[0]["loops"][0][0][1]
    assert abs(list(ys)[0] - (wall_y - V.PROUD_M)) < 1e-9      # drawn just outside it
    xs, zs = [p[0] for p in pts], [p[2] for p in pts]
    assert abs((max(xs) - min(xs)) - 2.0) < 1e-9 and abs((max(zs) - min(zs)) - 1.5) < 1e-9
    assert abs((max(xs) + min(xs)) / 2 - 3.0) < 1e-9 and abs((max(zs) + min(zs)) / 2 - 1.5) < 1e-9
    assert win["area_m2"] == 3.0 and win["used_as"].startswith("glass")


def test_a_panel_cut_to_its_face_says_how_much_was_counted():
    # FRAGMENT-ISSUES 5b-330: a storey-high panel counts only its part on the
    # Space's face. The view draws the whole panel, as Revit has it, and says
    # how much of it the load counted - so the take-off can still be checked.
    from test_takeoff import curtain_office, SOUTH, STOREY
    t = T.read(curtain_office(STOREY, SOUTH))
    v = V.build(t, B.run(t, PROJECT, {"Office": OFFICE}))
    first = faces_of(v, 61)[0]
    assert first["area_m2"] == 10.0, first
    assert "9.20 m2 of its 10.00 m2" in first["used_as"], first["used_as"]
    xs = [p[0] for p in first["loops"][0]]
    assert abs((max(xs) - min(xs)) - 2.5) < 1e-9                # drawn whole
    win = faces_of(view(), "101S-win")[0]
    assert " of its " not in win["used_as"], win["used_as"]      # a window is counted whole


def test_true_north_turns_the_facing_colours():
    south = faces_of(view(), "101S")[0]
    assert south["facing"] == "S" and south["facing_deg"] == 180.0
    # True North 90 East of project north (Revit's stored +90) puts true north
    # where project east is, so a face to project south faces true east - the
    # sign measured 2026-10-06 (FRAGMENT-ISSUES 5b-345). This read W, 270, until then.
    turned = faces_of(view(l_building(true_north=90.0)), "101S")[0]
    assert turned["facing"] == "E" and turned["facing_deg"] == 90.0
    assert turned["colours"]["facing"] != south["colours"]["facing"]


def test_a_refused_space_shows_refused_on_the_face_that_caused_it():
    d = l_building()
    d["types"]["bare"] = {"name": "Basic Wall: Generic", "category": "Walls", "u_w_m2k": None,
                          "shgc": None, "absorptance": None}
    c = [s for s in d["spaces"] if s["id"] == "103"][0]
    for f in c["faces"]:
        if f["element"] == "103N":
            f["type"] = "bare"
    v = view(d)
    bad = faces_of(v, "103N")[0]
    assert bad["used_as"].startswith("REFUSED") and bad["colours"]["u"] == V.NO_U[0]
    assert v["spaces"]["103"]["status"] == "refused"
    refused = dict((k, c) for k, c, _l in V.STATUS)["refused"]
    assert all(f["colours"]["status"] == refused for f in v["faces"]
               if f["space"] == "103" and not f["opening"])
    assert v["spaces"]["101"]["status"] == "ok"


def test_the_load_colours_come_from_the_run():
    v = view()
    loads = dict((sid, sp["w_per_m2"]) for sid, sp in v["spaces"].items())
    high = max(loads, key=loads.get)
    low = min(loads, key=loads.get)
    assert high != low
    assert faces_of(v, "%sR" % high)[0]["colours"]["cooling"] == V.RAMP[-1]
    assert faces_of(v, "%sR" % low)[0]["colours"]["cooling"] == V.RAMP[0]
    unrun = view(run=False)
    assert all(f["colours"]["cooling"] == V.NOT_LOADED[0] for f in unrun["faces"]
               if not f["opening"])
    assert unrun["spaces"]["101"]["status"] == "not calculated"


def test_a_face_with_no_outline_is_counted_and_said_not_drawn():
    d = l_building()
    d["spaces"][0]["faces"][0]["loops"] = []
    d["spaces"][0]["faces"][1].pop("loops")
    v = view(d)
    assert any("2 face(s) came with no outline" in n for n in v["notes"]), v["notes"]
    d = l_building()
    south = [f for f in d["spaces"][0]["faces"] if f["element"] == "101S"][0]
    south["openings"][0]["centre"] = None                            # A's south window
    assert any("1 window(s) or door(s)" in n for n in view(d)["notes"])


def test_glass_is_added_up_by_the_way_it_faces():
    s = T.summary(T.read(l_building()))
    g = s["glass_by_facing"]
    assert abs(g["S"]["glass_m2"] - (2.0 * 1.5 + 3.0 * 1.5)) < 1e-9 and abs(g["S"]["wall_m2"] - 36.0) < 1e-9
    assert abs(g["E"]["glass_m2"] - 3.0) < 1e-9 and abs(g["E"]["wall_m2"] - 30.0) < 1e-9
    assert g["N"]["glass_m2"] == 0.0 and abs(g["N"]["glass_pct_of_wall"]) < 1e-9
    assert abs(s["glass_pct_of_floor"] - 100.0 * 13.5 / 90.0) < 1e-9
    assert s["levels"] == {"Level 1": {"spaces": 3, "area_m2": 90.0}}


def test_spaces_that_share_no_wall_are_a_check():
    assert not [f for f in T.qa(T.read(l_building())) if "groups" in f["text"]]
    rooms = dict(ROOMS, D=("104", (20.0, 0.0, 25.0, 5.0)))
    found = [f for f in T.qa(T.read(l_building(rooms=rooms))) if "groups" in f["text"]]
    assert len(found) == 1 and "form 2 groups" in found[0]["text"] and found[0]["level"] == "WARN"


def test_the_l_building_runs_and_its_block_is_below_its_peaks():
    t = T.read(l_building())
    asked = [a["input"] for a in B.needs(t, PROJECT, {"Office": OFFICE})]
    assert asked == ["beyond:101F", "beyond:102F", "beyond:103F"], asked
    r = B.run(t, GROUNDED, {"Office": OFFICE})
    assert [s["status"] for s in r["spaces"]] == ["ok", "ok", "ok"], [s["why"] for s in r["spaces"]]
    assert r["building"]["block_w"] < r["building"]["sum_of_peaks_w"]


def test_the_compass_comes_from_the_same_rule_as_the_facing():
    # Second review N6: the page worked True North out with its own formula.
    # The brain sends the north direction, made by heron_takeoff.azimuth_deg,
    # so a sign fixed there turns the compass with the facing colours.
    import math
    for north in (0.0, 20.0, 90.0, -30.0):
        v = view(l_building(true_north=north))
        x, y = v["north_xy"]
        assert abs(T.azimuth_deg([x, y, 0.0], north)) % 360.0 < 1e-6 or \
            abs(T.azimuth_deg([x, y, 0.0], north) - 360.0) < 1e-6, (north, v["north_xy"])
        assert abs(math.hypot(x, y) - 1.0) < 1e-9


def test_the_compass_points_where_the_site_tab_says_true_north_is():
    # The check above holds whichever way the sign runs. This one is the
    # measurement (FRAGMENT-ISSUES 5b-345, 2026-10-06): Revit's stored +30 is
    # "30.00 deg East", so the arrow leans 30 degrees toward project east; a
    # stored -12.5, "12.5 West", leans it toward project west.
    import math
    for north, side in ((30.0, 1.0), (-12.5, -1.0)):
        x, y = view(l_building(true_north=north))["north_xy"]
        lean = math.radians(abs(north))
        assert abs(x - side * math.sin(lean)) < 1e-6 and abs(y - math.cos(lean)) < 1e-6, \
            (north, x, y)


def test_glass_refused_only_where_it_is_counted():
    # Second review m4: glass in a wall between two Spaces carries no load, so
    # a missing SHGC there refuses nothing.
    d = l_building()
    d["types"]["g2"] = {"name": "Window: Internal", "category": "Windows", "u_w_m2k": 2.8,
                        "shgc": None, "absorptance": None}
    a = [s for s in d["spaces"] if s["id"] == "101"][0]
    shared = [f for f in a["faces"] if f["element"] == "101E"][0]
    shared["openings"].append({"element": "101E-int", "kind": "window", "type": "g2",
                               "area_m2": 2.0, "centre": [6.0, 2.5, 1.5], "width_m": 2.0,
                               "height_m": 1.0})
    v = view(d)
    inner = faces_of(v, "101E-int")[0]
    assert not inner["used_as"].startswith("REFUSED"), inner["used_as"]
    assert v["spaces"]["101"]["status"] == "ok"


def test_imports_only_the_standard_library():
    import ast
    allowed = set(getattr(sys, "stdlib_module_names", ())) | {"__future__", "heron_takeoff"}
    tree = ast.parse(open(os.path.join(ROOT, "brain", "heron_loads_view.py")).read())
    names = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            names.update(alias.name.split(".")[0] for alias in node.names)
        elif isinstance(node, ast.ImportFrom):
            names.add((node.module or "").split(".")[0])
    assert names <= allowed, names - allowed


if __name__ == "__main__":
    sys.exit(run_all(sys.modules[__name__]))
