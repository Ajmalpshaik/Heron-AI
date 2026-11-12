#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A keep that changed nothing leaves nothing in Revit's undo list (D-160).

    python tests/test_empty_keep.py

WHY THIS EXISTS
---------------
A change asked to KEEP that Revit counted no element added, changed or
deleted in was assimilated anyway - an empty "Heron: <name>" entry in the
modeller's undo list. Row 117 named the hazard: told to press Ctrl+Z for a
change that did nothing, a modeller undoes whatever they did BEFORE it. The
add-in now rolls such a keep back and says so.

TWO HALVES
----------
  THE WIRING    RevitFragment.Run turns the keep into a rollback only when
                Revit's report was WITNESSED, counted ZERO (setup steps
                included) and the job is not a publish - before Assimilate;
                the verdict has its own kind and sentence. Read as TEXT, with
                comments and strings removed, as test_change_report does.
  THE SERVER    the new kind is ROLLED BACK in the activity list and in the
                run record's words - run here on the real modules.

Whether Revit raises the events where a real empty run needs them is the
PC's, NEEDS-CHECKING Group NW.

MEASURED AGAINST THE CODE AS IT STOOD: every check below fails, no traceback.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import test_failure_note as NOTE                                # noqa: E402

EXECUTOR = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def wiring():
    raw = NOTE.read(EXECUTOR) or ""
    code = NOTE.code_only(raw)
    run = NOTE.body_of(code, "public static string Run(UIApplication app, string request, bool writing")
    print("1. a keep Revit counted nothing in is rolled back, not kept")
    rule = re.search(r"if\s*\(\s*apply\s*&&\s*!sendsOut\s*&&\s*described\s*!=\s*null\s*&&\s*"
                     r"described\.Witnessed\s*&&\s*described\.Total\s*==\s*0\s*\)\s*\{\s*"
                     r"apply\s*=\s*false;\s*keptNothing\s*=\s*true;", run)
    check(rule is not None,
          "only when asked to keep, not a publish, Revit's report WITNESSED and its total - setup "
          "steps included - ZERO")
    at = rule.start() if rule else -1
    keep = run.find("if (apply) keptNote = KeepAndWatch(")
    described = run.find("described = changed.Describe();")
    check(0 <= described < at < keep,
          "decided after the report is read and before Assimilate, so nothing empty is kept")
    check("answered == null && !keptNothing && rolledBack != null && rolledBack.Held" in run
          and "answered == null && !keptNothing ? HeronReadBack.DryRun" in run,
          "and it is not passed off as a dry run's read-back")
    check(re.search(r"WithVerdict\([^;]*keptNothing,\s*described\)", run) is not None,
          "the verdict is told")
    verdict = NOTE.body_of(code, "string WithVerdict(")
    check(re.search(r"else\s+if\s*\(\s*held\s*&&\s*keptNothing\s*\)\s*\{\s*verdict\s*=\s*"
                    r"NothingChangedSentence\(name\);", verdict) is not None,
          "and has its own sentence when the rollback held - never the plain rollback's "
          "'send it again with apply'")
    sentence = raw[raw.find("string NothingChangedSentence("):raw.find("string WithVerdict(")]
    sentence = re.sub(r'"\s*\+\s*"', "", sentence)    # the string's pieces, joined
    check("NO new entry in Revit's undo list" in sentence
          and "Ctrl+Z would undo whatever was done before this" in sentence,
          "which says there is no undo entry, and what Ctrl+Z would do instead")
    check('kind = "unchanged";' in raw, "under its own kind, 'unchanged'")


def server():
    print("\n2. the server reads 'unchanged' as rolled back")
    try:
        import heron_server_core as CORE
        import heron_run_records as RUNS
    except Exception as why:                         # noqa: BLE001 - one clean failure
        check(False, "the server modules import: %s" % why)
        return
    reply = {"ok": True, "verdictKind": "unchanged", "applied": False, "rolledBack": True}
    check(CORE._change_outcome(reply) == CORE.ROLLED_BACK,
          "the activity list says ROLLED BACK - found %r" % CORE._change_outcome(reply))
    check(CORE._change_outcome(dict(reply, rolledBack=False)) == CORE.UNKNOWN,
          "and not when the rollback did not hold")
    check(RUNS._ended(reply) == "nothing changed, nothing kept - no undo entry",
          "the run record says nothing changed and no undo entry - found %r" % RUNS._ended(reply))


def main():
    wiring()
    server()
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a keep that changed nothing leaves nothing in the undo list")
    return 0


if __name__ == "__main__":
    sys.exit(main())
