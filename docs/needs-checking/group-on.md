# Needs checking — Group ON

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group ON - ASSIGN_LOCATION_DATA version 3: room or space name and number into two parameters, a space tested by its boundary, rooms in a link when asked, DRAFT until proved (2026-11-19)

**Tracked as GitHub issue [#1059](https://github.com/Ajmalpshaik/Heron-AI/issues/1059) since 2026-11-19** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** on 2026-11-19 the owner asked for every duct accessory's `ID_Room Name` and `ID_Room Number` on a
real job model to be filled from the MEP space it sits in. Version 2 would have glued the number onto the
name, had no input for the number, and tested a space by its bounding box, so the job was stopped before
anything was written ([row 5b-1856](../fragment-issues/row-5b-1856.md)).
[`assign-location-data`](../../brain/fragments/assign-location-data/fragment.yaml) **version 3**:

- `roomParameter` takes the room's or space's Name parameter (`ROOM_NAME`) **alone**; new optional
  `numberParameter` takes its Number; `levelParameter` as before. All three optional (`absent: none`).
- A space is tested with `Space.IsPointInSpace`, a room with `Room.IsPointInRoom` - never a bounding box.
  Zero-area containers are skipped, as `COUNT_BY_SPATIAL_CONTAINER` skips them.
- **New logic - the storey is checked.** A container that has the element in plan (Revit's test at the
  container's mid-height, or at its location point's height as COUNT_BY asks) is taken only when the
  element is **inside** it (Revit's test at the element's own point) or **above it but below the top of
  its storey** - its location point plus the height from its level to the next **storey** level up
  (Building Story ticked; every level when none is). Anything else -
  another floor, below the floor, an unknown storey top, two equally good - is **not written** and is
  named with the container and the reason. So on a multi-storey model it can write fewer than COUNT_BY
  counts in a space, never another floor's name. A "ceiling" or slab level ticked as a storey (or any
  level, in a model with none ticked) makes the storey short; ceiling-void elements above it are then
  named, not written.
- Each target must be a text parameter, and every value written is read back.
- **`includeLinks`** (D-59, optional, absent = host only) - added the same day on Ajmal PS's word, *"YES
  ADD LINKED ROOMS TO THE FIX"*: his job's rooms are in the architect's link, which a dry run with
  `useSpaces=false` never saw. Rooms or spaces of every loaded link are asked too, through each
  placement's transform, with the link's own levels for the storey - `COUNT_BY_SPATIAL_CONTAINER`'s
  mechanism. **This model's own container wins** where both pass the storey check, and the answer counts
  it; `linksSearched` and `linkedMatches` say what was read and which link each value came from.

**DRAFT again at a new version; version 2's proof block is kept on the card as the record of what it
proved** (rooms only, on Project2). **Spaces and links have never been proved on this card.** It compiles
on all eight releases (`tools/check-fragments-compile.py`, 2026-11-19, the owner's PC, no Revit run).
**Nothing here has run in Revit.** Read from the code and owed a real Revit: that `ROOM_NAME` on a space
gives the name alone; that `Level.ProjectElevation` and a container's location point Z agree on where its
level is (the storey top is the location point plus a difference of two levels' elevations); how the
tests behave on spaces with volume computation off; and whether rooms of two phases in one place tie.

**The model, every row: a scratch copy, never a working model.** Copy
`C:\Program Files\Autodesk\Revit 2020\Samples\rme_advanced_sample_project.rvt` (read-only - `chmod u+w`
the copy) to `%TEMP%\heron-work\on\on-mep.rvt`. ON0L adds a linked copy, `on-arch.rvt`, in the same
folder. **Opening it in a Revit that has a working model in front needs the owner's word first:**
`ACTIVATE_DOCUMENT` has been seen to write the document it leaves ([row 5b-1851](../fragment-issues/row-5b-1851.md),
Group OL) - so either the working model is saved and closed first, or the copy is opened in a Revit with
nothing else open. Every run below aims at it with `--in on-mep`; read the document name on every reply.
`includeLinks` given in a run's values reaches the setup filter too: `FILTER_ELEMENTS_BY_CATEGORY` then
reports linked matches as text and never selects them, so the selection is unchanged.

| ID | Do this | Pass looks like |
|---|---|---|
| **ON0** | *Arrange and measure, on `on-mep`.* (1) `revit_levels` and `REPORT_LEVEL_ELEVATIONS` - which storeys hold what, and which levels have Building Story ticked. (2) `COUNT_BY_SPATIAL_CONTAINER` `containerKind=space` on Air Terminals, then on Duct Accessories, each on Level 1 (`FILTER_ELEMENTS_BY_CATEGORY` `levelId=Level 1`) - how many land in a space and how many in none. (3) `REPORT_ROOM_SPACE_DATA` on the Level 1 spaces - each space's name and number as Revit holds them, and `roomsSeen` 0. (4) `ADD_PROJECT_PARAMETER` twice, kept on the scratch copy: `ID_Room Name` and `ID_Room Number`, text, instance, bound to Air Terminals and Duct Accessories. Save the copy | Counts written down. At least one Level 1 air terminal (or duct accessory) in a space and the model holding no rooms. If no air terminal lands in a space, change the job file's category to the one that does, and say so here |
| ~~**ON1**~~ | *Positive - name and number apart.* [`tools/jobs/assign-location-data-v3-on-2026-11-19.yaml`](../../tools/jobs/assign-location-data-v3-on-2026-11-19.yaml), rolled back: Level 1 air terminals, `useSpaces=true`, `roomParameter=ID_Room Name`, `numberParameter=ID_Room Number`, `levelParameter=none` | `written` equals ON0's landed count, or fewer with every missing one named in `refused` with its space and reason; `nameWritten` and `numberWritten` equal `written`; `notKept 0`; every `samples` line reads *name "X" \| number "N"* with **no number inside X**, each pair matching a space in ON0 (3). The binding note names the selection ON0 measured |
| ~~**ON2**~~ | *Negative - the same selection, asked for rooms the model has not got.* The job file's negative leg: `useSpaces=false`, the rest unchanged | `written 0`, every element in `noRoomFound`, `refused` saying the model has no placed and enclosed ROOMS of its own and to ask with includeLinks. Nothing falls back to the spaces or to a box |
| **ON3** | *Read back, kept, on the scratch copy only.* The ON1 values kept once on `on-mep`, then `READ_ELEMENT_PARAMETERS` for `ID_Room Name, ID_Room Number` on the same elements | Each element reads exactly its `samples` pair - the name alone, the number alone. A count is not this row's evidence; the read is |
| **ON4** | *Second route - agreement with the count.* `COUNT_BY_SPATIAL_CONTAINER` (space) on the same elements, beside ON3's read | Every element COUNT_BY puts in a space is either written that space's number, or named in ON1's `refused` with a reason (another storey, below the floor, unknown storey top). COUNT_BY's label shows the number twice ([row 5b-1858](../fragment-issues/row-5b-1858.md)) - compare on the number |
| **ON5** | *The boundary, not the box.* An element placed (rolled back, `PLACE_FAMILY_INSTANCES` in `setup:`) inside an L-shaped or irregular space's bounding box but outside its boundary - find one in ON0 (3) by its shape | NOT given that space: written to the space it is really in, or named in `noRoomFound`. Version 2 gave it the box's space. If the sample has no such space, write NOT RUN - no irregular space, and the reason |
| **ON6** | *Stacked storeys, the ceiling void.* Only if ON0 (1) finds spaces on two storeys over one another: an element in the lower storey's ceiling void, an upper space over it in plan | Given the LOWER space, `refused` counting it as settled by storey. Version 2 and COUNT_BY take whichever answers first. If no stacked spaces, NOT RUN - single storey, and say so |
| **ON11** | *Another floor, one container.* An element placed (rolled back) on a storey above Level 1 directly over a Level 1 space, with no space on its own storey there | **NOT written**, in `noRoomFound`, and named in `refused`: *"in plan of the SPACE '...', but it sits ABOVE that space's storey - a higher floor"*. Version 2 wrote Level 1's name onto it. Needs a level above Level 1; if the sample has none, NOT RUN and why **The same rule seen on ROOMS 2026-11-20 on Revit 2020 (build 20191106_1200, add-in 0.2.0.0), not `on-mep` but *House Scratch 1042* (a copy of `rac_basic_sample_project.rvt`, ROOMS, no spaces) with *Datum Link 1036.rvt* (a second copy, 14 placed rooms) linked, for issue [#1059](https://github.com/Ajmalpshaik/Heron-AI/issues/1059); the 9 Plumbing Fixtures on Level 2 (`FILTER_ELEMENTS_BY_CATEGORY`), writing into their Comments and Mark; rolled back** (not struck - the row is about a space): `useSpaces=false roomParameter=Comments numberParameter=Mark` - written 5, name and number apart (*name "Master Bath" \| number "207"*, *name "Bath" \| number "203"*), and 937572 and 938762 NOT written: *"in plan of the ROOM 'Kitchen & Dining 101', but it sits ABOVE that room's storey, whose top is the storey level 'Ceiling'"*. |
| **ON0L** | *The link rig, on the same scratch folder.* If ON0 found a loaded link holding rooms over `on-mep` (`LIST_LINKED_MODELS`, then `COUNT_BY_SPATIAL_CONTAINER` `containerKind=room`, `includeLinks=true`), use it and skip the rest of this row. Otherwise: copy `on-mep.rvt` again as `on-arch.rvt`; open it (aim with `--in on-arch`); `PLACE_ROOMS` on Level 1 so rooms sit where its spaces are; save and close it; then `LINK_FILES` it into `on-mep`, `placement` origin to origin, kept on the scratch copy. Save `on-mep` | `COUNT_BY_SPATIAL_CONTAINER` `containerKind=room`, `includeLinks=true` on ON1's elements: `linksSearched 1`, groups named *"on-arch :: ..."*, and `on-mep` itself still holding no room |
| ~~**ON8**~~ | *Linked rooms - positive.* ON1's elements, `useSpaces=false`, `includeLinks=true`, `roomParameter=ID_Room Name`, `numberParameter=ID_Room Number`, `levelParameter=none`, rolled back - by hand: `validate --in on-mep --out brain/proof-drafts/runs/assign-location-data@links.json ... assign-location-data` | `written` equals ON0L's landed count, or fewer with each missing one named with its reason; every `samples` line ends *"- from the link on-arch"*, the name without the number; `linksSearched 1`; `linkedMatches` first line *"1 link(s) read: N element(s) given a linked ROOM, 0 one of this model's own"* |
| ~~**ON9**~~ | *Linked rooms - negative.* The same, `includeLinks` left out; then once more with `on-arch` UNLOADED (Manage Links > Unload) and `includeLinks=true` | First: `written 0`, `linkedMatches` *"Host model only - links not read"*, the no-rooms line telling the modeller to ask with includeLinks - this is ON2. Second: `linksSearched 0`, *"Links asked for, NONE loaded - host only"*, `written 0`. Reload `on-arch` afterwards |
| ~~**ON10**~~ | *This model's own wins.* ON1's elements, `useSpaces=true`, `includeLinks=true` - `on-arch` carries the same spaces as `on-mep`, so both hold every element | Every value from `on-mep`'s own spaces (no *"from the link"* in `samples`), `refused` counting each as held by both - *"this model's own was written, never the link's"* **The same rule seen on ROOMS 2026-11-20 on Revit 2020 (build 20191106_1200, add-in 0.2.0.0), not `on-mep` but *House Scratch 1042* (a copy of `rac_basic_sample_project.rvt`, ROOMS, no spaces) with *Datum Link 1036.rvt* (a second copy, 14 placed rooms) linked, for issue [#1059](https://github.com/Ajmalpshaik/Heron-AI/issues/1059); the 9 Plumbing Fixtures on Level 2 (`FILTER_ELEMENTS_BY_CATEGORY`), writing into their Comments and Mark; rolled back** (not struck - the row is about spaces): `includeLinks=true` - *"1 link(s) read: 0 element(s) found in a linked ROOM, 5 in one of this model's own"*, *"Datum Link 1036: 14 placed and enclosed ROOM(s) asked"*, refused counting *"3 element(s) sit in one of this model's own ROOMS AND in a linked one - this model's own was written, never the link's"*; samples as host-only. |
| **ON12** | *Version 4 - a success is not a refusal ([row 5b-2037](../fragment-issues/row-5b-2037.md), added 2026-11-26).* The card is version 4 since then: its closing summary moved from `refused` to a new `findings` output. Read both on ON1's reply - no extra run is needed | `refused` holds only what was declined, and on a run where nothing was declined it is **empty**; the summary line *"N element(s) written, using SPACES - name N, number N, level 0; ..."* is in `findings`. Version 3 put that line in `refused` on every run, so every success read as "refused 1" (seen 2026-11-24, Revit 2024, *Night Rig 1124*) |
| ~~**ON7**~~ | Nothing named, and version 2's call. (a) `roomParameter`, `numberParameter` and `levelParameter`… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | (a) `written 0`, `refused` "Nothing was named to fill", nothing touched. |

**A second route for a person:** schedule the elements with `ID_Room Name` and `ID_Room Number` as
columns beside a Level 1 plan showing the spaces' tags (ON3 kept on the scratch copy).

**Then:** the owner signs from the draft ON1 and ON2 leave
([fragment-proving skill](../../.claude/skills/fragment-proving/SKILL.md)) - **but only once ON8 and ON9
have passed too**, because the link half is the code his own job needs, and the job file runs the space
half alone. ON5, ON6, ON10 and ON11 each pass or are written NOT RUN with the reason, and ON12 passes on ON1's reply, before he signs.
Until he does, the card stays DRAFT, and `revit_change` refuses it for a kept run on any model (D-117).

### Results, 2026-11-26 - Revit 2020, session 32484, add-in 0.3.0.0 (file `df0d401c52ca05f4`), card version 4 from main

**Model:** *P1102 MEP* - a FRESH scratch copy of Revit 2020's `rme_advanced_sample_project.rvt` in
`%TEMP%\heron-work\p1102`, standing in for `on-mep` (the same sample, freshly copied, so one model fewer loaded).
**The sample is not as this group assumed:** it holds **76 placed rooms** (2 unplaced) beside 89 spaces, and no
duct accessories at all. Proof job
[`on-assign-location-data-v4-p1102-...`](../../tools/jobs/on-assign-location-data-v4-p1102-revit2020-2026-11-26.yaml) -
**PASS** (ON1 positive; negative: the same 98 with a target parameter they do not carry - written 0), a draft that,
by this group's own rule, is signed only after ON8 and ON9.

| Row | Result |
|---|---|
| ON0, run 2026-11-26 | **DONE.** (1) 4 levels: Level 1 94.2 mm, Level 2 3800 mm, Level 3, Roof Level. (2) Level 1 Air Terminals by space: **98 of 98 landed in one** (24 spaces); Duct Accessories: **0** on any level - the model has none. (3) Level 1 spaces: 30, `roomsSeen` 0 for the spaces; but the model holds 78 Rooms, 76 placed (`REPORT_ROOM_SPACE_DATA` on Rooms: `unplaced` 2). (4) `ADD_PROJECT_PARAMETER`, KEPT on the scratch copy: *ID_Room Name* and *ID_Room Number*, TEXT, instance, bound to Duct Accessories and Air Terminals, from a NEW scratch file `%TEMP%\heron-work\on\on-shared.txt` (`createFile`, `createMissing`); the finding says *"Revit's shared parameter file setting was put back to 'D:\04_BIM\06_Shared_Parameters\Shared_Parameters.txt'"*. Not saved. |
| ON1, run 2026-11-26 | **PASS** (rolled back) - the 98 Level 1 air terminals, `useSpaces=true`: *"98 element(s) written, using SPACES - name 98, number 98, level 0; 98 found in one of this model's own SPACES, 0 in a linked one."*, `nameWritten` 98, `numberWritten` 98, `notKept` 0, `noRoomFound` 0; samples *383543: name "Archive" \| number "21"*, *386293: name "Washing Room" \| number "25"* - no number inside a name. |
| ON2, run 2026-11-26 | **CANNOT PASS AS WRITTEN on this sample** - `useSpaces=false`: *"97 element(s) written, using ROOMS"*, `noRoomFound` [408625] - the sample has rooms. And **`COUNT_BY_SPATIAL_CONTAINER containerKind=room` on the same 98 says "4 landed in one, 94 did not"** - the two tools disagree about rooms (they agree about spaces, 98 and 98). Recorded as row [5b-2057](../fragment-issues/row-5b-2057.md), issue [#1457](https://github.com/Ajmalpshaik/Heron-AI/issues/1457). |
| ON4, run 2026-11-26, totals only | **AGREE on totals** - COUNT_BY (space) puts all 98 in a space; ON1 wrote 98. Not compared element by element (ON1 prints 10 samples). |
| ON12, read on ON1's reply | **HALF** - the summary line is in `findings`, as version 4 says. But `refused` is **not empty** on a run where nothing was declined: it holds *"97 element(s) were in plan of more than one SPACE - another storey's - and were given the one on their own storey"* and *"2 SPACE(S) in this model are UNPLACED or UNENCLOSED and were not asked"*. ON6's own text wants such storey counts in `refused`, so this row and ON6 disagree - a question for the owner, not recorded as a defect. |
| ON0L, ON8, ON9, ON10 | **NOT RUN** - the link rig needs a second model loaded, and Revit's committed memory stood at 11.7 GB (the stop is about 13). |
| ON3 | **NOT RUN** - a DRAFT's kept run raises the Keep box (D-119), the owner's press. |
| ON5, ON6, ON11 | **NOT RUN** this sitting - each needs an element placed in setup at a chosen point; owed. |

### Results, later 2026-11-26 - Revit 2020, session 27788 (after a Revit restart), add-in file `df0d401c52ca05f4`

**Model:** *on-mep* - a fresh scratch copy of Revit 2020's `rme_advanced_sample_project.rvt` in `%TEMP%\heron-work\on`,
the *ID_Room* parameters added again (kept) from the same scratch shared file. Its **78 rooms deleted, KEPT, on the
owner's typed yes** ("Yes, delete them") with `DELETE_ELEMENTS` version 5 (PROVEN that hour, PR #1470) and
`expectedCount=81` - *"81 deleted. Asked for 78: Rooms 78. Also went 3: (no category) Element 3."* - so on-mep holds
no room of its own, as this group assumed. *on-arch* (a second fresh copy, rooms and spaces intact) is linked
**inside each rolled-back run** as a setup step (`LINK_FILES`, DRAFT, origin to origin) - never kept. Unsaved.

| Row | Result |
|---|---|
| ON10, run 2026-11-26 (before the rooms went) | **PASS** - `useSpaces=true`, `includeLinks=true`: `written` 98 from on-mep's own spaces, `linksSearched` 1, *"1 link(s) read: 0 element(s) found in a linked SPACE, 98 in ..."*, `refused` counting the 98 held by both; no sample *"from the link"*. Negative: a target parameter they do not carry - written 0. |
| ON8, run 2026-11-26 | **PASS** - `useSpaces=false`, `includeLinks=true`: `written` 97, `noRoomFound` [408625], `linksSearched` 1, *"1 link(s) read: 97 element(s) found in a linked ROOM, 0 in o..."*, *"on-arch: 76 placed and enclosed ROOM(s) asked"*; samples *383543: name "Archive" \| number "21" - from the link on-arch*, the name without the number. |
| ON9, run 2026-11-26 | **PASS** - (1) `includeLinks` false: `written` 0, *"Host model only - links not read"*, `refused` *"this model has no placed and enclosed ROOMS of its own. If t..."* (ask with includeLinks). (2) `includeLinks=true` with NO link loaded (the link exists only inside a rolled-back run, so none is loaded outside it - in place of Manage Links > Unload): *"Links asked for, NONE loaded - host only"*, `linksSearched` 0, `written` 0. |
| ON2, run again 2026-11-26 | **PASS now** - with on-mep's rooms gone, `useSpaces=false` writes 0 and every element is in `noRoomFound` (ON9 (1) above is the same call). |
| ON1 | Struck on its 2026-11-26 PASS above (98 of 98 by space, name and number apart). |

The link-half proof draft (`validate` record `%TEMP%\heron-work\on\on8.json`: ON8 positive, ON9 (1) negative) passes
`accept`'s evidence check - waiting for the owner. Still owed: ON3 (a kept DRAFT run, the Keep box), ON4 element by
element, ON5, ON6, ON11, ON12's question.

**Signed 2026-11-26 by Ajmal PS** (his typed word in chat, "Yes, sign it"; signature put on by the session): `assign-location-data` version 4, on the link-half record (ON8 positive, ON9 (1) negative), Revit 2020, *on-mep* - card PROVEN, as this group's closing paragraph asks (after ON8 and ON9 passed).
