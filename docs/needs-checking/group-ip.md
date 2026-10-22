# Needs checking — Group IP

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-22 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IP - a form pictured by the id the form tools give it: `revit_view_image` with `elements` takes a UniqueId (2026-10-22)

From [row 5b-1314](../fragment-issues/section-5b-rows-401-425.md). `revit_view_image` with `elements` took only
Revit's numbers, and `CREATE_FAMILY_FORMS`, `REPORT_FAMILY_FORMS` and `SELECT_FAMILY_FORMS` name a form only by
its UniqueId. `RevitCapture.OfElements` now looks a UniqueId up through `Document.GetElement(string)` beside the
numbers, and refuses by name every id that resolves to nothing. It compiles on 2020 to 2027 and every Revit
member exists in each; **none of it has run in Revit.** Every row needs the add-in rebuilt and deployed from
this change, Revit restarted, and the Claude app restarted.

**Framing a form is a separate fix, merged first:** row 5b-1311, [Group IO](group-io.md). This group finds the
form; that one frames it. Run IO's rows in the same build; IP3 below needs both.

**The models - scratch, never a working one.** A new family from Revit 2020's `Generic Model.rft`, saved as
*Heron Form Picture Test.rfa*, with two extrusions made through Heron (`CREATE_FAMILY_FORMS`): one a box, given
a subcategory (Object Styles, a new subcategory under Generic Models), one a cylinder left as made. Note both
`formIds` from the answer, and check them against `REPORT_FAMILY_FORMS` with `which` "all". And Group ER's
*Heron Capture Test.rvt*, with its three ducts: their numbers from Manage > IDs of Selection, their UniqueIds
from `revit_parameters` on Ducts (each row's `id`).

| # | Run | Look for |
|---|---|---|
| **IP0** | *Negatives, in the family.* `revit_view_image` with `elements` = (a) the box's UniqueId with one hex digit of its GUID changed; (b) `abc` and the box's UniqueId; (c) the box's UniqueId twice | (a) `no_such_elements`, *"1 of the 1 id(s) name no placed element in ..."* and that id; (b) `bad_ids` naming `"abc"`; (c) refused, *"typed more than once"*. **Nothing pictured** each time, no 3D view left behind |
| **IP1** | *Negative, in a project.* *Heron Capture Test* in front; `elements` = the box's UniqueId from IP0's family | `no_such_elements` naming it, *"An id belongs to one model, or one family"*. Nothing pictured. The D-30 negative for the project half |
| **IP2** | *The positive, in the family.* Back in the family; `elements` = the box's UniqueId alone | **A picture of the box alone**, close up, in the chat; the words say 1 element framed. The D-30 positive |
| **IP3** | *Both forms by UniqueId* | Both in the picture, 2 framed - the cylinder, at the family's own subcategory, framed too (row 5b-1311). *"has no category"* for it means the build is missing that fix |
| **IP4** | *Mixed, in the project.* `elements` = duct 1's number, duct 2's UniqueId; then duct 1's number and duct 1's UniqueId | The first: 2 ducts framed. The second: **1 framed** - one duct named two ways is pictured once |
| **IP5** | *Nothing kept.* Count the family's forms (`REPORT_FAMILY_FORMS` "all") and the project's elements before IP2 and IP4, and after; open Undo's list | The same counts; no Heron entry in Undo; the words say the temporary view was rolled back |
| **IP6** | *Numbers as before.* Group ER's ER2 call - the three duct numbers and the level's | The same answer ER2 records: 3 framed, the level *is a datum* |
| **IP7** | *Revit 2024 and 2027* - IP0 (a), IP2 and IP4 again, a family and a project made the same way on each | The same. 2024 is the first release with 64-bit ids, so a UniqueId's hex tail is read there for the first time |

**Then** row 5b-1314 can close, and the family method's step 13 can move from views by name to `elements` with
`SELECT_FAMILY_FORMS`' ids - once row 5b-1311's fix has passed its own rows too.
