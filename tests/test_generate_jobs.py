#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The job-file generator, checked without Revit.

    python tests/test_generate_jobs.py

WHY THIS ONE HAS A TEST. The same reason `tests/test_batch_prove.py` does: it
CONCLUDES. It decides which fragments can be attempted, which need the write
path, and what their inputs are called - and every one of those is a decision
somebody would otherwise make by reading, which is where the six mistyped input
names of 2026-09-09 came from.

THE TEST THAT MATTERS MOST IS THE FIRST ONE. `generate-jobs.py` carries a
transcription of the types `RevitFragment.FromRequest` will accept, because the
authority is C# inside the add-in and nothing in Python can call it. A
transcription drifts. This reads the branches out of the C# and fails when the
two disagree - which turns a copy that is remembered into a copy that is checked,
and those are different things.

Everything here runs on the real library and the real C#, and none of it touches
Revit. Reading files IS the whole job.
"""

import io
import os
import re
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tools"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import yaml                                                       # noqa: E402

import heron_fragment as HF                                       # noqa: E402

# tools/generate-jobs.py is not an importable module name - the hyphen is right
# for a command and wrong for an import - so it is loaded by path, the same way
# `tests/test_batch_prove.py` loads the runner and the same way a person runs it.
try:
    import importlib.util as _util

    def _load(path, name):
        spec = _util.spec_from_file_location(name, path)
        module = _util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
except ImportError:                                          # pragma: no cover
    import imp

    def _load(path, name):
        return imp.load_source(name, path)


GJ = _load(os.path.join(ROOT, "tools", "generate-jobs.py"), "generate_jobs")
BP = _load(os.path.join(ROOT, "tools", "batch-prove.py"), "batch_prove")

REVIT_FRAGMENT = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


class FakeFragment(object):
    """Only what the generator reads: a slug, a status, a risk and a contract."""

    def __init__(self, slug, needs=None, provides=None, risk="READ",
                 status="DRAFT"):
        self.slug = slug
        self.status = status
        self.data = {"risk": risk}
        self._needs = needs or []
        self._provides = provides or []

    def needs(self):
        return self._needs

    def provides(self):
        return self._provides


# ---------------------------------------------------------------------------
# The transcription, checked against the C# it was transcribed from
# ---------------------------------------------------------------------------

def accepted_by_revit():
    """The types `FromRequest` has a branch for, read out of the add-in.

    The method ends its accepting half at a marker comment - `named refusals` -
    below which `XYZ` and `ElementId` are matched only in order to be REFUSED by
    name. Splitting there is what keeps a refusal from being read as an
    acceptance.
    """
    source = io.open(REVIT_FRAGMENT, encoding="utf-8").read()
    body = source[source.index("private static object FromRequest"):
                  source.index("private static object Shape")]
    accepting = body[:body.index("---- named refusals")]
    return set(re.findall(r'wanted\s*==\s*"([^"]+)"', accepting))


def test_receivable_agrees_with_the_add_in():
    print("What this tool thinks Revit accepts is what Revit accepts")

    theirs, ours = accepted_by_revit(), set(GJ.RECEIVABLE)

    missing = sorted(theirs - ours)
    check(not missing,
          "every type FromRequest accepts is in RECEIVABLE (missing: %s)"
          % (", ".join(missing) or "none"))

    # THE OTHER DIRECTION MATTERS MORE. A type here that the add-in does NOT
    # accept means a job emitted for a fragment that will refuse the moment it
    # reaches the model - the exact failure 3h.4 asked this tool to prevent.
    extra = sorted(ours - theirs)
    check(not extra,
          "nothing in RECEIVABLE is unknown to FromRequest (extra: %s)"
          % (", ".join(extra) or "none"))


def test_the_shapes_d54_refuses_are_refused_here():
    print("The shapes D-54 refuses are marked, not emitted")

    # The five named in FRAGMENT-ISSUES section 6, plus the collection forms
    # that reach the same refusal by a different route.
    for kind in ("XYZ", "IList<XYZ>", "IList<IList<XYZ>>",
                 "ElementId", "IList<ElementId>", "ICollection<ElementId>",
                 "IDictionary<ElementId, string>",
                 "IList<Element>",
                 "OverrideGraphicSettings",
                 "View3D", "Color", "Material", "ForgeTypeId"):
        ok, why = GJ.receivable(kind)
        check(not ok and why, "%s is refused, with a reason" % kind)

    # And the reason is the ONE Revit gives, not a restatement of the type name.
    # A reader learning that a point waits on a UNITS decision and an id on a
    # 2024 TYPE CHANGE has learned two different problems with two different
    # fixes; "unsupported" twice teaches neither.
    _, point = GJ.receivable("XYZ")
    _, ident = GJ.receivable("ElementId")
    check("millimetres" in point, "a point says which decision it waits on")
    check("2024" in ident, "an id says which change it waits on")


def test_an_element_is_a_type_and_a_list_of_them_is_still_refused():
    print("`Element` resolves to a TYPE; the instance boundary stays")

    # `Element` became receivable when OneElement landed. It resolves to an
    # element TYPE by name and refuses a particular wall or duct, because an
    # instance has no name of its own - Element.Name on one returns its TYPE's
    # name, so "Generic - 200mm" would match every wall in the model.
    ok, _ = GJ.receivable("Element")
    check(ok, "a single Element can be typed in")

    # AND THE LIST FORM DID NOT COME WITH IT. The three fragments declaring
    # `IList<Element>` as a caller value want instances, and a comma-separated
    # list of type names is not what they are asking for. Letting it through on
    # the strength of the singular would bind the wrong thing quietly.
    refused, why = GJ.receivable("IList<Element>")
    check(not refused, "a list of them is not")
    check(why and "not one of them yet" in why,
          "and says so rather than half-accepting it")

    # The hint a person reads beside the blank names the boundary, so they meet
    # it while filling the file in rather than after a run against a model.
    hint = GJ.how_to_type("Element")
    check("TYPE" in hint and "cannot be typed in" in hint,
          "the hint says a type is wanted and an instance cannot be given")


def test_the_narrowed_declarations_resolve():
    print("A contract that says which type it wants can be handed one")

    # Eight contracts were narrowed off `Element` on 2026-09-09, and the point of
    # narrowing is that the search is confined: "Generic - 200mm" is unique among
    # WALL types where it may not be among every element type in the model.
    for kind in ("WallType", "FloorType", "CeilingType", "FilledRegionType",
                 "Phase", "FilterElement", "HostObjAttributes", "MEPCurveType"):
        ok, why = GJ.receivable(kind)
        check(ok, "%s can be typed in (%s)" % (kind, why or "yes"))

    # FamilySymbol was not narrowed off anything - it came with the same
    # mechanism, and it is what set-sheet-title-block and distribute-along-run
    # were waiting on. FRAGMENT-ISSUES section 6 counted four fragments on it.
    ok, _ = GJ.receivable("FamilySymbol")
    check(ok, "and so can a family type, which nothing had to be narrowed for")

    # THE LIBRARY IS THE REAL CHECK. Whatever was narrowed, every one of those
    # needs has to be something Revit can now receive - a contract narrowed to a
    # type with no rule would be a REGRESSION dressed as precision.
    found, _ = HF.load_all()
    stranded = []
    for frag in found.values():
        for need in frag.needs():
            declared = (need.get("type") or "")
            if HF.need_source(need) != "request":
                continue
            if declared in ("WallType", "FloorType", "CeilingType",
                            "FilledRegionType", "Phase", "FilterElement",
                            "HostObjAttributes", "MEPCurveType"):
                ok, why = GJ.receivable(declared)
                if not ok:
                    stranded.append("%s %s (%s)" % (frag.slug, need.get("name"), declared))
    check(not stranded, "no fragment was narrowed onto a type Revit cannot take (%s)"
          % ("; ".join(stranded[:3]) or "none"))


def test_spaces_in_a_type_do_not_change_the_answer():
    print("A type is compared the way FromRequest compares it")

    ok, _ = GJ.receivable("IList< string >")
    check(ok, "IList< string > is the same type as IList<string>")


# ---------------------------------------------------------------------------
# The write path - Golden Rule 19
# ---------------------------------------------------------------------------

def test_the_write_threshold_comes_from_the_registry():
    print("`write: true` is read from the tool registry, never assumed")

    name, ordinal, ladder = GJ.write_threshold()
    ops = GJ.declared_operations()

    check(name == ops[GJ.WRITE_OP],
          "the threshold IS the risk run_fragment_write declares (%s)" % name)
    check(ordinal == ladder[name], "and its place on the ladder is the enum's")
    check(ladder[ops[GJ.READ_OP]] < ordinal,
          "the read executor sits below it, so a read is never sent as a write")

    # The ladder is HeronRisk's, not a second opinion about it.
    for level in ("READ", "ANALYZE", "SUGGEST", "EXECUTE", "MODIFY", "PUBLISH",
                  "ADMIN"):
        check(level in ladder, "HeronRisk declares %s" % level)
    check(ladder["MODIFY"] > ladder["EXECUTE"] > ladder["READ"],
          "and the order is the one HeronPermissions declares")


def test_a_broken_registry_stops_the_run_rather_than_guessing():
    print("A registry that cannot answer stops it, and says so")

    real = GJ.declared_operations
    try:
        GJ.declared_operations = lambda path=None: {GJ.READ_OP: "ANALYZE"}
        try:
            GJ.write_threshold()
            check(False, "a missing run_fragment_write row is refused")
        except SystemExit as exc:
            check("Golden Rule 19" in str(exc),
                  "and the refusal says why it may not guess instead")

        # The dangerous rearrangement: the write executor no longer above the
        # read one. Every `write:` line would be wrong, and silently.
        GJ.declared_operations = lambda path=None: {GJ.READ_OP: "MODIFY",
                                                    GJ.WRITE_OP: "MODIFY"}
        try:
            GJ.write_threshold()
            check(False, "a write path no longer above the read path is refused")
        except SystemExit as exc:
            check("would be a guess" in str(exc),
                  "and says nothing was generated rather than generating it")
    finally:
        GJ.declared_operations = real


def test_risk_decides_the_write_path_for_every_fragment_in_the_library():
    print("Every fragment's write path follows its own declared risk")

    name, ordinal, ladder = GJ.write_threshold()
    found, _ = HF.load_all()

    # MODIFY is at the threshold and EXECUTE is below it - `set-selection`'s own
    # comment says so in as many words: changing what is highlighted is not a
    # change to the model.
    check(ladder["MODIFY"] >= ordinal, "a MODIFY fragment needs the write path")
    check(ladder["EXECUTE"] < ordinal, "an EXECUTE fragment does not")
    check(ladder["READ"] < ordinal, "and neither does a READ one")

    unreadable = [f.slug for f in found.values()
                  if f.data.get("risk") not in ladder]
    check(not unreadable,
          "every fragment declares a risk HeronRisk knows (%s)"
          % (", ".join(sorted(unreadable)[:5]) or "all of them do"))


# ---------------------------------------------------------------------------
# Which fragments are attempted at all
# ---------------------------------------------------------------------------

def test_finished_work_is_never_emitted():
    print("A fragment that is not DRAFT is not a candidate")

    found, _ = HF.load_all()
    library = dict((f.slug, f) for f in found.values())
    picked = set(GJ.candidates(library))

    wrong = sorted(s for s in picked if library[s].status != "DRAFT")
    check(not wrong, "nothing at PROVEN or PRODUCTION is picked up (%s)"
          % (", ".join(wrong[:5]) or "none"))

    # `batch-prove` would refuse them anyway and report ALREADY. Leaning on that
    # refusal to find out is the mistake that cost a whole pass on 2026-09-09,
    # and a generator that emits them has moved the mistake rather than fixed it.
    for slug in sorted(picked)[:40]:
        verdict, _ = BP.job_refusal(
            {"fragment": slug, "setup": GJ.SETUP_CHAIN, "set": {},
             "negative-set": {"categoryName": "x"}, "negative-in": None,
             "expect": None, "cross": None}, library)
        if verdict == BP.ALREADY:
            check(False, "%s would be reported ALREADY" % slug)
            break
    else:
        check(True, "and the runner reports ALREADY for none of them")


def test_a_fragment_already_run_is_left_alone():
    print("A fragment with a run record is not offered again")

    found, _ = HF.load_all()
    library = dict((f.slug, f) for f in found.values())
    picked = set(GJ.candidates(library))

    recorded = set()
    if os.path.isdir(GJ.RUNS):
        recorded = set(os.path.splitext(f)[0] for f in os.listdir(GJ.RUNS)
                       if f.endswith(".json"))
    overlap = sorted(picked & recorded)
    check(not overlap, "nothing in brain/proof-drafts/runs/ is re-offered (%s)"
          % (", ".join(overlap[:5]) or "none"))


def test_a_fragment_with_nothing_to_vary_is_marked():
    print("A fragment with no second leg is marked, not emitted")

    supply = {"elements": "IList<Element>"}
    _, ordinal, ladder = GJ.write_threshold()

    lonely = FakeFragment("report-open-documents",
                          needs=[{"name": "doc", "type": "Document"},
                                 {"name": "app", "type": "Application"}])
    reasons = GJ.blockers(lonely, supply, ordinal, ladder)
    check(any("same run twice" in r for r in reasons),
          "both legs would be the same run, so it says so")
    check(any("TRACKING" in r for r in reasons),
          "and it points at tracking rather than leaving a dead end")


def test_two_needs_filled_from_one_value_are_marked():
    print("Two needs bound to one chain value are marked")

    supply = {"elements": "IList<Element>"}
    _, ordinal, ladder = GJ.write_threshold()

    # find-nearest-elements: the things to measure FROM and the things to
    # measure TO, and the chain leaves one selection.
    pair = FakeFragment("find-nearest-elements", needs=[
        {"name": "elements", "type": "IList<Element>"},
        {"name": "targets", "type": "IList<Element>", "binds": "elements"},
    ])
    reasons = GJ.blockers(pair, supply, ordinal, ladder)
    check(any("same set" in r for r in reasons),
          "it says both would arrive as the same set")


def test_a_need_the_chain_cannot_fill_is_marked():
    print("A fragment-sourced need the setup chain does not leave is marked")

    supply = {"elements": "IList<Element>"}
    _, ordinal, ladder = GJ.write_threshold()

    fed = FakeFragment("sum-by-group", needs=[
        {"name": "quantities", "type": "IDictionary<ElementId, double>"},
    ])
    reasons = GJ.blockers(fed, supply, ordinal, ladder)
    check(any("setup chain does not leave one" in r for r in reasons),
          "it names what the chain does not provide")


def test_a_risk_out_of_reach_is_marked():
    print("A fragment Heron will not run is marked before it costs a slot")

    supply = {"elements": "IList<Element>"}
    _, ordinal, ladder = GJ.write_threshold()

    for risk in ("PUBLISH", "ADMIN"):
        out = FakeFragment("export-model-to-nwc", risk=risk,
                           needs=[{"name": "elements", "type": "IList<Element>"}])
        reasons = GJ.blockers(out, supply, ordinal, ladder)
        check(any("out of reach" in r for r in reasons),
              "risk: %s is marked, not emitted" % risk)

    check("PUBLISH" not in GJ.CLIENT.RUNNABLE_RISKS
          and "ADMIN" not in GJ.CLIENT.RUNNABLE_RISKS,
          "and the list it reads is the client's own, not a copy")


# ---------------------------------------------------------------------------
# What comes out - the shape, and the blanks
# ---------------------------------------------------------------------------

def generated():
    """The whole file, generated from the real library."""
    found, _ = HF.load_all()
    library = dict((f.slug, f) for f in found.values())
    name, ordinal, ladder = GJ.write_threshold()
    return GJ.build(library, ordinal, name, ladder)


def test_the_output_is_the_shape_batch_prove_already_parses():
    print("What comes out is example.yaml's shape, read by the real reader")

    text, counts, jobs, blocked = generated()

    parsed = yaml.safe_load(text)
    check(isinstance(parsed, dict), "it is a mapping")
    check("model" in parsed and "defaults" in parsed and "jobs" in parsed,
          "with model, defaults and jobs - the keys example.yaml uses")

    example = yaml.safe_load(io.open(
        os.path.join(ROOT, "tools", "jobs", "example.yaml"), encoding="utf-8"))
    check(set(parsed) <= set(example),
          "and no key example.yaml has not got (%s)"
          % ", ".join(sorted(set(parsed) - set(example))))
    check(set(parsed["defaults"]) <= set(example["defaults"]),
          "the same inside defaults: (%s)"
          % ", ".join(sorted(set(parsed["defaults"]) - set(example["defaults"]))))

    if jobs:
        keys = set()
        for row in parsed["jobs"]:
            keys |= set(row)
        allowed = set(["fragment", "setup", "set", "negative-set", "write",
                       "cross", "in", "negative-in", "expect", "timeout", "note"])
        check(keys <= allowed, "and every job key is one read_jobs reads (%s)"
              % ", ".join(sorted(keys - allowed)))


def test_the_generated_file_is_runnable_once_the_blanks_are_filled():
    print("It dry-runs clean against the real library")

    text, counts, jobs, blocked = generated()
    if not jobs:                                             # pragma: no cover
        check(True, "nothing to emit today, so nothing to check")
        return

    found, _ = HF.load_all()
    library = dict((f.slug, f) for f in found.values())

    # `read_jobs` reads a PATH, so the filled-in file goes to disk the way a
    # person's would. Filling in every blank with one word is not an ARRANGEMENT
    # - a real one needs the model open, and rule 2 is what decides it - but it
    # is exactly what the dry run checks: names, statuses, the setup chain, and
    # whether a negative case is arranged at all. None of those depend on which
    # category was typed.
    handle, path = tempfile.mkstemp(suffix=".yaml")
    os.close(handle)
    try:
        with io.open(path, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text.replace(GJ.FILL_IN, "Ducts"))
        rows, problems = BP.read_jobs(path)
        check(not problems, "the job file reads without complaint (%s)"
              % "; ".join(problems))
        check(len(rows) == len(jobs),
              "and every job survives the read (%d of %d)" % (len(rows), len(jobs)))

        refused = []
        for job in rows:
            verdict, why = BP.job_refusal(job, library)
            if verdict is not None:
                refused.append("%s: %s - %s" % (job["fragment"], verdict, why))
        check(not refused, "and none is refused by the dry run (%s)"
              % ("; ".join(refused[:3]) or "none"))
    finally:
        os.unlink(path)


def test_the_category_and_the_view_are_left_blank():
    print("The two judgement values are blank, and marked")

    text, counts, jobs, blocked = generated()
    parsed = yaml.safe_load(text)

    for where in ("set", "negative-set"):
        for name in GJ.SHARED_INPUTS:
            check(parsed["defaults"][where].get(name) == GJ.FILL_IN,
                  "defaults %s %s is blank" % (where, name))

    # AND NOTHING WAS QUIETLY FILLED IN ANYWHERE. A single guessed value is what
    # produced eleven confident meaningless results in one batch.
    guessed = []
    for row in parsed["jobs"]:
        for where in ("set", "negative-set"):
            for name, value in (row.get(where) or {}).items():
                if value != GJ.FILL_IN:
                    guessed.append("%s %s %s=%r" % (row["fragment"], where,
                                                    name, value))
    check(not guessed, "no value in any job was guessed (%s)"
          % ("; ".join(guessed[:3]) or "none"))

    check(parsed["model"].startswith(GJ.FILL_IN), "and the model line is blank")


def test_the_input_names_are_the_contract_s_own():
    print("Every input name is spelled from contract.needs")

    text, counts, jobs, blocked = generated()
    parsed = yaml.safe_load(text)
    found, _ = HF.load_all()
    library = dict((f.slug, f) for f in found.values())

    wrong, covered = [], []
    for row in parsed["jobs"]:
        frag = library[row["fragment"]]
        declared = set(n.get("name") for n in frag.needs()
                       if HF.need_source(n) == "request")
        typed = set(row.get("set") or {}) - set(GJ.SHARED_INPUTS)
        for name in sorted(typed - declared):
            wrong.append("%s does not need %r" % (frag.slug, name))
        for name in sorted(declared - typed - set(GJ.SHARED_INPUTS)):
            wrong.append("%s needs %r and it was not emitted" % (frag.slug, name))
        covered += sorted(typed)

    check(not wrong, "every name matches the contract exactly (%s)"
          % ("; ".join(wrong[:4]) or "none"))
    check(covered or not parsed["jobs"],
          "and %d caller value(s) were spelled out" % len(covered))


def test_no_expect_line_is_invented():
    print("`expect:` is offered as a comment, never chosen")

    text, counts, jobs, blocked = generated()
    parsed = yaml.safe_load(text)

    invented = [row["fragment"] for row in parsed["jobs"] if row.get("expect")]
    check(not invented, "no job carries an expect: this tool chose (%s)"
          % (", ".join(invented[:3]) or "none"))
    if jobs:
        check("expect:" in text,
              "but the names to choose from are written down for a person")


def test_nothing_touches_status_or_proof():
    print("It writes no heron-status and no proof")

    text, _, _, _ = generated()
    for forbidden in ("heron-status:", "heron_status", "proof:"):
        check(forbidden not in text,
              "the generated file contains no %r" % forbidden)


def test_the_blocked_are_listed_with_a_reason_each():
    print("Every fragment not emitted says why")

    text, counts, jobs, blocked = generated()
    check(counts["seen"] == counts["jobs"] + counts["blocked"],
          "every candidate is either a job or a marked one, never dropped")
    for slug, reasons in blocked:
        if not reasons:                                      # pragma: no cover
            check(False, "%s was blocked with no reason" % slug)
            break
        if slug not in text:                                 # pragma: no cover
            check(False, "%s was blocked but not written down" % slug)
            break
    else:
        check(True, "%d marked fragment(s), each with a reason in the file"
              % counts["blocked"])


def main():
    for test in (test_receivable_agrees_with_the_add_in,
                 test_the_shapes_d54_refuses_are_refused_here,
                 test_an_element_is_a_type_and_a_list_of_them_is_still_refused,
                 test_the_narrowed_declarations_resolve,
                 test_spaces_in_a_type_do_not_change_the_answer,
                 test_the_write_threshold_comes_from_the_registry,
                 test_a_broken_registry_stops_the_run_rather_than_guessing,
                 test_risk_decides_the_write_path_for_every_fragment_in_the_library,
                 test_finished_work_is_never_emitted,
                 test_a_fragment_already_run_is_left_alone,
                 test_a_fragment_with_nothing_to_vary_is_marked,
                 test_two_needs_filled_from_one_value_are_marked,
                 test_a_need_the_chain_cannot_fill_is_marked,
                 test_a_risk_out_of_reach_is_marked,
                 test_the_output_is_the_shape_batch_prove_already_parses,
                 test_the_generated_file_is_runnable_once_the_blanks_are_filled,
                 test_the_category_and_the_view_are_left_blank,
                 test_the_input_names_are_the_contract_s_own,
                 test_no_expect_line_is_invented,
                 test_nothing_touches_status_or_proof,
                 test_the_blocked_are_listed_with_a_reason_each):
        test()
        print("")

    if FAILURES:
        print("%d failure(s):" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("The generator spells the names, reads the risk, and guesses nothing.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
