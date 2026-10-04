# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
What the Companion's 3D view draws - docs/44 section 12.

The SAME faces the loads were worked out from: every Space's walls, roof,
floor and partitions as REPORT_SPACE_ENVELOPE outlined them, the windows and
doors in them, and - for each face - what it was counted as in the load, so
the modeller can look at the building and check the take-off before anything
is written back into Revit.

Everything that MEANS something is decided here: the categories, what each
colour stands for, the legends, the numbers shown on a click. The page only
draws (mcp/companion README rule 4). Model text is carried as data and the
page puts it on screen as text.

It adds no physics: areas, U-values and loads are the take-off's and the
run's own; the only arithmetic is placing an opening's rectangle on its face
and moving the drawing near the origin.

Standard library only.
"""

import math

import heron_takeoff as TAKEOFF

FORMAT = 1
# An opening is drawn this far proud of its wall, so it shows in front of it.
PROUD_M = 0.02

# category: (colour, what it is, in the modeller's words)
CATEGORIES = (
    ("outside_wall", "#b98b5e", "wall to outside"),
    ("wall_unconditioned", "#d6a874", "wall to an unconditioned space"),
    ("wall_between_spaces", "#e4ded3", "wall between two Spaces - no load"),
    ("wall_unknown", "#d9534f", "wall - nothing found beyond it"),
    ("roof", "#8a5a44", "roof"),
    ("ceiling_unconditioned", "#d8c3a5", "ceiling to an unconditioned space"),
    ("ceiling_between_spaces", "#efe9dd", "ceiling under another Space - no load"),
    ("ceiling_unknown", "#d9534f", "ceiling - nothing found above it"),
    ("exposed_floor", "#a26b4a", "floor open to outside below"),
    ("floor_unconditioned", "#c9b79c", "floor over an unconditioned space"),
    ("floor_between_spaces", "#ddd5c7", "floor over another Space - no load"),
    ("floor_unknown", "#8f8f8f", "floor - nothing found below it (ground?)"),
    ("window", "#4f9fd1", "window"),
    ("curtain_panel", "#6fb7d9", "curtain wall panel"),
    ("skylight", "#4f9fd1", "skylight"),
    ("door", "#7a5638", "door"),
)
_CATEGORY = dict((c[0], c) for c in CATEGORIES)
BEYOND = (("outside", "#e07a3a", "outside"),
          ("space", "#d9d4ca", "another Space"),
          ("unconditioned", "#c58f5a", "an unconditioned space"),
          ("unknown", "#d9534f", "nothing found"))
FACING = (("N", "#3b6fb6", "faces north (315 to 45 degrees)"),
          ("E", "#3ba86b", "faces east (45 to 135)"),
          ("S", "#e0a030", "faces south (135 to 225)"),
          ("W", "#c0503a", "faces west (225 to 315)"))
NOT_FACING = ("#cfcac0", "roof, floor or ceiling - no facing")
STATUS = (("ok", "#5aa469", "calculated"),
          ("refused", "#d9534f", "refused - see why in the table"),
          ("missing", "#e0a030", "an input is still missing"),
          ("left out", "#9e9e9e", "not placed - left out"))
NO_U = ("#c03ad6", "no U-value in the model - its Space is refused")
NOT_LOADED = ("#c9c9c9", "no load worked out for this Space")
# A ramp from low to high, for U-values and loads per floor area.
RAMP = ("#2b6cb0", "#63b3ed", "#f6e05e", "#ed8936", "#c53030")
MODES = (("type", "Surface type"), ("beyond", "What is beyond"), ("u", "U-value"),
         ("facing", "Which way it faces"), ("cooling", "Space cooling load per m2"),
         ("heating", "Space heating load per m2"), ("status", "Space status"))


def _ramp(t):
    t = 0.0 if t != t else max(0.0, min(1.0, t))
    x = t * (len(RAMP) - 1)
    i = min(len(RAMP) - 2, int(math.floor(x)))
    f = x - i
    a = [int(RAMP[i][k:k + 2], 16) for k in (1, 3, 5)]
    b = [int(RAMP[i + 1][k:k + 2], 16) for k in (1, 3, 5)]
    return "#%02x%02x%02x" % tuple(int(round(a[k] + (b[k] - a[k]) * f)) for k in range(3))


def _scale(value, low, high):
    if value is None:
        return None
    return _ramp((value - low) / ((high - low) or 1.0))


def category(face, kind=None):
    """The category a face or an opening is drawn in."""
    if kind:
        return kind if kind in _CATEGORY else "window"
    side, beyond = face.get("side"), face.get("beyond")
    if side == "wall":
        return {"outside": "outside_wall", "unconditioned": "wall_unconditioned",
                "space": "wall_between_spaces"}.get(beyond, "wall_unknown")
    if side == "top":
        return {"outside": "roof", "unconditioned": "ceiling_unconditioned",
                "space": "ceiling_between_spaces"}.get(beyond, "ceiling_unknown")
    return {"outside": "exposed_floor", "unconditioned": "floor_unconditioned",
            "space": "floor_between_spaces"}.get(beyond, "floor_unknown")


_USED = {"none": "no load - conditioned on both sides",
         "wall": "an outside wall: conduction and sun on it",
         "roof": "a roof: conduction and sun on it",
         "exposed_floor": "a floor open below: heating loss only",
         "partition": "a partition to an unconditioned space, at the project's temperature "
                      "beyond it",
         "floor": "a floor: heating loss only, at the project's ground temperature"}


def _sub(a, b):
    return [a[0] - b[0], a[1] - b[1], a[2] - b[2]]


def _cross(a, b):
    return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]


def _unit(v):
    n = math.sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2])
    return [v[0] / n, v[1] / n, v[2] / n] if n > 1e-12 else None


def rectangle(centre, normal, width, height):
    """An opening's outline: width along the face, height up it, centred, drawn proud."""
    n = _unit([float(x) for x in normal])
    if n is None:
        return None
    across = _unit(_cross([0.0, 0.0, 1.0], n)) or [1.0, 0.0, 0.0]
    up = _unit(_cross(n, across))
    c = [float(centre[k]) + n[k] * PROUD_M for k in range(3)]
    w, h = float(width) / 2.0, float(height) / 2.0
    return [[c[k] + across[k] * sx * w + up[k] * sy * h for k in range(3)]
            for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]


def _legend(rows):
    return [{"colour": c, "label": label} for _key, c, label in rows]


def build(takeoff, result=None):
    """The 3D view's data for one take-off, coloured by the run when there is one."""
    t = TAKEOFF.read(takeoff)
    north = t.site.get("project_to_true_north_deg")
    rows = dict((str(s.get("id")), s) for s in (result or {}).get("spaces") or [])
    names = dict((str(s.get("id")), ("%s %s" % (s.get("number") or "", s.get("name") or "")).strip())
                 for s in t.spaces)

    # Loads per floor area, from the run's own numbers, for the two load modes.
    density = {}
    for sid, r in rows.items():
        shown = r.get("shown") or {}
        area = float(r.get("area_m2") or 0.0)
        if r.get("status") == "ok" and area > 0:
            density[sid] = (shown.get("w_per_m2"), (shown.get("heating_w") or 0.0) / area)
    us = [float(k["u_w_m2k"]) for k in t.types.values() if k.get("u_w_m2k") is not None]
    u_low, u_high = (min(us), max(us)) if us else (0.0, 1.0)
    cool = [d[0] for d in density.values() if d[0] is not None]
    heat = [d[1] for d in density.values() if d[1] is not None]
    c_low, c_high = (min(cool), max(cool)) if cool else (0.0, 1.0)
    h_low, h_high = (min(heat), max(heat)) if heat else (0.0, 1.0)

    faces, outline_missing, unplaced_openings = [], 0, 0
    spaces = {}
    every = []
    for s in t.spaces:
        sid = str(s.get("id"))
        r = rows.get(sid) or {}
        status = r.get("status") or ("left out" if not s.get("placed") else "not calculated")
        status_colour = dict((k, c) for k, c, _l in STATUS).get(status, NOT_LOADED[0])
        cd = density.get(sid)
        cooling_colour = _scale(cd[0], c_low, c_high) if cd else NOT_LOADED[0]
        heating_colour = _scale(cd[1], h_low, h_high) if cd else NOT_LOADED[0]
        floor_points = []
        for i, f in enumerate(s.get("faces") or []):
            loops = [lp for lp in (f.get("loops") or []) if len(lp) >= 3]
            if not loops:
                outline_missing += 1
                continue
            kind = t.types.get(str(f.get("type"))) or {}
            used = TAKEOFF.role(f)
            u = kind.get("u_w_m2k")
            facing = (TAKEOFF.azimuth_deg(f["normal"], north)
                      if f.get("side") == "wall" and f.get("normal") else None)
            cat = category(f)
            note = _USED[used]
            if used != "none" and u is None:
                note = "REFUSED - its type has no U-value in the model"
            face = {
                "id": "%s-%d" % (sid, i), "space": sid, "level": s.get("level"),
                "element": f.get("element"), "type": kind.get("name"), "link": f.get("link"),
                "side": f.get("side"), "beyond": f.get("beyond"),
                "beyond_space": names.get(str(f.get("beyond_space"))),
                "category": cat, "label": _CATEGORY[cat][2], "used_as": note,
                "area_m2": f.get("area_m2"), "u_w_m2k": u, "absorptance": kind.get("absorptance"),
                "facing_deg": facing, "facing": TAKEOFF.compass(facing) if facing is not None else None,
                "loops": loops, "opening": False,
                "colours": {"type": _CATEGORY[cat][1],
                            "beyond": dict((k, c) for k, c, _l in BEYOND).get(f.get("beyond"),
                                                                              BEYOND[3][1]),
                            "u": (NO_U[0] if u is None and used != "none"
                                  else _scale(u, u_low, u_high) if u is not None
                                  else BEYOND[1][1]),
                            "facing": (dict((k, c) for k, c, _l in FACING)[TAKEOFF.compass(facing)]
                                       if facing is not None else NOT_FACING[0]),
                            "cooling": cooling_colour, "heating": heating_colour,
                            "status": status_colour}}
            faces.append(face)
            every.extend(p for lp in loops for p in lp)
            if f.get("side") == "bottom":
                floor_points.extend(p for lp in loops for p in lp)
            for j, o in enumerate(f.get("openings") or []):
                if not o.get("centre") or not o.get("width_m") or not o.get("height_m") \
                        or not f.get("normal"):
                    unplaced_openings += 1
                    continue
                outline = rectangle(o["centre"], f["normal"], o["width_m"], o["height_m"])
                if outline is None:
                    unplaced_openings += 1
                    continue
                okind = t.types.get(str(o.get("type"))) or {}
                ocat = category(f, o.get("kind"))
                ou = okind.get("u_w_m2k")
                glazed = o.get("kind") in TAKEOFF.GLAZED
                refused = ou is None or (glazed and okind.get("shgc") is None)
                faces.append({
                    "id": "%s-%d-%d" % (sid, i, j), "space": sid, "level": s.get("level"),
                    "element": o.get("element"), "type": okind.get("name"),
                    "link": f.get("link"), "side": f.get("side"), "beyond": f.get("beyond"),
                    "beyond_space": names.get(str(f.get("beyond_space"))),
                    "category": ocat, "label": _CATEGORY[ocat][2],
                    "used_as": ("REFUSED - its type has no %s in the model"
                                % ("U-value" if ou is None else "SHGC")) if refused
                    else ("no load - conditioned on both sides" if used == "none"
                          else "glass: conduction and sun through it" if glazed
                          else "a door: conduction through it"),
                    "area_m2": o.get("area_m2"), "u_w_m2k": ou, "shgc": okind.get("shgc"),
                    "facing_deg": facing,
                    "facing": TAKEOFF.compass(facing) if facing is not None else None,
                    "loops": [outline], "opening": True,
                    "colours": {"type": _CATEGORY[ocat][1],
                                "beyond": _CATEGORY[ocat][1],
                                "u": NO_U[0] if ou is None else _scale(ou, u_low, u_high),
                                "facing": _CATEGORY[ocat][1], "cooling": _CATEGORY[ocat][1],
                                "heating": _CATEGORY[ocat][1], "status": _CATEGORY[ocat][1]}})
        pts = floor_points or [p for f in faces if f["space"] == sid for lp in f["loops"]
                               for p in lp]
        label_at = ([sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts),
                     min(p[2] for p in pts) + 0.2] if pts else None)
        shown = r.get("shown") or {}
        spaces[sid] = {"name": names[sid], "level": s.get("level"), "zone": s.get("zone"),
                       "status": status, "why": list(r.get("why") or []),
                       "area_m2": s.get("area_m2"), "label_at": label_at,
                       "total_w": shown.get("total_w"), "w_per_m2": shown.get("w_per_m2"),
                       "heating_w": shown.get("heating_w"), "supply_ls": shown.get("supply_ls"),
                       "peak": shown.get("peak")}

    # Moved near the origin, so the page works with small numbers.
    origin = ([min(p[k] for p in every) for k in range(3)] if every else [0.0, 0.0, 0.0])
    for f in faces:
        f["loops"] = [[[round(p[k] - origin[k], 3) for k in range(3)] for p in lp]
                      for lp in f["loops"]]
    for sp in spaces.values():
        if sp["label_at"]:
            sp["label_at"] = [round(sp["label_at"][k] - origin[k], 3) for k in range(3)]

    levels = {}
    for f in faces:
        z = min(p[2] for lp in f["loops"] for p in lp)
        name = f["level"] or "(no level)"
        levels[name] = min(levels.get(name, z), z)

    def ramp_legend(low, high, unit):
        return {"ramp": list(RAMP), "low": low, "high": high, "unit": unit}

    used_types = set(f["category"] for f in faces)
    legends = {
        "type": [{"colour": c, "label": label} for key, c, label in CATEGORIES
                 if key in used_types],
        "beyond": _legend(BEYOND),
        "u": dict(ramp_legend(u_low, u_high, "W/m2.K"), extra=[{"colour": NO_U[0],
                                                                 "label": NO_U[1]}]),
        "facing": _legend(FACING) + [{"colour": NOT_FACING[0], "label": NOT_FACING[1]}],
        "cooling": dict(ramp_legend(c_low, c_high, "W/m2 of floor, total at its peak"),
                        extra=[{"colour": NOT_LOADED[0], "label": NOT_LOADED[1]}]),
        "heating": dict(ramp_legend(h_low, h_high, "W/m2 of floor"),
                        extra=[{"colour": NOT_LOADED[0], "label": NOT_LOADED[1]}]),
        "status": _legend(STATUS) + [{"colour": NOT_LOADED[0], "label": NOT_LOADED[1]}]}
    notes = []
    if outline_missing:
        notes.append("%d face(s) came with no outline and are not drawn - read the model again "
                     "with the current REPORT_SPACE_ENVELOPE" % outline_missing)
    if unplaced_openings:
        notes.append("%d window(s) or door(s) are counted in the loads but not drawn - Revit did "
                     "not say where they sit" % unplaced_openings)
    return {"format": FORMAT, "document": t.document, "faces": faces, "spaces": spaces,
            "levels": [name for name, _z in sorted(levels.items(), key=lambda kv: kv[1])],
            "modes": [{"key": k, "label": label} for k, label in MODES],
            "legends": legends, "true_north_deg": north or 0.0,
            "origin_m": [round(x, 3) for x in origin], "notes": notes,
            "summary": TAKEOFF.summary(t)}
