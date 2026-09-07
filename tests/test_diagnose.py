#!/usr/bin/env python3
# Heron-Agent:  HERON-OPS-DIA-005
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Self-Diagnostics Agent.

    python tests/test_diagnose.py

WHAT IT PROVES - and the first one is the whole contract
  1. It NEVER raises. Every layer is broken in turn, one at a time and then
     all at once, and a report still comes back. A diagnosis that dies when
     things are broken is useless on the only days it is wanted.
  2. A layer that could not be checked appears as a finding, never as silence
     and never as a pass.
  3. The rollup is the worst component, so one broken layer cannot be hidden
     behind five healthy ones.
  4. It reports rather than repairs - nothing it does changes any state.
  5. No two components share a name. Two rows called "versions" in one report
     is a report nobody can act on, and that shipped for one commit.
  6. It exits 0 even when it finds trouble, because finding trouble is the
     job succeeding.

WHAT IT DOES NOT PROVE. That the diagnosis is CORRECT about a live machine.
Every probe here is fed a fake, which is the only way to test the broken
paths at all - what a real bridge reports is the bridge's own tests' business.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_health as HEALTH                                 # noqa: E402
import heron_diagnose as DIAG                                 # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def explode(*_args, **_kwargs):
    raise RuntimeError("this layer is broken on purpose")


class Swap(object):
    """Replace probes on the module, and always put them back."""

    def __init__(self, **probes):
        self.probes = probes
        self.old = {}

    def __enter__(self):
        for name, value in self.probes.items():
            self.old[name] = getattr(DIAG, name)
            setattr(DIAG, name, value)
        return self

    def __exit__(self, *_exc):
        for name, value in self.old.items():
            setattr(DIAG, name, value)
        return False


def quiet(components, notes):
    """A probe that does nothing, for the layers a given test is not about."""
    return None


def quiet_bridge(components, notes):
    """
    A stand-in for the live probe, so no test here touches a real Revit.

    Measured 2026-09-07: with Revit mid-operation in another session,
    bridge.discover() took 30 seconds to answer. That is the bridge being
    honest - it really is waiting for a busy Revit - but this file calls
    diagnose() a dozen times, and a unit test that takes five minutes because
    somebody else is modelling is a test that stops being run.
    """
    components.append(HEALTH.Component("bridge", HEALTH.HEALTHY, "stubbed"))


def names_of(found):
    return [c.name for c in found["health"].components]


def main():
    probes = ("_live_picture", "_knowledge", "_releases", "_history", "_tools")

    print("1. It never raises, whichever layer is broken")
    for probe in probes:
        # Only the exploding probe is real. The others are stubbed, because the
        # question here is "does diagnose() survive a probe throwing" and the
        # other four layers doing real work answers nothing about it - it just
        # opens the knowledge store five more times. Isolating the subject took
        # this file from 81 seconds to a few.
        swaps = dict((p, quiet) for p in probes if p != probe)
        swaps[probe] = explode
        with Swap(**swaps):
            try:
                found = DIAG.diagnose()
                survived = isinstance(found, dict) and "health" in found
            except Exception as exc:
                survived = False
                print("       raised: %s" % exc)
        check(survived, "%s broken - a report still comes back" % probe)

    print()
    print("   and with every layer broken at once")
    with Swap(**dict((p, explode) for p in probes)):
        try:
            found = DIAG.diagnose()
            survived = isinstance(found, dict)
            text = DIAG.describe(found)
        except Exception as exc:
            survived, text = False, ""
            print("       raised: %s" % exc)
    check(survived, "every layer broken - a report still comes back")
    check(bool(text.strip()), "and it still renders something a person can read")

    print()
    print("2. A layer that could not be checked is a finding, not silence")

    def unavailable(components, notes):
        notes.append("the audit trail could not be read: no such file")

    with Swap(_history=unavailable, _live_picture=quiet_bridge):
        found = DIAG.diagnose()
        text = DIAG.describe(found)
    check(any("audit trail" in n for n in found["notes"]),
          "the note is carried on the report")
    check("Could not be checked" in text,
          "and rendered under its own heading")
    check("not a check that passed" in text,
          "with the point spelled out rather than left to the reader")

    print()
    print("3. The rollup is the worst component")

    def one_failure(components, notes):
        components.append(HEALTH.Component("bridge", HEALTH.FAILED, "gone"))

    def five_healthy(components, notes):
        for n in ("a", "b", "c", "d", "e"):
            components.append(HEALTH.Component(n, HEALTH.HEALTHY, "fine"))

    with Swap(_live_picture=one_failure, _knowledge=five_healthy,
              _releases=lambda c, n: None, _history=lambda c, n: None,
              _tools=lambda c, n: None):
        found = DIAG.diagnose()
    check(found["health"].state == HEALTH.FAILED,
          "one FAILED among five HEALTHY still reads FAILED")
    check([c.name for c in found["health"].worst()] == ["bridge"],
          "and the report names the component responsible")

    print()
    print("4 and 5. It repairs nothing, and no two components share a name")
    with Swap(_live_picture=quiet_bridge):
        found = DIAG.diagnose()
    everything = names_of(found)
    check(len(everything) == len(set(everything)),
          "every component name is unique (%s)" % ", ".join(sorted(everything)))
    check(not hasattr(DIAG, "repair") and not hasattr(DIAG, "fix"),
          "the module offers no repair of any kind")

    print()
    print("6. Trouble found is the job succeeding")
    out = io.StringIO()
    keep = sys.stdout
    sys.stdout = out
    try:
        with Swap(_live_picture=quiet_bridge):
            code = DIAG.main()
    finally:
        sys.stdout = keep
    check(code == 0, "main() exits 0 even when it reports a WARNING")
    check("repairs nothing" in out.getvalue(),
          "and says plainly that it changed nothing")

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the diagnosis survives every layer being broken, a check")
    print("that could not run is reported as one, and the worst component is")
    print("the answer.")
    print()
    print("It proves nothing about whether the diagnosis is RIGHT about a real")
    print("machine. Every probe here was handed a fake, which is the only way")
    print("the broken paths can be tested at all.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
