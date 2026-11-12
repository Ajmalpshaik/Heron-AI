#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Heron undoes its last kept change only while it is still on top (D-157).

    python tests/test_undo_last.py

WHY THIS EXISTS
---------------
Revit's Undo takes the top of the undo list, and Revit has no API that names
an entry and undoes that one (proposal F29). The list is shared with the
person modelling: an Undo sent "for Heron" after they moved a wall would take
their wall. So `undo_last` posts Revit's own Undo only while HeronUndoGuard has
seen Heron's entry land on top and nothing land on it since.

TWO HALVES, AND ONE OF THEM CANNOT BE RUN HERE
-----------------------------------------------
  THE RULE      what covers Heron's entry, what puts it back, and what the
                modeller is told when Undo is not sent - HeronUndoGuard.cs,
                which names no Autodesk type and is linked by source into
                tests/Heron.UndoGuard.TestHost. Built and RUN here.
  THE WIRING    that the keep tells the guard after Assimilate, that the
                permanent listener tells it every commit, undo and redo -
                before the witness's own early return - and counts a commit
                inside Heron's own armed job as no cover; that a sync and a
                close are heard; and that RevitUndo checks the pin and the
                guard BEFORE it posts anything, opens no transaction, and
                marks the Undo sent. That half calls the Revit API, so it is
                read as TEXT, with comments and strings removed.

What the chat sends and says is tests/test_mcp_serves.py's (revit_undo_last).
Whether Revit raises the events as the guard expects - one undo event for an
assimilated group, a commit for the modeller's own edit - is the PC's, and
NEEDS-CHECKING Group NU says what it owes.

MEASURED AGAINST THE CODE AS IT STOOD: with the add-in from before the change
and no HeronUndoGuard.cs, every wiring check fails and the host does not
build - exit 1, not a traceback.

IT EXITS 3 WHEN .NET IS ABSENT AND THE WIRING PASSED, and that is NOT a pass.
"""

import io
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import test_failure_note as NOTE                                # noqa: E402

ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
EXECUTOR = os.path.join(ADDIN, "RevitFragment.cs")
APPLICATION = os.path.join(ADDIN, "HeronApplication.cs")
OPERATION = os.path.join(ADDIN, "RevitUndo.cs")
OPERATIONS = os.path.join(ADDIN, "RevitOperations.cs")
RULE = os.path.join(ADDIN, "HeronUndoGuard.cs")
REGISTRY = os.path.join(ROOT, "platform", "Heron.Core", "HeronOperationRegistry.cs")
HOST = os.path.join(ROOT, "tests", "Heron.UndoGuard.TestHost")
COULD_NOT_RUN = 3

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def at(code, needle, start=0):
    return code.find(needle, start)


def wiring():
    code = NOTE.code_only(NOTE.read(EXECUTOR) or "")
    check(bool(code), "RevitFragment.cs is there to read")

    print("\n1. the keep tells the guard, after Assimilate")
    keep = NOTE.body_of(code, "string KeepAndWatch(")
    assimilated = at(keep, "group.Assimilate() != TransactionStatus.Committed")
    told = at(keep, "Undo.Kept(watch.Document, watch.Job, watch.Label")
    witness = at(keep, "Witness.Assimilated(")
    check(0 <= assimilated < told < witness,
          "only once Assimilate committed - a group that did not keep is not on the list - "
          "and before the witness, so a witness that throws cannot lose it")

    print("\n2. the permanent listener tells it every change, before the witness's early return")
    listener = NOTE.body_of(code, "void OnDocumentChanged(")
    hear = at(listener, "if (Undo.Any) HearForUndo(e);")
    early = at(listener, "if (armed == null && !Witness.Any) return;")
    check(0 <= hear < early,
          "heard first - a Ctrl+Z or an edit between two jobs is exactly what the guard is "
          "for, and the witness returns early then")
    hearing = NOTE.body_of(code, "void HearForUndo(")
    check(re.search(r"var\s+ours\s*=\s*armed\s*!=\s*null\s*&&\s*armed\.Document\s*==\s*key\s*;",
                    hearing) is not None,
          "a commit is Heron's own only while Heron's job is armed in THAT model")
    check("Undo.Committed(key, names, ours)" in hearing
          and "Undo.UndoneIn(key, names)" in hearing
          and "Undo.RedoneIn(key, names)" in hearing,
          "commits, undos and redos each reach the guard")
    check(re.search(r"catch\s*\(\s*Exception\s+ex\s*\)\s*\{\s*HeronApplication\.Log\(", hearing)
          is not None,
          "and nothing escapes into Revit's event")

    print("\n3. a sync and a close are heard")
    app = NOTE.code_only(NOTE.read(APPLICATION) or "")
    check("DocumentSynchronizedWithCentral += OnSynchronized" in app
          and "DocumentSynchronizedWithCentral -= OnSynchronized" in app,
          "sync with central - which clears Revit's undo list - is heard, and let go at shutdown")
    check("RevitFragment.UndoSynced(e.Document)" in NOTE.body_of(app, "void OnSynchronized(")
          and "RevitFragment.UndoForget(e.Document)" in NOTE.body_of(app, "void OnDocumentClosing("),
          "and each tells the guard - a closed model is forgotten")

    print("\n4. Undo is posted only after the pin and the guard said yes")
    op = NOTE.code_only(NOTE.read(OPERATION) or "")
    last = NOTE.body_of(op, "public static string Last(")
    pin = at(last, "RevitOperations.ProjectKey(doc) != expectProject")
    asked = at(last, "RevitFragment.Undo.Refusal(key, doc.Title, true, canPost)")
    refused = at(last, "if (refusal != null) return Json.Error(")
    posted = at(last, "app.PostCommand(undo);")
    sent = at(last, "RevitFragment.Undo.SentFor(key);")
    check(0 <= pin < asked < refused < posted < sent,
          "the pinned model, then the guard, then the post - and marked sent after it, so a "
          "second request before Revit reports it is refused")
    check("PostableCommand.Undo" in last and "CanPostCommand(undo)" in last,
          "it is Revit's own Undo command, and Revit is asked whether it can take it now")
    check("Transaction" not in op and ".Delete(" not in op and ".Set(" not in op,
          "and nothing else: no transaction, no second change made to reverse the first")

    print("\n5. it is MODIFY, routed, and the rule names nothing of Revit's")
    registry = NOTE.read(REGISTRY) or ""
    check(re.search(r'\{\s*"undo_last",\s*HeronRisk\.Modify\s*\}', registry) is not None,
          "undo_last is declared MODIFY, so it needs Changes ON like any change")
    check('case "undo_last":' in (NOTE.read(OPERATIONS) or "")
          and "RevitUndo.Last(app" in (NOTE.read(OPERATIONS) or ""),
          "and routed to RevitUndo.Last")
    rule = NOTE.read(RULE)
    check(rule is not None and "Autodesk" not in NOTE.code_only(rule),
          "HeronUndoGuard.cs names no Autodesk type, so a host can link it without Revit")


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

    dll = os.path.join(HOST, out_dir, "Heron.UndoGuard.TestHost.dll")
    ran = subprocess.run(["dotnet", dll], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    for line in (ran.stdout or b"").decode("utf-8", "replace").splitlines():
        print("  " + line)
    check(ran.returncode == 0, "the host's own checks pass (exit %d)" % ran.returncode)
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
    print("PASS    Heron undoes its last change only while it is still on top")
    return 0


if __name__ == "__main__":
    sys.exit(main())
