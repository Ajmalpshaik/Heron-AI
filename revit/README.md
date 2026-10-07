# revit/ — Part 1, Heron Revit

**Everything here loads into `Revit.exe`.**

| | |
|---|---|
| Language | C# |
| Runs | inside Revit's process |
| Changing it needs | **a Revit restart** — a loaded assembly cannot be unloaded |
| Built | once per Revit generation: `net472`, `net48`, `net8.0-windows`, `net10.0-windows` |
| Size | derive it: `find revit -name '*.cs' -not -path '*/obj/*' \| xargs wc -c`. A typed figure here went stale |

## What's here

| Project | Does |
|---|---|
| `Heron.Revit.Addin` | Ribbon, Connect, Status, the tool windows, and the `ExternalEvent` handler (`RevitDispatcher.cs`) that runs every bridge request on Revit's thread. The `Revit*.cs` files are the element operations; `Resources/` holds the ribbon icons |
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
