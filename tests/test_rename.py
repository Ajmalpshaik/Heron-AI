# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-NAM-REN-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Auto rename - identity first, then the name, then every reference.

    python tests/test_rename.py

WHAT IT PROVES
  1. THE RULE IS docs/06 s136's, read out of that document rather than
     trusted: identity must exist BEFORE anything is renamed
     automatically.

  2. AND THE ORDER IS REAL. A file that has NO identity AND a wrong new
     name is refused for the identity, not for the name. If the checks
     were the other way round the answer would differ, so this is the
     order proved rather than described.

  3. IT HOLDS NO CONVENTION OF ITS OWN - the new name goes to
     HERON-NAM-VAL-002, and its refusal is carried through by name.

  4. A RENAME IS NOT A MOVE, and the refusal says whose job that is.

  5. A REFERENCE BLOCKS IT unless it is part of the same change - no
     broken references, docs/28's own words for HERON-NAM-REF-007.

  6. THE COLLISION ONLY THE BATCH CAN SEE.

  7. NOTHING IS RENAMED AND NOTHING IS OPENED.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND
     REACHED, and the code refuses nothing the contract omits.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_rename as REN                                     # noqa: E402
import heron_naming as NAMING                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_rename.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]

    def ask(renames, **kw):
        answer = REN.plan(renames, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        for entry in (answer.get("refused_names") or []):
            reached.add(entry["refused"])
        return answer

    def only(answer, was):
        for entry in (answer.get("refused_names") or []):
            if entry.get("from") == was:
                return entry
        return {}

    IDENTIFIED = "# Heron-Agent:  HERON-WSP-PTH-007\n"
    ANONYMOUS = "# just a helper\n"

    print("1. The rule is docs/06 s136's, read out of the document")
    platform = io.open(os.path.join(ROOT, "docs", "06-heron-platform.md"),
                       encoding="utf-8").read()
    flat = " ".join(platform.split())
    check("identity must exist *before* anything is allowed to rename "
          "automatically" in flat,
          "docs/06 says identity must exist BEFORE anything is renamed")
    check("Renaming files that are identified by name is how knowledge "
          "bases lose track of themselves" in flat,
          "and says what goes wrong when it does not")
    check("identity is an ID, never a filename" in flat,
          "because identity is an ID, never a filename")

    print("\n2. And the order is real, not described")
    # THE SAME FILE, TWICE: no identity and a name that is also wrong.
    both_wrong = ask([{"from": "brain/helper.py", "to": "NOT A NAME AT ALL",
                       "kind": "module"}],
                     read=lambda path: ANONYMOUS)
    check(only(both_wrong, "brain/helper.py").get("refused")
          == "NO_IDENTITY_YET",
          "a file with NO identity and a wrong new name is refused for the "
          "IDENTITY")
    # And the same wrong name on a file that HAS one gets the other answer,
    # so the first result was the order and not the only possible answer.
    named_wrong = ask([{"from": "brain/heron_paths.py",
                        "to": "NOT A NAME AT ALL", "kind": "module"}],
                      read=lambda path: IDENTIFIED)
    check(only(named_wrong, "brain/heron_paths.py").get("refused")
          == "THE_NEW_NAME_IS_WRONG",
          "while the SAME wrong name on an identified file is refused for "
          "the name - so the first answer was the order")
    check("identity" in code.split("NO_IDENTITY_YET")[0].split(
        "THE_NEW_NAME_IS_WRONG")[0],
        "and the identity check really does come first in the code")

    print("\n3. It holds no convention of its own")
    check("NAMING.check" in code, "the new name goes to HERON-NAM-VAL-002")
    for invented in ("re.compile", "SCREAMING", "FRG-", "heron_[a-z]"):
        check(invented not in code,
              "it compiles no pattern and holds no literal %s" % invented)
    carried = only(named_wrong, "brain/heron_paths.py")
    direct = NAMING.check("module", "NOT A NAME AT ALL").get("refused")
    check(carried.get("by") == direct,
          "and VAL-002's own refusal is carried through by name, not "
          "flattened: %s" % carried.get("by"))
    check(direct == "UNLIKE_EVERY_OTHER",
          "which for a MODULE is UNLIKE_EVERY_OTHER, because that rule is "
          "observed rather than written - the distinction survives the "
          "carry-through")
    # A kind whose rule IS written gives the stronger refusal, so the two
    # are not the same answer wearing two names.
    check(NAMING.check("capability", "not a capability").get("refused")
          == "WRONG_SHAPE",
          "while a capability, whose rule docs/29 s128 states, gives "
          "WRONG_SHAPE")
    unknown = ask([{"from": "x", "to": "y", "kind": "colour",
                    "identity": "HERON-X"}])
    check(only(unknown, "x").get("refused") == "NOT_A_RENAME",
          "and a kind VAL-002 does not own is refused rather than added")

    print("\n4. A rename is not a move")
    for place in ("brain/heron_x.py", "..\\heron_x.py", "sub/heron_x.py"):
        moved = ask([{"from": "brain/heron_flags.py", "to": place,
                      "kind": "module", "identity": "HERON-OPS-FLG-009"}])
        check(only(moved, "brain/heron_flags.py").get("refused")
              == "A_RENAME_IS_NOT_A_MOVE",
              "'%s' names a place, not a thing" % place)
    check("HERON-WSP-PLC-005" in code and "HERON-WSP-REP-004" in code,
          "and the refusal names whose job a move is")

    print("\n5. A reference blocks it unless it is part of the change")
    pointed = {"brain/heron_update.py": ["brain/heron_migration.py",
                                         "tests/test_update.py"]}
    blocked = ask([{"from": "brain/heron_update.py", "to": "heron_updates.py",
                    "kind": "module", "identity": "HERON-OPS-UPD-010"}],
                  referenced_by=pointed)
    entry = only(blocked, "brain/heron_update.py")
    check(entry.get("refused") == "REFERENCES_NOT_UPDATED",
          "two things name it, so the rename is refused")
    check(entry.get("referenced_by") == ["brain/heron_migration.py",
                                         "tests/test_update.py"],
          "and both are named back, so a caller knows what to fix")
    partly = ask([{"from": "brain/heron_update.py", "to": "heron_updates.py",
                   "kind": "module", "identity": "HERON-OPS-UPD-010"}],
                 referenced_by=pointed,
                 also_updating=["brain/heron_migration.py"])
    check(only(partly, "brain/heron_update.py").get("refused")
          == "REFERENCES_NOT_UPDATED",
          "updating ONE of the two is still refused - a window in the "
          "middle is a window")
    whole_change = ask([{"from": "brain/heron_update.py",
                         "to": "heron_updates.py", "kind": "module",
                         "identity": "HERON-OPS-UPD-010"}],
                       referenced_by=pointed,
                       also_updating=["brain/heron_migration.py",
                                      "tests/test_update.py"])
    check(len(whole_change["rename"]) == 1,
          "and with both in the same change it may proceed")
    check("HERON-NAM-REF-007" in code,
          "the refusal names the agent that will find them")
    register = io.open(os.path.join(ROOT, "docs", "28-agent-registry.md"),
                       encoding="utf-8").read()
    check("No broken references" in register,
          "and docs/28 really does say no broken references")

    print("\n6. The collision only the batch can see")
    each_fine = ask([{"from": "brain/a.py", "to": "heron_x.py",
                      "kind": "module", "identity": "HERON-A"}])
    check(len(each_fine["rename"]) == 1, "one of them alone is fine")
    together = ask([{"from": "brain/a.py", "to": "heron_x.py",
                     "kind": "module", "identity": "HERON-A"},
                    {"from": "brain/b.py", "to": "heron_x.py",
                     "kind": "module", "identity": "HERON-B"}])
    check(together.get("refused") == "TWO_RENAMES_COLLIDE",
          "and the two together are refused")
    check(together["colliding"] == {"heron_x.py": ["brain/a.py",
                                                   "brain/b.py"]},
          "with both sources named")
    check(not together.get("rename"),
          "and NOTHING in the batch proceeds - a batch that half-applies "
          "is worse than one that refuses")

    print("\n7. Nothing is renamed and nothing is opened")
    good = ask([{"from": "brain/heron_paths.py", "to": "heron_pathing.py",
                 "kind": "module"}], read=lambda path: IDENTIFIED)
    check(good["renamed"] is False, "`renamed` is false")
    check(len(good["rename"]) == 1 and good["rename"][0]["to"]
          == "heron_pathing.py", "the plan comes back instead")
    for forbidden in ("os.rename", "shutil", "open(", "os.listdir",
                      "os.walk", "os.remove"):
        check(forbidden not in code, "the code never uses %s" % forbidden)
    asked_for = []
    ask([{"from": "brain/heron_paths.py", "to": "heron_pathing.py",
          "kind": "module"}],
        read=lambda path: (asked_for.append(path), IDENTIFIED)[1])
    check(asked_for == ["brain/heron_paths.py"],
          "`read` is handed in, asked once, and its answer used")
    check(len(good["unjudged"]) == 4, "four things are left unjudged")
    check(any("IDENTITY WAS CHECKED FIRST" in line
              for line in good["unjudged"]),
          "including that identity was checked first, and why")

    print("\n8. Every failure is named and reached")
    for bad, why in ((None, "None is not a batch"),
                     ([], "an empty batch reports success and changes "
                          "nothing")):
        check(ask(bad).get("refused") == "NOTHING_TO_RENAME", why)
    check(ask([{"from": "a", "to": "b"}], read="not callable").get("refused")
          == "NOT_A_READER", "a reader that is not callable is refused")
    for entry, why in ((["not a map"], "an entry that is not a map"),
                       ([{"to": "heron_x.py", "kind": "module"}],
                        "a rename with nothing to rename"),
                       ([{"from": "brain/heron_x.py", "to": "heron_x.py",
                          "kind": "module", "identity": "H"}],
                        "and one that changes nothing")):
        refusals = [row["refused"] for row in
                    (ask(entry).get("refused_names") or [])]
        check(refusals == ["NOT_A_RENAME"], why)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-NAM-REN-003.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 8, "the contract declares 8 failures")
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

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    identity first, and the order is proved")
    return 0


if __name__ == "__main__":
    sys.exit(main())
