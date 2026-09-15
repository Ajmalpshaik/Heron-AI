# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-NAM-VAL-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Naming validation - it holds no rule of its own.

    python tests/test_naming.py

WHAT IT PROVES
  1. IT KEEPS NO SECOND COPY. Every pattern it uses is heron_fragment's
     object, compared by identity, and the code holds no literal of its
     own for a fragment id, an area or a capability.

  2. THE DERIVATION IS CHECKED AGAINST ALL 360 FRAGMENTS ON DISK, and
     the agent's answer agrees with every one of them. A rule proved on
     one example is a worked example.

  3. EVERY AGENT ID IN docs/28 PASSES, read out of the register.

  4. THE TWO OBSERVED RULES ARE REPORTED AS OBSERVED, with the count
     that is the only evidence for them - not as rules somebody wrote.

  5. THE ONE NAME IT CANNOT CHECK IS REFUSED, NOT GUESSED, and the two
     documents really do disagree about how many parts that name has.

  6. WHAT IS ENFORCED BY SHAPE HAS NO SECOND CHECK - a version or a kind
     genuinely cannot get into a fragment id.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND
     REACHED, and the code refuses nothing the contract omits.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_naming as NAM                                     # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_naming.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]

    def ask(kind, name, of=None):
        answer = NAM.check(kind, name, of=of)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. It keeps no second copy of any rule")
    # BY IDENTITY, not by value - two equal patterns are still two things
    # to drift.
    check(NAM.FRAG.ID_PATTERN is FRAG.ID_PATTERN,
          "the fragment id pattern IS heron_fragment's object")
    check(NAM.FRAG.CAPABILITY_PATTERN is FRAG.CAPABILITY_PATTERN,
          "so is the capability pattern")
    check(NAM.FRAG.AREAS is FRAG.AREAS,
          "and the area list")
    # It names FRG-<AREA>-<NNN> in a REFUSAL MESSAGE, which is a sentence
    # and not a rule. What it must not hold is a second pattern - so count
    # the patterns instead of searching for words.
    compiled = code.count("re.compile(")
    check(compiled == 3,
          "it compiles exactly 3 patterns of its own (%d), and all three "
          "are for rules heron_fragment does not own" % compiled)
    for mine in ("AGENT_SHAPE", "MODULE_SHAPE", "SUITE_SHAPE"):
        check(("%s = re.compile" % mine) in code,
              "  %s - %s" % (mine, "the register's house style"
                             if mine == "AGENT_SHAPE" else "observed, "
                             "stated nowhere"))
    for borrowed in ("[A-Z]{2,5}", '"SEL"', "'SEL'", "[0-9]{3}"):
        check(borrowed not in code,
              "and no literal %s - that rule is heron_fragment's"
              % borrowed)
    check("FRAG.ID_PATTERN" in code and "FRAG.AREAS" in code
          and "FRAG.CAPABILITY_PATTERN" in code,
          "it reaches for all three through heron_fragment")
    check("registry_agents" in code,
          "and reads docs/28 through heron_fragment.registry_agents()")

    print("\n2. The derivation, against all 360 fragments on disk")
    folder = os.path.join(ROOT, "brain", "fragments")
    seen, agreed, capless = 0, 0, 0
    for entry in sorted(os.listdir(folder)):
        card = os.path.join(folder, entry, "fragment.yaml")
        if not os.path.isfile(card):
            continue
        capability = None
        for line in io.open(card, encoding="utf-8"):
            if line.strip().startswith("capability:"):
                capability = line.split(":", 1)[1].strip().strip('"').strip("'")
                break
        if capability is None:
            capless += 1
            continue
        seen += 1
        answer = NAM.check("fragment-folder", entry, of=capability)
        if answer.get("ok"):
            agreed += 1
        elif seen - agreed <= 3:
            print("        %s <- %s" % (entry, capability))
    check(seen >= 350, "%d fragments on disk carry a capability" % seen)
    check(capless == 0, "every one of them declares it (%d did not)" % capless)
    check(agreed == seen,
          "and the agent agrees with all %d - the folder really is derived"
          % seen)
    check(NAM.folder_for("FILTER_ELEMENTS_BY_CATEGORY")
          == "filter-elements-by-category",
          "docs/29 s130's own worked example gives the same answer")

    print("\n3. Every agent id in docs/28 passes")
    known = FRAG.registry_agents()
    check(len(known) == 250, "the register declares %d agents" % len(known))
    wrong = [aid for aid in sorted(known)
             if not NAM.check("agent-id", aid).get("ok")]
    check(not wrong,
          "and every one of them passes%s"
          % ("" if not wrong else ": %s" % ", ".join(wrong[:4])))
    ghost = ask("agent-id", "HERON-NAM-ZZZ-999")
    check(ghost.get("refused") == "NOT_IN_THE_REGISTER",
          "a well-formed id nobody assigned is refused - it is the one that "
          "reads as correct in a review")
    check(ask("agent-id", "heron-naming").get("refused") == "WRONG_SHAPE",
          "and a wrongly shaped one is refused earlier, for a different "
          "reason")

    print("\n4. The observed rules are reported as observed")
    module = ask("module", "heron_naming.py")
    check(module["checked"] == "observed, not stated",
          "a module says in its answer that the rule is observed")
    got, every = module["observed"]
    check(got == every and every > 40,
          "and gives the evidence: %d of the %d files in brain/ follow it"
          % (got, every))
    check("nothing enforces" in module["why"].lower()
          or "nothing states" in module["why"].lower(),
          "the words say nothing states it and nothing enforces it")
    suite = ask("suite", "test_naming.py")
    check(suite["observed"][0] == suite["observed"][1],
          "every suite in tests/ follows its own, %d of %d"
          % suite["observed"])
    odd = ask("module", "naming.py")
    check(odd.get("refused") == "UNLIKE_EVERY_OTHER",
          "and a name that breaks it is UNLIKE_EVERY_OTHER - not "
          "WRONG_SHAPE, because those are different claims")
    check("weaker" in odd["why"],
          "the refusal says so itself: it is the weaker claim, reported as "
          "the weaker one")

    print("\n5. The one name it cannot check is refused, not guessed")
    guessed = ask("generated-name", "mep-duct-sizing-revit-2026-v1")
    check(guessed.get("refused") == "UNSTATED_CONVENTION",
          "a generated name is refused")
    check("F15" in guessed["why"], "and points at PROPOSALS F15")
    # THE DISAGREEMENT, read out of both documents rather than described.
    register = io.open(os.path.join(ROOT, "docs", "28-agent-registry.md"),
                       encoding="utf-8").read()
    baseline = io.open(os.path.join(ROOT, "docs",
                                    "00c-master-handover-baseline.md"),
                       encoding="utf-8").read()
    five = [line for line in register.splitlines() if "NAM-GEN-001" in line]
    check(len(five) == 1 and "domain, capability, purpose, platform, version"
          in five[0],
          "docs/28 names five parts")
    check("component type" in baseline and "component type" not in five[0],
          "and docs/00c adds a sixth, component type, which docs/28 omits")
    check("predictable and searchable" in io.open(
        os.path.join(ROOT, "docs", "06-heron-platform.md"),
        encoding="utf-8").read(),
        "while all the specification gives for the shape is that naming "
        "must be predictable and searchable - a goal, not a convention")

    print("\n6. What is enforced by shape has no second check")
    # The rule docs/29 s127 states, and the reason it needs no code.
    for carrying in ("FRG-SEL-001-V2", "FRG-FILTER-001", "FRG-SEL-FILTER-001",
                     "FRG-SEL-1", "FRG-SEL-0011"):
        check(ask("fragment-id", carrying).get("refused") == "WRONG_SHAPE",
              "'%s' cannot be a fragment id" % carrying)
    # Asserted on the message the agent ACTUALLY produces, not on the
    # source: adjacent string literals are joined by quotes there, so a
    # search of the file finds a sentence the reader never sees.
    said = ask("fragment-id", "FRG-SEL-001-V2")["why"]
    check("never the name, never the kind, never the version" in said,
          "the refusal quotes docs/29 s127 to the reader")
    check("cannot express a version cannot carry one" in said,
          "and says plainly why no separate check is needed")
    standard = io.open(os.path.join(ROOT, "docs", "29-metadata-standard.md"),
                       encoding="utf-8").read()
    check("never the name, never the kind, never the version" in standard,
          "which is docs/29 s127's own wording")

    print("\n7. Every failure is named and reached")
    check(ask("fragment-id", "FRG-MECH-001").get("refused")
          == "NOT_A_KNOWN_AREA", "an unlisted area is an error, not a guess")
    check(ask("capability", "filter elements").get("refused") == "WRONG_SHAPE",
          "a capability that is not SCREAMING_SNAKE_CASE is refused")
    check(ask("fragment-folder", "anything").get("refused")
          == "NOTHING_TO_DERIVE_FROM",
          "a folder with no capability cannot be judged at all")
    check(ask("fragment-folder", "category-filter",
              of="FILTER_ELEMENTS_BY_CATEGORY").get("refused")
          == "FOLDER_IS_NOT_DERIVED",
          "and one that is not the derivation is refused")
    check(ask("colour", "blue").get("refused") == "NOT_A_KIND_OF_NAME",
          "a kind nobody declared is refused")
    for blank, why in ((None, "nothing named"), ("", "an empty name"),
                       ("  heron_x.py  ", "a name with space around it")):
        check(ask("module", blank).get("refused") == "NO_NAME", why)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-NAM-VAL-002.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 9, "the contract declares 9 failures")
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
    check("os.rename" not in code and "shutil" not in code,
          "and it renames nothing - REN-003 does that, after identity")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    it holds no rule of its own")
    return 0


if __name__ == "__main__":
    sys.exit(main())
