# Needs checking — Group IV

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-22 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IV - a family's connectors hidden from a picture of its forms: `revit_view_image` with `elements` in a family (2026-10-22)

From [row 5b-1327](../fragment-issues/section-5b-rows-401-425.md). On Revit 2020, in a scratch copy of the
owner's 24 Inch CCTV monitor family, every picture of forms by id carried two large green circles with cross
lines and two small blue markers at the same pixels whatever was framed, and the picture was fitted to them -
the screen, `2713`, came out about a quarter of the width ([Group IO](group-io.md) IO1, [Group IP](group-ip.md)
IP3). A family with no connectors framed tight. `RevitCapture.Prepare`
([`RevitCapture.cs`](../../revit/Heron.Revit.Addin/RevitCapture.cs)) now also hides `OST_ConnectorElem` in the
temporary 3D view, **in a family only and only when no connector is among the ids**, through the same guarded
hide as every other category it hides: one Revit refuses costs that one setting, never the picture. A project
is pictured exactly as before. Light sources and reference lines are not hidden with them - the comment on
`Prepare` says why. It compiles on 2020 to 2027 and every Revit member exists in each; **none of it has run in
Revit.**

**What is not known, and IV0 asks it.** Why the old loop missed the connectors: it walks
`doc.Settings.Categories` and passes over model categories, so either the connectors' category is a model one,
or that list has not got it at all - on Revit 2024 `FILTER_ELEMENTS_BY_CATEGORY` found no *"Connectors"* in a
family by name ([row 5b-354](../fragment-issues/section-5b-rows-351-375.md)). And that the marks ARE the
connectors: nobody has pictured that family with them hidden. IV2 is that picture.

**The family - scratch, never the owner's own.** `%TEMP%\heron-work\group-ip\IO7 scratch - CCTV Monitor 24
Inch.rfa`, the copy Groups IO and IP ran on: 111 forms, 3 electrical connectors (`7571` to `7573`, at X -130 to
42, Y 41 to 49, Z 54 to 55 mm, pointing back), the screen `2713` (UniqueId
`ce590062-ddf7-4c0d-99c8-3603c9951e09-00000a99`), the box behind it `...-00001987`, the base plate
`ce590062-ddf7-4c0d-99c8-3603c9951e09-00001b6d`. If it is gone, copy the owner's 24 Inch family again into that
folder and read the numbers back with `REPORT_FAMILY_FORMS` and `REPORT_FAMILY_CONNECTORS`; never picture his
own file. **And for the negatives** the same folder's *GIP M_Skylight-Pyramid.rfa* (no connectors) and *Heron
Capture Test.rvt* (a project). Open each with `ACTIVATE_DOCUMENT`. Every row on **Revit 2020**, every picture
1280 px wide (the default), so a before and an after compare pixel for pixel.

| # | Run | Look for |
|---|---|---|
| **IV0** | *Why the loop missed them - read only, any build.* CCTV copy in front. A read-only probe (scratch C# sent as `run_fragment_read` under a READ card's contract, as Group IP's numbers were read): `Category.GetCategory(doc, BuiltInCategory.OST_ConnectorElem)` - its `Name` and `CategoryType`; whether `doc.Settings.Categories` holds a category with that id; and the family's existing 3D view's `CanCategoryBeHidden` for that id | Three facts, recorded as they come. A `CategoryType` of `Model`, or a list that has not got it, is why the old loop never hid them. **`CanCategoryBeHidden` false means this fix cannot work** - stop, reopen row 5b-1327, and the next step is hiding the connector elements one by one, a change of its own |
| **IV1** | *Before - the add-in deployed now, without this change.* CCTV copy: `revit_view_image`, `elements` = `2713` | IO1's picture again: the screen about a quarter of the width, the green circles and blue markers over it. **Keep the picture.** This is what shows the check can see the fault - without it IV2 proves nothing |
| | *Then rebuild the add-in from this change for Revit 2020, deploy it, restart Revit, open the CCTV copy again* | The add-in log stamps the new build |
| **IV2** | *After - the positive.* `elements` = `2713` | **The screen fills the width of the frame, and no green circle or blue marker anywhere.** The words as IO1's: 1 framed, nothing left out, the temporary view rolled back. Circles still there: the fix failed - reopen row 5b-1327 with IV0's facts |
| **IV3** | *Row 5b-1327's other two pictures.* (a) the screen and the box behind it by UniqueId, `ce590062-ddf7-4c0d-99c8-3603c9951e09-00000a99, ce590062-ddf7-4c0d-99c8-3603c9951e09-00001987`; (b) the base plate alone, `ce590062-ddf7-4c0d-99c8-3603c9951e09-00001b6d` | No circles in either. (a) the screen and box fill the frame; (b) the base plate, its H base and castors, fill it |
| **IV4** | *Negative - a connector asked for keeps its category.* (a) `elements` = `7571`; (b) `2713, 7571` | **Never an empty picture.** Either (a) frames the connector and its symbol is in the picture, and (b) the screen with the connector beside it, framed to both - or the connector is left out, *"is not a model element (Connectors)"*: (a) refused `nothing_to_frame`, (b) the screen alone and still no circles. Record which; both are right |
| **IV5** | *Negatives - nothing else moved.* (a) Skylight, the Glass solid alone, `a8cc9e63-4da8-4dd2-9b35-df4c8c3f3d63-00009df9` - IP2's call; (b) *Heron Capture Test*, `392168,392170,392174,378117` - ER2's call, as IP6 ran it | (a) IP2's picture: the skylight close up. (b) IP6's words exactly - *"A picture of 3 element(s) ... Left out of the frame: 378117 is a datum (Levels)"* - and its picture. **A project is untouched by this change**, and a family with no connectors frames as it did |
| **IV6** | *Nothing kept.* Before IV2 and after IV5, in each document: the element count (each read's *"Heron counted N element(s) before ... and the same after"*), the views (`revit_views`), and Undo | The same counts, the same views - no 3D view left behind - and Undo disabled after a fresh open, as Group IP's IP5 read it |
| **IV7** | *Revit 2024 and 2027 - not needed to close the row.* A scratch copy of the same family opened (and so upgraded) in each: IV2 and IV4 (a) | The same as on 2020 |

**Then** row 5b-1327 closes on IV1 to IV6 - there is no card to sign: `capture_view` is the add-in's own, not a
fragment. A row that fails reopens 5b-1327; nothing in a row changes to make it pass. Restarting the Claude app
also picks up `revit_view_image`'s description, which now says a family's connectors are hidden unless one is
among the ids.
