# Needs checking — Group BZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group BZ - every setting of a family connector already made, Global included, and a list of a family's connectors (2026-10-04)

**The owner's PC, Revit 2024, session 8804, on `TRG_PLMB_VLV_PPR Gate Valve_GV_R0` with his word in
the chat** (*"you can change it but ... bring back to ... the global"*), every run ROLLED BACK. Read
before and after: two pipe connectors, both Global, 32 mm driven by Nominal Diameter, Port Left primary,
not linked - the same both times. Row [5b-307](../fragment-issues/section-5b-rows-176-200.md).

| # | Run | Look for |
|---|---|---|
| **BZ1** | `set-family-connector-roles`, setup `add-family-connector` (Domestic Cold Water on Port Left), connectors `all`, settings `system=Global` | **MEASURED 2026-10-04:** the new connector read Global, the two already Global said so. Negative `system=Supply Air`: refused per connector with the systems a pipe connector takes. Record `brain/proof-drafts/runs/set-family-connector-roles.json` - **unsigned** |
| **BZ2** | connectors `Port Right`, settings `flowDirection=Out; flowConfiguration=Preset; sizeParameters=Nominal Diameter; description=Outlet; flip=true`, primary `Port Right`, links `Port Left > Port Right` | **MEASURED 2026-10-04:** flow Out, Preset, described Outlet, pointing left (-X) where it pointed right, PRIMARY, linked both ways. Negative `Port Middle` refused naming the connectors |
| **BZ3** | an electrical connector (Power Balanced) and `system=Global` | **MEASURED 2026-10-04: Revit 2024 ACCEPTS it** and reads back Global - the brief expected a refusal. Kept as a case so a release that refuses it shows |
| **BZ4** | `add-family-connector`, system `Global` on the Pipe Accessories valve | **MEASURED 2026-10-04:** a pipe connector reading Global, the domain taken from the category. Negative `Globl` refused |
| **BZ5** | `report-family-connectors` on the valve; negative in Project1 | **MEASURED 2026-10-04:** both connectors with system, size and driver, flow, pointing, position and plane; Project1 `notAFamily`. Record `brain/proof-drafts/runs/report-family-connectors.json` - **unsigned** |
| **BZ6** | Loss Method and Loss Coefficient / Pressure Drop on a duct connector of a Mechanical Equipment family | **NOT RUN.** The valve's pipe connectors show no Loss Method in the read; whether RBS_*_FITTING_LOSS_METHOD_PARAM is the connector's own Loss Method is unmeasured |
| **BZ7** | BZ1 to BZ5 on **Revit 2020** | The same answers; FlipDirection and the System Classification setter are in its reference assemblies, not yet run there |
