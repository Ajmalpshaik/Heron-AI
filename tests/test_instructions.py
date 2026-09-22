# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-PRO-011
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The instruction registry - assembled from one source, and nothing copied.

    python tests/test_instructions.py

WHAT IT PROVES
  1. THE CONSTITUTION PARSES INTO ITS 30 NUMBERED RULES, including the three
     with a letter (12a, 12b, 12c). A parser that silently found 27 would
     drop three rules about which model is being written to, which are the
     three it can least afford to drop.

  2. AN ARTICLE IS ASSEMBLED, NOT COPIED. The composed instruction carries the
     Constitution's own words because it read them, and the registry refuses
     an instruction that pastes them instead. This is the whole reason the
     module exists.

  3. INCLUDES COMPOSE, AND A CYCLE IS REFUSED RATHER THAN HUNG.

  4. A MISSING INSTRUCTION, INCLUDE OR ARTICLE RAISES - each one a failure
     state the contract declares. A composer that returned a partial
     instruction would be an agent running without a rule it was never given.

  5. AN INSTRUCTION WITH NO EVALUATION CASE IS REFUSED (docs/23 s9).

  6. THE REAL REGISTRY VALIDATES AND ITS CASES PASS - the check that another
     commit can break, which is what makes it worth running.

  7. A MODIFY AGENT IS NOT TOLD ITS PERMISSION STOPS AT READING. agent.modify
     included agent.read for one commit and inherited exactly that sentence.
     The case that catches it lives in the registry; this asserts the registry
     still carries that case, because the case is the guard.

  8. EVERY CHAT IS GIVEN `host.chat`, AND IT STAYS SHORT. The MCP server
     hands the host the registry's HOST instruction - read here through the
     same seam the server uses, heron_brain.host_instructions - with its
     house rules guarded by cases and Article 9 arriving WITH its one
     recorded exception, D-99. It is read in every chat, so it has a budget.

  9. THE CONSTITUTION'S ENFORCEMENT TABLE SAYS WHAT THE REGISTRY DOES. Its
     third column said "yes" on every row while nothing handed an Article to
     a running AI. Every Article it lists is now classified - every chat,
     assembled but not given, or no - and each class is checked against what
     the instructions actually assemble, so the table cannot drift back.

 10. AN ARTICLE NEVER CARRIES AWAY THE RULE THAT CLOSES ITS SECTION. The last
     rule of each Article used to end in the Constitution's `---` separator,
     and that line reached every chat's instructions.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_instructions as INS                              # noqa: E402

CONSTITUTION = os.path.join(ROOT, "HERON_CONSTITUTION.md")
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")

# WHAT EVERY CHAT PAYS FOR, whatever it asks - docs/19: "if a context
# assembly exceeds its budget, that is a bug in retrieval, not a reason to
# raise the budget". host.chat assembled to 620 words the day it was written;
# the budget leaves room for an Article or two and not for a second
# Constitution. Raising it is a decision, and the reason goes here.
HOST_BUDGET_WORDS = 800

# The three words the Enforcement table's third column may use, and what
# each one claims.
EVERY_CHAT = "every chat"
NOT_GIVEN = "assembled, not given"
NOWHERE = "no"

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def raises(call, kind):
    try:
        call()
    except kind:
        return True
    except Exception:                                        # noqa: BLE001
        return False
    return False


def enforcement_rows():
    """
    [(articles in the first column, {class: articles} from the third)] out of
    the Constitution's Enforcement table, read as text.

    A cell the three words do not describe comes back under its own text, so
    a stray "yes" is reported by name rather than skipped.
    """
    rows, inside = [], False
    with io.open(CONSTITUTION, encoding="utf-8") as fh:
        for line in fh:
            if line.startswith("## "):
                inside = line.strip() == "## Enforcement"
                continue
            if not inside or not line.startswith("| ") or line.startswith("| Article"):
                continue
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if len(cells) < 3:
                continue
            listed = [a.strip() for a in cells[0].split(",") if a.strip()]
            claims = {}
            for part in cells[-1].split("\u00b7"):
                label, _, numbers = part.partition(":")
                label = label.strip()
                claims.setdefault(label, []).extend(
                    n.strip() for n in numbers.split(",") if n.strip())
            rows.append((listed, claims))
    return rows


def host_and_table(rules, known):
    print()
    print("8. Every chat is given host.chat, and it stays short")
    host = getattr(INS, "HOST", None)
    check(host is not None, "the registry names the instruction every chat is given")
    check(host in known, "and it is an instruction on disk (%s)" % host)
    # A SECTION THAT CANNOT RUN IS WORTH LESS THAN ONE THAT FAILS (heron-ship
    # 2a). With no host instruction nothing reaches a chat, and the table
    # below is still checked against exactly that.
    text, used = INS.compose(host) if host in known else ("", [])
    try:
        import heron_brain as BRAIN
        given = BRAIN.host_instructions()
    except Exception as why:                                 # noqa: BLE001
        given = "(%s: %s)" % (type(why).__name__, why)
    check(given == text,
          "the seam the MCP server uses hands over exactly what the registry "
          "assembles")
    server = io.open(SERVER, encoding="utf-8").read()
    check('instructions=_host_instructions()' in server
          and "brain.host_instructions()" in server,
          "and the server is built with it as its instructions")
    words = len(text.split())
    check(0 < words <= HOST_BUDGET_WORDS,
          "it is %d words - something, and within the %d every chat pays for"
          % (words, HOST_BUDGET_WORDS))
    guards = [c for c in (known.get(host) or {}).get("cases") or []]
    said = " ".join(" ".join(c.get("must-contain") or []) for c in guards)
    for rule in ("Revit's own Undo", "Never make a second change to reverse",
                 "Never call a model, a system or a design",
                 "ask the modeller what it means"):
        check(rule in said and rule in text,
              "a case guards, and the text says: %s" % rule)
    check("9" in used and "D-99" in rules.get("9", ""),
          "Article 9 arrives assembled, carrying its one recorded exception")
    check("You are one agent inside Heron" not in text,
          "and no internal agent's brief is mixed into it")

    print()
    print("9. The Enforcement table says what the registry does")
    chat = set(used)
    others = set()
    for key in known:
        if key != host:
            others.update(INS.compose(key)[1])
    others -= chat
    rows = enforcement_rows()
    check(len(rows) >= 8, "the table's rows were read - %d" % len(rows))
    everywhere = set()
    for listed, claims in rows:
        where = ", ".join(listed)
        classified = [a for numbers in claims.values() for a in numbers]
        everywhere.update(listed)
        stray = sorted(set(claims) - set([EVERY_CHAT, NOT_GIVEN, NOWHERE]))
        check(not stray, "row %s says only what the three words mean%s"
              % (where, "" if not stray else " - it also says %r" % stray))
        check(sorted(classified) == sorted(listed),
              "row %s classifies each of its Articles exactly once" % where)
        for number in listed:
            truth = (EVERY_CHAT if number in chat
                     else NOT_GIVEN if number in others else NOWHERE)
            said = [label for label, numbers in claims.items() if number in numbers]
            check(said == [truth],
                  "Article %s: the table says %s, the registry says %s"
                  % (number, said or "nothing", truth))
    check(everywhere == set(rules),
          "and every one of the Constitution's %d Articles has a row%s"
          % (len(rules), "" if everywhere == set(rules) else
             " - missing: %s" % ", ".join(sorted(set(rules) - everywhere))))


def main():
    rules = INS.articles()

    print("1. The Constitution parses into its numbered rules")
    check(len(rules) == 30, "30 numbered rules found, not 27")
    check(all(n in rules for n in ("12a", "12b", "12c")),
          "the three lettered rules about which model survive the parse")
    check("one press of undo" in rules["8"].lower()
          or "one step" in rules["8"].lower(),
          "article 8 carries its own words, read from the file")

    print()
    print("2. Assembled, never copied")
    text, used = INS.compose("agent.modify")
    check("12a" in used and "9" in used,
          "agent.modify assembles the articles it declares")
    check(rules["9"].splitlines()[0] in text,
          "the article's own wording reaches the agent because it was read")

    pasted = {
        "copycat": {
            "id": "copycat", "version": "1.0.0", "purpose": "x",
            "text": rules["9"], "cases": [{"name": "c",
                                           "must-contain": ["x"]}],
            "_path": "copycat.yaml",
        }
    }
    problems = INS.validate(pasted, rules)
    check(any("repeats the wording of article" in p for p in problems),
          "an instruction that pastes an article is refused, and told to "
          "declare it instead")

    print()
    print("3. Includes compose, and a cycle is refused")
    check("State your evidence" in text,
          "agent.modify carries what agent.base gives it")
    loop = {
        "a": {"id": "a", "version": "1.0.0", "purpose": "p", "text": "A",
              "includes": ["b"], "cases": [{"name": "c"}], "_path": "a.yaml"},
        "b": {"id": "b", "version": "1.0.0", "purpose": "p", "text": "B",
              "includes": ["a"], "cases": [{"name": "c"}], "_path": "b.yaml"},
    }
    check(raises(lambda: INS.compose("a", loop, rules), ValueError),
          "a -> b -> a is reported as a cycle, not followed")
    check(any("INCLUDE_CYCLE" in p for p in INS.validate(loop, rules)),
          "validate() names it INCLUDE_CYCLE rather than hanging on it")

    print()
    print("3b. An included instruction's articles arrive once, not twice")
    modify, used = INS.compose("agent.modify")
    check(len(used) == len(set(used)), "no article number is collected twice")
    first_line = rules["22"].splitlines()[0]
    check(modify.count(first_line) == 1,
          "agent.base's article 22 appears once in agent.modify")
    check(modify.count("Rules you may not violate") == 1,
          "and the line that introduces them appears once")

    print()
    print("4. Missing things raise, one per declared failure state")
    check(raises(lambda: INS.compose("no.such.instruction"), KeyError),
          "an unknown instruction raises")
    missing_include = {
        "x": {"id": "x", "version": "1.0.0", "purpose": "p", "text": "X",
              "includes": ["gone"], "cases": [{"name": "c"}], "_path": "x.yaml"}
    }
    check(raises(lambda: INS.compose("x", missing_include, rules), KeyError),
          "an unknown include raises")
    missing_article = {
        "y": {"id": "y", "version": "1.0.0", "purpose": "p", "text": "Y",
              "articles": [99], "cases": [{"name": "c"}], "_path": "y.yaml"}
    }
    check(raises(lambda: INS.compose("y", missing_article, rules), KeyError),
          "an article that is not in the Constitution raises")
    check(any("not in HERON_CONSTITUTION.md" in p
              for p in INS.validate(missing_article, rules)),
          "and validate() says which article it was")

    print()
    print("4b. Two files claiming one id is refused, not silently merged")
    # THE CLAIM IS: a duplicate is refused for a directory OUTSIDE the repo root.
    # mkdtemp() lands on C: while the repository is on D:, and os.path.relpath
    # RAISES across drives on Windows - which it did, inside instructions(),
    # BEFORE the duplicate check could run. On Linux the tempdir is on the same
    # filesystem, so these two checks pass there whether the guard exists or
    # not. Do not delete heron_instructions' repo_relative() call as redundant:
    # that is exactly how A14 was "fixed on Linux, where the defect cannot
    # appear". The bare `except ValueError` below is also why this reported
    # itself as a duplicate-detection failure for days - it cannot tell this
    # module's deliberate refusal from the stdlib's cross-drive crash.
    import tempfile, shutil
    workspace = tempfile.mkdtemp(prefix="heron-instructions-")
    try:
        for name in ("a.yaml", "b.yaml"):
            io.open(os.path.join(workspace, name), "w",
                    encoding="utf-8").write(
                "id: same.id\nversion: 1.0.0\npurpose: p\ntext: t\n"
                "cases:\n  - name: c\n")
        was, INS.INSTRUCTIONS_DIR = INS.INSTRUCTIONS_DIR, workspace
        raised = ""
        try:
            INS.instructions()
        except ValueError as exc:
            raised = str(exc)
        finally:
            INS.INSTRUCTIONS_DIR = was
        check("INSTRUCTION_DUPLICATED" in raised,
              "a duplicate id is refused by name")
        check("a.yaml" in raised and "b.yaml" in raised,
              "and both files are named, so neither is the silent loser")
    finally:
        shutil.rmtree(workspace)

    print()
    print("5. An instruction nothing tests is refused")
    untested = {
        "z": {"id": "z", "version": "1.0.0", "purpose": "p", "text": "Z",
              "cases": [], "_path": "z.yaml"}
    }
    check(any("no evaluation case" in p for p in INS.validate(untested, rules)),
          "no evaluation case is a refusal, not a warning")

    print()
    print("5b. A case that asserts nothing is not a case")
    empty_case = {
        "z": {"id": "z", "version": "1.0.0", "purpose": "p", "text": "Z",
              "cases": [{"name": "smoke"}], "_path": "z.yaml"}
    }
    check(any("asserts nothing" in p
              for p in INS.validate(empty_case, rules)),
          "a case with a name and no assertion is refused")

    print()
    print("5c. An assertion list written as a string is refused")
    as_string = {
        "y": {"id": "y", "version": "1.0.0", "purpose": "p", "text": "doing",
              "cases": [{"name": "c", "must-contain": "do"}], "_path": "y.yaml"}
    }
    problems = INS.validate(as_string, rules)
    check(any("one character at a time" in p for p in problems),
          "a bare string would be scored one character at a time, and is "
          "refused")
    _passed, case_failures = INS.run_cases(as_string, rules)
    check(any("bare string" in f for f in case_failures),
          "and run_cases() refuses to score it rather than passing it")

    print()
    print("6. The registry in this repository")
    known = INS.instructions()
    check(len(known) >= 3, "there are instructions to check")
    problems = INS.validate(known, rules)
    check(problems == [], "every instruction validates")
    for line in problems:
        print("        %s" % line)
    passed, case_failures = INS.run_cases(known, rules)
    check(passed > 0 and case_failures == [],
          "%d evaluation assertion(s) pass, none fail" % passed)
    for line in case_failures:
        print("        %s" % line)

    print()
    print("7. The guard that caught the composition bug is still there")
    modify = known.get("agent.modify", {})
    guard = [c for c in modify.get("cases") or []
             if "Your permission stops at reading"
             in (c.get("must-not-contain") or [])]
    check(bool(guard),
          "agent.modify still asserts it is NOT told it may only read")
    check("Your permission stops at reading" not in text,
          "and the assembled text does not contain it")

    host_and_table(rules, known)

    print()
    print("10. An article never carries away the rule that closes its section")
    trailing = sorted(n for n, body in rules.items()
                      if body.rstrip().endswith("---"))
    check(not trailing,
          "no article's text ends in the Constitution's section separator%s"
          % ("" if not trailing else " - it does in: %s" % ", ".join(trailing)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    instructions assemble from one source, and nothing copies")
    return 0


if __name__ == "__main__":
    sys.exit(main())
