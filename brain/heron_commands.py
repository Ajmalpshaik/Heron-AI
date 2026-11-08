# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-USR-SKL-003
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Saved commands - one job the modeller did once, kept as a named button.

    python brain/heron_commands.py            # list this PC's saved commands

WHAT IT IS FOR (docs/49)
------------------------
A modeller asks the chat for a job, keeps it, and says "save that as Room
Sections". What is saved is ONE capability, the values it ran with, and the
ones the modeller wants to change each time pulled out as named INPUTS:

    {"heron-command": 1,
     "name": "Room Sections", "tool": "change",
     "capability": "CREATE_SECTION_VIEW",
     "values": [{"name": "levelName", "value": "{level}"}],
     "inputs": [{"name": "level", "label": "Level", "default": "Level 1"}]}

Next time it runs from the chat by its name, or from Revit's Heron panel,
with only the inputs asked.

ONE CAPABILITY, NOT A SCRIPT. Article 8 is one request, one named
TransactionGroup, one undo. A click on a saved command is one request, so it
may run exactly one revit_change - a list of steps would leave several undo
entries behind one click. Several steps wait for a decision (docs/49 s6).

WHAT IS CHECKED HERE AND WHAT IS NOT
-------------------------------------
Saving checks the SHAPE against the fragment's own contract - every value is
a name the fragment takes from the request, every {input} is declared and
every input is used - so a typo is caught when the command is made, not the
day somebody clicks it. Whether the capability is PROVEN, which switch it
needs, and whether this Revit is the right one are NOT checked here: they are
revit_change's, asked again on every run, because a proof can go stale
between the day a command is saved and the day it runs (D-30).

WHERE THEY LIVE
---------------
`<data home>/commands/personal/<slug>.json` - the Python side's data home,
%USERPROFILE%\\.heron on Windows (heron_paths.home, D-135), because the chat
and Revit must read ONE folder and a Microsoft Store Claude keeps a private
copy of %APPDATA%. The add-in reads the same folder (HeronPaths.Commands).
Personal only for now; sharing with the team is docs/49 s6.

THE RUNNER NOTE
---------------
Revit's panel has no Python of its own. Each chat's MCP process leaves
`commands/runner.json` naming the Python and the Heron folder it runs from;
the panel starts the runner with exactly those. A command can only have been
saved by a chat, so the note is there before the first command is.

Stdlib only. Nothing here talks to Revit.
"""

from __future__ import print_function

import io
import json
import os
import re
import sys
import time

FORMAT = 1
TOOLS = ("change", "read")
COMMANDS_FOLDER = "commands"
PERSONAL = "personal"
RUNNER_NOTE = "runner.json"

# An input's name goes inside braces in a value and is typed by a person into
# a box in Revit, so it is kept to what reads well in both places.
INPUT_NAME = re.compile(r"^[A-Za-z][A-Za-z0-9_]{0,39}$")
PLACEHOLDER = re.compile(r"\{([A-Za-z][A-Za-z0-9_]{0,39})\}")
NAME_MAX = 60


class CommandError(Exception):
    """A command that cannot be saved, found or filled - the message says why
    in the modeller's words."""


# ---------------------------------------------------------------- the folder

def _home(home=None):
    if home:
        return home
    here = os.path.dirname(os.path.abspath(__file__))
    if here not in sys.path:
        sys.path.insert(0, here)
    import heron_paths
    return heron_paths.home()


def folder(home=None, make=False):
    """`<home>/commands/personal`, or None on a machine with no data home."""
    base = _home(home)
    if not base:
        return None
    path = os.path.join(base, COMMANDS_FOLDER, PERSONAL)
    if make and not os.path.isdir(path):
        os.makedirs(path)
    return path


def slug(name):
    """The file name a command is kept under: lower case, words joined by a
    hyphen. "Room Sections (L1)" -> "room-sections-l1"."""
    text = re.sub(r"[^a-z0-9]+", "-", (name or "").lower()).strip("-")
    return text[:NAME_MAX]


# ---------------------------------------------------------------- the shape

def _request_names(needs):
    return set(n.get("name") for n in (needs or []) if n.get("source") == "request")


def problems(command, needs):
    """Every reason `command` cannot be saved as it is, in plain words. Empty
    when it can. `needs` is the fragment's contract.needs (fragment_needs)."""
    found = []
    if not isinstance(command, dict):
        return ["A command must be a set of named fields."]
    name = (command.get("name") or "").strip()
    if not name:
        found.append("It has no name. Give it the name the modeller will look for.")
    elif len(name) > NAME_MAX or not slug(name):
        found.append("The name '%s' must be 1 to %d characters with at least one letter "
                     "or digit." % (name, NAME_MAX))
    if command.get("tool") not in TOOLS:
        found.append("'tool' must be 'change' or 'read', not '%s'." % command.get("tool"))
    if not (command.get("capability") or "").strip():
        found.append("It names no capability.")
    if needs is None:
        found.append("Heron could not read what '%s' takes, so it cannot be checked."
                     % command.get("capability"))
        return found

    takes = _request_names(needs)
    values = command.get("values") or []
    inputs = command.get("inputs") or []
    declared = []
    for entry in inputs:
        key = (entry or {}).get("name") or ""
        if not INPUT_NAME.match(key):
            found.append("The input '%s' needs a short name of letters, digits and _ "
                         "starting with a letter." % key)
        elif key in declared:
            found.append("The input '%s' is declared twice." % key)
        else:
            declared.append(key)

    used, seen = set(), set()
    for entry in values:
        key = (entry or {}).get("name") or ""
        text = (entry or {}).get("value")
        if key not in takes:
            found.append("'%s' is not a value %s takes%s." % (
                key, command.get("capability"),
                " - it takes: %s" % ", ".join(sorted(takes)) if takes else " - it takes none"))
        if key in seen:
            found.append("'%s' is given twice." % key)
        seen.add(key)
        if not isinstance(text, str):
            found.append("The value of '%s' must be text." % key)
            continue
        if "\n" in text or "\r" in text:
            found.append("The value of '%s' runs over more than one line." % key)
        for placeholder in PLACEHOLDER.findall(text):
            used.add(placeholder)
            if placeholder not in declared:
                found.append("'{%s}' in '%s' is not one of the inputs." % (placeholder, key))
    for key in declared:
        if key not in used:
            found.append("The input '%s' is used by no value." % key)
    return found


# ---------------------------------------------------------------- keep and read

def _path(name, home=None, make=False):
    where = folder(home, make=make)
    if where is None:
        raise CommandError("This PC has no folder for Heron's data, so nothing can be "
                           "saved or read here. On Windows it is %USERPROFILE%\\.heron.")
    return os.path.join(where, slug(name) + ".json")


def save(command, needs, home=None, replace=False, now=None):
    """Check `command` against the fragment's needs and keep it. Returns the
    path. Raises CommandError naming every problem, and keeps nothing."""
    found = problems(command, needs)
    if found:
        raise CommandError("Not saved:\n  - " + "\n  - ".join(found))
    path = _path(command["name"], home, make=True)
    if os.path.exists(path) and not replace:
        raise CommandError("A command called '%s' is already saved. Say to replace it, "
                           "or give this one another name." % command["name"])
    kept = {
        "heron-command": FORMAT,
        "name": command["name"].strip(),
        "description": (command.get("description") or "").strip(),
        "tool": command["tool"],
        "capability": command["capability"].strip().upper(),
        "values": [{"name": v["name"], "value": v["value"]} for v in command.get("values") or []],
        "inputs": [{"name": i["name"],
                    "label": (i.get("label") or i["name"]).strip(),
                    "default": i.get("default") if isinstance(i.get("default"), str) else ""}
                   for i in command.get("inputs") or []],
        "words": (command.get("words") or "").strip(),
        "saved": now or time.strftime("%Y-%m-%dT%H:%M:%S"),
    }
    # WRITTEN WHOLE OR NOT AT ALL: the panel may be reading this folder while
    # the chat writes, and half a file would show as a broken command.
    part = path + ".part"
    with io.open(part, "w", encoding="utf-8") as fh:
        fh.write(json.dumps(kept, indent=2, ensure_ascii=False))
    os.replace(part, path)
    return path


def _read(path):
    with io.open(path, "r", encoding="utf-8") as fh:
        data = json.load(fh)
    if not isinstance(data, dict) or data.get("heron-command") != FORMAT:
        raise ValueError("not a Heron command of format %d" % FORMAT)
    return data


def all_commands(home=None):
    """(commands, unreadable) - every saved command sorted by name, and the
    file names that could not be read, so a broken file is named, not hidden."""
    where = folder(home)
    if where is None or not os.path.isdir(where):
        return [], []
    found, broken = [], []
    for name in sorted(os.listdir(where)):
        if not name.endswith(".json"):
            continue
        try:
            found.append(_read(os.path.join(where, name)))
        except (OSError, ValueError):
            broken.append(name)
    found.sort(key=lambda c: c.get("name", "").lower())
    return found, broken


def find(name, home=None):
    """The command saved under `name`, matched by its slug, or CommandError."""
    path = _path(name, home)
    if not os.path.isfile(path):
        have = [c["name"] for c in all_commands(home)[0]]
        raise CommandError("No command called '%s' is saved.%s" % (
            name, " Saved: %s." % ", ".join(have) if have else " None is saved yet."))
    try:
        return _read(path)
    except (OSError, ValueError) as why:
        raise CommandError("The command '%s' could not be read: %s" % (name, why))


def delete(name, home=None):
    """Remove one saved command. Returns its name as it was saved."""
    command = find(name, home)
    os.remove(_path(name, home))
    return command["name"]


# ---------------------------------------------------------------- fill

def fill(command, given=None):
    """The command's values with its inputs put in, as revit_change's
    `values` text: one "name=value" per line. An input not given takes its
    default; one with neither is refused by name."""
    given = dict(given or {})
    declared = [i["name"] for i in command.get("inputs") or []]
    unknown = sorted(set(given) - set(declared))
    if unknown:
        raise CommandError("'%s' has no input called %s. Its inputs: %s." % (
            command.get("name"), ", ".join(unknown), ", ".join(declared) or "none"))
    chosen = {}
    for entry in command.get("inputs") or []:
        value = given.get(entry["name"])
        if value is None or value == "":
            value = entry.get("default") or ""
        if value == "":
            raise CommandError("'%s' needs a value for %s." % (command.get("name"),
                                                               entry.get("label") or entry["name"]))
        if "\n" in value or "\r" in value:
            raise CommandError("%s must be one line." % (entry.get("label") or entry["name"]))
        chosen[entry["name"]] = value

    lines = []
    for entry in command.get("values") or []:
        text = PLACEHOLDER.sub(lambda m: chosen[m.group(1)], entry["value"])
        # TRIMMED UNLESS IN DOUBLE QUOTES - revit_change's own rule - so a
        # value whose edges are spaces is quoted to keep them.
        if text != text.strip():
            text = '"%s"' % text
        lines.append("%s=%s" % (entry["name"], text))
    return "\n".join(lines)


# ---------------------------------------------------------------- runner note

def write_runner_note(python, root, home=None, now=None):
    """Leave the Python and Heron folder this chat runs from, for Revit's
    panel. Returns the path, or None on a machine with no data home. Never
    fatal: a chat must not stop because this note could not be written."""
    try:
        base = _home(home)
        if not base:
            return None
        where = os.path.join(base, COMMANDS_FOLDER)
        if not os.path.isdir(where):
            os.makedirs(where)
        note = {
            "python": python,
            "root": root,
            "runner": os.path.join(root, "mcp", "server", "heron_command_run.py"),
            "written": now or time.strftime("%Y-%m-%dT%H:%M:%S"),
        }
        path = os.path.join(where, RUNNER_NOTE)
        part = path + ".part"
        with io.open(part, "w", encoding="utf-8") as fh:
            fh.write(json.dumps(note, indent=2, ensure_ascii=False))
        os.replace(part, path)
        return path
    except (OSError, ImportError):
        return None


def describe(command):
    """One command in two lines, for the chat."""
    inputs = ", ".join("%s (default %s)" % (i.get("label") or i["name"], i["default"])
                       if i.get("default") else (i.get("label") or i["name"])
                       for i in command.get("inputs") or [])
    return "%s - %s %s%s" % (
        command.get("name"), "changes the model with" if command.get("tool") == "change"
        else "reads with", command.get("capability"),
        "\n    inputs: %s" % inputs if inputs else "\n    no inputs")


def main(argv):
    found, broken = all_commands()
    if folder() is None:
        print("This PC has no folder for Heron's data.")
        return 0
    if not found:
        print("No saved commands in %s" % folder())
    for command in found:
        print(describe(command))
    for name in broken:
        print("could not read %s" % name)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
