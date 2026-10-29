# Needs checking — Group JL

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-24 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JL - a divided surface already there, changed: its grid rotated for a diamond pattern, offset, cut at the levels: `SET_DIVIDED_SURFACE_LAYOUT` version 1 (2026-10-28)

A new card, [`set-divided-surface-layout`](../../brain/fragments/set-divided-surface-layout/fragment.yaml),
written in a cloud session from the modeller's own sentences - *"rotate the divided surface grid 45 degrees
for a diamond pattern"*, *"change the U grid to 20 divisions"*, *"set the V grid to 1500 spacing"*, *"divide
the surface at every level"*, *"make the border tiles empty"*, *"offset the U grid 750"*, *"flip the panels
on the divided surface"*. [`DIVIDE_FAMILY_SURFACE`](../../brain/fragments/divide-family-surface/fragment.yaml)
(PROVEN) makes a divided surface - both grids centred, unrotated, with no offset - and refuses a face
divided once; [`REPORT_DIVIDED_SURFACES`](../../brain/fragments/report-divided-surfaces/fragment.yaml)
(DRAFT) reads one. Nothing changed one afterwards.

**What Revit's API gives, and what it does not say.** Read with `tools/api-surface` off the reference
assemblies of 2020 to 2027 on 2026-10-28, one signature on every release: `DividedSurface` carries
`AllGridRotation`, `BorderTile`, `ComponentRotation`, `IsComponentFlipped`, `IsComponentMirrored`,
`UPatternIndent` and `VPatternIndent` (each get and set), the two `SpacingRule`s, and
`CanBeIntersectionElement`, `AddIntersectionElement`, `GetAllIntersectionElements` and
`RemoveAllIntersectionElements`; a `SpacingRule` is set whole by `SetLayoutFixedNumber`,
`SetLayoutFixedDistance`, `SetLayoutMaximumSpacing`, `SetLayoutMinimumSpacing` (value, justification,
rotation, offset) or `SetLayoutNone`. The card hands back every part of a grid the request does not name.
**What no reference says**, and this group finds out: whether changing the all grid rotation moves either
grid's own rotation (JL10), which way U runs on a face (JL9), whether a level's cut counts among Revit's
gridlines (JL8), and whether a level lying on the face's edge can cut it (JL3).

**What is done and what is not.** The card compiles for every release 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-28, in the cloud), and its seven sentences were asked of this
container's backend by `tools/check-routing.py`. **Nothing has met a Revit.** `revit_change` refuses it
until JL13 is signed.

**The rig (JL0).** A NEW family from the metric **Mass** template, Revit 2024 - the release
`DIVIDE_FAMILY_SURFACE` was proved on - **saved** as *Heron JL Divided Surface Test.rfa* in
`%TEMP%\heron-work\group-jl` and left open in the Family Editor: a run needs a saved file (D-121). In it,
made by hand or through Heron as usual and kept: *Level 2* at 3000 mm and *Level 3* at 6000 mm beside the
template's *Level 1*; a box by `CREATE_CONCEPTUAL_FORM` - a 6000 x 3000 closed profile of reference lines on
*Level 1*, pushed 9000 mm up; and its **front** face divided by `DIVIDE_FAMILY_SURFACE` with uGrid *6*, vGrid
*3000 mm*, pattern empty - U Number 6, V Fixed Distance 3000 mm, no cut. One divided surface only. Write
down the box form's id: JL2 names it.
[`tools/jobs/set-divided-surface-layout.yaml`](../../tools/jobs/set-divided-surface-layout.yaml) runs JL1's
rolled-back half and JL2, once its `FORM-ID` is replaced.

| # | Run | Look for |
|---|---|---|
| **JL0** | The rig, by hand | The family as above; nothing selected; the surface's Properties read U Number 6, V Distance 3000 mm, both Center, 0 degrees, offset 0 |
| **JL1** | *The diamond.* `surfaceLayout` "u layout=distance; u distance=1500; u offset=750; v layout=number; v number=8; all rotation=45; border=empty; flip=yes; intersect=Level 2, Level 3", `dividedSurface` left out - once through `try-once`, on the owner's yes, Keep | `applied` 9 (8 with `alreadyAsAsked` 1 if the border tiles were already Empty), `skipped` empty; `readBack` each named setting *"(asked, was ...)"* and every other one as it was; **the grid runs at 45 degrees - diamonds**, the border cells empty, lines across the face at 3000 and 6000 mm; the surface's Properties read the same as `readBack`; one Ctrl+Z takes it all back |
| **JL2** | *Negative.* The same `surfaceLayout`, `dividedSurface` the **box form's id** - `validate set-divided-surface-layout --write --kept --negative-set ...` after a kept JL1, or the job file's negative | `applied` 0, `readBack` empty, refused *"... is form ..., which the surface is on - not the divided surface itself"*, naming the surface by its id; NOTHING WAS CHANGED; the surface's Properties unchanged |
| **JL3** | *Every level.* `surfaceLayout` "intersect=levels" | `applied` 1; Level 2 and Level 3 cut the surface; **Level 1, on the face's bottom edge: cut, or in `skipped` with Revit's reason - not known which**. If Revit throws on it instead, the call fails and nothing is kept - record the message; that is a finding against this card |
| **JL4** | *A layout changed, its distance handed back.* "v layout=maximum spacing" | `applied` 1; V *Maximum Spacing (asked, was Fixed Distance)*, distance *3000.0 mm* with no *asked* beside it; the U grid exactly as before |
| **JL5** | *Two parts changed, the number handed back.* "u justification=beginning; u rotation=30" | `applied` 2; U layout Fixed Number and number 6 left as they were; justification Beginning, rotation 30 degrees |
| **JL5a** | *The component settings and an indent.* The rig with the Rhomboid tile pattern picked by hand in the Type Selector; "component rotation=90; mirror=yes; u indent=1" | `applied` 3; component rotation 90 degrees, mirror yes and U indent 1, each *asked*; both grids as before; the pattern visibly turned and mirrored. If Revit holds another indent than the one written, the call fails and nothing is kept - record its message: whether an indent is bounded by the pattern is **not known** |
| **JL6** | *Refusals, each with nothing changed.* "u layout=distance; u distance=1.5"; "v number=20" alone; "intersect=Level 9"; "u layot=number"; an empty table; and the JL1 table with a project in front | in turn: outside 100 to 100000 mm with *"1.5 m is 1500"*; *"A V number does nothing while the V layout is Fixed Distance"*; Level 9 not found, naming Level 1, 2 and 3; the settings this changes listed; *"Nothing was named to change"*; `notAFamily` true. `applied` 0 every time |
| **JL7** | *Two surfaces, none named.* The rig with the box's back face divided too; `dividedSurface` left out | refused *"This family has 2 divided surfaces and none was named"*, both ids listed; neither changed |
| **JL8** | *The gridline counts*, read off JL1's and JL3's `readBack` | Record each grid's count before and now. Whether Number 6 gives six cells or six lines is [BV21](group-bv.md)'s question, asked again here; whether the level cuts are counted among them is this one's |
| **JL9** | *Which way U runs* on the front face, by eye in JL1 | Record it - across or up the face. The card says U and V as Revit names them and never says vertical or horizontal |
| **JL10** | *Anything Revit moved that was not asked*, from JL1's `findings` | The line *"Revit moved what was NOT asked"* absent. If present - one grid's rotation following the all grid rotation, say - RECORD it word for word: the card reports it and keeps the change, and whether it should refuse instead is the owner's call |
| **JL11** | *The other end of the range.* JL1's table again on Revit 2020, rolled back | The same `readBack`. One path serves every release |
| **JL12** | *Routing, on the PC's backend.* The seven sentences above; "set the divided surface spacing to 1500 mm"; and four questions - "what is the U grid layout of the divided surface", "what are the border tiles on the divided surface", "is the divided surface flipped", "which levels cut the divided surface" | Each of the seven reaches this card. The spacing sentence is declared by `DIVIDE_FAMILY_SURFACE` (PROVEN) for a face being divided and reaches it by identity, but ranks this card first; on a surface already divided that card refuses: **whether the sentence should move here is the owner's to decide** - both cards' routing rows say so. **No question may reach this card.** On the cloud backend, 2026-10-28, the first two were a coin toss with `REPORT_DIVIDED_SURFACES` (which reads both) and the last two ranked this card first - nothing read a flip or a cut then; `REPORT_DIVIDED_SURFACES` version 2 (2026-10-29, [DR5a](group-dr.md)) reads both and declares both questions. With the lookup's question mode on (D-128), all four reached `REPORT_DIVIDED_SURFACES` and this card was left out. Record where each lands on the PC, both ways |
| **JL13** | The signature, on the owner's word only, after JL0 to JL11 | `python brain/heron_validate.py accept set-divided-surface-layout --by "Ajmal PS"` |

A row that fails stays open; nothing in a row changes to make it pass.
