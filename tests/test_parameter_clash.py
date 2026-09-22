#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   5
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The ambiguous-parameter warning counting what it lists. Runs without Revit.

`revit_parameters` closes with a sentence naming the parameter names that are
not safe to ask by, because more than one parameter on a single element answers
to them. That sentence counted ROWS and listed DISTINCT NAMES, and the two are
not the same number.

The add-in says so itself, in the `reads` note it sends beside every answer:
"the same name can appear twice, once as each, and they are two different
parameters" - one row for the instance, one for the type. A name ambiguous in
both buckets is two rows and one name, so the warning read

    2 parameter name(s) answer to TWO different parameters ... - Comments.

and a modeller counting the list finds one. The second name is not missing. It
never existed.

The list also stops at five and said nothing about stopping, while the
`notListed` block twenty lines above it goes to the trouble of naming which
kind was cut, for the reason it states: "a truncated answer that does not say
what it dropped is the one a reader trusts by mistake".

IT READS THE FUNCTION RATHER THAN IMPORTING THE MODULE, which is what
tests/test_values_crossing.py does and for the reason recorded there: `import
heron_mcp_server` needs the MCP SDK, which gates.yml leaves out on purpose, so
an importing suite exits 3, lands in "could not run", and never runs where it
matters.

WHAT IT CANNOT DO: it proves the sentence is arithmetic a reader can check. It
does not prove Revit ever reports a clash - that needs a model, see D-30.

    python tests/test_parameter_clash.py
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")

DASH = "—"


def coverage():
    """`_parameter_coverage` lifted out of the server, or None if it moved."""
    try:
        text = io.open(SERVER, encoding="utf-8").read()
    except OSError:
        return None

    nl = chr(10)
    lifted = {}

    # TWO BLANK LINES END A TOP-LEVEL FUNCTION in this file, and the one after
    # `_clip` is a decorated tool, so the boundary is looked for rather than
    # assumed. A helper that has been renamed or re-indented returns None and
    # becomes ONE clean failure below, not a traceback over every check.
    for name in ("_clip", "_parameter_coverage"):
        opens = nl + "def %s(" % name
        start = text.find(opens)
        if start < 0:
            return None
        end = text.find(nl + nl + nl, start)
        if end < 0:
            return None
        lifted[name] = text[start + 1:end]

    namespace = {}
    try:
        exec(lifted["_clip"], namespace)                  # noqa: S102
        exec(lifted["_parameter_coverage"], namespace)    # noqa: S102
    except BaseException:                                 # noqa: BLE001
        return None
    return namespace.get("_parameter_coverage")


FAILURES = []


def check(condition, what):
    if condition:
        print("  ok    %s" % what)
    else:
        print("  FAIL  %s" % what)
        FAILURES.append(what)


def row(name, where, clash=1):
    """One parameter row shaped the way RevitParameters.Coverage writes it."""
    return {"name": name, "where": where, "storageType": "String",
            "readOnly": False, "shared": False,
            "onElements": 900, "withAValue": 3, "noValue": 5, "blank": 1,
            "sameNameOnOneElement": clash}


def warning(cover, rows):
    """The clash sentence, or "" when the answer carries none."""
    reply = {"elements": 900, "distinctParameters": len(rows),
             "parameters": rows, "listed": len(rows), "notListed": 0}
    last = cover(reply, "Project1.rvt", "doors").splitlines()[-1]
    return last if "answer to TWO different parameters" in last else ""


def stated(sentence):
    """The number the sentence states, or -1 if it states none."""
    head = sentence.split(" ")[0]
    return int(head) if head.isdigit() else -1


def listed(sentence):
    """The names the sentence actually prints."""
    if DASH not in sentence:
        return []
    tail = sentence.split(DASH, 1)[1].split(".")[0]
    return [part.strip() for part in tail.split(",") if part.strip()]


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    # ASK BEFORE CALLING - heron-ship section 2a. A renamed helper must be one
    # failure, never an AttributeError standing in for every check below it.
    cover = coverage()
    check(cover is not None,
          "the server still has a helper that renders parameter coverage")
    if cover is None:
        print()
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("one name, ambiguous on its instance row AND on its type row")
    # The add-in keys rows "i:" + name and "t:" + name, so this is two rows
    # carrying one name - the shape its own `reads` note describes.
    one = warning(cover, [row("Comments", "type", 2),
                          row("Comments", "instance", 2)])
    check(one != "", "the warning is still printed")
    check(stated(one) == 1,
          "two rows carrying one name read as ONE, and it says %d" % stated(one))
    check(len(listed(one)) == 1, "and it lists one")
    check(stated(one) == len(listed(one)),
          "the number it states is the number it lists")

    print()
    print("two genuinely different ambiguous names are still two")
    two = warning(cover, [row("Comments", "instance", 2),
                          row("Mark", "instance", 3)])
    check(stated(two) == 2, "it says two")
    check(listed(two) == ["Comments", "Mark"], "and names both")

    print()
    print("a list that stops short says so")
    many = ["Comments", "Mark", "Level", "Width", "Height", "Phase", "Zone"]
    cut = warning(cover, [row(name, "instance", 2) for name in many])
    check(stated(cut) == 7, "it says seven")
    check("more" in cut,
          "and says the rest were left out, rather than printing five and "
          "stopping - the `notListed` block above it states that rule")

    print()
    print("a list that stops nowhere says nothing")
    five = warning(cover, [row(name, "instance", 2) for name in many[:5]])
    check(stated(five) == 5, "five names read five")
    check("more" not in five,
          "and nothing is claimed to have been cut, because nothing was")

    print()
    print("no clash, no sentence")
    clean = warning(cover, [row("Comments", "instance"),
                            row("Mark", "type")])
    check(clean == "",
          "a model with no ambiguous name gets no warning at all")

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("PASSED - the warning counts the names it lists, and says what it cut.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
