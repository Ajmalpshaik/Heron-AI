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
styles that say which numbers are dates, and the sheet itself).

ONLY FOUR KINDS OF FILE ARE OPENED: .xlsx, .xlsm, .csv and .tsv, by their
name. Anything else - an old .xls, an .xlsb, an .ods, a file with no
extension at all (.env, id_rsa) - is refused in words before a byte of it is
read, and a refusal never quotes what a file holds beyond the header cells of
a file already accepted, cut at 200 characters. A path a chat hands in is
never a reason to read an arbitrary file.

HOW BIG. Every part read - the CSV, and each XML part inside an .xlsx - is
refused above MOST_PART_BYTES (8 MB unpacked), so a damaged or hostile file
cannot take the chat's memory: a 1,000-row register is well under 2 MB. The
sheet itself is parsed row by row (iterparse, each row cleared once read) and
reading stops once more than MOST_ROWS sheets are found.

THE SHAPE. The first row (or `header_row`) is the headers; each row after it
is one sheet. A column headed Sheet Number (or Number, Sheet No, Drawing
Number ...) is required; Sheet Name (or Name, Title, Drawing Title ...) and
Title Block are optional; every other column names a sheet parameter - Drawn
By, Checked By, Discipline, an issue date - which Revit matches by name when
the sheets are made. Which parameter a column means is never guessed here:
the card says which columns matched nothing, and the host asks (D-33, D-34).

WHAT IS SAID, NEVER FIXED SILENTLY:
  a blank row between sheets  skipped, and listed (blank rows after the last
                              sheet are the end of the table, not listed)
  a row with no number        not passed, and listed
  a number twice in the file  NEITHER row passed - which one is meant is the
                              modeller's to say
  a Sheet Number Excel shows  NOT passed, and listed: a number shown through a
  through a number format     format ("000" shows 1 as 001) is stored as 1,
                              and 1 is not the sheet asked for. Typed as text
                              in Excel, it is read as typed
  a multi-line cell           made one line, and listed - a sheet name or a
                              parameter value is one line in Revit
  an Excel error (#N/A)       left empty, and listed
  a value under no header     not passed, and listed, row by row
  a header used twice         neither column passed, and listed
  hidden or filtered rows     read like any other, and listed in a note
Each list is cut at 20 lines in what the chat is told, "... and N more".
An Excel DATE arrives as an ISO date, 2026-11-09 (with the time when it has
one): Excel stores a date as a serial number, and the cell's style says it is
one. A duration ([h]:mm) is a number of days and arrives as the number. A
CSV's dates are text and arrive as written.

WHAT REACHES REVIT is `card_value(...)`: ONE line, rows separated by ";",
cells by "|" - the shape SET_PARAMETER_VALUES_BY_ID's `rows` and the
Companion's table Apply already use. A chat value loses its line breaks, and
nothing between the chat and the card splits on ";" or "|". Inside a cell,
everything that could end that line early or be read as more than text is
percent-encoded (`_encode`): ";" "|" "%", both kinds of quote and the four
curly single quotes PowerShell also takes as quotes, "$", the backtick and
the backslash. The card decodes every %XX (Uri.UnescapeDataString, UTF-8).

WHILE THE CARD IS DRAFT the value travels on a Windows command line, which
holds 32,767 characters, so a DRAFT command above MOST_COMMAND_CHARS is not
printed: the modeller splits the file. Once the card is PROVEN, revit_change
carries the value, up to MOST_VALUE_CHARS.
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
# leaves room for the card. It bounds the PROVEN route, revit_change.
MOST_VALUE_CHARS = 600000
# A Windows command line holds 32,767 characters (CreateProcess). The DRAFT
# route - the command-line preview and try-once - carries the whole value on
# one, so a command above this is refused and the file split.
MOST_COMMAND_CHARS = 30000
# A spreadsheet part larger than this, unpacked, is not a sheet register.
MOST_PART_BYTES = 8 * 1024 * 1024
# What the chat is shown of any one list, and of header cells in a refusal.
MOST_LINES = 20
MOST_QUOTED_CHARS = 200

NUMBER, NAME, TITLE_BLOCK = "Sheet Number", "Sheet Name", "Title Block"

# Header words, compared after _key(): lower case, "#" as "no", dots, dashes
# and underscores as spaces.
NUMBER_HEADERS = ("sheet number", "sheet no", "sheet num", "number", "no",
                  "drawing number", "drawing no", "dwg number", "dwg no")
NAME_HEADERS = ("sheet name", "sheet title", "name", "title", "drawing title",
                "drawing name", "dwg title", "dwg name")
TITLE_BLOCK_HEADERS = ("title block", "titleblock", "title block type",
                       "title block name", "titleblock type")

WORKBOOK_KINDS = (".xlsx", ".xlsm")
DELIMITED_KINDS = (".csv", ".tsv")
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
# Built-in formats with no date in them, by their code, to name one.
BUILT_IN_CODES = {1: "0", 2: "0.00", 3: "#,##0", 4: "#,##0.00", 9: "0%",
                  10: "0.00%", 11: "0.00E+00", 12: "# ?/?", 13: "# ??/??",
                  37: "#,##0 ;(#,##0)", 38: "#,##0 ;[Red](#,##0)",
                  39: "#,##0.00;(#,##0.00)", 40: "#,##0.00;[Red](#,##0.00)",
                  48: "##0.0E+0", 49: "@"}

# What _encode writes for each character it encodes - percent-encoded UTF-8,
# which Uri.UnescapeDataString decodes on every release.
ENCODED = [("%", "%25"), (";", "%3B"), ("|", "%7C"), ('"', "%22"), ("'", "%27"),
           (u"\u2018", "%E2%80%98"), (u"\u2019", "%E2%80%99"),
           (u"\u201a", "%E2%80%9A"), (u"\u201b", "%E2%80%9B"),
           ("$", "%24"), ("`", "%60"), ("\\", "%5C")]


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


def _quoted(cells):
    """Header cells for a refusal, cut at MOST_QUOTED_CHARS."""
    text = ", ".join("\"%s\"" % c for c in cells if c)
    return text if len(text) <= MOST_QUOTED_CHARS else text[:MOST_QUOTED_CHARS] + " ..."


def _capped(items, most=MOST_LINES):
    """At most `most` lines, the rest said as a count."""
    items = list(items)
    if len(items) <= most:
        return items
    return items[:most] + ["... and %d more" % (len(items) - most)]


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
    """(is a date, has a time) for a custom number format code. An ELAPSED
    time - [h]:mm, [m]:ss - is a duration, a number of days, and neither."""
    lowered = (code or "").lower()
    if re.search(r"\[(h+|m+|s+)\]", lowered):
        return False, False
    cleaned = re.sub(r'"[^"]*"|\\.|\[[^\]]*\]', "", lowered)
    cleaned = cleaned.split(";")[0]
    has_time = "h" in cleaned or "s" in cleaned
    has_date = "y" in cleaned or "d" in cleaned or ("m" in cleaned and not has_time)
    return has_date, has_time


# ---------------------------------------------------------------------------
# .xlsx - a zip of XML parts
# ---------------------------------------------------------------------------

class _Refused(Exception):
    pass


class _Row(object):
    """One row of the grid: its number, its cells, the problems met reading
    it, whether Excel hides it, and which cells hold a number Excel shows
    through a number format (column index -> the format's code)."""

    __slots__ = ("number", "cells", "errors", "hidden", "formatted")

    def __init__(self, number, cells, errors=None, hidden=False, formatted=None):
        self.number = number
        self.cells = cells
        self.errors = errors or []
        self.hidden = hidden
        self.formatted = formatted or {}


def _part(book, name):
    try:
        info = book.getinfo(name)
    except KeyError:
        return None
    if info.file_size > MOST_PART_BYTES:
        raise _Refused("its part %s is %d MB unpacked - more than %d MB is not a sheet register"
                       % (name, info.file_size // (1024 * 1024), MOST_PART_BYTES // (1024 * 1024)))
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


def _styles(book, styles_path):
    """{cell style index: (is a date, has a time, format code or None)} -
    the code is None for General (numFmtId 0)."""
    found = {}
    data = _part(book, styles_path) if styles_path else None
    if data is None:
        return found
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
            if fmt == 0:
                continue
            if fmt in DATE_FORMATS:
                found[index] = (True, fmt == 22, "built-in date %d" % fmt)
            elif fmt in TIME_FORMATS:
                found[index] = (False, True, "built-in time %d" % fmt)
            elif fmt in custom:
                is_date, has_time = _format_is_date(custom[fmt])
                found[index] = (is_date, has_time, custom[fmt])
            else:
                found[index] = (False, False, BUILT_IN_CODES.get(fmt, "built-in format %d" % fmt))
    return found


def _sheet_rows(data, shared, styles, date1904, header_row):
    """Each row of a worksheet part, parsed row by row and cleared once read.
    Stops once MOST_ROWS + 1 rows with anything in them follow the header -
    the caller refuses that many - so a long sheet costs no more than that."""
    rows = []
    after_header = 0
    try:
        for _event, row in ET.iterparse(io.BytesIO(data), events=("end",)):
            if _local(row.tag) != "row":
                continue
            try:
                number = int(row.get("r"))
            except (TypeError, ValueError):
                number = (rows[-1].number + 1) if rows else 1
            cells, errors, formatted = {}, [], {}
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
                ref = "%s%d" % (_column_letter(at), number)
                if kind == "s":
                    try:
                        text = shared[int(value)]
                    except (TypeError, ValueError, IndexError):
                        text = ""
                        errors.append("%s: a shared text Excel did not store" % ref)
                elif kind == "inlineStr":
                    text = inline or ""
                elif kind == "str":
                    text = value or ""
                elif kind == "b":
                    text = "TRUE" if (value or "").strip() in ("1", "true") else "FALSE"
                elif kind == "e":
                    text = ""
                    errors.append("%s: Excel shows the error %s - left empty" % (ref, value or "?"))
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
                        is_date, has_time, code = styles.get(style, (False, False, None))
                        if is_date:
                            text = excel_date(serial, date1904, with_time=has_time or None)
                            if text is None:
                                text = _number_text(serial)
                                errors.append("%s: %s is not a date Excel can show - passed as "
                                              "the number" % (ref, _number_text(serial)))
                        elif has_time:
                            whole = datetime.datetime(2000, 1, 1) + datetime.timedelta(
                                seconds=round((serial % 1) * 86400))
                            text = whole.strftime("%H:%M")
                        else:
                            text = _number_text(serial)
                            if code is not None:
                                formatted[at] = code
                cells[at] = text
            hidden = (row.get("hidden") or "").lower() in ("1", "true")
            width = (max(cells) + 1) if cells else 0
            rows.append(_Row(number, [cells.get(i, "") for i in range(width)], errors,
                             hidden, formatted))
            row.clear()
            if number > header_row and any((t or "").strip() for t in cells.values()):
                after_header += 1
                if after_header > MOST_ROWS:
                    break
    except ET.ParseError as why:
        raise _Refused("its sheet is not readable XML (%s)" % why)
    return rows


def read_xlsx(path, sheet=None, header_row=1):
    """(sheet name, every sheet's name, [_Row], notes) - or raises _Refused."""
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
                               % (sheet, _quoted(names)))
            chosen = chosen[0]
        else:
            chosen = listed[0]
        notes = []
        if chosen[2] != "visible":
            notes.append("the sheet \"%s\" is hidden in Excel" % chosen[0])
        if not sheet and len(listed) > 1:
            notes.append("the workbook has %d sheets; the first, \"%s\", was read - others: %s"
                         % (len(listed), chosen[0], _quoted(names[1:])))

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
        styles = _styles(book, styles_path)

        data = _part(book, target)
        if data is None:
            raise _Refused("its sheet \"%s\" is missing from the file" % chosen[0])

    rows = _sheet_rows(data, shared, styles, date1904, header_row)
    return chosen[0], names, rows, notes


# ---------------------------------------------------------------------------
# .csv
# ---------------------------------------------------------------------------

def read_csv(path, header_row=1, tab=False):
    """([_Row], notes). The delimiter is the one that gives the HEADER ROW
    the most columns - not physical line 1, which may be a title."""
    with io.open(path, "rb") as handle:
        raw = handle.read(MOST_PART_BYTES + 1)
    if len(raw) > MOST_PART_BYTES:
        raise _Refused("it is larger than %d MB - not a sheet register"
                       % (MOST_PART_BYTES // (1024 * 1024)))
    if raw.startswith(b"PK") or raw.startswith(b"\xd0\xcf\x11\xe0"):
        raise _Refused("it is a workbook, not text, whatever its name says. " + SAVE_AS)
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
                         "'CSV' on a Western Windows. Check any accented names in the preview. A "
                         "file from an Arabic-locale Windows is Windows-1256, and its Arabic names "
                         "read here as nonsense: save it from Excel as 'CSV UTF-8' and read it again")

    def parse(delimiter, limit=None):
        records = []
        for fields in csv.reader(io.StringIO(text, newline=""), delimiter=delimiter):
            records.append(fields)
            if limit is not None and len(records) >= limit:
                break
        return records

    candidates = ("\t",) if tab else (",", ";", "\t")
    best, most = candidates[0], 0
    for delimiter in candidates:
        try:
            records = parse(delimiter, limit=header_row)
        except csv.Error:
            continue
        count = len(records[header_row - 1]) if len(records) >= header_row else 0
        if count > most:
            best, most = delimiter, count
    if best != "," and not tab:
        notes.append("the columns are separated by %s" % ("tabs" if best == "\t" else "\"%s\"" % best))
    rows = []
    after_header = 0
    try:
        for number, fields in enumerate(csv.reader(io.StringIO(text, newline=""), delimiter=best), 1):
            rows.append(_Row(number, fields))
            if number > header_row and any((f or "").strip() for f in fields):
                after_header += 1
                if after_header > MOST_ROWS:
                    break
    except csv.Error as why:
        raise _Refused("it is not a readable CSV (%s)" % why)
    return rows, notes


# ---------------------------------------------------------------------------
# the register
# ---------------------------------------------------------------------------

def read(path, sheet=None, header_row=1):
    """
    The sheet register in a .xlsx, .xlsm, .csv or .tsv:

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
    # BY ITS NAME, BEFORE ANYTHING IS OPENED. A dotfile's whole name is its
    # stem to splitext ('.env' has no extension), so it is refused here too.
    extension = os.path.splitext(path)[1].lower()
    if extension in REFUSED_KINDS:
        return refuse("it is %s, which Heron cannot read without a new dependency. %s"
                      % (REFUSED_KINDS[extension], SAVE_AS))
    if extension not in WORKBOOK_KINDS + DELIMITED_KINDS:
        return refuse("Heron reads a sheet register from .xlsx, .xlsm, .csv or .tsv only, and "
                      "this %s. %s" % ("has no file extension" if not extension
                                       else "is a '%s' file" % extension, SAVE_AS))
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
        if extension in WORKBOOK_KINDS:
            answer["kind"] = "xlsx"
            answer["sheet"], answer["sheets"], grid, notes = read_xlsx(path, sheet, header_row)
        else:
            answer["kind"] = "csv"
            if sheet:
                answer["notes"].append("a CSV has one sheet, so \"%s\" was not needed" % sheet)
            grid, notes = read_csv(path, header_row, tab=(extension == ".tsv"))
    except _Refused as why:
        return refuse(str(why))
    except (IOError, OSError) as why:
        return refuse("it could not be opened (%s) - is it open in Excel with a lock, or on a "
                      "drive that is not connected?" % why)
    answer["notes"].extend(notes)

    by_number = dict((row.number, row) for row in grid)
    header_cells = by_number[header_row].cells if header_row in by_number else []
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
    no_header = set()
    for index, raw in enumerate(header_cells):
        header, _broken = _one_line(raw)
        letter = _column_letter(index)
        if not header:
            no_header.add(index)
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
                return refuse("two columns could be the %s - %s and %s: %s. Rename one, so "
                              "which is meant is not a guess"
                              % (role.lower(), _column_letter(roles[role]), letter,
                                 _quoted([_one_line(header_cells[roles[role]])[0], header])))
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
                      "they are on" % (header_row, _quoted(found)))

    parameters = [(i, h) for i, h in parameters if i not in unusable]
    width = len(header_cells)
    number_at = roles[NUMBER]

    after = sorted(n for n in by_number if n > header_row)
    # THE END OF THE TABLE is its last row with a sheet number: blank rows
    # after it - often hundreds of formatted, empty ones - are not listed.
    last = max([n for n in after
                if number_at < len(by_number[n].cells)
                and (by_number[n].cells[number_at] or "").strip()] or [header_row])

    rows = []
    numbers = {}
    hidden = []
    for number in after:
        row = by_number[number]
        cells = row.cells
        if not any((c or "").strip() for c in cells):
            if number < last:
                answer["skipped"].append("row %d: blank" % number)
            continue
        if row.hidden:
            hidden.append(number)
        answer["problems"].extend("cell " + e for e in row.errors)
        for index in sorted(no_header | set(range(width, len(cells)))):
            if index < len(cells) and (cells[index] or "").strip():
                answer["problems"].append("row %d, column %s has a value under no header - not passed"
                                          % (number, _column_letter(index)))

        def cell(index, header):
            if index is None or index >= len(cells):
                return ""
            text, broken = _one_line(cells[index])
            if broken:
                answer["problems"].append("row %d, %s: the cell has a line break - passed as one "
                                          "line, \"%s\"" % (number, header, text))
            return text

        entry = {"row": number,
                 "number": cell(number_at, NUMBER),
                 "name": cell(roles.get(NAME), NAME),
                 "titleBlock": cell(roles.get(TITLE_BLOCK), TITLE_BLOCK),
                 "values": {}}
        for index, header in parameters:
            entry["values"][header] = cell(index, header)
        if not entry["number"]:
            answer["problems"].append("row %d%s: no sheet number - not passed"
                                      % (number, " (\"%s\")" % entry["name"] if entry["name"] else ""))
            continue
        if number_at in row.formatted:
            # The number Excel SHOWS is not the number it STORES: "000" shows
            # 1 as 001, "0.00" shows 1.1 as 1.10. Which the sheet is meant to
            # be is the modeller's to say, so the row is not passed.
            answer["problems"].append("row %d, %s: Excel shows the stored number %s through the "
                                      "number format \"%s\" (a formatted number like 001) - not "
                                      "passed. Type the number as text in Excel (start it with ') "
                                      "and read the file again"
                                      % (number, NUMBER, entry["number"], row.formatted[number_at]))
            continue
        numbers.setdefault(entry["number"].lower(), []).append(number)
        rows.append(entry)

    twice = dict((k, v) for k, v in numbers.items() if len(v) > 1)
    for key, where in sorted(twice.items(), key=lambda kv: kv[1][0]):
        shown_number = next(r["number"] for r in rows if r["number"].lower() == key)
        answer["problems"].append("%s is on rows %s - neither is passed; which one is meant is "
                                  "yours to say" % (shown_number, " and ".join(str(n) for n in where)))
    rows = [r for r in rows if r["number"].lower() not in twice]

    if hidden:
        answer["notes"].append("row(s) %s are hidden or filtered out in Excel - they were read like "
                               "any other; check them in the preview"
                               % ", ".join(str(n) for n in _capped(hidden, 10)))
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
    sheets_found = len([n for n in after if any((c or "").strip() for c in by_number[n].cells)])
    if len(rows) > MOST_ROWS or sheets_found > MOST_ROWS:
        return refuse("it lists more than %d sheets, and one call makes at most %d - split the "
                      "list into parts and read each" % (MOST_ROWS, MOST_ROWS))
    if not rows:
        return refuse("it has headers but no sheet below them that could be passed - %d "
                      "problem(s) in the rows; read them with the file open in Excel"
                      % len(answer["problems"]))
    answer["ok"] = True
    return answer


def _encode(text):
    """A cell made safe inside the one value - every character in ENCODED,
    "%" first. ";" and "|" separate rows and cells; quotes (straight and the
    curly singles PowerShell also honours), "$", the backtick and the
    backslash could end or change a quoted command-line argument. The card
    decodes every %XX, UTF-8 included."""
    text = text or ""
    for raw, code in ENCODED:
        text = text.replace(raw, code)
    return text


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
    """One single-quoted PowerShell argument. _encode has already taken every
    quote, "$", backtick and backslash out of the value; the doubling is for
    anything typed round it."""
    return "'" + text.replace("'", "''") + "'"


def draft_commands(value, needs_title_block):
    """(preview command, try-once command), or None when either is longer
    than a Windows command line safely holds (MOST_COMMAND_CHARS)."""
    title = " --set 'titleBlockName=...'" if needs_title_block else ""
    base = ("python mcp/client/heron_bridge_client.py %s create-sheets-from-table --set "
            + _powershell("sheets=" + value).replace("%", "%%"))
    preview = (base % "fragment") + " --write --set previewOnly=true" + title
    once = (base % "try-once") + title
    if max(len(preview), len(once)) > MOST_COMMAND_CHARS:
        return None
    return preview, once


def chat_answer(register, status=None, most_shown=40):
    """What heron_sheet_list says: what was read, every problem (cut at
    MOST_LINES), the sheets, and the values for CREATE_SHEETS_FROM_TABLE -
    the value printed ONCE, with the route its card's status allows (D-117,
    D-119)."""
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
            out.extend("  - " + i for i in _capped(items))
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
    needs_title_block = (not register.get("titleBlockColumn")
                         or any(not r["titleBlock"] for r in rows))
    out.append("")
    out.append("Which of these numbers are already in the model - FIND_SHEETS through "
               "revit_read, which changes nothing; its notFound are the free ones:")
    numbers = ", ".join(r["number"] for r in rows)
    out.append("sheetNumbers=" + numbers)
    if needs_title_block:
        out.append("titleBlockName: ASK the modeller, as Family: Type - there is no default.")
    state = (status or "").upper()
    if state in ("PROVEN", "PRODUCTION"):
        if len(value) > MOST_VALUE_CHARS:
            out.append("The list is %d characters as one value - more than one call carries "
                       "(%d). Split the file into parts and read each." % (len(value), MOST_VALUE_CHARS))
            return "\n".join(out)
        out.append("CREATE_SHEETS_FROM_TABLE through revit_change, previewOnly=true first (which "
                   "numbers are used, which columns match no parameter), then without it:")
        out.append("sheets=" + value)
        return "\n".join(out)

    commands = draft_commands(value, needs_title_block)
    if commands is None:
        out.append("CREATE_SHEETS_FROM_TABLE is %s, so it runs only from a command line, and %d "
                   "sheet(s) make a command longer than Windows holds (%d characters as one value; "
                   "a command line takes about %d). Nothing is printed to run. Split the file "
                   "into parts of about %d rows and read each part - or wait until the card is "
                   "PROVEN, when revit_change carries the whole list."
                   % (state or "not PROVEN", len(rows), len(value), MOST_COMMAND_CHARS,
                      max(1, len(rows) * MOST_COMMAND_CHARS // max(1, len(value)) * 9 // 10)))
        return "\n".join(out)
    preview, once = commands
    out.append("CREATE_SHEETS_FROM_TABLE is %s, so revit_change refuses it (D-117). The preview "
               "runs on the modeller's PC, in PowerShell in the Heron folder, rolled back - "
               "nothing kept:" % (state or "not PROVEN"))
    out.append("  " + preview)
    out.append("The creation is its one try-once (D-119), only when the modeller asks, on the "
               "saved model in front - Revit asks Keep or Discard: the same command with "
               "try-once in place of fragment, and without --write --set previewOnly=true.")
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
