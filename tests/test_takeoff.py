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


def test_a_face_with_nothing_found_beyond_is_asked_never_guessed():
    # Review C1: a face with nothing found beyond it was counted as a wall to an
    # unconditioned space - a slab between two storeys added a third to a Space's
    # cooling. It is a QUESTION now, per element, and nothing is assumed.
    d = copy()
    d["spaces"][0]["faces"][1]["beyond"] = "unknown"           # the roof element 12
    t = T.read(d)
    s = T.surfaces(t, t.spaces[0])
    assert s["roofs"] == [] and s["partitions"] == []
    assert any("12" in r and "say what is there" in r for r in s["refused"]), s["refused"]
    assert T.unknowns(t) and "12" in T.unknowns(t)
    roofs = T.surfaces(t, t.spaces[0], {"12": "outside"})["roofs"]
    assert [r["name"] for r in roofs] == ["Roof (12)"]
    above = T.surfaces(t, t.spaces[0], {"12": "conditioned"})
    assert above["roofs"] == [] and above["partitions"] == [] and not above["refused"]
    grounded = copy()
    grounded["spaces"][0]["faces"][1].update(side="bottom", beyond="unknown")
    g = T.read(grounded)
    assert T.surfaces(g, g.spaces[0], {"12": "ground"})["floors"]


def test_a_face_bounded_by_an_unread_link_says_so():
    # Review I6: it said "type None (None) has no U-value".
    d = copy()
    d["spaces"][0]["faces"][1].update(element=None, type=None, beyond="unknown")
    t = T.read(d)
    said = " ".join(T.surfaces(t, t.spaces[0])["refused"])
    assert "None" not in said and "linked model" in said and "links included" in said
    assert any(f["level"] == "FAIL" and "linked model" in f["text"] for f in T.qa(t))


def test_a_placed_space_with_no_faces_is_a_fail():
    # Review I5c: a Space whose faces Revit could not work out ran on its
    # internal gains alone.
    d = copy()
    d["spaces"][0]["faces"] = []
    assert any(f["level"] == "FAIL" and f["space"] == 1 and "no faces" in f["text"]
               for f in T.qa(T.read(d)))


def test_glass_in_a_wall_that_is_not_exterior_is_a_check():
    # Review I5e: a facade wall whose type is not Exterior files its windows
    # as glass to an unconditioned space - no sun at all.
    d = copy()
    d["spaces"][0]["faces"][0]["beyond"] = "unconditioned"
    assert any(f["level"] == "WARN" and "Function" in f["text"] and "Exterior" in f["text"]
               for f in T.qa(T.read(d)))


def test_a_face_nothing_bounds_is_not_blamed_on_links():
    # Second review N4: a Space face Revit gave no bounding element for, and a
    # face bounded by an unread link, were the same in the take-off.
    d = copy()
    d["spaces"][0]["faces"][1].update(element=None, type=None, beyond="unknown")
    d["spaces"][0]["faces"][1]["bounded_by"] = "nothing"
    t = T.read(d)
    said = " ".join(T.surfaces(t, t.spaces[0])["refused"])
    assert "links" not in said and "no element bounds" in said, said
    d["spaces"][0]["faces"][1]["bounded_by"] = "unread link"
    d["spaces"][0]["faces"][1]["link"] = "Architecture.rvt"
    said = " ".join(T.surfaces(T.read(d), T.read(d).spaces[0])["refused"])
    assert "Architecture.rvt" in said and "links included" in said, said


def test_a_placed_space_with_nothing_above_it_is_a_fail():
    d = copy()
    d["spaces"][0]["faces"] = [f for f in d["spaces"][0]["faces"] if f["side"] != "top"]
    assert any(f["level"] == "FAIL" and f["space"] == 1 and "above" in f["text"]
               for f in T.qa(T.read(d)))


def test_a_curtain_walls_frames_need_no_u_of_its_own():
    # Second review N5: what is left of a curtain wall's face between its
    # panels needed a U on the curtain wall type, which such types do not carry.
    d = copy()
    d["types"]["cw"] = {"name": "Curtain Wall: Storefront", "category": "Walls",
                        "u_w_m2k": None, "shgc": None, "absorptance": None}
    d["types"]["pn"] = {"name": "System Panel: Glazed", "category": "Curtain Panels",
                        "u_w_m2k": 2.0, "shgc": 0.3, "absorptance": None}
    d["spaces"][0]["faces"][0].update(type="cw", curtain=True, area_m2=13.5, openings=[
        {"element": 50 + i, "kind": "curtain_panel", "type": "pn", "area_m2": 4.4}
        for i in range(3)])                                         # 13.2 of 13.5 m2 glass
    t = T.read(d)
    s = T.surfaces(t, t.spaces[0])
    assert not s["refused"], s["refused"]
    frames = [w for w in s["windows"] if "frames" in w["name"]]
    assert frames and abs(frames[0]["area_m2"] - 0.3) < 1e-9 and frames[0]["shgc"] == 0.0
    assert frames[0]["u_w_m2k"] == 2.0 and s["assumed"], s


def curtain_office(panels, face_loop, height=3.775):
    """One office behind a storey-high curtain wall, as REPORT_SPACE_ENVELOPE gives it: the
    face stops at the Space's top, the panels run the storey. No `curtain` key on the face -
    the fragment never writes one."""
    d = copy()
    d["types"]["cw"] = {"name": "Curtain Wall: Exterior Glazing", "category": "Walls",
                        "u_w_m2k": None, "shgc": None, "absorptance": None}
    d["types"]["pn"] = {"name": "System Panel: Glazed", "category": "Curtain Panels",
                        "u_w_m2k": 6.7, "shgc": 0.86, "absorptance": None}
    xs = [p[0] for p in face_loop]
    zs = [p[2] for p in face_loop]
    d["spaces"][0]["height_m"] = height
    d["spaces"][0]["faces"][0] = {
        "element": 60, "type": "cw", "side": "wall", "normal": [0.0, -1.0, 0.0],
        "area_m2": round((max(xs) - min(xs)) * (max(zs) - min(zs)), 6),
        "beyond": "outside", "beyond_space": None, "loops": [face_loop],
        "openings": [{"element": 61 + i, "kind": "curtain_panel", "type": "pn",
                      "area_m2": w * h, "centre": [x, 0.0, z], "width_m": w, "height_m": h}
                     for i, (x, z, w, h) in enumerate(panels)]}
    return d


SOUTH = [[18.0, 0.0, 3.775], [12.062, 0.0, 3.775], [12.062, 0.0, 0.0], [18.0, 0.0, 0.0]]
STOREY = [(13.25, 2.0, 2.5, 4.0), (15.75, 2.0, 2.5, 4.0), (17.5, 2.0, 1.0, 4.0)]


def test_a_curtain_panel_counts_only_the_part_on_the_spaces_face():
    # FRAGMENT-ISSUES 5b-330: the panels run the storey, 4.0 m, past the slab's
    # underside where the Space stops (3.775 m), and the first runs 62 mm past
    # the wall at its side - 24.00 m2 of panels on a 22.42 m2 face, and the
    # Space was refused. Only the part of each panel on the face counts.
    t = T.read(curtain_office(STOREY, SOUTH))
    s = T.surfaces(t, t.spaces[0])
    assert not s["refused"], s["refused"]
    got = sorted(round(w["area_m2"], 4) for w in s["windows"])
    assert got == [3.775, 9.2035, 9.4375], got           # 1.0, 2.438 and 2.5 m, 3.775 m high
    assert abs(sum(got) - 22.41595) < 1e-3
    assert not [w for w in s["windows"] if "frames" in w["name"]]   # all glass, nothing left
    assert not [f for f in T.qa(t) if "larger than" in f["text"]], T.qa(t)


def test_a_curtain_walls_mullions_are_its_frames_on_a_real_takeoff():
    # The frames rule (second review N5) waited for a `curtain` key the
    # fragment never writes, so on a real model a curtain wall WITH mullions
    # left its mullions to the curtain wall type - which carries no U - and
    # refused its Space. A face with curtain panels in it is a curtain wall's.
    narrow = [(13.25, 2.0, 2.4, 3.9), (15.75, 2.0, 2.4, 3.9), (17.5, 2.0, 0.9, 3.9)]
    t = T.read(curtain_office(narrow, SOUTH))
    s = T.surfaces(t, t.spaces[0])
    assert not s["refused"], s["refused"]
    frames = [w for w in s["windows"] if "frames" in w["name"]]
    glass = sum(w["area_m2"] for w in s["windows"] if "frames" not in w["name"])
    assert frames and frames[0]["shgc"] == 0.0 and frames[0]["u_w_m2k"] == 6.7, frames
    assert abs(frames[0]["area_m2"] + glass - 22.41595) < 1e-3, (frames, glass)


def test_a_panel_across_two_spaces_counts_its_part_on_each():
    # The take-off lists a panel once, under the face its middle is on; a
    # panel across the partition between two Spaces lies on both faces, and
    # each Space counts its own part of it - none counted twice, none lost.
    d = curtain_office([(2.0, 1.5, 4.0, 3.0), (6.0, 1.5, 4.0, 3.0), (9.0, 1.5, 2.0, 3.0)],
                       [[4.9, 0.0, 3.0], [0.0, 0.0, 3.0], [0.0, 0.0, 0.0], [4.9, 0.0, 0.0]],
                       height=3.0)
    other = _copy.deepcopy(d["spaces"][0])
    other.update(id=2, unique_id="u2", number="2", name="Office 02")
    other["faces"][0].update(area_m2=14.7, loops=[[[10.0, 0.0, 3.0], [5.1, 0.0, 3.0],
                                                   [5.1, 0.0, 0.0], [10.0, 0.0, 0.0]]])
    mine = d["spaces"][0]["faces"][0]["openings"]
    d["spaces"][0]["faces"][0]["openings"] = mine[:1]               # its middle is on Office 01
    other["faces"][0]["openings"] = mine[1:]                        # these two on Office 02
    d["spaces"].append(other)
    t = T.read(d)
    first, second = (T.surfaces(t, sp) for sp in t.spaces)
    assert not first["refused"] and not second["refused"], (first["refused"], second["refused"])
    assert abs(sum(w["area_m2"] for w in first["windows"]) - 14.7) < 1e-6     # 12 + 0.9 x 3
    assert abs(sum(w["area_m2"] for w in second["windows"]) - 14.7) < 1e-6    # 2.9 x 3 + 6
    assert not [f for f in T.qa(t) if "larger than" in f["text"]]


def test_a_window_is_counted_whole_and_too_big_still_refuses():
    # Only a curtain panel's outline is exact. A window sits by the middle of
    # its box, which a frame or a sill moves, so it is never cut - and one
    # larger than its face is still a FAIL: the refusal for a real mismatch.
    d = copy()
    d["spaces"][0]["faces"][0]["loops"] = [[[0.0, 5.0, 2.7], [0.0, 0.0, 2.7],
                                            [0.0, 0.0, 0.0], [0.0, 5.0, 0.0]]]
    d["spaces"][0]["faces"][0]["openings"][0].update(centre=[0.0, 2.5, 2.6], width_m=1.0,
                                                     height_m=2.0)
    t = T.read(d)
    assert T.surfaces(t, t.spaces[0])["windows"][0]["area_m2"] == 2.0
    d["spaces"][0]["faces"][0]["openings"][0]["area_m2"] = 14.0
    assert any(f["level"] == "FAIL" and "larger than" in f["text"] for f in T.qa(T.read(d)))


def test_the_glass_by_facing_is_the_glass_on_the_faces():
    t = T.read(curtain_office(STOREY, SOUTH))
    south = T.summary(t)["glass_by_facing"]["S"]
    assert abs(south["glass_m2"] - 22.41595) < 1e-3, south
    assert south["glass_pct_of_wall"] <= 100.0 + 1e-6, south


def test_the_answers_count_wherever_beyond_is_read():
    # Second review m5: a face answered "outside" is an outside face for the
    # INFO line and the glass by facing too.
    d = copy()
    d["spaces"][0]["faces"][0]["beyond"] = "unknown"
    d["spaces"][0]["faces"][1]["beyond"] = "unknown"
    t = T.read(d)
    said = {"10": "outside", "12": "outside"}
    assert not [f for f in T.qa(t, said) if "no outside face" in f["text"]]
    assert T.summary(t, said)["glass_by_facing"]["W"]["glass_m2"] == 2.0


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
