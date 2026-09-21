# Heron-Agent:  HERON-KRN-CAP-008
# Heron-Step:   12
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Capability Registry - ask for what you want done, never for who does it.

    python brain/heron_capability.py                 what exists, and the gaps
    python brain/heron_capability.py FILTER_ELEMENTS_BY_CATEGORY

Step 12 of docs/27-build-order.md, and the roadmap calls it the highest-leverage
single piece in the phase.

WHAT IT IS FOR (docs/18 s2)
---------------------------
Without it, whatever plans the work has to know which fragment does what - so it
accumulates domain knowledge, which is the monolith both specifications forbid.
With it, the planner matches a REQUEST to a CAPABILITY and never learns what a
duct is. Fragments become replaceable, retirement is safe, and a capability with
no provider IS the capability gap - no separate report needed.

ALMOST ALL OF IT IS DERIVED, AND THAT IS D-40 APPLIED
-----------------------------------------------------
"An edge is derived before it is stored. Store an edge only when it cannot be
computed from an artifact on demand."

A fragment already declares its capability, its contract, its risk and the Revit
releases it supports. So the registry does NOT store providers, contracts,
platform support or status - it computes them from the fragments every time it
is rebuilt. What is left stored is the little that nothing underneath carries.

WHY RISK IS DERIVED AND NOT DECLARED HERE
-----------------------------------------
Risk already has two homes in this repository - mcp/server/heron_tools.py for
what an MCP TOOL may do, and each fragment.yaml for what a FRAGMENT does. Giving
the registry a third declaration would guarantee that one day two of them
disagree and nobody knows which is true.

So a capability's risk is **the highest risk among its providers**, computed.
And when two providers of one capability declare DIFFERENT risks, that is
reported as a defect rather than resolved quietly: a thing that reads and a
thing that modifies are not two implementations of one capability, whatever
they have been named. That check is the reason deriving beats declaring here -
a declared value would have hidden exactly this.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_scope as SCOPE                                   # noqa: E402
import heron_fragment as FRAG                                 # noqa: E402

# Ordered least to most dangerous. Same vocabulary as the tool registry and the
# fragment header, deliberately - a third spelling of one idea is a third thing
# to keep in step.
RISK_ORDER = ("READ", "ANALYZE", "SUGGEST", "EXECUTE", "MODIFY", "PUBLISH",
              "ADMIN")

# docs/02 section 4. A fragment is plain deterministic code, so a capability
# provided only by fragments is T1 by construction. The tier is STORED rather
# than derived because nothing beneath it carries the fact - and it is the one
# question the planner asks before selecting anything: can this be done without
# a model call?
TIERS = ("T1", "T2", "T3")

# Trust order for choosing between providers. docs/18 says "ordered by trust
# score"; until a score exists, the lifecycle IS the trust - it is the only
# evidence in the system that anybody has watched the thing work.
TRUST = {
    "PRODUCTION": 6, "PROVEN": 5, "VALIDATED": 4, "TESTING": 3,
    "DRAFT": 2, "DISCOVERED": 1, "DEPRECATED": 0, "ARCHIVED": -1,
}


class Capability(object):
    def __init__(self, name, rows):
        self.name = name
        self.rows = rows                      # provider rows, best first

    @property
    def providers(self):
        return [r["id"] for r in self.rows]

    @property
    def risk(self):
        """The highest risk any provider carries. Derived, never declared."""
        worst = None
        for row in self.rows:
            here = row["risk"]
            if here not in RISK_ORDER:
                continue
            if worst is None or RISK_ORDER.index(here) > RISK_ORDER.index(worst):
                worst = here
        return worst

    @property
    def domain(self):
        return self.rows[0]["domain"] if self.rows else None

    @property
    def revit(self):
        """Releases EVERY provider supports - an intersection, not a union.

        A union would claim the capability works on 2027 because one provider
        does, and then hand back a provider that does not. The honest answer to
        "can Heron do this on 2027" is "only where every way it knows of doing
        it works there"; per-provider support is still on each row for the
        retrieval filter to use.
        """
        sets = [set((r["revit"] or "").split(",")) - {""} for r in self.rows]
        if not sets:
            return []
        keep = sets[0]
        for other in sets[1:]:
            keep &= other
        return sorted(keep)

    @property
    def status(self):
        """The BEST status among providers - what this capability can be
        trusted to at its best, not on average."""
        return self.rows[0]["status"] if self.rows else None

    def __repr__(self):
        return "<Capability %s %s %d provider(s)>" % (
            self.name, self.risk, len(self.rows))


# ---------------------------------------------------------------------------
# Building the registry
# ---------------------------------------------------------------------------

def ensure_tables(store):
    """The stored half. Small on purpose - everything else is computed."""
    store.db.executescript("""
        CREATE TABLE IF NOT EXISTS capabilities (
            name  TEXT PRIMARY KEY,
            tier  TEXT NOT NULL,
            note  TEXT NOT NULL DEFAULT ''
        );
        CREATE TABLE IF NOT EXISTS capabilities_wanted (
            name TEXT PRIMARY KEY,
            why  TEXT NOT NULL
        );
    """)
    store.db.commit()


def rebuild(store):
    """Note every capability the fragments provide. Returns how many.

    Does not delete a capability whose providers have gone: that is precisely
    the gap docs/18 wants visible, and deleting it would erase the evidence
    that anybody ever wanted it.
    """
    ensure_tables(store)
    seen = set()
    for row in store.fragments():
        name = row["capability"]
        if not name or name in seen:
            continue
        seen.add(name)
        store.execute(
            "INSERT OR IGNORE INTO capabilities (name, tier, note) "
            "VALUES (?, 'T1', ?)",
            (name, "provided by fragments, which are deterministic code"))
    store.db.commit()
    return len(seen)


# How several reasons for one capability are kept in one column. The table
# holds one row per capability by design - the gap is the NAME, and that is
# what docs/18 says is the finding - so the reasons share the cell rather
# than the row. Distinct ones only: the common caller is a loop over every
# skill, which would otherwise write the same sentence on every run.
WHY_JOIN = "; "


def wanted_by(why):
    """The distinct reasons in one stored `why`, in the order they arrived."""
    found = []
    for part in str(why or "").split(WHY_JOIN):
        part = part.strip()
        if part and part not in found:
            found.append(part)
    return found


def want(store, name, why):
    """Record that something NEEDS this capability, whether or not it exists.

    This is what turns "we have no fragment for that" from a silence into a
    finding. A skill written in Step 14 that needs a capability nobody has
    built says so here, and the gap report reads it.

    EVERY REASON IS KEPT, and that is the whole point of the function. It
    used to be `ON CONFLICT DO UPDATE SET why = ?`, so the last caller won
    and the earlier ones were gone. MEASURED 2026-09-21: three different
    things wanting TRACE_DUCT_SYSTEM - two named skills and a user asking by
    name - left ONE reason, and it was the least useful of the three.
    `heron_brain.resolve()` writes the generic "asked for by name and no
    fragment provides it", so one person asking erased which skills were
    blocked. The more people asked, the less the gap report could say about
    who needed it, which is backwards. Golden Rule 14. Row 5b-94.
    """
    ensure_tables(store)
    why = str(why or "").strip()
    if not why:
        # A want with no reason is still a want - the NAME is the finding -
        # but it must not blank a reason somebody else gave.
        why = "wanted, with no reason recorded"
    row = store.execute(
        "SELECT why FROM capabilities_wanted WHERE name = ?", (name,)).fetchone()
    if row is None:
        store.execute(
            "INSERT INTO capabilities_wanted (name, why) VALUES (?,?)",
            (name, why))
    else:
        reasons = wanted_by(row["why"])
        if why not in reasons:
            reasons.append(why)
            store.execute(
                "UPDATE capabilities_wanted SET why = ? WHERE name = ?",
                (WHY_JOIN.join(reasons), name))
    store.db.commit()


# ---------------------------------------------------------------------------
# Resolution - the whole point
# ---------------------------------------------------------------------------

def resolve(store, name, revit=None):
    """Who can do this. Returns a Capability, or None if nobody can.

    Providers are ordered by TRUST first - the lifecycle status, which is the
    only evidence in the system that somebody watched it work - then by version,
    newest first, then by id so the order never depends on luck.

    THE CALLER NEVER NAMES A FRAGMENT. That is the entire point: add a second
    provider, retire the first, split one into three, and every call site here
    is unchanged because no call site ever mentioned one.
    """
    rows = []
    for row in store.fragments():
        if row["capability"] != name:
            continue
        if revit is not None:
            supported = set((row["revit"] or "").split(","))
            if str(revit) not in supported:
                continue
        rows.append(row)

    if not rows:
        return None

    rows.sort(key=lambda r: (-TRUST.get(r["status"], 0), r["id"]))
    return Capability(name, rows)


def best_provider(store, name, revit=None):
    """One fragment id, or None. The shortest way to ask."""
    got = resolve(store, name, revit)
    return got.providers[0] if got else None


# ---------------------------------------------------------------------------
# The checks that make deriving worth it
# ---------------------------------------------------------------------------

def problems(store):
    """Everything wrong with the registry as it stands."""
    found = []
    by_capability = {}
    for row in store.fragments():
        by_capability.setdefault(row["capability"], []).append(row)

    for name, rows in sorted(by_capability.items()):
        risks = sorted(set(r["risk"] for r in rows if r["risk"]))
        if len(risks) > 1:
            found.append(
                "%s has providers declaring DIFFERENT risks (%s). A thing that "
                "reads and a thing that modifies are not two implementations of "
                "one capability, whatever they have been named - split them"
                % (name, ", ".join(risks)))

        kinds = sorted(set(r["kind"] for r in rows if r["kind"]))
        if len(kinds) > 1:
            found.append(
                "%s is provided by both a %s and a %s. One capability, one "
                "shape - a caller composing against it cannot know which it "
                "will get" % (name, kinds[0], kinds[1]))
    return found


def gaps(store, required=None):
    """Capabilities somebody needs and nobody provides.

    docs/18: "A capability with no provider IS the capability gap." No separate
    report, no separate list to keep in step - the absence is the finding.
    """
    ensure_tables(store)
    have = set(row["capability"] for row in store.fragments() if row["capability"])

    wanted = {}
    for row in store.execute(
            "SELECT name, why FROM capabilities_wanted").fetchall():
        wanted[row["name"]] = row["why"]
    for name in (required or []):
        wanted.setdefault(name, "asked for by the caller")

    return sorted((name, why) for name, why in wanted.items()
                  if name not in have)


def main(argv):
    store = SCOPE.open_scope(SCOPE.GLOBAL)
    try:
        count = rebuild(store)

        if argv:
            name = argv[0]
            got = resolve(store, name)
            if got is None:
                print("%s - NO PROVIDER. That is the capability gap." % name)
                return 1
            print("Capability  %s" % got.name)
            print("Risk        %s   (derived from its providers, never declared)"
                  % got.risk)
            print("Domain      %s" % got.domain)
            print("Revit       %s   (every provider, not any)"
                  % (", ".join(got.revit) or "none in common"))
            print("Best status %s" % got.status)
            print("Providers   in trust order")
            for row in got.rows:
                print("   %-14s %-11s %s" % (row["id"], row["status"], row["kind"]))
            return 0

        print("%d capability(ies) in the global scope" % count)
        print()
        for name in sorted(set(r["capability"] for r in store.fragments())):
            got = resolve(store, name)
            print("  %-32s %-8s %d provider(s)"
                  % (name, got.risk or "?", len(got.rows)))

        wrong = problems(store)
        if wrong:
            print()
            for line in wrong:
                print("  PROBLEM  %s" % line)

        missing = gaps(store)
        if missing:
            print()
            for name, why in missing:
                print("  GAP      %-32s %s" % (name, why))
        print()
        print("A caller asks for a capability and never names a fragment.")
        print("That is what lets one be replaced without touching anything.")
        return 1 if wrong else 0
    finally:
        store.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
