#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Ask to Delete - D-197: one switch in Settings, on at every Revit start, that
stops Revit's Delete box (D-142) and a DRAFT's Keep or Discard (D-119).

    python tests/test_ask_to_delete.py

WHY THIS EXISTS
---------------
The owner, 2026-11-23, after a Keep or Discard box for an unproven tool broke
his concentration while Ask to Keep was off: one more switch, so Revit asks
nothing about deletes and the chat asks in words first. He chose one switch for
both boxes, on at every Revit start.

TWO HALVES
----------
  THE WIRING  read as TEXT: the switch is held in memory and starts ON, no file
              key or request field can set it, the executor reads it once per
              request, the delete gate and the DRAFT's box both skip on it, the
              replies say nobody was asked, Settings has the row, and the toggle
              asks only on the way off. A weak test of behaviour and a strong
              one of a missing or misplaced line.
  THE PYTHON  RUN here: the client reads `askToDelete` into four answers, the
              health lines say each, and the try-once client records a box-less
              run as NOT asked rather than calling the add-in old.

None of it proves Revit then does what it is told. NEEDS-CHECKING Group PR says
what the PC owes.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
import heron_tools as SERVER_TOOLS                          # noqa: E402
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_bridge_client as CLIENT                               # noqa: E402
import heron_health as HEALTH                                      # noqa: E402

ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
PERMS = os.path.join(ROOT, "platform", "Heron.Core", "HeronPermissions.cs")
CONFIG = os.path.join(ROOT, "platform", "Heron.Core", "HeronConfig.cs")
EXECUTOR = os.path.join(ADDIN, "RevitFragment.cs")
GATE = os.path.join(ADDIN, "RevitFragmentDelete.cs")
COMMANDS = os.path.join(ADDIN, "Commands.cs")
DISPATCHER = os.path.join(ADDIN, "RevitDispatcher.cs")
BRIDGE = os.path.join(ROOT, "revit", "Heron.Bridge", "BridgeServer.cs")
CLIENT_PY = os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    with io.open(path, "r", encoding="utf-8") as fh:
        return fh.read()


def section(title):
    print("")
    print(title)


def main():
    perms = read(PERMS)
    config = read(CONFIG)
    executor = read(EXECUTOR)
    gate = read(GATE)
    commands = read(COMMANDS)
    dispatcher = read(DISPATCHER)
    bridge = read(BRIDGE)
    client = read(CLIENT_PY)

    section("1. The switch is held in memory, ON when Revit starts, and set by nothing on the wire")
    check(re.search(r"private static bool _asksInRevit = true;", perms) is not None,
          "HeronPermissions holds _asksInRevit, and it starts true (on at every Revit start)")
    getter = re.search(r"public static bool AsksInRevit\(\)\s*\{(.*?)\}", perms, re.S)
    check(getter is not None and "HeronConfig" not in getter.group(1),
          "AsksInRevit reads the field, never heron.config")
    check("askToDelete" not in config and "asksInRevit" not in config.lower(),
          "HeronConfig declares no key for it, so no file can turn the box off")
    sets = re.findall(r"SetAsksInRevit\(", read(COMMANDS) + bridge + executor + gate)
    check(len(sets) == 1, "SetAsksInRevit is called once, from the Settings toggle (found %d)" % len(sets))
    check("SetAsksInRevit" not in bridge, "the bridge never sets it")
    check(re.search(r'Json\.Bool\("askToDelete", HeronPermissions\.AsksInRevit\(\)\)', bridge) is not None,
          "`info` says askToDelete, from the one reader")
    check('ReadString(request, "askToDelete")' not in executor + gate + bridge
          and 'ReadBool(request, "askToDelete")' not in executor + gate + bridge,
          "no request field is read for it")

    section("2. The executor reads it ONCE per request, above the run-again label")
    read_at = executor.find("var asksInRevit = HeronPermissions.AsksInRevit();")
    again_at = executor.find("runAgain:")
    check(read_at >= 0, "Run reads the switch into asksInRevit")
    check(0 <= read_at < again_at, "and reads it before runAgain, so D-142's second run is decided alike")
    check(executor.count("HeronPermissions.AsksInRevit()") == 1,
          "and nowhere else in the executor")

    section("3. Off, the Delete gate keeps the run as it ran - no box, no second run")
    check(re.search(r"bool keeping, bool asksInRevit,", gate) is not None,
          "DeleteGate takes the switch")
    off = gate.find("if (!asksInRevit)")
    ask = gate.find("AskToDelete(app, target, name, request, going")
    keeping = gate.find("if (!keeping) return null;")
    check(0 <= keeping < off < ask, "it is read after `not keeping` and before the box is asked")
    block = gate[off:gate.find("return null;", off) + len("return null;")]
    check("again" not in block.replace("unasked", ""), "off sets no second run")
    check("unasked = changed.DeletedIds.Count;" in block, "off still counts what Revit deleted")
    check("Delete box: off (Ask to Delete)" in block, "and logs it")
    check(re.search(r'Json\.Bool\("deleteAsked", false\)', gate) is not None
          and '"deletedUnasked"' in gate and '"deleteAsking"' in gate,
          "the reply says deleteAsked false, how many, and the sentence the chat prints first")
    check("WithDeleteUnasked(verdict, deletedUnasked)" in executor,
          "Run puts those fields on a kept reply")

    section("4. Off, a DRAFT's one run is kept with no box, and the reply says nobody was asked")
    skip = executor.find("if (asking && !asksInRevit)")
    box = executor.find("else if (asking || askToKeep)")
    check(0 <= skip < box, "the skip sits right before the Keep or Discard box")
    check("keptUnasked = true;" in executor[skip:box] and "apply = true;" in executor[skip:box],
          "it keeps the run and marks it unasked")
    check(re.search(r'Json\.Bool\("asked", false\)\s*\+ "," \+ Json\.Str\("askSkipped", AskSkippedBySwitch\)',
                    executor) is not None,
          "WithUnasked says asked false and askSkipped")
    check('AskSkippedBySwitch = "askToDeleteOff"' in executor, "askSkipped's value is askToDeleteOff")
    check("RevitFragment.AskSkippedBySwitch" in dispatcher and "Not asked in Revit - Ask to Delete off" in dispatcher,
          "the banner says it was kept without asking")

    section("5. Settings has the row, and the toggle asks on the way OFF only")
    check('Name = "Ask to Delete",' in commands, "a Settings row named Ask to Delete")
    check("Toggle = () => Run(new AskToDeleteToggleCommand())," in commands, "running its own command")
    cmd = commands[commands.find("public sealed class AskToDeleteToggleCommand"):]
    cmd = cmd[:cmd.find("private static HeronSwitchQuestion Question()")]
    check("if (asks)" in cmd and "HeronSwitchWindow.Ask(" in cmd,
          "the question is asked only while it is on")
    check("SetAsksInRevit(!asks)" in cmd, "and flips the switch after")
    check("ask.DefaultButton = TaskDialogResult.CommandLink2;" in
          commands[commands.find("public sealed class AskToDeleteToggleCommand"):
                   commands.find("public sealed class SettingsCommand")],
          "the plain fallback's Enter lands on Keep asking")

    section("6. The Python reads four answers and says each")

    class Fake(object):
        def __init__(self, reply):
            self.reply = reply

        def request(self, op, **_):
            return self.reply

    check(CLIENT.ask_to_delete_state(Fake({"ok": True, "askToDelete": True})) is True, "true is ON")
    check(CLIENT.ask_to_delete_state(Fake({"ok": True, "askToDelete": False})) is False, "false is OFF")
    check(CLIENT.ask_to_delete_state(Fake({"ok": True})) == CLIENT.ASK_TO_KEEP_NOT_SENT,
          "no field is an older add-in")
    check(CLIENT.ask_to_delete_state(Fake({"ok": True, "askToDelete": 0})) is None,
          "a number is no answer")
    check(CLIENT.ask_to_delete_state(Fake(None)) is None, "no reply is no answer")
    check(HEALTH.ask_to_delete_line(True) == HEALTH.ASK_TO_DELETE_ON, "the ON line")
    check(HEALTH.ask_to_delete_line(False) == HEALTH.ASK_TO_DELETE_OFF
          and "Article 7" in HEALTH.ASK_TO_DELETE_OFF and "BEFORE" in HEALTH.ASK_TO_DELETE_OFF,
          "the OFF line tells the chat to ask in words before any delete")
    check(HEALTH.ask_to_delete_line(HEALTH.ASK_TO_KEEP_NOT_SENT) == HEALTH.ASK_TO_DELETE_OLDER,
          "the older add-in's line")
    check(HEALTH.ask_to_delete_line(1) == HEALTH.ASK_TO_DELETE_NOT_READ, "1 is not True")
    check('"askToDelete"' not in client.replace('reply.get("askToDelete", _FIELD_MISSING)', ""),
          "the client names the field only where a reply is read")

    server = SERVER_TOOLS.server_text()
    diagnose = read(os.path.join(ROOT, "mcp", "server", "heron_diagnose.py"))
    check("if bridge.ask_to_delete_state(b) is False:" in server,
          "revit_health prints the Ask to Delete line only when it is OFF")
    check("if deletes[b.pid] is False" in diagnose,
          "and heron_diagnose shows the same line in the same case")

    section("7. try-once records a box-less run as NOT asked, never as an old add-in")
    unasked = client.find('unasked = reply.get("asked") is not True and reply.get("askSkipped") == "askToDeleteOff"')
    older = client.find('if reply.get("asked") is not True and not unasked:')
    check(0 <= unasked < older, "the switch is recognised before the older-add-in refusal")
    check('"asked": not unasked,' in client, "the record says asked false")
    check("NOBODY LOOKED" in client and "NOBODY WAS ASKED" in client,
          "the record and the printout both say nobody looked")

    print("")
    if FAILURES:
        print("%d FAILED" % len(FAILURES))
        return 1
    print("all passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
