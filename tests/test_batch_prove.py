#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The batch runner, checked without Revit.

    python tests/test_batch_prove.py

WHY THIS HAS A TEST WHEN generate-agent-map.py DOES NOT. Same reason
`tests/test_catalog.py` does: this one CONCLUDES. It decides whether a fragment's
positive case did any work and whether its negative came back empty, and both of
those judgements were got wrong once already - on 2026-09-09, in the throwaway
script this replaced, which passed two fragments that had done nothing at all.

The two holes are the first two tests, and they are written from the fragments
that fell through them:

  * `read-graphic-overrides` left an `OverrideGraphicSettings` OBJECT, and
    "unreadable, therefore non-zero, therefore it worked" passed a fragment
    whose only real result was 0 in both legs.
  * `check-flow-direction` returned `jointsChecked 15` and
    `bidirectionalSkipped 25`, which count work done rather than things found,
    while `bothIn` and `bothOut` were 0 in both legs.

Every case here runs on a synthetic run record. That is not a shortcut: judging
is exactly the half that needs no Revit, which is why it is the half that can
have tests at all.
"""

import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tools"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_fragment as HF                                       # noqa: E402

# tools/batch-prove.py is not an importable module name - the hyphen is right
# for a command and wrong for an import - so it is loaded by path, the same way
# a person runs it.
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


BP = _load(os.path.join(ROOT, "tools", "batch-prove.py"), "batch_prove")

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


# A CHECKOUT OF CARDS FOR THE FAKES. `job_refusal` asks the client's own
# `risk_refusal`, which reads `risk:` off a card on disk - and a fragment with
# no card is refused as an unreadable risk, exactly as `validate` refuses it.
# So every fake carries one, READ unless the test says otherwise.
CARDS = tempfile.mkdtemp(prefix="heron-batch-cards-")


class FakeFragment(object):
    """Only what the judge reads: a slug, a status, a contract and a card."""

    def __init__(self, slug, provides, status="DRAFT", risk="READ"):
        self.slug = slug
        self.status = status
        self._provides = provides
        self.folder = os.path.join(CARDS, "brain", "fragments", slug)
        if not os.path.isdir(self.folder):
            os.makedirs(self.folder)
        with io.open(os.path.join(self.folder, "fragment.yaml"), "w",
                     encoding="utf-8") as fh:
            fh.write(u"risk: %s\n" % risk)

    def provides(self):
        return self._provides


def record(positive=None, negative=None, positive_ok=True, negative_ok=True,
           positive_error=None, negative_error=None):
    phases = []
    if positive is not None or positive_error:
        phases.append({"phase": "positive",
                       "ok": positive_ok and not positive_error,
                       "provides": positive or {},
                       "error": positive_error, "message": positive_error})
    if negative is not None or negative_error:
        phases.append({"phase": "negative",
                       "ok": negative_ok and not negative_error,
                       "provides": negative or {},
                       "error": negative_error, "message": negative_error})
    return {"fragment": "sample", "phases": phases}


# ---------------------------------------------------------------------------
# Hole 2 - a fragment that did nothing must not pass
# ---------------------------------------------------------------------------

def test_unreadable_is_not_evidence_of_work():
    print("An unreadable value is not evidence the fragment did anything")

    # read-graphic-overrides, exactly as it came back. `overrides` renders as a
    # bare type name, and `withOverride` - the only thing that counts - was 0.
    frag = FakeFragment("read-graphic-overrides", [
        {"name": "overrides", "type": "OverrideGraphicSettings"},
        {"name": "findings", "type": "IList<string>"},
        {"name": "withOverride", "type": "int"},
    ])
    both = {"overrides": "OverrideGraphicSettings",
            "findings": "1 item(s) [no view overrides found]",
            "withOverride": "0"}

    verdict, why = BP.judge(record(positive=both, negative=both), frag)
    check(verdict == BP.POSITIVE_EMPTY,
          "an object plus a zero count reads as EMPTY, not as work (%s)" % verdict)

    # And with NOTHING readable at all, the honest answer is that we cannot tell -
    # which is a different fact from "it returned zero", and also not a pass.
    only_object = FakeFragment("sample", [
        {"name": "overrides", "type": "OverrideGraphicSettings"}])
    verdict, why = BP.judge(
        record(positive={"overrides": "OverrideGraphicSettings"},
               negative={"overrides": "OverrideGraphicSettings"}), only_object)
    check(verdict == BP.POSITIVE_UNREADABLE,
          "nothing readable is UNREADABLE, and still not a pass (%s)" % verdict)


def test_a_flag_that_flips_is_evidence_and_one_that_does_not_is_not():
    print("A boolean result counts only when the two legs disagree")

    # apply-view-filter, exactly as declared: ONE result, and it is a bool.
    # `_as_count` reads a boolean as zero on purpose, so before 2026-09-15 this
    # fragment was POSITIVE EMPTY however well it was arranged - no view, no
    # filter and no override could have answered it, because the judge was not
    # reading the value at all.
    frag = FakeFragment("apply-view-filter", [
        {"name": "applied", "type": "bool"},
        {"name": "findings", "type": "IList<string>"},
    ])

    # The real run, 2026-09-15 on Snowdon-scratch: `Model Linking` took the
    # filter, `Project View` refused it in Revit's own words.
    verdict, why = BP.judge(record(
        positive={"applied": "true",
                  "findings": "1 item(s) ['ABL - General Lighting' is now on 'Model Linking']"},
        negative={"applied": "false",
                  "findings": "1 item(s) ['ABL - General Lighting' could not be applied]"}), frag)
    check(verdict == BP.PASS,
          "true against false is the comparison D-30 asks for (%s)" % verdict)
    check("applied" in (why or ""),
          "and the verdict NAMES the flag that flipped: %r" % why)

    # THE HALF THAT MATTERS MORE. report-global-parameters answers
    # `allowed: true` in BOTH legs, because the document permits globals either
    # way. Counting that as content would make an empty answer impossible for
    # it to demonstrate, which is the whole reason a boolean reads as zero.
    same = {"allowed": "true", "globalCount": "0"}
    steady = FakeFragment("report-global-parameters", [
        {"name": "allowed", "type": "bool"},
        {"name": "globalCount", "type": "int"},
    ])
    verdict, why = BP.judge(record(positive=same, negative=same), steady)
    check(verdict == BP.POSITIVE_EMPTY,
          "a flag true in both legs stays EMPTY - it is not evidence (%s)" % verdict)

    # And a flag the contract calls bookkeeping is not evidence either, however
    # it moves. D-52: the evidence it LOOKED is never a thing it FOUND.
    booked = FakeFragment("sample", [
        {"name": "ran", "type": "bool", "role": "accounting"},
        {"name": "found", "type": "int"},
    ])
    verdict, why = BP.judge(record(positive={"ran": "true", "found": "0"},
                                   negative={"ran": "false", "found": "0"}), booked)
    check(verdict == BP.POSITIVE_EMPTY,
          "an accounting flag flipping proves nothing (%s)" % verdict)


def test_a_sentence_result_is_read_as_found_or_nothing():
    print("A result declared as a STRING is found when it says something")

    # EDIT_REVISION version 2, as declared: `changed` is ONE joined string so
    # the reply prints every column, and it is the only result. Row 5b-411:
    # on 2026-10-10 (Project2 scratch, Revit 2020) both legs held and the
    # runner said POSITIVE UNREADABLE, because it could only read a count.
    # `changed` below is the positive's own value from that row. The
    # negative's `alreadyThat` and the `revisionName` are stand-ins: the row
    # records only that the negative named the description and the date.
    frag = FakeFragment("edit-revision", [
        {"name": "changed", "type": "string", "role": "result"},
        {"name": "alreadyThat", "type": "string", "role": "accounting"},
        {"name": "nowIssued", "type": "bool", "role": "accounting"},
        {"name": "revisionName", "type": "string", "role": "accounting"},
        {"name": "findings", "type": "string"},
        {"name": "refused", "type": "string", "role": "accounting"},
    ])
    positive = {"changed": "issued by (blank) -> 'M.Sagheer'; issued to "
                           "(blank) -> 'A.Rahmani'; issued",
                "alreadyThat": "", "nowIssued": "true",
                "revisionName": "Seq. 2 'For review'",
                "findings": "Seq. 2 'For review' is issued", "refused": ""}
    negative = {"changed": "",
                "alreadyThat": "description 'For review'; date '2026-10-10'",
                "nowIssued": "true", "revisionName": "Seq. 2 'For review'",
                "findings": "Seq. 2 'For review' is issued", "refused": ""}

    verdict, why = BP.judge(record(positive=positive, negative=negative), frag)
    check(verdict == BP.PASS,
          "a sentence in the positive and nothing in the negative is a PASS (%s)"
          % verdict)
    check((why or "").startswith("positive: changed issued by"),
          "and the verdict names it: %r" % why)
    check(BP.positive_worked({"provides": positive}, frag)[0] is None,
          "the signing gate's own question agrees: the positive found something")

    # EMPTY IS NOTHING, however it arrives.
    for nothing in ("", "  ", "(null)"):
        quiet = dict(positive, changed=nothing)
        verdict, _ = BP.judge(record(positive=quiet, negative=negative), frag)
        check(verdict == BP.POSITIVE_EMPTY,
              "`changed %r` found nothing (%s)" % (nothing, verdict))

    # ACCOUNTING IS NEVER EVIDENCE, sentence or not. `alreadyThat` filled and
    # `changed` empty is exactly what a second identical run answers (D-52) -
    # so a positive that IS the negative's answer must not pass.
    verdict, _ = BP.judge(record(positive=dict(negative), negative=negative), frag)
    check(verdict == BP.POSITIVE_EMPTY,
          "an accounting sentence is not a result (%s)" % verdict)

    # THE COUNTS DECIDE (D-51), and a sentence never outranks them. Found by
    # review before merging: set-material-colour declares `changed: int`
    # beside `wasColour` and `nowColour`, which read back the colour whether
    # or not anything changed. Asked for the colour a material already has,
    # it changes nothing, `changed` is 0 - and with both colours filled the
    # sentence reading alone would have PASSED it.
    colour = FakeFragment("set-material-colour", [
        {"name": "changed", "type": "int", "role": "result"},
        {"name": "wasColour", "type": "string", "role": "result"},
        {"name": "nowColour", "type": "string", "role": "result"},
        {"name": "alreadyThat", "type": "bool", "role": "accounting"},
    ])
    same_colour = {"changed": "0", "wasColour": "200,120,60",
                   "nowColour": "200,120,60", "alreadyThat": "true"}
    not_found = {"changed": "0", "wasColour": "", "nowColour": "",
                 "alreadyThat": "false"}
    verdict, why = BP.judge(record(positive=same_colour, negative=not_found),
                            colour)
    check(verdict == BP.POSITIVE_EMPTY,
          "a zero count beside a filled sentence is EMPTY (%s: %s)"
          % (verdict, why))
    check(BP.positive_worked({"provides": same_colour}, colour)[0]
          == BP.POSITIVE_EMPTY,
          "and the signing gate's question says the same")
    verdict, _ = BP.judge(record(positive=dict(same_colour, changed="1"),
                                 negative=not_found), colour)
    check(verdict == BP.PASS, "a count that moved still passes (%s)" % verdict)
    # A flag is the fragment saying whether it worked, so it decides too: a
    # refused request with `changed false` and the category read back filled.
    category = FakeFragment("set-family-category", [
        {"name": "changed", "type": "bool", "role": "result"},
        {"name": "category", "type": "string", "role": "result"},
    ])
    verdict, _ = BP.judge(record(
        positive={"changed": "false", "category": "Generic Models"},
        negative={"changed": "false", "category": ""}), category)
    check(verdict == BP.POSITIVE_EMPTY,
          "a false flag beside a filled sentence is EMPTY (%s)" % verdict)
    # And `expect:` naming the sentence alone is a person saying it is the
    # result - the escape hatch for a fragment whose count means something else.
    verdict, _ = BP.judge(record(positive=same_colour, negative=not_found),
                          colour, expect=["nowColour"])
    check(verdict == BP.PASS,
          "`expect: nowColour` judges the sentence alone (%s)" % verdict)

    # ONLY A DECLARED `role: result`. A string the contract never called a
    # result - no role at all - stays unreadable rather than counting.
    unsaid = FakeFragment("sample", [{"name": "summary", "type": "string"}])
    verdict, _ = BP.judge(record(positive={"summary": "three ducts sized"},
                                 negative={"summary": ""}), unsaid)
    check(verdict == BP.POSITIVE_UNREADABLE,
          "a string with no `role: result` is still not read as found (%s)"
          % verdict)

    # A SENTENCE IN BOTH LEGS IS A FINDING, NOT A PASS - the negative is
    # judged by the same reading as the positive.
    verdict, why = BP.judge(record(positive=positive,
                                   negative=dict(negative, changed="issued")),
                            frag)
    check(verdict == BP.NEG_NOT_EMPTY,
          "a result string in the negative is content (%s)" % verdict)
    check(why == "the negative returned changed issued",
          "and it says what came back, even one word: %r" % why)

    # THE HOLE A ONE-SIDED FIX WOULD OPEN. A one-word string such as
    # `SupplyAir` matches the shape of a bare type name, which the negative
    # used to skip as a helper object. Read as found in the positive and
    # skipped in the negative, the same word in both legs would have PASSED.
    kind = FakeFragment("create-mep-system-type", [
        {"name": "classification", "type": "string", "role": "result"}])
    same = {"classification": "SupplyAir"}
    verdict, _ = BP.judge(record(positive=same, negative=same), kind)
    check(verdict != BP.PASS,
          "the same word in both legs is never a pass (%s)" % verdict)
    verdict, _ = BP.judge(record(positive=same,
                                 negative={"classification": ""}), kind)
    check(verdict == BP.PASS,
          "a word in the positive and nothing in the negative is (%s)" % verdict)

    # `expect:` STILL DECIDES WHAT IS JUDGED. Naming the result reads it as a
    # sentence; naming an accounting string does not make it evidence.
    verdict, _ = BP.judge(record(positive=positive, negative=negative), frag,
                          expect=["changed"])
    check(verdict == BP.PASS, "`expect: changed` passes (%s)" % verdict)
    verdict, _ = BP.judge(record(positive=negative, negative=negative), frag,
                          expect=["alreadyThat"])
    check(verdict == BP.POSITIVE_UNREADABLE,
          "`expect: alreadyThat` is still accounting, still unreadable (%s)"
          % verdict)


def test_counts_and_ids_read_as_they_did():
    print("ElementIds, item counts and (null) are read exactly as before")

    # An ElementId arrives as its number, a creator that declined as "(null)".
    made = FakeFragment("create-level", [
        {"name": "created", "type": "ElementId", "role": "result"}])
    verdict, _ = BP.judge(record(positive={"created": "312345"},
                                 negative={"created": "(null)"}), made)
    check(verdict == BP.PASS, "an id against (null) passes (%s)" % verdict)
    verdict, _ = BP.judge(record(positive={"created": "(null)"},
                                 negative={"created": "(null)"}), made)
    check(verdict == BP.POSITIVE_EMPTY, "(null) in both is EMPTY (%s)" % verdict)

    # A collection arrives as "N item(s) [...]".
    found = FakeFragment("sample", [
        {"name": "found", "type": "IList<ElementId>", "role": "result"}])
    verdict, _ = BP.judge(record(positive={"found": "3 item(s) [1, 2, 3]"},
                                 negative={"found": "0 item(s)"}), found)
    check(verdict == BP.PASS, "3 item(s) against 0 item(s) passes (%s)" % verdict)
    verdict, _ = BP.judge(record(positive={"found": "0 item(s)"},
                                 negative={"found": "0 item(s)"}), found)
    check(verdict == BP.POSITIVE_EMPTY, "0 item(s) in both is EMPTY (%s)" % verdict)

    # A sentence that is not declared a string is still not a quantity.
    verdict, _ = BP.judge(record(positive={"found": "three ducts"},
                                 negative={"found": "0 item(s)"}), found)
    check(verdict == BP.POSITIVE_UNREADABLE,
          "a sentence where a list was declared is UNREADABLE (%s)" % verdict)


def test_a_work_counter_is_not_a_finding():
    print("A count of what was looked at is not a count of what was found")

    # check-flow-direction, as declared after 2026-09-09.
    frag = FakeFragment("check-flow-direction", [
        {"name": "bothOut", "type": "IList<string>"},
        {"name": "bothIn", "type": "IList<string>"},
        {"name": "jointsChecked", "type": "int", "role": "accounting"},
        {"name": "bidirectionalSkipped", "type": "int", "role": "accounting"},
    ])
    both = {"bothOut": "0 item(s)", "bothIn": "0 item(s)",
            "jointsChecked": "15", "bidirectionalSkipped": "25"}

    verdict, why = BP.judge(record(positive=both, negative=both), frag)
    check(verdict == BP.POSITIVE_EMPTY,
          "declared accounting never counts as work (%s)" % verdict)

    # THE PATTERNS DO NOT CATCH AN ECHO OF THE INPUT, and pretending otherwise
    # would be the more comfortable test. add-schedule-fields provides `wanted` -
    # the field names the CALLER asked for - which is 1 in both legs and is not
    # a finding. Nothing in the contract or in the naming says so.
    #
    # What saves it is that an echo is non-zero in the NEGATIVE too, so the
    # second leg refuses the job. The verdict then reads NEG NOT EMPTY when the
    # real defect is `added 0` in both legs - honest, and pointing one step to
    # the side of the truth.
    undeclared = FakeFragment("add-schedule-fields", [
        {"name": "added", "type": "int"},
        {"name": "alreadyPresent", "type": "int"},
        {"name": "unknownField", "type": "IList<string>"},
        {"name": "wanted", "type": "IList<string>"},
    ])
    both = {"added": "0", "alreadyPresent": "0", "unknownField": "0 item(s)",
            "wanted": "1 item(s) [Comments]"}
    verdict, why = BP.judge(record(positive=both, negative=both), undeclared)
    check(verdict == BP.NEG_NOT_EMPTY,
          "an input echo passes the positive, and the negative catches it (%s)"
          % verdict)

    # `expect:` is what names the defect properly, and this is the case the
    # skill tells the next session to reach for it on.
    verdict, why = BP.judge(record(positive=both, negative=both), undeclared,
                            expect=["added"])
    check(verdict == BP.POSITIVE_EMPTY,
          "`expect: added` says plainly that it added nothing (%s)" % verdict)


def test_a_working_positive_passes_and_a_full_one_is_named():
    print("A fragment that did something says what it did")

    frag = FakeFragment("check-sleeve-size", [
        {"name": "undersized", "type": "IList<ElementId>"},
        {"name": "findings", "type": "IList<string>"},
        {"name": "scanned", "type": "int"},
    ])
    verdict, why = BP.judge(record(
        positive={"undersized": "22 item(s)", "findings": "22 item(s)",
                  "scanned": "22"},
        negative={"undersized": "0 item(s)", "findings": "1 item(s)",
                  "scanned": "0"}), frag)
    check(verdict == BP.PASS, "both halves held (%s)" % verdict)
    check("undersized" in why, "the report names the result that moved: %s" % why)


def test_expect_beats_the_patterns():
    print("`expect:` is where a person's knowledge of the fragment goes")

    # comparedCount is a work counter the naming patterns do NOT catch, so
    # without `expect:` this fragment passes on having looked at things.
    frag = FakeFragment("compare-elements", [
        {"name": "differing", "type": "IDictionary<string, IList<string>>"},
        {"name": "identicalCount", "type": "int"},
        {"name": "comparedCount", "type": "int"},
    ])
    phases = record(positive={"differing": "0 entry(ies)", "identicalCount": "0",
                              "comparedCount": "22"},
                    negative={"differing": "0 entry(ies)", "identicalCount": "0",
                              "comparedCount": "0"})

    verdict, _ = BP.judge(phases, frag)
    check(verdict == BP.PASS,
          "left to the patterns, comparedCount 22 reads as work (%s)" % verdict)

    verdict, why = BP.judge(phases, frag, expect=["differing"])
    check(verdict == BP.POSITIVE_EMPTY,
          "named explicitly, `differing` was 0 and the job did not pass (%s)"
          % verdict)


def test_the_negative_is_still_judged():
    print("The negative leg is judged by heron_validate, not by a second copy")

    frag = FakeFragment("sample", [{"name": "found", "type": "IList<ElementId>"}])
    verdict, why = BP.judge(record(positive={"found": "22 item(s)"},
                                   negative={"found": "9 item(s)"}), frag)
    check(verdict == BP.NEG_NOT_EMPTY,
          "a negative that returns content is a FINDING (%s)" % verdict)
    check("9 item(s)" in why, "and it says what came back: %s" % why)

    verdict, why = BP.judge(record(positive={"found": "22 item(s)"},
                                   negative_error="needs_unbound"), frag)
    check(verdict == BP.NO_NEGATIVE,
          "a negative that never ran leaves D-30's second leg missing (%s)"
          % verdict)


def test_the_positive_is_reported_first():
    print("When both halves are wrong, the POSITIVE is the headline")

    frag = FakeFragment("sample", [{"name": "found", "type": "IList<ElementId>"}])
    verdict, why = BP.judge(record(positive={"found": "0 item(s)"},
                                   negative={"found": "9 item(s)"}), frag)
    check(verdict == BP.POSITIVE_EMPTY,
          "a fragment that found nothing makes its own negative meaningless (%s)"
          % verdict)
    check("negative did not come back empty" in why,
          "and the negative's problem is still carried: %s" % why)


def test_no_reply_is_reported_as_a_timeout():
    print("Revit not answering is a timeout, and says nothing about the fragment")

    frag = FakeFragment("set-mep-size", [{"name": "sized", "type": "int"}])
    verdict, why = BP.judge(record(positive_ok=False, positive_error="no_reply"),
                            frag)
    check(verdict == BP.TIMEOUT, "no_reply is a TIMEOUT (%s)" % verdict)


# ---------------------------------------------------------------------------
# Hole 1 - finished work is never re-proved
# ---------------------------------------------------------------------------

def test_every_proven_fragment_in_the_library_is_refused():
    print("A dry run over every PROVEN fragment reports ALREADY and runs nothing")

    found, _ = HF.load_all()
    library = dict((frag.slug, frag) for frag in found.values())
    proven = sorted(slug for slug, frag in library.items()
                    if frag.status in BP.FINISHED)

    check(len(proven) > 0,
          "the library has %d fragment(s) at PROVEN or PRODUCTION" % len(proven))

    # DERIVED, NEVER TYPED. A committed job file naming today's proven fragments
    # would stop testing anything the day one of them is demoted, and would say
    # nothing about the ones promoted since. The library is the list.
    workspace = tempfile.mkdtemp(prefix="heron-batch-")
    try:
        job_file = os.path.join(workspace, "already-proven.yaml")
        with io.open(job_file, "w", encoding="utf-8") as fh:
            fh.write(u"defaults:\n  negative-set:\n    categoryName: Sheets\n")
            fh.write(u"jobs:\n")
            for slug in proven:
                fh.write(u"  - fragment: %s\n" % slug)

        jobs, problems = BP.read_jobs(job_file)
        check(not problems, "the job file reads cleanly")
        check(len(jobs) == len(proven),
              "all %d job(s) were read" % len(proven))

        verdicts = [BP.job_refusal(job, library)[0] for job in jobs]
        wrong = [job["fragment"] for job, verdict in zip(jobs, verdicts)
                 if verdict != BP.ALREADY]
        check(not wrong,
              "every one reports ALREADY%s"
              % ("" if not wrong else " - except %s" % ", ".join(wrong[:5])))

        report = os.path.join(workspace, "findings.json")
        code = BP.main([job_file, "--dry-run", "--report", report])
        check(code == 0, "the dry run exits 0 - finished work is not an error")

        findings = json.loads(io.open(report, encoding="utf-8").read())
        check(all(r["verdict"] == BP.ALREADY for r in findings["results"]),
              "and the written findings say ALREADY for every one")
        check(findings["dry_run"] is True, "the findings record that it was a dry run")

        # NOTHING WAS PROMOTED. The whole point of the refusal is that these
        # fragments are not touched at all - so their proofs are still theirs.
        after, _ = HF.load_all()
        moved = [f.slug for f in after.values()
                 if library[f.slug].status != f.status]
        check(not moved, "no fragment's heron-status moved%s"
              % ("" if not moved else " - %s did" % ", ".join(moved)))
    finally:
        shutil.rmtree(workspace)


# ---------------------------------------------------------------------------
# The job file itself - refusals that happen before Revit is touched
# ---------------------------------------------------------------------------

def test_a_job_with_no_negative_arrangement_is_refused():
    print("A job that would stop and wait at the keyboard is refused instead")

    library = {"sample": FakeFragment("sample", [{"name": "found"}])}
    job = {"fragment": "sample", "setup": [], "set": {}, "negative-set": {},
           "write": False, "cross": None, "in": None, "negative-in": None,
           "expect": None, "timeout": 60}

    verdict, why = BP.job_refusal(job, library)
    check(verdict == BP.REFUSED, "refused before anything is sent (%s)" % verdict)
    check("negative-set" in why, "and it says how to fix it: %s" % why)

    job["negative-set"] = {"categoryName": "Sheets"}
    verdict, why = BP.job_refusal(job, library)
    check(verdict is None, "with a negative arrangement it is runnable")

    # A NEGATIVE THAT DIFFERS ONLY IN THE ARRANGEMENT IS STILL A NEGATIVE.
    # FRAGMENT-ISSUES row 149: `negative-setup-set` gives the setup chain its
    # own values, and this check did not know about it - so a job whose two
    # legs differ only in how they were ARRANGED was refused here, before the
    # client was ever called, with a message naming the two keys it did know.
    # The client's own half of that bug had already been repaired and this one
    # still said no, which is why it was found by RUNNING such a job rather
    # than by reading either file.
    job["negative-set"] = {}
    job["negative-setup-set"] = {"categories": "Floors"}
    verdict, why = BP.job_refusal(job, library)
    check(verdict is None,
          "a negative that differs ONLY in its setup chain is runnable too")

    check("negative-setup-set" in BP.job_refusal(
              {"fragment": "sample", "setup": [], "set": {}, "negative-set": {},
               "write": False, "cross": None, "in": None, "negative-in": None,
               "expect": None, "timeout": 60}, library)[1],
          "and the refusal names all three ways to say it, not two")

    # AND A JOB DICT WITHOUT THE KEY AT ALL MUST NOT CRASH. It is optional by
    # design, and every fixture written before today leaves it out.
    bare = {"fragment": "sample", "setup": [], "set": {},
            "negative-set": {"categoryName": "Sheets"}, "write": False,
            "cross": None, "in": None, "negative-in": None,
            "expect": None, "timeout": 60}
    verdict, _ = BP.job_refusal(bare, library)
    check(verdict is None, "a job dict missing the optional key reads as absent")


def test_a_negative_that_would_hand_the_chain_the_wrong_values_is_refused():
    print("A setup chain with its own values needs them on the negative leg too")
    # Row 5b-242: the client gives the negative leg's chain
    # `negative_setup_values or using or values` - the NEGATIVE-SET, never the
    # positive's `setup-set`. A job whose chain needs other values than its
    # fragment was refused on the negative leg before the fragment ran, and
    # the dry run said WOULD RUN.
    library = {"sample": FakeFragment("sample", [{"name": "found"}]),
               "find-schedules": FakeFragment("find-schedules", [{"name": "schedules"}])}
    job = {"fragment": "sample", "setup": ["find-schedules"],
           "setup-set": {"nameContains": "Air Terminal Schedule"},
           "set": {"fieldNames": "Flow"}, "negative-set": {"fieldNames": "Family and Type"},
           "negative-setup-set": {}, "write": True, "cross": None, "in": None,
           "negative-in": None, "expect": None, "timeout": 60}
    verdict, why = BP.job_refusal(job, library)
    check(verdict == BP.REFUSED and "negative-setup-set" in (why or ""),
          "`setup-set` and `negative-set` with no `negative-setup-set` is refused, naming the key (%s)" % why)
    job["negative-setup-set"] = {"nameContains": "Air Terminal Schedule"}
    check(BP.job_refusal(job, library)[0] is None, "given its own chain values, it runs")
    job["setup-set"], job["negative-setup-set"] = {}, {}
    check(BP.job_refusal(job, library)[0] is None,
          "and a chain with no values of its own reads the negative-set, as it always did")


def test_the_job_file_is_checked_against_the_contract():
    print("A mistyped name in the job file is caught before Revit is touched")

    library = {"sample": FakeFragment("sample", [{"name": "found"}]),
               "select-by-category-name": FakeFragment("select-by-category-name", [])}
    base = {"fragment": "sample", "setup": [], "set": {},
            "negative-set": {"categoryName": "Sheets"}, "write": False,
            "cross": None, "in": None, "negative-in": None, "expect": None,
            "timeout": 60}

    job = dict(base, expect=["fnud"])
    verdict, why = BP.job_refusal(job, library)
    check(verdict == BP.REFUSED, "`expect: fnud` is refused (%s)" % verdict)
    check("found" in why, "and the fragment's real provides are named: %s" % why)

    job = dict(base, setup=["select-by-category-name", "set-slection"])
    verdict, why = BP.job_refusal(job, library)
    check(verdict == BP.REFUSED, "a setup step that is not a fragment is refused")

    job = dict(base, cross="elements")
    verdict, why = BP.job_refusal(job, library)
    check(verdict == BP.REFUSED, "an unknown cross-check is refused")

    job = dict(base, fragment="no-such-fragment")
    verdict, why = BP.job_refusal(job, library)
    check(verdict == BP.NO_FRAGMENT, "an unknown fragment is NO FRAGMENT")


def test_an_admin_or_publish_fragment_is_never_sent_by_a_batch():
    print("An ADMIN or PUBLISH fragment is its own bucket, never WOULD RUN")
    # Row 5b-440. `validate` without --allow-publish refuses these (D-106
    # point 6: no batch sends one), and the dry run never asked - it said
    # WOULD RUN for add-family-shared-parameters at risk: ADMIN, and the real
    # run came back DID NOT RUN.
    base = {"fragment": "admin-sample", "setup": [], "set": {"a": "1"},
            "negative-set": {"a": "2"}, "write": False, "cross": None,
            "in": None, "negative-in": None, "expect": None, "timeout": 60}
    library = {"admin-sample": FakeFragment("admin-sample", [], risk="ADMIN"),
               "publish-sample": FakeFragment("publish-sample", [],
                                              risk="PUBLISH"),
               "modify-sample": FakeFragment("modify-sample", [],
                                             risk="MODIFY"),
               "odd-sample": FakeFragment("odd-sample", [], risk="LAUNCH")}

    word = getattr(BP, "NOT_IN_A_BATCH", None)
    check(word is not None, "the runner has a verdict for this at all")
    if word is None:
        word = "WILL NOT RUN IN A BATCH"   # so the checks below still RUN and FAIL

    verdict, why = BP.job_refusal(base, library)
    check(verdict == word, "an ADMIN fragment is %s (%s)" % (word, verdict))
    why = why or ""
    check("risk ADMIN" in why, "and it names the risk: %s" % why)
    check("validate --allow-publish" in why and "--write" in why,
          "and the command that does send it, with --allow-publish and --write")
    check("admin-sample" in why.split("`")[1] if why.count("`") >= 2 else False,
          "and that command names the fragment")
    check("Admin" in why, "and the switch that has to be on in Revit")

    verdict, why = BP.job_refusal(dict(base, fragment="publish-sample"), library)
    check(verdict == word and "risk PUBLISH" in (why or ""),
          "a PUBLISH fragment is the same bucket (%s)" % verdict)

    check(BP.job_refusal(dict(base, fragment="modify-sample"), library)[0] is None,
          "a MODIFY fragment is still runnable - only the levels above are held")

    verdict, _ = BP.job_refusal(dict(base, fragment="odd-sample"), library)
    check(verdict == BP.REFUSED, "a risk nobody knows is REFUSED (%s)" % verdict)

    # A SETUP STEP ABOVE MODIFY IS NEVER SENT, WHATEVER THE FLAGS - `validate`
    # asks `risk_refusal` of every step with no switch. So a job with one is
    # REFUSED outright, behind a runnable fragment and an ADMIN one alike: the
    # by-name command would fail on the step too.
    library["no-card-step"] = FakeFragment("no-card-step", [])
    os.remove(os.path.join(library["no-card-step"].folder, "fragment.yaml"))
    for step, risk in (("admin-sample", "ADMIN"), ("publish-sample", "PUBLISH")):
        for fragment in ("modify-sample", "admin-sample"):
            verdict, why = BP.job_refusal(
                dict(base, fragment=fragment, setup=[step]), library)
            check(verdict == BP.REFUSED and step in (why or "")
                  and ("risk %s" % risk) in (why or ""),
                  "a setup step at %s behind %s is REFUSED, naming the step and "
                  "its risk (%s)" % (risk, fragment, verdict))
    verdict, why = BP.job_refusal(
        dict(base, fragment="modify-sample", setup=["no-card-step"]), library)
    check(verdict == BP.REFUSED and "no-card-step" in (why or ""),
          "a setup step whose risk cannot be read is REFUSED (%s)" % verdict)
    check(BP.job_refusal(dict(base, fragment="modify-sample",
                              setup=["modify-sample"]), library)[0] is None,
          "a MODIFY setup step is still runnable")

    # A JOB DICT MISSING THE OPTIONAL KEYS MUST NOT CRASH THE REFUSAL. The
    # command it prints is built from them, and job_refusal is called with
    # partial dicts by design.
    try:
        verdict, _ = BP.job_refusal({"fragment": "admin-sample", "setup": [],
                                     "negative-set": {"a": "2"},
                                     "negative-in": None, "expect": None,
                                     "cross": None}, library)
        check(verdict == word, "a partial job dict gets the same verdict")
    except (KeyError, TypeError) as exc:
        check(False, "a partial job dict crashed the refusal: %r" % exc)

    # AND THE BATCH ITSELF: a dry run says so, the exit code stays 0 - the job
    # file is not broken, a batch is the wrong way to send it - and a real run
    # sends nothing, because the refusal comes before `run_one`.
    sent = []
    real_run_one, real_load_all = BP.run_one, BP.HF.load_all
    workspace = tempfile.mkdtemp(prefix="heron-batch-")
    try:
        job_file = os.path.join(workspace, "admin.yaml")
        with io.open(job_file, "w", encoding="utf-8") as fh:
            fh.write(u"jobs:\n  - fragment: admin-sample\n"
                     u"    set: {a: 1}\n    negative-set: {a: 2}\n")
        BP.HF.load_all = lambda *a, **k: ({"x": library["admin-sample"]}, [])
        BP.run_one = lambda *a, **k: sent.append(a) or (None, BP.DID_NOT_RUN, "")
        for flags in (["--dry-run"], []):
            report = os.path.join(workspace, "findings.json")
            code = BP.main([job_file, "--report", report] + flags)
            findings = json.loads(io.open(report, encoding="utf-8").read())
            label = "dry run" if flags else "real run"
            check([r["verdict"] for r in findings["results"]] == [word],
                  "the %s reports %s, not WOULD RUN" % (label, word))
            check(code == 0, "the %s exits 0 (%s)" % (label, code))
        check(not sent, "and nothing was ever handed to `validate`")
    finally:
        BP.run_one, BP.HF.load_all = real_run_one, real_load_all
        shutil.rmtree(workspace)


def test_defaults_merge_key_by_key():
    print("One selection written once, and a job overrides only what differs")

    workspace = tempfile.mkdtemp(prefix="heron-batch-")
    try:
        path = os.path.join(workspace, "jobs.yaml")
        with io.open(path, "w", encoding="utf-8") as fh:
            fh.write(u"defaults:\n"
                     u"  setup: [select-by-category-name, set-selection]\n"
                     u"  set:\n    categoryName: Ducts\n"
                     u"    inViewOnly: \"FloorPlan: M1\"\n"
                     u"  negative-set:\n    categoryName: Sheets\n"
                     u"jobs:\n"
                     u"  - fragment: a\n"
                     u"  - fragment: b\n    set:\n      toleranceMm: 25\n")
        jobs, problems = BP.read_jobs(path)
        check(not problems, "the file reads cleanly")
        check(jobs[1]["set"] == {"categoryName": "Ducts",
                                 "inViewOnly": "FloorPlan: M1",
                                 "toleranceMm": 25},
              "the second job kept the shared selection and added its own value")
        check(jobs[0]["setup"] == ["select-by-category-name", "set-selection"],
              "and inherited the setup chain")
    finally:
        shutil.rmtree(workspace)


def test_the_command_line_is_one_the_client_actually_parses():
    """THE TEST THAT EARNED ITS PLACE. Written after every job in the example
    file came back with the client's usage text instead of a run.

    `--in`, `--negative-in`, `--cross` and `--out` are read POSITIONALLY: the
    client walks pairs off the front of what is left and then requires exactly
    one bare token. Put the fragment name first and none of the four is ever
    seen - so the line looks perfectly reasonable, parses to nothing, and the
    only way to find out is to spend a Revit session on it.

    So the line is handed to the client's own `main`, with `cmd_validate`
    replaced. Nothing connects, nothing is discovered, and the parser is the
    real one rather than a reading of it.
    """
    print("The line this builds is one heron_bridge_client can parse")

    if os.name != "nt":
        # The client refuses on the first line off Windows - named pipes - so
        # the parser below is unreachable there. Skipped rather than faked.
        check(True, "skipped off Windows: the client refuses before parsing")
        return

    import heron_bridge_client as CLIENT

    captured = {}

    def capture(name, **kwargs):
        captured["fragment"] = name
        captured.update(kwargs)
        return 0

    real, CLIENT.cmd_validate = CLIENT.cmd_validate, capture
    try:
        jobs, problems = BP.read_jobs(os.path.join(ROOT, "tools", "jobs",
                                                   "example.yaml"))
        check(not problems, "the shipped example job file still reads cleanly")

        for job in jobs:
            captured.clear()
            argv = BP.validate_command(job, "out.json")
            code = CLIENT.main(["heron_bridge_client.py"] + argv[2:])
            check(code == 0 and captured.get("fragment") == job["fragment"],
                  "%s parses, and reaches cmd_validate as itself" % job["fragment"])

        # And the values arrive whole, not split at the space in a view name.
        last = captured
        views = [v["value"] for v in (last.get("values") or [])
                 if v["name"] == "inViewOnly"]
        check(views == ["FloorPlan: M1"],
              "a view name with a space arrives in one piece: %r" % views)
        check([s for s in (last.get("setup") or [])] ==
              ["select-by-category-name", "set-selection"],
              "the setup chain arrives in order")
        check(last.get("out") == "out.json", "--out is seen at all")
        check(last.get("negative_values"),
              "the negative case's own values arrive separately")
    finally:
        CLIENT.cmd_validate = real


def test_a_value_with_a_space_is_quoted_when_printed():
    print("The line the dry run prints can be pasted into a shell")

    line = BP.readable_command(["validate", "x", "--set", "inViewOnly=FloorPlan: M1"])
    check('"inViewOnly=FloorPlan: M1"' in line,
          "a value holding a space is quoted: %s" % line)


# ---------------------------------------------------------------------------
# --unattended - a night with nobody at the keyboard
# ---------------------------------------------------------------------------
#
# WHAT `validate` PRINTS WHEN IT NEVER REACHED THE MODEL. Each is the sentence
# the add-in or the client prints, laid out as the client prints it - the
# lease's as FRAGMENT-ISSUES section 3i quotes it from a batch on 2026-09-09.
# The last test reads every marker back out of the file that prints it, so a
# sentence reworded there fails here rather than a night later.

OPENING = ("Could not identify the active model:\n  %s\n"
           "Refusing to record evidence about a model that will not name itself.\n")

LEASE_HELD = OPENING % (       # HeronLease.Claim, refused - session_in_use
    "This Revit is in use by another chat, so Heron has refused rather than "
    "taking it over mid-job. Nothing was sent to Revit.\nIt frees up after "
    "about 4 minute(s) without activity there, or immediately if that chat "
    "disconnects from the Heron button.")

MODEL_HELD = ("Model 'Materials IA Test' is in use by another chat. It is free "
              "when that chat says it is done, after 3 quiet minutes, or with "
              "Free this model on the Heron ribbon - at the latest in about 2 "
              "minutes from now. Nothing was sent to the model.")   # HeronModelHold.InUse

BUSY = ("Revit is busy and did not take the request. A dialog may be open, or "
        "a command may be running. Finish what is open in Revit and ask again.")

KEEP_TITLE = "Heron - keep this change?"    # HeronKeepQuestion.Title, read back below


def phase(name, error=None, message=None, provides=None):
    return {"phase": name, "ok": error is None, "error": error,
            "message": message, "provides": provides or {}}


def test_unattended_asks_again_only_when_revit_was_never_reached():
    print("--unattended asks again when Revit was held or busy, and never after a verdict")
    frag = FakeFragment("unattended-sample", [{"name": "found", "role": "result"}])

    # NOTHING WAS WRITTEN, so the verdict is DID NOT RUN on the first line
    # of what `validate` said - and the reason is on the line after it.
    first = LEASE_HELD.splitlines()[0]
    check(BP.retry_reason(BP.DID_NOT_RUN, first + "\n" + LEASE_HELD) == BP.HELD,
          "the lease held by another chat is waited out")
    check(BP.retry_reason(BP.DID_NOT_RUN, first) == BP.HELD,
          "and so is the opening's first line alone - all the wrapper ever saw")
    busy = OPENING % BUSY
    check(BP.retry_reason(BP.DID_NOT_RUN, busy.splitlines()[0] + "\n" + busy) == BP.BUSY,
          "a busy Revit is BUSY, though its refusal opens the same way")

    # A RECORD WAS WRITTEN AND A PHASE WAS REFUSED - judged as without the flag.
    cases = [
        ("the positive refused model_in_use",
         {"phases": [phase("positive", "model_in_use", MODEL_HELD)]}, BP.DID_NOT_RUN, BP.HELD),
        ("the positive refused revit_busy",
         {"phases": [phase("positive", "revit_busy", BUSY)]}, BP.DID_NOT_RUN, BP.BUSY),
        ("the negative refused model_in_use",
         {"phases": [phase("positive", provides={"found": "3"}),
                     phase("negative", "model_in_use", MODEL_HELD)]}, BP.NO_NEGATIVE, BP.HELD),
        ("the negative refused session_in_use",
         {"phases": [phase("positive", provides={"found": "3"}),
                     phase("negative", "session_in_use", "refused")]}, BP.NO_NEGATIVE, BP.HELD),
        ("the negative refused revit_busy",
         {"phases": [phase("positive", provides={"found": "3"}),
                     phase("negative", "revit_busy", BUSY)]}, BP.NO_NEGATIVE, BP.BUSY),
        ("a value the contract asks for, never given",
         {"phases": [phase("positive", "needs_request_values",
                           "this needs 'categoryName'")]}, BP.DID_NOT_RUN, None),
        ("a pass",
         {"phases": [phase("positive", provides={"found": "3"}),
                     phase("negative", provides={"found": "0"})]}, BP.PASS, None),
        # JUDGED, SO NEVER ASKED AGAIN. The positive found nothing before the
        # negative was refused: that is a finding about the arrangement, and
        # the held negative rides along in its detail.
        ("a positive that found nothing, the negative held",
         {"phases": [phase("positive", provides={"found": "0"}),
                     phase("negative", "model_in_use", MODEL_HELD)]}, BP.POSITIVE_EMPTY, None),
    ]
    for label, rec, expected_verdict, expected_reason in cases:
        verdict, why = BP.judge(rec, frag)
        if expected_verdict is not None:
            check(verdict == expected_verdict,
                  "%s reads %s (%s)" % (label, expected_verdict, verdict))
        else:
            check(verdict not in BP.NOTHING_JUDGED,
                  "%s is judged - %s" % (label, verdict))
        check(BP.retry_reason(verdict, why) == expected_reason,
              "%s: asked again as %s" % (label, expected_reason))

    for verdict in (BP.TIMEOUT, BP.POSITIVE_EMPTY, BP.NEG_NOT_EMPTY, BP.PASS):
        check(BP.retry_reason(verdict, LEASE_HELD + BUSY) is None,
              "%s is never asked again, whatever the text says" % verdict)


def test_unattended_waits_and_stops_at_the_limit():
    print("--unattended waits 30 s when held, 10 s when busy, and gives up at the limit")
    import argparse
    args = argparse.Namespace(session="4242", verbose=False, release=None)
    job = {"fragment": "unattended-sample", "expect": None}
    real_prove = BP.prove
    try:
        def scripted(answers):
            calls = []

            def fake(job, library, env, args, transcript=None):
                said, verdict, why = answers[min(len(calls), len(answers) - 1)]
                calls.append(1)
                if transcript is not None and said:
                    transcript.append(said)
                return verdict, why
            return fake, calls

        lease = (LEASE_HELD, BP.DID_NOT_RUN, LEASE_HELD.splitlines()[0])
        busy = (OPENING % BUSY, BP.DID_NOT_RUN, "Could not identify the active model:")
        passed = ("", BP.PASS, "positive: found 3")

        waits, closed = [], []
        BP.prove, calls = scripted([lease, lease, passed])
        verdict, why, tries = BP.prove_unattended(
            job, {}, {}, args, wait=waits.append,
            close=lambda pids: closed.append(pids) or [])
        check((verdict, tries) == (BP.PASS, 3) and waits == [30, 30] and not closed,
              "held twice, then a pass: three tries, two 30 s waits, nothing closed "
              "(%s, %d, %s)" % (verdict, tries, waits))

        waits, closed = [], []
        BP.prove, calls = scripted([busy, passed])
        verdict, why, tries = BP.prove_unattended(
            job, {}, {}, args, wait=waits.append,
            close=lambda pids: closed.append(pids) or ["closed it"])
        check((verdict, tries) == (BP.PASS, 2) and waits == [10] and closed == [[4242]],
              "busy once: the closer is asked about Revit 4242 alone, one 10 s wait "
              "(%s, %d, %s, %s)" % (verdict, tries, waits, closed))

        waits = []
        BP.prove, calls = scripted([lease])
        verdict, why, tries = BP.prove_unattended(
            job, {}, {}, args, wait=waits.append, close=lambda pids: [],
            attempts=BP.ATTEMPTS)
        check(tries == BP.ATTEMPTS == len(calls) and len(waits) == BP.ATTEMPTS - 1,
              "held every time: %d tries and no more" % BP.ATTEMPTS)
        check(verdict == BP.DID_NOT_RUN and why == LEASE_HELD.splitlines()[0],
              "and the last try's own verdict stands: %s %s" % (verdict, why))

        waits = []
        BP.prove, calls = scripted([("", BP.POSITIVE_EMPTY, "found 0")])
        verdict, _, tries = BP.prove_unattended(job, {}, {}, args, wait=waits.append)
        check((verdict, tries, waits) == (BP.POSITIVE_EMPTY, 1, []),
              "a finding is reported at once, never asked again")

        def broken(*a, **k):
            raise ValueError("a record nobody could read")
        BP.prove = broken
        verdict, why, tries = BP.prove_unattended(job, {}, {}, args, wait=waits.append)
        check(verdict == BP.DID_NOT_RUN and "ValueError" in why and tries == 1,
              "a job that raises is that job's DID NOT RUN, not the night's end: %s" % why)
    finally:
        BP.prove = real_prove


def test_the_closer_closes_revits_memory_warning_and_nothing_else():
    print("The closer posts WM_CLOSE to the memory warning alone, and prints the rest")
    warning = BP.MEMORY_WARNING
    check(warning == "Virtual Memory - High Usage",
          "it is looking for Revit's own title: %s" % warning)
    windows = [
        {"hwnd": 1, "pid": 4242, "name": warning, "owned": True},
        {"hwnd": 2, "pid": 4242, "name": KEEP_TITLE, "owned": True},
        {"hwnd": 3, "pid": 4242, "name": warning.lower(), "owned": True},
        {"hwnd": 4, "pid": 4242, "name": warning + " (2)", "owned": True},
        {"hwnd": 5, "pid": 9999, "name": warning, "owned": True},
        {"hwnd": 6, "pid": 4242, "owned": False,
         "name": "Autodesk Revit 2020 - [Materials IA Test - Floor Plan: Level 1]"},
        {"hwnd": 7, "pid": 4242, "name": "", "owned": True},
    ]
    posted = []
    lines = BP.close_memory_warning([4242], windows=windows,
                                    post=lambda hwnd: posted.append(hwnd) or True)
    check(posted == [1], "WM_CLOSE went to the exact warning on Revit 4242 alone: %s" % posted)
    check(any(line.startswith("closed") and warning in line for line in lines),
          "and it says so")
    for name in (KEEP_TITLE, warning.lower(), warning + " (2)"):
        check(any("left as it is" in line and '"%s"' % name in line for line in lines),
              "%s is left open and printed for a person" % name)
    check(not any("9999" in line for line in lines),
          "another process's windows are neither closed nor printed")

    posted = []
    lines = BP.close_memory_warning([4242], windows=windows[1:2],
                                    post=lambda hwnd: posted.append(hwnd) or True)
    check(not posted and any("nothing was closed" in line for line in lines),
          "with only the Keep or Discard box up, nothing is closed")

    if not BP.ON_WINDOWS:
        lines = BP.close_memory_warning([4242])
        check(lines and lines[0].startswith("not on Windows"),
              "off Windows it says so and closes nothing: %s" % lines)


def test_the_log_has_one_line_per_fragment():
    print("The log gets the time, the fragment, its verdict and why's first line")
    workspace = tempfile.mkdtemp(prefix="heron-batch-log-")
    try:
        log = os.path.join(workspace, "logs", "batch-prove.log")
        BP.append_log(log, BP.fragment_line(
            "set-railing-type", BP.DID_NOT_RUN, LEASE_HELD, tries=3),
            when="2026-10-23 03:14:07")
        BP.append_log(log, BP.fragment_line(
            "read-room-tags", BP.PASS, u"positive: names R�om 1"),
            when="2026-10-23 03:15:00")
        lines = io.open(log, encoding="utf-8").read().splitlines()
        check(lines[0] == ("2026-10-23 03:14:07  set-railing-type  DID NOT RUN  "
                           "Could not identify the active model:  (after 3 tries)"),
              "one line, the first line of why, and the tries: %s" % lines[0])
        check(len(lines) == 2 and u"�" in lines[1] and "(after" not in lines[1],
              "a U+FFFD from Revit is written, not a crash; one try says nothing")
    finally:
        shutil.rmtree(workspace)


def test_an_unattended_batch_logs_reports_and_hands_back_each_fragment():
    print("An unattended batch: a line and a report after each fragment, models handed back")
    library = {"night-one": FakeFragment("night-one", [{"name": "found", "role": "result"}]),
               "night-two": FakeFragment("night-two", [{"name": "found", "role": "result"}]),
               "night-done": FakeFragment("night-done", [], status="PROVEN")}
    workspace = tempfile.mkdtemp(prefix="heron-batch-night-")
    real = (BP.prove, BP.HF.load_all, BP.free_models, BP.HELD_WAIT)
    freed, seen = [], []
    try:
        job_file = os.path.join(workspace, "night.yaml")
        with io.open(job_file, "w", encoding="utf-8") as fh:
            fh.write(u"defaults:\n  set: {a: 1}\n  negative-set: {a: 2}\n"
                     u"jobs:\n  - fragment: night-one\n  - fragment: night-done\n"
                     u"  - fragment: night-two\n")
        answers = {"night-one": [(LEASE_HELD, BP.DID_NOT_RUN, "Could not identify the active model:"),
                                 ("", BP.PASS, "positive: found 3")],
                   "night-two": [("", BP.POSITIVE_EMPTY, "found 0")]}

        def fake(job, lib, env, args, transcript=None):
            said, verdict, why = answers[job["fragment"]].pop(0)
            if transcript is not None and said:
                transcript.append(said)
            report = os.path.join(workspace, "findings.json")
            if os.path.isfile(report):
                seen.append(len(json.loads(io.open(report, encoding="utf-8").read())["results"]))
            return verdict, why

        BP.prove = fake
        BP.HF.load_all = lambda *a, **k: (dict(library), [])
        BP.free_models = lambda conversation, session=None: freed.append(conversation)
        BP.HELD_WAIT = 0
        log = os.path.join(workspace, "night.log")
        report = os.path.join(workspace, "findings.json")
        code = BP.main([job_file, "--unattended", "--log", log, "--report", report])

        lines = io.open(log, encoding="utf-8").read().splitlines()
        check(code == 0, "the batch exits 0 (%s)" % code)
        check(len(lines) == 5 and "3 job(s), unattended" in lines[0]
              and lines[-1].split("  ", 1)[1].startswith("finished:"),
              "the log opens, has one line per fragment, and closes: %d line(s)" % len(lines))
        check("night-one  PASS" in lines[1] and "(after 2 tries)" in lines[1],
              "a fragment that waited once says so: %s" % lines[1])
        check("night-done  ALREADY" in lines[2],
              "a fragment never sent still has its line: %s" % lines[2])
        check(len(freed) == 3 and len(set(freed)) == 1,
              "models handed back after each sent fragment and at the end: %d" % len(freed))
        check(seen == [2],
              "the report held the first two fragments before the third ran: %s" % seen)
        findings = json.loads(io.open(report, encoding="utf-8").read())
        check(findings["finished"] and findings["unattended"]
              and [r["verdict"] for r in findings["results"]]
              == [BP.PASS, BP.ALREADY, BP.POSITIVE_EMPTY],
              "and the last report is the whole night, marked finished")
    finally:
        BP.prove, BP.HF.load_all, BP.free_models, BP.HELD_WAIT = real
        shutil.rmtree(workspace)


def test_every_marker_is_still_what_its_source_prints():
    print("Every marker --unattended reads is still in the file that prints it")
    sources = {
        "in use by another chat": [
            os.path.join(ROOT, "platform", "Heron.Core", "HeronLease.cs"),
            os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronModelHold.cs")],
        "session_in_use": [os.path.join(ROOT, "revit", "Heron.Bridge", "BridgeServer.cs")],
        "model_in_use": [os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitOperations.cs")],
        "Could not identify the active model": [
            os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py")],
        "revit_busy": [os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitDispatcher.cs")],
        "Revit is busy": [os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitDispatcher.cs")],
        KEEP_TITLE: [os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronKeepQuestion.cs")],
    }
    markers = set(BP.HELD_ELSEWHERE) | set(BP.REVIT_BUSY)
    check(markers <= set(sources), "every marker has a source to check against")
    for marker, paths in sorted(sources.items()):
        for path in paths:
            text = io.open(path, encoding="utf-8").read()
            check(marker in text, "%r is printed by %s" % (marker, os.path.basename(path)))
    for sentence, marker in ((LEASE_HELD, "in use by another chat"),
                             (MODEL_HELD, "in use by another chat"), (BUSY, "Revit is busy")):
        check(marker in sentence, "the test's own copy carries %r" % marker)


def main():
    for test in (test_unreadable_is_not_evidence_of_work,
                 test_a_flag_that_flips_is_evidence_and_one_that_does_not_is_not,
                 test_a_sentence_result_is_read_as_found_or_nothing,
                 test_counts_and_ids_read_as_they_did,
                 test_a_work_counter_is_not_a_finding,
                 test_a_working_positive_passes_and_a_full_one_is_named,
                 test_expect_beats_the_patterns,
                 test_the_negative_is_still_judged,
                 test_the_positive_is_reported_first,
                 test_no_reply_is_reported_as_a_timeout,
                 test_every_proven_fragment_in_the_library_is_refused,
                 test_a_job_with_no_negative_arrangement_is_refused,
                 test_a_negative_that_would_hand_the_chain_the_wrong_values_is_refused,
                 test_the_job_file_is_checked_against_the_contract,
                 test_an_admin_or_publish_fragment_is_never_sent_by_a_batch,
                 test_defaults_merge_key_by_key,
                 test_the_command_line_is_one_the_client_actually_parses,
                 test_a_value_with_a_space_is_quoted_when_printed,
                 test_unattended_asks_again_only_when_revit_was_never_reached,
                 test_unattended_waits_and_stops_at_the_limit,
                 test_the_closer_closes_revits_memory_warning_and_nothing_else,
                 test_the_log_has_one_line_per_fragment,
                 test_an_unattended_batch_logs_reports_and_hands_back_each_fragment,
                 test_every_marker_is_still_what_its_source_prints):
        test()
        print("")

    shutil.rmtree(CARDS, ignore_errors=True)

    if FAILURES:
        print("%d failure(s):" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("The batch runner judges both halves, and refuses finished work.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
