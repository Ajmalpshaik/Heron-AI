# 16 — Version Support Strategy

> **Requirement (decided 2026-08-27, [D-05](DECISIONS.md)):** Heron AI supports
> **Revit 2020 through the latest release**, and every future release after it.
>
> This is a firm requirement, not an aspiration. It is also the single most expensive
> commitment in the platform, so it needs a strategy rather than good intentions.

---

## 1. What "2020 → latest" actually contains

Eight versions today, growing by one every year:

| Revit | Runtime | ElementId | Notes |
|---|---|---|---|
| 2020 | .NET Framework 4.7.2 | 32-bit | Oldest supported |
| 2021 | .NET Framework 4.8 | 32-bit | |
| 2022 | .NET Framework 4.8 | 32-bit | |
| 2023 | .NET Framework 4.8 | 32-bit | |
| 2024 | .NET Framework 4.8 | **64-bit** | `ElementId.IntegerValue` → `ElementId.Value` |
| 2025 | **.NET 8** | 64-bit | **Runtime break** |
| 2026 | .NET 8 | 64-bit | |
| 2027 | **.NET 10** | 64-bit | **Third runtime.** **Confirmed** — `brain/heron_dotnet.py` pins `2027: 10`, `Directory.Build.props` maps it, CI compiles 2027 with the .NET 10 SDK on every pull request and every push to `main`, and a real Revit 2027 loaded the add-in on 2026-09-19 ([`A12`](NEEDS-CHECKING.md)). This cell said *"Reported by a shipping project ([26 sec.3](26-prior-art-revit-mcp.md)); confirm against the SDK"* until 2026-09-21 — an open action that had closed twice over ([row 5b-62](FRAGMENT-ISSUES.md)) |

*Runtime versions must be verified against the installed SDKs before being locked in — see spec §24.*

## 2. The two breaks that matter

### Break 1 — `ElementId` becomes 64-bit at Revit 2024

`ElementId.IntegerValue` (int) was replaced by `ElementId.Value` (long). Code touching element IDs
needs two paths.

*Mitigation:* Heron stores and transmits `UniqueId` strings, never raw `ElementId`
(see [03 §7](03-heron-revit.md)). This confines the problem to a handful of internal call sites
instead of spreading it through the codebase — which is exactly why that rule exists.

### What the 2025 dimension split ALSO did, which was not written down until 2026-09-19

The break table records that 2025 split one `Dimension` class into `LinearDimension`,
`RadialDimension` and `ArcLengthDimension`, and that exact-type checks fail across it. True, and not
the whole change: **2025 is also the first release where a radial or arc-length dimension can be
created in a PROJECT at all.**

Before it, every `New...Dimension` call in the API belongs to the family editor's creation object,
which a project document cannot reach. `RadialDimension.Create` and `ArcLengthDimension.Create` arrive
at 2025; `AngularDimension.Create` and the base factory's `NewDimension` have been there since 2020;
and `DiameterDimension.Create` exists on no release at all — a diameter dimension is the radial call
with `isDiameter: true`, so it starts at 2025 too.

Measured against all eight reference assemblies on 2026-09-19, after
`brain/fragments/create-linear-dimension/` failed to compile on the obvious-sounding name. The full
table is in [`.claude/skills/revit-version-support/SKILL.md`](../.claude/skills/revit-version-support/SKILL.md).

**This is why `create-radial-dimension` declares three releases rather than eight** — as narrow as any
`revit:` list in the library (`attach-walls` claims the same three, 2025 to 2027), and the compile gate
enforces it.

### Break 2 — .NET Framework → .NET 8 at Revit 2025

This is the real one. **One assembly cannot target both runtimes.** There is no clever way around it.

---

## 3. Strategy: one source tree, multi-targeted

```xml
<TargetFrameworks>net48;net8.0-windows;net10.0-windows</TargetFrameworks>
```

**[NOTE]** Three runtimes, not two — Revit 2027 moves to .NET 10 ([26 §3](26-prior-art-revit-mcp.md)).
This does not change the strategy; the adapter layer absorbs it. It does confirm that a new runtime
arrives roughly every two years, which is the strongest argument for keeping all version-conditional
code confined to adapters.

One codebase. Two build outputs. Version differences handled by compilation symbols:

```text
REVIT2020  REVIT2021  REVIT2022  REVIT2023  REVIT2024  REVIT2025  REVIT2026  REVIT2027
REVIT2024_OR_GREATER     <- 64-bit ElementId
REVIT2025_OR_GREATER     <- .NET 8
```

> **As built (2026-10-18):** one property chooses the release - `dotnet build -p:RevitVersion=2024` - and
> [`Directory.Build.props`](../Directory.Build.props) maps it to **four** target frameworks, not three:
> `net472` for 2020, `net48` for 2021 to 2024, `net8.0-windows` for 2025 and 2026, `net10.0-windows` for
> 2027; a release it does not list is an error, never a guess. Each release builds into its own folder,
> `bin\x64\<Configuration>\<RevitVersion>\`, so one build per release rather than two outputs. The symbols
> are `REVIT<year>` plus a cumulative `REVIT<year>_OR_GREATER` from 2024 up - `REVIT2026_OR_GREATER` and
> `REVIT2027_OR_GREATER` as well as the two above. `python tools/check-compile.py` builds every release,
> and CI's job *"The C# compiles, 2020 to 2027"* runs it on every pull request and every push to `main` - a
> compile, not a proof.

### Why not separate branches per version

Branch-per-version is the intuitive answer and it is a trap. A fix made in one branch has to be
cherry-picked into seven others, forever. Within a year the branches diverge, and Golden Rule 4
("never break a working version") becomes impossible to verify because there is no single thing to test.

One source tree, multi-targeted, with a version matrix in CI, is the only approach where
"does this still work in 2020?" has an answer that can be checked automatically.

---

## 4. The adapter layer

Everything that differs between versions lives in **one place**, behind a stable internal interface:

```text
Heron.Revit.Abstractions      <- interfaces only, no Autodesk types
Heron.Revit.Adapters
   ElementIdAdapter            <- 32-bit / 64-bit
   SelectionAdapter
   ParameterAdapter
   ...
Heron.Revit.Core              <- written once, against the abstractions
```

Rules:

1. **Version-conditional code lives only in adapters.** If `#if REVIT2024_OR_GREATER` appears in core
   logic, the adapter is missing.
2. **Adapters are the version compatibility surface.** A new Revit release should mean touching adapters,
   not touching features.
3. **Core never references `Autodesk.Revit.*` directly.** This also gives testability
   ([13 §3](13-testing-and-quality.md)) and keeps the door open for non-Revit platforms
   ([01 §8](01-vision-and-principles.md)) — one discipline, three payoffs.

This is the same "adapter required?" logic the spec already describes for fragments (§11, §68),
applied to the platform itself.

> **As built (2026-10-18):** there are no `Heron.Revit.Abstractions`, `Heron.Revit.Adapters` or
> `Heron.Revit.Core` projects. The C# is `revit/` (`Heron.Bridge`, `Heron.Doc`, `Heron.Revit.Addin`,
> `Heron.Scripting`, `Heron.Tools`) and `platform/` (`Heron.Core`, and the installer's `Heron.Installer`,
> `Heron.Installer.App` and `Heron.Installer.Cli`); `Autodesk.Revit` may appear only inside
> `revit/` (`tools/check-structure.py`) and `Heron.Bridge` stays Revit-free (its own project file references
> no Revit assembly). Outside the
> fragments, `#if REVIT...` appears in a handful of add-in files (`grep -rl '#if REVIT' revit platform`),
> rule 1's intent kept by keeping that list short rather than by an adapter project.

---

## 5. Fragments follow the same shape

```text
fragments/select-elements-by-category/
  fragment.yaml           <- declares: revit >= 2020
  impl/net48/             <- Revit 2020-2024
  impl/net8/              <- Revit 2025+
  tests/
```

Most fragments will have **one** implementation that works everywhere, because they are written
against the Heron abstractions rather than raw Revit types. Only fragments that genuinely need a
version-specific API get a second implementation — which is precisely the decision tree in spec §68.

> **As built (2026-10-18):** every fragment has one implementation, `impl/any/fragment.cs`
> (`ls -d brain/fragments/*/impl/*/`), and declares an explicit `revit:` list, never a range. A version
> difference is an `#if REVIT...` branch inside it: the add-in compiles fragments with Roslyn inside Revit
> and hands it the add-in's own release symbols (`HeronFragmentSymbols.cs`, row 5b-181).
> `python tools/check-fragments-compile.py` compiles each one against every release it claims, in CI.

---

## 6. Build and deployment matrix

Each Revit version needs its own `.addin` manifest and its own assembly set:

| Install path | Build |
|---|---|
| `%AppData%\Autodesk\Revit\Addins\2020\Heron\` | net48 |
| `%AppData%\Autodesk\Revit\Addins\2024\Heron\` | net48 |
| `%AppData%\Autodesk\Revit\Addins\2025\Heron\` | net8 |
| `%AppData%\Autodesk\Revit\Addins\2026\Heron\` | net8 |

The installer detects which Revit versions are present and deploys only those.

> **As built (2026-10-18):** every release, 2020 to 2027, installs per user at
> `%APPDATA%\Autodesk\Revit\Addins\<version>\` with its own build - `net472` for 2020 (not `net48` as the
> table above has it), `net48` for 2021 to 2024, `net8.0-windows` for 2025 and 2026, `net10.0-windows` for
> 2027 - and `tools/deploy-addin.ps1` refuses a build whose runtime does not match the release. Revit
> 2027's move of the *all-user* folder to `Program Files` does not touch a per-user install
> ([`revit-version-support`](../.claude/skills/revit-version-support/SKILL.md)). A real Revit has loaded the
> add-in on 2020, 2024, 2026 and 2027 ([A12](needs-checking/group-a.md),
> [Group EQ](needs-checking/group-eq.md)); on 2026 no fragment could run until a repair that has not yet
> been run in Revit ([Group GW](needs-checking/group-gw.md)).

### As built — release by release, built and seen (2026-10-18)

The runtime and the folder come from [`Directory.Build.props`](../Directory.Build.props) (`HeronTfm`,
`OutputPath`). The last two columns are a real Revit's, and a compile counts in neither.

| Revit | .NET | Build folder | The add-in loaded in a real Revit | A signed proof names it |
|---|---|---|---|---|
| 2020 | `net472` | `bin\x64\<Configuration>\2020\` | **yes** - [A12](needs-checking/group-a.md) 2026-09-19, [EQ6](needs-checking/group-eq.md) 2026-10-17 | yes |
| 2021 | `net48` | `bin\x64\<Configuration>\2021\` | NOT RUN - EQ1: no PC reached has it | no |
| 2022 | `net48` | `bin\x64\<Configuration>\2022\` | NOT RUN - EQ2 | no |
| 2023 | `net48` | `bin\x64\<Configuration>\2023\` | NOT RUN - EQ3 | no |
| 2024 | `net48` | `bin\x64\<Configuration>\2024\` | **yes** - A12, EQ6, and the safety groups run on 2026-10-18 ([Group ET](needs-checking/group-et.md)) | yes |
| 2025 | `net8.0-windows` | `bin\x64\<Configuration>\2025\` | NOT RUN - EQ4: no PC reached has it | no |
| 2026 | `net8.0-windows` | `bin\x64\<Configuration>\2026\` | **loads, and no fragment ran** - EQ5, [row 5b-575](fragment-issues/section-5b-rows-401-425.md); the repair is [Group GW](needs-checking/group-gw.md), not yet run on 2026 | no |
| 2027 | `net10.0-windows` | `bin\x64\<Configuration>\2027\` | **yes** - A12, EQ6, and fragments ran in the safety groups of 2026-10-18 (Group ET) | no |

*A signed proof names it* is derived, never typed: the two commands at the top of
[Group EQ](needs-checking/group-eq.md) list the release each PROVEN fragment's own proof names, and a proof
on a further release is an entry of `proof.releases` ([D-131](DECISIONS.md)), which `proven_releases` in
[`heron_matrix.py`](../brain/heron_matrix.py) reads beside it. Run them again before trusting this column.

**Per-user location, no admin rights** — see [07 §5](07-installation-and-update.md).

### Building requires Revit API assemblies per version

`RevitAPI.dll` / `RevitAPIUI.dll` are Autodesk's and are **not redistributable**. In CI they come from
the official NuGet packages published for this purpose, referenced with *Copy Local = false* so they
are never shipped — Revit provides them at runtime.

---

## 7. The testing consequence

Eight versions × every fragment is a large matrix. It is also the only thing that makes Golden Rule 4 real.

**Practical tiering:**

| Tier | Versions | When |
|---|---|---|
| **Primary** | The version used daily for development | Every commit |
| **Runtime representatives** | One .NET Framework version + one .NET 8 version | Every commit |
| **Full matrix** | All eight | Before release, and on any adapter change |

Testing two versions on every commit catches the runtime break immediately, which is where almost all
breakage will occur. The full sweep runs when it matters.

> **As built (2026-10-18):** the full matrix **compiles** on every pull request and every push to `main` -
> the add-in for all eight releases and each fragment for every release it claims. What runs in Revit is
> per fragment and per release, and the table in §6 says how far each release has got: a
> fragment's proof names the release it ran on, and a proof on another release is an entry of
> `proof.releases` ([D-131](DECISIONS.md)). No release runs Heron's tests inside Revit automatically.

See [13 §3](13-testing-and-quality.md) for how Revit tests actually execute.

---

## 8. Sequencing — supporting all versions does not mean building all versions first

Important distinction, so the requirement does not stall Phase 0:

> **Supporting 2020 → latest is a requirement of the finished platform.
> Building the first vertical slice on one version is how you get there.**

The multi-targeting, the adapter layer and the `UniqueId` discipline are designed in **from the first
line of code** — they cost almost nothing up front and are ruinous to retrofit.

The *validation* across all eight versions is added as the slice proves itself:

1. Phase 0 — build and prove on **one** version, with the multi-target structure already in place
2. Phase 0.5 — add the **other runtime** (one net48 + one net8 version). This is where reality bites
3. Phase 1+ — fan out across the remaining versions, adapter by adapter

Nothing about this sequence weakens the requirement. It just avoids debugging eight versions
of something that has never worked once.

---

## 9. New Revit releases

An annual event, so it should be a routine rather than a project:

1. Verify the runtime and API changes for the new version
2. Add the compilation symbol and build target
3. Run the full fragment matrix
4. Fix only what the adapters need
5. Record any fragment that genuinely cannot support it — with the reason

**Golden Rule 4 applies in both directions.** A new Revit version must never be the reason an older
one stops working.
