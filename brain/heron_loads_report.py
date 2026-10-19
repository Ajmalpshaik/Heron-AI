# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The load calculation sheet - docs/44 section 8, and its air systems, section 13.

One self-contained HTML page (inline CSS, no script, no outside address) with a
page of its own for each air system, a CSV of the results, a CSV of the surface
take-off every load was built from, a CSV of each air system's figures and one
of its outdoor air Space by Space, and - where Microsoft Edge or Google Chrome
is on the PC - a PDF printed from the page in the browser's headless mode. No new package: with no browser the
HTML page is the report, and the answer says so.

THE SHEET IS PRINTED FROM THE NUMBERS THE RUN USED, never from a separate
list: every value comes out of the run's own result and inputs, each with the
label saying where it came from.

Standard library only.
"""

import csv
import html as _html
import io
import os
import pathlib
import subprocess

import heron_building_loads as LOADS
import heron_hvac as HVAC
import heron_loads_systems as SYSTEMS
import heron_takeoff as TAKEOFF

DISCLAIMER = ("Prepared with Heron - a design aid from published methods. A peak estimate, "
              "not an hourly simulation like HAP. The engineer of record approves every "
              "value.")
MONTHS = ("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec")

CSS = """
@page { size: A4; margin: 14mm 12mm; }
body { font-family: "Segoe UI", Arial, sans-serif; font-size: 9.5pt; line-height: 1.4; color: #1a2230;
       max-width: 1100px; margin: 0 auto; padding: 18px 22px; }
.sheet-head { border-bottom: 2px solid #1e4fb8; padding-bottom: 8pt; margin-bottom: 10pt; }
.kind { font-size: 8pt; letter-spacing: .08em; text-transform: uppercase; color: #5a6778; }
h1 { font-size: 18pt; margin: 2pt 0 1pt; color: #0f1a2a; }
.sub { margin: 0 0 6pt; color: #4f5d6e; }
.meta { display: flex; flex-wrap: wrap; gap: 3pt 20pt; margin: 4pt 0 6pt; }
.meta span { color: #6b7888; margin-right: 4pt; }
h2 { font-size: 11.5pt; margin: 16pt 0 6pt; padding-bottom: 3pt; color: #0f1a2a;
     border-bottom: 1px solid #c9d2dc; page-break-after: avoid; break-after: avoid; }
table { border-collapse: collapse; width: 100%; margin: 4pt 0 8pt; }
th, td { border: 1px solid #d5dce4; padding: 3pt 5pt; text-align: left; vertical-align: top; }
th { background: #eef2f6; color: #2b3a4d; font-weight: 600; }
tr:nth-child(even) td { background: #f8fafc; }
td.n { text-align: right; font-variant-numeric: tabular-nums; }
table.summary td:first-child { width: 42%; font-weight: 600; }
tr { page-break-inside: avoid; } .fail { color: #b3261e; } .warn { color: #9a5b00; }
.note { font-size: 8.5pt; color: #3c4a5c; }
.lead { margin: 2pt 0; font-weight: 600; }
.disclaimer { border: 1px solid #c9d2dc; border-left: 4px solid #9a5b00; background: #fdf8ee;
              padding: 6pt 8pt; margin: 6pt 0; }
.disclaimer.fail { border-left-color: #b3261e; background: #fbe7e5; }
h3 { font-size: 10pt; margin: 10pt 0 4pt; color: #0f1a2a; page-break-after: avoid;
     break-after: avoid; }
section.system { page-break-before: always; break-before: page; }
@media print { body { padding: 0; max-width: none; } }
"""


def _e(text):
    return _html.escape("" if text is None else str(text))


def _n(v, places=0):
    if v is None or v == "":
        return "-"
    return ("%." + str(places) + "f") % float(v)


def _table(head, rows, numeric=()):
    out = ["<table><tr>%s</tr>" % "".join("<th>%s</th>" % _e(h) for h in head)]
    for r in rows:
        cells = []
        for i, v in enumerate(r):
            cells.append("<td%s>%s</td>" % (' class="n"' if i in numeric else "", _e(v)))
        out.append("<tr>%s</tr>" % "".join(cells))
    out.append("</table>")
    return "".join(out)


def _value(entry):
    return entry.get("value") if isinstance(entry, dict) else entry


def _source(entry):
    return entry.get("source", "instruction") if isinstance(entry, dict) else "instruction"


def _when(month, hour):
    return "%s 21, %02d:00" % (MONTHS[month - 1], hour) if month else "-"


def html(result, standards=None, takeoff=None, project_name=None):
    """The load calculation sheet as one self-contained page.

    With the take-off it was worked out from, the sheet also says whether THAT
    take-off was confirmed (gate 1) and adds the glass by the way it faces;
    without it, a recorded confirmation is reported as recorded.
    """
    r = result
    b = r.get("building") or {}
    if takeoff is not None:
        sure = LOADS.confirmed(r, takeoff)
    else:
        sure = bool(r.get("geometry_confirmed"))
    stamp = r.get("geometry_confirmed") or {}
    facts = ([("Project", project_name)] if project_name else []) + [
        ("Model", r.get("document")), ("Run", r.get("run_id")), ("Calculated", r.get("when"))]
    # The name the Companion's panel carries too (2026-10-04): what the sheet
    # IS - an HVAC load calculation - and what it is not.
    parts = ["<!doctype html><html><head><meta charset='utf-8'>",
             "<title>HVAC Load Calculation - %s</title><style>%s</style></head><body>"
             % (_e(r.get("document")), CSS),
             "<header class='sheet-head'><div class='kind'>Heron - design aid</div>",
             "<h1>HVAC Load Calculation</h1>",
             "<p class='sub'>Cooling (AC) and heating load per Space - ASHRAE method - a peak "
             "estimate, not an hourly simulation like HAP</p>",
             "<div class='meta'>%s</div>" % "".join(
                 "<div><span>%s</span><b>%s</b></div>" % (_e(k), _e(v)) for k, v in facts),
             "</header>",
             "<p class='lead'>%s</p>" % _e(DISCLAIMER),
             ("<p class='note'>Take-off checked and confirmed by %s, %s.</p>"
              % (_e(stamp.get("by")), _e(stamp.get("at"))) if sure else
              "<p class='disclaimer fail'><b>DRAFT - THE TAKE-OFF WAS NOT CONFIRMED.</b> Nobody "
              "has yet checked the faces these loads were worked out from. Check them in the "
              "Heron Companion's 3D view and confirm them before this sheet is used.</p>")]

    # The building's figures first, as the brain added them up.
    parts.append("<h2>Summary</h2>")
    summary = _table(("", "value", "at"), [
        ("Cooling (AC) load - the rooms' block",
         "%s W  (%s TR)" % (_n(b.get("block_w")), _n(b.get("block_tr"), 2)),
         _when(b.get("block_month"), b.get("block_hour"))),
        ("Cooling with the outdoor air at the coil",
         "%s W  (%s TR)" % (_n(b.get("coil_block_w")), _n(b.get("coil_block_tr"), 2)),
         _when(b.get("coil_block_month"), b.get("coil_block_hour"))),
        ("Heating load", "%s W" % _n(b.get("heating_w")), "-"),
        ("Supply air", "%s L/s" % _n(b.get("supply_ls"), 1), "-"),
        ("Outdoor air", "%s L/s" % _n(b.get("outdoor_air_ls"), 1), "-"),
        ("Cooling per floor area", "%s W/m2" % _n(b.get("block_w_per_m2"), 1), "-"),
        ("Floor area calculated", "%s m2 of %s m2" % (_n(b.get("calculated_area_m2"), 1),
                                                      _n(b.get("area_m2"), 1)), "-"),
        ("Spaces calculated", "%s of %s" % (b.get("calculated", "-"), b.get("spaces", "-")),
         "-")])
    parts.append(summary.replace("<table>", "<table class='summary'>", 1))

    inputs = r.get("inputs") or {}
    project = inputs.get("project") or {}
    parts.append("<h2>Site - where the sun is worked out</h2>")
    site = r.get("site") or {}
    parts.append(_table(("", "value", "source"), [
        ("place", site.get("named") or "-", "model"),
        ("latitude, degrees north", _n(site.get("latitude_deg"), 3), "model"),
        ("longitude, degrees east", _n(site.get("longitude_deg"), 3), "model"),
        ("time zone, hours from UTC", _n(site.get("utc_offset_h"), 1), "model"),
        ("elevation, m", _n(site.get("elevation_m"), 1), "model"),
        ("True North, degrees from project north", _n(site.get("project_to_true_north_deg"), 1),
         "model")]))

    parts.append("<h2>Design conditions</h2>")
    parts.append(_table(("input", "value", "source"),
                        [(k, _value(v), _source(v)) for k, v in sorted(project.items())
                         if not str(k).startswith("beyond")]))

    # What the modeller said is beyond the faces Revit could not see past -
    # part of the take-off, so a reader can check it like any other face.
    said = LOADS.answers(project, strict=False)
    if said:
        names = {}
        if takeoff is not None:
            t = TAKEOFF.read(takeoff)
            for sp in t.spaces:
                for f in sp.get("faces") or []:
                    if f.get("element") is not None:
                        names[str(f["element"])] = "%s (%s)" % (
                            (t.types.get(str(f.get("type"))) or {}).get("name") or "an element",
                            f["element"])
        parts.append("<h2>What is beyond the faces Revit could not see past</h2>")
        parts.append(_table(("element", "beyond it", "source"),
                            [(names.get(k, "element %s" % k), v, "instruction")
                             for k, v in sorted(said.items())]))

    parts.append("<h2>Standards in force</h2>")
    if standards:
        parts.append(_table(("standard", "value"), sorted((k, v) for k, v in standards.items())))
    else:
        parts.append("<p>None recorded for this project.</p>")

    parts.append("<h2>Checks on the model (the geometry gate)</h2>")
    qa = r.get("qa") or []
    if qa:
        parts.append("<ul>%s</ul>" % "".join(
            "<li class='%s'><b>%s</b> %s</li>" % (_e(str(f["level"]).lower()), _e(f["level"]),
                                                 _e(f["text"])) for f in qa))
    else:
        parts.append("<p>Nothing found.</p>")

    if takeoff is not None:
        sm = TAKEOFF.summary(takeoff, LOADS.answers(project, strict=False))
        parts.append("<h2>Glass by the way it faces</h2>")
        rows = [(q, _n(v["wall_m2"], 1), _n(v["glass_m2"], 1), _n(v["glass_pct_of_wall"], 1))
                for q, v in sorted(sm["glass_by_facing"].items(),
                                   key=lambda kv: TAKEOFF.COMPASS.index(kv[0]))]
        parts.append(_table(("faces", "outside wall m2", "glass m2", "glass % of wall"), rows,
                            numeric=(1, 2, 3)))
        parts.append("<p class='note'>Glass to outside, %s m2, is %s %% of the %s m2 of floor "
                     "placed. Taken from the model's own windows and walls.</p>"
                     % (_n(sm["glass_m2"], 1), _n(sm["glass_pct_of_floor"], 1),
                        _n(sm["floor_m2"], 1)))

    parts.append("<h2>Inputs per Space type</h2>")
    for key, values in sorted((inputs.get("profiles") or {}).items()):
        parts.append("<p><b>%s</b></p>" % _e(key))
        parts.append(_table(("input", "value", "source"),
                            [(k, _value(v), _source(v)) for k, v in sorted(values.items())]))
    if inputs.get("overrides"):
        parts.append("<p><b>Per-Space changes</b></p>")
        parts.append(_table(("Space id", "input", "value", "source"),
                            [(sid, k, _value(v), _source(v))
                             for sid, vals in sorted(inputs["overrides"].items())
                             for k, v in sorted(vals.items())]))

    parts.append("<h2>Results per Space</h2>")
    rows = []
    for s in r.get("spaces") or []:
        sh = s.get("shown") or {}
        rows.append(("%s %s" % (s.get("number") or "", s.get("name") or ""), s.get("zone") or "-",
                     _n(s.get("area_m2"), 1), _n(sh.get("sensible_w")), _n(sh.get("latent_w")),
                     _n(sh.get("total_w")), _n(sh.get("w_per_m2"), 1), _n(sh.get("tr"), 2),
                     _n(sh.get("heating_w")), _n(sh.get("supply_ls"), 1),
                     _n(sh.get("outdoor_air_ls"), 1), sh.get("peak") or "-", s["status"]))
    parts.append(_table(("Space", "zone", "m2", "cooling sensible W", "cooling latent W",
                         "cooling total W", "cooling W/m2", "cooling TR", "heating W",
                         "supply air L/s", "outdoor air L/s", "cooling peak at", "status"),
                        rows, numeric=range(2, 11)))

    parts.append("<h2>Each Space at its peak hour, by component</h2>")
    for s in r.get("spaces") or []:
        if s["status"] != "ok":
            continue
        parts.append("<p><b>%s %s</b> - %s</p>" % (_e(s.get("number")), _e(s.get("name")),
                                                  _e((s.get("shown") or {}).get("peak"))))
        parts.append(_table(("component", "sensible W", "latent W"),
                            [(c["name"], _n(c["sensible_w"]), _n(c["latent_w"]))
                             for c in s["cooling"]["components"]], numeric=(1, 2)))

    parts.append("<h2>Zones and building</h2>")

    def zone_row(name, z):
        return (name, _n(z.get("area_m2"), 1), _n(z.get("block_w")),
                _when(z.get("block_month"), z.get("block_hour")), _n(z.get("coil_block_w")),
                _when(z.get("coil_block_month"), z.get("coil_block_hour")),
                _n(z.get("sum_of_peaks_w")), _n(z.get("heating_w")))
    zrows = [zone_row(z["name"], z) for z in r.get("zones") or []]
    zrows.append(zone_row("Building", b))
    parts.append(_table(("", "m2", "rooms' block W", "at", "with outdoor air at the coil W",
                         "at", "sum of peaks W", "heating W"), zrows,
                        numeric=(1, 2, 4, 6, 7)))
    parts.append("<p class='note'>The rooms' block is the largest hour-by-hour sum of the Spaces' "
                 "own cooling loads. With the outdoor air at the coil adds each Space's outdoor "
                 "air, hour by hour. The sum of peaks adds each Space's own peak - what each "
                 "Space's supply air is worked out from. None of them selects plant: an hourly "
                 "method does (see the method notes).</p>")

    parts.append("<h2>The engine's own checks</h2>")
    checked = [("%s %s" % (s.get("number") or "", s.get("name") or ""), level, text)
               for s in r.get("spaces") or [] for level, text in s.get("checks") or []]
    parts.append(_table(("Space", "level", "check"), checked) if checked else "<p>None.</p>")

    parts.append("<h2>Spaces refused or left out</h2>")
    off = [("%s %s" % (s.get("number") or "", s.get("name") or ""), s["status"],
            "; ".join(s.get("why") or [])) for s in r.get("spaces") or [] if s["status"] != "ok"]
    parts.append(_table(("Space", "status", "why"), off) if off else "<p>None.</p>")

    parts.extend(_air_systems(r.get("air_systems")))

    parts.append("<h2>Method notes</h2>")
    parts.append("".join("<p class='note'>%s</p>" % _e(n) for n in r.get("notes") or []))
    parts.append("<p class='disclaimer'>%s %s</p>" % (_e(DISCLAIMER), _e(HVAC.DISCLAIMER)))
    parts.append("</body></html>")
    return "".join(parts)


def _value_text(f):
    """A figure's value as printed - its own number of places, and "-" for none."""
    return _n(f.get("value"), f.get("places", 0))


def _asked_table(asked):
    return _table(("input", "unit", "for", "why", "a standard offers"),
                  [(a["input"], a["unit"], a["for"], a["why"],
                    a.get("offer") or "no figure held - asked, never filled in")
                   for a in asked])


def _air_systems(air):
    """The air systems (docs/44 s13): one overview, then a page for each - what its unit
    must deliver, the air through its coil, and its outdoor air Space by Space, every value
    beside where it came from."""
    if not air:
        return ["<h2>Air systems</h2><p>This run was worked out before Heron sized air "
                "systems - calculate again to add them.</p>"]
    systems = air.get("systems") or []
    out = ["<h2>Air systems - the unit serving each group of Spaces</h2>",
           "<p class='note'>Grouped by %s. Each is sized at the coil's own outdoor design "
           "point, with the rooms at their design condition. %s</p>"
           % (_e(air.get("grouped_by")), _e(HVAC.NOT_HAP))]
    if not systems:
        out.append("<p>No calculated Space is in a Revit zone, and no air system lists its "
                   "Spaces, so no unit was sized.</p>")
    else:
        rows = []
        for s in systems:
            c = s.get("coil") or {}
            rows.append((s["name"], len(s.get("spaces") or []), _n(s.get("area_m2"), 1),
                         _n(c.get("supply_ls"), 1), _n(c.get("outdoor_air_ls"), 1),
                         _n((c.get("total_w") or 0) / 1000.0, 2) if c else "-",
                         _n(c.get("tr"), 2), _n(c.get("shr"), 3), s["status"]))
        out.append(_table(("air system", "Spaces", "m2", "supply air L/s", "outdoor air L/s",
                           "coil total kW", "coil TR", "SHR", "status"), rows,
                          numeric=range(1, 8)))
    for level, text in air.get("checks") or []:
        out.append("<p class='%s'><b>%s</b> %s</p>" % (_e(str(level).lower()), _e(level),
                                                        _e(text)))
    if air.get("asked"):
        out.append("<p><b>Still to be given before %s sized</b> - never filled in (D-33):</p>"
                   % ("these are" if len(systems) > 1 else "it is"))
        seen, asked = set(), []
        for a in air["asked"]:
            if a["input"] not in seen:
                seen.add(a["input"])
                asked.append(a)
        out.append(_asked_table(asked))
    for s in systems:
        out.append("<section class='system'>")
        spaces = ", ".join(("%s %s" % (x.get("number") or "", x.get("name") or "")).strip()
                           for x in s.get("spaces") or [])
        out.append("<h2>Air system: %s</h2>" % _e(s["name"]))
        out.append("<p class='sub'>Serves %s - %s m2. Grouped by %s.</p>"
                   % (_e(spaces or "no Space"), _n(s.get("area_m2"), 1), _e(s["grouped_by"])))
        if s["status"] != "ok":
            out.append("<p class='disclaimer fail'><b>NOT SIZED</b> - %s</p>"
                       % _e("; ".join(s.get("why") or [s["status"]])))
            if s.get("asked"):
                out.append(_asked_table(s["asked"]))
            out.append("</section>")
            continue
        out.append("<h3>What this unit must deliver</h3>")
        out.append(_table(("", "value", "unit", "source"),
                          [(f["item"], _value_text(f), f["unit"], f["source"])
                           for f in s["figures"]], numeric=(1,)))
        out.append("<h3>The air through its cooling coil</h3>")
        out.append(_table(("air", "dry bulb C", "wet bulb C", "g/kg dry air", "RH %",
                           "source"),
                          [(a["point"], _n(a["dry_bulb_c"], 1), _n(a["wet_bulb_c"], 1),
                            _n(a["w_g_kg"], 2), _n(a["rh_pct"], 0), a["source"])
                           for a in s["air"]], numeric=(1, 2, 3, 4)))
        out.append("<h3>Outdoor air, Space by Space - ASHRAE 62.1</h3>")
        out.append(_table(("Space", "m2", "people", "Rp L/s per person", "Ra L/s per m2",
                           "Vbz L/s", "Ez", "Voz L/s", "exhaust L/s", "outdoor air L/s",
                           "set by", "rates from"),
                          [(v["space"], _n(v["area_m2"], 1), v["people"], _n(v["rp"], 2),
                            _n(v["ra"], 2), _n(v["vbz_ls"], 1), _n(v["ez"], 2),
                            _n(v["voz_ls"], 1), _n(v["exhaust_ls"], 1),
                            _n(v["outdoor_air_ls"], 1), v["set_by"], v["rates_source"])
                           for v in s["ventilation"]], numeric=range(1, 10)))
        out.append("<p class='note'>Vbz = Rp x people + Ra x area; Voz = Vbz / Ez. Where a "
                   "Space exhausts more than its own Voz, the difference is added to the "
                   "unit's outdoor air - no air is counted as transferred from another "
                   "Space.</p>")
        if s.get("checks"):
            out.append("<h3>Checks</h3>")
            out.append("<ul>%s</ul>" % "".join(
                "<li class='%s'><b>%s</b> %s</li>" % (_e(str(lv).lower()), _e(lv), _e(t))
                for lv, t in s["checks"]))
        out.append("<h3>What this page does not include</h3>")
        out.append("".join("<p class='note'>%s</p>" % _e(n)
                           for n in list(air.get("not_done") or []) + list(s.get("notes") or [])))
        out.append("<p class='note'><b>%s</b></p>" % _e(HVAC.NOT_HAP))
        out.append("</section>")
    return out


def _csv(header, rows):
    buf = io.StringIO()
    w = csv.writer(buf, lineterminator="\n")
    w.writerow(header)
    for r in rows:
        w.writerow(r)
    return buf.getvalue()


def csv_text(result):
    """One row per Space: the numbers the run used."""
    rows = []
    for s in result.get("spaces") or []:
        sh = s.get("shown") or {}
        peak = (s.get("cooling") or {}).get("peak") or {}
        rows.append([s.get("number"), s.get("name"), s.get("zone"), _n(s.get("area_m2"), 2),
                     _n(sh.get("sensible_w")), _n(sh.get("latent_w")), _n(sh.get("total_w")),
                     _n(sh.get("w_per_m2"), 1), _n(sh.get("tr"), 2), _n(sh.get("heating_w")),
                     _n(sh.get("supply_ls"), 1), _n(sh.get("outdoor_air_ls"), 1),
                     peak.get("month") or "", peak.get("hour") or "", s["status"]])
    return _csv(["number", "name", "zone", "area_m2", "sensible_w", "latent_w", "total_w",
                 "w_per_m2", "tr", "heating_w", "supply_ls", "outdoor_air_ls", "peak_month",
                 "peak_hour", "status"], rows)


def systems_csv(result):
    """Every air system's figures, one per row, each with its unit and where it came
    from - and a row saying why a system was not sized."""
    rows = []
    for s in ((result.get("air_systems") or {}).get("systems")) or []:
        rows.append([s["name"], "status", s["status"], "", "; ".join(s.get("why") or [])])
        for f in s.get("figures") or []:
            rows.append([s["name"], f["item"], _value_text(f), f["unit"], f["source"]])
        for a in s.get("air") or []:
            for key, unit, places in (("dry_bulb_c", "C DB", 2), ("wet_bulb_c", "C WB", 2),
                                      ("w_g_kg", "g/kg", 3), ("rh_pct", "% RH", 1)):
                rows.append([s["name"], a["point"], _n(a[key], places), unit, a["source"]])
    return _csv(["system", "item", "value", "unit", "source"], rows)


def ventilation_csv(result):
    """Every air system's outdoor air, Space by Space."""
    rows = []
    for s in ((result.get("air_systems") or {}).get("systems")) or []:
        for v in s.get("ventilation") or []:
            rows.append([s["name"], v["space"], _n(v["area_m2"], 2), v["people"],
                         _n(v["rp"], 2), _n(v["ra"], 2), _n(v["vbz_ls"], 2), _n(v["ez"], 2),
                         _n(v["voz_ls"], 2),
                         "" if v["exhaust_ls"] is None else _n(v["exhaust_ls"], 2),
                         _n(v["outdoor_air_ls"], 2), v["set_by"], v["rates_source"]])
    return _csv(["system", "space", "area_m2", "people", "rp_ls_per_person", "ra_ls_per_m2",
                 "vbz_ls", "ez", "voz_ls", "exhaust_ls", "outdoor_air_ls", "set_by",
                 "rates_source"], rows)


def takeoff_csv(t):
    """Every face and every opening the loads were built from, so an area can be checked."""
    t = TAKEOFF.read(t)
    north = t.site.get("project_to_true_north_deg")
    rows = []

    def kind(type_id):
        return t.types.get(str(type_id)) or {}

    for s in t.spaces:
        for f in s.get("faces") or []:
            k = kind(f.get("type"))
            facing = (_n(TAKEOFF.azimuth_deg(f["normal"], north), 1)
                      if f.get("side") == "wall" and f.get("normal") else "")
            rows.append([s.get("number"), s.get("name"), f.get("element"), k.get("name"),
                         f.get("side"), f.get("beyond"), facing, _n(f.get("area_m2"), 3),
                         k.get("u_w_m2k"), k.get("shgc")])
            for o in f.get("openings") or []:
                ok = kind(o.get("type"))
                rows.append([s.get("number"), s.get("name"), o.get("element"), ok.get("name"),
                             o.get("kind"), f.get("beyond"), facing, _n(o.get("area_m2"), 3),
                             ok.get("u_w_m2k"), ok.get("shgc")])
    return _csv(["space_number", "space_name", "element_id", "type", "side", "beyond",
                 "facing_deg", "area_m2", "u_w_m2k", "shgc"], rows)


def browsers():
    """The four usual places a Windows PC keeps Edge and Chrome."""
    out = []
    for base in (os.environ.get("ProgramFiles(x86)"), os.environ.get("ProgramFiles")):
        if base:
            out.append(os.path.join(base, "Microsoft", "Edge", "Application", "msedge.exe"))
    for base in (os.environ.get("ProgramFiles(x86)"), os.environ.get("ProgramFiles")):
        if base:
            out.append(os.path.join(base, "Google", "Chrome", "Application", "chrome.exe"))
    return out


def pdf(html_path, pdf_path, search=None):
    """(made, why) - the page printed to PDF by a headless Edge or Chrome. Never raises for
    a missing browser: the HTML page is then the report."""
    exe = next((p for p in (browsers() if search is None else search) if os.path.isfile(p)),
               None)
    if exe is None:
        return False, "no Edge or Chrome found - the HTML page is the report"
    command = [exe, "--headless", "--disable-gpu", "--no-pdf-header-footer",
               "--print-to-pdf=" + os.path.abspath(pdf_path),
               pathlib.Path(os.path.abspath(html_path)).as_uri()]
    try:
        done = subprocess.run(command, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                              stdin=subprocess.DEVNULL, timeout=60)
    except (OSError, subprocess.TimeoutExpired) as why:
        return False, "the browser could not print the page (%s) - the HTML page is the report" % why
    if done.returncode != 0 or not os.path.isfile(pdf_path):
        return False, ("the browser exited %s without a PDF - the HTML page is the report"
                       % done.returncode)
    return True, "printed by %s" % os.path.basename(exe)


def write(folder, takeoff, result, standards=None, search=None, project_name=None):
    """Write the sheet, its CSV, the take-off CSV and - where a browser can - the PDF.

    Returns {"ok", "said", "html", "pdf", "csv", "takeoff_csv"} - and "systems_csv" and
    "ventilation_csv" when the run sized or asked about any air system.
    """
    if not os.path.isdir(folder):
        os.makedirs(folder)
    stem = "loads-%s" % (result.get("run_id") or "run")
    paths = {"html": os.path.join(folder, stem + ".html"),
             "csv": os.path.join(folder, stem + ".csv"),
             "takeoff_csv": os.path.join(folder, stem + "-takeoff.csv")}
    with io.open(paths["html"], "w", encoding="utf-8") as fh:
        fh.write(html(result, standards, takeoff, project_name))
    with io.open(paths["csv"], "w", encoding="utf-8", newline="") as fh:
        fh.write(csv_text(result))
    with io.open(paths["takeoff_csv"], "w", encoding="utf-8", newline="") as fh:
        fh.write(takeoff_csv(takeoff))
    if ((result.get("air_systems") or {}).get("systems")):
        paths["systems_csv"] = os.path.join(folder, stem + "-systems.csv")
        paths["ventilation_csv"] = os.path.join(folder, stem + "-ventilation.csv")
        with io.open(paths["systems_csv"], "w", encoding="utf-8", newline="") as fh:
            fh.write(systems_csv(result))
        with io.open(paths["ventilation_csv"], "w", encoding="utf-8", newline="") as fh:
            fh.write(ventilation_csv(result))
    files = "%s CSV files" % ("four" if "systems_csv" in paths else "two")
    pdf_path = os.path.join(folder, stem + ".pdf")
    made, why = pdf(paths["html"], pdf_path, search)
    paths["pdf"] = pdf_path if made else None
    said = ("Report written to %s - %s." % (folder, "the PDF, the HTML page and " + files
                                             if made else "the HTML page and " + files +
                                             "; no PDF: " + why))
    return dict(paths, ok=True, said=said)
