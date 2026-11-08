# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-USR-SKL-003
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The chat's `heron_commands` tool, through the real fragment contracts (docs/49).

    python tests/test_commands_tool.py

WHAT IT PROVES
  1. It is declared READ with no operation, and the panel's runs are declared
     at revit_change's and revit_read's own levels.
  2. A command is saved through the real fragment contract; a typo is refused
     by name; a PUBLISH job and an unknown capability save nothing; a READ
     fragment is saved to run through revit_read; list, show and delete work.

It needs the MCP SDK and exits 3 without it - NOT a pass. The store and the
runner, which need nothing, are tests/test_commands.py.
"""

from __future__ import print_function

import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def tool(home):
    print("the MCP tool")
    os.environ["HERON_DATA"] = home
    import heron_mcp_server as S
    import heron_tools as T
    check(T.TOOLS.get("heron_commands") == (T.READ, None), "declared READ, no operation")
    check(T.PANEL_ACTIONS["panel_command_change"] == (T.MODIFY, "run_fragment_write")
          and T.PANEL_ACTIONS["panel_command_read"] == (T.ANALYZE, "run_fragment_read"),
          "the panel's runs are revit_change's and revit_read's own levels")
    body = getattr(S.heron_commands, "__wrapped__", S.heron_commands)
    said = body("save", name="Sheets A", capability="CREATE_SHEETS",
                values="numbers={first}\nnamePrefix=A-", inputs="first|First number|A101")
    check(said.startswith("Saved 'Sheets A'"), "a command is saved through the real contract")
    said = body("save", name="Typo", capability="CREATE_SHEETS", values="numbrs=A1")
    check("'numbrs' is not a value CREATE_SHEETS takes" in said, "a typo is refused by name")
    said = body("save", name="Sync", capability="SYNC_WITH_CENTRAL")
    check("not saved as a button" in said, "a PUBLISH job is never saved as a button")
    said = body("save", name="Nothing", capability="NO_SUCH_THING")
    check("nothing was saved" in said, "an unknown capability saves nothing")
    check("Sheets A" in body("list"), "it is listed")
    shown = body("show", name="sheets a")
    check("numbers={first}" in shown and "revit_change" in shown, "show gives values and the door")
    said = body("save", name="Find Duplicates", capability="FIND_DUPLICATE_ELEMENTS")
    check(said.startswith("Saved") and "revit_read" in body("show", name="Find Duplicates"),
          "a READ fragment is saved to run through revit_read - the read door's own ceiling")
    check(body("delete", name="Sheets A").startswith("Deleted"), "it is deleted when asked")
    check("not 'run'" in body("run"), "an unknown action is named")


def main():
    try:
        import mcp  # noqa: F401
    except ImportError:
        print("NOT RUN - the MCP SDK is not installed")
        return 3
    home = tempfile.mkdtemp(prefix="heron-commands-tool-")
    try:
        tool(home)
    finally:
        shutil.rmtree(home, ignore_errors=True)
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    heron_commands saves through the real contracts and touches no model")
    return 0


if __name__ == "__main__":
    sys.exit(main())
