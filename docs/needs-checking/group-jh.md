# Needs checking — Group JH

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JH - a duct or pipe fitting built by the method end to end, and the four fitting facts the research left UNSURE: `mep-fitting-family-creation` deepened (2026-10-28)

The owner asked on 2026-10-28 for Heron to be *"very advanced in fittings creation"*. Every tool a fitting
needs was already there and most were PROVEN; the method was thin. It was rewritten
([`mep-fitting-family-creation`](../../brain/skills/mep-fitting-family-creation.yaml)) from a research pass
recorded in [43 §10, *Fittings in depth*](../43-building-revit-families.md), with the routing groups and Part
Types read in the API XML of all eight releases.

**What is done and what is not.** The skill and the doc pass every gate. **Nothing here has run in Revit.**
The skill is DRAFT, as every family skill is, until a fitting has been built with it and a negative case
watched (D-30). The tools it calls keep their own status; this group proves the METHOD - that following it
gives a fitting a run takes - and settles the facts the research could not.

**The models - scratch, never a working project.** Revit 2020 first, the owner's release. A new family from
Revit's own metric pipe fitting template (or a Generic Model changed to Pipe Fittings) saved as
`%TEMP%\heron-work\group-jh\JH Elbow.rfa`; a second for the duct work, `JH Duct Elbow.rfa`; and a project from
Revit's own metric mechanical template saved as `%TEMP%\heron-work\group-jh\JH Runs.rvt`. **Revit's own
fittings** for JH1 and JH2 are whichever of the installed library's generic pipe elbow and rectangular duct
tap the owner picks - their file names differ by release and are not guessed here; write the one used in the
results.

| # | Run | Look for |
|---|---|---|
| **JH0** | *Open.* `JH Elbow.rfa` in front in Revit 2020, nothing of the owner's open | The title bar reads *JH Elbow* |
| **JH1** | *Step 1 of the method, on Revit's own pipe elbow, read only.* Open it; `REPORT_FAMILY_TEMPLATE`, `REPORT_FAMILY_CONNECTORS`, `REPORT_FAMILY_SIZE_TABLE`, `REPORT_FAMILY_FORMS`; close it without saving | Category Pipe Fittings, Part Type Elbow; **2** connectors, both Fitting, one PRIMARY pointing along X, each Angle reading *(<- Angle)*, each diameter driven by a parameter. **Any of these reading otherwise is a finding against the skill's kind table and step 8** - write it in the results and change the skill to what Revit's own does |
| **JH2** | *The tap, read only - settles 43 §10's tap row.* The same four reads on Revit's own rectangular duct tap | Part Type Tap - Perpendicular or Tap - Adjustable, and **how many connectors**, which is primary, and where each points. The count decides the skill's tap row |
| **JH3** | *The positive - build a pipe elbow by the method.* In `JH Elbow.rfa`, steps 3 to 8 with Nominal Diameter, Nominal Radius, Angle and a three-row table (25, 50, 100 mm), centre-line radius `Nominal Diameter * 1.5` | Step 9's three reads: `CHECK_FAMILY_STANDARDS` no FAIL; `CHECK_FAMILY_SIZE_TABLES` no table, column or row problem; `REPORT_FAMILY_CONNECTORS` 2 Fitting connectors, one primary on X, both *(<- Angle)* |
| **JH4** | *Flex - step 10.* `FLEX_FAMILY` at `Nominal Diameter=25 mm; Angle=11.25° \| Nominal Diameter=100 mm; Angle=90° \| Nominal Diameter=50 mm; Angle=45°` | Every trial solves, every form and connector follows; the family comes back as it was |
| **JH5** | *In a run.* Save; `LOAD_FAMILY` into `JH Runs.rvt`; `REPORT_ROUTING_PREFERENCES` on a pipe type, then `SET_ROUTING_PREFERENCE` putting *JH Elbow* in Elbows; the owner draws a pipe at 25, 50 and 100 mm turning 90° and 45° | An elbow at every turn, sized to the pipe; `FIND_DEAD_ENDS` on the runs names no open end at an elbow |
| **JH6** | *The negative - the D-30 negative.* Back in `JH Elbow.rfa`, `LINK_FAMILY_PARAMETER` on "Pipe Connector", `Angle=none` (the link removed), rolled back; then the same in a saved copy, loaded, and a 45° turn drawn | `REPORT_FAMILY_CONNECTORS` no longer reads *(<- Angle)*; in the run the 45° turn gets no elbow or the wrong one - the skill's fault table's second row. **If the run still takes the elbow at 45°, the skill's claim that an unlinked Angle stops the flex is wrong** and is struck |
| **JH7** | *A rectangular duct elbow, not square.* `JH Duct Elbow.rfa`, Duct Fittings, Part Type Elbow, ports sized by Width and Height; `FLEX_FAMILY` at `Width=600; Height=300 \| Width=300; Height=600` | Each port reads 600 x 300 then 300 x 600 in `REPORT_FAMILY_CONNECTORS` - never turned through 90° |
| **JH8** | *Allow Slope Adjustments on a Fitting connector - settles 43 §10's slope row.* In `JH Elbow.rfa`, `SET_FAMILY_CONNECTOR_ROLES` `allowSlopeAdjustments=yes` on both, rolled back | Either *slope adjustments allowed* is read back on a Fitting connector, or the tool refuses with Revit's words. **Write which** - the forum answers say the box is hidden while the system is Fitting |
| **JH9** | *Where a flange is offered - settles 43 §10's flange row.* In `JH Runs.rvt`, Revit's own pipe flange loaded; the pipe type's Routing Preferences dialog opened by the owner | Which group's list offers it (Unions, Mechanical Joints, or another). Write it in the results |

**Then sign**, on the owner's word only: the skill's `heron-status` is set to PROVEN in a change of its own once
JH3, JH5 and JH6 pass, naming this group. A row that fails is a finding against the skill, written in the
results and corrected in the skill and 43 §10; nothing in a row changes to make it pass.
