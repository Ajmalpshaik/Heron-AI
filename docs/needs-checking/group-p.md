# Needs checking — Group P

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group P — parameters, and the two ways the answer is confidently wrong (needs Revit and a real model)

`HERON-REVIT-PAR-011` — [`revit/Heron.Revit.Addin/RevitParameters.cs`](../../revit/Heron.Revit.Addin/RevitParameters.cs).
Compiles on 2020–2027, 0 errors, 2026-09-16. **Proved 2026-09-19 and signed** ([`brain/agent-proofs/HERON-REVIT-PAR-011.yaml`](../../brain/agent-proofs/HERON-REVIT-PAR-011.yaml)) - so it is no longer NEVER RUN. It went signed, then WITHDRAWN, then signed again the same day: the first proof rested on two models and claimed no empty input existed, which [U5](../NEEDS-CHECKING.md#) disproved. The one that stands varies `category` across Ducts, Pipes and Air Terminals, and the last of those IS the empty case. That proof is D-53 tracking and it is NOT these rows: the cases below are still unchecked, and tracking says the answer follows the model, not that any one of them is right. Read-only — no parameter is written, and no
transaction is opened.

Use a real project, not a sample: one with shared parameters loaded, at least one project parameter
bound to a category, and a schedule you already trust. **The schedule is the instrument** — almost every
row below is settled by putting Heron's answer next to a schedule of the same thing.

Two tools, one operation. `revit_parameters(category)` with no parameter named gives the COVERAGE answer
— which parameters these elements carry and how many are filled in. Naming one gives the VALUES.

| ID | Do this | Pass looks like |
|---|---|---|
| **P1** | Ask for the coverage of a category you know well — *"which parameters are filled in on the ducts?"* | Every parameter you can see in the Properties palette appears, plus the type ones. A parameter Revit shows and Heron does not is the finding |
| **P2** | **The row this agent exists for.** Ask for a parameter that lives on the TYPE — Fire Rating on doors, Assembly Code on anything | It comes back with `where` reading **type**, and a count that matches the schedule. A confident *"0 of 340"* here means the type is not being read, and that is the plausible zero this whole file was written against |
| **P3** | Take one length parameter — a duct width, a wall height — and compare `value` with the schedule | **The same string, units and all.** If it reads `0.656` where the schedule reads `200 mm`, `AsValueString` is not doing what this code assumes and every number it has ever given is in decimal feet |
| **P4** | Check the project's units are NOT millimetres, then repeat P3 | The value follows the PROJECT, not a fixed unit. This is the row that catches a conversion baked in by accident |
| **P5** | On a type row in the coverage answer, compare `onElements` with the number of TYPES in that category | They match. A type row reporting 900 against 900 doors means types are being counted once per element — the answer is not wrong, but it is not the number the column says |
| **P6** | Make a schedule of one parameter, sort by it, and count the blanks. Compare with `noValue` + `blank` | The same total. **Then check the split:** `blank` should be the ones holding a space or an unset material, `noValue` the ones nobody ever touched. A schedule cannot tell them apart and this is the only place the difference shows |
| **P7** | Put a single SPACE into a text parameter on one element and ask again | It moves from `noValue` to `blank` — not to filled in. Counting a typed space as data is how a completeness check passes when it should fail |
| **P8** | Load a shared parameter with the SAME NAME as a built-in one and bind it to a category | `sameNameOnOneElement` reads 2 on that row, and asking for it by name reports `ambiguous`. If it silently answers with one of them, the clash detection is not working and writing by name later would hit the wrong parameter |
| **P9** | A category with several thousand elements — the ducts on a real tower | It answers in a few seconds. Both shapes read each TYPE once; if it hangs, the per-type caching is not being hit and every element is re-reading its type |
| **P10** | A category with more than 500 elements carrying a named parameter | `listed` reads 500, `notListed` carries the rest, and the reply says so. A truncated answer that does not admit it is worse than a slow one |
| **P11** | Ask for a category that has no elements at all in this model | It says so in words. An empty answer and *"this model has none of those"* must not read the same |
| **P12** | Ask for a parameter that exists nowhere — a typo | `withoutTheParameter` equals the element count, and the wording says it is probably an unbound project parameter or a different family, not missing data. **That distinction is the whole value of the answer** |
| **P13** | Pin to one project, switch Revit to another, and ask again | Refused, and nothing is read. A completeness report from the wrong building reads exactly like one from the right building |
| **P14** | Check a read-only parameter — Area, Volume, an elevation | `readOnly` is true. If it reads false, a later write agent would try it and fail at the transaction |
| **P15** | Check a Yes/No parameter and a material parameter | The Yes/No prints as Revit prints it; the material prints the material's NAME, not a number. An ElementId pointing at nothing counts as `blank` |

**Why this group exists at all:** *"is it filled in?"* is the question asked before every schedule, IFC
export and hand-over, and today the only way to answer it is a throwaway schedule and a pair of eyes.
The two ways a machine gets it wrong are both silent — reading only the instance when the data sits on
the type, and printing decimal feet where the project says millimetres — and both produce an answer that
is formatted, confident and believed.

**What it deliberately does not do:** write, and report a parameter's data type. `Definition.ParameterType`
is **not in 2023** and `Definition.GetDataType()` is not in 2020, so no single member answers across the
eight releases and [D-05](../DECISIONS.md) does not extrapolate one. `storageType` is the narrower fact
reported instead.

---
