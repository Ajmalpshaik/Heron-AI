# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-ARC-001
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
The Workspace Architect - the only place two plans are seen together.

    python tests/test_workspace.py

WHAT IT PROVES
  1. THE NINE IT ORDERS ARE THE DEPARTMENT'S TWELVE MINUS THREE, and the
     twelve are read out of docs/28 rather than typed here. Each of the
     three exclusions has a reason the suite states.

  2. THE CONFLICT NOBODY ELSE CAN SEE IS FOUND - a path INSIDE another
     agent's path, in either order. And the two cases that are NOT
     conflicts stay clear: one agent twice, and two reads.

  3. ADMIN DOES NOT MEAN IT MAY ACT. It runs nothing, it approves
     nothing, and it refuses a list containing itself - Golden Rule 7,
     read out of docs/14.

  4. docs/06 s142's GATE IS REAL AND IS THE DOCUMENT'S, read out of it:
     cleanup and repair never touch the data class unconfirmed.

  5. THE ORDER IS THE DECLARED ONE, and a backup never lands after a
     migration.

  6. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND
     REACHED, and the code refuses nothing the contract omits.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_workspace as ARC                                  # noqa: E402
import heron_paths as PATHS                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_workspace.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]

    def ask(intents, **kw):
        answer = ARC.review(intents, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    def intent(agent, does, path):
        return {"agent": "HERON-WSP-" + agent, "does": does, "path": path}

    print("1. The nine are the department's twelve minus three")
    register = io.open(os.path.join(ROOT, "docs", "28-agent-registry.md"),
                       encoding="utf-8").read()
    block = register.split("## 12. Workspace & Folder Architecture")[1]
    block = block.split("\n## 13.")[0]
    twelve = re.findall(r"`(HERON-WSP-[A-Z]{3}-\d{3})`", block)
    check(len(set(twelve)) == 12,
          "docs/28's department really lists 12 agents")
    ordered = set(agent for agent, _why in ARC.ORDER)
    check(len(ARC.ORDER) == 9, "the table orders 9 of them")
    left_out = set(twelve) - ordered
    check(left_out == {ARC.SELF, ARC.RESTORE, "HERON-WSP-PTH-007"},
          "and the three left out are itself, the restore, and the path "
          "table")
    check(ordered < set(twelve),
          "every one it orders is in the department - none invented")
    # THE REASONS, each checkable against the register or the rules.
    check("no agent approves itself" in io.open(
        os.path.join(ROOT, "docs", "14-golden-rules.md"),
        encoding="utf-8").read().lower(),
        "itself, because docs/14 says no agent approves itself")
    check("Restore and rebuild" in block,
          "the restore, because docs/28 calls it restore and rebuild - the "
          "other direction")
    pth = [line for line in block.splitlines() if "PTH-007" in line][0]
    check(pth.split("|")[5].strip() in ("", "—"),
          "and the path table, because docs/28 gives it no risk at all: it "
          "classifies, it never touches")

    print("\n2. The conflict nobody else can see")
    for first, second, how in (
            (intent("CLN-009", "move", "Fragments"),
             intent("PLC-005", "write", "Fragments/select.json"),
             "a parent archived while a child is written"),
            (intent("PLC-005", "write", "Fragments/select.json"),
             intent("CLN-009", "move", "Fragments"),
             "and the same pair the other way round"),
            (intent("CLN-009", "remove", "Logs/old.txt"),
             intent("REP-004", "move", "Logs/old.txt"),
             "two agents on one exact path")):
        answer = ask([first, second])
        check(answer.get("refused") == "PLANS_CONFLICT", how)
        check(len(answer.get("conflict") or []) == 2,
              "  and both intents come back, so a person can choose")
    # AND THE TWO THAT ARE NOT CONFLICTS.
    same = ask([intent("CLN-009", "move", "Logs/a"),
                intent("CLN-009", "remove", "Logs/a/b")],
               confirmed=["Logs/a", "Logs/a/b"])
    check(not same.get("refused"),
          "one agent twice on its own paths is NOT a conflict - its plan is "
          "its own business")
    reads = ask([intent("VAL-003", "read", "Fragments"),
                 intent("REG-012", "read", "Fragments/select.json")])
    check(not reads.get("refused"),
          "and two READS of one path are not a conflict either")
    check("Fragments" not in str(reads.get("conflict") or ""),
          "nothing was reported as colliding")

    print("\n3. ADMIN does not mean it may act")
    good = ask([intent("BAK-010", "read", "Brain"),
                intent("CRE-002", "create", "Company")])
    check(good["approved"] is False, "`approved` is false on a clean run")
    itself = ask([intent("ARC-001", "write", "Core")])
    check(itself.get("refused") == "WOULD_APPROVE_ITSELF",
          "a list containing ARC-001 is refused - Golden Rule 7")
    check("Golden Rule 7" in code, "and the code says which rule")
    restore = ask([intent("RST-011", "replace", "Brain"),
                   intent("CRE-002", "create", "Company")])
    check(restore.get("refused") == "RESTORE_IS_NOT_A_STEP",
          "a restore beside anything else is refused, not ordered")
    for forbidden in ("open(", "makedirs", "shutil", "os.remove",
                      "os.listdir", "subprocess"):
        check(forbidden not in code, "the code never uses %s" % forbidden)

    print("\n4. docs/06 s142's gate is the document's, not invented here")
    platform = io.open(os.path.join(ROOT, "docs", "06-heron-platform.md"),
                       encoding="utf-8").read()
    flat = " ".join(platform.split())
    check("never touch the **data** class without explicit per-run "
          "confirmation" in flat,
          "docs/06 really says cleanup and repair never touch the data "
          "class unconfirmed")
    check("Cleanup Agent`" in flat and "Folder Repair Agent`" in flat,
          "and names those two agents")
    check(sorted(ARC.GATED) == ["HERON-WSP-CLN-009", "HERON-WSP-REP-004"],
          "which are exactly the two the code gates")
    for agent in ARC.GATED:
        one = intent(agent[-7:], "remove", "Fragments/old.json")
        check(ARC.review([one]).get("refused") == "NO_CONFIRMATION",
              "%s changing a DATA path unconfirmed is refused" % agent[-7:])
        reached.add("NO_CONFIRMATION")
        yes = ARC.review([one], confirmed=["Fragments/old.json"])
        check(not yes.get("refused") and len(yes["gated"]) == 1,
              "  and confirming THAT PATH lets it through, listed as gated")
        other = ARC.review([one], confirmed=["Fragments/something-else.json"])
        check(other.get("refused") == "NO_CONFIRMATION",
              "  while confirming a DIFFERENT path does not - yes to a run "
              "is not yes to a file")
    reading = ask([intent("CLN-009", "read", "Fragments/old.json")])
    check(not reading.get("refused"),
          "a cleanup that only READS needs no confirmation")
    derived = ask([intent("CLN-009", "remove", "Cache/index.bin")])
    check(not derived.get("refused"),
          "and the gate is the DATA class only - %s is derived and needs "
          "none" % "Cache/index.bin")
    check(PATHS.classify("Cache/index.bin")["class"] == PATHS.DERIVED,
          "which the path table agrees with")
    ungated = ask([intent("MIG-008", "replace", "Brain/global.db")])
    check(not ungated.get("refused"),
          "and a migration is not gated by this rule - docs/06 s142 names "
          "two agents, and MIG-008 has its own backup rule")

    print("\n5. The order is the declared one")
    shuffled = ask([intent("CLN-009", "move", "Logs/old"),
                    intent("PLC-005", "write", "Fragments/new.json"),
                    intent("BAK-010", "read", "Brain"),
                    intent("REG-012", "read", "Company"),
                    intent("CRE-002", "create", "Memory")],
                   confirmed=["Logs/old"])
    got = [one["agent"][-7:] for one in shuffled["order"]]
    check(got == ["REG-012", "BAK-010", "CRE-002", "PLC-005", "CLN-009"],
          "handed in backwards, it comes back in run order: %s"
          % " then ".join(got))
    check(ARC.RANK["HERON-WSP-BAK-010"] < ARC.RANK["HERON-WSP-MIG-008"],
          "a backup never lands after a migration - it would have copied "
          "the wrong thing")
    check(ARC.RANK["HERON-WSP-CLN-009"] == len(ARC.ORDER) - 1,
          "and a cleanup is last, never first")
    check(all(why.strip() for _agent, why in ARC.ORDER),
          "every place in the table says WHY it is there")
    twice = ask([intent("CRE-002", "create", "Memory"),
                 intent("CRE-002", "create", "Company")])
    check([one["path"] for one in twice["order"]] == ["Company", "Memory"],
          "two intents from one agent are ordered by path, so the answer "
          "is stable")

    print("\n6. Every failure is named and reached")
    for bad, why in ((None, "None is not a run"),
                     ([], "an empty run - it reports success and changes "
                          "nothing"),
                     (["not an intent"], "an intent that is not a map"),
                     ([intent("XXX-999", "read", "x")], "an agent with no "
                                                        "place in the table"),
                     ([intent("CRE-002", "ponder", "x")], "a word no plan "
                                                          "does"),
                     ([intent("CRE-002", "create", "  ")], "an intent "
                                                           "naming no path")):
        check(ask(bad).get("refused") == "NOT_A_PLAN", why)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-WSP-ARC-001.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 5, "the contract declares 5 failures")
    for failure in named:
        check(failure in code, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    spare = sorted(reached - set(named))
    check(not spare,
          "and the code refuses nothing the contract omits%s"
          % ("" if not spare else ": %s" % ", ".join(spare)))
    check(len(good["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    it orders plans and refuses runs, and runs nothing")
    return 0


if __name__ == "__main__":
    sys.exit(main())
