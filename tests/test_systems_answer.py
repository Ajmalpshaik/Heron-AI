#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   4
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
What `revit_systems` says about a duct or pipe system. Runs without Revit.

THE DEFECT, measured 2026-09-22 on Project1, Revit 2024, session 17492:

    duct  Mechanical Supply Air 1        0 element(s)   [Supply Air]
    3 MEP element(s) are on NO SYSTEM.

while `revit_parameters` read `System Name = Mechanical Supply Air 1` on both
of that model's ducts. `RevitSystems.cs` read what a system holds from
`MEPSystem.Elements` alone, which Autodesk's own API notes describe - the same
words in every release from 2020 to 2027 - as *"Terminal elements in the
system"*. The ducts, pipes and fittings are in `DuctNetwork` / `PipingNetwork`
and the equipment is `BaseEquipment`, so every one of them read as on no
system, on every model.

THE ADD-IN NOW SENDS TWO MORE NUMBERS PER SYSTEM, `network` and `components`,
and a key the add-in sends that the formatter never reads prints NOWHERE -
the seam every compiler and every gate is blind to. So this checks both sides
of it:

  1. the answer prints a system's run and its components, and they add up;
  2. the on-no-system sentence says which systems it read, and has stopped
     calling itself "the wider group" - nothing ever counted that;
  3. the connected-to-nothing sentence no longer says those elements are on
     no system - that was reasoned, never counted, and a terminal can be put
     on a system before anything is joined to it;
  4. a reply from an add-in OLDER than the fix is said to be one, rather than
     printing its terminals-only zero as if it were the answer;
  5. an empty `onNoSystem` - D-30's negative case for this agent - is said
     in words rather than left as a silence;
  6. every per-system number the C# writes is one the formatter reads.

AND A SECOND DEFECT FROM THE SAME RUN: `withAnOpenConnector` read 0 on that
model while the proven find-dead-ends fragment walked the same two ducts and
found both open ends. `Connector.AllRefs` holds "both physical connection and
logical connection", and `IsJoined` counted the duct's reference to its own
system as a partner. The report-connectors fragment had already learned that
rule. So:

  7. `IsJoined` drops a reference owned by a duct or pipe system - a text
     check, and the most a suite without Revit can say about it.

IT READS THE FUNCTION RATHER THAN IMPORTING THE MODULE, for the reason
tests/test_parameter_clash.py records: `import heron_mcp_server` needs the
MCP SDK, which gates.yml leaves out on purpose, so an importing suite exits 3
and never runs where it matters.

WHAT IT CANNOT DO: say what Revit puts in `DuctNetwork`, or whether the
System Browser agrees. That needs a model - D-30, and Group N of
docs/NEEDS-CHECKING.md.

    python tests/test_systems_answer.py
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")
ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitSystems.cs")

WHERE = "Project1 (Revit 2024, session 17492)"


def read(path):
    """A file's text with its line endings normalised, or None.

    Text mode with no `newline=` turns CRLF into `\\n`, so the two-newline
    boundary below means the same on Ajmal's PC as in CI.
    """
    try:
        return io.open(path, encoding="utf-8").read()
    except OSError:
        return None


def lifted():
    """`_systems_answer` out of the server's source, or None if it is gone.

    TWO BLANK LINES END A TOP-LEVEL FUNCTION in that file. A helper renamed
    or moved returns None, which becomes ONE clean failure below rather than
    a traceback standing in for every check (heron-ship section 2a).
    """
    text = read(SERVER)
    if text is None:
        return None
    nl = chr(10)
    start = text.find(nl + "def _systems_answer(")
    if start < 0:
        return None
    end = text.find(nl + nl + nl, start)
    if end < 0:
        return None
    namespace = {}
    try:
        exec(text[start + 1:end], namespace)              # noqa: S102
    except BaseException:                                 # noqa: BLE001
        return None
    return namespace.get("_systems_answer")


def emitted_row_keys():
    """Every key the C# writes into one system's row, or None.

    Read from `Systems()`'s own body, so a number added there and not taught
    to the formatter is caught here and not by a modeller who never sees it.
    """
    text = read(ADDIN)
    if text is None:
        return None
    start = text.find("private static List<string> Systems(")
    if start < 0:
        return None
    end = text.find("private static", start + 1)
    body = text[start:end if end > 0 else len(text)]
    return sorted(set(re.findall(r'Json\.(?:Num|Str)\("(\w+)"', body)))


FAILURES = []


def check(condition, what):
    # ASCII ON THE WAY OUT. The answers carry an em dash, and a console that
    # cannot encode one raises in print() - one traceback in place of every
    # check still to run.
    shown = what.encode("ascii", "replace").decode("ascii")
    if condition:
        print("  ok    %s" % shown)
    else:
        print("  FAIL  %s" % shown)
        FAILURES.append(shown)


def system(name, network, components, kind="duct", system_type="Supply Air"):
    """One system row shaped the way RevitSystems.Systems writes it now."""
    return {"id": "uid-" + name, "name": name, "kind": kind,
            "systemType": system_type,
            "elements": network + components,
            "network": network, "components": components}


def reply(systems, examined, on_no_system, loose=()):
    return {"ok": True, "document": "Project1",
            "systems": list(systems),
            "connectedToNothing": list(loose),
            "systemCount": len(systems),
            "mepElementsExamined": examined,
            "withAnOpenConnector": 0,
            "connectedToNothingCount": len(loose),
            "reportedNoConnectors": 0,
            "onNoSystem": on_no_system}


def line_for(answer, name):
    for one in answer.splitlines():
        if name in one:
            return one
    return ""


def finish():
    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1
    print("PASSED - a system's run is counted and printed, and no sentence "
          "claims what nothing counted.")
    return 0


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    answer_of = lifted()
    check(answer_of is not None,
          "the server has a helper that words a list_systems reply")
    if answer_of is None:
        return finish()

    print()
    print("Project1 as it stands: one supply run, two ducts and an elbow")
    fixed = answer_of(reply([system("Mechanical Supply Air 1", 3, 0)], 3, 0),
                      WHERE)
    row = line_for(fixed, "Mechanical Supply Air 1")
    check("3 element(s)" in row,
          "the system counts the three elements of its run: %r" % row.strip())
    check("3 in the run" in row and "0 terminal" in row,
          "and says how many are the run and how many are terminals or "
          "equipment, so the System Browser's count can be held against it")
    check("NO SYSTEM" not in fixed and "on no duct or pipe system" not in fixed,
          "with every element on a system, nothing is reported off one")
    check("Every one of the 3" in fixed,
          "and that empty answer is SAID - D-30's negative case for this agent "
          "is a sentence, not a silence")

    print()
    print("the run and the components add up, whatever the mix")
    mixed = answer_of(reply([system("Hydronic Supply 2", 41, 6, kind="pipe",
                                    system_type="Hydronic Supply")], 50, 3),
                      WHERE)
    row = line_for(mixed, "Hydronic Supply 2")
    check("47 element(s)" in row and "41 in the run" in row
          and "6 terminal" in row,
          "41 in the run and 6 terminals or equipment print as 47: %r"
          % row.strip())

    print()
    print("the on-no-system sentence")
    sentence = line_for(mixed, "3 MEP element(s)")
    check(sentence != "", "three elements off every system are reported")
    check("duct or pipe system" in sentence,
          "it names the systems it read - electrical circuits are not among "
          "them: %r" % sentence[:90])
    check("electrical" in sentence.lower(),
          "and says anything electrical lands in it, rather than leaving a "
          "light fitting on a circuit to read as a fault")
    check("wider group" not in mixed,
          "it no longer calls itself the wider group - nothing ever counted "
          "that connected-to-nothing sits inside it")

    print()
    print("the connected-to-nothing sentence")
    lone = {"id": "uid-lone", "name": "Supply Diffuser", "category": "Air Terminals",
            "connectors": 1, "level": "Level 1"}
    loose = answer_of(reply([system("Mechanical Supply Air 2", 0, 1)], 4, 0,
                            loose=[lone]), WHERE)
    said = line_for(loose, "CONNECTED TO NOTHING")
    check(said != "", "a loose element is still listed")
    check("on no system" not in said.lower(),
          "and is no longer SAID to be on no system - here it is the one "
          "terminal Mechanical Supply Air 2 holds: %r" % said[:90])
    check("Supply Diffuser" in loose, "with its name")

    print()
    print("an add-in older than the fix")
    old_row = {"id": "uid-old", "name": "Mechanical Supply Air 1",
               "kind": "duct", "systemType": "Supply Air", "elements": 0}
    old = answer_of(reply([old_row], 3, 3), WHERE)
    check("OLDER" in old,
          "a reply with no `network` is named as an old add-in, so its "
          "terminals-only zero is not read as the answer")
    check("0 element(s)" in line_for(old, "Mechanical Supply Air 1"),
          "and what it did send is still printed, not hidden")

    print()
    print("the seam between the C# and the formatter")
    keys = emitted_row_keys()
    check(keys is not None, "RevitSystems.cs still has a Systems() that writes rows")
    keys = keys or []
    for key in ("network", "components"):
        check(key in keys, "the C# writes `%s` into every system's row" % key)
    server = read(SERVER) or ""
    start = server.find(chr(10) + "def _systems_answer(")
    body = server[start:server.find(chr(10) * 3, start)] if start >= 0 else ""
    for key in keys:
        if key == "id":
            continue                  # for a machine to act on, never printed
        check('"%s"' % key in body,
              "and the formatter reads `%s`, so it prints somewhere" % key)

    print()
    print("a duct or pipe system is not a join")
    addin = read(ADDIN) or ""
    start = addin.find("private static bool IsJoined(")
    joined = ""
    if start >= 0:
        end = addin.find("private static", start + 1)
        joined = addin[start:end if end > 0 else len(addin)]
    check(joined != "", "RevitSystems.cs still decides joins in IsJoined()")
    check("is MechanicalSystem" in joined and "is PipingSystem" in joined,
          "and skips a reference owned by a duct or pipe system - counted, it "
          "made every open end on a system read as joined")

    return finish()


if __name__ == "__main__":
    sys.exit(main())
