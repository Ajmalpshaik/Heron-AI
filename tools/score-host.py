#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Did the host AI call the right Heron tools, in order, for what the modeller
said? Scored from a recorded chat.

    python tools/score-host.py SCENARIOS.json CHAT.jsonl          # the report
    python tools/score-host.py SCENARIOS.json CHAT.jsonl --json   # the same, as JSON

CHAT.jsonl is a Claude Code transcript - the chat's own file under
~/.claude/projects. SCENARIOS.json names what the modeller says and what
should follow (tools/host-scenarios.example.json is the shape):

    {"heron-host-scenarios": 1, "scenarios": [
      {"id": "count-ducts", "says": "how many ducts are on level 2",
       "expect": [{"tool": "heron_lookup"},
                  {"tool": "revit_read", "capability": "COUNT_ELEMENTS"}],
       "never": ["revit_change"]}]}

WHAT IT MEASURES. check-routing and score-routing measure Heron's own search -
whether the right card comes first for a sentence. Neither sees what the host
does with it. This reads the host's calls: for each scenario, the first time
the modeller's words appear in the chat, the Heron tools called before the
modeller spoke again.

THE RULES, one each:

  PASS       every expected step came, in order - other calls between them
             allowed. A step names one tool or a list of tools any one of
             which will do, and may name a `capability`, compared without case.
  FAIL       a step missing or out of order, the wrong capability, or a tool
             the scenario lists under `never` - and it says which.
  NOT FOUND  the chat never says the words. Never a pass.

CALIBRATE IT: tests/test_score_host.py scores deliberately broken expectations
on the same chat and holds that each FAILS. A scorer that passed them would
pass anything.

A REPORT, NOT A GATE. It exits 0 whatever it finds, and 2 only when a file it
was named cannot be read or is not the shape above. No Revit, no network, no
model: it reads two files.
"""

from __future__ import print_function

import io
import json
import re
import sys

SERVER = re.compile(r"^mcp__(?:[A-Za-z0-9_-]*_)?heron__(.+)$")
VERDICTS = ("PASS", "FAIL", "NOT FOUND")


class Unreadable(Exception):
    """A file this was named cannot be read, or is not the shape it needs."""


def _blocks(record):
    content = (record.get("message") or {}).get("content")
    if isinstance(content, str):
        return [{"type": "text", "text": content}]
    return [b for b in content or [] if isinstance(b, dict)]


def _words(text):
    return " ".join(re.findall(r"[a-z0-9]+", (text or "").lower()))


def read_chat(stream):
    """Every request the modeller made, in order: {"said", "calls"} - the
    Heron tools called until the modeller spoke again, each {"tool", "input"}.
    A tool result comes back as a user record and is not a request; a line
    that is not JSON - a chat cut short by a crash - is passed over."""
    turns = []
    for line in stream:
        line = line.strip()
        if not line:
            continue
        try:
            record = json.loads(line)
        except ValueError:
            continue
        if not isinstance(record, dict):
            continue
        blocks = _blocks(record)
        if record.get("type") == "user":
            text = " ".join(b.get("text") or "" for b in blocks if b.get("type") == "text")
            if text.strip() and not any(b.get("type") == "tool_result" for b in blocks):
                turns.append({"said": text, "calls": []})
        elif record.get("type") == "assistant" and turns:
            for block in blocks:
                if block.get("type") != "tool_use":
                    continue
                match = SERVER.match(str(block.get("name") or ""))
                if match:
                    turns[-1]["calls"].append({"tool": match.group(1),
                                               "input": block.get("input") or {}})
    return turns


def _matches(step, call):
    tools = step.get("tool")
    tools = tools if isinstance(tools, list) else [tools]
    if call["tool"] not in tools:
        return False
    wanted = step.get("capability")
    if wanted:
        return str(call["input"].get("capability") or "").strip().lower() == str(wanted).strip().lower()
    return True


def _step_text(step):
    tools = step.get("tool")
    text = " or ".join(tools) if isinstance(tools, list) else str(tools)
    return text + (" %s" % step["capability"] if step.get("capability") else "")


def score(scenarios, turns):
    """One verdict a scenario: {"id", "says", "verdict", "why", "calls"}."""
    out = []
    for scenario in scenarios:
        says = _words(scenario.get("says"))
        turn = next((t for t in turns if says and says in _words(t["said"])), None)
        verdict = {"id": scenario.get("id"), "says": scenario.get("says"), "verdict": "NOT FOUND",
                   "why": "the chat never says these words - not a pass", "calls": []}
        if turn is not None:
            calls = turn["calls"]
            verdict["calls"] = [c["tool"] + (" %s" % c["input"]["capability"] if c["input"].get("capability")
                                             else "") for c in calls]
            at, missing = 0, None
            for step in scenario.get("expect") or []:
                found = next((i for i in range(at, len(calls)) if _matches(step, calls[i])), None)
                if found is None:
                    missing = step
                    break
                at = found + 1
            banned = [c["tool"] for c in calls if c["tool"] in (scenario.get("never") or [])]
            if missing is not None:
                verdict["verdict"] = "FAIL"
                verdict["why"] = "expected %s%s, not called%s" % (
                    _step_text(missing), " after the steps before it" if at else "",
                    " - the calls were: %s" % ", ".join(verdict["calls"]) if verdict["calls"]
                    else " - no Heron tool was called")
            elif banned:
                verdict["verdict"] = "FAIL"
                verdict["why"] = "called %s, which this request must never call" % ", ".join(sorted(set(banned)))
            else:
                verdict["verdict"] = "PASS"
                verdict["why"] = "every expected step, in order"
        out.append(verdict)
    return out


def load_scenarios(path):
    try:
        with io.open(path, encoding="utf-8") as fh:
            data = json.load(fh)
    except (OSError, ValueError) as why:
        raise Unreadable("%s could not be read as JSON: %s" % (path, why))
    scenarios = data.get("scenarios") if isinstance(data, dict) else None
    if not isinstance(scenarios, list) or not all(
            isinstance(s, dict) and isinstance(s.get("says"), str) and isinstance(s.get("expect"), list)
            for s in scenarios):
        raise Unreadable("%s is not a scenarios file: it needs \"scenarios\", a list, each with \"says\" "
                         "(text) and \"expect\" (a list of steps)" % path)
    return scenarios


def main(argv):
    args = [a for a in argv if not a.startswith("--")]
    if len(args) != 2:
        print(__doc__.strip().splitlines()[2])
        return 2
    try:
        scenarios = load_scenarios(args[0])
        try:
            with io.open(args[1], encoding="utf-8", errors="replace") as fh:
                turns = read_chat(fh)
        except OSError as why:
            raise Unreadable("%s could not be read: %s" % (args[1], why))
    except Unreadable as why:
        print("score-host: %s" % why, file=sys.stderr)
        return 2
    verdicts = score(scenarios, turns)
    counts = dict((v, sum(1 for x in verdicts if x["verdict"] == v)) for v in VERDICTS)
    if "--json" in argv:
        print(json.dumps({"requests": len(turns), "counts": counts, "verdicts": verdicts}, indent=2))
        return 0
    print("score-host - %d scenario(s) against %d request(s) in the chat" % (len(verdicts), len(turns)))
    for v in verdicts:
        print("  %-9s %s - %s" % (v["verdict"], v["id"], v["why"]))
    print("%d PASS, %d FAIL, %d NOT FOUND. NOT FOUND is not a pass. A report: it exits 0 whatever it finds."
          % (counts["PASS"], counts["FAIL"], counts["NOT FOUND"]))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
