# Heron-Agent:  HERON-FRG-MTX-009
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Compatibility Matrix Agent.

    python tests/test_matrix.py

WHAT IT PROVES
  1. The runtime per release is READ from Directory.Build.props, not carried
     here. A second copy of that table is a second answer to "what runtime
     does Revit 2025 need", and the props file is the one the builds obey.
  2. A release the props file does not list is ABSENT rather than guessed.
     The props file says an unlisted version is unsupported; a matrix that
     invented net48 for it would quietly undo that.
  3. A claim is never reported as a compile. With no evidence file, every
     claimed cell stays CLAIMED and the report says so out loud.
  4. Compile evidence promotes a cell, and a fragment named in `failed` is NOT
     promoted - the one case where a wrong answer would read as a pass.
  5. A proof promotes ONE release, the one its prose names. A proof taken on
     2024 leaves 2020 exactly where it was.
  6. A proof naming two releases, or none, promotes nothing. Ambiguous is not
     good news.
  7. A missing or corrupt evidence file reads as "no evidence", never as
     "everything passed".

WHAT IT DOES NOT PROVE. That any fragment works on any release. The matrix
reports what was measured; measuring is somebody else's job, and the gap
between COMPILES and PROVEN is the thing it exists to keep visible.
"""

import io
import os
import sys
import json
import shutil
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as HF                                   # noqa: E402
import heron_matrix as MX                                     # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def frag(slug, fid, revit, status="DRAFT", proof=None):
    """A Fragment in memory - no disk, so nothing here can touch the library."""
    data = {
        "id": fid,
        "capability": "TEST_" + fid.replace("-", "_"),
        "revit": list(revit),
        "heron-status": status,
    }
    if proof is not None:
        data["proof"] = proof
    return HF.Fragment(data, os.path.join("nowhere", slug))


def main():
    print("1 and 2. The runtime table comes from Directory.Build.props")
    table = MX.runtimes()
    check(table.get("2020") == "net472", "2020 reads as net472")
    check(table.get("2024") == "net48", "2024 reads as net48 (a range condition)")
    check(table.get("2025") == "net8.0-windows", "2025 reads as net8.0-windows")
    check(table.get("2027") == "net10.0-windows", "2027 reads as net10.0-windows")
    check("2028" not in table and "2019" not in table,
          "a release the props file does not list is absent, not guessed")

    print()
    print("3. With no evidence, a claim stays a claim")
    library = [frag("a", "FRG-A-001", ["2020", "2024"])]
    matrix = MX.build_matrix(library=library, evidence=None, tfm=table)
    check(matrix["cells"]["a"]["2020"] == MX.CLAIMED,
          "a claimed release with no evidence is CLAIMED")
    check(matrix["cells"]["a"]["2025"] == MX.NOT_CLAIMED,
          "a release the fragment does not claim is blank, not a failure")
    out = io.StringIO()
    MX.report(matrix, out)
    check("NO COMPILE EVIDENCE" in out.getvalue(),
          "and the report says there is no evidence rather than showing a clean column")

    print()
    print("4. Compile evidence promotes - unless the fragment failed")
    evidence = {"at": "2026-09-07T00:00:00Z", "fragments": 2,
                "versions": {"2020": {"ok": False, "claimed_by": 2,
                                      "failed": ["FRG-B-002"]},
                             "2024": {"ok": True, "claimed_by": 2, "failed": []}}}
    library = [frag("a", "FRG-A-001", ["2020", "2024"]),
               frag("b", "FRG-B-002", ["2020", "2024"])]
    matrix = MX.build_matrix(library=library, evidence=evidence, tfm=table)
    check(matrix["cells"]["a"]["2020"] == MX.COMPILES,
          "a fragment not named in `failed` compiled on a tested release")
    check(matrix["cells"]["b"]["2020"] == MX.CLAIMED,
          "the fragment that FAILED is not promoted - it stays CLAIMED")
    check(matrix["cells"]["a"]["2025"] == MX.NOT_CLAIMED,
          "an untested, unclaimed release is still blank")

    print()
    print("5 and 6. A proof promotes ONE release - the one it names")
    proven = frag("c", "FRG-C-003", ["2020", "2024"], status="PROVEN",
                  proof={"model": "Snowdon Towers Sample HVAC (9,641 elements), "
                                  "Revit 2024, session 42676"})
    evidence["versions"]["2020"]["failed"] = []
    matrix = MX.build_matrix(library=[proven], evidence=evidence, tfm=table)
    check(matrix["cells"]["c"]["2024"] == MX.PROVEN,
          "the release named in the proof reads PROVEN")
    check(matrix["cells"]["c"]["2020"] == MX.COMPILES,
          "and 2020 stays at COMPILES - a proof on 2024 is evidence about 2024")

    two = frag("d", "FRG-D-004", ["2020", "2024"], status="PROVEN",
               proof={"model": "one model in Revit 2020 and again in Revit 2024"})
    matrix = MX.build_matrix(library=[two], evidence=evidence, tfm=table)
    check(MX.PROVEN not in matrix["cells"]["d"].values(),
          "a proof naming two releases promotes neither - ambiguous is not good news")

    none = frag("e", "FRG-E-005", ["2024"], status="PROVEN",
                proof={"model": "Snowdon Towers Sample HVAC, 9,641 elements"})
    matrix = MX.build_matrix(library=[none], evidence=evidence, tfm=table)
    check(matrix["cells"]["e"]["2024"] == MX.COMPILES,
          "a proof naming no release promotes nothing")

    print()
    print("7. A missing or corrupt evidence file is not a pass")
    work = tempfile.mkdtemp(prefix="heron-matrix-")
    try:
        missing = os.path.join(work, "not-here.json")
        check(MX.compile_evidence(missing) is None, "a missing file reads as no evidence")

        broken = os.path.join(work, "broken.json")
        io.open(broken, "w", encoding="utf-8").write(u"{ this is not json")
        check(MX.compile_evidence(broken) is None, "a corrupt file reads as no evidence")

        wrong = os.path.join(work, "wrong.json")
        io.open(wrong, "w", encoding="utf-8").write(json.dumps({"hello": "world"}))
        check(MX.compile_evidence(wrong) is None,
              "a file with no versions key reads as no evidence")
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the runtime table is read from the file the builds obey, a")
    print("claim is never printed as a compile, a fragment that failed to")
    print("compile is not promoted, and a proof stays on the one release it")
    print("names.")
    print()
    print("It proves nothing about whether any fragment works on any release.")
    print("The gap between COMPILES and PROVEN is what the matrix reports; it")
    print("is not something the matrix can close.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
