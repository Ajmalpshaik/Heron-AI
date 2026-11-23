#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Consuming what an earlier fragment left, without consuming something else.

WHAT THIS GUARDS
----------------
The chain is reset between calls, and the add-in's own comment says why: a
fragment run an hour later would otherwise bind *"elements collected by
something nobody remembers running"*. That reset is right, and it is kept.

The cost was that no two fragments could be joined at all. `select-by-categories`
takes a list and works; `set-selection` needs its result FROM the chain; so
*"isolate the pipes and the fittings"* - one sentence, one ordinary job - could
not be done. Measured 2026-09-16.

`expectChain` is the way out that does not give the reset up. The caller names
the producer it means to consume; the executor checks the carried values were
left by THAT fragment, with the inputs the caller named, BEFORE it binds
anything; a mismatch refuses. Declare nothing and the old behaviour is exactly
what happens. docs/36.

WHY THE CHECKS BELOW ARE MOSTLY ABOUT REFUSALS
-----------------------------------------------
A guard that never says no is worse than no guard, because it is believed. So
what is asserted here is that each distinct fault has its OWN refusal and its
own name:

    chain_empty            the producer never ran, or ran on another model
    chain_mismatch         a DIFFERENT fragment left these
    chain_inputs_differ    the right fragment, the wrong inputs
    chain_contradiction    reset and expect in one request, which can only fail
    chain_taken_back       left by a run that was taken back - a dry run, a
                           proof's trial, a Discard (Q-89, 2026-11-16)

Those are five different things to go and fix, and collapsing them into "the
chain does not match" would send a reader to the wrong fragment.

AND IT IS A TEXT TEST, DELIBERATELY. The binder needs Revit; every other test
here reads the C# as text for the same reason. That is weak evidence about
behaviour and strong evidence about a guard being present and reachable - which
is what silently disappears when somebody simplifies a method.

    python tests/test_chain_expectation.py
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_tools as TOOLS                                     # noqa: E402

FAILURES = []


def check(condition, what):
    if not condition:
        FAILURES.append(what)


def main():
    fragment = io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin",
                                    "RevitFragment.cs"), encoding="utf-8").read()
    client = io.open(os.path.join(ROOT, "mcp", "client",
                                  "heron_bridge_client.py"), encoding="utf-8").read()
    server = TOOLS.server_text()

    # ------------------------------------------------------------------
    # THE DEFAULT MUST NOT MOVE. Everything already written depends on it.
    # ------------------------------------------------------------------
    check('Forget(client)' in fragment,
          "the chain is still reset when asked - the expectation is opt-in and "
          "does NOT replace the reset, or every proof and job file already "
          "written would quietly start consuming what an earlier run left")

    check('args["chain"] = "reset"' in client,
          "and the client still resets by default, so a caller that says "
          "nothing gets exactly today's behaviour")

    # The reset and the expectation are the two arms of one if/else, never both.
    reset_at = client.find('args["chain"] = "reset"')
    expect_at = client.find('args["expectChain"] = expect')
    check(0 < expect_at < reset_at,
          "and they are alternatives - the expectation arm comes first and the "
          "reset is the else, so no path can send both")

    # ------------------------------------------------------------------
    # THE GUARD ITSELF.
    # ------------------------------------------------------------------
    check('Json.ReadString(request, "expectChain")' in fragment,
          "the add-in reads the caller's expectation")

    # THE CALL, NOT THE DEFINITION. `'ChainDisagrees('` matches the method's
    # own signature, so deleting the call and leaving the method behind would
    # sail past it - which is exactly what happened when this was checked by
    # removing the guard on 2026-09-16.
    check('var wrong = ChainDisagrees(chain, carried, expect, target);' in fragment,
          "and CALLS it against what is carried - a dead method that nobody "
          "invokes is the shape this guard would rot into")

    # BEFORE, not after. The whole point is refusing INSTEAD of binding, and a
    # check placed after the bind loop would report the same fault while having
    # already acted on it.
    where_check = fragment.find("var wrong = ChainDisagrees(")
    where_chain_branch = fragment.find("// 1. THE CHAIN.")
    check(0 < where_check < where_chain_branch,
          "and checks BEFORE the binding step, not after it - a check that runs "
          "after the bind describes what happened instead of preventing it, "
          "which is what the binding note already does")

    # ------------------------------------------------------------------
    # FOUR FAULTS, FOUR NAMES.
    # ------------------------------------------------------------------
    for code, why in [
        ("chain_empty",
         "nothing carried is its own fault - the producer did not run, or ran "
         "against another model"),
        ("chain_mismatch",
         "a DIFFERENT fragment left them is a second, different fault"),
        ("chain_inputs_differ",
         "the right fragment with the wrong inputs is a third - same name, "
         "different elements, and the one a fragment-name-only check would miss"),
        ("chain_contradiction",
         "reset AND expect in one request is a fourth: it could only ever fail, "
         "and it would fail saying nothing was carried, which reads as the "
         "producer having done nothing"),
    ]:
        check('Json.Error("%s"' % code in fragment,
              "%s is refused by name - %s" % (code, why))

    # ------------------------------------------------------------------
    # WHAT MAKES THE INPUT CHECK POSSIBLE AT ALL.
    # ------------------------------------------------------------------
    check("public string Inputs;" in fragment,
          "the chain records what the producer RAN WITH. Two runs of one "
          "fragment leave values under the same name and the same `By`; only "
          "the inputs tell 'the pipes in this view' from 'the walls in another'")

    check("DescribeSupplied(" in fragment,
          "and renders them one way, sorted, so two identical requests compare "
          "equal rather than by dictionary order")

    # ------------------------------------------------------------------
    # A SEGMENT, NOT A SUBSTRING - added 2026-09-21.
    #
    # The comparison was `actual.IndexOf(want, OrdinalIgnoreCase) >= 0` over
    # the whole rendered string, which PASSES the case this refusal exists to
    # stop. MEASURED in real C#, not argued: against "categories=Pipes", the
    # substring search answers TRUE for `categories=Pipe`, for `s=Pipes`, and
    # (against "view=Level 10") for `view=Level 1`. Three of the four cases
    # it is for, and every one of them is "a different set of elements",
    # which is the refusal's own closing sentence. FRAGMENT-ISSUES section 5b.
    check("SuppliedCarries(actual, want)" in fragment,
          "the input check asks a NAMED comparison rather than searching the "
          "rendered string for the expectation")

    # THE METHOD BODY, NOT THE FILE. `SuppliedCarries`'s own docstring quotes
    # the line it replaced, which is exactly what a reader coming to this in
    # a year needs - and a whole-file search for it reported the fixed code
    # as broken. Watched doing it.
    disagrees = fragment[fragment.index("private static string ChainDisagrees("):]
    disagrees = disagrees[:disagrees.index("\n        }\n")]
    check("IndexOf(want" not in disagrees,
          "and the substring search is gone from the check itself - it passed "
          "`categories=Pipe` against `categories=Pipes`, and `view=Level 1` "
          "against `Level 10`")

    carries = fragment[fragment.index("private static bool SuppliedCarries("):]
    carries = carries[:carries.index("\n        }")]
    check('.Split(\';\')' in carries,
          "SuppliedCarries splits the rendered pairs the same way the WANT "
          "side is already split, so the two sides agree about a semicolon")
    check("string.Equals(piece.Trim(), want" in carries,
          "and compares a whole segment for EQUALITY, which is what makes "
          "`categories=Pipe` stop being a match for `categories=Pipes`")
    check("OrdinalIgnoreCase" in carries,
          "still ignoring case, because that half was never the defect")

    # ------------------------------------------------------------------
    # REACHABLE FROM BOTH DOORS, or it is a feature nobody can use.
    # ------------------------------------------------------------------
    check('"--expect-from"' in client,
          "the command line can declare an expectation")

    check('args["expectChain"] = expect' in client,
          "and sends it INSTEAD of the reset, never alongside it")

    check("expect_from: str" in server,
          "and so can a host, through revit_change - a guard only reachable "
          "from a command line would not have helped the case that found this")

    check('args.pop("chain", None)' in server,
          "which also drops the reset rather than sending the contradiction")

    # ------------------------------------------------------------------
    # AND NEVER SENT TWICE - added 2026-10-01, from the A20 run.
    #
    # Running a consumer replaces the chain, so a resend after a lost answer
    # is refused on the very expectation the first run satisfied, and the
    # caller hears "nothing was bound" for a fragment that ran. The bridge
    # client resends a lost answer only for a request marked idempotent, so
    # every door that sends expectChain must mark it not.
    # ------------------------------------------------------------------
    sender = client[client.index("def cmd_fragment("):]
    sender = sender[:sender.index("\ndef ")]
    check("idempotent=" in sender and "expect" in
          sender[sender.index("idempotent="):sender.index("idempotent=") + 80],
          "the command line sends a consuming run NOT idempotent, so a lost "
          "answer is reported rather than resent")

    reader = server[server.index("def revit_read("):]
    reader = reader[:reader.index("\n@server.tool()")]
    check("idempotent=not consumes" in reader,
          "and so does revit_read, whose default of 'a read is safe to ask "
          "twice' is false for a read that consumes the chain")

    # AND THE SHOW DOOR, for the same reason: zooming twice is harmless, but a
    # zoom that consumed the chain has used it up. Found absent, the check
    # fails rather than raising (heron-ship 2a).
    at = server.find("def revit_show(")
    shower = server[at:] if at >= 0 else ""
    shower = shower[:shower.find("\n@server.tool()")] if "\n@server.tool()" in shower else shower
    check(at >= 0 and "idempotent=not consumes" in shower,
          "and so does revit_show - selecting or zooming twice is harmless, a "
          "run that consumed the chain is not")

    taken_back(fragment, server)
    stopped_part_way(fragment)
    by_identity(fragment)

    if FAILURES:
        print("FAILED")
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1

    print("PASSED - a fragment can consume what another left, and only what it "
          "said it expected.")
    return 0


def body_of(text, opening):
    """The method that starts with `opening`, to its closing brace at the
    method's own indent - or "" when it is not there, so a check FAILS rather
    than raising (heron-ship 2a)."""
    at = text.find(opening)
    if at < 0:
        return ""
    body = text[at:]
    end = body.find("\n        }\n")
    return body if end < 0 else body[:end]


def by_identity(fragment):
    """
    FRAGMENT-ISSUES 5b ROW 1164, 2026-11-20: the chain is matched to a model by
    what identifies it, never by its title. Read as text - the comparison asks
    Revit's Document. Seen to FAIL on RevitFragment.cs as it stood, where every
    one of the three comparisons was `chain.Document == target.Title`.
    """
    print("\n  the chain belongs to a model, not to a title (5b row 1164)")
    check("chain.Document == target.Title" not in fragment
          and "chain.Document != target.Title" not in fragment,
          "no carried value is matched to a model by its title")
    owner = body_of(fragment, "public bool Of(Document target)")
    check("RevitOperations.DocumentKey(target)" in owner and "Title" not in owner,
          "the chain asks the model's key - CreationGUID, path, title - as the grant "
          "and the rollback witness do")
    keep = body_of(fragment, "private static void Remember(")
    check("chain.Key = RevitOperations.DocumentKey(target);" in keep
          and "if (!chain.Of(target) || chain.TakenBack != null) chain.Clear();" in keep,
          "and Remember clears a chain taken in another model by that key, and stamps it")
    binder = body_of(fragment, "private static string BindNeeds(")
    check("chain.Of(target)" in binder,
          "the binder hands on carried values only from the same model")


def taken_back(fragment, server):
    """
    A FIFTH FAULT, ITS OWN NAME - OPEN-QUESTIONS Q-89, decided 2026-11-15:
    as built, every run replaces what is carried, and Heron also refuses any
    attempt to use what a run that was TAKEN BACK left - a dry run, a proof's
    trial, a Discard. Elements Revit no longer has are never handed on.

    What TakenBack and CarriedFromTakenBack answer is proved by
    Heron.BindingNote.TestHost (tests/test_binding_note.py); the server's half
    by tests/test_run_records.py and tests/test_outcome_line.py. What is read
    here is the one thing only the text can show: that RevitFragment.cs asks
    them, where it must. None of it has run in Revit (NEEDS-CHECKING Group NT).
    """
    check('Json.Error("chain_taken_back"' in fragment,
          "chain_taken_back is refused by name - a fifth fault: what is carried was "
          "left by a run that was taken back, and is of a change Revit no longer holds")

    disagrees = body_of(fragment, "private static string ChainDisagrees(")
    at_taken = disagrees.find('"chain_taken_back"')
    at_empty = disagrees.find('"chain_empty"')
    check(0 < at_taken < at_empty,
          "and it is asked FIRST, before nothing-carried and before a mismatch: whatever "
          "is expected, everything carried was replaced by the run taken back")
    check("chain.TakenBack != null" in disagrees[:max(at_taken, 0)]
          and "HeronBindingNote.CarriedFromTakenBack(" in disagrees,
          "on the chain's own record of it, in the binding note's words")

    check("public string TakenBack;" in fragment,
          "the chain records HOW the run that left it was taken back")

    remember = body_of(fragment, "private static void Remember(")
    check("string takenBack = null" in remember
          and "chain.TakenBack = takenBack;" in remember,
          "Remember writes it down with what the run left - and a setup step, which "
          "passes none, leaves what it made usable by the fragment after it")
    check("chain.TakenBack != null) chain.Clear();" in remember,
          "and the next run to leave anything starts the chain afresh, so no name a run "
          "taken back left outlives the mark")
    check("HeronBindingNote.TakenBack(writing, apply, answered != null, keptNothing));"
          in fragment,
          "the run's own Remember is told how it ended - a read, a keep, a keep Revit "
          "counted nothing in, a Discard, or a run sent without apply")

    binder = body_of(fragment, "private static string BindNeeds(")
    gate = binder.find("&& chain.TakenBack == null")
    asked = binder.find("var wrong = ChainDisagrees(")
    check(0 < gate < asked,
          "and nothing a run taken back left is bound BY NAME either - the binder's "
          "carried values are none while the mark stands, before anything is asked")

    # THE SERVER'S HALF: refused before a session is bound, at all three doors.
    for door in ("revit_change", "revit_read", "revit_show"):
        body = server[server.find("def %s(" % door):]
        body = body[:body.find("\n@server.tool()")] if "\n@server.tool()" in body else body
        refused = body.find("_TAKEN_BACK.refusal(expect_from)")
        bound = body.find("session = binding.resolve()")
        check(0 < refused < bound,
              "%s refuses an expect_from naming a run this chat took back BEFORE a "
              "session is bound - nothing is sent to Revit" % door)
    change = server[server.find("def revit_change("):]
    noted = change.find("_TAKEN_BACK.answered(")
    check(0 < change.find("failure = analyse(reply") < noted
          < change.find("if failure is not None:"),
          "and revit_change notes how each run ended, from the add-in's answer, before "
          "any branch returns")


def stopped_part_way(fragment):
    """
    FRAGMENT-ISSUES ROW 5b-1743, 2026-11-23: what an ARRANGEMENT STEP left
    inside a write's own group is marked as left inside an open group; the
    run's own Remember clears the mark at its end; and a request that finds
    the mark still set - the run before it stopped part way, its group rolled
    back - turns it into TakenBack before anything binds, so Q-89's refusals
    hand none of it on. What StoppedPartWay answers is proved by
    Heron.BindingNote.TestHost (tests/test_binding_note.py); that
    RevitFragment.cs asks it, where it must, only the text shows. Not run in
    Revit (NEEDS-CHECKING, row 5b-1743's group).
    """
    print("\n  what a run that stopped inside its group left is not handed on (5b-1743)")
    check("public bool OpenGroup;" in fragment,
          "the chain records that what it holds was left inside an open group")

    remember = body_of(fragment, "private static void Remember(")
    check("bool insideOpenGroup = false" in remember
          and "chain.OpenGroup = insideOpenGroup;" in remember,
          "Remember writes the mark down - and a run's own Remember, which passes none, "
          "clears it at the run's end")

    setup = body_of(fragment, "private static string RunSetupSteps(")
    check("ranWith, null, true);" in setup,
          "an arrangement step's Remember marks what it made as left inside the open group")

    marker = body_of(fragment, "private static void MarkStoppedPartWay(")
    check("chain.OpenGroup" in marker
          and "HeronBindingNote.StoppedPartWay(true, chain.TakenBack)" in marker
          and "chain.OpenGroup = false;" in marker,
          "a mark still set when a request arrives becomes TakenBack, in the binding "
          "note's words, and is spent")

    run = body_of(fragment, "public static string Run(")
    asked = run.find("MarkStoppedPartWay(client);")
    reset = run.find("Forget(client);")
    first_bind = run.find("BindNeeds(")
    check(0 < reset < asked and (first_bind < 0 or asked < first_bind),
          "and it is asked once per request, after a reset and before anything binds - "
          "never inside a run, where the fragment after a step binds what it made")


if __name__ == "__main__":
    sys.exit(main())
