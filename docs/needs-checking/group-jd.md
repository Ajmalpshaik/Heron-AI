# Needs checking — Group JD

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JD - every entry reaches the chat, and one subcategory's style copied on its own: `READ_ELEMENT_PARAMETERS` version 6, the add-in's `providesEntries`, `TRANSFER_OBJECT_STYLES_BETWEEN_DOCUMENTS` version 2 (2026-10-28)

From rows [5b-1341 and 5b-1342](../fragment-issues/section-5b-rows-401-425.md). Both were met on 2026-10-28
on the owner's laptop, Revit 2020 (session 56456), on a real project, **4355-BHVD-3D-21S10-BL001A**, read only:
the owner asked for the 38 volume control dampers with their Equipment Tag in one table, and it took some
forty reads in groups of twelve or fewer; and the Signature pen-ink colour he set in that project could not
be copied to his four other models without restyling Generic Annotations and all 194 of its other
subcategories in each. Both fixed the same day:

| What | Where | What changed |
|---|---|---|
| The add-in's reply | [`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs) `Report` | `providesEntries` carries EVERY entry of a dictionary, not the first twelve; `Describe` unchanged, so every proof record's "N entry(ies)" is as it was (5b-1341) |
| `READ_ELEMENT_PARAMETERS` v6 | [`read-element-parameters`](../../brain/fragments/read-element-parameters/fragment.yaml) | READ. `findings` names every element read and every blank and absent id (5b-1341) |
| `TRANSFER_OBJECT_STYLES_BETWEEN_DOCUMENTS` v2 | [`transfer-object-styles-between-documents`](../../brain/fragments/transfer-object-styles-between-documents/fragment.yaml) | MODIFY. `subcategoryContains`, optional, absent `all`: given, only subcategories whose name contains it are styled or made, and the category itself is not restyled. A partial `sourceDocumentTitle` matching several open projects refuses by name; an exact title wins (5b-1342) |

**What is done and what is not.** The library compiles for 2020 and 2027 (`tools/check-fragments-compile.py`,
2026-10-28) and the add-in builds for 2020 (`tools/check-compile.py 2020`, all 23 projects).
`tests/test_read_door.py` and `tests/test_run_records.py` pass unchanged: the server already printed every
entry that arrived and says "... and N more" only for an older add-in that sent fewer. **Nothing here has run
in Revit.** `TRANSFER_OBJECT_STYLES_BETWEEN_DOCUMENTS` was PROVEN on Revit 2024 only (2026-09-08, Snowdon
Towers Sample Architectural) and **never on Revit 2020**, the release the owner works in.

**What to restart.** JD0 to JD3 need the add-in **built and deployed for Revit 2020 and Revit restarted**
(`providesEntries` is the add-in's). The fragments' C# is read live, so JD4 to JD9 need nothing restarted.

**The models.** JD0 to JD3: 4355-BHVD-3D-21S10-BL001A as it stands, read only. JD4 to JD9: that project as the
SOURCE and one of the owner's other models of the same job as the TARGET, both open in the same Revit 2020;
every run rolled back (`validate --write`, no `--apply`).

| # | Run | Look for |
|---|---|---|
| **JD0** | *Negative FIRST.* `SELECT_BY_FAMILY familyName=VCD exactMatch=false categories=Duct Accessories includeLinks=false`, then `READ_ELEMENT_PARAMETERS parameterName=ZZZNOTHINGHERE` | `findings` *"ZZZNOTHINGHERE": 0 of 38 carry a value, 0 blank, 38 absent. Absent: <all 38 ids>.* - every id, no "... and N more"; `values 0 entry(ies)` with no line under it |
| **JD1** | *The fault's own case.* The same 38, `parameterName=Equipment Tag` | `findings` names all 38, VCD001 to VCD038; the reply shows `values 38 entry(ies)` and 38 lines under it, and no "... and 26 more" anywhere |
| **JD2** | `parameterName=Mark` on the same 38 | 37 values and `Blank: 7987914` (VCD005 carries no Mark) - every value printed |
| **JD3** | *An older add-in.* On a Revit whose add-in was NOT rebuilt, JD1 again | `findings` still names all 38 (the fragment is live); the chat shows twelve entries and "... and 26 more" - the server says what it was not sent |
| **JD4** | *Negative FIRST for the copy.* `TRANSFER_OBJECT_STYLES_BETWEEN_DOCUMENTS sourceDocumentTitle=4355-BHVD-3D-21S10-BL001A nameContains=Generic Annotations subcategoryContains=ZZZNOTHINGHERE`, in the target, rolled back | `changed` 0, `created` 0, `refused` *"No categories in '...' matching 'Generic Annotations' with a subcategory matching 'ZZZNOTHINGHERE'."*; LIST_CATEGORIES on the target's Generic Annotations reads as before |
| **JD5** | *The fault's own case.* The same with `subcategoryContains=Signature` | `changed` (or `created`) holds ONLY *Generic Annotations : Signature*, its colour moving to 30,80,170; nothing for the Generic Annotations row itself or any other subcategory |
| **JD6** | JD5 kept - on the owner's word only, one model at a time | LIST_CATEGORIES `categories=Generic Annotations > Signature` reads 30,80,170, weight 1, Solid; one Ctrl+Z puts it back |
| **JD7** | `subcategoryContains` LEFT OUT, `nameContains=Generic Annotations`, rolled back | version 1's answer: the category and every subcategory that differs in `changed`, the missing ones in `created` - proves absent means all |
| **JD8** | *Two sources.* `sourceDocumentTitle=4355-BHVD` with two of the job's models open besides the target | `refused` naming both and asking for the whole title; nothing changed |
| **JD9** | `sourceDocumentTitle=4355-BHVD-3D-21S10-BL001A.rvt` | the same as JD5 - ".rvt" is read as the title |
| **JD10** | *A link is no source.* `sourceDocumentTitle=` the exact title of one of the source's LINKED models (4355-BU3D-21S10-BL001A) | `refused` *"No open project matching ..."*, and its "open now" list names no linked model - added 2026-10-28 when the first JD5 run listed seven links among the open projects |

**Then sign** `read-element-parameters` version 6 on JD1 with JD0's negative, after JD2 has passed, and
`transfer-object-styles-between-documents` version 2 on JD5 with JD4's negative, after JD7 to JD9 have passed -
on Revit 2020, so the proof is the release the owner works in. A row that fails reopens its row; nothing in a
row changes to make it pass.

### Results - 2026-10-28, the owner's laptop: Revit 2020, session 33972

The add-in was built from this branch (7bb93bca) and deployed to Revit 2020, 2024, 2026 and 2027 with
`tools/deploy-addin.ps1`, each deployed DLL hash-equal to its build. Revit 2020 restarted with
**4355-BHVD-3D-21S10-BL001A** (source) and **Project1** (target) open. Every write `validate --write`, rolled back.

| # | Came back |
|---|---|
| **JD0** | **PASS.** `values 0 entry(ies)`, `absent 38`; `findings` *"ZZZNOTHINGHERE": 0 of 38 carry a value, 0 blank, 38 absent. Absent: 7986880, 7987841, ...* - all 38 ids, no "... and N more" |
| **JD1** | **PASS** (batch-prove). `values 38 entry(ies)`; `findings` names all 38, VCD001 to VCD038, and says no "more". The run record keeps `provides` only, so what the chat shows was checked separately: through `revit_read` in the chat, the same 38 came back as `values 38 entry(ies)` with **all 38** `<id>: VCDnnn` lines under it, VCD001 to VCD038, and no "... and N more" - the add-in's `providesEntries` at 7bb93bca |
| **JD2** | **PASS.** `values 37 entry(ies)`, `blank 1 [7987914]`; `findings` prints all 37 Marks and *"Blank: 7987914"*. Seven dampers carry fire-damper Marks (MFD-0xx, MFD-12x) - the model's data, recorded for the owner |
| **JD3** | **NOT RUN** - every Revit on the PC now runs the rebuilt add-in; it is a statement about the old one, as IU4 was |
| **JD4** | **PASS.** `changed 0`, `created 0`, `refused` *"No categories in '4355-BHVD-3D-21S10-BL001A' matching 'Generic Annotations' with a subcategory matching 'ZZZNOTHINGHERE'."* |
| **JD5** | **PASS** (batch-prove). `created 1 [Generic Annotations : Signature]`, `changed 1 [Generic Annotations : Signature: colour]` - Project1 had no Signature row; nothing else touched |
| **JD6** | **NOT RUN** - keeping it is the owner's word, one model at a time, and this chat cannot keep a change (its server code changed under it) |
| **JD7** | **PASS.** `subcategoryContains` left out: `changed 185`, `created 193` across Generic Annotations - version 1's whole-category answer, rolled back. It is also what the Signature copy would have done to each of the owner's models before version 2 |
| **JD8** | **NOT RUN** - needs two of the job's models open beside the target; only one was |
| **JD9** | **PASS.** `sourceDocumentTitle=4355-BHVD-3D-21S10-BL001A.rvt` answered as JD5 |
| **JD10** | **PASS, after a fix found by this run.** The FIRST JD5 run listed nine "open" projects, seven of them the source's LINKED models (`4355-BU3D-21S10-BL001A` ...): the purpose says links are not offered, the code never checked. `IsLinked` is skipped now; re-run, `openTitles` is the two real projects, and the link's exact title is refused *"No open project matching '4355-BU3D-21S10-BL001A' ... open now: 4355-BHVD-3D-21S10-BL001A, Project1."* JD5 was run again on the fixed code and is the record kept |
