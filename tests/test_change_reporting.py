#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
What `revit_change` tells the user a write actually did.

WHY THIS FILE EXISTS
--------------------
Until 2026-09-16 it told them nothing. `heron_mcp_server.py` printed
`"<CAPABILITY> ran in <model>"` and then looked for the detail under
`reply.get("answer")` - A KEY NOTHING EMITS, on either side of the bridge.

  * `RevitFragment.Report` leaves `provides`, `providesCount`, `bound`, `ran`,
    `document`.
  * `WithVerdict` adds `applied`, `rolledBack` and `verdict`.
  * Nothing, anywhere, writes `answer`.

So the value was always `None`, and EVERY write through that tool reported
only that it had run. Measured: `RENAME_PHASE` answered `"RENAME_PHASE ran in
PIPE."` while the fragment had produced `renamed: false` and a refusal carrying
Revit's own words - *"This element does not support assignment of a
user-specified name."* The model was untouched and the tool said nothing about
it either way. FRAGMENT-ISSUES row 111.

THE POINT OF TESTING IT AS TEXT
--------------------------------
`revit_change` needs a live bridge and the MCP SDK to call, so every other test
here reads the server as TEXT and asserts on what it contains. That is a weak
test of behaviour and a STRONG test of this particular defect, which was a
field name and nothing else.

AND ONE THING IT NO LONGER SAYS AFTER THE FACT. "Not PROVEN" was a line
under the result of a change already kept. Article 11 says untested code
never touches a live model, so since D-117 that is said INSTEAD of sending
anything - and the last section here holds the refusal before the send.

AND IT MUST FAIL ON THE OLD KEY, or it is decoration. Row 111's own state line
asks for that: without it the next rename of a reply field puts the bug
straight back, silently, exactly as it arrived.

    python tests/test_change_reporting.py
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

FAILURES = []


def check(condition, what):
    if not condition:
        FAILURES.append(what)


def main():
    server = io.open(os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py"),
                     encoding="utf-8").read()
    fragment = io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin",
                                    "RevitFragment.cs"), encoding="utf-8").read()

    # ------------------------------------------------------------------
    # THE DEFECT ITSELF. A read of a key no producer writes.
    # ------------------------------------------------------------------
    live = [line for line in server.splitlines()
            if 'reply.get("answer")' in line and not line.lstrip().startswith("#")]
    check(not live,
          "revit_change does NOT read reply.get('answer') - a key neither side "
          "emits, so the verdict was always None and every write reported only "
          "that it ran (FRAGMENT-ISSUES row 111). A mention in a comment is "
          "fine; a live read is the bug")

    check('reply.get("verdict")' in server,
          "it reads `verdict` instead - the field WithVerdict actually adds, "
          "and the add-in's own words rather than a sentence composed here "
          "from `applied`, which is the request and not the result")

    check('reply.get("provides")' in server,
          "and it prints `provides`, because A REFUSAL LIVES THERE AND NOWHERE "
          "ELSE. A fragment that declined says so in its results, and 'it ran' "
          "is the one sentence that makes a refusal look like a success")

    check('reply.get("bound")' in server,
          "and the binding note, which is the line a proof is judged on "
          "(fragment-proving rule 5) - the difference between a fragment that "
          "ran on the selection and one that ran on a stale carried value")

    # ------------------------------------------------------------------
    # THE OTHER SIDE. These are what makes the keys above the right ones,
    # so a rename there fails HERE rather than in front of a modeller.
    # ------------------------------------------------------------------
    check('Json.Str("verdict", verdict)' in fragment,
          "the add-in emits `verdict` - if this line is renamed, the server's "
          "read above is wrong again and this test is where that surfaces")

    check('parts.Add("\\"provides\\":{" + string.Join(",", left) + "}")' in fragment,
          "and emits `provides` as the fragment's own read-back")

    check('"answer"' not in fragment,
          "and never emits `answer` at all - which is what made the server's "
          "read dead rather than merely wrong")

    # ------------------------------------------------------------------
    # The verdict's THREE cases must all survive, because the third is the
    # one that matters and the easiest to lose.
    # ------------------------------------------------------------------
    check("THE ROLLBACK DID NOT REPORT SUCCESS" in fragment,
          "the add-in still distinguishes a rollback that reported success "
          "from one that did not - on 2026-09-09 a rollback did not hold while "
          "the client claimed it had, and a write that may still be standing "
          "is the one thing a reader must not have to guess at")

    # ------------------------------------------------------------------
    # ROW 117. THE `applied` BRANCH WAS THE ONE STILL SPEAKING FROM THE
    # REQUEST, and it was only visible because row 111 above shipped: before
    # that the whole reply was "RENAME_ELEMENTS ran in test projject" and the
    # contradiction had nowhere to show.
    # ------------------------------------------------------------------
    claiming = [line for line in fragment.splitlines()
                if "the model was CHANGED" in line
                and not line.lstrip().startswith("//")]
    check(not claiming,
          "the add-in no longer ASSERTS 'the model was CHANGED' - measured "
          "2026-09-17, a RENAME_ELEMENTS that matched nothing reported "
          "`renamed 0` and said it anyway, because the sentence came from "
          "`applied`, the request to keep the transaction, and not from any "
          "work having happened (FRAGMENT-ISSUES row 117). As with `answer` "
          "above, a mention in a comment is fine; a live claim is the bug")

    check("was KEPT: Revit accepted the transaction" in fragment,
          "it says what was KEPT instead - the transaction surviving is the "
          "thing this side can actually witness. Which counts mean WORK is "
          "D-52's `role` and lives in fragment.yaml, which the add-in is "
          "never handed, so deciding here would be a guess")

    check("THE COUNTS ABOVE ARE WHAT THIS FRAGMENT DID" in fragment,
          "and sends the reader to the counts, which are already in the same "
          "reply - 'every number is zero' would not have worked either, "
          "because `notMatched 10` was non-zero in the run that changed "
          "nothing")

    check("IF anything was written, all of it is ONE" in fragment
          and "no new undo entry" in fragment,
          "and the Ctrl+Z instruction is CONDITIONAL - if nothing was written "
          "there may be no new undo entry, and a modeller told to press it "
          "undoes whatever they did BEFORE. Row 117's first repair made this "
          "conditional; rewording it for the setup-steps case put the "
          "unconditional promise straight back, half an hour later")

    check("THE COUNTS ABOVE ARE WHAT THIS FRAGMENT DID" in fragment
          and "do not by themselves" in fragment,
          "and it does not conclude the model is UNTOUCHED from counts of "
          "zero - a request carrying deferred write SETUP steps runs them in "
          "the same group and Assimilate keeps what they did, so the counts "
          "of the fragment under test cannot speak for the whole group. Found "
          "by review on PR #198, and it is row 117's own mistake made twice")

    # ------------------------------------------------------------------
    # ARTICLE 11 (D-117). "Not PROVEN" used to be said AFTER the change was
    # kept - a line under the result. It is said instead of sending anything.
    # ------------------------------------------------------------------
    start = server.find("\ndef revit_change(")
    body = server[start:server.find("\n@server.tool()", start)] if start > 0 else ""
    code = "\n".join(line for line in body.splitlines()
                     if not line.lstrip().startswith("#"))
    check(start > 0, "revit_change is still in the server")
    check("not PROVEN - it has no recorded proof" not in code
          and "Look at what it did before trusting it" not in code,
          "revit_change no longer warns AFTER the run that what it kept was not "
          "proven - by then the change was already in the model")

    gate = code.find("bridge.unproven_refusal(capability, brain.proof_status(folder))")
    risk = code.find("risk_refusal(root, folder, switched=True)")
    source = code.find("fh.read()")
    bind = code.find("binding.resolve(")
    sent = code.find("session.request(")
    # PRESENCE FIRST: `find` gives -1 for what is not there, and an ordering
    # check then passes loudest when the guard it orders is gone (heron-ship 2a).
    check(gate > 0, "it asks the client's unproven_refusal with the fragment's own "
                    "proof status, read from its files")
    check(gate > 0 and risk > 0 and risk < gate,
          "after the risk refusal, so a level that is never sent is still said first")
    check(gate > 0 and source > 0 and bind > 0 and sent > 0
          and gate < source and gate < bind and gate < sent,
          "and BEFORE the code is read, a session is bound or anything is sent - "
          "the refusal touches nothing")
    check('return _ended("refused", unproven)' in code,
          "and a refusal is told to the activity list as one")

    # ------------------------------------------------------------------
    # D-126. REVIT ASKS KEEP OR DISCARD BEFORE A CHANGE IS KEPT - D-119's box,
    # one box for both doors - and the reply must say who decided: a change
    # somebody looked at and a change kept with nobody asked read the same
    # otherwise.
    # ------------------------------------------------------------------
    # READ SO THAT ITS ABSENCE FAILS RATHER THAN CRASHES (heron-ship 2a):
    # against the code from before D-126 every check below must say so.
    asked_path = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronKeepQuestion.cs")
    question = (io.open(asked_path, encoding="utf-8").read()
                if os.path.exists(asked_path) else "")
    check(bool(question), "HeronKeepQuestion.cs holds the words the modeller and the chat read")
    check("return answered != null ? WithAnswer(verdict, answered, refreshed) : verdict;" in fragment,
          "a reply the modeller answered carries D-119's asked, modellerAnswer and kept - "
          "for a keep from a chat as for a DRAFT's one run")
    check("HeronKeepQuestion.NothingToAskSentence" in fragment
          and "was not asked." in question,
          "and a run kept with no question - Revit counted no change - says it was "
          "not asked, so a question that never appeared is not read as answered")

    # A DISCARD IS THE MODELLER'S ANSWER, NOT A ROLLBACK TO TRY AGAIN. Its
    # verdict is its own - never the plain rollback's "send it again with
    # apply" - and revit_change ends it as refused, with no after-change table.
    check("verdict = HeronKeepQuestion.Discarded(name, held, selectionRestored)" in fragment,
          "a Discard's verdict is HeronKeepQuestion.Discarded, with whether the rollback held")
    check("NOTHING WAS KEPT" in question and "MAY STILL HOLD" in question
          and "send it again only if they ask" in question,
          "which says NOTHING WAS KEPT only when the rollback held, otherwise "
          "that the model may still hold the change - and never to resend it unasked")
    discard = server.find('if reply.get("asked") and not reply.get("kept"):')
    offer = server.find("_offer_change(capability, folder, values")
    check(0 < discard < offer,
          "revit_change ends a box the modeller did not keep as refused, before any "
          "after-change table is offered")
    check("There is no preview (D-99)" not in server and "THERE IS NO PREVIEW" not in server,
          "and its description no longer says there is no preview - D-126 superseded D-99")

    if FAILURES:
        print("FAILED")
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1

    print("PASSED - a write now reports what it did, a refusal is visible "
          "as a refusal, and an unproven change is refused before it is sent.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
