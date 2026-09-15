# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-SKL-CRE-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Skill authoring - it enters at DRAFT, invents no utterance, and
overwrites nothing.

    python tests/test_authoring.py

WHAT IT PROVES
  1. WHAT IT WRITES PASSES HERON-SKL-VAL-004 CLEAN. The strongest single
     check there is: the validator is a different file with different
     rules, and it is satisfied.

  2. THE YAML IS DUMPED, NOT HAND-WRITTEN. `revit` reads back as STRINGS
     and a purpose carrying a colon survives - both of which a
     hand-written line gets wrong.

  3. IT ENTERS AT DRAFT AND THE AUTHOR GETS NO SAY. A draft arriving at
     PRODUCTION is refused, never quietly lowered.

  4. NOT ONE UTTERANCE IS INVENTED. One given is refused and ASKED for;
     the answer carries the question.

  5. NOTHING IS OVERWRITTEN, and the file on disk is unchanged by the
     refusal.

  6. A CARD MAY NOT DECLARE LESS RISK THAN ITS FRAGMENTS CARRY, and
     declaring more is allowed and reported.

  7. THE REQUIRED FIELDS AND THE LADDERS ARE IMPORTED, not copied - by
     identity where they are objects, by derivation where they are not.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_authoring as CRE                                  # noqa: E402
import heron_skill as SKILL                                    # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_capability as CAP                                 # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

FRAGMENTS = [{"id": "count-them", "capability": "COUNT_ELEMENTS",
              "risk": "READ"},
             {"id": "move-them", "capability": "MOVE_ELEMENTS",
              "risk": "MODIFY"}]


def draft(**changes):
    card = {"id": "tally-terminals", "name": "How many: terminals",
            "domain": "revit.reporting",
            "purpose": "Counts air terminals: and says which filter it used.",
            "utterances": ["how many air terminals",
                           "count the terminals on level 2"],
            "needs": ["COUNT_ELEMENTS"],
            "preconditions": ["a document is open"],
            "risk": "READ", "revit": ["2024", "2025"]}
    card.update(changes)
    return card


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_authoring.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]
    where = tempfile.mkdtemp()

    try:
        print("\n1. what it writes passes HERON-SKL-VAL-004 clean")
        good = CRE.author(draft(), fragments=FRAGMENTS, into=where)
        check(good["authored"] is True, "the card was written")
        written = SKILL.load(good["path"])
        problems = SKILL.validate(written)
        check(not problems,
              "and the validator - a different file, with different rules "
              "- finds nothing%s"
              % ("" if not problems else ": %s" % "; ".join(problems)))

        print("\n2. the YAML is dumped, not hand-written")
        kinds = set(type(one).__name__ for one in written.data["revit"])
        # A HAND-WRITTEN `revit: 2024` reads back as an INTEGER, and every
        # release comparison in this project is against strings.
        check(kinds == {"str"},
              "`revit` reads back as strings, not integers: %s"
              % ", ".join(sorted(kinds)))
        check(written.data["purpose"].count(":") == 1
              and written.data["name"] == "How many: terminals",
              "and a colon inside a value survives instead of breaking "
              "the file")

        print("\n3. it enters at DRAFT and the author gets no say")
        check(good["card"]["heron-status"] == "DRAFT",
              "the status written is DRAFT")
        check(CRE.ENTERS_AT == "DRAFT"
              and CRE.ENTERS_AT in FRAG.STATUSES,
              "and DRAFT is read off docs/09's ladder in heron_fragment")
        high = CRE.author(draft(id="a", **{"heron-status": "PRODUCTION"}),
                          into=where)
        reached.add(high.get("refused"))
        check(high["refused"] == "NOT_A_DRAFT",
              "a draft arriving at PRODUCTION is REFUSED")
        check("refused rather than quietly lowered" in high["why"],
              "not quietly lowered - D-35, unapproved is refused")
        check(not os.path.exists(os.path.join(where, "a.yaml")),
              "and nothing was written for it")
        # THE STEP AND VERSION ARE THIS FILE'S OWN, so they cannot drift.
        header = dict(
            (line[2:].split(":", 1)[0], line.split(":", 1)[1].strip())
            for line in whole.split("\n")[:8] if line.startswith("# Heron-"))
        check(str(good["card"]["heron-step"]) == header["Heron-Step"]
              and good["card"]["heron-since"] == header["Heron-Since"],
              "step %s and version %s came out of the agent's own header"
              % (good["card"]["heron-step"], good["card"]["heron-since"]))

        print("\n4. not one utterance is invented")
        thin = CRE.author(draft(id="a", utterances=["just the one"]),
                          into=where)
        reached.add(thin.get("refused"))
        check(thin["refused"] == "TOO_FEW_UTTERANCES",
              "one utterance is refused")
        check(thin.get("asked"),
              "and the answer carries the QUESTION - D-33, never assume "
              "an input, ask once: %r" % thin["asked"])
        check("D-34" in thin["why"],
              "with the reason: Heron builds nothing to decide how a "
              "person talks")
        check(len(good["card"]["utterances"]) == 2
              and good["card"]["utterances"] == draft()["utterances"],
              "and the written card carries the author's two words, "
              "unchanged")

        # AN ID IS A NAME, NOT A PATH. `id: "../agents/ESCAPED"` joined
        # cleanly and wrote an agent-shaped YAML beside the skill library
        # until 2026-09-15, and nothing here or in HERON-SKL-VAL-004
        # restricted the syntax. The drafts this agent writes are
        # MODEL-GENERATED, which is exactly the input a path has to be
        # checked rather than trusted. Found by a review.
        for who, what in (("../agents/ESCAPED", "climbs out with .."),
                          (os.path.join(where, "ABSOLUTE"), "is absolute"),
                          ("sub/dir/NESTED", "carries a separator"),
                          ("..\\windows", "carries a WINDOWS separator - "
                           "one filename here, an escape on the machine "
                           "Revit runs on"),
                          ("..", "is a directory, not a name")):
            escaped = CRE.author(draft(id=who), fragments=FRAGMENTS,
                                 into=where)
            reached.add(escaped.get("refused"))
            check(escaped.get("refused") == "ID_IS_NOT_A_NAME",
                  "an id that %s is refused" % what)
        written = sorted(one for _r, _d, files in os.walk(where)
                         for one in files)
        check(all(os.sep not in one and "/" not in one for one in written),
              "and nothing landed outside the folder: %s"
              % ", ".join(written))

        print("\n5. nothing is overwritten")
        before = io.open(good["path"], encoding="utf-8").read()
        again = CRE.author(draft(name="a different name"),
                           fragments=FRAGMENTS, into=where)
        reached.add(again.get("refused"))
        check(again["refused"] == "ALREADY_EXISTS",
              "a second card at the same path is refused")
        check(io.open(good["path"], encoding="utf-8").read() == before,
              "and the file on disk is byte-for-byte unchanged - GR 14")
        in_library = CRE.author(draft(id="known"),
                                skills=[{"id": "known"}], into=where)
        check(in_library["refused"] == "ALREADY_EXISTS",
              "an id already in the library is refused before any path "
              "is touched")
        check("HERON-SKL-UPD-003" in again["why"],
              "and the answer names the agent that IS allowed to revise")

        print("\n6. a card may not declare less risk than its fragments")
        under = CRE.author(draft(id="a", needs=["MOVE_ELEMENTS"]),
                           fragments=FRAGMENTS, into=where)
        reached.add(under.get("refused"))
        check(under["refused"] == "RISK_IS_UNDERSTATED",
              "READ over a MODIFY fragment is refused")
        check(under["declared"] == "READ" and under["needed"] == "MODIFY"
              and under["through"] == "MOVE_ELEMENTS",
              "the answer names declared %s, needed %s, through %s"
              % (under["declared"], under["needed"], under["through"]))
        over = CRE.author(draft(id="careful", risk="MODIFY"),
                          fragments=FRAGMENTS, into=where)
        check(over["authored"] and over["cautious"] is True,
              "MODIFY over a READ fragment is written AND reported as "
              "cautious")
        check(over["card"]["risk"] == "MODIFY",
              "and it was not lowered to what it needs")
        # NOT KNOWN IS NOT THE SAME AS EQUAL. With no fragment serving
        # the capability, nothing here knows what the work costs.
        blind = CRE.author(draft(id="blind"), into=where)
        check(blind["authored"] and blind["cautious"] is False
              and blind["needs_at_least"] is None,
              "with no fragment serving it, `needs_at_least` is None - "
              "not the declared risk echoed back")
        check(any("neither confirmed nor contradicted" in line
                  for line in blind["unjudged"]),
              "and the answer says so, rather than claiming they agree")

        print("\n7. the required fields and the ladders are imported")
        check(CRE.RISK is CAP.RISK_ORDER,
              "RISK is CAP.RISK_ORDER - the same object")
        check(CRE.VERSIONS is FRAG.REVIT_VERSIONS,
              "VERSIONS is FRAG.REVIT_VERSIONS - the same object")
        check(tuple(sorted(CRE.AUTHORS + CRE.WRITES))
              == tuple(sorted(SKILL.REQUIRED)),
              "AUTHORS and WRITES together ARE heron_skill.REQUIRED, "
              "split on the metadata prefix - so a field added there "
              "becomes required here with no edit")
        check(not set(CRE.AUTHORS) & set(CRE.WRITES),
              "and no field is in both: the author supplies %d, the agent "
              "writes %d" % (len(CRE.AUTHORS), len(CRE.WRITES)))
        for rung in CAP.RISK_ORDER:
            check('"%s"' % rung not in logic,
                  "the module's code does not write %s" % rung)

        print("\n8. every declared failure is named and reached")
        for card, kwargs, name in (
                (None, {}, "NOTHING_TO_AUTHOR"),
                ("a string", {}, "NOT_A_DRAFT"),
                ({"id": "a"}, {}, "INCOMPLETE"),
                (draft(id="a", needs=["count-them"]),
                 {"fragments": FRAGMENTS}, "NAMES_A_FRAGMENT"),
                (draft(id="a", revit=["2028"]), {}, "NOT_A_VERSION"),
                (draft(id="a", risk="DELETE"), {}, "NOT_A_RISK")):
            answer = CRE.author(card, into=where, **kwargs)
            reached.add(answer.get("refused"))
            check(answer.get("refused") == name, "%s is reached" % name)

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-SKL-CRE-002.yaml"))
        named = contract.get("failures") or []
        check(len(named) == 10, "the contract declares 10 failures")
        for failure in named:
            check(failure in logic, "the code names %s" % failure)
        unreached = sorted(set(named) - reached)
        check(not unreached,
              "and every one was reached above%s"
              % ("" if not unreached else ": %s" % ", ".join(unreached)))
        check(len(good["unjudged"]) == 5, "five things are left unjudged")
    finally:
        shutil.rmtree(where)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    at DRAFT, and nobody's words are invented")
    return 0


if __name__ == "__main__":
    sys.exit(main())
