# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The load calculation sheet - docs/44 section 8.

One self-contained HTML page (inline CSS, no script, no outside address), a
CSV of the results, a CSV of the surface take-off every load was built from,
and - where Microsoft Edge or Google Chrome is on the PC - a PDF printed from
the page in the browser's headless mode. No new package: with no browser the
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

    parts.append("<h2>Method notes</h2>")
    parts.append("".join("<p class='note'>%s</p>" % _e(n) for n in r.get("notes") or []))
    parts.append("<p class='disclaimer'>%s %s</p>" % (_e(DISCLAIMER), _e(HVAC.DISCLAIMER)))
    parts.append("</body></html>")
    return "".join(parts)


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

    Returns {"ok", "said", "html", "pdf", "csv", "takeoff_csv"}.
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
    pdf_path = os.path.join(folder, stem + ".pdf")
    made, why = pdf(paths["html"], pdf_path, search)
    paths["pdf"] = pdf_path if made else None
    said = ("Report written to %s - %s." % (folder, "the PDF, the HTML page and two CSV files"
                                             if made else "the HTML page and two CSV files; "
                                             "no PDF: " + why))
    return dict(paths, ok=True, said=said)
