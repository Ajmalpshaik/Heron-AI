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
    OUTCOMES, _outcome,     how a call ended in ONE FIXED WORD, the line a
    _sent                   door's reply carries it on, and the answer for a
                            door that breaks after its request has left (row
                            5b-1159)
    RUNNING                 what this process loaded, against the disk now
    LIBRARY_ONLY            off Windows, started as the server (D-132) -
                            said by the entry file, through started()

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
from any function defined here. And there is ONE LIBRARY_ONLY, this one: the
entry file binds no copy, and reads it as an attribute of this module.

THE SERVER OBJECT IS MADE THE FIRST TIME IT IS ASKED FOR, not as this module
loads (`__getattr__`, at the foot). Its instructions and which tools it
registers both turn on LIBRARY_ONLY, and only the entry file knows that - so
the entry file says it first, with started(), and asks for `server` after.
"""

import functools
import inspect
import os
import sys
import threading
import time
import traceback

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
import heron_failure as failures                           # noqa: E402
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
# THE ENTRY FILE SAYS SO, AND NOTHING HERE WORKS IT OUT. Only that file knows
# it was run - its own `__name__ == "__main__"`, the fact this rule was
# written on - so it hands the fact over, once, with started(), before the
# server object is made. Until then this is False: every tool, which is what a
# suite or a tool that loads one file of the server alone is served.
#
# FOR A DAY THIS MODULE GUESSED, AND THE GUESS FAILED OPEN. When it was cut out
# of the entry file on 2026-11-11 it asked whether
# `sys.modules["__main__"].__file__` was the entry file - a question about how
# Python was started, not about this file having been run. Run as `__main__` by
# `exec(compile(...))` or by `runpy.run_module`, on Linux, the answer was no:
# fifty-five tools offered, thirty-four of them revit_, the chat never told
# there is no Revit here, and the runner note written. No shipped launcher
# starts it that way, and D-132 may still not depend on which one does. Found
# by the review of FRAGMENT-ISSUES row 5b-1555, before it was merged.
LIBRARY_ONLY = False


def library_only(run_as_main, system=None):
    """
    D-132's rule and nothing else: the library alone when the entry file was
    RUN (`run_as_main` - its own `__name__ == "__main__"`) on any system but
    Windows. `system` is os.name; a suite passes it to ask about another
    system than the one it runs on.
    """
    return bool(run_as_main) and (os.name if system is None else system) != "nt"


def started(run_as_main):
    """
    THE ENTRY FILE'S ONE CALL, made before it asks for the server object:
    whether it was run. Sets LIBRARY_ONLY, and hands it back.

    AFTER THE SERVER OBJECT IS MADE THE ANSWER MAY NOT CHANGE. Its instructions
    are written and its first tools registered by what LIBRARY_ONLY was then,
    so a different answer now would leave a server that is half one thing -
    and off Windows that half could be every revit_ tool. That is refused,
    loudly, and the process does not start: it is also what stops the entry
    file being loaded a second time under its own name while it runs.
    """
    global LIBRARY_ONLY
    said = library_only(run_as_main)
    if "server" in globals() and said != LIBRARY_ONLY:
        raise RuntimeError(
            "Heron's MCP server was already made %s when its entry file said it "
            "is to be made %s. Nothing is served. Start it with:  python "
            "mcp/server/heron_mcp_server.py"
            % tuple("with the library alone" if alone else "with every tool"
                    for alone in (LIBRARY_ONLY, said)))
    LIBRARY_ONLY = said
    return said

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

    async def call_tool(self, name, *args, **kwargs):
        """
        The SDK's call, and for a tool heron_tools.STATES_OUTCOME lists the
        word its reply states, as `outcome` in the STRUCTURED content beside
        the `result` the SDK already sends there (row 5b-1159) - for a host
        that reads structure rather than text.

        READ OFF THE REPLY'S OWN LINE (`outcome_of`), never worked out again,
        so the two cannot disagree. NOT DECLARED in the tool's output schema:
        the SDK writes that schema from the `-> str` every tool returns, and
        declaring a second key would mean a different return type and a
        different listing for a host. The schema it lists - `result`,
        required, and nothing forbidding another key - is still met.

        NEVER `isError`. The SDK sets that itself, for a tool that raised. A
        refusal is Heron working; and the MCP schema says of the flag only
        that it lets the model "see that an error occurred and self-correct"
        - and trying again is the one thing a chat must not be invited to do
        with an outcome nobody knows. What each host does with the flag is
        not something this file can show (row 5b-1561).

        A result this does not recognise is handed on untouched: the line in
        the text is the outcome, and this is a copy of it.
        """
        result = await super(_Labelled, self).call_tool(name, *args, **kwargs)
        if name not in tools.STATES_OUTCOME:
            return result
        try:
            return _structured_outcome(result)
        except Exception:                            # noqa: BLE001 - never cost a tool its answer
            return result


def _structured_outcome(result):
    """
    A door's result with `outcome` beside `result` in its structured content,
    in whichever shape this SDK hands one back: 2.x a result object carrying
    `structured_content`, a later 1.x a pair - the content blocks, then the
    structured dictionary - or an object carrying `structuredContent`. Any
    other shape, and a reply that states no word, comes back as it was.
    """
    if (isinstance(result, tuple) and len(result) == 2
            and isinstance(result[1], dict)):
        word = outcome_of(result[1].get("result"))
        return (result[0], dict(result[1], outcome=word)) if word else result
    for name in ("structured_content", "structuredContent"):
        structured = getattr(result, name, None)
        if isinstance(structured, dict):
            word = outcome_of(structured.get("result"))
            if word:
                setattr(result, name, dict(structured, outcome=word))
            break
    return result


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


# HOW A CALL ENDED, IN ONE FIXED WORD - FRAGMENT-ISSUES row 5b-1159.
#
# Every reply was prose. How a call ended WAS worked out - `_ended` below,
# heron_failure's five outcomes, the add-in's own `verdictKind` - and shown to
# the Companion's activity list alone, so a chat told "kept" from "refused"
# from "nobody knows" by reading sentences. It got that wrong: a refusal read
# as success, and "never ran" said of an answer that was lost.
#
# So the tools heron_tools.STATES_OUTCOME lists say it, on a line of their
# reply that is always written the same way:
#
#     outcome: REFUSED - Heron or Revit looked at what was asked and said no; ...
#
# ONE VOCABULARY, THIS TABLE, and nothing else decides a word. Each row is
# the word, what its line goes on to say, and what it means.
#
# EVERY LINE STANDS ALONE. What follows the word is its whole meaning and what
# to do next, in words that lean on no sentence of the reply: a chat is told
# to go by this line and not by the wording around it (host.chat), and some
# replies say nothing more - a change that came back `ok` with no verdict
# says only that it ran.
#
# A WORD NEVER SAYS MORE THAN THE CODE THAT RETURNED KNEW. That is the whole
# rule, and it is why:
#   - KEPT is said only of a reply whose own `verdictKind` is "kept" and whose
#     other fields do not contradict it. Never of an `ok` that names no
#     verdict, never while Revit is still asking the modeller Keep or Discard
#     (that is STILL RUNNING), never of a Discard. And it claims the
#     transaction and nothing else: a fragment that found nothing to change
#     is KEPT too, so the line says "what, if anything, it changed" and
#     promises no undo step - told to press Ctrl+Z after a run that wrote
#     nothing, a modeller undoes what they did before it.
#   - ROLLED BACK is said only by a door that opens a transaction group, and
#     says what is known: Heron asked Revit to roll the whole job back. An
#     error reply carries no field for whether that held (row 5b-1559: C#,
#     and a real Revit), so the line does not say it did, and does not send
#     the chat to the reply to find out - a reply whose rollback held says
#     nothing about it at all.
#   - On revit_read and revit_show nothing is rolled back: no transaction is
#     open. The same failure there - the code threw, Heron's clock or a Revit
#     dialog stopped it - is NOT ANSWERED for a read, and OUTCOME UNKNOWN for
#     a show, whose selection or view may stand as the run left it.
#   - Every failure code heron_failure has no row for is OUTCOME UNKNOWN, at
#     every door, and so is nothing coming back at all (`_failure_outcome`).
#   - A door that returns without saying how it ended is OUTCOME UNKNOWN, and
#     so is one that breaks after its request has left (`_after_it_left`).
#     The line fails closed: an unsaid ending is never guessed to have worked.
#
# REFUSED OR NOT RUN: REFUSED when Heron or Revit looked at what was asked and
# said no to it; NOT RUN when nothing said no - there was no capability by
# that name or those words, no Revit session to send it to, or Revit could not
# take it just then. NOT RUN, and not "not sent": some of it was sent - Revit
# was busy, or another chat held the model - and the add-in answered before
# anything ran.
KEPT = "KEPT"
ANSWERED = "ANSWERED"
NOT_ANSWERED = "NOT ANSWERED"
REFUSED = "REFUSED"
NOT_RUN = "NOT RUN"
ROLLED_BACK = "ROLLED BACK"
STILL_RUNNING = "STILL RUNNING"
UNKNOWN = "OUTCOME UNKNOWN"

OUTCOMES = (
    (KEPT,
     "Revit kept this call's transaction; the result lines say what, if "
     "anything, it changed",
     "A change ran and Revit kept its transaction - the add-in's own verdict, "
     "with nothing beside it saying otherwise. Not a claim that anything "
     "changed: the result lines are that."),
    (ANSWERED,
     "it ran, and what follows is its answer; this door keeps nothing in the "
     "model",
     "revit_read or revit_show ran to its end and answered. Neither opens a "
     "transaction, so Heron kept nothing."),
    (NOT_ANSWERED,
     "it stopped before it answered, and Heron had no transaction open to keep "
     "anything; fix what stopped it before asking again",
     "revit_read only: its code threw, or Heron's clock or a Revit dialog "
     "stopped it. Nothing is rolled back, because nothing was open - which is "
     "why this is not ROLLED BACK."),
    (REFUSED,
     "Heron or Revit looked at what was asked and said no; do what the reply "
     "says or tell the modeller, and never get round it another way",
     "A switch that is off, a capability that is not proved, the wrong door, "
     "the wrong model, a card Heron cannot read - or an answer from another "
     "model, which Heron would not show."),
    (NOT_RUN,
     "nothing ran in Revit and nothing needs undoing; choose or fix what the "
     "reply names, then ask again",
     "REFUSED is a no to what was asked; NOT RUN is when nothing said no - "
     "there was no capability by that name or those words, no Revit session to "
     "send it to, Heron's library could not be read, or Revit could not take "
     "it just then."),
    (ROLLED_BACK,
     "Heron did not keep it: it asked Revit to roll the whole job back; send "
     "it again only once what stopped it is fixed, or the modeller asks",
     "revit_change only: the job failed part way, Heron stopped it, or the "
     "modeller pressed Discard. What is known is that the rollback was asked "
     "for - not, from an error reply, that it held."),
    (STILL_RUNNING,
     "do not send it again; no id follows a run yet, so ask the modeller what "
     "Revit shows",
     "Revit took it and has not finished, or is still asking the modeller. "
     "Sending it again is how one job is done twice."),
    (UNKNOWN,
     "Heron cannot tell what this did in Revit; look at the model first, and "
     "do not send a change again",
     "The answer was lost, too large or unreadable; the failure is one Heron "
     "has no row for; a rollback was not confirmed, or a verdict contradicts "
     "itself; it ran in a model this chat does not work on; a show stopped "
     "part way; or the door broke, or did not say how it ended."),
)

#: How every outcome line starts. One spelling, here.
OUTCOME_LINE = "outcome: "
_OUTCOME_TAIL = " - "
_WORDS = tuple(row[0] for row in OUTCOMES)
_SAYS_NEXT = dict((row[0], row[1]) for row in OUTCOMES)

# heron_failure's five outcomes, each to its word - a classification that is
# already made, read here and never made a second time.
_BY_FAILURE = {
    failures.NEVER_RAN: NOT_RUN,
    failures.REFUSED: REFUSED,
    failures.RUNNING: STILL_RUNNING,
    failures.ROLLED_BACK: ROLLED_BACK,
    failures.UNKNOWN: UNKNOWN,
}

# The two labels `_ended` is given that are a word by themselves. "ok" is not
# one - it is KEPT at one door and ANSWERED at another, and the door says
# which - and "failed" is four different endings.
_BY_LABEL = {"refused": REFUSED, "handed_back": NOT_RUN}


def outcome_line(word):
    """The line a reply carries `word` on: `outcome: WORD - ` and what the
    word means and what to do next, whole."""
    said = _SAYS_NEXT.get(word)
    return OUTCOME_LINE + word + (_OUTCOME_TAIL + said if said else "")


def outcome_of(text):
    """
    The word a door's reply states, or None - read off the one place the
    line is written, the reply's second line. For a host, and for the
    structured content `_Labelled.call_tool` sends: one line written, one
    line read, so the two can never disagree.
    """
    lines = text.split(NEWLINE, 2) if isinstance(text, str) else []
    if len(lines) < 2 or not lines[1].startswith(OUTCOME_LINE):
        return None
    word = lines[1][len(OUTCOME_LINE):].split(_OUTCOME_TAIL, 1)[0]
    return word if word in _WORDS else None


def _outcome(word, answer=None):
    """
    THE WORD FOR HOW THIS CALL ENDED, said by the door on the line before
    the `_ended` it goes with, wherever that label is not a word by itself -
    and the answer handed back unchanged, as `_ended` hands it.

    It changes nothing `_ended` says: the activity list's label is its own.
    """
    _ctx.how = word
    return answer


def _sent():
    """
    Said by a door on the line before its request leaves for Revit. From
    there on a fault in Heron's own code is no longer "nothing happened":
    `_after_it_left` reads this, and answers OUTCOME UNKNOWN rather than let
    the exception reach the host.
    """
    _ctx.sent = True


def _failure_outcome(failure):
    """
    A classified failure's word: heron_failure's own outcome, through
    `_BY_FAILURE` - for a code heron_failure HAS A ROW FOR.

    ANYTHING IT DOES NOT POSITIVELY RECOGNISE IS OUTCOME UNKNOWN, at every
    door. `analyse` files an unrecognised code on a read under "refused",
    which is its answer to "may this be asked again" and not a statement
    that nothing ran: `handler_failed` is left out of its table on purpose,
    and the bridge raises it after the job may have run. A reply with no
    `ok` and no code at all goes the same way.

    AND SO IS NOTHING COMING BACK AT ALL. `no_reply` is what `analyse` makes
    of that, and on a read it files it under never-ran beside the reason
    "Heron cannot tell whether Revit was reached" (row 5b-1560). It has no
    row either, so the one rule covers it: not known - for a read as for a
    change.
    """
    if not failures.recognised(failure.code):
        return UNKNOWN
    return _BY_FAILURE.get(failure.outcome, UNKNOWN)


def _change_outcome(reply):
    """
    How a change the add-in answered `ok` ended - from the add-in's own
    `verdictKind`, the field heron_run_records already prints the verdict by.
    Never from `ok` alone, and never from the verdict's sentence.

    KEPT only when the add-in says "kept", and ROLLED BACK only when it says
    the rollback HELD (`rolledBack`, which RevitFragment.WithVerdict sets
    only when Revit reported the group rolled back and nothing the run added
    still stands) - the modeller's Discard is one.

    AND ONLY WHEN THE FIELDS BESIDE THE VERDICT AGREE WITH IT. The add-in
    writes `applied`, `rolledBack`, `asked` and `kept` in the same reply, and
    today they cannot disagree with `verdictKind`. A reply in which they do -
    kept beside a rollback, kept beside a modeller who was asked and did not
    keep, discarded beside `applied` - is from an add-in this server was not
    written against, and one field is wrong: which one is not knowable here,
    so it is not known. The same goes for a rollback that did not hold or
    was not confirmed, and an `ok` old enough to name no verdict at all.
    """
    kind = reply.get("verdictKind")
    declined = bool(reply.get("asked")) and not reply.get("kept")
    if kind == "kept":
        if (reply.get("rolledBack") is True or reply.get("applied") is False
                or declined):
            return UNKNOWN
        return KEPT
    if kind in ("rolledBack", "discarded") and reply.get("rolledBack") is True:
        if reply.get("applied") is True or reply.get("kept") is True:
            return UNKNOWN
        return ROLLED_BACK
    return UNKNOWN


def _said_outcome(tool, reply, label, how):
    """
    The reply with its outcome line, for a tool heron_tools.STATES_OUTCOME
    lists - and untouched for every other tool, and for an answer that is
    not text.

    THE WORD is the one the door said (`_outcome`, or `_failure_ended` for a
    classified failure); with none said, the label when the label is a word
    by itself (`_BY_LABEL`); and otherwise OUTCOME UNKNOWN.

    ROLLED BACK IS PRINTED ONLY FOR A TOOL THAT OPENS SOMETHING TO ROLL BACK
    (heron_tools.writes). heron_failure calls `fragment_threw`,
    `ran_too_long` and `dialog_refused` rolled back whichever operation
    they came from, and on the read and show paths nothing is: no
    transaction is open (RevitFragment, the `!writing` branch). There the
    word is NOT ANSWERED for a tool that only reads, and OUTCOME UNKNOWN for
    one that acts on the screen - a selection or a view the run changed
    before it stopped is still changed.

    THE SECOND LINE, directly under the reply's opening sentence. Not the
    first and not the last, because both ends of a door's reply are already
    held by tests/test_mcp_serves.py: a kept change to a model never saved
    OPENS with the NEVER SAVED warning, and a hand-back CLOSES on the
    sentence that names the door to call again. Nothing a door wrote is
    moved or dropped: take the line out and the reply is what it was
    (tests/test_outcome_line.py).
    """
    if tool not in tools.STATES_OUTCOME or not isinstance(reply, str):
        return reply
    word = how if how in _WORDS else _BY_LABEL.get(label, UNKNOWN)
    if word == ROLLED_BACK and not _changes_model(tool):
        word = NOT_ANSWERED if tools.risk_of(tool) <= tools.ANALYZE else UNKNOWN
    if not reply.strip():
        return outcome_line(word)
    first, newline, rest = reply.partition(NEWLINE)
    return first + NEWLINE + outcome_line(word) + newline + rest


def _after_it_left(tool, error):
    """
    The answer for a door whose own code raised AFTER its request had left
    for Revit - or None, and the exception goes on to the host as before.

    AN EXCEPTION OUT OF A DOOR IS REPORTED BY THE SDK AS "Error executing
    tool revit_change" AND NOTHING ELSE. Before the request leaves that is
    the truth - the tool failed and Revit was not asked. After it, the same
    sentence says nothing of a change Revit may have made and kept, and a
    tool that "failed" is what a chat sends again. So from `_sent()` on the
    door answers for itself: OUTCOME UNKNOWN, the sentence that sends a
    person to the model, and no exception - which also means no `isError`.

    WHAT RAISED IS RECORDED AND NOT REPEATED. The kind of fault is named in
    the reply; its text is not, for the reason the SDK keeps a crash's text
    on the server - it can quote what the model sent. The traceback goes to
    this process's stderr, which is the host's log for the server, and the
    activity list has the call as failed (`_recorded` notes it).
    """
    if tool not in tools.STATES_OUTCOME or not getattr(_ctx, "sent", False):
        return None
    try:
        sys.stderr.write("Heron: %s raised after its request had left for Revit, "
                         "and answered OUTCOME UNKNOWN:%s" % (tool, NEWLINE))
        traceback.print_exception(type(error), error, error.__traceback__,
                                  file=sys.stderr)
    except Exception:                                # noqa: BLE001 - the answer matters more
        pass
    _outcome(UNKNOWN)
    return ("%s broke in Heron's own code (%s) after its request had gone to "
            "Revit, so Heron cannot tell you what Revit did with it.%s"
            "Look at the model before anything is sent again - repeating a "
            "change could do the work twice."
            % (tool, type(error).__name__, NEWLINE))


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
    asked, which the list calls failed.

    AND THE WORD FOR THE OUTCOME LINE, which keeps apart the four endings
    that label folds into one (`_failure_outcome`, row 5b-1159)."""
    _outcome(_failure_outcome(failure))
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

    AND WHERE A DOOR'S REPLY GETS ITS OUTCOME LINE (`_said_outcome`, row
    5b-1159) - here, once, so the body of a door is the same text it always
    was, and the Companion's page and Revit's Heron panel, which run that
    body through `_through`, read exactly what they read before.
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
        kept_how, _ctx.how = getattr(_ctx, "how", None), None
        if outer:
            # NOTHING HAS LEFT FOR REVIT YET, in this call (`_sent`).
            _ctx.sent = False
        started, clock = time.strftime("%H:%M:%S"), time.time()
        broke = False
        try:
            reply = _refused_for_old_code(fn.__name__)
            if reply is None:
                reply = fn(*a, **k)
        except Exception as error:
            if outer:
                _note(fn.__name__, started, time.time() - clock, error=error)
            # A DOOR THAT BREAKS AFTER ITS REQUEST HAS LEFT STILL ANSWERS
            # (`_after_it_left`); every other exception goes on, as before.
            reply = _after_it_left(fn.__name__, error) if outer else None
            if reply is None:
                raise
            broke = True
        finally:
            said, _ctx.outcome = getattr(_ctx, "outcome", None), kept
            how, _ctx.how = getattr(_ctx, "how", None), kept_how
            if outer:
                _depth.busy = False
        if outer and not broke:
            _note(fn.__name__, started, time.time() - clock, reply=reply, outcome=said)
        if outer:
            # AFTER THE NOTE, so the activity list still shows the reply's own
            # first sentence; and only for the call the host made - a door
            # run inside another tool answers that tool, not the chat.
            reply = _said_outcome(fn.__name__, reply, said, how)
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


def __getattr__(name):
    """
    `server`, made the first time anything asks this module for it - and only
    once: it is this module's own name from then on, and this is not asked
    again. Not made as the module loads, because what it is told and which
    tools it registers turn on LIBRARY_ONLY, which the entry file sets first
    (started, above). A file of the server loaded alone asks for it without
    that call, and is served every tool, as an import always was.
    """
    if name != "server":
        raise AttributeError("module %r has no attribute %r" % (__name__, name))
    global server
    server = _Labelled("heron", instructions=_host_instructions())
    return server


# One chat, one Revit. Lives as long as this server does, which is as long as
# the chat does - the correct scope for a binding (docs/25).
binding = SessionBinding()

# One chat, one MODEL - the other half of the same question (Golden Rule 20),
# and the approval the user has actually been shown. Same scope, same reason.
pinned = DocumentPin()
approval = PendingApproval()
