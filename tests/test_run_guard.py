#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A running Heron job cannot save, sync, export or print a model it was not
allowed to - Constitution Article 10, held in code.

    python tests/test_run_guard.py

WHY THIS EXISTS
---------------
Until this change the add-in listened to four Revit events - ViewActivated,
DocumentClosing, Idling, DocumentClosed - and none of them was a save, a sync
or an export. A MODIFY fragment calling doc.Save() or an export met nothing.
The run guard listens for those while a job runs, cancels what the policy says
to, refuses the job, rolls its group back, closes what it left open in the
background, and asks the switches again before anything is kept.

TWO HALVES, BECAUSE ONE OF THEM CANNOT BE RUN HERE
---------------------------------------------------
  THE POLICY    HeronRunPolicy.cs, which names no Autodesk type, linked by
                source into tests/Heron.RunGuard.TestHost and RUN here: every
                risk level against every act and every origin, the record,
                the sentences, the switches asked again, and the splice.
  THE WIRING    HeronRunGuard.cs, the dispatcher, the fragment executor and
                the application, read as TEXT: every event subscribed and
                unsubscribed, no Cancel() without asking Cancellable first,
                armed and disarmed around the job in a finally, the record on
                the reply and the audit line, the risk passed in and the
                switches asked again before Assimilate, and the job's own
                code handed to the guard so a family EditFamily opens is not
                taken for a new file (5b-1091). A weak test of behaviour and
                a strong one of a missing line.

Neither proves Revit raises the events, lets them be cancelled, or closes
what it is told to. That is the PC's: docs/needs-checking/group-ex.md.

IT EXITS 3 WHEN .NET IS ABSENT AND THE WIRING PASSED, which is NOT a pass:
the policy was not run.
"""

import io
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
GUARD = os.path.join(ADDIN, "HeronRunGuard.cs")
POLICY = os.path.join(ADDIN, "HeronRunPolicy.cs")
DISPATCHER = os.path.join(ADDIN, "RevitDispatcher.cs")
EXECUTOR = os.path.join(ADDIN, "RevitFragment.cs")
OPERATIONS = os.path.join(ADDIN, "RevitOperations.cs")
APPLICATION = os.path.join(ADDIN, "HeronApplication.cs")
FAILURES_PY = os.path.join(ROOT, "mcp", "server", "heron_failure.py")
DOTNET_PY = os.path.join(ROOT, "brain", "heron_dotnet.py")
HOST = os.path.join(ROOT, "tests", "Heron.RunGuard.TestHost")
COULD_NOT_RUN = 3

# The pre-events the guard must hear, and the ones it decides on. ViewExporting
# is raised by the DB Application, not the ControlledApplication OnStartup
# gets, so it is checked separately below.
PRE_EVENTS = ("DocumentSaving", "DocumentSavingAs", "DocumentSynchronizingWithCentral",
              "FileExporting", "DocumentPrinting")
POST_EVENTS = ("DocumentCreated", "DocumentOpened", "DocumentChanged")

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


def strip(text):
    """C# without its comments, and with every string's contents emptied.

    A check that a call is present must not be passed by a comment naming it,
    and this repository's comments name every call they explain. Walked one
    character at a time, so a // inside a string never ends a line early.
    """
    out, i, n = [], 0, len(text)
    while i < n:
        pair = text[i:i + 2]
        if pair == "//":
            while i < n and text[i] != "\n":
                i += 1
            continue
        if pair == "/*":
            end = text.find("*/", i + 2)
            i = n if end < 0 else end + 2
            continue
        if pair == '@"':
            i += 2
            while i < n and not (text[i] == '"' and text[i + 1:i + 2] != '"'):
                i += 2 if text[i] == '"' else 1
            out.append('""')
            i += 1
            continue
        if text[i] == '"':
            i += 1
            while i < n and text[i] != '"':
                i += 2 if text[i] == "\\" else 1
            out.append('""')
            i += 1
            continue
        if text[i] == "'":
            end = i + 1
            while end < n and text[end] != "'":
                end += 2 if text[end] == "\\" else 1
            out.append("' '")
            i = end + 1
            continue
        out.append(text[i])
        i += 1
    return "".join(out)


def block(code, header):
    """The braces-balanced body after the first `header`, or '' when absent."""
    at = code.find(header)
    if at < 0:
        return ""
    start = code.find("{", at)
    depth = 0
    for i in range(start, len(code)):
        if code[i] == "{":
            depth += 1
        elif code[i] == "}":
            depth -= 1
            if depth == 0:
                return code[start:i + 1]
    return ""


def guard_file():
    print("\n1. the guard hears every event it decides on, and lets go of each")
    code = strip(read(GUARD) or "")
    check(bool(code), "HeronRunGuard.cs is there")

    attach = block(code, "static void Attach(")
    detach = block(code, "static void Detach(")
    for event in PRE_EVENTS + POST_EVENTS:
        on = re.search(r"\.%s\s*\+=" % event, attach) is not None
        off = re.search(r"\.%s\s*-=" % event, detach) is not None
        check(on and off, "%s is subscribed in Attach and unsubscribed in Detach" % event)

    check(re.search(r"\.ViewExporting\s*\+=", code) is not None
          and re.search(r"\.ViewExporting\s*-=", detach) is not None,
          "ViewExporting is subscribed - on the DB Application - and unsubscribed in Detach")

    print("\n2. nothing is cancelled without asking Revit whether it can be")
    cancels = [m.start() for m in re.finditer(r"\.Cancel\s*\(\s*\)", code)]
    check(len(cancels) >= 1, "the guard cancels something at all - %d call(s)" % len(cancels))
    for at in cancels:
        before = code[max(0, at - 160):at]
        check(re.search(r"if\s*\(\s*\w+\.Cancellable\s*\)\s*\{?[^;{}]*$", before) is not None,
              "the Cancel() near code line %d sits inside `if (e.Cancellable)` - "
              "calling it blindly throws" % (code.count("\n", 0, at) + 1))

    judge = block(code, "static void Judge(")
    check(judge.find("Cancels(") >= 0 and judge.find("Cancels(") < judge.find(".Cancel("),
          "the policy is asked before anything is cancelled")
    check("Stopped(" in judge, "and whatever it decided is recorded")

    changed = block(code, "static void OnChanged(")
    check("TransactionRolledBack" in changed and "TransactionGroupRolledBack" in changed,
          "a rollback is not counted as a change - Heron's own group rolling back included")
    check("IsElsewhere(" in changed and "IsTarget(" in changed,
          "and a change is only 'elsewhere' when it is not the model the job ran in")

    leftovers = block(code, "static void CloseLeftovers(")
    check("Close(false)" in leftovers.replace(" ", ""),
          "what the job left open is closed WITHOUT saving")
    check("OpenAtStart" in leftovers and "IsLinked" in leftovers
          and "ShownInAWindow(" in leftovers,
          "and only a document that was not open at the start, is not a link, "
          "and no window shows")
    shown = block(code, "static bool ShownInAWindow(")
    check("GetOpenUIViews()" in shown, "a window is counted with GetOpenUIViews")

    disarm = block(code, "static HeronRunPolicy Disarm(")
    check("CloseLeftovers(" in disarm and "finally" in disarm,
          "disarming closes the leftovers and always forgets the job")

    # 5b-1091: Revit 2024.3 raised DocumentCreated for doc.EditFamily, and the
    # guard refused it as a new document below Admin - two PROVEN fragments
    # came back run_guard. The policy tells the two apart; these say the guard
    # asks it, before anything is refused, and keeps the answer for the job.
    created = block(code, "static void OnCreated(")
    asks = created.find("FromEditFamily(")
    check("IsFamilyDocument" in created and asks >= 0,
          "a new document is asked whether it is a family, and the policy whether the "
          "job's own code explains it by EditFamily - 5b-1091")
    check(asks >= 0 and created.find(".Created(") > asks,
          "and that is asked BEFORE it is recorded as a refused new document")
    check("EditedByJob.Add(" in created, "a family EditFamily opened is kept apart for the job")
    check("EditedByJob" in block(code, "static HeronRunPolicy.Origin OriginOf("),
          "and OriginOf answers EditedByJob for it, so its saves are judged by that rule")
    check("EditedByJob.Clear()" in block(code, "static void Forget("),
          "and it is forgotten with the job")
    check(".Reads(" in block(code, "static void Code("),
          "the guard hands the job's code to the policy, which decides")

    # HERON'S OWN EXPORT IS THE ONE LET THROUGH, and only where it is wrapped:
    # capture_view's ExportImage, into Heron's own folder. Any other export in
    # the add-in's own code would be cancelled under a read - so every one
    # must sit inside the scope, or the guard breaks Heron's own tool.
    walked = 0
    for name in sorted(os.listdir(ADDIN)):
        if not name.endswith(".cs"):
            continue
        text = strip(read(os.path.join(ADDIN, name)) or "")
        for found in re.finditer(r"\.\s*(ExportImage|Export)\s*\(", text):
            walked += 1
            before = text[max(0, found.start() - 400):found.start()]
            check("HeronRunGuard.HeronExports()" in before,
                  "%s exports at code line %d inside HeronRunGuard.HeronExports()"
                  % (name, text.count("\n", 0, found.start()) + 1))
    check(walked >= 1, "the add-in's own exports were found - %d" % walked)
    check("_heronExports > 0" in judge,
          "and the scope reaches the policy's question")

    policy = read(POLICY)
    check(policy is not None and "Autodesk" not in strip(policy),
          "HeronRunPolicy.cs names no Autodesk type, so a host can link it without Revit")


def dispatcher():
    print("\n3. armed around every job, disarmed in a finally, and reported")
    code = strip(read(DISPATCHER) or "")
    execute = block(code, "public void Execute(")
    arm = execute.find("HeronRunGuard.Arm(")
    run = execute.find("RevitOperations.Run(")
    check(0 <= arm < run, "the guard is armed before the job runs")
    tail = execute[run:]
    final = re.search(r"finally\s*\{[^{}]*HeronRunGuard\.Disarm\(", tail)
    check(final is not None,
          "and disarmed in the finally that follows it - never left armed over the "
          "modeller's own Ctrl+S")
    check(re.search(r"=\s*\w+\.Splice\(\s*response\s*\)", execute) is not None,
          "the record is spliced onto the reply")

    raw = "\n".join(line for line in (read(DISPATCHER) or "").splitlines()
                    if not line.lstrip().startswith("//"))
    listed = re.search(r"var\s+(\w+)\s*=\s*new\s+List<KeyValuePair<string,\s*long>>", raw)
    name = listed.group(1) if listed else "numbers"
    for number in ("blocked", "closed"):
        check(re.search(r'\b%s\.Add\(\s*new\s+KeyValuePair<string,\s*long>\(\s*"%s"'
                        % (name, number), raw) is not None,
              "the audit line carries `%s`, in the list it is written from" % number)
    check(re.search(r"HeronAudit\.Record\([^;]*,\s*%s\s*\)\s*;" % name, raw) is not None,
          "and that list is the one HeronAudit.Record is handed")


def executor():
    print("\n4. the risk is passed in, and asked again before anything is kept")
    operations = strip(read(OPERATIONS) or "")
    for op in ("run_fragment_read", "run_fragment_show", "run_fragment_write",
               "run_fragment_publish", "run_fragment_admin"):
        raw = read(OPERATIONS) or ""
        at = raw.find('case "%s":' % op)
        statement = raw[at:raw.find(";", at)] if at >= 0 else ""
        check("HeronOperationRegistry.RiskOf(op)" in statement,
              "%s hands the executor the registry's risk for the operation" % op)
    check(re.search(r"RevitFragment\.Run\(\s*app\s*,\s*request\s*\)", operations) is None,
          "no route reaches the executor without a risk")

    code = strip(read(EXECUTOR) or "")
    check("HeronRunGuard.Target(" in code,
          "the executor names the model it chose, so its own changes are never 'elsewhere'")

    run = block(code, "public static string Run(")
    handed = run.find("HeronRunGuard.Code(source)")
    check(handed >= 0, "the executor hands the guard the fragment's own code - 5b-1091")
    check(0 <= handed < run.find("RunSetupSteps(") and handed < run.find("RunScript(")
          and handed < run.find("Compile("),
          "before any of it is compiled or run, so a family EditFamily opens is known "
          "for what it is when Revit reports it")
    check(re.search(r'ReadObjectArray\(request, "setup"\)[\s\S]{0,400}HeronRunGuard\.Code\(',
                    read(EXECUTOR) or "") is not None,
          "and every setup step's code as well - a starter in a step counts")

    keep = code.find("KeepAndWatch(group")
    check(keep > 0, "the write path still keeps its group, through KeepAndWatch")
    before = code[max(0, keep - 2500):keep]
    check("HeronRunGuard.Policy" in before,
          "the guard's record is read before the group is kept")
    recheck = before.rfind("WhyNotKept(")
    check(recheck >= 0 and before.rfind("AskTheModeller(") < recheck,
          "the switches are asked again right before the group is kept - AFTER the "
          "modeller's Keep or Discard, which can come minutes later")
    check("RollBackAndCheck(" in before[recheck:] if recheck >= 0 else False,
          "and a refusal there rolls the group back through the one rollback helper")
    asked = block(code, "static string WhyNotKept(")
    check("HeronRunPolicy.Withdrawn(" in asked and "HeronConfig.Load()" in asked
          and "HeronStop.IsStopped" in asked,
          "and that reads heron.config FRESH and asks the stop again - Article 13, "
          "never a cached permission")
    check(code.count("group.Assimilate()") == 1 and code.count("KeepAndWatch(group") == 1,
          "and there is one place a group is kept, so nothing skips the check")

    raw = read(EXECUTOR) or ""
    for code_name in ("run_guard", "changed_other_model", "permission_withdrawn"):
        check(re.search(r'Json\.Error\(\s*"%s"' % code_name, raw) is not None,
              "the executor can refuse with `%s`" % code_name)


def classified():
    print("\n5. the new refusals are classified for the chat, and the host is built")
    text = read(FAILURES_PY) or ""
    for code_name, outcome in (("run_guard", "ROLLED_BACK"),
                               ("permission_withdrawn", "ROLLED_BACK"),
                               ("changed_other_model", "UNKNOWN")):
        row = re.search(r'"%s":\s*\((\w+),\s*(\w+)' % code_name, text)
        check(row is not None and row.group(1) == outcome,
              "`%s` is %s in heron_failure.py%s"
              % (code_name, outcome, "" if row is None else " - found %s" % row.group(1)))
    row = re.search(r'"changed_other_model":\s*\(\w+,\s*(\w+)', text)
    check(row is not None and row.group(1) == "LOOK_AT_THE_MODEL",
          "and a change to another model sends a person to look at it")

    app = strip(read(APPLICATION) or "")
    check("HeronRunGuard.Attach(" in block(app, "public Result OnStartup("),
          "the guard is attached once, in OnStartup")
    check("HeronRunGuard.Detach(" in block(app, "public Result OnShutdown("),
          "and detached in OnShutdown")

    check("tests/Heron.RunGuard.TestHost/Heron.RunGuard.TestHost.csproj" in (read(DOTNET_PY) or ""),
          "the host is in heron_dotnet's PROJECTS, so check-compile builds it on every release")


def _asked(what):
    try:
        out = subprocess.check_output(["dotnet", what], stderr=subprocess.STDOUT)
    except Exception:                                      # noqa: BLE001
        return None
    return out.decode("utf-8", "replace")


def _majors(said, prefix=None):
    found = set()
    for line in (said or "").splitlines():
        parts = line.split()
        if prefix:
            if not line.startswith(prefix):
                continue
            parts = parts[1:]
        if parts:
            try:
                found.add(int(parts[0].split(".")[0]))
            except ValueError:
                pass
    return sorted(found)


def _tfm():
    """The newest framework an installed SDK can build and a runtime can run."""
    runtimes = _majors(_asked("--list-runtimes"), "Microsoft.NETCore.App")
    sdks = _majors(_asked("--list-sdks"))
    if not runtimes or not sdks:
        return None
    usable = [m for m in runtimes if m <= max(sdks)]
    return "net%d.0" % max(usable) if usable else None


def policy():
    print("\n6. the policy itself, built and run without Revit")
    tfm = _tfm()
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

    dll = os.path.join(HOST, out_dir, "Heron.RunGuard.TestHost.dll")
    ran = subprocess.run(["dotnet", dll], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    for line in (ran.stdout or b"").decode("utf-8", "replace").splitlines():
        print("  " + line)
    check(ran.returncode == 0, "the host's own checks pass (exit %d)" % ran.returncode)
    return 0


def main():
    guard_file()
    dispatcher()
    executor()
    classified()
    host = policy()

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    if host == COULD_NOT_RUN:
        print("COULD NOT RUN - the wiring passed and the policy was not run. Exit 3,")
        print("which is NOT a pass.")
        return COULD_NOT_RUN
    print("PASS    a running job cannot publish what its operation does not allow")
    return 0


if __name__ == "__main__":
    sys.exit(main())
