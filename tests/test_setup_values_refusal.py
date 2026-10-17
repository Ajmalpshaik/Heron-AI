#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Setup values for a step that changes the model are refused before anything is sent - row 5b-917.

    python tests/test_setup_values_refusal.py

WHAT HAPPENED
-------------
`validate --write --setup-set ...` with a setup step at MODIFY is refused:
such a step travels inside the fragment's own transaction group, and the
add-in binds it from the run's values, so a value meant only for the setup
has nowhere to go. Two things were wrong around that refusal, measured
2026-10-17 through a fake Revit with `delete-revision`'s arrangement:

  - `validate` refused only AFTER it had asked Revit for `count_elements`,
    while the fragment-proving skill said it refused "before Revit is asked".
  - `tools/batch-prove.py --dry-run` said WOULD RUN for such a job, so a
    batch would discover the refusal one job at a time - the row 5b-440
    shape. Five revision job rows in tools/jobs/ carried it.

WHAT THIS HOLDS
---------------
  1. THE RULE, ON ITS OWN. `setup_values_refusal` names every step that
     changes the model, and lets a chain that only reads through.
  2. DRIVEN THROUGH A FAKE REVIT, by the command line a person types.
     `validate --write` with setup values and a MODIFY step is refused before
     Revit is asked anything; with a chain that only reads it still runs and
     the chain gets its own values.
  3. THE BATCH RUNNER SAYS SO IN A DRY RUN. `job_refusal` refuses such a job
     as REFUSED, naming the step; a read-only chain with `setup-set:` stays
     runnable.

WHAT IT CANNOT DO
-----------------
Give a setup step that changes the model values of its own. That needs the
add-in to read per-step values - C#, and a real Revit to prove.
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
_SCRATCH = tempfile.mkdtemp(prefix="heron-setup-values-")
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

# THE ARRANGEMENT THE GAP WAS MEASURED ON, by name. Section 1 checks each
# still carries the risk this suite leans on.
UNDER_TEST = "delete-revision"             # MODIFY
MAKER = "create-revision"                  # MODIFY - a setup step that makes one
SELECTOR = "select-by-parameter-value"     # READ

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def card_risk(name):
    return CLIENT.fragment_risk(os.path.join(FRAGMENTS, name, "fragment.yaml"))


class FakeBridge(object):
    """A Revit that answers every request and writes down what it was sent."""

    revit_version = "2020"
    pid = 4242

    def __init__(self):
        self.sent = []

    def request(self, op, **kwargs):
        args = kwargs.get("op_args") or {}
        self.sent.append((op, args.get("name"), args.get("values")))
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


def validate(*words):
    """The command line a person types, through the client's own `main`."""
    record = os.path.join(_SCRATCH, "run.json")
    return CLIENT.main(["heron_bridge_client.py", "validate", "--out", record]
                       + list(words))


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
    rule = getattr(CLIENT, "setup_values_refusal", None)

    # ------------------------------------------------------------------
    print("1. The rule, on its own")
    check(rule is not None, "the client has a setup_values_refusal to ask")
    check(card_risk(UNDER_TEST) == "MODIFY", "%s is still MODIFY" % UNDER_TEST)
    check(card_risk(MAKER) == "MODIFY", "%s is still MODIFY" % MAKER)
    check(card_risk(SELECTOR) == "READ", "%s is still READ" % SELECTOR)

    if rule is None:
        rule = lambda steps: None   # so the checks below still RUN and FAIL

    said = rule([(SELECTOR, "READ"), (MAKER, "MODIFY")]) or ""
    check(said != "", "setup values with a MODIFY step are refused")
    check(MAKER in said and SELECTOR not in said,
          "the refusal names the step that changes the model, and only it: %r"
          % said[:120])
    check("Nothing was sent to Revit" in said, "and says nothing was sent")
    check("5b-917" in said, "and cites the row")
    check(rule([(SELECTOR, "READ")]) is None and rule([]) is None,
          "a chain that only reads, or none, goes through")

    # ------------------------------------------------------------------
    print()
    print("2. Driven through a fake Revit")
    arranged = ["--write", "--setup", SELECTOR, "--setup", MAKER, "--keep-chain",
                "--set", "confirm=Seq. 2", "--negative-set", "confirm=",
                "--setup-set", "categories=Revisions",
                "--negative-setup-set", "categories=Revisions"]

    with fake_revit() as (bridge, asked, out):
        code = validate(*(arranged + [UNDER_TEST]))
    check(code == 2, "`validate --write --setup-set` with %s in the setup is "
          "refused (exit %s)" % (MAKER, code))
    check(asked["discover"] == 0 and not bridge.sent,
          "before Revit is asked anything - asked %d time(s), sent %r"
          % (asked["discover"], [op for op, _, _ in bridge.sent]))
    check("setup values cannot reach" in out.getvalue() and MAKER in out.getvalue(),
          "and the refusal printed names the step")

    with fake_revit() as (bridge, asked, out):
        code = validate("--write", "--setup", SELECTOR, "--keep-chain",
                        "--set", "confirm=Seq. 2", "--negative-set", "confirm=",
                        "--setup-set", "categories=Revisions",
                        "--negative-setup-set", "categories=Revisions", UNDER_TEST)
    chain = [values for op, name, values in bridge.sent if name == SELECTOR]
    check(code == 0 and len(chain) == 2,
          "a chain that only reads still runs with --setup-set, once per phase "
          "(exit %s, sent %r)" % (code, [(op, n) for op, n, _ in bridge.sent]))
    check(all(v and "Revisions" in str(v) for v in chain),
          "and the chain is given its own values: %r" % chain[:1])

    # ------------------------------------------------------------------
    print()
    print("3. The batch runner says so in a dry run")
    bp = _load(os.path.join(ROOT, "tools", "batch-prove.py"), "batch_prove")
    library = dict((n, Card(n)) for n in (UNDER_TEST, MAKER, SELECTOR))
    base = {"fragment": UNDER_TEST, "set": {"confirm": "Seq. 2"},
            "negative-set": {"confirm": ""}, "write": True, "cross": None,
            "in": None, "negative-in": None, "expect": None, "timeout": 60,
            "setup-set": {"categories": "Revisions"},
            "negative-setup-set": {"categories": "Revisions"}}

    verdict, why = bp.job_refusal(dict(base, setup=[SELECTOR, MAKER]), library)
    check(verdict == bp.REFUSED,
          "a job giving `setup-set:` with %s in its setup is REFUSED (%s)"
          % (MAKER, verdict))
    check(MAKER in (why or ""), "naming the step: %r" % (why or "")[:120])

    verdict, why = bp.job_refusal(
        dict(base, setup=[SELECTOR, MAKER], **{"setup-set": {}}), library)
    check(verdict == bp.REFUSED,
          "`negative-setup-set:` alone is refused the same way (%s)" % verdict)

    verdict, why = bp.job_refusal(dict(base, setup=[SELECTOR]), library)
    check(verdict is None,
          "a chain that only reads, with `setup-set:`, stays runnable (%s %s)"
          % (verdict, why))

    verdict, why = bp.job_refusal(
        dict(base, setup=[SELECTOR, MAKER],
             **{"setup-set": {}, "negative-setup-set": {}}), library)
    check(verdict is None,
          "the same chain with no setup values stays runnable (%s %s)"
          % (verdict, why))

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for one in FAILURES:
            print("  - %s" % one)
        return 1
    print("PASSED - setup values for a step that changes the model are refused "
          "before anything reaches Revit, and a dry run says so.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
