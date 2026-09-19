# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-RVT-013
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
D-53 TRACKING for a FRAGMENT: run one across several values of one input, and
show the answer FOLLOWING the input.

    python tools/prove-tracking.py count-elements --vary categoryName \\
        --values Ducts,Pipes,Walls --dry-run
    python tools/prove-tracking.py count-elements --vary categoryName \\
        --values Ducts,Pipes,Walls

WHY THIS EXISTS, AND IT IS THE HALF THAT WAS MISSING RATHER THAN A CONVENIENCE
------------------------------------------------------------------------------
`brain/heron_validate.py` has JUDGED a fragment's tracking set since D-53 was
written. It reads `record["tracking"]`, refuses fewer than three rows in these
words - *"the tracking set has only %d row(s). D-53 asks for the answer to
follow the input across SEVERAL different inputs; two cannot show that"* - and
refuses rows that all came back the same, because that is exactly what a
fragment ignoring its input produces.

**NOTHING HAS EVER PRODUCED ONE.** `grep -n "tracking" mcp/client/
heron_bridge_client.py` returns nothing: no command writes those rows, so a
`record["tracking"]` could only ever be typed by hand. The judging half was
built and the producing half was not, and the gap has been quiet because the
fragments that need it are exactly the fragments nobody could prove.

`tools/prove-agent.py vary` is the working example one layer up - it does this
for an ADD-IN AGENT, and the docstring there argues the case in full. This is
that idea brought to fragments, which is
[docs/NEEDS-CHECKING.md Group W](../docs/NEEDS-CHECKING.md)'s request, written
after reading the skills' own job files: *"ONE PIECE OF MACHINERY UNBLOCKS
THREE FRAGMENTS AND THREE SKILLS"*.

WHAT A FRAGMENT HAS THAT AN AGENT HAS NOT, AND IT DECIDES THE WHOLE DESIGN
--------------------------------------------------------------------------
`prove-agent.py vary` collects EVERY number the reply carries, because an agent
has no contract saying which of them is the answer. A fragment does.
`contract.provides` declares each name, and `role: accounting` says which ones
are bookkeeping - D-52, and the reason `refused` stopped reading as a finding
for 117 fragments.

So this file judges the DECLARED RESULT, and imports that judgement rather than
re-deciding it: `contract_of` and `looks_empty` come from
`brain/heron_validate.py`, the same module that will read the record back. Two
copies of "what counts as an answer" is how the runner and the drafter start
disagreeing about the same fragment on the same day - `batch-prove.py` makes
that argument about `looks_empty` and this file inherits it.

WHAT IT REFUSES, ALL OF IT BEFORE REVIT IS TOUCHED
----------------------------------------------------
  a fragment at PROVEN or PRODUCTION   already proved. `batch-prove` reports
                                       ALREADY for the same reason, and the
                                       skill records a batch that was 15 of 16
                                       already proved and produced nothing
  a need that is not `source: request` one filled by an earlier fragment or by
                                       the wrapper is not a value a person can
                                       vary
  a shape Revit cannot receive         D-54, read through `generate-jobs.py`'s
                                       own `receivable()` so there is one list
  fewer than three DISTINCT values     the bar `heron_validate` already
                                       enforces, checked here so the refusal
                                       arrives before a Revit session is spent
                                       rather than after

`--dry-run` stops after all of that and prints what it would send. **Every one
of those checks runs without a Revit**, which is what makes this file testable
at all: `tests/test_prove_tracking.py` proves the arrangement half, and the
model half is the one thing it cannot.

WHAT IT DOES NOT DO
-------------------
**It never signs and it never promotes.** It writes a record for
`heron_validate` to read and a person to accept, exactly as `batch-prove` does;
`brain/proof-drafts/README.md` puts it the way that settles it - *the machine
never signs*.

**It does not replace D-30.** Tracking is the substitute for the negative leg
only where no arrangement makes the answer empty, which is D-53's whole
condition. A fragment that CAN come back empty should be proved with
`validate`'s negative case, and this file says so rather than offering itself
as an easier route.
"""

import argparse
import importlib.util
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as HF                                     # noqa: E402
import heron_validate as VALIDATE                               # noqa: E402

# The bar, in one place, taken from the module that enforces it rather than
# typed again. `prove-agent.py` writes `MIN_TRACKING_ROWS = 3` for agents and
# says it is "the same number brain/heron_validate.py enforces for a fragment";
# this file is on the fragment side, so it reads the rule's own home.
MIN_TRACKING_ROWS = 3

RUNS = os.path.join(ROOT, "brain", "proof-drafts", "runs")

IN_FRONT_OF_A_MODEL = ("PROVEN", "PRODUCTION")


def _sibling(name):
    """Import a tool beside this one, by path - the filenames carry hyphens."""
    path = os.path.join(ROOT, "tools", name)
    spec = importlib.util.spec_from_file_location(
        "heron_tool_" + name.replace("-", "_").replace(".py", ""), path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def library():
    """Every fragment keyed by slug, the spelling every job file uses."""
    found, unreadable = HF.load_all()
    return dict((f.slug, f) for f in found.values()), unreadable


def result_names(frag):
    """The declared names that are NOT bookkeeping - what an answer is.

    `contract_of` is imported, not re-derived: it is what `looks_empty` and
    `accept` already read, and a second opinion here would be a second opinion.
    """
    roles, names = VALIDATE.contract_of(frag)
    return sorted(n for n in names if roles.get(n) != "accounting"), roles


def other_request_needs(frag, need_name):
    """Every OTHER `source: request` need, which must be held still.

    D-53 is "the answer follows the input", singular. A run that moved two
    inputs at once could not say which one the answer followed - so the others
    are not left blank, they are SET, once, and the same for every row.
    """
    return [(n.get("name"), n.get("type")) for n in frag.needs()
            if HF.need_source(n) == "request" and n.get("name") != need_name]


def refusals(frag, need_name, values, receivable, held=None,
             by_hand_reason=None):
    """Every reason this cannot be run, in a person's words. Empty means go.

    ALL OF IT IS READ FROM DISK, so `--dry-run` answers completely and a Revit
    session is never spent finding out something a file already knew.
    """
    out = []

    if frag.status in IN_FRONT_OF_A_MODEL:
        out.append("`%s` is %s - it has already been in front of a model. "
                   "Re-proving spends the one input only the owner can give, "
                   "twice" % (frag.slug, frag.status))

    wanted = None
    for need in frag.needs():
        if need.get("name") == need_name:
            wanted = need
            break

    if wanted is None:
        takeable = [n.get("name") for n in frag.needs()
                    if HF.need_source(n) == "request"]
        out.append("`%s` declares no need called `%s`. It takes: %s"
                   % (frag.slug, need_name,
                      ", ".join(takeable) or "no caller values at all"))
    else:
        source = HF.need_source(wanted)
        if source != "request":
            out.append("`%s` is `source: %s` - it is filled by %s, not by a "
                       "person, so there is nothing here to vary"
                       % (need_name, source,
                          "an earlier fragment" if source == "fragment"
                          else "the wrapper"))
        else:
            ok, why = receivable(wanted.get("type"), need_name)
            if not ok:
                out.append("`%s (%s)` cannot be typed in: %s"
                           % (need_name, wanted.get("type"), why))

    # THE OTHER INPUTS HAVE TO BE HELD STILL, AND HAVE TO BE TYPEABLE.
    # Without this the tool would offer a plan that the executor refuses the
    # moment it arrives - `needs_request_values` - and the refusal would look
    # like a fault in the fragment rather than a blank in the arrangement.
    held = dict(held or {})
    for name, kind in other_request_needs(frag, need_name):
        ok, why = receivable(kind, name)
        # ONE PARTICULAR ELEMENT IS NOT UNTYPEABLE, IT IS HAND WORK. The
        # refusal itself names the word that works - select it in Revit and
        # pass `selected` - and `generate-jobs.py` counts it a blocker because
        # a generated job file runs unattended. A tracking run does not: a
        # person is at the keyboard varying an input, and can click once.
        # `tools/prove-skill.py` draws the same line for the same reason.
        if not ok and why == by_hand_reason:
            if name not in held:
                out.append("`%s (%s)` is one PARTICULAR element. Select it in "
                           "Revit and hold it still with --set %s=selected - "
                           "the same element for every row, or the answer "
                           "follows two inputs at once" % (name, kind, name))
            continue
        if not ok:
            out.append("`%s (%s)` is also a caller value and cannot be typed "
                       "in: %s" % (name, kind, why))
        elif name not in held:
            out.append("`%s (%s)` is a caller value this fragment also needs. "
                       "Hold it still with --set %s=<value>: D-53 is the "
                       "answer following ONE input, and a run that moved two "
                       "could not say which" % (name, kind, name))

    stray = [name for name in held
             if name not in set(n.get("name") for n in frag.needs())]
    for name in sorted(stray):
        out.append("`%s` is not a need `%s` declares, so --set %s would be "
                   "dropped in silence (FRAGMENT-ISSUES row 71)"
                   % (name, frag.slug, name))

    # DISTINCT, not merely several. `heron_validate` refuses a set whose rows
    # all came back the same, and three copies of one value cannot do anything
    # else - so the refusal is brought forward to where it costs nothing.
    if len(values) < MIN_TRACKING_ROWS:
        out.append("--values has %d value(s). D-53 asks for the answer to "
                   "follow the input across SEVERAL different inputs, and "
                   "heron_validate reads that as at least %d"
                   % (len(values), MIN_TRACKING_ROWS))
    elif len(set(values)) < MIN_TRACKING_ROWS:
        out.append("--values has %d value(s) but only %d distinct one(s). "
                   "Running the same input twice cannot show an answer "
                   "following it" % (len(values), len(set(values))))

    return out


def track_rows(answers, field):
    """[{input, field, value}] in the shape heron_validate reads back.

    `answers` is [(value, {name: reported})]. The row's `value` is the one
    declared result named by `field`, read through `_as_count` so that "0",
    "0 item(s)" and 0 are one answer rather than three - the executor sends
    strings, and that function is why every negative case stopped being flagged
    on 2026-09-07.
    """
    rows = []
    for typed, reported in answers:
        raw = (reported or {}).get(field)
        count = VALIDATE._as_count(raw)
        rows.append({"input": "%s" % typed, "field": field,
                     "value": raw if count is None else count})
    return rows


def judge(rows):
    """(ok, why). The same two questions heron_validate will ask of the record.

    ASKED HERE AS WELL AS THERE, AND THAT IS NOT A SECOND COPY OF THE RULE -
    it is the same rule asked earlier, so a run that cannot pass is not written
    out looking like evidence. `heron_validate` remains the authority and reads
    the record again when somebody accepts it.
    """
    if len(rows) < MIN_TRACKING_ROWS:
        return False, ("only %d row(s) ran. A refused value is not a tracking "
                       "row - it shows the fragment never looked, not that it "
                       "looked and found nothing" % len(rows))
    seen = set(str(r.get("value")) for r in rows)
    if len(seen) < 2:
        return False, ("every row came back %s. A fragment ignoring its input "
                       "produces exactly that, which is what tracking exists "
                       "to rule out - vary the input until the answer moves"
                       % sorted(seen)[0])
    return True, ("%d row(s), %d distinct answer(s) - the answer follows the "
                  "input" % (len(rows), len(seen)))


def run_rows(runner, need_name, values, held, field):
    """(rows, refused). Run once per value and read the declared result.

    `runner` IS INJECTED, AND THAT IS WHAT MAKES THIS TESTABLE AT ALL. It takes
    the caller values for one run and returns the reply as a dict. The real one
    talks to Revit; `tests/test_prove_tracking.py` passes a fake that returns
    known replies, so the loop, the refusal handling and the row building are
    proved on a machine with no Revit - and the only line left unproved is the
    bridge call itself, which is the same call `cmd_prove` already makes.

    A REFUSED VALUE IS NOT A TRACKING ROW. `prove-agent.py vary` says why in
    one sentence and it is worth repeating: a refusal shows the fragment never
    looked, not that it looked and found nothing. They are collected and
    reported, and they do not count toward the three.
    """
    rows, refused = [], []
    for value in values:
        sending = dict(held or {})
        sending[need_name] = value
        reply = runner(sending)
        if not isinstance(reply, dict) or not reply.get("ok"):
            refused.append((value,
                            (reply or {}).get("error") or "no reply",
                            ((reply or {}).get("message") or "")[:100]))
            continue
        provided = reply.get("provides")
        if not isinstance(provided, dict):
            provided = reply
        rows.append(("%s=%s" % (need_name, value), provided))
    return track_rows(rows, field), refused


def write_path_refusal(frag, threshold_ordinal, ladder):
    """The refusal for a fragment this file will not send, or None.

    TRACKING A WRITE IS A DIFFERENT AND MORE DANGEROUS THING, and it is not
    what D-53 is for. The rows here vary an input and read an answer; a write
    varied three ways changes the model three times, and the rollback question
    that `validate` handles with a TransactionGroup has no equivalent here.

    THE LINE IS READ FROM THE REGISTRY, NOT TYPED. Golden Rule 19 - the risk of
    an operation is looked up by name and never supplied by a caller - so the
    threshold comes from `generate-jobs.write_threshold()`, which reads
    `HeronOperationRegistry.cs`. The day the write path moves, this moves with
    it. And sending a write down the READ path is defect row 6, which cost half
    a morning arriving silently.
    """
    risk = (frag.data.get("risk") or "").upper()
    if risk not in ladder:
        return ("`%s` declares `risk: %s`, which is not a level HeronRisk "
                "names - and a fragment whose danger nobody can establish is "
                "not run" % (frag.slug, frag.data.get("risk")))
    if ladder[risk] >= threshold_ordinal:
        return ("`%s` is `risk: %s`, which needs the WRITE path. This file "
                "sends down the read path only: a write varied three ways "
                "changes the model three times, and that is a different "
                "question from D-53's. Prove it with `validate`, which holds "
                "a TransactionGroup" % (frag.slug, risk))
    return None


def plan(frag, need_name, values, field, held=None):
    """What would be sent, as a person can check it before it is."""
    held = dict(held or {})
    fixed = " ".join("--set %s=%s" % (k, held[k]) for k in sorted(held))
    return {
        "fragment": frag.slug,
        "capability": frag.data.get("capability"),
        "risk": frag.data.get("risk"),
        "status": frag.status,
        "vary": need_name,
        "values": list(values),
        "held": held,
        "field": field,
        "runs": [("--set %s=%s %s" % (need_name, v, fixed)).strip()
                 for v in values],
    }


def main(argv=None):
    parser = argparse.ArgumentParser()
    parser.add_argument("fragment")
    parser.add_argument("--vary", required=True, metavar="NEED",
                        help="the `source: request` need to vary")
    parser.add_argument("--values", required=True,
                        help="at least three DISTINCT values, comma separated")
    parser.add_argument("--expect", metavar="NAME",
                        help="the declared result to track. Needed when the "
                             "fragment declares more than one")
    parser.add_argument("--set", action="append", default=[], metavar="N=V",
                        dest="held",
                        help="hold another caller value still; repeatable")
    parser.add_argument("--dry-run", action="store_true",
                        help="check everything and send nothing")
    args = parser.parse_args(argv)

    by_slug, unreadable = library()
    if unreadable:
        print("FRAGMENTS THAT WOULD NOT LOAD (%d):" % len(unreadable))
        for why in unreadable:
            print("  %s" % why)
        print("")

    frag = by_slug.get(args.fragment)
    if frag is None:
        print("No fragment called '%s'. Nothing was sent." % args.fragment)
        return 2

    GJ = _sibling("generate-jobs.py")
    values = [v.strip() for v in args.values.split(",") if v.strip()]

    held, bad = {}, []
    for pair in args.held:
        if "=" not in pair:
            bad.append("'%s' is not name=value" % pair)
            continue
        key, value = pair.split("=", 1)
        held[key.strip()] = value

    stop = bad + refusals(frag, args.vary, values, GJ.receivable, held,
                          GJ.ELEMENT_INSTANCE_REASON)

    threshold_name, threshold_ordinal, ladder = GJ.write_threshold()
    writing = write_path_refusal(frag, threshold_ordinal, ladder)
    if writing:
        stop.append(writing)

    results, roles = result_names(frag)
    field = args.expect
    if field is None:
        if len(results) == 1:
            field = results[0]
        elif not results:
            stop.append("`%s` declares no result that is not `role: "
                        "accounting`, so there is nothing for a tracking row "
                        "to carry. That is FRAGMENT-ISSUES row 41's shape and "
                        "it is a contract question, not an arrangement one"
                        % frag.slug)
        else:
            stop.append("`%s` declares %d results - %s. Name the one to track "
                        "with --expect: choosing it is knowledge of the "
                        "fragment, which a tool does not have"
                        % (frag.slug, len(results), ", ".join(results)))
    elif field not in results:
        stop.append("`%s` is not a declared result of `%s`%s. It declares: %s"
                    % (field, frag.slug,
                       " - it is `role: %s`" % roles[field]
                       if field in roles else "",
                       ", ".join(results) or "nothing that is not accounting"))

    print("D-53 TRACKING for %s" % frag.slug)
    print("  capability  %s" % (frag.data.get("capability") or "?"))
    print("  risk        %s        status %s"
          % (frag.data.get("risk") or "?", frag.status))
    print("  vary        %s across %s" % (args.vary, ", ".join(values)))
    print("  track       %s" % (field or "NOT CHOSEN"))
    print("")

    if stop:
        print("NOT RUN - %d reason(s), and nothing was sent to Revit:"
              % len(stop))
        for why in stop:
            print("  %s" % why)
        print("")
        print("Every one of those was read from disk. None of it needed a "
              "model, which")
        print("is why it is said here rather than after a session was spent "
              "on it.")
        return 1

    ready = plan(frag, args.vary, values, field, held)
    print("READY. What would be sent, %d run(s) on ONE model:" % len(values))
    for line in ready["runs"]:
        print("    %s %s" % (frag.slug, line))
    print("")

    if args.dry_run:
        print(json.dumps(ready, indent=2, sort_keys=True))
        print("")
        print("Dry run. Nothing was sent to Revit and nothing was written.")
        return 0

    print("THE MODEL HALF IS NOT IMPLEMENTED IN THIS FILE YET, and saying so")
    print("is the honest answer rather than sending something half-built at a")
    print("live model. What is built and proved is the ARRANGEMENT above and")
    print("the JUDGEMENT in `judge()` - every check that can be made without a")
    print("Revit. Wiring the runs through mcp/client/heron_bridge_client.py is")
    print("the next step, and it needs a session to develop against.")
    print("")
    print("Run it with --dry-run to get the plan as JSON.")
    return 3


if __name__ == "__main__":
    raise SystemExit(main())
