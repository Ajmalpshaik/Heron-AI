#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Heron undoes its last kept change only while it has seen nothing since (D-157).

    python tests/test_undo_last.py

WHY THIS EXISTS
---------------
Revit's Undo takes the top of the undo list, and Revit has no API that names
an entry and undoes that one, or that reads the list (proposal F29). The list
is shared with the person modelling: an Undo sent "for Heron" after they moved
a wall would take their wall. So `undo_last` posts Revit's own Undo only while
HeronUndoGuard saw Heron's change land - Revit counted elements in it - and
has seen nothing reported in that model since.

TWO HALVES, AND ONE OF THEM CANNOT BE RUN HERE
-----------------------------------------------
  THE RULE      what covers Heron's entry, what puts it back, what Heron does
                when it missed something, and what the modeller is told when
                Undo is not sent - HeronUndoGuard.cs, which names no vendor
                type and is linked by source into
                tests/Heron.UndoGuard.TestHost. Built and RUN here.
  THE WIRING    that the keep tells the guard WHAT REVIT COUNTED; that the
                permanent listener tells it every commit, undo and redo -
                before the witness's own early return - and counts a commit
                inside Heron's own armed job as no cover; that a sync, a
                reload, a save under a new name and a close are heard; that
                everything Heron can miss makes the guard refuse; that
                RevitUndo checks the PINNED MODEL and the guard BEFORE it posts
                anything, opens no transaction, and marks the Undo sent; and
                that nothing queued runs between the post and Revit's Undo.
                That half calls the Revit API, so it is read as TEXT, with
                comments and strings removed.

What the chat sends and says is tests/test_mcp_serves.py's and
tests/test_outcome_line.py's (revit_undo_last). Whether Revit raises the
events as the guard expects - one undo event for an assimilated group, a
commit for the modeller's own edit - is the PC's, and NEEDS-CHECKING Group NU
says what it owes.

MEASURED AGAINST THE CODE AS IT STOOD: with the add-in from before the change
and no HeronUndoGuard.cs, every wiring check fails and the host does not
build - exit 1, not a traceback.

THE REVIEW OF 2026-11-12, AND THE THREE ASSERTIONS HERE THAT PINNED A DEFECT.
An independent review found the feature could take the modeller's own work.
Each repair's check was seen to fail by assertion on the code before it. Three
checks this file already made were wrong, and are changed rather than defended:
  - the keep was held to `Undo.Kept(watch.Document, watch.Job, watch.Label`:
    told of every keep, whatever it left. A job that opens no transaction
    leaves no undo entry, and the next Undo took the modeller's work (row
    5b-1603). The keep is now held to passing what Revit COUNTED.
  - the post was held to follow `RevitOperations.ProjectKey(doc) !=
    expectProject` and nothing else. That id is the template's, shared by
    every project started from it, and a family has none (row 5b-1604). It is
    now held to follow the pinned model found as a kept change finds it, and
    being the model on screen.
  - the rollback witness was held to `return RevitUndo.KeyOf(doc);`, a third
    copy of RevitOperations.DocumentKey. It is held to the one owner.

IT EXITS 3 WHEN .NET IS ABSENT AND THE WIRING PASSED, and that is NOT a pass.
"""

import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import test_failure_note as NOTE                                # noqa: E402

ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
EXECUTOR = os.path.join(ADDIN, "RevitFragment.cs")
MOVE = os.path.join(ADDIN, "RevitWrite.cs")
APPLICATION = os.path.join(ADDIN, "HeronApplication.cs")
DISPATCHER = os.path.join(ADDIN, "RevitDispatcher.cs")
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


def in_order(code, *needles):
    """Every needle is there, each after the one before it. `str.find` answers
    -1 for a needle that is missing, which an `a < b` alone reads as first."""
    last = -1
    for needle in needles:
        found = code.find(needle, last + 1)
        if found < 0:
            return False
        last = found
    return True


def wiring():
    code = NOTE.code_only(NOTE.read(EXECUTOR) or "")
    check(bool(code), "RevitFragment.cs is there to read")

    print("\n1. the keep tells the guard what Revit counted, after Assimilate")
    keep = NOTE.body_of(code, "string KeepAndWatch(")
    check(re.search(r"string KeepAndWatch\(\s*TransactionGroup group,\s*Document doc,\s*"
                    r"HeronRollbackWitness\.Watch watch,\s*int counted,\s*bool read\)", code)
          is not None,
          "KeepAndWatch is handed what Revit counted in the job, and whether all of it was "
          "read - a keep cannot be told to the guard without them")
    check(in_order(keep, "group.Assimilate() != TransactionStatus.Committed",
                   "Undo.Kept(key,", "DateTime.Now, counted, read)", "Witness.Assimilated("),
          "the guard is told only once Assimilate committed, WITH the count - a job Revit "
          "counted nothing in left no undo entry - and before the witness, so a witness "
          "that throws cannot lose it")
    unkept = NOTE.body_of(keep, "if (group.Assimilate() != TransactionStatus.Committed)")
    check("RevitUndo.Doubt(doc," in unkept,
          "a group Revit did not report kept is not left looking as it did: the guard doubts")
    check(re.search(r"catch\s*\(\s*Exception\s+ex\s*\)\s*\{[^}]*RevitUndo\.Doubt\(null,", keep)
          is not None,
          "and a keep the guard could not be told of makes every model refuse")
    run = NOTE.body_of(code, "public static string Run(")
    check(re.search(r"KeepAndWatch\(\s*group,\s*target,\s*watch,\s*described == null \? 0 : "
                    r"described\.Total,\s*described != null && described\.Unreadable == 0\)",
                    run) is not None,
          "a fragment job's keep passes Revit's own count - added, changed and deleted, "
          "setup steps included - and whether an event went unread")
    move = NOTE.code_only(NOTE.read(MOVE) or "")
    check("RevitFragment.KeepAndWatch(group, doc, watch, reallyMoved + partly, true)" in move,
          "a move's keep passes how many elements were verified to have moved")
    loose = [name for name, text in (("RevitFragment.cs", code), ("RevitWrite.cs", move))
             if re.search(r"KeepAndWatch\(\s*\w+,\s*\w+,\s*\w+\s*\)", text)]
    check(not loose, "and no keep anywhere is made without a count%s"
          % ("" if not loose else " - found in " + ", ".join(loose)))

    print("\n2. the permanent listener tells it every change, before the witness's early return")
    listener = NOTE.body_of(code, "void OnDocumentChanged(")
    check(in_order(listener, "try", "if (Undo.Any) HearForUndo(e);",
                   "if (armed == null && !Witness.Any) return;"),
          "heard first, inside the listener's own try - a Ctrl+Z or an edit between two jobs "
          "is exactly what the guard is for, and the witness returns early then")
    hearing = NOTE.body_of(code, "void HearForUndo(")
    check(re.search(r"var\s+ours\s*=\s*armed\s*!=\s*null\s*&&\s*armed\.Document\s*==\s*key\s*;",
                    hearing) is not None,
          "a commit is Heron's own only while Heron's job is armed in THAT model")
    check("Undo.Committed(key, names, ours)" in hearing
          and "Undo.UndoneIn(key, names)" in hearing
          and "Undo.RedoneIn(key, names)" in hearing,
          "commits, undos and redos each reach the guard, with the names Revit gave")
    check(re.search(r"catch\s*\(\s*Exception\s+ex\s*\)\s*\{\s*HeronApplication\.Log\(", hearing)
          is not None,
          "and nothing escapes into Revit's event")

    print("\n3. what Heron misses, the guard is told - it does not go on saying nothing changed")
    check(re.search(r"catch\s*\(\s*Exception\s+ex\s*\)\s*\{\s*HeronApplication\.Log\([^;]*;\s*"
                    r"try\s*\{\s*Undo\.Doubt\(key,", hearing) is not None
          and in_order(hearing, "string key = null;", "try", "key = WitnessKey("),
          "a change the listener could not read: that model doubted - every model, when "
          "even which one could not be read")
    rollback = NOTE.body_of(code, "HeronRollbackWitness.Outcome RollBackAndCheck(")
    check(in_order(rollback, "SafeRollBack(group)", "if (!after.Held || after.Lost > 0)",
                   "RevitUndo.Doubt(doc,", "return after;"),
          "a rollback that did not hold, or took kept work with it: doubted")
    check(re.search(r"if\s*\(target\.IsModifiable\)\s*\{\s*return LeftOpen\(target, name,", run)
          is not None
          and in_order(NOTE.body_of(code, "static string LeftOpen("),
                       "RevitUndo.Doubt(target,", "return reply;"),
          "a job that left a transaction of its own open - neither kept nor rolled back: "
          "doubted, before the reply that says to press Undo")
    dispatcher = NOTE.code_only(NOTE.read(DISPATCHER) or "")
    execute = NOTE.body_of(dispatcher, "public void Execute(")
    check(re.search(r"catch\s*\(\s*Exception\s+ex\s*\)\s*\{[^}]*if\s*\(HeronOperationRegistry\."
                    r"Writes\(op\)\)\s*RevitUndo\.Doubt\(null,", execute) is not None,
          "and a job that could change the model and ended in an exception: every model "
          "doubted")

    print("\n4. a sync, a reload, a save under a new name and a close are heard")
    app = NOTE.code_only(NOTE.read(APPLICATION) or "")
    check("DocumentSynchronizedWithCentral += OnSynchronized" in app
          and "DocumentSynchronizedWithCentral -= OnSynchronized" in app,
          "sync with central - which clears Revit's undo list - is heard, and let go at shutdown")
    check("RevitUndo.Synced(e.Document)" in NOTE.body_of(app, "void OnSynchronized(")
          and "RevitUndo.Forget(e.Document)" in NOTE.body_of(app, "void OnDocumentClosing(")
          and "RevitFragment" not in NOTE.body_of(app, "void OnSynchronized("),
          "and each tells the guard through RevitUndo - never RevitFragment, which would "
          "load Roslyn outside its guard - and a closed model is forgotten")
    check("RevitUndo.Listen(application.ControlledApplication);" in app
          and "RevitUndo.StopListening(application.ControlledApplication);" in app,
          "RevitUndo is given the application to listen to, and lets go at shutdown")
    raw_op = NOTE.read(OPERATION) or ""
    op = NOTE.code_only(raw_op)
    listen = NOTE.body_of(op, "static void Listen(")
    check('ReloadEvent = "DocumentReloadedLatest"' in raw_op
          and "GetEvent(ReloadEvent)" in listen and "AddEventHandler(application," in listen
          and ".DocumentReloadedLatest" not in op,
          "Reload Latest is heard where this Revit has the event - found BY NAME, so the "
          "one build still loads on Revit 2020, which has none")
    check("Guard.ReloadedIn(RevitOperations.DocumentKey(doc))"
          in NOTE.body_of(op, "static void OnReloaded("),
          "and tells the guard the list was cleared, as a sync does")
    check(all("application.Document%s += On%s;" % (event, event) in listen
              for event in ("Saving", "SavingAs", "Saved", "SavedAs"))
          and "Guard.Renamed(before, after)" in NOTE.body_of(op, "static void Saved("),
          "a save is heard before and after: one that moves the model's key - a first "
          "save, Save As - moves what the guard remembers with it")
    check("internal static readonly HeronUndoGuard Guard = new HeronUndoGuard(HeronApplication.Log)"
          in op,
          "the guard is held by RevitUndo, and writes every transition to the add-in's log")
    check("return RevitOperations.DocumentKey(doc);" in NOTE.body_of(code, "string WitnessKey(")
          and "KeyOf(" not in op and "RevitOperations.DocumentKey(" in op,
          "one owner for a model's key: the witness and the guard ask "
          "RevitOperations.DocumentKey, and RevitUndo keeps no copy of its own")

    print("\n5. Undo is posted only in the PINNED model, on screen, after the guard said yes")
    last = NOTE.body_of(op, "public static string Last(")
    check(in_order(last, "NotThePinned(app, request, doc)",
                   "Guard.Refusal(key, doc.Title, true, canPost)",
                   "if (checkOnly) return Checked(",
                   "if (refusal != null) return Json.Error(",
                   "app.PostCommand(undo);",
                   "Guard.SentFor(key, DateTime.Now);"),
          "the pinned model, then the guard, then the post - and marked sent after it, so a "
          "second request before Revit reports it is refused")
    pinned = NOTE.body_of(op, "static string NotThePinned(")
    check(in_order(pinned, "string.IsNullOrEmpty(wantedPath) && string.IsNullOrEmpty(wanted)",
                   "return", "RevitOperations.HoldTarget(app, request)",
                   "if (pinned == null)", "return", "if (!pinned.Equals(doc))", "return",
                   "RevitOperations.ProjectKey(doc) != expectProject", "return null;"),
          "the pinned model is found among the open ones as a kept change finds it (its "
          "path, else its title) and must BE the model on screen; a request that names "
          "none, or a model that is not open, is refused - and the shared Project "
          "Information id is asked last, never alone")
    check('"documentPath"' in raw_op and '"document"' in raw_op and "PostCommand" not in pinned,
          "by the two fields revit_change sends, and nothing is posted while it is asked")
    check("PostableCommand.Undo" in last and "CanPostCommand(undo)" in last,
          "it is Revit's own Undo command, and Revit is asked whether it can take it now")
    check("Transaction" not in op and ".Delete(" not in op and ".Set(" not in op,
          "and nothing else: no transaction, no second change made to reverse the first")
    check(len(re.findall(r"(?<!Can)PostCommand\(", op)) == 1
          and "PostCommand" not in NOTE.body_of(op, "static string Checked("),
          "a check posts NOTHING: the one PostCommand is past the check's return")

    print("\n6. nothing of Heron's runs between the post and Revit's Undo")
    check(in_order(last, "app.PostCommand(undo);", "_posted = true;"),
          "a posted Undo is noted for the dispatcher")
    check(in_order(execute, "job.Finish(response);", "if (RevitUndo.TakePosted())",
                   "RaiseForTheRest();", "return;"),
          "the dispatcher stops draining its queue once Undo is posted - Revit runs a "
          "posted command only when the handler returns - and asks Revit to call again")
    raise_again = NOTE.body_of(dispatcher, "void RaiseForTheRest(")
    check(in_order(raise_again, "waiting = _queue.Count;", "if (waiting == 0) return;",
                   "raiser.Raise()"),
          "raising the event again only when something is still queued")
    check(in_order(raise_again, "answer = raiser == null", "raiser.Raise()",
                   "ExternalEventRequest.Accepted", "_log(")
          and raise_again.count("_log(") >= 2,
          "and what Revit answered the raise is read and logged, taken or not - a raise it "
          "refused leaves the jobs queued, said so, never lost in silence")
    raw_operations = NOTE.read(OPERATIONS) or ""
    operations = NOTE.code_only(raw_operations)
    check(in_order(NOTE.body_of(operations, "public static string Run("),
                   "RevitUndo.Waiting(app, request, op)", "if (waiting != null) return waiting;",
                   "return Route(app, request, op);"),
          "and a job that would act in that model is held back until Revit reports the Undo")
    waiting = NOTE.body_of(op, "static string Waiting(")
    check("HeronRisk.Execute" in waiting and "Guard.Pending(" in waiting,
          "from EXECUTE up - a read leaves nothing for the Undo to take")
    check("Guard.Pending(DateTime.Now)" in waiting and "HoldTarget" not in waiting
          and "DocumentKey" not in waiting,
          "in ANY model, not only the one the Undo was sent in: a job that brings another "
          "model to the front would send the Undo there")
    guard_code = NOTE.code_only(NOTE.read(RULE) or "")
    asking = NOTE.body_of(guard_code, "internal string Pending(DateTime now)")
    check(bool(asking) and "State =" not in asking and ".Remove(" not in asking
          and "Guard.Pending(" not in last
          and not any(writes in last[:max(0, last.find("if (checkOnly) return Checked("))]
                      for writes in ("Guard.SentFor(", "Guard.Doubt(", "Guard.Kept(")),
          "asking whether a job is held changes nothing in the guard, and nothing on a "
          "check's path writes to it - a check is read-only")
    sent_for = NOTE.body_of(guard_code, "internal void SentFor(")
    kept = NOTE.body_of(guard_code, "internal void Kept(")
    check("_outstanding[document] = new Posted" in sent_for
          and in_order(kept, "if (Outstanding(document))", "entry.State = Unsure;",
                       "entry.Why = OutstandingWhy;", "else if (counted > 0) entry.State = OnTop;"),
          "an Undo posted is marked outstanding apart from the entry a keep replaces, and a "
          "keep made while it stands is unsure before anything else is asked")
    cleared = sorted(set(name for name in ("UndoneIn", "SyncedIn", "ReloadedIn", "Closed",
                                           "Committed", "Kept", "Doubt", "Pending", "RedoneIn")
                         if "_outstanding.Remove(document)"
                         in NOTE.body_of(guard_code, "internal %s %s(" % (
                             "string" if name == "Pending" else "void", name))))
    check(cleared == ["Closed", "ReloadedIn", "SyncedIn", "UndoneIn"],
          "and only an undo Revit reports, a sync, a reload or a close clears it - never a "
          "commit, a keep, a doubt or time: found %s" % cleared)

    print("\n7. it is MODIFY, its check is READ, both routed, and the rule names nothing of Revit's")
    registry = NOTE.read(REGISTRY) or ""
    check(re.search(r'\{\s*"undo_guarded",\s*HeronRisk\.Modify\s*\}', registry) is not None,
          "undo_guarded is declared MODIFY, so it needs Changes ON like any change")
    check(re.search(r'\{\s*"undo_check",\s*HeronRisk\.Read\s*\}', registry) is not None,
          "undo_check is declared READ: it sends nothing, so it answers with Changes off")
    check(re.search(r'case "undo_guarded":\s*return RevitUndo\.Last\(app, request, false, null\);',
                    raw_operations) is not None,
          "undo_guarded is routed to RevitUndo.Last with the whole request - the pinned "
          "model is named in it")
    check(re.search(r'case "undo_check":\s*return RevitUndo\.Last\(app, request, true, '
                    r'Gate\("undo_guarded"\)\);', raw_operations) is not None,
          "undo_check to the same code with nothing sent, told what the switches would "
          "answer the real one")
    # THE FIRST BUILD'S NAME ANSWERS NOTHING (row 5b-1604, the second review).
    # That build posts on the shared id and counts every keep as on top; a
    # Revit still running it knows `undo_last`. So the repaired pair never
    # uses that name: the server cannot ask an unrepaired add-in to post.
    tools_text = NOTE.read(os.path.join(ROOT, "mcp", "server", "heron_tools.py")) or ""
    server_text = NOTE.read(os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")) or ""
    check(re.search(r'\{\s*"undo_last"\s*,', registry) is None
          and 'case "undo_last":' not in raw_operations,
          "the first build's operation name, undo_last, is declared and routed nowhere")
    check('"revit_undo_last":          (MODIFY,  "undo_guarded")' in tools_text
          and re.search(r'session\.request\(\s*"undo_last"', server_text) is None
          and '"undo_check" if check_only else "undo_guarded"' in server_text,
          "and the chat's tool sends undo_guarded, never undo_last - an add-in without the "
          "repair answers that it has no such operation, and nothing is posted")
    with_state = NOTE.body_of(code, "string WithUndoState(")
    check("WithUndoState(verdict, target)" in run and "RevitUndo.StateOf(doc)" in with_state,
          "a kept reply carries what the guard holds straight after the keep")
    check("RevitUndo.WhyOf(doc)" in with_state
          and "entry.Why" in NOTE.body_of(op, "static string WhyOf("),
          "and, when the guard is unsure, the guard's own reason - so an Undo still "
          "outstanding is said as that")
    rule_text = NOTE.read(RULE)
    check(rule_text is not None and "Autodesk" not in NOTE.code_only(rule_text),
          "HeronUndoGuard.cs names no vendor type, so a host can link it without Revit")


def rule():
    print("\n8. the rule itself, built and run without Revit")
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
    print("PASS    Heron undoes its last change only while it has seen nothing since")
    return 0


if __name__ == "__main__":
    sys.exit(main())
