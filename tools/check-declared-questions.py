# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Does a fragment that CHANGES THE MODEL claim a QUESTION in writing?

    python tools/check-declared-questions.py
    python tools/check-declared-questions.py --all

Always exits 0. This reports; it does not gate.

WHY THIS EXISTS, AND WHY THE TWO SIBLINGS CANNOT SEE IT
---------------------------------------------------------
`check-routing.py` asks each fragment's own utterances back to the search and
separates the one contest that is not a judgement call: a sentence a READ
claims, answered by something that WRITES. `check-risk-crossings.py` asks
sentences NOBODY declares, because the first can only test what is declared.

**A WRITE THAT DECLARES A QUESTION IS INVISIBLE TO BOTH**, and to
`check-routing` for two independent reasons rather than one:

  - its first test keeps a row where `rung(risk[winner]) > rung(risk[claimer])`,
    and a fragment declaring its OWN sentence and winning has winner ==
    claimer, so the comparison is false and there is no crossing to report;
  - its second opens `if risk_of(store, fid) != "READ": continue`, so a
    sentence claimed by a MODIFY is skipped on the first line - and two lines
    later `if served == fid: continue` carries the belief in a comment,
    **`# the host answers correctly`**.

That comment is right about every case that tool was built for and wrong
about this one. Nothing in it is at fault; the question was never asked of it.
And the sentence IS declared, so it is not one of the ones
`check-risk-crossings` was written to try - that sweep exists precisely
because the first can only test what is declared.

The defect lives in the gap between the two, and it needs no search at all to
find: it is a property of the card.

Found 2026-09-19 while checking whether four sentences had a READ to give
them to (FRAGMENT-ISSUES row 146). *"what scale is this view"* resolves to
`SET_VIEW_SCALE`, a MODIFY, by `identity` - because
`brain/fragments/set-view-scale/fragment.yaml` declares that exact phrase in
its own `utterances:` block. **No ranking change can repair that**, which is
why it is worth a separate sweep.

WHAT IT READS, AND WHAT IT DOES NOT
-------------------------------------
The FILES, through `heron_fragment.load_all()`. **It never touches the
store**, so it holds in CI where there is none and two runs disagree only if
somebody edited a fragment - the distinction rows 116 and 136 are about.

The write threshold is read from `HeronOperationRegistry.cs` through
`generate-jobs.write_threshold()` - Golden Rule 19, where risk is looked up by
name and never supplied by a caller. A risk a fragment declares that is not a
level `HeronRisk` names is REPORTED rather than assumed safe.

WHAT IT DOES NOT DECIDE
-----------------------
**Whether the sentence should move or the fragment should.** A question on a
write is sometimes exactly right - *"which elbow this type inserts, change
it"* is an instruction wearing a question's first word, and so is *"do the
grayout"*. Those are listed SEPARATELY and are not findings: what marks one is
that the sentence asks and stops.

That test is crude and deliberately so, the same argument
`check-risk-crossings.py` makes about its own: a tool that parsed intent would
be a second, worse retriever. Judgement stays with the reader, which is why
this exits 0 whatever it finds.

**AND IT NEVER SUGGESTS DELETING AN UTTERANCE TO TIDY THE REPORT.** Row 113's
forbidden move is weakening a declaration to buy a number, and the mirror of it
would be deleting a sentence a modeller really says so that a sweep comes back
clean. The repair for a question on a write is to declare it on the READ that
should own it - and where no READ exists, the finding is a capability gap,
which is a different and larger thing.
"""

import argparse
import importlib.util
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as FRAG                                 # noqa: E402


def _sibling(name):
    """Import a tool that lives beside this one, by path.

    THE FILENAME HAS A HYPHEN IN IT. `write_threshold()` is imported rather
    than copied for the reason its own docstring gives: it refuses to guess
    when the registry has been rearranged, and a second copy would be a second
    opinion about where the write line is.
    """
    path = os.path.join(ROOT, "tools", name)
    spec = importlib.util.spec_from_file_location(
        "heron_tool_" + name.replace("-", "_").replace(".py", ""), path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


GJ = _sibling("generate-jobs.py")


# A QUESTION BY THE SHAPE OF THE SENTENCE, AND NOTHING CLEVERER. Every word
# here opens a sentence a modeller ends without doing anything. `do` and `did`
# are deliberately NOT in it: "do the grayout" is an imperative, and including
# them cost two false findings the first time this was run by hand.
ASKS = re.compile(
    r"^(what|which|whose|when|where|who|why|how)\b"
    r"|^(is|are|was|were|does|can|could|has|have|had|should|will|would)\b",
    re.I)

# AND AN INSTRUCTION WEARING A QUESTION'S FIRST WORD IS NOT A QUESTION.
# "which elbow this type inserts, change it" asks and then says what to do.
# Listed separately, never counted as a finding - the same separation
# check-risk-crossings.py makes for imperatives.
TELLS = re.compile(
    r"\b(change it|change them|fix it|fix them|set it|set them|do it|"
    r"make it|make them|and change|then change|and set|then set)\b", re.I)


def questions_on_writes(fragments, threshold, ladder):
    """(asked, told, unranked) over the fragments handed in.

    `asked` is the finding: a fragment at or above the write threshold
    declaring a sentence that asks and stops. `told` is the same shape with an
    instruction in it, reported and not counted. `unranked` is a fragment whose
    declared risk is not a level HeronRisk names - REPORTED rather than assumed
    safe, because a risk nobody can place is not a low one (D-52).
    """
    asked, told, unranked = [], [], []
    for frag in fragments:
        risk = str(frag.data.get("risk") or "").upper()
        capability = str(frag.data.get("capability") or "")
        where = ladder.get(risk)
        if where is None:
            unranked.append((capability, risk, frag.data.get("id")))
            continue
        if where < threshold:
            continue
        for said in (frag.data.get("utterances") or []):
            phrase = str(said).strip()
            if not ASKS.match(phrase):
                continue
            row = (capability, risk, phrase, frag.data.get("id"), frag.folder)
            (told if TELLS.search(phrase) else asked).append(row)
    return asked, told, unranked


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--all", action="store_true",
                        help="also print the instructions, which are not "
                             "findings")
    args = parser.parse_args()

    found, problems = FRAG.load_all()
    _name, threshold, ladder = GJ.write_threshold()

    asked, told, unranked = questions_on_writes(
        sorted(found.values(), key=lambda f: str(f.data.get("capability"))),
        threshold, ladder)

    out = sys.stdout.write
    out("A FRAGMENT THAT CHANGES THE MODEL, CLAIMING A QUESTION\n")
    out("=" * 62 + "\n\n")
    out("%d fragment(s) read from disk. The write line is at or above "
        "ordinal %d,\nread from the registry by name (Golden Rule 19).\n\n"
        % (len(found), threshold))

    out("DECLARED IN WRITING, AND THE SENTENCE ASKS AND STOPS (%d):\n"
        % len(asked))
    if not asked:
        out("  none\n")
    for capability, risk, phrase, fid, folder in asked:
        out("  %-30s %-7s %s\n" % (capability, risk, repr(phrase)))
        out("  %-30s %s/fragment.yaml\n" % ("", folder))
    if asked:
        out("\n")
        out("  These resolve by IDENTITY, which short-circuits before any\n")
        out("  ranking runs - so no re-ranking repairs one, and neither\n")
        out("  check-routing.py nor check-risk-crossings.py can see them.\n")
        out("  The repair is to declare the sentence on the READ that should\n")
        out("  own it. Where no READ exists, this is a capability gap.\n")

    out("\n")
    out("ASKS AND THEN SAYS WHAT TO DO (%d) - not findings:\n" % len(told))
    if not told:
        out("  none\n")
    elif not args.all:
        out("  %d, hidden. --all prints them.\n" % len(told))
    else:
        for capability, risk, phrase, fid, folder in told:
            out("  %-30s %-7s %s\n" % (capability, risk, repr(phrase)))

    if unranked:
        out("\n")
        out("A RISK HERONRISK DOES NOT NAME (%d) - reported, never assumed "
            "safe:\n" % len(unranked))
        for capability, risk, fid in unranked:
            out("  %-30s %-12s %s\n" % (capability, repr(risk), fid))

    for line in problems[:5]:
        out("  unreadable: %s\n" % line)

    out("\n")
    out("Exit 0 whatever it finds. A finding is a question for a reader, and\n")
    out("deleting an utterance a modeller really says is not the repair.\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
