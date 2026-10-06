# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Every sprinkler checked against its Space (docs/46 section 13.2), and the fire
water put together with the sprinkler demand (section 13.1).

    python tests/test_sprinkler_spacing.py

WHAT IT PROVES
  Spacing: a 6 x 4 m office with four heads 3 m apart passes at 4.6 m and
  20.9 m2 and fails at 2.5 m; the same office turned 30 degrees, its pipes
  turned too, gives the same S and L, the angle read from its pipes; a Space
  with no hazard is "not checked", never ok; a class with no limits is asked
  once, with the class's own figures offered; a head in no Space is listed,
  not checked; a head outside its Space's outline refuses that Space only.
  Fire water: nothing included gives the sprinkler demand alone and says
  each part was not included; a standpipe running at the same time adds its
  flow and its higher pressure governs; not at the same time, it adds
  nothing; the tank is the engine's own water_storage on the same figures; a
  pump short of the total FAILS; a missing pump curve asks the pump only.

WHAT IT DOES NOT PROVE
  That a real model's Space outlines are read as format 1 says - that is
  NEEDS-CHECKING group CE.
"""

from __future__ import print_function

import copy
import math
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests"))

import heron_fire as FIRE                                     # noqa: E402
import heron_sprinkler_water as WATER                              # noqa: E402
import heron_sprinkler_run as R                               # noqa: E402
import heron_sprinkler_spacing as S                           # noqa: E402
import test_sprinkler_run as RUN                              # noqa: E402
import test_sprinkler_takeoff as NET                          # noqa: E402

STD = "NFPA 13-2022"
LIGHT = {"max_spacing_m": 4.6, "max_area_m2": 20.9, "max_wall_distance_m": 2.3}


def office(degrees=0.0, sid="7001"):
    """A 6 x 4 m office at z 3: two branch lines along its length, two heads on each."""
    t = math.radians(degrees)

    def at(x, y, z=3.0):
        return (round(x * math.cos(t) - y * math.sin(t), 6),
                round(x * math.sin(t) + y * math.cos(t), 6), z)

    c = NET._c
    # The riser to a tee at (0.2, 2); a cross main up to y 1 and down to y 3;
    # on each branch line a tee at the first head and a pipe on to the second.
    els = [NET._part("100", "equipment", None, None, c(0, at(-1, 2), ("201", 0)),
                     family="Alarm Valve", type="DN80")]
    els.append(NET._pipe("201", 50, 52.5, 1.2, c(0, at(-1, 2), ("100", 0)),
                         c(1, at(0.2, 2), ("301", 0))))
    els.append(NET._part("301", "fitting", "Tee", None, c(0, at(0.2, 2), ("201", 1)),
                         c(1, at(0.2, 2), ("202", 0)), c(2, at(0.2, 2), ("203", 0))))
    els.append(NET._pipe("202", 25, 26.6, 1.0, c(0, at(0.2, 2), ("301", 1)),
                         c(1, at(0.2, 1), ("211", 0))))
    els.append(NET._pipe("203", 25, 26.6, 1.0, c(0, at(0.2, 2), ("301", 2)),
                         c(1, at(0.2, 3), ("213", 0))))
    for y, tee, first, second, p1, p2, cross in ((1.0, "311", "401", "402", "211", "212", "202"),
                                                 (3.0, "313", "403", "404", "213", "214", "203")):
        els.append(NET._pipe(p1, 25, 26.6, 1.3, c(0, at(0.2, y), (cross, 1)),
                             c(1, at(1.5, y), (tee, 0))))
        els.append(NET._part(tee, "fitting", "Tee", None, c(0, at(1.5, y), (p1, 1)),
                             c(1, at(1.5, y), (p2, 0)), c(2, at(1.5, y), (first, 0))))
        els.append(NET._pipe(p2, 25, 26.6, 3.0, c(0, at(1.5, y), (tee, 1)),
                             c(1, at(4.5, y), (second, 0))))
        for hid, x, link in ((first, 1.5, (tee, 2)), (second, 4.5, (p2, 1))):
            h = NET._head(hid, "t1", at(x, y), link)
            h["at"] = list(at(x, y))
            h["space_id"] = sid
            els.append(h)
    outline = [list(at(x, y)[:2]) for x, y in ((0, 0), (6, 0), (6, 4), (0, 4), (0, 0))]
    return {"format": 1, "document": "t",
            "systems": [{"id": "9", "name": "FP 1", "classification": "FireProtectWet",
                         "chosen": True}],
            "system": {"id": "9", "name": "FP 1", "classification": "FireProtectWet",
                       "base_equipment": "100"},
            "elements": els,
            "spaces": [{"id": sid, "number": "101", "name": "Office", "level": "L1",
                        "area_m2": 24.0, "outline": outline}],
            "findings": []}


def spacing(n, hazard="light", limits=None, angle=None):
    inputs = {"hazard": {"7001": hazard} if hazard else {},
              "limits": {"light": limits if limits is not None else LIGHT}}
    if angle is not None:
        inputs["angle_deg"] = {"7001": angle}
    return S.check(n, inputs, STD)


def test_office_passes_and_fails():
    got = spacing(office())
    row = got["spaces"][0]
    assert row["status"] == "ok", row
    heads = row["answer"]["data"]["heads"]
    assert all(abs(h["s_m"] - 3.0) < 1e-6 and abs(h["l_m"] - 2.0) < 1e-6 for h in heads.values())
    assert row["angle_from"].startswith("the model's pipes") and row["model_angle_deg"] in (0.0, 180.0)
    tight = spacing(office(), limits=dict(LIGHT, max_spacing_m=2.5))
    assert tight["spaces"][0]["status"] == "fail" and "S 3.00 m" in tight["spaces"][0]["why"]


def test_turned_office_measures_the_same():
    got = spacing(office(30.0))
    row = got["spaces"][0]
    # Points cross from Revit to the millimetre, so the angle is read to about 0.01 degree.
    assert abs(row["model_angle_deg"] - 30.0) < 0.01, row["model_angle_deg"]
    assert row["status"] == "ok", row
    for h in row["answer"]["data"]["heads"].values():
        assert abs(h["s_m"] - 3.0) < 1e-3 and abs(h["l_m"] - 2.0) < 1e-3, h


def test_angle_along_y_is_ninety():
    angle, metres, how = S.branch_angle(office(90.0), S.spaces(office(90.0))[0]["7001"])
    assert abs(angle - 90.0) < 1e-6 and metres > 8 and "smallest pipe" in how


def grid(branch_deg, main_deg, branch_m, main_m, pieces=5):
    """Level pipes only: a DN50 main and DN25 branch lines, in a large Space."""
    c = NET._c
    els = []

    def line(prefix, deg, total, dn, inner, y0):
        t = math.radians(deg)
        step = total / pieces
        for i in range(pieces):
            a = (round(1.0 + i * step * math.cos(t), 6), round(y0 + i * step * math.sin(t), 6), 3.0)
            b = (round(1.0 + (i + 1) * step * math.cos(t), 6),
                 round(y0 + (i + 1) * step * math.sin(t), 6), 3.0)
            els.append(NET._pipe("%s%d" % (prefix, i), dn, inner, step, c(0, a), c(1, b)))
    line("8", main_deg, main_m, 50, 52.5, 20.0)
    line("9", branch_deg, branch_m, 25, 26.6, 20.0)
    n = office()
    n["elements"] = els + [e for e in n["elements"] if e["kind"] == "sprinkler"]
    n["spaces"][0]["outline"] = [[-40, -40], [40, -40], [40, 60], [-40, 60]]
    return n


def test_equal_main_and_branches_read_the_branches():
    # The review's case: 30 degrees x 10 m with 120 degrees x 10 m - a doubled-angle
    # mean gave 76.7; the grid is at 30, and the DN25 lines run at 30.
    n = grid(30.0, 120.0, 10.0, 10.0)
    angle, metres, how = S.branch_angle(n, S.spaces(n)[0]["7001"])
    assert abs(angle - 30.0) < 0.01 and abs(metres - 20.0) < 1e-3, (angle, how)
    n = grid(120.0, 30.0, 10.0, 10.0)
    angle, _m, _h = S.branch_angle(n, S.spaces(n)[0]["7001"])
    assert abs(angle - 120.0) < 0.01, angle


def test_a_spread_grid_is_asked_not_guessed():
    n = grid(30.0, 75.0, 10.0, 10.0)
    angle, _m, how = S.branch_angle(n, S.spaces(n)[0]["7001"])
    assert angle is None and "give the angle" in how
    got = spacing(n)
    assert got["spaces"][0]["status"] == "asked"
    assert any(a["input"] == "spacing.angle_deg.7001" for a in got["asked"])


def test_an_en_project_gets_en_classes():
    got = S.check(office(), {"hazard": {"7001": "OH1"}}, "BS EN 12845")
    assert "OH4" in got["classes"] and got["spaces"][0]["hazard"] != "ordinary hazard group 1"
    offer = [a for a in got["asked"] if a["input"].endswith("max_area_m2")][0]["offer"]
    assert "12845" in offer and "NFPA" not in offer


def test_farthest_point_is_in_model_coordinates():
    row = spacing(office(30.0))["spaces"][0]
    far = row["farthest"]
    # In the office's own frame the farthest point is a corner; turned 30 degrees about
    # the outline's first corner, it is no longer at (6, 4).
    corners = [S._turned_back(x * 1000.0, y * 1000.0, (0.0, 0.0), 30.0)
               for x, y in ((0, 0), (6, 0), (6, 4), (0, 4))]
    assert any(abs(far["x_m"] - cx) < 0.3 and abs(far["y_m"] - cy) < 0.3 for cx, cy in corners)
    assert not (abs(far["x_m"] - 6.0) < 0.3 and abs(far["y_m"] - 4.0) < 0.3)


def test_a_read_without_spaces_is_not_read():
    n = office()
    del n["spaces"]
    got = spacing(n)
    assert got["status"] == "not read" and not got["loose"]
    assert "read the model again" in S.summary_line(got)


def test_separation_lines_are_never_plain_ok():
    n = office()
    n["spaces"][0]["separation_edges"] = 1
    row = spacing(n)["spaces"][0]
    assert row["status"] == "check" and "separation lines" in row["why"]
    assert set(S.head_status({"spacing": spacing(n)}).values()) == {"check"}


def test_no_hazard_is_not_checked():
    got = spacing(office(), hazard=None)
    assert got["spaces"][0]["status"] == "not checked" and not got["asked"]


def test_limits_asked_once_with_the_class_figures():
    n = office()
    n["spaces"].append(dict(n["spaces"][0], id="7002", number="102"))
    n["elements"][-1]["space_id"] = "7002"
    got = S.check(n, {"hazard": {"7001": "light", "7002": "light"}}, STD)
    inputs = [a["input"] for a in got["asked"]]
    assert inputs.count("spacing.limits.light hazard.max_spacing_m") == 1
    offer = [a for a in got["asked"] if a["input"] == "spacing.limits.light hazard.max_area_m2"][0]
    assert "light hazard" in offer["offer"] and offer["required"]
    assert all(r["status"] == "asked" for r in got["spaces"])


def test_head_in_no_space_is_listed():
    n = office()
    n["elements"][-1]["space_id"] = None
    got = spacing(n)
    assert got["loose"] == ["404"] and "404" not in got["spaces"][0]["heads"]


def test_head_outside_its_outline_refuses_that_space():
    n = office()
    n["elements"][-1]["at"] = [9.0, 9.0, 3.0]
    got = spacing(n)
    assert got["spaces"][0]["status"] == "refused" and "outside" in got["spaces"][0]["why"]


def test_head_status_for_the_view():
    n = office()
    result = {"spacing": spacing(n, limits=dict(LIGHT, max_spacing_m=2.5))}
    marks = S.head_status(result)
    assert set(marks.values()) == {"fail"} and len(marks) == 4


# --- fire water -------------------------------------------------------------------

def solved():
    return R.run(NET.net(), RUN.given(operating=["401", "402"]))


QCDD = {"sprinkler_standard": STD, "fire_authority": "QCDD"}
STANDPIPE = {"standpipe_class": "i", "standpipes": 1, "first_flow_lpm": 1893,
             "outlet_pressure_bar": 6.9, "height_m": 30, "duration_min": 30}
PUMP = {"rated_flow_lpm": 2000, "rated_pressure_bar": 10, "churn_pressure_bar": 12,
        "pressure_at_150_bar": 7, "suction_pressure_bar": 0}


def test_nothing_included_is_said():
    r = solved()
    w = WATER.run(r, {}, QCDD)
    assert w["status"] == "ok" and w["total"]["flow_lpm"] == r["answer"]["data"]["total_lpm"]
    assert sum("not included by the modeller" in t for t in w["notes"]) == 4


def test_simultaneous_standpipe_adds_and_governs():
    r = solved()
    w = WATER.run(r, {"include": {"standpipe": True}, "simultaneous": {"standpipe": True},
                      "standpipe": STANDPIPE}, QCDD)
    sp = w["parts"]["standpipe"]["data"]
    assert abs(w["total"]["flow_lpm"] - (r["answer"]["data"]["total_lpm"] + 1893)) < 1e-9
    assert w["total"]["pressure_bar"] == sp["source_bar"] > r["answer"]["data"]["supply_bar"]
    assert any("not solved together" in t for t in w["notes"])
    apart = WATER.run(r, {"include": {"standpipe": True}, "standpipe": STANDPIPE}, QCDD)
    assert apart["total"]["flow_lpm"] == r["answer"]["data"]["total_lpm"]
    assert any("NOT running at the same time" in t for t in apart["notes"])


def test_tank_is_the_engines_own():
    r = solved()
    w = WATER.run(r, {"include": {"standpipe": True, "storage": True},
                      "simultaneous": {"standpipe": True}, "standpipe": STANDPIPE,
                      "storage": {"duration_min": 60}}, QCDD)
    d = r["answer"]["data"]
    by_hand = FIRE.run("water_storage", dict(QCDD, sprinkler_flow_lpm=d["demand_lpm"],
                                             duration_min=60, other_demands=[
                                                 {"name": "standpipes", "flow_lpm": 1893,
                                                  "duration_min": 30}]))
    assert w["parts"]["storage"]["data"]["total_m3"] == by_hand["data"]["total_m3"]


def test_short_pump_fails_and_missing_curve_asks_pump_only():
    r = solved()
    small = dict(PUMP, rated_flow_lpm=60, churn_pressure_bar=0.9, rated_pressure_bar=0.7,
                 pressure_at_150_bar=0.5)
    w = WATER.run(r, {"include": {"pump": True}, "pump": small}, QCDD)
    assert w["parts"]["pump"]["data"]["ok"] is False
    half = WATER.run(r, {"include": {"pump": True, "standpipe": True}, "pump": {},
                         "standpipe": STANDPIPE}, QCDD)
    assert half["status"] == "missing" and half["parts"]["standpipe"]["status"] == "ok"
    assert all(a["input"].startswith("water.pump.") for a in half["asked"])


def test_fire_authority_is_asked_once_and_used():
    r = solved()
    nfpa_only = {"sprinkler_standard": STD}
    w = WATER.run(r, {"include": {"standpipe": True, "pump": True},
                      "simultaneous": {"standpipe": True},
                      "standpipe": STANDPIPE, "pump": PUMP}, nfpa_only)
    asked = [a["input"] for a in w["asked"]]
    assert asked.count("standards.fire_authority") == 1
    assert w["status"] == "ok", "a standard asked once does not leave the parts unfinished"
    q = WATER.run(r, {"include": {"standpipe": True}, "standpipe": STANDPIPE}, QCDD)
    assert any("QCDD" in t for _lv, t in q["parts"]["standpipe"]["checks"])
    assert "sprinkler_standard" not in q["parts"]["standpipe"]["ignored"]


def test_hose_allowance_and_standpipe_may_be_the_same_water():
    r = R.run(NET.net(), RUN.given(operating=["401", "402"], criteria=dict(
        RUN.GIVEN["criteria"], hose_allowance_lpm=950)))
    w = WATER.run(r, {"include": {"standpipe": True}, "simultaneous": {"standpipe": True},
                      "standpipe": STANDPIPE}, QCDD)
    assert any("may be the same water" in t for t in w["notes"])


def test_fields_carry_offers_and_the_pump_suction_is_required():
    f = WATER.fields()
    pump = dict((x["input"], x) for x in f["pump"])
    assert pump["suction_pressure_bar"]["required"] is True
    sp = dict((x["input"], x) for x in f["standpipe"])
    assert "30 min" in (sp["duration_min"]["offer"] or "")
    assert "sprinkler system's source" in sp["height_m"]["why"]
    assert sp["first_flow_lpm"]["offer"]


def test_a_bad_section_refuses_that_section_only():
    r = R.run(NET.net(), dict(RUN.given(operating=["401", "402"]), water="not a map",
                              spacing=["nor", "this"]))
    assert r["status"] == "ok"
    assert r["water"]["status"] == "refused" and r["spacing"]["status"] in ("refused", "not read")


def test_a_space_name_is_escaped_on_the_sheet():
    import heron_sprinkler_report as REPORT
    n = office()
    n["spaces"][0]["name"] = "<script>alert(1)</script>"
    r = R.run(n, {"spacing": {"hazard": {"7001": "light"}, "limits": {"light hazard": LIGHT}}})
    page = REPORT.html(r, n)
    assert "<script>alert" not in page and "&lt;script&gt;alert" in page


def test_not_solved_means_no_water():
    w = WATER.run(R.run(NET.net(), {}), {"include": {"pump": True}}, QCDD)
    assert w["status"] == "not solved" and not w["parts"]


def test_the_run_carries_both():
    n = office()
    given = RUN.given(operating=["401", "402"], fittings={
        "tee or cross, flow turned 90 degrees|DN25": 0},
        spacing={"hazard": {"7001": "light"}, "limits": {"light hazard": LIGHT}},
        water={"include": {"pump": True}, "pump": PUMP})
    r = R.run(n, given)
    assert r["status"] == "ok", (r["refused"], r["asked"])
    assert r["spacing"]["spaces"][0]["status"] == "ok"
    assert r["water"]["status"] == "ok" and r["water"]["parts"]["pump"]["status"] == "ok"
    text = R.summary_text(r)
    assert "Spacing: 1 Space(s) with heads - 1 ok" in text and "Fire water:" in text
    kept = R.carried(r, {"water": {"pump": {"churn_pressure_bar": ""}}})
    assert kept["spacing"]["hazard"]["7001"] == "light"
    assert "churn_pressure_bar" not in kept["water"]["pump"]
    assert kept["water"]["pump"]["rated_flow_lpm"] == 2000
    assert kept["water"]["include"]["pump"] is True


def test_spacing_runs_before_anything_is_solved():
    r = R.run(office(), {"spacing": {"hazard": {"7001": "light"},
                                     "limits": {"light hazard": LIGHT}}})
    assert r["status"] == "missing" and r["spacing"]["spaces"][0]["status"] == "ok"
    assert r["water"]["status"] == "not solved"


if __name__ == "__main__":
    sys.exit(NET.run_all(sys.modules[__name__]))
