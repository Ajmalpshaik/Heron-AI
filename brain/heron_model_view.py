# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-QA-BIM-011
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
What the Model Dashboard's 3D view draws - docs/40 section 21.13.

REPORT_MODEL_GEOMETRY's `geometryJson` (one read, which changes nothing)
becomes the data Heron's own WebGL renderer draws (static/model3d.js). The
page only projects and paints; what each colour MEANS is decided here
(mcp/companion README rule 4):

    CATEGORIES   one colour per category, the same colour for the same
                 category every time - a fixed colour for the categories a
                 modeller knows by colour (ducts blue, pipes green,
                 sprinklers red), an even hue step for the rest.
    ELEMENTS     passed through as the fragment wrote them, CHECKED: every
                 triangle's corners must be points the element has, so a
                 damaged read is refused whole rather than drawn wrong.
    WHAT WAS NOT DRAWN IS SAID - elements left out by the triangle budget,
                 elements with nothing drawable, and anything not read.
    MATERIALS    (the read's version 3, docs/40 s21.18) each face's material
                 with the shading colour Revit's Shaded view uses, checked: an
                 element's material index must name one the read listed, or
                 be -1. A read with none - version 2 - still draws, by
                 category only.
    VOLUMES      the categories the read drew as volumes - rooms and spaces -
                 which the page draws see-through.

Standard library only.
"""

from __future__ import annotations

import colorsys
import json

FORMAT = 1
READS = 1
#: Elements the page will draw - what the fragment's own limit allows.
ELEMENTS_MAX = 200000

#: The categories a modeller already reads by colour. Anything else gets an
#: even step around the hue wheel from its place in the list, so two
#: categories never share a colour by accident.
KNOWN = {
    "Walls": "#b9b4ab", "Floors": "#cfcac0", "Roofs": "#a38f7a", "Ceilings": "#e3dfd6",
    "Doors": "#8a5a44", "Windows": "#7fb6d9", "Columns": "#9a948a",
    "Structural Columns": "#7d7a74", "Structural Framing": "#8c8780",
    "Structural Foundations": "#6f6b65", "Stairs": "#a7a196", "Railings": "#8f8a80",
    "Ducts": "#4f8fd1", "Duct Fittings": "#3f7fc1", "Flex Ducts": "#6fa6dd",
    "Air Terminals": "#2c6cb0", "Duct Accessories": "#5d93cc",
    "Pipes": "#3ba86b", "Pipe Fittings": "#2f985d", "Flex Pipes": "#5cbb85",
    "Pipe Accessories": "#46a874", "Plumbing Fixtures": "#2c7a7b",
    "Sprinklers": "#d9534f", "Fire Alarm Devices": "#c0392b",
    "Mechanical Equipment": "#e0a030", "Electrical Equipment": "#8e6bbf",
    "Electrical Fixtures": "#a084cf", "Lighting Fixtures": "#f0c040",
    "Cable Trays": "#9b7fc8", "Cable Tray Fittings": "#8d70bb", "Conduits": "#b39ddb",
    "Generic Models": "#9e9e9e", "Furniture": "#c8a27a", "Casework": "#b08b65",
    "Specialty Equipment": "#d08a50", "Topography": "#9fb38a", "Toposolid": "#9fb38a",
    # Drawn see-through as volumes (s21.18), in the brand's Accent (D-149,
    # docs/brand/README.md) on the owner's word, "use brand colours for rooms
    # and spaces": a see-through Accent is in the palette, a new hex is not.
    # Rooms and Spaces share it; the legend keeps them apart, each hideable.
    "Rooms": "#fd641a", "Spaces": "#fd641a",
}
#: The highlight the page paints a picked or looked-at element in.
HIGHLIGHT = "#ff7a00"
#: What everything else fades to while something is highlighted.
DIMMED = "#dfe3e8"
#: How see-through a room's volume is drawn, 0 clear to 1 solid.
VOLUME_ALPHA = 0.18
#: A face with no material known, when the view is coloured by material.
NO_MATERIAL = "#c9c9c9"
#: The most materials the legend lists; the rest are counted.
LEGEND_MATERIALS = 40


def _hue(i, n):
    r, g, b = colorsys.hls_to_rgb((0.08 + i / float(max(n, 1))) % 1.0, 0.55, 0.45)
    return "#%02x%02x%02x" % (int(r * 255), int(g * 255), int(b * 255))


def _int(value):
    try:
        number = int(value)
    except (TypeError, ValueError):
        return 0
    return number if number > 0 else 0


def _names(values, most=500):
    out = []
    for value in values if isinstance(values, list) else []:
        if value is None:
            continue
        text = str(value).strip()
        if text:
            out.append(text)
        if len(out) >= most:
            break
    return out


def read(text):
    """
    geometryJson as the fragment returns it, checked. Raises ValueError with a
    sentence a modeller can read when it is not a geometry read this Heron
    understands, or when a triangle names a point its element does not have.
    """
    if not isinstance(text, str) or not text.strip():
        raise ValueError("Revit sent no geometry - the read returned nothing.")
    try:
        data = json.loads(text)
    except ValueError:
        raise ValueError("what Revit sent is not geometry Heron can read.")
    if not isinstance(data, dict):
        raise ValueError("what Revit sent is not geometry Heron can read.")
    if data.get("format") != READS:
        raise ValueError("the geometry is in format %s; this Heron reads format %d - the add-in "
                         "and Heron are from different releases."
                         % (data.get("format"), READS))
    categories = _names(data.get("categories"), most=10000)
    materials = _materials(data.get("materials"))
    volumes = sorted({v for v in data.get("volumes") or [] if isinstance(v, int)
                      and not isinstance(v, bool) and 0 <= v < len(categories)})
    elements = []
    raw = data.get("elements")
    for e in raw if isinstance(raw, list) else []:
        if not isinstance(e, dict):
            continue
        ident = str(e.get("id") or "")
        v, t = e.get("v"), e.get("t")
        c = e.get("c")
        if not ident.isdigit() or not isinstance(v, list) or not isinstance(t, list) \
                or not isinstance(c, int) or not 0 <= c < len(categories):
            raise ValueError("an element in the geometry is damaged (id %r) - nothing was drawn."
                             % ident)
        if len(v) % 3 or len(t) % 3:
            raise ValueError("element %s's geometry is damaged - its points or triangles do not "
                             "come in threes. Nothing was drawn." % ident)
        points = len(v) // 3
        try:
            coords = [float(x) for x in v]
            corners = [int(i) for i in t]
        except (TypeError, ValueError):
            raise ValueError("element %s's geometry holds something that is not a number. "
                             "Nothing was drawn." % ident)
        if any(i < 0 or i >= points for i in corners):
            raise ValueError("element %s has a triangle naming a point it does not have. "
                             "Nothing was drawn." % ident)
        if any(x != x or x in (float("inf"), float("-inf")) for x in coords):
            raise ValueError("element %s's geometry holds a point that is not a number. "
                             "Nothing was drawn." % ident)
        # Its material: one index, or one a triangle; -1 is none known. Left
        # out (a version-2 read) it is -1.
        m = e.get("m", -1)
        ok_index = lambda i: isinstance(i, int) and not isinstance(i, bool) \
            and -1 <= i < len(materials)
        if isinstance(m, list):
            if len(m) != len(corners) // 3 or not all(ok_index(i) for i in m):
                raise ValueError("element %s's materials do not match its triangles. "
                                 "Nothing was drawn." % ident)
            if len(set(m)) == 1:
                m = m[0]
        elif not ok_index(m):
            raise ValueError("element %s names a material the read did not list. "
                             "Nothing was drawn." % ident)
        if corners:
            elements.append({"id": ident, "c": c, "v": coords, "t": corners, "m": m})
        if len(elements) > ELEMENTS_MAX:
            raise ValueError("the geometry holds more than %d elements - more than the page can "
                             "draw. Read the active 3D view instead of the whole model."
                             % ELEMENTS_MAX)
    offset = data.get("offset")
    if not (isinstance(offset, list) and len(offset) == 3):
        offset = [0.0, 0.0, 0.0]
    return {"scope": str(data.get("scope") or ""), "offset": [float(x) for x in offset],
            "categories": categories, "elements": elements,
            "materials": materials, "volumes": volumes,
            "triangles": sum(len(e["t"]) // 3 for e in elements),
            "elementsRead": _int(data.get("elementsRead")),
            "noGeometry": _int(data.get("noGeometry")), "leftOut": _int(data.get("leftOut")),
            "notRead": _names(data.get("notRead")), "capped": _names(data.get("capped")),
            # WHICH LIMIT STOPPED THE READ - "triangles", "time", or None when
            # it read everything (REPORT_MODEL_GEOMETRY v2). Anything else is
            # not believed.
            "stopped": data.get("stopped") if data.get("stopped") in ("triangles", "time") else None}


def _colour(text):
    text = str(text or "")
    if len(text) == 7 and text[0] == "#":
        try:
            int(text[1:], 16)
            return text.lower()
        except ValueError:
            pass
    return None


def _materials(values):
    """The read's materials, each {name, colour, transparency}; a damaged
    entry is kept in its place (indexes must not move) with no colour."""
    out = []
    for m in values if isinstance(values, list) else []:
        m = m if isinstance(m, dict) else {}
        try:
            seen = int(m.get("transparency") or 0)
        except (TypeError, ValueError):
            seen = 0
        out.append({"name": str(m.get("name") or "")[:200], "colour": _colour(m.get("colour")),
                    "transparency": max(0, min(100, seen))})
    return out


def colours(categories):
    """One colour per category: the known ones fixed, the rest an even hue step."""
    unknown = [c for c in categories if c not in KNOWN]
    return [KNOWN.get(c) or _hue(unknown.index(c), len(unknown)) for c in categories]


def build(data):
    """The 3D view's whole content from read()'s result."""
    palette = colours(data["categories"])
    counts = [0] * len(data["categories"])
    for e in data["elements"]:
        counts[e["c"]] += 1
    legend = [{"name": n, "colour": palette[i], "count": counts[i]}
              for i, n in enumerate(data["categories"]) if counts[i]]
    legend.sort(key=lambda r: (-r["count"], r["name"]))
    volumes = set(data.get("volumes") or [])
    for row in legend:
        row["volume"] = data["categories"].index(row["name"]) in volumes
    # BY MATERIAL: how many triangles each material covers, for its legend.
    materials = data.get("materials") or []
    covered = [0] * len(materials)
    unknown = 0
    for e in data["elements"]:
        if e["c"] in volumes:
            continue
        m = e.get("m", -1)
        for i in (m if isinstance(m, list) else [m] * (len(e["t"]) // 3)):
            if 0 <= i < len(materials) and materials[i]["colour"]:
                covered[i] += 1
            else:
                unknown += 1
    by_material = [{"name": materials[i]["name"] or "(no name)", "colour": materials[i]["colour"],
                    "transparency": materials[i]["transparency"], "triangles": covered[i]}
                   for i in range(len(materials)) if covered[i]]
    by_material.sort(key=lambda r: (-r["triangles"], r["name"]))
    rooms = sum(1 for e in data["elements"] if e["c"] in volumes)
    return {"format": FORMAT, "scope": data["scope"], "offset": data["offset"],
            "categories": [{"name": n, "colour": palette[i]}
                           for i, n in enumerate(data["categories"])],
            "legend": legend, "elements": data["elements"],
            # Every material in its place (an element's m indexes this list),
            # its colour checked; NO_MATERIAL stands in for one with none.
            "materials": [{"name": m["name"], "colour": m["colour"] or NO_MATERIAL,
                           "alpha": round(1.0 - m["transparency"] / 100.0, 3)}
                          for m in materials],
            "materialLegend": by_material[:LEGEND_MATERIALS],
            "materialsMore": max(0, len(by_material) - LEGEND_MATERIALS),
            "noMaterial": NO_MATERIAL, "unknownTriangles": unknown,
            "hasMaterials": bool(by_material),
            "volumes": sorted(volumes), "volumeAlpha": VOLUME_ALPHA, "rooms": rooms,
            "triangles": data["triangles"], "drawn": len(data["elements"]),
            "noGeometry": data["noGeometry"], "leftOut": data["leftOut"],
            "stopped": data.get("stopped"),
            "notRead": list(data["notRead"]), "capped": list(data["capped"]),
            "highlight": HIGHLIGHT, "dimmed": DIMMED, "said": summary(data)}


def summary(data):
    """One line for the chat and the page."""
    said = "3D view - %s: %d element(s), %d triangle(s)." % (
        data["scope"] or "the model", len(data["elements"]), data["triangles"])
    if data["leftOut"]:
        why = {"time": "the read stopped itself before Revit's time limit, largest elements "
                       "first",
               "triangles": "the triangle limit was reached, largest elements first"}.get(
            data.get("stopped"), "a limit was reached")
        said += (" %d element(s) were left out - %s; read the active 3D view with a section "
                 "box, or fewer categories, to see them." % (data["leftOut"], why))
    volumes = set(data.get("volumes") or [])
    rooms = sum(1 for e in data["elements"] if e["c"] in volumes)
    if rooms:
        said += " %d room(s) or space(s) drawn as see-through volumes." % rooms
    if data["noGeometry"]:
        said += " %d had nothing to draw." % data["noGeometry"]
    if data["notRead"]:
        said += " Not read: %s." % "; ".join(data["notRead"][:3])
    return said + " Nothing was changed."


def view(text):
    """read() then build(), as the MCP server calls it. Returns {"view", "said"};
    "view" is None, and "said" says why, when the geometry could not be used."""
    try:
        data = read(text)
    except ValueError as why:
        return {"view": None, "said": "No 3D view was made: %s" % why}
    built = build(data)
    return {"view": built, "said": built["said"]}
