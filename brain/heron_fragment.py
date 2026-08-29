# Heron-Agent:  HERON-FRG-VAL-001
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
What a fragment IS on disk, and the validator that will not let it lie.

    python brain/heron_fragment.py              validate every fragment
    python brain/heron_fragment.py --show <id>  print one

Step 7 of docs/27-build-order.md. Files and one validator - no database, no
search, no embeddings. Those are Steps 8 to 11 and they all index whatever
shape this file settles, so the shape is settled first and on its own.

THREE THINGS THIS FILE ENFORCES, each from a decision already taken
-------------------------------------------------------------------
1. IDENTITY IS NEVER THE FILENAME (docs/09 section 4). `id` is declared inside
   the file. Rename the folder, move it, copy it into another scope - it is the
   same knowledge object, and everything downstream keys on that id rather than
   on a path. A library that identifies knowledge by where it happens to sit
   cannot be reorganised without breaking every reference into it.

2. THE CONTRACT IS DATA, NOT PROSE (D-29). A fragment declares what it NEEDS in
   scope and what it PROVIDES, as a list a machine can read. Composition then
   becomes checkable before Revit is involved: a filter that provides nothing
   an action needs is a defect a tool finds in a second.

3. A PROOF CARRIES A NEGATIVE CASE (D-30). Promotion to PROVEN or PRODUCTION is
   refused unless the proof says what came back EMPTY when it should have. The
   defect that gate exists for is the fragment that SUCCEEDS WHILE DOING
   NOTHING - it passes ten runs, it passes a thousand, and a count of successful
   executions cannot see it. Only a case that should return nothing can.

WHAT A PASS DOES AND DOES NOT MEAN
----------------------------------
A valid fragment is a well-FORMED fragment. It says nothing about whether the
implementation works - `status` says that, and only a recorded proof against a
real model may set it past VALIDATED. This file is the shape; NEEDS-CHECKING.md
is the truth.
"""

import io
import os
import re
import sys
import hashlib

try:
    import yaml
except ImportError:                                          # pragma: no cover
    sys.stderr.write(
        "Heron's brain needs PyYAML to read fragment.yaml.\n"
        "  pip install --user pyyaml\n"
        "It installs per-user and needs no administrator rights, which is the\n"
        "install promise D-01 makes. The BRIDGE CLIENT stays dependency-free -\n"
        "that rule is about mcp/client, which has to run on a locked-down\n"
        "machine with nothing on it. This layer is not that layer.\n")
    raise

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENTS_DIR = os.path.join(ROOT, "brain", "fragments")

# D-29. A fragment is a COMPOSABLE PIECE, not a whole answer: a filter says
# which elements, an action says what to do to them, and they are joined. A
# recipe is the named third kind for a job that genuinely cannot be composed -
# named so it cannot quietly become a giant fragment. A RISING RECIPE COUNT
# MEANS COMPOSITION IS FAILING and is worth watching as a signal.
KINDS = ("filter", "action", "recipe")

# docs/09 section 5. In order: a fragment only ever moves along this list.
STATUSES = ("DISCOVERED", "DRAFT", "TESTING", "VALIDATED",
            "PROVEN", "PRODUCTION", "DEPRECATED", "ARCHIVED")

# The statuses that mean "this has been shown to work against a real model".
# Everything at or past PROVEN needs the D-30 proof, and nothing below it does.
NEEDS_PROOF = ("PROVEN", "PRODUCTION")

# Mirrors Directory.Build.props. D-05: an unlisted release is an error, never a
# guess, and the table is never extrapolated forward.
REVIT_VERSIONS = ("2020", "2021", "2022", "2023", "2024", "2025", "2026", "2027")

# The field names come from docs/29 section 2, which specified this file's shape
# before Step 7 built it. Step 7 changed two things there and both are recorded
# in that document rather than diverged from in silence:
#
#   inputs/outputs -> contract.needs/provides   D-29 came after docs/29 and says
#                                               the contract is DATA, with named
#                                               and typed entries a tool can
#                                               check composition against.
#   revit: ">=2020" -> an explicit list          A range CLAIMS EVERY FUTURE
#                                               RELEASE, which is precisely what
#                                               D-05 forbids. A list cannot
#                                               silently assert 2028.
REQUIRED = ("heron-agent", "heron-step", "heron-status", "heron-since",
            "heron-layer",
            "id", "semantic-identity", "kind", "domain", "capability",
            "version", "source", "risk", "purpose", "contract", "revit",
            "runtime", "utterances")

# Names the generated wrapper ALWAYS has in scope, so no fragment has to be
# preceded by another one to get them. Found by writing the first two fragments
# rather than by designing: a filter provides `elements`, the action that
# consumes it also needs `uidoc`, and nothing upstream provides a `uidoc` -
# which made the pair read as non-composable when it composes perfectly.
#
# Fragments still DECLARE these in `needs`, deliberately. The declaration is
# what lets a tool ask the opposite question - "does the wrapper this runs in
# actually have a uidoc?" - which is a real failure for an operation invoked
# with no UI document open. Silence would not survive that question.
AMBIENT = {
    "doc":   "Document",
    "uidoc": "UIDocument",
    "app":   "Application",
}

# WHERE A NEEDED NAME CAN COME FROM. Found the same way AMBIENT was - by writing
# real fragments and watching the model fail to describe them.
#
#   fragment   another fragment provides it. The default, and the only one
#              composition has anything to say about
#   ambient    the wrapper always has it - doc, uidoc, app
#   request    THE REQUEST supplies it. `category` for a filter,
#              `parameterName` for a parameter read, `whatWasChecked` for a
#              report. No fragment will ever provide these and none should:
#              they are what the user's sentence carries
#
# Without the third, the orphan check reported two perfectly good fragments as
# "an action nothing can feed", because it was looking for a producer of a value
# that only a human can supply. A contract model that cannot say "this comes
# from the question" makes every parameterised fragment look broken.
SOURCES = ("fragment", "ambient", "request")


def need_source(entry):
    """Where this need comes from. Ambient names are recognised by name, so a
    fragment does not have to remember to label doc and uidoc."""
    declared = entry.get("source")
    if declared in SOURCES:
        return declared
    if AMBIENT.get(entry.get("name")) == entry.get("type"):
        return "ambient"
    return "fragment"

# ---------------------------------------------------------------------------
# Naming
# ---------------------------------------------------------------------------
#
# docs/10 section 6 asks for two things that pull against each other, and the
# split below is how both are had:
#
#   "Naming must be predictable and searchable"   -> the CAPABILITY and the
#                                                    folder, which are the same
#                                                    words in two casings
#   "identity is an ID, never a name.             -> the ID, which is stable and
#    Rename freely; identity survives"               says nothing about the name
#
# So an id NEVER contains the fragment's name, its kind or its version. Encoding
# `kind` in an id was the first mistake here: a fragment that later stops being
# a filter would carry an id that lies about it forever, and ids are the one
# thing that cannot be corrected without breaking every reference.
#
# The pattern follows the agent registry's house style (HERON-RAG-FMT-004) - a
# stable area, a number, readable at a glance - with its own namespace, because
# a fragment is knowledge and an agent is code.
ID_PATTERN = re.compile(r"^FRG-[A-Z]{2,5}-[0-9]{3}$")

# Fixed on purpose. An unlisted area is an error rather than a guess - the same
# rule D-05 applies to Revit releases, for the same reason: the moment this is
# open, one fragment says MEP and the next says MECH and neither search finds
# both. Adding an area is a deliberate edit here.
AREAS = {
    "ELE":  "elements in general",
    "SEL":  "selection",
    "VIEW": "views",
    "SHT":  "sheets",
    "PAR":  "parameters",
    "MEP":  "ducts, pipes, systems",
    "GEO":  "geometry and location",
    "QA":   "checks and audits",
    "DOC":  "the document and the session",
}

CAPABILITY_PATTERN = re.compile(r"^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*$")

# A contract name is composed into generated C# (D-28), so it has to survive
# being a variable there. Deliberately stricter than C# itself, which would
# accept `@checked` and Unicode identifiers: a name that needs escaping to be
# legal is a name that will be got wrong by whoever composes it next.
IDENTIFIER_PATTERN = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")

# C#'s reserved words. A contract may not use one as a name - see the check in
# _check_contract_side for the fragment that made this necessary.
CSHARP_KEYWORDS = {
    "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
    "checked", "class", "const", "continue", "decimal", "default", "delegate",
    "do", "double", "else", "enum", "event", "explicit", "extern", "false",
    "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
    "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
    "new", "null", "object", "operator", "out", "override", "params",
    "private", "protected", "public", "readonly", "ref", "return", "sbyte",
    "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
    "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong",
    "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile",
    "while",
}


def folder_for(capability):
    """The folder name a capability must live in. Mechanically derivable, so
    the library is predictable to search and impossible to misfile quietly."""
    return capability.lower().replace("_", "-")


def naming_problems(frag):
    """Whether this fragment is named the way the standard says.

    Kept separate from identity on purpose. A fragment in a renamed folder is
    STILL THE SAME FRAGMENT - that is what an id is for - and it is ALSO
    misfiled. Heron reports both and confuses neither.
    """
    problems = []
    where = frag.slug

    if frag.id and not ID_PATTERN.match(frag.id):
        problems.append(
            "%s: id %r must look like FRG-<AREA>-<NNN>, e.g. FRG-ELE-001. An id "
            "never carries the name, the kind or the version - those all change "
            "and an id may not" % (where, frag.id))
    elif frag.id:
        area = frag.id.split("-")[1]
        if area not in AREAS:
            problems.append(
                "%s: id area %r is not one Heron knows. Known: %s. Adding one is "
                "a deliberate edit to AREAS, never a guess"
                % (where, area, ", ".join(sorted(AREAS))))

    cap = frag.data.get("capability")
    if cap and not CAPABILITY_PATTERN.match(cap):
        problems.append(
            "%s: capability %r must be SCREAMING_SNAKE_CASE, verb first - "
            "FILTER_ELEMENTS_BY_CATEGORY, not ElementsFilterByCategory" % (where, cap))
    elif cap and frag.slug != folder_for(cap):
        problems.append(
            "%s: capability %s belongs in a folder called %r, not %r. The folder "
            "IS the capability in lower case - that is what makes the library "
            "predictable to search. (The fragment's identity is unaffected: it is "
            "still %s.)" % (where, cap, folder_for(cap), frag.slug, frag.id))

    return problems


class Fragment(object):
    """One fragment, as it is on disk. `folder` is where it was FOUND, never
    what it IS - see `id`."""

    def __init__(self, data, folder):
        self.data = data
        self.folder = folder

    # -- identity -----------------------------------------------------------

    @property
    def id(self):
        return self.data.get("id")

    @property
    def slug(self):
        """The folder's name. Convenience for messages ONLY - never a key, and
        never compared against `id`. Two fragments may sit in identically named
        folders in different scopes and still be different knowledge."""
        return os.path.basename(self.folder)

    @property
    def semantic_identity(self):
        """The canonical phrasing of what this does. Step 9's exact-match short
        circuit keys on this, so it is what turns the 400th 'select all ducts'
        into one lookup instead of a retrieval pipeline."""
        return self.data.get("semantic-identity")

    @property
    def kind(self):
        return self.data.get("kind")

    @property
    def status(self):
        """The lifecycle state. It is `heron-status` and not a second `status`
        key, because docs/29 already owns that name for every file in the
        repository and one fact wants one home."""
        return self.data.get("heron-status")

    @property
    def supported(self):
        return [str(v) for v in self.data.get("revit", []) or []]

    # -- the contract, as data ----------------------------------------------

    def utterances(self):
        """What somebody might actually type to want this. Declared by the
        author, indexed by Step 9, and the input the exact-match short circuit
        really runs on."""
        return list(self.data.get("utterances", []) or [])

    def needs(self):
        return list(self.data.get("contract", {}).get("needs", []))

    def provides(self):
        return list(self.data.get("contract", {}).get("provides", []))

    # -- the proof ----------------------------------------------------------

    @property
    def proof(self):
        return self.data.get("proof")

    def proof_files(self):
        """Every file the proof rested on, repo-relative. The implementation is
        what a proof is a claim ABOUT, so the implementation is what staleness
        is measured against."""
        impl = os.path.join(self.folder, "impl")
        found = []
        for base, _dirs, files in os.walk(impl):
            for name in sorted(files):
                full = os.path.join(base, name)
                found.append(os.path.relpath(full, ROOT))
        return sorted(found)

    def fingerprint(self):
        """One hash over the exact bytes of the implementation.

        Content, not timestamps - the same rule tests/test_golden.py follows,
        and for the same reason: a file touched but unchanged must not
        invalidate a proof, and a file changed back to what it was must not
        either. Returns None when there is nothing to hash, which is itself a
        finding rather than a pass.
        """
        paths = self.proof_files()
        if not paths:
            return None
        digest = hashlib.sha256()
        for path in paths:
            digest.update(path.encode("utf-8"))
            digest.update(io.open(os.path.join(ROOT, path), "rb").read())
        return digest.hexdigest()[:16]

    def proof_is_stale(self):
        """True when the code moved under the proof.

        D-30: 'A proof can go stale, and that must be visible.' Unknown counts
        as stale - a proof that never recorded what it was taken against cannot
        be shown to still hold, and treating unknown as fresh is exactly the
        silent-pass this repository keeps catching itself in.
        """
        if not self.proof:
            return False
        recorded = self.proof.get("fingerprint")
        if not recorded:
            return True
        return recorded != self.fingerprint()

    def __repr__(self):
        return "<Fragment %s (%s) %s>" % (self.id, self.kind, self.status)


# ---------------------------------------------------------------------------
# Loading
# ---------------------------------------------------------------------------

def load(folder):
    """Read one fragment folder. Raises ValueError with a readable message."""
    path = os.path.join(folder, "fragment.yaml")
    if not os.path.exists(path):
        raise ValueError("%s has no fragment.yaml" % folder)
    data = yaml.safe_load(io.open(path, encoding="utf-8").read())
    if not isinstance(data, dict):
        raise ValueError("%s: fragment.yaml is not a mapping" % folder)
    return Fragment(data, folder)


def load_all(root=None):
    """Every fragment under `root`, keyed BY ID rather than by path.

    Keying by id here is the whole of point 1 in this file's header: if two
    fragments ever collide on an id the library has a real problem, and it
    surfaces at load rather than as a silently preferred winner much later.
    """
    root = root or FRAGMENTS_DIR
    found, problems = {}, []
    if not os.path.isdir(root):
        return found, problems

    for name in sorted(os.listdir(root)):
        folder = os.path.join(root, name)
        if not os.path.isdir(folder):
            continue
        try:
            frag = load(folder)
        except ValueError as exc:
            problems.append(str(exc))
            continue
        if not frag.id:
            problems.append("%s: no id, so it has no identity to be known by" % name)
            continue
        if frag.id in found:
            problems.append(
                "%s: id %s is already used by %s. Two fragments cannot share an "
                "identity - everything downstream keys on it"
                % (name, frag.id, found[frag.id].slug))
            continue
        found[frag.id] = frag
    return found, problems


# ---------------------------------------------------------------------------
# Validation
# ---------------------------------------------------------------------------

def _check_contract_side(side, entries, problems, where):
    """A contract side is a list of {name, type}. Prose is refused.

    D-29 says the contract is data, not prose, and this is where that is made
    true rather than merely stated. A free-text 'needs a document and a
    category' reads fine to a person and cannot be checked by anything.
    """
    if not isinstance(entries, list):
        problems.append("%s: contract.%s must be a list of {name, type}, not %s"
                        % (where, side, type(entries).__name__))
        return
    for i, entry in enumerate(entries):
        if not isinstance(entry, dict):
            problems.append("%s: contract.%s[%d] is %s - it must be {name, type}. "
                            "A sentence cannot be composed against"
                            % (where, side, i, type(entry).__name__))
            continue
        for key in ("name", "type"):
            if not entry.get(key):
                problems.append("%s: contract.%s[%d] has no %s"
                                % (where, side, i, key))
        if entry.get("source") and entry["source"] not in SOURCES:
            problems.append(
                "%s: contract.%s[%d] source %r is not one of %s"
                % (where, side, i, entry["source"], ", ".join(SOURCES)))

        # A CONTRACT NAME BECOMES A C# VARIABLE, so it has to be able to be one.
        #
        # Found on 2026-08-29 by compiling the fragments for the first time:
        # FRG-QA-001 declared a value called `checked`, which is a reserved C#
        # keyword. The fragment's own code read `if (checked == 0)` and could
        # never have compiled - and it had passed every check here, because
        # nothing had ever asked whether a name was a legal identifier.
        #
        # Checked here as well as by the compiler on purpose: this costs
        # nothing and answers instantly, while the compile check needs the
        # .NET SDK and several minutes. The compiler stays the authority; this
        # is the fast half that stops the mistake being made.
        name = entry.get("name")
        if isinstance(name, str) and name:
            if not IDENTIFIER_PATTERN.match(name):
                problems.append(
                    "%s: contract.%s[%d] is named %r, which cannot be a C# "
                    "variable. A contract name is composed into generated code "
                    "(D-28), so it must be a plain identifier"
                    % (where, side, i, name))
            elif name in CSHARP_KEYWORDS:
                problems.append(
                    "%s: contract.%s[%d] is named %r, which is a RESERVED C# "
                    "keyword. The generated code will not compile - rename it "
                    "(e.g. %sCount, or what it actually counts)"
                    % (where, side, i, name, name))


def validate(frag):
    """Every way this fragment is malformed. Empty list means well-formed -
    which is NOT the same as working."""
    problems = []
    where = frag.slug

    for field in REQUIRED:
        if frag.data.get(field) in (None, "", [], {}):
            problems.append("%s: missing %s" % (where, field))

    if frag.kind and frag.kind not in KINDS:
        problems.append("%s: kind %r is not one of %s"
                        % (where, frag.kind, ", ".join(KINDS)))

    if frag.status and frag.status not in STATUSES:
        problems.append("%s: status %r is not a lifecycle state (%s)"
                        % (where, frag.status, ", ".join(STATUSES)))

    contract = frag.data.get("contract")
    if isinstance(contract, dict):
        _check_contract_side("needs", contract.get("needs", []), problems, where)
        _check_contract_side("provides", contract.get("provides", []), problems, where)
        # A filter or action that provides nothing cannot be composed with
        # anything, which is the one thing D-29 says a fragment is FOR.
        if frag.kind in ("filter", "action") and not contract.get("provides"):
            problems.append(
                "%s: a %s provides nothing, so nothing can be composed onto it. "
                "If it genuinely is a whole job, it is a recipe (D-29)"
                % (where, frag.kind))
    elif contract is not None:
        problems.append("%s: contract must be a mapping with needs and provides" % where)

    unknown = [v for v in frag.supported if v not in REVIT_VERSIONS]
    if unknown:
        problems.append(
            "%s: revit names releases Heron does not know: %s. "
            "Supported: %s. D-05 - the table is never guessed"
            % (where, ", ".join(unknown), ", ".join(REVIT_VERSIONS)))
    if not frag.supported:
        problems.append(
            "%s: revit is empty - every fragment must name the releases it is "
            "for, as a LIST. A range like '>=2020' claims every future release, "
            "which is what D-05 exists to prevent" % where)

    # docs/09 section 2 asked for these and Step 7 did not build them; Step 9
    # found out why they matter. A fragment nobody can PHRASE A REQUEST FOR is
    # unfindable, and being findable is the entire point of Steps 8 to 11.
    # They are also the short circuit's real input: users type "select all
    # ducts", never "all elements of one category in the active document".
    said = frag.data.get("utterances")
    if said is not None:
        if not isinstance(said, list) or not said:
            problems.append(
                "%s: utterances must be a non-empty list of things somebody "
                "might actually say" % where)
        else:
            for i, line in enumerate(said):
                if not isinstance(line, str) or len(line.strip()) < 3:
                    problems.append("%s: utterances[%d] is not a phrase" % (where, i))

    problems.extend(naming_problems(frag))
    problems.extend(proof_problems(frag))
    return problems


# ---------------------------------------------------------------------------
# The D-30 gate
# ---------------------------------------------------------------------------
#
# Q-9 originally proposed a COUNT - "how many successful executions, suggest
# N = 10?" D-30 replaced it, on evidence from a real library: one fragment's
# level chain never tried the right parameter, so a level filter matched ZERO
# elements AND REPORTED SUCCESS. A fragment that succeeds while doing nothing
# passes ten runs. It passes a thousand. A count measures that nothing threw,
# which is not the property anybody cares about.
#
# What caught it was a comparison - 3 against 0, side by side. So the gate is a
# recorded proof containing a case that should come back EMPTY.

PROOF_REQUIRED = ("date", "by", "model", "positive_case", "negative_case",
                  "second_route")


def proof_problems(frag):
    """Whether this fragment's proof, if it has one, is a proof at all."""
    problems = []
    where = frag.slug
    proof = frag.proof

    # A PROOF TAKEN SOMEWHERE ELSE IS NOT A PROOF OF THIS CODE.
    #
    # Ajmal's instruction, 2026-08-29, on re-authoring from his earlier
    # library: *"even in the AJ AI proven fragment don't mark in Heron this is
    # proven, because we will check each and every one again in Heron AI."*
    #
    # This is where that is made true rather than merely agreed. The fingerprint
    # in a proof is a hash of THIS fragment's own implementation bytes, so a
    # proof carried over from another library cannot match one - it reads as
    # stale, which is exactly what it is.
    #
    # `can_promote` already refused a stale proof. `validate` did NOT, and
    # validate is what `python brain/heron_fragment.py` and check-gaps run. So
    # a fragment could sit at PROVEN on somebody else's proof and pass every
    # check in the repository - measured on 2026-08-29 by writing one and
    # watching it come back clean. The gate existed and nothing stood on it.
    if frag.status in NEEDS_PROOF and proof is not None and frag.proof_is_stale():
        problems.append(
            "%s: status is %s but the proof does not match this "
            "implementation - no fingerprint, or one taken against different "
            "bytes. A proof recorded elsewhere, or before the code changed, is "
            "evidence about THAT code. Re-take it here (D-30), or set the "
            "status back to DRAFT" % (where, frag.status))

    if proof is None:
        if frag.status in NEEDS_PROOF:
            problems.append(
                "%s: status is %s but there is no proof. D-30 - a fragment "
                "reaches PROVEN on one recorded proof against a real model, "
                "and 'it worked' is not one" % (where, frag.status))
        return problems

    if not isinstance(proof, dict):
        problems.append("%s: proof must be a mapping" % where)
        return problems

    for field in PROOF_REQUIRED:
        if not proof.get(field):
            problems.append(
                "%s: proof has no %s. All six of %s are required - a proof "
                "nobody signed, on no stated model, is a claim"
                % (where, field, ", ".join(PROOF_REQUIRED)))

    # The one that catches "succeeded and did nothing". `second_route` may be
    # declared absent with a reason, because D-30 says "where one exists" - but
    # it has to be a DECLARED absence, because a missing key and a considered
    # "there isn't one" read identically on disk and mean opposite things.
    if proof.get("negative_case") and len(str(proof["negative_case"]).strip()) < 10:
        problems.append(
            "%s: negative_case is too short to be one. It must say what came "
            "back EMPTY and why that is the right answer" % where)

    return problems


def can_promote(frag, target):
    """(allowed, reason). Whether this fragment may move to `target` now."""
    if target not in STATUSES:
        return False, "%r is not a lifecycle state" % target

    if STATUSES.index(target) <= STATUSES.index(frag.status or "DISCOVERED"):
        # Sideways and backwards are not promotions. DEPRECATED and ARCHIVED
        # are reached deliberately, not by this function.
        return False, "%s is not ahead of %s" % (target, frag.status)

    broken = validate(frag)
    if broken:
        return False, "it is not well-formed yet: %s" % broken[0]

    if target in NEEDS_PROOF:
        missing = proof_problems(frag)
        if missing:
            return False, missing[0]
        if frag.proof_is_stale():
            return False, (
                "the proof is STALE - the implementation changed under it. "
                "Re-take it against the current code; D-30 says a proof going "
                "stale must be VISIBLE, which is what this is")

    if frag.proof:
        return True, "proof recorded by %s on %s" % (frag.proof.get("by"),
                                                     frag.proof.get("date"))
    return True, "well-formed, and %s needs no proof" % target


# ---------------------------------------------------------------------------
# Composition - the reason the contract is data
# ---------------------------------------------------------------------------

def composable(producer, consumer):
    """(ok, reason). Can `consumer` run after `producer`?

    Every name the consumer NEEDS must be PROVIDED by the producer, with a
    matching type. This is the check D-29 exists to make possible, and it costs
    nothing: a filter that provides no `elements` and an action that needs one
    is a defect found here rather than in front of a user, mid-job, with a
    transaction open.
    """
    # A consumer that needs nothing FROM A FRAGMENT does not compose after
    # anything - it stands alone. Without this, composable() returns True
    # against every producer, because there is nothing left to fail on, and
    # "anything may precede a thing that needs nothing" is true in the way that
    # is no use to anybody. It made the graph report all seven fragments as
    # feeding the category filter, and made an action look composable before a
    # filter that consumes nothing it makes.
    wanted = [n for n in consumer.needs() if need_source(n) == "fragment"]
    if not wanted:
        return False, ("%s consumes nothing another fragment provides - its "
                       "inputs come from the wrapper or the request, so it "
                       "starts a chain rather than continuing one"
                       % consumer.slug)

    supply = dict((p.get("name"), p.get("type")) for p in producer.provides())
    for need in wanted:
        name, want = need.get("name"), need.get("type")
        if name not in supply:
            return False, ("%s needs %r and %s does not provide it"
                           % (consumer.slug, name, producer.slug))
        if supply[name] != want:
            return False, ("%s needs %r as %s but %s provides %s"
                           % (consumer.slug, name, want, producer.slug, supply[name]))
    return True, "%s -> %s composes" % (producer.slug, consumer.slug)


# ---------------------------------------------------------------------------
# Command line
# ---------------------------------------------------------------------------

def main(argv):
    if argv[:1] == ["--show"]:
        if len(argv) < 2:
            print("--show needs a fragment id")
            return 2
        found, _ = load_all()
        frag = found.get(argv[1])
        if frag is None:
            print("No fragment with id %s. Known: %s"
                  % (argv[1], ", ".join(sorted(found)) or "none"))
            return 1
        print(yaml.safe_dump(frag.data, default_flow_style=False, sort_keys=False))
        print("found in:   %s" % os.path.relpath(frag.folder, ROOT))
        print("fingerprint %s" % (frag.fingerprint() or "nothing to hash"))
        if frag.proof:
            print("proof       %s" % ("STALE" if frag.proof_is_stale() else "stands"))
        return 0

    found, problems = load_all()
    print("Fragments in %s" % os.path.relpath(FRAGMENTS_DIR, ROOT))
    print()

    if not found and not problems:
        print("None yet. Step 7 of docs/27-build-order.md is the shape they take.")
        return 0

    by_status = {}
    for frag in sorted(found.values(), key=lambda f: f.id):
        broken = validate(frag)
        problems.extend(broken)
        mark = "FAIL" if broken else ("STALE" if frag.proof_is_stale() else "ok")
        print("  %-5s %-28s %-8s %s" % (mark, frag.slug, frag.kind, frag.id))
        by_status[frag.status] = by_status.get(frag.status, 0) + 1

    print()
    for status in STATUSES:
        if by_status.get(status):
            print("  %-12s %d" % (status, by_status[status]))

    if problems:
        print()
        for line in problems:
            print("  %s" % line)
        print()
        print("%d problem(s). A fragment that is not well-formed cannot be" % len(problems))
        print("indexed, composed or promoted - Steps 8 to 12 all read this shape.")
        return 1

    print()
    print("All %d well-formed. That is the SHAPE agreeing - it is not evidence" % len(found))
    print("that any implementation works. Only a recorded proof against a real")
    print("model moves a fragment past VALIDATED, and D3 in NEEDS-CHECKING.md is")
    print("still the line that catches a unit error.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
