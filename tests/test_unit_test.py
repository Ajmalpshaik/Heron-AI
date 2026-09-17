# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-UNT-011
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The suite sweep - what it must do, asserted.

    python tests/test_unit_test.py

WHAT IT PROVES
  1. SUITES ARE DISCOVERED, NOT LISTED. A suite added to the tree is swept
     without anything being told about it.

  2. EXIT 3 IS WAITING, NOT FAILING, and a waiting suite is never counted
     as a pass. This is the one distinction the agent exists for.

  3. EVERY FAILURE CARRIES THE LAST LINE IT PRINTED, and `sharedReason`
     fires only when they all say the same thing - because "3 failed" is
     the shape a real regression hides in.

  4. IT RUNS AND IT DOES NOT FIX. `ranAnything` is true, `fixedAnything`
     is false, and no suite file is modified by a sweep.

  5. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED. The names
     are READ FROM THE CONTRACT, never typed here - a test that agrees
     only with itself is what let a withdrawn agent keep its contract
     (PROPOSALS F27).

IT SWEEPS A FIXTURE, NOT HERON
-------------------------------
Every case below builds a small tests/ tree in a temporary directory and
points the agent at it. Running Heron's own 196 suites inside one of
Heron's own suites would take ten minutes and would make this test fail
whenever anything else did, which is the opposite of a test.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_unit_test as UNT                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []
CHECKED = []

# What a fixture suite does, by name. The body is what the real thing
# reads: an exit code, and a last line to be carried back.
BODIES = {
    "test_green.py": "print('all good')\nraise SystemExit(0)\n",
    "test_waiting.py": "print('needs the MCP SDK')\nraise SystemExit(3)\n",
    "test_red.py": "print('FAIL the level moved 304.8x')\nraise SystemExit(1)\n",
    "test_red_too.py": "print('FAIL the level moved 304.8x')\nraise SystemExit(1)\n",
    "test_other.py": "print('FAIL something else entirely')\nraise SystemExit(1)\n",
    "test_slow.py": "import time\ntime.sleep(30)\n",
    "test_silent.py": "raise SystemExit(1)\n",
}


def check(condition, what):
    CHECKED.append(what)
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def tree(names):
    """A repository root holding exactly these suites."""
    where = tempfile.mkdtemp(prefix="heron-sweep-")
    os.makedirs(os.path.join(where, "tests"))
    for name in names:
        io.open(os.path.join(where, "tests", name), "w",
                encoding="utf-8").write(BODIES[name])
    return where


def main():
    reached = set()
    made = []

    def sweep(names, **kw):
        where = tree(names)
        made.append(where)
        out = UNT.sweep(root=where, **kw)
        if out.get("refused"):
            reached.add(out["refused"])
        return out, where

    try:
        print("\n1. suites are discovered, not listed")
        out, where = sweep(["test_green.py", "test_waiting.py"])
        check(out["of"] == 2, "two suites in the tree, two swept")
        io.open(os.path.join(where, "tests", "test_extra.py"), "w",
                encoding="utf-8").write("raise SystemExit(0)\n")
        again = UNT.sweep(root=where)
        check(again["of"] == 3,
              "a third appears in the tree and is swept, nothing told")
        check("test_extra.py" in again["ok"], "and it is the new one")

        print("\n2. exit 3 is WAITING, and waiting is not passing")
        out, _ = sweep(["test_green.py", "test_waiting.py"])
        check(out["passed"] is True, "a waiting suite does not fail the sweep")
        check([w["suite"] for w in out["waiting"]] == ["test_waiting.py"],
              "it is listed as waiting")
        check("test_waiting.py" not in out["ok"],
              "and NOT as a pass - it proved nothing either way")
        check(out["waiting"][0]["why"] == "needs the MCP SDK",
              "carrying the reason it printed")
        check(any("unproven" in line for line in out["unjudged"]),
              "and the answer says so unprompted")

        print("\n3. a failure carries its last line, and a shared one is named")
        out, _ = sweep(["test_red.py", "test_red_too.py"])
        check(out["passed"] is False, "a real failure fails the sweep")
        check(out["failed"][0]["why"] == "FAIL the level moved 304.8x",
              "the last line it printed is the reason")
        check(out["sharedReason"] == "FAIL the level moved 304.8x",
              "two failures saying one thing is ONE finding, and it is named")
        check("machine" in out["why"],
              "and the sentence says a shared reason usually means the machine")

        out, _ = sweep(["test_red.py", "test_other.py"])
        check(out["sharedReason"] is None,
              "two failures saying two things share nothing")
        check("2 different reasons" in out["why"],
              "and are reported as two findings, not as a total")

        out, _ = sweep(["test_red.py"])
        check(out["sharedReason"] is None,
              "one failure cannot share a reason with itself")
        check("test_red.py said" in out["why"], "it is quoted instead")

        out, _ = sweep(["test_silent.py"])
        check("not one of them printed a reason" in out["why"]
              or "said: nothing at all" in out["why"],
              "a failure that printed nothing says so rather than being blank")

        print("\n4. it runs, and it fixes nothing")
        names = ["test_red.py"]
        where = tree(names)
        made.append(where)
        before = io.open(os.path.join(where, "tests", "test_red.py"),
                         encoding="utf-8").read()
        out = UNT.sweep(root=where)
        after = io.open(os.path.join(where, "tests", "test_red.py"),
                        encoding="utf-8").read()
        check(out["ranAnything"] is True, "ranAnything is true - it ran them")
        check(out["fixedAnything"] is False, "fixedAnything is false, always")
        check(before == after, "and the failing suite is byte-for-byte unchanged")
        source = io.open(os.path.join(ROOT, "brain", "heron_unit_test.py"),
                         encoding="utf-8").read()
        logic = source.split('"""', 2)[2]
        check(".skip" not in logic and "quarantine" not in logic,
              "nothing in it skips or quarantines a suite")

        print("\n5. every failure the contract declares is reached")
        out, _ = sweep(["test_slow.py"], timeout=1)
        check(out["timedOut"] and out["timedOut"][0]["bound"] == 1,
              "a suite that hangs is TIMED_OUT at its bound")
        check(out["passed"] is False,
              "and a hung suite is not a pass - it is not a failure either")

        empty = tempfile.mkdtemp(prefix="heron-sweep-")
        made.append(empty)
        os.makedirs(os.path.join(empty, "tests"))
        out = UNT.sweep(root=empty)
        reached.add(out.get("refused"))
        check(out["refused"] == "NO_SUITES",
              "a tree with no suites is refused, not reported as passing")

        out = UNT.sweep(root=os.path.join(empty, "nowhere"))
        reached.add(out.get("refused"))
        check(out["refused"] == "NOT_A_ROOT", "a root with no tests/ is refused")

        out, _ = sweep(["test_green.py"], named=["test_missing.py"])
        check(out["refused"] == "NOT_A_SUITE",
              "a named suite that is not there is refused, never dropped")

        out, _ = sweep(["test_green.py"], timeout=0)
        check(out["refused"] == "BAD_TIMEOUT", "a timeout of zero is refused")
        out, _ = sweep(["test_green.py"], timeout="soon")
        check(out["refused"] == "BAD_TIMEOUT", "and so is one that is not a number")

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-DEV-UNT-011.yaml"))
        declared = set(contract["failures"])
        check(declared <= reached,
              "every failure the contract declares is reached by this suite: "
              "missing %s" % (sorted(declared - reached) or "none"))
        check(reached - {None} <= declared,
              "and nothing is refused that the contract does not declare: "
              "extra %s" % (sorted((reached - {None}) - declared) or "none"))

    finally:
        for where in made:
            shutil.rmtree(where, ignore_errors=True)

    print("\n%d checked, %d failed" % (len(CHECKED), len(FAILURES)))
    if FAILURES:
        for one in FAILURES:
            print("  FAILED  %s" % one)
        return 1
    print("\nThe sweep runs, and it does not judge. That is "
          "HERON-DEV-QA-016's.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
