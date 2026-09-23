# Needs checking — Group L

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group L — linked models (needs Revit and a model that links something)

`HERON-REVIT-LNK-015` — [`revit/Heron.Revit.Addin/RevitLinks.cs`](../../revit/Heron.Revit.Addin/RevitLinks.cs).
Compiles on 2020–2027, 0 warnings, 2026-09-15. **Never run.** Read-only, so nothing here can damage a
model — but every claim below is a claim about an API surface, not about behaviour.

Use a real job file: one with an architectural or structural link, and ideally one broken link.

| ID | Do this | Pass looks like |
|---|---|---|
| **L1** | Open a model with links and ask Heron *"what links does this model have?"* | Every link in Manage Links is listed, with the same names. A link Revit shows and Heron does not is the finding |
| **L2** | Compare `hostElements` against `count_elements` for the same model | **The same number.** If they differ, one of the two collectors is not doing what its comment says |
| **L3** | Unload one link in Manage Links, ask again | That link reads `unloaded` and its element count reads **not known**, never `0`. Revit's own Manage Links still shows it as unloaded afterwards — Heron must not have reloaded it |
| **L4** | Rename or move a linked file on disk, reopen the host, ask again | The link reads `not found`. This is the status a real job hits most often |
| **L5** | A model with one link placed twice | `linkTypes` is 1 and `placements` is 2. They are different facts and the answer must not merge them |
| **L6** | A nested link (a link inside a link) | It is listed and marked `[nested]`. A nested link is not attached to this model, so *"reload it"* is answered somewhere else |
| **L7** | Check the path shown | It matches what Manage Links shows — including the `RSN://` or BIM 360 form for a server model, not a raw internal path |

---
