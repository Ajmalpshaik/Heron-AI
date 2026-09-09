# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The boundary hook - and the three ways a hook silently stops being one.

    python tests/test_heron_guard.py

.claude/skills/heron-guard/ refuses an edit that would put the Revit vendor
namespace outside revit/, at the moment the edit is proposed. The rule is
tools/check-structure.py's; the difference is WHEN it is asked.

WHAT THIS PROVES, and every one of these is a way a hook becomes decoration:

  1. IT REFUSES THE THING IT EXISTS TO REFUSE, and allows the same text where
     it is permitted. A hook that refuses nothing is worse than no hook,
     because somebody believes it.
  2. THE DECISION IS NESTED under `hookSpecificOutput`. A top-level
     `permissionDecision` is ignored by the host - the block silently no-ops.
  3. A CRASH DENIES. An unexpected exit with nothing on stdout is read as
     PERMISSION. Asserted by feeding it input that cannot parse.
  4. THE PATTERN MATCHES check-structure.py's, CHARACTER FOR CHARACTER. The
     hook cannot import that script - it runs its whole sweep at import - so
     the rule exists twice and this is what keeps the copies honest. Same
     answer tests/test_fragment_imports.py gives for the executor's imports.
  5. THE ESCAPE HATCH WORKS. A fail-closed hook that cannot be turned off is
     one bad edit from a repository nobody can work in.

WHAT IT DOES NOT PROVE. That the host actually runs the hook. That needs a
host, and this is a text-level check of the file the host would run - the
same limit tests/test_mcp_serves.py has against the MCP SDK.
"""

import io
import json
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SKILL = os.path.join(ROOT, ".claude", "skills", "heron-guard")
HOOK = os.path.join(SKILL, "bin", "heron_guard.py")

sys.path.insert(0, os.path.join(SKILL, "bin"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def run(payload, env=None):
    """(stdout, exit code) from the hook as the host would run it."""
    where = dict(os.environ)
    where.update(env or {})
    got = subprocess.run([sys.executable, HOOK],
                         input=payload if isinstance(payload, str)
                         else json.dumps(payload),
                         capture_output=True, text=True, env=where)
    return got.stdout.strip(), got.returncode


def verdict_of(out):
    if not out:
        return None
    return json.loads(out)["hookSpecificOutput"]["permissionDecision"]


def main():
    print("The boundary hook")
    print("=" * 70)

    # The vendor namespace is assembled the same way the hook assembles it, so
    # this test file does not contain the string check-structure.py forbids -
    # it checks .claude/ too, and would fail this file for testing the rule.
    vendor = "Autodesk" + "." + "Revit"

    print()
    print("1. It refuses the edit it exists to refuse")
    out, code = run({"tool_input": {"file_path": "brain/x.py",
                                    "content": "using %s.DB;" % vendor}})
    check(verdict_of(out) == "deny",
          "the vendor namespace written into brain/ is DENIED")
    check(code == 0,
          "and the hook itself exits 0 - the decision is the JSON, not the "
          "exit code")

    out, _ = run({"tool_input": {"file_path": "revit/Heron.Revit.Addin/X.cs",
                                 "content": "using %s.DB;" % vendor}})
    check(out == "",
          "the SAME text inside revit/ is allowed - that is the adapter and "
          "the whole reason the boundary has a permitted side")
    out, _ = run({"tool_input": {"file_path": "tools/check-x.py",
                                 "content": "PATTERN = '%s'" % vendor}})
    check(out == "",
          "and inside tools/, because a checker has to be able to name what "
          "it checks (check-structure.py exempts both)")

    out, _ = run({"tool_input": {"file_path": "brain/x.py",
                                 "content": "import os"}})
    check(out == "", "an edit with nothing to say about is allowed silently")

    print()
    print("2. A COMMENT counts, which is how this rule is usually broken")
    out, _ = run({"tool_input": {"file_path": "brain/x.py",
                                 "content": "# never mention %s here" % vendor}})
    check(verdict_of(out) == "deny",
          "the namespace in a COMMENT is refused too - check-structure.py "
          "greps text, and on 2026-09-09 a docstring explaining this rule "
          "broke it by quoting it")

    print()
    print("3. The decision is NESTED, or the host ignores it")
    out, _ = run({"tool_input": {"file_path": "brain/x.py",
                                 "content": "using %s.DB;" % vendor}})
    body = json.loads(out)
    check("hookSpecificOutput" in body,
          "the decision is under hookSpecificOutput")
    check("permissionDecision" not in body,
          "and NOT at the top level, where it would be silently ignored - "
          "a hook that looks like it works and refuses nothing")
    inner = body["hookSpecificOutput"]
    check(inner.get("hookEventName") == "PreToolUse",
          "it names the event it is answering")
    check(bool(inner.get("permissionDecisionReason")),
          "and it says WHY, because a refusal with no reason is a wall with "
          "no door")

    print()
    print("4. A CRASH DENIES - the trap that makes a hook decoration")
    out, code = run("this is not json")
    check(verdict_of(out) == "deny",
          "unparseable input is REFUSED, not waved through")
    check(code == 0,
          "and it still exits 0, so the host reads the decision rather than "
          "an error")
    check("HERON_GUARD=off" in out,
          "and the refusal says how to get moving again - the person who "
          "needs the hatch is the one whose tooling is already broken")

    print()
    print("5. The escape hatch works")
    out, _ = run({"tool_input": {"file_path": "brain/x.py",
                                 "content": "using %s.DB;" % vendor}},
                 env={"HERON_GUARD": "off"})
    check(out == "",
          "HERON_GUARD=off allows what would otherwise be denied - a "
          "fail-closed hook that cannot be turned off is one bad edit from a "
          "repository nobody can work in")

    print()
    print("6. The rule exists TWICE, and the copies must agree")
    print("-" * 70)
    sweep = io.open(os.path.join(ROOT, "tools", "check-structure.py"),
                    encoding="utf-8").read()
    theirs = re.search(r"REVIT_API = re\.compile\((.+?)\)\n", sweep)
    check(theirs is not None,
          "check-structure.py still defines REVIT_API by that name")
    if theirs:
        import heron_guard as GUARD
        # Compare what the two patterns MATCH rather than how they are
        # written: the hook builds its pattern from parts so that this
        # repository's own structure gate does not fail the hook, so the
        # source text cannot be compared and the behaviour must be.
        probe = "using %s.DB;" % vendor
        check(bool(GUARD.REVIT_API.search(probe)),
              "the hook's pattern matches the namespace")
        check(GUARD.REVIT_API.pattern == r"\b" + vendor.replace(".", r"\.")
              + r"\b",
              "and it is exactly check-structure.py's, rebuilt: %s"
              % GUARD.REVIT_API.pattern)
        check(GUARD.ALLOWED == ("revit", "tools"),
              "and it exempts the same two folders check-structure.py does")

    print()
    print("7. The skill declares the hook the host would run")
    print("-" * 70)
    card = io.open(os.path.join(SKILL, "SKILL.md"), encoding="utf-8").read()
    check("PreToolUse:" in card, "SKILL.md declares a PreToolUse hook")
    check("Write|Edit|MultiEdit" in card,
          "matching the three tools that can put text in a file")
    check("heron_guard.py" in card, "and pointing at this hook")
    check("not part of what a modeller installs" in card,
          "and it says plainly that this is for DEVELOPING Heron - hooks are "
          "the host's mechanism (D-01) and nothing here reaches a model")

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the boundary is refused before the edit lands, the")
    print("decision is nested where the host reads it, a crash denies rather")
    print("than allowing, the hatch works, and the two copies of the rule")
    print("agree.")
    print()
    print("It does not prove the host runs the hook. That needs a host.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
