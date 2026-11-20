# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
What an edited parameter file would change, worked out BEFORE Revit reads
it - OPEN-QUESTIONS Q-87, decided 2026-11-15: Heron compares the edited file
with the file as exported, outside Revit, lists the changed cells with old
and new values, and sends only those.

    python brain/heron_sheet_compare.py "C:\\Work\\doors.csv" "C:\\Work\\doors edited.xlsx"
    python brain/heron_sheet_compare.py doors.csv "doors edited.csv" --out "C:\\Work\\doors changes.csv"

The first file is the one EXPORT_PARAMETERS_TO_CSV wrote, kept as it was;
the second is the copy the modeller edited - a CSV or an .xlsx, read by
brain/heron_sheet_list.py's own reader (read_grid), never a second one.

NOTHING HERE TOUCHES REVIT. It reads two files and writes one: a small CSV
holding ONLY the cells that changed, in exactly the shape
SET_PARAMETER_VALUES_BY_ID reads through `csvPath` - the ElementId column
first, then one column per parameter that has a change, a row per element
that has one, every other cell blank. That file goes to the same card as
before, with blankCells=skip: its blanks are the cells nobody changed. No
second tool writes to the model, and neither card is changed (both are read
here only for their formats).

THE TWO FORMATS, read off the cards' C# (2026-11-15):
  the export   header "ElementId" and the parameter names, a row per element,
               each value as Revit DISPLAYS it; a cell holding a comma, a
               quote or a line feed is quoted, a quote doubled; UTF-8 with
               no byte-order mark (File.WriteAllLines).
  the return   File.ReadAllText - a byte-order mark tells it the encoding,
               none means UTF-8 - then RFC 4180: commas, quoted cells, a
               doubled quote inside one, a carriage return outside quotes
               ignored, a blank line dropped. The first column is matched
               as text against each element's id; the headers are trimmed;
               a cell is written as it stands. A blank cell is skip or
               clear - blankCells, asked, no default.
  So the file written here is UTF-8 WITH a byte-order mark (the return
  strips it, and Excel then shows an accented name right), comma-separated,
  quoted only where a cell needs it, lines ended CR LF.

WHAT IS SENT - a cell whose text differs from the export's, after line
endings and spaces at either end. WHAT IS NOT, EACH LISTED APART:
  blanked on purpose      a value emptied. Emptying is a yes of its own -
                          the card's clear, or WRITE_ELEMENT_PARAMETERS
                          clear=true - and the small file's blanks mean
                          "unchanged", so it is never put in it
  the same number         0.50 saved as 0.5, 007 as 7, 2600 as 2.6E+03 -
                          Excel's doing on the way through, not an edit,
                          and a number the export wrote ROUNDED stays out
  an id not in the export a row added, or an id typed over - a row makes no
                          element; one whose other cells match a row that
                          went missing is named as that row with its id
                          changed
  an id on two rows       neither is sent: which is meant is the modeller's
  a column renamed or     nothing in it is sent - there is no old value to
  added                   compare, and a renamed header names another
                          parameter, or none
  a header used twice     neither column is sent
  a character lost to     U+FFFD in a cell - a file saved in a code page
  the file's encoding     that could not hold a letter (row 5b-1528)
  an Excel error, #N/A    left out, never sent as a blank
  a number Excel SHOWS    "000" shows 8 as 008 and stores 8 - which was meant
  through a format        is the modeller's (heron_sheet_list's own rule)
  a column moved on its   three or more changed cells in one column that are
  own                     the export's own values on other elements - sorted
                          alone, most likely - unless --moved-on-purpose
Each list is cut at MOST_LINES in what is printed, "... and N more".

WHAT IT CANNOT SEE: a value changed in Revit since the export, in a cell the
modeller ALSO edited here - only a read of the model, or a UniqueId column
in the export (Q-87 question c - not built for now, D-182), reaches that. Nor whether the
return then takes the file: that is Revit's, NEEDS-CHECKING Group NJ.
"""

from __future__ import print_function

import collections
import csv
import io
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import heron_sheet_list as SHEETS                              # noqa: E402

# The Heron folder - the small file is never written inside it.
ROOT = os.path.dirname(HERE)

# Rows read from either file. An export of every door in a large project is
# thousands of rows; the reader's 8 MB a part bounds the memory either way.
MOST_ROWS = 50000
# What is printed of any one list.
MOST_LINES = SHEETS.MOST_LINES
# A column whose changed cells are only the export's own values moved round,
# at least this many of them, is held back as sorted on its own. Two is a
# swap somebody makes on purpose.
MOST_MOVED = 3

WHAT = "a parameter file"
LOST = u"\ufffd"
SUFFIX = "-changed-cells.csv"
NUMBER = re.compile(r"^([-+]?(?:\d+\.?\d*|\.\d+))([eE][-+]?\d+)?\s*(.*)$")


# ---------------------------------------------------------------------------
# small pieces
# ---------------------------------------------------------------------------

def _text(value):
    """A cell as compared: line endings made one kind, spaces at either end
    left out."""
    value = "" if value is None else str(value)
    return value.replace("\r\n", "\n").replace("\r", "\n").strip()


def _number(text):
    """(value, unit text, exponent or None, digits after the point) - or None
    when the cell is not a number with, at most, a unit after it."""
    found = NUMBER.match(text)
    if not found:
        return None
    mantissa, exponent, unit = found.groups()
    try:
        value = float(mantissa + (exponent or ""))
    except ValueError:
        return None
    decimals = len(mantissa.split(".", 1)[1]) if "." in mantissa else 0
    return value, unit.strip(), (int(exponent[1:]) if exponent else None), decimals


def same_number(old, new):
    """True when two cells hold the same number written another way - 0.50
    and 0.5, 007 and 7, 1200 mm and 1200.0 mm - or when `new` is Excel's
    E-notation of `old` rounded to show it, 123456789012 as 1.23457E+11. The
    unit after the number must be the same text."""
    a, b = _number(old), _number(new)
    if a is None or b is None or a[1] != b[1]:
        return False
    if abs(a[0] - b[0]) <= 1e-9 * max(1.0, abs(a[0]), abs(b[0])):
        return True
    if b[2] is not None and a[2] is None:
        # Excel wrote fewer digits than the cell held: half its last digit.
        return abs(a[0] - b[0]) <= 0.5 * 10 ** (b[2] - b[3])
    return False


def classify(old, new):
    """'same', 'blanked', 'number' or 'changed' - how one cell of the edited
    file stands against the export's."""
    before, after = _text(old), _text(new)
    if before == after:
        return "same"
    if not after:
        return "blanked"
    if same_number(before, after):
        return "number"
    return "changed"


def _quote(text, most=80):
    text = _text(text).replace("\n", " / ")
    if len(text) > most:
        text = text[:most] + "..."
    return "\"%s\"" % text


def _capped(items, most=None):
    return SHEETS._capped(items, MOST_LINES if most is None else most)


def _inside(path, folder):
    path, folder = os.path.realpath(path), os.path.realpath(folder)
    try:
        return os.path.commonpath([path, folder]) == folder
    except ValueError:          # another drive on Windows
        return False


def _same_file(a, b):
    try:
        return os.path.exists(a) and os.path.exists(b) and os.path.samefile(a, b)
    except OSError:
        return os.path.realpath(a) == os.path.realpath(b)


# ---------------------------------------------------------------------------
# one file, as a table
# ---------------------------------------------------------------------------

def _table(read, id_header=None):
    """The header and rows of a grid read_grid gave. With `id_header` the id
    column is the one carrying that header (the edited file); without it,
    the first column (the export, which always writes its id first).

        {"headers": [text], "id": index or None, "columns": {header: index},
         "doubled": [header], "rows": [(row number, id, cells, _Row)],
         "noHeader": [(row number, letter)], "problems": [...]}"""
    grid = read["grid"]
    by_number = dict((row.number, row) for row in grid)
    header_cells = by_number[1].cells if 1 in by_number else []
    headers = [SHEETS._one_line(h)[0] for h in header_cells]
    table = {"headers": headers, "id": None, "columns": {}, "doubled": [], "rows": [],
             "noHeader": [], "problems": []}
    if id_header is None:
        table["id"] = 0 if headers and headers[0] else None
    else:
        at = [i for i, h in enumerate(headers) if h.lower() == id_header.lower()]
        table["id"] = at[0] if len(at) == 1 else None
        if len(at) > 1:
            table["problems"].append("two columns are headed \"%s\" (%s)"
                                     % (id_header, ", ".join(SHEETS._column_letter(i) for i in at)))
    seen = {}
    for index, header in enumerate(headers):
        if index == table["id"] or not header:
            continue
        if header in seen:
            if header not in table["doubled"]:
                table["doubled"].append(header)
            continue
        seen[header] = index
    table["columns"] = dict((h, i) for h, i in seen.items() if h not in table["doubled"])
    for number in sorted(n for n in by_number if n > 1):
        row = by_number[number]
        cells = row.cells
        if not any((c or "").strip() for c in cells):
            continue
        for index in range(len(cells)):
            if (cells[index] or "").strip() and (index >= len(headers) or not headers[index]) \
                    and index != table["id"]:
                table["noHeader"].append((number, SHEETS._column_letter(index)))
        ident = ""
        if table["id"] is not None and table["id"] < len(cells):
            ident = SHEETS._one_line(cells[table["id"]])[0]
        table["rows"].append((number, ident, cells, row))
    return table


def _errors(row):
    """{column index} of the cells the reader found an Excel error in."""
    found = set()
    for text in row.errors:
        ref = re.match(r"([A-Z]+)\d+:", text or "")
        if ref:
            found.add(SHEETS._column_index(ref.group(1)))
    return found


# ---------------------------------------------------------------------------
# the comparison
# ---------------------------------------------------------------------------

def compare(exported_path, edited_path, sheet=None, moved_on_purpose=False):
    """
    What the edited file changes against the file as exported:

        {"ok": True, "exported", "edited", "kind", "sheet", "idHeader",
         "parameters": [header compared, ...],
         "changed": [{"id", "row", "parameter", "old", "new"}],   <- sent
         "blanked": [{"id", "row", "parameter", "old"}],
         "sameNumber": [{"id", "row", "parameter", "old", "new"}],
         "heldBack": [{"id", "row", "parameter", "old", "new", "why"}],
         "ids": {"added": [{"row", "id", "looksLike"}], "doubled": [{"id", "rows"}],
                 "missing": [id], "blank": [row], "doubledInExport": [id]},
         "headers": {"renamed": [{"from", "to", "letter"}], "added": [...],
                     "deleted": [...], "doubled": [...], "doubledInExport": [...]},
         "problems": [...], "notes": [...], "warnings": [...],
         "exportRows", "editedRows"}

    or {"ok": False, "refused": "why, in words", ...}. Reads only - it
    writes nothing, write_changes() does.
    """
    answer = {"ok": False, "refused": None, "exported": exported_path, "edited": edited_path,
              "kind": None, "sheet": None, "idHeader": None, "parameters": [], "changed": [],
              "blanked": [], "sameNumber": [], "heldBack": [],
              "ids": {"added": [], "doubled": [], "missing": [], "blank": [], "doubledInExport": []},
              "headers": {"renamed": [], "added": [], "deleted": [], "doubled": [],
                          "doubledInExport": []},
              "problems": [], "notes": [], "warnings": [], "exportRows": 0, "editedRows": 0}

    before = SHEETS.read_grid(exported_path, header_row=1, what=WHAT, most_rows=MOST_ROWS)
    if not before["ok"]:
        answer["refused"] = "The file as exported: " + before["refused"]
        return answer
    after = SHEETS.read_grid(edited_path, sheet=sheet, header_row=1, what=WHAT,
                             most_rows=MOST_ROWS)
    if not after["ok"]:
        answer["refused"] = "The edited file: " + after["refused"]
        return answer
    answer["exported"], answer["edited"] = before["file"], after["file"]
    answer["kind"], answer["sheet"] = after["kind"], after["sheet"]
    if _same_file(before["file"], after["file"]):
        answer["refused"] = ("The two files are the same file. Give the file as the export wrote "
                             "it first, then the copy that was edited.")
        return answer
    answer["notes"].extend("the file as exported: " + n for n in before["notes"])
    answer["notes"].extend("the edited file: " + n for n in after["notes"])
    try:
        if os.path.getmtime(before["file"]) > os.path.getmtime(after["file"]):
            # Given the wrong way round, the file written would hold the
            # export's values and none of the edits.
            answer["warnings"].append(
                "The file given as exported, '%s', was saved AFTER the edited one, '%s'. Check they "
                "are the right way round - the export first - or the file written holds the old "
                "values." % (os.path.basename(before["file"]), os.path.basename(after["file"])))
    except OSError:
        pass

    old = _table(before)
    if old["id"] is None:
        answer["refused"] = ("The file as exported has no header in its first column, so it is not "
                             "the file the export wrote - its first column is ElementId. Give that "
                             "file, kept as it was.")
        return answer
    id_header = old["headers"][old["id"]]
    answer["idHeader"] = id_header
    new = _table(after, id_header=id_header)
    if new["id"] is None:
        answer["refused"] = ("The edited file has %s headed \"%s\" - the export's first column, "
                             "the one that names each element. Put it back as it was exported, "
                             "header and ids, and compare again."
                             % ("two columns" if new["problems"] else "no column", id_header))
        return answer
    if len(old["rows"]) > MOST_ROWS or len(new["rows"]) > MOST_ROWS:
        answer["refused"] = ("A file holds more than %d rows - more than one comparison reads. "
                             "Export fewer elements at a time." % MOST_ROWS)
        return answer
    if new["id"] != 0:
        answer["notes"].append("the edited file has %s in column %s, not A - it was found by its "
                               "header, and the file written puts it first"
                               % (id_header, SHEETS._column_letter(new["id"])))

    # THE HEADERS. Compared as the return reads them: trimmed, case kept.
    answer["headers"]["doubledInExport"] = list(old["doubled"])
    answer["headers"]["doubled"] = list(new["doubled"])
    exported = [h for h in old["headers"] if h in old["columns"]]
    edited = [h for h in new["headers"] if h in new["columns"]]
    deleted = [h for h in exported if h not in new["columns"] and h not in new["doubled"]]
    added = [h for h in edited if h not in old["columns"] and h not in old["doubled"]]
    renamed = []
    for header in list(added):
        # The same word in another case, or the column at the same letter.
        twin = [d for d in deleted if d.lower() == header.lower()]
        if not twin:
            at = new["columns"][header]
            twin = [d for d in deleted if old["columns"][d] == at]
        if twin:
            renamed.append({"from": twin[0], "to": header,
                            "letter": SHEETS._column_letter(new["columns"][header])})
            deleted.remove(twin[0])
            added.remove(header)
    answer["headers"].update(renamed=renamed, added=added, deleted=deleted)
    compared = [h for h in exported if h in new["columns"]]
    answer["parameters"] = compared

    # THE EXPORT'S ROWS, by id. An id twice in the export is used for nothing.
    export_rows = collections.OrderedDict()
    export_count = collections.Counter(ident for _n, ident, _c, _r in old["rows"] if ident)
    for number, ident, cells, _row in old["rows"]:
        if not ident or export_count[ident] > 1:
            continue
        export_rows[ident] = dict((h, cells[i] if i < len(cells) else "")
                                  for h, i in old["columns"].items())
    answer["ids"]["doubledInExport"] = sorted(i for i, c in export_count.items() if c > 1)
    answer["exportRows"] = len(old["rows"])
    answer["editedRows"] = len(new["rows"])

    edited_count = collections.Counter(ident for _n, ident, _c, _r in new["rows"] if ident)
    where = collections.defaultdict(list)
    for number, ident, _c, _r in new["rows"]:
        if ident:
            where[ident].append(number)
    for ident in sorted((i for i, c in edited_count.items() if c > 1), key=lambda i: where[i][0]):
        answer["ids"]["doubled"].append({"id": ident, "rows": where[ident]})
    answer["ids"]["missing"] = [i for i in export_rows if i not in edited_count]

    # A ROW WHOSE ID IS NOT IN THE EXPORT: named as a missing row with its id
    # changed when its other cells are exactly that row's, and only one row's.
    signatures = collections.defaultdict(list)
    for ident in answer["ids"]["missing"]:
        values = tuple(_text(export_rows[ident].get(h, "")) for h in compared)
        if any(values):
            signatures[values].append(ident)

    for number, ident, cells, row in new["rows"]:
        if not ident:
            answer["ids"]["blank"].append(number)
            continue
        if edited_count[ident] > 1 or ident in answer["ids"]["doubledInExport"]:
            continue
        if ident not in export_rows:
            values = tuple(_text(cells[new["columns"][h]]) if new["columns"][h] < len(cells) else ""
                           for h in compared)
            match = signatures.get(values, [])
            answer["ids"]["added"].append({"row": number, "id": ident,
                                           "looksLike": match[0] if len(match) == 1 else None})
            continue
        errors = _errors(row)
        for header in compared:
            index = new["columns"][header]
            was = export_rows[ident].get(header, "")
            now = cells[index] if index < len(cells) else ""
            cell = {"id": ident, "row": number, "parameter": header, "old": was, "new": now}
            if index in errors:
                cell["why"] = "Excel shows an error in that cell (%s%d) - it is not sent as a blank" \
                              % (SHEETS._column_letter(index), number)
                answer["heldBack"].append(cell)
                continue
            how = classify(was, now)
            if how == "same":
                continue
            if how == "blanked":
                answer["blanked"].append({"id": ident, "row": number, "parameter": header, "old": was})
            elif how == "number":
                answer["sameNumber"].append(cell)
            elif LOST in now:
                cell["why"] = ("it holds a character the file's encoding lost - save the file as CSV "
                               "UTF-8 (Comma delimited), or as .xlsx, and compare again")
                answer["heldBack"].append(cell)
            elif index in row.formatted:
                cell["why"] = ("Excel shows the stored number %s through the number format \"%s\" - "
                               "which was meant is yours to say: type it as text in Excel (start it "
                               "with ') and compare again" % (_text(now), row.formatted[index]))
                answer["heldBack"].append(cell)
            else:
                answer["changed"].append(cell)

    # A COLUMN SORTED ON ITS OWN moves the export's own values onto other
    # elements: every changed cell's new value is some other changed cell's
    # old one. Held back unless the modeller says the move was meant.
    if not moved_on_purpose:
        for header in compared:
            cells = [c for c in answer["changed"] if c["parameter"] == header]
            if len(cells) < MOST_MOVED:
                continue
            if collections.Counter(_text(c["old"]) for c in cells) == \
                    collections.Counter(_text(c["new"]) for c in cells):
                for c in cells:
                    c["why"] = ("the %d changed cells of %s are the export's own values on other "
                                "elements - the column sorted on its own, most likely. If the move "
                                "was meant, compare again with --moved-on-purpose"
                                % (len(cells), header))
                    answer["heldBack"].append(c)
                answer["changed"] = [c for c in answer["changed"] if c["parameter"] != header]

    for number, letter in new["noHeader"]:
        answer["problems"].append("row %d, column %s has a value under no header - not sent"
                                  % (number, letter))
    answer["ok"] = True
    return answer


# ---------------------------------------------------------------------------
# the small file
# ---------------------------------------------------------------------------

def changed_columns(result):
    """The parameters with a change, in the export's order."""
    return [h for h in result["parameters"] if any(c["parameter"] == h for c in result["changed"])]


def small_file_text(result):
    """The CSV SET_PARAMETER_VALUES_BY_ID reads through csvPath: the id
    column first, a column per parameter with a change (the export's order),
    a row per element with one (the edited file's order), every other cell
    blank - to be read with blankCells=skip.
    Text only; write_changes() puts it on disk, with a byte-order mark."""
    columns = changed_columns(result)
    order = []
    values = {}
    for cell in result["changed"]:
        if cell["id"] not in values:
            order.append(cell["id"])
            values[cell["id"]] = {}
        values[cell["id"]][cell["parameter"]] = cell["new"]
    out = io.StringIO()
    writer = csv.writer(out, lineterminator="\r\n")
    writer.writerow([result["idHeader"] or "ElementId"] + columns)
    for ident in order:
        writer.writerow([ident] + [values[ident].get(h, "") for h in columns])
    return out.getvalue()


def default_out(edited_path):
    stem = os.path.splitext(os.path.basename(edited_path))[0]
    return os.path.join(os.path.dirname(os.path.abspath(edited_path)), stem + SUFFIX)


def write_changes(result, out_path=None):
    """Writes the small file and returns {"ok", "path", "replaced", "refused"}.
    Nothing is written when nothing changed, over either file compared, or
    inside the Heron folder."""
    done = {"ok": False, "path": None, "replaced": False, "refused": None}
    if not result.get("ok"):
        done["refused"] = result.get("refused") or "the files were not compared"
        return done
    if not result["changed"]:
        done["refused"] = "nothing changed, so no file was written"
        return done
    path = os.path.abspath(os.path.expandvars(os.path.expanduser(
        (out_path or "").strip().strip('"').strip("'") or default_out(result["edited"]))))
    done["path"] = path
    if not path.lower().endswith(".csv"):
        done["refused"] = "the file written must end in .csv - the return reads a CSV"
        return done
    if _same_file(path, result["edited"]) or _same_file(path, result["exported"]):
        done["refused"] = "it would be written over one of the two files compared - give another name"
        return done
    if _inside(path, ROOT):
        done["refused"] = ("it would be written inside the Heron folder - put it in your own work "
                           "folder, beside the export (--out)")
        return done
    done["replaced"] = os.path.exists(path)
    folder = os.path.dirname(path)
    try:
        if folder and not os.path.isdir(folder):
            os.makedirs(folder)
        with io.open(path, "w", encoding="utf-8-sig", newline="") as handle:
            handle.write(small_file_text(result))
    except (IOError, OSError) as why:
        done["refused"] = "it could not be written (%s)" % why
        return done
    done["ok"] = True
    return done


# ---------------------------------------------------------------------------
# what is printed
# ---------------------------------------------------------------------------

def report(result, written=None):
    """The comparison in a modeller's words: what changed, the file written
    and how it goes back, and everything not sent with why."""
    if not result.get("ok"):
        return (result.get("refused") or "The files were not compared.") + \
            " Nothing was written, and nothing in Revit was read or touched."
    name = os.path.basename
    changed = result["changed"]
    elements = len(set(c["id"] for c in changed))
    columns = changed_columns(result)
    where = (" (sheet \"%s\")" % result["sheet"]) if result.get("sheet") else ""
    out = ["Compared '%s'%s with the file as exported, '%s'. Nothing in Revit was read or touched."
           % (name(result["edited"]), where, name(result["exported"])),
           "%d row(s) in the export, %d in the edited file. Columns compared: %s."
           % (result["exportRows"], result["editedRows"], ", ".join(result["parameters"]) or "none")]
    out.extend("CHECK: " + w for w in result.get("warnings", []))

    out.append("")
    if changed:
        out.append("CHANGED - %d cell(s) on %d element(s), in %d column(s): %s"
                   % (len(changed), elements, len(columns), ", ".join(columns)))
        out.extend(_capped("  element %s  %s: %s -> %s" % (c["id"], c["parameter"], _quote(c["old"]),
                                                            _quote(c["new"])) for c in changed))
    else:
        out.append("CHANGED - nothing. No cell of the edited file differs from the export.")

    if written is not None:
        out.append("")
        if written.get("ok"):
            out.append("%s '%s' - the %s column, the %d column(s) with a change and the %d element(s) "
                       "with one; every other cell is blank."
                       % ("Replaced" if written["replaced"] else "Written to", written["path"],
                          result["idHeader"], len(columns), elements))
            out.append("Bring back THAT file, not the edited one: SET_PARAMETER_VALUES_BY_ID with "
                       "csvPath that file and blankCells=skip. Its blank cells are the ones nobody "
                       "changed - with clear, every one of them would be emptied.")
        else:
            out.append("No file was written: %s." % written["refused"])

    asks = []

    def group(title, lines, most=None):
        lines = list(lines)
        if lines:
            if title:
                asks.append("  - " + title)
            asks.extend(("    " if title else "  - ") + line for line in _capped(lines, most))

    blanked = result["blanked"]
    group("BLANKED ON PURPOSE (%d) - not in the file written. Emptying a value is a yes of its own, "
          "asked apart: the return's blankCells=clear, which empties text only, or "
          "WRITE_ELEMENT_PARAMETERS clear=true on those elements:" % len(blanked),
          ("element %s  %s: %s -> blank" % (c["id"], c["parameter"], _quote(c["old"])) for c in blanked))
    held = result["heldBack"]
    group("HELD BACK (%d) - changed, and not sent:" % len(held),
          ("element %s  %s: %s -> %s - %s" % (c["id"], c["parameter"], _quote(c["old"]), _quote(c["new"]),
                                               c.get("why", "")) for c in held))
    ids = result["ids"]
    group(None, (("row %d: element %s is not in the export - its other cells are element %s's, so that "
                  "row's id was changed, most likely. Nothing on it is sent; put the id back."
                  % (a["row"], a["id"], a["looksLike"])) if a["looksLike"] else
                 ("row %d: element %s is not in the export - a row added, or an id typed over. A row "
                  "makes no element, so nothing on it is sent." % (a["row"], a["id"]))
                 for a in ids["added"]))
    group(None, ("element %s is on rows %s - neither is sent; which one is meant is yours to say"
                 % (d["id"], " and ".join(str(n) for n in d["rows"])) for d in ids["doubled"]))
    if ids["blank"]:
        group(None, ["row(s) %s have no %s - nothing on them is sent"
                     % (", ".join(str(n) for n in _capped(ids["blank"], 10)), result["idHeader"])])
    group(None, ("element %s is on two rows of the file as exported - nothing is sent for it" % i
                 for i in ids["doubledInExport"]))
    headers = result["headers"]
    group(None, ("the column \"%s\" (column %s) was \"%s\" in the export - renamed, so nothing in it is "
                 "sent. Put the header back as it was exported, or say which parameter it means"
                 % (r["to"], r["letter"], r["from"]) for r in headers["renamed"]))
    group(None, ("the column \"%s\" is not in the export - there is no old value to compare it with, "
                 "so nothing in it is sent. Export that parameter with the others and edit it in that "
                 "file" % h for h in headers["added"]))
    group(None, ("two columns of the edited file are headed \"%s\" - neither is sent" % h
                 for h in headers["doubled"]))
    group(None, ("two columns of the file as exported are headed \"%s\" - neither is compared" % h
                 for h in headers["doubledInExport"]))
    group(None, result["problems"])
    if asks:
        out.append("")
        out.append("NOT SENT - ask the modeller, never guess:")
        out.extend(asks)

    alone = []
    same = result["sameNumber"]
    if same:
        alone.append("%d cell(s) hold the same number written another way - Excel's doing, not an "
                     "edit, and the model keeps its own value: %s"
                     % (len(same), "; ".join("element %s %s %s -> %s" % (c["id"], c["parameter"],
                                                                          _quote(c["old"]), _quote(c["new"]))
                                             for c in same[:3]) + (" ..." if len(same) > 3 else "")))
    missing = ids["missing"]
    if missing:
        alone.append("%d row(s) of the export are not in the edited file - nothing is sent for them: %s"
                     % (len(missing), ", ".join(_capped(missing, 10))))
    for h in headers["deleted"]:
        alone.append("the column \"%s\" is not in the edited file - nothing is sent from it" % h)
    if alone:
        out.append("")
        out.append("LEFT AS THEY ARE - nothing to ask:")
        out.extend("  - " + a for a in alone)
    if result["notes"]:
        out.append("")
        out.append("Notes:")
        out.extend("  - " + n for n in _capped(result["notes"]))
    out.append("")
    out.append("What this cannot see: a value changed in Revit since the export, in a cell that was also "
               "edited here. Bring the file back in the same sitting.")
    return "\n".join(out)


def main(argv):
    import argparse
    parser = argparse.ArgumentParser(
        description="Compare an edited parameter file with the file as exported, and write a small "
                    "CSV holding only the changed cells. Reads two files; nothing in Revit is touched.")
    parser.add_argument("exported", help="the CSV EXPORT_PARAMETERS_TO_CSV wrote, kept as it was")
    parser.add_argument("edited", help="the copy that was edited - .csv, .tsv, .xlsx or .xlsm")
    parser.add_argument("--sheet", default=None, help="the sheet of an .xlsx to read (the first if not given)")
    parser.add_argument("--out", default=None,
                        help="where the small file goes (default: beside the edited file, its name "
                             "ending %s)" % SUFFIX)
    parser.add_argument("--moved-on-purpose", action="store_true",
                        help="send a column whose values were moved between elements on purpose")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)
    # A cell may hold any letter; a Windows console redirected to a file
    # encodes print() in the ANSI code page, which has none of Arabic.
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass
    result = compare(args.exported, args.edited, sheet=args.sheet,
                     moved_on_purpose=args.moved_on_purpose)
    written = write_changes(result, args.out) if result.get("ok") and result["changed"] else None
    if args.json:
        print(json.dumps(dict(result, written=written), indent=2, ensure_ascii=False))
    else:
        print(report(result, written))
    if not result.get("ok"):
        return 1
    return 1 if written is not None and not written.get("ok") else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
