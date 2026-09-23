# Needs checking — Group M

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group M — phases and design options (needs Revit and a real job file)

`HERON-REVIT-PHS-032` — [`revit/Heron.Revit.Addin/RevitPhases.cs`](../../revit/Heron.Revit.Addin/RevitPhases.cs).
Compiles on 2020–2027, 0 warnings, 2026-09-15. **Tracked against two models 2026-09-19 and signed** ([`brain/agent-proofs/HERON-REVIT-PHS-032.yaml`](../../brain/agent-proofs/HERON-REVIT-PHS-032.yaml)) - so it is no longer NEVER RUN. That proof is D-53 tracking and it is NOT these rows: the cases below are still unchecked, and tracking says the answer follows the model, not that any one of them is right. Read-only — nothing here sets a view's
phase or activates an option, because both change what everybody else on the job sees in that view.

Use a real job file: one with Existing and New Construction at least, and ideally one design option set.
A single-phase model with no options proves nothing, and the tool says so rather than pretending.

| ID | Do this | Pass looks like |
|---|---|---|
| **M1** | Open a phased model and ask Heron *"what phases does this model have?"* | Every phase in Manage → Phasing is listed, in the same order. Order matters: Existing comes before New Construction and a list that reorders them is the finding |
| **M2** | Add the `created` counts across all phases, plus `withNoPhase` | It should equal `placedElements`. If it does not, an element is being counted in two phases or in none, and the breakdown cannot be trusted |
| **M3** | Compare `placedElements` against `count_elements` for the same model | **The same number.** Both claim to collect placed elements and exclude types |
| **M4** | A model with a design option set holding two options | Both options are listed, each naming its set, and exactly one is marked `primary` |
| **M5** | Add the `elements` across all options, plus `inMainModel` | It should equal `placedElements`. This is the check that says whether the breakdown is a partition or an overlap — **and the answer deliberately asserts nothing about how a collector treats a non-primary option**, so M5 is what settles it |
| **M6** | Note the active view, then check `viewPhase` and `viewPhaseFilter` | They match what the view's Properties palette shows. A number read off a plan has been through both |
| **M7** | Switch the active view to one on a different phase and ask again | `viewPhase` changes and the phase COUNTS do not — the counts are of the model, not of the view. If the counts move, the collector is view-scoped and the whole answer means something else |
| **M8** | A model with demolished elements | `demolished` is non-zero on the phase they were demolished in, not on the one they were built in |

**Why this group exists at all:** the same reason as Group L. A count given without its phase and its
design option is a number a person will act on. On a real job the Existing phase holds the survey, New
Construction holds the work, and an option set can hold two complete alternative arrangements of the
same shafts — and *"all ducts"* means something different in each.

---
