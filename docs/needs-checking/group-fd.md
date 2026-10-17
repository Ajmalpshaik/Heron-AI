# Needs checking — Group FD

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group FD - a text note's numbering, bullets, indent and bold set through Heron: `EDIT_TEXT_VALUES` mode format, and the read that checks it (2026-10-17)

[FRAGMENT-ISSUES 5b-630](../fragment-issues/section-5b-rows-401-425.md). `EDIT_TEXT_VALUES` version 5 adds
mode **format**: on a text note, it sets numbers, bullets, lowercase or uppercase letters or no list, a
list's start number, an indent, and bold, italic or underline - on every paragraph or on the paragraphs
named - and changes no word. `READ_ELEMENT_PARAMETERS` version 4 reads it back: each paragraph's list, each
list's start number, and which paragraphs are bold, italic or underlined.

**What is compiled and what is not run.** Both cards compile for all eight releases 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-17), and every Revit member they call reads the same in each
release's RevitAPI (`tools/api-surface`, one release at a time). **None of it has run in Revit.** Both cards
are DRAFT.

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
| **FD1** | `python tools/batch-prove.py tools/jobs/note-format-2026-10-17.yaml` - the edit leg: both notes selected, field `Text`, mode `format`, text `numbers 2-4; bold 1`, replacement empty, rolled back | **The D-30 positive.** `changed` holds Note A only, and `noteFormatting` reads back *"1 no list, 2-4 ArabicNumbers from 1; bold 1"*, words unchanged. Note B is in `absent` and its line says it *"has 3 paragraph(s), so 'numbers 2-4' names paragraph 4, which is not there - it was NOT edited"*. Afterwards both notes on the plan read exactly as typed (the run was rolled back) |
| **FD2** | The same job's negative leg: text `numbers 2-5` | **The D-30 negative.** REFUSED - *"the paragraphs named are not there in 2 of the 2 element(s) handed in, so NOTHING WAS CHANGED"* - and `changed` empty. Both notes as typed |
| **FD3** | The job's read leg: `READ_ELEMENT_PARAMETERS` parameterName `Text` on the same two notes; negative `ZZZNOTHINGHERE` | Note A's `noteWording` line says *"no list"* and nothing about bold; Note B's ends *"lists: 1-3 Bullet \|\| bold 2; underline 3"*. Negative: both `absent`, `noteWording` null |
| **FD4** | *The one kept run, only if you choose to.* In the chat: *"number paragraphs 2 to 4 of this note and make the first line bold"* with Note A selected. Read Revit's Keep or Discard box, look at the note, choose **Keep** | The note on the plan shows `GENERAL NOTES` in bold and `1. ALL DIMENSIONS…` `2. DO NOT SCALE…` `3. SEE DETAIL A` - one undo entry. Then ask *"is this note numbered"*: it must answer through `READ_ELEMENT_PARAMETERS` and say *"2-4 ArabicNumbers from 1 \|\| bold 1"*. Write down whether the numbers 1. 2. 3. look bold too - Heron bolds the whole paragraph, closing carriage return included, and Autodesk says that is what a list number takes its bold from |
| **FD5** | On Note B (bullets): text `numbers 2-3`, replacement empty, rolled back | *An observation, not a proof.* Autodesk's reference says changing one paragraph of a list changes the whole list. Write down which happened: paragraph 1 renumbered with 2-3 (then the note must be in `unverified` and `noteFormatting` must say *"paragraph 1 was not named and reads back ArabicNumbers"*), or paragraph 1 left a bullet and the note `changed` |
| **FD6** | On Note A: text `start 3 2-4` (no list there yet), then text `numbers 2-4; start 3 2-4`, then `indent 9 2`, each rolled back | First: `readOnly`, NOT edited, *"paragraph 2 is no list, and only numbers or letters have a start"*. Second: `changed`, read back *"2-4 ArabicNumbers from 3"* - the plan would show 3. 4. 5. Third: `readOnly`, *"Revit indents a paragraph N level(s) at most"* - write down N |
| **FD7** | On Note A: text `lowercase letters 2-4`, then `uppercase letters 2-4`, then `no list`, then `italic last`, then `not bold 1`, each rolled back | Each `changed`, read back as asked (*LowerCaseLetters*, *UpperCaseLetters*, *no list*, *italic 4*); `not bold 1` on a note with nothing bold is `untouched` - *"already as … asks - nothing written"* |
| **FD8** | FD1 and FD2 once on Revit 2024 and once on 2027, same scratch project saved up | The same answers - one implementation for every release |
