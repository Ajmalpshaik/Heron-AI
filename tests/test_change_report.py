#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A fragment write says which elements Revit changed, and keeps every one on disk.

    python tests/test_change_report.py

WHY THIS EXISTS
---------------
Until this change nothing in revit/ or platform/ tracked a change at all - no
DocumentChanged, no updater. A write's reply carried the fragment's own
variables, and `revit_change` sent every one of them back into the chat:
322,725 characters across two family builds (FRAGMENT-ISSUES 5b-407). Article
22 asks the record for "elements touched", and nothing could say.

TWO HALVES, BECAUSE ONE OF THEM CANNOT BE RUN HERE
---------------------------------------------------
  THE RULE      whose change a transaction was, how each element is counted,
                what the reply carries and what the run record lists -
                HeronChangeReport.cs, which names no Autodesk type and is
                linked by source into tests/Heron.ChangeReport.TestHost with
                the bridge's Json.cs. Built and RUN here, and the JSON it
                writes is read back by Python's own reader, which did not
                write it.
  THE WIRING    that RevitFragment listens from the moment its group starts
                to the moment it is described, counts only a commit of the
                target, reads nothing heavy inside the event, describes
                BEFORE Assimilate or the rollback, and is let go on every way
                out; that the dispatcher writes the record and links it to
                the audit line by workflow id. That half calls the Revit API,
                so it is read as TEXT, with comments and strings removed.

Neither half proves Revit raises DocumentChanged for every commit inside a
group, for an edit-scope fragment's own transactions, or on a rollback - nor
that a category can be read before Assimilate. That is the PC's, and
NEEDS-CHECKING says what it owes.

MEASURED AGAINST THE CODE AS IT STOOD: with RevitFragment.cs and
RevitDispatcher.cs from before the change and no HeronChangeReport.cs, every
wiring check fails and the host does not build - exit 1, not a traceback.

IT EXITS 3 WHEN .NET IS ABSENT AND THE WIRING PASSED, and that is NOT a pass:
the rule was not run.
"""

import io
import json
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

# The comment-and-string stripper, the brace walker and the framework picker
# test_failure_note.py already proved - imported, not copied, so the two
# suites read C# the same way.
import test_failure_note as NOTE                                # noqa: E402

ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
EXECUTOR = os.path.join(ADDIN, "RevitFragment.cs")
DISPATCHER = os.path.join(ADDIN, "RevitDispatcher.cs")
APPLICATION = os.path.join(ADDIN, "HeronApplication.cs")
REPORT = os.path.join(ADDIN, "HeronChangeReport.cs")
HOST = os.path.join(ROOT, "tests", "Heron.ChangeReport.TestHost")
COULD_NOT_RUN = 3

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def at(code, needle, start=0):
    """Where `needle` first appears at or after `start`, or -1."""
    return code.find(needle, start)


def wiring():
    raw = NOTE.read(EXECUTOR)
    check(raw is not None, "RevitFragment.cs is there to read")
    code = NOTE.code_only(raw or "")

    print("\n1. the collector listens for exactly the length of the write")
    run = NOTE.body_of(code, "public static string Run(UIApplication app, string request, bool writing)")
    check(bool(run), "Run(app, request, writing) is there")
    first = run.lstrip("{ \n\t\r")
    check(first.startswith("Elements = null;"),
          "the first thing Run does is drop any element list an earlier run left, so a "
          "refusal or a read never hands the dispatcher another job's list")
    started = at(run, "group.Start();")
    watch = at(run, "new ChangeWatch(", max(started, 0))
    setup = at(run, "RunSetupSteps(", max(started, 0))
    check(0 <= started < watch < setup,
          "the collector is made right after group.Start() and before the setup steps run, "
          "so a setup step's commits are seen too")
    check(re.search(r"using\s+var\s+watch\s*=\s*new\s+ChangeWatch\(", run) is not None,
          "as a using declaration, so it is let go however the group's block is left - "
          "every early return included")
    described = at(run, "changed = watch.Describe();")
    assimilate = at(run, "if (apply) group.Assimilate();")
    check(0 < described < assimilate,
          "what changed is described BEFORE Assimilate or the rollback, while every added "
          "element still exists to be asked its category")
    check(re.search(r"else\s+rolledBack\s*=\s*SafeRollBack\(group\)", run[described:]) is not None,
          "and the rollback that makes the default a preview comes after it too")
    check("Elements = changed == null ? null : changed.ElementsField();" in run,
          "the full element list is left for the dispatcher's record")
    check(re.search(r"WithVerdict\([^;]*,\s*changed\)", run) is not None,
          "and the verdict is handed what changed")

    print("\n2. the handler counts a commit of the target, and reads nothing heavy")
    watcher = NOTE.body_of(code, "class ChangeWatch")
    check(bool(watcher), "ChangeWatch is there")
    handler = NOTE.body_of(watcher, "void Changed(")
    check("UndoOperation.TransactionCommitted" in handler,
          "only a commit is counted - a rollback, an undo or a redo names elements with the "
          "meaning reversed or repeated")
    check(".Equals(_target)" in handler,
          "only the target document's - another open model's change is not this job's")
    check("GetElement(" not in handler and ".Category" not in handler,
          "nothing is read inside the event: no element, no category - Revit is still "
          "finishing the transaction")
    check(re.search(r"catch\s*\(\s*Exception\s*\)\s*\{\s*_report\.CouldNotRead\(\);", handler)
          is not None,
          "and nothing escapes into Revit's event: an event it cannot read is COUNTED as "
          "unreadable, so the report can say its numbers are incomplete")
    check("DocumentChanged += Changed" in watcher.replace("  ", " ")
          and "DocumentChanged -= Changed" in watcher.replace("  ", " "),
          "it subscribes when made and unsubscribes when let go")
    describe = NOTE.body_of(watcher, "Describe()")
    check(at(describe, "Dispose();") >= 0
          and at(describe, "Dispose();") < at(describe, "_report.Describe("),
          "and Describe stops listening before it reads, so nothing after it is counted")
    category = NOTE.body_of(watcher, "string CategoryOf(")
    check("_ids.TryGetValue(" in category and "GetElement(id)" in category,
          "a category is read from the ElementId Revit handed over - no ElementId is built "
          "from a number (5b-441)")

    print("\n3. the verdict counts setup changes only when Revit was seen to report them")
    verdict = NOTE.body_of(code, "string WithVerdict(")
    check("changed.Witnessed" in verdict,
          "the setup admission changes only for a witnessed report")
    check("ReplyFields()" in verdict, "and the counts go on the reply")
    check('"verdictKind"' in (raw or "") and '"verdictShort"' in (raw or ""),
          "beside the verdict's kind and its one-sentence form")
    rule = re.search(r"const\s+string\s+UndoRule\s*=([^;]*);", raw or "", re.S)
    check(rule is not None and verdict.count("UndoRule") >= 2,
          "and the undo rule is ONE constant both forms use - two copies of it drifted "
          "twice on PR #198")

    print("\n4. the dispatcher writes the record and links it to the audit line")
    told = "\n".join(line for line in (NOTE.read(DISPATCHER) or "").splitlines()
                     if not line.lstrip().startswith("//"))
    check(re.search(r"response\s*=\s*RunRecord\(job,\s*op,\s*response,\s*RevitFragment\.TakeElements\(\)\)",
                    told) is not None,
          "every finished job hands its reply and the element list to RunRecord")
    record = NOTE.body_of(NOTE.code_only(NOTE.read(DISPATCHER) or ""), "string RunRecord(")
    written = at(record, "HeronRuns.Write(job.WorkflowId")
    run_field = (NOTE.read(DISPATCHER) or "").find('Json.Str("run", job.WorkflowId)')
    check(written >= 0 and "if (!HeronRuns.Write(" in record,
          "the record is named by the workflow id - the audit line's own key")
    check(run_field >= 0,
          "and `run` goes on the reply only once the file was written, so no reply names a "
          "record nobody can read")
    audit_at = told.find("HeronAudit.Record(job.WorkflowId")
    record_at = told.find("response = RunRecord(")
    check(0 <= record_at < audit_at,
          "before the audit line is written from the same reply")
    check(re.search(r'"elementsAdded",\s*"elementsModified",\s*"elementsDeleted"', told)
          is not None
          and re.search(r"Json\.ReadLong\(response,\s*touched\)", told) is not None
          and re.search(r"if\s*\(\s*count\.HasValue\s*\)\s*numbers\.Add\(", told) is not None,
          "and the elements touched reach the audit line as numbers, only when the reply "
          "carried them (Article 22)")

    print("\n5. the records are pruned, and the rule names nothing of Revit's")
    app = NOTE.code_only(NOTE.read(APPLICATION) or "")
    check("HeronRuns.Prune(config.GetInt(" in app,
          "the add-in prunes old records at start-up by runs.retainDays")
    check('"runs.retainDays"' in (NOTE.read(APPLICATION) or ""),
          "read under that setting's own name")
    note = NOTE.read(REPORT)
    check(note is not None, "HeronChangeReport.cs is there")
    check(note is not None and "Autodesk" not in NOTE.code_only(note),
          "and names no Autodesk type, so a host can link it without Revit")


def rule():
    print("\n6. the rule itself, built and run without Revit")
    tfm = NOTE._tfm()
    if tfm is None:
        print("  NOT RUN - no framework here that an installed SDK can build AND")
        print("  a runtime can run. Linux: apt-get install -y dotnet-sdk-10.0")
        return COULD_NOT_RUN

    out_dir = "bin/x64/Debug-%s/" % tfm
    built = subprocess.run(
        ["dotnet", "build", HOST, "-p:RevitVersion=2024", "-p:HeronTfm=%s" % tfm,
         "-p:OutputPath=%s" % out_dir, "--nologo", "-v", "q"],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built.returncode != 0:
        check(False, "the host builds for %s" % tfm)
        for line in (built.stdout or b"").decode("utf-8", "replace").splitlines()[-20:]:
            print("    %s" % line)
        return 1

    dll = os.path.join(HOST, out_dir, "Heron.ChangeReport.TestHost.dll")
    ran = subprocess.run(["dotnet", dll], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    said = (ran.stdout or b"").decode("utf-8", "replace")
    blobs = {}
    for line in said.splitlines():
        for prefix in ("REPLY-JSON ", "ODD-JSON "):
            if line.startswith(prefix):
                blobs[prefix.strip()] = line[len(prefix):]
        print("  " + line)
    check(ran.returncode == 0, "the host's own checks pass (exit %d)" % ran.returncode)

    print("\n7. what the hand-written writer wrote, read by a reader that did not write it")
    try:
        reply = json.loads(blobs.get("REPLY-JSON", ""))
    except ValueError as bad:
        check(False, "the reply's fields are JSON: %s" % bad)
        reply = {}
    changes = reply.get("changes") or {}
    check(changes.get("witnessed") is True and changes.get("total") == 5,
          "the reply's `changes` parses, witnessed, with the job's total")
    fragment = changes.get("fragment") or {}
    check(fragment.get("firstIds") == ["8", "9", "7"]
          and [row.get("category") for row in fragment.get("byCategory") or []]
          == ["Generic Models", "Reference Planes"],
          "with the fragment's first ids and its counts by category")
    check(reply.get("elementsAdded") == 3 and reply.get("changeEvents") == 2,
          "and the whole-job numbers beside it, as numbers")
    try:
        odd = json.loads(blobs.get("ODD-JSON", ""))
    except ValueError as bad:
        check(False, "an id and a category holding a quote and a backslash are still JSON: %s" % bad)
        odd = {}
    elements = odd.get("elements") or [{}]
    check(elements[0].get("id") == 'a"b\\c' and elements[0].get("category") == 'Cat "quoted"'
          and (odd.get("changes") or {}).get("fragment", {}).get("firstIds") == ['a"b\\c'],
          "an id and a category holding a quote and a backslash come back exactly as given")
    return 0


def main():
    wiring()
    host = rule()

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    if host == COULD_NOT_RUN:
        print("COULD NOT RUN - the wiring passed and the rule was not run. Exit 3,")
        print("which is NOT a pass.")
        return COULD_NOT_RUN
    print("PASS    a fragment write counts what Revit says it changed, and keeps every id")
    return 0


if __name__ == "__main__":
    sys.exit(main())
