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

Those are four different things to go and fix, and collapsing them into "the
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

FAILURES = []


def check(condition, what):
    if not condition:
        FAILURES.append(what)


def main():
    fragment = io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin",
                                    "RevitFragment.cs"), encoding="utf-8").read()
    client = io.open(os.path.join(ROOT, "mcp", "client",
                                  "heron_bridge_client.py"), encoding="utf-8").read()
    server = io.open(os.path.join(ROOT, "mcp", "server",
                                  "heron_mcp_server.py"), encoding="utf-8").read()

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

    if FAILURES:
        print("FAILED")
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1

    print("PASSED - a fragment can consume what another left, and only what it "
          "said it expected.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
