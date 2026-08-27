# platform/ — Part 4, Heron Platform

**The Kernel and the machinery around it.** Everything depends on this; it depends on nothing.

| | |
|---|---|
| Language | C# today; Python later for the outside-Revit half |
| Runs | wherever it is referenced |
| Rule | **plumbing, never intelligence** |

## What's here

| Project | Does |
|---|---|
| `Heron.Core` | `HeronPaths` (product / data / derived), `HeronConfig`, `HeronIdentity` |

## What will be here

Installer · update system · package manager · event bus · workflow engine · registries ·
secret store · permission manager.

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
