# HANDOVER — 2026-09-10 (the PROVING track, day four): 0 proved, 5 defects found, and the add-in was a day stale

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**Nothing was promoted, and that is the honest outcome rather than a bad night.** Two drafts are
written and wait for a signature; five defects were found, four of them recorded for the first time;
and the add-in Revit had loaded turned out to predate the change everybody assumed was live.

**Read [`docs/FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) §5 first — rows 8, 11, 12, 13 and 14.** Every
one was observed in front of a model, with what was passed and what came back.

### Two drafts wait for a person, and only a person can sign them

Both against **Project1**, a scratch model, Revit 2024 session 28596.

| | Positive | Negative |
|---|---|---|
| `create-wall` | `created 4` — *"4 wall(s) built on 'Level 1' at 3048 mm high"* | two identical points: `created 0`, `tooShort 1` |
| `create-grids` | `created 6`, named A, B, C, positions `[0, 20, 40]` | no spacings: `created 0`, and it SPOKE — *"no spacings given in either direction - nothing to set out"* |

```
python brain/heron_validate.py accept create-wall  --by "Ajmal PS"
python brain/heron_validate.py accept create-grids --by "Ajmal PS"
```

**Both carry a sentence no earlier proof could:** *"Revit reported the transaction group ROLLED
BACK."* That is the check from §5 row 10 answering for the first time instead of the client repeating
its own flag. It was confirmed independently by reading the model back — 0 walls in Project1
afterwards — rather than by trusting the line.

**The arithmetic is the point, not the counts.** Five points gave FOUR walls because a closed run has
four segments; two spacings gave THREE grids because spacings are gaps, not positions. A fragment
that returned "some" of each would have passed a weaker reading.

### THE METHOD THAT CHANGED, AND IT CAME FROM THE OWNER

**Build the content on purpose in a scratch model, instead of hunting for it in a real one.** Rule 2
says ask for what the model has, and every earlier session answered it by searching Snowdon for
content that might not be there — `read-space-loads` (all 17 spaces `noLoad`), `check-flow-direction`
(no bad joint exists), `check-surface-fit` (the floors are a link, so nothing is ever hit). A model
built for the proof has the content by construction, and the negative can be arranged honestly.

**It also removes the objection that kept MODIFY fragments out of every batch so far.** §1c's rollback
has failed three times; in a throwaway project a rollback that does not hold costs nothing. That is
why the first two write proofs in this file's history were taken tonight rather than a week ago.

### The add-in Revit was running was a DAY OLD — §5 row 12

`select-in-region` and `check-surface-fit` refused with *"A point cannot be typed in yet."*
**That sentence exists nowhere in the source.** Deployed binary **2026-09-08 23:06**;
`RevitFragment.cs` last written **2026-09-09 23:14**.

**The row above claiming the add-in was "rebuilt and redeployed to Revit 2024 on 2026-09-09" was
wrong.** D-67 — the widening that "took the arrangeable library from 6 to 40" — had never reached the
machine, and neither had §5 row 10's rollback check. Two things everyone believed were live were not.

**Now deployed and verified at binary level on 2020, 2024 AND 2027** (all three are installed; only
2024 had been kept up). Deploy one release at a time: the build output folder is shared and the
newest wins, which is the 2026-09-08 failure. Never run `check-compile.py` before a deploy.

### Three more defects, each found by a fragment refusing in front of a model

**Row 11 — the setup chain's values are wiped before the fragment under test runs.** `validate` runs
the setup chain, then sends `chain: "reset"` for the fragment itself
(`heron_bridge_client.py:1377` → `RevitFragment.cs:232`), discarding what the setup just left. **A
setup chain can hand over only what lives in Revit's OWN state — the selection — never a value.**
`group-and-count` and `sum-by-group` both refused with `needs_unbound: 'values' was never supplied`
while the setup reported success. **The obvious fix is wrong:** the chain OUTRANKS the selection
(`RevitFragment.cs:917`), so dropping the reset would make `set-selection` decorative inside every
existing arrangement. It has to be opt-in per job.

**Row 13 — an `Element` caller value binds a TYPE where all twelve fragments mean an INSTANCE.**
`OneElement` is `OneOfClass(doc, typeof(ElementType), ...)` and returns the type **with no problem
set**, so nothing refuses. `select-by-host` compares against a type's id that nothing is hosted on;
`filter-elements-by-type` calls `GetTypeId()` on a type that has none. **`generate-jobs.py` lists all
twelve as arrangeable** because it checks the name RESOLVES, never that the resolution FITS — so of
its 39, **27 are genuinely testable.**

**Row 14 — `select-in-region` converts millimetres to feet a second time.** D-67 put the conversion in
`OnePoint`, so an `XYZ` arrives already in feet; the fragment divides again. A box half a kilometre
either side of origin became about **10.7 feet** across and returned `elements 0`. **It is the only
one** — all 29 XYZ-taking fragments were read line by line, and the rest convert a SCALAR
(`spacingMm`, `gapMm`) or use `1.0/304.8` as a one-millimetre tolerance. `create-grid` carries the
rule in a comment: *"adding one 'to match the other fragments' would place every grid 304.8 times too
far out."*

### Row 8 had no named victims. It has fifteen, and it is now FIXED

`create-grid`, `create-floor` and `create-ceiling` all returned `POSITIVE UNREADABLE  created
ElementId cannot be read as a quantity`. **29 fragments declare a bare `ElementId` result and 15
DRAFT ones have no other countable result at all** — `create-sheet`, `create-schedule`,
`create-plan-view`, `create-text-note`, `create-filled-region`, `duplicate-type`,
`place-mep-fitting` and the rest.

**And it was the WHOLE fix, which was not obvious.** The batch also said *"the negative did not come
back empty either"*, which looked like a second defect — an undeclared `findings` role, §3h.2. It is
not: `findings` is already in `NOTE_KEYS`. The record showed the real cause — `create-ceiling`'s
NEGATIVE reported `created ElementId` while its own findings said *"A ceiling needs at least three
boundary points and 2 were given"*. **It created nothing; the id was `InvalidElementId`; the renderer
gave it the same word as a real one.** One line repairs both legs of all three.

`Describe` now returns `"(null)"` for `InvalidElementId` and `"1 item(s) [id N]"` otherwise — no
`IntegerValue` (deprecated at 2024) and no constructor (int to long at 2024), so no version `#if`.
Compiles clean on 2020, 2024 and 2027, and **is deployed**.

### What tomorrow starts with

1. **Sign the two drafts**, above. They are the first write proofs in this file.
2. **Re-run the creators.** `tools/jobs/creators-project1.yaml` is written, dry-run clean and correct
   on units; `create-grid`, `create-floor` and `create-ceiling` should now pass on the deployed fix.
   Then the other twelve ElementId-blocked DRAFT fragments.
3. **Put walls in Project1 to test the transforms.** `move-elements`, `copy-elements`,
   `rotate-elements`, `mirror-elements`, `array-elements`, `snap-to-grid` all need something to act
   on, and the proving batch rolls back so it leaves nothing. It needs `--write --apply`, which is a
   deliberate act by a person.
4. **Row 11's opt-in chain flag**, which unblocks every producer-to-consumer pair.
5. **Row 13's refusal** — `OneElement` should decline for the instance-meaning names rather than
   bind a type.

### Three things that will bite whoever picks this up

**`write.enabled` is `false` in `%APPDATA%\Heron\config\heron.config`, and that is the right
default.** Every MODIFY fragment refuses with `write_disabled` until it is `true`. It is read fresh on
every call, so nothing needs restarting either way. It was turned on for tonight's batch and **turned
back off**.

**`write: true` is per job in a job file and batch-prove DOES NOT derive it from the fragment's
`risk:`.** It defaults to false, and a MODIFY fragment sent down the READ path is refused politely and
reads like a fragment declining — §5 row 6, which cost a whole commit once.

**UNITS, and the two are not the same.** `XYZ` and `IList<XYZ>` are MILLIMETRES, converted by
`OnePoint` in the resolver. A bare `double` is INTERNAL FEET, raw — every creator header says
*"Lengths are internal FEET."* **The name carries it: a `...Mm` suffix means millimetres, a plain name
means feet.** So `points` are mm and `height` is feet in the same fragment, and `height: 10` built a
wall 3048 mm tall.

---
