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
import io
import json
import os
import re

import heron_hvac as HVAC
import heron_takeoff as TAKEOFF

FORMAT = 1
PROJECT_KEYS = ("design_weather", "room_dry_bulb_c", "room_rh_pct", "supply_dry_bulb_c",
                "ground_reflectance", "outside_surface_coefficient_w_m2k",
                "heating_outdoor_dry_bulb_c", "heating_room_dry_bulb_c",
                "unconditioned_temp_c", "ground_temp_c", "altitude_m")
# name: (unit, why) - for the questions
PROJECT_ASK = {
    "design_weather": ("one of: doha-0.4 (or months)", "the site's design weather, month by "
                       "month"),
    "room_dry_bulb_c": ("C", "room design dry bulb for cooling"),
    "room_rh_pct": ("%", "room design relative humidity for cooling"),
    "supply_dry_bulb_c": ("C", "supply air temperature - for each Space's supply airflow"),
    "ground_reflectance": ("0-1", "the ground's solar reflectance in front of the walls"),
    "outside_surface_coefficient_w_m2k": ("W/m2.K", "outside surface coefficient ho - "
                                          "with each type's absorptance it gives a/ho"),
    "heating_outdoor_dry_bulb_c": ("C", "outdoor design dry bulb for heating"),
    "heating_room_dry_bulb_c": ("C", "room design dry bulb for heating"),
    "unconditioned_temp_c": ("C", "temperature beyond a wall to an unconditioned space"),
    "ground_temp_c": ("C", "temperature beneath a floor with no Space below - for heating"),
    "altitude_m": ("m", "site altitude, for air density"),
}
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


def _checked(profile):
    out = {}
    for k, (unit, why, low, high) in PROFILE_KEYS.items():
        raw = _value(profile.get(k))
        try:
            if isinstance(raw, bool):
                raise TypeError(k)
            v = float(raw)
        except (TypeError, ValueError):
            raise _Refused("%s must be a number in %s - got %r" % (k, unit, raw))
        if not low <= v <= high:
            raise _Refused("%s must be between %s and %s %s - got %s" % (k, low, high, unit, raw))
        out[k] = v
    return out


def _opaque(recs, ho):
    out = []
    for r in recs:
        r = dict(r)
        a = r.pop("absorptance", None)
        if a is None:
            raise _Refused("%s has no solar absorptance in the model - set its type's "
                           "thermal properties" % r["name"])
        r["absorptance_over_ho"] = float(a) / ho
        out.append(r)
    return out


def _surfaces(t, space):
    s = TAKEOFF.surfaces(t, space)
    if s["refused"]:
        raise _Refused("; ".join(s["refused"]))
    return s


def monthly_inputs(t, space, project, profile):
    """The engine's monthly_load inputs for one Space - exposed so a test can call it by hand."""
    s = _surfaces(t, space)
    p = _checked(profile)
    area = float(space["area_m2"])
    people = int(round(p["people_per_m2"] * area))
    ho = float(_value(project["outside_surface_coefficient_w_m2k"]))
    if ho <= 0:
        raise _Refused("outside_surface_coefficient_w_m2k must be above 0 - got %s" % ho)
    i = {"floor_area_m2": area, "room_height_m": space.get("height_m"),
         "latitude_deg": t.site.get("latitude_deg"), "longitude_deg": t.site.get("longitude_deg"),
         "utc_offset_h": t.site.get("utc_offset_h"),
         "ground_reflectance": _value(project["ground_reflectance"]),
         "room_dry_bulb_c": _value(project["room_dry_bulb_c"]),
         "room_rh_pct": _value(project["room_rh_pct"]),
         "altitude_m": _value(project["altitude_m"]),
         "walls": _opaque(s["walls"], ho), "roofs": _opaque(s["roofs"], ho),
         "windows": s["windows"], "skylights": s["skylights"],
         "partitions": [dict(x, adjacent_temp_c=_value(project["unconditioned_temp_c"]))
                        for x in s["partitions"]],
         "people": {"count": people, "sensible_w_each": p["sensible_w_each"],
                    "latent_w_each": p["latent_w_each"]},
         "lighting": {"w_per_m2": p["lighting_w_per_m2"]},
         "equipment": [{"w_per_m2": p["equipment_w_per_m2"]}],
         "infiltration": {"ach": p["infiltration_ach"]},
         "outdoor_air_ls": people * p["outdoor_air_ls_per_person"]
                           + area * p["outdoor_air_ls_per_m2"]}
    if _value(project.get("months")) is not None:
        i["months"] = _value(project["months"])
    else:
        i["design_weather"] = _value(project["design_weather"])
    return {k: v for k, v in i.items() if v not in (None, [])}


def heating_inputs(t, space, project, profile):
    """The engine's heating_load inputs for one Space."""
    s = _surfaces(t, space)
    p = _checked(profile)

    def given(key):
        return _value(project[key])

    surf = [{"name": r["name"], "area_m2": r["area_m2"], "u_w_m2k": r["u_w_m2k"]}
            for r in s["walls"] + s["roofs"] + s["windows"] + s["skylights"]]
    surf += [dict(r, adjacent_temp_c=given("unconditioned_temp_c")) for r in s["partitions"]]
    surf += [dict(r, adjacent_temp_c=given("ground_temp_c")) for r in s["floors"]]
    i = {"floor_area_m2": float(space["area_m2"]), "room_height_m": space.get("height_m"),
         "surfaces": surf, "room_dry_bulb_c": given("heating_room_dry_bulb_c"),
         "outdoor_dry_bulb_c": given("heating_outdoor_dry_bulb_c"),
         "infiltration": {"ach": p["infiltration_ach"]}, "altitude_m": given("altitude_m")}
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
        "people_per_m2": table("ventilation_rates"),
        "sensible_w_each": table("people_heat_gain"),
        "latent_w_each": table("people_heat_gain"),
        "equipment_w_per_m2": table("equipment_density"),
        "outdoor_air_ls_per_person": table("ventilation_rates"),
        "outdoor_air_ls_per_m2": table("ventilation_rates"),
    }


def _absent(d, key):
    return _value((d or {}).get(key)) is None


def needs(t, project, profiles):
    """What is still to be asked - project inputs first, then each profile a placed Space uses."""
    offers = _offers()
    out = []
    for k in PROJECT_KEYS:
        if k == "design_weather" and not _absent(project, "months"):
            continue
        if _absent(project, k):
            unit, why = PROJECT_ASK[k]
            out.append({"input": k, "unit": unit, "why": why, "offer": offers.get(k),
                        "for": "project"})
    keys = []
    for s in t.spaces:
        if s.get("placed") and s.get("area_m2"):
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


def _block(spaces):
    hours = {}
    peaks = heating = area = 0.0
    for s in spaces:
        area += float(s["area_m2"] or 0.0)
        if s["status"] != "ok":
            continue
        peaks += s["cooling"]["peak"]["total_w"]
        heating += s["heating"]["loss_w"]
        for h in s["cooling"]["hours"]:
            k = (h["month"], h["hour"])
            hours[k] = hours.get(k, 0.0) + h["total_w"]
    if hours:
        (month, hour), block = max(hours.items(), key=lambda kv: kv[1])
    else:
        month = hour = None
        block = 0.0
    return {"area_m2": area, "sum_of_peaks_w": peaks, "block_w": block, "block_month": month,
            "block_hour": hour, "heating_w": heating, "block_tr": block / HVAC.W_PER_TR,
            "block_when": ("%s %02d:00" % (_MONTHS[month - 1], hour)) if month else None}


def run(t, project, profiles, overrides=None, recorded=None):
    """Every Space through the room engine, then zones and the building. See docs/44 s5."""
    t = TAKEOFF.read(t)
    project = project or {}
    profiles = profiles or {}
    overrides = {str(k): v for k, v in (overrides or {}).items()}
    qa = TAKEOFF.qa(t)
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
        if space.get("id") in failed or site_fail:
            row["status"] = "refused"
            row["why"] = failed.get(space.get("id"), []) + site_fail
            continue
        profile = dict(profiles.get(key) or {}, **overrides.get(str(space.get("id")), {}))
        missing = [k for k in PROJECT_KEYS if _absent(project, k)
                   and not (k == "design_weather" and not _absent(project, "months"))]
        missing += [k for k in PROFILE_KEYS if _absent(profile, k)]
        if missing:
            row["status"], row["why"] = "missing", ["missing: %s" % k for k in missing]
            continue
        try:
            cool_in = monthly_inputs(t, space, project, profile)
            heat_in = heating_inputs(t, space, project, profile)
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
            "supply_dry_bulb_c": _value(project["supply_dry_bulb_c"]),
            "room_dry_bulb_c": _value(project["room_dry_bulb_c"]),
            "room_rh_pct": _value(project["room_rh_pct"]),
            "altitude_m": _value(project["altitude_m"]),
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
        row["outdoor_air_ls"] = cool_in.get("outdoor_air_ls")
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
            "inputs": inputs, "units": dict(t.units), "notes": notes}


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
    """A few lines for the chat: counts, the block load, the sum of peaks, heating."""
    spaces = result.get("spaces") or []
    count = {}
    for s in spaces:
        count[s["status"]] = count.get(s["status"], 0) + 1
    b = result.get("building") or {}
    lines = ["Spaces calculated: %d; refused: %d; still missing inputs: %d; left out "
             "(not placed): %d." % (count.get("ok", 0), count.get("refused", 0),
                                   count.get("missing", 0), count.get("left out", 0))]
    if b.get("block_w"):
        lines.append("Building cooling, block load: %.1f kW (%.1f TR) at %s 21, %02d:00 - "
                     "what the plant is sized to." % (
                         b["block_w"] / 1000.0, b["block_w"] / HVAC.W_PER_TR,
                         _MONTHS[b["block_month"] - 1], b["block_hour"]))
        lines.append("Sum of each Space's own peak: %.1f kW - what the terminals are sized "
                     "to." % (b["sum_of_peaks_w"] / 1000.0))
        lines.append("Building heating loss: %.1f kW." % (b["heating_w"] / 1000.0))
    reasons = {}
    for s in spaces:
        if s["status"] in ("refused", "missing"):
            for why in s.get("why") or []:
                reasons[why] = reasons.get(why, 0) + 1
    for why, n in sorted(reasons.items(), key=lambda kv: -kv[1])[:3]:
        lines.append("Refused (%d Space(s)): %s" % (n, why))
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


def _unit(table, symbol, what):
    if symbol not in table:
        raise ValueError("the model shows %s in %r, which Heron does not convert to - nothing "
                         "is written; set the project's %s units to one of %s"
                         % (what, symbol, what, ", ".join(sorted(table))))
    return table[symbol]


def finalize_rows(t, result):
    """SET_PARAMETER_VALUES_BY_ID rows [id, unique_id, parameter, was, new] for every ok Space.

    `new` is in the project's own display units, read from the take-off - a
    unit Heron cannot convert to refuses the whole Finalize (ValueError).
    """
    t = TAKEOFF.read(t)
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


def save(project_key, result):
    """Keep one run; the path it was written to. A second run in the same second is not lost."""
    folder = _folder(project_key)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    base = run_id = result["run_id"]
    path = os.path.join(folder, run_id + ".json")
    n = 1
    while os.path.exists(path):
        n += 1
        run_id = "%s-%d" % (base, n)
        path = os.path.join(folder, run_id + ".json")
    result["run_id"] = run_id
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(json.dumps(result, indent=1))
    return path


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
        if not name.endswith(".json"):
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
