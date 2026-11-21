# Needs checking — Group PG

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PG - REPORT_ROOF_SLOPES, the pitch of each roof read edge by edge, changing nothing - DRAFT until proved (2026-11-21)

**Why:** row [5b-1940](../fragment-issues/row-5b-1940.md), GitHub issue
[#1161](https://github.com/Ajmalpshaik/Heron-AI/issues/1161): asked *"what pitch is this roof"* or *"how steep is this
roof"*, every backend answered `SLOPE_ROOF_EDGES`, a write, because no read in the library read a roof's slope. The
new card [`report-roof-slopes`](../../brain/fragments/report-roof-slopes/fragment.yaml) (`REPORT_ROOF_SLOPES`, DRAFT
v1) reads a roof by footprint's footprint lines - which define slope, and each one's slope in degrees and as a rise
over a run - and a roof by extrusion's profile lines, each from level. Written in a cloud container with no Revit; it
compiles on 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-11-21). **Nothing here has run in Revit.** No
add-in code changed: the card travels as source.

**The model is a scratch copy, never a working model** - *roofjoin-oy*, built for [Group OY](group-oy.md) (OY1), with
one roof more: a flat roof by footprint, no edge Defining Slope. The arrangement is in the card's `tests/cases.yaml`.
A read: nothing is changed, and the undo list gains no entry. Any release; the one the owner's runs use first.

| ID | Do this | Pass looks like |
|---|---|---|
| **PG1** | *Negative - THE FIRST CASE TO RUN.* The main roof selected. Before the run, Edit Footprint and write down each line's Defines Slope and Slope, then Cancel | `roofsRead` 1; the two long sides *"slopes at 30 degrees (rise 0.577 per 1 of run)"* - **30 degrees, not 0.577 called an angle**; the two ends *"defines no slope"*; each line's length (12000 and 8000 mm); line for line as written down |
| **PG2** | *Positive.* The wing roof (by extrusion) selected | `roofsRead` 1; its two profile lines each *"at 30 degrees"*, to within 0.1 |
| **PG3** | *Negatives.* (a) The flat roof selected. (b) A wall and the main roof selected | (a) *"0 of its 4 footprint lines define slope - it is flat"*, no line given a pitch. (b) `roofsRead` 1 and a finding *"Walls <id> is not a roof, so it was left out"* |
| **PG4** | *Nothing selected* | Every roof by footprint and by extrusion in the model read, and the finding says *"every roof in the model, as none was selected"*; Edit > Undo shows no entry from the run |
