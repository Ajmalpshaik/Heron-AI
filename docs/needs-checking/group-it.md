# Needs checking — Group IT

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-22 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IT - a dictionary's entries reach the chat (`providesEntries`, `READ_ELEMENT_PARAMETERS` version 5), and the end a flex would move (`LABEL_FAMILY_DIMENSION` version 2) (2026-10-22)

From rows [5b-1319 and 5b-1320](../fragment-issues/section-5b-rows-401-425.md). Both were seen on Revit 2020
(session 78736) on 2026-10-22, building a nested floor-drain family: one parameter read on four connectors
came back as *"values 4 entry(ies)"* and nothing else, and a height labelled from a new, unpinned plane moved
that plane instead of the geometry when flexed, until the plane was pinned by hand. Both fixed the same day
with no Revit:

| What | Where | What changed |
|---|---|---|
| The add-in's reply | [`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs) `Report` | Every dictionary a fragment leaves travels as its count AND its first twelve entries, in `providesEntries`; `Describe` unchanged (5b-1319) |
| The chat's reply, both doors | [`heron_mcp_server.py`](../../mcp/server/heron_mcp_server.py) `_provides_lines`; [`heron_run_records.py`](../../mcp/server/heron_run_records.py) `entries` | Each entry prints under its count - *"    14194: Yes"* - and past twelve how many more; NOT SENT only when no entries came. A run record pages `entry` lines (5b-1319) |
| `READ_ELEMENT_PARAMETERS` v5 | [`read-element-parameters`](../../brain/fragments/read-element-parameters/fragment.yaml) | READ. `findings`, a string, says the answer in words: each id, value and where it came from, the first twelve, then blank and absent ids (5b-1319) |
| `LABEL_FAMILY_DIMENSION` v2 | [`label-family-dimension`](../../brain/fragments/label-family-dimension/fragment.yaml) | MODIFY. Each end is read before the label: a level, pinned, defines the origin, or referenced by a labelled dimension, an EQ or a lock is held; a free end is named in `freeEnd` with the pin route. `pinFreeEnd=true` pins it first, read back. The same-place refusal names the route (5b-1320) |

**What is done and what is not.** The library compiles for 2020 and 2027 (`tools/check-fragments-compile.py`,
2026-10-22) and every Revit member the add-in calls exists in 2020 to 2027 (`tools/check-api-surface.py`); the add-in builds for 2020 to 2027 (`tools/check-compile.py`, all 23 projects, 2026-10-22).
`tests/test_read_door.py` §7 and `tests/test_run_records.py` carry ten new checks that fail on main's server and
pass on this change. **Nothing here has run in Revit.** Both cards are DRAFT: `revit_change` refuses
`LABEL_FAMILY_DIMENSION` until IT6 is signed, and `revit_read` answers `READ_ELEMENT_PARAMETERS` as DRAFT until
IT1 is. **The add-in must be rebuilt and deployed and Revit restarted** on each release before IT1 to IT4 mean
anything - with the old add-in the chat says NOT SENT exactly as before, which IT4 checks on purpose; the
server needs a Claude app restart. The fragments' C# is read live.

**The family.** A scratch family, never a working one: a new family from Revit 2020's *Metric Generic
Model.rft*, saved as *Heron IT Planes.rfa* in `%TEMP%\heron-work\group-it`. For IT1 and IT2 any project with
four duct connectors and twenty walls does - a copy of a sample, named in the run record. Run every row on
**Revit 2020**, then IT1 and IT6 again on the newest Revit on the PC.

| # | Run | Look for |
|---|---|---|
| **IT0** | *Negative FIRST.* The four connectors of one duct selected (`FILTER_ELEMENTS_BY_ID`, or Revit's selection), then `revit_read READ_ELEMENT_PARAMETERS parameterName=ZZZNOTHINGHERE` | `findings` reads *"ZZZNOTHINGHERE": 0 of 4 carry a value, 0 blank, 4 absent. Absent: <the four ids>.*; `values 0 entry(ies)` with NO line under it and NO "NOT SENT"; `absent 4 item(s)` |
| **IT1** | *The fault's own case.* The same four connectors, `parameterName=Allow Slope Adjustments` | The reply shows `values 4 entry(ies)` and under it four lines *"<id>: Yes"* (or No), `resolvedFrom 4 entry(ies)` and under it four *"<id>: instance"*; NO "NOT SENT" anywhere; `findings` names all four ids with their values. **Copy the reply's `values` block into the run record whole** - it is the proof that the entries travel |
| **IT2** | *Past twelve.* Twenty walls selected, `parameterName=Comments` with every Comments filled in (SET_PARAMETER_VALUES_BY_ID first if needed) | Twelve *"<id>: <text>"* lines under `values 20 entry(ies)` then *"... and 8 more: the first 12 of 20 are shown"*; `findings` names twelve and says *"... and 8 more - read fewer at a time, or GROUP_AND_COUNT"* |
| **IT3** | *A write's record.* Any `revit_change` whose reply shows an `N entry(ies)` value (one the card provides as a dictionary), kept, then `heron_run_record run=<its id>` | The reply shows that dictionary's entries under its count, and the record pages *"entry     <name>  <key>: <value>"* lines right under *"value     <name>  N entry(ies)"* |
| **IT4** | *An add-in from before the field.* On a Revit whose Heron add-in has NOT been rebuilt from this change, IT1 again | *"NOT SENT: what is in resolvedFrom and values ..."* exactly as before this change - the server does not invent entries it was not sent |
| **IT5** | *Negative FIRST for the label.* In the scratch family: `CREATE_REFERENCE_PLANES` "Box Top" at Z 250; `SET_PIN_STATE` pins it; `ADD_FAMILY_PARAMETERS` Height (length); `SET_FAMILY_TYPE_VALUES` Height=250; then `LABEL_FAMILY_DIMENSION fromPlane=Ref. Level toPlane=Box Top parameterName=Height` | `labelled` *Height drives "Ref. Level" to "Box Top", reading 250 mm*; `freeEnd` EMPTY; a finding *"Each end is held: "Ref. Level" is a level; "Box Top" is pinned."*; `pinnedEnd` empty; nothing pinned by this call. **No free-end warning** - this is the negative case |
| **IT6** | *The fault's own case.* A new family: "Trap Top" at Z 300 with TWO other labelled dimensions on it (e.g. Trap Top to a "Rim" plane labelled Rim Height, and an EQ), "Floor Top" a NEW plane at Z 0 - unpinned, nothing on it; "Trap Depth" (length) = 300; `LABEL_FAMILY_DIMENSION fromPlane=Floor Top toPlane=Trap Top parameterName=Trap Depth`, `pinFreeEnd` left out | `labelled` reading 300 mm AND `freeEnd` *""Floor Top" is held by nothing ... while "Trap Top" is held by the dimension labelled "Rim Height" and an EQ. A flex of "Trap Depth" may move "Floor Top" ... Pin it (SET_PIN_STATE, pinned=true) or dimension it to a level ..."*; `pinnedEnd` empty; Floor Top shows NO pin in Properties. **Then** `SET_FAMILY_TYPE_VALUES` Trap Depth=400: Floor Top moves to Z 100 (the fault, as warned) - look in the Front elevation, then Undo |
| **IT7** | IT6 again with `pinFreeEnd=true` | `pinnedEnd` *""Floor Top" pinned first, as asked (pinFreeEnd), and reads back pinned ..."*; Floor Top shows the pin; `SET_FAMILY_TYPE_VALUES` Trap Depth=400 now moves TRAP TOP to Z 400 and Floor Top stays at Z 0 |
| **IT8** | *Both free.* Two NEW planes "A" at Z 0 and "B" at Z 200, nothing on either, "Gap" = 200; label A to B with `pinFreeEnd=true` | `freeEnd` *"Both ends are held by nothing ... which one a flex of "Gap" moves is Revit's choice ... pins "A", the first named."*; `pinnedEnd` names "A"; A pinned, B not |
| **IT9** | *Same place.* "A" and "C" both at Z 0, "Gap" = 0; label A to C | Refused: *"... are in the same place - there is no distance to label. Pin the one that must stay (SET_PIN_STATE, pinned=true), move the other ..., set "Gap" to that distance (SET_FAMILY_TYPE_VALUES), then label ..."*; nothing made, nothing pinned |

**Then sign** `read-element-parameters` version 5 on IT1 with IT0's negative, after IT2 to IT4 have passed, and
`label-family-dimension` version 2 on IT6 and IT7 with IT5's negative, after IT8 and IT9 have passed. A row that
fails reopens its row; nothing in a row changes to make it pass. IT4 is a statement about the old add-in, not
a signature: it needs no row of its own if every Revit on the PC already runs the new one.

**Route 2 needs `Q-PE-12` as well as `Q-PE-16`.** Route 1 needs only the door.
