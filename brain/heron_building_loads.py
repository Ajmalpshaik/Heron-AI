# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Every Space's heating and cooling load from a model's take-off - docs/44 section 5.

Builds one heron_hvac input per Space from the take-off, the project's inputs
and the Space's profile, runs the engine's own room calculations, and adds
the answers up by zone and building - the block load hour by hour, beside the
sum of the peaks. It adds no physics (one fact, one home: heron_hvac.py).

A value nobody gave is ASKED, with a standard's figure offered and named, and
never filled in (D-33). A Space whose types carry no thermal values, or whose
profile holds a value out of range, is refused on its own - the rest of the
building still runs. Every value the run used carries where it came from.

Standard library only (heron_designbasis is reached for the knowledge folder
only when a run is saved or loaded, as it is for a project's standards).
"""

import datetime
import hashlib
import io
import json
import math
import os
import re

import heron_hvac as HVAC
import heron_takeoff as TAKEOFF

FORMAT = 1
PROJECT_KEYS = ("design_weather", "room_dry_bulb_c", "room_rh_pct", "supply_dry_bulb_c",
                "ground_reflectance", "outside_surface_coefficient_w_m2k",
                "heating_outdoor_dry_bulb_c", "heating_room_dry_bulb_c",
                "unconditioned_temp_c", "heating_unconditioned_temp_c", "ground_temp_c",
                "altitude_m")
# name: (unit, why, low, high) - the question, and the range a value must sit
# in. A range refuses what cannot be a design figure - a fraction typed for a
# percentage, a value in the wrong unit; it is never a design value (D-33).
PROJECT_ASK = {
    "design_weather": ("one of: doha-0.4 (or months)", "the site's design weather, month by "
                       "month", None, None),
    "room_dry_bulb_c": ("C", "room design dry bulb for cooling", 10, 35),
    "room_rh_pct": ("%", "room design relative humidity for cooling", 10, 90),
    "supply_dry_bulb_c": ("C", "supply air temperature - for each Space's supply airflow", 2, 25),
    "ground_reflectance": ("0-1", "the ground's solar reflectance in front of the walls", 0, 1),
    "outside_surface_coefficient_w_m2k": ("W/m2.K", "outside surface coefficient ho - with "
                                          "each type's absorptance it gives a/ho", 5, 40),
    "heating_outdoor_dry_bulb_c": ("C", "outdoor design dry bulb for heating", -50, 30),
    "heating_room_dry_bulb_c": ("C", "room design dry bulb for heating", 5, 35),
    "unconditioned_temp_c": ("C", "temperature beyond a wall or ceiling to an unconditioned "
                             "space, at the cooling design hour", 0, 70),
    "heating_unconditioned_temp_c": ("C", "temperature beyond a wall or ceiling to an "
                                     "unconditioned space, on the heating design day", -50, 40),
    "ground_temp_c": ("C", "temperature beneath a floor on the ground - for heating", -30, 40),
    "altitude_m": ("m", "site altitude, for air density", -500, 6000),
    "door_absorptance": ("0-1", "solar absorptance of the outside doors whose type carries "
                         "none - asked only when one does", 0, 1),
}
DOOR_KEY = "door_absorptance"
# The set points one Space may have of its own (docs/44 s5.3), beside its profile.
SPACE_KEYS = ("room_dry_bulb_c", "room_rh_pct", "heating_room_dry_bulb_c", "supply_dry_bulb_c")
# name: (unit, why, low, high)
PROFILE_KEYS = {
    "people_per_m2": ("people/m2", "occupant density at the design hour", 0, 10),
    "sensible_w_each": ("W", "sensible heat per person at their activity", 0, 1000),
    "latent_w_each": ("W", "latent heat per person at their activity", 0, 1000),
    "lighting_w_per_m2": ("W/m2", "lighting power density", 0, 500),
    "equipment_w_per_m2": ("W/m2", "equipment power density", 0, 500),
    "infiltration_ach": ("ACH", "infiltration air changes per hour", 0, 20),
    "outdoor_air_ls_per_person": ("L/s per person", "outdoor air per person (62.1 Rp)", 0, 50),
    "outdoor_air_ls_per_m2": ("L/s per m2", "outdoor air per floor area (62.1 Ra)", 0, 50),
}
# Where each named design weather set was measured. The sun is worked out at
# the model's site and the weather is the station's, so the two must be the
# same place: a model left at a template's city would give Doha's weather with
# another city's sun. The station's own coordinates - data, not a design value.
STATIONS = {"doha-0.4": ("Doha International (WMO 411700)", 25.261, 51.565)}
SITE_KM = 150.0
OA_NOTE = ("outdoor air per Space is the breathing-zone sum, people x Rp + area x Ra; zone "
           "air distribution effectiveness and system ventilation efficiency are the "
           "ventilation calculation's and are not applied here")
SOURCES = ("model", "instruction", "assumption")
_MONTHS = ("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec")


class _Refused(ValueError):
    pass


def _value(v):
    return v.get("value") if isinstance(v, dict) else v


def _labelled(d):
    out = {}
    for k, v in (d or {}).items():
        if _value(v) is None:
            continue
        out[k] = dict(v) if isinstance(v, dict) else {"value": v, "source": "instruction"}
        out[k].setdefault("source", "instruction")
    return out


def profile_key(space):
    """The Space Type's name, else the Space's name without its number, else (unnamed)."""
    if space.get("space_type"):
        return space["space_type"]
    name = re.sub(r"[\s\-_]*\d+$", "", (space.get("name") or "").strip())
    return name or "(unnamed)"


def _number(key, raw, unit, low, high):
    try:
        if isinstance(raw, bool):
            raise TypeError(key)
        v = float(raw)
    except (TypeError, ValueError):
        raise _Refused("%s must be a number in %s - got %r" % (key, unit, raw))
    if v != v or (low is not None and not low <= v <= high):
        raise _Refused("%s must be between %s and %s %s - got %s" % (key, low, high, unit, raw))
    return v


def _checked(profile):
    return dict((k, _number(k, _value(profile.get(k)), unit, low, high))
                for k, (unit, _why, low, high) in PROFILE_KEYS.items())


def _checked_project(project):
    """The project's numbers, each in its range - or _Refused naming the one that is not."""
    out = {}
    for k, (unit, _why, low, high) in PROJECT_ASK.items():
        raw = _value((project or {}).get(k))
        if raw is None or k == "design_weather":
            continue
        out[k] = _number(k, raw, unit, low, high)
    return out


def _table(project):
    """The table of answers, project["beyond"] = {element: word} - as given, or labelled
    {"value": table, "source": ...}. Not _value(): that takes ANY dict for a labelled value
    and returns its "value" key, which a table has not got, so the table vanished
    (FRAGMENT-ISSUES 5b-332)."""
    raw = (project or {}).get("beyond")
    if isinstance(raw, dict) and "value" in raw:
        raw = raw["value"]
    return raw if isinstance(raw, dict) else {}


def _given_answers(project):
    """Every word given about what is beyond a face, as typed - right or wrong. A
    "beyond:<element>" key wins over the table's row for the same element."""
    out = dict((str(k), _value(v)) for k, v in _table(project).items())
    for k, v in (project or {}).items():
        if isinstance(k, str) and k.startswith("beyond:"):
            out[k.split(":", 1)[1]] = _value(v)
    return out


def flat(project):
    """The project's inputs with a table of answers written out as the "beyond:<element>"
    keys the Companion's questions post - one shape from here on, so each answer is kept,
    merged and labelled on its own, and a later table changes only the elements it names.
    A key given beside the table wins over the table's row for the same element."""
    out = dict(project or {})
    raw, table = out.get("beyond"), _table(out)
    if table or (isinstance(raw, dict) and "value" not in raw):
        for k, v in table.items():
            out.setdefault("beyond:%s" % k, v)
        del out["beyond"]
    return out


def answers(project, strict=True):
    """What the modeller said is beyond each face Revit could not see past: {element: word}.

    Given as project["beyond"] = {element: word} - labelled or not - or as
    "beyond:<element>" keys, the shape the Companion's questions post. A word
    that is not one of the four refuses the run when strict, and is left
    unanswered when not.
    """
    out = _given_answers(project)
    for k in sorted(out):
        if out[k] not in TAKEOFF.ANSWERS:
            if strict:
                raise _Refused("beyond:%s must be one of %s - got %r"
                               % (k, ", ".join(TAKEOFF.ANSWERS), out[k]))
            del out[k]
    return out


def _opaque(recs, ho, door_absorptance=None):
    out = []
    for r in recs:
        r = dict(r)
        door = r.pop("door", False)
        a = r.pop("absorptance", None)
        if a is None and door and door_absorptance is not None:
            a = door_absorptance
        if a is None:
            raise _Refused("%s has no solar absorptance in the model - %s"
                           % (r["name"], "give door_absorptance" if door
                              else "set its type's thermal properties"))
        r["absorptance_over_ho"] = float(a) / ho
        out.append(r)
    return out


def _surfaces(t, space, said):
    s = TAKEOFF.surfaces(t, space, said)
    if s["refused"]:
        raise _Refused("; ".join(s["refused"]))
    return s


def _doors_without_absorptance(t, space, said):
    return any(w.get("door") and w.get("absorptance") is None
               for w in TAKEOFF.surfaces(t, space, said)["walls"])


def monthly_inputs(t, space, project, profile):
    """The engine's monthly_load inputs for one Space - exposed so a test can call it by hand."""
    s = _surfaces(t, space, answers(project))
    p = _checked(profile)
    q = _checked_project(project)
    area = float(space["area_m2"])
    people = int(round(p["people_per_m2"] * area))
    ho = q["outside_surface_coefficient_w_m2k"]
    door = q.get(DOOR_KEY)
    outdoor_air = people * p["outdoor_air_ls_per_person"] + area * p["outdoor_air_ls_per_m2"]
    i = {"floor_area_m2": area, "room_height_m": space.get("height_m"),
         "latitude_deg": t.site.get("latitude_deg"), "longitude_deg": t.site.get("longitude_deg"),
         "utc_offset_h": t.site.get("utc_offset_h"),
         "ground_reflectance": q["ground_reflectance"],
         "room_dry_bulb_c": q["room_dry_bulb_c"], "room_rh_pct": q["room_rh_pct"],
         "altitude_m": q["altitude_m"],
         "walls": _opaque(s["walls"], ho, door) + [
             dict(r, absorptance_over_ho=0.0, facing=0.0, no_direct_sun=True)
             for r in s["exposed_floors"]],
         "roofs": _opaque(s["roofs"], ho),
         "windows": s["windows"], "skylights": s["skylights"],
         "partitions": [dict(x, adjacent_temp_c=q["unconditioned_temp_c"])
                        for x in s["partitions"]],
         "people": {"count": people, "sensible_w_each": p["sensible_w_each"],
                    "latent_w_each": p["latent_w_each"]},
         "lighting": {"w_per_m2": p["lighting_w_per_m2"]},
         "equipment": [{"w_per_m2": p["equipment_w_per_m2"]}],
         "infiltration": {"ach": p["infiltration_ach"]},
         # None when no outdoor air is given: the engine refuses a zero flow,
         # and then the coil load is the room load.
         "outdoor_air_ls": outdoor_air if outdoor_air > 0 else None}
    if _value(project.get("months")) is not None:
        i["months"] = _value(project["months"])
    else:
        i["design_weather"] = _value(project["design_weather"])
    return {k: v for k, v in i.items() if v not in (None, [])}


def heating_inputs(t, space, project, profile):
    """The engine's heating_load inputs for one Space.

    A partition loses to the HEATING unconditioned temperature - the cooling
    one is a summer figure - and a floor on the ground to the ground's.
    """
    s = _surfaces(t, space, answers(project))
    p = _checked(profile)
    q = _checked_project(project)
    surf = [{"name": r["name"], "area_m2": r["area_m2"], "u_w_m2k": r["u_w_m2k"]}
            for r in s["walls"] + s["roofs"] + s["windows"] + s["skylights"]
            + s["exposed_floors"]]
    surf += [dict(r, adjacent_temp_c=q["heating_unconditioned_temp_c"]) for r in s["partitions"]]
    surf += [dict(r, adjacent_temp_c=q["ground_temp_c"]) for r in s["floors"]]
    i = {"floor_area_m2": float(space["area_m2"]), "room_height_m": space.get("height_m"),
         "surfaces": surf, "room_dry_bulb_c": q["heating_room_dry_bulb_c"],
         "outdoor_dry_bulb_c": q["heating_outdoor_dry_bulb_c"],
         "infiltration": {"ach": p["infiltration_ach"]}, "altitude_m": q["altitude_m"]}
    return {k: v for k, v in i.items() if v not in (None, [])}


def _offers():
    """Each input's offered figure and its standard - from the engine, never invented here."""
    engine = {}
    for name in ("monthly_load", "heating_load", "supply_airflow"):
        for m in HVAC.run(name, {})["missing"]:
            if m.get("reference"):
                engine.setdefault((name, m["input"]), m["reference"])
    table = HVAC.offer_table
    return {
        "design_weather": engine.get(("monthly_load", "design_weather")),
        "ground_reflectance": engine.get(("monthly_load", "ground_reflectance")),
        "heating_outdoor_dry_bulb_c": engine.get(("heating_load", "outdoor_dry_bulb_c")),
        "altitude_m": engine.get(("supply_airflow", "altitude_m")),
        "outside_surface_coefficient_w_m2k": table("sol_air"),
        DOOR_KEY: table("sol_air"),
        "people_per_m2": table("ventilation_rates"),
        "sensible_w_each": table("people_heat_gain"),
        "latent_w_each": table("people_heat_gain"),
        "equipment_w_per_m2": table("equipment_density"),
        "outdoor_air_ls_per_person": table("ventilation_rates"),
        "outdoor_air_ls_per_m2": table("ventilation_rates"),
    }


def _absent(d, key):
    return _value((d or {}).get(key)) is None


def _placed(t):
    return [s for s in t.spaces if s.get("placed") and s.get("area_m2")]


def needs(t, project, profiles):
    """What is still to be asked - project inputs first, then what is beyond the faces Revit
    could not see past, then each profile a placed Space uses."""
    offers = _offers()
    said = answers(project, strict=False)
    out = []
    for k in PROJECT_KEYS:
        if k == "design_weather" and not _absent(project, "months"):
            continue
        if _absent(project, k):
            unit, why = PROJECT_ASK[k][:2]
            out.append({"input": k, "unit": unit, "why": why, "offer": offers.get(k),
                        "for": "project"})
    if _absent(project, DOOR_KEY) and any(_doors_without_absorptance(t, s, said)
                                          for s in _placed(t)):
        unit, why = PROJECT_ASK[DOOR_KEY][:2]
        out.append({"input": DOOR_KEY, "unit": unit, "why": why, "offer": offers.get(DOOR_KEY),
                    "for": "project"})
    given = _given_answers(project)
    for element, u in sorted(TAKEOFF.unknowns(t, said).items()):
        where = {"top": "above", "bottom": "below"}.get(u["side"], "beyond")
        why = ("Revit found nothing %s %s, which bounds %s - what is there?"
               % (where, u["name"], ", ".join(u["spaces"])))
        if given.get(element) not in (None, ""):
            why += " (the answer %r is not one of the four)" % (given[element],)
        out.append({"input": "beyond:%s" % element,
                    "unit": "outside, unconditioned, conditioned or ground",
                    "why": why, "offer": None, "for": "project"})
    keys = []
    for s in _placed(t):
        key = profile_key(s)
        if key not in keys:
            keys.append(key)
    for key in keys:
        profile = (profiles or {}).get(key) or {}
        for k, (unit, why, _low, _high) in PROFILE_KEYS.items():
            if _absent(profile, k):
                out.append({"input": k, "unit": unit, "why": why, "offer": offers.get(k),
                            "for": key})
    return out


def _engine_why(answer):
    why = list(answer.get("refused") or [])
    why += ["missing: %s" % m["input"] for m in answer.get("missing") or []]
    return why or ["the engine answered %s" % answer.get("status")]


def _km(lat1, lon1, lat2, lon2):
    """Great-circle distance in km between two points on the earth."""
    p1, p2 = math.radians(lat1), math.radians(lat2)
    dp, dl = p2 - p1, math.radians(lon2 - lon1)
    h = math.sin(dp / 2) ** 2 + math.cos(p1) * math.cos(p2) * math.sin(dl / 2) ** 2
    return 2 * 6371.0 * math.asin(math.sqrt(min(1.0, h)))


def site_checks(t, project):
    """The site the sun is worked out for, said every run - and a FAIL when it is not the
    place the design weather was measured."""
    site = t.site or {}
    lat, lon = site.get("latitude_deg"), site.get("longitude_deg")
    if lat is None or lon is None:
        return []
    named = site.get("named") or "the model's site"
    found = [{"level": "INFO", "space": None,
              "text": "the sun is worked out for %s, at %.3f, %.3f, UTC%+g, True North turned "
                      "%s degrees from project north - from the model (Manage > Location)"
                      % (named, lat, lon, site.get("utc_offset_h") or 0,
                         ("%.1f" % site["project_to_true_north_deg"])
                         if site.get("project_to_true_north_deg") is not None else "-")}]
    station = STATIONS.get(str(_value((project or {}).get("design_weather"))))
    if station and _absent(project, "months"):
        away = _km(float(lat), float(lon), station[1], station[2])
        if away > SITE_KM:
            found.append({"level": "FAIL", "space": None,
                          "text": "the model's site, %s, is %.0f km from %s, where the design "
                                  "weather was measured - the sun would be another city's. Set "
                                  "the model's location (Manage > Location), or give the site's "
                                  "own weather as months" % (named, away, station[0])})
    return found


def _block(spaces):
    hours, coil = {}, {}
    peaks = heating = area = done_area = supply = outdoor = 0.0
    calculated = 0
    for s in spaces:
        area += float(s["area_m2"] or 0.0)
        if s["status"] != "ok":
            continue
        calculated += 1
        done_area += float(s["area_m2"] or 0.0)
        supply += float(s.get("supply_ls") or 0.0)
        outdoor += float(s.get("outdoor_air_ls") or 0.0)
        peaks += s["cooling"]["peak"]["total_w"]
        heating += s["heating"]["loss_w"]
        for h in s["cooling"]["hours"]:
            k = (h["month"], h["hour"])
            hours[k] = hours.get(k, 0.0) + h["total_w"]
            coil[k] = coil.get(k, 0.0) + (h["coil_w"] if h.get("coil_w") is not None
                                          else h["total_w"])

    def top(series):
        if not series:
            return None, None, 0.0
        (month, hour), value = max(series.items(), key=lambda kv: kv[1])
        return month, hour, value

    month, hour, block = top(hours)
    c_month, c_hour, c_block = top(coil)

    def when(m, h):
        return ("%s %02d:00" % (_MONTHS[m - 1], h)) if m else None
    return {"area_m2": area, "sum_of_peaks_w": peaks, "block_w": block, "block_month": month,
            "block_hour": hour, "heating_w": heating, "block_tr": block / HVAC.W_PER_TR,
            "block_when": when(month, hour),
            "coil_block_w": c_block, "coil_block_tr": c_block / HVAC.W_PER_TR,
            "coil_block_month": c_month, "coil_block_hour": c_hour,
            "coil_block_when": when(c_month, c_hour),
            # The page's summary row (2026-10-04): the air the calculated Spaces
            # need, and the load per square metre of the floor that WAS
            # calculated - a refused Space's floor would only dilute it.
            "spaces": len(spaces), "calculated": calculated, "calculated_area_m2": done_area,
            "supply_ls": supply, "outdoor_air_ls": outdoor,
            "block_w_per_m2": block / done_area if done_area else None}


def run(t, project, profiles, overrides=None, recorded=None):
    """Every Space through the room engine, then zones and the building. See docs/44 s5."""
    t = TAKEOFF.read(t)
    # A table of answers is kept as the page's own keys, one per element (5b-332).
    project = flat(project)
    profiles = profiles or {}
    overrides = {str(k): v for k, v in (overrides or {}).items()}
    # A project value that cannot be a design figure, or an answer that is not
    # one of the four, refuses every Space - never a crash, never a guess.
    try:
        said = answers(project)
        _checked_project(project)
        building_refusal = None
    except _Refused as why:
        said = answers(project, strict=False)
        building_refusal = str(why)
    qa = TAKEOFF.qa(t, said) + site_checks(t, project)
    order = {"FAIL": 0, "WARN": 1, "INFO": 2}
    qa.sort(key=lambda f: order.get(f["level"], 3))
    failed = {}
    for f in qa:
        if f["level"] == "FAIL":
            failed.setdefault(f["space"], []).append(f["text"])
    site_fail = failed.get(None, [])
    notes = [HVAC.NOT_HAP, HVAC.STEADY_HOURS, OA_NOTE]
    rows = []
    for space in t.spaces:
        key = profile_key(space)
        row = {"id": space.get("id"), "unique_id": space.get("unique_id"),
               "number": space.get("number"), "name": space.get("name"),
               "zone": space.get("zone"), "profile": key, "status": "ok", "why": [],
               "area_m2": space.get("area_m2"), "cooling": None, "heating": None,
               "supply_ls": None, "outdoor_air_ls": None, "checks": [],
               "terminals": list(space.get("terminals") or [])}
        rows.append(row)
        if not space.get("placed") or not space.get("area_m2"):
            row["status"], row["why"] = "left out", failed.get(space.get("id"), [])
            continue
        if building_refusal or space.get("id") in failed or site_fail:
            row["status"] = "refused"
            row["why"] = ([building_refusal] if building_refusal else []) + \
                failed.get(space.get("id"), []) + site_fail
            continue
        own = overrides.get(str(space.get("id")), {})
        profile = dict(profiles.get(key) or {}, **dict((k, v) for k, v in own.items()
                                                       if k in PROFILE_KEYS))
        space_project = dict(project, **dict((k, v) for k, v in own.items() if k in SPACE_KEYS))
        missing = [k for k in PROJECT_KEYS if _absent(space_project, k)
                   and not (k == "design_weather" and not _absent(project, "months"))]
        if _absent(project, DOOR_KEY) and _doors_without_absorptance(t, space, said):
            missing.append(DOOR_KEY)
        missing += ["beyond:%s" % f["element"] for f in space.get("faces") or []
                    if f.get("element") is not None and TAKEOFF.role(f, said) == "unknown"]
        missing += [k for k in PROFILE_KEYS if _absent(profile, k)]
        if missing:
            row["status"], row["why"] = "missing", ["missing: %s" % k for k in missing]
            continue
        try:
            cool_in = monthly_inputs(t, space, space_project, profile)
            heat_in = heating_inputs(t, space, space_project, profile)
            q = _checked_project(space_project)
        except _Refused as why:
            row["status"], row["why"] = "refused", [str(why)]
            continue
        cool = HVAC.run("monthly_load", cool_in, recorded)
        heat = HVAC.run("heating_load", heat_in, recorded)
        for answer in (cool, heat):
            if answer["status"] != "ok":
                row["status"], row["why"] = answer["status"], _engine_why(answer)
                break
        if row["status"] != "ok":
            continue
        peak = cool["data"]["peak"]
        air = HVAC.run("supply_airflow", {
            "sensible_load_w": max(peak["sensible_w"], 0.0),
            "latent_load_w": max(peak["latent_w"], 0.0),
            "supply_dry_bulb_c": q["supply_dry_bulb_c"],
            "room_dry_bulb_c": q["room_dry_bulb_c"],
            "room_rh_pct": q["room_rh_pct"],
            "altitude_m": q["altitude_m"],
            "floor_area_m2": float(space["area_m2"]),
            "room_height_m": space.get("height_m")}, recorded)
        if air["status"] != "ok":
            row["status"], row["why"] = air["status"], _engine_why(air)
            continue
        row["cooling"] = {"peak": peak, "components": cool["data"]["components"],
                          "hours": cool["data"]["hours"]}
        row["heating"] = {"loss_w": heat["data"]["loss_w"],
                          "components": heat["data"]["components"]}
        row["supply_ls"] = air["data"]["supply_ls"]
        row["outdoor_air_ls"] = cool_in.get("outdoor_air_ls", 0.0)
        # What the page shows beside each Space - worked out here, so the
        # Companion does no arithmetic of its own (mcp/companion README rule 4).
        area = float(space["area_m2"])
        row["shown"] = {"sensible_w": peak["sensible_w"], "latent_w": peak["latent_w"],
                        "total_w": peak["total_w"], "w_per_m2": peak["total_w"] / area,
                        "tr": peak["total_w"] / HVAC.W_PER_TR,
                        "heating_w": heat["data"]["loss_w"], "supply_ls": row["supply_ls"],
                        "outdoor_air_ls": row["outdoor_air_ls"],
                        "ach": (row["supply_ls"] * 3.6 / (area * space["height_m"])
                                if space.get("height_m") else None),
                        "peak": "%s %02d:00" % (_MONTHS[peak["month"] - 1], peak["hour"])}
        for line in TAKEOFF.surfaces(t, space, said)["assumed"]:
            if line not in notes:
                notes.append(line)
        for answer in (cool, heat, air):
            for line in answer.get("assumed") or []:
                if line not in notes:
                    notes.append(line)
            # The engine's own WARN and FAIL checks stay with the Space they
            # are about - a negative latent from dry outdoor air, say.
            for level, text in answer.get("checks") or []:
                if level in ("WARN", "FAIL") and (level, text) not in row["checks"]:
                    row["checks"].append((level, text))
    zones = []
    for name in sorted({r["zone"] or "(no zone)" for r in rows}):
        z = _block([r for r in rows if (r["zone"] or "(no zone)") == name])
        z["name"] = name
        zones.append(z)
    now = datetime.datetime.now()
    inputs = {"project": _labelled(project),
              "profiles": {k: _labelled(v) for k, v in profiles.items()}}
    if overrides:
        inputs["overrides"] = {k: _labelled(v) for k, v in overrides.items()}
    return {"format": FORMAT, "document": t.document,
            "when": now.strftime("%Y-%m-%dT%H:%M:%S"), "run_id": now.strftime("%Y%m%d-%H%M%S"),
            "qa": qa, "spaces": rows, "zones": zones, "building": _block(rows),
            "inputs": inputs, "units": dict(t.units), "notes": notes, "site": dict(t.site),
            "takeoff_fingerprint": fingerprint(t)}


# --- what the chat is told (never the rows - those are on the page) ---------

def questions_text(asked):
    """The questions, project first, each with the figure a standard offers - never applied."""
    lines = ["Nothing was calculated. Heron needs these first - put them to the modeller, "
             "never fill them in (D-33):"]
    for a in asked:
        where = "for the project" if a["for"] == "project" else "for '%s' Spaces" % a["for"]
        line = "  - %s (%s) %s: %s" % (a["input"], a["unit"], where, a["why"])
        if a.get("offer"):
            line += " - offered: %s" % a["offer"]
        lines.append(line)
    return "\n".join(lines)


def summary_text(result):
    """A few lines for the chat: counts, the rooms' block, the coil block, heating."""
    spaces = result.get("spaces") or []
    count = {}
    for s in spaces:
        count[s["status"]] = count.get(s["status"], 0) + 1
    b = result.get("building") or {}
    lines = ["Spaces calculated: %d; refused: %d; still missing inputs: %d; left out "
             "(not placed): %d." % (count.get("ok", 0), count.get("refused", 0),
                                   count.get("missing", 0), count.get("left out", 0))]
    if b.get("block_w"):
        lines.append("Rooms' block load: %.1f kW (%.1f TR) at %s 21, %02d:00 - the largest "
                     "hour-by-hour sum of the Spaces' own cooling loads." % (
                         b["block_w"] / 1000.0, b["block_w"] / HVAC.W_PER_TR,
                         _MONTHS[b["block_month"] - 1], b["block_hour"]))
        lines.append("With the outdoor air at the coil: %.1f kW (%.1f TR) at %s." % (
            b["coil_block_w"] / 1000.0, b["coil_block_w"] / HVAC.W_PER_TR,
            b["coil_block_when"] or "-"))
        lines.append("Sum of each Space's own peak: %.1f kW - what each Space's supply air is "
                     "worked out from." % (b["sum_of_peaks_w"] / 1000.0))
        lines.append("Building heating loss: %.1f kW." % (b["heating_w"] / 1000.0))
    # EVERY SPACE NOT CALCULATED IS NAMED, with every reason it has - never the
    # commonest three lines, which named one refused Space twice and left the
    # third unnamed (FRAGMENT-ISSUES 5b-333). Spaces with the same reasons share
    # a line; a reason is said once, without its Space's own name in front.
    groups = []
    for s in spaces:
        if s["status"] not in ("refused", "missing"):
            continue
        label = ("%s %s" % (s.get("number") or "", s.get("name") or "")).strip() or str(s["id"])
        own = "Space %s: " % label
        why = tuple(w[len(own):] if w.startswith(own) else w for w in s.get("why") or [])
        for g in groups:
            if g[0] == s["status"] and g[1] == why:
                g[2].append(label)
                break
        else:
            groups.append((s["status"], why, [label]))
    for status in ("refused", "missing"):
        for _status, why, labels in [g for g in groups if g[0] == status]:
            lines.append("%s - %s: %s" % ("Refused" if status == "refused" else "Waiting",
                                          ", ".join(labels), "; ".join(why) or "no reason given"))
    lines.append("The full table is in the Heron Companion.")
    lines.append(HVAC.NOT_HAP)
    lines.append(HVAC.DISCLAIMER)
    return "\n".join(lines)


# --- back into Revit (docs/44 s6, gate 2) -----------------------------------

# One W in IT Btu/h (1 Btu = 1055.05585262 J); one L/s in CFM (1 ft3 = 28.316846592 L)
# and in m3/h. Exact definitions, not design values.
POWER = {"W": (1.0, "%.0f W"), "kW": (0.001, "%.2f kW"), "Btu/h": (3.412141633, "%.0f Btu/h")}
AIRFLOW = {"L/s": (1.0, "%.1f L/s"), "CFM": (2.118880003, "%.0f CFM"),
           "m3/h": (3.6, "%.0f m3/h")}
FINALIZE_FIELDS = ("Design Cooling Load", "Design Heating Load", "Specified Supply Airflow")

#: The Spaces schedule Finalize makes (Ajmal, 2026-10-05) - named the way the
#: calculation is named, with every value Finalize writes beside the Space.
SCHEDULE_NAME = "HVAC Load Calculation - Spaces"
SCHEDULE_FIELDS = ("Number", "Name", "Level", "Area") + FINALIZE_FIELDS


def _number_in(text):
    """The number a value string shows as Revit prints it - "3,300 W", "238.0 L/s",
    "238,0 L/s", "3.300,5 W" - or None. Whichever of . and , comes last is the
    decimal point; one alone is a thousands mark only before exactly three digits."""
    if text is None:
        return None
    m = re.search(r"-?\d[\d.,]*", str(text))
    if not m:
        return None
    s = m.group(0).rstrip(".,")
    if "." in s and "," in s:
        point = "." if s.rfind(".") > s.rfind(",") else ","
        s = s.replace("," if point == "." else ".", "").replace(point, ".")
    elif s.count(",") + s.count(".") > 1:
        s = s.replace(",", "").replace(".", "")
    elif "," in s or "." in s:
        mark = "," if "," in s else "."
        s = s.replace(mark, "" if len(s.split(mark)[1]) == 3 else ".")
    try:
        return float(s)
    except ValueError:
        return None


def read_back(rows, fresh):
    """What Revit holds now beside what Finalize wrote, row by row:
    [{"id", "space", "parameter", "written", "reads", "ok"}].

    `fresh` is the take-off read AGAIN after the write: its `current` is each
    field as Revit prints it. No element is asked for by a typed id - the
    add-in refuses those, which is how the first real Finalize lost its
    read-back (Project2, 2026-10-05). Revit's own rounding is allowed: one in
    the unit shown, or half a percent. A value that cannot be read is no match.
    """
    t = TAKEOFF.read(fresh)
    spaces = {str(s.get("id")): s for s in t.spaces}
    out = []
    for sid, _uid, field, _was, new in rows:
        s = spaces.get(str(sid)) or {}
        reads = (s.get("current") or {}).get(field)
        wrote, holds = _number_in(new), _number_in(reads)
        ok = wrote is not None and holds is not None and \
            abs(wrote - holds) <= max(1.0, abs(wrote) * 0.005)
        name = ("%s %s" % (s.get("number") or "", s.get("name") or "")).strip()
        out.append({"id": str(sid), "space": name or str(sid), "parameter": field,
                    "written": new, "reads": reads, "ok": ok})
    return out


def schedule_made(names):
    """Whether the model already holds the Spaces schedule Finalize makes, among the names
    of its schedules. Revit refuses a second schedule of the same name - CREATE_SCHEDULE
    threw on the second Finalize of 2026-10-06 (FRAGMENT-ISSUES 5b-335) - so Finalize looks
    first. Matched as Revit matches view names, case and outer spaces aside."""
    want = SCHEDULE_NAME.strip().lower()
    return any(str(n or "").strip().lower() == want for n in names or [])


def _items(value):
    """(how many, the first ids shown) from what the add-in reports for a list -
    "2 item(s) [352700, 352701]", "0 item(s)" - or for a number, "3"."""
    m = re.match(r"^\s*(\d+)(?: item\(s\))?(?: \[(.*)\])?\s*$", str(value or "0"))
    if not m:
        return 0, []
    shown = [x.strip() for x in (m.group(2) or "").split(",") if x.strip() not in ("", "...")]
    return int(m.group(1)), shown


#: Why SET_AIR_TERMINAL_FLOW left a diffuser alone, by the name it reports it under.
NOT_WRITTEN = (
    ("alreadyThatFlow", "already held that flow, so there was nothing to change"),
    ("noFlowParameter", "have a family that ties the duct connector's flow to no parameter "
                        "(the connector's flow is Calculated or System, or the family has no "
                        "duct connector or two), so nothing can set it from outside - tie the "
                        "connector's Flow to a family parameter, then Finalize again"),
    ("refused", "were not kept by Revit, and each was put back as it was"),
    ("notATerminal", "are not air terminals"),
    ("rowsUnmatched", "were not among the air terminals Revit handed over - every one in the "
                      "model is handed over, so the model holds none with that id now; read the "
                      "loads again"),
    ("badRows", "lines of Heron's own file of flows could not be read, so nothing was written "
                "for them"),
)


def diffusers_said(provides, handed):
    """What Finalize says about the diffusers it handed SET_AIR_TERMINAL_FLOW:
    (how many were written, [the lines to say]).

    `provides` is that fragment's own answer and `handed` how many rows the file of flows
    held. Every diffuser not written is put down to the reason the fragment gave for it -
    never to a list of what might have happened: on 2026-10-06 none of three was handed
    over, and the page blamed their family (FRAGMENT-ISSUES 5b-335)."""
    p = provides or {}
    written = _items(p.get("changed"))[0]
    lines = ["Diffusers: %d of %d written with their Space's share of the supply air%s."
             % (written, handed, ", each read back through its connector" if written else "")]
    told = 0
    for key, why in NOT_WRITTEN:
        n, ids = _items(p.get(key))
        if n:
            told += n
            lines.append("Not written: %d %s%s." % (
                n, why, " (%s%s)" % (", ".join(ids), ", ..." if n > len(ids) else "")
                if ids else ""))
    if written + told < handed:
        lines.append("Not written: %d, and Revit's answer does not say why - check those "
                     "diffusers' Flow in Revit." % (handed - written - told))
    n, ids = _items(p.get("builtInDisagrees"))
    if n:
        lines.append("Written, but the Flow Revit shows in Properties still reads something else "
                     "on %d (%s) - look at them in Revit." % (n, ", ".join(ids) or "-"))
    return written, lines


def _unit(table, symbol, what):
    if symbol not in table:
        raise ValueError("the model shows %s in %r, which Heron does not convert to - nothing "
                         "is written; set the project's %s units to one of %s"
                         % (what, symbol, what, ", ".join(sorted(table))))
    return table[symbol]


# --- gate 1: the modeller confirms the take-off (docs/44 s6) ------------------

def fingerprint(t):
    """The take-off's own fingerprint - a confirmation holds for exactly this geometry.

    It leaves out what changes without the building changing: the values
    Finalize writes into the Spaces (`current`), the findings' wording and the
    file's name. So a Finalize does not unconfirm the take-off it wrote from,
    and Finalize can re-read the model and tell a moved wall from a written load.
    """
    t = TAKEOFF.read(t)
    raw = dict((k, v) for k, v in t.raw.items() if k not in ("findings", "document"))
    raw["spaces"] = [dict((k, v) for k, v in s.items() if k != "current") for s in t.spaces]
    text = json.dumps(raw, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(text.encode("utf-8")).hexdigest()[:16]


def answers_key(result):
    """A short key for what the run was told is beyond the faces Revit could not see past -
    part of what the modeller confirms with the take-off."""
    project = ((result or {}).get("inputs") or {}).get("project") or {}
    said = sorted(answers(project, strict=False).items())
    return hashlib.sha256(json.dumps(said).encode("utf-8")).hexdigest()[:12]


def confirm(result, t, by="the modeller, in the Heron Companion"):
    """Record that the modeller has checked this take-off - with when, which one, and the
    answers about what is beyond its faces."""
    result["geometry_confirmed"] = {
        "at": datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S"), "by": by,
        "takeoff": fingerprint(t), "answers": answers_key(result)}
    return result


def confirmed(result, t):
    """True only when the run carries a confirmation of THIS take-off with THESE answers."""
    got = (result or {}).get("geometry_confirmed") or {}
    return (bool(got) and got.get("takeoff") == fingerprint(t)
            and got.get("answers") == answers_key(result))


NOT_CONFIRMED = ("the take-off has not been confirmed - look at the 3D view and the checks on "
                 "the model in the Heron Companion, then press 'The take-off is right'")


def finalize_rows(t, result):
    """SET_PARAMETER_VALUES_BY_ID rows [id, unique_id, parameter, was, new] for every ok Space.

    `new` is in the project's own display units, read from the take-off - a
    unit Heron cannot convert to refuses the whole Finalize (ValueError). So
    does a take-off the modeller has not confirmed (gate 1, docs/44 s6): the
    loads of geometry nobody looked at are never written into Revit.
    """
    t = TAKEOFF.read(t)
    if not confirmed(result, t):
        raise ValueError(NOT_CONFIRMED)
    power = _unit(POWER, (t.units or {}).get("power"), "power")
    air = _unit(AIRFLOW, (t.units or {}).get("airflow"), "airflow")
    current = {str(s.get("id")): s.get("current") or {} for s in t.spaces}
    rows = []
    for s in result.get("spaces") or []:
        if s.get("status") != "ok":
            continue
        was = current.get(str(s["id"]), {})
        values = (power[1] % (s["cooling"]["peak"]["total_w"] * power[0]),
                  power[1] % (s["heating"]["loss_w"] * power[0]),
                  air[1] % (s["supply_ls"] * air[0]))
        for field, new in zip(FINALIZE_FIELDS, values):
            rows.append([str(s["id"]), s.get("unique_id") or "", field,
                         was.get(field, ""), new])
    return rows


# --- kept with the project --------------------------------------------------

def _folder(project_key):
    import heron_designbasis
    scope = heron_designbasis._scope()
    base = scope.knowledge_dir()
    if base is None:
        raise ValueError("No %APPDATA% and no HERON_KNOWLEDGE, so there is nowhere to keep "
                         "a run. Set HERON_KNOWLEDGE to a folder.")
    if not project_key:
        raise ValueError("No project key, so there is no folder to keep the run in - ask "
                         "which project this is (D-33).")
    return os.path.join(base, "projects", scope._safe_key(project_key) + ".loads")


def _kept(result):
    """The run as it is kept: everything but each Space's hour-by-hour rows, which the
    zone and building totals have already been worked out from."""
    out = dict(result)
    out["spaces"] = []
    for s in result.get("spaces") or []:
        s = dict(s)
        if s.get("cooling"):
            s["cooling"] = dict((k, v) for k, v in s["cooling"].items() if k != "hours")
        out["spaces"].append(s)
    return out


def save(project_key, result, replace=False, takeoff=None):
    """Keep one run; the path it was written to. A second run in the same second is not lost.

    `replace` writes over the kept copy of this same run - how a confirmation
    made after the run was kept is recorded with it. With `takeoff`, the
    take-off the run was worked out from is kept beside it, once per
    fingerprint, so an earlier run can be read again with its own geometry
    (docs/44 s5.4).
    """
    folder = _folder(project_key)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    if takeoff is not None:
        t = TAKEOFF.read(takeoff)
        result["takeoff_fingerprint"] = fingerprint(t)
        kept = os.path.join(folder, "takeoff-%s.json" % result["takeoff_fingerprint"])
        if not os.path.exists(kept):
            with io.open(kept, "w", encoding="utf-8") as fh:
                fh.write(json.dumps(t.raw))
    base = run_id = result["run_id"]
    path = os.path.join(folder, run_id + ".json")
    n = 1
    while os.path.exists(path) and not replace:
        n += 1
        run_id = "%s-%d" % (base, n)
        path = os.path.join(folder, run_id + ".json")
    result["run_id"] = run_id
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(json.dumps(_kept(result), indent=1))
    return path


def load_takeoff(project_key, takeoff_fingerprint):
    """The take-off kept with a run, by its fingerprint - or None."""
    if not re.match(r"^[0-9a-f]{16}$", str(takeoff_fingerprint or "")):
        return None
    try:
        path = os.path.join(_folder(project_key), "takeoff-%s.json" % takeoff_fingerprint)
    except ValueError:
        return None
    if not os.path.isfile(path):
        return None
    with io.open(path, encoding="utf-8") as fh:
        return TAKEOFF.read(fh.read())


#: The note, in a project's runs folder, of where the modeller last chose to
#: put its load reports. Never a run.
REPORT_FOLDER_NOTE = "report-folder.json"


def report_folder(project_key):
    """The folder this project's load reports go to, as the modeller last chose it -
    or None when none was chosen. Whether it is still there is the caller's to check."""
    try:
        path = os.path.join(_folder(project_key), REPORT_FOLDER_NOTE)
    except ValueError:
        return None
    try:
        with io.open(path, encoding="utf-8") as fh:
            got = json.loads(fh.read()).get("folder")
    except (OSError, ValueError, AttributeError):
        return None
    return got if isinstance(got, str) and got else None


def set_report_folder(project_key, folder):
    """Keep the folder the modeller chose for this project's load reports.

    Only a folder that is already on this PC, named in full: a typed name that
    is not there is a typing slip, and making it would leave reports in a
    folder nobody meant (Ajmal, 2026-10-04: "I can give the location").
    """
    if not isinstance(folder, str) or not os.path.isabs(folder) or not os.path.isdir(folder):
        raise ValueError("%r is not a folder on this PC" % (folder,))
    base = _folder(project_key)
    if not os.path.isdir(base):
        os.makedirs(base)
    with io.open(os.path.join(base, REPORT_FOLDER_NOTE), "w", encoding="utf-8") as fh:
        fh.write(json.dumps({"folder": folder}))
    return folder


def runs(project_key):
    """This project's runs, newest first: [{"run_id", "when", "block_w"}]."""
    try:
        folder = _folder(project_key)
    except ValueError:
        return []
    if not os.path.isdir(folder):
        return []
    out = []
    for name in os.listdir(folder):
        if not name.endswith(".json") or name.startswith("takeoff-") \
                or name == REPORT_FOLDER_NOTE:
            continue
        try:
            with io.open(os.path.join(folder, name), encoding="utf-8") as fh:
                d = json.loads(fh.read())
        except (OSError, ValueError):
            continue
        out.append({"run_id": d.get("run_id"), "when": d.get("when"),
                    "block_w": (d.get("building") or {}).get("block_w")})
    out.sort(key=lambda r: (r["when"] or "", r["run_id"] or ""), reverse=True)
    return out


def load(project_key, run_id=None):
    """One kept run - the newest when no run_id is named - or None."""
    try:
        folder = _folder(project_key)
    except ValueError:
        return None
    if not os.path.isdir(folder):
        return None
    if run_id is None:
        kept = runs(project_key)
        if not kept:
            return None
        run_id = kept[0]["run_id"]
    path = os.path.join(folder, "%s.json" % run_id)
    if os.path.basename(path) != "%s.json" % run_id or not os.path.isfile(path):
        return None
    with io.open(path, encoding="utf-8") as fh:
        return json.loads(fh.read())
