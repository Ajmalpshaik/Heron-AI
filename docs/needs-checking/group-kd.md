# Needs checking — Group KD

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KD - columns attached to a selected target: `ATTACH_COLUMNS` (2026-11-01)

Asked for in the modeller's words: *"attach these columns to the roof"*, *"attach the column tops to the
beams above"*, *"attach column base to the footing"*, *"detach these columns"* - Revit's Modify Column >
Attach Top/Base and Detach Top/Base. Nothing in Heron did it for a column: `ATTACH_WALLS` attaches walls,
on Revit 2025 on, and `SET_WALL_CONSTRAINTS` and `PLACE_STRUCTURAL_FAMILY` attach nothing.
`brain/heron_retrieve.py` on each sentence, Revit 2024, offered `PLACE_COLUMN_REBAR`, `COPE_FRAMING` and
`PLACE_STRUCTURAL_FAMILY` first.

| What | Where | What it does |
|---|---|---|
| `ATTACH_COLUMNS` (MODIFY) | [`brain/fragments/attach-columns`](../../brain/fragments/attach-columns/fragment.yaml) | Attaches the top or base of each structural or architectural column to the roof, floor, ceiling, beam or footing **selected with the columns**, or detaches it. Revit says which selected elements are columns; the rest are targets. With no target selected nothing is attached; a target Revit accepts for none of the columns is refused by name and nothing changes. Each column takes the one selected target its plan extent meets - two meeting it, or none, and it is named and left. A column already attached to that target is left alone; one attached at that end to something else is moved, and both are named. Cut style, justification and offset (mm) are Revit's Options bar values, left out as Cut Column, Minimum and 0. Each change read back after a regenerate |

**Why a new card and not a widening.** `ATTACH_WALLS` is DRAFT and could have been widened, but its call,
`Wall.AddAttachment`, exists only from Revit 2025, where `ColumnAttachment` is on every release 2020 to
2027. It also finds each wall's target itself, by nearness, and a wall attachment has no cut style,
justification or offset. Its `impl/` is untouched; its card gained routing comment lines only.

**Why the target is in the selection.** A separate `target` need was the first shape tried. The add-in
fills an `Element` need from *"selected"* only when exactly one element is selected, and the columns are
the selection, so that shape could not be supplied with the columns selected at the same time.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of
all eight with `tools/api-surface --members`: `ColumnAttachment.AddColumnAttachment(Document,
FamilyInstance, Element, int, ColumnAttachmentCutStyle, ColumnAttachmentJustification, double)`,
`RemoveColumnAttachment(FamilyInstance, int)`, `GetColumnAttachment(FamilyInstance, int)`,
`IsValidColumn`, `IsValidTarget(FamilyInstance, Element)`, `TargetId`, both enums and `SubTransaction` are
present and unchanged 2020-2027. **Nothing here has run in Revit.** The card is DRAFT; its risk is MODIFY -
it changes columns' attachments and deletes nothing.

**Not known from the assemblies, and recorded here rather than guessed** (KD2 measures them): that
`baseOrTop` is 0 for the base and 1 for the top - the wrong way round shows at once in a section; whether
Revit accepts a second attachment at an end that already has one, which is why the fragment removes the
first inside the same SubTransaction and puts it back if the new one is refused; and whether a beam cut
back to a column's face still meets the column in plan within the 10 mm the fragment allows.

**The model - KD Structure in Revit 2020**, a scratch copy of Revit 2020's
`rst_advanced_sample_project.rvt` in `%TEMP%\heron-work\columns-kd`. **Which of its views shows columns
and the one floor over them is not known here.** Every run rolled back. The job file is
[`tools/jobs/attach-columns-kd-2026-11-01.yaml`](../../tools/jobs/attach-columns-kd-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). **Its view is left for KD1 to read** and is marked in the
file; until it is filled in the run should stop at the setup - expected, NOT RUN. Every line below with
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KD0** | *Open.* KD Structure open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon | The title bar reads the scratch copy's name |
| **KD1** | *Read the arrangement.* A view showing fewer than twenty structural columns and exactly one floor (or roof) over them, and at least one door; the columns not attached at the top (Modify Column shows Detach Top/Base greyed) | Write the view into the job file, both places; Roofs for Floors if it is a roof. **If no view shows a door, name another category Revit will refuse that the view does show** |
| **KD2** | *Attach, the D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's job | Negative: the doors as target - `changed` 0, the doors named as not accepted, *NOTHING WAS CHANGED*. Positive: `changed` the number of columns, each *top attached to Floors <id>*; read in the same rolled-back group, a section shows the tops at the floor's underside. **Record** whether the top or the base moved - that settles `baseOrTop` |
| **KD3** | *No target, and "both" on an attach.* KD2's columns alone; then the positive's selection with `where=both` | `changed` 0 both times - *No target was selected*; *Attach takes one end at a time* |
| **KD4** | *Two beams over one column.* One column and two beams framing into its top, the three selected | `changed` 0; the column in `left`, the report naming both beams |
| **KD5** | *A second run, and a detach.* KD2's positive, then the same selection again, then the columns with `detach=true`, all in one rolled-back group | Second run: `changed` 0, every column in `alreadyDone`. Detach: each *top detached from Floors <id>* |
| **KD6** | *Kept, through `try-once`, on the modeller's yes.* KD2's positive on a saved copy, a section through the columns in front | Keep or Discard; afterwards Revit's own Modify Column shows Detach Top/Base available on the same columns - the second route - and one Ctrl+Z detaches them all |
| **KD7** | *Routing, on the owner's PC.* The routing paragraph below, measured again on `model+rerank` | The four sentences reach this card by identity. Any sentence the re-ranker moves to this card from another is a row of its own |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept attach-columns --by "Ajmal
PS"` once its positive and negative pass, and `heron-status` set to PROVEN in the same change. A row that
fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**Routing, measured 2026-11-01 in a cloud container on its `model` backend, re-ranker absent** - the
owner's PC ranks with `model+rerank`, which is **NOT RUN** here - through `brain/heron_retrieve.py` and
`tools/check-routing.py` on a scratch store each, against this tree with and without the card. The four
sentences reach this card by identity on 2020, 2024 and 2025; without it they reached
`PLACE_COLUMN_REBAR`, `COPE_FRAMING` and `PLACE_STRUCTURAL_FAMILY`, ranked. Three undeclared phrasings -
*"attach the columns to the floor above"*, *"detach the column tops"* and *"attach top of the columns to
the slab"* - reach this card ranked. `tools/check-routing.py`: no question answered by a write; *CLAIMED IN
A ROUTING TABLE AND NOT REACHED* 65 both times; *SENTENCES TWO FRAGMENTS BOTH WANT* 696 before, 695 after -
none added, and `PLACE_FAMILY_INSTANCES`'s *"place a diffuser"* no longer on it. **No claimed sentence
moved to this card**, so no row was owed. `ATTACH_WALLS`'s five sentences still reach it by identity on
2025. **On 2020 to 2024, where `ATTACH_WALLS` is not offered**, four of them now rank this card first;
before it they ranked `PLACE_HOSTED_FAMILY`, `PLACE_AREA_REINFORCEMENT`, `PAINT_FACES` and
`CREATE_FROM_ROOM_BOUNDARIES` - none of which attaches a wall, and none of which claims them. Handed walls
this card changes nothing and says none of them is a column. Both cards say so in their routing comments.

**Written back 2026-11-12, read in a cloud container (no Revit) - nothing was run here.** Since this group was
written: `ATTACH_WALLS` reads PROVEN on its card, at version 1 - its `proof` block is signed by Ajmal PS, 2026-11-07,
on Attach Walls Scratch 2026 (3,332 elements), Revit 2026, session 35148. So where this page calls a card named here
DRAFT, or says nothing has run in Revit, that was true when it was written and is out of date as a statement about the
card. It is a card this group names in passing, not the one its rows prove.
