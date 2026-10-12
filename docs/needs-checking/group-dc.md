# Needs checking — Group DC

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-11 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DC - 3D model text placed in a family, on a named plane, one undo entry (2026-10-12)

On 2026-10-12 a modeller built three fixed-size electrical families through Heron's family-creation
method on Revit 2020 - a fire alarm control panel, a three-phase rotary isolator and an LV switchboard.
`heron_lookup("add model text to a family")` fell through to `ADD_FAMILY_PARAMETERS`: no tool placed
model text, so the panel's lettering was built as dot-matrix text from thousands of 1 mm square loops in
`CREATE_FAMILY_FORMS` - heavy, and not editable as text.

**What was built - a new capability, after the search found none.** `ADD_FAMILY_MODEL_TEXT`
(FRG-GEO-061) places one or many model texts in one call: each row gives the text, a named plane, `at`
in that plane's own two model coordinates (as the form tools write a profile), `heightMm` and `depthMm`,
and optionally `align`, `readFrom`, `offsetMm`, `material`, `detailLevels` and `views`. The height is a
Model Text type: one of that size is used, or the first type by name is copied as "<height> mm". The
family-creation method names it in step 12.

**Measured without Revit, 2026-10-12.** `FamilyItemFactory.NewModelText` and every `ModelText` member it
uses have one signature 2020 to 2027 (`tools/api-surface --members`). It compiles on all eight releases
(`tools/check-fragments-compile.py`), and "add model text to a family", "add text to the family" and
"write a label on the panel" reach it on Revit 2020 and 2024. **Nothing below has run in Revit.** The
arrangement is in
[`add-family-model-text-2026-10-12.yaml`](../../tools/jobs/add-family-model-text-2026-10-12.yaml), read
by `batch-prove --dry-run` as 1 WOULD RUN.

**What the code assumes and only Revit can say** - each is reported in the answer rather than refused,
so a wrong assumption shows in the read-back instead of blocking the call: Revit lays the letters along
the sketch plane's own X with its Y as up and stands them out along its normal; `at` is a point on the
baseline at the alignment's end; the metric templates hold a Model Text type; model text takes
`MATERIAL_ID_PARAM`.

Never on the owner's own families. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two
Revits are connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DC1** | Arrange on **Revit 2020**: File, New, Family, Metric Generic Model.rft, saved as **Heron Model Text Test**. Then `CREATE_FAMILY_FORMS` "kind=extrusion workPlane=Ref. Level profile=rect -300,-150 300,150 startMm=0 endMm=600 solid=true" | one box, front face Y -150, top Z 600, right face X 300. **Say whether the template holds a Model Text type** (Project Browser, Families, Model Text) - none means every call is refused, and that refusal is DC6 |
| **DC2** | `python tools/batch-prove.py tools/jobs/add-family-model-text-2026-10-12.yaml`, FAMILY-TITLE filled | positive: `textCount` 1, `placed` naming a 40 mm type, read from the front, running +X and up +Z, on a work plane of its own (the offset), and **where its letters reach on Y - record it**: -150 to -152 stands out of the face, -150 to -148 is sunk into the box. negative ("Center (Front Back)"): refused naming the planes, `textCount` 0. Look at the family afterwards - no text and no new type: rollback has failed on this PC before (BD1) |
| **DC3** | By hand through `revit_change`, DC2's positive kept, then the Front elevation | FIRE ALARM reads **left to right, not mirrored**, centred on X 0 at Z 400; Properties: Text, Depth 2 mm, Horizontal Align Center; Edit Type: Text Size 40 mm and the font. If it reads mirrored or lies sideways, the sketch-plane axes the code assumes are wrong - a FRAGMENT-ISSUES row the same day, with the `placed` sentence whole |
| **DC4** | `revit_change` with two rows: 'text="L1 L2 L3" workPlane=Ref. Level at=-200,0 heightMm=25 depthMm=1 offsetMm=600' and 'text=400 V workPlane=Center (Left/Right) at=0,300 heightMm=25 depthMm=1 readFrom=right offsetMm=300 material=by category detailLevels=medium, fine' | `textCount` 2, ONE undo entry (one Ctrl+Z takes both); one 25 mm type used by both; the first reads correctly in the Ref. Level plan on the box top, the second in the Right elevation on the right face; the second hidden at Coarse |
| **DC5** | `revit_change` with 'text=PANEL workPlane=Center (Front/Back) at=-250,100 heightMm=30 depthMm=3 align=left' - no offset, on the plane itself | `placed` says **sketched on the plane itself** or **a work plane of its own** - which one tells whether Center (Front/Back)'s own sketch plane reads from the front. Then move Center (Front/Back) 50 mm back: text on the plane itself follows it |
| **DC6** | The refusals: `heightMm=0`; `readFrom=front` on Ref. Level; a project in front; and, if DC1 found no Model Text type, any row | each refused with its reason and nothing placed; the project names CREATE_TEXT_NOTE, `notAFamily` true |
| **DC7** | The second route: Revit's own Model Text button places "FIRE ALARM" by hand on the same face, beside DC3's | the two read the same way, at the same height and depth |
| **DC8** | DC2 and DC3 on **Revit 2024** and **2027**, the arrangement made again from that release's template | the same answers. Compiled on all eight releases; what Revit DOES was measured on none |
| **DC9** | The signature, on the owner's word only, after DC2, DC3 and DC7 | `python brain/heron_validate.py accept add-family-model-text --by "Ajmal PS"` |
