# platform/ — Part 4, Heron Platform

**The Kernel and the machinery around it.** Everything depends on this; it depends on nothing.

| | |
|---|---|
| Language | C# today; Python later for the outside-Revit half |
| Runs | wherever it is referenced |
| Rule | **plumbing, never intelligence** |

## What's here

One project, `Heron.Core`. Nine classes, each the Kernel half of an agent in
[the registry](../docs/28-agent-registry.md):

| Class | Does | Step |
|---|---|---|
| [`HeronPaths.cs`](Heron.Core/HeronPaths.cs) | **The Path Manager** — the single place that knows where anything lives. Product, data and derived are separate in code, and `IsSafeToDelete` returns false for anything under either | 1 |
| [`HeronConfig.cs`](Heron.Core/HeronConfig.cs) | The Configuration Manager. `Load` and `Save`, and deliberately **no** `ApplyFromRequest` | 1 |
| [`HeronIdentity.cs`](Heron.Core/HeronIdentity.cs) | Stable identity for the things Heron must be able to name twice | 1 |
| [`HeronOperationRegistry.cs`](Heron.Core/HeronOperationRegistry.cs) | The Tool Registry — every operation Heron will run, and the risk level of each | 3 |
| [`HeronAudit.cs`](Heron.Core/HeronAudit.cs) | The audit trail: one append-only line per request, keyed by Workflow ID | 4 |
| [`HeronLease.cs`](Heron.Core/HeronLease.cs) | Who currently holds a Revit, and who may therefore send it anything ([D-22](../docs/DECISIONS.md)) | 6 |
| [`HeronPermissions.cs`](Heron.Core/HeronPermissions.cs) | The seven permission levels of [docs/12 §1](../docs/12-security-and-permissions.md), in order | 6 |
| [`HeronStop.cs`](Heron.Core/HeronStop.cs) | Emergency Stop — one switch that stops Heron doing anything further | 6 |
| [`HeronUnits.cs`](Heron.Core/HeronUnits.cs) | Unit conversion. Millimetres, which is what the user says, to whatever Revit wants | 6 |

## What will be here

Installer · update system · package manager · event bus · workflow engine ·
the remaining registries · secret store.

## Rules for this folder

1. **No BIM knowledge, ever.** If a duct is mentioned in here, the boundary has been crossed. The
   Kernel should compile and pass its tests with no Revit knowledge present at all.
2. **No dependencies on other parts.** `platform/` is the bottom of the stack.
   Enforced by `tools/check-structure.py`.
3. **`HeronPaths` is the only thing that builds a Heron path.** Product, data and derived are separated
   in code rather than in prose, and `IsSafeToDelete` returns false for anything under data — so an
   update or a cleanup cannot reach the user's knowledge. [docs/06 §2](../docs/06-heron-platform.md)
4. **Configuration is a security boundary.** Unknown keys are ignored, not adopted. There is a `Load`
   and a `Save` and deliberately no `ApplyFromRequest` — nothing Heron *reads* may change what Heron
   *is*. [Golden Rule 19](../docs/14-golden-rules.md)

## Fix things here when

Something is written to the wrong place · config is not honoured · an update overwrote user data ·
identity or correlation ids are wrong.
