# Needs checking — Group DA

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group DA - many extrusions and revolves in one call, one undo entry (2026-10-11)

On 2026-10-09 and 10 a duty/standby booster pump set of 107 forms was built through Heron on the owner's
laptop (Revit 2024). Each form was its own `CREATE_FAMILY_EXTRUSION` or `CREATE_FAMILY_REVOLUTION` call,
so the build took 11 batches of up to 10 calls, every profile typed out again as tool input (about 112 s
of streaming), about 83 s of per-batch round trips, 11 more turns carrying the new ids forward, and about
130 entries in the undo list where Article 8 wants one per request
([FRAGMENT-ISSUES 5b-378](../fragment-issues/section-5b-rows-376-400.md),
[OPEN-QUESTIONS Q-62](../open-questions/tier-2.md)). On 2026-10-11 the owner said to build what that
session showed Heron needs.

**What was built.** New `CREATE_FAMILY_FORMS` (FRG-GEO-060, DRAFT, MODIFY). One value, `forms`: rows that
each start `kind=extrusion` or `kind=revolve` and carry the single-form tools' own fields under their own
names (`workPlane`, `profile`, `startMm`/`endMm` or `axisLine`/`startDegrees`/`endDegrees`, `solid`), and
optionally `material`, `detailLevels` and `views` as `SET_FAMILY_FORM_MATERIAL` and
`SET_FAMILY_FORM_VISIBILITY` take them - or the full path of a text file in the temporary folder holding
the rows one per line. Every row is checked before anything is built; every form is read back as the
single-form tools read theirs; one row refused or read back wrong keeps none. It hands back `formIds`,
every id in row order with commas between, which the lock, link, cut, material and visibility tools take
as they are. The family method's step 7 names it (`brain/skills/family-creation.yaml`).

**New rather than widened, and that was weighed.** Q-62 named widening `CREATE_FAMILY_EXTRUSION` and
`CREATE_FAMILY_REVOLUTION` first. Neither can take a list without a value every existing call would then
have to type: the add-in lets only a `bool` request value be left out (`HeronBindingNote.AbsentValue`,
`heron_fragment.need_may_be_absent`), so a list need added to either card is required on every call; one
list holding both kinds cannot live on a card named for one of them; and both are PROVEN, so their code
would go stale under their signatures. Neither card's code was touched. The profile reader is the
single-form tools' own, copied line for line, so a row reads exactly as those tools read it.

Compiled for Revit 2020 to 2027 on 2026-10-11 (`tools/check-fragments-compile.py`, every release ok).
**Nothing below has run in Revit.** The job file is
[`family-forms-many-2026-10-11.yaml`](../../tools/jobs/family-forms-many-2026-10-11.yaml). Every run is
on a scratch family made for it, never on a family somebody is building; with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DA1** | the job file: ten rows in the value, five extrusions and five revolves on all three template planes, two voids, one material, one visibility; negative the same with row 7's plane misnamed | positive: `formCount` 10, `formIds` ten ids, `built` ten entries with their extents and the solids' volumes; negative: refused naming row 7, `formCount` 0. Both legs ROLLED BACK - REPORT_FAMILY_FORMS reads the scratch family's form count unchanged afterwards |
| **DA2** | DA1's ten rows in a file under `%TEMP%\heron-work`, one per line with a `#` comment line, `forms` its full path, `--write --apply` | the same ten built and KEPT; `REPORT_FAMILY_FORMS` lists ten forms with the same ids, in the same order; the undo list shows ONE entry for the call |
| **DA3** | a row 0.5 mm deep; a `kind=blend` row; a file outside the temporary folder; a project in front | each refused with nothing built - the row named and the least depth said, a row is an extrusion or a revolve, the temporary folder named, `notAFamily` |
| **DA4** | the second route: the same ten built one call at a time with the single-form tools on a second scratch family | the two families' `REPORT_FAMILY_FORMS` read the same kinds, extents and volumes; ten undo entries there, one here |
| **DA5** | a list the size of the pump set - about 100 rows | the time the call takes inside Revit (the audit log's `ms`), against the 11 batches it replaces |
| **DA6** | DA1 to DA3 on **Revit 2020** and **2027** | the same answers - what Revit DOES has been measured on 2024 only, if at all |
| **DA8** | [5b-430](../fragment-issues/section-5b-rows-401-425.md): on a PC where Revit's temporary folder is a GUID-named sub-folder of the user's (the owner's laptop - `%TEMP%` in a Revit-side read shows `...\Temp\<guid>\`), DA2 with the file under the chat's own `%TEMP%\heron-work`; negative the same rows in `%TEMP%\elsewhere\rows.txt` and in `%TEMP%\heron-workshop\rows.txt` | positive: built on the FIRST call, no copy into Revit's folder; negative: refused naming both folders read, `formCount` 0, nothing read. On the office PC, where the two folders are one, DA2 is the whole check |
| **DA7** | the signature | `python brain/heron_validate.py accept create-family-forms --by "Ajmal PS"` on DA1's record, on the owner's word only |

**Through the chat** (`revit_change`) the new capability needs its row in the shared knowledge store,
which the store is given once this is merged to main and rebuilt from it; until then `heron_resolve`
answers *Nothing provides CREATE_FAMILY_FORMS*.
