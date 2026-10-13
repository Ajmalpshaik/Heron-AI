# Needs checking — Group DU

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-13 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DU - a layer's material and function: `SET_COMPOUND_LAYER_WIDTH` version 2 sets a wall, floor, roof or ceiling type's layer material, function and width, finds a layer by function, reads and checks (2026-10-13)

[FRAGMENT-ISSUES row 5b-449](../fragment-issues/section-5b-rows-401-425.md). Heron could not give a layer of a
type a material - the house in Project2 got its colours by painting faces. `SET_COMPOUND_LAYER_WIDTH` (FRG-ELE-070)
was widened rather than a card added: `material` and `layerFunction` are new values, BLANK for "leave it", and
`checkOnly` an optional bool. It went from PROVEN to DRAFT, because its code changed.

**Measured on Revit 2020 only, 2026-10-13, Project1 (unsaved, session 18392)**, on a NEW type
`TRG_Wall_Proof_Layer_Material` duplicated from `Generic - 200mm`: layer 0 set to `Brick, Common` and KEPT, read
back by `REPORT_COMPOUND_STRUCTURE`; `TRG_Material_White_Render` refused with nothing changed, read back the same way;
the other modes rolled back. The rolled-back proof run is recorded (fingerprint `b5eacdcd6e0fa9fb`) - a copy is in
the main checkout's `brain/proof-drafts/runs/` and in `%TEMP%\heron-work\layer-material\keep`. Every fragment
compiles on all eight releases.

Never on a working project. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DU1** | The signature, on the owner's word only, from a checkout holding this code (the main checkout once merged): `python brain/heron_validate.py draft set-compound-layer-width --from brain/proof-drafts/runs/set-compound-layer-width.json`, read it, then `python brain/heron_validate.py accept set-compound-layer-width --by "Ajmal PS"`, then `heron-status: PROVEN` | the draft's fingerprint is the card's (`python brain/heron_fragment.py --show FRG-ELE-070`); a later code change makes it stale and the run is made again |
| **DU2** | **Revit 2024**, a duplicated FLOOR type of three or more layers: `layerIndex` -1, `layerFunction` "Finish 1", `material` a name the project holds | the finish layer - not layer 0 unless it is the top - holding the material, read back by `REPORT_COMPOUND_STRUCTURE` (its lines count from 1) |
| **DU3** | A type with two layers of one function, `layerIndex` -1 and that function; and a curtain wall type | the first refused naming both numbers; the second refused as having no layers. Nothing changed in either |
| **DU4** | A duplicated type with walls placed, then a material changed | the finding counts the walls placed, and every one of them shows the new material in a shaded view - it is a TYPE change |
| **DU5** | The same layer edited by hand in Edit Assembly on a second copy of the type | both copies read the same in `REPORT_COMPOUND_STRUCTURE` - the second route D-30 asks for where one exists |
| **DU6** | DU2 on **Revit 2027** | the same answers. Compiled on every declared release; what Revit DOES was measured on 2020 only |
