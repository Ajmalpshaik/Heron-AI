# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-DOC-017
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The contract reference - a promise the code does not keep, and nothing
else.

    python tests/test_contract_reference.py

WHAT IT PROVES
  1. A REFUSAL IS RECOGNISED BY POSITION, NOT BY SHAPE. A capability
     called COUNT_DUCTS is not a refusal; a single word like INCOMPLETE
     is one.

  2. A NAME QUOTED IN A DOCSTRING IS NOT A REFUSAL, which is the whole
     difference between a promise kept and a promise described.

  3. A SUITE IS NOT THE AGENT. tests/ carries the same header and is
     shown, never read for what the agent does.

  4. THE TWO DIRECTIONS USE DIFFERENT EVIDENCE, and a refusal passed
     through from an agent this one calls is composition, not a broken
     promise.

  5. EVERYTHING ELSE WRONG WITH A CONTRACT IS ASKED OF ITS OWNER.

  6. THE PAGE IS WRITTEN, AND EVERY FINDING ON IT IS REAL.
"""

import ast
import io
import os
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_contract as CON                                   # noqa: E402

FAILURES = []

# A module that excludes items with a card and refuses the whole call
# with a refusal. Built line by line so that neither quoting style in it
# collides with the one this file is written in.
CARDS = 'def one(these):\n    out = []\n    for item in these:\n        if bad(item):\n            out.append({"id": item, "refused": "WRONG_REVIT_VERSION"})\n            continue\n        out.append(item)\n    if not out:\n        return {"ran": False, "refused": "NOTHING_LEFT"}\n    return {"ran": True, "excluded": out}\n'


def load():
    import importlib.util
    path = os.path.join(ROOT, "tools", "generate-contract-reference.py")
    spec = importlib.util.spec_from_file_location("heron_doc_017", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


GEN = load()


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def written(body):
    """A throwaway module on disk, because the agent reads files."""
    handle, path = tempfile.mkstemp(suffix=".py")
    os.close(handle)
    io.open(path, "w", encoding="utf-8").write(body)
    return path


def main():
    print("\n1. a refusal is recognised by position, not by shape")
    path = written('''
CAPABILITY = "COUNT_DUCTS"
SHAPE = "SCREAMING_SNAKE_CASE"
STATUS = "DISCOVERED"


def one(bad):
    if bad:
        return {"ran": False, "refused": "NOT_A_THING", "why": "no"}
    if bad is None:
        return dict(ran=False, refused="INCOMPLETE")
    raise ValueError("NO_SUCH_HANDLE: nothing by that name")
''')
    try:
        found = GEN.refusals_in(path)
        check(found == {"NOT_A_THING", "INCOMPLETE", "NO_SUCH_HANDLE"},
              "the three refusals are found and nothing else: %s"
              % ", ".join(sorted(found)))
        for noise in ("COUNT_DUCTS", "SCREAMING_SNAKE_CASE", "DISCOVERED"):
            check(noise not in found,
                  "  %s is a value in this repository, not a refusal" % noise)
        check("INCOMPLETE" in found,
              "A SINGLE WORD IS A REFUSAL. INCOMPLETE, MODIFIED and PINNED "
              "are three real ones, and requiring a second word reported "
              "four agents as breaking a promise they keep")
    finally:
        os.unlink(path)

    # A CARD IS NOT A REFUSAL, and this one was a finding this agent got
    # wrong about somebody else before the rule was written.
    path = written(CARDS)
    try:
        found = GEN.refusals_in(path)
        check(found == {"NOTHING_LEFT"},
              "the RETURNED refusal is found and the APPENDED card is "
              "not: %s" % ", ".join(sorted(found)))
        check("WRONG_REVIT_VERSION" not in found,
              "  a per-item reason inside a list of excluded things is "
              "not a refusal of the CALL - the call succeeded, and "
              "declaring it would tell a caller to handle something "
              "that never comes back")
    finally:
        os.unlink(path)

    print("\n2. a name quoted in a docstring is not a refusal")
    path = written('''
"""This agent never returns "NEVER_HAPPENS" - see docs/28."""


def one():
    """It does not raise "ALSO_NEVER" either."""
    return {"ran": True, "refused": None}
''')
    try:
        check(GEN.refusals_in(path) == set(),
              "neither NEVER_HAPPENS nor ALSO_NEVER is counted - a promise "
              "DESCRIBED is not a promise KEPT, which is the whole point")
        check(GEN.mentions(path) >= {"NEVER_HAPPENS", "ALSO_NEVER"},
              "but mentions() sees both, which is the wider question")
    finally:
        os.unlink(path)

    print("\n3. a suite is not the agent")
    rows, without, _notes = GEN.collect()
    check(rows, "the real repository was read: %d contract(s)" % len(rows))
    for row in rows:
        check_once = all(one["layer"] != "test" for one in row["files"])
        if not check_once:
            check(False, "%s counted a test file as its own" % row["agent"])
            break
    else:
        check(True, "no contract row reads a tests/ file for what it does")
    orphans = [one for one in without if one["only_a_suite"]]
    check(all(one["layer"] != "test" for one in without if
              not one["only_a_suite"]),
          "and no test file is reported as an agent's own implementation")
    check(all(one["suites"] and not one["files"] for one in orphans),
          "an agent claimed by NOTHING BUT its suite is shown rather than "
          "dropped (Golden Rule 14): %s"
          % (", ".join(one["agent"] for one in orphans) or "none"))

    # THE STRONGEST CHECK ON THIS PAGE, and it is here because two bugs
    # got past everything else. tools/agent-count.py owns the count of
    # what is built, and this page reads the same headers from the same
    # roots, so the two sets must be IDENTICAL - not close.
    #
    #   ONE HEADER MAY CLAIM SEVERAL AGENTS. 31 files do. Reading the
    #   line whole made a comma-joined string into one agent that exists
    #   nowhere, and left every real one in it looking unclaimed.
    #
    #   A C# HEADER STARTS WITH `//` AND NOT `#`. The first expression
    #   here required a `#`, so it never matched a single .cs file and
    #   the whole revit/ layer was invisible - 26 agents.
    #
    # Neither showed up as an error. The page just quietly described a
    # smaller repository than the one it was standing in.
    import importlib.util
    count_spec = importlib.util.spec_from_file_location(
        "heron_agent_count", os.path.join(ROOT, "tools", "agent-count.py"))
    counter = importlib.util.module_from_spec(count_spec)
    count_spec.loader.exec_module(counter)
    theirs = set(counter.built())
    check(set(GEN.claims()) == theirs,
          "this page sees exactly the %d agent(s) agent-count.py sees - "
          "not one more, not one fewer" % len(theirs))
    check(len(rows) + len(without) == len(theirs),
          "  and every one of them is ON the page: %d with a contract plus "
          "%d without is %d" % (len(rows), len(without), len(theirs)))
    csharp = [one for one in without if one["layer"] == "revit"]
    check(csharp, "C# agents are among them - %d of them, which the first "
          "expression here saw none of" % len(csharp))
    check(not any("," in agent for agent in GEN.claims()),
          "and no agent id contains a comma, which is what a header "
          "claiming several looks like when it is read whole")
    with_suite = [row for row in rows if row["suites"]]
    check(len(with_suite) == len(rows),
          "every contract still SHOWS its suite - which file proves an "
          "agent is worth a column: %d of %d"
          % (len(with_suite), len(rows)))
    classify = [row for row in rows
                if row["agent"] == "HERON-IMP-CLS-003"]
    check(classify and "NOT_A_FOLDER" not in classify[0]["undeclared"],
          "HERON-IMP-CLS-003 does not produce NOT_A_FOLDER - that name is "
          "in its suite's FIXTURES, and counting it was a real finding "
          "that was not real")

    print("\n4. the two directions use different evidence")
    caller = written('''
import heron_walk


def one(where):
    return heron_walk.walk(where)
''')
    try:
        wide = GEN.mentions(caller)
        narrow = GEN.refusals_in(caller)
        check("NOT_A_FOLDER" in wide,
              "mentions() follows the import and sees heron_walk's "
              "NOT_A_FOLDER - a pass-through is composition, so it EXCUSES "
              "the declaration")
        check(narrow == set(),
              "refusals_in() does not - so it never ACCUSES this file of "
              "producing one it only hands on")
        check(wide > narrow,
              "wider to excuse, narrower to accuse, and being wrong in "
              "either direction the other way puts a false finding on a "
              "page whose only value is that its findings are real")
    finally:
        os.unlink(caller)
    passed = [row for row in rows if row["agent"] == "HERON-IMP-FEX-004"]
    check(passed and "NOT_A_FOLDER" in passed[0]["failures"]
          and not passed[0]["unreachable"],
          "HERON-IMP-FEX-004 declares NOT_A_FOLDER, never writes it, and is "
          "not accused - HERON-IMP-FIL-002 produces it and FEX-004 hands "
          "the answer back")

    print("\n5. everything else is asked of the contract's owner")
    logic = io.open(os.path.join(ROOT, "tools",
                                 "generate-contract-reference.py"),
                    encoding="utf-8").read()
    calls = set()
    for node in ast.walk(ast.parse(logic)):
        if isinstance(node, ast.Call) and isinstance(node.func, ast.Attribute):
            if getattr(node.func.value, "id", None) == "CON":
                calls.add(node.func.attr)
    check("validate" in calls,
          "CON.validate is called - a second opinion written here would "
          "drift from it (Golden Rule 3)")
    check("contracts" in calls and "registry_ids" in calls,
          "and so are contracts() and registry_ids(): %s"
          % ", ".join(sorted(calls)))
    check("yaml" not in logic.lower().split("def collect")[0]
          or "import yaml" not in logic,
          "the file is never parsed here - its owner loads it once")
    check(sum(len(row["problems"]) for row in rows) == 0,
          "and heron_contract reports nothing wrong with any of the %d"
          % len(rows))

    print("\n6. the page is written, and every finding on it is real")
    out = os.path.join(tempfile.mkdtemp(), "page.html")
    was = os.environ.get("HERON_CONTRACT_REFERENCE_OUT")
    GEN.OUT = out
    try:
        check(GEN.main() == 0, "the generator runs clean")
        page = io.open(out, encoding="utf-8").read()
        check("__DATA__" not in page, "the payload is substituted in")
        check(page.lstrip().startswith("<title>"),
              "and the page names itself first")
        for row in rows[:40]:
            if row["agent"] not in page:
                check(False, "%s is missing from the page" % row["agent"])
                break
        else:
            check(True, "every agent checked appears on it")
    finally:
        if was is not None:
            os.environ["HERON_CONTRACT_REFERENCE_OUT"] = was

    # EVERY FINDING VERIFIED AGAINST THE SOURCE, not taken on trust. A page
    # of findings that are wrong is worse than no page: it teaches the
    # reader to skip the table, which is where the real ones are.
    findings = 0
    for row in rows:
        for name in row["undeclared"]:
            findings += 1
            source = "".join(
                io.open(os.path.join(ROOT, one["file"]), encoding="utf-8")
                .read() for one in row["files"])
            check(name in source and name not in row["failures"],
                  "%s: %s is in the code and not in the contract"
                  % (row["agent"], name))
        for name in row["unreachable"]:
            findings += 1
            check(name in row["failures"],
                  "%s: %s is declared" % (row["agent"], name))
    check(findings == sum(len(row["undeclared"]) + len(row["unreachable"])
                          for row in rows),
          "all %d finding(s) checked against the source" % findings)
    check(not any(row["no_file"] for row in rows),
          "and every contract has a file that claims it")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a promise the code does not keep, and nothing else")
    return 0


if __name__ == "__main__":
    sys.exit(main())
