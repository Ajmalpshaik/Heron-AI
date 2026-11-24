#!/usr/bin/env python3
# Heron-Agent:  HERON-MCP-SRV-001
# Heron-Step:   5
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
The model this chat is pinned to: aiming a request at it, the model in front
of it, and its own record of answers.

    SESSION_READS,        the cards that read the Revit session, not one model,
    _reads_the_session    and so are aimed at no pin (row 5b-1887)
    _aim_at_pin           the pinned model's identity, put on a request
    _FRONT_SAID,          a pinned model and a different one in front, said
    _pin_and_front,       once for each pair, before any write (issue #1031)
    _front_sentence,
    _said_front,
    _front_refusal
    _why_no_project,      what an answer says first about the open model's own
    _project_note,        record - or why it has none (5b-324, D-113)
    _with_project_note,
    _asks_project

NOT RUN, AND NO TOOL HERE. revit_health, revit_change, revit_read, revit_show,
revit_undo_last and the knowledge and calculation tools call these, and all of
them stay in heron_mcp_server.py, which imports this file near its top. A file
that holds no tool registers nothing, so where it is imported changes nothing a
host is served (heron_tools.served_order).

WHY THEY ARE HERE. They sat in heron_mcp_server.py until 2026-11-24 and were
moved whole and unchanged, to make that file smaller. The pin itself, and the
one binding, are heron_server_core's - one of each in a process - and this file
takes them from there. It imports no other file of the server, and never the
entry file, which runs as `__main__` and would run a second time.

ONE _FRONT_SAID. The entry file imports it by name, and a name imported is the
same set, so what revit_health adds and what revit_change reads is one record.
"""

# heron_server_core first: it puts the bridge client on the path, as it says.
from heron_server_core import pinned                       # noqa: E402
import heron_bridge_client as bridge                       # noqa: E402
import heron_brain as brain                                # noqa: E402


# READS OF THE REVIT SESSION, NOT OF ONE MODEL - row 5b-1887 (issue #1112).
# REPORT_OPEN_DOCUMENTS lists every model open in the Revit it runs in; it is
# what a chat asks to find out what is open. Aimed at the pin like every other
# read, it was REFUSED the moment the pinned model was closed - "No open model
# saved at <that family>. Open: Project1." (Revit 2020, 2026-11-19) - which is
# exactly when the family method asks it whether the save landed. So a card
# named here is sent with no model to aim at, and its reply pins nothing and
# is checked against no pin: it is about the session, and the pin stays as it
# was. A card goes on this list only if its answer is the same whichever open
# model it runs in - tests/test_read_door.py reads each one's C# for the loop
# over the application's documents and its risk for READ.
SESSION_READS = frozenset({"REPORT_OPEN_DOCUMENTS"})


def _reads_the_session(capability):
    """True for a card whose answer is the Revit session's, not one model's."""
    return (capability or "").strip().upper() in SESSION_READS


def _aim_at_pin(args):
    """
    Put the pinned model's identity on a fragment request, in place.

    ONE COPY, FOR BOTH DOORS. revit_change and revit_read send the same three
    keys because `RevitFragment.Run` is one method for both, and reads the
    same three whichever door was used. The reasons are below, as they were
    written for revit_change - on the WRITE path "too late" means committed,
    and on the READ path it means an answer about a model nobody pointed at.
    """
    # THE PIN TRAVELS WITH THE REQUEST, because checking it on the reply is
    # checking it too late - and on the WRITE path "too late" means committed.
    # The fragment runs inside a TransactionGroup and group.Assimilate() has
    # already kept the work by the time a reply exists, so the pinned.check()
    # after the reply used to refuse a change that was already in the model,
    # in a sentence saying nothing had been sent to Revit. The same defect as the
    # selection tool's, on the one tool where the cost is the model. Found by
    # a review 2026-09-15. The add-in refuses on this key before it opens the
    # transaction group.
    #
    # Empty when this chat has not pinned a project yet, which the add-in
    # reads as "do not check": a first request has nothing to compare
    # against, and refusing it would make the pin unobtainable.
    #
    # WHERE THIS STILL GOES OUT EMPTY WITH A PIN SET, stated rather than
    # discovered later: a FAMILY document has no Project Information, and a
    # reply from an add-in too old to send one carries none, so there is no
    # id the add-in could compare - the gate stays off and the caller's
    # pinned.check() is the only cover. Closing that would mean the add-in
    # comparing a compound identity rather than one id, which is a change to
    # the contract select_by_category shares and is not this fix.
    #
    # WHAT IT SENDS IS THE SHARED ID, BY ITS OWN NAME (5b-324, D-113). The
    # add-in compares RevitOperations.ProjectKey, the Project Information
    # UniqueId, so that is what goes - `pinned.project_key` is the model's own
    # CreationGUID now and would refuse every request. Between two models
    # made from one template this gate still cannot tell them apart; the AIM
    # below is what keeps a write in the model the chat was pointed at.
    args["expectProject"] = pinned.project_information_id or ""

    # AIM THE WRITE AT THE MODEL THIS CHAT WAS POINTED AT, rather than at
    # whatever happens to be in front of Revit.
    #
    # WHY THIS AND NOT THE GUARD BELOW. `expectProject` asks "did the user
    # move?" and answers it by comparing project keys - and E11 was run in
    # front of Revit on 2026-09-15 and FAILED. `ProjectKey` is
    # `ProjectInformation.UniqueId`, which is inherited from the TEMPLATE: two
    # blank projects and an unrelated PIPE.rvt open in a different Revit
    # release all reported `8764c510-...-0000c160`. The comparison found them
    # equal, the guard passed, and `CREATE_LEVEL ran in Project2` - a write
    # into the model the chat was NOT pointed at, which is the one thing the
    # guard exists to stop. Nothing is fixed by tightening a comparison whose
    # two sides are equal and both wrong.
    #
    # So the question is inverted. Not "did the user move?" but "which model
    # was I told to work on?" - which needs no key, and does not care what is
    # on screen. It is also what the owner asked for in as many words: pin
    # project 1, refer to project 2, and be sat in a third while it works.
    #
    # THE ADD-IN ALREADY DOES THIS. `RevitFragment.Run` has always accepted a
    # `document` and found it among the open ones, refusing with
    # `no_such_document` and listing them when it cannot. This path simply
    # never sent one. Demonstrated end to end the same day with a FAMILY in
    # front throughout: ten levels written into Project1 by name, verified by
    # reading back, Project2 untouched.
    #
    # BOTH IDENTITIES TRAVEL, strongest first. A path tells two open models
    # apart when they share a name, which is the case this class was built
    # around; an unsaved model has none and falls back to the title, and the
    # add-in refuses an AMBIGUOUS title rather than picking one.
    #
    # Nothing is sent before a pin exists - the first request has no model to
    # aim at and is what obtains the pin.
    if pinned.title:
        args["document"] = pinned.title
        if pinned.document_path:
            args["documentPath"] = pinned.document_path


# THE PAIRS OF MODELS THIS CHAT HAS ALREADY BEEN TOLD ABOUT - issue #1031. A
# pinned model and a different model in front, said once, by revit_health or
# by revit_change refusing before its first write. Kept as long as this server,
# which is as long as the chat, like the pin itself.
_FRONT_SAID = set()


def _pin_and_front(session):
    """
    (pinned title, title in front) when the model in front of this Revit is
    not the one this chat is pinned to - or None: nothing pinned yet, the
    add-in names no model in front, the two names agree, or `info` could not
    be read.

    Issue #1031: revit_health named the model in front, "Project1", and the
    next change ran in the real project the chat was pinned to - by D-74, on
    purpose, since a write is aimed at the pinned model whatever is in front.
    Health and the pin said two different things, and the modeller learned
    which model was meant only after a transaction had been opened in a job
    model nobody chose.

    BY TITLE, FROM `info`, which is lease-exempt and claims no model, so
    asking claims nothing. Two titles that differ are two models; two that
    agree may still be two (two Project1s), and then nothing is said here -
    the pin's own identity check stays the cover for that, as before.
    """
    if not pinned.title:
        return None
    try:
        front = bridge.open_model_named(session)
    except Exception:                             # noqa: BLE001 - says nothing, never stops a call
        return None
    if not front or front == pinned.title:
        return None
    return pinned.title, front


def _front_sentence(pin, front):
    """The words for a chat pinned to `pin` while `front` is in front."""
    return ("This chat works on %s, and %s is in front in Revit now. A change from this "
            "chat goes to %s, not to the model in front (D-74)." % (pin, front, pin))


def _said_front(pin, front):
    """Remember that this chat was told about this pin and this model in front."""
    _FRONT_SAID.add((pinned.key or pin, front))


def _front_refusal(session):
    """
    The refusal revit_change gives BEFORE its first write while the pinned
    model is not the one in front - issue #1031, "say so before any write" -
    or None.

    ONCE FOR EACH PAIR OF MODELS. D-74 is the owner's own design - pin
    project 1, look at project 3, and Heron still works on project 1 - so
    this never stops that work: it says it, once, before anything is sent,
    and the same request sent again goes to the pinned model as D-74 says.
    A pair revit_health has already named is not refused again.
    """
    pair = _pin_and_front(session)
    if pair is None:
        return None
    pin, front = pair
    if (pinned.key or pin, front) in _FRONT_SAID:
        return None
    _said_front(pin, front)
    return ("%s Nothing was sent to Revit.\n\n"
            "Ask the modeller which model they mean before anything is changed. To change "
            "%s, say 'use this model' first (revit_use_this_model). To go on in %s, send "
            "the same request again - this is said once for these two models."
            % (_front_sentence(pin, front), front, pin))


# ---------------------------------------------------------------------------
# The open model's own record (FRAGMENT-ISSUES 5b-324, D-113). A project's
# answers are kept under the model's own id, DocumentPin.project_key. What was
# kept before under the id every template-born project shares is a QUESTION
# put at the top of the answers that would have used it - never a match.
# ---------------------------------------------------------------------------

def _why_no_project():
    """Why the open model has no record of its own, as a modeller reads it."""
    if not pinned.is_pinned:
        return ("Heron learns which project this is from the open model: ask it to select "
                "or count something in Revit first. It will not guess (D-33).")
    if pinned.project_information_id:
        return ("This model reports no id of its own - Revit 2020 to 2023 have none, and "
                "Revit 2024 and later report one only once Heron's add-in is updated and "
                "Revit restarted (D-113). The only id it does report is its template's, "
                "shared by every model made from that template, so Heron keeps nothing for "
                "it rather than share one model's answers with another (5b-324).")
    return ("The model this chat is pinned to has no Project Information - a family - and "
            "Heron keeps no project answers for a family.")


def _project_note():
    """
    What an answer that keeps or reads a project's answers says FIRST about
    the open model's own record, or "".

    While answers kept under the shared id are waiting for the modeller's
    word, the question about them - they were NOT used (heron_earlier). When a
    pinned model reports no id of its own, why nothing is kept for it. A chat
    that has read no model yet says nothing here: its answers already say
    that the project is not known.
    """
    if not pinned.is_pinned:
        return ""
    if pinned.project_key:
        try:
            return brain.earlier_question(pinned.project_key,
                                          pinned.project_information_id) or ""
        except brain.BrainUnavailable as why:
            return str(why)
    if pinned.project_information_id:
        return "NOTHING IS KEPT FOR THIS MODEL. " + _why_no_project()
    return ""


def _with_project_note(said):
    note = _project_note()
    return "%s\n\n%s" % (note, said) if note else said


def _asks_project(scopes):
    """Whether a knowledge answer was asked of the project scope."""
    return "project" in [one.strip().lower() for one in (scopes or "").split(",")]
