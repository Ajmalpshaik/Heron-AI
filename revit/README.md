# revit/ — Part 1, Heron Revit

**Everything here loads into `Revit.exe`.**

| | |
|---|---|
| Language | C# |
| Runs | inside Revit's process |
| Changing it needs | **a Revit restart** — a loaded assembly cannot be unloaded |
| Built | once per Revit generation: `net472`, `net48`, `net8.0-windows`, `net10.0-windows` |
| Size | **13 files, ~275 KB of C#** — `find revit -name '*.cs' | xargs wc -c`. Derive it; do not read it here |

## What's here

| Project | Does |
|---|---|
| `Heron.Revit.Addin` | Ribbon, Connect, Status. Step 2 adds the `ExternalEvent` handler |
| `Heron.Bridge` | The named-pipe server. No Revit reference — which is what makes it testable without Revit |

## Rules for this folder

1. **Keep it small.** Everything here is expensive to change (restart), multiplied by the version
   matrix, and dangerous when wrong (a crash takes the user's model down). If it can live outside
   Revit, it should.
2. **`Autodesk.Revit` appears only in `Heron.Revit.Addin`.** `Heron.Bridge` must stay Revit-free.
   Enforced by `tools/check-structure.py`.
3. **No network.** No `HttpClient`, no sockets. The bridge is a local named pipe and nothing else —
   see [docs/03 §1a](../docs/03-heron-revit.md).
4. **Version-conditional code lives in adapters**, never in logic. [docs/16](../docs/16-version-support-strategy.md)

## Fix things here when

The ribbon is wrong · Connect fails · Revit crashes on load · an element operation misbehaves ·
a Revit version needs supporting.
