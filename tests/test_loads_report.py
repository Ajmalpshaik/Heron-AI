# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The load calculation sheet (docs/44 section 8): printed from the numbers the
run used, self-contained, honest about what it is - and no PDF, no error,
where no browser is found.

    python tests/test_loads_report.py

WHAT IT DOES NOT PROVE
  That a PDF printed by Edge looks right - that is opened and read on the
  owner's PC (the plan's Task 8 step 5).
"""

from __future__ import print_function

import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests"))

import heron_building_loads as B                              # noqa: E402
import heron_loads_report as R                                # noqa: E402
import heron_takeoff as T                                     # noqa: E402
from test_takeoff import ROOM, run_all                        # noqa: E402
from test_building_loads import PROJECT, OFFICE, bad_second_office   # noqa: E402


def built():
    return B.run(T.read(ROOM), PROJECT, {"Office": OFFICE})


def test_report_prints_the_numbers_the_run_used():
    r = built()
    page = R.html(r)
    assert "Office 01" in page
    assert "%.0f" % r["building"]["block_w"] in page
    assert "%.0f" % r["spaces"][0]["shown"]["total_w"] in page
    for name in PROJECT:                         # every design condition, with its source
        assert name in page


def test_report_is_self_contained_and_honest():
    page = R.html(built()).lower()
    assert "http://" not in page and "https://" not in page and "<script" not in page
    assert "design aid" in page and "not an hourly simulation" in page
    assert "compliant" not in page
    assert "page-break-inside: avoid" in page


def test_report_names_what_was_refused_and_why():
    t = T.read(bad_second_office())
    page = R.html(B.run(t, PROJECT, {"Office": OFFICE}))
    assert "Generic" in page and "refused" in page


def test_csv_has_one_row_per_space():
    lines = R.csv_text(built()).strip().splitlines()
    assert len(lines) == 2 and lines[0].startswith("number,")


def test_takeoff_csv_lists_every_face_and_opening():
    lines = R.takeoff_csv(T.read(ROOM)).strip().splitlines()
    assert len(lines) == 1 + 3               # header, west wall, its window, the roof
    assert ",270.0," in lines[1]             # the west wall faces 270, true north


def test_no_browser_means_no_pdf_and_no_error():
    tmp = tempfile.mkdtemp(prefix="heron-report-")
    try:
        html_path = os.path.join(tmp, "r.html")
        with open(html_path, "w", encoding="utf-8") as fh:
            fh.write(R.html(built()))
        ok, said = R.pdf(html_path, os.path.join(tmp, "r.pdf"),
                         search=[os.path.join(tmp, "none.exe")])
        assert ok is False and "HTML page is the report" in said
        assert not os.path.exists(os.path.join(tmp, "r.pdf"))
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


def test_write_leaves_three_files_and_says_where():
    tmp = tempfile.mkdtemp(prefix="heron-report-")
    try:
        got = R.write(os.path.join(tmp, "run"), T.read(ROOM), built(),
                      search=[os.path.join(tmp, "none.exe")])
        assert got["ok"] and got["pdf"] is None
        for key in ("html", "csv", "takeoff_csv"):
            assert os.path.isfile(got[key]), key
        assert "no PDF" in got["said"]
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


def test_imports_only_the_standard_library():
    import ast
    allowed = set(getattr(sys, "stdlib_module_names", ())) | {
        "__future__", "heron_hvac", "heron_takeoff", "heron_building_loads",
        "heron_designbasis", "heron_psychro"}
    for module in ("heron_loads_report", "heron_building_loads"):
        tree = ast.parse(open(os.path.join(ROOT, "brain", module + ".py")).read())
        names = set()
        for node in ast.walk(tree):
            if isinstance(node, ast.Import):
                names.update(alias.name.split(".")[0] for alias in node.names)
            elif isinstance(node, ast.ImportFrom):
                names.add((node.module or "").split(".")[0])
        assert names <= allowed, (module, names - allowed)


if __name__ == "__main__":
    sys.exit(run_all(sys.modules[__name__]))
