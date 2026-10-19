# revit/ — Part 1, Heron Revit

**Everything here loads into `Revit.exe`.**

| | |
|---|---|
| Language | C# |
| Runs | inside Revit's process |
| Changing it needs | **a Revit restart** — a loaded assembly cannot be unloaded |
| Built | once per Revit generation: `net472`, `net48`, `net8.0-windows`, `net10.0-windows` |
| Size | derive it: `find revit -name '*.cs' -not -path '*/obj/*' \| xargs wc -c` |
| Status | every add-in file says `Heron-Status: DRAFT` - `grep -h 'Heron-Status:' revit/Heron.Revit.Addin/*.cs \| sort \| uniq -c`. What a person signed about an add-in agent is in [`brain/agent-proofs/`](../brain/agent-proofs/README.md), and it promotes nothing |

## What's here

| Project | Does |
|---|---|
| `Heron.Revit.Addin` | The Heron tab - **Heron** (connect, with **Bridge Status** under its arrow), **Changes**, **Admin**, **Publish**, **Ask to Keep**, **Free this model** and **Companion** (`HeronApplication.cs`, `Commands.cs`, `HeronCompanionCommands.cs`) - the tool windows, and the `ExternalEvent` handler (`RevitDispatcher.cs`) that runs every bridge request on Revit's thread. `RevitFragment.cs` compiles and runs a fragment - a read or a show with no transaction open, a change inside one named `TransactionGroup`; around a change stand Revit's **Keep or Discard** box and the grant to keep the rest of one job (`HeronKeepQuestion.cs`, `HeronKeepGrant.cs`, `RevitChangedSet.cs` - [D-126](../docs/DECISIONS.md), [D-138](../docs/DECISIONS.md)), the run guard (`HeronRunGuard.cs`, `HeronRunPolicy.cs`), the run clock that stops a fragment that never ends (`HeronRunClock.cs`, [D-129](../docs/DECISIONS.md)), the dialog watch (`HeronDialogWatch.cs`, `HeronDialogNote.cs`) and the rollback check (`HeronRollbackWitness.cs`). `HeronLiveState.cs` writes the live file the Companion page reads. `HeronModelHold.cs` holds a model for one chat at a time, named by the request's `conversation` (`HeronConversation.cs`), claimed in `RevitOperations.Run` ([D-141](../docs/DECISIONS.md)). The other `Revit*.cs` files are the fixed element operations; `Resources/` holds the ribbon icons. A `Heron*.cs` file that says it has nothing Revit-shaped in it is linked by a test host in `tests/` and run with no Revit |
| `Heron.Scripting` | The C# compiler door as an assembly of its own - `HeronScriptEngine.cs` and `HeronStackGuard.cs`, linked from `Heron.Revit.Addin`, no source of its own and no Revit reference. On Revit 2025 and later [`HeronScriptHost`](Heron.Revit.Addin/HeronScriptHost.cs) loads it into a load context of Heron's own, so Heron compiles on its own Roslyn whatever another add-in loaded ([row 5b-575](../docs/fragment-issues/section-5b-rows-401-425.md)); on 2020 to 2024 the same two files are built into the add-in. `Heron.Scripting.targets` copies it and the compiler beside the add-in |
| `Heron.Bridge` | The named-pipe server. No Revit reference — which is what makes it testable without Revit. References `platform/Heron.Core` |
| `Heron.Tools` | **Stage 2 shape proof, not a product** — a second piece of the same Heron tab. Listed `PROVING` in [`platform/heron-products.json`](../platform/heron-products.json) |
| `Heron.Doc` | **Stage 2 shape proof, not a product** — a second Heron tab with one dummy button. `PROVING` in the same manifest; its own header says it is deleted when Stage 2 closes |

Every project targets `$(HeronTfm)`, which [`Directory.Build.props`](../Directory.Build.props) maps
from `RevitVersion`.

## Rules for this folder

1. **Keep it small.** Everything here is expensive to change (restart), multiplied by the version
   matrix, and dangerous when wrong (a crash takes the user's model down). If it can live outside
   Revit, it should.
2. **`Autodesk.Revit` appears only inside `revit/`**, and within it `Heron.Bridge` must stay
   Revit-free. `tools/check-structure.py` enforces the first half; the second is held by the bridge's
   own project file, which references no Revit assembly.
3. **No network.** No `HttpClient`, no sockets. The bridge is a local named pipe and nothing else —
   see [docs/03 §1a](../docs/03-heron-revit.md).
   The Heron Companion page ([D-108](../docs/DECISIONS.md)) does not change this: its web server lives
   in `mcp/companion/`, and all the add-in gives it is a status file, `HeronLiveState.cs`.
4. **Version-conditional code lives in adapters**, never in logic. [docs/16](../docs/16-version-support-strategy.md)

## Fix things here when

The ribbon is wrong · Connect fails · Revit crashes on load · an element operation misbehaves ·
a Revit version needs supporting.
