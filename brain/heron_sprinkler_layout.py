# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
A sprinkler layout for the rooms the modeller chose - docs/47 section 5.

Reads REPORT_SPRINKLER_LAYOUT_SPACES' JSON, asks what the model cannot say
(the hazard class, the ceiling height, the branch-line angle of each room; the
sprinkler type, the deflector distance and the spacing limits once), runs
heron_fire's `sprinkler_layout_room` per room, and builds the one placing an
Apply sends: the points of every room that passed on ONE level, for one
level-based sprinkler type. After Apply it reads the rooms back and checks the
heads that are really there.

It adds no physics (one fact, one home: heron_fire.py). A value nobody gave is
ASKED, with the standard's figure offered, and never filled in (D-33): the
ceiling height read from the model is OFFERED, the angle of the longest wall
is OFFERED, and neither is used until the modeller gives it.

Which heads are IN a room is decided here, in plan, against the very outline
the layout uses - never by Revit's volume test, which misses a head above the
room's upper limit (the plan's review, R1).

Standard library only (heron_designbasis is reached for the knowledge folder
only when a job's answers are kept or read back).
"""

import hashlib
import io
import json
import os

import heron_fire as FIRE

FORMAT = 1
#: The per-room answers, with their units - the page holds no list of its own.
ROOM_FIELDS = (("hazard", "class", "the room's occupancy hazard class"),
               ("ceiling_mm", "mm", "the ceiling's height above the room's level"),
               ("angle_deg", "degrees", "which way the branch lines run in plan"))
#: The job's answers.
JOB_FIELDS = (("type", "Family: Type", "the sprinkler type to place - a level-based one"),
              ("deflector_mm", "mm", "how far below the ceiling the head is placed - the "
                                     "family's insertion point, measured from the ceiling"))
#: The limits asked per hazard class - heron_fire's own names.
LIMITS = ("max_spacing_m", "max_area_m2", "max_wall_distance_m", "min_spacing_m",
          "min_wall_distance_m")
REQUIRED = ("max_spacing_m", "max_area_m2", "max_wall_distance_m", "min_wall_distance_m")
LEVEL_BASED = "OneLevelBased"
#: The bridge reads one line of at most 1 MiB; a points line stays well under it.
POINTS_MAX_CHARS = 700000
STATUS = ("ok", "fail", "asked", "skipped", "refused")


class LayoutError(ValueError):
    """An input or a read that cannot be laid out - said, never repaired."""


def _value(v):
    return v.get("value") if isinstance(v, dict) else v


def _mm(m):
    return None if m is None else float(m) * 1000.0


# --- the read -----------------------------------------------------------------

def read(text):
    """The layout read as a dict, refusing anything but format 1."""
    try:
        data = json.loads(text) if isinstance(text, str) else text
    except ValueError as why:
        raise LayoutError("the layout read is not JSON: %s" % why)
    if not isinstance(data, dict) or data.get("format") != FORMAT:
        raise LayoutError("the layout read is not format %d - is the add-in older than this "
                          "brain?" % FORMAT)
    if not isinstance(data.get("spaces"), list):
        raise LayoutError("the layout read has no list of rooms")
    return data


def _poly_mm(raw):
    if not isinstance(raw, list) or len(raw) < 3:
        return None
    return [(float(p[0]) * 1000.0, float(p[1]) * 1000.0) for p in raw]


def _in_room(outline, holes, x, y):
    return FIRE.inside(outline, x, y) and not any(
        FIRE.inside(h, x, y) and not FIRE._on_edge(h, x, y) for h in holes)


def rooms(data):
    """Each room as the layout sees it - outline and holes in mm, the heads IN it (in plan),
    the ceilings over it, and the angle OFFERED (its longest wall), with why it cannot be
    laid out, when it cannot."""
    out = []
    for raw in data.get("spaces") or []:
        outline = _poly_mm(raw.get("outline"))
        holes = [h for h in (_poly_mm(x) for x in raw.get("holes") or []) if h]
        heads = []
        if outline:
            for h in raw.get("sprinklers") or []:
                x, y = _mm(h.get("x")), _mm(h.get("y"))
                if x is not None and y is not None and _in_room(outline, holes, x, y):
                    heads.append({"id": str(h.get("id")), "type": h.get("type"), "x_mm": x,
                                  "y_mm": y, "z_mm": _mm(h.get("z")),
                                  "offset_mm": _mm(h.get("offset_m"))})
        problem = None
        if data.get("heads_read") is not True:
            problem = ("the sprinklers already in the model could not be read - a room is "
                       "never laid out over heads Heron cannot see")
        elif not outline:
            problem = "its outline could not be read"
        elif int(raw.get("separation_edges") or 0) > 0:
            problem = ("it is bounded partly by separation lines - the layout would hold "
                       "them as walls; lay it out when its walls bound it")
        elif heads:
            problem = ("it already has %d sprinkler(s) - a grid laid over heads it cannot "
                       "see is a clash, never a layout" % len(heads))
        label = ("%s %s" % (raw.get("number") or "", raw.get("name") or "")).strip()
        out.append({
            "key": str(raw.get("id")), "number": raw.get("number"), "name": raw.get("name"),
            "label": label or str(raw.get("id")), "kind": raw.get("kind"),
            "level": raw.get("level"),
            "level_mm": _mm(raw.get("level_project_elevation_m")),
            "level_elevation_mm": _mm(raw.get("level_elevation_m")),
            "area_m2": raw.get("area_m2"), "outline_mm": outline, "holes_mm": holes,
            "separation_edges": int(raw.get("separation_edges") or 0),
            "ceilings": [{"id": str(c.get("id")), "type": c.get("type"),
                          "height_mm": _mm(c.get("height_m"))}
                         for c in raw.get("ceilings") or []],
            "heads": heads,
            "angle_offer_deg": (round(FIRE._longest_edge_deg(outline), 1) if outline
                                else None),
            "problem": problem})
    return out


def types(data):
    """The sprinkler types loaded - only a level-based one can be placed by Apply."""
    out = []
    for t in data.get("sprinkler_types") or []:
        usable = t.get("placement") == LEVEL_BASED
        out.append({"name": t.get("name"), "placement": t.get("placement"), "k": t.get("k"),
                    "usable": usable,
                    "why": None if usable else (
                        "a %s family needs a face to sit on, which placing at points does "
                        "not give" % (t.get("placement") or "non level-based"))})
    return out


def heads_by_level(data):
    """{level: [heads IN its rooms]} - what Apply's read-back compares against."""
    out = {}
    for r in rooms(data):
        out.setdefault(r["level"], []).extend(r["heads"])
    return out


# --- what the page asks -------------------------------------------------------

def hazard_classes(standard):
    return list(FIRE.HAZARD_SETS[FIRE.family_said(standard) or "nfpa"])


def fields(standard=None):
    """What the page asks, from here - so it holds no engineering list of its own."""
    classes = hazard_classes(standard)
    return {"room": [list(f) for f in ROOM_FIELDS], "job": [list(f) for f in JOB_FIELDS],
            "limits": [[c, "m2" if c.endswith("m2") else "m", c.replace("_", " "),
                        c in REQUIRED] for c in LIMITS],
            "hazard_classes": classes,
            "limit_offers": dict((c, FIRE.spacing_offers(c, standard)) for c in classes)}


def _standard(inputs, recorded):
    given = (inputs.get("standards") or {}).get("sprinkler_standard")
    if given:
        return _value(given)
    held = (recorded or {}).get("sprinkler_standard")
    return _value(held)


def normalise(inputs):
    """The inputs as a job keeps them: {standards, job, limits: {class: {...}}, rooms: {key:
    {...}}}. A section that is not a map is refused, that section only being lost."""
    if isinstance(inputs, str):
        inputs = json.loads(inputs) if inputs.strip() else {}
    inputs = inputs if isinstance(inputs, dict) else {}
    out = {}
    for part in ("standards", "job", "limits", "rooms"):
        got = inputs.get(part) or {}
        out[part] = got if isinstance(got, dict) else {}
    for name, got in list(out["limits"].items()):
        if not isinstance(got, dict):
            out["limits"].pop(name)
    for name, got in list(out["rooms"].items()):
        if not isinstance(got, dict):
            out["rooms"].pop(name)
    return out


def _class(hazard, standard):
    """A hazard class as the engine names it ("light" -> "light hazard")."""
    family = FIRE.family_said(standard) or "nfpa"
    return FIRE.hazard_key(hazard, family) or str(hazard)


def canonical(inputs, standard):
    """The answers with every hazard class named as the engine names it - a room's class
    and the limits' keys - so the page shows "light" as the "light hazard" it offers."""
    inputs = normalise(inputs)
    for values in inputs["rooms"].values():
        if _value(values.get("hazard")):
            values["hazard"] = _class(_value(values["hazard"]), standard)
    limits = {}
    for name, values in inputs["limits"].items():
        limits.setdefault(_class(name, standard), {}).update(values)
    inputs["limits"] = limits
    return inputs


def _limits_for(inputs, hazard, standard):
    """The limits given for one class, whatever spelling of the class they were given under."""
    for name, values in inputs["limits"].items():
        if _class(name, standard) == hazard:
            return values
    return {}


def _number(raw):
    raw = _value(raw)
    if raw is None or raw == "":
        return None
    try:
        return float(raw)
    except (TypeError, ValueError):
        raise LayoutError("%r is not a number" % (raw,))


# --- the preview --------------------------------------------------------------

def preview(data, inputs, recorded=None):
    """
    Each room laid out, or what it still needs: {"status", "rooms": [...], "asked",
    "levels": {level: {"rooms", "heads", "ok"}}, "type", "standard"}. Nothing is placed.
    A room's status: ok (passed), fail (shown, never placed), asked (an answer missing),
    skipped (it has heads, separation lines, or no outline), refused (the engine said no).
    """
    standard = _standard(normalise(inputs), recorded)
    inputs = canonical(inputs, standard)
    job = inputs["job"]
    asked = []
    type_name = _value(job.get("type"))
    usable = dict((t["name"], t) for t in types(data))
    if not type_name:
        asked.append({"input": "job.type", "why": JOB_FIELDS[0][2]})
    elif type_name not in usable:
        asked.append({"input": "job.type", "why": "%s is not a sprinkler type loaded in this "
                                                  "model" % type_name})
    elif not usable[type_name]["usable"]:
        asked.append({"input": "job.type", "why": usable[type_name]["why"]})
    try:
        deflector = _number(job.get("deflector_mm"))
    except LayoutError as why:
        deflector = None
        asked.append({"input": "job.deflector_mm", "why": str(why)})
    if deflector is None and not any(a["input"] == "job.deflector_mm" for a in asked):
        asked.append({"input": "job.deflector_mm", "why": JOB_FIELDS[1][2]})
    out_rooms = []
    for room in rooms(data):
        out_rooms.append(_room(room, inputs, standard, deflector, recorded))
    levels = {}
    for r in out_rooms:
        lv = levels.setdefault(r["level"], {"rooms": 0, "heads": 0, "ok": 0})
        lv["rooms"] += 1
        if r["status"] == "ok":
            lv["ok"] += 1
            lv["heads"] += r["count"]
    for r in out_rooms:
        for a in r["asked"]:
            if a["input"].startswith("limits.") and a not in asked:
                asked.append(a)
    ready = not [a for a in asked if a["input"].startswith("job.")]
    any_ok = any(r["status"] == "ok" for r in out_rooms)
    status = "ok" if ready and any_ok else ("asked" if [a for a in asked] or
                                            any(r["status"] == "asked" for r in out_rooms)
                                            else "nothing to place")
    return {"status": status, "rooms": out_rooms, "asked": asked, "levels": levels,
            "inputs": inputs, "type": type_name, "deflector_mm": deflector, "standard": standard,
            "findings": list(data.get("findings") or [])}


def _room(room, inputs, standard, deflector, recorded):
    given = inputs["rooms"].get(room["key"]) or {}
    out = {"key": room["key"], "label": room["label"], "level": room["level"],
           "status": None, "why": None, "asked": [], "count": 0, "points": [],
           "measured": None, "checks": [], "outline_mm": room["outline_mm"],
           "answer": None, "heads": room["heads"], "angle_offer_deg": room["angle_offer_deg"],
           "ceilings": room["ceilings"]}
    if room["problem"]:
        out.update(status="skipped", why=room["problem"])
        return out
    hazard = _value(given.get("hazard"))
    try:
        ceiling = _number(given.get("ceiling_mm"))
        angle = _number(given.get("angle_deg"))
    except LayoutError as why:
        out.update(status="refused", why=str(why))
        return out
    for key, value, why in (("hazard", hazard, ROOM_FIELDS[0][2]),
                            ("ceiling_mm", ceiling, ROOM_FIELDS[1][2]),
                            ("angle_deg", angle, ROOM_FIELDS[2][2])):
        if value is None or value == "":
            out["asked"].append({"input": "rooms.%s.%s" % (room["key"], key), "why": why})
    limits = {}
    if hazard:
        hazard = _class(hazard, standard)
        held = _limits_for(inputs, hazard, standard)
        for name in LIMITS:
            v = _value(held.get(name))
            if v is not None and v != "":
                limits[name] = v
            elif name in REQUIRED:
                out["asked"].append({"input": "limits.%s.%s" % (hazard, name),
                                     "why": "the %s for %s" % (name.replace("_", " "), hazard),
                                     "offer": FIRE.spacing_offers(hazard, standard).get(name)})
    if out["asked"]:
        out["status"] = "asked"
        return out
    z = None
    if deflector is not None and room["level_mm"] is not None:
        z = room["level_mm"] + ceiling - deflector
    engine_in = dict(limits, outline_mm=[list(p) for p in room["outline_mm"]],
                     holes_mm=[[list(p) for p in h] for h in room["holes_mm"]],
                     branch_angle_deg=angle, hazard=hazard)
    if standard:
        engine_in["sprinkler_standard"] = standard
    if z is not None:
        engine_in["mounting_z_mm"] = z
    answer = FIRE.run("sprinkler_layout_room", engine_in, recorded=recorded)
    out["answer"] = answer
    if answer["status"] != "ok" or not answer.get("data"):
        out.update(status="refused" if answer.get("refused") else "asked",
                   why="; ".join(answer.get("refused") or []) or None,
                   asked=[{"input": m["input"], "why": m.get("why"),
                           "offer": m.get("reference")} for m in answer.get("missing") or []])
        return out
    d = answer["data"]
    # WHAT THE MODELLER CHECKS BEFORE APPLY (docs/47 s7): every head's S, L, area and walls,
    # and the engine's own checks - the farthest point among them - made here, shown there.
    measured = [t for t in answer.get("tables") or []
                if str(t.get("title", "")).startswith("Each sprinkler")]
    out["measured"] = ({"columns": list(measured[0]["columns"]),
                        "rows": [list(r) for r in measured[0]["rows"]]} if measured else None)
    out["checks"] = [list(c) for c in answer.get("checks") or []]
    out["count"] = len(d["points"])
    out["points"] = [{"id": p["id"], "x_mm": p["x_mm"], "y_mm": p["y_mm"], "z_mm": z}
                     for p in d["points"]]
    out["z_mm"] = z
    out["z_from"] = (None if z is None else
                     "level %s at %s mm + ceiling %s mm - deflector %s mm" % (
                         room["level"], FIRE._f(room["level_mm"], 0), FIRE._f(ceiling, 0),
                         FIRE._f(deflector, 0)))
    if not d["passed"]:
        out.update(status="fail", why="no regular grid passes in this room - it is shown, "
                                      "never placed")
    elif z is None:
        out.update(status="asked", why="the heads' height cannot be worked out yet")
    else:
        out["status"] = "ok"
    return out


# --- the placing --------------------------------------------------------------

def plan(result, level):
    """
    The one placing an Apply sends for `level`: {"symbol", "level", "points", "rooms",
    "count"} - every room on that level that passed, for the job's type. Refused when the
    type cannot be placed, nothing on the level passed, or the points line is too long.
    """
    if not result or result.get("status") not in ("ok",):
        raise LayoutError("nothing previewed can be placed yet - answer what is asked and "
                          "Preview again")
    chosen = [r for r in result["rooms"] if r["level"] == level and r["status"] == "ok"]
    if not chosen:
        raise LayoutError("no room on level %s passed its preview" % level)
    # OVERLAPPING ROOMS ARE REFUSED: a Room and a Space over the same floor both pass,
    # and their grids together put two heads where one belongs (the Codex review of #418).
    for i, a in enumerate(chosen):
        for b in chosen[i + 1:]:
            if any(FIRE.inside(b["outline_mm"], p["x_mm"], p["y_mm"]) for p in a["points"]) or \
                    any(FIRE.inside(a["outline_mm"], p["x_mm"], p["y_mm"]) for p in b["points"]):
                raise LayoutError("%s and %s overlap - a Room and a Space over the same floor? "
                                  "Lay out only one of them" % (a["label"], b["label"]))
    pts = [p for r in chosen for p in r["points"]]
    line = "; ".join("%.1f,%.1f,%.1f" % (p["x_mm"], p["y_mm"], p["z_mm"]) for p in pts)
    if len(line) > POINTS_MAX_CHARS:
        raise LayoutError("%d heads on one level is more than one placing can carry - "
                          "preview fewer rooms at a time" % len(pts))
    return {"symbol": result["type"], "level": level, "points": line,
            "rooms": [r["key"] for r in chosen], "count": len(pts),
            "sent": [{"room": r["key"], "x_mm": p["x_mm"], "y_mm": p["y_mm"],
                      "z_mm": p["z_mm"]} for r in chosen for p in r["points"]]}


def fingerprint(room):
    """What a preview was made from: outline, holes, level and its elevation, the ceilings
    and the heads already in the room. Apply refuses a room whose fingerprint moved (R2)."""
    def r(v):
        return None if v is None else round(v, 0)
    body = {"outline": [[r(x), r(y)] for x, y in room["outline_mm"] or []],
            "holes": [[[r(x), r(y)] for x, y in h] for h in room["holes_mm"]],
            "level": room["level"], "level_mm": r(room["level_mm"]),
            "ceilings": sorted((c["id"], r(c["height_mm"])) for c in room["ceilings"]),
            "heads": sorted((h["id"], r(h["x_mm"]), r(h["y_mm"])) for h in room["heads"])}
    return hashlib.sha256(json.dumps(body, sort_keys=True).encode("utf-8")).hexdigest()[:16]


def changed(before, after, keys):
    """[why] for each room of `keys` that is missing from `after` or moved since `before`."""
    was = dict((r["key"], r) for r in rooms(before))
    now = dict((r["key"], r) for r in rooms(after))
    out = []
    if after.get("heads_read") is not True:
        return ["the sprinklers already in the model could not be read again, so whether a "
                "room has heads now is unknown"]
    for k in keys:
        if k not in now:
            out.append("%s is no longer in the model" % (was.get(k, {}).get("label") or k))
        elif k not in was or fingerprint(was[k]) != fingerprint(now[k]):
            out.append("%s changed since the preview - its outline, level, ceiling or "
                       "heads" % now[k]["label"])
    return out


def read_back(before, after, sent, result, inputs):
    """
    After Apply: per room, the heads now in it that were not there before, set beside the
    points sent - each one's difference in plan and in height to the millimetre, SAID and
    never judged against a tolerance Heron made up (R14) - then `sprinkler_spacing` on every
    head now in the room. {"rooms": [...], "placed", "sent", "text"}.
    """
    inputs = normalise(inputs)
    was = dict((r["key"], r) for r in rooms(before))
    now = dict((r["key"], r) for r in rooms(after))
    previewed = dict((r["key"], r) for r in result["rooms"])
    out = []
    total = 0
    for key in sent["rooms"]:
        room = now.get(key)
        mine = [s for s in sent["sent"] if s["room"] == key]
        if room is None:
            out.append({"key": key, "label": key, "placed": 0, "sent": len(mine),
                        "text": "the room could not be read back"})
            continue
        old = set(h["id"] for h in (was.get(key) or {}).get("heads", []))
        new = [h for h in room["heads"] if h["id"] not in old]
        total += len(new)
        worst_plan = worst_z = None
        unmatched = list(mine)
        for h in new:
            if not unmatched:
                break
            best = min(unmatched, key=lambda s: (s["x_mm"] - h["x_mm"]) ** 2
                       + (s["y_mm"] - h["y_mm"]) ** 2)
            unmatched.remove(best)
            dp = ((best["x_mm"] - h["x_mm"]) ** 2 + (best["y_mm"] - h["y_mm"]) ** 2) ** 0.5
            worst_plan = dp if worst_plan is None else max(worst_plan, dp)
            if h["z_mm"] is not None and best["z_mm"] is not None:
                dz = abs(h["z_mm"] - best["z_mm"])
                worst_z = dz if worst_z is None else max(worst_z, dz)
        spacing = _spacing(room, previewed.get(key), inputs, result.get("standard"))
        line = "%d of %d head(s) found in %s" % (len(new), len(mine), room["label"])
        if worst_plan is not None:
            line += "; the farthest from where it was sent is %s mm away in plan" % (
                FIRE._f(worst_plan, 0))
        if worst_z is not None:
            line += ", %s mm in height" % FIRE._f(worst_z, 0)
            if worst_z > 1.0:
                line += (" - CHECK: the height is not the one asked (the level's elevation "
                         "may be counted twice, or the family's insertion point is not its "
                         "deflector) - Revit's undo takes the placing back")
        if spacing is not None:
            line += "; spacing of every head now in the room: %s" % spacing["verdict"]
        out.append({"key": key, "label": room["label"], "placed": len(new), "sent": len(mine),
                    "worst_plan_mm": worst_plan, "worst_z_mm": worst_z, "spacing": spacing,
                    "text": line})
    sent_n = len(sent["sent"])
    text = "%d of %d head(s) placed are in their rooms. %s" % (
        total, sent_n, " ".join(r["text"] + "." for r in out))
    return {"rooms": out, "placed": total, "sent": sent_n, "text": text}


def _spacing(room, previewed, inputs, standard):
    """`sprinkler_spacing` on the heads really in the room, at the room's angle (R17)."""
    given = inputs["rooms"].get(room["key"]) or {}
    hazard = _value(given.get("hazard"))
    angle = _value(given.get("angle_deg"))
    if not room["heads"] or hazard is None or angle is None:
        return None
    origin = room["outline_mm"][0]
    outline = FIRE._turn(room["outline_mm"], origin, -float(angle))
    heads = FIRE._turn([(h["x_mm"], h["y_mm"]) for h in room["heads"]], origin, -float(angle))
    hazard = _class(hazard, standard)
    limits = dict((k, _value(v)) for k, v in _limits_for(inputs, hazard, standard).items()
                  if k in LIMITS and _value(v) not in (None, ""))
    engine_in = dict(limits, outline_mm=[list(p) for p in outline], branch_axis="x",
                     sprinklers=[{"id": room["heads"][k]["id"], "x": x, "y": y}
                                 for k, (x, y) in enumerate(heads)], hazard=hazard)
    if standard:
        engine_in["sprinkler_standard"] = standard
    answer = FIRE.run("sprinkler_spacing", engine_in)
    fails = [t for lv, t in answer.get("checks") or [] if lv == "FAIL"]
    if answer["status"] != "ok":
        verdict = "not checked - %s" % ("; ".join(answer.get("refused") or [])
                                        or "an answer is missing")
    else:
        verdict = "FAIL - %s" % fails[0] if fails else "OK"
    return {"verdict": verdict, "answer": answer}


def view(data, result):
    """What the page draws for each room - made here, drawn there: the outline, the holes,
    the heads already in it, and each new head with whether it passed. Plan, mm."""
    previewed = dict((r["key"], r) for r in (result or {}).get("rooms") or [])
    out = []
    for room in rooms(data):
        p = previewed.get(room["key"]) or {}
        heads = (((p.get("answer") or {}).get("data") or {}).get("heads")) or {}
        out.append({"key": room["key"], "outline": room["outline_mm"],
                    "holes": room["holes_mm"],
                    "existing": [[h["x_mm"], h["y_mm"]] for h in room["heads"]],
                    "points": [[q["x_mm"], q["y_mm"], not (heads.get(q["id"]) or {}).get("fails")]
                               for q in p.get("points") or []]})
    return out


def summary_text(result):
    """One paragraph for the chat - totals only; the rooms are on the Companion."""
    rooms_ = result.get("rooms") or []
    by = {}
    for r in rooms_:
        by[r["status"]] = by.get(r["status"], 0) + 1
    parts = ["%d room(s) read" % len(rooms_)]
    for s in STATUS:
        if by.get(s):
            parts.append("%d %s" % (by[s], s))
    heads = sum(r["count"] for r in rooms_ if r["status"] == "ok")
    text = ", ".join(parts) + "."
    if heads:
        text += " %d head(s) ready to place, level by level, from the Companion's Apply." % heads
    if result.get("asked"):
        text += " Still to answer: %s." % ", ".join(sorted(set(
            a["input"] for a in result["asked"])))[:600]
    return text


# --- kept with the project ----------------------------------------------------

def _folder(project_key):
    import heron_designbasis
    scope = heron_designbasis._scope()
    base = scope.knowledge_dir()
    if base is None:
        raise ValueError("No %APPDATA% and no HERON_KNOWLEDGE, so there is nowhere to keep "
                         "the layout's answers. Set HERON_KNOWLEDGE to a folder.")
    if not project_key:
        raise ValueError("No project key, so there is no folder to keep the answers in - "
                         "ask which project this is (D-33).")
    return os.path.join(base, "projects", scope._safe_key(project_key) + ".sprinkler-layout")


def save_inputs(project_key, inputs):
    """Keep the job's answers, so a room or a limit is not asked twice."""
    folder = _folder(project_key)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    path = os.path.join(folder, "answers.json")
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(json.dumps(normalise(inputs), indent=1))
    return path


def load_inputs(project_key):
    """The kept answers, or an empty job."""
    try:
        path = os.path.join(_folder(project_key), "answers.json")
    except ValueError:
        return normalise({})
    if not os.path.isfile(path):
        return normalise({})
    try:
        with io.open(path, encoding="utf-8") as fh:
            return normalise(json.loads(fh.read()))
    except (OSError, ValueError):
        return normalise({})


def carried(kept, inputs):
    """This call's answers over the kept ones, section by section and key by key. A value
    given as nothing CLEARS the kept one, so a field blanked on the page is asked again."""
    kept, now = normalise(kept), normalise(inputs)
    out = {}
    for part in ("standards", "job"):
        merged = dict(kept[part])
        for k, v in now[part].items():
            if _value(v) is None or _value(v) == "":
                merged.pop(k, None)
            else:
                merged[k] = v
        out[part] = merged
    for part in ("limits", "rooms"):
        merged = dict((k, dict(v)) for k, v in kept[part].items())
        for name, values in now[part].items():
            row = merged.setdefault(name, {})
            for k, v in values.items():
                if _value(v) is None or _value(v) == "":
                    row.pop(k, None)
                else:
                    row[k] = v
        out[part] = merged
    return out
