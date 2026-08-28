#!/usr/bin/env python3
# Heron-Agent:  HERON-MCP-CFG-004
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
MCP Configuration. The same settings file the add-in reads, read from this side.

WHY THIS IS NOT COSMETIC. The two halves of Heron had two different ideas about
time. The add-in reads `revit.operationTimeoutSeconds` from the config file and
defaults it to 60. The client hardcoded its own deadline at 90. With those two
numbers the design works, and the dispatcher says why:

    Both stay BELOW the client's own deadline, so the bridge is what answers.
    A client that gives up first can only say "no reply", which tells the user
    nothing about what to do next.

But one of those numbers is configurable and the other was a constant. Raise
the add-in's operation timeout to 120 - an entirely reasonable thing to do on a
slow model - and the invariant inverts silently. The client gives up at 90, and
instead of *"Revit started it and is still working; do not repeat the
request"*, the user gets *"no answer"*. The useful message is replaced by the
useless one at exactly the moment it is needed, and nothing anywhere reports
that this has happened.

So the client's deadline is now DERIVED from the add-in's, and the ordering
holds by construction rather than by two files happening to agree.

CONFIGURATION IS A CLOSED SET, mirroring the add-in exactly: a key not declared
here is ignored rather than adopted. This is not tidiness - a settings file is
something a user edits by hand, and honouring an undeclared key means a typo
becomes a silently-active setting.
"""

import io
import os

# The declared keys and their defaults, mirroring HeronConfig.Defaults. A test
# reads the C# and fails if the two lists drift, because two hand-maintained
# copies of one closed set is not a closed set.
DEFAULTS = {
    "bridge.autoConnect": "false",
    "bridge.idleReleaseMinutes": "3",
    "bridge.leaseMinutes": "5",
    "revit.busyTimeoutSeconds": "10",
    "revit.operationTimeoutSeconds": "60",
    "write.enabled": "false",
    "log.retainDays": "14",
}

# How far the client's deadline must sit beyond the add-in's, in seconds.
#
# It only has to cover the pipe and the JSON on the way back, which is
# milliseconds. Thirty seconds is far more than that on purpose: the cost of
# being too generous is that a truly dead bridge takes half a minute longer to
# give up on, and the cost of being too tight is losing the one message that
# tells the user what is actually happening.
CLIENT_MARGIN_S = 30.0


def config_path():
    """
    Where the add-in keeps its settings: %APPDATA%\\Heron\\config\\heron.config.

    HERON_CONFIG overrides it, which is what the tests use - and is also the
    only way to read this on a machine with no %APPDATA% at all, which is
    where a good deal of Heron gets written.
    """
    override = os.environ.get("HERON_CONFIG")
    if override:
        return override

    appdata = os.environ.get("APPDATA")
    if not appdata:
        return None
    return os.path.join(appdata, "Heron", "config", "heron.config")


def load(path=None):
    """
    The settings, as a dict. Missing or unreadable file means the defaults -
    never an error.

    A settings file is the one input guaranteed to be edited by hand, so
    nothing here throws. An unreadable config must not stop Heron answering
    "is Revit connected?", which is the question somebody asks when things are
    already going wrong.
    """
    values = dict(DEFAULTS)

    target = path if path is not None else config_path()
    if not target or not os.path.exists(target):
        return values

    try:
        for raw in io.open(target, encoding="utf-8", errors="replace"):
            line = raw.strip()
            if not line or line.startswith("#"):
                continue
            if "=" not in line:
                continue
            key, _, value = line.partition("=")
            key, value = key.strip(), value.strip()
            # Closed set, exactly as the add-in does it. A key nobody declared
            # is noise or a typo, and adopting it would make a misspelling into
            # a working setting.
            for declared in DEFAULTS:
                if declared.lower() == key.lower():
                    values[declared] = value
                    break
    except (IOError, OSError):
        pass

    return values


def _number(values, key, fallback):
    try:
        return float(values.get(key, DEFAULTS.get(key, fallback)))
    except (TypeError, ValueError):
        return float(fallback)


def truthy(values, key):
    """The add-in's own idea of true: true, 1 or yes, case-insensitively."""
    raw = str(values.get(key, DEFAULTS.get(key, ""))).strip().lower()
    return raw in ("true", "1", "yes")


def operation_timeout(values=None):
    """What the add-in will wait for an operation it has started."""
    values = load() if values is None else values
    return max(1.0, _number(values, "revit.operationTimeoutSeconds", 60))


def busy_timeout(values=None):
    """What the add-in will wait for Revit to pick the request up at all."""
    values = load() if values is None else values
    return max(1.0, _number(values, "revit.busyTimeoutSeconds", 10))


def response_timeout(values=None):
    """
    How long THIS side waits - always beyond the add-in's own deadline.

    Derived, never configured separately. The whole point is that the far side
    is the one that gets to answer, because only it knows whether Revit took
    the request, started it, or never saw it.
    """
    values = load() if values is None else values
    return operation_timeout(values) + CLIENT_MARGIN_S


def writing_enabled(values=None):
    """Whether the add-in will permit a change to the model (D-19)."""
    values = load() if values is None else values
    return truthy(values, "write.enabled")


def problems(values=None):
    """
    Anything wrong with the settings, in the user's terms. Empty when fine.

    Reported rather than corrected. These are the user's own choices in the
    user's own file, and silently overriding one is how somebody spends an
    afternoon wondering why a setting has no effect.
    """
    values = load() if values is None else values
    found = []

    busy = busy_timeout(values)
    operation = operation_timeout(values)

    if busy > operation:
        found.append(
            "revit.busyTimeoutSeconds (%g) is longer than revit.operationTimeoutSeconds (%g). "
            "Heron would wait longer for Revit to START the work than for it to FINISH, so "
            "'Revit is busy' could never be reported. Set the busy timeout lower."
            % (busy, operation))

    if operation + CLIENT_MARGIN_S > 3600:
        found.append(
            "revit.operationTimeoutSeconds (%g) is over an hour. Nothing Heron does takes that "
            "long, and a request that hangs would appear to be working the whole time." % operation)

    return found
