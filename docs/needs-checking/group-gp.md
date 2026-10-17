# Needs checking — Group GP

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group GP - copies that stay on their workset: `COPY_ELEMENTS` version 3, the workset chosen and read back (2026-10-17)

Found 2026-10-15 on a real BIM 360 project in Revit 2020: builders-work openings copied from instances on
`BHVD_BW_WALL_OPENING` landed on the ACTIVE workset, because that is where Revit puts every copy, and had to be
moved with `SET_ELEMENT_WORKSET` afterwards. [Row 5b-860](../fragment-issues/section-5b-rows-401-425.md).

**What changed.** [`COPY_ELEMENTS`](../../brain/fragments/copy-elements/fragment.yaml) (FRG-GEO-005) takes
`toWorkset`: `source` puts each copy on the workset of the element it is a copy of (its Workset value set
from its original's, as `SET_ELEMENT_WORKSET` writes it), a user workset's name puts every copy on it (the
copy made with that workset ACTIVE, as a modeller would, and the active workset put back after), and `active`
is Revit's own way. `onWorksets` reads every copy's workset back from the model,
with a count on each, and says how many follow their host or view (a curtain panel, a mullion) and were not set.
**Left out, `toWorkset` is `active`** - the card's `absent: active` - and version 2's code runs with no workset
read or written, so every caller, the building-shell skill and every job file are unchanged. A choice in a
model that is not workshared, or a workset name the model does not have, is refused before anything is copied.
Both the offset path and the onto-levels path take it.

**What is compiled and what is not run.** Every fragment compiles on all eight releases 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-17), and every new Revit member was read off the reference
assemblies as the same on all eight. **None of it has run in Revit.** `impl/` changed, so version 2's proof
(fingerprint `e99fc62f3cec4117`, Group DX) no longer describes the code: **the card is DRAFT**, and until GP11
`revit_change` refuses to keep a change from it ([D-117](../DECISIONS.md)) - that includes building-shell's
typical storey copied onto levels. A DRAFT may still be run once on the saved model in front behind Revit's
Keep or Discard box (`try-once`, [D-119](../DECISIONS.md)).

**TWO NEW PROJECTS, NEVER A WORKING MODEL AND NEVER ANY 4355-\* MODEL.** Made by hand before GP1, in Revit 2020
first, exactly as [`tools/jobs/copy-elements-v3-worksets-2026-10-17.yaml`](../../tools/jobs/copy-elements-v3-worksets-2026-10-17.yaml)
says: *Heron Copy Worksets WS* - a new project from the default metric template, worksets enabled, a second
user workset *Heron Openings* beside Workset1, four walls on Level 1 drawn with Heron Openings active, then
Workset1 made active again; and *Heron Copy Worksets Plain* - a new project from the same template, never made
workshared, with four walls on Level 1. Version 2's run record in `brain/proof-drafts/runs/copy-elements.json`
is moved aside first, because the job writes to that path.

| # | Run | Look for |
|---|---|---|
| **GP0** | **First** - the add-in rebuilt and deployed for Revit 2020 with the `absent:` rule ([Group EI](group-ei.md) EI3, [row 5b-488](../fragment-issues/section-5b-rows-401-425.md)), Revit restarted and the Claude app restarted | EI3 passes. **Without it a call that leaves `toWorkset` out is refused** for a missing value - every existing caller of `COPY_ELEMENTS` would stop - so nothing below is run on an add-in older than 2026-10-16 |
| **GP1** | `python tools/batch-prove.py tools/jobs/copy-elements-v3-worksets-2026-10-17.yaml`, *Heron Copy Worksets WS* in front - `toWorkset=source`, offset 3000,0,0, rolled back. Or the same once with `try-once`, kept on the owner's word ([D-120](../DECISIONS.md)) | **the D-30 positive**: 4 copies; `onWorksets` names the active workset Workset1, reads *"Read back, all 4 copies: Heron Openings 4"* and *"4 of 4 copies whose workset can be set are on the workset asked for"* |
| **GP2** | The same job's negative leg, *Heron Copy Worksets Plain* in front, the same values | **the D-30 negative**: `copies` 0, `onWorksets` empty, `refused` *"This model is not workshared ... Nothing was copied."*; the plain project still has four walls |
| **GP3** | Second route, in a scratch copy where GP1 was KEPT (or `try-once` kept): select the four copies and read Workset in the Properties palette; then Visibility/Graphics > Worksets with Heron Openings hidden | the palette says Heron Openings for every copy; with Heron Openings hidden, all eight walls disappear and nothing is left on Workset1 |
| **GP4** | In *Heron Copy Worksets WS*: four walls drawn with **Workset1** active, then `COPY_ELEMENTS` on them with `toWorkset=Heron Openings` (rolled back); then `toWorkset=Heron Nope` | the first: *"Every copy asked onto workset "Heron Openings" - made with it the active workset, and "Workset1" the active workset again after"*, *"Heron Openings 4"*, and the status bar's active workset Workset1 after the call; the second refused, naming *Heron Nope* and listing Heron Openings and Workset1, *"Nothing was copied"*. Then close Heron Openings (Collaborate > Worksets, Close) and ask for it again: refused, *"is closed in this session"*, nothing copied, the active workset unchanged |
| **GP5** | Mixed in one call: two walls on Workset1 and two on Heron Openings, both pairs the SAME wall type, `toWorkset=source` | *"Workset1 2, Heron Openings 2"* - each copy on its own original's workset, told apart by its place in plan |
| **GP6** | Hosted: a wall on Workset1 with a door placed while Heron Openings was active, only the WALL given, `toWorkset=source`; then a curtain wall on Heron Openings | the door's copy on Heron Openings and the wall's on Workset1; the curtain wall's panels and mullions counted as *"follow their host or view and were not set"*, and read back on the curtain wall's workset - write down what Revit actually did with them |
| **GP7** | Onto levels: the four Heron Openings walls, `offset=0,0,0`, `toLevels=Level 2`, `toWorkset=source` | `copiedTo` as version 2 (*"4 of 4 belong to Level 2"*) and `onWorksets` *"Heron Openings 4"* - the workset is set while the copies stand aside, before they are brought back |
| **GP8** | After GP0: the GP1 call with **no `toWorkset` at all** | **exactly version 2**: the copies on the active workset Workset1, `onWorksets` empty, `refused` empty, and the binding note *toWorkset not given - "active", the card's own default (optional)* |
| **GP9** | GP1, GP2 and GP8 once on Revit 2024 and once on 2027 | the same answers - a WorksetId is an int on every release, and 2024's 64-bit ElementId is not involved |
| **GP10** | Routing, on the owner's PC after the store is rebuilt from `main`: `heron_lookup` *"copy these and keep them on the same workset"* | `COPY_ELEMENTS`, by identity - measured in the cloud on both backends (trained model with and without the re-ranker), never yet on the owner's PC |
| **GP11** | After GP1 and GP2 pass: read `brain/proof-drafts/copy-elements.yaml`, then `python brain/heron_validate.py accept copy-elements --by "Ajmal PS"` and set `heron-status: PROVEN` | the card PROVEN again with a new fingerprint; `revit_change` keeps changes from it again, and building-shell's typical storey works as before |
| **GP12** | *The first use, on the owner's word, on a project he chooses - never a 4355-\* model:* builders-work openings copied with `toWorkset=source` | every copy on BHVD_BW_WALL_OPENING (or that project's own openings workset), no `SET_ELEMENT_WORKSET` afterwards |
