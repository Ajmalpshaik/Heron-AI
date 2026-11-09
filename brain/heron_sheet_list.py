# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
A sheet register read from a spreadsheet - .xlsx or .csv - for
CREATE_SHEETS_FROM_TABLE (docs/48 section 5 item 7, "Sheets from Excel").

    python brain/heron_sheet_list.py "C:\\Projects\\sheet list.xlsx"
    python brain/heron_sheet_list.py list.csv --sheet "Register" --header-row 3

READING THE FILE IS PYTHON, NOT REVIT. Nothing here touches a model, and
nothing outside the standard library is used: openpyxl is not a Heron
dependency and adding one needs a decision, so an .xlsx is read as what it
is - a zip of XML parts (the workbook, its sheet list, the shared strings, the
styles that say which numbers are dates, and the sheet itself). An old binary
.xls, an .xlsb and an .ods are refused in words, with how to save the file as
.xlsx or CSV.

THE SHAPE. The first row (or `header_row`) is the headers; each row after it
is one sheet. A column headed Sheet Number (or Number, Sheet No, Drawing
Number ...) is required; Sheet Name (or Name, Title, Drawing Title ...) and
Title Block are optional; every other column names a sheet parameter - Drawn
By, Checked By, Discipline, an issue date - which Revit matches by name when
the sheets are made. Which parameter a column means is never guessed here:
the card says which columns matched nothing, and the host asks (D-33, D-34).

WHAT IS SAID, NEVER FIXED SILENTLY:
  a blank row                skipped, and listed
  a row with no number       not passed, and listed
  a number twice in the file NEITHER row passed - which one is meant is the
                             modeller's to say
  a multi-line cell          made one line, and listed - a sheet name or a
                             parameter value is one line in Revit
  an Excel error (#N/A)      left empty, and listed
  a column with no header,   not passed, and listed
  or a header used twice
An Excel DATE arrives as an ISO date, 2026-11-09 (with the time when it has
one): Excel stores a date as a serial number, and the cell's style says it is
one. A CSV's dates are text and arrive as written.

WHAT REACHES REVIT is `card_value(...)`: ONE line, rows separated by ";",
cells by "|", a ";", "|", "%" or '"' in a cell written %3B, %7C, %25, %22 -
the shape SET_PARAMETER_VALUES_BY_ID's `rows` and the Companion's table Apply
already use. A chat value loses its line breaks, and nothing between the chat and the
card splits on ";" or "|".
"""

from __future__ import print_function

import csv
import datetime
import io
import json
import os
import posixpath
import re
import sys
import zipfile
import xml.etree.ElementTree as ET

# The card makes at most this many sheets in one call, and says so.
MOST_ROWS = 1000
# The bridge reads one mebibyte a line with the card's source beside the
# value - heron_mcp_server._apply_table refuses its table at 700,000. This
# leaves room for the card.
MOST_VALUE_CHARS = 600000
# A spreadsheet part larger than this is not a sheet register.
MOST_PART_BYTES = 50 * 1024 * 1024

NUMBER, NAME, TITLE_BLOCK = "Sheet Number", "Sheet Name", "Title Block"

# Header words, compared after _key(): lower case, "#" as "no", dots, dashes
# and underscores as spaces.
NUMBER_HEADERS = ("sheet number", "sheet no", "sheet num", "number", "no",
                  "drawing number", "drawing no", "dwg number", "dwg no")
NAME_HEADERS = ("sheet name", "sheet title", "name", "title", "drawing title",
                "drawing name", "dwg title", "dwg name")
TITLE_BLOCK_HEADERS = ("title block", "titleblock", "title block type",
                       "title block name", "titleblock type")

REFUSED_KINDS = {
    ".xls": "an Excel 97-2003 workbook (.xls)",
    ".xlsb": "an Excel binary workbook (.xlsb)",
    ".ods": "an OpenDocument spreadsheet (.ods)",
    ".numbers": "an Apple Numbers file",
}
SAVE_AS = ("In Excel: File > Save As > Excel Workbook (*.xlsx), or CSV UTF-8 "
           "(Comma delimited), and give that file.")

# Built-in number formats that are dates, or times only (ECMA-376 18.8.30,
# with the East Asian date formats 27-36 and 50-58).
DATE_FORMATS = set([14, 15, 16, 17, 22] + list(range(27, 37)) + list(range(50, 59)))
TIME_FORMATS = set([18, 19, 20, 21, 45, 46, 47])


# ---------------------------------------------------------------------------
# small pieces
# ---------------------------------------------------------------------------

def _local(tag):
    """An XML tag without its namespace - transitional and strict OOXML both."""
    return tag.rsplit("}", 1)[-1]


def _key(header):
    text = (header or "").lower().replace("#", " no ")
    text = re.sub(r"[._\-]+", " ", text)
    return " ".join(text.split())


def _one_line(text):
    """(text on one line, whether it had a line break)."""
    text = "" if text is None else str(text)
    broken = "\n" in text or "\r" in text
    return " ".join(text.split()) if broken else text.strip(), broken


def _column_index(ref):
    """'C12' -> 2. None when the reference has no letters."""
    letters = re.match(r"([A-Za-z]+)", ref or "")
    if not letters:
        return None
    index = 0
    for ch in letters.group(1).upper():
        index = index * 26 + (ord(ch) - 64)
    return index - 1


def _column_letter(index):
    letters = ""
    index += 1
    while index:
        index, rest = divmod(index - 1, 26)
        letters = chr(65 + rest) + letters
    return letters


def _number_text(value):
    if value == int(value) and abs(value) < 1e15:
        return str(int(value))
    return "%.15g" % value


def excel_date(serial, date1904=False, with_time=None):
    """An Excel serial as ISO text - 45970 -> '2025-11-09'. Excel's 1900
    system counts a 29 February 1900 that never was (serial 60), kept so
    every later serial lands on its real day. Returns None for serial 60
    and for a negative serial."""
    if serial < 0:
        return None
    if date1904:
        base = datetime.datetime(1904, 1, 1)
    elif serial < 60:
        base = datetime.datetime(1899, 12, 31)
    elif serial < 61:
        return None
    else:
        base = datetime.datetime(1899, 12, 30)
    moment = base + datetime.timedelta(days=serial)
    # To the nearest minute, so 0.99999 of a day is not 23:59:59.
    moment = (moment + datetime.timedelta(seconds=30)).replace(second=0, microsecond=0)
    if with_time is None:
        with_time = abs(serial - round(serial)) > 1e-9
    if with_time and (moment.hour or moment.minute):
        return moment.strftime("%Y-%m-%d %H:%M")
    return moment.strftime("%Y-%m-%d")


def _format_is_date(code):
    """(is a date, has a time) for a custom number format code."""
    cleaned = re.sub(r'"[^"]*"|\\.|\[[^\]]*\]', "", (code or "").lower())
    cleaned = cleaned.split(";")[0]
    has_time = "h" in cleaned or "s" in cleaned
    has_date = "y" in cleaned or "d" in cleaned or ("m" in cleaned and not has_time)
    return has_date, has_time


# ---------------------------------------------------------------------------
# .xlsx - a zip of XML parts
# ---------------------------------------------------------------------------

class _Refused(Exception):
    pass


def _part(book, name):
    try:
        info = book.getinfo(name)
    except KeyError:
        return None
    if info.file_size > MOST_PART_BYTES:
        raise _Refused("its part %s is %d MB unpacked - too large to be a sheet register"
                       % (name, info.file_size // (1024 * 1024)))
    return book.read(name)


def _xml(data):
    try:
        return ET.fromstring(data)
    except ET.ParseError as why:
        raise _Refused("one of its parts is not readable XML (%s)" % why)


def _rels(book, name):
    """{id: target} of a .rels part, targets made zip paths."""
    data = _part(book, name)
    if data is None:
        return {}
    base = posixpath.dirname(posixpath.dirname(name))  # xl/_rels/x.rels -> xl
    found = {}
    for rel in _xml(data):
        target = rel.get("Target") or ""
        if target.startswith("/"):
            path = target.lstrip("/")
        else:
            path = posixpath.normpath(posixpath.join(base, target))
        found[rel.get("Id")] = (path, rel.get("Type") or "")
    return found


def _text_of(element):
    """Every <t> in a shared string or an inline string, phonetic runs left out."""
    parts = []

    def walk(node):
        name = _local(node.tag)
        if name == "rPh":
            return
        if name == "t":
            parts.append(node.text or "")
        for child in node:
            walk(child)

    walk(element)
    return "".join(parts)


def read_xlsx(path, sheet=None):
    """(sheet name, every sheet's name, [[cell text, ...] per row with its
    row number], notes) - or raises _Refused."""
    try:
        book = zipfile.ZipFile(path)
    except zipfile.BadZipFile:
        raise _Refused("it is not a readable .xlsx - an .xlsx is a zip, and this is not one. "
                       "A password-protected workbook is not readable either. " + SAVE_AS)
    with book:
        workbook_path = "xl/workbook.xml"
        for _id, (target, kind) in _rels(book, "_rels/.rels").items():
            if kind.endswith("/officeDocument"):
                workbook_path = target
        workbook = _part(book, workbook_path)
        if workbook is None:
            raise _Refused("it has no workbook inside - not an Excel workbook. " + SAVE_AS)
        workbook = _xml(workbook)
        folder = posixpath.dirname(workbook_path)
        rels = _rels(book, posixpath.join(folder, "_rels", posixpath.basename(workbook_path) + ".rels"))

        date1904 = False
        listed = []
        for node in workbook.iter():
            name = _local(node.tag)
            if name == "workbookPr":
                date1904 = (node.get("date1904") or "").lower() in ("1", "true")
            elif name == "sheet":
                rid = None
                for attr, value in node.attrib.items():
                    if _local(attr) == "id":
                        rid = value
                listed.append((node.get("name") or "", rid, node.get("state") or "visible"))
        if not listed:
            raise _Refused("the workbook lists no sheets")

        names = [n for n, _r, _s in listed]
        if sheet:
            chosen = [entry for entry in listed if entry[0].strip().lower() == sheet.strip().lower()]
            if not chosen:
                raise _Refused("it has no sheet called \"%s\" - its sheets are: %s"
                               % (sheet, ", ".join("\"%s\"" % n for n in names)))
            chosen = chosen[0]
        else:
            chosen = listed[0]
        notes = []
        if chosen[2] != "visible":
            notes.append("the sheet \"%s\" is hidden in Excel" % chosen[0])
        if not sheet and len(listed) > 1:
            notes.append("the workbook has %d sheets; the first, \"%s\", was read - others: %s"
                         % (len(listed), chosen[0], ", ".join("\"%s\"" % n for n in names[1:])))

        target = rels.get(chosen[1], (None, ""))[0]
        if not target:
            raise _Refused("its sheet \"%s\" points at no part inside the file" % chosen[0])

        shared = []
        styles_path = None
        for _id, (part, kind) in rels.items():
            if kind.endswith("/sharedStrings"):
                data = _part(book, part)
                if data is not None:
                    shared = [_text_of(si) for si in _xml(data) if _local(si.tag) == "si"]
            elif kind.endswith("/styles"):
                styles_path = part

        # Which cell styles are dates: cellXfs[s].numFmtId, against the
        # built-in date ids and the workbook's own format codes.
        date_style = {}
        if styles_path:
            data = _part(book, styles_path)
            if data is not None:
                styles = _xml(data)
                custom = {}
                for node in styles.iter():
                    if _local(node.tag) == "numFmt":
                        try:
                            custom[int(node.get("numFmtId"))] = node.get("formatCode") or ""
                        except (TypeError, ValueError):
                            pass
                for node in styles:
                    if _local(node.tag) != "cellXfs":
                        continue
                    for index, xf in enumerate(x for x in node if _local(x.tag) == "xf"):
                        try:
                            fmt = int(xf.get("numFmtId") or 0)
                        except ValueError:
                            fmt = 0
                        if fmt in DATE_FORMATS:
                            date_style[index] = (True, fmt == 22)
                        elif fmt in TIME_FORMATS:
                            date_style[index] = (False, True)
                        elif fmt in custom:
                            is_date, has_time = _format_is_date(custom[fmt])
                            if is_date or has_time:
                                date_style[index] = (is_date, has_time)

        data = _part(book, target)
        if data is None:
            raise _Refused("its sheet \"%s\" is missing from the file" % chosen[0])
        grid = _xml(data)

    rows = []
    for row in grid.iter():
        if _local(row.tag) != "row":
            continue
        try:
            number = int(row.get("r"))
        except (TypeError, ValueError):
            number = (rows[-1][0] + 1) if rows else 1
        cells = {}
        errors = []
        at = -1
        for c in row:
            if _local(c.tag) != "c":
                continue
            index = _column_index(c.get("r"))
            at = index if index is not None else at + 1
            kind = c.get("t") or "n"
            value = None
            inline = None
            for part in c:
                name = _local(part.tag)
                if name == "v":
                    value = part.text or ""
                elif name == "is":
                    inline = _text_of(part)
            if kind == "s":
                try:
                    text = shared[int(value)]
                except (TypeError, ValueError, IndexError):
                    text = ""
                    errors.append("%s%d: a shared text Excel did not store" % (_column_letter(at), number))
            elif kind == "inlineStr":
                text = inline or ""
            elif kind == "str":
                text = value or ""
            elif kind == "b":
                text = "TRUE" if (value or "").strip() in ("1", "true") else "FALSE"
            elif kind == "e":
                text = ""
                errors.append("%s%d: Excel shows the error %s - left empty"
                              % (_column_letter(at), number, value or "?"))
            elif kind == "d":
                text = (value or "").replace("T00:00:00", "").replace("T", " ")
            elif value is None or value == "":
                text = ""
            else:
                try:
                    serial = float(value)
                except ValueError:
                    text = value
                else:
                    try:
                        style = int(c.get("s") or 0)
                    except ValueError:
                        style = 0
                    is_date, has_time = date_style.get(style, (False, False))
                    if is_date:
                        text = excel_date(serial, date1904, with_time=has_time or None)
                        if text is None:
                            text = _number_text(serial)
                            errors.append("%s%d: %s is not a date Excel can show - passed as the number"
                                          % (_column_letter(at), number, _number_text(serial)))
                    elif has_time:
                        whole = datetime.datetime(2000, 1, 1) + datetime.timedelta(
                            seconds=round((serial % 1) * 86400))
                        text = whole.strftime("%H:%M")
                    else:
                        text = _number_text(serial)
            cells[at] = text
        width = (max(cells) + 1) if cells else 0
        rows.append((number, [cells.get(i, "") for i in range(width)], errors))
    return chosen[0], names, rows, notes


# ---------------------------------------------------------------------------
# .csv
# ---------------------------------------------------------------------------

def read_csv(path):
    """([(row number, [cell text, ...], [])], notes)."""
    with io.open(path, "rb") as handle:
        raw = handle.read(MOST_PART_BYTES + 1)
    if len(raw) > MOST_PART_BYTES:
        raise _Refused("it is larger than %d MB - too large to be a sheet register"
                       % (MOST_PART_BYTES // (1024 * 1024)))
    notes = []
    if raw.startswith(b"\xef\xbb\xbf"):
        text = raw.decode("utf-8-sig")
    elif raw.startswith(b"\xff\xfe") or raw.startswith(b"\xfe\xff"):
        text = raw.decode("utf-16")
    else:
        try:
            text = raw.decode("utf-8")
        except UnicodeDecodeError:
            text = raw.decode("cp1252", errors="replace")
            notes.append("the file is not UTF-8, so it was read as Windows-1252 - Excel's plain "
                         "'CSV' on Windows. Check any accented or Arabic names in the preview; "
                         "'CSV UTF-8' keeps them exactly")
    first = text.split("\n", 1)[0]
    best, most = ",", 1
    for delimiter in (",", ";", "\t"):
        try:
            fields = next(csv.reader([first], delimiter=delimiter))
        except (csv.Error, StopIteration):
            continue
        if len(fields) > most:
            best, most = delimiter, len(fields)
    if best != ",":
        notes.append("the columns are separated by %s" % ("tabs" if best == "\t" else "\"%s\"" % best))
    rows = []
    try:
        for number, fields in enumerate(csv.reader(io.StringIO(text, newline=""), delimiter=best), 1):
            rows.append((number, fields, []))
    except csv.Error as why:
        raise _Refused("it is not a readable CSV (%s)" % why)
    return rows, notes


# ---------------------------------------------------------------------------
# the register
# ---------------------------------------------------------------------------

def read(path, sheet=None, header_row=1):
    """
    The sheet register in a .xlsx or .csv:

        {"ok": True, "file", "kind": "xlsx"|"csv", "sheet", "sheets",
         "headerRow", "columns": [{"letter", "header", "role"}],
         "parameters": [header, ...], "titleBlockColumn": bool,
         "rows": [{"row", "number", "name", "titleBlock", "values": {}}],
         "problems": [...], "skipped": [...], "notes": [...]}

    or {"ok": False, "refused": "why, in words", ...}. Reads only.
    """
    path = (path or "").strip().strip('"').strip("'")
    path = os.path.expandvars(os.path.expanduser(path))
    answer = {"ok": False, "file": path, "kind": None, "sheet": None, "sheets": [],
              "headerRow": header_row, "columns": [], "parameters": [],
              "titleBlockColumn": False, "rows": [], "problems": [], "skipped": [],
              "notes": [], "refused": None}
    shown = os.path.basename(path) or path

    def refuse(why):
        answer["refused"] = "'%s' was not read: %s" % (shown, why)
        return answer

    if not path:
        return refuse("no file was named")
    extension = os.path.splitext(path)[1].lower()
    if extension in REFUSED_KINDS:
        return refuse("it is %s, which Heron cannot read without a new dependency. %s"
                      % (REFUSED_KINDS[extension], SAVE_AS))
    if not os.path.isfile(path):
        return refuse("there is no file at %s" % path)
    try:
        header_row = int(header_row or 1)
    except (TypeError, ValueError):
        return refuse("the header row \"%s\" is not a row number" % header_row)
    if header_row < 1:
        return refuse("the header row must be 1 or more")
    answer["headerRow"] = header_row

    try:
        with io.open(path, "rb") as handle:
            magic = handle.read(8)
        if magic.startswith(b"\xd0\xcf\x11\xe0"):
            return refuse("it is an old binary Excel file or a password-protected workbook, "
                          "whatever its name says. " + SAVE_AS)
        if extension in (".xlsx", ".xlsm") or magic.startswith(b"PK"):
            answer["kind"] = "xlsx"
            if extension not in (".xlsx", ".xlsm"):
                answer["notes"].append("read as an Excel workbook, which is what it holds")
            answer["sheet"], answer["sheets"], grid, notes = read_xlsx(path, sheet)
        elif extension in (".csv", ".txt", ".tsv", ""):
            answer["kind"] = "csv"
            if sheet:
                answer["notes"].append("a CSV has one sheet, so \"%s\" was not needed" % sheet)
            grid, notes = read_csv(path)
        else:
            return refuse("Heron reads .xlsx and .csv, and this is a '%s' file. %s" % (extension, SAVE_AS))
    except _Refused as why:
        return refuse(str(why))
    except (IOError, OSError) as why:
        return refuse("it could not be opened (%s) - is it open in Excel with a lock, or on a "
                      "drive that is not connected?" % why)
    answer["notes"].extend(notes)

    by_number = dict((number, (cells, errors)) for number, cells, errors in grid)
    header_cells = by_number.get(header_row, ([], []))[0]
    if not any((h or "").strip() for h in header_cells):
        return refuse("row %d, which should hold the headers, is empty. Give the row the "
                      "headers are on (header_row) - a register with a title above its table "
                      "often has them on row 2 or 3" % header_row)

    # THE COLUMNS. Which is the number, the name and the title block; every
    # other column with a header names a parameter.
    roles = {}
    parameters = []
    seen = {}
    unusable = set()
    for index, raw in enumerate(header_cells):
        header, _broken = _one_line(raw)
        letter = _column_letter(index)
        if not header:
            continue
        key = _key(header)
        role = None
        if key in NUMBER_HEADERS:
            role = NUMBER
        elif key in NAME_HEADERS:
            role = NAME
        elif key in TITLE_BLOCK_HEADERS:
            role = TITLE_BLOCK
        if role:
            if role in roles:
                return refuse("two columns could be the %s - %s \"%s\" and %s \"%s\". Rename one, "
                              "so which is meant is not a guess"
                              % (role.lower(), _column_letter(roles[role]), header_cells[roles[role]],
                                 letter, header))
            roles[role] = index
            answer["columns"].append({"letter": letter, "header": header, "role": role})
            continue
        if header.lower() in seen:
            answer["problems"].append("columns %s and %s are both headed \"%s\" - neither is passed; "
                                      "which one is meant is yours to say"
                                      % (seen[header.lower()][0], letter, header))
            unusable.add(seen[header.lower()][1])
            unusable.add(index)
            continue
        seen[header.lower()] = (letter, index)
        parameters.append((index, header))

    if NUMBER not in roles:
        found = [h for h in (_one_line(c)[0] for c in header_cells) if h]
        return refuse("no column is headed Sheet Number (or Number, Sheet No, Drawing Number). "
                      "Row %d holds: %s. The first row must be the headers - or give the row "
                      "they are on" % (header_row, ", ".join("\"%s\"" % h for h in found)))

    parameters = [(i, h) for i, h in parameters if i not in unusable]
    width = len(header_cells)

    rows = []
    numbers = {}
    for number in sorted(n for n in by_number if n > header_row):
        cells, errors = by_number[number]
        answer["problems"].extend("cell " + e for e in errors)
        if not any((c or "").strip() for c in cells):
            answer["skipped"].append("row %d: blank" % number)
            continue
        for index in range(width, len(cells)):
            if (cells[index] or "").strip():
                answer["problems"].append("row %d, column %s has a value under no header - not passed"
                                          % (number, _column_letter(index)))
                break

        def cell(index, header):
            if index is None or index >= len(cells):
                return ""
            text, broken = _one_line(cells[index])
            if broken:
                answer["problems"].append("row %d, %s: the cell has a line break - passed as one "
                                          "line, \"%s\"" % (number, header, text))
            return text

        entry = {"row": number,
                 "number": cell(roles.get(NUMBER), NUMBER),
                 "name": cell(roles.get(NAME), NAME),
                 "titleBlock": cell(roles.get(TITLE_BLOCK), TITLE_BLOCK),
                 "values": {}}
        for index, header in parameters:
            entry["values"][header] = cell(index, header)
        if not entry["number"]:
            answer["problems"].append("row %d%s: no sheet number - not passed"
                                      % (number, " (\"%s\")" % entry["name"] if entry["name"] else ""))
            continue
        numbers.setdefault(entry["number"].lower(), []).append(number)
        rows.append(entry)

    twice = dict((k, v) for k, v in numbers.items() if len(v) > 1)
    for key, where in sorted(twice.items(), key=lambda kv: kv[1][0]):
        shown_number = next(r["number"] for r in rows if r["number"].lower() == key)
        answer["problems"].append("%s is on rows %s - neither is passed; which one is meant is "
                                  "yours to say" % (shown_number, " and ".join(str(n) for n in where)))
    rows = [r for r in rows if r["number"].lower() not in twice]

    empty = [h for _i, h in parameters if not any(r["values"].get(h) for r in rows)]
    for header in empty:
        answer["notes"].append("the column \"%s\" is empty in every row, so nothing is written from it"
                               % header)
    answer["parameters"] = [h for _i, h in parameters]
    answer["titleBlockColumn"] = TITLE_BLOCK in roles
    for index, header in parameters:
        answer["columns"].append({"letter": _column_letter(index), "header": header,
                                  "role": "parameter"})
    answer["columns"].sort(key=lambda c: _column_index(c["letter"]))
    answer["rows"] = rows
    if len(rows) > MOST_ROWS:
        return refuse("it lists %d sheets, and one call makes at most %d - split the list"
                      % (len(rows), MOST_ROWS))
    if not rows:
        return refuse("it has headers but no sheet below them with a number"
                      + ("" if not answer["problems"] else " - " + "; ".join(answer["problems"][:5])))
    answer["ok"] = True
    return answer


def _encode(text):
    """A cell made safe inside the one value: "%" first, then ";" and "|",
    which separate rows and cells, and a double quote, which a PowerShell
    command line and the chat's own quote rule (`_values_array`) both treat
    as more than text. The card decodes every %XX."""
    return ((text or "").replace("%", "%25").replace(";", "%3B").replace("|", "%7C")
            .replace('"', "%22"))


def card_value(register):
    """The `sheets` value CREATE_SHEETS_FROM_TABLE takes, as one line: the
    header row, then a row per sheet. Empty when the register was refused."""
    if not register.get("ok"):
        return ""
    headers = [NUMBER, NAME]
    if register.get("titleBlockColumn"):
        headers.append(TITLE_BLOCK)
    headers.extend(h for h in register["parameters"]
                   if any(r["values"].get(h) for r in register["rows"]))
    lines = ["|".join(_encode(h) for h in headers)]
    for row in register["rows"]:
        cells = [row["number"], row["name"]]
        if register.get("titleBlockColumn"):
            cells.append(row["titleBlock"])
        rest = headers[len(cells):]
        cells.extend(row["values"].get(h, "") for h in rest)
        lines.append("|".join(_encode(c) for c in cells))
    return ";".join(lines)


def _powershell(text):
    return "'" + text.replace("'", "''") + "'"


def chat_answer(register, status=None, most_shown=40):
    """What heron_sheet_list says: what was read, every problem, the sheets,
    and the exact values for CREATE_SHEETS_FROM_TABLE - with the route its
    card's status allows (D-117, D-119)."""
    if not register.get("ok"):
        return register.get("refused") or "The file was not read."
    rows = register["rows"]
    where = ("sheet \"%s\" of " % register["sheet"]) if register.get("sheet") else ""
    out = ["Read %s'%s': %d sheet(s) to make. Nothing in Revit was touched."
           % (where, os.path.basename(register["file"]), len(rows))]
    out.append("Columns: " + "; ".join(
        "%s \"%s\" -> %s" % (c["letter"], c["header"],
                             "a sheet parameter, matched by name in Revit" if c["role"] == "parameter"
                             else c["role"].lower())
        for c in register["columns"]))
    for title, items in (("Problems - ask the modeller, never guess", register["problems"]),
                         ("Skipped", register["skipped"]), ("Notes", register["notes"])):
        if items:
            out.append("%s (%d):" % (title, len(items)))
            out.extend("  - " + i for i in items)
    out.append("Sheets:")
    for row in rows[:most_shown]:
        bits = ["  %s  %s" % (row["number"], row["name"] or "(no name - Revit names it)")]
        if register.get("titleBlockColumn"):
            bits.append("[%s]" % (row["titleBlock"] or "titleBlockName"))
        values = ["%s=%s" % (h, v) for h, v in row["values"].items() if v]
        if values:
            bits.append("; ".join(values))
        out.append("  ".join(bits))
    if len(rows) > most_shown:
        out.append("  ... and %d more" % (len(rows) - most_shown))

    value = card_value(register)
    if len(value) > MOST_VALUE_CHARS:
        out.append("The list is %d characters as one value - too long for one call. Split the "
                   "file and read each part." % len(value))
        return "\n".join(out)
    needs_title_block = (not register.get("titleBlockColumn")
                         or any(not r["titleBlock"] for r in rows))
    out.append("")
    out.append("Values for CREATE_SHEETS_FROM_TABLE, exactly:")
    out.append("sheets=" + value)
    if needs_title_block:
        out.append("titleBlockName=<the title block - ASK the modeller, as Family: Type; there is "
                   "no default>")
    out.append("Which of these numbers are already in the model - FIND_SHEETS through "
               "revit_read, which changes nothing; its notFound are the free ones:")
    out.append("sheetNumbers=" + ", ".join(r["number"] for r in rows))
    state = (status or "").upper()
    if state in ("PROVEN", "PRODUCTION"):
        out.append("Run it through revit_change with previewOnly=true first: which numbers are "
                   "already used, which columns match no parameter. Then without previewOnly.")
    else:
        command = ("python mcp/client/heron_bridge_client.py %%s create-sheets-from-table "
                   "--set %s" % _powershell("sheets=" + value).replace("%", "%%"))
        out.append("CREATE_SHEETS_FROM_TABLE is %s, so revit_change refuses it (D-117). The "
                   "preview runs on the modeller's PC, in PowerShell in the Heron folder, rolled "
                   "back - nothing kept:" % (state or "not PROVEN"))
        out.append("  " + (command % "fragment") + " --write --set previewOnly=true"
                   + (" --set 'titleBlockName=...'" if needs_title_block else ""))
        out.append("and the creation is its one try-once (D-119), only when the modeller asks, "
                   "on the saved model in front - Revit asks Keep or Discard:")
        out.append("  " + (command % "try-once")
                   + (" --set 'titleBlockName=...'" if needs_title_block else ""))
    return "\n".join(out)


def main(argv):
    import argparse
    parser = argparse.ArgumentParser(description="Read a sheet register from .xlsx or .csv.")
    parser.add_argument("path")
    parser.add_argument("--sheet", default=None)
    parser.add_argument("--header-row", type=int, default=1)
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)
    register = read(args.path, sheet=args.sheet, header_row=args.header_row)
    if args.json:
        print(json.dumps(dict(register, value=card_value(register)), indent=2, ensure_ascii=False))
    else:
        print(chat_answer(register))
    return 0 if register.get("ok") else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
