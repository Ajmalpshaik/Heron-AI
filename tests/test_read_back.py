#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A write says, element by element, which value went from what to what (D-156).

    python tests/test_read_back.py

WHY THIS EXISTS
---------------
The change report (5b-407) says WHICH elements Revit counted as changed, and
nothing about how. "12 ducts modified" reads the same for a 300 mm move and
for ducts Revit only regenerated because a joined wall moved. The read-back
reads each changed element's values before and after, and says which moved -
and says plainly when one Revit counted moved nothing Heron reads.

THREE HALVES, AND ONE OF THEM CANNOT BE RUN HERE
-------------------------------------------------
  THE RULE      which values differ, what is NOT READ rather than unchanged,
                what the reply carries and what it counts instead -
                HeronReadBack.cs, which names no Autodesk type and is linked
                by source into tests/Heron.ReadBack.TestHost with the bridge's
                Json.cs. Built and RUN here, and the JSON it writes is read
                back by Python's own reader, which did not write it.
  THE WIRING    when RevitFragment reads each side: a run that keeps reads the
                selection before anything runs; every write reads after,
                before the box, Assimilate or the rollback; a run nobody was
                asked about reads its "before" only once the rollback HELD;
                and a Discard carries no read-back. That half calls the Revit
                API, so it is read as TEXT, with comments and strings removed.
  THE CHAT      what revit_change prints from the field - heron_run_records'
                read_back_lines, run here on the host's own JSON.

None of it proves that a value Revit reads after a real change differs from
the one it read before, or that reading every parameter of fifty elements is
quick enough. That is the PC's, and NEEDS-CHECKING Group NS says what it owes.

MEASURED AGAINST THE CODE AS IT STOOD: with RevitFragment.cs from before the
change and no HeronReadBack.cs, every wiring check fails and the host does
not build - exit 1, not a traceback.

IT EXITS 3 WHEN .NET IS ABSENT AND EVERYTHING ELSE PASSED, and that is NOT a
pass: the rule was not run.
"""

import io
import json
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

# The comment-and-string stripper, the brace walker and the framework picker
# test_failure_note.py already proved - imported, not copied.
import test_failure_note as NOTE                                # noqa: E402

ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
EXECUTOR = os.path.join(ADDIN, "RevitFragment.cs")
RULE = os.path.join(ADDIN, "HeronReadBack.cs")
READER = os.path.join(ADDIN, "RevitReadBack.cs")
HOST = os.path.join(ROOT, "tests", "Heron.ReadBack.TestHost")
COULD_NOT_RUN = 3

FAILURES = []


def records():
    """heron_run_records, or None - so a module that is not there FAILS the checks
    that need it rather than ending every section with a traceback."""
    try:
        import heron_run_records as RUNS
        return RUNS
    except Exception as exc:                                    # noqa: BLE001
        check(False, "heron_run_records can be imported: %s: %s" % (type(exc).__name__, exc))
        return None


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def at(code, needle, start=0):
    """Where `needle` first appears at or after `start`, or -1."""
    return code.find(needle, start)


def wiring():
    raw = NOTE.read(EXECUTOR)
    check(raw is not None, "RevitFragment.cs is there to read")
    code = NOTE.code_only(raw or "")
    run = NOTE.body_of(code, "public static string Run(UIApplication app, string request, bool writing")
    check(bool(run), "Run(app, request, writing) is there")

    print("\n1. a run that keeps reads the selection BEFORE anything runs")
    selection = at(run, "readBefore = RevitReadBack.Read(target, RevitReadBack.SelectedIn(app, target)")
    again = at(run, "runAgain:")
    started = at(run, "group.Start();")
    check(0 <= selection < again < started,
          "the selection is read before the group starts and outside D-142's second run, "
          "so its values are the model as the modeller left it")
    check(re.search(r"if\s*\(\s*apply\s*\|\|\s*asking\s*\)\s*readBefore\s*=", run) is not None,
          "and only for a run that may keep - a dry run reads its 'before' after the rollback")

    print("\n2. every write reads AFTER at the moment the change report is described")
    described = at(run, "described = changed.Describe();")
    after = at(run, "readAfter = RevitReadBack.Read(target, changed.Changed")
    asked = at(run, "AskTheModeller(")
    kept = at(run, "if (apply) keptNote = KeepAndWatch(")
    check(0 < described < after < asked < kept,
          "after the report is sealed and before the modeller is asked, Assimilate or the "
          "rollback - the transaction that shows the change on screen is never read as it")
    check("RevitReadBack.Keys(readBefore)" in run[after:after + 200] if after >= 0 else False,
          "and a run that keeps reads only the elements whose 'before' it holds")

    print("\n3. a run nobody was asked about reads BEFORE once the rollback has held")
    rolled = re.search(r"else\s*\{\s*rolledBack\s*=\s*RollBackAndCheck\(group,\s*target,\s*watch\);"
                       r"(.*?)\}\s*\}", run, re.S)
    branch = rolled.group(1) if rolled else ""
    check(rolled is not None
          and re.search(r"if\s*\(\s*answered\s*==\s*null\s*&&\s*rolledBack\s*!=\s*null\s*&&\s*"
                        r"rolledBack\.Held\s*\)\s*readBefore\s*=\s*RevitReadBack\.Read\(", branch)
          is not None,
          "only after RollBackAndCheck, only when it HELD, and never after a Discard - a value "
          "read from a model still holding the change is not a 'before'")

    print("\n4. the field goes on a kept change or a dry run, never on a Discard")
    verdict = at(run, "var verdict = WithVerdict(")
    spliced = at(run, "verdict = WithReadBack(verdict, HeronReadBack.Compare(readMode")
    check(0 < verdict < spliced,
          "the read-back is spliced onto the add-in's verdict")
    check(re.search(r"string\s+readMode\s*=\s*apply\s*\?\s*HeronReadBack\.Selection\s*:\s*"
                    r"answered\s*==\s*null\s*\?\s*HeronReadBack\.DryRun\s*:\s*null;", run)
          is not None,
          "kept is 'selection', rolled back with nobody asked is 'dryRun', a Discard is nothing")

    print("\n5. the reader changes nothing, and the rule names nothing of Revit's")
    reader = NOTE.code_only(NOTE.read(READER) or "")
    check(bool(reader), "RevitReadBack.cs is there")
    check("Transaction" not in reader and ".Set(" not in reader and "Delete(" not in reader,
          "it opens no transaction and sets nothing - a read laid over the change")
    check(len(re.findall(r"catch\s*\(\s*Exception\s*\)", reader)) >= 6,
          "and every read is caught on its own, so a value that throws costs that value, "
          "never the change")
    rule = NOTE.read(RULE)
    check(rule is not None and "Autodesk" not in NOTE.code_only(rule),
          "HeronReadBack.cs names no Autodesk type, so a host can link it without Revit")
    most = re.search(r"const\s+int\s+MostRead\s*=\s*(\d+)\s*;", rule or "")
    RUNS = records()
    check(most is not None and RUNS is not None
          and int(most.group(1)) == getattr(RUNS, "READ_BACK_MOST_READ", None),
          "and the chat's sentence names the same limit the add-in reads to (%s)"
          % (most.group(1) if most else "not found"))


def chat(kept):
    """What revit_change prints from the field - heron_run_records.read_back_lines."""
    print("\n8. what the chat is shown")
    RUNS = records()
    if RUNS is None or not hasattr(RUNS, "read_back_lines"):
        check(False, "heron_run_records has read_back_lines")
        return
    lines = RUNS.read_back_lines({"readBack": kept.get("readBack")})
    said = "\n".join(lines)
    print("\n".join("    | " + line for line in lines))
    check(lines and lines[0].startswith("  before -> after: 2 of 4 changed element(s)"),
          "it opens with how many were read on both sides, of how many Revit counted")
    check('101 Ducts: Offset "2400 mm" -> "2700 mm"; Comments "" -> "moved"' in said,
          "each element on one line, old -> new, an empty value shown as \"\"")
    check("1 element(s) Revit counted as changed show no difference" in said,
          "the element Revit counted with no value moved is said, in those words")
    check("2 changed element(s) were not read on both sides - only the selection is read "
          "before a kept change; a dry run shows them all" in said,
          "and what was NOT READ is said, never called unchanged, with what would show it")
    check(RUNS.read_back_lines({}) == [] and RUNS.read_back_lines({"readBack": "x"}) == [],
          "a reply with no read-back - a read, a Discard, an older add-in - prints nothing")

    wide = {"readBack": {"mode": "dryRun", "changed": 30, "read": 30, "withDifference": 30,
                         "noValueChanged": 0, "notRead": 0, "elementsMore": 10,
                         "elements": [{"id": str(i), "values": [
                             {"name": "P%d" % j, "before": "a", "after": "b"} for j in range(6)],
                             "valuesMore": 2} for i in range(20)]}}
    lines = RUNS.read_back_lines(wide)
    check(len(lines) == 1 + RUNS.READ_BACK_ELEMENTS + 1,
          "a wide one is held to %d elements, and counts the rest on one line"
          % RUNS.READ_BACK_ELEMENTS)
    check(lines[1].endswith("; and 4 more") and lines[-1] == "    and 20 more element(s) "
          "with a value that changed",
          "with what each element and the whole list left out counted, never dropped")
    check(", from the dry run" in lines[0], "and a dry run says it is one")


def rule():
    print("\n6. the rule itself, built and run without Revit")
    tfm = NOTE._tfm()
    if tfm is None:
        print("  NOT RUN - no framework here that an installed SDK can build AND")
        print("  a runtime can run. Linux: apt-get install -y dotnet-sdk-10.0")
        return COULD_NOT_RUN, {}

    out_dir = "bin/x64/Debug-%s/" % tfm
    built = subprocess.run(
        ["dotnet", "build", HOST, "-p:RevitVersion=2024", "-p:HeronTfm=%s" % tfm,
         "-p:OutputPath=%s" % out_dir, "--nologo", "-v", "q"],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built.returncode != 0:
        check(False, "the host builds for %s" % tfm)
        for line in (built.stdout or b"").decode("utf-8", "replace").splitlines()[-20:]:
            print("    %s" % line)
        return 1, {}

    dll = os.path.join(HOST, out_dir, "Heron.ReadBack.TestHost.dll")
    ran = subprocess.run(["dotnet", dll], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    said = (ran.stdout or b"").decode("utf-8", "replace")
    blobs = {}
    for line in said.splitlines():
        for prefix in ("KEPT-JSON ", "CAPPED-JSON ", "ODD-JSON "):
            if line.startswith(prefix):
                blobs[prefix.strip()] = line[len(prefix):]
                break
        else:
            print("  " + line)
    check(ran.returncode == 0, "the host's own checks pass (exit %d)" % ran.returncode)

    print("\n7. what the hand-written writer wrote, read by a reader that did not write it")
    parsed = {}
    for key in ("KEPT-JSON", "CAPPED-JSON", "ODD-JSON"):
        try:
            parsed[key] = json.loads(blobs.get(key, ""))
        except ValueError as bad:
            check(False, "%s is JSON: %s" % (key, bad))
            parsed[key] = {}
    kept = (parsed.get("KEPT-JSON") or {}).get("readBack") or {}
    check(kept.get("mode") == "selection" and kept.get("changed") == 4
          and kept.get("read") == 2 and kept.get("notRead") == 2
          and kept.get("noValueChanged") == 1,
          "a kept change's field parses, with every count the chat reads")
    first = (kept.get("elements") or [{}])[0]
    check(first.get("id") == "101" and first.get("values") ==
          [{"name": "Offset", "before": "2400 mm", "after": "2700 mm"},
           {"name": "Comments", "before": "", "after": "moved"}],
          "and its element's values, old and new, exactly as read")
    capped = (parsed.get("CAPPED-JSON") or {}).get("readBack") or {}
    check(len(capped.get("elements") or []) == 20 and capped.get("elementsMore") == 6
          and len(((capped.get("elements") or [{}])[0]).get("values") or []) == 12
          and ((capped.get("elements") or [{}])[0]).get("valuesMore") == 3,
          "a wide one carries 20 elements of 12 values, and counts the rest")
    odd = ((parsed.get("ODD-JSON") or {}).get("readBack") or {}).get("elements") or [{}]
    check(odd[0].get("id") == '9"9' and odd[0].get("category") == 'Cat "q"'
          and (odd[0].get("values") or [{}])[0] == {"name": "Comments", "before": "a\\b",
                                                    "after": 'line\nnext "quoted"'},
          "an id, a category and values holding quotes, a backslash and a new line come "
          "back exactly as given")
    return 0, parsed.get("KEPT-JSON") or {}


def main():
    wiring()
    host, kept = rule()
    if kept:
        chat(kept)

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
    print("PASS    a write says which value went from what to what, and what it did not read")
    return 0


if __name__ == "__main__":
    sys.exit(main())
