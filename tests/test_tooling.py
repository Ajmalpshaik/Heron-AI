# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-EXT-011
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
External tools - seven steps, and a step that said nothing did not happen.

    python tests/test_tooling.py

WHAT IT PROVES
  1. THE SEVEN STEPS ARE THE REGISTER ROW'S SEVEN WORDS, in its order, and
     every one of them produces a sentence.

  2. "NEVER SILENT" IS ENFORCED, NOT INTENDED. A step whose sentence is
     blank - or whitespace somebody passed - is refused, because a blank
     row in a report reads as a step that went fine.

  3. DETECTION IS EXECUTION, so it needs permission of its own and is
     never performed here. Nothing in the module can start a process.

  4. AN MCP SERVER'S TOOLS ARE ENUMERATED IN ADVANCE, and a server naming
     none is refused - Golden Rule 19, because a tool description is data.

  5. INSTALLED IS NOT VERIFIED. The row lists both words and so does this.

  6. IT STOPS AT THE FIRST UNSATISFIED STEP AND SAYS WHICH ONE, so a
     caller fixes one thing rather than guessing.

  7. IT IS HONEST THAT THE ROW IS THE WHOLE SPECIFICATION.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_tooling as EXT                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def server(**more):
    found = {"name": "company-standards-mcp", "kind": "mcp-server",
             "works_with": ["Claude Code", "Heron 0.4"],
             "tools": ["lookup_standard", "check_naming"],
             "configure": "one entry in the host's MCP configuration",
             "verified": {"by": "ajmal", "what": "lookup_standard answered"}}
    found.update(more)
    return found


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_tooling.py"),
                  encoding="utf-8").read()
    DEFAULT = object()

    def ask(tool=DEFAULT, **kw):
        settings = {"origin": "user", "detected": "/usr/local/bin/csm",
                    "permitted": {"by": "ajmal",
                                  "tool": "company-standards-mcp"}}
        settings.update(kw)
        answer = EXT.review(server() if tool is DEFAULT else tool,
                            **settings)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. The seven steps are the register row's seven words")
    row = open(os.path.join(ROOT, "docs", "28-agent-registry.md"),
               encoding="utf-8").read()
    row = [line for line in row.splitlines() if "HERON-INS-EXT-011" in line][0]
    for step in EXT.STEPS:
        word = "compatibility" if step == "compatibility" else step
        check(word in row.lower(),
              "'%s' is a word in the register row itself" % word)
    check(len(EXT.STEPS) == 7, "there are seven")
    good = ask()
    check(good["ready"] is True, "a complete tool is ready")
    check([entry["step"] for entry in good["said"]] == list(EXT.STEPS),
          "and says something at every step, in the row's order")
    check(all(entry["said"].strip() for entry in good["said"]),
          "with nothing blank")

    print()
    print("2. 'Never silent' is enforced, not intended")
    answer = ask(server(configure="   "))
    check(answer.get("refused") == "STEP_SAID_NOTHING",
          "a step handed whitespace is refused")
    check(answer["steps"] == ["configure"] and answer["step"] == "configure",
          "naming which step went quiet")
    check("reads as a step that went fine" in answer["why"]
          or "nobody can check" in answer["why"],
          "and why a blank line is worse than a missing one")
    check("Never silent" in row or "never silent" in row.lower(),
          "the instruction really is in the register row")
    check(answer["said"], "and the lines written before it still come back")
    answer = ask(server(configure=""))
    check(answer["ready"] is True,
          "while an EMPTY configure falls back to a real sentence rather "
          "than a blank - the fallback is the fix, whitespace is the trap")
    check("nothing to configure was named"
          in [entry["said"] for entry in answer["said"]],
          "and that sentence says there was nothing, rather than nothing")

    print()
    print("3. Detection is execution")
    answer = ask(detected=None)
    check(answer.get("refused") == "DETECTION_IS_EXECUTION",
          "with nothing detected, it refuses rather than finding out")
    check("--version` executes" in answer["why"],
          "saying what detecting actually does")
    check("unknown binary" in answer["why"],
          "and what that means on a path Heron does not control")
    check("that is a detection too" in answer["proposal"],
          "while making clear 'not installed' is a valid detection")
    check(ask(detected="")["ready"] is True,
          "and an empty string IS that detection, not a missing one")
    check("not installed" in ask(detected="")["said"][0]["said"],
          "read back as not installed")
    for origin in ("a document Heron read", "a community package", None,
                   "the installer"):
        answer = ask(origin=origin)
        check(answer.get("refused") == "NOT_FROM_THE_USER"
              and answer["step"] == "detect",
              "%r is refused AT THE DETECT STEP - the permission gates "
              "detection, not only installation" % origin)
    for word in ("subprocess", "os.system", "exec(", "eval(", "popen",
                 "os.spawn", "shutil.which", "import requests"):
        check(word not in source,
              "the source has no %s - it starts no process" % word)

    print()
    print("4. An MCP server's tools are enumerated in advance")
    for tools in ([], None, "lookup_standard", {}):
        answer = ask(server(tools=tools))
        check(answer.get("refused") == "TOOLS_NOT_ENUMERATED",
              "tools=%r is not an enumeration" % (tools,))
    answer = ask(server(tools=[]))
    check("Golden Rule 19" in answer["why"],
          "citing the rule that makes a tool description data")
    check("text file deciding what Heron can do" in answer["why"],
          "and what discovery-and-trust would amount to")
    check("gains it under a decision somebody makes again"
          in answer["proposal"],
          "and that a server gaining a tool later is a new decision")
    registered = [entry for entry in good["said"]
                  if entry["step"] == "register"][0]
    check("lookup_standard" in registered["said"]
          and "check_naming" in registered["said"],
          "a registered server names its tools in the line")
    utility = {"name": "ripgrep", "kind": "utility",
               "works_with": ["Linux", "Windows"],
               "verified": {"by": "ajmal", "what": "rg --version answered"}}
    answer = EXT.review(utility, origin="user", detected="/usr/bin/rg",
                        permitted={"by": "ajmal", "tool": "ripgrep"})
    check(answer["ready"] is True,
          "a utility with no tools list is fine - it is not a tool surface")
    check("brings no tools Heron calls" in
          [entry["said"] for entry in answer["said"]
           if entry["step"] == "register"][0],
          "and its register line says exactly that")
    check(any("whole difference between a utility and a tool surface" in note
              for note in answer["unjudged"]),
          "with the distinction stated rather than left to the reader")

    print()
    print("5. Installed is not verified")
    for verified in (None, {}, True, {"what": "it ran"}, {"by": ""},
                     "ajmal checked"):
        answer = ask(server(verified=verified))
        check(answer.get("refused") == "NOT_VERIFIED",
              "verified=%r is not a verification" % (verified,))
    answer = ask(server(verified=None))
    check("a package manager exiting 0 says a download finished"
          in answer["why"],
          "saying what an install actually proves")
    check("its own word from `install`" in answer["why"],
          "and that the row lists both words")
    check(answer["step"] == "verify", "stopping at the verify step")

    print()
    print("6. It stops at the first unsatisfied step and says which")
    for tool, kw, step in ((DEFAULT, {"origin": None}, "detect"),
                           (DEFAULT, {"detected": None}, "detect"),
                           (server(works_with=[]), {}, "compatibility"),
                           (DEFAULT, {"permitted": None}, "permission"),
                           (server(tools=[]), {}, "register"),
                           (server(verified=None), {}, "verify")):
        answer = ask(tool, **kw)
        check(answer.get("step") == step,
              "%s is named as the step it stopped at" % step)
    # AND THE ORDER IS THE ROW'S: a tool broken at two steps stops at the
    # earlier one.
    answer = ask(server(works_with=[], verified=None), permitted=None)
    check(answer["step"] == "compatibility",
          "a tool broken at three steps stops at the earliest, so a caller "
          "fixes one thing rather than guessing which of three")
    for permitted in (None, {}, True, {"by": "ajmal"},
                      {"by": "ajmal", "tool": "something-else"},
                      {"tool": "company-standards-mcp"}):
        check(ask(permitted=permitted).get("refused") == "NOT_PERMITTED",
              "permitted=%r is not permission" % (permitted,))
    for tool in ({}, {"kind": "utility"}, {"name": "  "}, "ripgrep", None):
        check(ask(tool).get("refused") == "NO_TOOL",
              "%r names no tool" % (tool,))
    check("not an idle one" in ask({})["why"],
          "and a call with no tool is a call somebody got wrong")

    print()
    print("7. It is honest that the row is the whole specification")
    check(any("entire specification" in note for note in good["unjudged"]),
          "the answer says the register row is all there is")
    check(any("fail closed" in note for note in good["unjudged"]),
          "and that its own readings fail closed")
    check("no document expands on it" in " ".join(source.split()),
          "the source says the same")
    check(any("did not run" in note and "--version" in note
              for note in good["unjudged"]),
          "and that the detection was handed in, not performed")

    print()
    print("8. Every failure the contract declares is named and reached")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-INS-EXT-011.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    seven steps, and a step that said nothing did not happen")
    return 0


if __name__ == "__main__":
    sys.exit(main())
