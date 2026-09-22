# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RPT-CMP-001
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Report composition - the answer's shape follows the request's shape.

    python tests/test_compose.py

WHAT IT PROVES
  1. D-27's TABLE IS WHAT THE AGENT CARRIES - all five rows, read out of
     DECISIONS.md.

  2. A BREAKDOWN IS SORTED BY GROUP, NOT BY QUANTITY. The one rule
     somebody would otherwise get backwards, proved with data where the
     two orders differ.

  3. A NARROWED SET WITHOUT IDS IS REFUSED - it answers the question and
     blocks the next one.

  4. A CLOSE MUST SAY WHAT WAS VERIFIED. The middle part is the one
     people leave out.

  5. A COMPARISON GETS A PICTURE WITHOUT BEING ASKED, and one number is
     not turned into a comparison.

  6. AN OVERRIDE MUST CARRY A DATE, because a correction that costs
     nothing to record is one nobody records.

  7. NO MODEL IS CALLED, against the agent's own register row.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_compose as CMP                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def decision_log():
    """DECISIONS.md and every record under docs/decisions/. Since 2026-09-23
    each decision's full text is its own file and DECISIONS.md is the index
    (tools/split-decisions.py), so what a decision says is in one of them."""
    paths = [os.path.join(ROOT, "docs", "DECISIONS.md")]
    folder = os.path.join(ROOT, "docs", "decisions")
    if os.path.isdir(folder):
        paths += [os.path.join(folder, n) for n in sorted(os.listdir(folder)) if n.endswith(".md")]
    return " ".join(io.open(p, encoding="utf-8").read() for p in paths)


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_compose.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(shape, data, **kw):
        answer = CMP.compose(shape, data, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. D-27's table is what the agent carries")
    decisions = " ".join(decision_log().split())
    check("What varies is the shape of the answer, chosen from the shape "
          "of the request" in decisions,
          "the shape of the answer is chosen from the shape of the request")
    for quoted in ("The number. One line, nothing else",
                   "sorted the way a schedule sorts, not by quantity",
                   "The items themselves, with their ids",
                   "what was done, what was actually verified, what still "
                   "needs deciding",
                   "A picture, without being asked for one"):
        check(quoted in decisions, "  D-27 says: %s" % quoted[:42])
    check(len(CMP.SHAPES) == 5, "the agent carries five shapes")
    check(sorted(CMP.SHAPES) == ["breakdown", "comparison", "count",
                                 "finished", "narrowed"],
          "and they are those five")

    print("\n2. A breakdown is sorted by group, not by quantity")
    # THE TWO ORDERS DIFFER, which is the only way to tell them apart.
    groups = {"Supply": 31, "Extract": 12, "Return": 4, "Alpha": 99}
    rows = ask("breakdown", groups)["includes"][0]["rows"]
    by_group = [row["group"] for row in rows]
    check(by_group == ["Alpha", "Extract", "Return", "Supply"],
          "sorted by group: %s" % ", ".join(by_group))
    by_quantity = [name for name in
                   sorted(groups, key=lambda each: -groups[each])]
    check(by_group != by_quantity,
          "which is NOT the quantity order (%s), so the two really are "
          "different here" % ", ".join(by_quantity))
    check("not by quantity" in ask("breakdown", groups)["why"],
          "and the answer says which it used")
    check("ranking nobody asked for" in ask("breakdown", groups)["why"],
          "and why sorting by count would be wrong")
    check([row["value"] for row in rows] == [99, 12, 4, 31],
          "the values travel with their groups, unchanged")

    print("\n3. A narrowed set without ids is refused")
    good = ask("narrowed", [{"id": 418302}, {"id": 418303}])
    check(good["composed"] and len(good["includes"][0]["items"]) == 2,
          "items with ids compose")
    for missing in ([{"size": "300x300"}],
                    [{"id": 1}, {"size": "x"}],
                    [{"id": ""}],
                    ["not a map"]):
        answer = ask("narrowed", missing)
        check(answer.get("refused") == "NO_IDS",
              "%r is refused" % (missing,))
    check("blocks the next one" in ask("narrowed", [{"a": 1}])["why"],
          "because a list without ids answers the question and blocks the "
          "next one")
    check(ask("narrowed", [{"id": 1}, {"size": "x"}])["how_many"] == 1,
          "and the answer says how many were short")

    print("\n4. A close must say what was verified")
    full = {"done": "tagged 47", "verified": "12 spot-checked",
            "undecided": "3 with no system"}
    close = ask("finished", full)
    check(close["composed"], "all three parts compose")
    check([one["part"] for one in close["includes"]]
          == ["done", "verified", "undecided"],
          "in D-27's order: %s"
          % ", ".join(one["part"] for one in close["includes"]))
    for leave_out in CMP.A_CLOSE_CARRIES:
        short = dict(full)
        del short[leave_out]
        answer = ask("finished", short)
        check(answer.get("refused") == "AN_INCOMPLETE_CLOSE",
              "a close with no '%s' is refused" % leave_out)
        check(answer["missing"] == [leave_out], "  and says which")
    check("what people leave out" in ask("finished", {"done": "x",
                                                      "undecided": "y"}
                                         )["why"],
          "with the reason: verified is the one people leave out")

    print("\n5. A comparison gets a picture unasked")
    picture = ask("comparison", [47, 52])
    check(picture["includes"][0]["part"] == "a picture",
          "two numbers give a picture")
    check("without being asked" in picture["why"],
          "without being asked for one")
    check("default rather than a flourish" in picture["why"],
          "and the answer says it is a default rather than a flourish")
    check(ask("comparison", [47]).get("refused") == "NOTHING_TO_COMPARE",
          "one number is not a comparison")
    check("this agent does not turn it into one"
          in ask("comparison", [47])["why"],
          "and is not turned into one on the caller's behalf")
    check(ask("comparison", [47, "not a number", 52]
              )["includes"][0]["numbers"] == [47, 52],
          "while non-numbers are simply not compared")

    print("\n6. An override must carry a date")
    dated = [{"shape": "count", "instead": "a table", "at": "2026-09-15"}]
    applied = ask("count", 47, overrides=dated)
    check(applied["override"] == dated[0], "a dated override is applied")
    check("2026-09-15" in applied["why"], "and its date is in the answer")
    for field in ("shape", "instead", "at"):
        short = dict(dated[0])
        del short[field]
        answer = ask("count", 47, overrides=[short])
        check(answer.get("refused") == "NOT_AN_OVERRIDE",
              "an override with no `%s` is refused" % field)
    check("nobody records" in ask("count", 47, overrides=[
        {"shape": "count", "instead": "x"}])["why"],
        "because a correction that costs nothing to record is one nobody "
        "records")
    elsewhere = ask("count", 47, overrides=[
        {"shape": "breakdown", "instead": "x", "at": "d"}])
    check(elsewhere["override"] is None,
          "an override for a different shape does not apply")
    check(decisions.count("a format correction has to cost one line to "
                          "record") == 1,
          "and D-27 really says a format correction has to cost one line")

    print("\n7. No model is called, against its own register row")
    row = [line for line in io.open(
        os.path.join(ROOT, "docs", "28-agent-registry.md"),
        encoding="utf-8").read().splitlines() if "RPT-CMP-001" in line][0]
    check("for this reader" in row,
          "docs/28's row says 'at what depth for this reader'")
    check("Judgement, so a model call" in row,
          "and 'judgement, so a model call'")
    check("There is no persona" in " ".join(io.open(
        os.path.join(ROOT, "docs", "22-users-modes-and-extensibility.md"),
        encoding="utf-8").read().split()),
        "while docs/22's DECIDED block says there is no persona")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-RPT-CMP-001.yaml"))
    check(contract.get("allowed-tools") == [],
          "so the contract declares no tools")
    for reaching in ("anthropic", "openai", "requests", "urlopen", "http",
                     "subprocess", "model("):
        check(reaching not in logic.lower(),
              "and the code never reaches for %s" % reaching)
    check("F19" in whole, "the agent records the gap as PROPOSALS F19")
    check(any("NOT THE READER" in line for line in
              ask("count", 47)["unjudged"]),
          "and every answer says the shape came from the request")

    print("\n8. Every failure is named and reached")
    for shape in ("trend", "", "summary"):
        check(ask(shape, 1).get("refused") == "NOT_A_SHAPE",
              "'%s' is not a shape" % shape)
    for shape, data in (("count", None), ("count", "forty-seven"),
                        ("breakdown", []), ("narrowed", []),
                        ("finished", "done")):
        answer = ask(shape, data)
        check(answer.get("refused") in ("NOTHING_TO_COMPOSE",
                                        "AN_INCOMPLETE_CLOSE"),
              "%s with %r is refused as %s"
              % (shape, data, answer.get("refused")))
    named = contract.get("failures") or []
    check(len(named) == 6, "the contract declares 6 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(ask("count", 47)["unjudged"]) == 4,
          "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the request's shape decides, and a schedule sorts by group")
    return 0


if __name__ == "__main__":
    sys.exit(main())
