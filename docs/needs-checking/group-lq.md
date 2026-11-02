# Needs checking — Group LQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LQ - parallel copies of a pipe or conduit run, side by side or above or below it, with the elbows re-made: `CREATE_PARALLEL_RUNS` (2026-11-02)

Asked for in the modeller's words: *"add 3 parallel pipes to this run at 150 mm spacing"*, *"copy this conduit
run 4 times side by side at 50 mm"* and *"run two parallel pipes above this one at 200 mm"* - Revit's Systems >
Parallel Pipes and Parallel Conduits. Revit has no API call for either. heron-duplicate-checker's method, run
before writing: `COPY_ELEMENTS` (DRAFT) copies by **one vector**, `count` times at `spacingMm` - a parallel run
only for a straight run or straight up and down; copied sideways, a run that bends lands its own leg inside the
original - a **look-alike**. `OFFSET_ELEMENTS` (PROVEN) moves each piece sideways **in place**, makes no copy and
leaves the run apart at its joints - a look-alike. `CREATE_DUCT_RUNS` (DRAFT) draws new runs along points the
caller types; `FIT_MEP_JOINTS` (PROVEN) fits ducts and pipes already drawn, not conduit; `OFFSET_MEP_SECTION`
(PROVEN) lifts a stretch of one duct or pipe. `ARRAY_ELEMENTS` is retired into `COPY_ELEMENTS`. None of them
reads a run and copies it at right angles to each piece, so a new card; none was touched but for a routing row.

| What | Where | What it does |
|---|---|---|
| `CREATE_PARALLEL_RUNS` (MODIFY) | [`brain/fragments/create-parallel-runs`](../../brain/fragments/create-parallel-runs/fragment.yaml) | Reads **one connected run of pipes or of conduits** (fittings optional in the selection) through its connectors, works out its centreline's corners, and draws **`count`** copies **`spacingMm`** apart centre to centre - **left or right** looking along the run from its start (its west end, named in the reply), **north, south, east or west** on its first piece, or **up or down** - each piece perpendicular to itself and every bend re-mitred, on the original piece's type, system, level and size, then re-makes every **elbow**, union and transition. Count and spacing are never assumed; a spacing no wider than the run's largest pipe or conduit, ducts, a valve or anything else in the run, a branch (a tee joining three handed-in pieces), a riser for up or down, and an inside corner the spacing closes up are **refused before anything is drawn** (`refused`, `copies` empty). A piece or fitting Revit refuses, or a size it snaps, **throws** - nothing kept (Article 8). `copies` every new id; `perCopy` one line per copy; the original is never touched |

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`, the same 2020 to 2027: `Pipe.Create(Document, ElementId, ElementId, ElementId, XYZ,
XYZ)`, `Conduit.Create(Document, ElementId, XYZ, XYZ, ElementId)`, `Creation.Document.NewElbowFitting`,
`NewUnionFitting` and `NewTransitionFitting(Connector, Connector)`, `MEPCurve` `ReferenceLevel`, `ConnectorManager`
and `Diameter`, `FamilyInstance.MEPModel`, `Connector` `AllRefs`, `Owner`, `Origin`, `ConnectorType` and
`IsConnected`, and `RBS_PIPE_DIAMETER_PARAM`, `RBS_CONDUIT_DIAMETER_PARAM`, `RBS_PIPING_SYSTEM_TYPE_PARAM`.
**Nothing here has run in Revit.** The card is DRAFT. **Only Revit can settle** whether `NewElbowFitting` on two
pieces drawn to the same corner trims them as it does for a duct (`FIT_MEP_JOINTS` relies on it for duct and pipe;
conduit is untried); whether a conduit type makes a union between two conduits in line; which side *left* lands on;
and whether a sloped run's copies, kept at the original's corner heights, read as the modeller expects. **Not done:**
a branched run (refused, not copied), insulation and lining (the copies have the type's own, not the original's),
and Revit's own *vertical number* and *horizontal number* together - one direction per call.

**Routing.** `tools/check-routing.py` on its own scratch store, on a copy of the tree at `066dce1` and on this
change (`model` backend): no question answered by a write; claimed and not reached 67 before and 67 after, the
same lines. The three declared sentences reach `CREATE_PARALLEL_RUNS` by identity, and *"add parallel pipes to this
run"* and *"make parallel conduits beside this run"* on the hybrid route; every sentence `COPY_ELEMENTS` and
`OFFSET_ELEMENTS` declare reaches them as before. A first draft's purpose took *"link the elbow's end angle to
Bend Angle"* (`LINK_FAMILY_PARAMETER`) and *"what diameter are these pipes"* on the keyword route alone; the purpose
was reworded before this change was finished (*perpendicular*, *largest pipe or conduit*), and neither is this
card's after. On the keyword route alone two lines remain changed, neither this card's: *"make a selection set"*
(`UPDATE_SAVED_SET` second there, `SET_SELECTION` first) and *"what diameter are these pipes"* (`READ_ELEMENT_PARAMETERS`
tenth there; first moved from `REPORT_SPRINKLER_NETWORK`, a read, to `CREATE_REBAR_BAR_TYPE`, a write - the word
weights shifting as a card is added, not this card's words); the host's lookup answers both by identity as before,
and `check-routing`'s *no question answered by a write* stays empty. `COPY_ELEMENTS` and
`OFFSET_ELEMENTS` carry a routing row each for the new card, card only - `OFFSET_ELEMENTS`' fingerprint unchanged.

**The models** - scratch projects from Revit's metric Plumbing and Electrical templates, saved under
`%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job
file is [`tools/jobs/parallel-runs-lq-2026-11-02.yaml`](../../tools/jobs/parallel-runs-lq-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LQ1** | *Arrange.* `parallel-lq`: in "Level 1", one L-shaped DN50 Domestic Cold Water run at 2700 mm - 3000 mm east, 2000 mm north, 1500 mm east - with the two elbows Revit puts in; nothing else that is a pipe. `parallel-conduit-lq`: in "Level 1", one straight run of three 25 mm conduits, 2000 mm each, east to west at 3000 mm, joined by unions. Save each | The ids of the original pieces and fittings |
| **LQ2** | *Parallel pipes - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's job, `--write`, rolled back | Negative, `spacingMm` 40: `copies` empty, `refused` *"... not wider than the run's largest pipe, 50 mm"*, nothing drawn. Positive, count 3, 150, left: `copies` 15 ids, three pipes and two elbows per copy, each elbow joined both sides, DN50 on Domestic Cold Water; the inside copies' legs shorter than the original's, the outside longer; the original's ids, lengths and joints unchanged. **RECORD which side *left* came out on** |
| **LQ3** | *Parallel conduits, by hand.* `parallel-conduit-lq`, the conduits selected, count 4, `spacingMm` 50, `side` north, `--write`, rolled back; then `spacingMm` 25 | Four straight runs 50 to 200 mm north, three 25 mm conduits and two unions each, `copies` 20 ids - **or Revit's words refusing the conduit union, recorded**. At 25: `copies` empty, refused |
| **LQ4** | *Up, and the refusals.* `parallel-lq`, count 2, 200, `side` up; then a duct run selected; then the L-shaped run with a branch teed off its middle pipe, all selected | Two copies 200 and 400 mm over the original, the same plan shape. The duct run: refused naming the ducts. The branch: refused naming the tee. Nothing drawn for either |
| **LQ5** | *The second route.* Revit's own Parallel Pipes on a copy of `parallel-lq`, Horizontal Number 3 and Horizontal Offset 150 mm, on the same side | Each copy's pipe lengths and elbow positions the same as LQ2's, read in the Properties palette; Undo takes each run back as one entry |
