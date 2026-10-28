# Needs checking — Group JE

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JE - three sheets moved onto another title block family in one call: `SET_SHEET_TITLE_BLOCK` version 2 (2026-10-28)

From [row 5b-1343](../fragment-issues/section-5b-rows-401-425.md). On Revit 2020 (session 33972), in a real
BIM 360 project, five sheets on one title block family were asked onto a type of another family. Version 1
threw *"The referenced object is not valid, possibly because it has been deleted from the database"* and
changed nothing: it walked one list of every title block for each sheet, and on the second sheet it read the
title block it had deleted for the first.

**What changed.** [`impl/any/fragment.cs`](../../brain/fragments/set-sheet-title-block/impl/any/fragment.cs)
keys each title block by the sheet it sits on, in one pass, before the first delete; each sheet looks up its
own. A sheet handed in twice reads the sheet again instead of touching the instance its first pass deleted.
Same-family swaps, the read back and every count are as version 1 had them.

**What is done and what is not.** It compiles for all eight releases (`tools/check-fragments-compile.py`,
2026-10-28). **Nothing here has run in Revit.** The card is DRAFT until JE3 and JE4 pass on one release and
the owner signs; version 1's proof is kept on the card as a record and is stale. The owner asked for it on
**Revit 2020, 2024 and 2027**: the first release signs the proof, the other two are `--release` entries
([D-131](../decisions/D-131.md)).

**The models - scratch, never a working project.** One copy per Revit, every one saved with the same title,
*JE Title Blocks*, so one job file serves all three and `--session <process id>` says which Revit:

| Revit | Copy of | Saved as |
|---|---|---|
| 2020 | `C:\Program Files\Autodesk\Revit 2020\Samples\rac_basic_sample_project.rvt` | `%TEMP%\heron-work\group-je\2020\JE Title Blocks.rvt` |
| 2024 | `C:\Program Files\Autodesk\Revit 2024\Samples\Snowdon Towers Sample HVAC.rvt` | `%TEMP%\heron-work\group-je\2024\JE Title Blocks.rvt` |
| 2027 | `C:\Program Files\Autodesk\Revit 2027\Samples\Snowdon Towers Sample HVAC.rvt` | `%TEMP%\heron-work\group-je\2027\JE Title Blocks.rvt` |

The title blocks are two families from Revit 2020's own library, loaded into all three:
`C:\ProgramData\Autodesk\RVT 2020\Libraries\US Imperial\Titleblocks\C 17 x 22 Horizontal.rfa` and
`...\D 22 x 34 Horizontal.rfa`, each with one type named as its family (read from the files' own type list,
2026-10-28). The job file is [`tools/jobs/set-sheet-title-block-je-2026-10-28.yaml`](../../tools/jobs/set-sheet-title-block-je-2026-10-28.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **JE0** | *Open.* The copy for this release open and in front in that Revit, nothing else of the owner's in it | The title bar reads *JE Title Blocks* |
| **JE1** | *Arrange, kept in the copy.* `fragment --in "JE Title Blocks" load-family --write --apply --set "familyPath=C:/ProgramData/Autodesk/RVT 2020/Libraries/US Imperial/Titleblocks/C 17 x 22 Horizontal.rfa"`, the same for `D 22 x 34 Horizontal.rfa`, then `fragment --in "JE Title Blocks" create-sheets --write --apply --set "numbers=JE-01,JE-02,JE-03" --set "names=JE one,JE two,JE three" --set "titleBlockName=C 17 x 22 Horizontal"` | Both families loaded; three sheets made, none refused, *titleBlockNotFound* false |
| **JE2** | *Check the arrangement.* `fragment --in "JE Title Blocks" find-sheets --set numberOrNameContains=JE-`, then `fragment --in "JE Title Blocks" report-sheet-title-blocks --expect-from find-sheets` | Three sheets; `typesInUse` exactly *C 17 x 22 Horizontal : C 17 x 22 Horizontal*. Anything else and the runs below prove nothing |
| **JE3** | *The positive - the D-30 positive.* `python tools/batch-prove.py tools/jobs/set-sheet-title-block-je-2026-10-28.yaml --session <pid>`, its positive leg | **`replaced` 3**, JE-01, JE-02 and JE-03 each named *"'C 17 x 22 Horizontal : C 17 x 22 Horizontal' was DELETED and replaced"*; `refused` empty, `swappedInPlace` 0, `alreadyCarried` 0. The binding note reads *"elements from find-sheets (3)"*. **FAIL:** *"threw while running ... The referenced object is not valid"* - version 1's answer - or `refused` naming JE-02 or JE-03 |
| **JE4** | *The negative - the D-30 negative.* JE3's negative leg, the same three sheets onto the type they carry | `replaced` empty, `swappedInPlace` 0, **`alreadyCarried` 3**, `refused` empty |
| **JE5** | *Nothing kept.* JE2 again | All three still on *C 17 x 22 Horizontal*: both legs were rolled back |
| **JE6** | *Revit 2024.* JE0 to JE5 on the 2024 copy, JE3 with `--release 2024` once the first release is signed | The same answers |
| **JE7** | *Revit 2027.* JE0 to JE5 on the 2027 copy, JE3 with `--release 2027` | The same answers |

The handed-twice route in `cases.yaml` is not arranged here: a selection never holds one sheet twice, and no
setup step in the library hands one in twice.

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept set-sheet-title-block --by
"Ajmal PS"` on the first release that passes JE3 and JE4, and `heron-status` set to PROVEN in the same change;
then `accept set-sheet-title-block --release 2024 --by "Ajmal PS"` and the same for 2027. A row that fails
reopens 5b-1343; nothing in a row changes to make it pass.

**What this group does not settle.** A real replace KEPT in a model goes through the add-in's delete question
first ([D-142](../decisions/D-142.md)) - Revit counts what goes with each old title block. On the owner's
project five sheets made it count 40 through `CHANGE_ELEMENT_TYPE`; which 40, and whether this card's replace
counts the same, is not read here. And the new title block is placed at the sheet's origin, as version 1
placed it - a sheet whose old one was moved off the origin is not arranged here.
