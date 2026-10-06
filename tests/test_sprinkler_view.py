# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Sprinkler panel's 3D data and the hydraulic calculation sheet
(docs/46 sections 7 and 8).

    python tests/test_sprinkler_view.py

WHAT IT PROVES
  Every pipe is a segment and every head a point; before a solve only the
  size and remote-area modes are offered; after it, an operating head and a
  dry one differ in colour and a pipe above the velocity limit is drawn in
  the check's FAIL colour; the sheet is stamped DRAFT until the network is
  confirmed and loses the stamp after; model text is escaped; the sheet's
  demand is the engine's own text; with no browser the HTML page is the
  report and the answer says no PDF.

WHAT IT DOES NOT PROVE
  How the page draws it - that is the Companion's test and a person's eye.
"""

from __future__ import print_function

import copy
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests"))

import heron_fire as FIRE                                     # noqa: E402
import heron_sprinkler_report as REPORT                       # noqa: E402
import heron_sprinkler_run as R                               # noqa: E402
import heron_sprinkler_view as V                              # noqa: E402
import test_sprinkler_run as RUN                              # noqa: E402
import test_sprinkler_takeoff as NET                          # noqa: E402


def solved(**criteria):
    g = RUN.given(operating=["402"])
    g["criteria"].update(criteria)
    return R.run(NET.net(), g)


def test_every_pipe_and_head_drawn():
    v = V.build(NET.net())
    assert len(v["segments"]) == 5 and len(v["points"]) == 2
    assert [m["key"] for m in v["modes"]] == ["size", "operating"]
    assert all(s["a"] and s["b"] for s in v["segments"])


def test_after_a_solve():
    r = solved(max_velocity_ms=0.5)
    assert r["status"] == "ok", r["refused"]
    dry = dict((s["id"], s) for s in V.build(NET.net(), r)["segments"])["204"]
    assert dry["colour"]["flow"] == V.DRY and dry["colour"]["operating"] != V.OPERATING[0][1]
    g = RUN.given(operating=["401", "402"])
    g["criteria"]["max_velocity_ms"] = 0.5
    both = R.run(NET.net(), g)
    fail = V.CHECKS[1][1]
    fast = [s for s in V.build(NET.net(), both)["segments"] if s["colour"].get("checks") == fail]
    assert fast and all(s["values"]["v_ms"] > 0.5 for s in fast)
    v = V.build(NET.net(), r)
    keys = [m["key"] for m in v["modes"]]
    # This network was read without its Spaces, so there is no spacing colour (review R4).
    assert keys == ["size", "operating", "flow", "velocity", "pressure", "checks"]
    pts = dict((p["id"], p) for p in v["points"])
    assert pts["402"]["colour"]["operating"] != pts["401"]["colour"]["operating"]
    assert pts["402"]["values"]["p_bar"] > 0 and pts["401"]["colour"]["pressure"] == V.DRY
    assert v["source"]["id"] == "100"


def test_sheet_draft_then_confirmed():
    n = NET.net()
    r = solved()
    page = REPORT.html(r, n)
    assert REPORT.DRAFT in page
    R.confirm(r, n)
    page = REPORT.html(r, n)
    assert REPORT.DRAFT not in page and "confirmed by" in page
    demand = [t for name, t in r["answer"]["results"] if name == "Sprinkler demand"][0]
    assert FIRE.flow_text(r["answer"]["data"]["demand_lpm"]) in page or demand in page
    assert "fire consultant" in page


def test_model_text_is_escaped():
    n = NET.net()
    n["system"]["name"] = "<script>alert(1)</script>"
    r = R.run(n, RUN.given(operating=["402"]))
    page = REPORT.html(r, n)
    assert "<script>" not in page and "&lt;script&gt;" in page


def test_not_solved_says_why():
    page = REPORT.html(R.run(NET.net(), {}), NET.net())
    assert "Not solved" in page and "criteria.c_factor" in page


def test_write_without_a_browser():
    folder = tempfile.mkdtemp()
    try:
        r = solved()
        got = REPORT.write(folder, NET.net(), r, search=[])
        assert got["ok"] and got["pdf"] is None and "no PDF" in got["said"]
        assert os.path.isfile(got["html"]) and os.path.isfile(got["pipes_csv"])
        with open(got["csv"]) as fh:
            lines = fh.read().splitlines()
        assert lines[0].startswith("head,") and lines[1].startswith("402,")
    finally:
        shutil.rmtree(folder, ignore_errors=True)


def test_spacing_colour_and_outlines():
    import test_sprinkler_spacing as SP
    n = SP.office()
    r = R.run(n, {"spacing": {"hazard": {"7001": "light"},
                              "limits": {"light hazard": dict(SP.LIGHT, max_spacing_m=2.5)}}})
    v = V.build(n, r)
    fail = dict((k, c) for k, c, _t in V.SPACING_MARKS)["fail"]
    assert all(p["colour"]["spacing"] == fail for p in v["points"])
    assert len(v["outlines"]) == 1 and v["outlines"][0]["label"] == "101 Office"
    assert len(v["outlines"][0]["points"]) == 4
    assert all(s["colour"]["spacing"] == V.DRY for s in v["segments"])


def test_sheet_has_fire_water_and_spacing():
    import test_sprinkler_spacing as SP
    n = SP.office()
    g = RUN.given(operating=["402", "404"], fittings={
        "tee or cross, flow turned 90 degrees|DN25": 0},
        spacing={"hazard": {"7001": "light"}, "limits": {"light hazard": SP.LIGHT}},
        water={"include": {"standpipe": True}, "simultaneous": {"standpipe": True},
               "standpipe": SP.STANDPIPE})
    r = R.run(n, g)
    page = REPORT.html(r, n)
    assert "<h2>Fire water</h2>" in page and "<h2>Spacing in each Space</h2>" in page
    assert page.count("not included by the modeller") == 3
    assert "not solved together" in page and "101 Office" in page
    unsolved = REPORT.html(R.run(n, {}), n)
    assert "so the fire water is not worked out" in unsolved


def test_view_is_copied_not_shared():
    n = NET.net()
    before = copy.deepcopy(n)
    V.build(n, solved())
    assert n == before


if __name__ == "__main__":
    sys.exit(NET.run_all(sys.modules[__name__]))
