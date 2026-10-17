# Needs checking — Group EK

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group EK - up to 50 elements described fully in one read, by typed ids and inside a link: `REPORT_PARAMETER_INVENTORY` version 3 (2026-10-17)

Asked for by the owner on 2026-10-17: one read-only call that describes up to 50 elements fully. Version 2
([Group DI](group-di.md)) took only a selection or a chained set, had no cap - 535 elements came back as a
6.2 MB record (DI3) - and an id from a linked model found nothing.
[FRAGMENT-ISSUES row 5b-489](../fragment-issues/section-5b-rows-401-425.md).

**What was built - version 3 of the same capability, not a new one.** `elementIds` takes ids as Revit shows
them, matched as text in the model looked in; an id that names nothing lands in `missing` and the rest are
described. `linkName` looks the ids up inside a placed link's own document. The record holds the first 50
of N and adds, per element, the unique id, level (and which parameter gave it), owner view, workset in a
workshared model, curve length and a placed link's model title; per parameter, sorted by name, the
built-in name and the raw value in Revit's internal units. It stops at `maxChars` - 60000 when left out -
with a last line naming every element not written; `parameterFilter` keeps only the parameters asked
about.

**What is compiled and what is not run.** Compiled on all eight releases 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-17). **None of it has run in Revit.** The binding of the four
new values left out - `elementIds` "none", `linkName` "host", `maxChars` "60000", `parameterFilter` "all" - is
the add-in's `absent:` rule from [Group EI](group-ei.md), which needs that add-in deployed.

**A TEST COPY, never a working model.** Copy Snowdon-scratch_ajmal.al and save it as *Heron Record WS.rvt*;
if Collaborate > Worksets says the copy is not workshared, enable worksets on the copy. Keep its placed
links loaded. Read the ids each row types with Manage > IDs of Selection, and for a linked element from the
linked file opened alone.

| # | Run | Look for |
|---|---|---|
| **EK1** | **The D-30 positive.** 50 MIXED ids typed in `elementIds`, nothing selected - 10 ducts in FloorPlan: M1, 10 pipes (10 more ducts if the model has no pipes), 10 walls, 10 mechanical equipment, 5 duct tags, 4 air terminals and the one placed link of your choice - with fullRecord, includeTypeParameters and includeTags true, sampleOnly false, maxChars 1000000. The selection-chain half is `tools/jobs/report-parameter-inventory-v3-2026-10-17.yaml` | header "50 element(s)" and "50 id(s) typed, 50 found", `missing` empty, no RECORD CUT line, and **every field filled where it applies**: a unique id on every block; a level - walls *from LevelId*, ducts and pipes *from RBS_START_LEVEL_PARAM*; a workset name; the duct tags' owner view by name, every model element *owner view: none*; a length in mm on the ducts, pipes and walls equal to Properties' Length; the link's *link document* title. Parameters sorted by name, each with its built-in name or `-` and a raw value |
| **EK2** | EK1's 50 ids with **maxChars left out** | the binding note *maxChars not given - "60000", the card's own default*; `record` no longer than 60000 characters, ending in *RECORD CUT AT THE 60000-CHARACTER BUDGET* naming the elements written whole, the one cut and every id not written; those ids asked for again come back, nothing before them repeated |
| **EK3** | 60 ids - EK1's and 10 more ducts | *FIRST 50 OF 60* with the 51st id named as the next; the inventory in `findings` read from all 60 |
| **EK4** | **The D-30 negative.** One real duct id, `999999999` and `abc` in `elementIds`; then three ids that name nothing; then the job file's negative - its selection with two unknown ids typed | `missing` holds `999999999` and *abc (not an id - digits only)* and the duct is still described whole; the three unknown alone give no ELEMENT block and *None of the 3 id(s) typed ...*; the job's negative describes nothing and says the selected elements were NOT read |
| **EK5** | **The linked model.** Three structural framing ids from inside the placed *Snowdon Towers Sample Structural* link with `linkName` "Structural" | *read in '...Structural...' through the link* with its id; three framing blocks, each id the link's own, its level from the link, its location in the link's coordinates with *in this model's coordinates* beside it; *tags not read*; `missing` empty |
| **EK6** | EK5's ids **without** `linkName`; then `linkName` "ZZZNOTHINGHERE"; then the link unloaded in Manage Links | in the host each id lands in `missing` or names something that is not structural framing - the case version 3 exists for; no match names every placed link and leaves the three in `missing`; unloaded says *present but NOT LOADED* - never "nothing found" |
| **EK7** | `parameterFilter` "level, offset" on EK1's ids; then `maxChars` 100 | each parameter heading *N kept of M*, every kept line holding *level* or *offset* in its name or built-in name, the inventory in `findings` unfiltered; 100 raised to 5000 in the header, the record no longer than that with its RECORD CUT line |
| **EK8** | 10 ducts selected and two other ids typed; then EK1's ids on a model that is not workshared | only the two typed are described, a finding saying the 10 selected were not read; no workset line anywhere and *worksets not shown - not a workshared model* |
| **EK9** | EK1 and EK5 once on **Revit 2020** and once on **2027** | the same answers. The members were read off the reference assemblies for every release and compiled, not run |
| **EK10** | Second route: Manage > Select by ID with each typed id, and Properties' Workset, Level and Length for it; for a linked id, the linked file opened alone | the element Revit selects is the one the block describes, and the three values agree |
