#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Heron's recent changes, docked in Revit, with Select, Zoom and Undo (D-163).

    python tests/test_activity_pane.py

TWO HALVES, AND ONE OF THEM CANNOT BE RUN HERE
-----------------------------------------------
  THE RULE      what the pane lists - the run records read into rows, the
                outcome in words, which row offers Undo, what each button
                sends - HeronActivity.cs, which names no vendor type and no
                window, linked by source into tests/Heron.Activity.TestHost.
                Built and RUN here; one request it writes is parsed by
                Python's reader.
  THE WIRING    the pane never calls the Revit API - every button goes to the
                ONE dispatcher (D-09) from a background thread; show_elements
                is Execute, fixed, with no transaction; the pane is registered
                in OnStartup inside its own try, opened from a row of the Heron
                menu, never a ribbon button (D-150); and each run record
                carries its outcome at the top. Read as TEXT.

Whether the pane draws, docks and stays responsive in a real Revit, and the
selection lands, is the PC's: NEEDS-CHECKING Group NW.

MEASURED AGAINST THE CODE AS IT STOOD: every wiring check fails, and the host
does not build - exit 1, not a traceback. Exit 3 when .NET is absent and the
wiring passed: NOT a pass.

THE PANE'S UNDO, ROW 5b-1741 (2026-11-16). The button sent `undo_last`, which
nothing had answered since the operation was renamed, so pressing it showed the
add-in's list of operations. The owner's answer, 2026-11-16: the pane's Undo is
the modeller's own click in Revit, like Ctrl+Z - Heron's last change in the
model on screen, whichever chat made it - so no chat is asked, and D-157's
rule still decides. Section 4 holds what was built: a pane-only operation,
`undo_from_pane`, answered only for a job the pane's own in-process door
queued - never one off the pipe, whatever it carries - and the status line in
Revit words. Seen to fail on the code as it stood. ONE ASSERTION THAT PINNED
THE OLD DOOR WAS CHANGED, not defended: DispatchFromPane was held to
`dispatcher.Dispatch(request)`, the pipe's own door; it is now held to the
dispatcher's pane door, which shares Dispatch's queue, event and gate.

SELECT AND ZOOM, ROW 5b-1825 (D-176, 2026-11-16). They were refused for a
chat's model-hold quiet minutes after its change, exactly when the modeller
looks at it. They now look past the hold - for the pane's own job and that
operation only. Section 3a holds the wiring; the rule is run by
tests/Heron.ModelHold.TestHost, section 10. Both seen to fail on the code as
it stood.
"""

import io
import json
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import test_failure_note as NOTE                                # noqa: E402

ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
HOST = os.path.join(ROOT, "tests", "Heron.Activity.TestHost")
COULD_NOT_RUN = 3
FAILURES = []

# The vendor's database namespace, built from parts: the structure gate reads
# every file's text, and a test may not spell it out.
VENDOR_DB = "Autodesk" + ".Revit" + ".DB"


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def code(name):
    return NOTE.code_only(NOTE.read(os.path.join(ADDIN, name)) or "")


def wiring():
    print("1. the pane never calls the Revit API")
    pane = code("HeronActivityPane.cs")
    check(bool(pane), "HeronActivityPane.cs is there")
    # `row.Document` is the record's model NAME, text - so the word alone is
    # not the test; a document, an element or the selection reached is.
    check(all(word not in pane for word in (VENDOR_DB, "UIDocument", "GetElement(", "Selection",
                                            "Transaction", "ExternalEvent")),
          "no model, selection, transaction or external event of its own - it is modeless")
    send = NOTE.body_of(pane, "void Send(")
    check("Task.Run(" in send and "_dispatch(request)" in send and "BeginInvoke(" in send,
          "each button hands its request to the dispatcher OFF Revit's thread, and the answer "
          "comes back on the pane's own")
    check("if (_busy" in send, "one request at a time")
    rule = code("HeronActivity.cs")
    check(bool(rule) and "Autodesk" not in rule and "System.Windows" not in rule,
          "what it lists is decided by HeronActivity.cs, which names no vendor type and no window")

    print("\n2. the one dispatcher, and the operation behind Select and Zoom")
    app = code("HeronApplication.cs")
    dispatch = NOTE.body_of(app, "string DispatchFromPane(")
    check("dispatcher.DispatchFromPane(request)" in dispatch and "dispatcher.Dispatch(request)" not in dispatch,
          "the pane reaches Revit through the add-in's one dispatcher (D-09) - by the "
          "dispatcher's pane door, the same queue, event, gate and audit trail a chat's "
          "request goes through")
    startup = NOTE.body_of(app, "public Result OnStartup(")
    check(re.search(r"try\s*\{\s*application\.RegisterDockablePane\(HeronActivityPane\.Id,",
                    startup) is not None,
          "registered in OnStartup, in a try of its own - its fault costs the pane, never the add-in")
    ribbon = NOTE.read(os.path.join(ADDIN, "HeronApplication.cs")) or ""
    check("typeof(ActivityPaneCommand).FullName" in ribbon
          and "bridgeGroup.AddPushButton(activityRow);" in ribbon,
          "opened from a row of the Heron menu - never a ribbon button of its own (D-150)")
    registry = NOTE.read(os.path.join(ROOT, "platform", "Heron.Core", "HeronOperationRegistry.cs")) or ""
    check(re.search(r'\{\s*"show_elements",\s*HeronRisk\.Execute\s*\}', registry) is not None,
          "show_elements is EXECUTE - the selection and the view, so it runs with Changes off")
    show = code("RevitShowElements.cs")
    check(bool(show) and "Transaction" not in show and ".Delete(" not in show,
          "and opens no transaction and deletes nothing")
    check("Selection.SetElementIds(found)" in show and "ShowElements(found)" in show,
          "it selects, and zooms when asked")
    check("!string.Equals(doc.Title, document" in show,
          "only in the model the change was made in")
    routes = NOTE.read(os.path.join(ADDIN, "RevitOperations.cs")) or ""
    check('case "show_elements":' in routes and "RevitShowElements.Show(app" in routes,
          "routed to RevitShowElements")
    record = NOTE.read(os.path.join(ADDIN, "RevitDispatcher.cs")) or ""
    check('Json.Str("kind", Json.ReadString(response, "verdictKind"))' in record,
          "each run record carries its outcome at the top, where the pane can read it")


def in_order(text, *needles):
    """Every needle is there, each after the one before it - never `find(a) < find(b)`,
    which reads a missing needle (-1) as first."""
    last = -1
    for needle in needles:
        found = text.find(needle, last + 1)
        if found < 0:
            return False
        last = found
    return True


def literal(name):
    """C# with its comments removed and its strings KEPT - for a check on a string
    literal, which code_only blanks, that a comment naming it must not satisfy."""
    text = NOTE.read(os.path.join(ADDIN, name)) or ""
    out, i, n = [], 0, len(text)
    while i < n:
        two = text[i:i + 2]
        if two == "//":
            while i < n and text[i] != "\n":
                i += 1
            continue
        if two == "/*":
            end = text.find("*/", i + 2)
            i = n if end < 0 else end + 2
            continue
        if text[i] == '"':
            end = i + 1
            while end < n and text[end] != '"':
                end += 2 if text[end] == "\\" else 1
            out.append(text[i:end + 1])
            i = end + 1
            continue
        out.append(text[i])
        i += 1
    return "".join(out)


def pane_undo():
    print("\n3. the pane's Undo: the modeller's own click, reachable only from the pane (row 5b-1741)")
    pane = code("HeronActivityPane.cs")
    rule_code = literal("HeronActivity.cs")
    asks = NOTE.body_of(rule_code, "static string UndoRequest(Row row)")
    check("() => HeronActivity.UndoRequest(row)" in pane,
          "the Undo button asks for the change on ITS row, not for whatever is on top")
    check('Json.Str("op", "undo_from_pane")' in asks
          and re.search(r'Json\.Str\("run", (row == null \? null : )?row\.Run\)', asks) is not None
          and re.search(r'Json\.Str\("document", (row == null \? null : )?row\.Document\)', asks)
          is not None
          and '"undo_last"' not in rule_code and '"undo_own"' not in rule_code,
          "it sends undo_from_pane with the row's run and model - never undo_last, which nothing "
          "answers, nor a chat's undo_own")
    check(bool(asks) and "conversation" not in asks and "HeronConversation" not in rule_code,
          "and names no chat: it is the modeller's own click (the owner's answer, 2026-11-16)")

    dispatcher = literal("RevitDispatcher.cs")
    pipe_door = NOTE.body_of(dispatcher, "public string Dispatch(string request)")
    pane_door = NOTE.body_of(dispatcher, "internal string DispatchFromPane(string request)")
    check(in_order(pane_door, "_paneDoor = true;", "try", "return Dispatch(request);", "finally",
                   "_paneDoor = false;")
          and re.search(r"\[ThreadStatic\]\s*private static bool _paneDoor;", dispatcher) is not None,
          "the dispatcher has two doors to ONE queue: the pane's DispatchFromPane IS Dispatch, "
          "with its own calling thread marked for that one call")
    check(in_order(pipe_door, "var fromPane = _paneDoor;", "_paneDoor = false;",
                   'var op = Json.ReadString(request, "op");',
                   "new RevitJob(request, HeronIdentity.NewWorkflowId(), fromPane)")
          and dispatcher.count("_paneDoor = true") == 1,
          "Dispatch takes the mark as it starts and clears it, and hands it to the job - a "
          "bridge listener thread never carries it, and only the pane's door sets it")
    job = NOTE.body_of(dispatcher, "private sealed class RevitJob")
    check("public bool FromPane { get; private set; }" in job
          and dispatcher.count("FromPane = ") == 1 and "FromPane = fromPane;" in job,
          "and it is the job's, set once by its constructor - never read from the request's text")
    execute = NOTE.body_of(dispatcher, "public void Execute(")
    from_pane = NOTE.body_of(dispatcher, "internal static bool RunningFromPane")
    check(in_order(execute, "_running = job;", "RevitOperations.Run(app, job.Request)")
          and re.sub(r"\s+", " ", from_pane).find("var job = _running; return job != null && job.FromPane;") >= 0,
          "the operations read the mark off the job Revit's thread is running - false when "
          "none is, so whatever asks fails closed")
    app = literal("HeronApplication.cs")
    check("Bridge.RequestHandler = Dispatcher.Dispatch;" in app
          and "RequestHandler = Dispatcher.DispatchFromPane" not in app
          and app.count("DispatchFromPane") == 3,
          "the pipe is given the pipe's door; the pane's door is handed to the pane alone")
    bridge = NOTE.read(os.path.join(ROOT, "revit", "Heron.Bridge", "BridgeServer.cs")) or ""
    check("DispatchFromPane" not in bridge and "FromPane" not in bridge,
          "and the bridge has no way to it")

    operations = literal("RevitOperations.cs")
    run = NOTE.body_of(operations, "public static string Run(UIApplication app, string request)")
    check(in_order(run, 'if (op == "undo_from_pane" && !RevitDispatcher.RunningFromPane) '
                        'return RevitUndo.NotFromPane();',
                   "var gate = Gate(op);"),
          "a request for it off the pipe is refused before anything - the gate included, so a "
          "chat is never told to turn Changes on for a request refused either way")
    check(in_order(run, "var gate = Gate(op);", 'if (op != "undo_from_pane")',
                   "HoldModel(app, request, out holdKey)", "RevitUndo.Waiting(app, request, op)",
                   "return Route(app, request, op);"),
          "from the pane it meets the gate - Changes on, the emergency stop - and the hold on "
          "every job while an Undo is out, but no chat's hold on the model: it is the modeller's "
          "own click, like Ctrl+Z")
    route = NOTE.body_of(operations, "private static string Route(")
    check(re.search(r'case "undo_from_pane":\s*return RevitDispatcher\.RunningFromPane\s*\?\s*'
                    r'RevitUndo\.FromPane\(app, request\)\s*:\s*RevitUndo\.NotFromPane\(\);', route)
          is not None,
          "and routed to RevitUndo.FromPane only for the pane's own job - asked again where it is routed")
    registry = NOTE.read(os.path.join(ROOT, "platform", "Heron.Core", "HeronOperationRegistry.cs")) or ""
    check(re.search(r'\{\s*"undo_from_pane",\s*HeronRisk\.Modify\s*\}', registry) is not None,
          "undo_from_pane is declared MODIFY: it needs Changes on, as D-163 said, and the stop blocks it")
    check('case "undo_from_pane":' in NOTE.body_of(dispatcher, "private static string DescribeJob(")
          and 'case "pane_only":' in NOTE.body_of(dispatcher, "private static string Explain("),
          "the banner says what it is in words, and a refusal off the pipe in words")
    refused = NOTE.body_of(literal("RevitUndo.cs"), "static string NotFromPane(")
    check(bool(refused) and "PostCommand" not in refused and "Guard." not in refused
          and 'Json.Error("pane_only"' in refused
          and "Nothing was sent to Revit and nothing was undone." in refused,
          "and that refusal posts nothing, asks the guard nothing, and says nothing was sent")
    mcp = ""
    for folder, _dirs, files in os.walk(os.path.join(ROOT, "mcp")):
        for name in files:
            if name.endswith(".py"):
                mcp += NOTE.read(os.path.join(folder, name)) or ""
    check(bool(mcp) and "undo_from_pane" not in mcp,
          "and no chat's Heron ever sends it - the MCP side names it nowhere")


def pane_looks():
    """
    D-176, row 5b-1825: the pane's Select and Zoom look past a chat's model hold.

    A chat holds a model for its quiet minutes after its last request there
    (D-141), and the pane's requests carry no conversation, so Select and Zoom
    pressed on a change a chat had just kept were refused "in use by another
    chat" - exactly when the modeller looks at it. The owner, 2026-11-16: "Yes,
    both." They change the selection and the view, never the model.

    The rule is RUN by tests/Heron.ModelHold.TestHost, section 10; this holds
    where Run asks it, and that it is asked with the job's own pane mark and
    never anything in the request's text.
    """
    print("\n3a. the pane's Select and Zoom look past a chat's hold - D-176, row 5b-1825")
    operations = literal("RevitOperations.cs")
    run = NOTE.body_of(operations, "public static string Run(UIApplication app, string request)")
    check(in_order(run, "var gate = Gate(op);", 'if (op != "undo_from_pane")',
                   "if (!HeronModelHold.LooksPastHolds(op, RevitDispatcher.RunningFromPane))",
                   "HoldModel(app, request, out holdKey)", "RevitUndo.Waiting(app, request, op)",
                   "return Route(app, request, op);"),
          "Run asks the hold's rule before the hold, with the operation and the job's own pane "
          "mark - after the gate, and before the wait for an Undo and the route")
    at = run.find("HeronModelHold.LooksPastHolds(")
    asked = run[at:run.find(")", at) + 2] if at >= 0 and run.find(")", at) >= 0 else ""
    check(bool(asked) and "request" not in asked and "Json." not in asked,
          "nothing in the request's text decides it: the operation by name, the door off the job")
    hold = literal("HeronModelHold.cs")
    rule_text = NOTE.body_of(hold, "public static bool LooksPastHolds(string operation, bool fromPane)")
    check(re.search(r"return\s+fromPane\s*&&\s*string\.Equals\(\s*operation\s*,\s*PaneShowOperation\s*,"
                    r"\s*StringComparison\.Ordinal\s*\)\s*;", rule_text) is not None
          and 'public const string PaneShowOperation = "show_elements";' in hold,
          "BOTH halves or nothing: the pane's own door AND show_elements, exactly")
    registry = NOTE.read(os.path.join(ROOT, "platform", "Heron.Core", "HeronOperationRegistry.cs")) or ""
    check(re.search(r'\{\s*"show_elements",\s*HeronRisk\.Execute\s*\}', registry) is not None,
          "and show_elements is EXECUTE - the selection and the view, never the model")
    rule_code = literal("HeronActivity.cs")
    show = NOTE.body_of(rule_code, "static string ShowRequest(Row row, bool zoom)")
    check('Json.Str("op", "show_elements")' in show and 'Json.Str("claim", "no")' in show,
          "the pane still sends Select and Zoom as a look that claims nothing (D-163 item 5 kept)")


def rule():
    print("\n4. what the pane lists, built and run without Revit")
    tfm = NOTE._tfm()
    if tfm is None:
        print("  NOT RUN - no .NET SDK and runtime here. Linux: apt-get install -y dotnet-sdk-10.0")
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
    ran = subprocess.run(["dotnet", os.path.join(HOST, out_dir, "Heron.Activity.TestHost.dll")],
                         stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    said = (ran.stdout or b"").decode("utf-8", "replace")
    blob = ""
    undo_blob = ""
    for line in said.splitlines():
        if line.startswith("SHOW-JSON "):
            blob = line[len("SHOW-JSON "):]
        elif line.startswith("UNDO-JSON "):
            undo_blob = line[len("UNDO-JSON "):]
        else:
            print("  " + line)
    check(ran.returncode == 0, "the host's own checks pass (exit %d)" % ran.returncode)
    try:
        show = json.loads(blob)
    except ValueError as bad:
        check(False, "the request it writes is JSON: %s" % bad)
        show = {}
    check(show == {"op": "show_elements", "ids": "101,102,103", "zoom": "true",
                   "document": "Tower MEP", "claim": "no"},
          "Zoom's request parses with every field - found %r" % show)
    try:
        undo = json.loads(undo_blob)
    except ValueError as bad:
        check(False, "the Undo request it writes is JSON: %s" % bad)
        undo = {}
    check(undo == {"op": "undo_from_pane", "run": "wf-20261112-140500-0002-bbbb",
                   "document": "Tower MEP", "fragment": "raise-ducts", "claim": "no"},
          "Undo's request parses with every field, and no chat in it - found %r" % undo)
    return 0


def main():
    wiring()
    pane_undo()
    pane_looks()
    host = rule()
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    if host == COULD_NOT_RUN:
        print("COULD NOT RUN - the wiring passed and the rule was not run. Exit 3, NOT a pass.")
        return COULD_NOT_RUN
    print("PASS    Heron's recent changes, with Select, Zoom and Undo, reached through the one dispatcher")
    return 0


if __name__ == "__main__":
    sys.exit(main())
