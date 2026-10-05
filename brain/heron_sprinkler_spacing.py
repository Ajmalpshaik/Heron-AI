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

WHICH WAY THE BRANCH LINES RUN is read from the model: the length-weighted
direction of the horizontal pipes inside the Space. It is shown with how many
metres of pipe it was read from, and the modeller can change it. The Space
and its heads are turned by that angle before the check, so a building at an
angle is measured along its own branch lines - the check itself only knows x
and y. Turning a drawing is the only arithmetic here.

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
STATUS = ("ok", "fail", "asked", "not checked", "refused")


def _value(v):
    return v.get("value") if isinstance(v, dict) else v


def _inside(poly, x, y):
    hit = False
    j = len(poly) - 1
    for i in range(len(poly)):
        xi, yi = poly[i]
        xj, yj = poly[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / ((yj - yi) or 1e-30) + xi:
            hit = not hit
        j = i
    return hit


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
            "outline": _outline_m(s.get("outline")) or None, "heads": []})
        entry["heads"].append((str(e["id"]), float(at[0]), float(at[1])))
    return out, loose


def label(space):
    return ("%s %s" % (space.get("number") or "", space.get("name") or "")).strip() or space["id"]


def branch_angle(n, space):
    """
    (degrees 0 to 180 from model x, metres read) - the length-weighted direction of the
    horizontal pipe segments with both ends inside the Space's outline, by the doubled-angle
    mean so 10 and 170 degrees average to 0, not 90. (None, 0) with no such pipe.
    """
    outline = space.get("outline")
    if not outline:
        return None, 0.0
    records, segments = TAKEOFF.graph(n)
    c2 = s2 = metres = 0.0
    for seg in segments:
        a, b = records[seg["a"]].get("at"), records[seg["b"]].get("at")
        if not a or not b:
            continue
        run = math.hypot(b[0] - a[0], b[1] - a[1])
        if run < 1e-6 or abs(b[2] - a[2]) > LEVEL_SLOPE * run:
            continue
        if not (_inside(outline, a[0], a[1]) and _inside(outline, b[0], b[1])):
            continue
        t = math.atan2(b[1] - a[1], b[0] - a[0])
        c2 += run * math.cos(2 * t)
        s2 += run * math.sin(2 * t)
        metres += run
    if metres <= 0 or math.hypot(c2, s2) < 1e-9:
        return None, metres
    return round(math.degrees(math.atan2(s2, c2) / 2.0) % 180.0, 3), metres


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
    return FIRE.family_of(standard) or "nfpa"


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
    out = {"spaces": [], "loose": loose, "asked": [],
           "classes": FIRE.HAZARD_NAMES[family], "standard": standard}
    asked_for = set()
    for sid in sorted(found, key=lambda k: (found[k].get("level") or "", label(found[k]))):
        sp = found[sid]
        model_angle, metres = branch_angle(n, sp)
        given = angles.get(sid)
        row = {"id": sid, "label": label(sp), "level": sp.get("level"),
               "heads": [h[0] for h in sp["heads"]], "hazard": hazards.get(sid),
               "angle_deg": None, "angle_from": None, "model_angle_deg": model_angle,
               "model_angle_m": round(metres, 2), "status": None, "why": None,
               "answer": None}
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
            row["angle_from"] = "the model's pipes, %.1f m read" % metres
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
            row.update(status="asked", why="which way the branch lines run is not known - no "
                       "level pipe was found inside this Space; give the angle")
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
            row.update(status="fail" if fails else "ok", why="; ".join(fails) or None)
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
            if row.get("status") in ("ok", "fail") and h in data:
                out[h] = "fail" if data[h]["fails"] else "ok"
            else:
                out[h] = "not checked"
    for h in sp.get("loose") or []:
        out[h] = "no Space"
    return out


def summary_line(spacing):
    """One line for the chat."""
    rows = (spacing or {}).get("spaces") or []
    if not rows and not (spacing or {}).get("loose"):
        return ""
    count = dict((s, sum(1 for r in rows if r["status"] == s)) for s in STATUS)
    line = "Spacing: %d Space(s) with heads - %d ok, %d failing, %d not checked" % (
        len(rows), count["ok"], count["fail"], count["not checked"] + count["asked"]
        + count["refused"])
    if (spacing or {}).get("loose"):
        line += "; %d head(s) in no Space" % len(spacing["loose"])
    return line + "."
