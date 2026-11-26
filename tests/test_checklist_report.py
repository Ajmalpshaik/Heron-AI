#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A QA checklist run, written as a report a coordinator can send - one Excel
file and one HTML page (D-178).

    python tests/test_checklist_report.py

WHAT IT HOLDS
-------------
  1. THE PAGE        one self-contained HTML file: every check, its result
                     word, its coverage and its failing ids; the modeller's own
                     words escaped, never run; no script, nothing fetched from
                     anywhere; and PASS said as "meets the value checked" -
                     never that the model complies.
  2. THE WORKBOOK    a real .xlsx - a zip whose every part is well-formed XML -
                     with a Summary sheet and a Checks sheet, one row a check;
                     every value a plain string or number, never a formula, so
                     a sentence that starts with "=" stays text; a character
                     XML cannot hold is dropped, and no cell is longer than
                     Excel allows.
  3. THE FILES       written into the folder given, under names made safe for
                     Windows; a second report of the same run is a NEW file -
                     nothing is ever written over.
  4. THE WIRING      heron_checklist's run takes `report`, and writes the
                     files only when it is true - read as text.

No Revit: the run is Heron's own, on a stand-in for the server's reads, as in
tests/test_checklist_no_data.py.
"""

from __future__ import print_function

import io
import os
import re
import shutil
import sys
import tempfile
import time
import zipfile
from xml.dom import minidom

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
import heron_tools as SERVER_TOOLS                          # noqa: E402
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_checklists as Q                                  # noqa: E402

try:
    import heron_checklist_report as R                        # noqa: E402
except ImportError as why:                                    # the module is what this holds
    R = None
    IMPORT_FAILURE = str(why)

FAILURES = []
BANNED = re.compile(r"\b(compliant|compliance|healthy)\b", re.I)
HOSTILE = "<script>alert(1)</script> every door is =HYPERLINK(\"x\")\x07 tagged"
NOW = time.strptime("2026-11-17 14:05:09", "%Y-%m-%d %H:%M:%S")


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def checklist():
    return {"name": "Before issue / L2", "description": "What we check before an issue", "checks": [
        {"sentence": "every duct has a System Name", "capability": "CHECK_DUCT_NAMES",
         "values": [], "output": "failures", "op": "==", "target": 0, "ids": "offenders",
         "idsKind": "list", "missing": "notChecked", "applicable": "scanned"},
        {"sentence": HOSTILE, "capability": "SELECT_UNENCLOSED_ROOMS",
         "values": [], "output": "unenclosed", "op": "==", "target": 0, "ids": "unenclosed",
         "idsKind": "list", "severity": "high", "clause": "BEP 4.2",
         "advice": "Close each room boundary"},
        {"sentence": "every pipe has a System Name", "capability": "CHECK_PIPE_NAMES",
         "values": [], "output": "failures", "op": "==", "target": 0,
         "missing": "notChecked", "applicable": "scanned"},
        {"sentence": "every linked model is loaded", "capability": "LIST_LINKED_MODELS",
         "values": [], "output": "notLoaded", "op": "==", "target": 0},
    ]}


def answer():
    """A real run: PASS, FAIL with ids, NO DATA, and a check never reached."""
    replies = {
        "CHECK_DUCT_NAMES": {"failures": "0", "scanned": "43", "notChecked": "0",
                             "offenders": "0 item(s)"},
        "SELECT_UNENCLOSED_ROOMS": {"unenclosed": "3 item(s) [7, 8, 9]"},
        "CHECK_PIPE_NAMES": {"failures": "0", "scanned": "12", "notChecked": "5"},
    }

    def ask(check_):
        if check_["capability"] not in replies:
            raise RuntimeError("Revit closed")
        return {"ran": True, "document": "Tower MEP", "provides": replies[check_["capability"]]}
    return Q.run(checklist(), ask)


def page():
    print("1. the page")
    run = answer()
    states = [r["state"] for r in run["rows"]]
    check(states == ["pass", "fail", "missing", "not run"],
          "the run under test is PASS, FAIL, NO DATA, NOT RUN - found %s" % states)
    text = R.html(run, now=NOW)
    check(text.lower().startswith("<!doctype html>"), "a whole HTML document")
    check("<script" not in text.lower(), "no script tag at all - the modeller's words are escaped")
    check("&lt;script&gt;alert(1)&lt;/script&gt;" in text, "the hostile sentence is shown, escaped")
    check("\x07" not in text, "a control character is dropped")
    check(re.search(r"(src|href)\s*=\s*[\"']?(https?:)?//", text, re.I) is None,
          "nothing is fetched from anywhere: no remote src or href")
    for label in ("PASS", "FAIL", "NO DATA", "NOT RUN"):
        check(">%s<" % label in text, "the result word %s is on the page" % label)
    check("Before issue / L2" in text and "Tower MEP" in text, "the checklist and the model are named")
    check("2026-11-17 14:05" in text, "the time of the report is on it")
    check(run["coverage"]["said"] in text, "the coverage line is on it")
    check("high" in text and "BEP 4.2" in text and "Close each room boundary" in text,
          "a FAIL carries its severity, clause and advice")
    check(all(i in text for i in ("7", "8", "9")) and "Failing element ids" in text,
          "a FAIL lists its failing element ids")
    check("meets the value checked" in text, "PASS is said as meeting the value checked")
    check(BANNED.search(text) is None, "it never calls the model compliant or healthy")
    check("Nothing in the model was changed" in text, "it says the run changed nothing")


def _parts(blob):
    zf = zipfile.ZipFile(io.BytesIO(blob))
    return dict((n, zf.read(n)) for n in zf.namelist())


def workbook():
    print("\n2. the workbook")
    run = answer()
    blob = R.xlsx(run, now=NOW)
    check(blob[:2] == b"PK", "a zip, as every .xlsx is")
    parts = _parts(blob)
    for need in ("[Content_Types].xml", "_rels/.rels", "xl/workbook.xml",
                 "xl/_rels/workbook.xml.rels", "xl/styles.xml",
                 "xl/worksheets/sheet1.xml", "xl/worksheets/sheet2.xml"):
        check(need in parts, "it holds %s" % need)
    well = True
    for name, data in parts.items():
        try:
            minidom.parseString(data)
        except Exception as why:                     # noqa: BLE001 - said by name
            well = False
            check(False, "%s is not well-formed XML: %s" % (name, why))
    check(well, "every part is well-formed XML")
    book = parts.get("xl/workbook.xml", b"").decode("utf-8")
    check('name="Summary"' in book and 'name="Checks"' in book, "a Summary sheet and a Checks sheet")
    rows = parts.get("xl/worksheets/sheet2.xml", b"").decode("utf-8")
    check(rows.count("<row ") == 1 + len(run["rows"]), "the Checks sheet has a header and one row a check")
    check("<f>" not in rows and "<f " not in rows, "no cell is a formula")
    check("=HYPERLINK" in rows and 't="inlineStr"' in rows,
          "a sentence starting a formula is kept as plain text")
    check("\x07" not in rows, "a character XML cannot hold is dropped")
    longest = max([len(t) for t in re.findall(r"<t[^>]*>(.*?)</t>", rows, re.S)] or [0])
    check(longest <= 32767, "no cell is longer than Excel allows")
    for label in ("PASS", "FAIL", "NO DATA", "NOT RUN"):
        check(">%s<" % label in rows, "the result word %s is in the sheet" % label)
    summary = parts.get("xl/worksheets/sheet1.xml", b"").decode("utf-8")
    check("Tower MEP" in summary and run["coverage"]["said"] in summary,
          "the summary names the model and carries the coverage line")
    check(BANNED.search(summary + rows) is None, "it never calls the model compliant or healthy")


def files(home):
    print("\n3. the files")
    run = answer()
    where = os.path.join(home, "reports")
    first = R.write(run, where, now=NOW)
    check(len(first) == 2 and all(os.path.isfile(p) for p in first), "two files are written")
    check(sorted(os.path.splitext(p)[1] for p in first) == [".html", ".xlsx"], "one .xlsx and one .html")
    check(all(os.path.dirname(os.path.abspath(p)) == os.path.abspath(where) for p in first),
          "both inside the folder given")
    names = [os.path.basename(p) for p in first]
    check(all(re.search(r'[<>:"/\\|?*]', n) is None for n in names),
          "names are safe on Windows - found %s" % names)
    check(all("Before issue" in n and "Tower MEP" in n and "2026-11-17 1405" in n for n in names),
          "each name says the checklist, the model and when")
    before = [io.open(p, "rb").read() for p in first]
    second = R.write(run, where, now=NOW)
    check(set(second).isdisjoint(first), "a second report of the same run is a new file")
    check([io.open(p, "rb").read() for p in first] == before, "the first report is not written over")


def wiring():
    print("\n4. the wiring")
    server = SERVER_TOOLS.server_text()
    head = server[server.find("def heron_checklist("):]
    head = head[:head.find(")")]
    check("report: bool = False" in head, "heron_checklist takes `report`, off unless asked")
    body = server[server.find("def heron_checklist("):server.find("def heron_checklist(") + 12000]
    check(re.search(r"if report\b", body) is not None and "checklist_report" in body,
          "and writes the report only when asked")


def main():
    if R is None:
        check(False, "brain/heron_checklist_report.py could not be imported: %s" % IMPORT_FAILURE)
    else:
        home = tempfile.mkdtemp(prefix="heron-report-")
        try:
            for part in (page, workbook):
                try:
                    part()
                except Exception as why:             # noqa: BLE001 - one failure, not a traceback
                    check(False, "%s raised %s: %s" % (part.__name__, type(why).__name__, why))
            try:
                files(home)
            except Exception as why:                 # noqa: BLE001 - one failure, not a traceback
                check(False, "files raised %s: %s" % (type(why).__name__, why))
        finally:
            shutil.rmtree(home, ignore_errors=True)
    wiring()
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for what in FAILURES:
            print("  - %s" % what)
        return 1
    print("PASS    a QA checklist run, written to Excel and HTML, never over another file (D-178)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
