# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The pip line a person is told to type for one of Heron's own packages.

    import heron_install as INSTALL
    INSTALL.install_line("pip install --user pyyaml")

ON WINDOWS IT IS ALWAYS THE LOCK. requirements-lock.txt pins and hashes every
package Heron installs - PyYAML, the MCP package and everything they pull in -
and it is the file HeronInstaller.exe installs from. Unpinned, `pip install
mcp` pulled a cryptography that broke on import on 2026-09-16 (README.md), so
no message on Windows names an unpinned line for those (rows 5b-944, 5b-979).

OFF WINDOWS THE CALLER'S OWN LINE COMES BACK. The lock pins pywin32, which has
no wheel but Windows', and pip refuses the whole file for that one line -
measured on Linux on 2026-10-17. So there the unpinned line is the right one.

The lock is for Python 3.11 to 3.14, as its header says. tools/heron-ready.py
checks the Python; this does not.

It imports nothing but os, so a module whose own import has just failed can
still ask it what to say. tests/test_install_lines.py holds LOCK_LINE to the
lock's head, and holds every pip line for a locked package in brain/, mcp/
and tools/ to install_line().
"""

import os

#: requirements-lock.txt's own line, from its head. Run in the Heron folder.
LOCK_LINE = "python -m pip install --user --require-hashes -r requirements-lock.txt"


def install_line(unpinned, windows=None):
    """The pip line for Heron's packages: the lock on Windows, `unpinned` elsewhere."""
    windows = (os.name == "nt") if windows is None else windows
    return LOCK_LINE if windows else unpinned
