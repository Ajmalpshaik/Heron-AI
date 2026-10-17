#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A setup is run in the order it was typed, or refused before anything is sent - row 5b-917.

    python tests/test_setup_order.py

WHAT HAPPENED
-------------
On 2026-10-17, on Revit 2020 (row 5b-520, finding 1), a proof was arranged
with `validate <fragment> --write --setup <make two text notes> --setup
select-by-category-name`. The selector handed on 0 - `elements from ... (0)` -
because `cmd_validate` runs every READ or EXECUTE setup step FIRST, each as its
own read (`arrange`), and sends every MODIFY step inside the fragment's own
rolled-back transaction group (`deferred_setup`). So the selector looked at the
model before the notes existed. A view a setup step makes could not be named in
a later read step's view value either: "No view called ...".

WHAT THIS HOLDS
---------------
  1. THE RULE, ON ITS OWN. `setup_order_refusal` refuses a step that runs as
     a read typed after a step that changes the model, naming both steps and
     their risks, and lets through every order that runs as typed.
  2. DRIVEN THROUGH A FAKE REVIT. `validate --write` with the order that
     cannot run as typed is refused before Revit is asked anything, and the
     order that can still runs - its read steps reaching Revit before the
     changes, which is the order typed. Without --write nothing moved: a
     MODIFY setup step is refused there already (row 5b-350).
  3. THE BATCH RUNNER SAYS SO IN A DRY RUN. `job_refusal` refuses such a job
     as REFUSED, so a dry run never reads WOULD RUN for a job `validate` will
     turn away.

WHAT IT CANNOT DO
-----------------
Run a setup in Revit. Whether a read step could instead run INSIDE the
fragment's group, in its typed place, is a question for the add-in and a
real model.
"""

import contextlib
import io
import os
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")

# The client reads heron.config for its timeouts. Point it at a file that is
# not there, so the owner's own settings are neither read nor needed.
_SCRATCH = tempfile.mkdtemp(prefix="heron-setup-order-")
os.environ["HERON_CONFIG"] = os.path.join(_SCRATCH, "heron.config")

sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tools"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_bridge_client as CLIENT                          # noqa: E402

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

# THE FRAGMENTS OF THE ROW'S SHAPE, by name. Section 1 checks each still
# carries the risk this suite leans on, so a card that changed fails there
# rather than making the sections after it vacuous.
UNDER_TEST = "edit-text-values"            # MODIFY - the fragment in row 5b-520
MAKER = "create-text-note"                 # MODIFY - a setup step that makes notes
SELECTOR = "select-by-category-name"       # READ   - the step that handed on 0
PICKER = "set-selection"                   # EXECUTE - runs as a read as well

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def card_risk(name):
    return CLIENT.fragment_risk(os.path.join(FRAGMENTS, name, "fragment.yaml"))


class FakeBridge(object):
    """A Revit that answers every request and writes down what it was sent:
    the operation, the fragment it ran, and any steps carried inside it."""

    revit_version = "2020"
    pid = 4242

    def __init__(self):
        self.sent = []

    def request(self, op, **kwargs):
        args = kwargs.get("op_args") or {}
        carried = [s.get("name") for s in (args.get("setup") or [])]
        self.sent.append((op, args.get("name"), carried))
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


def run_order(sent):
    """What reached Revit, step by step, in the order it would run there.

    A step carried inside a write runs before the fragment that carries it."""
    order = []
    for op, name, carried in sent:
        if op == "count_elements":
            continue
        order.extend(carried)
        order.append(name)
    return order


class Card(object):
    """A library entry for the batch runner: a slug and the folder its card is in."""

    def __init__(self, slug):
        self.slug = slug
        self.status = "DRAFT"
        self.folder = os.path.join(FRAGMENTS, slug)

    def provides(self):
        return []


def main():
    # ASK BEFORE CALLING - heron-ship 2a. On the code as it stood this name
    # does not exist, and a traceback would replace every failure below.
    rule = getattr(CLIENT, "setup_order_refusal", None)

    # ------------------------------------------------------------------
    print("1. The rule, on its own")
    check(rule is not None, "the client has a setup_order_refusal to ask")
    check(card_risk(UNDER_TEST) == "MODIFY",
          "%s, the fragment in row 5b-520, is still MODIFY" % UNDER_TEST)
    check(card_risk(MAKER) == "MODIFY", "%s is still MODIFY" % MAKER)
    check(card_risk(SELECTOR) == "READ", "%s is still READ" % SELECTOR)
    check(card_risk(PICKER) == "EXECUTE", "%s is still EXECUTE" % PICKER)

    if rule is None:
        rule = lambda steps: None   # so the checks below still RUN and FAIL

    said = rule([(MAKER, "MODIFY"), (SELECTOR, "READ")]) or ""
    check(said != "", "a READ step typed after a MODIFY step is refused")
    check(MAKER in said and SELECTOR in said,
          "the refusal names both steps: %r" % said[:120])
    check("MODIFY" in said and "READ" in said, "and the risk of each")
    check("Nothing was sent to Revit" in said, "and says nothing was sent")
    check("5b-917" in said, "and cites the row")

    said = rule([(SELECTOR, "READ"), (MAKER, "MODIFY"), (PICKER, "EXECUTE")]) or ""
    check(PICKER in said and MAKER in said and SELECTOR not in said,
          "an EXECUTE step after a MODIFY one is refused too, and the READ "
          "step typed before both is not blamed: %r" % said[:120])

    for steps in ([], [(SELECTOR, "READ")], [(MAKER, "MODIFY")],
                  [(SELECTOR, "READ"), (PICKER, "EXECUTE"), (MAKER, "MODIFY"),
                   (UNDER_TEST, "MODIFY")]):
        check(rule(steps) is None,
              "%s runs as typed, so it goes through"
              % (" -> ".join(s for s, _ in steps) or "no setup"))

    # ------------------------------------------------------------------
    print()
    print("2. Driven through a fake Revit")
    record = os.path.join(_SCRATCH, "run.json")

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_validate(UNDER_TEST, out=record, negative_in="Other",
                                   writing=True, setup=[MAKER, SELECTOR])
    check(code == 2 and asked["discover"] == 0 and not bridge.sent,
          "`validate --write --setup %s --setup %s` is refused before Revit is "
          "asked (exit %s, sent %r)" % (MAKER, SELECTOR, code, bridge.sent[:3]))
    check(MAKER in out.getvalue() and SELECTOR in out.getvalue(),
          "and the refusal printed names both steps")

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_validate(UNDER_TEST, out=record, negative_in="Other",
                                   writing=True, setup=[SELECTOR, MAKER])
    order = run_order(bridge.sent)
    check(code == 0 and order == [SELECTOR, MAKER, UNDER_TEST,
                                  SELECTOR, MAKER, UNDER_TEST],
          "typed read-first, it still runs - and each phase reaches Revit in "
          "the order typed: %r" % order)

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_validate(UNDER_TEST, out=record, negative_in="Other",
                                   setup=[MAKER, SELECTOR])
    check(code == 2 and asked["discover"] == 0 and not bridge.sent,
          "without --write it is refused as it was, for the MODIFY step on "
          "the read path (exit %s)" % code)
    check("5b-350" in out.getvalue(), "and for that reason, not this one")

    # ------------------------------------------------------------------
    print()
    print("3. The batch runner says so in a dry run")
    bp = _load(os.path.join(ROOT, "tools", "batch-prove.py"), "batch_prove")
    library = dict((n, Card(n)) for n in (UNDER_TEST, MAKER, SELECTOR, PICKER))
    base = {"fragment": UNDER_TEST, "set": {"mode": "replace"},
            "negative-set": {"mode": "set"}, "write": True, "cross": None,
            "in": None, "negative-in": None, "expect": None, "timeout": 60}

    verdict, why = bp.job_refusal(dict(base, setup=[MAKER, SELECTOR]), library)
    check(verdict == bp.REFUSED, "a job typed change-first is REFUSED (%s)" % verdict)
    check(MAKER in (why or "") and SELECTOR in (why or ""),
          "naming both steps: %r" % (why or "")[:120])

    verdict, why = bp.job_refusal(dict(base, setup=[SELECTOR, MAKER]), library)
    check(verdict is None, "typed read-first, it is runnable (%s %s)" % (verdict, why))

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for one in FAILURES:
            print("  - %s" % one)
        return 1
    print("PASSED - a setup runs in the order it was typed, or is refused "
          "before anything reaches Revit.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
