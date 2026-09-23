# Needs checking — Group I

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group I — the twenty-three that were tried and never passed, sorted by WHY

Read 2026-09-19, from the run records in `brain/proof-drafts/runs/` rather than by re-running
anything. **71 fragments are DRAFT. 44 cannot be arranged at all** (`generate-jobs.py`, measured over
all 71 rather than only the never-tried ones). **27 can be, and 23 of those already have a run
record** — they were put in front of a real model between 9 and 17 September and no signature
followed. Nobody had been back to ask why.

> **THE TWO COUNTS ABOVE WENT STALE THE SAME DAY. THE SPLIT BELOW DID NOT.**
> Re-derived 2026-09-19 on a Linux container at `main`: `heron_fragment.load_all` counts **395
> fragments, 328 PROVEN, 67 DRAFT**, and `python tools/generate-jobs.py` reports **27 emitted,
> 40 unarrangeable** — not 71 and 44. **THE FIRST VERSION OF THIS CORRECTION SAID 37 AND WAS
> WRONG, WHICH IS THE PARAGRAPH'S OWN LESSON ARRIVING ONE LAYER UP.** 37 was read out of
> [PROPOSALS § the seven that moved](../PROPOSALS.md), where it is **correct** — and it is measured
> over a different population: *DRAFT with no run record*, on a machine that HAD run records.
> This container has none, so `generate-jobs.py` measures all 67 and answers 40. Re-measured at
> `3dd7159`, the very commit the 37 was written at: **40 there too**, so nothing moved — the
> number was taken from prose and attributed to a command that says something else. Run it.
> The seven that moved are named by the commit that freed them:
> [PR #196](https://github.com/Ajmalpshaik/Heron-AI/pull/196) is titled *"Five of the six proposed
> rules are built, and **seven fragments can be arranged**"*, and `git merge-base --is-ancestor`
> puts it **after** the commit carrying this paragraph. **Derive both rather than reading them
> here** — this file's own rule, and [FRAGMENT-ISSUES row 143](../FRAGMENT-ISSUES.md) is the row.
>
> **AND NOTHING BELOW CAN BE RE-DERIVED ANYWHERE BUT THE PROVING MACHINE.** The sort was read from
> `brain/proof-drafts/runs/`, which is **gitignored**: on a fresh clone it does not exist, which is
> why `generate-jobs.py` measured all 67 DRAFT above rather than the 27 this paragraph describes.
> So I1, I2 and I3 are a **one-machine fact recorded in a shared file** — no clone and no CI run can
> check a single row of them. The split is still the useful part; the totals are not.

The answer is not one answer. It is three, and only the first is proving work.

### I1 — the arrangement was wrong, and that is all (6)

Each of these ran, or failed to run, for a reason that is fixed by typing different values. **Every
one still has to be re-run before anyone believes it**; what follows is a diagnosis, not a result.

| Fragment | What the record says | What to change |
|---|---|---|
| `auto-size-pipe` | positive `setup_failed - the arrangement could not be re-made`; the **negative ran fine** and did real work (`noFlow 14`, `refused 8`) | Only the positive selection broke. The model it needs is `Snowdon-scratch_ajmal.al` — it has the pipes; Project1 has none |
| `set-wall-constraints` | positive `rehosted: 4`, negative `notAWall: 3, rehosted: 0` — **both legs already look right** | Nothing. This is the one STALE signature: it passed, then the code changed under it. It needs re-proving, not re-arranging |
| `set-design-option` | `available: 0, wasPossible: false` on both legs | It ran against a model with **no design options**. `Project1 work_ajmal.al` has **1** |
| `create-workset-3d-views` | both legs `created: 2` — identical | The negative was not a different case. It needs a value-driven one |
| `place-accessory-on-run` | `bad_request_value: No family type called "Damper"` | Name a family type the model actually carries |
| `switch-active-project` | `switched: false`, *"Revit refused to change to the view"* | The view name was wrong. Note this one changes which project is in front, so it wants care rather than a batch |

### I2 — the model has not got it (13)

**Not failures, and re-running them changes nothing.** Each needs content no open model carries, and
`fragment-proving` rule 2 is exactly this: a fragment asked for what the model has not got returns
POSITIVE EMPTY and wears the blame.

| Needs | Fragments |
|---|---|
| **An IMPORTED CAD** — a link is not an import, and both said so in the same words: `notAnImport: 307`, `importsRead: 0` | `convert-cad-to-directshape`, `extract-cad-curves` |
| **Rooms, and doors between them** | `report-door-room-links`, `center-room-tags` (room tags) |
| **Tags or dimensions** — `notMovable: 15`, *"0 by its tag head, 0 by its location point"* | `move-annotation` |
| **Assemblies** — `notAnAssembly: 22` | `create-assembly-views` |
| **A named DWG export setup** — *"name the DWG export setup; Revit's defaults..."* | `export-views-to-dwg` |
| **The flow-arrow family loaded** — `No symbol 'M_Air Flow Arrow : Standard' is loaded` | `place-flow-arrows` |
| **An area scheme with areas** — `areas: 0, placedCount: 0` | `report-areas` |
| **Electrical circuits** — `panels: 0, elements: 0` | `select-by-electrical-circuit` |
| **Openings** — `found: 0` across all five opening categories | `select-openings` |
| **MEP runs that can take an offset** — `set: 0, withoutOffsets: 0` | `set-mep-justification` |
| **A section mark placed on a sheet** — `markers: 4` but `hidden: 0, shown: 0` | `set-section-mark-visibility` |

**ONE MODEL COULD ANSWER MOST OF THIS.** Rooms with doors, a section on a sheet, an imported DWG
beside the linked one, and an area scheme would clear **six** of the thirteen at a stroke. That is a
request to the owner, not a job for a batch.

### I3 — blocked on something being built (4)

| Fragment | What stops it |
|---|---|
| `find-clashes` | `'against' is an id, and Heron resolves one by NAMING the thing it belongs to. There is no rule for this name yet` |
| `place-rooms` | `levelId`, `phaseId`, `planViewId` are all `ElementId` |
| `set-global-parameter` | `"ParameterValue" is not one of them yet` — D-54 takes a view, a level, a category, a name, a number and true/false |
| `place-structural-family` | Not an input problem — [row 128](../FRAGMENT-ISSUES.md) of FRAGMENT-ISSUES. It placed a column when told `structuralType=Beam`, and which of two causes that is has not been settled |

The first three are the same shape as the 44, and **PR #190 proposed rules for six such shapes on the
same day** — proposals only, no code. If those land, this group shrinks.

### What this changes about the count

**The honest figure for "worth re-running" is 6, not 23 and not 41.** Thirteen are waiting on a
model and four on a decision. Saying 23 without the split would send somebody to re-run thirteen
fragments that cannot pass and to blame them when they do not.

---
