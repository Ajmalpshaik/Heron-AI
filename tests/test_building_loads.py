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
           "heating_room_dry_bulb_c": 21, "unconditioned_temp_c": 35,
           "heating_unconditioned_temp_c": 18, "ground_temp_c": 25, "altitude_m": 10}
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
    rows = B.finalize_rows(t, B.confirm(r, t))
    assert [x[2] for x in rows] == ["Design Cooling Load", "Design Heating Load",
                                   "Specified Supply Airflow"]
    assert rows[0][:2] == ["1", "u1"] and rows[0][3] == "0.00 W"
    assert rows[0][4] == "%.0f W" % r["spaces"][0]["cooling"]["peak"]["total_w"]


def test_finalize_converts_to_project_units():
    d = copy()
    d["units"] = {"power": "Btu/h", "airflow": "CFM"}
    t = T.read(d)
    r = B.run(t, PROJECT, {"Office": OFFICE})
    rows = {x[2]: x[4] for x in B.finalize_rows(t, B.confirm(r, t))}
    w = r["spaces"][0]["cooling"]["peak"]["total_w"]
    assert rows["Design Cooling Load"] == "%.0f Btu/h" % (w * 3.412141633)
    assert rows["Specified Supply Airflow"] == "%.0f CFM" % (r["spaces"][0]["supply_ls"] * 2.118880003)


def test_refused_space_is_never_written():
    t = T.read(bad_second_office())
    rows = B.finalize_rows(t, B.confirm(B.run(t, PROJECT, {"Office": OFFICE}), t))
    assert {x[0] for x in rows} == {"1"}


def test_unknown_unit_refuses_finalize():
    d = copy()
    d["units"] = {"power": "kcal/h", "airflow": "L/s"}
    t = T.read(d)
    r = B.run(t, PROJECT, {"Office": OFFICE})
    try:
        B.finalize_rows(t, B.confirm(r, t))
        refused = ""
    except ValueError as why:
        refused = str(why)
    assert "kcal/h" in refused


def test_finalize_refuses_a_takeoff_nobody_confirmed():
    t = T.read(ROOM)
    r = B.run(t, PROJECT, {"Office": OFFICE})
    try:
        B.finalize_rows(t, r)
        said = ""
    except ValueError as why:
        said = str(why)
    assert "not been confirmed" in said
    assert B.finalize_rows(t, B.confirm(r, t))


def test_a_confirmation_holds_for_that_takeoff_only():
    t = T.read(ROOM)
    r = B.confirm(B.run(t, PROJECT, {"Office": OFFICE}), t)
    assert B.confirmed(r, t)
    moved = copy()
    moved["spaces"][0]["faces"][0]["area_m2"] = 14.0               # the model changed
    assert not B.confirmed(r, T.read(moved))


# --- the review's findings, each reproduced before it was fixed --------------

def unknown_roof():
    d = copy()
    d["spaces"][0]["faces"][1]["beyond"] = "unknown"           # the roof, element 12
    return d


def test_review_c1_a_face_nobody_can_see_beyond_is_asked_per_element():
    d = two_offices([1.0, 0.0, 0.0])
    for s in d["spaces"]:
        s["faces"][1]["beyond"] = "unknown"                     # both share element 12
    t = T.read(d)
    asked = [a for a in B.needs(t, PROJECT, {"Office": OFFICE}) if a["input"].startswith("beyond:")]
    assert [a["input"] for a in asked] == ["beyond:12"], asked
    assert "outside" in asked[0]["unit"] and "ground" in asked[0]["unit"]
    r = B.run(t, PROJECT, {"Office": OFFICE})
    assert [s["status"] for s in r["spaces"]] == ["missing", "missing"]
    answered = dict(PROJECT, **{"beyond:12": "outside"})
    assert not B.needs(t, answered, {"Office": OFFICE})
    r = B.run(t, answered, {"Office": OFFICE})
    assert [s["status"] for s in r["spaces"]] == ["ok", "ok"]
    bad = B.run(t, dict(PROJECT, **{"beyond:12": "sky"}), {"Office": OFFICE})
    assert bad["spaces"][0]["status"] == "refused" and "beyond:12" in " ".join(bad["spaces"][0]["why"])


def test_review_i2_heating_has_its_own_unconditioned_temperature():
    d = copy()
    d["spaces"][0]["faces"][0]["beyond"] = "unconditioned"      # a wall to a store
    t = T.read(d)
    hot = dict(PROJECT, unconditioned_temp_c=45, heating_unconditioned_temp_c=15)
    r = B.run(t, hot, {"Office": OFFICE})
    s = r["spaces"][0]
    assert s["status"] == "ok", s["why"]
    store = [c for c in s["heating"]["components"] if c["name"].startswith("Ext wall")]
    assert store and store[0]["w"] > 0                          # 21 C inside, 15 C beyond: a loss
    without = dict(PROJECT)
    del without["heating_unconditioned_temp_c"]
    assert "heating_unconditioned_temp_c" in [a["input"] for a in B.needs(t, without, {})]


def door_building():
    d = copy()
    d["types"]["dr"] = {"name": "Single Flush: 900", "category": "Doors", "u_w_m2k": 2.0,
                        "shgc": None, "absorptance": None}
    d["spaces"][0]["faces"][0]["openings"].append(
        {"element": 30, "kind": "door", "type": "dr", "area_m2": 1.89})
    return d


def test_review_i3_an_outside_door_with_no_absorptance_is_asked_once():
    t = T.read(door_building())
    asked = [a["input"] for a in B.needs(t, PROJECT, {"Office": OFFICE})]
    assert asked == ["door_absorptance"], asked
    r = B.run(t, dict(PROJECT, door_absorptance=0.6), {"Office": OFFICE})
    assert r["spaces"][0]["status"] == "ok", r["spaces"][0]["why"]
    assert any(c["name"] == "Single Flush: 900 (30)" for c in r["spaces"][0]["cooling"]["components"])


def test_review_i1_the_coil_block_carries_the_outdoor_air():
    r = B.run(T.read(ROOM), PROJECT, {"Office": OFFICE})
    b = r["building"]
    assert b["coil_block_w"] > b["block_w"] > 0
    none = B.run(T.read(ROOM), PROJECT, {"Office": dict(OFFICE, outdoor_air_ls_per_person=0,
                                                          outdoor_air_ls_per_m2=0)})
    assert none["spaces"][0]["status"] == "ok", none["spaces"][0]["why"]
    assert abs(none["building"]["coil_block_w"] - none["building"]["block_w"]) < 1e-6
    assert "plant is sized to" not in B.summary_text(r)


def test_review_i8_project_values_are_range_checked_too():
    t = T.read(ROOM)
    for key, bad in (("outside_surface_coefficient_w_m2k", "ten"),
                     ("outside_surface_coefficient_w_m2k", 1000),
                     ("room_rh_pct", 0.5), ("room_dry_bulb_c", -5)):
        r = B.run(t, dict(PROJECT, **{key: bad}), {"Office": OFFICE})      # never raises
        assert r["spaces"][0]["status"] == "refused", (key, bad)
        assert key in " ".join(r["spaces"][0]["why"]), r["spaces"][0]["why"]
        assert r["building"]["block_w"] == 0


def test_review_i7_a_site_far_from_the_weather_station_is_a_fail():
    d = copy()
    d["site"].update(latitude_deg=42.36, longitude_deg=-71.06, named="Boston, MA")
    r = B.run(T.read(d), PROJECT, {"Office": OFFICE})
    assert any(f["level"] == "FAIL" and "km" in f["text"] and "Boston" in f["text"] for f in r["qa"])
    assert r["spaces"][0]["status"] == "refused"
    ok = B.run(T.read(ROOM), PROJECT, {"Office": OFFICE})
    assert any("Doha" in f["text"] and f["level"] == "INFO" for f in ok["qa"])
    assert ok["site"]["latitude_deg"] == 25.28


def test_review_i11_a_space_can_have_its_own_set_point():
    t = T.read(two_offices([-1.0, 0.0, 0.0]))
    r = B.run(t, PROJECT, {"Office": OFFICE}, overrides={2: {"room_dry_bulb_c": 21}})
    a, b = r["spaces"]
    assert b["cooling"]["peak"]["total_w"] > a["cooling"]["peak"]["total_w"]


def test_review_i11_a_kept_run_keeps_its_takeoff_and_not_its_hours():
    with knowledge_folder():
        t = T.read(ROOM)
        r = B.run(t, PROJECT, {"Office": OFFICE})
        path = B.save("project-a", r, takeoff=t)
        kept = json.load(io.open(path, encoding="utf-8"))
        assert "hours" not in kept["spaces"][0]["cooling"]
        back = B.load_takeoff("project-a", kept["takeoff_fingerprint"])
        assert back is not None and B.fingerprint(back) == B.fingerprint(t)


def test_review_i9_the_fingerprint_ignores_what_finalize_writes():
    t = T.read(ROOM)
    written = copy()
    written["spaces"][0]["current"]["Design Cooling Load"] = "2021 W"
    written["findings"] = ["something said differently"]
    assert B.fingerprint(T.read(written)) == B.fingerprint(t)
    moved = copy()
    moved["spaces"][0]["faces"][0]["area_m2"] = 14.0
    assert B.fingerprint(T.read(moved)) != B.fingerprint(t)


# --- the second review's findings, each reproduced before it was fixed -------

def test_review2_n3_a_floor_open_below_or_over_an_unconditioned_space_is_not_ground():
    def floor_answered(word):
        d = copy()
        d["spaces"][0]["faces"].append(
            {"element": 40, "type": "r", "side": "bottom", "normal": [0.0, 0.0, -1.0],
             "area_m2": 20.0, "beyond": "unknown", "beyond_space": None, "openings": []})
        t = T.read(d)
        return B.run(t, dict(PROJECT, **{"beyond:40": word}), {"Office": OFFICE})["spaces"][0]
    ground, outside, store = (floor_answered(w) for w in ("ground", "outside", "unconditioned"))
    assert ground["status"] == outside["status"] == store["status"] == "ok"
    # Open below: conduction to the outdoor air, hour by hour - more cooling than
    # the ground, which carries none.
    assert outside["cooling"]["peak"]["total_w"] > ground["cooling"]["peak"]["total_w"]
    # Over a store: the unconditioned temperatures, cooling and heating.
    assert store["cooling"]["peak"]["total_w"] > ground["cooling"]["peak"]["total_w"]
    heat = dict((c["name"], c["w"]) for c in store["heating"]["components"])
    assert heat["Roof (40)"] == 20.0 * 0.3 * (21 - 18)                # heating_unconditioned 18 C


def test_review2_n2_an_override_cleared_on_the_page_is_cleared():
    brain = _brain()
    with knowledge_folder():
        t = json.dumps(two_offices([-1.0, 0.0, 0.0]))
        base = brain.building_loads(t, {"project": PROJECT, "profiles": {"Office": OFFICE}},
                                    project="project-a")
        bumped = brain.building_loads(t, {"overrides": {"2": {"equipment_w_per_m2": 40}}},
                                      project="project-a")
        cleared = brain.building_loads(t, {"overrides": {"2": {"equipment_w_per_m2": None}}},
                                       project="project-a")
        total = lambda got: got["result"]["spaces"][1]["cooling"]["peak"]["total_w"]
        assert total(bumped) > total(base)
        assert abs(total(cleared) - total(base)) < 1e-6, (total(cleared), total(base))
        assert "2" not in (cleared["inputs"]["overrides"] or {})


def test_review2_n1_the_page_shows_the_checks_the_run_used():
    brain = _brain()
    d = copy()
    d["site"].update(latitude_deg=42.36, longitude_deg=-71.06, named="Boston, MA")
    d["spaces"][0]["faces"][1]["beyond"] = "unknown"
    got = brain.building_loads(json.dumps(d), {"project": dict(PROJECT, **{"beyond:12": "outside"}),
                                               "profiles": {"Office": OFFICE}}, save=False)
    texts = " ".join(f["text"] for f in got["qa"])
    assert "km from" in texts and "the sun is worked out for" in texts, texts
    assert "Heron asks" not in texts                                # 12 was answered
    asked = brain.building_loads(json.dumps(d), {"project": PROJECT}, save=False)
    assert any("the sun is worked out for" in f["text"] for f in asked["qa"])


def test_review2_m2_changing_an_answer_takes_the_confirmation_away():
    d = copy()
    d["spaces"][0]["faces"][1]["beyond"] = "unknown"
    t = T.read(d)
    r = B.confirm(B.run(t, dict(PROJECT, **{"beyond:12": "outside"}), {"Office": OFFICE}), t)
    assert B.confirmed(r, t)
    other = B.run(t, dict(PROJECT, **{"beyond:12": "conditioned"}), {"Office": OFFICE})
    other["geometry_confirmed"] = dict(r["geometry_confirmed"])     # carried over by mistake
    assert not B.confirmed(other, t)


def test_review2_m12_a_wrong_answer_word_is_said_when_asked_again():
    d = copy()
    d["spaces"][0]["faces"][1]["beyond"] = "unknown"
    asked = [a for a in B.needs(T.read(d), dict(PROJECT, **{"beyond:12": "sky"}), {"Office": OFFICE})
             if a["input"] == "beyond:12"]
    assert asked and "'sky'" in asked[0]["why"], asked


def test_5b332_a_table_of_answers_is_read_and_kept():
    # FRAGMENT-ISSUES 5b-332: project["beyond"] = {element: word}, the shape
    # answers() promises beside "beyond:<element>" keys, was dropped by _value(),
    # which took the table for one labelled value - both questions came back.
    d = copy()
    d["spaces"][0]["faces"][1]["beyond"] = "unknown"           # the roof, element 12
    t = T.read(d)
    table = dict(PROJECT, beyond={"12": "outside"})
    labelled = dict(PROJECT, beyond={"value": {"12": "outside"}, "source": "instruction"})
    assert B.answers(table) == {"12": "outside"} == B.answers(labelled)
    assert not [a for a in B.needs(t, table, {"Office": OFFICE})
                if a["input"].startswith("beyond:")]
    r = B.run(t, table, {"Office": OFFICE})
    assert r["spaces"][0]["status"] == "ok", r["spaces"][0]["why"]
    # Kept with the run as the page's own keys: the sheet prints it, the next
    # call remembers it, and the confirmation covers it as the same answer.
    keyed = B.run(t, dict(PROJECT, **{"beyond:12": "outside"}), {"Office": OFFICE})
    assert B.answers(r["inputs"]["project"]) == {"12": "outside"}
    assert B.answers_key(r) == B.answers_key(keyed)
    bad = B.run(t, dict(PROJECT, beyond={"12": "sky"}), {"Office": OFFICE})
    assert bad["spaces"][0]["status"] == "refused"
    assert "beyond:12" in " ".join(bad["spaces"][0]["why"]), bad["spaces"][0]["why"]


def test_5b333_the_chat_answer_names_every_space_not_calculated():
    # FRAGMENT-ISSUES 5b-333 (2): the chat answer said "refused: 3" and listed
    # the three commonest refusal LINES - two for one Space, one for another -
    # so the third refused Space was never named.
    d = copy()
    for n in range(2, 6):
        other = _copy.deepcopy(d["spaces"][0])
        other.update(id=n, unique_id="u%d" % n, number=str(n), name="Office 0%d" % n)
        other["faces"][0]["openings"][0]["area_m2"] = 14.0      # larger than its 13.5 m2 face
        d["spaces"].append(other)
    d["spaces"][1]["faces"][1]["openings"] = [{"element": 99, "kind": "skylight", "type": "g",
                                               "area_m2": 25.0}]   # a second reason, Office 02
    r = B.run(T.read(d), PROJECT, {"Office": OFFICE})
    said = B.summary_text(r)
    assert "refused: 4;" in said, said
    for name in ("2 Office 02", "3 Office 03", "4 Office 04", "5 Office 05"):
        assert name in said, (name, said)
    assert "Office 01" not in said                                 # calculated: on the page
    assert "openings of 25.00 m2 are larger than the 20.00 m2 face" in said, said   # both reasons


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


def test_seam_5b332_a_later_table_changes_only_its_own_answers():
    # Through the tool's own path: a table given in one call is kept per
    # element, so a later table that answers one element again replaces that
    # answer and leaves the other - never the whole table for the new one.
    brain = _brain()
    d = copy()
    d["spaces"][0]["faces"][0]["beyond"] = "unknown"           # the wall, element 10
    d["spaces"][0]["faces"][1]["beyond"] = "unknown"           # the roof, element 12
    with knowledge_folder():
        first = brain.building_loads(json.dumps(d), {
            "project": dict(PROJECT, beyond={"10": "outside", "12": "outside"}),
            "profiles": {"Office": OFFICE}}, project="project-a")
        assert not first["asked"], first["asked"]
        assert first["result"]["spaces"][0]["status"] == "ok"
        again = brain.building_loads(json.dumps(d), {"project": {"beyond": {"12": "conditioned"}}},
                                     project="project-a")
        assert not again["asked"], again["asked"]
        assert B.answers(again["result"]["inputs"]["project"]) == {"10": "outside",
                                                                   "12": "conditioned"}


def test_seam_5b333_a_run_is_kept_only_while_keep_says_so():
    # FRAGMENT-ISSUES 5b-333 (1): a Recalculate overtaken by a fresh read of the
    # model saved its run AFTER the fresh one - so the newest run kept was a
    # building no longer there. The Companion hands the brain `keep`, which
    # saves only while the page still holds the take-off the run came from.
    brain = _brain()
    inputs = {"project": PROJECT, "profiles": {"Office": OFFICE}}
    with knowledge_folder():
        try:
            gone = brain.building_loads(json.dumps(ROOM), inputs, project="project-a",
                                        keep=lambda save: (False, None))
        except TypeError as no_keep:                        # the old seam takes no keep
            gone = {"saved": "", "said": "no keep: %s" % no_keep}
        assert gone["saved"] is None and "NOT KEPT" in gone["said"], gone["said"]
        assert not B.runs("project-a")
        kept = brain.building_loads(json.dumps(ROOM), inputs, project="project-a",
                                    keep=lambda save: (True, save()))
        assert kept["saved"] and len(B.runs("project-a")) == 1


def test_seam_refuses_an_unreadable_takeoff():
    got = _brain().building_loads("not json", {})
    assert got["result"] is None and "Nothing was calculated" in got["said"]


def test_the_building_total_carries_the_air_and_the_load_per_square_metre():
    # The page's summary row (2026-10-04) shows these; the page adds nothing up
    # itself (mcp/companion README rule 4), so the brain does.
    r = B.run(bad_second_office(), PROJECT, {"Office": OFFICE})
    ok = [s for s in r["spaces"] if s["status"] == "ok"]
    b = r["building"]
    assert b["spaces"] == 2 and b["calculated"] == 1 == len(ok)
    assert abs(b["supply_ls"] - ok[0]["supply_ls"]) < 1e-9
    assert abs(b["outdoor_air_ls"] - ok[0]["outdoor_air_ls"]) < 1e-9
    assert abs(b["calculated_area_m2"] - ok[0]["area_m2"]) < 1e-9
    assert abs(b["block_w_per_m2"] - b["block_w"] / ok[0]["area_m2"]) < 1e-9
    nothing = B._block([])
    assert nothing["calculated"] == 0 and nothing["block_w_per_m2"] is None


def _confirmed_rows():
    t = T.read(ROOM)
    r = B.confirm(B.run(t, PROJECT, {"Office": OFFICE}), t, "test")
    return t, B.finalize_rows(t, r)


def test_read_back_matches_what_finalize_wrote():
    # The first real Finalize (Project2, 2026-10-05) wrote the Spaces right, but
    # its read-back asked for them by typed ids, which the add-in refuses - so
    # the read-back is the take-off read again, compared value by value.
    t, rows = _confirmed_rows()
    fresh = copy()
    fresh["spaces"][0]["current"] = {field: new for _sid, _uid, field, _was, new in rows}
    back = B.read_back(rows, fresh)
    assert len(back) == len(rows) == 3 and all(b["ok"] for b in back)
    assert back[0]["space"] and back[0]["written"] == rows[0][4]


def test_read_back_allows_revits_rounding_and_says_what_does_not_match():
    t, rows = _confirmed_rows()
    fresh = copy()
    written = {field: new for _sid, _uid, field, _was, new in rows}
    shown = dict(written)
    # Revit prints its own rounding - "238 L/s" for 237.6 - and thousands grouped.
    cool = B._number_in(written["Design Cooling Load"])
    shown["Design Cooling Load"] = "{:,.0f} W".format(cool)
    shown["Design Heating Load"] = "1 W"                           # an edit made in Revit
    fresh["spaces"][0]["current"] = shown
    back = {b["parameter"]: b for b in B.read_back(rows, fresh)}
    assert back["Design Cooling Load"]["ok"]
    assert not back["Design Heating Load"]["ok"] and back["Design Heating Load"]["reads"] == "1 W"
    fresh["spaces"][0]["current"] = {}
    assert not any(b["ok"] for b in B.read_back(rows, fresh))     # nothing readable is no match
    assert B._number_in("3.300,5 W") == 3300.5 and B._number_in("238,0 L/s") == 238.0
    assert B._number_in("") is None and B._number_in(None) is None


def test_finalize_makes_the_schedule_of_what_it_wrote():
    # Ajmal, 2026-10-05: Finalize also makes the Spaces schedule - named the way
    # the calculation is named, with every value Finalize writes.
    assert B.SCHEDULE_NAME == "HVAC Load Calculation - Spaces"
    assert B.SCHEDULE_FIELDS[:2] == ("Number", "Name")
    assert all(f in B.SCHEDULE_FIELDS for f in B.FINALIZE_FIELDS)


def test_5b335_finalize_says_the_true_reason_a_diffuser_was_not_written():
    # FRAGMENT-ISSUES 5b-335: the filter handed over none of the three diffusers
    # (rowsUnmatched 3) and the page said they "sit on a level no calculated
    # Space is on, or their family's flow is not tied to its connector" - a
    # list of maybes, and the wrong one first. Each reason is now the one
    # SET_AIR_TERMINAL_FLOW gave, as the add-in reports it.
    said = getattr(B, "diffusers_said", None)
    assert said is not None, "the brain has no diffusers_said"
    written, lines = said({"changed": "0", "rowsUnmatched": "3", "alreadyThatFlow": "0 item(s)",
                           "noFlowParameter": "0 item(s)", "refused": "0 item(s)",
                           "notATerminal": "0 item(s)", "builtInDisagrees": "0 item(s)",
                           "badRows": "0"}, 3)
    text = " ".join(lines)
    assert written == 0 and "0 of 3" in text, text
    assert "3 were not among the air terminals" in text, text
    assert "family" not in text and "level" not in text, text
    written, lines = said({"changed": "1", "noFlowParameter": "2 item(s) [352700, 352701]",
                           "alreadyThatFlow": "0 item(s)", "rowsUnmatched": "0"}, 3)
    text = " ".join(lines)
    assert written == 1 and "1 of 3" in text and "352700, 352701" in text, text
    assert "no parameter" in text and "were not among" not in text, text
    written, lines = said({"changed": "3", "alreadyThatFlow": "0 item(s)"}, 3)
    assert written == 3 and lines == ["Diffusers: 3 of 3 written with their Space's share of "
                                      "the supply air, each read back through its connector."], lines


def test_5b335_a_second_finalize_finds_the_schedule_already_there():
    # The second Finalize's schedule step threw - Revit refuses a second
    # schedule of that name - where docs/44 s12.8 says it is said to be there.
    made = getattr(B, "schedule_made", None)
    assert made is not None, "the brain has no schedule_made"
    assert made(["Space Schedule", "HVAC Load Calculation - Spaces"])
    assert made([" hvac load calculation - spaces "])           # as Revit compares view names
    assert not made(["HVAC Load Calculation - Spaces 2", "Space Schedule"]) and not made([])


def _calculated(brain):
    return brain.building_loads(json.dumps(ROOM), {"project": PROJECT,
                                                   "profiles": {"Office": OFFICE}},
                                project="project-a", save=False)


def test_report_goes_to_the_folder_the_modeller_chose_and_is_remembered():
    # Ajmal, 2026-10-04: "I can give the location" - the sheet goes where he
    # says, straight into that folder, and that folder is offered next time.
    brain = _brain()
    with knowledge_folder():
        got = _calculated(brain)
        assert got.get("report_folder") is None             # nothing chosen yet
        chosen = tempfile.mkdtemp(prefix="heron-report-")
        try:
            wrote = brain.loads_report(got["takeoff"], got["result"], folder=chosen,
                                       project="project-a", project_name="t")
            assert wrote["ok"] and wrote["folder"] == chosen
            assert os.path.dirname(wrote["html"]) == chosen and os.path.isfile(wrote["html"])
            assert B.report_folder("project-a") == chosen
            assert _calculated(brain)["report_folder"] == chosen
            again = brain.loads_report(got["takeoff"], got["result"], project="project-a")
            assert again["ok"] and again["folder"] == chosen
            # The note that remembers the folder is not a run.
            assert all(r["run_id"] for r in B.runs("project-a"))
        finally:
            shutil.rmtree(chosen, ignore_errors=True)


def test_a_report_folder_that_is_not_there_is_refused_and_nothing_is_written():
    brain = _brain()
    with knowledge_folder():
        got = _calculated(brain)
        missing = os.path.join(tempfile.mkdtemp(prefix="heron-report-"), "not-there")
        said = brain.loads_report(got["takeoff"], got["result"], folder=missing,
                                  project="project-a")
        assert said["ok"] is False and "not a folder" in said["said"]
        assert not os.path.exists(missing) and B.report_folder("project-a") is None
        relative = brain.loads_report(got["takeoff"], got["result"], folder="reports",
                                      project="project-a")
        assert relative["ok"] is False and B.report_folder("project-a") is None
        assert raises(ValueError, B.set_report_folder, "project-a", "reports")


def test_a_remembered_folder_that_was_removed_falls_back_to_the_usual_place():
    brain = _brain()
    with knowledge_folder() as tmp:
        got = _calculated(brain)
        chosen = tempfile.mkdtemp(prefix="heron-report-")
        B.set_report_folder("project-a", chosen)
        shutil.rmtree(chosen)
        wrote = brain.loads_report(got["takeoff"], got["result"], project="project-a")
        assert wrote["ok"] and wrote["folder"] != chosen and wrote["folder"].startswith(tmp)


def test_tool_reads_the_envelope_checks_the_pin_and_tells_only_totals():
    server = io.open(SERVER, encoding="utf-8").read()
    body = server[server.index("def revit_building_loads("):]
    body = body[:body.index(chr(10) + "@server.tool()")]
    assert '_through(revit_read, reply_out=out)(' in body
    # D-59: links are read only when the modeller asks - absent means host only.
    assert '"REPORT_SPACE_ENVELOPE", "includeLinks=true" if include_links else ""' in body
    assert "include_links: bool = False" in body
    assert body.index("if pinned.check(reply):") < body.index("brain.building_loads(")
    assert "LOADS_PANEL.open(document, answer, _pin_identity())" in body
    assert "revit_change" not in body and "_change(" not in body
    sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
    import heron_tools as TOOLS
    assert TOOLS.TOOLS.get("revit_building_loads") == (TOOLS.ANALYZE, "run_fragment_read")


if __name__ == "__main__":
    sys.exit(run_all(sys.modules[__name__]))
