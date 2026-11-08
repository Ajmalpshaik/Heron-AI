# Needs checking — Group MW

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MW - a duct or pipe system drawn as a single-line schematic on a new drafting view, labelled with Revit's own size, flow and pressure drop: `DRAW_MEP_SCHEMATIC` (2026-11-08)

The owner's ask: [docs/48](../48-studying-bimlogiq.md) section 5 item 5, *"3D to 2D MEP schematic - one system
traced, drawn as single lines on a drafting view, with sizes, flow and pressure drop"*, and his "yes start item
5". Made NEW, at DRAFT. It overlaps `TRACE_CONNECTIVITY` (DRAFT), `REPORT_MEP_PRESSURE_DROP`,
`CREATE_DRAFTING_VIEW`, `CREATE_TEXT_NOTE` and `DRAW_FAMILY_DETAIL_LINES` (all PROVEN) - none was edited;
widening a PROVEN one would send it back to DRAFT and out of the chat's reach ([D-117](../DECISIONS.md)). The
overlap and the merge planned once this is proved are
[FRAGMENT-ISSUES row 5b-1479](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `DRAW_MEP_SCHEMATIC` (MODIFY) | [`brain/fragments/draw-mep-schematic`](../../brain/fragments/draw-mep-schematic/fragment.yaml) | The system named (exactly, or the ONE whose name contains the words - said), or the system of the element handed in (`start`, "selected"), is traced through its HVAC or piping connectors (`Connector.AllRefs`) inside Revit's own sets for it - `MEPSystem.Elements`, `DuctNetwork` / `PipingNetwork`, the base equipment - breadth-first from `start`, else the base equipment, else (said) an element with one joint. A joint to another system's element is not followed and is counted; an END connector of a run, fitting or accessory joined to nothing is an open end (`openEnds`, X on the drawing); a joint that would close a loop is cut and both ends marked L1, L2 ... (`loopsCut`); elements of the system never reached are `skipped`. Above `maxElements` (2000) it refuses before anything is made. Between nodes (root, equipment, terminals, junctions, ends, open ends, loop ends) runs, elbows, transitions and accessories are one line; a change of size starts a new labelled segment. Each segment's label: size (`RBS_CALCULATED_SIZE`), Q flow (the first run's Flow), Δp (the Pressure Drop parameter on each run, elbow, transition and accessory of the segment - one shown as Revit shows it, several ADDED and formatted by `UnitFormatUtils`); a system Revit has not calculated says "not calculated", never 0; a missing value is in `labelsWithoutValue`. Laid out as an orthogonal tree, NOT to scale: main run right, the child with most terminals straight on, branches square to it on alternating sides, each label and branch placed clear of the last on its side by `spacingMm` paper mm, judged from the text type's size - then every text note's real box is read back and any two touching are counted in findings. Detail lines, two-arc circles for terminals, rectangles for equipment, text notes (named or the project's default text type), a legend above. A NEW drafting view, "Schematic - <system>" or `newViewName`, a taken name getting " (2)" - an existing view is never touched. Nothing in the 3D model is changed. One undo |

**What is done and what is not.** It compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-08, a Linux container). Every Revit member it uses is in the
card's compatibility note, looked up with `tools/api-surface --members`; the one version branch is the format
of a sum of pressure drops (UnitType on 2020, SpecTypeId from 2021). Its layout was ported line for line to a
scratch Python script and run on 1,200 random trees of 2 to 120 nodes: no two label boxes overlapping, no label
on a line, no two lines crossing - and with the gap made negative on purpose the same check found 910 overlaps,
so it can fail. That is the layout's arithmetic, not Revit: **nothing here has run in Revit.** The card is
DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (the rows below measure them): whether
`MEPSystem.Elements` with `DuctNetwork` / `PipingNetwork` holds every element of the system, fittings and
accessories included (MW2); whether a duct or pipe FITTING carries a Pressure Drop parameter at all - if not,
Δp is the runs' alone and the legend over-says it (MW10); whether `AllRefs` from an air handler reaches the
return side's elements only through connectors this card then leaves (MW2); whether `TextNote.Create` with
Left / Top alignment puts a note's top-left at the point given, so the estimate matches the drawing (MW8,
MW13); whether `get_BoundingBox(view)` of a new text note is readable after `Regenerate` in the same
transaction (MW13); how long a large system takes (MW7); whether `UnitFormatUtils.Format` gives the same text
as `AsValueString` for one value (MW10).

**Linked models are not read** - a system in a link is not found, and `findings` says so on every run.

**Routing.** Measured 2026-11-08 with `tools/score-routing.py --sentences` (49 sentences frozen before the
card was written: 8 for this card, 27 of the neighbouring cards' own words, 14 questions; lexical and model -
the re-ranked backends NOT ASKED, no cross-encoder on the container) and `tools/check-routing.py` (model,
scratch stores), on this tree against the same tree without the card. All 27 neighbour sentences reach the same
card after as before, on both backends. Of the 14 questions, the same ones reach a write before and after (5
lexical, 7 model) - **one moved: *"is there already a schematic of this system"* reaches this card on model
(before: `EDIT_MEP_SYSTEM`, also a write; lexical still `EDIT_MEP_SYSTEM`)** - OPEN, row 5b-1479. Two wordings
were changed after `check-routing` named them: "the system is traced" in the purpose took
`TRACE_CONNECTIVITY`'s *"trace this system"*, and the utterance "3D to 2D MEP schematic" took FRG-ELE-001's
*"all the mep in this view"*; after the change `check-routing` names this card for no other card's sentence and
reports no crossing. None of the overlapping cards was edited.

**The model - a SAVED scratch copy of Revit's "Snowdon Towers Sample HVAC", Revit 2024**, the model groups MT
and MU use. Never a working model. Every run below is rolled back except MW1's arrangement and MW8's one run.
For Revit 2020, *MEP Night Test* - the model `REPORT_MEP_PRESSURE_DROP` was proved on, with its calculated
"Mechanical Supply Air" systems. The job file is
[`tools/jobs/draw-mep-schematic-mw-2026-11-08.yaml`](../../tools/jobs/draw-mep-schematic-mw-2026-11-08.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MW0** | *Open.* Revit 2024, the scratch copy, Changes on at the ribbon | The title bar names the scratch copy |
| **MW1** | *Arrange, and KEEP it.* In the System Browser **record**: one SMALL supply air system with base equipment and air terminals (under about 40 elements) and Revit's flows calculated - its name and its terminal count; one pipe system (hydronic, if the sample has one) - its name; whether any system has an open end; a word several system names contain ("Supply"). If no system name is "Mechanical Supply Air 1", write the small system's name into the job file. In the copy: one pipe LOOP - two pipes of one pipe system joined so the system closes on itself (record which); one supply air system with ONE terminal disconnected so Revit cannot calculate it (record which). Save | The names recorded; the loop and the open system in the copy |
| **MW2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: refused naming "ZZZ No Such System" and listing the systems, viewId null, **no new view** in the Project Browser. Positive: viewId a drafting view, viewName "Schematic - <system>", nodes, segments and terminals above 0, **terminals = the terminal count MW1 recorded**, skipped empty (or each one named - record why), findings saying nothing in the model was changed. Record the elements count from findings against the System Browser's count for the system |
| **MW3** | *Each refusal.* systemName left out with nothing selected; systemName MW1's word several names contain; systemName MW1's system with `start` "selected" on a pipe of another system; `start` "selected" on a wall; `start` "selected" on an air handler on a supply AND a return system with systemName left out; maxElements 5; textType "ZZZ No Such Text"; viewScale 0; spacingMm 0; maxElements 0 - each rolled back | Each refused in its own words and **no view made**. The several-names case lists them; the air handler case names both systems; maxElements 5 names the system's element count |
| **MW4** | *No flow calculated.* MW1's supply air system with the disconnected terminal, rolled back | The schematic drawn; **every Δp label "not calculated", never 0**; findings saying Revit has NOT calculated the system; labelsWithoutValue one line per segment; the disconnected terminal in skipped "not joined to the drawn part by its connectors"; the duct end it left in openEnds and marked X. `REPORT_MEP_PRESSURE_DROP` on the same system says UNCALCULATED |
| **MW5** | *A pipe loop.* MW1's pipe system with the loop, rolled back | loopsCut one line naming the two pipes of the cut joint; both ends marked L1 on the drawing with "loop L1 cut"; every other pipe drawn once - **no element drawn twice**, no line crossing another |
| **MW6** | *Open ends.* A system with an open end in the sample, or the MW4 system, rolled back | openEnds naming each element with an end joined to nothing; findings counting the open connectors; an X at each on the drawing, labelled "open end" |
| **MW7** | *A large system, and its time.* The largest duct system of the copy, maxElements above its count, rolled back; then the same with maxElements below its count | First: **record the time** findings gives ("took N s") and the element count; open the view before rolling back if the run allows, else run it once more in MW8's way on a copy - the drawing tidy, no labels touching (findings' read-back line). Second: refused naming the count, nothing made. If it takes over a minute, record it - maxElements' default of 2000 is then for the owner to lower |
| **MW8** | *Kept, through `try-once`, on the modeller's yes.* MW2's positive on the saved copy, a plan in front | Keep or Discard. **Open the view** and look: base equipment a rectangle at the left with its Mark (or type name) behind it; main run going right; branches square to it, alternating up and down; a circle and Mark at every terminal; each straight segment with three lines - size, Q, Δp; a legend above saying NOT TO SCALE and that the values are Revit's own; **no two labels touching and no label on a line**. One Ctrl+Z (before Keep) takes the whole view away. Then, rolled back: the same call again - a NEW view "... (2)", findings saying the first was left alone, the kept view unchanged |
| **MW9** | *From the selection.* One duct of MW1's supply air system selected; `start` "selected", systemName left out, rolled back | The same system drawn from that duct - "start - <duct type>" beside the root, the base equipment a node further along |
| **MW10** | *The labels against Revit.* On MW8's kept view: three labelled segments, each checked against Properties of its first duct - Size, Flow, Pressure Drop; for one segment of SEVERAL ducts, the ducts' Pressure Drop added by hand against its Δp. **Select one elbow and one transition of the system and record whether Properties shows a Pressure Drop for each** | Size and Q exactly as Properties shows them; Δp of a one-duct segment exactly its Pressure Drop; Δp of the several-duct segment the hand sum, in the project's pressure units. **Record whether fittings carry Pressure Drop**: if not, the legend's "elbows, transitions and accessories, added" over-says it and the card is corrected |
| **MW11** | *Revit 2020.* *MEP Night Test* in Revit 2020: MW2's two legs with systemName "Mechanical Supply Air 3" (or the smallest calculated system there - record it), rolled back; then once more on a scratch copy, kept, and looked at | The view made at 1:100 with its labels - **the pressure units of a several-run Δp come through the 2020 route (UnitType)** in the project's units, never "(Revit internal units)"; the negative refused |
| **MW12** | *Revit 2025 or 2026.* The Snowdon copy opened (upgraded on a further copy) in Revit 2025 or 2026: MW2's two legs, rolled back; MW8's look on a kept copy | As MW2 and MW8 - the SpecTypeId route for a several-run Δp; Plumbing Equipment recognised as equipment if the model has any |
| **MW13** | *The size estimate against the drawing.* MW8's kept view, and the same system with textType a LARGER text type of the model (record which), rolled back | findings' read-back line: none of the text notes touches another - with the larger type too, since the gaps follow the type's height. Any touching is a FAIL of the estimate: record which labels, and the text type |
| **MW14** | *Linked models.* A model with the HVAC model as a LINK (or the copy's architecture, if it holds a duct system - else NOT COVERED): systemName of a system that is only in the link, rolled back | Refused, no such system - findings on another run saying linked models are not read; never a schematic of the link's system |
