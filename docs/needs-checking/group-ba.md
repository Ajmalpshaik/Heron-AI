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
| Customize Numbering: Edit, rename included - and since 2026-10-10 (version 2) the Revit 2020/2021 Numbering Options | [`edit-revision-numbering-sequence`](../../brain/fragments/edit-revision-numbering-sequence/fragment.yaml) FRG-SHT-025 |
| Customize Numbering: Delete, with `confirm` | [`delete-revision-numbering-sequence`](../../brain/fragments/delete-revision-numbering-sequence/fragment.yaml) FRG-SHT-026 |

**The rules they share.** The per-revision tools are handed ONE revision by a find - SELECT_BY_PARAMETER_VALUE
on the Revisions category, by `Revision Sequence` or `Revision Description` - never the selection, because
other chats share the same Revit. Deleting and merging refuse unless `confirm` names that same revision -
its element id or Revit's "Seq. N - description", never a bare number (Article 7); deleting a sequence needs
its name repeated. An issued revision is refused BY NAME: Revit locks its description, date, issued to,
issued by and numbering, and Heron also refuses a move, a merge, a delete, a numbering change on a revision,
a numbering switch or a sequence change that would renumber what an issued revision prints. Every write is
compared first (`alreadyThat`) and read back after; a write Revit took that reads back different, or a
refusal after another write went in, throws and the add-in rolls the whole call back.

**Reviewed before merge, 2026-09-28** - Codex was out of review credit, so an independent review read the
ten files and found three gaps, all fixed in the same PR before it merged: (1) a numbering change on EDIT_REVISION, and
a merge or delete, could renumber a LATER issued revision without a word - now refused, naming it; (2) in
EDIT_REVISION, SET_REVISION_SETTINGS and EDIT_REVISION_NUMBERING_SEQUENCE a FIRST write that Revit took but
that read back different was reported as "nothing was changed" and kept - now it throws; (3) `confirm`
accepted a bare sequence number, which the find and the confirm share, so a yes given before a renumber
would pass on whichever revision holds that number now - now only the element id or the full name binds.
These fixed paths are compiled on every release and NOT yet run: Revit was not connected when they
were made. BA2 and the renumber job cover them.

**Revit releases.** The seven revision tools compile and are written for 2020 to 2027. The three sequence tools
were 2022 to 2027 only: `RevisionNumberingSequence` arrived in 2022, and 2020/2021 have one numeric and one
alphanumeric scheme with no names. Since 2026-10-10 EDIT_REVISION_NUMBERING_SEQUENCE spans 2020 to 2027 (below,
and BA7 to BA9); New, Duplicate and Delete stay 2022 on, because 2020/2021 always have exactly those two.
Where the numbering API parts at 2022, the 2020-2027 tools reach both spellings by name, because the add-in
compiles fragments with no release symbol ([row 5b-181](../FRAGMENT-ISSUES.md)).
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
shipped: [row 5b-245](../FRAGMENT-ISSUES.md). *Note 2026-10-17, [row 5b-350](../fragment-issues/section-5b-rows-326-350.md): "nothing could change" here
leaned on Revit refusing every change with no transaction open - LIST_REVISIONS afterwards is what showed the
model unchanged. That refusal is not the whole guarantee, and the command line now refuses a MODIFY fragment
without `--write`; these runs stand as recorded, and a repeat uses `--write` and no `--apply`, rolled back.*

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
dialog. Editing the 2020/2021 project numbering schemes (their "Numbering Options") was not built until
2026-10-10 - now EDIT_REVISION_NUMBERING_SEQUENCE version 2, below; LIST_REVISIONS reads them there.

| # | Check | Expected |
|---|---|---|
| **BA1** | **On the WORKING model, by eye.** Manage, Sheet Issues/Revisions: read the row "Issued for Review" - Seq. 1 since "Revision 1" was deleted | Issued to reads *Ajmal PS*; every other column as before (Numeric, 28-09-2026, Issued for Review, not issued, issued by Heron, Cloud and Tag). Numbering Per Sheet, arc length 20.0, sequences Numeric and Custom |
| **BA2** | The two legs ([D-30](../DECISIONS.md)) for nine of the ten - **drafts, not yet signed.** On TWO TEST COPIES of "heron ai bulding", both open, made BY HAND first as the job file's header says - "heron ai bulding test copy" with a second revision 'HERON PROOF second' added unissued, and "heron ai bulding issued copy", the same with that revision ticked Issued - with `HERON_CLIENT_ID=ajmal-pc`: `python tools/batch-prove.py tools/jobs/sheet-issues-revisions-test-copy-2026-09-28.yaml --dry-run`, then without `--dry-run` | `PASS` for all nine, and then `tools/jobs/sheet-issues-revisions-renumber-2026-09-28.yaml` - `PASS`, its negative refusing a numbering change that would renumber an ISSUED later revision. Delete, move and merge find the second revision the test copy holds; the edit-revision negative runs in the issued copy, on Seq. 2, and must be refused naming it. **Rewritten 2026-10-17 ([row 5b-917](../fragment-issues/section-5b-rows-401-425.md)):** until then the setups MADE the second revision and ISSUED Seq. 1 themselves, which `validate` now refuses before Revit is asked - a step that changes the model runs after the find, so the find saw nothing. Then LIST_REVISIONS on both copies: nothing kept |
| **BA3** | LIST_REVISIONS re-proved - read-only, so the working model is fair: `validate list-revisions` with a second open model as the negative, the arrangement its kept 2026-09-13 proof block records | Its sheet lines and `onNoSheets` as before, plus `revisionTable`, `numbering`, `numberingSequences` and `arcLength` as the dialog shows them |
| **BA4** | **Revit 2020, by eye then by run.** LIST_REVISIONS, then CREATE_REVISION with `numbering=alphanumeric` on a test project | The 2020/2021 route - `Revision.NumberType` and the project's two schemes, reached by name - has never run. The table's numbering column reads Numeric / Alphanumeric / None, and `numberingSequences` shows the two schemes |
| **BA5** | `numbering=none` on Revit 2022 or later, on a test copy, through EDIT_REVISION | Unknown: the tool sets no sequence and reads back what Revit holds. Write down whether Revit accepts a revision with no numbering sequence, and correct the purpose if it does not |
| **BA6** | Signing - **after BA2 and BA3, never before.** `accept` each draft in `brain/proof-drafts/` under the owner's own name | The four widened fragments go back to PROVEN and the six new ones join them. Until then all ten are DRAFT, and a chat says so on every run |

**2026-10-10 - EDIT_REVISION_NUMBERING_SEQUENCE version 2: the Revit 2020/2021 Numbering Options. DRAFT, compiled,
never run.** The modeller works in Revit 2020 (Heron session 36948) and asked to *"change the alphabetic revision
list to AA, BB, CC, DD"* - his project standard is the doubled letters AA to ZZ. `heron_lookup` filtered to 2020
offered nothing. FRG-SHT-025 was widened rather than a second tool written (one job, one fragment): it was DRAFT
with no proof, so no proof went stale. On 2020/2021 `name=Alphanumeric` or `name=Numeric` picks one of the
project's two schemes (empty is allowed when the values say which); a rename and minimum digits are refused there.
On every release the custom list is now taken whole - an empty value, a value twice (capitals ignored), an ellipsis,
`<blank>` or a list of nothing is refused by name. Every member that exists on only part of 2020-2027 is reached by
name, as LIST_REVISIONS does - each looked up in the `RevitAPI.xml` of all eight reference packages, because
`tools/api-surface` cannot run on this PC (Application Control blocks the freshly built `ApiSurface.dll`, as group
CY found) - and `tools/check-fragments-compile.py` compiled it on all eight releases (2020 and 2021 on net472). The issued-revision rule is unchanged and now applies on 2020 too: a settings change is refused while any
ISSUED revision is numbered by that scheme. **On the modeller's working model that may well refuse** - its list read
*AA, BB, CC, D, E, F, …* on 2026-10-10, so revisions AA to CC may be issued - even though AA to ZZ would leave AA,
BB and CC printing what they print now. Whether the rule should refuse only when an issued revision's PRINTED
number would move is the owner's decision, not made here.

**Routing, measured 2026-10-10** on private scratch stores, before and after, on the trained-model backend with the
re-ranker (as the live Heron answers) and on the lexical one (as CI does). `check-routing` - unfiltered, `--revit
2020`, and lexical - moved only by the new sentence, found first by both routes, and by *"write on the drawing"*
rising #3 to #2 under the same winner; no new question answered by a writer and no new unreached routing claim.
`check-risk-crossings` (2020 and 2024), `check-intrusion` and `check-skill-routing`: no winner moved. Thirty nearby
sentences through `RETRIEVE.find`: unfiltered and on 2024 none moved. **On Revit 2020 twelve now reach this tool**,
and the version-1 purpose with only the releases widened gives the same twelve, so it is the tool being offered
on 2020 that moves them, not the wording. Seven are its own (the modeller's sentence, which went to EDIT_REVISION;
*numbering options*, *double letters*, *alphanumeric revision sequence*, *numbering prefix*, *edit the numbering
sequence*, *rename the numbering*). Four are writes that went to another write before - *make an alphabetic
sequence* (was RENUMBER_SEQUENTIAL), *delete the numbering sequence* (was DELETE_ELEMENTS; 2020 has none to delete),
*use alphanumeric numbering on revision 2* (was SET_REVISION_SETTINGS; EDIT_REVISION is right) and *change the
sheet numbers to start at 100* (was CREATE_SHEET; 2024 already sent it here). **One is a question:** *"what letters
does the alphanumeric revision numbering use"* went to LIST_REVISIONS on 2020 and now reaches this write - on 2022
and later it already reached CREATE_REVISION_NUMBERING_SEQUENCE, a write. Guarded, as row 5b-246's two are: a
question carries no values, and with every value empty this tool refuses *"nothing was asked for"*. Not
declared by anybody, so not tuned here; a register row is owed.

| # | Check | Expected |
|---|---|---|
| **BA7** | **Revit 2020, the two legs ([D-30](../DECISIONS.md)) for version 2.** On "Project2", a scratch project, in front: LIST_REVISIONS first - the alphanumeric list must read A, B, C … Z and no ISSUED revision may be numbered alphanumeric. Then `python tools/batch-prove.py tools/jobs/edit-revision-numbering-revit-2020-2026-10-10.yaml --dry-run`, then with `--session <the Revit 2020 pid>` and without `--dry-run` | `PASS`: the positive `changed` *custom sequence A, B, C … Z (26 values) -> AA, BB, CC … ZZ (26 values)*, read back in `findings`; the negative (`AA, BB, CC, AA`) `refused` naming 'AA' 2 times with `changed` empty. Then LIST_REVISIONS: still A … Z (both legs rolled back), and every revision's numbering column as before |
| **BA8** | Revit 2020, the refusals - `fragment` runs on Project2 with `--write` and no `--apply`, so whatever a run does is rolled back: `name=Numeric customSequence=A, B`; `name=Alphanumeric newName=Letters`; `name=Numeric minimumDigits=2`; `customSequence=AA, , CC`; `customSequence=AA, BB, ..., ZZ`; `customSequence=<blank>`; `name=Nonesuch` | Each `refused` by name, ending "Nothing was changed." - the last one naming Numeric and Alphanumeric. A valid call goes through and the group is ROLLED BACK instead, which is the measurement. (Written for runs WITHOUT `--write`, where a valid call came back in Revit's missing-transaction words; the command line refuses that for a MODIFY fragment since 2026-10-17 - [row 5b-350](../fragment-issues/section-5b-rows-326-350.md)) |
| **BA9** | Revit 2020, the issued-revision refusal: on Project2, one revision numbered alphanumeric and ticked Issued BY HAND in the dialog, then BA7's positive values with `--write` and no `--apply` - not without `--write`, which the command line refuses for a MODIFY fragment since 2026-10-17 ([row 5b-350](../fragment-issues/section-5b-rows-326-350.md)). Un-tick it afterwards | `refused` *"'Alphanumeric' numbers ISSUED revision(s) Seq. N …"*, nothing changed, and the group rolled back |

**2026-10-10 - EDIT_REVISION version 3: several revisions in one call. DRAFT, compiled, never run.** On the
owner's job `4355-BHVD-3D-50C10-BL001A` (Revit 2020) seven revisions "IFI - Issued for Information" were issued
through version 2 - a find and an edit for each, Seq. 1 up to Seq. 7 - and later un-issued and moved to
Alphanumeric, which version 2 refused for Seq. 1 first (*"would renumber Seq. 2 … Seq. 7 …, which are ISSUED"*)
and took from Seq. 7 down. The owner asked why it went one by one. FRG-SHT-016 was widened, not a second tool
written: the find now hands in the whole set, and the new value `confirmCount` must equal the number it found -
blank is fine for one; several with it blank, or a count that disagrees, are refused with every one named and
nothing changed. One call is one entry in Revit's undo list, all or nothing. The writes go un-issue (last
sequence first), text columns, numbering (last first), show, issue (first sequence first) - the orders Revit
took on the job - and the renumber check judges each revision by the state the set will be in when numbering
is written. A lock or renumber refusal on any revision changes none of them. One revision answers exactly as
version 2 did, but `confirmCount` must be SENT (blank) - the add-in refuses a request value that never arrives
([row 5b-227](../FRAGMENT-ISSUES.md)) - so both job files above now send it. `tools/check-fragments-compile.py`
compiled it on all eight releases. Row [5b-409](../FRAGMENT-ISSUES.md).

**Version 2 was signed on Revit 2020 in #470** (fingerprint `b928479162dc855d`, Project2, 2026-10-10). This code
moves under that proof, so EDIT_REVISION is DRAFT again with that proof block kept; version 3 needs BA10 and BA11.
BA11's negative is also the refusal of an edit to an ISSUED revision that [row 5b-412](../FRAGMENT-ISSUES.md)
says is unmeasured - arranged by hand, so no setup step changes the model.

**Routing, measured 2026-10-10** on scratch stores, before and after, trained backend: check-routing's own
sentences - EDIT_REVISION's nine reach it by words, the new one *"mark all the revisions as issued"* first by
words and nearness; *"change the revision description"* goes from second to third by nearness (REORDER_REVISION
first in both); three other tools' READ sentences each move one rank. The questions answered by a write stay
three, the routing-table claims not reached stay ninety-five. `heron_retrieve.find` on 26 held-out sentences -
edits, questions about revisions, the other revision tools - answered identically before and after, Revit 2020
and 2024. A first wording that opened the purpose with "several" cost the card its own *"change the revision
date"* by nearness (first to fourth), so the purpose keeps version 2's opening and the history is in comments.

| # | Check | Expected |
|---|---|---|
| ~~**BA10**~~ | **Revit 2020, the two legs ([D-30](../DECISIONS.md)) for version 3 - the count.** On "Project2", a scratch project, in front - NEVER the job model. Arrange first, kept, by chat: CREATE_REVISION three times, description *HERON PROOF several*, numbering numeric, not issued; LIST_REVISIONS shows exactly three such rows. Then `python tools/batch-prove.py tools/jobs/edit-revision-several-2026-10-10.yaml --dry-run`, then with `--session <the Revit 2020 pid>` and without `--dry-run` | `PASS`. The binding note reads *elements from select-by-parameter-value (3)*. Positive (`confirmCount=3`, issued by M.Sagheer, issued to A.Rahmani, issue): `changed` names all three, apart by " \|\| ", each ending *issued*; `nowIssued` true. Negative (`confirmCount=2`): `refused` names all three and both numbers, `changed` empty. Then LIST_REVISIONS: the three still unissued with issued by and to blank (both legs rolled back) - **Struck 2026-10-18:** run 2026-10-17 on "Project1", Revit 2020 - PASS, *2026-10-17 - run on Revit 2020* below - and signed with BA14: `edit-revision` PROVEN, fingerprint `d3169656228813bf` in its `fragment.yaml`. |
| ~~**BA11**~~ | **Revit 2020, version 3 - the order, and the issued-revision refusal.** On the same scratch project, after BA10's three: CREATE_REVISION three times more, *HERON PROOF several issued*, numbering numeric, ISSUED - they must be the LAST three sequences. Then `tools/jobs/edit-revision-several-issued-2026-10-10.yaml`, dry run first | `PASS`. Positive (`confirmCount=3`, unissue, numbering alphanumeric): `changed` *un-issued; numbering Numeric -> Alphanumeric* on all three, `refused` empty - the request version 2 refused for Seq. 1 first. Negative (issued by, no unissue): `refused` naming all three as ISSUED and *issued by*, none changed - the refusal BA2's job could not arrange. Then LIST_REVISIONS: all three still issued and Numeric - **Struck 2026-10-18:** run 2026-10-17 on "Project1", Revit 2020 - PASS, *2026-10-17 - run on Revit 2020* below - and signed with BA14: `edit-revision` PROVEN, fingerprint `d3169656228813bf` in its `fragment.yaml`. |
| **BA12** | **By eye, in Revit, once - the one undo entry.** After a KEPT run of version 3 on the scratch project (three revisions, one call), open Revit's Undo list | ONE entry for the call, named after it, and one Undo puts all three back. Then signing: `accept` BA10's draft under the owner's own name - BA11's draft is the second leg of the same fragment and goes in its proof notes |

**2026-10-17 - run on Revit 2020 (session 51568): `EDIT_REVISION` version 3 three times and `LIST_REVISIONS` once,
four `PASS`es, every write leg rolled back. Two drafts wait for Ajmal PS's signature.** Not "Project2": the scratch
"Project1" Ajmal PS had open - a new project from Revit 2020's mechanical template, never saved - where he made the
rows BY HAND in Manage - Sheet Issues/Revisions (Heron's change tool was blocked in that chat, and `CREATE_REVISION`
is DRAFT): Seq. 1-3 *HERON PROOF several*, not issued; Seq. 4-6 *HERON PROOF several issued*, ISSUED; all Numeric,
on no sheet, Per Project. Each job file is its 2026-10-10 namesake with `in: "Project1"` added.

- **BA10, the count** - [`edit-revision-several-project1-2026-10-17.yaml`](../../tools/jobs/edit-revision-several-project1-2026-10-17.yaml):
  binding note *elements from select-by-parameter-value (3)*; positive `confirmCount=3` - `changed` names Seq. 1, 2 and
  3, each *issued by (blank) -> 'M.Sagheer'; issued to (blank) -> 'A.Rahmani'; issued*, `nowIssued` true; negative
  `confirmCount=2` - `refused` *"the find brought back 3 revision(s) (...) and confirmCount says 2 ... Nothing was
  changed."*, `changed` empty. **This is the draft to sign.**
- **BA11, the order and the issued-revision refusal** - [`edit-revision-several-issued-project1-2026-10-17.yaml`](../../tools/jobs/edit-revision-several-issued-project1-2026-10-17.yaml):
  positive - Seq. 4-6 *un-issued; numbering Numeric -> Alphanumeric* in one call, `refused` empty: the 2020
  numbering route, through version 3; negative (a new issued by, no un-issue) - `refused` naming all three as ISSUED,
  `changed` empty. The refusal [row 5b-412](../fragment-issues/section-5b-rows-401-425.md) said was unmeasured.
- **One revision** - [`edit-revision-one-project1-2026-10-17.yaml`](../../tools/jobs/edit-revision-one-project1-2026-10-17.yaml),
  version 2's case through version 3's code, `confirmCount` sent blank: Seq. 1 issued by, to and issued; asked for
  the description and date it held, `alreadyThat` *description 'HERON PROOF several'; date 'Date 1'* and nothing
  written.
- **BA3 and BA4, `LIST_REVISIONS`** - [`list-revisions-project1-2026-10-17.yaml`](../../tools/jobs/list-revisions-project1-2026-10-17.yaml):
  positive on Project1 - all six rows with every column, and the 2020/2021 route read the project's Numeric scheme and
  Alphanumeric A to Z by name; negative on Ajmal PS's family `TRG_GEN_CutOut_Rectangular_HVAC` - nothing, *"is a
  family, and a family holds no revisions"*. A family is the only negative a revision read has: a project always
  keeps one revision. Until that day this fragment threw on a family - [row 5b-880](../fragment-issues/section-5b-rows-401-425.md),
  fixed in the same change, so its fingerprint moved and this is the code the draft is for.

`LIST_REVISIONS` read Project1 before the first run, after the first and after the last: the six rows as he made
them each time. Run records and drafts kept at `%TEMP%\heron-work\proofs-2026-10-17\keep`.

| # | Check | Expected |
|---|---|---|
| **BA13** | Revit 2020, read only: `LIST_REVISIONS` on a project where a SHEET carries a revision - BL001A is fair, nothing is written | each revision's sheet line - *"N sheet(s): ..."* and what it prints on each - as the sheets' revision schedules show. 2026-10-17's run had no revision on a sheet; the sheet half was last seen on Revit 2024, 2026-09-13 |
| ~~**BA14**~~ | Signing, after reading both drafts: `python brain/heron_validate.py accept edit-revision --by "Ajmal PS"` and `... accept list-revisions --by "Ajmal PS"`, from the checkout that holds them | proof blocks written; `heron-status: PROVEN` is a separate yes. BA12 - the one undo entry of a KEPT call - is still owed and needs a kept run, which a DRAFT gets only through `try-once` on a saved model. **DONE 2026-10-17: both signed by Ajmal PS and PROVEN** (`edit-revision` `d3169656228813bf`, `list-revisions` `bb5283ef92b73801`); BA12 now needs only a kept `revit_change` call, as the tool is PROVEN - **Struck 2026-10-18:** done, as it says; BA12 is still owed in its own row. |
