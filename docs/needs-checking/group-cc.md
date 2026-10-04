# Needs checking — Group CC

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CC - building loads from the model: the take-off, the loads written back, and their read-back (2026-10-04)

**`report-space-envelope` (FRG-MEP-058) version 1, DRAFT, and the building loads feature around it**
([docs/44](../44-building-loads-from-the-model.md)). Built 2026-10-04: the take-off reader, the
building runner in `brain/`, the Companion's Loads panel, the load calculation sheet, and Finalize.
**Compiled on 2020 to 2027; nothing below has run in Revit.** The model is the one the owner names -
docs/44 section 10 proposes his L-shaped office, "heron ai bulding" (15 Spaces, 5 zones). Every take-off
run is a READ: run it with `validate` and no `--write`, so Revit refuses any change. Finalize is a
write: run it on a rolled-back setup and undo it.

| # | Run | Look for |
|---|---|---|
| **CC1** | **A window type's U and SHGC** - open one window type's Type Properties, Analytical Properties, beside the take-off's `types` entry for it | `u_w_m2k` and `shgc` are the **same numbers** the palette shows. The U is read as is, on the assumption that Revit's internal unit for it IS W/(m2.K) - **the first thing checked**. A type with no SHGC set reads `null`, never 0 |
| **CC2** | **A wall type's U and absorptance**, and a roof's, the same way | Same numbers as the palette. A Basic Wall whose thermal values come from its layers: say whether Revit fills the type's U at all, or leaves it empty (the brain then refuses that Space, by name) |
| **CC3** | **The U parameter on Revit 2027** - the same window type in 2027 | 2027 removed `ANALYTICAL_HEAT_TRANSFER_COEFFICIENT` and has `ANALYTICAL_THERMAL_TRANSMITTANCE` ("Thermal Transmittance (U)"); the fragment finds whichever exists by name. The U must read the palette's number on 2027, not `null` |
| **CC4** | **True North on a rotated project** - Manage > Position > Rotate True North by 30 degrees, then a wall drawn to face project north | `project_to_true_north_deg` reads 30 or -30, and `heron_takeoff.azimuth_deg` turns that wall to 30 or 330. **Which one matches the drawing fixes the sign** - in `azimuth_deg` only, never in the C# |
| **CC5** | **A corner office** - its outside walls measured by hand on the plan | Two wall faces `beyond: outside`, facing two different ways; each face's `area_m2` against the hand take-off (finish face, floor to the Space's top) within 2 %; each window's `area_m2` its width x height |
| **CC6** | **A Space under the roof** and **the corridor wall between two offices** | A `top` face `beyond: outside`; the corridor wall `beyond: space` with the OTHER Space's id |
| **CC7** | **A Space not enclosed** (Not Enclosed in the Space schedule) - THE NEGATIVE CASE | `placed: false` and `faces: []` - never a zero-area row WITH faces. The brain lists it and leaves it out |
| **CC8** | **A curtain wall** on an outside face | Its panels as `curtain_panel` openings with Revit's own panel areas, and the curtain wall's own face netting to nothing. If the mullions leave a sliver of face whose type has no U, say so - that Space would refuse |
| **CC9** | **A Space bounded by a Room Separation line**, and **a Space whose ceiling is not room-bounding** | The separator face reads `beyond: unknown` with a WARN (nothing is behind it), not `outside`; the unbounded ceiling: say what the top face reports |
| **CC10** | **A boundary in a linked model** (walls in an architectural link) | A finding naming the linked boundary, and that face `beyond: unknown` - never `outside` by guess |
| **CC11** | **Finalize, in W and L/s** - on a rolled-back setup | Design Cooling Load, Design Heating Load and Specified Supply Airflow on each calculated Space read back through READ_SPACE_LOADS and REPORT_SPACE_AIRFLOW as the values the panel showed. **Two undo entries**: the Space values, then the diffusers. A Space edited in Revit between the read and Finalize refuses the WHOLE first write |
| **CC12** | **Finalize in a model set to Btu/h and CFM** (Manage > Project Units) | The take-off's `units` read `Btu/h` and `CFM` - **check the exact symbols Revit prints**; `finalize_rows` converts to them, and a symbol it does not know (BTU/h, cfm?) refuses the write by name. Read back the same numbers |
| **CC13** | **The diffusers' flows** after Finalize | SET_AIR_TERMINAL_FLOW, chained after FILTER_ELEMENTS_BY_ID, writes each Space's flow split equally across the air terminals Revit places in that Space (`FamilyInstance.Space` in the active view's phase). A terminal Revit puts in no Space is not written |

**Then sign.** CC5 and CC6 are the positive case and CC7 the negative for D-30; CC1 to CC4 and the
Space schedule's own Area and Volume are the second look. The fragment moves to PROVEN only with
Ajmal PS's signature on that evidence.
