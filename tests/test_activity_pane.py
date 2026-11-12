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
    check("dispatcher.Dispatch(request)" in dispatch,
          "the pane reaches Revit through the add-in's one dispatcher (D-09) - the gate, the "
          "hold and the audit trail a chat's request goes through")
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


def rule():
    print("\n3. what the pane lists, built and run without Revit")
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
    for line in said.splitlines():
        if line.startswith("SHOW-JSON "):
            blob = line[len("SHOW-JSON "):]
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
        print("COULD NOT RUN - the wiring passed and the rule was not run. Exit 3, NOT a pass.")
        return COULD_NOT_RUN
    print("PASS    Heron's recent changes, with Select, Zoom and Undo, reached through the one dispatcher")
    return 0


if __name__ == "__main__":
    sys.exit(main())
