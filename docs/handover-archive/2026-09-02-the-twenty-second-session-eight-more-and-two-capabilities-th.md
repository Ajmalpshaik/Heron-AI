# The twenty-second session, 2026-09-02 — eight more, and two capabilities that cannot be built

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **154 to 162**, same container, same limitation. `A9` now names
**twenty-four** fragments no compiler has read.

Chosen the same way: the owner's sentences through `heron_brain.lookup`, wrong answers first.

| | |
|---|---|
| `AUTO_SIZE_MEP` | *"auto size the ducts from the flow"* was answering `MEASURE_MEP_VELOCITY` — a read. Sizes from the flow Revit already has, **always rounding UP**: rounding to the nearer size pushes velocity above the design figure, which is the direction that causes noise and rework. **The unit assumption is printed before anything is written** — the raw internal value beside Revit's own display string, so a wrong constant shows itself on the first run rather than through a mis-sized system |
| `CONNECT_AIR_TERMINALS` | *"connect the air terminals to the duct"* was answering `REPORT_CONNECTOR_LOADS`. This is the **TAP**, cut into the duct's side — not `CONNECT_OPEN_ENDS`, which butt-joins two ends already at one point. Nearest is measured to the terminal's **connector**, which on a ceiling diffuser is on top of it; measuring from the diffuser body picks the wrong duct where two run side by side. **The call returns a bool, and false is not an exception** |
| `HIGHLIGHT_VS_REST` | *"grey everything except the ducts"* was answering `SET_CATEGORY_GRAPHICS`. **This is the owner's own grayout**, and *"do the grayout"* now lands on it. Insulation and lining travel with their host **both ways** — his standing rule: colour a duct and you colour its insulation, or you cannot see it |
| `APPLY_VIEW_FILTER` | *"apply this filter to the view"* was answering `APPLY_VIEW_TEMPLATE` — a far bigger change than the one asked for. The missing link after `CREATE_VIEW_FILTER`: without it a filter exists in the project and does nothing to any drawing. **Projection and cut are both the caller's job**, and a half-built override is warned about, because Revit derives neither from the other |
| `CREATE_FLOOR` | *"make a floor here"* was answering `CREATE_PLAN_VIEW`. Same version split as the ceiling and confirmed the same way: **`Floor.Create` is absent on 2020 and `Document.Create.NewFloor` is gone by 2024**, so both are looked up at run time. Project elevation again, not elevation |
| `RENAME_FAMILY` | *"rename the family"* was answering `LOAD_FAMILY` — a write of the wrong kind. It renames the family EVERYWHERE, and says how many types and instances went with it. **The .rfa on disk keeps its own name**, so reloading brings the old one back — worth knowing before somebody renames the same family twice |
| `CREATE_LEGEND_VIEW` | *"make a legend"* was answering `CREATE_SCHEDULE`. Duplicating is the only route: **Revit exposes no legend creation method on any release**, so a project with none cannot get its first from a script, and this says so plainly rather than failing obscurely |
| `CHECK_ROOM_MEP_COMPLETENESS` | *"which rooms have no mep in them"* was answering `PLACE_ROOMS`. Checks rooms against a rule and reports what is short. **The total found per category is printed first** — a category with zero anywhere means the devices are in a linked model, and without that line every room reads as missing them |

### Two capabilities cannot be built, and both refusals are the finding

- **`SET_DESIGN_OPTION`.** An element only lands in a design option while that option is ACTIVE, and
  **no option can be made active from a script** — checked against 2020 AND 2027, where the only member
  anywhere is the read-only `GetActiveDesignOptionId`. There is no setter on `Document`, on
  `DesignOption`, or anywhere else. A row in `SET_ELEMENT_WORKSET`'s table says so.
- **`PURGE_UNUSED`.** This one is *possible* and is deliberately not built. `FIND_UNUSED_FAMILIES`
  reports and deletes nothing, which the eighteenth session decided on purpose: purging is hard to
  reverse, Revit has the command, and doing it to a shared model from here would be the most damaging
  thing in the library. **Building it now would quietly undo that decision**, so the row in that
  fragment's table records why instead.

> That is three impossibilities established at both ends of the range in two sessions — scope box,
> design option, and the legend's first instance. **The pattern is worth the sentence: check the far
> end before writing "cannot".** The `CREATE_CEILING` lesson exists because somebody did not.

### A third false absence, and this one was in the checking tool

The metadata reader matches a type by its SHORT name and returns the first hit — and for several types
that first hit is an **internal marshalling struct**, not the real class. `OverrideGraphicSettings` reads
as having **no constructors at all** that way; it has two, including the copy constructor this session
needed. Match the full name.

That is now three ways the same tool can report a member that exists as missing: a base-type member, an
inherited one, and a short-name collision. All three are in `A9` where the method is described.

### The routing, measured against the 154 that were there before

| | before | after |
|---|---|---|
| Utterances | 906 | 946 |
| Claimed in a routing table and not reached | 3 | **3** — the same three |
| Shortlist collisions | 129 (14.2%) | 148 (15.6%) |

**The collision rate rose more than in the previous two batches**, which is what adding into the two
most crowded areas — views and MEP — does. Every one of the twenty new collisions was checked through
`heron_brain.lookup` and resolves correctly; the keyword route is not what the host calls. One new
unreached claim appeared and was fixed: `HIGHLIGHT_VS_REST`'s table claimed *"bring these forward"*,
which `OVERRIDE_GRAPHICS_IN_VIEW` already owns as *"bring the services forward"* — the table row was
changed rather than the sentence declared, because declaring it would have been a real fight over a
sentence another fragment answers correctly.

Reciprocal rows went into twenty-one counterpart fragments.

---
