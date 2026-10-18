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
 10. THE KEEP BOX, FROM REVIT'S JOURNAL (row 5b-1031): two boxes inside two
     runs, one in the chat's minutes outside every call, one before the
     chat - counted apart, a Discard counted, and Revit's own time less the
     boxes run by run. A shown stamp earlier than its run is cut to the
     run's start, and says so.
 11. THE NEGATIVE CASE: a journal with no Keep box - 0 boxes, and Revit's
     own time the same with the journal as without it.
 12. NO JOURNAL, NO OFFSET, TWO JOURNALS: NOT MEASURED with the flag named;
     without --journal-utc-offset only the whole journal's boxes are
     counted; a DRAFT's box is read too; a journal that is not there exits 2.
 13. THE BOX'S WORDS ARE THE ADD-IN'S: the tool reads the headline and the
     Keep link HeronKeepQuestion.cs shows.
"""

import json
import os
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "time-family-build.py")
KEEP_QUESTION = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronKeepQuestion.cs")
KEEP_GRANT = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronKeepGrant.cs")

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


#: The PC's clock in the journals below: three hours ahead of UTC, so the
#: journal's 15:00:11 is the chat's and the audit's 12:00:11Z.
OFFSET = "+03:00"
SHOWN = "' 0:< TaskDialog \"Keep what Heron just did?\""
ANSWER = "        \"TaskDialogResult\"  , \"Keep what Heron just did?\" ,  _"


def stamp(clock, letter="H"):
    return "'%s 05-Jan-2026 %s;" % (letter, clock)


def box(shown, answered, button):
    """A Keep box as a journal writes it: five lines."""
    return [stamp(shown), SHOWN, stamp(answered), "Jrn.Data  _", ANSWER, '        "%s"' % button]


def write_journal(path, lines, encoding="utf-16"):
    """Revit writes UTF-16 LE with a byte-order mark, or latin-1; Python's
    "utf-16" codec writes the mark and little-endian on every machine this
    runs on, but the mark and the order are spelled out to be sure."""
    text = "\r\n".join(lines) + "\r\n"
    with open(path, "wb") as fh:
        if encoding == "utf-16":
            fh.write(b"\xff\xfe" + text.encode("utf-16-le"))
        else:
            fh.write(text.encode(encoding))
    return path


def build_k(folder, early=False):
    """A build with D-126's Keep box. The chat (UTC):

        1 12:00:00   prompt
        2 12:00:10   revit_change k1 CREATE_FAMILY_EXTRUSION
        3 12:00:16   its result
        4 12:00:20   revit_change k2 CREATE_FAMILY_SWEEP
        5 12:00:30   its result
        6 12:00:40   revit_change k3 CREATE_FAMILY_EXTRUSION
        7 12:00:42   its result
        8 12:00:50   "Done."

    The audit - `at` is written once the job is over, so a run spans its
    `at` less its `ms` to its `at`:

        1 at 12:00:15.900, 5 500 ms - 10.4 to 15.9   extrusion, first in S1
        2 at 12:00:29.800, 9 000 ms - 20.8 to 29.8   sweep, first in S1
        3 at 12:00:41.500, 1 000 ms - 40.5 to 41.5   extrusion again

    The journal, local time three hours ahead (lines in brackets):

        D [4-9]    14:59:00 -> 14:59:02  Keep it     before the chat
        A [10-15]  15:00:11 -> 15:00:15  Keep it     4.0 s inside run 1
        B [16-20]  15:00:21.5 -> 15:00:29 Discard it 7.5 s inside run 2
        C [21-26]  15:00:33 -> 15:00:35.5 Keep it    2.5 s, outside every call
        [27-28]   another TaskDialog inside run 3 - not the Keep box

    Boxes in the chat's minutes 3, 14.0 s; inside runs 2, 11.5 s. Revit's own
    time 15.5 s, less the boxes 4.0 s; the first runs 14.5 s, less 3.0 s.
    `early` stamps box A's TaskDialog line at 15:00:10.000, before run 1
    began at 10.4: its journal seconds are 5.0, and 4.6 of them are in the run.
    """
    transcript = write(os.path.join(folder, "chat-k.jsonl"), [
        prompt("12:00:00.000", "build the pump"),
        assistant("12:00:10.000", "K1", call("k1", HERON + "revit_change",
                                             capability="CREATE_FAMILY_EXTRUSION"), 100, 0),
        result("12:00:16.000", "k1", "Kept in Family1.\n\n  formId  k-1"),
        assistant("12:00:20.000", "K2", call("k2", HERON + "revit_change",
                                             capability="CREATE_FAMILY_SWEEP"), 100, 0),
        result("12:00:30.000", "k2", "The modeller chose Discard in Revit"),
        assistant("12:00:40.000", "K3", call("k3", HERON + "revit_change",
                                             capability="CREATE_FAMILY_EXTRUSION"), 100, 0),
        result("12:00:42.000", "k3", "Kept in Family1.\n\n  formId  k-2"),
        assistant("12:00:50.000", "K4", said("Done."), 10, 0),
    ])
    trail = write(os.path.join(folder, "audit-k-202601.jsonl"), [
        audit("12:00:15.900", WRITE, 5500, EXTRUSION),
        audit("12:00:29.800", WRITE, 9000, SWEEP),
        audit("12:00:41.500", WRITE, 1000, EXTRUSION),
    ])
    journal = write_journal(os.path.join(folder, "journal.0001.txt"), [
        "'Build: invented for this test",
        stamp("14:58:00.000", "C") + "  started recording journal file",
        "' 0:< Initial VM: invented",
    ] + box("14:59:00.000", "14:59:02.000", "Keep it")
      + box("15:00:10.000" if early else "15:00:11.000", "15:00:15.000", "Keep it")
      + [stamp("15:00:21.500", "C") + "   " + SHOWN.lstrip("' "), stamp("15:00:29.000"),
         "Jrn.Data  _", ANSWER, '        "Discard it"']
      + box("15:00:33.000", "15:00:35.500", "Keep it")
      + [stamp("15:00:40.800"), "' 0:< TaskDialog \"An invented warning\""])
    return transcript, trail, journal


def build_k_without_box(folder):
    """The same build's journal with no Keep box in it - the owner's switch
    `write.askBeforeKeeping` off, as every build before D-126 - written as
    latin-1, the journal's other encoding."""
    transcript, trail, _ = build_k(folder)
    journal = write_journal(os.path.join(folder, "journal.0002.txt"), [
        "'Build: invented for this test",
        stamp("14:58:00.000", "C") + "  started recording journal file",
        stamp("15:00:12.000"),
        "' 0:< TaskDialog \"An invented warning\"",
        stamp("15:00:13.000"),
        "' 0:< caf\xe9 - a latin-1 byte",
    ], encoding="latin-1")
    return transcript, trail, journal


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


def quantity(report, key):
    return (report or {}).get("quantities", {}).get(key) or {}


def section_keep_box(folder):
    print()
    print("10. The Keep box, from Revit's journal (row 5b-1031)")
    print("-" * 72)
    transcript, trail, journal = build_k(folder)
    code, r = as_json([transcript, "--audit", trail, "--journal", journal,
                       "--journal-utc-offset", OFFSET])
    check(code == 0 and r is not None, "it reads the chat, the audit and the journal, exit 0")
    if r is None:
        return
    name = "journal.0001.txt"
    check(near(value(r, "revit_fragment_s"), 15.5),
          "Revit's own time is still the audit's 15.5 s - the boxes in it")
    check(value(r, "keep_boxes_in_journal") == 4, "4 Keep boxes in the whole journal")
    check(value(r, "keep_boxes") == 3,
          "3 in the chat's minutes - box D, before the prompt, is not this chat's")
    check(lines_from(r, "keep_boxes", name) == [10, 11, 12, 14, 15, 16, 17, 19, 20,
                                                21, 22, 23, 25, 26],
          "... each named by its stamps, its TaskDialog line, its answer and its button")
    check(near(value(r, "keep_boxes_s"), 14.0), "14.0 s shown to answered: 4.0, 7.5 and 2.5")
    check(value(r, "keep_boxes_discarded") == 1
          and quantity(r, "keep_boxes_discarded").get("by_answer")
          == {"Keep it": 2, "Discard it": 1},
          "1 not kept - the answers as read: Keep it 2, Discard it 1")
    check(value(r, "keep_boxes_in_runs") == 2 and near(value(r, "keep_boxes_in_runs_s"), 11.5),
          "2 boxes inside this chat's runs, 11.5 s of them")
    check(lines_from(r, "keep_boxes_in_runs", "audit-k-202601.jsonl") == [1, 2],
          "... in the runs on audit lines 1 and 2")
    check(value(r, "keep_boxes_not_in_runs") == 1
          and lines_from(r, "keep_boxes_not_in_runs", name) == [21, 22, 23, 25, 26],
          "box C, answered between two calls, is inside no run - and is named")
    check(near(value(r, "revit_fragment_less_boxes_s"), 4.0),
          "Revit's own time less the boxes: 15.5 - 11.5 = 4.0 s")
    check(lines_from(r, "revit_fragment_less_boxes_s", "audit-k-202601.jsonl") == [1, 2, 3]
          and lines_from(r, "revit_fragment_less_boxes_s", name)
          == [10, 11, 12, 14, 15, 16, 17, 19, 20],
          "... from every joined run's audit line and the two boxes' journal lines")
    check(near(value(r, "revit_first_runs_less_boxes_s"), 3.0),
          "the first runs, 14.5 s, less their boxes: 3.0 s")
    runs = [(x.get("audit"), x.get("ms"), x.get("ms_less_boxes"), len(x.get("boxes") or []))
            for x in r.get("runs") or []]
    check(runs == [("audit-k-202601.jsonl:1", 5500, 1500, 1),
                   ("audit-k-202601.jsonl:2", 9000, 1500, 1),
                   ("audit-k-202601.jsonl:3", 1000, 1000, 0)],
          "run by run: 5 500 ms less a 4.0 s box, 9 000 less 7.5 s, 1 000 with no box")
    boxes = (r.get("runs") or [{}])[0].get("boxes") or [{}]
    check(boxes[0].get("lines") == [10, 11, 12, 14, 15] and boxes[0].get("journal") == name
          and boxes[0].get("cut_to_run_start") is False,
          "... and each run's box names its journal lines")
    head = (r.get("journal") or [{}])[0]
    check(head.get("encoding") == "UTF-16 LE" and head.get("boxes") == 4,
          "the journal is read as UTF-16 LE from its byte-order mark")
    sources = [key for key in ("keep_boxes_in_journal", "keep_boxes", "keep_boxes_s",
                               "keep_boxes_discarded", "keep_boxes_in_runs",
                               "keep_boxes_in_runs_s", "keep_boxes_not_in_runs",
                               "revit_fragment_less_boxes_s", "revit_first_runs_less_boxes_s")
               if quantity(r, key).get("status") != "MEASURED"
               or not all(s.get("file") and s.get("lines") for s in quantity(r, key).get("from")
                          or [{}])]
    check(not sources, "every box figure is measured and names a file and lines"
          + (" - not: %s" % ", ".join(sources) if sources else ""))

    code, out = run([transcript, "--audit", trail, "--journal", journal,
                     "--journal-utc-offset", OFFSET])
    check(code == 0 and "Revit's own time less boxes" in out and name in out
          and "less the box" in out, "the text report shows the boxes and each run less its box")

    transcript, trail, journal = build_k(folder, early=True)
    code, r = as_json([transcript, "--audit", trail, "--journal", journal,
                       "--journal-utc-offset", OFFSET])
    check(r is not None and near(value(r, "keep_boxes_s"), 15.0)
          and near(value(r, "keep_boxes_in_runs_s"), 12.1)
          and quantity(r, "keep_boxes_in_runs_s").get("cut") == 1,
          "a box stamped before its run began: 5.0 s in the journal, 4.6 of them in the run, "
          "and said to be cut")
    first = ((r or {}).get("runs") or [{}])[0]
    check(near(first.get("ms_less_boxes"), 900.0)
          and ((first.get("boxes") or [{}])[0]).get("cut_to_run_start") is True,
          "... that run: 5 500 ms less 4 600, and its box marked cut to the run's start")
    check(near(value(r, "revit_fragment_less_boxes_s"), 3.4), "Revit's own time less them: 3.4 s")


def section_keep_box_none(folder):
    print()
    print("11. The negative case - a journal with no Keep box")
    print("-" * 72)
    transcript, trail, journal = build_k_without_box(folder)
    _, without = as_json([transcript, "--audit", trail])
    code, r = as_json([transcript, "--audit", trail, "--journal", journal,
                       "--journal-utc-offset", OFFSET])
    check(code == 0 and r is not None, "exit 0")
    if r is None or without is None:
        return
    check(value(r, "keep_boxes_in_journal") == 0 and value(r, "keep_boxes") == 0
          and near(value(r, "keep_boxes_s"), 0.0) and value(r, "keep_boxes_in_runs") == 0,
          "0 boxes - another TaskDialog is not the Keep box")
    check(lines_from(r, "keep_boxes", "journal.0002.txt") == [1, 6],
          "... and the zero names the journal it searched, lines 1 to 6")
    check(near(value(r, "revit_fragment_s"), value(without, "revit_fragment_s"))
          and near(value(r, "revit_fragment_s"), 15.5),
          "Revit's own time is 15.5 s with the journal and without it")
    check(near(value(r, "revit_fragment_less_boxes_s"), 15.5),
          "... and less no box it is the same 15.5 s")
    check([x.get("ms_less_boxes") for x in r.get("runs") or []] == [5500, 9000, 1000],
          "every run less the boxes is its own ms")
    check(((r.get("journal") or [{}])[0]).get("encoding") == "latin-1",
          "a journal with no byte-order mark and no UTF-16 bytes is read as latin-1")


def section_keep_box_flags(folder):
    print()
    print("12. No journal, no offset, two journals, a journal that is not there")
    print("-" * 72)
    transcript, trail, journal = build_k(folder)
    _, r = as_json([transcript, "--audit", trail])
    entry = quantity(r, "keep_boxes")
    check(entry.get("status") == "NOT MEASURED" and "--journal" in (entry.get("why") or ""),
          "no --journal: the boxes are NOT MEASURED, and the reason names the flag")
    check(status(r, "revit_fragment_less_boxes_s") == "NOT MEASURED"
          and near(value(r, "revit_fragment_s"), 15.5),
          "... Revit's own time is still the audit's, and less the boxes is NOT MEASURED")

    draft = write_journal(os.path.join(folder, "journal.0003.txt"), [
        stamp("15:01:00.000", "C") + "  started recording journal file",
        stamp("15:01:01.000"),
        "' 0:< TaskDialog \"'Invented Fragment' has never been proved. Keep what it did?\"",
        stamp("15:01:04.000"),
        "Jrn.Data  _",
        "        \"TaskDialogResult\"  , \"'Invented Fragment' has never been proved. "
        "Keep what it did?\" ,  _",
        '        "Discard it"',
    ], encoding="utf-16-le")
    _, r = as_json([transcript, "--audit", trail, "--journal", journal, "--journal", draft])
    check(value(r, "keep_boxes_in_journal") == 5,
          "two journals, no offset: 5 boxes in the whole of both - a DRAFT's box read too, from "
          "UTF-16 LE with no byte-order mark")
    entry = quantity(r, "keep_boxes")
    check(entry.get("status") == "NOT MEASURED"
          and "--journal-utc-offset" in (entry.get("why") or ""),
          "... and with no offset the boxes in the chat's minutes are NOT MEASURED, the flag named")
    check(status(r, "revit_fragment_less_boxes_s") == "NOT MEASURED",
          "... and so is Revit's own time less the boxes")

    code, out = run([transcript, "--journal", os.path.join(folder, "journal.9999.txt"),
                     "--journal-utc-offset", OFFSET])
    check(code == 2 and "No journal at" in out and "journal.9999.txt" in out,
          "a journal that is not there: exit 2, named")
    code, out = run([transcript, "--journal", journal, "--journal-utc-offset", "three"])
    check(code == 2 and "is not Z or +HH:MM" in out, "an offset that is not +HH:MM: exit 2")
    _, r = as_json([transcript, "--audit", trail, "--journal", journal,
                    "--journal-utc-offset=-03:00"])
    check(value(r, "keep_boxes_in_journal") == 4 and value(r, "keep_boxes") == 0,
          "an offset the wrong way round puts every box outside the chat - and the whole "
          "journal's 4 say they are there")


def section_keep_box_words():
    print()
    print("13. The box's words are the add-in's")
    print("-" * 72)
    with open(TOOL, encoding="utf-8") as fh:
        tool = fh.read()
    with open(KEEP_QUESTION, encoding="utf-8") as fh:
        added = fh.read()
    headline = re.search(r'^KEEP_HEADLINE = "([^"]*)"', tool, re.MULTILINE)
    link = re.search(r'^KEEP_LINK = "([^"]*)"', tool, re.MULTILINE)
    check(headline is not None and '"%s"' % headline.group(1) in added,
          "the tool's headline is the one HeronKeepQuestion.Headline shows a PROVEN run")
    check(link is not None and 'KeepLink = "%s"' % link.group(1) in added,
          "the tool's Keep link is HeronKeepQuestion.KeepLink")
    # D-138'S THIRD LINK KEEPS TOO. Counted as a Discard, a job the modeller
    # let run on would read as a job they turned down.
    granted = ""
    if os.path.isfile(KEEP_GRANT):
        with open(KEEP_GRANT, encoding="utf-8") as fh:
            granted = fh.read()
    rest = re.search(r'^KEEP_REST_LINK = "([^"]*)"', tool, re.MULTILINE)
    check(rest is not None and 'KeepRestLink = "%s"' % rest.group(1) in granted,
          "the tool's third link is HeronKeepGrant.KeepRestLink (D-138)")
    check(re.search(r"^KEEPS = \(KEEP_LINK, KEEP_REST_LINK\)$", tool, re.MULTILINE) is not None
          and 'b["button"] not in KEEPS' in tool,
          "and an answer of either link is counted as kept, never as a Discard")
    draft = re.search(r'^KEEP_HEADLINE_DRAFT = r"(.*)"$', tool, re.MULTILINE)
    check(draft is not None and r"has never been proved\. Keep what it did\?" in draft.group(1)
          and "' has never been proved. Keep what it did?\"" in added,
          "and the DRAFT headline the tool reads is the add-in's wording")


def main():
    print("THE FAMILY-BUILD TIMER")
    print("=" * 72)
    if not os.path.isfile(TOOL):
        check(False, "tools/time-family-build.py exists")
        return 1
    folder = tempfile.mkdtemp(prefix="heron-time-build-")
    try:
        for i, section in enumerate((section_a, section_b, section_b2, section_none,
                                     section_no_audit, section_sources, section_edges,
                                     section_cards, section_wrapped, section_keep_box,
                                     section_keep_box_none, section_keep_box_flags,
                                     section_keep_box_words)):
            sub = os.path.join(folder, str(i))
            os.makedirs(sub)
            if section in (section_cards, section_keep_box_words):
                section()
            else:
                section(sub)
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
