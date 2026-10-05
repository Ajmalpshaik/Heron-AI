# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
What the Companion's Sprinkler panel draws in 3D - docs/46 section 7.

The SAME pipes and heads the hydraulic calculation solved: every pipe of the
system as a line between the points its connectors meet at, every sprinkler
as a dot, and - once a run has solved - each pipe's flow, velocity and loss
and each head's pressure, so the modeller can look at the system and check
the network before the sheet is final.

Everything that MEANS something is decided here: the colours, the legends,
what each mode shows, the lines shown on a click. The page only draws
(mcp/companion README rule 4). Model text is carried as data and the page
puts it on screen as text.

It adds no physics: every figure is the run's own (heron_fire's `hydraulic`
answer, its `data`). The only arithmetic is moving the drawing near the
origin and choosing a colour on a ramp.

Standard library only.
"""

import math

import heron_sprinkler_takeoff as TAKEOFF

FORMAT = 1
RAMP = ("#2b6cb0", "#63b3ed", "#f6e05e", "#ed8936", "#c53030")
SIZES = ("#9e9e9e", "#4f9fd1", "#3ba86b", "#e0a030", "#c0503a", "#8a5a44", "#6b46c1",
         "#2c7a7b")
OPERATING = (("on", "#c53030", "operating - in the remote area"),
             ("off", "#9fb3c8", "not operating"))
CHECKS = (("ok", "#5aa469", "within the limits given"),
          ("fail", "#d9534f", "above the velocity limit given"),
          ("none", "#cfcac0", "no limit given, or not solved"))
DRY = "#cfcac0"
#: Below this a pipe is dry - the solver's round-off on a branch with no open head.
WET_LPM = 1e-6
SOURCE = "#2d3748"


def _ramp(t):
    t = 0.0 if t != t else max(0.0, min(1.0, t))
    x = t * (len(RAMP) - 1)
    i = min(len(RAMP) - 2, int(math.floor(x)))
    f = x - i
    a = [int(RAMP[i][k:k + 2], 16) for k in (1, 3, 5)]
    b = [int(RAMP[i + 1][k:k + 2], 16) for k in (1, 3, 5)]
    return "#%02x%02x%02x" % tuple(int(round(a[k] + (b[k] - a[k]) * f)) for k in range(3))


def _ramp_legend(low, high, unit):
    steps = 4
    return [[_ramp(i / float(steps)), "%.3g %s" % (low + (high - low) * i / float(steps), unit)]
            for i in range(steps + 1)]


def _scale(v, low, high):
    if v is None:
        return DRY
    return _ramp((v - low) / ((high - low) or 1.0))


def _value(entry):
    return entry.get("value") if isinstance(entry, dict) else entry


def build(network, result=None):
    """
    {"format", "segments", "points", "modes", "levels", "source"} - the
    drawing of one network, with the run's figures when `result` solved.
    Points are metres, moved so the drawing sits near the origin.
    """
    n = TAKEOFF.read(network)
    records, pipes = TAKEOFF.graph(n)
    result = result or {}
    answer = result.get("answer") or {}
    data = (answer.get("data") or {}) if answer.get("status") == "ok" else {}
    solved = bool(data)
    operating = set(str(x) for x in (result.get("inputs") or {}).get("operating") or [])
    vmax = _value(((result.get("inputs") or {}).get("criteria") or {}).get("max_velocity_ms"))

    # The run's pipe figures, by the pipe segment they were solved for - the
    # solve's own row index into what it was given, so a pruned branch is
    # simply not there (it carried no water).
    by_pipe = {}
    if solved:
        given = result.get("pipes") or []
        for got in data.get("pipes") or []:
            i = got.get("index")
            if i is not None and 0 <= i < len(given):
                by_pipe[str(given[i].get("segment") or given[i].get("element"))] = got
    heads = data.get("heads") or {}
    pressures = data.get("pressures") or {}
    head_node = TAKEOFF.head_nodes(n)

    every = [r["at"] for r in records.values() if r.get("at")]
    if every:
        centre = [sum(p[i] for p in every) / len(every) for i in range(3)]
        centre[2] = min(p[2] for p in every)
    else:
        centre = [0.0, 0.0, 0.0]

    def moved(p):
        return [round(p[i] - centre[i], 3) for i in range(3)] if p else None

    sizes = sorted(set(TAKEOFF._size_key(seg["e"]) or "size not read" for seg in pipes),
                   key=lambda s: (len(s), s))
    size_colour = dict((s, SIZES[i % len(SIZES)]) for i, s in enumerate(sizes))
    flows = [abs(v.get("q_lpm") or 0.0) for v in by_pipe.values()]
    speeds = [v.get("v_ms") or 0.0 for v in by_pipe.values()]
    head_p = [h.get("p_bar") for h in heads.values() if h.get("p_bar") is not None]
    q_hi = max(flows) if flows else 1.0
    v_hi = max(speeds + ([vmax] if vmax else [])) if speeds else 1.0
    p_lo = min(head_p) if head_p else 0.0
    p_hi = max(head_p) if head_p else 1.0

    segments = []
    for seg in pipes:
        e, a, b = seg["e"], seg["a"], seg["b"]
        got = by_pipe.get(seg["id"])
        size = TAKEOFF._size_key(e) or "size not read"
        colour = {"size": size_colour[size]}
        info = [["Pipe", str(e["id"])], ["Type", e.get("type") or "-"], ["Size", size],
                ["Bore", "%.1f mm" % (float(e["inner_diameter_m"]) * 1000.0)
                 if e.get("inner_diameter_m") else "not in the model"],
                ["Length", "%.2f m" % float(e["length_m"]) if e.get("length_m") else "-"],
                ["Level", e.get("level") or "-"]]
        values = {"size": size}
        if solved:
            # A dry branch solves to a flow of round-off size, not zero.
            # A dry branch solves to a flow of round-off size, or was left out
            # of the solve: either way it carries no water.
            q = abs(got.get("q_lpm") or 0.0) if got else 0.0
            q = q if q > WET_LPM else 0.0
            v = (got.get("v_ms") or 0.0) if q else 0.0
            colour["flow"] = _scale(q, 0.0, q_hi) if q else DRY
            colour["velocity"] = _scale(v, 0.0, v_hi) if v else DRY
            if vmax and q:
                colour["checks"] = CHECKS[1][1] if v > float(vmax) + 1e-12 else CHECKS[0][1]
            else:
                colour["checks"] = CHECKS[2][1]
            colour["operating"] = OPERATING[0][1] if q else OPERATING[1][1]
            if got:
                values.update({"q_lpm": got.get("q_lpm"), "v_ms": v,
                               "loss_bar": got.get("loss_bar")})
                info += [["Flow", "%.1f L/min" % q], ["Velocity", "%.2f m/s" % v],
                         ["Length solved", "%.2f m" % (got.get("length_m") or 0.0)],
                         ["Fittings", "%.2f m equivalent" % (got.get("eq_m") or 0.0)],
                         ["Loss", "%.4f bar" % (got.get("loss_bar") or 0.0)]]
        segments.append({"id": seg["id"], "element": str(e["id"]),
                         "a": moved(records[a]["at"]),
                         "b": moved(records[b]["at"]), "values": values, "colour": colour,
                         "info": info, "level": e.get("level")})

    points = []
    for e in n.elements:
        if e.get("kind") != "sprinkler":
            continue
        nid = head_node.get(str(e["id"]))
        rec = records.get(nid) if nid else None
        on = str(e["id"]) in operating
        colour = {"size": "#2d3748", "operating": OPERATING[0][1] if on else OPERATING[1][1]}
        info = [["Sprinkler", str(e["id"])], ["Type", ": ".join(
            x for x in (e.get("family"), e.get("type")) if x) or "-"],
            ["Level", e.get("level") or "-"], ["Space", e.get("space") or "-"],
            ["Remote area", "yes" if on else "no"]]
        values = {"operating": on}
        if solved:
            h = heads.get(nid) if nid else None
            p = h.get("p_bar") if h else (pressures.get(nid) if nid else None)
            colour["pressure"] = _scale(p, p_lo, p_hi) if h else DRY
            colour["flow"] = colour["velocity"] = colour["checks"] = colour["operating"]
            if h:
                values.update({"q_lpm": h.get("q_lpm"), "p_bar": h.get("p_bar")})
                info += [["Flow", "%.1f L/min" % h["q_lpm"]],
                         ["Pressure", "%.3f bar" % h["p_bar"]],
                         ["Needs", "%.1f L/min at %.3f bar" % (h["q_req_lpm"], h["p_req_bar"])]]
            if nid == data.get("governing"):
                info.append(["Governs", "this head runs at exactly what it needs"])
        points.append({"id": str(e["id"]), "kind": "sprinkler",
                       "at": moved(rec["at"]) if rec else None, "values": values,
                       "colour": colour, "info": info, "level": e.get("level")})
    source = result.get("source")
    src = records.get(source) if source else None

    modes = [{"key": "size", "label": "Pipe size",
              "legend": [[size_colour[s], s] for s in sizes]},
             {"key": "operating", "label": "Remote area",
              "legend": [[c, t] for _k, c, t in OPERATING]}]
    if solved:
        modes += [{"key": "flow", "label": "Flow", "legend": _ramp_legend(0.0, q_hi, "L/min")},
                  {"key": "velocity", "label": "Velocity",
                   "legend": _ramp_legend(0.0, v_hi, "m/s")},
                  {"key": "pressure", "label": "Pressure at the heads",
                   "legend": _ramp_legend(p_lo, p_hi, "bar")},
                  {"key": "checks", "label": "Checks",
                   "legend": [[c, t] for _k, c, t in CHECKS]}]
    levels = sorted(set(x.get("level") for x in segments + points if x.get("level")))
    return {"format": FORMAT, "segments": segments, "points": points, "modes": modes,
            "levels": levels, "dry": DRY,
            "source": {"at": moved(src["at"]), "colour": SOURCE, "id": source}
            if src and src.get("at") else None}
