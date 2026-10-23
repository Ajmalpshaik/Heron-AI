# Needs checking — Group IX

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IX - the floor over the ducts hidden from a picture of them: `revit_view_image` with `elements` in a project (2026-10-23)

From [row 5b-1328](../fragment-issues/section-5b-rows-401-425.md). On Revit 2020, in *Heron Capture Test* (a copy
of Revit 2020's `rme_basic_sample_project.rvt`), ER2's call - the three ducts `392168,392170,392174` - pictured
the top of the slab over them, with walls and framing on it, and the ducts only as dark rectangles where the box
cut them ([Group IP](group-ip.md) IP6). The box is the ducts' own plus `max(1 ft, 10% of the longest side)` on
every side, and the isometric looks down. `RevitCapture.OfElements`
([`RevitCapture.cs`](../../revit/Heron.Revit.Addin/RevitCapture.cs)) now hides, in the temporary 3D view, **the
floors, ceilings and roofs lying wholly above the framed elements** - each one's lowest point at or above the
top of the elements' own box, before the margin - that the view shows inside the section box, never one of the
ids asked for, and **in a project only**. The answer names each one hidden, *"Hidden from the picture, as they
lie over them: id (Floors)"*. The box, the camera and every other element are as before; a family is pictured
exactly as before. It compiles on 2020 to 2027 and every Revit member exists in each; **none of it has run in
Revit.** `HideCoversAbove`'s comment says why this way and not the row's other two.

**What is not known, and IX0 asks it.** Which element the slab in IP6's picture is - its category, its level and
its thickness were not read - and whether its lowest point is above the ducts' top. A slab in a linked model, or
in a category other than Floors, Ceilings or Roofs, is not hidden by this change; neither is one reaching down
below the ducts' top.

**The model - scratch, never a working one.** `%TEMP%\heron-work\group-ip\Heron Capture Test.rvt`, the copy
Groups ER, IP and IV ran on. If it is gone, copy Revit 2020's `rme_basic_sample_project.rvt` there again under
that name and read the three duct ids back with Manage > IDs of Selection. **And for the family negative** the
same folder's *GIP M_Skylight-Pyramid.rfa*. Open each with `ACTIVATE_DOCUMENT`. Every row on **Revit 2020**,
every picture 1280 px wide (the default), so a before and an after compare pixel for pixel.

| # | Run | Look for |
|---|---|---|
| **IX0** | *What lies over the ducts - read only, any build.* *Heron Capture Test* in front. `SELECT_BY_CATEGORIES` with Floors, Ceilings and Roofs, the whole model, then `REPORT_BOUNDING_BOX` on that selection; then the same on the three ducts | Recorded as they come: each floor, ceiling and roof's id, lowest corner Z and height; the ducts' top (lowest corner Z plus height, the combined box). **The slab over the ducts named by id** - the one whose box spans their plan - and whether its lowest Z is at or above the ducts' top. **Below the ducts' top, or no floor, ceiling or roof over them at all (a linked slab, another category), means this fix cannot hide it** - stop, reopen row 5b-1328 with the facts. Also name **N**: the floor, ceiling or roof whose top (lowest Z plus height) is highest. Nothing of the three can lie wholly above N, so N is IX3's negative |
| **IX1** | *Before - the add-in deployed now, without this change.* (a) `elements` = `392168,392170,392174`; (b) `392168` alone; (c) N alone; (d) in the Skylight family, the Glass solid alone, `a8cc9e63-4da8-4dd2-9b35-df4c8c3f3d63-00009df9` | (a) IP6's picture: the slab top, the ducts dark rectangles at the box's edge; (b) the duct under a strip of the slab, as row 5b-1328 records; (c) and (d) whatever they show. **Keep all four pictures.** (a) and (b) are what shows the check can see the fault; (c) and (d) are what IX3 and IX4 compare against |
| | *Then rebuild the add-in from this change for Revit 2020, deploy it, restart Revit and the Claude app, open the copy again* | The add-in log stamps the new build |
| **IX2** | *After - the positive.* (a) `392168,392170,392174`; (b) `392168` alone; (c) ER2's whole call, `392168,392170,392174,378117` | **(a) The three ducts seen from above, whole along their length, no slab over them**, and the words *"A picture of 3 element(s) ..."* then *"Hidden from the picture, as they lie over them: "* naming IX0's slab by id, *(Floors)* or its category. Nothing IX0 found below the ducts' top is in that list. (b) the duct itself, the same slab named. (c) IP6's words - *"Left out of the frame: 378117 is a datum (Levels)"* - and the same hidden line. **The slab still in the picture, or no hidden line: FAIL** - reopen row 5b-1328 |
| **IX3** | *Negatives - nothing over it, and an id asked for.* (a) N alone; (b) `392168,392170,392174` and the slab's id from IX2 | (a) **No hidden line**, and IX1 (c)'s picture, pixel for pixel: an element with nothing over it is pictured exactly as before. (b) 4 framed, and **the slab never named as hidden** - an id asked for stays; its top is in the picture, the ducts under it. Anything else named in (b) lies wholly above the slab's own top, by IX0's numbers |
| **IX4** | *Negative - a family is untouched.* The Skylight family in front, the Glass solid alone as in IX1 (d) | IX1 (d)'s picture, pixel for pixel, and no hidden line. A family keeps today's picture |
| **IX5** | *Nothing kept.* Before IX2 and after IX4, in each document: the element count (each read's *"Heron counted N element(s) before ... and the same after"*), the views (`revit_views`), Undo; then in *Heron Capture Test*, the slab IX2 hid, in any existing 3D view | The same counts, the same views - no 3D view left behind - Undo disabled after a fresh open, as Group IP's IP5 read it. **The slab is still in the model and shows in its views**: the hide was in the temporary view only, and that view is gone |
| **IX6** | *Revit 2024 and 2027 - not needed to close the row.* A scratch copy of the same project opened (and so upgraded) in each: IX2 (a) and IX3 (a) | The same as on 2020 |

**Then** row 5b-1328 closes on IX0 to IX5 - there is no card to sign: `capture_view` is the add-in's own, not a
fragment. A row that fails reopens 5b-1328; nothing in a row changes to make it pass. Restarting the Claude app
picks up `revit_view_image`'s description, which now says floors, ceilings and roofs over the elements are
hidden in a project, and named.
