# Needs checking — Group PW

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PW - ARRAY_FAMILY_FORMS version 6: a form already in an array, or in a group, is refused in a sentence, not thrown - DRAFT until proved (2026-11-23)

**Tracked as GitHub issue [#1290](https://github.com/Ajmalpshaik/Heron-AI/issues/1290) since 2026-11-23** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** [row 5b-1998](../fragment-issues/row-5b-1998.md). On Revit 2020 (`louvre-scratch`, session 49564) a blade
that was already a copy in an array was named again and the call threw (`fragment_threw`) where it should have
refused. Version 6 of [`array-family-forms`](../../brain/fragments/array-family-forms/fragment.yaml) reads the
form's group before anything is tried. It compiles for Revit 2020 to 2027 (`tools/check-fragments-compile.py`).
**None of it has met a Revit.** The card is `DRAFT` until PW1 and PW2 pass ([D-30](../DECISIONS.md)). Rebuild the
add-in and restart Revit first (`python tools/heron-ready.py`) - that needs Revit closed, so it is the owner's step.
Version 5's own proof is in pull request #1275; whichever merges second keeps the card `DRAFT` at version 6.

| Tool | Was | Now | Row | Checks |
|---|---|---|---|---|
| [`array-family-forms`](../../brain/fragments/array-family-forms/fragment.yaml) (`ARRAY_FAMILY_FORMS`) | DRAFT v5 | DRAFT v6 | [5b-1998](../fragment-issues/row-5b-1998.md) | PW1, PW2, PW3 |

**The model: a scratch family, never a working one.** `louvre-scratch` again, or a new metric Generic Model family
saved in `%TEMP%\heron-work\pv`, with an Integer parameter *Blade_Count* = 5 and a blade extrusion sketched on
*Ref. Level*.

| ID | Do this | Pass looks like |
|---|---|---|
| **PW1** | *Positive, as before.* A loose blade, `count=Blade_Count`, `step=Z 45`, through the Keep or Discard box; press Keep. | Arrayed, 5 in all, the count following *Blade_Count*; `members` and `memberForms` four ids each. Nothing about groups is said. |
| **PW2** | *Negative - an ARRAYED form.* After PW1, `validate --write --kept` with `--negative-set "form=<a copy's form id from memberForms>" --negative-set "count=Blade_Count" --negative-set "step=X 45"`. | **`negative` is a refusal, NOT `fragment_threw`.** `refused` begins *"Nothing was arrayed."* and says the blade is a copy in the array (PW1's `arrayId`), *5 in all, its count following "Blade_Count"*, names the group and the array's original, and says to change *Blade_Count* in each type or name a form in no array or group. Nothing is kept. **RECORD** whether the same comes back for the ORIGINAL blade's id (*"the original of the array"*). |
| **PW3** | *Negative - a plain model group.* A second extrusion, grouped by hand (Create Group), no array; `count=3`, `step=Z 45`. | Refused naming the group and *"Ungroup it in Revit first"*; nothing made. |

### Results, 2026-11-24 - Revit 2020, session 29500

**Arranged:** scratch family *PX Box* (`%TEMP%\heron-work\px`, see [Group PX](group-px.md)) with an INSTANCE Integer
*Blade_Count* = 5 and two loose blade extrusions on *Center (Left/Right)*. The arrayed form for PW2 was made by the
PROVEN version 5 (kept, `count=3`, `step=Z 45`: array `3601d8ce-...-00000ad3`), not by PW1, so both halves of this
proof ran rolled back (`validate --write`) and no save was needed. Run from branch `claude/prove-three-family-tools`.

| Row | Result |
|---|---|
| PW1, run 2026-11-24 | **PASS** (rolled back, not through the Keep box) - loose blade `6fcb91fb-...-00000a87`, `count=Blade_Count`, `step=Z 45`: *"Arrayed the solid extrusion ...: 5 in all, 45 mm apart along +Z, the count following "Blade_Count" in each type - every copy read back."* Nothing about groups said. |
| PW2, run 2026-11-24 | **PASS** - a copy from version 5's array (`3601d8ce-...-00000acb`), `step=X 45`: a refusal, not `fragment_threw` - *"Nothing was arrayed. The solid extrusion ... is already a copy in the array ... - 3 in all - and sits inside the group "Array Group 1" ..., that array's member. A form already in an array is not arrayed again. The array's original is the solid extrusion ...-00000a90."* Signed by Ajmal PS 2026-11-24, card PROVEN. |
| PW3 | **NOT RUN** - needs a group made by hand (Create Group); not needed for the proof. Still owed. |
