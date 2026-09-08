#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The caller's half, as typed at a keyboard, becomes what the executor reads.
Runs without Revit.

WHAT THIS GUARDS. 288 of the 308 unproven fragments declare at least one need
as `source: request` - a view, a category, a name to match, a distance - and
the executor refuses to guess one, because guessing is how a job runs against
the wrong thing and reports success. Two small functions in the client turn a
command line into those values, and both have a failure mode that is silent.

  pull_values    lifts --set and --view out of the argument list. If it
                 disturbed the ORDER of what is left, `prove --in "Project1"`
                 would stop finding its own flag - and the symptom would be a
                 run against the wrong model, which is the exact failure
                 --in exists to prevent.

  caller_values  splits name=value on the FIRST '=' only. A view called
                 "Level 1 = Mech" truncated at the second one resolves to no
                 view at all, and the message would name a string the user
                 never typed.

Neither needs Revit, so neither has any excuse to be unproved.
"""

import os
import sys

sys.path.insert(0, os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "mcp", "client"))

import heron_bridge_client as client            # noqa: E402


def run():
    failures = []
    passes = []

    def check(what, got, expected):
        if got == expected:
            passes.append(what)
        else:
            failures.append("%s\n          expected %r\n          got      %r"
                            % (what, expected, got))

    # ---- pull_values: what is lifted, and what is left alone ----------------

    kept, pairs, negs = client.pull_values(["list-levels"])
    check("a plain fragment name is left untouched", (kept, pairs), (["list-levels"], []))

    kept, pairs, negs = client.pull_values(["audit-view-filters", "--view", "Level 1"])
    check("--view is lifted and rewritten as view=",
          (kept, pairs), (["audit-view-filters"], ["view=Level 1"]))

    kept, pairs, negs = client.pull_values(["frag", "--set", "categories=Ducts"])
    check("--set is lifted verbatim", (kept, pairs), (["frag"], ["categories=Ducts"]))

    # ORDER. --in is read positionally by cmd_prove after this runs, so a value
    # typed anywhere must not shuffle what is left.
    kept, pairs, negs = client.pull_values(
        ["--in", "Project1", "audit-view-filters", "--view", "Level 1"])
    check("the flags that follow keep their positions",
          kept, ["--in", "Project1", "audit-view-filters"])

    kept, pairs, negs = client.pull_values(
        ["--view", "Level 1", "--in", "Project1", "frag"])
    check("a value typed FIRST still leaves --in at the front",
          kept, ["--in", "Project1", "frag"])

    kept, pairs, negs = client.pull_values(
        ["frag", "--set", "a=1", "--view", "Level 1", "--set", "b=2"])
    check("several values are collected in the order typed",
          pairs, ["a=1", "view=Level 1", "b=2"])

    kept, pairs, negs = client.pull_values(["frag", "--view"])
    check("--view with nothing after it is refused, not silently dropped",
          (kept, pairs), (None, None))

    kept, pairs, negs = client.pull_values(["frag", "--set"])
    check("--set with nothing after it is refused", (kept, pairs), (None, None))

    # ---- the negative case's own values ------------------------------------
    #
    # A proof's negative case is an arrangement where the answer must be empty.
    # For a fragment that reads a view, the arrangement IS a different view -
    # so these travel separately and must not leak into the positive case.

    kept, pairs, negs = client.pull_values(
        ["report-category-visibility", "--view", "L2", "--negative-view", "Model Linking"])
    check("the negative view is kept apart from the positive one",
          (kept, pairs, negs),
          (["report-category-visibility"], ["view=L2"], ["view=Model Linking"]))

    kept, pairs, negs = client.pull_values(
        ["frag", "--negative-set", "categories=Walls"])
    check("--negative-set lands in the negative list only",
          (pairs, negs), ([], ["categories=Walls"]))

    kept, pairs, negs = client.pull_values(["frag", "--view", "L2"])
    check("no negative typed means no negative values - not a copy of the positive",
          negs, [])

    kept, pairs, negs = client.pull_values(["frag", "--negative-view"])
    check("--negative-view with nothing after it is refused",
          (kept, pairs, negs), (None, None, None))

    # ---- caller_values: the split, and what is rejected ---------------------

    check("nothing typed is not an error - it is no values",
          client.caller_values([]), [])

    check("one pair becomes one {name, value}",
          client.caller_values(["view=Level 1"]),
          [{"name": "view", "value": "Level 1"}])

    # THE FIRST '=' ONLY. This is the whole reason the function exists rather
    # than a bare split().
    check("a value containing '=' survives intact",
          client.caller_values(["view=Level 1 = Mech"]),
          [{"name": "view", "value": "Level 1 = Mech"}])

    check("an empty value is legal - 'match nothing' is a real request",
          client.caller_values(["nameContains="]),
          [{"name": "nameContains", "value": ""}])

    check("spaces around the name are not part of it",
          client.caller_values([" view =Level 1"]),
          [{"name": "view", "value": "Level 1"}])

    check("a pair with no '=' is refused rather than guessed at",
          client.caller_values(["view"]), None)

    check("a pair with no name before the '=' is refused",
          client.caller_values(["=Level 1"]), None)

    # ---- the two together, as a command line actually arrives ---------------

    kept, pairs, negs = client.pull_values(
        ["report-tags-and-targets", "--view", "01 - Mech Plan"])
    check("end to end: a typed line becomes what crosses the wire",
          (kept, client.caller_values(pairs)),
          (["report-tags-and-targets"],
           [{"name": "view", "value": "01 - Mech Plan"}]))

    for line in passes:
        print("  PASS  " + line)

    if failures:
        print()
        for line in failures:
            print("  FAIL  " + line)
        print()
        return 1

    print()
    print("The caller's half survives the trip from keyboard to request.")
    print("Whether Revit then resolves \"Level 1\" to the right view is a")
    print("separate question, and it needs a model - see D-30.")
    return 0


if __name__ == "__main__":
    sys.exit(run())
