#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A `prove` run is a plan, and a plan stops at the step that failed - row 148.

    python tests/test_prove_plan.py

WHAT HAPPENED
-------------
On 2026-09-19 the `count-elements` skill's plan was run end to end on a real
model with `prove` - the first time any skill's composition met a model.
Handed values it could not bind, `filter-elements-by-category` refused with
`needs_request_values`, and `count-elements` RAN ANYWAY and answered
`count 1` - a floor left selected by an earlier probe. A confident number a
modeller would believe, from a step whose producer never ran.

WHAT THIS HOLDS, THROUGH A FAKE REVIT
-------------------------------------
  1. A STEP THAT FAILS STOPS THE PLAN. The steps after it are not sent, each
     is named NOT RUN with the step that stopped it, and the run exits 1.
  2. A STEP WITH NO REPLY STOPS IT TOO - nothing says what that step left.
  3. `--keep-going` runs the rest anyway, for a batch of fragments that do
     not hand anything on - and says each one after the failure answers from
     what was left before it.
  4. A PLAN WHOSE STEPS ALL RUN is unchanged: every step sent, exit 0.
  5. The command line takes `--keep-going`, and the skill job files'
     `prove` line does not pass it.

WHAT IT CANNOT DO
-----------------
Run a plan in Revit. Whether the stop reads right on the owner's PC, on the
count-elements plan that found this, is owed there (NEEDS-CHECKING PD).
"""

import contextlib
import io
import os
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The client reads heron.config for its timeouts. Point it at a file that is
# not there, so the owner's own settings are neither read nor needed.
_SCRATCH = tempfile.mkdtemp(prefix="heron-prove-plan-")
os.environ["HERON_CONFIG"] = os.path.join(_SCRATCH, "heron.config")

sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_bridge_client as CLIENT                          # noqa: E402

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

# The plan of the row: the count-elements skill's two steps, both READ.
FILTER = "filter-elements-by-category"
COUNT = "count-elements"

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


class FakeBridge(object):
    """A Revit whose answer to each fragment is set by the test."""

    revit_version = "2024"
    pid = 4242

    def __init__(self, answers):
        self.answers = answers
        self.ran = []

    def request(self, op, op_args=None, **kwargs):
        if op == "count_elements":
            return {"ok": True, "document": "Fake model", "count": 3}
        name = (op_args or {}).get("name")
        self.ran.append(name)
        return self.answers.get(name, {"ok": True, "document": "Fake model",
                                       "provides": {}})

    def release(self):
        pass

    def close(self):
        pass


@contextlib.contextmanager
def fake_revit(answers):
    bridge = FakeBridge(answers)
    saved = (CLIENT.refuse_off_windows, CLIENT.discover)
    CLIENT.refuse_off_windows = lambda: False
    CLIENT.discover = lambda prune=True: ([bridge], [], [], [])
    out = io.StringIO()
    try:
        with contextlib.redirect_stdout(out):
            yield bridge, out
    finally:
        CLIENT.refuse_off_windows, CLIENT.discover = saved


REFUSED = {"ok": False, "error": "needs_request_values",
           "message": "category was not given"}
STALE = {"ok": True, "document": "Fake model", "provides": {"count": "1"},
         "bound": "elements from the selection (1)"}


def prove(names, **kw):
    try:
        return CLIENT.cmd_prove(names, **kw)
    except TypeError as exc:          # an old cmd_prove with no keep_going
        print("cmd_prove refused %r: %s" % (kw, exc))
        return None


def section_1():
    print("1. a step that fails stops the plan")
    with fake_revit({FILTER: REFUSED, COUNT: STALE}) as (bridge, out):
        code = CLIENT.cmd_prove([FILTER, COUNT])
    said = out.getvalue()
    check(bridge.ran == [FILTER],
          "the step after the failed one is never sent - sent %r" % bridge.ran)
    check(code == 1, "the run exits 1 (%r)" % code)
    lines = [line for line in said.splitlines() if COUNT in line]
    check(any("NOT RUN" in line and FILTER in line for line in lines),
          "and the step left out is named NOT RUN, with the step that stopped it - %r"
          % lines)
    check("count 1" not in said and "count  " not in said,
          "no number from a step that should not have run is printed")


def section_2():
    print("2. a step with no reply stops it too")

    class Silent(FakeBridge):
        def request(self, op, op_args=None, **kwargs):
            if op != "count_elements" and (op_args or {}).get("name") == FILTER:
                self.ran.append(FILTER)
                return None
            return FakeBridge.request(self, op, op_args=op_args, **kwargs)

    bridge = Silent({COUNT: STALE})
    saved = (CLIENT.refuse_off_windows, CLIENT.discover)
    CLIENT.refuse_off_windows = lambda: False
    CLIENT.discover = lambda prune=True: ([bridge], [], [], [])
    out = io.StringIO()
    try:
        with contextlib.redirect_stdout(out):
            code = CLIENT.cmd_prove([FILTER, COUNT])
    finally:
        CLIENT.refuse_off_windows, CLIENT.discover = saved
    check(bridge.ran == [FILTER] and code == 1,
          "nothing after a step with no reply is sent (sent %r, exit %r)" % (bridge.ran, code))


def section_3():
    print("3. --keep-going runs the rest, and says what they answer from")
    with fake_revit({FILTER: REFUSED, COUNT: STALE}) as (bridge, out):
        code = prove([FILTER, COUNT], keep_going=True)
    said = out.getvalue()
    check(bridge.ran == [FILTER, COUNT],
          "with keep_going every step is sent - sent %r" % bridge.ran)
    check(code == 1, "and the run still exits 1, because a step failed (%r)" % code)
    check("after a step that failed" in said,
          "each step after the failure says it answers from what was left before it")


def section_4():
    print("4. a plan whose steps all run is unchanged")
    good = {FILTER: {"ok": True, "document": "Fake model",
                     "provides": {"elements": "8 item(s)"}},
            COUNT: {"ok": True, "document": "Fake model", "provides": {"count": "8"},
                    "bound": "elements from filter-elements-by-category (8)"}}
    with fake_revit(good) as (bridge, out):
        code = CLIENT.cmd_prove([FILTER, COUNT])
    check(bridge.ran == [FILTER, COUNT] and code == 0,
          "both steps sent, exit 0 (sent %r, exit %r)" % (bridge.ran, code))
    check("NOT RUN" not in out.getvalue(), "and nothing is called NOT RUN")


def section_5():
    print("5. the command line")
    with fake_revit({FILTER: REFUSED, COUNT: STALE}) as (bridge, out):
        code = CLIENT.main(["heron_bridge_client.py", "prove", FILTER, COUNT,
                            "--keep-going"])
    check(bridge.ran == [FILTER, COUNT],
          "`prove ... --keep-going` runs every step (sent %r, exit %r)" % (bridge.ran, code))
    job = io.open(os.path.join(ROOT, "tools", "jobs", "skills",
                               "count-elements-model-half.yaml"), encoding="utf-8").read()
    check("prove \\" in job and "--keep-going" not in job,
          "and a skill's job file runs its plan without it, so the plan stops")


def main():
    for section in (section_1, section_2, section_3, section_4, section_5):
        section()
        print("")
    if FAILURES:
        print("%d failure(s):" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("A prove run stops at the step that failed, unless told to keep going.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
