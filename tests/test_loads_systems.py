# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The air systems of a load run (docs/44 section 13): the unit serving each group
of Spaces - its cooling coil, its supply and outdoor air, and its outdoor air
Space by Space - worked out through heron_hvac's own calculations.

    python tests/test_loads_systems.py

WHAT IT PROVES
  Nothing is sized until the coil's own outdoor design point and each
  system's ventilation inputs are given, and none of them is offered as a
  figure or filled in (D-33) - while the room loads still run. A one-room
  system's coil is the psychrometric chain worked again from heron_psychro's
  primitives: its supply mass, its outdoor air, the mix, the leaving humidity
  its rooms' latent load sets, and the coil's total and sensible load. Spaces
  are grouped by Revit zone, or exactly as the modeller lists them; a Space in
  Revit's Default zone is on none; a Space on two systems, a Space that is not
  there, and a refused Space each stop their system and say why. A value the
  Spaces disagree on is asked for the system. A Space's exhaust above its own
  outdoor air is added to the unit's. A fan adds exactly its heat to the coil,
  draw-through or blow-through. A 100 % outdoor-air unit's coil takes the
  outdoor air; a multiple-zone system's intake is the engine's own 62.1
  figure. The inputs are kept with the run, a table among them intact, and
  the next run reads them back. Every page says it is not an hourly
  simulation.

WHAT IT DOES NOT PROVE
  That a coil is right for a building - that is an engineer's comparison with
  an hourly program on the same inputs. Nothing here has run in Revit or in
  the Companion.
"""

from __future__ import print_function

import copy as _copy
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests"))

import heron_building_loads as B                              # noqa: E402
import heron_hvac as H                                        # noqa: E402
import heron_psychro as PSY                                   # noqa: E402
import heron_takeoff as T                                     # noqa: E402
from test_takeoff import ROOM, run_all                        # noqa: E402
from test_building_loads import (PROJECT, OFFICE, bad_second_office,   # noqa: E402
                                 knowledge_folder, two_offices, _brain)

COIL = {"coil_outdoor_dry_bulb_c": 46, "coil_outdoor_wet_bulb_c": 29}
SINGLE = {"ventilation_system": "single-zone", "ez": 1.0}


def sized(take=None, systems=None, project=None, overrides=None):
    """A run of the take-off with the coil's design point given, and its air systems."""
    t = T.read(take or ROOM)
    return B.run(t, dict(PROJECT, **COIL) if project is None else project, {"Office": OFFICE},
                 overrides, systems=systems)


def air(r):
    return r.get("air_systems") or {}


def one(r, name=None):
    systems = air(r).get("systems") or []
    if name is None:
        assert len(systems) == 1, [s["name"] for s in systems]
        return systems[0]
    return next(s for s in systems if s["name"] == name)


def both_in(zone="Z1", normal=(1.0, 0.0, 0.0)):
    d = two_offices(list(normal))
    d["spaces"][1]["zone"] = zone
    return d


def test_nothing_is_sized_until_asked_and_the_rooms_still_run():
    r = B.run(T.read(ROOM), PROJECT, {"Office": OFFICE})
    assert r["spaces"][0]["status"] == "ok"                       # never held up
    s = one(r)
    assert s["name"] == "Z1" and s["status"] == "missing" and s["coil"] == {}
    asked = {a["input"]: a for a in air(r)["asked"]}
    assert set(asked) == {"coil_outdoor_dry_bulb_c", "coil_outdoor_wet_bulb_c",
                          "systems.Z1.ventilation_system", "systems.Z1.ez"}, sorted(asked)
    assert asked["coil_outdoor_dry_bulb_c"]["offer"] is None      # Heron holds no figure
    assert asked["coil_outdoor_wet_bulb_c"]["offer"] is None
    assert "Table 6-4" in (asked["systems.Z1.ez"]["offer"] or "")  # offered, never used
    said = B.summary_text(r)
    assert "coil_outdoor_dry_bulb_c" in said and "never fill them in" in said
    plain = B.run(T.read(ROOM), dict(PROJECT, **COIL), {"Office": OFFICE},
                  systems={"Z1": SINGLE})
    assert abs(plain["spaces"][0]["cooling"]["peak"]["total_w"]
               - r["spaces"][0]["cooling"]["peak"]["total_w"]) < 1e-9   # the rooms unmoved


def test_one_room_system_is_the_psychrometric_chain_by_hand():
    r = sized(systems={"Z1": SINGLE})
    s, room = one(r), r["spaces"][0]
    assert s["status"] == "ok", s["why"]
    c = s["coil"]
    # By hand, from heron_psychro's primitives - Doha's 10 m, 24 C / 50 %, 46 / 29 C.
    p = PSY.pressure_at_altitude(10.0)
    wr = PSY.humidity_ratio_from_rh(24.0, 50.0, p)
    wo = PSY.humidity_ratio_from_wet_bulb(46.0, 29.0, p)
    m = room["supply_ls"] / 1000.0 / PSY.specific_volume(13.0, wr, p)
    oa = 2 * 2.5 + 20.0 * 0.3                       # 0.1 people/m2 on 20 m2: 2 people
    mo = oa / 1000.0 / PSY.specific_volume(46.0, wo, p)
    w_mix = (mo * wo + (m - mo) * wr) / m
    h_mix = (mo * PSY.enthalpy(46.0, wo) + (m - mo) * PSY.enthalpy(24.0, wr)) / m
    t_mix = (h_mix - 2501.0 * w_mix) / (1.006 + 1.86 * w_mix)
    latent = max(h["latent_w"] for h in room["cooling"]["hours"])
    w_out = wr - latent / 1000.0 / (m * (2501.0 + 1.86 * 24.0))
    total = m * (h_mix - PSY.enthalpy(13.0, w_out)) * 1000.0
    sensible = m * (1.006 + 1.86 * w_mix) * (t_mix - 13.0) * 1000.0
    assert abs(c["supply_ls"] - room["supply_ls"]) < 1e-9 and abs(c["outdoor_air_ls"] - oa) < 1e-9
    assert abs(c["mass_kg_s"] - m) < 1e-9
    assert abs(c["entering_db_c"] - t_mix) < 1e-6, (c["entering_db_c"], t_mix)
    assert abs(c["total_w"] - total) < 1e-6 * total, (c["total_w"], total)
    assert abs(c["sensible_w"] - sensible) < 1e-6 * sensible, (c["sensible_w"], sensible)
    assert abs(c["shr"] - sensible / total) < 1e-6
    assert abs(c["w_per_m2"] - total / 20.0) < 1e-6 and abs(c["m2_per_kw"] - 20.0 / (total / 1000.0)) < 1e-9
    adp = PSY.apparatus_dew_point(t_mix, w_mix, 13.0, w_out, p)
    assert adp is not None and abs(c["adp_c"] - adp) < 1e-6
    assert abs(c["bypass_factor"] - (13.0 - adp) / (t_mix - adp)) < 1e-6
    sources = [f["source"] for f in s["figures"]]
    assert all(sources) and len(s["figures"]) >= 20               # every value says where from
    assert [a["point"] for a in s["air"]][:2] == ["Outdoor - the coil's design point",
                                                  "Return - the rooms"]


def test_grouped_by_the_modellers_list():
    d = two_offices([1.0, 0.0, 0.0])
    d["spaces"][1]["zone"] = "Z2"
    r = sized(d, {"RTU-1": dict(SINGLE, spaces=["1", "2"])})
    s = one(r)
    assert air(r)["grouped_by"] == "your list" and s["name"] == "RTU-1"
    assert s["status"] == "ok", s["why"]
    assert abs(s["area_m2"] - 40.0) < 1e-9
    assert [x["number"] for x in s["spaces"]] == ["1", "2"]
    by_zone = sized(d, {"Z1": SINGLE, "Z2": SINGLE})
    assert [x["name"] for x in air(by_zone)["systems"]] == ["Z1", "Z2"]


def test_a_space_on_two_systems_or_not_there_stops_its_system():
    d = both_in()
    r = sized(d, {"A": dict(SINGLE, spaces=["1", "2"]), "B": dict(SINGLE, spaces="2, 9")})
    a, b = one(r, "A"), one(r, "B")
    assert a["status"] == b["status"] == "refused"
    assert any("2 Office 02 is listed on A and B" in w for w in a["why"]), a["why"]
    assert any("'9'" in w for w in b["why"]), b["why"]
    r = sized(d, {"A": dict(SINGLE, spaces=["1"]), "B": SINGLE})
    assert one(r, "B")["status"] == "refused" and "lists no Spaces" in one(r, "B")["why"][0]


def test_revits_default_zone_is_on_no_system():
    d = _copy.deepcopy(ROOM)
    d["spaces"][0]["zone"] = "Default"
    r = sized(d, {"Default": SINGLE})
    assert air(r)["systems"] == []
    said = "\n".join(text for _level, text in air(r)["checks"])
    assert "on no air system: 1 Office 01" in said, said
    assert "systems names 'Default'" in said, said
    assert "Air systems: none" in B.summary_text(r)


def test_set_points_the_spaces_disagree_on_are_asked_for_the_system():
    d = both_in()
    r = sized(d, {"Z1": SINGLE}, overrides={"2": {"supply_dry_bulb_c": 14}})
    s = one(r)
    assert s["status"] == "missing"
    assert [a["input"] for a in s["asked"]] == ["systems.Z1.supply_dry_bulb_c"], s["asked"]
    r = sized(d, {"Z1": dict(SINGLE, supply_dry_bulb_c=13)},
              overrides={"2": {"supply_dry_bulb_c": 14}})
    s = one(r)
    assert s["status"] == "ok", s["why"]
    assert any(f["item"] == "Design supply air temperature" and "systems.Z1" in f["source"]
               for f in s["figures"])


def test_a_spaces_exhaust_above_its_own_outdoor_air_is_added():
    r = sized(both_in(), {"Z1": dict(SINGLE, exhaust_ls={"2": 30})})
    s = one(r)
    assert s["status"] == "ok", s["why"]
    rows = {v["space"]: v for v in s["ventilation"]}
    assert rows["2 Office 02"]["set_by"] == "its exhaust" and rows["1 Office 01"]["set_by"] == "62.1"
    assert abs(rows["2 Office 02"]["outdoor_air_ls"] - 30.0) < 1e-9
    assert abs(s["coil"]["vot_ls"] - 22.0) < 1e-9                  # 2 x (2 x 2.5 + 20 x 0.3)
    assert abs(s["coil"]["outdoor_air_ls"] - (22.0 + 30.0 - 11.0)) < 1e-9
    wrong = sized(both_in(), {"Z1": dict(SINGLE, exhaust_ls={"7": 30})})
    assert one(wrong)["status"] == "refused" and "'7'" in one(wrong)["why"][0]


def test_a_refused_space_stops_its_system_and_is_named():
    d = bad_second_office()
    d["spaces"][1]["zone"] = "Z1"
    s = one(sized(d, {"Z1": SINGLE}))
    assert s["status"] == "refused"
    assert any("2 Office 02 was refused" in w for w in s["why"]), s["why"]


def test_a_fan_adds_exactly_its_heat_to_the_coil():
    fan = {"fan_total_pressure_pa": 100, "fan_efficiency_pct": 65, "motor_efficiency_pct": 90,
           "drive_efficiency_pct": 100, "motor_in_airstream": True}
    part = one(sized(systems={"Z1": dict(SINGLE, fan_total_pressure_pa=100)}))
    assert part["status"] == "missing"
    assert {a["input"] for a in part["asked"]} >= {"systems.Z1.fan_position",
                                                   "systems.Z1.fan_efficiency_pct"}
    bare = one(sized(systems={"Z1": SINGLE}))["coil"]
    heat = bare["supply_ls"] / 1000.0 * 100 / 0.65 / 0.9           # motor in the air stream
    for where in ("draw-through", "blow-through"):
        s = one(sized(systems={"Z1": dict(SINGLE, fan_position=where, **fan)}))
        assert s["status"] == "ok", s["why"]
        c = s["coil"]
        assert abs(c["fan_heat_w"] - heat) < 1e-6
        assert abs((c["total_w"] - bare["total_w"]) - heat) < 1e-9 * heat, (
            where, c["total_w"] - bare["total_w"], heat)
        if where == "draw-through":
            assert c["leaving_db_c"] < 13.0 and c["entering_db_c"] == bare["entering_db_c"]
        else:
            assert c["leaving_db_c"] == 13.0 and c["entering_db_c"] > bare["entering_db_c"]


def test_a_100_percent_outdoor_air_unit_cools_outdoor_air():
    s = one(sized(systems={"Z1": dict(SINGLE, ventilation_system="100-percent-outdoor-air")}))
    assert s["status"] == "ok", s["why"]
    c = s["coil"]
    assert abs(c["entering_db_c"] - 46.0) < 1e-9 and abs(c["entering_wb_c"] - 29.0) < 1e-4
    assert abs(c["outdoor_air_ls"] - c["supply_ls"]) < 1e-9
    assert c["total_w"] > one(sized(systems={"Z1": SINGLE}))["coil"]["total_w"] * 3


def test_a_multiple_zone_intake_is_the_engines_own_62_1_figure():
    r = sized(both_in(), {"Z1": dict(SINGLE, ventilation_system="multiple-zone",
                                     ev_method="simplified")})
    s = one(r)
    assert s["status"] == "ok", s["why"]
    zones = [{"name": x["space"], "area_m2": x["area_m2"], "people": x["people"],
              "rp_ls_per_person": x["rp"], "ra_ls_per_m2": x["ra"], "ez": 1.0,
              "primary_flow_ls": sp["supply_ls"]}
             for x, sp in zip(s["ventilation"], r["spaces"])]
    by_engine = H.run("ventilation", {"system": "multiple-zone", "ev_method": "simplified",
                                      "zones": zones})
    assert abs(s["coil"]["vot_ls"] - by_engine["data"]["vot_ls"]) < 1e-9
    asked = one(sized(both_in(), {"Z1": dict(SINGLE, ventilation_system="multiple-zone")}))
    assert [a["input"] for a in asked["asked"]] == ["systems.Z1.ev_method"]


def test_what_cannot_be_a_design_figure_is_refused_in_words():
    hot = one(sized(project=dict(PROJECT, coil_outdoor_dry_bulb_c=30, coil_outdoor_wet_bulb_c=32),
                    systems={"Z1": SINGLE}))
    assert hot["status"] == "refused" and "never above its dry bulb" in hot["why"][0]
    far = one(sized(project=dict(PROJECT, coil_outdoor_dry_bulb_c=460, coil_outdoor_wet_bulb_c=29),
                    systems={"Z1": SINGLE}))
    assert far["status"] == "refused" and "coil_outdoor_dry_bulb_c must be between" in far["why"][0]
    typo = one(sized(systems={"Z1": dict(SINGLE, ez_value=1)}))
    assert typo["status"] == "refused" and "ez_value is not an air system input" in typo["why"][0]
    small = one(sized(systems={"Z1": dict(SINGLE, supply_ls=5)}))
    assert small["status"] == "refused" and "needs 11.0 L/s of outdoor air" in small["why"][0]


def test_the_inputs_are_kept_and_read_back_a_table_intact():
    brain = _brain()
    systems = {"Z1": dict(SINGLE, exhaust_ls={"1": 15})}
    with knowledge_folder():
        first = brain.building_loads(json.dumps(ROOM), {
            "project": dict(PROJECT, **COIL), "profiles": {"Office": OFFICE},
            "systems": systems}, project="project-a")
        s = one(first["result"])
        assert s["status"] == "ok", s["why"]
        kept = first["result"]["inputs"]["systems"]["Z1"]
        assert kept["exhaust_ls"] == {"value": {"1": 15}, "source": "instruction"}, kept
        again = brain.building_loads(json.dumps(ROOM), {}, project="project-a")
        s2 = one(again["result"])
        assert s2["status"] == "ok", s2["why"]
        assert abs(s2["coil"]["outdoor_air_ls"] - 15.0) < 1e-9     # the exhaust, read back
        assert abs(s2["coil"]["total_w"] - s["coil"]["total_w"]) < 1e-6
        assert "cooling coil" in again["said"] and H.NOT_HAP in again["said"]


def test_every_page_says_what_it_is_not():
    a = air(sized(systems={"Z1": SINGLE}))
    text = " ".join(a["not_done"]).lower()
    for words in ("constant volume", "no thermal storage", "no fan static pressure",
                  "no heating coil", "no safety factor"):
        assert words in text, words


if __name__ == "__main__":
    sys.exit(run_all(sys.modules[__name__]))
