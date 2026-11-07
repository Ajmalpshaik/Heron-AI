# Needs checking — Group MQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MQ - the visualiser's answer no longer says a 3D view is not one, and names what a preset replaced: `APPLY_VISUAL_PRESET` v4, `REPORT_VISUAL_PRESET` v4 (2026-11-07)

On 2026-11-06, on Villa 1 in Revit 2024, Sruthina found every answer from both preset tools on the 3D view
`Exterior - Day` printing *"is not a 3D view"* and *"has no Graphic Display Options"* beside readings that showed
both ([issue #799](https://github.com/Ajmalpshaik/Heron-AI/issues/799) item 7,
[row 5b-1464](../fragment-issues/section-5b-rows-401-425.md)). The two sentences were built on every run; they are
now built only where a setting is skipped. `APPLY_VISUAL_PRESET` also names every setting it replaced, from and to
([issue #799](https://github.com/Ajmalpshaik/Heron-AI/issues/799) item 8, [row 5b-1465](../fragment-issues/section-5b-rows-401-425.md)) - it had put a Sun Intensity
of 60, set on purpose, back to 80 without saying so. Nothing either card writes or counts changed. Both were PROVEN
at version 3 and are DRAFT at version 4 until this group proves them.

| What | Where | What changed |
|---|---|---|
| `APPLY_VISUAL_PRESET` v4 (MODIFY) | [`brain/fragments/apply-visual-preset`](../../brain/fragments/apply-visual-preset/fragment.yaml) | `noRender` and `noDisplay` are `Func<string>`, called where a render or display setting is skipped; a finding *"Replaced what '<view>' had:"* names each setting changed, from and to |
| `REPORT_VISUAL_PRESET` v4 (READ) | [`brain/fragments/report-visual-preset`](../../brain/fragments/report-visual-preset/fragment.yaml) | The same two lines, byte for byte |

**What is done and what is not.** Both compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-07, a Linux container). **Neither has met a Revit model at version 4.**

| # | Run | Look for |
|---|---|---|
| **MQ0** | *Open.* Revit 2024 (where it was found), on a **saved test copy** with a 3D view and a floor plan. Changes on at the ribbon | The title bar names the copy |
| **MQ1** | *REPORT_VISUAL_PRESET - D-30 pair, NEGATIVE FIRST.* Negative: preset "Studio Glow" on the 3D view. Positive: the 3D view, preset Exterior Day | Negative: refused naming the seven presets, nothing compared. Positive: `noRender` and `noDisplay` named only on the line of values that are code; **no "is not a 3D view" and no "has no Graphic Display Options" anywhere in the answer**. Version 3 printed both here |
| **MQ2** | *REPORT_VISUAL_PRESET on the floor plan, preset Exterior Day* | Each render setting skipped still says *"is not a 3D view, and only a 3D view has Rendering Settings"* - the sentence is made where it is true |
| **MQ3** | *APPLY_VISUAL_PRESET - D-30 pair, through Keep or Discard.* Negative: preset "Studio Glow". Positive: the 3D view, preset Exterior Day, renderFor issue | Negative: refused, nothing changed. Positive: the same absence as MQ1; Render Quality High and the output DPI read back as set. Keep or Discard is the modeller's |
| **MQ4** | *APPLY_VISUAL_PRESET on the floor plan* | The render settings skipped, each naming the reason, as MQ2 |
| **MQ5** | *APPLY_VISUAL_PRESET after a hand-set value, through Keep or Discard.* SET_GRAPHIC_DISPLAY_OPTIONS Sun Intensity 60 on the 3D view, then preset Exterior Day | A finding *"Replaced what '<view>' had:"* naming Sun Intensity 60 -> 80, and each other setting it changed from and to. Version 3 said only the count |

**Run on 2026-11-07**, on the scrap test copy `Test for Issue` (Revit 2024, session 30316; not a job model), 3D view `{3D}` and
floor plan `MP Plan S1` - `L1 - Architectural` could not be named, because a floor plan and a ceiling plan share the name and
`FloorPlan: L1 - Architectural` is refused ([row 5b-1468](../fragment-issues/section-5b-rows-401-425.md)). **MQ1 and MQ2 held**:
neither sentence on the 3D view, the reason named on the floor plan; the negative refused. **MQ3's negative and MQ4 held**,
rolled back. **MQ3's positive** (`try-once`, KEPT by the owner): neither sentence, Render Quality High read back - but the output
stayed Screen, not 300 DPI ([row 5b-1472](../fragment-issues/section-5b-rows-401-425.md)). **MQ5 not confirmed**: the
*"Replaced what '{3D}' had:"* finding is there, cut at 60 characters before it leaves Revit
([row 5b-1467](../fragment-issues/section-5b-rows-401-425.md)). `REPORT_VISUAL_PRESET`'s draft is built and waits for the owner;
`APPLY_VISUAL_PRESET` stays DRAFT.
