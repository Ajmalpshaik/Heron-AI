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
import hashlib
import os
import pathlib
import sqlite3
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "brain"))


# WHICH INDEX THIS RAN AGAINST - because two runs of this tool were not
# comparable and nothing said so.
#
# Measured 2026-09-17: three sweeps minutes apart, same code, same machine,
# returned 7, 9 and 8 crossings, and two of those had byte-identical inputs.
# FRAGMENT-ISSUES row 116 carries the diagnosis. The short version is that the
# count is not a property of the ranker:
#
#   * `how many pipes are there` is answered by the IDENTITY route, not by
#     ranking - COUNT_ELEMENTS declares it, which is how row 116 fixed it. A
#     run where that identity MISSES falls through to the ranked search, and
#     the ranked search is what answers a question with a write.
#   * `identities` and `fragment_text` are DELETEd and rebuilt wholesale by
#     heron_search.index(), so what this tool measures depends on when that
#     last ran and on which fragment tree ran it.
#   * global.db is ONE file for every worktree on the machine. Another session
#     re-indexing it moves this tool's answer with no commit in this repo.
#
# So a bare number from this tool is a sample. Printing the store's identity
# next to the number is what makes two samples worth comparing - and if they
# disagree while the fingerprint matches, the ranker really is the suspect.
def _index_fingerprint():
    """{path, md5, counts} for the store this sweep will actually read.

    A failure to read it is REPORTED, never returned as a zero: a fingerprint
    that silently reads as an empty store is the plausible-zero shape D-52
    names, in the one place whose whole job is to make runs comparable.
    """
    import heron_scope as SCOPE

    out = {}
    try:
        # One spelling. scope_path joins with os.sep while HERON_KNOWLEDGE
        # arrives however the caller typed it, so the same file printed twice
        # can read as two - which is the one thing this block exists to stop.
        out["path"] = SCOPE.scope_path(SCOPE.GLOBAL).replace(os.sep, "/")
    except ValueError as why:
        return {"error": str(why)}

    try:
        with open(out["path"], "rb") as handle:
            out["md5"] = hashlib.md5(handle.read()).hexdigest()
    except (IOError, OSError) as why:
        out["md5"] = "unreadable (%s)" % why
        return out

    counts = {}
    try:
        uri = pathlib.Path(out["path"]).as_uri() + "?mode=ro"
        db = sqlite3.connect(uri, uri=True)
        try:
            for table in ("identities", "fragments", "vectors", "utterances"):
                counts[table] = db.execute(
                    "SELECT COUNT(*) FROM %s" % table).fetchone()[0]
        finally:
            db.close()
    except Exception as why:                                   # noqa: BLE001
        # Named, not swallowed. Whatever went wrong here, the reader must not
        # be handed a row of zeroes that looks like a fresh store.
        out["counts_error"] = "%s: %s" % (type(why).__name__, why)
        return out

    out["counts"] = counts
    return out


def _md5(path):
    """The store's hash again, for the did-this-run-write-it check."""
    try:
        with open(path, "rb") as handle:
            return hashlib.md5(handle.read()).hexdigest()
    except (IOError, OSError):
        return None


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

    # THE SENTENCE THIS FILE'S OWN HEADER IS ABOUT, AND IT WAS NEVER ASKED
    # HERE. FRAGMENT-ISSUES row 109 measured it by hand on 2026-09-16 -
    # SET_MEP_SLOPE, a MODIFY that re-slopes pipework, 2.4 ranks clear, with
    # ISOLATE_ELEMENTS fourth. It is quoted at the top of this docstring as
    # the reason the tool exists and then does not appear in the list below
    # it, so every sweep since has measured 45 questions that do not include
    # the one that started this. Added 2026-09-17; the docstring asks for
    # exactly this ("add to this list whenever a real session produces one").
    #
    # IT MAY NOT REGISTER AS A CROSSING, AND THAT IS WORTH WATCHING RATHER
    # THAN TUNING. The discriminator below is "was a READ beaten", and the
    # right answer here is ISOLATE_ELEMENTS - an EXECUTE, a view change,
    # which `reads` deliberately excludes along with the writes. So a sweep
    # can report this sentence under "reached a write with nothing safe
    # close" while row 109 calls it the clearest crossing found by hand.
    # If that is what happens, the honest reading is that the DISCRIMINATOR
    # is narrower than the defect, not that the sentence is fine - and the
    # fix is a question for the owner, not a quiet widening of `reads`.
    "isolate all the pipes in the current view but leave out the "
    "condensate drain system",

    # SENTENCES THE LIBRARY ITSELF SAYS AJMAL SAYS, AND THAT NO FRAGMENT
    # CLAIMS. Added 2026-09-19 by the session that built
    # tools/check-skill-routing.py, which is the docstring's instruction
    # ("add to this list whenever a real session produces one") met from a
    # source that did not exist when this list was written: brain/skills.
    #
    # THE CRITERION WAS FIXED BEFORE THE RESULTS WERE SEEN, on purpose. Each
    # of these is declared in a skill's `utterances:` and declared by NO
    # fragment - measured, not judged - which is exactly the hole rows 113 and
    # 116 name: identity cannot fire, so ranking decides, and ranking is the
    # thing that answers a question with a write. Choosing them by which ones
    # FAILED would be optimising against the measurement, which is the drift
    # this list's own docstring exists to prevent.
    #
    # They are a SAMPLE of the 37 such sentences and not all of them: asking
    # all 37 here would make this sweep a duplicate of check-skill-routing.py,
    # which asks every one of them against a different discriminator - a
    # skill's own declared risk, rather than "was a READ beaten".
    "how much air does this room need",
    "how many sprinklers",
    "how many diffusers for this room",
    "is the ductwork still connected",
    "what is this connected to",
    "which unit feeds this",
    "what sizes are the ducts",
    "which ducts have no system name",
    "what is missing before I issue this",
    "how many of each size",
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

    # READ IT BEFORE ASKING ANYTHING. This tool is not read-only about the
    # store - measured 2026-09-17, one sweep against a private copy changed
    # that copy's md5 - so a fingerprint taken afterwards would describe a
    # store this run had already moved.
    index = _index_fingerprint()

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
    print("THE INDEX THIS RAN AGAINST - compare it before comparing counts:")
    if index.get("error"):
        print("  no store: %s" % index["error"])
    else:
        print("  store    %s" % index["path"])
        print("  md5      %s" % index["md5"])
        if index.get("counts_error"):
            print("  counts   COULD NOT BE READ - %s" % index["counts_error"])
            print("           Not reported as zero on purpose: a store that")
            print("           cannot be read is not an empty one (D-52).")
        else:
            print("  rows     %s" % ", ".join(
                "%s %d" % (name, index["counts"][name])
                for name in sorted(index.get("counts") or {})))
        print("  Two runs whose numbers differ while THIS block matches are a")
        print("  question about the ranker. Two whose numbers differ and whose")
        print("  block differs are a question about the index, and that is the")
        print("  way round it has been every time so far - see row 116.")
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

    # DID ASKING CHANGE THE THING ASKED? This tool's docstring said "it reads
    # the store" until 2026-09-17, when a sweep against a private copy that
    # nothing else on the machine could reach came back with a different md5.
    # Saying so every run is cheaper than somebody re-discovering it: the next
    # reader comparing two fingerprints needs to know that RUNNING THIS is one
    # of the things that can move them.
    if index.get("path") and index.get("md5"):
        after = _md5(index["path"])
        if after and after != index["md5"]:
            print("")
            print("THE STORE CHANGED WHILE THIS RAN:")
            print("  before   %s" % index["md5"])
            print("  after    %s" % after)
            print("  Asking moved the index. That is this tool, another")
            print("  session, or both - global.db is ONE file for every")
            print("  worktree on the machine. It is why a count from here is a")
            print("  sample rather than a measurement.")

    print("")
    print("Exit 0 whatever this finds. A crossing is a judgement a person")
    print("makes, the same rule tools/check-routing.py sets - and weakening a")
    print("question to buy back a rank is never the answer.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
