#!/usr/bin/env python3
# Heron-Agent:  HERON-OPS-HLT-004
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
Platform Health. One state per component, and one state overall.

The registry asks for HEALTHY / WARNING / DEGRADED / FAILED across every
component, and the four are only worth having if they mean different things:

    HEALTHY   working as intended. Nothing to say.
    WARNING   working, but something deserves to be noticed.
    DEGRADED  partly working. Something that should be usable is not.
    FAILED    not working. Heron itself is broken, not merely idle.

THE DISTINCTION THAT MATTERS MOST HERE: no Revit connected is **not a
failure**. It is Heron's designed resting state - a Revit that was never
connected is invisible on purpose, because nothing should reach a model the
user did not offer up. Reporting that as FAILED would teach the user to ignore
the word, and then it would be ignored on the day it means something.

So FAILED is reserved for Heron being unable to do its job at all, and "there
is nothing to talk to yet" is a WARNING that says which button to press.

WRITING BEING ENABLED IS A WARNING, and it is meant to be a loud one. While
the write path has never been compiled or run (D-19), a config that permits it
is the single most consequential state this file can report - and the whole
point of a health check is to say so before something goes wrong rather than
afterwards.
"""

HEALTHY = "HEALTHY"
WARNING = "WARNING"
DEGRADED = "DEGRADED"
FAILED = "FAILED"

# Worst wins, and the order is the definition of "worst".
_SEVERITY = {HEALTHY: 0, WARNING: 1, DEGRADED: 2, FAILED: 3}


class Component(object):
    """One named part of Heron, its state, and why."""

    def __init__(self, name, state, detail):
        self.name = name
        self.state = state
        self.detail = detail

    def __repr__(self):
        return "<%s %s>" % (self.name, self.state)


class Health(object):
    """Every component, and the rollup."""

    def __init__(self, components):
        self.components = components

    @property
    def state(self):
        """The worst state among the components. HEALTHY if there are none."""
        if not self.components:
            return HEALTHY
        return max((c.state for c in self.components), key=lambda s: _SEVERITY[s])

    def worst(self):
        """The components actually responsible for the rollup."""
        return [c for c in self.components if c.state == self.state]

    def describe(self):
        """As a person reads it: the rollup, then only what is not healthy."""
        lines = ["Heron: %s" % self.state]

        notable = [c for c in self.components if c.state != HEALTHY]
        if not notable:
            lines.append("  Every component healthy.")
            return "\n".join(lines)

        for c in sorted(notable, key=lambda c: -_SEVERITY[c.state]):
            lines.append("  %-9s %-12s %s" % (c.state, c.name, c.detail))
        return "\n".join(lines)


def assess(live=None, starting=None, mismatched=None, config_problems=None,
           writing=False, protocol_version=None, versions_agree=True,
           discovery_readable=True):
    """
    Build the picture from facts already gathered elsewhere.

    Everything arrives as an argument rather than being discovered here, for
    one reason: it makes this testable without Revit, without a bridge and
    without a config file - which is the only way it was ever going to be
    tested at all on the machine it was written on.
    """
    live = live or []
    starting = starting or []
    mismatched = mismatched or []
    config_problems = config_problems or []

    components = []

    # --- Heron's own settings ---------------------------------------------
    if config_problems:
        components.append(Component(
            "config", WARNING,
            "%d problem(s): %s" % (len(config_problems), config_problems[0])))
    else:
        components.append(Component("config", HEALTHY, "readable, values sane"))

    # --- where sessions announce themselves -------------------------------
    if not discovery_readable:
        # Heron cannot find a Revit even if one is connected. That is Heron
        # broken, not Heron idle, and it is the one thing here that earns
        # FAILED.
        components.append(Component(
            "discovery", FAILED,
            "the folder where Revit sessions announce themselves cannot be read, "
            "so Heron cannot find any Revit at all"))
    else:
        components.append(Component("discovery", HEALTHY, "readable"))

    # --- the bridge -------------------------------------------------------
    if live:
        if starting or mismatched:
            components.append(Component(
                "bridge", DEGRADED,
                "%d session(s) answering, %d still starting, %d on another protocol"
                % (len(live), len(starting), len(mismatched))))
        else:
            components.append(Component(
                "bridge", HEALTHY,
                "%d session(s) answering" % len(live)))
    elif mismatched:
        components.append(Component(
            "bridge", DEGRADED,
            "%d session(s) connected but speaking a different protocol - restart that "
            "Revit to finish updating" % len(mismatched)))
    elif starting:
        components.append(Component(
            "bridge", WARNING,
            "%d Revit(s) running but not answering yet - probably still starting up"
            % len(starting)))
    else:
        # The designed resting state. Not a fault.
        components.append(Component(
            "bridge", WARNING,
            "no Revit connected. Open Revit and press Heron AI > Heron - a Revit that "
            "was never connected is invisible to Heron by design"))

    # --- the two halves understanding each other --------------------------
    if not versions_agree:
        components.append(Component(
            "versions", DEGRADED,
            "the add-in and this client are different versions - half-understanding "
            "each other is how a wrong answer looks exactly like a right one"))
    elif live:
        components.append(Component(
            "versions", HEALTHY,
            "add-in and client agree%s"
            % ("" if protocol_version is None else " on protocol %s" % protocol_version)))

    # --- can Heron change the model right now? ----------------------------
    if writing:
        components.append(Component(
            "write gate", WARNING,
            "write.enabled is TRUE, so Heron is permitted to change the model. The write "
            "path has never been proven against a real Revit (D-19) - set it back to false "
            "unless you are deliberately testing it"))
    else:
        components.append(Component(
            "write gate", HEALTHY,
            "closed - Heron cannot change the model"))

    return Health(components)
