#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Folder paths hidden from what the chat is told, when the modeller asks for it
(D-180).

    python tests/test_hide_paths.py

WHAT IT HOLDS
-------------
  1. THE RULE      a Windows path - drive letter, network share, either slash,
                   or written into JSON with doubled backslashes - keeps its
                   last part, the file or folder name, and loses every folder
                   above it; two paths in one line are each hidden; and text
                   that is not a path - a ratio, a bare drive, a web address -
                   is left exactly as it was. Anything that is not text
                   passes through untouched.
  2. THE SETTING   privacy.hidePaths, off unless the modeller turns it on, and
                   declared the same on both sides (Python and C#).
  3. THE PLACE     every tool's answer to the chat passes through one door,
                   `_recorded`: hidden there when the setting is on, untouched
                   when it is off - run, through that door, on a stand-in tool.
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

FAILURES = []
CONFIG_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronConfig.cs")


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


CASES = [
    ("Saved to C:\\Projects\\Client X\\Tower MEP.rvt today.", "Saved to …\\Tower MEP.rvt today.",
     "a drive path keeps its file name, folders with spaces gone"),
    ("Share: \\\\fileserver\\jobs\\2041 Acme\\model.rvt", "Share: …\\model.rvt", "a network share"),
    ("C:/Users/ajm/Documents/a.csv", "…/a.csv", "forward slashes"),
    ('{"path": "D:\\\\Work\\\\Acme Tower\\\\run.json"}', '{"path": "…\\\\run.json"}',
     "a path written into JSON with doubled backslashes"),
    ("Excel: C:\\a\\b\\x.xlsx and page: E:\\c\\y.html", "Excel: …\\x.xlsx and page: …\\y.html",
     "two paths in one line, each hidden"),
    ("Its folder is C:\\Users\\ajm\\.heron", "Its folder is …\\.heron", "a folder keeps its own name"),
    ("ratio 3:4, drive C: alone, see https://example.com/a/b", "ratio 3:4, drive C: alone, see https://example.com/a/b",
     "text that is not a path is left as it was"),
    ("No path here at all.", "No path here at all.", "plain text untouched"),
]


def rule():
    print("1. the rule")
    import heron_privacy as P
    for given, wanted, what in CASES:
        got = P.hide_paths(given)
        check(got == wanted, "%s - got %r" % (what, got))
    blob = object()
    check(P.hide_paths(blob) is blob, "anything that is not text passes through untouched")
    check(P.hide_paths(None) is None, "and nothing is still nothing")


def setting():
    print("\n2. the setting")
    import heron_config as cfg
    check(cfg.DEFAULTS.get("privacy.hidePaths") == "false", "privacy.hidePaths, off unless turned on")
    cs = io.open(CONFIG_CS, encoding="utf-8").read()
    check('{ "privacy.hidePaths", "false" }' in cs, "and declared the same in the C# settings")


def place():
    print("\n3. the place - every answer's one door")
    import heron_server_core as C
    folder = tempfile.mkdtemp(prefix="heron-hide-")
    config = os.path.join(folder, "heron.config")
    before = os.environ.get("HERON_CONFIG")
    os.environ["HERON_CONFIG"] = config
    try:
        def heron_capabilities():
            return "The report is at C:\\Projects\\Acme\\report.html"
        door = C._recorded(heron_capabilities)
        for value, hidden in (("true", True), ("false", False)):
            with io.open(config, "w", encoding="utf-8") as fh:
                fh.write(u"privacy.hidePaths = %s\n" % value)
            got = door()
            check(("Acme" not in got and "…\\report.html" in got) if hidden else "C:\\Projects\\Acme" in got,
                  "privacy.hidePaths = %s: %s - got %r" % (value, "folders hidden" if hidden else "untouched", got))
        os.remove(config)
        check("C:\\Projects\\Acme" in door(), "no settings file: the default, untouched")
    finally:
        if before is None:
            os.environ.pop("HERON_CONFIG", None)
        else:
            os.environ["HERON_CONFIG"] = before
        shutil.rmtree(folder, ignore_errors=True)


def main():
    for part in (rule, setting, place):
        try:
            part()
        except Exception as why:                     # noqa: BLE001 - one failure, not a traceback
            check(False, "%s raised %s: %s" % (part.__name__, type(why).__name__, why))
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for what in FAILURES:
            print("  - %s" % what)
        return 1
    print("PASS    folder paths are hidden from the chat when the modeller asks (D-180)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
