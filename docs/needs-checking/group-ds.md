# Needs checking — Group DS

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DS - widenings of DRAFT cards: grid intersections, unenclosed areas, face hosting found by a ray and on links, tray and conduit connectors, material assets, CAD coordinates, distances within a radius, filters of several rules, and ordering by position (2026-10-13)

The gap fill's first batch of widenings ([`work-notes/plans/gap-fill-2026-10-12.md`](../work-notes/plans/gap-fill-2026-10-12.md)):
jobs Heron did in part, finished by widening a card that was DRAFT already - so no PROVEN card's proof went stale.

**What changed - ten widenings and one new card.** `LIST_GRIDS` v3: `withIntersections`, where each two grids
cross, in millimetres - the points a column at every grid intersection goes on. `SELECT_UNENCLOSED_ROOMS` v3:
`includeAreas`. `PLACE_FAMILY_ON_FACE` v2: finds the face itself - each point looks up, down or across through a
3D view - and hosts on a loaded link's face too; version 1 took face references no request can carry, and refused
linked faces on a belief never measured. `ADD_FAMILY_CONNECTOR` v4: cable tray and conduit. `LIST_MATERIALS` v2:
`withAssets`, each physical and thermal asset's numbers as Revit formats them. `EXTRACT_CAD_CURVES` v2: `listOnly`,
the segments' coordinates with nothing drawn. `FIND_NEAREST_ELEMENTS` v2: `withinMm`, everything within a distance,
nearest first. `CREATE_VIEW_FILTER` v2: `moreRules` and `combine`, several rules matched all or any.
`MOVE_ANNOTATION` v2: `arrange`, a set of tags, text notes or dimensions lined up or spread evenly in their view.
`COMBINE_FAMILY_FORMS` v2: `forms` "list" says every combination in the family, "separate <ids>" takes named ones
apart - words in the value it already took, so no family build's call changes.
**New:** `ORDER_ELEMENTS_BY_POSITION` (FRG-SEL-038): the order a numbering should follow - left to right, reading,
clockwise, along a line, level by level - handed on to `RENUMBER_SEQUENTIAL`, which by design decides none.

**Measured without Revit, 2026-10-13.** Every Revit API member was read off `RevitAPI.xml` for all eight
releases (Revit 2026 changed how two curves intersect; `LIST_GRIDS` takes each release's own call); every fragment
compiles on every release it declares, all eight run; the job file reads `4 WOULD RUN` and the two job files that
gained `withinMm` read as they did. **Nothing below has run in Revit.**

Never on a working project. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DS1** | Arrange on **Revit 2024**: the two scratch projects the header of [`gap-fill-widenings-2026-10-13.yaml`](../../tools/jobs/gap-fill-widenings-2026-10-13.yaml) describes; then `python tools/batch-prove.py` on it, the face-based type filled | six intersections A/1 to B/3; the doors in reading order; the stray area unenclosed; two downlights hosted on the ceiling, nothing from the negative |
| **DS2** | `PLACE_FAMILY_ON_FACE` with the ceiling in a loaded architectural **link**, searchLinks true | hosted on the linked ceiling - or Revit's refusal named per point. **This settles what version 1 only believed** |
| **DS3** | A Cable Tray Fittings family: `ADD_FAMILY_CONNECTOR` system "Cable Tray", two size parameters; a Conduit Fittings family, "Conduit", one | a tray connector sized by both and a conduit connector by its diameter, each read back and following a FLEX_FAMILY resize |
| **DS4** | `LIST_MATERIALS` withAssets true on a project whose Concrete has physical and thermal assets | the density and conductivity on Concrete's line, the same as Properties shows |
| **DS5** | `EXTRACT_CAD_CURVES` listOnly true on a DWG's grid layer | the segments in millimetres, against the DWG opened in CAD; nothing drawn |
| **DS6** | `FIND_NEAREST_ELEMENTS` withinMm 3000, terminals to lights | every light within 3 m of each terminal, nearest first, read against a measured plan |
| **DS7** | `CREATE_VIEW_FILTER` with moreRules "Comments contains L2", combine and, then or | the filter's rules in Visibility/Graphics match the two runs |
| **DS10** | `MOVE_ANNOTATION` arrange "align left", then "spread down", on four ragged text notes in a plan | lined up on the leftmost, then evenly spaced, each read back within 1 mm |
| **DS11** | A family with a solid cut by a void: `COMBINE_FAMILY_FORMS` forms "list", then "separate" and the solid's id | the one combination and its two forms; then both forms standing alone, the hole gone, both still in the family |
| **DS8** | The rest on **Revit 2020** and **2027** | the same answers; on 2027 the intersections come from the newer call. Compiled on every declared release; what Revit DOES was measured on none |
| **DS9** | The signatures, on the owner's word only, after the rows above | `python brain/heron_validate.py accept <fragment> --by "Ajmal PS"` for each |

### 2026-10-15 - DS7: `CREATE_VIEW_FILTER` version 3 ran on Revit 2020; signature owed

Version 2 could never reach Revit: its `parameterId` was an ElementId the add-in binds by no name
([FRAGMENT-ISSUES 5b-191](../FRAGMENT-ISSUES.md#)), so DS7 could not be run as written. Version 3 is the
same card edited ([row 5b-479](../FRAGMENT-ISSUES.md#)): `parameterName`, typed as Revit shows it, and a
`matchValue` that may lead with the Filters dialog's operator, *"does not contain Level 1"*. Its rule
factories are found by reflection, so no `#if` is left. Compiled on all eight releases.

Run on the owner's PC, **Revit 2020, session 25244**, in "Project2" - a new, unsaved project from the
Systems template, opened that day for the other view-filter proofs. "Project1", named for this, had
been closed. `python tools/batch-prove.py tools/jobs/create-view-filter-project2-revit2020-2026-10-15.yaml`:

- **PASS.** Levels, `parameterName` Name, `matchValue` *does not contain Level 1*, red, on `{3D}` -
  `created` id 918728 (rolled back). With `parameterName` System Classification - refused, *"no parameter
  the filter's categories share is called "System Classification" - nothing was made"*, `created` null.
- **Second route.** The positive run once more KEPT (`fragment --in Project2 create-view-filter --write
  --apply`, id 918729). `REPORT_VIEW_FILTERS` on `{3D}`, which carried no filter before, then showed
  *HERON PROOF CVF Levels*: Visibility ON, lines and foreground patterns 255,0,0, projection and cut,
  and the project's filters went from 10 to 11. `TEST_VIEW_FILTER_MATCH` on both levels: matched Level 2
  only. The kept filter is still in Project2, which is unsaved.
- **Still open in DS7:** `moreRules` with `combine` and, then or. **DS8 for this card:** Revit 2021 to
  2027 - on 2023 and later the two-argument factory is taken, and whether it ignores capitals is not
  measured. **DS9 for this card:** the signature -
  `python brain/heron_validate.py accept create-view-filter --by "Ajmal PS"`, run in the worktree that
  holds the draft, then `heron-status: PROVEN`.
