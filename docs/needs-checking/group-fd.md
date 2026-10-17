# Needs checking — Group FD

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group FD - a text note's numbering, bullets, indent and bold set through Heron: `EDIT_TEXT_VALUES` mode format, and the read that checks it (2026-10-17)

[FRAGMENT-ISSUES 5b-737](../fragment-issues/section-5b-rows-401-425.md). `EDIT_TEXT_VALUES` version 5 adds
mode **format**: on a text note, it sets numbers, bullets, lowercase or uppercase letters or no list, a
list's start number, an indent, and bold, italic or underline - on every paragraph or on the paragraphs
named - and changes no word. `READ_ELEMENT_PARAMETERS` version 4 reads it back: each paragraph's list, each
list's start number, and which paragraphs are bold, italic or underlined.

**What is run and what is not.** Both cards compile for all eight releases 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-17), and every Revit member they call reads the same in each
release's RevitAPI (`tools/api-surface`, one release at a time). **Run 2026-10-17 in Revit 2020 (session
55496), every run rolled back:** FD1, FD2, FD3, FD5, FD6, FD7 and FD9, each answered as this table looks
for, and the scratch model counted 12,088 elements before the first run and after the last. Both proof
drafts are written - `edit-text-values` from FD1/FD2, fingerprint `83f7bbbd82e2acee`, and
`read-element-parameters` from FD3, fingerprint `9f2e553abbfabdd2` - and both cards are DRAFT until
FD10. **Not run:** FD4 (a kept run - the owner's choice), FD8 (Revit 2024 and 2027), and a note typed by
hand in Revit's editor (below).

**How it was arranged, which is not quite the arrangement below.** Not a new project: making one is
`CREATE_PROJECT_DOCUMENT`, an ADMIN tool, so the scratch project is a copy of Revit 2020's own
`rst_basic_sample_project.rvt`, saved as *Heron Note Format Test* in `%TEMP%\heron-work\fd-295356\`. Not
typed by hand: a scratch setup step kept outside the repository (`scratch-fd-notes`, copied beside the
project under `keep\`) made Note A and Note B exactly as worded below, through `FormattedText` -
`SetListType(Bullet)` for Note B's bullets, bold and underline on paragraph 2's and 3's words - in the
drafting view *Continuous Footing*, which holds no text note of its own, inside each run's own rolled-back
group; it hands both notes on, so each run's binding note reads `elements from scratch-fd-notes (2)`.
`tools/jobs/note-format-2026-10-17.yaml` was not used: it selects notes typed into Level 1 before the run,
and a setup step that makes them runs after every read step ([row 5b-520](../fragment-issues/section-5b-rows-401-425.md)).
Each leg went through `heron_bridge_client.py validate --write --keep-chain --setup scratch-fd-notes`, one
fragment at a time. **So what a hand-typed note does is not measured** - the editor's Bullets button
against `SetListType` is the gap; FD4 or FD8 on a hand-typed note closes it.

**ON YOUR OWN PC, IN A LOCAL SESSION, ON A SCRATCH PROJECT - NEVER ON THE 4355 MODEL OR ANY WORKING
PROJECT.** In Revit 2020 first: a new project from the default metric template, saved as
*Heron Note Format Test*. In the Level 1 floor plan, type exactly two text notes and nothing else:

- **Note A** - four paragraphs, Enter after each of the first three, no list, nothing bold:
  `GENERAL NOTES` / `ALL DIMENSIONS ARE IN MM` / `DO NOT SCALE FROM THIS DRAWING` / `SEE DETAIL A`
- **Note B** - three paragraphs made a bulleted list with the editor's Bullets button, paragraph 2 bold,
  paragraph 3 underlined: `FIRST POINT` / `SECOND POINT` / `THIRD POINT`

Save. Changes must be ON for FD1 and FD2.

| # | Run | Look for |
|---|---|---|
| **FD1** | `python tools/batch-prove.py tools/jobs/note-format-2026-10-17.yaml` - the edit leg: both notes selected, field `Text`, mode `format`, text `numbers 2-4; bold 1`, replacement empty, rolled back | **The D-30 positive.** `changed` holds Note A only, and `noteFormatting` reads back *"1 no list, 2-4 ArabicNumbers from 1; bold 1"*, words unchanged. Note B is in `absent` and its line says it *"has 3 paragraph(s), so 'numbers 2-4' names paragraph 4, which is not there - it was NOT edited"*. Afterwards both notes on the plan read exactly as typed (the run was rolled back). **RUN 2026-10-17, Revit 2020, rolled back - as looked for.** `changed 1 item(s) [1474096]`, `absent 1 item(s) [1474097]`, `noteFormatting` *"note 1474096: 'numbers 2-4; bold 1' - 4 paragraph(s), words unchanged; read back 1 no list, 2-4 ArabicNumbers from 1; bold 1 ## note 1474097 has 3 paragraph(s), so 'numbers 2-4' names paragraph 4, which is not there - it was NOT edited"*. The proof draft's positive |
| **FD2** | The same job's negative leg: text `numbers 2-5` | **The D-30 negative.** REFUSED - *"the paragraphs named are not there in 2 of the 2 element(s) handed in, so NOTHING WAS CHANGED"* - and `changed` empty. Both notes as typed. **RUN 2026-10-17, Revit 2020, rolled back - as looked for.** `changed 0 item(s)`, `refused` *"the paragraphs named are not there in 2 of the 2 element(s) handed in, so NOTHING WAS CHANGED"*, each note named with 4 and 3 paragraphs. The proof draft's negative |
| **FD3** | The job's read leg: `READ_ELEMENT_PARAMETERS` parameterName `Text` on the same two notes; negative `ZZZNOTHINGHERE` | Note A's `noteWording` line says *"no list"* and nothing about bold; Note B's ends *"lists: 1-3 Bullet \|\| bold 2; underline 3"*. Negative: both `absent`, `noteWording` null. **RUN 2026-10-17, Revit 2020, rolled back - as looked for.** Note A *"... SEE DETAIL A \|\| no list"*, Note B *"FIRST POINT\rSECOND POINT\rTHIRD POINT \|\| lists: 1-3 Bullet \|\| bold 2; underline 3"*, `values 2 entry(ies)`; negative `absent 2 item(s)`, `values 0 entry(ies)`, `noteWording (null)`. The read's proof draft. Also run on the 8-paragraph 1-2-3 note of FD9: *"lists: 1-8 ArabicNumbers from 1 \|\| partly bold 1, 4; italic 6; underline 5 \|\| not typed plainly: ’ (U+2019)"* |
| **FD4** | *The one kept run, only if you choose to.* In the chat: *"number paragraphs 2 to 4 of this note and make the first line bold"* with Note A selected. Read Revit's Keep or Discard box, look at the note, choose **Keep** | The note on the plan shows `GENERAL NOTES` in bold and `1. ALL DIMENSIONS…` `2. DO NOT SCALE…` `3. SEE DETAIL A` - one undo entry. Then ask *"is this note numbered"*: it must answer through `READ_ELEMENT_PARAMETERS` and say *"2-4 ArabicNumbers from 1 \|\| bold 1"*. Write down whether the numbers 1. 2. 3. look bold too - Heron bolds the whole paragraph, closing carriage return included, and Autodesk says that is what a list number takes its bold from |
| **FD5** | On Note B (bullets): text `numbers 2-3`, replacement empty, rolled back | *An observation, not a proof.* Autodesk's reference says changing one paragraph of a list changes the whole list. Write down which happened: paragraph 1 renumbered with 2-3 (then the note must be in `unverified` and `noteFormatting` must say *"paragraph 1 was not named and reads back ArabicNumbers"*), or paragraph 1 left a bullet and the note `changed`. **RUN 2026-10-17, Revit 2020, rolled back - the WHOLE LIST changed, as Autodesk's reference says.** Note B in `unverified`, *"the words are as they were but the formatting READS BACK DIFFERENT from 'numbers 2-3' - paragraph 1 was not named and reads back ArabicNumbers, not Bullet - Revit gives every paragraph of one list the same list type"*; Note A `changed`, *"1 no list, 2-3 ArabicNumbers from 1, 4 no list"*. Kept, that note would be numbered 1-3 and reported unverified, never changed |
| **FD6** | On Note A: text `start 3 2-4` (no list there yet), then text `numbers 2-4; start 3 2-4`, then `indent 9 2`, each rolled back | First: `readOnly`, NOT edited, *"paragraph 2 is no list, and only numbers or letters have a start"*. Second: `changed`, read back *"2-4 ArabicNumbers from 3"* - the plan would show 3. 4. 5. Third: `readOnly`, *"Revit indents a paragraph N level(s) at most"* - write down N. **RUN 2026-10-17, Revit 2020, rolled back - as looked for.** First: `readOnly` *"NOT edited - 'start 3 2-4': paragraph 2 is no list, and only numbers or letters have a start"*. Second: `changed`, *"2-4 ArabicNumbers from 3"*. Third: both notes `readOnly`, *"Revit indents a paragraph 7 level(s) at most"* - **N is 7** on Revit 2020. Note B was `absent` in the first two (it has no paragraph 4) |
| **FD7** | On Note A: text `lowercase letters 2-4`, then `uppercase letters 2-4`, then `no list`, then `italic last`, then `not bold 1`, each rolled back | Each `changed`, read back as asked (*LowerCaseLetters*, *UpperCaseLetters*, *no list*, *italic 4*); `not bold 1` on a note with nothing bold is `untouched` - *"already as … asks - nothing written"*. **RUN 2026-10-17, Revit 2020, rolled back - as looked for.** *"2-4 LowerCaseLetters from 1"*, *"2-4 UpperCaseLetters from 1"*; `no list` left Note A `untouched` (*"already as 'no list' asks - nothing written"*) and changed Note B to *"1-3 no list; bold 2; underline 3"*; `italic last` *"italic 4"* on Note A and *"bold 2; italic 3; underline 3"* on Note B; `not bold 1` left both `untouched`. Also `numbers` with no paragraphs named: both notes `changed`, *"1-4 ArabicNumbers from 1"* and *"1-3 ArabicNumbers from 1; bold 2; underline 3"*. Every one's negative, `numbers 2-9`, refused with `changed 0` |
| **FD8** | FD1 and FD2 once on Revit 2024 and once on 2027, same scratch project saved up | The same answers - one implementation for every release |
| **FD9** | *A wording edit keeps 1-2-3.* On an 8-paragraph note numbered 1-8 (paragraph 3 holding two Shift+Enter breaks and a curly apostrophe, `PROVIDE` and the first `DETAIL A` bold, paragraph 5 underlined, 6 italic) beside a 4-paragraph bulleted note with paragraphs 2-3 at indent 1: mode `replace`, text `DETAIL A`, replacement `DETAIL B`; then mode `set` with eight new paragraphs typed with `\r` and `\v`; negatives `replace ZZZNOSUCH` and `set` with no text; each rolled back | The numbered note `changed` and read back *"1-8 ArabicNumbers from 1"* both times; the bulleted note `untouched` by the replace. **RUN 2026-10-17, Revit 2020, rolled back - as looked for.** Replace: *"note 1474104: 8 paragraph(s) before, 8 after; read back 1-8 ArabicNumbers from 1; indent and text style kept"*, the bulleted note `untouched`. Set: *"read back 1-8 ArabicNumbers from 1; indent and text style given back paragraph by paragraph; NOT KEPT: paragraph 1 was partly bold and is now not bold; paragraph 4 was partly bold and is now not bold"* - the partly bold paragraphs written plain and said so, as version 4 designed; the bulleted note, 4 paragraphs made 8, read back *"1 Bullet, 2-3 Bullet indent 1, 4-8 Bullet"*. Negatives: `untouched 2`, and *"no text was given ... NOTHING WAS CHANGED"*. Not a proof draft - the evidence that version 5 kept version 4's behaviour |
| **FD10** | *The signatures, then PROVEN - Ajmal PS only.* In this branch's worktree, read `brain/proof-drafts/edit-text-values.yaml` and `read-element-parameters.yaml` (copies, their run records and every FD5-FD9 record are in `%TEMP%\heron-work\fd-295356\keep`), then `python brain/heron_validate.py accept edit-text-values --by "Ajmal PS"` and the same for `read-element-parameters`; then `heron-status: PROVEN` on both cards | Each card's `proof:` becomes the 2026-10-17 run on *Heron Note Format Test*, Revit 2020, fingerprints `83f7bbbd82e2acee` and `9f2e553abbfabdd2`, and `tools/check-signatures.py` lists neither as STALE. **FAIL:** `accept` calls a draft stale - the code moved after the run; run FD1-FD3 again |
