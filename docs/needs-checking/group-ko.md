# Needs checking — Group KO

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KO - which workset is active: `REPORT_ACTIVE_WORKSET` (2026-11-01)

Asked for in the modeller's words: *"which workset is active"*, *"what is the active workset"*, *"am I on
the right workset"* - the Active Workset box on Revit's status bar. No card reported it:
[row 5b-1366](../fragment-issues/section-5b-rows-401-425.md) recorded the first sentence reaching
`SET_ACTIVE_WORKSET`, a write, when the host did not flag a question, and `SELECT_BY_WORKSET`, a read
that does not answer it, when it did.

| What | Where | What it does |
|---|---|---|
| `REPORT_ACTIVE_WORKSET` (READ) | [`brain/fragments/report-active-workset`](../../brain/fragments/report-active-workset/fragment.yaml) | Names the **active workset** (`activeWorkset`) with whether it is open, editable and owned by somebody, and lists the model's **user worksets**, each open or closed, editable or not, the active one marked (`userWorksets`). A model that is **not workshared** comes back `workshared` false, `activeWorkset` empty, and the finding **says** it has no worksets - never a bare empty. Opens no transaction |

**A new read, not a widening of `LIST_WORKSETS`.** That card is PROVEN; naming the active workset there would
change its `impl/` and make its proof stale until it is proved again, which waits for Ajmal PS's yes. The
contracts differ - `LIST_WORKSETS` provides `worksetCount` and `closedCount`, this card the active workset's
name and its editable state.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of all
eight with `tools/api-surface --members`: `Document.IsWorkshared`, `Document.GetWorksetTable`,
`WorksetTable.GetActiveWorksetId` and `GetWorkset(WorksetId)`, the `FilteredWorksetCollector(Document)`
constructor, `OfKind` and `ToWorksets`, `WorksetKind.UserWorkset`, `Workset.Name`, `Id`, `Kind`, `IsOpen`,
`IsEditable` and `Owner`, `WorksetId.InvalidWorksetId` and `WorksetId`'s `==` are present and unchanged
2020-2027. **Nothing here has run in Revit.** The card is DRAFT.

**Routing.** Measured through `heron_retrieve.find` on two fresh scratch stores, before and after the card,
`model` backend, re-ranker absent, no Revit filter and Revit 2020, 2024 and 2025: the three sentences reach
this card with and without the host's question flag (they reached `SET_ACTIVE_WORKSET` or `CREATE_WORKSET`
without it, `SELECT_BY_WORKSET` with it). Every declared utterance and routing-table sentence in the library
reaches the card it reached before without the flag; with it, only sentences that writes declare moved,
from one read to another. *"what is my active workset"* and *"is Mechanical the active workset"* - probes,
declared nowhere - still reach `SET_ACTIVE_WORKSET` without the flag, and this card with it.

**The models - Group KN's two scratch projects in Revit 2024**, never a working model: the workshared local
of [Group KN](group-kn.md)'s KN1, and the never-workshared project beside it. Nothing below changes the
model. The job file is
[`tools/jobs/report-active-workset-ko-2026-11-01.yaml`](../../tools/jobs/report-active-workset-ko-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KO1** | *Arrange.* [Group KN](group-kn.md)'s KN1 models: the workshared local (`active-workset-kn`) and the never-workshared project saved as `active-workset-kn-plain`, both open in one Revit 2024. Choose **Mechanical** in the local's status bar Active Workset box | Collaborate > Worksets lists Shared Levels and Grids, Workset1, Mechanical and Architecture; the status bar reads Mechanical |
| **KO2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative, the plain project: `activeWorkset` empty, `workshared` false, findings *This model is not workshared, so it has no worksets and no active workset*. Positive, the local: `activeWorkset` *Mechanical*, `workshared` true, `userWorksets` with Mechanical marked ACTIVE |
| **KO3** | *The second route.* Read the status bar's Active Workset box and Collaborate > Worksets after KO2 | The box still says Mechanical - nothing changed - and every user workset's open and Editable state matches `userWorksets` |
| **KO4** | *Tracking.* Choose **Workset1** in the Active Workset box by hand, run the job file again | Positive: `activeWorkset` *Workset1* and the ACTIVE mark moved - the answer follows the model; the negative as in KO2 |
| **KO5** | *Revit 2020.* KO1 and KO2 again | The same answers. Nothing the card calls changed between releases |
