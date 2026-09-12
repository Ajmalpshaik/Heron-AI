# The twenty-first session, 2026-09-02 — eight more, and the metadata check earned its keep

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **146 to 154**, in the same container and with the same
limitation - no .NET SDK, so **these eight have not been compiled either**. `A9` now names sixteen.

The batch was chosen the same way as the last one: the owner's sentences put through
`heron_brain.lookup`, and the wrong answers written down first.

| | |
|---|---|
| `CREATE_CEILING` | *"create a ceiling in this room"* was answering `MEASURE_CEILING_HEIGHT` - a read, to a request to build. **The earlier library had recorded this job as IMPOSSIBLE and it was not**: `Ceiling.Create` arrived in 2022, the note left the version off, and it read as "ceilings cannot be made" while two of three installed Revits could. Reached by name at run time, so one source spans the range and 2020 gets an answer rather than an error |
| `CREATE_ELECTRICAL_RUN` | *"draw a cable tray"* was answering `DIMENSION_MEP_RUNS` - a write, of dimensions. **Tray and conduit are ONE fragment**: separate classes, the same creation shape, and two near-identical fragments in a crowded area cost more than they buy. Which one is wanted is asked for, as `PLACE_ROOMS` asks about rooms and spaces |
| `JOIN_GEOMETRY` | *"join these walls together"* was answering `PLACE_MEP_FITTING`, and after last session also `CONNECT_OPEN_ENDS` - two MEP writes for a question about walls. Both would have found nothing to do and said so, which is the quiet kind of wrong |
| `CREATE_WORKSET` | *"add a new workset"* was answering `LIST_WORKSETS`. **ADMIN risk**: a workset is project structure the whole team works inside. A workset is also **not an element** - no id - so what comes back is names |
| `REPLACE_MATERIAL` | *"change the material on these"* was answering `READ_ELEMENT_MATERIAL`. A material hides in the compound-structure LAYERS and in material PARAMETERS, and a swap that misses one leaves the old material half in use, where it refuses to purge and nobody can see why. **The layers that come back are COPIES** - editing them in place changes nothing |
| `RELOAD_LINKS` | *"reload the links"* was answering `LIST_LINKED_MODELS`. It reloads or unloads and **never removes** - removing deletes what is hosted on the link. Revit's own result code is printed raw, because which code comes back for an already-current link is still not established |
| `CREATE_SELECTION_FILTER` | *"make a selection set"* was answering `SET_SELECTION` - a highlight gone at the next click. A saved set survives the model closing; a view filter is a RULE. Three different things, one sentence |
| `SET_VIEW_UNDERLAY` | *"set the underlay on this plan"* was answering `CREATE_TEXT_NOTE`. Base and top are set TOGETHER - writing the base alone leaves the old top and shows a range nobody asked for, which reads as a corrupted view |

### One capability was refused, and the refusal is the finding

`CREATE_SCOPE_BOX` was in the batch and was dropped. **Revit exposes no way to create a scope box on
any supported release** — checked against the shipped assemblies for **2020 AND 2027**, where the only
matches for the word are print and export flags and a few parameter ids. The earlier library had
established this on 2020 alone; this confirms it at the far end, which is what the `CREATE_CEILING`
lesson demands of every "impossible".

So there is no fragment. The sentence *"put a scope box round this area"* is answered instead by a row
in `SET_VIEW_SECTION_BOX`'s routing table saying plainly that nothing can, and that one drawn by hand
works with everything else here. **A fragment whose only behaviour is to refuse would be a capability
that does nothing**, and the library counts capabilities.

### The metadata check caught three things a compiler would have

It is not a compiler and it is not a substitute for one, and it still paid for itself twice more this
session:

- **`VariesAcrossGroups` is on `InternalDefinition`, not on `Definition`.** The obvious line —
  `parameter.Definition.VariesAcrossGroups` — compiles on **no release at all**. Caught before it was
  written, and the definition is cast first.
- **`Ceiling.Create` is absent from the 2020 assembly and present on 2024.** That confirmed the run-time
  lookup was necessary rather than defensive.

> **And a warning about the tool itself: walk the BASE types.** `Space` declares neither `Number` nor
> `Area`; `HostObjAttributes` does not declare `FamilyName`. Both inherit them. A member check that
> stops at the declared type reports a false absence, and a session that trusts it will rewrite working
> code to avoid a member that was there all along.

### An `mcp` name collision that cost twenty minutes, and does not affect the repo

Last session installed the MCP SDK to run `test_mcp_serves.py`. **The pip package is called `mcp` and so
is this repository's own top-level directory** — and `mcp/` has no `__init__.py`, so it is a namespace
package, which a regular installed package **outbeats regardless of `sys.path` order**. After that,
`from mcp.server import heron_brain` reaches the SDK and fails.

**The repository is not affected**: it imports `heron_brain` as a top-level module after putting
`mcp/server` on the path, which is immune. What broke was an ad-hoc call written the natural way. The
SDK was uninstalled — it was broken in this container anyway, its `cryptography` bindings panicking on
import — and `test_mcp_serves.py` is back to its honest skip.

> To ask the brain a question from the repo root: `sys.path.insert(0, "mcp/server")` then
> `import heron_brain`. Not `from mcp.server import ...`.

### The routing, measured against the 146 that were there before

| | before | after |
|---|---|---|
| Utterances | 860 | 906 |
| Claimed in a routing table and not reached | 3 | **3** — the same three, all near-synonym pairs |
| Shortlist collisions | 122 (14.2%) | 129 (14.2%) |

**No new unreached claims this time**, because every quoted sentence was kept on one line and matched to
a declared utterance — the two mistakes of the previous batch. Reciprocal rows went into twenty
counterpart fragments, and all six new collisions resolve correctly through `heron_brain.lookup`,
checked one at a time.

**One row written last session had already gone stale and was corrected**: `PLACE_MEP_FITTING`'s table
said *"join these walls together"* was covered by nothing in the library. True when it was written, and
false one session later. A cross-reference that names an absence dates the moment the absence is filled.

---
