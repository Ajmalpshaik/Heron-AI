# Needs checking — Group DW

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-13 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DW - a different value on each element, matched by any parameter value: `SET_PARAMETER_VALUES_BY_ID` version 2 takes a room's Number, a sheet's Sheet Number or a Mark as the key (2026-10-13)

[FRAGMENT-ISSUES row 5b-459](../fragment-issues/section-5b-rows-401-425.md). Giving 13 rooms 13 names took 26
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
| **DW1** | The signature, on the owner's word only, from a checkout holding this code (the main checkout once merged): `python brain/heron_validate.py draft set-parameter-values-by-id --from brain/proof-drafts/runs/set-parameter-values-by-id.json`, read it, then `python brain/heron_validate.py accept set-parameter-values-by-id --by "Ajmal PS"`, then `heron-status: PROVEN` | the draft's fingerprint is the card's (`python brain/heron_fragment.py --show FRG-PAR-027`); a later code change makes it stale and the run is made again |
| **DW2** | Through `revit_change` once merged and the Claude app restarted (this chat's Heron ran the main checkout's code, so the runs above went through the bridge client from the worktree): `rows=key=Number\|in=Rooms;1\|Name=...` on a test copy | the same answer as the bridge run - `resultJson` format 2, every `readBack` value the name asked |
| **DW3** | **Sheets**, key `Sheet Number` - a sheet carries two parameters of that name - writing `Sheet Name` on three sheets | each sheet matched once and renamed, read back by `FIND_SHEETS`; a sheet number held by no sheet refused by name |
| **DW4** | **Doors and Windows together**, `in=Doors,Windows`, key `Mark`, with a door and a window both marked `W1` | the `W1` row refused naming both ids; every other row written |
| **DW5** | The Companion's element table Apply (docs/40 section 8.4) on a test copy, after merging | unchanged from version 1: a stale cell stops the whole Apply, an unchanged table writes every edited cell |
| **DW6** | DW2 on **Revit 2024** and **Revit 2027** | the same answers - an element id is 64-bit from 2024, and this card never builds one from a number. Compiled on every declared release; what Revit DOES was measured on 2020 only |
