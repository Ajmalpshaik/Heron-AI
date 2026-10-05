# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The hydraulic calculation sheet - docs/46 section 8.

One self-contained HTML page (inline CSS, no script, no outside address), a
CSV of the operating heads, a CSV of the pipes and - where Microsoft Edge or
Google Chrome is on the PC - a PDF, printed by the loads report's own printer
(heron_loads_report.pdf - one home).

THE SHEET IS PRINTED FROM THE NUMBERS THE RUN USED, never from a separate
list: every figure comes out of the run's own answer (heron_fire's `hydraulic`,
its tables and its data) and every input out of the run's own inputs, each
with the label saying where it came from.

Standard library only.
"""

import io
import os

import heron_fire as FIRE
import heron_sprinkler_water as WATER
import heron_loads_report as SHEET
import heron_sprinkler_run as RUN
import heron_sprinkler_takeoff as TAKEOFF

_e = SHEET._e
WATER_FLOW = FIRE.flow_text
WATER_PRESSURE = FIRE.pressure_text
_table = SHEET._table
_csv = SHEET._csv

DRAFT = "DRAFT - THE NETWORK WAS NOT CONFIRMED"
TITLE = "Sprinkler hydraulic calculation"


def _v(entry):
    return entry.get("value") if isinstance(entry, dict) else entry


def _src(entry):
    return entry.get("source", "instruction") if isinstance(entry, dict) else "instruction"


def html(result, network=None, standards=None, project_name=None):
    """The sheet as one self-contained page. With the network, whether THAT network was
    confirmed (gate 1) is checked; without it, a recorded confirmation is reported as
    recorded."""
    r = result
    inputs = r.get("inputs") or {}
    answer = r.get("answer") or {}
    data = answer.get("data") or {}
    ok = r.get("status") == "ok"
    if network is not None:
        sure = RUN.confirmed(r, network)
    else:
        sure = bool(r.get("network_confirmed"))
    system = r.get("system") or {}
    parts = ["<!DOCTYPE html><html><head><meta charset='utf-8'><title>%s</title>"
             "<style>%s</style></head><body>" % (_e(TITLE), SHEET.CSS)]
    parts.append("<div class='sheet-head'><div class='kind'>%s</div><h1>%s</h1>"
                 "<p class='sub'>%s</p><div class='meta'>"
                 "<div><span>Run</span>%s</div><div><span>Worked out</span>%s</div>"
                 "<div><span>Source</span>%s</div><div><span>Network</span>%s</div></div></div>"
                 % (_e(TITLE), _e(system.get("name") or "Sprinkler system"),
                    _e(" - ".join(x for x in (project_name, system.get("classification")) if x)),
                    _e(r.get("run_id")), _e(r.get("when")), _e(r.get("source")),
                    _e(r.get("network_fingerprint"))))
    if not sure:
        parts.append("<p class='disclaimer fail'><b>%s.</b> Look at the 3D view and the model "
                     "checks in the Heron Companion, then press 'The network is right'.</p>"
                     % _e(DRAFT))
    else:
        got = r.get("network_confirmed") or {}
        parts.append("<p class='note'>The network was confirmed by %s at %s.</p>" % (
            _e(got.get("by")), _e(got.get("at"))))
    if not ok:
        parts.append("<h2>Not solved</h2>")
        parts.append("".join("<p class='lead fail'>%s</p>" % _e(t)
                             for t in r.get("refused") or []))
        parts.append("".join("<p class='lead'>Still asked: %s - %s</p>" % (
            _e(a.get("input")), _e(a.get("why"))) for a in r.get("asked") or []))
    else:
        parts.append("<h2>Summary</h2>")
        rows = [["Operating heads", "%d" % len(inputs.get("operating") or [])],
                ["Governing head", data.get("governing")],
                ["Sprinkler demand", FIRE.flow_text(data.get("demand_lpm"))],
                ["Hose allowance", FIRE.flow_text(data.get("hose_lpm") or 0.0)],
                ["Total demand", FIRE.flow_text(data.get("total_lpm"))],
                ["Pressure at the source %s" % data.get("source"),
                 FIRE.pressure_text(data.get("source_bar"))],
                ["With device losses", FIRE.pressure_text(data.get("supply_bar"))]]
        if data.get("supply"):
            s = data["supply"]
            rows.append(["Flow test gives at the demand", FIRE.pressure_text(s["at_demand_bar"])])
            rows.append(["Margin", FIRE.pressure_text(s["at_demand_bar"] - data["supply_bar"])])
        else:
            rows.append(["Supply", "no flow test given - the supply is not checked"])
        parts.append(_table(["", ""], rows).replace("<table>", "<table class='summary'>", 1))
        for name, text in answer.get("results") or []:
            parts.append("<p class='note'>%s: %s</p>" % (_e(name), _e(text)))
    parts.append("<h2>Design criteria</h2>")
    crit = inputs.get("criteria") or {}
    parts.append(_table(["Criterion", "Value", "Unit", "Where it came from"],
                        [[k, FIRE._g(_v(crit[k])), RUN.CRITERIA[k][0], _src(crit[k])]
                         for k in RUN.ORDER if k in crit], numeric=(1,)))
    std = dict(standards or {})
    std.update((k, v) for k, v in (answer.get("standards") or {}).items())
    if std:
        parts.append(_table(["Standard", "Value", "From"], [
            [k, _v(v) if not isinstance(v, dict) else v.get("value"),
             v.get("from", "") if isinstance(v, dict) else "the project's record"]
            for k, v in sorted(std.items())]))
    parts.append("<h2>Sprinkler K-factors</h2>")
    types = TAKEOFF.sprinkler_types(network) if network is not None else {}
    kk = inputs.get("k") or {}
    parts.append(_table(["Type", "Heads", "Revit holds", "K L/min/bar^0.5", "From"], [
        [(types.get(k) or {}).get("name", k), len((types.get(k) or {}).get("heads") or []),
         (types.get(k) or {}).get("parameter") or "-", FIRE._g(_v(v)), _src(v)]
        for k, v in sorted(kk.items())], numeric=(1, 3)))
    parts.append("<h2>Fittings and valves - equivalent lengths</h2>")
    rows_f = TAKEOFF.fitting_rows(network) if network is not None else {}
    ff = inputs.get("fittings") or {}
    parts.append(_table(["Fitting", "Size", "Count", "Equivalent length m", "From"], [
        [(rows_f.get(k) or {}).get("kind", k.split("|")[0]),
         (rows_f.get(k) or {}).get("size") or k.split("|")[-1],
         (rows_f.get(k) or {}).get("count", "-"), FIRE._g(_v(v)), _src(v)]
        for k, v in sorted(ff.items())], numeric=(2, 3)))
    parts.append("<p class='note'>Each fitting's equivalent length is added to the smallest "
                 "pipe that meets it - the branch of a tee, the outlet of a reducer - because "
                 "the flow direction is not known before the solve; on a tee of one size it "
                 "goes to the pipe with the larger id, and a run straight through a tee is "
                 "counted as a turn - both err long. Every fitting is counted as modelled.</p>")
    parts.append("<h2>Remote area</h2><p>%s</p>" % _e(", ".join(inputs.get("operating") or [])
                                                       or "none ticked"))
    if r.get("suggested"):
        parts.append("<p class='note'>Heron suggested: %s. %s</p>" % (
            _e(", ".join(r["suggested"].get("heads") or [])),
            _e("The modeller kept it." if sorted(r["suggested"].get("heads") or [])
               == sorted(inputs.get("operating") or []) else "The modeller changed it.")))
    if ok:
        for t in answer.get("tables") or []:
            parts.append("<h2>%s</h2>" % _e(t["title"]))
            parts.append(_table(t["columns"], t["rows"], numeric=tuple(range(1, len(
                t["columns"])))))
        parts.append("<h2>Checks</h2>")
        parts.append("".join("<p class='lead %s'>%s - %s</p>" % (
            "fail" if lv == "FAIL" else "warn" if lv == "WARN" else "", _e(lv), _e(t))
            for lv, t in answer.get("checks") or []))
    parts.append(water_html(r.get("water")))
    parts.append(spacing_html(r.get("spacing")))
    parts.append("<h2>Model checks</h2>")
    parts.append("".join("<p class='lead %s'>%s - %s</p>" % (
        "fail" if f["level"] == "FAIL" else "warn" if f["level"] == "WARN" else "",
        _e(f["level"]), _e(f["text"])) for f in r.get("qa") or []))
    if ok:
        parts.append("<h2>Method</h2>")
        parts.append("".join("<p class='note'>%s</p>" % _e(t) for t in answer.get("method") or []))
        if answer.get("assumed"):
            parts.append("<h2>Assumed</h2>")
            parts.append("".join("<p class='note'>%s</p>" % _e(t) for t in answer["assumed"]))
        parts.append("<h2>Sources</h2>")
        parts.append("".join("<p class='note'>%s</p>" % _e(t) for t in answer.get("sources") or []))
    parts.append("".join("<p class='note'>%s</p>" % _e(t) for t in r.get("notes") or []))
    parts.append("<p class='disclaimer'>%s</p>" % _e(FIRE.DISCLAIMER))
    parts.append("</body></html>")
    return "".join(parts)


def _answer_html(answer):
    """One engine answer's results, tables and checks - as the engine printed them."""
    out = []
    for name, text in answer.get("results") or []:
        out.append("<p class='note'>%s: %s</p>" % (_e(name), _e(text)))
    for t in answer.get("tables") or []:
        out.append("<p class='lead'>%s</p>" % _e(t["title"]))
        out.append(_table(t["columns"], t["rows"]))
    for lv, t in answer.get("checks") or []:
        out.append("<p class='lead %s'>%s - %s</p>" % (
            "fail" if lv == "FAIL" else "warn" if lv == "WARN" else "", _e(lv), _e(t)))
    for t in answer.get("assumed") or []:
        out.append("<p class='note'>Assumed: %s</p>" % _e(t))
    return "".join(out)


def water_html(water):
    """The Fire water section (docs/46 s13.1) - each part as the engine answered it, a part
    not included said so, and how the total was made."""
    if not water:
        return ""
    out = ["<h2>Fire water</h2>"]
    if water.get("status") == "not solved":
        return out[0] + "".join("<p class='note'>%s</p>" % _e(t) for t in water.get("notes") or [])
    total = water.get("total") or {}
    out.append(_table(["", ""], [
        ["Total flow at the source", WATER_FLOW(total.get("flow_lpm"))],
        ["Pressure the source must give", WATER_PRESSURE(total.get("pressure_bar"))]])
        .replace("<table>", "<table class='summary'>", 1))
    for line in total.get("from") or []:
        out.append("<p class='note'>From: %s</p>" % _e(line))
    for key, _calc, label in WATER.PARTS + (("supply", "water_supply",
                                             "The flow test against the total"),):
        got = (water.get("parts") or {}).get(key)
        if got is None:
            continue
        out.append("<p class='lead'>%s</p>" % _e(label))
        if got.get("status") == "ok":
            out.append(_answer_html(got))
        elif got.get("status") == "missing":
            out.append("<p class='note'>Not worked out - still asked: %s</p>" % _e(
                ", ".join(m["input"] for m in got.get("missing") or [])))
        else:
            out.append("<p class='lead fail'>%s</p>" % _e("; ".join(got.get("refused") or [])))
    for t in water.get("notes") or []:
        out.append("<p class='note'>%s</p>" % _e(t))
    return "".join(out)


def spacing_html(spacing):
    """The Spacing section (docs/46 s13.2) - every Space with a head, its class, its result,
    every failing head; a Space not checked said as not checked."""
    if not spacing or not (spacing.get("spaces") or spacing.get("loose")):
        return ""
    out = ["<h2>Spacing in each Space</h2>"]
    rows = []
    for sp in spacing.get("spaces") or []:
        rows.append([sp.get("label"), sp.get("level") or "-", len(sp.get("heads") or []),
                     sp.get("hazard") or "-",
                     "-" if sp.get("angle_deg") is None else "%.1f (%s)" % (
                         sp["angle_deg"], sp.get("angle_from")),
                     sp.get("status"), sp.get("why") or ""])
    out.append(_table(["Space", "Level", "Heads", "Hazard class", "Branch lines, degrees",
                       "Result", "Why"], rows, numeric=(2,)))
    for sp in spacing.get("spaces") or []:
        answer = sp.get("answer") or {}
        if sp.get("status") in ("ok", "fail") and answer.get("tables"):
            out.append("<p class='lead'>%s</p>" % _e(sp.get("label")))
            out.append(_answer_html(answer))
    if spacing.get("loose"):
        out.append("<p class='lead warn'>In no Space, so not checked: %s</p>"
                   % _e(", ".join(spacing["loose"])))
    out.append("<p class='note'>Each Space is turned so its branch lines lie along x before "
               "S and L are measured; the direction is read from the model's level pipes in the "
               "Space unless the modeller gave one. A Space with no hazard class is not "
               "checked - it is never passed.</p>")
    return "".join(out)


def heads_csv(result):
    """One row per operating head: the numbers the run used."""
    d = ((result.get("answer") or {}).get("data") or {})
    rows = [[h, FIRE._g(v["k"]), "%.2f" % v["q_req_lpm"], "%.4f" % v["p_req_bar"],
             "%.2f" % v["q_lpm"], "%.4f" % v["p_bar"]]
            for h, v in sorted((d.get("heads") or {}).items())]
    return _csv(["head", "k_lpm_bar", "needs_lpm", "needs_bar", "gives_lpm", "at_bar"], rows)


def pipes_csv(result):
    """One row per pipe, in the flow direction, with the element it was read from."""
    d = ((result.get("answer") or {}).get("data") or {})
    rows = []
    given = result.get("pipes") or []
    for p in d.get("pipes") or []:
        a, b = (p["from"], p["to"]) if p["q_lpm"] >= 0 else (p["to"], p["from"])
        row = given[p["index"]] if 0 <= p.get("index", -1) < len(given) else {}
        rows.append([row.get("segment") or row.get("element"), a, b, "%.2f" % p["bore_mm"], FIRE._g(p["c"]),
                     "%.3f" % p["length_m"], "%.3f" % p["eq_m"], "%.2f" % abs(p["q_lpm"]),
                     "%.3f" % p["v_ms"], "%.5f" % p["loss_bar"]])
    return _csv(["pipe", "from", "to", "bore_mm", "c", "length_m", "fittings_m", "flow_lpm",
                 "velocity_ms", "loss_bar"], rows)


def write(folder, network, result, standards=None, search=None, project_name=None):
    """Write the sheet, its two CSVs and - where a browser can - the PDF.

    Returns {"ok", "said", "html", "pdf", "csv", "pipes_csv"}.
    """
    if not os.path.isdir(folder):
        os.makedirs(folder)
    stem = "sprinkler-%s" % (result.get("run_id") or "run")
    paths = {"html": os.path.join(folder, stem + ".html"),
             "csv": os.path.join(folder, stem + "-heads.csv"),
             "pipes_csv": os.path.join(folder, stem + "-pipes.csv")}
    with io.open(paths["html"], "w", encoding="utf-8") as fh:
        fh.write(html(result, network, standards, project_name))
    with io.open(paths["csv"], "w", encoding="utf-8", newline="") as fh:
        fh.write(heads_csv(result))
    with io.open(paths["pipes_csv"], "w", encoding="utf-8", newline="") as fh:
        fh.write(pipes_csv(result))
    pdf_path = os.path.join(folder, stem + ".pdf")
    made, why = SHEET.pdf(paths["html"], pdf_path, search)
    paths["pdf"] = pdf_path if made else None
    said = ("Report written to %s - %s." % (folder, "the PDF, the HTML page and two CSV files"
                                             if made else "the HTML page and two CSV files; "
                                             "no PDF: " + why))
    return dict(paths, ok=True, said=said)
