# Needs checking — Group PM

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PM - a dismissed warning names its elements, and a picture's longest side is the width asked - BUILT, NOT RUN IN REVIT (2026-11-21)

**Tracked as GitHub issue [#1214](https://github.com/Ajmalpshaik/Heron-AI/issues/1214) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** issue [#1112](https://github.com/Ajmalpshaik/Heron-AI/issues/1112), rows
[5b-1889](../fragment-issues/row-5b-1889.md) and [5b-1890](../fragment-issues/row-5b-1890.md), both from field report
[#1051](https://github.com/Ajmalpshaik/Heron-AI/issues/1051) (Revit 2020, 2026-11-19). Ajmal PS allowed `revit/` for
these two rows on 2026-11-21. Built in a cloud container with no Revit: the add-in compiles for every release
(`tools/check-compile.py`, the .NET 10 SDK); the two rules run in `tests/Heron.FailureNote.TestHost`
(`tests/test_failure_note.py`, section 12) and the new `tests/Heron.Picture.TestHost` (`tests/test_picture_fit.py`),
which also read the Revit half as text; the chat's words run in `tests/test_mcp_serves.py` against a stand-in Revit.
**None of it has met a Revit.** Rebuild the add-in and restart Revit first (`python tools/heron-ready.py`). No
fragment card changed, so no proof went stale.

| Change | Files | Row | Checks |
|---|---|---|---|
| Every warning Heron dismisses - and every failure that stops a job - carries the ids Revit named (`ElementId.ToString()`). The verdict names the first 5 of each kind, *"(2 times, 4 elements, element ids 352621, 352622, 352630)"*; `warningsDismissed[].elementIds` lists up to 50, with `elementIdsMore` beside a list cut short; the run record's `warning` line lists them. A warning whose ids could not be read reads exactly as before | `HeronFailureNote.cs` (`Posted.Ids`, `Said.Ids`, `Listed`), `RevitFragment.cs` (`FailureDiscipline.Read`, `WithVerdict`), `mcp/server/heron_run_records.py` | [5b-1889](../fragment-issues/row-5b-1889.md) | PM1, PM2 |
| A picture that comes back taller than the width asked is exported again fitted on its height (`FitDirectionType.Vertical`), so its longest side is the width asked; the reply sends `pictureWidth`, `pictureHeight`, `fittedOn`, and `refitFailed` when Revit refused the second export (the first picture is then sent). The chat's first line says *"1000 px tall"* and one line says why | `HeronPicture.cs` (`Size`, `TooTall`), `RevitCapture.cs` (`Exported`, `ExportOnce`), `mcp/server/heron_inventory.py` | [5b-1890](../fragment-issues/row-5b-1890.md) | PM3, PM4 |
| In a FAMILY, `elements=` hides every model element that was not asked for and whose extent reaches out past a face of the section box, and names it (`hiddenCrossing`). What lies wholly inside the box stays. A project is unchanged | `HeronPicture.cs` (`ReachesOut`), `RevitCapture.cs` (`HideReachingOut`), `mcp/server/heron_inventory.py` | [5b-1890](../fragment-issues/row-5b-1890.md) | PM5, PM6 |

**The models: scratch copies, never a working model** - in `%TEMP%\heron-work\pm`. Revit 2020 first, where both
rows were seen; then Revit 2024. Every picture is a read (its group is always rolled back); PM1 and PM2 are run
through `validate --write`, rolled back. Read the document name on every reply.

| ID | Do this | Pass looks like |
|---|---|---|
| **PM1** | *A dismissed warning names its elements - Revit 2020, a scratch Generic Model family.* In a chat, `CREATE_FAMILY_FORMS`: one extrusion whose profile has one edge about 1000 mm long and 0.1 mm off horizontal (the field report's cause was a 0.1 mm rounding on 45-degree braces); rolled back. If Revit raises no *"slightly off axis"* warning, write that down and try 0.01 mm - Revit's threshold is not known here | The verdict says *"Revit raised 1 warning ... "Line in Sketch is slightly off axis ..." (N element(s), element id(s) ...)"*, and `warningsDismissed[0].elementIds` holds the same ids as strings. **Write down what each id names** - a form the reply lists, a sketch line, or nothing left in the family once the form is made (a sketch's lines may be consumed by it). The count beside them is as before. Then the same with three such forms in one call: each warning's ids say which of the three it is about |
| **PM2** | *Negative - a write Revit raises nothing on, and a refusal.* The same family: one extrusion with a square profile (no warning); then one Revit refuses - an extrusion with a profile that crosses itself | The square one: no *"Revit raised"* sentence at all, `warningsDismissed` empty, `warnings` 0 - exactly as before. The refused one: *"Revit would not let it through: ..."* - write down whether it names element ids (it does only if Revit named failing elements), and that nothing was kept |
| **PM3** | *A tall picture fitted on its height - Revit 2020, a scratch copy of a tall, slim family* (the field report's rod hanger, about 760 x 140 mm, or any family much taller than wide). In a chat, `revit_view_image` `view` = *View 1*, `width` = 1000 | The first line says *"1000 px tall"*; a line says *"It is taller than it is wide, so it was fitted on its height: W x 1000 px"*; the picture shows the whole family, not a sliver past the chat's size. Write down W, and the 1000 x 5159 the same view gave before. *Again, nothing was changed* |
| **PM4** | *Negative - a wide picture, and a plan.* A project's floor plan with its crop on and its annotation crop off, `width` = 1280; then a 3D view wider than tall | Both say *"1280 px wide"* and no line about being fitted on the height - exactly as before. The plan's *"Model position ..."* line is given as it was ([MX19](group-mx.md)); the picture's PNG is 1280 px wide |
| **PM5** | *A family's parts framed close up - Revit 2020, the PM3 family with a context slab* (an extrusion that runs well past the hanger on two sides). `revit_view_image` `elements` = the small parts' ids (two or three, as `REPORT_FAMILY_FORMS` names them) | *"Hidden from the picture, as they reach out of the frame and were not asked for: <slab id> (form)"* (or its subcategory) and *"They are still in the family."*; the picture shows the parts close up, not the slab. The slab is still in the family after (Revit's Undo list gains nothing). **Negative in the same family:** a small form lying wholly inside the parts' box, not asked for, is still in the picture and not named; and with the slab's own id added to `elements`, the slab is pictured, never hidden |
| **PM6** | *Negative - a project is unchanged.* A scratch copy of the project row 5b-1328 used (*rme_basic_sample_project*), `elements` = three ducts with a wall or a pipe running through the box round them | No *"reach out of the frame"* line, no `hiddenCrossing`; the wall or pipe is in the picture; a floor over the ducts is still hidden and named *"as they lie over them"*, as [row 5b-1328](../fragment-issues/section-5b-rows-1326-1350.md)'s fix does. **Write down** whether the picture zoomed out past the box to the wall - if it did, that is a new row for projects, not a FAIL here |

**After PM1 to PM6 pass**, rows 5b-1889 and 5b-1890 close, and issue #1112 is ticked for both.
