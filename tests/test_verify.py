# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-VAL-012
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Import verification - no third validator, and unowned is not verified.

    python tests/test_verify.py

WHAT IT PROVES
  1. THE VALIDATORS ARE THE OWNERS' OWN FUNCTIONS, by identity - so no
     third implementation exists to disagree with them.

  2. IT WORKS AGAINST THE REAL LIBRARY. A real fragment card and a real
     skill card both come back complete, which is the only way to know
     the wiring is right rather than merely present.

  3. THE PROBLEMS ARE THE OWNER'S OWN WORDS - identical to calling that
     validator directly, not reworded.

  4. A KIND NOBODY VALIDATES LANDS IN `no_validator`, NEVER `ready`.

  5. NOTHING IS OPENED AND NOTHING IS SAVED, which is what makes
     "before it is saved" true.

  6. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_verify as VER                                     # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_skill as SKILL                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_verify.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. the validators are the owners' own functions")
    check(VER.OWNERS["fragment"][1] is FRAG.validate,
          "a fragment goes to heron_fragment.validate - the same object")
    check(VER.OWNERS["skill"][1] is SKILL.validate,
          "a skill goes to heron_skill.validate - the same object")
    check(VER.OWNERS["fragment"][0] == "HERON-FRG-VAL-001"
          and VER.OWNERS["skill"][0] == "HERON-SKL-VAL-004",
          "and each is named by the agent that owns it")
    check("def validate" not in logic,
          "this module defines no validate of its own")
    check(len(VER.OWNERS) == 2,
          "two kinds have an owner, and only two")

    print("\n2. it works against the real library")
    found, _ = FRAG.load_all()
    skills, _ = SKILL.load_all()
    real_fragment = list(found.values())[0]
    real_skill = list(skills.values())[0]
    good = VER.verify([
        {"name": real_fragment.slug, "kind": "fragment",
         "card": dict(real_fragment.data)},
        {"name": real_skill.id, "kind": "skill",
         "card": dict(real_skill.data)}])
    check(len(good["ready"]) == 2 and good["problems"] == [],
          "a real fragment card and a real skill card both come back "
          "complete")
    check(sorted(one["by"] for one in good["ready"])
          == ["HERON-FRG-VAL-001", "HERON-SKL-VAL-004"],
          "each checked by its own owner")
    check(good["of"] == 2, "two items were read")

    print("\n3. the problems are the owner's own words")
    half = {"id": "FRG-T-001"}
    mine = VER.verify([{"name": "half-migrated", "kind": "fragment",
                        "card": dict(half)}])
    theirs = FRAG.validate(FRAG.Fragment(
        dict(half), os.path.join(ROOT, "brain", "fragments",
                                 "half-migrated")))
    check(mine["problems"][0]["problems"] == theirs,
          "identical to calling heron_fragment.validate directly - %d "
          "problem(s), not reworded" % len(theirs))
    check(mine["problems"][0]["by"] == "HERON-FRG-VAL-001",
          "and attributed to it")
    check(mine["ready"] == [],
          "an item with problems is not also ready")
    thin_skill = VER.verify([{"name": "x", "kind": "skill",
                              "card": {"id": "x"}}])
    check(thin_skill["problems"][0]["problems"]
          == SKILL.validate(SKILL.Skill(
              {"id": "x"}, os.path.join(ROOT, "brain", "skills", "x.yaml"))),
          "and the same holds for a skill, through its own validator")

    print("\n4. a kind nobody validates is not verified")
    for kind in ("document", "asset", "config", "note"):
        answer = VER.verify([{"name": "x", "kind": kind,
                              "card": {"title": "y"}}])
        check([one["item"] for one in answer["no_validator"]] == ["x"],
              "a %r lands in no_validator" % kind)
        check(answer["ready"] == [] and answer["problems"] == [],
              "  and in neither ready nor problems")
    doc = VER.verify([{"name": "notes", "kind": "document",
                       "card": {"title": "y"}}])
    check("must not land beside the ones that were actually checked"
          in doc["no_validator"][0]["why"],
          "with why it is kept apart")
    check(any("HERON-IMP-DUP-007" in line for line in doc["unjudged"]),
          "naming the agent that refuses the same shape one step earlier")
    # AND MIXED IN WITH REAL ONES IT STILL SEPARATES.
    mixed = VER.verify([
        {"name": real_fragment.slug, "kind": "fragment",
         "card": dict(real_fragment.data)},
        {"name": "notes", "kind": "document", "card": {"title": "y"}}])
    check(len(mixed["ready"]) == 1 and len(mixed["no_validator"]) == 1,
          "one complete and one unowned stay in separate lists")

    print("\n5. nothing is opened and nothing is saved")
    check(good["saved"] is False,
          "`saved` is false, and it is always false")
    check("Nothing was saved" in good["why"], "and the answer says so")
    for writing in ("io.open", "write(", "makedirs", "listdir"):
        check(writing not in logic, "the agent never uses %s" % writing)
    check("lost the argument" in " ".join(good["unjudged"]),
          "and says why: a validator that runs after the write has "
          "already lost the argument")
    imports = sorted(line.split()[1] for line in logic.split("\n")
                     if line.startswith("import "))
    check(imports == ["heron_fragment", "heron_skill", "os", "sys"],
          "the whole import list is os, sys and the two owners: %s"
          % ", ".join(imports))

    print("\n6. every failure the contract declares is named and reached")
    for these, name in (
            ([], "NOTHING_TO_VERIFY"),
            (["a string"], "NOT_AN_ITEM"),
            ([{"name": "x"}], "NOT_AN_ITEM"),
            ([{"kind": "fragment", "card": {"id": "a"}}], "NOT_AN_ITEM"),
            ([{"name": "x", "kind": "fragment", "card": {"id": "a"}},
              {"name": "x", "kind": "skill", "card": {"id": "b"}}],
             "NOT_AN_ITEM"),
            ([{"name": "x", "kind": "fragment"}], "NO_CARD"),
            ([{"name": "x", "kind": "fragment", "card": {}}], "NO_CARD"),
            ([{"name": "x", "kind": "fragment", "card": "a string"}],
             "NO_CARD")):
        answer = VER.verify(these)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-IMP-VAL-012.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 3, "the contract declares 3 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(good["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    no third validator, and unowned is not verified")
    return 0


if __name__ == "__main__":
    sys.exit(main())
