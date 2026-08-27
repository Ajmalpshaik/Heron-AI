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
        """The picker, numbered from 1. Numbers, because a process id is not
        something a person should be asked to read or repeat."""
        lines = []
        for index, b in enumerate(sessions, 1):
            mark = "  <- currently in use" if b.pid == self._pid else ""
            lines.append("  %d) Revit %s   (session %s)%s"
                         % (index, b.revit_version, b.pid, mark))
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
