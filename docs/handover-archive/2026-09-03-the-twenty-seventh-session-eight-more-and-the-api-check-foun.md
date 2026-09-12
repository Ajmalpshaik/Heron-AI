# The twenty-seventh session, 2026-09-03 — eight more, and the API check found a third shape of version break

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **194 to 202**. Same container, same limitation. `A9` now names
**sixty-four**.

| | |
|---|---|
| `CREATE_VIEW_FILTERS_BY_VALUE` | *"Make a filter for every different system type"* was answering `CREATE_VIEW_FILTER`, which makes ONE. **Deferred twice as too close to `COLOR_BY_PARAMETER` and built on the third asking**, because reading both purposes settled it: that one writes per-element OVERRIDES and an element drawn tomorrow gets nothing; this writes real FILTERS that re-evaluate forever. Investigate with one, set a standard with the other. **The category set comes from the elements, never guessed** — a filter naming a category that lacks the parameter is rejected by Revit *entirely*, so being helpful about it destroys the whole run |
| `REMOVE_VIEW_FILTER` | *"Take that filter off the view"* was answering `COPY_VIEW_FILTERS`. **Off one view and deleted from the project are completely different in reach**, so deleting is a separate flag and the fragment reports how many OTHER views use it first — a filter on one view is a leftover, one on twenty is somebody's standard |
| `REPORT_CATEGORY_VISIBILITY` | *"Which categories are turned off in this view"* was answering `SET_CATEGORY_VISIBILITY` — the fragment that turns things off. **It deliberately does NOT scope to the view, which is the opposite decision from `REPORT_CATEGORY_OVERRIDES` built two sessions ago**: a hidden category's elements do not appear in a view-scoped collector at all, so scoping would hide exactly what it exists to find. The two look alike and the difference is not a style choice |
| `SET_CROP_BOX_SETTINGS` | *"The tags are printing outside the crop"* had no answer. **The annotation crop is the half nothing else covers and the one that spoils sheets** — the model crop trims geometry while tags and dimensions keep printing outside it. Each flag is optional and empty means LEAVE ALONE, because a batch that forces three settings is how a drawing set loses its crop boundaries overnight |
| `CHECK_SURFACE_FIT` | *"Is the equipment sitting flat on the floor"* was answering `SNAP_TO_GRID` — which moves things. **A single centre ray is right in the middle of a surface and lies at the edges, and edges are where the mistakes are.** Five sample points, four named verdicts: STRADDLING, OVERHANGING, UNEVEN, SLOPED. Its real use is deciding which elements are safe to move automatically |
| `CREATE_MEP_SYSTEM_TYPE` | *"Make a duct system type"* was answering `READ_MEP_SYSTEM`. **There is no create call on any release** — one is made by duplicating — so the fragment's real job is the parent: the new type inherits its classification and **that cannot be changed afterwards**. Copy a Return to make a Supply and it behaves as a Return forever while reading correctly on every drawing |
| `REMOVE_PARAMETER_VALUE` | *"Clear the value out of this parameter"* was answering `COPY_PARAMETER_VALUE`. **`WRITE_ELEMENT_PARAMETERS` cannot do this, and the reason is its own best decision** — it takes the value as TEXT on purpose, and an empty string clears a text field but not a length, a number or an element reference. A blank and a zero are different things in a schedule |
| `ASSIGN_LOCATION_DATA` | *"Put the room name on all the equipment"* was answering `FIND_DUPLICATE_ELEMENTS`. Not the parameter writer, because **the value is not given — it is worked out per element**. The probe point is nudged to the room's own mid-height, and without that a ceiling diffuser tests false from its own position: the obvious version leaves every air terminal blank, which is exactly the set somebody built the register for |

**The API check earned its keep a seventh time, and on a third distinct shape of version break.** The
first three were members a release does not ship. The fourth was a member present on the OLD end and gone
by the new. The fifth was a whole capability removed. The sixth was reflection that still named a missing
TYPE in its own lookup. **This one is an OVERLOAD whose ARITY changed**:
`ParameterFilterRuleFactory.CreateEqualsRule` for a text value is `(ElementId, string, bool)` on Revit
2020 and `(ElementId, string)` by 2027 — the case-sensitivity argument was dropped. Both releases HAVE
the member; the member's name gives nothing away; **each spelling compiles on exactly one end and breaks
the other**, and only reading the two signatures byte for byte shows it. `CREATE_VIEW_FILTERS_BY_VALUE`
chooses the overload at run time by argument count. **A member being present at both ends does not mean
the call is.**

**Four sentences were already answered and are recorded rather than rebuilt.** *"Put the categories back
to normal in this view"* → `SET_CATEGORY_GRAPHICS` with an EMPTY settings object, which its own purpose
already names as the reason there is no separate clear. *"Which pipes are still open at the end"* →
`FIND_DEAD_ENDS`. *"Make a named set I can pick again later"* → `CREATE_SELECTION_FILTER`. *"Create
spaces in every enclosed area"* → `PLACE_ROOMS`, which places rooms **or** spaces. All four were declared
as utterances so the routing now matches the decision.

**A fifth impossibility joins the list:** **a phase cannot be CREATED from the API on any release** —
`Document.Phases` is a read-only collection and no factory offers one, checked at both ends. That is now
a routing row on `REPORT_PHASES`, alongside the scope box, the design-option activation, the first legend
and splitting a wall.

**And a small thing worth the sentence.** A routing row I wrote this session claimed *"which ones have no
value"* for `DESCRIBE_BLANK_PARAMETERS` — a sentence `READ_ELEMENT_PARAMETERS` already declares. The
checker caught it, and the fix was to correct **my table** to a sentence that fragment really declares,
not to move the sentence. A routing table is a claim about the index, and the index is the authority.

---
