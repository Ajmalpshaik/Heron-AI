# Needs checking — Group LX

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LX - Structure > Rebar in any concrete host - one bar or set of a named shape fitted into a box, or a bar sketched through points, of a named bar type, read back: `PLACE_REBAR` (2026-11-02)

Asked for in the modeller's words: *"place a T16 bar of shape 21 along this wall at 200 spacing"*, *"draw a
T12 L-bar in this slab through these points"*, *"add bars of shape 00 in this column face"* and *"sketch a
rebar along this line in the wall"* - Revit's Structure > Rebar, a standard shape or a sketched one, in any
concrete host. `brain/heron_retrieve.py` on each sentence, Revit 2024, offered `PLACE_BEAM_REBAR`,
`PLACE_AREA_REINFORCEMENT`, `PLACE_COLUMN_REBAR` or a non-rebar card first. heron-duplicate-checker's method,
run before writing: `brain/heron_merge.py` found no shared signature among the rebar cards; the duplicate
check (`0 already here`) and the Matcher (`0 match(es), 0 partial`) found nothing equal. **Look-alikes, kept
separate:** `PLACE_COLUMN_REBAR`, `PLACE_BEAM_REBAR` and `PLACE_FOOTING_REBAR` each compute a whole layout
from one kind of host's own solid; `PLACE_AREA_REINFORCEMENT` and `PLACE_PATH_REINFORCEMENT` make a
reinforcement system, not a bar. None puts a bar of a named shape where the modeller says, in any host.

| What | Where | What it does |
|---|---|---|
| `PLACE_REBAR` (MODIFY, Revit 2020-2027) | [`brain/fragments/place-rebar`](../../brain/fragments/place-rebar/fragment.yaml) | One rebar bar or set in the ONE host selected. **Shape-driven:** a Rebar Shape by its exact name fitted into a box of two points (a straight shape) or three at a right angle. **Sketched:** no shape named, the bar drawn straight through two or more points in one plane, no hooks. Bar type by exact name or a diameter matched to one type; nothing is chosen for the modeller. A set is two of spacing, count and set length, and spreads the way `spreadToward` says. A host Revit will not take is refused with Revit's answer. Read back: the bar's id, shape, bar type, count, layout and spacing, its plane, length, the host's cover (never changed) and a first bar outside the host's extents |

**What is done and what is not.** It compiles on every release 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member it calls was read
off the reference assemblies of all eight releases with `tools/api-surface --members` - the list is in the
card's `compatibility-note`; the sketch has two routes, `CreateFromCurves` with hook types 2020-2025 and
with a `BarTerminationsData` 2026 on, so a proof on one says nothing of the other. **Nothing here has run in
Revit.** The card is DRAFT; its risk is MODIFY - it adds one rebar and deletes nothing but a bar Revit
refused in the same run.

**Not known from the assemblies, and recorded here rather than guessed** (LX2 to LX6 measure them): how
`ScaleToBox` treats a straight shape whose box has no second side - the fragment hands it the bar's own
diameter as that side; whether a shape-driven bar placed by `CreateFromRebarShape` is moved to the host's
cover by Revit or stays where the box puts it; which shape `CreateFromCurves` matches for a sketched L; and
which way `RebarShapeDrivenAccessor.Normal` points for a box, which decides whether `spreadToward` is
accepted as its side.

**The model - Group HY's scratch project in Revit 2020**, from Revit's metric structural template, in
`%TEMP%\heron-work\rebar-hy` ([group-hy.md](group-hy.md)): wall W1, floor S1, the steel column N1. Every
run rolled back. The job file is
[`tools/jobs/place-rebar-lx-2026-11-02.yaml`](../../tools/jobs/place-rebar-lx-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). **Its ids, bar type, shape name and points are left for LX1
to read** and are marked in the file; until they are filled in the run is refused - expected, NOT RUN.
Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected,
and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LX0** | *Open.* The HY scratch project open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon | The title bar reads the scratch copy's name; W1, S1 and N1 show in `{3D}` |
| **LX1** | *Read the values.* `LIST_REBAR_TYPES`; W1's location line and base from its Properties; W1's and N1's ids with Manage > IDs of Selection | A loaded bar type, the straight shape's exact name and one bent shape's name, two points 1000 mm apart on W1's line 1000 mm above its base, and the wall's direction - all written into the job file |
| **LX2** | *The D-30 positive and its negative - THE NEGATIVE FIRST.* The job file's job | Negative: N1 - `placed` 0, *Revit says ... cannot hold rebar*, Structural Rebar count in `{3D}` unchanged. Positive: `placed` 1, *11 bar(s), maximum spacing, spacing 200 mm*, W1's cover named and *not changed*, `readBackWrong` empty; in the rolled-back group an elevation of W1 shows the bars 200 apart over 2000 mm. **Record** whether the bars sit where the box says or were moved to the cover |
| **LX3** | *Refusals.* LX2's positive with `barType` `T16` (none named so); with `shape` a name not loaded; with `spreadToward` left out; with `setLength` left out; with W1 and S1 both selected | Each `placed` 0 and refused in words: the loaded bar types; the shapes whose name holds the text; the side the set spreads not said; a spacing over what length; a bar goes in ONE host |
| **LX4** | *A bent shape.* W1, the bent shape LX1 read, three box points at a right angle in W1's plane | `placed` 1, the shape read back as the one named, its sides fitted to the box |
| **LX5** | *Sketched.* S1, `shape` left out, `through` three points in S1's mid-depth plane making an L (1500 and 800 mm legs) | `placed` 1, *sketched through 3 points*, the shape Revit matched or made named, S1's top and bottom cover named |
| **LX6** | *The second route - Revit 2026 or 2027.* LX5 again | The same answer: the `BarTerminationsData` route |
| **LX7** | *Kept, through `try-once`, on the modeller's yes.* LX2's positive on a saved copy, an elevation of W1 in front | Keep or Discard; afterwards Revit's own Structure > Rebar with the same shape and type beside it matches in Properties - the second route - and one Ctrl+Z takes the set back |
