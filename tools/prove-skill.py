# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-SKL-VAL-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Is a SKILL proved? The skill-level twin of `tools/batch-prove.py`.

    python tools/prove-skill.py                       both halves, every skill
    python tools/prove-skill.py --plan-only           the disk half, in seconds
    python tools/prove-skill.py --skill count-elements
    python tools/prove-skill.py --jobs tools/jobs     write the model half out
    python tools/prove-skill.py --list

WHAT "PROVED" MEANS FOR A SKILL, AND IT IS TWO HALVES
-------------------------------------------------------
`tools/check-skill-routing.py` measures one of them and says so in its own last
line: *"Understanding is half a proof; the other half is a model (D-30)."* This
file is the sentence after that one.

  **HALF 1 - UNDERSTANDING.** Every utterance reaches a capability the skill
  declares in `needs`. Asked through `heron_brain.lookup`, the seam a host
  uses, and classified by `check-skill-routing.classify` - IMPORTED, so the two
  tools cannot disagree about what a crossing is.

  **HALF 2 - THE MODEL.** The skill's capabilities, run in order on a real
  model, do the thing the skill says, with a negative case per D-30.

**HALF 2 CANNOT BE RUN WHERE THIS FILE WAS WRITTEN** - there is no Revit in a
Linux container, and there is no way to fake one that would not be worse than
saying so. So it is EMITTED: a job file in exactly `tools/jobs/example.yaml`'s
shape, which `tools/batch-prove.py` already parses, listing the skill's own
capabilities in an order that composes. The Revit session runs it.

BETWEEN THE TWO THERE IS A THIRD THING, AND IT IS WHERE THE FINDINGS ARE
-------------------------------------------------------------------------
Before a model is worth opening, four things about a skill's PLAN are readable
on disk, and each of them blocks a proof on its own:

  **a provider at all**     a capability nothing provides is docs/18's gap
  **a PROVEN provider**     a skill cannot be proved above the fragments it
                            rests on. A DRAFT provider has never been in front
                            of a model with a negative case, so the skill's
                            model half has nothing underneath it
  **a risk that covers it** a capability whose risk OUTRANKS the skill's own
                            declared risk. See below - this is the one nothing
                            had ever asked
  **an order that runs**    every step's fragment-sourced need filled by the
                            setup chain or by an earlier step. `trace-system`
                            fails here: it declares REPORT_FINDINGS, whose
                            `findings` nothing in its own plan provides

**THE RISK CHECK IS NEW AND THE REGISTER ASKED FOR IT.**
`docs/28-agent-registry.md` gives HERON-SKL-VAL-004, the Skill Validation
Agent, the job *"Metadata complete, fragments exist and are compatible, risk
level correct, no duplicate skill"*. `brain/heron_skill.validate` checks that
`risk:` is one of the seven words. Nothing compares it against the capabilities
the skill goes on to declare. `check-skill-routing.py` decides a crossing from
the skill's DECLARED risk, so a declaration that understates the plan quietly
moves the line that tool measures against - FRAGMENT-ISSUES row 140.

THIS HALF IS READ FROM DISK AND NOT FROM THE STORE, ON PURPOSE
----------------------------------------------------------------
The same distinction `check-skill-routing.py` draws and for the same reason:
row 116 measured 7, 9 and 8 crossings from three runs of one sweep minutes
apart, and row 136 found the cause - `global.db` is one file for every worktree
on the machine and every reader is a writer. So the plan half is computed from
`brain/fragments/*/fragment.yaml` and `brain/skills/*.yaml`, and two runs of it
disagree only if somebody edited something.

`--plan-only` is that half alone. It takes seconds, where the understanding
half takes twenty-five minutes for forty-three sentences.

AND `--routing-from` READS A RECORDING THAT CAN OUTLIVE ITS SENTENCES
----------------------------------------------------------------------
Twenty-five minutes is why the understanding half is saved and read back. The
fingerprint printed beside it answers *which library was asked*. It cannot
answer *which sentences* - utterances are edited in `brain/skills/*.yaml`
without the store changing at all, so a recording stays word-perfect about
four sentences after all four have been rewritten.

`ROUTING.words_moved()` compares the phrases, which are under version control.
A skill whose words have moved gets `OUT OF DATE` and **never `UNDERSTOOD`**;
a crossing found on a sentence it STILL says is kept and still outranks
everything, because that danger is real whatever the rest of the recording is
worth; one found on a sentence nobody says any more is dropped rather than
reported. Register row 152.

WHAT IT WILL NEVER PRINT
------------------------
**PROVEN.** Not once, from here. A skill is proved when a model has answered,
and no model has. The verdict this file can reach is `UNDERSTOOD` - half 1 held
and half 2 is owed - and it says which job file owes it. `brain/proof-drafts/
README.md` puts it the way that settles it: *the machine never signs.*

Exit 0 whatever it finds, the rule all three siblings set. A blocked skill is a
finding a person judges, and a tool that failed a build over one would teach
people to trim a skill's `needs` to buy a number.
"""

import argparse
import importlib.util
import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as HF                                     # noqa: E402
import heron_skill as SKILL                                     # noqa: E402


# A fragment at one of these has been in front of a model with a negative case.
# Anything else has not, whatever else is true about it.
IN_FRONT_OF_A_MODEL = ("PROVEN", "PRODUCTION")


def _sibling(name):
    """Import a tool that lives beside this one, by path.

    THE FILENAMES HAVE HYPHENS IN THEM, so `import generate-jobs` is not a
    sentence Python will read. That is the whole of this function.

    IMPORTED AND NOT COPIED, which is the argument `batch-prove.py` makes about
    `looks_empty` and this file inherits twice over. `receivable()` is D-54's
    list of shapes Revit can be handed, checked against the add-in's own source
    by `tests/test_generate_jobs.py`; `classify()` is what a crossing IS. A
    second copy of either would be a second opinion, and the two tools would
    start disagreeing about the same skill on the same day.
    """
    path = os.path.join(ROOT, "tools", name)
    spec = importlib.util.spec_from_file_location(
        "heron_tool_" + name.replace("-", "_").replace(".py", ""), path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


GJ = _sibling("generate-jobs.py")
ROUTING = _sibling("check-skill-routing.py")


# ---------------------------------------------------------------------------
# 1. The library, read from disk
# ---------------------------------------------------------------------------

def library():
    """Every fragment keyed BY SLUG, plus the capability table.

    `heron_fragment.load_all` keys by ID - FRG-MEP-001 - and every job file,
    setup chain and `--only` in this repository is written in slugs. Re-keying
    here is what `generate-jobs.py` does on its own last page, for the same
    reason.
    """
    found, unreadable = HF.load_all()
    by_slug = dict((frag.slug, frag) for frag in found.values())

    by_capability = {}
    for frag in found.values():
        capability = frag.data.get("capability")
        if capability:
            by_capability.setdefault(capability, []).append(frag)
    for rows in by_capability.values():
        rows.sort(key=lambda f: f.slug)
    return by_slug, by_capability, unreadable


def order_plan(providers, supply):
    """(steps, stuck). The skill's capabilities in an order that can run.

    Greedy and deliberately dull: a step may be placed once every one of its
    `source: fragment` needs is a name already in `supply` at the declared
    type, and placing it adds what it provides. Repeat until nothing moves.

    THE NAME LOOKED FOR IS THE BOUND ONE. `heron_fragment.need_binds` exists
    because a need is not always filled by a provide of its own name, and
    checking the need's own name instead would report `find-nearest-elements`
    as arrangeable when both its sets would arrive as one.

    `stuck` is not a failure of the ordering - it is the finding. A capability
    nothing in the skill's own plan can feed is a plan the skill cannot run,
    whatever the library holds elsewhere.
    """
    supply = dict(supply)
    steps, left = [], list(providers)
    while True:
        moved = False
        for frag in list(left):
            wanted = [n for n in frag.needs()
                      if HF.need_source(n) == "fragment"]
            if all(supply.get(HF.need_binds(n)) == n.get("type")
                   for n in wanted):
                steps.append(frag)
                left.remove(frag)
                for entry in frag.provides():
                    supply[entry.get("name")] = entry.get("type")
                moved = True
        if not moved:
            return steps, left


def unfed(frag, supply):
    """The fragment-sourced needs `supply` cannot fill, named as the contract
    writes them. This is the sentence a reader of a stuck step needs."""
    out = []
    for need in frag.needs():
        if HF.need_source(need) != "fragment":
            continue
        bound = HF.need_binds(need)
        if supply.get(bound) != need.get("type"):
            shown = need.get("name")
            if bound != shown:
                shown = "%s (bound to %r)" % (shown, bound)
            out.append("%s as %s" % (shown, need.get("type")))
    return out


# ---------------------------------------------------------------------------
# 2. The plan half - what is true before a model is opened
# ---------------------------------------------------------------------------

class Plan(object):
    """What one skill's `needs` amount to, and every reason it cannot be run."""

    def __init__(self, skill, by_capability, chain_supply, ladder):
        self.skill = skill
        self.id = skill.data.get("id") or os.path.basename(skill.path)
        self.risk = (skill.data.get("risk") or "").upper()
        self.needs = list(skill.needs())

        self.providers = {}
        self.no_provider = []
        for capability in self.needs:
            rows = by_capability.get(capability) or []
            if not rows:
                self.no_provider.append(capability)
            else:
                # ONE PROVIDER IS THE NORMAL CASE and more than one is not an
                # error - the registry exists so a capability can be served by
                # several. The first by slug is taken for the PLAN and the rest
                # are reported, because choosing among them is a judgement and
                # a job file that silently preferred one would hide it.
                self.providers[capability] = rows

        self.unproven = [(c, rows[0]) for c, rows in self.providers.items()
                         if rows[0].status not in IN_FRONT_OF_A_MODEL]

        # ROW 140. The skill's own declared risk against the risk of what it
        # declares it needs. Both are written down, so this is a comparison and
        # not a guess about phrasing.
        self.outranked = []
        mine = ladder.get(self.risk, -1)
        for capability, rows in sorted(self.providers.items()):
            theirs = ladder.get((rows[0].data.get("risk") or "").upper(), -1)
            if mine >= 0 and theirs > mine:
                self.outranked.append((capability, rows[0], theirs - mine))

        # WHAT `batch-prove.py` WILL REFUSE, WORKED OUT BEFORE THE FILE IS
        # WRITTEN. It reports `ALREADY` for a fragment at PROVEN or PRODUCTION
        # and never sends it to Revit - deliberately, and with no flag to
        # override. A skill built entirely out of fragments that are already
        # proved therefore emits a job file in which EVERY step is refused,
        # and the refusal is correct: each capability's own model half is
        # already evidenced. What is not evidenced is the COMPOSITION, and
        # that is the thing no runner in this repository can run. Row 141.
        self.already = [(c, rows[0]) for c, rows in sorted(self.providers.items())
                        if rows[0].status in IN_FRONT_OF_A_MODEL]

        chosen = [self.providers[c][0] for c in self.needs
                  if c in self.providers]
        self.steps, self.stuck = order_plan(chosen, chain_supply)
        self.stuck_because = [(f.slug, unfed(f, chain_supply))
                              for f in self.stuck]

        # D-54, through the list the add-in's own `FromRequest` branches on.
        #
        # TWO OUTCOMES, AND THEY ARE NOT THE SAME THING. A shape with no rule
        # yet - a face, a dictionary keyed by an element - blocks: nothing a
        # person can type reaches it. `Element` meaning ONE PARTICULAR element
        # does NOT block, because the refusal itself names the word that works:
        # select it in Revit and pass `selected`. `generate-jobs.py` still
        # counts that as a blocker, which is right for a tool asking "can this
        # be emitted unattended" and wrong for one asking "can this skill be
        # proved at all" - the answer there is yes, with a hand on the mouse.
        self.untypeable, self.by_hand = [], []
        for frag in self.steps:
            for need in frag.needs():
                if HF.need_source(need) != "request":
                    continue
                ok, why = GJ.receivable(need.get("type"), need.get("name"))
                if ok:
                    continue
                row = (frag.slug, need.get("name"), need.get("type"), why)
                if why == GJ.ELEMENT_INSTANCE_REASON:
                    self.by_hand.append(row)
                else:
                    self.untypeable.append(row)

    def blockers(self):
        """Every reason the model half cannot be attempted, in a person's words.

        Empty means the job file below is worth running. It does NOT mean the
        skill works - that is what running it is for.
        """
        out = []
        for capability in self.no_provider:
            out.append("`%s` has no provider at all - nothing in the library "
                       "declares it, so the skill names a capability Heron "
                       "does not have (docs/18)" % capability)
        for capability, frag in sorted(self.unproven):
            out.append("`%s` is provided by `%s`, which is %s - it has never "
                       "been in front of a model with a negative case, and a "
                       "skill cannot be proved above what it rests on"
                       % (capability, frag.slug, frag.status))
        for capability, frag, gap in self.outranked:
            out.append("`%s` is %s and this skill declares `risk: %s` - the "
                       "capability outranks the skill by %d rung(s). A skill "
                       "approved at its own declared risk would run something "
                       "above it (FRAGMENT-ISSUES row 140)"
                       % (capability, (frag.data.get("risk") or "?"),
                          self.risk, gap))
        for slug, missing in self.stuck_because:
            out.append("`%s` cannot be reached in this skill's own plan: it "
                       "needs %s, and neither the setup chain nor any earlier "
                       "capability this skill declares provides one"
                       % (slug, ", ".join(missing) or "something nothing here "
                          "provides"))
        for slug, name, kind, why in self.untypeable:
            out.append("`%s` wants `%s (%s)`, which cannot be typed in: %s"
                       % (slug, name, kind, why))
        return out


# ---------------------------------------------------------------------------
# 3. The understanding half - the seam a host actually uses
# ---------------------------------------------------------------------------

def understanding(plan, revit):
    """[(phrase, capability, risk, route, where, told)] for every utterance.

    `where` is `check-skill-routing.classify`'s word, imported rather than
    re-decided. A tool that judged a crossing differently from the sweep that
    reports crossings would be the drift, not the fix.

    `told` is `ROUTING.unsettled`'s reading of the retriever's OWN note - what
    it said about how little it found. It is a sixth field on purpose: a
    recording written before this exists is still read correctly, because
    every consumer pads and slices rather than unpacking a fixed width.
    """
    import heron_brain as brain

    declared = set(plan.needs)
    out = []
    for phrase in plan.skill.utterances():
        try:
            answer = brain.lookup(phrase, revit=revit)
        except Exception as why:                               # noqa: BLE001
            out.append((phrase, None, None, None,
                        "unresolved: %s: %s" % (type(why).__name__, why), ()))
            continue
        capability = answer.get("capability")
        if not capability:
            out.append((phrase, None, None, None, "unresolved: no capability",
                        ROUTING.unsettled(answer.get("note"))))
            continue
        risk = (answer.get("risk") or "").upper()
        out.append((phrase, capability, risk, answer.get("route") or "?",
                    ROUTING.classify(plan.risk, risk, capability, declared),
                    ROUTING.unsettled(answer.get("note"))))
    return out


def save_routing(path, revit, index, measured):
    """Write a routing measurement down, with what it was taken against.

    TWENTY-FIVE MINUTES FOR FORTY-THREE SENTENCES, and every re-run of this
    tool for any other reason paid it again. That is a real cost and it is why
    the measurement is written out - but a RECORDED measurement is a claim
    about a moment, so what it is stored with matters more than the numbers.

    The index fingerprint goes in beside it for the reason row 116 gives: the
    store is one file for every worktree on the machine and every reader is a
    writer (row 136), so two measurements are comparable only when the block
    above the numbers matches. `--routing-from` prints both back, says the file
    is a recording rather than a run, and refuses to pretend otherwise.
    """
    import datetime
    import json
    with io.open(path, "w", encoding="utf-8") as handle:
        handle.write(json.dumps({
            "taken": datetime.datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ"),
            "revit": revit,
            "index": index,
            "skills": dict((sid, [list(row) for row in rows])
                           for sid, rows in measured.items()),
        }, indent=2, sort_keys=True))


def load_routing(path):
    """(taken, revit, index, {skill: rows}) from a saved measurement."""
    import json
    with io.open(path, encoding="utf-8") as handle:
        doc = json.loads(handle.read())
    return (doc.get("taken"), doc.get("revit"), doc.get("index") or {},
            dict((sid, [tuple(row) for row in rows])
                 for sid, rows in (doc.get("skills") or {}).items()))


# ---------------------------------------------------------------------------
# 4. The model half, written out rather than run
# ---------------------------------------------------------------------------

def correct_the_hint(block, frag, plan):
    """Replace the hint on a need `generate-jobs.py` would never have emitted.

    THIS EXISTS BECAUSE EMITTING MORE THAN THE SIBLING DOES MEANS OWNING WHAT
    IT SAYS. `how_to_type` writes *an element TYPE by name - "Basic Wall:
    Generic - 200mm"* for anything declared `Element`, and its own comment
    says why that is safe there: *"Only a `...Type` need reaches this hint
    now - one meaning a particular element is refused above and never
    emitted."* This file DOES emit those - see `by_hand` - so the hint arrives
    at a reader for whom it is exactly wrong.

    And the way it would be wrong is the expensive way, measured 2026-09-10:
    a typed name handed to a need wanting one element resolves to a TYPE, the
    fragment runs on it, and all twelve came back 0 with no error at all. A
    hint that produces a clean wrong answer is worse than no hint.
    """
    names = set(name for slug, name, _k, _w in plan.by_hand
                if slug == frag.slug)
    if not names:
        return block
    out = []
    for line in block:
        stripped = line.strip()
        for name in names:
            if stripped.startswith(name + ":") and "#" in line:
                line = "%s# the ONE element selected in Revit - type `selected`" % (
                    line[:line.index("#")],)
                break
        out.append(line)
    return out


def job_file(plan, by_slug, chain_supply, threshold_ordinal,
             threshold_name, ladder, measured):
    """The skill's plan as a job file `batch-prove.py` can run, as text.

    THE SHAPE IS `tools/jobs/example.yaml`'S AND THE BLOCKS ARE GENERATED BY
    `generate-jobs.py`, not re-written here. That file's whole reason for
    existing is that six input names were mistyped in one day; writing a second
    emitter would hand those six typos back.

    What this file adds is the ORDER and the reason for it. `generate-jobs.py`
    emits one fragment per job with no relation between them, because it is
    asking which fragments are unproved. A skill is a COMPOSITION, so each step
    after the first carries the steps before it as its `setup:`, and a step
    whose need can only come from the chain carries `keep-chain: true` - the
    flag `.claude/skills/fragment-proving/SKILL.md` records `group-and-count`
    being told *"no earlier fragment in this session left a value of that
    name"* without.
    """
    lines = []
    add = lines.append

    add("# GENERATED by tools/prove-skill.py. Read it before running it.")
    add("#")
    for line in GJ.wrap(
            "THE MODEL HALF OF ONE SKILL: %s (%s), %d capability(ies) in an "
            "order that composes. A skill is proved when its words reach the "
            "capabilities it declares AND those capabilities, run in order on "
            "a real model, do what the skill says - with a negative case "
            "(D-30). The first half was measured where this file was written. "
            "The second is what this file is for."
            % (plan.id, plan.risk or "no risk declared", len(plan.steps)),
            78, "# "):
        add(line)
    add("#")
    add("# WHAT THE SKILL SAYS IT DOES:")
    for line in GJ.wrap(" ".join((plan.skill.data.get("purpose") or "").split()),
                        78, "#   "):
        add(line)
    add("#")
    add("# THE WORDS IT CLAIMS, AND WHERE EACH ONE ACTUALLY LANDED:")
    if measured is None:
        add("#   NOT MEASURED on this run - see --plan-only. Run without it")
        add("#   before trusting the order below to be the thing Ajmal asked for.")
    for phrase, capability, _risk, route, where in measured or []:
        add("#   %-9s %-40s -> %s%s"
            % (where, phrase[:40], capability or "-",
               " (%s)" % route if route else ""))
    if measured:
        add("#")
        for line in GJ.wrap(
                "A `miss` is not automatically a defect - a skill composes "
                "several capabilities and `lookup` returns ONE, so the single "
                "best answer sitting outside the plan may mean the plan is "
                "short a capability or that the sentence names a composition no "
                "fragment can win. A `crossing` is not that: it is a question "
                "answered by something that changes the model, and it is the "
                "line to read first.", 78, "# "):
            add(line)
    add("#")
    if plan.by_hand:
        add("# ONE THING HERE NEEDS A HAND ON THE MOUSE BEFORE THE BATCH RUNS:")
        for slug, name, kind, _why in plan.by_hand:
            for line in GJ.wrap(
                    "%s wants `%s (%s)` - one PARTICULAR element, not a type. "
                    "Select it in Revit and type `selected` in the blank. The "
                    "add-in refuses if none or several are selected, so the "
                    "batch has to be arranged around that one pick."
                    % (slug, name, kind), 78, "#   "):
                add(line)
        add("#")
    for line in GJ.wrap(
            "EVERY VALUE BELOW IS BLANK AND THAT IS DELIBERATE. Which category "
            "this model has, which view holds a small number of them, and what "
            "makes the answer empty are judgement - a wrong category produces a "
            "confident meaningless result, which happened eleven times in one "
            "batch on 2026-09-09. The five rules that decide them are in "
            ".claude/skills/fragment-proving/SKILL.md. Every blank is marked %s."
            % GJ.FILL_IN, 78, "# "):
        add(line)
    add("#")
    add("#     python tools/batch-prove.py <this file> --dry-run")
    add("#     python tools/batch-prove.py <this file>")
    add("#")
    if plan.already:
        for line in GJ.wrap(
                "READ THIS BEFORE RUNNING IT: %d of the %d step(s) below are "
                "fragments already at PROVEN or PRODUCTION, and batch-prove "
                "refuses those - it reports ALREADY and sends nothing to "
                "Revit. There is deliberately no flag to override that. So "
                "this file is a PLAN a person reads, and only the steps not "
                "listed here will actually run."
                % (len(plan.already), len(plan.steps)), 78, "# "):
            add(line)
        for capability, frag in plan.already:
            add("#   ALREADY  %-30s %s" % (frag.slug, frag.status))
        add("#")
        for line in GJ.wrap(
                "THE COMPOSITION IS WHAT IS STILL OWED, AND NOTHING HERE RUNS "
                "ONE. `prove` in mcp/client/heron_bridge_client.py runs several "
                "fragments in ONE process on ONE lease - the first resets the "
                "chain and the rest continue it, which IS a skill's plan - but "
                "it calls run_fragment_read only, so it cannot run a MODIFY "
                "step, and it judges nothing: no negative case, no draft. "
                "FRAGMENT-ISSUES row 141.", 78, "# "):
            add(line)
        add("#")
        add("#     python mcp/client/heron_bridge_client.py prove \\")
        add("#         %s \\" % " \\\n#         ".join(
            f.slug for f in plan.steps))
        add("#         --set categoryName=%s --set inViewOnly=%s"
            % (GJ.FILL_IN.replace(" ", "-"), GJ.FILL_IN.replace(" ", "-")))
        add("#")
    for line in GJ.wrap(
            "`write: true` is not typed here by hand - it is the fragment's "
            "own `risk:` against the risk %s carries in the operation "
            "registry, which is %s. Golden Rule 19."
            % (GJ.WRITE_OP, threshold_name), 78, "# "):
        add(line)
    add("")
    add("# %s. Recorded, never enforced." % GJ.FILL_IN)
    add("model: %s" % GJ.quote("%s - the model these values were arranged for"
                               % GJ.FILL_IN))
    add("")

    lines += GJ.defaults_block(True)

    add("jobs:")
    supply = dict(chain_supply)
    earlier = []
    for number, frag in enumerate(plan.steps, 1):
        wanted = [n for n in frag.needs() if HF.need_source(n) == "fragment"]
        # THE NEEDS ONLY AN EARLIER STEP CAN FILL. The setup chain leaves the
        # SELECTION, and `set-selection` writes Revit's own - which survives a
        # chain reset. A need for anything else can only come down the chain,
        # and the chain is thrown away unless the job asks to keep it: that is
        # `group-and-count` being told "no earlier fragment in this session
        # left a value of that name" while `read-element-parameters`, one step
        # before it in the same run, had just produced 25 of them.
        from_earlier = [n for n in wanted
                        if chain_supply.get(HF.need_binds(n)) != n.get("type")]

        block = GJ.job_block(frag, ladder.get(
            (frag.data.get("risk") or "").upper(), -1) >= threshold_ordinal,
            supply)
        block = correct_the_hint(block, frag, plan)

        out = []
        for line in block:
            out.append(line)
            if not line.startswith("  - fragment:"):
                continue
            out.append("    # STEP %d of %d in %s's plan, which is %s"
                       % (number, len(plan.steps), plan.id,
                          (frag.data.get("capability") or "?")))
            if not from_earlier:
                continue
            # ONLY HERE IS THE CHAIN LENGTHENED, and only because nothing else
            # can fill this step. Splicing earlier steps into every job would
            # be worse than useless: a setup step binds from the SAME `set:`
            # block as the job, so one whose own caller values are not in it
            # arrives unbound - and a setup that fails is reported as
            # `setup_failed`, not as the fragment.
            out.append("    setup:             # the earlier steps of this "
                       "skill, because nothing else")
            out.append("                       # leaves %s"
                       % ", ".join(sorted(HF.need_binds(n)
                                          for n in from_earlier)))
            for step in GJ.SETUP_CHAIN:
                out.append("      - %s" % step)
            for step in earlier:
                out.append("      - %s" % step)
            out.append("    keep-chain: true   # without it the chain those "
                       "steps filled is reset")
            out.append("                       # a moment before this fragment "
                       "runs (defect row 11)")
            owed = []
            for step in earlier:
                for need in by_slug[step].needs():
                    if HF.need_source(need) != "request":
                        continue
                    if need.get("name") in GJ.SHARED_INPUTS:
                        continue
                    owed.append((step, need.get("name"), need.get("type")))
            if owed:
                out.append("    # AND THE SETUP STEPS ABOVE WANT THEIR OWN "
                           "VALUES OUT OF THE SAME")
                out.append("    # `set:` BLOCK. Add these to `set:` and "
                           "`negative-set:` below, or the")
                out.append("    # setup fails and the report blames the "
                           "arrangement, not the fragment:")
                for step, name, kind in owed:
                    out.append("    #   %s (%s) for %s" % (name, kind, step))
        lines += out

        earlier.append(frag.slug)
        for entry in frag.provides():
            supply[entry.get("name")] = entry.get("type")

    return "\n".join(lines) + "\n"


# ---------------------------------------------------------------------------
# 5. Saying what was found
# ---------------------------------------------------------------------------

UNDERSTOOD = "UNDERSTOOD"
PLAN_OK = "PLAN OK"
BLOCKED = "BLOCKED"
NOT_UNDERSTOOD = "NOT UNDERSTOOD"
CROSSING = "CROSSING"
OUT_OF_DATE = "OUT OF DATE"


def verdict(plan, measured, moved=None):
    """(word, why). Never `PROVEN`, and the docstring says why at the top.

    BOTH HALVES ARE ALWAYS REPORTED, and the first version of this function
    got that wrong in the way that mattered. It returned the plan blockers and
    stopped, so `check-connectivity` printed four reasons about DRAFT providers
    and never printed that *"check the connections"* resolves to
    `MIRROR_ELEMENTS`, a MODIFY. A crossing hidden behind a bookkeeping
    blocker is the same mistake the sweep's own docstring records - a risk
    escalation filed in a list of 21 misses, where it reads exactly like a
    wrong-but-harmless answer.

    So a crossing outranks everything for the WORD as well: a skill whose
    plan is tidy and whose question is answered by a write is not in better
    shape than one resting on a DRAFT fragment.
    """
    # A RECORDING OUTLIVES THE SENTENCES IT COUNTED. `moved` is
    # ROUTING.words_moved()'s answer for this skill, and a row about a
    # sentence the skill no longer says is dropped before anything is
    # counted - it is not a finding about today's library.
    gone = list((moved or {}).get("gone") or [])
    fresh = list((moved or {}).get("fresh") or [])
    rows = [r for r in (measured or []) if r[0] not in gone]
    crossings = [r for r in rows if r[4] == "crossing"]
    other = [r for r in rows if r[4] not in ("crossing", "reach")]

    def said(row, lead):
        # PADDED AND SLICED, never unpacked to a fixed width - a recording
        # written before `told` existed has five fields and must still read.
        phrase, capability, risk, _route, where, told = (
            list(row) + [None] * 6)[:6]
        # ONE return, and it starts with the format string - the suite reads
        # this function's returns to prove no verdict word is built on the
        # spot, and a helper with a bare `return out` reads like one.
        return "%s: %r reached %s (%s), which is a %s%s" % (
            lead, phrase, capability or "nothing", risk or "?", where,
            "".join("\n      and the RETRIEVER said so itself: %s" % tag
                    for tag in (told or ())))

    why = [said(r, "ANSWERED BY A WRITE") for r in crossings]
    why += plan.blockers()
    why += [said(r, "understanding") for r in other]
    if gone or fresh:
        # SAID WHATEVER THE WORD IS, because a reader who sees BLOCKED must
        # still learn that the other half is about different sentences.
        why.append(
            "THE RECORDING IS OUT OF DATE: %d sentence(s) this skill says "
            "now were never measured%s%s"
            % (len(fresh), (" (%s)" % ", ".join(repr(one) for one in fresh))
               if fresh else "",
               ("; %d measured sentence(s) it no longer says were dropped"
                % len(gone)) if gone else ""))

    if crossings:
        return CROSSING, why
    if plan.blockers():
        return BLOCKED, why
    if measured is None:
        return PLAN_OK, ["nothing on disk blocks the model half. THE "
                         "UNDERSTANDING HALF WAS NOT MEASURED on this run - "
                         "--plan-only, or a recording that does not cover this "
                         "skill - so nothing here says its own words reach its "
                         "own capabilities, and that is the half where the "
                         "crossings are"]
    if other:
        return NOT_UNDERSTOOD, why
    if fresh or gone:
        # EVERY SENTENCE THE RECORDING HOLDS REACHES, AND IT IS THE WRONG SET.
        # UNDERSTOOD here would be the exact claim row 152 is about, made by
        # the tool that WRITES the recordings rather than the page that
        # reads them.
        return OUT_OF_DATE, why + [
            "every sentence the recording holds reaches a declared "
            "capability - and the skill's words have moved since it was "
            "taken, so that is a statement about a different set. Re-run "
            "without --routing-from"]
    return UNDERSTOOD, ["every utterance reaches a declared capability, and "
                        "every capability has a PROVEN provider the plan can "
                        "reach in order. THE MODEL HALF IS STILL OWED - D-30 "
                        "is not met by understanding"]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--revit", default="2020")
    parser.add_argument("--skill", action="append",
                        help="one skill id; repeatable. Default is all ten")
    parser.add_argument("--plan-only", action="store_true",
                        help="the disk half only - seconds, not minutes")
    parser.add_argument("--jobs", metavar="DIR",
                        help="write each skill's model half here as "
                             "<id>-model-half.yaml")
    parser.add_argument("--routing-to", metavar="FILE",
                        help="save the understanding half here - it costs "
                             "twenty-five minutes to take")
    parser.add_argument("--routing-from", metavar="FILE",
                        help="read a saved one instead of asking again. It is "
                             "a RECORDING, and the run says so")
    parser.add_argument("--list", action="store_true",
                        help="print the plans and run nothing")
    args = parser.parse_args()

    by_slug, by_capability, unreadable = library()
    if unreadable:
        print("FRAGMENTS THAT WOULD NOT LOAD (%d) - a capability they provide "
              "reads as missing below:" % len(unreadable))
        for why in unreadable:
            print("  %s" % why)
        print("")

    found, problems = SKILL.load_all()
    if problems:
        print("SKILLS THAT WOULD NOT LOAD (%d):" % len(problems))
        for why in problems:
            print("  %s" % why)
        print("")

    threshold_name, threshold_ordinal, ladder = GJ.write_threshold()
    chain_supply = GJ.chain_provides(by_slug)

    wanted = set(args.skill or [])
    plans = [Plan(skill, by_capability, chain_supply, ladder)
             for skill in sorted(found.values(),
                                 key=lambda s: s.data.get("id") or "")
             if not wanted or (skill.data.get("id") in wanted)]

    if wanted:
        for name in sorted(wanted - set(p.id for p in plans)):
            print("No skill called %r in %s"
                  % (name, os.path.relpath(SKILL.SKILLS_DIR, ROOT)))
    if not plans:
        return 0

    if args.list:
        for plan in plans:
            print("%s  (%s)" % (plan.id, plan.risk or "no risk declared"))
            print("    plan:  %s" % (" -> ".join(f.slug for f in plan.steps)
                                     or "nothing that composes"))
            if plan.stuck:
                print("    STUCK: %s" % ", ".join(f.slug for f in plan.stuck))
            for phrase in plan.skill.utterances():
                print("    says:  %s" % phrase)
        return 0

    print("WHAT IS TRUE BEFORE A MODEL IS OPENED - read from brain/fragments")
    print("and brain/skills on disk, so two runs of this half disagree only if")
    print("somebody edited something (rows 116 and 136):")
    print("")
    for plan in plans:
        print("  %-22s %-7s %d capability(ies), %d provided, %d PROVEN"
              % (plan.id, plan.risk or "?", len(plan.needs),
                 len(plan.providers),
                 len([c for c in plan.providers
                      if plan.providers[c][0].status in IN_FRONT_OF_A_MODEL])))
        print("  %-22s plan: %s" % ("", " -> ".join(f.slug for f in plan.steps)
                                    or "nothing that composes"))
        for capability, frag, gap in plan.outranked:
            print("  %-22s RISK: %s is %s, %d rung(s) above this skill's own %s"
                  % ("", capability, frag.data.get("risk"), gap, plan.risk))
        for slug, missing in plan.stuck_because:
            print("  %-22s STUCK: %s needs %s and nothing in the plan leaves one"
                  % ("", slug, ", ".join(missing)))
        for slug, name, kind, _why in plan.by_hand:
            print("  %-22s BY HAND: %s wants %s - the ONE element chosen in "
                  "Revit. Pass `selected`" % ("", slug, name))
        if plan.already:
            print("  %-22s ALREADY: batch-prove refuses %d of %d step(s) - %s "
                  "already proved" % ("", len(plan.already), len(plan.steps),
                                      ", ".join(f.slug for _c, f in
                                                plan.already)))
    print("")

    measured, moved = {}, {}
    if args.routing_from:
        taken, revit, index, measured = load_routing(args.routing_from)
        print("THE UNDERSTANDING HALF IS A RECORDING, NOT A RUN. Taken %s"
              % (taken or "at an unrecorded time"))
        print("against Revit %s and this index:" % (revit or "?"))
        for line in ROUTING.fingerprint_lines(index):
            print(line)
        for line in GJ.wrap(
                "The store is one file for every worktree on the machine and "
                "every reader is a writer (row 136), so this is only as true "
                "as that fingerprint. Re-run without --routing-from before "
                "acting on a crossing.", 74, "  "):
            print(line)
        print("")
        missing = [p.id for p in plans if p.id not in measured]
        if missing:
            print("  NOT IN THE RECORDING, so no verdict for: %s"
                  % ", ".join(missing))
            print("")
        # AND A SKILL THE RECORDING COVERS CAN STILL HAVE MOVED UNDER IT.
        # The fingerprint above cannot see this: utterances are edited in
        # brain/skills/*.yaml without the store changing at all (row 152).
        for plan in plans:
            if plan.id not in measured:
                continue
            gone, fresh = ROUTING.words_moved(measured[plan.id],
                                              plan.skill.utterances())
            moved[plan.id] = {"gone": gone, "fresh": fresh}
            if gone or fresh:
                print("  OUT OF DATE  %-22s %d sentence(s) never measured, "
                      "%d measured and no longer said"
                      % (plan.id, len(fresh), len(gone)))
        if any(moved[k]["gone"] or moved[k]["fresh"] for k in moved):
            for line in GJ.wrap(
                    "Those skills get no UNDERSTOOD from this run. A crossing "
                    "on a sentence the skill STILL says is kept and still "
                    "counts - the danger is real whatever the rest of the "
                    "recording is worth.", 74, "  "):
                print(line)
            print("")
    elif not args.plan_only:
        asked = sum(len(p.skill.utterances()) for p in plans)
        print("ASKING THE LIBRARY %d SENTENCE(S) THROUGH heron_brain.lookup -"
              % asked)
        print("the same seam a host uses. This is the slow half.")
        print("")
        index = ROUTING._index_fingerprint()
        print("THE INDEX THIS RAN AGAINST - compare it before comparing counts:")
        for line in ROUTING.fingerprint_lines(index):
            print(line)
        print("")
        for plan in plans:
            measured[plan.id] = understanding(plan, args.revit)
        if args.routing_to:
            save_routing(args.routing_to, args.revit, index, measured)
            print("Understanding half saved to %s" % args.routing_to)
            print("")

    written = {}
    if args.jobs:
        if not os.path.isdir(args.jobs):
            os.makedirs(args.jobs)
        for plan in plans:
            path = os.path.join(args.jobs, "%s-model-half.yaml" % plan.id)
            text = job_file(plan, by_slug, chain_supply,
                            threshold_ordinal, threshold_name, ladder,
                            measured.get(plan.id))
            # READ IT BACK BEFORE WRITING IT OUT, the rule generate-jobs.py
            # sets: a file that would not parse is never handed to anybody.
            import yaml
            yaml.safe_load(text)
            with io.open(path, "w", encoding="utf-8") as handle:
                handle.write(text)
            written[plan.id] = os.path.relpath(path, ROOT)

    print("BY SKILL - and the verdict is never PROVEN from here:")
    print("")
    counts = {}
    for plan in plans:
        word, why = verdict(plan, measured.get(plan.id),
                            moved.get(plan.id))
        counts[word] = counts.get(word, 0) + 1
        print("  %-22s %s" % (plan.id, word))
        for line in why:
            for row in GJ.wrap(line, 74, "      "):
                print(row)
        if plan.id in written:
            print("      MODEL HALF: %s" % written[plan.id])
        print("")

    print("  %s" % ", ".join("%s %d" % (word, counts[word])
                             for word in sorted(counts)))
    print("")
    for line in GJ.wrap(
            "NOTHING ABOVE SAYS A SKILL WORKS. UNDERSTOOD means its words "
            "reach its own capabilities and its plan composes over PROVEN "
            "fragments - which is half a proof. The other half is a model "
            "(D-30), and the machine never signs for it.", 74, "  "):
        print(line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
