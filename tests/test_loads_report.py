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

import copy as _copy
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


def test_an_unconfirmed_takeoff_is_stamped_draft():
    t = T.read(ROOM)
    r = built()
    page = R.html(r, takeoff=t)
    assert "DRAFT - THE TAKE-OFF WAS NOT CONFIRMED" in page
    page = R.html(B.confirm(r, t), takeoff=t)
    assert "NOT CONFIRMED" not in page and "checked and confirmed by" in page
    moved = _copy.deepcopy(ROOM)                                  # a wall moved in the model
    moved["spaces"][0]["faces"][0]["area_m2"] = 14.0
    assert "NOT CONFIRMED" in R.html(r, takeoff=T.read(moved))


def test_glass_by_the_way_it_faces_is_in_the_sheet():
    page = R.html(built(), takeoff=T.read(ROOM))
    assert "Glass by the way it faces" in page and "glass % of wall" in page


def test_report_shows_the_site_the_coil_block_and_the_answers():
    # Review I7 and I1: the site the sun is worked out for, and the outdoor
    # air at the coil, were on no page; the answers about faces Revit could
    # not see past are part of the take-off a reader must be able to check.
    d = _copy.deepcopy(ROOM)
    d["spaces"][0]["faces"][1]["beyond"] = "unknown"
    t = T.read(d)
    r = B.run(t, dict(PROJECT, **{"beyond:12": "outside"}), {"Office": OFFICE})
    page = R.html(r, takeoff=t)
    assert "Site" in page and "25.280" in page and "Doha" in page
    assert "with the outdoor air at the coil" in page.lower()
    assert "%.0f" % r["building"]["coil_block_w"] in page
    assert "beyond:12" not in page and "Roof (12)" in page and "outside" in page
    assert "plant is sized to" not in page


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


def test_a_page_for_each_air_system_every_value_with_its_source():
    # docs/44 s13: the unit serving each group of Spaces, on a page of its own.
    t = T.read(ROOM)
    project = dict(PROJECT, coil_outdoor_dry_bulb_c=46, coil_outdoor_wet_bulb_c=29)
    r = B.run(t, project, {"Office": OFFICE},
              systems={"Z1": {"ventilation_system": "single-zone", "ez": 1.0}})
    page = R.html(r, takeoff=t)
    assert page.count("<section class='system'>") == 1
    assert "Air system: Z1" in page and "page-break-before: always" in page
    for words in ("What this unit must deliver", "The air through its cooling coil",
                  "Outdoor air, Space by Space", "What this page does not include",
                  "Apparatus dew point", "Bypass factor", "Sensible heat ratio",
                  "Mixed outdoor and return air"):
        assert words in page, words
    c = r["air_systems"]["systems"][0]["coil"]
    assert "%.2f" % (c["total_w"] / 1000.0) in page and "%.1f" % c["supply_ls"] in page
    section = page[page.index("<section class='system'>"):]
    assert "NOT AN HOURLY SIMULATION" in section                        # on its own page
    assert "instruction - project coil_outdoor_dry_bulb_c" in section


def test_an_air_system_not_sized_says_why_and_what_is_asked():
    page = R.html(built())
    assert "Air system: Z1" in page and "NOT SIZED" in page
    assert "coil_outdoor_dry_bulb_c" in page and "never filled in" in page


def test_a_run_from_before_air_systems_says_so():
    r = built()
    del r["air_systems"]
    assert "worked out before Heron sized air systems" in R.html(r)


def test_systems_and_ventilation_csv_carry_every_value_and_its_source():
    t = T.read(ROOM)
    project = dict(PROJECT, coil_outdoor_dry_bulb_c=46, coil_outdoor_wet_bulb_c=29)
    r = B.run(t, project, {"Office": OFFICE},
              systems={"Z1": {"ventilation_system": "single-zone", "ez": 1.0}})
    import csv
    rows = list(csv.reader(R.systems_csv(r).splitlines()))
    assert rows[0] == ["system", "item", "value", "unit", "source"]
    assert all(len(x) == 5 for x in rows)
    assert all(x[4] for x in rows[2:]), [x for x in rows[2:] if not x[4]]
    assert any(x[1] == "Bypass factor" for x in rows)
    vent = list(csv.reader(R.ventilation_csv(r).splitlines()))
    assert len(vent) == 2 and vent[1][1] == "1 Office 01" and vent[1][-2] == "62.1"
    tmp = tempfile.mkdtemp(prefix="heron-report-")
    try:
        got = R.write(os.path.join(tmp, "run"), t, r, search=[os.path.join(tmp, "none.exe")])
        for key in ("systems_csv", "ventilation_csv"):
            assert os.path.isfile(got[key]), key
        assert "four CSV files" in got["said"]
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


def test_imports_only_the_standard_library():
    import ast
    allowed = set(getattr(sys, "stdlib_module_names", ())) | {
        "__future__", "heron_hvac", "heron_takeoff", "heron_building_loads",
        "heron_loads_view", "heron_loads_systems",
        "heron_designbasis", "heron_psychro"}
    for module in ("heron_loads_report", "heron_building_loads", "heron_loads_view",
                   "heron_loads_systems"):
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
