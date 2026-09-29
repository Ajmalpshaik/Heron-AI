# Needs checking — Group BH

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group BH - D-59's third batch: seven more reading tools look inside linked models when asked (2026-09-28)

**The same shape as [Group BE](group-be.md) and [Group BG](group-bg.md), on seven more fragments** -
`includeLinks` (absent means this model only), `linksSearched` (linked FILES read) and `linkedMatches` (what
was found there, as text). Nothing from a link is selected or carried ([row 75](../FRAGMENT-ISSUES.md));
nested links are counted and not read. **Only model elements are read from a link - never its views or
sheets**, on the owner's word of 2026-09-28. Why these seven, and what was decided:
[row 5b-257](../FRAGMENT-ISSUES.md).

**What is new: a linked wall's Fire Rating.** `audit-mep-openings`, `check-sleeve-size` and `select-touching`
name the linked wall, floor or structural member a service passes through, with its type, its id IN THE
LINK and its `Fire Rating` - the owner's own case: *"my pipe crosses a linked wall - is it a 2-hour wall?"*

**All seven compile on all eight releases and none has been run.** Five were `PROVEN` and went back to `DRAFT`
as version 2 ([D-30](../DECISIONS.md)); `select-touching` and `select-by-electrical-circuit` were already
`DRAFT`.

**BE1 must pass first** - it is the add-in change that lets `includeLinks` be left out, and every row here
depends on it. The model is `Snowdon-scratch_ajmal.al`; the negative case for every row is a model with no
links, checked in Manage Links first.

| ID | Do this | Pass looks like |
|---|---|---|
| **BH1** | `audit-mep-openings` on the openings in the selection, `includeLinks=true`. Pick at least one opening through a wall of the architectural link | `stale`, `combined` and `unhosted` unchanged from the host-only run; that opening's line reads `inside <link> - Walls '<type>' (id N in the link), Fire Rating <value>`. Tab-pick the wall in the link and compare the id and the rating. **Negative:** an opening in open space reads `inside nothing in any link` |
| **BH2** | `check-sleeve-size` on sleeves through a linked wall whose type carries a Fire Rating, `includeLinks=true` | `undersized` and `orphaned` unchanged; each sleeve's line names the wall and its rating. **Negative:** a wall type with no Fire Rating reads `Fire Rating not set` |
| **BH3** | `select-touching` with ONE pipe that runs through a linked wall as the target, `includeLinks=true` | `elements` unchanged; the link's line reads `1 overlap (Walls 1)` and the next names the wall with its Fire Rating. **Negative:** a pipe clear of every linked element reads `0 overlap` on each link. **Also record the time** on a large link - AJ9 asks the same about the host |
| **BH4** | `select-by-electrical-circuit`, `circuitKind=power`, on a model with the electrical model linked, `includeLinks=true` | `elements` and `panels` unchanged; the link's line gives loads, circuits and panel names, checked against a panel schedule opened in the link |
| **BH5** | `report-mep-pressure-drop`, on a model with a plumbing or mechanical link holding a fully connected system, `includeLinks=true` | The host totals unchanged; the link's line gives calculated and UNCALCULATED counts, and the connected system's critical-path total matches its own properties opened in the link |
| **BH6** | `report-level-elevations`, `includeLinks=true`, with the architectural link loaded | The host verdicts unchanged; one line per linked level with both heights in mm and `same height` or `DIFFERS by N mm`. Check one against a level head in a section with the link visible. **Negative:** a linked level name this model lacks reads `NO level of that name in this model` |
| **BH7** | `check-category-mismatch`, `nameKeywords=Door`, `expectedCategory=Doors`, `includeLinks=true` | `elements` and `foundIn` unchanged; the link's line gives its wrong, right and scanned counts, the right count matching a door schedule opened in the link |

**What cannot be answered here.** Every row needs a Revit. What was checked without one: the seven compile on
2020 to 2027, and every suite passes.
