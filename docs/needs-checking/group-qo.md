# Needs checking — Group QO

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group QO - seven reading tools count what they pass over, WN-7 batch 2 (D-64) - DRAFT until proved (2026-11-24)

**Tracked as GitHub issue [#1376](https://github.com/Ajmalpshaik/Heron-AI/issues/1376) since 2026-11-24** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**Why:** [D-64](../decisions/D-64.md) - a reading tool that goes looking and can drop a candidate declares how many it
dropped. Its worklist is [row 5b-1909](../fragment-issues/row-5b-1909.md), work-note item WN-7 of issue
[#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005); batch 1 is [Group QH](group-qh.md). This is batch 2: each
card below passed over a candidate whose read Revit refused without a word. In some cards that made a failed read
look like a "no" - *"an orphan tag pointing at nothing"*, *"every view and sheet shows"*, *"unconstrained"*. Each card
now counts it as an accounting output. Where the card has a `findings` list, it says the count in one sentence
**only when the count is above 0**. Each card's C# changed, so each is **DRAFT** with its version raised until its check
below runs and Ajmal PS signs (D-30). Each card keeps its version 1 proof as the record. All seven were PROVEN:

- [`REPORT_CONSTRAINTS`](../../brain/fragments/report-constraints/fragment.yaml) **version 2** - `unresolvedConstraints`:
  constraints whose references would not read, so an element they hold could be listed unconstrained.
- [`FIND_DEGENERATE_LINES`](../../brain/fragments/find-degenerate-lines/fragment.yaml) **version 2** - `skippedCurves`:
  elements whose curve or length would not read, never measured.
- [`CHECK_STRUCTURAL_SUPPORTS`](../../brain/fragments/check-structural-supports/fragment.yaml) **version 2** -
  `skippedMembers`: beams with no location line and columns with no base point, never checked.
- [`SELECT_BY_HOST`](../../brain/fragments/select-by-host/fragment.yaml) **version 2** - `unresolvedElements`: elements
  whose host would not read, which version 1 read as not hosted.
- [`SELECT_BY_PIN_STATE`](../../brain/fragments/select-by-pin-state/fragment.yaml) **version 2** - `unresolvedElements`:
  elements whose pinned state would not read.
- [`REPORT_TAGS_AND_TARGETS`](../../brain/fragments/report-tags-and-targets/fragment.yaml) **version 2** -
  `unresolvedTags`: tags whose targets would not read, which version 1 listed as orphans.
- [`REPORT_BROWSER_ORGANIZATION`](../../brain/fragments/report-browser-organization/fragment.yaml) **version 2** -
  `unresolvedItems`: views and sheets whose filter test or folders would not read, which version 1 counted as shown;
  *"Every view and sheet shows"* is said only when every one was read.

Not in this group, nothing to run: `ORDER_ELEMENTS_BY_POSITION` and `LIST_SHEET_SETS` only declare the `noPlace` and
`unread` their C# already counted and named. No line of their C# changed, so they stay PROVEN.

**What every check here looks for, the same rule each time:**
- The new count is always in the answer.
- Its sentence is there **only** when the count is above 0.
- An answer with nothing passed over reads exactly as version 1.

**Write each count down even when it is 0.** Nobody knows yet how often Revit refuses these reads on an ordinary
model, and that number is what this group is for. Arranging a model where Revit refuses a read is not asked, because
no one knows how to make it do so.

Written in a cloud container with no Revit. Every fragment compiles on 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-24). No routing words changed. **Nothing here has run in Revit.**

**The models are scratch copies, never a working model.** Each is the one the card's last proof or arrangement used,
named in the job files. Two job files, because the cards were last run on two releases:

```text
python tools/batch-prove.py tools/jobs/qo-wn7-batch2-revit2024-2026-11-24.yaml --dry-run
python tools/batch-prove.py tools/jobs/qo-wn7-batch2-revit2024-2026-11-24.yaml --session <process id>
python tools/batch-prove.py tools/jobs/qo-wn7-batch2-revit2020-2026-11-24.yaml --session <process id>
```

Dry runs on 2026-11-24: 6 WOULD RUN (2024) and 1 WOULD RUN (2020). `REPORT_CONSTRAINTS` and
`REPORT_BROWSER_ORGANIZATION` cannot come back empty, and their version 1 proofs were by D-53 tracking. So a batch
reads their negative as NEG NOT EMPTY - that is how the card works, not a failure.

| ID | Do this | Pass looks like |
|---|---|---|
| **QO1** | *`REPORT_CONSTRAINTS` version 2 - Revit 2024, Snowdon-scratch_ajmal.al.* The walls of one plan, then its ducts | Both: `constrainedBy`, `lockedTo` and `unconstrained` as version 1 gives them on the same elements; `unresolvedConstraints` written down (0 expected) |
| **QO2** | *`SELECT_BY_PIN_STATE` version 2 - Revit 2024, Snowdon-scratch_ajmal.al. THE NEGATIVE FIRST.* Ducts pinned, then not pinned | Version 1's *"0 of N scanned element(s) are pinned"* and *"N of N ... NOT pinned"*; `unresolvedElements` written down (0 expected) and **no** *"would not say whether they are pinned"* sentence while it is 0 |
| **QO3** | *`REPORT_TAGS_AND_TARGETS` version 2 - Revit 2024, Snowdon-scratch_ajmal.al.* The Parking plan, then a plan with tags | Parking: `tagCount` 0, `unresolvedTags` empty. The plan with tags: `tagTargets`, `orphanTags` and `multiTags` as version 1 reads them; `unresolvedTags` written down (empty expected) - a tag in it is in neither `orphanTags` nor `tagTargets` |
| **QO4** | *`FIND_DEGENERATE_LINES` version 2 - Revit 2024, Project1 work_ajmal.al, version 1's own job.* Ducts at 100000 mm, then 0.001 mm | Both as version 1's; `skippedCurves` written down (0 expected) and **no** *"would not give their curve or its length"* sentence while it is 0 |
| **QO5** | *`REPORT_BROWSER_ORGANIZATION` version 2 - Revit 2024, Snowdon-scratch_ajmal.al and a new Project1.* | Both: the organisation lines and first finding as version 1 reads them; `unresolvedItems` written down (0 expected) and no *"would not say whether the organisation's filter hides them"* sentence while it is 0 |
| **QO6** | *`CHECK_STRUCTURAL_SUPPORTS` version 2 - Revit 2024, Heron Gap Fill Batch 7 Test, its gap-fill arrangement.* Level 2 framing, then the Level 1 columns with a floor under them | Both as version 1's; `skippedMembers` written down (0 expected) and no *"had no location line or base point"* sentence while it is 0 |
| **QO7** | *`SELECT_BY_HOST` version 2 - Revit 2020, Project2, round 5's arrangement (places two windows - a scratch write).* | Negative: version 1's *"Nothing is HOSTED on it"*. Positive: the windows, as version 1's proof. Both: `unresolvedElements` written down (0 expected) and **no** *"would not say what hosts them"* sentence while it is 0 |
