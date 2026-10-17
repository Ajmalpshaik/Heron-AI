#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Revit asks the modeller Keep or Discard before a change from a chat is kept.

    python tests/test_keep_question.py

WHY THIS EXISTS
---------------
D-99 kept every revit_change at once, with no preview. D-126 supersedes it:
the change runs inside Heron's TransactionGroup, Revit selects and isolates
what it touched, and a TaskDialog asks Keep or Discard. Keep is one undo entry;
Discard rolls the group back and puts the modeller's selection back.

TWO HALVES, BECAUSE ONE OF THEM CANNOT BE RUN HERE
---------------------------------------------------
  THE RULE      the words the modeller reads, the mapping from a dialog result
                to Keep or Discard, and D-60's cap of 500 - HeronKeepQuestion.cs,
                which names no Autodesk type and is linked by source into
                tests/Heron.KeepQuestion.TestHost. Built and RUN here.
  THE WIRING    who decides to ask (the owner's switch, read in the add-in -
                never a field in the request, Golden Rule 19), the ORDER inside
                the group (collect, show, ask, end the isolation, then keep or
                roll back), the dispatcher's longer wait while the modeller is
                asked, the banner stepping aside, the client's deadline, and
                how a Discard is classified. That half calls the Revit API and
                is read as TEXT - a weak test of behaviour and a strong one of a
                missing line or a line in the wrong place.

Neither half proves Revit then does what it is told. That is the PC's, and
NEEDS-CHECKING Group ET says what it owes.

MEASURED AGAINST THE CODE AS IT STOOD: with the files from before D-126 every
wiring section fails and the host does not build, and this suite exits 1 rather
than raising.

IT EXITS 3 WHEN .NET IS ABSENT AND THE WIRING PASSED, and that is NOT a pass:
the rule was not run.
"""

import glob
import io
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "tests"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

# THE SAME READERS test_failure_note.py uses on the same file - comments out,
# string contents blanked, a braces-balanced body - so the two suites read
# RevitFragment.cs by one rule.
import test_failure_note as NOTE_SUITE                            # noqa: E402
import heron_config as CFG                                         # noqa: E402
import heron_failure as FAIL                                       # noqa: E402

ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
EXECUTOR = os.path.join(ADDIN, "RevitFragment.cs")
DISPATCHER = os.path.join(ADDIN, "RevitDispatcher.cs")
BANNER = os.path.join(ADDIN, "HeronActivityBanner.cs")
QUESTION = os.path.join(ADDIN, "HeronKeepQuestion.cs")
COLLECTOR = os.path.join(ADDIN, "RevitChangedSet.cs")
PERMISSIONS = os.path.join(ROOT, "platform", "Heron.Core", "HeronPermissions.cs")
CONFIG_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronConfig.cs")
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")
CLIENT = os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py")
HOST = os.path.join(ROOT, "tests", "Heron.KeepQuestion.TestHost")
COULD_NOT_RUN = 3

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    try:
        return io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return None


def code(path):
    return NOTE_SUITE.code_only(read(path) or "")


def before(text, first, then):
    """`first` appears, `then` appears, and the first comes first."""
    a, b = text.find(first), text.find(then)
    return 0 <= a < b


def wiring():
    executor = code(EXECUTOR)
    raw = read(EXECUTOR) or ""

    print("\n1. the rule names no Autodesk type, so a host can run it without Revit")
    question = read(QUESTION)
    check(question is not None, "HeronKeepQuestion.cs is there")
    check(question is not None and "Autodesk" not in NOTE_SUITE.code_only(question),
          "and names no Autodesk type")
    check(os.path.exists(COLLECTOR), "the change collector is there - RevitChangedSet.cs")

    print("\n2. who decides to ask: the owner's switch, read in the add-in")
    check("var askToKeep = apply && HeronPermissions.AsksBeforeKeeping();" in executor,
          "a keep from a chat is asked only on a run that keeps, and only when the owner's "
          "switch says so")
    # THE ONE REQUEST WORD THAT ASKS IS D-119's "ask" - a narrowing, never a
    # widening: it asks for a DRAFT's one run to be shown to the modeller.
    # Nothing on the wire may turn the owner's question OFF.
    asked_words = re.findall(r'Json\.Read\w+\(\s*request\s*,\s*"([^"]*)"\)\s*,\s*"ask"', raw)
    check(re.search(r'Json\.Read\w+\(\s*request\s*,\s*"[^"]*[Aa]sk[^"]*"', raw) is None,
          "no request field named for asking is read (Golden Rule 19)")
    check(asked_words == ["apply"],
          "the only request word that asks is D-119's apply = \"ask\" - found %s" % asked_words)
    permissions = code(PERMISSIONS)
    check(re.search(r'GetBool\(\s*AskBeforeKeepingKey\s*,\s*true\s*\)', permissions) is not None,
          "the switch is read fresh, and a value nobody can read means ASK")
    check('"write.askBeforeKeeping"' in (read(PERMISSIONS) or ""),
          "under the key write.askBeforeKeeping")
    config_cs = read(CONFIG_CS) or ""
    check(re.search(r'\{\s*"write\.askBeforeKeeping"\s*,\s*"true"\s*\}', config_cs) is not None
          and CFG.DEFAULTS.get("write.askBeforeKeeping") == "true",
          "on by default, on both sides of the bridge")
    for path in (SERVER, CLIENT):
        said = read(path) or ""
        check("askBeforeKeeping" not in said.replace("write.askBeforeKeeping", ""),
              "%s sends no field that could skip the question" % os.path.basename(path))

    print("\n3. one box for both doors, inside the group: collect, show, ask, end the showing, decide")
    run = NOTE_SUITE.body_of(executor, "public static string Run(UIApplication app, string request, bool writing)")
    # EVERY WRITE, SINCE 5b-407: the change report counts a write nobody is
    # asked about too, so the collector is no longer made only for a door
    # that asks - either form is still the one collector, held in a `using`.
    check(re.search(r"using\s*\(\s*var\s+changed\s*=\s*(?:asking\s*\|\|\s*askToKeep\s*\?\s*)?"
                    r"new\s+RevitChangedSet\(", run) is not None,
          "the collector is held in a `using` for either door, so its handler comes off "
          "however the group ends")
    check(before(run, "group.Start();", "changed.Begin();"),
          "and it starts listening only once the group has started")
    check(re.search(r"if\s*\(\s*asking\s*\|\|\s*askToKeep\s*\)\s*\{[^}]*answered\s*=\s*AskTheModeller\(",
                    run) is not None,
          "one box for both: a DRAFT's one run (D-119) and a keep from a chat (D-126)")
    check(run.count("AskTheModeller(") == 1,
          "and it is asked from one place only")
    check("if (answered != null) apply = answered == Keep;" in run,
          "the modeller's answer, when there was a box, is what sets apply")
    # ONE PLACE KEEPS, AFTER A FRESH LOOK AT THE DIALOG WATCH (Group EL).
    # Showing the change commits a transaction of its own, and a Revit dialog
    # met there must stop the job like one met in the fragment.
    asked_at = run.find("answered = AskTheModeller(")
    recheck = run.find("if (HeronDialogWatch.Refused)", asked_at)
    kept_at = run.find("if (apply) keptNote = KeepAndWatch(group, target, watch);")
    check(0 < asked_at < recheck < kept_at,
          "the box asks, the dialog watch is asked again, and only then is anything kept")
    check(before(run, "rolledBack = RollBackAndCheck(group, target, watch);",
                 "selectionRestored = putBack();"),
          "and on Discard their selection is put back after the rollback")
    check("WithAnswer(verdict, answered, refreshed)" in run
          and "answered != null && answered != Keep" in run,
          "the reply says who decided (D-119's fields), and a Discard's verdict is its own")

    ask = NOTE_SUITE.body_of(executor, "private static string AskTheModeller(")
    check(bool(ask), "AskTheModeller is there")
    check("Assimilate(" not in ask and "SafeRollBack(" not in ask,
          "and it only asks - it never keeps or rolls back the group itself")
    check(before(ask, "changed.End();", "Isolate("),
          "the collector stops before anything is shown, so showing is not counted as the change")
    check(re.search(r"if\s*\(\s*!neverProved\s*&&\s*!HeronKeepQuestion\.Asks\(", ask) is not None
          and before(ask, "HeronKeepQuestion.Asks(", "box.Show()"),
          "a keep from a chat that Revit counted no change in is not asked - a DRAFT's run always is")
    check("front.Document.Equals(target)" in ask,
          "it draws on the model only when that model is the one in front")
    check(before(ask, "var theirs = front.Selection.GetElementIds();",
                 "front.Selection.SetElementIds(show);"),
          "the modeller's own selection is saved BEFORE it is replaced (D-60)")
    check(before(ask, "Isolate(", "RefreshActiveView()") and before(ask, "RefreshActiveView()", "box.Show()"),
          "the view is redrawn after the change is isolated, and before the box")
    check("HeronKeepQuestion.Headline(shown, neverProved)" in ask
          and "HeronKeepQuestion.Content(" in ask,
          "the box's words come from HeronKeepQuestion, which the host proves")
    # THE JOB'S DIALOG WATCH (Group EL) declines every Revit dialog a job
    # meets. This box is the modeller's, so it is shown with the watch
    # stood down - or Heron would answer its own question with Cancel.
    check(re.search(r"using\s*\(\s*HeronDialogWatch\.HeronAsks\(\)\s*\)\s*\{\s*result\s*=\s*box\.Show\(\);",
                    ask) is not None,
          "the box is shown inside HeronDialogWatch.HeronAsks(), so the job's dialog "
          "watch never answers it")
    check(before(ask, "RevitDispatcher.AwaitingModeller()", "box.Show()"),
          "the dispatcher is told a person is being waited for, before the box")
    shown_at = ask.find("box.Show()")
    ends = ask.find("DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate)")
    check(0 < shown_at < ends < ask.find("return Keep;"),
          "the isolation is ended INSIDE the group, before the answer goes back to be "
          "kept or rolled back (D-115)")

    # ROW 5b-621: Revit's DefaultButton setter refuses a button the box does
    # not have yet. Set in the TaskDialog's initializer, before AddCommandLink,
    # it threw "Corresponding button not found" on the first kept change in
    # Revit 2024 - and threw from outside the guard round Show, so the
    # isolation was never ended and the chat got the raw .NET sentence.
    print("\n3b. the box is built where a failure to build it is a Discard, its default set last")
    made = ask.find("new TaskDialog(")
    link1 = ask.find("AddCommandLink(TaskDialogCommandLinkId.CommandLink1")
    link2 = ask.find("AddCommandLink(TaskDialogCommandLinkId.CommandLink2")
    default = ask.find("DefaultButton = TaskDialogResult.CommandLink2")
    check(0 <= made < link1 < link2 < default,
          "Discard is made the default only AFTER both command links are added - "
          "Revit refuses a default the box does not have yet")
    check(ask.count("DefaultButton") == 1,
          "and it is set in one place only, never also in the initializer")
    tries = [m.start() for m in re.finditer(r"\btry\s*\{", ask) if m.start() < made]
    guard = tries[-1] if made >= 0 and tries else -1
    caught = ask.find("catch", guard) if guard >= 0 else -1
    check(guard >= 0 and 0 <= made < shown_at < caught
          and ask.find("catch", guard, shown_at) < 0,
          "the box is built inside the same try that guards box.Show(), so a box that "
          "cannot be built is answered as Closed - rolled back, nothing kept")
    check(0 <= caught < ends,
          "and the isolation is ended after that guard, whichever way it went")
    # EVERY TASKDIALOG IN THE ADD-IN, NOT JUST THIS ONE: the same setter
    # refuses the same mistake in any box with command links.
    for path in sorted(glob.glob(os.path.join(ROOT, "revit", "**", "*.cs"), recursive=True)):
        if os.sep + "obj" + os.sep in path or os.sep + "bin" + os.sep in path:
            continue
        text = code(path)
        for hit in re.finditer(r"DefaultButton\s*=\s*TaskDialogResult\.(CommandLink\d)", text):
            box_at = text.rfind("new TaskDialog(", 0, hit.start())
            added = text.find("AddCommandLink(TaskDialogCommandLinkId." + hit.group(1),
                              box_at, hit.start()) if box_at >= 0 else -1
            check(box_at >= 0 and added > box_at,
                  "%s: DefaultButton = %s comes after that link is added to its box"
                  % (os.path.relpath(path, ROOT).replace(os.sep, "/"), hit.group(1)))

    showing = NOTE_SUITE.body_of(executor, "View Isolate(")
    check("view.IsTemporaryHideIsolateActive()" in showing,
          "a view the modeller has already isolated in is not isolated again")
    check("InItsOwnTransaction(" in showing,
          "isolating goes through a transaction of its own")
    own = NOTE_SUITE.body_of(executor, "bool InItsOwnTransaction(")
    check(before(own, "new Transaction(", "Discipline(") and before(own, "Discipline(", ".Commit("),
          "under Heron's failure rule before it commits")

    print("\n4. the verdict says what happened")
    verdict = NOTE_SUITE.body_of(executor, "string WithVerdict(")
    check("HeronKeepQuestion.NothingToAskSentence" in verdict,
          "a run kept with no question says why none was put")
    check(before(verdict, "var held = rolledBack != null && rolledBack.Held;",
                 "HeronKeepQuestion.Discarded(name, held, selectionRestored)"),
          "and a Discard has its own verdict - never the plain rollback's \"send it again with apply\"")

    print("\n5. the dispatcher waits for the modeller, and the banner steps aside")
    dispatcher = code(DISPATCHER)
    awaiting = NOTE_SUITE.body_of(dispatcher, "internal static void AwaitingModeller()")
    check("job.MarkAskedModeller()" in awaiting and "banner.StepAside(true)" in awaiting,
          "AwaitingModeller marks the job and moves the banner out of the box's way")
    execute = NOTE_SUITE.body_of(dispatcher, "public void Execute(UIApplication app)")
    check("_runningBanner = _banner;" in execute and "_runningBanner = null;" in execute
          and "if (job.AskedModeller) _banner.StepAside(false);" in execute,
          "the banner comes back when the job returns")
    check(before(execute, "_banner.StepAside(false)", "_banner.End(failure == null, Ended(failure, response)"),
          "before it says how the job ended")
    # RAW TEXT: the field name is a string, and code_only blanks strings.
    ended = NOTE_SUITE.body_of(read(DISPATCHER) or "", "private static string Ended(")
    check('"modellerAnswer"' in ended,
          "and it says Discarded or Kept from the modeller's own answer, not Done")
    banner = code(BANNER)
    check("public void StepAside(bool aside)" in banner, "the banner can step aside")
    render = NOTE_SUITE.body_of(banner, "private void Render()")
    check(before(render, "_aside", "_active"),
          "and while it is aside, a render draws nothing - the check comes first")

    print("\n6. the client waits for the modeller too, on a run that keeps")
    # ASKED BEFORE IT IS CALLED (heron-ship 2a): against a client with no
    # allowance for the modeller this section must FAIL, once and cleanly,
    # not end the suite with an AttributeError.
    ask_timeout = getattr(CFG, "ask_timeout", None)
    takes_keeping = "keeping" in CFG.fragment_timeout.__code__.co_varnames[
        :CFG.fragment_timeout.__code__.co_argcount]
    check(ask_timeout is not None and takes_keeping,
          "the client knows revit.askTimeoutSeconds, and a fragment run can say it keeps")
    if ask_timeout is None or not takes_keeping:
        return
    for operation in (10, 60, 300):
        for ask_for in (1, 60, 300, 3600):
            values = dict(CFG.DEFAULTS)
            values["revit.operationTimeoutSeconds"] = str(operation)
            values["revit.askTimeoutSeconds"] = str(ask_for)
            longest = (CFG.busy_timeout(values) + CFG.operation_timeout(values)
                       + ask_timeout(values))
            waits = CFG.fragment_timeout(values, keeping=True)
            check(waits > longest,
                  "operation=%s ask=%s -> the add-in answers within %g, a keeping run waits %g"
                  % (operation, ask_for, longest, waits))
    values = dict(CFG.DEFAULTS)
    check(CFG.fragment_timeout(values) == 180.0,
          "a read still waits exactly the 180 it always did")
    check(CFG.fragment_timeout(values, keeping=True) == CFG.try_once_timeout(values),
          "and a keep the owner's switch asks about waits what D-119's try-once waits - "
          "the same box, the same wait")
    values["write.askBeforeKeeping"] = "false"
    check(CFG.fragment_timeout(values, keeping=True) == 180.0,
          "and with the owner's question off, a keep does not wait for one")
    server = read(SERVER) or ""
    check("configuration.fragment_timeout(keeping=True)" in server,
          "revit_change waits as a run that keeps")
    check(re.search(r"fragment_timeout\(\s*keeping=writing and apply_it\)", read(CLIENT) or "")
          is not None,
          "and so does the command line's --write --apply")

    print("\n7. a Discard is an answer, never a failure to retry (Article 25)")
    check('if reply.get("asked") and not reply.get("kept"):' in server
          and 'return _ended("refused", "\\n".join(lines))' in server,
          "revit_change reports a box the modeller did not keep as refused, and offers "
          "no after-change table")
    waiting = FAIL.analyse({"ok": False, "error": "awaiting_modeller", "message": "m"}, writes=True)
    check(waiting is not None and waiting.outcome == FAIL.RUNNING
          and waiting.next_step == FAIL.WAIT and not waiting.may_retry,
          "a box still open is RUNNING / WAIT, never sent again on its own")


def rule():
    """The half that runs: build the host and let it check the rule."""
    print("\n8. the rule itself, built and run without Revit")
    tfm = NOTE_SUITE._tfm()
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

    dll = os.path.join(HOST, out_dir, "Heron.KeepQuestion.TestHost.dll")
    ran = subprocess.run(["dotnet", dll], stdout=subprocess.PIPE,
                         stderr=subprocess.STDOUT)
    said = (ran.stdout or b"").decode("utf-8", "replace")
    for line in said.splitlines():
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
    print("PASS    Revit asks Keep or Discard before a change from a chat is kept")
    return 0


if __name__ == "__main__":
    sys.exit(main())
