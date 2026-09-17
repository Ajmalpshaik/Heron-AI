# The ten agents against two models, in two Revits at once

**2026-09-17.** The REVIT ENGINEERING session, continued. The ten agents merged as
[#177](https://github.com/Ajmalpshaik/Heron-AI/pull/177) had compiled on eight releases and never run.
This is them running, against **two different models in two Revit sessions at the same time**.

The two-session arrangement was the owner's idea: *"CAN I OPEN ANOTHER REVIT SESSION SO YOU CAN WORK
FROM YOUR SIDE"*. It is what made the proof possible in one sitting — one Revit stayed with another
session working in `test projject`, the second was handed over with a different model.

---

## 1. Why tracking, and not the usual proof

`tools/batch-prove.py` cannot see these agents. **The entire proving apparatus is fragment-shaped** —
`batch-prove`, `heron_validate`, the drafts in `brain/proof-drafts/`, `check-signatures.py` and the
`heron-status:` field they move are all per-fragment. Add-in operations live in
`revit/Heron.Revit.Addin/` and have no route at all: **all 26 `.cs` files there are `DRAFT`**,
including agents built weeks ago and run many times.

So the method is the one [`fragment-proving`](../../.claude/skills/fragment-proving/SKILL.md) names
under *"fragments that cannot come back empty"*: **D-53 tracking.** A `list_*` agent describes
whatever model it is handed, so it has no empty case in the ordinary sense. The second leg of
[D-30](../DECISIONS.md) is met by showing the answer **follows the input** — an agent falling back to
a cached `Document`, the active view or the whole session cannot match two different models' numbers.

**Model A's numbers were written down before Model B was opened**, so the comparison could not be
written to fit the result.

| | |
|---|---|
| **Model A** | `test projject` · Revit 2024 · session **64416** · 3,565 elements |
| **Model B** | `Project1 work_ajmal.al` · Revit 2024 · session **86712** · 3,513 elements |

Both sessions were connected to Heron simultaneously. `revit_use_session 86712` bound this chat to B
explicitly rather than to *"whatever is in front"*, which is meaningless with two open — the rule
[docs/25](../25-multi-session-and-binding.md) already states.

---

## 2. The result: nine of ten moved

| agent | Model A | Model B | moved |
|---|---|---|---|
| `revit_levels` | 2 levels · **29** and **7** on them | 2 levels · **8** and **8** | **yes** |
| `revit_rooms` | **7 rooms**, 2 spaces, **3 not enclosed** | **none at all** | **yes** |
| `revit_views` | **26** views, **18** templates | **24** views, **16** templates | **yes** |
| `revit_sheets` | **2** sheets, **0 blank** | **1** sheet, **1 BLANK** | **yes** |
| `revit_schedules` | **4** schedules · Ducts **8** | **3** schedules · Ducts **18** | **yes** |
| `revit_families` | 252/560/**127** · 540 unused | 251/559/**108** · 545 unused | **yes** |
| `revit_annotation` | **6** dimensions · **0** text | **0** dimensions · **15** text | **yes** |
| `revit_worksets` | **not workshared** | **2 worksets**, 250 of 3,513 sampled | **yes** |
| `revit_export_check` | **3 of 7 rooms** with no area | **1 blank sheet** | **yes** |
| `revit_imports` | 0 / 0 / 0 | 0 / 0 / 0 | **NO** |

### The one that did not move, and it is not a pass

**`revit_imports` returned identically on both models, because neither has any imports or links.**
Tracking cannot be claimed for it. It has been *run*, twice, and answered sensibly twice — that is
all. Proving it needs a model with a linked or imported CAD file in it, and preferably one of each so
the distinction it exists to draw is exercised.

Recording it as unproven is the point. Nine of ten is the honest number.

---

## 3. Three cross-checks, and they tracked too

Independent agents reaching the same figure is worth more than any single reading, because a shared
fallback would have to be wrong in the same direction twice. **All three held on BOTH models, at
different values:**

| | Model A | Model B |
|---|---|---|
| dimensions: `revit_annotation` ↔ `revit_families` *Linear Dimension Style* | **6 ↔ 6** | **0 ↔ 0** |
| text notes: `revit_annotation` ↔ `revit_families` *Text* | — | **15 ↔ 15** |
| MEP spaces: `revit_rooms` ↔ `revit_schedules` *Spaces* | **2 ↔ 2** | **none ↔ none** |
| export blocker: `revit_sheets` ↔ `revit_export_check` | **0 blank ↔ 0 blank** | **1 blank ↔ 1 blank** |
| rooms with no area: `revit_rooms` ↔ `revit_export_check` | **3 ↔ 3** | **0 ↔ 0** |

**The direction flipped between models and the pairs stayed together.** On A the export blocker was
rooms and there were no blank sheets; on B it was a blank sheet and there were no rooms at all. Each
agent found what was actually in front of it.

---

## 4. What the run proved that a compiler could not

- **Units.** Level 2 reads **4000**, not 13.12. Revit stores that as decimal feet whatever the project
  is set to; it arrived in the project's own millimetres because it went through Revit's own
  formatter. That was the specific defect the agent was written to avoid, and it is now measured
  rather than argued.
- **The empty case is a sentence, not a zero.** Model B has no rooms, and `revit_rooms` says *"nothing
  here to answer from"*. A bare `0` would have read as a clean bill.
- **`not workshared` is its own answer.** Model A gave it; Model B gave two worksets and a sampled
  ownership reading — **250 of 3,513, with the bound stated in the answer**, which is the whole design
  of that agent and could not be exercised on A.
- **A schedule is a filtered view.** Model B's `HERON TEST KS` schedules Ducts where the model holds
  **18**; Model A's held **8**. The number beside the schedule followed the model.
- **The blank sheet.** `A101 "Unnamed"`, nothing on it, invisible in the Project Browser. The finding
  that agent exists for, found on the first model that had one.

---

## 5. Two real findings for the owner, and one about my own tool

**In `test projject`:** Rooms **2, 3 and 6** are not enclosed. Zero area, still in every room
schedule, and an area total exported today would be wrong by three rooms with nothing warning.

**In `Project1 work_ajmal.al`:** sheet **A101** would print blank, and **545 of 559 types** are placed
nowhere.

**And a gap in `revit_worksets`' own reporting.** On Model B it printed `Workset1 181` and
`Shared Levels and Grids 2` against **3,513 placed elements** — 183 accounted for, and the other 3,330
not explained anywhere in the answer. The underlying operation *does* compute `onNoWorkset`; the MCP
tool simply never prints it. Numbers that visibly do not add up teach a reader to stop trusting the
ones that do, so this is a defect in the presentation rather than the count. Not fixed here.

---

## 6. What this is and is not

**It is not a signature.** [D-30](../DECISIONS.md) ends with a person typing their own name, and
`brain/proof-drafts/README.md` is explicit that neither the arranger nor the runner may sign.

**There is also nothing to sign into.** No `heron-status:` field exists for an add-in agent and no tool
promotes one — see §1. Nine agents now have tracking evidence and there is no mechanism that can record
that fact against them. **Building a proof path for `revit/` is real work that belongs to somebody**,
and until it exists these ten stay `DRAFT` alongside the sixteen that came before them.
