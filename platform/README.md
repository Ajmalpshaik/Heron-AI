# platform/ — Part 4, Heron Platform

**The Kernel and the machinery around it.** Everything depends on this; it depends on nothing.

| | |
|---|---|
| Language | C# today; Python later for the outside-Revit half |
| Runs | wherever it is referenced |
| Rule | **plumbing, never intelligence** |

## What's here

**Four projects** — `Heron.Core`, and the installer, which this folder's own *What will be here* had
listed since it was written. The installer is three projects on purpose: the engine is
release-independent and runs anywhere, the window needs Windows, and the command line
(`heron-install`, added 2026-09-22) is the door an AI can knock on without a mouse. Derive the list
with `find platform -name '*.csproj'` rather than trusting this sentence.

### `Heron.Core`

One file per class (`ls platform/Heron.Core/*.cs`), each the Kernel half of an agent in
[the registry](../docs/28-agent-registry.md):

| Class | Does | Step |
|---|---|---|
| [`HeronPaths.cs`](Heron.Core/HeronPaths.cs) | **The Path Manager** — the single place that knows where anything lives. Product, data and derived are separate in code, and `IsSafeToDelete` returns false for anything under either | 1 |
| [`HeronConfig.cs`](Heron.Core/HeronConfig.cs) | The Configuration Manager. `Load` and `Save`, and deliberately **no** `ApplyFromRequest` | 1 |
| [`HeronIdentity.cs`](Heron.Core/HeronIdentity.cs) | Stable identity for the things Heron must be able to name twice | 1 |
| [`HeronOperationRegistry.cs`](Heron.Core/HeronOperationRegistry.cs) | The Tool Registry — every operation Heron will run, and the risk level of each | 3 |
| [`HeronAppend.cs`](Heron.Core/HeronAppend.cs) | One line onto the end of a file another Revit process may be appending to at the same moment — or a plain answer that it could not be done. Used by the audit trail and the add-in log | 1 |
| [`HeronAudit.cs`](Heron.Core/HeronAudit.cs) | The audit trail: one append-only line per request, keyed by Workflow ID | 4 |
| [`HeronAtomicWrite.cs`](Heron.Core/HeronAtomicWrite.cs) | Write a file so an interrupted write cannot destroy what was already there — never delete-then-move | 6 |
| [`HeronLease.cs`](Heron.Core/HeronLease.cs) | Who currently holds a Revit, and who may therefore send it anything ([D-22](../docs/DECISIONS.md)) | 6 |
| [`HeronPermissions.cs`](Heron.Core/HeronPermissions.cs) | The seven permission levels of [docs/12 §1](../docs/12-security-and-permissions.md), in order | 6 |
| [`HeronStop.cs`](Heron.Core/HeronStop.cs) | Emergency Stop — one switch that stops Heron doing anything further | 6 |
| [`HeronUnits.cs`](Heron.Core/HeronUnits.cs) | Unit conversion. Millimetres, which is what the user says, to whatever Revit wants | 6 |

### `Heron.Installer` — RUN ON THE OWNER'S PC, NOT EVERY ROW

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
| [`WindowsAdapters.cs`](Heron.Installer/WindowsAdapters.cs) | where it touches Windows. They shell out to the scripts in `tools/` rather than repeating their rules. **First run on the owner's PC 2026-09-21** — Group `AB` in [NEEDS-CHECKING](../docs/needs-checking/group-ab.md) |
| [`InstallerScreen.cs`](Heron.Installer/InstallerScreen.cs) | what the window and the command line may offer: which rows appear, which may be ticked, and the sentence under a greyed one |
| [`InstallSource.cs`](Heron.Installer/InstallSource.cs) | whether a named source may be used at all, and the only place a fetch address is built from what was accepted |
| [`ReleaseAssets.cs`](Heron.Installer/ReleaseAssets.cs) | what a release carries and how a downloaded file is checked. Pure; agrees with `tools/build-release-assets.py` |
| [`ReleaseDownload.cs`](Heron.Installer/ReleaseDownload.cs) | fetches a product's files from a **named release**, verifies them before use, unpacks them. Nothing downloaded is executed |
| [`ProductFolder.cs`](Heron.Installer/ProductFolder.cs) | the same as the download, minus the wire: Heron's files already on this PC, no internet |
| [`UpdateCheck.cs`](Heron.Installer/UpdateCheck.cs) | the verdict on whether a newer release exists — up to date, newer, ahead, or cannot tell |

**It is not Kernel plumbing**, and outside this folder only its own test host references it — the
window and the command line below sit on it, and nothing outside the installer does.
It pins `net8.0` rather than following the Revit release, because it installs **for** a release without
ever loading into one.

> **Everything it DECIDES is tested here; what it DOES has run only on the owner's PC**, on
> 2026-09-21, recorded as Group `AB` in
> [`docs/needs-checking/group-ab.md`](../docs/needs-checking/group-ab.md) — it found three Revit
> releases, read the Addins folder and installed the AI Bridge. Rows still owed are listed there and in
> [NEEDS-CHECKING](../docs/NEEDS-CHECKING.md). **How many checks the tested half is, derive it** rather
> than reading a number here:
>
> ```bash
> dotnet run --project tests/Heron.Installer.TestHost -c Release | grep -c '^  ok '
> ```
>
> This line typed **38** until 2026-09-21 and nothing measurable matched it — row 5b-81.

### `Heron.Installer.App` — SEEN ON THE OWNER'S PC, NOT EVERY ROW

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

> **It has been seen.** Group `AB` was run on the owner's PC on 2026-09-21: the window appeared, read
> cleanly after one layout fix, and installed the AI Bridge for real —
> [`docs/needs-checking/group-ab.md`](../docs/needs-checking/group-ab.md). **Not every row passed**:
> which are still owed or blocked is that file's and
> [NEEDS-CHECKING](../docs/NEEDS-CHECKING.md)'s answer, not this one's. **The group is named rather
> than its range**, because `AB8` was added on 2026-09-21 and this line still said `AB1` to `AB7` an
> hour later.

### `Heron.Installer.Cli` — BUILT, NOT RUN ON WINDOWS

`heron-install`, Stage 6 — routes 1 and 2, the AI installing. The choices arrive already made on a
command line; everything after that is the same engine the window uses. [`Arguments.cs`](Heron.Installer.Cli/Arguments.cs)
reads the command line and refuses an unknown flag by name; [`Program.cs`](Heron.Installer.Cli/Program.cs)
wires things together and decides nothing. `net8.0`, no window, so it runs where Heron is developed.
**Its runs on a PC with Revit are owed** — Group `AE` in
[`docs/needs-checking/group-ae.md`](../docs/needs-checking/group-ae.md).

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
