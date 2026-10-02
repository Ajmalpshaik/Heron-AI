# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The HVAC design engine - its arithmetic against published values, and its
refusal to supply a design value nobody gave it.

    python tests/test_hvac.py

WHAT IT PROVES
  1. THE PHYSICS MATCHES ITS SOURCES. Saturation pressure against steam-table
     values; a state point against a psychrometric chart, and ASHRAE's own
     Examples 1 and 4 (a state point, and a mixing); the Colebrook
     friction factor against a published library's own doctest; duct friction
     against ten Colebrook points worked by an independent solver and the one
     published duct example found; Huebscher and Heyt & Diaz against their own
     identities; water's density, cp and viscosity against IAPWS. Within
     stated tolerances, never exactly - a chart is read to a few percent.

  2. THE WORKED EXAMPLES COME OUT. ASHRAE 62.1's Ventilation Rate Procedure on
     one office, on three zones by the simplified procedure, by Appendix A and
     as a DOAS, and against 62.1's own Appendix M check value. A small room's
     cooling load summed by hand. A chilled-water flow and a fan's power by
     hand.

  3. HERON SUPPLIES NO DESIGN VALUE (D-33). Every calculation handed nothing
     computes nothing and asks. A reference figure is OFFERED beside the
     question and never used: naming an occupancy does not fill in its rate.
     A missing factor that would REDUCE a load is not applied, and says so.

  4. IT NEVER ROUNDS A DUCT DOWN. Across a sweep of flows the size chosen
     meets the friction rate, and the size below it does not.

  5. A VALUE IT CANNOT USE IS REFUSED, NOT REPAIRED - negative, not a number,
     outside an equation's range, given twice in two units - and a key nobody
     reads is named IGNORED rather than dropped.

  6. IT REACHES NOTHING. The two modules import the standard library and each
     other and nothing else - no network, no file, no Revit - and the MCP
     registry declares the tool READ with no bridge operation.

  7. THE OWNER'S TWO ANSWERS HOLD. A supply neck in an NC/RC 30 room is held
     to the office's 2.5 m/s and the answer says whose figure it is, while
     any other criterion is still asked (D-110). A project's standards are
     asked once, never block the arithmetic, are kept for THAT project only,
     replace an old answer while recording it, survive an unreadable file
     without overwriting it, and are not kept at all when no project is
     known (D-111).

  8. MONTH BY MONTH, A UNIT, AND THE WAY INTO REVIT. The design day's hourly
     shape reproduces ASHRAE's own 2017 workbook. A Doha room run on all
     twelve months peaks on a summer afternoon, and its July 15:00 hour comes
     out as summed by hand. A fan coil or split is picked only from its
     maker's catalogue, never on its total alone, and nothing is picked
     without one. Every Revit tool an answer names is a capability in the
     fragment library.

WHAT IT DOES NOT PROVE
  That any answer is right for a building. A design value is the engineer's;
  this proves the arithmetic agrees with its sources, not that the inputs
  were the right ones. And nothing here has met a real project - docs/41
  says what checking it against a HAP run would take.
"""

from __future__ import print_function

import ast
import json
import math
import os
import re
import sys

ROOT =os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_psychro as PSY                                   # noqa: E402
import heron_hvac as H                                        # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def near(value, expected, tolerance):
    """Within a RELATIVE tolerance."""
    return value is not None and abs(value - expected) <= abs(expected) * tolerance


def result(answer, label):
    for name, text in answer["results"]:
        if name == label:
            return text
    return None


def first_number(text):
    token = text.split()[0].replace(",", "")
    return float(token)


def physics():
    print("1. the physics against its sources")
    # Saturation pressure (Hyland-Wexler) against steam-table values, kPa.
    for t, kpa in ((0.01, 0.61165), (20.0, 2.3392), (25.0, 3.1699),
                   (40.0, 7.3849), (60.0, 19.946), (100.0, 101.418)):
        got = PSY.saturation_pressure(t)
        check(near(got, kpa, 0.0015),
              "saturation pressure at %s C is %.5f kPa, the steam table's %s within 0.15 %%"
              % (t, got, kpa))
    check(near(PSY.pressure_at_altitude(1000.0), 89.875, 0.0005),
          "the standard atmosphere at 1000 m is 89.875 kPa")
    s = PSY.state(25.0, 101.325, rh_pct=50.0)
    check(near(s["w"], 0.00988, 0.005), "25 C 50 %% RH holds %.5f kg/kg - the chart's 0.0099"
          % s["w"])
    check(abs(s["twb_c"] - 17.9) < 0.15, "its wet bulb is %.2f C - the chart's 17.9" % s["twb_c"])
    check(abs(s["tdp_c"] - 13.9) < 0.15, "its dew point is %.2f C - the chart's 13.9" % s["tdp_c"])
    check(near(s["h_kj_kg"], 50.3, 0.005), "its enthalpy is %.2f kJ/kg - the chart's 50.3"
          % s["h_kj_kg"])
    back = PSY.state(25.0, 101.325, twb_c=s["twb_c"])
    check(near(back["w"], s["w"], 1e-6), "the wet bulb solved back gives the same humidity "
          "ratio - the two equations agree with each other")
    # ASHRAE Fundamentals 2017 Ch. 1 Example 1, as two independent test suites
    # quote its printed answers: 40 C dry bulb, 20 C wet bulb, sea level. To
    # the printed precision - the book reads its figures to two or three places.
    ex1 = PSY.state(40.0, 101.325, twb_c=20.0)
    check(abs(ex1["w"] - 0.0065) <= 1e-4 and abs(ex1["h_kj_kg"] - 56.7) <= 0.1
          and abs(ex1["tdp_c"] - 7.0) <= 0.5 and abs(ex1["rh_pct"] - 14.0) <= 1.0
          and near(ex1["v_m3_kg"], 0.896, 0.01),
          "ASHRAE's Example 1, 40 C / 20 C wet bulb: W %.4f, h %.1f, dew point %.1f, %.0f %% RH, "
          "v %.3f - its 0.0065, 56.7, 7, 14, 0.896" % (ex1["w"], ex1["h_kj_kg"], ex1["tdp_c"],
                                                       ex1["rh_pct"], ex1["v_m3_kg"]))
    # ASHRAE's Example 4: 2 m3/s at 4 C / 2 C wet bulb mixed with 6.25 m3/s
    # at 25 C 50 %. Mixed by dry-air mass, which is the volume over v.
    cold = PSY.state(4.0, 101.325, twb_c=2.0)
    room = PSY.state(25.0, 101.325, rh_pct=50.0)
    mixed = PSY.mix(2.0 / cold["v_m3_kg"], cold, 6.25 / room["v_m3_kg"], room, 101.325)
    check(near(mixed["t_c"], 19.5, 0.007),
          "ASHRAE's Example 4, a mixing of two airstreams: %.2f C - its 19.5" % mixed["t_c"])
    # PsychroLib's published example: the dew point of 25 C 80 % RH is
    # 21.309397163661785 C. Both invert the same saturation equation exactly.
    check(abs(PSY.state(25.0, 101.325, rh_pct=80.0)["tdp_c"] - 21.309397163661785) < 1e-6,
          "the dew point of 25 C 80 %% RH is %.9f C - PsychroLib's 21.309397164"
          % PSY.state(25.0, 101.325, rh_pct=80.0)["tdp_c"])
    try:
        PSY.saturation_pressure(250.0)
        refused = False
    except PSY.PsychroRangeError:
        refused = True
    check(refused, "a temperature past the equation's 200 C is refused, never extrapolated")

    check(near(H.friction_factor(1e5, 1e-4), 0.018513866077471, 1e-9),
          "Colebrook at Re 1e5, e/D 1e-4 is the fluids library's own doctest 0.0185138660")
    # Ten Colebrook points worked by an independent solver: Q L/s, D mm, Pa/m,
    # at rho 1.204 and nu 1/66 400 - ASHRAE's standard air - and 0.09 mm.
    for q, d, pa_m in ((50, 100, 5.938), (100, 160, 2.099), (250, 250, 1.264),
                       (500, 315, 1.461), (1000, 400, 1.633), (2000, 500, 1.983),
                       (3000, 630, 1.349), (5000, 800, 1.078), (10000, 1000, 1.318),
                       (20000, 1250, 1.619)):
        got = H.pipe_friction(q, d, 1.204, 1.204 / 66400.0, 0.09)[4]
        check(near(got, pa_m, 0.002), "%d L/s in %d mm round: %.3f Pa/m against %.3f"
              % (q, d, got, pa_m))
    got = H.pipe_friction(500.0, 300.0, 1.2043, 1.8134e-5, 0.09)[4]
    check(near(got, 1.848, 0.01), "the published duct example, 500 L/s in 300 mm: %.3f Pa/m "
          "against its 1.848 (it used Altshul-Tsal; Colebrook here, within 1 %%)" % got)
    check(near(H.huebscher(600, 300), 457.0, 0.0005), "Huebscher: 600 x 300 mm is 457 mm round")
    check(near(H.huebscher(400, 400) / 400.0, 1.0932, 0.0002), "a square duct's equivalent is "
          "1.0932 x its side")
    check(near(H.flat_oval_equivalent(500, 250), 369.7, 0.0005), "Heyt & Diaz: 500 x 250 flat "
          "oval is 369.7 mm round")
    check(near(H.flat_oval_equivalent(400, 400) / 400.0, 1.0011, 0.0002),
          "a flat oval with equal axes is (almost exactly) its own circle")

    # Water at 101.325 kPa against IAPWS, as the iapws 1.5.5 package computes
    # it - IAPWS-95 for density and cp, IAPWS 2008 for viscosity: C, kg/m3,
    # kJ/kg.K, mPa.s. docs/41 s9 states the tolerances for 0 to 99 C.
    for t, rho, cp, mu in ((5, 999.9666, 4.20504, 1.5182), (10, 999.7025, 4.19516, 1.3059),
                           (20, 998.2072, 4.18405, 1.0016), (40, 992.2164, 4.17941, 0.65273),
                           (60, 983.1958, 4.18495, 0.46604), (80, 971.7904, 4.19675, 0.35405)):
        got = (PSY.water_density(t), PSY.water_cp(t), PSY.water_viscosity(t) * 1000.0)
        check(near(got[0], rho, 1.5e-5) and near(got[1], cp, 2e-5) and near(got[2], mu, 3.2e-3),
              "water at %d C: %.4f kg/m3, %.5f kJ/kg.K, %.4f mPa.s against IAPWS's "
              "%s, %s, %s" % (t, got[0], got[1], got[2], rho, cp, mu))

    # The sun over Doha on 21 July with ASHRAE's 2017 optical depths for the
    # month, against the clear-sky chain that reproduces ASHRAE's own 2017
    # load-calculation workbook: hour, altitude, azimuth, beam, diffuse.
    n = H.day_of_year(7, 21)
    for hour, beta, phi, eb, ed in ((9, 52.938, -90.917, 585.3, 239.9),
                                    (12, 83.349, 44.572, 664.3, 268.5),
                                    (15, 43.949, 94.676, 533.6, 222.3)):
        sun = H.sun_position(25.261, 51.565, 3, n, hour)
        beam, diffuse, _m = H.clear_sky(sun["eo"], sun["altitude"], 0.686, 1.592)
        check(abs(sun["altitude"] - beta) < 0.01 and abs(sun["azimuth"] - phi) < 0.01
              and near(beam, eb, 0.001) and near(diffuse, ed, 0.001),
              "Doha 21 July %02d:00: sun at %.3f / %.3f deg, %.1f beam and %.1f diffuse W/m2"
              % (hour, sun["altitude"], sun["azimuth"], beam, diffuse))
    noon = H.sun_position(25.261, 51.565, 3, n, 12.0)
    lst = 12.0 - noon["et_min"] / 60.0 - (51.565 - 45.0) / 15.0
    sun = H.sun_position(25.261, 51.565, 3, n, lst)
    beam, diffuse, _m = H.clear_sky(sun["eo"], sun["altitude"], 0.686, 1.592)
    check(abs(beam - 665.6) < 3.15 and abs(diffuse - 268.1) < 3.15,
          "at solar noon %.1f / %.1f W/m2 - ASHRAE's own Doha July table says 665.6 / 268.1, "
          "printed to 1 Btu/h.ft2 (3.15 W/m2)" % (beam, diffuse))
    sun = H.sun_position(25.261, 51.565, 3, n, 15)
    beam, diffuse, _m = H.clear_sky(sun["eo"], sun["altitude"], 0.686, 1.592)
    west = H.on_surface(sun["altitude"], sun["azimuth"], beam, diffuse, 90.0, 90.0, 0.2)
    check(abs(west["incidence"] - 44.15) < 0.01 and near(west["beam"], 382.9, 0.001)
          and near(west["diffuse"], 227.8, 0.001) and near(west["reflected"], 59.3, 0.002)
          and near(west["total"], 670.0, 0.001),
          "a west wall in Doha at 15:00 on 21 July takes %.1f W/m2 at %.2f deg - "
          "382.9 beam + 227.8 sky + 59.3 ground" % (west["total"], west["incidence"]))
    print()


def worked_examples():
    print("2. the worked examples come out")
    office = {"name": "office", "area_m2": 100, "people": 5, "rp_ls_per_person": 2.5,
              "ra_ls_per_m2": 0.3, "ez": 1.0}
    one = H.run("ventilation", {"system": "single-zone", "zones": [office]})
    check(one["status"] == "ok" and near(first_number(result(one, "Outdoor air intake Vot")),
                                         42.5, 1e-6),
          "62.1, one office of 100 m2 and 5 people: Vot = 2.5 x 5 + 0.3 x 100 = 42.5 L/s")
    zones = [dict(office, primary_flow_ls=400),
             {"name": "conference", "area_m2": 30, "people": 15, "rp_ls_per_person": 2.5,
              "ra_ls_per_m2": 0.3, "ez": 1.0, "primary_flow_ls": 150},
             {"name": "office 2", "area_m2": 200, "people": 10, "rp_ls_per_person": 2.5,
              "ra_ls_per_m2": 0.3, "ez": 1.0, "primary_flow_ls": 800}]
    simple = H.run("ventilation", {"system": "multiple-zone", "zones": zones,
                                   "system_population": 24, "ev_method": "simplified"})
    check(simple["status"] == "ok"
          and near(first_number(result(simple, "Outdoor air intake Vot")), 212.0, 1e-4),
          "three zones, Ps 24, simplified procedure: D 0.8, Vou 159, Ev 0.75, Vot 212.0 L/s")
    check(any(level == "OK" for level, _t in simple["checks"]),
          "and each zone's minimum primary airflow is checked against 1.5 x Voz (Eq. 6-9)")
    appendix = H.run("ventilation", {"system": "multiple-zone", "zones": zones,
                                     "system_population": 24, "ev_method": "appendix-a",
                                     "system_primary_flow_ls": 1200})
    check(appendix["status"] == "ok"
          and near(first_number(result(appendix, "Outdoor air intake Vot")), 193.3, 1e-3),
          "the same by Appendix A with Vps 1200: the conference room is critical, Ev "
          "0.8225, Vot 193.3 L/s")
    doas = H.run("ventilation", {"system": "100-percent-outdoor-air", "zones": zones})
    check(doas["status"] == "ok"
          and near(first_number(result(doas, "Outdoor air intake Vot")), 174.0, 1e-6),
          "the same zones on a DOAS: Vot = sum of Voz = 174.0 L/s, no diversity, no Ev")
    m1 = H.run("ventilation", {"system": "multiple-zone", "zones": [office],
                               "ev_method": "simplified"})
    per_area = first_number(result(m1, "Per floor area"))
    check(near(per_area, 0.567, 0.002),
          "62.1 Appendix M's office check value, 0.57 L/s per m2: got %.3f" % per_area)

    room = {"standard_air": True, "floor_area_m2": 50, "room_dry_bulb_c": 24,
            "outdoor_dry_bulb_c": 46, "supply_dry_bulb_c": 13,
            "windows": [{"area_m2": 10, "u_w_m2k": 2.0, "shgc": 0.25, "irradiance_w_m2": 500}],
            "walls": [{"area_m2": 20, "u_w_m2k": 0.5, "temp_difference_k": 15}],
            "people": {"count": 5, "sensible_w_each": 75, "latent_w_each": 55},
            "lighting": {"w_per_m2": 10},
            "equipment": [{"w_per_m2": 15}]}
    load = H.run("cooling_load", room)
    sensible = first_number(result(load, "Room sensible load") or "0")
    latent = first_number(result(load, "Room latent load") or "0")
    check(load["status"] == "ok" and near(sensible, 3465.0, 1e-6) and near(latent, 275.0, 1e-6),
          "a 50 m2 room summed by hand: 440 + 1250 + 150 + 375 + 500 + 750 = 3465 W "
          "sensible, 275 W latent")
    airflow = first_number(result(load, "Supply airflow") or "0")
    check(near(airflow, 3465.0 / (1.23 * 11.0), 0.0005),
          "its supply air at 13 C in standard air is 3465 / (1.23 x 11) = %.1f L/s" % airflow)
    check(any(H.NOT_HAP in line for line in load["method"]),
          "and the answer says it is a peak estimate, not an hourly simulation like HAP")
    check(any("use factor" in line for line in load["assumed"]),
          "a lighting load with no use factor is not reduced - and the answer says so")
    doha = H.run("cooling_load", {"floor_area_m2": 20, "room_dry_bulb_c": 23,
                                  "room_rh_pct": 50, "outdoor_dry_bulb_c": 45.7,
                                  "outdoor_wet_bulb_c": 22.2, "altitude_m": 10.7,
                                  "outdoor_air_ls": 20,
                                  "people": {"count": 2, "sensible_w_each": 75,
                                             "latent_w_each": 55}})
    check(doha["status"] == "ok"
          and any(level == "WARN" and "DRIER than the room" in text
                  for level, text in doha["checks"]),
          "Doha's July 0.4 %% peak dry bulb (45.7/22.2 C) is drier than a 23 C 50 %% room, "
          "and the answer says the latent design needs the dehumidification condition")

    chw = H.run("chw_flow", {"load_kw": 100, "supply_temp_c": 6, "return_temp_c": 12})
    expected = 100.0 / (PSY.water_cp(9.0) * 6.0) / PSY.water_density(9.0) * 1000.0
    check(chw["status"] == "ok" and near(first_number(result(chw, "Water flow")), expected, 1e-3),
          "100 kW at 6/12 C: %.3f L/s of water" % expected)
    fan = H.run("fan_power", {"flow_ls": 1000, "total_pressure_pa": 500,
                              "fan_efficiency_pct": 70, "motor_efficiency_pct": 90,
                              "drive_efficiency_pct": 100})
    check(fan["status"] == "ok"
          and near(first_number(result(fan, "Electrical input")), 500 / 0.7 / 0.9, 1e-3),
          "1000 L/s at 500 Pa through 70 % and 90 %: 793.7 W, 0.794 W per L/s")
    tf = H.run("terminal_flows", {"flow_ls": 100, "terminal_ids": ["101", "102", "103"]})
    flows = [float(line.split(",")[1]) for line in tf["csv"].strip().split("\n")[1:]]
    check(abs(sum(flows) - 100.0) < 1e-9 and sorted(flows) == [33.3, 33.3, 33.4],
          "100 L/s over three terminals is 33.4 + 33.3 + 33.3 - nothing lost or invented")
    check(tf["csv"].startswith("element_id,flow_ls\n"),
          "and the file SET_AIR_TERMINAL_FLOW reads starts with its header line")
    conv = H.run("convert", {"value": 500, "from": "cfm", "to": "l/s"})
    check(near(first_number(conv["results"][0][1]), 235.9737, 1e-5),
          "500 cfm is 235.97 L/s, from the exact cubic foot")
    tr = H.run("convert", {"value": 1, "from": "TR", "to": "kW"})
    check(near(first_number(tr["results"][0][1]), 3.51685, 1e-5), "one ton of refrigeration is "
          "3.51685 kW")
    print()


def no_design_value():
    print("3. Heron supplies no design value (D-33)")
    for name in H.CALCULATIONS:
        if name == "reference":
            continue
        got = H.run(name, {})
        check(got["status"] == "missing" and not got["results"] and not got["tables"],
              "%s handed nothing computes nothing and asks for %d input(s)"
              % (name, len(got["missing"])))
    asked = H.run("ventilation", {"system": "single-zone",
                                  "zones": [{"occupancy": "office space", "area_m2": 100,
                                             "people": 5, "ez": 1.0}]})
    rp = [m for m in asked["missing"] if m["input"].endswith("rp_ls_per_person")]
    check(asked["status"] == "missing" and not asked["results"],
          "naming an occupancy does NOT fill in its rate - the answer still asks")
    check(rp and "2.5" in rp[0].get("reference", "") and "do not assume" in rp[0]["reference"],
          "and 62.1's 2.5 L/s per person is offered beside the question, never applied")
    people = H.run("ventilation", {"system": "single-zone",
                                   "zones": [{"occupancy": "office space", "area_m2": 200,
                                              "rp_ls_per_person": 2.5, "ra_ls_per_m2": 0.3,
                                              "ez": 1.0}]})
    pz = [m for m in people["missing"] if m["input"].endswith("people")]
    check(pz and "10.0 people" in pz[0].get("reference", ""),
          "a missing population is offered as 62.1's default density times the area, "
          "marked as allowed only when the real one cannot be established")
    size = H.run("duct_size", {"flow_ls": 500, "shape": "round", "standard_air": True,
                               "material": "galvanized steel",
                               "round_sizes_mm": [200, 250, 315, 400]})
    check(size["status"] == "missing"
          and size["missing"][0]["input"] == "max_friction_pa_m",
          "a duct is not sized until somebody says to what - no friction rate is assumed")
    air = H.run("duct_friction", {"flow_ls": 500, "diameter_mm": 300, "material": "galvanized steel"})
    check(air["status"] == "missing" and any(m["input"] == "altitude_m" for m in air["missing"]),
          "nor is the air: standard air or a temperature AND an altitude must be said")
    print()


def never_rounds_down():
    print("4. it never rounds a duct down")
    sizes = [100, 125, 160, 200, 250, 315, 400, 500, 630, 800, 1000, 1250]
    wrong = []
    for q in (40, 75, 130, 260, 480, 900, 1700, 3100, 6000):
        got = H.run("duct_size", {"flow_ls": q, "max_friction_pa_m": 1.0, "shape": "round",
                                  "round_sizes_mm": sizes, "standard_air": True,
                                  "roughness_mm": 0.09})
        d = first_number(result(got, "Size"))
        here = H.pipe_friction(q, d, 1.204, 1.204 / 66400.0, 0.09)[4]
        index = sizes.index(int(d))
        below = (H.pipe_friction(q, sizes[index - 1], 1.204, 1.204 / 66400.0, 0.09)[4]
                 if index else None)
        if here > 1.0 or (below is not None and below <= 1.0):
            wrong.append(q)
    check(not wrong, "nine flows sized at 1 Pa/m: each size meets it and the size below "
          "does not%s" % ("" if not wrong else " - wrong at %s" % wrong))
    rect = H.run("duct_size", {"flow_ls": 800, "max_friction_pa_m": 1.0, "shape": "rectangular",
                               "rect_step_mm": 50, "fixed_height_mm": 300,
                               "standard_air": True, "roughness_mm": 0.09})
    w = int(first_number(result(rect, "Size")))
    at = H.duct_friction_at(800, "rectangular", H.Air(1.204, 1.204 / 66400.0, "", True), 0.09,
                            w=w, h=300)["pa_m"]
    narrower = H.duct_friction_at(800, "rectangular", H.Air(1.204, 1.204 / 66400.0, "", True),
                                  0.09, w=w - 50, h=300)["pa_m"]
    check(at <= 1.0 < narrower, "800 L/s at a fixed 300 mm depth: %d x 300 meets 1 Pa/m and "
          "%d x 300 does not" % (w, w - 50))
    both = H.run("duct_size", {"flow_ls": 1000, "max_friction_pa_m": 2.0, "max_velocity_ms": 5.0,
                               "shape": "round", "round_sizes_mm": sizes,
                               "standard_air": True, "roughness_mm": 0.09})
    check(first_number(result(both, "Size")) == 630,
          "with a loose friction rate and a 5 m/s cap, the velocity decides: 630 mm")
    print()


def refusals():
    print("5. what it cannot use is refused, and what it does not read is named")
    for label, name, inputs in (
            ("a negative flow", "duct_friction", {"flow_ls": -5, "diameter_mm": 300,
                                                  "standard_air": True, "roughness_mm": 0.09}),
            ("a flow given twice", "duct_friction", {"flow_ls": 500, "flow_cfm": 1060,
                                                     "diameter_mm": 300, "standard_air": True,
                                                     "roughness_mm": 0.09}),
            ("standard air AND a temperature", "duct_friction",
             {"flow_ls": 500, "diameter_mm": 300, "standard_air": True, "air_temp_c": 13,
              "roughness_mm": 0.09}),
            ("text where a number belongs", "air_changes", {"volume_m3": "big", "ach": 6}),
            ("two humidity readings at once", "psychrometrics",
             {"dry_bulb_c": 25, "rh_pct": 50, "wet_bulb_c": 18, "altitude_m": 0}),
            ("supply air warmer than the room", "supply_airflow",
             {"sensible_load_w": 2000, "room_dry_bulb_c": 24, "supply_dry_bulb_c": 26,
              "standard_air": True}),
            ("a single-zone system with two zones", "ventilation",
             {"system": "single-zone", "zones": [{"area_m2": 1}, {"area_m2": 2}]})):
        got = H.run(name, inputs)
        check(got["status"] == "refused" and not got["results"], "%s is refused" % label)
    nan = H.run("air_changes", {"volume_m3": float("nan"), "ach": 6})
    check(nan["status"] == "refused", "a NaN is refused, not calculated with")
    typo = H.run("duct_friction", {"flow_lps": 500, "diameter_mm": 300, "standard_air": True,
                                   "roughness_mm": 0.09})
    check("flow_lps" in typo["ignored"] and typo["status"] == "missing",
          "a mistyped flow_lps is named IGNORED, and the real flow_ls is still asked for")
    unknown = H.run("size_everything", {})
    check(unknown["status"] == "unknown" and "duct_size" in unknown["known"],
          "a calculation Heron does not have is said so, with the list")
    bad = H.run("duct_size", "{not json")
    check(bad["status"] == "refused", "inputs that are not a JSON object are refused")
    print()


def reaches_nothing():
    print("6. it reaches nothing")
    allowed = {"collections", "json", "math", "os", "sys", "heron_psychro"}
    for module in ("heron_hvac", "heron_psychro"):
        tree = ast.parse(open(os.path.join(ROOT, "brain", module + ".py")).read())
        names = set()
        for node in ast.walk(tree):
            if isinstance(node, ast.Import):
                names.update(alias.name.split(".")[0] for alias in node.names)
            elif isinstance(node, ast.ImportFrom):
                names.add((node.module or "").split(".")[0])
        check(names <= allowed, "%s imports only %s" % (module, ", ".join(sorted(names))))
    try:
        import heron_tools as TOOLS
        check(TOOLS.TOOLS.get("heron_hvac") == (TOOLS.READ, None),
              "the MCP registry declares heron_hvac READ, with no bridge operation")
    except ImportError as why:
        check(False, "heron_tools could not be imported: %s" % why)
    for entry in H.catalogue():
        if entry["calculation"] == "reference":
            continue
        check(entry["needs"] and entry["purpose"],
              "%s says what it is for and what it needs, derived by running it on nothing"
              % entry["calculation"])
    text = H.describe(H.run("convert", {"value": 1, "from": "kw"}))
    check(H.DISCLAIMER in text, "every completed answer carries the disclaimer")
    print()


FOUR = ("ventilation_standard", "energy_standard", "qcs_edition", "cibse_beside_ashrae")
STANDARDS = {"ventilation_standard": "ASHRAE 62.1-2022", "energy_standard": "90.1-2022",
             "qcs_edition": "QCS 2014", "cibse_beside_ashrae": False}


def checks_text(answer):
    return " | ".join(text for _level, text in answer["checks"])


def owners_answers():
    print("7. the owner's two answers hold (D-110, D-111)")
    # ASKED, NOT CALLED BLIND (.claude/skills/heron-ship s2a): against an engine
    # without these, every check below must FAIL and say which, not stop the
    # suite at the first unknown keyword.
    takes_record = "recorded" in H.run.__code__.co_varnames
    check(takes_record, "the engine takes a project's record at all - run(..., recorded=)")
    H_run = H.run

    def run(name, inputs, recorded=None):
        if takes_record:
            return H_run(name, inputs, recorded=recorded)
        return dict(H_run(name, inputs), ask_once=None, standards={})

    def asks(answer):
        return [q["input"] for q in (answer.get("ask_once") or [])]

    def std_of(answer, name):
        return (answer.get("standards") or {}).get(name) or {}

    neck = {"flow_ls": 120, "neck_sizes_mm": [150, 200, 250, 300], "neck_shape": "round"}

    at_30 = run("diffuser_select", dict(neck, max_nc=30))
    limit = result(at_30, "Neck velocity limit") or ""
    check(at_30["status"] == "ok" and result(at_30, "Neck size") == "250 mm round"
          and "2.5 m/s" in limit and "D-110" in limit and "2.2" in limit,
          "an NC/RC 30 room's supply neck is held to the office's 2.5 m/s, and the answer "
          "says whose figure it is and what ASHRAE prints instead: %s" % limit)
    at_35 = run("diffuser_select", dict(neck, max_nc=35))
    asked = [m for m in at_35["missing"] if m["input"] == "max_neck_velocity_ms"]
    check(at_35["status"] == "missing" and asked and "RC/NC 35" in asked[0].get("reference", ""),
          "NC 35 is not the office's figure's condition: the limit is ASKED, with ASHRAE's "
          "row for NC 35 offered beside it")
    given = run("diffuser_select", dict(neck, max_nc=30, max_neck_velocity_ms=2.0))
    check(given["status"] == "ok" and result(given, "Neck size") == "300 mm round"
          and "as given" in (result(given, "Neck velocity limit") or ""),
          "a figure the modeller states wins over the office's own")
    nothing = run("diffuser_select", neck)
    check(nothing["status"] == "missing"
          and any(m["input"] == "max_neck_velocity_ms" for m in nothing["missing"]),
          "with no criterion and no limit, nothing is applied - it asks")
    check(H.reference_lookup("air_terminal_guidance", "RC/NC 30")[1] == 2.2,
          "ASHRAE's table is kept as ASHRAE prints it - the office's figure sits beside it")

    office = {"system": "single-zone", "zones": [{"name": "office", "area_m2": 100,
                                                  "people": 5, "rp_ls_per_person": 2.5,
                                                  "ra_ls_per_m2": 0.3, "ez": 1.0}]}
    unknown = run("ventilation", office)
    check(unknown["status"] == "ok" and asks(unknown) == list(FOUR)
          and "not checked" in checks_text(unknown)
          and "ASK ONCE FOR THIS PROJECT" in H.describe(unknown),
          "with the project's standards unknown it still calculates, asks all four ONCE at "
          "the top of the answer, and says the check it could not run")
    recorded = dict((k, {"value": v, "recorded": "2026-10-02T00:00:00Z"})
                    for k, v in STANDARDS.items())
    kept = run("ventilation", office, recorded=recorded)
    check(kept.get("ask_once") == [] and std_of(kept, "ventilation_standard").get("from") == "record"
          and "62.1-2022 governs" in checks_text(kept),
          "with the project's record handed in, nothing is asked and the check is made")
    told = run("ventilation", dict(office, ventilation_standard="62.1-2019"), recorded=recorded)
    check(std_of(told, "ventilation_standard") == {"value": "62.1-2019", "from": "request"}
          and "follows ASHRAE 62.1-2019" in checks_text(told),
          "a standard said in the request wins over the record, and an edition Heron's "
          "tables are not from is said beside the answer")
    other = run("ventilation", dict(office, ventilation_standard="other"), recorded=recorded)
    check("a comparison and not the project's requirement" in checks_text(other),
          "a project not governed by 62.1 is told the procedure is only a comparison")

    coil = {"load_kw": 100, "supply_temp_c": 6, "return_temp_c": 12}
    none = run("chw_flow", dict(coil, energy_standard="none"), recorded=recorded)
    check(none["status"] == "ok" and "not this project's" in checks_text(none)
          and "Section 6.5.4.7) - this one" not in checks_text(none),
          "a project with no energy code is not held to 90.1's coil rule")
    applies = run("chw_flow", coil, recorded=recorded)
    check(any(level == "WARN" and "90.1-2022 applies" in text for level, text in applies["checks"]),
          "a 90.1 project's 6 K coil is flagged against the 8.33 K rule")
    loads = run("cooling_load", {"floor_area_m2": 20, "room_dry_bulb_c": 23,
                                 "people": {"count": 2, "sensible_w_each": 75,
                                            "latent_w_each": 55}},
                recorded=recorded)
    reported = getattr(H, "QCS_2014_REPORTED", "46 C DB / 30 C WB")
    check(reported in checks_text(loads) and "search summaries" in checks_text(loads),
          "a QCS 2014 project's load answer carries what QCS 2014 is reported to set, and "
          "that it was never read in the text")
    bad = run("ventilation", dict(office, ventilation_standard="62.1"))
    check(bad["status"] == "refused", "an edition with no year is refused, not guessed")
    odd = dict(recorded, energy_standard={"value": "Part L", "recorded": "2026-10-02"})
    again = run("chw_flow", coil, recorded=odd)
    check(asks(again) == ["energy_standard"],
          "a recorded answer the engine cannot read is ASKED AGAIN, never repaired")
    ducts = run("duct_friction", {"flow_ls": 500, "diameter_mm": 300, "standard_air": True,
                                  "roughness_mm": 0.09, "energy_standard": "90.1-2022"})
    check("energy_standard" in ducts["ignored"],
          "a calculation no standard changes names a standard given to it IGNORED")

    import tempfile
    import shutil
    try:
        import heron_designbasis as KEEP
    except ImportError:
        KEEP = None
    check(KEEP is not None, "the project's record has a module to keep it - heron_designbasis")
    import heron_brain as BRAIN
    seam_takes_project = "project" in BRAIN.hvac.__code__.co_varnames
    check(seam_takes_project, "the brain seam takes the open model's project - hvac(..., project=)")
    if KEEP is None or not seam_takes_project:
        print()
        return
    home = tempfile.mkdtemp()
    was = dict((k, os.environ.get(k)) for k in ("HERON_KNOWLEDGE", "HERON_AUDIT"))
    os.environ["HERON_KNOWLEDGE"] = home
    os.environ["HERON_AUDIT"] = os.path.join(home, "audit")
    try:
        changes, _note = KEEP.record("PROJECT-A", {"ventilation_standard": "62.1-2022"})
        check(changes == [("ventilation_standard", None, "62.1-2022")]
              and KEEP.read("PROJECT-A")[0]["ventilation_standard"]["value"] == "62.1-2022",
              "an answer is kept for its project and read back")
        check(KEEP.read("PROJECT-B") == ({}, None)
              and KEEP.path_for("PROJECT-A") != KEEP.path_for("PROJECT-B"),
              "another project has its own file and sees nothing of the first's")
        check(KEEP.record("PROJECT-A", {"ventilation_standard": "62.1-2022"})[0] == [],
              "the same answer again changes nothing")
        changes, _note = KEEP.record("PROJECT-A", {"ventilation_standard": "62.1-2019"})
        held = json.loads(open(KEEP.path_for("PROJECT-A")).read())
        check(changes == [("ventilation_standard", "62.1-2022", "62.1-2019")]
              and held["history"][0]["value"] == "62.1-2022",
              "a changed answer replaces the old one and records the replacement")
        path = KEEP.path_for("PROJECT-C")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        open(path, "w").write("{not json")
        values, note = KEEP.read("PROJECT-C")
        check(values == {} and note and "could not be read" in note,
              "an unreadable record is reported, and nothing in it is used")
        KEEP.record("PROJECT-C", {"qcs_edition": "QCS 2014"})
        aside = [n for n in os.listdir(os.path.dirname(path)) if ".unreadable-" in n]
        check(len(aside) == 1 and open(os.path.join(os.path.dirname(path), aside[0])).read()
              == "{not json", "and it is set aside whole, never overwritten, when a new one starts")
        try:
            KEEP.path_for(None)
            named = True
        except ValueError:
            named = False
        check(not named, "no project key, no file - a project is never guessed")

        first = BRAIN.hvac("ventilation", dict(office, **STANDARDS), project="PROJECT-D",
                           project_name="Tower D")
        check("kept for this project: ventilation_standard = 62.1-2022" in first["text"]
              and first["ask_once"] == [],
              "through the seam, the four answers given once are kept for the open project")
        second = BRAIN.hvac("ventilation", office, project="PROJECT-D")
        check(second["ask_once"] == []
              and second["standards"]["qcs_edition"]["from"] == "record"
              and "recorded for this project" in second["text"],
              "and the next answer there asks nothing and says where each came from")
        elsewhere = BRAIN.hvac("ventilation", office, project="PROJECT-E")
        check(asks(elsewhere) == list(FOUR),
              "a different project is asked afresh - nothing crosses between projects")
        nowhere = BRAIN.hvac("ventilation", dict(office, **STANDARDS), project=None)
        check("NOT KEPT" in nowhere["text"]
              and not os.path.exists(os.path.join(home, "projects", "None" + KEEP.SUFFIX)),
              "with no project known, nothing is kept and the answer says why")
    finally:
        for key, value in was.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value
        shutil.rmtree(home, ignore_errors=True)
    print()


# ASHRAE's own 2017 load-calculation workbook, sheet "Weather Data - OA":
# Atlanta's monthly 5 % design day as the sheet prints it, in F to 0.1 F - the
# month's design DB, mean coincident WB, daily DB range and daily WB range,
# then {hour: (DB, WB)}. In December's small hours the wet bulb is held at the
# dry bulb. The profile is linear, so the same function serves F and C alike.
ATLANTA = (("July", (91.6, 74.3, 20.2, 6.1),
            {2: (73.0, 68.7), 5: (71.4, 68.2), 8: (76.7, 69.8), 12: (89.0, 73.5),
             15: (91.6, 74.3), 18: (86.8, 72.8), 24: (75.0, 69.3)}),
           ("December", (64.6, 59.0, 19.5, 13.2),
            {2: (46.7, 46.7), 5: (45.1, 45.1), 8: (50.2, 49.2), 12: (62.1, 57.3),
             15: (64.6, 59.0), 18: (59.9, 55.8), 24: (48.6, 48.2)}))

# A Doha room with one west window - the room the sweep was checked on by hand.
DOHA_ROOM = {"floor_area_m2": 20, "room_dry_bulb_c": 23, "latitude_deg": 25.261,
             "longitude_deg": 51.565, "utc_offset_h": 3, "ground_reflectance": 0.2,
             "windows": [{"area_m2": 4, "u_w_m2k": 2.8, "shgc": 0.3, "facing": "W"}],
             "people": {"count": 2, "sensible_w_each": 75, "latent_w_each": 55},
             "lighting": {"w_per_m2": 8}}
DOHA_JULY = {"month": 7, "db_c": 45.7, "mcwb_c": 22.2, "db_range_k": 11.7,
             "wb_range_k": 6.6, "tau_b": 0.686, "tau_d": 1.592}

FCU_ROWS = [{"model": "FCU-02", "total_kw": 2.0, "sensible_kw": 1.6, "airflow_ls": 90},
            {"model": "FCU-03", "total_kw": 3.0, "sensible_kw": 2.1, "airflow_ls": 130},
            {"model": "FCU-04", "total_kw": 4.0, "sensible_kw": 3.0, "airflow_ls": 170}]


def table_of(answer, title):
    for got in answer["tables"]:
        if got["title"].startswith(title):
            return got
    return {"rows": []}


def month_by_month():
    print("8. month by month, a unit from its maker's catalogue, and the way into Revit")
    # ASKED, NOT CALLED BLIND (.claude/skills/heron-ship s2a): against an engine
    # without these, a missing function is looked up rather than called and an
    # unknown calculation answers "unknown" - each check FAILS and says which.
    day = getattr(H, "design_day", None)
    check(day is not None, "the engine has the design day's hourly shape - design_day()")
    for month, inputs, hours in ATLANTA:
        got = dict((h, (t, wb)) for h, t, wb in day(*inputs)) if day else {}
        off = [h for h, (t, wb) in sorted(hours.items())
               if h not in got or abs(got[h][0] - t) > 0.05 + 1e-9
               or abs(got[h][1] - wb) > 0.05 + 1e-9]
        check(day is not None and not off,
              "ASHRAE's own workbook, Atlanta's %s design day at seven hours: dry and wet "
              "bulb within its 0.1 F print%s" % (month, "" if not off else " - off at %s" % off))

    year = H.run("monthly_load", dict(DOHA_ROOM, design_weather="doha-0.4"))
    peaks = table_of(year, "Each month's peak")["rows"]
    check(year["status"] == "ok" and len(peaks) == 12,
          "a Doha room is run on all twelve months of the 0.4 % design weather held")
    by_total = sorted(peaks, key=lambda row: float(row[6]))
    top, low = (by_total[-1], by_total[0]) if by_total else ([None] * 7, [None] * 7)
    check(top[0] in ("may", "jun", "jul", "aug") and top[1] in ("13:00", "14:00", "15:00",
                                                                 "16:00"),
          "its west window peaks on a summer afternoon - %s at %s" % (top[0], top[1]))
    check(low[0] in ("nov", "dec", "jan", "feb"),
          "and its lowest monthly peak is in winter - %s" % low[0])
    # By hand: at 15:00 the profile is at the design dry bulb, and section 1
    # checked 670.0 W/m2 on a west wall then. 2 people x 75 + 8 W/m2 x 20 m2 +
    # 2.8 x 4 x (45.7 - 23) + 4 x 0.3 x 670.0 = 1368.2 W; the table prints whole watts.
    hand = 2 * 75 + 8 * 20 + 2.8 * 4 * (45.7 - 23) + 4 * 0.3 * 670.0
    july = [row for row in peaks if row[0] == "jul"]
    check(bool(july) and july[0][1] == "15:00" and abs(float(july[0][4]) - hand) <= 1.5
          and july[0][5] == "110",
          "July peaks at 15:00 with %s W sensible - %.1f W by hand - and 2 x 55 = 110 W latent"
          % (july[0][4] if july else "-", hand))
    check(len(table_of(year, "The peak day hour by hour")["rows"]) == 24
          and getattr(H, "STEADY_HOURS", None) in year["method"],
          "the peak day is shown hour by hour, and the answer says it is steady state with "
          "no storage - not HAP")
    check("not a part-load figure" in (result(year, "Lowest monthly peak") or ""),
          "the lowest month is said to be the smallest peak, not a part-load figure")
    own = H.run("monthly_load", dict(DOHA_ROOM, months=[DOHA_JULY]))
    at_15 = [row for row in table_of(own, "The peak day hour by hour")["rows"]
             if row[0] == "15:00"]
    check(own["status"] == "ok" and bool(at_15) and bool(july) and at_15[0][3] == july[0][4],
          "the same July given as the modeller's own row gives the same 15:00 hour")
    asked = H.run("monthly_load", DOHA_ROOM)
    weather = [m for m in asked["missing"] if m["input"] == "design_weather"]
    check(asked["status"] == "missing" and not asked["results"] and bool(weather)
          and "doha-0.4" in weather[0]["unit"] and bool(weather[0].get("reference")),
          "with no design weather named nothing is run - the set held is offered and asked "
          "for, never chosen for the modeller")
    both = H.run("monthly_load", dict(DOHA_ROOM, design_weather="doha-0.4",
                                      months=[DOHA_JULY]))
    twice = H.run("monthly_load", dict(DOHA_ROOM, months=[DOHA_JULY, DOHA_JULY]))
    check(both["status"] == "refused" and twice["status"] == "refused",
          "a weather set named AND the modeller's own months are refused, and so is a month "
          "given twice")

    fcu = H.run("unit_select", {"unit_type": "fan-coil", "load_kw": 2.5, "sensible_kw": 2.2,
                                "flow_ls": 120, "max_oversize_pct": 25,
                                "catalogue": FCU_ROWS})
    verdicts = dict((row[0], row[-1]) for row in table_of(fcu, "The catalogue")["rows"])
    check(fcu["status"] == "ok" and (result(fcu, "Selected") or "").startswith("FCU-04")
          and verdicts.get("FCU-03", "").startswith("sensible")
          and "total" not in verdicts.get("FCU-03", "total"),
          "FCU-03 covers the 2.5 kW total but not the 2.2 kW sensible, so FCU-04 is chosen - "
          "a unit is never picked on its total alone")
    check(any(level == "WARN" and "60 % over" in text for level, text in fcu["checks"]),
          "and FCU-04, 60 % over the load, is flagged past the 25 % the modeller gave")
    check(any(level == "OK" and "chw_flow" in text for level, text in fcu["checks"]),
          "a fan coil's answer points on to its chilled water - chw_flow, then pipe_size")
    big = H.run("unit_select", {"unit_type": "fan-coil", "load_kw": 5, "catalogue": FCU_ROWS})
    check(big["status"] == "ok" and result(big, "Selected") is None
          and any(level == "FAIL" for level, _t in big["checks"]),
          "a load no unit in the catalogue covers selects nothing and FAILS - the biggest is "
          "never offered as near enough")
    bare = H.run("unit_select", {"unit_type": "split", "load_kw": 2.5})
    check(bare["status"] == "missing" and any(m["input"] == "catalogue" for m in bare["missing"]),
          "with no maker's catalogue nothing is picked - Heron holds no unit's capacity")
    split = H.run("unit_select", {"unit_type": "split", "load_kw": 2.5,
                                  "catalogue": [{"model": "SPL-09", "total_kw": 2.6}]})
    check(split["status"] == "ok" and any(level == "WARN" and "refrigerant piping" in text
                                          for level, text in split["checks"]),
          "a split unit's answer says its refrigerant piping is the maker's to state")
    worse = H.run("unit_select", {"unit_type": "split", "load_kw": 2.5, "sensible_kw": 3,
                                  "catalogue": [{"model": "SPL-09", "total_kw": 2.6}]})
    check(worse["status"] == "refused", "a sensible load bigger than the total is refused")

    neck = H.run("diffuser_select", {"flow_ls": 120, "neck_sizes_mm": [150, 200, 250, 300],
                                     "neck_shape": "round", "max_neck_velocity_ms": 2.5})
    listed = H.run("diffuser_select", {"flow_ls": 120, "max_nc": 30, "catalogue": [
        {"size": "300x300", "flow_ls": 100, "nc": 20},
        {"size": "300x300", "flow_ls": 150, "nc": 28}]})
    check(neck["status"] == listed["status"] == "ok"
          and all(any("CHANGE_ELEMENT_TYPE" in line and "SET_AIR_TERMINAL_FLOW" in line
                      for line in got["next"]) for got in (neck, listed)),
          "a diffuser chosen either way names what puts it in the model - CHANGE_ELEMENT_TYPE "
          "to the size's type, SET_AIR_TERMINAL_FLOW for its flow")

    # Every tool an INTO REVIT line names is a capability the library has, read
    # from the source so a line no test reaches is checked too.
    capabilities = set()
    fragments = os.path.join(ROOT, "brain", "fragments")
    for folder in os.listdir(fragments):
        path = os.path.join(fragments, folder, "fragment.yaml")
        if os.path.isfile(path):
            for line in open(path):
                if line.startswith("capability:"):
                    capabilities.add(line.split(":", 1)[1].strip().strip("'\""))
    named = set()
    tree = ast.parse(open(os.path.join(ROOT, "brain", "heron_hvac.py")).read())
    for node in ast.walk(tree):
        if isinstance(node, ast.Call) and getattr(node.func, "attr", None) == "into_revit":
            for part in ast.walk(node):
                text = ""
                if isinstance(part, ast.Constant) and isinstance(part.value, str):
                    text = part.value
                elif isinstance(part, ast.Name) and isinstance(getattr(H, part.id, None), str):
                    text = getattr(H, part.id)
                named.update(re.findall(r"\b[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)+\b", text))
    unknown = sorted(named - capabilities)
    check(len(named) >= 5 and not unknown,
          "each of the %d Revit tools the answers name is a capability in brain/fragments%s"
          % (len(named), "" if not unknown else " - not found: %s" % ", ".join(unknown)))
    print()


def main():
    physics()
    worked_examples()
    no_design_value()
    never_rounds_down()
    refusals()
    reaches_nothing()
    owners_answers()
    month_by_month()

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1
    print("PASSED - the HVAC engine's arithmetic agrees with its sources and its")
    print("worked examples, it rounds no duct down, and it supplies no design")
    print("value it was not given.")
    print()
    print("It proves nothing about a building. Every design value is the")
    print("engineer's, and no answer here has been checked against a real project.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
