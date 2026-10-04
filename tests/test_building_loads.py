# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The building runner (docs/44 section 5): every Space through the unchanged
room engine, then zones and the building - and the values that go back into
Revit.

    python tests/test_building_loads.py

WHAT IT PROVES
  A one-room building gives exactly what the engine gives called by hand; the
  block load is the largest hourly sum, below the sum of peaks when an east
  and a west office peak at different hours; one bad Space does not stop the
  building; a profile value that is not a number or out of range refuses its
  Space and nothing is calculated for it; an override changes one Space only;
  what is asked is exactly what is missing, project first; every input
  carries its source; the answer says it is not HAP; a run is kept and read
  back; Finalize writes three fields per calculated Space in the project's own
  units and refuses a unit it does not know.

WHAT IT DOES NOT PROVE
  That any load is right for a building - that is an engineer's comparison
  with an hourly program on the same inputs (docs/44 section 10).
"""

from __future__ import print_function

import copy as _copy
import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests"))

import heron_building_loads as B                              # noqa: E402
import heron_hvac as H                                        # noqa: E402
import heron_takeoff as T                                     # noqa: E402
from test_takeoff import ROOM, copy, raises, run_all          # noqa: E402

PROJECT = {"design_weather": "doha-0.4", "room_dry_bulb_c": 24, "room_rh_pct": 50,
           "supply_dry_bulb_c": 13, "ground_reflectance": 0.2,
           "outside_surface_coefficient_w_m2k": 17, "heating_outdoor_dry_bulb_c": 10,
           "heating_room_dry_bulb_c": 21, "unconditioned_temp_c": 35, "ground_temp_c": 25,
           "altitude_m": 10}
OFFICE = {"people_per_m2": 0.1, "sensible_w_each": 75, "latent_w_each": 55,
          "lighting_w_per_m2": 10, "equipment_w_per_m2": 15, "infiltration_ach": 0.3,
          "outdoor_air_ls_per_person": 2.5, "outdoor_air_ls_per_m2": 0.3}


class knowledge_folder(object):
    """HERON_KNOWLEDGE pointed at a fresh folder, and put back after."""

    def __enter__(self):
        self.path = tempfile.mkdtemp(prefix="heron-loads-")
        self.was = os.environ.get("HERON_KNOWLEDGE")
        os.environ["HERON_KNOWLEDGE"] = self.path
        return self.path

    def __exit__(self, *exc):
        if self.was is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = self.was
        shutil.rmtree(self.path, ignore_errors=True)
        return False


def two_offices(second_normal):
    d = copy()
    other = _copy.deepcopy(d["spaces"][0])
    other.update(id=2, unique_id="u2", number="2", name="Office 02")
    other["faces"][0]["normal"] = second_normal
    d["spaces"].append(other)
    return d


def bad_second_office():
    d = two_offices([-1.0, 0.0, 0.0])
    d["types"]["bare"] = {"name": "Generic", "category": "Walls", "u_w_m2k": None,
                          "shgc": None, "absorptance": None}
    d["spaces"][1]["faces"][0]["type"] = "bare"
    return d


def test_profile_key_strips_the_number():
    assert B.profile_key({"space_type": None, "name": "Office 01"}) == "Office"
    assert B.profile_key({"space_type": "Classroom", "name": "Room 4"}) == "Classroom"
    assert B.profile_key({"space_type": None, "name": ""}) == "(unnamed)"


def test_needs_asks_for_what_is_missing_and_nothing_else():
    t = T.read(ROOM)
    project = dict(PROJECT)
    del project["room_dry_bulb_c"]
    assert [a["input"] for a in B.needs(t, project, {"Office": OFFICE})] == ["room_dry_bulb_c"]


def test_needs_asks_per_profile():
    t = T.read(ROOM)
    office = dict(OFFICE)
    del office["lighting_w_per_m2"]
    asked = B.needs(t, PROJECT, {"Office": office})
    assert [(a["input"], a["for"]) for a in asked] == [("lighting_w_per_m2", "Office")]


def test_needs_offers_a_standard_and_never_fills_it_in():
    asked = {a["input"]: a for a in B.needs(T.read(ROOM), {}, {})}
    assert "Doha" in (asked["design_weather"]["offer"] or "")
    assert "people_heat_gain" in (asked["sensible_w_each"]["offer"] or "")
    r = B.run(T.read(ROOM), {}, {})
    assert r["spaces"][0]["status"] == "missing" and r["building"]["block_w"] == 0


def test_one_room_building_equals_the_engine_called_by_hand():
    t = T.read(ROOM)
    r = B.run(t, PROJECT, {"Office": OFFICE})
    s = r["spaces"][0]
    assert s["status"] == "ok", s["why"]
    by_hand = H.run("monthly_load", B.monthly_inputs(t, t.spaces[0], PROJECT, OFFICE))
    assert not by_hand["ignored"], by_hand["ignored"]
    assert abs(s["cooling"]["peak"]["total_w"] - by_hand["data"]["peak"]["total_w"]) < 1e-6
    assert abs(r["building"]["block_w"] - s["cooling"]["peak"]["total_w"]) < 1e-6
    heat = H.run("heating_load", B.heating_inputs(t, t.spaces[0], PROJECT, OFFICE))
    assert not heat["ignored"], heat["ignored"]
    assert abs(s["heating"]["loss_w"] - heat["data"]["loss_w"]) < 1e-6


def test_block_load_is_the_largest_hourly_sum_not_the_sum_of_peaks():
    r = B.run(T.read(two_offices([1.0, 0.0, 0.0])), PROJECT, {"Office": OFFICE})
    peaks = sum(s["cooling"]["peak"]["total_w"] for s in r["spaces"])
    assert abs(r["building"]["sum_of_peaks_w"] - peaks) < 1e-6
    assert r["building"]["block_w"] < peaks          # east and west peak at different hours
    assert r["building"]["block_month"] and r["building"]["block_hour"]
    assert [z["name"] for z in r["zones"]] == ["Z1"]


def test_one_bad_space_does_not_stop_the_building():
    r = B.run(T.read(bad_second_office()), PROJECT, {"Office": OFFICE})
    assert [s["status"] for s in r["spaces"]] == ["ok", "refused"]
    assert "Generic" in " ".join(r["spaces"][1]["why"])
    assert r["building"]["block_w"] > 0


def test_unplaced_space_is_left_out_not_calculated_as_zero():
    d = two_offices([1.0, 0.0, 0.0])
    d["spaces"][1].update(placed=False, area_m2=0.0, faces=[])
    r = B.run(T.read(d), PROJECT, {"Office": OFFICE})
    assert [s["status"] for s in r["spaces"]] == ["ok", "left out"]
    assert r["spaces"][1]["cooling"] is None


def test_profile_values_are_range_checked():
    t = T.read(ROOM)
    for bad in ("ten", -5, 900, True):
        r = B.run(t, PROJECT, {"Office": dict(OFFICE, lighting_w_per_m2=bad)})
        assert r["spaces"][0]["status"] == "refused", bad
        assert "lighting_w_per_m2" in " ".join(r["spaces"][0]["why"])
        assert r["building"]["block_w"] == 0


def test_override_changes_one_space_only():
    t = T.read(two_offices([-1.0, 0.0, 0.0]))
    r = B.run(t, PROJECT, {"Office": OFFICE}, overrides={2: {"equipment_w_per_m2": 40}})
    a, b = r["spaces"]
    assert b["cooling"]["peak"]["total_w"] > a["cooling"]["peak"]["total_w"]


def test_every_input_carries_a_source_label():
    project = dict(PROJECT, ground_reflectance={"value": 0.2,
                                                "source": "standard:ASHRAE 2017 example"})
    r = B.run(T.read(ROOM), project, {"Office": OFFICE})
    labels = [v["source"] for v in r["inputs"]["project"].values()]
    labels += [v["source"] for p in r["inputs"]["profiles"].values() for v in p.values()]
    assert labels and all(x in ("model", "instruction", "assumption") or x.startswith("standard:")
                          for x in labels)
    assert r["inputs"]["project"]["ground_reflectance"]["source"].startswith("standard:")
    assert r["spaces"][0]["status"] == "ok"


def test_the_answer_says_it_is_not_hap():
    r = B.run(T.read(ROOM), PROJECT, {"Office": OFFICE})
    assert H.NOT_HAP in r["notes"] and H.STEADY_HOURS in r["notes"]


def test_save_and_load_round_trip():
    with knowledge_folder() as tmp:
        r = B.run(T.read(ROOM), PROJECT, {"Office": OFFICE})
        path = B.save("project-a", r)
        assert path.startswith(tmp) and B.load("project-a")["run_id"] == r["run_id"]
        assert B.load("project-b") is None
        again = B.run(T.read(ROOM), PROJECT, {"Office": dict(OFFICE, equipment_w_per_m2=30)})
        again["run_id"] = r["run_id"]                    # the same second: neither is lost
        B.save("project-a", again)
        assert len(B.runs("project-a")) == 2
        assert B.load("project-a", "../" + r["run_id"]) is None


def test_finalize_writes_the_three_fields_for_each_ok_space():
    t = T.read(ROOM)
    r = B.run(t, PROJECT, {"Office": OFFICE})
    rows = B.finalize_rows(t, r)
    assert [x[2] for x in rows] == ["Design Cooling Load", "Design Heating Load",
                                   "Specified Supply Airflow"]
    assert rows[0][:2] == ["1", "u1"] and rows[0][3] == "0.00 W"
    assert rows[0][4] == "%.0f W" % r["spaces"][0]["cooling"]["peak"]["total_w"]


def test_finalize_converts_to_project_units():
    d = copy()
    d["units"] = {"power": "Btu/h", "airflow": "CFM"}
    t = T.read(d)
    r = B.run(t, PROJECT, {"Office": OFFICE})
    rows = {x[2]: x[4] for x in B.finalize_rows(t, r)}
    w = r["spaces"][0]["cooling"]["peak"]["total_w"]
    assert rows["Design Cooling Load"] == "%.0f Btu/h" % (w * 3.412141633)
    assert rows["Specified Supply Airflow"] == "%.0f CFM" % (r["spaces"][0]["supply_ls"] * 2.118880003)


def test_refused_space_is_never_written():
    t = T.read(bad_second_office())
    rows = B.finalize_rows(t, B.run(t, PROJECT, {"Office": OFFICE}))
    assert {x[0] for x in rows} == {"1"}


def test_unknown_unit_refuses_finalize():
    d = copy()
    d["units"] = {"power": "kcal/h", "airflow": "L/s"}
    t = T.read(d)
    r = B.run(t, PROJECT, {"Office": OFFICE})
    try:
        B.finalize_rows(t, r)
        refused = ""
    except ValueError as why:
        refused = str(why)
    assert "kcal/h" in refused


# --- the seam and the tool (Task 5) - through heron_brain, which needs no MCP SDK

SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")


def _brain():
    sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
    import heron_brain
    return heron_brain


def _files(folder):
    return [os.path.join(d, f) for d, _s, fs in os.walk(folder) for f in fs]


def test_seam_calculates_and_keeps_the_run():
    brain = _brain()
    with knowledge_folder() as tmp:
        got = brain.building_loads(json.dumps(ROOM),
                                   json.dumps({"project": PROJECT, "profiles": {"Office": OFFICE}}),
                                   project="project-a", project_name="t")
        assert not got["asked"] and got["result"]["spaces"][0]["status"] == "ok"
        assert "Spaces calculated: 1;" in got["said"] and " kW (" in got["said"]
        assert "Office 01" not in got["said"]                  # never the rows
        assert H.NOT_HAP in got["said"] and "compliant" not in got["said"].lower()
        assert got["saved"] and got["saved"].startswith(tmp)


def test_seam_asks_and_calculates_nothing():
    brain = _brain()
    with knowledge_folder() as tmp:
        project = dict(PROJECT)
        del project["room_dry_bulb_c"]
        got = brain.building_loads(json.dumps(ROOM),
                                   {"project": project, "profiles": {"Office": OFFICE}},
                                   project="project-a")
        assert [a["input"] for a in got["asked"]] == ["room_dry_bulb_c"]
        assert got["result"] is None and "Nothing was calculated" in got["said"]
        assert not [f for f in _files(tmp) if f.endswith(".json") and ".loads" in f]


def test_seam_remembers_the_last_answers():
    brain = _brain()
    with knowledge_folder():
        brain.building_loads(json.dumps(ROOM), {"project": PROJECT,
                                                "profiles": {"Office": OFFICE}},
                             project="project-a")
        again = brain.building_loads(json.dumps(ROOM), {}, project="project-a")
        assert not again["asked"] and again["result"]["spaces"][0]["status"] == "ok"


def test_seam_refuses_an_unreadable_takeoff():
    got = _brain().building_loads("not json", {})
    assert got["result"] is None and "Nothing was calculated" in got["said"]


def test_tool_reads_the_envelope_checks_the_pin_and_tells_only_totals():
    server = io.open(SERVER, encoding="utf-8").read()
    body = server[server.index("def revit_building_loads("):]
    body = body[:body.index(chr(10) + "@server.tool()")]
    assert '_through(revit_read, reply_out=out)("REPORT_SPACE_ENVELOPE"' in body
    assert body.index("if pinned.check(reply):") < body.index("brain.building_loads(")
    assert "LOADS_PANEL.open(document, answer, _pin_identity())" in body
    assert "revit_change" not in body and "_change(" not in body
    sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
    import heron_tools as TOOLS
    assert TOOLS.TOOLS.get("revit_building_loads") == (TOOLS.ANALYZE, "run_fragment_read")


if __name__ == "__main__":
    sys.exit(run_all(sys.modules[__name__]))
