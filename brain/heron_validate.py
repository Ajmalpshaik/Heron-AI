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
    python brain/heron_validate.py draft <slug> --release 2025 --from <record>
    python brain/heron_validate.py accept <slug> --release 2025 --by "Your Name"
    python brain/heron_validate.py draft <slug> --second-route --from <record>
    python brain/heron_validate.py accept <slug> --second-route --by "Your Name"
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
   restores it if `heron-status` moved by so much as a character. Its two
   D-131 forms write less still: `--release` one entry of `proof.releases`,
   `--second-route` the one `second_route` line - and each restores the file
   if any other field of the proof read back different.
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
- **Never marks a fragment it could not run.** Measured off disk on 2026-09-06,
  279 of the 349 fragments there were then need a value only a person's sentence
  carries (a category, a name, a distance), and there is no route for those.
  They are listed, per fragment, with the value they are waiting for.
- **Never touches the write path.** `run_fragment_read` opens no transaction. A
  fragment at anything other than `risk: READ` is out of scope until Phase 1 is
  proven, and `plan` will not schedule one.
"""

import argparse
import hashlib
import io
import re
import json
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as HF                                       # noqa: E402
# Which releases a proof and its entries name. The matrix owns reading that,
# prose and field alike, and a second reader here would be a second answer.
import heron_matrix as MX                                         # noqa: E402

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
        # A switch the card lets be left out is not waited on - the add-in
        # binds it false (HF.need_may_be_absent, row 5b-252) - and nor is a
        # value whose card names its default (`absent:`, row 5b-488).
        if source == "request" and not HF.need_may_be_absent(need):
            wanted.append(need)
    if wanted:
        names = ", ".join(sorted(n.get("name") for n in wanted))
        return NEEDS_VALUES, ("waiting on a value only the request carries: %s"
                              % names)

    from_fragment = [n for n in frag.needs() if HF.need_source(n) == "fragment"]
    if not from_fragment:
        return STANDALONE, "runs on the open document alone"
    # An element list the card lets be empty is not waited on either - the
    # add-in binds it empty when nothing is selected (HF.need_may_be_empty).
    if all(HF.need_may_be_empty(n) for n in from_fragment):
        return STANDALONE, ("runs on the open document alone, or on what is "
                            "selected: %s may be left empty"
                            % ", ".join(sorted(n.get("name") for n in from_fragment)))

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
        declared, warning = declared_negative(frag)
        by_hand = declared_second_route(frag)
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
            "declared_negative": declared,
            "declared_second_route": by_hand,
            "negative_warning": warning,
        }
        entries.append(entry)

    # A fragment with a native cross-check available is worth running first:
    # it is the only kind that can come out of a run with all three of D-30's
    # legs already recorded, needing nothing from a person but a reading.
    entries.sort(key=lambda e: (PRIORITY.index(e["route"]),
                                0 if e["cross_checks"] else 1,
                                e["slug"]))
    return entries


# The arrangement that was disproved. NEEDS-CHECKING.md, 2026-09-07: an empty
# selection reaches an `elements` fragment as an unbound need, so it is refused
# rather than answered, and a refusal is not evidence of an honest empty answer.
# Eighteen fragments still declare their negative case this way.
DISPROVED_NEGATIVE = re.compile(
    r"(empty (element|selection) list|an empty selection|nothing selected"
    r"|no elements selected)", re.I)


def declared_second_route(frag):
    """
    The second route this fragment's own tests/cases.yaml names.

    D-30 wants a third leg: a different way to reach the same fact. `plan` had
    only ever offered the bridge's own operations, and said "none this agent
    can run" for everything else - 329 of 349 fragments. Every one of those 349
    declares a second route in its case file, and it was never read.

    They are almost all a PERSON's route rather than an agent's - "Revit's own
    Edit Type / Structure dialog", "the Project Browser", "Manage / Project
    Information". That is exactly why the plan should print them: the agent
    cannot run them, and the person about to sit in front of Revit can.

    Kept out of draft_from_record on purpose. A draft records what was actually
    RUN, and printing a route into a draft nobody followed would manufacture
    the evidence D-30 exists to demand.
    """
    cases, problem = frag.cases()
    if problem:
        return []
    routes = []
    rows = cases.get("second_route") or []
    # A case file may write its route as ONE folded sentence rather than a
    # list. Walking that string handed back its first LETTER as the route, so
    # `plan` told the person at the PC "A" for select-by-parameter-value and
    # "R" for set-sheet-title-block. Row 5b-511.
    if isinstance(rows, str):
        rows = [rows]
    for row in rows:
        if isinstance(row, dict):
            row = row.get("given") or row.get("route") or ""
        if row:
            routes.append(" ".join(str(row).split()))
    return routes


def declared_negative(frag):
    """
    What this fragment's OWN tests/cases.yaml says would prove it empty.

    Preferred over the generic per-route advice for the reason the generic
    advice had to be corrected at all: the useful negative case is specific to
    what the fragment reports - "a curtain wall", "a schedule as the view", "a
    set containing a duct and a door" - and no rule derived from the contract
    can know that. The author already wrote it down. Nothing read it.

    Returns (givens, warning). The warning is set when the declared arrangement
    is the one that was tried on 36 fragments and produced 36 refusals.
    """
    cases, problem = frag.cases()
    if problem:
        return [], "tests/cases.yaml cannot be read: %s" % problem

    givens = []
    for row in (cases.get("negative") or []):
        if isinstance(row, dict) and row.get("given"):
            givens.append(" ".join(str(row["given"]).split()))

    warning = None
    if not _reads_the_selection_itself(frag):
        stranded = [g for g in givens if DISPROVED_NEGATIVE.search(g)]
        workable = len(givens) - len(stranded)
        if stranded:
            # WHETHER THIS BLOCKS THE PROOF IS THE USEFUL HALF, and it is why
            # this counts rather than just flagging. Every one of the eighteen
            # fragments carrying a stranded case also carries at least one
            # workable one, so not one of them is actually blocked - and a
            # warning that reads like a blocker on eighteen fragments nobody
            # needs to touch is how a real finding gets skimmed past.
            #
            # NOT DELETED, on purpose. These carry real requirements -
            # ISOLATE_ELEMENTS asks for "the view LEFT AS IT WAS", which is a
            # safety rule, not a test. The executor refuses before the fragment
            # runs, so the requirement is unreachable rather than untested, and
            # throwing the sentence away would lose the rule along with the
            # unusable arrangement. Rewriting it into a reachable one is a
            # judgement about how Revit behaves for THAT fragment, which wants
            # a real model and the person who wrote it.
            warning = (
                "%d declared negative case(s) cannot be run - %r. This "
                "fragment is fed `elements`, so an empty list is refused as "
                "needs_unbound and it never runs; the requirement inside it "
                "is unreachable rather than untested. %s"
                % (len(stranded), stranded[0],
                   ("NOT blocking: %d workable negative case(s) remain"
                    % workable) if workable else
                   "BLOCKING: no workable negative case remains, so this "
                   "fragment cannot be proved at all"))
    return givens, warning


def _reads_the_selection_itself(frag):
    """
    Does this fragment ask Revit for the selection, rather than being handed it?

    The distinction decides what its negative case even IS. One that takes
    `uidoc` does the asking, so an empty selection is a real empty answer. One
    that takes `elements` is HANDED a list, and an empty list is
    indistinguishable from never having been asked - so it is refused instead,
    and cannot be proved that way.
    """
    contract = frag.data.get("contract") or {}
    names = set()
    for item in (contract.get("needs") or []):
        if isinstance(item, dict) and item.get("name"):
            names.add(item["name"])
    return "uidoc" in names and "elements" not in names


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
        # CORRECTED 2026-09-07, and the correction cost 36 fragment runs.
        #
        # This used to read "run it with NOTHING selected. It must report 0."
        # That instruction was carried out, on 36 unproven READ fragments in
        # one pass, and produced 36 refusals and zero proofs: a fragment fed
        # `elements` cannot BIND an empty selection, so it returns
        # needs_unbound rather than an empty answer. The executor was right
        # and the plan was wrong. NEEDS-CHECKING.md recorded the lesson the
        # same day; this function, which issues the instruction, did not.
        #
        # A fragment that takes `uidoc` and reads the selection ITSELF is the
        # exception, and it is why the distinction is real rather than
        # pedantic: it can tell "nothing selected" from "nobody asked",
        # because it did the asking. READ_SELECTION's own proof is that case.
        if _reads_the_selection_itself(frag):
            return ("run it with NOTHING selected. It takes uidoc and reads "
                    "the selection itself, so an empty selection reaches it "
                    "as a real empty answer rather than an unbound need")
        return ("select something that contains NONE of what it reports - "
                "ducts selected for a fragment that reports curtain walls. "
                "NOT an empty selection: it is fed `elements`, so nothing "
                "selected is refused as needs_unbound and the fragment never "
                "runs. A refusal is not an empty answer")
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
        elif entry["declared_second_route"]:
            print("  %-32s   second route: none this agent can run. Its case "
                  "file names one for a PERSON to run:" % "")
            for route in entry["declared_second_route"][:2]:
                print("  %-32s     %s" % ("", route))
        else:
            print("  %-32s   second route: none this agent can run, and none "
                  "declared - the draft will say NOT ESTABLISHED" % "")
        print("  %-32s   negative case: %s" % ("", entry["negative_case"]))
        for given in entry["declared_negative"][:3]:
            print("  %-32s     declared: %s" % ("", given))
        if entry["negative_warning"]:
            print("  %-32s   ** %s" % ("", entry["negative_warning"]))
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


def source_hash(frag):
    """The hash of a fragment's C# as the client SENDS it, or None. D-120.

    The bridge client records this over the text it sent when a run is kept
    through the box in Revit (`try-once`). Read here the same way - text mode,
    universal newlines, sixteen characters - so the same file gives the same
    answer on both sides; tests/test_try_once.py holds the two together,
    because the client is stdlib only and cannot import this.

    NOT `frag.fingerprint()`. That hashes every file under impl/ with their
    paths, which is the right question for a proof going stale; this one asks
    a narrower thing - is this the code that RAN - and the run sent one file.
    """
    folder = getattr(frag, "folder", None)
    if not folder:
        return None
    path = os.path.join(folder, "impl", "any", "fragment.cs")
    try:
        with io.open(path, "r", encoding="utf-8") as fh:
            text = fh.read()
    except (OSError, UnicodeDecodeError):
        return None
    return hashlib.sha256(text.encode("utf-8")).hexdigest()[:16]


def kept_problem(frag, phase):
    """Why a positive phase taken from a KEPT run cannot stand, or None. D-120.

    A phase that carries `kept` came from the fragment's one run on the live
    model, through the box in Revit. It is the positive case only when the
    modeller pressed Keep, and only for the code that ran - so a discarded run,
    or code that has moved since, is refused here, by `draft` and by `accept`
    alike. A phase without `kept` is an ordinary run and is not this
    function's business.
    """
    if not phase or "kept" not in phase:
        return None
    if phase.get("kept") is not True:
        return ("the positive phase is a run the modeller did NOT keep, so it is no "
                "evidence the fragment works (D-120)")
    current = source_hash(frag)
    if not phase.get("source_hash") or phase.get("source_hash") != current:
        return ("the kept run was taken against different code - it ran %s and the "
                "fragment is now %s. A kept run is evidence for the code that ran and "
                "no other (D-120)" % (phase.get("source_hash"), current))
    return None


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

    # D-120: a positive taken from a kept run stands only if it was kept, and
    # only for the code that ran.
    kept_wrong = kept_problem(frag, positive)
    if positive and positive.get("ok") and kept_wrong:
        positive_text = NOT_ESTABLISHED + " - " + kept_wrong
        gaps.append("the kept run cannot be the positive case")
    elif positive and positive.get("ok"):
        positive_text = describe_phase(positive)
    else:
        positive_text = NOT_ESTABLISHED + " - " + phase_failure(positive)
        gaps.append("the positive case did not run")

    # D-53. SOME FRAGMENTS CANNOT COME BACK EMPTY, and that is a property of what
    # they are rather than a weakness. COUNT_ELEMENTS describes whatever it is
    # handed; there is no selection that makes it report nothing, short of an
    # unbound need, which is a refusal rather than an answer. For those, the leg
    # D-30 wants - proof the fragment is not "succeeding and doing nothing" - is
    # met by the answer TRACKING the input across several different inputs.
    # A fragment ignoring its input, or falling back to the whole model, cannot
    # match five different counts exactly.
    tracking = record.get("tracking")
    if tracking:
        # WHAT WAS VARIED IS NOT ALWAYS A SELECTION. D-53 was written while
        # proving COUNT_ELEMENTS, where it always was, so the wording said
        # "selections" and each row read "28 selected -> count 28". A fragment
        # that reads a VIEW cannot come back empty either - every Revit view
        # hides something by default, so REPORT_CATEGORY_VISIBILITY has no
        # empty case in ANY model, checked across 189 views in two - and its
        # rows vary by view, not by selection.
        #
        # A row saying "selected" about a view would describe an arrangement
        # nobody made, which is the one thing a proof may never do. So a row
        # may carry `input` instead, and the wording follows it.
        varied = "input" if any(t.get("input") for t in tracking) else "selection"
        rows = "\n".join(
            "      %s -> %s %s" % (t.get("input") or ("%s selected" % t.get("selected")),
                                   t.get("field"), t.get("value"))
            for t in tracking)
        negative_text = (
            "CANNOT COME BACK EMPTY, so proved by TRACKING instead (D-53). "
            "%s describes whatever it is given, so no arrangement makes its answer "
            "empty. What D-30's negative case exists to catch - a fragment that "
            "succeeds while doing nothing, or answers about a set it was not given "
            "- is caught here by the answer following the input exactly across "
            "%d different %ss:\n\n%s\n\n"
            "    Different inputs, exact matches every time. A fragment falling back "
            "to the active view, the whole model, or a previous run could not do "
            "that."
            % (frag.slug, len(tracking), varied, rows))
    elif negative and negative.get("ok"):
        negative_text = describe_phase(negative)
        # `provides` is a method on Fragment; a stub in the tests may expose it
        # as a plain list, and a record can be drafted for a fragment whose
        # contract cannot be read at all - in which case judge everything,
        # which is the stricter behaviour and the right default.
        strings = None
        try:
            declaration = frag.provides() if callable(frag.provides) else frag.provides
            # ONLY WHAT THE FRAGMENT ACTUALLY WROTE DOWN, which is now every
            # provide it has: `heron_fragment.py` requires a role, so this map
            # is total by validation rather than by accident.
            #
            # IT WAS ONCE TOTAL BY ACCIDENT, AND THAT WAS A DEFECT. provide_role()
            # answers "result" for an entry with no `role:` key - right for
            # reading one entry, wrong in this map, because a name mapped to
            # "result" was indistinguishable from one the author had DECLARED as
            # a result, and the judge skipped its naming patterns for anything it
            # found here. So the patterns never ran at all: 102 names across 134
            # fragments - `scanned`, `unplaced`, `noConnectors`, `notASheet` -
            # were judged as findings while D-51 and D-52 sat unable to speak.
            # Found 2026-09-08 proving select-scope-boxes, whose negative case
            # came back with every result at zero and `scanned: 5`. The patterns
            # are gone now; only the declaration decides.
            declared = dict((d.get("name"), d["role"])
                            for d in (declaration or [])
                            if isinstance(d, dict) and d.get("name") and d.get("role"))
            declared_names = set(d.get("name") for d in (declaration or [])
                                 if isinstance(d, dict) and d.get("name"))
            # A sentence result read as `positive_worked` reads it (5b-411).
            strings = string_results(frag)
        except Exception:
            declared = None
        if not looks_empty(negative, declared, declared_names, strings):
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
    if text == "(null)":
        # NOTHING IS THE CLEAREST POSSIBLE EMPTY, and it read as unreadable.
        # RevitFragment.Describe renders a null as "(null)", which is what a
        # creator leaves behind when it declined to create - `created` is an
        # ElementId or a string on 56 fragments, not a list, so it never
        # arrives as "0 item(s)". This returned None, looks_empty refused to
        # judge, and every one of those negative cases was flagged as content.
        # Found 2026-09-08 proving create-level and create-drafting-view.
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

# THE NAMING PATTERNS ARE GONE, AND WHAT THEY WERE IS WORTH KEEPING.
#
# Three of them guessed D-52 from a name. `REJECT_PREFIX` was `^(no|not|un)[A-Z]`
# - the camelCase boundary load-bearing, so `notes` and `nodes` stayed ordinary
# result fields. `REJECT_NAMES` held the five singles that did not fit that
# shape and had been met one at a time: `unmeasurable`, `unplaced`, `unenclosed`,
# `elementsWithNoMaterial`, `withoutJoins`. `WORK_COUNTER` was `scanned|checked`,
# for `jointsChecked` and `constraintsScanned`. Between them they answered, for
# any name nobody had declared, "is this a count of what it FOUND, or of what it
# was HANDED and could not use".
#
# This file already said what should replace them, and now it has:
#
#     THE REAL FIX IS IN THE CONTRACT, NOT HERE. A fragment knows which of its
#     provides are results and which count what it was handed and could not
#     use... A `role: result | accounting` key on each `provides` entry would
#     replace every pattern in this file with something the fragment states
#     about itself.
#
# Every provide in the library now carries an explicit role - 1,192 of them,
# declared on 2026-09-09 by reading what each fragment is FOR rather than what
# its output is called. That reading disagreed with these patterns 166 times,
# IN BOTH DIRECTIONS. `select-unenclosed-rooms` declares `unplaced` and
# `unenclosed`, the two faults it exists to find, and `REJECT_NAMES` swallowed
# both. `set-mep-slope.inGroup` and `flip-elements.cannotFlip` are bookkeeping
# no pattern could see, and a non-zero one of those would have banked a proof
# for a run that changed nothing.
#
# A NAME THAT MATCHES A BOOKKEEPING SHAPE IS A QUESTION, NOT A VERDICT - which
# is why the guessing had to go rather than be widened once more. The blanket
# `without*` rule considered on 2026-09-07 was refused for the same reason:
# `report-routing-preferences.missingFamilies` is that fragment's finding.
#
# NOTHING GUESSES NOW, AND NOTHING FALLS THROUGH TO A DEFAULT EITHER.
# `heron_fragment.py` REQUIRES a role on every provide, so a fragment written
# tomorrow that forgets one fails validation rather than being read as a result.
# `findings` is the single exemption and NOTE_KEYS above is where it lives.


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


def contract_of(frag):
    """(roles, names) in the shape looks_empty expects.

    `names` is WHAT TO JUDGE, so the fragment's own working values are dropped;
    `roles` is only the names carrying an explicit `role:`, so a name the author
    said nothing about still reaches the naming patterns.

    HERE RATHER THAN IN tools/batch-prove.py, where it used to live alone. Two
    callers need it now - the batch runner and `accept` - and a rule with two
    copies is a rule that will disagree with itself.
    """
    declaration = frag.provides() or []
    roles = dict((d.get("name"), d["role"]) for d in declaration
                 if isinstance(d, dict) and d.get("name") and d.get("role"))
    names = set(d.get("name") for d in declaration
                if isinstance(d, dict) and d.get("name"))
    return roles, names


def string_results(frag):
    """The names a fragment declares `type: string` AND `role: result`.

    A FRAGMENT WHOSE RESULT IS A SENTENCE COULD NOT BE BATCH-PROVED. Every
    reading in this module asks for a count moving off zero, and a sentence is
    not one, so `_as_count` answers None and the positive reads UNREADABLE
    however well both legs held. Found 2026-10-10 re-proving `edit-revision`
    on Project2 (Revit 2020): `changed` came back "issued by (blank) ->
    'M.Sagheer'; issued to (blank) -> 'A.Rahmani'; issued" in the positive and
    empty in the negative - the contrast D-30 asks for - and `batch-prove` said
    POSITIVE UNREADABLE. Row 5b-411.

    BOTH KEYS, NEVER ONE. The type says the value is text rather than an object
    rendered as its type name; the role says the text is what the fragment
    FOUND. A sentence declared `role: accounting` - what was already there,
    what was refused, which revision was handed in - says the fragment looked,
    never that it found (D-52), and stays out. So does a string with no role,
    which `heron_fragment.py` would not have passed.

    `provides` may be a plain list on a test stub, as `draft_from_record`
    already allows.
    """
    declaration = frag.provides() if callable(frag.provides) else frag.provides
    return set(d.get("name") for d in (declaration or [])
               if isinstance(d, dict) and d.get("name")
               and d.get("role") == "result"
               and str(d.get("type") or "").strip().lower() == "string")


def _as_found(value):
    """What a declared STRING result found: 0 for nothing, otherwise not 0.

    Empty, blank and "(null)" are nothing, exactly as `_as_count` reads them.
    A value `_as_count` CAN read - "0", "3 item(s)", "true" - keeps that
    reading, so nothing that was readable before reads differently now. Only
    what `_as_count` gives up on, which for a declared string is a sentence,
    counts as one thing found.
    """
    if value is None:
        return 0
    count = _as_count(value)
    return 1 if count is None else count



def _result_appeared(positive, negative, frag):
    """Is a declared result PRESENT in the positive and GONE in the negative?

    The comparison D-30 asks for, for fragments whose result is a thing rather
    than a count. Returns False on anything doubtful - no negative phase, a
    value that is empty in both, a value that is present in both - so it can
    only ever rescue a positive that has a real contrast behind it.
    """
    if not negative:
        return False

    roles, names = contract_of(frag)
    before = positive.get("provides") or {}
    after = negative.get("provides") or {}

    def empty(value):
        text = ("" if value is None else str(value)).strip()
        return text in ("", "(null)", "None") or _as_count(value) == 0

    for key, value in before.items():
        if key not in names or roles.get(key) == "accounting" or key in NOTE_KEYS:
            continue
        if not empty(value) and empty(after.get(key)):
            return True
    return False


def _flag_flipped(positive, negative, frag):
    """Is a declared result a FLAG that is TRUE in the positive and FALSE in
    the negative? Returns the name, or None.

    A boolean is not a quantity and `_as_count` deliberately reads one as zero.
    That rule is right and stays: `report-global-parameters` answers
    `allowed: true` in BOTH legs, because the document permits globals either
    way, and counting that as content would make an empty answer impossible for
    it to demonstrate.

    WHAT IT ALSO DID WAS MAKE A FRAGMENT WHOSE ONLY DECLARED RESULT IS A
    BOOLEAN UNPROVABLE BY ANY ARRANGEMENT. `apply-view-filter` declares
    `applied: bool` and nothing else. On 2026-09-15 it produced exactly the
    contrast D-30 asks for - `applied true` on `Model Linking`, which took the
    filter, against `applied false` on `Project View`, which refused it in
    Revit's own words - and was judged POSITIVE EMPTY. No view, no filter and
    no override could have changed that, because the judge was not reading the
    value at all.

    SO ASK THE COMPARISON RATHER THAN THE COUNT. That is stronger than counting
    one leg, not weaker, and it is the same move `_result_appeared` makes for a
    result that is a thing rather than a number:

      * a flag TRUE in both phases - the case the zero rule exists for - cannot
        pass this;
      * a fragment that succeeds while doing nothing reports the same flag both
        times and cannot pass it either;
      * only a declared result that is TRUE where the work was possible and
        FALSE where it was not gets through.

    Returns the NAME rather than a bare true, because the verdict line is worth
    saying out loud: which flag flipped is the whole evidence.
    """
    if not negative or not negative.get("ok"):
        return None

    roles, names = contract_of(frag)
    before = positive.get("provides") or {}
    after = negative.get("provides") or {}

    def flag(value):
        if isinstance(value, bool):
            return value
        if isinstance(value, str) and value.strip().lower() in ("true", "false"):
            return value.strip().lower() == "true"
        return None

    for key in sorted(before):
        if key not in names or roles.get(key) == "accounting" or key in NOTE_KEYS:
            continue
        if flag(before.get(key)) is True and flag(after.get(key)) is False:
            return key
    return None


def positive_worked(phase, frag, expect=None):
    """Did the POSITIVE move a declared result off zero? (reason, detail).

    `reason` is None when it worked.

    MOVED HERE FROM tools/batch-prove.py ON 2026-09-12, unchanged, because the
    SIGNING GATE needs the same question and was not asking it. batch-prove's
    own note called this HOLE 2: heron_validate asked whether the NEGATIVE came
    back empty - D-30's leg, which a fragment that does nothing satisfies
    without trying - and nothing asked the opposite question.

    It was answered in the batch runner and left unanswered in `accept`, so a
    draft whose real result was 0 in BOTH legs read as complete and could be
    signed. Found by checking 65 waiting drafts against it: three passed.

    THREE RULES, EACH FROM A FRAGMENT THAT GOT PAST THE OTHER TWO:

      * **Only what the fragment declared.** The executor reports every variable
        left in scope, including its own working constants, and `MetresPerFoot`
        is never zero.
      * **Only results, never accounting.** Every provide declares which it is,
        so nothing here reads a NAME. `jointsChecked: 15` is a count of work
        done, and `check-flow-direction` passed on it while `bothIn` and
        `bothOut` were 0 in both legs.
      * **Unreadable is not work.** An `OverrideGraphicSettings` object is not a
        count that could have been zero.

    AND ONE READING THAT IS NOT A COUNT: a result declared `type: string` is
    found when it says something and nothing when it is empty - see
    `string_results`. It is never taken for a helper object, because a
    declared string arrives as its own text, and a one-word answer such as
    `SupplyAir` has the same shape as a bare type name. `looks_empty` reads the
    negative the same way, so the same word in both legs is content there and
    cannot pass.
    """
    provides = phase.get("provides") or {}
    roles, names = contract_of(frag)
    strings = string_results(frag)

    if expect:
        judged = [(k, provides.get(k)) for k in expect]
        absent = [k for k, v in judged if v is None]
        if absent:
            return ("POSITIVE UNREADABLE",
                    "the run never reported %s, which `expect:` asked for"
                    % ", ".join(absent))
    else:
        judged = []
        for key, value in provides.items():
            if key not in names:
                continue                        # a working value, not a result
            if roles.get(key) == "accounting":
                continue                        # declared as bookkeeping
            if key in NOTE_KEYS:
                continue                        # prose, never a quantity
            if key not in strings and _is_helper_object(value):
                continue                        # a type name, not a quantity
            judged.append((key, value))

    if not judged:
        return ("POSITIVE UNREADABLE",
                "nothing it returned is a declared result that can be read as a "
                "quantity, so there is no evidence it did anything")

    # THE COUNTS DECIDE (D-51), AND A SENTENCE NEVER OUTRANKS ONE. A string
    # result often READS BACK the state - `set-material-colour`'s `wasColour`
    # and `nowColour` are filled whenever the material exists - so beside a
    # count or a flag that says whether anything changed, a filled sentence
    # is not evidence that it did. Asked for the colour a material already
    # had, that fragment answers `changed 0` with both colours filled, and
    # reading the sentences would have passed it. Found by review before
    # 5b-411 merged. A sentence decides only where no declared result can be
    # read as a count - `edit-revision`, whose one result is `changed` - or
    # where `expect:` names it alone.
    aside = []
    if strings and any(k not in strings and _as_count(v) is not None
                       for k, v in judged):
        aside = sorted(k for k, v in judged if k in strings)
        judged = [(k, v) for k, v in judged if k not in strings]

    unreadable, zeros = [], []
    for key, value in judged:
        count = _as_found(value) if key in strings else _as_count(value)
        if count is None:
            unreadable.append("%s %s" % (key, value))
        elif count != 0:
            return None, "%s %s" % (key, value)
        else:
            zeros.append(key)

    if unreadable:
        return ("POSITIVE UNREADABLE",
                "%s cannot be read as a quantity%s"
                % ("; ".join(unreadable),
                   ", and everything readable was zero (%s)" % ", ".join(zeros)
                   if zeros else ""))

    return ("POSITIVE EMPTY",
            "every declared result came back zero (%s)%s"
            % (", ".join(sorted(zeros)),
               "; beside a count, a sentence is not counted (%s)"
               % ", ".join(aside) if aside else ""))


def looks_empty(phase, declared=None, declared_names=None, strings=None):
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

    `strings` is `string_results(frag)`: those names are read as text, found
    or nothing, the same reading `positive_worked` gives them. Left out, the
    negative is read as it was before 2026-10-11 - and a one-word result in
    it is skipped as a helper object, which is the hole that reading closes.
    """
    provides = phase.get("provides") or {}
    if not provides:
        return False
    strings = strings or ()

    # JUDGE ONLY WHAT THE FRAGMENT SAID IT WOULD PROVIDE. The executor reports
    # every variable left in scope, which includes the fragment's own working
    # values - `MetresPerFoot` 0.3048, `cubic` 0.028, a `Func` it defined. Those
    # are never zero and never change between runs, so judging them meant a
    # fragment could not demonstrate an empty answer no matter what it found.
    #
    # The contract is the authority on which names are results, and using it
    # replaces guesswork about leftovers with the fragment's own declaration.
    #
    # TWO DIFFERENT QUESTIONS, AND THEY WERE ONE ARGUMENT UNTIL 2026-09-08.
    # `declared_names` is WHAT TO JUDGE - every name the contract lists, so the
    # fragment's working values are dropped. `declared` is which of those are
    # ACCOUNTING. They stay two arguments now that the naming patterns are gone,
    # because they still answer different questions: one drops a `Func` the
    # fragment left in scope, the other drops a count of what it could not use.
    roles = declared if isinstance(declared, dict) else None
    judge = declared_names if declared_names else declared
    if judge:
        provides = dict((k, v) for k, v in provides.items() if k in judge)
        if not provides:
            return False

    counted = 0
    for key, value in provides.items():
        # THE FRAGMENT'S OWN WORD, AND NOTHING ELSE. There is no fallback to
        # read a name any more: `heron_fragment.py` requires a role on every
        # provide, so a name arriving here without one is a contract that never
        # passed validation - and judging it as a result is the stricter read.
        if roles is not None and roles.get(key) == "accounting":
            continue
        if key in NOTE_KEYS:
            continue
        if key not in strings and _is_helper_object(value):
            continue
        count = _as_found(value) if key in strings else _as_count(value)
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


# ---------------------------------------------------------------------------
# D-131 - a further release, and a second route, on a fragment already PROVEN
# ---------------------------------------------------------------------------
#
# `proof:` stays the record that made the fragment PROVEN. A run on another
# release becomes one entry of `proof.releases`; a second route fills the one
# `second_route` line. Each is drafted and signed exactly as a proof is, and
# each has its OWN draft and run record, named `<slug>@<release>` and
# `<slug>@second-route`, so neither can be mistaken for - or overwrite - the
# record the proof was signed on. Both names sit where the gitignore already
# covers drafts and runs, and every reader that opens a record by its slug
# keeps finding only the proof's.

SECOND_ROUTE_KEY = "second-route"
RELEASE_SHAPE = re.compile(r"^20\d\d$")


def proof_key(slug, release=None, second_route=False):
    """The name a draft and its run record carry: slug, slug@2025, slug@second-route."""
    if release:
        return "%s@%s" % (slug, release)
    if second_route:
        return "%s@%s" % (slug, SECOND_ROUTE_KEY)
    return slug


def run_record_path(key):
    """Where the run record for `key` is read back from at `accept`."""
    return os.path.join(DRAFTS_DIR, "runs", key + ".json")


def record_release(record):
    """(release, None) for the one Revit release a run record names, or (None, why).

    The client writes the release into `model:` - "..., Revit 2025, session
    123" - so it is read with the matrix's own pattern, and a record naming
    none or two is refused rather than guessed at.
    """
    found = sorted(set(MX.RELEASE_IN_PROSE.findall(str(record.get("model") or ""))))
    if not found:
        return None, "the run record names no Revit release"
    if len(found) > 1:
        return None, "the run record names more than one release (%s)" % ", ".join(found)
    return found[0], None


def release_draft(frag, record, release):
    """A draft of one `proof.releases` entry, from the same judgement a proof gets.

    It is `draft_from_record`'s draft with the second route taken out - that
    is the fragment's, not the release's - and the release and the record's
    own reading of it added, so a run on the wrong Revit is a gap on the page
    before anybody types a name.
    """
    draft = draft_from_record(frag, record)
    proof = draft["proof-draft"]
    gaps = [g for g in draft["gaps"] if g != "no second route was run"]
    named, why = record_release(record)
    if named != release:
        gaps.append("the run is for Revit %s, and %s" % (
            release, why if why else "the run record says it ran on Revit %s" % named))
    draft["gaps"] = gaps
    draft["release"] = release
    draft["proof-draft"] = {
        "release": release,
        "date": proof["date"],
        "by": "",
        "model": proof["model"],
        "positive_case": proof["positive_case"],
        "negative_case": proof["negative_case"],
        "fingerprint": proof["fingerprint"],
    }
    return draft


def second_route_draft(frag, record):
    """A draft of the `second_route` line, from a run that recorded one.

    The route's answer is set BESIDE the fragment's own answer from the same
    run, because a route is only evidence when the two can be compared - and
    the comparison is the person's to make before signing, not this file's.
    """
    phases = {p.get("phase"): p for p in record.get("phases", [])}
    positive, cross = phases.get("positive"), phases.get("second_route")
    gaps = []
    if not (cross and cross.get("ok")):
        text = (NOT_ESTABLISHED + " - the run record holds no second route that "
                "ran. Run the fragment with `validate --cross`")
        gaps.append("no second route was run")
    elif not (positive and positive.get("ok") and positive.get("provides")):
        text = (NOT_ESTABLISHED + " - the fragment's own answer is not in the "
                "same run, so there is nothing to set the route beside")
        gaps.append("the positive case did not run")
    else:
        provides = positive["provides"]
        text = ("%s. In the same run the fragment returned %s"
                % (describe_phase(cross), ", ".join(
                    "%s %s" % (k, provides[k]) for k in sorted(provides))))
    return {
        "fragment": frag.slug,
        "capability": frag.data.get("capability"),
        "drafted_by": "Fragment Validation Agent (HERON-FRG-VAL-001)",
        "drafted_from": record.get("run_record") or "(unnamed run record)",
        "confirmed": False,
        "gaps": gaps,
        "proof-draft": {
            "date": record.get("date") or "",
            "by": "",
            "model": record.get("model") or NOT_ESTABLISHED,
            "second_route": text,
            "fingerprint": frag.fingerprint(),
        },
    }


def _child_indent(lines, start, end):
    """The indent of the keys directly inside the block at lines[start:end]."""
    for index in range(start + 1, end):
        line = lines[index]
        if line.strip() and not line.lstrip().startswith("#"):
            return line[:len(line) - len(line.lstrip())]
    return "  "


def splice_proof_key(text, key, value):
    """`text` with ONE key of its proof block replaced, or added at its end.

    The text-edit rule above, one level down: every other line of the proof -
    the signed record itself - stays exactly where it was. `releases` is a
    list D-131 writes and only D-131 reads, so it is rendered whole; the
    caller reads the file back and refuses anything that moved besides `key`.
    """
    bounds = _block_bounds(text, "proof")
    if not bounds:
        raise ValueError("this fragment.yaml has no proof block to add %s to. "
                         "Nothing was written." % key)
    start, end = bounds
    lines = text.split("\n")
    indent = _child_indent(lines, start, end)
    rendered = [indent + line for line in yaml.safe_dump(
        {key: value}, allow_unicode=True, sort_keys=False,
        default_flow_style=False, width=88).rstrip("\n").split("\n")]

    first = None
    for index in range(start + 1, end):
        if lines[index].startswith(indent + key + ":"):
            first = index
            break

    if first is None:
        last = end
        while last > start + 1 and not lines[last - 1].strip():
            last -= 1
        return "\n".join(lines[:last] + rendered + lines[last:])

    stop = first + 1
    while stop < end:
        line = lines[stop]
        if not line.strip():
            stop += 1
            continue
        lead = len(line) - len(line.lstrip())
        if lead > len(indent) or (lead == len(indent) and line.lstrip().startswith("- ")):
            stop += 1
            continue
        break
    while stop > first + 1 and not lines[stop - 1].strip():
        stop -= 1
    return "\n".join(lines[:first] + rendered + lines[stop:])


def _write_into_proof(frag, key, value):
    """Write one key of a fragment's proof, and prove nothing else moved.

    Returns (code, problem) - (0, None) when written. On any difference but
    `key` - the status, another field of the proof, another top-level key -
    the file is put back as it was and nothing is saved.
    """
    path = os.path.join(frag.folder, "fragment.yaml")
    with io.open(path, "r", encoding="utf-8") as fh:
        before = fh.read()
    base = HF.load(frag.folder)

    try:
        after_text = splice_proof_key(before, key, value)
    except ValueError as exc:
        return 2, str(exc)
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(after_text)

    def without(mapping, name):
        return dict((k, v) for k, v in (mapping or {}).items() if k != name)

    moved = []
    try:
        after = HF.load(frag.folder)
    except ValueError as exc:
        after = None
        moved.append("the file no longer reads: %s" % exc)
    if after is not None:
        if after.status != base.status:
            moved.append("the status moved from %s to %s" % (base.status, after.status))
        if without(after.data, "proof") != without(base.data, "proof"):
            moved.append("a key outside the proof changed")
        if without(after.proof, key) != without(base.proof, key):
            moved.append("another field of the proof changed")
        if (after.proof or {}).get(key) != value:
            moved.append("%s did not read back as written" % key)
    if moved:
        with io.open(path, "w", encoding="utf-8") as fh:
            fh.write(before)
        return 1, ("%s while writing proof.%s. Nothing was saved - this writes "
                   "one key and must leave every other one alone"
                   % ("; ".join(moved), key))
    return 0, None


def _proven_fragment(slug, by, library, what):
    """(frag, None) for a PROVEN fragment whose proof still stands, or (None, (code, why))."""
    if not by or not by.strip():
        return None, (2, "accept needs a person's name: --by \"Your Name\"")
    library = library if library is not None else load_library()
    match = [f for f in library if f.slug == slug]
    if not match:
        return None, (2, "no fragment called '%s'" % slug)
    frag = match[0]
    if frag.status not in HF.NEEDS_PROOF or not isinstance(frag.proof, dict):
        return None, (2, (
            "'%s' is %s. %s is recorded on a fragment already PROVEN - its first "
            "proof is `accept %s --by` without it (D-131)"
            % (slug, frag.status, what, slug)))
    if frag.proof_is_stale():
        return None, (2, (
            "'%s' has a proof the code has moved under. %s on top of a proof "
            "that no longer describes what runs would be evidence about two "
            "different codes - prove it again first (D-30, D-131)" % (slug, what)))
    return frag, None


def accept_release(slug, release, by, library=None):
    """A PERSON confirms a run on one more Revit release. D-131.

    Writes one entry of `proof.releases` and nothing else. Refused for a
    release already recorded by evidence that still stands - the proof's own
    release, or an entry whose fingerprint still matches - so a good record is
    never overwritten. An entry gone STALE is replaced, which is the same rule
    `accept` keeps for a stale proof.
    """
    release = str(release or "").strip()
    if not RELEASE_SHAPE.match(release):
        return 2, "--release takes a year, e.g. --release 2025"
    frag, refused = _proven_fragment(slug, by, library,
                                     "A proof on another release")
    if refused:
        return refused

    if release not in frag.supported:
        return 2, ("'%s' does not claim Revit %s - its `revit:` is %s"
                   % (slug, release, ", ".join(frag.supported)))
    fresh, _stale = MX.proven_releases(frag)
    if release in fresh:
        return 2, ("Revit %s is already recorded on '%s', by %s, and it still "
                   "matches the code. Signing it again would overwrite a good "
                   "record with the same thing" % (
                       release, slug, "its proof" if fresh[release] == "proof"
                       else "an entry of proof.releases"))

    key = proof_key(slug, release=release)
    draft = read_draft(key)
    if draft is None:
        return 2, ("no draft for '%s' on Revit %s in brain/proof-drafts/ - run it "
                   "there, then `draft %s --release %s --from %s`"
                   % (slug, release, slug, release,
                      HF.repo_relative(run_record_path(key), ROOT)))
    entry = dict(draft.get("proof-draft") or {})
    if str(entry.get("release") or "") != release:
        return 2, ("the draft is for Revit %s, not %s"
                   % (entry.get("release") or "no release", release))
    missing = [k for k in HF.RELEASE_REQUIRED if k != "by" and not entry.get(k)]
    if missing:
        return 2, ("the draft is not complete - no %s. A draft with a gap in it "
                   "is a finding to act on, not a proof to accept"
                   % ", ".join(missing))
    for phase_name in ("positive_case", "negative_case"):
        if NOT_ESTABLISHED in str(entry.get(phase_name) or ""):
            return 2, ("the %s says NOT ESTABLISHED. A phase that did not run is "
                       "an absence, and an absence cannot be signed."
                       % phase_name.replace("_", " "))
    problem = _stale_draft_refusal(frag, entry)
    if problem:
        return 2, problem

    record_path = run_record_path(key)
    try:
        with io.open(record_path, "r", encoding="utf-8") as fh:
            named, why = record_release(json.load(fh))
    except (ValueError, OSError):
        named, why = None, None
    if why or (named and named != release):
        return 2, ("%s, so it is no evidence about Revit %s. Run it on that "
                   "release - `--session <pid>` names which Revit"
                   % (why or "the run record says it ran on Revit %s" % named,
                      release))
    problem = _record_refusal(frag, record_path)
    if problem:
        return 2, problem

    entry["by"] = by.strip()
    ordered = dict((k, entry[k]) for k in HF.RELEASE_REQUIRED)
    rows = [row for row in MX.release_entries(frag)
            if str(row.get("release")) != release]
    code, problem = _write_into_proof(frag, "releases", rows + [ordered])
    if code:
        return code, problem

    os.remove(draft_path(key))
    return 0, ("Revit %s recorded on %s by %s, as an entry of proof.releases. "
               "The proof itself and the status (%s) are as they were"
               % (release, slug, by.strip(), frag.status))


def accept_second_route(slug, by, library=None):
    """A PERSON confirms a second route on a fragment already PROVEN. D-131.

    Writes the one `second_route` line of the proof, from a recorded run,
    and only where the proof says NOT ESTABLISHED: a route already written is
    never replaced. Nothing here judges whether the route AGREES - the draft
    sets the two answers side by side, and that is what the name signs.
    """
    frag, refused = _proven_fragment(slug, by, library, "A second route")
    if refused:
        return refused

    written = str(frag.proof.get("second_route") or "").strip()
    if written and NOT_ESTABLISHED not in written:
        return 2, ("'%s' already has a second route: %s ... Writing another would "
                   "overwrite it" % (slug, written[:80]))

    key = proof_key(slug, second_route=True)
    draft = read_draft(key)
    if draft is None:
        return 2, ("no second-route draft for '%s' in brain/proof-drafts/ - run it "
                   "with `validate --cross`, then `draft %s --second-route --from %s`"
                   % (slug, slug, HF.repo_relative(run_record_path(key), ROOT)))
    proof = dict(draft.get("proof-draft") or {})
    text = str(proof.get("second_route") or "").strip()
    if not text or NOT_ESTABLISHED in text:
        return 2, ("the draft's second route says %s. A route that did not run "
                   "cannot be signed" % (text or "nothing"))
    problem = _stale_draft_refusal(frag, proof)
    if problem:
        return 2, problem

    record_path = run_record_path(key)
    try:
        with io.open(record_path, "r", encoding="utf-8") as fh:
            record = json.load(fh)
    except (ValueError, OSError) as exc:
        return 2, ("no run record to re-check at %s (%s). The draft is a sentence; "
                   "the record is the evidence" % (record_path, exc))
    phases = {p.get("phase"): p for p in record.get("phases", [])}
    cross, positive = phases.get("second_route"), phases.get("positive")
    if not (cross and cross.get("ok")):
        return 2, "the run record holds no second route that ran"
    if not (positive and positive.get("ok") and positive.get("provides")):
        return 2, ("the run record holds no answer of the fragment's to set the "
                   "route beside")
    kept_wrong = kept_problem(frag, positive)
    if kept_wrong:
        return 2, kept_wrong

    value = ("%s. Run %s on %s. Signed by %s through `accept --second-route` "
             "(D-131)" % (text, proof.get("date") or record.get("date") or "?",
                          proof.get("model") or record.get("model") or "?",
                          by.strip()))
    code, problem = _write_into_proof(frag, "second_route", value)
    if code:
        return code, problem

    os.remove(draft_path(key))
    return 0, ("second route recorded on %s by %s. Every other line of the proof "
               "and the status (%s) are as they were" % (slug, by.strip(), frag.status))


def _evidence_refusal(slug, frag, proof):
    """Why this draft must not be signed, or None.

    RE-READ FROM THE RUN RECORD, not from the draft's prose. The draft's
    `positive_case` is a sentence for a person; the record beside it holds the
    numbers the fragment actually returned, and those are what can be judged.

    A RECORD THAT CANNOT BE FOUND IS A REFUSAL, not a pass. The alternative is
    signing on the strength of a sentence nothing can check, which is the shape
    of every defect this file exists to catch.
    """
    for phase_name in ("positive_case", "negative_case"):
        text = str(proof.get(phase_name) or "")
        if "NOT ESTABLISHED" in text:
            return ("the %s says NOT ESTABLISHED. A phase that did not run is "
                    "an absence, and an absence cannot be signed."
                    % phase_name.replace("_", " "))

    # ALREADY PROVEN, AND THE PROOF STILL DESCRIBES THE CURRENT CODE. Signing
    # again overwrites a good proof with an identical one and buys nothing -
    # and it is ROW 44'S COMPLAINT ARRIVING FROM THE OTHER SIDE. `accept` does
    # not delete the draft it consumed, so a proved fragment keeps a draft file
    # sitting in `brain/proof-drafts/` for ever, and anything that offers up
    # "every draft on disk" offers it again.
    #
    # Measured 2026-09-13, by walking straight into it: an audit of all 72
    # drafts reported 16 ready to sign, and NINE of those were already PROVEN -
    # `audit-mep-openings`, `disallow-join`, `report-connectors` and six more.
    # They were handed to the owner as work waiting for him. He had already
    # done every one. The message he got back the last time he signed one of
    # these read "Status is still DRAFT" while the file said PROVEN, so nothing
    # on screen told him either.
    #
    # STALE IS THE EXCEPTION AND IT IS THE WHOLE POINT OF THE RULE. A PROVEN
    # fragment whose code has moved under it has a proof that no longer
    # describes what runs, and re-proving it is exactly what D-30 asks for - so
    # that case falls through to the fingerprint check below and is allowed.
    if frag.status in ("PROVEN", "PRODUCTION") and not frag.proof_is_stale():
        return ("'%s' is already %s, and its recorded proof still matches the "
                "current code. Signing it again would overwrite a good proof "
                "with the same thing and change nothing. The draft left in "
                "brain/proof-drafts/ is a leftover from when it WAS proved, "
                "not work waiting to be done. A proof on ANOTHER Revit release "
                "is `accept --release`, and a second route `accept "
                "--second-route` - neither touches this proof (D-131)."
                % (slug, frag.status))

    stale = _stale_draft_refusal(frag, proof)
    if stale:
        return stale

    return _record_refusal(frag, os.path.join(DRAFTS_DIR, "runs", slug + ".json"))


def _stale_draft_refusal(frag, proof):
    """Why a draft taken against other code cannot be signed, or None."""

    # A DRAFT TAKEN AGAINST CODE THAT HAS SINCE CHANGED CANNOT BE USED, AND
    # SIGNING IT SPENDS THE ONE THING ONLY A PERSON CAN GIVE. `accept` writes
    # the draft's fingerprint through unchanged, so the proof lands already
    # stale and `can_promote` refuses it for ever - the signature is real, the
    # fragment stays DRAFT, and the next round offers it up again. That loop is
    # row 44, found by the owner rather than by a tool:
    #
    #   "becose i signed item i have to do again i sow lkike that for
    #    exambple set view crop i singe multiple time"
    #
    # Measured 2026-09-13: `check-ceiling-coordination` held a draft from before
    # the D-71 units work, fingerprint c905e58bb9224d02 against a current
    # 4db71c18b96ed9bb, and every other check here passed it. Re-running is
    # cheap and a wasted signature is not, so the refusal belongs before the
    # name is typed rather than after.
    recorded = (proof.get("fingerprint") or "").strip()
    current = frag.fingerprint()
    if recorded and current and recorded != current:
        return ("the draft was taken against different code - it records "
                "fingerprint %s and the fragment is now %s. Signing it would "
                "write a proof that is stale the moment it lands, so it could "
                "never be promoted and the signature would be spent for "
                "nothing. Re-run the fragment and draft it again."
                % (recorded, current))
    return None


def _record_refusal(frag, record_path):
    """Why the run record at `record_path` is no evidence, or None.

    The half of `_evidence_refusal` that reads the RECORD rather than the
    draft - split out so a release entry (D-131) is judged by exactly the
    questions a proof is, against its own record.
    """
    if not os.path.isfile(record_path):
        return ("no run record at %s, so the evidence cannot be re-checked. "
                "The draft's wording is a sentence for a person; the record is "
                "what a machine can judge, and without it nothing here is "
                "verifiable. Re-run the fragment." % record_path)

    try:
        with io.open(record_path, "r", encoding="utf-8") as fh:
            record = json.load(fh)
    except (ValueError, OSError) as exc:
        return "the run record at %s cannot be read: %s" % (record_path, exc)

    phases = {p.get("phase"): p for p in record.get("phases", [])}

    positive = phases.get("positive")
    if positive is None:
        return "the run record has no positive phase in it"
    kept_wrong = kept_problem(frag, positive)
    if kept_wrong:
        return kept_wrong
    reason, detail = positive_worked(positive, frag)
    flipped = _flag_flipped(positive, phases.get("negative"), frag)
    if reason and flipped:
        # A FLAG IS THE EVIDENCE WHEN IT FLIPS - see _flag_flipped. Checked
        # before the UNREADABLE rescue below because a boolean reaches this
        # function as EMPTY rather than as unreadable: `_as_count` reads it as
        # zero on purpose, so the refusal it produces is POSITIVE EMPTY and no
        # arrangement could have answered it.
        reason = None
    if reason == "POSITIVE UNREADABLE" and _result_appeared(positive,
                                                            phases.get("negative"),
                                                            frag):
        # A FRAGMENT THAT MAKES ONE THING RETURNS THE THING, NOT A COUNT, AND
        # `positive_worked` CAN ONLY COUNT. It sees `created` rendered as
        # `MechanicalSystemType` - a bare CamelCase word - and `_is_helper_object`
        # cannot tell that from `OverrideGraphicSettings`, so a real result is
        # discarded as a working value and the positive reads UNREADABLE.
        #
        # Measured 2026-09-13 on `create-mep-system-type`, which duplicates a
        # duct system type. Positive: `created MechanicalSystemType`,
        # `classification SupplyAir`. Negative, given a name the model has not
        # got: `created (null)`, `classification ""`. The evidence is plainly
        # there and is not a number, and the same heuristic reads the NEGATIVE
        # correctly - `(null)` and `""` count as zero - so only the positive leg
        # was ever mis-read.
        #
        # SO ASK THE QUESTION D-30 ACTUALLY ASKS, WHICH IS A COMPARISON. This is
        # stronger than counting one leg, not weaker: a fragment succeeding while
        # doing nothing returns the SAME value in both phases and cannot pass it,
        # and a genuine working object - the `OverrideGraphicSettings` the rule
        # above exists for - is present in both legs and cannot pass it either.
        # Only a declared result that is THERE in the positive and GONE in the
        # negative gets through.
        reason = None
    if reason:
        return ("%s - %s. The positive case is the evidence it DOES something; "
                "without it the negative proves only that a fragment which does "
                "nothing also finds nothing." % (reason, detail))

    # SOME FRAGMENTS HAVE NO EMPTY CASE, AND `draft` HAS KNOWN THAT SINCE D-53
    # WHILE THIS FUNCTION DID NOT. `count-elements` describes whatever it is
    # handed; every Revit view hides something, so a visibility report always
    # reports. For those the leg D-30 wants is met by TRACKING - the answer
    # following the input across several different inputs - and `draft` writes
    # exactly that negative_case from `record["tracking"]`.
    #
    # This function then refused it twice over, and both refusals read as though
    # the fragment were at fault: with the negative phase dropped, "the run
    # record has no negative phase in it"; with it kept, "the negative case came
    # back with content". Measured 2026-09-13 on `report-filterable-parameters`,
    # which cannot come back empty - any category it is given is either
    # filterable or is reported as unfilterable, so one of its results is always
    # non-zero. A D-53 proof could be drafted and never signed, which quietly
    # closed the route the decision exists to open.
    #
    # THE BAR IS THE VARIATION, NOT THE ROW COUNT. Rows that all carry the same
    # value are what a fragment ignoring its input produces, so identical rows
    # are refused here rather than counted - the same question D-30 asks of the
    # negative case, put to the tracking set instead.
    tracking = record.get("tracking") or []
    if tracking:
        values = [str(t.get("value")) for t in tracking]
        if len(tracking) < 3:
            return ("the tracking set has only %d row(s). D-53 asks for the "
                    "answer to follow the input across SEVERAL different "
                    "inputs; two cannot show that." % len(tracking))
        if len(set(values)) < 2:
            return ("every tracking row came back %s. A fragment ignoring its "
                    "input produces exactly that, which is what tracking exists "
                    "to rule out - vary the input until the answer moves."
                    % values[0])
        return None

    negative = phases.get("negative")
    if negative is None:
        return "the run record has no negative phase in it"
    roles, names = contract_of(frag)
    if not looks_empty(negative, roles, names, string_results(frag)):
        return ("the negative case came back with content, so it is a FINDING "
                "rather than a proof. The leg that must find nothing found "
                "something - read the draft and the run record before signing.")

    return None


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

    # A FILLED FIELD IS NOT EVIDENCE. Everything above asks whether the draft
    # has the shape of a proof; nothing asked whether it SAYS anything.
    #
    # Both of these read as complete here until 2026-09-12:
    #   duplicate-views   given 5 views, `duplicated 0` in BOTH legs
    #   copy-elements     the leg that had to find nothing made 5 copies
    #
    # 62 of the 65 drafts then waiting were one of those two shapes. Signing
    # them would have moved Heron from 198 proven to 260 on evidence that
    # demonstrates nothing - which is worse than leaving them unproven, because
    # a wrong answer wearing a signature is the one nobody re-checks.
    #
    # The question is batch-prove's, which had it right and could not reach
    # here; positive_worked now lives beside looks_empty so both callers ask it.
    problem = _evidence_refusal(slug, frag, proof)
    if problem:
        return 2, problem

    proof["by"] = by.strip()

    # D-131. A proof taken again after the code moved replaces the record of
    # the old code, as before - but the releases already recorded are not
    # this draft's to drop. Each entry keeps the fingerprint it was taken
    # against, so the matrix stops counting it until it is proved again.
    earlier = (frag.proof or {}).get("releases") if isinstance(frag.proof, dict) else None
    if earlier and "releases" not in proof:
        proof["releases"] = earlier

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
# whose implementation changed ON OR AFTER its proof date, because that one
# really is stale and re-stamping it would erase the only signal saying so. The
# same day counts as after: a proof records a date, not a time, so a change made
# hours after the signature cannot be told from one made hours before it.

def restamp(library=None, apply_changes=False):
    """Re-record fingerprints that differ only because of the platform.

    Returns (rows, refused). A row is (slug, old, new). `refused` names the
    fragments whose code moved on or after the day the proof was taken; those are
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
    """Did the code move on or after the day the proof was taken? Unknown
    counts as YES, and so does the SAME DAY.

    Asked of git, because the alternative is trusting the fingerprint - which is
    the thing under suspicion. No git, no answer, and no answer means refuse:
    the whole point of this guard is that a genuinely stale proof must not be
    re-stamped back into looking fresh.

    SAME DAY COUNTS AS AFTER, and that is not pedantry - it is the hole this
    guard actually had. A proof carries a DATE and no time, so `2026-09-09`
    cannot be ordered against a commit made at some hour of `2026-09-09`. The
    comparison was `>`, so a change made hours after its own proof read as "did
    not move".

    `set-view-section-box` went through it on 2026-09-09: the proof was taken,
    then the implementation was changed the same day by the commit that split
    `viewRefused` in two - altering the very negative case the proof recorded -
    and `restamp` was willing to stamp the new bytes with the old proof still
    attached. This library proves and fixes within a single day as a matter of
    routine, so same-day is the COMMON case here, not the edge.

    The cost is that a genuine platform-skew re-stamp done on the day of the
    last code commit is now refused too. That is the safe direction, it is the
    one this function already takes for every other unknown, and the remedy is
    the honest one: set the status back to DRAFT and re-prove.
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
    return last >= proof_date        # same day is unorderable - refuse


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
    one_of = p_draft.add_mutually_exclusive_group()
    one_of.add_argument("--release", help="a run on one more Revit release of a "
                                          "PROVEN fragment (D-131), e.g. 2025")
    one_of.add_argument("--second-route", action="store_true",
                        help="a recorded second route for a PROVEN fragment (D-131)")

    p_review = sub.add_parser("review", help="read the drafts")
    p_review.add_argument("slug", nargs="?")

    p_accept = sub.add_parser("accept", help="a PERSON confirms a draft")
    p_accept.add_argument("slug")
    p_accept.add_argument("--by", required=True, help="your name. This is the signature")
    one_of = p_accept.add_mutually_exclusive_group()
    one_of.add_argument("--release", help="write one entry of proof.releases on a "
                                          "PROVEN fragment (D-131), e.g. 2025")
    one_of.add_argument("--second-route", action="store_true",
                        help="write proof.second_route on a PROVEN fragment, from "
                             "its recorded run (D-131)")

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
        if args.release and not RELEASE_SHAPE.match(args.release):
            print("--release takes a year, e.g. --release 2025")
            return 2
        key = proof_key(args.slug, args.release, args.second_route)
        if key != args.slug and (os.path.abspath(args.record)
                                 != os.path.abspath(run_record_path(key))):
            # `accept` re-reads the evidence from where this name says it is,
            # so a record anywhere else would be drafted and then refused -
            # and the default `validate --out` is the PROOF's record.
            print("A %s draft is re-checked at accept against %s, and this record "
                  "is %s. Run it with `validate --out %s`."
                  % ("release" if args.release else "second-route",
                     HF.repo_relative(run_record_path(key), ROOT), args.record,
                     HF.repo_relative(run_record_path(key), ROOT)))
            return 2
        with io.open(args.record, "r", encoding="utf-8") as fh:
            record = json.loads(fh.read())
        if args.release:
            draft = release_draft(match[0], record, args.release)
            how = "--release %s " % args.release
        elif args.second_route:
            draft = second_route_draft(match[0], record)
            how = "--second-route "
        else:
            draft = draft_from_record(match[0], record)
            how = ""
        path = write_draft(key, draft)
        print("Drafted %s" % HF.repo_relative(path, ROOT))
        for gap in draft["gaps"]:
            print("  gap: %s" % gap)
        print("")
        print("Nothing has been promoted. Read it, then:")
        print("  python brain/heron_validate.py accept %s %s--by \"Your Name\""
              % (args.slug, how))
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
        if args.release:
            code, message = accept_release(args.slug, args.release, args.by)
        elif args.second_route:
            code, message = accept_second_route(args.slug, args.by)
        else:
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
