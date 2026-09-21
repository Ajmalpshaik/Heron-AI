#!/usr/bin/env python3
# Heron-Agent:  HERON-INS-ENV-002, HERON-KRN-CAP-008
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
What can Heron actually do, right now, and when it cannot - why.

THE PROBLEM THIS EXISTS FOR
---------------------------
brain/heron_capability.py answers "who can do this" and returns None when
nobody can. None is the right answer to a different question from the one a
planner is asking, because it is returned for reasons that are not alike:

    nothing in the library provides that capability at all
    something does, but not on the Revit release in front of us
    something does, and nothing has told us which release that is
    something does, and the trust gate will not let it run
    something does, and there is no Revit connected to run it in

A planner given None can only say "I cannot". A planner given the REASON can
say which, and the five sentences a modeller needs are different every time:
"Heron has no fragment for that" is a gap, "not on Revit 2020" is a version
answer, "press the Heron button" is an instruction, and "write.enabled is
false" is a setting. Rolling five into one is how a platform comes to answer
every question with a shrug.

WHY IT LOOKS LIKE heron_health.py
----------------------------------
Because that file already solved the same problem for components and it works:
every fact arrives as an ARGUMENT rather than being discovered here. That is
what makes it testable on a machine with no Revit, no bridge and no config -
which is the only way it was ever going to be tested at all - and it keeps this
file free of any opinion about where a fact comes from.

NOT DETECTED IS NOT UNAVAILABLE, AND THAT IS THE WHOLE POINT
--------------------------------------------------------------
If nothing has said which Revit is in front of us, Heron does not know whether
a 2020-only capability is usable. Reporting that as unavailable is a guess, and
it is the guess that makes a platform feel broken when it is merely uninformed.
NOT_DETECTED says so, and names the fact that is missing.
"""

# ------------------------------------------------------------------- verdicts
AVAILABLE = "AVAILABLE"
NO_PROVIDER = "NO_PROVIDER"
UNSUPPORTED_RELEASE = "UNSUPPORTED_RELEASE"
NOT_DETECTED = "NOT_DETECTED"
BLOCKED_BY_TRUST = "BLOCKED_BY_TRUST"
NEEDS_REVIT = "NEEDS_REVIT"

# Same seven levels, same order and same spelling as mcp/server/heron_tools.py
# and the fragment header. A third vocabulary for one idea is a third thing to
# keep in step, and this repository has said so twice already.
RISK_ORDER = ["READ", "ANALYZE", "SUGGEST", "EXECUTE", "MODIFY", "PUBLISH", "ADMIN"]

# What the write gate actually gates. write.enabled false does not stop Heron
# reading, analysing or selecting - it stops it CHANGING the model, and MODIFY
# is where that line is drawn in heron_tools.py.
CEILING_WITHOUT_WRITE = "EXECUTE"


def _rank(risk):
    try:
        return RISK_ORDER.index((risk or "READ").upper())
    except ValueError:
        return len(RISK_ORDER)          # unknown risk is treated as the worst


class Capability(object):
    """One capability, its verdict, and the sentence behind it."""

    def __init__(self, name, verdict, detail, risk=None, provider=None):
        self.name = name
        self.verdict = verdict
        self.detail = detail
        self.risk = risk
        self.provider = provider

    def __repr__(self):
        return "<%s %s>" % (self.name, self.verdict)

    def as_dict(self):
        return {"capability": self.name, "verdict": self.verdict,
                "detail": self.detail, "risk": self.risk,
                "provider": self.provider}


class Snapshot(object):
    """Every capability asked about, plus the environment it was asked in."""

    def __init__(self, environment, capabilities):
        self.environment = environment
        self.capabilities = capabilities

    def by_verdict(self, verdict):
        return [c for c in self.capabilities if c.verdict == verdict]

    def as_dict(self):
        return {"environment": self.environment,
                "capabilities": [c.as_dict() for c in self.capabilities]}

    def describe(self):
        lines = ["Environment:"]
        for key in sorted(self.environment):
            lines.append("  %-16s %s" % (key, self.environment[key]))
        lines.append("")
        counts = {}
        for cap in self.capabilities:
            counts[cap.verdict] = counts.get(cap.verdict, 0) + 1
        lines.append("Capabilities: %d" % len(self.capabilities))
        for verdict in (AVAILABLE, NO_PROVIDER, UNSUPPORTED_RELEASE,
                        NOT_DETECTED, BLOCKED_BY_TRUST, NEEDS_REVIT):
            if counts.get(verdict):
                lines.append("  %-21s %d" % (verdict, counts[verdict]))
        unavailable = [c for c in self.capabilities if c.verdict != AVAILABLE]
        if unavailable:
            lines.append("")
            for cap in unavailable:
                lines.append("  %-21s %-34s %s"
                             % (cap.verdict, cap.name, cap.detail))
        return "\n".join(lines)


def snapshot(wanted, providers=None, release=None, bridge=None,
             write_enabled=False, document=None, ceiling=None):
    """
    Build the picture from facts already gathered elsewhere.

    wanted        capability names being asked about
    providers     name -> list of {"id", "risk", "revit": [...], "status"}
                  as derived from the fragment library. An empty list and a
                  missing key mean the same thing and both mean NO_PROVIDER
    release       the Revit release in front of us, or None if nothing said
    bridge        "connected" | "starting" | "none" | None if nothing said
    write_enabled whether the write gate is open
    document      the pinned document, or None
    ceiling       an explicit highest permitted risk. Derived from
                  write_enabled when not given, so a caller cannot forget it
    """
    providers = providers or {}
    if ceiling is None:
        ceiling = RISK_ORDER[-1] if write_enabled else CEILING_WITHOUT_WRITE

    environment = {
        "revit release": release or "not detected",
        "bridge": bridge or "not detected",
        "document": (document or {}).get("title") if document else "none pinned",
        "write gate": "open" if write_enabled else "closed",
        "risk ceiling": ceiling,
    }

    rows = []
    for name in wanted:
        rows.append(_judge(name, providers.get(name) or [], release, bridge,
                           write_enabled, ceiling))
    return Snapshot(environment, rows)


def _judge(name, rows, release, bridge, write_enabled, ceiling):
    if not rows:
        return Capability(
            name, NO_PROVIDER,
            "nothing in the library provides it - this is a capability gap, "
            "not a fault")

    # THE ORDER OF THESE THREE MATTERS, and it runs from the most certain fact
    # to the least. A release we were never told is not evidence against a
    # provider, so it cannot be allowed to answer before a provider list that
    # genuinely has nothing for this release.
    if release is not None:
        supported = [r for r in rows if str(release) in [str(v) for v in (r.get("revit") or [])]]
        if not supported:
            return Capability(
                name, UNSUPPORTED_RELEASE,
                "%d provider(s), none declaring Revit %s"
                % (len(rows), release),
                risk=_worst_risk(rows))
        rows = supported
    else:
        return Capability(
            name, NOT_DETECTED,
            "%d provider(s), and nothing has said which Revit is in front of us, "
            "so whether they apply is unknown rather than no" % len(rows),
            risk=_worst_risk(rows))

    # THE RISK AND THE PROVIDER COME FROM THE SAME ROW, which they did not
    # until 2026-09-21. `risk` was the WORST across the providers and
    # `provider` was `rows[0]["id"]` - so a capability answered by a READ
    # fragment and a MODIFY one reported risk MODIFY beside the READ
    # fragment's name, and a planner reading the pair would blame a fragment
    # that is not the one being blocked. Naming the worst provider is also
    # the only name worth printing beside a worst-case figure.
    # FRAGMENT-ISSUES section 5b, row 27.
    decided = _worst_provider(rows)
    risk = decided.get("risk")
    if _rank(risk) > _rank(ceiling):
        return Capability(
            name, BLOCKED_BY_TRUST,
            "%s, and the ceiling here is %s%s"
            % (risk, ceiling,
               " - write.enabled is false" if not write_enabled else ""),
            risk=risk, provider=decided.get("id"))

    if bridge != "connected":
        return Capability(
            name, NEEDS_REVIT,
            "permitted, and there is no Revit answering - open Revit and press "
            "Heron > AI Bridge > Heron",
            risk=risk, provider=decided.get("id"))

    return Capability(name, AVAILABLE, "%d provider(s)" % len(rows),
                      risk=risk, provider=decided.get("id"))


def _worst_provider(rows):
    """
    The provider with the highest risk, never the first one.

    Same rule brain/heron_capability.py applies and for the same reason: a
    capability answered by a thing that reads and a thing that modifies is
    as dangerous as its most dangerous provider, whichever happens to sort
    first.

    THE ROW, NOT JUST ITS RISK. This returned the risk alone, and every
    caller then printed `rows[0]["id"]` beside it - two facts about two
    different fragments, presented as one. Returning the row makes that
    impossible to get wrong again. An empty dict for no rows, because the
    only caller has already answered NO_PROVIDER by then.
    """
    worst = None
    for row in rows:
        if worst is None or _rank(row.get("risk")) > _rank(worst.get("risk")):
            worst = row
    return worst or {}


def _worst_risk(rows):
    """The highest risk among the providers. See _worst_provider."""
    return _worst_provider(rows).get("risk")
