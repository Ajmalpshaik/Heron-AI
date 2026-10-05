# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Every sprinkler checked against its Space - docs/46 section 13.2.

REPORT_SPRINKLER_NETWORK gives, for every MEP Space a head of the system sits
in, the Space's outline in plan, and each head its Space. This runs
heron_fire's `sprinkler_spacing` on each Space - each head's S, L, area and
wall distance as NFPA 13 measures them - with the hazard class the ENGINEER
gave that Space and the limits the modeller gave that class. Nothing about a
limit or a class is supplied here (D-33): a Space with no class is "not
checked", never "ok", and a class with no limits is asked, with the
standard's figures for that class offered.

WHICH WAY THE BRANCH LINES RUN is read from the model's level pipes inside
the Space, in two steps. The GRID's direction comes from the mean of four
times each pipe's angle - so branch lines and the cross main, 90 degrees
apart, agree instead of cancelling (the plan's review, R1). Which of the
grid's two axes is the branch line is the one that carries more of the
smallest pipe size there. A grid whose pipes do not agree (their four-times
mean under 0.9 of their length) is ASKED, never guessed - a few degrees wrong
splits every branch line and fails every head. It is shown with how many
metres of pipe it was read from, and the modeller can change it. The Space
and its heads are turned by that angle before the check, so a building at an
angle is measured along its own branch lines - the check itself only knows x
and y. Turning a drawing is the only arithmetic here.

AN EDGE THAT IS NOT A WALL. A Space may be bounded by a separation line
rather than a wall; the check holds every edge as a wall. A Space with
separation-line edges is never reported plainly "ok" (the review, R5).

Standard library and heron_fire only.
"""

import math

import heron_fire as FIRE
import heron_sprinkler_takeoff as TAKEOFF

FORMAT = 1
LIMITS = ("max_spacing_m", "max_area_m2", "max_wall_distance_m", "min_spacing_m",
          "min_wall_distance_m")
REQUIRED = ("max_spacing_m", "max_area_m2", "max_wall_distance_m")
#: A pipe is horizontal when its ends differ in height by less than this per metre of run.
LEVEL_SLOPE = 0.05
#: How well the level pipes must agree on one grid before its direction is used.
AGREEMENT = 0.9
#: A pipe within this many degrees of a grid axis runs along it.
ALONG_DEG = 10.0
STATUS = ("ok", "fail", "check", "asked", "not checked", "refused")
SEPARATION = ("the Space is partly bounded by separation lines, which the check holds as "
              "walls - a head near one is held to a wall that is not there, and its neighbour "
              "across the line is not seen; check those heads against the next Space")


def _value(v):
    return v.get("value") if isinstance(v, dict) else v


def _inside(poly, x, y):
    return FIRE.inside(poly, x, y)


def _outline_m(raw):
    """The outline as [(x, y)] metres, a closing point that repeats the first left out."""
    pts = [(float(p[0]), float(p[1])) for p in raw or []
           if isinstance(p, (list, tuple)) and len(p) >= 2]
    while len(pts) > 1 and abs(pts[0][0] - pts[-1][0]) < 1e-6 and abs(pts[0][1] - pts[-1][1]) < 1e-6:
        pts.pop()
    return pts


def spaces(n):
    """
    ({space id: {"id", "number", "name", "level", "area_m2", "outline" [(x, y) m] or None,
    "heads": [(head id, x m, y m)]}}, [heads in no Space]). Only Spaces with a head.
    """
    n = TAKEOFF.read(n)
    held = dict((str(s.get("id")), s) for s in n.raw.get("spaces") or [])
    out, loose = {}, []
    for e in n.elements:
        if e.get("kind") != "sprinkler":
            continue
        at = e.get("at")
        sid = e.get("space_id")
        if sid is None or str(sid) not in held or not isinstance(at, (list, tuple)):
            loose.append(str(e["id"]))
            continue
        s = held[str(sid)]
        entry = out.setdefault(str(sid), {
            "id": str(sid), "number": s.get("number"), "name": s.get("name"),
            "level": s.get("level"), "area_m2": s.get("area_m2"),
            "outline": _outline_m(s.get("outline")) or None,
            "separation_edges": int(s.get("separation_edges") or 0),
            "inner_loops": int(s.get("inner_loops") or 0), "heads": []})
        entry["heads"].append((str(e["id"]), float(at[0]), float(at[1])))
    return out, loose


def label(space):
    return ("%s %s" % (space.get("number") or "", space.get("name") or "")).strip() or space["id"]


def branch_angle(n, space, graph=None):
    """
    (degrees 0 to 180 from model x, metres read, how) - the branch lines' direction in the
    Space from its level pipes, or (None, metres, why not). `graph` is TAKEOFF.graph(n),
    passed in when many Spaces are read from one network.
    """
    outline = space.get("outline")
    if not outline:
        return None, 0.0, "the Space has no outline"
    records, segments = graph or TAKEOFF.graph(n)
    level = []
    for seg in segments:
        a, b = records[seg["a"]].get("at"), records[seg["b"]].get("at")
        if not a or not b:
            continue
        run = math.hypot(b[0] - a[0], b[1] - a[1])
        if run < 1e-6 or abs(b[2] - a[2]) > LEVEL_SLOPE * run:
            continue
        if not (_inside(outline, a[0], a[1]) and _inside(outline, b[0], b[1])):
            continue
        dn = seg["e"].get("nominal_diameter_m")
        level.append((run, math.atan2(b[1] - a[1], b[0] - a[0]), dn))
    metres = sum(r for r, _t, _d in level)
    if metres <= 0:
        return None, 0.0, "no level pipe was found inside the Space"
    c4 = sum(r * math.cos(4 * t) for r, t, _d in level)
    s4 = sum(r * math.sin(4 * t) for r, t, _d in level)
    agree = math.hypot(c4, s4) / metres
    if agree < AGREEMENT:
        return None, metres, ("the level pipes in the Space do not run on one grid (they agree "
                              "%.2f, under %.2f) - give the angle" % (agree, AGREEMENT))
    grid = math.degrees(math.atan2(s4, c4) / 4.0) % 90.0
    smallest = min((d for _r, _t, d in level if d), default=None)

    def along(axis, only_smallest):
        total = 0.0
        for r, t, d in level:
            if only_smallest and d != smallest:
                continue
            gap = abs((math.degrees(t) - axis + 90.0) % 180.0 - 90.0)
            if gap <= ALONG_DEG:
                total += r
        return total
    first, second = grid, (grid + 90.0) % 180.0
    a1, a2 = along(first, True), along(second, True)
    if abs(a1 - a2) < 1e-9:
        a1, a2 = along(first, False), along(second, False)
    if abs(a1 - a2) < 1e-9:
        return None, metres, ("the pipes run equally both ways in the Space, so which way the "
                              "branch lines run cannot be told - give the angle")
    angle = first if a1 > a2 else second
    return round(angle, 3), metres, ("%.1f m of level pipe on a grid at %.1f degrees; the "
                                     "branch lines are the axis with more of the smallest "
                                     "pipe" % (metres, grid))


def _turned(points, origin, degrees):
    """Points turned by -degrees about origin, in mm - so the branch lines lie along x."""
    t = -math.radians(degrees)
    c, s = math.cos(t), math.sin(t)
    out = []
    for p in points:
        x, y = p[-2] - origin[0], p[-1] - origin[1]
        out.append((x * c - y * s) * 1000.0)
        out.append((x * s + y * c) * 1000.0)
    return [[out[i], out[i + 1]] for i in range(0, len(out), 2)]


def _family(standard):
    return FIRE.family_said(standard) or "nfpa"


def _turned_back(x_mm, y_mm, origin, degrees):
    """A point of the turned frame, in mm, back to model coordinates in metres."""
    t = math.radians(degrees)
    x, y = x_mm / 1000.0, y_mm / 1000.0
    return (round(origin[0] + x * math.cos(t) - y * math.sin(t), 3),
            round(origin[1] + x * math.sin(t) + y * math.cos(t), 3))


def check(n, inputs, standard=None):
    """
    Every Space with a head, checked. `inputs` is {"hazard": {space id: class},
    "limits": {class: {criterion: value}}, "angle_deg": {space id: degrees}}.

    Returns {"spaces": [...], "loose": [head ids in no Space], "asked": [...],
    "classes": the class names the standard holds}. Each Space: {"id", "label",
    "level", "heads", "hazard", "angle_deg", "angle_from", "status", "why",
    "answer"}.
    """
    n = TAKEOFF.read(n)
    inputs = inputs if isinstance(inputs, dict) else {}
    if "spaces" not in n.raw:
        # Read before the Spaces were part of the read (the review, R4): never
        # "every head in no Space".
        return {"status": "not read", "spaces": [], "loose": [], "asked": [],
                "why": "this read of the model did not include the Spaces - read the model "
                       "again to check the spacing", "standard": standard}
    if inputs.get("_error"):
        return {"status": "refused", "spaces": [], "loose": [], "asked": [],
                "why": "the spacing inputs could not be read: %s" % inputs["_error"],
                "standard": standard}
    hazards = dict((str(k), _value(v)) for k, v in (inputs.get("hazard") or {}).items()
                   if _value(v) not in (None, ""))
    angles = dict((str(k), _value(v)) for k, v in (inputs.get("angle_deg") or {}).items()
                  if _value(v) not in (None, ""))
    family = _family(standard)
    limits = {}
    for k, v in (inputs.get("limits") or {}).items():
        key = FIRE.hazard_key(k, family) or str(k)
        limits[key] = dict((c, _value(x)) for c, x in (v or {}).items()
                           if c in LIMITS and _value(x) not in (None, ""))
    found, loose = spaces(n)
    graph = TAKEOFF.graph(n)
    out = {"status": "ok", "spaces": [], "loose": loose, "asked": [],
           "classes": FIRE.HAZARD_NAMES[family], "standard": standard}
    asked_for = set()
    for sid in sorted(found, key=lambda k: (found[k].get("level") or "", label(found[k]))):
        sp = found[sid]
        model_angle, metres, how = branch_angle(n, sp, graph)
        given = angles.get(sid)
        row = {"id": sid, "label": label(sp), "level": sp.get("level"),
               "heads": [h[0] for h in sp["heads"]], "hazard": hazards.get(sid),
               "angle_deg": None, "angle_from": None, "model_angle_deg": model_angle,
               "model_angle_m": round(metres, 2), "model_angle_how": how,
               "status": None, "why": None, "warns": [], "answer": None, "farthest": None,
               "separation_edges": sp.get("separation_edges", 0),
               "inner_loops": sp.get("inner_loops", 0)}
        out["spaces"].append(row)
        if given is not None:
            try:
                row["angle_deg"] = float(given) % 180.0
                row["angle_from"] = "the modeller"
            except (TypeError, ValueError):
                row.update(status="refused", why="the branch angle %r is not a number" % given)
                continue
        elif model_angle is not None:
            row["angle_deg"] = model_angle
            row["angle_from"] = "the model's pipes: %s" % how
        if not sp.get("outline") or len(sp["outline"]) < 3:
            row.update(status="refused", why="the Space's outline could not be read from the "
                       "model - check it is placed and bounded")
            continue
        if row["hazard"] is None:
            row.update(status="not checked", why="no hazard class given for this Space")
            continue
        key = FIRE.hazard_key(row["hazard"], family)
        if key is None:
            row.update(status="refused", why="%r is not a class of %s - its classes are %s" % (
                row["hazard"], FIRE.FAMILIES[family][0], FIRE.HAZARD_NAMES[family]))
            continue
        row["hazard"] = key
        held = limits.get(key) or {}
        missing = [c for c in REQUIRED if c not in held]
        if missing:
            row.update(status="asked", why="the limits for %s are not all given" % key)
            if key not in asked_for:
                asked_for.add(key)
                offers = FIRE.spacing_offers(key, standard)
                for c in LIMITS:
                    if c not in held:
                        out["asked"].append({
                            "input": "spacing.limits.%s.%s" % (key, c),
                            "unit": "m2" if c.endswith("m2") else "m",
                            "why": "%s for %s%s" % (c.replace("_", " "), key,
                                                    "" if c in REQUIRED else " (optional)"),
                            "offer": offers.get(c), "required": c in REQUIRED})
            continue
        if row["angle_deg"] is None:
            row.update(status="asked", why="which way the branch lines run is not known - %s"
                       % how)
            out["asked"].append({"input": "spacing.angle_deg.%s" % sid, "unit": "degrees from "
                                 "model x", "why": "which way the branch lines run in %s"
                                 % row["label"], "offer": None, "required": True})
            continue
        origin = sp["outline"][0]
        outline = _turned(sp["outline"], origin, row["angle_deg"])
        heads = _turned([(h[1], h[2]) for h in sp["heads"]], origin, row["angle_deg"])
        inputs_for = {"branch_axis": "x", "hazard": key, "outline_mm": outline,
                      "sprinklers": [{"id": h[0], "x": p[0], "y": p[1]}
                                     for h, p in zip(sp["heads"], heads)]}
        if standard:
            inputs_for["sprinkler_standard"] = standard
        inputs_for.update(held)
        got = FIRE.run("sprinkler_spacing", inputs_for)
        row["answer"] = got
        if got["status"] == "ok":
            fails = [t for lv, t in got["checks"] if lv == "FAIL"]
            row["warns"] = [t for lv, t in got["checks"] if lv == "WARN"]
            far = (got.get("data") or {}).get("farthest")
            if far:
                x, y = _turned_back(far["x_mm"], far["y_mm"], origin, row["angle_deg"])
                row["farthest"] = {"x_m": x, "y_m": y, "distance_m": far["distance_m"],
                                   "beyond_m": far["beyond_m"]}
            row.update(status="fail" if fails else "ok", why="; ".join(fails) or None)
            if row["separation_edges"]:
                row["warns"].insert(0, SEPARATION)
                if not fails:
                    row.update(status="check", why=SEPARATION)
        elif got["status"] == "missing":
            row.update(status="asked", why="; ".join(m["input"] for m in got["missing"]))
        else:
            row.update(status="refused", why="; ".join(got["refused"][:2]))
    return out


def head_status(result):
    """{head id: "ok" | "fail" | "not checked" | "no Space"} - for the 3D view."""
    out = {}
    sp = (result or {}).get("spacing") or {}
    for row in sp.get("spaces") or []:
        data = ((row.get("answer") or {}).get("data") or {}).get("heads") or {}
        for h in row.get("heads") or []:
            if row.get("status") in ("ok", "fail", "check") and h in data:
                out[h] = "fail" if data[h]["fails"] else (
                    "check" if row.get("status") == "check" else "ok")
            else:
                out[h] = "not checked"
    for h in sp.get("loose") or []:
        out[h] = "no Space"
    return out


def summary_line(spacing):
    """One line for the chat."""
    if (spacing or {}).get("status") in ("not read", "refused"):
        return "Spacing: not checked - %s." % spacing["why"]
    rows = (spacing or {}).get("spaces") or []
    if not rows and not (spacing or {}).get("loose"):
        return ""
    count = dict((s, sum(1 for r in rows if r["status"] == s)) for s in STATUS)
    line = "Spacing: %d Space(s) with heads - %d ok, %d failing, %d to check by eye, %d not " \
        "checked" % (len(rows), count["ok"], count["fail"], count["check"],
                     count["not checked"] + count["asked"] + count["refused"])
    if (spacing or {}).get("loose"):
        line += "; %d head(s) in no Space" % len(spacing["loose"])
    return line + "."
