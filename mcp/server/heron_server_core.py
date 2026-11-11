#!/usr/bin/env python3
# Heron-Agent:  HERON-MCP-SRV-001, HERON-MCP-VER-007
# Heron-Step:   5
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
What every file of Heron's MCP server shares: the server object, this chat's
state, and what every tool call passes through.

NOT RUN, AND NOT WHERE A TOOL LIVES. The file a host starts is
heron_mcp_server.py, and the tools are `@server.tool()` functions in the
files heron_tools.SERVER_FILES lists. This one holds what more than one of
those files needs, so that there is exactly ONE of each in a process:

    server                  the SDK's server, labelled from heron_tools.TOOLS
    binding, pinned,        one chat, one Revit, one model, and the approval
    approval                the user has actually been shown
    _revit_lock             one conversation with Revit at a time
    _recorded, _ended,      the activity line, how a call ended, and the one
    _ended_by_reply         place a chat hears that its code is old
    RUNNING                 what this process loaded, against the disk now
    LIBRARY_ONLY            off Windows, started as the server (D-132)

WHY IT IS A FILE OF ITS OWN. Until 2026-11-11 all of this sat at the top of
heron_mcp_server.py, which held every tool as well - over eight thousand
lines in one file that day, so every change to any tool collided with every
other (FRAGMENT-ISSUES row 5b-1555). A tool cannot move to a second file and
import these from the entry file: the entry file runs as `__main__`, so
`import heron_mcp_server` from a module it loads would execute it a SECOND
time, under its other name - a second server object, a second lock, a second
pin, and every tool registered twice. So what is shared is defined here, the
entry file and every tool file import it, and this file imports neither.

    heron_mcp_server (entry)   imports   heron_server_core, heron_inventory
    heron_inventory            imports   heron_server_core
    heron_server_core          imports   no file of the server

A NAME REBOUND ON THE ENTRY MODULE DOES NOT REACH HERE. `_old_code` reads this
module's RUNNING and `_Labelled` this module's LIBRARY_ONLY, so a suite that
stands one in sets it on this module - sys.modules[fn.__module__] finds it
from any function defined here.
"""

import functools
import inspect
import os
import sys
import threading
import time

# The bridge client is the layer below the server, and heron_session imports
# it. The entry file has already put it on the path when this is loaded
# through it; a suite that loads a tool file alone has not.
_CLIENT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "client")
if _CLIENT not in sys.path:
    sys.path.insert(0, _CLIENT)

from heron_session import SessionBinding                   # noqa: E402
from heron_write import DocumentPin, PendingApproval       # noqa: E402
from heron_failure import analyse                          # noqa: E402
from heron_failure import REFUSED as FAILURE_REFUSED       # noqa: E402
import heron_tools as tools                                # noqa: E402
import heron_brain as brain                                # noqa: E402
import heron_running_code as running_code                  # noqa: E402

# WHAT THIS CHAT IS RUNNING, fingerprinted now - before a pull can land under
# it. A git pull changes the files and not this process, and on 2026-09-07 that
# had revit_apply_move answering from code a fix had already replaced
# (NEEDS-CHECKING B8). heron_running_code says how, and what is decided when
# the two differ: every reply says so, and nothing changes the model.
RUNNING = running_code.RunningCode.of_checkout(
    os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

# WHAT A PERSON IS TOLD TO INSTALL, when the SDK is missing or has no image
# type: requirements-lock.txt's own line on Windows, the unpinned one elsewhere
# (rows 5b-944, 5b-979). brain/heron_install.py says it for every part of
# Heron; heron_brain, imported above, has put brain/ on the path.
from heron_install import install_line                     # noqa: E402


# THE SDK RENAMED THIS CLASS, AND AN UNPINNED INSTALL GETS THE NEW ONE.
# `pip install --user mcp` - which is what tools/HeronRevit.ps1 told a user to
# run until 2026-10-17 - resolved to 1.x when this server was written and
# resolves to 2.x now.
# In 2.x `mcp.server.fastmcp` does not exist: FastMCP was renamed MCPServer.
# The import is the ONLY thing that changed for this file. Measured 2026-08-31
# against both SDKs installed side by side: `MCPServer("heron")`, the
# `@server.tool()` decorator and `server.run()` behave identically, and all ten
# tools are served with the same names, descriptions and argument schemas.
#
# So the class is looked up rather than assumed, newest first. This is D-05's
# rule about Revit releases applied to a Python dependency: an unlisted version
# must be a loud failure, never a silent one - which is why the final except
# re-raises with the install line rather than leaving an ImportError that names
# a module the user never typed.
#
# Context comes from the same module as the server class, in both SDKs, so it
# is looked up beside it rather than on its own.
try:                                                       # noqa: E402
    from mcp.server.mcpserver import MCPServer as _Server   # SDK 2.x
    from mcp.server.mcpserver import Context as _Context
except ImportError:
    try:
        from mcp.server.fastmcp import FastMCP as _Server   # SDK 1.x
        from mcp.server.fastmcp import Context as _Context
    except ImportError as exc:
        raise ImportError(
            "Heron's MCP server needs the MCP SDK, and this one has neither "
            "mcp.server.mcpserver (2.x) nor mcp.server.fastmcp (1.x): %s\n"
            "Install it, in the Heron folder, with:  %s"
            % (exc, install_line("pip install --user mcp"))
        )

NEWLINE = chr(10)

# THE SAFETY LABELS NEED A TYPE THE SDK ONLY GAINED PART-WAY THROUGH 1.x, so it
# is looked up the same way the server class is. Without it every tool is
# served unlabelled - which is SAFE, because a host told nothing assumes the
# worst of a tool (not read-only, destructive, not idempotent) - and so an old
# SDK costs the labels and never the server.
try:                                                       # noqa: E402
    from mcp.types import ToolAnnotations as _Annotations
except ImportError:
    _Annotations = None

# A PICTURE GOES BACK AS THE SDK'S OWN IMAGE HELPER, and it moved with the
# server class - so it is looked up the same way, newest first (D-45). Both
# SDKs carry it beside the class above; None means an SDK without one, and
# then revit_view_image says so rather than the server failing to start.
try:                                                       # noqa: E402
    from mcp.server.mcpserver import Image as _Image       # SDK 2.x
except ImportError:
    try:
        from mcp.server.fastmcp import Image as _Image     # SDK 1.x
    except ImportError:
        _Image = None

_TAKES_LABELS = (_Annotations is not None
                 and "annotations" in inspect.signature(_Server.tool).parameters)

# THE DESCRIPTION A HOST IS SERVED IS THE DOCSTRING CLEANED, ON EVERY PYTHON.
# The SDK takes `fn.__doc__` as it finds it, and Python 3.12 - the owner's -
# keeps every line of it indented four spaces where 3.13 strips them: measured
# 2026-10-17, 3,014 characters of indentation across the list, paid for in
# context and read by nobody. inspect.cleandoc is what ast.get_docstring
# returns, so heron_tools' --sizes reads off the source exactly what a host is
# handed (row 5b-491).
_TAKES_DESCRIPTION = "description" in inspect.signature(_Server.tool).parameters

# NO REVIT ON THIS MACHINE, AND THE LIBRARY ALONE (Q-67, D-132). The bridge is
# a Windows named pipe and Revit is Windows-only, so off Windows - a cloud
# session is the case - this server registers heron_tools.LIBRARY and nothing
# else. It exited 2 here until 2026-10-17, which took the library search out
# of every cloud session along with the Revit tools.
#
# Only when the entry file is RUN as the server: a test or a tool that IMPORTS
# it is served every tool on any system, exactly as before - test_mcp_serves
# calls them in-process on Linux, and heron_tools --sizes measures them all.
# On Windows this is False and nothing below changes.
#
# THE ENTRY FILE ASKED ITS OWN `__name__` UNTIL 2026-11-11, and this module
# cannot: the server object is made HERE, as this module loads, and both its
# instructions and which tools it registers turn on the answer - and here
# `__name__` is never "__main__". So the same fact is asked from the other
# side: whether the program Python was started on IS the entry file, the last
# of heron_tools.SERVER_FILES. Off Windows only; on Windows the question is
# never put (FRAGMENT-ISSUES row 5b-1555).
def _started_as_the_server():
    """Whether this process was started on the server's entry file - `python
    mcp/server/heron_mcp_server.py`, as .mcp.json starts it - and not on a
    suite, a tool or a runner that imports it."""
    started = getattr(sys.modules.get("__main__"), "__file__", None)
    if not started:
        return False
    try:
        return os.path.samefile(started, tools.server_files()[-1])
    except (OSError, ValueError):
        return False


LIBRARY_ONLY = os.name != "nt" and _started_as_the_server()

NO_REVIT_HERE = "No Revit on this machine (it is not Windows)"


class _Labelled(_Server):
    """
    The SDK's server with one difference: every `@server.tool()` is registered
    with the MCP safety labels its declared risk implies.

    WHY A SUBCLASS AND NOT A LABEL ON EACH DECORATOR. A label typed beside a
    tool is a second declaration of what the tool can do, and the first one -
    heron_tools.TOOLS - is the one the add-in's gate is checked against. Typed
    twice, the two drift; and the drift that matters is a write labelled
    read-only, which a host may call without asking. So the decorator every
    tool already carries stays exactly as it is, and the label is read off the
    registry at the moment the tool registers. tests/test_tool_registry.py
    holds the rule; tests/test_mcp_serves.py reads the labels back from a real
    SDK and compares them with the registry.

    The labels are advice to the host. The gate stays in the add-in.
    """

    def tool(self, *args, **kwargs):
        register = super(_Labelled, self).tool

        def decorate(fn):
            # A COPY, so one decorator applied twice cannot hand the second
            # tool the first one's label.
            options = dict(kwargs)
            name = (args[0] if args else None) or options.get("name") or fn.__name__
            if LIBRARY_ONLY and name not in tools.LIBRARY:
                # NOT REGISTERED, so the host never lists it - never listed
                # and then refused. Still callable from the server's own
                # code, as every tool is.
                return _recorded(fn)
            if _TAKES_LABELS and "annotations" not in options:
                options["annotations"] = _Annotations.model_validate(
                    tools.annotations(name))
            if _TAKES_DESCRIPTION and "description" not in options and fn.__doc__:
                options["description"] = inspect.cleandoc(fn.__doc__)
            return register(*args, **options)(_recorded(fn))

        return decorate

    async def list_tools(self, *args, **kwargs):
        """The SDK's list, with heron_tools.ALWAYS_LOADED marked to be loaded
        at session start rather than found by a search (see that list for
        why). The SDK's tool() takes no _meta in 1.x, so it is added here; a
        list that cannot be marked is served unmarked, which only costs the
        chat the search step it already had."""
        listed = await super(_Labelled, self).list_tools(*args, **kwargs)
        for tool in listed:
            if getattr(tool, "name", None) in tools.ALWAYS_LOADED:
                try:
                    tool.meta = dict(tool.meta or {}, **{"anthropic/alwaysLoad": True})
                except (AttributeError, TypeError, ValueError):
                    pass
        return listed


def _host_instructions():
    """
    What every chat is told before its first tool call - or, when it cannot be
    assembled, a sentence saying so in the same place.

    The Constitution's Enforcement table said its Articles were injected into
    agent instructions, and this server was built with none: the AI a modeller
    talks to was given no Article at all. The words come from the Instruction
    Registry (`host.chat`), with the Articles assembled from
    HERON_CONSTITUTION.md when the server starts - so an amendment reaches the
    next chat with nobody editing this file.

    A FAILURE IS SAID, NOT FATAL. A server that refused to start over its own
    instructions would take every Revit tool out of the host with it.

    OFF WINDOWS THE CHAT IS TOLD FIRST THAT THERE IS NO REVIT HERE (D-132), so
    a library that answers is never read as a Revit that is connected.
    """
    try:
        said = brain.host_instructions()
    except brain.BrainUnavailable as why:
        said = ("Heron could not give this chat its rules: %s\n"
                "They are in HERON_CONSTITUTION.md, and they bind this chat "
                "whether or not they arrived here." % why)
    if not LIBRARY_ONLY:
        return said
    return ("%s. Heron serves only its library here - %s - and no revit_ "
            "tool, because there is no Revit to reach. Anything that needs a "
            "model is done on the modeller's Windows PC.%s%s"
            % (NO_REVIT_HERE, ", ".join(tools.LIBRARY), NEWLINE + NEWLINE, said))


_depth = threading.local()

# ONE CONVERSATION WITH REVIT AT A TIME, from this process. Every tool call
# holds it (through _recorded) and so does the Companion's Apply, so a change
# from the page can never open a second pipe connection while the chat's
# request is in flight - which would displace it and lose its answer (docs/40
# section 4). Re-entrant, because a tool may call another.
_revit_lock = threading.RLock()

# THE COMPANION'S SIDE CHANNEL INTO THE TWO DOORS. The page's Apply runs
# revit_change itself, and a table read runs revit_read itself - their own
# bodies, unchanged, so every rule the suites read in them holds for the page
# too. What the page needs that the chat does not - the add-in's raw reply,
# and not to be offered its own change back as a new table - travels here,
# per thread, set only for the length of one call.
_ctx = threading.local()


def _ended(outcome, answer=None):
    """
    HOW THIS CALL ENDED, said by the code that knows - for the Companion's
    activity list - and the answer handed back unchanged, so a tool writes
    `return _ended("refused", text)` (row 5b-274).

    The list used to guess it from the answer's first line, and got two
    kinds of reply backwards: a words-path hand-back was labelled refused,
    and an add-in refusal whose first line said nothing of refusing was
    labelled OK. Say "ok", "refused", "failed" or "handed_back"
    (heron_companion.Activity.OUTCOMES); a call that says nothing is still
    guessed, as before. Display only: it never changes the answer.
    """
    _ctx.outcome = outcome
    return answer


def _failure_ended(failure):
    """A classified bridge failure's label: a refusal only when Revit or the
    add-in said no (heron_failure's REFUSED). Never ran, still running,
    rolled back and an unknown outcome all mean the work was not done as
    asked, which the list calls failed."""
    return "refused" if failure.outcome == FAILURE_REFUSED else "failed"


def _ended_by_reply(tool, reply):
    """
    HOW A SINGLE-PURPOSE TOOL'S CALL ENDED, said from the add-in's reply the
    moment it arrives (row 5b-389). revit_levels, revit_views and the rest
    send one operation and answer from it, so the reply is what knows: a
    good one is "ok", and a missing or failed one is labelled by
    heron_failure's own classification through _failure_ended, exactly as
    the three doors label theirs. `writes` is the tool's declared risk
    (heron_tools.writes), never a flag typed here.

    The lines after it still speak last: the pin's refusal is
    `return _ended("refused", wrong_model)`. Display only, like _ended - the
    answer each branch returns is untouched.
    """
    failure = analyse(reply, writes=tools.writes(tool))
    _ended("ok" if failure is None else _failure_ended(failure))


def _recorded(fn):
    """
    The same tool, and a line on the Companion's activity list (D-108,
    docs/40 section 7) - the tool's name, the first line of its answer, how
    it ended and how long it took.

    functools.wraps keeps the signature the SDK reads, which
    tests/test_mcp_serves.py checks against the real SDK. The answer and any
    exception pass through untouched, and a fault in the recording is
    dropped: the list is display, and must never cost a tool its answer. A
    tool called from inside another is not listed twice.

    AND THE ONE PLACE A CHAT HEARS THAT ITS CODE IS OLD. While RUNNING finds
    a file changed since it was loaded, a tool that changes the model is
    refused before its body runs, and every answer ends with one line saying
    to reopen the chat - a read still answers in full (heron_running_code has
    the decision). Nothing changed, nothing is added: a stat per file.
    """
    @functools.wraps(fn)
    def run(*a, **k):
        with _revit_lock:
            return _run(*a, **k)

    def _run(*a, **k):
        outer = not getattr(_depth, "busy", False)
        _depth.busy = True
        # HOW IT ENDED, AS THE TOOL SAID IT (_ended, row 5b-274). Emptied
        # before the call and put back after, so a tool called inside
        # another never speaks for the outer one's line.
        kept, _ctx.outcome = getattr(_ctx, "outcome", None), None
        started, clock = time.strftime("%H:%M:%S"), time.time()
        try:
            reply = _refused_for_old_code(fn.__name__)
            if reply is None:
                reply = fn(*a, **k)
        except Exception as error:
            if outer:
                _note(fn.__name__, started, time.time() - clock, error=error)
            raise
        finally:
            said, _ctx.outcome = getattr(_ctx, "outcome", None), kept
            if outer:
                _depth.busy = False
        if outer:
            _note(fn.__name__, started, time.time() - clock, reply=reply, outcome=said)
            reply = _said_old_code(reply)
        return reply
    return run


def _changes_model(tool):
    """heron_tools.writes, and a tool nobody declared is never harmless."""
    try:
        return tools.writes(tool)
    except tools.NotDeclared:
        return True


def _old_code():
    """What RUNNING finds changed - and nothing when the watch itself faults,
    which leaves a tool as it was before the watch existed rather than
    costing every tool its answer."""
    try:
        return RUNNING.changed()
    except Exception:                                # noqa: BLE001 - never cost a tool its answer
        return []


def _refused_for_old_code(tool):
    """The refusal, said as one, when `tool` would change the model and this
    process is not running the code on disk - else None."""
    if _changes_model(tool) and _old_code():
        return _ended("refused", running_code.REFUSED)
    return None


def _said_old_code(reply):
    """The reply with one line after it while this process is not running the
    code on disk, and untouched otherwise. A refusal for it already says so."""
    if not _old_code():
        return reply
    if isinstance(reply, str):
        if running_code.REFUSED in reply:
            return reply
        return reply + NEWLINE + NEWLINE + running_code.SAID
    if isinstance(reply, (list, tuple)):
        return list(reply) + [running_code.SAID]
    return reply


def _note(tool, started, seconds, reply=None, error=None, outcome=None):
    # A TOOL THAT ANSWERS WITH A PICTURE ANSWERS WITH A LIST - its words, then
    # the image (revit_view_image). The list shows the words, never the repr
    # of an image object.
    if isinstance(reply, (list, tuple)):
        reply = next((part for part in reply if isinstance(part, str)), "")
    try:
        _companion_module().ACTIVITY.record(tool, started, seconds, reply=reply, error=error,
                                            outcome=outcome)
    except Exception:                                # noqa: BLE001 - display only
        pass


def _companion_module():
    """mcp/companion, imported on first use - it is not on the server's path."""
    sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                    "..", "companion"))
    import heron_companion as companion_page
    return companion_page


server = _Labelled("heron", instructions=_host_instructions())

# One chat, one Revit. Lives as long as this server does, which is as long as
# the chat does - the correct scope for a binding (docs/25).
binding = SessionBinding()

# One chat, one MODEL - the other half of the same question (Golden Rule 20),
# and the approval the user has actually been shown. Same scope, same reason.
pinned = DocumentPin()
approval = PendingApproval()
