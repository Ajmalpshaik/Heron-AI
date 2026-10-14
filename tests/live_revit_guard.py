#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

r"""
A test suite must never reach the Revit running on this PC - FRAGMENT-ISSUES
row 5b-461.

    import live_revit_guard as GUARD
    guard = GUARD.Guard.around(bridge).install()   # BEFORE discovery is moved
    env = guard.for_child(os.environ, scratch)     # and for a child process
    ...
    guard.stop()
    guard.touched()                                # [] when nothing was

Not a suite: check-gaps runs tests/test_*.py, and this is what two of them
import.

WHY IT EXISTS. test_mcp_serves.py and test_mcp_stdio.py ask the bridge client
which Revits are connected - heron_resolve's version wall, revit_health,
heron_lookup - and until row 5b-461 the client answered from the REAL
discovery folder, %LOCALAPPDATA%\Heron\bridges, by opening the pipe of every
Revit listed there. Measured 2026-10-13: 1,842 open attempts on a live Revit's
pipe in one run of the first suite, 121 in the second, and both exited 0 -
an answered ping changes no assertion. Every check-gaps and change-evidence
sweep runs both while the owner's chats use that Revit, and until row 5b-458
a new connection cut off the answer Revit was still working on.

WHAT IT WATCHES, through sys.addaudithook, recording each with its path:

  * an open of a NAMED PIPE - REFUSED with PermissionError before Windows is
    asked. Any named pipe, not only the ones the folder lists: neither suite
    has a reason to open one, since their Revits are stand-ins answering
    in-process, and a Revit started half-way through a run is not in a list
    read at its start.
  * a listing of the real discovery folder, or a file opened inside it.
  * a file removed from it - REFUSED. discover() prunes the entry of a Revit
    whose process has gone, and that is the add-in's address book, not the
    suite's.

The refusals are what make it safe to run a suite as it stood before its fix,
which heron-ship section 2a asks for: the check goes red on the old code
without the old code reaching Revit.

IN A CHILD the hook is installed by a sitecustomize.py written to a scratch
folder put first on PYTHONPATH - the way the 2026-10-13 measurement did it -
and each finding is appended to a file the parent reads afterwards, because a
child ended with terminate() runs no atexit. A sitecustomize of the machine's
own would be shadowed for that one child; the owner's Python has none
(measured 2026-10-14).

WHAT IT CANNOT SEE. A pipe opened by any route but open() - ctypes, _winapi -
raises no "open" event. The bridge client opens with open(), which is the
route guarded here; a second route would need its own event.
"""

import io
import json
import os
import sys

# The environment variable a guarded child reads its guard from.
CHILD_SPEC = "HERON_TEST_LIVE_REVIT_GUARD"

SITECUSTOMIZE = (
    "# Written by tests/live_revit_guard.py for one test run. Not part of Heron.\n"
    "import importlib.util, json, os\n"
    "_spec = json.loads(os.environ[%r])\n"
    "_load = importlib.util.spec_from_file_location('live_revit_guard', _spec['module'])\n"
    "_guard = importlib.util.module_from_spec(_load)\n"
    "_load.loader.exec_module(_guard)\n"
    "_guard.Guard(_spec['folder'], _spec['pipes'], _spec['log']).install()\n"
    % CHILD_SPEC)

WATCHED = ("open", "os.listdir", "os.scandir", "os.remove")


def _text(value):
    """The path an audit event carried, as text - None for a descriptor."""
    if value is None or isinstance(value, int):
        return None
    try:
        value = os.fspath(value)
    except TypeError:
        return None
    if isinstance(value, bytes):
        value = value.decode("utf-8", "replace")
    return value


def _pipe_key(path):
    return path.replace("/", "\\").lower()


def is_named_pipe(path):
    key = _pipe_key(path)
    return key.startswith("\\\\.\\pipe\\") or key.startswith("\\\\?\\pipe\\")


def _same(path):
    return os.path.normcase(os.path.abspath(path))


class Guard(object):
    """Watches one process for any touch of the real discovery folder or a pipe."""

    def __init__(self, folder, pipes=(), log=None):
        self.folder = _same(folder)
        self.pipes = set(_pipe_key(p) for p in pipes)
        self.log = log
        self.seen = []
        self._child_logs = []
        self._on = False

    @classmethod
    def around(cls, bridge):
        """The guard for the folder `bridge` names NOW - so it is built before a
        suite points the client anywhere else - and the pipes its records name,
        spelled by the client's own Bridge.pipe_path."""
        folder = bridge.DISCOVERY_DIR
        pipes = []
        try:
            names = sorted(os.listdir(folder))
        except OSError:
            names = []
        for name in names:
            if not name.endswith(".json"):
                continue
            path = os.path.join(folder, name)
            try:
                with io.open(path, encoding="utf-8") as fh:
                    record = json.load(fh)
            except (OSError, ValueError):
                continue
            if isinstance(record, dict) and record.get("pipeName"):
                pipes.append(bridge.Bridge(record, path).pipe_path)
        return cls(folder, pipes)

    def describe(self):
        if not os.path.isdir(self.folder):
            return "the real discovery folder %s (not on this PC)" % self.folder
        return ("the real discovery folder %s, which names %d Revit pipe(s)"
                % (self.folder, len(self.pipes)))

    def install(self):
        self._on = True
        sys.addaudithook(self._hook)
        return self

    def stop(self):
        """An audit hook cannot be removed. It can stop looking."""
        self._on = False

    def _hook(self, event, args):
        if not self._on or event not in WATCHED or not args:
            return
        path = _text(args[0])
        if path is None:
            return
        if is_named_pipe(path):
            if event == "open":
                listed = " - a Revit on this PC listens on it" if _pipe_key(path) in self.pipes else ""
                self._note("pipe %s opened%s" % (path, listed))
                raise PermissionError("refused by tests/live_revit_guard.py - a test may "
                                      "not open a named pipe: %s" % path)
            return
        here = _same(path)
        if event in ("os.listdir", "os.scandir"):
            if here == self.folder:
                self._note("listed %s, the real discovery folder" % path)
        elif os.path.dirname(here) == self.folder:
            if event == "open":
                self._note("read %s, in the real discovery folder" % path)
            else:
                self._note("removed %s, from the real discovery folder" % path)
                raise PermissionError("refused by tests/live_revit_guard.py - a test may "
                                      "not remove a Revit's discovery file: %s" % path)

    def _note(self, what):
        # The log's own open raises an event too, and it is neither a pipe nor
        # in the folder, so it passes straight through the hook above.
        self.seen.append(what)
        if self.log:
            try:
                with io.open(self.log, "a", encoding="utf-8") as fh:
                    fh.write(what + "\n")
            except OSError:
                pass

    def for_child(self, env, scratch):
        """`env` for a child that is guarded too, with the same folder and pipes.
        Whatever it records is read back by touched()."""
        boot = os.path.join(scratch, "live-revit-guard")
        os.makedirs(boot, exist_ok=True)
        with io.open(os.path.join(boot, "sitecustomize.py"), "w", encoding="utf-8") as fh:
            fh.write(SITECUSTOMIZE)
        log = os.path.join(scratch, "live-revit-guard.log")
        io.open(log, "w", encoding="utf-8").close()
        self._child_logs.append(log)
        child = dict(env)
        child[CHILD_SPEC] = json.dumps({"module": os.path.abspath(__file__),
                                        "folder": self.folder,
                                        "pipes": sorted(self.pipes),
                                        "log": log})
        child["PYTHONPATH"] = os.pathsep.join(
            [boot] + ([child["PYTHONPATH"]] if child.get("PYTHONPATH") else []))
        return child

    def touched(self):
        """Every touch recorded, in this process and in each guarded child."""
        found = list(self.seen)
        for log in self._child_logs:
            try:
                with io.open(log, encoding="utf-8") as fh:
                    found.extend(line.rstrip("\n") for line in fh if line.strip())
            except OSError:
                found.append("the child's guard log %s could not be read" % log)
        return found


def summary(touched, most=3):
    """One line: how many of each kind, and the first few verbatim."""
    if not touched:
        return "nothing"
    counts = {}
    for what in touched:
        kind = what.split(" ", 1)[0]
        counts[kind] = counts.get(kind, 0) + 1
    shown = []
    for what in touched:
        if what not in shown:
            shown.append(what)
        if len(shown) == most:
            break
    return "%s - e.g. %s" % (", ".join("%s x%d" % (k, n) for k, n in sorted(counts.items())),
                             "; ".join(shown))
