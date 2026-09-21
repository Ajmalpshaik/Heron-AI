#!/usr/bin/env python3
# Heron-Agent:  HERON-OPS-DIA-005
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
The Self-Diagnostics Agent - *"diagnose Heron"*, in one report.

    python mcp/server/heron_diagnose.py

docs/28: `HERON-OPS-DIA-005` - *"Diagnose Heron" - one clean report.*

WHY ONE REPORT IS THE WHOLE FEATURE
-----------------------------------
Every piece of this already existed and none of it was in one place. Somebody
whose Heron is not working had to know to run `revit_health` for the pipe,
`heron_capabilities` for the knowledge layer, `heron_bridge_client.py doctor`
for a connection failure, and two developer scripts for anything else - and
had to know which of those was even the right question. That is a developer's
troubleshooting flow handed to a BIM modeller, which Golden Rule 1 exists to
prevent.

So this asks every layer once, in the order they depend on each other, and
answers the only question actually being asked: **what is wrong, and what do I
do about it.**

IT COMPOSES, IT DOES NOT REIMPLEMENT
------------------------------------
`heron_health.assess` already builds the live picture and already decides what
"worst" means; this calls it rather than growing a second opinion about
whether a bridge is healthy. The knowledge layer answers through the same seam
the MCP tools use. The version picture comes from the Compatibility Matrix.
Nothing here is a fresh judgement about a component that already has one -
where this file adds a Component of its own, it is for something nobody else
was checking.

EVERY LAYER IS OPTIONAL, AND SAYING SO IS THE POINT
---------------------------------------------------
A diagnosis that cannot run when things are broken is not a diagnosis. Revit
may be shut, PyYAML may be missing, the knowledge store may be unbuildable -
those are the days this is wanted. So every probe is wrapped, a failure
becomes a finding about that layer, and the report never stops early.

WHAT IT WILL NOT DO
-------------------
It repairs nothing. Self-Healing is a different agent (`HERON-OPS-HEA-006`)
and it is not built; a diagnosis that quietly fixed things would make the next
diagnosis a lie about what the machine was like. Every finding says what to
run or change, and a person runs it.
"""

import io
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
for path in (HERE, os.path.join(ROOT, "mcp", "client"), os.path.join(ROOT, "brain")):
    if path not in sys.path:
        sys.path.insert(0, path)

import heron_health as HEALTH                                 # noqa: E402

NEWLINE = chr(10)


def _component(name, state, detail):
    return HEALTH.Component(name, state, detail)


def _live_picture(components, notes):
    """Config, discovery, the bridge, the versions - via the existing assessor."""
    try:
        import heron_bridge_client as bridge
        import heron_config as configuration
    except ImportError as exc:
        components.append(_component(
            "connection", HEALTH.FAILED,
            "Heron cannot load its own bridge client (%s), so it cannot reach "
            "Revit at all. Reinstall Heron." % exc))
        return None

    try:
        live, starting, _stale, mismatched = bridge.discover()
        readable = True
    except Exception as exc:
        live, starting, mismatched, readable = [], [], [], False
        notes.append("the session list could not be read: %s" % exc)

    settings = configuration.load()
    rollup = HEALTH.assess(
        live=live, starting=starting, mismatched=mismatched,
        config_problems=configuration.problems(settings),
        writing=configuration.writing_enabled(settings),
        protocol_version=bridge.PROTOCOL_VERSION,
        versions_agree=not mismatched,
        discovery_readable=readable)
    components.extend(rollup.components)
    return rollup


def _knowledge(components, notes):
    """Can Heron read what it knows - and is there anything in it."""
    try:
        import heron_brain as brain
    except ImportError as exc:
        components.append(_component(
            "knowledge", HEALTH.FAILED,
            "Heron cannot load its knowledge layer (%s)." % exc))
        return None

    try:
        found = brain.catalogue()
    except brain.BrainUnavailable as why:
        # The one failure with an instruction attached, so it is passed
        # through rather than reworded - BrainUnavailable exists to carry it.
        components.append(_component("knowledge", HEALTH.DEGRADED, str(why)))
        return None
    except Exception as exc:
        components.append(_component(
            "knowledge", HEALTH.DEGRADED,
            "the knowledge layer could not be read (%s: %s). The Revit tools "
            "are unaffected." % (type(exc).__name__, exc)))
        return None

    skills = found.get("skills") or []
    ready = [s for s in skills if s.get("ready")]
    if not skills:
        components.append(_component(
            "knowledge", HEALTH.DEGRADED,
            "readable, but Heron knows no jobs by name. Its skills did not load."))
    elif not ready:
        components.append(_component(
            "knowledge", HEALTH.WARNING,
            "%d job(s) known, none with every part provided" % len(skills)))
    else:
        components.append(_component(
            "knowledge", HEALTH.HEALTHY,
            "%d job(s) known, %d ready" % (len(skills), len(ready))))
    return found


def _releases(components, notes):
    """Which Revit releases are supported, and how much of that is measured."""
    try:
        import heron_brain as brain
        found = brain.compatibility()
    except Exception as exc:
        notes.append("the compatibility matrix could not be built: %s" % exc)
        return None

    if not found.get("has_compile_evidence"):
        components.append(_component(
            "releases", HEALTH.WARNING,
            "Heron claims %d Revit release(s), and NOTHING has been checked on "
            "this machine. Run  python tools/check-fragments-compile.py"
            % len(found.get("releases") or [])))
        return found

    proven = [r for r in found["rows"] if r["proven"]]
    if not proven:
        components.append(_component(
            "releases", HEALTH.WARNING,
            "everything builds, and NO release has a tool proved against a "
            "real model yet"))
    else:
        # The finding the matrix was built to surface, said out loud here
        # rather than left to somebody reading a table: a proof lives on the
        # release it was taken on, so eight releases carried by one release's
        # evidence is a fact worth being told without asking.
        names = ", ".join(r["release"] for r in proven)
        state = HEALTH.HEALTHY if len(proven) > 1 else HEALTH.WARNING
        components.append(_component(
            "releases", state,
            "%d release(s) claimed; behaviour proved on %s only"
            % (len(found.get("releases") or []), names)))
    return found


def _history(components, notes):
    """What has actually been failing, from Heron's own trail."""
    try:
        import heron_brain as brain
        found = brain.gaps()
    except Exception as exc:
        notes.append("the audit trail could not be read: %s" % exc)
        return None

    stats = found.get("found") or {}
    if not stats.get("requests"):
        components.append(_component(
            "history", HEALTH.HEALTHY,
            "no requests recorded yet - nothing has gone wrong because nothing "
            "has been asked"))
        return found

    # JUDGED ON THE MOST RECENT DAY, not on the whole trail. This warned "132
    # of 678 failed" for its first hours, and 131 of those were one compile bug
    # on 2026-09-06 that was fixed the same week - 1 defect in 353 runs the day
    # after. A health check reporting a problem that no longer exists is one
    # people learn to skip, and then it is worse than nothing. The all-time
    # figure is still said, as history rather than as an alarm.
    defects = sum(row["count"] for row in found.get("defects") or [])
    recent = found.get("recent") or {}
    day, today, lately = recent.get("day"), recent.get("defects", 0),         recent.get("requests", 0)

    if today:
        components.append(_component(
            "history", HEALTH.WARNING,
            "%d of %d request(s) on %s failed for a reason Heron should have "
            "handled. Ask for the gap report to see which" % (today, lately, day)))
    elif defects:
        components.append(_component(
            "history", HEALTH.HEALTHY,
            "%d of %d request(s) on %s went wrong. %d earlier failure(s) in the "
            "trail, none of them recent" % (today, lately, day, defects)))
    else:
        components.append(_component(
            "history", HEALTH.HEALTHY,
            "%d request(s) recorded, none failed for a reason Heron should "
            "have handled" % stats["requests"]))
    return found


def _backups(components, notes):
    """
    Has the user's own data ever been copied, and how long ago.

    Loaded by path because the tool's filename has hyphens in it and because
    the path logic must stay in ONE place - asking the backup agent where
    backups live beats this file working it out again.

    It reports and never takes one. A diagnosis that quietly started copying
    files would be doing work nobody asked for, at the moment they are already
    dealing with something going wrong.
    """
    import importlib.util
    path = os.path.join(ROOT, "tools", "heron-backup.py")
    spec = importlib.util.spec_from_file_location("heron_backup_tool", path)
    if spec is None or spec.loader is None:
        notes.append("the backup agent could not be loaded from %s" % path)
        return None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)

    found = module.backups()
    if not found:
        components.append(_component(
            "backups", HEALTH.WARNING,
            "none. The audit trail is Heron's only record of what it has done, "
            "it is not in git, and nothing has ever copied it. Run  "
            "python tools/heron-backup.py backup"))
        return []

    newest, path_of_newest, manifest = found[0]
    problems = module.verify(path_of_newest, manifest)
    if problems:
        components.append(_component(
            "backups", HEALTH.WARNING,
            "%d taken, and the newest does not verify: %s"
            % (len(found), problems[0])))
    else:
        components.append(_component(
            "backups", HEALTH.HEALTHY,
            "%d taken, newest %s, %d file(s), verified"
            % (len(found), manifest.get("taken", newest),
               len(manifest.get("files", [])))))
    return found


def _tools(components, notes):
    """
    Is every declared tool actually being offered - and nothing offered that
    nobody declared.

    IT USED TO ASK NEITHER. This docstring has said "is every declared tool
    actually being offered" since it was written, and the body counted the
    table and stopped: the component was HEALTHY whatever the server did, in
    a report whose closing line is "a check that did not run is not a check
    that passed". FRAGMENT-ISSUES section 5b, row 34.

    THE SERVER IS READ AS TEXT, NOT IMPORTED, and that is the whole reason
    this can run on the day it is wanted. Importing heron_mcp_server needs
    the MCP SDK, which is exactly the thing that may be missing when
    somebody types "diagnose Heron" - and a probe that cannot run when the
    machine is broken is not a probe.

    An undeclared tool is the serious direction: risk_of() raises for a name
    nobody declared, so a tool offered without an entry cannot be called at
    all. A declared tool nobody offers is a dead entry - untidy rather than
    dangerous - and both are named.
    """
    try:
        import heron_tools as tools
    except ImportError as exc:
        components.append(_component(
            "tools", HEALTH.FAILED,
            "the tool registry could not be read (%s)" % exc))
        return None

    declared = set(tools.TOOLS)
    changing = sum(1 for name in declared if tools.writes(name))

    server = os.path.join(HERE, "heron_mcp_server.py")
    try:
        import re
        text = io.open(server, encoding="utf-8", errors="replace").read()
        offered = set(re.findall(r"@server\.tool\(\)\s*\ndef\s+([a-z_]+)\s*\(",
                                 text))
    except (IOError, OSError) as exc:
        notes.append("the server's own tool list could not be read: %s" % exc)
        components.append(_component(
            "tools", HEALTH.WARNING,
            "%d declared, %d of them able to change a model - and whether the "
            "server offers them could not be checked" % (len(declared), changing)))
        return tools.TOOLS

    undeclared = sorted(offered - declared)
    unoffered = sorted(declared - offered)

    if undeclared:
        components.append(_component(
            "tools", HEALTH.FAILED,
            "the server offers %d tool(s) that the registry does not declare: "
            "%s. An undeclared tool has no risk level, so Heron refuses it - "
            "it is offered and cannot be called"
            % (len(undeclared), ", ".join(undeclared))))
    elif unoffered:
        components.append(_component(
            "tools", HEALTH.WARNING,
            "%d declared tool(s) are not offered by the server: %s. The entry "
            "is dead rather than dangerous, and one of the two files is wrong "
            "about what Heron can do" % (len(unoffered), ", ".join(unoffered))))
    else:
        components.append(_component(
            "tools", HEALTH.HEALTHY,
            "%d declared and all %d offered, %d of them able to change a model"
            % (len(declared), len(offered), changing)))
    return tools.TOOLS


def _ask(probe, label, components, notes):
    """
    Run one probe. A probe that throws becomes a finding about that layer.

    EACH PROBE ALREADY GUARDS ITS OWN INSIDES, and that was not enough. This
    file's contract is that diagnose() never raises, and the first test written
    against it broke every probe in turn and got an exception each time: the
    guards covered the failures I had thought of, and the call itself covered
    none of the ones I had not. A contract kept only where it was remembered is
    not a contract.

    So the guard is here, at the one place every probe passes through, and the
    inner ones stay because they produce a BETTER message - they know which
    part of their own layer failed. This one only knows which layer.
    """
    try:
        return probe(components, notes)
    except Exception as exc:
        notes.append("%s could not be checked (%s: %s)"
                     % (label, type(exc).__name__, exc))
        components.append(_component(
            label, HEALTH.DEGRADED,
            "this check could not run, so nothing here says whether it is "
            "working"))
        return None


def diagnose():
    """Every layer, asked once. Never raises - that is the whole contract."""
    components, notes = [], []
    live = _ask(_live_picture, "connection", components, notes)
    knowledge = _ask(_knowledge, "knowledge", components, notes)
    releases = _ask(_releases, "releases", components, notes)
    history = _ask(_history, "history", components, notes)
    declared = _ask(_tools, "tools", components, notes)
    copies = _ask(_backups, "backups", components, notes)

    return {
        "health": HEALTH.Health(components),
        "notes": notes,
        "live": live,
        "knowledge": knowledge,
        "releases": releases,
        "history": history,
        "tools": declared,
        "backups": copies,
    }


def describe(found):
    """The report, in the words a modeller would use."""
    health = found["health"]
    lines = [health.describe(), ""]

    healthy = [c for c in health.components if c.state == HEALTH.HEALTHY]
    if healthy:
        lines.append("Working:")
        for c in sorted(healthy, key=lambda c: c.name):
            lines.append("  %-12s %s" % (c.name, c.detail))
        lines.append("")

    if found["notes"]:
        lines.append("Could not be checked:")
        for note in found["notes"]:
            lines.append("  %s" % note)
        lines.append("")
        lines.append("A check that did not run is not a check that passed.")
        lines.append("")

    lines.append("This repairs nothing. Every line above says what to run or "
                 "change; a person runs it.")
    return NEWLINE.join(lines)


def main():
    found = diagnose()
    sys.stdout.write(describe(found) + NEWLINE)
    # Exit 0 whatever the state. A diagnosis reporting trouble has SUCCEEDED -
    # failing here would make "Heron is not connected", an ordinary and
    # expected answer, indistinguishable from "the diagnosis itself broke".
    return 0


if __name__ == "__main__":
    sys.exit(main())
