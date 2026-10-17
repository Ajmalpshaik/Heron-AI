#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
D-131 - a proof on one more Revit release, and a second route, on a fragment
already PROVEN, checked without Revit.

    python tests/test_release_proofs.py

WHAT IT HOLDS
  1. An entry of `proof.releases` is held to the evidence a proof is, by
     heron_fragment, and a malformed one is named.
  2. `accept --release` writes ONE entry and nothing else: the signed proof,
     every other key and the routing comments read back exactly as they were.
  3. A release already recorded by evidence that still stands is refused -
     the proof's own release and a fresh entry alike. A STALE entry is
     replaced, the rule `accept` keeps for a stale proof.
  4. A stale fingerprint is refused - in the draft, and under the proof.
  5. A run on another Revit than `--release` is refused.
  6. `heron-status` is never written, and a write that moved anything but
     its one key is put back.
  7. `accept --second-route` fills a NOT ESTABLISHED line, signed, from a
     recorded run, and never overwrites a route already written.
  8. The matrix turns a cell PROVEN only on a recorded release - from the
     proof or a fresh entry, never from `revit:`.
  9. `batch-prove --release` runs only PROVEN fragments not yet proved on
     that release, writes `<slug>@<year>` records, and calls a run on another
     Revit WRONG RELEASE.

Every fragment here is written into a temporary folder. Nothing touches the
library, and nothing here proves any fragment on any release.
"""

import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_fragment as HF                                       # noqa: E402
import heron_matrix as MX                                         # noqa: E402
import heron_validate as HV                                       # noqa: E402
import heron_bridge_client as BRIDGE                              # noqa: E402

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


# ---------------------------------------------------------------------------
# A PROVEN fragment in a temporary checkout, and its run records
# ---------------------------------------------------------------------------

CARD = """heron-agent: HERON-REVIT-LVL-027
heron-step: 15
heron-status: %(status)s
heron-since: 0.1.0
heron-layer: brain

id: FRG-TST-131
semantic-identity: "a sample used only by the D-131 tests"
kind: filter
domain: revit.levels
capability: SAMPLE_RELEASE_CAPABILITY
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
      role: result

proof:
  date: '2026-09-10'
  by: A Person
  model: a test model (100 elements), Revit 2024, session 1
  positive_case: returned elements 5 on a selection of five walls
  negative_case: returned elements 0 on a selection holding no walls at all
  second_route: NOT ESTABLISHED - no second route was run. D-30 asks for one 'where
    one exists'; whether one exists here has not been settled
  fingerprint: "%(fingerprint)s"
%(releases)s
revit: %(revit)s
runtime: [net472, net48, net8.0-windows, net10.0-windows]
tests: tests/

# ROUTING - this comment is load-bearing and must survive every write.
#
#   "sample this"  -> here

utterances:
  - sample this
"""

EIGHT = '["2020", "2021", "2022", "2023", "2024", "2025", "2026", "2027"]'


def write_card(folder, status="PROVEN", fingerprint=None, releases="",
               revit=EIGHT):
    """The card, with the proof stamped against the code beside it."""
    impl = os.path.join(folder, "impl", "any")
    if not os.path.isdir(impl):
        os.makedirs(impl)
        with io.open(os.path.join(impl, "fragment.cs"), "w", encoding="utf-8") as fh:
            fh.write(u"// the sample's code\n")
    values = {"status": status, "fingerprint": fingerprint or "pending",
              "releases": releases, "revit": revit}
    path = os.path.join(folder, "fragment.yaml")
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(CARD % values)
    if fingerprint is None:
        values["fingerprint"] = HF.load(folder).fingerprint()
        values["releases"] = releases.replace("CURRENT", values["fingerprint"])
        with io.open(path, "w", encoding="utf-8") as fh:
            fh.write(CARD % values)
    return HF.load(folder)


def entry_text(release, fingerprint):
    return ("  releases:\n"
            "  - release: '%s'\n"
            "    date: '2026-10-01'\n"
            "    by: A Person\n"
            "    model: a test model, Revit %s, session 2\n"
            "    positive_case: returned elements 5 on five walls\n"
            "    negative_case: returned elements 0 on a selection with no walls\n"
            "    fingerprint: '%s'\n" % (release, release, fingerprint))


def write_record(key, ran_on, positive=None, negative=None, cross=None):
    """A run record where `accept` re-reads it, named as the client names it."""
    runs = os.path.join(HV.DRAFTS_DIR, "runs")
    if not os.path.isdir(runs):
        os.makedirs(runs)
    phases = []
    if positive is not None:
        phases.append({"phase": "positive", "ok": True, "provides": positive,
                       "document": "a test model"})
    if negative is not None:
        phases.append({"phase": "negative", "ok": True, "provides": negative,
                       "document": "a test model"})
    if cross is not None:
        phases.append({"phase": "second_route", "ok": True, "provides": cross,
                       "document": "a test model",
                       "arranged": "count_elements counted the whole active "
                                   "document by a different route"})
    path = HV.run_record_path(key)
    record = {"run_record": path, "fragment": key.split("@")[0],
              "date": "2026-10-20",
              "model": "a test model (100 elements), Revit %s, session 9" % ran_on,
              "phases": phases}
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(json.dumps(record, indent=1))
    return record


def release_run(frag, release, ran_on=None):
    """Run and draft one release, the way batch-prove leaves it for a person."""
    key = HV.proof_key(frag.slug, release=release)
    record = write_record(key, ran_on or release, positive={"elements": 5},
                          negative={"elements": 0})
    HV.write_draft(key, HV.release_draft(frag, record, release))
    return key


def text_of(frag):
    with io.open(os.path.join(frag.folder, "fragment.yaml"), encoding="utf-8") as fh:
        return fh.read()


def proof_lines(text):
    start, end = HV._block_bounds(text, "proof")
    return text.split("\n")[start:end]


class Workspace(object):
    """A temporary checkout with its own drafts folder, put back on exit."""

    def __enter__(self):
        self.root = tempfile.mkdtemp(prefix="heron-d131-")
        self.drafts = HV.DRAFTS_DIR
        HV.DRAFTS_DIR = os.path.join(self.root, "drafts")
        return self

    def fragment(self, slug="sample", **card):
        return write_card(os.path.join(self.root, "brain", "fragments", slug), **card)

    def __exit__(self, *_):
        HV.DRAFTS_DIR = self.drafts
        shutil.rmtree(self.root, ignore_errors=True)


# ---------------------------------------------------------------------------
# 1. An entry is held to a proof's evidence
# ---------------------------------------------------------------------------

def test_an_entry_is_validated_like_a_proof():
    print("1. An entry of proof.releases is held to the evidence a proof is")
    with Workspace() as ws:
        frag = ws.fragment(releases=entry_text("2025", "CURRENT"))
        check(not HF.proof_problems(frag),
              "a complete entry on a claimed release raises nothing: %s"
              % HF.proof_problems(frag))
        check(getattr(HF, "RELEASE_REQUIRED", None) and
              "release" in HF.RELEASE_REQUIRED and "fingerprint" in HF.RELEASE_REQUIRED,
              "the entry's fields name the release and the code it ran against")

        broken = ws.fragment(slug="no-negative", releases=entry_text("2025", "CURRENT")
                             .replace("    negative_case: returned elements 0 on a "
                                      "selection with no walls\n", ""))
        check(any("negative_case" in p for p in HF.proof_problems(broken)),
              "an entry with no negative case is named")

        unclaimed = ws.fragment(slug="unclaimed", releases=entry_text("2025", "CURRENT"),
                                revit='["2023", "2024"]')
        check(any("does not claim" in p for p in HF.proof_problems(unclaimed)),
              "an entry on a release the fragment does not claim is named")

        twice = ws.fragment(slug="twice", releases=entry_text("2025", "CURRENT") +
                            entry_text("2025", "CURRENT").replace("  releases:\n", ""))
        check(any("twice" in p for p in HF.proof_problems(twice)),
              "two entries for one release are named")

        flat = ws.fragment(slug="flat", releases="  releases: '2025'\n")
        check(any("must be a list" in p for p in HF.proof_problems(flat)),
              "a releases value that is not a list is named")

        stale = ws.fragment(slug="stale-entry", releases=entry_text("2025", "0000000000000000"))
        check(not HF.proof_problems(stale),
              "a STALE entry is not a validation problem - the matrix stops counting it")


# ---------------------------------------------------------------------------
# 2, 3, 4, 5, 6 - accept --release
# ---------------------------------------------------------------------------

def test_a_release_entry_is_accepted():
    print("2. accept --release writes one entry and nothing else")
    with Workspace() as ws:
        frag = ws.fragment()
        before = text_of(frag)
        key = release_run(frag, "2025")
        check(key == "sample@2025", "a release draft is named <slug>@<year>")

        code, message = HV.accept_release("sample", "2025", "A Reviewer", library=[frag])
        check(code == 0, "a complete draft on a new release is accepted: %s" % message)

        after = HF.load(frag.folder)
        text = text_of(after)
        check(after.status == "PROVEN", "the status is untouched")
        rest = dict((k, v) for k, v in after.proof.items() if k != "releases")
        check(rest == frag.proof, "every field of the signed proof reads back the same")
        check(proof_lines(text)[:len(proof_lines(before)) - 1]
              == proof_lines(before)[:-1],
              "and every line of it is where it was - the entry is added below")
        check("# ROUTING - this comment is load-bearing" in text,
              "the routing comment survives")
        outside = [line for line in before.split("\n")
                   if line not in proof_lines(before)]
        check(all(line in text.split("\n") for line in outside),
              "no line outside the proof moved")
        rows = after.proof.get("releases") or []
        check(len(rows) == 1 and rows[0].get("release") == "2025"
              and rows[0].get("by") == "A Reviewer"
              and rows[0].get("fingerprint") == frag.fingerprint(),
              "one entry, on 2025, signed by the person, against the current code")
        check(list(rows[0]) == list(HF.RELEASE_REQUIRED) if rows else False,
              "its fields in the order heron_fragment requires them")
        check(not HF.proof_problems(after), "the fragment still validates: %s"
              % HF.proof_problems(after))
        check(not os.path.isfile(HV.draft_path(key)), "the draft is consumed")

        code, message = HV.accept_release("sample", "2026", "A Reviewer", library=[after])
        check(code != 0 and "no draft" in message,
              "a release with no draft is refused, naming where the record goes")
        release_run(after, "2026")
        code, message = HV.accept_release("sample", "2026", "A Reviewer", library=[after])
        two = HF.load(frag.folder)
        check(code == 0 and [r["release"] for r in two.proof["releases"]] == ["2025", "2026"],
              "a second release is added after the first: %s" % message)
        check(dict((k, v) for k, v in two.proof.items() if k != "releases") == frag.proof,
              "and the signed proof is still untouched")


def test_a_release_already_recorded_is_refused():
    print("3. A release already recorded is refused")
    with Workspace() as ws:
        frag = ws.fragment(releases=entry_text("2025", "CURRENT"))
        before = text_of(frag)

        release_run(frag, "2025")
        code, message = HV.accept_release("sample", "2025", "A Reviewer", library=[frag])
        check(code != 0 and "already recorded" in message,
              "a release with a fresh entry is refused: %s" % message)

        release_run(frag, "2024")
        code, message = HV.accept_release("sample", "2024", "A Reviewer", library=[frag])
        check(code != 0 and "already recorded" in message and "its proof" in message,
              "the release the proof itself names is refused: %s" % message)
        check(text_of(frag) == before, "and the file is byte-for-byte as it was")

        code, message = HV.accept_release("sample", "25", "A Reviewer", library=[frag])
        check(code != 0 and "year" in message, "a release that is not a year is refused")
        code, message = HV.accept_release("sample", "2025", "", library=[frag])
        check(code != 0 and "name" in message, "accept --release needs a person's name")

    with Workspace() as ws:
        frag = ws.fragment(revit='["2023", "2024"]')
        release_run(frag, "2025")
        code, message = HV.accept_release("sample", "2025", "A Reviewer", library=[frag])
        check(code != 0 and "does not claim" in message,
              "a release the fragment does not claim is refused: %s" % message)

    with Workspace() as ws:
        frag = ws.fragment(status="DRAFT")
        release_run(frag, "2025")
        code, message = HV.accept_release("sample", "2025", "A Reviewer", library=[frag])
        check(code != 0 and "already PROVEN" in message,
              "a fragment that is not PROVEN is refused - its first proof is `accept`")


def test_a_stale_fingerprint_is_refused():
    print("4. A stale fingerprint is refused")
    with Workspace() as ws:
        frag = ws.fragment()
        before = text_of(frag)
        key = release_run(frag, "2025")
        draft = HV.read_draft(key)
        draft["proof-draft"]["fingerprint"] = "0123456789abcdef"
        HV.write_draft(key, draft)
        code, message = HV.accept_release("sample", "2025", "A Reviewer", library=[frag])
        check(code != 0 and "different code" in message,
              "a draft taken against other code is refused: %s" % message)
        check(text_of(frag) == before, "and nothing was written")

        with io.open(os.path.join(frag.folder, "impl", "any", "fragment.cs"), "w",
                     encoding="utf-8") as fh:
            fh.write(u"// the code moved\n")
        moved = HF.load(frag.folder)
        release_run(moved, "2025")
        code, message = HV.accept_release("sample", "2025", "A Reviewer", library=[moved])
        check(code != 0 and "moved under" in message,
              "an entry on top of a stale proof is refused: %s" % message)
        check(text_of(moved) == before, "and nothing was written")

    with Workspace() as ws:
        frag = ws.fragment(releases=entry_text("2025", "0000000000000000"))
        found, stale = MX.proven_releases(frag)
        check("2025" not in found and stale == ["2025"],
              "an entry against other code is recorded and not counted")
        release_run(frag, "2025")
        code, message = HV.accept_release("sample", "2025", "A Reviewer", library=[frag])
        after = HF.load(frag.folder)
        rows = after.proof.get("releases") or []
        check(code == 0 and len(rows) == 1 and rows[0]["fingerprint"] == frag.fingerprint(),
              "a STALE entry is replaced by a fresh one, never duplicated: %s" % message)


def test_a_run_on_another_release_is_refused():
    print("5. A run on another Revit than --release is refused")
    with Workspace() as ws:
        frag = ws.fragment()
        before = text_of(frag)
        key = release_run(frag, "2025", ran_on="2024")
        check(any("2024" in gap for gap in HV.read_draft(key)["gaps"]),
              "the draft itself says the run was on 2024")
        code, message = HV.accept_release("sample", "2025", "A Reviewer", library=[frag])
        check(code != 0 and "Revit 2024" in message,
              "accept refuses it, naming where it ran: %s" % message)
        check(text_of(frag) == before, "and nothing was written")


def test_heron_status_is_never_written():
    print("6. heron-status is never written, and a write that moves more is put back")
    with Workspace() as ws:
        frag = ws.fragment()
        before = text_of(frag)
        release_run(frag, "2025")

        honest = HV.splice_proof_key

        def also_promotes(text, key, value):
            return honest(text, key, value).replace("heron-status: PROVEN",
                                                    "heron-status: PRODUCTION")

        HV.splice_proof_key = also_promotes
        try:
            code, message = HV.accept_release("sample", "2025", "A Reviewer",
                                              library=[frag])
        finally:
            HV.splice_proof_key = honest
        check(code == 1 and "status moved" in message,
              "a write that moved the status is reported: %s" % message)
        check(text_of(frag) == before, "and the file is put back exactly")

        def also_edits(text, key, value):
            return honest(text, key, value).replace("returned elements 5 on a selection",
                                                    "returned elements 6 on a selection")

        HV.splice_proof_key = also_edits
        try:
            code, message = HV.accept_release("sample", "2025", "A Reviewer",
                                              library=[frag])
        finally:
            HV.splice_proof_key = honest
        check(code == 1 and "another field of the proof" in message,
              "a write that edited the signed proof is reported: %s" % message)
        check(text_of(frag) == before, "and the file is put back exactly")
        check(os.path.isfile(HV.draft_path("sample@2025")),
              "and the draft is kept, because nothing was recorded")


def test_the_main_accept_keeps_recorded_releases():
    print("A proof taken again after the code moved keeps the releases recorded")
    with Workspace() as ws:
        frag = ws.fragment(releases=entry_text("2025", "CURRENT"))
        with io.open(os.path.join(frag.folder, "impl", "any", "fragment.cs"), "w",
                     encoding="utf-8") as fh:
            fh.write(u"// the code moved\n")
        moved = HF.load(frag.folder)
        check(moved.proof_is_stale(), "the proof is stale once the code moved")

        record = write_record("sample", "2024", positive={"elements": 5},
                              negative={"elements": 0})
        HV.write_draft("sample", HV.draft_from_record(moved, record))
        code, message = HV.accept("sample", "A Reviewer", library=[moved])
        after = HF.load(frag.folder)
        check(code == 0, "the stale proof is taken again: %s" % message)
        rows = after.proof.get("releases") or []
        check([r.get("release") for r in rows] == ["2025"],
              "and the 2025 entry is still there")
        check(MX.proven_releases(after) == ({"2024": "proof"}, ["2025"]),
              "and it is not counted until 2025 is proved again on the new code")


# ---------------------------------------------------------------------------
# 7. accept --second-route
# ---------------------------------------------------------------------------

def test_a_second_route_is_written_signed_and_never_overwritten():
    print("7. accept --second-route fills a NOT ESTABLISHED line, and only that")
    with Workspace() as ws:
        frag = ws.fragment()
        before = text_of(frag)
        key = HV.proof_key("sample", second_route=True)
        check(key == "sample@second-route", "a second-route draft has its own name")

        record = write_record(key, "2024", positive={"elements": 5},
                              negative={"elements": 0}, cross={"count": 5})
        HV.write_draft(key, HV.second_route_draft(frag, record))
        code, message = HV.accept_second_route("sample", "A Reviewer", library=[frag])
        check(code == 0, "a recorded second route is accepted: %s" % message)

        after = HF.load(frag.folder)
        route = after.proof.get("second_route") or ""
        check("count_elements" in route and "count 5" in route and "elements 5" in route,
              "the line sets the route's answer beside the fragment's")
        check("Signed by A Reviewer" in route and HV.NOT_ESTABLISHED not in route,
              "and it is signed, by the person")
        check(dict((k, v) for k, v in after.proof.items() if k != "second_route")
              == dict((k, v) for k, v in frag.proof.items() if k != "second_route"),
              "every other field of the proof is untouched")
        check(after.status == "PROVEN", "the status is untouched")
        check(text_of(after).split("second_route:")[0] == before.split("second_route:")[0],
              "and every line above it is where it was")

        HV.write_draft(key, HV.second_route_draft(after, record))
        code, message = HV.accept_second_route("sample", "A Reviewer", library=[after])
        check(code != 0 and "already has a second route" in message,
              "a route already written is never overwritten: %s" % message)

    with Workspace() as ws:
        frag = ws.fragment()
        key = HV.proof_key("sample", second_route=True)
        record = write_record(key, "2024", positive={"elements": 5},
                              negative={"elements": 0})
        draft = HV.second_route_draft(frag, record)
        check(HV.NOT_ESTABLISHED in draft["proof-draft"]["second_route"],
              "a run with no second route drafts NOT ESTABLISHED, never a route")
        HV.write_draft(key, draft)
        code, message = HV.accept_second_route("sample", "A Reviewer", library=[frag])
        check(code != 0 and "cannot be signed" in message,
              "and it cannot be signed: %s" % message)

    with Workspace() as ws:
        frag = ws.fragment(status="DRAFT")
        code, message = HV.accept_second_route("sample", "A Reviewer", library=[frag])
        check(code != 0 and "already PROVEN" in message,
              "a fragment that is not PROVEN is refused")


# ---------------------------------------------------------------------------
# 8. The matrix
# ---------------------------------------------------------------------------

def test_the_matrix_marks_only_the_recorded_release():
    print("8. A cell is PROVEN only on a recorded release")
    tfm = dict((r, "net48") for r in HF.REVIT_VERSIONS)
    evidence = {"versions": dict((r, {"failed": []}) for r in HF.REVIT_VERSIONS)}
    with Workspace() as ws:
        frag = ws.fragment(releases=entry_text("2025", "CURRENT")
                           + entry_text("2026", "0000000000000000").replace("  releases:\n", ""))
        claim = ws.fragment(slug="claim-only", status="DRAFT")
        matrix = MX.build_matrix(library=[frag, claim], evidence=evidence, tfm=tfm)

        row = matrix["cells"]["sample"]
        check(row["2024"] == MX.PROVEN, "PROVEN on 2024, the release the proof names")
        check(row["2025"] == MX.PROVEN, "PROVEN on 2025, from a fresh entry")
        check(row["2026"] == MX.COMPILES, "NOT on 2026, whose entry is stale")
        check(all(row[r] == MX.COMPILES for r in ("2020", "2021", "2022", "2023", "2027")),
              "and nowhere else, though `revit:` claims all eight")
        check(not any(v == MX.PROVEN for v in matrix["cells"]["claim-only"].values()),
              "a fragment with a claim and no proof is PROVEN nowhere")

        matrix = MX.build_matrix(library=[frag], evidence=None, tfm=tfm)
        check(not any(v == MX.PROVEN for v in matrix["cells"]["sample"].values()),
              "with no compile evidence no cell is PROVEN - as before D-131")

        demoted = ws.fragment(slug="demoted", status="DRAFT",
                              releases=entry_text("2025", "CURRENT"))
        check(MX.proven_releases(demoted) == ({}, []),
              "an entry on a fragment set back to DRAFT counts for nothing")


# ---------------------------------------------------------------------------
# 9. batch-prove --release
# ---------------------------------------------------------------------------

def job(name):
    return {"fragment": name, "setup": [], "keep-chain": False, "set": {},
            "negative-set": {"categoryName": "Sheets"}, "setup-set": {},
            "negative-setup-set": {}, "write": False, "cross": None, "in": None,
            "negative-in": None, "expect": None, "timeout": 30, "note": None}


def test_batch_prove_runs_only_what_is_proven_elsewhere():
    print("9. batch-prove --release")
    with Workspace() as ws:
        library = {
            "sample": ws.fragment(),
            "unproven": ws.fragment(slug="unproven", status="DRAFT"),
            "on-2025": ws.fragment(slug="on-2025", releases=entry_text("2025", "CURRENT")),
            "only-2024": ws.fragment(slug="only-2024", revit='["2024"]'),
        }
        stale = ws.fragment(slug="stale")
        with io.open(os.path.join(stale.folder, "impl", "any", "fragment.cs"), "w",
                     encoding="utf-8") as fh:
            fh.write(u"// the code moved\n")
        library["stale"] = HF.load(stale.folder)

        verdict = lambda name, release: BP.job_refusal(job(name), library, release)[0]
        check(verdict("sample", "2025") is None,
              "a PROVEN fragment not yet proved on 2025 would run")
        check(verdict("sample", "2024") == BP.ALREADY,
              "on the release its proof names it is ALREADY")
        check(verdict("on-2025", "2025") == BP.ALREADY,
              "with a fresh entry for the release it is ALREADY")
        check(verdict("unproven", "2025") == BP.NOT_PROVEN,
              "a DRAFT is NOT PROVEN - its first proof runs without --release")
        check(verdict("stale", "2025") == BP.NOT_PROVEN,
              "a proof the code moved under is NOT PROVEN for this mode")
        check(verdict("only-2024", "2025") == BP.REFUSED,
              "a release the fragment does not claim is refused")
        check(verdict("sample", None) == BP.ALREADY,
              "and without --release a PROVEN fragment is ALREADY, exactly as before")

        line = BP.validate_command(job("sample"), BP.record_name("sample", "2025"))
        check(line[line.index("--out") + 1].endswith(os.path.join("runs", "sample@2025.json")),
              "its run record is <slug>@<year>, never the proof's own")

        check(BP.wrong_release({"model": "m, Revit 2024, session 1"}, "2025"),
              "a record naming 2024 is the wrong release for a 2025 batch")
        check(BP.wrong_release({"model": "m, Revit 2025, session 1"}, "2025") is None,
              "and one naming 2025 is not")


FAKE_CLIENT = r'''
import io, json, os, sys
argv = sys.argv[1:]
out = argv[argv.index("--out") + 1]
name = argv[-1]
record = {"run_record": out, "fragment": name, "date": "2026-10-20",
          "model": "a test model, Revit %s, session 5" % os.environ["HERON_TEST_RAN_ON"],
          "phases": [{"phase": "positive", "ok": True, "provides": {}},
                     {"phase": "negative", "ok": True, "provides": {}}]}
folder = os.path.dirname(out)
if not os.path.isdir(folder):
    os.makedirs(folder)
io.open(out, "w", encoding="utf-8").write(json.dumps(record))
'''


def a_fragment_proven_elsewhere(library):
    """A real PROVEN fragment a 2025 batch would send - derived, never typed."""
    for slug in sorted(library):
        frag = library[slug]
        if (BP.job_refusal(job(slug), library, "2025")[0] is None
                and BRIDGE.fragment_risk(os.path.join(frag.folder, "fragment.yaml")) == "READ"):
            return slug
    return None


def test_a_batch_on_the_wrong_revit_says_so():
    print("9. A batch run on another Revit than --release is WRONG RELEASE, and the "
          "library is untouched")
    found, _ = HF.load_all()
    library = dict((f.slug, f) for f in found.values())
    before = dict((s, f.status) for s, f in library.items())
    slug = a_fragment_proven_elsewhere(library)
    check(slug is not None, "the library has a PROVEN fragment not yet proved on 2025")
    if slug is None:
        return

    workspace = tempfile.mkdtemp(prefix="heron-d131-batch-")
    saved = (BP.CLIENT, BP.VALIDATOR, BP.RUNS, os.environ.get("HERON_TEST_RAN_ON"))
    try:
        client = os.path.join(workspace, "client.py")
        io.open(client, "w", encoding="utf-8").write(FAKE_CLIENT)
        validator = os.path.join(workspace, "validator.py")
        io.open(validator, "w", encoding="utf-8").write(u"import sys\nsys.exit(0)\n")
        BP.CLIENT, BP.VALIDATOR = client, validator
        BP.RUNS = os.path.join(workspace, "runs")

        jobs = os.path.join(workspace, "jobs.yaml")
        io.open(jobs, "w", encoding="utf-8").write(
            u"defaults:\n  negative-set:\n    categoryName: Sheets\n"
            u"jobs:\n  - fragment: %s\n" % slug)

        report = os.path.join(workspace, "dry.json")
        code = BP.main([jobs, "--dry-run", "--release", "2025", "--report", report])
        rows = json.loads(io.open(report, encoding="utf-8").read())["results"]
        check(code == 0 and rows[0]["verdict"] == BP.WOULD_RUN
              and "%s@2025.json" % slug in rows[0]["why"],
              "the dry run would send it, to its own <slug>@2025 record")
        check(not os.path.isdir(BP.RUNS), "and the dry run wrote nothing")

        os.environ["HERON_TEST_RAN_ON"] = "2024"
        report = os.path.join(workspace, "wrong.json")
        code = BP.main([jobs, "--release", "2025", "--report", report])
        rows = json.loads(io.open(report, encoding="utf-8").read())["results"]
        check(rows[0]["verdict"] == BP.WRONG_RELEASE and "2024" in rows[0]["why"],
              "a run that came back from Revit 2024 is WRONG RELEASE: %s" % rows[0]["why"])
        check(code == 1, "and the batch exits 1 - it answered a question nobody asked")
        check(os.path.isfile(os.path.join(BP.RUNS, "%s@2025.json" % slug)),
              "the record it wrote is <slug>@2025.json")

        os.environ["HERON_TEST_RAN_ON"] = "2025"
        report = os.path.join(workspace, "right.json")
        BP.main([jobs, "--release", "2025", "--report", report])
        rows = json.loads(io.open(report, encoding="utf-8").read())["results"]
        check(rows[0]["verdict"] != BP.WRONG_RELEASE,
              "a run that came back from 2025 is judged on its evidence instead: %s"
              % rows[0]["verdict"])

        after, _ = HF.load_all()
        moved = [f.slug for f in after.values() if before.get(f.slug) != f.status]
        check(not moved, "no fragment's heron-status moved%s"
              % ("" if not moved else " - %s did" % ", ".join(moved)))
    finally:
        BP.CLIENT, BP.VALIDATOR, BP.RUNS = saved[:3]
        if saved[3] is None:
            os.environ.pop("HERON_TEST_RAN_ON", None)
        else:
            os.environ["HERON_TEST_RAN_ON"] = saved[3]
        shutil.rmtree(workspace, ignore_errors=True)


def main():
    for test in (test_an_entry_is_validated_like_a_proof,
                 test_a_release_entry_is_accepted,
                 test_a_release_already_recorded_is_refused,
                 test_a_stale_fingerprint_is_refused,
                 test_a_run_on_another_release_is_refused,
                 test_heron_status_is_never_written,
                 test_the_main_accept_keeps_recorded_releases,
                 test_a_second_route_is_written_signed_and_never_overwritten,
                 test_the_matrix_marks_only_the_recorded_release,
                 test_batch_prove_runs_only_what_is_proven_elsewhere,
                 test_a_batch_on_the_wrong_revit_says_so):
        # A SECTION THAT CANNOT RUN FAILS, AND THE NEXT ONE STILL RUNS. Against
        # code without D-131, `proof_key` and `accept_release` are simply not
        # there, and one traceback would hide every other section's answer.
        try:
            test()
        except Exception as exc:
            check(False, "%s could not run: %s: %s"
                  % (test.__name__, type(exc).__name__, exc))
        print("")

    if FAILURES:
        print("%d failure(s):" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("A further release and a second route are recorded beside the proof, "
          "never over it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
