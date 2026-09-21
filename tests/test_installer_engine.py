#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The install engine's decisions, run against a fake Revit.

WHAT THIS PROVES
----------------
That tools/../platform/Heron.Installer decides correctly before it acts:

    a heading installs nothing of its own, and says which pieces to pick
    a PROVING product is never offered to a user
    a release the product does not support is skipped WITH THE REASON - R-10
    a release on the PC that was not ticked is reported, not passed over
    it WAITS while Revit is open and carries on by itself - R-38a
    a Revit whose release cannot be read blocks everything rather than guessing
    reaching the wait ceiling changes NOTHING
    one product failing does not stop the others - R-19

Every one of those is a way an installer goes wrong quietly, which is the
kind this repository has been bitten by: AJ Tools' lesson L3 installed nothing
at all on three releases and reported no failure.

WHAT IT CANNOT PROVE
--------------------
That anything installs. No file is written, no Revit is looked for, no
PowerShell runs. The two adapters that reach Windows - PowerShellRevit and
DeployScriptDeployer - are NOT exercised here and need the owner's machine.
A green run here is not an install and must never be reported as one.

    python tests/test_installer_engine.py

Exit 0 = the engine decides correctly.
Exit 1 = it does not, and the failing rule is named.
Exit 3 = could not run: no .NET SDK on this machine. NOT a pass.
"""

import os
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HOST = os.path.join("tests", "Heron.Installer.TestHost")

NO_SDK = 3


def run(args, timeout):
    proc = subprocess.Popen(args, cwd=ROOT, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT)
    try:
        out = proc.communicate(timeout=timeout)[0]
    except subprocess.TimeoutExpired:
        proc.kill()
        return None, "timed out after %d seconds" % timeout
    return proc.returncode, out.decode("utf-8", "replace")


def main():
    if shutil.which("dotnet") is None:
        print("NOT RUN  no `dotnet` on this machine, so the engine cannot be built.")
        print()
        print("         This is the machine, not the change. Install the SDK and")
        print("         run it again - one apt package, and nothing here needs")
        print("         Windows or Revit:")
        print()
        print("             apt-get update && apt-get install -y dotnet-sdk-10.0")
        print()
        print("         Exit 3 means COULD NOT RUN. It is not a pass.")
        return NO_SDK

    print("Building the test host")
    code, out = run(["dotnet", "build", HOST, "--nologo", "-v", "quiet"], 600)
    if code != 0:
        print(out)
        print("FAILED  the install engine does not build, so none of its rules")
        print("        could be checked. That is the change, not the machine.")
        return 1
    print("  ok    built")
    print()

    code, out = run(["dotnet", "run", "--project", HOST, "--nologo",
                     "--no-build"], 300)
    print(out)

    if code is None:
        print("FAILED  %s" % out)
        return 1

    if code != 0:
        print("FAILED  the engine does not decide correctly. The failing rules are")
        print("        named above, and each one is a way an install goes wrong")
        print("        quietly on somebody's machine.")
        return 1

    # The host's own closing lines are its claim about what it did NOT do.
    # If they ever go missing, a green run here starts reading like an install.
    if "HAS INSTALLED NOTHING" not in out:
        print("FAILED  the test host no longer says that it installed nothing.")
        print("        That sentence is what stops a green run here being read")
        print("        as a working install. Put it back.")
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
