#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Make a command line's own output safe to print on a Windows console.

A Windows console - and a pipe or a file a script's output is sent to - is
cp1252 by default. That code page has no Arabic letters, no 'Δ' and no '→',
so a script that prints a sheet name, a model title, a heading path or a typed
word holding one of them stops with UnicodeEncodeError half-way through its
output. On a Gulf project those names are ordinary. Rows 5b-2040 to 5b-2044 of
the register found it in `heron_validate.py` and some thirty other scripts.

`safe_streams()` is called at the top of each script's `main`, before it
prints anything. It reconfigures stdout and stderr to UTF-8 with
errors="replace", as `tools/batch-prove.py` has done since 2026-10-21. Only
what is printed changes - files are still written with their own encoding. A
stream that cannot be reconfigured - a test's StringIO - is left as it is.

It is called from `main`, never at import: the MCP server imports these
modules for their functions, and its own streams are not theirs to change.
"""

import sys


def safe_streams():
    """Reconfigure stdout and stderr to UTF-8 with errors="replace"."""
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
