# Needs checking — Group FD

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group FD - a text note's numbering, bullets, indent and bold set through Heron: `EDIT_TEXT_VALUES` mode format, and the read that checks it (2026-10-17)

**Tracked as GitHub issue [#985](https://github.com/Ajmalpshaik/Heron-AI/issues/985) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

[FRAGMENT-ISSUES 5b-737](../fragment-issues/section-5b-rows-726-750.md). `EDIT_TEXT_VALUES` version 5 adds
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
`read-element-parameters` from FD3, fingerprint `9f2e553abbfabdd2` - **both signed by Ajmal PS and
PROVEN on 2026-10-18 (FD10).** **Not run:** FD4 (a kept run - the owner's choice), FD8 (Revit 2024 and 2027), and a note typed by
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
and a setup step that makes them runs after every read step ([row 5b-520](../fragment-issues/section-5b-rows-501-525.md)).
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
| ~~**FD1**~~ | `python tools/batch-prove.py tools/jobs/note-format-2026-10-17.yaml` - the edit leg: both notes… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | **The D-30 positive.** |
| ~~**FD2**~~ | The same job's negative leg: text `numbers 2-5` — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | **The D-30 negative.** |
| ~~**FD3**~~ | The job's read leg: `READ_ELEMENT_PARAMETERS` parameterName `Text` on the same two notes; negative… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | Note A's `noteWording` line says "no list" and nothing about bold; Note B's ends "lists… |
| **FD4** | *The one kept run, only if you choose to.* In the chat: *"number paragraphs 2 to 4 of this note and make the first line bold"* with Note A selected. Read Revit's Keep or Discard box, look at the note, choose **Keep** | The note on the plan shows `GENERAL NOTES` in bold and `1. ALL DIMENSIONS…` `2. DO NOT SCALE…` `3. SEE DETAIL A` - one undo entry. Then ask *"is this note numbered"*: it must answer through `READ_ELEMENT_PARAMETERS` and say *"2-4 ArabicNumbers from 1 \|\| bold 1"*. Write down whether the numbers 1. 2. 3. look bold too - Heron bolds the whole paragraph, closing carriage return included, and Autodesk says that is what a list number takes its bold from |
| ~~**FD5**~~ | On Note B (bullets): text `numbers 2-3`, replacement empty, rolled back | *An observation, not a proof.* Autodesk's reference says changing one paragraph of a list changes the whole list. Write down which happened: paragraph 1 renumbered with 2-3 (then the note must be in `unverified` and `noteFormatting` must say *"paragraph 1 was not named and reads back ArabicNumbers"*), or paragraph 1 left a bullet and the note `changed`. **RUN 2026-10-17, Revit 2020, rolled back - the WHOLE LIST changed, as Autodesk's reference says.** Note B in `unverified`, *"the words are as they were but the formatting READS BACK DIFFERENT from 'numbers 2-3' - paragraph 1 was not named and reads back ArabicNumbers, not Bullet - Revit gives every paragraph of one list the same list type"*; Note A `changed`, *"1 no list, 2-3 ArabicNumbers from 1, 4 no list"*. Kept, that note would be numbered 1-3 and reported unverified, never changed - **Struck 2026-10-18:** run as looked for, and signed with FD10 - `edit-text-values` and `read-element-parameters` PROVEN. |
| ~~**FD6**~~ | On Note A: text `start 3 2-4` (no list there yet), then text `numbers 2-4; start 3 2-4`, then… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | First: `readOnly`, NOT edited, "paragraph 2 is no list, and only numbers or letters have… |
| ~~**FD7**~~ | On Note A: text lowercase letters 2-4, then uppercase letters 2-4, then no list, then italic… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | Each `changed`, read back as asked (LowerCaseLetters, UpperCaseLetters, no list, italic… |
| **FD8** | FD1 and FD2 once on Revit 2024 and once on 2027, same scratch project saved up | The same answers - one implementation for every release |
| ~~**FD9**~~ | A wording edit keeps 1-2-3. On an 8-paragraph note numbered 1-8 (paragraph 3 holding two… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | The numbered note `changed` and read back "1-8 ArabicNumbers from 1" both times; the… |
| ~~**FD10**~~ | DONE 2026-10-18 — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | Each card's `proof:` becomes the 2026-10-17 run on Heron Note Format Test, Revit 2020… |
