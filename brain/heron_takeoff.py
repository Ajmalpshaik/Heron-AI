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
is never defaulted. Every number carries its unit in its key name.

Standard library only.
"""

import json
import math

FORMAT = 1
GLAZED = ("window", "curtain_panel", "skylight")


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


def surfaces(t, space):
    """One Space's faces as the room engine's surface records, and what refused."""
    out = {"walls": [], "roofs": [], "windows": [], "skylights": [], "partitions": [],
           "floors": [], "refused": []}
    north = t.site.get("project_to_true_north_deg")
    for face in space.get("faces") or []:
        beyond = face.get("beyond")
        if beyond == "space":
            continue                                # conditioned on both sides: no load
        kind = t.types.get(str(face.get("type"))) or {}
        name = _name(t, face.get("type"), face.get("element"))
        side = face.get("side")
        facing = azimuth_deg(face["normal"], north) if side == "wall" else None
        net = float(face.get("area_m2") or 0.0)
        for o in face.get("openings") or []:
            area = float(o.get("area_m2") or 0.0)
            net -= area
            okind = t.types.get(str(o.get("type"))) or {}
            oname = _name(t, o.get("type"), o.get("element"))
            glazed = o.get("kind") in GLAZED
            if okind.get("u_w_m2k") is None:
                out["refused"].append(_no_value(oname, "U-value"))
                continue
            if glazed and okind.get("shgc") is None:
                out["refused"].append(_no_value(oname, "SHGC"))
                continue
            if beyond != "outside":
                out["partitions"].append({"name": oname, "area_m2": area,
                                          "u_w_m2k": okind["u_w_m2k"]})
            elif glazed and side == "wall":
                out["windows"].append({"name": oname, "area_m2": area,
                                       "u_w_m2k": okind["u_w_m2k"], "shgc": okind["shgc"],
                                       "facing": facing})
            elif glazed:
                out["skylights"].append({"name": oname, "area_m2": area,
                                         "u_w_m2k": okind["u_w_m2k"], "shgc": okind["shgc"]})
            else:                                   # an opaque door to outside
                out["walls"].append({"name": oname, "area_m2": area,
                                     "u_w_m2k": okind["u_w_m2k"], "facing": facing,
                                     "absorptance": okind.get("absorptance")})
        if net <= 1e-9:
            continue
        if kind.get("u_w_m2k") is None:
            out["refused"].append(_no_value(name, "U-value"))
            continue
        rec = {"name": name, "area_m2": round(net, 6), "u_w_m2k": kind["u_w_m2k"]}
        if beyond != "outside":
            (out["floors"] if side == "bottom" else out["partitions"]).append(rec)
        elif side == "wall":
            rec["facing"] = facing
            rec["absorptance"] = kind.get("absorptance")
            out["walls"].append(rec)
        elif side == "top":
            rec["absorptance"] = kind.get("absorptance")
            out["roofs"].append(rec)
        else:
            out["floors"].append(rec)
    return out


def qa(t):
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
        if not any(f.get("beyond") == "outside" for f in s.get("faces") or []):
            add("INFO", sid, "Space %s has no outside face - only its internal gains load it"
                % label)
        for face in s.get("faces") or []:
            if face.get("beyond") == "unknown":
                add("WARN", sid, "Space %s: nothing was found beyond the face of element %s - "
                    "say what is there" % (label, face.get("element")))
            gross = float(face.get("area_m2") or 0.0)
            holes = sum(float(o.get("area_m2") or 0.0) for o in face.get("openings") or [])
            if holes > gross + 1e-6:
                add("FAIL", sid, "Space %s: openings of %.2f m2 are larger than the %.2f m2 "
                    "face of element %s" % (label, holes, gross, face.get("element")))
        for why in surfaces(t, s)["refused"]:
            add("FAIL", sid, "Space %s: %s" % (label, why))
    for text in t.findings:
        add("WARN", None, text)
    order = {"FAIL": 0, "WARN": 1, "INFO": 2}
    found.sort(key=lambda f: order.get(f["level"], 3))
    return found
