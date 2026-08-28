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

## Members that simply ARRIVED — the class the table above misses

Everything above is something that **changed**. The failure that actually reached this repository was
a member that was **added** partway through the supported range: it does not look like a version
problem, because there is no old spelling and no deprecation warning. It compiles on a new release and
does not exist on an old one.

| Member | Exists from | Use instead, on 2020+ |
|---|---|---|
| `Document.CreationGUID` | **2024** | `doc.ProjectInformation.UniqueId` — created with the document, survives save, rename and move, and present on every release 2020–2027 |

`RevitWrite.DocumentKey()` used `CreationGUID` to pin the document for Golden Rule 20. It had been read
several times, including once by a review that listed it as a *likely* problem spot, and it survived
every reading. The compiler found it in seconds.

**Prefer a member that exists everywhere over a `#if` that hides one that does not.** A branch is two
code paths to keep true forever; `ProjectInformation.UniqueId` is one expression that is simply correct.
That is [D-20](../../../docs/DECISIONS.md) applied to identity rather than units — prefer the thing with
nothing in it for Autodesk to move.

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

- If you are not certain a class, method or enum exists in a target release, **do not assume it because
  the name sounds right — go and look.** Saying "this needs verification in Revit 2026" is the fallback,
  not the first move, because verification takes about two minutes and needs neither Windows nor Revit:

  ```bash
  python tools/check-compile.py                # the whole repository, on all eight releases
  python tools/check-api-surface.py            # every member Heron calls, on 2020 THROUGH 2027
  ```

  The compile gate reaches **2020 through 2027 anywhere**, given an SDK that carries the WindowsDesktop
  MSBuild targets — on Ubuntu `dotnet-sdk-10.0`, not `dotnet-sdk-8.0`, which stops at 2024. It skips a
  release it cannot build and names the package to install; a skip is never reported as a pass.

  The second adds what the first does not: it reads the **shipped** assemblies for all eight releases,
  where a compile reads the NuGet reference packages for the one it is building. It matches by name only
  — a changed signature still needs a real compile — but it closes the missing-member class everywhere.

  For one member rather than the whole repository, read the shipped `RevitAPI.dll` for each release —
  the NuGet reference assemblies under `~/.nuget/packages/` are the real API surface. That is how the
  `CreationGUID` row above was pinned to 2024 exactly, across five releases, rather than guessed at as
  "somewhere after 2020". Online documentation does not reliably say which release introduced what.
- "It builds" is not "it works". A release the add-in has compiled for but never been launched in is
  **built, not proven**, and should be described that way.
- Re-check the newest release's runtime against the Autodesk SDK each year before building for it.
