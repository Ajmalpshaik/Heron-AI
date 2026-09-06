# Heron-Agent:  HERON-FRG-VAL-001
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Step 7 - the fragment on disk, and the validator that will not let it lie.

    python tests/test_fragment_store.py

Runs anywhere. No Revit, no Windows, no database - Step 7 is files and one
validator, and this proves the three things the build order asks it to:

  1. Identity survives the folder being renamed.
  2. Two contracts are checkably composable, as DATA rather than as prose.
  3. A promotion whose proof has no negative case is REFUSED, by name.

WHAT IT DOES NOT PROVE. Nothing about whether either fragment's C# works. They
have never run against a model and are both DRAFT, which is what DRAFT means.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as F                                    # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def scratch(data, name="do-a-test-thing", impl="// code"):
    """A fragment folder in a temp dir, so nothing here touches the library."""
    import yaml
    base = tempfile.mkdtemp(prefix="heron-frg-")
    folder = os.path.join(base, name)
    os.makedirs(os.path.join(folder, "impl", "any"))
    io.open(os.path.join(folder, "fragment.yaml"), "w", encoding="utf-8").write(
        yaml.safe_dump(data, default_flow_style=False, sort_keys=False))
    io.open(os.path.join(folder, "impl", "any", "fragment.cs"), "w",
            encoding="utf-8").write(impl)
    return base, folder


def well_formed(**over):
    data = {
        "heron-agent": "HERON-REVIT-CMP-021",
        "heron-step": 7,
        "heron-status": "DRAFT",
        "heron-since": "0.1.0",
        "heron-layer": "brain",
        "id": "FRG-ELE-001",
        "semantic-identity": "a test fragment",
        "kind": "filter",
        "domain": "test",
        "capability": "DO_A_TEST_THING",
        "version": 1,
        "source": "OFFICIAL",
        "risk": "READ",
        "purpose": "Exists to be validated.",
        "contract": {
            "needs": [{"name": "doc", "type": "Document"}],
            "provides": [{"name": "elements", "type": "IList<Element>"}],
        },
        "revit": ["2020", "2024"],
        "runtime": ["net472", "net48"],
        "utterances": ["do the test thing"],
    }
    data.update(over)
    return data


def main():
    print("The library on disk loads and is well-formed")
    found, problems = F.load_all()
    check(not problems, "no load problems in brain/fragments%s"
          % ("" if not problems else ": " + problems[0]))
    check(len(found) >= 2, "at least the two hand-written fragments are there")
    for frag in found.values():
        broken = F.validate(frag)
        check(not broken, "%s is well-formed%s"
              % (frag.slug, "" if not broken else ": " + broken[0]))

    print()
    print("1. Identity is not the filename")
    base, folder = scratch(well_formed())
    try:
        first = F.load(folder)
        renamed = os.path.join(base, "a-completely-different-name")
        os.rename(folder, renamed)
        second = F.load(renamed)
        check(first.id == second.id == "FRG-ELE-001",
              "renaming the folder does not change the id")
        check(first.slug != second.slug,
              "the folder name did change, so the test is testing something")
        check(second.semantic_identity == "a test fragment",
              "and everything else came with it")
        misfiled = F.naming_problems(second)
        check(any("belongs in a folder called" in p for p in misfiled),
              "and it is reported as MISFILED - still the same fragment, now in "
              "the wrong place. Identity and naming are different questions")
    finally:
        shutil.rmtree(base, ignore_errors=True)

    print()
    print("Two fragments cannot share one identity")
    base_a, folder_a = scratch(well_formed(), name="one")
    try:
        os.makedirs(os.path.join(base_a, "two", "impl", "any"))
        shutil.copy(os.path.join(folder_a, "fragment.yaml"),
                    os.path.join(base_a, "two", "fragment.yaml"))
        _found, dupes = F.load_all(base_a)
        check(any("already used by" in p for p in dupes),
              "a duplicate id is reported, not silently preferred")
    finally:
        shutil.rmtree(base_a, ignore_errors=True)

    print()
    print("2. The contract is data, and composition is checkable")
    by_id = dict((f.id, f) for f in found.values())
    filt = by_id.get("FRG-ELE-001")
    act = by_id.get("FRG-SEL-001")
    check(filt is not None and act is not None,
          "the filter and the action are both loadable by id")
    if filt and act:
        ok, why = F.composable(filt, act)
        check(ok, "filter -> action composes: %s" % why)
        backwards, why_not = F.composable(act, filt)
        check(not backwards,
              "action -> filter does NOT compose, and says why: %s" % why_not)

    print("  ..a need nothing provides is caught")
    base_b, folder_b = scratch(well_formed(
        id="FRG-SEL-002", capability="MAKE_A_CONSUMER",
        contract={"needs": [{"name": "nothingProvidesThis", "type": "int"}],
                  "provides": [{"name": "x", "type": "int"}]}),
        name="make-a-consumer")
    try:
        consumer = F.load(folder_b)
        ok, why = F.composable(filt, consumer) if filt else (False, "no filter")
        check(not ok, "an unmet need is refused: %s" % why)
    finally:
        shutil.rmtree(base_b, ignore_errors=True)

    print("  ..and prose in place of data is refused")
    base_c, folder_c = scratch(well_formed(
        contract={"needs": "a document and a category", "provides": []}))
    try:
        prose = F.load(folder_c)
        broken = F.validate(prose)
        check(any("must be a list" in p for p in broken),
              "a sentence cannot be composed against, and is rejected as such")
    finally:
        shutil.rmtree(base_c, ignore_errors=True)

    print()
    print("3. A proof without a negative case is not a proof (D-30)")
    good_proof = {
        "date": "2026-08-28",
        "by": "Ajmal PS",
        "model": "scratch MEP model, 4 ducts",
        "positive_case": "4 ducts found, matching a manual selection",
        "negative_case": "asked for air terminals, of which there are none, "
                         "and got EMPTY rather than a silent success",
        "second_route": "Revit's own schedule count agreed",
    }
    base_d, folder_d = scratch(well_formed(**{"heron-status": "VALIDATED"}, proof=good_proof))
    try:
        frag = F.load(folder_d)
        frag.data["proof"]["fingerprint"] = frag.fingerprint()
        allowed, why = F.can_promote(frag, "PROVEN")
        check(allowed, "a complete proof promotes to PROVEN: %s" % why)

        del frag.data["proof"]["negative_case"]
        refused, reason = F.can_promote(frag, "PROVEN")
        check(not refused, "removing the negative case REFUSES the promotion")
        check("negative_case" in reason,
              "and the refusal names what is missing: %s" % reason.split(".")[0])
    finally:
        shutil.rmtree(base_d, ignore_errors=True)

    print("  ..a one-word negative case is not one either")
    thin = dict(good_proof)
    thin["negative_case"] = "none"
    base_e, folder_e = scratch(well_formed(**{"heron-status": "VALIDATED"}, proof=thin))
    try:
        frag = F.load(folder_e)
        frag.data["proof"]["fingerprint"] = frag.fingerprint()
        refused, reason = F.can_promote(frag, "PROVEN")
        check(not refused, "'none' is refused as a negative case")
    finally:
        shutil.rmtree(base_e, ignore_errors=True)

    print("  ..and claiming PROVEN with no proof at all fails validation")
    base_f, folder_f = scratch(well_formed(**{"heron-status": "PROVEN"}))
    try:
        broken = F.validate(F.load(folder_f))
        check(any("no proof" in p for p in broken),
              "status cannot be typed into the file to make it true")
    finally:
        shutil.rmtree(base_f, ignore_errors=True)

    print()
    print("A proof goes stale when the code moves under it")
    base_g, folder_g = scratch(well_formed(**{"heron-status": "VALIDATED"}, proof=good_proof),
                               impl="// the code the proof was taken against")
    try:
        frag = F.load(folder_g)
        frag.data["proof"]["fingerprint"] = frag.fingerprint()
        check(not frag.proof_is_stale(), "fresh while the implementation is unchanged")

        io.open(os.path.join(folder_g, "impl", "any", "fragment.cs"), "w",
                encoding="utf-8").write("// somebody edited this afterwards")
        check(frag.proof_is_stale(), "STALE the moment the implementation changes")
        blocked, why = F.can_promote(frag, "PRODUCTION")
        check(not blocked, "and a stale proof cannot promote: %s" % why.split(".")[0])

        frag.data["proof"]["fingerprint"] = None
        check(frag.proof_is_stale(),
              "a proof that never recorded what it was taken against is STALE, "
              "not fresh - unknown is never a pass here")
    finally:
        shutil.rmtree(base_g, ignore_errors=True)

    print()
    print("Names follow the standard, and the checker says so")
    for bad_id, why in [("select-elements-by-category", "a name, not an id"),
                        ("FRG-filter-001", "a lower-case kind in it"),
                        ("FRG-ELE-1", "an unpadded number"),
                        ("FRG-NOSUCHAREA-001", "an area nobody defined")]:
        base_i, folder_i = scratch(well_formed(id=bad_id))
        try:
            broken = F.naming_problems(F.load(folder_i))
            check(bool(broken), "%r is refused - %s" % (bad_id, why))
        finally:
            shutil.rmtree(base_i, ignore_errors=True)

    base_j, folder_j = scratch(well_formed(capability="doATestThing"),
                               name="do-a-test-thing")
    try:
        broken = F.naming_problems(F.load(folder_j))
        check(any("SCREAMING_SNAKE_CASE" in p for p in broken),
              "a camelCase capability is refused")
    finally:
        shutil.rmtree(base_j, ignore_errors=True)

    check(F.folder_for("FILTER_ELEMENTS_BY_CATEGORY") == "filter-elements-by-category",
          "a folder name is derivable from its capability, not invented")

    print()
    print("A fragment nobody can phrase a request for is unfindable")
    base_u, folder_u = scratch(well_formed(utterances=[]))
    try:
        broken = F.validate(F.load(folder_u))
        check(any("utterances" in p for p in broken),
              "an empty utterances list is refused, not allowed as a to-do")
    finally:
        shutil.rmtree(base_u, ignore_errors=True)

    print()
    print("Releases Heron does not know are an error, never a guess (D-05)")
    base_h, folder_h = scratch(well_formed(revit=["2028"]))
    try:
        broken = F.validate(F.load(folder_h))
        check(any("does not know" in p for p in broken),
              "an unlisted release is refused rather than extrapolated")
    finally:
        shutil.rmtree(base_h, ignore_errors=True)

    print()
    print("D-48 - one unreadable fragment costs ONE fragment, never the library")
    base_i = tempfile.mkdtemp()
    try:
        for slug, text in (
            ("aaa-good", "id: FRG-ISO-001\nname: first good one\n"),
            ("bbb-broken", "id: FRG-ISO-002\nname: [unclosed\n  bad: : :\n"),
            ("ccc-good", "id: FRG-ISO-003\nname: second good one\n"),
        ):
            d = os.path.join(base_i, slug)
            os.makedirs(d)
            io.open(os.path.join(d, "fragment.yaml"), "w",
                    encoding="utf-8").write(text)

        # The broken one sorts BETWEEN the two good ones on purpose: a loader
        # that dies on it would take the third with it and leave the first
        # looking like a partial success rather than a failure.
        raised = None
        try:
            found_i, problems_i = F.load_all(base_i)
        except Exception as exc:                              # noqa: BLE001
            raised, found_i, problems_i = exc, {}, []

        check(raised is None,
              "load_all survives a fragment.yaml that cannot be parsed")
        check(sorted(found_i) == ["FRG-ISO-001", "FRG-ISO-003"],
              "both good fragments load, including the one AFTER the broken one")
        check(any("FRG-ISO" not in p and "bbb-broken" in p for p in problems_i),
              "the broken one is named in problems rather than swallowed")
    finally:
        shutil.rmtree(base_i, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - identity survives a rename, contracts compose as data, and")
    print("a proof without a negative case is refused - and one unreadable")
    print("fragment costs one fragment rather than all 343 (D-48).")
    print("It says NOTHING about whether either fragment's C# works: both are")
    print("DRAFT and neither has met a model. See NEEDS-CHECKING.md.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
