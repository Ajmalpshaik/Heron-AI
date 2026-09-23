# Fragment issues — section 3g-ii

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 3g-ii. WHAT BLOCKS THE REMAINING 172, DERIVED RATHER THAN ASSUMED — 2026-09-10

After a night of proving fragment by fragment, the obvious question is what is actually left. This is
computed from the contracts and from `FromRequest` in
[`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs), not estimated.

| Count | What blocks it |
|---|---|
| **97** | **Arrangeable — blocked only by CONTENT or by a hard negative** |
| **67** | A request value that cannot be supplied ([D-54](../DECISIONS.md)) |
| 3 | Needs TWO element sets, and one selection cannot say which is which |
| 3 | Its only declared result is a bare `ElementId`, skipped as a helper object |
| 1 | Its only declared result is a `bool` — neither leg can be non-empty |
| 1 | No declared result at all |
| **172** | |

### The refused values are ONE problem wearing several hats

82 refused request values across 27 types, and two of them are half of it:

| Count | Type | Example |
|---|---|---|
| 28 | `ElementId` | `add-revision-cloud.revisionId` |
| 14 | `Element` | `align-elements.reference` |
| 4 | `OverrideGraphicSettings` | `apply-view-filter.overrides` |
| 3 each | `View3D`, `IList<Element>`, `IList<ElementId>`, `ICollection<ElementId>`, `Color` | `create-view-filter.categoryIds` |

**`ElementId` and `Element` together are 42 of the 82.** That is D-54's own claim, now counted: naming
one particular element in the model is the single most-wanted thing a caller cannot say.

### A first pass at this said 111, and it was wrong

The first attempt assumed only `string`, `double`, `int` and `bool` could be supplied, and reported
**111** blocked on values. `FromRequest` resolves far more than that by name — `View`, `Level`,
`Category`, `BuiltInCategory`, `FamilySymbol`, `WallType`, `FloorType`, `CeilingType`,
`MEPCurveType`, `FilledRegionType`, `Phase`, `IList<XYZ>`, `IList<string>` and the `ICollection` /
`IEnumerable` / `List` forms of each. Every one of those was used in a job file tonight:
`view=1 - Mech`, `categories=Ducts`, `points=0,0,0; 6000,0,0`.

> The count was wrong because the resolver was assumed instead of read. **Read `FromRequest` before
> claiming a type cannot be supplied.**

`Element` and `ElementId` are the conditional case, and the condition is §5 row 13's fix: they resolve
only when the need NAME ends in `Type`, because a type can be found by name and one particular
instance cannot.

### What this means for the next proving session

**The binding constraint is no longer the models.** 97 of 172 are arrangeable as the harness stands —
more than half — and tonight's four unblocked fragments all came out of that group by reading a triage
row and going to the model that had what it named.

The 67 are a different kind of work: they need value passing, not a better model, and no job file can
route around them.

---
