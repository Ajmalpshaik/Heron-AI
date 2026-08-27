---
name: revit-version-support
description: Use for any question about which Revit release needs which .NET runtime, how to build Heron for a given release, and every Revit API difference between 2020 and the latest supported version. Covers net472 / net48 / net8.0-windows / net10.0-windows, the compile symbols this repository defines, and the breaking changes at 2021 (units), 2022 (parameter specs and tags), 2024 (ElementId became 64-bit), 2025 (dimension subclasses), 2026 (manifest isolation) and 2027 (.NET 10, all-user add-in path). Trigger on "which .NET for Revit 20xx", "does this work in 2020", "build for all versions", "why will my add-in not load", "target framework", "ElementId IntegerValue", or any cross-version question.
---

# Revit Version Support

Heron supports **Revit 2020 through the latest confirmed release**, permanently. That promise is
[D-05](../../../docs/DECISIONS.md), and it is why this file exists: a change is not finished when it
works in one release.

## Runtime per release

| Revit | .NET | Target framework |
|---|---|---|
| 2020 | .NET Framework 4.7.2 | `net472` |
| 2021 – 2024 | .NET Framework 4.8 | `net48` |
| 2025 – 2026 | .NET 8 | `net8.0-windows` |
| 2027 | .NET 10 | `net10.0-windows` |
| 2028 and later | **unknown** | **confirm against the Autodesk SDK first** |

An assembly built for one framework family will not load in another. These are four separate builds
from one source tree.

> **Never extrapolate the table forward.** Autodesk moved the runtime at 2025 and again at 2027. A
> release this table does not list is an error, not a guess.

`Directory.Build.props` enforces that: an unlisted version fails the build with a plain-English message
telling you to confirm the target in the Autodesk SDK and add a row. That guard replaced a condition
reading `>= 2027`, which silently claimed every future release was .NET 10 and would have produced an
add-in that does not load, with no warning at build time.

## Building

One property selects the release:

```bash
dotnet build revit/Heron.Revit.Addin/Heron.Revit.Addin.csproj -p:RevitVersion=2024
```

The Revit API assemblies resolve from NuGet by default. To use the copies from an installed Revit
instead — offline, or to match an exact machine:

```bash
dotnet build revit/Heron.Revit.Addin/Heron.Revit.Addin.csproj -p:RevitVersion=2024 -p:RevitApiDir="C:/Program Files/Autodesk/Revit 2024"
```

Never ship the Revit API assemblies. They are Autodesk's, are not redistributable, and Revit supplies
them at runtime. The project references them with `Private=false` so they are never copied to output.

`tools/setup.ps1` discovers which Revit releases are installed and builds for each. It discovers rather
than checking a fixed list of years, so a newly released Revit is found rather than silently skipped.

## Install path

| Revit | Per-user | All-user |
|---|---|---|
| 2020 – 2026 | `%APPDATA%\Autodesk\Revit\Addins\<version>\` | `C:\ProgramData\Autodesk\Revit\Addins\<version>\` |
| 2027 and later | unchanged | `C:\Program Files\Autodesk\Revit\Addins\<version>\` |

**Heron always installs per-user.** It works on every release, needs no administrator rights — which
matters on a locked-down corporate machine — and is unaffected by the 2027 move.

## Compile symbols

`Directory.Build.props` defines `REVIT<year>` for the release being built, plus a cumulative
`REVIT<year>_OR_GREATER` for each supported release from 2024 up.

> A `_OR_GREATER` symbol only works if the project actually defines it. Reference an undefined one and
> the `#if` silently takes the wrong branch, with no warning. If you add a release to the table, add its
> symbol in the same commit.

## Breaking API changes

| Release | What changed | Old | New |
|---|---|---|---|
| 2021 | Units | `DisplayUnitType` | `UnitTypeId` / `ForgeTypeId` |
| 2022 | Parameter and spec type | `ParameterType` | `SpecTypeId` / `ForgeTypeId` |
| 2022 | Tag references | `TaggedElementId`, `LeaderEnd`, `HasElbow` | `GetTaggedElementIds()`, `GetLeaderEnd(ref)`, `HasLeaderElbow()` |
| 2024 | Element id storage | `IntegerValue` (int) | `Value` (long) — `IntegerValue` is deprecated and **throws** above 32 bits |
| 2024 | Built-in category and parameter enums | 32-bit | 64-bit — old int casts throw |
| 2025 | Dimensions | one `Dimension` class | `LinearDimension`, `RadialDimension`, `ArcLengthDimension`. Exact-type checks fail |
| 2026 | Add-in isolation | — | `<ManifestSettings>` in the manifest. ⚠ Read by **Revit 2025 or older this crashes Revit** — it must be stripped per release |
| 2027 | Runtime | .NET 8 | .NET 10 |
| 2027 | All-user add-in path | `ProgramData` | `Program Files` |

Also removed or deprecated at 2027: AXM import, several `Mechanical.Zone` members, legacy rebar
creation methods, and several `EnergyDataSettings` properties.

### Two rules that follow

**Never store an element id as `int`.** Use `long`, or a string in any report or export. A 2024+ id
overflows an int column silently.

**The 2026 manifest setting is a trap for the deploy script.** `tools/deploy-addin.ps1` currently copies
one manifest to every release. That is safe today because the manifest carries no version-specific
settings. The moment one is added, the copy must become per-release or it will crash older Revits.

## Where a version branch is allowed to live

Version-conditional code belongs **only in an adapter** — never in core logic. Put the branch in a small
helper and call the clean helper from everywhere else, so adding a release means editing one file rather
than hunting through twenty.

```csharp
internal static long GetIdValue(ElementId id)
{
#if REVIT2024_OR_GREATER
    return id.Value;
#else
    return id.IntegerValue;
#endif
}
```

This is the adapter boundary from [docs/16](../../../docs/16-version-support-strategy.md), and
`tools/check-structure.py` enforces that `Autodesk.Revit` appears only inside `revit/`.

## Honesty rules

- If you are not certain a class, method or enum exists in a target release, **say so** — "this needs
  verification in Revit 2026" — rather than assuming it because the name sounds right.
- "It builds" is not "it works". A release the add-in has compiled for but never been launched in is
  **built, not proven**, and should be described that way.
- Re-check the newest release's runtime against the Autodesk SDK each year before building for it.
