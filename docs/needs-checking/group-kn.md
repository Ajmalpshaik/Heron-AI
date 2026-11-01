# Needs checking — Group KN

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KN - the active workset: `SET_ACTIVE_WORKSET` (2026-11-01)

Asked for in the modeller's words: *"make Mechanical the active workset"*, *"set the active workset to
Shared Levels and Grids"*, *"switch to the Architecture workset"* - Revit's Collaborate > Active Workset,
and the Active Workset box on the status bar. Nothing in Heron left it set: `COPY_ELEMENTS` sets it for
one copy and puts it back. `brain/heron_retrieve.py`, Revit 2024, sent the three sentences to
`SET_VIEW_WORKSET_VISIBILITY`, `SET_ELEMENT_WORKSET` and `SET_VIEW_WORKSET_VISIBILITY`.

| What | Where | What it does |
|---|---|---|
| `SET_ACTIVE_WORKSET` (MODIFY) | [`brain/fragments/set-active-workset`](../../brain/fragments/set-active-workset/fragment.yaml) | Makes one user workset, **named exactly** (case and spaces included), the active workset. Reads the active workset **before and after** (`activeBefore`, `activeAfter`, read back from the workset table). A model that is **not workshared** is refused in Revit words and never made workshared. A name matching no user workset changes nothing and **lists every user workset**; one differing only in case or spacing is offered as *"did you mean"* and still refused. A **closed** workset is refused - open it first. A workset somebody else owns is still made active, as Revit's own box allows, and the owner is named (Article 12). Already active: `alreadyActive`, nothing set twice. One that does not read back as asked is set back and named |

**Risk and confirmation, as the nearest workset writes.** MODIFY, like `SET_ELEMENT_WORKSET` and
`RENAME_WORKSET`; Constitution Article 7's explicit confirmation is the workset's name typed exactly - no
find picks the target - and `revit_change`'s Keep or Discard ([D-126](../DECISIONS.md)) is the preview.
Not ADMIN like `CREATE_WORKSET`: it adds no project structure and moves no element; it changes which
workset this modeller's next new elements go on.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of
all eight with `tools/api-surface --members`: `Document.IsWorkshared`, `Document.GetWorksetTable`,
`WorksetTable.GetActiveWorksetId`, `SetActiveWorksetId` and `GetWorkset`, `FilteredWorksetCollector.OfKind`
and `ToWorksets`, `WorksetKind.UserWorkset`, `Workset.Name`, `Id`, `IsOpen` and `Owner`,
`WorksetId.InvalidWorksetId` and `WorksetId`'s `==` are present and unchanged 2020-2027. **Nothing here
has run in Revit.** The card is DRAFT.

**The transaction, and the question only a real Revit answers.** Revit's API documentation for
`SetActiveWorksetId`, the same in all eight releases (`RevitAPI.xml` in the Nice3point reference packages,
read 2026-11-01), lists `ArgumentException` and `ArgumentNullException` and **not**
`ModificationOutsideTransactionException`, which `RenameWorkset` and `Workset.Create` both list. So the call
needs no transaction and nothing says it refuses one; the snippet runs inside Heron's, as `COPY_ELEMENTS`'
own call does, and declares no `noTransactionOpen` ([D-125](../DECISIONS.md)). **Not known:** whether a
rolled-back run, a Discard or Revit's Undo puts the active workset back. If it does not, Constitution Article
8's one-step reversal does not hold for this card, and `activeBefore` is the only way back - KN4 measures it.

**Routing.** Measured through `heron_retrieve.find` on two scratch stores, before and after the card: every
declared utterance and routing-table sentence in the library reaches the card it reached before. **Questions
nobody declares** - *"which workset is active"*, *"what is the active workset"* - reached `SELECT_BY_WORKSET`
(a read) before and reach this card after; with the host's `question=true` they reach `SELECT_BY_WORKSET`,
never this card - and no card reports the active workset:
[row 5b-1366](../fragment-issues/section-5b-rows-401-425.md), OPEN, for the owner.

**The model - a NEW scratch project in Revit 2024**, never a working model. Every run below is rolled back
except KN1's arrangement and KN6's one run. The job file is
[`tools/jobs/set-active-workset-kn-2026-11-01.yaml`](../../tools/jobs/set-active-workset-kn-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KN0** | *Open.* Revit 2024, Changes on at the ribbon | Nothing else open that Heron could pin |
| **KN1** | *Arrange, and KEEP it.* A new project from the metric architectural template; Collaborate > Worksets, OK (worksharing on); add user worksets **Mechanical** and **Architecture**; save as a central in `%TEMP%\heron-work\active-workset-kn`, close, and open a LOCAL of it. Choose **Workset1** in the status bar's Active Workset box. Separately, a second new project, never workshared, saved | Collaborate > Worksets lists Shared Levels and Grids, Workset1, Mechanical and Architecture; the status bar reads Workset1 |
| **KN2** | *Read first.* `LIST_WORKSETS` on the local | The four worksets, all open; the status bar still Workset1 |
| **KN3** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: `mechanical` - `changed` false, `refused` *No user workset is called "mechanical" exactly - did you mean "Mechanical"?* with the four listed, `activeAfter` *"Workset1"*. Positive: `changed` true, `activeBefore` *"Workset1"*, `activeAfter` *"Mechanical"*, `refused` empty |
| **KN4** | *Does a rollback put it back?* Straight after KN3's rolled-back positive, read the status bar's Active Workset box | **Record** whether it reads Workset1 (the rollback put it back) or Mechanical (it did not). If Mechanical, choose Workset1 again by hand before going on, and the card's reversibility note stands as written |
| **KN5** | *The refusals.* `validate set-active-workset --write` on the local: `worksetName` *Plumbing*; then close **Architecture** (Collaborate > Worksets, Close) and ask for *Architecture*; then an empty name; then on the never-workshared project, *Mechanical* | Plumbing refused, the four listed, no *did you mean*; Architecture refused *is closed in this session ... Open it first*, and still closed after; empty refused *No workset was named*; the plain project refused *This model is not workshared*, and still not workshared after. The status bar unchanged after every one |
| **KN6** | *Kept, through `try-once`, on the modeller's yes.* KN3's positive on the saved local, Workset1 active | Keep or Discard. Kept: the status bar reads Mechanical - the second route - and the same run again returns `alreadyActive` true. **Discarded: record what the status bar reads** - that is KN4's question again, through the Keep or Discard box |
| **KN7** | *Revit 2020.* KN1 and KN3 again | The same answers. Nothing the card calls changed between releases |
