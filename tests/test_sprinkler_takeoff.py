# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-FPD-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The network a Revit sprinkler system gives for a hydraulic calculation
(docs/46 section 4): read whole or refused whole, checked before anything is
solved (gate 1), and turned into heron_fire's nodes and pipes.

    python tests/test_sprinkler_takeoff.py

WHAT IT PROVES
  Connectors Revit joined become one node; a pipe-to-pipe joint is one
  junction; a fitting's equivalent length lands once, on the smallest pipe
  that meets it; a sprinkler type with no confirmed K is a FAIL by name; a
  pipe with no bore is a FAIL; an open end is a WARN and a source candidate;
  a network in two pieces is a FAIL; a network over the solver's limit is a
  FAIL; the fingerprint ignores the findings and changes with a diameter; the
  module imports nothing beyond the standard library and heron_fire.

WHAT IT DOES NOT PROVE
  That REPORT_SPRINKLER_NETWORK reads a real model the way format 1 says -
  that is a run on a named model (docs/needs-checking).
"""

from __future__ import print_function

import ast
import copy
import os
import sys
import traceback

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_sprinkler_takeoff as T                           # noqa: E402


def _c(cid, point, *to):
    return {"id": cid, "type": "end", "point": list(point), "radius_m": None,
            "to": [list(x) for x in to]}


def _pipe(eid, dn, inner_mm, length, a, b):
    return {"id": eid, "kind": "pipe", "category": "Pipes", "family": "Pipe Types",
            "type": "Steel", "type_id": "pt", "inner_diameter_m": inner_mm / 1000.0,
            "nominal_diameter_m": dn / 1000.0, "length_m": length, "part_type": None,
            "angle_deg": None, "k": None, "level": "L1", "space": None, "connectors": [a, b]}


def _part(eid, kind, part, angle, *conns, **names):
    return {"id": eid, "kind": kind, "category": "Pipe Fittings", "family": names.get("family"),
            "type": names.get("type"), "type_id": names.get("type_id"),
            "inner_diameter_m": None, "nominal_diameter_m": None, "length_m": None,
            "part_type": part, "angle_deg": angle, "k": None, "level": "L1", "space": None,
            "connectors": list(conns)}


def _head(eid, type_id, point, *to):
    e = _part(eid, "sprinkler", None, None, _c(0, point, *to), family="Pendent",
              type="K80", type_id=type_id)
    e["category"] = "Sprinklers"
    e["k"] = {"connector": 1.23, "parameter": "80 L/min/bar^1/2"}
    return e


# A riser from the alarm valve to a tee; one arm, an elbow and a drop to head
# 401; the other arm two pipes joined end to end to head 402.
NET = {"format": 1, "document": "t",
       "systems": [{"id": "9", "name": "FP 1", "classification": "Wet Fire Protection",
                    "chosen": True}],
       "system": {"id": "9", "name": "FP 1", "classification": "Wet Fire Protection",
                  "base_equipment": "100"},
       "elements": [
           _part("100", "equipment", None, None, _c(0, (0, 0, 0), ("201", 0)),
                 family="Alarm Valve", type="DN80"),
           _pipe("201", 80, 77.9, 3.0, _c(0, (0, 0, 0), ("100", 0)), _c(1, (0, 0, 3), ("301", 0))),
           _part("301", "fitting", "Tee", None, _c(0, (0, 0, 3), ("201", 1)),
                 _c(1, (0, 0, 3), ("202", 0)), _c(2, (0, 0, 3), ("203", 0))),
           _pipe("202", 50, 52.5, 4.0, _c(0, (0, 0, 3), ("301", 1)), _c(1, (4, 0, 3), ("302", 0))),
           _part("302", "fitting", "Elbow", 90.0, _c(0, (4, 0, 3), ("202", 1)),
                 _c(1, (4, 0, 3), ("204", 0))),
           _pipe("204", 25, 26.6, 0.3, _c(0, (4, 0, 3), ("302", 1)),
                 _c(1, (4, 0, 2.7), ("401", 0))),
           _head("401", "t1", (4, 0, 2.7), ("204", 1)),
           _pipe("203", 50, 52.5, 4.0, _c(0, (0, 0, 3), ("301", 2)), _c(1, (-4, 0, 3), ("205", 0))),
           _pipe("205", 50, 52.5, 3.0, _c(0, (-4, 0, 3), ("203", 1)),
                 _c(1, (-7, 0, 3), ("402", 0))),
           _head("402", "t1", (-7, 0, 3), ("205", 1)),
       ],
       "findings": []}


def net():
    return copy.deepcopy(NET)


def raises(fn, *args):
    try:
        fn(*args)
    except ValueError as why:
        return str(why)
    raise AssertionError("expected a refusal")


def levels(found, level):
    return [f["text"] for f in found if f["level"] == level]


def test_read_refuses_whole():
    assert "format" in raises(T.read, {"format": 2})
    assert "JSON" in raises(T.read, "{not json")
    bad = net()
    del bad["elements"][0]["id"]
    assert "no id" in raises(T.read, bad)
    assert T.read(net()).system["name"] == "FP 1"


def test_nodes_join_and_junction():
    records, pipes = T.graph(net())
    # equipment, tee, elbow, two heads, and one pipe-to-pipe junction
    assert sorted(records) == ["100", "301", "302", "401", "402", "j203.1"], sorted(records)
    assert len(pipes) == 5
    assert records["j203.1"]["elevation_m"] == 3.0
    assert not any(r["open"] for r in records.values())


def test_fitting_rows_and_offer():
    rows = T.fitting_rows(net())
    assert sorted(rows) == ["90 degree standard elbow|DN25",
                            "tee or cross, flow turned 90 degrees|DN50"], sorted(rows)
    tee = rows["tee or cross, flow turned 90 degrees|DN50"]
    assert tee["count"] == 1 and tee["elements"] == ["301"]
    # The chart holds a 2 in tee (10 ft) and no 1 in elbow - the row, for the
    # runner to offer adjusted to the pipe; never applied here.
    assert tee["chart"][1] == "2" and tee["chart"][2] == 10 and tee["bore_mm"] == 52.5
    assert rows["90 degree standard elbow|DN25"]["chart"] is None


def test_equivalent_length_lands_once_on_smallest_pipe():
    eq = {"tee or cross, flow turned 90 degrees|DN50": 3.0,
          "90 degree standard elbow|DN25": 0.6}
    nodes, pipes, _notes = T.network(net(), {"t1": 80.0}, eq, ["401", "402"], "100")
    by = dict((p["element"], p) for p in pipes)
    # The tee meets 201 (DN80) and 202, 203 (DN50): the larger id of the two smallest.
    assert by["203"]["equivalent_length_m"] == 3.0 and by["202"]["equivalent_length_m"] == 0.0
    # The elbow meets 202 (DN50) and 204 (DN25).
    assert by["204"]["equivalent_length_m"] == 0.6
    assert sum(p["equivalent_length_m"] for p in pipes) == 3.6
    heads = [n for n in nodes if "k_lpm_bar" in n]
    assert sorted(n["id"] for n in heads) == ["401", "402"]
    assert by["201"]["bore_mm"] == 77.9


def test_only_operating_heads_carry_k():
    eq = {"tee or cross, flow turned 90 degrees|DN50": 0, "90 degree standard elbow|DN25": 0}
    nodes, _pipes, _notes = T.network(net(), {"t1": 80.0}, eq, ["402"], "100")
    assert [n["id"] for n in nodes if "k_lpm_bar" in n] == ["402"]


def test_missing_k_and_bore_fail():
    found = T.qa(net(), {}, "100")
    assert any("K80" in t and "no confirmed K-factor" in t for t in levels(found, "FAIL"))
    bad = net()
    bad["elements"][1]["inner_diameter_m"] = None
    found = T.qa(bad, {"t1": 80.0}, "100")
    assert any("201" in t and "no inside diameter" in t for t in levels(found, "FAIL"))
    assert "no inside diameter" in raises(
        T.network, bad, {"t1": 80.0}, {"tee or cross, flow turned 90 degrees|DN50": 0,
                                       "90 degree standard elbow|DN25": 0}, ["401"], "100")


def test_clean_network_has_no_fail():
    found = T.qa(net(), {"t1": 80.0}, "100")
    assert not levels(found, "FAIL"), levels(found, "FAIL")
    assert found[-1]["level"] == "INFO"


def test_open_end_is_warn_and_a_candidate():
    bad = net()
    # Cut the joint between 203 and 205: two open ends, and two pieces.
    bad["elements"][7]["connectors"][1]["to"] = []
    bad["elements"][8]["connectors"][0]["to"] = []
    found = T.qa(bad, {"t1": 80.0}, "100")
    assert any("open pipe end" in t for t in levels(found, "WARN"))
    assert any("2 pieces" in t for t in levels(found, "FAIL"))
    ids = [c["id"] for c in T.source_candidates(bad)]
    assert ids[0] == "100" and "e203.1" in ids and "e205.0" in ids


def test_no_source_without_base_equipment():
    bad = net()
    bad["system"]["base_equipment"] = None
    assert any("choose the source" in t for t in levels(T.qa(bad, {"t1": 80.0}), "FAIL"))


def test_no_system_chosen_lists_them():
    bad = net()
    bad["system"] = None
    bad["elements"] = []
    found = T.qa(bad)
    assert len(found) == 1 and "FP 1" in found[0]["text"]


def test_too_big_is_said_not_refused():
    big = net()
    for i in range(T.MAX_NODES):
        a = (10 + i, 0, 3)
        big["elements"].append(_pipe("9%04d" % i, 25, 26.6, 1.0, _c(0, a), _c(1, a)))
    found = T.qa(big, {"t1": 80.0}, "100")
    assert any("left out of the solve" in t for t in levels(found, "INFO"))


def test_prune_takes_dead_ends_only():
    eq = {"tee or cross, flow turned 90 degrees|DN50": 0, "90 degree standard elbow|DN25": 0}
    nodes, pipes, _ = T.network(net(), {"t1": 80.0}, eq, ["402"], "100")
    kept_nodes, kept_pipes, removed = T.prune(nodes, pipes, ["100", "402"])
    # The dry arm to 401 - the elbow, the drop and the head - goes; nothing else.
    assert removed == 2 and sorted(x["id"] for x in kept_nodes) == ["100", "301", "402",
                                                                      "j203.1"]
    assert sorted(p["element"] for p in kept_pipes) == ["201", "203", "205"]


def test_tap_splits_its_pipe():
    tapped = net()
    main = tapped["elements"][7]                          # pipe 203, 4 m along x
    main["connectors"].append(dict(_c(2, (-1, 0, 3), ("601", 0)), type="curve"))
    tapped["elements"].append(_part("601", "fitting", "TapPerpendicular", None,
                                    _c(0, (-1, 0, 3), ("203", 2)), _c(1, (-1, 0, 3), ("602", 0))))
    tapped["elements"].append(_pipe("602", 25, 26.6, 0.5, _c(0, (-1, 0, 3), ("601", 1)),
                                    _c(1, (-1, 0, 2.5), ("603", 0))))
    tapped["elements"].append(_head("603", "t1", (-1, 0, 2.5), ("602", 1)))
    records, segments = T.graph(tapped)
    parts = sorted((s["id"], round(s["length_m"], 6)) for s in segments if s["e"]["id"] == "203")
    assert parts == [("203/1", 1.0), ("203/2", 3.0)], parts
    assert T.fitting_kind(tapped["elements"][-3]) == T.TEE


def test_two_heads_at_one_point_fail():
    bad = net()
    bad["elements"].append(_head("499", "t1", (4, 0, 2.7), ("401", 0)))
    bad["elements"][6]["connectors"][0]["to"].append(["499", 0])
    assert any("meet at one point" in t for t in levels(T.qa(bad, {"t1": 80.0}, "100"), "FAIL"))


def test_joined_to_another_system_is_warn():
    other = net()
    other["elements"][9]["connectors"][0]["to"].append(["7777", 0])
    assert any("7777" in t and "not in this system" in t
               for t in levels(T.qa(other, {"t1": 80.0}, "100"), "WARN"))


def test_inch_sizes_snap():
    assert T.nominal_label(0.0254) == "DN25" and T.nominal_label(0.025) == "DN25"
    assert T.nominal_label(0.0508) == "DN50" and T.nominal_label(0.0635) == "DN65"
    assert "not a standard" in T.nominal_label(0.042)


def test_fitting_centre_length_added():
    centred = net()
    centred["elements"][4]["at"] = [4.0, 0.0, 3.1]      # the elbow's centre, 0.1 m off
    eq = {"tee or cross, flow turned 90 degrees|DN50": 0, "90 degree standard elbow|DN25": 0}
    _nodes, pipes, notes = T.network(centred, {"t1": 80.0}, eq, ["401"], "100")
    by = dict((p["element"], p) for p in pipes)
    assert by["202"]["length_m"] == 4.1 and by["204"]["length_m"] == 0.4
    assert any("cut to cut" in t for t in notes)


def test_fingerprint():
    a = T.fingerprint(net())
    other = net()
    other["findings"] = ["something said"]
    other["document"] = "renamed"
    assert T.fingerprint(other) == a
    other["elements"][6]["space"] = "a renamed Space"
    assert T.fingerprint(other) == a
    other["elements"][1]["inner_diameter_m"] = 0.08
    assert T.fingerprint(other) != a


def test_valve_named_by_chart_words():
    e = _part("501", "accessory", None, None, family="OS&Y Gate Valve", type="DN80")
    assert T.fitting_kind(e) == "gate valve"
    e = _part("502", "accessory", None, None, family="Flow Switch", type="DN50")
    assert T.fitting_kind(e).startswith("valve or device:")


def test_imports_standard_library_only():
    allowed = {"hashlib", "json", "heron_fire"}
    tree = ast.parse(open(os.path.join(ROOT, "brain", "heron_sprinkler_takeoff.py")).read())
    names = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            names.update(alias.name.split(".")[0] for alias in node.names)
        elif isinstance(node, ast.ImportFrom):
            names.add((node.module or "").split(".")[0])
    assert names <= allowed | {"__future__"}, names


def run_all(module):
    """Every test_ function in the module, in order; exit 1 if any failed."""
    own = [n for n, f in vars(module).items() if n.startswith("test_") and callable(f)
           and f.__module__ == module.__name__]
    failed = []
    for name in sorted(own, key=lambda n: getattr(module, n).__code__.co_firstlineno):
        try:
            getattr(module, name)()
            print("  ok    %s" % name)
        except Exception:                               # noqa: BLE001 - reported, not hidden
            print("  FAIL  %s" % name)
            traceback.print_exc(file=sys.stdout)
            failed.append(name)
    print("FAILED - %d of %d" % (len(failed), len(own)) if failed
          else "PASSED - %d of %d" % (len(own), len(own)))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(run_all(sys.modules[__name__]))
