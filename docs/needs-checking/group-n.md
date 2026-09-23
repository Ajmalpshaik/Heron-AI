# Needs checking — Group N

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group N — duct and pipe systems, and what is connected to nothing (needs Revit and a real MEP model)

`HERON-REVIT-SYS-030` — [`revit/Heron.Revit.Addin/RevitSystems.cs`](../../revit/Heron.Revit.Addin/RevitSystems.cs).
Compiles on 2020–2027, 0 errors, 2026-09-16. **Tracked against two models 2026-09-19 and signed** ([`brain/agent-proofs/HERON-REVIT-SYS-030.yaml`](../../brain/agent-proofs/HERON-REVIT-SYS-030.yaml)) - so it is no longer NEVER RUN. That proof is D-53 tracking and it is NOT these rows: the cases below are still unchecked, and tracking says the answer follows the model, not that any one of them is right. Read-only — nothing is connected, renamed
or put on a system, and no transaction is opened.

Use a real MEP job, not a sample: one with duct AND pipe systems, and ideally one you already know has
a loose element in it. **A clean model proves the least here** — the whole row is about what is missing
from a total, and a model with nothing missing cannot show that.

**The distinction the whole group turns on:** an OPEN CONNECTOR is normal — the end of every run is one,
and a stub waiting for next week's coordination is one on purpose. An element CONNECTED TO NOTHING in
any direction is not, and it is on no system by definition.

| ID | Do this | Pass looks like |
|---|---|---|
| **N1** | Open an MEP model and ask Heron *"what systems does this model have?"* | Every system in the System Browser is listed, with the same names, and marked `duct` or `pipe` to match. A system Revit shows and Heron does not is the finding |
| **N2** | Compare each system's `elements` against what the System Browser shows for it | The same number. This is the one that says whether `MEPSystem.Elements` means what the browser means |
| **N3** | Add `elements` across every system and compare with `mepElementsExamined` | They will NOT match and that is expected — an element can be on no system, and fittings may report differently. **What matters is the direction:** examined should be the larger. If it is smaller, elements are being counted on two systems |
| **N4** | Draw one duct on its own, joined to nothing, and ask again | It appears in `connectedToNothing`, with its category and its level. **This is the row the agent exists for** |
| **N5** | Take a properly connected run and check it does NOT appear in `connectedToNothing` | Only its end connectors are open, so it counts in `withAnOpenConnector` and not in the loose list. A run that appears in both is a false positive and makes the whole answer noise |
| **N6** | A model with no MEP at all — an architectural or structural file | It says so in words rather than returning an empty list. An empty answer and *"this file has no MEP in it"* must not read the same |
| **N7** | Check `reportedNoConnectors` against what you expect | Some MEP families genuinely have none. A large number here means the connector question is being asked of things that cannot answer it, and `mepElementsExamined` is then the wrong denominator for N3 |
| **N8** | A duct connected only to itself, or a fitting whose connectors reference its own owner | It is still reported as connected to nothing — `IsJoined` skips references back to the same owner. If it comes back joined, the self-reference check is not doing its job |
| **N9** | Compare `onNoSystem` with `connectedToNothingCount` | `onNoSystem` should be the LARGER of the two, and the gap is the interesting number: an element joined to its neighbour but sitting on no system is a half-built run. If they are equal on a real job, either every loose element is also the only unsystemed one — possible — or `MEPSystem.Elements` is not returning what the System Browser shows, which N2 is what settles |

**Why this group exists at all:** a duct that LOOKS joined on screen and is not carries no flow, appears
on no system, and is missing from every number downstream — the schedule prints, the sizing calculates,
the System Browser shows a tidy tree, and none of them mentions it. On a federated Qatar job that is the
difference between a riser that balances and one that is found on site.

**What it deliberately does not do:** judge. It reports counts and one list, and asserts nothing about
what Revit considers a valid system. The compiler agreed about every name it calls and can say nothing
about what any of them returns.

**2026-09-22 - N2, N4, N5 and N9 answered, three of them by defects, and what the fix changes under
this group.** Before the fix, on `Project1` (3,420 elements), Revit 2024, session 17492:

| Row | What the model showed | What it meant |
|---|---|---|
| N2 | `Mechanical Supply Air 1 - 0 element(s)`, while both ducts' own `System Name` read that system | `MEPSystem.Elements` is terminals only, in Autodesk's own words. **A defect** - [FRAGMENT-ISSUES](../FRAGMENT-ISSUES.md) row 171 |
| N9 | `onNoSystem 3`, equal to `mepElementsExamined 3` - as it was, 34 and 34 and 16 and 16, in the 2026-09-19 proof | The equality this row warned about, and N2 is what settled it |
| N5 | `withAnOpenConnector 0` on a run with two open ends; the `find-dead-ends` fragment walked the same two ducts and found both | `IsJoined` counted each duct's reference to its own system as a join. **A defect** - row 172 |

**After the fix** - fingerprint `7ad8b6c5570fce82`, built and deployed for 2020, 2024 and 2027 - on
`test projject` (Revit 2024, session 45408, 3,565 elements) and `PIPE` (Revit 2020, session 12684,
3,332 elements):

| Row | Result |
|---|---|
| N2 | **PASS for the run, by a second route.** Every system's `elements` equals the number of elements whose own `System Name` names it: all nine on `PIPE` (37 in all, from 25 pipes and 12 fittings - Hydronic Supply 2 is 6 and 4, and says 10), all ten on `test projject` (one duct or pipe each). **The System Browser half is NOT done**: neither model has a terminal or base equipment on a system, so `components` is 0 throughout and has not been held against anything yet |
| N4 | **PASS.** `test projject` holds ten single ducts and pipes joined to nothing, and all ten are listed in `connectedToNothing` with category and level. Each is ALSO on a system Revit made for it alone - Mechanical Supply Air 1 to 8, Hydronic Supply 1 and 2 - which is exactly why the old rule could not see them |
| N5 | **PASS.** `PIPE`: `withAnOpenConnector 19`, and `find-dead-ends` over the same 37 pipes and fittings counted 19 elements with an open end - the same unit, by a different mechanism: it asks each connector's own `IsConnected`, where this agent walks `AllRefs`. The totals agree; the two lists were not compared element by element |
| N9 | **Premise withdrawn** - see below. On `test projject`, `onNoSystem 6` against `connectedToNothingCount 16`: the six equipment units are both, and the ten ducts and pipes only the second. `read-mep-system`, run separately, puts the same six on no system and the ten on one each |

**What the fix changes under these rows, so they are read right from here on:**

- **`elements` counts the run now.** Each system also reports `network` (its ducts or pipes and their
  fittings) and `components` (its terminals and base equipment), each element once, adding up to
  `elements`. `components` is the number N2 should hold against the System Browser.
- **N3's direction is no longer guaranteed.** One piece of base equipment can serve several systems, so
  `elements` summed across systems can exceed `mepElementsExamined` with nothing counted twice inside
  any one system. On both models above the sum equalled `mepElementsExamined` exactly, with no
  equipment on any system.
- **N9's premise is withdrawn, and so is this group's opening sentence.** *"It is on no system by
  definition"* was reasoned, never counted - and `test projject` counts the opposite: ten elements joined
  to nothing, every one of them on a system. The two are separate counts now and neither is inferred from
  the other.
- **A join is physical.** A connector's reference to its own duct or pipe system is not counted as one;
  an electrical circuit's still is, on purpose, because dropping it would list every light fitting on a
  circuit as connected to nothing.
- **The 2026-09-19 proof is stale** - the source moved under it - and is replaced by a new tracking run
  on the two models above ([`HERON-REVIT-SYS-030.yaml`](../../brain/agent-proofs/HERON-REVIT-SYS-030.yaml)).
  That draft's standard sentence says no negative case could be run for this agent. **One was**:
  `onNoSystem 0` on `PIPE`, where every element names a system.

---
