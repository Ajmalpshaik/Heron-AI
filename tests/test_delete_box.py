#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Anything a kept change deletes is asked in Revit first - D-142.

    python tests/test_delete_box.py

WHY THIS EXISTS
---------------
The owner, 2026-10-18: "anything deleting from the model should come through
the pop-up, in any situation, Ask to Keep on or off. It shows: this many
elements are going to be deleted - can we delete or not? If I agree, it
deletes. If I disagree, no." Until then "delete these" ran on the live
selection and, with Ask to Keep off or under D-138's grant, was kept with no
box at all, although Constitution Article 7 asks for an explicit yes.

THREE HALVES
------------
  THE PYTHON     the client waits for the box whatever Ask to Keep says, the
                 chat's sentence for the modeller's Delete, how each refusal is
                 classified, and the chat's words sent for the box to quote.
                 RUN here.
  THE WIRING     where the gate sits in RevitFragment.Run - after the change is
                 described, before D-138's grant and D-126's box are looked at,
                 keyed on a run that keeps and on Revit's count alone - and how
                 the box asks: inside HeronAsks, Don't delete the default, only
                 the Delete link deleting, the selection put back whatever the
                 answer, and the run made again. Read as TEXT - a weak test of
                 behaviour and a strong one of a missing line or a line in the
                 wrong place.
  THE WORDS      HeronDeleteQuestion.cs, linked by source into
                 tests/Heron.KeepQuestion.TestHost and run there by
                 tests/test_keep_question.py, sections 11 to 15.

None of it proves Revit then does what it is told. NEEDS-CHECKING Group HL
says what the PC owes.

MEASURED AGAINST THE CODE AS IT STOOD: with the files from before D-142 every
wiring section and the timeout section fail, and the suite exits 1.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "tests"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import test_failure_note as NOTE_SUITE                            # noqa: E402
import heron_config as CFG                                         # noqa: E402
import heron_failure as FAIL                                       # noqa: E402
import heron_run_records as RUNS                                   # noqa: E402

ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
EXECUTOR = os.path.join(ADDIN, "RevitFragment.cs")
GATE = os.path.join(ADDIN, "RevitFragmentDelete.cs")
WORDS = os.path.join(ADDIN, "HeronDeleteQuestion.cs")
GRANT = os.path.join(ADDIN, "HeronKeepGrant.cs")
COLLECTOR = os.path.join(ADDIN, "RevitChangedSet.cs")
DISPATCHER = os.path.join(ADDIN, "RevitDispatcher.cs")
CLOCK = os.path.join(ADDIN, "HeronRunClock.cs")
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    try:
        return io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return ""


def code(path):
    return NOTE_SUITE.code_only(read(path))


def before(text, first, then):
    """`first` appears, `then` appears, and the first comes first."""
    a, b = text.find(first), text.find(then)
    return 0 <= a < b


def timeout():
    print("\n1. the client waits for the box on every run that keeps, Ask to Keep on or off")
    for asks in ("false", "true"):
        for operation in (10, 60, 300):
            for ask_for in (1, 300, 3600):
                values = dict(CFG.DEFAULTS)
                values["write.askBeforeKeeping"] = asks
                values["revit.operationTimeoutSeconds"] = str(operation)
                values["revit.askTimeoutSeconds"] = str(ask_for)
                longest = (CFG.busy_timeout(values) + CFG.operation_timeout(values)
                           + CFG.ask_timeout(values))
                waits = CFG.fragment_timeout(values, keeping=True)
                check(waits > longest,
                      "Ask to Keep %s, operation=%s ask=%s -> the add-in answers within %g, "
                      "a keeping run waits %g" % (asks, operation, ask_for, longest, waits))
    values = dict(CFG.DEFAULTS)
    check(CFG.fragment_timeout(values, keeping=True)
          == CFG.fragment_timeout(values) + CFG.ask_timeout(values),
          "with Ask to Keep off - the default - a keep still waits for the delete box")
    check(CFG.fragment_timeout(values) == 180.0,
          "and a read, or a run that rolls back, waits exactly the 180 it always did")


def reply():
    print("\n2. the chat is told the modeller was asked, how many were shown, and Delete")
    batch = RUNS.Batch()
    asked = ("The modeller was asked in Revit whether to delete 7 elements - listed by "
             "category, 6 of them selected on screen - and pressed DELETE.")
    kept = {"verdict": "'delete-elements' was KEPT: ...", "verdictKind": "kept",
            "verdictShort": "'delete-elements' was KEPT.", "deleteAsked": True,
            "deleteShown": 7, "deleteAnswer": "delete", "deleteAsking": asked}
    line = RUNS.verdict_line(kept, batch) or ""
    check(line.startswith(asked), "the delete sentence is printed first, before the KEPT verdict")
    both = dict(kept, asking="You were asked in Revit and pressed KEEP after seeing the result.")
    line = RUNS.verdict_line(both, batch) or ""
    check(line.startswith(asked) and "pressed KEEP" in line,
          "a DRAFT's one run asked both questions says both, the delete box first")
    plain = {"verdict": "'set-x' was KEPT: ...", "verdictKind": "kept"}
    check(RUNS.verdict_line(plain, batch) == "'set-x' was KEPT: ...",
          "a change that deleted nothing reads exactly as it did")

    print("\n3. every refusal is classified: the modeller's no is never a fault to retry")
    rows = {
        "delete_declined": (FAIL.ROLLED_BACK, FAIL.STOP),
        "delete_too_late": (FAIL.ROLLED_BACK, FAIL.STOP),
        "delete_set_changed": (FAIL.ROLLED_BACK, FAIL.START_OVER),
        "delete_not_measured": (FAIL.ROLLED_BACK, FAIL.STOP),
        "delete_in_publish": (FAIL.ROLLED_BACK, FAIL.STOP),
        "delete_not_shown": (FAIL.UNKNOWN, FAIL.LOOK_AT_THE_MODEL),
        "awaiting_delete_answer": (FAIL.RUNNING, FAIL.WAIT),
    }
    for name, (outcome, step) in sorted(rows.items()):
        got = FAIL.analyse({"ok": False, "error": name, "message": "m"}, writes=True)
        check(got is not None and got.outcome == outcome and got.next_step == step,
              "%s is %s, %s" % (name, outcome, step))
    declined = FAIL.explain(FAIL.analyse({"ok": False, "error": "delete_declined",
                                          "message": "NOTHING WAS DELETED."}, writes=True))
    check("NOTHING WAS DELETED." in declined and "try" not in declined.lower(),
          "and a no is said in the add-in's own words, with no offer to try again")

    print("\n4. the chat's own words travel, to be quoted in the box and obeyed by nothing")
    server = read(SERVER)
    check(re.search(r'args\["chatWords"\]\s*=\s*request\.strip\(\)', server) is not None,
          "revit_change sends the words it was given as chatWords")
    gate = read(GATE)
    check(gate.count('"chatWords"') == 1
          and re.search(r'HeronDeleteQuestion\.Content\(\s*Json\.ReadString\(request, "chatWords"\)',
                        gate) is not None,
          "and the add-in reads them in one place: the box's text")


def wiring():
    executor = code(EXECUTOR)
    gate_code = code(GATE)
    run = NOTE_SUITE.body_of(executor, "public static string Run(")

    print("\n5. the gate sits in Run after the change is described, ahead of the grant and the box")
    check("internal static partial class RevitFragment" in executor
          and "internal static partial class RevitFragment" in gate_code,
          "RevitFragment.cs and RevitFragmentDelete.cs are one class")
    check(before(run, "described = changed.Describe();", "DeleteGate(")
          and before(run, "DeleteGate(", "UnderTheGrant(")
          and before(run, "DeleteGate(", "AskTheModeller("),
          "DeleteGate is called after Describe and before UnderTheGrant and Keep or Discard")
    check(re.search(r"DeleteGate\(app, target, name, request, apply \|\| asking,", run) is not None,
          "it asks of every run that keeps - apply, or a DRAFT's one run - and of no preview")
    check("HeronPermissions.AsksBeforeKeeping" not in gate_code and "askToKeep" not in gate_code,
          "Ask to Keep never reaches it")
    check("HeronKeepGrant" not in gate_code and "Delete" not in code(GRANT),
          "and nor does the grant: the rule is at the call site, HeronKeepGrant.cs untouched")
    check(re.search(r"if\s*\(\s*confirmed\s*!=\s*null\s*\)\s*\{\s*askToKeep\s*=\s*false;\s*"
                    r"keptUnderGrant\s*=\s*null;", run) is not None,
          "a confirmed delete is never called kept without asking, and is not asked twice")
    check(re.search(r"if\s*\(\s*deleteAgain\s*\)\s*\{\s*said\s*=\s*new HeronFailureNote\(\);"
                    r"\s*goto runAgain;", run) is not None
          and before(run, "runAgain:", "new TransactionGroup(target, label)"),
          "on Delete the whole run is made again, from a new group, with a fresh failure note")
    check("if (confirmed != null) answer = confirmed.OnReply(answer);" in run,
          "and the reply of the kept second run carries the delete box's fields")

    print("\n6. by Revit's own count, never by a card")
    collector = code(COLLECTOR)
    check("public ICollection<ElementId> DeletedIds" in collector
          and "_deleted.Add(id);" in collector,
          "RevitChangedSet keeps the ids Revit reported deleted, not only a count")
    gate = NOTE_SUITE.body_of(gate_code, "private static string DeleteGate(")
    check("changed.DeletedIds" in gate and "risk" not in gate.lower().replace("risky", ""),
          "the gate reads what Revit deleted and nothing a card says")
    check(re.search(r"HeronDeleteQuestion\.Differs\(confirmed\.Ids,\s*now\)", gate) is not None
          and before(gate, "HeronDeleteQuestion.Differs(", "RollBackAndCheck("),
          "the second run is held to the set shown, and rolled back when it differs")
    check(before(gate, "var measured = RollBackAndCheck(group, target, watch);", "AskToDelete(")
          and "measured.Held" in gate and "InModel(target, id)" in gate,
          "the first run is rolled back, and every element found again, before anything is asked")
    # EVERY REFUSAL IT ANSWERS FOLLOWS A ROLLBACK THROUGH THE HELPER, with no
    # way out between - which is what lets tests/test_rollback_witness.py take
    # Run's `return deleteRefused;` as rolled back.
    refusals = [m.start() for m in re.finditer(r"\breturn\s+(?!null\s*;)", gate)]
    rolled = True
    for at in refusals:
        last = gate.rfind("RollBackAndCheck(group, target, watch)", 0, at)
        if last < 0 or "return null;" in gate[last:at]:
            rolled = False
    check(len(refusals) >= 5 and rolled,
          "each of DeleteGate's %d refusals is answered after the group was rolled back "
          "through the helper" % len(refusals))
    check(before(gate, "if (sendsOut)", "AskToDelete("),
          "an export that deletes is refused rather than sent out twice")
    check("described.Witnessed" in gate and "ReadFootprint.Of(target)" in gate,
          "a delete whose report never arrived is caught by the model's own count")

    print("\n7. the box: asked of a person, Don't delete the default, only Delete deletes")
    ask = NOTE_SUITE.body_of(gate_code, "private static string AskToDelete(")
    check(re.search(r"using\s*\(\s*HeronDialogWatch\.HeronAsks\(\)\s*\)\s*\{\s*result\s*=\s*box\.Show\(\);",
                    ask) is not None,
          "it is shown inside HeronAsks, so the job's dialog watch never answers it")
    check(before(ask, "RevitDispatcher.AwaitingDeleteAnswer()", "box.Show()"),
          "the dispatcher is told a person is being asked before it is shown")
    check("CommandLink3" not in ask,
          "there is no third link - nothing like keep the rest of this job")
    check(before(ask, "AddCommandLink(TaskDialogCommandLinkId.CommandLink2",
                 "box.DefaultButton = TaskDialogResult.CommandLink2;"),
          "Don't delete is the default, set once its link exists")
    check(re.search(r"result\s*==\s*TaskDialogResult\.CommandLink1\s*\?\s*\(\s*HeronDeleteQuestion\.Late\(",
                    ask) is not None,
          "only the Delete link can delete, and only in time")
    check(before(ask, "box.Show()", "var restored = putBack();")
          and before(ask, "var restored = putBack();", "confirmed = new ConfirmedDelete("),
          "their selection and view are put back whatever they answered, before the second run")
    check("HeronRunClock.Again();" in ask and "internal static void Again()" in code(CLOCK),
          "the second run gets its own time on the run clock")
    check("new Transaction(" not in gate_code and "new TransactionGroup(" not in gate_code,
          "showing them opens no transaction - nothing reaches the undo list")

    print("\n8. no answer in time deletes nothing")
    dispatcher = code(DISPATCHER)
    dispatch = NOTE_SUITE.body_of(dispatcher, "public string Dispatch(string request)")
    check(before(dispatch, "job.AnswerBy(", "_queue.Enqueue(job)"),
          "the last moment a Delete is kept is set before Revit can take the job")
    check(before(dispatch, "if (!finished) job.CallerGone();", "job.AskedToDelete"),
          "and a caller that stops waiting ends it at once")
    check('"awaiting_delete_answer"' in read(DISPATCHER),
          "a caller that stops waiting on the delete box is told nothing is deleted")


def main():
    timeout()
    reply()
    wiring()
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    anything a kept change deletes is asked in Revit first (D-142)")
    print("        - the words are run by tests/test_keep_question.py, sections 11 to 15")
    return 0


if __name__ == "__main__":
    sys.exit(main())
