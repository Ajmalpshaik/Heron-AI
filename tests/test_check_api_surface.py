# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The API-surface checker says NOT RUN when it cannot answer, never FAIL.

    python tests/test_check_api_surface.py

WHY THIS EXISTS
---------------
FRAGMENT-ISSUES row 5b-150. `tools/check-api-surface.py` answers whether every
Revit member Heron calls exists in every release - and every way it could NOT
answer exited 1, the code a missing member uses. Measured on a machine with no
`dotnet`: a FileNotFoundError traceback, exit 1. So a reader could not tell
"Heron calls an API Revit 2020 does not have" from "this machine has no .NET".

WHAT IT PROVES, on any machine, because it hides `dotnet` itself:
  1. With no `dotnet` on PATH the tool exits 3 and says NOT RUN, before it
     builds or fetches anything.
  2. An unknown release is still refused with its own exit code, 2 - the
     argument question is answered first.
  3. change-evidence records a gate that exits 3 as NOT RUN, not FAIL.
"""

import importlib.util
import io
import os
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-api-surface.py")

for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except AttributeError:
        pass

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def without_dotnet(*args):
    """The tool run with a PATH that holds no `dotnet` - an empty folder."""
    empty = tempfile.mkdtemp(prefix="heron-nodotnet-")
    env = dict(os.environ, PATH=empty)
    out = subprocess.run([sys.executable, TOOL] + list(args), cwd=ROOT, env=env,
                         capture_output=True, text=True, encoding="utf-8",
                         errors="replace", timeout=120)
    os.rmdir(empty)
    return out.returncode, out.stdout + out.stderr


def main():
    print("1. No .NET is NOT RUN, exit 3 - not a failure, not a traceback")
    code, said = without_dotnet("2020")
    check(code == 3, "exit 3 with no dotnet on PATH (got %s)" % code)
    check("NOT RUN" in said and "dotnet" in said,
          "and it says NOT RUN, naming what is missing")
    check("Traceback" not in said, "no traceback")
    check("Building" not in said and "Fetching" not in said,
          "asked before it builds or fetches anything")

    print()
    print("2. An unknown release is still refused first, with its own code")
    code, said = without_dotnet("1999")
    check(code == 2 and "1999" in said, "exit 2 for a release Heron does not know (got %s)" % code)

    print()
    print("3. change-evidence records a gate's exit 3 as NOT RUN")
    spec = importlib.util.spec_from_file_location(
        "heron_change_evidence_api", os.path.join(ROOT, "tools", "change-evidence.py"))
    evidence = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(evidence)
    real = evidence.run
    evidence.run = lambda cmd, timeout=1800: (3, "NOT RUN - no dotnet")
    try:
        got = evidence.gate_results(["check-api-surface"])["check-api-surface"]["result"]
    finally:
        evidence.run = real
    check(got == evidence.NOT_RUN, "a gate that exits 3 is NOT RUN (got %s)" % got)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        return 1
    print("PASSED  a machine that cannot answer says so, and never reads as a defect")
    return 0


if __name__ == "__main__":
    sys.exit(main())
