#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
CONNECT_AIR_TERMINALS decides "joined" from what is physically joined, and
chooses the duct from the ones that fit before it chooses the nearest.

    python tests/test_connect_air_terminals_evidence.py

WHY IT EXISTS - WORK-NOTE ITEM WN-79 (C05), ISSUE #1005
-------------------------------------------------------
Version 2 of brain/fragments/connect-air-terminals/impl/any/fragment.cs took a
terminal's FIRST End connector whatever its domain, let Revit's IsConnected
flag - an intent flag, wrong in a real model (REPORT_CONNECTORS) - decide both
whether to skip a terminal and whether it ended up joined, and chose the
nearest duct before any compatibility filter. So a nearer return duct hid the
supply duct behind it, and Revit 2024 was seen joining a supply diffuser to a
return duct (row 5b-187).

WHAT IT CHECKS, AS TEXT (comments removed first, so a rule only described in
words does not count)
  1. IsConnected IS NEVER READ in the code - not for the skip, not for the
     result, not for a head.
  2. PARTNERS COME FROM AllRefs, leaving out the connector's own element and
     the logical MEPSystem.
  3. AN AIR TERMINAL IS ITS CATEGORY (OST_DuctTerminal, through an ElementId,
     never IntegerValue), and its connector is an HVAC End connector.
  4. THE DUCTS ARE FILTERED BEFORE THE NEAREST IS TAKEN: a Duct only, and one
     on the opposite air system is set aside (and said) before the distance
     comparison that picks `nearest`.
  5. AFTER THE TAP, CONNECTED MEANS JOINED TO THAT DUCT: connected++ follows a
     notJoinedTo(terminal, nearest.Id) that came back null.
  6. A HEAD'S JOIN is the riser among the head connector's partners.
  7. THE CARD FOLLOWS THE CODE: version at least 3, DRAFT, and cases.yaml has
     the nearer-wrong-system case and the not-an-air-terminal case.

WHAT IT CANNOT DO
Run the fragment. What Revit puts in AllRefs after ConnectAirTerminalOnDuct -
a takeoff whose other side reaches the duct, or the duct itself - is NEEDS
REAL REVIT: NEEDS-CHECKING Group PZ, issue #1312. A green run here means the
rule is written, not that it has been seen to work.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARD_DIR = os.path.join(ROOT, "brain", "fragments", "connect-air-terminals")
IMPL = os.path.join(CARD_DIR, "impl", "any", "fragment.cs")
CARD = os.path.join(CARD_DIR, "fragment.yaml")
CASES = os.path.join(CARD_DIR, "tests", "cases.yaml")

failures = []
passes = [0]


def check(ok, what):
    print("  %-5s %s" % ("ok" if ok else "FAIL", what))
    if ok:
        passes[0] += 1
    else:
        failures.append(what)


def strip_comments(code):
    """C# without // and /* */ comments; string literals are kept whole."""
    out, i, n = [], 0, len(code)
    while i < n:
        c = code[i]
        if c == '"':
            j = i + 1
            while j < n and code[j] != '"':
                j += 2 if code[j] == "\\" else 1
            out.append(code[i:j + 1])
            i = j + 1
        elif code.startswith("//", i):
            j = code.find("\n", i)
            i = n if j < 0 else j
        elif code.startswith("/*", i):
            j = code.find("*/", i + 2)
            i = n if j < 0 else j + 2
        else:
            out.append(c)
            i += 1
    return "".join(out)


def read(path):
    with open(path, encoding="utf-8") as handle:
        return handle.read()


def main():
    code = strip_comments(read(IMPL))
    flat = re.sub(r"\s+", " ", code)
    card = read(CARD)
    cases = read(CASES)

    print("1. the IsConnected flag decides nothing")
    check(".IsConnected" not in flat, "no .IsConnected is read anywhere in the code")

    print("2. partners come from AllRefs, without the element itself or a system")
    partners = re.search(r"Func<Connector, List<ElementId>> partnersOf = c => \{(.*?)\};", flat)
    check(partners is not None, "partnersOf is there")
    body = partners.group(1) if partners else ""
    check("c.AllRefs" in body, "it walks AllRefs")
    check("owner is MEPSystem" in body and "owner.Id == self" in body,
          "it leaves out an MEPSystem and the connector's own element")

    print("3. an air terminal is its category, its connector the duct one")
    check("new ElementId(BuiltInCategory.OST_DuctTerminal)" in flat,
          "OST_DuctTerminal through new ElementId(BuiltInCategory)")
    check("IntegerValue" not in flat, "no IntegerValue (removed in later releases)")
    check(re.search(r"if \(!isAirTerminal\(terminal\)\) \{ var categoryName"
                    r".*?refused\.Add\(.*?not an air terminal.*?continue; \}", flat) is not None,
          "anything not an air terminal is refused")
    check("connector.ConnectorType == ConnectorType.End && connector.Domain == Domain.DomainHvac"
          in flat, "the terminal's connector is an HVAC End connector")

    print("4. the ducts are filtered before the nearest is taken")
    loop = re.search(r"foreach \(var duct in ducts\) \{(.*?)\} var passedOver", flat)
    check(loop is not None, "the duct loop is there")
    body = loop.group(1) if loop else ""
    check(body.startswith(" if (!(duct is Duct)) continue;"),
          "a non-Duct is passed over first")
    wrong = body.find("ductSystem != terminalSystem")
    pick = body.find("nearest = duct")
    check(wrong >= 0 and pick > wrong,
          "the opposite air system is set aside before nearest is chosen")
    check("was passed over - it is" in flat, "a nearer duct passed over is said")

    print("5. after the tap, connected means joined to that duct")
    check(re.search(r"var why = notJoinedTo\(terminal, nearest\.Id\); if \(why != null\) \{ "
                    r"refused\.Add\(", flat) is not None,
          "a tap not joined to the chosen duct goes to refused")
    check(re.search(r"notJoinedTo\(terminal, nearest\.Id\);.*?continue; \} connected\+\+;", flat)
          is not None, "connected++ only after that read-back")

    print("6. a head's join is the riser among its partners")
    check("joined = partners != null && partners.Contains(riser.Id);" in flat,
          "the head is joined when the riser is its partner")

    print("7. the card follows the code")
    version = re.search(r"^version: (\d+)$", card, re.M)
    check(version is not None and int(version.group(1)) >= 3,
          "the card's version is at least 3 (found %s)"
          % (version.group(1) if version else "none"))
    check(re.search(r"^heron-status: (DRAFT|PROVEN)$", card, re.M) is not None,
          "the card carries a status")
    check("passed over for its system" in cases and "not an air terminal" in cases,
          "cases.yaml has the nearer-wrong-system and not-an-air-terminal cases")

    print()
    if failures:
        print("FAIL - %d of %d checks" % (len(failures), len(failures) + passes[0]))
        for what in failures:
            print("  - " + what)
        return 1
    print("PASS - %d checks" % passes[0])
    return 0


if __name__ == "__main__":
    sys.exit(main())
