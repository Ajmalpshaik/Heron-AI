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

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


# The identity of ONE unsaved model called Project1, as each side reports it.
# Unsaved on purpose: no documentPath, which is the case with nothing to fall
# back on. UID is the Project Information UniqueId RevitOperations.ProjectKey
# returns.
UID = "8a1f0c22-0000-4b7e-9d31-2f4c6b8e0a15-0003f2a1"
READ_REPLY = {"ok": True, "document": "Project1", "projectKey": UID}
FRAGMENT_REPLY = {"ok": True, "document": "Project1", "ran": "move-elements"}


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
    check(pin.project_key == UID,
          "but a key already held is not lost by a reply that omitted it")

    print()
    print("A path match DOES teach the pin, because a path is identity")
    pin = DocumentPin()
    pin.check({"document": "Tower A", "documentPath": r"C:\jobs\Tower A.rvt"})
    check(pin.project_key is None, "pinned by path alone, it names no store")
    check(pin.check({"document": "Tower A", "documentPath": r"C:\jobs\Tower A.rvt",
                     "projectKey": UID}) is None,
          "the same file again is the same document")
    check(pin.project_key == UID,
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
    check("Nothing has been sent to Revit" in (other or ""),
          "and answers 'did it half-do something?' unasked")

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

    print()
    print("A fragment reply cannot smuggle a write into a differently-named model")
    pin = DocumentPin()
    pin.check(READ_REPLY)
    check(pin.check({"document": "Annexe", "ran": "move-elements"}) is not None,
          "no key on the reply, but the titles disagree -> refused on the best "
          "evidence there is, rather than waved through")

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
