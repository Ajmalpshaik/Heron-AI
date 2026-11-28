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
  7. ITS WRITE GATE IS WHAT EACH CONNECTED REVIT SAYS, AS revit_health HAS IT
     (row 5b-1744, D-166) - never a file. Stand-in Revits whose `info` gives
     Changes, Admin and Publish, beside a chat's own heron.config that says
     all three are on: Changes ON in a Revit warns, naming it; every Revit
     that said off is closed; none saying is not known; each Revit's switches
     are said in revit_health's own lines; nothing but the lease-exempt
     `info` is sent.

WHAT IT DOES NOT PROVE. That the diagnosis is CORRECT about a live machine.
Every probe here is fed a fake, which is the only way to test the broken
paths at all - what a real bridge reports is the bridge's own tests' business.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real
# trail - and so does the store it opens, which with no HERON_KNOWLEDGE is
# the shared one at the default home (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant(data=True)

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


class Revit(object):
    """A connected Revit whose `info` answers as its add-in would - and that
    answers nothing else, so a diagnosis that sent more would be seen."""

    def __init__(self, pid, release, info):
        self.pid, self.revit_version, self.info = pid, release, info
        self.asked, self.closed = [], False

    def request(self, op, op_args=None, **_ignored):
        self.asked.append(op)
        if op != "info":
            return {"ok": False, "error": "refused_in_test",
                    "message": "this stand-in answers `info` alone"}
        return dict(self.info, ok=True, inUse=False, mine=False,
                    leaseSecondsRemaining=0)

    def close(self):
        self.closed = True


def switches_as_revit_has_them():
    """
    Section 7 - row 5b-1744, D-166. The write gate is what the connected
    Revits said, by the rule revit_health uses: the diagnosis read it from
    this chat's own heron.config, and since D-166 said no Revit had said,
    whatever they said. Every other probe is stubbed: the question is the
    connection's alone.
    """
    print()
    print("7. The write gate is what each Revit says, never a file (row 5b-1744, D-166)")
    import heron_bridge_client as bridge

    home = tempfile.mkdtemp(prefix="heron-diagnose-d166-")
    mine = os.path.join(home, "heron.config")
    had = os.environ.get("HERON_CONFIG")
    os.environ["HERON_CONFIG"] = mine
    # THE CHAT'S OWN COPY SAYS ALL THREE ARE ON, for the whole section. D-166
    # makes such a line inert; a diagnosis that read it would warn below.
    with io.open(mine, "w", encoding="utf-8") as fh:
        fh.write("write.enabled = true\nadmin.enabled = true\npublish.enabled = true\n")
    real = bridge.discover
    others = dict((p, quiet) for p in ("_knowledge", "_releases", "_history",
                                       "_tools", "_backups"))

    def told(changes):
        return {"askToKeep": False, "changes": changes, "admin": False,
                "publish": False}

    def diagnosed(*revits):
        bridge.discover = lambda: (list(revits), [], [], [])
        try:
            with Swap(**others):
                found = DIAG.diagnose()
            return found, DIAG.describe(found)
        finally:
            bridge.discover = real

    def gate(found):
        rows = [c for c in found["health"].components if c.name == "write gate"]
        return rows[0] if rows else HEALTH.Component("write gate", "(none)", "(no row)")

    try:
        hot = Revit(5102, "2025", told(True))
        found, text = diagnosed(hot)
        row = gate(found)
        check(row.state == HEALTH.WARNING and row.detail.startswith(
            "Changes is ON in Revit 2025, session 5102, as that Revit has it"),
            "Changes ON in a Revit: the write gate warns, naming it, as revit_health does "
            "- found %s %r" % (row.state, row.detail[:70]))
        check("Changes ON, Admin off, Publish off in this Revit right now" in text
              and "Ask to Keep is off" in text,
              "and that Revit's switches are said in revit_health's own lines")
        check(hot.asked and set(hot.asked) == set(["info"]) and hot.closed,
              "it was asked through the lease-exempt `info` alone, and let go - %r"
              % (hot.asked,))

        found, text = diagnosed(Revit(5101, "2024", told(False)))
        row = gate(found)
        check(row.state == HEALTH.HEALTHY
              and row.detail.startswith("closed (as Revit 2024, session 5101, has it)")
              and "Changes is ON" not in text,
              "every Revit has it off: closed, naming it, though the chat's own file says "
              "all three are on - found %s %r" % (row.state, row.detail[:60]))

        found, text = diagnosed(Revit(5104, "2020", {"askToKeep": False}))
        row = gate(found)
        check(row.state == HEALTH.HEALTHY and "no connected Revit said" in row.detail
              and "does not report them" in text,
              "only an add-in from before D-166: not known, and its line says it does not "
              "report them - the chat's file is never read for it - found %r"
              % row.detail[:60])

        found, text = diagnosed()
        row = gate(found)
        check(row.state == HEALTH.HEALTHY and "no connected Revit said" in row.detail
              and "Switches, as each" not in text and mine not in text,
              "no Revit connected: not known, no switches said for a Revit that is not "
              "there, and the chat's file named nowhere - found %r" % row.detail[:60])
    finally:
        bridge.discover = real
        if had is None:
            os.environ.pop("HERON_CONFIG", None)
        else:
            os.environ["HERON_CONFIG"] = had
        shutil.rmtree(home, ignore_errors=True)


def installed_heron_names_no_missing_tool():
    """Section 8 (row 5b-2084): an installed Heron - Setup's download or the
    Claude Code plugin - carries no tools/heron-backup.py and no
    tools/check-fragments-compile.py, so the diagnosis names neither there."""
    print()
    print("8. An installed Heron is told to run nothing it does not carry (row 5b-2084)")
    import heron_brain as BRAIN
    import heron_tools as REGISTRY

    # NO EARLY RETURN WHEN THE RULE IS MISSING: the checks below still run,
    # and FAIL, on the code before it (heron-ship 2a).
    helper = getattr(BRAIN, "shipped_tool", None)
    check(helper is not None, "heron_brain has one rule for naming a tool")
    check(helper is not None and helper("check-docs.py") == "python tools/check-docs.py"
          and helper("no-such-tool.py") is None,
          "it names a tool this copy carries, and nothing for one it does not")

    was_root = DIAG.ROOT
    installed = tempfile.mkdtemp(prefix="heron-diagnose-installed-")
    try:
        DIAG.ROOT = installed
        components, notes = [], []
        raised = None
        try:
            got = DIAG._backups(components, notes)
        except Exception as exc:                            # noqa: BLE001 - a raise is the FAIL
            got, raised = None, exc
        check(raised is None,
              "with no backup tool on disk the backups probe does not raise (%s)" % raised)
        check(got is None and not [c for c in components if c.name == "backups"],
              "and reports no WARNING or DEGRADED for a tool that copy never had")
        check(any("not part of an installed Heron" in n for n in notes),
              "a note says why backups were not checked: %s" % notes)
    finally:
        DIAG.ROOT = was_root
        shutil.rmtree(installed, ignore_errors=True)

    was_tool, was_compat = helper, BRAIN.compatibility
    try:
        BRAIN.compatibility = lambda *a, **k: {"has_compile_evidence": False,
                                               "releases": ["2024", "2025"], "rows": []}
        BRAIN.shipped_tool = lambda name: None
        components = []
        DIAG._releases(components, [])
        rows = [c for c in components if c.name == "releases"]
        check(rows and "tools/" not in rows[0].detail
              and "not part of an installed Heron" in rows[0].detail,
              "with no compile check on disk, releases names none: %r"
              % (rows[0].detail if rows else None))
        BRAIN.shipped_tool = was_tool or (lambda name: "python tools/%s" % name)
        components = []
        DIAG._releases(components, [])
        rows = [c for c in components if c.name == "releases"]
        check(rows and "python tools/check-fragments-compile.py" in rows[0].detail,
              "and a checkout, which carries it, is still told to run it")
    finally:
        BRAIN.compatibility = was_compat
        if was_tool is None:
            del BRAIN.shipped_tool
        else:
            BRAIN.shipped_tool = was_tool

    # heron_compatibility is read as TEXT (row 5b-2039): importing the server
    # needs the MCP SDK.
    server = REGISTRY.server_text()
    tool = server[server.find("def heron_compatibility("):]
    tool = tool[:tool.find("\n@server.tool()")]
    check('brain.shipped_tool("check-fragments-compile.py")' in tool
          and '"tools/check-fragments-compile.py to turn' not in tool,
          "heron_compatibility asks the same rule, and no longer types the tool's path")


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
    print("6. The tools probe asks the question its own docstring asks")
    # IT DID NOT. "Is every declared tool actually being offered" was the
    # docstring, and the body counted the table and stopped, so the
    # component was HEALTHY whatever the server did - in a report whose
    # closing line is "a check that did not run is not a check that passed".
    # FRAGMENT-ISSUES section 5b, row 34.
    components, notes = [], []
    DIAG._tools(components, notes)
    row = [c for c in components if c.name == "tools"][0]
    check(row.state == HEALTH.HEALTHY,
          "on this tree the registry and the server agree, so it is HEALTHY")
    check("offered" in row.detail,
          "and it says so in terms of what is OFFERED, not just declared")

    # AND IT NOTICES WHEN THEY DISAGREE. The server is read as TEXT rather
    # than imported - importing it needs the MCP SDK, which is exactly what
    # may be missing on the day somebody asks for a diagnosis - so a
    # different file is all it takes to test the disagreement.
    import heron_tools as REGISTRY
    keep = REGISTRY.TOOLS
    try:
        REGISTRY.TOOLS = dict(keep)
        REGISTRY.TOOLS.pop("revit_health")
        components, notes = [], []
        DIAG._tools(components, notes)
        row = [c for c in components if c.name == "tools"][0]
        check(row.state == HEALTH.FAILED and "revit_health" in row.detail,
              "a tool the server offers and the registry does not declare is "
              "FAILED, and named - it cannot be called at all")

        REGISTRY.TOOLS = dict(keep)
        REGISTRY.TOOLS["revit_invented"] = (REGISTRY.READ, None)
        components, notes = [], []
        DIAG._tools(components, notes)
        row = [c for c in components if c.name == "tools"][0]
        check(row.state == HEALTH.WARNING and "revit_invented" in row.detail,
              "and a declared tool nobody offers is a WARNING - a dead entry "
              "rather than a danger")
    finally:
        REGISTRY.TOOLS = keep

    switches_as_revit_has_them()
    installed_heron_names_no_missing_tool()

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
