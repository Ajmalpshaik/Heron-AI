# Needs checking — Group AP

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-24 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AP - what three downloaded skill sets raised: a pitched roof, a name two parameters share, and two claims to settle (2026-09-24)

**The owner's PC, Revit 2024 first, on a scratch model - never his own design.** Every row comes out of
[39](../39-three-downloaded-skill-sets.md), which studied three sets of skill files the owner downloaded,
and each row says which of its findings it settles:

- [`slope-roof-edges`](../../brain/fragments/slope-roof-edges/fragment.yaml) - `SLOPE_ROOF_EDGES`, FRG-ELE-071 - a
  flat footprint roof made a gable or a hip, at a pitch given in degrees
- [`building-shell`](../../brain/skills/building-shell.yaml) - the skill, the order a building's shell goes up in
- [row 5b-203](../FRAGMENT-ISSUES.md#) - ten fragments that CHANGE the model with a parameter found by a
  name, now refusing a name two parameters share. Nine of them were `PROVEN` and went back to `DRAFT`,
  because the refusal is inside `impl/` and moved every fingerprint

**What is already known, and it is NOT a proof.** The new fragment and the ten repaired ones compile on all
eight releases, 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-09-24, 408 fragments), and the new
one's own sentences route to it (`tools/check-routing.py --revit 2024`). **None of it has met a model.**

**The arrangement, for AP4 to AP8:** a new project from the metric architectural template with two
levels, one run of curtain wall of any type, and a copy of it saved as a scratch file. AP1 to AP3 use a
test copy of `test projject`, where `create-roof` was proved. Rows are run rolled back unless they say
otherwise, so every scratch file ends as it began.

| # | Check | Expected |
|---|---|---|
| **AP1** | `SLOPE_ROOF_EDGES`'s proof, on a TEST COPY of `test projject` in Revit 2024. **Count the roofs first** - the setup selects every roof in the view, and a leftover rectangular one would be sloped on the negative leg. Check the view `{3D}` exists, or put another view that shows roofs into the job. Then `python tools/batch-prove.py tools/jobs/slope-roof-edges-test-projject-2026-09-24.yaml --dry-run --session <pid>`, and the same without `--dry-run` | **Positive** - a flat 10000 x 6000 roof on Level 2, made gable at 30 degrees: `sloped` 2, `upright` 2, `differs` empty; `readBack` names the two 10000 mm edges as eaves at 30.00 degrees with a rise of 0.5774 per 1 of run; `measuredMm` has the highest point about 2200 mm above the lowest where it was 400 mm. **About 90000 mm there would be the degrees written unconverted**, the one mistake this fragment exists to make impossible. **Negative** - the same request on an L-shaped roof: `sloped` 0, and `refused` saying its outline has six sides and no single pair of long eaves |
| **AP2** | The rest of `slope-roof-edges/tests/cases.yaml`, by hand in the same test copy: hip at 30; a long side drawn in two pieces; a roof drawn with Architecture, Roof by Footprint instead of by Heron; and pitch 0, pitch 100, a square outline, `mansard`, a wall selected, an extrusion roof. **Then the second route:** slope the same 10000 x 6000 roof by hand in Edit Footprint, and cut a section through both | Every expectation the case file writes down - hip `sloped` 4; two pieces `sloped` 3 and `upright` 2, because sides are counted and not sketch lines; the hand-drawn roof the same answer as Heron's; each refusal in its own words with `sloped` 0. In the section, eave and ridge at the same heights to the millimetre |
| **AP3** | AP1 on **Revit 2020**. Then, still rolled back, `create-roof` followed straight away by `slope-roof-edges` with NOTHING selected | The same answer as AP1 on 2020. **The second half measures a suspicion, not a fact:** read in `RevitFragment.Shape` and never run, the add-in hands `create-roof`'s `created` - one id - to `elements`, which is a list. Record what really comes back. A refusal in words is fine; an exception is a row |
| **AP4** | `create-roof` with the default `{3D}` view open in front - not a plan - asked for exactly what its own proof asked: Level 2, `Generic - 400mm`, a 5000 x 4000 boundary | `created` and a `measuredMm` of 5000 x 4000 on plan, base at Level 2's height - the same as its proof. **That settles, for Heron, the notes' claim that a footprint roof needs a plan view open.** A refusal or a *"Value cannot be null"* here means the claim is Heron's too, and `create-roof` owes a row |
| **AP5** | Select the curtain wall and click Edit Type; then close it and read the wall's own Properties | Write down every name printed twice. **Expected on the type:** `Layout`, `Spacing` and `Adjust for Mullion Size`, once under Vertical Grid and once under Horizontal Grid; `Interior Type`, `Border 1 Type` and `Border 2 Type`, once under Vertical Mullions and once under Horizontal Mullions. **Expected on the wall itself:** `Justification`, `Angle` and `Offset` under both grid headings. That list is what [row 5b-203](../FRAGMENT-ISSUES.md#)'s refusal has to meet on a curtain wall, so a name that is NOT there twice matters as much as one that is |
| **AP6** | `revit_parameters` on Walls, naming `Spacing` | The curtain wall's TYPE row reads `sameNameOnOneElement` 2, and the reply calls the name ambiguous. If it reads 1, the name does not repeat on this release, and AP7 and AP8 need another name from AP5's list |
| **AP7** | `duplicate-type` on the curtain wall's type; then `write-element-parameters` on the NEW type, `Spacing` = 1500; then Edit Type on the new type | `ambiguous` holds the type, `written` is 0, and BOTH Spacings read what they read before. **This is the negative case the re-proof in AP8 owes.** On a checkout from before this change the same run answered `written 1` with one of the two changed - that is the defect, and it is worth one run on `main` first to see it with your own eyes |
| **AP8** | **The re-proof of the nine that went back to `DRAFT`** - `write-element-parameters`, `copy-parameter-value`, `edit-parameter-text`, `edit-text-values`, `import-parameter-values`, `remove-parameter-value`, `renumber-sequential`, `assign-location-data`, `create-view-filters-by-value` - each with the positive its last proof ran, and the new negative. **Where the negative comes from:** a NUMBER written to a type uses AP7's curtain wall type; a TEXT field needs [P8](group-p.md)'s arrangement - a shared text parameter loaded with the same name as a built-in one and bound to the category - because the curtain wall's repeated names are numbers and choices, not text | For each: the positive as its last proof recorded it; the negative lands the element in `ambiguous`, or in the fragment's own count of names carried twice, with BOTH parameters as they were; and a fresh fingerprint (D-30). `color-by-parameter` is the tenth, was `DRAFT` already, and owes the same negative on its first proof |
| **AP9** | A design option, active. Manage, Design Options: one option set with one option; make it active from the status bar. Then `create-wall`, one wall, APPLIED - and read the new wall's Design Option in Properties before pressing Undo | Record where it landed, the main model or the active option. **If it is the option, every fragment that creates anything lands there silently,** and that is a new row, not a note. This measures a claim from the notes' untried half rather than taking it |
| **AP10** | The `building-shell` skill in one chat, on a NEW project with nothing in it: *"a two-storey box 12000 by 8000 on levels at 0, 3000 and 6000; walls Generic - 200mm; one wall across each floor at 6000; a 300 slab on each floor; a room either side of it; ceilings at 2700; and a hip roof at 25 degrees"* | The host follows the skill's order and asks for anything the sentence leaves out. **The count at the skill's last step, derived from the sentence:** 3 levels; 5 walls a floor, 10 in all; 2 slabs; 4 rooms, none unenclosed; 4 ceilings; 1 roof; and a total wall length of 96000 mm on the walls' centrelines. Record every place it strays from the order - that is the evidence for or against writing the order down as steps |

**Not in this group, because not built:** everything in [39](../39-three-downloaded-skill-sets.md) §5.
