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

SERVED SINCE 2026-11-15, AS heron_why (PROPOSALS F1)
-----------------------------------------------------
Nothing called snapshot() until then, deliberately: offering it changed the
risk table in heron_tools.py, a declaration with a human in it. F1 was
decided on the owner's delegation that day, and heron_why - READ, no bridge
operation - gathers the facts without asking Revit anything and puts the
verdict into Revit words with in_revit_words() below, which is as pure as
the rest of this file.

Being read changed one thing here: the switches above Changes (D-106). The
snapshot gave a ceiling of ADMIN for Changes alone - FRAGMENT-ISSUES row 254,
item 5, recorded while nothing called it - so an Admin or Publish capability
read as permitted with only Changes on, which the add-in refuses. Each of the
two upper levels now needs its own switch as well as Changes.
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

# And what it lets through when it is ON (D-106). Changes alone lets MODIFY
# through and nothing above it: PUBLISH and ADMIN each need the owner's own
# switch for that level AS WELL, so neither is a higher ceiling - Admin on and
# Publish off lets an Admin change through and not a Publish one, which no
# single ceiling can say. Each level's switch, by the settings key the add-in
# reads (HeronPermissions, mcp/client/heron_config.py).
CEILING_WITH_WRITE = "MODIFY"
SWITCHED_LEVELS = {"PUBLISH": "publish.enabled", "ADMIN": "admin.enabled"}


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
             write_enabled=False, document=None, ceiling=None,
             admin_enabled=False, publish_enabled=False):
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
    admin_enabled, publish_enabled
                  the two switches above Changes (D-106). Each lets its own
                  level through, and only while write_enabled is on as well.
                  Read only when the ceiling is derived: an explicit ceiling
                  is the whole rule, as it was
    """
    providers = providers or {}
    above = set()
    derived = ceiling is None
    if derived:
        ceiling = CEILING_WITH_WRITE if write_enabled else CEILING_WITHOUT_WRITE
        if write_enabled:
            above = set(level for level, on in (("PUBLISH", publish_enabled),
                                                ("ADMIN", admin_enabled)) if on)

    environment = {
        "revit release": release or "not detected",
        "bridge": bridge or "not detected",
        "document": (document or {}).get("title") if document else "none pinned",
        "write gate": "open" if write_enabled else "closed",
        "risk ceiling": ceiling,
        "also let through": ", ".join(sorted(above)) or "nothing above it",
    }

    rows = []
    for name in wanted:
        rows.append(_judge(name, providers.get(name) or [], release, bridge,
                           write_enabled, ceiling, above, derived))
    return Snapshot(environment, rows)


def _judge(name, rows, release, bridge, write_enabled, ceiling,
           above=frozenset(), derived=False):
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
    if _rank(risk) > _rank(ceiling) and (risk or "").upper() not in above:
        # WHICH SWITCH, BY ITS KEY. Changes first, because the two above it
        # do nothing without it; then, with Changes on, the level's own.
        switch = ""
        if not write_enabled:
            switch = " - write.enabled is false"
        elif derived and (risk or "").upper() in SWITCHED_LEVELS:
            switch = " - %s is false" % SWITCHED_LEVELS[(risk or "").upper()]
        return Capability(
            name, BLOCKED_BY_TRUST,
            "%s, and the ceiling here is %s%s" % (risk, ceiling, switch),
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


# ------------------------------------------------------------ in Revit words
#
# WHAT A MODELLER IS TOLD FOR EACH VERDICT, AND WHAT THEY CAN DO ABOUT IT
# (PROPOSALS F1). The six verdicts are words for a planner; heron_why hands a
# modeller the sentence behind one. As pure as the rest of this file - every
# fact is an argument - so tests/test_runtime_snapshot.py reads each sentence
# with no Revit, no SDK and no library.

BUTTON = "Heron > AI Bridge > Heron"

# The switch each level needs, by the name it has in Heron's Settings in
# Revit (D-106, D-150), and what a capability at that level does.
SWITCH_NAMES = {"MODIFY": "Changes", "PUBLISH": "Publish", "ADMIN": "Admin"}
WHAT_IT_DOES = {
    "MODIFY": "changes the model",
    "PUBLISH": "publishes - an export, a print, a save or a sync",
    "ADMIN": ("is an Admin change - a project or global parameter, a "
              "workset, a new family file or the model's coordinates"),
}


def releases_of(rows):
    """Every Revit release any of `rows` is written for, oldest first."""
    found = set()
    for row in rows or []:
        found.update(str(v) for v in (row.get("revit") or []))
    return sorted(found)


def switches_needed(risk):
    """The owner's switches a risk needs, in the order they go on - [] for a
    risk that needs none."""
    level = (risk or "").upper()
    if level == "MODIFY":
        return ["Changes"]
    if level in SWITCHED_LEVELS:
        return ["Changes", SWITCH_NAMES[level]]
    return []


def _switch_phrase(names):
    """"the owner's Changes switch", "the owner's Changes and Admin switches"."""
    return "the owner's %s switch%s" % (" and ".join(names),
                                        "es" if len(names) > 1 else "")


def in_revit_words(cap, rows=None, release=None, how=None, no_revit_here=False,
                   write_enabled=False, said_by=None):
    """
    (what stands in the way, what the modeller can do) for one Capability
    snapshot() judged - two sentences in Revit's words, never a verdict name.

    rows           its providers, as snapshot() was given them
    release        the release it was judged on, or None
    how            why there is no release, in the session layer's own words
                   ("nothing is connected", "2 Revits are connected and none
                   has been chosen"), or None
    no_revit_here  no Revit can be on this machine - it is not Windows (D-132)
    write_enabled  the Changes switch it was judged with
    said_by        the Revit whose word the switches were - "Revit 2024
                   (session 4242)" - when revit_health found that Revit's
                   settings file is not this PC's (Q-70); None when they were
                   this PC's own settings file
    """
    name = cap.name
    written_for = ", ".join(releases_of(rows)) or "no release"
    level = (cap.risk or "").upper()
    needs = switches_needed(level)

    if cap.verdict == NO_PROVIDER:
        return ("Heron has no way of doing %s: nothing in its library does it, "
                "on any Revit release. That is a gap in what Heron knows, not a "
                "fault - nothing in your model or in Revit is stopping it." % name,
                "Nothing can be switched on for it. heron_lookup with the "
                "request in your own words may find it under another name; "
                "otherwise it has to be written and proved before Heron can "
                "do it.")

    if cap.verdict == UNSUPPORTED_RELEASE:
        return ("Heron can do %s, but not on Revit %s: what does it is written "
                "for Revit %s only, and Heron never runs anything on a release "
                "it was not written for." % (name, release, written_for),
                "Do it in one of those releases, or it needs a version written "
                "and checked for Revit %s." % release)

    if cap.verdict == NOT_DETECTED:
        said = ("Heron can do %s on Revit %s, and it cannot tell which Revit "
                "release you are on - %s - so whether it applies to yours is "
                "not known. That is not a no."
                % (name, written_for, how or "nothing has said"))
        if no_revit_here:
            return (said,
                    "There is no Revit on this machine (it is not Windows). Ask "
                    "again with release set to your Revit release, such as "
                    "\"2024\", to hear whether it would run there.")
        return (said,
                "Ask again with release set to your Revit release, such as "
                "\"2024\". For Heron to see it for itself, open Revit and press "
                "%s - with more than one open, choose one with "
                "revit_use_session." % BUTTON)

    if cap.verdict == BLOCKED_BY_TRUST:
        if level not in RISK_ORDER:
            return ("Heron cannot read what %s is allowed to do, so it treats it "
                    "as the most dangerous kind and will not run it." % name,
                    "No switch changes that: what it declares has to be put "
                    "right in Heron's library first.")
        if not needs:
            # Only an explicit ceiling below EXECUTE blocks one of these, and
            # no switch of the owner's moves that.
            return ("%s is declared %s, above what Heron may run here." % (name, level),
                    "No switch of the owner's changes that here.")
        off = "Changes" if not write_enabled else SWITCH_NAMES[level]
        turn_on = " then ".join(needs) if not write_enabled else off
        # WHOSE WORD THE SWITCH WAS (Q-70). This PC's settings file, unless
        # revit_health found the Revit's own is not it - then that Revit's,
        # as it last said it: Revit decides by its own file either way.
        source = ("That is what %s said it has, the last time revit_health "
                  "asked it; Revit reads its settings again before every change, "
                  "so if it answers differently now, Revit is right." % said_by
                  if said_by else
                  "That is what Heron's settings file on this PC says; Revit "
                  "reads its own copy before every change, so if Revit answers "
                  "differently, Revit is right.")
        return ("%s %s, which needs %s on, and %s is off - so Heron refuses it "
                "before anything reaches Revit."
                % (name, WHAT_IT_DOES[level], _switch_phrase(needs), off),
                "The owner turns %s on in Heron's Settings in Revit. %s"
                % (turn_on, source))

    if cap.verdict == NEEDS_REVIT:
        also = ("; because it %s, %s must be on there too"
                % (WHAT_IT_DOES[level], _switch_phrase(needs))) if needs else ""
        if no_revit_here:
            return ("Heron can do %s on Revit %s, and nothing in its library "
                    "stands in the way - but there is no Revit on this machine "
                    "(it is not Windows)." % (name, release),
                    "It is done on the modeller's Windows PC: open Revit %s there "
                    "and press %s%s." % (release, BUTTON, also))
        return ("Heron can do %s on Revit %s and is allowed to, but no Revit %s "
                "has Heron connected." % (name, release, release),
                "Open Revit %s and press %s on the ribbon. If it is already "
                "pressed, revit_health says why Heron cannot see it."
                % (release, BUTTON))

    return ("Nothing stands in the way of %s on Revit %s: Heron's library does "
            "it on that release, it is allowed, and Revit %s has Heron "
            "connected." % (name, release, release),
            "Nothing to change - ask for it. If it is refused all the same, the "
            "refusal names the reason.")
