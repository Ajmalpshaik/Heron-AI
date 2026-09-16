# Heron-Agent:  HERON-RAG-RNK-006
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Does a QUESTION ever get answered by something that CHANGES THE MODEL?

    python tools/check-risk-crossings.py
    python tools/check-risk-crossings.py --revit 2024
    python tools/check-risk-crossings.py --list

WHY THIS EXISTS ALONGSIDE check-routing.py
-------------------------------------------
`check-routing.py` asks each fragment's OWN declared utterances back to the
search, and separates the one contest that is not a judgement call: a sentence
a READ claims, answered by something that WRITES. That check is right and it
is kept.

**IT CAN ONLY TEST SENTENCES A FRAGMENT DECLARES**, and on 2026-09-16 both
crossings found in a real session were sentences nobody declares:

    "can I edit these"                     -> UPDATE_SAVED_SET  (MODIFY)
    "isolate all the pipes but leave out
     the condensate drain"                 -> SET_MEP_SLOPE     (MODIFY)

Neither appears in any `utterances:` block, so `check-routing` reported one
crossing and the library looked almost clean. Asking 45 ordinary questions
instead found **17**. The gap was never in the ranking - it was in what the
gate was allowed to ask.

SO THE QUESTIONS HERE ARE WRITTEN DOWN RATHER THAN DERIVED
-----------------------------------------------------------
They are what a modeller says on a normal day - sizes, counts, levels, phases,
worksets, systems, clashes, slopes. **A question a fragment already declares is
worth less here**, not more: the point is the sentences nobody thought to
claim.

**Add to this list whenever a real session produces one.** That is how it stays
honest; a fixed list stops measuring the day somebody optimises against it.

WHAT IT DOES NOT DECIDE
-----------------------
**An imperative is not a question.** "isolate all the pipes" resolving to
ISOLATE_ELEMENTS is CORRECT even though isolating is not a READ, and so is
"hide everything except the walls" landing on HIDE_ELEMENTS. Those are reported
separately and are not failures: what marks a real crossing is that **a READ
was right there and lost**.

That test is crude and deliberately so. A tool that tried to parse intent would
be a second, worse retriever. Judgement stays with the reader - which is why
this exits 0 whatever it finds, exactly like check-routing.

WHAT IT CANNOT SEE
------------------
It reads the store, so a fragment edited but not re-indexed is judged as the
search actually sees it - correct for this question, and NOT a statement about
the file on disk. And it asks the same seam a host uses, so a result here is
what a user would really have got, not a simulation of one.
"""

import argparse
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))


# WHAT ACTUALLY CHANGES A MODEL, taken from docs/12 s2 rather than from "not
# READ". The ladder is READ, ANALYZE, SUGGEST, EXECUTE, MODIFY, PUBLISH, ADMIN
# and docs/12 gives ANALYZE side effects "none" - it computes over what was
# read. Flagging it would have reported `describe-blank-parameters` answering
# "which parameters are empty" as a danger, when that fragment IS the right
# answer to that question.
#
# EXECUTE IS THE HONEST MIDDLE and is counted separately. `isolate-elements`
# and `zoom-to-elements` change what a VIEW shows - real, visible, and undone
# by Reset Temporary Hide/Isolate. That is not the failure this tool exists to
# catch, which is a question answered by a change to the MODEL.
CHANGES_THE_MODEL = ("MODIFY", "PUBLISH", "ADMIN")
CHANGES_A_VIEW = ("EXECUTE",)


# Ordinary things a modeller asks. Questions FIRST, then the imperatives that
# are allowed to reach a write, kept in one list so the tool sorts them by what
# the search does rather than by what somebody assumed.
QUESTIONS = [
    "can I edit these",
    "who owns this element",
    "is anyone else working on this",
    "what size is this duct",
    "what is the diameter of this pipe",
    "how long is this run",
    "which level is this on",
    "what phase is this on",
    "what workset is this on",
    "how many ducts are in this view",
    "how many pipes are there",
    "count the air terminals",
    "what systems are in this model",
    "is this connected to anything",
    "what is this pipe connected to",
    "show me the clashes",
    "are there any clashes here",
    "what is clashing with what",
    "which elements are not on a system",
    "what is missing its mark",
    "which parameters are empty",
    "what families are loaded",
    "what types are in this model",
    "which types are unused",
    "what views are on this sheet",
    "which sheets are missing a titleblock",
    "what revisions are on this sheet",
    "is this model workshared",
    "how big is this model",
    "what links are loaded",
    "what is the slope of this pipe",
    "is this pipe sloped correctly",
    "does this drain fall the right way",
    "what material is this",
    "what is the insulation thickness",
    "is this insulated",
    "where is this element",
    "what is the elevation of this",
    "how high is this off the floor",
    "select all the pipes in this view",
    "find the fire dampers",
    "which valves are in this riser",
    # Imperatives. These MAY reach a write and it is not a defect.
    "isolate all the pipes",
    "show me just the ducts",
    "hide everything except the walls",
]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--revit", default="2020")
    parser.add_argument("--list", action="store_true",
                        help="print the questions and run nothing")
    args = parser.parse_args()

    if args.list:
        for question in QUESTIONS:
            print(question)
        return 0

    import heron_brain as brain

    crossings, allowed, unresolved = [], [], []

    for question in QUESTIONS:
        try:
            found = brain.lookup(question, revit=args.revit)
        except Exception as why:                       # noqa: BLE001
            unresolved.append((question, "%s: %s" % (type(why).__name__, why)))
            continue

        capability = found.get("capability")
        if not capability:
            unresolved.append((question, "no capability"))
            continue

        risk = (found.get("risk") or "").upper()
        if risk not in CHANGES_THE_MODEL and risk not in CHANGES_A_VIEW:
            continue

        # THE DISCRIMINATOR, AND THE WHOLE JUDGEMENT THIS TOOL MAKES: was a
        # READ right there and beaten? If nothing read-only was close, the
        # request probably WAS asking for the change.
        reads = [c["capability"] for c in found.get("candidates") or []
                 if (c.get("risk") or "").upper() not in CHANGES_THE_MODEL
                 and (c.get("risk") or "").upper() not in CHANGES_A_VIEW
                 and c["capability"] != capability]

        row = (question, capability, risk, reads[0] if reads else None)
        if reads and risk in CHANGES_THE_MODEL:
            crossings.append(row)
        else:
            allowed.append(row)

    print("Revit %s   questions asked: %d" % (args.revit, len(QUESTIONS)))
    print("")
    print("A QUESTION ANSWERED BY SOMETHING THAT WRITES, WITH A READ BEATEN (%d):"
          % len(crossings))
    if not crossings:
        print("  none")
    for question, capability, risk, read in crossings:
        print("  %-42s -> %-26s %-7s  beat %s"
              % (question[:42], capability, risk, read))

    if crossings:
        print("")
        print("  A caller acting on one of these does not get a poor answer to")
        print("  its question; it CHANGES THE MODEL in reply to one. heron_lookup")
        print("  warns on exactly this shape at the seam a host uses - the")
        print("  warning is the floor, not the fix.")

    print("")
    print("REACHED A VIEW CHANGE, OR A WRITE WITH NOTHING SAFE CLOSE (%d) - read them:"
          % len(allowed))
    for question, capability, risk, _ in allowed:
        print("  %-42s -> %-26s %s" % (question[:42], capability, risk))

    if unresolved:
        print("")
        print("NOT RESOLVED (%d):" % len(unresolved))
        for question, why in unresolved:
            print("  %-42s %s" % (question[:42], why))

    print("")
    print("Exit 0 whatever this finds. A crossing is a judgement a person")
    print("makes, the same rule tools/check-routing.py sets - and weakening a")
    print("question to buy back a rank is never the answer.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
