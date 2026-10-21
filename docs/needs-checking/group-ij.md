# Needs checking — Group IJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-21 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IJ - a lighting fixture's light source, all of it Heron's: every family type, the Light Source tick, position and aim, a wattage, loss factor, dimming and filter (2026-10-21)

From rows [5b-1294, 5b-1295 and 5b-1296](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-21 the
owner, building the pendant LED family *TRG_Lighting Fixture_LED Pendant_CoreLine WT120C* on Revit 2020 with
a Standard and an Emergency type, found `SET_FAMILY_LIGHT_SOURCE` reaching one type of two and failing when
writing lumens to all types, and the lighting method still sending him to tick Light Source, place or aim the
source and skip the wattage by hand. Built the same day, in the cloud, with no Revit:

| What | Where | What it does |
|---|---|---|
| `SET_FAMILY_LIGHT_SOURCE` v2 | [`set-family-light-source`](../../brain/fragments/set-family-light-source/fragment.yaml) | the family's half (shape, distribution, `position`, `aim`) OR the types' half (intensity incl. a wattage, colour, `lightLossFactor`, `dimming`, `colorFilter`) per call; every FAMILY type, each made current, proved from its own Type Properties; every other type checked untouched |
| `SET_FAMILY_SETTINGS` v3 | [`set-family-settings`](../../brain/fragments/set-family-settings/fragment.yaml) | `Light Source=yes`, proved by Revit's lighting API finding a light source after the write |
| the lighting method | [`lighting-fixture-family-creation.yaml`](../../brain/skills/lighting-fixture-family-creation.yaml) | step 3 now three Heron calls; its BY HAND list only what no API call reaches |

**What is done and what is not.** Both cards validate (`python brain/heron_fragment.py`) and compile for 2020
to 2027 (`tools/check-fragments-compile.py`); every Revit member they call was read with
`tools/api-surface --members` and in RevitAPI.xml on all eight releases, and none moved. **Nothing here has
run in Revit.** Both are DRAFT until these rows pass and the owner signs; their old proofs are kept as records.

**Four things the cards do not claim, and these rows settle.** (1) Whether a light type's values are written
to the type it is named for or to the CURRENT type, and why `GetNumberOfLightTypes` leaves a family type out
(IJ1, IJ2) - the reference says neither. (2) Whether `LIGHTING_FIXTURE_LIGHT_EMITTER` ("Light Emitter") is
the Light Source box, and whether it is on the family element at all (IJ4). (3) Whether the light source is
an element of the Light Source category that `MoveElement` and `RotateElement` move (IJ5, IJ6). (4) Which
unit `InitialWattageIntensity` takes - "universal unit" in its constructor's remark, W and lm/W in its
properties' (IJ7, BV18). Each card fails safe on the wrong answer - nothing kept - and says so; the row
records which answer Revit gave, and the card's purpose is corrected to it.

**The arrangement.** On **Revit 2020**, a COPY of the owner's family saved as
`%TEMP%\heron-work\light-ij\Heron IJ Pendant.rfa` - never the office's library file - with its two types
TRG_Standard and TRG_Emergency, Light Source ticked, and a reference plane named `Bottom of Fixture` below
the body. Before IJ1, open Family Types and write down each type's Initial Intensity, Luminous Flux, Initial
Color, Light Loss Factor and Emergency, and which type is current; note the light source's position from a
section. A second family for IJ4: a new **Generic Model** from the metric template, switched to Lighting
Fixtures with `SET_FAMILY_CATEGORY`, Light Source NOT ticked. Every row is a change: run it on the saved
family through `revit_change`, read the answer and Family Types, and press **Discard** unless the row says
Keep - so each row starts from the family as saved.

| # | Run | Look for |
|---|---|---|
| **IJ0** | *Set up.* The copy above open, TRG_Emergency made CURRENT by `SET_FAMILY_TYPE_VALUES` | Family Types shows TRG_Emergency current. The values noted |
| **IJ1** | *The 5b-1294 negative FIRST.* `shape` "line 1160", `distribution` "hemispherical", `intensity` "4500 lm", `colorTemperature` "4000 K", `typeName` empty | **Refused before anything is written** - both halves in one call, with Revit's reason quoted. Family Types unchanged |
| **IJ2** | *Every type - the measurement.* `intensity` "4500 lm", `colorTemperature` "4000 K", `typeName` empty, TRG_Emergency still current | **Either** both types read Luminous Flux 4500 lm and 4000 K in the report AND in Family Types with each type chosen, TRG_Emergency named *not listed by the lighting API; reached with it current*, and TRG_Emergency current afterwards - **or** the call fails naming TRG_Emergency, *No call on any release makes a light type* and the hand fix, Family Types unchanged. **Record which**, and whether TRG_Standard was the one listed. Then the same with TRG_Standard current. Keep the passing run if it passes |
| **IJ3** | *One type, the other untouched.* `colorTemperature` "3000 K", `typeName` TRG_Standard | TRG_Standard 3000 K; **TRG_Emergency's light values exactly as IJ2 left them** in Family Types - else the call fails naming the type it changed. Then `shape` "line 1160", `typeName` TRG_Standard: **refused** - the family's light source, every type takes it |
| **IJ4** | *The tick.* The Generic Model family above; `SET_FAMILY_SETTINGS` "Light Source=yes" | **Either** settingReport *Light Source No -> Yes*, the finding that the lighting API finds the light source, Family Category and Parameters showing Light Source ticked, and `SET_FAMILY_LIGHT_SOURCE` `shape` "point" then running without refusing - **or** a refusal naming the missing switch / the call failing because the switch written is not the tick. **Record which.** Negative: "Light Source=no" on the pendant - refused, nothing changed |
| **IJ5** | *Move.* The pendant; `position` "z 2400", then `position` "plane Bottom of Fixture offset -10" | The light source at Z 2400 mm with X and Y unchanged, read back, and LOOKED AT in a section; then 10 mm below the plane. **Record** whether exactly one element of the Light Source category was found. Negative: `position` "plane No Such Plane" - refused naming the planes there |
| **IJ6** | *Aim.* `aim` "tilt 15 about x" | Its Z axis reported turned 15° about the family's X, the position unchanged; a section shows the source tilted. If the light source is locked to a plane, the call fails saying so - **record it** |
| **IJ7** | *A wattage - BV18.* `intensity` "35.5 W at 127 lm/W", `typeName` TRG_Standard | **Either** Type Properties Wattage 35.5 W and Efficacy 127 lm/W, Luminous Flux about 4508.5 lm, and the finding *BV18: Type Properties read the wattage back in watts* - **or** the call failing with *this Revit did not take W*, nothing kept. **Record which** - it settles BV18. Then `intensity` "4500 lm at 35.5 W": a wattage intensity, Luminous Flux 4500 lm. Negative: `intensity` "35.5 W" alone - refused, no efficacy or lumens |
| **IJ8** | *The catalog's Emergency.* `SET_FAMILY_TYPE_VALUES` Emergency No on TRG_Standard and Yes on TRG_Emergency, each by `typeName`; then `REPORT_FAMILY_TYPE_CATALOG` | The Emergency column **0** for TRG_Standard and **1** for TRG_Emergency, matching Family Types. If it shows one value for both while Family Types shows two, that is a defect of the report - a 5b row |
| **IJ9** | *Loss factor, dimming, filter.* `lightLossFactor` "0.8", `dimming` "incandescent", `colorFilter` "255 240 200", `typeName` TRG_Standard | Each read back; Total Light Loss Factor 0.8 in TRG_Standard's Type Properties; Dimming Lamp Color Temperature Shift and Color Filter as asked in Family Types; TRG_Emergency unchanged. Then `lightLossFactor` "advanced 0.95 0.9 1 0.9 1 1 1": total 0.7695. Negative: `dimming` "bright" - refused naming None and Incandescent |
| **IJ10** | *The family's half, every listed type.* `shape` "line 1160", `distribution` "web <a real .ies file> tilt 30", `typeName` empty | Every listed light type reads line 1160 mm and the web with tilt 30°; the report names the IES file; the Light Source Definition dialog shows the same. Then the method end to end (FLEX_FAMILY, LOAD_FAMILY into a scratch project, one copy rendered or in a lighting analysis) |

**The order matters.** IJ1 before IJ2, so the refusal is seen on a family nothing has changed. IJ2 is the row
that decides whether 5b-1295's every-type route works; if it fails safe, the cards' purposes say so and the
method's BY HAND list gains the hand fix for an unlisted type.
