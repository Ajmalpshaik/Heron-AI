# NEEDS-CHECKING archive — Group FD

> **Checks that were done, moved out of the live register.** These are rows of Group FD of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a text note's numbering, bullets, indent and bold set through Heron: `EDIT_TEXT_VALUES` mode format, and the read that checks it (2026-10-17)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row FD1

*Moved from the register on 2026-11-17.*

**Run.** `python tools/batch-prove.py tools/jobs/note-format-2026-10-17.yaml` - the edit leg: both notes selected, field `Text`, mode `format`, text `numbers 2-4; bold 1`, replacement empty, rolled back

**Look for.** **The D-30 positive.** `changed` holds Note A only, and `noteFormatting` reads back *"1 no list, 2-4 ArabicNumbers from 1; bold 1"*, words unchanged. Note B is in `absent` and its line says it *"has 3 paragraph(s), so 'numbers 2-4' names paragraph 4, which is not there - it was NOT edited"*. Afterwards both notes on the plan read exactly as typed (the run was rolled back). **RUN 2026-10-17, Revit 2020, rolled back - as looked for.** `changed 1 item(s) [1474096]`, `absent 1 item(s) [1474097]`, `noteFormatting` *"note 1474096: 'numbers 2-4; bold 1' - 4 paragraph(s), words unchanged; read back 1 no list, 2-4 ArabicNumbers from 1; bold 1 ## note 1474097 has 3 paragraph(s), so 'numbers 2-4' names paragraph 4, which is not there - it was NOT edited"*. The proof draft's positive - **Struck 2026-10-18:** run as looked for, and signed with FD10 - `edit-text-values` and `read-element-parameters` PROVEN.

---

### Row FD2

*Moved from the register on 2026-11-17.*

**Run.** The same job's negative leg: text `numbers 2-5`

**Look for.** **The D-30 negative.** REFUSED - *"the paragraphs named are not there in 2 of the 2 element(s) handed in, so NOTHING WAS CHANGED"* - and `changed` empty. Both notes as typed. **RUN 2026-10-17, Revit 2020, rolled back - as looked for.** `changed 0 item(s)`, `refused` *"the paragraphs named are not there in 2 of the 2 element(s) handed in, so NOTHING WAS CHANGED"*, each note named with 4 and 3 paragraphs. The proof draft's negative - **Struck 2026-10-18:** run as looked for, and signed with FD10 - `edit-text-values` and `read-element-parameters` PROVEN.

---

### Row FD3

*Moved from the register on 2026-11-17.*

**Run.** The job's read leg: `READ_ELEMENT_PARAMETERS` parameterName `Text` on the same two notes; negative `ZZZNOTHINGHERE`

**Look for.** Note A's `noteWording` line says *"no list"* and nothing about bold; Note B's ends *"lists: 1-3 Bullet \|\| bold 2; underline 3"*. Negative: both `absent`, `noteWording` null. **RUN 2026-10-17, Revit 2020, rolled back - as looked for.** Note A *"... SEE DETAIL A \|\| no list"*, Note B *"FIRST POINT\rSECOND POINT\rTHIRD POINT \|\| lists: 1-3 Bullet \|\| bold 2; underline 3"*, `values 2 entry(ies)`; negative `absent 2 item(s)`, `values 0 entry(ies)`, `noteWording (null)`. The read's proof draft. Also run on the 8-paragraph 1-2-3 note of FD9: *"lists: 1-8 ArabicNumbers from 1 \|\| partly bold 1, 4; italic 6; underline 5 \|\| not typed plainly: ’ (U+2019)"* - **Struck 2026-10-18:** run as looked for, and signed with FD10 - `edit-text-values` and `read-element-parameters` PROVEN.

---

### Row FD6

*Moved from the register on 2026-11-17.*

**Run.** On Note A: text `start 3 2-4` (no list there yet), then text `numbers 2-4; start 3 2-4`, then `indent 9 2`, each rolled back

**Look for.** First: `readOnly`, NOT edited, *"paragraph 2 is no list, and only numbers or letters have a start"*. Second: `changed`, read back *"2-4 ArabicNumbers from 3"* - the plan would show 3. 4. 5. Third: `readOnly`, *"Revit indents a paragraph N level(s) at most"* - write down N. **RUN 2026-10-17, Revit 2020, rolled back - as looked for.** First: `readOnly` *"NOT edited - 'start 3 2-4': paragraph 2 is no list, and only numbers or letters have a start"*. Second: `changed`, *"2-4 ArabicNumbers from 3"*. Third: both notes `readOnly`, *"Revit indents a paragraph 7 level(s) at most"* - **N is 7** on Revit 2020. Note B was `absent` in the first two (it has no paragraph 4) - **Struck 2026-10-18:** run as looked for, and signed with FD10 - `edit-text-values` and `read-element-parameters` PROVEN.

---

### Row FD7

*Moved from the register on 2026-11-17.*

**Run.** On Note A: text `lowercase letters 2-4`, then `uppercase letters 2-4`, then `no list`, then `italic last`, then `not bold 1`, each rolled back

**Look for.** Each `changed`, read back as asked (*LowerCaseLetters*, *UpperCaseLetters*, *no list*, *italic 4*); `not bold 1` on a note with nothing bold is `untouched` - *"already as … asks - nothing written"*. **RUN 2026-10-17, Revit 2020, rolled back - as looked for.** *"2-4 LowerCaseLetters from 1"*, *"2-4 UpperCaseLetters from 1"*; `no list` left Note A `untouched` (*"already as 'no list' asks - nothing written"*) and changed Note B to *"1-3 no list; bold 2; underline 3"*; `italic last` *"italic 4"* on Note A and *"bold 2; italic 3; underline 3"* on Note B; `not bold 1` left both `untouched`. Also `numbers` with no paragraphs named: both notes `changed`, *"1-4 ArabicNumbers from 1"* and *"1-3 ArabicNumbers from 1; bold 2; underline 3"*. Every one's negative, `numbers 2-9`, refused with `changed 0` - **Struck 2026-10-18:** run as looked for, and signed with FD10 - `edit-text-values` and `read-element-parameters` PROVEN.

---

### Row FD9

*Moved from the register on 2026-11-17.*

**Run.** *A wording edit keeps 1-2-3.* On an 8-paragraph note numbered 1-8 (paragraph 3 holding two Shift+Enter breaks and a curly apostrophe, `PROVIDE` and the first `DETAIL A` bold, paragraph 5 underlined, 6 italic) beside a 4-paragraph bulleted note with paragraphs 2-3 at indent 1: mode `replace`, text `DETAIL A`, replacement `DETAIL B`; then mode `set` with eight new paragraphs typed with `\r` and `\v`; negatives `replace ZZZNOSUCH` and `set` with no text; each rolled back

**Look for.** The numbered note `changed` and read back *"1-8 ArabicNumbers from 1"* both times; the bulleted note `untouched` by the replace. **RUN 2026-10-17, Revit 2020, rolled back - as looked for.** Replace: *"note 1474104: 8 paragraph(s) before, 8 after; read back 1-8 ArabicNumbers from 1; indent and text style kept"*, the bulleted note `untouched`. Set: *"read back 1-8 ArabicNumbers from 1; indent and text style given back paragraph by paragraph; NOT KEPT: paragraph 1 was partly bold and is now not bold; paragraph 4 was partly bold and is now not bold"* - the partly bold paragraphs written plain and said so, as version 4 designed; the bulleted note, 4 paragraphs made 8, read back *"1 Bullet, 2-3 Bullet indent 1, 4-8 Bullet"*. Negatives: `untouched 2`, and *"no text was given ... NOTHING WAS CHANGED"*. Not a proof draft - the evidence that version 5 kept version 4's behaviour - **Struck 2026-10-18:** run as looked for, and signed with FD10 - `edit-text-values` and `read-element-parameters` PROVEN.

---

### Row FD10

*Moved from the register on 2026-11-17.*

**Run.** **DONE 2026-10-18** - both drafts accepted by Ajmal PS in his own terminal, fingerprints `83f7bbbd82e2acee` and `9f2e553abbfabdd2`, and both cards set PROVEN by him. *The signatures, then PROVEN - Ajmal PS only.* In this branch's worktree, read `brain/proof-drafts/edit-text-values.yaml` and `read-element-parameters.yaml` (copies, their run records and every FD5-FD9 record are in `%TEMP%\heron-work\fd-295356\keep`), then `python brain/heron_validate.py accept edit-text-values --by "Ajmal PS"` and the same for `read-element-parameters`; then `heron-status: PROVEN` on both cards

**Look for.** Each card's `proof:` becomes the 2026-10-17 run on *Heron Note Format Test*, Revit 2020, fingerprints `83f7bbbd82e2acee` and `9f2e553abbfabdd2`, and `tools/check-signatures.py` lists neither as STALE. **FAIL:** `accept` calls a draft stale - the code moved after the run; run FD1-FD3 again

---
