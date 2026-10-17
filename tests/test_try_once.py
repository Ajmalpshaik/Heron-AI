#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A DRAFT fragment's one run on the live model, behind Keep or Discard (D-117 to D-120).

    python tests/test_try_once.py

THE PATH
--------
`try-once` sends a DRAFT at risk MODIFY as `run_fragment_write` with
`"apply": "ask"`. The add-in runs it inside one TransactionGroup, redraws the
view, and a box in Revit asks the modeller to keep it or discard it. A kept
run is recorded and becomes the positive case of the fragment's proof
(`validate --write --kept`); the negative case is still its own rolled-back
run, and `heron_validate` refuses a kept phase that was discarded or whose
code has moved.

WHAT IT PROVES
  1. THE ADD-IN READS "ask" AND ASKS ONLY IN ITS PLACE - its conditions are
     checked above the TransactionGroup, the box comes after the fragment's
     transaction and before Assimilate, Discard is the default, and only Keep
     keeps.
  2. THE DISPATCHER WAITS FOR A PERSON, not for a timeout - the extra wait is
     the configured ask timeout, only for a job that put the box up.
  3. THE SETTING EXISTS ON BOTH SIDES, and the client waits beyond the add-in.
  4. `try-once` SENDS WHAT IT SHOULD, RECORDS WHAT CAME BACK, AND REFUSES
     what it must - before anything is sent.
  5. `validate --kept` TAKES THE POSITIVE FROM THE KEPT RUN, runs only the
     negative, and refuses a discarded run or moved code.
  6. THE BRAIN READS THE SAME HASH, and `draft` and `accept` refuse a kept
     phase that cannot stand.
  7. THE AUTHOR AGENT SAYS THE NEW RULE, on both hosts' copies.

WHAT IT CANNOT SAY
------------------
That Revit draws the change before the box, that Keep leaves one undo entry
and Discard none. That is NEEDS-CHECKING Group EM, on a real Revit.
"""

import contextlib
import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The client reads heron.config for its timeouts. Point it at a file that is
# not there, so the owner's own settings are neither read nor needed.
_SCRATCH = tempfile.mkdtemp(prefix="heron-try-once-")
os.environ["HERON_CONFIG"] = os.path.join(_SCRATCH, "heron.config")

sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_bridge_client as CLIENT                          # noqa: E402
import heron_config as CONFIG                                 # noqa: E402
import heron_fragment as HF                                   # noqa: E402
import heron_validate as HV                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(*parts):
    with io.open(os.path.join(ROOT, *parts), encoding="utf-8") as handle:
        return handle.read()


def before(text, first, second):
    """Both are there, and `first` comes before `second`. Never -1 < n."""
    a, b = text.find(first), text.find(second)
    return a >= 0 and b >= 0 and a < b


def client_hash(text):
    """The client's source hash - or, when it has none, the rule it should
    follow, so the checks below still RUN and FAIL (heron-ship 2a)."""
    import hashlib
    found = getattr(CLIENT, "source_hash", None)
    if found is None:
        return hashlib.sha256(text.encode("utf-8")).hexdigest()[:16]
    return found(text)


def body_of(text, signature):
    """The text from `signature` to the next method's summary, or ''."""
    at = text.find(signature)
    if at < 0:
        return ""
    end = text.find("/// <summary>", at)
    return text[at:end if end > at else len(text)]


# ---------------------------------------------------------------------------

def test_the_add_in():
    print("1. The add-in reads \"ask\", and asks only in its place")
    run = read("revit", "Heron.Revit.Addin", "RevitFragment.cs")

    check('"apply"), "ask"' in run,
          "RevitFragment reads apply = \"ask\"")
    check(before(run, "AskRefusal(request, app, target)", "new TransactionGroup(target, label)"),
          "its conditions are checked ABOVE the TransactionGroup, so a refusal changes nothing")

    refusal = body_of(run, "private static string AskRefusal(")
    check('"run_fragment_write"' in refusal,
          "only on the change path - never an export or an admin change")
    check("ActiveUIDocument" in refusal and ".Equals(target)" in refusal,
          "only on the model in front, so the modeller can see it")
    check("IsModified" in refusal and "PathName" in refusal,
          "only on a saved model with nothing unsaved (D-119)")
    for code in ("ask_needs_change_path", "ask_needs_model_in_front",
                 "ask_needs_saved_model"):
        check(code in refusal, "refuses as %s" % code)

    ask = body_of(run, "private static string AskTheModeller(")
    check(before(ask, "RefreshActiveView()", "box.Show()"),
          "the view is redrawn before the box is shown")
    check(before(ask, "RevitDispatcher.AwaitingModeller()", "box.Show()"),
          "the dispatcher is told a person is being waited for, before the box")
    check("using (HeronDialogWatch.HeronAsks())" in ask
          and before(ask, "using (HeronDialogWatch.HeronAsks())", "result = box.Show();"),
          "the box is shown inside HeronAsks, so Heron's own dialog watch never answers it")
    check(before(run, "if (HeronDialogWatch.Refused)", "answered = AskTheModeller("),
          "a job a Revit dialog stopped is rolled back before anybody is asked to keep it")
    check("DefaultButton = TaskDialogResult.CommandLink2" in ask,
          "Discard is the default button")
    check("AllowCancellation = true" in ask,
          "Escape and the close button are allowed - and they discard")
    check("TaskDialogResult.CommandLink1) return Keep" in ask
          and ask.count("return Keep") == 1,
          "Keep is returned for the Keep link and nothing else")

    check(before(run, "transaction.Commit() != TransactionStatus.Committed",
                 "answered = AskTheModeller(")
          and before(run, "answered = AskTheModeller(", "if (apply) group.Assimilate();"),
          "the box comes after the fragment's transaction commits and before Assimilate")
    check("apply = answered == Keep;" in run,
          "the modeller's Keep is what sets apply - nothing on the wire can")

    answer = body_of(run, "private static string WithAnswer(")
    for field in ("asked", "modellerAnswer", "kept", "viewRefreshed"):
        check('"%s"' % field in answer, "the reply says %s" % field)


def test_the_dispatcher():
    print("\n2. The dispatcher waits for a person, not for a timeout")
    dispatcher = read("revit", "Heron.Revit.Addin", "RevitDispatcher.cs")
    check('"revit.askTimeoutSeconds"' in dispatcher,
          "it reads revit.askTimeoutSeconds")
    check("job.AskedModeller && job.WaitForFinish(_askTimeout)" in dispatcher,
          "the extra wait is the ask timeout, and only for a job that asked")
    check('"awaiting_modeller"' in dispatcher,
          "running out of it says the box is still open, not \"still running\"")
    check("[ThreadStatic]" in dispatcher and "_running = job;" in dispatcher
          and "_running = null;" in dispatcher,
          "the running job is found only on Revit's own thread, and forgotten after")
    check("internal static void AwaitingModeller()" in dispatcher,
          "the fragment path can mark the job without being handed it")


def test_the_setting():
    print("\n3. The ask timeout is a setting on both sides")
    core = read("platform", "Heron.Core", "HeronConfig.cs")
    check('{ "revit.askTimeoutSeconds", "300" }' in core,
          "HeronConfig declares revit.askTimeoutSeconds = 300")
    check(CONFIG.DEFAULTS.get("revit.askTimeoutSeconds") == "300",
          "heron_config declares the same default")

    ask = getattr(CONFIG, "try_once_timeout", None)
    check(ask is not None, "heron_config has a try-once deadline at all")
    if ask is None:
        return
    for operation, asking in ((60, 300), (300, 300), (60, 30), (900, 1200)):
        values = dict(CONFIG.DEFAULTS)
        values["revit.operationTimeoutSeconds"] = str(operation)
        values["revit.askTimeoutSeconds"] = str(asking)
        waits = ask(values)
        addin = (CONFIG.busy_timeout(values) + CONFIG.operation_timeout(values)
                 + CONFIG.ask_timeout(values))
        check(waits > addin,
              "at operation %ds and ask %ds the client waits %gs, beyond the add-in's %gs"
              % (operation, asking, waits, addin))


# ---------------------------------------------------------------------------
# The client, against a Revit that is not there.

class FakeBridge(object):
    """A Revit that answers from a script and keeps what it was sent."""

    revit_version = "2024"
    pid = 4242

    def __init__(self, answers):
        self.sent = []
        self.answers = answers

    def request(self, op, op_args=None, **kwargs):
        self.sent.append((op, dict(op_args or {}), dict(kwargs)))
        answer = self.answers(op, op_args or {})
        return None if answer is None else dict(answer)

    def release(self):
        pass

    def close(self):
        pass


def library_fragment(risk, status):
    """The first fragment in the library with this risk and status, or None.

    Read with the brain's own loader rather than the client's, so a client
    that cannot read a status yet still gets a fragment to FAIL on.
    """
    folder = os.path.join(ROOT, "brain", "fragments")
    for name in sorted(os.listdir(folder)):
        card = os.path.join(folder, name, "fragment.yaml")
        if not (os.path.isfile(card)
                and os.path.isfile(os.path.join(folder, name, "impl", "any", "fragment.cs"))
                and CLIENT.fragment_risk(card) == risk):
            continue
        if HF.load(os.path.join(folder, name)).status == status:
            return name
    return None


@contextlib.contextmanager
def no_revit(bridge, once_path):
    """The client with discovery, the platform check and the record path replaced."""
    saved = (CLIENT.refuse_off_windows, CLIENT.discover,
             getattr(CLIENT, "once_record_path", None))
    CLIENT.refuse_off_windows = lambda: False
    CLIENT.discover = lambda prune=True: ([bridge], [], [], [])
    CLIENT.once_record_path = lambda root, name: once_path
    try:
        yield
    finally:
        CLIENT.refuse_off_windows, CLIENT.discover = saved[0], saved[1]
        if saved[2] is None:
            del CLIENT.once_record_path
        else:
            CLIENT.once_record_path = saved[2]


def try_once(name, bridge, once_path, **kwargs):
    run = getattr(CLIENT, "cmd_try_once", None)
    if run is None:
        return None, ""
    out = io.StringIO()
    with no_revit(bridge, once_path), contextlib.redirect_stdout(out):
        code = run(name, **kwargs)
    return code, out.getvalue()


def kept_reply(op, args):
    return {"ok": True, "document": "Scratch", "provides": {"renamed": 3},
            "applied": True, "rolledBack": False, "verdict": "kept",
            "asked": True, "modellerAnswer": "keep", "kept": True,
            "viewRefreshed": True, "asking": "You pressed KEEP."}


def discarded_reply(op, args):
    return {"ok": True, "document": "Scratch", "provides": {"renamed": 3},
            "applied": False, "rolledBack": True, "verdict": "nothing kept",
            "asked": True, "modellerAnswer": "discard", "kept": False,
            "viewRefreshed": True, "asking": "You pressed DISCARD."}


def test_try_once(draft, once_path):
    print("\n4. try-once sends what it should and records what came back")
    check(draft is not None, "the library has a DRAFT fragment at risk MODIFY to try")
    if draft is None:
        return

    bridge = FakeBridge(kept_reply)
    code, out = try_once(draft, bridge, once_path)
    check(code == 0, "a kept run exits 0 (got %r)" % code)
    sent = bridge.sent
    check(len(sent) == 1 and sent[0][0] == "run_fragment_write",
          "ONE request, as run_fragment_write - %r" % [s[0] for s in sent])
    args = sent[0][1] if sent else {}
    extra = sent[0][2] if sent else {}
    check(args.get("apply") == "ask", "sent with apply = \"ask\", never \"true\"")
    check(args.get("chain") == "reset", "and a fresh chain")
    check(extra.get("idempotent") is False, "never sent twice - a resend could keep it twice")
    deadline = getattr(CONFIG, "try_once_timeout", lambda: 0)()
    check(extra.get("response_timeout") == deadline,
          "it waits the try-once deadline, %gs, not a fragment's" % deadline)

    record = None
    if os.path.isfile(once_path):
        with io.open(once_path, encoding="utf-8") as fh:
            record = json.load(fh)
    check(record is not None, "the run is recorded")
    if record is None:
        return
    source = read("brain", "fragments", draft, "impl", "any", "fragment.cs")
    check(record.get("kept") is True and record["phase"].get("kept") is True,
          "as KEPT, on the record and on its phase")
    check(record.get("source_hash") == client_hash(source)
          and record["phase"].get("source_hash") == record.get("source_hash"),
          "naming the code that ran")
    check(record["phase"].get("phase") == "positive"
          and record["phase"].get("provides") == {"renamed": 3},
          "with what the fragment returned, as a positive phase")
    check("pressed Keep" in record["phase"].get("arranged", ""),
          "and saying the modeller pressed Keep")

    print("\n   ...and refuses what it must, before anything is sent")
    again = FakeBridge(kept_reply)
    code, out = try_once(draft, again, once_path)
    check(code == 2 and not again.sent,
          "a version already KEPT is refused - again would make the change twice")
    check("--kept" in out, "and the refusal names the next step, the negative case")

    discarded = FakeBridge(discarded_reply)
    os.remove(once_path)
    code, _ = try_once(draft, discarded, once_path)
    with io.open(once_path, encoding="utf-8") as fh:
        record = json.load(fh)
    check(code == 0 and record.get("kept") is False,
          "a discarded run is recorded as NOT kept")
    retry = FakeBridge(discarded_reply)
    try_once(draft, retry, once_path)
    check(len(retry.sent) == 1, "and a discarded version may be asked for again")

    old = FakeBridge(lambda op, args: {"ok": True, "document": "Scratch",
                                       "applied": False, "rolledBack": True,
                                       "verdict": "NOTHING WAS KEPT."})
    os.remove(once_path)
    code, out = try_once(draft, old, once_path)
    check(code == 1 and not os.path.isfile(once_path),
          "an add-in older than the box (no `asked`) is said, and nothing is recorded")
    check("older" in out, "and the answer says the add-in is older")

    silent = FakeBridge(lambda op, args: None)
    code, out = try_once(draft, silent, once_path)
    check(code == 1 and not os.path.isfile(once_path),
          "no answer is not recorded")
    check("stands" in out, "and says what the modeller presses still stands")

    for risk, status, why in (("READ", "DRAFT", "a READ fragment"),
                              ("MODIFY", "PROVEN", "a PROVEN fragment")):
        name = library_fragment(risk, status)
        if name is None:
            continue
        refused = FakeBridge(kept_reply)
        code, _ = try_once(name, refused, once_path)
        check(code == 2 and not refused.sent, "%s is refused before Revit is asked" % why)

    code, _, _ = parse_try_once(draft, "--write")
    check(code == 2, "try-once takes no --write: whether it stays is Keep, not a flag")


def parse_try_once(*tokens):
    captured = {}

    def capture(name, values=None, **kwargs):
        captured.update(name=name, values=values, **kwargs)
        return 0

    real = getattr(CLIENT, "cmd_try_once", None)
    CLIENT.cmd_try_once = capture
    out = io.StringIO()
    try:
        with contextlib.redirect_stdout(out):
            code = CLIENT.main(["heron_bridge_client.py", "try-once"] + list(tokens))
    finally:
        if real is None:
            del CLIENT.cmd_try_once
        else:
            CLIENT.cmd_try_once = real
    return code, captured, out.getvalue()


def test_validate_kept(draft, once_path, workspace):
    print("\n5. validate --kept takes the positive from the kept run")
    if draft is None:
        return
    source = read("brain", "fragments", draft, "impl", "any", "fragment.cs")

    def once(kept=True, version=None):
        version = version or client_hash(source)
        with io.open(once_path, "w", encoding="utf-8") as fh:
            fh.write(json.dumps({
                "fragment": draft, "kept": kept, "source_hash": version,
                "answer": "keep" if kept else "discard",
                "phase": {"phase": "positive", "ok": True, "kept": kept,
                          "source_hash": version, "provides": {"renamed": 3},
                          "document": "Scratch",
                          "arranged": "run ONCE ... pressed Keep"}}))

    def answers(op, args):
        if op == "count_elements":
            return {"ok": True, "document": "Scratch", "count": 10}
        return {"ok": True, "document": "Scratch", "provides": {"renamed": 0},
                "rolledBack": True}

    def validate(**kwargs):
        bridge = FakeBridge(answers)
        out = os.path.join(workspace, "run.json")
        if os.path.isfile(out):
            os.remove(out)
        printed = io.StringIO()
        with no_revit(bridge, once_path), contextlib.redirect_stdout(printed):
            try:
                code = CLIENT.cmd_validate(
                    draft, out=out, writing=kwargs.pop("writing", True),
                    negative_values=[{"name": "nothing", "value": "here"}], **kwargs)
            except TypeError as error:
                code = None
                printed.write(u"%s" % error)
        record = None
        if os.path.isfile(out):
            with io.open(out, encoding="utf-8") as fh:
                record = json.load(fh)
        return code, bridge.sent, record, printed.getvalue()

    once()
    code, sent, record, _ = validate(kept=True)
    writes = [s for s in sent if s[0] == "run_fragment_write"]
    check(code == 0 and record is not None, "a kept run validates (exit %r)" % code)
    check(len(writes) == 1, "ONE write is sent - the negative; the positive is not run again "
          "(%d sent)" % len(writes))
    check(writes and "apply" not in writes[0][1],
          "and the negative is rolled back - no apply")
    phases = dict((p.get("phase"), p) for p in (record or {}).get("phases", []))
    check(phases.get("positive", {}).get("kept") is True
          and phases.get("positive", {}).get("provides") == {"renamed": 3},
          "the record's positive IS the kept run")
    check("negative" in phases, "and the negative ran")
    check((record or {}).get("positive_from", "").endswith(draft + ".once.json"),
          "and the record names where the positive came from")

    once(kept=False)
    code, sent, record, out = validate(kept=True)
    check(code == 2 and not sent, "a DISCARDED run is refused, nothing sent")

    once(version="0000000000000000")
    code, sent, record, out = validate(kept=True)
    check(code == 2 and not sent, "a kept run of OTHER code is refused, nothing sent")
    check("different code" in out, "and says the code moved")

    once()
    code, sent, record, out = validate(kept=True, writing=False)
    check(code == 2 and not sent, "--kept without --write is refused")


# ---------------------------------------------------------------------------
# The brain.

SAMPLE = """heron-agent: HERON-REVIT-LVL-027
heron-step: 15
heron-status: DRAFT
heron-since: 0.1.0
heron-layer: brain

id: FRG-TST-001
semantic-identity: "a sample used only by the tests"
kind: transform
domain: revit.views
capability: SAMPLE_CAPABILITY
version: 1
source: OFFICIAL
risk: MODIFY

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

utterances:
  - sample this
"""


def test_the_brain(workspace):
    print("\n6. The brain reads the same hash, and refuses a kept phase that cannot stand")
    folder = os.path.join(workspace, "sample")
    os.makedirs(os.path.join(folder, "impl", "any"))
    with io.open(os.path.join(folder, "fragment.yaml"), "w", encoding="utf-8") as fh:
        fh.write(SAMPLE)
    # CRLF on disk, as a Windows checkout writes it.
    with io.open(os.path.join(folder, "impl", "any", "fragment.cs"), "wb") as fh:
        fh.write(b"var a = 1;\r\nvar b = 2;\r\n")
    frag = HF.load(folder)

    brain_hash = getattr(HV, "source_hash", None)
    check(brain_hash is not None, "heron_validate can hash a fragment's source at all")
    if brain_hash is None:
        return
    with io.open(os.path.join(folder, "impl", "any", "fragment.cs"), "r",
                 encoding="utf-8") as fh:
        sent = fh.read()
    check(brain_hash(frag) == client_hash(sent),
          "the brain and the client give the same hash for the same file, CRLF and all")

    good = {"phase": "positive", "ok": True, "kept": True,
            "source_hash": brain_hash(frag), "provides": {"elements": 5},
            "arranged": "run ONCE on the live model; the modeller pressed Keep"}
    check(HV.kept_problem(frag, good) is None, "a kept run of this code stands")
    check(HV.kept_problem(frag, dict(good, kept=False)) is not None,
          "a discarded one does not")
    check(HV.kept_problem(frag, dict(good, source_hash="0000000000000000")) is not None,
          "nor one of other code")
    check(HV.kept_problem(frag, {"phase": "positive", "ok": True}) is None,
          "and an ordinary run is not its business")

    record = {"run_record": "x", "date": "2026-10-17", "model": "Scratch",
              "phases": [good, {"phase": "negative", "ok": True,
                                "provides": {"elements": 0}}]}
    draft = HV.draft_from_record(frag, record)
    check("pressed Keep" in draft["proof-draft"]["positive_case"]
          and "the kept run cannot be the positive case" not in draft["gaps"],
          "draft writes the kept run as the positive case")
    bad = dict(record, phases=[dict(good, source_hash="0000000000000000"),
                               record["phases"][1]])
    draft_bad = HV.draft_from_record(frag, bad)
    check(draft_bad["proof-draft"]["positive_case"].startswith(HV.NOT_ESTABLISHED)
          and "the kept run cannot be the positive case" in draft_bad["gaps"],
          "and NOT ESTABLISHED when the code moved")

    original = HV.DRAFTS_DIR
    try:
        HV.DRAFTS_DIR = os.path.join(workspace, "drafts")
        runs = os.path.join(HV.DRAFTS_DIR, "runs")
        os.makedirs(runs)

        def sign(phases):
            with io.open(os.path.join(runs, "sample.json"), "w", encoding="utf-8") as fh:
                fh.write(json.dumps(dict(record, phases=phases)))
            HV.write_draft("sample", HV.draft_from_record(frag, record))
            return HV.accept("sample", "A Person", library=[frag])

        code, message = sign([dict(good, kept=False), record["phases"][1]])
        check(code == 2 and "did NOT keep" in message,
              "accept refuses a record whose positive was discarded: %s" % message[:60])
        code, message = sign([dict(good, source_hash="0000000000000000"),
                              record["phases"][1]])
        check(code == 2 and "different code" in message,
              "accept refuses a kept run of other code")
        code, message = sign(record["phases"])
        check(code == 0, "and signs a kept run with its negative: %s" % message[:70])
        check(HF.load(folder).status == "DRAFT", "leaving the status DRAFT")
    finally:
        HV.DRAFTS_DIR = original


def test_the_author():
    print("\n7. The author agent says the new rule, on both hosts")
    claude = read(".claude", "agents", "heron-fragment-author.md")
    codex = read(".codex", "agents", "heron-fragment-author.toml")
    for host, text in (("claude", claude), ("codex", codex)):
        check("You never run it" not in text, "%s: \"You never run it\" is gone" % host)
        check("try-once" in text, "%s: names try-once as the one route" % host)
        check("only when the modeller asks" in text, "%s: only when the modeller asks" % host)
        check("D-120" in text and "D-117" in text, "%s: cites D-117 and D-120" % host)
        check("never" in text and "revit_change" in text,
              "%s: still never through revit_change" % host)


def main():
    workspace = tempfile.mkdtemp(prefix="heron-try-once-work-")
    once_path = os.path.join(workspace, "once.json")
    try:
        test_the_add_in()
        test_the_dispatcher()
        test_the_setting()
        draft = library_fragment("MODIFY", "DRAFT")
        test_try_once(draft, once_path)
        test_validate_kept(draft, once_path, workspace)
        test_the_brain(workspace)
        test_the_author()
    finally:
        shutil.rmtree(workspace, ignore_errors=True)
        shutil.rmtree(_SCRATCH, ignore_errors=True)

    print()
    if FAILURES:
        print("%d check(s) FAILED" % len(FAILURES))
        return 1
    print("All checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
