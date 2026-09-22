#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A skill whose risk is not a risk is exempt from every risk check.

    python tests/test_skill_routing.py

`tools/check-skill-routing.py` is 770 lines and NO SUITE LOADS IT. Its shared
seam is well held - `classify`, `unsettled`, `words_moved`,
`fingerprint_lines` and `NOTHING_FOUND` are all exercised by
`tests/test_skill_proving.py` and `tests/test_skill_catalog.py`, through
`prove-skill.py` and `generate-skill-catalog.py`, which import them. That
indirection is easy to miss and was missed once here: a first measurement
called this tool "named only in prose", which was wrong.

WHAT NOTHING TOUCHED IS THE TOOL'S OWN HALF - `skills`, `without_the_store`,
`declared_by_fragments`, `margin`, `rung` and the report they feed. This
suite is that half, and it deliberately does NOT re-assert the five verdicts
`test_skill_proving.py` already pins: two copies of a judgement is the thing
this tool's own docstring argues against.

THE HOLE IT FOUND. `classify` decides a crossing by comparing two declared
risks. A skill whose `risk:` is missing, or misspelt, is not on the ladder,
so `rung()` answers -1, `ASKS_A_QUESTION` does not hold, and EVERY risk
branch falls through to "reach" or "miss". Measured 2026-09-22:

    classify("",         "ADMIN",  ...)  ->  miss
    classify("Modifies", "MODIFY", ...)  ->  miss

`heron_skill.load_all()` does not call `heron_skill.validate()` - validation
is a separate function - so such a skill IS routed, and the report's only
heading about unrouted skills says "SKILLS THAT WOULD NOT LOAD", which it is
not one of. The reader gets `CROSSINGS: 0` for a skill whose risk nothing
knows, which is D-52's plausible zero in this tool's own face.

NOT A LIVE FAILURE, and saying so is part of it: all ten skills declare a
risk on the ladder today, and `tests/test_skills.py` calls `validate()`, so
CI would name such a file. What was missing is this tool saying it too,
about the verdicts it is the only one making.

WHAT IT DOES NOT DO. It never calls `heron_brain.lookup`, so it opens no
store and ranks nothing - that half is a sample rather than a measurement
(FRAGMENT-ISSUES row 116) and belongs to the tool, not to a test.
"""

import importlib.util
import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-skill-routing.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has a hyphen in it."""
    try:
        spec = importlib.util.spec_from_file_location("check_skill_routing", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


SKILL = """heron-status: DRAFT
heron-step: 14
heron-since: 0.1.0
heron-layer: brain
id: %(id)s
name: %(id)s
purpose: 'a skill written for a test'
preconditions: ['a model is open']
%(risk)srevit: '2020+'
utterances:
%(said)s
steps:
  - capability: COUNT_ELEMENTS
"""


def skill_file(folder, sid, risk, said):
    body = SKILL % {
        "id": sid,
        "risk": ("risk: %s\n" % risk) if risk is not None else "",
        "said": "\n".join("  - '%s'" % one for one in said),
    }
    io.open(os.path.join(folder, "%s.yaml" % sid), "w", encoding="utf-8",
            newline="\n").write(body)


def fragment_file(folder, fid, capability, risk, said):
    os.makedirs(os.path.join(folder, fid))
    io.open(os.path.join(folder, fid, "fragment.yaml"), "w", encoding="utf-8",
            newline="\n").write(
        "id: %s\ncapability: %s\nrisk: %s\nutterances:\n%s\n"
        % (fid, capability, risk,
           "\n".join("  - '%s'" % one for one in said)))


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    check(tool is not None, "tools/check-skill-routing.py loads")
    if tool is None:
        print()
        print("FAILED - it did not import")
        return 1

    for name in ("classify", "rung", "skills", "without_the_store",
                 "declared_by_fragments", "margin", "fingerprint_lines",
                 "unsettled", "words_moved", "main"):
        check(callable(getattr(tool, name, None)), "and it has %s()" % name)
    check(getattr(tool, "LADDER", None), "and it names the ladder it reads")

    print()
    print("1. A RISK THAT IS NOT ON THE LADDER IS NOT A RISK")
    print("   rung() already says so; the question is whether anything acts")
    print("   on the answer.")
    check(tool.rung("READ") == 0 and tool.rung("ADMIN") == len(tool.LADDER) - 1,
          "the ladder runs READ to ADMIN")
    check(tool.rung("") == -1 and tool.rung("Modifies") == -1,
          "and anything else is -1")

    print()
    print("2. THE TOOL NAMES A SKILL WHOSE DECLARED RISK IS NOT ONE")
    print("   heron_skill.load_all() does NOT call validate(), so such a")
    print("   skill is routed - and every risk branch in classify() falls")
    print("   through for it, which reads as CROSSINGS: 0.")
    home = tempfile.mkdtemp(prefix="heron-skill-routing-")
    try:
        folder = os.path.join(home, "skills")
        os.makedirs(folder)
        skill_file(folder, "good-skill", "READ", ["how many pipes", "count pipes"])
        skill_file(folder, "no-risk-skill", None, ["show the levels", "list levels"])
        skill_file(folder, "bad-risk-skill", "Modifies",
                   ["grey out the ducts", "grey the ducts"])

        import heron_skill as HS
        was_dir = HS.SKILLS_DIR
        try:
            HS.SKILLS_DIR = folder
            answer = tool.skills()
        finally:
            HS.SKILLS_DIR = was_dir

        check(isinstance(answer, tuple) and len(answer) == 3,
              "skills() answers (loaded, problems, unrated), and it answered "
              "%d value(s)" % (len(answer) if isinstance(answer, tuple) else 1))
        if isinstance(answer, tuple) and len(answer) == 3:
            loaded, problems, unrated = answer
            named = sorted(sid for sid, _risk in unrated)
            check(named == ["bad-risk-skill", "no-risk-skill"],
                  "both are named as unrated, and it named %r" % (named,))
            still = sorted(sid for sid, _r, _s, _n in loaded)
            check("good-skill" in still,
                  "and the good one is still routed, %r" % (still,))

        print()
        print("3. THE HOLE ITSELF, so the report above is not the only guard")
        print("   `test_skill_proving.py` owns the five verdicts; this owns")
        print("   only the case it does not reach.")
        check(tool.classify("", "ADMIN", "GRANT_PERMISSION", set()) == "miss",
              "an unrated skill reaching ADMIN is not called a crossing - it "
              "is 'miss', which is why the report has to name it instead")
        check(tool.classify("READ", "ADMIN", "GRANT_PERMISSION", set())
              == "crossing", "while a READ skill reaching ADMIN is one")

        print()
        print("4. What is true before anything is ranked")
        print("   Read from disk, so this half does not move between runs.")
        # declared_by_fragments() joins ROOT with brain/fragments, so the
        # fixture has to be that shape and not merely nearby.
        fragments = os.path.join(home, "brain", "fragments")
        os.makedirs(fragments)
        fragment_file(fragments, "count-elements", "COUNT_ELEMENTS", "READ",
                      ["how many pipes"])
        fragment_file(fragments, "grey-out", "SET_HALFTONE", "MODIFY",
                      ["grey out the ducts"])
        was_root = tool.ROOT
        try:
            tool.ROOT = home
            loaded = [
                ("count-elements", "READ", ["how many pipes", "count pipes"],
                 {"COUNT_ELEMENTS"}),
                ("mep-grayout", "MODIFY", ["grey out the ducts"], {"SET_MEP_SLOPE"}),
                ("other-skill", "READ", ["how many pipes"], {"COUNT_ELEMENTS"}),
            ]
            identity, ranked, collisions = tool.without_the_store(loaded)
        finally:
            tool.ROOT = was_root

        declared = [(sid, phrase, ok) for sid, _sr, phrase, _f, _c, _fr, ok
                    in identity]
        check(("count-elements", "how many pipes", True) in declared,
              "a phrase a fragment also declares resolves by IDENTITY and is "
              "in the skill's plan")
        check(("mep-grayout", "grey out the ducts", False) in declared,
              "and one answered outside the plan is an OUT - a guaranteed "
              "disagreement, not a probable one")
        check([p for _s, _r, p in ranked] == ["count pipes"],
              "a phrase no fragment declares goes to RANKING, and it listed "
              "%r" % ([p for _s, _r, p in ranked],))
        keys = [key for key, _who in collisions]
        check(keys == ["how many pipes"],
              "one sentence claimed by two skills is reported, and it "
              "reported %r" % (keys,))

        print()
        print("5. The margin is what the seam gives, not a number")
        print("   lookup() drops all four scores, so row 109's '2.4 ranks")
        print("   clear' is not reproducible here at all - it was taken by")
        print("   hand.")
        won, runner = tool.margin({"candidates": [
            {"why": "words #1 + nearness #7"}, {"why": "words #4"}]})
        check(won == "words #1 + nearness #7" and runner == "words #4",
              "both ranks come back, and it gave %r / %r" % (won, runner))
        won, runner = tool.margin({})
        check(won == "?" and runner is None,
              "and no candidates is '?' rather than a crash")

        print()
        print("6. A store that could not be read is NOT reported as empty")
        lines = tool.fingerprint_lines({"error": "no HERON_KNOWLEDGE"})
        check(any("no store" in one for one in lines),
              "a missing store says so")
        lines = tool.fingerprint_lines(
            {"path": "p", "md5": "m", "counts_error": "locked"})
        check(any("COULD NOT BE READ" in one for one in lines)
              and any("D-52" in one for one in lines),
              "and counts that could not be read name D-52 rather than zero")
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a skill whose risk is not a risk is named rather than")
    print("silently counted clean.")
    return 0


if __name__ == "__main__":
    sys.path.insert(0, os.path.join(ROOT, "brain"))
    sys.exit(main())
