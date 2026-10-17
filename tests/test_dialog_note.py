#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A Revit dialog that opens inside a Heron job is answered, never waited on.

    python tests/test_dialog_note.py

WHY THIS EXISTS
---------------
FRAGMENT-ISSUES section 5b row 300, measured 2026-10-03 on Revit 2024:
LOAD_FAMILY given a family saved in a later release opened Revit's own modal
"saved in a later version" dialog INSIDE the job. The chat read
unknown_outcome, then revit_busy, then still_running for over 90 seconds, and
nothing moved until somebody closed the dialog by hand.

The add-in now listens to Revit's DialogBoxShowing while - and only while - a
job runs, declines the dialog (Cancel, or Close) unless a row with evidence
says otherwise, stops the job so its group rolls back, and puts what Revit
asked on the reply.

TWO HALVES, BECAUSE ONE OF THEM CANNOT BE RUN HERE
---------------------------------------------------
  THE RULE      which answer a dialog gets, what the modeller is told and what
                the reply carries - HeronDialogNote.cs, which names no Autodesk
                type and is linked by source into
                tests/Heron.DialogNote.TestHost with the bridge's Json.cs.
                Built and RUN here, so its checks are real behaviour.
  THE WIRING    that Revit's event is subscribed, that only a job arms it, that
                a dialog already open before a job still gets revit_busy, that
                every write path checks before it keeps anything, and that no
                Heron prompt on the job path is answered by Heron. That half
                calls the Revit API and cannot run off a Revit, so it is read
                as TEXT - with comments and string contents removed, so a
                sentence that names a call cannot satisfy a check about it.

Neither half proves Revit takes the answer. That is the PC's, and
NEEDS-CHECKING group EJ says what it owes.

MEASURED AGAINST THE CODE AS IT STOOD: with revit/ as it was before this
change, every wiring section fails and the host does not build - exit 1.

IT EXITS 3 WHEN .NET IS ABSENT AND THE WIRING PASSED, and that is NOT a pass:
the rule was not run.
"""

import io
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.append(os.path.join(ROOT, "tests"))
sys.path.append(os.path.join(ROOT, "mcp", "server"))

# THE HELPERS test_failure_note.py already proves - comment stripping, brace
# matching, the source walk, the framework probe - taken from it rather than
# copied, so a fix to one is a fix to both.
import test_failure_note as FN                                   # noqa: E402

ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
APPLICATION = os.path.join(ADDIN, "HeronApplication.cs")
DISPATCHER = os.path.join(ADDIN, "RevitDispatcher.cs")
EXECUTOR = os.path.join(ADDIN, "RevitFragment.cs")
WATCH = os.path.join(ADDIN, "HeronDialogWatch.cs")
NOTE = os.path.join(ADDIN, "HeronDialogNote.cs")
HOST = os.path.join(ROOT, "tests", "Heron.DialogNote.TestHost")
COULD_NOT_RUN = 3

FAILURES = []

# A prompt Heron puts in front of the modeller. On the job path every one of
# these must be shown inside HeronDialogWatch.HeronAsks().
PROMPT = re.compile(r"new\s+TaskDialog\s*\(|TaskDialog\s*\.\s*Show\s*\(|"
                    r"MessageBox\s*\.\s*Show\s*\(|\.\s*ShowDialog\s*\(")


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def code(path):
    raw = FN.read(path)
    check(raw is not None, "%s is there to read" % os.path.basename(path))
    return FN.code_only(raw or "")


def unanswered_prompts(text):
    """How many Heron prompts in this C# are NOT covered by HeronAsks.

    A file that never stands the watch down cannot show a prompt safely on the
    job path, so every prompt in it counts; a file that does is trusted to
    have put each one inside the scope - the reviewer reads which.
    """
    stripped = FN.code_only(text)
    shown = len(PROMPT.findall(stripped))
    return 0 if "HeronDialogWatch.HeronAsks()" in stripped else shown


def subscribed():
    print("\n1. Revit's dialog event is subscribed, and acts only while a job runs")
    app = code(APPLICATION)
    startup = FN.body_of(app, "Result OnStartup(")
    shutdown = FN.body_of(app, "Result OnShutdown(")
    check("HeronDialogWatch.Attach(application" in startup,
          "OnStartup attaches the watch, on Revit's own thread")
    check("HeronDialogWatch.Detach(application" in shutdown,
          "and OnShutdown detaches it")

    watch = code(WATCH)
    check(re.search(r"DialogBoxShowing\s*\+=\s*OnDialogBoxShowing",
                    FN.body_of(watch, "void Attach(")) is not None,
          "Attach subscribes to DialogBoxShowing")
    check(re.search(r"DialogBoxShowing\s*-=\s*OnDialogBoxShowing",
                    FN.body_of(watch, "void Detach(")) is not None,
          "and Detach unsubscribes")

    handler = FN.body_of(watch, "void OnDialogBoxShowing(")
    guard = re.search(r"if\s*\(\s*met\s*==\s*null\s*\|\|\s*_heronAsking\s*>\s*0[^)]*\)\s*return\s*;",
                      handler)
    first_answer = handler.find("OverrideResult(")
    check(guard is not None and 0 < guard.start() < first_answer,
          "the handler returns before answering anything when no job is armed, "
          "or while Heron is asking a question of its own")
    check(re.search(r"var\s+met\s*=\s*_armed\s*;", handler) is not None,
          "and 'armed' means the note the dispatcher put there for this job")
    check("HeronDialogRule.For(" in handler,
          "the answer comes from the table")

    # EVERY OverrideResult IN revit/ IS THIS ONE, AND IT IS HANDED A CODE FROM
    # THE TABLE - never a number or an enum member typed at the call.
    calls = []
    for path in FN.sources(os.path.join(ROOT, "revit")):
        for found in re.finditer(r"OverrideResult\s*\(([^)]*)\)", FN.code_only(FN.read(path) or "")):
            calls.append((os.path.basename(path), found.group(1).strip()))
    check(len(calls) == 1 and calls[0] == ("HeronDialogWatch.cs", "code"),
          "OverrideResult is called in one place, with a code the table chose: %s" % calls)
    check(re.search(r"foreach\s*\(\s*var\s+code\s+in\s+row\.Codes\s*\)", handler) is not None,
          "and that code is one of the row's, tried in order")


def dispatcher():
    print("\n2. the dispatcher arms it around Run alone, and reports what it met")
    raw = FN.read(DISPATCHER) or ""
    text = FN.code_only(raw)
    execute = FN.body_of(text, "public void Execute(")
    started = execute.find("TryStart()")
    armed = execute.find("HeronDialogWatch.Arm()")
    run = execute.find("RevitOperations.Run(")
    disarmed = execute.find("HeronDialogWatch.Disarm()")
    check(0 < started < armed < run < disarmed,
          "Execute arms after Revit takes the job and before it runs, and disarms after")
    finally_at = execute.rfind("finally", 0, disarmed)
    check(run < finally_at < disarmed,
          "and disarms in a finally, so a job that throws cannot leave it armed")

    dispatch = FN.body_of(text, "public string Dispatch(")
    check(bool(dispatch) and "HeronDialogWatch" not in dispatch,
          "Dispatch - the listener thread - never touches the watch")
    check('Json.Error("revit_busy",' in raw,
          "so a dialog open BEFORE the job still gets revit_busy: Revit runs no "
          "ExternalEvent behind it, and nothing is armed")

    # The reply is completed BEFORE the banner and the audit line read it, or
    # a success turned into a refusal would be announced as Done.
    raw_execute = FN.body_of("\n".join(line for line in raw.splitlines()
                                       if not line.lstrip().startswith("//")),
                             "public void Execute(")
    attach = raw_execute.find("HeronDialogNote.Attach(response, dialogs,")
    failure = raw_execute.find('Json.ReadString(response, "error")')
    check(raw_execute.find("HeronDialogWatch.Disarm()") < attach < failure,
          "the dialogs go on the reply after disarming and before anything reads its error")
    check("HeronOperationRegistry.Writes(op)" in raw_execute[attach:attach + 160],
          "and the reply learns whether the job could write from the tool registry")

    listed = re.search(r"var\s+(\w+)\s*=\s*new\s+List<KeyValuePair<string,\s*long>>", raw_execute)
    check(listed is not None
          and re.search(r'if\s*\(\s*dialogs\.Count\s*>\s*0\s*\)\s*%s\.Add\(\s*new\s+KeyValuePair'
                        r'<string,\s*long>\(\s*"dialogs"\s*,\s*dialogs\.Count\s*\)'
                        % listed.group(1), raw_execute) is not None,
          "the audit line's numbers carry `dialogs` - only when one was met")
    check(re.search(r'case\s+"dialog_refused"\s*:', raw) is not None
          and re.search(r'case\s+"dialog_unconfirmed"\s*:', raw) is not None,
          "and the banner has words for both refusals")


def write_paths():
    print("\n3. every write path checks before it keeps anything")
    keepers = 0
    for path in FN.sources(os.path.join(ROOT, "revit")):
        text = FN.code_only(FN.read(path) or "")
        for found in re.finditer(r"\.\s*Assimilate\s*\(\s*\)", text):
            keepers += 1
            before = text[max(0, found.start() - 900):found.start()]
            asked = before.rfind("HeronDialogWatch.Refused")
            line = text.count("\n", 0, found.start()) + 1
            check(asked >= 0 and "SafeRollBack(group)" in before[asked:],
                  "%s keeps a group at code line %d only after asking whether a "
                  "dialog stopped the job, and rolls back when one did"
                  % (os.path.basename(path), line))
    check(keepers >= 2,
          "the walk reached the write paths - %d Assimilate call(s)" % keepers)

    executor = FN.code_only(FN.read(EXECUTOR) or "")
    check(len(re.findall(r"return\s+DialogRefusal\(\s*name\s*,\s*unwound\s*\)\s*\?\?\s*threw\s*;",
                         executor)) == 2,
          "a fragment that threw after a declined dialog is reported as the dialog, on both write branches")
    reading = executor.find("if (!writing)")
    script = executor.find("RunScript(", reading)
    refused = executor.find("DialogRefusal(name, null)", reading)
    check(0 < reading < script < refused < executor.find("TransactionGroup(", reading),
          "and so is a READ a declined dialog interrupted")
    refusal = FN.body_of(executor, "string DialogRefusal(")
    check(re.search(r"rolledBack\s*==\s*null\s*\?\s*HeronDialogNote\.Outcome\.ReadOnly", refusal) is not None
          and re.search(r"rolledBack\.Value\s*\?\s*HeronDialogNote\.Outcome\.RolledBack", refusal) is not None,
          "'as it was' is said only when Revit confirmed the rollback, 'nothing changed' only on a read")


def own_prompts():
    print("\n4. Heron never answers its own question")
    watch = code(WATCH)
    check("IDisposable HeronAsks()" in watch,
          "there is a way to stand the watch down for a Heron prompt")

    # THE JOB PATH: every file an operation runs in. Commands.cs and the
    # windows are the ribbon's, which no job can reach - a ribbon command and
    # a job are both Revit's thread, so one never runs inside the other.
    on_path = sorted(name for name in os.listdir(ADDIN)
                     if name.startswith("Revit") and name.endswith(".cs"))
    check("RevitOperations.cs" in on_path and "RevitFragment.cs" in on_path,
          "the job path is read: %d file(s)" % len(on_path))
    for name in on_path:
        left = unanswered_prompts(FN.read(os.path.join(ADDIN, name)) or "")
        check(left == 0,
              "%s shows no prompt outside HeronAsks (%d found)" % (name, left))

    # AND THE CHECK HAS TEETH: it fires on the shape it is about.
    planted = ('var ask = new TaskDialog("Keep these changes?");\n'
               'var said = ask.Show();\n')
    check(unanswered_prompts(planted) == 1,
          "a Keep/Discard prompt shown bare on the job path is found")
    check(unanswered_prompts("using (HeronDialogWatch.HeronAsks()) { " + planted + " }") == 0,
          "and the same prompt inside HeronAsks is not")
    check(unanswered_prompts('// new TaskDialog("a comment")\nvar s = "TaskDialog.Show(";\n') == 0,
          "a comment or a string naming a prompt is not one")


def outside():
    print("\n5. the rule is Revit-free, and the chat knows both refusals")
    note = FN.read(NOTE)
    check(note is not None and "Autodesk" not in FN.code_only(note),
          "HeronDialogNote.cs names no Autodesk type, so a host can link it without Revit")

    import heron_failure as HF                                    # noqa: E402
    refused = HF.analyse({"ok": False, "error": "dialog_refused", "message": "m"}, writes=True)
    check(refused is not None and refused.outcome == HF.ROLLED_BACK
          and refused.touched_the_model is False and not refused.may_retry,
          "dialog_refused: rolled back, the model untouched, never retried by itself")
    unsure = HF.analyse({"ok": False, "error": "dialog_unconfirmed", "message": "m"}, writes=True)
    check(unsure is not None and unsure.outcome == HF.UNKNOWN
          and unsure.next_step == HF.LOOK_AT_THE_MODEL and not unsure.may_retry,
          "dialog_unconfirmed: a person looks at the model")


def rule():
    """The half that runs: build the host and let it check the rule."""
    print("\n6. the rule itself, built and run without Revit")
    tfm = FN._tfm()
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

    dll = os.path.join(HOST, out_dir, "Heron.DialogNote.TestHost.dll")
    ran = subprocess.run(["dotnet", dll], stdout=subprocess.PIPE,
                         stderr=subprocess.STDOUT)
    said = (ran.stdout or b"").decode("utf-8", "replace")
    for line in said.splitlines():
        print("  " + line)
    check(ran.returncode == 0, "the host's own checks pass (exit %d)" % ran.returncode)
    return 0


def main():
    subscribed()
    dispatcher()
    write_paths()
    own_prompts()
    outside()
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
    print("PASS    a Revit dialog met inside a job is declined, the job stops, and the")
    print("        reply says what Revit asked")
    return 0


if __name__ == "__main__":
    # Redirected output on Windows is cp1252; Revit's words are not.
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass
    sys.exit(main())
