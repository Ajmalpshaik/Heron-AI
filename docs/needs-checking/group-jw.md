# Needs checking — Group JW

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group JW - a duct or piping system that already exists, edited: `EDIT_MEP_SYSTEM` (2026-11-01)

Asked for in the modeller's words: *"add these diffusers to Supply Air 2"*, *"take this terminal out of
the system"*, *"add these sprinklers to the FP system"*, *"remove this pipe run from Domestic Cold Water
1"*. Nothing in Heron edited a system that exists - `CREATE_MEP_SYSTEM` makes a new one from elements on
none, and `brain/heron_retrieve.py` on each sentence offered placing families, connecting terminals, the
create, `SET_MEP_SYSTEM_TYPE` and a sprinkler report.

| What | Where | What it does |
|---|---|---|
| `EDIT_MEP_SYSTEM` (MODIFY) | [`brain/fragments/edit-mep-system`](../../brain/fragments/edit-mep-system/fragment.yaml) | Add to System and Remove from System on one existing duct or piping system - named as the System Browser shows it, or selected, or (to take an element out) the one system it is on; every change read back off the system |

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). `MEPSystem.Add(ConnectorSet)`,
`MEPSystem.Remove(ICollection<ElementId>)`, `Elements` and `BaseEquipment` were read off the shipped
assemblies of all eight with `tools/api-surface --members`, and their documentation in the reference
packages of 2020 and 2027 read: Remove refuses taking every terminal out, and refuses the element joined
to the base equipment. **Nothing here has run in Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (JW8 and JW9 measure them):
what Revit's Remove from System does with a pipe or duct - the fragment never hands it one (JW8);
whether a terminal taken out of a system while its duct is still joined to it stays out, and what
happens to that duct's system (JW9).

**The model - MEP Night Test in Revit 2020**, the model `REPORT_MEP_PRESSURE_DROP` was proved on
(2026-10-21: 51 systems, among them *Mechanical Supply Air 1*, *2* and *3*). Every run rolled back. The
job file is [`tools/jobs/edit-mep-system-jw-2026-11-01.yaml`](../../tools/jobs/edit-mep-system-jw-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). **Its room is left for JW1 to read** and is marked in the
file; until it is filled in, no room answers to it and the setup should stop there, before the edit
runs - expected, NOT RUN. Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and
`< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **JW0** | *Open.* MEP Night Test open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon | The title bar reads *MEP Night Test* |
| **JW1** | *Read the arrangement.* `select-by-category-name` Air Terminals, then `read-mep-system` on them | Which system each terminal is on, and which are on none. Find a room holding **some but not all** of *Mechanical Supply Air 2*'s terminals and write its name into the job file. **If the system names differ, change the job file** - never the card |
| **JW2** | *Remove from System, the D-30 positive.* The job file's job | `changed` equal to the terminals in that room; `report` *N element(s) before, N-k after - k taken out* |
| **JW3** | *Its negative - THE FIRST CASE TO RUN.* The same selection, `systemName=Mechanical Supply Air` | `changed` 0, *NOTHING WAS CHANGED*, every supply system holding that word named; none edited |
| **JW4** | *Add to System.* Two supply diffusers JW1 found on **no** system, selected by hand, `validate edit-mep-system --write --set edit=add --set "systemName=Mechanical Supply Air 2"` | `changed` 2, the counts before and after; `read-mep-system` in the same group finds both on it. If JW1 found none on no system, say so here - do not arrange one by editing the model |
| **JW5** | *An add never moves.* Two diffusers on *Mechanical Supply Air 1* selected, `edit=add`, *Mechanical Supply Air 2* | `changed` 0, each named as on Mechanical Supply Air 1 already, to be taken out of it first; both still on 1 |
| **JW6** | *Every terminal out - the Article 7 case.* All of *Mechanical Supply Air 3*'s terminals selected, `edit=remove` | `changed` 0, *NOTHING WAS CHANGED*, the finding saying it would leave the system with nothing on it; the system still there |
| **JW7** | *A piping system.* One sprinkler or plumbing fixture on no piping system, `edit=add`, the name of a piping system of its kind | `changed` 1 - the same card editing a PIPING system. If the model has none on no system, record that and run JW5's shape on a piping system instead |
| **JW8** | *A pipe or a duct.* Two ducts of *Mechanical Supply Air 2* selected, `edit=remove`. Then, **by hand in Revit**, Edit Duct System on it and try Remove from System on one duct | The fragment: `changed` 0, each duct named as on its system through what it connects to. By hand: **record what Revit does** - refused, taken out, or the button not offered. That decides whether a later version may hand a run to Revit |
| **JW9** | *Taken out while still joined.* After JW2, in the same rolled-back group, `read-mep-system` on the duct feeding a terminal taken out | **Record** whether the terminal stays off the system, and the duct's system name. A terminal Revit puts back on its own makes `changed` count it as taken out when it is not - the read-back is immediate |
| **JW10** | *The base equipment.* The system's base equipment selected, `edit=remove`, the system named | `changed` 0, named as the base equipment and left |
| **JW11** | *Kept, through `try-once`, on the modeller's yes.* Remove, as JW2, on a saved copy | Keep: the System Browser lists the system without those terminals; then `read-mep-system` on them finds no system - the second route |
| **JW12** | *Routing, on the owner's PC.* The routing paragraph below, measured again on `model+rerank` | The four sentences reach this card by identity, and *"make a supply air system from these"* reaches `CREATE_MEP_SYSTEM`. Any sentence the re-ranker moves to this card from another is a row of its own |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept edit-mep-system --by
"Ajmal PS"` once its positive and negative pass, and `heron-status` set to PROVEN in the same change. A
row that fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**Routing, measured 2026-11-01 in a cloud container on its `model` backend, re-ranker absent** - the
owner's PC ranks with `model+rerank`, which is **NOT RUN** here. The four sentences above reach this card
by identity. *"add this diffuser to the existing supply system"* and *"remove these air terminals from
the system"* reach it ranked; on the same tree without it they reached `CREATE_MEP_SYSTEM` and
`CONNECT_AIR_TERMINALS`. *"take these sprinklers off the system"* still reaches `REPORT_SPRINKLER_NETWORK`,
as before. *"make a supply air system from these"*, which `CREATE_MEP_SYSTEM`'s routing table claims,
reached this card when it was written - and a view-filter card before that - so it is declared on
`CREATE_MEP_SYSTEM` now and reaches it by identity. The card's first identity and purpose also took
*"add a sprinkler"*, *"add an air terminal"* and *"change SUP to SUPPLY"*, and put it first for *"put
them on the screen"*; they were reworded, and all four reach the cards they reached before
([row 5b-1359](../fragment-issues/section-5b-rows-401-425.md)). `CREATE_MEP_SYSTEM` and
`SET_MEP_SYSTEM_TYPE` are PROVEN; the edit is outside `impl/`, so no fingerprint moved.
