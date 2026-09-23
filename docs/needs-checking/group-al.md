# Needs checking — Group AL

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group AL - duct routing in three calls: draw every run, fit every joint, join (2026-09-24)

**The owner's PC, Revit 2024 first, then 2020.** Two fragments were written on 2026-09-23 while
routing supply duct in Project1 with the owner watching, and the skill that uses them the next day:

- [`create-duct-runs`](../../brain/fragments/create-duct-runs/fragment.yaml) - `CREATE_DUCT_RUNS`,
  FRG-MEP-052: every run of a layout, each at its own size, in one call.
- [`fit-mep-joints`](../../brain/fragments/fit-mep-joints/fragment.yaml) - `FIT_MEP_JOINTS`,
  FRG-MEP-051: the fitting at every joint in one call - elbow, tee, tap, transition, union, cross -
  all or nothing.
- [`duct-layout`](../../brain/skills/duct-layout.yaml) - the skill: read the connectors, plan each
  unit in its own direction, then those two and `CONNECT_OPEN_ENDS`.

**What is already known, and it is NOT a proof.** Run live through `revit_change` on Project1,
Revit 2024, session 42080, and read back with `select-by-connection-status` each time:
five rooms (50 ducts, 45 fittings - 15 taps, 25 elbows, 5 transitions - 25 joins), then thirty rooms
in six blocks at 0, 45, 90, 180, -90 and -51 degrees (180 runs, 300 ducts, 270 fittings - 90 taps,
150 elbows, 30 transitions - 150 joins), every duct and fitting joined at every end, no diffuser
moved. **What is missing is D-30's record:** no run went through `heron_validate`, nothing carries a
negative case, and neither fragment has a fingerprint. Both are `DRAFT` until the owner accepts one.

*Placed 2026-09-24 with FRAGMENT-ISSUES rows 5b-192 to 5b-194.*

| # | Check | Expected |
|---|---|---|
| **AL1** | `create-duct-runs` through `heron_validate`, on a working copy: two runs, `850x195` then `300x300`, sharing a point | `runsDrawn 2`, `segments` as many as the point pairs, `sized` the same, and the sizes read back from the model - not from the answer |
| **AL2** | `create-duct-runs` with one run of three whose point is written `3075;-2100` instead of `x,y,z` | It THROWS before any duct exists, naming `run 3` and the point; the model as before, re-read. A run that snaps - a size the duct type will not take - rolls back the same way, naming asked and made |
| **AL3** | `fit-mep-joints` through `heron_validate`, on ducts drawn by AL1's arrangement plus a branch started on the main's centreline, with a duct type whose routing preference is Taps | One transition - built into the LARGER run - and one tap; `freeEnds` counts the ends meeting nothing; `select-by-connection-status` finds no open end among what it fitted |
| **AL4** | `fit-mep-joints` on a joint Revit will not fit - a drop shorter than its elbow's leg | Nothing kept anywhere, and the answer names the joint. **Watch which layer refuses:** on 2026-09-23 Revit posted *"modified to be in the opposite direction"* at commit and the add-in rolled back - the fragment's own all-or-nothing throw has never been seen to fire |
| **AL5** | The `duct-layout` skill end to end on a model the owner lays out fresh, one room at an angle nobody has used | The three calls, then `select-by-connection-status` on Ducts and Duct Fittings: 0 open. The route follows his rules in the skill's note - taps, radius elbows, a straight main out of the unit |
| **AL6** | AL1 to AL3 on **Revit 2020** | The same answers. net472 is the older runtime and the routing preferences of the 2020 template may differ - record the duct type used |
