# Needs checking — Group KE

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KE - rebar couplers on bar ends or between two bars: `PLACE_REBAR_COUPLERS` (2026-11-01)

Asked for in the modeller's words: *"put a coupler at the ends of these bars"*, *"join these two bars
with a coupler"*, *"add couplers to the top end of the column bars"* - Revit's
Structure > Reinforcement > Rebar Coupler. Nothing in Heron placed a coupler: `PLACE_BEAM_REBAR`, `PLACE_COLUMN_REBAR` and
`PLACE_FOOTING_REBAR` place bars in a host, `CREATE_REBAR_BAR_TYPE` makes bar types, and `LIST_REBAR_TYPES`
and `REPORT_HOST_REBAR` read. `brain/heron_retrieve.py` on each sentence, Revit 2024, offered
`PLACE_BEAM_REBAR` or `PLACE_COLUMN_REBAR` first.

| What | Where | What it does |
|---|---|---|
| `PLACE_REBAR_COUPLERS` (MODIFY) | [`brain/fragments/place-rebar-couplers`](../../brain/fragments/place-rebar-couplers/fragment.yaml) | Places couplers of the Structural Rebar Couplers type the modeller **names** - by its type name, or *Family: Type* where two families share one. **None named, or one not loaded: nothing is made** and the loaded coupler types come back. *Coupler at bar end* (left out) caps the start, end, both (left out), top or bottom of each selected bar or rebar set - top and bottom read from the bar's centre line, and a level bar named and left. *Coupler between bars* (`between`) joins two selected bars where their ends meet; an end three bars meet at is named and left. An end that already has a coupler is left alone. Each refusal is Revit's `RebarCouplerError`, said per bar end in Revit words. "Include end treatments in Rebar Shape definition" being on is named beside every refusal and never changed. Each coupler read back on its bar ends after a regenerate |

**Why a new card and not a widening.** Every rebar card places, types or reads **bars**; a coupler is a
second element on bars already placed, and no card's contract has a coupler type or a bar end in it.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of
all eight with `tools/api-surface --members`: `RebarCoupler.Create(Document, ElementId, ReinforcementData,
ReinforcementData, out RebarCouplerError)`, `GetCouplerQuantity`, `RebarReinforcementData.Create(ElementId,
int)`, the eight `RebarCouplerError` values, `Rebar.GetCouplerId(int)`, `NumberOfBarPositions`,
`DoesBarExistAtPosition`, `ReinforcementSettings.RebarShapeDefinesEndTreatments`,
`BuiltInCategory.OST_Coupler` and `FamilySymbol.IsActive` / `Activate` are present and unchanged
2020-2027; `Rebar.GetCenterlineCurves` (2026) and `ReinforcementSettings.GetReinforcementSettings` (2022)
renamed a parameter with the same types. **Nothing here has run in Revit.** The card is DRAFT; its risk is
MODIFY - it adds couplers and deletes nothing.

**Not known from the assemblies, and recorded here rather than guessed** (KE2 to KE5 measure them): that
end 0 of `RebarReinforcementData` and `GetCouplerId` is the end where the bar's centre line starts, which
*top* and *bottom* rely on - the wrong way round puts the coupler at the foot of the column in a section;
whether a coupler type must be active before `RebarCoupler.Create` (the fragment activates it if not);
whether two bars drawn end to end meet within the 10 mm the fragment allows for pairing; and whether
"Include end treatments in Rebar Shape definition" on refuses a coupler or changes the bar's shape.

**The model - Group HY's scratch project in Revit 2020**, from Revit's metric structural template, in
`%TEMP%\heron-work\rebar-hy`, **after HY6 is kept**, so column C1 holds two main bar sets and one tie set
([group-hy.md](group-hy.md)). **Which coupler type it has loaded, if any, is not known here.** Every run
rolled back. The job file is
[`tools/jobs/place-rebar-couplers-ke-2026-11-01.yaml`](../../tools/jobs/place-rebar-couplers-ke-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). **Its coupler type is left for KE1 to read** and is marked
in the file; until it is filled in the run is refused naming the loaded types - expected, NOT RUN. Every
line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and
`< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KE0** | *Open.* The HY scratch project, HY6 kept, open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon | The title bar reads the scratch copy's name; C1's bars show in `FloorPlan: Level 1` |
| **KE1** | *Read the coupler types - also the "none named" negative.* This card on C1's bars with `couplerType` left out | `placed` 0, *No coupler type was named - Heron never chooses one*, `couplerTypes` listing what is loaded. Write one into the job file, both places. **If none is loaded**, load one coupler family into the scratch copy by hand, save, and run KE1 again |
| **KE2** | *Top ends, the D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's job | Negative: the columns instead of their bars - `placed` 0, every column *not a rebar bar or rebar set*, *NOTHING WAS MADE*. Positive: `placed` 2, one coupler at the top of each main bar set, the tie set skipped as having no top end; read in the same rolled-back group, a section shows the couplers at the **upper** ends. **Record** which end moved - that settles end 0 |
| **KE3** | *A second run, and a name not loaded.* KE2's positive twice in one rolled-back group; then `couplerType` `KE no such coupler` | Second run: `placed` 0, both main bar sets in `alreadyCoupled`. The unknown name: refused, the loaded coupler types named |
| **KE4** | *A hooked end.* A bar with a hook at one end, `barEnd` that end | That end skipped: *this bar end has a hook - a coupler goes only on an end with no hook* (Revit's `IncorrectEndTreatmentHook`). If Revit answers otherwise, record what it said |
| **KE5** | *Between two bars.* Two straight bars drawn by hand end to end in one host, both selected, `between=true`; then the same two bars moved 100 mm apart | `placed` 1, the coupler reading back on both ends. Moved apart: `placed` 0, *no other selected bar meets it end to end* |
| **KE6** | *Kept, through `try-once`, on the modeller's yes.* KE2's positive on a saved copy, a section through C1 in front | Keep or Discard; afterwards Revit's own Rebar Coupler on another bar end matches - the second route - and one Ctrl+Z takes every coupler back |
| **KE7** | *Revit 2026 or 2027.* KE2 again | The same answer. `GetCenterlineCurves` renamed a parameter at 2026; the call is positional, so nothing should differ |
| **KE8** | *Routing, on the owner's PC.* The routing paragraph below, measured again on `model+rerank` | The three sentences reach this card by identity. Any sentence the re-ranker moves to this card from another is a row of its own |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept place-rebar-couplers --by
"Ajmal PS"` once its positive and negative pass, and `heron-status` set to PROVEN in the same change. A row
that fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**Routing, measured 2026-11-01 in a cloud container on its `model` backend, re-ranker absent** - the
owner's PC ranks with `model+rerank`, which is **NOT RUN** here - through `brain/heron_retrieve.py` and
`tools/check-routing.py` on a scratch store each, against this tree with and without the card. The three
sentences reach this card by identity on 2020 and 2024; without it they ranked `PLACE_BEAM_REBAR` and
`PLACE_COLUMN_REBAR` first. Three undeclared phrasings - *"add a coupler to this rebar"*, *"couple these
bars together"* and *"put rebar couplers on the column bars"* - reach this card ranked. `tools/check-routing.py`:
no risk-ladder crossing either time; *CLAIMED IN A ROUTING TABLE AND NOT REACHED* 65 both times, the same
65; *SENTENCES TWO FRAGMENTS BOTH WANT* 695 before, 697 after - the two added are `PLACE_FAMILY_INSTANCES`'s
*"place a diffuser"* and `PLACE_STRUCTURAL_FAMILY`'s *"put a column at each grid intersection"*, each
now ranked second behind another card that is not this one, and each still reaching its own card by
identity on 2020 and 2024. **No claimed sentence moved**, so no row was owed. `PLACE_COLUMN_REBAR`'s
routing table gained one comment row naming this card for the column-bar sentence; its `impl/` is
untouched.
