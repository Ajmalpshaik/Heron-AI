# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The fire water a sprinkler system's source must give - docs/46 section 13.1.

After the sprinkler hydraulics are solved, this puts the rest of the fire
water together with them: the standpipes and the hose reels the modeller
includes, the tank, the fire pump, and the flow test checked again against the
total. Every figure is heron_fire's own: `standpipe`, `hose_reels`,
`water_storage`, `fire_pump`, `water_supply`. What this adds is only the
bookkeeping between them - which parts the modeller included, which run at the
same time as the sprinklers. Even the sum of those is the engine's:
`combined_demand` adds the flows, takes the highest pressure any of them needs
at the source, and says in words that each part's own path is NOT solved
together with the others.

Nothing is included unless the modeller ticks it, and a part not included is
said on the sheet - never left silent. A part's missing input is asked in
that part only; it never stops another part, or the sprinkler solve.

Standard library and heron_fire only.
"""

import heron_fire as FIRE

#: The parts, in the order they are worked out: (key, the engine's calculation,
#: what the modeller calls it).
PARTS = (("standpipe", "standpipe", "Standpipes"),
         ("hose_reels", "hose_reels", "Hose reels"),
         ("storage", "water_storage", "Fire water tank"),
         ("pump", "fire_pump", "Fire pump"))
#: Inputs this module fills from the sprinkler run and the other parts - never
#: asked on the page.
FILLED = {"water_storage": ("sprinkler_flow_lpm", "hose_allowance_lpm", "other_demands"),
          "fire_pump": ("demand_flow_lpm", "demand_pressure_bar")}
#: Parts that can run at the same time as the sprinklers.
SIMULTANEOUS = ("standpipe", "hose_reels")
#: The project standards each calculation reads - and only those are handed to it,
#: so none comes back IGNORED.
READS = {"standpipe": ("fire_authority",), "hose_reels": ("fire_authority",),
         "water_storage": ("sprinkler_standard", "fire_authority"),
         "fire_pump": ("fire_authority",), "water_supply": (), "combined_demand": ()}
#: The sprinkler hose allowance and a standpipe may be the same water (docs/46 s13.1).
SAME_WATER = ("the sprinkler hose allowance and a standpipe running at the same time may be "
              "the same water - counted here as both; whether one serves for the other is the "
              "engineer's and the authority's call - NFPA 14's rule for a combined system is "
              "not held by Heron: from your copy")
HEIGHT = ("how far the most remote outlet is ABOVE the sprinkler system's source - the same "
          "point the sprinkler demand is reported at, so the two pressures can be compared")
NOT_SOLVED_TOGETHER = ("the pressure the source must give is the highest any included demand "
                       "needs there - each part's own path is not solved together with the "
                       "others")


def _value(v):
    return v.get("value") if isinstance(v, dict) else v


def fields():
    """{part: [{"input", "unit", "why", "required"}]} - the engine's own questions for each
    part, less what this module fills; so the page holds no list of its own."""
    out = {}
    for key, calc, _label in PARTS:
        skip = FILLED.get(calc, ())
        out[key] = [dict(f) for f in FIRE.fields(calc) if f["input"] not in skip]
        for f in out[key]:
            if f["input"] == "height_m":
                f["why"] = HEIGHT
            # The runner always gives the pump a demand, and then the engine
            # needs the suction it starts from.
            if calc == "fire_pump" and f["input"] == "suction_pressure_bar":
                f["required"] = True
        # The standpipe calculation asks no duration of its own; the tank needs one.
        if key in SIMULTANEOUS and not any(f["input"] == "duration_min" for f in out[key]):
            rule = FIRE.lookup("standpipe_rules", "duration")
            out[key].append({"input": "duration_min", "unit": "min",
                             "why": "how long this part must run - for the tank",
                             "required": False,
                             "offer": ("%s: %s - offered, not applied" % (
                                 FIRE.REFERENCES["standpipe_rules"]["source"], rule[1]))
                             if rule else None})
    return out


def _given(water, key):
    """The part's own inputs, plain values, blanks left out."""
    part = (water or {}).get(key) or {}
    return dict((k, _value(v)) for k, v in part.items()
                if _value(v) is not None and _value(v) != "")


def _included(water, key):
    return bool(((water or {}).get("include") or {}).get(key))


def _simultaneous(water, key):
    return bool(((water or {}).get("simultaneous") or {}).get(key))


def _asked(key, answer):
    return [{"input": "water.%s.%s" % (key, m["input"]), "unit": m.get("unit"),
             "why": m.get("why"), "offer": m.get("reference")} for m in answer["missing"]]


def _std(standards, calc):
    """Only the project standards this calculation reads."""
    return dict((k, v) for k, v in (standards or {}).items() if k in READS.get(calc, ()))


def _run(calc, inputs, standards, recorded, out):
    """One engine calculation, its once-per-project questions gathered for the run."""
    got = FIRE.run(calc, dict(_std(standards, calc), **inputs), recorded=recorded)
    for q in got.get("ask_once") or []:
        name = "standards.%s" % q["input"]
        if not any(a["input"] == name for a in out["asked"]):
            out["asked"].append({"input": name, "unit": q.get("unit"), "why": q.get("why"),
                                 "offer": q.get("reference")})
    out["answers"].append(got)
    return got


def run(sprinkler, water, standards=None, recorded=None):
    """
    The fire water for one solved sprinkler run. `sprinkler` is the run's
    result (heron_sprinkler_run.run); `water` the modeller's inputs:
    {"include": {part: bool}, "simultaneous": {part: bool}, "<part>": {...}}.

    Returns {"status", "asked", "parts": {key: answer or None}, "total",
    "notes"}. "status" is "ok" when every included part answered, "missing"
    when one still asks, "refused" when one refused, "not solved" when the
    sprinklers are not.
    """
    water = water if isinstance(water, dict) else {}
    standards = dict(standards or {})
    out = {"status": None, "asked": [], "parts": {}, "total": None, "notes": [],
           "answers": [], "included": dict((k, _included(water, k)) for k, _c, _l in PARTS)}
    if water.get("_error"):
        out["status"] = "refused"
        out["refused"] = ["the fire water inputs could not be read: %s" % water["_error"]]
        return out
    answer = (sprinkler or {}).get("answer") or {}
    if (sprinkler or {}).get("status") != "ok" or not answer.get("data"):
        out["status"] = "not solved"
        out["notes"].append("the sprinkler system is not solved yet, so the fire water is not "
                            "worked out")
        return out
    d = answer["data"]
    demands = [{"name": "sprinklers with their hose allowance", "flow_lpm": float(d["total_lpm"]),
                "pressure_bar": float(d["supply_bar"])}]
    made = ["sprinklers %s with the hose allowance, needing %s at the source"
            % (FIRE.flow_text(d["total_lpm"]), FIRE.pressure_text(d["supply_bar"]))]
    others = []
    failed = []
    for key, calc, label in PARTS[:2]:
        if not _included(water, key):
            out["parts"][key] = None
            out["notes"].append("%s: not included by the modeller" % label)
            continue
        given = _given(water, key)
        duration = given.pop("duration_min", None) if calc == "standpipe" else None
        got = _run(calc, given, standards, recorded, out)
        out["parts"][key] = got
        if got["status"] == "missing":
            out["asked"] += _asked(key, got)
            continue
        if got["status"] != "ok":
            failed.append("%s: %s" % (label, "; ".join(got["refused"][:2])))
            continue
        part = got["data"]
        if not _simultaneous(water, key):
            out["notes"].append("%s: included, and NOT running at the same time as the "
                                "sprinklers - its flow is not added to theirs" % label)
            continue
        demands.append({"name": label.lower(), "flow_lpm": part["flow_lpm"],
                        "pressure_bar": part["source_bar"]})
        made.append("%s %s, needing %s at the source%s" % (
            label.lower(), FIRE.flow_text(part["flow_lpm"]),
            FIRE.pressure_text(part["source_bar"]),
            "" if not got.get("assumed") else " - assumed: %s" % "; ".join(got["assumed"])))
        last = duration if calc == "standpipe" else given.get("duration_min")
        others.append((label.lower(), part["flow_lpm"], last))
        if key == "standpipe" and d.get("hose_lpm"):
            out["notes"].append(SAME_WATER)
    both = _run("combined_demand", {"demands": demands}, standards, recorded, out)
    out["parts"]["combined"] = both
    flow = both["data"]["flow_lpm"]
    pressure = both["data"]["pressure_bar"]
    out["total"] = {"flow_lpm": flow, "pressure_bar": pressure, "from": made,
                    "governing": both["data"]["governing"]}
    if len(demands) > 1:
        out["notes"].append(NOT_SOLVED_TOGETHER)

    # The tank: the sprinkler flow and the hose allowance over the duration,
    # and each simultaneous part over its own.
    if _included(water, "storage"):
        given = _given(water, "storage")
        demands = []
        for name, q, minutes in others:
            if minutes is None:
                out["asked"].append({"input": "water.%s.duration_min" % (
                    "standpipe" if name.startswith("standpipe") else "hose_reels"),
                    "unit": "min", "why": "how long the %s must run - the tank holds it for "
                    "that long" % name, "offer": None})
                continue
            demands.append({"name": name, "flow_lpm": q, "duration_min": minutes})
        inputs = dict(given)
        inputs["sprinkler_flow_lpm"] = d["demand_lpm"]
        if d.get("hose_lpm"):
            inputs["hose_allowance_lpm"] = d["hose_lpm"]
        if demands:
            inputs["other_demands"] = demands
        got = _run("water_storage", inputs, standards, recorded, out)
        out["parts"]["storage"] = got
        if got["status"] == "missing":
            out["asked"] += _asked("storage", got)
        elif got["status"] != "ok":
            failed.append("Fire water tank: %s" % "; ".join(got["refused"][:2]))
    else:
        out["parts"]["storage"] = None
        out["notes"].append("Fire water tank: not included by the modeller")

    # The pump, against the total.
    if _included(water, "pump"):
        inputs = dict(_given(water, "pump"))
        inputs["demand_flow_lpm"] = flow
        inputs["demand_pressure_bar"] = pressure
        got = _run("fire_pump", inputs, standards, recorded, out)
        out["parts"]["pump"] = got
        if got["status"] == "missing":
            out["asked"] += _asked("pump", got)
        elif got["status"] != "ok":
            failed.append("Fire pump: %s" % "; ".join(got["refused"][:2]))
    else:
        out["parts"]["pump"] = None
        out["notes"].append("Fire pump: not included by the modeller")

    # The flow test against the total, when the total is more than the sprinklers'.
    crit = (sprinkler.get("inputs") or {}).get("criteria") or {}
    test = dict((k, _value(crit[k])) for k in ("supply_static_bar", "supply_residual_bar",
                                              "supply_test_flow_lpm", "safety_margin_bar")
                if k in crit)
    if len(made) > 1 and "supply_static_bar" in test:
        got = _run("water_supply", dict(test, demand_flow_lpm=flow,
                                        demand_pressure_bar=pressure), standards, recorded, out)
        out["parts"]["supply"] = got
        if got["status"] == "missing":
            out["asked"] += _asked("supply", got)
    if failed:
        out["status"] = "refused"
        out["refused"] = failed
    elif any(not a["input"].startswith("standards.") for a in out["asked"]):
        # A project standard asked once (the fire authority) is a question, and the
        # parts that answered without it still stand - only a part's own missing
        # input leaves the fire water unfinished.
        out["status"] = "missing"
    else:
        out["status"] = "ok"
    return out


def summary_line(water):
    """One line for the chat."""
    if not water or water.get("status") == "not solved":
        return ""
    t = water.get("total") or {}
    inc = [k for k, v in (water.get("included") or {}).items() if v]
    if not inc:
        return "Fire water: no standpipes, hose reels, tank or pump included yet."
    line = "Fire water: %s at %s at the source" % (FIRE.flow_text(t.get("flow_lpm")),
                                                   FIRE.pressure_text(t.get("pressure_bar")))
    tank = (water.get("parts") or {}).get("storage")
    if tank and tank.get("status") == "ok":
        line += "; tank %s" % FIRE.volume_text(tank["data"]["total_m3"])
    pump = (water.get("parts") or {}).get("pump")
    if pump and pump.get("status") == "ok":
        line += "; the pump %s" % ("meets it" if pump["data"]["ok"] else "FAILS a check")
    if water.get("asked"):
        line += "; %d thing(s) still to answer" % len(water["asked"])
    return line + "."
