#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Which Heron add-in build and which Revit build a proof was taken on -
`built_on:` - checked without Revit.

    python tests/test_proof_build.py

WHAT IT HOLDS
  1. The shape. `built_on` is optional; one that is there names a release and
     at least one build, and names nothing else.
  2. FIELD PRESENT. A run record that carries `built_on` gives a draft that
     carries it, and a signature carries it into the card. A draft whose
     build is not its record's is refused.
  3. FIELD ABSENT - AN OLD PROOF. A proof signed before the field is NOT
     RECORDED: never stale, never current. A record without it drafts with no
     `built_on` at all, and nothing re-stamps an old proof.
  4. THE ADD-IN'S HASH CHANGED. The proof is stale for that build, naming the
     add-in's file; a new run on the new build may be signed in its place,
     and one on the same build is still refused. Revit's own build the same.
     A build is compared only with a build of the same release.
  5. The client reads the two new fields from the add-in's address-book entry
     under the same names the brain reads, and the add-in writes them.
  6. The line under an answer says a proof taken on another build, and says
     nothing for a proof that recorded none.

Every fragment here is written into a temporary folder. Nothing touches the
library, and nothing here proves anything on any Revit.
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
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

sys.path.insert(0, os.path.join(ROOT, "tests", "support"))

import heron_fragment as HF                                       # noqa: E402
import heron_validate as HV                                       # noqa: E402
import heron_bridge_client as BRIDGE                              # noqa: E402
import scratch_audit                                              # noqa: E402

# Section 6 imports the server, which can open a knowledge store - and that
# writes audit lines. They go to a scratch folder, never the owner's trail.
scratch_audit.plant()

FAILURES = []
WAITING = []
COULD_NOT_RUN = 3


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


# The names the fix uses, asked for rather than assumed, so that against code
# without them each section FAILS rather than crashes (heron-ship 2a).
FIELDS = getattr(HF, "BUILT_ON_FIELDS", None)
STALE = getattr(HF, "BUILD_STALE", "stale")
CURRENT = getattr(HF, "BUILD_CURRENT", "current")
NOT_RECORDED = getattr(HF, "BUILD_NOT_RECORDED", "not recorded")
UNKNOWN = getattr(HF, "BUILD_NOT_KNOWN", "unknown")

BUILD_A = {"revit_release": "2024", "revit_build": "20230308_1635(x64)",
           "addin_version": "0.1.0.0", "addin_hash": "aaaaaaaaaaaaaaaa"}


def with_(base, **changes):
    built = dict(base)
    built.update(changes)
    return built


# ---------------------------------------------------------------------------
# A fragment in a temporary checkout, and its run records
# ---------------------------------------------------------------------------

CARD = """heron-agent: HERON-REVIT-LVL-027
heron-step: 15
heron-status: %(status)s
heron-since: 0.1.0
heron-layer: brain

id: FRG-TST-BLD
semantic-identity: "a sample used only by the built_on tests"
kind: filter
domain: revit.levels
capability: SAMPLE_BUILD_CAPABILITY
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

%(proof)s
revit: ["2020", "2021", "2022", "2023", "2024", "2025", "2026", "2027"]
runtime: [net472, net48, net8.0-windows, net10.0-windows]
tests: tests/

# ROUTING - this comment is load-bearing and must survive every write.
#
#   "sample this"  -> here

utterances:
  - sample this
"""

PROOF = """proof:
  date: '2026-09-10'
  by: A Person
  model: a test model (100 elements), Revit 2024, session 1
  positive_case: returned elements 5 on a selection of five walls
  negative_case: returned elements 0 on a selection holding no walls at all
  second_route: NOT ESTABLISHED - no second route was run
%(built_on)s  fingerprint: "%(fingerprint)s"
%(releases)s"""


def built_on_text(built):
    if not built:
        return ""
    return "  built_on:\n" + "".join(
        "    %s: '%s'\n" % (field, built[field]) for field in sorted(built))


def entry_text(release, built_on):
    """One `proof.releases` entry against the current code - CURRENT is filled in."""
    return ("  releases:\n"
            "  - release: '%s'\n"
            "    date: '2026-10-01'\n"
            "    by: A Person\n"
            "    model: a test model, Revit %s, session 2\n"
            "    positive_case: returned elements 5 on five walls\n"
            "    negative_case: returned elements 0 on a selection with no walls\n"
            "    fingerprint: 'CURRENT'\n"
            "%s" % (release, release, "".join(
                "  " + line + "\n"
                for line in built_on_text(built_on).rstrip("\n").split("\n")
                if line)))


def write_card(folder, status="PROVEN", built_on=None, proof=True, releases=""):
    impl = os.path.join(folder, "impl", "any")
    if not os.path.isdir(impl):
        os.makedirs(impl)
        with io.open(os.path.join(impl, "fragment.cs"), "w", encoding="utf-8") as fh:
            fh.write(u"// the sample's code\n")
    path = os.path.join(folder, "fragment.yaml")

    def render(fingerprint):
        block = (PROOF % {"built_on": built_on_text(built_on),
                          "fingerprint": fingerprint,
                          "releases": releases.replace("CURRENT", fingerprint)}
                 ) if proof else ""
        return CARD % {"status": status, "proof": block}

    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(render("pending"))
    # Hashed BEFORE the file is opened again - opening it for writing empties it.
    fingerprint = HF.load(folder).fingerprint()
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(render(fingerprint))
    return HF.load(folder)


def write_record(slug, built_on, release="2024"):
    # `slug` is the record's key - `sample`, or `sample@2025` for a release.
    """A run record where `accept` re-reads it, as the client writes one."""
    runs = os.path.join(HV.DRAFTS_DIR, "runs")
    if not os.path.isdir(runs):
        os.makedirs(runs)
    path = HV.run_record_path(slug)
    record = {"run_record": path, "fragment": slug, "date": "2026-11-16",
              "model": "a test model (100 elements), Revit %s, session 9" % release,
              "phases": [
                  {"phase": "positive", "ok": True, "provides": {"elements": 5},
                   "document": "a test model"},
                  {"phase": "negative", "ok": True, "provides": {"elements": 0},
                   "document": "a test model"}]}
    record["fragment"] = slug.split("@")[0]
    if built_on is not None:
        record["built_on"] = built_on
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(json.dumps(record, indent=1))
    return record


def text_of(frag):
    with io.open(os.path.join(frag.folder, "fragment.yaml"), encoding="utf-8") as fh:
        return fh.read()


class Workspace(object):
    """A temporary checkout with its own drafts folder, put back on exit."""

    def __enter__(self):
        self.root = tempfile.mkdtemp(prefix="heron-built-on-")
        self.drafts = HV.DRAFTS_DIR
        HV.DRAFTS_DIR = os.path.join(self.root, "drafts")
        return self

    def fragment(self, slug="sample", **card):
        return write_card(os.path.join(self.root, "brain", "fragments", slug), **card)

    def __exit__(self, *_):
        HV.DRAFTS_DIR = self.drafts
        shutil.rmtree(self.root, ignore_errors=True)


def state_of(frag, current):
    method = getattr(frag, "proof_build_state", None)
    if method is None:
        return None, None, None
    return method(current)


# ---------------------------------------------------------------------------
# 1. The shape
# ---------------------------------------------------------------------------

def test_the_shape():
    print("1. built_on is optional, and one that is there says what it must")
    check(FIELDS == ("revit_release", "revit_build", "addin_version", "addin_hash"),
          "the four fields are named once, in heron_fragment: %r" % (FIELDS,))
    with Workspace() as ws:
        full = ws.fragment(built_on=BUILD_A)
        check(not HF.proof_problems(full),
              "a proof carrying all four validates: %s" % HF.proof_problems(full))
        old = ws.fragment(slug="old")
        check(not HF.proof_problems(old),
              "a proof carrying none validates - every proof signed before it")
        no_release = ws.fragment(slug="no-release",
                                 built_on={"addin_hash": "aaaaaaaaaaaaaaaa"})
        check(any("revit_release" in p for p in HF.proof_problems(no_release)),
              "one with no release is named - a build is only read against its release")
        bare = ws.fragment(slug="bare", built_on={"revit_release": "2024"})
        check(any("no build" in p for p in HF.proof_problems(bare)),
              "one naming a release and no build is named")
        odd = ws.fragment(slug="odd", built_on=with_(BUILD_A, machine="PC-7"))
        check(any("machine" in p for p in HF.proof_problems(odd)),
              "a field that is not one of the four is named")


# ---------------------------------------------------------------------------
# 2. Field present
# ---------------------------------------------------------------------------

def test_a_record_with_the_field():
    print("2. A record carrying built_on gives a draft and a proof carrying it")
    with Workspace() as ws:
        frag = ws.fragment(status="DRAFT", proof=False)
        record = write_record("sample", BUILD_A)
        draft = HV.draft_from_record(frag, record)
        check((draft.get("proof-draft") or {}).get("built_on") == BUILD_A,
              "the draft's proof carries the record's build, field for field")
        check("aaaaaaaaaaaaaaaa" in str(draft.get("build"))
              and "20230308_1635(x64)" in str(draft.get("build")),
              "and the draft says which build in words: %s" % draft.get("build"))
        check(list(draft["proof-draft"])[-1] == "fingerprint",
              "the fingerprint is still the proof's last line")

        HV.write_draft("sample", draft)
        code, message = HV.accept("sample", "A Reviewer", library=[frag])
        signed = HF.load(frag.folder)
        check(code == 0, "the draft is signed: %s" % message)
        check(HF.built_on_of(signed.proof.get("built_on")) == BUILD_A,
              "the card's proof now records the build it was taken on")
        check(signed.status == "DRAFT", "and the status is untouched")
        check(not HF.proof_problems(signed), "the card validates: %s"
              % HF.proof_problems(signed))
        state, changed, recorded = state_of(signed, BUILD_A)
        check(state == CURRENT and not changed,
              "against the same build the proof is current: %s" % state)

        # A draft whose build was typed in, not copied from its record.
        frag2 = ws.fragment(slug="typed", status="DRAFT", proof=False)
        record2 = write_record("typed", BUILD_A)
        draft2 = HV.draft_from_record(frag2, record2)
        draft2["proof-draft"]["built_on"] = with_(BUILD_A, addin_hash="bbbbbbbbbbbbbbbb")
        HV.write_draft("typed", draft2)
        code, message = HV.accept("typed", "A Reviewer", library=[frag2])
        check(code != 0 and "run record" in message,
              "a draft whose build is not its record's is refused: %s" % message)
        check(HF.load(frag2.folder).proof is None,
              "and nothing was written to the card")


# ---------------------------------------------------------------------------
# 3. Field absent - an old proof
# ---------------------------------------------------------------------------

def test_an_old_proof_and_an_old_record():
    print("3. A proof or a record without built_on is NOT RECORDED")
    with Workspace() as ws:
        old = ws.fragment()
        for current in (BUILD_A, with_(BUILD_A, addin_hash="bbbbbbbbbbbbbbbb")):
            state, changed, recorded = state_of(old, current)
            check(state == NOT_RECORDED and not changed and recorded is None,
                  "an old proof against %s is NOT RECORDED - never stale, never "
                  "current: %s" % (current["addin_hash"], state))
        check(not old.proof_is_stale(),
              "and its code fingerprint still stands - the two are separate questions")

        record = write_record("sample", None)
        draft = HV.draft_from_record(old, record)
        check("built_on" not in draft["proof-draft"],
              "a record naming no build drafts with no built_on - never filled in")
        check("NOT RECORDED" in str(draft.get("build")),
              "and the draft says NOT RECORDED: %s" % draft.get("build"))

        before = text_of(old)
        restamp = getattr(HV, "restamp", None)
        if restamp is not None:
            restamp(library=[old], apply_changes=True)
        check(text_of(old) == before and "built_on" not in HF.load(old.folder).proof,
              "restamp leaves an old proof exactly as it was - no build is stamped on")

        HV.write_draft("sample", draft)
        code, message = HV.accept("sample", "A Reviewer", library=[old])
        check(code != 0 and "already" in message,
              "a draft naming no build cannot re-sign a proof that stands: %s" % message)


# ---------------------------------------------------------------------------
# 4. The add-in's hash changed
# ---------------------------------------------------------------------------

def test_the_addin_hash_changed():
    print("4. A new add-in build makes the proof stale for that build")
    build_b = with_(BUILD_A, addin_hash="bbbbbbbbbbbbbbbb")
    with Workspace() as ws:
        frag = ws.fragment(built_on=BUILD_A)
        state, changed, recorded = state_of(frag, build_b)
        check(state == STALE and changed == ["addin_hash"],
              "another add-in file is STALE, naming the file: %s %s" % (state, changed))
        check(recorded == BUILD_A, "and it says what the proof was taken on")
        check(not frag.proof_is_stale(),
              "the code fingerprint is untouched - a flag, never the D-117 gate")

        state, changed, _ = state_of(frag, with_(BUILD_A, revit_build="20240101_0900(x64)"))
        check(state == STALE and changed == ["revit_build"],
              "another Revit build of the same release is STALE too: %s" % changed)
        state, changed, _ = state_of(frag, with_(BUILD_A, revit_release="2025",
                                                 addin_hash="cccccccccccccccc"))
        check(state == NOT_RECORDED,
              "a build of another release is not compared - D-131's question: %s" % state)
        state, changed, _ = state_of(frag, {"revit_release": "2024"})
        check(state == UNKNOWN,
              "an add-in too old to say its build is UNKNOWN, not current: %s" % state)
        state, changed, _ = state_of(frag, BUILD_A)
        check(state == CURRENT, "the same build is CURRENT: %s" % state)

        # A new run on the new build may be signed in the proof's place.
        record = write_record("sample", build_b)
        draft = HV.draft_from_record(frag, record)
        check("stale for this build" in str(draft.get("build"))
              and "add-in's file" in str(draft.get("build")),
              "the draft says the proof on the card is stale for this build: %s"
              % draft.get("build"))
        HV.write_draft("sample", draft)
        code, message = HV.accept("sample", "A Reviewer", library=[frag])
        check(code == 0, "a run on the new build is signed in its place: %s" % message)
        again = HF.load(frag.folder)
        check(HF.built_on_of(again.proof.get("built_on")) == build_b,
              "and the card now records the new build")
        check(again.status == "PROVEN", "the status is untouched")
        check("# ROUTING - this comment is load-bearing" in text_of(again),
              "the routing comment survives")

        # One on the SAME build is still the leftover it always was.
        record = write_record("sample", build_b)
        HV.write_draft("sample", HV.draft_from_record(again, record))
        code, message = HV.accept("sample", "A Reviewer", library=[again])
        check(code != 0 and "already" in message,
              "a run on the build the proof already records is refused: %s" % message)

        # A run on another release never replaces the proof through plain accept.
        other = with_(BUILD_A, revit_release="2025", addin_hash="dddddddddddddddd")
        record = write_record("sample", other, release="2025")
        HV.write_draft("sample", HV.draft_from_record(again, record))
        code, message = HV.accept("sample", "A Reviewer", library=[again])
        check(code != 0, "a run on another release cannot replace the proof - that "
                         "is accept --release: %s" % message)


def test_a_release_entry_on_a_new_build():
    print("4b. accept --release replaces an entry whose build moved, and no other")
    on_2025 = with_(BUILD_A, revit_release="2025", revit_build="20240410_1500(x64)",
                    addin_hash="eeeeeeeeeeeeeeee")
    moved = with_(on_2025, addin_hash="ffffffffffffffff")
    with Workspace() as ws:
        frag = ws.fragment(built_on=BUILD_A, releases=entry_text("2025", on_2025))
        check(not HF.proof_problems(frag),
              "an entry carrying its own build validates: %s" % HF.proof_problems(frag))
        state, changed, recorded = state_of(frag, moved)
        check(state == STALE and changed == ["addin_hash"] and recorded == on_2025,
              "a 2025 build is read against the 2025 entry, not the 2024 proof: %s"
              % state)

        key = HV.proof_key("sample", release="2025")
        same = write_record(key, on_2025, release="2025")
        HV.write_draft(key, HV.release_draft(frag, same, "2025"))
        code, message = HV.accept_release("sample", "2025", "A Reviewer", library=[frag])
        check(code != 0 and "already recorded" in message,
              "a run on the build the entry records is refused: %s" % message)

        record = write_record(key, moved, release="2025")
        HV.write_draft(key, HV.release_draft(frag, record, "2025"))
        code, message = HV.accept_release("sample", "2025", "A Reviewer", library=[frag])
        check(code == 0, "a run on a new 2025 build replaces the entry: %s" % message)
        after = HF.load(frag.folder)
        rows = after.proof.get("releases") or []
        check(len(rows) == 1 and HF.built_on_of(rows[0].get("built_on")) == moved
              and rows[0].get("by") == "A Reviewer",
              "one 2025 entry, signed, on the new build")
        check(HF.built_on_of(after.proof.get("built_on")) == BUILD_A,
              "and the 2024 proof's own build is untouched")
        check(not HF.proof_problems(after), "the card validates: %s"
              % HF.proof_problems(after))


# ---------------------------------------------------------------------------
# 5. The client and the add-in
# ---------------------------------------------------------------------------

def test_the_client_reads_what_the_addin_writes():
    print("5. The client reads the add-in's two new fields under the brain's names")
    entry = {"pid": 9, "pipeName": "heron.2024.9", "revitVersion": "2024",
             "addinVersion": "0.1.0.0", "revitBuild": "20230308_1635(x64)",
             "addinHash": "aaaaaaaaaaaaaaaa", "protocolVersion": 2, "token": "t"}
    bridge = BRIDGE.Bridge(entry, "9.json")
    built = bridge.built_on() if hasattr(bridge, "built_on") else {}
    check(built == BUILD_A, "a session's built_on is the four fields: %r" % (built,))
    check(FIELDS is not None and set(built) == set(FIELDS),
          "under exactly the names heron_fragment reads")

    older = dict(entry)
    del older["revitBuild"], older["addinHash"]
    built = BRIDGE.Bridge(older, "9.json").built_on() if hasattr(bridge, "built_on") else {}
    check(built == {"revit_release": "2024", "addin_version": "0.1.0.0"},
          "an older add-in's entry gives what it said and nothing invented: %r" % (built,))
    check(HF.build_state(BUILD_A, built)[0] == UNKNOWN if hasattr(HF, "build_state")
          else False, "which reads as UNKNOWN against a proof that recorded a build")
    reader = getattr(BRIDGE, "built_on_of", None)
    check(reader is not None and reader(object()) == {}
          and reader(bridge) == bridge.built_on(),
          "a session object that cannot say its build gives {} - NOT RECORDED")

    with io.open(os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py"),
                 encoding="utf-8") as fh:
        client = fh.read()
    check(client.count('"built_on": built_on,') >= 2,
          "the validate record and the try-once record both carry built_on")
    with io.open(os.path.join(ROOT, "revit", "Heron.Bridge", "BridgeIdentity.cs"),
                 encoding="utf-8") as fh:
        identity = fh.read()
    check('\\"revitBuild\\"' in identity and '\\"addinHash\\"' in identity,
          "the add-in's address-book entry writes revitBuild and addinHash")
    with io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronApplication.cs"),
                 encoding="utf-8") as fh:
        startup = fh.read()
    check("VersionBuild" in startup and "BridgeIdentity.FileHash(" in startup,
          "and the add-in fills them from Revit's build and its own file")


# ---------------------------------------------------------------------------
# 6. The line under an answer
# ---------------------------------------------------------------------------

class _Session(object):
    def __init__(self, built):
        self._built = built

    def built_on(self):
        return dict(self._built)


def test_the_line_under_an_answer():
    print("6. The line under an answer says a proof taken on another build")
    try:
        import heron_brain as BRAIN
    except Exception as exc:                                # noqa: BLE001
        WAITING.append("heron_brain could not be imported here: %s" % exc)
        print("  NOT RUN  heron_brain could not be imported: %s" % exc)
        return
    with Workspace() as ws:
        ws.fragment(built_on=BUILD_A)
        ws.fragment(slug="old")
        real_root = BRAIN.ROOT
        BRAIN.ROOT = ws.root
        try:
            moved = BRAIN.proof_build("sample", with_(BUILD_A, addin_hash="b" * 16))
            same = BRAIN.proof_build("sample", BUILD_A)
            old = BRAIN.proof_build("old", BUILD_A)
        finally:
            BRAIN.ROOT = real_root
    check(moved and moved["state"] == STALE and moved["changed"] == ["addin_hash"],
          "the brain answers STALE for another add-in file: %r" % (moved,))
    check(same and same["state"] == CURRENT, "CURRENT for the same build")
    check(old and old["state"] == NOT_RECORDED, "NOT RECORDED for an old proof")

    # D-174: SAID, NEVER REFUSED. The D-117 gate is asked with the status the
    # brain reads from the card, and a build that moved does not make it stale.
    with Workspace() as ws:
        ws.fragment(built_on=BUILD_A)
        real_root = BRAIN.ROOT
        BRAIN.ROOT = ws.root
        try:
            found = BRAIN.proof_status("sample")
        finally:
            BRAIN.ROOT = real_root
    check(found and found["stale"] is False
          and BRIDGE.unproven_refusal("SAMPLE_BUILD_CAPABILITY", found) is None,
          "a proof stale only for another build is not refused by D-117's gate (D-174)")

    with io.open(os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py"),
                 encoding="utf-8") as fh:
        server = fh.read()
    for door in ("revit_read", "revit_show", "revit_change"):
        start = server.find("def %s(" % door)
        end = server.find("\n@server.tool()", start + 1)
        body = server[start:end if end > 0 else len(server)]
        check(start >= 0 and "_build_line(capability, folder, session)" in body,
              "%s says it under its answer" % door)

    line = _server_build_line()
    if line is None:
        return
    status = {"status": "PROVEN", "stale": False, "proof": True}
    for name, build, said in (
            ("stale", {"state": STALE, "changed": ["addin_hash"], "recorded": BUILD_A}, True),
            ("old", {"state": NOT_RECORDED, "changed": [], "recorded": None}, False),
            ("unknown", {"state": UNKNOWN, "changed": [], "recorded": BUILD_A}, False),
            ("current", {"state": CURRENT, "changed": [], "recorded": BUILD_A}, False)):
        text = line(status, build)
        if said:
            check("add-in's file" in text and "20230308_1635(x64)" in text
                  and "D-30" in text,
                  "a %s proof is said, naming what changed and what it was taken on: %s"
                  % (name, text))
        else:
            check(text == "", "a %s proof adds no line: %r" % (name, text))
    check(line(dict(status, stale=True), {"state": STALE, "changed": ["addin_hash"],
                                          "recorded": BUILD_A}) == "",
          "a proof whose code moved is left to the line that already says so")


def _server_build_line():
    """The server's `_build_line`, with the brain stood in - or None, waiting."""
    try:
        import mcp  # noqa: F401
    except BaseException as exc:                            # noqa: BLE001 - see heron-ship 4
        WAITING.append("the MCP SDK is not importable here (%s)" % exc)
        print("  NOT RUN  the server's line needs the MCP SDK: pip install --user mcp")
        return None
    home = tempfile.mkdtemp(prefix="heron-built-on-server-")
    had = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = home
    real_discovery = BRIDGE.DISCOVERY_DIR
    BRIDGE.DISCOVERY_DIR = os.path.join(home, "bridges")
    os.makedirs(BRIDGE.DISCOVERY_DIR)
    try:
        import heron_mcp_server as SERVER
    except Exception as exc:                                # noqa: BLE001
        check(False, "the server could not be imported: %s: %s" % (type(exc).__name__, exc))
        return None
    finally:
        BRIDGE.DISCOVERY_DIR = real_discovery
        if had is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = had
        shutil.rmtree(home, ignore_errors=True)

    method = getattr(SERVER, "_build_line", None)
    check(method is not None, "the server has a line for a proof's build")
    if method is None:
        return None

    def line(status, build):
        real_status = SERVER.brain.proof_status
        real_build = getattr(SERVER.brain, "proof_build", None)
        SERVER.brain.proof_status = lambda folder: status
        SERVER.brain.proof_build = lambda folder, current: build
        try:
            return method("SAMPLE_BUILD_CAPABILITY", "sample",
                          _Session(with_(BUILD_A, addin_hash="b" * 16)))
        finally:
            SERVER.brain.proof_status = real_status
            if real_build is not None:
                SERVER.brain.proof_build = real_build
    return line


def main():
    for test in (test_the_shape,
                 test_a_record_with_the_field,
                 test_an_old_proof_and_an_old_record,
                 test_the_addin_hash_changed,
                 test_a_release_entry_on_a_new_build,
                 test_the_client_reads_what_the_addin_writes,
                 test_the_line_under_an_answer):
        # A SECTION THAT CANNOT RUN FAILS, AND THE NEXT ONE STILL RUNS.
        try:
            test()
        except Exception as exc:                            # noqa: BLE001
            check(False, "%s could not run: %s: %s"
                  % (test.__name__, type(exc).__name__, exc))
        print("")

    if FAILURES:
        print("%d failure(s):" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    if WAITING:
        print("COULD NOT RUN IN FULL - nothing failed, and this is not a pass:")
        for why in WAITING:
            print("  - %s" % why)
        return COULD_NOT_RUN
    print("A proof records which add-in and which Revit build it was taken on, "
          "and says so when either moves.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
