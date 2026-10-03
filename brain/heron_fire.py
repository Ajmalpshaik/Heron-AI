# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Fire protection design, worked out before it is modelled - hazard classes,
sprinkler spacing and counts, design areas, pipe schedules (above and below a
ceiling too), hydraulics, water supply, storage, pumps, standpipes,
extinguishers, detectors - from the modeller's own design criteria.

    python brain/heron_fire.py                       every calculation, and what each needs
    python brain/heron_fire.py pipe_schedule '{"hazard": "light", ...}'
    python brain/heron_fire.py reference pipe_schedule_light

docs/42-fire-protection-design.md is the reading of NFPA 13, 14, 20, 72 and 10,
of BS EN 12845 and FM Global's data sheet 3-26, of BS 5839-1, BS 5306 and BS
9990, and of what is known about Qatar and the UAE that this file was written
from, and says what each calculation is and is not.

WHY IT EXISTS
-------------
Heron could already MODEL a sprinkler system - read a room, place heads at
points, find the beam over one, report what each head covers - and could not
say how many heads a room needs, how far apart they may be, what size a pipe
feeding ten of them must be, or what pressure the riser needs. The owner,
2026-10-02: *"same like that we need to do the firefighting also ... sprinkler
spacing minimum maximum ... number of sprinklers ... one sprinkler this much
pipe size ... combined sprinkler above and below ceiling"*. This is that part,
built the way the HVAC engine is (heron_hvac.py, docs/41).

IT SUPPLIES NO DESIGN VALUE - D-33 - AND WHERE THE LINE FALLS IN FIRE
-------------------------------------------------------------------
A design CRITERION is an input, every time: a hazard class, a density, a
design area, a spacing or area limit, a minimum pressure, a C factor, a hose
allowance, a duration, a margin. A calculation missing one computes nothing
and says what to ask for, with the figure NFPA lists for it OFFERED beside the
question - offered, never applied. The hazard class above all: Heron shows
what NFPA 13's annex lists beside an occupancy, and the class stays the
engineer's and the authority's.

A FACT the modeller names is looked up, as heron_hvac.py looks up a named
material's roughness: a steel pipe's bore by its schedule. And a TABLE the
modeller asks about is answered from that table and labelled as its answer -
"what size pipe feeds ten sprinklers by the light hazard schedule", "how high
may the deflector sit this far from the beam" - because the question asked
for the table's figure, not for Heron's. docs/42 s3 draws this line and F43
puts it to the owner.

A factor that would REDUCE a demand - a quick-response area reduction - is
never applied unless given. A factor that INCREASES one - a dry system's
larger design area - is ASKED, not skipped: leaving it out would err small,
and a sprinkler system that errs small is the failure this whole discipline
exists to prevent.

A FIGURE NOBODY COULD CHECK IS NOT HERE
---------------------------------------
This was written where the standards' pages could not be opened - only
search extracts of them, and the owner's own firefighting file. A table row held
here was confirmed by at least one of those, and docs/42 s10 says which; a row
that could only be remembered - most of the fittings chart, a handful of pipe
schedule cells, FM Global's spacing - is left out and ASKED for, never filled in
from memory. A cell left out of a table says so when a question reaches it.

ONE STANDARD AT A TIME
----------------------
A project's sprinklers follow ONE standard - an edition of NFPA 13, BS EN
12845, or FM Global's data sheets - asked once and kept (D-111). Its classes are
the ones read and its figures the ones offered; another standard's class is
refused rather than read across, because EN 12845's OH1 is not NFPA 13's.
Until the standard is known, NFPA 13's are offered and every answer says so.

IT READS NO MODEL AND CHANGES NOTHING
-------------------------------------
Pure arithmetic over what it is handed: no Revit, no network, no file, nothing
beyond Python itself and the HVAC engine whose answer machinery it shares -
reading inputs, refusing, offering a cited figure, rendering - so every MEP
design answer has one shape. What puts a result INTO the model is a fragment -
PLACE_FAMILY_INSTANCES, SET_MEP_SIZE, CHECK_OBSTRUCTIONS - and an answer names
which. A project's governing standards are asked once and kept for it (D-111),
by the caller; this file reads no store.

IT IS A DESIGN AID, AND FIRE IS LIFE SAFETY
-------------------------------------------
Every answer ends saying the fire consultant approves every value and the
authority having jurisdiction - QCDD on a Qatar project - approves the design.
The hydraulic solver is exact for the method it uses (Hazen-Williams, q = K
sqrt(p), total pressure); it is not a listed hydraulic program, and docs/42 s14
says what comparing it against one would take.
"""

import collections
import math
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_hvac as BASE                                     # noqa: E402

# The answer machinery is the HVAC engine's, shared rather than copied.
Refused = BASE.Refused
Answer = BASE.Answer
parse_inputs = BASE.parse_inputs
_g = BASE._g
_f = BASE._f


# ---------------------------------------------------------------------------
# Exact conversions. Definitions, not measurements - never "improve" them.
# ---------------------------------------------------------------------------

LPM_PER_GPM = 3.785411784                       # US gallon, exact
PA_PER_PSI = 6894.757293168361                  # 0.45359237 kg x 9.80665 / 0.0254^2
PSI_PER_BAR = 1.0e5 / PA_PER_PSI                # 14.5037738...
M_PER_FT = 0.3048
MM_PER_IN = 25.4
M2_PER_FT2 = M_PER_FT * M_PER_FT                # 0.09290304
MM_MIN_PER_GPM_FT2 = LPM_PER_GPM / M2_PER_FT2   # 40.7458...: 1 gpm/ft2 in L/min.m2
K_METRIC_PER_US = LPM_PER_GPM * math.sqrt(PSI_PER_BAR)   # 14.4163...
# Elevation: NFPA 13's 0.433 psi per foot of water, converted exactly. It is
# water's own weight (62.35 lb/ft3 over 144 in2), and NFPA's worked figures
# use it - docs/42 s10.
BAR_PER_M = 0.433 / PSI_PER_BAR / M_PER_FT      # 0.0979486... bar per metre
# Hazen-Williams as NFPA 13 prints it in SI: bar per metre for L/min and mm.
# It is the general Hazen-Williams equation for water in these units, and
# tests/test_fire.py derives it from that equation rather than trusting it.
HW_SI = 6.05e5
HW_US = 4.52
HW_EXPONENT = 1.85
HW_DIAMETER_EXPONENT = 4.87

DISCLAIMER = ("A design aid from published methods, by Heron's fire protection "
              "engine - DRAFT, and no engineer has signed it off. The fire "
              "consultant approves every design value and the authority having "
              "jurisdiction approves the design - on a Qatar project final approval "
              "is QCDD's: confirm with the fire consultant before construction "
              "(DISCLAIMER.md).")

NOT_LISTED = ("NOT A LISTED HYDRAULIC PROGRAM: the network is solved exactly for "
              "Hazen-Williams friction, q = K.sqrt(p) at each sprinkler and the "
              "total-pressure method; it has not been compared against a program "
              "an authority accepts - submit that program's calculation, and use "
              "this one to check it.")


# ---------------------------------------------------------------------------
# Number formatting - what a modeller reads. Metric first, US in brackets,
# because Qatar draws in millimetres and NFPA prints in feet.
# ---------------------------------------------------------------------------

def flow_text(lpm):
    return "%s L/min  (%s L/s, %s US gpm)" % (_f(lpm, 1), _f(lpm / 60.0, 2),
                                             _f(lpm / LPM_PER_GPM, 1))


def pressure_text(bar):
    return "%s bar  (%s kPa, %s psi)" % (_f(bar, 3), _f(bar * 100.0, 1),
                                         _f(bar * PSI_PER_BAR, 2))


def density_text(mm_min):
    return "%s mm/min  (%s gpm/ft2)" % (_f(mm_min, 2), _f(mm_min / MM_MIN_PER_GPM_FT2, 3))


def area_text(m2):
    return "%s m2  (%s ft2)" % (_f(m2, 2), _f(m2 / M2_PER_FT2, 0))


def length_text(m):
    return "%s m  (%s ft)" % (_f(m, 3), _f(m / M_PER_FT, 2))


def volume_text(m3):
    return "%s m3  (%s US gal)" % (_f(m3, 2), _f(m3 * 1000.0 / LPM_PER_GPM, 0))


def k_text(k_metric):
    return "K %s L/min.bar^0.5  (K %s gpm/psi^0.5)" % (_f(k_metric, 1),
                                                       _f(k_metric / K_METRIC_PER_US, 2))


# ---------------------------------------------------------------------------
# Reading the inputs fire needs that the HVAC engine's reader does not have.
# Each takes a view (Inputs) and reads one quantity given in exactly one unit.
# ---------------------------------------------------------------------------

def _view(thing):
    return BASE._view(thing)


def _absent(view, key, unit, why, required, reference):
    if required:
        view.answer.need(view.name(key), unit, why, reference)
    elif not view.prefix:
        view.answer.optional_input(key, unit, why)
    return None


def _one_of(view, prefix, units, why, required, reference, low, high, label):
    """One quantity given in exactly one of `units` ((suffix, factor, unit name), ...)."""
    view = _view(view)
    given = []
    for suffix, factor, name in units:
        key = "%s_%s" % (prefix, suffix) if prefix else suffix
        raw = view.raw(key)
        if raw is not None and raw != "":
            given.append((key, raw, factor, name))
    if not given:
        first = "%s_%s" % (prefix, units[0][0]) if prefix else units[0][0]
        others = ", ".join(("%s_%s" % (prefix, u[0]) if prefix else u[0]) for u in units[1:])
        return _absent(view, first, "%s (or %s)" % (units[0][2], others) if others
                       else units[0][2], why, required, reference)
    if len(given) > 1:
        view.answer.refuse("give %s once - %s were all given, and preferring one would "
                           "hide which was meant"
                           % (view.name(prefix or label), ", ".join(view.name(g[0])
                                                                     for g in given)))
        return None
    key, raw, factor, name = given[0]
    try:
        value = BASE._to_number(raw, view.name(key))
        BASE._within(value, view.name(key), name, None, None, False)
    except Refused as why_not:
        view.answer.refuse(str(why_not))
        return None
    out = value * factor
    if low is not None and out < low - 1e-12:
        view.answer.refuse("%s %s %s is below what Heron will calculate from as %s"
                           % (view.name(key), _g(value), name, label))
        return None
    if high is not None and out > high + 1e-12:
        view.answer.refuse("%s %s %s is above what Heron will calculate from as %s - "
                           "check the unit" % (view.name(key), _g(value), name, label))
        return None
    return out


_FLOW = (("lpm", 1.0, "L/min"), ("ls", 60.0, "L/s"), ("m3h", 1000.0 / 60.0, "m3/h"),
         ("gpm", LPM_PER_GPM, "US gpm"))
_PRESSURE = (("bar", 1.0, "bar"), ("kpa", 0.01, "kPa"), ("psi", 1.0 / PSI_PER_BAR, "psi"))
_DENSITY = (("mm_min", 1.0, "mm/min (L/min per m2)"),
            ("gpm_ft2", MM_MIN_PER_GPM_FT2, "gpm/ft2"))
_AREA = (("m2", 1.0, "m2"), ("ft2", M2_PER_FT2, "ft2"))
_K = (("lpm_bar", 1.0, "L/min per bar^0.5"), ("gpm_psi", K_METRIC_PER_US, "gpm per psi^0.5"))


def flow_lpm(view, why, prefix="flow", required=True, reference=None, high=1.0e6):
    """A water flow in L/min - given in L/min, L/s, m3/h or US gpm, exactly once."""
    return _one_of(view, prefix, _FLOW, why, required, reference, 0.0, high, "a flow")


def pressure_bar(view, why, prefix="pressure", required=True, reference=None,
                 low=0.0, high=100.0):
    """A gauge pressure in bar - given in bar, kPa or psi, exactly once."""
    return _one_of(view, prefix, _PRESSURE, why, required, reference, low, high,
                   "a pressure")


def density_mm_min(view, why, prefix="density", required=True, reference=None):
    """A design density in mm/min (L/min per m2) - or gpm/ft2."""
    return _one_of(view, prefix, _DENSITY, why, required, reference, 0.5, 100.0,
                   "a design density")


def area_m2(view, why, prefix, required=True, reference=None, low=0.01, high=1.0e6):
    """An area in m2 - or ft2."""
    return _one_of(view, prefix, _AREA, why, required, reference, low, high, "an area")


def k_factor(view, why, prefix="k", required=True, reference=None):
    """A sprinkler K-factor in L/min.bar^0.5 - or gpm/psi^0.5, NFPA's nominal K."""
    if reference is None:
        reference = ("the sprinkler's LISTED K, from its data sheet - NFPA 13's nominal K list: "
                     "`reference k_factors`")
    return _one_of(view, prefix, _K, why, required, reference, 10.0, 2000.0,
                   "a K-factor")


# ---------------------------------------------------------------------------
# Hydraulics - NFPA 13's method: Hazen-Williams friction, q = K sqrt(p) at a
# sprinkler, elevation at 0.433 psi per foot, the total-pressure method.
# ---------------------------------------------------------------------------

def hw_friction(flow_lpm, bore_mm, c):
    """Hazen-Williams friction in bar per metre - NFPA 13's SI form."""
    return HW_SI * flow_lpm ** HW_EXPONENT / (c ** HW_EXPONENT * bore_mm ** HW_DIAMETER_EXPONENT)


def velocity_ms(flow_lpm, bore_mm):
    return flow_lpm / 60000.0 / (math.pi * (bore_mm / 1000.0) ** 2 / 4.0)


def head_flow(k_metric, p_bar):
    """A sprinkler's discharge in L/min at a pressure: q = K sqrt(p)."""
    return k_metric * math.sqrt(p_bar) if p_bar > 0 else 0.0


def head_pressure(k_metric, q_lpm):
    """The pressure a sprinkler needs to discharge q: p = (q / K)^2."""
    return (q_lpm / k_metric) ** 2


# ---------------------------------------------------------------------------
# The network solver. Node pressures by Newton's method, for a tree or a grid:
# every pipe obeys Hazen-Williams, every operating sprinkler q = K sqrt(p), and
# flow balances at every node. The source pressure is found so that the
# GOVERNING sprinkler - the one that runs out of pressure first - gets exactly
# what it needs and every other one at least that.
# ---------------------------------------------------------------------------

_Q0 = 0.01      # L/min - below this a pipe's relation is taken as linear, so the
                # slope stays finite at zero flow; 0.01 L/min is far below what
                # any sprinkler figure is read to
_P0 = 1.0e-6    # bar - the same for a sprinkler near zero pressure


class Unsolved(ValueError):
    """The network could not be solved; carries the sentence that says why."""


def _pipe(dh, r):
    """Flow a->b in L/min for a head difference dh (bar), and its slope dQ/d(dh)."""
    h0 = r * _Q0 ** HW_EXPONENT
    if abs(dh) <= h0:
        return dh * _Q0 / h0, _Q0 / h0
    q = (abs(dh) / r) ** (1.0 / HW_EXPONENT)
    return (q if dh > 0 else -q), q / (HW_EXPONENT * abs(dh))


def _head(p, k):
    """Discharge of a sprinkler at p, and its slope dq/dp."""
    if p <= 0.0:
        return 0.0, 0.0
    if p < _P0:
        slope = k / math.sqrt(_P0)
        return slope * p, slope
    root = math.sqrt(p)
    return k * root, k / (2.0 * root)


def _gauss(a, b):
    """Solve a.x = b by Gaussian elimination with partial pivoting; skips zeros."""
    n = len(b)
    m = [list(a[i]) + [b[i]] for i in range(n)]
    for col in range(n):
        piv = max(range(col, n), key=lambda r: abs(m[r][col]))
        if abs(m[piv][col]) < 1e-300:
            raise Unsolved("the network's equations are singular - a part of it is "
                           "connected to nothing that carries flow")
        if piv != col:
            m[col], m[piv] = m[piv], m[col]
        top = m[col]
        inv = 1.0 / top[col]
        for r in range(col + 1, n):
            factor = m[r][col]
            if factor != 0.0:
                factor *= inv
                row = m[r]
                for c in range(col, n + 1):
                    if top[c] != 0.0:
                        row[c] -= factor * top[c]
    x = [0.0] * n
    for r in range(n - 1, -1, -1):
        s = m[r][n]
        row = m[r]
        for c in range(r + 1, n):
            if row[c] != 0.0:
                s -= row[c] * x[c]
        x[r] = s / row[r]
    return x


def solve_network(nodes, pipes, source):
    """
    nodes: {id: {"z": m, "k": K metric or None, "p_req": bar or None}}
    pipes: [{"a": id, "b": id, "r": bar per (L/min)^1.85}]
    source: the node whose pressure is found.

    Returns (pressures, flows, governing): node pressures in bar, each pipe's
    flow a->b in L/min, and the sprinkler that governs. Raises Unsolved.
    """
    ids = list(nodes)
    index = dict((n, i) for i, n in enumerate(ids))
    heads = [n for n in ids if nodes[n].get("k") and nodes[n].get("p_req") is not None]
    if not heads:
        raise Unsolved("no operating sprinkler - mark at least one node with its K-factor")
    touching = dict((n, []) for n in ids)
    for j, p in enumerate(pipes):
        touching[p["a"]].append(j)
        touching[p["b"]].append(j)
    seen, todo = {source}, [source]
    while todo:
        here = todo.pop()
        for j in touching[here]:
            other = pipes[j]["b"] if pipes[j]["a"] == here else pipes[j]["a"]
            if other not in seen:
                seen.add(other)
                todo.append(other)
    cut_off = [n for n in ids if n not in seen]
    if cut_off:
        raise Unsolved("these nodes are not connected to the source: %s"
                       % ", ".join(cut_off[:12]))

    zs = nodes[source]["z"]
    top = max(nodes[h]["p_req"] + BAR_PER_M * (nodes[h]["z"] - zs) for h in heads)
    x = [top + 0.5 - BAR_PER_M * (nodes[n]["z"] - zs) for n in ids]

    def residual(x, governing):
        f = [0.0] * len(ids)
        jac = [[0.0] * len(ids) for _ in ids]
        for p in pipes:
            a, b = index[p["a"]], index[p["b"]]
            dh = x[a] - x[b] + BAR_PER_M * (nodes[p["a"]]["z"] - nodes[p["b"]]["z"])
            q, slope = _pipe(dh, p["r"])
            f[b] += q
            f[a] -= q
            jac[b][a] += slope
            jac[b][b] -= slope
            jac[a][a] -= slope
            jac[a][b] += slope
        for h in heads:
            i = index[h]
            q, slope = _head(x[i], nodes[h]["k"])
            f[i] -= q
            jac[i][i] -= slope
        s, g = index[source], index[governing]
        f[s] = x[g] - nodes[governing]["p_req"]
        jac[s] = [0.0] * len(ids)
        jac[s][g] = 1.0
        return f, jac

    def norm(f):
        return max(abs(v) for v in f)

    governing = max(heads, key=lambda h: nodes[h]["p_req"] + BAR_PER_M * (nodes[h]["z"] - zs))
    for _round in range(len(heads) + 1):
        for _step in range(200):
            f, jac = residual(x, governing)
            size = norm(f)
            if size < 1e-9:
                break
            dx = _gauss(jac, [-v for v in f])
            t = 1.0
            while t > 1e-6:
                trial = [xi + t * di for xi, di in zip(x, dx)]
                if norm(residual(trial, governing)[0]) < size:
                    break
                t *= 0.5
            x = trial
            if max(abs(t * d) for d in dx) < 1e-13:
                break
        else:
            raise Unsolved("the network did not converge in 200 steps - check that every "
                           "pipe has a length and a bore, and every sprinkler a K-factor")
        if norm(residual(x, governing)[0]) > 1e-6:
            raise Unsolved("the network did not balance - check for a pipe with no length "
                           "or a loop of pipes with no resistance")
        short = min(heads, key=lambda h: x[index[h]] - nodes[h]["p_req"])
        if x[index[short]] - nodes[short]["p_req"] >= -1e-9:
            break
        governing = short
    else:
        raise Unsolved("no sprinkler could be found that governs every other - the "
                       "network is not one Heron can solve")
    pressures = dict((n, x[index[n]]) for n in ids)
    flows = []
    for p in pipes:
        dh = (pressures[p["a"]] - pressures[p["b"]]
              + BAR_PER_M * (nodes[p["a"]]["z"] - nodes[p["b"]]["z"]))
        flows.append(_pipe(dh, p["r"])[0])
    return pressures, flows, governing


# ---------------------------------------------------------------------------
# Plan geometry - points in mm, a room outline as a polygon of [x, y] in mm.
# ---------------------------------------------------------------------------

def read_outline(view, why, key="outline_mm", required=True):
    """A room outline: a list of at least three [x, y] points in mm, not closed twice."""
    view = _view(view)
    raw = view.raw(key)
    if raw is None or raw == "" or raw == []:
        return _absent(view, key, "list of [x, y] in mm", why, required, None)
    if not isinstance(raw, (list, tuple)) or len(raw) < 3:
        view.answer.refuse("%s must list at least three [x, y] points" % view.name(key))
        return None
    pts = []
    for i, p in enumerate(raw):
        if isinstance(p, dict):
            p = (p.get("x"), p.get("y"))
        if not isinstance(p, (list, tuple)) or len(p) != 2:
            view.answer.refuse("%s[%d] must be [x, y]" % (view.name(key), i))
            return None
        try:
            pts.append((BASE._to_number(p[0], "x"), BASE._to_number(p[1], "y")))
        except Refused as why_not:
            view.answer.refuse("%s[%d]: %s" % (view.name(key), i, why_not))
            return None
    if len(pts) > 3 and _close(pts[0], pts[-1]):
        pts = pts[:-1]
    if abs(_polygon_area(pts)) < 1.0:
        view.answer.refuse("%s encloses no area" % view.name(key))
        return None
    return pts


def read_points(view, why, key, required=True):
    """A list of plan points: [x, y] or {"id", "x", "y"}, in mm. Returns [(id, x, y)]."""
    view = _view(view)
    raw = view.raw(key)
    if raw is None or raw == "" or raw == []:
        return _absent(view, key, "list of [x, y] or {id, x, y} in mm", why, required, None)
    if not isinstance(raw, (list, tuple)):
        view.answer.refuse("%s must be a list of points" % view.name(key))
        return None
    out = []
    for i, p in enumerate(raw):
        label = str(i + 1)
        if isinstance(p, dict):
            label = str(p.get("id", label))
            p = (p.get("x"), p.get("y"))
        if not isinstance(p, (list, tuple)) or len(p) != 2:
            view.answer.refuse("%s[%d] must be [x, y] or {id, x, y}" % (view.name(key), i))
            return None
        try:
            out.append((label, BASE._to_number(p[0], "x"), BASE._to_number(p[1], "y")))
        except Refused as why_not:
            view.answer.refuse("%s[%d]: %s" % (view.name(key), i, why_not))
            return None
    if len(set(o[0] for o in out)) != len(out):
        view.answer.refuse("%s names the same id twice" % view.name(key))
        return None
    return out


def _close(a, b, tol=1e-6):
    return abs(a[0] - b[0]) < tol and abs(a[1] - b[1]) < tol


def _polygon_area(pts):
    s = 0.0
    for (x0, y0), (x1, y1) in zip(pts, pts[1:] + pts[:1]):
        s += x0 * y1 - x1 * y0
    return s / 2.0


def _edges(pts):
    return list(zip(pts, pts[1:] + pts[:1]))


def inside(pts, x, y):
    """True if (x, y) is inside the polygon - crossing number, edges count as inside."""
    if _on_edge(pts, x, y):
        return True
    hit = False
    for (x0, y0), (x1, y1) in _edges(pts):
        if (y0 > y) != (y1 > y):
            cross = x0 + (y - y0) * (x1 - x0) / (y1 - y0)
            if cross > x:
                hit = not hit
    return hit


def _on_edge(pts, x, y, tol=1e-6):
    return any(_segment_distance(x, y, a, b) < tol for a, b in _edges(pts))


def _segment_distance(x, y, a, b):
    (x0, y0), (x1, y1) = a, b
    dx, dy = x1 - x0, y1 - y0
    span = dx * dx + dy * dy
    t = 0.0 if span == 0 else max(0.0, min(1.0, ((x - x0) * dx + (y - y0) * dy) / span))
    return math.hypot(x - (x0 + t * dx), y - (y0 + t * dy))


def wall_distance(pts, x, y):
    """Distance in plan from a point to the nearest wall of the outline, mm."""
    return min(_segment_distance(x, y, a, b) for a, b in _edges(pts))


def ray_to_wall(pts, x, y, dx, dy):
    """Distance along (dx, dy) from a point to the first wall it meets, mm, or None."""
    best = None
    for (x0, y0), (x1, y1) in _edges(pts):
        ex, ey = x1 - x0, y1 - y0
        den = dx * ey - dy * ex
        if abs(den) < 1e-12:
            continue
        t = ((x0 - x) * ey - (y0 - y) * ex) / den
        u = ((x0 - x) * dy - (y0 - y) * dx) / den
        if t > 1e-9 and -1e-9 <= u <= 1.0 + 1e-9:
            best = t if best is None else min(best, t)
    return best


SAMPLES_MAX = 200000     # an outline is sampled at no more points than this
WORK_MAX = 2.0e7         # samples x devices - the distances one answer may measure


def sampling_step(pts, step, devices=1):
    """
    The step a sample of the outline is taken at - `step`, or coarser for a
    big outline or many devices, so one answer stays within a few seconds.
    """
    area = abs(_polygon_area(pts))
    most = min(SAMPLES_MAX, WORK_MAX / max(1, devices))
    return max(step, math.sqrt(area / most))


def _reach(x, y, points, k):
    """How far (x, y) is from its k-th nearest point - the nearest when k is 1."""
    if k == 1:
        return min(math.hypot(x - px, y - py) for _i, px, py in points)
    return sorted(math.hypot(x - px, y - py) for _i, px, py in points)[k - 1]


def farthest_point(pts, points, step, k=1):
    """
    The point of the outline farthest from every given point, sampled on a
    `step` mm grid: (distance mm, x, y, samples). With k above 1, the distance
    is to each sample's k-th nearest point - "every part reached by two hose
    reels" is k = 2. A sample, not a proof - a finer step finds a corner a
    coarse one steps over.
    """
    step = sampling_step(pts, step, len(points))
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    worst = (-1.0, None, None)
    samples = []
    x = min(xs) + step / 2.0
    while x < max(xs):
        y = min(ys) + step / 2.0
        while y < max(ys):
            if inside(pts, x, y):
                d = _reach(x, y, points, k)
                samples.append(d)
                if d > worst[0]:
                    worst = (d, x, y)
            y += step
        x += step
    for (x0, y0) in pts:                 # the corners themselves
        d = _reach(x0, y0, points, k)
        samples.append(d)
        if d > worst[0]:
            worst = (d, x0, y0)
    return worst[0], worst[1], worst[2], samples


# ---------------------------------------------------------------------------
# A rectangular grid - the fewest devices meeting every limit, then the
# squarest module. Shared by sprinklers and detectors.
# ---------------------------------------------------------------------------

def grid(length, width, smax=None, amax=None, wmax=None, smin=None, wmin=None,
         count=None, limit=400, rmax=None):
    """
    (rows, cols) for a room of length x width, each device centred in its
    module, or None. rows run along the width, cols along the length.

    smax: largest spacing either way; amax: largest module area; wmax: largest
    distance to a wall (half a module, centred); smin: smallest spacing between
    two devices; wmin: smallest distance to a wall; count: exactly so many;
    rmax: largest distance from any point to its device - half a module's
    diagonal, the farthest a centred grid leaves any point.
    """
    best = None
    for rows in range(1, limit + 1):
        for cols in range(1, limit + 1):
            n = rows * cols
            if count is not None and n != count:
                if n > count:
                    break
                continue
            sx, sy = length / cols, width / rows
            ok = True
            if smax is not None and (sx > smax + 1e-9 or sy > smax + 1e-9):
                ok = False
            if amax is not None and sx * sy > amax + 1e-9:
                ok = False
            if rmax is not None and math.hypot(sx, sy) / 2.0 > rmax + 1e-9:
                ok = False
            if wmax is not None and (sx / 2.0 > wmax + 1e-9 or sy / 2.0 > wmax + 1e-9):
                ok = False
            if smin is not None and ((cols > 1 and sx < smin - 1e-9)
                                     or (rows > 1 and sy < smin - 1e-9)):
                # Adding more along this row only brings them closer.
                break
            if wmin is not None and (sx / 2.0 < wmin - 1e-9 or sy / 2.0 < wmin - 1e-9):
                break
            if not ok:
                continue
            score = (n, abs(sx - sy))
            if best is None or score < best[0]:
                best = (score, rows, cols)
            break
    return None if best is None else (best[1], best[2])


def grid_points(length, width, rows, cols):
    """Centres of a rows x cols grid from the room corner, in mm."""
    sx, sy = length / cols, width / rows
    return [((i + 0.5) * sx * 1000.0, (j + 0.5) * sy * 1000.0)
            for j in range(rows) for i in range(cols)]


# ---------------------------------------------------------------------------
# The project's governing standards - asked ONCE per project and kept for it
# (D-111), exactly as the HVAC engine asks its own. Two questions: which
# standard governs the sprinklers - an edition of NFPA 13, BS EN 12845, or FM
# Global's data sheets, each with its own hazard classes and figures - and who
# approves the design (QCDD on a Qatar project, UAE Civil Defence under the UAE
# Fire and Life Safety Code; each authority's own tables govern where they
# differ). They never block a calculation; until they are known, the checks
# that need them say so. The caller keeps them (heron_designbasis.py) and
# hands them back as `recorded` - this file reads no store.
# ---------------------------------------------------------------------------

PROJECT_STANDARDS = collections.OrderedDict([
    ("sprinkler_standard",
     ("NFPA 13-<year>, EN 12845, FM Global or other",
      "which standard governs this project's sprinklers - an edition of NFPA 13, BS EN "
      "12845, or FM Global's data sheets; other if something else does")),
    ("fire_authority",
     ("QCDD, UAE Civil Defence, other or none",
      "who approves this project's fire protection design - QCDD on a Qatar project, UAE "
      "Civil Defence under the UAE Fire and Life Safety Code, other for another authority")),
])

# The editions this engine's tables were taken from.
HELD_NFPA13 = "NFPA 13-2022"
EN12845 = "EN 12845"
HELD_EN12845 = "EN 12845:2015"        # with A1:2019 - A2:2026, current since April 2026, not read
FM_GLOBAL = "FM Global"
OTHER_STANDARD = BASE.OTHER_STANDARD
NO_STANDARD = BASE.NO_STANDARD
QCDD = "QCDD"
UAE = "UAE Civil Defence"
_QCDD_WORDS = ("qcdd", "qcd", "qatar civil defence", "qatar civil defense",
               "civil defence", "civil defense", "gdcd",
               "general directorate of civil defence", "general directorate of civil defense")
_UAE_WORDS = ("uae civil defence", "uae civil defense", "uae", "dcd", "dubai civil defence",
              "dubai civil defense", "adcda", "abu dhabi civil defence",
              "abu dhabi civil defense", "uae fire and life safety code",
              "uae fire and life safety code of practice")
_FM_WORDS = ("fm", "fm global", "fmglobal", "fm global data sheets", "fm data sheets",
             "fm global property loss prevention data sheets", "fmds", "factory mutual")
# A national body's letters before EN 12845 - BS EN, I.S. EN, DIN EN and the rest.
_EN_BODIES = ("bs", "en", "i.s.", "is", "din", "uni", "nf", "sn", "une", "nbn", "ds", "sfs",
              "ss", "ns", "onorm", "pn", "csn", "nen", "cen")

# Each family's name, and the edition its figures here were read from.
FAMILIES = collections.OrderedDict([
    ("nfpa", ("NFPA 13", HELD_NFPA13)),
    ("en", ("BS EN 12845", "EN 12845:2015+A1:2019")),
    ("fm", ("FM Global", "FM Global data sheet 3-26 (2019 and 2020)")),
])


def _en12845(text):
    """'EN 12845', or 'EN 12845:<year>', for the ways a person writes it - or None."""
    text = text.replace("en12845", "en 12845")
    for mark in (":", "+", "/", "-", "(", ")", ","):
        text = text.replace(mark, " ")
    words = [w for w in text.split() if w not in _EN_BODIES]
    if not words or words[0] != "12845":
        return None
    rest = words[1:]
    if not rest:
        return EN12845
    if not (len(rest[0]) == 4 and rest[0].isdigit() and 2004 <= int(rest[0]) <= 2099):
        return None
    for word in rest[1:]:
        amendment = len(word) == 2 and word[0] == "a" and word[1].isdigit()
        year = len(word) == 4 and word.isdigit()
        if not (amendment or year):
            return None
    return "%s:%s" % (EN12845, rest[0])


def standard_value(name, raw):
    """One project standard as said, normalised - or Refused saying what is accepted."""
    unit = PROJECT_STANDARDS[name][0]
    text = " ".join(str(raw).strip().lower().replace("_", " ").split())
    if name == "sprinkler_standard":
        if text == OTHER_STANDARD:
            return OTHER_STANDARD
        year = BASE._edition_year(text, ("nfpa",), "13")
        if year is not None:
            return "NFPA 13-%d" % year
        en = _en12845(text)
        if en is not None:
            return en
        if text in _FM_WORDS:
            return FM_GLOBAL
        compact = text.replace(" ", "").replace("nfpa", "")
        if compact.startswith("13r") or compact.startswith("13d"):
            raise Refused("%s %r is a residential standard - Heron holds NFPA 13R's and 13D's "
                          "figures only as reference rows (`reference nfpa_residential`); say "
                          "other, and give each figure" % (name, raw))
    elif name == "fire_authority":
        if text in (OTHER_STANDARD, NO_STANDARD):
            return text
        plain = " ".join(text.replace("(", " ").replace(")", " ").split())
        if plain in _QCDD_WORDS:
            return QCDD
        if plain in _UAE_WORDS:
            return UAE
    raise Refused("%s must be %s - got %r" % (name, unit, raw))


def standard_text(value):
    return BASE.standard_text(value)


def family_of(standard):
    """'nfpa', 'en' or 'fm' for a project's sprinkler standard - None when unknown or other."""
    if not standard or standard == OTHER_STANDARD:
        return None
    if standard.startswith("NFPA 13-"):
        return "nfpa"
    if standard.startswith(EN12845):
        return "en"
    if standard == FM_GLOBAL:
        return "fm"
    return None


def figures_of(held):
    """The family whose figures an answer offers: the project's own, else NFPA 13's."""
    return family_of(held.get("sprinkler_standard")) or "nfpa"


def project_standards(a, names=None):
    """
    The project standards this answer needs - each from this request, else
    from the project's record, else None and asked once. A value given here wins
    over the record; the caller keeps it, the old one in the record's history.
    `names` limits it to the ones the calculation uses; the others are None and
    not asked, so a pump question is not asked which sprinkler standard governs.
    """
    out = collections.OrderedDict()
    for name, (unit, why) in PROJECT_STANDARDS.items():
        if names is not None and name not in names:
            out[name] = None
            continue
        raw = a.raw(name)
        if raw is not None and raw != "":
            try:
                out[name] = standard_value(name, raw)
            except Refused as why_not:
                a.refuse(str(why_not))
                out[name] = None
                continue
            a.standards[name] = {"value": out[name], "from": "request"}
            continue
        kept = a.recorded.get(name)
        value = None
        if isinstance(kept, dict) and "value" in kept:
            try:
                value = standard_value(name, kept["value"])
            except Refused:
                # Asked again, never repaired into the nearest thing it might mean.
                value = None
        if value is None:
            a.ask_once.append({"input": name, "unit": unit, "why": why})
        else:
            a.standards[name] = {"value": value, "from": "record",
                                 "recorded": kept.get("recorded")}
        out[name] = value
    return out


def _check_standard(a, standard, what, used="nfpa"):
    """
    The answer's line on which standard governs the sprinkler figures it used
    or offered - `used` is the family they were taken from.
    """
    name, held = FAMILIES[used]
    family = family_of(standard)
    if standard is None:
        a.check("WARN", "not checked: which standard governs this project's sprinklers is not "
                        "known yet - %s %s's" % (what, held))
    elif family is None:
        a.check("WARN", "this project's sprinklers are governed by something other than NFPA "
                        "13, EN 12845 or FM Global - %s %s's, a comparison and not the "
                        "project's requirement" % (what, held))
    elif family != used:
        a.check("WARN", "this project follows %s - %s %s's, a comparison and not the "
                        "project's requirement" % (standard, what, held))
    elif used == "nfpa" and standard != HELD_NFPA13:
        a.check("WARN", "this project follows %s and %s %s's - NFPA renumbers its "
                        "sections between editions, so confirm each figure in %s"
                % (standard, what, held, standard))
    elif used == "en" and standard == EN12845:
        a.check("WARN", "this project follows EN 12845 with no edition said - %s %s's; "
                        "A2:2026, current since April 2026, was not read" % (what, held))
    elif used == "en" and standard != HELD_EN12845:
        a.check("WARN", "this project follows %s and %s %s's - confirm each figure in %s"
                % (standard, what, held, standard))
    elif used == "fm":
        a.check("WARN", "FM Global's data sheets govern this project's sprinklers, and %s %s "
                        "as search extracts reproduce it - a data sheet is revised often, so "
                        "confirm against the current one" % (what, held))
    else:
        a.check("OK", "%s governs this project's sprinklers, and %s that edition's%s" % (
            standard, what, " (with A1:2019 - A2:2026, current since April 2026, was not "
            "read)" if used == "en" else ""))


def _check_authority(a, authority, governs):
    """What the approving authority changes - its own tables govern where they exist."""
    if authority == QCDD:
        a.check("WARN", "QCDD approves this design: where the Civil Defence Technical "
                        "Requirements Guide sets %s, its figure governs over the standard's, and "
                        "Heron holds only what search extracts reproduce of it (`reference "
                        "qatar`) - confirm with the fire consultant" % governs)
    elif authority == UAE:
        a.check("WARN", "UAE Civil Defence approves this design: where the UAE Fire and Life "
                        "Safety Code of Practice sets %s, its figure governs, and Heron holds "
                        "only what search extracts reproduce of it (`reference uae`) - "
                        "confirm with the fire consultant" % governs)
    elif authority is None:
        a.check("WARN", "not checked: who approves this design is not known yet - on a "
                        "Qatar project QCDD's own requirements govern %s" % governs)
    elif authority == OTHER_STANDARD:
        a.check("WARN", "another authority approves this design - its own requirements "
                        "for %s govern over the standard's" % governs)


# ---------------------------------------------------------------------------
# Reference tables - SHOWN when asked, OFFERED beside a missing input, and
# applied only where the modeller asked that table's question or named a fact
# in it (a schedule's bore). Each carries its citation and how far it was
# checked; docs/42 s10 says the same at length, and a row nobody could check is
# not here.
# ---------------------------------------------------------------------------

REFERENCES = collections.OrderedDict()

NOT_HELD = "not held"     # a cell that could not be checked - a question reaching it says so


def _ref(table, title, source, columns, rows, note=None):
    REFERENCES[table] = {"title": title, "source": source,
                         "columns": list(columns), "rows": [list(r) for r in rows],
                         "note": note}


def lookup(table, key):
    return BASE.reference_lookup(table, key, REFERENCES)


def offer(table, key, column, unit):
    return BASE.offer(table, key, column, unit, REFERENCES)


def offer_table(table):
    return BASE.offer_table(table, REFERENCES)


def _norm(text):
    return " ".join(str(text).strip().lower().replace("_", " ").replace("-", " ").split())


def _rule(table, key):
    """The value column of one rule row, as a sentence offering it - or None."""
    row = lookup(table, key)
    if row is None:
        return None
    return ("%s: %s (%s) - offer it to the modeller, do not assume it"
            % (REFERENCES[table]["source"], row[1], row[0]))


# --- nominal pipe sizes ------------------------------------------------------

# (inch label, DN, inches). The label is how NFPA's tables print a size.
NOMINALS = (("3/4", 20, 0.75), ("1", 25, 1.0), ("1 1/4", 32, 1.25), ("1 1/2", 40, 1.5),
            ("2", 50, 2.0), ("2 1/2", 65, 2.5), ("3", 80, 3.0), ("3 1/2", 90, 3.5),
            ("4", 100, 4.0), ("5", 125, 5.0), ("6", 150, 6.0), ("8", 200, 8.0),
            ("10", 250, 10.0), ("12", 300, 12.0))


def nominal_key(raw):
    """
    A nominal size as said - "DN50", "50", "2", "2 in", '2"', "2-1/2", "2.5" -
    as its inch label, or None. A bare number of 15 or more is a DN in mm; 12
    or less is inches; there is no size where the two readings meet.
    """
    if raw is None:
        return None
    text = str(raw).strip().lower()
    for word in ("dn", "nps", "inches", "inch", "in.", "in", '"', "mm"):
        text = text.replace(word, " ")
    text = " ".join(text.replace("-", " ").split())
    value = None
    try:
        value = float(text)
    except ValueError:
        parts = text.split()
        try:
            if len(parts) == 2 and "/" in parts[1]:
                num, den = parts[1].split("/")
                value = float(parts[0]) + float(num) / float(den)
            elif len(parts) == 1 and "/" in parts[0]:
                num, den = parts[0].split("/")
                value = float(num) / float(den)
        except (ValueError, ZeroDivisionError):
            return None
    if value is None:
        return None
    for label, dn, inches in NOMINALS:
        if value >= 15 and abs(value - dn) < 1e-9:
            return label
        if value <= 12 and abs(value - inches) < 1e-9:
            return label
    return None


def nominal_text(label):
    for name, dn, _inches in NOMINALS:
        if name == label:
            return "DN%d (%s in)" % (dn, name)
    return str(label)


def _nominal_order(label):
    for i, (name, _dn, _inches) in enumerate(NOMINALS):
        if name == label:
            return i
    return len(NOMINALS)


# ---------------------------------------------------------------------------
# The calculations. Each is registered with a name, a title and a group; its
# docstring's first paragraph is the one-line purpose the catalogue shows.
# ---------------------------------------------------------------------------

CALCULATIONS = collections.OrderedDict()


def calculation(name, title, group):
    def register(fn):
        doc = (fn.__doc__ or "").strip().split("\n\n")[0]
        CALCULATIONS[name] = {"title": title, "group": group, "run": fn,
                              "purpose": " ".join(doc.split())}
        return fn
    return register


# --- units -------------------------------------------------------------------

_CONVERT = collections.OrderedDict([
    ("flow", [("l/min", 1.0), ("l/s", 60.0), ("m3/h", 1000.0 / 60.0),
              ("us gpm", LPM_PER_GPM)]),
    ("pressure", [("bar", 1.0), ("kpa", 0.01), ("psi", 1.0 / PSI_PER_BAR),
                  ("m head", BAR_PER_M), ("ft head", BAR_PER_M * M_PER_FT)]),
    ("density", [("mm/min", 1.0), ("l/min.m2", 1.0), ("gpm/ft2", MM_MIN_PER_GPM_FT2)]),
    ("k-factor", [("l/min.bar^0.5", 1.0), ("gpm/psi^0.5", K_METRIC_PER_US)]),
    ("area", [("m2", 1.0), ("ft2", M2_PER_FT2)]),
    ("length", [("m", 1.0), ("mm", 0.001), ("ft", M_PER_FT), ("in", MM_PER_IN / 1000.0)]),
    ("volume", [("m3", 1.0), ("l", 0.001), ("us gal", LPM_PER_GPM / 1000.0)]),
    ("velocity", [("m/s", 1.0), ("ft/s", M_PER_FT)]),
])

_ALIASES = {"lpm": "l/min", "l/m": "l/min", "litre/min": "l/min", "liter/min": "l/min",
            "lps": "l/s", "m3/hr": "m3/h", "cmh": "m3/h", "gpm": "us gpm",
            "usgpm": "us gpm", "psig": "psi", "mwc": "m head", "m of water": "m head",
            "ft of water": "ft head", "lpm/m2": "l/min.m2", "l/min/m2": "l/min.m2",
            "gpm/sqft": "gpm/ft2", "gpm/sq ft": "gpm/ft2", "k metric": "l/min.bar^0.5",
            "l/min/bar^0.5": "l/min.bar^0.5", "k us": "gpm/psi^0.5",
            "gpm/psi^.5": "gpm/psi^0.5", "sqm": "m2", "sq m": "m2", "sqft": "ft2",
            "sq ft": "ft2", "feet": "ft", "foot": "ft", "inch": "in", "inches": "in",
            "litre": "l", "liter": "l", "litres": "l", "gal": "us gal", "gallon": "us gal",
            "us gallon": "us gal", "m/sec": "m/s", "fps": "ft/s"}


def _unit(text):
    key = " ".join(str(text).strip().lower().split())
    key = _ALIASES.get(key, key)
    for quantity, units in _CONVERT.items():
        for name, factor in units:
            if name == key:
                return quantity, name, factor
    return None


@calculation("convert", "Unit conversion", "units")
def calc_convert(a):
    """Converts a fire protection quantity between units - flows, pressures and heads, densities, K-factors, areas, lengths, volumes and velocities - with exact factors."""
    value = a.number("value", "number", "the figure to convert")
    source = a.word("from", "the unit it is in - one of: %s" % "; ".join(
        "%s (%s)" % (q, ", ".join(u[0] for u in units)) for q, units in _CONVERT.items()))
    target = a.word("to", "the unit wanted")
    if a.incomplete():
        return
    got, want = _unit(source), _unit(target)
    if got is None or want is None:
        a.refuse("unknown unit %r - Heron converts: %s" % (
            source if got is None else target,
            "; ".join("%s: %s" % (q, ", ".join(u[0] for u in units))
                      for q, units in _CONVERT.items())))
        return
    if got[0] != want[0]:
        a.refuse("%s is a %s and %s is a %s - they do not convert" % (
            got[1], got[0], want[1], want[0]))
        return
    out = value * got[2] / want[2]
    a.result("%s %s" % (_g(value), got[1]), "%s %s" % (_g(out), want[1]))
    a.uses("exact factors: US gallon 3.785411784 L, psi 6894.757293168 Pa, foot 0.3048 m; "
           "a head of water at NFPA 13's 0.433 psi per foot (0.0979 bar per metre); "
           "K metric = K US x 3.785411784 x sqrt(14.5037738) = K US x %s"
           % _g(round(K_METRIC_PER_US, 4)))


# --- one sprinkler -------------------------------------------------------------

def _min_pressure_offer(family=None, hazard=None):
    """The least pressure a standard sets at a sprinkler, as an offer - both when unknown."""
    if family is None:
        return "; ".join(t for t in (_design_offer("nfpa", None, "pressure"),
                                     offer_table("en12845_criteria")) if t)
    if family == "en" and hazard is None:
        return offer_table("en12845_criteria")
    return _design_offer(family, hazard, "pressure")


def _min_pressure(a, required=True, family=None, hazard=None):
    """The least pressure at an operating sprinkler - asked, with the standard's offered."""
    return pressure_bar(a, "the least pressure any operating sprinkler may run at - the "
                        "standard sets one and the sprinkler's listing may set more",
                        prefix="min_pressure", required=required,
                        reference=_min_pressure_offer(family, hazard))


@calculation("sprinkler_flow", "One sprinkler's discharge - q = K sqrt(p)", "hydraulics")
def calc_sprinkler_flow(a):
    """What one sprinkler discharges at a pressure, the pressure it needs for a flow, or the flow and pressure a density over its area asks of it - from its K-factor, q = K sqrt(p)."""
    k = k_factor(a, "the sprinkler's K-factor, from its data sheet")
    p = pressure_bar(a, "the pressure at the sprinkler", required=False)
    q = flow_lpm(a, "the flow wanted from the sprinkler", required=False)
    density = density_mm_min(a, "the design density, with area_per_sprinkler, for the "
                             "flow it asks of this sprinkler", required=False)
    area = area_m2(a, "the floor area this sprinkler protects (S x L)",
                   "area_per_sprinkler", required=False)
    given = [n for n, v in (("pressure", p), ("flow", q), ("density", density)) if v is not None]
    if not given and not a.refused:
        a.need("pressure_bar", "bar (or flow_lpm, or density_mm_min with "
               "area_per_sprinkler_m2)", "what to work from - a pressure, a flow, or a "
               "density over the sprinkler's area")
    if len(given) > 1:
        a.refuse("give one of pressure, flow or density - %s were all given" % ", ".join(given))
    if density is not None and area is None:
        a.need("area_per_sprinkler_m2", "m2", "the floor area this sprinkler protects, "
               "S x L - the density is over that area")
    pmin = _min_pressure(a, required=density is not None or q is not None)
    if a.incomplete():
        return
    a.result("K-factor", k_text(k))
    if p is not None:
        a.result("Flow", flow_text(head_flow(k, p)))
        a.result("At pressure", pressure_text(p))
        if pmin is not None:
            a.check("OK" if p >= pmin - 1e-12 else "FAIL",
                    "%s against the %s minimum given" % (pressure_text(p), pressure_text(pmin)))
    else:
        need_q = q if q is not None else density * area
        if density is not None:
            a.result("Flow the density asks", "%s - %s x %s" % (
                flow_text(need_q), density_text(density), area_text(area)))
        p_flow = head_pressure(k, need_q)
        p_need = max(p_flow, pmin)
        q_out = head_flow(k, p_need)
        a.result("Pressure needed", pressure_text(p_need))
        if p_need > p_flow + 1e-12:
            a.result("Flow at that pressure", "%s - the minimum pressure governs, not the "
                     "flow: the sprinkler discharges more than asked" % flow_text(q_out))
            a.check("OK", "the minimum pressure %s governs; the flow alone needed %s"
                    % (pressure_text(pmin), pressure_text(p_flow)))
        else:
            a.result("Flow at that pressure", flow_text(q_out))
            a.check("OK", "the flow governs: %s is above the %s minimum"
                    % (pressure_text(p_flow), pressure_text(pmin)))
    a.uses("q = K sqrt(p): L/min with K in L/min.bar^0.5 and p in bar (gpm, psi in US units)")
    a.cite(SRC_K)
    a.into_revit("REPORT_CONNECTOR_LOADS reads the K-factor Revit holds on each sprinkler's "
                 "connector - a sprinkler with none counts as zero flow in Revit's own "
                 "pressure calculation")


# --- friction ------------------------------------------------------------------

def _bore_row(schedule, nominal):
    word = _norm(schedule).replace("schedule", "").replace("sch", "").strip()
    for row in REFERENCES["steel_pipe_bores"]["rows"]:
        if row[0] == word and row[1] == nominal:
            return row
    return None


def _bore(view, why, required=True):
    """
    (bore mm, nominal label or None, the sentence saying where the bore came
    from) - bore_mm as given, or a nominal size and schedule looked up in the
    cited steel table. A named schedule is a fact, not a design value.
    """
    view = _view(view)
    has_b, has_n = view.has("bore_mm"), view.has("nominal")
    if has_b and has_n:
        view.answer.refuse("%s gives bore_mm AND nominal - one decides the bore"
                           % view.name("bore_mm"))
        return None, None, None
    if has_b:
        b = view.number("bore_mm", "mm", why, 5, 2000)
        return b, None, None if b is None else "%s mm bore, as given" % _g(b)
    if has_n:
        nominal = nominal_key(view.raw("nominal"))
        if nominal is None:
            view.answer.refuse("%s %r is not a nominal size Heron knows - give DN25, 1 in "
                               "or the bore as bore_mm" % (view.name("nominal"),
                                                           view.raw("nominal")))
            return None, None, None
        schedule = view.raw("schedule")
        if schedule is None or schedule == "":
            schedule = view.answer.top.raw("schedule")
        if schedule is None or schedule == "":
            view.answer.need(view.name("schedule"), "10 or 40",
                             "the steel pipe's schedule, to read its bore - or give bore_mm "
                             "for anything else", offer_table("steel_pipe_bores"))
            return None, None, None
        row = _bore_row(schedule, nominal)
        if row is None:
            view.answer.refuse("no bore for %s schedule %s in %s - give bore_mm"
                               % (nominal_text(nominal), schedule,
                                  REFERENCES["steel_pipe_bores"]["source"]))
            return None, None, None
        return float(row[5]), nominal, "%s schedule %s steel, %s mm bore (%s)" % (
            nominal_text(nominal), row[0], _g(float(row[5])),
            REFERENCES["steel_pipe_bores"]["source"])
    if required:
        view.answer.need(view.name("bore_mm"), "mm (or nominal with schedule)", why,
                         offer_table("steel_pipe_bores"))
    return None, None, None


def _c_offer(family=None):
    """The C values a standard gives, as an offer - both tables when the standard is unknown."""
    tables = {"nfpa": ("c_factors_nfpa",), "en": ("c_factors_en12845",), "fm": ()}
    names = tables.get(family, ("c_factors_nfpa", "c_factors_en12845"))
    parts = []
    for table in names:
        data = REFERENCES[table]
        parts.append("%s: %s" % (data["source"], "; ".join(
            "%s %s" % (r[0], _g(r[1])) for r in data["rows"] if r[1] not in (None, NOT_HELD))))
    if not parts:
        return "FM Global's C values were not read in this work - take them from data sheet 2-0"
    return ("%s - offer the one for the pipe and the system, do not assume it; the authority "
            "may set its own" % " | ".join(parts))


def _c_factor(view, why, required=True, family=None):
    return view.number("c_factor", "Hazen-Williams C", why, 50, 160, required=required,
                       reference=_c_offer(family))


@calculation("friction_loss", "Pipe friction loss - Hazen-Williams", "hydraulics")
def calc_friction_loss(a):
    """Friction loss in a sprinkler pipe at a flow - Hazen-Williams as NFPA 13 prints it - per metre and over a length with its fittings' equivalent length, with the velocity and the pressure an elevation change adds."""
    q = flow_lpm(a, "the flow in the pipe")
    bore, _nominal, said = _bore(a, "the pipe's inside diameter")
    c = _c_factor(a, "the pipe's Hazen-Williams C - from the pipe and the system type")
    length = a.number("length_m", "m", "the pipe's length, for the total loss", 0.01, 5000,
                      required=False)
    extra = a.number("equivalent_length_m", "m", "the fittings' and valves' equivalent length "
                     "- NFPA 13's table, or the manufacturer's", 0, 1000, required=False)
    rise = a.number("elevation_change_m", "m", "how far the far end is ABOVE this end - "
                    "negative for below", -500, 500, required=False)
    if a.incomplete():
        return
    per_m = hw_friction(q, bore, c)
    v = velocity_ms(q, bore)
    a.result("Bore", said)
    a.result("Velocity", "%s m/s  (%s ft/s)" % (_f(v, 2), _f(v / M_PER_FT, 2)))
    a.result("Friction", "%s bar/m  (%s psi/ft)" % (_f(per_m, 5),
                                                   _f(per_m * PSI_PER_BAR * M_PER_FT, 4)))
    if length is not None or extra:
        run = (length or 0.0) + (extra or 0.0)
        a.result("Length", "%s m of pipe + %s m equivalent for fittings = %s m"
                 % (_f(length or 0.0, 2), _f(extra or 0.0, 2), _f(run, 2)))
        a.result("Friction loss", pressure_text(per_m * run))
    if rise is not None:
        a.result("Elevation", "%s over %s m - a rise costs pressure, a fall gives it"
                 % (pressure_text(rise * BAR_PER_M), _g(rise)))
    if length is not None and extra is None:
        a.assume("no equivalent length given for fittings, so none added - every tee, elbow "
                 "and valve adds friction, and leaving them out errs small")
    a.uses("Hazen-Williams, NFPA 13's SI form: p = 6.05 x 10^5 Q^1.85 / (C^1.85 d^4.87), "
           "bar per metre for Q in L/min and d in mm (4.52 Q^1.85 / C^1.85 d^4.87 psi per "
           "foot in US units)")
    a.cite(SRC_HW)
    a.into_revit("REPORT_MEP_PRESSURE_DROP reads Revit's own figure for a pipe to compare "
                 "against - Revit's default pipe calculation is Darcy-Weisbach, not "
                 "Hazen-Williams, so the two differ")


# --- fittings ------------------------------------------------------------------------

# A fitting as a person names it, against the name NFPA's chart prints. Only
# which row is READ changes here; a fitting with no row held is asked for.
_FITTINGS = collections.OrderedDict([
    ("tee or cross, flow turned 90 degrees", ("tee", "cross", "tee or cross", "branch tee")),
    ("90 degree standard elbow", ("elbow", "90 elbow", "90 degree elbow", "standard elbow",
                                  "ell", "90 ell", "90 degree standard elbow")),
    ("90 degree long-turn elbow", ("long turn elbow", "long-turn elbow", "long radius elbow",
                                   "lt elbow", "90 degree long-turn elbow",
                                   "90 degree long turn elbow")),
    ("45 degree elbow", ("45 elbow", "45 degree elbow", "45 ell")),
    ("gate valve", ("gate", "gate valve", "os&y", "os and y", "os&y gate valve")),
    ("butterfly valve", ("butterfly", "butterfly valve")),
    ("swing check", ("check", "check valve", "swing check", "swing check valve")),
    ("globe valve", ("globe", "globe valve")),
    ("angle valve", ("angle", "angle valve")),
])


def fitting_key(raw):
    """A fitting as said, as the chart names it - or None."""
    if raw is None:
        return None
    text = " ".join(str(raw).strip().lower().replace("_", " ").replace("-", " ").split())
    for key, words in _FITTINGS.items():
        if text == key.replace("-", " ") or text in [w.replace("-", " ") for w in words]:
            return key
    return None


@calculation("equivalent_length", "Equivalent length of fittings and valves", "hydraulics")
def calc_equivalent_length(a):
    """The equivalent pipe length of the fittings and valves named, read from NFPA 13's chart - schedule 40 steel at C = 120 - and adjusted to the pipe's own C and bore, for `friction_loss` and `hydraulic` to add. The chart answered as the chart; a cell not read is asked for."""
    fittings = a.records("fittings", "each fitting or valve: {fitting, nominal, count} - a "
                         "tee, a 90 degree elbow, a gate valve, a butterfly valve, a check valve")
    c = _c_factor(a, "the pipe's Hazen-Williams C - the chart is for C = 120", family=None)
    schedule = a.choice("schedule", ("10", "40"), "the steel pipe's schedule, for its bore - or "
                        "give bore_mm on each fitting for another pipe", required=False)
    rows = []
    if fittings:
        for v in fittings:
            raw = v.raw("fitting")
            name = fitting_key(raw)
            raw_n = v.raw("nominal")
            nominal = nominal_key(raw_n)
            count = v.integer("count", "how many of this fitting", 1, 1000, required=False)
            bore = v.number("bore_mm", "mm", "this pipe's own bore, where it is not schedule 10 "
                            "or 40 steel", 5, 2000, required=False)
            if raw is None or str(raw).strip() == "":
                a.need(v.name("fitting"), "text", "which fitting or valve")
                continue
            if name is None:
                a.refuse("%s %r is not a fitting Heron knows - %s" % (
                    v.name("fitting"), raw, ", ".join(_FITTINGS)))
                continue
            if raw_n is None or str(raw_n).strip() == "":
                a.need(v.name("nominal"), "DN or inches", "the fitting's nominal size")
                continue
            if nominal is None:
                a.refuse("%s %r is not a nominal size Heron knows - give DN50, 2 in"
                         % (v.name("nominal"), raw_n))
                continue
            rows.append((v, name, nominal, count or 1, bore))
    if rows and schedule is None and any(r[4] is None for r in rows) and not a.refused:
        a.need("schedule", "10 or 40", "the steel pipe's schedule, for its bore - or give "
               "bore_mm on each fitting", offer_table("steel_pipe_bores"))
    if a.incomplete():
        return
    data = REFERENCES["equivalent_lengths"]
    c_factor = (c / 120.0) ** HW_EXPONENT
    table = []
    total = 0.0
    for v, name, nominal, count, bore in rows:
        held = [r for r in data["rows"] if r[0] == name and r[1] == nominal]
        if not held:
            a.refuse("NFPA 13's chart for a %s at %s was not read in this work - give its "
                     "equivalent_length_m from your copy" % (name, nominal_text(nominal)))
            return
        ft = held[0][2]
        base = _bore_row("40", nominal)
        if bore is None:
            own = _bore_row(schedule, nominal)
            if own is None or base is None:
                a.refuse("no schedule %s bore for %s - give bore_mm" % (schedule,
                                                                        nominal_text(nominal)))
                return
            bore = float(own[5])
        size = (bore / float(base[5])) ** HW_DIAMETER_EXPONENT
        each = ft * M_PER_FT * c_factor * size
        total += each * count
        table.append([name, nominal_text(nominal), count, _g(ft), _f(ft * M_PER_FT, 3),
                      _f(c_factor * size, 3), _f(each, 3), _f(each * count, 3)])
    a.table("Equivalent lengths - the chart's, adjusted", ("fitting", "size", "count", "chart ft",
                                                           "chart m", "factor", "m each",
                                                           "m total"), table)
    a.result("Equivalent length", "%s m - give it as equivalent_length_m" % _f(total, 3))
    a.uses("factor = (C / 120)^1.85 x (bore / schedule 40 bore)^4.87 - the C form as two "
           "result sets give it; the bore form is the same Hazen-Williams algebra, a fitting "
           "taken to lose what it loses in schedule 40 steel (docs/42 s10)")
    a.cite(data["source"], SRC_HW)
    a.into_revit("REPORT_MEP_PRESSURE_DROP reads Revit's own fitting losses, by its own method, "
                 "to compare against")


# --- the network -----------------------------------------------------------------

def supply_pressure(static, residual, test_flow, q):
    """
    Pressure a supply gives at flow q, from a flow test - the N^1.85 relation
    NFPA's hydraulic graph paper is drawn on: P = Ps - (Ps - Pr)(Q / Qt)^1.85.
    """
    return static - (static - residual) * (q / test_flow) ** HW_EXPONENT


def supply_flow(static, residual, test_flow, p):
    """The flow a tested supply gives at pressure p - the same relation solved for Q."""
    if p >= static:
        return 0.0
    return test_flow * ((static - p) / (static - residual)) ** (1.0 / HW_EXPONENT)


def _read_supply(a):
    """(static, residual, test flow) of a flow test, all three or none - or None."""
    static = pressure_bar(a, "the supply's static pressure, from a flow test",
                          prefix="supply_static", required=False)
    residual = pressure_bar(a, "the residual pressure measured at the test flow",
                            prefix="supply_residual", required=False)
    test = flow_lpm(a, "the flow the test measured the residual at",
                    prefix="supply_test_flow", required=False)
    given = [v for v in (static, residual, test) if v is not None]
    if not given:
        return None
    if len(given) != 3:
        a.need("supply_static_bar", "bar, with supply_residual_bar and "
               "supply_test_flow_lpm", "a flow test is three figures - the static "
               "pressure, and the residual at a measured flow - and a curve needs all three")
        return None
    if residual >= static:
        a.refuse("the residual pressure must be below the static pressure - a supply "
                 "loses pressure as it flows")
        return None
    if test <= 0:
        a.refuse("the test flow must be more than zero")
        return None
    return static, residual, test


def _supply_lines(a, supply, need_q, need_p, margin):
    static, residual, test = supply
    have = supply_pressure(static, residual, test, need_q)
    a.result("Supply at the demand", "%s at %s - from the flow test's curve"
             % (pressure_text(have), flow_text(need_q)))
    a.result("Margin", "%s above the %s needed" % (pressure_text(have - need_p),
                                                   pressure_text(need_p)))
    if have < need_p - 1e-12:
        a.check("FAIL", "the supply gives %s at %s - %s SHORT of what the system needs; a "
                        "pump, a bigger pipe or a lower demand" % (
                            pressure_text(have), flow_text(need_q),
                            pressure_text(need_p - have)))
    elif margin is not None and have - need_p < margin - 1e-12:
        a.check("FAIL", "the supply clears the demand by %s, less than the %s margin given"
                % (pressure_text(have - need_p), pressure_text(margin)))
    else:
        a.check("OK", "the supply clears the demand by %s%s" % (
            pressure_text(have - need_p),
            "" if margin is None else ", inside the %s margin given" % pressure_text(margin)))
    if need_q > test * (1 + 1e-9):
        a.check("WARN", "the demand %s is beyond the %s the supply was tested at - the "
                        "curve is extrapolated there; a test nearer the demand would show it"
                % (flow_text(need_q), flow_text(test)))
    a.uses("supply curve P = Ps - (Ps - Pr)(Q/Qt)^1.85 through the flow test - the relation "
           "NFPA's N^1.85 hydraulic graph is drawn on")


@calculation("hydraulic", "Sprinkler system hydraulic calculation", "hydraulics")
def calc_hydraulic(a):
    """Solves a sprinkler network - a tree or a looped grid - for the design area's sprinklers: each one's flow and pressure, each pipe's flow, velocity and loss, and the flow and pressure the source must give, against a flow test's supply curve if one is given."""
    node_views = a.records("nodes", "every junction and sprinkler in the network: id, "
                           "elevation_m, and for a sprinkler its K-factor (k_lpm_bar or "
                           "k_gpm_psi) and area_per_sprinkler_m2 or min_flow_lpm")
    pipe_views = a.records("pipes", "every pipe: from, to, length_m, bore_mm or nominal "
                           "(with schedule), and its fittings' equivalent_length_m")
    held = project_standards(a, ("sprinkler_standard",))
    family = figures_of(held)
    source = a.word("source", "the node where the demand is reported - the base of the "
                    "riser, a pump's discharge or the supply connection")
    density = density_mm_min(a, "the design density, for sprinklers given by their area",
                             required=False, reference=_design_offer(family, None, "density"))
    gmin = pressure_bar(a, "the least pressure any operating sprinkler may run at - for "
                        "sprinklers that do not give their own", prefix="min_pressure",
                        required=False, reference=_min_pressure_offer(family))
    gc = _c_factor(a, "the Hazen-Williams C for pipes that do not give their own",
                   required=False, family=family)
    hose = flow_lpm(a, "hose streams to add at the source - the hose allowance",
                    prefix="hose_allowance", required=False,
                    reference=_design_offer(family, None, "hose"))
    vmax = a.number("max_velocity_ms", "m/s", "a velocity no pipe may exceed, if the "
                    "standard or the authority sets one", 0.5, 20, required=False,
                    reference=_rules_offer("en12845_rules", ("velocity",))
                    if family == "en" else None)
    pmax = pressure_bar(a, "the most pressure any sprinkler may see - its rated pressure, "
                        "from its data sheet", prefix="max_pressure", required=False,
                        reference=_rules_offer("sprinkler_rules", ("rated working pressure",))
                        if family == "nfpa" else None)
    device = pressure_bar(a, "losses between the source node and the supply not drawn as "
                          "pipe - an alarm valve, a backflow preventer, a meter - from "
                          "their data sheets at the demand", prefix="device_loss",
                          required=False)
    margin = pressure_bar(a, "the margin the supply must clear the demand by, if the "
                          "authority sets one", prefix="safety_margin", required=False)
    supply = _read_supply(a)
    if node_views is None or pipe_views is None or a.refused:
        return
    if len(node_views) > 400 or len(pipe_views) > 600:
        a.refuse("%d nodes and %d pipes - Heron solves up to 400 and 600; a whole building "
                 "is a hydraulic program's job, the design area is this one's"
                 % (len(node_views), len(pipe_views)))
        return

    nodes = collections.OrderedDict()
    heads = []
    asked_area = asked_pmin = False
    for v in node_views:
        nid = v.text("id")
        if nid is None:
            a.refuse("%s has no id" % v.name("id"))
            continue
        if nid in nodes:
            a.refuse("node %r is listed twice" % nid)
            continue
        z = v.number("elevation_m", "m", "the node's elevation - every node's, so the "
                     "rise from the source is counted", -1000, 1000)
        k = k_factor(v, "the sprinkler's K-factor", required=False)
        on = v.flag("operating", "false for a sprinkler outside the design area - every "
                    "sprinkler given is taken as operating unless it says so")
        entry = {"z": z, "k": None, "p_req": None}
        if k is not None and on is not False:
            qmin = flow_lpm(v, "the least flow this sprinkler must give", prefix="min_flow",
                            required=False)
            area = area_m2(v, "the floor area this sprinkler protects, S x L - the density "
                           "is over it", "area_per_sprinkler", required=False)
            pmin = pressure_bar(v, "this sprinkler's own least pressure",
                                prefix="min_pressure", required=False)
            if qmin is not None and area is not None:
                a.refuse("%s gives both min_flow and area_per_sprinkler - one decides its "
                         "flow" % v.name("id"))
            if qmin is None and area is None and not asked_area:
                a.need(v.name("area_per_sprinkler_m2"), "m2 (or min_flow_lpm)",
                       "the floor area this sprinkler protects, S x L, for the density - "
                       "or the least flow it must give")
                asked_area = True
            if area is not None and density is None:
                a.need("density_mm_min", "mm/min (or density_gpm_ft2)",
                       "the design density the sprinklers' areas are multiplied by",
                       _design_offer(family, None, "density"))
            if pmin is None:
                pmin = gmin
                if pmin is None and not asked_pmin:
                    a.need("min_pressure_bar", "bar (or min_pressure_psi)",
                           "the least pressure any operating sprinkler may run at - the "
                           "standard sets one and the sprinkler's listing may set more",
                           _min_pressure_offer(family))
                    asked_pmin = True
            entry.update({"k": k, "qmin": qmin, "area": area, "pmin": pmin})
            heads.append(nid)
        nodes[nid] = entry
    if source is not None and nodes and source not in nodes:
        a.refuse("the source %r is not one of the nodes" % source)
    if source is not None and source in heads:
        a.refuse("the source %r is a sprinkler - name the node the demand is reported at"
                 % source)

    pipes = []
    asked_c = False
    for v in pipe_views:
        frm, to = v.text("from"), v.text("to")
        if frm is None or to is None:
            a.refuse("%s needs both from and to" % v.name("from"))
            continue
        for end in (frm, to):
            if nodes and end not in nodes:
                a.refuse("%s names %r, which is not a node" % (v.name("from"), end))
        if frm == to:
            a.refuse("%s runs from %r to itself" % (v.name("from"), frm))
        length = v.number("length_m", "m", "the pipe's length", 0.001, 5000)
        bore, nominal, said = _bore(v, "the pipe's inside diameter")
        c = _c_factor(v, "this pipe's C", required=False, family=family)
        if c is None:
            c = gc
            if c is None and not asked_c:
                a.need("c_factor", "Hazen-Williams C", "the C for pipes that do not give "
                       "their own - from the pipe material and the system type",
                       _c_offer(family))
                asked_c = True
        eq = v.number("equivalent_length_m", "m", "the fittings' and valves' equivalent "
                      "length on this pipe - NFPA 13's table, or the manufacturer's", 0, 1000,
                      required=False)
        pipes.append({"a": frm, "b": to, "length": length, "bore": bore, "nominal": nominal,
                      "c": c, "eq": eq or 0.0, "has_eq": eq is not None, "said": said})
    if a.incomplete():
        return

    for nid in heads:
        e = nodes[nid]
        qmin = e["qmin"] if e["qmin"] is not None else density * e["area"]
        e["q_req"] = qmin
        e["p_req"] = max(head_pressure(e["k"], qmin), e["pmin"])
    for p in pipes:
        p["r"] = hw_friction(1.0, p["bore"], p["c"]) * (p["length"] + p["eq"])
    try:
        pressures, flows, governing = solve_network(nodes, pipes, source)
    except Unsolved as why:
        a.refuse(str(why))
        return

    demand = sum(head_flow(nodes[h]["k"], pressures[h]) for h in heads)
    out_of_source = 0.0
    for p, q in zip(pipes, flows):
        if p["a"] == source:
            out_of_source += q
        elif p["b"] == source:
            out_of_source -= q
    p_source = pressures[source] + (device or 0.0)
    a.result("Governing sprinkler", "%s - it runs at exactly what it needs, every other "
             "at least that" % governing)
    a.result("Sprinkler demand", flow_text(demand))
    a.result("Pressure at %s" % source, pressure_text(pressures[source]))
    if device:
        a.result("Plus devices", "%s - so %s at the supply" % (pressure_text(device),
                                                               pressure_text(p_source)))
    total = demand + (hose or 0.0)
    if hose:
        a.result("Hose allowance", flow_text(hose))
        a.result("Total demand", "%s at %s" % (flow_text(total), pressure_text(p_source)))
    rows = []
    worst_pmax = None
    for h in heads:
        e = nodes[h]
        p_at = pressures[h]
        q_at = head_flow(e["k"], p_at)
        rows.append([h, _f(e["k"], 1), _f(e["q_req"], 1), _f(e["p_req"], 3), _f(q_at, 1),
                     _f(p_at, 3), _f(q_at - e["q_req"], 1)])
        if pmax is not None and p_at > pmax + 1e-12:
            if worst_pmax is None or p_at > pressures[worst_pmax]:
                worst_pmax = h
    a.table("Operating sprinklers", ("sprinkler", "K metric", "needs L/min", "needs bar",
                                     "gives L/min", "at bar", "over L/min"), rows)
    prow = []
    fast = []
    for p, q in zip(pipes, flows):
        frm, to = (p["a"], p["b"]) if q >= 0 else (p["b"], p["a"])
        v = velocity_ms(abs(q), p["bore"])
        loss = p["r"] * abs(q) ** HW_EXPONENT
        prow.append(["%s>%s" % (frm, to), _f(p["bore"], 2), _g(p["c"]), _f(p["length"], 2),
                     _f(p["eq"], 2), _f(abs(q), 1), _f(v, 2),
                     _f(hw_friction(abs(q), p["bore"], p["c"]), 5), _f(loss, 4)])
        if vmax is not None and v > vmax + 1e-12:
            fast.append("%s>%s %s m/s" % (frm, to, _f(v, 2)))
    a.table("Pipes - flow in the direction shown", ("pipe", "bore mm", "C", "length m",
                                                    "fittings m", "L/min", "m/s", "bar/m",
                                                    "loss bar"), prow)
    if abs(out_of_source - demand) > 1e-6 * max(1.0, demand):
        a.check("FAIL", "flow does not balance: %s leaves the source and the sprinklers "
                        "discharge %s" % (flow_text(out_of_source), flow_text(demand)))
    else:
        a.check("OK", "flow balances: the %d operating sprinklers discharge what leaves the "
                      "source, %s" % (len(heads), flow_text(demand)))
    a.check("OK", "every operating sprinkler gives at least its flow and its minimum "
                  "pressure; %s gives exactly that" % governing)
    if vmax is not None:
        if fast:
            a.check("FAIL", "above the %s m/s given: %s" % (_g(vmax), "; ".join(fast[:10])))
        else:
            a.check("OK", "no pipe is above the %s m/s given" % _g(vmax))
    if pmax is not None:
        if worst_pmax is not None:
            a.check("FAIL", "sprinkler %s sees %s, above the %s given" % (
                worst_pmax, pressure_text(pressures[worst_pmax]), pressure_text(pmax)))
        else:
            a.check("OK", "no sprinkler sees more than the %s given" % pressure_text(pmax))
    if supply is not None:
        _supply_lines(a, supply, total, p_source, margin)
    _check_standard(a, held["sprinkler_standard"], "the method and the figures offered are",
                    family)
    a.uses("node pressures solved by Newton's method so every node balances: Hazen-Williams "
           "in every pipe, q = K sqrt(p) at every operating sprinkler, elevation at 0.433 psi "
           "per foot; the source pressure raised until the governing sprinkler has exactly "
           "its need and no other has less")
    a.uses("the total-pressure method - velocity pressure is not taken off at the "
           "sprinklers, which NFPA 13 permits and which errs toward more pressure")
    a.uses(NOT_LISTED)
    a.cite(SRC_HW, SRC_K)
    if not any(p["has_eq"] for p in pipes):
        a.assume("no fittings' equivalent length given on any pipe, so none added - every "
                 "tee, elbow and valve adds friction, and leaving them out errs small")
    if not hose:
        a.assume("no hose allowance given, so none added - the supply must still carry the "
                 "hose streams the hazard's table asks for: `reference design_criteria`")
    a.into_revit("READ_MEP_SYSTEM and REPORT_CONNECTOR_LOADS give the network and each "
                 "sprinkler's K-factor this calculation takes; SET_MEP_SIZE changes a pipe "
                 "that needs to be bigger; REPORT_MEP_PRESSURE_DROP reads Revit's own figure, "
                 "which by default is Darcy-Weisbach and not this one")


@calculation("water_supply", "Water supply against a demand - a flow test's curve", "supply")
def calc_water_supply(a):
    """Whether a tested water supply - static pressure, and the residual at a measured flow - meets a sprinkler demand plus hose streams, and by how much, and the most flow it gives at the demand's pressure."""
    supply = _read_supply(a)
    if supply is None and not a.missing and not a.refused:
        a.need("supply_static_bar", "bar, with supply_residual_bar and supply_test_flow_lpm",
               "the flow test - static pressure, and the residual at a measured flow")
    q = flow_lpm(a, "the system's demand flow at the supply - from the hydraulic "
                 "calculation", prefix="demand_flow",
                 reference="from `hydraulic`; for a pre-calculated BS EN 12845 system, %s"
                 % offer_table("en12845_precalculated"))
    p = pressure_bar(a, "the pressure the system needs at the supply at that flow",
                     prefix="demand_pressure")
    hose = flow_lpm(a, "hose streams added at the supply", prefix="hose_allowance",
                    required=False, reference=offer_table("design_criteria"))
    margin = pressure_bar(a, "the margin the authority asks the supply to clear the demand "
                          "by", prefix="safety_margin", required=False)
    if a.incomplete():
        return
    total = q + (hose or 0.0)
    a.result("Demand", "%s at %s%s" % (flow_text(total), pressure_text(p),
                                       "" if not hose else " (with %s of hose)" % flow_text(hose)))
    _supply_lines(a, supply, total, p, margin)
    a.result("Most flow at that pressure", flow_text(supply_flow(supply[0], supply[1],
                                                                supply[2], p)))
    if not hose:
        a.assume("no hose allowance given, so none added")


@calculation("water_storage", "Fire water storage volume", "supply")
def calc_water_storage(a):
    """The water a fire tank must hold - each demand's flow over its own duration, summed, plus any volume the pumps cannot draw - with what QCDD and the UAE code are reported to ask of the tank itself."""
    held = project_standards(a, ("sprinkler_standard", "fire_authority"))
    family = figures_of(held)
    q = flow_lpm(a, "the sprinkler system's demand flow at the supply - from the hydraulic "
                 "calculation", prefix="sprinkler_flow")
    t = a.number("duration_min", "min", "how long the supply must last - the hazard's "
                 "duration, or the authority's own table", 1, 1440,
                 reference=_design_offer(family, None, "duration"))
    hose = flow_lpm(a, "hose streams over the same duration", prefix="hose_allowance",
                    required=False, reference=_design_offer(family, None, "hose"))
    others = a.records("other_demands", "any other system the tank also serves at the same "
                       "time - each {name, flow_lpm, duration_min}", required=False)
    dead = a.number("unusable_volume_m3", "m3", "water below the pumps' suction that cannot "
                    "be drawn - from the tank and pump arrangement", 0, 1e6, required=False)
    rows = []
    if others:
        for v in others:
            name = v.text("name") or v.name("name")
            fq = flow_lpm(v, "this demand's flow")
            ft = v.number("duration_min", "min", "this demand's duration", 1, 1440)
            if fq is not None and ft is not None:
                rows.append([name, fq, ft])
    if a.incomplete():
        return
    lines = [["sprinklers", q, t]]
    if hose:
        lines.append(["hose streams", hose, t])
    lines.extend(rows)
    volume = sum(fq * ft for _n, fq, ft in lines) / 1000.0
    a.table("Demands", ("demand", "L/min", "minutes", "m3"),
            [[n, _f(fq, 1), _g(ft), _f(fq * ft / 1000.0, 2)] for n, fq, ft in lines])
    a.result("Effective volume", volume_text(volume))
    total = volume + (dead or 0.0)
    if dead:
        a.result("With unusable volume", volume_text(total))
    else:
        a.assume("no unusable volume given, so none added - water below the pump suction "
                 "is not usable, and a tank sized to the effective volume alone runs dry early")
    if held["fire_authority"] == QCDD:
        tank = lookup("qatar", "fire water tank")
        a.check("WARN", "QCDD is reported to ask %s - for %s that would be two compartments "
                        "of %s, and an infill of at least %s; and its annex sets storage by "
                        "building class (`reference qatar`), which may ask more than this sum"
                % (tank[1] if tank else "two compartments and a 6 h refill", volume_text(total),
                   volume_text(total / 2.0), flow_text(total * 1000.0 / 360.0)))
    elif held["fire_authority"] == UAE:
        rule = lookup("uae", "fire water storage")
        a.check("WARN", "the UAE Fire and Life Safety Code is reported to ask %s - compare it "
                        "with the pump set's rated flow (`reference uae`)"
                % (rule[1] if rule else "an hour of the pump set's capacity"))
    _check_standard(a, held["sprinkler_standard"], "the durations offered are", family)
    _check_authority(a, held["fire_authority"], "the storage duration and volume by "
                     "occupancy")
    a.uses("volume = sum of each demand's flow x its own duration; every demand listed is "
           "taken as running at the same time, as given")
    a.cite(SRC_STORAGE)


@calculation("coverage_check", "Coverage of a room by devices within a radius", "devices")
def calc_coverage_check(a):
    """Whether every point of a room lies within a radius of a device - or of two, as hose reels are asked to reach every part - hose reels by hose length and throw, extinguishers by travel distance, detectors by a radius - sampled over the room's outline, with the farthest point found."""
    outline = read_outline(a, "the room's outline in plan - READ_ROOM_GEOMETRY gives it")
    points = read_points(a, "the devices' plan positions", "points_mm")
    radius = a.number("radius_m", "m", "the distance every point must be within - the rule "
                      "being checked", 0.1, 500, reference=offer_table("coverage_rules"))
    by = a.integer("reached_by", "how many devices every point must be within the radius of "
                   "- 2 where two hoses must reach every part; 1 if not given", 1, 4,
                   required=False)
    step = a.number("step_mm", "mm", "the sampling step - 250 mm if not given", 20, 5000,
                    required=False)
    if a.incomplete():
        return
    if by is not None and by > len(points):
        a.refuse("every point reached by %d devices needs at least %d - %d were given"
                 % (by, by, len(points)))
        return
    if step is None:
        step = 250.0
    k = by or 1
    used = sampling_step(outline, step, len(points))
    a.uses("sampled every %s mm%s - give step_mm for finer" % (
        _f(used, 0), "" if used <= step + 1e-9 else ", coarser than asked so one answer "
        "measures no more than %d distances" % int(WORK_MAX)))
    worst, wx, wy, samples = farthest_point(outline, points, step, k)
    within = sum(1 for d in samples if d <= radius * 1000.0 + 1e-9)
    nearest = "the nearest device" if k == 1 else "its %s nearest device" % (
        {2: "second", 3: "third", 4: "fourth"}[k])
    a.result("Farthest point", "%s m from %s, at x %s y %s mm"
             % (_f(worst / 1000.0, 2), nearest, _f(wx, 0), _f(wy, 0)))
    a.result("Covered", "%d of %d sampled points (%s %%)%s" % (
        within, len(samples), _f(100.0 * within / len(samples), 1),
        "" if k == 1 else " - each within %s m of %d devices" % (_g(radius), k)))
    if worst > radius * 1000.0 + 1e-9:
        a.check("FAIL", "a point %s m from %s - beyond the %s m given"
                % (_f(worst / 1000.0, 2), "any device" if k == 1 else nearest, _g(radius)))
    else:
        a.check("OK", "every sampled point is within %s m of %s" % (
            _g(radius), "a device" if k == 1 else "%d devices" % k))
    if by is None:
        a.assume("every point checked against ONE device - where two must reach every part, "
                 "as QCDD is reported to ask of hose reels, give reached_by 2")
    a.check("WARN", "distances are STRAIGHT LINES in plan. A travel distance is walked round "
                    "walls and furniture, and is longer - a straight-line FAIL is a fail, a "
                    "straight-line OK is not proof of the walked route")
    a.uses("the outline sampled on a square grid and at its corners; each sample's distance "
           "to the nearest device")
    a.into_revit("REPORT_COVERAGE reads the same devices' coverage in Revit at a radius")


# --- hazard classes ------------------------------------------------------------

# Each standard's own classes, as a person may say them - after _norm, so
# "HC-1" arrives as "hc 1". A class is never inferred from an occupancy, and a
# class is read only in the standard the project follows: EN 12845's OH3 is
# not an NFPA 13 class, and NFPA 13's OH1 is not EN 12845's OH1.
HAZARDS = collections.OrderedDict([
    ("light hazard", ("light", "lh", "light hazard")),
    ("ordinary hazard group 1", ("oh1", "oh 1", "ordinary 1", "ordinary group 1",
                                 "ordinary hazard 1", "ordinary hazard group 1",
                                 "ordinary hazard group i")),
    ("ordinary hazard group 2", ("oh2", "oh 2", "ordinary 2", "ordinary group 2",
                                 "ordinary hazard 2", "ordinary hazard group 2",
                                 "ordinary hazard group ii")),
    ("extra hazard group 1", ("eh1", "eh 1", "extra 1", "extra group 1", "extra hazard 1",
                              "extra hazard group 1", "extra hazard group i")),
    ("extra hazard group 2", ("eh2", "eh 2", "extra 2", "extra group 2", "extra hazard 2",
                              "extra hazard group 2", "extra hazard group ii")),
])

_ROMAN = ("i", "ii", "iii", "iv")

HAZARDS_EN = collections.OrderedDict([("LH", ("lh", "light", "light hazard"))])
for _n in range(1, 5):
    HAZARDS_EN["OH%d" % _n] = (
        "oh%d" % _n, "oh %d" % _n, "ordinary %d" % _n, "ordinary group %d" % _n,
        "ordinary hazard %d" % _n, "ordinary hazard group %d" % _n,
        "ordinary hazard group %s" % _ROMAN[_n - 1])
for _n in range(1, 5):
    HAZARDS_EN["HHP%d" % _n] = (
        "hhp%d" % _n, "hhp %d" % _n, "high hazard process %d" % _n,
        "high hazard process group %d" % _n, "high hazard process group %s" % _ROMAN[_n - 1])
for _n in range(1, 5):
    HAZARDS_EN["HHS%d" % _n] = (
        "hhs%d" % _n, "hhs %d" % _n, "high hazard storage %d" % _n,
        "high hazard storage category %d" % _n,
        "high hazard storage category %s" % _ROMAN[_n - 1])

HAZARDS_FM = collections.OrderedDict()
for _n in range(1, 4):
    HAZARDS_FM["HC-%d" % _n] = ("hc %d" % _n, "hc%d" % _n, "hazard category %d" % _n,
                                "hazard category %s" % _ROMAN[_n - 1])
del _n

HAZARD_SETS = {"nfpa": HAZARDS, "en": HAZARDS_EN, "fm": HAZARDS_FM}
HAZARD_NAMES = {"nfpa": "light, OH1, OH2, EH1 or EH2",
                "en": "LH, OH1 to OH4, HHP1 to HHP4 or HHS1 to HHS4",
                "fm": "HC-1, HC-2 or HC-3"}


def hazard_key(raw, family="nfpa"):
    """
    A hazard class as said - "OH1", "ordinary hazard group 1", "HC-2" - as one
    standard's own class, or None. `family` is the standard: nfpa, en or fm.
    """
    if raw is None:
        return None
    text = " ".join(_norm(raw).replace("(", " ").replace(")", " ").split())
    for key, words in HAZARD_SETS[family].items():
        if text == _norm(key) or text in words:
            return key
    return None


def _hazard(a, why, held, required=True):
    """
    The hazard class, as the ENGINEER classified it, in the classes of the
    standard the project follows - NFPA 13's until that is known. Never
    inferred from an occupancy, and never read across from another standard.
    """
    family = figures_of(held)
    view = _view(a)
    raw = view.raw("hazard")
    if raw is None or raw == "":
        return _absent(view, "hazard", "%s (%s)" % (HAZARD_NAMES[family], FAMILIES[family][0]),
                       why, required,
                       "the class is the engineer's and the authority's - `hazard_class` "
                       "shows what the lists hold beside an occupancy")
    key = hazard_key(raw, family)
    if key is None:
        standard = held.get("sprinkler_standard")
        elsewhere = [f for f in FAMILIES if f != family and hazard_key(raw, f) is not None]
        if elsewhere:
            name = FAMILIES[elsewhere[0]][0]
            if family_of(standard) is None:
                view.answer.refuse(
                    "hazard %r is a %s class, and %s - its classes are read as NFPA 13's, %s, "
                    "until sprinkler_standard says otherwise; give sprinkler_standard if the "
                    "project follows %s" % (
                        raw, name, "which standard governs this project's sprinklers is not "
                        "known" if standard is None else "this project's sprinklers follow "
                        "another standard", HAZARD_NAMES["nfpa"], name))
            else:
                view.answer.refuse("hazard %r is a %s class, and this project follows %s, "
                                   "whose classes are %s" % (raw, name, standard,
                                                             HAZARD_NAMES[family]))
        else:
            view.answer.refuse("hazard %r is not a class Heron knows - %s's are %s"
                               % (raw, FAMILIES[family][0], HAZARD_NAMES[family]))
    return key


def _read_all(a, *keys):
    """
    Mark a calculation's own inputs as read when it refuses before reaching
    them - each WAS an input of it, and is not to be reported as ignored.
    """
    for key in keys:
        a.raw(key)


def _spacing_family(hazard):
    """The row family of NFPA 13's standard spray spacing table a class reads, or None."""
    if hazard is None:
        return None
    if hazard.startswith("ordinary hazard"):
        return "ordinary hazard"
    if hazard.startswith("extra hazard"):
        return "extra hazard"
    if hazard == "light hazard":
        return hazard
    return None


def _offer_cell(table, row, column, unit, label=None):
    """One cell of a held table as an offer - saying so where it was not read or allowed."""
    data = REFERENCES[table]
    value = row[data["columns"].index(column)]
    if value is None or value == NOT_HELD:
        return ("%s: not read in this work for %s - take it from your copy"
                % (data["source"], label or row[0]))
    if isinstance(value, str):
        return "%s gives, for %s: %s" % (data["source"], label or row[0], value)
    return ("%s gives %s %s for %s - offer it to the modeller, do not assume it"
            % (data["source"], _g(value), unit, label or row[0]))


def _criteria_offer(hazard, column, unit, second=False):
    """One NFPA 13 design_criteria figure for a hazard, as an offer - or the table's pointer."""
    if hazard is None:
        return offer_table("design_criteria")
    data = REFERENCES["design_criteria"]
    row = lookup("design_criteria", hazard)
    if row is None:
        return offer_table("design_criteria")
    if second:
        cols = data["columns"]
        return ("%s gives %s mm/min over %s m2 (%s gpm/ft2 over %s ft2) for %s where "
                "combustible concealed spaces are NOT sprinklered (its second point) - offer "
                "it to the modeller, do not assume it" % (
                    data["source"], _g(row[cols.index("second density mm/min")]),
                    _g(row[cols.index("second area m2")]),
                    _g(row[cols.index("second density gpm/ft2")]),
                    _g(row[cols.index("second area ft2")]), hazard))
    return offer("design_criteria", hazard, column, unit)


# What a design question asks for, as each family's table names it.
_EN_COLUMNS = {"density": ("density mm/min", "mm/min"), "area": ("area wet m2", "m2"),
               "dry": ("area dry m2", "m2"), "duration": ("duration min", "min"),
               "pressure": ("least pressure bar", "bar")}
_FM_COLUMNS = {"density": ("density mm/min", "mm/min"), "area": ("area wet m2", "m2"),
               "dry": ("area dry m2", "m2"), "hose": ("hose L/min", "L/min"),
               "duration": ("duration min", "min")}


def _design_offer(family, hazard, what, second=False):
    """
    The sentence offering one design figure - density, area, dry, hose,
    duration or pressure - for a class in the project's standard, or that
    standard's table. OFFERED beside the question, never applied.
    """
    if family == "en":
        if what not in _EN_COLUMNS:
            return None
        row = lookup("en12845_criteria", hazard) if hazard else None
        if row is None:
            return offer_table("en12845_criteria")
        column, unit = _EN_COLUMNS[what]
        return _offer_cell("en12845_criteria", row, column, unit)
    if family == "fm":
        if what not in _FM_COLUMNS:
            return None
        rows = reference_rows("fm_criteria", hazard) if hazard else []
        if not rows:
            return offer_table("fm_criteria")
        column, unit = _FM_COLUMNS[what]
        cols = REFERENCES["fm_criteria"]["columns"]
        ceiling = cols.index("ceiling up to")
        return "; ".join(_offer_cell("fm_criteria", r, column, unit,
                                     "%s, a ceiling up to %s" % (r[0], r[ceiling]))
                         for r in rows)
    if what == "density":
        return _criteria_offer(hazard, "density mm/min", "mm/min", second)
    if what == "area":
        return _criteria_offer(hazard, "design area m2", "m2", second)
    if what == "hose":
        return _criteria_offer(hazard, "hose allowance L/min", "L/min")
    if what == "duration":
        return _criteria_offer(hazard, "duration min", "min")
    if what == "dry":
        return _rules_offer("design_area_rules", ("dry pipe", "double-interlock preaction"))
    if what == "pressure":
        return _rules_offer("sprinkler_rules", ("minimum operating pressure",))
    return None


def _rules_offer(table, keys):
    """The rule rows named, as one sentence offering them - or None if none is held."""
    rows = [lookup(table, k) for k in keys]
    rows = [r for r in rows if r is not None]
    if not rows:
        return None
    return ("%s: %s - offer it to the modeller, do not assume it" % (
        REFERENCES[table]["source"], "; ".join("%s - %s" % (r[0], r[1]) for r in rows)))


def reference_rows(table, key):
    return BASE.reference_rows(table, key, REFERENCES)


def _spacing_offer(hazard, what, family="nfpa"):
    """The sentence offering a class's most area and spacing in the project's standard."""
    if family == "en":
        row = lookup("en12845_criteria", hazard) if hazard else None
        if row is None:
            return offer_table("en12845_criteria")
        cols = REFERENCES["en12845_criteria"]["columns"]
        area, spacing = (row[cols.index("most area per sprinkler m2")],
                         row[cols.index("most spacing m")])
        stagger = lookup("en12845_rules", "staggered spacing")
        return ("%s gives, for %s (%s): %s m2 per sprinkler, %s m apart in a standard layout%s "
                "- offer it to the modeller, do not assume it" % (
                    REFERENCES["en12845_criteria"]["source"], hazard, what, _g(area),
                    _g(spacing), "" if stagger is None else "; staggered - %s" % stagger[1]))
    if family == "fm":
        return ("FM Global's spacing by hazard category (data sheet 2-0) was not read in this "
                "work - take it from the data sheet")
    nfpa = _spacing_family(hazard)
    data = REFERENCES.get("spacing_standard_spray")
    if nfpa is None or data is None:
        return offer_table("spacing_standard_spray")
    cols = data["columns"]
    rows = [r for r in data["rows"] if r[0] == nfpa]
    if not rows:
        return offer_table("spacing_standard_spray")
    parts = []
    for r in rows:
        spacing = r[cols.index("max spacing m")]
        parts.append("%s, %s: %s m2 (%s ft2), spacing %s" % (
            r[cols.index("construction")], r[cols.index("system")],
            _g(r[cols.index("max area m2")]), _g(r[cols.index("max area ft2")]),
            "%s m (%s ft)" % (_g(spacing), _g(r[cols.index("max spacing ft")]))
            if spacing not in (None, NOT_HELD) else "not read in this work"))
    return ("%s gives, for %s standard spray sprinklers (%s): %s - offer the row that fits "
            "the construction, do not assume one" % (data["source"], nfpa, what,
                                                     "; ".join(parts)))


def _wall_offer(family):
    """The most distance from a sprinkler to a wall, as the project's standard sets it."""
    if family == "en":
        return _rules_offer("en12845_rules", ("most distance to a wall",))
    if family == "fm":
        return "FM Global's wall distances (data sheet 2-0) were not read in this work"
    return _rule("sprinkler_rules", "maximum distance to a wall")


def _least_spacing_offer(family):
    """The least distance between two sprinklers, as the project's standard sets it."""
    if family == "en":
        return _rules_offer("en12845_rules", ("least distance between sprinklers",))
    if family == "fm":
        return None
    return _rule("sprinkler_rules", "minimum distance between sprinklers")


def _spacing_source(family):
    """What a spacing answer cites, as the project's standard sets it."""
    if family == "en":
        return REFERENCES["en12845_criteria"]["source"]
    if family == "fm":
        return "FM Global data sheet 2-0 - its spacing was not read in this work"
    return SRC_SPACING


# --- the design area -------------------------------------------------------------

SYSTEMS = ("wet", "dry", "alternate", "preaction-single-interlock", "preaction-double-interlock",
           "preaction-non-interlock", "deluge", "antifreeze")
CONCEALED = ("none", "noncombustible", "sprinklered", "unsprinklered-combustible")

# The systems each standard works a larger area for - NFPA 13's dry and
# double-interlock preaction, EN 12845's dry and alternate, and FM Global's dry.
_LARGER_AREA = {"nfpa": ("dry", "preaction-double-interlock"),
                "en": ("dry", "alternate"),
                "fm": ("dry", "preaction-double-interlock", "alternate")}


def _larger_area_offer(family, hazard, system):
    """The increase a standard's own table gives for a dry system, as an offer."""
    if family == "nfpa":
        return _design_offer("nfpa", hazard, "dry")
    table = "en12845_criteria" if family == "en" else "fm_criteria"
    cols = REFERENCES[table]["columns"]
    rows = ([lookup(table, hazard)] if family == "en" else reference_rows(table, hazard)) \
        if hazard else []
    parts = []
    for row in [r for r in rows if r is not None]:
        wet, dry = row[cols.index("area wet m2")], row[cols.index("area dry m2")]
        label = row[0] if family == "en" else "%s, a ceiling up to %s" % (
            row[0], row[cols.index("ceiling up to")])
        if isinstance(wet, float) or isinstance(wet, int):
            if isinstance(dry, (int, float)):
                parts.append("%s: %s m2 %s against %s m2 wet - %s %%" % (
                    label, _g(dry), system, _g(wet), _g(round(100.0 * (dry / wet - 1.0), 1))))
                continue
            if dry not in (None, NOT_HELD):
                parts.append("%s: %s" % (label, dry))
                continue
        parts.append("%s: not read in this work" % label)
    if not parts:
        return offer_table(table)
    return ("%s gives %s - offer it to the modeller, do not assume it"
            % (REFERENCES[table]["source"], "; ".join(parts)))


@calculation("design_area", "Design area - the sprinklers it holds and the water it needs", "design")
def calc_design_area(a):
    """The remote design area after its adjustments - dry systems, sloped ceilings, quick-response, high-temperature sprinklers - how many sprinklers it holds and how many along a branch line (1.2 sqrt A), each one's least flow and the end sprinkler's pressure, and the least water it needs with hose streams and duration; with sprinklers above a ceiling too, combined with those below where the modeller says so. NFPA 13's, EN 12845's or FM Global's figures offered, as the project follows."""
    held = project_standards(a, ("sprinkler_standard", "fire_authority"))
    family = figures_of(held)
    hazard = _hazard(a, "the occupancy hazard class - only to offer its figures beside "
                     "each question", held, required=False)
    concealed = a.choice("concealed_space", CONCEALED, "the space above the ceiling - none, "
                         "noncombustible, sprinklered, or combustible and NOT sprinklered, "
                         "which changes the design area NFPA 13 asks", required=False)
    second = concealed == "unsprinklered-combustible" and family == "nfpa"
    density = density_mm_min(a, "the design density",
                             reference=_design_offer(family, hazard, "density", second))
    area = area_m2(a, "the design area before any adjustment", "design_area",
                   reference=_design_offer(family, hazard, "area", second))
    each = area_m2(a, "the floor area each sprinkler protects in the layout, S x L - the "
                   "layout's own, not the most allowed", "area_per_sprinkler")
    s = a.number("spacing_along_branch_m", "m", "the sprinkler spacing along the branch "
                 "lines, S - for how many sprinklers NFPA 13's 1.2 sqrt A side holds", 0.3, 10,
                 required=family == "nfpa")
    system = a.choice("system_type", SYSTEMS, "the system type - a dry system works a larger "
                      "area", required=False)
    increase = None
    if system in _LARGER_AREA[family]:
        increase = a.number("dry_area_increase_pct", "%", "the increase in design area for "
                            "a %s system - ASKED, not skipped, because leaving it out errs "
                            "small" % system, 0, 200,
                            reference=_larger_area_offer(family, hazard, system))
    slope = a.number("ceiling_slope_pct", "%", "the ceiling's slope, rise over run - a steep "
                     "ceiling works a larger area", 0, 1000, required=False)
    slope_increase = None
    if slope is not None and slope > 0:
        slope_increase = a.number("slope_area_increase_pct", "%", "the design area increase "
                                  "for a ceiling this steep - 0 if the standard asks none at "
                                  "this pitch", 0, 200,
                                  reference=_rules_offer("design_area_rules", ("sloped ceiling",))
                                  if family == "nfpa" else "%s's sloped-ceiling rule was not "
                                  "read in this work" % FAMILIES[family][0])
    qr = a.number("qr_reduction_pct", "%", "a quick-response reduction of the design area - "
                  "NEVER applied unless given, because it makes the demand smaller", 0, 75,
                  required=False,
                  reference=_rules_offer("design_area_rules", ("quick-response",))
                  if family == "nfpa" else None)
    hot = a.number("high_temp_reduction_pct", "%", "a reduction of an extra hazard design "
                   "area for high-temperature sprinklers - NEVER applied unless given", 0, 75,
                   required=False,
                   reference=_rules_offer("design_area_rules", ("high-temperature sprinklers",))
                   if family == "nfpa" else None)
    k = k_factor(a, "the sprinklers' K-factor - for the end sprinkler's pressure",
                 required=False)
    pmin = _min_pressure(a, required=k is not None, family=family, hazard=hazard)
    hose = flow_lpm(a, "hose streams to add", prefix="hose_allowance", required=False,
                    reference=_design_offer(family, hazard, "hose"))
    duration = a.number("duration_min", "min", "how long the supply must last", 1, 1440,
                        required=False, reference=_design_offer(family, hazard, "duration"))
    above = a.record("above_ceiling", "sprinklers in the space above the ceiling: "
                     "{density_mm_min, design_area_m2, area_per_sprinkler_m2}", required=False)
    combine = None
    if above is not None:
        combine = a.flag("combine_above_and_below", "whether the design area counts the "
                         "sprinklers above the ceiling AND those below it together", False)
        if combine is None and not a.refused:
            a.need("combine_above_and_below", "true or false",
                   "whether the sprinklers above the ceiling and those below it are counted "
                   "in ONE design area or calculated apart, the larger governing",
                   "the authority's and the engineer's call - NFPA 13's own rule on it was "
                   "not read in this work")
    up = None
    if above is not None:
        up_density = density_mm_min(above, "the density above the ceiling")
        up_area = area_m2(above, "the design area above the ceiling", "design_area")
        up_each = area_m2(above, "the area each sprinkler above the ceiling protects",
                          "area_per_sprinkler")
        up = (up_density, up_area, up_each)
    if a.incomplete():
        return
    if qr is not None and system not in (None, "wet"):
        a.refuse("a quick-response reduction is for wet systems - this one is %s" % system)
        return
    if (qr is not None or hot is not None) and family != "nfpa":
        a.refuse("the quick-response and high-temperature reductions held here are NFPA "
                 "13's - this project follows %s" % held["sprinkler_standard"])
        return
    if hot is not None and hazard is not None and not hazard.startswith("extra hazard"):
        a.refuse("the high-temperature reduction is for extra hazard - this is %s" % hazard)
        return
    # Every adjustment is a percentage of the area FIRST SELECTED, and they are
    # added - NFPA 13's own worked example as a search extract reproduces it:
    # 2500 + 750 - 625 = 2625 ft2 for a dry system with high-temperature
    # sprinklers. Never multiplied in turn.
    steps = [("design area", 0.0, area)]
    worked = area
    for label, pct, sign in (("%s system" % system, increase, 1.0),
                             ("sloped ceiling", slope_increase, 1.0),
                             ("quick-response", qr, -1.0),
                             ("high-temperature sprinklers", hot, -1.0)):
        if pct:
            worked += sign * pct / 100.0 * area
            steps.append(("%s, %s%s %% of %s" % (label, "+" if sign > 0 else "-", _g(pct),
                                                 _f(area, 1)), sign * pct / 100.0 * area,
                          worked))
    if worked <= 0:
        a.refuse("the reductions given take the whole design area away")
        return
    if len(steps) > 1:
        a.table("The design area, each adjustment taken on the area first selected",
                ("step", "change m2", "m2", "ft2"),
                [[n, "-" if i == 0 else "%+.1f" % d, _f(v, 1), _f(v / M2_PER_FT2, 0)]
                 for i, (n, d, v) in enumerate(steps)])
        rule = lookup("design_area_rules", "more than one adjustment")
        if family == "nfpa" and rule is not None:
            a.uses("NFPA 13, more than one adjustment: %s (%s)" % (rule[1], rule[3]))
        else:
            a.uses("the adjustment is a percentage of the area first selected")
    n = int(math.ceil(worked / each - 1e-9))
    q_each = density * each
    a.result("Design area", area_text(worked))
    a.result("Sprinklers in it", "%d - %s / %s each, rounded up" % (n, _f(worked, 1), _f(each, 2)))
    if family == "nfpa":
        side = 1.2 * math.sqrt(worked)
        per_line = int(math.ceil(side / s - 1e-9))
        lines = int(math.ceil(float(n) / per_line - 1e-9))
        a.result("Along a branch line", "%d - the area's long side, 1.2 sqrt A = %s, over S = "
                 "%s m, rounded up" % (per_line, length_text(side), _g(s)))
        a.result("Branch lines", "%d" % lines)
    a.result("Least flow per sprinkler", "%s - %s x %s" % (flow_text(q_each),
                                                          density_text(density), area_text(each)))
    floor_q = n * q_each
    if k is not None:
        p_flow = head_pressure(k, q_each)
        p_end = max(p_flow, pmin)
        a.result("End sprinkler pressure", "%s at %s%s" % (
            pressure_text(p_end), k_text(k),
            " - the minimum pressure governs" if p_end > p_flow + 1e-12 else ""))
        floor_q = n * head_flow(k, p_end)
    a.result("Least sprinkler demand", "%s - every sprinkler at the end sprinkler's flow; the "
             "hydraulic calculation's is higher, as pressure rises toward the source"
             % flow_text(floor_q))
    total_q = floor_q
    if up is not None:
        u_density, u_area, u_each = up
        u_n = int(math.ceil(u_area / u_each - 1e-9))
        u_q = u_n * u_density * u_each
        a.result("Above the ceiling", "%d sprinklers, at least %s" % (u_n, flow_text(u_q)))
        if combine:
            total_q = floor_q + u_q
            a.result("Combined", "%s - above and below the ceiling together, as the modeller "
                     "said" % flow_text(total_q))
        else:
            total_q = max(floor_q, u_q)
            a.result("Governing", "%s - the larger of the two, calculated apart, as the "
                     "modeller said" % flow_text(total_q))
    if hose:
        a.result("With hose streams", flow_text(total_q + hose))
    if duration:
        a.result("Least water", "%s over %s min" % (
            volume_text((total_q + (hose or 0.0)) * duration / 1000.0), _g(duration)))
    if qr and n < 5:
        rule = lookup("design_area_rules", "quick-response")
        a.check("FAIL", "the quick-response reduction leaves %d sprinklers - NFPA 13 never takes "
                        "the design area below five (%s)" % (n, rule[3] if rule else "NFPA 13"))
    if hot:
        floor = lookup("design_area_rules", "high-temperature sprinklers")
        if floor is not None and worked < floor[2] - 1e-9:
            a.check("FAIL", "the high-temperature reduction leaves %s - NFPA 13 keeps it at "
                            "%s m2 (2000 ft2) or more, after every adjustment (%s)"
                    % (area_text(worked), _f(floor[2], 1), floor[3]))
    if second:
        # Applied AFTER every other adjustment, as NFPA 13 says.
        least = lookup("design_area_rules", "unsprinklered combustible concealed spaces")
        if least is not None and worked < least[2] - 1e-9:
            a.check("WARN", "a %s design area, after its adjustments, is below the %s m2 (%s) "
                            "NFPA 13 asks where combustible concealed spaces are not "
                            "sprinklered" % (area_text(worked), _g(round(least[2], 1)),
                                             least[1]))
    if concealed == "unsprinklered-combustible" and family == "en":
        rule = lookup("en12845_rules", "concealed spaces")
        a.check("WARN", "a concealed space is not sprinklered - %s" % (
            "BS EN 12845: %s" % rule[1] if rule else "EN 12845 sets when one must be"))
    if concealed == "sprinklered" and above is None:
        a.check("WARN", "the space above the ceiling is sprinklered, and those sprinklers "
                        "were not given - give above_ceiling, and say whether they combine")
    if family == "en" and hazard is not None:
        row = lookup("en12845_criteria", hazard)
        cols = REFERENCES["en12845_criteria"]["columns"]
        dry = row[cols.index("area dry m2")] if row else None
        if system in _LARGER_AREA["en"] and isinstance(dry, str) and dry != NOT_HELD:
            a.check("FAIL", "BS EN 12845 does not allow a %s system at %s - it is %s"
                    % (system, hazard, dry.replace("not allowed - ", "")))
        if hazard.startswith("HHS") or hazard == "HHP4":
            a.check("WARN", "BS EN 12845 designs %s from its own tables - %s - which were not "
                            "read in this work; the figures used are the modeller's"
                    % (hazard, "storage by category and height" if hazard.startswith("HHS")
                       else "a deluge system"))
    _check_standard(a, held["sprinkler_standard"], "the figures offered are", family)
    _check_authority(a, held["fire_authority"], "the design density, area and duration")
    if system is None:
        a.assume("no system type given - a dry system works a larger area, and none was added")
    a.uses("sprinklers in the area = area / area per sprinkler, a fraction carried to the "
           "next whole sprinkler")
    if family == "nfpa":
        a.uses("along a branch line = 1.2 sqrt(area) / S, rounded up - NFPA 13's rectangular "
               "design area, the side parallel to the branch lines at least 1.2 sqrt A (two "
               "search result sets agree; docs/42 s10)")
    else:
        a.uses("no count along a branch line: 1.2 sqrt A is NFPA 13's, and %s's rule for the "
               "shape of the area was not read in this work" % FAMILIES[family][0])
    a.cite(SRC_DESIGN_AREA if family == "nfpa" else REFERENCES[
        "en12845_criteria" if family == "en" else "fm_criteria"]["source"])
    a.into_revit("the hydraulic calculation (`hydraulic`) takes these sprinklers as the "
                 "operating ones; SELECT_BY_MEP_SYSTEM and REPORT_CONNECTOR_LOADS read them")


# --- the sprinkler's temperature rating ----------------------------------------------

@calculation("temperature_rating", "Sprinkler temperature rating for a ceiling temperature", "design")
def calc_temperature_rating(a):
    """The temperature classifications NFPA 13's table allows at the highest temperature a ceiling may reach - each with its rating and colours, the lowest first - or the glass bulbs BS EN 12845's 30 C rule leaves, as the project follows. A table answered as the table: which rating is fitted is the engineer's, with the sprinkler's listing."""
    held = project_standards(a, ("sprinkler_standard",))
    family = figures_of(held)
    c = a.number("ceiling_temperature_c", "C", "the highest temperature the space may reach at "
                 "the sprinklers", -50, 400, required=False)
    f = a.number("ceiling_temperature_f", "F", "the same in F", -60, 750, required=False)
    if c is not None and f is not None:
        a.refuse("give ceiling_temperature_c OR ceiling_temperature_f - one temperature")
    if c is None and f is None and not a.refused:
        a.need("ceiling_temperature_c", "C (or ceiling_temperature_f)", "the highest "
               "temperature the space may reach at the sprinklers - the ceiling, not the room")
    if a.incomplete():
        return
    if family == "fm":
        a.refuse("FM Global's temperature rating guidance (data sheet 2-0) was not read in this "
                 "work - give the rating from it")
        return
    t = c if c is not None else _f_to_c(f)
    a.result("Ceiling temperature", "%s C (%s F)" % (_f(t, 1), _f(t * 9.0 / 5.0 + 32.0, 0)))
    if family == "en":
        data = REFERENCES["temperature_en12845"]
        rule = lookup("en12845_rules", "temperature rating")
        need = t + 30.0
        a.result("Least rating", "%s C - %s C + 30 C, EN 12845's rule (%s)" % (
            _f(need, 1), _f(t, 1), rule[2] if rule else "one search extract"))
        bulbs = [r for r in data["rows"] if r[0] >= need - 1e-9]
        if bulbs:
            a.table("Glass bulbs held here at or above it - the nearest first", data["columns"],
                    bulbs)
            a.result("Nearest bulb", "%s C, %s - close to the least, as the rule asks"
                     % (_g(bulbs[0][0]), bulbs[0][1]))
        else:
            a.result("Bulbs", "none of the bulbs held here reaches %s C - above 141 C only one "
                     "extract's colour bands were read: `reference temperature_en12845`"
                     % _f(need, 1))
        _check_standard(a, held["sprinkler_standard"], "the rule and the colours used are",
                        "en")
        a.cite(data["source"])
    else:
        data = REFERENCES["temperature_ratings"]
        cols = data["columns"]
        top = cols.index("most ceiling F")
        allowed = [r for r in data["rows"] if _f_to_c(r[top]) >= t - 1e-9]
        if not allowed:
            a.refuse("a ceiling of %s C is hotter than any classification NFPA 13's table "
                     "allows - its highest is %s C" % (_f(t, 1),
                                                       _f(_f_to_c(data["rows"][-1][top]), 1)))
            return
        a.table("Classifications NFPA 13's table allows at this ceiling - the lowest first",
                cols, allowed)
        low = allowed[0]
        a.result("Lowest allowed", "%s - %s F (%s C) rating, for a ceiling up to %s F (%s C); "
                 "frame %s, bulb %s" % (
                     low[0], low[cols.index("rating F")], low[cols.index("rating C")],
                     _g(low[top]), _g(low[cols.index("most ceiling C")]),
                     low[cols.index("frame colour")], low[cols.index("bulb colour")]))
        _check_standard(a, held["sprinkler_standard"], "the table used is", "nfpa")
        a.cite(data["source"])
    a.check("WARN", "near a heat source - a unit heater, a skylight, an uninsulated roof, a "
                    "process - a higher rating is asked by distance, and that table was not "
                    "read; the rating fitted is the engineer's, within the sprinkler's listing")
    a.uses("the ceiling temperature compared with the most ceiling temperature each "
           "classification allows, converted exactly from F" if family == "nfpa" else
           "the least rating = the highest ambient + 30 C")
    a.into_revit("SELECT_BY_FAMILY finds the sprinklers; CHANGE_ELEMENT_TYPE swaps them to the "
                 "type with the rating chosen")


# --- spacing ---------------------------------------------------------------------

def _limits(a, hazard, family="nfpa", required_wall=True):
    """The spacing limits a layout or a check is held to - every one asked (D-33)."""
    smax = a.number("max_spacing_m", "m", "the most allowed between sprinklers, along and "
                    "between branch lines", 1, 10,
                    reference=_spacing_offer(hazard, "maximum spacing and area", family))
    amax = area_m2(a, "the most floor area one sprinkler may protect", "max_area",
                   reference=_spacing_offer(hazard, "maximum area and spacing", family))
    wmax = a.number("max_wall_distance_m", "m", "the most allowed from a sprinkler to a wall",
                    0.1, 10, required=required_wall, reference=_wall_offer(family))
    smin = a.number("min_spacing_m", "m", "the least allowed between two sprinklers", 0.3,
                    10, required=False)
    wmin = a.number("min_wall_distance_m", "m", "the least allowed from a sprinkler to a "
                    "wall", 0.0, 2, required=False)
    return smax, amax, wmax, smin, wmin


def _limits_not_checked(a, smin, wmin, family="nfpa"):
    name = FAMILIES[family][0]
    if smin is None:
        a.check("WARN", "minimum spacing NOT CHECKED - give min_spacing_m; %s"
                % (_least_spacing_offer(family) or "%s sets one" % name))
    if wmin is None:
        a.check("WARN", "minimum distance to a wall NOT CHECKED - give min_wall_distance_m; %s"
                % ((_rule("sprinkler_rules", "minimum distance to a wall")
                    if family == "nfpa" else None) or "%s sets one" % name))


@calculation("sprinkler_layout", "Sprinkler count and layout for a room", "layout")
def calc_sprinkler_layout(a):
    """How many sprinklers a rectangular room needs and where - the fewest on a grid meeting the most spacing, the most area per sprinkler and the most distance to a wall given - with the spacings, each one's area and its position."""
    held = project_standards(a, ("sprinkler_standard",))
    family = figures_of(held)
    length = a.number("room_length_m", "m", "the room's length, along its x axis", 0.3, 1000)
    width = a.number("room_width_m", "m", "the room's width, along its y axis", 0.3, 1000)
    hazard = _hazard(a, "the occupancy hazard class - only to offer its spacing figures",
                     held, required=False)
    smax, amax, wmax, smin, wmin = _limits(a, hazard, family)
    ox = a.number("origin_x_mm", "mm", "x of the room corner the grid starts at, for "
                  "placement points", -1e9, 1e9, required=False)
    oy = a.number("origin_y_mm", "mm", "y of that corner", -1e9, 1e9, required=False)
    z = a.number("mounting_height_mm", "mm", "the sprinklers' elevation - asked, never "
                 "defaulted: it is the ceiling less the deflector distance", -1e6, 1e6,
                 required=False)
    rot = a.number("rotation_deg", "degrees", "the room's x axis angle from project x - 0 if "
                   "not given", -360, 360, required=False)
    if a.incomplete():
        return
    found = grid(length, width, smax=smax, amax=amax, wmax=wmax, smin=smin, wmin=wmin)
    if found is None:
        a.refuse("no grid meets those limits in a %s x %s m room - a minimum spacing too "
                 "large for the room, or a room too large to search" % (_g(length), _g(width)))
        return
    rows, cols = found
    sx, sy = length / cols, width / rows
    a.result("Sprinklers", "%d - %d along the length x %d across" % (rows * cols, cols, rows))
    a.result("Spacing", "%s m along x, %s m along y" % (_f(sx, 3), _f(sy, 3)))
    a.result("Area per sprinkler", "%s - S x L" % area_text(sx * sy))
    a.result("To the walls", "%s m to each wall across x, %s m to each wall across y - half "
             "a module" % (_f(sx / 2.0, 3), _f(sy / 2.0, 3)))
    a.check("OK", "spacing %s and %s m against %s m; area %s against %s m2; wall %s m against "
                  "%s m" % (_f(sx, 2), _f(sy, 2), _g(smax), _f(sx * sy, 2), _f(amax, 2),
                            _f(max(sx, sy) / 2.0, 2), _g(wmax)))
    _limits_not_checked(a, smin, wmin, family)
    points = grid_points(length, width, rows, cols)
    if None not in (ox, oy):
        r = math.radians(rot or 0.0)
        placed = [(ox + x * math.cos(r) - y * math.sin(r), oy + x * math.sin(r) + y * math.cos(r))
                  for x, y in points]
        a.table("Sprinkler points (mm)", ("#", "x", "y", "z"),
                [[k + 1, "%.0f" % px, "%.0f" % py, "%.0f" % z if z is not None else "ASK"]
                 for k, (px, py) in enumerate(placed)])
        if z is None:
            a.check("WARN", "no mounting_height_mm - ask for it before placing, or every "
                            "sprinkler lands at the level's own elevation")
        else:
            a.into_revit("PLACE_FAMILY_INSTANCES at these points, on the level they are "
                         "measured from; then CHECK_OBSTRUCTIONS on the same points for the "
                         "beams and ducts a grid cannot see")
    else:
        a.table("Sprinkler points from the room corner (mm)", ("#", "x", "y"),
                [[k + 1, "%.0f" % px, "%.0f" % py] for k, (px, py) in enumerate(points)])
    _check_standard(a, held["sprinkler_standard"], "the spacing figures offered are", family)
    a.uses("a grid of rows x cols, each sprinkler centred in its module; the fewest "
           "sprinklers first, then the squarest module")
    a.cite(_spacing_source(family))
    a.into_revit("READ_ROOM_GEOMETRY gives a room's size and outline; MEASURE_CEILING_HEIGHT "
                 "the ceiling the mounting height hangs from; `sprinkler_spacing` checks a "
                 "layout already drawn")


def _rows_of(points, axis, tolerance):
    """Group sprinklers into branch lines: same coordinate across the axis, within tolerance."""
    across = 1 if axis == 0 else 0
    ordered = sorted(points, key=lambda p: p[1 + across])
    lines = []
    for p in ordered:
        if lines and abs(p[1 + across] - lines[-1][0]) <= tolerance:
            lines[-1][1].append(p)
            lines[-1][0] = sum(q[1 + across] for q in lines[-1][1]) / len(lines[-1][1])
        else:
            lines.append([p[1 + across], [p]])
    for line in lines:
        line[1].sort(key=lambda p: p[1 + axis])
    return lines


def _side(gap, wall, tol):
    """
    One side of a sprinkler: (the distance it counts, the wall distance if the
    wall is what it counts). A neighbour counts only if it stands before the wall.
    """
    if gap is not None and (wall is None or gap <= wall + tol):
        return gap, None
    return 2.0 * (wall or 0.0), wall


@calculation("sprinkler_spacing", "Sprinkler spacing check of a drawn layout", "layout")
def calc_sprinkler_spacing(a):
    """Checks sprinklers already placed in a room against the spacing limits given - each one's S along its branch line and L between lines as NFPA 13 measures them, its protection area, its distance to the walls and to its nearest neighbour - from their positions and the room's outline."""
    outline = read_outline(a, "the room's outline in plan - READ_ROOM_GEOMETRY gives it")
    points = read_points(a, "the sprinklers' plan positions, mm", "sprinklers")
    axis = a.choice("branch_axis", ("x", "y"), "which way the branch lines run in plan")
    held = project_standards(a, ("sprinkler_standard",))
    family = figures_of(held)
    hazard = _hazard(a, "the occupancy hazard class - only to offer its spacing figures",
                     held, required=False)
    smax, amax, wmax, smin, wmin = _limits(a, hazard, family)
    tol = a.number("row_tolerance_mm", "mm", "how far off one line a sprinkler may sit and "
                   "still be read as on it - 100 mm if not given", 1, 2000, required=False)
    if a.incomplete():
        return
    if tol is None:
        tol = 100.0
        a.uses("sprinklers within 100 mm across the branch direction are read as one branch "
               "line - give row_tolerance_mm to change that")
    outside = [p[0] for p in points if not inside(outline, p[1], p[2])]
    if outside:
        a.refuse("these sprinklers are outside the outline: %s - check the outline and the "
                 "points are in the same coordinates" % ", ".join(outside[:10]))
        return
    ax = 0 if axis == "x" else 1
    unit = (1.0, 0.0) if ax == 0 else (0.0, 1.0)
    perp = (0.0, 1.0) if ax == 0 else (1.0, 0.0)
    lines = _rows_of(points, ax, tol)
    rows = []
    fails = []
    for li, (coord, members) in enumerate(lines):
        for mi, p in enumerate(members):
            pid, x, y = p
            ahead = ray_to_wall(outline, x, y, unit[0], unit[1])
            behind = ray_to_wall(outline, x, y, -unit[0], -unit[1])
            nxt = members[mi + 1][1 + ax] - p[1 + ax] if mi + 1 < len(members) else None
            prv = p[1 + ax] - members[mi - 1][1 + ax] if mi > 0 else None
            up, wall_up = _side(nxt, ahead, tol)
            down, wall_down = _side(prv, behind, tol)
            s = max(up, down)
            side_a = ray_to_wall(outline, x, y, perp[0], perp[1])
            side_b = ray_to_wall(outline, x, y, -perp[0], -perp[1])
            line_a = lines[li + 1][0] - coord if li + 1 < len(lines) else None
            line_b = coord - lines[li - 1][0] if li > 0 else None
            gap_a, wall_a = _side(line_a, side_a, tol)
            gap_b, wall_b = _side(line_b, side_b, tol)
            l_ = max(gap_a, gap_b)
            walls = [w for w in (wall_up, wall_down, wall_a, wall_b) if w is not None]
            far_wall = max(walls) if walls else None
            wall = wall_distance(outline, x, y)
            others = [math.hypot(x - q[1], y - q[2]) for q in points if q[0] != pid]
            near = min(others) if others else None
            bad = []
            if s > smax * 1000.0 + 1e-6:
                bad.append("S %s m" % _f(s / 1000.0, 2))
            if l_ > smax * 1000.0 + 1e-6:
                bad.append("L %s m" % _f(l_ / 1000.0, 2))
            if s * l_ / 1e6 > amax + 1e-9:
                bad.append("area %s m2" % _f(s * l_ / 1e6, 2))
            if far_wall is not None and far_wall > wmax * 1000.0 + 1e-6:
                bad.append("wall %s m away" % _f(far_wall / 1000.0, 2))
            if smin is not None and near is not None and near < smin * 1000.0 - 1e-6:
                bad.append("neighbour %s m" % _f(near / 1000.0, 2))
            if wmin is not None and wall < wmin * 1000.0 - 1e-6:
                bad.append("wall %s m close" % _f(wall / 1000.0, 2))
            rows.append([pid, _f(s / 1000.0, 2), _f(l_ / 1000.0, 2), _f(s * l_ / 1e6, 2),
                         "-" if far_wall is None else _f(far_wall / 1000.0, 2),
                         "-" if near is None else _f(near / 1000.0, 2),
                         "OK" if not bad else "FAIL: " + ", ".join(bad)])
            if bad:
                fails.append("%s (%s)" % (pid, ", ".join(bad)))
    a.result("Sprinklers", "%d, on %d branch line(s) running along %s" % (len(points),
                                                                         len(lines), axis))
    a.table("Each sprinkler, as NFPA 13 measures it", ("sprinkler", "S m", "L m", "area m2",
                                                      "end wall m", "nearest m", "result"), rows)
    if fails:
        a.check("FAIL", "%d of %d sprinklers break a limit: %s" % (
            len(fails), len(points), "; ".join(fails[:12])))
    else:
        a.check("OK", "every sprinkler is within %s m, %s m2 and %s m to a wall"
                % (_g(smax), _f(amax, 2), _g(wmax)))
    _limits_not_checked(a, smin, wmin, family)
    worst, wx, wy, _samples = farthest_point(outline, points, 250.0)
    reach = math.sqrt(amax / 2.0)
    if worst > reach * 1000.0 + 1e-6:
        a.check("WARN", "a point at x %s y %s mm is %s m from every sprinkler - more than "
                        "the %s m half-diagonal of the largest square module allowed; a part "
                        "of the room may have no sprinkler over it" % (
                            _f(wx, 0), _f(wy, 0), _f(worst / 1000.0, 2), _f(reach, 2)))
    _check_standard(a, held["sprinkler_standard"], "the spacing figures offered are", family)
    a.uses("S: along the branch line, the larger of the distances to the next sprinkler "
           "either way, or twice the distance to the wall for an end sprinkler; L: the same "
           "between branch lines; area = S x L - NFPA 13's protection area of coverage")
    a.uses("walls found by casting a line from each sprinkler along and across its branch; "
           "an obstruction is not a wall here - CHECK_OBSTRUCTIONS finds those")
    a.cite(_spacing_source(family))
    a.into_revit("REPORT_COVERAGE and CHECK_OBSTRUCTIONS read the same sprinklers in Revit; "
                 "a sprinkler that fails is moved with the move fragments, after the modeller "
                 "says where")


# --- pipe schedule ---------------------------------------------------------------

def _schedule_table(family, above_below, wide):
    """The pipe schedule table name a question reads, or None."""
    if family == "light hazard":
        return "pipe_schedule_light_above_below" if above_below else "pipe_schedule_light"
    if family == "ordinary hazard":
        if above_below:
            return "pipe_schedule_ordinary_above_below"
        return "pipe_schedule_ordinary_wide" if wide else "pipe_schedule_ordinary"
    return None


def schedule_size(table, material, count):
    """
    (size, why not, kind) - the smallest nominal size the table allows for
    `count` sprinklers in `material`, or None with the reason and its kind:
    "limits" where NFPA 13 hands over to the system's area limits, "beyond"
    past the table's last size, "not held" at a cell left out because it could
    not be checked.

    A cell left out still bounds itself: every schedule carries strictly more
    sprinklers on each larger size, so an unread cell carries fewer than the
    next one held. A count at or above that next figure cannot be the unread
    size's, and the search goes on past it; a count below it is undecided, and
    is refused rather than guessed.
    """
    data = REFERENCES[table]
    col = data["columns"].index("%s sprinklers" % material)
    rows = data["rows"]
    for i, row in enumerate(rows):
        allowed = row[col]
        if allowed is None:
            area = lookup("pipe_schedule_rules", "area one system may protect")
            return None, ("%s is where the table hands over to the area one system may "
                          "protect%s" % (nominal_text(row[0]), "" if area is None
                                         else " - %s" % area[1])), "limits"
        if allowed == NOT_HELD:
            later = [r[col] for r in rows[i + 1:] if r[col] not in (None, NOT_HELD)]
            if later and count >= later[0]:
                continue
            return None, ("the %s %s cell of this table was not read in this work, so "
                          "Heron cannot say whether it carries %d - read it in your copy"
                          % (nominal_text(row[0]), material, count)), "not held"
        if count <= allowed:
            return row[0], None, None
    return None, "the table holds no size for %d" % count, "beyond"


def _largest(table, material):
    """The largest size a table holds a figure for, and that figure."""
    data = REFERENCES[table]
    col = data["columns"].index("%s sprinklers" % material)
    held = [r for r in data["rows"] if r[col] not in (None, NOT_HELD)]
    return held[-1][0], held[-1][col]


def _next_size(label):
    i = _nominal_order(label)
    return NOMINALS[i + 1][0] if i + 1 < len(NOMINALS) else None


@calculation("pipe_schedule", "Pipe size by NFPA 13's pipe schedule - incl. above and below a ceiling", "pipe")
def calc_pipe_schedule(a):
    """The pipe size NFPA 13's pipe schedule gives for the number of sprinklers a pipe feeds - light or ordinary hazard, steel or copper, sprinklers above and below a ceiling on common branch lines, and ordinary hazard at more than 3.7 m spacing - with the branch line limit checked; a table answered as the table, not a hydraulic design."""
    held = project_standards(a, ("sprinkler_standard", "fire_authority"))
    family = family_of(held["sprinkler_standard"])
    if family in ("en", "fm"):
        _read_all(a, "hazard", "material", "pipes", "sprinklers", "above_and_below_ceiling",
                  "spacing_over_3_7m", "most_on_a_branch", "above", "below")
        a.refuse("NFPA 13's pipe schedule is NFPA's, and this project follows %s - %s"
                 % (held["sprinkler_standard"], "a pre-calculated BS EN 12845 system is sized "
                    "from that standard's own tables, which were not read in this work "
                    "(`reference en12845_precalculated` holds its design points); calculate "
                    "it with `hydraulic`" if family == "en" else "an FM Global system is "
                    "hydraulically calculated - use `hydraulic`"))
        return
    hazard = _hazard(a, "the occupancy hazard class, as the engineer classified it - it picks "
                     "the schedule", held)
    material = a.choice("material", ("steel", "copper"), "the pipe material - the schedules "
                        "differ for steel and copper tube")
    pipes = a.records("pipes", "each pipe: {id, sprinklers} - the sprinklers it feeds; for "
                      "sprinklers above and below a ceiling {id, above, below}", required=False)
    count = a.integer("sprinklers", "the number of sprinklers the pipe feeds - or give pipes",
                      1, 100000, required=False)
    above_below = a.flag("above_and_below_ceiling", "true where sprinklers above AND below a "
                         "ceiling are fed from common branch lines or one cross main")
    wide = a.flag("spacing_over_3_7m", "true where sprinklers on a branch line, or the branch "
                  "lines, are more than 3.7 m (12 ft) apart - ordinary hazard only")
    per_branch = a.integer("most_on_a_branch", "the most sprinklers on any branch line on "
                           "either side of a cross main - for the branch line limit", 1, 1000,
                           required=False)
    entries = []
    if pipes is None and count is None and not a.refused and not above_below:
        a.need("sprinklers", "count (or pipes)", "how many sprinklers the pipe feeds - or "
               "pipes, a list of {id, sprinklers}")
    if pipes is not None and count is not None:
        a.refuse("give sprinklers for one pipe OR pipes for many - not both")
    if pipes is not None:
        for v in pipes:
            pid = v.text("id") or v.name("id")
            if above_below:
                up = v.integer("above", "sprinklers above the ceiling this pipe feeds", 0, 100000)
                down = v.integer("below", "sprinklers below the ceiling this pipe feeds", 0,
                                 100000)
                entries.append((pid, None, up, down))
            else:
                n = v.integer("sprinklers", "the sprinklers this pipe feeds", 1, 100000)
                entries.append((pid, n, None, None))
    elif above_below:
        up = a.integer("above", "sprinklers above the ceiling the pipe feeds", 0, 100000)
        down = a.integer("below", "sprinklers below the ceiling the pipe feeds", 0, 100000)
        if count is not None:
            a.refuse("with sprinklers above and below a ceiling give above and below, not "
                     "sprinklers - the table counts the two levels together")
        entries.append(("the pipe", None, up, down))
    elif count is not None:
        entries.append(("the pipe", count, None, None))
    if a.incomplete():
        return
    family = _spacing_family(hazard)
    if family == "extra hazard":
        a.refuse("extra hazard is hydraulically calculated - NFPA 13's pipe schedule serves "
                 "only additions to an existing extra hazard pipe schedule system; use "
                 "`hydraulic`")
        return
    if wide and family != "ordinary hazard":
        a.refuse("the more-than-3.7 m schedule is ordinary hazard's - this is %s" % hazard)
        return
    if above_below and wide:
        a.refuse("NFPA 13 gives the above-and-below-ceiling schedule and the wide-spacing "
                 "schedule separately - say which governs this pipe")
        return
    table = _schedule_table(family, above_below, wide)
    regular = _schedule_table(family, False, False)
    rows = []
    sizes = collections.OrderedDict()
    for pid, n, up, down in entries:
        if above_below:
            total = up + down
            if total == 0:
                a.refuse("%s feeds no sprinklers" % pid)
                return
            size, why, kind = schedule_size(table, material, total)
            how = "%d above + %d below = %d, from the above-and-below table" % (up, down, total)
            if size is None:
                top, top_count = _largest(table, material)
                if kind != "beyond":
                    a.refuse("%s: %s" % (pid, why))
                    return
                step = _next_size(top)
                bigger, why2, _kind = schedule_size(regular, material, max(up, down))
                if bigger is None:
                    a.refuse("%s: %s" % (pid, why2))
                    return
                size = max((step, bigger), key=_nominal_order)
                how = ("%d above + %d below = %d, more than the %d the above-and-below table "
                       "allows %s: increased to %s, and sized by the %s schedule for the "
                       "larger level, %d" % (up, down, total, top_count, nominal_text(top),
                                             nominal_text(step), family, max(up, down)))
        else:
            size, why, _kind = schedule_size(table, material, n)
            how = "%d sprinklers" % n
            if size is None:
                a.refuse("%s: %d sprinklers - %s; calculate it" % (pid, n, why))
                return
        rows.append([pid, how, nominal_text(size)])
        sizes.setdefault(size, []).append(pid)
    a.table("Pipe sizes from %s" % REFERENCES[table]["title"], ("pipe", "counted", "size"), rows)
    if len(rows) == 1:
        a.result("Size", "%s %s" % (nominal_text(list(sizes)[0]), material))
    limit = lookup("pipe_schedule_rules", "branch lines")
    if per_branch is not None:
        if per_branch > 8:
            a.check("FAIL", "%d sprinklers on a branch line on one side of the cross main - "
                            "NFPA 13's pipe schedule allows 8%s (%s)" % (
                                per_branch, " above and 8 below" if above_below else "",
                                limit[1] if limit else ""))
        else:
            a.check("OK", "%d on a branch line on one side of the cross main, within the 8 "
                          "NFPA 13's pipe schedule allows%s" % (
                              per_branch, " above and 8 below" if above_below else ""))
    else:
        a.check("WARN", "the branch line limit NOT CHECKED - give most_on_a_branch: NFPA 13's "
                        "pipe schedule allows 8 sprinklers on a branch line either side of a "
                        "cross main%s" % (" (8 above and 8 below)" if above_below else ""))
    if above_below:
        rule = lookup("pipe_schedule_rules", "above and below a ceiling")
        if rule:
            a.uses("%s: %s" % (rule[0], rule[1]))
    where = lookup("pipe_schedule_rules", "where it may be used")
    a.check("WARN", "the pipe schedule is a TABLE, not a hydraulic design: NFPA 13 limits "
                    "where a new system may use it - %s - and an authority may require every "
                    "system calculated" % (where[1] if where else "see its limits"))
    _check_standard(a, held["sprinkler_standard"], "the schedule used is", "nfpa")
    _check_authority(a, held["fire_authority"], "whether a pipe schedule system is accepted")
    a.cite(REFERENCES[table]["source"])
    a.into_revit("SET_MEP_SIZE per size: %s" % "; ".join(
        "%s - %s" % (nominal_text(s), ", ".join(ids)) for s, ids in sizes.items()))


# --- obstructions ----------------------------------------------------------------

SPRINKLER_TYPES = ("standard-upright-pendent", "standard-sidewall", "extended-coverage",
                   "residential", "esfr", "cmsa")


def beam_rule(distance_mm):
    """The row of the beam rule table for a horizontal distance A in mm, or None past it."""
    for row in REFERENCES["beam_rule_standard"]["rows"]:
        lo, hi = row[1], row[2]
        if distance_mm >= lo - 1e-9 and (hi is None or distance_mm < hi - 1e-9):
            return row
    return None


@calculation("obstruction", "Sprinkler near an obstruction - the beam rule and the three-times rule", "layout")
def calc_obstruction(a):
    """How high a standard upright or pendent sprinkler's deflector may sit above the bottom of a beam or duct at its distance from it (NFPA 13's beam rule) - or how far away it must be for a given height - how far it must stand from a column, pipe or light (the three-times rule), and whether an obstruction is wide enough to need sprinklers under it. The tables answered as the tables."""
    held = project_standards(a, ("sprinkler_standard",))
    if family_of(held["sprinkler_standard"]) in ("en", "fm"):
        _read_all(a, "sprinkler_type", "distance_to_obstruction_mm", "deflector_above_bottom_mm",
                  "obstruction_width_mm", "clear_distance_mm")
        a.refuse("the beam rule and the three-times rule held here are NFPA 13's, and this "
                 "project follows %s, whose own obstruction rules were not read in this work"
                 % held["sprinkler_standard"])
        return
    kind = a.choice("sprinkler_type", SPRINKLER_TYPES, "the sprinkler's type - each type has "
                    "its own obstruction rules")
    dist = a.number("distance_to_obstruction_mm", "mm", "horizontal distance from the "
                    "sprinkler to the side of the beam or duct, A", 0, 100000, required=False)
    above = a.number("deflector_above_bottom_mm", "mm", "how far the deflector sits ABOVE the "
                     "bottom of the beam or duct, B - 0 or negative when it is level or below",
                     -10000, 10000, required=False)
    width = a.number("obstruction_width_mm", "mm", "a column's, pipe's or light's widest "
                     "dimension - for the three-times rule and the wide-obstruction rule",
                     1, 100000, required=False)
    clear = a.number("clear_distance_mm", "mm", "horizontal distance from the sprinkler to "
                     "that column, pipe or light", 0, 100000, required=False)
    if dist is None and above is None and width is None and not a.refused:
        a.need("distance_to_obstruction_mm", "mm (or deflector_above_bottom_mm, or "
               "obstruction_width_mm)", "what to check - a beam or duct by its distance and "
               "the deflector's height above its bottom, or a column, pipe or light by its "
               "width")
    if clear is not None and width is None:
        a.need("obstruction_width_mm", "mm", "the obstruction's widest dimension - the "
               "three-times rule is three times it")
    if a.incomplete():
        return
    if kind != "standard-upright-pendent":
        a.refuse("Heron holds the obstruction rules of standard upright and pendent spray "
                 "sprinklers only - a %s sprinkler has its own in NFPA 13 and in its listing"
                 % kind)
        return
    beam = REFERENCES["beam_rule_standard"]
    last = beam["rows"][-1]
    if dist is not None:
        row = beam_rule(dist)
        if row is None:
            a.result("Beam rule at A = %s mm" % _g(dist), "past the %s mm the table held here "
                     "reaches - the sources disagree beyond it; read the allowance in your "
                     "copy" % _g(last[2]))
            if above is not None:
                if above <= last[3] + 1e-9:
                    a.check("OK", "B = %s mm is within %s mm, what the beam rule allows at %s - "
                                  "it allows at least that this far out" % (
                                      _g(above), _g(last[3]), last[0]))
                else:
                    a.check("WARN", "NOT CHECKED: B = %s mm at A = %s mm is past the rows "
                                    "held here - read the allowance in your copy"
                            % (_g(above), _g(dist)))
        else:
            allowed = row[3]
            a.result("Beam rule at A = %s mm" % _g(dist), "the deflector may sit at most %s mm "
                     "above the bottom of the obstruction (%s)" % (_g(allowed), row[0]))
            if above is not None:
                if above <= allowed + 1e-9:
                    a.check("OK", "B = %s mm is within the %s mm the beam rule allows at A = "
                                  "%s mm" % (_g(above), _g(allowed), _g(dist)))
                else:
                    a.check("FAIL", "B = %s mm is above the %s mm the beam rule allows at A = "
                                    "%s mm - move the sprinkler away from the obstruction, or "
                                    "lower it" % (_g(above), _g(allowed), _g(dist)))
    if above is not None and dist is None:
        if above <= 0:
            a.result("Beam rule", "a deflector level with or below the bottom of the "
                     "obstruction is clear of the beam rule at any distance")
        else:
            need = None
            for row in beam["rows"]:
                if row[3] >= above - 1e-9:
                    need = row
                    break
            if need is None:
                a.result("Beam rule", "%s mm needs more than %s mm of distance - past the rows "
                         "held here; read the rest in your copy" % (_g(above), _g(last[1])))
            else:
                a.result("Beam rule", "a deflector %s mm above the bottom needs the sprinkler "
                         "at least %s mm from the side of the obstruction (%s)"
                         % (_g(above), _g(need[1]), need[0]))
    if beam.get("note"):
        a.uses(beam["note"])
    if width is not None:
        rule = REFERENCES["obstruction_rules"]
        cap = lookup("obstruction_rules", "three-times rule")
        cap_mm = cap[2] if cap else None
        need3 = 3.0 * width if cap_mm is None else min(3.0 * width, cap_mm)
        a.result("Three-times rule", "%s mm clear from a %s mm obstruction%s" % (
            _g(round(need3, 1)), _g(width),
            "" if cap_mm is None or 3.0 * width <= cap_mm else
            " - three times it, but never more than %s mm" % _g(cap_mm)))
        if clear is not None:
            if clear >= need3 - 1e-9:
                a.check("OK", "%s mm clear meets the %s mm the three-times rule needs"
                        % (_g(clear), _g(round(need3, 1))))
            else:
                a.check("FAIL", "%s mm clear is less than the %s mm the three-times rule needs"
                        % (_g(clear), _g(round(need3, 1))))
        wide = lookup("obstruction_rules", "wide obstructions")
        if wide and width > wide[2] + 1e-9:
            a.check("WARN", "a %s mm wide obstruction is wider than %s mm - NFPA 13 asks for "
                            "sprinklers UNDER a fixed obstruction that wide (a duct, a deck, "
                            "a platform), measured across its lesser horizontal side"
                    % (_g(width), _g(wide[2])))
        for r in rule["rows"]:
            a.uses("%s: %s" % (r[0], r[1]))
        a.cite(rule["source"])
    _check_standard(a, held["sprinkler_standard"], "the tables used are", "nfpa")
    a.cite(beam["source"])
    a.into_revit("CHECK_OBSTRUCTIONS finds which sprinklers have a beam or duct within a "
                 "distance; CHECK_VERTICAL_CLEARANCE measures the gap between services")


# --- what NFPA 13's annex lists --------------------------------------------------

_STOP = {"the", "and", "of", "for", "a", "an", "in", "with", "area", "areas", "room",
         "rooms", "space", "spaces", "building", "buildings", "including", "or", "to", "on",
         "other", "than", "use", "used", "only"}


# A modeller's word for a space beside the word NFPA's annex uses for it. Only
# which rows are SHOWN changes; nothing here classifies anything.
_SAME = {"car": ("automobile",), "carpark": ("automobile", "parking"),
         "kitchen": ("restaurant", "service"), "canteen": ("restaurant", "service"),
         "school": ("educational",), "classroom": ("educational",),
         "clinic": ("hospital",), "ward": ("hospital",), "mosque": ("church",),
         "mall": ("mercantile", "retail"), "shop": ("mercantile", "retail"),
         "workshop": ("machine",), "hangar": ("aircraft",)}


def _words(text, widen=False):
    out = set()
    for w in _norm(text).replace(",", " ").replace("/", " ").replace("(", " ") \
            .replace(")", " ").split():
        w = w.strip(".;:")
        if not w or w in _STOP:
            continue
        w = w[:-1] if len(w) > 4 and w.endswith("s") else w
        out.add(w)
        if widen:
            out.update(_SAME.get(w, ()))
    return out


def _hits(wanted, have):
    """How many asked words an example holds - whole, or one the start of the other."""
    hit = 0
    for w in wanted:
        if any(w == h or (min(len(w), len(h)) >= 4 and (h.startswith(w) or w.startswith(h)))
               for h in have):
            hit += 1
    return hit


@calculation("hazard_class", "Occupancy hazard class - what the lists hold, and what each class asks for", "hazard")
def calc_hazard_class(a):
    """Which occupancy hazard class NFPA 13's annex - or the owner's own file - lists an occupancy under, and the design figures each class carries in the standard the project follows: NFPA 13, BS EN 12845 or FM Global. It never classifies: the class is the engineer's and the authority's, and the annex is guidance, not a rule."""
    held = project_standards(a, ("sprinkler_standard", "fire_authority"))
    family = figures_of(held)
    occupancy = a.word("occupancy", "what the space is used for - an office, a car park, a "
                       "commercial kitchen", required=False)
    hazard = _hazard(a, "a class already decided - to show what it asks for", held,
                     required=False)
    if occupancy is None and hazard is None and not a.refused:
        a.need("occupancy", "text (or hazard)", "the occupancy to look up - or a hazard class "
               "already decided, to see its figures")
    if a.incomplete():
        return
    classes = []
    examples = REFERENCES["hazard_examples"]
    if occupancy is not None:
        wanted = _words(occupancy, widen=True)
        scored = []
        for row in examples["rows"]:
            hit = _hits(wanted, _words(row[1]))
            if hit:
                scored.append((hit, row))
        scored.sort(key=lambda t: (-t[0], list(HAZARDS).index(t[1][0])))
        if scored:
            shown = [r for _h, r in scored][:12]
            a.table("What the lists hold beside %r" % occupancy,
                    ("class", "example", "listed by"), [[r[0], r[1], r[2]] for r in shown])
            for r in shown:
                if r[0] not in classes:
                    classes.append(r[0])
            if len(classes) > 1:
                a.check("WARN", "%r is listed under %d classes - which one depends on what is "
                                "in the space and how much will burn, and that is the "
                                "engineer's call" % (occupancy, len(classes)))
            if any(r[2] != "NFPA 13 annex" for r in shown):
                a.check("WARN", "a row from the owner's own file is not NFPA 13's annex - where "
                                "the two differ, docs/42 s12 records it and the owner decides")
        else:
            a.result("Lists", "no example matches %r - classify it from the definitions "
                     "below, with the fire consultant" % occupancy)
            classes = list(HAZARDS)
        a.cite(examples["source"])
        if family != "nfpa":
            a.check("WARN", "the examples above are NFPA 13's annex and the owner's file - "
                            "NFPA 13's classes, a comparison: %s's own list of occupancies "
                            "was not read in this work, and the same name is not the same "
                            "class there" % FAMILIES[family][0])
    if family != "nfpa":
        # The project's own standard's classes: the one decided, or all of them.
        classes = [hazard] if hazard is not None else list(HAZARD_SETS[family])
    elif hazard is not None:
        classes = [hazard]
    if family == "nfpa":
        crit = REFERENCES["design_criteria"]
        cols = crit["columns"]

        def at(row, name):
            return _g(row[cols.index(name)])

        a.table("What each class asks for - OFFERED, never applied (%s)" % crit["source"],
                ("class", "density over area", "if concealed spaces are not sprinklered",
                 "hose allowance", "duration min"),
                [[r[0], "%s mm/min over %s m2 (%s gpm/ft2 over %s ft2)" % (
                    at(r, "density mm/min"), at(r, "design area m2"), at(r, "density gpm/ft2"),
                    at(r, "design area ft2")),
                  "%s mm/min over %s m2" % (at(r, "second density mm/min"),
                                            at(r, "second area m2")),
                  "%s L/min (%s gpm)" % (at(r, "hose allowance L/min"),
                                         at(r, "hose allowance gpm")),
                  at(r, "duration min")]
                 for r in crit["rows"] if r[0] in classes])
        defs = REFERENCES["hazard_definitions"]
    else:
        crit = REFERENCES["en12845_criteria" if family == "en" else "fm_criteria"]
        a.table("What each class asks for - OFFERED, never applied (%s)" % crit["source"],
                crit["columns"], [r for r in crit["rows"] if r[0] in classes])
        defs = REFERENCES["en12845_hazards" if family == "en" else "fm_hazards"]
    if crit.get("note"):
        a.uses(crit["note"])
    shown_defs = [r for r in defs["rows"] if any(_class_of(r[0], c) for c in classes)]
    a.table("The classes, in Heron's words after %s's definitions" % FAMILIES[family][0],
            defs["columns"], shown_defs)
    a.check("WARN", "Heron does not classify. The class is the engineer's and the authority's "
                    "- the annex's examples are guidance, and what is stored, and how high, "
                    "decides the class, not the room's name")
    _check_standard(a, held["sprinkler_standard"], "the classes and the figures shown are",
                    family)
    _check_authority(a, held["fire_authority"], "the classification of a building or an "
                     "occupancy")
    a.cite(defs["source"], crit["source"])
    a.into_revit("the class a modeller confirms can be written to the Spaces with the "
                 "parameter fragments, so every later answer reads it from the model")


def _class_of(defined, key):
    """Whether a definitions row - one class, or a group such as OH - covers a class key."""
    if defined == key:
        return True
    return (defined in ("OH", "HHP", "HHS") and key.startswith(defined)
            and key[len(defined):].isdigit())


# --- the pump --------------------------------------------------------------------

def _curve(points, q):
    """A pump's pressure at q by the parabola through its three test points."""
    (x0, y0), (x1, y1), (x2, y2) = points
    return (y0 * (q - x1) * (q - x2) / ((x0 - x1) * (x0 - x2))
            + y1 * (q - x0) * (q - x2) / ((x1 - x0) * (x1 - x2))
            + y2 * (q - x0) * (q - x1) / ((x2 - x0) * (x2 - x1)))


@calculation("fire_pump", "Fire pump against NFPA 20's curve rules and a demand", "supply")
def calc_fire_pump(a):
    """Checks a fire pump's curve against NFPA 20's two shape rules - churn no more than 140 % of rated pressure, at least 65 % of it at 150 % of rated flow - and whether it meets a system demand from the suction it starts with; lists NFPA 20's rated capacities at or above the demand."""
    rated_q = flow_lpm(a, "the pump's rated flow, from its data sheet", prefix="rated_flow")
    rated_p = pressure_bar(a, "the pump's rated net pressure at that flow", prefix="rated_pressure")
    churn = pressure_bar(a, "the pump's net pressure at no flow - churn", prefix="churn_pressure")
    p150 = pressure_bar(a, "the pump's net pressure at 150 % of rated flow, from its curve",
                        prefix="pressure_at_150")
    dq = flow_lpm(a, "the system's demand at the pump discharge, with hose streams",
                  prefix="demand_flow", required=False)
    dp = pressure_bar(a, "the pressure the system needs at the pump discharge at that flow",
                      prefix="demand_pressure", required=False)
    suction = pressure_bar(a, "the residual pressure at the pump suction at the demand - 0 "
                           "for a tank at pump level", prefix="suction_pressure",
                           required=False, low=-1.0)
    suction_static = pressure_bar(a, "the HIGHEST static suction pressure, for the pressure "
                                  "at churn", prefix="suction_static", required=False, low=-1.0)
    pmax = pressure_bar(a, "the pressure the system's components are rated to, for churn",
                        prefix="max_system_pressure", required=False)
    if (dq is None) != (dp is None) and not a.refused:
        a.need("demand_pressure_bar" if dp is None else "demand_flow_lpm",
               "bar" if dp is None else "L/min", "a demand is a flow AND the pressure at it")
    if dq is not None and suction is None:
        a.need("suction_pressure_bar", "bar", "the suction the pump starts from at the demand "
               "- 0 for a tank at pump level, or the residual of a pressurised supply")
    held = project_standards(a, ("fire_authority",))
    if a.incomplete():
        return
    if not (churn > rated_p > p150 >= 0):
        a.refuse("a pump curve falls as flow rises - churn %s, rated %s and 150 %% %s do not"
                 % (_g(churn), _g(rated_p), _g(p150)))
        return
    curve = [(0.0, churn), (rated_q, rated_p), (1.5 * rated_q, p150)]
    rules = REFERENCES["fire_pump_rules"]
    a.result("Rated point", "%s at %s" % (flow_text(rated_q), pressure_text(rated_p)))
    a.check("OK" if churn <= 1.40 * rated_p + 1e-12 else "FAIL",
            "churn %s is %s %% of rated - NFPA 20 allows at most 140 %%"
            % (pressure_text(churn), _f(100.0 * churn / rated_p, 1)))
    a.check("OK" if p150 >= 0.65 * rated_p - 1e-12 else "FAIL",
            "at 150 %% of rated flow the pump gives %s, %s %% of rated - NFPA 20 asks at least "
            "65 %%" % (pressure_text(p150), _f(100.0 * p150 / rated_p, 1)))
    if dq is not None:
        if dq > 1.5 * rated_q + 1e-9:
            a.check("FAIL", "the demand %s is beyond 150 %% of the rated flow - past the curve "
                            "NFPA 20 holds a pump to" % flow_text(dq))
        else:
            net = _curve(curve, dq)
            at = net + suction
            a.result("At the demand", "the pump adds %s; with %s suction, %s at the discharge"
                     % (pressure_text(net), pressure_text(suction), pressure_text(at)))
            a.check("OK" if at >= dp - 1e-12 else "FAIL",
                    "%s at the discharge against the %s the system needs - %s"
                    % (pressure_text(at), pressure_text(dp),
                       "a margin of %s" % pressure_text(at - dp) if at >= dp
                       else "SHORT by %s" % pressure_text(dp - at)))
            a.result("Demand as a share of rated", "%s %%" % _f(100.0 * dq / rated_q, 0))
        ratings = REFERENCES["fire_pump_ratings"]
        above = [r for r in ratings["rows"] if r[1] >= dq - 1e-9][:4]
        if above:
            a.table("NFPA 20's rated capacities at or above the demand - OFFERED, the "
                    "selection is the engineer's", ratings["columns"], above)
    if suction_static is not None:
        top = churn + suction_static
        a.result("Highest pressure", "%s - churn plus the highest static suction"
                 % pressure_text(top))
        if pmax is not None:
            a.check("OK" if top <= pmax + 1e-12 else "FAIL",
                    "%s against the %s the components are rated to%s" % (
                        pressure_text(top), pressure_text(pmax),
                        "" if top <= pmax else " - a pressure relief or a lower-pressure pump"))
    for r in rules["rows"]:
        a.uses("%s: %s" % (r[0], r[1]))
    a.uses("the pump's net pressure between its three points by the parabola through them")
    _check_authority(a, held["fire_authority"], "the pump arrangement (electric, diesel, "
                     "jockey) and the pump room")
    a.cite(rules["source"])


# --- standpipes --------------------------------------------------------------------

@calculation("standpipe", "Standpipe system demand", "supply")
def calc_standpipe(a):
    """The flow and the pressure at its source a standpipe system needs - the first standpipe's flow and each additional one's up to the most a building needs, the residual at the remote outlet, the rise and the friction - and whether a low outlet needs a pressure-regulating device."""
    klass = a.choice("standpipe_class", ("i", "ii", "iii"), "the standpipe class - I (65 mm "
                     "for the fire service), II (40 mm hose for occupants) or III (both)")
    n = a.integer("standpipes", "how many standpipes the system has", 1, 100)
    offered = "%s; BS 9990's wet main: %s; QCDD and the UAE code: `reference qatar`, " \
              "`reference uae`" % (offer_table("standpipe_rules"), offer_table("bs9990_rules"))
    first = flow_lpm(a, "the flow for the first (most remote) standpipe",
                     prefix="first_flow", reference=offered)
    more = None
    if n is not None and n > 1 and klass != "ii":
        more = flow_lpm(a, "the flow for each additional standpipe", prefix="additional_flow",
                        reference=offer_table("standpipe_rules"))
    cap = flow_lpm(a, "the most the whole system must flow - it depends on whether the "
                   "building is sprinklered throughout", prefix="max_total_flow",
                   required=more is not None or (n is not None and n > 1 and klass != "ii"),
                   reference=offer_table("standpipe_rules"))
    outlet = pressure_bar(a, "the residual pressure needed at the most remote outlet",
                          prefix="outlet_pressure", reference=offered)
    rise = a.number("height_m", "m", "how far the most remote outlet is ABOVE the source", -200,
                    1000)
    friction = pressure_bar(a, "the friction loss from the source to that outlet at the "
                            "demand - from `friction_loss` along the path",
                            prefix="friction_loss", required=False)
    static = pressure_bar(a, "the source's pressure at no flow - a pump's churn plus its "
                          "suction - for the pressure at a low outlet", prefix="source_static",
                          required=False)
    low = a.number("lowest_outlet_height_m", "m", "how far the LOWEST outlet is above the "
                   "source", -200, 1000, required=False)
    omax = pressure_bar(a, "the most pressure allowed at an outlet before it needs a "
                        "pressure-regulating device", prefix="max_outlet_pressure",
                        required=False, reference=offered)
    held = project_standards(a, ("fire_authority",))
    if a.incomplete():
        return
    total = first + (n - 1) * (more or 0.0)
    if cap is not None and total > cap:
        a.result("Flow", "%s - %d standpipes would ask %s, held to the %s given"
                 % (flow_text(cap), n, flow_text(total), flow_text(cap)))
        total = cap
    else:
        a.result("Flow", "%s - the first standpipe %s%s" % (
            flow_text(total), flow_text(first),
            "" if not more else " and %d more at %s" % (n - 1, flow_text(more))))
    need = outlet + rise * BAR_PER_M + (friction or 0.0)
    a.result("Pressure at the source", "%s - %s at the outlet, %s for %s m of rise%s" % (
        pressure_text(need), pressure_text(outlet), pressure_text(rise * BAR_PER_M), _g(rise),
        "" if friction is None else ", %s friction" % pressure_text(friction)))
    if friction is None:
        a.assume("no friction loss given, so none added - the source pressure above is short "
                 "by the pipe's whole loss at the demand")
    if static is not None and low is not None:
        at_low = static - low * BAR_PER_M
        a.result("Static at the lowest outlet", pressure_text(at_low))
        if omax is not None:
            a.check("OK" if at_low <= omax + 1e-12 else "FAIL",
                    "%s at the lowest outlet against the %s allowed%s" % (
                        pressure_text(at_low), pressure_text(omax),
                        "" if at_low <= omax else " - a pressure-regulating device is needed "
                        "there"))
    elif omax is not None:
        a.check("WARN", "the outlet pressure limit NOT CHECKED - give source_static_bar and "
                        "lowest_outlet_height_m")
    if held["fire_authority"] == QCDD:
        rule = lookup("qatar", "standpipes and landing valves")
        a.check("WARN", "QCDD is reported to ask %s"
                % (rule[1] if rule else "65 mm landing valves on every level"))
    elif held["fire_authority"] == UAE:
        rule = lookup("uae", "landing valves")
        a.check("WARN", "the UAE code is reported to ask %s"
                % (rule[1] if rule else "a pressure-regulating landing valve above 7 bar"))
    _check_authority(a, held["fire_authority"], "landing valves, hose reels and their "
                     "pressures")
    a.uses("flow = the first standpipe + each additional, to the most given; pressure at the "
           "source = the outlet's residual + rise x 0.0979 bar/m + friction")
    a.cite(REFERENCES["standpipe_rules"]["source"])
    a.into_revit("SET_MEP_SIZE for the standpipe's riser once its size is decided")


# --- hose reels ------------------------------------------------------------------------

@calculation("hose_reels", "Hose reel system demand", "supply")
def calc_hose_reels(a):
    """The flow and the pressure at its source a hose reel system needs - the reels running at once, each one's flow, the pressure at the most remote, the rise and the friction - and the water for the time asked, with what QCDD, the UAE code and BS 5306-1 are reported to ask beside each question."""
    offered = offer_table("hose_reel_rules")
    n = a.integer("reels_operating", "how many reels run at once", 1, 20, reference=offered)
    q = flow_lpm(a, "each reel's flow at the pressure below", prefix="reel_flow",
                 reference=offered)
    p = pressure_bar(a, "the pressure needed at the most remote reel", prefix="reel_pressure",
                     reference=offered)
    rise = a.number("height_m", "m", "how far the most remote reel is ABOVE the source", -200,
                    1000)
    friction = pressure_bar(a, "the friction loss from the source to that reel at the demand "
                            "- from `friction_loss` along the path", prefix="friction_loss",
                            required=False)
    duration = a.number("duration_min", "min", "how long the reels must run", 1, 600,
                        required=False, reference=offered)
    held = project_standards(a, ("fire_authority",))
    if a.incomplete():
        return
    total = n * q
    need = p + rise * BAR_PER_M + (friction or 0.0)
    a.result("Flow", "%s - %d reel%s at %s" % (flow_text(total), n, "" if n == 1 else "s",
                                               flow_text(q)))
    a.result("Pressure at the source", "%s - %s at the reel, %s for %s m of rise%s" % (
        pressure_text(need), pressure_text(p), pressure_text(rise * BAR_PER_M), _g(rise),
        "" if friction is None else ", %s friction" % pressure_text(friction)))
    if duration:
        a.result("Water", "%s over %s min" % (volume_text(total * duration / 1000.0),
                                              _g(duration)))
    if friction is None:
        a.assume("no friction loss given, so none added - the source pressure above is short "
                 "by the pipe's whole loss at the demand")
    if held["fire_authority"] == QCDD:
        rule = lookup("qatar", "hose reels")
        a.check("WARN", "QCDD is reported to ask %s - check the reach with `coverage_check`, "
                        "reached_by 2" % (rule[1] if rule else "two hoses at every point"))
    elif held["fire_authority"] == UAE:
        rule = lookup("uae", "hose reels")
        a.check("WARN", "the UAE code is reported to ask %s"
                % (rule[1] if rule else "4.5 bar at the most remote reel"))
    _check_authority(a, held["fire_authority"], "hose reels, their flow and their pressure")
    a.uses("flow = reels at once x each one's flow; pressure at the source = the reel's + rise x "
           "0.0979 bar/m + friction")
    a.cite(REFERENCES["hose_reel_rules"]["source"])
    a.into_revit("PLACE_FAMILY_INSTANCES at the reels' positions; REPORT_COVERAGE reads what "
                 "they reach")


# --- extinguishers ------------------------------------------------------------------

@calculation("extinguishers", "Portable extinguishers - the least count by floor area", "devices")
def calc_extinguishers(a):
    """The fewest Class A portable extinguishers a floor needs by its area - by NFPA 10, each one's rating times the area a unit of A may cover, never more than the most one may cover; or by BS 5306-8, a combined rating per square metre with a least count and a least combined rating - with the travel distance left to check against their positions."""
    area = area_m2(a, "the floor area to protect", "floor_area")
    by_rating = a.has("rating_per_m2")
    if by_rating:
        rating = a.number("rating_a", "A units", "the extinguisher's Class A rating - 13 for an "
                          "EN 3 13A, from its label", 1, 200)
        per_m2 = a.number("rating_per_m2", "A per m2", "the combined Class A rating each "
                          "square metre of floor asks", 0.001, 1,
                          reference=offer_table("bs5306_8"))
        least_total = a.number("min_total_rating", "A units", "the least combined rating "
                               "on the floor", 1, 10000, required=False,
                               reference=offer_table("bs5306_8"))
        least_count = a.integer("min_count", "the fewest extinguishers on the floor", 1, 100,
                                required=False, reference=offer_table("bs5306_8"))
        if a.has("area_per_a_m2") or a.has("max_area_m2"):
            a.refuse("give rating_per_m2 (BS 5306-8's way) OR area_per_a with max_area "
                     "(NFPA 10's) - two methods, and their ratings are not the same scale")
    else:
        a.optional_input("rating_per_m2", "A per m2", "BS 5306-8's way instead - the combined "
                         "Class A rating each square metre asks")
        rating = a.number("rating_a", "A units", "the extinguisher's Class A rating - 2 for "
                          "2-A, from its label", 1, 40)
        per_unit = area_m2(a, "the floor area one unit of A may cover for this hazard",
                           "area_per_a", reference="%s; or, BS 5306-8's way, give "
                           "rating_per_m2 - %s" % (offer_table("extinguisher_rules"),
                                                   offer_table("bs5306_8")))
        most = area_m2(a, "the most floor area one extinguisher may cover", "max_area",
                       reference=offer_table("extinguisher_rules"))
        least = a.number("min_rating_a", "A units", "the least rating allowed for this hazard",
                         1, 40, required=False, reference=offer_table("extinguisher_rules"))
    travel = a.number("max_travel_m", "m", "the longest walk allowed to an extinguisher",
                      1, 200, required=False, reference=offer_table("coverage_rules"))
    held = project_standards(a, ("fire_authority",))
    if a.incomplete():
        return
    if by_rating:
        asked = max(per_m2 * area, least_total or 0.0)
        n = max(least_count or 1, int(math.ceil(asked / rating - 1e-9)))
        a.result("Extinguishers", "%d at %sA - combined %sA against the %sA asked" % (
            n, _g(rating), _g(n * rating), _f(asked, 1)))
        a.result("Combined rating asked", "%sA - %s A per m2 x %s%s" % (
            _f(per_m2 * area, 1), _g(per_m2), area_text(area),
            "" if not least_total or least_total <= per_m2 * area else
            ", raised to the %sA least given" % _g(least_total)))
        if least_count is None or least_total is None:
            a.check("WARN", "the least count or the least combined rating NOT CHECKED - give "
                            "min_count and min_total_rating: BS 5306-8 asks at least two on a "
                            "storey over 50 m2, combined at least 26A (`reference bs5306_8`)")
        a.uses("extinguishers = the combined rating asked / one extinguisher's rating, rounded "
               "up, and never fewer than the least count given")
        a.cite(REFERENCES["bs5306_8"]["source"])
    else:
        each = min(rating * per_unit, most)
        n = int(math.ceil(area / each - 1e-9))
        a.result("Extinguishers", "%d at %s-A - each covers %s" % (n, _g(rating),
                                                                    area_text(each)))
        if least is not None:
            a.check("OK" if rating >= least - 1e-9 else "FAIL",
                    "a %s-A extinguisher against the %s-A least given" % (_g(rating), _g(least)))
        a.uses("extinguishers = area / min(rating x area per unit of A, most per extinguisher), "
               "rounded up")
        a.cite(REFERENCES["extinguisher_rules"]["source"])
    if travel is not None:
        a.check("WARN", "the count by area is a MINIMUM - the %s m travel distance usually "
                        "needs more: place them, then run `coverage_check` with their points "
                        "and radius_m %s" % (_g(travel), _g(travel)))
    else:
        a.check("WARN", "the travel distance NOT CHECKED - it usually decides the count, not "
                        "the area")
    _check_authority(a, held["fire_authority"], "extinguisher types, sizes and travel distance")
    a.into_revit("PLACE_FAMILY_INSTANCES or PLACE_FAMILY_ON_FACE at the positions chosen")


# --- detectors -------------------------------------------------------------------------

_RADIUS_OFFER = ("NFPA 72's second way: every point of the ceiling within 0.7 S - 6.4 m at S = "
                 "9.1 m (`reference detector_rules`); BS 5839-1: 7.5 m for a point smoke "
                 "detector, 5.3 m for a point heat detector (`reference bs5839_rules`) - offer "
                 "the one the project follows, do not assume it")


@calculation("detector_layout", "Smoke or heat detector count and layout for a room", "devices")
def calc_detector_layout(a):
    """How many spot smoke or heat detectors a rectangular room's flat ceiling needs and where - either no more than a spacing apart and a wall distance from a wall (NFPA 72's first way), or every point within a radius of one (NFPA 72's 0.7 S, BS 5839-1's 7.5 m and 5.3 m) - with a heat detector's spacing reduced for a high ceiling where a factor is given, and the least wall distance and the ceiling height checked where given."""
    length = a.number("room_length_m", "m", "the room's length, along x", 0.3, 1000)
    width = a.number("room_width_m", "m", "the room's width, along y", 0.3, 1000)
    kind = a.choice("detector_type", ("smoke", "heat"), "smoke or heat")
    by_radius = a.has("radius_m")
    s = wall = r = None
    if by_radius:
        if a.has("spacing_m") or a.has("max_wall_distance_m"):
            a.refuse("give radius_m, OR spacing_m with max_wall_distance_m - two ways of "
                     "spacing detectors, and one decides the layout")
        r = a.number("radius_m", "m", "the most distance from any point of the ceiling to a "
                     "detector", 1, 30, reference=_RADIUS_OFFER)
    else:
        a.optional_input("radius_m", "m", "every point of the ceiling within it - instead of "
                         "spacing_m and max_wall_distance_m")
        s = a.number("spacing_m", "m (or radius_m)", "the detector's spacing on a smooth flat "
                     "ceiling - the listed spacing for a heat detector; or give radius_m, "
                     "every point within it", 1, 30,
                     reference="%s; or, by a radius, %s" % (offer_table("detector_rules"),
                                                            _RADIUS_OFFER))
        wall = a.number("max_wall_distance_m", "m", "the most allowed from a detector to a "
                        "wall", 0.1, 30, reference=offer_table("detector_rules"))
    wmin = a.number("min_wall_distance_m", "m", "the least distance from a detector to a wall",
                    0.0, 10, required=False,
                    reference=_rules_offer("bs5839_rules", ("from a wall or obstruction",)))
    factor = a.number("height_factor", "multiplier", "a heat detector's spacing reduction for "
                      "the ceiling height - 1 if none", 0.1, 1.0, required=False,
                      reference=offer_table("heat_detector_height"))
    height = a.number("ceiling_height_m", "m", "the ceiling's height - to check against the "
                      "highest a detector may serve", 1, 100, required=False)
    hmax = a.number("max_ceiling_height_m", "m", "the highest ceiling this detector may serve",
                    1, 100, required=height is not None,
                    reference=_rules_offer("bs5839_rules", (
                        "highest ceiling, general / for up to 10 % of the area",)))
    held = project_standards(a, ("fire_authority",))
    if a.incomplete():
        return
    if factor is not None and kind != "heat":
        a.refuse("the height factor reduces a HEAT detector's spacing - this is a smoke detector")
        return
    f = factor or 1.0
    if by_radius:
        reach = r * f
        found = grid(length, width, rmax=reach, wmin=wmin)
    else:
        used, wall_used = s * f, wall * f
        found = grid(length, width, smax=used, wmax=wall_used, wmin=wmin)
    if found is None:
        a.refuse("no grid meets those limits in a %s x %s m room%s" % (
            _g(length), _g(width), "" if wmin is None else " - the least wall distance "
            "may leave no room for one"))
        return
    rows, cols = found
    sx, sy = length / cols, width / rows
    far = math.hypot(sx, sy) / 2.0
    a.result("Detectors", "%d - %d along the length x %d across" % (rows * cols, cols, rows))
    a.result("Spacing", "%s m along x, %s m along y%s" % (
        _f(sx, 2), _f(sy, 2), "" if not factor else " (%s %s m x %s for the height)"
        % ("radius" if by_radius else "spacing", _g(r if by_radius else s), _g(factor))))
    a.result("Farthest point", "%s m from a detector - half a module's diagonal" % _f(far, 2))
    if by_radius:
        a.check("OK", "every point of the ceiling within %s m of a detector - the farthest "
                      "%s m" % (_f(reach, 2), _f(far, 2)))
    else:
        a.check("OK", "spacing within %s m and walls within %s m" % (_f(used, 2),
                                                                     _f(wall_used, 2)))
    if wmin is not None:
        a.check("OK", "every detector at least %s m from a wall - the nearest %s m"
                % (_g(wmin), _f(min(sx, sy) / 2.0, 2)))
    else:
        a.check("WARN", "the least distance to a wall NOT CHECKED - give min_wall_distance_m; "
                        "BS 5839-1 asks at least 500 mm (`reference bs5839_rules`)")
    if height is not None:
        a.check("OK" if height <= hmax + 1e-9 else "FAIL",
                "a %s m ceiling against the %s m highest given%s" % (
                    _g(height), _g(hmax), "" if height <= hmax + 1e-9 else
                    " - a detector made for a higher ceiling, or another kind of detection"))
    a.check("WARN", "a smooth flat ceiling is assumed - beams, joists, a sloped or a high "
                    "ceiling change it: NFPA 72's reductions in `reference detector_rules`, "
                    "BS 5839-1 treats a beam deeper than 10 % of the ceiling height as a wall")
    _check_authority(a, held["fire_authority"], "detector spacing and the fire alarm design")
    a.table("Detector points from the room corner (mm)", ("#", "x", "y"),
            [[k + 1, "%.0f" % px, "%.0f" % py]
             for k, (px, py) in enumerate(grid_points(length, width, rows, cols))])
    a.uses("a grid of rows x cols, each detector centred in its module - the fewest first")
    if by_radius:
        a.uses("by a radius: half each module's diagonal no more than the radius, so no point "
               "is farther - NFPA 72's 0.7 S and BS 5839-1's radii are both this rule")
        a.cite(REFERENCES["detector_rules"]["source"], REFERENCES["bs5839_rules"]["source"])
    else:
        a.uses("NFPA 72 allows either of two ways for smoke detectors: no more than S apart and "
               "S/2 from a wall (this layout), OR every point of the ceiling within 0.7 S of a "
               "detector - give radius_m for the second")
        a.cite(REFERENCES["detector_rules"]["source"])
    a.into_revit("PLACE_FAMILY_INSTANCES or PLACE_FAMILY_ON_FACE on the ceiling at these "
                 "points; REPORT_COVERAGE to check what was placed")


# --- reference ----------------------------------------------------------------------

@calculation("reference", "Reference tables from the standards", "reference")
def calc_reference(a):
    """The cited fire protection tables Heron holds - NFPA 13's hazard examples, design criteria, spacing, pipe schedules (above and below a ceiling too), the beam rule, C values, K-factors, fittings and temperature ratings; BS EN 12845's and FM Global's design figures; pipe bores, pumps, standpipes, hose reels, extinguishers and detectors to NFPA, BS 5839-1, BS 5306 and BS 9990; and what is known of Qatar and the UAE - each with how far it was checked, shown so a figure can be OFFERED, never applied by itself."""
    BASE.show_reference(a, REFERENCES)


# ---------------------------------------------------------------------------
# Sources. Cited by document, edition and section - never quoted at length.
# ---------------------------------------------------------------------------

SRC_K = ("q = K sqrt(p) - a sprinkler's K-factor as NFPA 13's hydraulic method uses it, "
         "the orifice relation every listing's K is measured by")
SRC_HW = ("NFPA 13 (2019, 2022) Chapter 28, Plans and Calculations - Hazen-Williams friction, "
          "4.52 Q^1.85 / (C^1.85 d^4.87) psi per foot, 6.05 x 10^5 in SI; tests/test_fire.py "
          "derives both from the general Hazen-Williams equation")
SRC_DESIGN_AREA = ("NFPA 13-2022 Section 19.2.3 - the density/area method, Table 19.2.3.1.1's "
                   "single design points for a new system (2016 Figure 11.2.3.1.1 and 2019 "
                   "Figure 19.3.3.1.1 printed curves), 19.2.3.1.5 for unsprinklered concealed "
                   "spaces")
SRC_SPACING = ("NFPA 13-2022 Section 10.2, standard pendent and upright spray sprinklers - "
               "protection areas Table 10.2.4.2.1(a) to (d), distances 10.2.5 (2016: Table "
               "8.6.2.2.1 and 8.6)")
SRC_STORAGE = ("NFPA 13-2022 Table 19.2.3.1.2 - hose allowance and duration; NFPA 22 - water "
               "tanks: a tank is sized to supply the equipment it serves, refilled within 8 h")


# --- the hazard classes --------------------------------------------------------------

_ref("hazard_definitions", "The occupancy hazard classes",
     "NFPA 13 occupancy classifications (2016: 5.2 to 5.4.2; 2019: 4.3.2 to 4.3.6), in "
     "Heron's words after them",
     ("class", "what puts a space in it", "checked"),
     [("light hazard", "little that burns, and what there is burns with a relatively low heat "
       "release", "the meaning in two search extracts"),
      ("ordinary hazard group 1", "contents of low combustibility in moderate quantity, "
       "stockpiles no higher than 2.4 m (8 ft), moderate heat release", "two search extracts"),
      ("ordinary hazard group 2", "moderate to high quantity and combustibility, stockpiles no "
       "higher than 3.7 m (12 ft), moderate to high heat release", "two search extracts"),
      ("extra hazard group 1", "very high quantity and combustibility with dust, lint or the "
       "like - fast fires of high heat release, but little or no flammable or combustible "
       "liquid", "two search extracts"),
      ("extra hazard group 2", "moderate to substantial flammable or combustible liquids, or "
       "combustibles extensively shielded from the spray", "one search extract")])

_ANNEX = "NFPA 13 annex"
_OWNER = "the owner's own file"

_ref("hazard_examples", "Occupancies listed beside each hazard class",
     "NFPA 13's annex to the occupancy classes (2016 A.5.2 to A.5.4; 2019 A.4.3), as search "
     "extracts reproduce it; and the owner's own firefighting file",
     ("class", "example", "listed by"),
     [("light hazard", e, _ANNEX) for e in
      ("churches", "educational", "offices", "theaters", "museums", "hospitals")]
     + [("light hazard", e, _OWNER) for e in
        ("offices", "hotels", "hospital wards", "mosques")]
     + [("ordinary hazard group 1", e, _ANNEX) for e in
        ("automobile parking and showrooms", "bakeries", "electronic plants", "laundries",
         "restaurant service areas")]
     + [("ordinary hazard group 1", e, _OWNER) for e in
        ("parking", "laundries", "restaurant service areas")]
     + [("ordinary hazard group 2", e, _ANNEX) for e in
        ("agricultural facilities", "barns and stables", "cereal mills",
         "confectionery products", "distilleries", "dry cleaners",
         "exterior loading docks (ordinary combustibles only)", "feed mills",
         "horse stables", "libraries, large stack rooms", "machine shops", "metal working",
         "mercantile", "paper and pulp mills", "paper process plants", "piers and wharves",
         "post offices", "printing and publishing", "repair garages", "stages",
         "textile manufacturing", "tire manufacturing", "wood machining")]
     + [("ordinary hazard group 2", e, _OWNER) for e in
        ("workshops", "stores", "kitchens", "retail")]
     + [("extra hazard group 1", e, _ANNEX) for e in
        ("aircraft hangars", "combustible hydraulic fluid use areas", "die casting",
         "plywood and particleboard manufacturing", "saw mills",
         "textile picking and opening", "upholstering with plastic foams")],
     note="light hazard and ordinary hazard group 1 examples were confirmed by one search, "
          "group 2 by two, extra hazard group 1 by one; NO extra hazard group 2 example could "
          "be read, and the annex's longer lists are not reproduced. The owner's file puts "
          "kitchens in group 2 where the annex lists restaurant service areas in group 1 - "
          "docs/42 s12.")


# --- design criteria -----------------------------------------------------------------

_ref("design_criteria", "Density/area design points, hose allowance and duration",
     "NFPA 13-2022 Table 19.2.3.1.1 (density/area, as corrected by errata 13-22-1) and "
     "Table 19.2.3.1.2 (hose allowance and duration)",
     ("hazard", "density mm/min", "design area m2", "density gpm/ft2", "design area ft2",
      "second density mm/min", "second area m2", "second density gpm/ft2", "second area ft2",
      "hose allowance L/min", "hose allowance gpm", "duration min"),
     [("light hazard", 4.1, 140, 0.10, 1500, 2.9, 280, 0.07, 3000, 378.5, 100, "30"),
      ("ordinary hazard group 1", 6.1, 140, 0.15, 1500, 4.9, 280, 0.12, 3000, 946.4, 250,
       "60 to 90"),
      ("ordinary hazard group 2", 8.1, 140, 0.20, 1500, 6.9, 280, 0.17, 3000, 946.4, 250,
       "60 to 90"),
      ("extra hazard group 1", 12.2, 230, 0.30, 2500, 11.4, 280, 0.28, 3000, 1892.7, 500,
       "90 to 120"),
      ("extra hazard group 2", 16.3, 230, 0.40, 2500, 15.5, 280, 0.38, 3000, 1892.7, 500,
       "90 to 120")],
     note="the 2022 edition's single points for a new system; the SECOND point only where "
          "19.2.3.1.5 applies - combustible concealed spaces that are not sprinklered. 2016 "
          "and 2019 printed density/area curves with the first point as their smallest-area "
          "end, and 2025 removes the curves. The first points are confirmed by four sources, "
          "the second by NFPA's own errata. SI as the 2022 edition prints it - 1500 ft2 is "
          "140 m2 there and 139 m2 in the 2019 text and the owner's file. Hose allowance is "
          "inside and outside hose combined (inside alone 0, 50 or 100 gpm), converted "
          "exactly; the edition's SI rounds it. The shorter duration applies where waterflow "
          "and valves are supervised to a constantly attended place.")

_ref("design_area_rules", "Rules that change a design area",
     "NFPA 13 (2016 11.2.3.2 to 11.2.3.3; 2022 19.2.3), as search extracts reproduce it",
     ("rule", "what NFPA 13 says", "least m2", "checked"),
     [("unsprinklered combustible concealed spaces", "a design area of at least 3000 ft2 "
       "(280 m2), at the second density point - 2022 19.2.3.1.5 - applied after every other "
       "adjustment", 3000 * M2_PER_FT2, "two search extracts and NFPA's errata"),
      ("an area below 1500 ft2", "a design area under 1500 ft2 (139 m2) uses the 1500 ft2 "
       "density - the 2019 text", None, "one search extract"),
      ("the rectangle", "the side parallel to the branch lines at least 1.2 sqrt(A); a "
       "fraction of a sprinkler is carried to the next whole one", None,
       "two result sets for 1.2 sqrt A, one for the rounding"),
      ("dry pipe", "the design area increased 30 %", None, "two result sets"),
      ("double-interlock preaction", "the design area increased 30 %", None,
       "one search extract"),
      ("sloped ceiling", "steeper than 2 in 12 (16.7 %, 9.5 degrees): the design area "
       "increased 30 %", None, "one search extract"),
      ("quick-response", "a wet system, light or ordinary hazard, a ceiling no higher than "
       "20 ft (6.1 m), no unprotected ceiling pocket over 32 ft2 (3.0 m2) and no unprotected "
       "area above a cloud ceiling: the area reduced by 40 % at a ceiling of 10 ft (3.05 m) "
       "or less, falling to 25 % at 20 ft (6.1 m), and never to fewer than five sprinklers",
       None, "one search extract; the curve between its two ends not read"),
      ("high-temperature sprinklers", "extra hazard: the design area reduced 25 % without "
       "changing the density, never below 2000 ft2 (186 m2) after every adjustment",
       2000 * M2_PER_FT2, "two result sets for 25 %, one for 2000 ft2"),
      ("more than one adjustment", "each is taken on the area first selected, and they are "
       "added - 2500 + 750 - 625 = 2625 ft2 for a dry system with high-temperature "
       "sprinklers", None, "one site, two of its pages")],
     note="every percentage here is OFFERED beside its question and applied only as the "
          "modeller gives it: an increase is asked, a reduction never applied unless given.")


# --- spacing ---------------------------------------------------------------------------

_ref("spacing_standard_spray", "Protection area and spacing - standard pendent and upright "
     "spray sprinklers",
     "NFPA 13-2022 Table 10.2.4.2.1(a) to (c) (2016: Table 8.6.2.2.1(a) to (c))",
     ("family", "construction", "system", "max area m2", "max area ft2", "max spacing m",
      "max spacing ft", "checked"),
     [("light hazard", "noncombustible, or combustible unobstructed without exposed members",
       "hydraulically calculated", 20.9, 225, 4.6, 15,
       "a search extract and the owner's file agree"),
      ("light hazard", "noncombustible, or combustible unobstructed without exposed members",
       "pipe schedule", 18.6, 200, 4.6, 15, "one search extract"),
      ("light hazard", "combustible, with exposed members 3 ft (0.91 m) or more on centre",
       "either", round(168 * M2_PER_FT2, 1), 168, 4.6, 15, "one search extract"),
      ("light hazard", "combustible, with exposed members less than 3 ft (0.91 m) on centre",
       "either", round(130 * M2_PER_FT2, 1), 130, 4.6, 15, "one search extract"),
      ("light hazard", "a combustible concealed space - wood joists or trusses, a pitch of "
       "4 in 12 or more; 15 ft along the slope, 10 ft (3.05 m) across it",
       "either", round(120 * M2_PER_FT2, 1), 120, 4.6, 15, "one search extract"),
      ("ordinary hazard", "all", "either", 12.1, 130, 4.6, 15,
       "a search extract and the owner's file agree"),
      ("extra hazard", "all", "pipe schedule", 8.4, 90, NOT_HELD, NOT_HELD,
       "one search extract; its spacing not read"),
      ("extra hazard", "all", "hydraulically calculated", 9.3, 100, NOT_HELD, NOT_HELD,
       "the owner's file only; its density condition and spacing not read")],
     note="the light hazard rows under combustible construction came from one search "
          "extract that did not say 'obstructed' - read them in your copy. Sidewall and "
          "extended coverage sprinklers: `reference spacing_sidewall` and `reference "
          "spacing_extended_coverage`; a sprinkler's listing sets its own.")

_ref("sprinkler_rules", "Distances for standard spray sprinklers",
     "NFPA 13-2022 10.2.5 and 10.2.6 (2016: 8.6.3 and 8.6.4), as search extracts reproduce "
     "them, and the owner's own file",
     ("rule", "value", "checked"),
     [("maximum distance to a wall", "one-half of the allowable distance between sprinklers "
       "(2022 10.2.5.2.1) - 2.3 m where 4.6 m is allowed",
       "search extracts and the owner's file agree"),
      ("minimum distance to a wall", "4 in - 100 mm as the 2022 SI prints it, 102 mm exactly",
       "a search extract and the owner's file agree"),
      ("minimum distance between sprinklers", "6 ft (1.8 m), unless a baffle separates them",
       "two search extracts and the owner's file agree"),
      ("baffles", "midway between the sprinklers, noncombustible or limited-combustible, at "
       "least 8 in (200 mm) long and 6 in (150 mm) high, their tops 2 to 3 in (50 to 75 mm) "
       "above upright deflectors", "one search extract"),
      ("small rooms", "a light hazard room of up to 800 ft2 (74 m2) may have a sprinkler up "
       "to 9 ft (2.7 m) from a wall", "one search extract"),
      ("deflector below the ceiling, unobstructed construction", "1 to 12 in - 25 to 300 mm "
       "in the owner's file, 305 mm exactly", "a search extract and the owner's file"),
      ("deflector, obstructed construction", "1 to 6 in below the structural members, and no "
       "more than 22 in below the deck", "one search extract"),
      ("clearance below a deflector to storage", "18 in (457 mm) for standard spray "
       "sprinklers; 36 in (914 mm) for special sprinklers - the owner's file gives 450 mm",
       "one search extract; the owner's file for 450 mm"),
      ("minimum operating pressure", "7 psi (0.5 bar) at any sprinkler, or the listing's "
       "minimum where that is more", "one search extract"),
      ("rated working pressure", "system components rated for at least 175 psi (12.1 bar), "
       "or the most the system works at where that is more", "one search extract, whose SI "
       "read 12 bar")],
     note="NFPA 13's temperature ratings: `reference temperature_ratings`; its K-factors: "
          "`reference k_factors`.")


# --- obstructions -----------------------------------------------------------------------

_ref("beam_rule_standard", "The beam rule - how high a deflector may sit above the bottom of "
     "an obstruction, by distance",
     "NFPA 13 beam rule for standard upright and pendent spray sprinklers (2016 Table "
     "8.6.5.1.2; 2019 Table 10.2.7.1.2)",
     ("A, sprinkler to the side of the obstruction", "from mm", "to mm (less than)",
      "most B mm", "most B in", "checked"),
     [("less than 1 ft", 0.0, 304.8, 0.0, 0, "two search extracts"),
      ("1 ft to less than 1 ft 6 in", 304.8, 457.2, 63.5, 2.5, "two search extracts"),
      ("1 ft 6 in to less than 2 ft", 457.2, 609.6, 88.9, 3.5, "two search extracts"),
      ("2 ft to less than 2 ft 6 in", 609.6, 762.0, 139.7, 5.5, "one search extract"),
      ("2 ft 6 in to less than 3 ft", 762.0, 914.4, 190.5, 7.5, "one search extract"),
      ("3 ft to less than 3 ft 6 in", 914.4, 1066.8, 241.3, 9.5, "one search extract"),
      ("3 ft 6 in to less than 4 ft", 1066.8, 1219.2, 304.8, 12, "one search extract"),
      ("4 ft to less than 4 ft 6 in", 1219.2, 1371.6, 355.6, 14, "two search extracts"),
      ("4 ft 6 in to less than 5 ft", 1371.6, 1524.0, 419.1, 16.5, "two search extracts"),
      ("5 ft to less than 5 ft 6 in", 1524.0, 1676.4, 457.2, 18, "two search extracts")],
     note="millimetres converted exactly from the inches NFPA prints - the 2022 SI prints "
          "them rounded, 450 mm for 18 in; from 5 ft 6 in (1676 mm) out the sources disagree "
          "- one table goes on rising, another stops at 18 in - so those rows are NOT held. "
          "An obstruction against a wall no more than 30 in (762 mm) wide may also use this "
          "table (one source).")

_ref("obstruction_rules", "Obstructions to sprinkler discharge",
     "NFPA 13 (2019/2022 10.2.7.2.1.3 and 9.5.5.3; 2016 8.6.5.2.1.3, 8.6.5.2.1.4 and 8.5.5.3), "
     "as search extracts reproduce it",
     ("rule", "what NFPA 13 says", "value mm", "checked"),
     [("three-times rule", "a sprinkler stands at least three times an obstruction's largest "
       "dimension away from it - a column, a pipe, a light - and never needs more than 24 in "
       "(610 mm); one source says the 24 in cap does not apply to a vertical obstruction "
       "such as a column", 609.6, "two search extracts"),
      ("light and ordinary hazard", "only structural members need be considered under the "
       "three-times rule - non-structural elements are exempt (2016 8.6.5.2.1.4 and its "
       "annex)", None, "two search extracts"),
      ("wide obstructions", "sprinklers are needed below a fixed obstruction wider than 4 ft "
       "(1.2 m) - a duct, a deck, open grating, a cutting table, an overhead door - but not "
       "where a noncombustible obstruction's bottom is 24 in (600 mm) or less above the "
       "floor; the width is the lesser horizontal side", 1219.2,
       "two search extracts; the width's meaning one"),
      ("the 18 in boundary", "an obstruction within 18 in (457 mm) below the deflector falls "
       "under the spray-pattern rules, one further below under the rules for discharge "
       "reaching the hazard", 457.2, "one search extract")])


# --- pipe schedules -------------------------------------------------------------------

_PS_SOURCE = ("NFPA 13-2022 28.5 pipe schedules (2016: 23.5), as search extracts reproduce "
              "them")
_PS_COLUMNS = ("nominal", "steel sprinklers", "copper sprinklers")

_ref("pipe_schedule_light", "Light hazard pipe schedule - the most sprinklers a pipe may feed",
     _PS_SOURCE + "; 1 to 2 1/2 in steel also the owner's file", _PS_COLUMNS,
     [("1", 2, 2), ("1 1/4", 3, 3), ("1 1/2", 5, 5), ("2", 10, 12), ("2 1/2", 30, 40),
      ("3", 60, 65), ("3 1/2", NOT_HELD, 115), ("4", None, None)],
     note="one search extract each; the 3 1/2 in steel figure could not be confirmed and is "
          "not held; at 4 in the table hands over to the system's area limits.")

_ref("pipe_schedule_light_above_below", "Light hazard - sprinklers above AND below a ceiling "
     "on common branch lines", _PS_SOURCE, _PS_COLUMNS,
     [("1", 2, 2), ("1 1/4", 4, 4), ("1 1/2", 7, 7), ("2", 15, 18), ("2 1/2", 50, 65)],
     note="counted as the greatest number on any two adjacent levels; past the 2 1/2 in "
          "figure the pipe is 3 in, then sized from the light hazard schedule by the larger "
          "of the two levels - one search extract, in an older edition's wording.")

_ref("pipe_schedule_ordinary", "Ordinary hazard pipe schedule - the most sprinklers a pipe "
     "may feed", _PS_SOURCE, _PS_COLUMNS,
     [("1", 2, 2), ("1 1/4", 3, 3), ("1 1/2", 5, 5), ("2", 10, 12), ("2 1/2", 20, 25),
      ("3", 40, NOT_HELD), ("3 1/2", 65, NOT_HELD), ("4", 100, 115), ("5", 160, 180),
      ("6", 275, 300)],
     note="one search extract (3 in steel confirmed by a second); the 3 and 3 1/2 in copper "
          "figures came back different from two searches and are not held; the 8 in row "
          "was not found.")

_ref("pipe_schedule_ordinary_wide", "Ordinary hazard - sprinklers or branch lines more than "
     "3.7 m (12 ft) apart", _PS_SOURCE, _PS_COLUMNS,
     [("1", 2, 2), ("1 1/4", 3, 3), ("1 1/2", 5, 5), ("2", 10, 12), ("2 1/2", 15, 20),
      ("3", 30, 35), ("3 1/2", 60, 65), ("4", 100, 115), ("5", 160, 180), ("6", 275, 300)],
     note="the 2 1/2 to 3 1/2 in rows are this table's own (one search extract); the others "
          "are the ordinary hazard schedule's.")

_ref("pipe_schedule_ordinary_above_below", "Ordinary hazard - sprinklers above AND below a "
     "ceiling on common branch lines", _PS_SOURCE, _PS_COLUMNS,
     [("1", 2, 2), ("1 1/4", 4, 4), ("1 1/2", 7, 7), ("2", 15, 18), ("2 1/2", 30, 40),
      ("3", NOT_HELD, NOT_HELD)],
     note="one search extract; the 3 in row - and with it the step to 3 1/2 in past it - "
          "could not be confirmed and is not held.")

_ref("pipe_schedule_rules", "When and how the pipe schedule may be used",
     _PS_SOURCE, ("rule", "what NFPA 13 says", "checked"),
     [("where it may be used", "a new system of 5000 ft2 (465 m2) or less; a larger new "
       "system only where the schedule's water supply is there at 50 psi (3.4 bar) or more "
       "residual at the highest sprinkler; and additions to or changes in an existing pipe "
       "schedule system", "two search extracts"),
      ("branch lines", "no more than 8 sprinklers on a branch line either side of a cross "
       "main - 9 where the two end lengths are 1 in and 1 1/4 in and standard sizes follow",
       "one search extract"),
      ("above and below a ceiling", "where sprinklers above and below a ceiling share branch "
       "lines or a cross main, no more than 8 above and 8 below on either side of the cross "
       "main; pipe sized from the above-and-below table by the greatest number on any two "
       "adjacent levels; past the table's last figure the next size up, then sized from the "
       "regular schedule by the larger level", "one search extract, an older edition's "
       "wording"),
      ("extra hazard", "hydraulically calculated; the schedule serves only additions to an "
       "existing extra hazard pipe schedule system", "one search extract"),
      ("water supply, light hazard", "15 psi (1.0 bar) residual at the highest sprinkler's "
       "elevation; 500 to 750 gpm (1893 to 2839 L/min) at the base of the riser with hose "
       "streams; 30 to 60 min", "two search extracts, SI converted here"),
      ("water supply, ordinary hazard", "20 psi (1.4 bar) residual; 850 to 1500 gpm (3218 to "
       "5678 L/min); 60 to 90 min", "two search extracts, SI converted here"),
      ("area one system may protect", "on any one floor, per riser: 52,000 ft2 (4,831 m2) at "
       "light and at ordinary hazard; 25,000 ft2 (2,323 m2) at extra hazard on a pipe "
       "schedule; 40,000 ft2 (3,716 m2) for high-piled storage", "one search extract, a "
       "2022 plan-check sheet")])


# --- pipe ---------------------------------------------------------------------------------

_ref("steel_pipe_bores", "Steel pipe inside diameters, schedules 10 and 40",
     "ASME B36.10M, as the fluids package's piping tables reproduce it",
     ("schedule", "nominal", "DN", "outside mm", "wall mm", "bore mm"),
     [("10", "3/4", 20, 26.7, 2.11, 22.48), ("10", "1", 25, 33.4, 2.77, 27.86),
      ("10", "1 1/4", 32, 42.2, 2.77, 36.66), ("10", "1 1/2", 40, 48.3, 2.77, 42.76),
      ("10", "2", 50, 60.3, 2.77, 54.76), ("10", "2 1/2", 65, 73.0, 3.05, 66.90),
      ("10", "3", 80, 88.9, 3.05, 82.80), ("10", "3 1/2", 90, 101.6, 3.05, 95.50),
      ("10", "4", 100, 114.3, 3.05, 108.20), ("10", "5", 125, 141.3, 3.40, 134.50),
      ("10", "6", 150, 168.3, 3.40, 161.50), ("10", "8", 200, 219.1, 3.76, 211.58),
      ("10", "10", 250, 273.0, 4.19, 264.62), ("10", "12", 300, 323.8, 4.57, 314.66),
      ("40", "3/4", 20, 26.7, 2.87, 20.96), ("40", "1", 25, 33.4, 3.38, 26.64),
      ("40", "1 1/4", 32, 42.2, 3.56, 35.08), ("40", "1 1/2", 40, 48.3, 3.68, 40.94),
      ("40", "2", 50, 60.3, 3.91, 52.48), ("40", "2 1/2", 65, 73.0, 5.16, 62.68),
      ("40", "3", 80, 88.9, 5.49, 77.92), ("40", "3 1/2", 90, 101.6, 5.74, 90.12),
      ("40", "4", 100, 114.3, 6.02, 102.26), ("40", "5", 125, 141.3, 6.55, 128.20),
      ("40", "6", 150, 168.3, 7.11, 154.08), ("40", "8", 200, 219.1, 8.18, 202.74),
      ("40", "10", 250, 273.0, 9.27, 254.46), ("40", "12", 300, 323.8, 10.31, 303.18)],
     note="bore = outside diameter - 2 x wall, every row; schedule 40 agrees with the HVAC "
          "engine's own table. Copper tube, CPVC and anything else: give bore_mm.")


# --- supply ---------------------------------------------------------------------------------

_ref("standpipe_rules", "Standpipe flows and pressures",
     "NFPA 14 (2016 and 2019 editions: 7.10 flow, 7.8.1 pressure, 7.2.3 regulating devices, "
     "7.3 locations), as search extracts reproduce it",
     ("item", "value", "checked"),
     [("Class I and III, first standpipe", "500 gpm (1893 L/min) for the most remote "
       "standpipe - 250 gpm at each of its two most remote outlets", "two search extracts"),
      ("each additional standpipe", "250 gpm (946 L/min), at its topmost outlet",
       "two search extracts"),
      ("most for the building", "1000 gpm (3785 L/min) where it is sprinklered throughout to "
       "NFPA 13; 1250 gpm (4731 L/min) otherwise", "two search extracts"),
      ("Class II", "100 gpm (379 L/min), no more for more standpipes", "two search extracts"),
      ("residual pressure", "100 psi (6.9 bar) at the most remote 2 1/2 in outlet; 65 psi "
       "(4.5 bar) at the most remote 1 1/2 in outlet", "two search extracts and the owner's "
       "file"),
      ("pressure-regulating devices", "2013 and 2016: where static exceeds 175 psi (12.1 bar) "
       "at a connection, it is limited - to 100 psi (6.9 bar) at a 1 1/2 in outlet for "
       "trained personnel, 175 psi elsewhere; 2019 and later changed this and the sources "
       "disagree how", "two for 2016; the later text unsettled"),
      ("duration", "at least 30 min at the system demand", "two search extracts"),
      ("Class I hose connections", "2016 on: no point more than 130 ft (39.7 m) from one where "
       "unsprinklered, 200 ft (61 m) where sprinklered, along the path of travel",
       "two search extracts"),
      ("standpipe size", "4 in (100 mm) for Class I and III; 6 in (150 mm) in a combined "
       "system", "two weak search extracts")],
     note="NFPA 14-2026 reportedly separates hose systems - check the numbering in a newer "
          "edition. The owner's file gives the building's most as 1250 gpm, which is NFPA "
          "14's figure for a building that is NOT sprinklered throughout - docs/42 s12.")

_ref("fire_pump_rules", "Fire pump rules",
     "NFPA 20 (2016 to 2022: 6.2 the curve, 4.14 suction, Annex A.4.26 and A.14.2.6 the "
     "pressure maintenance pump), as search extracts reproduce it",
     ("rule", "what NFPA 20 says", "checked"),
     [("the curve", "at least 150 % of rated flow at no less than 65 % of rated pressure; "
       "shutoff (churn) no more than 140 % of rated pressure, for any pump type",
       "three search extracts and the owner's file"),
      ("rated net pressure", "at least 40 psi (2.7 bar)", "one search extract"),
      ("suction", "at 150 % of rated flow the suction gauge reads at least 0 psi - down to "
       "-3 psi (-0.2 bar) from a suction tank whose base is at or above the pump; no more "
       "than 15 ft/s (4.57 m/s) within 10 pipe diameters upstream", "two search extracts"),
      ("jockey pump flow", "the annex: make up the allowable leakage in 10 minutes, or 1 gpm "
       "(3.8 L/min), whichever is more, for underground mains; above ground only, less than "
       "one sprinkler's flow", "two search extracts"),
      ("pressure settings", "the annex, to 2022: the jockey stops at churn plus the least "
       "static suction and starts at least 10 psi (0.68 bar) lower; the fire pump starts 5 "
       "psi (0.34 bar) below the jockey's start, each further pump 10 psi lower - 2025 "
       "reportedly moved the fire pump to 10 psi below", "two search extracts to 2022"),
      ("pump room", "high-rise: 2-hour construction or 50 ft (15.2 m) separation; a fully "
       "sprinklered building that is not high-rise may use 1-hour", "two search extracts "
       "for the 2-hour and 50 ft")])

_ref("fire_pump_ratings", "Rated fire pump capacities",
     "NFPA 20 rated capacities (2016: 4.8; 2019: 4.10), as search extracts reproduce them - "
     "L/min converted exactly",
     ("rated gpm", "rated L/min"),
     [(g, round(g * LPM_PER_GPM, 1)) for g in
      (25, 50, 100, 150, 200, 250, 300, 400, 450, 500, 750, 1000, 1250, 1500, 2000, 2500,
       3000, 3500, 4000, 4500, 5000)])


# --- devices -----------------------------------------------------------------------------

_ref("extinguisher_rules", "Class A portable extinguishers",
     "NFPA 10 (2018, 2022) Table 6.2.1.1, as search extracts reproduce it - SI converted "
     "exactly",
     ("hazard", "least rating", "area per unit of A m2", "area per unit of A ft2",
      "most per extinguisher m2", "most per extinguisher ft2", "most travel m",
      "most travel ft"),
     [("light (low)", "2-A", 278.7, 3000, 1045.2, 11250, 22.9, 75),
      ("ordinary (moderate)", "2-A", 139.4, 1500, 1045.2, 11250, 22.9, 75),
      ("extra (high)", "4-A", 92.9, 1000, 1045.2, 11250, 22.9, 75)],
     note="Class B (liquid up to 1/4 in deep), Table 6.3.1.1: light 5-B within 30 ft (9.1 m) "
          "or 10-B within 50 ft (15.2 m); ordinary 10-B or 20-B; extra 40-B or 80-B. Class K: "
          "within 30 ft (9.1 m) of the hazard (6.6.2). The owner's file gives 23 m of travel "
          "where 75 ft is 22.9 m.")

_ref("detector_rules", "Spot detector spacing and manual fire alarm boxes",
     "NFPA 72 (2013 to 2019: 17.7.3.2 smoke, 17.6.3 heat, 17.14.8 manual boxes, 18.4.3 "
     "audibility; later editions renumber), as search extracts reproduce it",
     ("rule", "value", "checked"),
     [("smoke detectors, smooth ceiling", "nominal spacing S = 30 ft (9.1 m); no more than "
       "S/2 from walls or partitions reaching the top 15 % of the ceiling height - or, the "
       "other way, every point of the ceiling within 0.7 S (21 ft, 6.4 m)",
       "two search extracts; the owner's file gives 9.1 m"),
      ("heat detectors", "their LISTED spacing, reduced by the ceiling height factor - "
       "`reference heat_detector_height`; the S/2 and 0.7 S wording for heat detectors was "
       "not read in this work", "-"),
      ("heat detectors, joists and beams", "solid joists: no more than 50 % of the "
       "smooth-ceiling spacing across them; beams deeper than 12 in (305 mm) or more than "
       "8 ft (2.4 m) apart: no more than 2/3 across them; beams deeper than 18 in (457 mm) "
       "and more than 8 ft apart: each bay its own area", "two search extracts"),
      ("smoke detectors, level beam ceilings", "beams shallower than 0.1 H (H the ceiling "
       "height): smooth-ceiling spacing; 0.1 H or deeper and 0.4 H or more apart: a detector "
       "in each pocket; 0.1 H or deeper and less than 0.4 H apart: smooth spacing along the "
       "beams, half across them; solid joists count as beams", "two search extracts"),
      ("manual fire alarm boxes", "within 5 ft (1.5 m) of each exit doorway on each floor; "
       "no more than 200 ft (61 m) of travel to one on the same floor; the operable part 42 "
       "to 48 in (1.07 to 1.22 m) above the floor - whether they are needed at all is the "
       "building code's or NFPA 101's", "two search extracts for the figures"),
      ("audible signals, public mode", "at least 15 dB above the average ambient, or 5 dB "
       "above the maximum lasting 60 s, whichever is greater, 5 ft (1.5 m) above the floor",
       "two search extracts")])

_ref("heat_detector_height", "Heat detector spacing by ceiling height",
     "NFPA 72 Table 17.6.3.5.1, as search extracts and two manufacturers' instructions "
     "reproduce it - metres converted exactly",
     ("ceiling above m", "up to m", "multiply the listed spacing by", "checked"),
     [(0.0, 3.05, 1.00, "two"), (3.05, 3.66, 0.91, "two"), (3.66, 4.27, 0.84, "one"),
      (4.27, 4.88, 0.77, "one"), (4.88, 5.49, 0.71, "one"), (5.49, 6.10, 0.64, "two"),
      (6.10, 6.71, 0.58, "one"), (6.71, 7.32, 0.52, "one"), (7.32, 7.92, 0.46, "one"),
      (7.92, 8.53, 0.40, "one"), (8.53, 9.14, 0.34, "two")],
     note="10 ft to 30 ft in 2 ft steps; the factor applies before any beam, joist or slope "
          "reduction.")


# --- BS EN 12845 ----------------------------------------------------------------------

_EN_SOURCE = ("BS EN 12845:2015+A1:2019, as search extracts and copies of the standard "
              "reproduce it (A2:2026 not read)")
_EN_NOT_DRY = "not allowed - designed as %s"

_ref("en12845_criteria", "BS EN 12845 design figures by hazard class",
     _EN_SOURCE,
     ("class", "density mm/min", "area wet m2", "area dry m2", "duration min",
      "most area per sprinkler m2", "most spacing m", "least pressure bar", "checked"),
     [("LH", 2.25, 84, _EN_NOT_DRY % "OH1", 30, 21.0, 4.6, 0.70, "two result sets"),
      ("OH1", 5.0, 72, 90, 60, 12.0, 4.0, 0.35, "two result sets"),
      ("OH2", 5.0, 144, 180, 60, 12.0, 4.0, 0.35, "two result sets"),
      ("OH3", 5.0, 216, 270, 60, 12.0, 4.0, 0.35, "two result sets, and a project "
       "specification for OH3"),
      ("OH4", 5.0, 360, _EN_NOT_DRY % "HHP1", 60, 12.0, 4.0, 0.35, "two result sets"),
      ("HHP1", 7.5, 260, 325, 90, 9.0, 3.7, 0.50, "two result sets"),
      ("HHP2", 10.0, 260, 325, 90, 9.0, 3.7, 0.50, "two result sets"),
      ("HHP3", 12.5, 260, 325, 90, 9.0, 3.7, 0.50, "two result sets"),
      ("HHP4", NOT_HELD, NOT_HELD, NOT_HELD, 90, 9.0, 3.7, 0.50, "one search extract: a "
       "deluge system, its design not read"),
      ("HHS1", NOT_HELD, NOT_HELD, NOT_HELD, 90, 9.0, 3.7, 0.50, "storage designed from "
       "tables by category and height, not read"),
      ("HHS2", NOT_HELD, NOT_HELD, NOT_HELD, 90, 9.0, 3.7, 0.50, "the same"),
      ("HHS3", NOT_HELD, NOT_HELD, NOT_HELD, 90, 9.0, 3.7, 0.50, "the same"),
      ("HHS4", NOT_HELD, NOT_HELD, NOT_HELD, 90, 9.0, 3.7, 0.50, "the same")],
     note="copies of the 2004, 2004+A2:2009 and 2015+A1:2019 texts were read through search "
          "extracts, which seldom said which; A2:2026 became the current British Standard on "
          "9 April 2026 and moved ESFR and CMSA sprinklers to EN 12845-2:2024, and no figure "
          "here was checked against it. Table numbers came back only beside a query's own "
          "words, so none is quoted. The dry or alternate area is the wet one plus 25 % in "
          "every row that has one - read from the table, never applied as a rule. "
          "Durations: LH 30 min and high "
          "hazard 90 min by two result sets, OH 60 min by a project specification (one "
          "result gave 60 to 90 min from the Indian IS 15105, not adopted). The least "
          "pressure is at the hydraulically least favourable sprinkler with every one in the "
          "area flowing, and governs where the density asks less; in-rack sprinklers ask "
          "1.0 bar (K115) and 2.0 bar (K80). Nothing found shows QCDD accepting EN 12845 in "
          "place of NFPA 13.")

_ref("en12845_hazards", "BS EN 12845's hazard classes",
     _EN_SOURCE, ("class", "what puts a space in it", "checked"),
     [("LH", "non-industrial occupancies of low fire load and low combustibility - reported "
       "too as no compartment over 126 m2, with walls of 30 min fire resistance",
       "two result sets; the compartment limit one"),
      ("OH", "trading and industrial occupancies of medium fire load, in four groups, OH1 "
       "to OH4", "two result sets"),
      ("HHP", "processes whose materials have a high fire load and high combustibility and "
       "can spread a fire quickly or burn intensely, in four groups, HHP1 to HHP4",
       "two result sets for the meaning, one for the four groups"),
      ("HHS", "storage stacked higher than the ordinary hazard limits; HHS1 to HHS4 follow "
       "categories I to IV of the goods and their packaging, and how they are stored",
       "one search extract")],
     note="EN 12845's Annex A, which lists occupancies by class, was NOT read - the one list "
          "that came back put hotels and hospitals in LH, against what is remembered of it, "
          "and is not used.")

_ref("en12845_rules", "BS EN 12845 distances and other rules",
     _EN_SOURCE, ("rule", "value", "checked"),
     [("most distance to a wall", "2.0 m in a standard layout, 2.3 m staggered, 1.5 m under "
       "open joists or exposed rafters", "one search extract"),
      ("least distance between sprinklers", "2 m, unless a baffle about 200 x 150 mm, or the "
       "building itself, stops one wetting the next; intermediate in-rack sprinklers, and "
       "escalators and stairwells, are excepted", "one search extract"),
      ("staggered spacing", "LH 4.6 m; OH 4.6 m along the range and 4.0 m between ranges; "
       "HHP and HHS 3.7 m", "two result sets"),
      ("deflector below the ceiling", "0.075 to 0.15 m (not for ceiling, flush or recessed "
       "types); never more than 0.3 m below a combustible ceiling or 0.45 m below a "
       "Euroclass A1 or A2 one; a row at the ridge where the slope is over 30 degrees",
       "one search extract"),
      ("concealed spaces", "deeper than 0.8 m: sprinklered; 0.8 m or less: only where they "
       "hold, or are built of, combustible material - to LH where the main class is LH, "
       "otherwise to OH1", "one search extract"),
      ("sidewall sprinklers", "LH 17.0 m2, 4.6 m along the wall, 2.3 m to the end wall; OH "
       "9.0 m2, 3.4 m along the wall, 1.8 m to the end wall; the deflector 0.10 to 0.15 m "
       "below the ceiling and 0.05 to 0.15 m out from the wall; more rows where the room is "
       "wider than 7.4 m", "one search extract"),
      ("K-factor", "LH K57; OH K80 (K115 reported permitted); HHP and HHS K80 or K115",
       "two result sets for OH K80, one for the rest"),
      ("velocity", "at most 6 m/s through valves and flow switches, 10 m/s anywhere else",
       "two result sets"),
      ("Hazen-Williams", "p = 6.05 x 10^5 L Q^1.85 / (C^1.85 d^4.87) - bar over the "
       "equivalent length L in m, Q in L/min, d the bore in mm: NFPA 13's SI form",
       "one search extract"),
      ("pre-calculated systems", "LH and OH only - a high hazard system is calculated in "
       "full", "one search extract"),
      ("temperature rating", "close to, and not less than, 30 C above the highest ambient "
       "temperature expected", "one search extract")])

_ref("en12845_precalculated", "BS EN 12845 pre-calculated systems - the design point at the "
     "control valve",
     _EN_SOURCE + "; its Table 6, the title confirmed in three languages",
     ("class", "system", "design L/min", "design bar + ps", "most demand L/min",
      "most demand bar + ps", "checked"),
     [("LH", "wet or pre-action", 225, 2.2, None, None, "two result sets"),
      ("OH1", "wet or pre-action", 375, 1.0, 540, 0.7, "two result sets"),
      ("OH1", "dry or alternate", 725, 1.4, 1000, 1.0, "two result sets"),
      ("OH2", "dry or alternate", 1100, 1.7, 1350, 1.4, "two result sets"),
      ("OH3", "dry or alternate", 1800, 2.0, 2100, 1.5, "two result sets"),
      ("OH4", "dry or alternate", "not allowed", None, None, None, "one search extract"),
      ("OH2", "wet or pre-action", NOT_HELD, NOT_HELD, NOT_HELD, NOT_HELD,
       "two searches gave different figures - not held"),
      ("OH3", "wet or pre-action", NOT_HELD, NOT_HELD, NOT_HELD, NOT_HELD, "the same"),
      ("OH4", "wet or pre-action", NOT_HELD, NOT_HELD, NOT_HELD, NOT_HELD, "the same")],
     note="ps is the static pressure of the height between the highest sprinkler and the "
          "control valve set's C gauge. The two result sets agreeing were probably the same "
          "copies. The pipe size tables a pre-calculated system is built from were NOT "
          "read, so Heron cannot size one.")


# --- FM Global ---------------------------------------------------------------------------

_FM_SOURCE = ("FM Global Property Loss Prevention Data Sheet 3-26, non-storage occupancies "
              "(April 2019 and October 2020 copies), its Table 2, as search extracts "
              "reproduce it - converted exactly from gpm/ft2 and ft2, FM's own metric columns "
              "not read")


def _fm(category, ceiling, density, wet, dry, hose, k, checked):
    """One FM Global row, its SI converted exactly from the US figures FM prints."""
    def si(value, factor, places):
        return value if value in (None, NOT_HELD) else round(value * factor, places)
    return (category, ceiling, density, si(density, MM_MIN_PER_GPM_FT2, 2), wet,
            si(wet, M2_PER_FT2, 1), dry, si(dry, M2_PER_FT2, 1), hose,
            si(hose, LPM_PER_GPM, 1), 60, k, checked)


_ref("fm_criteria", "FM Global design figures for non-storage occupancies", _FM_SOURCE,
     ("hazard category", "ceiling up to", "density gpm/ft2", "density mm/min", "area wet ft2",
      "area wet m2", "area dry ft2", "area dry m2", "hose gpm", "hose L/min", "duration min",
      "least K", "checked"),
     [_fm("HC-1", "30 ft (9.1 m)", 0.10, 1500, NOT_HELD, 250, "K5.6",
          "two result sets; the dry area left out - both gave 1500 ft2, which sits oddly "
          "beside HC-2 and HC-3; K one"),
      _fm("HC-2", "30 ft (9.1 m)", 0.20, 2500, 3500, 250, "K8.0 or K11.2EC",
          "two result sets; K one"),
      _fm("HC-3", "30 ft (9.1 m)", 0.30, 2500, 3500, 500, "K11.2 or K11.2EC",
          "two result sets; K one"),
      _fm("HC-1", "60 ft (18.3 m)", 0.20, 2500, 3500, 250, NOT_HELD, "two result sets"),
      _fm("HC-2", "60 ft (18.3 m)", NOT_HELD, NOT_HELD, NOT_HELD, 250, NOT_HELD,
          "the summaries disagree - not held"),
      _fm("HC-3", "60 ft (18.3 m)", NOT_HELD, NOT_HELD, NOT_HELD, 500, NOT_HELD,
          "the summaries disagree - not held")],
     note="60 min for sprinklers and hose together at every category, as the 2019 and 2020 "
          "data sheets read - an older engineering bulletin reads otherwise. Above 60 ft "
          "(18.3 m) the data sheet refers to its Table 2a, not read. Extended coverage "
          "uprights at HC-2 and HC-3: 0.30 gpm/ft2 over 1500 ft2 with K11.2EC at 160 F, over "
          "1000 ft2 with K14.0EC (one search extract). Spacing by hazard category, data "
          "sheet 2-0, was NOT found.")

_ref("fm_hazards", "FM Global's hazard categories", _FM_SOURCE,
     ("class", "what puts a space in it", "checked"),
     [("HC-%d" % n, "FM Global's hazard category %d - data sheet 3-26's own definition was "
       "not read in this work; read it there" % n, "-") for n in (1, 2, 3)])


# --- Hazen-Williams C ------------------------------------------------------------------------

_ref("c_factors_nfpa", "Hazen-Williams C values - NFPA 13",
     "NFPA 13's table of C values (numbered 27.2.4.8.1 or 28.3.4.8.1 by edition, neither "
     "settled), as search extracts reproduce it",
     ("pipe", "C", "checked"),
     [("black steel, dry or preaction", 100, "two result sets"),
      ("black steel, wet or deluge", 120, "two result sets"),
      ("black steel, dry with nitrogen or vacuum", 120, "one search extract, a newer "
       "provision"),
      ("galvanized steel, every system", 120, "two result sets"),
      ("unlined cast or ductile iron", 100, "two result sets"),
      ("plastic, listed (CPVC)", 150, "two result sets"),
      ("copper tube", 150, "two result sets"),
      ("stainless steel", 150, "one search extract"),
      ("cement-lined cast or ductile iron", NOT_HELD, "140 and 120 both came back - not held")],
     note="the authority may set its own. NFPA 13's equivalent lengths assume C = 120: "
          "`equivalent_length` adjusts them.")

_ref("c_factors_en12845", "Hazen-Williams C values - BS EN 12845",
     _EN_SOURCE, ("pipe", "C", "checked"),
     [("cast iron", 100, "two result sets"),
      ("mild steel", 120, "two result sets"),
      ("stainless steel", 140, "two result sets"),
      ("copper", 140, "two result sets"),
      ("ductile iron", NOT_HELD, "one weak result, which another search could not find in "
       "the standard's own text - not held"),
      ("galvanised steel", NOT_HELD, "the same"),
      ("spun cement or cement-lined", NOT_HELD, "the same")])


# --- sprinklers themselves -----------------------------------------------------------------

def _f_to_c(f):
    return (f - 32.0) * 5.0 / 9.0


_ref("temperature_ratings", "Sprinkler temperature ratings, classifications and colours - "
     "NFPA 13",
     "NFPA 13-2019 Table 7.2.4.1, as search extracts reproduce it - degrees C converted "
     "exactly from the F NFPA prints",
     ("classification", "most ceiling F", "most ceiling C", "rating F", "rating C",
      "frame colour", "bulb colour", "checked"),
     [("ordinary", 100, round(_f_to_c(100), 1), "135 to 170", "57 to 77",
       "uncoloured or black", "orange or red", "two result sets; the rating in C one"),
      ("intermediate", 150, round(_f_to_c(150), 1), "175 to 225", "79 to 107", "white",
       "yellow or green", "two result sets; the rating in C one"),
      ("high", 225, round(_f_to_c(225), 1), "250 to 300", "121 to 149", "blue", "blue",
       "two result sets; the rating in C one"),
      ("extra high", 300, round(_f_to_c(300), 1), "325 to 375", "163 to 191", "red",
       "purple", "two for the ceiling, one for the rest"),
      ("very extra high", 375, round(_f_to_c(375), 1), "400 to 475", "204 to 246", "green",
       "black", "two for the ceiling in F, one for the rest"),
      ("ultra high", 475, round(_f_to_c(475), 1), "500 to 575", "260 to 302", "orange",
       "black", "two for the ceiling in F, one for the rest"),
      ("ultra high, 650 F", 625, round(_f_to_c(625), 1), "650", "343", "orange", "black",
       "one search extract")],
     note="the most ceiling temperature is what the space may reach at the sprinkler; a "
          "sprinkler near a heat source - a unit heater, a skylight, an uninsulated roof - "
          "needs a higher rating by NFPA 13's own distance table, which was not read. One "
          "maker's sheet gives 465 F for the ultra high ceiling; that is the maker's.")

_ref("temperature_en12845", "Glass bulb ratings and colours - BS EN 12845",
     _EN_SOURCE, ("bulb C", "colour", "checked"),
     [(57, "orange", "two result sets"), (68, "red", "two result sets"),
      (79, "yellow", "two result sets"), (93, "green", "two result sets"),
      (141, "blue", "two result sets")],
     note="EN 12845 asks a rating close to, and not less than, 30 C above the highest "
          "ambient temperature expected (one search extract). Its wider colour bands - "
          "green 93 to 100, blue 121 to 141, mauve 163 to 182, black 204 to 343 C - came "
          "from one extract; the fusible link colours between the ends were NOT found.")

_ref("k_factors", "Nominal K-factors - NFPA 13",
     "NFPA 13 Table 6.2.3.1 (2016 numbering), Sprinkler Discharge Characteristics "
     "Identification, as search extracts reproduce it - the metric K as NFPA prints it",
     ("K gpm/psi^0.5", "K L/min/bar^0.5 as printed", "checked"),
     [(1.4, 20, "one"), (1.9, 27, "one"), (2.8, 40, "one"), (4.2, 60, "two overlapping"),
      (5.6, 80, "one"), (8.0, 115, "one"), (11.2, 160, "two result sets"), (14.0, 200, "one"),
      (16.8, 240, "one"), (19.6, 280, "one"), (22.4, 320, "one"), (25.2, 360, "one"),
      (28.0, 400, "one")],
     note="a nominal K names a sprinkler; its LISTED K, on its data sheet, is the one a "
          "calculation uses. K 33.6 was not found. EN 12845's K57 and K80 are that "
          "standard's own nominals - NFPA prints 60 for K 4.2.")

_ref("equivalent_lengths", "Equivalent length of fittings and valves - NFPA 13",
     "NFPA 13's equivalent schedule 40 steel pipe length chart (22.4.3.1.1 in 2010; the "
     "2019 and 2022 numbers not found), as search extracts reproduce it - feet of schedule "
     "40 steel at C = 120",
     ("fitting", "nominal", "ft", "m", "checked"),
     [(f, n, ft, round(ft * M_PER_FT, 3), c) for f, n, ft, c in (
         ("swing check", "1", 5, "one search extract"),
         ("tee or cross, flow turned 90 degrees", "2", 10, "two result sets"),
         ("90 degree standard elbow", "2", 5, "one search extract"),
         ("gate valve", "2", 1, "one search extract"),
         ("gate valve", "4", 2, "one site, twice"),
         ("butterfly valve", "4", 12, "one site, twice"),
         ("swing check", "4", 22, "one site, twice"),
         ("tee or cross, flow turned 90 degrees", "6", 30, "two searches, likely one page"),
         ("gate valve", "6", 3, "two searches, likely one page"),
         ("swing check", "6", 32, "two searches, likely one page"),
         ("90 degree long-turn elbow", "6", 9, "one search extract"),
         ("butterfly valve", "6", 10, "one search extract"),
         ("gate valve", "8", 4, "one search extract"),
         ("butterfly valve", "8", 12, "one search extract"),
         ("swing check", "8", 45, "one search extract"))],
     note="only these cells came back; every other size and fitting - the 45 degree elbow, "
          "the globe and angle valves, most of the chart - was NOT read and is asked for. A "
          "tee counts only where the flow turns 90 degrees. For C other than 120 the length "
          "is multiplied by (C / 120)^1.85 (two result sets for the form).")

_ref("spacing_sidewall", "Standard sidewall sprinklers - NFPA 13",
     "NFPA 13 standard sidewall spacing, as search extracts reproduce it - three results "
     "from one set of pages",
     ("hazard", "along the wall", "across the room", "most area", "checked"),
     [("light", "14 ft (4.3 m)", "12 ft (3.7 m) under a combustible ceiling, 14 ft (4.3 m) "
       "under a noncombustible or limited-combustible one", NOT_HELD, "one set of pages"),
      ("ordinary", "10 ft (3.05 m)", "10 ft (3.05 m)", NOT_HELD, "one set of pages")],
     note="the protection areas were NOT found - a sidewall sprinkler's listing sets its own.")

_ref("spacing_extended_coverage", "Extended coverage upright and pendent sprinklers - NFPA 13",
     "NFPA 13 extended coverage spacing (11.2.2.1.2 by one source), as search extracts and "
     "makers' bulletins reproduce it",
     ("hazard", "spacing", "protection area", "checked"),
     [("light", "16 to 20 ft (4.9 to 6.1 m)", "256 to 400 ft2 (23.8 to 37.2 m2)",
       "one search extract"),
      ("ordinary", "12 to 20 ft (3.7 to 6.1 m)", "144 to 400 ft2 (13.4 to 37.2 m2)",
       "one search extract"),
      ("extra and high-piled storage", NOT_HELD, NOT_HELD, "the summary was garbled - not "
       "held")],
     note="an extended coverage sprinkler is used only within its listing, which sets its "
          "spacing, area and flow; the least distance between them was not found.")

_ref("nfpa_residential", "NFPA 13R and 13D - residential sprinkler systems, for reference",
     "NFPA 13R and NFPA 13D (editions not identified), as search extracts reproduce them",
     ("item", "NFPA 13D (one- and two-family dwellings)", "NFPA 13R (low-rise residential)",
      "checked"),
     [("design sprinklers", "every sprinkler in the compartment, at most 2",
       "at most 4 in the compartment", "two result sets for 13D, one for 13R"),
      ("density", "at least 0.05 gpm/ft2 (2.0 mm/min), or the listing's if more",
       "the same", "two result sets; the mm/min one"),
      ("duration", "10 min - 7 min for a one-storey dwelling under 2000 ft2 (186 m2)",
       "30 min", "two result sets; the 7 min condition one")],
     note="Heron does not design to either: a project following one is `other`, with every "
          "figure given.")


# --- alarms, hose and extinguishers to British standards -------------------------------------

_BS5839 = ("BS 5839-1 (2017, and 2025 where said), as makers' and the Fire Industry "
           "Association's guides reproduce it - the standard's own text not read")

_ref("bs5839_rules", "Fire detection and alarm - BS 5839-1", _BS5839,
     ("rule", "value", "checked"),
     [("point smoke detector, flat ceiling", "every point within 7.5 m of one - a 10.6 m "
       "square, 112 m2 per detector", "two result sets"),
      ("point heat detector, flat ceiling", "every point within 5.3 m of one - a 7.5 m "
       "square, 56 m2 per detector", "two result sets"),
      ("from a wall or obstruction", "at least 500 mm", "two result sets"),
      ("element below the ceiling", "smoke 25 to 600 mm; heat 25 to 150 mm",
       "two result sets"),
      ("highest ceiling, general / for up to 10 % of the area", "heat class A1 9.0 / 10.5 m; "
       "other heat classes 7.5 / 10.5 m; point smoke 10.5 / 12.5 m; optical beam 25 m",
       "two result sets; the beam's 10 % figure one"),
      ("corridors under 2 m wide", "smoke detectors up to 15 m apart and 7.5 m from the end "
       "wall; heat 10.6 m and 5.3 m, and heat is not advised in escape routes; wider than "
       "2 m, a room", "two result sets for smoke, one for heat"),
      ("beams", "deeper than 10 % of the ceiling height, treated as walls",
       "two result sets"),
      ("manual call points", "no more than 45 m along the route to one; 30 m in a straight "
       "line where the layout is not known; 25 m where the hazard is higher or a wheelchair "
       "user may operate one; 1.4 m above the floor (2025: +200 / -300 mm)",
       "two result sets - whether 2025 kept the 25 m is disputed"),
      ("sounders", "at least 65 dB(A), or 5 dB(A) over a background above 60 dB(A); 75 "
       "dB(A) at the bedhead in sleeping rooms; at most 120 dB(A); 500 to 1000 Hz",
       "two result sets")],
     note="no clause number came back; the 2025 edition was not reported to change the radii "
          "or the ceiling heights, and prefers smoke to heat detection in sleeping rooms "
          "(one).")

_ref("hose_reel_rules", "Hose reels - what each standard asks",
     "each row's own source, as search extracts reproduce it",
     ("source", "what it asks", "checked"),
     [("QCDD", "25 mm bore, 30 m of hose; every part reached by TWO hoses, each throwing at "
       "most 6 m; water for 15 min for at least two hoses - `reference qatar`",
       "two result sets"),
      ("UAE Fire and Life Safety Code", "25 mm bore and 30 m of hose (or a 40 mm hose rack); "
       "at least 4.5 bar at the most remote or topmost reel - `reference uae`",
       "two result sets"),
      ("BS 5306-1:2006", "a reel on each floor beside the exits on the escape routes, the "
       "nozzle reaching into every room and within 6 m of every part; two reels at once - "
       "the most remote and its neighbour - each at least 24 L/min (0.4 L/s); 200 kPa at "
       "the highest reel", "two result sets; 200 kPa one"),
      ("BS EN 671-1", "19, 25 or 33 mm bore; at most 30 m of hose in one piece; one maker's "
       "25 mm reel with an 8 mm nozzle gives 39 L/min at 2 bar - the standard's own flow "
       "table was not found", "two result sets; the flow one")])

_ref("bs9990_rules", "Fire mains - BS 9990 and Approved Document B",
     "BS 9990:2015 and Approved Document B guidance pages, as search extracts reproduce them",
     ("rule", "value", "checked"),
     [("when a fire main is needed", "a floor more than 18 m above the fire service access "
       "level, or a basement more than 10 m below it", "two result sets"),
      ("dry or wet", "18 to 50 m: either; any floor more than 50 m up: wet",
       "two result sets"),
      ("hose distance", "every part within 60 m of an outlet in a firefighting shaft along a "
       "hose-laying route where sprinklered; unsprinklered, also within 45 m of one in a "
       "protected stair", "two result sets"),
      ("wet main", "a pump of at least 1500 L/min serving TWO landing valves at once, each "
       "(750 +/- 75) L/min at (8 +/- 0.5) bar through pressure-reducing landing valves",
       "two result sets; the tolerances one"),
      ("dry main inlet", "a fire appliance within 18 m of it", "one search extract")],
     note="'1500 L/min for three landing valves at 4 to 5 bar' is the withdrawn BS 5306-1:1976, "
          "not BS 9990; Singapore's 27 L/s figures came back labelled British and are not "
          "held.")

_ref("bs5306_8", "Portable extinguishers - BS 5306-8:2023",
     "BS 5306-8:2023, as the NSI's technical bulletin and makers' guides reproduce it",
     ("storey", "at least", "combined Class A rating", "checked"),
     [("up to 50 m2", "1 extinguisher", "at least 26A", "two result sets"),
      ("over 50 m2, up to 400 m2", "2 extinguishers", "at least 26A", "two result sets"),
      ("over 400 m2", "2 extinguishers", "at least 0.065 x the floor area in m2",
       "two result sets")],
     note="no Class A extinguisher more than 30 m of travel away (two result sets). An EN 3 "
          "rating - 13A, 21A - is NOT an NFPA 10 rating - 2-A, 4-A: the two scales are "
          "measured differently and do not convert.")


# --- Qatar, the UAE, and the owner's file --------------------------------------------

_ref("qatar", "What is known of Qatar's fire requirements",
     "Qatar Civil Defence's published documents - the Civil Defence Technical Requirements "
     "Guide (2022), the Fire Safety Guidelines and their annex (revision 2015) - as search "
     "extracts reproduce them; none was opened in this work",
     ("item", "what is known", "checked"),
     [("the authority's document", "the Civil Defence Technical Requirements Guide - "
       "Ministry of Interior, General Directorate of Civil Defence, launched March 2022, "
       "updating the 2015 Fire Safety Guidelines and their annex; a copy marked 2023 "
       "circulates", "two search extracts"),
      ("NFPA in Qatar", "the guide adopts NFPA as its principal reference for whatever it "
       "does not set itself; NFPA 10, 13, 14, 20, 22, 24, 25, 70, 72, 90A, 90B, 92, 101, "
       "110, 111, 170, 204, 701 and 5000 are named; which editions was not established - "
       "NFPA 101 and 5000 of 2012 were named once, perhaps in the 2015 guidelines",
       "two result sets for the adoption, one for the list"),
      ("QCS", "QCS 2014 Section 23, Fire Fighting and Fire Alarm Systems (Part 1 General, "
       "Part 2 Fire Alarm and Detection), its Part 03 analogue addressable fire alarm "
       "systems with panels, detectors and cables approved by Civil Defence; QCS 2024 "
       "exists, its fire section not established", "two search extracts for Section 23, "
       "one for Part 03"),
      ("which buildings are sprinklered", "a building takes the next higher class unless it "
       "meets BOTH its height and its area limit; a basement of two or more levels, or over "
       "1,115 m2, is sprinklered; hotels: low-rise to 15 m and 500 m2, medium-rise to 28 m "
       "and 1,000 m2, high-rise above - the copies disagree whether 28 m itself is medium "
       "or high", "two result sets for the basements and the hotel classes, one for the "
       "both-limits rule"),
      ("hose reels", "25 mm (1 in) bore, 30 m of hose; every part of a floor reached by TWO "
       "hoses, each with a throw of at most 6 m (a longer throw needs hydraulic "
       "justification); water for 15 min for at least two hoses; their flow and pressure "
       "were NOT found", "two result sets, the justification one"),
      ("standpipes and landing valves", "a standpipe where the highest habitable floor is "
       "more than 9 m up; 65 mm landing valves on every level; 250 gpm at 100 psi (946 L/min "
       "at 6.9 bar) at an outlet; NFPA 14 for whatever the guide does not set",
       "two result sets for 9 m, 65 mm and NFPA 14; one for the flow, which is NFPA 14's"),
      ("fire pumps", "listed or approved, sized to NFPA 20 for the single largest system "
       "demand; at least one duty and one standby pump", "two result sets for NFPA 20, one "
       "for duty and standby"),
      ("fire water tank", "non-combustible, in two compartments, each at most 50 % of the "
       "volume with its own automatic inlet, so one can be maintained while the other "
       "serves; an automatic infill that refills the tank in 6 hours", "two result sets for "
       "50 % and 6 h, one for the inlets"),
      ("fire water storage", "the annex's own tables by building class: 24,000 US gal "
       "(90.8 m3) for 60 min at ordinary hazard; 1,500 to 3,000 gal (5.7 to 11.4 m3) for "
       "30 min at low-rise light hazard; 19,500 to 60,000 gal (73.8 to 227 m3) for a "
       "medium-rise building - which row carries which figure, and the high-rise duration, "
       "differ between copies, so none is applied", "two result sets for each figure, the "
       "rows unsettled"),
      ("extinguishers", "selected and placed to NFPA 10; a Qatar travel distance was NOT "
       "found", "two result sets"),
      ("fire alarm", "to NFPA 72, its latest edition; detector spacing and the distance to a "
       "manual call point were NOT found, and BS 5839 did not appear", "one search extract")],
     note="ask the fire consultant for the guide's own tables before relying on any row - "
          "two result sets agreeing may still be two copies of one document, and the annex "
          "is revision 2015 where the guide is 2022.")

_ref("uae", "What is known of the UAE Fire and Life Safety Code of Practice",
     "the UAE Fire and Life Safety Code of Practice, chapter 9 (copies marked 2017 and "
     "September 2018), as search extracts reproduce it - none was opened in this work, and "
     "which edition each figure is from was not established",
     ("item", "what is known", "checked"),
     [("hose reels", "25 mm bore, 30 m of reinforced rubber hose in a cabinet, or a 40 mm "
       "hose rack; at least 4.5 bar at the most remote or topmost reel", "two result sets"),
      ("landing valves", "65 mm; at least 250 gpm at 6.9 bar (946 L/min) at the most remote "
       "or topmost; at most 12 bar residual at any landing valve, and above 7 bar a "
       "pressure-regulating landing valve set to 7 bar", "two result sets for 65 mm and, "
       "weakly, 12 bar; one for the rest"),
      ("fire pumps", "a main electric pump and a standby diesel pump with their controllers, "
       "on the lowest - ground or fire access - level, pumping upward; pumps above it "
       "feeding down are not allowed", "two result sets"),
      ("fire water storage", "at least 60 min of the fire pump set's capacity, in a tank of "
       "two compartments or two interconnected tanks", "two overlapping result sets for "
       "60 min, one for the compartments")],
     note="the hazard classes and densities that came back were Singapore's and EN 12845's, "
          "not the UAE's, and are not held; a 25 m extinguisher distance came only from "
          "suppliers' pages and conflicts with NFPA 10's 22.9 m, so it is not held either.")

_ref("coverage_rules", "Distances a coverage check is held to - OFFERED, the rule is the "
     "project's",
     "each row's own source, as the tables it comes from say",
     ("device", "every point within", "reached by", "source", "checked"),
     [("hose reel, Qatar", "30 m of hose along its route, then a throw of at most 6 m", 2,
       "QCDD - `reference qatar`", "two result sets"),
      ("hose reel, UAE", "30 m of hose along its route", "not found",
       "UAE Fire and Life Safety Code - `reference uae`", "two result sets"),
      ("Class A extinguisher", "22.9 m (75 ft) of travel", 1,
       "NFPA 10 Table 6.2.1.1 - `reference extinguisher_rules`", "search extracts"),
      ("Class K extinguisher", "9.1 m (30 ft) of travel from the hazard", 1,
       "NFPA 10 6.6.2", "search extracts"),
      ("spot smoke detector, the second way", "0.7 S - 6.4 m (21 ft) at S = 9.1 m", 1,
       "NFPA 72 - `reference detector_rules`", "two search extracts"),
      ("manual fire alarm box", "61 m (200 ft) of travel on the same floor", 1,
       "NFPA 72 - `reference detector_rules`", "two search extracts"),
      ("hose reel, BS 5306-1", "the nozzle within 6 m of every part, the hose reaching into "
       "every room", 1, "BS 5306-1:2006 - `reference hose_reel_rules`", "two result sets"),
      ("Class A extinguisher, BS 5306-8", "30 m of travel", 1,
       "BS 5306-8:2023 - `reference bs5306_8`", "two result sets"),
      ("point smoke detector, BS 5839-1", "7.5 m, flat ceiling", 1,
       "BS 5839-1 - `reference bs5839_rules`", "two result sets"),
      ("point heat detector, BS 5839-1", "5.3 m, flat ceiling", 1,
       "BS 5839-1 - `reference bs5839_rules`", "two result sets"),
      ("manual call point, BS 5839-1", "45 m along the route; 30 m in a straight line where "
       "the layout is not known", 1, "BS 5839-1 - `reference bs5839_rules`",
       "two result sets"),
      ("fire main outlet, Approved Document B", "60 m along a hose-laying route where "
       "sprinklered", 1, "`reference bs9990_rules`", "two result sets")],
     note="coverage_check measures STRAIGHT LINES in plan. A hose runs round walls and a "
          "travel distance is walked, both longer than the line: a straight-line check can "
          "FAIL a layout, never pass one by itself.")

_ref("office_firefighting", "The owner's own firefighting figures, beside NFPA's as held here",
     "the owner's own firefighting knowledge file, read 2026-10-02",
     ("figure", "the owner's file", "NFPA, as held here"),
     [("light hazard area per sprinkler", "20.9 m2 (225 ft2)", "225 ft2 hydraulically "
       "calculated; 200 ft2 (18.6 m2) on a pipe schedule; less under some combustible "
       "construction"),
      ("ordinary hazard area per sprinkler", "12.1 m2 (130 ft2)", "the same"),
      ("extra hazard area per sprinkler", "9.3 m2 (100 ft2)", "90 ft2 (8.4 m2) on a pipe "
       "schedule; the hydraulic row was not read"),
      ("spacing", "4.6 m most (light, ordinary); 1.8 m least; half the spacing, at most "
       "2.3 m, to a wall; 100 mm least to a wall", "the same"),
      ("deflector below the ceiling", "25 to 300 mm", "1 to 12 in, 25 to 305 mm"),
      ("clearance below a deflector to storage", "450 mm least", "not read in this work"),
      ("density and area", "4.1, 6.1 and 8.1 mm/min over 139 m2", "the same over 140 m2 "
       "in the 2022 SI"),
      ("light hazard pipe schedule", "25 mm 2, 32 mm 3, 40 mm 5, 50 mm 10, 65 mm 30",
       "the same, in steel"),
      ("standpipe flow", "500 gpm and 250 gpm for each more, at most 1250 gpm",
       "at most 1000 gpm sprinklered throughout, 1250 gpm otherwise"),
      ("standpipe residual", "6.9 bar at the topmost outlet", "the same"),
      ("fire pump", "churn at most 140 %; at least 65 % at 150 % flow", "the same"),
      ("jockey pump", "about 1 % of the main pump's flow, as practice",
       "the annex: the leakage in 10 minutes or 1 gpm, and less than one sprinkler"),
      ("extinguisher travel", "23 m at most", "75 ft, 22.9 m"),
      ("smoke detector spacing", "about 9.1 m, one per about 81 m2", "9.1 m; an S x S square "
       "is 83 m2"),
      ("manual call point travel", "30 m - common QCDD practice, to verify",
       "NFPA 72: 61 m (200 ft)"),
      ("kitchens", "ordinary hazard group 2", "the annex lists restaurant service areas in "
       "group 1")],
     note="where the two differ, docs/42 s12 records it and F43 asks the owner which governs; "
          "neither is applied by Heron.")



# ---------------------------------------------------------------------------
# Running one, and saying what came out - through the HVAC engine's own
# runner and renderer, so the two engines' answers cannot drift apart.
# ---------------------------------------------------------------------------

DISCIPLINE = "Fire protection"

# The catalogue in the order a design runs: classify, size the area, lay out,
# size the pipe, calculate, supply, the devices beside, then the helpers.
GROUP_ORDER = ("hazard", "design", "layout", "pipe", "hydraulics", "supply", "devices",
               "units", "reference")
_ordered = sorted(CALCULATIONS.items(), key=lambda kv: GROUP_ORDER.index(kv[1]["group"]))
CALCULATIONS.clear()
CALCULATIONS.update(_ordered)


def run(name, inputs=None, recorded=None):
    """
    One calculation, as a dict whose `status` is ok, missing, refused or
    unknown - heron_hvac.run's shape exactly. `recorded` is the project's
    governing standards as kept for it (D-111), the caller's to keep.
    """
    return BASE.run(name, inputs, recorded=recorded, calculations=CALCULATIONS)


def catalogue():
    """Every calculation and what it needs - derived by running each on nothing."""
    return BASE.catalogue(CALCULATIONS)


def describe(answer):
    """The answer as the text a modeller reads."""
    return BASE.describe(answer, discipline=DISCIPLINE, disclaimer=DISCLAIMER)


def describe_catalogue():
    """Every calculation and what it needs, as text."""
    lines = ["Heron's fire protection design calculations. Each takes its inputs as named "
             "values and supplies no design value it was not given (D-33).", ""]
    group = None
    for entry in catalogue():
        if entry["group"] != group:
            group = entry["group"]
            lines.append(group.upper())
        lines.append("  %s - %s" % (entry["calculation"], entry["title"]))
        lines.append("      %s" % entry["purpose"])
        if entry["needs"]:
            lines.append("      needs: %s" % ", ".join(entry["needs"]))
        if entry["optional"]:
            lines.append("      may take: %s" % ", ".join(entry["optional"]))
        if entry["asks_once"]:
            lines.append("      asks once per project: %s" % ", ".join(entry["asks_once"]))
    lines.append("")
    lines.append("A project's governing standards - which standard governs its sprinklers "
                 "(an NFPA 13 edition, BS EN 12845 or FM Global) and who approves the design - "
                 "are asked once and kept for that project (D-111). The hazard class is always "
                 "the engineer's: `hazard_class` shows what the lists hold.")
    lines.append("")
    lines.append(DISCLAIMER)
    return "\n".join(lines)


def main(argv):
    if len(argv) < 2:
        print(describe_catalogue())
        return 0
    name = argv[1]
    if name == "reference":
        inputs = {"table": argv[2]} if len(argv) > 2 else {}
        if len(argv) > 3:
            inputs["search"] = argv[3]
    else:
        try:
            inputs = parse_inputs(argv[2] if len(argv) > 2 else "")
        except Refused as why:
            print("refused: %s" % why)
            return 2
    answer = run(name, inputs)
    print(describe(answer))
    return 0 if answer["status"] == "ok" else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
