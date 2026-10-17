# Needs checking — Group FP

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group FP - a family placed at the height asked when its insertion point is not its own level: PLACE_FAMILY_INSTANCES version 3 (2026-10-17)

[Row 5b-590](../fragment-issues/section-5b-rows-401-425.md): on a real project in Revit 2020,
[`PLACE_FAMILY_INSTANCES`](../../brain/fragments/place-family-instances/fragment.yaml) version 2 refused two
`TRG_GEN_CutOut_Rectangular_HVAC` cut-outs asked 2152 and 2127 mm above their level - *"2014.5 mm above"* -
while each stood where it was asked. The family ticks `Defines Origin` on the plane at the bottom of its
opening, so its insertion point stands half the opening's height below its own level, and version 2
measured the insertion point. Version 3 reads the height where the family's own level stands - what
Elevation from Level and Offset from Host hold - and says where the insertion point is when it differs.
(FA, FB, FC and FG were taken by open pull requests when this was written, so this group is FP.)

**What is run and not run.** Run 2026-10-17 in Revit 2020 on the scratch `Project1`, every run rolled back:
`validate --write` tracked 1, 2, 3 and 5 cut-outs on Level 2 (the proof draft), and version 2 and version 3
side by side on the same placements, twice - the cut-out refused by one and placed by the other, every
version 2 case answered word for word the same by both. Compiled for all eight releases 2020 to 2027
(`tools/check-fragments-compile.py`). **Not run:** Revit 2021 to 2027, the 12.5mm Cutout Buffer version
saved from the real project, and a placement anybody has looked at. The card is DRAFT.

**Never on a 4355 model.** Every row on a **scratch project**; the family is loaded into it, never
placed into the real project from here.

| # | Run | Look for |
|---|---|---|
| **FP1** | *The signature.* Read `brain/proof-drafts/place-family-instances.yaml` in this branch's worktree (copies of it and its run record are in `%TEMP%\heron-work\pfi-height-d32778\keep`), then `python brain/heron_validate.py accept place-family-instances --by "Ajmal PS"` | The card's `proof:` becomes the 2026-10-17 run on Project1, Revit 2020, fingerprint `d9db040d807d158c`. **FAIL:** `accept` calls it stale - the code moved after the run; run `validate` again as row 5b-590 says before signing |
| **FP2** | The real family. Save `TRG_GEN_CutOut_Rectangular_HVAC` out of the real project (Edit Family, Save As - nothing in the project changes), load it into a scratch project in Revit 2020 with a level above 0, and run `validate --in "<scratch>" place-family-instances --write` with `symbol=TRG_GEN_CutOut_Rectangular_HVAC: 12.5mm Cutout Buffer`, that level, and two points 2152 and 2127 mm above it | `placed 2`, and `readBack` says *standing 2152 mm above it - ... Elevation from Level 2152 mm, its insertion point N mm below that* with N half the cut-out's height (137.5 for Object Height 250). **FAIL:** a refusal - say which height it gives |
| **FP3** | *The second route, by hand.* On a scratch project, the FP2 placement run once and kept with `try-once` (Revit asks Keep or Discard; press Keep) | In a section through the cut-out its centre is at the height asked, and Properties shows that number as Elevation from Level. Then Ctrl+Z, once, removes the whole layout. **FAIL:** the centre anywhere else |
| **FP4** | Another release. FP2's run on Revit 2024 or later (the family upgrades when it loads), then the fan coil unit `M_Fan Coil Unit - Horizontal Basic Concealed` at 2700 mm above Level 1 | The cut-out as in FP2; the fan coil unit's `readBack` *level-based, on 'Level 1', standing 2700 mm above it - Level 'Level 1', Elevation from Level 2700 mm*, with no insertion-point words. **FAIL:** a different answer on that release |
| **FP5** | *The negative case on the real path.* `M_Supply Diffuser - Circular - Round Neck - Ceiling Mounted` loaded into a scratch project, `validate --write` asked 3000 mm above Level 1 | Refused, nothing kept, in version 2's words: *"... carries no Offset from Host on a level's plane, so it cannot stand 3000 mm above 'Level 1'"*. **FAIL:** placed - reading the family's own level let a family that cannot be lifted through |
