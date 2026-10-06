# Needs checking — Group CF

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CF - sprinkler layout from the model: the rooms read, the heads placed, and their height (2026-10-06)

**Built 2026-10-06 from [docs/47](../47-sprinkler-layout-from-the-model.md) and
[its plan](../work-notes/plans/sprinkler-layout-2026-10-06.md). REPORT_SPRINKLER_LAYOUT_SPACES is DRAFT
and compiles on Revit 2020, 2024 and 2027; nothing below has run in Revit.** The read's arrangement is
in [`tools/jobs/report-sprinkler-layout-spaces-2026-10-06.yaml`](../../tools/jobs/report-sprinkler-layout-spaces-2026-10-06.yaml),
read back with `python tools/batch-prove.py <file> --dry-run`. **The model is the owner's to name, on a
TEST COPY**: an L-shaped office Space with a room-bounding column and a ceiling, a store Space with one
sprinkler in it, both on a level that is **not at zero**, with the project base point moved, and one
level-based sprinkler family loaded. CF1 to CF3 read only; **CF4 to CF7 place heads** and need the
Changes switch on.

| # | Run | Look for |
|---|---|---|
| **CF1** | **REPORT_SPRINKLER_LAYOUT_SPACES, the positive** - the office and the store selected, `spaces` `*` | Both rooms in `layoutJson`; the office's `outline` drawn over the plan matches its boundary to the millimetre, `holes` holds the column, `area_m2` the schedule's Area; `ceilings` the office's ceiling at its Height Offset From Level; the store's head in `sprinklers` even though it sits above the Space's upper limit |
| **CF2** | **The D-30 negative** - `spaces` set to an id that matches nothing; then nothing selected and `*` | No room, `spaceCount` 0 and a finding naming the id; then no room and a finding saying to select the rooms |
| **CF3** | **The two elevations** - the same read on the level not at zero | Write down `level_elevation_m`, `level_project_elevation_m`, and the store head's `z` and `offset_m`. **Which elevation plus the offset gives the z** is the answer FRAGMENT-ISSUES 5b-338 waits on |
| **CF4** | **The whole flow** - "place sprinklers in these rooms" in a chat, the office and the store selected | The Sprinkler Layout panel opens; the store is shown with its head and not laid out; after the type, the deflector distance, the office's class, ceiling and angle, and the limits are given, Preview draws the office's heads all OK |
| **CF5** | **Apply with the Changes switch OFF** | Nothing placed; the page says the switch is off and where it is; Revit's undo list has no new entry |
| **CF6** | **Apply with the switch ON - the height** | One undo entry; the read-back finds every head in the office; **each head's height in Revit equals the height asked** (level + ceiling - deflector). If every head is one level's elevation too high or too low, the read-back says CHECK and the z question (5b-338) is answered the other way - undo, and record it |
| **CF7** | **The deflector** - one placed head opened in its family | Is the family's insertion point its deflector? If not, `ceiling - deflector` puts the wrong point at that height and the read-back still passes - write down the offset to add |

**Then sign** REPORT_SPRINKLER_LAYOUT_SPACES on CF1 and CF2. It also needs **one store row** on the
owner's PC before a chat can route to it there (`python tools/check-routing.py` rebuilds the store when
its count disagrees with the cards).
