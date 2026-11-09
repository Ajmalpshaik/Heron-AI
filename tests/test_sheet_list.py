# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The sheet register reader (brain/heron_sheet_list.py) - docs/48 section 5
item 7, "Sheets from Excel". Runs without Revit and with the standard library
only.

    python tests/test_sheet_list.py

WHAT IT PROVES
  1. A REAL .xlsx, written here with zipfile exactly as Excel lays one out -
     shared strings, an inline string, numbers, a date styled by a built-in
     format and one by a custom format, a boolean, an error cell, a blank row,
     a row with no number, a number twice, a multi-line cell, a second sheet -
     reads to the rows, the problems and the skips it should.
  2. A named sheet is read; a sheet that is not there is refused, naming the
     sheets the workbook has; the 1904 date system lands on the same day.
  3. A CSV with a BOM, ";" between columns and a quoted cell with a line
     break reads the same way; a header on row 3 is found when it is named.
  4. .xls - by its name or by what it holds - .xlsb and .ods are refused in
     words, with how to save the file; a file with no Sheet Number column, a
     file that is not there and two columns that could both be the number are
     refused.
  5. card_value is one line, a ";", "|", "%" or '"' in a cell encoded, and
     decodes back to the same table - the shape the card splits.
  6. chat_answer gives the value exactly and ONCE, and the route the card's
     status allows: the command line and try-once while DRAFT, revit_change
     once PROVEN.
  7-12. THE INDEPENDENT REVIEW'S PROBES (2026-11-09), each a test: a curly
     apostrophe, "$(calc)" and a trailing backslash cannot end or change the
     PowerShell argument; a DRAFT command above a Windows command line is
     refused in words and the split asked for; a Sheet Number shown through
     a number format (000, 0.00) is not passed; a value under no header is
     listed; formatted blank rows after the table are not listed; a hidden
     row is noted; a [h]:mm duration is a number; lists are cut at 20; a big
     part is refused; only .xlsx, .xlsm, .csv and .tsv are opened and a
     refusal quotes nothing of a file; the delimiter comes from the header
     row; more than 1,000 rows is refused.

WHAT IT CANNOT DO: say that Revit makes the sheets. That is NEEDS-CHECKING
Group MY, on a real model.
"""

from __future__ import print_function

import io
import os
import shutil
import sys
import tempfile
import zipfile

try:
    from urllib.parse import unquote
except ImportError:                                   # pragma: no cover
    from urllib import unquote

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_sheet_list as L                                  # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


MAIN = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
PKG = "http://schemas.openxmlformats.org/package/2006/relationships"


def write_xlsx(path, sheets, shared, styles_xf, custom_formats=None, date1904=False):
    """A minimal real workbook. `sheets` is [(name, sheetData XML)]."""
    book = zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED)
    book.writestr("[Content_Types].xml",
                  '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
                  '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">'
                  '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>'
                  '<Default Extension="xml" ContentType="application/xml"/></Types>')
    book.writestr("_rels/.rels",
                  '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="%s">'
                  '<Relationship Id="rId1" Type="%s/officeDocument" Target="xl/workbook.xml"/>'
                  '</Relationships>' % (PKG, REL))
    listed = "".join('<sheet name="%s" sheetId="%d" r:id="rId%d"/>' % (name, i + 1, i + 1)
                     for i, (name, _d) in enumerate(sheets))
    book.writestr("xl/workbook.xml",
                  '<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="%s" xmlns:r="%s">'
                  '<workbookPr%s/><sheets>%s</sheets></workbook>'
                  % (MAIN, REL, ' date1904="1"' if date1904 else "", listed))
    rels = "".join('<Relationship Id="rId%d" Type="%s/worksheet" Target="worksheets/sheet%d.xml"/>'
                   % (i + 1, REL, i + 1) for i in range(len(sheets)))
    rels += ('<Relationship Id="rId90" Type="%s/sharedStrings" Target="sharedStrings.xml"/>'
             '<Relationship Id="rId91" Type="%s/styles" Target="styles.xml"/>' % (REL, REL))
    book.writestr("xl/_rels/workbook.xml.rels",
                  '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="%s">%s</Relationships>'
                  % (PKG, rels))
    sst = "".join("<si>%s</si>" % s for s in shared)
    book.writestr("xl/sharedStrings.xml",
                  '<?xml version="1.0" encoding="UTF-8"?><sst xmlns="%s" count="%d" uniqueCount="%d">%s</sst>'
                  % (MAIN, len(shared), len(shared), sst))
    fmts = "".join('<numFmt numFmtId="%d" formatCode="%s"/>' % (i, code)
                   for i, code in (custom_formats or {}).items())
    xfs = "".join('<xf numFmtId="%d" fontId="0" fillId="0" borderId="0"/>' % f for f in styles_xf)
    book.writestr("xl/styles.xml",
                  '<?xml version="1.0" encoding="UTF-8"?><styleSheet xmlns="%s">%s'
                  '<cellXfs count="%d">%s</cellXfs></styleSheet>'
                  % (MAIN, ("<numFmts>%s</numFmts>" % fmts) if fmts else "", len(styles_xf), xfs))
    for i, (_name, data) in enumerate(sheets):
        book.writestr("xl/worksheets/sheet%d.xml" % (i + 1),
                      '<?xml version="1.0" encoding="UTF-8"?><worksheet xmlns="%s">'
                      '<sheetData>%s</sheetData></worksheet>' % (MAIN, data))
    book.close()


def s(ref, index):
    return '<c r="%s" t="s"><v>%d</v></c>' % (ref, index)


def n(ref, value, style=0):
    return '<c r="%s" s="%d"><v>%s</v></c>' % (ref, style, value)


def register_xlsx(folder, date1904=False):
    shared = ["Sheet Number", "Sheet Name", "Drawn By", "Issue Date", "Checked",
              "Level 1 Plan", "AP", "Level 2 Plan", "<t>Line one</t>", "Sections",
              "JS", "M-104", "Typed Date", "Scale"]
    # Rich text with a phonetic run: only the runs' text counts.
    shared[8] = "<r><t>Ground floor</t></r><r><t xml:space=\"preserve\">\nzone A</t></r><rPh><t>x</t></rPh>"
    shared = ["<t>%s</t>" % t if not t.startswith("<") else t for t in shared]
    # style 0 general, 1 built-in date (14), 2 custom date "dd/mm/yyyy" (164),
    # 3 custom "0.00" (165) - a number, not a date.
    serial_2026_11_09 = 46335
    serial_1904 = serial_2026_11_09 - 1462
    serial = serial_1904 if date1904 else serial_2026_11_09
    rows = [
        '<row r="1">%s%s%s%s%s%s%s</row>' % (s("A1", 0), s("B1", 1), s("C1", 2), s("D1", 3),
                                             s("E1", 4), s("F1", 12), s("G1", 13)),
        # M-101: shared strings, a built-in date, TRUE, a custom date, a 0.00 number.
        '<row r="2"><c r="A2" t="inlineStr"><is><t>M-101</t></is></c>%s%s%s'
        '<c r="E2" t="b"><v>1</v></c>%s%s</row>'
        % (s("B2", 5), s("C2", 6), n("D2", serial, 1), n("F2", serial, 2), n("G2", "50", 3)),
        # M-102: a multi-line rich-text name, FALSE, an error cell.
        '<row r="3"><c r="A3" t="inlineStr"><is><t>M-102</t></is></c>%s%s%s'
        '<c r="E3" t="b"><v>0</v></c><c r="F3" t="e"><v>#N/A</v></c></row>'
        % (s("B3", 8), s("C3", 6), n("D3", serial + 1, 1)),
        # A blank row - cells there but nothing in them.
        '<row r="4"><c r="A4" t="inlineStr"><is><t>  </t></is></c></row>',
        # A row with a name but no number.
        '<row r="5">%s%s</row>' % (s("B5", 9), s("C5", 10)),
        # A number stored as a number: 105 must arrive as "105", not "105.0".
        '<row r="6">%s%s</row>' % (n("A6", "105"), s("B6", 7)),
        # M-104 twice - neither may pass.
        '<row r="7">%s%s</row>' % (s("A7", 11), s("B7", 9)),
        '<row r="8"><c r="A8" t="str"><v>m-104</v></c>%s</row>' % s("B8", 7),
    ]
    path = os.path.join(folder, "sheet list.xlsx")
    write_xlsx(path, [("Register", "".join(rows)),
                      ("Notes", '<row r="1">%s</row>' % s("A1", 9))],
               shared, styles_xf=[0, 14, 164, 165],
               custom_formats={164: "dd/mm/yyyy", 165: "0.00"}, date1904=date1904)
    return path


def section_xlsx(folder):
    print("1. a real .xlsx")
    path = register_xlsx(folder)
    got = L.read(path)
    check(got["ok"], "it is read")
    check(got["sheet"] == "Register" and got["sheets"] == ["Register", "Notes"],
          "the first sheet is read, and both are named")
    check(any("the first, \"Register\", was read" in x for x in got["notes"]),
          "the note says which of two sheets was read")
    roles = dict((c["header"], c["role"]) for c in got["columns"])
    check(roles.get("Sheet Number") == L.NUMBER and roles.get("Sheet Name") == L.NAME,
          "the number and the name are found by their headers")
    check(got["parameters"] == ["Drawn By", "Issue Date", "Checked", "Typed Date", "Scale"],
          "every other column is a parameter, in the file's order")
    by = dict((r["number"], r) for r in got["rows"])
    check(sorted(by) == ["105", "M-101", "M-102"], "three sheets pass: %s" % sorted(by))
    first = by.get("M-101", {"values": {}})
    check(first.get("name") == "Level 1 Plan" and first["values"].get("Drawn By") == "AP",
          "shared strings and an inline string read")
    check(first["values"].get("Issue Date") == "2026-11-09",
          "a date in a built-in date format arrives as an ISO date: %r" % first["values"].get("Issue Date"))
    check(first["values"].get("Typed Date") == "2026-11-09",
          "a date in a custom dd/mm/yyyy format arrives as an ISO date")
    check(first["values"].get("Scale") == "50", "a number in a 0.00 format stays a number, 50")
    check(first["values"].get("Checked") == "TRUE" and by.get("M-102", {"values": {}})["values"].get("Checked") == "FALSE",
          "a boolean reads TRUE or FALSE")
    second = by.get("M-102", {"values": {}, "name": ""})
    check(second.get("name") == "Ground floor zone A",
          "a multi-line rich-text cell is one line, its phonetic run left out: %r" % second.get("name"))
    check(second["values"].get("Issue Date") == "2026-11-10", "the next serial is the next day")
    check(by.get("105", {}).get("number") == "105", "a number stored as a number is 105, not 105.0")
    problems = " | ".join(got["problems"])
    check("line break" in problems and "row 3" in problems, "the line break is listed")
    check("#N/A" in problems, "the error cell is listed")
    check("row 5" in problems and "no sheet number" in problems, "the row with no number is listed")
    check("M-104 is on rows 7 and 8 - neither is passed" in problems,
          "a number twice, capitals ignored, passes neither row")
    check(got["skipped"] == ["row 4: blank"], "the blank row is skipped and said")

    print("2. a named sheet, a missing one, and the 1904 date system")
    named = L.read(path, sheet="notes")
    check(not named["ok"] and "no column is headed Sheet Number" in named["refused"],
          "the sheet named is the one read - Notes has no register in it")
    missing = L.read(path, sheet="Drawings")
    check(not missing["ok"] and "\"Register\", \"Notes\"" in missing["refused"],
          "a sheet that is not there is refused, naming the sheets it has")
    folder1904 = os.path.join(folder, "1904")
    os.makedirs(folder1904)
    mac = L.read(register_xlsx(folder1904, date1904=True))
    first = [r for r in mac["rows"] if r["number"] == "M-101"]
    check(first and first[0]["values"].get("Issue Date") == "2026-11-09",
          "a 1904-system workbook lands on the same day")
    check(L.excel_date(1) == "1900-01-01" and L.excel_date(61) == "1900-03-01"
          and L.excel_date(60) is None and L.excel_date(45000.5) == "2023-03-15 12:00",
          "Excel's serials: day 1, the day after its false 29 February, a time")


def section_csv(folder):
    print("3. a CSV - a BOM, ';', a quoted line break, a header lower down")
    path = os.path.join(folder, "register.csv")
    text = (u"Sheet No.;Drawing Title;Title Block;Drawn By;Issue Date\r\n"
            u"E-101;\"Lighting\nLevel 1\";A1: Heron;AP;09/11/2026\r\n"
            u";;;;\r\n"
            u"E-102;Power, Level 1;;AP;\r\n"
            u"E-101;Again;;;\r\n"
            u"E-103;Café réseau;;JS;\r\n")
    with io.open(path, "wb") as handle:
        handle.write(b"\xef\xbb\xbf" + text.encode("utf-8"))
    got = L.read(path)
    check(got["ok"] and got["kind"] == "csv", "it is read as a CSV")
    check(any("\";\"" in x for x in got["notes"]), "the note says the columns are separated by ;")
    check([c["role"] for c in got["columns"]][:3] == [L.NUMBER, L.NAME, L.TITLE_BLOCK],
          "Sheet No. and Drawing Title are the number and the name; Title Block is found")
    check(got["columns"][0]["header"] == "Sheet No.", "the header is kept as written")
    by = dict((r["number"], r) for r in got["rows"])
    check(sorted(by) == ["E-102", "E-103"], "E-101 twice passes neither: %s" % sorted(by))
    check(by.get("E-102", {}).get("name") == "Power, Level 1", "a comma inside a ;-separated cell stays")
    check(by.get("E-103", {}).get("name") == u"Café réseau", "UTF-8 with a BOM keeps accents")
    check(got["skipped"] == ["row 3: blank"], "the blank row is skipped")
    check(got["titleBlockColumn"], "the Title Block column is said")

    ansi = os.path.join(folder, "ansi.csv")
    with io.open(ansi, "wb") as handle:
        handle.write(u"Number,Name\r\nA-1,Café\r\n".encode("cp1252"))
    got = L.read(ansi)
    check(got["ok"] and got["rows"][0]["name"] == u"Café"
          and any("Windows-1252" in x for x in got["notes"]),
          "a Windows 'CSV' is read as Windows-1252, and said")

    titled = os.path.join(folder, "titled.csv")
    with io.open(titled, "w", encoding="utf-8", newline="") as handle:
        handle.write(u"DRAWING REGISTER,,\r\n,,\r\nNumber,Name,Discipline\r\nM-1,Plan,Mech\r\n")
    got = L.read(titled)
    check(not got["ok"] and "give the row they are on" in got["refused"],
          "a title above the table is refused, asking which row the headers are on")
    got = L.read(titled, header_row=3)
    check(got["ok"] and got["rows"][0]["values"] == {"Discipline": "Mech"},
          "with header_row 3 the table is read")


def section_refused(folder):
    print("4. refused in words")
    for name, magic in (("old.xls", b"\xd0\xcf\x11\xe0\xa1\xb1\x1a\xe1"),
                        ("book.xlsb", b"PK\x03\x04"), ("calc.ods", b"PK\x03\x04")):
        path = os.path.join(folder, name)
        with io.open(path, "wb") as handle:
            handle.write(magic + b"\x00" * 64)
        got = L.read(path)
        check(not got["ok"] and "Save As" in got["refused"], "%s is refused with how to save it" % name)
    disguised = os.path.join(folder, "really-old.xlsx")
    with io.open(disguised, "wb") as handle:
        handle.write(b"\xd0\xcf\x11\xe0\xa1\xb1\x1a\xe1" + b"\x00" * 64)
    got = L.read(disguised)
    check(not got["ok"] and "old binary Excel file" in got["refused"],
          "an old .xls renamed .xlsx is refused by what it holds")
    nothing = L.read(os.path.join(folder, "not-here.csv"))
    check(not nothing["ok"] and "there is no file" in nothing["refused"], "a missing file is said")
    plain = os.path.join(folder, "no-number.csv")
    with io.open(plain, "w", encoding="utf-8") as handle:
        handle.write(u"Title,Drawn By\nPlan,AP\n")
    got = L.read(plain)
    check(not got["ok"] and "\"Title\", \"Drawn By\"" in got["refused"],
          "no Sheet Number column is refused, naming the headers there")
    both = os.path.join(folder, "both.csv")
    with io.open(both, "w", encoding="utf-8") as handle:
        handle.write(u"Sheet Number,Drawing Number,Name\nA,B,C\n")
    got = L.read(both)
    check(not got["ok"] and "two columns could be the sheet number" in got["refused"],
          "two columns that could be the number are refused, not guessed")


def section_value(folder):
    print("5. the value the card takes")
    path = os.path.join(folder, "awkward.csv")
    with io.open(path, "w", encoding="utf-8", newline="") as handle:
        handle.write(u'Sheet Number,Sheet Name,Notes,Empty\r\n'
                     u'A-1,"Plan; Zone | A",50% "done",\r\n'
                     u'A-2,,,\r\n')
    got = L.read(path)
    value = L.card_value(got)
    check("\n" not in value and value.count(";") == 2, "one line, a ; between rows only")
    check(value.split(";")[0] == "Sheet Number|Sheet Name|Notes",
          "the header row - a column empty in every row is left out")
    back = [[unquote(c) for c in row.split("|")] for row in value.split(";")]
    check(back[1] == ["A-1", "Plan; Zone | A", '50% "done"'],
          "a ; | %% and \" in a cell come back whole: %r" % (back[1],))
    check(back[2] == ["A-2", "", ""], "an empty cell stays an empty cell, in its place")
    check(any("\"Empty\" is empty in every row" in x for x in got["notes"]), "the empty column is said")
    check(L.card_value({"ok": False}) == "", "a refused register gives no value")


def section_answer(folder):
    print("6. what the chat is told")
    got = L.read(register_xlsx(folder))
    draft = L.chat_answer(got, status="DRAFT")
    value = L.card_value(got)
    check(draft.count("sheets=" + value) == 1 and draft.count(value) == 1,
          "the exact value is given, and given ONCE")
    check("titleBlockName: ASK the modeller" in draft and "--set 'titleBlockName=...'" in draft,
          "with no Title Block column, the title block is to be asked - no default")
    check("revit_change refuses it (D-117)" in draft and "fragment create-sheets-from-table" in draft
          and "--write --set previewOnly=true" in draft and "try-once in place of fragment" in draft,
          "while DRAFT: the command-line preview, rolled back, and the one try-once")
    check("Nothing in Revit was touched" in draft, "it says it touched no model")
    check("sheetNumbers=M-101, M-102, 105" in draft and "FIND_SHEETS through revit_read" in draft,
          "the numbers for FIND_SHEETS, the read that says which are already used")
    proven = L.chat_answer(got, status="PROVEN")
    check("through revit_change, previewOnly=true first" in proven and "try-once" not in proven
          and proven.count(value) == 1,
          "once PROVEN: revit_change, previewing first, the value once")
    check(L.chat_answer({"ok": False, "refused": "'x' was not read: because"}) == "'x' was not read: because",
          "a refusal is said as it is")


def section_quoting(folder):
    print("7. nothing in a cell can end the quoted value or be run (review finding 1)")
    path = os.path.join(folder, "quotes.csv")
    awkward = [u"Owner\u2019s Area", u"$(calc)", u"ends with a backslash\\",
               u"\u2018single\u2019 \u201alow\u201b", u"`back`tick", u"it's"]
    with io.open(path, "w", encoding="utf-8", newline="") as handle:
        handle.write(u"Sheet Number,Sheet Name\r\n")
        for i, name in enumerate(awkward, 1):
            handle.write(u"Q-%d,\"%s\"\r\n" % (i, name))
    got = L.read(path)
    value = L.card_value(got)
    raw_left = [ch for ch in (u"'", u"\u2018", u"\u2019", u"\u201a", u"\u201b", u"$", u"`", u"\\", u'"')
                if ch in value]
    check(got["ok"] and not raw_left, "no quote, $, backtick or backslash is left raw in the value: %r" % raw_left)
    check("Owner%E2%80%99s Area" in value and "%24(calc)" in value
          and "ends with a backslash%5C;" in value and "it%27s" in value and "%60back%60tick" in value,
          "each is percent-encoded as UTF-8: %E2%80%99, %24, %5C, %27, %60")
    back = [unquote(row.split("|")[1]) for row in value.split(";")[1:]]
    check(back == awkward, "every name decodes back whole - as Uri.UnescapeDataString does: %r" % (back,))
    preview, _once = L.draft_commands(value, True)
    inside = preview.split("--set '", 1)[1].split("' --write", 1)[0]
    check("'" not in inside and "$" not in inside and "`" not in inside and not inside.endswith("\\"),
          "the PowerShell argument holds no quote, $, backtick and no trailing backslash")


def section_size(folder):
    print("8. a DRAFT command never outgrows a Windows command line (finding 2)")
    path = os.path.join(folder, "big.csv")
    with io.open(path, "w", encoding="utf-8", newline="") as handle:
        handle.write(u"Sheet Number,Sheet Name,Drawn By,Checked By,Discipline,Issue Date\r\n")
        for i in range(400):
            handle.write(u"M-%04d,Level %d Mechanical Ductwork Plan - Zone A North,AP,JS,Mechanical,"
                         u"2026-11-09\r\n" % (i, i))
    got = L.read(path)
    value = L.card_value(got)
    draft = L.chat_answer(got, status="DRAFT")
    check(got["ok"] and len(value) > L.MOST_COMMAND_CHARS, "400 rows: %d characters as one value" % len(value))
    check(L.draft_commands(value, True) is None and value not in draft
          and "Split the file into parts of about" in draft and "PROVEN" in draft,
          "the DRAFT commands are refused in words, the value not printed, the split asked for")
    check(len(draft) < 10000, "and the answer stays short: %d characters" % len(draft))
    proven = L.chat_answer(got, status="PROVEN")
    check(proven.count(value) == 1, "once PROVEN the whole value is given, once")
    sub = os.path.join(folder, "small-one")
    os.makedirs(sub)
    small = L.read(register_xlsx(sub))
    commands = L.draft_commands(L.card_value(small), True)
    check(commands is not None and max(len(c) for c in commands) <= L.MOST_COMMAND_CHARS,
          "a small register's commands are printed, within %d characters" % L.MOST_COMMAND_CHARS)

    many = os.path.join(folder, "many.csv")
    with io.open(many, "w", encoding="utf-8", newline="") as handle:
        handle.write(u"Sheet Number\r\n")
        for i in range(L.MOST_ROWS + 5):
            handle.write(u"X-%d\r\n" % i)
    got = L.read(many)
    check(not got["ok"] and "more than %d sheets" % L.MOST_ROWS in got["refused"],
          "more than %d rows is refused, split asked for" % L.MOST_ROWS)


def section_review_xlsx(folder):
    print("9. number formats, a value under no header, hidden and trailing blank rows (findings 3-5, 9)")
    shared = ["Sheet Number", "Sheet Name", "Drawn By", "Plan", "AP", "STRAY", "Hours"]
    shared = ["<t>%s</t>" % t for t in shared]
    rows = ['<row r="1">%s%s%s%s</row>' % (s("A1", 0), s("B1", 1), s("D1", 2), s("E1", 6))]
    # Row 2: number 1 shown through "000" (001), a value under no header (C).
    rows.append('<row r="2">%s%s%s%s</row>' % (n("A2", 1, 1), s("B2", 3), s("C2", 5), s("D2", 4)))
    # Row 3: hidden in Excel; its number is text - passed.
    rows.append('<row r="3" hidden="1"><c r="A3" t="inlineStr"><is><t>M-3</t></is></c>%s</row>' % s("B3", 3))
    # Row 4: 1.1 shown through "0.00" (1.10).
    rows.append('<row r="4">%s%s</row>' % (n("A4", "1.1000000000000001", 2), s("B4", 3)))
    # Row 5: a plain General number, and a duration of 1.5 days in [h]:mm.
    rows.append('<row r="5">%s%s%s</row>' % (n("A5", "105"), s("B5", 3), n("E5", "1.5", 4)))
    # 200 formatted, empty rows after the table - Excel writes these.
    for r in range(6, 206):
        rows.append('<row r="%d" s="3" customFormat="1"><c r="A%d" s="3"/><c r="B%d" s="3"/></row>' % (r, r, r))
    path = os.path.join(folder, "formats.xlsx")
    write_xlsx(path, [("S", "".join(rows))], shared, styles_xf=[0, 164, 2, 0, 165],
               custom_formats={164: "000", 165: "[h]:mm"})
    got = L.read(path)
    numbers = [r["number"] for r in got["rows"]]
    problems = " | ".join(got["problems"])
    check(numbers == ["M-3", "105"], "the two formatted numbers are not passed: %s" % numbers)
    check("row 2, Sheet Number: Excel shows the stored number 1 through the number format \"000\"" in problems
          and "row 4" in problems and "\"0.00\"" in problems and "Type the number as text" in problems,
          "each is a problem naming its format and how to fix it")
    check("row 2, column C has a value under no header - not passed" in problems,
          "a value under a column with no header is listed")
    check(got["skipped"] == [], "200 formatted blank rows after the table are not listed: %d" % len(got["skipped"]))
    check(any("row(s) 3 are hidden or filtered out" in x for x in got["notes"]), "the hidden row is noted")
    check(got["rows"][1]["values"].get("Hours") == "1.5", "a [h]:mm duration is the number 1.5, not a date: %r"
          % got["rows"][1]["values"].get("Hours"))
    answer = L.chat_answer(got, status="DRAFT")
    check(len(answer) < 4000, "the answer stays short: %d characters" % len(answer))

    print("10. lists are cut, and big parts refused (finding 5)")
    lots = os.path.join(folder, "lots.csv")
    with io.open(lots, "w", encoding="utf-8", newline="") as handle:
        handle.write(u"Sheet Number,Sheet Name\r\n")
        for i in range(30):
            handle.write(u",Name only %d\r\n" % i)
        handle.write(u"OK-1,Fine\r\n")
    got = L.read(lots)
    answer = L.chat_answer(got, status="DRAFT")
    check(len(got["problems"]) == 30 and "... and 10 more" in answer
          and answer.count("no sheet number - not passed") == L.MOST_LINES,
          "30 problems are kept, 20 shown and the rest counted")
    huge = os.path.join(folder, "huge.xlsx")
    write_xlsx(huge, [("S", '<row r="1">%s</row>' % s("A1", 0) + "<!--%s-->" % ("x" * (L.MOST_PART_BYTES + 10)))],
               shared, styles_xf=[0])
    got = L.read(huge)
    check(not got["ok"] and "MB unpacked" in got["refused"], "a sheet part above %d MB is refused"
          % (L.MOST_PART_BYTES // (1024 * 1024)))


def section_safety(folder):
    print("11. only four kinds of file, and no content in a refusal (finding 6)")
    secret = u"API_KEY=sk-live-123\nSheet Number,x\nA-1,y\n"
    for name in (".env", "id_rsa", "notes.txt", "data.json", "register.ini"):
        path = os.path.join(folder, name)
        with io.open(path, "w", encoding="utf-8") as handle:
            handle.write(secret)
        got = L.read(path)
        check(not got["ok"] and "sk-live" not in got["refused"] and ".xlsx, .xlsm, .csv or .tsv" in got["refused"],
              "%s is refused by its name, nothing of it quoted" % name)
    got = L.read(os.path.join(folder, ".env"), header_row=2)
    check(not got["ok"], "a header row does not open a dotfile either")
    tsv = os.path.join(folder, "reg.tsv")
    with io.open(tsv, "w", encoding="utf-8", newline="") as handle:
        handle.write(u"Sheet Number\tSheet Name\r\nT-1\tPlan, Level 1\r\n")
    got = L.read(tsv)
    check(got["ok"] and got["rows"][0]["name"] == "Plan, Level 1", "a .tsv is read with tabs")
    disguised = os.path.join(folder, "zip.csv")
    with io.open(disguised, "wb") as handle:
        handle.write(b"PK\x03\x04" + b"\x00" * 40)
    got = L.read(disguised)
    check(not got["ok"] and "workbook, not text" in got["refused"], "a workbook named .csv is refused")
    wide = os.path.join(folder, "wide.csv")
    with io.open(wide, "w", encoding="utf-8") as handle:
        handle.write(u",".join(u"Column number %d with a long header" % i for i in range(40)) + u"\nx\n")
    got = L.read(wide)
    holds = got["refused"].split("holds: ", 1)[1].split(". The first row", 1)[0]
    check(len(holds) <= L.MOST_QUOTED_CHARS + 4, "header cells in a refusal are cut at %d characters: %d"
          % (L.MOST_QUOTED_CHARS, len(holds)))

    print("12. the delimiter is the header row's, under a title (finding 9)")
    titled = os.path.join(folder, "titled-semicolon.csv")
    with io.open(titled, "w", encoding="utf-8", newline="") as handle:
        handle.write(u"Project X sheet register\n\nSheet Number;Sheet Name;Drawn By\nM-101;Plan, L1;AP\n")
    got = L.read(titled, header_row=3)
    check(got["ok"] and got["rows"][0]["name"] == "Plan, L1" and got["rows"][0]["values"] == {"Drawn By": "AP"},
          "';' found from row 3, the header, not from the title on line 1")
    ansi = os.path.join(folder, "arabic.csv")
    with io.open(ansi, "wb") as handle:
        handle.write(u"Sheet Number,Sheet Name\r\nA-1,\u0645\u062e\u0637\u0637\r\n".encode("cp1256"))
    got = L.read(ansi)
    check(any("Windows-1256" in x for x in got["notes"]), "a non-UTF-8 CSV's note names Windows-1256 for Arabic")


def main():
    folder = tempfile.mkdtemp(prefix="heron-sheet-list-")
    try:
        section_xlsx(folder)
        section_csv(folder)
        section_refused(folder)
        section_value(folder)
        section_answer(folder)
        section_quoting(folder)
        section_size(folder)
        section_review_xlsx(folder)
        section_safety(folder)
    finally:
        shutil.rmtree(folder, ignore_errors=True)
    print()
    if FAILURES:
        print("%d FAILED" % len(FAILURES))
        return 1
    print("All passed. That is the reader agreeing with these files - not evidence that "
          "Revit makes the sheets (NEEDS-CHECKING Group MY).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
