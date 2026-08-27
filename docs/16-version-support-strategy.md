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
| 2027 | **.NET 10** | 64-bit | **Third runtime.** Reported by a shipping project ([26 sec.3](26-prior-art-revit-mcp.md)); confirm against the SDK |

*Runtime versions must be verified against the installed SDKs before being locked in — see spec §24.*

## 2. The two breaks that matter

### Break 1 — `ElementId` becomes 64-bit at Revit 2024

`ElementId.IntegerValue` (int) was replaced by `ElementId.Value` (long). Code touching element IDs
needs two paths.

*Mitigation:* Heron stores and transmits `UniqueId` strings, never raw `ElementId`
(see [03 §7](03-heron-revit.md)). This confines the problem to a handful of internal call sites
instead of spreading it through the codebase — which is exactly why that rule exists.

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

---

## 6. Build and deployment matrix

Each Revit version needs its own `.addin` manifest and its own assembly set:

```text
%AppData%\Autodesk\Revit\Addins\2020\Heron\  ...  net48 build
%AppData%\Autodesk\Revit\Addins\2024\Heron\  ...  net48 build
%AppData%\Autodesk\Revit\Addins\2025\Heron\  ...  net8  build
%AppData%\Autodesk\Revit\Addins\2026\Heron\  ...  net8  build
```

The installer detects which Revit versions are present and deploys only those.

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
