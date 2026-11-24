# FRAGMENT-ISSUES archive — section 5b, rows 1251 to 1275

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 1251 to 1275 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1261

*Moved from the register on 2026-11-17.*

**Defect.** **`ARRAY_FAMILY_FORMS` ARRAYED FORMS SKETCHED ON PLANES A PARAMETER MOVES, AND EVERY ARRAYED FORM LOST ITS WORK PLANE - THE ROW BROKE AT ANOTHER TYPE AND THE MODELLER UNDID 15 STEPS. ITS TRIAL ROW CANNOT SEE THIS; WHAT THE TOOL ALREADY READS COULD.** Measured 2026-10-20 on Revit 2020 in the owner's build of a parametric electric duct heater family (`TRG_Duct Accessory_Electric Duct Heater`, five types), through Heron's own tools: legs sketched on the reference plane *Opening Panel Side* and bends on *Element Tip* - both planes a parameter moves - followed their length perfectly before the array. After [`ARRAY_FAMILY_FORMS`](../../brain/fragments/array-family-forms/fragment.yaml) - count from an Integer formula parameter, step `Z 80` and `Z -80` - [`REPORT_FAMILY_FORMS`](../../brain/fragments/report-family-forms/fragment.yaml) read every arrayed form as *on `<not associated>`*, and at another type the copies stayed at the old length or drifted diagonally, their Y extents shifted about 40 mm at each step. The tool made the array without a word: its trial row (`impl/any/fragment.cs`, the block under *"will every copy leave the original?"*, line 354) measures where three copies land at the type in front, and there they landed where asked, so nothing it measures was wrong until another type moved the plane. **What worked instead**, the same day: the repeated part as a nested Work Plane-Based family whose own planes and labels stretch it, its lengths linked with `LINK_NESTED_TYPE_PARAMETER` to the host's formulas, placed at a fixed point - the host's origin, on Ref. Level - and `ARRAY_FAMILY_FORMS` run on the nested instance: two arrays from the centre, up from Z 0 and down from a second instance on a height plane at Z -80, each count an Integer formula; `FLEX_FAMILY` at 4 sizes held, the row reading 9, 15 and 17 elements. [`brain/skills/family-creation.yaml`](../../brain/skills/family-creation.yaml) steps 0, 7 and 10 now say this. **CAN THE TOOL DETECT IT? Read in its source, not run:** two signals sit in what it already reads. (1) Before the array, `sketchPlaneOf` (line 158) gives the form's `SketchPlane`, whose `Name` the refusal already prints, and `heldBy` (line 195) already walks every `Dimension`'s `References`, `FamilyLabel` and `IsLocked` - the same walk over the reference plane that sketch plane names would show a labelled dimension driving it. (2) After the array, the read-back could read each member's sketch plane `Name` as `REPORT_FAMILY_FORMS` does - its `impl/any/fragment.cs` line 51 prints `SketchPlane.Name`, which is where `<not associated>` came from - and fail the call, which the host rolls back. **What was NOT checked:** whether a form on a plane no parameter moves also reads `<not associated>` after an array - if it does, signal 2 alone refuses every array of forms, and the card's own proof on Family3 does not record which plane its form was on; which end of a labelled dimension Revit moves - the centre plane a size is dimensioned from is named by the same labelled dimension and does not move, so signal 1 needs Defines Origin, a pin or the dimension's other end to tell the two apart; a plane moved only through a chain of locks and formulas; any release but 2020; and the other family methods that name `ARRAY_FAMILY_FORMS` - only `line-based-family-creation.yaml` step 5 describes an array, and it does not sketch on a moving plane.

**State.** **MOVED TO ISSUE [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** The tool is PROVEN at version 3 with a fingerprint, so a refusal for a form on a moving plane is a version 4 at DRAFT, owing a Revit run with this case as its negative and an array on a fixed plane as its positive (D-30). Until then the method keeps such a row off forms.

---

### Row 5b-1267

*Moved from the register on 2026-11-17.*

**Defect.** **WHERE REVIT'S API STOPS FOR RENDERING, READ OFF THE API REFERENCE OF EVERY RELEASE 2020 TO 2027 ON 2026-10-20 - SO A TOOL SAYS SO RATHER THAN SEEMING TO DO IT.** (1) **No release lets an add-in run the render**: `View3D` has `GetRenderingSettings` / `SetRenderingSettings` and no member that renders; the nearest route is posting `PostableCommand.Render` (present 2020-2027, with `RenderInCloud` and `RenderGallery`) and the modeller pressing Render. (2) **Graphic Display Options**: Cast Shadows, Show Ambient Shadows, Ambient Light and Photographic Exposure have no member - the `GRAPHIC_DISPLAY_OPTIONS_*` built-in parameters are the dialog's buttons, not values - and a background cannot be set back to None (`ViewDisplayBackground` creates a sky, a gradient or an image only). (3) **Rendering**: `SkyBackgroundSettings` has no member, so the sky's haze is out of reach; the exposure has no reset to automatic; `RenderingQuality` is Draft, Medium, High, VeryHigh and Custom on every release (Heron says Best for VeryHigh - [Group IB](../needs-checking/group-ib.md) IB3 asks what the dialog shows). (4) **Camera**: no field-of-view member on any release, and a perspective view's crop box is documented only as *"an outline encompassing the crop region"* - so no angle is offered; `FRAME_ELEMENTS_IN_CAMERA` sets the crop box to the elements' box in the view's own frame, which the reference says moves the crop region and far clip to contain it, and IB1 records what that looks like. (5) **Visual style**: Ray Trace has an API value on 2020 only, where Revit throws on setting it (measured 2026-10-13, `SET_VIEW_PROPERTIES`' card); 2021 to 2023 have no value 9 and 2024 on use 9 for Textures. (6) **Sun**: `SiteLocation.ConvertFromProjectTime` applies no daylight saving, so a time typed for a site that uses it may show an hour on in the Sun Settings dialog; the tool reads `UsesDST` back and says so.

**State.** **MOVED TO ISSUE [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - beyond the API on every release read; each tool answers it in words.** `SET_GRAPHIC_DISPLAY_OPTIONS` takes the four unreachable settings as inputs only so that asking for one is answered, never dropped. Revisit when a new release's reference adds a member.
