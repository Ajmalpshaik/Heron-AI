# Heron-Agent:  HERON-SKL-VAL-004
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Step 14 - skills, and the capability gaps writing them reveals.

    python tests/test_skills.py

WHAT IT PROVES
  1. Every skill is well-formed, and names CAPABILITIES rather than fragments -
     which is what lets a fragment be replaced without editing a skill.
  2. A skill's capabilities resolve to real providers, or the shortfall is a
     reported GAP rather than a silent failure at run time.
  3. Writing skills first PRODUCES the build queue: what to build next, ordered
     by how many real jobs want it, rather than by guess.

WHAT IT DOES NOT PROVE
  Nothing about whether a skill works. All ten are DRAFT and not one has met a
  model. Phase 2's definition of done is "ten real skills work" and the word
  that is not satisfied here is WORK.
"""

import io
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


def main():
    home = tempfile.mkdtemp(prefix="heron-skills-")
    os.environ["HERON_KNOWLEDGE"] = home

    import heron_skill as SKILL
    import heron_fragment as FRAG
    import heron_scope as SCOPE
    import heron_capability as CAP
    import heron_graph as GRAPH

    try:
        print("1. Every skill is well-formed")
        skills, problems = SKILL.load_all()
        check(not problems, "no load problems%s"
              % ("" if not problems else ": " + problems[0]))
        check(len(skills) >= 10,
              "at least ten skills, which is Phase 2's own target (%d)"
              % len(skills))
        for skill in sorted(skills.values(), key=lambda s: s.id):
            broken = SKILL.validate(skill)
            check(not broken, "%s is well-formed%s"
                  % (skill.id, "" if not broken else ": " + broken[0]))

        print()
        print("2. A skill names CAPABILITIES, never fragments")
        fragments, _ = FRAG.load_all()
        ids = set(fragments)
        for skill in skills.values():
            named = [c for c in skill.needs() if c in ids]
            check(not named,
                  "%s names no fragment id%s"
                  % (skill.id, "" if not named else ": " + ", ".join(named)))
        check(all(c.isupper() for s in skills.values() for c in s.needs()),
              "every requirement is a CAPABILITY name")

        print()
        print("3. Capabilities resolve, or the shortfall is a reported GAP")
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            CAP.rebuild(store)
            for skill in skills.values():
                for capability in skill.needs():
                    GRAPH.skill_needs(store, skill.id, capability)
                    CAP.want(store, capability,
                             "needed by skill %s" % skill.id)

            gaps = dict(CAP.gaps(store))
            covered = [s for s in skills.values()
                       if not any(c in gaps for c in s.needs())]
            check(covered,
                  "%d skill(s) have every capability provided: %s"
                  % (len(covered), ", ".join(sorted(s.id for s in covered))))
            # EVERY capability the ten skills need is now provided - the seven
            # that were missing were written on 2026-08-29. So the real library
            # can no longer demonstrate a gap, and asserting that it has one
            # would be asserting that the work is unfinished.
            #
            # What must still be proven is the MECHANISM: that a capability
            # nobody provides is REPORTED rather than discovered at run time.
            # Tested with a name nothing will ever provide, so this check keeps
            # meaning whatever the library does next.
            check(len(covered) == len(skills),
                  "ALL %d skill(s) have every capability provided" % len(skills))
            CAP.want(store, "NO_SUCH_CAPABILITY_EXISTS",
                     "asked for by this test, and provided by nothing")
            invented = dict(CAP.gaps(store))
            check("NO_SUCH_CAPABILITY_EXISTS" in invented,
                  "a capability nobody provides is REPORTED as a gap - the "
                  "absence IS the finding, with no second list to keep in step")

            for skill in covered:
                for capability in skill.needs():
                    provider = CAP.best_provider(store, capability)
                    check(provider is not None,
                          "%s -> %s resolves to %s"
                          % (skill.id, capability, provider))

            print()
            print("4. Writing skills first PRODUCES the build queue")
            # The queue is EMPTY now, and that is the queue having been worked
            # rather than the mechanism being broken. What is still worth
            # proving is that it would be ORDERED BY REAL DEMAND if it were not
            # empty - so it is built over every capability the skills ask for,
            # which is the same arithmetic the gap queue used.
            demand = {}
            for skill in skills.values():
                for capability in skill.needs():
                    demand.setdefault(capability, []).append(skill.id)
            ordered = sorted(demand.items(), key=lambda kv: (-len(kv[1]), kv[0]))
            outstanding = [(c, w) for c, w in ordered if c in gaps]
            check(not outstanding,
                  "nothing is outstanding - every capability the skills ask "
                  "for has a provider")
            check(ordered and len(ordered[0][1]) >= 2,
                  "and demand is still counted per capability - %s is wanted "
                  "by %d skills, which is what ordered the queue while there "
                  "was one" % (ordered[0][0], len(ordered[0][1])))
            for capability, wanters in ordered[:4]:
                print("        %-28s %d skill(s)" % (capability, len(wanters)))

            print()
            print("5. The graph reaches skills through capabilities")
            impact = GRAPH.impact(store, "FRG-ELE-001")
            affected = [s for s, _c in impact["skills_affected"]]
            check(len(affected) >= 3,
                  "changing the category filter touches %d skill(s), none of "
                  "which named it: %s" % (len(affected),
                                          ", ".join(sorted(set(affected))[:4])))
        finally:
            store.close()

        print()
        print("6. Every skill is DRAFT, and that is not an oversight")
        check(all(s.status == "DRAFT" for s in skills.values()),
              "not one is claimed as proven - none has met a model")
    finally:
        shutil.rmtree(home, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)

    print()
    print("7. D-48 - one unreadable skill costs ONE skill, never the library")
    room = tempfile.mkdtemp(prefix="heron-skills-d48-")
    try:
        io.open(os.path.join(room, "aaa-good.yaml"), "w",
                encoding="utf-8").write("id: SKL-ISO-001\nname: first good\n")
        # A DIRECTORY carrying the .yaml extension. load_all filtered on the
        # extension and never asked whether the entry was a file, so io.open
        # raised PermissionError straight out of the loader.
        os.makedirs(os.path.join(room, "bbb-a-directory.yaml"))
        io.open(os.path.join(room, "ccc-broken.yaml"), "w",
                encoding="utf-8").write("id: SKL-ISO-002\nname: [unclosed\n  x: : :\n")
        io.open(os.path.join(room, "ddd-good.yaml"), "w",
                encoding="utf-8").write("id: SKL-ISO-003\nname: second good\n")

        raised = None
        try:
            found_d, problems_d = SKILL.load_all(room)
        except Exception as exc:                              # noqa: BLE001
            raised, found_d, problems_d = exc, {}, []

        check(raised is None,
              "load_all survives both a directory named .yaml and a bad parse")
        check(sorted(found_d) == ["SKL-ISO-001", "SKL-ISO-003"],
              "both good skills load, including the one AFTER the two failures")
        check(any("not a file" in p for p in problems_d),
              "the directory is named as a problem rather than swallowed")
        check(any("could not be parsed" in p for p in problems_d),
              "the malformed one is named too")
    finally:
        shutil.rmtree(room, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - ten skills, none of which names a fragment, and a build")
    print("queue produced by real jobs rather than by guessing - and one")
    print("unreadable skill costs one skill rather than all ten (D-48).")
    print()
    print("Phase 2's definition of done is 'ten real skills WORK'. Ten are")
    print("written and every one is DRAFT. That last word needs a Revit.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
