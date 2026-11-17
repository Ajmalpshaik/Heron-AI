# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
Folder paths hidden from what the chat is told (D-180).

A reply Heron gives the chat goes to the AI provider with it. A model's full
path - C:\\Projects\\Acme Hospital Phase 2\\... - can say more than the model
does: the client, the job, the office's folder plan. With privacy.hidePaths
on, every Windows path in a reply keeps only its LAST part - the file or
folder name - and loses every folder above it:

    C:\\Projects\\Client X\\Tower MEP.rvt     ->   …\\Tower MEP.rvt
    \\\\fileserver\\jobs\\2041\\model.rvt        ->   …\\model.rvt

Applied once, where every tool's answer leaves for the chat
(heron_server_core._recorded). Nothing else changes: the model, the files on
disk, the Companion's own activity list on this PC, and the logs keep their
full paths. NOT ANONYMISATION: a file name, an element's parameter value or a
view's name can still say what a folder would have.

No Revit, no network; `said` reads the settings file and nothing else.
tests/test_hide_paths.py holds it, with no MCP SDK needed.
"""

import re

# One part of a path: anything Windows allows in a file or folder name, spaces
# and dots included. A colon is not allowed, which is what stops one path from
# running on into the next sentence.
_PART = r'[^\\/:*?"<>|\r\n\t]+'
# The separator: one backslash, or two - a path written into JSON.
_BACK = r'(\\\\|\\)'

_DRIVE = re.compile(r'(?<![A-Za-z0-9])[A-Za-z]:' + _BACK + r'(?:' + _PART + r'(?:\\\\|\\))+')
_DRIVE_FORWARD = re.compile(r'(?<![A-Za-z0-9])[A-Za-z]:/(?:' + _PART + r'/)+')
_SHARE = re.compile(u'(?<![\\\\:\u2026])' + r'(\\\\\\\\|\\\\)' + _PART + r'(?:\\\\|\\)(?:' + _PART + r'(?:\\\\|\\))*')

HIDDEN = u"\u2026"


def hide_paths(text):
    """`text` with every Windows path's folders replaced by "…", its last part
    kept. Anything that is not text is returned as it came."""
    if not isinstance(text, str):
        return text
    # THE DRIVE FIRST: in JSON "D:\\\\Work" the doubled backslash after the
    # drive would otherwise read as the start of a network share.
    text = _DRIVE.sub(lambda m: HIDDEN + m.group(1), text)
    text = _DRIVE_FORWARD.sub(HIDDEN + "/", text)
    return _SHARE.sub(lambda m: HIDDEN + ("\\\\" if m.group(1) == "\\\\\\\\" else "\\"), text)


def said(reply):
    """`reply` as the chat is told it: every path's folders hidden when the
    modeller turned privacy.hidePaths on, untouched when it is off or the
    reply is not text (a picture). Read fresh each time - a change to the
    settings file applies to the next answer. heron_config.load never raises;
    an unreadable file is the default, off."""
    if not isinstance(reply, str):
        return reply
    import heron_config as configuration
    if configuration.truthy(configuration.load(), "privacy.hidePaths"):
        return hide_paths(reply)
    return reply
