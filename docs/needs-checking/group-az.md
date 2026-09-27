# Needs checking — Group AZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AZ - `apply-view-filter` and `report-view-filters` version 2: every control of a view's Filters tab, set and read back per filter (2026-09-28)

**Two existing fragments widened in one sitting on 2026-09-28, and the add-in's override parser with them -
no new capability, on the owner's rule that an existing tool is edited, never a second one added.**
[`apply-view-filter`](../../brain/fragments/apply-view-filter/fragment.yaml) - APPLY_VIEW_FILTER,
FRG-VIEW-039 - now sets the whole row of Visibility/Graphics, Filters tab: Enable Filter (`enabled`),
Visibility (`visible`), and in `overrides` the projection and cut line pattern, colour and weight, the
surface and cut foreground and background pattern, colour and Visible tick, transparency and halftone.
`solidFill` puts Revit's solid fill on as the foreground pattern from fragment code. `keepOtherSettings`
blank changes only what is given. Cut values on a filter none of whose categories can be cut are NOT
written, and the answer says "cut not applicable to this filter's categories" and names them.
[`report-view-filters`](../../brain/fragments/report-view-filters/fragment.yaml) - REPORT_VIEW_FILTERS,
FRG-VIEW-041 - takes `viewName` and reads every one of those controls back, per filter, as one string.
Both compile on every release from 2020 to 2027, and both went back from `PROVEN` to `DRAFT`: their
proofs describe code that no longer runs ([D-30](../DECISIONS.md)). Why: [row 5b-243](../FRAGMENT-ISSUES.md).

**What Revit 2020 has, read off the reference assemblies, not remembered.** `View.SetIsFilterEnabled` and
`GetIsFilterEnabled` are present 2021 to 2027 and ABSENT from 2020 - the owner's belief, confirmed. Every
pattern, colour and Visible member of `OverrideGraphicSettings`, `Category.IsCuttable`,
`FillPattern.IsSolidFill` and `LinePatternElement.GetSolidPatternId` are present 2020 to 2027. The Enable
Filter members are reached BY NAME at run time rather than under `#if REVIT2020`, because the add-in
compiles fragments with no release symbols ([row 5b-181](../FRAGMENT-ISSUES.md)).

**Two halves, and they go live at different times.** The fragments are read off disk on every call, so
`enabled`, `keepOtherSettings`, `solidFill`, the cut check and the read-back ran on 2026-09-28 with Revit
open. The parser's new keys - `surface-foreground-pattern=solid`, `projection-line-pattern=Dash`, the
background patterns and colours, the Visible ticks, `none` - are add-in C#, and exist in Revit only after
the add-in is rebuilt, redeployed and Revit restarted. Until then the running add-in refuses those keys as
unknown settings. AZ2 is the parser half.

**The working run, 2026-09-28 - chat runs, not a proof.** "heron ai bulding", Revit 2024, Heron session
46596, Changes on, the fragment-side path only. For each of the six pairs - Supply Air, Return Air and
Exhaust Air on `FloorPlan: Level 1 - Air Terminal Layout` and on `{3D}` - the owner's company colours
(Supply 0,0,255, Return 255,0,255, Exhaust 139,69,19) on the projection lines, cut lines and surface and
cut foreground patterns, `solidFill=both`, `visible=true`, `enabled=true`, `keepOtherSettings` blank:

1. Read first with REPORT_VIEW_FILTERS: all six had Enable Filter and Visibility ON and the colours set,
   but **no fill pattern**, so the pattern colour painted nothing; and all eight duct categories answer
   `IsCuttable` false.
2. Six writes. Each: `applied` true, *"Changed: surface foreground pattern no override -> &lt;Solid
   fill&gt;"*, and *"cut not applicable to this filter's categories: Duct Placeholders, Duct Linings, Duct
   Insulations, Flex Ducts, Duct Accessories, Air Terminals, Duct Fittings, Ducts ... The cut values given
   were NOT set."*
3. Read back: all six with Enable Filter ON, Visibility ON, projection line colour, surface foreground
   `<Solid fill>` in the company colour, transparency 0, halftone off, and cut not applicable. The cut line
   and cut pattern colours an EARLIER session stamped are still stored - this run left them, being keep
   mode - and Revit draws none of them.
4. The same six writes again: all six *"NOTHING CHANGED - the filter already had exactly these settings
   on this view."*
5. `{3D}` / Exhaust Air with `enabled=false`, then `enabled=true`, `overrides=halftone=false`: Enable
   Filter read back OFF, then ON, and every colour and the solid fill listed as kept. The owner's end state.
6. Refused, nothing written: `enabled=maybe`; `solidFill=crosshatch`; and the old add-in refusing
   `surface-foreground-pattern` as an unknown key. REPORT_VIEW_FILTERS with `viewName=No Such View; 1 -
   Mech`: `filterSettings` empty, and the first two findings *"No view called 'No Such View'"* and *"View
   '1 - Mech' (FloorPlan) carries no filters."*

| # | Check | Expected |
|---|---|---|
| **AZ1** | **On the WORKING model, by eye - the second route for both fragments.** Open `Level 1 - Air Terminal Layout`, then `{3D}`: Visibility/Graphics, Filters tab | Supply Air, Return Air, Exhaust Air each with **Enable Filter ticked** and **Visibility ticked**; Projection/Surface Lines in the company colour; Projection/Surface Patterns showing **Solid fill** in the company colour; the Cut Lines and Cut Patterns cells **greyed out** for all three. And on the plan itself the ducts and air terminals drawn solid blue, magenta and brown. This is what the read-back of 2026-09-28 said; the dialog is what decides |
| **AZ2** | **The parser half, after the add-in is rebuilt, redeployed on every Revit and Revit restarted.** In a chat on the working model: APPLY_VIEW_FILTER on `{3D}` / Supply Air with `overrides=surface-foreground-pattern=solid; surface-foreground-colour=0,0,255`, `solidFill` blank; then `overrides=surface-foreground-pattern=NoSuchPattern`; then a MODEL pattern's name; then `overrides=enable-filter=false`; then `overrides=none` with `enabled` blank | The first: *"NOTHING CHANGED"* - the parser's `solid` is the same pattern `solidFill` found. The second: refused before Revit is touched, *"No drafting fill pattern called \"NoSuchPattern\""* and the drafting patterns listed. The third: refused as *"a MODEL pattern"*. The fourth: refused, saying Enable Filter is APPLY_VIEW_FILTER's `enabled`. The fifth: runs, NOTHING CHANGED. **Write down which ran on which add-in build** |
| **AZ3** | The two legs for `apply-view-filter` ([D-30](../DECISIONS.md)) - **a draft, not yet signed.** On a TEST COPY saved after 2026-09-27, with `HERON_CLIENT_ID=ajmal-pc` set and the copy in front: `python tools/batch-prove.py tools/jobs/apply-view-filter-heron-ai-bulding-2026-09-28.yaml --dry-run`, then the same without `--dry-run`. Then write AZ1 into the draft's `second_route`, sign with `python brain/heron_validate.py accept apply-view-filter --by "Ajmal PS"` and set `heron-status: PROVEN` | `PASS` on the flag: `applied` true putting Supply Air onto `1 - Mech`, which carries no filter, with the summary saying *"cut not applicable"* and naming the eight categories; `applied` false on `Project View`, which refuses a filter in Revit's own words. Afterwards `1 - Mech` carries no filter - both legs are rolled back |
| **AZ4** | The two legs for `report-view-filters` - **a draft, not yet signed.** READ ONLY, so the working model will do: `python tools/batch-prove.py tools/jobs/report-view-filters-heron-ai-bulding-2026-09-28.yaml --dry-run`, then without `--dry-run`. Sign only after AZ1 is written into its draft's `second_route` | `PASS`: `viewName=*` gives six rows in `filterSettings`, matching what AZ1 saw on screen; `viewName=1 - Mech` gives `filterSettings` EMPTY and the first finding *"carries no filters"* |
| **AZ5** | **Revit 2020.** On a copy of a 2020 model with a view filter on a view, in a chat: APPLY_VIEW_FILTER with `enabled=true`; then the same with `enabled` blank; then REPORT_VIEW_FILTERS with that view's name. **NEEDS REAL REVIT 2020** - no 2020 session was open on 2026-09-28 | The first: `applied` false, NOTHING written, *"Revit 2020 has no Enable Filter tick - it arrived at Revit 2021"*. The second: applied, *"No Enable Filter tick in this release"*. The report: the view's heading says 2020 has no Enable Filter tick and no row claims one. **All three prove the fragment still compiles on a real 2020**, which the reference-assembly compile cannot |
