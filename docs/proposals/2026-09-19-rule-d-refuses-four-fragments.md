# Proposals — 2026-09-19

> One section of [the register](../PROPOSALS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 2026-09-19 — rule (d) refuses four fragments, and only two of them deserve it

**A PROPOSAL, AND NO CODE CHANGED.** It relaxes a guard, and a guard that was written for a real
reason, so it is Ajmal's call rather than a session's.

### Where it came from

Building the `IList<Element>` rule above, the session driving Revit 36908 asked whether
`find-nearest-elements` and `check-minimum-clearance` should have `targets` flipped from
`source: fragment` to `source: request`, which would have let the new category rule fill it.
**Measured: it would work** — with that one line changed in each, `GJ.blockers` returns nothing and
both are emitted as jobs. **And it was declined, correctly**, with a model measurement behind it:

> `check-minimum-clearance` on 8 ducts, `targets` bound to `elements` — 56 pairs checked, 10 too
> close at a 5 m threshold, and 4 still too close at 0.001 mm, because four of those ducts are
> joined end to end and their gap really is zero.

That is a correct and useful answer to *"are any of these ducts too close to each other"*, which is
the clearance check somebody runs on a real job. It is not a degraded stand-in for the real one.
`binds: elements` is the fragment saying **same set, on purpose**, and `RevitFragment.Binds()` exists
to honour exactly that. Flipping the source would delete the default and change what the fragment
MEANS rather than how it is fed.

### The mechanism, measured rather than reasoned about

Rule (d) fires on **exactly four fragments in the library**, and here is the part that decides the
shape of any fix:

| fragment | the two needs | how the duplicate arises |
|---|---|---|
| `check-minimum-clearance` | `elements`, `targets` | `targets` declares `binds: elements` |
| `find-nearest-elements` | `elements`, `targets` | `targets` declares `binds: elements` |
| `switch-join-order` | `first`, `second` | **both** declare `binds: elements` |
| `unjoin-geometry` | `first`, `second` | **both** declare `binds: elements` |

**In all four, the duplicate comes from an explicitly declared `binds:`.** There is no case in the
library where rule (d) fires without one. So *"exempt a fragment that declared `binds:` on purpose"*
— the obvious relaxation, and the one proposed — **would delete rule (d) outright**, not narrow it.

### And the four are not the same case. They split two and two

The two clearance-and-distance fragments measure a set **against itself**, and that is the job.

The other two do not, **and both say so in their own purpose text**. `unjoin-geometry`:

> "A join is a pair, and this takes pairs. Handing in a set and unjoining everything from everything
> is a different and much more destructive operation."

`first` and `second` are the two **sides of a pair**, element by element. Filling both from one set
pairs every element with itself — a no-op at best, and against a fragment whose own text warns that
the set-shaped version is *much more destructive*. Rule (d) is right about those two and should keep
refusing them.

### So the discriminator cannot be `binds:`, and the proposal is that the contract says which

`binds: elements` is currently carrying two different meanings — *"the same set, deliberately"* and
*"one side of a pair that happens to come from the same chain"* — and rule (d) cannot tell them
apart, which is why it refuses all four. **Nothing in a contract distinguishes them today.**

Three ways out, in the order they seem worth considering:

1. **A contract key that says it.** Something like `same-set-is-valid: true` on the second need, set
   on the two that mean it and absent on the two that do not. Rule (d) then exempts by declaration
   rather than by guessing, and the two destructive ones stay guarded. Costs: a new key, and two
   `fragment.yaml` edits.
2. **A per-fragment allowlist in `generate-jobs.py`.** Cheaper and worse: the knowledge lives in the
   tool rather than in the fragment that owns it, which is the arrangement this repository argues
   against everywhere else.
3. **Leave it.** Two fragments stay unarrangeable by the batch runner and reachable by hand, which
   is what happens today and is not nothing — the hand route is how the measurement above was taken.

**A note on what else it touches.** The batch runner refuses these; the exploring/`validate` path
runs them, because it honours `binds:` and has no equivalent guard. Whatever is decided has to be
decided for both, or the two paths keep disagreeing and the one a person uses to decide what goes in
a job file is the unguarded one.

### The separate thing underneath it

`check-minimum-clearance`'s purpose opens *"Measures the real gap between TWO SETS of elements"*, and
with `binds:` and no `source:` there is **no route by which a caller can ever supply a second,
different set**. So the contract promises something unreachable. Either a `source: request` route
should exist alongside the bind — which the new `IList<Element>` rule would now serve — or the
purpose should stop promising it. That one is a fragment question and is being carried as
FRAGMENT-ISSUES row 135 by the session that owns those files.
