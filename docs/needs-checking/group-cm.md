# Needs checking — Group CM

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CM - a new family or project from a template, saved where the caller says and opened in a window (2026-10-06)

`CREATE_FAMILY_DOCUMENT` version 2 saves the new family to the `savePath` the caller names (never over an
existing file), closes the windowless copy the API makes, and opens the saved file in a Revit window.
`CREATE_PROJECT_DOCUMENT` (new, FRG-DOC-039) does the same from a project template; its body is the
family one word for word, held level by `tests/test_new_document_twins.py`. Version 1 left the family in
memory - seen by no window and savable by nothing, because `SAVE_DOCUMENT` never invents a path - so the
parts of Rejin's pipe support were each started by hand ([row 5b-344](../fragment-issues/section-5b-rows-326-350.md)).
`CREATE_FAMILY_DOCUMENT` version 2 is **PROVEN - signed by Ajmal PS on 2026-10-06** on the CM5 record, on his word. `CREATE_PROJECT_DOCUMENT` is **DRAFT**: it has never run on a model.

**Run on 2026-10-06 on Revit 2024 (session 51820)** through `fragment --write` (rolled back - the group
only ever touched the document named by `--in`, and that document was never changed). Every new family
was saved in this session's scratchpad as `HeronNewFam_<letter>.rfa`; no `GM_PipeSupport_*` or `FamilyN`
document was written to. `REPORT_OPEN_DOCUMENTS` was read before and after every run.

| # | Run | Look for |
|---|---|---|
| **CM1** | `fragment --in "Project2" create-family-document --write --set "templatePath=Metric Generic Model" --set "savePath=<scratch>\HeronNewFam_A.rfa"` - the document in front was another family | **RAN 2026-10-06:** `templateUsed` = `C:\ProgramData\Autodesk\RVT 2024\Family Templates\English\Metric Generic Model.rft`, saved (446,464 bytes), `title` HeronNewFam_A; afterwards the active title was **HeronNewFam_A** and open families went **8 → 9** - one, so the windowless copy was closed. Repeated as HeronNewFam_D and _F with the same result |
| **CM2** | the same with `savePath` = HeronNewFam_A.rfa again | **RAN 2026-10-06:** refused - *"... already exists. Nothing was created and the file was not touched"*; open families stayed at 14 and the file's time stamp stayed 21:25 |
| **CM3** | aimed at the document in front (`--in` its own title) | **MEASURED 2026-10-06, three routes.** Heron's transaction open there: *"The active document is currently modifiable"* (HeronNewFam_B saved, not opened). Group only, D-112's route: the window opened AND Revit threw *"An internal error has occurred"* (HeronNewFam_C) - not built on. Deferred to Revit's Idling event: it never fired in two runs (HeronNewFam_E, _G) - not built on. **So version 2 saves it and returns `openNext`**: run on HeronNewFam_H, saved, active title unchanged, `openNext` the path |
| **CM4** | `fragment activate-document --set "documentPath=<scratch>\HeronNewFam_G.rfa"` after CM3 | **RAN 2026-10-06:** `activeAfter` HeronNewFam_G, active title HeronNewFam_G, families 13 → 14. The two-call route for CM3 works |
| **CM5** | the record to sign: `validate --in "HeronNewFam_A" --negative-in "HeronNewFam_A" --write --allow-publish create-family-document --set "templatePath=Metric Generic Model" --set "savePath=<scratch>\HeronNewFam_P2.rfa" --negative-set "templatePath=Metric Generic Model" --negative-set "savePath=<scratch>\HeronNewFam_C.rfa"` - HeronNewFam_A and _B reopened first with ACTIVATE_DOCUMENT so the target was a scratch family behind the one in front | **RAN 2026-10-06, SIGNED:** positive saved HeronNewFam_P2 and it became the active window (families 14 to 15); negative refused the existing HeronNewFam_C.rfa, its time stamp still 21:26:04. Fingerprint e4496168a6640651. `heron_validate.py accept create-family-document --by "Ajmal PS"` on the owner's word; second route not run, as for version 1 |
| **CM6** | `CREATE_PROJECT_DOCUMENT` with a project template by name, and with an existing `.rvt` | **NOT RUN.** It compiles for 2020-2027 and its body is CM1's; it has never opened a project |
| **CM7** | CM1 on **Revit 2020** and **2027** | **NOT RUN.** Compiled for all eight releases; `OpenAndActivateDocument(string)`, `NewFamilyDocument`, `NewProjectDocument(string)`, `SaveAs(string, SaveAsOptions)` read off the 2020 and 2027 reference assemblies |
| **CM8** | the two Idling handlers CM3 left registered | Revit holds them until it closes; if one ever fires it reopens HeronNewFam_E or _G, a scratch file. A Revit restart clears them |

**Left open in Revit:** HeronNewFam_A, _B, _P1 and _P2 (after the CM5 run; _C, _D, _F and _G had been closed since) - scratch families, safe to close without saving.
