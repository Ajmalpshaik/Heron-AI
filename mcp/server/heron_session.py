#!/usr/bin/env python3
# Heron-Agent:  HERON-SES-BND-003, HERON-MCP-CMP-008
# Heron-Step:   5
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
One chat, one Revit - the binding, and the rule that makes it safe.

Step 5. Choosing between sessions is where the field notes say silent
wrong-model damage begins: someone finishes a job in the wrong project and
does not find out for a week. So the whole of this file is about refusing to
guess, and about knowing WHY the current session is the current one.

THE DISTINCTION THAT MATTERS (docs/25, and it was found by running it, not by
reasoning about it):

    An assumption is not a choice.

If one Revit was open, Heron simply used it. That is an assumption. If the
user was asked and answered, that is a choice. Treating them alike is what
turns "sticky" into "keep whatever we picked first" - and then opening a
second Revit mid-conversation leaves every later command silently going to the
first one, which is the exact failure the binding exists to prevent.

    How it was bound   What changed            What happens
    ----------------   --------------------    ------------------------------
    ASSUMED            a second Revit appears  ASK. They never chose this one
    ASSUMED            it closed               quietly take the remaining one
    CHOSEN             more Revits appear      keep it. Do not nag
    CHOSEN             it closed               STOP. Never slide onto another

Every refusal says "nothing has been sent to Revit" in as many words. The
user's first thought on any refusal is "did it half-do something?", and
answering that unasked is the difference between a safe stop and a
frightening one.

The binding lives for the life of this MCP server, which is the life of one
chat. That is the correct scope: one chat, one Revit.
"""

import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "client"))

import heron_bridge_client as bridge          # noqa: E402


class NotBound(Exception):
    """
    No session could be resolved, and the message says what to do about it.

    Carries `sessions` when there is a real choice to present, so the caller
    can offer a picker rather than repeating the list itself.
    """

    def __init__(self, message, sessions=None):
        Exception.__init__(self, message)
        self.sessions = sessions or []


# ---------------------------------------------------------------- the wall
#
# WHICH REVIT RELEASE THE VERSION FILTER SHOULD USE, AND HOW THAT WAS DECIDED.
# A PURE function of what is connected, so it can be tested with no Revit and
# no MCP SDK - which is the whole reason it lives here rather than beside its
# one caller in `heron_mcp_server.py`, a module CI cannot import.

NOTHING_CONNECTED = "nothing is connected"
CANNOT_BE_READ = "the connected Revits could not be read"


def version_for_filter(live, chosen_pid, chosen_explicitly):
    """(release, how) for the version wall, from what is connected.

    SIX ANSWERS, WHERE THE OLD BODY HAD FOUR AND ONE OF THOSE COVERED THREE
    DIFFERENT SITUATIONS:

        release + "chosen"                       picked, and still live
        release + "assumed, not chosen"          bound without being picked
        release + "the only Revit connected"     one live, so used and named
        None    + "nothing is connected"         )
        None    + "N Revits ... none chosen"     ) all three were (None, None)
        None    + "the Revit you chose ... "     )

    `_revit_version` returned `(None, None)` for the last three alike, and its
    caller printed *"No Revit is connected, so the version filter did not
    run"* for all of them. FRAGMENT-ISSUES row 130 is a modeller with two
    Revits open being told none was - measured with sessions 20472 and 36908
    live, and `revit_use_session 36908` in the very next call naming one of
    them. Row 159 is the third, which was worse: it did not print that
    sentence at all, it returned the SURVIVOR's release.

    **AND THE WORDING IS THE SMALLER HALF.** The filter genuinely did not run,
    so no fragment was checked against the release it would run on - and the
    configuration where that check matters MOST is exactly the one that
    reached this path: more than one release open at once.

    `how` is never None. An answer with no release still says WHY, because
    *nothing is connected* and *four are and you have not picked* send a
    reader to different actions, and D-52's rule is that an absent measurement
    is not a clean one.

    AN ASSUMPTION IS STILL NAMED AS ONE. With exactly one Revit live and no
    choice made, the release is used and called *the only Revit connected* -
    this file's own table says an assumption is not a choice, and the cheapest
    place to keep that honest is where it is made.
    """
    live = list(live or [])
    if chosen_pid is not None:
        for one in live:
            if one.pid == chosen_pid:
                return one.revit_version, ("chosen" if chosen_explicitly
                                           else "assumed, not chosen")
        if chosen_explicitly:
            # A CHOICE THAT HAS CLOSED IS NOT AN INVITATION TO TAKE THE NEXT
            # ONE, and the old code took it. With the chosen Revit gone and
            # exactly one other live, it fell to the branch below and returned
            # that one's release as "the only Revit connected" - the version
            # wall quietly moving to a release the user never picked. This
            # file's own table, four lines from here, is the rule it broke:
            # CHOSEN + it closed -> STOP. Never slide onto another.
            #
            # Found 2026-09-20 by reading this function while repairing row
            # 130, not by the row, and recorded separately as row 159.
            return None, ("the Revit you chose (pid %s) has closed"
                          % chosen_pid)
    if len(live) == 1:
        return live[0].revit_version, "the only Revit connected"
    if not live:
        return None, NOTHING_CONNECTED
    # SEVERAL, AND NONE PICKED. Naming the count is what makes the sentence
    # actionable: a reader who is told "2 are connected" knows there is
    # something to pick between, where "none is connected" tells them to go
    # and start Revit.
    return None, ("%d Revits are connected and none has been chosen"
                  % len(live))


class SessionBinding(object):
    """Which Revit this chat is talking to, and how that was decided."""

    def __init__(self):
        self._pid = None
        self._explicit = False      # did the user actually choose, or did we assume?

    @property
    def pid(self):
        return self._pid

    @property
    def was_chosen(self):
        return self._explicit

    def forget(self):
        self._pid = None
        self._explicit = False

    # ------------------------------------------------------------------ list

    def sessions(self):
        """
        Every connected Revit, built LIVE - never a cached snapshot.

        A picker made from a remembered list offers sessions that have since
        closed and hides ones that have since opened, which is worse than no
        picker at all.
        """
        live, starting, stale, mismatched = bridge.discover()
        return live, starting, mismatched

    def describe(self, sessions):
        """
        The picker, numbered from 1. Numbers, because a process id is not
        something a person should be asked to read or repeat.

        Each row now carries whether that Revit is FREE or already held by
        another chat. docs/25 called this the missing data that makes the list
        honest: before the lease, the only thing shown was which one THIS chat
        was using, and a user picking a busy Revit found out by having their
        request refused. Asking costs nothing and claims nothing - `info` is
        lease-exempt.
        """
        lines = []
        for index, b in enumerate(sessions, 1):
            mark = "  <- currently in use" if b.pid == self._pid else ""
            lines.append("  %d) Revit %s   (session %s)  %s%s"
                         % (index, b.revit_version, b.pid, bridge.availability(b), mark))
        return "\n".join(lines)

    # ----------------------------------------------------------------- choose

    def choose(self, number_or_pid):
        """
        Bind deliberately, by picker number or by session id. This is the only
        way `was_chosen` becomes true.
        """
        live, starting, mismatched = self.sessions()
        if not live:
            raise NotBound(_nothing_connected(starting, mismatched))

        wanted = str(number_or_pid).strip()
        chosen = None

        if wanted.isdigit() and 1 <= int(wanted) <= len(live):
            chosen = live[int(wanted) - 1]
        if chosen is None:
            for b in live:
                if str(b.pid) == wanted:
                    chosen = b
                    break

        if chosen is None:
            raise NotBound(
                "There is no session %s. Connected right now:\n%s\n\n"
                "Nothing has been sent to Revit." % (wanted, self.describe(live)),
                live)

        self._pid = chosen.pid
        self._explicit = True
        return chosen

    # ---------------------------------------------------------------- resolve

    def resolve(self):
        """
        The session to act on, or NotBound with a message saying what to do.

        This is the whole four-case table, and the order of the checks is the
        design. Read it against docs/25 before changing anything here.
        """
        live, starting, mismatched = self.sessions()

        if not live:
            self.forget()
            raise NotBound(_nothing_connected(starting, mismatched))

        if self._pid is not None:
            current = next((b for b in live if b.pid == self._pid), None)

            if current is not None:
                # Their own choice stands even as other Revits come and go.
                # They already answered this question; asking again is nagging.
                if self._explicit or len(live) == 1:
                    return current

                # Only ASSUMED, and now there is a real choice to make. Ask
                # before anything is sent - they never picked this one.
                self.forget()
                raise NotBound(
                    "Another Revit has been connected since this chat started, so there are now "
                    "%d and it is no longer safe to assume:\n%s\n\n"
                    "Which one do you mean? Nothing has been sent to Revit."
                    % (len(live), self.describe(live)),
                    live)

            # The session being used has gone.
            was_chosen, lost = self._explicit, self._pid
            self.forget()

            if was_chosen:
                # They named this one. Sliding onto whatever else is open is
                # exactly the "finished in the wrong project" failure this
                # mechanism exists to prevent. Fail closed.
                raise NotBound(
                    "The Revit session you chose (%s) has closed, so Heron has stopped rather "
                    "than moving to another model on its own.\n\nStill connected:\n%s\n\n"
                    "Choose one to carry on. Nothing has been sent to Revit."
                    % (lost, self.describe(live)),
                    live)
            # Never explicitly chosen, so there is nothing of theirs to
            # contradict. Fall through and pick again.

        if len(live) == 1:
            # The ordinary case, and it must feel exactly as it always did:
            # one Revit, no questions asked.
            self._pid = live[0].pid
            self._explicit = False
            return live[0]

        raise NotBound(
            "%d Revit sessions are connected, so it is not safe to guess which one you mean:\n"
            "%s\n\nWhich one? Nothing has been sent to Revit."
            % (len(live), self.describe(live)),
            live)


def _nothing_connected(starting, mismatched):
    if starting:
        return ("Revit is running but its bridge is not answering yet - it is probably still "
                "starting up. Try again in a moment.")
    if mismatched:
        return ("A Revit is connected but speaks a different protocol version. Restart that "
                "Revit to finish updating; Heron will not talk across protocols.")
    return ("No Revit is connected. Open Revit, then press  Heron AI > Heron  on the ribbon. "
            "The button lights up when it is connected.")
