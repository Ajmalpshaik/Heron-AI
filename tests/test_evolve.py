# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-EVO-005
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Evolution - it routes, and the order is Golden Rule 4's.

    python tests/test_evolve.py

WHAT IT PROVES
  1. THE EIGHT VERDICTS ARE docs/28's, and every one names an owner that
     is a real agent in this repository or a document section.

  2. FOUR ARE ON GOLDEN RULE 4's LADDER IN ITS ORDER, read out of
     docs/14, and four are reported beside it.

  3. SPLIT AND MERGE ARE RULED OUT UNLESS THEIR OWNERS PROPOSED ONE -
     ruled out and named, never quietly missing.

  4. ARCHIVE IS REACHABLE ONLY FROM DEPRECATED, and DEPRECATE only when
     it is not already - docs/09 s98's transitions.

  5. IT DECIDES NOTHING. `decided` is false on every path, and the
     module holds no rule of its own to decide with.

  6. A REASON IS REQUIRED, and a verdict nobody named is refused.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_evolve as EVO                                     # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

WHY = "it failed twice on Revit 2021 and nobody has run it there since"


def frag(**changes):
    card = {"id": "count-elements", "heron-status": "PROVEN"}
    card.update(changes)
    return card


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_evolve.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]
    register = io.open(os.path.join(ROOT, "docs",
                                    "28-agent-registry.md"),
                       encoding="utf-8").read()
    rules = io.open(os.path.join(ROOT, "docs", "14-golden-rules.md"),
                    encoding="utf-8").read()

    print("\n1. the eight verdicts are docs/28's")
    check(len(EVO.VERDICTS) == 8, "there are eight")
    check(" / ".join(EVO.VERDICTS) in register,
          "and docs/28 carries them in exactly that order: %s"
          % " / ".join(EVO.VERDICTS))
    # THE OWNER MUST BE A REAL AGENT IN THE REGISTER, not one that
    # happens to be built. An unbuilt agent still owns its verdict.
    unbuilt = []
    for verdict, (owner, _) in sorted(EVO.OWNS.items()):
        if owner.startswith("HERON-"):
            check("`%s`" % owner in register,
                  "%s is owned by %s, which docs/28 lists"
                  % (verdict, owner))
            if not os.path.exists(os.path.join(ROOT, "brain", "agents",
                                               "%s.yaml" % owner)):
                unbuilt.append(owner)
        else:
            check(owner in ("docs/09 s98", "this fragment's owner"),
                  "%s is owned by %s" % (verdict, owner))
    print("       owners not yet built: %s"
          % (", ".join(sorted(set(unbuilt))) or "none"))
    check(sorted(EVO.OWNS) == sorted(EVO.VERDICTS),
          "every verdict has an owner and no owner has a spare verdict")

    print("\n2. four are on Golden Rule 4's ladder, in its order")
    check("Keep, extend, adapt, or version-branch — in that order of "
          "preference" in rules,
          "docs/14 carries Golden Rule 4's sentence verbatim")
    check([theirs for _, theirs in EVO.LADDER]
          == ["keep", "extend", "adapt", "version-branch"],
          "and the module's ladder is that order, in those words")
    check(EVO.ON_THE_LADDER == ("KEEP", "EXTEND", "UPDATE", "BRANCH"),
          "mapped onto KEEP, EXTEND, UPDATE, BRANCH")
    check(sorted(EVO.BESIDE_IT)
          == ["ARCHIVE", "DEPRECATE", "MERGE", "SPLIT"],
          "and the other four sit beside it: %s"
          % ", ".join(sorted(EVO.BESIDE_IT)))
    good = EVO.consider(frag(), WHY, {"SPLIT": "two parts proposed"})
    check(good["order"] == ["KEEP", "EXTEND", "UPDATE", "BRANCH"],
          "an answer orders them keep, extend, adapt, version-branch")
    check(good["order"].index("BRANCH") == len(good["order"]) - 1,
          "with the last resort LAST, which is the whole point of an "
          "order")
    check(any("how the last resort gets picked first" in line
              for line in good["unjudged"]),
          "and the answer says why the mapping is stated out loud")

    print("\n3. split and merge are ruled out unless proposed")
    bare = EVO.consider(frag(), WHY)
    out = dict((one["verdict"], one) for one in bare["ruled_out"])
    check("SPLIT" in out and "MERGE" in out,
          "with no findings, both are ruled out")
    check(out["SPLIT"]["owner"] == "HERON-FRG-SPL-003"
          and out["MERGE"]["owner"] == "HERON-FRG-MRG-004",
          "each naming the agent that did not propose one")
    check("menu rather than a proposal" in out["SPLIT"]["why"],
          "with the reason: offering it anyway would be a menu")
    check("SPLIT" not in [one["verdict"] for one in bare["available"]],
          "and it is not also available")
    told = EVO.consider(frag(), WHY, {"SPLIT": "two parts proposed"})
    split = [one for one in told["available"]
             if one["verdict"] == "SPLIT"][0]
    check(split["supported_by"] == "two parts proposed",
          "proposed, it becomes available AND carries what supports it")
    check(split["on_the_ladder"] is False,
          "and is marked as not on Golden Rule 4's ladder")

    print("\n4. ARCHIVE only from DEPRECATED")
    check("ARCHIVE" in out,
          "from PROVEN, ARCHIVE is ruled out")
    check("only from DEPRECATED" in out["ARCHIVE"]["why"],
          "citing docs/09 s98's transition")
    gone = EVO.consider(frag(**{"heron-status": "DEPRECATED"}), WHY)
    verdicts = [one["verdict"] for one in gone["available"]]
    check("ARCHIVE" in verdicts,
          "from DEPRECATED it becomes available")
    check("DEPRECATE" not in verdicts,
          "and DEPRECATE is ruled out - it is already deprecated")
    older = EVO.consider(frag(**{"heron-status": "ARCHIVED"}), WHY)
    check("DEPRECATE" not in [one["verdict"]
                              for one in older["available"]],
          "the same from ARCHIVED")
    check(EVO.DO_ARCHIVE != EVO.ARCHIVED,
          "the VERDICT and the STATUS are different words for different "
          "things: %s against %s" % (EVO.DO_ARCHIVE, EVO.ARCHIVED))

    print("\n5. it decides nothing")
    check(good["decided"] is False,
          "`decided` is false on the good path, and it is always false")
    for verdict in EVO.VERDICTS:
        owner = EVO.OWNS[verdict][0]
        check(owner != "HERON-FRG-EVO-005",
              "%s is not this agent's to decide" % verdict)
    check(good["may_be_applied"] is True, "a PROVEN one may be applied to")
    live = EVO.consider(frag(**{"heron-status": "PRODUCTION"}), WHY)
    check(live["may_be_applied"] is False
          and "docs/09 s118" in live["unjudged"][3],
          "a PRODUCTION one may not, and the answer cites docs/09 s118")
    for writing in ("open(", "write(", "makedirs", "subprocess"):
        check(writing not in logic, "the agent never uses %s" % writing)

    print("\n6. a reason is required")
    for reason in (None, "", "   "):
        answer = EVO.consider(frag(), reason)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "NO_REASON",
              "%r is not a reason" % reason)
    silent = EVO.consider(frag(), None)
    check(silent.get("asked") and "count-elements" in silent["asked"],
          "and the answer asks what happened: %r" % silent["asked"])
    check("a menu rather than a proposal" in silent["why"],
          "because without one every answer is KEEP, probably")
    check(good["because"] == WHY, "a reason given is read back as given")

    print("\n7. every declared failure is named and reached")
    for fragment, reason, findings, name in (
            (None, WHY, {}, "NOTHING_TO_EVOLVE"),
            ("a string", WHY, {}, "NOT_A_FRAGMENT"),
            ({"id": "  "}, WHY, {}, "NOT_A_FRAGMENT"),
            (frag(), WHY, {"RETIRE": "somebody said so"}, "NOT_A_VERDICT"),
            (frag(), WHY, {"keep": "x", "BIN_IT": "y"}, "NOT_A_VERDICT")):
        answer = EVO.consider(fragment, reason, findings)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)
    check(EVO.consider(frag(), WHY, {"keep": "nothing needs doing"})
          ["considered"] is True,
          "though a lower-case verdict IS one - case is not a new word")

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-FRG-EVO-005.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 4, "the contract declares 4 failures")
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
    print("PASS    it routes, and the order is Golden Rule 4's")
    return 0


if __name__ == "__main__":
    sys.exit(main())
