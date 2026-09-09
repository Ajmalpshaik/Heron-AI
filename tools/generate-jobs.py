#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-RVT-013
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
The Revit Test Agent's arranging half - the derivable part of a job file, typed
by a machine that cannot misspell.

    python tools/generate-jobs.py                          to the screen
    python tools/generate-jobs.py --out tools/jobs/next.yaml
    python tools/generate-jobs.py --risk MODIFY --out tools/jobs/modify.yaml

Then FILL IN the category and the view, and check it before it costs a session:

    python tools/batch-prove.py tools/jobs/next.yaml --dry-run

WHY THIS EXISTS
---------------
`tools/batch-prove.py` takes a hand-written job list. Most of that list is
derivable from the fragment library, and the hand-written part is where the
mistakes are: **six input names were mistyped on 2026-09-09 alone**, `widthMm`
for `width` and `sortByFields` for `sortFieldNames` among them. A mistyped input
name is not a small error - the fragment refuses, or worse binds nothing and
reports zero, and both read exactly like a fragment that is broken.

FRAGMENT-ISSUES 3h.4 asked for this, fourth of four, and said what it must not
do in the same breath. That half is below.

WHAT IT DERIVES, AND FROM WHERE - every one from a file, none from memory
------------------------------------------------------------------------
  which fragments        `heron-status: DRAFT` and no run record in
                         `brain/proof-drafts/runs/`. Finished work is not
                         re-proved and unfinished work is not re-run blind
  `write: true`          the fragment's own `risk:`, compared against the risk
                         `run_fragment_write` carries in
                         `platform/Heron.Core/HeronOperationRegistry.cs`
  the setup chain        whether the fragment needs anything a FRAGMENT
                         provides. If it does not, it gets no setup
  THE INPUT NAMES        `contract.needs`, spelled out. **This is the point of
                         the tool.** Every `source: request` name, exactly as
                         the contract writes it, with its type beside it
  what cannot be run     a need whose shape Revit has no way to receive
                         (D-54) is MARKED rather than emitted as a job that
                         would refuse the moment it reached the model

WHAT IT WILL NOT DERIVE, DELIBERATELY
-------------------------------------
**The category and the view are left blank.** They are judgement - which
category this model has, which view holds a small number of them - and a wrong
category produces a confident meaningless result. That happened **eleven times
in one batch** on 2026-09-09. This tool removes the errors a person makes while
TYPING; it does not remove the judgement a person has to make, and pretending
otherwise would make it worse than useless.

The same goes for `expect:`, for every value, and for the negative case. What is
emitted is the NAME, correctly spelled, with a blank beside it and a note saying
what has to be decided. `.claude/skills/fragment-proving/SKILL.md` is how to
decide it.

THE CANDIDATE LIST IS A LOCAL FACT, AND SO IS THIS FILE'S OUTPUT
----------------------------------------------------------------
`brain/proof-drafts/runs/` is **gitignored**. A run record is written on the
machine that has Revit, and it never travels - so on a fresh clone every fragment
looks as though it has never been in front of a model, and on the proving machine
the list shrinks each time a batch finishes.

That is right rather than broken: the question this tool asks is *"what is left to
try HERE"*, and the answer depends on what has already been tried here. Two things
follow. **Generate it on the machine that will run it**, and **generate it fresh** -
a job file committed on Tuesday is a claim about Tuesday, and this repository has
been bitten twice by a number that was true when it was typed.

THE SHAPE IS `tools/jobs/example.yaml`'S, EXACTLY
-------------------------------------------------
Same keys, same nesting, same meaning. `batch-prove.py` already parses that file
and this one does not get to change the parser - a generator that invents its own
dialect is a second parser waiting to disagree with the first. What is emitted is
read back with `yaml.safe_load` before it is written out, so a file that would
not parse is never handed to anybody.
"""

import argparse
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_fragment as HF                                     # noqa: E402

# IMPORTED, NOT COPIED - the same argument `batch-prove.py` makes about
# `looks_empty`. `RUNNABLE_RISKS` is the client's own list of what it will send
# to Revit at all, and a second copy here would emit jobs for fragments the
# client refuses on the doorstep. The list changes when Publish and Admin become
# reachable; it should change in one place.
import heron_bridge_client as CLIENT                            # noqa: E402

try:
    import yaml
except ImportError:                                          # pragma: no cover
    sys.stderr.write("A job file is YAML, so this needs PyYAML:\n"
                     "  pip install --user pyyaml\n")
    raise

RUNS = os.path.join(ROOT, "brain", "proof-drafts", "runs")
REGISTRY = os.path.join(ROOT, "platform", "Heron.Core", "HeronOperationRegistry.cs")
PERMISSIONS = os.path.join(ROOT, "platform", "Heron.Core", "HeronPermissions.cs")


# ---------------------------------------------------------------------------
# 1. What Revit has a way to receive (D-54)
# ---------------------------------------------------------------------------
#
# `RevitFragment.FromRequest` turns one caller-supplied string into the type the
# contract declares, and it refuses anything it has no rule for. The list below
# is that method's branches, transcribed - which is a copy, and a copy is
# normally the thing this repository argues against.
#
# IT IS A COPY ON PURPOSE, AND THE ALTERNATIVE IS WORSE. The authority is C#
# inside the add-in; nothing in Python can call it, and asking Revit would mean a
# live session to answer a question about a text file. So it is transcribed, kept
# next to the file it came from by name, and CHECKED rather than trusted:
# `tests/test_generate_jobs.py` reads the branches out of `RevitFragment.cs` and
# fails when the two disagree. A copy that is checked is a different thing from a
# copy that is remembered.
#
# Types are compared with every space removed, because `FromRequest` does the
# same - `IList<string>` and `IList< string >` are one type to Revit.
RECEIVABLE = frozenset([
    # Scalars.
    "string", "String",
    "int", "Int32",
    "double", "Double",
    "bool", "Boolean",
    # Resolved inside Revit, by name, and refused when the name matches twice.
    "View", "Level", "Category", "BuiltInCategory",
    # Lists, comma separated.
    "IList<string>", "List<string>", "ICollection<string>", "IEnumerable<string>",
    "IList<int>", "List<int>",
    "IList<double>", "List<double>",
    "IList<View>", "List<View>", "ICollection<View>", "IEnumerable<View>",
    "IList<BuiltInCategory>", "List<BuiltInCategory>", "ICollection<BuiltInCategory>",
    "IList<Category>", "List<Category>", "ICollection<Category>",
])

# The two `FromRequest` refuses BY NAME, with the reason it gives. Everything
# else falls through to its catch-all, and the catch-all is what `Element`,
# `FamilySymbol`, `OverrideGraphicSettings`, `View3D`, `Color` and the rest meet.
#
# Repeating the reasons here rather than saying "not supported" is the whole
# value of the marking: a reader of the generated file learns that a point is
# waiting on a UNITS DECISION and an id on a 2024 TYPE CHANGE, which are
# different problems with different fixes, and neither is "the fragment is bad".
NAMED_REFUSALS = [
    ("XYZ",
     lambda t: t == "XYZ" or "<XYZ>" in t,
     "a point. The Revit API works in feet and this library talks millimetres, "
     "so which unit a typed number is in has to be settled before one can be "
     "accepted"),
    ("ElementId",
     lambda t: "ElementId" in t,
     "an element id. Its type changed size at Revit 2024 and the add-in builds "
     "2020 to 2027 from one source. Name the thing instead, or select it"),
]


def receivable(declared):
    """(ok, why-not). Can a caller type this type in at all?

    `why-not` is the reason Revit itself would give, not a restatement of the
    type name - see NAMED_REFUSALS.
    """
    wanted = (declared or "").replace(" ", "")
    if wanted in RECEIVABLE:
        return True, None
    for _, matches, reason in NAMED_REFUSALS:
        if matches(wanted):
            return False, reason
    return False, ("Heron can be handed a view, a level, a category, a name, a "
                   "number, or true/false - and lists of those. This is not one "
                   "of them yet")


# ---------------------------------------------------------------------------
# 2. Whether the job needs the write path - Golden Rule 19
# ---------------------------------------------------------------------------
#
# GOLDEN RULE 19 - the risk of an operation is looked up BY NAME in the tool
# registry. It is never read from the request, never inferred from what an
# element is called, and never supplied by a caller.
#
# THIS TOOL IS A CALLER, so it does not get to decide either. What it does is
# read two declarations and put them side by side:
#
#   the FRAGMENT says what risk IT carries          `risk:` in fragment.yaml
#   the REGISTRY says what risk `run_fragment_write` carries
#
# `write: true` is emitted when the first is at or above the second. Nothing is
# assumed about which level that is - "MODIFY needs the write path" is true today
# and is read out of the registry rather than typed in here, so the day
# `run_fragment_write` moves, this moves with it instead of quietly disagreeing.
#
# The ORDERING comes from the enum, for the same reason. `Modify` being above
# `Execute` is a fact declared in HeronPermissions.cs, and this file has no
# business holding a second opinion about it.

WRITE_OP = "run_fragment_write"
READ_OP = "run_fragment_read"

_ENUM = re.compile(r"^\s*(\w+)\s*=\s*(\d+)\s*,?\s*$")
_ROW = re.compile(r'\{\s*"([a-z_]+)"\s*,\s*HeronRisk\.(\w+)\s*\}')


def risk_ladder(path=PERMISSIONS):
    """The HeronRisk enum as {NAME: ordinal}, read from the C#.

    Upper-cased on the way out because a fragment writes `MODIFY` and the enum
    writes `Modify`, and one of the two has to give.
    """
    ladder, inside = {}, False
    with io.open(path, encoding="utf-8") as fh:
        for line in fh:
            if "enum HeronRisk" in line:
                inside = True
                continue
            if inside:
                if "}" in line:
                    break
                found = _ENUM.match(line)
                if found:
                    ladder[found.group(1).upper()] = int(found.group(2))
    return ladder


def declared_operations(path=REGISTRY):
    """The tool registry's own table as {operation: RISK}."""
    with io.open(path, encoding="utf-8") as fh:
        return dict((op, risk.upper()) for op, risk in _ROW.findall(fh.read()))


def write_threshold():
    """The risk at which a fragment needs the write path. Raises if it cannot.

    FAILS LOUDLY RATHER THAN GUESSING. If the registry has been rearranged so
    that the write executor is no longer above the read one, every `write:` line
    this tool emits would be wrong - and a job file that sends a write down the
    read path is the defect that cost half a morning on 2026-09-09, arriving
    silently. Refusing to generate is the cheap failure; generating a plausible
    wrong file is not.
    """
    ladder, ops = risk_ladder(), declared_operations()

    for op in (WRITE_OP, READ_OP):
        if op not in ops:
            raise SystemExit(
                "%s is not declared in %s, so this tool cannot tell which "
                "fragments need the write path. Golden Rule 19 says that risk is "
                "read from the registry, and it is not there to read."
                % (op, os.path.relpath(REGISTRY, ROOT)))
        if ops[op] not in ladder:
            raise SystemExit("the registry declares %s at HeronRisk.%s, which is "
                             "not in the enum" % (op, ops[op]))

    writing, reading = ladder[ops[WRITE_OP]], ladder[ops[READ_OP]]
    if not writing > reading:
        raise SystemExit(
            "%s is declared at %s and %s at %s, so the write path is no longer "
            "above the read path. Nothing was generated: every `write:` line "
            "this tool emits would be a guess."
            % (WRITE_OP, ops[WRITE_OP], READ_OP, ops[READ_OP]))

    return ops[WRITE_OP], writing, ladder


# ---------------------------------------------------------------------------
# 3. The setup chain
# ---------------------------------------------------------------------------
#
# One chain, and it is the one the skill teaches and both hand-written job files
# use: select a category in a view, then put the result in the Revit selection.
#
# WHY NOT SEARCH THE LIBRARY FOR A CHAIN. `heron_fragment.feeders()` will happily
# name every fragment that provides `elements`, and there are dozens. Choosing
# among them, working out what THEY need, and ordering the result is a planning
# problem whose answer would be a chain nobody has ever run - which is exactly
# the confident guess this tool exists not to make. So: the known-good chain is
# emitted, and a need it does not cover is MARKED with the fragments that could
# cover it, for a person to choose from.
#
# ONLY THE FIRST STEP RESETS THE CHAIN, and step two consumes what step one left
# (`select-by-category-name` leaves `elements`, `set-selection` needs them).
# `batch-prove` and the client both already know that; it is repeated here
# because the order of these two names is not interchangeable.
SETUP_CHAIN = ["select-by-category-name", "set-selection"]

# The caller values that chain wants. Both are left BLANK - see the module
# docstring - and both live in `defaults:` so the arrangement is typed ONCE.
# Retyping it per job is how one copy comes to name a different view than the
# rest, which is `_merge`'s reason for existing in `batch-prove.py`.
SHARED_INPUTS = ["categoryName", "inViewOnly"]


def chain_provides(library):
    """{name: type} the setup chain leaves behind, read from the chain itself."""
    supply = {}
    for step in SETUP_CHAIN:
        frag = library.get(step)
        if frag is None:
            raise SystemExit("the setup chain names '%s', which is not in the "
                             "library" % step)
        for entry in frag.provides():
            supply[entry.get("name")] = entry.get("type")
    return supply


# ---------------------------------------------------------------------------
# 4. Which fragments can be attempted, and which cannot
# ---------------------------------------------------------------------------

def candidates(library):
    """DRAFT, and never yet in front of a model.

    Two filters, and the second is the one 3h.4 asked for. `batch-prove` already
    refuses a fragment at PROVEN - reported `ALREADY`, never sent to Revit - so
    the first filter only saves the reading. The second is a judgement about
    where the value is: a fragment with a run record has been arranged once
    already, and whoever arranged it knows more about it than this file does.
    """
    out = []
    for slug in sorted(library):
        frag = library[slug]
        if frag.status != "DRAFT":
            continue
        if os.path.isfile(os.path.join(RUNS, "%s.json" % slug)):
            continue
        out.append(slug)
    return out


def blockers(frag, supply, threshold_ordinal, ladder):
    """Why this fragment cannot be emitted as a job, in a person's words.

    Returns a list of reasons; empty means it can be arranged. Every one of these
    was met on a real run, and every one of them would otherwise be emitted as a
    job that fails - or worse, one that answers.
    """
    found = []
    risk = frag.data.get("risk")

    # (a) OUT OF REACH ENTIRELY. The client refuses these before anything is
    #     sent, so a job for one costs a batch slot and returns nothing. Read
    #     from the client's own list rather than a copy of it.
    if risk is None:
        found.append("it does not say what `risk:` it carries, and a fragment "
                     "whose danger nobody can establish is not run")
    elif risk not in CLIENT.RUNNABLE_RISKS:
        found.append("`risk: %s` is above what Heron runs today - "
                     "HeronPermissions puts Publish and Admin out of reach for "
                     "Phase 0 and Phase 1, so nothing would be sent to Revit"
                     % risk)
    elif risk not in ladder:
        found.append("`risk: %s` is not a level HeronRisk declares" % risk)

    # (b) A SHAPE REVIT CANNOT BE HANDED. D-54: values cross as text and become
    #     their declared type inside Revit, and the types with no rule yet are
    #     refused by name. 85 of the 120 unproven fragments stop here.
    for need in frag.needs():
        if HF.need_source(need) != "request":
            continue
        ok, why = receivable(need.get("type"))
        if not ok:
            found.append("`%s (%s)` cannot be typed in: %s"
                         % (need.get("name"), need.get("type"), why))

    # (c) SOMETHING THE SETUP CHAIN DOES NOT LEAVE BEHIND. The name looked for is
    #     the BOUND one, which is not always the need's own - a fragment may want
    #     `targets` filled by a provide called `elements`.
    for need in frag.needs():
        if HF.need_source(need) != "fragment":
            continue
        bound = HF.need_binds(need)
        if supply.get(bound) != need.get("type"):
            found.append("`%s` wants %r as %s from another fragment, and the "
                         "setup chain does not leave one"
                         % (need.get("name"), bound, need.get("type")))

    # (d) TWO NEEDS FILLED FROM ONE VALUE. `find-nearest-elements` wants the
    #     things to measure FROM and the things to measure TO, and both are
    #     `elements`. The chain leaves one selection, so both would arrive as the
    #     same set - which RUNS, and answers, and the answer means nothing. That
    #     is worse than a refusal, and it is the shape 3h.1 is about.
    bound = [HF.need_binds(n) for n in frag.needs()
             if HF.need_source(n) == "fragment"]
    for name in sorted(set(b for b in bound if bound.count(b) > 1)):
        found.append("two needs are both filled by %r, and the setup chain "
                     "leaves one. Both would arrive as the same set" % name)

    # (e) NOTHING TO VARY, SO NO SECOND LEG. D-30 needs an arrangement where the
    #     answer must be nothing, and a fragment taking only `doc` has no such
    #     arrangement: the two legs would be the same run twice. The skill calls
    #     these out by name and sends them to TRACKING (D-53) with `validate`,
    #     one at a time - which is not what `batch-prove` does.
    if not [n for n in frag.needs() if HF.need_source(n) in ("request", "fragment")]:
        found.append("it takes nothing but the model itself, so both legs would "
                     "be the same run twice. D-30's second leg is met by "
                     "TRACKING here (D-53) - prove it with `validate`, "
                     "one at a time")

    return found


def request_needs(frag):
    """The caller's half: (name, type) for every `source: request` need.

    THE ONE THING THIS TOOL IS REALLY FOR. Read out of `contract.needs` and
    written down verbatim, because `widthMm` for `width` is a five-second typo
    that costs a Revit session and reads like a broken fragment.
    """
    return [(n.get("name"), n.get("type")) for n in frag.needs()
            if HF.need_source(n) == "request"]


def how_to_type(declared):
    """What a person needs to know to fill this one in, beyond its type.

    Derived from `FromRequest`'s own branches, and only where the type alone
    would mislead:

      A LIST arrives as ONE string split on commas. `Parts()` does the splitting
      inside Revit, so `IList<string>` is not "several lines of YAML" - it is
      "Mark, Comments" on one, and a list typed as a single value binds one item
      and looks like a fragment ignoring the rest.

      A VIEW or a CATEGORY is resolved BY NAME inside Revit, and a name matching
      twice is refused rather than chosen from. These are also the values this
      tool will not guess at all: which view holds a small number of the thing
      is the judgement the whole file is arranged around.
    """
    wanted = (declared or "").replace(" ", "")
    hints = []
    if "<" in wanted:
        hints.append("comma separated")
    if re.search(r"\b(View|Category|BuiltInCategory|Level)\b", wanted):
        hints.append("resolved BY NAME in Revit; a name matching twice is refused")
    return ", ".join(hints)


def results_of(frag):
    """The provided names that are not declared bookkeeping.

    Offered as a COMMENT, never as an `expect:` line. Choosing which of them is
    the real result is exactly the knowledge `expect:` exists to carry, and the
    fragment's own contract is what would have to be fixed if the patterns get it
    wrong - a job file cannot say so on the fragment's behalf.
    """
    return [p.get("name") for p in frag.provides()
            if isinstance(p, dict) and p.get("name")
            and HF.provide_role(p) != "accounting"]


# ---------------------------------------------------------------------------
# 5. Writing it out
# ---------------------------------------------------------------------------
#
# Written as text rather than dumped, because the comments ARE the file. A job
# file is read by a person before it costs a Revit session, and `yaml.dump`
# produces something correct that says nothing about why - which is the half
# `tools/jobs/modify-never-run.yaml` gets right and no dumper can.

FILL_IN = "FILL IN"


def quote(value):
    """A scalar YAML will read back as the string it was given."""
    return '"%s"' % str(value).replace("\\", "\\\\").replace('"', '\\"')


def wrap(text, width, indent):
    """`text` as comment lines, none longer than `width`."""
    lines, current = [], indent
    for word in text.split():
        if current != indent and len(current) + 1 + len(word) > width:
            lines.append(current)
            current = indent
        current += ("" if current == indent else " ") + word
    if current != indent:
        lines.append(current)
    return lines


def header(counts, threshold):
    """The top of the file: what was derived, and what is still owed."""
    out = []
    add = out.append
    add("# GENERATED by tools/generate-jobs.py. Read it before running it.")
    add("#")
    for line in wrap(
            "%d fragment(s) are still DRAFT with no run record in "
            "brain/proof-drafts/runs/ - they have never been in front of a "
            "model. %d of them can be arranged as written and are below. %d "
            "cannot, and are listed at the end with the reason, because a "
            "fragment that would refuse the moment it reached Revit costs a "
            "batch slot and teaches nothing."
            % (counts["seen"], counts["jobs"], counts["blocked"]), 78, "# "):
        add(line)
    add("#")
    for line in wrap(
            "THE CATEGORY AND THE VIEW ARE BLANK ON PURPOSE, and so is every "
            "other value. This tool typed the NAMES, which is where six "
            "mistakes came from on 2026-09-09; the values are judgement and are "
            "yours. A wrong category produces a confident meaningless result - "
            "it happened eleven times in one batch. Every blank is marked "
            "%s." % FILL_IN, 78, "# "):
        add(line)
    add("#")
    add("#     python tools/batch-prove.py <this file> --dry-run")
    add("#     python tools/batch-prove.py <this file>")
    add("#")
    for line in wrap(
            "`write: true` is not typed here by hand. It is the fragment's own "
            "`risk:` compared against the risk %s carries in "
            "platform/Heron.Core/HeronOperationRegistry.cs, which is %s - "
            "Golden Rule 19, where risk is looked up by name and never supplied "
            "by a caller."
            % (WRITE_OP, threshold), 78, "# "):
        add(line)
    add("#")
    for line in wrap(
            "The five rules that decide the values are in "
            ".claude/skills/fragment-proving/SKILL.md, and every one of them was "
            "learned by getting it wrong in front of a model.", 78, "# "):
        add(line)
    add("")
    add("# %s. Recorded, never enforced - the run record carries what Revit "
        "actually" % FILL_IN)
    add("# said the model was. This line is so a reader knows what these values "
        "were")
    add("# chosen for.")
    add("model: %s" % quote("%s - the model these values were arranged for" % FILL_IN))
    add("")
    return out


def defaults_block(needs_chain):
    """`defaults:` - the arrangement, typed once.

    `set:` and `negative-set:` merge key by key over these in `batch-prove`, so a
    job overrides only what differs. That is why the selection is here and not
    repeated into every job.
    """
    out = []
    add = out.append
    add("defaults:")
    if needs_chain:
        add("  # Re-run before EACH phase: a rolled-back write clears the Revit")
        add("  # selection, so the arrangement is re-made rather than made once.")
        add("  # Only the first step resets the chain - step two consumes what step")
        add("  # one left. The order of these two is not interchangeable.")
        add("  setup:")
        for step in SETUP_CHAIN:
            add("    - %s" % step)
        add("")
    add("  set:")
    add("    # %s. Which category this model has, and a view holding a SMALL" % FILL_IN)
    add("    # number of them - a heavy write on a big selection is not a stronger")
    add("    # test, it is a slower one, and a timeout says nothing about the")
    add("    # fragment. Rule 1.")
    for name in SHARED_INPUTS:
        add("    %s: %s" % (name, quote(FILL_IN)))
    add("")
    add("  negative-set:")
    add("    # %s. A DIFFERENT selection, not a cleared one - clearing it leaves"
        % FILL_IN)
    add("    # `elements` unbound and the executor refuses, which proves nothing.")
    add("    # It must be a category that IS visible in the view named, and one")
    add("    # the model genuinely has nothing of for this fragment. Rules 3 and 5.")
    for name in SHARED_INPUTS:
        add("    %s: %s" % (name, quote(FILL_IN)))
    add("")
    return out


def job_block(frag, writes, supply):
    """One job, with everything derivable filled in and everything else marked."""
    out = []
    add = out.append
    slug = frag.slug

    add("  - fragment: %s" % slug)

    # The setup chain, per job, because a fragment needing nothing from another
    # fragment gets none - running the chain for it would select elements it
    # never asked for and put a stale selection where a reader expects the
    # arrangement.
    wanted = [n for n in frag.needs() if HF.need_source(n) == "fragment"]
    if not wanted:
        add("    setup: []          # it needs nothing another fragment provides")

    if writes:
        add("    write: true        # risk: %s. Without `apply` the run is wrapped"
            % frag.data.get("risk"))
        add("                       # in a TransactionGroup and rolled back (D-55)")

    caller = request_needs(frag)
    if caller:
        # THE NAMES, SPELLED FROM THE CONTRACT, in a column so a reader can see
        # at a glance which are still blank. The width is computed rather than
        # guessed, because a name longer than the column would push its own type
        # off the end of the line where nobody reads it.
        own = [(name, kind) for name, kind in caller if name not in SHARED_INPUTS]
        width = max([len(name) for name, _ in own] + [1])

        def value_line(name, kind):
            hint = how_to_type(kind)
            return ("      %-*s %s   # %s%s"
                    % (width + 1, name + ":", quote(FILL_IN), kind,
                       " - " + hint if hint else ""))

        add("    set:")
        for name, kind in caller:
            if name in SHARED_INPUTS:
                # Already in `defaults:`, and a second blank here would win the
                # merge - so somebody filling in the default would find this job
                # still empty. One name, one home.
                add("      # %s (%s) comes from the shared arrangement above"
                    % (name, kind))
                continue
            add(value_line(name, kind))
        add("    negative-set:")
        for line in wrap("%s. The negative may be driven by the VALUE rather "
                         "than the selection - a parameter no element carries, "
                         "a name the model has not got. If it answers anyway it "
                         "is falling back, and that is a finding." % FILL_IN,
                         76, "      # "):
            add(line)
        for name, kind in own:
            add(value_line(name, kind))
    elif wanted:
        add("    # No caller values. Both legs are arranged entirely by the")
        add("    # selection in `defaults:`, so those two are the whole proof.")

    results = results_of(frag)
    if results:
        for line in wrap("expect: names the one result that has to move off "
                         "zero, and it beats every pattern the runner has. Left "
                         "off because choosing it is knowledge of the fragment, "
                         "not of the library. This one declares: %s"
                         % ", ".join(results), 76, "    # "):
            add(line)
    add("")
    return out


def blocked_block(rows):
    """The fragments that were NOT emitted, and exactly why.

    Kept in the same file rather than dropped silently. A generator that quietly
    emits seven jobs out of a hundred and twenty candidates has told a reader
    almost nothing; one that says what happened to the other hundred and thirteen
    has told them where the remaining work actually is.
    """
    out = []
    add = out.append
    add("# " + "=" * 74)
    add("# NOT EMITTED - %d fragment(s) that cannot be arranged as a job today."
        % len(rows))
    add("#")
    for line in wrap("These are not failures and not defects. Most are waiting "
                     "on a way to receive one input - D-54 accepts a view, a "
                     "level, a category, a name, a number and true/false, and "
                     "lists of those. Each other shape needs its own resolution "
                     "rule, which is one decision each rather than one big one. "
                     "docs/FRAGMENT-ISSUES.md section 6 is the running count.",
                     78, "# "):
        add(line)
    add("#")
    for slug, reasons in rows:
        add("#   %s" % slug)
        for reason in reasons:
            for line in wrap(reason, 78, "#       "):
                add(line)
    return out


def build(library, threshold_ordinal, threshold_name, ladder, wanted_risk=None,
          limit=None, only=None):
    """The whole file as one string, plus the counts for the report."""
    supply = chain_provides(library)

    jobs, blocked = [], []
    for slug in candidates(library):
        frag = library[slug]
        if only and slug not in only:
            continue
        if wanted_risk and frag.data.get("risk") != wanted_risk:
            continue
        reasons = blockers(frag, supply, threshold_ordinal, ladder)
        (blocked if reasons else jobs).append((slug, reasons))

    if limit is not None:
        jobs = jobs[:limit]

    counts = {"seen": len(jobs) + len(blocked), "jobs": len(jobs),
              "blocked": len(blocked)}

    lines = header(counts, threshold_name)
    lines += defaults_block(any(
        [n for n in library[slug].needs() if HF.need_source(n) == "fragment"]
        for slug, _ in jobs))

    lines.append("jobs:")
    if not jobs:
        # A `jobs:` list that is empty is refused by `batch-prove`, and rightly.
        # Say so here rather than emitting a file whose first act is to be
        # rejected for a reason that has nothing to do with the fragments.
        lines.append("  # NOTHING TO EMIT. Every candidate is listed below with a")
        lines.append("  # reason, and `batch-prove` refuses a file with no jobs -")
        lines.append("  # so this file is a report, not a runnable job list.")
        lines.append("")
    for slug, _ in jobs:
        frag = library[slug]
        writes = ladder.get(frag.data.get("risk"), -1) >= threshold_ordinal
        lines += job_block(frag, writes, supply)

    if blocked:
        lines += blocked_block(blocked)

    return "\n".join(lines) + "\n", counts, jobs, blocked


# ---------------------------------------------------------------------------
# 6. The command line
# ---------------------------------------------------------------------------

def main(argv=None):
    parser = argparse.ArgumentParser(
        description="Generate the derivable half of a batch-prove job file. "
                    "It leaves the category and the view blank on purpose.")
    parser.add_argument("--out", help="write here instead of to the screen")
    parser.add_argument("--risk", help="only fragments at this risk, e.g. MODIFY")
    parser.add_argument("--limit", type=int, help="at most this many jobs")
    parser.add_argument("--only", action="append",
                        help="just this fragment. Repeatable")
    args = parser.parse_args(argv)

    threshold_name, threshold_ordinal, ladder = write_threshold()

    found, unreadable = HF.load_all()
    for problem in unreadable:
        sys.stderr.write("unreadable fragment: %s\n" % problem)
    library = dict((frag.slug, frag) for frag in found.values())

    if args.risk and args.risk.upper() not in ladder:
        sys.stderr.write("`--risk %s` is not a level HeronRisk declares. It is "
                         "one of: %s\n"
                         % (args.risk, ", ".join(sorted(ladder, key=ladder.get))))
        return 1

    text, counts, jobs, blocked = build(
        library, threshold_ordinal, threshold_name, ladder,
        wanted_risk=args.risk.upper() if args.risk else None,
        limit=args.limit, only=set(args.only) if args.only else None)

    # READ IT BACK BEFORE ANYBODY ELSE DOES. A generator that emits YAML it has
    # not parsed is one quoting mistake away from handing somebody a file that
    # fails for a reason that has nothing to do with the fragments in it.
    try:
        parsed = yaml.safe_load(text)
    except yaml.YAMLError as exc:
        sys.stderr.write("the generated file is not valid YAML, so it was not "
                         "written: %s\n" % str(exc).split("\n")[0])
        return 1
    if jobs and len(parsed.get("jobs") or []) != len(jobs):
        sys.stderr.write("the generated file parses to %d job(s) where %d were "
                         "built, so it was not written.\n"
                         % (len(parsed.get("jobs") or []), len(jobs)))
        return 1

    if args.out:
        with io.open(args.out, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text)
    else:
        sys.stdout.write(text)

    where = sys.stderr if not args.out else sys.stdout
    where.write("\n")
    where.write("%d DRAFT fragment(s) with no run record.\n" % counts["seen"])
    where.write("%d emitted as jobs, %d marked as unarrangeable.\n"
                % (counts["jobs"], counts["blocked"]))
    if args.out:
        where.write("\nWritten to %s. FILL IN the category and the view, then:\n"
                    % args.out)
        where.write("  python tools/batch-prove.py %s --dry-run\n" % args.out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
