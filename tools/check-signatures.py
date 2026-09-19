# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Is anybody's signature sitting unused?

    python tools/check-signatures.py           # findings only
    python tools/check-signatures.py --all     # including what is fine

Exit 1 when a signature is being wasted, 0 otherwise.

WHY THIS EXISTS
---------------
`heron_validate.py accept` writes the proof and DELIBERATELY never writes
`heron-status` - its own header says so, and that separation is right: the
evidence and the decision to trust it are different acts. But it leaves a gap
nothing was watching. Promotion is a hand edit, a hand edit gets forgotten, and
a forgotten one is invisible: the fragment still reads DRAFT, so the next round
counts it as unproved and offers it up to be proved AGAIN.

Measured 2026-09-13. THIRTEEN fragments carried Ajmal PS's signature and were
still DRAFT, the oldest six days old. He noticed before this tool did, from the
one symptom visible from outside:

    "becose i signed item i have to do again i sow lkike that for
     exambple set view crop i singe multiple time"

He was right, and about the worst possible thing to be right about. The machine
gathers evidence; only a PERSON may sign (brain/proof-drafts/README.md). His
signature is the single scarcest input this library has, and the loop above
spends it twice for one fragment while the count of remaining work never moves.

THE TWO FINDINGS ARE NOT THE SAME FINDING
------------------------------------------
Of those thirteen, six were waste and seven were not, and merging them would
hide both:

  * UNUSED - signed, the code underneath unchanged, `can_promote` says yes,
    still DRAFT. Nothing stands between the signature and PROVEN except an edit
    nobody made. This is the waste, and it is what fails this check.

  * STALE - signed, but the implementation changed afterwards, so the proof no
    longer describes the code that would run. Re-proving is CORRECT here; D-30
    wants a stale proof to be loud. Reported so the re-signing is expected
    rather than mysterious, but NOT a failure - the system working is not a
    defect.

That split is D-52's rule in another costume: a count of things correctly
refused is never evidence of something found.

WHAT IT CANNOT DO
-----------------
It does not judge the proof. `can_promote` does that, and this asks it rather
than re-deriving the answer, so a rule added there is honoured here for free.
It also cannot tell a forgotten promotion from one being deliberately held
back - if a fragment is signed and meant to WAIT, that intent has to live in
the file as a caveat, because this tool can only see the two facts.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as F                                   # noqa: E402


def findings():
    """(unused, stale, held, signed_total). Every fragment signed, not promoted.

    `held` IS A THIRD ANSWER AND IT WAS MISSING. This file's own header says a
    fragment "signed and meant to WAIT" has to carry that intent as a caveat -
    and then had no way to READ one, so a deliberate hold was reported as an
    oversight and failed the gate. `create-roof` on 2026-09-19 is the case:
    signed on real evidence, correctly NOT promoted because `tests/cases.yaml`
    declares a second route nobody has run, and nothing could say so.

    THE CAVEAT IS A DECLARED KEY, NOT PROSE. `proof-held:` in fragment.yaml
    holds the REASON, so a reader sees why and this tool can tell a hold from a
    forgotten promotion. Prose in a comment would leave it guessing again.
    """
    unused, stale, held, signed = [], [], [], 0

    folders, _problems = F.load_all()
    for _id, frag in sorted(folders.items()):
        proof = getattr(frag, "proof", None) or {}
        by = (proof.get("by") or "").strip()
        if not by:
            continue
        signed += 1
        if (frag.status or "") != "DRAFT":
            continue

        allowed, why = F.can_promote(frag, "PROVEN")
        waiting = (frag.data.get("proof-held") or "").strip()
        row = (frag.slug, by, proof.get("date"), waiting or why)
        if waiting:
            held.append(row)
        else:
            (unused if allowed else stale).append(row)

    return unused, stale, held, signed


def main(argv):
    show_all = "--all" in argv
    unused, stale, held, signed = findings()

    print("Signatures in the library: %d" % signed)
    print("")

    if unused:
        print("UNUSED - signed, nothing blocking, still DRAFT (%d)" % len(unused))
        print("A person already looked at the evidence and put their name to it.")
        print("Set heron-status: PROVEN, or record in the file why it waits.")
        print("")
        for name, by, date, why in unused:
            print("  %-32s %s, %s" % (name, by, date))
        print("")

    if held:
        print("HELD - signed, and DELIBERATELY not promoted (%d)" % len(held))
        print("Each one says in its own file why it waits. This is not a gap.")
        print("")
        for name, by, date, why in held:
            print("  %-32s %s, %s" % (name, by, date))
            print("      %s" % why)
        print("")

    if stale:
        print("STALE - signed, then the code changed under it (%d)" % len(stale))
        print("These must be proved again. That is D-30 working, not a fault.")
        print("")
        for name, by, date, _why in stale:
            print("  %-32s %s, %s" % (name, by, date))
        print("")

    if not unused and not stale:
        print("No signature is waiting. Every one is either promoted or")
        print("correctly held.")
    elif show_all and not unused:
        print("Nothing is being wasted.")

    return 1 if unused else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
