# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Air systems from a load run - docs/44 section 13: what the AHU or packaged
unit serving a group of Spaces must do, beside the Spaces' own loads.

The building runner (heron_building_loads) gives every Space its peak. This
step groups the calculated Spaces into air systems - one per Revit zone, or
the modeller's own list - and works out, for each one, its cooling coil (total
and sensible load, sensible heat ratio, the air entering and leaving it, its
apparatus dew point and bypass factor), its supply and outdoor air, and the
outdoor air Space by Space. Every figure comes out of heron_hvac's own
calculations - psychrometrics, air_mixing, coil_load, ventilation, fan_power -
so this file adds no physics (one fact, one home).

THE COIL HAS ITS OWN OUTDOOR DESIGN POINT, AND IT IS ASKED (D-33)
-----------------------------------------------------------------
The room loads run on each month's peak dry-bulb day, and its mean coincident
wet bulb is DRY: Doha's 45.7 C comes with air drier than the room, so the
outdoor air's latent load comes out negative and the engine already warns that
coil sizing needs the dehumidification design condition. A coil is therefore
worked out at its OWN outdoor dry bulb and wet bulb - a project's 46 / 29 C,
say - which the modeller gives. Heron holds no dehumidification design data,
so it offers no figure: it asks. So are the kind of ventilation system and its
zone air distribution effectiveness, with 62.1's table offered. A value the
Spaces of one system all share - their supply temperature, their room design
condition - is taken from them; where they differ it is asked for the system.

WHAT IT DOES NOT DO - and every page says so (NOT_DONE)
-------------------------------------------------------
No hourly storage or time lag (a peak estimate, not an hourly simulation like
HAP); a constant-volume supply, the sum of the Spaces' own; no fan heat unless
the fan is given, and no duct heat gain or leakage; return air at the rooms'
design condition, with no plenum or return gain; no heating coil; no safety
factor. Nothing here selects a unit.

Standard library only.
"""

import collections

import heron_hvac as HVAC
import heron_psychro as PSY

#: Revit puts every Space nobody has zoned into a zone of this name, so those
#: Spaces are on no air system until the modeller says which (D-33).
DEFAULT_ZONE = "default"

# name: (unit, why, low, high) - asked once for the project
COIL_KEYS = collections.OrderedDict([
    ("coil_outdoor_dry_bulb_c", ("C", "outdoor dry bulb the cooling coils are sized at - the "
                                 "site's coil design point, not the peak dry-bulb day the room "
                                 "loads run on. Heron holds no figure for it: take the "
                                 "project's own, as its design basis or the station's climatic "
                                 "design conditions give it", 0, 60)),
    ("coil_outdoor_wet_bulb_c", ("C", "outdoor wet bulb at that design point - the coil's "
                                 "latent load stands on it", 0, 40)),
])

EV_METHODS = ("simplified", "appendix-a")
FAN_POSITIONS = ("draw-through", "blow-through")
FAN_KEYS = ("fan_total_pressure_pa", "fan_efficiency_pct", "motor_efficiency_pct",
            "drive_efficiency_pct", "fan_position", "motor_in_airstream")
# A system's numbers: (unit, why, low, high).
NUMBERS = {
    "ez": ("factor", "zone air distribution effectiveness Ez of the Spaces this unit "
                     "supplies - how its air is supplied and returned", 0.5, 1.5),
    "system_population": ("people", "the most people in all its Spaces at once (Ps), for "
                          "occupant diversity - D is 1 when it is not given", 0, 1e6),
    "supply_dry_bulb_c": ("C", "this unit's design supply air temperature - asked because its "
                          "Spaces' own supply temperatures differ", 2, 25),
    "return_dry_bulb_c": ("C", "its return air dry bulb - asked because its Spaces' room design "
                          "conditions differ", 10, 35),
    "return_rh_pct": ("%", "its return air relative humidity - asked because its Spaces' room "
                      "design conditions differ", 10, 90),
    "supply_ls": ("L/s", "the unit's supply airflow, when it is not the sum of its Spaces' own",
                  1, 1e6),
    "fan_total_pressure_pa": ("Pa", "the supply fan's total pressure", 1, 10000),
    "fan_efficiency_pct": ("%", "fan total efficiency at the duty point", 5, 95),
    "motor_efficiency_pct": ("%", "motor efficiency at that load", 30, 99),
    "drive_efficiency_pct": ("%", "drive efficiency - 100 for a direct drive", 50, 100),
}
# A system's words: (the words it may be, why).
WORDS = {
    "ventilation_system": (HVAC.SYSTEMS, "the kind of system, as ASHRAE 62.1 counts it - one "
                           "ventilation zone, 100 % outdoor air, or several zones on one "
                           "recirculating unit"),
    "ev_method": (EV_METHODS, "how 62.1's system ventilation efficiency Ev is found on a "
                  "multiple-zone system"),
    "fan_position": (FAN_POSITIONS, "whether the supply fan draws air through the coil (its "
                     "heat after the coil) or blows through it (its heat before)"),
}
FLAGS = {"motor_in_airstream": "whether the fan's motor sits in the air stream - then its "
                               "losses heat the air as well"}
LISTS = ("spaces", "exhaust_ls")
KEYS = tuple(NUMBERS) + tuple(WORDS) + tuple(FLAGS) + LISTS

NOT_DONE = (
    "The supply air is constant volume: the sum of each Space's own supply airflow, each at "
    "its own peak sensible load. A VAV unit's smaller coincident airflow is not worked out.",
    "The coil is worked out at the outdoor design point given for it, with the rooms at their "
    "design condition - one condition, not hour by hour, and with no thermal storage.",
    "Return air is taken at the rooms' design condition: no heat from lights into a ceiling "
    "plenum, and no return duct or return fan gain.",
    "Fan heat is counted only when the fan is given; no fan static pressure is worked out "
    "here, and no supply duct heat gain or leakage is counted.",
    "No heating coil is sized, and no safety factor is applied.",
    "Outdoor air volumes are taken at the outdoor design state, as the engine's air_mixing "
    "takes each stream at its own.",
)


class _Refused(ValueError):
    pass


def _value(v):
    return v.get("value") if isinstance(v, dict) and "value" in v else v


def _absent(d, key):
    return _value((d or {}).get(key)) in (None, "", [], {})


def labelled(systems):
    """The systems' inputs as kept with the run: each value {"value", "source"}. A table -
    `exhaust_ls` - is wrapped whole, never read as a labelled value (FRAGMENT-ISSUES 5b-332
    lost a table that way)."""
    out = collections.OrderedDict()
    for name, given in (systems or {}).items():
        keep = collections.OrderedDict()
        for k, v in (given or {}).items():
            if _value(v) in (None, ""):
                continue
            if isinstance(v, dict) and "value" in v:
                keep[k] = dict(v)
                keep[k].setdefault("source", "instruction")
            else:
                keep[k] = {"value": v, "source": "instruction"}
        out[str(name)] = keep
    return out


def _source(v):
    return v.get("source", "instruction") if isinstance(v, dict) and "value" in v \
        else "instruction"


def _number(key, raw, unit, low, high):
    try:
        if isinstance(raw, bool):
            raise TypeError(key)
        v = float(raw)
    except (TypeError, ValueError):
        raise _Refused("%s must be a number in %s - got %r" % (key, unit, raw))
    if v != v or not low <= v <= high:
        raise _Refused("%s must be between %s and %s %s - got %r" % (key, low, high, unit, raw))
    return v


def _word(key, raw, words):
    word = str(raw).strip().lower().replace("_", "-").replace(" ", "-")
    if word not in words:
        raise _Refused("%s must be one of %s - got %r" % (key, ", ".join(words), raw))
    return word


def _flag(key, raw):
    if isinstance(raw, bool):
        return raw
    word = str(raw).strip().lower()
    if word in ("true", "yes", "y", "1"):
        return True
    if word in ("false", "no", "n", "0"):
        return False
    raise _Refused("%s must be true or false - got %r" % (key, raw))


def _label(row):
    return ("%s %s" % (row.get("number") or "", row.get("name") or "")).strip() or \
        str(row.get("id"))


def _items(raw):
    if isinstance(raw, str):
        raw = [w for w in raw.replace(";", ",").split(",")]
    if not isinstance(raw, (list, tuple)):
        raise _Refused("spaces must be a list of Space numbers or element ids")
    return [str(w).strip() for w in raw if str(w).strip()]


def _find(rows, word):
    """The Space a modeller's word names - by its number, else by its element id."""
    word = str(word).strip()
    by_number = [r for r in rows if str(r.get("number") or "").strip() == word]
    if len(by_number) > 1:
        raise _Refused("two Spaces are numbered %s - name them by element id" % word)
    if by_number:
        return by_number[0]
    by_id = [r for r in rows if str(r.get("id")) == word]
    return by_id[0] if by_id else None


def groups(rows, systems):
    """The air systems and their Spaces: ([(name, grouped_by, [rows], [problems])], mode).

    Once any system lists its Spaces, the systems are the modeller's list, exactly.
    Otherwise there is one per Revit zone, named for it - a Space with no zone, or in
    Revit's Default zone, is on none. Spaces not placed are never on one.
    """
    placed = [r for r in rows if r.get("status") != "left out"]
    systems = systems or {}
    listed = [n for n in systems if not _absent(systems[n], "spaces")]
    if not listed:
        zones = collections.OrderedDict()
        for r in placed:
            zone = str(r.get("zone") or "").strip()
            if zone and zone.lower() != DEFAULT_ZONE:
                zones.setdefault(zone, []).append(r)
        return [(z, "Revit zone", zones[z], []) for z in sorted(zones)], "Revit zone"
    out, owner = [], collections.OrderedDict()
    for name in systems:
        problems, members = [], []
        if name not in listed:
            problems.append("lists no Spaces - once one air system lists its Spaces, every "
                            "one does")
        else:
            try:
                for word in _items(_value(systems[name]["spaces"])):
                    r = _find(placed, word)
                    if r is None:
                        problems.append("lists Space %r, which is no placed Space in this "
                                        "take-off" % word)
                    elif r not in members:
                        members.append(r)
                        owner.setdefault(str(r.get("id")), []).append(name)
            except _Refused as why:
                problems.append(str(why))
        out.append((name, "your list", members, problems))
    for sid, names in owner.items():
        if len(names) > 1:
            for name, _how, members, problems in out:
                if name in names:
                    label = next(_label(r) for r in members if str(r.get("id")) == sid)
                    problems.append("Space %s is listed on %s - one Space is on one air "
                                    "system" % (label, " and ".join(names)))
    return out, "your list"


def _ask(name, key, unit, why, offer=None):
    return {"input": "systems.%s.%s" % (name, key), "unit": unit, "why": why, "offer": offer,
            "for": "air system '%s'" % name}


def _shared(members, key):
    """The value every Space of the system has for `key`, or None where they differ."""
    got = set(round(float(m["air"][key]), 6) for m in members)
    return got.pop() if len(got) == 1 else None


def _checked(given):
    """A system's own inputs, each checked - or _Refused naming the one that is not right."""
    out = {}
    for k, v in (given or {}).items():
        raw = _value(v)
        if raw in (None, ""):
            continue
        if k in NUMBERS:
            unit, _why, low, high = NUMBERS[k]
            out[k] = _number(k, raw, unit, low, high)
        elif k in WORDS:
            out[k] = _word(k, raw, WORDS[k][0])
        elif k in FLAGS:
            out[k] = _flag(k, raw)
        elif k in LISTS:
            out[k] = raw
        else:
            raise _Refused("%s is not an air system input Heron reads - it reads %s"
                           % (k, ", ".join(sorted(KEYS))))
    return out


def _asks(name, given, members):
    """What is still to be asked for this system: [ask]."""
    out = []
    if "ventilation_system" not in given:
        out.append(_ask(name, "ventilation_system", "one of: " + ", ".join(HVAC.SYSTEMS),
                        WORDS["ventilation_system"][1]))
    if "ez" not in given:
        out.append(_ask(name, "ez", NUMBERS["ez"][0], NUMBERS["ez"][1],
                        HVAC.offer_table("zone_air_distribution")))
    if given.get("ventilation_system") == "multiple-zone" and "ev_method" not in given:
        out.append(_ask(name, "ev_method", "one of: " + ", ".join(EV_METHODS),
                        WORDS["ev_method"][1]))
    ok = [m for m in members if m.get("status") == "ok"]
    if ok and "supply_dry_bulb_c" not in given and _shared(ok, "supply_dry_bulb_c") is None:
        out.append(_ask(name, "supply_dry_bulb_c", "C", NUMBERS["supply_dry_bulb_c"][1]))
    if ok and (_shared(ok, "room_dry_bulb_c") is None or _shared(ok, "room_rh_pct") is None):
        for k in ("return_dry_bulb_c", "return_rh_pct"):
            if k not in given:
                out.append(_ask(name, k, NUMBERS[k][0], NUMBERS[k][1]))
    if any(k in given for k in FAN_KEYS):
        for k in FAN_KEYS:
            if k not in given:
                unit = ("one of: " + ", ".join(FAN_POSITIONS) if k == "fan_position" else
                        "true or false" if k in FLAGS else NUMBERS[k][0])
                why = (WORDS[k][1] if k in WORDS else FLAGS[k] if k in FLAGS
                       else NUMBERS[k][1])
                out.append(_ask(name, k, unit, why + " - asked because the fan was given"))
    return out


def _engine_why(answer):
    why = list(answer.get("refused") or [])
    why += ["missing: %s" % m["input"] for m in answer.get("missing") or []]
    return why or ["the engine answered %s" % answer.get("status")]


def _run(name, inputs, recorded=None):
    got = HVAC.run(name, inputs, recorded)
    if got["status"] != "ok":
        raise _Refused("%s: %s" % (name, "; ".join(_engine_why(got))))
    return got


def _state(alt, **given):
    inputs = dict(given, altitude_m=alt)
    return _run("psychrometrics", inputs)["data"]["state"]


def _engine_checks(system, answer):
    for level, text in answer.get("checks") or []:
        if level in ("WARN", "FAIL") and (level, text) not in system["checks"]:
            system["checks"].append((level, text))


def _hourly(members):
    """{(month, hour): [sensible, latent, total]} - the Spaces' own loads added hour by hour."""
    out = {}
    for m in members:
        for h in (m.get("cooling") or {}).get("hours") or []:
            k = (h["month"], h["hour"])
            got = out.setdefault(k, [0.0, 0.0, 0.0])
            got[0] += h["sensible_w"]
            got[1] += h["latent_w"]
            got[2] += h["total_w"]
    return out


_MONTHS = ("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec")


def _when(k):
    return "%s 21, %02d:00" % (_MONTHS[k[0] - 1], k[1])


def _figure(system, item, value, unit, places, source):
    system["figures"].append({"item": item, "value": value, "unit": unit, "places": places,
                              "source": source})


def _air(system, point, state, source):
    system["air"].append({"point": point, "dry_bulb_c": state["t_c"],
                          "wet_bulb_c": state["twb_c"], "w_g_kg": state["w"] * 1000.0,
                          "rh_pct": state["rh_pct"], "source": source})


def _ventilation(system, name, members, given, recorded):
    """Each Space's 62.1 outdoor air, its exhaust, and the system's intake Vot -
    every figure from the engine's own ventilation calculation."""
    kind, ez = given["ventilation_system"], given["ez"]
    exhaust, table = {}, given.get("exhaust_ls") or {}
    if not isinstance(table, dict):
        raise _Refused("exhaust_ls must be a table of Space number to L/s - {\"105\": 60}")
    for word, flow in table.items():
        r = _find(members, word)
        if r is None:
            raise _Refused("exhaust_ls names Space %r, which is not on air system '%s'"
                           % (word, name))
        exhaust[str(r["id"])] = _number("exhaust_ls[%s]" % word, flow, "L/s", 0, 1e5)
    rows = []
    for m in members:
        a = m["air"]
        own = _run("ventilation", {"system": "single-zone", "zones": [{
            "name": _label(m), "area_m2": float(m["area_m2"]), "people": a["people"],
            "rp_ls_per_person": a["rp_ls_per_person"], "ra_ls_per_m2": a["ra_ls_per_m2"],
            "ez": ez}]}, recorded)
        z = own["data"]["zones"][0]
        e = exhaust.get(str(m["id"]))
        oa = max(z["voz"], e or 0.0)
        rows.append({"space": _label(m), "id": m["id"], "area_m2": float(m["area_m2"]),
                     "people": a["people"], "rp": a["rp_ls_per_person"],
                     "ra": a["ra_ls_per_m2"], "vbz_ls": z["vbz"], "ez": ez,
                     "voz_ls": z["voz"], "exhaust_ls": e, "outdoor_air_ls": oa,
                     "set_by": "its exhaust" if e is not None and e > z["voz"] else "62.1",
                     "rates_source": a["sources"].get("rates", "instruction")})
    zones = []
    if kind == "single-zone":
        az = sum(r["area_m2"] for r in rows)
        pz = sum(r["people"] for r in rows)
        zones.append({"name": name, "area_m2": az, "people": pz,
                      "rp_ls_per_person": (sum(r["rp"] * r["people"] for r in rows) / pz
                                           if pz else 0.0),
                      "ra_ls_per_m2": sum(r["ra"] * r["area_m2"] for r in rows) / az,
                      "ez": ez})
    else:
        for r, m in zip(rows, members):
            zone = {"name": r["space"], "area_m2": r["area_m2"], "people": r["people"],
                    "rp_ls_per_person": r["rp"], "ra_ls_per_m2": r["ra"], "ez": ez}
            if kind == "multiple-zone":
                zone["primary_flow_ls"] = m["supply_ls"]
            zones.append(zone)
    inputs = {"system": kind, "zones": zones}
    if kind == "multiple-zone":
        inputs["ev_method"] = given["ev_method"]
        if "system_population" in given:
            inputs["system_population"] = given["system_population"]
    whole = _run("ventilation", inputs, recorded)
    _engine_checks(system, whole)
    for line in whole.get("assumed") or []:
        if line not in system["notes"]:
            system["notes"].append(line)
    extra = sum(max(0.0, (r["exhaust_ls"] or 0.0) - r["voz_ls"]) for r in rows)
    system["ventilation"] = rows
    return whole["data"], extra


def _size(system, name, members, given, project, recorded):
    """One air system's supply and outdoor air, and its cooling coil - into `system`."""
    alt = float(_value(project["altitude_m"]))
    vent, extra = _ventilation(system, name, members, given, recorded)
    kind, vot = given["ventilation_system"], vent["vot_ls"]
    area = sum(float(m["area_m2"]) for m in members)
    own_supply = sum(float(m["supply_ls"]) for m in members)
    if "supply_ls" in given:
        supply = given["supply_ls"]
        supply_source = "instruction - systems.%s.supply_ls" % name
        if supply < own_supply * (1 - 1e-9):
            system["checks"].append(("WARN", "the supply air given, %.1f L/s, is less than the "
                                             "%.1f L/s its Spaces' own peaks need - some Spaces "
                                             "will be warm at their peak"
                                     % (supply, own_supply)))
    else:
        supply = own_supply
        supply_source = ("calculated - the sum of its Spaces' own supply air, each at its own "
                         "peak sensible load")
    need = vot + extra
    if need > supply * (1 + 1e-9):
        raise _Refused("it needs %.1f L/s of outdoor air and supplies %.1f L/s - give the "
                       "unit's own supply airflow as supply_ls%s"
                       % (need, supply, "" if kind == "100-percent-outdoor-air" else
                          ", or make it 100-percent-outdoor-air"))
    all_outdoor = kind == "100-percent-outdoor-air"
    oa = supply if all_outdoor else need

    # The air: the coil's outdoor design point, and the rooms' return condition.
    to = float(_value(project["coil_outdoor_dry_bulb_c"]))
    twb = float(_value(project["coil_outdoor_wet_bulb_c"]))
    if twb > to:
        raise _Refused("coil_outdoor_wet_bulb_c %s C is above coil_outdoor_dry_bulb_c %s C - "
                       "a wet bulb is never above its dry bulb" % (twb, to))
    ok = members
    tr = given.get("return_dry_bulb_c", _shared(ok, "room_dry_bulb_c"))
    rh = given.get("return_rh_pct", _shared(ok, "room_rh_pct"))
    ts = given.get("supply_dry_bulb_c", _shared(ok, "supply_dry_bulb_c"))
    outdoor = _state(alt, dry_bulb_c=to, wet_bulb_c=twb)
    back = _state(alt, dry_bulb_c=tr, rh_pct=rh)
    if ts >= tr:
        raise _Refused("its supply air at %s C is not below the return air at %s C" % (ts, tr))
    # The Spaces' supply air was sized as a volume at the supply temperature carrying the
    # room's moisture (heron_hvac supply_airflow) - its mass is read on the same basis.
    v_supply = _state(alt, dry_bulb_c=ts, humidity_ratio_g_kg=back["w"] * 1000.0)["v_m3_kg"]
    mass = supply / 1000.0 / v_supply
    outdoor_mass = mass if all_outdoor else oa / 1000.0 / outdoor["v_m3_kg"]
    back_mass = mass - outdoor_mass
    if all_outdoor:
        mixed = outdoor
    elif outdoor_mass <= 0:
        mixed = back
    else:
        mixed = _run("air_mixing", {
            "outdoor_flow_ls": oa, "outdoor_dry_bulb_c": to, "outdoor_wet_bulb_c": twb,
            "return_flow_ls": back_mass * back["v_m3_kg"] * 1000.0,
            "return_dry_bulb_c": tr, "return_rh_pct": rh, "altitude_m": alt})["data"]["mixed"]

    # The fan, only when it is given.
    fan_heat = 0.0
    if "fan_total_pressure_pa" in given:
        fan = _run("fan_power", {"flow_ls": supply,
                                 "total_pressure_pa": given["fan_total_pressure_pa"],
                                 "fan_efficiency_pct": given["fan_efficiency_pct"],
                                 "motor_efficiency_pct": given["motor_efficiency_pct"],
                                 "drive_efficiency_pct": given["drive_efficiency_pct"]},
                   recorded)
        _engine_checks(system, fan)
        fan_heat = fan["data"]["electric_w" if given["motor_in_airstream"] else "shaft_w"]
    entering = mixed
    if fan_heat and given["fan_position"] == "blow-through":
        rise = fan_heat / HVAC.moist_air_sensible_w(mass, mixed["w"], 1.0)
        entering = _state(alt, dry_bulb_c=mixed["t_c"] + rise,
                          humidity_ratio_g_kg=mixed["w"] * 1000.0)

    # The rooms' latent load the supply air must take up sets the air leaving the coil.
    hours = _hourly(members)
    if not hours:
        raise _Refused("its Spaces' hour-by-hour loads are not in this run - calculate again")
    block = max(hours, key=lambda k: hours[k][2])
    wettest = max(hours, key=lambda k: hours[k][1])
    latent = hours[wettest][1]
    needed = HVAC.supply_humidity_needed(back["w"], tr, max(latent, 0.0), mass)
    if needed <= 0:
        raise _Refused("its rooms' latent load, %.0f W, cannot be taken up by %.1f L/s of "
                       "supply air at any humidity - more air, or a lower latent load"
                       % (latent, supply))
    w_out = min(needed, entering["w"])
    leave_t = ts
    if fan_heat and given["fan_position"] == "draw-through":
        leave_t = ts - fan_heat / HVAC.moist_air_sensible_w(mass, w_out, 1.0)
    p = PSY.pressure_at_altitude(alt)
    w_sat = PSY.saturation_humidity_ratio(leave_t, p)
    how = "set by the rooms' latent load"
    if needed >= entering["w"]:
        how = "a dry coil - the entering air is drier than the rooms need"
    if w_out > w_sat:
        w_out = w_sat
        how = "saturated at the leaving dry bulb"
        system["checks"].append(("WARN", "the air would leave the coil saturated at %.1f C "
                                         "- drier than the rooms need, with a bypass factor of "
                                         "0, which no real coil reaches. A real coil leaves it "
                                         "a little short of saturation, with less latent load "
                                         "than shown here, and the rooms sit below their design "
                                         "humidity" % leave_t))
    coil = _run("coil_load", {
        "flow_ls": mass * entering["v_m3_kg"] * 1000.0, "flow_measured_at": "entering",
        "entering_dry_bulb_c": entering["t_c"],
        "entering_humidity_ratio_g_kg": entering["w"] * 1000.0,
        "leaving_dry_bulb_c": leave_t, "leaving_humidity_ratio_g_kg": w_out * 1000.0,
        "altitude_m": alt})
    _engine_checks(system, coil)
    c = coil["data"]
    leaving = c["leaving"]
    supplied = leaving
    if fan_heat and given["fan_position"] == "draw-through":
        supplied = _state(alt, dry_bulb_c=ts, humidity_ratio_g_kg=w_out * 1000.0)
    capacity = HVAC.moist_air_sensible_w(mass, back["w"], tr - ts)
    if capacity < hours[block][0] * (1 - 1e-9):
        system["checks"].append(("WARN", "the supply air takes up %.0f W of sensible heat in "
                                         "the rooms and their block sensible load is %.0f W at "
                                         "%s" % (capacity, hours[block][0], _when(block))))
    if outdoor["w"] < back["w"]:
        system["checks"].append(("WARN", "the coil's outdoor design point, %.4g / %.4g C, is "
                                         "drier than the rooms - check it is the site's "
                                         "dehumidification or wet-bulb design condition"
                                 % (to, twb)))

    # What the page prints - every value with where it came from.
    def same(key):
        return "instruction - systems.%s.%s" % (name, key)
    total, sensible = c["total_w"], c["sensible_w"]
    spaces = "%d Space%s" % (len(members), "" if len(members) == 1 else "s")
    _figure(system, "Floor area served", area, "m2", 1, "model - the area of its %s" % spaces)
    _figure(system, "Supply air", supply, "L/s", 1, supply_source)
    _figure(system, "Supply air per floor area", supply / area, "L/s per m2", 2,
            "calculated - supply air / floor area")
    _figure(system, "Outdoor air", oa, "L/s", 1,
            "all of the supply air - a 100 % outdoor-air unit" if all_outdoor else
            "calculated - ASHRAE 62.1 intake Vot %.1f L/s%s" % (
                vot, (" + %.1f L/s its Spaces exhaust above their own outdoor air" % extra)
                if extra else ""))
    _figure(system, "Outdoor air share of the supply", 100.0 * oa / supply, "%", 1,
            "calculated - by volume")
    _figure(system, "Cooling coil - total", total / 1000.0, "kW", 2,
            "calculated - coil_load: dry-air mass x (entering - leaving enthalpy)")
    _figure(system, "Cooling coil - total", total / HVAC.W_PER_TR, "TR", 2,
            "calculated - 1 TR = 12 000 Btu/h")
    _figure(system, "Cooling coil - sensible", sensible / 1000.0, "kW", 2,
            "calculated - coil_load")
    _figure(system, "Cooling coil - latent", c["latent_w"] / 1000.0, "kW", 2,
            "calculated - coil_load: total - sensible")
    _figure(system, "Sensible heat ratio", c["shr"], "", 3, "calculated - sensible / total")
    _figure(system, "Coil load per floor area", total / area, "W/m2", 1,
            "calculated - coil total / floor area")
    _figure(system, "Floor area per coil kW", area / (total / 1000.0) if total > 0 else None,
            "m2/kW", 2, "calculated - floor area / coil total")
    _figure(system, "Design supply air temperature", ts, "C", 1,
            same("supply_dry_bulb_c") if "supply_dry_bulb_c" in given else
            "its Spaces' own supply_dry_bulb_c (%s)" % members[0]["air"]["sources"]["supply"])
    _figure(system, "Coil leaving dry bulb", leave_t, "C", 1,
            "calculated - the supply temperature less the fan's heat" if leave_t != ts else
            "the design supply air temperature")
    _figure(system, "Apparatus dew point", c["adp_c"], "C", 1,
            "calculated - coil_load: the coil's condition line met with saturation"
            if c["adp_c"] is not None else "none - " + how)
    _figure(system, "Bypass factor", c["bypass_factor"], "", 3,
            "calculated - (leaving - ADP) / (entering - ADP), by dry bulb")
    _figure(system, "Condensate", c["condensate_kg_h"], "kg/h", 1, "calculated - coil_load")
    if "fan_total_pressure_pa" in given:
        _figure(system, "Supply fan electrical input", fan["data"]["electric_w"], "W", 0,
                "calculated - fan_power, from the fan given")
        _figure(system, "Fan heat in the supply air", fan_heat, "W", 0,
                "calculated - %s, %s" % ("the electrical input: the motor is in the air "
                                         "stream" if given["motor_in_airstream"] else
                                         "the shaft power: the motor is out of the air stream",
                                         given["fan_position"]))
    _figure(system, "Rooms' block load", hours[block][2] / 1000.0, "kW", 2,
            "calculated - the largest hour-by-hour sum of its Spaces' own loads, %s"
            % _when(block))
    _figure(system, "Rooms' latent load the supply air takes up", latent, "W", 0,
            "calculated - the largest hour-by-hour sum of its Spaces' latent loads, %s"
            % _when(wettest))
    _figure(system, "Supply air's sensible capacity in the rooms", capacity, "W", 0,
            "calculated - mass x cp x (return - supply dry bulb)")
    _figure(system, "Heating load of its Spaces", sum(m["heating"]["loss_w"] for m in members),
            "W", 0, "calculated - the sum of its Spaces' heating loads; no heating coil is "
                    "sized")

    room_source = (same("return_dry_bulb_c") if "return_dry_bulb_c" in given else
                   "its Spaces' room design condition (%s)" % members[0]["air"]["sources"]["room"])
    _air(system, "Outdoor - the coil's design point", outdoor,
         "instruction - project coil_outdoor_dry_bulb_c / coil_outdoor_wet_bulb_c")
    _air(system, "Return - the rooms", back, room_source)
    if not all_outdoor:
        _air(system, "Mixed outdoor and return air", mixed,
             "calculated - air_mixing, by dry-air mass")
    if entering is not mixed:
        _air(system, "Entering the coil, after the fan", entering,
             "calculated - the fan's heat added")
    _air(system, "Leaving the coil", leaving, "calculated - %s" % how)
    if supplied is not leaving:
        _air(system, "Supply, after the fan", supplied, "calculated - the fan's heat added")
    if c["adp_c"] is not None:
        _air(system, "Apparatus dew point", _state(alt, dry_bulb_c=c["adp_c"], rh_pct=100.0),
             "calculated - on the saturation curve")
    system["coil"] = {
        "supply_ls": supply, "outdoor_air_ls": oa, "vot_ls": vot, "exhaust_extra_ls": extra,
        "total_w": total, "sensible_w": sensible, "latent_w": c["latent_w"], "shr": c["shr"],
        "tr": total / HVAC.W_PER_TR, "w_per_m2": total / area,
        "m2_per_kw": area / (total / 1000.0) if total > 0 else None,
        "entering_db_c": entering["t_c"], "entering_wb_c": entering["twb_c"],
        "leaving_db_c": leaving["t_c"], "leaving_wb_c": leaving["twb_c"],
        "adp_c": c["adp_c"], "bypass_factor": c["bypass_factor"], "supply_db_c": ts,
        "fan_heat_w": fan_heat, "mass_kg_s": mass, "area_m2": area,
        "ventilation_system": kind}


def size(rows, project, systems, recorded=None):
    """Every air system of a run: {"grouped_by", "systems", "asked", "checks", "not_done"}.

    `rows` are the run's Spaces, with their hour-by-hour loads still on them. A system
    is sized only when all its Spaces were calculated and nothing it needs is still to
    be asked; it never holds the room loads up.
    """
    project = project or {}
    systems = systems or {}
    found, mode = groups(rows, systems)
    out = {"grouped_by": mode, "systems": [], "asked": [], "checks": [],
           "not_done": list(NOT_DONE)}
    if found:
        for k, (unit, why, _low, _high) in COIL_KEYS.items():
            if _absent(project, k):
                out["asked"].append({"input": k, "unit": unit, "why": why, "offer": None,
                                     "for": "air systems"})
    project_problem = None
    try:
        for k, (unit, _why, low, high) in COIL_KEYS.items():
            if not _absent(project, k):
                _number(k, _value(project[k]), unit, low, high)
    except _Refused as why:
        project_problem = str(why)
    named = set()
    for name, how, members, problems in found:
        named.add(name)
        given_raw = systems.get(name) or {}
        system = {"name": name, "grouped_by": how,
                  "spaces": [{"id": m.get("id"), "number": m.get("number"),
                              "name": m.get("name"), "status": m.get("status")}
                             for m in members],
                  "area_m2": sum(float(m.get("area_m2") or 0.0) for m in members),
                  "status": "ok", "why": [], "asked": [], "figures": [], "air": [],
                  "ventilation": [], "coil": {}, "checks": [], "notes": []}
        out["systems"].append(system)
        try:
            given = _checked(given_raw)
        except _Refused as why:
            system["status"], system["why"] = "refused", [str(why)]
            continue
        system["asked"] = _asks(name, given, members)
        out["asked"].extend(system["asked"])
        stop = list(problems)
        if project_problem:
            stop.append(project_problem)
        if not members and not problems:
            stop.append("serves no Space")
        stop += ["Space %s was %s, so the unit would be sized without it"
                 % (_label(m), "refused" if m["status"] == "refused" else
                    "not calculated (%s)" % m["status"])
                 for m in members if m.get("status") not in ("ok", "missing")]
        if stop:
            system["status"], system["why"] = "refused", stop
            continue
        waiting = ["Space %s is still waiting for its own inputs" % _label(m)
                   for m in members if m.get("status") == "missing"]
        waiting += ["missing: %s" % a["input"] for a in out["asked"] if a["for"] == "air systems"]
        waiting += ["missing: %s" % a["input"] for a in system["asked"]]
        if waiting:
            system["status"], system["why"] = "missing", waiting
            continue
        try:
            _size(system, name, members, given, project, recorded)
        except (_Refused, PSY.PsychroRangeError) as why:
            system["status"], system["why"] = "refused", [str(why)]
            system["figures"], system["air"], system["coil"] = [], [], {}
    if mode == "Revit zone":
        for name in systems:
            if name not in named:
                out["checks"].append(("WARN", "systems names '%s', which is no Revit zone of a "
                                              "placed Space, and no air system lists its "
                                              "Spaces - nothing was sized for it" % name))
    on = set(str(m.get("id")) for _n, _h, members, _p in found for m in members)
    off = [_label(r) for r in rows if r.get("status") == "ok" and str(r.get("id")) not in on]
    if off:
        out["checks"].append(("INFO", "on no air system: %s - %s" % (", ".join(off), (
            "put them in a Revit zone, or list them under systems" if mode == "Revit zone"
            else "list them under systems"))))
    return out


def summary_lines(air):
    """A few lines for the chat: each system sized, and what is still to be asked."""
    if not air:
        return []
    systems = air.get("systems") or []
    if not systems:
        return ["Air systems: none - no calculated Space is in a Revit zone. Name them as "
                "inputs systems: {\"<name>\": {\"spaces\": [<Space numbers>]}} to size each "
                "unit's coil and outdoor air."]
    lines = ["Air systems (the coil and outdoor air of the unit serving each group of Spaces, "
             "by %s):" % air.get("grouped_by")]
    for s in systems:
        c = s.get("coil") or {}
        if s["status"] == "ok":
            lines.append("  %s (%d Space%s, %.1f m2): cooling coil %.1f kW (%.2f TR), sensible "
                          "%.1f kW, SHR %.2f; supply %.1f L/s with %.1f L/s outdoor air; on the "
                          "coil %.1f / %.1f C in, %.1f / %.1f C out (DB / WB); ADP %s, bypass "
                          "factor %s." % (
                              s["name"], len(s["spaces"]), "" if len(s["spaces"]) == 1 else "s",
                              s["area_m2"], c["total_w"] / 1000.0,
                              c["tr"], c["sensible_w"] / 1000.0, c["shr"], c["supply_ls"],
                              c["outdoor_air_ls"], c["entering_db_c"], c["entering_wb_c"],
                              c["leaving_db_c"], c["leaving_wb_c"],
                              "%.1f C" % c["adp_c"] if c.get("adp_c") is not None else "none",
                              "%.2f" % c["bypass_factor"]
                              if c.get("bypass_factor") is not None else "-"))
        elif s["status"] == "refused":
            lines.append("  %s - not sized: %s" % (s["name"], "; ".join(s["why"])))
    asked = air.get("asked") or []
    if asked:
        waiting = [s["name"] for s in systems if s["status"] == "missing"]
        lines.append("Not sized yet (%s) - Heron needs these first. Put them to the modeller, "
                     "never fill them in (D-33):" % ", ".join(waiting))
        seen = set()
        for a in asked:
            if a["input"] in seen:
                continue
            seen.add(a["input"])
            line = "  - %s (%s) for %s: %s" % (a["input"], a["unit"], a["for"], a["why"])
            if a.get("offer"):
                line += " - offered: %s" % a["offer"]
            lines.append(line)
        lines.append("Give them as inputs {\"project\": {\"coil_outdoor_dry_bulb_c\": ...}, "
                     "\"systems\": {\"<system>\": {...}}}.")
    else:
        for s in systems:
            if s["status"] == "missing":
                lines.append("  %s - waiting: %s" % (s["name"], "; ".join(s["why"])))
    for level, text in air.get("checks") or []:
        lines.append("  %s %s" % (level, text))
    return lines
