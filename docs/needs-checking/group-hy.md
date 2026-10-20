# Needs checking — Group HY

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-19 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HY - rebar: thirteen DRAFT tools that place and read reinforcement (2026-10-19)

From [row 5b-1257](../fragment-issues/section-5b-rows-401-425.md). A comparison of Revit jobs Heron could
not do found reinforcement missing entirely - [39 §5](../39-three-downloaded-skill-sets.md) had listed it
as unbuilt since 2026-09-24. Thirteen fragments and one method were written for it on 2026-10-19:

| Capability | Folder | What it does |
|---|---|---|
| `LIST_REBAR_TYPES` | [`list-rebar-types`](../../brain/fragments/list-rebar-types/fragment.yaml) | READ - bar types with diameters and bends, hooks, covers, shapes, area / path / fabric types |
| `REPORT_HOST_REBAR` | [`report-host-rebar`](../../brain/fragments/report-host-rebar/fragment.yaml) | READ - per host: cover on each face, every set's type, count, spacing, hooks; systems; a bar list |
| `CREATE_REBAR_BAR_TYPE` | [`create-rebar-bar-type`](../../brain/fragments/create-rebar-bar-type/fragment.yaml) | a bar type from a name, diameter and three bend diameters |
| `CREATE_REBAR_COVER_TYPE` | [`create-rebar-cover-type`](../../brain/fragments/create-rebar-cover-type/fragment.yaml) | a cover type from a name and a distance |
| `CREATE_REBAR_HOOK_TYPE` | [`create-rebar-hook-type`](../../brain/fragments/create-rebar-hook-type/fragment.yaml) | a hook type from a name, angle, extension and style |
| `SET_REBAR_COVER` | [`set-rebar-cover`](../../brain/fragments/set-rebar-cover/fragment.yaml) | a host's cover, face by face, by cover type name or distance |
| `PLACE_COLUMN_REBAR` | [`place-column-rebar`](../../brain/fragments/place-column-rebar/fragment.yaml) | main bars and ties in rectangular and round columns |
| `PLACE_BEAM_REBAR` | [`place-beam-rebar`](../../brain/fragments/place-beam-rebar/fragment.yaml) | bottom, top and side face bars and stirrups in straight beams |
| `PLACE_FOOTING_REBAR` | [`place-footing-rebar`](../../brain/fragments/place-footing-rebar/fragment.yaml) | bottom and top layers both ways in pad footings and pile caps |
| `PLACE_AREA_REINFORCEMENT` | [`place-area-reinforcement`](../../brain/fragments/place-area-reinforcement/fragment.yaml) | four layers over a whole slab, raft or wall |
| `PLACE_PATH_REINFORCEMENT` | [`place-path-reinforcement`](../../brain/fragments/place-path-reinforcement/fragment.yaml) | bars of one length across a path on one slab or wall |
| `PLACE_FABRIC_REINFORCEMENT` | [`place-fabric-reinforcement`](../../brain/fragments/place-fabric-reinforcement/fragment.yaml) | mesh sheets over a whole slab or wall |
| `SET_REBAR_VIEW_STATE` | [`set-rebar-view-state`](../../brain/fragments/set-rebar-view-state/fragment.yaml) | unobscured, and solid in 3D on 2020-2023 |

The method is [`brain/skills/reinforce-concrete.yaml`](../../brain/skills/reinforce-concrete.yaml).

**What is done and what is not.** Every card validates (`python brain/heron_fragment.py`) and compiles for
2020 to 2027 (`tools/check-fragments-compile.py`). **Nothing here has run in Revit.** Every card is DRAFT
until its rows below pass and the owner signs. **The placing tools have two routes**
([row 5b-1258](../fragment-issues/section-5b-rows-401-425.md)): Revit 2020 to 2025 call
`Rebar.CreateFromCurves` with hook types and `RebarHookOrientation`, 2026 and 2027 the form taking a
`BarTerminationsData`, so a proof on one release says nothing about the other - HY15 is the second.

**The model.** A **scratch** project from Revit's metric **structural** template (it loads rebar bar
types, hook types, cover types and rebar shapes), saved to `%TEMP%\heron-work\rebar-hy`, never a working
model. In it: Level 1 at 0 and Level 2 at 3000; two **400 x 400** concrete columns (C1, C2) from Level 1
to Level 2, 6000 apart; one **600 round** concrete column (C3); one **300 x 600** concrete beam (B1)
between C1 and C2 at Level 2; one **2000 x 1500 x 600** concrete pad footing (F1) under C1; one 200 mm
structural concrete floor (S1) and one 200 mm structural concrete wall (W1); and for the negatives one
steel column (N1), one architectural (non-structural) floor (N2) and a second scratch project from an
**architectural** template (N3). Run every row on **Revit 2020**, then HY15 on **Revit 2026 or 2027**.
Count Structural Rebar, Structural Area Reinforcement, Structural Path Reinforcement and Structural
Fabric Areas in `{3D}` before and after each row. Every value below is the proof's own, chosen for this
model - never a design value Heron offers.

| # | Run | Look for |
|---|---|---|
| **HY1** | `LIST_REBAR_TYPES` on the structural project; then on N3 | **Positive:** every count above 0 and each bar type's nominal diameter exactly as its Type Properties show. **Negative (N3):** every count 0, `listing` saying none for each, and the finding that no bar can be placed - nothing invented |
| **HY2** | `CREATE_REBAR_BAR_TYPE` typeName `HY 16`, diameter 16, bends 64 / 64 / 32; then the same again; then `HY 25`, diameter 25, stirrupBendDiameter 20 | **Positive:** one type, Type Properties 16 mm, bends 64 / 64 / 32. **Negatives:** the repeat refused naming `HY 16` at 16 mm; the 25 mm refused - a 20 mm bend is not larger than the bar. Bar type count up by exactly one |
| **HY3** | `CREATE_REBAR_COVER_TYPE` coverName `HY cover 45`, distance 45; then the same name with 50 | **Positive:** listed at 45 mm in Rebar Cover Settings. **Negative:** refused naming the existing type at 45 mm, which still reads 45 |
| **HY4** | `CREATE_REBAR_HOOK_TYPE` hookName `HY tie 135`, angle 135, extension 10, hookStyle stirrup/tie; then angle 200 | **Positive:** 135 degrees, 10 x bar diameter, Stirrup/Tie in Type Properties. **Negative:** the 200 refused - nothing made |
| **HY5** | `SET_REBAR_COVER` on B1 with coverOther `HY cover 45` (by name), others left out; again; then coverOther 33; then W1 with coverTop 45 | **Positive:** B1's Rebar Cover - Other Faces reads `HY cover 45`, Top and Bottom unchanged. **Negatives:** the repeat `facesChanged` 0, `alreadyAsAsked` 1; 33 refused naming the loaded cover types and `CREATE_REBAR_COVER_TYPE`; W1 skipped - a wall has no top cover |
| **HY6** | `PLACE_COLUMN_REBAR` on C1: mainBars `4 x 16mm`, ties `8mm @ 150`, tieHook a loaded Stirrup/Tie 135 hook, cover 40 | **Positive:** 3 sets read back - two of 2 main bars, one of ties; in a plan at Fine the bars 40 mm + one tie inside each face, **the tie hooks turned INTO the core** (FAIL if outward: the Left/Right rule is the wrong way round); the tie count what 150 or closer gives over the height. **Negatives:** run again - C1 skipped as already holding rebar, rebar count unchanged; C2 with mainBars `4 x T16` and no type named T16 - refused naming the loaded types; C2 with `8 x 16mm` and barsPerFace left out - skipped asking for a layout; N1 - skipped, not a rebar host |
| **HY7** | `PLACE_COLUMN_REBAR` on C2 with barsPerFace `3x3`, mainBars `8 x 16mm`; on C3 with `8 x 16mm`, ties `10mm @ 200`, cover left out | **Positive:** C2 8 bars - 3 on each face along the width, 1 between corners on each face along the depth; C3 8 bars evenly round the circle and hoops, at C3's own Other Faces cover. **Say what Revit did** if the hoop is refused - the bend radius or the closed loop |
| **HY8** | `PLACE_BEAM_REBAR` on B1: bottomBars `3 x 16mm`, topBars `2 x 12mm`, stirrups `8mm @ 200`, stirrupHook the Stirrup/Tie 135 hook, barHook a Standard 90 hook, cover 30; again; then bottomBars `12 x 20mm` on a fresh copy | **Positive:** 3 sets; in a section the bottom bars 30 + stirrup up, hooks up into the beam, top bars hooks down, stirrup hooks at a top corner into the core; the bars stop 30 mm short of the concrete at each end, not at the column centres. **Negatives:** the repeat skipped; 12 bars refused as closer than their diameter |
| **HY9** | `PLACE_FOOTING_REBAR` on F1: bottomLength `16mm @ 150`, bottomWidth `16mm @ 150`, topLength none, topWidth none, outerLayer length, cover 75; then outerLayer `bottom`; then S1 | **Positive:** 2 sets, the bars along the 2000 side lowest, 75 mm above the underside. **Negatives:** `bottom` refused - length or width; S1 skipped naming `PLACE_AREA_REINFORCEMENT` |
| **HY10** | `PLACE_AREA_REINFORCEMENT` on S1: majorDirection x, bottomMajor `12mm @ 200`, bottomMinor `12mm @ 200`, topMajor none, topMinor none; on W1: majorDirection horizontal, topMajor `10mm @ 150`, the other three none; then N2; then W1 with majorDirection x | **Positive:** S1 - `report` names Revit's own labels *Bottom Major Direction* and *Bottom Minor Direction* on at 200, top off; W1 - *Exterior Major Direction* on (FAIL if Revit names it interior: the wall mapping is wrong). **Negatives:** a second run skipped; N2 skipped, not a rebar host; W1 with x skipped - a wall's direction is horizontal or vertical |
| **HY11** | `PLACE_PATH_REINFORCEMENT` on S1 alone: a 4000 mm path along an edge inside the slab, bars `12mm @ 200`, barLength 1200, face top; then S1 with face bottom; then S1 and W1 together | **Positive:** `barsInPath` what 200 gives along 4000; Face reads Top. **The face value:** with face bottom, Face reads Bottom - or the finding says no value read bottom (record which numbers Revit stores). **Negative:** two hosts refused - one slab or wall |
| **HY12** | `PLACE_FABRIC_REINFORCEMENT` on a fresh copy of S1: a loaded fabric sheet type by name, majorDirection x, location bottom; then a name no sheet type has; then W1 with location bottom | **Positive:** `sheets` above 0, Location Bottom. **Negatives:** the unknown name refused naming the loaded sheet types; W1 skipped - its faces are exterior and interior |
| **HY13** | `SET_REBAR_VIEW_STATE` on C1 (the host) in `{3D}`, unobscured yes, solid yes; again; then on a column with no rebar | **Positive:** every set in C1 unobscured and, on 2020, solid in `{3D}` (View Visibility States dialog). **Negatives:** the repeat `changed` 0; the bare column named as holding no reinforcement. **On 2024 or later** solid is not set and the finding says to use Fine detail |
| **HY14** | `REPORT_HOST_REBAR` on C1, C3, B1, F1, S1, W1 and N1 | **Positive:** every set HY6 to HY12 made, its count and spacing as Properties shows, each host's cover; a bar list by type. **Negative:** N1 in `notHosts`; the counts match a Structural Rebar schedule grouped by host |
| **HY15** | **The second route.** HY6, HY8 and HY9 again on **Revit 2026 or 2027**, on a copy of the same project opened there | The same read-back as on 2020, the hooks the same way round. A difference between the two releases is a finding against the `#if REVIT2026_OR_GREATER` branch, never a pass |
| **HY16** | **The method.** `heron_method` for *"reinforce a column"*, then the chat following it on C2 of a fresh copy, from the sentence *"put 4 T16 main bars and T8 links at 150 in these columns, 40 cover"* | The chat runs `LIST_REBAR_TYPES` first and **asks what T16 and T8 mean** when no type carries those names - it never maps them itself - asks the tie hook, then places and reads back with `REPORT_HOST_REBAR` |

**Then sign** each card on its own rows: `list-rebar-types` HY1, `create-rebar-bar-type` HY2,
`create-rebar-cover-type` HY3, `create-rebar-hook-type` HY4, `set-rebar-cover` HY5, `place-column-rebar`
HY6 + HY7 + HY15, `place-beam-rebar` HY8 + HY15, `place-footing-rebar` HY9 + HY15,
`place-area-reinforcement` HY10, `place-path-reinforcement` HY11, `place-fabric-reinforcement` HY12,
`set-rebar-view-state` HY13, `report-host-rebar` HY14. The proof plan for the batch runner is
[`tools/jobs/rebar-hy-2026-10-19.yaml`](../../tools/jobs/rebar-hy-2026-10-19.yaml).
