# Needs checking — Group BA

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-27 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group BA - the Sheet Issues/Revisions dialog from chat: every column, Move, Merge, Delete, Numbering and the arc length (2026-09-28)

**Ten fragments, all DRAFT.** On 2026-09-28 the owner checked Manage > Sheet Issues/Revisions in Revit 2024
control by control and asked for every one from chat. Four existing fragments were widened - each went back
to DRAFT because its code moved under its proof, and each keeps its old `proof:` block to re-prove from - and
six were added:

| Dialog control | Fragment |
|---|---|
| The table, read: every column, the number each revision prints, Per Project / Per Sheet, every numbering sequence, the arc length | [`list-revisions`](../../brain/fragments/list-revisions/fragment.yaml) LIST_REVISIONS, FRG-SHT-005 - widened |
| Add, with every column (numbering, date, description, issued, issued to, issued by, show) | [`create-revision`](../../brain/fragments/create-revision/fragment.yaml) CREATE_REVISION, FRG-SHT-004 - widened |
| Editing a row, and the Issued tick as its own value | [`edit-revision`](../../brain/fragments/edit-revision/fragment.yaml) EDIT_REVISION, FRG-SHT-016 - widened, version 2 |
| Delete, with `confirm` | [`delete-revision`](../../brain/fragments/delete-revision/fragment.yaml) DELETE_REVISION, FRG-SHT-017 - widened, version 2 |
| Move Up / Move Down | [`reorder-revision`](../../brain/fragments/reorder-revision/fragment.yaml) REORDER_REVISION, FRG-SHT-021 |
| Merge Up / Merge Down, with `confirm` | [`merge-revision`](../../brain/fragments/merge-revision/fragment.yaml) MERGE_REVISION, FRG-SHT-022 |
| Numbering Per Project / Per Sheet, and Arc length | [`set-revision-settings`](../../brain/fragments/set-revision-settings/fragment.yaml) SET_REVISION_SETTINGS, FRG-SHT-023 |
| Customize Numbering: New and Duplicate | [`create-revision-numbering-sequence`](../../brain/fragments/create-revision-numbering-sequence/fragment.yaml) FRG-SHT-024 |
| Customize Numbering: Edit, rename included | [`edit-revision-numbering-sequence`](../../brain/fragments/edit-revision-numbering-sequence/fragment.yaml) FRG-SHT-025 |
| Customize Numbering: Delete, with `confirm` | [`delete-revision-numbering-sequence`](../../brain/fragments/delete-revision-numbering-sequence/fragment.yaml) FRG-SHT-026 |

**The rules they share.** The per-revision tools are handed ONE revision by a find - SELECT_BY_PARAMETER_VALUE
on the Revisions category, by `Revision Sequence` or `Revision Description` - never the selection, because
other chats share the same Revit. Deleting and merging refuse unless `confirm` names that same revision
(Article 7); deleting a sequence needs its name repeated. An issued revision is refused BY NAME: Revit locks
its description, date, issued to, issued by and numbering, and Heron also refuses a move, a numbering switch
or a sequence change that would renumber what an issued revision prints. Every write is compared first
(`alreadyThat`) and read back after; a half-done call throws and the add-in rolls it back.

**Revit releases.** The seven revision tools compile and are written for 2020 to 2027. The three sequence tools
are 2022 to 2027 only: `RevisionNumberingSequence` arrived in 2022, and 2020/2021 have one numeric and one
alphanumeric scheme with no names. Where the numbering API parts at 2022, the 2020-2027 tools reach both
spellings by name, because the add-in compiles fragments with no release symbol ([row 5b-181](../FRAGMENT-ISSUES.md)).
Every member was read with `tools/api-surface --members` on all eight reference assemblies. **Only Revit 2024
has run any of it.**

**The one kept change, 2026-09-28, on the owner's word - a chat run, not a proof.** "heron ai bulding",
Revit 2024, Heron session 46596. `SELECT_BY_PARAMETER_VALUE` (Revision Description equals Issued for Review,
Revisions) found exactly 1 of 2. `EDIT_REVISION` with `issuedTo=Ajmal PS` and every other value empty
answered `changed` *issued to (blank) -> 'Ajmal PS'* and was kept. Read back by SELECT_BY_PARAMETER_VALUE on
`Issued to` equals Ajmal PS: 1 of 2, Seq. 2. The same call again answered `changed` empty and `alreadyThat`
*issued to 'Ajmal PS'*.

**Later the same day, not by these tools:** on the owner's instruction the chat he works in attached
"Revision 1" to all seven sheets and then deleted it with main's DELETE_REVISION (version 1). The model now
holds ONE revision - "Issued for Review", element 929474, which Revit renumbered to **Seq. 1**. Every "Seq. 2"
above was measured before that.

**Measured without changing anything, 2026-09-28** - 36 `fragment` runs with no `--write` on the same model,
so no transaction was open and Revit refused every change. Every tool reached Revit's own call and reported
what it would have done, with Revit's words (*"Attempt to modify the model outside of transaction"*); every
refusal below changed nothing, and LIST_REVISIONS read the model unchanged afterwards:
merge without `confirm`, with `confirm=yes`, and with `confirm=1` for Seq. 2; delete without `confirm` and with
`confirm=2` for Seq. 1; two revisions handed to EDIT_REVISION; a show word and a sequence that do not exist;
an empty description; arc length 0; `Numeric` deleted while Seq. 1 and Seq. 2 use it (refused, naming both);
a custom list on a numeric sequence; a new sequence named `Numeric`. The same runs caught one defect before it
shipped: [row 5b-245](../FRAGMENT-ISSUES.md).

**Routing, measured 2026-09-28** through `heron_retrieve.find` on a scratch store built from this change: all
eleven sentences in the owner's brief reach their tool by identity - *set issued to*, *tick issued* (EDIT_REVISION),
*move revision 2 up* (REORDER_REVISION), *merge revision 2 down* (MERGE_REVISION), *delete revision 1*
(DELETE_REVISION), *number revisions per project* and *set the cloud arc length* (SET_REVISION_SETTINGS), *make an
alphabetic sequence* (CREATE_REVISION_NUMBERING_SEQUENCE), *rename the numbering* (EDIT_REVISION_NUMBERING_SEQUENCE),
*list the revisions* (LIST_REVISIONS) and *create a revision* (CREATE_REVISION) - and seven undeclared paraphrases
did too. Two undeclared QUESTIONS reach a setter; both are guarded, and recorded as [row 5b-246](../FRAGMENT-ISSUES.md).
Against main's own check-routing, the words ranking moved for one sentence of another tool's: *"rename these with a
suffix"* now leans to EDIT_REVISION_NUMBERING_SEQUENCE, where main already sent it to a wrong tool.

**Not covered, and why.** The revision cloud's own shape is drawn with ADD_REVISION_CLOUD, not in this
dialog. Editing the 2020/2021 project numbering schemes (their "Numbering Options") is not built - LIST_REVISIONS
reads them there.

| # | Check | Expected |
|---|---|---|
| **BA1** | **On the WORKING model, by eye.** Manage, Sheet Issues/Revisions: read the row "Issued for Review" - Seq. 1 since "Revision 1" was deleted | Issued to reads *Ajmal PS*; every other column as before (Numeric, 28-09-2026, Issued for Review, not issued, issued by Heron, Cloud and Tag). Numbering Per Sheet, arc length 20.0, sequences Numeric and Custom |
| **BA2** | The two legs ([D-30](../DECISIONS.md)) for nine of the ten - **drafts, not yet signed.** On a TEST COPY of "heron ai bulding", in front, with `HERON_CLIENT_ID=ajmal-pc`: `python tools/batch-prove.py tools/jobs/sheet-issues-revisions-test-copy-2026-09-28.yaml --dry-run`, then without `--dry-run` | `PASS` for all nine. The copy needs only the one revision the working model now has: delete, move and merge make a second one in their own setup. The edit-revision negative ISSUES Seq. 1 in its setup and must be refused naming it - the one issued-revision refusal no model on this PC could show yet. Then LIST_REVISIONS on the copy: nothing kept |
| **BA3** | LIST_REVISIONS re-proved - read-only, so the working model is fair: `validate list-revisions` with a second open model as the negative, the arrangement its kept 2026-09-13 proof block records | Its sheet lines and `onNoSheets` as before, plus `revisionTable`, `numbering`, `numberingSequences` and `arcLength` as the dialog shows them |
| **BA4** | **Revit 2020, by eye then by run.** LIST_REVISIONS, then CREATE_REVISION with `numbering=alphanumeric` on a test project | The 2020/2021 route - `Revision.NumberType` and the project's two schemes, reached by name - has never run. The table's numbering column reads Numeric / Alphanumeric / None, and `numberingSequences` shows the two schemes |
| **BA5** | `numbering=none` on Revit 2022 or later, on a test copy, through EDIT_REVISION | Unknown: the tool sets no sequence and reads back what Revit holds. Write down whether Revit accepts a revision with no numbering sequence, and correct the purpose if it does not |
| **BA6** | Signing - **after BA2 and BA3, never before.** `accept` each draft in `brain/proof-drafts/` under the owner's own name | The four widened fragments go back to PROVEN and the six new ones join them. Until then all ten are DRAFT, and a chat says so on every run |
