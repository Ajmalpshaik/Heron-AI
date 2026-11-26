# Needs checking — Group RB

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in this
> file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back into the
> register for every tool that reads the register.

## Group RB - no office's name built into Heron: starter materials with no default prefix, and two material refusals and the view-filter refusals with a neutral example - DRAFT until proved (2026-11-26)

**Tracked as GitHub issue [#1466](https://github.com/Ajmalpshaik/Heron-AI/issues/1466) since 2026-11-26** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** [row 5b-2062](../fragment-issues/row-5b-2062.md). Heron is for every office, and the owner's company name
was a default and part of messages every modeller sees. Changed 2026-11-26 on the owner's word ("Yes, remove it"):
[`CREATE_STARTER_MATERIALS`](../../brain/fragments/create-starter-materials/fragment.yaml) version 5 has no name
prefix unless one is given; [`SET_MATERIAL_APPEARANCE`](../../brain/fragments/set-material-appearance/fragment.yaml)
and [`SET_MATERIAL_PROPERTIES`](../../brain/fragments/set-material-properties/fragment.yaml) version 4 and
[`SET_VIEW_FILTER_CATEGORIES`](../../brain/fragments/set-view-filter-categories/fragment.yaml) give `ABC` as their
example. The three were PROVEN; each is DRAFT until this group is run and signed (D-30).

**What is done and what is not.** Compiled for Revit 2020 to 2027. **Never run in Revit.**

**The model: scratch** - Revit 2020, a new project from the default metric template, every run rolled back.

| ID | Do this | Pass looks like |
|---|---|---|
| **RB1** | **Negative, first.** `CREATE_STARTER_MATERIALS` with `starterPick=none` | Nothing made, and no material name in the answer starts with an office prefix |
| **RB2** | `CREATE_STARTER_MATERIALS` with `starterPick=glass; copper`, `namePrefix` left out | Two materials, named exactly *Glass Clear* and *Pipe Copper* - no prefix |
| **RB3** | The same with `namePrefix=XYZ_` | *XYZ_Glass Clear* and *XYZ_Pipe Copper* |
| **RB4** | `SET_MATERIAL_APPEARANCE` and `SET_MATERIAL_PROPERTIES`, each with no material named | Refused before anything changes; the example in the refusal reads `ABC_*` |
| **RB5** | `SET_VIEW_FILTER_CATEGORIES` with an operator and no value | Refused; the example reads *"does not contain ABC"* |
