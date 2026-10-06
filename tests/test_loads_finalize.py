# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Finalize, the server's own code, run against a stand-in Revit - docs/44 s12.8
and FRAGMENT-ISSUES 5b-335.

    python tests/test_loads_finalize.py

WHAT IT PROVES
  _loads_finalize hands the diffusers over by category with NO level - one
  placed with no level parameter is on none, and on 2026-10-06 none of three
  was handed over - and lets the file of ids pick which are written; puts
  every diffuser not written down to the reason SET_AIR_TERMINAL_FLOW gave,
  never to a list of maybes; looks for the Spaces schedule before making it,
  so a second Finalize says it is there instead of letting Revit refuse a
  second one of that name - and still makes it when the schedules cannot be
  read; and counts the undo entries it made.

WHAT IT DOES NOT PROVE
  That Revit does any of it. The stand-in answers the way the add-in's
  replies read on 2026-10-06; the proof is Group CC on a real model.

EXITS 3 when the MCP server will not import here (no MCP SDK) - NOT RUN,
which is not a pass. Every check below runs where it does import.
"""

from __future__ import print_function

import copy as _copy
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
for part in (("mcp", "client"), ("mcp", "server"), ("brain",), ("tests",)):
    sys.path.insert(0, os.path.join(ROOT, *part))

FAILURES = []


def check(condition, what):
    print("  %s %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def building():
    """One office with two diffusers in it, calculated and confirmed."""
    import heron_building_loads as B
    import heron_takeoff as T
    from test_takeoff import ROOM
    from test_building_loads import PROJECT, OFFICE
    d = _copy.deepcopy(ROOM)
    d["spaces"][0]["terminals"] = ["501", "502"]
    t = T.read(json.dumps(d))
    return t, json.dumps(d), B.confirm(B.run(t, PROJECT, {"Office": OFFICE}), t, "test")


class StandIn(object):
    """The add-in, as Finalize meets it through _through: one answer per capability. The
    take-off read again after the write holds what was written."""

    def __init__(self, server, takeoff_json, written_json, flows, schedules):
        self.calls = []
        self.reads = [takeoff_json, written_json]
        self.answers = {
            "REPORT_SPACE_ENVELOPE": {"ok": True, "provides": {"takeoffJson": takeoff_json}},
            "FILTER_ELEMENTS_BY_CATEGORY": {"ok": True,
                                            "provides": {"elements": "2 item(s) [a, b]"}},
            "SET_AIR_TERMINAL_FLOW": {"ok": True, "provides": flows},
            "REPORT_SPACE_AIRFLOW": {"ok": True, "provides": {}},
            "CREATE_SCHEDULE": {"ok": True, "provides": {"created": "1 item(s) [id 9]"}},
        }
        server._through = self.through
        server._apply_table = lambda rows, identity=None: ("Wrote the Space values.",
                                                           {"applied": True})
        server._moved_since = lambda identity: None
        server._schedule_names = lambda: schedules

    def through(self, tool, reply_out=None, origin="chat"):
        def call(capability, values="", expect_from=""):
            self.calls.append((capability, values, expect_from))
            reply = self.answers.get(capability, {"ok": False, "error": "not_stood_in"})
            if capability == "REPORT_SPACE_ENVELOPE":
                read = self.reads[min(len(self.ran(capability)) - 1, 1)]
                reply = {"ok": True, "provides": {"takeoffJson": read}}
            if reply_out is not None:
                reply_out["reply"] = reply
            return "%s answered" % capability
        return call

    def ran(self, capability):
        return [c for c in self.calls if c[0] == capability]


def finalize(server, flows, schedules):
    import heron_building_loads as B
    t, raw, result = building()
    written = json.loads(raw)
    written["spaces"][0]["current"] = dict((field, new) for _sid, _uid, field, _was, new
                                           in B.finalize_rows(t, result))
    revit = StandIn(server, raw, json.dumps(written), flows, schedules)
    return revit, server._loads_finalize(t, result, ("t", "", "1"))


def main():
    home = tempfile.mkdtemp(prefix="heron-finalize-")
    had = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        try:
            import heron_mcp_server as server
        except BaseException as absent:             # noqa: BLE001 - an install, not a defect
            print("Finalize against a stand-in Revit")
            print("  NOT RUN - the MCP server will not import here: %s: %s"
                  % (type(absent).__name__, absent))
            print("  This proves nothing either way. Install the SDK: pip install --user mcp")
            return 3
        import heron_building_loads as B

        print("Finalize hands the diffusers over with no level (5b-335)")
        revit, got = finalize(server, {"changed": "2", "alreadyThatFlow": "0 item(s)"},
                              [B.SCHEDULE_NAME])
        handed = revit.ran("FILTER_ELEMENTS_BY_CATEGORY")
        check(got.get("ok") and handed and handed[0][1] == "category=Air Terminals"
              + chr(10) + "levelId=none",
              "the air terminals are asked for by category with no level")
        check(len(revit.ran("SET_AIR_TERMINAL_FLOW")) == 1
              and revit.ran("SET_AIR_TERMINAL_FLOW")[0][2]
              == "filter-elements-by-category where category=Air Terminals",
              "and written in ONE call, from what that filter left - one undo entry")
        said = got.get("said") or ""
        check("Diffusers: 2 of 2 written" in said
              and "each read back through its connector" in said,
              "it says both diffusers were written, from the fragment's own count")
        check("Read back from Revit: 3 of 3 values hold what was written" in said,
              "and the Space values are read back from the take-off read again")

        print()
        print("A second Finalize finds the schedule already there")
        check(not revit.ran("CREATE_SCHEDULE"),
              "the schedule already in the model is not asked for again")
        check("is already in the model" in said
              and "2 new entries in Revit's undo list: the Space values, then the diffusers"
              in said, "it says the schedule is there, and counts two undo entries")

        print()
        print("Each diffuser not written, with the reason Revit gave")
        revit, got = finalize(server, {"changed": "1", "noFlowParameter": "1 item(s) [502]",
                                       "rowsUnmatched": "0"}, ["Space Schedule"])
        said = got.get("said") or ""
        check("Diffusers: 1 of 2 written" in said and "(502)" in said
              and "no parameter" in said,
              "the one not written is named, with its family's flow tied to no parameter")
        check("sit on a level" not in said and "already held that flow, sit" not in said,
              "never the old list of maybes")
        check(len(revit.ran("CREATE_SCHEDULE")) == 1 and "Made the schedule" in said,
              "with no such schedule in the model, the schedule is made")

        print()
        print("When the model's schedules cannot be read")
        revit, got = finalize(server, {"changed": "2"}, None)
        check(len(revit.ran("CREATE_SCHEDULE")) == 1,
              "the schedule is still asked for - its own refusal is then said")
    finally:
        if had is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = had
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASSED - Finalize hands the diffusers over with no level, says why one was not "
          "written, and makes the schedule once.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
