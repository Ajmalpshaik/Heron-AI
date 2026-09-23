# Fragment issues — section 3d

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 3d. GAPS — what proving showed is MISSING, not broken

Opened 2026-09-08 on the owner's instruction: *"if you find a gap also mention that, and new fragments
we need, we need to split this fragment, add new, edit — like that also mention in that list."*

Everything here was met by needing it, not by imagining it.

### New fragments the library wants

`list-levels`, `list-worksets`, `list-grids`, `list-sheets`, `list-revisions` and `list-linked-models`
all exist. The gaps below are the same shape and were each hit by having to write a throwaway probe
instead — several times in one session.

| Wanted | Why it was missed | How often today |
|---|---|---|
| **`LIST_VIEWS`** | `find-views` needs a `viewType` AND a `nameContains` before it answers. There is no way to ask *"what views are there"* — which is the first question of every proof that takes a view, and **53 fragments take one** | Every single view-based proof. The most-needed missing fragment of the day |
| `LIST_VIEW_TEMPLATES` | `select-view-templates` selects; nothing lists. Needed the names of all 18 before anything could be done with a template | 3 times |
| `LIST_LINE_STYLES` | `remap-line-styles` takes two style names and there is no way to discover one | 1 |
| `LIST_MEP_SYSTEM_TYPES` | `create-mep-system-type` takes `copyFromName` and nothing lists the 14 that exist | 1 |
| `LIST_MATERIALS` | `find-unused-materials` reports only the unused ones. There is no list of the materials that ARE used | 1 |
| `LIST_DWG_EXPORT_SETUPS` | `export-views-to-dwg` refuses without a named setup — *"Revit's default is not used"* — and nothing lists the setups a project has. It could not be proved at all for want of one name | 1 |

**The pattern is one sentence: every fragment that takes a NAME needs a way to discover the names.**
A caller who cannot discover a value cannot supply one, and [D-54](../DECISIONS.md) made supplying them
possible without making them findable.

### …and two fragments already show the cheaper fix

**A new `LIST_*` fragment is not the only answer, and may not be the best one.** Two fragments proved
today already solve it for themselves, by handing back the alternatives **in the refusal**:

| Fragment | What it returns when it cannot match the name |
|---|---|
| `set-print-settings` | `availableSizes` — all 79 the print driver offers |
| `open-view` | `matches` — what the name DID match, and why it was refused: *"[view template, cannot be opened]"* |

One round trip instead of two, and the list arrives exactly when it is wanted — at the moment somebody
got the name wrong. **Deciding between the two shapes is part of the sit-down**, because doing both
means the same list is maintained in two places. The five `LIST_*` rows above are written as new
fragments only because that is how the library already answers this question elsewhere
(`list-levels`, `list-worksets`, `list-grids`); the convention below may be the better trade.

### One fragment to SPLIT

| Fragment | Why |
|---|---|
| `check-family-standards` | It answers two unrelated questions under one word. `namePattern=*`, which everything matches, still reported **97 off standard** — because "off standard" also counts families with `(0 placed)`. *"This family is named wrongly"* and *"this family is loaded and never used"* are different findings with different fixes, and `find-unused-families` already owns the second one. Merged, neither can be proved: there is no arrangement that empties both at once |

### A decision to make, then edits to follow

**A name clash refuses in some creators and renames in others**, and nothing says which is right:

| Refuses | Renames anyway |
|---|---|
| `create-level` | `create-levels` |
| `create-drafting-view` | `create-view-template-from-view` |
| | `create-sheet-list` |
| | `create-key-schedule` |
| | `duplicate-type` — *"'Tees' duplicated as 'Tees'"* |

**Five against two now**, so the majority behaviour is renaming. If the decision goes that way it is two
fragments to change, not five — but it also means a caller cannot rely on a clash being refused
anywhere, which is the more important half.

Both behaviours are defensible. What is not defensible is that a caller cannot predict which they will
get, and it cost three failed negative cases today before the pattern was visible. **Decide once, then
edit the minority to match** — and note that `create-level` and `create-levels`, the singular and plural
of the same idea, are on opposite sides.

### One edit, small and specific

| Fragment | Edit |
|---|---|
| `set-crop-box-settings` | Its three settings accept `on`/`off`/`true`/`false`/`yes`/`no` — every value is a change. There is no way to say **leave this one alone**, so a caller wanting to turn the crop on without touching the annotation crop cannot. It also means the fragment has no empty case: both `on` and `off` report `changed 1`. Add a `leave` value, and make it the default |

---
