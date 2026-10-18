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
    # The extra wait for a job that has put a Keep or Discard box up: a DRAFT's
    # one run (D-119), or a keep from a chat while the owner asks (D-126).
    "revit.askTimeoutSeconds": "300",
    "write.enabled": "false",
    # The two switches above Changes (D-106): each lets one more level
    # through, and only while write.enabled is on as well.
    "admin.enabled": "false",
    "publish.enabled": "false",
    # D-126: Revit asks the modeller Keep or Discard before a change is kept.
    # Off by default since D-138 (the owner, 2026-10-18); the ribbon's Ask to
    # Keep turns it on. Read by the add-in; this side reads it only to know
    # how long a change it keeps may take to answer.
    "write.askBeforeKeeping": "false",
    "log.retainDays": "14",
    # How long each write's run record stays on disk (HeronRuns), pruned by
    # the add-in at start-up - DERIVED, like the log.
    "runs.retainDays": "14",
    "ui.activityBanner": "true",
    # The Companion page (D-108, D-109): on by default, switched from the
    # Companion button's arrow in Revit. Off leaves Heron itself untouched.
    "companion.enabled": "true",
    # Roslyn compiled once on a background thread while Revit starts, so the
    # first fragment does not pay its start-up (RevitFragment.WarmUp).
    "fragments.warmUp": "true",
}

# How far the client's deadline must sit beyond the add-in's, in seconds.
#
# It only has to cover the pipe and the JSON on the way back, which is
# milliseconds. Twenty seconds is far more than that on purpose: the cost of
# being too generous is that a truly dead bridge takes longer to give up on,
# and the cost of being too tight is losing the one message that tells the
# user what is actually happening.
#
# IT WAS THIRTY, OVER THE OPERATION TIMEOUT ALONE - and the add-in's longest
# answer is not the operation timeout. It waits up to revit.busyTimeoutSeconds
# for Revit to START a request (that clock runs from the enqueue, so time
# spent queued behind other requests counts) and then up to
# revit.operationTimeoutSeconds for it to FINISH. Thirty silently covered the
# default busy wait of ten; a busy wait above thirty, which the settings allow,
# let this side give up on a request that had waited its turn and was still
# going to answer - "the answer was lost" for work that then completed.
# FRAGMENT-ISSUES section 5b, row 458. Twenty over the SUM leaves the default
# deadline at exactly 90.
CLIENT_MARGIN_S = 20.0


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


# THE ADD-IN'S OWN WORDS, AND THEY HAVE TO BE THE SAME WORDS.
# HeronConfig.GetBool in platform/Heron.Core is the other reader of this same
# file. It accepts these four for true and these four for false, trims first,
# and returns the DEFAULT for anything else - "a value nobody can parse is a
# value nobody stated". tests/test_config_and_health.py reads the C# and
# fails if these lists drift, because two hand-maintained copies of one
# vocabulary is not one vocabulary.
TRUE_WORDS = ("true", "1", "yes", "on")
FALSE_WORDS = ("false", "0", "no", "off")


def truthy(values, key):
    """
    The add-in's own idea of true, for a key this file declares.

    AN UNRECOGNISED VALUE IS THE DEFAULT, NOT FALSE - which is what the C#
    does and what this used to get wrong in the same way the C# did until
    2026-09-21 (FRAGMENT-ISSUES section 5b row 8). While both halves read
    `on` as false they at least agreed; fixing one and not the other is
    worse, because `write.enabled = on` would then let the add-in permit a
    change while the MCP server's own health report says writing is off. One
    settings file with two readers that disagree is the exact hazard
    HeronPermissions names: "the button reports a state the permission gate
    does not honour".
    """
    raw = str(values.get(key, DEFAULTS.get(key, ""))).strip().lower()
    if raw in TRUE_WORDS:
        return True
    if raw in FALSE_WORDS:
        return False
    # Not a word either side understands. Fall back to what this key is
    # declared as, exactly as the C# falls back to its caller's argument.
    fallback = str(DEFAULTS.get(key, "")).strip().lower()
    return fallback in TRUE_WORDS


def operation_timeout(values=None):
    """What the add-in will wait for an operation it has started."""
    values = load() if values is None else values
    return max(1.0, _number(values, "revit.operationTimeoutSeconds", 60))


def busy_timeout(values=None):
    """What the add-in will wait for Revit to pick the request up at all."""
    values = load() if values is None else values
    return max(1.0, _number(values, "revit.busyTimeoutSeconds", 10))


def asks_before_keeping(values=None):
    """Whether Revit asks Keep or Discard before keeping a change (D-126)."""
    values = load() if values is None else values
    return truthy(values, "write.askBeforeKeeping")


def response_timeout(values=None):
    """
    How long THIS side waits - always beyond the add-in's own deadline.

    Derived, never configured separately. The whole point is that the far side
    is the one that gets to answer, because only it knows whether Revit took
    the request, started it, or never saw it.

    BOTH of the add-in's waits, because they run one after the other: the
    clock here starts when the request is sent, so a request queued in the
    add-in spends its busy wait inside this deadline too (row 458).
    """
    values = load() if values is None else values
    return busy_timeout(values) + operation_timeout(values) + CLIENT_MARGIN_S


# A FRAGMENT RUN IS NOT AN ORDINARY REQUEST. Roslyn compiles the source
# inside Revit before a line of it runs, so the five places that send one
# each wrote a longer wait by hand rather than using the derived figure.
# The number is theirs; what was missing is the floor being a FLOOR.
FRAGMENT_FLOOR_S = 180.0


def fragment_timeout(values=None, keeping=False):
    """
    How long to wait for a FRAGMENT to come back. Never below the add-in's.

    `keeping` IS A RUN SENT WITH apply - one the add-in may stop and ASK the
    modeller about (D-126). A job that puts the Keep or Discard box up is
    given revit.askTimeoutSeconds on top of its operation timeout, so this
    side adds it too - exactly what try_once_timeout adds for a DRAFT's one
    run (D-119), because it is the same box and the same wait. Without it the
    client gave up on a modeller still looking at the change, and their
    answer reached nobody. A read, a run that rolls back, or a change with
    the owner's switch off never asks, so it keeps the deadline it always
    had.

    Same rule as response_timeout, for the same reason: the far side is the
    one that gets to answer, because only it knows whether Revit took the
    request, started it, or never saw it.

    THE FIVE CALL SITES EACH TYPED `response_timeout=180.0` INSTEAD. At the
    default settings the add-in waits 60, so 180 is comfortably beyond it and
    the ordering held BY COINCIDENCE - the same coincidence
    tests/test_config_and_health.py records as BUG 2 and asserts against at
    every value rather than at the default. Set
    `revit.operationTimeoutSeconds` to 180 or more - which is what a person
    does when a model is slow, and a slow model is exactly when a fragment
    run is slow - and the literal is now BELOW the add-in's own deadline.
    This side gives up first and reports "no answer" for a request Revit is
    still working on. MEASURED: at 300 the add-in waits 300 and the literal
    gave up at 180, two minutes early. On the write path that is worse than
    a wrong sentence: the request is sent `idempotent=False`, so a lost
    answer cannot be asked again and nobody can say from here whether the
    model changed.
    """
    values = load() if values is None else values
    waited = max(FRAGMENT_FLOOR_S, response_timeout(values))
    if keeping and asks_before_keeping(values):
        waited += ask_timeout(values)
    return waited


def ask_timeout(values=None):
    """What the add-in adds to its wait once a job has asked the modeller (D-119)."""
    values = load() if values is None else values
    return max(1.0, _number(values, "revit.askTimeoutSeconds", 300))


def try_once_timeout(values=None):
    """
    How long to wait for a DRAFT's one run, which ends on a person (D-119).

    The fragment's own deadline PLUS the add-in's ask timeout, because the
    add-in waits for both one after the other. Below that, this side would
    give up while the Keep or Discard box is still open in Revit - and the
    request is not safe to send twice, so the answer would be lost.
    """
    values = load() if values is None else values
    return fragment_timeout(values) + ask_timeout(values)


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
