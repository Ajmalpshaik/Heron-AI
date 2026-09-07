#!/usr/bin/env python3
# Heron-Agent:  HERON-RAG-SMT-005
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
The one place the MCP server reaches the brain.

Steps 7 to 14 built eight modules, seven fragments and ten skills, and until
this file existed **nothing outside brain/ and tests/ imported any of them**.
They were complete, tested and unreachable from a conversation, which is not
what "built" was meant to mean. docs/27 states Phase 2's third definition-of-
done clause as *the Orchestrator resolves through capabilities rather than
agent names*; the Orchestrator is the host (D-01, docs/02 s7), the host reaches
Heron only through MCP tools, and every tool before this one went straight to
the bridge. This is the seam that closes it.

WHY A SEPARATE FILE RATHER THAN IMPORTS IN THE SERVER
-----------------------------------------------------
The server is where tools are declared and answers are worded. If it also knew
how a store is opened, when an index is stale and what a capability row looks
like, then every change to the brain would reach into the file that talks to
the user. One seam, named, is cheaper to keep honest - and it is the file a
future in-Revit panel would reuse without taking the MCP server with it
(docs/02 s7's note about keeping the core host-agnostic where that is free).

WHAT THIS FILE DOES NOT DO, DELIBERATELY
-----------------------------------------
It does not decide which skill the user meant. That judgement is the host's -
HERON-ORC-INT-002 and HERON-ORC-MAIN-001 are host-provided by D-01, and
tools/check-metadata.py already lists them as having no file here on purpose.
Heron's job is to hand the host a catalogue worth planning against and to
resolve a capability deterministically. Building a second intent matcher on
this side would duplicate the host's, disagree with it eventually, and put a
model call inside what docs/02 s6 says must stay T1.

THE CALLER NEVER NAMES A FRAGMENT
---------------------------------
`resolve()` takes a CAPABILITY. `lookup()` takes the user's own sentence and
answers with the CAPABILITY that would serve it - the provider is reported as
evidence, never as the thing to ask for next. That is Step 12's whole purpose
reaching its customer: a fragment can be replaced, split or retired and no
call site here changes, because no call site here mentions one.

NOTHING BELOW IS PROVEN
-----------------------
Every fragment and every skill is DRAFT, and D-30 promotes on one recorded
proof containing a negative case, which needs a real model. So this file can
tell the host what Heron knows how to do and who would do it; it cannot tell
it that any of it works. Every answer says so rather than leaving the reader
to remember.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BRAIN = os.path.join(ROOT, "brain")
if BRAIN not in sys.path:
    sys.path.insert(0, BRAIN)


class BrainUnavailable(Exception):
    """
    The brain cannot be reached from here, and why - in words a user can act on.

    Raised rather than returning an empty catalogue. "Heron knows how to do
    nothing" and "Heron cannot read what it knows" are completely different
    situations, and a user told the first will go looking for a skill that is
    sitting right there on disk.
    """


def _brain():
    """The brain modules, or BrainUnavailable saying what to install.

    Imported here rather than at module scope so that importing this file is
    always safe. The MCP server imports it at startup, and a missing PyYAML
    must degrade one group of tools rather than stop the server - the Revit
    tools underneath have no use for the brain and must keep working.
    """
    try:
        import heron_scope as SCOPE
        import heron_capability as CAP
        import heron_skill as SKILL
        import heron_search as SEARCH
        import heron_embed as EMBED
        import heron_retrieve as RETRIEVE
    except ImportError as exc:
        raise BrainUnavailable(
            "Heron's knowledge layer needs PyYAML and it is not installed: %s\n"
            "Install it with:  pip install --user pyyaml\n"
            "The Revit tools are unaffected - this stops Heron consulting what "
            "it knows, not talking to Revit." % exc)
    return SCOPE, CAP, SKILL, SEARCH, EMBED, RETRIEVE


def warm():
    """Start loading the trained encoder, off the request path. Never raises.

    Called once at server startup. If the brain is unavailable - no PyYAML, no
    model2vec - there is simply nothing to warm and the Revit tools are
    unaffected, which is the same rule _brain() already follows.
    """
    try:
        _S, _C, _SK, _SE, EMBED, _R = _brain()
    except BrainUnavailable:
        return
    try:
        EMBED.warm()
    except Exception:
        # A warm-up that cannot start must never stop the server starting. The
        # cost of failing here is a lexical backend, which is a documented
        # degradation rather than an outage.
        pass


class _Open(object):
    """A ready store: built if empty, indexed if stale, closed on the way out.

    The stores are DERIVED (Golden Rule 11) - deleting them is a safe recovery
    action - so building one on demand is not a repair, it is the documented
    way they come to exist. An empty store means a fresh machine, not damage.

    Indexing is content-hashed on both routes, so re-indexing files that have
    not changed costs nothing and there is no staleness flag to keep in step.
    """

    def __init__(self):
        self.store = None

    def __enter__(self):
        SCOPE, CAP, _SKILL, SEARCH, EMBED, _R = _brain()
        try:
            if SCOPE.knowledge_dir() is None:
                raise BrainUnavailable(
                    "Heron has nowhere to keep what it knows: there is no "
                    "%APPDATA% on this machine and HERON_KNOWLEDGE is not set. "
                    "Set HERON_KNOWLEDGE to a folder and ask again.")
            self.store = SCOPE.open_scope(SCOPE.GLOBAL)
            if self.store.count() == 0:
                self.store.close()
                self.store = None
                built, problems = SCOPE.rebuild()
                self.store = SCOPE.open_scope(SCOPE.GLOBAL)
                if built == 0:
                    # AN EMPTY LIBRARY IS NOT AN EMPTY ANSWER.
                    #
                    # Heron ships with fragments, so nothing indexing means
                    # they could not be read - not that Heron knows nothing.
                    # Told the second, a user goes looking for a skill that is
                    # sitting on disk, which is the confusion BrainUnavailable
                    # exists to prevent. Whatever the validator rejected is
                    # named, because "0 fragments" on its own says nothing
                    # about which file to look at.
                    raise BrainUnavailable(
                        "Heron indexed none of the fragments it ships with, so "
                        "it cannot say what it knows how to do.%s"
                        % ("\n  " + "\n  ".join(problems) if problems else
                           " Nothing was reported as invalid, so look at "
                           "whether brain/fragments/ arrived at all."))
            CAP.rebuild(self.store)
            SEARCH.index(self.store)
            EMBED.index(self.store)
        except BrainUnavailable:
            # __exit__ never runs when __enter__ raises, so the store has to be
            # closed here or the handle leaks - and this path is reachable with
            # one OPEN: the empty-library refusal above happens after reopening.
            self._shut()
            raise
        except Exception as exc:
            self._shut()
            raise BrainUnavailable(
                "Heron could not open what it knows: %s: %s"
                % (type(exc).__name__, exc))
        return self.store

    def _shut(self):
        if self.store is not None:
            self.store.close()
            self.store = None

    def __exit__(self, *_exc):
        self._shut()
        return False


def _capability_of(store, fragment_id):
    """The capability a fragment provides. Used to answer with the capability
    rather than with the fragment, on the two routes that return only an id."""
    for row in store.fragments():
        if row["id"] == fragment_id:
            return row["capability"]
    return None


# ---------------------------------------------------------------------------
# What can Heron currently do
# ---------------------------------------------------------------------------

def catalogue():
    """
    Every skill, what it needs, and whether anything provides it.

    This is HERON-KRN-CAP-008's question - *what can Heron currently do?* - in
    the form the host plans against. A skill whose capabilities are all
    provided is one Heron could attempt; a skill with a gap is one it cannot,
    and the gap names itself rather than needing a second list kept in step
    (docs/18: a capability with no provider IS the capability gap).
    """
    _S, CAP, SKILL, _SE, _E, _R = _brain()
    with _Open() as store:
        provided = set(r["capability"] for r in store.fragments() if r["capability"])
        found, problems = SKILL.load_all()

        skills = []
        for skill in sorted(found.values(), key=lambda s: s.id):
            needs = skill.needs()
            missing = [c for c in needs if c not in provided]
            skills.append({
                "id": skill.id,
                "name": skill.name,
                "status": skill.status,
                "utterances": skill.utterances(),
                "needs": needs,
                "missing": missing,
                "ready": not missing,
            })

        capabilities = []
        for name in sorted(provided):
            got = CAP.resolve(store, name)
            capabilities.append({
                "name": name,
                "risk": got.risk if got else None,
                "providers": len(got.rows) if got else 0,
                "status": got.status if got else None,
                "revit": got.revit if got else [],
            })

        wanted = {}
        for entry in skills:
            for capability in entry["missing"]:
                wanted.setdefault(capability, []).append(entry["id"])

        return {
            "skills": skills,
            "capabilities": capabilities,
            "gaps": sorted((name, sorted(who)) for name, who in wanted.items()),
            "problems": problems + CAP.problems(store),
        }


# ---------------------------------------------------------------------------
# Who can do this - the resolution the whole registry exists for
# ---------------------------------------------------------------------------

def resolve(capability, revit=None):
    """
    Who can do this capability, on this Revit. Never asked for by fragment id.

    `revit` is a WALL, not a preference (docs/05 s8). A fragment declared for
    2021 and applied on 2025 does not announce itself: it runs, it half-works,
    and somebody finds out later by measuring something. So a provider that
    does not declare this release is not ranked lower, it is absent - and the
    answer says which case it is, because "nobody can do that" and "nobody can
    do that ON 2025" send the user in different directions.
    """
    _S, CAP, _SK, _SE, _E, _R = _brain()
    with _Open() as store:
        provided = set(r["capability"] for r in store.fragments() if r["capability"])
        got = CAP.resolve(store, capability, revit=revit)

        if got is None:
            # Two different absences, and telling them apart is the whole
            # value of the answer.
            anywhere = CAP.resolve(store, capability) if revit else None
            return {
                "capability": capability,
                "providers": [],
                "known": capability in provided,
                "blocked_by_version": bool(anywhere),
                "revit": revit,
            }

        return {
            "capability": capability,
            "known": True,
            "risk": got.risk,
            "domain": got.domain,
            "status": got.status,
            "revit": got.revit,
            "asked_for_revit": revit,
            "providers": [{"id": r["id"], "status": r["status"],
                           "kind": r["kind"], "risk": r["risk"]}
                          for r in got.rows],
        }


# ---------------------------------------------------------------------------
# The user's own sentence, resolved to a capability
# ---------------------------------------------------------------------------

def lookup(request, revit=None):
    """
    What the user asked for, resolved to a CAPABILITY through retrieval.

    Steps 9, 10 and 11 reaching the host: identity and cache first, then
    keywords and nearness fused, behind the version wall. What comes back is
    reported as a capability, with the provider underneath as evidence - the
    host plans against the capability, and the fragment stays replaceable.

    The route is always reported, because the routes do not mean the same
    thing: `identity` is an exactly declared phrasing, `hybrid` is a ranked
    guess, and a reader shown only the answer cannot tell which they were
    given.
    """
    _S, _CAP, _SK, _SE, _E, RETRIEVE = _brain()
    with _Open() as store:
        answer = RETRIEVE.find(store, request, revit=revit)
        capability = (_capability_of(store, answer.fragment_id)
                      if answer.fragment_id else None)

        candidates = []
        for c in (answer.candidates or []):
            candidates.append({"capability": c["capability"],
                               "provider": c["id"],
                               "status": c["status"],
                               "why": c["why"]})

        _rows, excluded = RETRIEVE.eligible(store, revit)
        return {
            "request": request,
            "route": answer.route,
            "capability": capability,
            "provider": answer.fragment_id,
            "note": answer.note,
            "autorun": bool(getattr(answer, "autorun", False)),
            "candidates": candidates,
            "excluded": [{"id": e.id, "reason": e.reason} for e in excluded],
            "revit": revit,
        }


def gaps(days=None):
    """
    What Heron was asked for and could not do - HERON-AHR-GAP-001.

    Two halves that answer different questions and must not be merged. The
    audit trail knows what was ATTEMPTED and how it went; the capability
    registry knows what somebody DECLARED a need for and nobody provides. A
    thing can be missing without ever having been attempted, and a thing can
    fail constantly while being perfectly well provided - so the report keeps
    them apart and says which is which.

    The trail half needs no knowledge store, so it survives a missing PyYAML:
    the whole point of this report is being readable on the day something is
    already wrong. A store that will not open costs the registry half only.
    """
    import heron_gaps as GAPS

    entries, skipped = GAPS.read()
    entries = GAPS.since(entries, days)
    found = GAPS.analyse(entries)

    wanted = []
    try:
        _S, CAP, _SK, _SE, _E, _R = _brain()
        with _Open() as store:
            wanted = CAP.gaps(store)
    except Exception:
        # Broad on purpose, and the docstring above says why: the trail half
        # is the deliverable. Reporting nothing because the knowledge store is
        # unavailable would withhold the evidence at the moment it is wanted.
        wanted = []

    # The error CODES stay in heron_gaps, and so does the sentence explaining
    # each one. Resolved here rather than in the server because the server is
    # not allowed to import a brain module - that is what this file is for,
    # and a second import would put brain vocabulary in the file that talks to
    # the user.
    def explain(counter, glossary):
        return [{"code": code, "count": count, "says": glossary[code]}
                for code, count in counter.most_common()]

    return {
        "found": found,
        "wanted": wanted,
        "skipped": skipped,
        "defects": explain(found["defects"], GAPS.DEFECTS),
        "recent": found.get("recent") or {},
        "refusals": explain(found["refusals"], GAPS.CORRECT_REFUSALS),
        "unclassified": [{"code": code, "count": count}
                         for code, count in found["unclassified"].most_common()],
    }


def compatibility(release=None):
    """
    Which Revit releases Heron's fragments actually work on - HERON-FRG-MTX-009.

    Three states, never merged: a fragment CLAIMS a release in its own file,
    a compiler AGREED about that release, and a D-30 proof was recorded ON
    that release. The first is an assertion, the second is about the API
    surface, and only the third is about behaviour.

    Needs no Revit and no knowledge store - it reads the fragment files, the
    build's own runtime table, and whatever the last compile run recorded.
    """
    import heron_matrix as MX

    matrix = MX.build_matrix()
    rows = []
    for rel in matrix["releases"]:
        tally = {MX.CLAIMED: 0, MX.COMPILES: 0, MX.PROVEN: 0, MX.NOT_CLAIMED: 0}
        for cells in matrix["cells"].values():
            tally[cells.get(rel, MX.NOT_CLAIMED)] += 1
        rows.append({
            "release": rel,
            "runtime": matrix["runtimes"].get(rel),
            "claimed_only": tally[MX.CLAIMED],
            "compiles": tally[MX.COMPILES],
            "proven": tally[MX.PROVEN],
            "not_claimed": tally[MX.NOT_CLAIMED],
        })

    if release is not None:
        rows = [r for r in rows if r["release"] == str(release)]

    return {
        "rows": rows,
        "fragments": matrix["library"],
        "releases": matrix["releases"],
        "has_compile_evidence": bool(matrix["evidence"]),
        "tested": matrix["tested"],
        "evidence_at": (matrix["evidence"] or {}).get("at"),
    }
