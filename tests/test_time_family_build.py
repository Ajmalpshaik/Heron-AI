# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The family-build timer - does it derive docs/04's quantities, and only those?

    python tests/test_time_family_build.py

tools/time-family-build.py reads a Claude Code transcript and the add-in's
audit trail and prints the quantities docs/04 section 8.7 quotes for a family
build: request to finish, the model's share, thinking, Heron's calls, Revit's
own time, Heron's time around Revit, forms built and seconds a form. Those
were worked out by hand twice (row 5b-378); this holds the tool that replaces
the hand to transcripts and audit lines written here, with known timestamps.

NO REAL TRANSCRIPT AND NO REAL AUDIT LINE IS USED. Every fixture is built in a
temporary folder by the helpers below, on an invented day, with invented
sessions - so the expected numbers are worked out in the comments beside each
section, by hand, from the timestamps.

WHAT THIS PROVES
  1. A BUILD LIKE 2026-10-09'S: two requests with a wait for the go-ahead, a
     parallel batch, thinking with its tokens and seconds - every quantity,
     each from the line it names.
  2. A BUILD LIKE 2026-10-11'S, FROM AN OLDER TRANSCRIPT: a long planning turn,
     CREATE_FAMILY_FORMS whose form count is read off its reply, a fragment's
     first run in its Revit session - and thinking tokens, thinking seconds
     and effort NOT MEASURED, because that transcript does not carry them.
  3. A FORMS REPLY THAT DOES NOT SAY HOW MANY: forms and seconds a form are
     NOT MEASURED, never guessed from the rows sent.
  4. THE NEGATIVE CASE: a transcript with no Heron call reports zero calls and
     NOT MEASURED for Revit's time - although an audit line falls inside the
     chat's own minutes.
  5. NO AUDIT GIVEN: Revit's time NOT MEASURED, the chat's quantities still
     measured.
  6. EVERY MEASURED NUMBER NAMES A FILE AND ITS LINES, and every NOT MEASURED
     one says why and carries no value.
  7. A WINDOW, A FOLDER OF AUDIT FILES (the brain's own file left out), a torn
     audit line, and a transcript that is not there.
  8. WHICH CAPABILITIES BUILD FORMS IS READ FROM THE CARDS, not typed.
  9. A REPLY STORED AS CLAUDE CODE STORES HERON'S - the JSON text
     {"result": "..."}, as the content itself and as one text part: its
     formCount is read and its hand-back counted from the reply inside, and
     a wrapped reply with no count is still NOT MEASURED (row 5b-1030).
"""

import json
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "time-family-build.py")

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

FAILURES = []

#: An invented day, model and set of Revit sessions - nothing here is real.
DAY = "2026-01-05T"
MODEL = "claude-test-model"


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def near(got, want, places=3):
    return got is not None and want is not None and round(got - want, places) == 0


# ---------------------------------------------------------------- fixtures

def ts(clock):
    return DAY + clock + "Z"


def prompt(clock, text):
    return {"type": "user", "timestamp": ts(clock), "isSidechain": False,
            "origin": {"kind": "human"},
            "message": {"role": "user", "content": text}}


def assistant(clock, msg, block, output, thinking_tokens=None, thinking_ms=None,
              effort="xhigh", speed="standard", sidechain=False):
    usage = {"input_tokens": 2, "output_tokens": output}
    if thinking_tokens is not None:
        usage["output_tokens_details"] = {"thinking_tokens": thinking_tokens}
    if speed is not None:
        usage["speed"] = speed
    line = {"type": "assistant", "timestamp": ts(clock), "isSidechain": sidechain,
            "message": {"id": msg, "role": "assistant", "model": MODEL,
                        "content": [block], "usage": usage}}
    if effort is not None:
        line["effort"] = effort
    if thinking_ms is not None:
        line["thinkingDurationMs"] = thinking_ms
    return line


def thought():
    return {"type": "thinking", "thinking": "", "signature": "invented"}


def said(text):
    return {"type": "text", "text": text}


def call(ident, name, **arguments):
    return {"type": "tool_use", "id": ident, "name": name, "input": arguments}


def result(clock, ident, text, as_string=False):
    """A tool's answer. Its content is a list of text parts, or - as_string -
    the text itself, the two shapes a transcript stores."""
    return {"type": "user", "timestamp": ts(clock), "isSidechain": False,
            "message": {"role": "user", "content": [
                {"type": "tool_result", "tool_use_id": ident,
                 "content": text if as_string else [{"type": "text", "text": text}]}]}}


def wrapped(text):
    """A reply as Claude Code stores one from Heron's server: the JSON text
    {"result": "..."}, its newlines written as backslash-n (row 5b-1030)."""
    return json.dumps({"result": text})


def audit(clock, op, ms, fragment=None, session="S1", ok=True, error=None):
    line = {"at": DAY + clock + "0000Z", "workflow": "wf", "op": op, "ok": ok,
            "session": session, "document": "Family1"}
    if error:
        line["error"] = error
    if fragment:
        line["fragment"] = fragment
    line["ms"] = ms
    return line


def write(path, lines):
    with open(path, "w", encoding="utf-8") as fh:
        for line in lines:
            fh.write(line if isinstance(line, str) else json.dumps(line))
            fh.write("\n")
    return path


HERON = "mcp__heron__"
EXTRUSION, SWEEP, FORMS = "create-family-extrusion", "create-family-sweep", "create-family-forms"
WRITE = "run_fragment_write"

R1 = "Kept in Family1.\n\n  formId  aaa-1"
R2 = "Kept in Family1.\n\n  formId  aaa-2"
R3 = "Kept in Family1.\n\n  formId  bbb-1"
METHOD = "M" * 100


def build_a(folder):
    """2026-10-09's shape. The timeline, line by line (file line: seconds
    after the prompt, and what the interval ending there is):

         2  0.0   prompt
         3 20.0   model 20.0 (thinking, 15 000 ms, 2 500 tokens)
         4 21.0   model  1.0  heron_method
         5 21.5   tool   0.5
         6        a SIDECHAIN line - skipped
         7 31.5   model 10.0 (thinking, 8 000 ms, 1 000 tokens)
         8 32.0   model  0.5  revit_change c1
         9 32.0   model  0.0  revit_change c2
        10 32.7   tool   0.7
        11 33.0   tool   0.3
        12 40.0   model  7.0  "Go ahead?"
        13 53.0   person 13.0 "yes"
        14 63.0   model 10.0  revit_change c3
        15 63.6   tool   0.6
        16 70.0   model  6.4  "Done."

    request to finish 70.0; waiting 13.0; active 57.0; model 54.9; tool 2.1.
    """
    transcript = write(os.path.join(folder, "chat-a.jsonl"), [
        {"type": "queue-operation", "operation": "enqueue", "timestamp": ts("09:59:59.000")},
        prompt("10:00:00.000", "build a test family"),
        assistant("10:00:20.000", "A1", thought(), 3000, 2500, 15000),
        assistant("10:00:21.000", "A1", call("h1", HERON + "heron_method", job="family-creation"),
                  3000, 2500),
        result("10:00:21.500", "h1", METHOD),
        assistant("10:00:25.000", "SIDE", said("a subagent's line"), 10, 0, sidechain=True),
        assistant("10:00:31.500", "A2", thought(), 1200, 1000, 8000),
        assistant("10:00:32.000", "A2", call("c1", HERON + "revit_change",
                                             capability="CREATE_FAMILY_EXTRUSION"), 1200, 1000),
        assistant("10:00:32.000", "A2", call("c2", HERON + "revit_change",
                                             capability="CREATE_FAMILY_EXTRUSION"), 1200, 1000),
        result("10:00:32.700", "c1", R1),
        result("10:00:33.000", "c2", R2),
        assistant("10:00:40.000", "A3", said("Two forms built. Go ahead with the sweep?"), 100, 0),
        prompt("10:00:53.000", "yes"),
        assistant("10:01:03.000", "A4", call("c3", HERON + "revit_change",
                                             capability="CREATE_FAMILY_SWEEP"), 400, 0),
        result("10:01:03.600", "c3", R3),
        assistant("10:01:10.000", "A5", said("Done."), 50, 0),
    ])
    trail = write(os.path.join(folder, "audit-202601.jsonl"), [
        audit("09:00:00.000", WRITE, 2000, SWEEP),                  # 1 sweep's first run in S1
        audit("10:00:32.400", WRITE, 300, EXTRUSION),               # 2 extrusion's first in S1
        audit("10:00:32.900", WRITE, "50", EXTRUSION),              # 3 an old line: ms quoted
        audit("10:00:32.950", WRITE, 10, EXTRUSION, ok=False, error="refused"),  # 4
        audit("10:00:45.000", WRITE, 999, EXTRUSION, session="S2"),  # 5 another chat
        "{torn",                                                    # 6
        audit("10:01:03.500", WRITE, 100, SWEEP),                   # 7
    ])
    return transcript, trail


FORMS_REPLY = "Kept in Family1.\n\n  formCount  12\n  formIds    a,b,c"
FORMS_REPLY_NO_COUNT = "Kept in Family1.\n\n  formIds    a,b,c"
EXTRUSION_REPLY = "Kept in Family1.\n\n  formId  d"
#: Parses as JSON, but holds no string under "result" - kept as it is.
NOT_A_WRAPPER = json.dumps({"result": 12, "note": "not a reply's text"})


def build_b(folder, says_count=True, shape=None):
    """2026-10-11's shape, from a transcript too old to carry thinking tokens,
    thinking seconds or effort:

        1  0.0    prompt
        2  600.0  model 600.0 (thinking - the planning turn)
        3  601.0  model   1.0  revit_change f1 CREATE_FAMILY_FORMS
        4  603.0  tool    2.0  "formCount  12"
        5  610.0  model   7.0  revit_change f2 CREATE_FAMILY_EXTRUSION
        6  610.2  tool    0.2
        7  620.0  model   9.8  "Built."

    request to finish 620.0; model 617.8; tool 2.2; 13 forms.

    shape None writes both replies as plain text. "string" and "parts" write
    the FORMS reply wrapped as Claude Code stores Heron's - the content the
    JSON text itself, or that text as one text part - and the extrusion's
    reply as NOT_A_WRAPPER, which is JSON and no wrapper.
    """
    reply = FORMS_REPLY if says_count else FORMS_REPLY_NO_COUNT
    other = EXTRUSION_REPLY
    if shape:
        reply, other = wrapped(reply), NOT_A_WRAPPER
    as_string = shape == "string"
    transcript = write(os.path.join(folder, "chat-b.jsonl"), [
        prompt("11:00:00.000", "build the generator"),
        assistant("11:10:00.000", "B1", thought(), 50000, effort=None, speed=None),
        assistant("11:10:01.000", "B1", call("f1", HERON + "revit_change",
                                             capability="CREATE_FAMILY_FORMS",
                                             values="forms=C:\\Temp\\heron-work\\rows.txt"),
                  50000, effort=None, speed=None),
        result("11:10:03.000", "f1", reply, as_string),
        assistant("11:10:10.000", "B2", call("f2", HERON + "revit_change",
                                             capability="CREATE_FAMILY_EXTRUSION"),
                  300, effort=None, speed=None),
        result("11:10:10.200", "f2", other, as_string),
        assistant("11:10:20.000", "B3", said("Built."), 20, effort=None, speed=None),
    ])
    trail = write(os.path.join(folder, "audit-b-202601.jsonl"), [
        audit("10:59:00.000", WRITE, 700, EXTRUSION, session="S9"),  # 1 before the chat
        audit("11:10:02.000", "info", 5, session="S9"),              # 2 not a fragment run
        audit("11:10:02.900", WRITE, 1800, FORMS, session="S9"),     # 3 forms' first in S9
        audit("11:10:10.150", WRITE, 40, EXTRUSION, session="S9"),   # 4
    ])
    return transcript, trail


def build_none(folder):
    """No Heron call at all - a Read, and an answer."""
    return write(os.path.join(folder, "chat-n.jsonl"), [
        prompt("10:00:30.000", "what does this file say"),
        assistant("10:00:35.000", "N1", call("r1", "Read", file_path="x.txt"), 40, 0),
        result("10:00:35.100", "r1", "file text"),
        assistant("10:00:39.000", "N2", said("It says hello."), 10, 0),
    ])


# ---------------------------------------------------------------- running

def run(args):
    got = subprocess.run([sys.executable, TOOL] + list(args), capture_output=True,
                         text=True, encoding="utf-8", timeout=120)
    return got.returncode, got.stdout + got.stderr


def as_json(args):
    code, out = run(list(args) + ["--json"])
    try:
        return code, json.loads(out)
    except ValueError:
        print(out[:2000])
        return code, None


def value(report, key):
    entry = (report or {}).get("quantities", {}).get(key) or {}
    return entry.get("value")


def status(report, key):
    entry = (report or {}).get("quantities", {}).get(key) or {}
    return entry.get("status")


def lines_from(report, key, name):
    """The lines a quantity says it came from, in the file whose basename is
    `name`."""
    entry = (report or {}).get("quantities", {}).get(key) or {}
    found = []
    for source in entry.get("from") or []:
        if os.path.basename(source.get("file", "")) == name:
            found.extend(source.get("lines") or [])
    return sorted(found)


# ---------------------------------------------------------------- sections

def section_a(folder):
    print()
    print("1. A build like 2026-10-09's - every quantity, from the line it names")
    print("-" * 72)
    transcript, trail = build_a(folder)
    code, r = as_json([transcript, "--audit", trail])
    check(code == 0 and r is not None, "it reads both and answers, exit 0")
    if r is None:
        return

    check(near(value(r, "request_to_finish_s"), 70.0),
          "request to finish 70.0 s - the prompt on line 2 to the last line, 16")
    check(lines_from(r, "request_to_finish_s", "chat-a.jsonl") == [2, 16],
          "... and it names lines 2 and 16")
    check(near(value(r, "waiting_on_person_s"), 13.0),
          "13.0 s waiting for the go-ahead, the interval ending at line 13")
    check(lines_from(r, "waiting_on_person_s", "chat-a.jsonl") == [13],
          "... and it names line 13")
    check(near(value(r, "active_s"), 57.0), "active 57.0 s - request to finish less the wait")
    check(near(value(r, "model_s"), 54.9), "the model's own turns 54.9 s")
    check(lines_from(r, "model_s", "chat-a.jsonl") == [3, 4, 7, 8, 9, 12, 14, 16],
          "... from the eight assistant lines, the sidechain line 6 skipped")
    check(near(value(r, "model_share_of_active"), 54.9 / 57.0),
          "the model's share of active time, 96.3%")
    check(near(value(r, "model_share_of_request"), 54.9 / 70.0),
          "and of request to finish, 78.4%")
    check(near(value(r, "tool_s"), 2.1), "tool calls 2.1 s")
    check(near(value(r, "thinking_s"), 23.0) and
          lines_from(r, "thinking_s", "chat-a.jsonl") == [3, 7],
          "thinking 23.0 s from thinkingDurationMs on lines 3 and 7")
    check(value(r, "thinking_tokens") == 3500,
          "thinking tokens 3 500 - usage.output_tokens_details, each response once")
    check(value(r, "output_tokens") == 4750, "output tokens 4 750, each response once")

    longest = (r.get("quantities", {}).get("longest_model_turn") or {})
    check(near(longest.get("value"), 21.0) and longest.get("thinking_tokens") == 2500
          and longest.get("output_tokens") == 3000,
          "the longest model turn: 21.0 s, 2 500 thinking tokens, 3 000 output")
    check(lines_from(r, "longest_model_turn", "chat-a.jsonl") == [3, 4],
          "... on lines 3 and 4")
    check(r.get("quantities", {}).get("effort", {}).get("value") == {"xhigh": 5},
          "effort as recorded: xhigh on all five responses")
    check(r.get("quantities", {}).get("model", {}).get("value") == {MODEL: 5},
          "the model as recorded")
    check(r.get("quantities", {}).get("speed", {}).get("value") == {"standard": 5},
          "the speed as recorded")

    requests = r.get("requests") or []
    check([(q.get("line"), round(q.get("seconds"), 3)) for q in requests]
          == [(2, 40.0), (13, 17.0)],
          "two requests: line 2 took 40.0 s to its turn's end, line 13 took 17.0 s")

    check(value(r, "heron_calls") == 4 and value(r, "revit_change_calls") == 3,
          "4 Heron calls, 3 of them revit_change")
    check(r.get("quantities", {}).get("heron_calls", {}).get("by_tool")
          == {"heron_method": 1, "revit_change": 3}, "... by tool")
    check(lines_from(r, "revit_change_calls", "chat-a.jsonl") == [8, 9, 14],
          "... on lines 8, 9 and 14")
    check(value(r, "heron_batches") == 3, "3 batches - one per response that called Heron")
    check(value(r, "revit_change_handback_chars") == len(R1) + len(R2) + len(R3),
          "the revit_change hand-back, in characters")

    check(near(value(r, "revit_fragment_s"), 0.46) and value(r, "revit_fragment_runs") == 4,
          "Revit's own time 0.46 s over 4 fragment runs - the refused one counts its 10 ms")
    check(lines_from(r, "revit_fragment_s", "audit-202601.jsonl") == [2, 3, 4, 7],
          "... from audit lines 2, 3, 4 and 7, joined to this chat's calls by time")
    check(near(value(r, "revit_fragment_median_ms"), 75.0), "median 75 ms a run")
    check(value(r, "revit_first_runs") == 1 and near(value(r, "revit_first_runs_s"), 0.3),
          "one fragment's first run in its Revit session, 0.3 s of it")
    check(lines_from(r, "revit_first_runs", "audit-202601.jsonl") == [2],
          "... line 2 - the sweep ran first at line 1, before this chat")
    check(value(r, "audit_lines_not_joined") == 1 and
          lines_from(r, "audit_lines_not_joined", "audit-202601.jsonl") == [5],
          "line 5, another chat's run inside these minutes, is not this chat's")

    check(near(value(r, "heron_overhead_per_batch_median_s"), 0.57),
          "Heron's time around Revit, a median 0.57 s a batch (0.64 and 0.50)")
    check(near(value(r, "heron_overhead_per_call_median_s"), 0.32),
          "... and 0.32 s a call (0.32, 0.32, 0.50)")
    overhead = r.get("quantities", {}).get("heron_overhead_per_call_median_s") or {}
    check(near(overhead.get("low"), 0.32) and near(overhead.get("high"), 0.5)
          and overhead.get("count") == 3, "... its range and count")
    check(value(r, "heron_no_revit_calls") == 1 and
          near(value(r, "heron_no_revit_median_s"), 0.5),
          "heron_method reached no Revit: 1 call, 0.5 s")
    check(status(r, "heron_server_s_per_call") == "NOT MEASURED",
          "the server's own seconds a call: NOT MEASURED - kept in memory only")

    check(value(r, "forms_built") == 3, "3 forms - two extrusions and a sweep Revit kept")
    check(lines_from(r, "forms_built", "audit-202601.jsonl") == [2, 3, 7],
          "... from audit lines 2, 3 and 7; the refused run on line 4 built none")
    check(near(value(r, "seconds_per_form"), 70.0 / 3), "23.3 s a form")

    unread = (r.get("audit") or [{}])[0].get("unreadable")
    check(unread == [6], "the torn audit line 6 is counted, never fatal")
    check((r.get("transcript") or {}).get("sidechain_skipped") == [6],
          "the sidechain transcript line 6 is named as skipped")

    code, out = run([transcript, "--audit", trail])
    check(code == 0 and "70.0 s" in out and "chat-a.jsonl" in out
          and "audit-202601.jsonl" in out, "the text report names both files")
    check("NOT MEASURED" in out and "heron_companion" in out,
          "and says why the server's own time is not measured")


def section_b(folder):
    print()
    print("2. A build like 2026-10-11's, from an older transcript")
    print("-" * 72)
    transcript, trail = build_b(folder)
    code, r = as_json([transcript, "--audit", trail])
    check(code == 0 and r is not None, "exit 0")
    if r is None:
        return
    check(near(value(r, "request_to_finish_s"), 620.0), "request to finish 620.0 s")
    check(near(value(r, "model_s"), 617.8) and near(value(r, "tool_s"), 2.2),
          "model 617.8 s, tool 2.2 s")
    check(near(value(r, "model_share_of_request"), 617.8 / 620.0), "the model's share, 99.6%")
    longest = r.get("quantities", {}).get("longest_model_turn") or {}
    check(near(longest.get("value"), 601.0) and longest.get("output_tokens") == 50000
          and longest.get("thinking_tokens") is None,
          "the planning turn: 601.0 s, 50 000 output tokens, thinking tokens unknown")
    for key in ("thinking_tokens", "thinking_s", "effort", "speed"):
        check(status(r, key) == "NOT MEASURED" and value(r, key) is None,
              "%s NOT MEASURED - this transcript does not carry it" % key)
    check(value(r, "thinking_tokens") != 50000,
          "the output tokens are never passed off as thinking tokens")

    check(value(r, "revit_fragment_runs") == 2 and near(value(r, "revit_fragment_s"), 1.84),
          "Revit: 2 fragment runs, 1.84 s")
    check(value(r, "revit_other_ops") == 1 and near(value(r, "revit_other_s"), 0.005),
          "and one other operation, 5 ms, kept apart")
    check(value(r, "revit_first_runs") == 1 and near(value(r, "revit_first_runs_s"), 1.8),
          "CREATE_FAMILY_FORMS's first run in session S9: 1.8 s of the 1.84")
    check(near(value(r, "heron_overhead_per_batch_median_s"), 0.1775),
          "Heron's time around Revit, a median 0.1775 s a batch (0.195 and 0.16)")
    check(value(r, "forms_built") == 13, "13 forms: 12 from the FORMS reply, 1 extrusion")
    check(lines_from(r, "forms_built", "chat-b.jsonl") == [4] and
          lines_from(r, "forms_built", "audit-b-202601.jsonl") == [3, 4],
          "... from transcript line 4's formCount and audit lines 3 and 4")
    check(near(value(r, "seconds_per_form"), 620.0 / 13), "47.7 s a form")


def section_b2(folder):
    print()
    print("3. A FORMS reply that does not say how many")
    print("-" * 72)
    transcript, trail = build_b(folder, says_count=False)
    code, r = as_json([transcript, "--audit", trail])
    check(code == 0 and r is not None, "exit 0")
    if r is None:
        return
    check(status(r, "forms_built") == "NOT MEASURED" and value(r, "forms_built") is None,
          "forms built NOT MEASURED - never counted from the rows sent")
    check("formCount" in ((r.get("quantities", {}).get("forms_built") or {}).get("why") or ""),
          "... and the reason names formCount")
    check(status(r, "seconds_per_form") == "NOT MEASURED", "seconds a form NOT MEASURED")
    check(near(value(r, "revit_fragment_s"), 1.84), "Revit's time is still measured")


def section_none(folder):
    print()
    print("4. The negative case - no Heron call in the transcript")
    print("-" * 72)
    transcript = build_none(folder)
    _, trail = build_a(folder)
    code, r = as_json([transcript, "--audit", trail])
    check(code == 0 and r is not None, "exit 0")
    if r is None:
        return
    check(value(r, "heron_calls") == 0 and value(r, "revit_change_calls") == 0,
          "zero Heron calls, zero revit_change")
    for key in ("revit_fragment_s", "revit_fragment_runs", "heron_overhead_per_batch_median_s",
                "heron_overhead_per_call_median_s", "forms_built", "seconds_per_form"):
        check(status(r, key) == "NOT MEASURED" and value(r, key) is None,
              "%s NOT MEASURED" % key)
    check(near(value(r, "request_to_finish_s"), 9.0) and near(value(r, "model_s"), 8.9),
          "the chat itself is still measured: 9.0 s, the model 8.9 s")
    code, out = run([transcript, "--audit", trail])
    check(code == 0 and "NOT MEASURED" in out, "the text report says NOT MEASURED")


def section_no_audit(folder):
    print()
    print("5. No audit given")
    print("-" * 72)
    transcript, _ = build_a(folder)
    code, r = as_json([transcript])
    check(code == 0 and r is not None, "exit 0")
    if r is None:
        return
    check(status(r, "revit_fragment_s") == "NOT MEASURED" and
          "audit" in (r["quantities"]["revit_fragment_s"].get("why") or ""),
          "Revit's time NOT MEASURED, and the reason names the audit")
    check(status(r, "forms_built") == "NOT MEASURED", "forms NOT MEASURED")
    check(near(value(r, "request_to_finish_s"), 70.0) and value(r, "heron_calls") == 4,
          "the chat's quantities are still measured")


def section_sources(folder):
    print()
    print("6. Every number names where it came from")
    print("-" * 72)
    transcript, trail = build_a(folder)
    _, r = as_json([transcript, "--audit", trail])
    quantities = (r or {}).get("quantities") or {}
    bad = []
    for key, entry in sorted(quantities.items()):
        if entry.get("status") == "MEASURED":
            sources = entry.get("from") or []
            if not sources or not all(s.get("file") and s.get("lines") for s in sources):
                bad.append(key)
        elif entry.get("status") == "NOT MEASURED":
            if entry.get("value") is not None or not entry.get("why"):
                bad.append(key)
        else:
            bad.append(key)
    check(len(quantities) >= 20 and not bad,
          "every measured quantity names a file and lines, every other says why"
          + (" - not: %s" % ", ".join(bad) if bad else ""))


def section_edges(folder):
    print()
    print("7. A window, a folder of audit files, a transcript that is not there")
    print("-" * 72)
    transcript, trail = build_a(folder)
    _, r = as_json([transcript, "--audit", trail, "--until", ts("10:00:45.000")])
    check(r is not None and near(value(r, "request_to_finish_s"), 40.0)
          and len(r.get("requests") or []) == 1,
          "--until stops before the go-ahead: one request, 40.0 s")

    trails = os.path.join(folder, "audit-folder")
    os.makedirs(trails)
    shutil.copy(trail, os.path.join(trails, "audit-202601.jsonl"))
    # The brain's own file: a lookup inside heron_method's call. Read, it
    # would be counted as Revit's time; it is the brain's, and left out.
    write(os.path.join(trails, "audit-brain-202601.jsonl"),
          [audit("10:00:21.200", "lookup", 400, session="")])
    _, r = as_json([transcript, "--audit", trails])
    check(r is not None and near(value(r, "revit_fragment_s"), 0.46)
          and value(r, "heron_no_revit_calls") == 1,
          "a folder reads the add-in's audit-YYYYMM.jsonl and leaves the brain's file out")
    check(r is not None and [os.path.basename(a.get("file", "")) for a in r.get("audit") or []]
          == ["audit-202601.jsonl"], "... and names the one file it read")

    code, out = run([os.path.join(folder, "not-there.jsonl")])
    check(code == 2 and "not-there.jsonl" in out, "a transcript that is not there: exit 2, named")


def section_cards():
    print()
    print("8. Which capabilities build forms is read from the cards")
    print("-" * 72)
    code, r = as_json(["--form-cards"])
    check(code == 0 and r is not None, "--form-cards answers, exit 0")
    if r is None:
        return
    one, many = r.get("one_form") or [], r.get("many_forms") or []
    check(EXTRUSION in one and SWEEP in one, "an extrusion and a sweep build one form a run")
    check(FORMS in many, "CREATE_FAMILY_FORMS builds many, counted by its formCount")
    check("report-family-forms" not in one + many, "a READ that counts forms builds none")


def section_wrapped(folder):
    print()
    print("9. A reply stored as Claude Code stores Heron's - the JSON text {\"result\": ...}")
    print("-" * 72)
    # Row 5b-1030: five real CREATE_FAMILY_FORMS replies printed formCount
    # and the tool counted none, because the text it read was the wrapper,
    # with each newline written as two characters.
    for shape, how in (("string", "the content a string"), ("parts", "one text part")):
        sub = os.path.join(folder, shape)
        os.makedirs(sub)
        transcript, trail = build_b(sub, shape=shape)
        code, r = as_json([transcript, "--audit", trail])
        check(code == 0 and r is not None, "%s: exit 0" % how)
        if r is None:
            continue
        check(value(r, "forms_built") == 13,
              "%s: 13 forms - formCount 12 read from inside the wrapper, and 1 extrusion" % how)
        check(lines_from(r, "forms_built", "chat-b.jsonl") == [4],
              "%s: ... the 12 from transcript line 4" % how)
        check(near(value(r, "seconds_per_form"), 620.0 / 13), "%s: 47.7 s a form" % how)
        check(value(r, "revit_change_handback_chars") == len(FORMS_REPLY) + len(NOT_A_WRAPPER),
              "%s: the hand-back counts the reply's own characters, not the wrapper's, and "
              "keeps JSON with no \"result\" string as it is" % how)

    sub = os.path.join(folder, "no-count")
    os.makedirs(sub)
    transcript, trail = build_b(sub, says_count=False, shape="string")
    _, r = as_json([transcript, "--audit", trail])
    why = ((r or {}).get("quantities", {}).get("forms_built") or {}).get("why") or ""
    check(status(r, "forms_built") == "NOT MEASURED" and value(r, "forms_built") is None,
          "a wrapped reply with no formCount: forms built still NOT MEASURED")
    check("formCount" in why and "does not print a value" in why,
          "... with the tool's own reason")


def main():
    print("THE FAMILY-BUILD TIMER")
    print("=" * 72)
    if not os.path.isfile(TOOL):
        check(False, "tools/time-family-build.py exists")
        return 1
    folder = tempfile.mkdtemp(prefix="heron-time-build-")
    try:
        for i, section in enumerate((section_a, section_b, section_b2, section_none,
                                     section_no_audit, section_sources, section_edges)):
            sub = os.path.join(folder, str(i))
            os.makedirs(sub)
            section(sub)
        section_cards()
        sub = os.path.join(folder, "wrapped")
        os.makedirs(sub)
        section_wrapped(sub)
    finally:
        shutil.rmtree(folder, ignore_errors=True)
    print()
    if FAILURES:
        print("%d check(s) FAILED." % len(FAILURES))
        return 1
    print("Every check passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
