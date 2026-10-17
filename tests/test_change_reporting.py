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

AND IT MUST FAIL ON THE OLD KEY, or it is decoration. Row 111's own state line
asks for that: without it the next rename of a reply field puts the bug
straight back, silently, exactly as it arrived.

AND THE REPLY IS SHORT NOW - FRAGMENT-ISSUES 5b-407
----------------------------------------------------
Two family builds carried 322,725 characters of these replies back into the
chat, a quarter of it the same KEPT paragraph. The add-in now counts what
Revit says changed, keeps every element and the whole reply in a run record,
and writes the KEPT verdict twice - whole, and as one sentence carrying the
undo rule unshortened. The last section REPLAYS a family build's batches
through what revit_change prints, as it printed them before and as it prints
them now, and fails unless the new is measurably shorter while every reply
still carries the undo rule. The replay's replies are SHAPED from
CREATE_FAMILY_EXTRUSION's own code - the variables its script leaves, the
verdict word for word - not captured from a chat; the 2026-10-09 transcript
is on the owner's PC, not in this repository. The same replay through the
real tool and a real SDK is tests/test_mcp_serves.py's.

    python tests/test_change_reporting.py
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

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
    # 5b-407. THE SHORT FORM IS THE ADD-IN'S, AND ONLY FOR A KEPT CHANGE.
    # ------------------------------------------------------------------
    check('Json.Str("verdictShort", brief)' in fragment
          and re.search(r'brief\s*=\s*"\'"\s*\+\s*name\s*\+\s*"\' was KEPT\. "\s*\+\s*UndoRule;',
                        fragment) is not None,
          "the add-in writes the one-sentence form itself, from the same undo rule "
          "as the paragraph - the server only chooses between the two")
    check(fragment.count("brief = \"'\"") == 1,
          "and writes it for a KEPT change only: a rollback, and one Revit did not "
          "confirm, have no short form and are printed whole every time")
    check('"verdictKind"' in fragment and 'reply.get("verdict")' in server,
          "beside the kind, while `verdict` stays whole for any reader that knows "
          "neither field")

    replay(server)

    if FAILURES:
        print("FAILED")
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1

    print("PASSED - a write now reports what it did, and a refusal is visible "
          "as a refusal.")
    return 0


# THE VERDICTS, word for word as RevitFragment.WithVerdict writes them: before
# the change report (no event was counted), and with it.
UNDO = ("IF anything was written, all of it is ONE undo step; if nothing was, there is "
        "no new undo entry and Ctrl+Z would undo whatever you did before this.")
OLD_KEPT = ("'%s' was KEPT: Revit accepted the transaction rather than rolling it back. "
            "THE COUNTS ABOVE ARE WHAT THIS FRAGMENT DID and nothing more - anything a "
            "setup step did was kept in the same group and is not counted here, so counts "
            "of zero do not by themselves mean the model is untouched. " + UNDO)
NEW_KEPT = ("'%s' was KEPT: Revit accepted the transaction rather than rolling it back. "
            "THE COUNTS ABOVE ARE WHAT THIS FRAGMENT DID and nothing more - what its setup "
            "steps did is counted apart under changes, and counts of zero do not by "
            "themselves mean the model is untouched. " + UNDO)
BRIEF = "'%s' was KEPT. " + UNDO

# 2026-10-09's afternoon in proportion: 172 kept changes in 39 batches
# (FRAGMENT-ISSUES 5b-378), sixteen batches of five and twenty-three of four.
BATCHES = [5] * 16 + [4] * 23


def extrusion_reply(n, new):
    """One CREATE_FAMILY_EXTRUSION reply: every variable its script leaves at top
    level, rendered as RevitFragment.Describe renders it. `new` adds what the
    add-in sends since the change report."""
    vendor = "Auto" + "desk"
    name = "create-family-extrusion"
    reply = {
        "ok": True, "ran": name, "document": "Panel",
        "bound": "workPlane, profile, startMm, endMm and isSolid as given",
        "provides": {
            # EVERY FORM IS ITS OWN: its id, what was built and the finding
            # that says so differ on every call, as they did on the day - so
            # only what really repeats can be collapsed.
            "findings": "1 item(s) [Solid extrusion on \"Ref. Level\": Z 0 to %d mm]" % (40 + n),
            "formId": "b753cb5a-f4f9-4171-9428-41eb2f7129c0-%08x" % (0xb89 + n),
            "built": ("Solid extrusion on \"Ref. Level\": 1 outline, 0 holes; X -300 to 300, "
                      "Y -200 to 200, Z 0 to %d mm; volume %.4f m3" % (40 + n, 0.0024 * (40 + n) / 10)),
            "notAFamily": "false", "refused": "(null)", "invariant": "CultureInfo",
            "mm": "Func`2", "plain": "Func`2", "halfMillimetre": "0.00164041994750656",
            "axisOf": "Func`2", "along": "Func`3", "axisLetters": "3 item(s) [X, Y, Z]",
            "facing": "3 item(s) [facing left or right, facing front or back, horizontal]",
            "inPlane": "3 item(s) [System.Int32[], System.Int32[], System.Int32[]]",
            "ownName": "Func`2", "number": "Func`2", "pair": "Func`2",
            "problems": "0 item(s)", "shortest": "0.79375", "loopOf": "Func`3",
            "datums": "3 item(s) [(%s.Revit.DB.ReferencePlane, 1, 0, Center (Front/Back)), "
                      "(%s.Revit.DB.ReferencePlane, 0, 0, Center (Left/Right)), "
                      "(%s.Revit.DB.Level, 2, 0, Ref. Level)]" % (vendor, vendor, vendor),
            "plane": "Tuple`4",
            "loops": "1 item(s) [System.Collections.Generic.List`1[System.Double[]]]",
        },
        "applied": True, "rolledBack": False, "warnings": 0, "warningsDismissed": [],
        "verdict": OLD_KEPT % name,
    }
    if new:
        run = "wf-20261009-1334%02d-%04d-a1b2" % (n % 60, n)
        reply.update({
            "verdict": NEW_KEPT % name, "verdictKind": "kept", "verdictShort": BRIEF % name,
            "run": run, "elementsAdded": 6, "elementsModified": 1, "elementsDeleted": 0,
            "changeEvents": 1,
            "changes": {"events": 1, "unreadable": 0, "witnessed": True, "total": 7,
                        "fragment": {"added": 6, "modified": 1, "deleted": 0, "total": 7,
                                     "byCategory": [
                                         {"category": "Lines", "added": 4, "modified": 0},
                                         {"category": "Generic Models", "added": 1, "modified": 0},
                                         {"category": "(no category)", "added": 1, "modified": 1}],
                                     "firstIds": [str(4000 + 7 * n + k) for k in range(6)]},
                        "setup": {"added": 0, "modified": 0, "deleted": 0, "total": 0,
                                  "byCategory": [], "firstIds": []}},
        })
    return reply


def lift(text, name):
    """One helper out of the server text, run alone - test_read_door.py's way,
    because importing the server needs the MCP SDK and CI has none."""
    nl = chr(10)
    start = text.find(nl + "def %s(" % name)
    end = text.find(nl + nl + nl, start) if start >= 0 else -1
    if start < 0 or end < 0:
        return None
    namespace = {}
    try:
        exec(text[start + 1:end], namespace)              # noqa: S102
    except BaseException:                                 # noqa: BLE001
        return None
    return namespace.get(name)


def printed(reply, provides_lines, runs, batch, started, finished):
    """What revit_change prints for one reply, in revit_change's own order -
    held to it by the order check in replay(). `runs` None prints the way it
    did before the change report: every value, every verdict whole."""
    capability = "CREATE_FAMILY_EXTRUSION"
    if runs is not None:
        batch.turn(started, finished)
    lines = ["%s ran in %s." % (capability, reply.get("document"))]
    if reply.get("bound"):
        lines.append("  inputs: %s" % reply["bound"])
    if runs is not None:
        lines.extend(runs.change_lines(reply))
        shown, same = runs.shown_values(reply, capability, batch)
        lines.extend(provides_lines(shown) or ([""] if same else []))
        lines.extend(runs.unchanged_line(same))
    else:
        lines.extend(provides_lines(reply.get("provides")))
    if reply.get("verdict"):
        lines.append("")
        lines.append(runs.verdict_line(reply, batch) if runs is not None
                     else str(reply["verdict"]))
    lines.append("")
    lines.append("'%s' is DRAFT, not PROVEN - it has no recorded proof carrying a "
                 "negative case. Look at what it did before trusting it." % capability)
    return "\n".join(lines)


def replay(server):
    print("Replaying a family build's batches through what revit_change prints")
    change = server.split("\ndef revit_change(", 1)[-1].split("\n@server.tool()", 1)[0]
    order = [change.find(s) for s in ("_BATCH.turn(started, time.monotonic())",
                                      'lines.append("  inputs: %s" % bound)',
                                      "runs.change_lines(reply)",
                                      "_ignored_lines(undeclared, takeable)",
                                      "runs.shown_values(reply, capability, _BATCH)",
                                      'lines.extend(_provides_lines(shown) or ([""] if same else []))',
                                      "runs.unchanged_line(same)",
                                      "runs.verdict_line(reply, _BATCH)")]
    check(all(o > 0 for o in order) and order == sorted(order),
          "revit_change places the call in its batch, then prints inputs, the change "
          "lines, the ignored values, what the fragment left and then the verdict - the "
          "order this replay prints them in")

    provides_lines = lift(server, "_provides_lines")
    check(provides_lines is not None, "_provides_lines can be lifted out of the server")
    try:
        import heron_run_records as runs
    except ImportError as why:
        check(False, "heron_run_records imports (%s)" % why)
        runs = None
    if provides_lines is None or runs is None:
        return

    before, after, briefs, wholes = [], [], 0, 0
    batch = runs.Batch()
    clock, n = 1000.0, 0
    for size in BATCHES:
        for _ in range(size):
            before.append(printed(extrusion_reply(n, False), provides_lines, None,
                                  None, clock, clock + 0.03))
            text = printed(extrusion_reply(n, True), provides_lines, runs,
                           batch, clock, clock + 0.03)
            after.append(text)
            briefs += BRIEF % "create-family-extrusion" in text
            wholes += NEW_KEPT % "create-family-extrusion" in text
            clock += 0.05
            n += 1
        clock += 15.0       # a model turn between batches

    old, new = sum(len(t) for t in before), sum(len(t) for t in after)
    print("  %d replies in %d batches: %d characters as revit_change printed them before, "
          "%d now (%.1f%% shorter)" % (n, len(BATCHES), old, new, 100.0 * (old - new) / old))
    check(n == 172 and wholes == len(BATCHES) and briefs == n - len(BATCHES),
          "the KEPT paragraph is printed once per batch - %d times for %d batches - and "
          "the one-sentence form on the other %d" % (wholes, len(BATCHES), briefs))
    check(all(UNDO in text for text in after),
          "every reply, whole or brief, still carries the undo rule word for word "
          "(Article 8)")
    check(all("record:  heron_run_record run=wf-" in text and "changed: 6 added" in text
              for text in after),
          "every reply says what Revit changed and names the record that holds every id")
    check(all("formId" in text and "built" in text for text in after),
          "and every reply still prints the values that differ from the last - the new "
          "form's id and what was built")
    collapsed = sum("unchanged from the last reply - every one is in the record" in text
                    for text in after)
    check(collapsed == n - len(BATCHES),
          "while what the last reply showed word for word is counted, not printed, on "
          "every reply but a batch's first - %d of %d" % (collapsed, n))
    # THE FLOOR IS A GUARD, NOT THE CLAIM. The measured figure is printed above;
    # this fails only if a later change gives most of it back.
    check(new < old * 0.80,
          "and the replay is measurably shorter - at least 20%% less: %d -> %d" % (old, new))


if __name__ == "__main__":
    sys.exit(main())
