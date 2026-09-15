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


def describe_vertical(millimetres):
    """
    A vertical move as a modeller says it: "up 200 mm", "down 50 mm".

    THE SIGN IS NOT THE READER'S JOB. The caller used to write " up " itself
    and pass the signed number to describe(), so asking to lower something
    produced *"This would move 9 ducts up -50 mm"*. It did the right thing and
    described it the way a programmer would.

    Mirrors HeronUnits.DescribeVerticalMove on the C# side, which words the
    same move in the add-in's own reply and in Revit's undo history. The two
    have to agree: the user reads the preview here and the undo entry there,
    about one operation.

    Zero keeps no direction - "0 mm" moves nowhere, and claiming "up" would
    name a direction the move does not have.
    """
    rounded = round(millimetres, 3)
    if rounded == 0:
        return describe(0)
    if rounded < 0:
        return "down " + describe(-millimetres)
    return "up " + describe(millimetres)


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

    # The identity fields the add-in can send, MOST SPECIFIC FIRST, each with
    # the prefix its collapsed key carries. Read in this order everywhere, so
    # "which of these do both sides have" is one question with one answer.
    _FIELDS = (("projectKey", "project:"),
               ("documentPath", "path:"),
               ("document", "title:"))

    def __init__(self):
        self._key = None
        self._title = None
        # EVERY identity field of the pinned document, not just the winning
        # one. See check() for why keeping only the winner was the bug.
        self._seen = {}

    @property
    def key(self):
        """The STABLE identity, which is what a scope store is named after.

        `title` is for showing a person which model is pinned. It is NOT
        identity - this class's own docstring says so, and says why: two Revit
        sessions here really did have two documents both called Project1.
        Three brain tools were passing `title` into `open_scope`'s
        `project_key` slot anyway, so a project store was named after a display
        name that changes when somebody renames a file. Found by a review
        2026-09-11, in the class written to prevent exactly this.
        """
        return self._key

    @property
    def project_key(self):
        """The key a PROJECT KNOWLEDGE STORE may be named after, or None.

        Deliberately narrower than `key`. A pin only has to tell two open
        models apart, so a path or a title will do; a store's NAME has to
        survive the file being renamed and must never be shared with a
        different model that happens to sit at the same path. Only the Project
        Information UniqueId does both, so only that is returned here.

        None means "not known yet", and every caller must treat it as a
        question to ask rather than a scope to skip: skipping the project
        store silently answers a project question out of the company standard
        and says nothing about it.
        """
        if self._key and self._key.startswith("project:"):
            return self._key[len("project:"):]
        return None

    @property
    def is_pinned(self):
        return self._key is not None

    def forget(self):
        self._key = None
        self._title = None
        self._seen = {}

    @staticmethod
    def key_of(reply):
        """
        A document's identity as the add-in reports it.

        Title alone is NOT identity, and that is not theoretical here: the
        first live run of the selection tool had two Revit sessions with a
        model called Project1 open in each. The path distinguishes saved
        models; an unsaved one falls back to the title and is pinned as
        loosely as it deserves.

        `projectKey` IS PREFERRED AND IT IS THE ONLY ONE A KNOWLEDGE SCOPE MAY
        BE NAMED AFTER. heron_scope._safe_key defines the project key as the
        UniqueId of the document's Project Information element - created with
        the document, surviving save, rename and move. This returned a
        path-based key and three brain tools handed it to `open_scope`, so a
        project store was named after a FILE NAME: rename the file and the
        knowledge is gone; open a detached copy and it is somebody else's
        store. The add-in had that UniqueId all along for its own
        preview/commit pairing and never sent it. It does now. Found by a
        review 2026-09-11.

        THE PATH AND TITLE FALLBACKS STAY, FOR PINNING ONLY. Golden Rule 20
        needs to tell two open models apart and any stable string does that.
        Naming a STORE is a different question with a stricter answer, which
        is why `project_key` below returns only the first of these.
        """
        seen = DocumentPin.identity_of(reply)
        for field, prefix in DocumentPin._FIELDS:
            if field in seen:
                return prefix + seen[field]
        return None

    @staticmethod
    def identity_of(reply):
        """
        EVERY identity field the add-in sent, by name. Missing ones absent.

        `key_of` collapses these to one string and throws the rest away, which
        is right for naming a thing and wrong for comparing two of them - see
        check(). A field sent as JSON null (Json.Str writes `"documentPath":
        null` for an unsaved model) counts as not sent.
        """
        if not reply:
            return {}
        found = {}
        for field, _ in DocumentPin._FIELDS:
            value = reply.get(field)
            if value:
                found[field] = str(value)
        return found

    def _common(self, seen):
        """
        The most specific identity BOTH the pin and this reply carry, or None.

        This is the whole fix. Comparing collapsed keys compares whatever each
        side happened to win with, so one document answered for twice can
        produce two different strings.
        """
        for field, _ in self._FIELDS:
            if field in self._seen and field in seen:
                return field
        return None

    def check(self, reply):
        """
        Pin on first sight; afterwards, refuse a different document.

        Returns None when it is safe to carry on, or the refusal to show the
        user. Never raises: the caller is usually mid-sentence to a person.

        IT COMPARES LIKE WITH LIKE, WHICH IT DID NOT USED TO. The version
        before this one compared the collapsed keys `key_of` builds, so the
        answer depended on which fields a given reply happened to include
        rather than on which model it was about. Every operation sends
        `projectKey` except the fragment reply, which sent the title alone -
        so a chat pinned "project:<uid>" by any read tool, then read
        "title:Project1" back off every revit_change, and refused the write
        with a sentence naming THE SAME MODEL on both sides of the "but".
        revit_use_this_model could not clear it either: it repins from
        count_elements, which sends the key again. Reported 2026-09-15,
        against an unsaved model, where there was no path to stand in for the
        missing key. RevitFragment.Report now sends the identity like every
        other op; this side no longer depends on it having remembered to.

        THE GUARD IS NOT LOOSENED. Where both sides name a project key, that
        is still what decides; where both name a path, that is. A different
        one is still a refusal. What changes is only the case where one side
        did not say - and there the strongest thing both DID say answers,
        which is exactly the strength the protocol actually carried.
        """
        seen = self.identity_of(reply)
        if not seen:
            return None                     # nothing to pin against; say nothing

        title = reply.get("document") or "the open model"

        if self._key is None:
            self._seen = dict(seen)
            self._key = self.key_of(reply)
            self._title = title
            return None

        field = self._common(seen)
        if field is None:
            # Nothing in common, so nothing to disagree about. The same
            # silence as an unidentifiable reply above, for the same reason:
            # a refusal nobody could act on is worse than none.
            return None

        if self._seen[field] == seen[field]:
            self._title = title             # a save can rename it; same document

            # WHAT A MATCH MAY TEACH THE PIN. Matching on a project key or a
            # path means this IS the same model, so a field this reply adds
            # can be believed - and a pin that started loose becomes one a
            # knowledge store may be named after. Matching on the title alone
            # proves nothing of the kind: two open models called Project1 is
            # the case this class exists for. So a title match is enough to
            # let the work through and never enough to name a store (D-33).
            if field != "document":
                for name, value in seen.items():
                    self._seen.setdefault(name, value)
                self._key = self.key_of(self._seen)
            return None

        was, now = self._title, title

        # Deliberately does NOT say "click back to it". From here Heron cannot
        # tell whether the old model is still open or has been closed, and
        # sending someone to look for a model that is no longer there - at the
        # moment they are already unsure what just happened to their work - is
        # worse than saying less. The add-in CAN tell the two apart and does so
        # when it matters, which is at the point of writing (RevitWrite).
        if was == now:
            # A TRUE REFUSAL THAT READS LIKE THE BUG ABOVE. Two open models
            # really can share a name - it is why this class prefers a key to
            # a title - and then naming each side identifies neither. "Go back
            # to Project1" would be an instruction the user cannot carry out,
            # so this wording does not give it.
            return ("This chat has been working on a model called %s, and the model in "
                    "front in Revit now is a DIFFERENT one with the same name. Heron "
                    "will not change a model you did not point it at.\n\n"
                    "Nothing has been sent to Revit. Bring back the one you were "
                    "working on, or say 'use this model' to move this chat onto the "
                    "one in front deliberately." % was)

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

    @property
    def document_path(self):
        """The pinned document's file path, or None for an unsaved model.

        Public for the same reason `title` is, and for one more: a write is now
        AIMED at the pinned document rather than at whatever is in front, and a
        path is the one identity that tells two open models apart when they
        share a name. `title` alone cannot - this class exists because two
        sessions really did have a model called Project1 open in each.

        NOT `project_key`, deliberately, and that is the lesson of E11. The
        project key is `ProjectInformation.UniqueId`, which is inherited from
        the TEMPLATE: two blank projects and an unrelated model in another
        Revit release were all measured reporting the same one
        (NEEDS-CHECKING, Group E). It names a knowledge store well and it
        cannot pick a document out of a list at all.
        """
        return self._seen.get("documentPath")

    def repin(self, reply):
        """Move the pin, deliberately, because the user said so."""
        self._seen = self.identity_of(reply)
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
