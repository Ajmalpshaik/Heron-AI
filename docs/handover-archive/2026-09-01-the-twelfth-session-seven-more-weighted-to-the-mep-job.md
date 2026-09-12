# The twelfth session, 2026-09-01 — seven more, weighted to the MEP job

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **53 to 60**, chosen the same way. All 60 compile on all eight
releases; all 60 are `DRAFT`. The batch leans deliberately towards the work the owner actually does —
three MEP capabilities and three drawing-production ones.

| | |
|---|---|
| `MEASURE_MEP_VELOCITY` | *"Is this duct too small for the flow"* — which returned `CREATE_DUCT`. **Computes velocity from flow and area AND reads Revit's own, then reports both**: `RBS_VELOCITY` is derived from the size and flow last calculated, so a duct resized afterwards keeps reporting the old figure and looks fine on a schedule. Two numbers disagreeing is the finding |
| `SET_MEP_INSULATION` | Ducts and pipes in one selection, because a modeller does not sort a run by discipline first. **Existing insulation is replaced, not added to** — Revit carries two layers happily and the run then occupies a size nobody drew. **A clash check run before insulating is a clash check of the wrong model** |
| `READ_MEP_SYSTEM` | The system **name** and the system **type** are different things and "system" means both: two ducts can share *Supply Air* and be on separate systems, which is exactly what somebody is checking. Read as what the model **says** — `TRACE_CONNECTIVITY` is what goes and looks |
| `CREATE_3D_VIEW` | The name is set **after** the view exists, so a duplicate name leaves a usable view rather than an exception and an orphan. `named` is a separate output for that reason |
| `SET_VIEW_CROP` | Crop, section box and isolate are all asked for as *"just show me this bit"* and do different things. **Only the crop changes what prints** — on a view already on a sheet, whatever falls outside is gone from the issued drawing and no reviewer can tell it was ever there |
| `PLACE_VIEW_ON_SHEET` | Asks `CanAddViewToSheet` **before** attempting. A view can be on only one sheet, which is the cause of most refusals here, and the fix is to duplicate it — a different fragment |
| `MEASURE_ELEMENT_VOLUME` | Surface area is the MEP number: lagging and painting are bought by the square metre. Measured from the **solids**, walking nested geometry — reading only the top level finds nothing for most equipment. **Insulation is not included**, so pricing lagging means measuring the insulation elements |

### What the checkers caught this time

**`check-structure` caught a house-rule break the compiler was happy with.** The insulation fragment
used fully-qualified `Autodesk.Revit...` type names, which crosses the adapter boundary
([docs/16 §4](../16-version-support-strategy.md)) — every other fragment relies on the harness's own
`using` lines. It compiled perfectly on all eight releases; only the structure rule saw it.

> **And then the fix's own comment failed the same check**, because the rule matches on TEXT and the
> sentence explaining it spelled the namespace out. Reworded rather than teaching the checker to parse
> comments: a blunt rule that catches its own explanation costs one rewording, and a comment-aware
> parser costs something that can be wrong. Same family as `check-gaps` reporting its own regex as two
> undeclared agents.

**One error was caught by re-reading before compiling, not by a tool.** `SET_VIEW_CROP` first guarded
with `!view.CropBoxActive && !view.CanBePrinted`, which refuses on the wrong condition entirely and
would have compiled green. The rule it wanted was: a template or a view that cannot be printed.

**Retrieval, measured on 317 declared sentences**: words **92% first, 99% in the top three**; nearness
**67% and 84%**. Contested: **25 of 317**, all judgements. The risk-direction section is empty for the
reason it now states itself.

**What none of this is.** Not one of the seven has met a model. `check-gaps` counts **60** fragments
below `PROVEN`.

---
