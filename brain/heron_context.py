# Heron-Agent:  HERON-RAG-CTX-007
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Context Manager. What an agent is given, and nothing else.

    python brain/heron_context.py "select all ducts" --revit 2024
    python brain/heron_context.py "make a new duct type" --path generation

docs/19 sections 1 and 2, which specified this and were never implemented -
docs/32 s4.1 is where that gap is recorded, and it was the largest one found.

WHY THIS IS WORTH BUILDING AT ALL, in docs/19's own words:

    AI systems do not fail loudly when over-fed context - they fail QUIETLY, by
    attending to the wrong thing and producing a confident, plausible, wrong
    answer. In a system that then writes to a live project model, that is the
    dangerous failure mode.

So the useful thing here is not gathering. Gathering is easy and every piece
already exists. The useful thing is REFUSING - a named list of what each kind
of request may carry, and an error rather than a quiet extra when something
outside it is asked for.

IT DOES NOT CLASSIFY WHAT THE USER MEANT. D-01 PUT THAT IN THE HOST.
--------------------------------------------------------------------
HERON-ORC-INT-002 is host-provided and tools/check-metadata.py prints it every
run as having no file here on purpose. So `path` is an INPUT. The only thing
derived here is structural and needs no judgement: if the request is a
fragment's exact declared phrasing, the short circuit hit and the path is
CACHED. Everything else defaults to SIMPLE and the packet SAYS it was assumed
rather than classified, so nobody reads a default as a decision.

Building an intent matcher here would duplicate the host's, disagree with it
eventually, and put a model call inside what docs/02 s6 requires to stay T1 -
the same argument mcp/server/heron_brain.py already makes for itself.

THE BUDGET IS A LIST OF PARTS, NOT A NUMBER OF TOKENS
------------------------------------------------------
docs/19 s2 sets the budgets in exactly that shape - "intent + active project +
Revit version + one matched capability" - and that is the right shape for
Heron rather than a limitation:

  * Heron has no tokeniser and would have to invent one. A token count from a
    guessed tokeniser is a number that looks authoritative and is not, which
    is what D-33 exists to refuse.
  * The host counts tokens, and D-58 has just finished establishing that the
    host is where per-request cost lives. A second, worse count here would be
    a meter reading something nobody uses.
  * A parts list is CHECKABLE. "This packet contains a `neighbour` and the
    SIMPLE budget does not allow one" is a fact. "This packet is 3,400 tokens"
    is a measurement waiting for a threshold somebody will raise.

Size in characters is reported because it costs nothing to report and somebody
will want it. It is never a limit. Nothing here refuses on size.

EXCEEDING THE BUDGET RAISES. docs/19 s2:

    If a context assembly exceeds its budget, that is a bug in retrieval, not
    a reason to raise the budget.

THE REQUEST IS NEVER COMPRESSED, AND THAT IS THE ONE HARD RULE HERE
--------------------------------------------------------------------
docs/19 s2 wants compression. This module implements none, deliberately, and
the reason is docs/05 s4: BIM requests are full of tokens that must match
exactly and that every compressor handles worst - OST_DuctCurves,
RBS_DUCT_BOTTOM_ELEVATION, a shared-parameter GUID, "Revit 2024". A compressor
that shortens one of those has destroyed the only part of the sentence that
was load-bearing.

So the request text crosses verbatim, and compression - when somebody builds
it - may operate on the RETRIEVED parts and must never touch `request`. That
constraint is asserted in tests/test_context.py rather than promised here.

A PATH WHOSE SOURCE DOES NOT EXIST IS REFUSED BY NAME
------------------------------------------------------
STANDARDS asks for "the specific standard clauses cited, not the whole
standard". A scope store holds `fragments` and `meta` and no clause table -
read from heron_scope, not assumed. So that path raises and says which source
is missing, rather than returning a packet that is silently three-quarters of
what it claims to be. Degrading quietly is how a caller comes to trust a
smaller answer than it asked for.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import heron_scope as SCOPE                      # noqa: E402
import heron_search as SEARCH                    # noqa: E402
import heron_retrieve as RETRIEVE                # noqa: E402
import heron_capability as CAPABILITY            # noqa: E402
# For repo_relative() and nothing else. `os.path.relpath` RAISES on Windows
# across drives, and a fragment folder is not always inside the checkout - the
# same case that made _fragment_dir() report a present fragment as missing.
# heron_fragment already owns the one answer to that question, and heron_scope
# already imports it, so this adds no dependency and no second implementation.
import heron_fragment as FRAG                     # noqa: E402

FRAGMENTS = os.path.join(ROOT, "brain", "fragments")


# ---------------------------------------------------------------------------
# The four paths of docs/19 s2, and what each may carry
# ---------------------------------------------------------------------------

CACHED = "cached"
SIMPLE = "simple"
STANDARDS = "standards"
GENERATION = "generation"

# Part kinds. Named constants rather than strings at the call site, because a
# typo in a budget check that silently allows everything is the one bug this
# module cannot afford.
REQUEST = "request"          # what the user said. Verbatim, always, never cut
SITUATION = "situation"      # active project, Revit release, scope
CAPABILITY_PART = "capability"   # the one matched capability and its provider
EXCLUDED = "excluded"        # what the version wall removed, and why
STANDARD = "standard"        # the clauses cited - source does not exist yet
NEIGHBOUR = "neighbour"      # the closest existing fragment
TESTS = "tests"              # that fragment's declared cases
API = "api"                  # the API surface it uses

# docs/19 s2's table, made enforceable. Order is the order a reader gets them.
BUDGET = {
    CACHED:     (REQUEST, SITUATION, CAPABILITY_PART),
    SIMPLE:     (REQUEST, SITUATION, CAPABILITY_PART, EXCLUDED),
    STANDARDS:  (REQUEST, SITUATION, CAPABILITY_PART, EXCLUDED, STANDARD),
    GENERATION: (REQUEST, SITUATION, CAPABILITY_PART, EXCLUDED,
                 NEIGHBOUR, TESTS, API),
}

# Why each path exists, in the words docs/19 s2 uses. Printed with the packet
# so the budget is never a bare tuple somebody has to go and look up.
WHY = {
    CACHED:     "cached utterance to a known capability - no model call at all",
    SIMPLE:     "a simple BIM task - intent, project, release, one capability",
    STANDARDS:  "a standards check - the above plus the clauses CITED, never "
                "the whole standard",
    GENERATION: "code generation - the above plus the closest fragment, its "
                "tests, and the API surface it uses",
}


class OverBudget(Exception):
    """A part outside the path's budget was asked for.

    Raised rather than dropped, and rather than widening the budget. docs/19 s2
    is explicit that this means retrieval is wrong, and a caller that silently
    got less than it asked for cannot tell that from a caller that asked for
    less.
    """


class SourceMissing(Exception):
    """A path needs something this installation does not have.

    Named, so the answer is "there is no clause store yet" rather than a packet
    that looks complete and is not.
    """


class Part(object):
    """One piece of context, and where it came from.

    `source` is not decoration. docs/19 s1 asks for traceability - "know where
    important context came from" - and the moment a wrong answer has to be
    explained, the question is always which piece was wrong and who supplied
    it. A part that cannot say is a part nobody can check.
    """

    def __init__(self, kind, name, body, source, why):
        self.kind = kind
        self.name = name
        self.body = body
        self.source = source
        self.why = why

    @property
    def size(self):
        """Characters. A fact that is reported, never a limit that refuses."""
        return len(self.body if isinstance(self.body, str) else repr(self.body))

    def __repr__(self):
        return "<Part %s %s %dch>" % (self.kind, self.name, self.size)


class Context(object):
    """What one agent is given for one request, and nothing else."""

    def __init__(self, request, path, assumed_path):
        self.request = request
        self.path = path
        self.assumed_path = assumed_path
        self.parts = []
        self.refused = []

    def add(self, part):
        """Add a part, or refuse it because this path may not carry it."""
        if part.kind not in BUDGET[self.path]:
            raise OverBudget(
                "a '%s' part was assembled for the %s path, which may carry "
                "only %s. docs/19 s2: exceeding the budget is a bug in "
                "retrieval, not a reason to raise the budget."
                % (part.kind, self.path, ", ".join(BUDGET[self.path])))
        self.parts.append(part)
        return part

    def note_refused(self, kind, reason):
        """Something the budget allows but this request did not need or have."""
        self.refused.append((kind, reason))

    @property
    def size(self):
        return sum(p.size for p in self.parts)

    def kinds(self):
        return [p.kind for p in self.parts]

    def __repr__(self):
        return "<Context %s %d part(s) %dch>" % (self.path, len(self.parts),
                                                 self.size)


# ---------------------------------------------------------------------------
# Assembly
# ---------------------------------------------------------------------------

def _situation(revit, project, scope):
    """The cheap facts, gathered once.

    Deliberately small. docs/19 s1 lists "what project is active" and "what
    Revit version is active" and stops there - not the model, not the view,
    not the selection. HERON-REVIT-CTX-007 gathers those and it needs Revit
    open; this runs with no Revit at all and must not pretend otherwise.
    """
    lines = [
        "Revit release: %s" % (revit if revit is not None else
                               "not stated - no version filter was applied"),
        "project: %s" % (project or "none named"),
        "knowledge scope: %s" % (scope or SCOPE.GLOBAL),
    ]
    return "\n".join(lines)


def _fragment_dir(store, fragment_id):
    """Where a fragment lives on disk, from the STORE rather than from disk.

    THE FIRST VERSION SCANNED EVERY fragment.yaml IN THE LIBRARY to find one
    folder by id - 360 YAML parses per call, and it made the `generation` path
    435 ms against 3 ms for the others. The answer was one SQL lookup away the
    whole time: `heron_scope` has stored a repo-relative `folder` on every
    fragment row since Step 8.

    Found with tools/measure-brain.py, which was written earlier the same night
    for exactly this - and which the first version of this function would have
    failed on its first run. A tool is only worth building if it is then
    pointed at your own work.

    Reading it from the store rather than from disk is also the rule
    check-routing.py already follows and states: the store is what retrieval
    ranked, so a fragment edited but not re-indexed must be looked up as the
    search actually saw it.
    """
    for row in store.fragments():
        if row["id"] == fragment_id:
            folder = row["folder"]
            if not folder:
                return None
            # JOINED, NOT SPLIT-AND-JOINED, and the difference is a fragment
            # kept outside the checkout. heron_fragment.repo_relative() returns
            # an ABSOLUTE path when there is no relative form - a library beside
            # the user's data while Heron sits on another drive - and its
            # docstring says callers may join the result back onto ROOT because
            # os.path.join discards everything before an absolute component.
            #
            # Splitting on "/" first defeats exactly that: "/home/x/frag"
            # becomes ROOT + "/home/x/frag". The folder is then not found, and
            # the packet reports "in the store but not on disk in this working
            # tree" - a plausible sentence about a fragment that is on disk and
            # is fine.
            full = os.path.join(ROOT, folder)
            return full if os.path.isdir(full) else None
    return None


def _capability_part(store, fragment_id, revit, why):
    """The matched capability and who provides it - never the fragment alone.

    Step 12's rule reaching its customer: the caller is told WHAT can be done
    and, as evidence only, who would do it. A packet naming a fragment id as
    the thing to ask for next would put a call site back in the business of
    knowing which fragment does what.
    """
    row = None
    for candidate in store.fragments():
        if candidate["id"] == fragment_id:
            row = candidate
            break
    if row is None:
        return None

    name = row["capability"]
    got = CAPABILITY.resolve(store, name, revit=revit)
    body = [
        "capability: %s" % name,
        "matched fragment: %s (%s)" % (row["id"], row["status"]),
        # The capability's risk, not the matched fragment's: heron_capability
        # derives it as the HIGHEST any provider carries, so a caller deciding
        # whether to ask permission is told the worst case rather than the
        # case that happened to rank first today.
        "risk: %s" % ((got.risk if got else None) or row["risk"] or "not declared"),
        "best status among providers: %s" % (got.status if got else "none"),
        "providers for Revit %s: %s"
        % (revit if revit is not None else "any",
           ", ".join(got.providers) if got else "none"),
    ]
    return Part(CAPABILITY_PART, name, "\n".join(body),
                "derived from the fragment store", why)


def assemble(store, request, path=None, revit=None, project=None, scope=None):
    """Build the packet for one request.

    `path` is the caller's - D-01. Passing None derives only the structural
    case (a short circuit hit means CACHED) and otherwise assumes SIMPLE,
    recording that it was assumed.
    """
    SEARCH.ensure_tables(store)

    # THE VERSION WALL APPLIES TO THE SHORT CIRCUIT TOO, and the first version
    # of this file did not apply it - which is the one bug here that mattered.
    #
    # `short_circuit` answers from the identity table alone and knows nothing
    # about releases. `heron_retrieve.find()` filters its hit against
    # `eligible()` for exactly this reason and says why in its own words: the
    # wall does not have a door in it for convenience. This had one. On Revit
    # 2019, `find()` returned `nothing` and `assemble()` returned
    # FILTER_ELEMENTS_BY_CATEGORY - a fragment declared for 2020 and later,
    # handed over as a confident answer.
    #
    # That is the confident-wrong-retrieval failure this repository legislates
    # against harder than any other, committed in the module written to prevent
    # an agent being handed the wrong thing.
    allowed, excluded_by_the_walls = RETRIEVE.eligible(store, revit)
    offerable = set(row["id"] for row in allowed)

    assumed = False
    hit, _status = SEARCH.short_circuit(store, request)
    walled = bool(hit) and hit not in offerable
    if walled:
        hit = None
    if path is None:
        path = CACHED if hit else SIMPLE
        assumed = not hit
    if path not in BUDGET:
        raise ValueError(
            "'%s' is not a path. docs/19 s2 defines %s."
            % (path, ", ".join(sorted(BUDGET))))

    ctx = Context(request, path, assumed)

    # 1. What the user said. Verbatim, first, and never touched.
    ctx.add(Part(REQUEST, "the request", request, "the caller",
                 "what was asked, unaltered - a Revit token in it is the part "
                 "that must survive"))

    # 2. The cheap facts.
    ctx.add(Part(SITUATION, "situation", _situation(revit, project, scope),
                 "the caller and heron_scope",
                 "docs/19 s1: what project and what release are active"))

    # 3. The one matched capability.
    fragment_id, why = None, ""
    if hit:
        fragment_id = hit
        why = "the request is this fragment's own declared phrasing - one " \
              "lookup, no search, no model"
    elif path != CACHED:
        answer = RETRIEVE.find(store, request, revit=revit)
        fragment_id = answer.fragment_id
        why = "matched by the %s route: %s" % (answer.route, answer.note)
    elif walled:
        raise SourceMissing(
            "the CACHED path was asked for and this wording IS a fragment's "
            "declared phrasing - but that fragment is not declared for Revit "
            "%s. The version filter is a wall and it has no door in it for a "
            "good match: an incompatible fragment is absent, not demoted."
            % revit)
    else:
        raise SourceMissing(
            "the CACHED path was asked for, but this wording is not a "
            "fragment's declared phrasing and the utterance cache is empty "
            "(Q-43). Nothing can answer it in one lookup.")

    if fragment_id:
        part = _capability_part(store, fragment_id, revit, why)
        if part is not None:
            ctx.add(part)
        else:
            ctx.note_refused(CAPABILITY_PART,
                             "%s was matched but the store does not hold it"
                             % fragment_id)
    else:
        ctx.note_refused(CAPABILITY_PART, "nothing matched these words")

    # 4. What the walls removed. Traceability, and the reason a user is not
    #    left hunting for a fragment that is sitting right there.
    if EXCLUDED in BUDGET[path]:
        # Reusing the pass taken above for the wall, rather than taking a
        # second one. Two calls would be two answers to one question - cheap
        # here and wrong in principle, since a store changing between them
        # would produce a packet whose `excluded` list disagrees with the
        # filter its own capability was chosen through.
        excluded = excluded_by_the_walls
        if excluded:
            # Grouped by WHY, not listed one by one. eligible() excludes on
            # status as well as release, and an earlier version of this filtered
            # for the word "Revit" - which would have reported 0 exclusions on a
            # day when 218 fragments were held back for being DRAFT. Two
            # different walls, both worth knowing about, and neither may hide
            # behind the other.
            groups = {}
            for entry in excluded:
                head = "Revit release" if "Revit" in entry.reason else \
                       entry.reason.split(" is ")[0]
                groups.setdefault(head, []).append(entry)
            lines = []
            for head in sorted(groups):
                got = groups[head]
                lines.append("%d excluded by %s" % (len(got), head))
                for entry in got[:5]:
                    lines.append("    %s - %s" % (entry.id, entry.reason))
                if len(got) > 5:
                    lines.append("    ... and %d more" % (len(got) - 5))
            ctx.add(Part(EXCLUDED, "what the walls removed", "\n".join(lines),
                         "heron_retrieve.eligible()",
                         "they exist and are not offerable here - absent, not "
                         "ranked lower. 'Heron found nothing' and 'Heron found "
                         "something it may not offer you' are different answers"))
        else:
            ctx.note_refused(EXCLUDED,
                             "nothing was excluded - every fragment in the "
                             "store is offerable" + ("" if revit is not None
                             else ", and no release was stated so no version "
                                  "wall was applied"))

    # 5. The paths whose sources are not all here yet.
    if path == STANDARDS:
        raise SourceMissing(
            "the STANDARDS path needs the clauses a check CITES, and a scope "
            "store holds `fragments` and `meta` only - there is no clause "
            "store in this installation. Refusing rather than returning a "
            "packet that looks complete and is not.")

    if path == GENERATION:
        _generation_parts(ctx, store, fragment_id, revit)

    return ctx


# The executor's import list lives in the add-in, because a list of vendor
# namespaces is Revit knowledge wherever it is stored and check-structure.py
# refuses it anywhere else. This reads it rather than restating it: two copies
# of that list is exactly the drift tests/test_fragment_imports.py exists to
# prevent between the executor and the compile gate, and a third copy here
# would be the same mistake a second time.
IMPORTS = os.path.join(ROOT, "revit", "Heron.Revit.Addin",
                       "HeronFragmentImports.cs")


def _api_surface():
    """(the namespaces a fragment may assume, where they were read from).

    THE FIRST VERSION OF THIS READ THE FRAGMENT'S OWN `using` LINES and
    returned "no using directives" for all 360, every time - a part that looked
    like an answer and carried nothing. It was the wrong source: a fragment
    body is NOT STANDALONE and declares no imports at all, by design. Found by
    running the generation path over 120 real requests and noticing the part
    was 19 characters wide in every single one.
    """
    if not os.path.exists(IMPORTS):
        return None, IMPORTS
    with open(IMPORTS, encoding="utf-8") as fh:
        body = fh.read()
    found = re.findall(r'"([A-Za-z_][A-Za-z0-9_.]*)"', body)
    if not found:
        return None, IMPORTS
    return "\n".join(found), FRAG.repo_relative(IMPORTS)


def _generation_parts(ctx, store, fragment_id, revit):
    """The closest fragment, its declared cases, and the API surface it uses.

    docs/19 s2 asks for exactly these three and no more. The neighbour is the
    fragment retrieval already matched - "closest existing fragment" is the
    same question retrieval just answered, and asking it twice by another
    method would give two answers to one question.
    """
    if not fragment_id:
        for kind in (NEIGHBOUR, TESTS, API):
            ctx.note_refused(kind, "nothing matched, so there is no neighbour "
                                   "to carry")
        return

    folder = _fragment_dir(store, fragment_id)
    if folder is None:
        for kind in (NEIGHBOUR, TESTS, API):
            ctx.note_refused(kind, "%s is in the store but not on disk in this "
                                   "working tree" % fragment_id)
        return

    yaml_path = os.path.join(folder, "fragment.yaml")
    if os.path.exists(yaml_path):
        with open(yaml_path, encoding="utf-8") as fh:
            ctx.add(Part(NEIGHBOUR, fragment_id, fh.read(),
                         FRAG.repo_relative(yaml_path),
                         "the closest existing fragment - what to write like"))
    else:
        ctx.note_refused(NEIGHBOUR, "%s has no fragment.yaml" % fragment_id)

    cases = os.path.join(folder, "tests", "cases.yaml")
    if os.path.exists(cases):
        with open(cases, encoding="utf-8") as fh:
            ctx.add(Part(TESTS, "%s cases" % fragment_id, fh.read(),
                         FRAG.repo_relative(cases),
                         "what the neighbour is checked against"))
    else:
        ctx.note_refused(TESTS, "%s declares no cases.yaml" % fragment_id)

    surface, where = _api_surface()
    if surface:
        ctx.add(Part(API, "what is already in scope", surface, where,
                     "a fragment body is NOT STANDALONE - the executor supplies "
                     "these, so generated code must NOT re-import them"))
    else:
        ctx.note_refused(API, "the executor's import list could not be read "
                              "from %s" % where)


# ---------------------------------------------------------------------------
# Reporting
# ---------------------------------------------------------------------------

def report(ctx, out=None, full=False):
    """The packet, in the order an agent would read it."""
    write = (out or sys.stdout).write

    write("CONTEXT PACKET\n")
    write("=" * 70 + "\n")
    write("path      %s%s\n" % (ctx.path,
                                "  (ASSUMED, not classified - D-01 puts that "
                                "in the host)" if ctx.assumed_path else ""))
    write("          %s\n" % WHY[ctx.path])
    write("budget    %s\n" % ", ".join(BUDGET[ctx.path]))
    write("carried   %s\n" % (", ".join(ctx.kinds()) or "nothing"))
    write("size      %d characters, %d part(s)\n" % (ctx.size, len(ctx.parts)))
    write("\n")

    for part in ctx.parts:
        write("-- %s: %s  (%d ch)\n" % (part.kind, part.name, part.size))
        write("   from   %s\n" % part.source)
        write("   why    %s\n" % part.why)
        if full:
            for line in str(part.body).splitlines():
                write("   | %s\n" % line)
        write("\n")

    if ctx.refused:
        write("NOT CARRIED, and why - the budget allowed these\n")
        write("-" * 70 + "\n")
        for kind, reason in ctx.refused:
            write("  %-12s %s\n" % (kind, reason))
        write("\n")

    write("Size is reported, never enforced. Heron has no tokeniser and the\n")
    write("host counts tokens (D-58). What IS enforced is the parts list: a\n")
    write("part outside this path's budget raises rather than slipping in.\n")


def main(argv):
    if not argv:
        print(__doc__.strip().splitlines()[0])
        print()
        print('  python brain/heron_context.py "select all ducts" --revit 2024')
        print("  --path cached|simple|standards|generation   --full")
        return 2

    revit = path = None
    full = "--full" in argv
    if "--revit" in argv:
        revit = argv[argv.index("--revit") + 1]
    if "--path" in argv:
        path = argv[argv.index("--path") + 1]

    words = []
    skip = False
    for i, token in enumerate(argv):
        if skip:
            skip = False
            continue
        if token in ("--revit", "--path"):
            skip = True
            continue
        if token == "--full":
            continue
        words.append(token)
    request = " ".join(words)

    store = SCOPE.open_scope(SCOPE.GLOBAL)
    try:
        SEARCH.index(store)
        try:
            ctx = assemble(store, request, path=path, revit=revit)
        except (SourceMissing, OverBudget) as why:
            print("REFUSED - %s" % why)
            return 1
        report(ctx, full=full)
        return 0
    finally:
        store.close()


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
