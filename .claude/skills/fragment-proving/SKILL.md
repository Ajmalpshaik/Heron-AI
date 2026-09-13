---
name: fragment-proving
description: Use when proving fragments against a real Revit model - one at a time or a batch at once. Covers which fragments may be attempted at all, how to arrange a case so the answer means something, the five mistakes that account for nearly every failed proof (a selection too big, a positive case the model cannot fill, a category not visible in the view chosen, a value in the wrong units or shape, an answer given on a stale selection), how to write a job file for tools/batch-prove.py, how to read each verdict it reports, and what it deliberately cannot judge. Trigger on "prove this fragment", "prove a batch", "run fragments against the model", "why did the negative case fail", "arrange a proof", "batch-prove", or any work that ends in a proof draft.
---

# Proving fragments

A fragment is proved when it has been run against a real model and both halves held: it found
something when the model had it, and it found **nothing** when the model did not
([D-30](../../../docs/DECISIONS.md)). Compiling proves nothing. Running once proves nothing either — a
fragment that succeeds while doing nothing passes ten runs and a thousand.

Two halves, and they belong to different people:

| | |
|---|---|
| **Arranging the case** | Which fragments, which selection, which values, and what makes the answer empty. **This document.** Nearly every failed proof is here |
| **Running and judging** | [`tools/batch-prove.py`](../../../tools/batch-prove.py). Mechanical, no judgement about the model |

Neither may sign. Accepting a draft is a person typing their own name
([`brain/proof-drafts/README.md`](../../../brain/proof-drafts/README.md)).

## Before anything else: only attempt what is unproven

```bash
grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c
python brain/heron_validate.py plan
```

Picking names off a capability list, a to-do note or a conversation is how one batch came to be **15
of 16 fragments already PROVEN** — the run reported six passes and produced nothing. `batch-prove`
refuses a fragment at PROVEN or PRODUCTION and reports `ALREADY`, so the job file cannot repeat that
mistake; do not build one that leans on the refusal to find out.

## The five rules

Each was learned by breaking it. The full account of every one, with what was passed and what came
back, is in [docs/FRAGMENT-ISSUES.md](../../../docs/FRAGMENT-ISSUES.md).

### 1. Prove on a small selection

A heavy write on a big selection is not a stronger test, it is a slower one — and **a timeout says
nothing at all about the fragment.** `set-mep-size` timed out on 307 ducts and sized all 22
immediately when retried on a smaller view.

Find the small one before starting, and put it in the job file's `defaults:`. In the sample HVAC
model, `select-by-category-name --set "inViewOnly=FloorPlan: M1"` gives 22 ducts where
`FloorPlan: L3` gives 307. Every model has such a pair; look for it rather than assuming this one.

### 2. Ask for what the model has

D-30 is written about the negative case, so the **positive** quietly goes unarranged — and then the
positive is the empty half. That happened five times in one day: open duct ends asked for in a model
with none, a minimum slope nothing falls below, a coverage radius that leaves no gaps, a family
pattern nothing matches.

Before writing a value, know what the model would answer. A defect-finder needs the defect to exist:
on a clean model, build it on purpose or set the fragment aside as needing content this model lacks.

### 3. Check the category is visible where you select

The setup chain selects **in a view**. Sheets do not appear in a floor plan; levels do not appear in
their own plan. Twice the setup found nothing and the run read as a missing selection rather than as a
category that was never going to be there.

This applies to the negative case as much as the positive — the negative is a *different selection*,
not a cleared one (see rule 5's note below), and it has to be a selection that actually exists.

### 4. Match the input to the model's own units and shapes

`set-mep-size` was handed a width and a height for ducts that are **round**. An imperial model's ducts
come in diameters like 102, 152 and 203 mm, and a width on a round duct does nothing at all — which
reads as a fragment that ignored its input.

Ask what shape the thing is, and what units the model works in, before typing a number.

### 5. Read the binding note on the answer

Every reply says where its inputs came from — `elements from the selection (10)`. `find-overlapping-lines`
once answered on a **stale selection of ten equipment items** and looked perfectly healthy; only that
note gave it away.

If the count on the binding note is not the count you arranged, the answer is about something else.
Read it before reading the result.

## Writing a job file

Start from [`tools/jobs/example.yaml`](../../../tools/jobs/example.yaml).

```bash
python tools/batch-prove.py my-jobs.yaml --dry-run    # check it, run nothing
python tools/batch-prove.py my-jobs.yaml              # run it
```

`--dry-run` reads the file against the fragment library and answers, before Revit is touched, whether
every fragment exists, whether any is already proved, whether the setup chain is real, whether
`expect:` names something the fragment provides, and whether a negative case is arranged at all. It
prints the exact command each job would send. **Always dry-run first** — a batch of fourteen should
not discover on the eleventh that a name was mistyped on the first.

Three things the shape of the file is doing:

- **`defaults:` carries the selection, and a job overrides only what differs.** One arrangement typed
  once. Retyping it per job is how one copy comes to name a different view than the rest.
- **`setup:` is re-run before each phase, in order.** A rolled-back write clears the Revit selection,
  so the arrangement has to be re-made rather than made once. Only the first step resets the chain —
  step two consumes what step one left.
- **A need that can ONLY come from the chain wants `--keep-chain`, and without it the setup's work is
  thrown away.** The fragment under test resets the chain by default, *after* the setup steps filled
  it — so `group-and-count` asking for `values` was told *"no earlier fragment in this session left a
  value of that name"* while `read-element-parameters`, one step earlier in the same run, had just
  produced 25 of them. **Selection-shaped needs do not show this**, because `set-selection` writes
  Revit's own selection and a chain reset cannot touch that; every proof that chains
  `select-in-region → set-selection` works without the flag and hides the problem. With it:
  `values from read-element-parameters (25)`. The flag is deliberately opt-in — see `cmd_validate`
  and defect row 11 — because a kept chain outranks the selection.
- **A job with no `negative-set:` or `negative-in:` is refused.** Without one, `validate` stops and
  waits for somebody at the keyboard, which in a batch is a hang. It is also the leg D-30 exists for.

**Set `HERON_CLIENT_ID`** for anything driven from a command line. The lease identifies a *chat*, and
a command that exits leaves one held for five minutes, so the second call is refused. `batch-prove`
pins one id across the whole batch by itself; a hand-run `validate` or `prove` needs it set.

## Reading the report

| Verdict | What it means | What to do |
|---|---|---|
| `PASS` | both halves held | read the draft, then accept it under your own name |
| `ALREADY` | at PROVEN or PRODUCTION | nothing. Remove it from the job file |
| `POSITIVE EMPTY` | it ran and found nothing | **rule 2.** The arrangement is wrong, not the fragment |
| `POSITIVE UNREADABLE` | nothing it returned is a quantity | read the fragment's own source. Its results may need a `role:` |
| `NEG NOT EMPTY` | the negative returned content | a **finding**. The fragment may be falling back to something it was not given |
| `NO NEGATIVE` | the second leg never ran | usually `needs_unbound` — the negative selection was empty. **Rule 3** |
| `DID NOT RUN` | the positive never ran | read the message. Often a caller value the contract asks for |
| `TIMEOUT` | Revit did not answer in time | **rule 1.** Retry on a smaller selection. It says nothing about the fragment |
| `REFUSED` / `NO FRAGMENT` | the job file cannot be run as written | fix the file. Nothing was sent to Revit |

A failure is the tool's **output**, not an error — it exits 0 with fourteen failures and non-zero only
when the batch itself could not run. Set the failures aside and pass the clean ones.

## What the runner cannot judge, and where `expect:` comes in

It judges the positive by asking whether a **declared result moved off zero**, using the fragment's own
contract: names declared `role: accounting` are skipped, and so are names matching the bookkeeping
patterns [`brain/heron_validate.py`](../../../brain/heron_validate.py) already owns. Two shapes get
past that, and both were met on real runs:

- **A work counter the patterns miss.** `comparedCount 22` counts what was *looked at*.
- **An echo of the input.** `wanted 1 item(s) [Comments]` is the field the caller asked for, non-zero
  in both legs whatever the fragment does.

When a fragment has one, name the real result in the job file and the runner judges only that:

```yaml
  - fragment: compare-elements
    expect: differing
```

If a fragment's own contract is what is wrong — a work counter declared as a result — fix the contract.
`role: accounting` on the `provides` entry is the fragment saying so about itself, and it is worth more
than a line in a job file because every later run gets it too.

## Fragments that cannot come back empty

Some fragments have no empty case in any model — `count-elements` describes whatever it is handed;
every Revit view hides something, so a visibility report always reports. For those, D-30's second leg
is met by **tracking** ([D-53](../../../docs/DECISIONS.md)): the answer follows the input exactly
across several different inputs. A fragment falling back to the active view or the whole model cannot
match five different counts.

That is not what `batch-prove` does. Prove those one at a time with `validate`, and let
`heron_validate` draft the tracking rows.

## Honesty

- **A pass here is not a proof.** It means the evidence held. The signature is still owed, and it is a
  person's.
- **A fragment that fails in front of a model belongs in
  [docs/FRAGMENT-ISSUES.md](../../../docs/FRAGMENT-ISSUES.md) the same day**, with what was passed to
  it and what came back, verbatim. The value of that list is that every row was observed rather than
  expected.
- **Never record a phase that did not run.** A draft saying "with nothing selected it returned 0" when
  nothing checked is worse than no draft, because it looks like evidence.
