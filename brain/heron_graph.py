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
import re
import sqlite3
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


def orphans(store, fragments=None):
    """Fragments nothing can compose with, and capabilities nobody wants.

    An orphan is not automatically wrong - a recipe legitimately stands alone -
    but a FILTER that nothing consumes is usually a filter whose contract does
    not match anything, which is a defect a tool can see and a person cannot.

    `fragments` names the library to ask about, the same way composes_into()
    and composes_from() already take it, and it exists for one reason:
    tests/test_graph.py has to run this over a library with a contract broken
    on purpose. Without the parameter the only way to do that was to break
    brain/fragments ITSELF and put it back afterwards, which left the real
    library wrong for 11.9s of every run - long enough for a concurrent
    `git add -A` to commit 55 fragments nobody edited, and permanent if the run
    was killed rather than merely failing. See docs/FRAGMENT-ISSUES.md row 155.

    `is not None` rather than `or`, so an EMPTY library is asked about as an
    empty library. Falling through to the real one on a falsy argument is how
    a caller ends up being answered about a library it did not pass.
    """
    fragments = fragments if fragments is not None else _loaded()
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


# ---------------------------------------------------------------------------
# Document neighbours - Stage 5, and it begins with a COUNT rather than a route
# ---------------------------------------------------------------------------

# WHY THIS IS A COUNT AND NOT A FEATURE.
#
# docs/34 s2.13 measured a third retrieval stream over the FRAGMENT graph at
# six settings and ALL SIX LOST - the gentlest cost 1.1 points of P@1, the
# strongest 14, and P@5 never improved at any setting, so it did not widen
# recall either, which is the one thing a graph stream is supposed to be good
# at.
#
# And it named the property that decided it: DENSITY. Heron's composition
# graph runs a median of 50 neighbours per fragment, worst 230. A fragment
# providing IList<Element> composes with most of the library, so "the
# neighbours of the best hit" is not a signal - it is a large slice of the
# library added as competitors.
#
# A DOCUMENT GRAPH IS A DIFFERENT GRAPH, so that finding does not transfer
# automatically. The TEST that decided it does. So this derives the edges and
# counts them, and nothing reads them into retrieval until a count says it is
# worth it.
#
# D-40: EVERY EDGE HERE IS DERIVED ON DEMAND. A stored document edge is a
# cache that goes stale the moment a document is re-ingested.

# A DOTTED NUMBER IS NOT AUTOMATICALLY A CLAUSE NUMBER, and reading it as one
# put false edges into the only count that decides whether this route is worth
# having. In a standard full of ordinary measurements - "shall fall 1.5m", "a
# 2.5mm gap" - every one of those matched, and in a document that also happens
# to number a clause 1.5 or 2.5 an edge appeared between two clauses that have
# nothing to do with each other. Density is what Stage 5 measures; inflating it
# with false edges is measuring the regex. Found by a review 2026-09-11.
#
# So a match counts only when the text says it is a reference:
#
#   * a unit or a percent directly after it means it is a MEASUREMENT, never
#     a clause - checked first, because it is the common case; or
#   * three or more segments (21.3.2) - no measurement is written that way; or
#   * a citing word just before it - clause, section, table, appendix, part,
#     paragraph, item, or "in accordance with".
#
# A real reference written as bare "1.5" with no citing word is missed. That is
# the safe direction: a missing edge costs recall in a route that has no vote
# yet, and a false edge corrupts the measurement that decides whether it ever
# gets one.
_CLAUSE_REFERENCE = re.compile(r"\b\d+(?:\.\d+)+\b")
_MEASUREMENT_AFTER = re.compile(
    r"\s*(?:%|mm|cm|m|km|in|ft|kg|g|t|l|ml|pa|kpa|bar|mbar|c|k|w|kw|mw|va|"
    r"kva|hz|v|kv|a|ma|db|lux|lm|cfm|m2|m3)\b", re.I)
# A BARE "to" IS NOT A CITING WORD, AND HAVING IT HERE MANUFACTURED EDGES.
#
#     "spacing varies from 1.5 to 2.5 times the diameter"
#
# "times" is not a unit, so the measurement guard let it through, and "to"
# made 2.5 a reference to clause 2.5 wherever that locator existed. Every
# numeric RANGE in a standard is written this way, and standards are mostly
# ranges - so the false edges landed exactly where they are densest, in the
# number that decides whether the graph route is viable at all. Found by a
# review 2026-09-11, in a guard added a round earlier to fix the same class.
#
# The phrases that keep "to" are the ones that actually cite: refer to,
# according to, pursuant to, subject to. Written out rather than made optional,
# because "(refer\s+)?to" is the same bug with more characters.
_CITING_WORD = re.compile(
    r"(?:clause|section|sub-?clause|sub-?section|table|appendix|annex|part|"
    r"paragraph|item|rule|in\s+accordance\s+with|as\s+per|per|see|"
    r"refer(?:s|red|ring)?\s+to|according\s+to|pursuant\s+to|"
    r"subject\s+to|conform(?:s|ing)?\s+to|comply(?:ing)?\s+with)\s*$",
    re.I)


def _is_clause_reference(text, match):
    """Whether this dotted number is citing a clause rather than measuring."""
    after = text[match.end():match.end() + 8]
    if _MEASUREMENT_AFTER.match(after):
        return False
    if match.group(0).count(".") >= 2:
        return True
    return bool(_CITING_WORD.search(text[max(0, match.start() - 30):match.start()]))


def document_neighbours(store, chunk_id=None):
    """Every chunk's neighbours, derived. {chunk_id: set(chunk_id)}.

    THREE KINDS OF EDGE, all read off what the document already says:

      parent    the section a clause sits inside, and its clauses back
      sibling   the clauses under one parent - "the rest of 21.3"
      cites     a clause whose TEXT names another clause's number. This is
                the one that finds what words alone miss: "insulation shall
                comply with 21.3.2" links two clauses that share no subject.

    Nothing is stored. Ask again after a re-ingest and the answer is rebuilt
    from the rows, which is the whole of D-40.
    """
    try:
        rows = store.execute(
            "SELECT id, document_id, parent_id, locator, text "
            "FROM chunks").fetchall()
    except sqlite3.OperationalError as exc:
        # ONLY "THE TABLE IS NOT THERE", and the broad version of this was the
        # same defect heron_retrieve.documents() was corrected for a round
        # earlier and this copy did not get. A locked database or a missing
        # column returned {} - and document_density() then reported a
        # zero-chunk, zero-density corpus, so the Stage 5 measurement that
        # decides whether the graph route is ever worth a vote could record a
        # clean empty reading where no measurement ran at all. That is D-52's
        # plausible zero on the one number this module exists to produce.
        if "no such table" not in str(exc):
            raise
        return {}

    by_locator = {}
    for row in rows:
        if row["locator"]:
            by_locator.setdefault((row["document_id"], row["locator"]),
                                  row["id"])

    children = {}
    for row in rows:
        if row["parent_id"]:
            children.setdefault(row["parent_id"], []).append(row["id"])

    edges = {}
    for row in rows:
        near = set()
        if row["parent_id"]:
            near.add(row["parent_id"])
            for other in children.get(row["parent_id"], ()):
                if other != row["id"]:
                    near.add(other)          # sibling
        near.update(children.get(row["id"], ()))

        # A clause that names another clause's number, within the same
        # document. Across documents it would be a guess - "4.1.1" means
        # something different in every standard.
        body = row["text"] or ""
        for match in _CLAUSE_REFERENCE.finditer(body):
            found = match.group(0)
            if found == row["locator"]:
                continue
            if not _is_clause_reference(body, match):
                continue
            other = by_locator.get((row["document_id"], found))
            if other:
                near.add(other)

        near.discard(row["id"])
        edges[row["id"]] = near

    if chunk_id is not None:
        return {chunk_id: edges.get(chunk_id, set())}
    return edges


def document_density(store):
    """The count Stage 5 turns on. Returns a dict a report can print.

    WHAT THE NUMBER HAS TO BEAT. The fragment graph's median was 50 and its
    worst 230, and at that density the neighbours of a good hit are
    competitors rather than evidence. A document graph that looks like that
    loses the same way for the same reason, and the route is not built.
    """
    edges = document_neighbours(store)
    if not edges:
        return {"chunks": 0, "median": 0, "worst": 0, "isolated": 0,
                "total": 0}
    counts = sorted(len(near) for near in edges.values())
    # THE MEDIAN OF BOTH MIDDLE OBSERVATIONS, not the upper one. This number
    # is what decides whether the graph route is viable at all, so a dataset
    # whose two central counts differ was getting an overstated median and
    # could cross the decision boundary for arithmetic reasons.
    half = len(counts) // 2
    middle = (counts[half] if len(counts) % 2
              else (counts[half - 1] + counts[half]) / 2.0)
    return {"chunks": len(counts),
            "median": middle,
            "worst": counts[-1],
            "isolated": len([c for c in counts if c == 0]),
            "total": sum(counts)}


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
