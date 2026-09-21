#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
How the installer reads PowerShell back, which is the one thing about it
that hangs rather than fails.

    python tests/test_installer_adapters.py

WHY THIS FILE EXISTS
--------------------
platform/Heron.Installer/WindowsAdapters.cs shells out to PowerShell twice:
once to ask which Revit is installed and open, once per product to run
tools/deploy-addin.ps1. Until 2026-09-21 it read the two streams one after
the other:

    text.Append(process.StandardOutput.ReadToEnd());
    text.Append(process.StandardError.ReadToEnd());
    if (!process.WaitForExit(timeoutSeconds * 1000)) { ... }

A pipe holds a few kilobytes. A child that fills the one nobody is reading
blocks there, so it never closes the other, so the read never returns - and
the ceiling on the next line is never reached, because the thread is stuck
above it. MEASURED on this machine, same pattern, same runtime: 8 KB on
standard error came back in 0.0s, 200 KB never came back at all and a
10-second ceiling never fired. Row 5b-78 in docs/FRAGMENT-ISSUES.md.

The caller that reaches it is the deploy: 578 lines, $ErrorActionPreference
= "Stop", and a file Revit still has open puts a PowerShell error record on
standard error while Write-Host is filling standard output. That is an
installer window hung with no way out, on the one run where something went
wrong.

WHAT THIS CAN AND CANNOT DO
---------------------------
It CANNOT run the thing. PowerShellRunner names powershell.exe and this is
Linux, so nothing below executes a line of it - the same honest limit
tests/test_deploy_script.py works under. A harness that reproduced the
PATTERN against /bin/sh would be a test of the harness.

So it reads the source and holds the rule: both streams are taken at once,
they are kept apart, and the one that gets parsed is standard output alone.
Whether it then deploys is owed on Windows - AA7 to AA9 in
docs/NEEDS-CHECKING.md.
"""

from __future__ import annotations

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ADAPTERS = os.path.join(ROOT, "platform", "Heron.Installer", "WindowsAdapters.cs")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def at(text, needle):
    """Where `needle` is, or -1. Never compared without being found first."""
    return text.find(needle)


def main():
    try:
        with io.open(ADAPTERS, encoding="utf-8") as fh:
            text = fh.read()
    except IOError as e:
        print("FAILED - could not read %s (%s)" % (ADAPTERS, e))
        return 1

    print("\nThe two streams are read at once, not one after the other")
    check("ReadToEnd" not in text,
          "nothing calls ReadToEnd - that is the call that deadlocks when "
          "the other pipe fills")
    check("BeginOutputReadLine" in text and "BeginErrorReadLine" in text,
          "both streams are started before the wait, so neither can fill "
          "while the other is being drained")

    begin_out = at(text, "process.BeginOutputReadLine()")
    begin_err = at(text, "process.BeginErrorReadLine()")
    timed = at(text, "process.WaitForExit(Math.Max(1, timeoutSeconds)")
    check(begin_out > 0 and begin_err > 0 and timed > 0
          and begin_out < timed and begin_err < timed,
          "and both are started BEFORE the ceiling, which is what makes the "
          "ceiling reachable at all")

    # WaitForExit(int) returns when the process is gone, which can be before
    # the reader has handed over the last of what it read. The parameterless
    # one waits for the streams too. Without it the tail goes missing at
    # random, and a JSON answer with its tail missing does not parse.
    plain = at(text, "process.WaitForExit();")
    check(plain > timed > 0,
          "the parameterless WaitForExit follows it, so the readers are "
          "finished before the answer is used")

    print("\nThe streams are kept apart, and only one of them is parsed")
    check("public string StandardOutput;" in text,
          "standard output is carried on its own, beside the transcript")
    check("JsonDocument.Parse(run.StandardOutput)" in text,
          "the environment answer is parsed from standard output alone")
    check("JsonDocument.Parse(run.Output)" not in text,
          "and never from the two of them joined - one line on standard "
          "error lands in the middle of the JSON, and the window then says "
          "no Revit was found on a PC that has three")
    check("LastMeaningfulLine(run.Output)" in text,
          "while the sentence shown to a person still comes from both, "
          "which is what a person would have seen")

    print()
    if FAILURES:
        print("FAILED (%d)" % len(FAILURES))
        for one in FAILURES:
            print("  - %s" % one)
        return 1

    print("The installer takes both pipes at once and parses only the one")
    print("that carries an answer.")
    print()
    print("IT HAS NOT BEEN RUN. This is a text check on C# that names")
    print("powershell.exe, from a Linux machine. Whether it talks to a real")
    print("PowerShell is owed on Windows - AA7 to AA9 in NEEDS-CHECKING.md.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
