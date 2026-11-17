# NEEDS-CHECKING archive — Group DA

> **Checks that were done, moved out of the live register.** These are rows of Group DA of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *many extrusions and revolves in one call, one undo entry (2026-10-11)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row DA1

*Moved from the register on 2026-11-17.*

**Run.** **RUN, BOTH HALVES HELD 2026-10-17 - Revit 2024.3 on the laptop, session 44252, "Heron Forms Scratch" (1,713 elements)**, through `batch-prove` (`PASS  positive: formCount 10`); the negative refused naming row 7 and `"Ref Lvl"`, `formCount 0`; both rolled back, and `report-family-forms` read `formCount 0` afterwards. The draft waits for DA7. the job file: ten rows in the value, five extrusions and five revolves on all three template planes, two voids, one material, one visibility; negative the same with row 7's plane misnamed

**Look for.** positive: `formCount` 10, `formIds` ten ids, `built` ten entries with their extents and the solids' volumes; negative: refused naming row 7, `formCount` 0. Both legs ROLLED BACK - REPORT_FAMILY_FORMS reads the scratch family's form count unchanged afterwards - **Struck 2026-10-18:** passed, and signed with DA7 - `create-family-forms` PROVEN, fingerprint `4bd9ebf8e66973be` in its `fragment.yaml`.

---

### Row DA2

*Moved from the register on 2026-11-17.*

**Run.** **RUN, PASS 2026-10-17 - Revit 2024.3, session 14196.** `try-once` printed *"KEPT  You were asked in Revit and pressed KEEP after seeing the result."*, `formCount 10` and ten ids; `report-family-forms` read `formCount 10`, `scanned 10`, every one of the ten ids on `formList`; Revit's Undo list, read through UI Automation, held ONE entry for the call, `Heron: create-family-forms`. Results below. DA1's ten rows in a file under `%TEMP%\heron-work`, one per line with a `#` comment line, `forms` its full path, `--write --apply` - **refused since D-117 for a DRAFT; kept through `try-once` instead, see *Run it* below**

**Look for.** the same ten built and KEPT; `REPORT_FAMILY_FORMS` lists ten forms with the same ids, in the same order; the undo list shows ONE entry for the call

---

### Row DA3

*Moved from the register on 2026-11-17.*

**Run.** **RUN, PASS 2026-10-17 - Revit 2024.3, session 44252** - each refused as written below, `formCount 0`; the least depth Revit 2024 gave is 0.79 mm. The project in front was the owner's scratch `Project1`, which he offered. a row 0.5 mm deep; a `kind=blend` row; a file outside the temporary folder; a project in front

**Look for.** each refused with nothing built - the row named and the least depth said, a row is an extrusion or a revolve, the temporary folder named, `notAFamily`

---

### Row DA5

*Moved from the register on 2026-11-17.*

**Run.** **RUN, PASS 2026-10-17 - Revit 2024.3, session 44252:** `formCount 109` of the file's 109 rows, nothing refused, rolled back; the audit's `"ms": 3536`. Results below. a list the size of the pump set - about 100 rows

**Look for.** the time the call takes inside Revit (the audit log's `ms`), against the 11 batches it replaces

---

### Row DA6

*Moved from the register on 2026-11-17.*

**Run.** **RUN, PASS 2026-10-17 on Revit 2020.2 (session 33916) and 2027.3 (session 42696)** - DA1's ten `built` entries word for word 2024's, and the same row-7 refusal; DA3's four refusals the same but for each Revit's own temporary folder; DA2 rolled back, `formCount 10`. DA1 to DA3 on **Revit 2020** and **2027**

**Look for.** the same answers - what Revit DOES has been measured on 2024 only, if at all

---

### Row DA7

*Moved from the register on 2026-11-17.*

**Run.** **DONE 2026-10-17 - signed by Ajmal PS and promoted to PROVEN, merged in [pull request #624](https://github.com/Ajmalpshaik/Heron-AI/pull/624)** (`proof: by: Ajmal PS`, `date: '2026-10-17'` in `create-family-forms`' `fragment.yaml`; `combine-family-forms`' re-proof the same). the signature

**Look for.** `python brain/heron_validate.py accept create-family-forms --by "Ajmal PS"` on DA1's record, on the owner's word only

---

### Row DA8

*Moved from the register on 2026-11-17.*

**Run.** **RUN, PASS 2026-10-17 - the laptop, Revit 2024.3, session 44252**, Revit's temporary folder `C:\Users\ajmal\AppData\Local\Temp\6d4c294e-0255-4e75-b39b-c9e7bd4a7520\`: `formCount 10` on the first call from `heron-work`, no `.txt` in the GUID folder afterwards; `elsewhere` and `heron-workshop` refused naming both folders, `formCount 0`. [5b-430](../fragment-issues/section-5b-rows-426-450.md): on a PC where Revit's temporary folder is a GUID-named sub-folder of the user's (the owner's laptop - `%TEMP%` in a Revit-side read shows `...\Temp\<guid>\`), DA2 with the file under the chat's own `%TEMP%\heron-work`; negative the same rows in `%TEMP%\elsewhere\rows.txt` and in `%TEMP%\heron-workshop\rows.txt`

**Look for.** positive: built on the FIRST call, no copy into Revit's folder; negative: refused naming both folders read, `formCount` 0, nothing read. On the office PC, where the two folders are one, DA2 is the whole check

---
