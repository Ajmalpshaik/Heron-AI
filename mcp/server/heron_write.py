#!/usr/bin/env python3
# Heron-Agent:  HERON-SES-PIN-005, HERON-KRN-HUM-018, HERON-MCP-SEC-011
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
The conversation half of writing to a model.

Step 5 settled WHICH REVIT this chat is talking to. That is only half the
question, and Golden Rule 20 is about the other half: one Revit can hold
several projects open at once, and which one is in front changes the moment
the user clicks another window. A binding that stops at the session lets a
change land in the wrong building with every step individually correct.

So there are two pins, not one:

    SESSION   heron_session.py, Step 5   which Revit
    DOCUMENT  this file, Step 6          which model inside it

And there are two halves to the approval, deliberately in different places:

    THE MODEL'S HALF   RevitWrite, in the add-in. Holds the real preview,
                       re-counts against the live model immediately before
                       writing, and refuses if anything moved. It is the only
                       side that can see the model, so it is the only side
                       whose agreement means anything.

    THE CHAT'S HALF    here. Remembers what the USER was actually shown, so
                       an approval cannot be applied to something they never
                       saw. It is the only side that can see the conversation.

Neither is sufficient alone. The add-in cannot know whether a person read the
sentence; the chat cannot know whether the model has changed underneath it.
"""

import re

# --------------------------------------------------------------------- units

# Only units a modeller on this project actually says, and each one exact.
#
# Feet and inches are deliberately absent. Revit's internal unit is feet, so
# accepting them here would put the one number a user might mean in imperial
# right next to the one number the API means in imperial - and a mix-up
# between those two is a factor of 304.8 applied to a real model.
_UNITS = {
    "mm": 1.0,
    "millimetre": 1.0, "millimetres": 1.0,
    "millimeter": 1.0, "millimeters": 1.0,
    "cm": 10.0,
    "centimetre": 10.0, "centimetres": 10.0,
    "centimeter": 10.0, "centimeters": 10.0,
    "m": 1000.0,
    "metre": 1000.0, "metres": 1000.0,
    "meter": 1000.0, "meters": 1000.0,
}

# The same ceiling the Kernel enforces (HeronUnits.MaxMillimetres): 100 km.
# Checked on both sides on purpose - this side to say something useful to a
# person, the far side because the wire is not a place to extend trust.
MAX_MM = 100.0 * 1000.0 * 1000.0

_NUMBER = re.compile(r"^([+-]?(?:\d+\.?\d*|\.\d+))\s*([a-zA-Z]*)$")


class BadDistance(Exception):
    """Not a distance Heron will act on, with the reason in the message."""


def parse_millimetres(text):
    """
    A distance as a person writes it, to a number of millimetres.

    Accepts "200", "200mm", "200 mm", "-50", "0.5 m", "20 cm". A bare number
    is millimetres, which is the house convention and what the user says.

    REFUSES rather than guesses, every time. A distance is the one input here
    that becomes a physical change to a building, and the failure mode of
    guessing is not an error message - it is a model that moved by a thousand
    times what was meant, and looks fine until somebody measures it.
    """
    if text is None:
        raise BadDistance("No distance was given. Say how far, like '200 mm'.")

    cleaned = str(text).strip().replace(",", "")
    if not cleaned:
        raise BadDistance("No distance was given. Say how far, like '200 mm'.")

    match = _NUMBER.match(cleaned)
    if not match:
        raise BadDistance(
            "'%s' is not a distance Heron can read. Try a number of millimetres, "
            "like '200 mm'." % text)

    number, unit = match.group(1), match.group(2).lower()

    if unit and unit not in _UNITS:
        if unit in ("ft", "feet", "foot", "in", "inch", "inches", "'", '"'):
            raise BadDistance(
                "Heron works in millimetres here, not feet or inches. Say it in mm - "
                "'200 mm' - so there is no doubt which unit the model moves by.")
        raise BadDistance(
            "Heron does not know the unit '%s'. Use mm, cm or m." % unit)

    try:
        millimetres = float(number) * _UNITS.get(unit or "mm", 1.0)
    except (ValueError, OverflowError):
        raise BadDistance("'%s' is not a distance Heron can read." % text)

    # float('nan') and float('inf') cannot reach here through the regex, but
    # the check stays: this value is one step from a transaction, and the cost
    # of the check is nothing against the cost of being wrong.
    if millimetres != millimetres or millimetres in (float("inf"), float("-inf")):
        raise BadDistance("'%s' is not a distance Heron can read." % text)

    if abs(millimetres) > MAX_MM:
        raise BadDistance(
            "%s is further than Heron will move anything in one go. If that is really "
            "what you meant, do it in smaller steps." % describe(millimetres))

    if abs(millimetres) < 0.001:
        raise BadDistance(
            "Moving something by zero would change nothing. Say how far to move it.")

    return millimetres


def describe(millimetres):
    """A distance written the way the user wrote it back to them: always mm."""
    rounded = round(millimetres, 3)
    if rounded == int(rounded):
        return "%d mm" % int(rounded)
    return ("%.3f" % rounded).rstrip("0").rstrip(".") + " mm"


# ----------------------------------------------------------- document pinning


class DocumentPin(object):
    """
    Document Pinning Agent. Which MODEL this chat is working on, inside the
    Revit session it is bound to.

    GOLDEN RULE 20, and the reason it exists is worth keeping in front of
    whoever edits this: every step can be individually correct and the change
    still lands in the wrong building. Nobody makes a mistake. The user clicks
    another open project between two messages, and Heron - following the
    active window, as every Revit tool does by default - does exactly what it
    was told, somewhere else.

    So the pin is set the first time this chat sees a document, and after that
    a DIFFERENT document is a refusal, not a silent retarget.
    """

    def __init__(self):
        self._key = None
        self._title = None

    @property
    def title(self):
        return self._title

    @property
    def is_pinned(self):
        return self._key is not None

    def forget(self):
        self._key = None
        self._title = None

    @staticmethod
    def key_of(reply):
        """
        A document's identity as the add-in reports it.

        Title alone is NOT identity, and that is not theoretical here: the
        first live run of the selection tool had two Revit sessions with a
        model called Project1 open in each. The path distinguishes saved
        models; an unsaved one falls back to the title and is pinned as
        loosely as it deserves.
        """
        if not reply:
            return None
        path = reply.get("documentPath")
        title = reply.get("document")
        if path:
            return "path:" + str(path)
        if title:
            return "title:" + str(title)
        return None

    def check(self, reply):
        """
        Pin on first sight; afterwards, refuse a different document.

        Returns None when it is safe to carry on, or the refusal to show the
        user. Never raises: the caller is usually mid-sentence to a person.
        """
        key = self.key_of(reply)
        if key is None:
            return None                     # nothing to pin against; say nothing

        title = reply.get("document") or "the open model"

        if self._key is None:
            self._key = key
            self._title = title
            return None

        if key == self._key:
            self._title = title             # a save can rename it; same document
            return None

        was, now = self._title, title

        # Deliberately does NOT say "click back to it". From here Heron cannot
        # tell whether the old model is still open or has been closed, and
        # sending someone to look for a model that is no longer there - at the
        # moment they are already unsure what just happened to their work - is
        # worse than saying less. The add-in CAN tell the two apart and does so
        # when it matters, which is at the point of writing (RevitWrite).
        return ("This chat has been working on %s, but %s is in front in Revit now. "
                "Heron will not change a model you did not point it at.\n\n"
                "Nothing has been sent to Revit. Go back to %s if you meant that one, "
                "or say 'use this model' to move this chat onto %s deliberately."
                % (was, now, was, now))

    @property
    def title(self):
        """The pinned document's name, or None when nothing is pinned.

        Public because the Context Manager needs it: docs/19 s1 lists "what
        project is active" as part of the situation, and the packet said
        "project: none named" on every request until this existed - while the
        add-in had known the name since the first count_elements. Read-only, so
        reading it can never move the pin.
        """
        return self._title

    def repin(self, reply):
        """Move the pin, deliberately, because the user said so."""
        self._key = self.key_of(reply)
        self._title = reply.get("document") if reply else None
        return self._title


# ---------------------------------------------------------------- approval


class PendingApproval(object):
    """
    Human Approval Agent, chat side. What the user was shown, and therefore
    what they are able to approve.

    It holds the add-in's preview token rather than any description of the
    work. The token is what the add-in will check, so the two sides agree by
    construction instead of by both being written correctly.
    """

    def __init__(self):
        self._token = None
        self._summary = None

    @property
    def summary(self):
        return self._summary

    def offer(self, token, summary):
        self._token = token
        self._summary = summary

    def clear(self):
        self._token = None
        self._summary = None

    def take(self):
        """
        The token to send, once. Cleared as it is handed over.

        Single use on purpose. An approval is for one change: if the same
        token could be spent twice, "yes" said once would move the ducts
        200 mm and then 200 mm again, and the second move would look exactly
        as successful as the first.
        """
        token, self._token = self._token, None
        summary, self._summary = self._summary, None
        return token, summary
