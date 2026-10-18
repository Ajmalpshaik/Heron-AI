# Needs checking — Group CP

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CP - nested families: a read run that left an element, formula Yes/No values, hidden bodies in a box, and how a work-plane family stands (2026-10-07)

Four things seen on 2026-10-06/07 building the nested `GM_PipeSupport_*` families for Rejin's pipe support
on Revit 2024 (session 51820). The drivers and full call logs are in `Desktop/For Rejin/Pipe Support/build/`
and `build/log/` on the owner's PC. Nothing here is fixed; each row is the run that would settle it.
**Run every row on a SCRATCH family**, never on a `GM_PipeSupport_*` file, and count the family's elements
before and after each run.

| # | Run | Look for |
|---|---|---|
| **CP1** | A scratch Generic Model family with a reference plane and one small Generic Model loaded. **Count the family's elements before and after every run below.** Since 2026-10-17 the command line refuses a MODIFY fragment without `--write` ([row 5b-350](../fragment-issues/section-5b-rows-326-350.md)), so a read run of one is sent ON PURPOSE from a script in the session's scratch folder, through the client's own `needs_for`, `caller_values` and `Bridge.request("run_fragment_read", ...)` with the fragment's source - never by editing the client. **(1) The detector:** `place-nested-family` for it down the read path; then once with `--write`, rolled back. Record whether a level host does the same as a reference-plane host. **(2) The rollback:** a throwaway probe making the same placement with NO transaction open INSIDE a `TransactionGroup`, then rolling the group back - sent as `run_fragment_write` without `apply` and declaring D-112's `editScopeFailures` need, which is how the add-in runs a fragment inside its group with no transaction of its own. **(3) An open group around what reads do:** the same probe route around `EditFamily` (as `report-family-tables-in-project`), `OpenAndActivateDocument` (as `activate-document`), `OpenDocumentFile` and `Close` (as `compare-models`), and `group.Start()` on a model opened read-only. **(4) A project document:** on a scratch project, `place-family-instances` down the read path the same way as (1) | **(1) The no-write run must leave the count unchanged**; on 2026-10-06 it left one instance each time (57 where 56 were expected). Now the reply must be `read_changed_model` - *"THIS READ CHANGED THE MODEL: 1 element appeared"* - whenever the count moved, and never an ordinary answer. A count that did not move with an ordinary answer means the orphan did not form this time: it says nothing about the detector, so say so. Record Revit's words and both counts. **(2) Count back to the first figure:** a rollback removes an element made outside any transaction, and wrapping every read in a group is an option worth a decision against [D-115](../DECISIONS.md)'s *"nothing on the read path opens a transaction"*. **One more:** the wrap would not help, and is dropped. **(3)** each run completes as it does on the read path, or Revit's refusal in its own words - any refusal is a reason the wrap cannot be general. **(4)** count unchanged: the orphan is a family-document behaviour; one more: any placement can leave one, and the detector is the only thing between it and a clean answer |
| **CP2** | A family with a Yes/No `Flag`, a Yes/No `Derived = not(Flag)` and a Yes/No with no formula; `report-family-parameters` with Flag Yes, then No | **Each Yes/No should read Yes or No.** On 2026-10-06 every formula-driven Yes/No read `<blank>` ([row 5b-351](../fragment-issues/section-5b-rows-351-375.md)); record whether the no-formula one did too |
| **CP3** | A family holding two bodies far apart, each `Visible` linked to its own Yes/No, nested in a host with one switched off; `report-bounding-box` on the nested instance | **Record whether the box covers the hidden body.** On 2026-10-07 the C-channel read 200 x 1075 x 1100 mm with its horizontal and upright bodies both counted ([row 5b-352](../fragment-issues/section-5b-rows-351-375.md)) |
| **CP4** | A work-plane-based Generic Model with a body longer in its own Z, nested by `place-nested-family` on a VERTICAL reference plane in the host | **Record which way the body points.** On 2026-10-06 nested work-plane-based parts came in upright - the family's Z along the world's Z - so the pipe support's orientations were built as switchable bodies instead ([43 §9](../43-building-revit-families.md)). Whether that is Revit's rule or the fragment's choice of the plane's in-plane direction (`across` in `place-nested-family`) is not established |

The negative half of the lookup-table question - an ND that sits only in the first column - is **BV15**'s,
in [Group BV](group-bv.md), and is not repeated here.

### The 2026-10-18 run

**On the owner's laptop, 2026-10-18.** Revit 2024.3 session 63712, and for the rows marked 2027 Revit 2027.3
session 22024, both on add-in `0.1.0+48230c8c` - `main`, Debug, deployed 08:45 local by this run with
`tools/deploy-addin.ps1` for 2024, 2026 and 2027. Revit 2020 session 55496 belonged to another chat and ran
`ab77d5fb`; **nothing here ran on Revit 2020.** Models, all scratch copies in `%TEMP%\heron-work\safety-1018\models`:
*Heron Safety 2024* (Autodesk's `rme_basic_sample_project.rvt`, via the move-rails chat's *Rails-X2*; 21,562
elements at open), *Heron Guard B 2024* (a copy of *Rails-S*, the Dynamo sample) and *Heron Safety 2027* (Revit
2027's `Snowdon Towers Sample HVAC.rvt`, links unresolved; 9,813 elements). Changes, Admin and Publish on except
where a row says. **The owner pressed every Keep, Discard, Esc, X and Undo himself;** nothing answered a box for
him. Undo lists were read with UI Automation, read-only. Throwaway probe fragments were made in `brain/fragments/`
for the session and deleted by name at the end - none is committed.

**CP1 not run.** Its first step sends a MODIFY fragment's source down `run_fragment_read` from a script, skipping
the client's refusal on purpose; the same kind of step - a raw `run_fragment_show` for [ES2](group-es.md)(b) -
was refused by the session's auto-mode safety check, so CP1 was not attempted. What was seen of the detector:
every read and show this day said *"Heron counted N element(s) before this read and the same after it"* -
the negative half, many times over - and no read that changes the model was arranged.
