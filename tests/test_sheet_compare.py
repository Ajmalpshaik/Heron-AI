# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The comparison of an edited parameter file with the file as exported
(brain/heron_sheet_compare.py) - OPEN-QUESTIONS Q-87, decided 2026-11-15.
Runs without Revit and with the standard library only.

    python tests/test_sheet_compare.py

WHAT IT PROVES
  1. A file written exactly as EXPORT_PARAMETERS_TO_CSV writes one, against
     an edited CSV: the changed cells are found, with id, parameter, old and
     new; a cell left as exported is not sent, nor is a number Excel only
     re-wrote (0.50 as 0.5, 007 as 7, 1.23457E+11) or a rounded value
     nobody edited; a cell emptied on purpose is listed apart and kept out
     of the small file.
  2. Ids: one on two rows, one not in the export, one typed over (named as
     the row it was), a row with no id, rows deleted.
  3. Headers: renamed in place, renamed in case, added, deleted, used twice;
     the ElementId column found by its header when it was moved, and the
     edited file refused without it.
  4. An edited .xlsx - written here with zipfile as Excel lays one out - is
     read through heron_sheet_list's own reader, and the module has no
     reader of its own: an Excel error and a number shown through a format
     are held back, not sent.
  5. THE SMALL FILE IS WHAT SET_PARAMETER_VALUES_BY_ID READS. Its bytes - a
     byte-order mark, commas, CR LF, quotes only where needed - and what the
     card's own parsing makes of it, transliterated line for line from the
     card's C# and pinned to that C# by its text: the ElementId column, the
     changed columns only, each changed cell exactly, every other cell blank
     so blankCells=skip writes nothing else, and no blanked cell in it.
  6. Encoding: a plain-CSV edit saved in Windows-1252 arrives as UTF-8; a
     cell holding a character the encoding lost is held back; an Arabic name
     left alone is not sent.
  7. A column sorted on its own is held back - three or more cells that are
     only the export's values moved - and sent with --moved-on-purpose; a
     swap of two is sent.
  8. Nothing is written when nothing changed, over either file, or inside
     the Heron folder; two files given the wrong way round are warned of;
     the command line writes the file and exits 0, and 1 when refused.
  9. The method parameters-csv-round-trip names the command, the kept
     export and blankCells=skip for the small file.

WHAT IT CANNOT DO: say that Revit writes the small file's cells. That is
NEEDS-CHECKING Group NJ, NJ13, on a real model.
"""

from __future__ import print_function

import collections
import inspect
import io
import os
import shutil
import sys
import tempfile
import time
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_sheet_list as L                                  # noqa: E402

try:
    import heron_sheet_compare as C                           # noqa: E402
    MISSING = None
except ImportError as why:                                    # the check, not a crash
    C, MISSING = None, why

EXPORT_CS = os.path.join(ROOT, "brain", "fragments", "export-parameters-to-csv", "impl", "any",
                         "fragment.cs")
RETURN_CS = os.path.join(ROOT, "brain", "fragments", "set-parameter-values-by-id", "impl", "any",
                         "fragment.cs")
METHOD = os.path.join(ROOT, "brain", "skills", "parameters-csv-round-trip.yaml")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


# ---------------------------------------------------------------------------
# the two cards' formats
# ---------------------------------------------------------------------------

def export_csv(path, headers, rows):
    """A file as EXPORT_PARAMETERS_TO_CSV writes it: `ElementId` and the
    names, a cell quoted only when it holds a comma, a quote or a line feed,
    a quote doubled, lines joined CR LF (File.WriteAllLines on Windows), UTF-8
    with no byte-order mark."""
    def quoted(v):
        if "," not in v and '"' not in v and "\n" not in v:
            return v
        return '"' + v.replace('"', '""') + '"'
    lines = [",".join(["ElementId"] + [quoted(h) for h in headers])]
    lines.extend(",".join([ident] + [quoted(v) for v in values]) for ident, values in rows)
    with io.open(path, "wb") as handle:
        handle.write(("\r\n".join(lines) + "\r\n").encode("utf-8"))
    return path


def edited_csv(path, text, encoding="utf-8"):
    with io.open(path, "wb") as handle:
        handle.write(text.encode(encoding))
    # Saved after the export, as an edit is.
    later = time.time() + 5
    os.utime(path, (later, later))
    return path


def card_reads(path):
    """What SET_PARAMETER_VALUES_BY_ID makes of a csvPath file - its C#
    ("a CSV file", fragment.cs), statement for statement: File.ReadAllText
    (a byte-order mark names the encoding, none means UTF-8), every leading
    U+FEFF trimmed, then a quoted cell to its closing quote with a doubled
    quote kept as one, a comma ends a cell, a carriage return outside quotes
    is dropped, a line feed ends a row, a row with nothing in it is dropped.
    Returns (heading trimmed, [(id trimmed, {heading: cell as it stands})])."""
    with io.open(path, "rb") as handle:
        raw = handle.read()
    if raw.startswith(b"\xef\xbb\xbf"):
        text = raw[3:].decode("utf-8")
    elif raw.startswith(b"\xff\xfe") or raw.startswith(b"\xfe\xff"):
        text = raw.decode("utf-16")
    else:
        text = raw.decode("utf-8", "replace")
    body = text.lstrip(u"﻿")
    table, cells, cell, quoted, at = [], [], [], False, 0
    while at < len(body):
        c = body[at]
        if quoted:
            if c == '"':
                if at + 1 < len(body) and body[at + 1] == '"':
                    cell.append('"')
                    at += 1
                else:
                    quoted = False
            else:
                cell.append(c)
        elif c == '"':
            quoted = True
        elif c == ",":
            cells.append("".join(cell))
            cell = []
        elif c == "\r":
            pass
        elif c == "\n":
            cells.append("".join(cell))
            cell = []
            if any(x.strip() for x in cells):
                table.append(cells)
            cells = []
        else:
            cell.append(c)
        at += 1
    cells.append("".join(cell))
    if any(x.strip() for x in cells):
        table.append(cells)
    heading = [h.strip() for h in table[0]] if table else []
    rows = []
    for line in table[1:]:
        rows.append((line[0].strip(), dict((heading[i], line[i] if i < len(line) else "")
                                           for i in range(1, len(heading)))))
    return heading, rows


# ---------------------------------------------------------------------------
# a real .xlsx
# ---------------------------------------------------------------------------

MAIN = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
PKG = "http://schemas.openxmlformats.org/package/2006/relationships"


def write_xlsx(path, sheet_xml, shared, styles_xf, custom_formats=None):
    book = zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED)
    book.writestr("[Content_Types].xml",
                  '<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.'
                  'org/package/2006/content-types"><Default Extension="xml" ContentType="application/'
                  'xml"/></Types>')
    book.writestr("_rels/.rels", '<?xml version="1.0"?><Relationships xmlns="%s"><Relationship '
                  'Id="rId1" Type="%s/officeDocument" Target="xl/workbook.xml"/></Relationships>'
                  % (PKG, REL))
    book.writestr("xl/workbook.xml", '<?xml version="1.0"?><workbook xmlns="%s" xmlns:r="%s"><sheets>'
                  '<sheet name="Walls" sheetId="1" r:id="rId1"/></sheets></workbook>' % (MAIN, REL))
    book.writestr("xl/_rels/workbook.xml.rels",
                  '<?xml version="1.0"?><Relationships xmlns="%s">'
                  '<Relationship Id="rId1" Type="%s/worksheet" Target="worksheets/sheet1.xml"/>'
                  '<Relationship Id="rId2" Type="%s/sharedStrings" Target="sharedStrings.xml"/>'
                  '<Relationship Id="rId3" Type="%s/styles" Target="styles.xml"/></Relationships>'
                  % (PKG, REL, REL, REL))
    book.writestr("xl/sharedStrings.xml", '<?xml version="1.0"?><sst xmlns="%s">%s</sst>'
                  % (MAIN, "".join("<si><t>%s</t></si>" % t for t in shared)))
    fmts = "".join('<numFmt numFmtId="%d" formatCode="%s"/>' % (i, c)
                   for i, c in (custom_formats or {}).items())
    book.writestr("xl/styles.xml", '<?xml version="1.0"?><styleSheet xmlns="%s">%s<cellXfs>%s'
                  '</cellXfs></styleSheet>' % (MAIN, ("<numFmts>%s</numFmts>" % fmts) if fmts else "",
                                               "".join('<xf numFmtId="%d"/>' % f for f in styles_xf)))
    book.writestr("xl/worksheets/sheet1.xml", '<?xml version="1.0"?><worksheet xmlns="%s"><sheetData>'
                  '%s</sheetData></worksheet>' % (MAIN, sheet_xml))
    book.close()
    later = time.time() + 5
    os.utime(path, (later, later))
    return path


def sx(ref, index):
    return '<c r="%s" t="s"><v>%d</v></c>' % (ref, index)


def nx(ref, value, style=0):
    return '<c r="%s" s="%d"><v>%s</v></c>' % (ref, style, value)


# ---------------------------------------------------------------------------
# sections
# ---------------------------------------------------------------------------

def by_cell(cells):
    return dict(((c["id"], c["parameter"]), c) for c in cells)


def section_changed(folder):
    print("1. changed cells found; unchanged, re-written numbers and rounded values not sent")
    exported = export_csv(os.path.join(folder, "walls.csv"), ["Mark", "Comments", "Width", "Height"], [
        ("101", ["W-01", "Lobby, east", "0.50", "2600"]),        # 2600: shown rounded, held 2595.5
        ("102", ["W-02", "", "007", "3000 mm"]),
        ("103", ["W-03", "Old note", "1200 mm", "123456789012"]),
        ("104", ["W-04", 'Say "hi"', "10", "2700"]),
    ])
    edited = edited_csv(os.path.join(folder, "walls edited.csv"),
                        u"ElementId,Mark,Comments,Width,Height\r\n"
                        u"101,W-101,\"Lobby, east\",0.5,2600\r\n"
                        u"102,W-02,Filled in,7,3000.0 mm\r\n"
                        u"103,W-03,,1200 mm,1.23457E+11\r\n"
                        u"104,W-04,\"Say \"\"hello\"\"\",10,2750\r\n")
    got = C.compare(exported, edited)
    check(got["ok"], "the two files are compared: %s" % got.get("refused"))
    changed = by_cell(got["changed"])
    check(sorted(changed) == [("101", "Mark"), ("102", "Comments"), ("104", "Comments"), ("104", "Height")],
          "exactly the edited cells are found: %s" % sorted(changed))
    mark = changed.get(("101", "Mark"), {})
    check(mark.get("old") == "W-01" and mark.get("new") == "W-101" and mark.get("row") == 2,
          "each with its id, parameter, old value and new, and its row")
    check(changed.get(("102", "Comments"), {}).get("old") == "",
          "a cell blank in the export and filled in is a change")
    check(changed.get(("104", "Comments"), {}).get("new") == 'Say "hello"',
          "a quoted cell with doubled quotes reads as typed")
    check(("101", "Height") not in changed, "2600 left as exported - a value shown rounded - is not sent")
    same = by_cell(got["sameNumber"])
    check(sorted(same) == [("101", "Width"), ("102", "Height"), ("102", "Width"), ("103", "Height")],
          "0.50 as 0.5, 007 as 7, 3000 mm as 3000.0 mm, 123456789012 as 1.23457E+11 are the same "
          "number, listed apart: %s" % sorted(same))
    blanked = by_cell(got["blanked"])
    check(list(blanked) == [("103", "Comments")] and blanked[("103", "Comments")]["old"] == "Old note",
          "a cell emptied on purpose is listed apart, with what it held")
    check(("103", "Comments") not in changed, "and it is not a change to send")
    check(got["parameters"] == ["Mark", "Comments", "Width", "Height"], "every column compared, in order")
    text = C.report(got)
    check("CHANGED - 4 cell(s) on 3 element(s)" in text, "the report says how many cells and elements")
    check("element 101  Mark: \"W-01\" -> \"W-101\"" in text, "and shows old and new")
    check("BLANKED ON PURPOSE (1)" in text and "Old note" in text and "WRITE_ELEMENT_PARAMETERS clear=true" in text,
          "the blanked cell is asked about apart, naming Revit's own clear")
    check("Nothing in Revit was read or touched" in text, "it says Revit was not touched")
    check(C.same_number("2600", "2600.0") and not C.same_number("2600", "2610")
          and not C.same_number("10 mm", "10") and not C.same_number("1-2", "2026-01-02")
          and C.same_number("123456789012", "1.2E+11") and not C.same_number("123456789012", "1.3E+11"),
          "same_number: a unit must match, a date is not a number, E-notation only as close as it shows")
    return got


def section_ids(folder):
    print("2. ids changed, doubled, added, missing")
    exported = export_csv(os.path.join(folder, "doors.csv"), ["Mark", "Comments"], [
        ("201", ["D-01", "a"]), ("202", ["D-02", "b"]), ("203", ["D-03", "c"]),
        ("204", ["D-04", "d"]), ("205", ["D-05", "e"]),
    ])
    edited = edited_csv(os.path.join(folder, "doors edited.csv"),
                        u"ElementId,Mark,Comments\r\n"
                        u"201,D-101,a\r\n"
                        u"202,D-02,b2\r\n"
                        u"202,D-02,b3\r\n"          # 202 twice
                        u"2033,D-03,c\r\n"          # 203's row, its id typed over
                        u"999,D-99,new\r\n"         # a row added
                        u",D-06,orphan\r\n")        # no id; 204 and 205 deleted
    got = C.compare(exported, edited)
    check(got["ok"], "compared")
    check([(c["id"], c["parameter"]) for c in got["changed"]] == [("201", "Mark")],
          "only the matched, single row's change is sent: %s"
          % [(c["id"], c["parameter"]) for c in got["changed"]])
    check(got["ids"]["doubled"] == [{"id": "202", "rows": [3, 4]}], "an id on two rows: neither is sent")
    added = dict((a["id"], a) for a in got["ids"]["added"])
    check(sorted(added) == ["2033", "999"], "ids not in the export are named: %s" % sorted(added))
    check(added.get("2033", {}).get("looksLike") == "203" and added.get("999", {}).get("looksLike") is None,
          "a row whose id was typed over is named as 203's row; a new row is not")
    check(got["ids"]["blank"] == [7], "a row with no id is named")
    check(sorted(got["ids"]["missing"]) == ["203", "204", "205"], "rows not in the edited file: %s"
          % got["ids"]["missing"])
    text = C.report(got)
    check("element 202 is on rows 3 and 4 - neither is sent" in text, "the report says so for 202")
    check("its other cells are element 203's" in text, "and names 203 for the id typed over")


def section_headers(folder):
    print("3. headers renamed, added, deleted, doubled; the id column")
    exported = export_csv(os.path.join(folder, "rooms.csv"),
                          ["Name", "Number", "Comments", "Department", "Finish"], [
                              ("301", ["Lobby", "1", "x", "Admin", "Tile"]),
                              ("302", ["Store", "2", "y", "Ops", "Paint"])])
    edited = edited_csv(os.path.join(folder, "rooms edited.csv"),
                        u"Number,ElementId,name,Notes,Department,Occupant,Occupant\r\n"
                        u"1,301,Lobby 2,x2,Admin 2,A,B\r\n"
                        u"2,302,Store,y,Ops,C,D\r\n")
    got = C.compare(exported, edited)
    check(got["ok"], "compared, ElementId found in column B by its header")
    check(any("column B, not A" in n for n in got["notes"]), "and the move is noted")
    renamed = dict((r["from"], r["to"]) for r in got["headers"]["renamed"])
    check(renamed == {"Name": "name", "Comments": "Notes"},
          "a header in another case, and one renamed in its own place: %s" % renamed)
    check(got["headers"]["deleted"] == ["Finish"], "a deleted column is named: %s" % got["headers"]["deleted"])
    check(got["headers"]["doubled"] == ["Occupant"] and got["headers"]["added"] == [],
          "a header used twice is named, and is not also an added column")
    check([(c["id"], c["parameter"]) for c in got["changed"]] == [("301", "Department")],
          "nothing from a renamed column is sent - only Department, matched by name: %s"
          % [(c["id"], c["parameter"]) for c in got["changed"]])
    text = C.report(got)
    check("the column \"Notes\" (column D) was \"Comments\" in the export - renamed" in text,
          "the report names the rename and the column")
    added = edited_csv(os.path.join(folder, "rooms added.csv"),
                       u"ElementId,Name,Number,Comments,Department,Finish,Fire Rating\r\n"
                       u"301,Lobby,1,x,Admin,Tile,60\r\n302,Store,2,y,Ops,Paint,\r\n")
    got = C.compare(exported, added)
    check(got["headers"]["added"] == ["Fire Rating"] and not got["changed"],
          "a column not in the export is added, and nothing in it is sent")
    no_id = edited_csv(os.path.join(folder, "rooms no id.csv"), u"Id,Name\r\n301,Lobby\r\n")
    got = C.compare(exported, no_id)
    check(not got["ok"] and "no column headed \"ElementId\"" in got["refused"],
          "an edited file with no ElementId column is refused: %s" % got.get("refused"))


def section_xlsx(folder):
    print("4. an edited .xlsx, read by heron_sheet_list's reader")
    exported = export_csv(os.path.join(folder, "pipes.csv"), ["Mark", "Comments", "Size", "Count"], [
        ("401", ["P-01", "keep", "50", "007"]),
        ("402", ["P-02", "old", "65", "3"]),
        ("403", ["P-03", "x", "80", "4"]),
    ])
    shared = ["ElementId", "Mark", "Comments", "Size", "Count", "P-01", "keep", "P-202", "new note",
              "P-03", "x"]
    # style 0 General, 1 a custom "000" format (a number shown with zeros).
    rows = [
        '<row r="1">%s%s%s%s%s</row>' % (sx("A1", 0), sx("B1", 1), sx("C1", 2), sx("D1", 3), sx("E1", 4)),
        # 401 unchanged: 50 a number, 007 kept by the "000" format as 7.
        '<row r="2">%s%s%s%s%s</row>' % (nx("A2", "401"), sx("B2", 5), sx("C2", 6), nx("D2", "50"),
                                         nx("E2", "7", 1)),
        # 402: Mark and Comments changed; Size an Excel error; Count 8 shown as 008.
        '<row r="3">%s%s%s<c r="D3" t="e"><v>#N/A</v></c>%s</row>'
        % (nx("A3", "402"), sx("B3", 7), sx("C3", 8), nx("E3", "8", 1)),
        # 403 unchanged, a hidden row.
        '<row r="4" hidden="1">%s%s%s%s%s</row>' % (nx("A4", "403"), sx("B4", 9), sx("C4", 10),
                                                    nx("D4", "80"), nx("E4", "4")),
    ]
    edited = write_xlsx(os.path.join(folder, "pipes edited.xlsx"), "".join(rows), shared, [0, 164],
                        {164: "000"})
    got = C.compare(exported, edited)
    check(got["ok"] and got["kind"] == "xlsx" and got["sheet"] == "Walls", "the .xlsx is read: %s"
          % got.get("refused"))
    changed = by_cell(got["changed"])
    check(sorted(changed) == [("402", "Comments"), ("402", "Mark")]
          and changed[("402", "Mark")]["new"] == "P-202",
          "shared strings compared; the edited cells found: %s" % sorted(changed))
    check(("401", "Size") not in changed and ("401", "Count") not in changed,
          "a stored number equal to the export's (50; 007 kept as 7) is not sent")
    held = by_cell(got["heldBack"])
    check(("402", "Size") in held and "error" in held[("402", "Size")]["why"]
          and ("402", "Size") not in by_cell(got["blanked"]),
          "an Excel error is held back - never sent, never taken for a blank")
    check(("402", "Count") in held and "000" in held[("402", "Count")]["why"],
          "a number Excel shows through a format (8 as 008) is held back, the format named")
    source = inspect.getsource(C)
    check(C.SHEETS is L and "read_grid(" in source and "zipfile" not in source
          and "iterparse" not in source and "csv.reader" not in source,
          "the module reads through heron_sheet_list.read_grid and has no reader of its own")
    check("read_grid(" in inspect.getsource(L.read), "and heron_sheet_list.read stands on the same gate")
    refused = os.path.join(folder, "old.xls")
    with io.open(refused, "wb") as handle:
        handle.write(b"\xd0\xcf\x11\xe0" + b"\x00" * 40)
    got = C.compare(exported, refused)
    check(not got["ok"] and got["refused"].startswith("The edited file: 'old.xls' was not read")
          and "Excel 97-2003" in got["refused"] and "Save As" in got["refused"],
          "an .xls is refused by the shared gate, in words: %s" % got.get("refused"))
    odd = os.path.join(folder, "notes.txt")
    with io.open(odd, "w", encoding="utf-8") as handle:
        handle.write(u"x")
    got = C.compare(exported, odd)
    check(not got["ok"] and "Heron reads a parameter file from .xlsx" in got["refused"],
          "a .txt is refused naming what is read")


def section_small_file(folder, first):
    print("5. the small file is what SET_PARAMETER_VALUES_BY_ID reads")
    written = C.write_changes(first)
    check(written["ok"] and written["path"].endswith("walls edited-changed-cells.csv")
          and os.path.dirname(written["path"]) == os.path.dirname(first["edited"]),
          "written beside the edited file: %s %s" % (written.get("path"), written.get("refused") or ""))
    with io.open(written["path"], "rb") as handle:
        raw = handle.read()
    check(raw.startswith(b"\xef\xbb\xbf"), "UTF-8 with a byte-order mark")
    check(raw.count(b"\r\n") == 4 and b"\n" not in raw.replace(b"\r\n", b""),
          "lines end CR LF: a header and three elements")
    check(raw[3:].split(b"\r\n")[0] == b"ElementId,Mark,Comments,Height",
          "the header: ElementId, then only the columns with a change, in the export's order: %r"
          % raw[3:].split(b"\r\n")[0])
    check(b'"Say ""hello"""' in raw, "a quote inside a cell is quoted and doubled, as the card reads")
    heading, rows = card_reads(written["path"])
    check(heading == ["ElementId", "Mark", "Comments", "Height"], "the card's parsing reads that heading")
    check([r[0] for r in rows] == ["101", "102", "104"], "and a row for each element with a change only")
    sent = {}
    for ident, cells in rows:
        for header, value in cells.items():
            if value.strip():
                sent[(ident, header)] = value
    wanted = dict(((c["id"], c["parameter"]), c["new"]) for c in first["changed"])
    check(sent == wanted, "every non-blank cell the card reads is a changed cell, exactly: %s" % sent)
    check(("103", "Comments") not in sent and all(i != "103" for i, _c in rows),
          "the blanked cell, and its element, are not in it")
    check(all(not v.strip() for _i, cells in rows for (h, v) in cells.items()
              if (_i, h) not in wanted),
          "every other cell is blank - with blankCells=skip the card writes nothing else")

    # Commas, line breaks, spaces at the ends and non-Latin letters survive.
    exported = export_csv(os.path.join(folder, "notes.csv"), ["Comments", "Name"], [
        ("501", ["a", u"مخطط"]), ("502", ["b", "Room"])])
    edited = edited_csv(os.path.join(folder, "notes edited.csv"),
                        u"ElementId,Comments,Name\r\n"
                        u"501,\"one, two\r\nthree\",مخطط 2\r\n"
                        u"502, b ,Room\r\n")
    got = C.compare(exported, edited)
    C.write_changes(got)
    heading, rows = card_reads(C.default_out(edited))
    cells = dict(rows)
    check(cells.get("501", {}).get("Comments") == "one, two\r\nthree"
          and cells.get("501", {}).get("Name") == u"مخطط 2",
          "a comma, a line break and Arabic reach the card exactly as edited")
    check("502" not in cells and not [c for c in got["changed"] if c["id"] == "502"],
          "a cell that differs only by spaces at its ends is not sent")

    # What the transliteration above stands on, read off the card's own C#.
    with io.open(RETURN_CS, encoding="utf-8") as handle:
        card = handle.read()
    for anchor in ("System.IO.File.ReadAllText(csvPath.Trim())",
                   "csvText.TrimStart((char)0xFEFF)",
                   "var quoteMark = (char)34;",
                   "if (at + 1 < body.Length && body[at + 1] == quoteMark) { cellText.Append(quoteMark); at++; }",
                   "else if (c == ',') { cellsNow.Add(cellText.ToString()); cellText.Length = 0; }",
                   "else if (c == carriage) { }",
                   "if (cellsNow.Any(x => x.Trim().Length > 0)) table.Add(cellsNow);",
                   "var heading = table[0].Select(h => h.Trim()).ToList();",
                   "var rowId = line[0].Trim();",
                   "var text = candidate.Id.ToString();",
                   "if (blankHow == \"skip\") { blankCellsSkipped++; continue; }"):
        check(anchor in card, "the card's C# still reads so: %s" % anchor[:70])
    with io.open(EXPORT_CS, encoding="utf-8") as handle:
        export = handle.read()
    for anchor in ('header.Add("ElementId");',
                   "if (v.IndexOf(',') < 0 && v.IndexOf('\"') < 0 && v.IndexOf('\\n') < 0) return v;",
                   "System.IO.File.WriteAllLines(csvPath, lines.ToArray());"):
        check(anchor in export, "the export's C# still writes so: %s" % anchor[:70])


def section_encoding(folder):
    print("6. encoding")
    exported = export_csv(os.path.join(folder, "accent.csv"), ["Comments"], [
        ("601", [u"Café"]), ("602", [u"مخطط"]), ("603", ["plain"])])
    ansi = edited_csv(os.path.join(folder, "accent ansi.csv"),
                      u"ElementId,Comments\r\n601,Café 2\r\n603,plain 2\r\n", encoding="cp1252")
    got = C.compare(exported, ansi)
    check(any("Windows-1252" in n for n in got["notes"]), "a plain CSV's code page is said, from the reader")
    changed = by_cell(got["changed"])
    check(changed.get(("601", "Comments"), {}).get("new") == u"Café 2", "and its accent read right")
    C.write_changes(got)
    _h, rows = card_reads(C.default_out(ansi))
    check(dict(rows).get("601", {}).get("Comments") == u"Café 2",
          "the small file carries it as UTF-8 - the card reads Café 2 exactly")
    lost = edited_csv(os.path.join(folder, "accent lost.csv"),
                      u"ElementId,Comments\r\n601,Caf� 2\r\n602,مخطط\r\n")
    got = C.compare(exported, lost)
    held = by_cell(got["heldBack"])
    check(("601", "Comments") in held and "encoding" in held[("601", "Comments")]["why"]
          and not got["changed"], "a character the encoding lost is held back, not sent")
    check(("602", "Comments") not in by_cell(got["changed"]), "an Arabic name left alone is not sent")


def section_moved(folder):
    print("7. a column sorted on its own")
    exported = export_csv(os.path.join(folder, "tags.csv"), ["Mark", "Comments"], [
        ("701", ["A", "1"]), ("702", ["B", "2"]), ("703", ["C", "3"]), ("704", ["D", "4"])])
    sorted_alone = edited_csv(os.path.join(folder, "tags sorted.csv"),
                              u"ElementId,Mark,Comments\r\n701,D,1\r\n702,C,2\r\n703,B,3\r\n704,A,4x\r\n")
    got = C.compare(exported, sorted_alone)
    check([(c["id"], c["parameter"]) for c in got["changed"]] == [("704", "Comments")],
          "the moved Mark column is not sent; the real edit is")
    check(len([c for c in got["heldBack"] if c["parameter"] == "Mark"]) == 4
          and "sorted on its own" in got["heldBack"][0]["why"], "its four cells are held back, saying why")
    got = C.compare(exported, sorted_alone, moved_on_purpose=True)
    check(len(got["changed"]) == 5, "with --moved-on-purpose they are sent")
    swap = edited_csv(os.path.join(folder, "tags swap.csv"),
                      u"ElementId,Mark,Comments\r\n701,B,1\r\n702,A,2\r\n703,C,3\r\n704,D,4\r\n")
    got = C.compare(exported, swap)
    check(len(got["changed"]) == 2 and not got["heldBack"], "a swap of two is an edit, and is sent")


def section_safety(folder):
    print("8. nothing written where it must not be; the command line")
    exported = export_csv(os.path.join(folder, "same.csv"), ["Mark"], [("801", ["M"])])
    unchanged = edited_csv(os.path.join(folder, "same edited.csv"), u"ElementId,Mark\r\n801,M\r\n")
    got = C.compare(exported, unchanged)
    done = C.write_changes(got)
    check(got["ok"] and not got["changed"] and not done["ok"] and not os.path.exists(C.default_out(unchanged)),
          "nothing changed: no file is written")
    check("CHANGED - nothing" in C.report(got), "and the report says nothing changed")
    edited = edited_csv(os.path.join(folder, "one edited.csv"), u"ElementId,Mark\r\n801,N\r\n")
    got = C.compare(exported, edited)
    check(not C.write_changes(got, edited)["ok"] and not C.write_changes(got, exported)["ok"],
          "never written over either file compared")
    inside = C.write_changes(got, os.path.join(ROOT, "docs", "changes.csv"))
    check(not inside["ok"] and "Heron folder" in inside["refused"]
          and not os.path.exists(os.path.join(ROOT, "docs", "changes.csv")),
          "never written inside the Heron folder")
    check(not C.write_changes(got, os.path.join(folder, "x.txt"))["ok"], "only a .csv is written")
    backwards = C.compare(edited, os.path.join(folder, "same.csv"))
    check(any("right way round" in w for w in backwards["warnings"])
          and "CHECK:" in C.report(backwards),
          "the export saved after the edited file is warned of")
    check(not got["warnings"], "and the right way round is not")
    same = C.compare(exported, exported)
    check(not same["ok"] and "same file" in same["refused"], "one file given twice is refused")
    out = os.path.join(folder, "cli", "mine.csv")
    saved = sys.stdout
    sys.stdout = io.StringIO()
    try:
        code = C.main([exported, edited, "--out", out])
        printed = sys.stdout.getvalue()
        refused = C.main([exported, os.path.join(folder, "not-here.csv")])
    finally:
        sys.stdout = saved
    check(code == 0 and os.path.isfile(out) and "Written to '%s'" % out in printed,
          "the command writes the file where --out says, and exits 0")
    check("blankCells=skip" in printed and "with clear, every one of them would be emptied" in printed,
          "and says to bring it back with blankCells=skip, never clear")
    check(refused == 1, "a file that is not there exits 1")
    source = inspect.getsource(C)
    check("heron_bridge" not in source and "import socket" not in source,
          "the module reaches no bridge - nothing is sent to Revit from here")


def section_method():
    print("9. the method names the command")
    with io.open(METHOD, encoding="utf-8") as handle:
        method = handle.read()
    check("python brain/heron_sheet_compare.py" in method, "the method gives the command to run")
    check("KEEP THE FILE AS EXPORTED" in method, "it tells the modeller to keep the exported file")
    check("blankCells skip" in method or "blankCells=skip" in method, "and blankCells skip for the small file")
    check("Delete every column and every row that was not edited" not in method,
          "the hand rule the comparison replaces is gone")


def main():
    if C is None:
        check(False, "brain/heron_sheet_compare.py imports: %s" % MISSING)
        print()
        print("%d FAILED" % len(FAILURES))
        return 1
    folder = tempfile.mkdtemp(prefix="heron-sheet-compare-")
    try:
        first = section_changed(folder)
        section_ids(folder)
        section_headers(folder)
        section_xlsx(folder)
        section_small_file(folder, first)
        section_encoding(folder)
        section_moved(folder)
        section_safety(folder)
        section_method()
    finally:
        shutil.rmtree(folder, ignore_errors=True)
    print()
    if FAILURES:
        print("%d FAILED" % len(FAILURES))
        return 1
    print("All passed. That is the comparison agreeing with these files and the small file "
          "agreeing with the card's code - not evidence that Revit writes it (NEEDS-CHECKING "
          "Group NJ, NJ13).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
