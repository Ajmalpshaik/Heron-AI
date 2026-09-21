# platform/ — Part 4, Heron Platform

**The Kernel and the machinery around it.** Everything depends on this; it depends on nothing.

| | |
|---|---|
| Language | C# today; Python later for the outside-Revit half |
| Runs | wherever it is referenced |
| Rule | **plumbing, never intelligence** |

## What's here

**Three projects since 2026-09-21** — `Heron.Core`, and the installer, which this folder's own
*What will be here* has listed since it was written. The installer is two projects on purpose: the
engine is release-independent and runs anywhere, the window needs Windows.

### `Heron.Core`

Nine classes, each the Kernel half of an agent in
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

### `Heron.Installer` — BUILT, NOT PROVEN

The install engine, and **no window** — Stage 3 of
[the plugin extension plan](../docs/work-notes/plans/plugin-extension/02-implementation.md). Given the
product list, a set of products and a set of Revit releases, it decides what goes where, **waits** while
Revit is open, and reports per product per release.

| | |
|---|---|
| [`ProductManifest.cs`](Heron.Installer/ProductManifest.cs) | reads `platform/heron-products.json`. Nothing about a product is written in code |
| [`InstallPlan.cs`](Heron.Installer/InstallPlan.cs) | what will be installed where, and what is skipped **with the reason**. Pure: no files, no Revit, no clock |
| [`IRevitEnvironment.cs`](Heron.Installer/IRevitEnvironment.cs) | the two questions about Windows, as an interface, so a test can answer them |
| [`InstallEngine.cs`](Heron.Installer/InstallEngine.cs) | plan, wait, deploy, report. **It never closes Revit** |
| [`WindowsAdapters.cs`](Heron.Installer/WindowsAdapters.cs) | the only two places it touches Windows. Both shell out to the scripts in `tools/` rather than repeating their rules. **Neither has ever run** |

**It is the one project here that is not Kernel plumbing**, and it is also the only one nothing else
references — it sits at the bottom of the stack like everything in this folder, and nothing sits on it.
It pins `net8.0` rather than following the Revit release, because it installs **for** a release without
ever loading into one.

> **Everything it DECIDES is tested; nothing it DOES has run.** No file has been written and no
> PowerShell has executed — that needs Windows. **How many checks that is, derive it** rather than
> reading a number here:
>
> ```bash
> dotnet run --project tests/Heron.Installer.TestHost -c Release | grep -c '^  ok '
> ```
>
> This line typed **38** until 2026-09-21 and nothing measurable matched it — row 5b-81.

### `Heron.Installer.App` — BUILT, NOT SEEN

The installer **window** — `HeronInstaller.exe`, Stage 4. One window, one list, two buttons.

**It decides nothing.** Which products appear, which may be ticked, what a tick installs, what is greyed
out and what the grey says all come from
[`InstallerScreen`](Heron.Installer/InstallerScreen.cs) in the project above, which is tested against a
fake Revit and a fake disk. This project draws what it is handed.

**That split is not tidiness.** A WPF window needs `net8.0-windows` and the WindowsDesktop runtime, and
this repository is developed without either — so a rule written inside the window is a rule nothing can
check. [`tests/test_installer_window.py`](../tests/test_installer_window.py) is what holds the line: it
fails if the window names a product, asks a product anything, or grows an Update or Repair button.

**No XAML.** [`revit/Heron.Revit.Addin`](../revit/Heron.Revit.Addin/) builds its windows in C# too.

> **No pixel has been drawn.** It compiles on all eight releases with 0 warnings. Whether the window
> appears, is readable, and installs anything is owed on a Windows PC — **Group `AB`** in
> [NEEDS-CHECKING](../docs/NEEDS-CHECKING.md). **The group is named rather than its range**, because
> `AB8` was added on 2026-09-21 and this line still said `AB1` to `AB7` an hour later.

## What will be here

Update system · package manager · event bus · workflow engine ·
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
