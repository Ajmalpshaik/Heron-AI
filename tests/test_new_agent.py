# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The scaffolder refuses more than it writes - asserted, one refusal at a time.

    python tests/test_new_agent.py

WHAT IT PROVES
  1. AN ID THAT IS NOT IN THE REGISTER IS REFUSED. An agent is in the
     register before it is in the code, never the other way round.

  2. AN AGENT SOMETHING ALREADY CLAIMS IS REFUSED, and the refusal names
     the file that claims it.

  3. A MODULE THAT ALREADY EXISTS IS REFUSED, all three files or none.

  4. A MODULE NAME THAT IS A PREFIX OF AN EXISTING ONE IS REFUSED - the
     rule tests/test_references.py asserts, checked here BEFORE the file
     is written rather than ten minutes later in a full sweep.

  5. NOTHING IS WRITTEN BY ANY OF THEM. Checked by listing the tree
     before and after, not by reading the code.

  6. A --step THAT IS NOT A NUMBER IS REFUSED. `--part` is checked against
     a list and `--module` against a pattern; `--step` was not checked at
     all, and it is the third of the three things this tool puts in a
     header. Measured 2026-09-22: `--step banana` wrote
     `# Heron-Step:   banana` into the module AND the test, printed
     "written" three times and exited 0 - in the tool whose stated reason
     for existing is that "check-metadata.py finds the mistake AFTER the
     work, not before it". check-metadata asks only that the five fields
     are PRESENT, and its step arithmetic is guarded by `.isdigit()`, so
     such a file is quietly left out of the counts rather than named.

  7. WHAT IT WRITES WHEN IT DOES WRITE. Claims 1 to 5 are all refusals, so
     nothing asserted anything about the three files themselves. These
     cases build a tree in a temp folder and point the tool at it - the
     refusals above run against the REAL register on purpose, and a write
     case must not.

  8. THE STUB DOES NOT CLAIM THE AGENT. The module's own docstring argues
     at length that it must not, because agent-count.py reads any commented
     Heron-Agent line in the first 40 lines of a file - including one quoted
     inside a docstring - so an EXAMPLE of the line would raise the built
     count for work nobody has done. Nothing checked that the template it
     writes actually obeys its own argument. A negative result, made
     permanent.

WHY IT EXISTS - PROPOSALS F41
------------------------------
The scaffolder had no suite at all. It derived heron_regression_test.py
for "Regression Test Agent" on 2026-09-17, heron_regression.py had been
there for weeks, and because the derived file did not exist the
already-exists check was happy. The collision surfaced in a 198-suite
sweep ten minutes later.

EVERY CASE BELOW WRITES NOTHING, and that is not a convenience: this
runs inside the real repository, because the register and the module
list are what it is asserting against. Each refusal returns before the
first write, which is the property claim 5 exists to keep true.
"""

import importlib.util
import io
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

spec = importlib.util.spec_from_file_location(
    "new_agent", os.path.join(ROOT, "tools", "new-agent.py"))
NA = importlib.util.module_from_spec(spec)
spec.loader.exec_module(NA)

FAILURES = []
CHECKED = []


def check(condition, what):
    CHECKED.append(what)
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def snapshot():
    """Every file the scaffolder could write, as it stands."""
    seen = set()
    for part in ("brain", "tests", os.path.join("brain", "agents")):
        where = os.path.join(ROOT, part)
        if os.path.isdir(where):
            for name in os.listdir(where):
                seen.add(os.path.join(part, name))
    return seen


def run(argv):
    """The tool's exit code and what it printed. It is never given a TTY."""
    was = sys.stdout
    sys.stdout = said = io.StringIO()
    try:
        code = NA.main(["new-agent.py"] + argv)
    finally:
        sys.stdout = was
    return code, said.getvalue()


def main():
    before = snapshot()
    agents, claims = NA.register()

    print("\n1. an id that is not in the register is refused")
    code, said = run(["HERON-NOPE-XXX-999"])
    check(code == 1, "a made-up id exits 1")
    check("not in docs/28-agent-registry.md" in said,
          "and says the register is where an agent starts")

    print("\n2. an agent something already claims is refused")
    # TYPED ON PURPOSE, AND THE ASYMMETRY IS THE POINT.
    #
    # A fixture that must be BUILT is stable, because the register only moves
    # one way: rows get claimed, not un-claimed. A fixture that must be UNBUILT
    # is the fragile kind - every row closed is one fewer - and this suite lost
    # two of those in a single sitting on 2026-09-17/18: DEV-PRF-015 took
    # section 3's, and DEV-INT-012 took section 4's an hour later. Both are
    # derived now.
    #
    # So: NAME a built agent, DERIVE an unbuilt one.
    code, said = run(["HERON-DEV-QA-016"])
    check(code == 1, "an agent already built exits 1")
    check("already built" in said and "heron_qa.py" in said,
          "and names the file that claims it")

    print("\n3. a module that already exists is refused")
    # Derived from the register rather than typed: the first unbuilt row
    # whose derived module name is already a file. A typed id here would
    # go stale the day somebody builds it.
    exact = next((aid for aid, row in sorted(agents.items())
                  if aid not in claims
                  and os.path.exists(os.path.join(
                      ROOT, "brain",
                      "heron_%s.py" % NA.module_name(row["name"])))), None)
    # THE CASE CAN RUN OUT, AND RUNNING OUT IS NOT A FAILURE.
    #
    # The derivation above already avoids a typed id going stale. What it
    # cannot avoid is the register ceasing to offer ANY row of this shape -
    # and on 2026-09-17 that happened: `HERON-DEV-PRF-015` was the only
    # unbuilt row whose derived module was already a file
    # (`brain/heron_performance.py`), and the owner settled the row onto
    # `tools/measure-brain.py`, so it left the unbuilt list and took the
    # fixture with it.
    #
    # Failing here would report a repository in good order as broken, which
    # is `test_dotnet.py`'s lesson from five days earlier: a suite that cannot
    # tell "the tool is wrong" from "there is nothing here to try the tool on"
    # sends somebody hunting a defect that does not exist. So the absence is
    # NAMED and the section is skipped, and the day a colliding row appears
    # again the check comes back by itself.
    if exact is None:
        print("  ..    SKIPPED - no unbuilt row derives a module that already")
        print("        exists, so there is nothing to refuse. `new-agent.py`'s")
        print("        refusal is unexercised here; section 4 still covers the")
        print("        shadowing half. Not a failure: the register simply has")
        print("        no row of this shape today.")
    else:
        code, said = run([exact, "--part", "brain"])
        check(code == 1, "%s exits 1" % exact)
        check("already exists" in said, "and says so")
        check("worse than none" in said,
              "and says a half-scaffolded agent is worse than none")

    print("\n4. a name that shadows an existing module is refused")
    check(NA.prefix_collisions("brain", "regression_test")
          == ["heron_regression"],
          "heron_regression_test shadows heron_regression - the real case")
    check(NA.prefix_collisions("brain", "qa") == [],
          "a whole name that is nobody's prefix collides with nothing")
    check("heron_buildmatrix" not in NA.prefix_collisions(
              "brain", "buildmatrix"),
          "and a module never collides with itself")
    longer = NA.prefix_collisions("brain", "q")
    check(any(one.startswith("heron_q") for one in longer),
          "it catches the other direction too - a SHORTER new name that "
          "existing ones start with: %s" % (longer or "nothing"))

    shadowing = next((aid for aid, row in sorted(agents.items())
                      if aid not in claims
                      and NA.prefix_collisions(
                          "brain", NA.module_name(row["name"]))), None)
    check(shadowing is not None,
          "and the register still holds a row that would hit it (%s)"
          % shadowing)
    if shadowing:
        code, said = run([shadowing, "--part", "brain"])
        check(code == 1, "%s exits 1" % shadowing)
        check("shadows" in said, "and says what it would shadow")
        check("test_references.py" in said,
              "naming the suite that asserts the rule, not just the rule")
        check("--module" in said,
              "and points at the way forward rather than being a dead end")

    # DERIVED, NOT TYPED - and this line is why section 3 four screens up says
    # so in the first place.
    #
    # It read `HERON-DEV-INT-012` until 2026-09-18, when the owner settled that
    # row onto tests/test_bridge_roundtrip.py. `new-agent.py` then refused it
    # with "already built" instead of "shadows", which is the tool being
    # CORRECT and the fixture being stale - and it is the SECOND row this suite
    # lost in one day, after DEV-PRF-015 took section 3's.
    #
    # The point of the check is that `--module` cannot walk around the shadow
    # rule. Any unbuilt row proves that; which one is an accident of the
    # register on the day. So it asks the register rather than remembering an
    # answer, exactly as the two checks above it already do.
    unbuilt = next((aid for aid in sorted(agents) if aid not in claims), None)
    check(unbuilt is not None,
          "the register still holds an unbuilt row to try this with (%s)"
          % unbuilt)
    if unbuilt:
        code, said = run([unbuilt, "--part", "brain",
                          "--module", "regression_test"])
        check(code == 1 and "shadows" in said,
              "an explicit --module cannot walk around it either")

    print("\n5. nothing was written by any of them")
    after = snapshot()
    check(after == before,
          "the tree is unchanged: %s" % (sorted(after - before) or "nothing "
                                         "added"))

    # ------------------------------------------------------------------
    # 6, 7 and 8 POINT THE TOOL AT A TREE OF ITS OWN. Everything above runs
    # against the real register because the refusals are ABOUT the real
    # register. A case that writes must not, so ROOT and register() are
    # pointed elsewhere and put back.
    # ------------------------------------------------------------------
    home = tempfile.mkdtemp(prefix="heron-new-agent-")
    was_root, was_register = NA.ROOT, NA.register
    try:
        for folder in ("brain/agents", "brain", "tests", "docs"):
            full = os.path.join(home, *folder.split("/"))
            if not os.path.isdir(full):
                os.makedirs(full)
        NA.ROOT = home
        NA.register = lambda: (
            {"HERON-DEV-ARC-003": {"name": "Architecture Agent",
                                   "dept": "Development", "tier": "T2"}}, {})

        def wrote():
            return sorted(os.path.relpath(os.path.join(b, f), home)
                          .replace(os.sep, "/")
                          for b, _d, fs in os.walk(home) for f in fs)

        print("\n6. a --step that is not a number is refused")
        code, said = run(["HERON-DEV-ARC-003", "--step", "banana"])
        check(code == 2, "a non-numeric --step exits 2, and it exited %r" % code)
        check(not wrote(),
              "and nothing is written - it wrote %r" % (wrote(),))
        check("step" in said.lower(),
              "and it says which option, like --part and --module do")

        print("\n7. what it writes when it does write")
        code, said = run(["HERON-DEV-ARC-003", "--step", "15"])
        made = wrote()
        check(code == 0, "a good command exits 0, and it exited %r" % code)
        check(made == ["brain/agents/HERON-DEV-ARC-003.yaml",
                       "brain/heron_architecture.py",
                       "tests/test_architecture.py"],
              "all three files and nothing else - it wrote %r" % (made,))
        module = io.open(os.path.join(home, "brain", "heron_architecture.py"),
                         encoding="utf-8").read()
        header = module.splitlines()[:8]
        check(any(one.strip() == "# Heron-Step:   15" for one in header),
              "the step is the one that was asked for")
        check(any(one.strip() == "# Heron-Layer:  brain" for one in header),
              "and the layer is the one the part maps to")
        check(any(one.strip() == "# Heron-Status: DISCOVERED" for one in header),
              "and the status is DISCOVERED - identity assigned, nothing "
              "proven")

        print("\n8. the stub does not claim the agent")
        for where, text in (("module", module),
                            ("test", io.open(os.path.join(
                                home, "tests", "test_architecture.py"),
                                encoding="utf-8").read())):
            first = text.splitlines()[:40]
            claimed = [one for one in first
                       if re.match(r"^\s*#\s*Heron-Agent\s*:", one)]
            check(claimed == ["# Heron-Agent:  none"],
                  "the %s declares exactly `Heron-Agent: none`, and it "
                  "declares %r" % (where, claimed))
            check(not any(re.match(r"^\s*#\s*Heron-Agent\s*:\s*HERON-", one)
                          for one in first),
                  "and no line in its first 40 reads as a claim on the %s - "
                  "agent-count.py would count it" % where)

        print("\n9. a second run over its own output refuses, all three or none")
        code, said = run(["HERON-DEV-ARC-003", "--step", "15"])
        check(code == 1, "the second run exits 1, and it exited %r" % code)
        check(wrote() == made, "and the tree is exactly as it was")
        check("Nothing was written" in said,
              "and it says so rather than reporting a partial success")
    finally:
        NA.ROOT, NA.register = was_root, was_register
        shutil.rmtree(home, ignore_errors=True)

    check(snapshot() == before,
          "and the real repository is still untouched after all of it")

    print("\n%d checked, %d failed" % (len(CHECKED), len(FAILURES)))
    if FAILURES:
        for one in FAILURES:
            print("  FAILED  %s" % one)
        return 1
    print("\nIt refuses more than it writes, which is the point of it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
