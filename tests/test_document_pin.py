#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The document pin, against replies that describe one model in two ways.

WHAT WENT WRONG, because the shape of it is the whole point of this file.

Golden Rule 20 says a DIFFERENT model in front is a refusal. The pin decided
"different" by comparing the collapsed key `key_of` builds - `projectKey`, or
failing that `documentPath`, or failing that the title - which answers a
question nobody asked: not "is this the same model" but "did these two replies
carry the same FIELDS".

Every operation sends `projectKey` except the fragment reply, which sent the
title alone. So:

    revit_select_by_category   -> {"projectKey": "...", "document": "Project1"}
    revit_change (a fragment)  -> {                     "document": "Project1"}

                                  project:<uid>  !=  title:Project1

and the write was refused with a sentence naming the SAME model on both sides
of its "but". revit_use_this_model could not clear it, because it repins from
count_elements, which sends the key again - so the pin went straight back to
the string the fragment reply could never produce. An unsaved model was the
most exposed: with no path, there was nothing to stand in for the missing key.

Reported 2026-09-15, blocking the MCP write path entirely.

THE FIX IS TWO-SIDED AND THIS FILE TESTS BOTH SIDES OF IT:

    RevitFragment.Report   now sends documentPath and projectKey like every
                           other op, so the key does not degrade at all.

    DocumentPin.check      compares the strongest field BOTH sides carry, so
                           an op that forgets again is answered on the best
                           evidence available instead of on a mismatch it
                           invented. Tested here; the C# is checked only for
                           the fields being on the wire, which is as far as a
                           test without Revit can honestly go.

AND THE GUARD IS STILL A GUARD. Half of this file is the refusals that must
keep firing: same title with different paths, different project keys, a
different model entirely. A fix to a false refusal that quietly stops the true
ones is not a fix - it is the wrong building, changed silently.

    python tests/test_document_pin.py
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

from heron_write import DocumentPin                        # noqa: E402
import heron_tools as TOOLS                                # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


# The identity of ONE unsaved model called Project1, as each side reports it.
# Unsaved on purpose: no documentPath, which is the case with nothing to fall
# back on. UID is the Project Information UniqueId RevitOperations.ProjectKey
# returns; MODEL is the model's own id, Document.CreationGUID, which
# RevitOperations.CreationGuid returns on Revit 2024 and later.
UID = "8a1f0c22-0000-4b7e-9d31-2f4c6b8e0a15-0003f2a1"
MODEL = "5d0c7e21-9b44-4f0e-8a6b-3c2e1f9d7a10"
READ_REPLY = {"ok": True, "document": "Project1", "projectKey": UID,
              "creationGuid": MODEL}
FRAGMENT_REPLY = {"ok": True, "document": "Project1", "ran": "move-elements"}

# TWO MODELS MADE FROM ONE TEMPLATE, as measured 2026-10-06 (FRAGMENT-ISSUES
# 5b-324). "Heron loads test" is a new project from Revit 2024's default
# template, read in session 51820: its Project Information UniqueId is the
# template's, the same one Project1, Project2 and an unrelated PIPE.rvt
# reported on 2026-09-15 (NEEDS-CHECKING E12) - while Document.CreationGUID is
# its own. The second model's CreationGUID is invented; the 18 template-born
# projects on the owner's disk each carry a different one (the 2026-10-06
# survey in 5b-324).
TEMPLATE_UID = "8764c510-57b7-44c3-bddf-266d86c26380-0000c160"
LOADS_TEST = {"ok": True, "document": "Heron loads test",
              "documentPath": r"C:\Users\x\Desktop\test\Heron loads test.rvt",
              "projectKey": TEMPLATE_UID,
              "creationGuid": "c95db604-0abd-4bc5-9ebd-3ece37b3ce52"}
PROJECT2 = {"ok": True, "document": "Project2", "projectKey": TEMPLATE_UID,
            "creationGuid": "e66f36a9-173f-46c8-bdca-d5787b826308"}


def main():
    print("The document pin - one model described two ways")

    # --- THE BUG -----------------------------------------------------------
    print()
    print("Same document, one reply with projectKey and one without")

    pin = DocumentPin()
    check(pin.check(READ_REPLY) is None,
          "a read tool pins the model on its project key")
    refusal = pin.check(FRAGMENT_REPLY)
    check(refusal is None,
          "and a fragment reply that omits the key is NOT refused - it is the "
          "same model, and the write path was blocked entirely by this")
    if refusal is not None:
        check(False, "   the refusal said: %s" % refusal.splitlines()[0])

    print()
    print("And the other way round, because a chat can start with a fragment")
    pin = DocumentPin()
    check(pin.check(FRAGMENT_REPLY) is None, "a fragment reply pins on its title")
    check(pin.check(READ_REPLY) is None,
          "and the key arriving later is not a second model appearing")

    print()
    print("revit_use_this_model cannot leave it stuck either")
    pin = DocumentPin()
    pin.check(READ_REPLY)
    pin.repin(READ_REPLY)              # what the tool does: repin off count_elements
    check(pin.check(FRAGMENT_REPLY) is None,
          "repinning then running a fragment is allowed - the user's escape "
          "hatch has to actually open")

    print()
    print("A title match is enough to work, never enough to name a store")
    pin = DocumentPin()
    pin.check(FRAGMENT_REPLY)
    pin.check(READ_REPLY)
    check(pin.project_key is None,
          "two titles agreeing does not make a project key believable - D-33: "
          "one client's knowledge must not be written into another's file")
    pin = DocumentPin()
    pin.check(READ_REPLY)
    pin.check(FRAGMENT_REPLY)
    check(pin.project_key == MODEL,
          "but a key already held is not lost by a reply that omitted it")

    print()
    print("A path match DOES teach the pin, because a path is identity")
    pin = DocumentPin()
    pin.check({"document": "Tower A", "documentPath": r"C:\jobs\Tower A.rvt"})
    check(pin.project_key is None, "pinned by path alone, it names no store")
    check(pin.check({"document": "Tower A", "documentPath": r"C:\jobs\Tower A.rvt",
                     "projectKey": UID, "creationGuid": MODEL}) is None,
          "the same file again is the same document")
    check(pin.project_key == MODEL,
          "and the key it now carries can be believed - two models cannot "
          "occupy one path at one time")

    # --- THE GUARD, WHICH MUST STILL REFUSE --------------------------------
    print()
    print("Golden Rule 20 still fires - the user really does change model")

    pin = DocumentPin()
    pin.check(READ_REPLY)
    other = pin.check({"document": "Annexe", "projectKey": "99999999-dead-beef"})
    check(other is not None, "a different project key is refused")
    check("Project1" in (other or "") and "Annexe" in (other or ""),
          "and the refusal names both models")
    # THIS CHECK HELD THE SENTENCE "Nothing has been sent to Revit" UNTIL
    # 2026-11-11, and the sentence was the defect (FRAGMENT-ISSUES 5b-594,
    # 5b-1135): the pin is asked of a REPLY, so Revit had been asked and had
    # answered. Row 594's repair names this move - "the suite's rule moved to
    # it". The question it answers unasked is the same; the answer is true.
    check("Nothing has been sent to Revit" not in (other or "")
          and "Heron had already asked Revit, and Revit answered from Annexe" in (other or "")
          and "not showing that answer" in (other or ""),
          "and answers 'did it half-do something?' unasked, truthfully - Revit was "
          "asked and answered, and the answer is withheld (5b-1135): %s"
          % (other or "")[-260:])

    print()
    print("Two open models with the SAME name - the case the pin exists for")
    pin = DocumentPin()
    pin.check({"document": "Project1", "documentPath": r"C:\a\Project1.rvt"})
    twin = pin.check({"document": "Project1", "documentPath": r"C:\b\Project1.rvt"})
    check(twin is not None,
          "same title, different file -> still refused; title is not identity")
    check("DIFFERENT one with the same name" in (twin or ""),
          "and the refusal SAYS they share a name, instead of printing the "
          "same word on both sides of 'but' and reading like the bug above")
    check("Go back to Project1" not in (twin or ""),
          "it does not give an instruction the user cannot carry out")
    check("click back" not in (twin or "").lower(),
          "and still never says 'click back' - it cannot know the other is open")
    check("Nothing has been sent to Revit" not in (twin or "")
          and "Revit answered from the model in front" in (twin or ""),
          "nor that nothing was sent - it names the model in front, since the two "
          "share a name (5b-1135)")

    # --- 5b-1135: THE WRITE PATH SAYS WHAT THE ADD-IN REPORTED ------------
    print()
    print("After a change, the refusal says what the add-in reported (5b-1135)")
    import heron_write as WRITE
    server_text = TOOLS.server_text()
    reported = getattr(WRITE, "reported_elsewhere", None)
    check(reported is not None, "heron_write has the write path's own sentence")
    kept = {"document": "Annexe", "projectKey": "99999999-dead-beef",
            "verdict": "'move-elements' was KEPT: 4 elements moved."}
    did = (reported("MOVE_ELEMENTS", kept) if reported is not None else
           "MOVE_ELEMENTS WAS SENT and ran in Annexe.")
    check("MOVE_ELEMENTS WAS SENT" in did and "Annexe" in did
          and "'move-elements' was KEPT: 4 elements moved." in did
          and "not showing that as this chat's work" in did,
          "it names the capability, the model it ran in and the add-in's own verdict, "
          "whole: %s" % did[:160])
    silent = (reported("MOVE_ELEMENTS", {"document": "Annexe"})
              if reported is not None else "")
    check("did not say whether anything was kept" in silent,
          "and with no verdict - an older add-in - it says the add-in did not say, "
          "never that nothing happened")
    pin = DocumentPin()
    pin.check(READ_REPLY)
    try:
        after = pin.check(kept, did=did)
    except TypeError:
        # The pin as it stood takes no `did` (heron-ship 2a: fail, not crash).
        after = pin.check(kept)
    check(did in (after or "") and "Nothing has been sent to Revit" not in (after or "")
          and "not showing that answer" not in (after or ""),
          "the pin's refusal carries the write path's sentence in place of the read's, "
          "and never 'Nothing has been sent to Revit' after a change that may have "
          "been kept")
    check("Project1" in (after or "") and "Annexe" in (after or "")
          and "use this model" in (after or ""),
          "and still names both models and the way to move the chat on purpose")
    check(bool(after) and "will not change a model you did not point it at" not in after,
          "and does not promise beside it that Heron will not change a model the chat "
          "did not point it at - the add-in has just reported doing so")
    check("will not change a model you did not point it at" in (other or ""),
          "the promise stays where it is true: on a read, which changes nothing")

    # --- A CALLER THAT ASKS BEFORE A CHANGE SAYS THE CHANGE WAS NOT SENT ---
    # The move's preview and the dashboard's Colour read first and change
    # second. "Nothing has been sent to Revit" told their modeller outright
    # that nothing was moved or coloured; the sentence that replaced it is
    # true of the read and silent on the change (the review of 5b-1135).
    print()
    print("Before a change, the refusal still says the change was not sent")
    not_sent = "No move has been sent to Revit: nothing was moved, and there is nothing to approve."
    pin = DocumentPin()
    pin.check(READ_REPLY)
    asked_first = {"document": "Annexe", "projectKey": "99999999-dead-beef"}
    try:
        before = pin.check(asked_first, unsent=not_sent)
    except TypeError:
        # The pin as it stood takes no `unsent` (heron-ship 2a: fail, not crash).
        before = pin.check(asked_first)
    check(not_sent in (before or ""),
          "the caller's own plain sentence is in the refusal: nothing was moved")
    check("Heron had already asked Revit, and Revit answered from Annexe" in (before or "")
          and (before or "").find("Heron had already asked Revit") < (before or "").find(not_sent)
          and "Nothing has been sent to Revit" not in (before or ""),
          "after the sentence that is true of the read - which never says nothing was sent")
    check("will not change a model you did not point it at" in (before or ""),
          "and the promise is kept beside it: nothing has been changed")
    try:
        both = pin.check(kept, did=did, unsent=not_sent)
    except TypeError:
        both = ""
    check(bool(both) and not_sent not in both and did in both,
          "it is never said beside a change the add-in reported - `did` wins")
    for name, call in (("revit_preview_move", "reply, unsent="),
                       ("_dashboard_colour", "views, unsent=")):
        start = server_text.find("\ndef %s(" % name)
        body = server_text[start:server_text.find("\ndef ", start + 1)] if start >= 0 else ""
        check("pinned.check(" in body and call in body,
              "%s hands the pin its own sentence for the change it has not sent" % name)
    body = server_text[server_text.find("\ndef revit_change("):]
    body = body[:body.find("\n@server.tool()")]
    check("pinned.check(reply, did=reported_elsewhere(capability, reply))" in body,
          "revit_change hands the pin what the add-in reported - read as text, because "
          "CI cannot import the server")
    reads = [name for name in ("revit_read", "revit_levels", "revit_views")
             if "did=" in server_text[server_text.find("\ndef %s(" % name):][:
                 server_text[server_text.find("\ndef %s(" % name):].find("\n@server.tool()")]]
    check(not reads, "and the read tools hand it nothing: theirs is the pin's own "
          "sentence - %s" % (reads or "none does"))

    # --- D-157: REVIT'S UNDO SENT IN ANOTHER MODEL IS SAID TO HAVE BEEN SENT ---
    print()
    print("After revit_undo_last, the refusal says Undo WAS sent, and where (D-157)")
    undone = getattr(WRITE, "undo_sent_elsewhere", None)
    check(undone is not None, "heron_write has revit_undo_last's own sentence")
    posted = {"document": "Annexe", "documentPath": r"C:\jobs\Annexe.rvt",
              "projectKey": READ_REPLY.get("projectKey"), "sent": True,
              "undoing": "rename-views"}
    did = undone(posted) if undone is not None else "Nothing has been sent to Revit."
    check("Revit's Undo WAS SENT" in did and "Annexe" in did and "'rename-views'" in did
          and "Ctrl+Y" in did and "Nothing has been sent" not in did,
          "it says Undo was sent, in which model, for which job the add-in named, and "
          "how to put it back: %s" % did[:160])
    pin = DocumentPin()
    pin.check(dict(READ_REPLY, documentPath=r"C:\jobs\Tower.rvt"))
    try:
        after = pin.check(posted, did=did)
    except TypeError:
        after = pin.check(posted)
    check(after is not None and "Revit's Undo WAS SENT" in after
          and "Nothing has been sent to Revit" not in after
          and "will not change a model you did not point it at" not in after,
          "two models from ONE TEMPLATE share the Project Information id, and are still "
          "told apart by path - the refusal carries that sentence, and no promise that "
          "nothing was changed")
    body = server_text[server_text.find("\ndef revit_undo_last("):]
    body = body[:body.find("\n@server.tool()")]
    check("pinned.check(reply, did=undo_sent_elsewhere(reply))" in body
          and "_aim_at_pin(args)" in body and "if not pinned.is_pinned:" in body,
          "revit_undo_last hands the pin that sentence, names the pinned model as "
          "revit_change does, and refuses a chat with no pin - read as text")
    line = getattr(WRITE, "undo_line", None)
    check(line is not None, "heron_write has the kept reply's undo line")
    if line is not None:
        check("revit_undo_last" in (line({"undoState": "onTop"}) or "")
              and "Nothing to take back" in (line({"undoState": "nothing"}) or "")
              and "cannot take this back itself" in (line({"undoState": "unsure"}) or ""),
              "one line for each state the add-in's guard can hold after a keep")
        why = line({"undoState": "unsure", "undoWhy": "an Undo Heron sent earlier has not "
                    "been seen to run; it may still run and take this change"}) or ""
        check("an Undo Heron sent earlier has not been seen to run" in why
              and "missed something" not in why and "\n" not in why,
              "an unsure state says the add-in's own reason when it gives one - an Undo "
              "still outstanding is said as that")
        check(line({}) is None and line({"undoState": "something new"}) is None
              and line(None) is None,
              "and none for a reply that names no state, or one this side does not know - "
              "nothing is guessed")

    print()
    print("A fragment reply cannot smuggle a write into a differently-named model")
    pin = DocumentPin()
    pin.check(READ_REPLY)
    check(pin.check({"document": "Annexe", "ran": "move-elements"}) is not None,
          "no key on the reply, but the titles disagree -> refused on the best "
          "evidence there is, rather than waved through")

    # --- 5b-324: TWO MODELS FROM ONE TEMPLATE ARE TWO PROJECTS -------------
    print()
    print("Two models made from one template are two projects (5b-324)")
    print("  Measured 2026-10-06: 'Heron loads test', a new project from the")
    print("  default template, reported the SAME Project Information id as the")
    print("  Project2 whose loads answers were kept on 2026-10-05 - so its loads")
    print("  run asked nothing and used Project2's weather, set points, Office")
    print("  profile and an element id from Project2.")

    first, second = DocumentPin(), DocumentPin()
    first.check(LOADS_TEST)
    second.check(PROJECT2)
    check(first.project_key is not None and second.project_key is not None
          and first.project_key != second.project_key,
          "two models made from the same template name two DIFFERENT stores "
          "(%r, %r) - the Project Information id they share is the "
          "template's, not theirs" % (first.project_key, second.project_key))
    check(first.project_key == LOADS_TEST["creationGuid"],
          "and the store is named after the model's own id, "
          "Document.CreationGUID, which the add-in sends as creationGuid")

    pin = DocumentPin()
    pin.check(LOADS_TEST)
    other = pin.check(PROJECT2)
    check(other is not None,
          "a chat pinned to one of them REFUSES a reply from the other - the "
          "shared id used to decide that they were the same model")
    check(pin.title == "Heron loads test",
          "and the pin still names the model it was pointed at - it used to "
          "take the other model's name, so the next aimed write went there")
    check(pin.project_key == LOADS_TEST["creationGuid"],
          "and its store stays this model's")

    check(getattr(pin, "project_information_id", None) == TEMPLATE_UID,
          "the shared id is still known, by its own name: the add-in's "
          "expectProject guard compares it, and answers kept under it before "
          "this change are found by it - never used by it")

    old = DocumentPin()
    old.check({"document": "PIPE", "documentPath": r"C:\x\PIPE.rvt",
               "projectKey": TEMPLATE_UID})
    check(old.is_pinned and old.project_key is None,
          "a reply that carries ONLY the shared id - Revit 2020 to 2023, which "
          "have no CreationGUID, or an add-in older than this change - names "
          "no store at all, rather than the store every template-born "
          "project shares")

    print()
    print("The model's own id follows it through Save As")
    pin = DocumentPin()
    pin.check(LOADS_TEST)
    saved_as = dict(LOADS_TEST, document="Heron loads test v2",
                    documentPath=r"C:\Users\x\Desktop\test\Heron loads test v2.rvt")
    check(pin.check(saved_as) is None,
          "the same model saved under a new name is not refused - its "
          "CreationGUID did not change")
    check(pin.project_key == LOADS_TEST["creationGuid"],
          "and its answers stay where they were kept")

    print()
    print("A title match still never names a store")
    pin = DocumentPin()
    pin.check(FRAGMENT_REPLY)
    pin.check(READ_REPLY)
    check(pin.project_key is None,
          "a pin set on a title alone learns no model id from a reply that "
          "merely shares the title")

    print()
    print("A DIFFERENT Project Information id still proves a different model")
    print("  The same id proves nothing - two models from one template share it -")
    print("  but two different ones cannot be one model. Found by the review of")
    print("  this change: dropping the id from the comparison let a reply with no")
    print("  path and another model's id through on the title alone.")
    pin = DocumentPin()
    pin.check({"document": "Tower", "documentPath": r"C:\a\Tower.rvt",
               "projectKey": "PI-A"})
    check(pin.check({"document": "Tower", "projectKey": "PI-B"}) is not None,
          "a reply with another Project Information id is refused, even when "
          "the title matches and there is no path to compare")
    check(pin.check({"document": "Tower", "projectKey": "PI-A"}) is None,
          "while the same id and the same title still pass")
    pin = DocumentPin()
    pin.check({"document": "Annexe", "projectKey": "PI-A"})
    check(pin.check({"document": "Annexe", "projectKey": "PI-B",
                     "creationGuid": MODEL}) is not None,
          "and it is refused when the replies share nothing else at all")

    # --- THE C# HALF, as far as it can be checked from here ----------------
    print()
    print("The add-in sends the identity on the fragment reply too")
    fragment = io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin",
                                    "RevitFragment.cs"), encoding="utf-8").read()
    check('Json.Str("projectKey", projectKey)' in fragment,
          "RevitFragment.Report puts projectKey on the wire - the server had "
          "no way to obtain one from a fragment run before this")
    check('Json.Str("documentPath"' in fragment,
          "and documentPath, so a saved model degrades no further than a path")
    check("RevitOperations.ProjectKey(target)" in fragment,
          "using the SAME key every other op sends, not a second definition "
          "of identity that could drift from it")
    check('Json.Str("creationGuid", creationGuid)' in fragment
          and "RevitOperations.CreationGuid(target)" in fragment,
          "and the model's own id beside it, from the one helper every op "
          "uses (5b-324)")

    # EVERY REPLY THAT CARRIES THE SHARED ID CARRIES THE MODEL'S OWN ID TOO.
    # The pin learns its store key from whichever reply arrives first, so one
    # operation that forgot would leave a chat that started there with no
    # store at all - the gap row 5b-324's fix must not open.
    addin = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
    for name in sorted(os.listdir(addin)):
        if not name.endswith(".cs"):
            continue
        text = io.open(os.path.join(addin, name), encoding="utf-8").read()
        shared = text.count('Json.Str("projectKey"')
        if not shared:
            continue
        own = text.count('Json.Str("creationGuid"')
        check(own == shared,
              "%s sends the model's own id beside the shared one in every "
              "reply (%d shared, %d own)" % (name, shared, own))
    operations = io.open(os.path.join(addin, "RevitOperations.cs"),
                         encoding="utf-8").read()
    check('GetProperty("CreationGUID")' in operations,
          "CreationGuid reads Document.CreationGUID by reflection - it "
          "arrived at Revit 2024, so a direct call would not compile for "
          "2020 to 2023 (the revit-version-support skill's table)")

    # ---------------------------------------------------------------
    print()
    print("E11 failed in front of Revit, so the write is now AIMED, not guarded")
    print("  ProjectKey is ProjectInformation.UniqueId, which comes from the")
    print("  TEMPLATE. Two blank projects and an unrelated PIPE.rvt in another")
    print("  Revit release all reported 8764c510-...-0000c160, so the guard")
    print("  compared two equal strings and let CREATE_LEVEL run in Project2 -")
    print("  the model the chat was NOT pointed at. Measured 2026-09-15.")

    pin = DocumentPin()
    pin.check({"document": "Project1", "documentPath": r"C:\jobs\Project1.rvt",
               "projectKey": UID})
    check(pin.document_path == r"C:\jobs\Project1.rvt",
          "the pin exposes the PATH, which is the identity that tells two open "
          "models apart when they share a name - project keys cannot, and that "
          "is what E11 proved")

    unsaved = DocumentPin()
    unsaved.check({"document": "Project1", "projectKey": UID})
    check(unsaved.document_path is None,
          "and it is None for an UNSAVED model, so the title is what travels - "
          "which is why the add-in has to refuse an ambiguous title rather "
          "than pick one")

    server = TOOLS.server_text()
    check('args["document"] = pinned.title' in server,
          "revit_change AIMS the write at the pinned model by name instead of "
          "letting the add-in fall back to whatever is in front. That inverts "
          "the question from 'did the user move?' - which needs a key that "
          "works - to 'which model was I told to work on?', which needs none")
    check('args["documentPath"] = pinned.document_path' in server,
          "and sends the path too, strongest first")
    check("if pinned.title:" in server,
          "and sends NOTHING before a pin exists - the first request has no "
          "model to aim at and is what obtains the pin")

    check('Json.ReadString(request, "documentPath")' in fragment,
          "the add-in reads the path")
    check('Json.Error("ambiguous_document"' in fragment,
          "and REFUSES when two open models share the name, instead of letting "
          "the last one round the loop win silently - which is the worst "
          "answer available on a path about to commit a transaction, because "
          "it is indistinguishable from a correct one and depends on the order "
          "Revit hands its documents back")
    check(fragment.index('Json.ReadString(request, "documentPath")')
          < fragment.index("new TransactionGroup"),
          "and resolves the target before the transaction group opens, like "
          "the guard beside it")

    # THE GUARD MUST BE SENT THE ID THE ADD-IN COMPARES (5b-324). The add-in
    # compares RevitOperations.ProjectKey, the shared id; `pinned.project_key`
    # is the model's own id now, and sent there it would refuse every request
    # as `wrong_document`. Checked as an ABSENCE, because a presence check
    # passes while one site of four has gone back.
    check('"expectProject": pinned.project_key' not in server
          and 'args["expectProject"] = pinned.project_key' not in server,
          "no request sends the model's own id where the add-in compares the "
          "shared one")

    # EVERY ANSWER THAT READS A PROJECT'S KEPT ANSWERS ASKS ABOUT THE SHARED
    # ID'S FIRST (5b-324) - the sprinkler layout arrived in #418 while this
    # change was open, reading the project's fire standards too.
    for tool in ("revit_building_loads", "revit_sprinkler_hydraulics",
                 "revit_sprinkler_layout", "heron_hvac", "heron_fire",
                 "heron_standards", "heron_research"):
        start = server.find("def %s(" % tool)
        end = server.find("@server.tool()", start)
        body = server[start:end] if start >= 0 else ""
        check("_with_project_note(" in body,
              "%s puts the earlier-answers question at the top of its answer" % tool)

    print()
    if FAILURES:
        print("FAILED")
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1

    print("PASSED - one model answers as one model, and a second one is "
          "still refused.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
