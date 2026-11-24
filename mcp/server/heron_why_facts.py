#!/usr/bin/env python3
# Heron-Agent:  HERON-MCP-SRV-001
# Heron-Step:   5
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
What heron_why judges by, each fact read without asking Revit anything - and
what revit_health last heard from each Revit, kept here for heron_why to read:

    _announced            every Revit whose Heron button is on, from the
                          address book, with no pipe opened and no ping sent
    _why_facts            the release, the bridge, the switches and the
                          announced Revits, as one dict
    _SETTINGS_HEARD,      what each Revit last said of its switches and its
    _settings_heard,      settings file, as revit_health heard it (Q-70,
    _heard_from           D-166) - never a guess between two Revits
    _ago                  how long ago, in words
    _why_switches_line,   the two lines heron_why prints about where its facts
    _why_release_line     came from

NOT RUN, AND NO TOOL HERE. heron_why and revit_health call these, and both stay
in heron_mcp_server.py, which imports this file near its top. A file that holds
no tool registers nothing, so where it is imported changes nothing a host is
served (heron_tools.served_order). Why heron_why asks Revit nothing is the
comment above it, in that file.

WHY THEY ARE HERE. They sat in heron_mcp_server.py until 2026-11-24 and were
moved whole and unchanged, to make that file smaller. The binding and whether
this process serves the library alone are heron_server_core's, and this file
takes them from there. It imports no other file of the server, and never the
entry file, which runs as `__main__` and would run a second time.

ONE _SETTINGS_HEARD. The entry file imports it by name, and a name imported is
the same dict, so what revit_health records and what heron_why reads is one
record.
"""

import io
import os
import time

import heron_server_core as core                           # noqa: E402
from heron_server_core import NO_REVIT_HERE, binding       # noqa: E402
import heron_bridge_client as bridge                       # noqa: E402
import heron_session as session                            # noqa: E402


def _announced():
    """
    Every Revit whose Heron button is on, as the address book says - and
    nothing asked of any of them: no pipe opened, no ping sent.

    bridge.discover() reads the same folder and then talks to each entry; this
    stops before that. An entry is left out when its process is known to be
    gone, by the test discover() uses, or when it speaks a protocol this
    client does not - Heron would refuse to talk to it. "Cannot tell" is
    never "gone", discover()'s own rule. Nothing is deleted: pruning a dead
    entry is discover()'s, and an address book is the add-in's.
    """
    import json
    found = []
    try:
        names = sorted(os.listdir(bridge.DISCOVERY_DIR))
    except OSError:
        return found
    for name in names:
        if not name.endswith(".json"):
            continue
        path = os.path.join(bridge.DISCOVERY_DIR, name)
        try:
            with io.open(path, "r", encoding="utf-8") as fh:
                record = json.load(fh)
        except (OSError, ValueError):
            continue
        if not isinstance(record, dict):
            continue
        one = bridge.Bridge(record, path)
        if one.protocol_version != bridge.PROTOCOL_VERSION:
            continue
        if bridge.bridge_process_is_running(one.pid) is False:
            continue
        found.append(one)
    return found


def _why_facts(asked):
    """
    What heron_why judges by, each read without asking Revit anything:
    {"release", "how", "bridge", "switches", "announced"}.

    `asked` is the release the chat named, or None for the Revit this chat
    would use - the session layer's own rule (heron_session.version_for_filter)
    over the address book, with this chat's choice. `switches` is None off
    Windows (D-132): no switch of the owner's is on this machine to read.
    """
    if core.LIBRARY_ONLY:
        return {"release": asked, "how": "as asked" if asked else NO_REVIT_HERE,
                "bridge": "none", "switches": None, "announced": [],
                "heard": None}
    announced = _announced()
    if asked:
        release, how = asked, "as asked"
    else:
        release, how = session.version_for_filter(announced, binding.pid,
                                                  binding.was_chosen)
    there = release is not None and any(str(b.revit_version) == str(release)
                                        for b in announced)
    # WHAT THAT REVIT LAST SAID IT HOLDS (D-166, Q-70). Changes, Admin and
    # Publish are in each Revit's memory since D-166 and in no file, so the
    # only word there is is the Revit's - remembered by revit_health, never
    # asked here: heron_why sends nothing to Revit (PROPOSALS F1), and the
    # answer says how old the word is. With none heard, every switch is
    # judged off - the way each Revit starts - and the answer says that was
    # assumed, not said. (Until D-166 this read this PC's heron.config, and a
    # Revit's word only where revit_health found its file differed.)
    heard = _heard_from(release, announced)
    held = heard["switches"] if heard is not None else {}
    switches = {"write_enabled": bool(held.get("changes")),
                "admin_enabled": bool(held.get("admin")),
                "publish_enabled": bool(held.get("publish"))}
    return {"release": release, "how": how,
            "bridge": "connected" if there else "none",
            "switches": switches,
            "announced": announced, "heard": heard}


# WHAT EACH REVIT LAST SAID OF ITS SWITCHES TO THIS CHAT'S revit_health, by
# session (D-166, Q-70) - Changes, Admin and Publish as it holds them in
# memory, the settings file it named for the rest, and whether this chat's own
# copy was not that file. Read by heron_why alone, which may ask Revit
# nothing; nothing decides by it, and Revit looks at its switches again before
# every change whatever this holds.
_SETTINGS_HEARD = {}


def _settings_heard(b, held, said, differs):
    """Remember what one Revit said - its switches (a switches_state dict) and
    the file it named (a settings_state dict, or None) - or forget an earlier
    answer when this one said no switches Heron could use."""
    if isinstance(held, dict):
        _SETTINGS_HEARD[b.pid] = {"switches": held,
                                  "file": said if isinstance(said, dict) else None,
                                  "differs": bool(differs),
                                  "at": time.monotonic(), "pid": b.pid,
                                  "release": b.revit_version}
    else:
        _SETTINGS_HEARD.pop(b.pid, None)


def _heard_from(release, announced):
    """What revit_health last heard from the one announced Revit of this
    release - this chat's, when it is one of them - or None: never a guess
    between two."""
    if release is None:
        return None
    there = [b for b in announced if str(b.revit_version) == str(release)]
    there = [b for b in there if b.pid == binding.pid] or there
    if len(there) != 1:
        return None
    heard = _SETTINGS_HEARD.get(there[0].pid)
    if heard is None or str(heard["release"]) != str(release):
        return None
    return heard


def _ago(at):
    """How long ago a time.monotonic() reading was, in words."""
    minutes = int(max(0.0, time.monotonic() - at) // 60)
    if minutes < 1:
        return "just now"
    if minutes < 60:
        return "%d minute%s ago" % (minutes, "" if minutes == 1 else "s")
    hours = minutes // 60
    return "%d hour%s ago" % (hours, "" if hours == 1 else "s")


def _why_switches_line(facts):
    """Which switches heron_why judged by, and whose word they were, in one line."""
    on = facts["switches"]
    named = "Changes %s, Admin %s, Publish %s" % tuple(
        "on" if on[k] else "off" for k in ("write_enabled", "admin_enabled", "publish_enabled"))
    heard = facts["heard"]
    if heard is None:
        return ("%s - assumed, not said: no Revit of this release has told this chat's "
                "revit_health what it holds. Each Revit holds them in memory, off "
                "whenever it starts (D-166), and revit_health asks it" % named)
    who = "Revit %s (session %s)" % (heard["release"], heard["pid"])
    return ("%s - as %s said it holds them, when revit_health asked it %s; held in "
            "its memory (D-166), so a switch pressed since then is not seen here"
            % (named, who, _ago(heard["at"])))


def _why_release_line(facts):
    """Where heron_why's release came from, in one line."""
    release, how = facts["release"], facts["how"]
    if core.LIBRARY_ONLY:
        return ("Revit %s, as asked - %s" % (release, NO_REVIT_HERE) if release
                else "not known - %s" % NO_REVIT_HERE)
    if how == "as asked":
        return ("Revit %s, as asked; %s Revit %s has its Heron button on"
                % (release, "a" if facts["bridge"] == "connected" else "no",
                   release))
    if release is None:
        return "not known - %s" % how
    return "Revit %s - %s" % (release, how)
