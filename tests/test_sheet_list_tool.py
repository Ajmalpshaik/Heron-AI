# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The chat's `heron_sheet_list` tool, through the real server (docs/48 section
5 item 7).

    python tests/test_sheet_list_tool.py

WHAT IT PROVES
  1. It is declared READ with no operation - it reaches no Revit.
  2. Through the server's own function: a CSV register gives its sheets and
     the exact `sheets=` value; the route it names follows the status the
     card CREATE_SHEETS_FROM_TABLE has on disk today, read the way the server
     reads it; an .xls is refused in words.

It needs the MCP SDK and exits 3 without it - NOT a pass. The reader itself,
which needs nothing, is tests/test_sheet_list.py.
"""

from __future__ import print_function

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines and data go to a scratch folder, never the owner's
# real trail (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant(data=True)

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def tool(folder):
    print("the MCP tool")
    import heron_mcp_server as S
    import heron_tools as T
    import heron_sheet_list as L
    check(T.TOOLS.get("heron_sheet_list") == (T.READ, None), "declared READ, no operation")
    body = getattr(S.heron_sheet_list, "__wrapped__", S.heron_sheet_list)

    path = os.path.join(folder, "register.csv")
    with io.open(path, "w", encoding="utf-8", newline="") as handle:
        handle.write(u"Sheet Number,Sheet Name,Drawn By\r\nM-101,Level 1 Plan,AP\r\nM-102,Level 2 Plan,AP\r\n")
    said = body(path)
    value = L.card_value(L.read(path))
    check("2 sheet(s) to make" in said and "sheets=" + value in said,
          "a CSV gives its sheets and the exact value")
    _folder, status = S._fragment_for("CREATE_SHEETS_FROM_TABLE")
    check(status is not None, "the card is found on disk, at %s" % status)
    if (status or "").upper() in ("PROVEN", "PRODUCTION"):
        check("revit_change with previewOnly=true" in said, "PROVEN: revit_change is named")
    else:
        check("refuses it (D-117)" in said and "try-once create-sheets-from-table" in said,
              "not PROVEN: the command-line preview and try-once are named, not revit_change")
    old = os.path.join(folder, "old.xls")
    with io.open(old, "wb") as handle:
        handle.write(b"\xd0\xcf\x11\xe0" + b"\x00" * 32)
    check("Save As" in body(old), "an .xls is refused with how to save it")
    check("there is no file" in body(os.path.join(folder, "missing.xlsx")), "a missing file is said")


def main():
    try:
        import mcp  # noqa: F401
    except ImportError:
        print("NOT RUN - the MCP SDK is not installed")
        return 3
    folder = tempfile.mkdtemp(prefix="heron-sheet-list-tool-")
    try:
        tool(folder)
    finally:
        shutil.rmtree(folder, ignore_errors=True)
    print()
    if FAILURES:
        print("%d FAILED" % len(FAILURES))
        return 1
    print("All passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
