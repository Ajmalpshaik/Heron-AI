# Needs checking — Group AR

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-25 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AR - a curtain wall's grid and mullions: set on the type, read back off the wall (2026-09-25)

**The owner's PC, Revit 2024 first, on a TEST COPY of `test projject` - never his own design.** Built
on 2026-09-25 as the owner's first pick from what [39](../39-three-downloaded-skill-sets.md) §5 found
missing:

- [`set-curtain-wall-grid`](../../brain/fragments/set-curtain-wall-grid/fragment.yaml) - `SET_CURTAIN_WALL_GRID`, FRG-ELE-072
- [`set-curtain-wall-mullions`](../../brain/fragments/set-curtain-wall-mullions/fragment.yaml) - `SET_CURTAIN_WALL_MULLIONS`, FRG-ELE-073
- [`report-curtain-wall-type`](../../brain/fragments/report-curtain-wall-type/fragment.yaml) - `REPORT_CURTAIN_WALL_TYPE`, FRG-ELE-074, the reader - it exists because the two writers were winning the QUESTIONS about a curtain wall's grid, and a question must not land on a write
- [`curtain-wall`](../../brain/skills/curtain-wall.yaml) - the skill, the order that changes one wall and leaves the rest of its type alone

**What is already known, and it is NOT a proof.** All three compile on all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-09-25). Each reaches its settings by Revit's own parameter id -
`SPACING_LAYOUT_*`, `SPACING_LENGTH_*`, `AUTO_MULLION_*` - because every one of those settings shares its
display name with another ([row 5b-203](../FRAGMENT-ISSUES.md#)). Each job file's `--dry-run` says
WOULD RUN. **None of it has met a model.**

**The arrangement is built by each job, not found, and rolled back:** a 9000 x 3000 mm wall of `Curtain
Wall: Curtain Wall 1` drawn 60 m north of the origin, its type copied, the wall moved onto the copy, and
the new fragment run on the copy. **Check first** that `Curtain Wall 1` and `Rectangular Mullion: 50 x
150mm` exist in the test copy - AR3's refusal prints every mullion type the model has.

| # | Check | Expected |
|---|---|---|
| **AR1** | `python tools/batch-prove.py tools/jobs/set-curtain-wall-grid-test-projject-2026-09-24.yaml --dry-run --session <pid>`, then the same without `--dry-run` | **Positive:** `applied` 4 and `differs` empty - vertical fixed distance 1500, horizontal minimum spacing 800 - and `measured` naming one wall that had 0 grid lines each way and now has **5 vertical and 2 horizontal**. **Negative:** the same request on `Basic Wall: Generic - 200mm`, refused by name, nothing changed. **Record three things the reference could not settle:** which of Revit's U and V line sets the vertical lines came from; whether `Number`, `Justification`, `Angle` and `Offset` sit on the type or on the wall; and that the minimum-spacing layout, read off the assemblies as the value 5, reads back as Minimum Spacing |
| **AR2** | `tools/jobs/set-curtain-wall-mullions-test-projject-2026-09-24.yaml`, the same way | **Positive:** `applied` 4 - the four border positions, `Rectangular Mullion: 50 x 150mm` - and `measured` finding 4 mullions on the wall, one on each edge. **Negative:** the basic wall type, refused, nothing changed |
| **AR3** | `tools/jobs/set-curtain-wall-mullions-unknown-name-test-projject-2026-09-24.yaml` | **Negative:** `HERON NO SUCH MULLION` refused, and the refusal lists every mullion type this model has - write that list down, it settles AR2's arrangement |
| **AR4** | `tools/jobs/report-curtain-wall-type-test-projject-2026-09-24.yaml`, and then, by hand, the reader on an existing type such as `Curtain Wall: Storefront` with Type Properties open beside it | **Positive:** the grid writer's arrangement read back - vertical Fixed Distance at 1500 mm, horizontal Minimum Spacing at 800 mm, `gridsLaidOut` 2. **Negative:** the basic wall type, refused. **By hand:** every value the reader prints for Storefront matches Type Properties, each mullion by its full name |
| **AR5** | In a chat, *"draw a curtain wall between these points"* with a curtain wall type named | It reaches `CREATE_WALL`, and what it draws is a curtain wall - select it and it has a grid to edit. `CREATE_WALL` has no code for curtain walls and was proved on a basic wall; AR1's setup is the first time it draws a curtain one |
| **AR6** | Read `REPORT_CURTAIN_ELEMENTS` on AR1's wall before the rollback, or on a copy of the arrangement applied and then undone | Its vertical and horizontal line counts match AR1's `measured`. It reports Revit's V lines as vertical and U lines as horizontal; **if AR1 found the opposite, `REPORT_CURTAIN_ELEMENTS` owes a row** |
| **AR7** | The `curtain-wall` skill in one chat, on a scratch copy: *"draw a 9000 mm curtain wall of Curtain Wall 1 on Level 1, give just this wall a vertical grid every 1500 and 50 x 150 border mullions"* - with another wall of Curtain Wall 1 already placed | The host reads the type first, sees two walls use it, copies the type and moves the new wall onto the copy before changing anything, and the OTHER wall ends with no grid and no mullions. Record every place the host strays from the skill's order |
| **AR8** | AR1 and AR2 on **Revit 2020** | The same answers - one path on every release |

**Not in this group, because not built:** a fixed NUMBER of divisions, which is each wall's own count and
not the type's; one grid line placed, moved or removed by hand; the panel type; curtain SYSTEMS, whose
type is a different class.
