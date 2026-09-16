# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-MIG-008
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Workspace migration - the chain is continuous, or nothing runs.

    python tests/test_migration.py

WHAT IT PROVES
  1. A GAP REFUSES THE WHOLE RUN, not the part that works - and the gaps
     are named.

  2. THE CHAIN COMES BACK IN ORDER, every step from one version to the
     next, with no step skipped or repeated.

  3. UNVERSIONED DATA IS REFUSED, because the recorded version is the only
     thing that makes "idempotent" checkable.

  4. BACKWARDS IS A RESTORE, NOT A MIGRATION.

  5. THE THREE CONDITIONS ARE HERON-OPS-UPD-010's OWN LIST, imported
     rather than restated.

  6. THE BACKUP MUST BE IN THE DATA CLASS - a backup where an update
     replaces things wholesale is gone the next time one runs.

  7. ALREADY THERE IS A REAL ANSWER, not a refusal.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_migration as MIG                                  # noqa: E402
import heron_update as UPD                                     # noqa: E402
import heron_paths as PATHS                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def step(low, **more):
    found = {"from": low, "to": low + 1, "idempotent": True,
             "version": str(low + 1), "reversible": "backed up"}
    found.update(more)
    return found


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_migration.py"),
                  encoding="utf-8").read()

    def ask(**kw):
        settings = {"at": 1, "to": 4,
                    "migrations": [step(1), step(2), step(3)],
                    "backup": "Backup/pre-4"}
        settings.update(kw)
        answer = MIG.plan(**settings)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. A gap refuses the whole run")
    answer = ask(migrations=[step(1), step(3)])
    check(answer.get("refused") == "CHAIN_IS_BROKEN", "a missing 2-to-3 refuses")
    check(answer["missing_steps"] == ["2 to 3"], "naming the gap")
    check(not answer.get("chain"),
          "and the 1-to-2 that DOES exist is not returned - the whole run "
          "is refused, not the part that works")
    check("silent, and permanent" in answer["why"],
          "saying what stepping over it would cost")
    check("no migration can start from" in answer["proposal"],
          "and that a partial run strands the data")
    answer = ask(at=1, to=5, migrations=[step(2)])
    check(sorted(answer["missing_steps"]) == ["1 to 2", "3 to 4", "4 to 5"],
          "several gaps are all named, not just the first")

    print()
    print("2. The chain comes back in order")
    answer = ask()
    check(len(answer["chain"]) == 3, "three steps for 1 to 4")
    check([(e["from"], e["to"]) for e in answer["chain"]]
          == [(1, 2), (2, 3), (3, 4)], "in order, each to the next")
    check(answer["at"] == 1 and answer["to"] == 4, "with where and where to")
    check(answer["migrated"] is False, "and nothing ran")
    # ORDER IS NOT THE INPUT ORDER.
    answer = ask(migrations=[step(3), step(1), step(2)])
    check([(e["from"], e["to"]) for e in answer["chain"]]
          == [(1, 2), (2, 3), (3, 4)],
          "shuffled input still comes back in version order - the ordering "
          "is derived, not trusted")
    check(len(ask(at=1, to=2)["chain"]) == 1, "one step for one version")

    print()
    print("3. Unversioned data is refused")
    for at in (None, "", "two", -1, {}, "1.5"):
        check(ask(at=at).get("refused") == "DATA_VERSION_UNKNOWN",
              "at=%r is not a recorded version" % (at,))
    answer = ask(at=None)
    check("not because the number is interesting" in answer["why"],
          "and it says why the number matters")
    check("has this already run" in answer["why"],
          "naming the question only a recorded version can answer")
    check("idempotent was supposed to cover" in answer["proposal"],
          "and what running twice on untellable data is")
    check(ask(at=0, to=1, migrations=[step(0)],
              backup="Backup/x")["chain"],
          "while schema 0 is a real version, not a missing one")

    print()
    print("4. Backwards is a restore, not a migration")
    answer = ask(at=4, to=1)
    check(answer.get("refused") == "WOULD_GO_BACKWARDS", "4 to 1 is refused")
    check("threw information away" in answer["why"],
          "saying the forward step discarded something")
    check("invents what it threw" in answer["why"],
          "and that reversing it would invent it back")
    check("rule 4" in answer["proposal"],
          "pointing at the backup rule instead")

    print()
    print("5. The three conditions are the update agent's own list")
    check(MIG.A_MIGRATION_DECLARES is UPD.A_MIGRATION_DECLARES,
          "imported, the same object - not a copy")
    check("from heron_update import" in source,
          "by import, so it cannot drift")
    for field, _ in UPD.A_MIGRATION_DECLARES:
        thin = step(1)
        thin.pop(field)
        answer = ask(to=2, migrations=[thin])
        check(answer.get("refused") == "MIGRATION_NOT_DECLARED",
              "a migration missing '%s' is refused" % field)
        check(answer["missing"][0]["migration"] == 1,
              "  naming which migration")
    for bad, label in ((dict(step(1), to=3), "skips a version"),
                       (dict(step(1), to=1), "goes nowhere"),
                       ({"from": 1, "to": 2}, "declares nothing"),
                       ("not a record", "is not a record")):
        check(ask(to=2, migrations=[bad]).get("refused")
              == "MIGRATION_NOT_DECLARED", "a migration that %s" % label)
    check("is not a step" in str(ask(to=2,
                                     migrations=[dict(step(1), to=3)])
                                 ["missing"]),
          "and a skip says it is not a step")

    print()
    print("6. The backup must be in the data class")
    for where in ("", None, "   "):
        check(ask(backup=where).get("refused") == "NO_BACKUP",
              "backup=%r is no backup" % (where,))
    check("the half nothing regenerates" in ask(backup="")["why"],
          "and it says what the migration would run over")
    for where, klass in (("Core/pre-4", "product"), ("Cache/pre-4", "derived"),
                         ("who/knows", "unknown")):
        answer = ask(backup=where)
        check(answer.get("refused") == "NO_BACKUP",
              "a backup at %s (%s) is refused" % (where, klass))
        check(klass in answer["why"], "  naming the class it is in")
    check("gone the next time one runs" in ask(backup="Core/x")["why"],
          "and what a backup in the product class costs")
    check(PATHS.classify("Backup/pre-4")["class"] == PATHS.DATA,
          "while Backup/ really is the data class...")
    check(ask(backup="Backup/pre-4")["chain"], "...and is accepted")
    check(any("class, not for contents" in note.lower()
              for note in ask()["unjudged"]),
          "and the answer is honest that only the class was checked")

    print()
    print("7. Already there is a real answer")
    answer = MIG.plan(at=4, to=4, migrations=[step(1)], backup="Backup/x")
    check(answer.get("refused") is None, "4 to 4 is not a refusal")
    check(answer["chain"] == [], "with an empty chain")
    check("that is a real answer" in answer["why"],
          "and the answer says so")
    check("unlike an empty request" in answer["why"],
          "distinguishing it from a call that asked for nothing")
    check(MIG.plan(at=4, to=4)["chain"] == [],
          "and it needs no migrations to say it")

    print()
    print("8. Every failure the contract declares is named and reached")
    for to in (None, "", "four", -2):
        check(ask(to=to).get("refused") in ("NO_TARGET",
                                            "WOULD_GO_BACKWARDS"),
              "to=%r has no usable target" % (to,))
    check(ask(to=None).get("refused") == "NO_TARGET",
          "and nothing at all is NO_TARGET specifically")
    check("nobody can check afterwards" in ask(to=None)["why"],
          "saying what a migration with no destination is")
    for word in ("os.rename", "shutil", "os.remove", "open(", "subprocess",
                 "sqlite3", "os.makedirs"):
        check(word not in source, "the source has no %s" % word)
    check(any("NOTHING RAN" in note for note in ask()["unjudged"]),
          "and the answer says nothing ran")
    check(any("DECLARED, NOT DEMONSTRATED" in note
              for note in ask()["unjudged"]),
          "and that idempotent is a claim it did not test")
    print()
    print("R. THE SECOND CODEX REVIEW - two migrations out of one version")
    declares = {"idempotent": True, "reversible": True, "version": 2}
    answer = ask(at=1, to=2, backup="Backup/x",
                 migrations=[dict(declares, **{"from": 1, "to": 2,
                                               "name": "first"}),
                             dict(declares, **{"from": 1, "to": 2,
                                               "name": "second"})])
    check(answer.get("refused") == "CHAIN_FORKS",
          "two migrations starting at one schema version is refused - the "
          "later one used to replace the earlier in the chain dict and "
          "the run still reported complete, so a transformation was "
          "dropped and nothing said which")
    check(answer["forks"][0]["from"] == 1,
          "  and the fork names the version both start at")

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-WSP-MIG-008.yaml"))
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
    print("PASS    the chain is continuous, or nothing runs")
    return 0


if __name__ == "__main__":
    sys.exit(main())
