# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The take-off a Revit model gives for a building's loads - docs/44 section 4.

Reads REPORT_SPACE_ENVELOPE's JSON (format 1), checks it (gate 1, docs/44
section 6), and turns each Space's faces into the surface records
heron_hvac's room calculations read. It adds no physics: areas are netted,
azimuths turned to true north, and nothing else.

A type with no U-value, or a glazed type with no SHGC, refuses its Space - it
is never defaulted. A face Revit found nothing beyond is never guessed either:
it is a question for the modeller, asked once per element, and its Space waits
for the answer (`unknowns`, and the `answers` every function here takes).
Every number carries its unit in its key name.

Standard library only.
"""

import json
import math

FORMAT = 1
GLAZED = ("window", "curtain_panel", "skylight")
# What the modeller may say is beyond a face Revit could not see past.
ANSWERS = ("outside", "unconditioned", "conditioned", "ground")


class TakeoffError(ValueError):
    """The take-off cannot be read at all - wrong format, or not JSON."""


class Takeoff(object):
    """One model's take-off, as read - nothing in it is repaired."""

    def __init__(self, d):
        self.document = d.get("document")
        self.units = d.get("units") or {}
        self.site = d.get("site") or {}
        self.types = {str(k): v for k, v in (d.get("types") or {}).items()}
        self.spaces = list(d.get("spaces") or [])
        self.findings = list(d.get("findings") or [])
        self.raw = d


def read(raw):
    """The take-off, whole - or TakeoffError. Never half of one."""
    if isinstance(raw, Takeoff):
        return raw
    if isinstance(raw, str):
        try:
            raw = json.loads(raw)
        except ValueError as why:
            raise TakeoffError("the take-off is not JSON: %s" % why)
    if not isinstance(raw, dict) or raw.get("format") != FORMAT:
        raise TakeoffError("the take-off is not format %d - read the model again with "
                           "REPORT_SPACE_ENVELOPE" % FORMAT)
    for key in ("spaces", "types", "site", "units"):
        if key not in raw:
            raise TakeoffError("the take-off has no %r" % key)
    return Takeoff(raw)


def azimuth_deg(normal, project_to_true_north_deg):
    """Compass bearing the face looks toward: 0 north, 90 east, clockwise, true north.

    The ONE place the True North angle is applied - if a real model shows its
    sign the other way round, it is fixed here and nowhere else.
    """
    x, y = float(normal[0]), float(normal[1])
    project = math.degrees(math.atan2(x, y)) % 360.0
    return round((project + float(project_to_true_north_deg or 0.0)) % 360.0, 6)


def _name(t, type_id, element):
    kind = t.types.get(str(type_id)) or {}
    return "%s (%s)" % (kind.get("name") or "type %s" % type_id, element)


def _no_value(name, what):
    return "%s has no %s in the model - set its type's thermal properties" % (name, what)


UNREAD_LINK = ("a face of this Space is bounded by an element in a linked model that was not "
               "read - calculate again with links included")


def no_element(face):
    """Why a face with no bounding element refuses its Space - said for what it is."""
    if face.get("bounded_by") == "nothing":
        where = {"top": "above it", "bottom": "below it"}.get(face.get("side"), "on one side")
        return ("no element bounds it %s, so what is there is not known - make the ceiling, roof, "
                "floor or wall there room-bounding, or set the Space's limits" % where)
    if face.get("link"):
        return ("a face of this Space is bounded by %s, a linked model that was not read - "
                "calculate again with links included" % face["link"])
    return UNREAD_LINK


def beyond(face, answers=None):
    """What is beyond a face: as Revit found it, or as the modeller answered for its element.

    "outside", "space" (conditioned beyond), "unconditioned", "ground", or
    "unknown" while nobody has said.
    """
    found = face.get("beyond")
    if found != "unknown" or face.get("element") is None:
        return found
    said = (answers or {}).get(str(face.get("element")))
    if said == "conditioned":
        return "space"
    return said if said in ANSWERS else "unknown"


def role(face, answers=None):
    """What a face counts as in the load - the ONE rule surfaces() and the 3D view share.

    "none" (conditioned on both sides), "wall", "roof", "exposed_floor" (open
    to outside below: conduction to the outdoor air, both seasons, no sun),
    "partition" (a wall, ceiling or floor to an unconditioned space, at the
    project's unconditioned temperatures), "floor" (against the ground:
    heating only, at the project's ground temperature) or "unknown" (nothing
    found beyond it, and not yet answered: its Space waits).
    """
    where, side = beyond(face, answers), face.get("side")
    if where == "space":
        return "none"
    if where == "unknown":
        return "unknown"
    if where == "outside":
        return {"wall": "wall", "top": "roof"}.get(side, "exposed_floor")
    if where == "ground":
        return "floor"
    return "partition"


def unknowns(t, answers=None):
    """Every element Revit found nothing beyond, still unanswered - once each, with its Spaces."""
    out = {}
    for s in t.spaces:
        if not s.get("placed") or not s.get("area_m2"):
            continue
        label = ("%s %s" % (s.get("number") or "", s.get("name") or "")).strip()
        for f in s.get("faces") or []:
            if f.get("element") is None or role(f, answers) != "unknown":
                continue
            key = str(f["element"])
            entry = out.setdefault(key, {"name": _name(t, f.get("type"), f["element"]),
                                         "side": f.get("side"), "spaces": []})
            if label not in entry["spaces"]:
                entry["spaces"].append(label)
    return out


def surfaces(t, space, answers=None):
    """One Space's faces as the room engine's surface records, and what refused.

    An outside door carries `door: True` and its type's absorptance, which may
    be missing - the runner then asks for one door absorptance, once.
    """
    out = {"walls": [], "roofs": [], "windows": [], "skylights": [], "partitions": [],
           "floors": [], "exposed_floors": [], "refused": [], "assumed": []}
    north = t.site.get("project_to_true_north_deg")
    for face in space.get("faces") or []:
        used = role(face, answers)
        if used == "none":
            continue                                # conditioned on both sides: no load
        if used == "unknown":
            if face.get("element") is None:
                why = no_element(face)
                if why not in out["refused"]:
                    out["refused"].append(why)
            else:
                out["refused"].append(
                    "nothing was found beyond %s - say what is there: outside, unconditioned, "
                    "conditioned or ground" % _name(t, face.get("type"), face.get("element")))
            continue
        kind = t.types.get(str(face.get("type"))) or {}
        name = _name(t, face.get("type"), face.get("element"))
        side = face.get("side")
        facing = azimuth_deg(face["normal"], north) if side == "wall" else None
        gross = net = float(face.get("area_m2") or 0.0)
        panel_ua = panel_a = 0.0
        for o in face.get("openings") or []:
            area = float(o.get("area_m2") or 0.0)
            net -= area
            okind = t.types.get(str(o.get("type"))) or {}
            if o.get("kind") == "curtain_panel" and okind.get("u_w_m2k") is not None:
                panel_ua += float(okind["u_w_m2k"]) * area
                panel_a += area
            oname = _name(t, o.get("type"), o.get("element"))
            glazed = o.get("kind") in GLAZED
            if okind.get("u_w_m2k") is None:
                out["refused"].append(_no_value(oname, "U-value"))
                continue
            if used in ("partition", "floor"):
                # Glass or a door to an unconditioned space: conduction only,
                # so no SHGC is needed for it.
                rec = {"name": oname, "area_m2": area, "u_w_m2k": okind["u_w_m2k"]}
                (out["floors"] if used == "floor" else out["partitions"]).append(rec)
                continue
            if glazed and okind.get("shgc") is None:
                out["refused"].append(_no_value(oname, "SHGC"))
                continue
            if glazed and side == "wall":
                out["windows"].append({"name": oname, "area_m2": area,
                                       "u_w_m2k": okind["u_w_m2k"], "shgc": okind["shgc"],
                                       "facing": facing})
            elif glazed:
                out["skylights"].append({"name": oname, "area_m2": area,
                                         "u_w_m2k": okind["u_w_m2k"], "shgc": okind["shgc"]})
            else:                                   # an opaque door to outside
                out["walls"].append({"name": oname, "area_m2": area,
                                     "u_w_m2k": okind["u_w_m2k"], "facing": facing,
                                     "absorptance": okind.get("absorptance"), "door": True})
        # What is left of a face once its openings are out - a rounding sliver
        # is nothing; on a curtain wall it is the frames between the panels.
        if net <= max(1e-6, 0.001 * gross):
            continue
        if kind.get("u_w_m2k") is None and face.get("curtain") and panel_a > 0:
            frames = {"name": "%s frames" % name, "area_m2": round(net, 6),
                      "u_w_m2k": round(panel_ua / panel_a, 6)}
            out["assumed"].append(
                "%s: the frames between its panels, %.2f m2, are counted at its panels' "
                "U-value with no sun - the curtain wall type carries no U-value of its own"
                % (name, net))
            if used == "wall":
                out["windows"].append(dict(frames, shgc=0.0, facing=facing))
            elif used in ("partition", "floor"):
                (out["floors"] if used == "floor" else out["partitions"]).append(frames)
            continue
        if kind.get("u_w_m2k") is None:
            out["refused"].append(_no_value(name, "U-value"))
            continue
        rec = {"name": name, "area_m2": round(net, 6), "u_w_m2k": kind["u_w_m2k"]}
        if used == "exposed_floor":
            out["exposed_floors"].append(rec)
        elif used == "floor":
            out["floors"].append(rec)
        elif used == "partition":
            out["partitions"].append(rec)
        elif used == "wall":
            rec["facing"] = facing
            rec["absorptance"] = kind.get("absorptance")
            out["walls"].append(rec)
        else:
            rec["absorptance"] = kind.get("absorptance")
            out["roofs"].append(rec)
    return out


COMPASS = ("N", "E", "S", "W")


def compass(azimuth):
    """The quarter a bearing falls in - N for 315 to 45 degrees, and so on round."""
    return COMPASS[int(((float(azimuth) + 45.0) % 360.0) // 90.0)]


def summary(t, answers=None):
    """The take-off's own totals, for the checks, the panel and the report.

    Per level: Spaces placed and their floor area. Glass to outside by the way
    it faces - window area beside the outside wall it sits in, and the share
    of that wall that is glass - and glass against the floor area placed.
    """
    north = t.site.get("project_to_true_north_deg")
    levels = {}
    by_quarter = dict((q, {"glass_m2": 0.0, "wall_m2": 0.0}) for q in COMPASS)
    floor = glass = 0.0
    for s in t.spaces:
        if not s.get("placed") or not s.get("area_m2"):
            continue
        lv = levels.setdefault(s.get("level") or "(no level)", {"spaces": 0, "area_m2": 0.0})
        lv["spaces"] += 1
        lv["area_m2"] += float(s["area_m2"])
        floor += float(s["area_m2"])
        for f in s.get("faces") or []:
            if beyond(f, answers) != "outside" or f.get("side") != "wall" or not f.get("normal"):
                continue
            q = by_quarter[compass(azimuth_deg(f["normal"], north))]
            q["wall_m2"] += float(f.get("area_m2") or 0.0)
            for o in f.get("openings") or []:
                if o.get("kind") in GLAZED:
                    q["glass_m2"] += float(o.get("area_m2") or 0.0)
                    glass += float(o.get("area_m2") or 0.0)
    for q in by_quarter.values():
        q["glass_pct_of_wall"] = (100.0 * q["glass_m2"] / q["wall_m2"]) if q["wall_m2"] else None
    return {"levels": levels, "glass_by_facing": by_quarter, "glass_m2": glass,
            "floor_m2": floor, "glass_pct_of_floor": (100.0 * glass / floor) if floor else None}


def _groups(spaces):
    """How many groups the placed Spaces of one level form, joined by faces they share."""
    ids = [str(s.get("id")) for s in spaces]
    parent = dict((i, i) for i in ids)

    def top(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    for s in spaces:
        for f in s.get("faces") or []:
            other = str(f.get("beyond_space"))
            if f.get("beyond") == "space" and other in parent:
                parent[top(str(s.get("id")))] = top(other)
    return len(set(top(i) for i in ids))


def qa(t, answers=None):
    """Gate 1 - what is wrong, or worth saying, before any load runs. FAIL first."""
    found = []

    def add(level, space, text):
        found.append({"level": level, "space": space, "text": text})

    if t.site.get("latitude_deg") is None or t.site.get("longitude_deg") is None:
        add("FAIL", None, "the model has no site location - set it in Manage > Location")
    if t.site.get("project_to_true_north_deg") is None:
        add("FAIL", None, "the model's True North could not be read")
    if not t.spaces:
        add("FAIL", None, "the model has no MEP Spaces - place Spaces first; Rooms carry no load")
    for s in t.spaces:
        sid = s.get("id")
        label = ("%s %s" % (s.get("number") or "", s.get("name") or "")).strip()
        if not s.get("placed") or not s.get("area_m2"):
            add("FAIL", sid, "Space %s is not placed or not enclosed - it is left out" % label)
            continue
        faces = s.get("faces") or []
        if not faces:
            add("FAIL", sid, "Space %s: Revit gave no faces for it, so its walls, roof and "
                "windows are not known - it is left out; check it is bounded, then read the "
                "model again" % label)
            continue
        if not any(beyond(f, answers) == "outside" for f in faces):
            add("INFO", sid, "Space %s has no outside face - only its internal gains load it"
                % label)
        if not any(f.get("side") == "top" for f in faces):
            add("FAIL", sid, "Space %s: Revit gave no face above it, so what is over it - a "
                "ceiling, a roof, another Space - is not counted; make its ceiling or roof "
                "room-bounding, or set its upper limit, and read the model again" % label)
        for face in faces:
            if role(face, answers) == "unknown" and face.get("element") is not None:
                add("WARN", sid, "Space %s: nothing was found beyond %s - Heron asks what is "
                    "there" % (label, _name(t, face.get("type"), face.get("element"))))
            if (face.get("side") == "wall" and face.get("beyond") == "unconditioned"
                    and any(o.get("kind") in GLAZED for o in face.get("openings") or [])):
                add("WARN", sid, "Space %s: %s has glass but is not an Exterior wall, so its glass "
                    "gets no sun - if it is on the outside of the building, set its type's "
                    "Function to Exterior" % (label, _name(t, face.get("type"),
                                                          face.get("element"))))
            gross = float(face.get("area_m2") or 0.0)
            holes = sum(float(o.get("area_m2") or 0.0) for o in face.get("openings") or [])
            if holes > gross + 1e-6:
                add("FAIL", sid, "Space %s: openings of %.2f m2 are larger than the %.2f m2 "
                    "face of element %s" % (label, holes, gross, face.get("element")))
        for why in surfaces(t, s, answers)["refused"]:
            if why.startswith("nothing was found beyond"):
                continue                            # asked, not failed - see unknowns()
            add("FAIL", sid, "Space %s: %s" % (label, why))
    by_level = {}
    for s in t.spaces:
        if s.get("placed") and s.get("area_m2"):
            by_level.setdefault(s.get("level") or "(no level)", []).append(s)
    for level, placed in sorted(by_level.items()):
        n = _groups(placed)
        if n > 1:
            add("WARN", None, "the Spaces on %s form %d groups that share no wall - check for a "
                "gap between them, or a corridor, stair or shaft with no Space in it"
                % (level, n))
    for text in t.findings:
        add("WARN", None, text)
    order = {"FAIL": 0, "WARN": 1, "INFO": 2}
    found.sort(key=lambda f: order.get(f["level"], 3))
    return found
