#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Two tools met on a scratch roof model on 2026-11-21, read before a Revit is.

    python tests/test_grid_and_copy_values.py

Both were seen on the owner's PC, Revit 2024, on the scratch project
`roofjoin-oy`, and both cards were PROVEN:

  1. CREATE_GRIDS (row 5b-1948, issue #1174) put the sentence saying what it
     MADE in `refused` - a grid drawn and kept came back with `refused`
     holding "1 grid line(s) drawn between the points given, each one level".
     All three of its ways of making grids did it on purpose. `refused` holds
     a reason nothing was made, and nothing else; what was made is said in
     `findings`.
  2. COPY_ELEMENTS (row 5b-1949, issue #1175) refused a copy by an offset
     unless `toLevels`, `turnDegrees` and `turnCentreMm` were typed blank,
     though its card said blank copies by the offset. They may be left out
     now, and left out they read as the blank version 2 reads - an offset copy
     is asked with `offset` alone.

WHAT IT READS. The card's C# as text, for where each sentence goes; and the
card's contract through the same mirror of the add-in's binder that the
server asks before it sends anything (heron_fragment.need_may_be_absent,
through heron_brain.fragment_contract) - never a second copy of the rule.

WHAT IT CANNOT SAY. Nothing about Revit: no grid is made and nothing is
copied here, so a pass moves no row and proves no card. The runs are
NEEDS-CHECKING groups PI and PJ.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

for stream in (sys.stdout, sys.stderr):
    try:
        stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

import heron_fragment as HF                                    # noqa: E402

FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(*parts):
    with io.open(os.path.join(FRAGMENTS, *parts), "r", encoding="utf-8") as fh:
        return fh.read()


def provide(frag, name):
    for entry in frag.provides() or []:
        if isinstance(entry, dict) and entry.get("name") == name:
            return entry
    return None


def need(frag, name):
    for entry in frag.needs() or []:
        if isinstance(entry, dict) and entry.get("name") == name:
            return entry
    return None


def adder_of(code, phrase):
    """The list a sentence holding `phrase` is added to - the name before the
    nearest `.Add(` or `.AddRange(` above it - or None when it is not there."""
    at = code.find(phrase)
    if at < 0:
        return None
    calls = list(re.finditer(r"\b(\w+)\.Add(?:Range)?\(", code[:at]))
    return calls[-1].group(1) if calls else None


def grids():
    print("1. CREATE_GRIDS: `refused` holds a refusal and nothing else (row 5b-1948)")
    frag = HF.load(os.path.join(FRAGMENTS, "create-grids"))
    code = read("create-grids", "impl", "any", "fragment.cs")

    check(provide(frag, "findings") is not None,
          "the card declares `findings`, where what was made is said")
    check(re.search(r"^var findings = new List<string>\(\);", code, re.M) is not None,
          "and the C# leaves a `findings` list at its top level")

    # THE SENTENCE OF EACH OF THE THREE WAYS, as the run record of
    # 2026-11-21 and the 2026-11-02 proof quote them.
    # The string literal itself, `"{0} ...` - a comment quoting the sentence
    # with a number in it is not where it is added.
    for mode, phrase in (("radial", '"{0} arc grid(s) and {1} spoke grid(s) about the centre'),
                         ("between points", '"{0} grid line(s) drawn between the points given'),
                         ("bay spacings", '"{0} spacing(s) across gave {1} lettered grid(s)')):
        where = adder_of(code, phrase)
        check(where == "findings",
              "%s: the sentence %s...\" is added to findings, not refused - it is "
              "added to %s"
              % (mode, phrase, where))
    where = adder_of(code, "radialNotes)")
    check(where == "findings",
          "radial: a spoke outside the arcs' sweep is said in findings - drawn, never "
          "refused (added to %s)" % where)

    made = code.find("Grid.Create(")
    check(made >= 0, "the C# makes its grids with Grid.Create")
    late = re.findall(r"\brefused\.Add(?:Range)?\(", code[made:]) if made >= 0 else ["?"]
    check(not late,
          "nothing is added to `refused` once the first grid is made - a refusal "
          "means nothing was (%d added after)" % len(late))

    said = (provide(frag, "refused") or {})
    text = read("create-grids", "fragment.yaml")
    block = text[text.find("    - name: refused"):text.find("\nproof:")]
    check("after a run" not in block,
          "the card no longer says `refused` holds what was made after a run")
    check(said.get("role") == "accounting",
          "`refused` stays accounting - why it declined, never a thing found (D-52)")

    # PROVEN AGAIN ON 2026-11-24 (Revit 2024, Night Rig 1124): version 4's own
    # proof. DRAFT until then; PROVEN on a proof of older code is the defect.
    proved_now = (frag.proof or {}).get("fingerprint") == frag.fingerprint()
    check(int(frag.data.get("version") or 0) >= 4
          and (frag.status == "DRAFT" or (frag.status == "PROVEN" and proved_now)),
          "version 4 or later, DRAFT or PROVEN on a proof of this code - never "
          "PROVEN on a proof its C# moved past (it reads %s, version %s)"
          % (frag.status, frag.data.get("version")))
    check(not frag.data.get("proof-held"),
          "and it holds no proof back: its proof no longer describes the code")


def copies():
    print()
    print("2. COPY_ELEMENTS: a copy by an offset is asked with the offset alone (row 5b-1949)")
    frag = HF.load(os.path.join(FRAGMENTS, "copy-elements"))
    code = read("copy-elements", "impl", "any", "fragment.cs")

    try:
        import heron_brain as B
        contract = B.fragment_contract("copy-elements")
    except Exception as exc:                      # noqa: BLE001 - one clean failure
        contract = None
        print("        (the contract could not be read: %s)" % exc)
    required = (contract or {}).get("required")
    check(required == ["offset"],
          "the values the server asks for before it sends are `offset` alone - %s"
          % required)

    for name, variable in (("toLevels", "levelsText"), ("turnDegrees", "turnText"),
                           ("turnCentreMm", "centreText")):
        entry = need(frag, name) or {}
        check(HF.need_may_be_absent(entry),
              "%s may be left out - the add-in's binder, mirrored, binds it" % name)
        check(HF.need_absent_text(entry) == "",
              "%s left out reads as blank text, what version 2 means by blank (%r)"
              % (name, HF.need_absent_text(entry)))
        typed = re.search(r"var %s = \(%s \?\? \"\"\)\.Trim\(\);" % (variable, name), code)
        check(typed is not None,
              "and the C# trims %s, so left out, typed blank and typed as spaces "
              "all read the same" % name)

    offset = need(frag, "offset") or {}
    check(not HF.need_may_be_absent(offset),
          "`offset` is still asked for - a copy with no offset is not guessed")
    worksets = need(frag, "toWorkset") or {}
    check(HF.need_absent_text(worksets) == "active",
          "and `toWorkset` left out is still \"active\", as version 3 wrote it")

    # PROVEN AGAIN ON 2026-11-26 (Revit 2020, a copy of rac_basic_sample_project):
    # Group PJ ran the copy with the three left out, and Ajmal PS signed it. Before
    # that the card was DRAFT with the hold declared; either state is right, and
    # PROVEN on a proof of other code - or PROVEN with the hold still on - is not.
    held = str(frag.data.get("proof-held") or "")
    proved_now = (frag.proof or {}).get("fingerprint") == frag.fingerprint()
    signed_after_pj = str((frag.proof or {}).get("date") or "") >= "2026-11-26"
    check(int(frag.data.get("version") or 0) >= 6
          and ((frag.status == "DRAFT" and "PJ" in held)
               or (frag.status == "PROVEN" and proved_now and signed_after_pj
                   and not held)),
          "its contract changed, so at version 6 or later it is DRAFT with the hold "
          "DECLARED naming the PJ run it waits for, or PROVEN on that run's proof of "
          "this code with the hold lifted (it reads %s, version %s, proof-held %r)"
          % (frag.status, frag.data.get("version"), held[:80]))


def main():
    grids()
    copies()
    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1
    print("PASSED - CREATE_GRIDS says what it made in findings and refuses only in")
    print("refused; COPY_ELEMENTS copies by an offset asked with the offset alone.")
    print("COPY_ELEMENTS ran in Revit 2020 (NEEDS-CHECKING Group PJ, 2026-11-26);")
    print("CREATE_GRIDS' Group PI is still owed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
