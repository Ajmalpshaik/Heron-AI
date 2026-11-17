# Needs checking — Group DI

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DI - an element's full parameter record by GUID, with where it stands and its tags (2026-10-12)

**Tracked as GitHub issue [#982](https://github.com/Ajmalpshaik/Heron-AI/issues/982) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

On 2026-10-12, on Revit 2020, project 4355-BHVD-3D-60P00-BL006A carried two shared parameters called
*Equipment Tag* on the in-place units being swapped - GUID e133774c... filled, cb0704fc... empty - and a
by-name read showed the empty one. `REPORT_PARAMETER_INVENTORY` version 1 kept one line per name, so it
could not show the pair at all. [FRAGMENT-ISSUES row 5b-443](../fragment-issues/section-5b-rows-426-450.md).

**What was built - version 2 of the same capability, not a new one.** Every parameter is listed by what
it is, a shared one with its GUID; `sameNameTwice` names each name carried twice where one is shared or
project; `fullRecord` gives `record`, one string with the location, bounding box, facing, every instance
and type parameter and, with `includeTags`, every tag with its view, type, head and leader. The job
`replace-in-place-family` names it at steps 1 and 6.

**Run 2026-10-12, read only, on 4355-BHVD-3D-60A10-BL003A (Revit 2020, session 25812)** - a working
project, which a read cannot change: no transaction is open. The values are in
[`report-parameter-inventory-v2-2026-10-12.yaml`](../../tools/jobs/report-parameter-inventory-v2-2026-10-12.yaml).
The run record and its draft are in this worktree's `brain/proof-drafts/` (git-ignored), so DI4 is run
from the same folder.

| # | Run | Look for |
|---|---|---|
| ~~**DI1**~~ | Positive: the exhaust fan TRG_FAN_EXHAUST FAN: AX45 (450mm Long), id 7614932, sampleOnly, type… — [full row](../needs-checking-archive/group-di.md#row-di1) | **DONE 2026-10-12.** |
| ~~**DI2**~~ | Negative: the louvre TCM_EAL_T001_ExhaustLouvre: Standard, id 7701431, the same switches — [full row](../needs-checking-archive/group-di.md#row-di2) | **DONE 2026-10-12.** |
| ~~**DI3**~~ | Before and after: version 1 from a base worktree of origin/main against version 2, same fan, same… — [full row](../needs-checking-archive/group-di.md#row-di3) | **DONE 2026-10-12.** |
| **DI4** | Compare DI1 by hand: the fan's Properties palette, Edit Type, and the five views its tags are in | each value as the palette shows it; the two *Classification.Uniclass.Pr.Number* rows in Edit Type; each tag's head where the view shows it |
| **DI5** | The signature, on the owner's word only, from this worktree | `python brain/heron_validate.py accept report-parameter-inventory --by "Ajmal PS"`. **The draft quotes `record` whole** - about 50 KB lands in the card's proof block; say whether that is wanted before signing |
| **DI6** | On **Revit 2024** and **2027**: a model whose element carries a same-name shared pair and a tag | the same answer. The 2022 tag members (`GetTaggedLocalElementIds`, `GetLeaderEnd`) were compiled against, not run |
| **DI7** | On 4355-BHVD-3D-60P00-BL006A, when it is open: one of the units that carried the two *Equipment Tag*s, if any remain, or the new ones | both *Equipment Tag* lines, e133774c... and cb0704fc..., each with its own value |
