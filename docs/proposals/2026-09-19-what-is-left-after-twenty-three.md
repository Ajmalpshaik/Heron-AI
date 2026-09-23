# Proposals — 2026-09-19

> One section of [the register](../PROPOSALS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 2026-09-19 — what is left after twenty-three gaps were closed, and the two that never can be

Twenty-three fragments were built on 2026-09-19 for jobs the owner asked for that had no route
(PR #184). What follows is the remainder, split by whether anybody CAN build it — because the two
groups need opposite things and were being carried in one list.

### 🟡 Buildable, not built — nobody asked for these yet

| | why it is not there, and what it would take |
|---|---|
| **Colour fill scheme** | **The gap note had this wrong and the correction is the useful part.** It recorded a colour fill scheme as unreachable. `ColorFillScheme` carries `Duplicate(String)`, so one CAN be made from an existing scheme — plus `SetEntries`, `AddEntry` and `GetSupportedParameterIds` to fill it. What does NOT exist is creation from nothing, so the fragment would be *duplicate-and-edit* rather than *create*, and it should say so in its name |
| **Curtain grids and mullions** | `CREATE_WALL` will build a curtain wall — a curtain wall type IS a `WallType` — but it is proven on basic walls only, and nothing reaches inside one. `REPORT_CURTAIN_ELEMENTS` reads the panels and mullions and creates none |
| **Stairs and railings** | `Railing.Create` exists in three overloads and is the easiest of these. **Stairs are not**: `Stairs.Create` needs a `StairsEditScope`, which is a different shape from every fragment in the library and probably wants its own decision before anybody starts |
| **Topography and rebar** | Whole domains with no fragment at all. Worth naming so the absence is deliberate rather than overlooked |

### ⛔ NOT buildable — the API has no route, on any release

**These two are not backlog and must not be planned.** Both were checked against all eight reference
assemblies on 2026-09-19.

| | the finding |
|---|---|
| **Creating a phase** | `Phase` carries **no members of any kind** — no method, no static, nothing. Phases are made by hand in Revit; the API reads them and assigns elements to them. `REPORT_PHASES`, `SET_ELEMENT_PHASE`, `SET_VIEW_PHASE` and `SELECT_BY_PHASE` are the whole of what is possible |
| **Changing an element's category** | Not possible in Revit at all, by API or by hand. `CHECK_CATEGORY_MISMATCH` is the honest answer to what people mean when they ask — it finds things modelled in the wrong category so they can be rebuilt in the right one |

**A radial dimension is a third kind and belongs in neither column:** it is impossible on Revit 2020 to
2024 and ordinary from 2025, so `create-radial-dimension` exists and declares three releases. See
[16 §Break 2](../16-version-support-strategy.md).

---
