# Needs checking — Group CZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CZ - a revision for each date typed on the sheets, put on the sheets that carry it, in one call (2026-10-11)

On 2026-10-10 a modeller in Revit 2020, with model *4355-BHVD-3D-50C10-BL001A* (19 sheets) open, had
`LIST_DATES_ON_SHEETS` list the dates typed on the sheets - 7 dates on 15 sheets - and then said *"now do
one thing with this date - create revisions, the revision description is IFI - issued for
information"*, with each revision on the sheets carrying its date. Nothing did that in one call. It took
`CREATE_REVISION` seven times and `FIND_SHEETS` + `SET_SHEET_REVISIONS` once per sheet, each its own
Undo entry, with revisions named *Seq. N - description* from an earlier read - and when the owner
deleted Seq. 1, Revit renumbered the rest and one of those names went stale.
[Row 5b-421](../fragment-issues/section-5b-rows-401-425.md).

**What was built.** New `CREATE_REVISIONS_FROM_SHEET_DATES` (FRG-SHT-029, DRAFT, MODIFY). It takes the
document and seven values, every one sent blank or not: `description` (required), `dateFormat`,
`onlyDates`, `issuedBy`, `issuedTo`, `numbering`, `issueState`. In one call, one Undo entry:

1. reads every sheet's own text notes - a note whose owner view is a sheet - with
   `LIST_DATES_ON_SHEETS`' parser, copied character for character; a note inside a plan placed on a
   sheet is not read, and a numeric month is refused;
2. for each distinct date, reuses a revision already holding that description and that date (typed as
   this call would write it, or reading as that one date through the same parser), else makes one -
   oldest date first, so the new sequence numbers run in date order. Two that both match is a refusal
   naming both;
3. adds each revision to exactly the sheets carrying its date, appended to what each sheet lists -
   never replacing it - and read back;
4. issues the revisions it made only when `issueState` says `issue`, last;
5. regenerates and looks for every revision the project had and every one it made; one gone rolls the
   whole call back - `SET_SHEET_REVISIONS`' warning that writing a sheet's list can purge an unused
   revision;
6. reports every date on one line in `dateRevisions` - *"22-JUL-2025 -> Seq. 3 - IFI - Issued for
   Information, dated '22-JUL-2025', CREATED: added to 2 sheet(s) (S-01, S-02)"* - read back after the
   last write, with `sheetsWithNoDate` and a summary of what was not read.

Refused with nothing created and no sheet changed when no sheet carries a date, when the description is
empty, when the date format, numbering or `issueState` is not one it takes, or when `onlyDates` names a
date no sheet carries.

**New rather than widened, and that was weighed** - in the card's own comment. `LIST_DATES_ON_SHEETS`
is a read and would become a write; `CREATE_REVISION` makes one revision and two job files read its
contract; `SET_SHEET_REVISIONS` is PROVEN and answers a different question (this revision, these
sheets). Each one's code is reused rather than called; none of the three was touched beyond a routing
comment. `EDIT_REVISION` version 3 ([PR #469](https://github.com/Ajmalpshaik/Heron-AI/pull/469)) issues
and edits the revisions afterwards and is not duplicated.

Compiled for Revit 2020 to 2027 on 2026-10-11 (`tools/check-fragments-compile.py`, every release ok).
`tools/api-surface` had no cache on this PC, so the members were read from the `RevitAPI.xml` of all
eight reference packages instead: every one documented 2020 to 2027, `Revision.NumberType` 2020 to 2022
and `RevisionNumberingSequenceId` from 2022, both reached by name as `CREATE_REVISION` does. **Nothing
below has run in Revit.** The arrangement and commands are in
[`create-revisions-from-sheet-dates-2026-10-11.yaml`](../../tools/jobs/create-revisions-from-sheet-dates-2026-10-11.yaml),
which `batch-prove --dry-run` reads as 1 WOULD RUN.

With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`
on every line.

| # | Run | Look for |
|---|---|---|
| **CZ1** | Arrange: Group CY's two scratch projects, CY1 and CY2, made and open together. In **Heron Sheet Dates** add one revision in View > Revisions: description *IFI - Issued for Information*, date *15 May 2026*, on no sheet. Save. Never one of the owner's working models | Revisions: Revit's own Seq. 1 and the new Seq. 2, both on no sheet and in no cloud |
| **CZ2** | `python tools/batch-prove.py tools/jobs/create-revisions-from-sheet-dates-2026-10-11.yaml` | positive: `datesFound` 3, `revisionsCreated` 2, `revisionsReused` 1, `revisionsAddedToSheets` 5, `sheetsChanged` 3; `dateRevisions` *22-JUL-2025 -> Seq. 3 ... CREATED: added to 2 sheet(s) (S-01, S-02) \|\| 15-MAY-2026 -> Seq. 2 ... dated '15 May 2026', REUSED as it was: added to 2 sheet(s) (S-02, S-03) \|\| 09-SEP-2026 -> Seq. 4 ... CREATED: added to 1 sheet(s) (S-01)*; `sheetsWithNoDate` *S-04*; `revisionsRemaining` 4 and the summary saying the project had 2 - **Seq. 1 not purged**; the summary naming the sequence out of date order and `REORDER_REVISION`; no 01-JAN-2026. negative: `refused` *3 sheet(s) read and 6 text note(s) typed on them, and none carries a date with a written month*, every count 0, `dateRevisions` empty |
| **CZ3** | `LIST_REVISIONS` on **Heron Sheet Dates** after CZ2 | Seq. 1 and Seq. 2 only - both legs rolled back |
| **CZ4** | The second route, KEPT, on a COPY of **Heron Sheet Dates**: `revit_change` with capability `CREATE_REVISIONS_FROM_SHEET_DATES` and CZ2's values, then View > Revisions and each sheet's Revisions on Sheet by eye | S-01 ticks Seq. 3 and Seq. 4, S-02 Seq. 2 and Seq. 3, S-03 Seq. 2, S-04 none; dates as in CZ2; nothing issued; one entry in Revit's Undo list |
| **CZ5** | CZ4 run again on the same copy | `revisionsCreated` 0, `revisionsReused` 3, `revisionsAddedToSheets` 0, the summary opening *Nothing needed doing* - a second run never makes a second set |
| **CZ6** | The refusals on **Heron Sheet Dates**, each through `batch-prove` or a rolled-back `validate`: `description` blank; `dateFormat` *DD-MON-YYYY* and *MMM YYYY*; `onlyDates` *01-JAN-2026*; a second hand-made revision *IFI - Issued for Information* dated *15-MAY-2026* | each refused naming what it takes, the dates on the sheets, or both revisions; nothing created and no sheet changed |
| **CZ7** | CZ2 on **Revit 2020** and **2027** - the projects made again under the same titles from that release's own templates; on 2020 also `numbering` *alphanumeric* and `dateFormat` *DD.MM.YYYY* with `issueState` *issue* | the same answers; on 2020 the new revisions alphanumeric, dated *22.07.2025* and *09.09.2026*, and issued after the sheets took them. Compiled on all eight releases; what Revit DOES was measured on none |
| **CZ8** | The signature | `python brain/heron_validate.py accept create-revisions-from-sheet-dates --by "Ajmal PS"`, on CZ2's record, on the owner's word only, after CZ4 |

**Routing, measured 2026-10-11 - not proof.** 50 sentences through `heron_retrieve.find`, the host's own
route, Revit 2020 filter, private scratch stores: main at `70bbfa52` (the tree without this card,
extracted to a scratch folder) against this branch, on the trained backend with the re-ranker - what
the owner's live Heron runs - and on the lexical one, from a clean venv holding only PyYAML (what CI
runs). The sentences: the modeller's own, four paraphrases of the job, every declared sentence and
routing-table row of `CREATE_REVISION`, `SET_SHEET_REVISIONS`, `EDIT_REVISION`, `LIST_REVISIONS` and
the date cards near this one, and ten listing and counting questions from other domains.

- **Trained backend, final wording: 5 change winner, every one to this card** - the modeller's sentence
  and the four paraphrases (*create revisions for the dates on the sheets*, *make a revision for each
  date on the sheets*, *turn the sheet dates into revisions*, *add revisions from the dates typed on
  the drawings*). On main all five went to `EDIT_REVISION` - **a write that changes one revision's
  columns**, the wrong job.
- **Lexical backend: the same 5, and nothing else.** On main they went to `EDIT_REVISION`,
  `LIST_REVISIONS` and `EXTRACT_DATES_FROM_TEXT`.
- **Three earlier wordings each took a sentence another card claims**, and none was kept: a purpose
  paragraph saying nothing is issued unless asked took *mark that revision as issued* from
  `EDIT_REVISION`; an identity ending *put on the sheets that carry it* took *put that revision on the
  sheets that carry its date* from `SET_SHEET_REVISIONS`; an identity opening *make a revision* took
  *make the revisions alphanumeric* from `EDIT_REVISION_NUMBERING_SEQUENCE`. The last two were the
  re-ranker, which reads only a card's identity, capability and domain
  (`heron_retrieve._fragment_passage`) - so on this PC the identity is the words that decide. Nine
  identities were scored against the re-ranker before one was chosen.
- **Not caused here, recorded:** *what text is on this sheet*, *list all the text notes* and *what is
  the date today* reach `LIST_DATES_ON_SHEETS` on both trees; *issue the revisions* reaches
  `LIST_REVISIONS` on both.

**The house tools, trained backend, against main:** `check-routing --revit 2020` - the same three
questions answered by a write and the same 95 routing-table claims not reached (the first wording made
it 96, with *mark that revision as issued*); its keyword-route section gained one line, *change the
revision date* (this card first by words), which `find` still sends to `EDIT_REVISION` by exact match,
and moved twelve others by a rank or a few - one of them, *tag these*, to a different card first by
words; it is a declared sentence and reaches its own card by exact match. `check-routing --targets` passes: every row names a declared
capability. `check-intrusion` does not put this card in its top 12. `check-risk-crossings`, `check-skill-routing` and
`score-routing` (the owner's 79 questions) answer the same on main and on this branch - score-routing's
per-question pattern string is identical, character for character.
