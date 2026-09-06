#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The compile gate and the executor import the SAME namespaces. Runs without Revit.

THE FAILURE THIS GUARDS IS THE WORST SHAPE THERE IS, and the compile gate
predicted it in its own comment before the executor existed:

    "THIS LIST IS A CONTRACT WITH UNBUILT WORK. It declares what a fragment may
     assume is in scope, so D-28's in-process Roslyn executor must supply the
     same set. A namespace added here and not there compiles green and fails at
     the PC."

The executor is now built, so both halves of that sentence exist and can drift.
If the gate imports a namespace the executor does not:

    343 fragments pass a green compile gate
    one of them throws at the machine, in front of the owner
    and nothing on this side of the wire could have seen it coming

The reverse drift is quieter and just as wrong: a namespace the executor
supplies but the gate does not compile against is a fragment that works on the
PC and cannot be checked anywhere else - which is how the version boundary
stops being covered.

    python tests/test_fragment_imports.py
"""

import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

GATE = os.path.join(ROOT, "tools", "check-fragments-compile.py")
EXECUTOR = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronFragmentImports.cs")


def gate_namespaces():
    """The USINGS list the compile gate builds its wrapper from."""
    text = io.open(GATE, encoding="utf-8").read()
    block = re.search(r"^USINGS\s*=\s*\[(.*?)^\]", text, re.S | re.M)
    if not block:
        return None
    # Quoted strings only - the block is full of comment lines explaining why
    # each one is there, and those must not be mistaken for entries.
    return re.findall(r'"([^"]+)"', block.group(1))


def executor_namespaces():
    """The list the in-process executor hands to Roslyn."""
    text = io.open(EXECUTOR, encoding="utf-8").read()
    block = re.search(r"Namespaces\s*=\s*\{(.*?)\};", text, re.S)
    if not block:
        return None
    return re.findall(r'"([^"]+)"', block.group(1))


def run():
    failures = []

    gate = gate_namespaces()
    executor = executor_namespaces()

    # PROVE THE PATTERN CAN SEE WHAT IS THERE. This repository has been caught
    # three times by a count whose pattern silently matched nothing, and a
    # comparison of two empty lists passes perfectly.
    if not gate:
        failures.append(
            "could not read USINGS out of tools/check-fragments-compile.py - "
            "the pattern found nothing, which is not the same as the gate "
            "importing nothing")
    if not executor:
        failures.append(
            "could not read Namespaces out of HeronFragmentImports.cs - same "
            "problem, other side")

    if gate and executor:
        if len(gate) < 5 or len(executor) < 5:
            failures.append(
                "one of the lists came back implausibly short (%d gate, %d "
                "executor). A comparison of two nearly-empty lists passes and "
                "proves nothing" % (len(gate), len(executor)))

        only_gate = [n for n in gate if n not in executor]
        only_executor = [n for n in executor if n not in gate]

        if only_gate:
            failures.append(
                "the gate compiles against namespaces the executor does NOT "
                "supply: %s. Fragments using them pass the gate and throw at "
                "the machine" % ", ".join(only_gate))

        if only_executor:
            failures.append(
                "the executor supplies namespaces the gate does NOT compile "
                "against: %s. Fragments using them work on the PC and are "
                "checked nowhere" % ", ".join(only_executor))

        if gate != executor and not only_gate and not only_executor:
            failures.append(
                "same namespaces, different order. Not a fault, but the two "
                "lists are meant to diff cleanly - reorder one to match")

    print("Fragment imports - gate against executor")
    if gate:
        print("  gate     %d namespace(s)" % len(gate))
    if executor:
        print("  executor %d namespace(s)" % len(executor))

    if failures:
        for line in failures:
            print("  FAIL  " + line)
        print("\nFAILED - the two lists disagree, which is a green gate and a "
              "failure at the PC.")
        return 1

    print("  PASS  both sides import exactly the same set, in the same order")
    print("\nWhat a fragment may assume is one list, not two that look alike.")
    return 0


if __name__ == "__main__":
    sys.exit(run())
