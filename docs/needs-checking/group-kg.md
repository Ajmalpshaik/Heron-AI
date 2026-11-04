# Needs checking — Group KG

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KG - duct and pipe placeholders, drawn and converted: `CREATE_MEP_PLACEHOLDER`, `CONVERT_MEP_PLACEHOLDERS` (2026-11-01)

Asked for in the modeller's words: *"draw a duct placeholder from here to here"*, *"rough in the pipe
route as placeholders"*, *"convert these placeholders to real ducts"*, *"turn the pipe placeholders into
pipes"* - Revit's Systems > Duct Placeholder and Pipe Placeholder, and Modify > Convert Placeholder.
Nothing in Heron drew or converted a placeholder. `brain/heron_retrieve.py` on each sentence, Revit 2024,
offered `CREATE_DUCT`, `CREATE_FLEX_PIPE`, `AUTO_SIZE_MEP` and `SET_MEP_SYSTEM_TYPE`.

| What | Where | What it does |
|---|---|---|
| `CREATE_MEP_PLACEHOLDER` (MODIFY) | [`brain/fragments/create-mep-placeholder`](../../brain/fragments/create-mep-placeholder/fragment.yaml) | Draws duct or pipe placeholders between consecutive points - the same run of points as `CREATE_DUCT` - on the level named, of the system type and duct or pipe type the modeller **names**. The system type decides duct or pipe. **Either one left out, or not in the model: nothing is drawn** and the loaded system types and duct and pipe types are listed. A duct type with a piping system, or the other way round, is refused by name. A segment too short is counted in `tooShort`; one Revit refuses is named in `notMade` with Revit's words and the rest are still drawn. Each one made is read back as a placeholder, with its length |
| `CONVERT_MEP_PLACEHOLDERS` (MODIFY) | [`brain/fragments/convert-mep-placeholders`](../../brain/fragments/convert-mep-placeholders/fragment.yaml) | Turns the duct and pipe placeholders selected - or `CREATE_MEP_PLACEHOLDER`'s `elements` - into real ducts and pipes, one Revit call per discipline. Anything that is not a placeholder - a real duct, a fitting, a wall - is named in `notPlaceholders` and left alone; a placeholder borrowed by somebody else is named in `notConverted` and never taken. Read back: the real ducts and pipes with their lengths, any fittings Revit returned, and any placeholder still a placeholder |

**Why two new cards and not a widening.** `CREATE_DUCT` and `CREATE_PIPE` are PROVEN, so any change
under their `impl/` would make their proofs stale. A placeholder is also a different element: it is
single-line, has no fittings, and is what Convert Placeholder later turns into duct. Drawing and
converting are two jobs: one makes new elements from points, the other changes existing elements from
a selection and needs no type or point. Duct and pipe share each card, as tray and conduit share
`CREATE_ELECTRICAL_RUN`, and which one is wanted is never guessed. Neither neighbouring card was edited.

**What is done and what is not.** Both compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of
all eight with `tools/api-surface --members`: `Duct.CreatePlaceholder` and `Pipe.CreatePlaceholder`
(Document, three ElementIds, two XYZ), `MechanicalUtils.ConvertDuctPlaceholders` and
`PlumbingUtils.ConvertPipePlaceholders` (Document, `ICollection<ElementId>`, returning
`ICollection<ElementId>`), `Duct.IsPlaceholder`, `Pipe.IsPlaceholder` and
`WorksharingUtils.GetCheckoutStatus(Document, ElementId, out string)` are present and unchanged
2020-2027. **`IsPlaceholder` is declared on `Duct` and on `Pipe`, not on `MEPCurve`**, so both cards ask
each class. **Nothing here has run in Revit.** Both cards are DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (KG5 measures them): whether
Revit's convert keeps a placeholder's id or deletes it and makes a new duct; whether the collection it
returns holds only the new ducts or pipes, or their fittings too; and whether two placeholders that meet
get an elbow when converted together. The card does not rely on any of these. It sorts every id
returned by what it is, and looks again at every placeholder it handed in.

**The model - the Project2 scratch model in Revit 2020** (Revit's Systems template, never saved), the
one the MEP batches of 2026-10-18 ran on: Supply Air, `Rectangular Duct: Default`, Domestic Cold Water and
Level 1 are known there. **The pipe type's name is not known here.** Every run rolled back. The job file is
[`tools/jobs/mep-placeholders-kg-2026-11-01.yaml`](../../tools/jobs/mep-placeholders-kg-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KG0** | *Open.* The Project2 scratch model open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon | The title bar reads Project2 |
| **KG1** | *Read the types - also the "none named" negative.* `create-mep-placeholder` with `--write`, `systemType` Domestic Cold Water, `level` Level 1, two points, `ductOrPipeType` left out | `elements` empty, *No pipe type was named - Heron never chooses one*, `typesLoaded` listing the piping system types and pipe types. Write the pipe type into KG3 |
| **KG2** | *Duct placeholders, the D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's first job | Negative: the same point twice - `elements` empty, `tooShort` 1, no exception. Positive: `elements` 2 duct placeholders, 3000 mm each in `report`, no *reads back as a real duct* |
| **KG3** | *Pipe placeholders and the mismatch.* `systemType` Domestic Cold Water with KG1's pipe type, points `1000,8000,2700; 5000,8000,2700`; then `systemType` Supply Air with that pipe type | First: one pipe placeholder, 4000 mm. Second: nothing drawn, refused *... is a pipe type and ... is a duct system type* |
| **KG4** | *Arrange the convert.* By hand with Revit's own Duct Placeholder and Pipe Placeholder tools, in the strip 0,14000 to 6000,16000 mm at 2700 mm on Level 1: P1 and P2, Supply Air duct placeholders meeting at 3000,15000; P3, a Domestic Cold Water pipe placeholder; R1, one real Supply Air duct | Four elements in the strip; `select-in-region` over it with `Duct Placeholders, Pipe Placeholders, Ducts` selects 4 |
| **KG5** | *Convert, the D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's second job | Negative: R1 alone - `converted` empty, *None of the 1 element(s) given is a duct or pipe placeholder*, R1 unchanged. Positive: `converted` holds the ducts and pipe where P1 to P3 were, with lengths; `notPlaceholders` names R1 *already a real duct*. **Record** whether P1 kept its id, and whether `fittingsMade` holds an elbow at 3000,15000 |
| **KG6** | *Kept, through `try-once`, on the modeller's yes.* KG2's positive on a saved copy, a 3D view in front | Keep or Discard. Then Revit's own Duct Placeholder between the same points gives the same length - the second route - and one Ctrl+Z takes both placeholders back |
| **KG7** | *Revit 2024 or later.* KG2 and KG5 again | The same answers. Nothing either card calls changed between releases |
| **KG8** | *Routing, on the owner's PC.* The routing paragraph below, measured again on `model+rerank` | The four sentences reach these cards by identity. Any sentence the re-ranker moves to either card from another is a row of its own |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept <fragment> --by "Ajmal
PS"` for each once its positive and negative pass, and `heron-status` set to PROVEN in the same change. A
row that fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**Routing, measured 2026-11-01 in a cloud container on its `model` backend, re-ranker absent** - the
owner's PC ranks with `model+rerank`, which is **NOT RUN** here - through `brain/heron_retrieve.py` and
`tools/check-routing.py` on a scratch store each, against this tree at `da81e0a` with and without the two
cards. Each of the four sentences reaches its card by identity on 2024. Every declared sentence of
`CREATE_DUCT` (6) and `CREATE_PIPE` (7) still reaches its own card. So do *"draw all the ducts"* (still
`CREATE_DUCT_RUNS`), *"put the fittings in"* (still `FIT_MEP_JOINTS`) and *"change these ducts to return
air"* (still `SET_MEP_SYSTEM_TYPE`). Undeclared phrasings: *"draw a duct from here to here"* and *"draw a
pipe from here to here"* still rank `CREATE_DUCT` and `CREATE_PIPE` first, with `CREATE_MEP_PLACEHOLDER`
second. *"rough in the duct route"*, *"convert these to real ducts"* and *"turn these into pipes"* now rank
the new cards first. `tools/check-routing.py` found no risk-ladder crossing either time. *CLAIMED IN A
ROUTING TABLE AND NOT REACHED* was 67 both times; one of those lines changed its winner between two other
cards: *"draw a curtain wall"* (`CREATE_WALL` claims it), served by `REPORT_CURTAIN_WALL_TYPE` before and
`REPORT_CURTAIN_ELEMENTS` after, neither of them new and both reads. *SENTENCES TWO FRAGMENTS BOTH WANT* went from 703 of 3760
utterances to 698 of 3764. No line in either report names FRG-MEP-074 or FRG-MEP-075 as the answer.
**No claimed sentence moved to either card**, so no row was owed.
