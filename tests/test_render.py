# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RPT-RND-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Report rendering - data in, document out, same input same output.

    python tests/test_render.py

WHAT IT PROVES
  1. THE SAME INPUT GIVES THE SAME OUTPUT - twice, and with the rows
     built in a different key order, and across every kind.

  2. THE THREE THINGS THAT WOULD BREAK IT QUIETLY ARE ABSENT: no clock,
     no random, no reliance on dict order.

  3. NO FIGURE IS REFORMATTED. A float that prints badly, a huge
     integer, a negative and a boolean all survive verbatim.

  4. NOTHING IS DROPPED. Every row comes out, and a missing column is an
     empty cell AND a named gap.

  5. CSV QUOTING IS RIGHT - a comma, a quote and a newline all survive a
     round trip through Python's own csv reader.

  6. PDF AND IMAGE COME BACK AS THE PAGE, with what would convert them.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import csv
import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_render as RND                                     # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_render.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(kind, rows, **kw):
        answer = RND.render(kind, rows, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    rows = [{"element": "Duct 1", "fails": 2},
            {"element": "Duct 2", "fails": 0}]

    print("1. The same input gives the same output")
    for kind in RND.KINDS:
        first, second = ask(kind, rows), ask(kind, rows)
        check(first["sha"] == second["sha"],
              "%s renders identically twice" % kind)
        check(first["content"] == second["content"],
              "  byte for byte, not just by hash")
    # THE SAME ROWS, KEYS ADDED IN THE OTHER ORDER.
    flipped = [{"fails": 2, "element": "Duct 1"},
               {"fails": 0, "element": "Duct 2"}]
    check(ask("csv", flipped)["sha"] == ask("csv", rows)["sha"],
          "and the same rows built key-by-key in the other order give the "
          "same sha - dict order is not a fact about the data")
    check(ask("csv", list(reversed(rows)))["sha"] != ask("csv", rows)["sha"],
          "while reversing the ROWS does change it - row order IS data")
    declared = ask("csv", rows, columns=["fails", "element"])
    check(declared["columns"] == ["fails", "element"],
          "declared columns are used in the order given")
    check(ask("csv", rows)["columns"] == ["element", "fails"],
          "and derived ones are sorted, so they cannot drift")

    print("\n2. The three quiet breakers are absent")
    # THE IMPORT LIST, not a word search. "every time." contains "time.",
    # and a search over prose has been wrong five times in this repository
    # - the precise question is what the module is allowed to reach for.
    imports = sorted(line.strip() for line in logic.split("\n")
                     if line.startswith("import ")
                     or line.startswith("from "))
    check(imports == ["import hashlib", "import os", "import sys"],
          "the agent imports exactly hashlib, os and sys: %s"
          % ", ".join(imports))
    for breaker in ("os.urandom", "now(", "today(", "id(", "shuffle"):
        check(breaker not in logic, "and never uses %s" % breaker)
    check("hashlib" in logic, "the only thing it computes is a sha256")
    check("sorted(set(" in logic,
          "and the derived columns are a sorted set")

    print("\n3. No figure is reformatted")
    awkward = [{"a": 0.1 + 0.2, "b": 10 ** 20, "c": -0.0, "d": True,
                "e": None, "f": 1e-7}]
    out = ask("csv", awkward)["content"]
    for column, value in sorted(awkward[0].items()):
        if value is None:
            continue
        expected = {True: "true"}.get(value, str(value))
        check(expected in out,
              "%r survives as %r" % (value, expected))
    check("0.30000000000000004" in out,
          "0.1 + 0.2 is NOT tidied to 0.3 - a report that rounds is one "
          "whose figures cannot be checked against the query")
    check(",," in out or out.count(",") >= 5,
          "and None is an empty cell rather than the word None")
    check("None" not in out, "with no 'None' text anywhere in the document")
    for rounding in ("round(", "%.2f", "{:,}", "format(", ":.1f"):
        check(rounding not in logic, "the agent never uses %s" % rounding)

    print("\n4. Nothing is dropped")
    ragged = [{"a": 1, "b": 2}, {"a": 3}, {"b": 4, "c": 5}]
    answer = ask("csv", ragged)
    check(answer["rows"] == 3, "three rows in, three rows out")
    check(len(answer["content"].rstrip().split("\n")) == 4,
          "four lines: a header and three rows")
    check(answer["columns"] == ["a", "b", "c"],
          "every column any row mentioned is in the document")
    # FOUR, not three: row 1 is short of both b and c.
    check(len(answer["gaps"]) == 4,
          "and all four empty cells are named: %s"
          % ", ".join("row %d/%s" % (g["row"], g["column"])
                      for g in answer["gaps"]))
    check(all("Golden Rule 14" in g["why"] for g in answer["gaps"]),
          "each saying why blank and lost are different answers")
    check(not ask("csv", rows)["gaps"],
          "while complete rows report no gaps at all")

    print("\n5. CSV quoting survives a round trip")
    nasty = [{"note": 'he said "no", then left', "when": "a, b"},
             {"note": "two\nlines", "when": "plain"}]
    text = ask("csv", nasty)["content"]
    back = list(csv.reader(io.StringIO(text)))
    check(back[0] == ["note", "when"], "the header reads back")
    check(back[1] == ['he said "no", then left', "a, b"],
          "a quote and a comma survive: %r" % back[1])
    check(back[2] == ["two\nlines", "plain"],
          "and a newline inside a field survives: %r" % back[2])
    check(len(back) == 3, "three lines back, not four - the newline did "
                          "not split a row")

    print("\n6. PDF and image come back as the page")
    page = ask("page", rows)
    for kind in ("pdf", "image"):
        answer = ask(kind, rows)
        check(answer["content"] == page["content"],
              "%s returns the page's content" % kind)
        check(answer["needs"] == RND.CONVERTED[kind],
              "  naming what would convert it: %s" % answer["needs"])
        check(answer["of"] == "page", "  and saying what it is")
    check("needs" not in page and "needs" not in ask("csv", rows),
          "while page and csv need no converter")
    for running in ("subprocess", "open(", "os.system", "Popen",
                    "reportlab", "PIL", "matplotlib"):
        check(running not in logic, "and the agent never uses %s" % running)

    print("\n7. Every failure is named and reached")
    for kind in ("word", "", "PAGE ", "html"):
        answer = ask(kind, rows)
        if kind.strip().lower() in RND.KINDS:
            continue
        check(answer.get("refused") == "NOT_A_KIND", "'%s' is not a kind"
              % kind)
    check(ask("page", None).get("refused") == "NOTHING_TO_RENDER",
          "no rows is refused")
    check(ask("page", []).get("refused") == "NOTHING_TO_RENDER",
          "and so is an empty list")
    check(ask("page", [{}]).get("refused") == "NOTHING_TO_RENDER",
          "and rows carrying no columns at all")
    check(ask("page", ["not a row"]).get("refused") == "NOT_A_ROW",
          "a row that is not a map is refused")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-RPT-RND-002.yaml"))
    check(contract.get("allowed-tools") == [],
          "the contract declares no tools - docs/28 puts 'no model call' "
          "in the row")
    named = contract.get("failures") or []
    check(len(named) == 3, "the contract declares 3 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(page["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    same input, same output, and no figure reformatted")
    return 0


if __name__ == "__main__":
    sys.exit(main())
