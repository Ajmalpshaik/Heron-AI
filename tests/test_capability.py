# Heron-Agent:  HERON-KRN-CAP-008
# Heron-Step:   12
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Step 12 - the Capability Registry.

    python tests/test_capability.py

WHAT IT PROVES - and the first two are the build order's own acceptance test
  1. Add a SECOND provider of a capability and NO CALL SITE CHANGES.
  2. Remove the FIRST provider and the request still resolves.
  3. A capability with no provider IS the gap - no separate report.
  4. Risk is DERIVED from providers, and providers that disagree about it are
     reported as a defect rather than reconciled quietly.
  5. Platform support is an INTERSECTION - the capability claims a release only
     if every way of doing it works there.
"""

import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def add(store, fid, capability, status="DRAFT", risk="READ",
        revit="2020,2024", kind="filter", domain="test"):
    store.execute(
        "INSERT OR REPLACE INTO fragments (id, capability, semantic_identity, "
        "kind, status, domain, risk, folder, revit) VALUES (?,?,?,?,?,?,?,?,?)",
        (fid, capability, "does %s" % capability, kind, status, domain,
         risk, "x", revit))
    store.db.commit()


def main():
    home = tempfile.mkdtemp(prefix="heron-cap-")
    os.environ["HERON_KNOWLEDGE"] = home

    import heron_scope as SCOPE
    import heron_capability as CAP

    try:
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            CAP.rebuild(store)

            print("The real library resolves")
            got = CAP.resolve(store, "FILTER_ELEMENTS_BY_CATEGORY")
            check(got is not None and got.providers == ["FRG-ELE-001"],
                  "FILTER_ELEMENTS_BY_CATEGORY -> %s"
                  % (got.providers if got else "nothing"))
            check(got.risk == "READ", "its risk is READ, derived from the fragment")

            print()
            print("1. A SECOND provider changes no call site")
            # This is the call site. It is written once and never edited again.
            def call_site():
                return CAP.best_provider(store, "FILTER_ELEMENTS_BY_CATEGORY")

            # THE FIXTURE NOW SAYS WHAT IT MEANS. This block's claim is that
            # TRUST decides, and it needs a DRAFT original and a PROVEN
            # newcomer. It used to get the DRAFT half BY ACCIDENT, from
            # whatever status FRG-ELE-001 happened to ship with - and on
            # 2026-09-13 that fragment was proved against a real model and
            # promoted, so both providers were PROVEN, the newcomer outranked
            # nothing, and this check failed against a library that was
            # correct. Same shape as test_graph and test_reachable on
            # 2026-09-12. The store here is a throwaway in a temp directory,
            # so setting the status is a fixture and touches no fragment on
            # disk - and the claim below is unchanged and still fails if
            # trust stops deciding.
            store.execute("UPDATE fragments SET status = 'DRAFT' "
                          "WHERE id = 'FRG-ELE-001'")
            store.db.commit()
            CAP.rebuild(store)

            before = call_site()
            add(store, "FRG-ELE-777", "FILTER_ELEMENTS_BY_CATEGORY",
                status="PROVEN", risk="READ", domain="revit.elements")
            CAP.rebuild(store)
            after = call_site()

            check(before == "FRG-ELE-001", "before, it resolved to the original")
            check(after == "FRG-ELE-777",
                  "after, it resolves to the PROVEN one - trust decides, and "
                  "the call site is the same line of code")
            got = CAP.resolve(store, "FILTER_ELEMENTS_BY_CATEGORY")
            check(len(got.providers) == 2, "both are known, in trust order: %s"
                  % ", ".join(got.providers))

            print()
            print("2. Remove the first provider and it still resolves")
            store.execute("DELETE FROM fragments WHERE id = 'FRG-ELE-001'")
            store.db.commit()
            check(call_site() == "FRG-ELE-777",
                  "the same call site answers, with the survivor")

            print()
            print("3. A capability with no provider IS the gap")
            CAP.want(store, "TRACE_DUCT_SYSTEM", "a skill needs it")
            missing = CAP.gaps(store)
            check(any(n == "TRACE_DUCT_SYSTEM" for n, _w in missing),
                  "something wanted and unprovided is reported as a gap")
            check(CAP.resolve(store, "TRACE_DUCT_SYSTEM") is None,
                  "and resolving it returns nothing rather than a near miss")

            add(store, "FRG-MEP-900", "TRACE_DUCT_SYSTEM", domain="revit.mep")
            CAP.rebuild(store)
            missing = CAP.gaps(store)
            check(not any(n == "TRACE_DUCT_SYSTEM" for n, _w in missing),
                  "build a provider and the gap closes by itself - no list to "
                  "keep in step")

            print()
            print("4. Risk is derived, and disagreement is a DEFECT")
            add(store, "FRG-SEL-901", "MOVE_THINGS", risk="MODIFY",
                kind="action", domain="revit.geo")
            CAP.rebuild(store)
            check(CAP.resolve(store, "MOVE_THINGS").risk == "MODIFY",
                  "one MODIFY provider makes the capability MODIFY")

            add(store, "FRG-SEL-902", "MOVE_THINGS", risk="READ",
                kind="action", domain="revit.geo")
            CAP.rebuild(store)
            check(CAP.resolve(store, "MOVE_THINGS").risk == "MODIFY",
                  "adding a READ provider does NOT soften it - the highest "
                  "risk wins, because the caller must plan for the worst")
            wrong = CAP.problems(store)
            check(any("DIFFERENT risks" in p for p in wrong),
                  "and the disagreement is REPORTED: %s"
                  % [p for p in wrong if "DIFFERENT" in p][0][:70])

            print()
            print("  ..providers of different SHAPES are a defect too")
            add(store, "FRG-QA-903", "TRACE_DUCT_SYSTEM", kind="action",
                domain="revit.mep")
            CAP.rebuild(store)
            wrong = CAP.problems(store)
            check(any("One capability, one" in p for p in wrong),
                  "a filter and an action cannot be one capability - a caller "
                  "composing against it cannot know which it gets")

            print()
            print("5. Platform support is an INTERSECTION, not a union")
            add(store, "FRG-VIEW-910", "SHOW_A_THING", revit="2020,2024",
                domain="revit.view")
            CAP.rebuild(store)
            check(CAP.resolve(store, "SHOW_A_THING").revit == ["2020", "2024"],
                  "one provider: what it supports")

            add(store, "FRG-VIEW-911", "SHOW_A_THING", revit="2024,2027",
                domain="revit.view")
            CAP.rebuild(store)
            both = CAP.resolve(store, "SHOW_A_THING").revit
            check(both == ["2024"],
                  "two providers: only what BOTH support (%s). A union would "
                  "claim 2027 and then hand back a provider that fails there"
                  % ", ".join(both))

            print()
            print("6. The version filter reaches resolution too")
            only_2027 = CAP.resolve(store, "SHOW_A_THING", revit="2027")
            check(only_2027 is not None and
                  only_2027.providers == ["FRG-VIEW-911"],
                  "asking on 2027 returns only the provider that supports it")
            none_2021 = CAP.resolve(store, "SHOW_A_THING", revit="2021")
            check(none_2021 is None,
                  "and on 2021, where neither works, it returns nothing "
                  "rather than a provider that would fail")
        finally:
            store.close()
    finally:
        shutil.rmtree(home, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - providers can be added, replaced and removed without a")
    print("call site changing, and a capability nobody provides is the gap.")
    print()
    print("It proves nothing about whether a provider WORKS. Resolution")
    print("returning a fragment and that fragment doing the job are separate")
    print("claims, and only the first is tested here.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
