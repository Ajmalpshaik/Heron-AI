#!/usr/bin/env python3
# Heron-Agent:  HERON-MCP-VER-007
# Heron-Step:   5
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
Is this process still running the Python that is on disk?

THE TRAP THIS CLOSES
--------------------
The MCP server is one long-running process per chat. A git pull, a merge or a
redeploy changes its files and changes nothing it is running: Python read them
when the chat started. NEEDS-CHECKING B8 met it on 2026-09-07. The write path
had been fixed - the approval travels as `approvalToken` now - and
revit_apply_move kept answering "There is nothing waiting to be approved",
because the server had not been restarted and still read `token`. Redeploying
the add-in and restarting Revit does not reload this side. B8 was proved from a
fresh process instead, and group D warns about it by hand before D1
(docs/needs-checking/group-b.md, group-d.md). Nothing said it from inside the
chat.

WHAT IT WATCHES
---------------
Every module this process has loaded from Heron's own Python folders - the
server, the modules beside it, the bridge client, every brain module imported
so far - each from the first moment it is seen loaded. A brain module imported
lazily by the tenth call is fingerprinted from then, which is when its code was
read. A module not yet imported is not watched: it will be read from disk as it
is when it is first needed.

HOW, AND WHY IT IS CHEAP
------------------------
At first sight: the file's size and mtime, and a SHA-256 of its bytes. On every
look after that: one stat per file. Only a stat that moved costs a read, and
then the BYTES decide - so a checkout that rewrites a file with the same
content, or a touch, is not a change, and the warning never cries wolf. A file
whose bytes differ is a change until they come back; a file that is gone is a
change.

WHAT IT CANNOT SEE: a rewrite to the same size inside the same tick of the file
system's clock as the first look. A pull lands minutes or days after a chat
starts, not inside that tick.

WHAT THE SERVER DOES WITH THE ANSWER - THE DECISION, written down here because
this is where it is enforced (heron_mcp_server._recorded and _through):

    every reply        carries SAID, one line, while anything has changed
    a read             is never refused for it - it answers, and says SAID
    a tool that        is REFUSED, and sends nothing to Revit. Which tools
    changes the model  is heron_tools.writes(), never a list typed here, so
                       revit_apply_move and revit_change today, and any
                       write added later

An approve is refused because the code that would carry it is not the code on
disk, and the change on disk may be the very fix the modeller is relying on -
B8's was a write-path fix, and E12's was a write landing in the wrong model.
The refusal costs one restart and one preview shown again; a write through the
old code can cost the model. A preview (revit_preview_move) changes nothing, so
it answers and says SAID, and the approval it offers is refused until the chat
restarts.
"""

import hashlib
import os
import sys
import threading

# What the modeller is told. One line each, what happened and then what to do
# (the revit-addin-conventions wording rule), and the refusal says nothing was
# sent, in as many words.
SAID = ("Heron's code changed since this chat started, so this chat is still running "
        "the old code - close and reopen the chat to load it. Until then Heron will "
        "not change the model.")

REFUSED = ("Heron's code changed since this chat started, and this chat is still "
           "running the old code, so Heron will not change the model with it. Nothing "
           "has been sent to Revit. Close and reopen the chat, then ask for the change "
           "again.")

# Heron's Python lives in these folders of the checkout, and nothing else does.
# Named rather than "everything under the checkout" so that a virtual
# environment made inside it is never watched file by file.
FOLDERS = ("mcp", "brain", "tools")


def _digest(path):
    """SHA-256 of a file's bytes, or None when it cannot be read."""
    try:
        with open(path, "rb") as fh:
            return hashlib.sha256(fh.read()).hexdigest()
    except (OSError, ValueError):
        return None


def _file_of(module):
    """A module's __file__, read past any __getattribute__ - so a module
    imported lazily is not loaded by being looked at - or None."""
    try:
        return object.__getattribute__(module, "__dict__").get("__file__")
    except (AttributeError, TypeError):
        return None


class _Seen(object):
    """One file as it was first seen, and the last stat taken of it."""

    __slots__ = ("digest", "size", "mtime_ns", "moved")

    def __init__(self, path):
        self.size, self.mtime_ns = _stat(path)
        self.digest = _digest(path)
        self.moved = False


def _stat(path):
    try:
        st = os.stat(path)
        return st.st_size, st.st_mtime_ns
    except (OSError, ValueError):          # ValueError: a path no file can have
        return None, None


class RunningCode(object):
    """The Python this process has loaded from `roots`, against the disk now."""

    def __init__(self, roots, modules=None):
        self._roots = tuple(os.path.normcase(os.path.abspath(r)) + os.sep for r in roots)
        self._modules = sys.modules if modules is None else modules
        self._seen = {}            # absolute path -> _Seen
        self._looked = set()       # every __file__ string already placed
        self._lock = threading.Lock()
        self.changed()

    @classmethod
    def of_checkout(cls, root, modules=None):
        """A watch over Heron's own Python folders in the checkout at `root`."""
        return cls([os.path.join(root, folder) for folder in FOLDERS], modules)

    def watched(self):
        """Every file being watched, sorted."""
        with self._lock:
            self._adopt()
            return sorted(self._seen)

    def changed(self):
        """The watched files whose bytes are not the ones this process loaded,
        sorted - an empty list when nothing has changed."""
        with self._lock:
            self._adopt()
            return sorted(path for path, seen in self._seen.items()
                          if self._moved(path, seen))

    def _adopt(self):
        """Take a first look at every module loaded since the last one."""
        for module in list(self._modules.values()):
            name = _file_of(module)
            if not isinstance(name, str) or name in self._looked:
                continue
            self._looked.add(name)
            path = os.path.normcase(os.path.abspath(name))
            if (path.endswith(".py") and path.startswith(self._roots)
                    and path not in self._seen):
                self._seen[path] = _Seen(path)

    @staticmethod
    def _moved(path, seen):
        size, mtime_ns = _stat(path)
        if size is None:
            return True                    # gone: not the code that was loaded
        if (size, mtime_ns) != (seen.size, seen.mtime_ns):
            seen.size, seen.mtime_ns = size, mtime_ns
            seen.moved = seen.digest is None or _digest(path) != seen.digest
        return seen.moved
