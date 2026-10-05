# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
A sprinkler system's hydraulic calculation from the model's network -
docs/46 section 5.

Builds one input for heron_fire's `hydraulic` from the network
(heron_sprinkler_takeoff), the criteria the modeller gave, the K-factor they
confirmed for each sprinkler type, the equivalent length they confirmed for
each kind and size of fitting, the heads they ticked as the remote area and
the source - and runs it. It adds no physics (one fact, one home:
heron_fire.py). The one figure of its own is the remote-area SUGGESTION, a
ranking of the heads by how far they are from the source along the pipe; it
is never a hydraulic result and it is never used until the modeller ticks.

A value nobody gave is ASKED, with the standard's figure offered and named,
and never filled in (D-33). Every value the run used carries where it came
from.

Standard library only (heron_designbasis is reached for the knowledge folder
only when a run is kept or read back).
"""

import datetime
import heapq
import io
import json
import math
import os
import re

import heron_fire as FIRE
import heron_sprinkler_water as WATER
import heron_sprinkler_spacing as SPACING
import heron_sprinkler_takeoff as TAKEOFF

FORMAT = 1
# name: (unit, why, low, high, required). The range refuses what cannot be a
# design figure - a value in the wrong unit - and is never a design value.
CRITERIA = {
    "density_mm_min": ("mm/min", "the design density over the remote area", 0.5, 100, True),
    "area_per_sprinkler_m2": ("m2", "the floor area each sprinkler protects in the layout, "
                              "S x L - its flow is the density over it", 1, 50, True),
    "design_area_m2": ("m2", "the design (remote) area - how many heads the suggestion "
                       "ticks", 10, 2000, True),
    "min_pressure_bar": ("bar", "the least pressure any operating sprinkler may run at",
                         0.05, 20, True),
    "c_factor": ("Hazen-Williams C", "the C of the pipe - from its material and the system "
                 "type", 50, 160, True),
    "hose_allowance_lpm": ("L/min", "hose streams added at the source", 0, 20000, False),
    "max_velocity_ms": ("m/s", "a velocity no pipe may exceed, if the standard or the "
                        "authority sets one", 0.5, 20, False),
    "supply_static_bar": ("bar", "the supply's static pressure, from a flow test", 0, 50, False),
    "supply_residual_bar": ("bar", "the residual pressure at the test flow", 0, 50, False),
    "supply_test_flow_lpm": ("L/min", "the flow the test measured the residual at", 1, 1e5,
                             False),
    "device_loss_bar": ("bar", "losses between the source and the supply not drawn as pipe - "
                        "an alarm valve, a backflow preventer", 0, 20, False),
    "safety_margin_bar": ("bar", "the margin the supply must clear the demand by, if the "
                          "authority sets one", 0, 20, False),
}
ORDER = ("density_mm_min", "area_per_sprinkler_m2", "design_area_m2", "min_pressure_bar",
         "c_factor", "hose_allowance_lpm", "max_velocity_ms", "supply_static_bar",
         "supply_residual_bar", "supply_test_flow_lpm", "device_loss_bar", "safety_margin_bar")
# Criteria that are the remote area's, not the solver's - never handed to it.
NOT_SOLVER = ("design_area_m2", "area_per_sprinkler_m2")
K_RANGE = (10.0, 2000.0)
EQ_RANGE = (0.0, 100.0)
SOURCES = ("model", "instruction", "standard", "assumption")


class InputError(ValueError):
    """An input that cannot be a value of its kind - said, never repaired."""


def _value(v):
    return v.get("value") if isinstance(v, dict) else v


def _labelled(d):
    out = {}
    for k, v in (d or {}).items():
        if _value(v) is None or _value(v) == "":
            continue
        out[str(k)] = dict(v) if isinstance(v, dict) else {"value": v, "source": "instruction"}
        out[str(k)].setdefault("source", "instruction")
    return out


def _number(name, raw, unit, low, high):
    try:
        value = float(raw)
    except (TypeError, ValueError):
        raise InputError("%s %r is not a number in %s" % (name, raw, unit))
    if value != value or not (low <= value <= high):
        raise InputError("%s %s is outside %s to %s %s - check the unit" % (
            name, FIRE._g(value), FIRE._g(low), FIRE._g(high), unit))
    return value


def normalise(inputs):
    """The inputs as the run keeps them - every value labelled with where it came from.

    {"criteria", "k", "fittings", "operating", "source", "standards", "spacing",
    "water"}. A value that cannot be one raises InputError naming it. The
    spacing and fire water inputs are kept as given: their values are checked
    by the engine that reads them (heron_sprinkler_spacing, heron_sprinkler_water).
    """
    if isinstance(inputs, str):
        try:
            inputs = json.loads(inputs) if inputs.strip() else {}
        except ValueError as why:
            raise InputError("the inputs are not JSON: %s" % why)
    inputs = inputs if isinstance(inputs, dict) else {}
    out = {"criteria": _labelled(inputs.get("criteria")), "k": _labelled(inputs.get("k")),
           "fittings": _labelled(inputs.get("fittings")),
           "operating": [str(x) for x in inputs.get("operating") or []],
           # What Heron's Suggest ticked, kept so the sheet can say whether the
           # modeller changed it (gate 2) - never used to solve.
           "suggested": [str(x) for x in inputs.get("suggested") or []],
           "source": str(inputs["source"]) if inputs.get("source") else None,
           "standards": dict((k, v) for k, v in (inputs.get("standards") or {}).items()
                             if v not in (None, "")),
           "spacing": _section(inputs.get("spacing"), "spacing"),
           "water": _section(inputs.get("water"), "water")}
    unknown = [k for k in out["criteria"] if k not in CRITERIA]
    if unknown:
        raise InputError("%s is not a criterion Heron reads - %s" % (
            ", ".join(sorted(unknown)), ", ".join(ORDER)))
    for name, entry in out["criteria"].items():
        unit, _why, low, high, _req = CRITERIA[name]
        entry["value"] = _number(name, entry["value"], unit, low, high)
    for name, entry in out["k"].items():
        entry["value"] = _number("the K-factor of %s" % name, entry["value"], "L/min/bar^0.5",
                                 *K_RANGE)
    for name, entry in out["fittings"].items():
        entry["value"] = _number("the equivalent length of %s" % name, entry["value"], "m",
                                 *EQ_RANGE)
    return out


def _nested(raw, name):
    """A section of the inputs that is a map of maps (spacing, water), blanks left out."""
    if raw is None:
        return {}
    if not isinstance(raw, dict):
        raise InputError("the %s inputs are not a map" % name)
    out = {}
    for k, v in raw.items():
        if isinstance(v, dict):
            inner = _nested(v, "%s.%s" % (name, k))
            if inner:
                out[str(k)] = inner
        elif v is not None and v != "" and _value(v) not in (None, ""):
            out[str(k)] = v
    return out


def _section(raw, name):
    """A spacing or fire water section - one that cannot be read refuses that section only,
    never the sprinkler solve (docs/46 s13.1; the review, R13)."""
    try:
        return _nested(raw, name)
    except InputError as why:
        return {"_error": str(why)}


def _plain(d):
    return dict((k, _value(v)) for k, v in (d or {}).items())


def default_source(n):
    """The system's base equipment's node, when it has one - else None (never guessed)."""
    n = TAKEOFF.read(n)
    found = TAKEOFF.source_candidates(n)
    if found and found[0]["what"].startswith("the system's base equipment"):
        return found[0]["id"]
    return None


def _standard(inputs, recorded):
    """The project's sprinkler standard - as given in this call, else as kept for it."""
    given = (inputs.get("standards") or {}).get("sprinkler_standard")
    if given:
        return given
    held = (recorded or {}).get("sprinkler_standard")
    return held.get("value") if isinstance(held, dict) else held


def _fitting_offer(row, c_factor):
    """The chart's figure for a fitting row, as a sentence - adjusted to the pipe's own bore
    and C by heron_fire's `equivalent_length` when C is known, else the chart's own cell
    labelled for what it is. None when the chart holds no row for it."""
    chart = row.get("chart")
    if chart is None:
        return None
    if c_factor is not None and row.get("bore_mm"):
        got = FIRE.run("equivalent_length", {"c_factor": c_factor, "fittings": [
            {"fitting": chart[0], "nominal": chart[1], "count": 1,
             "bore_mm": row["bore_mm"]}]})
        if got["status"] == "ok":
            text = dict(got["results"]).get("Equivalent length", "")
            return ("%s for this pipe (%s mm bore, C %s) - NFPA 13's chart adjusted by "
                    "heron_fire; offered, not applied" % (text.split(" - ")[0],
                                                          FIRE._g(row["bore_mm"]),
                                                          FIRE._g(c_factor)))
    return ("%s m (%s ft) for schedule 40 steel at C = 120 - %s; give C to have it adjusted "
            "to this pipe; offered, not applied" % (
                FIRE._g(chart[3]), FIRE._g(chart[2]),
                FIRE.REFERENCES["equivalent_lengths"]["source"]))


def needs(n, inputs, recorded=None):
    """What is still to be asked, in order: the project's sprinkler standard (once, D-111),
    criteria, K per type, fittings, source, remote area.

    Each {"input", "unit", "why", "offer"} - the offer is a standard's or a
    chart's figure, offered and never applied (D-33).
    """
    n = TAKEOFF.read(n)
    inputs = inputs if isinstance(inputs, dict) and "criteria" in inputs else normalise(inputs)
    asked = []
    standard = _standard(inputs, recorded)
    if not standard:
        asked.append({"input": "standards.sprinkler_standard", "unit": "a standard and edition",
                      "why": "which standard governs this project's sprinklers - asked once "
                      "and kept for the project", "offer": "NFPA 13-2022, NFPA 13-2019, "
                      "BS EN 12845 or FM Global - the project's specification says which"})
    offers = FIRE.hydraulic_offers(standard)
    c_given = _value(inputs["criteria"].get("c_factor"))
    for name in ORDER:
        unit, why, _low, _high, required = CRITERIA[name]
        if required and name not in inputs["criteria"]:
            asked.append({"input": "criteria." + name, "unit": unit, "why": why,
                          "offer": offers.get(name)})
    for key, row in sorted(TAKEOFF.sprinkler_types(n).items()):
        if key not in inputs["k"]:
            held = [x for x in (row.get("parameter"), None if row.get("connector") is None
                                else "connector value %s (unit not known)" % FIRE._g(
                                    row["connector"])) if x]
            asked.append({"input": "k." + key, "unit": "L/min/bar^0.5",
                          "why": "the K-factor of sprinkler type %s (%d heads) - from its "
                          "data sheet" % (row["name"], len(row["heads"])),
                          "offer": ("Revit holds: %s - confirm it in L/min/bar^0.5"
                                    % "; ".join(held)) if held else None})
    for key, row in sorted(TAKEOFF.fitting_rows(n).items()):
        if key not in inputs["fittings"]:
            asked.append({"input": "fittings." + key, "unit": "m",
                          "why": "the equivalent length of one %s at %s (%d in the system)"
                          % (row["kind"], row["size"] or "a size not read", row["count"]),
                          "offer": _fitting_offer(row, c_given) or "not held - from your copy "
                          "of the chart or the maker's data; 0 if your standard counts none "
                          "for it"})
    source = inputs.get("source") or default_source(n)
    if source is None and n.system is not None:
        asked.append({"input": "source", "unit": "a point of the network",
                      "why": "where the demand is reported - the system has no base "
                      "equipment", "offer": "; ".join(
                          "%s: %s" % (c["id"], c["what"]) for c in
                          TAKEOFF.source_candidates(n)[:10]) or None})
    if not inputs.get("operating") and TAKEOFF.sprinkler_types(n):
        asked.append({"input": "operating", "unit": "sprinkler ids",
                      "why": "the heads in the remote area - tick them, or press Suggest",
                      "offer": "Suggest ticks the heads farthest from the source along the "
                      "pipe, as many as the design area holds"})
    return asked


def suggest(n, inputs):
    """
    (head ids, count, why) - the heads farthest from the source along the pipe,
    as many as ceil(design area / area per sprinkler). Farthest by the
    friction a pipe costs: the sum of (length + fittings) / bore^4.87 along
    the cheapest path. A SUGGESTION: the modeller ticks.
    """
    n = TAKEOFF.read(n)
    inputs = inputs if isinstance(inputs, dict) and "criteria" in inputs else normalise(inputs)
    c = _plain(inputs["criteria"])
    if c.get("design_area_m2") is None or c.get("area_per_sprinkler_m2") is None:
        raise InputError("the design area and the area per sprinkler are needed to know how "
                         "many heads to suggest")
    source = inputs.get("source") or default_source(n)
    if source is None:
        raise InputError("the source is not known yet - choose it first")
    count = int(math.ceil(c["design_area_m2"] / c["area_per_sprinkler_m2"] - 1e-9))
    records, pipes = TAKEOFF.graph(n)
    if source not in records:
        raise InputError("the source %s is not a point of this network" % source)
    eq = {}
    if inputs.get("fittings"):
        try:
            _nodes, solved, _notes = TAKEOFF.network(n, {}, _plain(inputs["fittings"]), [],
                                                     source)
            eq = dict((p["segment"], p["equivalent_length_m"]) for p in solved)
        except ValueError:
            eq = {}
    touching = dict((nid, []) for nid in records)
    for seg in pipes:
        bore = float(seg["e"].get("inner_diameter_m") or 0.0) * 1000.0
        if bore <= 0:
            continue
        w = ((seg["length_m"] or 0.0) + seg["added_m"] + eq.get(seg["id"], 0.0)) / bore ** 4.87
        touching[seg["a"]].append((seg["b"], w))
        touching[seg["b"]].append((seg["a"], w))
    far = {source: 0.0}
    todo = [(0.0, source)]
    while todo:
        d, here = heapq.heappop(todo)
        if d > far.get(here, float("inf")):
            continue
        for other, w in touching[here]:
            if d + w < far.get(other, float("inf")):
                far[other] = d + w
                heapq.heappush(todo, (d + w, other))
    heads = TAKEOFF.head_nodes(n)
    reached = [(far[nid], eid) for eid, nid in heads.items() if nid in far]
    reached.sort(key=lambda x: (-x[0], x[1]))
    chosen = [eid for _d, eid in reached[:count]]
    why = ("%d heads - %s m2 / %s m2 each, rounded up - the farthest from the source by the "
           "friction of the pipe between" % (count, FIRE._g(c["design_area_m2"]),
                                              FIRE._g(c["area_per_sprinkler_m2"])))
    if len(chosen) < count:
        why += "; the system has only %d heads the source reaches" % len(chosen)
    return chosen, count, why


def _now():
    return datetime.datetime.now()


def run(n, inputs, recorded=None):
    """
    One run (see _solve), then - whatever it came to - every head's spacing in
    its Space (heron_sprinkler_spacing), and, when the sprinklers are solved,
    the fire water (heron_sprinkler_water). Neither of those ever stops the solve.
    """
    result = _solve(n, inputs, recorded)
    given = result.get("inputs")
    result["spacing"] = None
    result["water"] = None
    if given is None:
        return result
    standard = _standard(given, recorded)
    result["spacing"] = SPACING.check(n, given.get("spacing"), standard)
    result["water"] = WATER.run(result, given.get("water"), given.get("standards"), recorded)
    return result


def _solve(n, inputs, recorded=None):
    """
    One run. While anything is asked, nothing is solved: {"status": "missing",
    "asked": [...]}. A FAIL in the model checks refuses the run with the
    checks' own words. Else heron_fire's `hydraulic` is run and its answer
    kept whole in "answer".
    """
    n = TAKEOFF.read(n)
    when = _now()
    result = {"format": FORMAT, "run_id": when.strftime("%Y%m%d-%H%M%S"),
              "when": when.strftime("%Y-%m-%dT%H:%M:%S"),
              "system": dict((k, (n.system or {}).get(k)) for k in ("id", "name",
                                                                    "classification")),
              "network_fingerprint": TAKEOFF.fingerprint(n), "status": None, "asked": [],
              "refused": [], "qa": [], "inputs": None, "source": None, "nodes": [],
              "pipes": [], "answer": None, "notes": [], "suggested": None,
              "network_confirmed": None}
    try:
        given = normalise(inputs)
    except InputError as why:
        result.update(status="refused", refused=[str(why)])
        return result
    source = given.get("source") or default_source(n)
    if source and not given.get("source"):
        given["source"] = source
        given["source_label"] = "model"
    result["inputs"] = given
    result["source"] = source
    if given["suggested"]:
        result["suggested"] = {"heads": list(given["suggested"])}
    k_plain, eq_plain = _plain(given["k"]), _plain(given["fittings"])
    result["qa"] = TAKEOFF.qa(n, k_plain, source)
    result["asked"] = needs(n, given, recorded)
    heads = TAKEOFF.head_nodes(n)
    stray = [h for h in given["operating"] if h not in heads]
    if stray:
        result.update(status="refused", refused=["%s %s not a sprinkler of this system" % (
            ", ".join(stray[:8]), "is" if len(stray) == 1 else "are")])
        return result
    if result["asked"]:
        result["status"] = "missing"
        return result
    fails = [f["text"] for f in result["qa"] if f["level"] == "FAIL"]
    if fails:
        result.update(status="refused", refused=fails)
        return result
    try:
        nodes, pipes, notes = TAKEOFF.network(n, k_plain, eq_plain, given["operating"], source)
    except ValueError as why:
        result.update(status="refused", refused=[str(why)])
        return result
    crit = _plain(given["criteria"])
    on = set(heads[h] for h in given["operating"])
    nodes, pipes, removed = TAKEOFF.prune(nodes, pipes, on | {source})
    if removed:
        notes.append("%d pipe(s) on branches with no head in the remote area are left out of "
                     "the solve - a dead end with no open head carries no water, so this "
                     "changes no pressure" % removed)
    if len(nodes) > TAKEOFF.MAX_NODES or len(pipes) > TAKEOFF.MAX_PIPES:
        result.update(status="refused", refused=[
            "%d points and %d pipes carry water - Heron solves up to %d and %d; select the "
            "part of the system that holds the remote area and read it again"
            % (len(nodes), len(pipes), TAKEOFF.MAX_NODES, TAKEOFF.MAX_PIPES)])
        return result
    for node in nodes:
        if node["id"] in on:
            node["area_per_sprinkler_m2"] = crit["area_per_sprinkler_m2"]
    solver = dict((k, v) for k, v in crit.items() if k not in NOT_SOLVER)
    solver.update(given.get("standards") or {})
    solver.update({"source": source,
                   "nodes": nodes,
                   "pipes": [dict((k, v) for k, v in p.items()
                                  if k not in ("element", "segment")) for p in pipes]})
    answer = FIRE.run("hydraulic", solver, recorded=recorded)
    result.update(nodes=nodes, pipes=pipes, notes=notes, answer=answer,
                  status=answer["status"])
    if answer["status"] == "missing":
        result["asked"] = [{"input": "standards." + m["input"] if m["input"] in (
            "sprinkler_standard",) else m["input"], "unit": m.get("unit"), "why": m.get("why"),
            "offer": m.get("reference")} for m in answer["missing"]]
    if answer["status"] == "refused":
        result["refused"] = list(answer["refused"])
    return result


# --- gate 1 -----------------------------------------------------------------

def _k_key(result):
    k = sorted(_plain(((result or {}).get("inputs") or {}).get("k")).items())
    return json.dumps(k)


def confirm(result, n, by="the modeller, in the Heron Companion"):
    """Record that the modeller checked this network, with these K-factors."""
    result["network_confirmed"] = {
        "at": _now().strftime("%Y-%m-%dT%H:%M:%S"), "by": by,
        "network": TAKEOFF.fingerprint(n), "k": _k_key(result)}
    return result


def confirmed(result, n):
    """True only when the run carries a confirmation of THIS network with THESE K-factors."""
    got = (result or {}).get("network_confirmed") or {}
    return (bool(got) and got.get("network") == TAKEOFF.fingerprint(n)
            and got.get("k") == _k_key(result))


# --- the chat's few lines -----------------------------------------------------

def summary_text(result):
    """Three to five lines for the chat - the rest is on the panel."""
    text = _summary(result)
    extra = [x for x in (SPACING.summary_line(result.get("spacing")),
                         WATER.summary_line(result.get("water"))) if x]
    return " ".join([text] + extra)


def _summary(result):
    system = (result.get("system") or {}).get("name") or "the system"
    if result.get("status") == "missing":
        names = [a["input"] for a in result.get("asked") or []]
        return ("The sprinkler system %s is read and on the Heron Companion's Sprinkler panel. "
                "Nothing is solved yet - %d thing(s) to answer there: %s%s. Ask the modeller; "
                "never fill a criterion in." % (system, len(names), ", ".join(names[:6]),
                                                 " and more" if len(names) > 6 else ""))
    if result.get("status") != "ok":
        return ("The sprinkler system %s was not solved: %s" % (
            system, "; ".join((result.get("refused") or ["no reason was given"])[:3])))
    d = result["answer"]["data"]
    lines = ["Sprinkler system %s, %d heads in the remote area: demand %s at %s at the "
             "source %s; the governing head is %s." % (
                 system, len(result["inputs"]["operating"]), FIRE.flow_text(d["total_lpm"]),
                 FIRE.pressure_text(d["supply_bar"]), d["source"], d["governing"])]
    if d.get("supply"):
        margin = d["supply"]["at_demand_bar"] - d["supply_bar"]
        lines.append("The flow test gives %s at the demand - a margin of %s." % (
            FIRE.pressure_text(d["supply"]["at_demand_bar"]), FIRE.pressure_text(margin)))
    else:
        lines.append("No flow test was given, so the supply is not checked.")
    fails = [t for lv, t in result["answer"].get("checks") or [] if lv == "FAIL"]
    if fails:
        lines.append("FAIL: %s" % "; ".join(fails[:3]))
    lines.append("The tables and the 3D view are on the Companion; the network is %s." % (
        "confirmed" if result.get("network_confirmed") else "NOT yet confirmed - the sheet "
        "stays DRAFT until the modeller presses 'The network is right'"))
    return " ".join(lines)


# --- kept with the project ------------------------------------------------------

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
    return os.path.join(base, "projects", scope._safe_key(project_key) + ".sprinkler")


def save(project_key, result, replace=False, network=None):
    """Keep one run, and the network it was worked out from beside it once per fingerprint."""
    folder = _folder(project_key)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    if network is not None:
        n = TAKEOFF.read(network)
        kept = os.path.join(folder, "network-%s.json" % TAKEOFF.fingerprint(n))
        if not os.path.exists(kept):
            with io.open(kept, "w", encoding="utf-8") as fh:
                fh.write(json.dumps(n.raw))
    base = run_id = result["run_id"]
    path = os.path.join(folder, run_id + ".json")
    k = 1
    while os.path.exists(path) and not replace:
        k += 1
        run_id = "%s-%d" % (base, k)
        path = os.path.join(folder, run_id + ".json")
    result["run_id"] = run_id
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(json.dumps(result, indent=1))
    return path


def runs(project_key, system_id=None):
    """This project's runs, newest first - of one system when `system_id` is given."""
    try:
        folder = _folder(project_key)
    except ValueError:
        return []
    if not os.path.isdir(folder):
        return []
    out = []
    for name in os.listdir(folder):
        if not name.endswith(".json") or name.startswith("network-"):
            continue
        try:
            with io.open(os.path.join(folder, name), encoding="utf-8") as fh:
                d = json.loads(fh.read())
        except (OSError, ValueError):
            continue
        if system_id is not None and str((d.get("system") or {}).get("id")) != str(system_id):
            continue
        out.append({"run_id": d.get("run_id"), "when": d.get("when"),
                    "system": (d.get("system") or {}).get("name"), "status": d.get("status")})
    out.sort(key=lambda r: (r["when"] or "", r["run_id"] or ""), reverse=True)
    return out


def load(project_key, run_id=None, system_id=None):
    """One kept run - the newest (of one system, when named) if no run_id - or None."""
    try:
        folder = _folder(project_key)
    except ValueError:
        return None
    if run_id is None:
        kept = runs(project_key, system_id)
        if not kept:
            return None
        run_id = kept[0]["run_id"]
    if not re.match(r"^[0-9]{8}-[0-9]{6}(-[0-9]+)?$", str(run_id)):
        return None
    path = os.path.join(folder, "%s.json" % run_id)
    if not os.path.isfile(path):
        return None
    with io.open(path, encoding="utf-8") as fh:
        return json.loads(fh.read())


def carried(last, inputs, heads=None):
    """The inputs, with what the last run of this system was told filled in where this call
    said nothing - so nothing is asked twice. A value this call gives as nothing CLEARS the
    kept one, so a field blanked on the page is asked again rather than coming back. The
    remote area is carried only for the heads still in the system (`heads`, the head ids
    the network now holds)."""
    raw = inputs
    if isinstance(raw, str):
        try:
            raw = json.loads(raw) if raw.strip() else {}
        except ValueError:
            raw = {}
    raw = raw if isinstance(raw, dict) else {}
    inputs = normalise(inputs)
    before = (last or {}).get("inputs") or {}
    for part in ("criteria", "k", "fittings"):
        blank = set(str(k) for k, v in (raw.get(part) or {}).items()
                    if _value(v) is None or _value(v) == "")
        for k, v in (before.get(part) or {}).items():
            if k not in blank:
                inputs[part].setdefault(k, v)
    if not inputs.get("source") and before.get("source") and \
            before.get("source_label") != "model" and "source" not in raw:
        inputs["source"] = before["source"]
    if not inputs.get("operating") and before.get("operating") and "operating" not in raw:
        keep = None if heads is None else set(str(x) for x in heads)
        inputs["operating"] = [h for h in before["operating"] if keep is None or str(h) in keep]
    for k, v in (before.get("standards") or {}).items():
        inputs["standards"].setdefault(k, v)
    for part in ("spacing", "water"):
        inputs[part] = _carry(before.get(part) or {}, inputs.get(part) or {},
                              raw.get(part) if isinstance(raw.get(part), dict) else {})
    return inputs


def _carry(before, now, raw):
    """`now` with what `before` held filled in where this call said nothing - a key this
    call gave as nothing (in `raw`) clears it, at any depth."""
    out = dict(now)
    for k, v in before.items():
        said = raw.get(k, None) if isinstance(raw, dict) else None
        # A map of further inputs is not a blank; only a value given as nothing is.
        blank = said is None or said == "" or (
            isinstance(said, dict) and "value" in said and said["value"] in (None, ""))
        if k in raw and blank:
            out.pop(k, None)
            continue
        if isinstance(v, dict):
            inner = _carry(v, out.get(k) if isinstance(out.get(k), dict) else {},
                           said if isinstance(said, dict) else {})
            if inner:
                out[k] = inner
        elif k not in out:
            out[k] = v
    return out
