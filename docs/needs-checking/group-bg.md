# Needs checking — Group BG

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group BG - D-59's second batch: ten more reading tools look inside linked models when asked (2026-09-28)

**The same shape as [Group BE](group-be.md), on ten more fragments** - `includeLinks` (absent means this model
only), `linksSearched` (linked FILES read) and `linkedMatches` (what was found there, as text). Nothing from a
link is selected or carried ([row 75](../FRAGMENT-ISSUES.md)); nested links are counted and not read, on the
owner's word of 2026-09-28. Why these ten, and what was decided: [row 5b-256](../FRAGMENT-ISSUES.md).

**All ten compile on all eight releases and none has been run.** Nine were `PROVEN` and went back to `DRAFT`
as version 2 ([D-30](../DECISIONS.md)); `select-openings` was already `DRAFT`.

**BE1 must pass first** - it is the add-in change that lets `includeLinks` be left out, and every row here
depends on it. The model is `Snowdon-scratch_ajmal.al`; the negative case for every row is a model with no
links, checked in Manage Links first.

| ID | Do this | Pass looks like |
|---|---|---|
| **BG1** | `filter-elements-by-type` with one host element selected as the exemplar, `includeLinks=true`. Pick a type the architectural link also uses | `elements` unchanged from the host-only run; the link's line reads `N of '<family> : <type>'`, N matching a schedule of that type opened in the link. A type the link does not use reads `0`. With a view on Revit 2020-2022: `Links NOT read` |
| **BG2** | `select-by-family`, `familyName=Door`, `includeLinks=true` | `elements` and `matchedFamilies` unchanged; the link's line gives its count and the families it hit |
| **BG3** | `select-by-level`, a host level whose NAME the link also has, `categories=Walls`, `includeLinks=true` | `elements` unchanged; the link's line reads `N on a level named '<name>'`. **Negative:** a level name the link lacks gives `0` on that line |
| **BG4** | `select-by-mep-system`, `matchOn=type`, a system type an MEP link uses, `includeLinks=true` | `elements` unchanged; each link's `N of M match`. A link with no ducts or pipes reads `0 of 0` |
| **BG5** | `select-by-insulation`, `Ducts`, `wantInsulated=true`, `includeLinks=true` | `elements` unchanged; each link's insulated count, checked against the insulation schedule opened in that link |
| **BG6** | `select-by-material` with a material that exists ONLY in the architectural link, `categories=Walls`, `includeLinks=true` | THE CASE THIS BATCH EXISTS FOR: the host findings say no material has that name, and the link's line gives its count. A link without it reads `no material called '<name>'` |
| **BG7** | `list-grids`, `includeLinks=true` | The host grids as before, then each link's grids in the same letter-then-number order. Check one direction against the plan; on a link placed rotated, the direction must be the one seen in THIS model |
| **BG8** | `select-by-phase`, `createdPhaseName` a phase the link has, `categories=Walls`, `includeLinks=true` | `elements` unchanged; the link's count. **Negative:** a phase name only the host has - the link's line says it has no such phase and lists its own |
| **BG9** | `select-without-level`, `categories=Walls,Doors`, `includeLinks=true` | `elements` unchanged; each link's `N of M have no level` |
| **BG10** | `select-openings`, `includeLinks=true`, then again with a plan view | `elements` and `byKind` unchanged; each link's openings per kind. With the view, fewer - the ones that plan draws. On Revit 2020-2022 with the view: `Links NOT read` |

**What cannot be answered here.** Every row needs a Revit. What was checked without one: the ten compile on
2020 to 2027, and every suite passes.
