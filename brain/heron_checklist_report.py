# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-QA-BIM-011
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
A QA checklist run, written as a report a coordinator can send (D-178): one
Excel workbook and one HTML page, from the run Heron already has.

WHAT IT IS GIVEN is the answer heron_checklists.run returns - its rows, counts,
coverage line, findings and history. Nothing is read from Revit again, and
nothing in the model is touched: a run only reads, and so does this.

WHAT IT PROMISES

  * EVERY RESULT WORD AS THE RUN SAID IT - PASS, FAIL, NO DATA, NOT RUN - and
    PASS explained as "meets the value checked". Never "compliant": whether a
    model complies is the engineer's or the authority's decision, not Heron's.
  * THE MODELLER'S WORDS ARE DATA. In the page every value is escaped and
    there is no script; in the workbook every value is an inline string or a
    number and never a formula, so a sentence that starts with "=" stays text.
    A character XML cannot hold is dropped.
  * NOTHING IS WRITTEN OVER. Each file is created new (O_EXCL); a name already
    taken gets " (2)", " (3)" ... So a report can never replace one already
    sent, and two reports written at once cannot land in one file.
  * SELF-CONTAINED. The page fetches nothing - no font, no image, no script -
    so it opens the same on a PC with no network, and in an email.

Pure standard library - zipfile and the XML it needs - so it adds nothing to
what Heron installs. tests/test_checklist_report.py holds all of it.
"""

import errno
import io
import os
import re
import time
import zipfile

# Excel's limit on the text in one cell.
CELL_MAX = 32767
# How many failing ids the page lists before "and N more" - the workbook keeps
# them all, up to the cell limit.
PAGE_IDS = 200
NAME_MAX = 60

PASS_MEANS = ("PASS means the model meets the value checked. Whether it complies with a "
              "standard is the engineer's or the authority's decision, not Heron's.")
CHANGED_NOTHING = ("Nothing in the model was changed: each check is a read with no transaction "
                   "open, so there is nothing to undo, keep or discard.")

# Characters XML 1.0 cannot carry: every control character but tab, LF and CR.
_ILLEGAL = re.compile(u"[\x00-\x08\x0b\x0c\x0e-\x1f￾￿]")
# Characters a Windows file name cannot hold, and the control characters.
_UNSAFE_NAME = re.compile(r'[<>:"/\\|?*\x00-\x1f]+')

_COLOUR = {"pass": "#1f8a5b", "fail": "#c0392b", "missing": "#b7791f", "not run": "#6b7280"}


def _clean(value):
    """Text with no character XML cannot hold, never None."""
    if value is None:
        return ""
    if not isinstance(value, str):
        value = str(value)
    return _ILLEGAL.sub("", value)


def _esc(value):
    return (_clean(value).replace("&", "&amp;").replace("<", "&lt;")
            .replace(">", "&gt;").replace('"', "&quot;").replace("'", "&#39;"))


def _stamp(now, fmt):
    if now is None:
        now = time.localtime()
    return time.strftime(fmt, now)


def _ids(row):
    return [str(i) for i in row.get("ids") or []]


def _change(row):
    label = row.get("changeLabel") or ""
    why = row.get("changeWhy") or ""
    return (label + (" - " + why if why else "")).strip(" -")


def _history_said(answer):
    history = answer.get("history")
    if isinstance(history, dict):
        return history.get("said") or ""
    return history if isinstance(history, str) else ""


def _summary_lines(answer, now):
    """(label, value) pairs, the same on the page and in the workbook."""
    counts = answer.get("counts") or {}
    lines = [("Checklist", answer.get("name") or ""),
             ("Description", answer.get("description") or ""),
             ("Model", answer.get("document") or "not read"),
             ("Report made", _stamp(now, "%Y-%m-%d %H:%M")),
             ("Checks", counts.get("total", len(answer.get("rows") or []))),
             ("PASS", counts.get("pass", 0)),
             ("FAIL", counts.get("fail", 0)),
             ("NO DATA", counts.get("missing", 0)),
             ("NOT RUN", counts.get("not run", 0)),
             ("Coverage", (answer.get("coverage") or {}).get("said", "")),
             ("Since the last run", _history_said(answer)),
             ("Stopped", answer.get("stopped") or "")]
    return [(k, v) for k, v in lines if v not in ("", None)]


# ---------------------------------------------------------------- the page

_STYLE = """
:root{--ink:#17202b;--mute:#5b6675;--line:#d9dee5;--band:#f3f5f8}
*{box-sizing:border-box}
body{margin:0;padding:24px 16px;color:var(--ink);background:#fff;
font:14px/1.5 "Segoe UI",Arial,sans-serif}
.wrap{max-width:1100px;margin:0 auto}
h1{font-size:22px;margin:0 0 4px}
.lead{color:var(--mute);margin:0 0 16px}
table{border-collapse:collapse;width:100%}
.meta td{padding:4px 12px 4px 0;vertical-align:top}
.meta td:first-child{color:var(--mute);white-space:nowrap}
.note{background:var(--band);border:1px solid var(--line);border-radius:6px;padding:10px 12px;margin:16px 0}
.checks{margin-top:8px}
.checks th,.checks td{border-bottom:1px solid var(--line);padding:8px 10px;text-align:left;vertical-align:top}
.checks th{background:var(--band);font-size:12px;text-transform:uppercase;letter-spacing:.04em;color:var(--mute)}
.state{display:inline-block;min-width:68px;text-align:center;color:#fff;border-radius:4px;
padding:2px 6px;font-weight:600;font-size:12px}
.small{color:var(--mute);font-size:12px}
.ids{font-family:Consolas,monospace;font-size:12px;word-break:break-all}
.scroll{overflow-x:auto}
footer{margin-top:20px;color:var(--mute);font-size:12px}
@media print{body{padding:0}.scroll{overflow:visible}}
"""


def html(answer, now=None):
    """The run as one self-contained HTML page: no script, nothing fetched."""
    title = "%s - QA report" % (answer.get("name") or "Checklist")
    out = ["<!doctype html>", '<html lang="en"><head><meta charset="utf-8">',
           '<meta name="viewport" content="width=device-width,initial-scale=1">',
           "<title>%s</title>" % _esc(title), "<style>%s</style></head><body>" % _STYLE,
           '<div class="wrap">', "<h1>%s</h1>" % _esc(answer.get("name") or "Checklist")]
    if answer.get("description"):
        out.append('<p class="lead">%s</p>' % _esc(answer["description"]))
    out.append('<table class="meta">')
    for key, value in _summary_lines(answer, now):
        if key in ("Checklist", "Description"):
            continue
        out.append("<tr><td>%s</td><td>%s</td></tr>" % (_esc(key), _esc(value)))
    out.append("</table>")
    out.append('<p class="note">%s</p>' % _esc(PASS_MEANS))
    for finding in answer.get("findings") or []:
        out.append('<p class="note">%s</p>' % _esc(finding))

    out.append('<div class="scroll"><table class="checks"><thead><tr>'
               "<th>#</th><th>Result</th><th>Check</th><th>Read</th><th>Since last run</th>"
               "<th>Detail</th></tr></thead><tbody>")
    for row in answer.get("rows") or []:
        state = row.get("state") or "not run"
        label = row.get("label") or state.upper()
        detail = []
        tags = [t for t in (row.get("severity") and "Severity " + row["severity"],
                            row.get("clause") and "Clause " + row["clause"]) if t]
        if tags:
            detail.append('<div class="small">%s</div>' % _esc(" | ".join(tags)))
        if row.get("why") and state != "pass":
            detail.append("<div>%s</div>" % _esc(row["why"]))
        if row.get("advice") and state == "fail":
            detail.append("<div><b>To fix:</b> %s</div>" % _esc(row["advice"]))
        ids = _ids(row)
        if ids:
            shown = ", ".join(ids[:PAGE_IDS])
            more = len(ids) - PAGE_IDS
            detail.append('<div class="small">Failing element ids (%d)</div><div class="ids">%s%s</div>'
                          % (len(ids), _esc(shown), _esc(" and %d more" % more) if more > 0 else ""))
        read = _esc(row.get("shown") or "-")
        if row.get("coverageShown"):
            read += '<div class="small">%s</div>' % _esc(row["coverageShown"])
        out.append("<tr><td>%s</td><td><span class=\"state\" style=\"background:%s\">%s</span></td>"
                   "<td>%s<div class=\"small\">%s - rule %s</div></td><td>%s</td><td>%s</td><td>%s</td></tr>"
                   % (_esc(row.get("n", "")), _COLOUR.get(state, "#6b7280"), _esc(label),
                      _esc(row.get("sentence") or ""), _esc(row.get("capability") or ""),
                      _esc(row.get("rule") or ""), read, _esc(_change(row)), "".join(detail)))
    out.append("</tbody></table></div>")
    out.append("<footer>%s Made by Heron on %s.</footer>"
               % (_esc(CHANGED_NOTHING), _esc(_stamp(now, "%Y-%m-%d %H:%M"))))
    out.append("</div></body></html>")
    return "\n".join(out)


# ------------------------------------------------------------- the workbook

def _column(index):
    """0 -> A, 25 -> Z, 26 -> AA."""
    letters = ""
    index += 1
    while index:
        index, rest = divmod(index - 1, 26)
        letters = chr(65 + rest) + letters
    return letters


def _cell(ref, value, bold=False):
    style = ' s="1"' if bold else ""
    if isinstance(value, bool):
        value = "yes" if value else "no"
    if isinstance(value, (int, float)):
        return '<c r="%s"%s><v>%s</v></c>' % (ref, style, value)
    text = _clean(value)[:CELL_MAX]
    text = text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
    return '<c r="%s"%s t="inlineStr"><is><t xml:space="preserve">%s</t></is></c>' % (ref, style, text)


def _sheet(rows, widths, frozen=False):
    out = ['<?xml version="1.0" encoding="UTF-8" standalone="yes"?>',
           '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">']
    if frozen:
        out.append('<sheetViews><sheetView workbookViewId="0"><pane ySplit="1" topLeftCell="A2" '
                   'activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>')
    out.append("<cols>%s</cols>" % "".join(
        '<col min="%d" max="%d" width="%d" customWidth="1"/>' % (i, i, w)
        for i, w in enumerate(widths, 1)))
    out.append("<sheetData>")
    for r, (values, bold) in enumerate(rows, 1):
        out.append('<row r="%d">%s</row>' % (r, "".join(
            _cell("%s%d" % (_column(c), r), v, bold) for c, v in enumerate(values))))
    out.append("</sheetData></worksheet>")
    return "".join(out)


_CHECK_HEADER = ("#", "Result", "Check", "Capability", "Rule", "Read", "Coverage", "Since last run",
                 "Severity", "Clause", "Why", "To fix", "Failing ids", "Failing element ids")

_PARTS = {
    "[Content_Types].xml":
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">'
        '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>'
        '<Default Extension="xml" ContentType="application/xml"/>'
        '<Override PartName="/xl/workbook.xml" '
        'ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>'
        '<Override PartName="/xl/worksheets/sheet1.xml" '
        'ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>'
        '<Override PartName="/xl/worksheets/sheet2.xml" '
        'ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>'
        '<Override PartName="/xl/styles.xml" '
        'ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>'
        '</Types>',
    "_rels/.rels":
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
        '<Relationship Id="rId1" '
        'Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" '
        'Target="xl/workbook.xml"/></Relationships>',
    "xl/workbook.xml":
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" '
        'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">'
        '<sheets><sheet name="Summary" sheetId="1" r:id="rId1"/>'
        '<sheet name="Checks" sheetId="2" r:id="rId2"/></sheets></workbook>',
    "xl/_rels/workbook.xml.rels":
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
        '<Relationship Id="rId1" '
        'Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" '
        'Target="worksheets/sheet1.xml"/>'
        '<Relationship Id="rId2" '
        'Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" '
        'Target="worksheets/sheet2.xml"/>'
        '<Relationship Id="rId3" '
        'Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" '
        'Target="styles.xml"/></Relationships>',
    "xl/styles.xml":
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">'
        '<fonts count="2"><font><sz val="11"/><name val="Calibri"/></font>'
        '<font><b/><sz val="11"/><name val="Calibri"/></font></fonts>'
        '<fills count="2"><fill><patternFill patternType="none"/></fill>'
        '<fill><patternFill patternType="gray125"/></fill></fills>'
        '<borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>'
        '<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>'
        '<cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>'
        '<xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs>'
        '<cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>'
        '</styleSheet>',
}


def xlsx(answer, now=None):
    """The run as an .xlsx workbook, as bytes: a Summary sheet and a Checks
    sheet, one row a check. Every value is text or a number - no formula."""
    summary = [(("QA report", ""), True)]
    summary += [((key, value), False) for key, value in _summary_lines(answer, now)]
    summary += [(("", ""), False), (("Note", PASS_MEANS), False)]
    summary += [(("Finding", f), False) for f in answer.get("findings") or []]
    summary += [(("", CHANGED_NOTHING), False)]

    checks = [(_CHECK_HEADER, True)]
    for row in answer.get("rows") or []:
        ids = _ids(row)
        checks.append(((row.get("n", ""), row.get("label") or "", row.get("sentence") or "",
                        row.get("capability") or "", row.get("rule") or "", row.get("shown") or "-",
                        row.get("coverageShown") or "", _change(row), row.get("severity") or "",
                        row.get("clause") or "", row.get("why") or "" if row.get("state") != "pass" else "",
                        row.get("advice") or "" if row.get("state") == "fail" else "",
                        len(ids), ", ".join(ids)), False))

    out = io.BytesIO()
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as zf:
        for name, data in _PARTS.items():
            zf.writestr(name, data)
        zf.writestr("xl/worksheets/sheet1.xml", _sheet(summary, (22, 110)))
        zf.writestr("xl/worksheets/sheet2.xml",
                    _sheet(checks, (5, 10, 48, 28, 18, 14, 30, 30, 10, 14, 48, 40, 11, 60), frozen=True))
    return out.getvalue()


# ---------------------------------------------------------------- the files

def _safe(text):
    text = _UNSAFE_NAME.sub(" ", _clean(text)).strip(" .")
    text = re.sub(r"\s+", " ", text)
    return text[:NAME_MAX].strip(" .") or "checklist"


def _create(folder, stem, extension, data):
    """Write `data` to a NEW file in `folder` - never over one that is there.
    A taken name gets " (2)", " (3)" ...; O_EXCL makes the check and the
    create one step, so nothing written meanwhile is replaced."""
    for number in range(1, 1000):
        name = "%s%s%s" % (stem, "" if number == 1 else " (%d)" % number, extension)
        path = os.path.join(folder, name)
        try:
            handle = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL | getattr(os, "O_BINARY", 0), 0o644)
        except OSError as why:
            if why.errno == errno.EEXIST:
                continue
            raise
        with os.fdopen(handle, "wb") as fh:
            fh.write(data)
        return path
    raise OSError("no free file name for %s%s in %s" % (stem, extension, folder))


def write(answer, folder, now=None):
    """Write the run's workbook and page into `folder`, made if missing, and
    return their two paths - [xlsx, html]. Never writes over a file."""
    if not os.path.isdir(folder):
        os.makedirs(folder)
    stem = "%s - %s - %s" % (_safe(answer.get("name") or "checklist"),
                             _safe(answer.get("document") or "no model"),
                             _stamp(now, "%Y-%m-%d %H%M"))
    book = _create(folder, stem, ".xlsx", xlsx(answer, now))
    page = _create(folder, stem, ".html", html(answer, now).encode("utf-8"))
    return [book, page]
