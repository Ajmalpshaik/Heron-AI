# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-QA-BIM-011
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The chat's `heron_checklist` tool, through the real fragment contracts (docs/50).

    python tests/test_checklist_tool.py

WHAT IT PROVES
  1. It is declared at revit_read's own level and operation - ANALYZE,
     run_fragment_read - because its run reads the model through that door.
  2. A checklist is saved through the real fragment cards: a typo in a value,
     a rule on an output the card does not declare and a capability that
     changes the model are each refused by name; HERON'S STARTER passes every
     check against today's cards, so the example cannot rot unseen.
  3. A run goes through revit_read's own body to a stand-in Revit - one
     run_fragment_read per check, never a write - and reads PASS and FAIL off
     the reply, with the failing ids; with no Revit it sends nothing and every
     check is NOT RUN.
  4. list, show and delete work, and an unknown action is named.

It needs the MCP SDK and exits 3 without it - NOT a pass. The store, the rule
and the run against a stand-in server, which need nothing, are
tests/test_checklists.py.
"""

from __future__ import print_function

import json
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real
# trail (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant()

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


class StandIn(object):
    """A Revit that answers every read from one reply and remembers each ask."""

    revit_version = "2024"
    pid = 4242

    def __init__(self, reply):
        self.reply, self.asked = reply, []

    def request(self, op, op_args=None, **_ignored):
        self.asked.append((op, dict(op_args or {})))
        return self.reply

    def close(self):
        pass


def reply_with(**provides):
    base = {"notLoaded": "0 item(s)", "unresolved": "0 item(s)", "unplaced": "0 item(s)",
            "unenclosed": "0 item(s)", "elements": "0 item(s)"}
    base.update(provides)
    return {"ok": True, "document": "Project1", "documentPath": "C:/jobs/Project1.rvt",
            "projectKey": "key-1", "wasActiveDocument": True, "provides": base,
            "providesCount": len(base), "elementsCounted": 1234}


def tool(home):
    print("the MCP tool")
    os.environ["HERON_DATA"] = home
    import heron_mcp_server as S
    import heron_tools as T
    check(T.TOOLS.get("heron_checklist") == (T.ANALYZE, "run_fragment_read"),
          "declared at revit_read's own level and operation")
    body = getattr(S.heron_checklist, "__wrapped__", S.heron_checklist)

    said = body("save", name="Typo", checks=json.dumps([
        {"sentence": "every room is placed", "capability": "SELECT_UNENCLOSED_ROOMS",
         "values": "includeRooms=true\nincludeSpacs=false", "rule": "unplaced == 0"}]))
    check("'includeSpacs' is not a value SELECT_UNENCLOSED_ROOMS takes" in said
          and "cannot run without a value for 'includeSpaces'" in said,
          "a typo in a value is refused by name, with the value it cannot run without")
    said = body("save", name="Wrong output", checks=json.dumps([
        {"sentence": "links loaded", "capability": "LIST_LINKED_MODELS", "rule": "unloaded == 0"}]))
    check("'unloaded' is not something LIST_LINKED_MODELS leaves behind - it leaves:" in said
          and "notLoaded" in said, "a rule on an output the card does not declare names what it does")
    said = body("save", name="Delete", checks=json.dumps([
        {"sentence": "delete the duplicates", "capability": "DELETE_ELEMENTS", "rule": "deleted == 0"}]))
    check("a check may only read" in said and "Not saved" in said,
          "a capability that changes the model is never a check")
    said = body("save", name="Bad json", checks="[{")
    check("JSON list" in said and "Nothing was saved" in said, "checks that are not JSON save nothing")
    check("starter" in body("list"), "with nothing saved, the list offers the starter")

    said = body("starter", name="Before issue")
    check(said.startswith("Saved 'Before issue'"),
          "Heron's starter passes every check against today's cards - %r" % said[:300])
    check("Before issue - 5 check(s)" in body("list"), "it is listed with its checks")
    shown = body("show", name="before issue")
    check("LIST_LINKED_MODELS, notLoaded == 0" in shown and "includeRooms=true" in shown,
          "show gives each check's capability, rule and values")

    real = S.binding.resolve
    try:
        revit = StandIn(reply_with(unenclosed="2 item(s) [311, 312]"))
        S.binding.resolve = lambda: revit
        S.pinned.forget()
        said = body("run", name="Before issue")
        ops = [a[0] for a in revit.asked if a[0].startswith("run_fragment")]
        check(ops and set(ops) == {"run_fragment_read"} and len(ops) == 5,
              "the run is five run_fragment_read requests, never a write - found %r" % ops)
        check(re.search(r"FAIL\s+no room is left unenclosed", said) is not None
              and "311, 312" in said and said.count("PASS") == 4,
              "four PASS and the failing check with its ids, read off the reply")
        check("4 meet the value checked, 1 do not, 0 not run" in said and "Project1" in said,
              "the counts, on the model it read")
        check(not re.search(r"\b(compliant|clean|healthy)\b", said, re.I), "and never a verdict")

        def gone():
            raise S.NotBound("No Revit is connected to this chat.")
        S.binding.resolve = gone
        S.pinned.forget()
        revit.asked[:] = []
        said = body("run", name="Before issue")
        check(not revit.asked and "0 meet the value checked, 0 do not, 5 not run" in said
              and "A check not run is not a pass" in said,
              "with no Revit nothing is sent, and every check is NOT RUN - never a pass")
    finally:
        S.binding.resolve = real
        S.pinned.forget()

    check(body("delete", name="Before issue").startswith("Deleted"), "it is deleted when asked")
    check("not 'explode'" in body("explode"), "an unknown action is named")


def main():
    try:
        import mcp  # noqa: F401
    except ImportError:
        print("NOT RUN - the MCP SDK is not installed")
        return 3
    home = tempfile.mkdtemp(prefix="heron-checklist-tool-")
    kept = dict((k, os.environ.get(k)) for k in ("HERON_DATA", "HERON_KNOWLEDGE"))
    os.environ["HERON_KNOWLEDGE"] = os.path.join(home, "knowledge")
    import heron_bridge_client as bridge
    real_discovery = bridge.DISCOVERY_DIR
    bridge.DISCOVERY_DIR = os.path.join(home, "bridges")
    os.makedirs(bridge.DISCOVERY_DIR)
    try:
        tool(os.path.join(home, "data"))
    finally:
        bridge.DISCOVERY_DIR = real_discovery
        for key, value in kept.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value
        shutil.rmtree(home, ignore_errors=True)
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    heron_checklist saves through the real contracts and runs through revit_read only")
    return 0


if __name__ == "__main__":
    sys.exit(main())
