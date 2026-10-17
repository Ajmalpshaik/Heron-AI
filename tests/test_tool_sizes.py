# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MCP-REG-003
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
What Heron's tool list costs a chat - measured, and held to a budget.

    python tests/test_tool_sizes.py
    python mcp/server/heron_tools.py --sizes      # the numbers themselves

D-03 accepted the cost in so many words: "the tool list grows large, and MCP
tool schemas cost context on every request", mitigated by "a small stable core
set". The core set is heron_tools.ALWAYS_LOADED, given to every chat before
the modeller types (docs/04 s8.6). Nothing held what it cost, and the one
typed figure for it - about 9,000 for five, 2026-10-01 - was stale the day a
sixth tool joined. This is the hold.

WHAT IT PROVES
  1. THE SIZES ARE DERIVED. heron_tools measures every tool the server
     decorates - exactly the tools the registry declares, none missing and
     none extra - and `--sizes --json` prints the same totals as a command.

  2. THE TOOLS EVERY CHAT IS GIVEN STAY WITHIN THEIR BUDGET - the
     descriptions of ALWAYS_LOADED, together.

  3. NO ONE TOOL OUTGROWS ITS BUDGET. Every other tool joins the chat the
     moment a search loads it, and a description is read whole each time.

  4. A TRIM NEVER TAKES WHAT A CALLER NEEDS. A budget teaches people to cut
     words to buy the number back - D-100's first reason for keeping a score
     a report. So every tool still names every argument it takes, and the two
     doors keep the sentences that are the whole of their safety. A number
     that can only be met by losing one of those is the wrong number.

  5. THE CHECK HAS TEETH. A planted tool over budget, and one that has
     stopped naming its argument, are both caught - from a source file the
     measurement reads exactly as it reads the real one.

WHAT IT DOES NOT
  The schemas. The SDK writes a tool's input schema from its signature at run
  time, so measuring one needs the SDK, and this suite runs where there is
  none - CI included. `--sizes` reports them when an SDK is installed, and
  tests/test_mcp_serves.py proves through a real SDK that the description a
  host is served is the one measured here.

IS IT A GATE OR A REPORT? A GATE - D-118. A tool over budget exits 1 and
stops a pull request. The owner decided it on 2026-10-17, asked as D-100 was
asked of the routing score (which stays a report): a size is exact and this
suite takes a second, so two of D-100's three reasons do not hold, and the
third - a gate teaches people to cut words to buy the number back - is what
section 4 is for. Making it a report needs a decision that supersedes D-118.
"""

import ast
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_tools as TOOLS                                   # noqa: E402

SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")

# WHAT EVERY CHAT PAYS FOR BEFORE IT ASKS ANYTHING, in characters of
# description - docs/19: "if a context assembly exceeds its budget, that is a
# bug in retrieval, not a reason to raise the budget". The six measured 6,802
# on 2026-10-17 before they were trimmed. Raising this is a decision, and the
# reason goes here.
ALWAYS_LOADED_BUDGET = 4500

# AND THE MOST ANY ONE TOOL MAY TAKE. On 2026-10-17 seven tools were over it,
# revit_change the largest at 2,870. What a description cannot hold belongs
# in the answer the tool gives at the moment it is needed - a hand-back, a
# refusal, a catalogue - or in a comment, which no host is served.
PER_TOOL_BUDGET = 1500

# THE SENTENCES THE TWO DOORS MAY NEVER LOSE TO A TRIM, each the whole of a
# safety a host relies on before its first call. Matched as written.
MUST_KEEP = {
    "revit_change": (
        "KEEP",          # the change is kept at once - D-99, no preview
        "Changes",       # and only while the owner's switch is on
        "Publish",       # D-106's two switches beside it
        "Admin",
        "Article 7",     # destructive work needs the user's yes first
        "Article 10",    # and nothing is saved, synced or exported unasked
        "Article 11",    # only a PROVEN capability is kept - D-117
        "revit_read",    # a question goes to the door that cannot write
        "Ctrl+Z",        # and one undo puts the model back
    ),
    "revit_read": (
        "change nothing",
        "Changes OFF",
        "revit_change",  # a change is the other door's
    ),
}

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def arguments(path=SERVER):
    """
    {tool: [argument names]} for every `@server.tool()`, read with ast.

    Not an argument annotated `_Context`: the SDK fills that in itself and a
    host never sees it (tests/test_mcp_serves.py holds that no served schema
    shows a `ctx`), so there is nothing for a description to explain.
    """
    with io.open(path, encoding="utf-8") as fh:
        tree = ast.parse(fh.read(), path)
    return dict((node.name, [a.arg for a in node.args.args
                             if not (a.annotation is not None
                                     and ast.unparse(a.annotation) == "_Context")])
                for node in tree.body
                if isinstance(node, ast.FunctionDef)
                and any(ast.unparse(d) == "server.tool()" for d in node.decorator_list))


def over_budget(report, per_tool=PER_TOOL_BUDGET):
    """The tools whose description is over `per_tool`, largest first."""
    return [(r["tool"], r["description"]) for r in report["tools"]
            if r["description"] > per_tool]


def unnamed(path=SERVER):
    """[(tool, argument)] for every argument a tool's description never names."""
    said = TOOLS.descriptions(path)
    return [(tool, arg) for tool, args in sorted(arguments(path).items())
            for arg in args
            if not re.search(r"\b%s\b" % re.escape(arg), said.get(tool, ""))]


def main():
    print("What Heron's tool list costs a chat, held to a budget")
    print("=" * 62)

    print()
    print("1. The sizes are derived, for exactly the tools the registry declares")
    report = TOOLS.sizes()
    measured = set(r["tool"] for r in report["tools"])
    check(measured == set(TOOLS.TOOLS),
          "every declared tool is measured, and nothing else: %d" % len(measured)
          + ("" if measured == set(TOOLS.TOOLS) else
             " - only measured %s; only declared %s"
             % (sorted(measured - set(TOOLS.TOOLS)), sorted(set(TOOLS.TOOLS) - measured))))
    empty = sorted(r["tool"] for r in report["tools"] if not r["description"])
    check(not empty, "every one has a description%s"
          % ("" if not empty else " - none on %s" % ", ".join(empty)))
    check(set(TOOLS.ALWAYS_LOADED) <= measured,
          "every tool in ALWAYS_LOADED is one of them")
    ran = subprocess.run([sys.executable, os.path.join(ROOT, "mcp", "server", "heron_tools.py"),
                          "--sizes", "--json"],
                         capture_output=True, encoding="utf-8", errors="replace")
    try:
        printed = json.loads(ran.stdout)
    except ValueError:
        printed = {}
    check(ran.returncode == 0
          and printed.get("description") == report["description"]
          and printed.get("always_loaded_description") == report["always_loaded_description"],
          "`heron_tools.py --sizes --json` prints the same totals: %s of descriptions, "
          "%s for ALWAYS_LOADED" % ("{:,}".format(report["description"]),
                                    "{:,}".format(report["always_loaded_description"])))

    print()
    print("2. The tools every chat is given stay within their budget")
    always = report["always_loaded_description"]
    check(always <= ALWAYS_LOADED_BUDGET,
          "ALWAYS_LOADED's %d descriptions are %s characters, within %s"
          % (len(TOOLS.ALWAYS_LOADED), "{:,}".format(always),
             "{:,}".format(ALWAYS_LOADED_BUDGET)))

    print()
    print("3. No one tool outgrows its budget")
    over = over_budget(report)
    check(not over, "every tool's description is within %s characters%s"
          % ("{:,}".format(PER_TOOL_BUDGET),
             "" if not over else " - over: " + ", ".join(
                 "%s %s" % (t, "{:,}".format(n)) for t, n in over)))
    largest = report["tools"] and max(report["tools"], key=lambda r: r["description"])
    if largest:
        print("        the largest is %s, %s" % (largest["tool"],
                                                "{:,}".format(largest["description"])))

    print()
    print("4. A trim never takes what a caller needs")
    missing = unnamed()
    check(not missing, "every tool names every argument it takes%s"
          % ("" if not missing else " - not named: " + ", ".join(
              "%s.%s" % pair for pair in missing)))
    said = TOOLS.descriptions()
    for tool, phrases in sorted(MUST_KEEP.items()):
        lost = [p for p in phrases if p not in said.get(tool, "")]
        check(not lost, "%s still says: %s%s"
              % (tool, ", ".join(phrases), "" if not lost else " - LOST %s" % lost))

    print()
    print("5. The check has teeth")
    home = tempfile.mkdtemp(prefix="heron-tool-sizes-")
    try:
        planted = os.path.join(home, "server.py")
        with io.open(planted, "w", encoding="utf-8") as fh:
            fh.write('@server.tool()\ndef fat(level: str = "") -> str:\n'
                     '    """%s"""\n\n\n'
                     '@server.tool()\ndef quiet(view: str = "") -> str:\n'
                     '    """Reads something and names no argument."""\n'
                     % ("Says the same thing again. " * 80))
        fake = TOOLS.sizes(path=planted)
        check([t for t, _n in over_budget(fake)] == ["fat"],
              "a planted tool over budget is named, and only it")
        check(unnamed(planted) == [("fat", "level"), ("quiet", "view")],
              "and a tool that stopped naming its argument is caught")
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    print("  The whole list's descriptions come to %s. Every size, and the schemas"
          % "{:,}".format(report["description"]))
    print("  when an MCP SDK is installed: python mcp/server/heron_tools.py --sizes")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the tool list is measured, and within its budget")
    return 0


if __name__ == "__main__":
    sys.exit(main())
