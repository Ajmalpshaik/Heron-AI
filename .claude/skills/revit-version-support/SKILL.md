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

**The table is the BUILD target, and a running Revit 2026 can be on .NET 10.** Measured 2026-10-17 on the
owner's laptop: Revit **2026.5** - every `*.runtimeconfig.json` in its install folder says `net10.0`, and
the running process loaded `coreclr.dll` from `Microsoft.NETCore.App\10.0.12`. Heron's `net8.0-windows`
2026 build loaded and ran on it (roll-forward), so the 2026 row's target is unchanged until the Autodesk
SDK says otherwise. What it does change: the framework assemblies another add-in sees as "preloaded" on
that Revit are 10.0, not 8.0.

**On every .NET release (2025 on) all add-ins share Revit's default load context, so an assembly NAME is
loaded once, by whichever add-in asks first.** On .NET Framework (2020-2024) two versions of one assembly
load side by side. Measured 2026-10-17
([row 5b-575](../../../docs/fragment-issues/section-5b-rows-401-425.md)): the owner's AJ Tools add-in
loaded the C# compiler 4.14 before Heron, Heron's own 4.8 C# layer was then bound to AJ Tools' 4.14 core,
and every fragment failed on Revit 2026 - while 2020 and 2024 showed the same journal conflict line and
were unaffected. A dependency another add-in may also ship is a 2025+ hazard, not a 2020-2024 one.

**Heron's answer, for the C# compiler: a load context of its own, on 2025 and later only.**
[`HeronScriptHost`](../../../revit/Heron.Revit.Addin/HeronScriptHost.cs) loads `Heron.Scripting.dll` (the engine
and the stack guard) into an `AssemblyLoadContext` that takes `Microsoft.CodeAnalysis*` from Heron's folder and
hands every other name - the Revit API above all - back to the default context, so a fragment still sees the
types Revit hands it. Not `<ManifestSettings>`: that isolates only 2026 on, and crashes 2025 or older. On
2025+ the add-in has no compile-time Roslyn reference, so a Roslyn type named outside the two engine files
fails check-compile there. Proved off Revit by `tests/test_script_isolation.py`; **not yet run in Revit**
([NEEDS-CHECKING group GW](../../../docs/needs-checking/group-gw.md)). **A new dependency another add-in may
also ship goes the same way** - into Heron's context, never the default one.

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

**A fragment gets the same symbols, from the add-in that compiles it.**
`revit/Heron.Revit.Addin/HeronFragmentSymbols.cs` reads them back from the add-in's own build and puts
them in front of every fragment script. So a symbol added to `Directory.Build.props` is added there too,
in the same commit - `tests/test_fragment_symbols.py` fails until it is. Until 2026-10-09 the add-in
handed a fragment no symbols at all, and a fragment's `#if REVIT2020` was false on Revit 2020
([row 5b-181](../../../docs/FRAGMENT-ISSUES.md)). A fragment `#if` may name release symbols only - the
same suite fails on `DEBUG` or a framework symbol, which the fragment gate defines and the add-in does not.

## Breaking API changes

| Release | What changed | Old | New |
|---|---|---|---|
| 2021 | Units | `DisplayUnitType` | `UnitTypeId` / `ForgeTypeId` |
| 2022 | Parameter and spec type | `ParameterType` | `SpecTypeId` / `ForgeTypeId` |
| 2022 | Tag references | `TaggedElementId`, `LeaderEnd`, `HasElbow` | `GetTaggedElementIds()`, `GetLeaderEnd(ref)`, `HasLeaderElbow()` |
| 2024 | Element id storage | `IntegerValue` (int) | `Value` (long) — `IntegerValue` is deprecated and **throws** above 32 bits |
| 2024 | Built-in category and parameter enums | 32-bit | 64-bit — old int casts throw |
| 2025 | Dimensions | one `Dimension` class | `LinearDimension`, `RadialDimension`, `ArcLengthDimension`. Exact-type checks fail — **and the same change brought the first PROJECT-side creation route**, see below |
| 2026 | Add-in isolation | — | `<ManifestSettings>` in the manifest. ⚠ Read by **Revit 2025 or older this crashes Revit** — it must be stripped per release |
| 2026 | Classification parameters RENAMED, same numbers | `BuiltInParameter.OMNICLASS_CODE`, `OMNICLASS_DESCRIPTION`, `UNIFORMAT_CODE`, `UNIFORMAT_DESCRIPTION` ("OmniClass Number", "OmniClass Title", "Assembly Code", "Assembly Description") | `CLASSIFICATION_CODE`, `CLASSIFICATION_DESCRIPTION`, `ASSEMBLY_CODE`, `ASSEMBLY_DESCRIPTION` ("Classification Number", "Classification Title"). The values are unchanged - -1002502, -1002503, -1002500, -1002501 - so **neither spelling compiles on every release**; resolve the enum by name at run time, see below |
| 2027 | Runtime | .NET 8 | .NET 10 |
| 2027 | All-user add-in path | `ProgramData` | `Program Files` |

Also removed or deprecated at 2027: AXM import, several `Mechanical.Zone` members, legacy rebar
creation methods, and several `EnergyDataSettings` properties.

**Rebar, read off the shipped assemblies on 2026-10-19** ([row 5b-1258](../../../docs/fragment-issues/section-5b-rows-401-425.md)):
`Rebar.CreateFromCurves` (and `CreateFromCurvesAndShape`) taking two `RebarHookType`s and two
`RebarHookOrientation`s is obsolete at **2026** and gone at **2027**, with the enum - 2026 on take a
`BarTerminationsData` and a `RebarTerminationOrientation`. `RebarBarType.BarDiameter` is 2020-2022;
`BarNominalDiameter` / `BarModelDiameter` arrive at **2022**. `SetSolidInView` on rebar, area and path
reinforcement is 2020-2023 only. `FabricSheet.SetUnobscuredInView` starts at **2021**. Each placing
rebar fragment - [`place-column-rebar`](../../../brain/fragments/place-column-rebar/impl/any/fragment.cs)
and its siblings - carries the two routes in one `makeBar` adapter.

**Rendering, read off the shipped assemblies and `RevitAPI.xml` on 2026-10-20** ([row 5b-1264](../../../docs/fragment-issues/section-5b-rows-401-425.md)):
the rendering, sun, Graphic Display Options and posted-command members are alike 2020-2027 - with four
exceptions. `DisplayStyle` value 9 is `Raytrace` on 2020 only, absent 2021-2023 and `Textures` from 2024,
so match a style by name. `SunStudyTimeInterval` gains `Seconds15`, `Seconds30`, `Seconds45` and `Minute`
at **2024**. `ImageType.WidthInPixels` / `HeightInPixels` arrive at **2021** - read
`RASTER_SYMBOL_PIXELWIDTH` / `_PIXELHEIGHT` instead. `View3D.ScalePerspectiveCropBox` arrives at
**2024**. **No release has a member that runs the render**, and none has a field of view.

**The classification rename, and the file behind it.** Read 2026-10-11 off the shipped assemblies with
`dotnet run --project tools/api-surface -- --members BuiltInParameter`: the four names above exist
2020-2025 and are gone at 2026, where the same four numbers carry the new names. A fragment that types
either spelling compiles on half the range, so one implementation asks the running Revit which name it
has - `Enum.IsDefined(typeof(BuiltInParameter), "CLASSIFICATION_CODE")` - and parses that one. Revit's
OmniClass list is a plain TAB-separated file no API reads (code, title, level, built-in category id):
`OmniClassTaxonomy.txt` to 2025, `Classification_Taxonomy.txt` from 2026, in
`Application.CurrentUsersDataFolderPath` with a cached copy in the release's `UserDataCache`. The 2020
to 2025 file holds Windows-1252 bytes, not UTF-8, and **from 2026 Revit ships only a 14-line sample**
with no real codes - so on 2026 and later nothing validates until the office supplies its own file.
Assembly Code and Keynote have tables the API does read: `AssemblyCodeTable` and `KeynoteTable`, on
every release from 2020.

## Creating a dimension — four routes, four different histories

**Measured 2026-09-19 against every reference assembly 2020 to 2027**, after a fragment failed to
compile on the obvious-looking call.

**The `New...Dimension` names are a trap.** `NewLinearDimension`, `NewRadialDimension`,
`NewDiameterDimension`, `NewArcLengthDimension` and `NewAngularDimension` all belong to the FAMILY
EDITOR's creation object. A project document's `doc.Create` is a different type and **cannot reach any
of them**. The name reads correct, the code does not compile, and nothing in the table above says so.

| want | the call that works in a PROJECT | from |
|---|---|---|
| Linear | `NewDimension(View, Line, ReferenceArray)` on the base item factory, via `doc.Create` | 2020 |
| Angular | `AngularDimension.Create(Document, View, Arc, IList<Reference>, DimensionType)` — a static | 2020 |
| Radial | `RadialDimension.Create(Document, View, Reference, bool)` | **2025** |
| Diameter | the same call with `isDiameter: true` | **2025** |
| Arc length | `ArcLengthDimension.Create(Document, View, Arc, Reference, IList<Reference>)` | **2025** |

**So a radius cannot be dimensioned through the API on Revit 2020 to 2024 at all** — not by Heron and
not by anything else. That is the API's limit, not a choice. `DiameterDimension.Create` exists on **no**
release; the `isDiameter` flag is the only route to a diameter dimension in a project.

`brain/fragments/create-radial-dimension/` therefore declares `revit: ["2025", "2026", "2027"]`, and
`tools/check-fragments-compile.py` enforces it. How many fragments claim a release is
`grep -h '^revit:' brain/fragments/*/fragment.yaml | grep -c '"2024"'` - never a typed number.

## Phase creation — there is no such thing, on any release

**`Phase` carries no members of any kind.** No `Create`, no static, nothing — checked in all eight
reference assemblies on 2026-09-19. Phases are made by hand in Revit and the API can only read them and
assign elements to them. Do not plan a fragment for it; the answer will not change with a release.

## Members that simply ARRIVED — the class the table above misses

Everything above is something that **changed**. The failure that actually reached this repository was
a member that was **added** partway through the supported range: it does not look like a version
problem, because there is no old spelling and no deprecation warning. It compiles on a new release and
does not exist on an old one.

| Member | Exists from | Use instead, on 2020+ |
|---|---|---|
| `Document.CreationGUID` | **2024** | **Nothing read-only identifies a MODEL on 2020–2023.** Read it by reflection where it exists — `RevitOperations.CreationGuid`, null on older releases — and say what is lost without it. **Not** `doc.ProjectInformation.UniqueId`: a new project inherits that element from its template, so every model made from one template reports the same id ([D-113](../../../docs/decisions/D-113.md)) |

`RevitWrite.DocumentKey()` used `CreationGUID` to pin the document for Golden Rule 20. It had been read
several times, including once by a review that listed it as a *likely* problem spot, and it survived
every reading. The compiler found it in seconds.

**THIS TABLE THEN RECOMMENDED THE WRONG SUBSTITUTE, until 2026-10-06.** It said the Project Information
UniqueId is *"created with the document"*. It is created with the TEMPLATE: two blank projects and an
unrelated `PIPE.rvt` reported one id on 2026-09-15 (NEEDS-CHECKING E12), and on 2026-10-06 a new project
was given another model's kept loads answers because both reported it
([FRAGMENT-ISSUES 5b-324](../../../docs/fragment-issues/section-5b-rows-301-325.md)). The compiler cannot
catch that class: the substitute compiles everywhere and answers a different question.

**Prefer a member that exists everywhere over a `#if` that hides one that does not — when it answers the
same question.** Measure that before taking it: read both on two real models that should differ. Where no
such member exists, read the newer one by reflection in ONE adapter (`RevitOperations.CreationGuid`, and
`SpecTypeId` in `RevitFragment`) rather than a `#if`, so one build still serves every release. That is
[D-20](../../../docs/DECISIONS.md) applied to identity rather than units — prefer the thing with nothing
in it for Autodesk to move.

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
