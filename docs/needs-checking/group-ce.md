# Needs checking — Group CE

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CE - sprinkler hydraulics from the model: the network read, the K-factors, and the solve (2026-10-04)

**Built 2026-10-04 from [docs/46](../46-sprinkler-hydraulics-from-the-model.md) and
[its plan](../work-notes/plans/sprinkler-panel-2026-10-04.md). REPORT_SPRINKLER_NETWORK is DRAFT and
compiles on Revit 2020, 2024 and 2027; nothing below has run in Revit.** The read's arrangement is in
[`tools/jobs/report-sprinkler-network-2026-10-04.yaml`](../../tools/jobs/report-sprinkler-network-2026-10-04.yaml),
read back with `python tools/batch-prove.py <file> --dry-run`. **The model is the owner's to name**: one
wet sprinkler system with its alarm valve as base equipment, and a domestic water system beside it that
shares no pipe. Every case reads only; nothing in this group writes to Revit.

| # | Run | Look for |
|---|---|---|
| **CE1** | **REPORT_SPRINKLER_NETWORK, the positive** - the wet system by its name | `elementCount` equals the System Browser's count for the system plus its base equipment; one pipe's Inside Diameter and Length in Properties equal its `inner_diameter_m` and `length_m`; every sprinkler has a connector joined to a pipe or a fitting. Then `heron_sprinkler_takeoff.qa()` on the JSON: no FAIL but the K-factors |
| **CE2** | **The D-30 negative** - the domestic water system's name, then `*` with a pipe of the WET system selected | The name: system null, nothing read, a finding names it. The selection: not one domestic water element in the network |
| **CE3** | **The K-factor read-back** - one sprinkler type's Properties open | `k.parameter` is the text the palette shows for its K-Factor; `k.connector` is a raw number. **Write down what unit `AssignedKCoefficient` is in** - Heron never converts it, and knowing it would let the page offer it |
| **CE4** | **A tap on a main** - a branch joined to the main with a tap (spud) fitting | The main has a connector of type `curve` at the tap; the brain splits the main there and its two parts add up to the main's length |
| **CE5** | **The fitting centre length** - read one elbow's location point and its two connector points | `at` is the elbow's insertion point; the length the brain adds to each pipe is the distance from it to that connector |
| **CE6** | **The whole flow in the Companion** - "run the hydraulics on this sprinkler system" in a chat | The Sprinkler panel opens with the system's 3D view; after the standard, the criteria, the K-factors and the fittings are answered and Suggest pressed, Calculate solves; the sheet writes HTML, CSV and, with Edge, a PDF |
| **CE7** | **The solve against a listed hydraulic program** - the same remote area on the same model, calculated by a listed program | Demand and pressure at the source within 2 %. Until this row is done, the sheet says it is not a listed program, and that stays true after it |

**Then sign** REPORT_SPRINKLER_NETWORK on CE1 and CE2. It also needs **one store row** on the owner's PC
before a chat can route to it there ([the put_fragment recipe](../../brain/heron_scope.py)).
