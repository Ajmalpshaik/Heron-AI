# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A fragment that changes the model, claiming a question in writing.

    python tests/test_declared_questions.py

WHY THIS EXISTS
---------------
`tools/check-declared-questions.py` CONCLUDES - it decides which sentences
ask and stop, and which fragments are above the write line - and
tests/test_catalog.py states the rule: a tool that only draws needs no test,
one that concludes does.

Both halves of the conclusion can be wrong while the report still renders. A
regex that called an imperative a question would file *"do the grayout"* as a
defect on the fragment that correctly owns it; one that missed a question
would report a clean sweep over the thing it was written to find.

WHAT IT PROVES
  1. THE SHAPES ARE PINNED BOTH WAYS, on sentences taken from the library
     rather than invented - every one below is a real declared utterance.

  2. AN IMPERATIVE IS NOT A QUESTION, and the two that cost false findings
     the first time this was run by hand are the cases.

  3. THE WRITE LINE IS READ FROM THE REGISTRY, never typed. Golden Rule 19.

  4. A RISK HERONRISK DOES NOT NAME IS REPORTED, never assumed safe. D-52:
     a risk nobody can place is not a low one.

  5. IT READS THE FILES AND NOT THE STORE, so it holds in CI where there is
     none - and the real library is REPORTED here, never gated on, because a
     finding is a question.
"""

import importlib.util
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

spec = importlib.util.spec_from_file_location(
    "heron_declared_questions",
    os.path.join(ROOT, "tools", "check-declared-questions.py"))
TOOL = importlib.util.module_from_spec(spec)
spec.loader.exec_module(TOOL)

FAILURES = []


def check(ok, said):
    print("  %-5s %s" % ("ok" if ok else "FAIL", said))
    if not ok:
        FAILURES.append(said)


class Card(object):
    """The two things the sweep reads off a fragment, and nothing else."""

    def __init__(self, capability, risk, utterances, folder="x"):
        self.data = {"capability": capability, "risk": risk,
                     "utterances": list(utterances), "id": "FRG-TEST-001"}
        self.folder = folder


def main():
    ladder = {"READ": 0, "ANALYZE": 1, "SUGGEST": 2, "EXECUTE": 3,
              "MODIFY": 4, "PUBLISH": 5, "ADMIN": 6}
    write = ladder["MODIFY"]

    print("1. a sentence that asks and stops, on a write")
    # EVERY ONE OF THESE IS A REAL DECLARED UTTERANCE in brain/fragments.
    # An invented sentence would prove the regex matches itself.
    real = ["what scale is this view", "what is the view range",
            "what paper sizes can i print", "is this grid 2d or 3d here",
            "which room is each device in", "what layers are in this dwg",
            "how far apart are these on the drawing",
            "why will this material not purge"]
    asked, told, unranked = TOOL.questions_on_writes(
        [Card("SET_SOMETHING", "MODIFY", real)], write, ladder)
    check(len(asked) == len(real),
          "all %d are findings (%d)" % (len(real), len(asked)))
    check(not told and not unranked, "and none is filed anywhere else")

    print("\n2. an imperative is not a question")
    # THE TWO THAT COST FALSE FINDINGS the first time this was run by hand.
    # "do the grayout" opens with a word a careless pattern reads as the
    # auxiliary in "does this...", and it is an instruction.
    asked, told, _ = TOOL.questions_on_writes(
        [Card("HIGHLIGHT_VS_REST", "MODIFY",
              ["do the grayout", "grey out the background",
               "set the scale of this view", "change these to 1 to 50",
               "dimension between the terminals"])], write, ladder)
    check(not asked and not told,
          "five instructions, and not one is reported")
    # AND ONE THAT ASKS AND THEN SAYS WHAT TO DO is separated rather than
    # counted - the same separation check-risk-crossings.py makes.
    asked, told, _ = TOOL.questions_on_writes(
        [Card("SET_ROUTING_PREFERENCE", "MODIFY",
              ["which elbow this type inserts, change it"])], write, ladder)
    check(not asked and len(told) == 1,
          "'...change it' asks and then tells, so it is listed and not "
          "counted")

    print("\n3. below the write line is not this sweep's business")
    asked, _told, _ = TOOL.questions_on_writes(
        [Card("COUNT_ELEMENTS", "READ", ["how many ducts are there"]),
         Card("REPORT_FINDINGS", "SUGGEST", ["what is missing"]),
         Card("SET_SELECTION", "EXECUTE", ["which ones did you find"])],
        write, ladder)
    check(not asked,
          "a question on a READ, a SUGGEST or an EXECUTE is the library "
          "working")

    print("\n4. a risk HeronRisk does not name")
    _asked, _told, unranked = TOOL.questions_on_writes(
        [Card("SOMETHING", "DANGEROUS", ["what is this"])], write, ladder)
    check(len(unranked) == 1,
          "reported, never assumed safe - a risk nobody can place is not a "
          "low one (D-52)")

    print("\n5. the write line comes from the registry, not from here")
    name, threshold, real_ladder = TOOL.GJ.write_threshold()
    check(threshold == real_ladder.get("MODIFY"),
          "the threshold is MODIFY's own ordinal, read from "
          "HeronOperationRegistry.cs by name (Golden Rule 19)")
    check("MODIFY" in real_ladder and "READ" in real_ladder,
          "and the ladder is HeronRisk's, with %d level(s)" % len(real_ladder))
    check("generate_jobs" in TOOL.GJ.write_threshold.__module__,
          "imported from generate-jobs.py rather than copied (%s) - it "
          "refuses to guess when the registry moves, and two copies would "
          "disagree" % TOOL.GJ.write_threshold.__module__)

    print("\n6. the real library, reported and not gated")
    import heron_fragment as FRAG
    found, _problems = FRAG.load_all()
    asked, told, unranked = TOOL.questions_on_writes(
        list(found.values()), threshold, real_ladder)
    print("       %d question(s) declared on a write, %d that ask and then "
          "tell, %d unrankable" % (len(asked), len(told), len(unranked)))
    for capability, risk, phrase, _fid, _folder in asked:
        print("         %-30s %-7s %r" % (capability, risk, phrase))
    check(True, "printed either way - a finding is a question, and this "
                "suite does not fail on the library's own state")
    check(not unranked,
          "and today every fragment's risk is a level HeronRisk names")

    print("\n7. the same rule, one layer up, on the real skills")
    # ROW 137 says a question inside a MODIFY skill can never register as a
    # crossing, because a crossing compares the sentence's reach against the
    # SKILL's own declared risk. This asks nothing about reach, so it sees
    # them - and the adapter is four lines rather than a second copy of the
    # rule, which would be a second opinion about what a question is.
    import heron_skill as SKILL
    skills, _ = SKILL.load_all()
    cards = [TOOL._AsCard(one) for one in skills.values()]
    check(all(c.data["utterances"] and c.data["risk"] for c in cards),
          "every skill adapts to the two fields the rule reads (%d)"
          % len(cards))
    s_asked, _s_told, s_unranked = TOOL.questions_on_writes(
        cards, threshold, real_ladder)
    print("       %d question(s) declared on a skill that writes"
          % len(s_asked))
    for sid, risk, phrase, _fid, _folder in sorted(s_asked):
        print("         %-22s %-7s %r" % (sid, risk, phrase))
    check(not s_unranked,
          "and every skill's risk is a level HeronRisk names")
    check(True, "printed either way - some of these are probably RIGHT, and "
                "deleting a sentence a modeller says to shorten the list is "
                "row 113's forbidden move mirrored")

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1
    print("PASSED - a question that asks and stops, declared by something")
    print("that changes the model, is found; an instruction is not.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
