# NEEDS-CHECKING archive — Group CM

> **Checks that were done, moved out of the live register.** These are rows of Group CM of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a new family or project from a template, saved where the caller says and opened in a window (2026-10-06)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row CM1

*Moved from the register on 2026-11-17.*

**Run.** `fragment --in "Project2" create-family-document --write --set "templatePath=Metric Generic Model" --set "savePath=<scratch>\HeronNewFam_A.rfa"` - the document in front was another family

**Look for.** **RAN 2026-10-06:** `templateUsed` = `C:\ProgramData\Autodesk\RVT 2024\Family Templates\English\Metric Generic Model.rft`, saved (446,464 bytes), `title` HeronNewFam_A; afterwards the active title was **HeronNewFam_A** and open families went **8 → 9** - one, so the windowless copy was closed. Repeated as HeronNewFam_D and _F with the same result - **Struck 2026-10-18:** run as recorded, 2026-10-06.

---

### Row CM2

*Moved from the register on 2026-11-17.*

**Run.** the same with `savePath` = HeronNewFam_A.rfa again

**Look for.** **RAN 2026-10-06:** refused - *"... already exists. Nothing was created and the file was not touched"*; open families stayed at 14 and the file's time stamp stayed 21:25 - **Struck 2026-10-18:** run as recorded, 2026-10-06.

---

### Row CM3

*Moved from the register on 2026-11-17.*

**Run.** aimed at the document in front (`--in` its own title)

**Look for.** **MEASURED 2026-10-06, three routes.** Heron's transaction open there: *"The active document is currently modifiable"* (HeronNewFam_B saved, not opened). Group only, D-112's route: the window opened AND Revit threw *"An internal error has occurred"* (HeronNewFam_C) - not built on. Deferred to Revit's Idling event: it never fired in two runs (HeronNewFam_E, _G) - not built on. **So version 2 saves it and returns `openNext`**: run on HeronNewFam_H, saved, active title unchanged, `openNext` the path - **Struck 2026-10-18:** measured and answered, as recorded - version 2 returns `openNext`, and CM4 is the second call.

---

### Row CM4

*Moved from the register on 2026-11-17.*

**Run.** `fragment activate-document --set "documentPath=<scratch>\HeronNewFam_G.rfa"` after CM3

**Look for.** **RAN 2026-10-06:** `activeAfter` HeronNewFam_G, active title HeronNewFam_G, families 13 → 14. The two-call route for CM3 works - **Struck 2026-10-18:** run as recorded, 2026-10-06.

---
