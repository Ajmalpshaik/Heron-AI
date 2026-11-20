# Needs checking — Group DW

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-13 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DW - a different value on each element, matched by any parameter value: `SET_PARAMETER_VALUES_BY_ID` version 2 takes a room's Number, a sheet's Sheet Number or a Mark as the key (2026-10-13)

**Tracked as GitHub issue [#982](https://github.com/Ajmalpshaik/Heron-AI/issues/982) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

[FRAGMENT-ISSUES row 5b-459](../fragment-issues/section-5b-rows-451-475.md). Giving 13 rooms 13 names took 26
calls, a find and a write per room, because a reply cannot carry element ids. `SET_PARAMETER_VALUES_BY_ID`
(FRG-PAR-027) was widened rather than a card added: a first row `key=<parameter>|in=<categories>` in the `rows`
it already takes, then `<key value>|<parameter>=<value>` per element. No new input. Id rows are version 1's code.

**Measured on Revit 2020 only, 2026-10-13, Project1 (a scrap model, session 33484)**, on 13 test rooms of
3 x 3 m drawn for it on Level 1 at Y 40000..43000 mm: the 13 named in ONE call keyed on Number, read back three
ways; a key matching all 13 (Name `Room`) and a key matching two (Number `12`) refused with nothing written for
that row, read back; a row Revit turned one value down for kept none of its values, read back; and five kinds of
id rows answered identically through main's code and this branch's. The rolled-back proof run is recorded
(fingerprint `10df47f9b61a2f16`) - a copy is in the main checkout's `brain/proof-drafts/runs/` and in
`%TEMP%\heron-work\spv27\keep`. Every fragment compiles on all eight releases.

Never on a working project. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| ~~**DW1**~~ | DONE 2026-10-13 — [full row](../needs-checking-archive/group-dw.md#row-dw1) | the draft's fingerprint is the card's (python brain/heron_fragment.py --show… |
| **DW2** | Through `revit_change` once merged and the Claude app restarted (this chat's Heron ran the main checkout's code, so the runs above went through the bridge client from the worktree): `rows=key=Number\|in=Rooms;1\|Name=...` on a test copy | the same answer as the bridge run - `resultJson` format 2, every `readBack` value the name asked |
| ~~**DW3**~~ | **Sheets**, key `Sheet Number` - a sheet carries two parameters of that name - writing `Sheet Name` on three sheets | each sheet matched once and renamed, read back by `FIND_SHEETS`; a sheet number held by no sheet refused by name **HELD 2026-11-20 on Revit 2020 (build 20191106_1200, add-in 0.2.0.0), `SET_PARAMETER_VALUES_BY_ID` version 9 on *House Scratch 2* (a copy of `rac_basic_sample_project.rvt` in `%TEMP%\heron-work\issues-1120`), for issue [#982](https://github.com/Ajmalpshaik/Heron-AI/issues/982), rolled back:** `rows=key=Sheet Number|in=Sheets;A101|Sheet Name=DW3 Site;A102|...;A103|...;Z999|...` - written 3, each sheet matched once (`resultJson` format 2, readBack A101 -> 959135 *"DW3 Site"*, A102 -> 382617 *"DW3 Plans"*, A103 -> 958184 *"DW3 Sections"*; Heron's change read-back the same three Sheet Names before -> after); `refused` *"Z999: no element in Sheets has Sheet Number 'Z999' - not written"*. Read back by the card and the add-in, not by `FIND_SHEETS` - the run was rolled back. |
| ~~**DW4**~~ | **Doors and Windows together**, `in=Doors,Windows`, key `Mark`, with a door and a window both marked `W1` | the `W1` row refused naming both ids; every other row written **HELD 2026-11-20 on Revit 2020 (build 20191106_1200, add-in 0.2.0.0), `SET_PARAMETER_VALUES_BY_ID` version 9 on *House Scratch 2* (a copy of `rac_basic_sample_project.rvt` in `%TEMP%\heron-work\issues-1120`), for issue [#982](https://github.com/Ajmalpshaik/Heron-AI/issues/982), rolled back,** after door 422466 and window 457479 were both given Mark *W1* (`WRITE_ELEMENT_PARAMETERS`, kept on the scratch copy): `rows=key=Mark|in=Doors,Windows;W1|Comments=DW4 both;104|Comments=DW4 one` - written 1 (readBack *104 -> 425292 Comments "DW4 one"*), refused *"W1: 2 elements in Doors, Windows have Mark 'W1' (ids 422466, 457479) - which one is meant would be a guess, so none is written"*. |
| **DW5** | The Companion's element table Apply (docs/40 section 8.4) on a test copy, after merging | unchanged from version 1: a stale cell stops the whole Apply, an unchanged table writes every edited cell |
| **DW6** | DW2 on **Revit 2024** and **Revit 2027** | the same answers - an element id is 64-bit from 2024, and this card never builds one from a number. Compiled on every declared release; what Revit DOES was measured on 2020 only |
