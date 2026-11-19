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

Q-88, 2026-11-16: ONLY THE CHAT THAT MADE A CHANGE MAY UNDO IT. The keep
tells the guard which chat kept it - the request's conversation (D-141) - and
the request that posts asks the guard whose change it is, after the D-157
rule and before the post: one request, no window. The operation is `undo_own`
and its check `undo_own_check`; `undo_guarded` and `undo_check` post nothing
any more - an add-in built before answers them by its older rule, so the chat
never sends them, and a chat started before is refused by this one. Seen to
fail on the code as it stood. THE ASSERTIONS THAT PINNED THE REQUEST'S OLD
MEANING WERE CHANGED, not defended: the keep's signature, its call to the
guard and its two callers (no chat); `undo_guarded` declared MODIFY and
routed to RevitUndo.Last; `undo_check` routed there with
`Gate("undo_guarded")`; and the tool sending `undo_guarded` / `undo_check`.

ROW 5b-1741, 2026-11-16: THE ACTIVITY PANE'S UNDO. The owner's answer of
2026-11-16: the pane's Undo is the modeller's own click in Revit, like Ctrl+Z -
Heron's last change in the model on screen, whichever chat made it. So
`RevitUndo.FromPane` asks no chat, and asks D-157's guard exactly as a chat's
Undo does; and, because the button sits beside ONE change, it is sent only when
that change - by the run it was kept in, which the keep now records - is
Heron's last one there. What a CHAT may undo (Q-88, `undo_own`) is unchanged.
Section 9 holds it; seen to fail on the code as it stood. THE ASSERTIONS THAT
PINNED WHERE THE POST WAS WRITTEN WERE MOVED, not defended: `Last` was held to
call `PostCommand` and `SentFor` itself, and now both doors hand the post to
the one `Post`, still past every refusal; and the keep was held to end its
call to the guard at `chat)`, and now passes the run after it.

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
                    r"HeronRollbackWitness\.Watch watch,\s*int counted,\s*bool read,\s*"
                    r"string chat\)", code)
          is not None,
          "KeepAndWatch is handed what Revit counted in the job, whether all of it was "
          "read, and the chat that kept it (Q-88) - a keep cannot be told to the guard "
          "without them")
    check(in_order(keep, "group.Assimilate() != TransactionStatus.Committed",
                   "Undo.Kept(key,", "DateTime.Now, counted, read, chat, RevitDispatcher.RunningWorkflow)",
                   "Witness.Assimilated("),
          "the guard is told only once Assimilate committed, WITH the count, the chat and the "
          "run - a job Revit counted nothing in left no undo entry, only the chat that kept one "
          "may undo it, and the Activity pane undoes only the run it shows (row 5b-1741) - and "
          "before the witness, so a witness that throws cannot lose it")
    unkept = NOTE.body_of(keep, "if (group.Assimilate() != TransactionStatus.Committed)")
    check("RevitUndo.Doubt(doc," in unkept,
          "a group Revit did not report kept is not left looking as it did: the guard doubts")
    check(re.search(r"catch\s*\(\s*Exception\s+ex\s*\)\s*\{[^}]*RevitUndo\.Doubt\(null,", keep)
          is not None,
          "and a keep the guard could not be told of makes every model refuse")
    run = NOTE.body_of(code, "public static string Run(")
    check(re.search(r"KeepAndWatch\(\s*group,\s*target,\s*watch,\s*described == null \? 0 : "
                    r"described\.Total,\s*described != null && described\.Unreadable == 0,"
                    r"\s*client\)", run) is not None
          and in_order(run, "var client = HeronConversation.ChatOf(",
                       "Json.ReadString(request, HeronConversation.EnvelopeField)",
                       "KeepAndWatch(group, target, watch,"),
          "a fragment job's keep passes Revit's own count - added, changed and deleted, "
          "setup steps included - whether an event went unread, and the request's own "
          "conversation as the chat that kept it (Q-88)")
    move = NOTE.code_only(NOTE.read(MOVE) or "")
    check("RevitFragment.KeepAndWatch(group, doc, watch, reallyMoved + partly, true, chat)" in move
          and re.search(r'ExecuteMove\(app,\s*Json\.ReadString\(request,\s*""\),\s*'
                        r"HeronConversation\.ChatOf\(\s*Json\.ReadString\(request,\s*"
                        r"HeronConversation\.EnvelopeField\)\)\)", move) is not None
          and "string ExecuteMove(UIApplication app, string token, string chat)" in move,
          "a move's keep passes how many elements were verified to have moved, and the "
          "conversation of the request that moved them")
    loose = [name for name, text in (("RevitFragment.cs", code), ("RevitWrite.cs", move))
             if re.search(r"KeepAndWatch\(\s*\w+,\s*\w+,\s*\w+\s*\)", text)]
    check(not loose, "and no keep anywhere is made without a count%s"
          % ("" if not loose else " - found in " + ", ".join(loose)))
    chatless = [name for name, text in (("RevitFragment.cs", code), ("RevitWrite.cs", move))
                for args in re.findall(r"\bKeepAndWatch\(\s*group\s*,(.*?)\);", text, re.S)
                if args.strip().split(",")[-1].strip() not in ("client", "chat")]
    found_keeps = sum(len(re.findall(r"\bKeepAndWatch\(\s*group\s*,", text))
                      for text in (code, move))
    check(found_keeps == 2 and not chatless,
          "and no keep anywhere is made without the chat that kept it - %d keep(s) found%s"
          % (found_keeps, "" if not chatless else ", without one in " + ", ".join(chatless)))

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
                   "if (refusal == null)",
                   "refusal = Guard.ChatRefusal(key, doc.Title, HeronConversation.ChatOf(",
                   "Json.ReadString(request, HeronConversation.EnvelopeField)",
                   "if (refusal != null) code =",
                   "if (checkOnly) return Checked(",
                   "if (refusal != null) return Json.Error(",
                   "return Post(app, doc, key, undo);"),
          "the pinned model, then the guard, then WHOSE change it is (Q-88) - asked of the "
          "request that posts, by its own conversation - then the post")
    post = NOTE.body_of(op, "static string Post(")
    check(in_order(post, "app.PostCommand(undo);", "Guard.SentFor(key, DateTime.Now);",
                   "_posted = true;")
          and "Refusal(" not in post,
          "the one post marks the Undo sent after it, so a second request before Revit "
          "reports it is refused - and asks nothing itself: every door asks before it")
    check(last.count("Guard.ChatRefusal(") == 1 and op.count("ChatRefusal(") == 1,
          "the chat is asked in that one place: one request, nothing checked in one and "
          "posted in another")
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
    check(in_order(post, "app.PostCommand(undo);", "_posted = true;"),
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
    check(re.search(r'\{\s*"undo_own",\s*HeronRisk\.Modify\s*\}', registry) is not None,
          "undo_own is declared MODIFY, so it needs Changes ON like any change")
    check(re.search(r'\{\s*"undo_own_check",\s*HeronRisk\.Read\s*\}', registry) is not None,
          "undo_own_check is declared READ: it sends nothing, so it answers with Changes off")
    check(re.search(r'case "undo_own":\s*return RevitUndo\.Last\(app, request, false, null\);',
                    raw_operations) is not None,
          "undo_own is routed to RevitUndo.Last with the whole request - the pinned model "
          "is named in it, and the chat is its conversation")
    check(re.search(r'case "undo_own_check":\s*return RevitUndo\.Last\(app, request, true, '
                    r'Gate\("undo_own"\)\);', raw_operations) is not None,
          "undo_own_check to the same code with nothing sent, told what the switches would "
          "answer the real one")
    # Q-88: THE NAMES A CHAT STARTED BEFORE STILL SENDS POST NOTHING. An add-in
    # built before answers `undo_guarded` by posting for whichever chat's change
    # is on top, so the chat never sends it; this add-in answers it - and
    # `undo_check` - with a refusal, at READ, so no Changes switch stands
    # between that chat and the reason.
    older = NOTE.body_of(op, "static string OlderChat(")
    check(re.search(r'\{\s*"undo_guarded",\s*HeronRisk\.Read\s*\}', registry) is not None
          and re.search(r'\{\s*"undo_check",\s*HeronRisk\.Read\s*\}', registry) is not None
          and re.search(r'case "undo_guarded":\s*return RevitUndo\.OlderChat\(false\);',
                        raw_operations) is not None
          and re.search(r'case "undo_check":\s*return RevitUndo\.OlderChat\(true\);',
                        raw_operations) is not None
          and operations.count("RevitUndo.Last(") == 2,
          "undo_guarded and undo_check, the names a chat started before Q-88 sends, are READ "
          "and answered by OlderChat - never by RevitUndo.Last, which can post")
    raw_older = NOTE.body_of(raw_op, "static string OlderChat(")
    check(bool(older) and "PostCommand" not in older and "Guard." not in older
          and 'Json.Error("undo_refused"' in raw_older
          and "Nothing was sent to Revit and nothing was undone." in raw_older
          and "Ctrl+Z in Revit still undoes" in raw_older,
          "and OlderChat posts nothing, asks the guard nothing, and refuses as undo_refused - "
          "which that chat's Heron already reads as a refusal - saying nothing was sent and "
          "that Ctrl+Z still works")
    # THE FIRST BUILD'S NAME ANSWERS NOTHING (row 5b-1604, the second review).
    # That build posts on the shared id and counts every keep as on top; a
    # Revit still running it knows `undo_last`. So the repaired pair never
    # uses that name: the server cannot ask an unrepaired add-in to post.
    tools_text = NOTE.read(os.path.join(ROOT, "mcp", "server", "heron_tools.py")) or ""
    server_text = NOTE.read(os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")) or ""
    check(re.search(r'\{\s*"undo_last"\s*,', registry) is None
          and 'case "undo_last":' not in raw_operations,
          "the first build's operation name, undo_last, is declared and routed nowhere")
    check('"revit_undo_last":          (MODIFY,  "undo_own")' in tools_text
          and re.search(r'session\.request\(\s*"undo_(last|guarded|check)"', server_text) is None
          and '"undo_own_check" if check_only else "undo_own"' in server_text,
          "and the chat's tool sends undo_own, never undo_guarded or undo_last - an add-in "
          "built before Q-88 answers that it has no such operation, and nothing is posted")
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

    print("\n8. the Activity pane's Undo: the modeller's own click, by D-157's rule, for the "
          "change it shows (row 5b-1741)")
    pane = NOTE.body_of(op, "public static string FromPane(UIApplication app, string request)")
    check(in_order(pane, "app.ActiveUIDocument", "if (doc == null)", "return Json.Error(",
                   "!string.Equals(doc.Title, document, StringComparison.Ordinal)", "return Json.Error(",
                   "RevitOperations.DocumentKey(doc)",
                   "RevitCommandId.LookupPostableCommandId(PostableCommand.Undo)",
                   "app.CanPostCommand(undo)",
                   "Guard.Refusal(key, doc.Title, true, canPost)",
                   "if (refusal == null)", "refusal = Guard.PaneRefusal(key, doc.Title,",
                   "if (refusal != null) return Json.Error(",
                   "return Post(app, doc, key, undo);"),
          "the model on screen, which must be the model the pane's change was kept in; then "
          "D-157's guard, exactly as a chat's Undo asks it; then whether Heron's last change "
          "there IS the change the pane shows; and only then the one post")
    check(bool(pane) and "ChatRefusal" not in pane and "HeronConversation" not in pane
          and "NotThePinned" not in pane and "HoldTarget" not in pane,
          "and no chat is asked - it is the modeller's own button, like Ctrl+Z (the owner's "
          "answer, 2026-11-16) - and no model but the one on screen is looked for")
    raw_pane = NOTE.body_of(raw_op, "public static string FromPane(UIApplication app, string request)")
    check('Json.ReadString(request, "run")' in raw_pane and 'Json.ReadString(request, "document")' in raw_pane
          and "HeronConversation.EnvelopeField" not in raw_pane,
          "what the pane asks for is read from the request - its run and its model - and never a chat")
    check(op.count("Guard.PaneRefusal(") == 1 and pane.count("Guard.PaneRefusal(") == 1
          and len(re.findall(r"(?<!Can)PostCommand\(", op)) == 1,
          "the pane's question is asked in that one place, and both doors share the one post")
    check(in_order(last, "Guard.ChatRefusal(") and "PaneRefusal" not in last,
          "and what a CHAT may undo is unchanged: undo_own still asks whose change it is (Q-88), "
          "and never the pane's question")
    pane_rule = NOTE.body_of(guard_code, "internal string PaneRefusal(")
    check(re.search(r"string\.Equals\(entry\.Run,\s*run,\s*StringComparison\.Ordinal\)", pane_rule)
          is not None and "string.IsNullOrEmpty(entry.Run)" in pane_rule
          and "string.IsNullOrEmpty(run)" in pane_rule,
          "the guard compares the run exactly, and a keep or a press with none is refused")
    check(re.search(r"internal void Kept\(string document, string job, string label, DateTime keptAt,"
                    r"\s*int counted, bool read, string chat, string run\)", guard_code) is not None
          and "Run = run" in kept,
          "the keep records the run it was kept in - the dispatcher's workflow id, the one the "
          "run record the pane reads carries")
    running = NOTE.body_of(dispatcher, "internal static string RunningWorkflow")
    check("_running" in running and ".WorkflowId" in running,
          "and that id is the job Revit's thread is running, read where the keep is made")


def rule():
    print("\n9. the rule itself, built and run without Revit")
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
