<!--
Heron-Agent:  HERON-MEP-HVD-001
Heron-Step:   17
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# Building loads from the model - implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: use superpowers:subagent-driven-development (recommended)
> or superpowers:executing-plans to implement this plan task by task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** a modeller says *"calculate the loads for this building"*. Heron reads every MEP Space from the open
Revit model, calculates each Space's cooling and heating load with the existing `heron_hvac` engine, shows
inputs and results in a new Companion **Loads** panel, writes a load calculation sheet (HTML, PDF, CSV), and
on the modeller's click writes the loads and airflows back into the Spaces and their diffusers.

**Architecture:** one new READ fragment (`REPORT_SPACE_ENVELOPE`) returns the raw take-off as one JSON
string. Everything else is Python in `brain/`: a take-off reader and QA gate (`heron_takeoff.py`), a
building runner over the unchanged room engine (`heron_building_loads.py`), and a report writer
(`heron_loads_report.py`). The MCP server gains one tool, `revit_building_loads`, that ties them together the
way `revit_edit_table` ties READ_ELEMENT_TABLE to the Companion. The Companion gains a `LoadsPanel` holder
and a page section; it holds data and calls hooks, and has no BIM logic. Finalize reuses
`SET_PARAMETER_VALUES_BY_ID` and `SET_AIR_TERMINAL_FLOW`.

**Tech stack:** C# fragment (Revit 2020-2027, one `impl/any`), Python standard library only in `brain/`,
plain HTML/JS/CSS in the Companion, headless Microsoft Edge (already on Windows) for the PDF.

**Spec:** [docs/44-building-loads-from-the-model.md](../../44-building-loads-from-the-model.md). Read it first;
the plan argues from it. The engine it builds on is [docs/41](../../41-hvac-design.md).

## Global constraints

- **No new Python package.** `brain/heron_takeoff.py`, `brain/heron_building_loads.py` and
  `brain/heron_loads_report.py` import only the standard library and Heron's own `brain/` modules, like
  `heron_hvac.py`; `tests/test_hvac.py`'s import check pattern is copied for them.
- **No physics outside `heron_hvac.py`.** The runner builds inputs and adds results. One fact, one home.
- **D-33:** no design value Heron was not given. A missing value is a question, with a standard's figure
  *offered* and named, never applied.
- **Every value in a run carries a source label**, one of: `model`, `standard:<name and table>`,
  `instruction`, `assumption`. The report prints it beside the value.
- **Every take-off number carries its unit in its key name** (`_m2`, `_m`, `_deg`, `_w_m2k`). No unitless
  temperature or length anywhere (the studied skill's defect 4).
- **A glazing type with no U or no SHGC, or an opaque type with no U, refuses that Space's run.** Never
  defaulted (the studied skill's defect 1).
- **The report is printed from the numbers the run used**, never from a separate list (defect 2).
- **Never call a design "compliant".** Every answer and the report carry the engine's `NOT_HAP` /
  `STEADY_HOURS` text and the design-aid disclaimer.
- **Writes:** only through `revit_change`'s body (`_change`), behind the Changes switch, one undo entry,
  stale rows refuse the whole Apply (Articles 8, 9, 12c; D-108).
- **The Revit API namespace only inside `revit/` and fragment `impl/`.** `tools/check-structure.py` and the
  heron-guard hook enforce it - even in a comment or a doc under `.claude/`.
- **Every new source file carries the five-field metadata header** (`tools/check-metadata.py`).
- **No name, wording or code from the studied skill.** Lessons only (AGENTS.md, docs/31).
- **The fragment starts DRAFT.** Nothing in this plan is called proven; the proof is a recorded run on a
  named model with a negative case (D-30), done after the build on a model the owner names.
- **Stage explicit paths. Never `git add -A`.**
- **Changed during the build, 2026-10-04: no pytest.** Heron's suites are plain scripts run as
  `python tests/test_x.py` (CI and `check-gaps.py` run them that way, and pytest is not installed on the
  owner's PC). The new test files keep the `test_` functions below, with plain `assert`, and run them with
  a small `run_all()` in `tests/test_takeoff.py` that the other two import; `pytest.raises` became a
  `raises()` helper and `tmp_path` / `monkeypatch` a `tempfile` folder with `HERON_KNOWLEDGE` set and put
  back. Every `python -m pytest tests/x.py -q` command below reads `python tests/x.py`. Task 1's checks
  went into `tests/test_hvac.py` as its section 9, in that file's own `check()` style, and run
  `monthly_load` on `DOHA_ROOM` **with** `design_weather="doha-0.4"` - without it the engine asks for the
  weather and the answer is `missing`.

## Review focus

The input classes the spec implies that are most likely to bite a modeller; each has its test in the task
that owns the code.

1. **A Space that is not enclosed or not placed** (area 0) - must be listed in QA and left out of the run,
   never calculated as 0 W. Test: Task 2, `test_unplaced_space_is_listed_not_run`.
2. **A window or wall type with no thermal values** - that Space refuses, named with the type; the other
   Spaces still run. Test: Task 2, `test_missing_shgc_refuses_that_space_only`; Task 3,
   `test_one_bad_space_does_not_stop_the_building`.
3. **A rotated project (True North not 0)** - every azimuth turns with it. Test: Task 2,
   `test_true_north_turns_every_azimuth`.
4. **A model in imperial display units** - the Finalize value is converted to the project's power and
   airflow units, not written as watts into a Btu/h field. Test: Task 7, `test_finalize_converts_to_project_units`.
5. **Inputs typed in the Companion that are out of range or not numbers** ("ten", -5, 50 for 0.50) - refused
   at the door, in words, nothing calculated. Test: Task 3, `test_profile_values_are_range_checked`.

---

## File map

| File | New / changed | One job |
|---|---|---|
| `brain/heron_hvac.py` | changed | Each load answer also carries machine-readable numbers (`data`) |
| `brain/heron_takeoff.py` | new | Read and check the take-off JSON; turn faces into engine surfaces; gate 1 QA |
| `brain/heron_building_loads.py` | new | Profiles per Space Type, one engine call per Space, zone and block totals, runs saved, Finalize rows |
| `brain/heron_loads_report.py` | new | The load calculation sheet - HTML, CSV, PDF through Edge |
| `brain/fragments/report-space-envelope/` | new | The Revit take-off reader (C#), card, cases |
| `mcp/server/heron_brain.py` | changed | `building_loads(...)` seam, like `hvac(...)` |
| `mcp/server/heron_mcp_server.py` | changed | Tool `revit_building_loads`; Companion hooks for recalculate, report, finalize |
| `mcp/server/heron_tools.py` | changed | Risk rows for the new tool and the Companion action |
| `mcp/companion/heron_companion.py` | changed | `LoadsPanel` holder and its `/api/loads...` routes |
| `mcp/companion/static/index.html`, `companion.js`, `companion.css` | changed | The Loads section |
| `brain/skills/space-airflow.yaml` | changed | Version 2: calculate first, then write |
| `tests/test_takeoff.py`, `tests/test_building_loads.py`, `tests/test_loads_report.py` | new | Their modules |
| `tests/test_hvac.py`, `tests/test_companion.py` | changed | `data`; the Loads panel |
| `docs/44-...md`, `docs/41-hvac-design.md`, `docs/needs-checking/group-<next>.md`, registers | changed | What is built, what Revit must check |

---

### Task 1: The engine says its numbers, not only its sentences

The runner needs each Space's load at **every hour of every month** to find the block load, and the
component rows as numbers for the report. Today `monthly_load` gives formatted text and tables only.

**Files:**
- Modify: `brain/heron_hvac.py` - `Answer.__init__` (line ~447), `_as_dict` (line ~3990), `run()`'s unknown
  branch (line ~3966), `calc_monthly_load` (line ~2403), `calc_heating_load` (line ~2577),
  `_supply_from_load` (line ~2196)
- Test: `tests/test_hvac.py` - a new section at the end, in that file's own section style

**Interfaces:**
- Produces: every answer dict gains `"data": {}` (empty unless a calculation fills it; empty when not ok).
  - `monthly_load` → `data = {"hours": [{"month": 1..12, "hour": 1..24, "sensible_w": float,
    "latent_w": float, "total_w": float, "coil_w": float|None}], "components": [{"name": str,
    "sensible_w": float, "latent_w": float}] (at the peak hour), "peak": {"month": int, "hour": int,
    "sensible_w": float, "latent_w": float, "total_w": float}}`
  - `heating_load` → `data = {"loss_w": float, "components": [{"name": str, "w": float}],
    "outdoor_air_w": float|None}`
  - `supply_airflow` → `data = {"supply_ls": float}`

- [ ] **Step 1: Write the failing tests**

```python
def test_monthly_load_carries_hourly_numbers():
    a = HVAC.run("monthly_load", DOHA_ROOM)          # the Doha room section 8 already uses
    assert a["status"] == "ok"
    hours = a["data"]["hours"]
    assert len(hours) == 12 * 24
    peak = a["data"]["peak"]
    top = max(hours, key=lambda h: h["total_w"])
    assert (peak["month"], peak["hour"]) == (top["month"], top["hour"])
    assert abs(peak["total_w"] - top["total_w"]) < 1e-9


def test_monthly_load_components_add_up_to_the_peak():
    a = HVAC.run("monthly_load", DOHA_ROOM)
    c = a["data"]["components"]
    assert abs(sum(x["sensible_w"] for x in c) - a["data"]["peak"]["sensible_w"]) < 1e-6
    assert abs(sum(x["latent_w"] for x in c) - a["data"]["peak"]["latent_w"]) < 1e-6


def test_heating_load_carries_its_loss():
    a = HVAC.run("heating_load", {"floor_area_m2": 20, "room_dry_bulb_c": 21,
                                  "outdoor_dry_bulb_c": 5,
                                  "surfaces": [{"name": "wall", "area_m2": 10, "u_w_m2k": 0.5}]})
    assert a["status"] == "ok"
    assert abs(a["data"]["loss_w"] - 10 * 0.5 * 16) < 1e-9
    assert a["data"]["components"] == [{"name": "wall", "w": 80.0}]


def test_unfinished_answer_carries_no_data():
    a = HVAC.run("heating_load", {"floor_area_m2": 20})
    assert a["status"] == "missing" and a["data"] == {}
```

`DOHA_ROOM` is the input dict section 8 of `tests/test_hvac.py` already builds for its twelve-month run;
reuse it by name, or lift it to module level if it is local to one test. If the suite has its own runner
rather than pytest, write the tests in its style - read its first 60 lines before writing.

- [ ] **Step 2: Run them and see them fail**

Run: `python tests/test_hvac.py`
Expected: the four new tests FAIL with `KeyError: 'data'`.

- [ ] **Step 3: Implement**

In `Answer.__init__` add `self.data = {}`. In `_as_dict` add `"data": answer.data if done else {},`.
In the unknown-calculation dict in `run()` add `"data": {}`.

In `calc_monthly_load`'s hour loop, keep the terms as they are added: start each hour with
`parts = [(r[0], r[1], r[2]) for r in constant]`, append `(s["name"], term, 0.0)` for each surface term, and
`("infiltration", qs, ql)` when there is infiltration; store `"parts": parts` in the hour's dict. After
`peaks` is complete, move the `top_m, top_hours, top = max(...)` line up and add:

```python
    a.data["peak"] = {"month": top_m["month"], "hour": top["hour"], "sensible_w": top["s"],
                      "latent_w": top["l"], "total_w": top["total"]}
    a.data["components"] = [{"name": n, "sensible_w": s, "latent_w": l}
                            for n, s, l in top["parts"]]
    a.data["hours"] = [{"month": m["month"], "hour": h["hour"], "sensible_w": h["s"],
                        "latent_w": h["l"], "total_w": h["total"], "coil_w": h["coil"]}
                       for m, hs, _pk in peaks for h in hs]
```

In `calc_heating_load`, after `loss` is final:

```python
    a.data["loss_w"] = loss
    a.data["components"] = [{"name": r[0], "w": r[1]} for r in rows]
    a.data["outdoor_air_w"] = None
```

and inside `if oa is not None:` set `a.data["outdoor_air_w"] = oq`.

In `_supply_from_load` set `a.data["supply_ls"]` to the supply flow it computes, where it computes it.

- [ ] **Step 4: Run the whole suite**

Run: `python tests/test_hvac.py`
Expected: every test PASS, the new four included. No existing test changes.

- [ ] **Step 5: Commit**

```bash
git add brain/heron_hvac.py tests/test_hvac.py
git commit -m "heron_hvac: load answers carry their numbers in data, for the building runner"
```

---

### Task 2: The take-off - read, check, and turn faces into surfaces

**Files:**
- Create: `brain/heron_takeoff.py`
- Test: `tests/test_takeoff.py` (copy the standard-library-only import check from `tests/test_hvac.py`)

**Interfaces:**
- Consumes: the JSON the fragment of Task 4 writes, **format 1**, defined here and nowhere else:

```json
{
  "format": 1,
  "document": "heron ai bulding",
  "units": {"power": "W", "airflow": "L/s"},
  "site": {"latitude_deg": 25.28, "longitude_deg": 51.53, "utc_offset_h": 3.0,
           "elevation_m": 10.0, "project_to_true_north_deg": 0.0, "named": "Doha"},
  "types": {"12345": {"name": "Basic Wall: Generic - 300mm", "category": "Walls",
                      "u_w_m2k": 0.45, "shgc": null, "absorptance": 0.7}},
  "spaces": [{
    "id": 927001, "unique_id": "...", "number": "1", "name": "Office 01", "level": "Level 1",
    "area_m2": 41.35, "volume_m3": 99.2, "height_m": 2.4, "zone": "Zone 1 - West Offices 01-04",
    "space_type": "Office", "placed": true,
    "current": {"Design Cooling Load": "0.00 W", "Design Heating Load": "0.00 W",
                "Specified Supply Airflow": "330.0 L/s"},
    "terminals": [927789, 927790, 927791],
    "faces": [{
      "element": 4501, "type": "12345", "side": "wall",
      "normal": [0.0, -1.0, 0.0], "area_m2": 21.6,
      "beyond": "outside", "beyond_space": null,
      "openings": [{"element": 6001, "kind": "window", "type": "777", "area_m2": 3.6}]
    }]
  }],
  "findings": ["..."]
}
```

`side` is `wall`, `top` or `bottom`. `beyond` is `outside`, `space` (with `beyond_space` its id),
`unconditioned` or `unknown`. `normal` is the face's outward normal in project coordinates (x east, y
project north). `area_m2` on a face is GROSS - its openings are inside it. Opening `kind` is `window`,
`door`, `curtain_panel` or `skylight`.
- Produces:
  - `read(text_or_dict) -> Takeoff` - raises `TakeoffError(str)` on a wrong format; never returns half.
  - `azimuth_deg(normal, project_to_true_north_deg) -> float` - 0 north, 90 east, clockwise, true north.
  - `qa(takeoff) -> list[dict]` - each `{"level": "FAIL"|"WARN"|"INFO", "space": id|None, "text": str}`.
  - `surfaces(takeoff, space) -> dict` with keys `walls`, `roofs`, `windows`, `skylights`, `partitions`,
    `floors`, `refused`. Walls and roofs: `{"name", "area_m2", "u_w_m2k", "absorptance"}` (+ `facing` for a
    wall); windows `{"name", "area_m2", "u_w_m2k", "shgc", "facing"}`; skylights the same without
    `facing`; partitions and floors `{"name", "area_m2", "u_w_m2k"}`; `refused` a list of sentences.
  - `Takeoff`: `.document, .units, .site, .types, .spaces, .findings`.

- [ ] **Step 1: Write the failing tests**

```python
import copy as _copy
import pytest
import heron_takeoff as T

ROOM = {"format": 1, "document": "t", "units": {"power": "W", "airflow": "L/s"},
        "site": {"latitude_deg": 25.28, "longitude_deg": 51.53, "utc_offset_h": 3.0,
                 "elevation_m": 10.0, "project_to_true_north_deg": 0.0, "named": "Doha"},
        "types": {"w": {"name": "Ext wall", "category": "Walls", "u_w_m2k": 0.5,
                        "shgc": None, "absorptance": 0.7},
                  "g": {"name": "Window 1200", "category": "Windows", "u_w_m2k": 2.8,
                        "shgc": 0.4, "absorptance": None},
                  "r": {"name": "Roof", "category": "Roofs", "u_w_m2k": 0.3,
                        "shgc": None, "absorptance": 0.6}},
        "spaces": [{"id": 1, "unique_id": "u1", "number": "1", "name": "Office 01",
                    "level": "L1", "area_m2": 20.0, "volume_m3": 54.0, "height_m": 2.7,
                    "zone": "Z1", "space_type": "Office", "placed": True,
                    "current": {"Design Cooling Load": "0.00 W",
                                "Design Heating Load": "0.00 W",
                                "Specified Supply Airflow": "0.0 L/s"},
                    "terminals": [],
                    "faces": [{"element": 10, "type": "w", "side": "wall",
                               "normal": [-1.0, 0.0, 0.0], "area_m2": 13.5,
                               "beyond": "outside", "beyond_space": None,
                               "openings": [{"element": 11, "kind": "window", "type": "g",
                                             "area_m2": 2.0}]},
                              {"element": 12, "type": "r", "side": "top",
                               "normal": [0.0, 0.0, 1.0], "area_m2": 20.0,
                               "beyond": "outside", "beyond_space": None, "openings": []}]}],
        "findings": []}


def copy(**change):
    d = _copy.deepcopy(ROOM)
    d.update(change)
    return d


def test_west_wall_nets_out_its_window():
    t = T.read(ROOM)
    s = T.surfaces(t, t.spaces[0])
    assert s["walls"] == [{"name": "Ext wall (10)", "area_m2": 11.5, "u_w_m2k": 0.5,
                           "facing": 270.0, "absorptance": 0.7}]
    assert s["windows"] == [{"name": "Window 1200 (11)", "area_m2": 2.0, "u_w_m2k": 2.8,
                             "shgc": 0.4, "facing": 270.0}]
    assert s["roofs"] == [{"name": "Roof (12)", "area_m2": 20.0, "u_w_m2k": 0.3,
                           "absorptance": 0.6}]
    assert s["refused"] == []


def test_true_north_turns_every_azimuth():
    assert T.azimuth_deg([0.0, 1.0, 0.0], 0.0) == 0.0
    assert T.azimuth_deg([1.0, 0.0, 0.0], 0.0) == 90.0
    assert T.azimuth_deg([-1.0, 0.0, 0.0], 0.0) == 270.0
    # project north turned 30 degrees east of true north: a face to project north faces 30
    assert abs(T.azimuth_deg([0.0, 1.0, 0.0], 30.0) - 30.0) < 1e-9


def test_missing_shgc_refuses_that_space_only():
    d = copy()
    d["types"]["g"]["shgc"] = None
    t = T.read(d)
    s = T.surfaces(t, t.spaces[0])
    assert any("Window 1200" in r and "SHGC" in r for r in s["refused"])


def test_unplaced_space_is_listed_not_run():
    d = copy()
    d["spaces"][0]["placed"] = False
    d["spaces"][0]["area_m2"] = 0.0
    findings = T.qa(T.read(d))
    assert any(f["level"] == "FAIL" and f["space"] == 1 and "not placed" in f["text"]
               for f in findings)


def test_wrong_format_is_refused_whole():
    with pytest.raises(T.TakeoffError):
        T.read({"format": 2})
    with pytest.raises(T.TakeoffError):
        T.read("not json")


def test_opening_larger_than_its_face_is_a_fail():
    d = copy()
    d["spaces"][0]["faces"][0]["openings"][0]["area_m2"] = 14.0
    assert any(f["level"] == "FAIL" and "larger than" in f["text"] for f in T.qa(T.read(d)))


def test_partition_to_another_space_carries_no_load():
    d = copy()
    d["spaces"][0]["faces"][0]["beyond"] = "space"
    d["spaces"][0]["faces"][0]["beyond_space"] = 2
    t = T.read(d)
    s = T.surfaces(t, t.spaces[0])
    assert s["walls"] == [] and s["windows"] == [] and s["partitions"] == []


def test_no_site_location_is_a_fail():
    d = copy()
    d["site"]["latitude_deg"] = None
    assert any(f["level"] == "FAIL" and "site location" in f["text"] for f in T.qa(T.read(d)))
```

- [ ] **Step 2: Run them and see them fail** - `python -m pytest tests/test_takeoff.py -q`; expected
  `ModuleNotFoundError: No module named 'heron_takeoff'`. (Check how other tests put `brain/` on `sys.path` -
  `tests/conftest.py` or a header in each file - and do the same.)

- [ ] **Step 3: Implement `brain/heron_takeoff.py`**

```python
"""
The take-off a Revit model gives for a building's loads - docs/44 section 4.

Reads REPORT_SPACE_ENVELOPE's JSON (format 1), checks it (gate 1, docs/44
section 6), and turns each Space's faces into the surface records
heron_hvac's room calculations read. It adds no physics: areas are netted,
azimuths turned to true north, and nothing else.

Standard library only.
"""
import json
import math

FORMAT = 1
GLAZED = ("window", "curtain_panel", "skylight")


class TakeoffError(ValueError):
    """The take-off cannot be read at all - wrong format, or not JSON."""


class Takeoff(object):
    def __init__(self, d):
        self.document = d.get("document")
        self.units = d.get("units") or {}
        self.site = d.get("site") or {}
        self.types = {str(k): v for k, v in (d.get("types") or {}).items()}
        self.spaces = list(d.get("spaces") or [])
        self.findings = list(d.get("findings") or [])


def read(raw):
    if isinstance(raw, str):
        try:
            raw = json.loads(raw)
        except ValueError as why:
            raise TakeoffError("the take-off is not JSON: %s" % why)
    if not isinstance(raw, dict) or raw.get("format") != FORMAT:
        raise TakeoffError("the take-off is not format %d - read the model again with "
                           "REPORT_SPACE_ENVELOPE" % FORMAT)
    for key in ("spaces", "types", "site", "units"):
        if key not in raw:
            raise TakeoffError("the take-off has no %r" % key)
    return Takeoff(raw)


def azimuth_deg(normal, project_to_true_north_deg):
    """Compass bearing the face looks toward: 0 north, 90 east, clockwise, true north."""
    x, y = float(normal[0]), float(normal[1])
    project = math.degrees(math.atan2(x, y)) % 360.0
    return round((project + float(project_to_true_north_deg or 0.0)) % 360.0, 6)


def _name(t, type_id, element):
    kind = t.types.get(str(type_id)) or {}
    return "%s (%s)" % (kind.get("name") or "type %s" % type_id, element)


def _no_value(name, what):
    return "%s has no %s in the model - set its type's thermal properties" % (name, what)


def surfaces(t, space):
    out = {"walls": [], "roofs": [], "windows": [], "skylights": [], "partitions": [],
           "floors": [], "refused": []}
    north = t.site.get("project_to_true_north_deg")
    for face in space.get("faces") or []:
        beyond = face.get("beyond")
        if beyond == "space":
            continue                                # conditioned on both sides: no load
        kind = t.types.get(str(face.get("type"))) or {}
        name = _name(t, face.get("type"), face.get("element"))
        side = face.get("side")
        facing = azimuth_deg(face["normal"], north) if side == "wall" else None
        net = float(face.get("area_m2") or 0.0)
        for o in face.get("openings") or []:
            area = float(o.get("area_m2") or 0.0)
            net -= area
            okind = t.types.get(str(o.get("type"))) or {}
            oname = _name(t, o.get("type"), o.get("element"))
            glazed = o.get("kind") in GLAZED
            if okind.get("u_w_m2k") is None:
                out["refused"].append(_no_value(oname, "U-value"))
                continue
            if glazed and okind.get("shgc") is None:
                out["refused"].append(_no_value(oname, "SHGC"))
                continue
            if beyond != "outside":
                out["partitions"].append({"name": oname, "area_m2": area,
                                          "u_w_m2k": okind["u_w_m2k"]})
            elif glazed and side == "wall":
                out["windows"].append({"name": oname, "area_m2": area,
                                       "u_w_m2k": okind["u_w_m2k"], "shgc": okind["shgc"],
                                       "facing": facing})
            elif glazed:
                out["skylights"].append({"name": oname, "area_m2": area,
                                         "u_w_m2k": okind["u_w_m2k"], "shgc": okind["shgc"]})
            else:                                   # an opaque door to outside
                out["walls"].append({"name": oname, "area_m2": area,
                                     "u_w_m2k": okind["u_w_m2k"], "facing": facing,
                                     "absorptance": okind.get("absorptance")})
        if net <= 1e-9:
            continue
        if kind.get("u_w_m2k") is None:
            out["refused"].append(_no_value(name, "U-value"))
            continue
        rec = {"name": name, "area_m2": round(net, 6), "u_w_m2k": kind["u_w_m2k"]}
        if beyond != "outside":
            (out["floors"] if side == "bottom" else out["partitions"]).append(rec)
        elif side == "wall":
            rec["facing"] = facing
            rec["absorptance"] = kind.get("absorptance")
            out["walls"].append(rec)
        elif side == "top":
            rec["absorptance"] = kind.get("absorptance")
            out["roofs"].append(rec)
        else:
            out["floors"].append(rec)
    return out


def qa(t):
    found = []

    def add(level, space, text):
        found.append({"level": level, "space": space, "text": text})

    if t.site.get("latitude_deg") is None or t.site.get("longitude_deg") is None:
        add("FAIL", None, "the model has no site location - set it in Manage > Location")
    if t.site.get("project_to_true_north_deg") is None:
        add("FAIL", None, "the model's True North could not be read")
    if not t.spaces:
        add("FAIL", None, "the model has no MEP Spaces - place Spaces first; Rooms carry no load")
    for s in t.spaces:
        sid = s.get("id")
        label = ("%s %s" % (s.get("number") or "", s.get("name") or "")).strip()
        if not s.get("placed") or not s.get("area_m2"):
            add("FAIL", sid, "Space %s is not placed or not enclosed - it is left out" % label)
            continue
        if not any(f.get("beyond") == "outside" for f in s.get("faces") or []):
            add("INFO", sid, "Space %s has no outside face - only its internal gains load it"
                % label)
        for face in s.get("faces") or []:
            if face.get("beyond") == "unknown":
                add("WARN", sid, "Space %s: nothing was found beyond the face of element %s - "
                    "say what is there" % (label, face.get("element")))
            gross = float(face.get("area_m2") or 0.0)
            holes = sum(float(o.get("area_m2") or 0.0) for o in face.get("openings") or [])
            if holes > gross + 1e-6:
                add("FAIL", sid, "Space %s: openings of %.2f m2 are larger than the %.2f m2 "
                    "face of element %s" % (label, holes, gross, face.get("element")))
        for why in surfaces(t, s)["refused"]:
            add("FAIL", sid, "Space %s: %s" % (label, why))
    for text in t.findings:
        add("WARN", None, text)
    return found
```

Add the five-field header at the top in the form `tools/check-metadata.py` wants for a Python file - copy
`brain/heron_hvac.py`'s first lines and change only the agent and status as needed.

- [ ] **Step 4: Run the tests** - `python -m pytest tests/test_takeoff.py -q`; expected all PASS.

- [ ] **Step 5: Commit**

```bash
git add brain/heron_takeoff.py tests/test_takeoff.py
git commit -m "heron_takeoff: read and check a Space take-off, faces to engine surfaces"
```

---

### Task 3: The building runner

**Files:**
- Create: `brain/heron_building_loads.py`
- Test: `tests/test_building_loads.py`

**Interfaces:**
- Consumes: `heron_takeoff.read/qa/surfaces` (Task 2); `heron_hvac.run(name, inputs, recorded)` and its
  `data` (Task 1).
- Produces:
  - `profile_key(space) -> str` - the Space Type name, else the Space name with trailing digits removed
    ("Office 01" → "Office"), else "(unnamed)".
  - `monthly_inputs(t, space, project, profile) -> dict` and `heating_inputs(...) -> dict` - the engine
    inputs for one Space (exposed so a test can call the engine by hand with the same inputs).
  - `needs(t, project, profiles) -> list[dict]` - what is still to be asked, each `{"input", "unit", "why",
    "offer": str|None, "for": "project"|<profile key>}`, project inputs first.
  - `run(t, project, profiles, overrides=None, recorded=None) -> dict` (shape below).
  - `save(project_key, result) -> str` (path) and `load(project_key, run_id=None) -> dict|None` - kept as
    `<knowledge>/projects/<safe key>.loads/<YYYYMMDD-HHMMSS>.json`, built the way
    `heron_designbasis.path_for` builds its own (reuse `heron_designbasis._scope()` for `knowledge_dir()`
    and `_safe_key`).

```python
{"format": 1, "document": str, "when": "2026-10-04T15:00:00", "run_id": "20261004-150000",
 "qa": [...],                                   # heron_takeoff.qa
 "spaces": [{"id", "unique_id", "number", "name", "zone", "profile",
             "status": "ok"|"refused"|"missing"|"left out", "why": [str], "area_m2",
             "cooling": {"peak": {...}, "components": [...]} | None,
             "heating": {"loss_w", "components"} | None,
             "supply_ls": float|None, "outdoor_air_ls": float|None, "terminals": [ids]}],
 "zones": [{"name", "area_m2", "sum_of_peaks_w", "block_w", "block_month", "block_hour",
            "heating_w"}],
 "building": {"area_m2", "sum_of_peaks_w", "block_w", "block_month", "block_hour", "heating_w"},
 "inputs": {"project": {name: {"value", "source"}}, "profiles": {key: {name: {"value", "source"}}}},
 "notes": [str]}
```

**Project inputs** (asked once, kept with the runs): `design_weather` (or `months`), `room_dry_bulb_c`,
`room_rh_pct`, `supply_dry_bulb_c`, `ground_reflectance`, `outside_surface_coefficient_w_m2k` (ho - for the
engine's a/ho), `heating_outdoor_dry_bulb_c`, `heating_room_dry_bulb_c`, `unconditioned_temp_c` (beyond a
partition to an unconditioned space), `ground_temp_c` (heating through a floor with nothing beneath),
`altitude_m`. **Profile inputs** (once per profile key; any can be overridden per Space):
`people_per_m2`, `sensible_w_each`, `latent_w_each`, `lighting_w_per_m2`, `equipment_w_per_m2`,
`infiltration_ach`, `outdoor_air_ls_per_person`, `outdoor_air_ls_per_m2`. Each value bare or
`{"value", "source"}`; bare means `source: "instruction"`.

- [ ] **Step 1: Write the failing tests**

```python
import copy as _copy
import pytest
import heron_building_loads as B
import heron_hvac as H
import heron_takeoff as T
from test_takeoff import ROOM, copy

PROJECT = {"design_weather": "doha-0.4", "room_dry_bulb_c": 24, "room_rh_pct": 50,
           "supply_dry_bulb_c": 13, "ground_reflectance": 0.2,
           "outside_surface_coefficient_w_m2k": 17, "heating_outdoor_dry_bulb_c": 10,
           "heating_room_dry_bulb_c": 21, "unconditioned_temp_c": 35, "ground_temp_c": 25,
           "altitude_m": 10}
OFFICE = {"people_per_m2": 0.1, "sensible_w_each": 75, "latent_w_each": 55,
          "lighting_w_per_m2": 10, "equipment_w_per_m2": 15, "infiltration_ach": 0.3,
          "outdoor_air_ls_per_person": 2.5, "outdoor_air_ls_per_m2": 0.3}


def two_offices(second_normal):
    d = copy()
    other = _copy.deepcopy(d["spaces"][0])
    other.update(id=2, unique_id="u2", number="2", name="Office 02")
    other["faces"][0]["normal"] = second_normal
    d["spaces"].append(other)
    return d


def test_profile_key_strips_the_number():
    assert B.profile_key({"space_type": None, "name": "Office 01"}) == "Office"
    assert B.profile_key({"space_type": "Classroom", "name": "Room 4"}) == "Classroom"
    assert B.profile_key({"space_type": None, "name": ""}) == "(unnamed)"


def test_needs_asks_for_what_is_missing_and_nothing_else():
    t = T.read(ROOM)
    project = dict(PROJECT)
    del project["room_dry_bulb_c"]
    assert [a["input"] for a in B.needs(t, project, {"Office": OFFICE})] == ["room_dry_bulb_c"]


def test_needs_asks_per_profile():
    t = T.read(ROOM)
    office = dict(OFFICE)
    del office["lighting_w_per_m2"]
    asked = B.needs(t, PROJECT, {"Office": office})
    assert [(a["input"], a["for"]) for a in asked] == [("lighting_w_per_m2", "Office")]


def test_one_room_building_equals_the_engine_called_by_hand():
    t = T.read(ROOM)
    r = B.run(t, PROJECT, {"Office": OFFICE})
    s = r["spaces"][0]
    assert s["status"] == "ok", s["why"]
    by_hand = H.run("monthly_load", B.monthly_inputs(t, t.spaces[0], PROJECT, OFFICE))
    assert abs(s["cooling"]["peak"]["total_w"] - by_hand["data"]["peak"]["total_w"]) < 1e-6
    assert abs(r["building"]["block_w"] - s["cooling"]["peak"]["total_w"]) < 1e-6


def test_block_load_is_the_largest_hourly_sum_not_the_sum_of_peaks():
    r = B.run(T.read(two_offices([1.0, 0.0, 0.0])), PROJECT, {"Office": OFFICE})
    peaks = sum(s["cooling"]["peak"]["total_w"] for s in r["spaces"])
    assert abs(r["building"]["sum_of_peaks_w"] - peaks) < 1e-6
    assert r["building"]["block_w"] < peaks          # east and west peak at different hours


def test_one_bad_space_does_not_stop_the_building():
    d = two_offices([-1.0, 0.0, 0.0])
    d["types"]["bare"] = {"name": "Generic", "category": "Walls", "u_w_m2k": None,
                          "shgc": None, "absorptance": None}
    d["spaces"][1]["faces"][0]["type"] = "bare"
    r = B.run(T.read(d), PROJECT, {"Office": OFFICE})
    assert [s["status"] for s in r["spaces"]] == ["ok", "refused"]
    assert "Generic" in " ".join(r["spaces"][1]["why"])
    assert r["building"]["block_w"] > 0


def test_profile_values_are_range_checked():
    t = T.read(ROOM)
    for bad in ("ten", -5, 900):
        r = B.run(t, PROJECT, {"Office": dict(OFFICE, lighting_w_per_m2=bad)})
        assert r["spaces"][0]["status"] == "refused"
        assert "lighting_w_per_m2" in " ".join(r["spaces"][0]["why"])
        assert r["building"]["block_w"] == 0


def test_override_changes_one_space_only():
    t = T.read(two_offices([-1.0, 0.0, 0.0]))
    r = B.run(t, PROJECT, {"Office": OFFICE}, overrides={2: {"equipment_w_per_m2": 40}})
    a, b = r["spaces"]
    assert b["cooling"]["peak"]["total_w"] > a["cooling"]["peak"]["total_w"]


def test_every_input_carries_a_source_label():
    r = B.run(T.read(ROOM), PROJECT, {"Office": OFFICE})
    labels = [v["source"] for v in r["inputs"]["project"].values()]
    labels += [v["source"] for p in r["inputs"]["profiles"].values() for v in p.values()]
    assert labels and all(x in ("model", "instruction", "assumption") or x.startswith("standard:")
                          for x in labels)


def test_the_answer_says_it_is_not_hap():
    r = B.run(T.read(ROOM), PROJECT, {"Office": OFFICE})
    assert H.NOT_HAP in r["notes"] and H.STEADY_HOURS in r["notes"]


def test_save_and_load_round_trip(tmp_path, monkeypatch):
    monkeypatch.setenv("HERON_KNOWLEDGE", str(tmp_path))
    r = B.run(T.read(ROOM), PROJECT, {"Office": OFFICE})
    path = B.save("project-a", r)
    assert path.startswith(str(tmp_path)) and B.load("project-a")["run_id"] == r["run_id"]
    assert B.load("project-b") is None
```

- [ ] **Step 2: Run and see them fail** - `python -m pytest tests/test_building_loads.py -q`.

- [ ] **Step 3: Implement `brain/heron_building_loads.py`**

```python
"""
Every Space's heating and cooling load from a model's take-off - docs/44 section 5.

Builds one heron_hvac input per Space from the take-off, the project's inputs
and the Space's profile, runs the engine's own room calculations, and adds
the answers up by zone and building - the block load hour by hour, beside the
sum of the peaks. It adds no physics (one fact, one home: heron_hvac.py).

Standard library only.
"""
import datetime
import json
import os
import re

import heron_hvac as HVAC
import heron_takeoff as TAKEOFF

FORMAT = 1
PROJECT_KEYS = ("design_weather", "room_dry_bulb_c", "room_rh_pct", "supply_dry_bulb_c",
                "ground_reflectance", "outside_surface_coefficient_w_m2k",
                "heating_outdoor_dry_bulb_c", "heating_room_dry_bulb_c",
                "unconditioned_temp_c", "ground_temp_c", "altitude_m")
# name: (unit, why, low, high)
PROFILE_KEYS = {
    "people_per_m2": ("people/m2", "occupant density at the design hour", 0, 10),
    "sensible_w_each": ("W", "sensible heat per person at their activity", 0, 1000),
    "latent_w_each": ("W", "latent heat per person at their activity", 0, 1000),
    "lighting_w_per_m2": ("W/m2", "lighting power density", 0, 500),
    "equipment_w_per_m2": ("W/m2", "equipment power density", 0, 500),
    "infiltration_ach": ("ACH", "infiltration air changes per hour", 0, 20),
    "outdoor_air_ls_per_person": ("L/s per person", "outdoor air per person (62.1 Rp)", 0, 50),
    "outdoor_air_ls_per_m2": ("L/s per m2", "outdoor air per floor area (62.1 Ra)", 0, 50),
}
OA_NOTE = ("outdoor air per Space is the breathing-zone sum, people x Rp + area x Ra; zone "
           "air distribution effectiveness and system ventilation efficiency are the "
           "ventilation calculation's and are not applied here")


class _Refused(ValueError):
    pass


def _value(v):
    return v.get("value") if isinstance(v, dict) else v


def _labelled(d):
    out = {}
    for k, v in (d or {}).items():
        if _value(v) is None:
            continue
        out[k] = dict(v) if isinstance(v, dict) else {"value": v, "source": "instruction"}
        out[k].setdefault("source", "instruction")
    return out


def profile_key(space):
    if space.get("space_type"):
        return space["space_type"]
    name = re.sub(r"[\s\-_]*\d+$", "", (space.get("name") or "").strip())
    return name or "(unnamed)"


def _checked(profile):
    out = {}
    for k, (unit, why, low, high) in PROFILE_KEYS.items():
        raw = _value(profile.get(k))
        try:
            v = float(raw)
        except (TypeError, ValueError):
            raise _Refused("%s must be a number in %s - got %r" % (k, unit, raw))
        if not low <= v <= high:
            raise _Refused("%s must be between %s and %s %s - got %s" % (k, low, high, unit, raw))
        out[k] = v
    return out


def _opaque(recs, ho):
    out = []
    for r in recs:
        r = dict(r)
        a = r.pop("absorptance", None)
        if a is None:
            raise _Refused("%s has no solar absorptance in the model - set its type's "
                           "thermal properties" % r["name"])
        r["absorptance_over_ho"] = float(a) / ho
        out.append(r)
    return out


def monthly_inputs(t, space, project, profile):
    s = TAKEOFF.surfaces(t, space)
    if s["refused"]:
        raise _Refused("; ".join(s["refused"]))
    p = _checked(profile)
    area = float(space["area_m2"])
    people = int(round(p["people_per_m2"] * area))
    ho = float(_value(project["outside_surface_coefficient_w_m2k"]))
    i = {"floor_area_m2": area, "room_height_m": space.get("height_m"),
         "latitude_deg": t.site["latitude_deg"], "longitude_deg": t.site["longitude_deg"],
         "utc_offset_h": t.site["utc_offset_h"],
         "ground_reflectance": _value(project["ground_reflectance"]),
         "room_dry_bulb_c": _value(project["room_dry_bulb_c"]),
         "room_rh_pct": _value(project["room_rh_pct"]),
         "altitude_m": _value(project["altitude_m"]),
         "walls": _opaque(s["walls"], ho), "roofs": _opaque(s["roofs"], ho),
         "windows": s["windows"], "skylights": s["skylights"],
         "partitions": [dict(x, adjacent_temp_c=_value(project["unconditioned_temp_c"]))
                        for x in s["partitions"]],
         "people": {"count": people, "sensible_w_each": p["sensible_w_each"],
                    "latent_w_each": p["latent_w_each"]},
         "lighting": {"w_per_m2": p["lighting_w_per_m2"]},
         "equipment": [{"w_per_m2": p["equipment_w_per_m2"]}],
         "infiltration": {"ach": p["infiltration_ach"]},
         "outdoor_air_ls": people * p["outdoor_air_ls_per_person"]
                           + area * p["outdoor_air_ls_per_m2"]}
    if project.get("months") is not None:
        i["months"] = _value(project["months"])
    else:
        i["design_weather"] = _value(project["design_weather"])
    return {k: v for k, v in i.items() if v not in (None, [])}


def heating_inputs(t, space, project, profile):
    s = TAKEOFF.surfaces(t, space)
    if s["refused"]:
        raise _Refused("; ".join(s["refused"]))
    p = _checked(profile)
    beyond = lambda key: _value(project[key])
    surf = [{"name": r["name"], "area_m2": r["area_m2"], "u_w_m2k": r["u_w_m2k"]}
            for r in s["walls"] + s["roofs"] + s["windows"] + s["skylights"]]
    surf += [dict(r, adjacent_temp_c=beyond("unconditioned_temp_c")) for r in s["partitions"]]
    surf += [dict(r, adjacent_temp_c=beyond("ground_temp_c")) for r in s["floors"]]
    i = {"floor_area_m2": float(space["area_m2"]), "room_height_m": space.get("height_m"),
         "surfaces": surf, "room_dry_bulb_c": beyond("heating_room_dry_bulb_c"),
         "outdoor_dry_bulb_c": beyond("heating_outdoor_dry_bulb_c"),
         "infiltration": {"ach": p["infiltration_ach"]}, "altitude_m": beyond("altitude_m")}
    return {k: v for k, v in i.items() if v not in (None, [])}
```

Then, in the same file:

- `needs(t, project, profiles)`: every `PROJECT_KEYS` entry absent or None in `project` (with `months`
  standing in for `design_weather`), then, for every profile key that a placed Space maps to, every
  `PROFILE_KEYS` entry absent or None in that profile. `offer`: run `HVAC.run("monthly_load", {})` once,
  index its `missing` entries by `input`, and copy the matching entry's `reference` text where there is one
  (that is where the engine keeps its offered figures and their standard); `None` otherwise.
- `run(...)`:
  1. `qa = TAKEOFF.qa(t)`; `failed = {f["space"] for f in qa if f["level"] == "FAIL"}`.
  2. For each Space: not placed → `left out`; in `failed` → `refused`, `why` = its FAIL texts. Else
     `profile = dict(profiles.get(key) or {}, **(overrides or {}).get(space["id"], {}))`; build
     inputs (a `_Refused` → `refused` with its sentence); `cool = HVAC.run("monthly_load", ...)`,
     `heat = HVAC.run("heating_load", ...)`, `air = HVAC.run("supply_airflow", {"sensible_load_w":
     cool peak sensible, "latent_load_w": cool peak latent, "supply_dry_bulb_c": ..., "room_dry_bulb_c":
     ..., "room_rh_pct": ..., "altitude_m": ..., "floor_area_m2": ..., "room_height_m": ...})`. Any status
     other than `ok` → that status, `why` = the engine's `refused` sentences or `"missing: <input>"` lines.
  3. Block: for each group (zone name or "(no zone)", and the building), sum `total_w` per `(month,
     hour)` over its `ok` Spaces; `block_w` the largest, with its month and hour; `sum_of_peaks_w` the sum
     of the Spaces' peaks; `heating_w` the sum of `loss_w` (heating has no diversity in time).
  4. `notes` = `[HVAC.NOT_HAP, HVAC.STEADY_HOURS, OA_NOTE]` + every `assumed` line from the engine's
     answers, de-duplicated, in order.
  5. `inputs` = `{"project": _labelled(project), "profiles": {k: _labelled(v) for k, v in
     profiles.items()}}`.
  6. `run_id` from the local time, `"%Y%m%d-%H%M%S"`.
- `save` / `load` as in the interfaces. `load` with no `run_id` returns the newest file; a folder that does
  not exist returns `None`.

Before writing step 2's engine calls, run `HVAC.run("monthly_load", monthly_inputs(...))` once in a scratch
script on the test building and read `ignored` - if the engine reports any key the runner sent as ignored,
remove it from the inputs rather than leave it.

- [ ] **Step 4: Run the tests** - `python -m pytest tests/test_building_loads.py tests/test_takeoff.py -q` and
  `python tests/test_hvac.py`; all PASS.

- [ ] **Step 5: Commit**

```bash
git add brain/heron_building_loads.py tests/test_building_loads.py
git commit -m "heron_building_loads: every Space through the room engine, zone and block loads"
```

---

### Task 4: The Revit take-off reader - REPORT_SPACE_ENVELOPE

Build it with the `heron-fragment-author` agent's conventions (`.claude/agents/heron-fragment-author.md`):
search first, write the card at DRAFT, compile for every release, write its proof plan with a negative case,
and add ONE store row (put_fragment one row; never rebuild the shared store).

**Files:**
- Create: `brain/fragments/report-space-envelope/fragment.yaml`, `impl/any/fragment.cs`, `tests/cases.yaml`
- Modify: whatever count lines `python tools/check-docs.py` reports as drifted (recompute, never type)

**Interfaces:**
- Needs: `doc`, `elements` (the Spaces - from the selection or `expect_from` a finder; an empty list means
  every Space in the document, collected inside).
- Provides: `takeoffJson` (role result, string - **format 1 exactly as Task 2 defines it**), `spaces`
  (int, result), `findings` (IList<string>, accounting). One JSON string because a fragment's list output
  reaches the caller as its first three names (5b-195) - the same workaround `READ_ELEMENT_TABLE` uses.
- `risk: READ`. Opens no transaction.
- **Changed during the build, 2026-10-04 - two things the real code said:**
  1. **`doc` only, no `elements`.** The add-in refuses a fragment whose `elements` was never supplied
     (`needs_unbound`, "Nothing is selected in Revit") before the snippet runs, so "an empty list means
     every Space" could never be reached. The reader takes every Space in the document; "calculate the
     loads for this building" is a whole-model question and nothing is usually selected when it is asked.
     Ids are written as strings (`ElementId.ToString()`), not numbers - the brain treats them as strings.
  2. **The U-value parameter was renamed in Revit 2027.** `ANALYTICAL_HEAT_TRANSFER_COEFFICIENT` exists
     2020-2026 and is gone in 2027, which has `ANALYTICAL_THERMAL_TRANSMITTANCE` ("Thermal Transmittance
     (U)") - measured in each release's RevitAPI.xml. Neither name compiles everywhere, so the fragment
     finds it by name at run time (`Enum.TryParse`, the older first). Compiled on all eight releases;
     which one a real 2027 model fills is a NEEDS-CHECKING row.

- [ ] **Step 1: Card** - `id` the next free `FRG-MEP-0NN` (find with
  `grep -h '^id: FRG-MEP' brain/fragments/*/fragment.yaml | sort | tail -1`), `capability:
  REPORT_SPACE_ENVELOPE`, `kind: filter`, `domain: revit.mep`, `risk: READ`, `heron-status: DRAFT`,
  `revit: ["2020", "2021", "2022", "2023", "2024", "2025", "2026", "2027"]`, the `runtime` list
  `set-parameter-values-by-id` carries, utterances such as "read the spaces' walls and windows for the load
  calculation", "space envelope take-off", "which way do this space's outside walls face". Purpose text in
  Heron's own words from docs/44 section 4.

- [ ] **Step 2: Code** - `impl/any/fragment.cs`. The steps, each a short block:

1. **Collect.** `elements` filtered to `Space`; if none were given, every `Space` in the document via a
   `FilteredElementCollector` on `BuiltInCategory.OST_MEPSpaces`.
2. **Phase.** The phase of the active view (`VIEW_PHASE`), else the document's last phase.
3. **Per Space, not placed** (`Area <= 0` or no `Location`) → `"placed": false`, `"faces": []`.
4. **Per Space, placed.** `new SpatialElementGeometryCalculator(doc, new SpatialElementBoundaryOptions {
   SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish })`;
   `CalculateSpatialElementGeometry(space)`; for each `Face` of `GetGeometry()`, for each
   `SpatialElementBoundarySubface` of `GetBoundaryFaceInfo(face)`:
   - `side` from `SubfaceType`: `Side` → wall, `Top` → top, `Bottom` → bottom;
   - the host: `doc.GetElement(sub.SpatialBoundaryElement.HostElementId)` - for a linked element
     (`LinkedElementId` set) record a finding "linked boundary - read from the link" and treat `beyond` as
     `unknown`;
   - `area_m2` = `sub.GetSubface().Area * 0.09290304` (1 ft = 0.3048 m exactly);
   - `normal` = the subface's normal at the middle of its UV bounding box - outward from the Space's solid;
   - `beyond`: the probe point = subface centre + normal x (host thickness + 0.5 ft); `doc.GetSpaceAtPoint(
     probe, phase)` another Space → `space` with its id; else a `Wall` whose `WallType.Function` is Exterior,
     or a `RoofBase`, → `outside`; else a wall → `unconditioned`; else → `unknown`;
   - openings, on walls: `((Wall)host).FindInserts(true, false, false, true)`, each a `FamilyInstance` kept
     when its `get_FromSpace(phase)`, `get_ToSpace(phase)` or `Space` is this Space; kind from its category
     (Windows → window, Doors → door); area from the instance's, else the type's, built-in width and height
     parameters (`WINDOW_WIDTH`/`WINDOW_HEIGHT`, `DOOR_WIDTH`/`DOOR_HEIGHT`, then `FAMILY_WIDTH_PARAM`/
     `FAMILY_HEIGHT_PARAM`); a size that cannot be read → a finding, and the opening is left out, never
     guessed;
   - a curtain wall (`((Wall)host).CurtainGrid != null`): each panel of `GetPanelIds()` as `curtain_panel`
     with its area from `HOST_AREA_COMPUTED`; a panel whose type is a wall or a door is `door`.
5. **Types met.** For each type id seen on a face or an opening: name (`FamilyName: Name` for a family
   type), category, and `ANALYTICAL_HEAT_TRANSFER_COEFFICIENT` → `u_w_m2k`,
   `ANALYTICAL_SOLAR_HEAT_GAIN_COEFFICIENT` → `shgc`, `ANALYTICAL_ABSORPTANCE` → `absorptance`; a parameter
   absent or with no value → `null`, never 0. The internal unit of a heat transfer coefficient is
   kg/(s3.K), which IS W/(m2.K), so it is read as is - THE FIRST THING CHECKED ON A REAL MODEL (cases.yaml).
6. **Site.** `doc.SiteLocation` latitude and longitude (radians → degrees), `TimeZone` (hours), `Elevation`
   (ft → m), `PlaceName`; `project_to_true_north_deg` from
   `doc.ActiveProjectLocation.GetProjectPosition(XYZ.Zero).Angle` (radians → degrees). The SIGN is checked
   on a rotated project (cases.yaml); the brain applies it in one place, `heron_takeoff.azimuth_deg`, so a
   wrong sign is fixed there, once.
7. **Current values and units.** `AsValueString()` of `ROOM_DESIGN_COOLING_LOAD_PARAM`,
   `ROOM_DESIGN_HEATING_LOAD_PARAM`, `ROOM_DESIGN_SUPPLY_AIRFLOW_PARAM` into `current` under the names Revit
   shows; the project's display symbols for power and airflow from the text after the number in those
   strings (no version-specific unit API).
8. **Terminals.** Air Terminals (`OST_DuctTerminal`) whose `Space` (phase) is this Space - their ids.
9. **Space fields.** `space_type` = name of `doc.GetElement(space.SpaceTypeId)` or null; `zone` =
   `space.Zone?.Name`; `area_m2`, `volume_m3` (x 0.028316846592), `height_m` from `UnboundedHeight`.
10. **Serialize** by hand into one string. Copy the JSON escaping helper `read-element-table`'s
    `impl/any/fragment.cs` uses for `tableJson` - do not add a JSON library reference.

Every API named exists from Revit 2020; the fragment author compiles for each release and stops if one does
not, recording it rather than working round it.

- [ ] **Step 3: cases.yaml** - positive: an L-shaped office with 15 Spaces gives 15 Spaces, a corner office
  two `outside` wall faces, a Space under a roof a `top` face `outside`, a corridor wall between two offices
  `space` with the other office's id. Negative: (1) one window's U and SHGC against its type's Properties on
  screen - the same numbers; (2) a project rotated 30 degrees - a wall drawn to face project north reports
  30 or 330, and which one fixes the sign; (3) a Space not enclosed is `placed: false` with no faces, never a
  zero-area row with faces.

- [ ] **Step 4: Gates** - `python tools/check-structure.py`, `python tools/check-metadata.py`,
  `python tools/check-routing.py` (new utterances must not take another card's questions - compare `find`
  on a scratch store against main), and the compile for every release.

- [ ] **Step 5: Commit** - stage the three files and any count line the docs check asked for, by path.

---

### Task 5: The seam and the tool - `revit_building_loads`

**Files:**
- Modify: `mcp/server/heron_brain.py` - `building_loads(takeoff_json, inputs, project=None,
  project_name=None)` next to `hvac()` (line ~1422): imports `heron_takeoff` and `heron_building_loads`
  inside the function (as `hvac()` imports its engine), reads the project's D-111 standards the way
  `_design` does, merges the last saved run's `inputs` under the given ones, returns `{"asked": [...],
  "qa": [...], "result": dict|None, "takeoff": Takeoff}`; one audit line `design.loads` naming how it ended,
  never the inputs.
- Modify: `mcp/server/heron_mcp_server.py` - the tool, beside `revit_edit_table` (line ~2150).
- Modify: `mcp/server/heron_tools.py` - risk row `revit_building_loads: READ` where the other tools' rows are.
- Test: `tests/test_building_loads.py` (the seam) and `tests/test_mcp_serves.py` if it lists tools.

**Interfaces:**
- `revit_building_loads(inputs: str = "", expect_from: str = "") -> str` - `inputs` is JSON
  `{"project": {...}, "profiles": {...}, "overrides": {...}}`, what the modeller has answered so far.
- Flow - mirror `revit_edit_table` for the read, the pin check and opening the page:
  1. Companion off → the same sentence `revit_edit_table` gives, and stop.
  2. `out = {}; said = _through(revit_read, reply_out=out)("REPORT_SPACE_ENVELOPE", "", expect_from)`;
     not a good reply → `return said`.
  3. `if pinned.check(reply): return said`.
  4. `answer = heron_brain.building_loads(provides["takeoffJson"], inputs, project, name)`.
  5. Open the panel: `LOADS_PANEL.open(document, answer, _pin_identity())`; open the browser the way
     `revit_edit_table` does when the page has not been seen.
  6. **If anything is asked**, the chat's answer is that list - project inputs first, each with its offered
     figure and its standard - plus "nothing was calculated". Otherwise: Spaces calculated / refused / left
     out; building block load in kW and TR with its month and hour; sum of peaks; heating; the three most
     common refusal reasons; "the full table is in the Heron Companion". **Never the rows.**

- [ ] **Step 1: tests** - stub `revit_read` the way `tests/test_companion.py` stubs the server's Revit side
  (read its setup first) to return `{"ok": True, "document": "t", "provides": {"takeoffJson":
  json.dumps(ROOM)}}`: with `PROJECT` and `{"Office": OFFICE}` the answer says 1 Space calculated and gives
  a kW figure; with `room_dry_bulb_c` absent it asks for it, says nothing was calculated, and no run file was
  written (point `HERON_KNOWLEDGE` at a temp folder and list it).
- [ ] **Step 2-4:** fail, implement, pass - `python -m pytest tests/test_building_loads.py
  tests/test_companion.py -q`.
- [ ] **Step 5: Commit** the three server files and the test, by path.

---

### Task 6: The Companion Loads panel

**Files:**
- Modify: `mcp/companion/heron_companion.py` - a `LoadsPanel` class beside `Tables` (line ~451),
  `LOADS_PANEL = LoadsPanel()`, and routes in `_Handler`: `GET /api/loads`, `POST /api/loads/recalculate`,
  `POST /api/loads/report`, `POST /api/loads/finalize` - each behind `_api_ok()` exactly as
  `/api/table/apply` is.
- Modify: `mcp/companion/static/index.html` - `<section class="card" id="loads" aria-labelledby="h-loads">`
  after `#table`; `companion.js` - poll `/api/loads` with the existing polling, render, post;
  `companion.css` - inputs and results side by side, stacked under 900 px.
- Modify: `mcp/server/heron_mcp_server.py` - set the hooks where the others are set (line ~5127):
  `LOADS_PANEL.recalculate = _loads_recalculate`, `.report = _loads_report`, `.finalize = _loads_finalize`.
- Modify: `mcp/server/heron_tools.py` - `COMPANION_ACTIONS["companion_loads_finalize"] = (MODIFY,
  "run_fragment_write")`, with a comment that recalculate and report touch no model and so have no row.
- Test: `tests/test_companion.py` - a new section.

**Interfaces:**
- `LoadsPanel.open(document, answer, identity)`; `.current() -> dict`; `.recalculate(body) -> dict`;
  `.report() -> dict`; `.finalize() -> dict`. The last three call `self.<hook>` when set, else answer
  `{"ok": False, "said": "the chat that opened this is no longer connected - nothing was done"}`. **The
  class holds data only - no arithmetic, no BIM rule** (`mcp/companion/README.md` rule 4).
- `GET /api/loads` → `{"document", "qa", "asked", "profiles", "project", "spaces", "zones", "building",
  "notes", "run_id", "report": {"html", "pdf", "csv"} | null, "finalized": {...} | null}`.
- `POST /api/loads/recalculate` body `{"project": {...}, "profiles": {...}, "overrides": {...}}` → the hook
  runs the brain on the **take-off already held** (no Revit call), saves the run, replaces what the panel
  holds, returns it.
- `POST /api/loads/finalize` → Task 7's hook.

- [ ] **Step 1: tests** - `LoadsPanel` returns what it was given; `recalculate` with no hook answers "no
  longer connected" and changes nothing; `POST /api/loads/finalize` without a redeemed pairing is refused
  (copy the existing `/api/table/apply` session test); the `PAGES` whitelist still serves only the page
  files; the existing "no outgoing connection" test still passes.
- [ ] **Step 2-4:** fail, implement, pass - `python -m pytest tests/test_companion.py -q`.
- [ ] **Step 5: page check** - serve the page the way docs/40 section 21's harness does, hand the panel a
  result from the Task 3 test building, load it in the Browser pane, confirm the Loads card renders with no
  console error (`read_console_messages`), at desktop width and at 375 px.
- [ ] **Step 6: Commit** by path.

**What the panel shows** (docs/44 section 7):
- **Asked** - when the brain still needs answers: one input per row with its offered figure and standard,
  a box to type the value, and *Use these* which posts them to recalculate.
- **Inputs** - one row per profile (people/m2, W each sensible and latent, lighting W/m2, equipment W/m2,
  ACH, OA L/s per person and per m2), each cell editable with its source shown as a small tag; a Space's own
  override row opens under that Space.
- **Results** - Space, zone, area, sensible, latent, total W, W/m2, TR, heating W, supply L/s, OA L/s, peak
  month and hour, status; refused and left-out rows with their reason. Zone and building rows show **block**
  and **sum of peaks** side by side.
- **QA** - always visible, FAIL first.
- **Runs** - every Recalculate saves a new run (Task 3's `save`), so a what-if is: change a value,
  Recalculate, and compare. A list of this project's runs (`GET /api/loads/runs`, newest first, from
  `heron_building_loads.load`'s folder) opens any earlier one read-only, with its block load beside the
  current run's. Add the route and a test that two Recalculates leave two runs listed.
- **Buttons** - *Recalculate*; *Report*; *Finalize to Revit*, disabled while nothing is `ok`, with the line:
  *writes the loads and airflows into N Spaces and M diffusers - two undo entries*.

---

### Task 7: Finalize - the values back into Revit

**Files:**
- Modify: `brain/heron_building_loads.py` - `finalize_rows(t, result) -> list[list[str]]`.
- Modify: `mcp/server/heron_mcp_server.py` - `_loads_finalize()`.
- Test: `tests/test_building_loads.py`.

**Interfaces:**
- `finalize_rows` returns `SET_PARAMETER_VALUES_BY_ID` rows `[str(id), unique_id, parameter, was, new]`
  for every `ok` Space: `Design Cooling Load` (total at the peak), `Design Heating Load` (loss),
  `Specified Supply Airflow` (supply L/s). `was` = the take-off's `current` string; `new` = the number in the
  take-off's `units`, with its symbol: `"%.0f W"`, `"%.2f kW"`, `"%.0f Btu/h"`, `"%.1f L/s"`, `"%.0f CFM"`,
  `"%.0f m3/h"`. Factors: 1 W = 3.412141633 Btu/h (IT Btu, 1055.05585262 J); 1 L/s = 2.118880003 CFM
  (1 ft3 = 28.316846592 L); 1 L/s = 3.6 m3/h. Any other symbol raises `ValueError` naming it - never guessed.
- `_loads_finalize()`:
  1. `text, result = _apply_table(rows, identity)` - the existing Apply: one SET_PARAMETER_VALUES_BY_ID,
     every row's `was` checked, one undo entry, refused whole if any row moved.
  2. If that applied and any `ok` Space has terminals: for each, `HVAC.run("terminal_flows", {"flow_ls":
     supply, "count": len(ids), "terminal_ids": ids})` and its `csv`; join the rows and run ONE
     `SET_AIR_TERMINAL_FLOW` through `_through(revit_change, reply_out=out, origin="companion")` with the
     input name that fragment's card declares for its rows (read `brain/fragments/set-air-terminal-flow/
     fragment.yaml` and use exactly that name and format).
  3. Read back: `READ_SPACE_LOADS` and `REPORT_SPACE_AIRFLOW` through `_through(revit_read, ...)` on the
     same Spaces; put each read-back value beside the calculated one in the panel's `finalized`.
  4. **Two writes are two undo entries.** The page says so. One entry needs both writes inside one
     TransactionGroup - an add-in change, recorded as a FRAGMENT-ISSUES row, not done here.

- [ ] **Step 1: tests**

```python
def test_finalize_writes_the_three_fields_for_each_ok_space():
    t = T.read(ROOM)
    r = B.run(t, PROJECT, {"Office": OFFICE})
    rows = B.finalize_rows(t, r)
    assert [x[2] for x in rows] == ["Design Cooling Load", "Design Heating Load",
                                   "Specified Supply Airflow"]
    assert rows[0][:2] == ["1", "u1"] and rows[0][3] == "0.00 W"
    assert rows[0][4] == "%.0f W" % r["spaces"][0]["cooling"]["peak"]["total_w"]


def test_finalize_converts_to_project_units():
    d = copy()
    d["units"] = {"power": "Btu/h", "airflow": "CFM"}
    t = T.read(d)
    r = B.run(t, PROJECT, {"Office": OFFICE})
    rows = {x[2]: x[4] for x in B.finalize_rows(t, r)}
    w = r["spaces"][0]["cooling"]["peak"]["total_w"]
    assert rows["Design Cooling Load"] == "%.0f Btu/h" % (w * 3.412141633)
    assert rows["Specified Supply Airflow"] == "%.0f CFM" % (r["spaces"][0]["supply_ls"] * 2.118880003)


def test_refused_space_is_never_written():
    d = two_offices([-1.0, 0.0, 0.0])
    d["types"]["bare"] = {"name": "Generic", "category": "Walls", "u_w_m2k": None,
                          "shgc": None, "absorptance": None}
    d["spaces"][1]["faces"][0]["type"] = "bare"
    t = T.read(d)
    rows = B.finalize_rows(t, B.run(t, PROJECT, {"Office": OFFICE}))
    assert {x[0] for x in rows} == {"1"}


def test_unknown_unit_refuses_finalize():
    d = copy()
    d["units"] = {"power": "kcal/h", "airflow": "L/s"}
    t = T.read(d)
    r = B.run(t, PROJECT, {"Office": OFFICE})
    with pytest.raises(ValueError, match="kcal/h"):
        B.finalize_rows(t, r)
```

- [ ] **Step 2-4:** fail, implement, pass.
- [ ] **Step 5: Commit** by path.

---

### Task 8: The load calculation sheet - HTML, CSV, PDF

**Files:**
- Create: `brain/heron_loads_report.py`
- Modify: `mcp/server/heron_mcp_server.py` - `_loads_report()` hook: writes into
  `<folder of the saved Revit model>/Heron loads/<run id>/` when the reply names a saved path, else into the
  run's own folder in the knowledge store; returns the three paths to the panel.
- Test: `tests/test_loads_report.py`

**Interfaces:**
- `html(result) -> str` - one self-contained page: inline CSS, no script, no external URL.
- `csv_text(result) -> str` - header + one row per Space: number, name, zone, area m2, sensible W, latent W,
  total W, W/m2, TR, heating W, supply L/s, OA L/s, peak month, peak hour, status.
- `takeoff_csv(t) -> str` - the surface take-off: one row per face and per opening - Space number, name,
  element id, type name, side, beyond, facing (degrees), area m2, U, SHGC - every row that went into a load,
  so a reviewer can check an area against the drawings. Written beside the other two files.
- `pdf(html_path, pdf_path, search=None) -> (bool, str)` - `search` defaults to the four usual places:
  `%ProgramFiles(x86)%` and `%ProgramFiles%` + `\Microsoft\Edge\Application\msedge.exe`, then
  `\Google\Chrome\Application\chrome.exe`; runs `[exe, "--headless", "--disable-gpu",
  "--no-pdf-header-footer", "--print-to-pdf=" + pdf_path, pathlib.Path(html_path).as_uri()]` with
  `subprocess.run(..., timeout=60)`; returns `(False, "no Edge or Chrome found - the HTML page is the
  report")` when none exists, and `(False, <why>)` on a non-zero exit or no file. Never raises for a missing
  browser. Runs only inside the hook, never at import.
- Sections, in order (docs/44 section 8): title block (project, model, date, run id, *prepared with Heron -
  a design aid*); design conditions with each value's source; standards in force (the D-111 record the run
  used); the QA list and the geometry confirmation; inputs per profile with sources; results per Space;
  per-Space components at the peak; zones and building - block and sum of peaks with month and hour,
  heating; refused and left-out Spaces with why; `notes` verbatim; the disclaimer - *a design aid from
  published methods; a peak estimate, not an hourly simulation like HAP; the engineer of record approves
  every value*. `tr { page-break-inside: avoid; }`.

- [ ] **Step 1: tests**

```python
import heron_loads_report as R


def built():
    t = T.read(ROOM)
    return B.run(t, PROJECT, {"Office": OFFICE})


def test_report_prints_the_numbers_the_run_used():
    r = built()
    page = R.html(r)
    assert "Office 01" in page
    assert "%.0f" % r["building"]["block_w"] in page


def test_report_is_self_contained_and_honest():
    page = R.html(built()).lower()
    assert "http://" not in page and "https://" not in page and "<script" not in page
    assert "design aid" in page and "not an hourly simulation" in page
    assert "compliant" not in page


def test_csv_has_one_row_per_space():
    lines = R.csv_text(built()).strip().splitlines()
    assert len(lines) == 2 and lines[0].startswith("number,")


def test_takeoff_csv_lists_every_face_and_opening():
    lines = R.takeoff_csv(T.read(ROOM)).strip().splitlines()
    assert len(lines) == 1 + 3               # header, west wall, its window, the roof


def test_no_browser_means_no_pdf_and_no_error(tmp_path):
    html_path = tmp_path / "r.html"
    html_path.write_text(R.html(built()), encoding="utf-8")
    ok, said = R.pdf(str(html_path), str(tmp_path / "r.pdf"), search=[str(tmp_path / "none.exe")])
    assert ok is False and "HTML page is the report" in said
    assert not (tmp_path / "r.pdf").exists()
```

(`B`, `T`, `ROOM`, `PROJECT`, `OFFICE` imported from the Task 2 and 3 tests.)
- [ ] **Step 2-4:** fail, implement, pass.
- [ ] **Step 5: on this PC** - write a report for the test building with Edge and open the PDF: sections in
  order, no table row split across a page.
- [ ] **Step 6: Commit** by path.

---

### Task 9: The skill, the docs, the registers, the gates, the pull request

**Files:**
- Modify: `brain/skills/space-airflow.yaml` - version 2: step 1 `revit_building_loads` (read, check, ask,
  calculate), step 2 the Companion review, step 3 Finalize; keep DRAFT. Append to docs/41 section 15 that
  this answers item 4 for this skill - append, never rewrite.
- Modify: `docs/44-building-loads-from-the-model.md` - append section 11 "What is built": each part, its test
  file, its status. `docs/41-hvac-design.md` section 13 - append that a building runner now exists.
- Create: `docs/needs-checking/group-<next id>.md` (the next id after the last file in
  `docs/needs-checking/`) - one row per Revit question: window and wall U, SHGC, absorptance against the
  Properties on screen; the True North sign on a rotated project; a curtain wall's panels; a room separator
  boundary; a ceiling that is not room-bounding; a linked boundary; the three Space fields written and read
  back in W and in Btu/h; the two undo entries.
- Modify: the last `docs/FRAGMENT-ISSUES*` 5b file - a new row at its END: Finalize is two undo entries until
  one TransactionGroup spans both writes.

- [ ] **Step 1:** `python tools/check-change.py --intent "building loads from the model" --area brain
  --risk low`, and capture before/after with `tools/change-evidence.py` per the heron-ship skill.
- [ ] **Step 2: the four gates** - `python tools/check-docs.py`, `python tools/check-metadata.py`,
  `python tools/check-structure.py`, `python tools/check-package.py`, `git diff --check`; then the seven
  CI-only checks the heron-ship skill lists, as far as this PC can run them. Report each PASS / FAIL /
  NOT RUN (why).
- [ ] **Step 3:** `python tools/check-gaps.py`; read its buckets, not its exit code.
- [ ] **Step 4: Pull request** - body ends with the Claude Code line. **Not merged** - the owner merges on
  his word after testing on the model he names.

---

## Added after the nine tasks, on the owner's word (2026-10-04)

The owner asked for the 3D view of phase 6 now, for the studied skill's other engineering ideas, and for
everything to come from the model ([docs/44 section 12](../../44-building-loads-from-the-model.md)). Built
on the same branch: `brain/heron_loads_view.py` and `tests/test_loads_view.py` (new);
`mcp/companion/static/loads3d.js` (new); REPORT_SPACE_ENVELOPE gains face outlines, opening positions and
D-59's `includeLinks` / `linksSearched`; **format 1 gains optional keys** - `loops` on a face, `centre`,
`width_m`, `height_m` on an opening, `link` on a face and a type, `links_read` on the take-off - which a
reader of the old shape simply does not see; gate 1 is built (`confirm`, `confirmed`, `fingerprint`, and
`finalize_rows` refuses an unconfirmed take-off); `heron_takeoff` gains `role`, `summary`, `compass` and
the separate-groups check.

## Changed after the whole-branch review (2026-10-04)

[docs/44 section 12.5](../../44-building-loads-from-the-model.md) lists each finding and its fix. What it changes in this plan:

- **Format 1 gains no keys for it**; the reader's look past a slab, its skylights, its panel and opening matching and its orphan-opening finding are all inside REPORT_SPACE_ENVELOPE.
- **Project inputs:** `heating_unconditioned_temp_c` joins `PROJECT_KEYS`; `door_absorptance` is asked only when an outside door's type has no absorptance; a face Revit found nothing beyond is asked as `beyond:<element>` (outside, unconditioned, conditioned or ground). Every project value is range-checked.
- **Results:** each zone and the building carry `coil_block_w` - with the outdoor air at the coil - beside the rooms' `block_w`; the result carries `site` and `takeoff_fingerprint`.
- **Gate 1 confirms the take-off before the report is final and before Finalize**, not before anything is calculated: a calculation is read-only and cheap, and the numbers help the check.
- **Not built, on purpose:** the Spaces' area against each level's floors (docs/44 section 12.5 says why).

## After the build - the proof (on the owner's word, on the model he names)

1. Deploy the add-in to Revit 2020, 2024 and 2027 with Revit closed; restart Revit, then the Claude app.
2. REPORT_SPACE_ENVELOPE on the named model: one corner office's faces against a hand take-off; the three
   negative cases of Task 4.
3. `revit_building_loads`; answer the questions; one Space's load checked by hand against `heron_hvac`.
4. Report; open the PDF.
5. Finalize on a rolled-back setup; read back with READ_SPACE_LOADS and REPORT_SPACE_AIRFLOW; Ctrl+Z.
6. Record the run. The fragment moves to PROVEN only with Ajmal PS's signature on that evidence.
