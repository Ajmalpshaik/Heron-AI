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
same time as the sprinklers - and two sums it says in words:

  * the total flow at the source is the sprinkler demand (with its hose
    allowance) plus every included part ticked as running at the same time;
  * the pressure the source must give is the highest any of those demands
    needs there. Each part's own path is NOT solved together with the others.

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
        out[key] = [f for f in FIRE.fields(calc) if f["input"] not in skip]
        # The standpipe calculation asks no duration of its own; the tank needs one.
        if key in SIMULTANEOUS and not any(f["input"] == "duration_min" for f in out[key]):
            out[key].append({"input": "duration_min", "unit": "min",
                             "why": "how long this part must run - for the tank",
                             "required": False})
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
           "included": dict((k, _included(water, k)) for k, _c, _l in PARTS)}
    answer = (sprinkler or {}).get("answer") or {}
    if (sprinkler or {}).get("status") != "ok" or not answer.get("data"):
        out["status"] = "not solved"
        out["notes"].append("the sprinkler system is not solved yet, so the fire water is not "
                            "worked out")
        return out
    d = answer["data"]
    flow = float(d["total_lpm"])
    pressure = float(d["supply_bar"])
    made = ["sprinklers %s with the hose allowance, needing %s at the source"
            % (FIRE.flow_text(flow), FIRE.pressure_text(pressure))]
    others = []
    failed = []
    for key, calc, label in PARTS[:2]:
        if not _included(water, key):
            out["parts"][key] = None
            out["notes"].append("%s: not included by the modeller" % label)
            continue
        given = _given(water, key)
        duration = given.pop("duration_min", None) if calc == "standpipe" else None
        got = FIRE.run(calc, dict(standards, **given), recorded=recorded)
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
        flow += part["flow_lpm"]
        if part["source_bar"] > pressure:
            pressure = part["source_bar"]
        made.append("%s %s, needing %s at the source" % (
            label.lower(), FIRE.flow_text(part["flow_lpm"]),
            FIRE.pressure_text(part["source_bar"])))
        last = duration if calc == "standpipe" else given.get("duration_min")
        others.append((label.lower(), part["flow_lpm"], last))
    out["total"] = {"flow_lpm": flow, "pressure_bar": pressure, "from": made}
    if len(made) > 1:
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
        inputs = dict(standards, **given)
        inputs["sprinkler_flow_lpm"] = d["demand_lpm"]
        if d.get("hose_lpm"):
            inputs["hose_allowance_lpm"] = d["hose_lpm"]
        if demands:
            inputs["other_demands"] = demands
        got = FIRE.run("water_storage", inputs, recorded=recorded)
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
        inputs = dict(standards, **_given(water, "pump"))
        inputs["demand_flow_lpm"] = flow
        inputs["demand_pressure_bar"] = pressure
        got = FIRE.run("fire_pump", inputs, recorded=recorded)
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
        got = FIRE.run("water_supply", dict(test, demand_flow_lpm=flow,
                                            demand_pressure_bar=pressure), recorded=recorded)
        out["parts"]["supply"] = got
        if got["status"] == "missing":
            out["asked"] += _asked("supply", got)
    if failed:
        out["status"] = "refused"
        out["refused"] = failed
    elif out["asked"]:
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
