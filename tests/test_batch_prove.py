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


class FakeFragment(object):
    """Only what the judge reads: a slug, a status, and a contract."""

    def __init__(self, slug, provides, status="DRAFT"):
        self.slug = slug
        self.status = status
        self._provides = provides

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


def main():
    for test in (test_unreadable_is_not_evidence_of_work,
                 test_a_flag_that_flips_is_evidence_and_one_that_does_not_is_not,
                 test_a_work_counter_is_not_a_finding,
                 test_a_working_positive_passes_and_a_full_one_is_named,
                 test_expect_beats_the_patterns,
                 test_the_negative_is_still_judged,
                 test_the_positive_is_reported_first,
                 test_no_reply_is_reported_as_a_timeout,
                 test_every_proven_fragment_in_the_library_is_refused,
                 test_a_job_with_no_negative_arrangement_is_refused,
                 test_the_job_file_is_checked_against_the_contract,
                 test_defaults_merge_key_by_key,
                 test_the_command_line_is_one_the_client_actually_parses,
                 test_a_value_with_a_space_is_quoted_when_printed):
        test()
        print("")

    if FAILURES:
        print("%d failure(s):" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("The batch runner judges both halves, and refuses finished work.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
