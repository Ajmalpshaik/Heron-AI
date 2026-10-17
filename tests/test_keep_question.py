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
D-99 kept every revit_change at once, with no preview. D-118 supersedes it:
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
NEEDS-CHECKING Group EO says what it owes.

MEASURED AGAINST THE CODE AS IT STOOD: with the files from before D-118 every
wiring section fails and the host does not build, and this suite exits 1 rather
than raising.

IT EXITS 3 WHEN .NET IS ABSENT AND THE WIRING PASSED, and that is NOT a pass:
the rule was not run.
"""

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
    check("var asking = apply && HeronPermissions.AsksBeforeKeeping();" in executor,
          "the executor asks only on a run that keeps, and only when the owner's switch says so")
    check(re.search(r'Json\.Read\w+\(\s*request\s*,\s*"[^"]*[Aa]sk', raw) is None,
          "and nothing in the request is read to decide it (Golden Rule 19)")
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

    print("\n3. inside the group, in order: collect, show, ask, end the showing, decide")
    run = NOTE_SUITE.body_of(executor, "public static string Run(UIApplication app, string request, bool writing)")
    check(re.search(r"using\s*\(\s*var\s+changed\s*=\s*asking\s*\?\s*new\s+RevitChangedSet\(", run)
          is not None,
          "the collector is held in a `using`, so its handler comes off however the group ends")
    check(before(run, "group.Start();", "changed.Begin();"),
          "and it starts listening only once the group has started")
    check(re.search(r"if\s*\(\s*asking\s*\)\s*\{[^}]*AskTheModeller\(", run) is not None
          and "var keep = apply;" in run and "if (keep) group.Assimilate();" in run,
          "with the switch on the modeller decides; with it off a keep is kept as before")
    # ONE PLACE KEEPS, AFTER A FRESH LOOK AT THE DIALOG WATCH (Group EL).
    # Showing the change commits a transaction of its own, and a Revit dialog
    # met there must stop the job like one met in the fragment.
    asked_at = run.find("AskTheModeller(")
    recheck = run.find("if (HeronDialogWatch.Refused)", asked_at)
    kept_at = run.find("if (keep) group.Assimilate();")
    check(0 < asked_at < recheck < kept_at,
          "the question asks, the dialog watch is asked again, and only then is anything kept")
    check(before(run, "rolledBack = SafeRollBack(group);", "selectionRestored = putBack();"),
          "and on Discard their selection is put back after the rollback")
    check(before(run, "if (discarded)", "Remember(name, state"),
          "a Discard returns before anything is remembered for the next fragment")
    for error in ("modeller_discarded", "discard_unconfirmed"):
        check(re.search(r'Json\.Error\(\s*"%s"' % error, raw) is not None,
              "a Discard answers with its own code: %s" % error)

    ask = NOTE_SUITE.body_of(executor, "HeronKeepQuestion.Answer AskTheModeller(")
    check(bool(ask), "AskTheModeller is there")
    check("Assimilate(" not in ask and "SafeRollBack(" not in ask,
          "and it only asks - it never keeps or rolls back the group itself")
    check(before(ask, "changed.End();", "Isolate("),
          "the collector stops before anything is shown, so showing is not counted as the change")
    check(re.search(r"if\s*\(\s*!HeronKeepQuestion\.Asks\(", ask) is not None
          and before(ask, "HeronKeepQuestion.Asks(", "dialog.Show()"),
          "a run Revit counted no change in is kept without a question")
    check("front.Document.Equals(target)" in ask,
          "it draws on the model only when that model is the one in front")
    check(before(ask, "theirs = front.Selection.GetElementIds();",
                 "front.Selection.SetElementIds(show);"),
          "the modeller's own selection is saved BEFORE it is replaced (D-60)")
    check("AllowCancellation = true" in ask
          and "CommonButtons = TaskDialogCommonButtons.None" in ask
          and "dialog.DefaultButton = TaskDialogResult.CommandLink2;" in ask,
          "two command links, Esc allowed, and Enter on Discard")
    check("HeronKeepQuestion.Decide((int)shown, (int)TaskDialogResult.CommandLink1)" in ask,
          "and the answer is read through the one rule the host proves")
    # THE JOB'S DIALOG WATCH (Group EL) declines every Revit dialog a job
    # meets. This question is the modeller's, so it is shown with the watch
    # stood down - or Heron would answer its own question with Cancel.
    check(re.search(r"using\s*\(\s*HeronDialogWatch\.HeronAsks\(\)\s*\)\s*\{\s*shown\s*=\s*dialog\.Show\(\);",
                    ask) is not None,
          "the question is shown inside HeronDialogWatch.HeronAsks(), so the job's "
          "dialog watch never answers it")
    check(before(ask, "RevitDispatcher.Asking(true);", "dialog.Show()")
          and re.search(r"finally\s*\{\s*RevitDispatcher\.Asking\(false\);", ask) is not None,
          "the dispatcher is told the modeller is asked, and told they answered whatever happens")
    shown_at = ask.find("dialog.Show()")
    ends = ask.find("DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate)")
    check(0 < shown_at < ends and ends < ask.rfind("return answer;"),
          "the isolation is ended INSIDE the group, before the answer goes back to be "
          "kept or rolled back (D-115)")

    showing = NOTE_SUITE.body_of(executor, "View Isolate(")
    check("view.IsTemporaryHideIsolateActive()" in showing,
          "a view the modeller has already isolated in is not isolated again")
    check("InItsOwnTransaction(" in showing,
          "isolating goes through a transaction of its own")
    own = NOTE_SUITE.body_of(executor, "bool InItsOwnTransaction(")
    check(before(own, "new Transaction(", "Discipline(") and before(own, "Discipline(", ".Commit("),
          "under Heron's failure rule before it commits")

    print("\n4. the verdict says who kept it")
    verdict = NOTE_SUITE.body_of(executor, "string WithVerdict(")
    check("HeronKeepQuestion.KeptSentence" in verdict,
          "a Keep says the modeller chose it")
    check("HeronKeepQuestion.NothingToAskSentence" in verdict,
          "and a run kept with no question says why none was put")
    check(re.search(r'Json\.Bool\(\s*"keptByModeller"', raw) is not None,
          "the reply carries keptByModeller")

    print("\n5. the dispatcher waits for the modeller, and the banner steps aside")
    dispatcher = code(DISPATCHER)
    raw_dispatcher = read(DISPATCHER) or ""
    check('GetInt("revit.askTimeoutSeconds"' in raw_dispatcher.replace(" ", ""),
          "it reads revit.askTimeoutSeconds")
    check(re.search(r"if\s*\(\s*!finished\s*&&\s*job\.Asked\s*\)", dispatcher) is not None
          and "job.WaitForAnswer(_askTimeout)" in dispatcher,
          "a job whose modeller is being asked waits for the answer, not the operation clock")
    check(re.search(r'Json\.Error\(\s*"awaiting_modeller"', raw_dispatcher) is not None,
          "and a question still open when the wait ends says so - never 'still running'")
    for error in ("modeller_discarded", "discard_unconfirmed"):
        check('case "%s"' % error in raw_dispatcher,
              "the banner has a line for %s" % error)
    execute = NOTE_SUITE.body_of(dispatcher, "public void Execute(UIApplication app)")
    check("_runningJob = job;" in execute and "_runningJob = null;" in execute,
          "the running job is set for Asking, and cleared when it ends")
    asking = NOTE_SUITE.body_of(dispatcher, "internal static void Asking(bool open)")
    check("job.Asking(open)" in asking and "_banner.StepAside(open)" in asking,
          "Asking reaches the job and the banner")
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
                       + ask_timeout(values) + CFG.operation_timeout(values))
            waits = CFG.fragment_timeout(values, keeping=True)
            check(waits > longest,
                  "operation=%s ask=%s -> the add-in answers within %g, a keeping run waits %g"
                  % (operation, ask_for, longest, waits))
    values = dict(CFG.DEFAULTS)
    check(CFG.fragment_timeout(values) == 180.0,
          "a read still waits exactly the 180 it always did")
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
    for error, outcome, step in (("modeller_discarded", FAIL.REFUSED, FAIL.STOP),
                                 ("discard_unconfirmed", FAIL.UNKNOWN, FAIL.LOOK_AT_THE_MODEL),
                                 ("awaiting_modeller", FAIL.RUNNING, FAIL.WAIT)):
        failure = FAIL.analyse({"ok": False, "error": error, "message": "m"}, writes=True)
        check(failure is not None and failure.outcome == outcome and failure.next_step == step,
              "%s is %s / %s" % (error, outcome, step))
        check(failure is not None and not failure.may_retry,
              "and %s is never sent again on its own" % error)
    discard = FAIL.analyse({"ok": False, "error": "modeller_discarded"}, writes=True)
    check(discard is not None and discard.touched_the_model is False,
          "a Discard Revit rolled back left nothing in the model")


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
