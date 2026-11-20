# Needs checking — Group DH

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DH - element ids typed back into Heron, through a read that names each one (2026-10-12)

**Tracked as GitHub issue [#992](https://github.com/Ajmalpshaik/Heron-AI/issues/992) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

On Revit 2020, `PLACE_FAMILY_INSTANCES` answered with the ids of three new units - 8453537, 8453556,
8453557 - and no capability would take them back. To turn and mirror each one, the chat selected it
with `SELECT_IN_REGION` drawn round a spot only that unit stood in
([row 5b-441](../fragment-issues/section-5b-rows-426-450.md)).

**What was built, by widening - no new capability.** The add-in's binder takes ids typed as digits for
the need named `elementIds`, **on a read only**, matched as TEXT against `ElementId.ToString()` in the
document being read; `FILTER_ELEMENTS_BY_ID` version 2 (FRG-ELE-003) adds `named`, each element's id,
category and "Family: Type". A change then takes exactly those elements:

    revit_read   FILTER_ELEMENTS_BY_ID   elementIds=8453537
    revit_change ROTATE_ELEMENTS         angleRadians=1.5708, pivot=<the unit's point in mm>
                 expect_from="filter-elements-by-id where elementIds=8453537"

**Refused by name before anything binds:** an id naming nothing in the model being read (every one
named, nothing bound), an id beside a name, an id typed twice, and typed ids on a write. Version 1's
proof is kept as a record and the card reads STALE until it is proved again.

**Measured without Revit, 2026-10-12.** The text rules (`HeronBindingNote.TypedIds`,
`TypedIdsNotFound`, `TypedIdsOnAWrite`) pass `tests/test_binding_note.py` on this PC, which also
reads `RevitFragment.cs` for the read-only guard and for no `new ElementId(` in code;
`tests/test_how_to_type.py` holds the hint to the same C#. The fragment compiles on all eight
releases (`tools/check-fragments-compile.py`). **Nothing below has run in Revit** - in particular,
that `ElementId.ToString()` prints exactly the digits Revit shows in Manage > IDs of Selection is
read from the add-in's own use of it (`Describe`, `HeronLiveState`), not measured here.

Never on a model the owner works in: a scratch copy, named in each answer. With
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and
`< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DH1** | Arrange on **Revit 2020**, a scratch copy of a project: `PLACE_FAMILY_INSTANCES` three units of one mechanical equipment type at three points, kept. Write down `placed` | three ids, and the three units visible where asked |
| **DH2** | Positive: `revit_read FILTER_ELEMENTS_BY_ID elementIds=<the three ids, as printed>` | `elements` 3, `missing` 0, and `named` "3 element(s): <id> Mechanical Equipment - "<Family>: <Type>"; ..." in the order typed - each matching the Properties palette with that unit selected (the second route: Manage > Select by ID, the same three) |
| ~~**DH3**~~ | The chain: `revit_change ROTATE_ELEMENTS` on ONE of them - DH2 again with only its id, then `expect_from="filter-elements-by-id where elementIds=<that id>"`, 90 degrees about its own point, kept. Then `MIRROR_ELEMENTS` on a second unit the same way | only that unit turns, and only the second mirrors; the third does not move. The job the row was found on, with no `SELECT_IN_REGION` **HELD 2026-11-20 on Revit 2020 (build 20191106_1200, add-in 0.2.0.0 from main de6dcbad), *House Scratch 1042* - a copy of Revit 2020's `rac_basic_sample_project.rvt` in `%TEMP%\heron-work\issues-1120`, for issue [#899](https://github.com/Ajmalpshaik/Heron-AI/issues/899)**, with three units of one type placed for it (`PLACE_FAMILY_INSTANCES`, *Column Scratch 1044*, ids 1102688-1102690) - the sample's bar chairs would not turn (`ROTATE_ELEMENTS` read back *"0 turned, 1 accepted the call and did not move (a group member ...)"* for 988128). `FILTER_ELEMENTS_BY_ID elementIds=1102688` (*"1 element(s): 1102688 Columns ..."*), then `ROTATE_ELEMENTS` 90 degrees with `expect_from="filter-elements-by-id where elementIds=1102688"`, kept: *"1 turned"*, rotatedIds [1102688]. The same for 1102689 with `MIRROR_ELEMENTS` about X 42500: mirrored 1. Read back with `REPORT_BOUNDING_BOX` (DRAFT, a read): 1102688 1000 x 1300 mm (turned), 1102689 lowest corner X 42200 (mirrored from 41500), 1102690 1300 x 1000 at X 43500 - not moved. No `SELECT_IN_REGION`. |
| **DH4** | **Negative - an id naming nothing:** `elementIds=<one real id>, 99999999` | **refused**, naming 99999999, "1 of the 2" and the model; NOTHING WAS BOUND - the real id is not bound alone, and no fragment ran |
| **DH5** | The other refusals, each its own call: `<id>, Level 1`; `<id>, <the same id>`; an id read in DH1, after Ctrl+Z has removed that unit | each refused with its reason, nothing bound. The undone one is named as naming nothing |
| **DH6** | `expect_from` that disagrees: DH2 with id A, then ROTATE with `where elementIds=<id B>` | **refused** `chain_inputs_differ`; nothing turned - the carry check, not this row's, but the half that makes the read-then-write safe |
| **DH7** | The write guard: a scratch card under a MODIFY contract declaring `elementIds` (request), typed ids | **refused**, naming FILTER_ELEMENTS_BY_ID and expect_from. No production card declares it today, so this is a scratch run only |
| **DH8** | An element TYPE's id (from a type list) and a view's id | found, and `named` names the type, and the view by its name with its category |
| **DH9** | DH2 and DH4 on **Revit 2024** and **2027** - 64-bit ids, the release where the constructor changed | the same answers. Compiled on all eight releases; what Revit DOES was measured on none **Revit 2024 half held 2026-11-20 on Revit 2024 (build 24.3.60.12, add-in 0.2.0.0), *Rig 1040* - a file copy of `night-1116\rig.rvt` (Heron's DefaultMetric scratch with kept walls, floors, beams) in `%TEMP%\heron-work\issues-1120`, for issue [#899](https://github.com/Ajmalpshaik/Heron-AI/issues/899):** DH2 - `FILTER_ELEMENTS_BY_ID elementIds=352528, 352589, 352591`: `elements` 3, `missing` 0, *"3 element(s): 352528 Floors - \"Floor: Generic 150mm\"; 352589 Structural Framing - \"UB-Universal Beams: UB305x165x40\"; 352591 Structural Framing - ..."*, in the order typed; DH4 - `elementIds=352528, 99999999`: `bad_request_value`, *"1 of the 2 element id(s) typed name no element in 'Rig 1040': 99999999 ... NOTHING WAS BOUND."* Not struck: the 2027 half is owed. |
| **DH10** | The signature, on the owner's word only, after DH2, DH3 and DH4 | `python brain/heron_validate.py accept filter-elements-by-id --by "Ajmal PS"` |

**Not built here, and recorded:** `PLACE_FAMILY_INSTANCES` leaves `placed` and `made`, not the
`created` every other creator leaves, so `expect_from="place-family-instances"` cannot hand its
units to `ROTATE_ELEMENTS` directly - row 5b-441 says why that was not the fix for this job.
