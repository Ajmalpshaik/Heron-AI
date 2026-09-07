#!/usr/bin/env python3
# Heron-Agent:  HERON-FRG-VAL-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Fragment Validation Agent. It gathers evidence for a proof. It never signs one.

    python brain/heron_validate.py plan                 what can be proved, and how
    python brain/heron_validate.py plan --all           including what cannot, and why
    python brain/heron_validate.py draft <slug> --from <run-record.json>
    python brain/heron_validate.py review [<slug>]      read the drafts
    python brain/heron_validate.py accept <slug> --by "Your Name"
    python brain/heron_validate.py restamp              fix platform-skewed fingerprints
    python brain/heron_validate.py audit --file <a.jsonl>

THE ONE RULE
------------
**This file never writes `heron-status`.** It gathers evidence and drafts a proof
block; a person confirms it. D-30 exists because an unproven claim quietly ages
into a believed one, and an agent able to stamp 333 fragments is the fastest
machine ever built for doing exactly that.

Clash detection finds the clashes and lists them. The engineer decides which are
real, and the engineer signs the drawing. **The machine never signs.**

That rule is enforced three ways rather than promised once:

1. Drafts are written to `brain/proof-drafts/`, never into `brain/fragments/`.
   `write_draft` refuses a path under the fragment library, so the agent has no
   route into the library at all.
2. `accept` writes only the `proof:` key. It reads the file back afterwards and
   restores it if `heron-status` moved by so much as a character.
3. A draft's `by:` field is left EMPTY. `proof_problems` in heron_fragment.py
   requires it, so a draft copied in without a person putting their name on it
   cannot pass validation, cannot be promoted, and fails loudly rather than
   quietly counting as proven.

WHY THIS LAYER, AND NOT THE BRIDGE
----------------------------------
`brain` may depend only on `platform` (tools/check-structure.py). It never
imports the bridge client and never resolves %APPDATA% - the brain has to stay
runnable, and testable, on a machine with no Revit on it, which is where this
file was written.

So the work splits at the honest seam. **Running fragments against Revit is the
client's job**; `heron_bridge_client.py validate` does it and writes a run
record. **Judging what came back is this file's job**, and it can be done, and
tested, anywhere. The record is the join: plain JSON, no Revit needed to read it.

WHAT IT REFUSES TO DO, AND WHY EACH ONE IS HERE
-----------------------------------------------
- **Never writes PROVEN.** Above.
- **Never invents a negative case it did not run.** A proof saying "with nothing
  selected it returned 0" when nothing checked is worse than no proof at all,
  because it looks like evidence. Every line of a draft comes from a recorded
  result, and a phase the run record does not contain is written as NOT
  ESTABLISHED rather than filled in.
- **Never calls two fragments a second route because their names look alike.**
  Retrieval similarity is not evidence: FIND_DEAD_ENDS and CHECK_FLOW_DIRECTION
  rank within 0.003 of each other and reach nothing like the same fact. The rule
  rule was tried, measured and dropped - see section 2, which records why. It
  offers the bridge's own operations instead, and otherwise says so plainly.
- **Never marks a fragment it could not run.** 279 of the 349 fragments need a
  value only a person's sentence carries (a category, a name, a distance), and
  there is no route for those. They are listed, per fragment, with the value
  they are waiting for.
- **Never touches the write path.** `run_fragment_read` opens no transaction. A
  fragment at anything other than `risk: READ` is out of scope until Phase 1 is
  proven, and `plan` will not schedule one.
"""

import argparse
import io
import re
import json
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as HF                                       # noqa: E402

try:
    import yaml
except ImportError:                                          # pragma: no cover
    sys.stderr.write("This agent reads fragment.yaml, so it needs PyYAML:\n"
                     "  pip install --user pyyaml\n")
    raise

ROOT = HF.ROOT
DRAFTS_DIR = os.path.join(ROOT, "brain", "proof-drafts")


# ---------------------------------------------------------------------------
# 1. Can this fragment be reached at all, and how
# ---------------------------------------------------------------------------
#
# Measured off disk on 2026-09-06 rather than read off the roadmap, because the
# roadmap's number and the reachable number are nothing like each other:
#
#     349 fragments      333 DRAFT, 16 PROVEN
#     279               need a value only a person's sentence carries
#     180               can change the model, so out of scope for a read-only pass
#      20               run on the document alone - and 16 of those are the
#                       16 already proven
#      56 READ           reachable with no typed value, once one chaining wave
#                       is allowed: a producer feeds the consumer's `elements`
#      40               of those, not yet proven. THIS IS THE WORKING SET.
#
# Forty is the honest size of this job. It is not 333, and saying 333 would make
# the agent look like an answer to a problem it can only dent. It is also not 4,
# which is what it would be if chaining were ignored.

STANDALONE = "STANDALONE"        # needs only doc / uidoc / app
FROM_SELECTION = "FROM_SELECTION"  # a person selects, and it runs
FROM_CHAIN = "FROM_CHAIN"        # a producer fragment must run first, in the same batch
NEEDS_VALUES = "NEEDS_VALUES"    # needs something only the user's sentence carries
UNREACHABLE = "UNREACHABLE"      # needs a value nothing in the library provides
WRITE_PATH = "WRITE_PATH"        # not risk: READ - out of scope entirely
PROVED = "PROVED"                # already carries a proof that still stands
RE_PROVE = "RE_PROVE"            # PROVEN, but the code moved under the proof

# The order they are worth working on. RE_PROVE first: a fragment claiming PROVEN
# on a proof that no longer matches its code is the one actively misleading
# somebody, which is worse than a fragment honestly marked DRAFT. Then the two
# that a person at the keyboard can run on their own, then the ones needing a
# batch assembled first.
PRIORITY = [RE_PROVE, STANDALONE, FROM_SELECTION, FROM_CHAIN, NEEDS_VALUES,
            UNREACHABLE, WRITE_PATH, PROVED]


def is_element_list(type_name):
    """Whether the executor would accept the selection for a need of this type.

    Copied deliberately from RevitFragment.IsElementList rather than
    approximated: an IList<ElementId> looks like a list of elements and is NOT
    one, and a plan that says "just select some" for a need the host will refuse
    sends somebody to the PC to find that out.
    """
    if not type_name:
        return False
    if "ElementId" in type_name:
        return False
    return "<Element>" in type_name


def providers(library):
    """(name, type) -> the fragments that provide it.

    Built once and passed around. Everything about reachability is a question
    about this table.
    """
    table = {}
    for frag in library:
        for entry in frag.provides():
            key = (entry.get("name"), entry.get("type"))
            table.setdefault(key, []).append(frag)
    return table


def route_for(frag, table):
    """(route, note). How this fragment could be run today, in one word and one line."""
    if frag.data.get("risk") != "READ":
        return WRITE_PATH, ("risk: %s - run_fragment_read opens no transaction, "
                            "so this is out of scope until Phase 1 is proven"
                            % frag.data.get("risk"))

    if frag.status in HF.NEEDS_PROOF:
        if frag.proof_is_stale():
            # TWO COMPLETELY DIFFERENT SITUATIONS WEAR THE SAME WORD. One is a
            # proof taken against code that has since changed, which is the
            # thing D-30 built this mechanism to catch. The other is a
            # fingerprint recorded under the old platform-dependent rule, which
            # is an artefact and needs one command, not a re-proof. Reporting
            # them alike is how sixteen artefacts would bury the first real one.
            if implementation_changed_after(frag):
                return RE_PROVE, ("claims %s, and the implementation changed "
                                  "AFTER the proof was taken - re-prove it"
                                  % frag.status)
            return RE_PROVE, ("claims %s; the code has not moved since the "
                              "proof, so this is the old platform-dependent "
                              "fingerprint - `restamp`, do not re-prove"
                              % frag.status)
        return PROVED, "proof recorded %s, and it still stands" % frag.proof.get("date")

    wanted = []
    for need in frag.needs():
        source = HF.need_source(need)
        if source == "request":
            wanted.append(need)
    if wanted:
        names = ", ".join(sorted(n.get("name") for n in wanted))
        return NEEDS_VALUES, ("waiting on a value only the request carries: %s"
                              % names)

    from_fragment = [n for n in frag.needs() if HF.need_source(n) == "fragment"]
    if not from_fragment:
        return STANDALONE, "runs on the open document alone"

    missing, sources = [], []
    for need in from_fragment:
        key = (HF.need_binds(need), need.get("type"))
        found = table.get(key)
        if not found:
            missing.append("%s (%s)" % (need.get("name"), need.get("type")))
        else:
            sources.append((need.get("name"), [f.slug for f in found]))
    if missing:
        return UNREACHABLE, ("nothing in the library provides %s"
                             % ", ".join(missing))

    # THE HOST'S OWN RULE, NOT AN APPROXIMATION OF IT. RevitFragment.BindNeeds
    # fills a need from the selection only when its type is a list of Elements
    # AND exactly one need could be answered that way - because "first" and
    # "second" are both unbound element lists and one selection cannot say which
    # is which. Anything else has to come from a producer earlier in the batch.
    selectable = [n for n in from_fragment if is_element_list(n.get("type"))]
    if len(selectable) == 1 and len(from_fragment) == 1:
        return FROM_SELECTION, ("select some %s in Revit and it runs - or feed "
                                "it from %s"
                                % (selectable[0].get("name"),
                                   ", ".join(sorted(sources[0][1])[:3])))

    return FROM_CHAIN, ("needs %s in one batch - a selection cannot supply "
                        "them, so run a producer first: %s"
                        % (", ".join(n for n, _ in sources),
                           ", ".join(sorted(sources[0][1])[:3])))


# ---------------------------------------------------------------------------
# 2. A second route - and the rule this agent REFUSES to invent
# ---------------------------------------------------------------------------
#
# D-30's third leg is "a second route to the answer where one exists". Finding
# one automatically was tried here and the attempt is recorded rather than
# quietly dropped, because the measurement is the useful part.
#
# THE TEMPTING RULE IS SIMILARITY, AND IT IS WRONG. FIND_DEAD_ENDS and
# CHECK_FLOW_DIRECTION rank within 0.003 of each other and reach nothing like
# the same fact. So similarity was never a candidate.
#
# THE STRUCTURAL RULE WAS TRIED, MEASURED, AND DROPPED. The idea: LIST_LEVELS
# and REPORT_LEVEL_ELEVATIONS genuinely do reach the same fact by different
# code, and they share a provided type - IDictionary<ElementId, double> - that
# the MEP pair does not share. If that type were rare, sharing it would mean
# something. Measured across the library on 2026-09-06: it is provided by
# **42 fragments**. It discriminates nothing.
#
# AND `domain` IS WORSE THAN NOTHING - IT IS INVERTED. The pair that really is
# a cross-check sits in two different domains (revit.levels and revit.document);
# the pair that is not sits in one (revit.mep, both). A domain rule would have
# proposed the wrong pair and missed the right one, with the confidence of
# something computed.
#
# So this agent does not propose a fragment as a second route at all. Nothing in
# the declared data says what fact a fragment establishes - only `purpose` and
# `semantic-identity` do, and those are prose. What it offers instead is the
# bridge's own operations, which are a genuinely different MECHANISM rather than
# another piece of the same library, and that is the strongest kind of second
# route there is: READ_SELECTION's real proof is exactly this, the UI selection
# store against a FilteredElementCollector query.
#
# Where none applies, the draft says NOT ESTABLISHED and says why. A person
# usually sees the second route in a second; a machine guessing at it produces
# a plausible sentence in a proof, which is the one thing worse than a gap.
#
# THE TABLE IS THIS SHORT BECAUSE THE BRIDGE IS. `count_elements` counts the
# whole active document and takes no argument; `select_by_category` knows four
# spellings of one word, "duct" (RevitOperations.cs, the Categories table).

NATIVE_CROSS_CHECKS = [
    ("count_elements",
     "counts the whole active document by a different route - a cross-check "
     "for anything reporting a model-wide total"),
    ("select_by_category",
     "collects every duct with a FilteredElementCollector and reports how many "
     "- a cross-check for anything counting ducts. Ducts only today; adding a "
     "row to the Categories table in RevitOperations.cs is what widens it"),
]


def cross_checks(frag):
    """The native operations worth running beside this fragment. Often none."""
    out = []
    provided = {(e.get("name") or "").lower() for e in frag.provides()}
    words = ((frag.data.get("semantic-identity") or "") + " "
             + (frag.data.get("capability") or "")).lower()

    if "count" in provided or "count" in words:
        out.append(NATIVE_CROSS_CHECKS[0])
    if "duct" in words:
        out.append(NATIVE_CROSS_CHECKS[1])
    return out


# ---------------------------------------------------------------------------
# 3. The plan
# ---------------------------------------------------------------------------

def build_plan(library, include_all=False):
    """One entry per fragment worth running, most useful first."""
    table = providers(library)

    entries = []
    for frag in library:
        route, note = route_for(frag, table)
        if not include_all and route in (PROVED, WRITE_PATH, UNREACHABLE, NEEDS_VALUES):
            continue
        entry = {
            "slug": frag.slug,
            "capability": frag.data.get("capability"),
            "status": frag.status,
            "route": route,
            "note": note,
            "cross_checks": [name for name, _ in cross_checks(frag)],
            "negative_case": negative_case_plan(frag, route),
        }
        entries.append(entry)

    # A fragment with a native cross-check available is worth running first:
    # it is the only kind that can come out of a run with all three of D-30's
    # legs already recorded, needing nothing from a person but a reading.
    entries.sort(key=lambda e: (PRIORITY.index(e["route"]),
                                0 if e["cross_checks"] else 1,
                                e["slug"]))
    return entries


def negative_case_plan(frag, route):
    """How to arrange an answer that MUST come back empty - the leg D-30 exists for.

    THIS IS THE HALF THAT DECIDES WHETHER A PROOF IS A PROOF, so it is planned
    per fragment rather than left to whoever runs it. The three shapes are real
    and were taken from proofs that already stand:

      STANDALONE  a second model that genuinely lacks the thing. This is how
                  LIST_LEVELS was proved - 11 levels on one model, exactly the
                  2 template levels on the other, in the same sitting.
      FROM_SELECTION  an empty selection. READ_SELECTION's own proof is this, and
                  it is the one that catches a fragment falling back to the
                  active view or to the previous run's elements.
      either      a fragment whose subject the model has none of at all.

    AND THE ONE THING THAT CAN BE ARRANGED WITHOUT A PERSON: `select_by_category`
    calls Selection.SetElementIds with whatever it collected, so asking for a
    category the model does not contain EMPTIES the selection. Today that is
    only "duct", so it works on a model with no ducts and nowhere else. Recorded
    as the narrow thing it is rather than as a general mechanism.
    """
    if route == FROM_SELECTION:
        return ("run it with NOTHING selected. It must report 0 rather than "
                "falling back to the active view, to the whole model, or to "
                "the previous run's elements")
    if route == FROM_CHAIN:
        return ("run it after a producer that returns an empty set. It must "
                "report 0 rather than falling back to the whole model")
    if route in (STANDALONE, RE_PROVE):
        return ("run it a second time against a model that genuinely lacks "
                "what it reports - the empty answer is the evidence, and one "
                "model cannot supply it")
    return "no route to a negative case today, so no proof is possible today"


def print_plan(entries, library):
    proven = len([f for f in library if f.status in HF.NEEDS_PROOF])
    print("Fragment Validation Agent - the run plan")
    print("=" * 72)
    print("")
    print("Library: %d fragments, %d claiming PROVEN." % (len(library), proven))
    print("%d worth running now." % len(entries))
    print("")

    current = None
    for entry in entries:
        if entry["route"] != current:
            current = entry["route"]
            print("")
            print("-- %s " % current + "-" * (68 - len(current)))
        print("  %-32s %s" % (entry["slug"], entry["note"]))
        if entry["cross_checks"]:
            print("  %-32s   second route: %s"
                  % ("", ", ".join(entry["cross_checks"])))
        else:
            print("  %-32s   second route: none this agent can run - the draft "
                  "will say NOT ESTABLISHED" % "")
        print("  %-32s   negative case: %s" % ("", entry["negative_case"]))
    print("")
    print("Nothing here has been run. This is what to run, and what each run has")
    print("to produce before it counts as evidence.")


# ---------------------------------------------------------------------------
# 4. The draft
# ---------------------------------------------------------------------------
#
# A run record is what `heron_bridge_client.py validate` writes: plain JSON,
# one object per phase, no Revit needed to read it. Turning it into a draft is
# arithmetic and honesty - every field either comes from a recorded phase or
# says it was not established.

DRAFT_HEADER = {
    "heron-agent": "HERON-FRG-VAL-001",
    "heron-step": 17,
    "heron-status": "DRAFT",
    "heron-since": "0.1.0",
    "heron-layer": "brain",
}

NOT_ESTABLISHED = "NOT ESTABLISHED"


def draft_from_record(frag, record):
    """A proof draft, built only from what the record actually contains.

    `by` is deliberately empty. heron_fragment.proof_problems requires it, so a
    draft that reaches a fragment.yaml without a person's name on it fails
    validation loudly instead of counting as a proof quietly. That is the one
    rule of this agent, expressed as a missing string rather than as a promise.
    """
    phases = {p.get("phase"): p for p in record.get("phases", [])}

    positive = phases.get("positive")
    negative = phases.get("negative")
    cross = phases.get("second_route")

    gaps = []

    if positive and positive.get("ok"):
        positive_text = describe_phase(positive)
    else:
        positive_text = NOT_ESTABLISHED + " - " + phase_failure(positive)
        gaps.append("the positive case did not run")

    if negative and negative.get("ok"):
        negative_text = describe_phase(negative)
        if not looks_empty(negative):
            negative_text += ("\n\nWARNING: this did NOT come back empty. A "
                              "negative case that returns content is a "
                              "FINDING, not a proof - the fragment may be "
                              "falling back to something it was not given.")
            gaps.append("the negative case returned content instead of nothing")
    else:
        negative_text = NOT_ESTABLISHED + " - " + phase_failure(negative)
        gaps.append("the negative case did not run, so D-30's second leg is missing")

    if cross and cross.get("ok"):
        cross_text = describe_phase(cross)
    else:
        cross_text = (NOT_ESTABLISHED + " - no second route was run. D-30 asks "
                      "for one 'where one exists'; whether one exists here has "
                      "not been settled, and a declared absence is not the same "
                      "as none")
        gaps.append("no second route was run")

    return {
        "fragment": frag.slug,
        "capability": frag.data.get("capability"),
        "drafted_by": "Fragment Validation Agent (HERON-FRG-VAL-001)",
        "drafted_from": record.get("run_record") or "(unnamed run record)",
        "confirmed": False,
        "gaps": gaps,
        "proof-draft": {
            "date": record.get("date") or "",
            # EMPTY ON PURPOSE. See this function's docstring.
            "by": "",
            "model": record.get("model") or NOT_ESTABLISHED,
            "positive_case": positive_text,
            "negative_case": negative_text,
            "second_route": cross_text,
            "fingerprint": frag.fingerprint(),
        },
    }


def describe_phase(phase):
    """What came back, in the words the run recorded. No interpretation added."""
    parts = []
    if phase.get("arranged"):
        parts.append(phase["arranged"])
    provides = phase.get("provides") or {}
    if provides:
        parts.append("returned " + ", ".join(
            "%s %s" % (k, provides[k]) for k in sorted(provides)))
    if phase.get("bound"):
        parts.append("inputs came %s" % phase["bound"])
    if phase.get("document"):
        parts.append("on %s" % phase["document"])
    return ". ".join(parts) if parts else "ran, and recorded nothing about it"


def phase_failure(phase):
    if not phase:
        return "the run record has no such phase"
    return "%s: %s" % (phase.get("error") or "failed",
                       phase.get("message") or "no message recorded")


# The executor renders a collection as "3 item(s) [...]" and a dictionary as
# "3 entry(ies)". Both are counts; neither survives float().
_ITEMS_RE = re.compile(r"^\s*(\d+)\s+(?:item\(s\)|entry\(ies\))")


def _as_count(value):
    """The number a provided value stands for, or None if it cannot be read.

    THE EXECUTOR SENDS STRINGS, AND THAT IS THE WHOLE POINT OF THIS FUNCTION.
    `RevitFragment.Describe` renders a collection as "3 item(s) [a, b, c]" and a
    scalar as its own text, so a count of zero arrives as the STRING "0" and an
    empty list as "0 item(s)". Comparing those with len() - which is what this
    module did until 2026-09-07 - makes "0" a one-character string and therefore
    "not empty", so looks_empty could never return True for anything this
    executor produced, and EVERY negative case was flagged.

    A warning that always fires is worse than no warning, because it is the one
    people learn to skip past.
    """
    if isinstance(value, bool):
        return 0
    if isinstance(value, (int, float)):
        return value
    if isinstance(value, (list, dict)):
        return len(value)
    if not isinstance(value, str):
        return None

    text = value.strip()
    if text == "":
        return 0
    if text.lower() in ("true", "false"):
        return 0                      # a flag, not a quantity - see below
    match = _ITEMS_RE.match(text)
    if match:
        return int(match.group(1))
    try:
        return float(text)
    except ValueError:
        return None


# Names that carry PROSE rather than a quantity. `findings` is the library's
# convention for the sentences a fragment wants read aloud - 134 of the 349
# fragments provide one - and a fragment that correctly reports "there are none
# here" still fills it. See D-51: the counts decide, the note does not.
NOTE_KEYS = frozenset(("findings",))

# Names that count what the fragment was GIVEN and could not report on, rather
# than what it FOUND. `noSystem: 16` does not mean sixteen systems; it means
# sixteen elements were examined and none had one. See D-52.
#
# The camelCase boundary is load-bearing: `no|not|un` must be followed by a
# CAPITAL, so `notes` and `nodes` are still ordinary result fields. The three
# single words below do not fit that shape and are listed because they were
# actually observed, not because the pattern was widened to admit them.
REJECT_PREFIX = re.compile(r"^(no|not|un)[A-Z]")
REJECT_NAMES = frozenset(("unmeasurable", "unplaced", "unenclosed"))


def _is_accounting(key):
    return bool(REJECT_PREFIX.match(key)) or key in REJECT_NAMES


# `RevitFragment.Describe` renders anything it cannot format as a count or a
# scalar by falling back to `value.GetType().Name` - so `Func\`3`, `HashSet\`1`,
# `SpatialElementBoundaryOptions`, `Action\`3`. Those are HELPERS the fragment
# left in scope, not results: they appear identically in the positive and the
# negative run and say nothing about what was found.
#
# This is the same class of mistake as reading the string "0" with len() - the
# checker not recognising its own executor's output - and NOT a loosening of the
# standard. A bare type name is not a quantity that could have been zero.
_TYPE_NAME = re.compile(r"^[A-Za-z_][A-Za-z0-9_.]*(?:`\d+)?$")


def _is_helper_object(value):
    if not isinstance(value, str):
        return False
    text = value.strip()
    if text.lower() in ("true", "false", ""):
        return False          # a flag, handled as a quantity of zero
    if not _TYPE_NAME.match(text):
        return False
    try:
        float(text)
    except ValueError:
        return True           # an identifier, not a number
    return False


def looks_empty(phase):
    """Whether a phase's numbers really do read as nothing.

    THE COUNTS DECIDE (D-51), AND ONLY THE COUNTS OF WHAT WAS FOUND (D-52).
    A fragment that finds nothing still writes a sentence saying so, and it
    still says how many things it looked at and turned down - `noSystem: 16`,
    `notSpatial: 28`, `unmeasurable: 28`. Neither is evidence it found
    something; both are evidence it ran, which silence would not be. Requiring literal silence flagged every
    negative case a reporting fragment could ever produce, and a warning that
    always fires is the one people learn to skip past.

    Still deliberately conservative in the two ways that matter:

      * a value it cannot read as a number returns False, so an unrecognised
        shape gets a person's attention rather than a shrug;
      * a phase that provides NOTHING BUT prose returns False, because there is
        then no count to judge and "the note was written" is not a measurement.

    Booleans are flags, not quantities, however they arrive - as a Python bool
    or as the string "true". `REPORT_GLOBAL_PARAMETERS` returns `allowed: true`
    in the positive AND the negative case, because the document permits globals
    either way, and counting that as content would make an empty answer
    impossible for it to demonstrate.
    """
    provides = phase.get("provides") or {}
    if not provides:
        return False

    counted = 0
    for key, value in provides.items():
        if key in NOTE_KEYS or _is_accounting(key):
            continue
        if _is_helper_object(value):
            continue
        count = _as_count(value)
        if count is None:
            return False              # unreadable shape - say so, do not assume
        if count != 0:
            return False
        counted += 1

    # Nothing but prose. There is no number here to have been zero.
    return counted > 0


# ---------------------------------------------------------------------------
# 5. Where a draft lives, and the one door into the library
# ---------------------------------------------------------------------------
#
# THE OPEN QUESTION THE BRIEF LEFT: a `proof-draft:` key inside fragment.yaml,
# or a separate file? Separate file, and the reason is not tidiness.
#
# A key in fragment.yaml means this agent needs a write path into
# brain/fragments/. Once that exists, "it never writes heron-status" is a
# property of the code being correct. With drafts in their own folder, the
# agent has no route into the library at all, and the rule holds because there
# is nothing to break - which is the difference between a guarantee and a
# promise. It also keeps unreviewed prose out of the library that the search,
# the graph and the routing checks all read.
#
# The cost is real and worth naming: one fact now has two homes until a person
# accepts it, which is what docs/29 warns about. That is why `accept` copies and
# then DELETES the draft - the split is temporary by construction.

def draft_path(slug):
    return os.path.join(DRAFTS_DIR, "%s.yaml" % slug)


# ---------------------------------------------------------------------------
# Editing a fragment.yaml WITHOUT rewriting it
# ---------------------------------------------------------------------------
#
# NEVER LOAD A fragment.yaml AND DUMP IT BACK. A round trip through yaml.safe_dump
# is silently destructive here: every ROUTING block is a YAML COMMENT, and a
# dumper cannot see a comment, so it writes a file that parses identically and
# has lost the part a human reads. list-levels alone carries 27 lines of routing
# - the record of which sentence belongs to which fragment and why one of them
# is deliberately left mis-ranked. That is not decoration; it is the most
# expensive knowledge in the file.
#
# So both writers below are TEXT edits: they change the lines they mean to change
# and leave every other byte exactly where it was.

def _block_bounds(text, key):
    """(start, end) line indices of a top-level `key:` block, or None.

    A block runs from its key line to the next line that starts in column 0 and
    is not blank. A comment in column 0 ENDS the block, which is what keeps a
    routing comment sitting after the proof from being swallowed by it.
    """
    lines = text.split("\n")
    start = None
    for index, line in enumerate(lines):
        if start is None:
            if line.startswith(key + ":"):
                start = index
            continue
        if line and not line[0].isspace():
            return start, index
    if start is None:
        return None
    return start, len(lines)


def splice_proof(text, proof):
    """`text` with its proof block replaced, or inserted before `revit:`.

    Existing proofs sit immediately above `revit:` in both fragments that carry
    one, and `revit` is a REQUIRED key, so it is an anchor that always exists.
    """
    rendered = yaml.safe_dump({"proof": proof}, allow_unicode=True,
                              sort_keys=False, default_flow_style=False,
                              width=88).rstrip("\n").split("\n")

    lines = text.split("\n")
    bounds = _block_bounds(text, "proof")
    if bounds:
        start, end = bounds
        return "\n".join(lines[:start] + rendered + lines[end:])

    anchor = _block_bounds(text, "revit")
    if not anchor:
        raise ValueError(
            "this fragment.yaml has no `revit:` key, so there is no anchor to "
            "put a proof above. Nothing was written.")
    at = anchor[0]
    return "\n".join(lines[:at] + rendered + [""] + lines[at:])


def splice_fingerprint(text, value):
    """`text` with the proof's fingerprint line replaced. Nothing else moves."""
    bounds = _block_bounds(text, "proof")
    if not bounds:
        raise ValueError("no proof block to re-stamp")
    start, end = bounds
    lines = text.split("\n")
    for index in range(start, end):
        stripped = lines[index].lstrip()
        if stripped.startswith("fingerprint:"):
            indent = lines[index][:len(lines[index]) - len(stripped)]
            lines[index] = '%sfingerprint: "%s"' % (indent, value)
            return "\n".join(lines)
    raise ValueError("the proof block has no fingerprint line to re-stamp")


def write_draft(slug, draft):
    """Write a draft. Refuses any path inside the fragment library."""
    path = draft_path(slug)
    library = os.path.join(ROOT, "brain", "fragments")
    if os.path.abspath(path).startswith(os.path.abspath(library) + os.sep):
        raise ValueError(
            "refusing to write a draft inside brain/fragments/. This agent has "
            "no write path into the library, and that is what makes 'it never "
            "signs' a guarantee rather than a promise.")

    if not os.path.isdir(DRAFTS_DIR):
        os.makedirs(DRAFTS_DIR)

    body = dict(DRAFT_HEADER)
    body.update(draft)
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(yaml.safe_dump(body, allow_unicode=True, sort_keys=False,
                                default_flow_style=False, width=88))
    return path


def read_draft(slug):
    path = draft_path(slug)
    if not os.path.isfile(path):
        return None
    with io.open(path, "r", encoding="utf-8") as fh:
        return yaml.safe_load(fh.read())


def accept(slug, by, library=None):
    """A PERSON confirms a draft. Copies the proof in; never touches the status.

    `by` is the signature, typed at the moment of acceptance. There is no way to
    reach this function without a person putting their name on it, which is the
    point: D-30 says "whoever ran it records it, under their name and the date,
    not a tick".

    The status is read before and after and restored if it moved. That check
    should never fire - nothing here writes it - and it exists precisely because
    "nothing here writes it" is the sort of sentence that stops being true when
    somebody adds a feature.
    """
    if not by or not by.strip():
        return 2, "accept needs a person's name: --by \"Your Name\""

    draft = read_draft(slug)
    if draft is None:
        return 2, "no draft for '%s' in brain/proof-drafts/" % slug

    library = library if library is not None else load_library()
    match = [f for f in library if f.slug == slug]
    if not match:
        return 2, "no fragment called '%s'" % slug
    frag = match[0]

    proof = dict(draft.get("proof-draft") or {})
    if not proof:
        return 2, "the draft for '%s' carries no proof-draft block" % slug

    missing = [k for k in HF.PROOF_REQUIRED if k != "by" and not proof.get(k)]
    if missing:
        return 2, ("the draft is not complete - no %s. A draft with a gap in it "
                   "is a finding to act on, not a proof to accept"
                   % ", ".join(missing))

    proof["by"] = by.strip()

    path = os.path.join(frag.folder, "fragment.yaml")
    with io.open(path, "r", encoding="utf-8") as fh:
        before = fh.read()
    status_before = frag.status

    try:
        after_text = splice_proof(before, proof)
    except ValueError as exc:
        return 2, str(exc)
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(after_text)

    after = HF.load(frag.folder)
    if after.status != status_before:
        with io.open(path, "w", encoding="utf-8") as fh:
            fh.write(before)
        return 1, ("the status moved from %s to %s while writing the proof. "
                   "Nothing was saved. This agent must never change a status "
                   "and something just did" % (status_before, after.status))

    os.remove(draft_path(slug))
    return 0, ("proof recorded on %s by %s. Status is still %s - promoting it "
               "is a separate, deliberate act, and it is yours"
               % (slug, by.strip(), after.status))


# ---------------------------------------------------------------------------
# 6. The fingerprint, and why 16 proofs read as stale on a machine that is
#    not Ajmal's PC
# ---------------------------------------------------------------------------
#
# Measured 2026-09-06 in a Linux container: ALL SIXTEEN proven fragments report
# STALE, and `python brain/heron_fragment.py` fails with sixteen problems. None
# of them is stale. Every implementation predates its own proof date - git says
# so, file by file - and every recorded fingerprint is reproducible exactly by
# hashing the SAME bytes with backslash paths and CRLF line endings.
#
# So the fingerprint was a fact about the operating system as much as about the
# code. It is stamped on Windows and checked on Linux, and it can never agree.
#
# heron_fragment.fingerprint() now normalises both, so the number is a fact about
# the content alone. The sixteen recorded values were taken under the old rule
# and have to be re-recorded once - `restamp` does that, and REFUSES any fragment
# whose implementation changed after its proof date, because that one really is
# stale and re-stamping it would erase the only signal saying so.

def restamp(library=None, apply_changes=False):
    """Re-record fingerprints that differ only because of the platform.

    Returns (rows, refused). A row is (slug, old, new). `refused` names the
    fragments whose code genuinely moved after the proof was taken; those are
    stale in the way the mechanism exists to catch and are left exactly as they
    are.
    """
    library = library if library is not None else load_library()
    rows, refused = [], []

    for frag in sorted(library, key=lambda f: f.slug):
        if frag.status not in HF.NEEDS_PROOF or not frag.proof:
            continue
        recorded = frag.proof.get("fingerprint")
        current = frag.fingerprint()
        if recorded == current:
            continue

        if implementation_changed_after(frag):
            refused.append(frag.slug)
            continue

        rows.append((frag.slug, recorded, current))
        if apply_changes:
            path = os.path.join(frag.folder, "fragment.yaml")
            with io.open(path, "r", encoding="utf-8") as fh:
                before = fh.read()
            status_before = frag.status
            with io.open(path, "w", encoding="utf-8") as fh:
                fh.write(splice_fingerprint(before, current))
            if HF.load(frag.folder).status != status_before:  # pragma: no cover
                with io.open(path, "w", encoding="utf-8") as fh:
                    fh.write(before)
                raise RuntimeError("restamp changed a status - it must not")

    return rows, refused


def implementation_changed_after(frag):
    """Did the code move after the proof was taken? Unknown counts as YES.

    Asked of git, because the alternative is trusting the fingerprint - which is
    the thing under suspicion. No git, no answer, and no answer means refuse:
    the whole point of this guard is that a genuinely stale proof must not be
    re-stamped back into looking fresh.
    """
    proof_date = str((frag.proof or {}).get("date") or "").strip()
    if not proof_date:
        return True
    try:
        result = subprocess.run(
            ["git", "log", "-1", "--format=%ad", "--date=short", "--"]
            + frag.proof_files(),
            cwd=ROOT, capture_output=True, text=True)
    except OSError:                                          # pragma: no cover
        return True
    if result.returncode != 0:                               # pragma: no cover
        return True
    last = result.stdout.strip()
    if not last:
        return True
    return last > proof_date


# ---------------------------------------------------------------------------
# 7. The audit log
# ---------------------------------------------------------------------------
#
# READ IT BEFORE RUNNING ANYTHING - some fragments have already failed for
# reasons that have nothing to do with the fragment.
#
# AND KNOW WHAT IT CANNOT TELL YOU, which is more than the brief assumed. Read
# on 2026-09-06 from the add-in's own writer: RevitDispatcher records `op`,
# `session`, `document`, `error` and `ms`. It does NOT record which fragment
# ran. So 285 `run_fragment_read` entries say that a fragment ran and whether it
# worked, and nothing at all about WHICH - failures cannot be attributed, and no
# amount of reading fixes that.
#
# The fix is one field in RevitDispatcher.Record and it needs a Windows build,
# so it is named here rather than half-done: until it lands, this reads as
# op-level history and says so.
#
# The path is passed in. brain/ does not resolve %APPDATA% - only HeronPaths.cs
# and the bridge client may (tools/check-structure.py), and that rule is what
# stops two parts of Heron disagreeing about where something lives.

def read_audit(paths):
    entries = []
    for path in paths:
        if not os.path.isfile(path):
            continue
        with io.open(path, "r", encoding="utf-8") as fh:
            for line in fh:
                line = line.strip()
                if not line:
                    continue
                try:
                    entries.append(json.loads(line))
                except ValueError:
                    # A truncated last line is normal in an append-only log that
                    # was being written when it was read. One unreadable line
                    # costs one line.
                    continue
    return entries


def summarise_audit(entries):
    ops, failures = {}, {}
    for entry in entries:
        op = entry.get("op") or "(none)"
        ok = bool(entry.get("ok"))
        ops[op] = ops.get(op, 0) + 1
        if not ok:
            reason = entry.get("error") or "(no error recorded)"
            failures[reason] = failures.get(reason, 0) + 1
    return {
        "entries": len(entries),
        "ok": len([e for e in entries if e.get("ok")]),
        "failed": len([e for e in entries if not e.get("ok")]),
        "by_op": ops,
        "by_error": failures,
        "attributable_to_a_fragment": 0,
    }


# ---------------------------------------------------------------------------
# Commands
# ---------------------------------------------------------------------------

def load_library():
    found, problems = HF.load_all()
    for problem in problems:                                 # pragma: no cover
        sys.stderr.write("unreadable: %s\n" % problem)
    return list(found.values())


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="The Fragment Validation Agent. It never sets heron-status.")
    sub = parser.add_subparsers(dest="command")

    p_plan = sub.add_parser("plan", help="what can be proved, and how")
    p_plan.add_argument("--all", action="store_true",
                        help="include what cannot be run, and why")
    p_plan.add_argument("--json", action="store_true")

    p_draft = sub.add_parser("draft", help="turn a run record into a proof draft")
    p_draft.add_argument("slug")
    p_draft.add_argument("--from", dest="record", required=True)

    p_review = sub.add_parser("review", help="read the drafts")
    p_review.add_argument("slug", nargs="?")

    p_accept = sub.add_parser("accept", help="a PERSON confirms a draft")
    p_accept.add_argument("slug")
    p_accept.add_argument("--by", required=True, help="your name. This is the signature")

    p_stamp = sub.add_parser("restamp", help="fix platform-skewed fingerprints")
    p_stamp.add_argument("--apply", action="store_true",
                         help="write the changes. Without this it only reports")

    p_audit = sub.add_parser("audit", help="read an audit log you point it at")
    p_audit.add_argument("--file", action="append", required=True)

    args = parser.parse_args(argv)
    if not args.command:
        parser.print_help()
        return 2

    if args.command == "plan":
        library = load_library()
        entries = build_plan(library, include_all=args.all)
        if args.json:
            print(json.dumps(entries, indent=2, sort_keys=True))
        else:
            print_plan(entries, library)
        return 0

    if args.command == "draft":
        library = load_library()
        match = [f for f in library if f.slug == args.slug]
        if not match:
            print("No fragment called '%s'." % args.slug)
            return 2
        with io.open(args.record, "r", encoding="utf-8") as fh:
            record = json.loads(fh.read())
        draft = draft_from_record(match[0], record)
        path = write_draft(args.slug, draft)
        print("Drafted %s" % os.path.relpath(path, ROOT))
        for gap in draft["gaps"]:
            print("  gap: %s" % gap)
        print("")
        print("Nothing has been promoted. Read it, then:")
        print("  python brain/heron_validate.py accept %s --by \"Your Name\"" % args.slug)
        return 0

    if args.command == "review":
        if not os.path.isdir(DRAFTS_DIR):
            print("No drafts yet.")
            return 0
        names = ([args.slug] if args.slug
                 else sorted(n[:-5] for n in os.listdir(DRAFTS_DIR)
                             if n.endswith(".yaml")))
        if not names:
            print("No drafts yet.")
            return 0
        for name in names:
            draft = read_draft(name)
            if draft is None:
                print("No draft for '%s'." % name)
                continue
            print("=" * 72)
            print(yaml.safe_dump(draft, allow_unicode=True, sort_keys=False,
                                 default_flow_style=False, width=88))
        return 0

    if args.command == "accept":
        code, message = accept(args.slug, args.by)
        print(message)
        return code

    if args.command == "restamp":
        rows, refused = restamp(apply_changes=args.apply)
        if not rows and not refused:
            print("Every recorded fingerprint already matches. Nothing to do.")
            return 0
        for slug, old, new in rows:
            print("  %-32s %s -> %s" % (slug, old, new))
        if refused:
            print("")
            print("REFUSED - the code moved after the proof was taken, so these")
            print("are genuinely stale and must be re-proved, not re-stamped:")
            for slug in refused:
                print("  %s" % slug)
        if not args.apply:
            print("")
            print("Nothing was written. Add --apply to record these.")
        return 0

    if args.command == "audit":
        summary = summarise_audit(read_audit(args.file))
        print("Audit: %d entries, %d ok, %d failed"
              % (summary["entries"], summary["ok"], summary["failed"]))
        for op in sorted(summary["by_op"], key=lambda k: -summary["by_op"][k]):
            print("  %-24s %d" % (op, summary["by_op"][op]))
        if summary["by_error"]:
            print("")
            print("What failed:")
            for err in sorted(summary["by_error"], key=lambda k: -summary["by_error"][k]):
                print("  %-40s %d" % (err[:40], summary["by_error"][err]))
        print("")
        print("NONE of these can be attributed to a fragment. The add-in records")
        print("the operation, not which fragment ran under it - so this is")
        print("op-level history and cannot say which fragment failed.")
        return 0

    parser.print_help()                                      # pragma: no cover
    return 2


if __name__ == "__main__":
    sys.exit(main())
