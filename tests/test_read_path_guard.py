#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A read run that changes the model is never answered as a clean read - row 5b-350.

    python tests/test_read_path_guard.py

WHAT HAPPENED
-------------
On 2026-10-06 `place-nested-family` was run WITHOUT `--write` in a scratch
family, to see whether a nested family was loaded. The reply was Revit's
refusal - "Attempt to modify the model outside of transaction" - and a real
AngleBar instance stayed at the origin each time, twice. The read path stood on
one sentence, said in three places: no transaction is open, so Revit refuses
every change. That run is the exception, and why it happens is not established
(NEEDS-CHECKING CP1).

WHAT THIS HOLDS
---------------
  1. THE COMMAND LINE'S READ PATH CARRIES NOTHING THAT CHANGES THE MODEL.
     `read_path_refusal` refuses MODIFY, PUBLISH and ADMIN and lets READ,
     ANALYZE, SUGGEST and EXECUTE through - over every fragment in the
     library, not a sample. EXECUTE stays: `set-selection` arranges every
     selection-based proof on this path, and changing the selection changes
     what is highlighted, not what exists.
  2. DRIVEN THROUGH A FAKE REVIT. `fragment`, `prove` and `validate` refuse a
     MODIFY fragment, or a MODIFY setup step, on the read path BEFORE Revit is
     asked anything; `--write` still sends it as run_fragment_write; a read
     still goes as run_fragment_read.
  3. THE ADD-IN MEASURES EVERY READ RUN, read as text: the element count and
     IsModified before and after, a change answered `read_changed_model`
     ahead of anything the fragment said, the count kept in the audit line,
     and the Revit banner able to name it.
  4. THE THREE PLACES THAT CLAIMED THE GUARANTEE NOW QUALIFY IT, citing the row.

WHAT IT CANNOT DO
-----------------
Run the detector. That needs Revit, and whether it fires on the
place-nested-family run is CP1's first question.
"""

import contextlib
import io
import os
import re
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
EXECUTOR = os.path.join(ADDIN, "RevitFragment.cs")
DISPATCHER = os.path.join(ADDIN, "RevitDispatcher.cs")
REGISTRY_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronOperationRegistry.cs")
TOOLS_PY = os.path.join(ROOT, "mcp", "server", "heron_tools.py")

# The client reads heron.config for its timeouts. Point it at a file that is
# not there, so the owner's own settings are neither read nor needed.
_SCRATCH = tempfile.mkdtemp(prefix="heron-read-guard-")
os.environ["HERON_CONFIG"] = os.path.join(_SCRATCH, "heron.config")

sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_bridge_client as CLIENT                          # noqa: E402

# THE FRAGMENT IN THE ROW, by name. Section 1 checks it is still MODIFY, so a
# card that changed its risk fails there rather than making section 2 vacuous.
PLACER = "place-nested-family"
SELECTOR = "set-selection"

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    try:
        return io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return ""


def card_risk(name):
    return CLIENT.fragment_risk(os.path.join(FRAGMENTS, name, "fragment.yaml"))


def library():
    return [n for n in sorted(os.listdir(FRAGMENTS))
            if os.path.isfile(os.path.join(FRAGMENTS, n, "fragment.yaml"))]


class FakeBridge(object):
    """A Revit that answers every request and writes down what it was sent."""

    revit_version = "2024"
    pid = 4242

    def __init__(self):
        self.sent = []

    def request(self, op, **kwargs):
        self.sent.append(op)
        if op == "count_elements":
            return {"ok": True, "document": "Fake model", "count": 3}
        return {"ok": True, "document": "Fake model", "provides": {},
                "rolledBack": True, "verdict": "rolled back (fake)"}

    def release(self):
        pass

    def close(self):
        pass


@contextlib.contextmanager
def fake_revit():
    """The client with a pipe that is a list, and its output kept.

    `discover` counts how often it was asked: a run refused before anything
    was sent must never have asked at all.
    """
    bridge = FakeBridge()
    asked = {"discover": 0}

    def discover(prune=True):
        asked["discover"] += 1
        return [bridge], [], [], []

    saved = (CLIENT.refuse_off_windows, CLIENT.discover, CLIENT._prompt)
    CLIENT.refuse_off_windows = lambda: False
    CLIENT.discover = discover
    CLIENT._prompt = lambda: ""
    out = io.StringIO()
    try:
        with contextlib.redirect_stdout(out):
            yield bridge, asked, out
    finally:
        CLIENT.refuse_off_windows, CLIENT.discover, CLIENT._prompt = saved


def first_with(risk):
    for name in library():
        if card_risk(name) == risk:
            return name
    return None


def read_branch(text):
    """The read path inside RevitFragment.Run: from `if (!writing)` to `else`."""
    start = text.find("if (!writing)\n")
    if start < 0:
        return ""
    end = text.find("\n            else\n", start)
    return text[start:end] if end > start else ""


def main():
    # ASK BEFORE CALLING - heron-ship 2a. On the code as it stood this name
    # does not exist, and a traceback would replace every failure below.
    refusal_of = getattr(CLIENT, "read_path_refusal", None)

    # ------------------------------------------------------------------
    print("1. The command line's read path refuses what changes the model")
    check(refusal_of is not None, "the client has a read_path_refusal to ask")
    check(card_risk(PLACER) == "MODIFY",
          "%s, the fragment in row 5b-350, is still MODIFY" % PLACER)
    check(card_risk(SELECTOR) == "EXECUTE",
          "%s, the step that arranges selection proofs, is still EXECUTE" % SELECTOR)

    passed, refused, wrong = [], [], []
    if refusal_of is not None:
        for name in library():
            risk = card_risk(name)
            said = refusal_of(ROOT, name)
            if risk in ("READ", "ANALYZE", "SUGGEST", "EXECUTE"):
                (passed if said is None else wrong).append(name)
            else:
                (refused if said else wrong).append(name)
    check(len(passed) > 100 and len(refused) > 100,
          "both halves are populated, so neither check below passes on nothing "
          "- %d through, %d refused" % (len(passed), len(refused)))
    check(wrong == [],
          "READ, ANALYZE, SUGGEST and EXECUTE go through and MODIFY and above "
          "are refused, over the whole library - wrong: %r" % wrong[:5])
    if refusal_of is not None:
        said = refusal_of(ROOT, PLACER) or ""
        check("Nothing was sent to Revit" in said and "5b-350" in said
              and "--write" in said,
              "the refusal says nothing was sent, cites the row, and names "
              "--write as the way")
        check(refusal_of(ROOT, "no-such-fragment-here") is not None,
              "a fragment whose risk cannot be read is refused, never let through")

    # ------------------------------------------------------------------
    print()
    print("2. Driven through a fake Revit")
    read_one = first_with("READ")
    record = os.path.join(_SCRATCH, "run.json")

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_fragment(PLACER)
    check(code == 2 and asked["discover"] == 0 and not bridge.sent,
          "`fragment %s` with no --write is refused before Revit is asked "
          "(exit %s, sent %r)" % (PLACER, code, bridge.sent))
    check("5b-350" in out.getvalue(), "and the refusal is printed")

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_fragment(PLACER, writing=True)
    check(code == 0 and bridge.sent == ["run_fragment_write"],
          "with --write it still goes, as run_fragment_write - sent %r" % bridge.sent)

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_fragment(read_one)
    check(code == 0 and bridge.sent == ["run_fragment_read"],
          "a READ fragment still goes as run_fragment_read - sent %r" % bridge.sent)

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_fragment(SELECTOR)
    check(code == 0 and bridge.sent == ["run_fragment_read"],
          "and so does an EXECUTE one - sent %r" % bridge.sent)

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_prove([read_one, PLACER])
    check(code == 2 and asked["discover"] == 0 and not bridge.sent,
          "`prove` refuses the whole batch before sending its first fragment "
          "(exit %s, sent %r)" % (code, bridge.sent))

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_validate(PLACER, out=record, negative_in="Other")
    check(code == 2 and asked["discover"] == 0 and not bridge.sent,
          "`validate %s` with no --write is refused before Revit is asked "
          "(exit %s, sent %r)" % (PLACER, code, bridge.sent))

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_validate(PLACER, out=record, negative_in="Other",
                                   writing=True)
    runs = [op for op in bridge.sent if op != "count_elements"]
    check(code == 0 and runs == ["run_fragment_write", "run_fragment_write"],
          "with --write both phases go as run_fragment_write - sent %r" % runs)

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_validate(read_one, out=record, negative_in="Other",
                                   setup=[PLACER])
    check(code == 2 and asked["discover"] == 0 and not bridge.sent,
          "a MODIFY setup step on a read proof is refused before anything is "
          "sent - on the read path it would run with no transaction open")

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_validate(read_one, out=record, negative_in="Other")
    runs = [op for op in bridge.sent if op != "count_elements"]
    check(code == 0 and runs == ["run_fragment_read", "run_fragment_read"],
          "and an ordinary read proof is exactly what it was - sent %r" % runs)

    # ------------------------------------------------------------------
    print()
    print("3. The add-in measures every read run")
    executor = read(EXECUTOR)
    branch = read_branch(executor)
    before = branch.find("ReadFootprint.Of(target)")
    runs_at = branch.find("RunScript(")
    after = branch.find("ReadChangedModel(")
    check(branch != "", "the read path is where this suite looks for it")
    check(before >= 0 and runs_at >= 0 and before < runs_at,
          "the model is measured BEFORE the fragment runs")
    check(runs_at >= 0 and after > runs_at,
          "and measured again after it, whether or not it threw")
    check("return changed;" in branch[after:] if after >= 0 else False,
          "a change is returned from the read path itself, before the chain "
          "or the answer is built")
    check('"read_changed_model"' in executor,
          "a changed model has its own error code, never a clean answer")
    check("WithCount(answer, counted);" in executor
          and '"elementsCounted"' in executor,
          "a clean read carries the count it was measured by, so the chat's "
          "'nothing was changed' is said from a measurement")
    check("WhereElementIsNotElementType" in executor
          and "WhereElementIsElementType" in executor
          and ".IsModified" in executor,
          "it counts placed elements and types, and reads IsModified")
    dispatcher = read(DISPATCHER)
    check('case "read_changed_model":' in dispatcher,
          "the Revit banner can name it")
    check('"elementsAppeared"' in dispatcher,
          "and the audit line keeps how many elements appeared")

    # ------------------------------------------------------------------
    print()
    print("4. The places that claimed the guarantee now qualify it")
    tools_text = read(TOOLS_PY)
    door = tools_text[tools_text.find("def door_refusal("):]
    door = door[:door.find('"""', door.find('"""') + 3)]
    check("What holds for every\n    caller is Revit's own refusal" not in door
          and "5b-350" in door,
          "heron_tools.door_refusal no longer says Revit's refusal holds for "
          "every caller, and cites the row")
    registry = read(REGISTRY_CS)
    row = registry[:registry.find('{ "run_fragment_read",')]
    row = row[row.rfind("// ANALYZE - runs a fragment"):]
    check("The guarantee is Revit's, not a" not in row and "5b-350" in row,
          "HeronOperationRegistry's run_fragment_read row cites the row and "
          "no longer calls the guarantee Revit's alone")
    summary = executor[:executor.find("internal static class RevitFragment")]
    check("CANNOT alter the model" not in summary and "5b-350" in summary,
          "RevitFragment's summary no longer says a read CANNOT alter the "
          "model, and cites the row")

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for one in FAILURES:
            print("  - %s" % one)
        return 1
    print("PASSED - the command line sends nothing that changes the model down "
          "the read path, and the add-in measures every read it runs.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
