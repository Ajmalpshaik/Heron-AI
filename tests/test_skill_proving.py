# Heron-Agent:  HERON-SKL-VAL-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Step 17 - `tools/prove-skill.py`, the skill-level twin of `batch-prove.py`.

    python tests/test_skill_proving.py

WHAT IT PROVES
  1. The word PROVEN cannot come out of it. A skill is proved when a model has
     answered, and this tool never opens one - so the verdict it can reach is
     UNDERSTOOD and no path reaches further.
  2. `classify` gives the register's own cases the register's own words. It is
     IMPORTED by `prove-skill.py` from `check-skill-routing.py` rather than
     copied, and this pins it so a later edit to one tool cannot quietly change
     what the other calls a crossing.
  3. The plan half names the four things that block a skill before a model is
     worth opening, each on a skill built to have exactly that fault.
  4. The emitted job file is a file `batch-prove.py` can actually read: valid
     YAML, every fragment real, every setup step real, and `keep-chain` present
     exactly where a need cannot come from the setup chain.

WHAT IT DOES NOT PROVE
  Nothing about whether a skill works, and nothing about the routing half -
  that asks the live store 43 times and takes twenty-five minutes, which is not
  a gate. `tools/check-skill-routing.py` is where that lives.

  IT DOES NOT ASSERT THE FOUR OUTRANKED DECLARATIONS ARE STILL THERE.
  FRAGMENT-ISSUES row 140 is a finding about the library, and the library is
  meant to be fixed. A test that failed when somebody repaired a skill would be
  a test arguing for the defect.
"""

import io
import os
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def fake_skill(risk, needs, utterances=("one", "two")):
    """A Skill object built in memory, never on disk.

    Built rather than written to a temp folder because these are cases the
    library does not have and must not be given: a skill needing a capability
    nothing provides is a defect, and a fixture that created one would be a
    defect somebody later finds and "fixes".
    """
    import heron_skill as SKILL
    return SKILL.Skill({
        "id": "fixture", "name": "fixture", "risk": risk, "needs": list(needs),
        "utterances": list(utterances), "purpose": "a fixture",
    }, "fixture.yaml")


def main():
    # The brain refuses to do anything without somewhere to keep knowledge, and
    # a container has no %APPDATA%. Nothing here reads the store - the plan half
    # is read from disk on purpose - but importing the brain still asks.
    os.environ.setdefault("HERON_KNOWLEDGE",
                          tempfile.mkdtemp(prefix="heron-skillproof-"))

    import yaml
    import heron_fragment as HF
    import heron_skill as SKILL

    sys.path.insert(0, os.path.join(ROOT, "tools"))
    import importlib.util
    spec = importlib.util.spec_from_file_location(
        "prove_skill", os.path.join(ROOT, "tools", "prove-skill.py"))
    PS = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(PS)

    by_slug, by_capability, unreadable = PS.library()
    check(not unreadable, "every fragment loads%s"
          % ("" if not unreadable else ": " + unreadable[0]))
    threshold_name, threshold_ordinal, ladder = PS.GJ.write_threshold()
    chain = PS.GJ.chain_provides(by_slug)

    print("1. PROVEN cannot come out of this tool")
    source = io.open(os.path.join(ROOT, "tools", "prove-skill.py"),
                     encoding="utf-8").read()
    words = set([PS.UNDERSTOOD, PS.BLOCKED, PS.NOT_UNDERSTOOD, PS.PLAN_OK,
                 PS.CROSSING])
    check("PROVEN" not in words,
          "the verdict vocabulary does not contain the word (%s)"
          % ", ".join(sorted(words)))
    # The verdict function's returns, read out of the code rather than trusted.
    body = source[source.index("def verdict("):source.index("def main():")]
    returns = [line.strip() for line in body.splitlines()
               if line.strip().startswith("return ")]
    verdicts = [line for line in returns
                if any(w in line for w in ("CROSSING", "BLOCKED", "PLAN_OK",
                                           "NOT_UNDERSTOOD", "UNDERSTOOD"))]
    check(len(verdicts) == 5,
          "verdict() reaches exactly five verdicts, each a named constant (%d)"
          % len(verdicts))
    check(not [line for line in returns
               if line not in verdicts and "said(" not in line
               and not line.startswith('return "%s')],
          "and no return in verdict() hands back a word built on the spot")

    # A CROSSING IS NEVER HIDDEN BEHIND A BOOKKEEPING BLOCKER. This is the
    # bug the first version of verdict() had: a skill resting on a DRAFT
    # fragment printed four reasons about that and never printed that its
    # question resolves to a write.
    both = fake_skill("ANALYZE", ["A_CAPABILITY_NOBODY_PROVIDES"])
    word, why = PS.verdict(
        PS.Plan(both, by_capability, chain, ladder),
        [("check the connections", "MIRROR_ELEMENTS", "MODIFY", "hybrid",
          "crossing")])
    check(word == PS.CROSSING,
          "a crossing outranks a plan blocker for the verdict word")
    check(any("MIRROR_ELEMENTS" in line for line in why)
          and any("has no provider" in line for line in why),
          "and BOTH halves are still reported - neither hides the other")
    check('IN_FRONT_OF_A_MODEL = ("PROVEN", "PRODUCTION")' in source,
          "PROVEN appears only as a FRAGMENT status it reads, never as a "
          "verdict it writes")

    print()
    print("2. classify() gives the register's cases the register's words")
    # FRAGMENT-ISSUES row 133, number 2, verbatim: a skill at ANALYZE whose
    # words reached UNGROUP_ELEMENTS, a MODIFY. The one the owner called
    # "the exact opposite of a diagnosis".
    check(PS.ROUTING.classify("ANALYZE", "MODIFY", "UNGROUP_ELEMENTS", set())
          == "crossing",
          "a question answered by a write is a crossing")
    check(PS.ROUTING.classify("MODIFY", "MODIFY", "PLACE_FAMILY_INSTANCES",
                              set(["PLACE_FAMILY_INSTANCES"])) == "reach",
          "row 132's limit is preserved: a write skill declaring a write is "
          "scored a REACH, and only the library repairs that")
    # Row 132 again: the rule that was too wide once and was narrowed.
    check(PS.ROUTING.classify("READ", "ANALYZE", "DESCRIBE_BLANK_PARAMETERS",
                              set(["DESCRIBE_BLANK_PARAMETERS"])) == "reach",
          "ANALYZE above READ is NOT an escalation - docs/12 gives ANALYZE no "
          "side effects, and the first version of that rule flagged the one "
          "case the register calls correct")
    check(PS.ROUTING.classify("EXECUTE", "MODIFY", "HIGHLIGHT_VS_REST", set())
          == "escalation",
          "`highlight them` at EXECUTE reaching a MODIFY is an escalation, "
          "which is the case the first run of the sweep buried in a list of 21")
    check(PS.ROUTING.classify("READ", "READ", "READ_MEP_SYSTEM", set())
          == "miss",
          "a read answering a read outside the plan is a miss, not a defect")

    print()
    print("3. The plan half names each blocker on a skill built to have it")
    nothing = fake_skill("READ", ["A_CAPABILITY_NOBODY_PROVIDES"])
    plan = PS.Plan(nothing, by_capability, chain, ladder)
    check(any("has no provider at all" in b for b in plan.blockers()),
          "a capability nothing provides is named as the gap it is")

    # An UNPROVEN provider. Read out of the library rather than invented, so
    # this test cannot pass against a library where no such fragment exists.
    draft = [c for c, rows in by_capability.items()
             if rows[0].status not in PS.IN_FRONT_OF_A_MODEL]
    check(bool(draft), "the library has at least one DRAFT provider to test on")
    if draft:
        resting = fake_skill("MODIFY", [sorted(draft)[0]])
        blocking = PS.Plan(resting, by_capability, chain, ladder).blockers()
        check(any("never been in front of a model" in b for b in blocking),
              "a skill resting on a DRAFT provider is blocked, because a skill "
              "cannot be proved above what it rests on")

    # ROW 140. A skill at READ declaring a capability whose provider is MODIFY.
    writing = [c for c, rows in by_capability.items()
               if (rows[0].data.get("risk") or "").upper() == "MODIFY"]
    check(bool(writing), "the library has a MODIFY capability to test on")
    if writing:
        understated = fake_skill("READ", [sorted(writing)[0]])
        blocking = PS.Plan(understated, by_capability, chain, ladder).blockers()
        check(any("outranks the skill" in b for b in blocking),
              "a capability above the skill's own declared risk is named - "
              "the check docs/28 gives HERON-SKL-VAL-004 and heron_skill "
              "never made (row 140)")
    settled = fake_skill("MODIFY", sorted(writing)[:1])
    check(not any("outranks" in b for b in
                  PS.Plan(settled, by_capability, chain,
                          ladder).blockers()),
          "and a skill whose declared risk covers its plan is NOT reported - "
          "the rule is a comparison, not a dislike of writes")

    print()
    print("4. The order composes, and what cannot be fed is said so")
    found, problems = SKILL.load_all()
    check(not problems, "every real skill loads%s"
          % ("" if not problems else ": " + problems[0]))
    real = [PS.Plan(s, by_capability, chain, ladder)
            for s in sorted(found.values(), key=lambda s: s.data.get("id"))]
    for plan in real:
        supply = dict(chain)
        ordered = True
        for frag in plan.steps:
            for need in frag.needs():
                if HF.need_source(need) != "fragment":
                    continue
                if supply.get(HF.need_binds(need)) != need.get("type"):
                    ordered = False
            for entry in frag.provides():
                supply[entry.get("name")] = entry.get("type")
        check(ordered, "%s's steps are in an order where every one can bind"
              % plan.id)
    check(all(f not in plan.steps for plan in real for f in plan.stuck),
          "a step that cannot be fed is reported STUCK, never quietly ordered")
    check(sum(len(p.steps) + len(p.stuck) for p in real)
          == sum(len(p.providers) for p in real),
          "every provided capability is either a step or a stuck one - none "
          "is dropped between the two lists")

    print()
    print("5. The emitted job file is one batch-prove can read")
    for plan in real:
        text = PS.job_file(plan, by_slug, chain, threshold_ordinal,
                           threshold_name, ladder, None)
        try:
            doc = yaml.safe_load(text)
        except yaml.YAMLError as why:
            check(False, "%s's job file parses: %s" % (plan.id, why))
            continue
        rows = doc.get("jobs") or []
        check(len(rows) == len(plan.steps),
              "%s emits one job per step (%d of %d)"
              % (plan.id, len(rows), len(plan.steps)))
        named = [r.get("fragment") for r in rows]
        check(all(n in by_slug for n in named),
              "%s names only fragments that exist" % plan.id)
        steps = [s for r in rows for s in (r.get("setup") or [])]
        steps += (doc.get("defaults") or {}).get("setup") or []
        check(all(s in by_slug for s in steps),
              "%s's setup steps all exist" % plan.id)

        # `keep-chain` EXACTLY where a need cannot come from the setup chain.
        # Both directions: missing it is defect row 11 arriving again, and
        # adding it where it is not wanted puts the chain above the selection.
        for row, frag in zip(rows, plan.steps):
            owed = [n for n in frag.needs()
                    if HF.need_source(n) == "fragment"
                    and chain.get(HF.need_binds(n)) != n.get("type")]
            check(bool(row.get("keep-chain")) == bool(owed),
                  "%s/%s carries keep-chain exactly when it needs one"
                  % (plan.id, frag.slug))
            if owed:
                check(bool(row.get("setup")),
                      "%s/%s carries the earlier steps that fill it"
                      % (plan.id, frag.slug))

        check("FILL IN" in text,
              "%s leaves the judgement blank and marks it" % plan.id)
        # A need meaning ONE PARTICULAR element must never carry the hint
        # `how_to_type` writes for an element TYPE. A typed name resolves to a
        # type, the fragment runs on it and returns 0 with no error - the
        # shape that cost twelve jobs on 2026-09-10.
        for slug, name, _kind, _why in plan.by_hand:
            for line in text.splitlines():
                if line.strip().startswith(name + ":") and "#" in line:
                    check("element TYPE by name" not in line,
                          "%s/%s's `%s` is not described as a type"
                          % (plan.id, slug, name))
                    check("`selected`" in line,
                          "%s/%s's `%s` says to type `selected`"
                          % (plan.id, slug, name))
        check(str(doc.get("model", "")).startswith("FILL IN"),
              "%s does not name a model it has never seen" % plan.id)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the tool measures the half that can be measured without a")
    print("Revit, refuses to say PROVEN about the half that cannot, and hands")
    print("that half over as a file the proving machine can run.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
