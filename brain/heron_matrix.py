# Heron-Agent:  HERON-FRG-MTX-009
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Compatibility Matrix Agent - every fragment against every Revit release.

    python brain/heron_matrix.py                 the matrix, by release
    python brain/heron_matrix.py --fragments     one row per fragment
    python brain/heron_matrix.py --release 2020  everything about one release

docs/28: *"Owns the matrix itself - every fragment against every Revit version
AND its .NET runtime, with the status coming from TESTS, NEVER ASSUMPTION. The
backbone of the 2020-to-latest commitment."*

THE WHOLE AGENT IS THAT ONE CAPITALISED PHRASE
----------------------------------------------
Every fragment.yaml already carries `revit: [2020 ... 2027]`. Reading that back
and printing it as a matrix would be worth nothing: it is the CLAIM, and 347 of
the 349 make the identical claim, which is what a default looks like rather
than what a measurement looks like. A matrix built from it would report 100%
support for eight releases and would be believed.

So a cell here holds one of four states, and they are never merged:

    CLAIMED    the fragment.yaml says the release is supported. An assertion.
               Nobody checked. This is the weakest thing in the file.
    COMPILES   a compiler read the fragment against that release's reference
               assemblies and agreed. Evidence about the API SURFACE, and
               nothing else - the contract is kept and the names exist.
    PROVEN     a D-30 proof was recorded against a real model, ON THAT RELEASE.
               Evidence about BEHAVIOUR.
    unknown    nobody has run anything. NOT a failure, and never printed as one.

The gap between column two and column three is the point of the whole thing. A
fragment can compile on all eight releases and be wrong on all eight.

WHERE EACH COLUMN COMES FROM, AND WHY NONE OF IT IS DECLARED HERE
-----------------------------------------------------------------
- The claim: brain/fragments/*/fragment.yaml, via heron_fragment.
- The runtime per release: **Directory.Build.props**, parsed. That file is the
  one place that maps a Revit release to a .NET target, and it is what the
  builds actually obey. Writing the table again here would give this repository
  a second answer to "what runtime is Revit 2025", and the props file already
  warns that a release the table does not list is unsupported rather than
  assumed - a rule a copy would lose.
- The compile result: build/compile-results.json, written by
  tools/check-fragments-compile.py. Absent until somebody runs it, and the
  report says so plainly instead of showing an empty column as a clean one.
- The proof: the fragment's own proof block.

THE RELEASE A PROOF WAS TAKEN ON IS PROSE, NOT A FIELD
------------------------------------------------------
A proof records `model: "Snowdon Towers Sample HVAC (9,641 elements), Revit
2024, session 42676"`. The release is in there, in a sentence, so this agent
reads it with a regular expression and SAYS THAT IT DID. It is the one input
here that is parsed rather than declared, and if PROOF_REQUIRED ever grows a
`revit:` field this should read that instead.

The consequence matters more than the parsing: **a proof on 2024 is evidence
about 2024.** Eight releases are claimed, and a fragment proven once is proven
on one of them. Nothing here spreads a proof sideways.
"""

import io
import os
import re
import sys
import json
import collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as HF                                   # noqa: E402

PROPS = os.path.join(ROOT, "Directory.Build.props")
RESULTS = os.path.join(ROOT, "build", "compile-results.json")

CLAIMED = "CLAIMED"
COMPILES = "COMPILES"
PROVEN = "PROVEN"
UNKNOWN = "unknown"
NOT_CLAIMED = "-"

# Weakest to strongest. A cell only ever moves up this list.
STRENGTH = [NOT_CLAIMED, UNKNOWN, CLAIMED, COMPILES, PROVEN]

# "Revit 2024" inside a proof's free-text model line.
RELEASE_IN_PROSE = re.compile(r"\bRevit\s*(20\d\d)\b", re.I)


# "nobody passed one" and "there is deliberately no evidence" are different
# answers, and None cannot carry both. Defaulting to None made the second
# unsayable: a caller asking for a matrix with NO evidence silently got
# whatever the last compile run had left on disk. A test asserting "with no
# evidence, a claim stays a claim" passed for an hour on that, then failed the
# moment a compile was run - for the right reason, on the wrong input.
UNSET = object()


def runtimes():
    """
    Release -> .NET target, read from Directory.Build.props.

    Parsed rather than copied. The props file carries conditions of three
    shapes - an equality and two ranges - and each is turned into the releases
    it covers. A release the file does not name is deliberately absent from the
    result: the props file says an unlisted version is unsupported rather than
    assumed, and this returning a guess for one would undo that.
    """
    table = {}
    try:
        text = io.open(PROPS, encoding="utf-8").read()
    except (IOError, OSError):
        return table

    for line in text.split("\n"):
        if "HeronTfm" not in line or "Condition" not in line:
            continue
        target = re.search(r">([a-z0-9.\-]+)</HeronTfm>", line)
        if not target:
            continue
        tfm = target.group(1)

        exact = re.search(r"'\$\(RevitVersion\)'\s*==\s*'(20\d\d)'", line)
        if exact:
            table[exact.group(1)] = tfm
            continue

        low = re.search(r"&gt;=\s*'(20\d\d)'", line)
        high = re.search(r"&lt;=\s*'(20\d\d)'", line)
        if low and high:
            for year in range(int(low.group(1)), int(high.group(1)) + 1):
                table[str(year)] = tfm
    return table


def compile_evidence(path=None):
    """
    What the compiler last found, or None if it has never been run here.

    None and "everything passed" are different answers and are never collapsed:
    an empty column printed as a clean one is the exact failure this
    repository keeps having to write about.
    """
    target = path if path is not None else RESULTS
    if not os.path.exists(target):
        return None
    try:
        data = json.loads(io.open(target, encoding="utf-8").read())
    except (ValueError, IOError, OSError):
        return None
    if not isinstance(data, dict) or "versions" not in data:
        return None
    return data


def proof_release(frag):
    """
    Which release a fragment's proof was taken on, read out of its prose.

    Returns (release, None) when one sentence names one release, and
    (None, why) otherwise - no proof, no release named, or more than one named,
    which is ambiguous rather than good news.
    """
    proof = frag.proof
    if not isinstance(proof, dict):
        return None, "no proof"
    model = str(proof.get("model") or "")
    found = sorted(set(RELEASE_IN_PROSE.findall(model)))
    if not found:
        return None, "the proof names no Revit release"
    if len(found) > 1:
        return None, "the proof names more than one release (%s)" % ", ".join(found)
    return found[0], None


def build_matrix(library=None, evidence=UNSET, tfm=None):
    """One cell per fragment per release, plus why each cell says what it does."""
    library = library if library is not None else list(HF.load_all()[0].values())
    tfm = tfm if tfm is not None else runtimes()
    evidence = compile_evidence() if evidence is UNSET else evidence

    releases = sorted(tfm) or sorted(
        set(str(v) for f in library for v in (f.data.get("revit") or [])))

    failed_on = {}
    tested = set()
    if evidence:
        for release, row in (evidence.get("versions") or {}).items():
            tested.add(release)
            failed_on[release] = set(row.get("failed") or [])

    cells = {}
    notes = collections.Counter()
    for frag in library:
        claims = set(str(v) for v in (frag.data.get("revit") or []))
        release, _why = proof_release(frag)
        proven_on = release if frag.status in ("PROVEN", "PRODUCTION") else None

        row = {}
        for rel in releases:
            if rel not in claims:
                row[rel] = NOT_CLAIMED
                continue
            state = CLAIMED
            if rel in tested:
                # A fragment named in `failed` did not compile. One not named,
                # in a release that WAS tested, did.
                state = CLAIMED if frag.id in failed_on.get(rel, ()) else COMPILES
            if proven_on == rel and state == COMPILES:
                state = PROVEN
            row[rel] = state
            notes[state] += 1
        cells[frag.slug] = row

    return {
        "releases": releases,
        "runtimes": tfm,
        "cells": cells,
        "counts": notes,
        "tested": sorted(tested),
        "evidence": evidence,
        "library": len(library),
    }


def report(matrix, out=None):
    write = (out or sys.stdout).write
    releases = matrix["releases"]

    write("COMPATIBILITY MATRIX\n")
    write("=" * 74 + "\n")
    write("%d fragments, %d release(s) from Directory.Build.props\n"
          % (matrix["library"], len(releases)))
    write("\n")

    if not matrix["evidence"]:
        write("NO COMPILE EVIDENCE ON THIS MACHINE.\n")
        write("  Every cell below is a CLAIM out of fragment.yaml and nothing\n")
        write("  more. Run:  python tools/check-fragments-compile.py\n")
        write("  An empty column is not a clean one.\n")
        write("\n")
    else:
        write("Compile evidence: %s, %d fragment(s), release(s) %s\n"
              % (matrix["evidence"].get("at", "?"),
                 matrix["evidence"].get("fragments", 0),
                 ", ".join(matrix["tested"]) or "none"))
        untested = [r for r in releases if r not in matrix["tested"]]
        if untested:
            write("  NOT tested in that run: %s\n" % ", ".join(untested))
        write("\n")

    header = "%-10s %-16s" % ("RELEASE", "RUNTIME")
    for state in (CLAIMED, COMPILES, PROVEN):
        header += " %10s" % state
    write(header + "\n")
    write("-" * len(header) + "\n")

    for rel in releases:
        tally = collections.Counter()
        for row in matrix["cells"].values():
            tally[row.get(rel, NOT_CLAIMED)] += 1
        line = "%-10s %-16s" % (rel, matrix["runtimes"].get(rel, "?"))
        for state in (CLAIMED, COMPILES, PROVEN):
            line += " %10d" % tally[state]
        write(line + "\n")

    write("\n")
    write("CLAIMED   the fragment says so. Nobody checked\n")
    write("COMPILES  a compiler agreed - the API surface, not the behaviour\n")
    write("PROVEN    a D-30 proof was recorded ON THAT RELEASE\n")
    write("\n")
    write("A fragment can compile on all eight releases and be wrong on all\n")
    write("eight. The distance between the last two columns is what this\n")
    write("matrix exists to show, and it does not close by compiling again.\n")


def fragment_rows(matrix, out=None):
    """One line per fragment - the view for 'what is left to do on THIS one'."""
    write = (out or sys.stdout).write
    releases = matrix["releases"]
    short = {NOT_CLAIMED: ".", UNKNOWN: "?", CLAIMED: "c", COMPILES: "O", PROVEN: "P"}

    write("%-34s %s\n" % ("FRAGMENT", " ".join(r[-2:] for r in releases)))
    write("-" * (34 + 3 * len(releases)) + "\n")
    for slug in sorted(matrix["cells"]):
        row = matrix["cells"][slug]
        write("%-34s %s\n"
              % (slug[:34], "  ".join(short[row.get(r, NOT_CLAIMED)] for r in releases)))
    write("\n")
    write(". not claimed   c claimed only   O compiles   P proven on that release\n")


def main(argv):
    matrix = build_matrix()

    if "--release" in argv:
        try:
            wanted = argv[argv.index("--release") + 1]
        except IndexError:
            sys.stderr.write("--release needs a year, e.g. --release 2020\n")
            return 2
        if wanted not in matrix["releases"]:
            sys.stdout.write(
                "Directory.Build.props does not list Revit %s, so Heron does "
                "not claim it.\nIt lists: %s\n"
                % (wanted, ", ".join(matrix["releases"])))
            return 1
        tally = collections.Counter()
        for row in matrix["cells"].values():
            tally[row.get(wanted, NOT_CLAIMED)] += 1
        sys.stdout.write("Revit %s runs on %s\n"
                         % (wanted, matrix["runtimes"].get(wanted, "?")))
        for state in (CLAIMED, COMPILES, PROVEN, NOT_CLAIMED):
            sys.stdout.write("  %-10s %d fragment(s)\n" % (state, tally[state]))
        return 0

    if "--fragments" in argv:
        fragment_rows(matrix)
        return 0

    report(matrix)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
