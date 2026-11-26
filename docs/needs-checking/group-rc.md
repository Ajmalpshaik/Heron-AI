# Needs checking — Group RC

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-11-26 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group RC - CREATE_SHEETS version 6: the copy job takes a bad copy back - a copy whose number or name Revit refuses is not kept, and is listed with Revit's words - DRAFT until proved (2026-11-26)

**Tracked as GitHub issue [#1469](https://github.com/Ajmalpshaik/Heron-AI/issues/1469) since 2026-11-26** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** the owner's answer of 2026-11-26 to [row 5b-1935](../fragment-issues/row-5b-1935.md) (issue
[#1156](https://github.com/Ajmalpshaik/Heron-AI/issues/1156)): *"Take the bad copy back"*. [D-193](../decisions/D-193.md)'s
rule 1 - a sheet whose number or name Revit refuses is taken back - held for a table and for typed numbers;
[`CREATE_SHEETS`](../../brain/fragments/create-sheets/fragment.yaml) version 6 takes it to the copy job. Each copy is
made in a SubTransaction of its own by `makeSheet`, the helper typed numbers use (moved out of their loop unchanged),
and a copy whose number or name Revit refuses - or reads back other than asked - is taken back: nothing of it kept,
its source in `notDuplicated`, a line in `notCreated` with Revit's words. Version 5 kept a copy whose number did not
take (in `created` and `refused`) and kept one whose name was refused under Revit's own name, with no word. The card
was **PROVEN at version 5** (signed 2026-11-24, the table route); its C# changed, so it is **DRAFT** at version 6
until this group runs and Ajmal PS signs (D-30). The table route and typed numbers answer as version 5 - their code
now calls the same helper, so RC4 asks one typed case again. The card's `tests/cases.yaml` holds every case.

**What is done and what is not.** Built 2026-11-26 in a Linux cloud container with no Revit; every fragment compiles
on every release it claims, 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-11-26). **Never run in Revit.**
That Revit refuses a `:` in a sheet name and in a sheet number is what [Group OZ](group-oz.md)'s OZ4
and the card's version 4 case expect, not something seen here.

**The model is a scratch copy, never a working model** - the saved copy of Project1, Revit 2020, that
[Group OZ](group-oz.md)'s OZ1 arranges, then Revit 2024. Every run below is ROLLED BACK. **Before
each run select two sheets that have a title block on them** in the Project Browser: `elements` is read from the
Revit selection, never typed. Every command line with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two
Revits are connected, and `< /dev/null`, in PowerShell in the Heron folder.

| ID | Do this | Pass looks like |
|---|---|---|
| **RC0** | *Open.* The scratch copy, Changes on at the ribbon. Count the sheets in the Project Browser and record the two sheets' numbers | The count and the two numbers recorded here |
| **RC1** | *A refused NAME - THE NEGATIVE FIRST.* The two sheets selected; the RC1 line below: `numberSuffix` *-RC*, `namePrefix` *BAD:* | **No copy kept:** `created` empty, `refused` empty, both sources in `notDuplicated`, two lines in `notCreated` *"<number>-RC: Revit refused the name "BAD:...", so the sheet was taken back. Revit said: ..."* with Revit's sentence; the sheet count as RC0 recorded, and no *-RC* sheet in the Project Browser under any name. **This fails on version 5**, which kept both copies under the names Revit gave them and said nothing. If Revit takes the `:`, record its words and run again with `{` |
| **RC2** | *A refused NUMBER.* The same two selected; the RC2 line: `numberSuffix` *-RC:1*, no prefix | **No copy kept:** `created` empty, `refused` EMPTY, both sources in `notDuplicated`, two lines in `notCreated` *"<number>-RC:1: Revit refused the number, so the sheet was taken back. Revit said: ..."* (or *"Revit kept the number as ..."*); the sheet count unchanged. **This fails on version 5**, which kept both copies under numbers Revit picked, in `created` and `refused`. **Record** what Revit said; if it takes the `:`, run again with `{` |
| **RC3** | *The positive.* The same two selected; the RC3 line: `numberSuffix` *-RC*, `namePrefix` *COPY* with its space | `created` two ids; `numberedAs` each source's number + *-RC*; `namedAs` *"COPY "* + each source's name, the space kept; each copy on its source's title block, no views on it; `notCreated`, `notDuplicated`, `numberTaken` and `refused` empty. Rolled back, so the count is RC0's afterwards |
| **RC4** | *Typed numbers through the same helper* - the card's version 4 case: the RC4 line, numbers *RC-201,RC-202*, names *HERON GOOD 1,HERON BAD:NAME*, `titleBlockName` as OZ1 recorded | `created` one id, RC-201; `notCreated` *"RC-202: Revit refused the name "HERON BAD:NAME", so the sheet was taken back. Revit said: ..."*; no RC-202 sheet - the answer version 5 gave, word for word |
| **RC5** | RC1 and RC3 on Revit 2024 | The same answers |

Then sign the card from the run, in the owner's name, on its proof (the
[`fragment-proving`](../../.claude/skills/fragment-proving/SKILL.md) skill) - never by a session.

### The command lines, by row

Each is ONE line in PowerShell; it is broken here only to be read.

```text
RC1: python mcp/client/heron_bridge_client.py fragment create-sheets --write
       --set "numberSuffix=-RC" --set "namePrefix=BAD:"
RC2: python mcp/client/heron_bridge_client.py fragment create-sheets --write
       --set "numberSuffix=-RC:1"
RC3: python mcp/client/heron_bridge_client.py fragment create-sheets --write
       --set "numberSuffix=-RC" --set "namePrefix=COPY "
RC4: python mcp/client/heron_bridge_client.py fragment create-sheets --write
       --set "numbers=RC-201,RC-202" --set "names=HERON GOOD 1,HERON BAD:NAME" --set "titleBlockName=A1 metric"
```
