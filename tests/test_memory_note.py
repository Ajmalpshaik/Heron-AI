#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Every fragment job leaves one line in the add-in log saying what Heron holds
and how much memory Revit has committed - FRAGMENT-ISSUES row 5b-1110.

    python tests/test_memory_note.py

THE ROW. On 2026-10-18 a long proving run on Revit 2020 grew about 0.27 GB a
job until Revit held 28 GB, the last jobs failed for want of memory and the
chat crashed. What holds the memory was never found. The candidate is the
compiled scripts: RevitFragment keeps every one for the Revit session, and on
.NET Framework an assembly is never unloaded.

WHAT IS BUILT. Not a fix - the cause is not known. A measurement: when a
fragment job ends, however it returns, the add-in log says how many compiled
scripts Heron holds, how many this job added, and Revit's committed memory
before and after. The next long run's log then says whether memory grows with
the compiles or without them.

WHAT THIS READS. RevitFragment.cs, as TEXT - the note calls the process API
inside Revit, which cannot run here. That the numbers are right on a real
Revit is NEEDS-CHECKING's (the group named in the row).

MEASURED AGAINST THE CODE AS IT STOOD: every check fails, no traceback.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENT = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    try:
        return io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return ""


def section(text, start, end):
    """From `start` to the first `end` after it - "" when `start` is absent."""
    at = text.find(start)
    if at < 0:
        return ""
    stop = text.find(end, at + len(start))
    return text[at:stop] if stop > at else text[at:]


def main():
    raw = read(FRAGMENT)
    check(raw != "", "RevitFragment.cs is there to read")

    print("\n1. every fragment job ends with the note")
    run = section(raw, "public static string Run(UIApplication app, string request, bool writing",
                  "\n        }\n")
    named = run.find('var name = Json.ReadString(request, "name")')
    noted = run.find("using var memoryNote = new MemoryNote(name);")
    first_return = run.find("return ", named if named >= 0 else 0)
    check(named >= 0 and noted > named,
          "Run starts the note once it knows the job's name")
    check(noted >= 0 and (first_return < 0 or noted < first_return),
          "and before anything returns, so a refusal is noted as a kept job is - "
          "`using var` ends it however Run returns")

    print("\n2. what the note says")
    note = section(raw, "private sealed class MemoryNote : IDisposable", "\n        }\n\n")
    check("lock (Compiled) { return Compiled.Count; }" in note,
          "it counts the compiled scripts Heron holds, under the cache's own lock")
    check("PagedMemorySize64" in note and "GetCurrentProcess()" in note,
          "and reads Revit's committed memory - the figure the row measured, not the "
          "working set")
    check("_compiledBefore" in note and "_committedBefore" in note
          and "this job" in note,
          "both before and after the job, so the line says what this job added")
    check("HeronApplication.Log(" in note and "row 5b-1110" in note,
          "into the add-in log, naming the row")
    dispose = section(note, "public void Dispose()", "\n            }\n\n")
    check("catch (Exception)" in dispose and "HeronApplication.Log(" in dispose,
          "a note that cannot be written never costs the job its answer")
    committed = section(note, "private static long Committed()", "\n            }\n")
    check("return -1;" in committed and "committed >= 0 && _committedBefore >= 0" in note,
          "a memory figure it cannot read is left out of the line, never guessed")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    every fragment job notes what Heron holds and what Revit has committed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
