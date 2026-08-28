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
            "runtime")

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
    supply = dict((p.get("name"), p.get("type")) for p in producer.provides())
    for need in consumer.needs():
        name, want = need.get("name"), need.get("type")
        if AMBIENT.get(name) == want:
            continue                      # the wrapper supplies it, not a fragment
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
