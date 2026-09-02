# Heron-Agent:  HERON-KRN-DEP-013
# Heron-Step:   13
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
What breaks if this changes.

    python brain/heron_graph.py FRG-ELE-001
    python brain/heron_graph.py --orphans

Step 13 of docs/27-build-order.md.

D-40, WHICH IS THE WHOLE DESIGN
-------------------------------
"An edge is derived before it is stored. Store an edge only when it cannot be
computed from an artifact on demand."

A fragment already declares its capability, its contract, its supported releases
and its runtime. Every edge that follows from those is COMPUTED here, every
time, from the fragments themselves. Nothing is written down twice, so nothing
can fall out of step - and freshness stops being a maintenance problem for the
part of the graph that is most of it.

  DERIVED   capability -> fragment      from the fragment's own capability
            fragment   -> fragment      from the contracts: A provides what B needs
            fragment   -> revit release from its declared list
            fragment   -> runtime       from its declared list

  STORED    skill -> capability         a skill's requirements are a CLAIM about
                                        intent that no artifact underneath it
                                        carries. This is the one edge that
                                        genuinely cannot be computed, and it is
                                        therefore the only one kept.

AND THE DERIVER MUST BE SHOWN TO CATCH SOMETHING
------------------------------------------------
The build order asks for this in as many words, and it is the same standard
check-api-surface.py was held to: a checker that has never caught anything is
evidence about the checker. tests/test_graph.py breaks a contract on purpose -
renames a provided name so a real composition stops composing - and asserts the
graph reports the break BEFORE any of its clean output is believed.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_scope as SCOPE                                   # noqa: E402
import heron_fragment as FRAG                                 # noqa: E402
import heron_capability as CAP                                # noqa: E402


def ensure_tables(store):
    """The stored half: one table, one kind of edge."""
    store.db.executescript("""
        CREATE TABLE IF NOT EXISTS skill_needs (
            skill      TEXT NOT NULL,
            capability TEXT NOT NULL,
            PRIMARY KEY (skill, capability)
        );
    """)
    store.db.commit()


def skill_needs(store, skill, capability):
    """Record that a skill requires a capability. The one stored edge."""
    ensure_tables(store)
    store.execute("INSERT OR IGNORE INTO skill_needs (skill, capability) "
                  "VALUES (?,?)", (skill, capability))
    store.db.commit()


# ---------------------------------------------------------------------------
# The derived edges
# ---------------------------------------------------------------------------

def _loaded():
    """The fragments on disk, by id. The contracts live in the files, not in
    the store - the store keeps what is searchable, the file keeps what is
    true."""
    found, _problems = FRAG.load_all()
    return found


def composes_into(fragment_id, fragments=None):
    """Fragments that could run AFTER this one. Computed from the contracts.

    Not "does run" - nothing has run yet. This is what the declared contracts
    permit, which is exactly the question "if I change what this provides, what
    stops fitting?"
    """
    fragments = fragments or _loaded()
    producer = fragments.get(fragment_id)
    if producer is None:
        return []

    out = []
    for other_id, other in fragments.items():
        if other_id == fragment_id:
            continue
        ok, _why = FRAG.composable(producer, other)
        if ok and other.needs():
            out.append(other_id)
    return sorted(out)


def composes_from(fragment_id, fragments=None):
    """Fragments that could run BEFORE this one."""
    fragments = fragments or _loaded()
    consumer = fragments.get(fragment_id)
    if consumer is None:
        return []

    # No special case for "needs nothing from a fragment" - composable() now
    # refuses that itself, which is where the rule belongs: it is a fact about
    # composition, not about this query.
    out = []
    for other_id, other in fragments.items():
        if other_id == fragment_id:
            continue
        ok, _why = FRAG.composable(other, consumer)
        if ok:
            out.append(other_id)
    return sorted(out)


def impact(store, fragment_id):
    """What breaks if this fragment changes. The question this file exists for.

    Returns a dict, and every entry is derived at the moment it is asked. The
    dangerous one is `sole_provider_of`: change a fragment that is the only
    provider of a capability and the CAPABILITY changes, which means every
    skill that asked for it changes, none of which mention the fragment by name.
    """
    fragments = _loaded()
    rows = {r["id"]: r for r in store.fragments()}
    row = rows.get(fragment_id)

    provides = row["capability"] if row else None
    sole = []
    shared = []
    if provides:
        got = CAP.resolve(store, provides)
        others = [p for p in (got.providers if got else []) if p != fragment_id]
        (shared if others else sole).append(provides)

    ensure_tables(store)
    skills = []
    for capability in sole + shared:
        for r in store.execute(
                "SELECT skill FROM skill_needs WHERE capability = ?",
                (capability,)).fetchall():
            skills.append((r["skill"], capability))

    return {
        "fragment": fragment_id,
        "exists": row is not None,
        "provides": provides,
        "sole_provider_of": sole,
        "shares_capability": shared,
        "downstream": composes_into(fragment_id, fragments),
        "upstream": composes_from(fragment_id, fragments),
        "skills_affected": sorted(set(skills)),
    }


def orphans(store):
    """Fragments nothing can compose with, and capabilities nobody wants.

    An orphan is not automatically wrong - a recipe legitimately stands alone -
    but a FILTER that nothing consumes is usually a filter whose contract does
    not match anything, which is a defect a tool can see and a person cannot.
    """
    fragments = _loaded()
    found = []
    for fragment_id, frag in sorted(fragments.items()):
        if frag.kind == "recipe":
            continue
        if frag.kind == "filter" and not composes_into(fragment_id, fragments):
            found.append((fragment_id, "a filter nothing can consume - check "
                                       "what it provides against what the "
                                       "actions need"))
        if frag.kind == "action":
            # Asked PER NEED, not per producer. Only names another FRAGMENT
            # could supply - a need the request carries, a category or a
            # parameter name, will never have a producer and must not make the
            # fragment look orphaned.
            #
            # This used to ask composes_from(), which is whether some SINGLE
            # fragment supplies EVERYTHING. That is the wrong question here and
            # it produced a false orphan the moment a consumer needed two
            # different things from two different places - a takeoff needs a
            # group key from a parameter read and a quantity from a
            # measurement, and both providers existed. D-46 recorded the
            # limitation and said to lift it when a real composition needed it.
            #
            # It is also STRICTER, not looser: the message now names the need
            # that has no producer instead of listing every need the fragment
            # has and leaving a reader to work out which one is the problem.
            _by_need, unmet = FRAG.feeders(frag, fragments)
            if unmet:
                found.append((fragment_id,
                              "an action nothing can feed - nothing provides %s"
                              % ", ".join(unmet)))
    return found


def main(argv):
    store = SCOPE.open_scope(SCOPE.GLOBAL)
    try:
        CAP.rebuild(store)

        if argv[:1] == ["--orphans"]:
            found = orphans(store)
            if not found:
                print("No orphans. Every filter has a consumer and every")
                print("action has something that can feed it.")
                return 0
            for fragment_id, why in found:
                print("  ORPHAN  %-14s %s" % (fragment_id, why))
            return 1

        if not argv:
            print("  python brain/heron_graph.py FRG-ELE-001")
            print("  python brain/heron_graph.py --orphans")
            return 2

        got = impact(store, argv[0])
        if not got["exists"]:
            print("%s is not in this scope." % argv[0])
            return 1

        print("If %s changes:" % got["fragment"])
        print()
        print("  provides            %s" % (got["provides"] or "nothing"))
        if got["sole_provider_of"]:
            print("  SOLE PROVIDER OF    %s" % ", ".join(got["sole_provider_of"]))
            print("                      change it and the CAPABILITY changes -")
            print("                      and nothing that asked for it named it")
        if got["shares_capability"]:
            print("  shares with others  %s" % ", ".join(got["shares_capability"]))
        print("  could run after it  %s" % (", ".join(got["downstream"]) or "nothing"))
        print("  could run before it %s" % (", ".join(got["upstream"]) or "nothing"))
        for skill, capability in got["skills_affected"]:
            print("  SKILL AFFECTED      %s (via %s)" % (skill, capability))
        print()
        print("Every line above was computed just now from the fragments")
        print("themselves. Nothing here is stored, so nothing here is stale.")
        return 0
    finally:
        store.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
