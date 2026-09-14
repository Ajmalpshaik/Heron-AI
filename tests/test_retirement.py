# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-RET-010
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Retirement - nothing is deleted, and nothing leaves while something needs it.

    python tests/test_retirement.py

WHAT IT PROVES
  1. THE MODULE DELETES NOTHING. Asserted against the source text, not against
     behaviour: the day somebody adds an unlink, a behaviour test would still
     pass on every case anybody thought to write.

  2. A RETIREMENT WITH NO REASON IS REFUSED. It is a gap in the history the
     act claims to preserve.

  3. NOBODY MAY RETIRE AN AGENT WITHOUT SIGNING, AND NO MACHINE MAY SIGN -
     the same rule as deployment, for the same reason.

  4. AN AGENT SOMETHING STILL POINTS AT DOES NOT LEAVE WITHOUT A SUCCESSOR,
     and the refusal names the files that point at it.

  5. THE REGISTER IS NOT A REFERENCE. It keeps retired agents - that is what a
     register is - and counting its row would make every retirement
     impossible.

  6. ROLLBACK IS CHECKED, NOT HOPED FOR. A record too thin to bring the agent
     back is refused: an archive that cannot be reversed is a deletion with
     better manners.

  7. WHAT IS KEPT IS ENOUGH TO REBUILD THE ENTRY - name, role, department,
     tier, files, contract and version, and the stage it was in.

  8. ONLY THE TWO RETIREMENT STAGES ARE ACCEPTED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_retirement as RET                                # noqa: E402
import heron_agents as REG                                    # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    agent = "HERON-KRN-EVT-004"
    found = REG.record(agent)

    def ask(**kw):
        kw.setdefault("record_of", found)
        kw.setdefault("to_stage", "DEPRECATED")
        return RET.retire(agent, kw.pop("to_stage"), **kw)

    print("1. Nothing is deleted, and the source says so")
    source = io.open(os.path.join(ROOT, "brain", "heron_retirement.py"),
                     encoding="utf-8").read()
    # CALL SHAPES, NOT WORDS. The first version of this check searched for
    # "rmtree" and failed on the module's own docstring, which lists the calls
    # it promises not to make - the same trap that made a scaffolded stub
    # claim an agent id. A rule about what the code DOES has to look for a
    # call, and prose about the rule has to be allowed to name it.
    for call in ("os.remove(", "os.unlink(", "shutil.rmtree(", "rmtree(",
                 "os.rmdir(", "makedirs("):
        check(call not in source,
              "the module makes no %s call" % call.rstrip("("))
    for write in ('"w"', "'w'", '"a"', "'a'"):
        check(write not in source,
              "and opens nothing for writing (%s)" % write)

    print()
    print("2, 3. A reason, and a person")
    check(ask(reason="", approved_by="the owner")["refused"]
          == "NO_REASON_GIVEN", "no reason is refused")
    check(ask(reason="superseded")["refused"] == "NEEDS_HUMAN_APPROVAL",
          "nobody signing is refused")
    for machine in ("HERON-AHR-CRT-006", "heron-ahr-bld-004"):
        answer = ask(reason="superseded", approved_by=machine)
        check(answer["refused"] == "MACHINE_MAY_NOT_SIGN",
              "'%s' may not sign a retirement" % machine)
    check("Golden Rule 7" in ask(reason="x",
                                 approved_by="HERON-X")["why"],
          "and the rule is cited rather than just applied")

    print()
    print("4 and 5. What still points at it")
    answer = ask(reason="superseded", approved_by="the owner")
    check(answer["refused"] == "STILL_REFERENCED",
          "an agent something still names does not leave")
    check(answer["references"] and all(
        not f.startswith("docs/") for f in answer["references"]),
        "the references are files, and the register is not among them")
    check(any("test" in f for f in answer["references"]),
          "the files naming it are listed by name")
    check("Name a successor" in answer["why"],
          "and the refusal says what would let it through")

    answer = ask(reason="superseded", approved_by="the owner",
                 successor="HERON-KRN-WFL-007")
    check(answer["retired"] and answer["record"]["successor"]
          == "HERON-KRN-WFL-007",
          "with a successor named, it retires and the record keeps it")

    quiet = REG.record("HERON-AHR-GAP-001")
    check(RET.references("HERON-AHR-GAP-001", quiet["files"]) is not None,
          "references() answers for an agent with few of them too")

    print()
    print("5b. A successor is an agent, and the record is this agent's")
    answer = ask(reason="superseded", approved_by="the owner",
                 successor="NOT-AN-AGENT")
    check(answer["refused"] == "STILL_REFERENCED"
          and "typo holding a gate open" in answer["why"],
          "a successor that is not in the register does not open the gate")
    answer = ask(reason="superseded", approved_by="the owner",
                 successor=agent)
    check(answer["refused"] == "STILL_REFERENCED",
          "and an agent cannot succeed itself")
    other = REG.record("HERON-AHR-GAP-001")
    answer = RET.retire(agent, "ARCHIVED", reason="x",
                        approved_by="the owner",
                        successor="HERON-KRN-WFL-007", record_of=other)
    check(answer["refused"] == "NOTHING_TO_RETIRE"
          and "cannot roll either back" in answer["why"],
          "a record belonging to a different agent is refused")

    print()
    print("6. Rollback is checked")
    for thin in ({"id": agent}, {"id": agent, "name": "Event Bus"},
                 {"id": agent, "name": "Event Bus", "files": ["x.py"]}):
        answer = RET.retire(agent, "ARCHIVED", reason="x",
                            approved_by="the owner",
                            successor="HERON-KRN-WFL-007", record_of=thin)
        check(answer["refused"] == "NOTHING_TO_RETIRE",
              "a record missing %s is refused"
              % ", ".join(sorted(set(("id", "name", "department", "files"))
                                 - set(thin))))
    check(RET.retire(agent, "ARCHIVED", reason="x", approved_by="the owner",
                     record_of=None)["refused"] == "NOTHING_TO_RETIRE",
          "and no record at all is refused")

    print()
    print("7. What is kept is enough to bring it back")
    kept = ask(reason="superseded by the workflow engine",
               approved_by="the owner",
               successor="HERON-KRN-WFL-007")["record"]
    for field in ("name", "role", "department", "tier", "files", "contract",
                  "contract_version", "state_when_retired"):
        check(kept["kept"].get(field) not in (None, "", []),
              "the record keeps %s" % field)
    check(kept["deleted"] is False, "and says plainly that nothing was deleted")
    check(kept["reason"] == "superseded by the workflow engine",
          "the reason is kept in the record, not only in the log line")

    print()
    print("8. Only the two retirement stages")
    for stage in ("PRODUCTION", "RETIRED", "GONE", ""):
        check(RET.retire(agent, stage, reason="x", approved_by="the owner",
                         record_of=found)["refused"] == "UNKNOWN_STAGE",
              "'%s' is not a retirement stage" % stage)
    for stage in RET.RETIRED_STAGES:
        answer = RET.retire(agent, stage, reason="x", approved_by="the owner",
                            successor="HERON-KRN-WFL-007", record_of=found)
        check(answer["retired"], "%s is" % stage)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    archived, never deleted, and never while something needs it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
