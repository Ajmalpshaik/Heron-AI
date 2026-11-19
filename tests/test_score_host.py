#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/score-host.py - did the host AI call the right Heron tools, in order,
for each thing the modeller said?

    python tests/test_score_host.py

check-routing and score-routing measure Heron's OWN search. Nothing measured
the host's choice: which Heron tools Claude Code called, in what order, for
the modeller's words. This holds the scorer, on a chat written here in the
shape Claude Code writes one:

  1. PASS          every expected call came, in order, other calls between
                   them allowed - a capability checked where one is named,
                   and one of several tools allowed where a step lists them.
  2. FAIL          a step missing or out of order, the wrong capability, or a
                   tool the scenario says must never be called - each saying
                   which, never just "failed".
  3. NOT FOUND     words the chat never said are NOT FOUND, never a pass.
  4. CALIBRATION   a deliberately broken expectation on the same chat must
                   FAIL - a scorer that passes it would pass anything.
  5. THE FILES     a scenarios file that is not one, or a chat that cannot be
                   read, exits 2 saying which; a report exits 0 whatever it
                   finds, as a report does.
"""

from __future__ import print_function

import importlib.util
import io
import json
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "score-host.py")
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    spec = importlib.util.spec_from_file_location("score_host", TOOL)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _use(n, tool, **args):
    return {"type": "tool_use", "id": "t%d" % n, "name": "mcp__heron__%s" % tool, "input": args}


def chat_lines():
    """Two of the modeller's requests, as Claude Code writes them."""
    rows = [
        {"type": "user", "message": {"role": "user", "content": "How many ducts are on Level 2?"}},
        {"type": "assistant", "message": {"id": "m1", "role": "assistant", "content": [
            {"type": "text", "text": "Looking it up."},
            _use(1, "heron_lookup", request="count ducts on a level", question=True)]}},
        {"type": "user", "message": {"role": "user", "content": [
            {"type": "tool_result", "tool_use_id": "t1", "content": "COUNT_ELEMENTS ..."}]}},
        {"type": "assistant", "message": {"id": "m2", "role": "assistant", "content": [
            {"type": "tool_use", "id": "x1", "name": "Read", "input": {"file_path": "/x"}},
            _use(2, "revit_read", capability="COUNT_ELEMENTS", values="category=Ducts")]}},
        {"type": "user", "message": {"role": "user", "content": [
            {"type": "tool_result", "tool_use_id": "t2", "content": "outcome: ANSWERED 126"}]}},
        {"type": "user", "message": {"role": "user", "content": [
            {"type": "text", "text": "move the selected ducts up 200 mm please"}]}},
        {"type": "assistant", "message": {"id": "m3", "role": "assistant", "content": [
            _use(3, "heron_lookup", request="move selected ducts up")]}},
        {"type": "assistant", "message": {"id": "m4", "role": "assistant", "content": [
            _use(4, "revit_change", capability="MOVE_ELEMENTS", values="dz=200")]}},
    ]
    return [json.dumps(r) for r in rows] + ["", "not json - a line a crash cut short"]


SCENARIOS = {"heron-host-scenarios": 1, "scenarios": [
    {"id": "count-ducts", "says": "how many ducts are on level 2",
     "expect": [{"tool": "heron_lookup"}, {"tool": "revit_read", "capability": "COUNT_ELEMENTS"}],
     "never": ["revit_change"]},
    {"id": "move-ducts", "says": "move the selected ducts up 200 mm",
     "expect": [{"tool": ["heron_lookup", "heron_resolve"]}, {"tool": "revit_change", "capability": "move_elements"}]},
    {"id": "never-said", "says": "purge the unused families",
     "expect": [{"tool": "revit_change"}]},
    # CALIBRATION - each of these is wrong on purpose, and must FAIL.
    {"id": "broken-order", "says": "how many ducts are on level 2",
     "expect": [{"tool": "revit_read"}, {"tool": "heron_lookup"}]},
    {"id": "broken-capability", "says": "how many ducts are on level 2",
     "expect": [{"tool": "revit_read", "capability": "LIST_LEVELS"}]},
    {"id": "broken-never", "says": "move the selected ducts up 200 mm",
     "expect": [{"tool": "heron_lookup"}], "never": ["revit_change"]},
]}


def scoring(S):
    print("1-4. scoring a chat")
    turns = S.read_chat(io.StringIO("\n".join(chat_lines())))
    check(len(turns) == 2, "two requests read; the tool results and the broken line are not requests - found %d"
          % len(turns))
    check([c["tool"] for c in turns[0]["calls"]] == ["heron_lookup", "revit_read"],
          "only Heron's calls are kept, in order - Read is not Heron's")
    verdicts = dict((v["id"], v) for v in S.score(SCENARIOS["scenarios"], turns))
    check(verdicts["count-ducts"]["verdict"] == "PASS", "the right tools in order, another call between: PASS")
    check(verdicts["move-ducts"]["verdict"] == "PASS",
          "one of two tools allowed, capability matched whatever its case: PASS")
    check(verdicts["never-said"]["verdict"] == "NOT FOUND", "words never said: NOT FOUND, never a pass")
    for name, words in (("broken-order", "heron_lookup"), ("broken-capability", "LIST_LEVELS"),
                        ("broken-never", "revit_change")):
        v = verdicts[name]
        check(v["verdict"] == "FAIL" and words in v["why"],
              "calibration %s: FAIL, saying %s - got %s: %s" % (name, words, v["verdict"], v["why"]))


def files():
    print("\n5. the files")
    folder = tempfile.mkdtemp(prefix="heron-score-host-")
    try:
        chat = os.path.join(folder, "chat.jsonl")
        with io.open(chat, "w", encoding="utf-8") as fh:
            fh.write(u"\n".join(chat_lines()))
        good = os.path.join(folder, "scenarios.json")
        with io.open(good, "w", encoding="utf-8") as fh:
            fh.write(json.dumps(SCENARIOS))
        bad = os.path.join(folder, "bad.json")
        with io.open(bad, "w", encoding="utf-8") as fh:
            fh.write(u'{"scenarios": "not a list"}')

        def run(*args):
            p = subprocess.run([sys.executable, TOOL] + list(args), capture_output=True, text=True)
            return p.returncode, p.stdout + p.stderr
        code, out = run(good, chat)
        check(code == 0, "a report exits 0 whatever it finds - got %d" % code)
        check("FAIL" in out and "PASS" in out and "NOT FOUND" in out and "broken-order" in out,
              "and says each scenario's verdict by name")
        code, out = run(bad, chat)
        check(code == 2 and "scenarios" in out, "a scenarios file that is not one: exit 2, saying so")
        code, out = run(good, os.path.join(folder, "missing.jsonl"))
        check(code == 2 and "missing.jsonl" in out, "a chat that cannot be read: exit 2, naming it")
        code, out = run(good, chat, "--json")
        check(code == 0 and json.loads(out).get("counts", {}).get("PASS") == 2, "--json gives the counts")
    finally:
        shutil.rmtree(folder, ignore_errors=True)


def main():
    if not os.path.isfile(TOOL):
        check(False, "tools/score-host.py is there")
    else:
        try:
            S = load()
            scoring(S)
        except Exception as why:                     # noqa: BLE001 - one failure, not a traceback
            check(False, "scoring raised %s: %s" % (type(why).__name__, why))
        try:
            files()
        except Exception as why:                     # noqa: BLE001 - one failure, not a traceback
            check(False, "files raised %s: %s" % (type(why).__name__, why))
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for what in FAILURES:
            print("  - %s" % what)
        return 1
    print("PASS    the host's Heron calls are scored against the modeller's words, calibrated")
    return 0


if __name__ == "__main__":
    sys.exit(main())
