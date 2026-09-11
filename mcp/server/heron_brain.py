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


class _NoAudit(object):
    """What the brain records when heron_audit cannot be imported at all.

    A trail is evidence, never a dependency. If brain/ is not importable the
    tools above are already failing for a better reason than logging, and a
    logger that can break a request it was only supposed to describe has the
    priority backwards.
    """

    def __getattr__(self, _name):
        return lambda *a, **k: False


def _audit():
    """heron_audit, or a no-op that swallows the call. Never raises."""
    try:
        import heron_audit
        return heron_audit
    except ImportError:
        return _NoAudit()


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

    # THE RE-RANKER IS WARMED FOR THE SAME REASON AND IT IS A HEAVIER IMPORT.
    #
    # `A7`/`A8` cost thirty minutes of a real tool call because a 1.0 s
    # model2vec import ran on the asyncio event loop. A cross-encoder pulls in
    # torch, which is larger. So it is loaded here, off the request path,
    # before anything asks - and until it finishes, retrieval keeps fusion's
    # order and says the re-ranker did not run.
    #
    # NOT a seventh member of _brain()'s tuple: six call sites unpack that
    # positionally, and heron_rerank needs no PyYAML, so importing it here is
    # always safe.
    try:
        import heron_rerank as RERANK
        RERANK.warm()
    except Exception:
        pass


# THE SOURCES ARE RECONCILED ONCE PER PROCESS, NOT ONCE PER REQUEST.
#
# Two things were built in Stage 6 and reachable from nothing: refresh(), which
# re-ingests a source file that changed on disk, and restore(), which rebuilds
# the document rows from the append-only manifest after somebody deletes the
# derived store. Both were called only by their own CLI and their own tests, so
# a served Heron answered from obsolete clauses until a person remembered a
# command, and a deleted store came back with every fragment and NO DOCUMENTS.
# Found by a review 2026-09-11, which was right that "maintenance is automatic"
# was a claim and not a fact.
#
# ONCE PER PROCESS is the compromise and the reason is cost: both walk every
# source file and hash it, which is fine at startup and is not fine on every
# catalogue lookup. A file changed while the server is up is still stale until
# it restarts, which is the same limit tests/test_maintenance.py already
# records - nothing here watches the filesystem.
_SYNCED = set()


def _reconcile(store):
    """Re-ingest what changed, restore what the manifest remembers. Never raises.

    A maintenance pass must not be able to take the server down: everything it
    does is derived, and the worst case of skipping it is the staleness that
    existed before it was wired in at all.
    """
    # KEYED ON THE STORE FILE, NOT THE SCOPE NAME. `project` is one scope name
    # and every project has its own database under it, so keying on the name
    # meant the first project reconciled in a session marked "project" done
    # and every OTHER project was skipped for the life of the process - their
    # changed files never re-ingested, their deleted stores never restored.
    # One name standing for many stores. Found by a review 2026-09-11.
    key = getattr(store, "path", None) or store.scope
    if key in _SYNCED:
        return
    try:
        import heron_ingest as INGEST
    except ImportError:
        # Nothing to reconcile with, and that will not change inside this
        # process. Marking it done stops every later request retrying an
        # import that already failed.
        _SYNCED.add(key)
        return
    try:
        # RESTORE FIRST. An empty document table after a deleted store is the
        # case restore() exists for, and refreshing nothing costs nothing.
        INGEST.restore(store)
        INGEST.refresh(store)
    except Exception:
        # MARKED ONLY ON SUCCESS, and the first version marked it BEFORE the
        # attempt. One transient failure - a locked database, a file being
        # written as it was read - then made every later request in the
        # process skip reconciliation, so the server answered from stale
        # clauses until somebody restarted it. A pass that gives up for good
        # after one bad moment is worse than one that was never wired in,
        # because it looks wired in. Found by a review 2026-09-11.
        return
    _SYNCED.add(key)


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
            # R-29: THE USER NEVER MANAGES THE INDEX BY HAND, and documents
            # were the half where that had stopped being true. Fragments were
            # re-indexed on every open here; chunks were not, so a document
            # ingested through the host stayed unsearchable until somebody
            # remembered a command. Both halves are derived and both are free
            # when nothing changed, so both are rebuilt in the same place.
            # BEFORE THE INDEXES, because both of these change the rows the
            # indexes are built from.
            _reconcile(self.store)
            SEARCH.index_chunks(self.store)
            EMBED.index_chunks(self.store)
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

        gap_list = sorted((name, sorted(who))
                          for name, who in wanted.items())
        # D-62. Declared in heron_audit and unwired until Codex noticed on
        # PR #44 that heron_capabilities left no trace at all - so the "brain
        # side of the trail" was two of its four operations.
        _audit().catalogue(skills=len(skills), capabilities=len(capabilities),
                           gaps=len(gap_list))
        return {
            "skills": skills,
            "capabilities": capabilities,
            "gaps": gap_list,
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
            known = capability in provided

            # D-63. THE WANT IS RECORDED HERE AND NOWHERE ELSE, and the reason
            # is the distinction this branch already draws.
            #
            # `blocked_by_version` is NOT a missing capability. A provider
            # exists; this release is not on its list. Recording that as a want
            # would put a capability on the gap report that ALREADY EXISTS, and
            # commissioning a fragment to build it again is the exact failure
            # heron_gaps.py was corrected for - its loudest error, needs_unbound
            # at 38 of 176, is the executor behaving correctly.
            #
            # A capability NOBODY provides is the other case, and it is the one
            # D-40 cannot derive: no artifact declares it, so no pass over the
            # library can compute it. Only somebody asking reveals it. That is
            # what want() was written for and why it is not deleted.
            if not known and not anywhere:
                CAP.want(store, capability,
                         "asked for by name and no fragment provides it")

            # D-62. A capability nobody provides is exactly the line the
            # Capability Gap Agent reads, so it is recorded as ok=false rather
            # than left out. A trail holding only the successes makes a gap
            # look like something nobody ever asked for.
            _audit().resolve(capability=capability, provider=None, ok=False,
                             revit=revit)
            return {
                "capability": capability,
                "providers": [],
                "known": known,
                "blocked_by_version": bool(anywhere),
                "revit": revit,
            }

        _audit().resolve(capability=capability,
                         provider=got.rows[0]["id"] if got.rows else None,
                         ok=True, revit=revit)
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

        # D-62. The trail only ever knew what reached Revit, so a request the
        # brain answered on its own left no record and the LIVE route share was
        # unmeasurable. THE SENTENCE IS NOT RECORDED - the route is what the
        # report reads, and `request` would put the user's own words into a
        # file that is append-only and never pruned.
        #
        # `excluded` goes in because D-52 is the rule this trail would break in
        # its own turn: a line naming what was found and nothing about what the
        # version wall removed is a count of the wrong thing.
        _audit().lookup(route=answer.route, capability=capability,
                        provider=answer.fragment_id,
                        candidates=len(candidates), excluded=len(excluded))

        return {
            "request": request,
            "route": answer.route,
            "capability": capability,
            "provider": answer.fragment_id,
            "note": answer.note,
            "autorun": bool(getattr(answer, "autorun", False)),
            "candidates": candidates,
            "excluded": [{"id": e.id, "reason": e.reason} for e in excluded],
            # WHICH BACKENDS ANSWERED, THROUGH THE SEAM A HOST ACTUALLY USES.
            #
            # heron_retrieve's command line prints both and this path printed
            # neither, so a caller could not tell a fusion-only answer from one
            # a cross-encoder had re-read - which is R-41's "degrades but SAYS
            # SO" holding in the one place nobody looks and failing in the one
            # place everybody does. The same shape as the citation, found the
            # same way, by a review.
            #
            # AND IT IS ASKED ABOUT THIS ANSWER, not about the machine. The
            # first version called backend() AFTER the search and reported what
            # was AVAILABLE - so an identity or cache short circuit, which runs
            # neither route, still claimed both had answered, and a warm-up
            # finishing mid-request could label a lexically answered query
            # "model". Found by a review 2026-09-11.
            "backends": _backends(answer),
            "revit": revit,
        }


# The routes that answer WITHOUT searching. heron_search.short_circuit() and
# recall() return a fragment by exact phrasing or by what was asked before -
# no nearness, no fusion, nothing to re-rank.
UNSEARCHED = ("identity", "cache", "nothing", "empty", "unindexed")


def _backends(answer=None):
    """Which optional backends answered THIS request. Never raises.

    Both are optional by construction and both report absence rather than
    failing, so asking them cannot be allowed to fail either.

    THE RUN RECORDS IT, THIS ONLY READS IT BACK. Two earlier versions asked
    the backends themselves, after the search, and each time the answer was a
    fact about the MACHINE dressed as a fact about the answer:

      * the first ignored the route, so an identity or cache short circuit -
        which searches nothing - still reported both as having answered
      * the second gated on the route, which fixed only that: a warm-up
        finishing between RETRIEVE.find() and this line still labelled a
        lexical, fusion-only result "model", and a loaded re-ranker whose
        scores() returned None was still reported as having re-read the
        shortlist

    heron_retrieve.ran() now records what produced a rank, at the moment it
    produced it, and hangs it on the Answer. There is nothing left here to get
    wrong. Found by two reviews, 2026-09-11.
    """
    route = getattr(answer, "route", None)
    recorded = getattr(answer, "backends", None)
    if recorded:
        return dict(recorded)
    if route is None or route in UNSEARCHED:
        return {"nearness": "not used",
                "nearness_why": "this answer came back on the %s route, which "
                                "does not search" % (route or "short-circuit"),
                "rerank": "not used",
                "rerank_why": "there was no shortlist to re-read"}
    # A SEARCHING ROUTE THAT RECORDED NOTHING. Not reachable from the routes in
    # this repository today, and saying so beats naming a backend on a run
    # nothing was recorded about.
    return {"nearness": "not said",
            "nearness_why": "the %s route did not record which backends ran"
                            % route,
            "rerank": "not said",
            "rerank_why": "the %s route did not record which backends ran"
                          % route}


class ContextRefused(Exception):
    """The Context Manager declined to assemble, and why.

    A REFUSAL IS NOT A FAULT, and the server could not tell them apart until
    this existed. `heron_context.assemble()` raises `OverBudget` when a part
    falls outside the path's budget and `SourceMissing` when a path needs
    something this installation has not got - both are ANSWERS. Anything else
    coming out of it is a bug.

    Catching `Exception` in the tool and calling all of it a refusal was the
    first shape, and it is the failure this whole night kept finding in other
    people's code: a message that misdescribes what happened. A TypeError would
    have been reported to the caller as *"Heron refused to assemble that
    context"*, which is a sentence about a decision Heron never made.

    The server cannot name `OverBudget` itself without importing heron_context,
    which would put brain internals back in the file that talks to the user -
    the split this seam exists to keep. So the seam translates.
    """


def _context_module():
    """heron_context, or BrainUnavailable saying what to install.

    A SEPARATE helper rather than a seventh member of _brain()'s tuple, and the
    reason is mechanical: six call sites unpack that tuple POSITIONALLY, and a
    positional unpack that is one short raises at run time in whichever tool
    happened to be called first - not at import, where it would be found. One
    more function is cheaper than six edits that all have to be right.
    """
    try:
        import heron_context as CONTEXT
    except ImportError as exc:
        raise BrainUnavailable(
            "Heron's Context Manager needs PyYAML and it is not installed: %s\n"
            "Install it with:  pip install --user pyyaml" % exc)
    return CONTEXT


def context(request, path=None, revit=None, full=False, depth=None,
            project=None):
    # `project` HERE IS THE NAME, NOT THE KEY. This packet never opens a
    # project store - _Open opens global - and heron_context._situation
    # renders it straight into "project: %s" for a person to read. Passing the
    # Project Information UniqueId put an opaque identifier on the one line
    # that exists to say which building this is. The key belongs where a store
    # is chosen; see standards() and check_answer(). Found by a review
    # 2026-09-11.
    """
    What one agent would be given for one request, and nothing else.

    docs/19 sections 1 and 2 reaching the host. The host asks for a request to
    be assembled; Heron decides what may be carried and REFUSES anything
    outside the path's budget, which is the half worth having - docs/19 s1:
    an over-fed agent does not fail loudly, it attends to the wrong thing and
    returns a confident, plausible, wrong answer.

    `path` IS THE HOST'S TO CHOOSE. D-01 puts intent classification there, and
    this call does not second-guess it. Passing None derives only the
    structural case - a request that is a fragment's own declared phrasing IS
    the cached path - and says it assumed the rest.

    THIS EXISTS BECAUSE OF WHAT ITS OWN TOOLING FOUND. On 2026-09-09
    tools/measure-routes.py established that heron_search.remember() is called
    from a test and from nowhere else, so the utterance cache can never fill
    (Q-43). A Context Manager reachable only from a command line would have
    been the same shape of mistake in the same week, and this file's own
    docstring already says what that costs: complete, tested, and unreachable
    from a conversation is not what "built" was meant to mean.
    """
    CONTEXT = _context_module()

    # DEPTH IS THE HOST'S TOO, for the same reason `path` is (D-01). How much
    # of a neighbour a host needs depends on what it is about to do with it,
    # and that is a judgement about the task - which is the host's half of the
    # boundary. Passing nothing means full, so a caller written before depth
    # existed gets byte-identical packets.
    wanted = CONTEXT.FULL
    if depth:
        by_name = dict((v, k) for k, v in CONTEXT.DEPTH_NAMES.items())
        if depth not in by_name:
            raise ValueError(
                "'%s' is not a depth. There are three: %s. Leave it out for "
                "the whole of every part." % (depth, ", ".join(sorted(by_name))))
        wanted = by_name[depth]

    with _Open() as store:
        try:
            # `project` is the pinned document's name and it comes from the
            # SERVER, which is the only side that knows it. Without it the
            # situation part said "project: none named" on every single
            # request, while the add-in had known the name since the first
            # count_elements - a packet quietly less true than it could be.
            # Found by Codex on PR #44, 2026-09-09.
            #
            # `scope` is NOT passed and that is deliberate rather than
            # forgotten: _Open always opens the GLOBAL store, so a project
            # scope would change how every brain tool resolves knowledge, not
            # just this one. That belongs with a real project store to test
            # against - see HANDOVER.
            ctx = CONTEXT.assemble(store, request, path=path, revit=revit,
                                   project=project, depth=wanted)
        except (CONTEXT.OverBudget, CONTEXT.TooDeep,
                CONTEXT.SourceMissing) as why:
            # A REFUSAL IS RECORDED, not dropped. A trail holding only the
            # assemblies makes a path that refuses every time - STANDARDS, on
            # every installation today - look like a path nobody used.
            _audit().context(path=path, depth=depth, parts=0, characters=0,
                             refused=str(why))
            raise ContextRefused(str(why))

        _audit().context(path=ctx.path,
                         depth=CONTEXT.DEPTH_NAMES[ctx.depth],
                         parts=len(ctx.parts), characters=ctx.size)
        return {
            "request": ctx.request,
            "path": ctx.path,
            "assumed_path": ctx.assumed_path,
            "why": CONTEXT.WHY[ctx.path],
            "budget": list(CONTEXT.BUDGET[ctx.path]),
            "carried": ctx.kinds(),
            "size": ctx.size,
            "depth": CONTEXT.DEPTH_NAMES[ctx.depth],
            "parts": [{"kind": p.kind, "name": p.name, "source": p.source,
                       "why": p.why, "size": p.size,
                       "depth": CONTEXT.DEPTH_NAMES[p.depth],
                       # THE CITATION, WITHOUT WHICH THE WHOLE CHAIN BREAKS AT
                       # THIS SEAM. The chunk id exists on the in-process Part
                       # and was not serialized here, so a host calling this -
                       # the only way production reaches heron_context - could
                       # not produce the [chunk id] marker heron_ground reads,
                       # and the binding R-63 and R-64 exist to create was
                       # lost exactly where it had to survive. Omitted when
                       # there is none, so a fragment part is unchanged.
                       **({"citation": p.citation} if p.citation else {}),
                       # Present only when something was actually left out.
                       # A part carrying all of itself says nothing extra.
                       "cut": p.cut,
                       "body": p.body if full else None}
                      for p in ctx.parts],
            "not_carried": [{"kind": k, "reason": r} for k, r in ctx.refused],
            "revit": revit,
        }


def _check_across(GROUND, CONTEXT, draft, request, wanted,
                  path=None, revit=None, project=None, project_name=None):
    """One grounding check per scope, combined by claim. No packet is pooled.

    THE COMBINING RULE IS ONE LINE AND THE REST IS BOOKKEEPING: a claim's
    verdict is whichever scope RESOLVED it. UNRESOLVED means "this packet does
    not carry that chunk", which every scope but one will say about any cited
    clause, so it is the only verdict that loses to another. Nothing else is
    reconciled - a FLAGGED claim stays flagged, because a scope that HAS the
    chunk and disagrees with the sentence is the answer.

    A scope that refuses (empty, unindexed, nothing indexed covering this) is
    recorded and skipped. The check refuses only when EVERY named scope did,
    and then it says which said what - one refusal per scope, because "it
    refused" without naming the scope sends somebody to the wrong store.
    """
    reports, refusals, asked = [], [], []
    for scope in wanted:
        store = _ready_scope(scope, project)
        if store is None:
            # A SCOPE THAT COULD NOT BE OPENED IS NAMED, NOT DROPPED. Skipping
            # it silently meant `scopes="company,proejct"` - one letter short -
            # produced an `ok` COMPANY-ONLY report with nothing saying the
            # other named source was never looked at. The same shape as the
            # mistyped ingest flag, one tool along: a typo turning a check off
            # and reporting success. Found by a review 2026-09-11.
            refusals.append("%s: not a knowledge scope, or it could not be "
                            "opened - nothing was checked against it" % scope)
            continue
        try:
            try:
                packet = CONTEXT.assemble(store, request,
                                          path=path or CONTEXT.STANDARDS,
                                          revit=revit,
                                          project=project_name or project)
            except (CONTEXT.OverBudget, CONTEXT.TooDeep,
                    CONTEXT.SourceMissing) as why:
                refusals.append("%s: %s" % (scope, why))
                continue
            reports.append(GROUND.check(draft, packet))
            asked.append(scope)
        finally:
            store.close()

    if not reports:
        _audit().record("knowledge.ground", False,
                        fields={"scope": ",".join(wanted),
                                "refused": " | ".join(refusals)})
        raise ContextRefused(
            "every scope named refused to supply the clauses this check needs."
            + ("\n  " + "\n  ".join(refusals) if refusals else
               " None of them could be opened at all."))

    first = reports[0]
    claims = list(first.claims)
    for other in reports[1:]:
        for i, claim in enumerate(other.claims):
            # Belt and braces: the same draft produces the same sentences in
            # the same order every time, so the indexes line up. If a future
            # change ever makes them not, this stops rather than pairing a
            # verdict with somebody else's sentence.
            if i >= len(claims) or claims[i].sentence != claim.sentence:
                break
            if claims[i].verdict == GROUND.UNRESOLVED:
                claims[i] = claim

    combined = GROUND.Report(claims, first.thresholds,
                             sum(r.sources for r in reports))
    _audit().record("knowledge.ground", combined.ok,
                    fields={"scope": ",".join(asked),
                            "refused": " | ".join(refusals)},
                    numbers={"claims": len(combined.claims),
                             "checked": combined.checked,
                             "flagged": len(combined.flagged),
                             "reversed": len(combined.reversed_claims),
                             "uncited": len(combined.uncited)})
    lines = combined.lines()
    if refusals:
        lines = list(lines) + [
            "",
            "%d of the scopes named supplied no clauses, so NOTHING WAS "
            "CHECKED against them:" % len(refusals)]
        lines.extend("  %s" % why for why in refusals)
    return {
        "ok": combined.ok,
        "checked": combined.checked,
        "sentences": len(combined.claims),
        "sources": combined.sources,
        "lines": lines,
        "claims": [{"sentence": c.sentence, "verdict": c.verdict,
                    "kind": c.kind, "ratio": c.ratio,
                    "threshold": c.threshold, "added": c.added,
                    "citation": c.citation} for c in combined.claims],
    }


def _ready_scope(scope, project=None):
    """Open one named scope, reconciled and indexed, or None. Never raises.

    BUILT ONCE, because the sequence is a rule rather than a convenience:
    reconcile BEFORE indexing, because reconciling changes the rows the
    indexes are built from. standards() and check_answer() both need it, and
    00-structure.md s3.7's rule about a measurement being built once is the
    same rule about an ordering - two copies of it become two orderings that
    drift.

    It returns an OPEN store and the caller closes it. That is deliberate:
    every multi-scope caller in this file closes each store before opening the
    next, and a helper that closed it here would have nothing to hand back.
    """
    try:
        import heron_scope as SCOPE
        import heron_search as SEARCH
        import heron_embed as EMBED
    except ImportError:
        return None
    if scope not in SCOPE.SCOPES:
        return None
    if scope == SCOPE.PROJECT and not project:
        return None
    try:
        store = SCOPE.open_scope(scope, project)
    except Exception:
        return None
    try:
        _reconcile(store)
        SEARCH.index_chunks(store)
        EMBED.index_chunks(store)
    except Exception:
        # An index that cannot be built is a scope that answers "unindexed" by
        # name, which find_documents already reports. It is not a reason to
        # fail every other scope, and the store is still usable.
        pass
    return store


def check_answer(draft, request, path=None, revit=None, project=None,
                 scopes=None, project_name=None):
    """A draft answer, and the request it answers. A grounding report, out.

    THE HALF OF THE FABRICATION CHECK THAT DID NOT EXIST IN PRODUCTION.
    heron_ground.check() was complete and tested and reachable from a command
    line with a file on disk - which is to say, from nowhere a conversation
    goes. A repo-wide search on 2026-09-11 found no tool, hook or host
    instruction that called it, so a standards answer could be shown with the
    pre-display gate never having run. This file's own docstring already names
    that shape of mistake; the review was right that it had it.

    D-01 IS NOT BROKEN BY THIS AND IT IS THE REASON FOR THE SIGNATURE. The
    brain does not write the answer and does not see one until the host hands
    it back. So the host drafts, calls this with the draft and the SAME
    request, and Heron reassembles the packet and reports. It returns a report
    and NEVER a rewrite (R-53).

    THE PACKET IS REASSEMBLED RATHER THAN REMEMBERED, deliberately. A packet
    held between two calls is a session, and a session is state that can go
    stale and be pointed at the wrong answer. Reassembling is cheap, and it
    checks the draft against what the store says NOW.

    `scopes` NAMES WHERE THE EVIDENCE CAME FROM, and without it this check was
    unusable for the workflow it was built for. It opened the GLOBAL store and
    only the global store, so a draft written from a heron_standards answer -
    the multi-scope tool, whose whole point is company and project - cited
    chunks the global store has never heard of. Every marker came back
    UNRESOLVED, or the reassembly refused outright because global holds no
    documents. A gate that refuses every honest answer teaches people to stop
    calling it. Found by a review 2026-09-11.

    EACH SCOPE IS CHECKED ON ITS OWN AND THE VERDICTS ARE COMBINED. No packet
    ever holds two scopes' clauses: the stores are opened one at a time and
    closed before the next, which is the discipline heron_conflict keeps and
    for the same reason (D-33, Golden Rule 5). A chunk id belongs to exactly
    one store, so a claim resolves in at most one run and UNRESOLVED elsewhere
    is the absence of that chunk, not a judgement about it.

    ONE LIMIT, REPORTED RATHER THAN DISCOVERED: checking per scope cannot see
    a citation that is ambiguous ACROSS scopes. Two documents in two different
    stores sharing clause "4.1" each resolve cleanly in their own run, and a
    draft citing the bare locator is checked against one of them without the
    ambiguity being raised. Within a scope it is still raised.
    """
    CONTEXT = _context_module()
    try:
        import heron_ground as GROUND
    except ImportError as exc:
        raise BrainUnavailable(
            "Heron's grounding check needs the brain modules and they are not "
            "importable: %s" % exc)

    wanted = [w.strip().lower() for w in (scopes or []) if w and w.strip()]
    if wanted:
        return _check_across(GROUND, CONTEXT, draft, request, wanted,
                             path=path, revit=revit, project=project,
                             project_name=project_name)

    with _Open() as store:
        try:
            # THE NAME, because this packet's `project` is only ever rendered
            # for a person - _Open opens the global store and nothing here
            # chooses one by key.
            packet = CONTEXT.assemble(store, request,
                                      path=path or CONTEXT.STANDARDS,
                                      revit=revit,
                                      project=project_name or project)
        except (CONTEXT.OverBudget, CONTEXT.TooDeep,
                CONTEXT.SourceMissing) as why:
            # TRANSLATED, THE SAME WAY context() DOES IT. A refusal from the
            # packet is an ANSWER - "nothing indexed covers this" is the
            # sentence a caller needs. Without this the tool above had to catch
            # every Exception to show it, which meant a real defect came back
            # wearing the words of a normal answer.
            _audit().record("knowledge.ground", False,
                            fields={"scope": store.scope, "refused": str(why)})
            raise ContextRefused(str(why))
        report = GROUND.check(draft, packet)
        _audit().record("knowledge.ground", report.ok,
                        fields={"scope": store.scope},
                        numbers={"claims": len(report.claims),
                                 "checked": report.checked,
                                 "flagged": len(report.flagged),
                                 "reversed": len(report.reversed_claims),
                                 "uncited": len(report.uncited)})
        return {
            "ok": report.ok,
            "checked": report.checked,
            "sentences": len(report.claims),
            "sources": report.sources,
            "lines": report.lines(),
            "claims": [{"sentence": c.sentence, "verdict": c.verdict,
                        "kind": c.kind, "ratio": c.ratio,
                        "threshold": c.threshold, "added": c.added,
                        "citation": c.citation} for c in report.claims],
        }


def _with_text(asked, project=None):
    """One scope's candidates, each carrying the clause a person has to read.

    The chunk text is not on the candidate - find_documents() deliberately
    returns pointers - so the scope is reopened to fetch it. One store at a
    time, closed before the next, which is the same discipline heron_conflict
    keeps and for the same reason.

    GOLDEN RULE 19 IS APPLIED HERE, NOT PROMISED BY THE RENDERER. The
    heron_standards response ends with "content, never instruction (Golden
    Rule 19)" and, until a review found it 2026-09-11, nothing on that path
    ran the guard: heron_context.screen() and the visible flag it raises were
    reached only through heron_context.build(), which this seam does not use.
    So an ingested clause carrying instruction-shaped text went to the host
    with a sentence claiming it had been checked. A claim about a guard, with
    no guard behind it, is worse than no claim.

    Each candidate therefore carries:

      findings     what the screen saw in the clause OR in its metadata, so
                   the renderer can raise the same flag the packet path does
      safe_*       the document-derived fields with their whitespace
                   collapsed and delimited, for the lines that do not quote

    NOTHING IS TRUNCATED and nothing is dropped (R-82) - trimming is what
    lets a payload be padded past a check.
    """
    if asked.answer is None:
        return []
    out = []
    try:
        import heron_scope as SCOPE
    except ImportError:
        return [dict(c) for c in asked.answer.candidates]
    try:
        import heron_context as CONTEXT
    except ImportError:
        CONTEXT = None
    # THE KEY IS PASSED IN, not read off the label. Asked.project is what the
    # Librarian SHOWS; the key is what names the store. They happen to be the
    # same value today and reading one for the other is how they stop being.
    try:
        store = SCOPE.open_scope(asked.scope, project)
    except Exception:
        return [dict(c) for c in asked.answer.candidates]
    try:
        for hit in asked.answer.candidates:
            got = dict(hit)
            row = store.execute("SELECT text FROM chunks WHERE id = ?",
                                (hit["id"],)).fetchone()
            got["text"] = row["text"] if row else None
            got["findings"] = []
            if CONTEXT is not None:
                # EVERY DOCUMENT-DERIVED FIELD, not only the body - the same
                # set heron_context._standard_parts screens, and for the same
                # reason: the title line is removed from the chunks during
                # ingestion, so a body scan can never see it.
                seen = CONTEXT.screen(hit["id"], "\n".join(
                    str(bit) for bit in (got["text"], hit.get("document"),
                                         hit.get("locator"),
                                         hit.get("heading_path")) if bit))
                got["findings"] = list(seen.findings)
                got["safe_document"] = CONTEXT.as_metadata(hit.get("document"))
                got["safe_locator"] = CONTEXT.as_metadata(hit.get("locator"))
                got["safe_path"] = CONTEXT.as_metadata(hit.get("path"))
            out.append(got)
    finally:
        store.close()
    return out


def standards(request, scopes, project=None, limit=5,
              project_name=None):
    """Each named scope asked on its own, and where their numbers disagree.

    THE MULTI-SCOPE PATH HAD NO SEAM AT ALL, and that covered two stages.
    `heron_retrieve.librarian()` (Stage 4) and `heron_conflict.disagreements()`
    (Stage 8) were both callable only from a command line and their own tests -
    so the scope wall Stage 4 built and the disagreement Stage 8 surfaces were
    invisible to any host. A review found it on the conflict half; the librarian
    half had been sitting there since Stage 4 and nobody had looked.

    NOTHING IS POOLED AND THAT IS THE LIBRARIAN'S GUARANTEE, NOT THIS
    FUNCTION'S. Each scope is asked separately and answers under its own label;
    this returns a list of them, never a merged one. D-33 and Golden Rule 5.

    R-29 IS KEPT HERE. Each scope's chunk indexes are built before it is asked,
    for the same reason `_Open` does it for the global scope: a document put in
    through the CLI must not stay unsearchable until somebody remembers a
    command.
    """
    try:
        import heron_scope as SCOPE
        import heron_retrieve as RETRIEVE
        import heron_conflict as CONFLICT
    except ImportError as exc:
        raise BrainUnavailable(
            "Heron's knowledge layer needs PyYAML and it is not installed: %s\n"
            "Install it with:  pip install --user pyyaml" % exc)

    wanted = [s.strip().lower() for s in (scopes or []) if s.strip()]
    if not wanted:
        raise ValueError(
            "name at least one scope. There are %s - and naming TWO is what "
            "makes a disagreement visible at all."
            % ", ".join(SCOPE.SCOPES))

    # RECONCILED AND INDEXED FIRST, one scope at a time, each closed before
    # the next.
    #
    # THE MAINTENANCE PASS _Open GIVES THE GLOBAL SCOPE, and until a review
    # found it 2026-09-11 this path had none. _Open only ever opens `global`,
    # so a company standard edited on disk, or a project store deleted as the
    # documented safe recovery action, stayed stale or stayed empty through
    # every served standards request until somebody ran maintenance by hand -
    # on exactly the scopes this tool exists to read.
    for scope in wanted:
        store = _ready_scope(scope, project)
        if store is not None:
            store.close()

    # ONE SEARCH PER SCOPE, SHARED. Calling disagreements() without `asked`
    # made it ask the Librarian again, so every served standards request
    # searched - and, where a cross-encoder is installed, re-ranked - every
    # scope twice. Worse than the waste: two shortlists that a finishing
    # warm-up or a changed file could make different, so the disagreement shown
    # could be about clauses other than the ones listed above it.
    asked = RETRIEVE.librarian(request, scopes=wanted, project=project,
                               limit=limit, project_name=project_name)
    found = CONFLICT.disagreements(request, scopes=wanted, project=project,
                                   limit=limit, asked=asked,
                                   project_name=project_name)

    # THE FOLDER INDEX LEARNS THE NAME WHILE BOTH ARE IN HAND. heron_scope
    # keeps projects/labels.json so that somebody opening the folder can tell
    # which .db is which - the key is the filename and the label is the only
    # readable thing about it. Nothing on the served path had ever written to
    # it, so every project store was an unreadable filename on disk.
    if project and project_name:
        try:
            SCOPE.remember_label(project, project_name)
        except Exception:
            # A label nobody could write loses nothing that matters: the key
            # is still the filename. Never let it fail a question.
            pass

    _audit().record("knowledge.standards", True,
                    fields={"scopes": ",".join(wanted)},
                    numbers={"answered": len([a for a in asked if a.answer]),
                             "disagreements": len(found)})

    return {
        "request": request,
        "scopes": [{
            "scope": a.scope,
            "label": a.label,
            "skipped": a.skipped,
            "route": a.answer.route if a.answer else None,
            "note": a.answer.note if a.answer else None,
            # THE CLAUSE ITSELF, not only its metadata. The tool's own closing
            # line said "both clauses are above, each with its own citation"
            # while the payload carried a title, a locator and a ranking
            # reason - no text to read and no path to open. A sentence that
            # describes something the response does not contain. Found by a
            # review 2026-09-11, in wording written the same day.
            "candidates": _with_text(a, project),
        } for a in asked],
        "disagreements": [{
            "unit": d.unit,
            "sources": ["%s - %s" % (label, doc) for label, doc in d.sources],
            "same_locator": d.same_locator,
            "would_be_preferred": d.would_be_preferred,
            "sentence": d.sentence(),
            "values": [{"label": v.label, "value": v.value, "unit": d.unit,
                        "document": v.document, "locator": v.locator,
                        "path": v.path, "chunk": v.chunk}
                       for v in d.values],
        } for d in found],
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
