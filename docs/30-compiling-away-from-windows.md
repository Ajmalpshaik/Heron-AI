<!--
Heron-Agent:  none
Heron-Step:   6
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  tool
See docs/29-metadata-standard.md
-->

# 30 — Compiling away from Windows

**The compiler was never the problem. Finding out it was reachable took three sessions.**

Heron's C# targets Revit, Revit is Windows-only, and the obvious conclusion — *so the code cannot be
compiled anywhere else* — is wrong, and it cost this project real time. Step 6, the first code that can
change a model, was written across two sessions and **never once passed through a compiler**. The
register's Group A was blocked on a Windows machine that was not available, and every session since had
to write the same sentence: *built, never compiled.*

It takes about five minutes to fix, on any Linux container.

```bash
python tools/check-compile.py
```

---

## 1. Why it works

Two things, both already true of this repository before anybody tried:

**The Revit API comes from NuGet.** `Heron.Revit.Addin.csproj` has always had two ways to resolve
`RevitAPI.dll` — a local Revit install, or public NuGet packages — and the NuGet route is the default.
Those packages are *reference assemblies*: the real shipped API surface, per Revit version, with no
implementation and nothing to install. Autodesk's DLLs are not redistributable
([docs/07 §4](07-installation-and-update.md)) and none of this redistributes them; they are read at
compile time and never copied to the output, which is what `Private=false` in that project file has
always meant.

**The .NET SDK is packaged by Linux distributions.** Microsoft's own download CDN is often unreachable
from a sandboxed container, which is the wall the earlier attempts hit. The distribution's package is
not:

```bash
apt-get install -y dotnet-sdk-8.0        # Debian/Ubuntu; other distributions have their own
```

That SDK builds `net472` and `net48` — the runtimes for Revit 2020 through 2024 — on Linux, without
Mono and without Wine.

---

## 2. What it covers, and what it cannot

| | |
|---|---|
| **Revit 2020–2024** | Fully compiled. `net472` and `net48`, every project |
| **Revit 2025–2027** | **Cannot be built off Windows.** They target `net8.0-windows` / `net10.0-windows` with WPF, which needs the Windows Desktop SDK — that ships in Microsoft's own SDK build and is absent from the source-built packages distributions carry |

`check-compile.py` reports those three as **SKIPPED**, never as passed, and its summary says in words
that a skip is not a pass. That distinction is the whole point of the script: the failure this
repository is most exposed to is a version-shaped hole that reads as green.

### The skip is now partly covered — `tools/check-api-surface.py`

A skip left the newest three releases with **nothing** checking them at all on the machines this project
is actually worked on. So the compiled add-in's reference tables are read instead — exactly the Revit
types and members the code calls — and each one is looked up in that release's shipped reference
assemblies:

```bash
python tools/check-api-surface.py
```

**Measured 2026-08-28: all 103 Revit types and members Heron calls exist in 2020, 2021, 2022, 2023,
2024, 2025, 2026 and 2027.** That is the missing-member class — the `CreationGUID` class — closed on
every supported release without a Windows machine.

It **matches by name**, so a member that still exists with a changed signature passes here and would
fail a real compile. It supplements the compile gate and never replaces it, and it says so on every run.

**It was validated before its clean result was believed**, by putting `doc.CreationGUID` back into
`RevitWrite.DocumentKey()` and running it against 2020: it reported the missing member and exited
non-zero. A checker that finds nothing is evidence about the checker until it has caught something —
the same lesson as the grep that "found nothing" and was reported as clean.

---

## 3. What a pass actually proves

**It proves the API surface agrees.** Every type and member the code names exists in that release, with
the signature it is used with. That is the entire *"worked in 2020, broke in 2024"* class — the one
[D-05](DECISIONS.md) and [docs/16](16-version-support-strategy.md) exist to guard, and the one that
otherwise surfaces as an add-in that will not load, in front of the user, mid-job.

**It proves nothing about behaviour.** Code that compiles can move a duct 200 feet instead of 200
millimetres and compile just as cleanly. `D3` in [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *move
them, then measure one* — is what catches that, and no compiler substitutes for it.

So the honest sentence after a green run is **"it compiles on 2020 through 2024"**, and not one word
more.

---

## 4. It found something the first time it ran

`RevitWrite.DocumentKey()` used `Document.CreationGUID` to identify the pinned document for Golden
Rule 20. It compiled clean on 2024 and failed on 2020: **the property does not exist there.** It had
been named in [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) as a *likely* problem spot, by reading — and
reading is what had already passed it twice.

The fix was not a `#if`. The document's Project Information element carries a `UniqueId` that is created
with the document, survives being saved, renamed and moved, and **exists on every release from 2020 to
2027** — so the same expression is correct everywhere and there is no version branch to maintain. That
is [D-20](DECISIONS.md)'s reasoning applied to identity instead of units: prefer the thing with nothing
in it for Autodesk to move.

**The method matters more than the fix.** Both facts were checked by loading the shipped `RevitAPI.dll`
for each release and listing the members of `Document` — not from memory, and not from documentation
that does not say which version introduced what. When a member's availability is in question, read the
assembly. It is a two-minute answer and it is the only kind that is not a guess.

---

## 5. The same trick runs the bridge

`Heron.Bridge` and `Heron.Core` have no Revit reference — that is enforced by
[`check-structure.py`](../tools/check-structure.py) and is the boundary
[docs/16 §4](16-version-support-strategy.md) describes. So they compile for `net8.0` as readily as for
`net48`, which means the Revit-free test host **runs** here too:

```bash
dotnet build tests/Heron.Bridge.TestHost -p:RevitVersion=2024 \
  -p:HeronTfm=net8.0 -p:OutputPath=bin/x64/Debug-net8.0/
python tests/test_bridge_roundtrip.py
```

The separate output directory is not decoration. `AppendTargetFrameworkToOutputPath` is false repo-wide,
so every target framework writes to the same `bin/x64/Debug` — and `check-compile.py` builds this project
once per Revit version, leaving whichever it did last. Run the two in one sitting without this and the
round-trip host is a `net48` `.exe` the runtime cannot start. It happened the first time they were run
together; the test now names the exact command to fix it rather than reporting a missing file.

.NET implements `NamedPipeServerStream` on Unix as a socket in the temp directory, so the same compiled
bridge speaks the same protocol; only the two lines that open the connection differ, and
`test_bridge_roundtrip.py` carries that shim rather than a second copy of itself.

All 32 of its checks pass here — the framing, the JSON parser, the token, the newest-connection-wins
handover, the toggle cycle, and **the whole lease**. What it does **not** cover is the Windows named
pipe itself: its naming, its security descriptor, and the `CreateNewInstance` flag that
[HANDOVER §4](../HANDOVER.md) note 2 was written about. Revit runs on Windows, so `A4` in the register
still means the Windows run — this is a strong signal ahead of it, not a replacement for it.

**It found a real defect the first time it ran**, in the test rather than in the bridge: the lease
section asserted that nothing held the lease, at a point where an earlier unknown-op probe had already
claimed it. `BridgeServer` claims for every operation that is not `ping` or `info`, an unknown one
included — correctly. The check had been written from reading, in a file that could not run on the
machine it was written on, and it was simply false. It now makes that observation where the lease is
genuinely still free, and states the truth where it is not.

---

## 6. The rule this leaves behind

> **An environment-specific block belongs in a sentence that names the environment.**

*"The C# cannot be compiled here"* was true of one container that could not reach one CDN. Written
without its environment, it became a property of the project, and three sessions inherited it as a fact
rather than as a thing to test. The same shape of error is recorded in the reference material this
project was allowed to study, about deleting git branches, where it also took several sessions to
unwind.

When something is refused, record **what refused it**. The next session can then try the other door
instead of re-deriving that the first one is shut.
