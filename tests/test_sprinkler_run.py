# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The sprinkler runner (docs/46 section 5): the model's network through the
unchanged `hydraulic` calculation, with every criterion asked and never
filled in.

    python tests/test_sprinkler_run.py

WHAT IT PROVES
  Nothing is solved while anything is asked; a sprinkler type with no K is
  asked by its name with what Revit holds offered; a fitting row the chart
  holds is offered with its figure and one it does not is asked plainly; the
  suggestion ticks the far heads, as many as the design area holds; a full
  run gives exactly what `hydraulic` gives on the same nodes and pipes typed
  by hand (one fact, one home); a head not ticked flows nothing; a value out
  of range refuses the run by name; a head that is not in the system refuses
  it; gate 1 holds for the same network and K, and breaks when a diameter or a
  K changes; a run is kept and read back; what the last run was told is
  carried, never over what this call says.

WHAT IT DOES NOT PROVE
  That any answer is right for a building - that is a comparison with a
  listed hydraulic program on the same remote area (docs/46 section 11).
"""

from __future__ import print_function

import ast
import copy
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests"))

import heron_fire as FIRE                                     # noqa: E402
import heron_sprinkler_run as R                               # noqa: E402
import test_sprinkler_takeoff as NET                          # noqa: E402

TEE = "tee or cross, flow turned 90 degrees|DN50"
ELBOW = "90 degree standard elbow|DN25"
GIVEN = {"criteria": {"density_mm_min": 4.1, "area_per_sprinkler_m2": 12,
                      "design_area_m2": 20, "min_pressure_bar": 0.5, "c_factor": 120},
         "k": {"t1": 80}, "fittings": {TEE: 3.05, ELBOW: 0.6},
         "standards": {"sprinkler_standard": "NFPA 13-2022"}}


def given(**changes):
    out = copy.deepcopy(GIVEN)
    out.update(changes)
    return out


def asked(result):
    return [a["input"] for a in result["asked"]]


def test_nothing_solved_while_asked():
    r = R.run(NET.net(), {})
    assert r["status"] == "missing" and r["answer"] is None
    # The standard first, asked once for the project (D-111), then the criteria.
    assert asked(r)[0] == "standards.sprinkler_standard"
    assert asked(r)[1:6] == ["criteria." + k for k in ("density_mm_min",
                                                       "area_per_sprinkler_m2",
                                                       "design_area_m2", "min_pressure_bar",
                                                       "c_factor")]
    # The offers are the engine's own words.
    c = [a for a in r["asked"] if a["input"] == "criteria.c_factor"][0]
    assert c["offer"] and "120" in c["offer"]
    d = [a for a in r["asked"] if a["input"] == "criteria.density_mm_min"][0]
    assert d["offer"] and "design_criteria" in d["offer"]
    # A standard kept for the project is not asked again.
    kept = R.run(NET.net(), {}, recorded={"sprinkler_standard": {"value": "NFPA 13-2022"}})
    assert "standards.sprinkler_standard" not in asked(kept)


def test_k_asked_by_name_with_what_revit_holds():
    r = R.run(NET.net(), given(k={}))
    k = [a for a in r["asked"] if a["input"] == "k.t1"][0]
    assert "Pendent: K80" in k["why"] and "2 heads" in k["why"]
    assert "80 L/min/bar^1/2" in k["offer"] and "unit not known" in k["offer"]


def test_fittings_offered_from_the_chart_or_asked():
    r = R.run(NET.net(), given(fittings={}))
    rows = dict((a["input"], a) for a in r["asked"])
    tee = rows["fittings." + TEE]["offer"]
    # C is given, so the chart's 2 in tee is adjusted to this pipe's bore by the engine.
    assert "for this pipe (52.5 mm bore, C 120)" in tee and "offered, not applied" in tee
    assert rows["fittings." + ELBOW]["offer"].startswith("not held")
    raw = R.run(NET.net(), given(fittings={}, criteria=dict(
        (k, v) for k, v in GIVEN["criteria"].items() if k != "c_factor")))
    tee = dict((a["input"], a) for a in raw["asked"])["fittings." + TEE]["offer"]
    assert "3.048 m (10 ft) for schedule 40 steel at C = 120" in tee, tee


def test_suggest_ticks_the_far_heads():
    heads, count, why = R.suggest(NET.net(), given())
    assert count == 2 and sorted(heads) == ["401", "402"] and "rounded up" in why
    one, count, _ = R.suggest(NET.net(), given(criteria=dict(GIVEN["criteria"],
                                                               design_area_m2=12)))
    # 402 is 7 m of DN50 away; 401 is 4 m of DN50 and 0.3 m of DN25 - the DN25 costs more.
    assert count == 1 and one == ["401"], one


def test_same_answer_as_the_engine_by_hand():
    r = R.run(NET.net(), given(operating=["401", "402"]))
    assert r["status"] == "ok", (r["refused"], r["asked"])
    nodes = copy.deepcopy(r["nodes"])
    pipes = [dict((k, v) for k, v in p.items() if k not in ("element", "segment"))
             for p in r["pipes"]]
    by_hand = FIRE.run("hydraulic", {"sprinkler_standard": "NFPA 13-2022", "source": "100",
                                     "density_mm_min": 4.1, "min_pressure_bar": 0.5,
                                     "c_factor": 120, "nodes": nodes, "pipes": pipes})
    assert by_hand["data"]["demand_lpm"] == r["answer"]["data"]["demand_lpm"]
    assert by_hand["data"]["source_bar"] == r["answer"]["data"]["source_bar"]
    # 3.05 m on the DN50 arm the tee meets and 0.6 m on the DN25 drop.
    eq = dict((p["element"], p["equivalent_length_m"]) for p in r["pipes"])
    assert eq["203"] == 3.05 and eq["204"] == 0.6


def test_a_head_not_ticked_is_pruned_exactly():
    r = R.run(NET.net(), given(operating=["402"]))
    d = r["answer"]["data"]
    assert list(d["heads"]) == ["402"]
    assert not [p for p in d["pipes"] if "401" in (p["from"], p["to"])]
    assert any("left out of the solve" in t for t in r["notes"])
    # The same network solved with the dry branch kept gives the same demand
    # and source pressure - the pruning is exact.
    nodes, pipes, _ = NET.T.network(NET.net(), {"t1": 80}, GIVEN["fittings"], ["402"], "100")
    for x in nodes:
        if x["id"] == "402":
            x["area_per_sprinkler_m2"] = 12
    whole = FIRE.run("hydraulic", {"sprinkler_standard": "NFPA 13-2022", "source": "100",
                                   "density_mm_min": 4.1, "min_pressure_bar": 0.5,
                                   "c_factor": 120, "nodes": nodes,
                                   "pipes": [dict((k, v) for k, v in p.items()
                                                  if k not in ("element", "segment"))
                                             for p in pipes]})
    assert abs(whole["data"]["demand_lpm"] - d["demand_lpm"]) < 1e-6
    assert abs(whole["data"]["source_bar"] - d["source_bar"]) < 1e-9


def test_out_of_range_refuses_by_name():
    r = R.run(NET.net(), given(criteria=dict(GIVEN["criteria"], density_mm_min=0.1)))
    assert r["status"] == "refused" and "density_mm_min" in r["refused"][0]
    r = R.run(NET.net(), given(criteria=dict(GIVEN["criteria"], density=4)))
    assert r["status"] == "refused" and "density" in r["refused"][0]


def test_head_not_in_the_system_refuses():
    r = R.run(NET.net(), given(operating=["401", "999"]))
    assert r["status"] == "refused" and "999" in r["refused"][0]


def test_a_model_fail_refuses_in_its_own_words():
    bad = NET.net()
    bad["elements"][1]["inner_diameter_m"] = None
    r = R.run(bad, given(operating=["401"]))
    assert r["status"] == "refused" and any("no inside diameter" in t for t in r["refused"])


def test_gate_one():
    n = NET.net()
    r = R.run(n, given(operating=["401", "402"]))
    assert not R.confirmed(r, n)
    R.confirm(r, n)
    assert R.confirmed(r, n)
    moved = NET.net()
    moved["elements"][1]["inner_diameter_m"] = 0.08
    assert not R.confirmed(r, moved)
    r["inputs"]["k"]["t1"]["value"] = 81
    assert not R.confirmed(r, n)


def test_kept_and_read_back_and_carried():
    folder = tempfile.mkdtemp()
    before = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = folder
    try:
        n = NET.net()
        r = R.run(n, given(operating=["401", "402"]))
        R.save("proj", r, network=n)
        kept = R.runs("proj", "9")
        assert len(kept) == 1 and kept[0]["status"] == "ok"
        back = R.load("proj", system_id="9")
        assert back["answer"]["data"]["governing"] == r["answer"]["data"]["governing"]
        assert R.load("proj", "../escape") is None
        carry = R.carried(back, {"criteria": {"c_factor": 100}}, heads=["402"])
        assert carry["criteria"]["c_factor"]["value"] == 100.0
        assert carry["criteria"]["density_mm_min"]["value"] == 4.1
        assert carry["k"]["t1"]["value"] == 80.0 and carry["operating"] == ["402"]
        blanked = R.carried(back, {"criteria": {"density_mm_min": ""}, "operating": []})
        assert "density_mm_min" not in blanked["criteria"] and blanked["operating"] == []
    finally:
        if before is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = before
        shutil.rmtree(folder, ignore_errors=True)


def test_summary_says_draft_and_never_compliant():
    r = R.run(NET.net(), given(operating=["401", "402"]))
    text = R.summary_text(r)
    assert "NOT yet confirmed" in text and "compliant" not in text.lower()
    assert "never fill" in R.summary_text(R.run(NET.net(), {}))


def test_imports_standard_library_only():
    allowed = {"datetime", "heapq", "io", "json", "math", "os", "re", "heron_fire",
               "heron_sprinkler_takeoff", "heron_designbasis"}
    tree = ast.parse(open(os.path.join(ROOT, "brain", "heron_sprinkler_run.py")).read())
    names = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            names.update(alias.name.split(".")[0] for alias in node.names)
        elif isinstance(node, ast.ImportFrom):
            names.add((node.module or "").split(".")[0])
    assert names <= allowed | {"__future__"}, names


if __name__ == "__main__":
    sys.exit(NET.run_all(sys.modules[__name__]))
