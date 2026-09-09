#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Fragment Validation Agent, checked without Revit and without a bridge.

    python tests/test_validate_agent.py

WHAT THESE ACTUALLY TEST, AND IT IS THE POINT OF THE SPLIT. Running fragments
against a model is the client's job and needs Windows, Revit and a lease. JUDGING
what came back is this agent's job, and every one of those judgements is
testable on a machine with no Revit on it - which is where it was written.

The four that matter most are the four refusals. A safety rule nobody tested is
a comment.
"""

import io
import json
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as HF                                       # noqa: E402
import heron_validate as HV                                       # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


# ---------------------------------------------------------------------------

SAMPLE = """heron-agent: HERON-REVIT-LVL-027
heron-step: 15
heron-status: DRAFT
heron-since: 0.1.0
heron-layer: brain

id: FRG-TST-001
semantic-identity: "a sample used only by the tests"
kind: filter
domain: revit.levels
capability: SAMPLE_CAPABILITY
version: 1
source: OFFICIAL
risk: READ

purpose: >
  A sample.

contract:
  needs:
    - name: doc
      type: Document
  provides:
    - name: elements
      type: IList<Element>

revit: ["2020", "2021", "2022", "2023", "2024", "2025", "2026", "2027"]
runtime: [net472, net48, net8.0-windows, net10.0-windows]
tests: tests/

# ROUTING - this comment is load-bearing and must survive every write.
#
#   "sample this"  -> here
#   "not this"     -> somewhere else

utterances:
  - sample this
"""


def make_fragment(folder, text=SAMPLE, source="// nothing\n"):
    os.makedirs(os.path.join(folder, "impl", "any"))
    with io.open(os.path.join(folder, "fragment.yaml"), "w", encoding="utf-8") as fh:
        fh.write(text)
    with io.open(os.path.join(folder, "impl", "any", "fragment.cs"), "w",
                 encoding="utf-8") as fh:
        fh.write(source)
    return HF.load(folder)


def test_reachability():
    print("What can be reached, and what honestly cannot")
    library = HV.load_library()
    table = HV.providers(library)

    routes = {}
    for frag in library:
        route, _ = HV.route_for(frag, table)
        routes[route] = routes.get(route, 0) + 1

    check(sum(routes.values()) == len(library),
          "every fragment gets exactly one route")
    check(routes.get(HV.WRITE_PATH, 0) > 0,
          "fragments that can change the model are routed out of scope")
    check(routes.get(HV.NEEDS_VALUES, 0) > 0,
          "fragments waiting on a value from the request are named, not run")

    # The two shapes the whole plan rests on, checked against real fragments
    # rather than against the counts.
    levels = [f for f in library if f.slug == "list-levels"][0]
    check(HV.route_for(levels, table)[0] in (HV.STANDALONE, HV.PROVED, HV.RE_PROVE),
          "list-levels needs nothing but the document")

    counters = [f for f in library if f.slug == "count-elements"]
    if counters:
        route, _ = HV.route_for(counters[0], table)
        # PROVED and RE_PROVE are accepted for the same reason list-levels
        # accepts them above: `route_for` answers "what is worth doing to this
        # fragment NEXT", and a fragment carrying a proof is answered PROVED
        # before its inputs are ever considered. Asserting FROM_SELECTION here
        # was really asserting that count-elements is still unproven, which
        # stopped being true on 2026-09-07 (D-53) and turned a passing suite
        # red on somebody else's good news.
        check(route in (HV.FROM_SELECTION, HV.PROVED, HV.RE_PROVE),
              "count-elements takes one set of elements, so a selection can "
              "feed it - unless it is already proved")


def test_plan_is_ordered_and_honest():
    print("The plan")
    library = HV.load_library()
    entries = HV.build_plan(library)
    check(entries, "the plan is not empty")

    positions = [HV.PRIORITY.index(e["route"]) for e in entries]
    check(positions == sorted(positions), "entries come out in priority order")

    check(all(e["negative_case"] for e in entries),
          "every entry says how its negative case would be arranged")
    check(not any(e["route"] in (HV.WRITE_PATH, HV.PROVED) for e in entries),
          "nothing that writes, and nothing already proven, is scheduled")


def test_a_draft_never_signs_itself():
    print("A draft is evidence, not a signature")
    workspace = tempfile.mkdtemp()
    try:
        frag = make_fragment(os.path.join(workspace, "sample"))
        record = {
            "run_record": "test",
            "date": "2026-09-06",
            "model": "a test model",
            "phases": [
                {"phase": "positive", "ok": True, "provides": {"count": 5}},
                {"phase": "negative", "ok": True, "provides": {"count": 0}},
            ],
        }
        draft = HV.draft_from_record(frag, record)

        check(draft["proof-draft"]["by"] == "",
              "the drafted proof is UNSIGNED - `by` is empty on purpose")
        check(HF.PROOF_REQUIRED and "by" in HF.PROOF_REQUIRED,
              "and `by` is one of the fields heron_fragment requires")
        check("5" in draft["proof-draft"]["positive_case"],
              "the positive case quotes what actually came back")
        check("0" in draft["proof-draft"]["negative_case"],
              "the negative case quotes what actually came back")
        check(HV.NOT_ESTABLISHED in draft["proof-draft"]["second_route"],
              "a second route that was not run is NOT ESTABLISHED, never invented")
        check(any("second route" in g for g in draft["gaps"]),
              "and the gap is listed for a person to see")
    finally:
        shutil.rmtree(workspace)


def test_a_negative_case_that_is_not_empty_is_a_finding():
    print("A negative case that comes back full")
    workspace = tempfile.mkdtemp()
    try:
        frag = make_fragment(os.path.join(workspace, "sample"))
        record = {
            "date": "2026-09-06", "model": "m",
            "phases": [
                {"phase": "positive", "ok": True, "provides": {"count": 5}},
                # The defect D-30 exists for: it should have returned nothing.
                {"phase": "negative", "ok": True, "provides": {"count": 5}},
            ],
        }
        draft = HV.draft_from_record(frag, record)
        check("WARNING" in draft["proof-draft"]["negative_case"],
              "a negative case returning content is flagged in the draft itself")
        check(any("returned content" in g for g in draft["gaps"]),
              "and recorded as a gap rather than read as a pass")

        # The conservative reading: an unrecognised shape must NOT read as empty.
        check(not HV.looks_empty({"provides": {"names": ["a"]}}),
              "a non-empty list does not read as empty")
        check(HV.looks_empty({"provides": {"count": 0, "found": []}}),
              "zeros and empty lists do read as empty")
        check(not HV.looks_empty({"provides": {}}),
              "a phase that recorded nothing does not count as an empty answer")
    finally:
        shutil.rmtree(workspace)


def test_a_missing_phase_is_never_filled_in():
    print("A run that did not happen")
    workspace = tempfile.mkdtemp()
    try:
        frag = make_fragment(os.path.join(workspace, "sample"))
        draft = HV.draft_from_record(frag, {"date": "d", "model": "m", "phases": []})
        for leg in ("positive_case", "negative_case", "second_route"):
            check(HV.NOT_ESTABLISHED in draft["proof-draft"][leg],
                  "%s says NOT ESTABLISHED when nothing ran" % leg)
        check(len(draft["gaps"]) == 3, "all three gaps are reported")
    finally:
        shutil.rmtree(workspace)


def test_the_write_refusals():
    print("The refusals")
    workspace = tempfile.mkdtemp()
    try:
        # 1. A draft may not be written into the fragment library.
        original = HV.DRAFTS_DIR
        HV.DRAFTS_DIR = os.path.join(HV.ROOT, "brain", "fragments", "sneaky")
        refused = False
        try:
            HV.write_draft("sneaky", {})
        except ValueError:
            refused = True
        finally:
            HV.DRAFTS_DIR = original
        check(refused, "writing a draft inside brain/fragments/ is refused")
        check(not os.path.exists(os.path.join(HV.ROOT, "brain", "fragments", "sneaky")),
              "and nothing was created there")

        # 2. accept needs a person's name.
        code, message = HV.accept("anything", "")
        check(code != 0 and "name" in message, "accept without a name is refused")

        # 3. An incomplete draft cannot be accepted.
        frag = make_fragment(os.path.join(workspace, "sample"))
        HV.DRAFTS_DIR = os.path.join(workspace, "drafts")
        try:
            HV.write_draft("sample", {"fragment": "sample", "proof-draft": {
                "date": "2026-09-06", "by": "", "model": "m",
                "positive_case": "it returned 5",
                "negative_case": "",          # the leg D-30 exists for
                "second_route": "none",
                "fingerprint": frag.fingerprint()}})
            code, message = HV.accept("sample", "A Person", library=[frag])
            check(code != 0 and "negative_case" in message,
                  "a draft missing its negative case cannot be accepted")
        finally:
            HV.DRAFTS_DIR = original
    finally:
        shutil.rmtree(workspace)


def test_accept_records_a_proof_and_leaves_the_status_alone():
    print("What accept does, and what it must never do")
    workspace = tempfile.mkdtemp()
    original = HV.DRAFTS_DIR
    try:
        folder = os.path.join(workspace, "sample")
        frag = make_fragment(folder)
        HV.DRAFTS_DIR = os.path.join(workspace, "drafts")

        HV.write_draft("sample", {"fragment": "sample", "proof-draft": {
            "date": "2026-09-06", "by": "", "model": "a test model",
            "positive_case": "returned count 5",
            "negative_case": "returned count 0 on an empty selection",
            "second_route": "count_elements agreed",
            "fingerprint": frag.fingerprint()}})

        code, message = HV.accept("sample", "A Person", library=[frag])
        check(code == 0, "a complete draft is accepted: %s" % message)

        after = HF.load(folder)
        check(after.status == "DRAFT",
              "THE STATUS IS UNTOUCHED - accept never promotes")
        check(after.proof is not None, "the proof is recorded")
        check(after.proof.get("by") == "A Person",
              "signed by the person who typed their name, not by the agent")
        check(not HF.proof_problems(after),
              "and the recorded proof satisfies heron_fragment's own rules")

        text = io.open(os.path.join(folder, "fragment.yaml"),
                       encoding="utf-8").read()
        check("# ROUTING - this comment is load-bearing" in text,
              "THE ROUTING COMMENTS SURVIVED - the write is surgical, not a dump")
        check('"not this"' in text, "every routing line survived, not just the header")
        check("utterances:" in text and "sample this" in text,
              "and nothing after the proof was lost")

        check(not os.path.exists(HV.draft_path("sample")),
              "the draft is removed once accepted, so one fact has one home again")
    finally:
        HV.DRAFTS_DIR = original
        shutil.rmtree(workspace)


def test_an_unsigned_draft_cannot_pass_validation():
    print("The last line of defence")
    workspace = tempfile.mkdtemp()
    try:
        folder = os.path.join(workspace, "sample")
        frag = make_fragment(folder)
        # Somebody pastes a draft straight in, and marks it PROVEN by hand.
        text = io.open(os.path.join(folder, "fragment.yaml"), encoding="utf-8").read()
        text = text.replace("heron-status: DRAFT", "heron-status: PROVEN")
        spliced = HV.splice_proof(text, {
            "date": "2026-09-06", "by": "", "model": "m",
            "positive_case": "returned 5",
            "negative_case": "returned 0 when it should have",
            "second_route": "something",
            "fingerprint": frag.fingerprint()})
        io.open(os.path.join(folder, "fragment.yaml"), "w",
                encoding="utf-8").write(spliced)

        problems = HF.proof_problems(HF.load(folder))
        check(any("by" in p for p in problems),
              "an unsigned proof at PROVEN fails validation loudly")
    finally:
        shutil.rmtree(workspace)


def test_restamp_refuses_a_genuinely_stale_proof():
    print("Re-stamping, and the one it must refuse")
    workspace = tempfile.mkdtemp()
    try:
        folder = os.path.join(workspace, "sample")
        make_fragment(folder)
        text = io.open(os.path.join(folder, "fragment.yaml"), encoding="utf-8").read()
        text = text.replace("heron-status: DRAFT", "heron-status: PROVEN")
        text = HV.splice_proof(text, {
            "date": "2026-09-06", "by": "A Person", "model": "m",
            "positive_case": "p", "negative_case": "n", "second_route": "s",
            "fingerprint": "0000000000000000"})
        io.open(os.path.join(folder, "fragment.yaml"), "w",
                encoding="utf-8").write(text)
        frag = HF.load(folder)

        # This fragment is not in git at all, so "did the code move after the
        # proof?" cannot be answered - and unknown must count as YES.
        check(HV.implementation_changed_after(frag),
              "a fragment git cannot speak for is treated as genuinely stale")
        rows, refused = HV.restamp(library=[frag], apply_changes=False)
        check(not rows and refused == ["sample"],
              "so restamp refuses it rather than making it look fresh")

        # And a proof with no date at all is refused for the same reason.
        frag.data["proof"]["date"] = ""
        check(HV.implementation_changed_after(frag),
              "a proof with no date cannot be shown to still hold")
    finally:
        shutil.rmtree(workspace)

    # SAME DAY COUNTS AS AFTER, which is the hole this guard had until
    # 2026-09-09. A proof carries a date and no time, so a change committed
    # hours after its own proof used to read as "did not move" - which is
    # exactly how set-view-section-box kept a PROVEN status against code that
    # had changed. Derived from git rather than hard-coded, so it cannot rot.
    tracked = "brain/heron_fragment.py"
    same_day = subprocess.run(
        ["git", "log", "-1", "--format=%ad", "--date=short", "--", tracked],
        cwd=HV.ROOT, capture_output=True, text=True).stdout.strip()
    if same_day:
        class SameDay(object):
            proof = {"date": same_day}
            def proof_files(self):
                return [tracked]
        check(HV.implementation_changed_after(SameDay()),
              "a change committed the SAME DAY as the proof counts as after "
              "(%s), because a date cannot be ordered against an hour"
              % same_day)


def test_the_fingerprint_is_about_content_only():
    print("The fingerprint")
    workspace = tempfile.mkdtemp()
    try:
        lf = make_fragment(os.path.join(workspace, "lf"), source="a\nb\n")
        crlf = make_fragment(os.path.join(workspace, "crlf"), source="a\r\nb\r\n")
        # The folders differ, so compare the content hash the way proof_files
        # feeds it: same relative shape, different line endings only.
        check(lf.fingerprint() is not None, "a fingerprint is produced")

        import hashlib

        def hashed(frag, sep, ending):
            digest = hashlib.sha256()
            for path in frag.proof_files():
                digest.update(path.replace("\\", "/").replace("/", sep).encode())
                data = io.open(os.path.join(HF.ROOT, path), "rb").read()
                digest.update(data if ending == "raw" else data.replace(b"\r\n", b"\n"))
            return digest.hexdigest()[:16]

        check(hashed(lf, "/", "lf") == lf.fingerprint(),
              "the recipe is normalised separators and normalised line endings")
        check(hashed(crlf, "/", "lf") == crlf.fingerprint(),
              "and a CRLF working copy produces the same number as an LF one")

        changed = make_fragment(os.path.join(workspace, "changed"), source="a\nc\n")
        check(changed.fingerprint() != lf.fingerprint(),
              "REAL content changes still change the fingerprint")
    finally:
        shutil.rmtree(workspace)


def test_audit_reading_says_what_it_cannot_say():
    print("The audit log")
    workspace = tempfile.mkdtemp()
    try:
        path = os.path.join(workspace, "audit-202609.jsonl")
        with io.open(path, "w", encoding="utf-8") as fh:
            fh.write(json.dumps({"op": "run_fragment_read", "ok": True}) + "\n")
            fh.write(json.dumps({"op": "run_fragment_read", "ok": False,
                                 "error": "needs_request_values"}) + "\n")
            fh.write(json.dumps({"op": "count_elements", "ok": True}) + "\n")
            fh.write("{truncated\n")        # an append-only log, read mid-write

        summary = HV.summarise_audit(HV.read_audit([path]))
        check(summary["entries"] == 3, "a truncated last line costs one line, not the file")
        check(summary["ok"] == 2 and summary["failed"] == 1, "outcomes are counted")
        check(summary["by_error"].get("needs_request_values") == 1,
              "failures are grouped by what Revit actually said")
        check(summary["attributable_to_a_fragment"] == 0,
              "AND IT SAYS PLAINLY that no failure can be tied to a fragment - "
              "the add-in records the operation, not which fragment ran")
    finally:
        shutil.rmtree(workspace)


def main():
    for test in (test_reachability,
                 test_plan_is_ordered_and_honest,
                 test_a_draft_never_signs_itself,
                 test_a_negative_case_that_is_not_empty_is_a_finding,
                 test_a_missing_phase_is_never_filled_in,
                 test_the_write_refusals,
                 test_accept_records_a_proof_and_leaves_the_status_alone,
                 test_an_unsigned_draft_cannot_pass_validation,
                 test_restamp_refuses_a_genuinely_stale_proof,
                 test_the_fingerprint_is_about_content_only,
                 test_audit_reading_says_what_it_cannot_say):
        test()
        print("")

    if FAILURES:
        print("%d failure(s):" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("The Fragment Validation Agent behaves, and refuses what it must.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
