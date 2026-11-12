#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A change whose answer was lost can still be read, by the request's own name (D-161).

    python tests/test_late_answer.py

WHY THIS EXISTS
---------------
When revit_change's answer is lost after its request left, the chat was told
only "look at the model before trying again". Revit finishes the job anyway
and the add-in writes its run record - but nothing tied that record to the
request the chat sent. Now revit_change names each request (`requestId`), the
add-in writes that name into the record, and heron_run_record finds the record
by it.

THREE HALVES
------------
  THE LOOKUP    heron_run_records.by_request, on a folder of records made
                here - found, not found, and a folder that is not there.
  THE WORDS     _late_answer, lifted out of the server's source and run: said
                for a lost answer or a job still running, never otherwise.
  THE WIRING    the add-in writes the request's name into the record, the
                server sends one on every change, and heron_run_record reads
                by it - read as TEXT.

Whether a late record lands while Revit is still working after the chat gave
up is the PC's, NEEDS-CHECKING Group NW.

MEASURED AGAINST THE CODE AS IT STOOD: every check fails, no traceback.
"""

import io
import json
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")
DISPATCHER = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitDispatcher.cs")
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def lookup():
    print("1. a record is found by the request it answers")
    import heron_run_records as RUNS
    finder = getattr(RUNS, "by_request", None)
    check(finder is not None, "heron_run_records has by_request")
    if finder is None:
        return
    folder = tempfile.mkdtemp(prefix="heron-late-")
    try:
        for run, request in (("wf-20261112-100000-0001-aaaa", "req-one"),
                             ("wf-20261112-100100-0002-bbbb", "req-two"),
                             ("wf-20261112-100200-0003-cccc", None)):
            with io.open(os.path.join(folder, run + ".json"), "w", encoding="utf-8") as fh:
                fh.write(json.dumps({"record": 1, "workflow": run, "requestId": request}))
        with io.open(os.path.join(folder, "wf-20261112-100300-0004-dddd.json"), "w",
                     encoding="utf-8") as fh:
            fh.write("{not json")
        check(finder("req-two", folder) == "wf-20261112-100100-0002-bbbb",
              "the record that names the request is found")
        check(finder("req-one", folder) == "wf-20261112-100000-0001-aaaa",
              "and an older one, past a record that cannot be read")
        check(finder("nobody", folder) is None and finder("", folder) is None,
              "a request no record names, or no request, finds nothing")
        check(finder("req-one", os.path.join(folder, "missing")) is None,
              "a folder that is not there finds nothing, and never raises")
    finally:
        shutil.rmtree(folder, ignore_errors=True)


def words():
    print("\n2. a lost answer says where it can still be read - and only then")
    text = io.open(SERVER, encoding="utf-8").read()
    start = text.find("\ndef _late_answer(")
    check(start >= 0, "the server has _late_answer")
    if start < 0:
        return
    end = text.index("\ndef ", start + 5)
    import heron_failure as F
    names = {"UNKNOWN_OUTCOME": F.UNKNOWN, "RUNNING_OUTCOME": F.RUNNING}
    exec(compile(text[start:end], "heron_mcp_server.py", "exec"), names)
    late = names["_late_answer"]
    lost = F.analyse(None, writes=True)
    said = late(lost, "abc123")
    check("heron_run_record request=abc123" in said and "before trying the change again" in said,
          "an answer lost after a change left names the request to read it by")
    running = F.Failure("still_running", F.RUNNING, F.WAIT, "still working")
    check("request=abc123" in late(running, "abc123"), "and so does a job Revit is still running")
    refused = F.analyse({"ok": False, "error": "write_disabled", "message": "Changes is off"},
                        writes=True)
    check(late(refused, "abc123") == "", "a refusal - nothing ran - says nothing of it")
    check(late(lost, None) == "" and late(None, "abc123") == "",
          "nor does a request with no name, or no failure")


def wiring():
    print("\n3. the request is named, recorded and read by its name")
    server = io.open(SERVER, encoding="utf-8").read()
    dispatcher = io.open(DISPATCHER, encoding="utf-8").read()
    record = dispatcher[dispatcher.find("string RunRecord("):dispatcher.find("string DescribeJob(")]
    check('Json.Str("requestId", Json.ReadString(job.Request, "requestId"))' in record,
          "the add-in writes the request's name into the run record")
    check('"requestId": uuid.uuid4().hex[:16],' in server,
          "revit_change names every request it sends")
    check(re.search(r'\+ _late_answer\(failure, args\.get\("requestId"\)\)\)', server) is not None,
          "and a failure's reply ends with where a late answer can be read")
    tool = server[server.find("def heron_run_record("):server.find("def heron_compatibility(")]
    check('request: str = ""' in tool and "runs.by_request(request.strip())" in tool,
          "heron_run_record takes `request` and reads the record by it")


def main():
    lookup()
    words()
    wiring()
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a lost answer can still be read, by the request's own name")
    return 0


if __name__ == "__main__":
    sys.exit(main())
