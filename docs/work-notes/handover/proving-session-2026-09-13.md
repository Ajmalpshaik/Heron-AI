# Proving session handover — 2026-09-13

> **Type:** Operational work note — the handover for one proving session. **Not specification.**
> Where a sentence here disagrees with the [Constitution](../../../HERON_CONSTITUTION.md), the
> [Golden Rules](../../14-golden-rules.md) or [DECISIONS.md](../../DECISIONS.md), **those win.**
> **Status:** **210 PROVEN / 150 DRAFT**, up from 199 at the start. Eleven proofs signed by Ajmal PS
> and promoted; three refused at the gate.
> **This note is scaffolding.** Everything durable is already in
> [`FRAGMENT-ISSUES.md`](../../FRAGMENT-ISSUES.md) rows 17–21, in
> [`NEEDS-CHECKING.md`](../../NEEDS-CHECKING.md) A16b, in
> [the sweep](../the-sweep-2026-09-13.md), and in the job files under
> [`tools/jobs/`](../../../tools/jobs/). **Delete this once the next session has read it.**

---

## 1. Where the library stands

**Derive it, do not read it here.**

```bash
grep -rh "^heron-status:" brain/fragments --include="*.yaml" | sort | uniq -c
```

At close: **210 `PROVEN`, 150 `DRAFT`, 360 total.** Of the 150 drafts, **3 carry a signature the gate
now refuses** and **147 have never been proved**.

## 2. The one finding that outranks the rest

**A rollback that reports success sometimes does not happen.** Not size, not the kind of edit —
**intermittent**. Recorded as [FRAGMENT-ISSUES row 19](../../FRAGMENT-ISSUES.md) and
[NEEDS-CHECKING A16b](../../NEEDS-CHECKING.md).

On one model, one afternoon, through the identical path with no `--apply`:

| What ran | Rolled back? |
|---|---|
| `rename-elements`, 8 views, first run | **No** — `L2` still reads `HERONL2` |
| `rename-elements`, same job, minutes later | Yes |
| `duplicate-views`, 11 views created | Yes |
| `hide-elements`, 191 ducts hidden | Yes |

**A16 above it is not wrong — it is narrower than it reads.** It watched a MOVE and held twice. It was
never evidence about a rename, and it had been read as if it were.

**Why three previous hunts missed it: a rename changes no count.** 9,638 before, 9,638 after. Verify a
rollback by reading the VALUE back — the name, the parameter, the elevation — never the element count.

**The experiment that would settle it** is in A16b: run one rename ten times, reading the name back
each time, and see whether the failures cluster or scatter.

## 3. The sweep changed what the backlog IS

See [the sweep](../the-sweep-2026-09-13.md) for the table and how to reproduce it.

Every DRAFT fragment was run once — 161 of them, **three minutes**. The result reframes everything:
**135 of the 147 outstanding are blocked by a VALUE nobody had said**, not by a thin model, and each
refusal names exactly which value. 86 want a string, 35 a bool, 34 a number.

**So the next session should not pick fragments one at a time.** Pick a *kind of value*, look it up
once in the model, and run every fragment that wants it.

**One trap, and it cost five hours of nothing:** `validate` PROMPTS at the keyboard when given no
`--negative-set`. A script must pass `stdin=subprocess.DEVNULL` or every call blocks until it times
out — and piping the run through `tail` hides that completely.

## 4. What was proved, and the defect that was hiding in it

Eleven promoted. The one worth reading is `select-in-region`: it **divided its region by 304.8 a second
time**, because the add-in's point parser already converts every XYZ to feet. A 200 m box became a
**656 mm** box, and it reported *"0 element(s) in a 656 mm volume"* — true about the box it searched,
read as true about the box that was asked for. **Checked every other fragment taking a point: it is the
only one.**

**Two proofs carry a written caveat inside the fragment**, so neither can be read as more than it is:

* `rename-elements` — *"what is NOT true is the sentence saying nothing was kept"*
* `test-view-filter-match` — `matched` was never moved off zero; no filter in this model covers Ducts

## 5. Three signatures the gate refuses

All signed 2026-09-09/10, **before the gate existed**. The signatures and the evidence stand; they need
a real negative case.

| Fragment | Why |
|---|---|
| `edit-text-values` | POSITIVE EMPTY — `changed` came back zero |
| `group-and-count` | the run record has no negative phase at all |
| `set-view-section-box` | the negative returned content, and the proof is **stale** |

## 6. What is genuinely blocked, and by what

**Do not spend another session arranging around these.**

| Blocker | Costs |
|---|---|
| Neither model has rooms, doors, walls, pipes, lines, schedules, sections, groups or nested families | ~30 fragments |
| PUBLISH has no runnable path — [row 21](../../FRAGMENT-ISSUES.md) | 8 fragments |
| Both open models are thin MEP samples sharing the same defaults | the 3 transfer fragments |
| `IList<IList<XYZ>>` is refused by decision, so `create-line` cannot be called | `create-line` + 3 line fragments |

**The single biggest unblock is a model with architecture in it.** Snowdon has 9,638 elements and still
cannot fill a third of the library — "rich" means variety of categories, not element count.

## 7. Do not repeat these

* **Read a gate's EXIT CODE, not its last lines.** `check-docs` prints its DRIFT list *above* a
  reassuring closing summary. Eight stale counts reached CI because of this.
* **Pin `HERON_CLIENT_ID` on every call, including `count`.** An unpinned first command takes the lease
  and refuses the second, and it looks exactly like a fragment failing.
* **`delete-elements` must not go in an unattended sweep.** Asked for 191 ducts it deleted **462**,
  correctly — and it ran inside a rollback now known to be intermittent.
