# Needs checking — Group IW

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IW - a simplify counts only the toposolids it changed: `EDIT_TOPOSOLID` version 2 (2026-10-23)

From [row 5b-1331](../fragment-issues/section-5b-rows-401-425.md). On Revit 2024.3 (session 83588), in *N23
Snowdon Site 2024* - a copy of Revit 2024's `Snowdon Towers Sample Site.rvt`, 10 toposolids - a rolled-back
simplify to 50% answered *"10 toposolid(s) edited (simplify) and read back."* and `resultIds` 10, while its own
report read *"toposolid 1264746: 2399 shape point(s) before, 1293 after."* and, for the other nine, *"0 shape
point(s) before, 0 after."* Version 1 counted every toposolid it touched.

**What changed - the simplify route only.** [`impl/any/fragment.cs`](../../brain/fragments/edit-toposolid/impl/any/fragment.cs)
still counts the shape points before and after `Simplify`, and now counts a toposolid as edited, and puts it in
`resultIds`, only when that count changed. One whose count did not change is named in the report - *"toposolid
N: nothing to simplify - 0 shape points, left as it was."*, or *"toposolid N: unchanged - M shape point(s)
before and after."* - and the first finding adds *"K toposolid(s) unchanged - the shape point count did not
change."* Excavate, unexcavate, split and smooth are as version 1 had them. **Nothing here has run in Revit.**

**The model - scratch, never a working project.** A new copy of Revit 2024's
`Snowdon Towers Sample Site.rvt` saved as `%TEMP%\heron-work\toposolid-iw\IW Snowdon Site 2024.rvt`, opened
in **Revit 2024**. The job file is [`tools/jobs/edit-toposolid-iw-2026-10-23.yaml`](../../tools/jobs/edit-toposolid-iw-2026-10-23.yaml)
(`--dry-run` reads it back and sends nothing). Every run is `validate --write` - rolled back, the model left as
it was. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and
`< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **IW1** | *Arrange.* The copy open and in front. In Revit, Select All Instances > In Entire Project on one toposolid, then on one wall | 10 toposolids and 10 walls, as the night's copy held. Other counts: write them down - the expectations below are then 1 edited and the rest named |
| **IW2** | `python tools/batch-prove.py tools/jobs/edit-toposolid-iw-2026-10-23.yaml` | The binding note on both legs reads *"elements from select-by-categories (10)"*. A smaller number means the arrangement failed, not the card |
| **IW3** | *The positive - the D-30 positive.* IW2's positive leg | **`resultIds` 1 - toposolid 1264746 - and `edited` 1.** Its report line *"toposolid 1264746: 2399 shape point(s) before, ... after."*, about half after (1293 on the night). The other nine each *"toposolid N: nothing to simplify - 0 shape points, left as it was."*, none of them in `resultIds`. The first finding *"1 toposolid(s) edited (simplify) and read back. 9 toposolid(s) unchanged - the shape point count did not change."* **FAIL:** `resultIds` 10, or *"10 toposolid(s) edited"* - version 1's answer |
| **IW4** | *The negative - the D-30 negative.* IW2's negative leg, the walls | `resultIds` 0, `edited` 0; each wall *"N: not a toposolid."*; the first finding *"0 toposolid(s) edited (simplify) and read back."* with no unchanged count after it |
| **IW5** | *Nothing kept.* IW2 once more, then Undo in Revit | The positive's line for 1264746 reads *2399 shape point(s) before* again - the first run's simplify was rolled back, not kept. Undo has nothing from either run |
| **IW6** | *Not needed to sign, if the PC has them.* The same copy opened (and so upgraded) in Revit 2025 and 2027: IW3 and IW4 | The same answers |

**Then sign** `edit-toposolid` on IW3/IW4, on the owner's word only - `python brain/heron_validate.py accept
edit-toposolid --by "Ajmal PS"`. One signature covers the card's code as it stands; the excavate, split and
smooth routes are still [Group DR](group-dr.md) **DR9**'s, on Revit 2025, and the signature says nothing about
them until DR9 runs. A row that fails reopens 5b-1331; nothing in a row changes to make it pass.
