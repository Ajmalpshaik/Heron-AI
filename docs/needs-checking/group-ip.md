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

### Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544

**The add-in that ran.** Built from main at `239d0b17` (Debug, 0 warnings, 0 errors each) and deployed with
`tools/deploy-addin.ps1` for 2020, then 2024, then 2027, Revit closed; each release's deployed
`Heron.Revit.Addin.dll`, `Heron.Bridge.dll` and `Heron.Core.dll` hash-equal to its build and stamped
`0.1.0+239d0b17`. Revit 2020 was started after the deploy (add-in log: *"Heron loaded. Revit 2020 ... pid
60544"*). 2026 was not rebuilt: its deployed add-in, `a74a42e7`, has the same `revit/` as `239d0b17`. The
Claude app was not restarted - this chat's Heron server started after the last change to Heron's Python.

**The arrangement as run - not the one written above.** The owner went to sleep after connecting Heron,
and with *Ask to Keep* on (`write.askBeforeKeeping = true`) every `revit_change` opens a Keep or Discard box
that waits for him, and Revit with it. So no form was made through Heron: every row ran on existing families,
copied to `%TEMP%\heron-work\group-ip` and opened with `ACTIVATE_DOCUMENT` (PROVEN, through `revit_show`).

| Scratch document | What it is | Plays |
|---|---|---|
| *GIP M_Skylight-Pyramid* | A copy of Revit 2020's own `M_Skylight-Pyramid.rfa` (`Libraries\US Metric\Windows`): 9 forms - solids at the subcategories Glass and Frame/Mullion, and two voids at the family's own subcategory, with no category | The family. The "box with a subcategory" is the Glass solid `a8cc9e63-4da8-4dd2-9b35-df4c8c3f3d63-00009df9` (40441); the form left as made is the void `adb89b7a-2040-4618-8ae2-3b66e7c5b964-0000961f` (38431) |
| *IO7 scratch - CCTV Monitor 24 Inch* | A copy of the owner's 2026-10-21 family: 111 forms, every one at the family's own subcategory, and 3 electrical connectors | IP3's second family: forms with no category at all |
| *Heron Capture Test* | A copy of Revit 2020's `rme_basic_sample_project.rvt`: 21,317 elements, 724 ducts | The project. Ducts 392168, 392170, 392174 (`7485c179-9364-48d7-b68b-6736718012c4-0005fbe8`, `...-0005fbea`, `...-0005fbee`), Level 1 378117, Grid A 586795 |

Numbers and UniqueIds were read with a read-only probe (scratch C# sent as `run_fragment_read` under
`find-views`' contract, no transaction open) and agree with `REPORT_FAMILY_FORMS`: on 2020 a form's number is its
UniqueId's last eight hex digits. Element counts are each read's own *"Heron counted N element(s) before ... and
the same after"*; views are `revit_views`'; Undo is the ribbon's Undo button read with UI Automation, read-only -
every document was opened fresh, so an entry left by a picture would have enabled it.

| # | Release | What was run and what came back | State |
|---|---|---|---|
| IP0 | 2020 | Skylight family. (a) The Glass solid's id with its GUID's last digit 3 made 4: *"1 of the 1 id(s) name no placed element in "GIP M_Skylight-Pyramid": a8cc9e63-4da8-4dd2-9b35-df4c8c3f3d64-00009df9. An id belongs to one model, or one family, and is gone once its element is deleted. Nothing was pictured."* (b) `abc` and the id: *"\"abc\" is not an element id. Give the ids as Heron's answers print them - Revit's number, digits only, or the long id the family form tools give back - comma separated, with nothing else in the list. Nothing was pictured."* (c) The id twice: *"a8cc9e63-4da8-4dd2-9b35-df4c8c3f3d63-00009df9 is typed more than once - type each id once. Nothing was pictured."* No picture each time; 10 views, 1 of them 3D, before and after; Undo disabled when read afterwards. The tool prints the words, not the error code | **PASS** |
| IP1 | 2020 | *Heron Capture Test* in front, the Glass solid's id: *"1 of the 1 id(s) name no placed element in "Heron Capture Test": a8cc9e63-4da8-4dd2-9b35-df4c8c3f3d63-00009df9. An id belongs to one model, or one family, and is gone once its element is deleted. Nothing was pictured."* | **PASS** |
| IP2 | 2020 | Skylight, the Glass solid's id alone: *"A picture of 1 element(s) in GIP M_Skylight-Pyramid (Revit 2020, session 60544), in a temporary 3D view, 1280 px wide. The temporary view is gone - Revit rolled it back. Nothing was changed."* **The picture:** the skylight close up, from above at an angle - its brown curb, the sloped glass panes and the blue frame, on a slab of the host roof the section box cuts square. The pane asked for is one of the sloped panes; its neighbours are in the picture too, because the box is the pane's own plus a margin of at least a foot | **PASS** |
| IP3 | 2020 | (i) Skylight, the Glass solid's id and the void's (no category - row 5b-1311): *"A picture of 2 element(s)"*; the frame grew to take in the void - the whole skylight, centred on its square of roof. (ii) CCTV copy, two forms at the family's own subcategory by id, `ce590062-ddf7-4c0d-99c8-3603c9951e09-00000a99` (the screen) and `...-00001987` (the box behind it): *"A picture of 2 element(s)"*, the screen in 3D. No *"has no category"* in either | **PASS** |
| IP4 | 2020 | Project. `392168, 7485c179-9364-48d7-b68b-6736718012c4-0005fbea` (duct 1 by number, duct 2 by UniqueId): *"A picture of 2 element(s)"*. `392168, 7485c179-9364-48d7-b68b-6736718012c4-0005fbe8` (duct 1 both ways): *"A picture of 1 element(s)"* - one rectangular duct running under a strip of the floor slab, two boxes hanging from the run | **PASS** |
| IP5 | 2020 | Skylight: 9 forms, 1,872 elements, 10 views (1 3D) before IP0 and after IP3 and IO2. Project: 21,317 elements, 140 views (11 3D) before IP1 and after IP4 and IP6. Undo disabled in both afterwards. Every picture's words: *"The temporary view is gone - Revit rolled it back. Nothing was changed."* `%LOCALAPPDATA%\Heron\captures` empty at the end | **PASS** |
| IP6 | 2020 | Project, ER2's call: `392168,392170,392174,378117`: *"A picture of 3 element(s) in Heron Capture Test ... Left out of the frame: 378117 is a datum (Levels). The temporary view is gone - Revit rolled it back. Nothing was changed."* - ER2's words. **The picture is mostly the top of the floor slab above the ducts**, walls and framing on it; the ducts show only as dark rectangles where the box cuts them. The box is the ducts' own plus at least a foot, and that reaches the slab above - [row 5b-1328](../fragment-issues/section-5b-rows-401-425.md) | words **PASS**; the picture is row 5b-1328 |
| IP7 | 2024, 2027 | Not run. Neither Revit was open, and Heron connects only when the owner presses Heron on the ribbon (`bridge.autoConnect = false`); pressing it by UI Automation was refused by the session's auto mode on 2026-10-17. The add-in carrying this change is deployed on both, stamped `0.1.0+239d0b17` | **NOT RUN** |

**Seen beside the rows.** In the CCTV copy every picture carries large green circles with cross lines and two
small blue markers, at the same pixels whatever was framed - including the base plate, whose box (Z -1225 to
-570 mm) cannot hold the family's connectors at Z 54 mm. The skylight, which has no connectors, showed none and
was framed tight. Most likely the connector symbols, which the temporary view does not hide and its fit takes
in, so the forms asked for come out small - [row 5b-1327](../fragment-issues/section-5b-rows-401-425.md). And the
frame follows what is asked for: the base plate alone pictured the stand's H base and castors, the screen out of view.

**Left for the owner.** IP7 on Revit 2024 and 2027 - open each on a scratch family and project, press Heron.
Row 5b-1314 closes on IP0 to IP6 here and IP7 there; there is no card to sign, `capture_view` being the add-in's own.

### Results - 2026-10-23, the owner's laptop: Revit 2024.3 (24.3.60.12), session 83588 - IP7's 2024 half

The add-in deployed 2026-10-22 from main at `239d0b17` (stamp `0.1.0+239d0b17`). Revit 2024 started on a scratch
copy of its own `Snowdon Towers Sample HVAC.rvt` (*N23 Snowdon HVAC 2024*, `%TEMP%\heron-work\night-1023\models`),
the owner pressed Heron; *GIP M_Skylight-Pyramid* - the same Revit 2020 library copy as above - opened in 2024
and upgraded in memory, never saved. The forms' UniqueIds read the same as on 2020; ducts' ids from the probe.

| # | Release | What was run and what came back | State |
|---|---|---|---|
| IP7 (a) | 2024 | Skylight, the Glass solid's id with its GUID's last digit 3 made 4: *"1 of the 1 id(s) name no placed element in "GIP M_Skylight-Pyramid": a8cc9e63-4da8-4dd2-9b35-df4c8c3f3d64-00009df9. An id belongs to one model, or one family, ..."*. Nothing pictured | **PASS** |
| IP7 IP2 | 2024 | Skylight, `a8cc9e63-4da8-4dd2-9b35-df4c8c3f3d63-00009df9` alone: *"A picture of 1 element(s) in GIP M_Skylight-Pyramid (Revit 2024, session 83588) ... The temporary view is gone - Revit rolled it back. Nothing was changed."* **The picture:** the same framing as 2020's IP2 - the skylight close up on its roof - on 2024's dark background | **PASS** |
| IP7 IP4 | 2024 | *N23 Snowdon HVAC 2024*: `1365662, 0b202da0-7e91-481c-adf0-ed84186e6285-00150c2e` (duct 1 by number, duct 2 by UniqueId): *"A picture of 2 element(s)"* - two short round duct runs meeting at a tee in front of a box-shaped unit. `1365662, ee75abd3-0b00-4374-8f65-b8d52f0bb76e-0014d69e` (duct 1 both ways): *"A picture of 1 element(s)"* - the tee alone, the frame closed in. 2024 is the first release with 64-bit ids; the UniqueIds' hex tails resolved | **PASS** |
| IP7 | 2027 | Not run: Revit 2027 not open | **NOT RUN** |

Row 5b-1314: IP0 to IP6 on 2020 and IP7 on 2024 pass; IP7 on 2027 is the one row left.
