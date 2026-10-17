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
| **DA1** | **RUN, BOTH HALVES HELD 2026-10-17 - Revit 2024.3 on the laptop, session 44252, "Heron Forms Scratch" (1,713 elements)**, through `batch-prove` (`PASS  positive: formCount 10`); the negative refused naming row 7 and `"Ref Lvl"`, `formCount 0`; both rolled back, and `report-family-forms` read `formCount 0` afterwards. The draft waits for DA7. the job file: ten rows in the value, five extrusions and five revolves on all three template planes, two voids, one material, one visibility; negative the same with row 7's plane misnamed | positive: `formCount` 10, `formIds` ten ids, `built` ten entries with their extents and the solids' volumes; negative: refused naming row 7, `formCount` 0. Both legs ROLLED BACK - REPORT_FAMILY_FORMS reads the scratch family's form count unchanged afterwards |
| ~~**DA2**~~ | **RUN, PASS 2026-10-17 - Revit 2024.3, session 14196.** `try-once` printed *"KEPT  You were asked in Revit and pressed KEEP after seeing the result."*, `formCount 10` and ten ids; `report-family-forms` read `formCount 10`, `scanned 10`, every one of the ten ids on `formList`; Revit's Undo list, read through UI Automation, held ONE entry for the call, `Heron: create-family-forms`. Results below. DA1's ten rows in a file under `%TEMP%\heron-work`, one per line with a `#` comment line, `forms` its full path, `--write --apply` - **refused since D-117 for a DRAFT; kept through `try-once` instead, see *Run it* below** | the same ten built and KEPT; `REPORT_FAMILY_FORMS` lists ten forms with the same ids, in the same order; the undo list shows ONE entry for the call |
| ~~**DA3**~~ | **RUN, PASS 2026-10-17 - Revit 2024.3, session 44252** - each refused as written below, `formCount 0`; the least depth Revit 2024 gave is 0.79 mm. The project in front was the owner's scratch `Project1`, which he offered. a row 0.5 mm deep; a `kind=blend` row; a file outside the temporary folder; a project in front | each refused with nothing built - the row named and the least depth said, a row is an extrusion or a revolve, the temporary folder named, `notAFamily` |
| **DA4** | **RUN, THE ROLLED-BACK HALF PASS 2026-10-17 - Revit 2024.3, session 44252, "Heron Forms Scratch 2"** - all ten calls' `built` read the same extents and solid volumes as DA1's ten entries (call 9: 0.524 L, where the list wrote 523,598.7 mm³). The kept half is still owed. the second route: the same ten built one call at a time with the single-form tools on a second scratch family - **rolled back for now, see *Run it* below** | the two families' `REPORT_FAMILY_FORMS` read the same kinds, extents and volumes; ten undo entries there, one here |
| ~~**DA5**~~ | **RUN, PASS 2026-10-17 - Revit 2024.3, session 44252:** `formCount 109` of the file's 109 rows, nothing refused, rolled back; the audit's `"ms": 3536`. Results below. a list the size of the pump set - about 100 rows | the time the call takes inside Revit (the audit log's `ms`), against the 11 batches it replaces |
| ~~**DA6**~~ | **RUN, PASS 2026-10-17 on Revit 2020.2 (session 33916) and 2027.3 (session 42696)** - DA1's ten `built` entries word for word 2024's, and the same row-7 refusal; DA3's four refusals the same but for each Revit's own temporary folder; DA2 rolled back, `formCount 10`. DA1 to DA3 on **Revit 2020** and **2027** | the same answers - what Revit DOES has been measured on 2024 only, if at all |
| ~~**DA8**~~ | **RUN, PASS 2026-10-17 - the laptop, Revit 2024.3, session 44252**, Revit's temporary folder `C:\Users\ajmal\AppData\Local\Temp\6d4c294e-0255-4e75-b39b-c9e7bd4a7520\`: `formCount 10` on the first call from `heron-work`, no `.txt` in the GUID folder afterwards; `elsewhere` and `heron-workshop` refused naming both folders, `formCount 0`. [5b-430](../fragment-issues/section-5b-rows-401-425.md): on a PC where Revit's temporary folder is a GUID-named sub-folder of the user's (the owner's laptop - `%TEMP%` in a Revit-side read shows `...\Temp\<guid>\`), DA2 with the file under the chat's own `%TEMP%\heron-work`; negative the same rows in `%TEMP%\elsewhere\rows.txt` and in `%TEMP%\heron-workshop\rows.txt` | positive: built on the FIRST call, no copy into Revit's folder; negative: refused naming both folders read, `formCount` 0, nothing read. On the office PC, where the two folders are one, DA2 is the whole check |
| **DA7** | **NOT RUN - the owner's signature on DA1's draft, his to give.** the signature | `python brain/heron_validate.py accept create-family-forms --by "Ajmal PS"` on DA1's record, on the owner's word only |

### Run it - inputs prepared 2026-10-17 in a cloud session, NONE OF IT RUN

Every row but DA7 has its input committed under `tools/jobs/` and its exact command below. Nothing here
has met Revit and no row above is marked run. **Before carrying them to the PC,**
`python tests/test_family_forms_rows.py` reads every rows file, DA1's two values and DA4's ten calls with
the fragment's own reader - its source up to the build half, the template's three datums standing in for
the family - and prints the words of each refusal. It says nothing about what Revit builds.

**Three things changed under the table since it was written, and the commands follow them:**

- **DA2's `--write --apply` is refused now.** Since [D-117](../decisions/D-117.md) a change is kept only
  from a PROVEN fragment, and `create-family-forms` is DRAFT. A DRAFT's change is kept one way:
  `try-once` ([D-119](../decisions/D-119.md)) - the run, then Revit's Keep or Discard box, once per
  version kept.
- **DA4 cannot keep the five extrusions.** `create-family-extrusion` is DRAFT again - its signature went
  STALE when its code moved (`python tools/check-signatures.py`) - so D-117 refuses `--apply` on it. DA4
  runs all ten rolled back and compares each call's own `built`.
- **REPORT_FAMILY_FORMS does not list in row order.** Its `formList` is sorted by kind, then id
  ([`report-family-forms`](../../brain/fragments/report-family-forms/impl/any/fragment.cs)), so DA2
  checks every id of `formIds` is on it, not the order.

**Commands, not job files.** A batch-prove job deletes and rewrites
`brain/proof-drafts/runs/create-family-forms.json` - DA1's record, the one DA7 signs. Only DA1 goes
through `batch-prove`.

**Order on the PC:** DA1, DA3, DA8 (laptop only), DA5 - all rolled back, on the EMPTY
"Heron Forms Scratch" - then DA2 LAST, because it keeps ten forms in it. DA4 on its own family; DA6 in
the 2020 and 2027 Revits. Run in Git Bash from the Heron folder, Changes on, the scratch family open and
in front. Every file goes under `C:/Users/ajmal/AppData/Local/Temp/heron-work/da/` - the chat's
`%TEMP%\heron-work` on both PCs ([5b-430](../fragment-issues/section-5b-rows-401-425.md)).

#### DA2 - the ten rows from a file, KEPT, one undo entry

Save the scratch family first (Ctrl+S): `try-once` refuses a model with changes not saved
([D-121](../decisions/D-121.md)). The Keep is the owner's to press ([D-122](../decisions/D-122.md)).

```bash
mkdir -p C:/Users/ajmal/AppData/Local/Temp/heron-work/da
cp tools/jobs/family-forms-ten-2026-10-17.rows.txt C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-ten.txt
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py try-once --in "Heron Forms Scratch" create-family-forms --set "forms=C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-ten.txt" < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --in "Heron Forms Scratch" report-family-forms < /dev/null
```

Look for: Revit shows the ten forms and asks Keep or Discard - press **Keep**; the client prints `KEPT`,
`formCount` 10 and ten ids in `formIds`, and writes `brain/proof-drafts/runs/create-family-forms.once.json`.
REPORT_FAMILY_FORMS: `formCount` 10, every `formIds` id on `formList`. Revit's Undo list: ONE entry for
the call. The file's header and the line before row 6 are `#` lines and are not read.

#### DA3 - the four refusals, each rolled back

For the last one, a scratch project: File > New > Project on the metric default template, saved as
**Heron Forms Project Scratch** under `%TEMP%\heron-work`, open and in front.

```bash
mkdir -p C:/Users/ajmal/AppData/Local/Temp/heron-work/da C:/Users/ajmal/heron-da-outside
cp tools/jobs/family-forms-shallow-row-2026-10-17.rows.txt C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-shallow.txt
cp tools/jobs/family-forms-blend-row-2026-10-17.rows.txt C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-blend.txt
cp tools/jobs/family-forms-ten-2026-10-17.rows.txt C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-ten.txt
cp tools/jobs/family-forms-ten-2026-10-17.rows.txt C:/Users/ajmal/heron-da-outside/forms-ten.txt
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch" create-family-forms --set "forms=C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-shallow.txt" < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch" create-family-forms --set "forms=C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-blend.txt" < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch" create-family-forms --set "forms=C:/Users/ajmal/heron-da-outside/forms-ten.txt" < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Project Scratch" create-family-forms --set "forms=C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-ten.txt" < /dev/null
rm -r C:/Users/ajmal/heron-da-outside
```

Every one: `formCount` 0, `formIds` empty, the scratch family's forms unchanged. `refused`, in turn:

1. *"... Row 3: the extrusion starts at -148.5 mm and ends at -148 mm, 0.5 mm deep - less than Revit's
   shortest line, so it cannot be built. Make it at least N mm deep."* - N is that Revit's shortest curve
   rounded up. Row 3 the only row named.
2. *"... Row 10: kind is "blend" - a row here is an extrusion or a revolve. A blend, a sweep and a swept
   blend each have a tool of their own."*
3. *"The file C:\Users\ajmal\heron-da-outside\forms-ten.txt is not in the temporary folder, ..."* naming
   Revit's temporary folder - and on the laptop *"..., nor in ...\Temp\heron-work\"* as well. Without
   that "nor in", this PC is not DA8's kind.
4. `notAFamily` true and *"The document in front is a project, not a family open in the Family Editor.
   ..."*

#### DA4 - the same ten, one call each, rolled back, on a second family

A second scratch family the same way: the metric Generic Model template, saved as
**Heron Forms Scratch 2**, open and in front.

```bash
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch 2" create-family-extrusion --set "workPlane=Ref. Level" --set "profile=circle 0,0 150" --set startMm=0 --set endMm=500 --set solid=true < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch 2" create-family-extrusion --set "workPlane=Ref. Level" --set "profile=rect -300,-200 300,200 | circle 0,0 50" --set startMm=-20 --set endMm=0 --set solid=true < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch 2" create-family-extrusion --set "workPlane=Center (Front/Back)" --set "profile=0,0; 600,0; 600,100; 100,100; 100,400; 0,400" --set startMm=-50 --set endMm=50 --set solid=true < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch 2" create-family-extrusion --set "workPlane=Center (Left/Right)" --set "profile=polygon 0,250 60 6" --set startMm=-100 --set endMm=100 --set solid=true < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch 2" create-family-extrusion --set "workPlane=Ref. Level" --set "profile=rect -20,-20 20,20" --set startMm=100 --set endMm=400 --set solid=false < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch 2" create-family-revolution --set "workPlane=Center (Front/Back)" --set "profile=0,600; 150,600; 0,800" --set "axisLine=0,0 0,1000" --set startDegrees=0 --set endDegrees=360 --set solid=true < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch 2" create-family-revolution --set "workPlane=Center (Front/Back)" --set "profile=0,0; 40,0; 40,80; 0,80" --set "axisLine=0,0 0,100" --set startDegrees=0 --set endDegrees=360 --set solid=false < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch 2" create-family-revolution --set "workPlane=Center (Left/Right)" --set "profile=circle 300,100 20" --set "axisLine=0,0 0,1000" --set startDegrees=0 --set endDegrees=360 --set solid=true < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch 2" create-family-revolution --set "workPlane=Center (Front/Back)" --set "profile=0,900; arc 50,950 0,1000" --set "axisLine=0,0 0,1000" --set startDegrees=0 --set endDegrees=360 --set solid=true < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch 2" create-family-revolution --set "workPlane=Center (Front/Back)" --set "profile=200,0; 260,0; 260,40; 200,40" --set "axisLine=0,0 0,1000" --set startDegrees=0 --set endDegrees=180 --set solid=true < /dev/null
```

Look for: call N's `built` reads the same X, Y and Z extents, and a solid the same volume, as entry N of
DA1's positive `built` (in `brain/proof-drafts/runs/create-family-forms.json`, `||` between entries).
`CREATE_FAMILY_FORMS` writes a volume under one litre in mm³ and the single-form tools always in litres -
compare the number, not the words. Rows 1 and 3's material and visibility are not single-form values,
so compare their shape only. **Owed, and not runnable today:** the same ten kept, REPORT_FAMILY_FORMS on
both families and the ten undo entries - once `create-family-extrusion` is PROVEN again.

#### DA5 - about 100 rows, timed

```bash
mkdir -p C:/Users/ajmal/AppData/Local/Temp/heron-work/da
cp tools/jobs/family-forms-pump-set-2026-10-17.rows.txt C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-pump-set.txt
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch" create-family-forms --set "forms=C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-pump-set.txt" < /dev/null
grep '"fragment": "create-family-forms"' C:/Users/ajmal/AppData/Roaming/Heron/audit/audit-$(date -u +%Y%m).jsonl | tail -1
```

Look for: `formCount` equal to the file's row count (the test prints it), nothing refused, rolled back;
the audit line's `"ms"` is Revit's time for the whole list. Set it against
[5b-378](../fragment-issues/section-5b-rows-376-400.md)'s pump set. A `TIMEOUT` says nothing about the
fragment (`revit.operationTimeoutSeconds` in `heron.config` sets the limit) - record it and stop. A refusal is a finding.

#### DA6 - DA1 to DA3 again on Revit 2020 and on 2027

In each release: the scratch family and the scratch project made again from THAT release's templates,
under the same titles. Pin the Revit: `<pid>` is its process id from
`python mcp/client/heron_bridge_client.py`, which lists every connected Revit. DA1 goes through
`validate` with its own `--out`, so DA1's 2024 record is never replaced; DA2 runs rolled back, because
its one Keep was spent on 2024 (`try-once` refuses a version already kept). DA3's four lines are the
ones above with `--session <pid>` after `--write`.

```bash
mkdir -p C:/Users/ajmal/AppData/Local/Temp/heron-work/da
cp tools/jobs/family-forms-ten-2026-10-17.rows.txt C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-ten.txt
cp tools/jobs/family-forms-row7-plane-2026-10-17.rows.txt C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-row7.txt
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py validate --session <pid> --in "Heron Forms Scratch" --out C:/Users/ajmal/AppData/Local/Temp/heron-work/da/da6-<release>.json --write --set "forms=C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-ten.txt" --negative-set "forms=C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-row7.txt" create-family-forms < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --session <pid> --in "Heron Forms Scratch" create-family-forms --set "forms=C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-ten.txt" < /dev/null
```

Look for: DA1's and DA3's answers on 2024, word for word except the numbers Revit measures; DA2's
`formCount` 10. A difference is a finding for [FRAGMENT-ISSUES](../FRAGMENT-ISSUES.md).

#### DA8 - the laptop only: Revit's temporary folder a GUID-named sub-folder

```bash
mkdir -p C:/Users/ajmal/AppData/Local/Temp/heron-work/da C:/Users/ajmal/AppData/Local/Temp/elsewhere C:/Users/ajmal/AppData/Local/Temp/heron-workshop
cp tools/jobs/family-forms-ten-2026-10-17.rows.txt C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-ten.txt
cp tools/jobs/family-forms-ten-2026-10-17.rows.txt C:/Users/ajmal/AppData/Local/Temp/elsewhere/rows.txt
cp tools/jobs/family-forms-ten-2026-10-17.rows.txt C:/Users/ajmal/AppData/Local/Temp/heron-workshop/rows.txt
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch" create-family-forms --set "forms=C:/Users/ajmal/AppData/Local/Temp/heron-work/da/forms-ten.txt" < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch" create-family-forms --set "forms=C:/Users/ajmal/AppData/Local/Temp/elsewhere/rows.txt" < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --write --in "Heron Forms Scratch" create-family-forms --set "forms=C:/Users/ajmal/AppData/Local/Temp/heron-workshop/rows.txt" < /dev/null
rm -r C:/Users/ajmal/AppData/Local/Temp/elsewhere C:/Users/ajmal/AppData/Local/Temp/heron-workshop
```

Look for: the first `formCount` 10 on the FIRST call, rolled back, with nothing copied into
`...\Temp\<guid>\`; the other two `formCount` 0 and *"The file ... is not in the temporary folder,
C:\Users\ajmal\AppData\Local\Temp\<guid>\, nor in C:\Users\ajmal\AppData\Local\Temp\heron-work\."* On the
office PC both of those paths are inside Revit's own temporary folder and are READ - they are not
negatives there.

#### The fragment warm-up, ON against OFF

`fragments.warmUp` is read at start-up by
[`HeronApplication.cs`](../../revit/Heron.Revit.Addin/HeronApplication.cs), and `WarmUp`/`WarmUpBody` in
[`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs) compile a throwaway script on a
background thread. What it should save is the FIRST fragment's compile after Revit starts. The switch is a
line in `%APPDATA%\Heron\config\heron.config` - `C:/Users/ajmal/AppData/Roaming/Heron/config/heron.config`,
`key=value`, `#` for comments; a missing line means ON. **Revit is closed and started again between the two
halves**, and nothing else is run before the first fragment.

1. Close Revit. Put `fragments.warmUp=false` in `heron.config` and save it.
2. Start Revit, open "Heron Forms Scratch", press Heron to connect, and wait one minute.
3. Run the same read twice, then read its two times and the log:

```bash
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --in "Heron Forms Scratch" report-family-forms < /dev/null
HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --in "Heron Forms Scratch" report-family-forms < /dev/null
grep '"fragment": "report-family-forms"' C:/Users/ajmal/AppData/Roaming/Heron/audit/audit-$(date -u +%Y%m).jsonl | tail -2
grep -E "Heron loaded|Fragment warm-up" C:/Users/ajmal/AppData/Local/Heron/logs/addin-$(date -u +%Y%m%d).log | tail -3
```

4. Close Revit. Change the line to `fragments.warmUp=true` and save it. Start Revit, the same model,
   connect, wait the same minute, and run step 3 again.

Read: OFF, the log has no `Fragment warm-up` line after the last `Heron loaded.`; ON, it has
*"Fragment warm-up compiled and emitted a throwaway script in N ms."* (any other `Fragment warm-up` line
means it failed and the ON half measures nothing). In each half the first audit `"ms"` is the cold run and
the second the same fragment already compiled; the warm-up saves the difference between the two halves'
first `ms`. Record all four, with the release and the PC.

**Through the chat** (`revit_change`) the new capability needs its row in the shared knowledge store,
which the store is given once this is merged to main and rebuilt from it; until then `heron_resolve`
answers *Nothing provides CREATE_FAMILY_FORMS*.

### Results - 2026-10-17, the owner's laptop: Revit 2024.3, 2020.2 and 2027.3

**Which add-in ran which row.** DA1, DA3, DA4, DA5 and DA8 ran on the add-in deployed that morning
(built from `76006767`). DA2, DA6, the warm-up and the `combine-family-forms` re-proof ran on one built
from main at `4496d040` plus the one-line fix of [pull request #623](https://github.com/Ajmalpshaik/Heron-AI/pull/623), its row 5b-1000,
deployed for 2020, 2024, 2026 and 2027 by `tools/setup.ps1`. Main alone compiled no fragment in Revit
(row 5b-1000), and DA2's Keep needed [row 5b-621](../fragment-issues/section-5b-rows-401-425.md)'s fix,
which the morning's add-in lacked: there a kept `create-family-revolution` came back
`[operation_failed]` *"Corresponding button not found. Parameter name: defaultButton"*, nothing kept.

**The arrangement as run.** Each release's scratch family was made by `create-family-document` (PROVEN)
from that release's own `Metric Generic Model.rft`, saved under `%TEMP%\heron-work\da\r<release>\` and
opened with `activate-document`. A template copied and renamed `.rfa` does not do: Revit 2024 refused it,
*"File Heron Forms Scratch.rfa is not a valid .rfa file. Try changing the file's extension to .rft and
opening it again."* DA3's project in front: on 2024 the owner's scratch `Project1`; on 2020 a copy of
`rst_basic_sample_project.rvt` and on 2027 a copy of `Snowdon Towers Sample HVAC.rvt`, each from that
release's `Samples` and saved as "Heron Forms Project Scratch". DA6's records are
`%TEMP%\heron-work\da\da6-2020.json` and `da6-2027.json`, outside the repository as the command above says.

**DA2 - the Undo list.** Read through UI Automation - the Undo button's history opened, read and
closed, nothing in it clicked: `Shaded`, `Default 3D View`, `Heron: create-family-forms`, newest first.
One entry for the call; the two above it were made after it, while the result was looked at. The family
with the ten kept forms was then saved with `save-document` to
`%TEMP%\heron-work\da\r2024\Heron Forms Scratch.rfa`.

**DA5 against the pump set.** One call built 109 forms in 3,536 ms of Revit's own time (the audit's
`ms`), rolled back. [Row 5b-378](../fragment-issues/section-5b-rows-376-400.md)'s pump set of 107 forms
took 11 batches of up to ten single-form calls, about 112 s of profiles streamed as tool input and about
83 s of round trips. What a chat spends writing 109 rows and reading one answer is not measured here -
that is the build timing below, NOT RUN.

**The warm-up, ON against OFF** - Revit 2024.3 on the laptop, the fixed add-in, "Heron Forms Scratch"
open, `report-family-forms` twice, Revit closed and started again between halves with nothing run before
the first read. **The switch was read from the Claude app's own copy of `heron.config`:** the app keeps a
private copy of `%APPDATA%\Heron`, a Revit started from a chat's shell reads that copy, and one the owner
starts reads the real file ([row 5b-1004](../fragment-issues/section-5b-rows-401-425.md)). Each half's
log line below shows which way it was read.

| Half | Revit | The log after `Heron loaded.` | First read | Second read | First read taken |
|---|---|---|---|---|---|
| OFF | pid 13124 | no `Fragment warm-up` line | 4,329 ms | 4 ms | 63 s after connecting |
| ON | pid 14196 | *"Fragment warm-up compiled and emitted a throwaway script in 4049 ms, on Microsoft.CodeAnalysis.CSharp 4.8.0.0 from ...\Addins\2024\Heron\Microsoft.CodeAnalysis.CSharp.dll."* | 1,214 ms | 2 ms | 62 s after connecting |
| OFF, early | pid 26160 | no `Fragment warm-up` line | 6,679 ms | 2 ms | 16 s after connecting - not the minute asked, so a second OFF sample and not the measurement |

The warm-up took 3,115 ms off the first fragment after Revit starts (4,329 to 1,214 ms); the cached run
is the same either way. [D-56](../decisions/D-56.md)'s baseline was 5,193 ms cold and 3 ms cached, on
`find-views`. The warm-up also compiled on 2020.2 (3,534 ms) and on 2027.3 (2,269 ms, *"in Heron's own
load context"*); neither was timed against OFF.

**`combine-family-forms` re-proved** - its code changed after the 2026-10-08 signature, and a family
build's cuts depend on it ([D-116](../decisions/D-116.md)). Revit 2024.3, session 14196, on DA2's kept
forms and one void kept for it: a `create-family-revolution` (PROVEN) bore 100 mm across from -10 to
510 mm through row 1's cylinder, kept through the Keep box. Both legs rolled back:

- positive, row 1's solid and the bore: `cut` true, *"Cut 1 solid(s) with 1 void(s): 35.341 L before,
  31.414 L after - 3.927 L taken away. Read back form by form: all 1 solid(s) are in the result - 2
  point(s) inside them that no void covers, every one found in it."* - a bore 100 mm across and 500 mm
  through the solid is 3.927 L;
- negative, row 5's and row 7's voids alone: `refused` *"Nothing was joined or cut. Every form named is
  a void. A void cuts a solid - name the solid it should cut as well."*, `cut` false, no combination.

`report-family-forms` afterwards: 11 forms, no combination. Draft fingerprint `37adc81646c3aa9d`, the
signature owed. The first try named row 5's void, which lies wholly inside row 1's cylinder; it threw
- [row 5b-1001](../fragment-issues/section-5b-rows-401-425.md).

**Later the same day: that signature went stale, on purpose.** The owner signed `37adc81646c3aa9d` and
promoted the card; 5b-1001's fix then changed the code - the `holds` check on a void's body threw where
a line ran through one of the void's edges, and now tries a spare direction. The card is DRAFT again.
Rolled back on a copy of the same family, Revit 2024.3, session 51856: row 5's void now cuts row 1's
cylinder (0.48 L taken away), the bore cuts as above, the two voids alone are refused as above. The new
draft is fingerprint `ca69cfbd3677c569`; **the owner's signature and promotion are owed again.** Ajmal PS signed it the same day and set the card to PROVEN himself.

**The build timing - NOT RUN.** A family built by a chat goes through `revit_change`, which runs a
PROVEN capability only ([D-117](../decisions/D-117.md)); `create-family-forms` stays DRAFT until DA7 is
signed and it is promoted, and `try-once`'s one keep for this version was DA2's.
`tools/time-family-build.py` has no build to time until then.

**Still owed:** DA7 and `combine-family-forms`' signature, both the owner's; DA4's kept half; the build
timing; the warm-up OFF against ON on 2020 and 2027.
