# Needs checking — Group HE

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HE - a new text note of several paragraphs, numbered, lettered or bulleted: `CREATE_TEXT_NOTE` version 2 (2026-10-17)

Found from [row 5b-520](../fragment-issues/section-5b-rows-401-425.md): `EDIT_TEXT_VALUES` version 4 learned to
keep a note's 1-2-3 numbering, but the tool that MAKES a note could still make only one paragraph with no
list - a chat's values arrive one per line, so a paragraph break could not even be typed. A GENERAL NOTES
block had to be made by hand. [Row 5b-930](../fragment-issues/section-5b-rows-401-425.md) has the finding.

**What changed.** [`CREATE_TEXT_NOTE`](../../brain/fragments/create-text-note/fragment.yaml) (FRG-VIEW-022, was
PROVEN, now DRAFT, version 2). `noteText` takes `\r` for a new paragraph (Enter), `\v` for a soft line break
(Shift+Enter) and `\\` for a backslash - the marks `EDIT_TEXT_VALUES` takes and `READ_ELEMENT_PARAMETERS`
shows. Two new inputs, both may be left out (`optional: true` with `absent:`, the mechanism from #545):
`listType` - None, Bullet, ArabicNumbers, LowerCaseLetters, UpperCaseLetters (or no list, bullets, numbers,
lowercase letters, uppercase letters), one for every paragraph or one per paragraph with commas between, the
last carrying on; left out, None. `listStartNumber` - the number the first numbered or lettered paragraph
takes; left out, 1. Refused before any note is made: a list word Revit has not got, more list words than
paragraphs, a start number that is not a whole number or is outside Revit's range, a start number on a note
with nothing numbered, and two different lists side by side (Autodesk's own rule: they would be one list).
Every paragraph's list is read back from the note; a note that reads back other than asked is in
`unverified`, never `created`. Two new outputs: `unverified` and `noteFormatting`. `EDIT_TEXT_VALUES` gains one declared sentence, *"turn this note into a numbered list"*, and no code: setting a list on a note that is already there is its version 5 mode `format` ([#586](https://github.com/Ajmalpshaik/Heron-AI/pull/586), Group FD).

**What is compiled, tested and not run.** Every fragment compiles on all eight releases 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-17). Every Revit member was read off RevitAPI.xml and the
shipped RevitAPI.dll for each release. The fragment's own code was run in a cloud container against
stand-in types with the same member names - not Revit: 18 cases, every refusal above and every read-back
line, all as written in [`tests/cases.yaml`](../../brain/fragments/create-text-note/tests/cases.yaml); with
two refusals deleted, their two cases went red. That proves the logic, not Revit's answer. **Nothing has run
in Revit.** The add-in must be main's from #545 on, which binds `absent:`; an older one refuses the call for
the missing `listType`.

**A NEW SCRATCH PROJECT, never a working model and never any 4355-* model.** Revit 2020 first: a project
from the default metric template, saved as *Heron Note List Test.rvt*. Nothing drawn - the Level 1 floor
plan and the template's "2.5mm Arial" text type are all it needs; press the Default 3D View button once
so that `{3D}` exists for HE7. With `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null` on every line. Each `validate
--write` row is ROLLED BACK: look at the Level 1 plan after each anyway - no note may be left.

The client lines below are `python mcp/client/heron_bridge_client.py validate --in "Heron Note List Test"
--write ... create-text-note`, shortened to `validate ...`. Every one also needs `--set "view=FloorPlan:
Level 1" --set "at=0,0,0" --set "textTypeId=2.5mm Arial"` and the same three as `--negative-set`. Type the
marks in SINGLE quotes, so the shell leaves the backslashes alone.

| # | Run | Look for |
|---|---|---|
| **HE1** | `python tools/batch-prove.py tools/jobs/create-text-note-lists-2026-10-17.yaml` - a heading kept out of a numbered list against a bullet straight after a number | Positive: `created` 1, `noteFormatting` *"4 paragraph(s); read back 1 no list, 2-4 ArabicNumbers from 1"*. Negative: `created` null, `unverified` null, `refused` naming *paragraphs 1 and 2*, NO NOTE WAS MADE. **The D-30 pair for paragraphs and a list per paragraph** |
| **HE2** | *The kept run, once.* `heron_bridge_client.py try-once --in "Heron Note List Test" create-text-note` with HE1's positive values, then **Keep**. Double-click the note | On screen: GENERAL NOTES with no number, then 1. 2. 3.; *"FROM THIS DRAWING"* on its own line inside item 2 with no number of its own. In the text editor the list button shows Numbers on paragraphs 2-4 and none on 1. **The second route** - what nothing rolled back can show |
| **HE3** | `READ_ELEMENT_PARAMETERS` parameterName Text on HE2's kept note | `noteWording` shows the four paragraphs with `\r`, the `\v` inside paragraph 3, and the lists *1 no list, 2-4 ArabicNumbers* - the read tool agreeing with the make tool's own read-back |
| **HE4** | One list for the whole note: `validate --set 'noteText=FIRST\rSECOND\rTHIRD' --set "listType=bullets" --set "listStartNumber=1" --negative-set 'noteText=FIRST\rSECOND\rTHIRD' --negative-set "listType=Numbered" --negative-set "listStartNumber=1"` | Positive: *"3 paragraph(s); read back 1-3 Bullet"*. Negative: `refused` names *'Numbered'* and the words Revit takes, nothing made |
| **HE5** | A start number: `validate --set 'noteText=FIRST\rSECOND\rTHIRD' --set "listType=lowercase letters" --set "listStartNumber=3" --negative-set 'noteText=ONE PARAGRAPH' --negative-set "listType=Bullet" --negative-set "listStartNumber=5"` | Positive: *"1-3 LowerCaseLetters from 3"*. Negative: `refused` - *a bullet list ... has no number to start from*, nothing made |
| **HE6** | *Revit's own range, an observation.* HE5's positive with `listStartNumber=100000`, then `0` | Each refused, nothing made; the refusal prints Revit's range *"(N to M)"* from GetMinimumListStartNumber and GetMaximumListStartNumber - **write both numbers down here**; nobody has read them off a running Revit |
| **HE7** | A version 1 caller: `validate --set "noteText=PROVIDE 300 CLEARANCE ABOVE"` and NO listType or listStartNumber; negative `--negative-set "view={3D}"` with the same text | Runs, never refused for a missing value; the binding note reads *listType not given - "None", the card's own default (optional)* and the same for listStartNumber, "1"; `created` 1, *"1 paragraph(s); read back 1 no list"* - version 1's note. Negative: *a text note is 2D annotation ...*, as version 1's proof |
| **HE8** | Paragraph marks alone: `validate --set 'noteText=A\rB\r' --set "listType=numbers" --set "listStartNumber=1" --negative-set 'noteText=\r\v' --negative-set "listType=None" --negative-set "listStartNumber=1"` | Positive: *"2 paragraph(s); read back 1-2 ArabicNumbers from 1"* - the closing `\r` makes no empty third paragraph. Negative: *Revit rejects an empty note*, nothing made |
| **HE9** | *Autodesk's rule behind HE1's negative, by hand, an observation.* In Revit's own text editor, a new two-paragraph note: Bullets on paragraph 1, then Numbers on paragraph 2 | Write down what Revit does - both numbered, both bulleted, or one of each. The refusal rests on SetListType's remarks (*"a list type ... changed ... is propagated to all the paragraphs in that list"*); if Revit keeps one of each, the refusal is wrong and goes to row 5b-930 |
| **HE10** | HE1, HE4, HE5 and HE7 on **Revit 2024** and **Revit 2027** | The same answers. Compiled for all eight releases here; what Revit DOES was measured on none |
| **HE11** | *Routing, on the owner's PC.* `heron_lookup` *"make a numbered note"*, *"put a bulleted note on the plan"*, *"turn this note into a numbered list"* | `CREATE_TEXT_NOTE` first for the first two, `EDIT_TEXT_VALUES` for the third (its mode format, #586). Measured in the cloud on the lexical and the trained backend, no re-ranker (row 5b-930); the re-ranked backends were not measured there. Also record what *"create a text note with a numbered list"* reaches: on lexical it went to `EDIT_TEXT_VALUES` because a DRAFT card loses PROVEN's tie-break |
| **HE12** | The signature, on the owner's word only, after HE1 to HE10 | `python brain/heron_validate.py accept create-text-note --by "Ajmal PS"`; then he says whether the card goes back to PROVEN |
